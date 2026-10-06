import { useCallback, useEffect, useRef, useState } from 'react';
import type { PlanningWorkbookResponseDto } from '../api/client';
import {
  fetchPlanningWorkbook,
  refreshPlanningWorkbook,
  toPlanningWorkbookError,
  type PlanningWorkbookError,
} from '../api/planningWorkbookApi';
import { DEFAULT_PLANNING_HORIZON_WEEKS, MIN_PLANNING_HORIZON_WEEKS } from '../planningWorkbook/planningWorkbookSummary';

const HORIZON_STORAGE_KEY = 'kst.planningWorkbook.horizonWeeks.v1';

/** How often an open Planning view re-checks the local business week (also checked on focus/visibility). */
export const BUSINESS_WEEK_CHECK_INTERVAL_MS = 60_000;

/** Planning's own horizon preference; deliberately independent of the MPS Dashboard horizon. */
export function loadPlanningHorizon(): number {
  try {
    const value = Number(window.localStorage.getItem(HORIZON_STORAGE_KEY));
    return Number.isInteger(value) && value >= MIN_PLANNING_HORIZON_WEEKS ? value : DEFAULT_PLANNING_HORIZON_WEEKS;
  } catch {
    return DEFAULT_PLANNING_HORIZON_WEEKS;
  }
}

/** Sunday start (local date, yyyy-MM-dd) of the accepted Sunday–Saturday business week containing `now`. */
export function currentBusinessWeekStart(now: Date = new Date()): string {
  const sunday = new Date(now.getFullYear(), now.getMonth(), now.getDate() - now.getDay());
  return `${sunday.getFullYear()}-${String(sunday.getMonth() + 1).padStart(2, '0')}-${String(sunday.getDate()).padStart(2, '0')}`;
}

type Keyed<T> = { identity: string; value: T };

/**
 * Loads the read-only Planning Workbook for one workspace context (assignment, site, loaded MPS
 * snapshot, Planning horizon and local business week). Every result is keyed by that context, so data
 * for a previous workspace, snapshot, horizon or business week is never shown for the current one, and
 * late responses are ignored. refresh() calls only the workbook refresh endpoint; MPS is never loaded
 * or refreshed here.
 *
 * The owner-confirmed horizon range is 12 weeks up to the MPS-derived maximum the API reports for
 * the loaded snapshot and current business week. When that maximum is below the current selection
 * (the API answers 400 with maximumHorizonWeeks), the selection is clamped and the request repeated.
 *
 * While the view stays open (including after the computer sleeps), the business week is re-checked
 * periodically and on focus/visibility. A new week starts a normal GET for the current projection
 * (new week labels and maximum); it never calls the refresh endpoint.
 */
export function usePlanningWorkbook(assignmentId: string, site: string, snapshotId: string | null) {
  const [horizonWeeks, setHorizonState] = useState(loadPlanningHorizon);
  const [weekStart, setWeekStart] = useState(() => currentBusinessWeekStart());
  const [maximum, setMaximum] = useState<Keyed<number> | null>(null);
  const [result, setResultState] = useState<Keyed<PlanningWorkbookResponseDto> | null>(null);
  const [failure, setFailure] = useState<Keyed<PlanningWorkbookError> | null>(null);
  const [refreshFailure, setRefreshFailure] = useState<Keyed<PlanningWorkbookError> | null>(null);
  const [refreshing, setRefreshing] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);
  const sequence = useRef(0);
  const resultRef = useRef<Keyed<PlanningWorkbookResponseDto> | null>(null);
  const context = `${assignmentId}|${site}|${snapshotId ?? ''}`;
  const identity = `${context}|${horizonWeeks}|${weekStart}`;

  const setResult = useCallback((next: Keyed<PlanningWorkbookResponseDto> | null) => {
    resultRef.current = next;
    setResultState(next);
  }, []);

  // Business-week rollover detection; setting an unchanged week is a no-op render-wise.
  useEffect(() => {
    const check = () => setWeekStart(currentBusinessWeekStart());
    const onVisibility = () => { if (document.visibilityState === 'visible') check(); };
    const timer = window.setInterval(check, BUSINESS_WEEK_CHECK_INTERVAL_MS);
    window.addEventListener('focus', check);
    document.addEventListener('visibilitychange', onVisibility);
    return () => {
      window.clearInterval(timer);
      window.removeEventListener('focus', check);
      document.removeEventListener('visibilitychange', onVisibility);
    };
  }, []);

  const accept = useCallback((data: PlanningWorkbookResponseDto, horizon: number) =>
    data.workspaceId === assignmentId && data.site === site && data.mpsSnapshotId === snapshotId
      && Number(data.horizonWeeks) === horizon, [assignmentId, site, snapshotId]);

  /** Records the reported range; a smaller maximum clamps the selection (not persisted as a preference). */
  const applyMaximum = useCallback((max: number) => {
    setMaximum({ identity: context, value: max });
    setHorizonState(current => (current > max ? Math.max(MIN_PLANNING_HORIZON_WEEKS, max) : current));
  }, [context]);

  useEffect(() => {
    const current = ++sequence.current;
    if (!snapshotId) return;
    void fetchPlanningWorkbook(assignmentId, snapshotId, horizonWeeks)
      .then(data => {
        if (current !== sequence.current) return;
        if (!accept(data, horizonWeeks)) { setFailure({ identity, value: { type: 'mismatch' } }); return; }
        setMaximum({ identity: context, value: Number(data.maximumHorizonWeeks) });
        setResult({ identity, value: data });
        setFailure(null);
      })
      .catch((err: unknown) => {
        if (current !== sequence.current) return;
        const error = toPlanningWorkbookError(err);
        if (error.type === 'horizon-range' && error.maximum < horizonWeeks) { applyMaximum(error.maximum); return; }
        setFailure({ identity, value: error });
      });
  }, [assignmentId, snapshotId, horizonWeeks, identity, context, attempt, accept, applyMaximum, setResult]);

  const knownMaximum = maximum?.identity === context ? maximum.value : null;
  const workbook = result?.identity === identity ? result.value : null;

  const setHorizonWeeks = useCallback((next: number) => {
    if (!Number.isInteger(next) || next < MIN_PLANNING_HORIZON_WEEKS) return;
    if (knownMaximum !== null && next > knownMaximum) return;
    setHorizonState(next);
    try { window.localStorage.setItem(HORIZON_STORAGE_KEY, String(next)); } catch { /* preference only */ }
  }, [knownMaximum]);

  const retry = useCallback(() => { setFailure(null); setAttempt(a => a + 1); }, []);

  const refresh = useCallback(async () => {
    // Guard: refresh only replaces a loaded workbook for the current context. While the current
    // context is still loading (initial load, changed workspace/snapshot/horizon/week), starting a
    // refresh would supersede that pending request, so it is not allowed.
    if (!snapshotId || resultRef.current?.identity !== identity) return;
    const current = ++sequence.current;
    const requested = identity;
    setRefreshing(requested);
    setRefreshFailure(null);
    try {
      const data = await refreshPlanningWorkbook(assignmentId, snapshotId, horizonWeeks);
      if (current !== sequence.current) return;
      if (!accept(data, horizonWeeks)) { setResult(null); setFailure({ identity: requested, value: { type: 'mismatch' } }); return; }
      setMaximum({ identity: context, value: Number(data.maximumHorizonWeeks) });
      setResult({ identity: requested, value: data });
      setFailure(null);
    } catch (err) {
      if (current !== sequence.current) return;
      const error = toPlanningWorkbookError(err);
      if (error.type === 'horizon-range' && error.maximum < horizonWeeks) { applyMaximum(error.maximum); return; }
      const retained = resultRef.current?.identity === requested;
      if (retained && (error.type === 'unavailable' || error.type === 'error')) {
        // Compatible retained data (same context) stays visible with a warning.
        setRefreshFailure({ identity: requested, value: error });
      } else {
        // Nothing compatible to keep (or 404/409): show the failure explicitly; never leave loading.
        setResult(null);
        setFailure({ identity: requested, value: error });
      }
    } finally {
      setRefreshing(r => (r === requested ? null : r));
    }
  }, [assignmentId, snapshotId, horizonWeeks, identity, context, accept, applyMaximum, setResult]);

  const error = failure?.identity === identity ? failure.value : null;
  return {
    horizonWeeks,
    minimumHorizonWeeks: MIN_PLANNING_HORIZON_WEEKS,
    maximumHorizonWeeks: knownMaximum,
    setHorizonWeeks,
    workbook,
    error,
    refreshError: refreshFailure?.identity === identity ? refreshFailure.value : null,
    isLoading: !!snapshotId && !workbook && !error,
    isRefreshing: refreshing === identity,
    canRefresh: !!snapshotId && !!workbook && refreshing !== identity,
    retry,
    refresh,
  };
}

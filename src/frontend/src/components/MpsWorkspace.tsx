import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import type { MpsDashboardResponseDto, WorkOrderImmediateMaterialComponentDto } from '../api/client';
import { fetchPlanningWindow, toWorkOrdersApiError } from '../api/workOrdersApi';
import { useFiscalCalendarSettings } from '../hooks/useFiscalCalendarSettings';
import {
  MAX_MPS_HORIZON_WEEKS,
  MIN_MPS_HORIZON_WEEKS,
} from '../hooks/useMpsDashboard';
import type { MpsApiError, MpsDateBasis } from '../api/mpsApi';
import { usePartDetail } from '../hooks/usePartDetail';
import { usePlanningWindowWorkOrders } from '../hooks/usePlanningWindowWorkOrders';
import { useBom } from '../hooks/useBom';
import { useComponentDetail } from '../hooks/useComponentDetail';
import { useApprovedVendors } from '../hooks/useApprovedVendors';
import { useWorkOrderImmediateMaterial } from '../hooks/useWorkOrderImmediateMaterial';
import { useWorkOrderImmediateMaterialSummary } from '../hooks/useWorkOrderImmediateMaterialSummary';
import { PartInfoPanel } from './PartInfoPanel';
import { BomPanel } from './BomPanel';
import { ComponentInfoModal } from './ComponentInfoModal';
import { WorkOrdersPanel } from './WorkOrdersPanel';
import { WorkOrderCandidatePanel } from './WorkOrderCandidatePanel';
import { getFiscalDisplayInfo } from '../fiscal/fiscalCalendar';
import { captureNavigationScroll, navigationLabel, restoreNavigationScroll, type MpsNavigationLevel } from '../mps/mpsNavigation';
import {
  type BucketSelection,
  bucketCellClassNames,
  describeBucket,
  describeBucketSelection,
  formatQuantity,
  formatWeekLabel,
  groupConsecutive,
  isWeeklyBucketWorkOrderEligible,
  parseIsoDateOnly,
  withPeriodColor,
  withQuarterColor,
} from '../mps/mpsPresentation';
import './MpsWorkspace.css';
import { AnalyticalExcelExport } from './AnalyticalExcelExport';
import { mpsExportRequest } from '../mps/mpsExport';
import { useLocalBusinessDate } from '../hooks/useLocalBusinessDate';
import type { MpsReportOrigin } from '../reports/partInfoShortcut';

interface MpsWorkspaceProps {
  active?: boolean;
  onShowMrp?: (origin: MpsReportOrigin) => Promise<void>;
  assignmentId: string;
  dashboard: MpsDashboardResponseDto | null;
  dateBasis: MpsDateBasis;
  horizonWeeks: number;
  isLoading: boolean;
  isRefreshing: boolean;
  error: MpsApiError | null;
  setDateBasis: (dateBasis: MpsDateBasis) => void;
  setHorizonWeeks: (horizonWeeks: number) => void;
  reload: () => Promise<void>;
  refresh: () => Promise<void>;
}

export function MpsWorkspace({
  assignmentId, dashboard, dateBasis, horizonWeeks, isLoading, isRefreshing, error, setDateBasis, setHorizonWeeks, reload, refresh, active = true, onShowMrp,
}: MpsWorkspaceProps) {
  const businessDate = useLocalBusinessDate();
  const { settings: fiscalSettings } = useFiscalCalendarSettings();

  // One transient trail owns selection, rendering and all navigation commands. Workspace/site
  // changes clear it; snapshot/basis changes retain only a still-valid MPS parent. Part Info/BOM
  // remain parent-scoped. Switching modules retains valid state, but cancels pending bucket entry.
  const identity = JSON.stringify([assignmentId, dashboard?.snapshot.snapshotId, dashboard?.snapshot.site, dateBasis]);
  const [previousContext, setPreviousContext] = useState({ assignmentId, site: dashboard?.snapshot.site, snapshotId: dashboard?.snapshot.snapshotId });
  const [navigation, setNavigation] = useState<{ identity: string; levels: MpsNavigationLevel[]; tab: 'partInfo' | 'bom' | 'workOrders' }>({ identity, levels: [], tab: 'partInfo' });
  const [bucketFeedback, setBucketFeedback] = useState<{ identity: string; pending: boolean; error: boolean; text: string } | null>(null);
  const [wasActive, setWasActive] = useState(active);
  if (wasActive !== active) { setWasActive(active); setBucketFeedback(null); }
  // Render-time masking prevents even one committed frame of obsolete navigation on identity change.
  const previousParent = navigation.levels[0];
  const keepParent = previousContext.assignmentId === assignmentId && previousContext.site === dashboard?.snapshot.site && previousParent?.kind === 'part' && dashboard?.parts.some((part) => part.parentPart === previousParent.part);
  const resetLevels = keepParent ? [{ ...previousParent, trigger: null, scroll: undefined }] : [];
  const resetTab = keepParent && previousContext.snapshotId === dashboard?.snapshot.snapshotId && navigation.tab === 'bom' ? 'bom' : 'partInfo';
  const levels = navigation.identity === identity ? navigation.levels : resetLevels;
  const activeTab = navigation.identity === identity ? navigation.tab : resetTab;
  const selectedParent = levels[0]?.kind === 'part' ? levels[0].part : null;
  const bucketLevel = levels.find((level) => level.kind === 'bucket');
  const selectedBucket = bucketLevel?.kind === 'bucket' ? bucketLevel.selection : null;
  const firstCandidateIndex = levels.findIndex((level) => level.kind === 'candidates');
  const rootMaterial = levels.slice(0, firstCandidateIndex < 0 ? levels.length : firstCandidateIndex).find((level) => level.kind === 'material');
  const selectedWoid = rootMaterial?.kind === 'material' ? rootMaterial.woid : null;
  const bucketOwner = useRef({ alive: true, attempt: 0 });
  useLayoutEffect(() => {
    const owner = bucketOwner.current;
    owner.alive = true;
    return () => { owner.alive = false; owner.attempt++; };
  }, [identity, active]);
  useEffect(() => {
    if (!bucketFeedback || bucketFeedback.pending || bucketFeedback.error) return;
    const timer = setTimeout(() => setBucketFeedback(null), 4000);
    return () => clearTimeout(timer);
  }, [bucketFeedback]);
  const [shortageInspection, setInspectedShortage] = useState<{
    identity: string;
    row: WorkOrderImmediateMaterialComponentDto;
    returnFocusEl: HTMLElement | null;
  } | null>(null);
  // Stage 8D.6: the component currently inspected in the blocking Component Information modal,
  // plus the originating BOM row so focus can be restored to it on close. Modal state is owned
  // here (not by BomPanel) so it survives independently of BOM filter/search state.
  const [componentInspection, setInspectedComponent] = useState<{
    identity: string;
    componentPart: string;
    returnFocusEl: HTMLElement | null;
  } | null>(null);
  const inspectedShortage = shortageInspection?.identity === identity ? shortageInspection : null;
  const inspectedComponent = componentInspection?.identity === identity ? componentInspection : null;
  if (navigation.identity !== identity) {
    setNavigation({ identity, levels: resetLevels, tab: resetTab });
    setPreviousContext({ assignmentId, site: dashboard?.snapshot.site, snapshotId: dashboard?.snapshot.snapshotId });
    setInspectedShortage(null);
    setInspectedComponent(null);
    setBucketFeedback(null);
  }
  // Keyed by parentPart so Escape can restore focus to the exact grid row that was drilled into,
  // even though the row stays mounted (never removed) while its detail view is open.
  const rowRefs = useRef<Map<string, HTMLTableRowElement>>(new Map());
  const workspaceRef = useRef<HTMLDivElement>(null);
  const currentIdentity = useRef(identity);
  useLayoutEffect(() => { currentIdentity.current = identity; }, [identity]);

  function showComponentMrp() {
    const snapshot = dashboard?.snapshot;
    if (!active || !inspectedComponent || !snapshot?.snapshotId || !snapshot.site || !onShowMrp) return;
    const inspection = inspectedComponent;
    const root = workspaceRef.current;
    // Capture all mounted grid/detail scrollers, including hidden candidate panes, plus shell ancestors.
    const scroll = [...captureNavigationScroll(root), ...[...(root?.querySelectorAll<HTMLElement>('*') ?? [])]
      .filter(element => element.scrollLeft !== 0 || element.scrollTop !== 0)
      .map(element => ({ element, left: element.scrollLeft, top: element.scrollTop }))];
    void onShowMrp({ assignmentId, snapshotId: snapshot.snapshotId, site: snapshot.site, part: inspection.componentPart,
      isCurrent: () => currentIdentity.current === identity && !!root?.isConnected,
      depart: () => setInspectedComponent(null),
      restore: () => {
        if (currentIdentity.current !== identity || root?.closest('[hidden], [inert]')) return;
        const target = inspection.returnFocusEl?.isConnected ? inspection.returnFocusEl : root?.querySelector<HTMLElement>('.mps-trail [aria-current]');
        target?.focus({ preventScroll: true });
        restoreNavigationScroll(scroll);
      },
    });
  }

  const { detail: partDetail, isLoading: isPartDetailLoading, error: partDetailError, retry: retryPartDetail } =
    usePartDetail(assignmentId, selectedParent);

  // Stage 7R parent-level planning window. A bucket entry separately acquires the same existing
  // endpoint's eligible bucket population before committing navigation; do not prefetch the full
  // parent population for a direct bucket entry.
  const {
    workOrders: planningWindowWorkOrders,
    isLoading: isPlanningWindowLoading,
    error: planningWindowError,
    retry: retryPlanningWindow,
  } = usePlanningWindowWorkOrders(
    assignmentId,
    dashboard?.snapshot.snapshotId ?? null,
    selectedBucket ? null : selectedParent,
    dateBasis,
    null,
  );

  // BOM is parent-contextual (never bucket/basis/horizon-contextual) and lazy: the request fires
  // only on the first explicit BOM-tab activation for the current (workspace, parent, snapshot)
  // identity — the tab click is the only path that starts a request, so a successful MPS refresh
  // (new snapshot id) invalidates the loaded BOM with no transient request and the next explicit
  // activation re-requests; a failed refresh leaves the snapshot id — and the displayed BOM —
  // untouched. Obsolete in-flight responses never commit to state (useBom).
  const { bom, isLoading: isBomLoading, error: bomError, activate: activateBom, retry: retryBom } = useBom(
    assignmentId,
    selectedParent,
    dashboard?.snapshot.snapshotId ?? null,
  );

  // Component Detail is lazy per Stage 8D.6: it loads only while a component is being inspected
  // (a BOM row was clicked) and never preloads for the whole BOM. Closing the modal
  // (`inspectedComponent = null`) immediately invalidates the identity so a late response for a
  // just-closed or just-replaced component can never populate the modal (see useComponentDetail).
  const {
    detail: componentDetail,
    isLoading: isComponentDetailLoading,
    error: componentDetailError,
    retry: retryComponentDetail,
  } = useComponentDetail(assignmentId, inspectedComponent?.componentPart ?? null);

  // Approved Vendors (Stage 8D.7) is independently lazy: no request until the modal's AVL section
  // is explicitly expanded. Owned at this level (mirroring Component Detail) so the modal itself
  // stays a pure render/props component.
  const {
    rows: approvedVendors,
    isLoading: isApprovedVendorsLoading,
    error: approvedVendorsError,
    activate: activateApprovedVendors,
    retry: retryApprovedVendors,
  } = useApprovedVendors(assignmentId, inspectedComponent?.componentPart ?? null);

  const { analysis: immediateMaterialAnalysis, isLoading: isImmediateMaterialLoading, error: immediateMaterialError, retry: retryImmediateMaterial } = useWorkOrderImmediateMaterial(
    assignmentId, dashboard?.snapshot.snapshotId ?? null, selectedWoid, dateBasis, selectedWoid !== null,
  );
  const { summary: immediateMaterialSummary, error: immediateMaterialSummaryError } = useWorkOrderImmediateMaterialSummary(
    assignmentId, dashboard?.snapshot.snapshotId ?? null, dateBasis,
  );

  function handleSelectComponent(componentPart: string, rowElement: HTMLElement) {
    bucketOwner.current.attempt++;
    setBucketFeedback(null);
    setInspectedComponent({ identity, componentPart, returnFocusEl: rowElement });
  }

  function handleCloseComponentInfo() {
    const returnFocusEl = inspectedComponent?.returnFocusEl ?? null;
    setInspectedComponent(null);
    if (returnFocusEl && returnFocusEl.isConnected) {
      returnFocusEl.focus();
    }
  }

  function handleOpenMaterialLines(woid: string | null, trigger?: HTMLButtonElement) {
    selectWorkOrder(woid, trigger, selectedBucket ? 2 : 1);
  }

  function selectWorkOrder(woid: string | null, trigger: HTMLButtonElement | undefined, listLength: number) {
    if (!woid) { navigateTo(listLength); return; }
    bucketOwner.current.attempt++;
    setBucketFeedback(null);
    setInspectedShortage(null);
    setNavigation({ identity, levels: [...levels.slice(0, listLength), { kind: 'material', woid, trigger: trigger ?? null, scroll: captureNavigationScroll(trigger ?? null) }], tab: 'workOrders' });
  }

  function enterCandidates(row: WorkOrderImmediateMaterialComponentDto, trigger: HTMLElement, originWoid: string, depth: number) {
    if (depth > 3) return;
    const attempt = ++bucketOwner.current.attempt;
    setBucketFeedback(null);
    setNavigation({ identity, levels: [...levels, { kind: 'candidates', row, originWoid, depth, trigger, scroll: captureNavigationScroll(trigger) }], tab: 'workOrders' });
    setTimeout(() => {
      if (bucketOwner.current.alive && bucketOwner.current.attempt === attempt) workspaceRef.current?.querySelector<HTMLElement>('.mps-trail [aria-current]')?.focus();
    }, 0);
  }

  function handleSelectShortageDetail(row: WorkOrderImmediateMaterialComponentDto, returnFocusEl: HTMLElement) {
    bucketOwner.current.attempt++;
    setBucketFeedback(null);
    setInspectedShortage({ identity, row, returnFocusEl });
  }

  function handleCloseShortageDetail() {
    const returnFocusEl = inspectedShortage?.returnFocusEl ?? null;
    setInspectedShortage(null);
    if (returnFocusEl?.isConnected) returnFocusEl.focus();
  }

  function clearSelection() {
    const partToRefocus = selectedParent;
    const scroll = levels[0]?.scroll;
    const attempt = ++bucketOwner.current.attempt;
    setBucketFeedback(null);
    setNavigation({ identity, levels: [], tab: 'partInfo' });
    setInspectedShortage(null);
    setInspectedComponent(null);
    // Deferred: the detail panel unmounting and the grid reverting to its full (unfiltered) row
    // set both commit in this same update: check connectivity only after that commit lands.
    if (partToRefocus) {
      setTimeout(() => {
        if (!bucketOwner.current.alive || bucketOwner.current.attempt !== attempt) return;
        const row = rowRefs.current.get(partToRefocus);
        if (row?.isConnected) row.focus({ preventScroll: true });
        restoreNavigationScroll(scroll);
      }, 0);
    }
  }

  function navigateTo(length: number) {
    if (length === 0) { clearSelection(); return; }
    const removed = levels[length];
    const trigger = removed?.trigger;
    const attempt = ++bucketOwner.current.attempt;
    setBucketFeedback(null);
    setInspectedShortage(null);
    setNavigation({ identity, levels: levels.slice(0, length), tab: length === 1 ? 'partInfo' : 'workOrders' });
    setTimeout(() => {
      if (!bucketOwner.current.alive || bucketOwner.current.attempt !== attempt) return;
      if (trigger?.isConnected) trigger.focus({ preventScroll: true });
      else workspaceRef.current?.querySelector<HTMLElement>('.mps-trail [aria-current]')?.focus();
      restoreNavigationScroll(removed?.scroll);
    }, 0);
  }

  function setActiveTab(tab: 'partInfo' | 'bom' | 'workOrders') {
    bucketOwner.current.attempt++;
    setBucketFeedback(null);
    setNavigation({ ...navigation, tab });
  }

  const visibleLevels = activeTab === 'workOrders' ? levels : levels.slice(0, 1);

  // Back, Escape and ancestor jumps truncate the same trail. A blocking modal owns Escape first
  // and must never also navigate the background investigation.
  useEffect(() => {
    function handleDocumentEscape(e: KeyboardEvent) {
      if (e.key !== 'Escape' || !active || e.defaultPrevented) return;
      // Yield before menu dismissal, or to its consumed event if its listener ran first.
      if (inspectedComponent || inspectedShortage || document.querySelector('[role="dialog"][aria-modal="true"], [role="alertdialog"][aria-modal="true"], [data-analytical-export-menu], [data-workspace-action-menu]')) return;
      if (!selectedParent && bucketFeedback?.identity === identity && bucketFeedback.pending) {
        bucketOwner.current.attempt++;
        setBucketFeedback(null);
        e.preventDefault(); e.stopPropagation(); return;
      }
      if (!selectedParent) return;
      e.preventDefault();
      e.stopPropagation();
      navigateTo(visibleLevels.length - 1);
    }
    document.addEventListener('keydown', handleDocumentEscape, true);
    return () => document.removeEventListener('keydown', handleDocumentEscape, true);
  });

  // Selecting the same parent toggles back to the grid. A new parent opens Part Info with the
  // accepted parent-only Work Orders planning window available on explicit tab activation.
  function handleParentRowSelect(partNumber: string) {
    if (selectedParent === partNumber) {
      clearSelection();
      return;
    }
    bucketOwner.current.attempt++;
    setBucketFeedback(null);
    const trigger = rowRefs.current.get(partNumber) ?? null;
    setNavigation({ identity, levels: [{ kind: 'part', part: partNumber, trigger, scroll: captureNavigationScroll(trigger) }], tab: 'partInfo' });
    setInspectedShortage(null);
  }

  // Enter only a nonempty, successful eligible result. Pending, empty and failed lookups retain
  // the current view/trail; context changes, navigation, and newer attempts supersede this one.
  async function handleBucketSelect(
    partNumber: string,
    kind: BucketSelection['kind'],
    weekLabel: string | null,
    e: React.SyntheticEvent,
  ) {
    e.stopPropagation();
    const snapshotId = dashboard?.snapshot.snapshotId;
    if (!active || !snapshotId || isLoading || isRefreshing || dashboard.dateBasis !== dateBasis) return;
    const trigger = e.currentTarget as HTMLElement;
    const scroll = captureNavigationScroll(trigger);
    const selection = { parentPart: partNumber, kind, weekLabel };
    const attempt = ++bucketOwner.current.attempt;
    setBucketFeedback({ identity, pending: true, error: false, text: 'Loading work orders for this bucket…' });
    const timeout = setTimeout(() => {
      if (!bucketOwner.current.alive || bucketOwner.current.attempt !== attempt) return;
      bucketOwner.current.attempt++;
      setBucketFeedback({ identity, pending: false, error: true, text: 'Work order lookup timed out. Select the bucket to try again.' });
    }, 15000);
    try {
      const result = await fetchPlanningWindow(assignmentId, { snapshotId, parentPart: partNumber, dateBasis, bucketKind: kind, weekLabel: kind === 'weekly' ? weekLabel ?? undefined : undefined });
      if (!bucketOwner.current.alive || bucketOwner.current.attempt !== attempt) return;
      if (result.snapshotId !== snapshotId) throw new Error('The work order response does not match the current snapshot. Refresh the MPS grid.');
      // A nested material dialog is owned by its candidate pane. Never replace its background
      // while it is blocking interaction, even if it opened after this lookup began.
      if (workspaceRef.current?.querySelector('[role="dialog"][aria-modal="true"]')) { setBucketFeedback(null); return; }
      if (result.workOrders.length === 0) {
        setBucketFeedback({ identity, pending: false, error: false, text: 'No work orders for this bucket' });
        return;
      }
      setBucketFeedback(null);
      setInspectedShortage(null);
      setNavigation({ identity, tab: 'workOrders', levels: [
        { kind: 'part', part: partNumber, trigger: rowRefs.current.get(partNumber) ?? null, scroll },
        { kind: 'bucket', selection, orders: result.workOrders, trigger, scroll },
      ] });
    } catch (err) {
      if (!bucketOwner.current.alive || bucketOwner.current.attempt !== attempt) return;
      const error = toWorkOrdersApiError(err);
      setBucketFeedback({ identity, pending: false, error: true, text: error?.type === 'stale' ? 'This schedule context is out of date. Refresh the MPS grid and reselect the bucket.' : error?.detail ?? 'Could not load work orders. Select the bucket to try again.' });
    } finally { clearTimeout(timeout); }
  }

  const allParts = dashboard?.parts ?? [];
  const parts = selectedParent ? allParts.filter((p) => p.parentPart === selectedParent) : allParts;
  const weeklyBuckets = allParts[0]?.buckets.slice(1) ?? [];

  const weekFiscalInfo = weeklyBuckets.map((bucket) =>
    bucket.weekLabel
      ? getFiscalDisplayInfo(fiscalSettings, parseIsoDateOnly(bucket.weekLabel))
      : null,
  );
  const quarterGroups = groupConsecutive(weekFiscalInfo, (info) =>
    info ? `${info.fiscalYear}-Q${info.fiscalQuarter}` : 'n/a',
  );
  const periodGroups = groupConsecutive(weekFiscalInfo, (info) =>
    info ? `${info.fiscalYear}-P${info.fiscalPeriod}` : 'n/a',
  );

  // Fiscal-quarter color family (1-4, cycling blue/green/amber/purple regardless of fiscal year —
  // see MpsWorkspace.css `.mps-grid__band-col--q1..4`). Period bands inherit their parent quarter's
  // family and alternate a reduced tint per period within that quarter, mirroring the prototype's
  // `qColors` / `periodBandsV` construction.
  const quarterBands = withQuarterColor(quarterGroups, weekFiscalInfo);
  const periodBands = withPeriodColor(periodGroups, weekFiscalInfo);
  let exportReason = isLoading ? 'MPS selections are loading. Wait for the loaded view.' : isRefreshing ? 'MPS refresh is in progress. Wait for it to finish.' : '';
  if (!exportReason && dashboard) {
    try { mpsExportRequest(dashboard, assignmentId, dateBasis, horizonWeeks, fiscalSettings, !!error); }
    catch (cause) { exportReason = cause instanceof Error ? cause.message : 'Reload the MPS view before exporting.'; }
  }

  return (
    <div className="mps-workspace" ref={workspaceRef}>
      <header className="mps-workspace__header">
        <div className="mps-workspace__controls">
          <div className="segmented" role="group" aria-label="Date basis">
            {(['dueDate', 'releaseDate'] as const).map((option) => (
              <button
                key={option}
                type="button"
                className={`segmented__btn${dateBasis === option ? ' segmented__btn--active' : ''}`}
                aria-pressed={dateBasis === option}
                onClick={() => setDateBasis(option)}
              >
                {option === 'dueDate' ? 'Due Date' : 'Release Date'}
              </button>
            ))}
          </div>

          <label className="mps-workspace__horizon">
            <span>Horizon (weeks)</span>
            <input
              type="number"
              min={MIN_MPS_HORIZON_WEEKS}
              max={MAX_MPS_HORIZON_WEEKS}
              value={horizonWeeks}
              onChange={(e) => {
                const next = Number(e.target.value);
                if (Number.isInteger(next) && next >= MIN_MPS_HORIZON_WEEKS && next <= MAX_MPS_HORIZON_WEEKS) {
                  setHorizonWeeks(next);
                }
              }}
              aria-label="Horizon in weeks"
            />
          </label>

          <button
            type="button"
            className="mps-workspace__btn mps-workspace__btn--primary"
            onClick={() => void refresh()}
            disabled={isRefreshing}
          >
            {isRefreshing ? 'Refreshing\u2026' : 'Refresh'}
          </button>
        </div>
        {dashboard && <span className="mps-workspace__snapshot">{dashboard.snapshot.site} snapshot loaded {dashboard.snapshot.lastSuccessfulRefreshAtUtc ? new Date(dashboard.snapshot.lastSuccessfulRefreshAtUtc).toLocaleString() : '—'}</span>}
      </header>

      {error && dashboard && (
        <div className="mps-workspace__banner mps-workspace__banner--warning" role="alert">
          {error.type === 'unavailable' ? error.detail : 'The last action could not be completed.'}
          {' Showing last known data.'}
        </div>
      )}

      {isLoading && !dashboard && (
        <div className="mps-workspace__state">Loading MPS data&hellip;</div>
      )}

      {!isLoading && error && !dashboard && (
        <div className="mps-workspace__state mps-workspace__state--error">
          <p>{error.type === 'unavailable' ? error.detail : 'Could not load MPS data.'}</p>
          <button type="button" className="mps-workspace__btn mps-workspace__btn--primary" onClick={() => void reload()}>
            Retry
          </button>
        </div>
      )}

      {!isLoading && dashboard && parts.length === 0 && (
        <div className="mps-workspace__state">
          No parts resolved for this workspace&apos;s current scope.
        </div>
      )}

      {!isLoading && dashboard && parts.length > 0 && (
        <div className={`mps-grid-frame${selectedParent ? ' mps-grid-frame--focused' : ''}`}>
          <div className="mps-grid-caption">
            <div><strong>MPS grid</strong><span className="mps-grid-caption__context">{dashboard.dateBasis === 'dueDate' ? 'Due Date' : 'Release Date'} · {dashboard.horizonWeeks} weeks{selectedParent ? ` · Showing ${parts.length} of ${allParts.length} parts` : ''}</span></div>
            {active && <AnalyticalExcelExport label="MPS" scopeLabel={`Export includes all ${allParts.length} parts`} disabledReason={exportReason}
              contextKey={JSON.stringify([assignmentId, dashboard, dateBasis, horizonWeeks, fiscalSettings, error, businessDate])}
              request={() => mpsExportRequest(dashboard, assignmentId, dateBasis, horizonWeeks, fiscalSettings, !!error)} />}
          </div>
          <p className="mps-grid-guidance">Click Falldown or any of the next 4 weeks to open work orders.</p>
          <div className="mps-grid-scroll">
            <table className="mps-grid" aria-label="MPS grid">
              <thead>
                <tr>
                  <th className="mps-grid__sticky mps-grid__part-col" rowSpan={3}>
                    Parent Part
                  </th>
                  <th className="mps-grid__sticky mps-grid__falldown-col" rowSpan={3}>
                    Falldown
                  </th>
                  {quarterBands.map((group, idx) => (
                    <th
                      key={`q-${idx}`}
                      className={`mps-grid__band-col mps-grid__band-col--q${group.quarterNum}`}
                      colSpan={group.span}
                    >
                      {group.key === 'n/a' ? '' : `Q${group.key.split('-Q')[1]}`}
                    </th>
                  ))}
                </tr>
                <tr>
                  {periodBands.map((group, idx) => (
                    <th
                      key={`p-${idx}`}
                      className={`mps-grid__band-col mps-grid__band-col--q${group.quarterNum}${
                        group.altShade ? ' mps-grid__band-col--alt' : ''
                      }`}
                      colSpan={group.span}
                    >
                      {group.key === 'n/a' ? '' : `P${group.key.split('-P')[1]}`}
                    </th>
                  ))}
                </tr>
                <tr>
                  {weeklyBuckets.map((bucket, idx) => (
                    <th key={bucket.weekLabel ?? `week-${idx}`} className="mps-grid__week-col">
                      {formatWeekLabel(bucket.weekLabel)}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {parts.map((part) => {
                  const falldown = part.buckets[0];
                  const weekly = part.buckets.slice(1);
                  const isSelected = part.parentPart === selectedParent;
                  const isFalldownSelected =
                    selectedBucket?.parentPart === part.parentPart && selectedBucket.kind === 'falldown';
                  return (
                    <tr
                      key={part.parentPart}
                      ref={(el) => {
                        if (el) rowRefs.current.set(part.parentPart, el);
                        else rowRefs.current.delete(part.parentPart);
                      }}
                      className={isSelected ? 'mps-grid__row--selected' : undefined}
                      onClick={() => handleParentRowSelect(part.parentPart)}
                      aria-selected={isSelected}
                      tabIndex={0}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          handleParentRowSelect(part.parentPart);
                        }
                      }}
                    >
                      <td className="mps-grid__sticky mps-grid__part-col">
                        <div className="mps-grid__part-cell">
                          <span className="mps-grid__part-number">{part.parentPart}</span>
                          <span className="mps-grid__part-desc" title={part.description ?? undefined}>
                            {part.description ?? '\u2014'}
                          </span>
                        </div>
                      </td>
                      <td
                        className={`mps-grid__sticky mps-grid__falldown-col${
                          falldown ? ` ${bucketCellClassNames(falldown)}` : ' mps-cell'
                        }${falldown ? ' mps-cell--clickable' : ''}${
                          isFalldownSelected ? ' mps-cell--selected-bucket' : ''
                        }`}
                        title={falldown ? describeBucket(falldown) : undefined}
                        tabIndex={falldown ? 0 : undefined}
                        onClick={
                          falldown
                            ? (e) => handleBucketSelect(part.parentPart, 'falldown', null, e)
                            : undefined
                        }
                        onKeyDown={
                          falldown
                            ? (e) => {
                                if (e.key === 'Enter' || e.key === ' ') {
                                  e.preventDefault();
                                  handleBucketSelect(part.parentPart, 'falldown', null, e);
                                }
                              }
                            : undefined
                        }
                      >
                        {falldown ? formatQuantity(falldown.quantity) : '\u2014'}
                        <span className="visually-hidden">{falldown ? describeBucket(falldown) : ''}</span>
                      </td>
                      {weekly.map((bucket, idx) => {
                        const isEligible = isWeeklyBucketWorkOrderEligible(idx);
                        const isBucketSelected =
                          selectedBucket?.parentPart === part.parentPart &&
                          selectedBucket.kind === 'weekly' &&
                          selectedBucket.weekLabel === bucket.weekLabel;
                        return (
                          <td
                            key={bucket.weekLabel ?? `week-${idx}`}
                            className={`mps-grid__week-col ${bucketCellClassNames(bucket)}${
                              isEligible ? ' mps-cell--clickable' : ''
                            }${isBucketSelected ? ' mps-cell--selected-bucket' : ''}`}
                            title={describeBucket(bucket)}
                            tabIndex={isEligible ? 0 : undefined}
                            onClick={
                              isEligible
                                ? (e) => handleBucketSelect(part.parentPart, 'weekly', bucket.weekLabel, e)
                                : undefined
                            }
                            onKeyDown={
                              isEligible
                                ? (e) => {
                                    if (e.key === 'Enter' || e.key === ' ') {
                                      e.preventDefault();
                                      handleBucketSelect(part.parentPart, 'weekly', bucket.weekLabel, e);
                                    }
                                  }
                                : undefined
                            }
                          >
                            {formatQuantity(bucket.quantity)}
                            <span className="visually-hidden">{describeBucket(bucket)}</span>
                          </td>
                        );
                      })}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {bucketFeedback?.identity === identity && <div className="mps-bucket-feedback" role={bucketFeedback.error ? 'alert' : 'status'}>{bucketFeedback.text}</div>}
      {selectedParent && (
        <div className="mps-detail">
          <nav className="mps-trail" aria-label="MPS drill-down trail">
            <button type="button" className="mps-workspace__btn" onClick={() => navigateTo(visibleLevels.length - 1)}>← Back</button>
            <ol><li><button type="button" onClick={clearSelection}>MPS grid</button></li>
              {visibleLevels.map((level, index) => <li key={index}><span aria-hidden="true">›</span>{index === visibleLevels.length - 1
                ? <span aria-current="page" tabIndex={-1}>{navigationLabel(level)}</span>
                : <button type="button" onClick={() => navigateTo(index + 1)}>{navigationLabel(level)}</button>}</li>)}
            </ol><span className="mps-trail__hint">Esc back one level</span>
          </nav>
          <div className="mps-detail__tabs" role="tablist" aria-label="Part detail">
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === 'partInfo'}
              className={`mps-detail__tab${activeTab === 'partInfo' ? ' mps-detail__tab--active' : ''}`}
              onClick={() => setActiveTab('partInfo')}
            >
              Part Info
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === 'bom'}
              className={`mps-detail__tab${activeTab === 'bom' ? ' mps-detail__tab--active' : ''}`}
              onClick={() => {
                setActiveTab('bom');
                activateBom();
              }}
            >
              BOM
            </button>
            {/* Stage 7R planning-window cards establish the selected Work Order for Stage 9. */}
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === 'workOrders'}
              className={`mps-detail__tab${activeTab === 'workOrders' ? ' mps-detail__tab--active' : ''}`}
              onClick={() => setActiveTab('workOrders')}
            >
              Work Orders
            </button>
          </div>

          <div className="mps-detail__content">
          {activeTab === 'partInfo' && <div className="mps-detail__scroll">
            <PartInfoPanel
              partNumber={selectedParent}
              detail={partDetail}
              isLoading={isPartDetailLoading}
              error={partDetailError}
              onRetry={() => void retryPartDetail()}
              onBack={clearSelection}
            />
          </div>}

          {activeTab === 'bom' && <div className="mps-detail__scroll">
            <BomPanel
              assignmentId={active ? assignmentId : undefined}
              snapshotId={dashboard?.snapshot.snapshotId ?? null}
              parentPart={selectedParent}
              bom={bom}
              isLoading={isBomLoading}
              error={bomError}
              onRetry={() => void retryBom()}
              onSelectComponent={handleSelectComponent}
            />
          </div>}

          {(activeTab === 'workOrders' || levels.length > 1) && <div className="mps-detail__work-orders" hidden={activeTab !== 'workOrders'}>
            <div className="mps-detail__scroll" hidden={firstCandidateIndex >= 0}>
              <WorkOrdersPanel
                parentPart={selectedParent}
                bucketLabel={selectedBucket ? describeBucketSelection(selectedBucket) : 'Planning window'}
                assignmentId={assignmentId}
                snapshotId={dashboard?.snapshot.snapshotId ?? null}
                workOrders={bucketLevel?.kind === 'bucket' ? bucketLevel.orders : planningWindowWorkOrders}
                isLoading={selectedBucket ? false : isPlanningWindowLoading}
                error={selectedBucket ? null : planningWindowError}
                onRetry={() => void retryPlanningWindow()}
                dateBasis={dateBasis}
                selectedWoid={selectedWoid}
                onSelectWoid={handleOpenMaterialLines}
                summary={immediateMaterialSummary}
                summaryError={immediateMaterialSummaryError}
                analysis={immediateMaterialAnalysis}
                isAnalysisLoading={isImmediateMaterialLoading}
                analysisError={immediateMaterialError}
                onRetryAnalysis={() => void retryImmediateMaterial()}
                detail={inspectedShortage?.row ?? null}
                onSelectDetail={handleSelectShortageDetail}
                onCloseDetail={handleCloseShortageDetail}
                onViewWorkOrders={(row, trigger) => enterCandidates(row, trigger, selectedWoid!, 2)}
              />
            </div>
            {levels.map((level, index) => {
              const material = levels[index + 1];
              return level.kind === 'candidates' && <div className="mps-detail__scroll" key={`${identity}:${index}:${level.row.componentPart}`} hidden={levels.slice(index + 1).some((item) => item.kind === 'candidates')}>
              <WorkOrderCandidatePanel assignmentId={assignmentId} snapshotId={dashboard?.snapshot.snapshotId ?? null}
                immediateParentWoid={level.originWoid} componentPart={level.row.componentPart} depth={level.depth} dateBasis={dateBasis}
                originLine={level.row}
                selectedWoid={material?.kind === 'material' ? material.woid : null}
                onSelectWoid={(woid, trigger) => selectWorkOrder(woid, trigger, index + 1)}
                onViewWorkOrders={(row, trigger, woid) => enterCandidates(row, trigger, woid, level.depth + 1)} />
            </div>; })}
          </div>}
          </div>
        </div>
      )}

      {inspectedComponent && (
        <ComponentInfoModal
          assignmentId={assignmentId}
          mpsSnapshotId={dashboard?.snapshot.snapshotId ?? null}
          workspaceSite={dashboard?.snapshot.site ?? null}
          componentPart={inspectedComponent.componentPart}
          detail={componentDetail}
          isLoading={isComponentDetailLoading}
          error={componentDetailError}
          onRetry={() => void retryComponentDetail()}
          onClose={handleCloseComponentInfo}
          onShowMrp={onShowMrp && active && dashboard?.snapshot.site && dashboard.snapshot.snapshotId ? showComponentMrp : undefined}
          approvedVendors={approvedVendors}
          isApprovedVendorsLoading={isApprovedVendorsLoading}
          approvedVendorsError={approvedVendorsError}
          onExpandApprovedVendors={activateApprovedVendors}
          onRetryApprovedVendors={() => void retryApprovedVendors()}
        />
      )}
    </div>
  );
}

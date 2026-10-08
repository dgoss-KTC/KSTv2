import { useCallback, useEffect, useRef, useState } from 'react';
import { fetchInventoryLocations } from '../api/inventoryLocationsApi';
import type { InventoryLocationsResponseDto } from '../api/client';

interface State {
  identity: string;
  report: InventoryLocationsResponseDto | null;
  loading: boolean;
  error: string | null;
}

export function useInventoryLocations(assignmentId: string | null, part: string, snapshotId: string | null, site: string | null) {
  const identity = assignmentId && snapshotId ? JSON.stringify([assignmentId, part, snapshotId, site]) : null;
  const [state, setState] = useState<State | null>(null);
  const active = useRef<{ identity: string; controller: AbortController; token: number } | null>(null);
  const sequence = useRef(0);
  const lastGood = useRef<State | null>(null);

  const load = useCallback(() => {
    if (!identity || !assignmentId || !snapshotId) return;
    active.current?.controller.abort();
    const request = { identity, controller: new AbortController(), token: ++sequence.current };
    active.current = request;
    const retained = lastGood.current?.identity === identity ? lastGood.current.report : null;
    setState({ identity, report: retained, loading: true, error: null });
    void fetchInventoryLocations(assignmentId, part, snapshotId, request.controller.signal).then(report => {
      if (active.current !== request || request.controller.signal.aborted) return;
      if (site !== null && report.site.toUpperCase() !== site.toUpperCase()) throw new Error('Inventory site changed.');
      const next = { identity, report, loading: false, error: null };
      if (!report.isStale) lastGood.current = next;
      setState(next);
    }).catch(() => {
      if (active.current !== request || request.controller.signal.aborted) return;
      // Server alone establishes compatible stale fallback after rechecking workspace/site/snapshot.
      // A transport error cannot establish that compatibility, so it must not reuse local data.
      setState({ identity, report: null, loading: false, error: 'Inventory locations could not be loaded. Please retry.' });
    });
  }, [assignmentId, identity, part, snapshotId, site]);

  useEffect(() => {
    const timer = setTimeout(load, 0);
    return () => {
      clearTimeout(timer);
      active.current?.controller.abort();
      active.current = null;
      lastGood.current = null;
    };
  }, [load]);

  const current = state?.identity === identity ? state : null;
  return { report: current?.report ?? null, loading: identity !== null && (current?.loading ?? true),
    error: current?.error ?? null, refresh: load };
}

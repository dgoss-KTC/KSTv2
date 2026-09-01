import { useCallback, useEffect, useState } from 'react';
import { fetchPlanningWindow, toWorkOrdersApiError, type WorkOrderSummary, type WorkOrdersApiError } from '../api/workOrdersApi';
import type { BucketSelection } from '../mps/mpsPresentation';

interface PlanningWindowWorkOrdersState {
  workOrders: WorkOrderSummary[];
  isLoading: boolean;
  error: WorkOrdersApiError | null;
}

const EMPTY_STATE: PlanningWindowWorkOrdersState = {
  workOrders: [],
  isLoading: false,
  error: null,
};

/**
 * Lazily loads the Stage 7R planning-window Work Order population for a selected parent. When
 * `selection` is null it loads the full parent-level window (Due-Date-based Falldown + Week 0-3
 * under the active basis); when set it loads the bucket-filtered population. Re-fetches when the
 * assignment, snapshot, parent, date basis, or bucket selection changes.
 */
export function usePlanningWindowWorkOrders(
  assignmentId: string,
  snapshotId: string | null,
  parentPart: string | null,
  dateBasis: string,
  selection: BucketSelection | null,
) {
  const [state, setState] = useState<PlanningWindowWorkOrdersState>(EMPTY_STATE);

  const load = useCallback(async () => {
    if (!parentPart || !snapshotId) {
      setState(EMPTY_STATE);
      return;
    }
    setState((prev) => ({ ...prev, isLoading: true, error: null }));
    try {
      const data = await fetchPlanningWindow(assignmentId, {
        snapshotId,
        parentPart,
        dateBasis,
        bucketKind: selection ? selection.kind : undefined,
        weekLabel: selection && selection.kind === 'weekly' ? (selection.weekLabel ?? undefined) : undefined,
      });
      setState({ workOrders: data.workOrders, isLoading: false, error: null });
    } catch (err: unknown) {
      setState({
        workOrders: [],
        isLoading: false,
        error: toWorkOrdersApiError(err) ?? { type: 'error', detail: 'Failed to load work orders.' },
      });
    }
  }, [assignmentId, snapshotId, parentPart, dateBasis, selection]);

  useEffect(() => {
    const id = setTimeout(() => void load(), 0);
    return () => clearTimeout(id);
  }, [load]);

  return { ...state, retry: load };
}

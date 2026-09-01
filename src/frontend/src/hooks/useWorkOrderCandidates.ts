import { useCallback, useEffect, useState } from 'react';
import type { WorkOrderSummaryDto } from '../api/client';
import { fetchWorkOrderCandidates, toWorkOrdersApiError, type WorkOrdersApiError } from '../api/workOrdersApi';

export interface WorkOrderCandidatesState {
  candidates: WorkOrderSummaryDto[] | null;
  isLoading: boolean;
  error: WorkOrdersApiError | null;
  retry: () => void;
}

/**
 * Loads bounded candidate subassembly work orders for one manufactured material component. Unlike
 * `useWorkOrderMaterialLines`, there is no `enabled` flag: the owning `WorkOrderCandidatePanel` is
 * only ever mounted while its branch is expanded, so mounting itself is the "enabled" signal.
 */
export function useWorkOrderCandidates(
  assignmentId: string,
  snapshotId: string | null,
  immediateParentWoid: string,
  componentPart: string,
  targetDepth: number,
  dateBasis: string,
): WorkOrderCandidatesState {
  const [candidates, setCandidates] = useState<WorkOrderSummaryDto[] | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<WorkOrdersApiError | null>(null);

  const load = useCallback(async () => {
    if (snapshotId === null) {
      setCandidates(null);
      setError(null);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    try {
      const result = await fetchWorkOrderCandidates(
        assignmentId,
        snapshotId,
        immediateParentWoid,
        componentPart,
        targetDepth,
        dateBasis,
      );
      setCandidates(result.candidates);
      setError(null);
    } catch (err) {
      setCandidates(null);
      setError(
        toWorkOrdersApiError(err) ?? { type: 'error', detail: 'Could not load candidate work orders. Try again.' },
      );
    } finally {
      setIsLoading(false);
    }
  }, [assignmentId, snapshotId, immediateParentWoid, componentPart, targetDepth, dateBasis]);

  useEffect(() => {
    const id = setTimeout(() => void load(), 0);
    return () => clearTimeout(id);
  }, [load]);

  return { candidates, isLoading, error, retry: load };
}

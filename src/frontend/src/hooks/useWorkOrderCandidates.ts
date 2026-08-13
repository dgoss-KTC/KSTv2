import { useCallback, useEffect, useState } from 'react';
import type { WorkOrderSummaryDto } from '../api/client';
import { fetchWorkOrderCandidates, toWorkOrdersApiError, type WorkOrdersApiError } from '../api/workOrdersApi';

export interface WorkOrderCandidatesState {
  candidates: WorkOrderSummaryDto[] | null;
  isTruncated: boolean;
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
): WorkOrderCandidatesState {
  const [candidates, setCandidates] = useState<WorkOrderSummaryDto[] | null>(null);
  const [isTruncated, setIsTruncated] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<WorkOrdersApiError | null>(null);

  const load = useCallback(async () => {
    if (snapshotId === null) {
      setCandidates(null);
      setIsTruncated(false);
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
      );
      setCandidates(result.candidates);
      setIsTruncated(result.isTruncated);
      setError(null);
    } catch (err) {
      setCandidates(null);
      setIsTruncated(false);
      setError(
        toWorkOrdersApiError(err) ?? { type: 'error', detail: 'Could not load candidate work orders. Try again.' },
      );
    } finally {
      setIsLoading(false);
    }
  }, [assignmentId, snapshotId, immediateParentWoid, componentPart, targetDepth]);

  useEffect(() => {
    const id = setTimeout(() => void load(), 0);
    return () => clearTimeout(id);
  }, [load]);

  return { candidates, isTruncated, isLoading, error, retry: load };
}

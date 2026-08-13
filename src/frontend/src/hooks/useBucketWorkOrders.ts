import { useCallback, useEffect, useState } from 'react';
import type { WorkOrderSummaryDto } from '../api/client';
import { fetchBucketWorkOrders, toWorkOrdersApiError, type WorkOrdersApiError } from '../api/workOrdersApi';
import type { BucketSelection } from '../mps/mpsPresentation';

export interface BucketWorkOrdersState {
  workOrders: WorkOrderSummaryDto[] | null;
  isLoading: boolean;
  error: WorkOrdersApiError | null;
}

/**
 * Loads lazily the eligible (Allocating/Frozen/Released) work orders contributing to the
 * workspace's currently selected schedule bucket. Only re-fetches when the assignment, snapshot,
 * bucket selection, date basis, or horizon change. Passing `selection = null` (no bucket selected)
 * clears state without calling the backend.
 */
export function useBucketWorkOrders(
  assignmentId: string,
  snapshotId: string | null,
  selection: BucketSelection | null,
  dateBasis: string,
  horizonWeeks: number,
) {
  const [workOrders, setWorkOrders] = useState<WorkOrderSummaryDto[] | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<WorkOrdersApiError | null>(null);

  const load = useCallback(async () => {
    if (selection === null || snapshotId === null) {
      setWorkOrders(null);
      setError(null);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    try {
      const result = await fetchBucketWorkOrders(
        assignmentId,
        snapshotId,
        selection.parentPart,
        selection.kind,
        selection.weekLabel,
        dateBasis,
        horizonWeeks,
      );
      setWorkOrders(result.workOrders);
      setError(null);
    } catch (err) {
      setWorkOrders(null);
      setError(toWorkOrdersApiError(err) ?? { type: 'error', detail: 'Could not load work orders. Try again.' });
    } finally {
      setIsLoading(false);
    }
  }, [assignmentId, snapshotId, selection, dateBasis, horizonWeeks]);

  useEffect(() => {
    const id = setTimeout(() => void load(), 0);
    return () => clearTimeout(id);
  }, [load]);

  return { workOrders, isLoading, error, retry: load };
}

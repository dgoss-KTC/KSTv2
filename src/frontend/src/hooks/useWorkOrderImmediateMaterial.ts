import { useCallback } from 'react';
import { fetchWorkOrderImmediateMaterial } from '../api/workOrdersApi';
import { useWorkOrderRequest } from './useWorkOrderRequest';

/** The backend remains the sole owner of Stage 9 calculations and cache semantics. */
export function useWorkOrderImmediateMaterial(assignmentId: string, snapshotId: string | null, woid: string | null, dateBasis: string, enabled: boolean) {
  const request = useCallback(async () => {
    const result = await fetchWorkOrderImmediateMaterial(assignmentId, snapshotId!, woid!, dateBasis);
    if (result.snapshotId !== snapshotId || result.workOrder.woid !== woid) throw new Error('The material response does not match the selected work order. Refresh the MPS grid.');
    return result;
  }, [assignmentId, snapshotId, woid, dateBasis]);
  const result = useWorkOrderRequest(JSON.stringify([assignmentId, snapshotId, woid, dateBasis, enabled]), enabled && !!snapshotId && !!woid, request);
  return { analysis: result.data, isLoading: result.isLoading, error: result.error, retry: result.retry };
}

import { useCallback } from 'react';
import { fetchWorkOrderImmediateMaterialSummary } from '../api/workOrdersApi';
import { useWorkOrderRequest } from './useWorkOrderRequest';

export function useWorkOrderImmediateMaterialSummary(assignmentId: string, snapshotId: string | null, dateBasis: string) {
  const request = useCallback(async () => {
    const result = await fetchWorkOrderImmediateMaterialSummary(assignmentId, snapshotId!, null, dateBasis);
    if (result.snapshotId !== snapshotId) throw new Error('The shortage indicators do not match the current snapshot.');
    return result;
  }, [assignmentId, snapshotId, dateBasis]);
  const result = useWorkOrderRequest(JSON.stringify([assignmentId, snapshotId, dateBasis]), !!snapshotId, request);
  return { summary: result.data, isLoading: result.isLoading, error: result.error, retry: result.retry };
}

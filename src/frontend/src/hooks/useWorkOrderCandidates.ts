import { useCallback } from 'react';
import { fetchWorkOrderCandidates } from '../api/workOrdersApi';
import { useWorkOrderRequest } from './useWorkOrderRequest';

export function useWorkOrderCandidates(assignmentId: string, snapshotId: string | null, immediateParentWoid: string, componentPart: string, targetDepth: number, dateBasis: string) {
  const request = useCallback(async () => {
    const result = await fetchWorkOrderCandidates(assignmentId, snapshotId!, immediateParentWoid, componentPart, targetDepth, dateBasis);
    if (result.snapshotId !== snapshotId) throw new Error('The candidate response does not match the current snapshot. Refresh the MPS grid.');
    return result.candidates;
  }, [assignmentId, snapshotId, immediateParentWoid, componentPart, targetDepth, dateBasis]);
  const result = useWorkOrderRequest(JSON.stringify([assignmentId, snapshotId, immediateParentWoid, componentPart, targetDepth, dateBasis]), !!snapshotId && targetDepth <= 3, request);
  return { candidates: result.data, isLoading: result.isLoading, error: result.error, retry: result.retry };
}

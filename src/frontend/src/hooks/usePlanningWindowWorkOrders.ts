import { useCallback } from 'react';
import { fetchPlanningWindow } from '../api/workOrdersApi';
import type { BucketSelection } from '../mps/mpsPresentation';
import { useWorkOrderRequest } from './useWorkOrderRequest';

/** Stage 7R population, optionally narrowed by the server to one eligible bucket. */
export function usePlanningWindowWorkOrders(assignmentId: string, snapshotId: string | null, parentPart: string | null, dateBasis: string, selection: BucketSelection | null) {
  const kind = selection?.kind;
  const weekLabel = selection?.weekLabel ?? undefined;
  const request = useCallback(async () => {
    const result = await fetchPlanningWindow(assignmentId, { snapshotId: snapshotId!, parentPart: parentPart!, dateBasis, bucketKind: kind, weekLabel });
    if (result.snapshotId !== snapshotId) throw new Error('The work order response does not match the current snapshot. Refresh the MPS grid.');
    return result.workOrders;
  }, [assignmentId, snapshotId, parentPart, dateBasis, kind, weekLabel]);
  const result = useWorkOrderRequest(JSON.stringify([assignmentId, snapshotId, parentPart, dateBasis, kind, weekLabel]), !!parentPart && !!snapshotId, request);
  return { workOrders: result.data, isLoading: result.isLoading, error: result.error, retry: result.retry };
}

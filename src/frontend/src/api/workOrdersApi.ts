import { ApiClient, ApiError } from './client';
import type { components } from '../generated/api';
import { resolveBackendBaseUrl } from './tauri-bridge';

/** Panel-presentable Work Order API failure. 'stale' means the MPS snapshot changed (409); the
 * panel prompts a refresh. 'error' is any other failure. */
export interface WorkOrdersApiError {
  type: 'stale' | 'error';
  detail: string;
}

/** Maps a fetch/network failure to a {@link WorkOrdersApiError}. A 409 (snapshot changed) becomes
 * 'stale'; anything else is a generic 'error'. Returns null when the error carries no usable detail. */
export function toWorkOrdersApiError(err: unknown): WorkOrdersApiError | null {
  if (err instanceof ApiError) {
    if (err.status === 409) {
      return { type: 'stale', detail: 'This schedule context is out of date.' };
    }
    return { type: 'error', detail: err.message };
  }
  if (err instanceof Error) {
    return { type: 'error', detail: err.message };
  }
  return null;
}

export type WorkOrderSummary = components['schemas']['WorkOrderSummaryDto'];
export type WorkOrderMaterialLine = components['schemas']['WorkOrderMaterialLineDto'];
export type KittingSummary = components['schemas']['KittingSummaryDto'];
export type WorkOrderPlanningWindowResponse = components['schemas']['WorkOrderPlanningWindowResponseDto'];
export type WorkOrderMaterialResponse = components['schemas']['WorkOrderMaterialResponseDto'];
export type WorkOrderCandidateResponse = components['schemas']['WorkOrderCandidateResponseDto'];

export interface PlanningWindowParams {
  snapshotId: string;
  parentPart: string;
  dateBasis: string;
  /** 'falldown' | 'weekly'; omitted for the full parent-level planning window. */
  bucketKind?: 'falldown' | 'weekly';
  /** Required when bucketKind is 'weekly' (the bucket's Monday week label). */
  weekLabel?: string;
}

/**
 * Loads the parent-scoped four-week Work Order planning window (Stage 7R): Due-Date-based Falldown
 * plus Week 0-3 under the active Due/Release weekly basis, optionally narrowed to one bucket.
 * Serves both the parent-level population and the bucket-filtered population from one endpoint.
 */
export async function fetchPlanningWindow(
  assignmentId: string,
  params: PlanningWindowParams,
): Promise<WorkOrderPlanningWindowResponse> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  return client.getPlanningWindowWorkOrders(
    assignmentId,
    params.snapshotId,
    params.parentPart,
    params.dateBasis,
    params.bucketKind,
    params.weekLabel,
  );
}

export async function fetchWorkOrderMaterialLines(
  assignmentId: string,
  snapshotId: string,
  woid: string,
): Promise<WorkOrderMaterialResponse> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  return client.getWorkOrderMaterialLines(assignmentId, snapshotId, woid);
}

export async function fetchWorkOrderCandidates(
  assignmentId: string,
  snapshotId: string,
  immediateParentWoid: string,
  componentPart: string,
  targetDepth: number,
  dateBasis: string,
): Promise<WorkOrderCandidateResponse> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  return client.getWorkOrderCandidates(assignmentId, snapshotId, immediateParentWoid, componentPart, targetDepth, dateBasis);
}

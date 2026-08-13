import {
  ApiClient,
  ApiError,
  type WorkOrderBucketResponseDto,
  type WorkOrderCandidateResponseDto,
  type WorkOrderMaterialResponseDto,
} from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

/**
 * The Work Orders tab's error shape collapses the API's HTTP semantics into two cases: `stale`
 * (the workspace's MPS snapshot moved on since this bucket was selected — 409 Conflict, whether
 * from MPS-not-loaded or a changed snapshot id) and a generic retryable `error` for everything
 * else (validation, not-found, QAD outage). A bucket can only be selected from a cell already
 * present in a loaded MPS grid, so `stale` is the one edge case genuinely reachable in normal use.
 */
export type WorkOrdersApiError = { type: 'stale' } | { type: 'error'; detail: string };

const DEFAULT_UNAVAILABLE_DETAIL = 'Database currently unavailable. Please try again in a few minutes.';

function extractDetail(body: string, fallback: string): string {
  try {
    const parsed = JSON.parse(body) as { detail?: string };
    return parsed.detail ?? fallback;
  } catch {
    return fallback;
  }
}

export function toWorkOrdersApiError(err: unknown): WorkOrdersApiError | null {
  if (!(err instanceof ApiError)) return null;

  if (err.status === 409) return { type: 'stale' };

  if (err.status === 404) {
    return { type: 'error', detail: extractDetail(err.message, 'The selected part or bucket could not be found.') };
  }

  if (err.status === 503) {
    return { type: 'error', detail: extractDetail(err.message, DEFAULT_UNAVAILABLE_DETAIL) };
  }

  if (err.status === 400) {
    return { type: 'error', detail: extractDetail(err.message, 'The work order request was invalid.') };
  }

  return null;
}

function isWorkOrderBucketResponseDto(value: unknown): value is WorkOrderBucketResponseDto {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<WorkOrderBucketResponseDto>;
  return typeof candidate.snapshotId === 'string' && Array.isArray(candidate.workOrders);
}

export async function fetchBucketWorkOrders(
  assignmentId: string,
  snapshotId: string,
  parentPart: string,
  bucketKind: 'falldown' | 'weekly',
  weekLabel: string | null,
  dateBasis: string,
  horizonWeeks: number,
): Promise<WorkOrderBucketResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  const result = await client.getBucketWorkOrders(
    assignmentId,
    snapshotId,
    parentPart,
    bucketKind,
    weekLabel,
    dateBasis,
    horizonWeeks,
  );
  if (!isWorkOrderBucketResponseDto(result)) {
    throw new Error('Received an unexpected work orders response shape.');
  }
  return result;
}

function isWorkOrderMaterialResponseDto(value: unknown): value is WorkOrderMaterialResponseDto {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<WorkOrderMaterialResponseDto>;
  return (
    typeof candidate.snapshotId === 'string' &&
    typeof candidate.woid === 'string' &&
    Array.isArray(candidate.lines) &&
    typeof candidate.kitting === 'object' &&
    candidate.kitting !== null
  );
}

export async function fetchWorkOrderMaterialLines(
  assignmentId: string,
  snapshotId: string,
  woid: string,
): Promise<WorkOrderMaterialResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  const result = await client.getWorkOrderMaterialLines(assignmentId, snapshotId, woid);
  if (!isWorkOrderMaterialResponseDto(result)) {
    throw new Error('Received an unexpected work order material response shape.');
  }
  return result;
}

function isWorkOrderCandidateResponseDto(value: unknown): value is WorkOrderCandidateResponseDto {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<WorkOrderCandidateResponseDto>;
  return (
    typeof candidate.snapshotId === 'string' &&
    Array.isArray(candidate.candidates) &&
    typeof candidate.isTruncated === 'boolean'
  );
}

export async function fetchWorkOrderCandidates(
  assignmentId: string,
  snapshotId: string,
  immediateParentWoid: string,
  componentPart: string,
  targetDepth: number,
): Promise<WorkOrderCandidateResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  const result = await client.getWorkOrderCandidates(
    assignmentId,
    snapshotId,
    immediateParentWoid,
    componentPart,
    targetDepth,
  );
  if (!isWorkOrderCandidateResponseDto(result)) {
    throw new Error('Received an unexpected work order candidates response shape.');
  }
  return result;
}

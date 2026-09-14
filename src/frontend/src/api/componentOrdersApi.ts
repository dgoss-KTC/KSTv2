import { ApiClient, ApiError } from './client';
import type { components } from '../generated/api';
import { resolveBackendBaseUrl } from './tauri-bridge';

export type ComponentOrderLine = components['schemas']['ComponentOrderLineDto'];
export type ComponentOrderGroup = components['schemas']['ComponentOrderGroupDto'];
export type ComponentOrdersResponse = components['schemas']['ComponentOrdersResponseDto'];

/** Panel-presentable Component Orders API failure. 'mps-not-loaded' and 'stale' are the two 409
 * variants (MPS not loaded / snapshot changed) — both prompt a refresh of the MPS context;
 * 'unavailable' is 503 (QAD read failed); 'error' is anything else. */
export interface ComponentOrdersApiError {
  type: 'mps-not-loaded' | 'stale' | 'unavailable' | 'error';
  detail: string;
}

interface ProblemDetails {
  title?: string;
  detail?: string;
}

function parseProblem(body: string): ProblemDetails | null {
  try {
    return JSON.parse(body) as ProblemDetails;
  } catch {
    return null;
  }
}

/** Maps a fetch/network failure to a {@link ComponentOrdersApiError}. The two 409 variants are
 * distinguished by the backend's stable Problem Details title. */
export function toComponentOrdersApiError(err: unknown): ComponentOrdersApiError | null {
  if (err instanceof ApiError) {
    const problem = parseProblem(err.message);
    if (err.status === 409) {
      if (problem?.title === 'MPS data not loaded') {
        return { type: 'mps-not-loaded', detail: problem.detail ?? "This workspace's MPS data has not been loaded yet." };
      }
      return { type: 'stale', detail: problem?.detail ?? 'This schedule context is out of date.' };
    }
    if (err.status === 503) {
      return {
        type: 'unavailable',
        detail: problem?.detail ?? 'Database currently unavailable. Please try again in a few minutes.',
      };
    }
    return { type: 'error', detail: err.message };
  }
  if (err instanceof Error) {
    return { type: 'error', detail: err.message };
  }
  return null;
}

/** Loads the active workspace's component-grouped conventional open PO lines for one MPS snapshot. */
export async function fetchComponentOrders(assignmentId: string, snapshotId: string): Promise<ComponentOrdersResponse> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  return client.getComponentOrders(assignmentId, snapshotId);
}

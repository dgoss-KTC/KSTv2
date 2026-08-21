import { ApiClient, ApiError, type BomResponseDto } from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

/**
 * The BOM tab's UI-facing error shape is a single retryable `error` case: the accepted 8D.3
 * endpoint's 404 (workspace-not-found / out-of-scope), 409 (MPS-not-loaded), and 400
 * (blank parent) are edge cases the UI cannot normally reach — a parent can only be selected
 * from a row already present in a loaded MPS grid — so, per the accepted Stage 6/8D.4 contract,
 * they all collapse into the same retryable error presentation as a QAD outage (503).
 * A valid in-scope parent with no BOM is a successful 200 with an empty `lines` array, never an
 * error.
 */
export type BomApiError = { type: 'error'; detail: string };

const DEFAULT_UNAVAILABLE_DETAIL =
  'Database currently unavailable. Please try again in a few minutes.';

function extractDetail(body: string, fallback: string): string {
  try {
    const parsed = JSON.parse(body) as { detail?: string };
    return parsed.detail ?? fallback;
  } catch {
    return fallback;
  }
}

export function toBomApiError(err: unknown): BomApiError | null {
  if (!(err instanceof ApiError)) return null;

  if (err.status === 404) {
    return { type: 'error', detail: extractDetail(err.message, 'The requested part could not be found.') };
  }

  if (err.status === 409) {
    return {
      type: 'error',
      detail: extractDetail(err.message, 'This workspace\u2019s MPS data has not been loaded yet.'),
    };
  }

  if (err.status === 503) {
    return { type: 'error', detail: extractDetail(err.message, DEFAULT_UNAVAILABLE_DETAIL) };
  }

  if (err.status === 400) {
    return { type: 'error', detail: extractDetail(err.message, 'The BOM request was invalid.') };
  }

  return null;
}

function isBomResponseDto(value: unknown): value is BomResponseDto {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<BomResponseDto>;
  return (
    typeof candidate.site === 'string' &&
    typeof candidate.parentPart === 'string' &&
    Array.isArray(candidate.lines) &&
    typeof candidate.isStale === 'boolean'
  );
}

export async function fetchBom(assignmentId: string, parentPart: string): Promise<BomResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  const result = await client.getBom(assignmentId, parentPart);
  if (!isBomResponseDto(result)) {
    throw new Error('Received an unexpected BOM response shape.');
  }
  return result;
}

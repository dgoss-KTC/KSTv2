import { ApiClient, ApiError, type PartDetailResponseDto } from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

/**
 * PartDetail's UI-facing error shape is intentionally coarser than the API's HTTP semantics: the
 * accepted Stage 6 contract's Part Info state machine only distinguishes "missing part" from a
 * generic "error" (workspace-not-found, MPS-not-loaded, and out-of-scope are all edge cases the UI
 * cannot normally reach — a parent can only be selected from a row already present in a loaded MPS
 * grid — so they collapse into the same retryable error presentation as a QAD outage).
 */
export type PartDetailApiError = { type: 'missing-part' } | { type: 'error'; detail: string };

const DEFAULT_UNAVAILABLE_DETAIL =
  'Database currently unavailable. Please try again in a few minutes.';

function extractTitle(body: string): string | null {
  try {
    const parsed = JSON.parse(body) as { title?: string; detail?: string };
    return parsed.title ?? null;
  } catch {
    return null;
  }
}

function extractDetail(body: string, fallback: string): string {
  try {
    const parsed = JSON.parse(body) as { detail?: string };
    return parsed.detail ?? fallback;
  } catch {
    return fallback;
  }
}

export function toPartDetailApiError(err: unknown): PartDetailApiError | null {
  if (!(err instanceof ApiError)) return null;

  if (err.status === 404) {
    const title = extractTitle(err.message);
    if (title === 'Part not found') return { type: 'missing-part' };
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
    return { type: 'error', detail: extractDetail(err.message, 'The part information request was invalid.') };
  }

  return null;
}

function isPartDetailResponseDto(value: unknown): value is PartDetailResponseDto {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<PartDetailResponseDto>;
  return (
    typeof candidate.partNumber === 'string' &&
    typeof candidate.site === 'string' &&
    Array.isArray(candidate.priceBreaks) &&
    typeof candidate.isStale === 'boolean'
  );
}

export async function fetchPartDetail(
  assignmentId: string,
  partNumber: string,
): Promise<PartDetailResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  const result = await client.getPartDetail(assignmentId, partNumber);
  if (!isPartDetailResponseDto(result)) {
    throw new Error('Received an unexpected part information response shape.');
  }
  return result;
}

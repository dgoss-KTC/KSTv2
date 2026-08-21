import { ApiClient, ApiError, type ComponentDetailResponseDto } from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

/**
 * Component Info's UI-facing error shape is the same single retryable `error` case used by BOM
 * and Part Info: the accepted Stage 8D.5 endpoint's 404 (workspace-not-found / out-of-scope),
 * 409 (MPS-not-loaded), and 400 (blank component part) all collapse into the same retryable
 * presentation as a QAD outage (503), since the UI can only reach this endpoint for a component
 * part already present in a loaded BOM.
 */
export type ComponentDetailApiError = { type: 'error'; detail: string };

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

export function toComponentDetailApiError(err: unknown): ComponentDetailApiError | null {
  if (!(err instanceof ApiError)) return null;

  if (err.status === 404) {
    return { type: 'error', detail: extractDetail(err.message, 'The requested component could not be found.') };
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
    return { type: 'error', detail: extractDetail(err.message, 'The component information request was invalid.') };
  }

  return null;
}

function isComponentDetailResponseDto(value: unknown): value is ComponentDetailResponseDto {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<ComponentDetailResponseDto>;
  return (
    typeof candidate.site === 'string' &&
    typeof candidate.componentPart === 'string' &&
    typeof candidate.isStale === 'boolean'
  );
}

export async function fetchComponentDetail(
  assignmentId: string,
  componentPart: string,
): Promise<ComponentDetailResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  const result = await client.getComponentDetail(assignmentId, componentPart);
  if (!isComponentDetailResponseDto(result)) {
    throw new Error('Received an unexpected component information response shape.');
  }
  return result;
}

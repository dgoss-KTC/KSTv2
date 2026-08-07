import { ApiClient, ApiError, type MpsDashboardResponseDto } from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

export type MpsDateBasis = 'dueDate' | 'releaseDate';

export type MpsApiError =
  | { type: 'not-found' }
  | { type: 'unavailable'; detail: string }
  | { type: 'validation' };

/**
 * Defensive shape guard: rejects any response that doesn't structurally match
 * MpsDashboardResponseDto (e.g. an unrelated JSON body from a misrouted/mocked
 * fetch, or a backend contract drift) instead of letting the UI crash on
 * unexpected `undefined` property access.
 */
function isMpsDashboardResponseDto(value: unknown): value is MpsDashboardResponseDto {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<MpsDashboardResponseDto>;
  return (
    typeof candidate.snapshot === 'object' &&
    candidate.snapshot !== null &&
    typeof candidate.snapshot.status === 'string' &&
    typeof candidate.dateBasis === 'string' &&
    typeof candidate.horizonWeeks === 'number' &&
    Array.isArray(candidate.parts)
  );
}

export function toMpsApiError(err: unknown): MpsApiError | null {
  if (!(err instanceof ApiError)) return null;
  if (err.status === 404) return { type: 'not-found' };
  if (err.status === 400) return { type: 'validation' };
  if (err.status === 503) {
    let detail = 'Database currently unavailable. Please try again in a few minutes.';
    try {
      const parsed = JSON.parse(err.message) as { detail?: string };
      if (parsed.detail) detail = parsed.detail;
    } catch {
      /* not parseable */
    }
    return { type: 'unavailable', detail };
  }
  return null;
}

export async function fetchMpsDashboard(
  assignmentId: string,
  dateBasis: MpsDateBasis,
  horizonWeeks: number,
): Promise<MpsDashboardResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  const result = await client.getMpsDashboard(assignmentId, dateBasis, horizonWeeks);
  if (!isMpsDashboardResponseDto(result)) {
    throw new Error('Received an unexpected MPS dashboard response shape.');
  }
  return result;
}

export async function refreshMpsDashboard(
  assignmentId: string,
  dateBasis: MpsDateBasis,
  horizonWeeks: number,
): Promise<MpsDashboardResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  const result = await client.refreshMpsDashboard(assignmentId, dateBasis, horizonWeeks);
  if (!isMpsDashboardResponseDto(result)) {
    throw new Error('Received an unexpected MPS dashboard response shape.');
  }
  return result;
}

import { ApiClient, ApiError, type ApprovedVendorDto } from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

/**
 * Approved Vendors' UI-facing error shape is the same single retryable `error` case used by
 * Component Detail/BOM/Part Info. The accepted Stage 8D.7 endpoint's 404 (unknown workspace) and
 * 400 (blank component part) collapse into the same retryable presentation as a QAD outage (503),
 * since the UI can only reach this endpoint for a component already established by Component
 * Detail.
 */
export type ApprovedVendorsApiError = { type: 'error'; detail: string };

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

export function toApprovedVendorsApiError(err: unknown): ApprovedVendorsApiError | null {
  if (!(err instanceof ApiError)) return null;

  if (err.status === 404) {
    return { type: 'error', detail: extractDetail(err.message, 'The requested component could not be found.') };
  }

  if (err.status === 503) {
    return { type: 'error', detail: extractDetail(err.message, DEFAULT_UNAVAILABLE_DETAIL) };
  }

  if (err.status === 400) {
    return { type: 'error', detail: extractDetail(err.message, 'The approved vendors request was invalid.') };
  }

  return null;
}

function isApprovedVendorDtoArray(value: unknown): value is ApprovedVendorDto[] {
  if (!Array.isArray(value)) return false;
  return value.every((row) => typeof row === 'object' && row !== null && typeof (row as Partial<ApprovedVendorDto>).supplier === 'string');
}

export async function fetchApprovedVendors(
  assignmentId: string,
  componentPart: string,
): Promise<ApprovedVendorDto[]> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  const result = await client.getApprovedVendors(assignmentId, componentPart);
  if (!isApprovedVendorDtoArray(result)) {
    throw new Error('Received an unexpected approved vendors response shape.');
  }
  return result;
}

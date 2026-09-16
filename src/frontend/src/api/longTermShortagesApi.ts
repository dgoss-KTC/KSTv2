import { ApiClient, ApiError } from './client';
import type { components } from '../generated/api';
import { resolveBackendBaseUrl } from './tauri-bridge';

export type LongTermShortageWeek = components['schemas']['LongTermShortageWeekDto'];
export type LongTermPurchaseOrder = components['schemas']['LongTermPurchaseOrderDto'];
export type LongTermShortageRow = components['schemas']['LongTermShortageRowDto'];
export type LongTermShortagesResponse = components['schemas']['LongTermShortagesResponseDto'];

export interface LongTermShortagePopulationOptions {
  includeManufacturedParts: boolean;
  includePhantoms: boolean;
}

export const DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS: LongTermShortagePopulationOptions = {
  includeManufacturedParts: false,
  includePhantoms: false,
};

export interface LongTermShortagesApiError {
  type: 'mps-not-loaded' | 'stale' | 'unavailable' | 'error';
  detail: string;
}

function parseProblem(body: string): { detail?: string } | null {
  try {
    return JSON.parse(body) as { detail?: string };
  } catch {
    return null;
  }
}

export function toLongTermShortagesApiError(error: unknown): LongTermShortagesApiError {
  if (error instanceof ApiError) {
    const detail = parseProblem(error.message)?.detail ?? error.message;
    if (error.status === 409) return { type: 'stale', detail };
    if (error.status === 503) return { type: 'unavailable', detail };
    return { type: 'error', detail };
  }
  return { type: 'error', detail: error instanceof Error ? error.message : String(error) };
}

export async function fetchLongTermShortages(
  assignmentId: string,
  snapshotId: string,
  options: LongTermShortagePopulationOptions,
): Promise<LongTermShortagesResponse> {
  return new ApiClient(await resolveBackendBaseUrl()).getLongTermShortages(
    assignmentId,
    snapshotId,
    options.includeManufacturedParts,
    options.includePhantoms,
  );
}

export async function exportLongTermShortages(
  assignmentId: string,
  snapshotId: string,
  componentParts: string[],
  options: LongTermShortagePopulationOptions,
): Promise<{ blob: Blob; fileName: string | null }> {
  return new ApiClient(await resolveBackendBaseUrl()).exportLongTermShortages(assignmentId, {
    snapshotId,
    componentParts,
    includeManufacturedParts: options.includeManufacturedParts,
    includePhantoms: options.includePhantoms,
  });
}

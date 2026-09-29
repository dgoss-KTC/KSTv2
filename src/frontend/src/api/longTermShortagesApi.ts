import { ApiClient, ApiError } from './client';
import type { components } from '../generated/api';
import { resolveBackendBaseUrl } from './tauri-bridge';

export type LongTermShortageBucket = components['schemas']['LongTermShortageBucketDto'];
export type LongTermMrpFact = components['schemas']['LongTermMrpFactDto'];
export type LongTermShortageRow = components['schemas']['LongTermShortageRowDto'];
export type LongTermShortagesResponse = components['schemas']['LongTermShortagesResponseDto'];
export type LongTermShortagePurchasing = components['schemas']['LongTermShortagePurchasingDto'];
export type LongTermShortagesScreen = components['schemas']['LongTermShortagesScreenDto'];
export type LongTermShortageScreenComponent = components['schemas']['LongTermShortageScreenComponentDto'];
export type LongTermShortageScreenMode = components['schemas']['LongTermShortageScreenModeDto'];
export type LongTermShortageProjectionDetail = components['schemas']['LongTermShortageProjectionDetailDto'];
export type LongTermShortageScreenRow = LongTermShortageScreenComponent & LongTermShortageScreenMode;

export interface LongTermShortagePopulationOptions {
  includeManufacturedParts: boolean;
  includePhantoms: boolean;
  includeUnconfirmed: boolean;
  horizonWeeks: 13 | 26 | 52 | 72;
  showAll: boolean;
}

export const DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS: LongTermShortagePopulationOptions = {
  includeManufacturedParts: false,
  includePhantoms: false,
  includeUnconfirmed: false,
  horizonWeeks: 26,
  showAll: false,
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
): Promise<LongTermShortagesScreen> {
  return new ApiClient(await resolveBackendBaseUrl()).getLongTermShortagesScreen(
    assignmentId,
    snapshotId,
    options.includeManufacturedParts,
    options.includePhantoms,
    options.horizonWeeks,
  );
}

export async function fetchLongTermShortageProjectionDetail(assignmentId: string, snapshotId: string,
  componentPart: string, options: LongTermShortagePopulationOptions): Promise<LongTermShortageProjectionDetail> {
  return new ApiClient(await resolveBackendBaseUrl()).getLongTermShortageProjectionDetail(assignmentId,
    snapshotId, componentPart, options.includeManufacturedParts, options.includePhantoms, options.horizonWeeks);
}

/** Selected-component open PO and buyer-comment detail; never reprojects the shortage matrix. */
export async function fetchLongTermShortagePurchasing(
  assignmentId: string, snapshotId: string, componentPart: string,
  options: LongTermShortagePopulationOptions,
): Promise<LongTermShortagePurchasing> {
  return new ApiClient(await resolveBackendBaseUrl()).getLongTermShortagePurchasing(assignmentId, snapshotId,
    componentPart, options.includeManufacturedParts, options.includePhantoms, options.horizonWeeks);
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
    includeUnconfirmed: options.includeUnconfirmed,
    horizonWeeks: options.horizonWeeks,
    showAll: options.showAll,
  });
}

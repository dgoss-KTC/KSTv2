import {
  ApiClient,
  ApiError,
  type PlanningWorkbookDraftResponseDto,
  type PlanningWorkbookResponseDto,
  type SavePlanningWorkbookDraftRequestDto,
} from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

/** Typed Planning Workbook failures, mapped from the accepted 14.3 status codes. */
export type PlanningWorkbookError =
  | { type: 'validation' }
  | { type: 'horizon-range'; maximum: number }
  | { type: 'not-found' }
  | { type: 'mps-not-loaded' }
  | { type: 'snapshot-changed' }
  | { type: 'unavailable' }
  | { type: 'mismatch' }
  | { type: 'error' };

export function toPlanningWorkbookError(err: unknown): PlanningWorkbookError {
  if (!(err instanceof ApiError)) return { type: 'error' };
  if (err.status === 400) {
    try {
      const maximum = (JSON.parse(err.message) as { maximumHorizonWeeks?: unknown }).maximumHorizonWeeks;
      if (typeof maximum === 'number' && Number.isInteger(maximum)) return { type: 'horizon-range', maximum };
    } catch { /* not Problem Details JSON */ }
    return { type: 'validation' };
  }
  if (err.status === 404) return { type: 'not-found' };
  if (err.status === 503) return { type: 'unavailable' };
  if (err.status === 409) {
    try {
      const title = (JSON.parse(err.message) as { title?: string }).title ?? '';
      if (/not loaded/i.test(title)) return { type: 'mps-not-loaded' };
    } catch { /* not Problem Details JSON */ }
    return { type: 'snapshot-changed' };
  }
  return { type: 'error' };
}

function isWorkbook(value: unknown): value is PlanningWorkbookResponseDto {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<PlanningWorkbookResponseDto>;
  return typeof candidate.mpsSnapshotId === 'string' && typeof candidate.today === 'string'
    && Array.isArray(candidate.weekLabels) && Array.isArray(candidate.parts);
}

async function checked(result: Promise<unknown>): Promise<PlanningWorkbookResponseDto> {
  const value = await result;
  if (!isWorkbook(value)) throw new Error('Received an unexpected Planning Workbook response shape.');
  return value;
}

export async function fetchPlanningWorkbook(assignmentId: string, mpsSnapshotId: string, horizonWeeks: number) {
  return checked(new ApiClient(await resolveBackendBaseUrl()).getPlanningWorkbook(assignmentId, mpsSnapshotId, horizonWeeks));
}

/** Refreshes Open Orders (shared Stage 13 report), inventory, prices and forecast. Never refreshes MPS. */
export async function refreshPlanningWorkbook(assignmentId: string, mpsSnapshotId: string, horizonWeeks: number) {
  return checked(new ApiClient(await resolveBackendBaseUrl()).refreshPlanningWorkbook(assignmentId, mpsSnapshotId, horizonWeeks));
}

// --- Stage 14.5 saved drafts (local files only; never applied to QAD) -------------------------

export type PlanningDraftError =
  | { type: 'corrupt' }
  | { type: 'invalid'; code: string | null }
  | { type: 'not-found' }
  | { type: 'mps-not-loaded' }
  | { type: 'snapshot-changed' }
  | { type: 'unavailable' }
  | { type: 'error' };

export function toPlanningDraftError(err: unknown): PlanningDraftError {
  if (!(err instanceof ApiError)) return { type: 'error' };
  let body: { code?: unknown; title?: unknown } = {};
  try { body = JSON.parse(err.message) as typeof body; } catch { /* not Problem Details JSON */ }
  const code = typeof body.code === 'string' ? body.code : null;
  if (err.status === 400) return { type: 'invalid', code };
  if (err.status === 404) return { type: 'not-found' };
  if (err.status === 409) {
    if (code === 'draft-corrupt') return { type: 'corrupt' };
    return /not loaded/i.test(typeof body.title === 'string' ? body.title : '') ? { type: 'mps-not-loaded' } : { type: 'snapshot-changed' };
  }
  if (err.status === 503) return { type: 'unavailable' };
  return { type: 'error' };
}

export async function fetchPlanningDraftPresence(assignmentId: string): Promise<boolean> {
  const value = await new ApiClient(await resolveBackendBaseUrl()).getPlanningWorkbookDraftPresence(assignmentId);
  if (typeof value?.exists !== 'boolean') throw new Error('Received an unexpected draft presence response.');
  return value.exists;
}

function checkedDraft(value: unknown): PlanningWorkbookDraftResponseDto {
  const candidate = value as Partial<PlanningWorkbookDraftResponseDto> | null;
  if (!candidate || typeof candidate.exists !== 'boolean' || typeof candidate.isCorrupt !== 'boolean' || !Array.isArray(candidate.rows)) {
    throw new Error('Received an unexpected Planning draft response shape.');
  }
  return candidate as PlanningWorkbookDraftResponseDto;
}

/** Reads the saved draft and its comparison against the workspace's currently loaded MPS snapshot. */
export async function loadPlanningDraft(assignmentId: string, mpsSnapshotId: string) {
  return checkedDraft(await new ApiClient(await resolveBackendBaseUrl()).loadPlanningWorkbookDraft(assignmentId, mpsSnapshotId));
}

export async function savePlanningDraft(assignmentId: string, request: SavePlanningWorkbookDraftRequestDto) {
  return checkedDraft(await new ApiClient(await resolveBackendBaseUrl()).savePlanningWorkbookDraft(assignmentId, request));
}

export async function deletePlanningDraft(assignmentId: string): Promise<void> {
  await new ApiClient(await resolveBackendBaseUrl()).deletePlanningWorkbookDraft(assignmentId);
}

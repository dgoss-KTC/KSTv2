import { ApiClient, ApiError, type WorkspaceAssignmentDto, type WorkspaceListResponseDto } from './client';
import type { CreateWorkspaceRequestDto } from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

export interface CreateWorkspaceFields {
  displayName?: string;
  site: string;
  customerNumber?: string;
  productLineFrom?: string;
  productLineTo?: string;
  isTemporary: boolean;
  coverageEndsOn?: string | null;
}

export interface WorkspaceValidationErrors {
  [field: string]: string[];
}

export type WorkspaceApiError =
  | { type: 'validation'; errors: WorkspaceValidationErrors }
  | { type: 'not-found' };

export async function fetchWorkspaces(): Promise<WorkspaceListResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  return client.listWorkspaces();
}

function toRequestDto(fields: CreateWorkspaceFields): CreateWorkspaceRequestDto {
  return {
    displayName: fields.displayName || null,
    site: fields.site,
    customerNumber: fields.customerNumber || null,
    productLineFrom: fields.productLineFrom || null,
    productLineTo: fields.productLineTo || null,
    isTemporary: fields.isTemporary,
    coverageEndsOn: fields.coverageEndsOn ?? null,
  };
}

function toValidationError(err: unknown): WorkspaceApiError | null {
  if (err instanceof ApiError && err.status === 400) {
    let parsed: { errors?: WorkspaceValidationErrors } = {};
    try {
      parsed = JSON.parse(err.message);
    } catch {
      /* not parseable */
    }
    return { type: 'validation', errors: parsed.errors ?? {} };
  }
  return null;
}

function toNotFoundError(err: unknown): WorkspaceApiError | null {
  if (err instanceof ApiError && err.status === 404) {
    return { type: 'not-found' };
  }
  return null;
}

export async function createWorkspace(
  fields: CreateWorkspaceFields,
): Promise<WorkspaceAssignmentDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);

  try {
    return await client.createWorkspace(toRequestDto(fields));
  } catch (err) {
    throw toValidationError(err) ?? err;
  }
}

export async function updateWorkspace(
  assignmentId: string,
  fields: CreateWorkspaceFields,
): Promise<WorkspaceAssignmentDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);

  try {
    return await client.updateWorkspace(assignmentId, toRequestDto(fields));
  } catch (err) {
    throw toValidationError(err) ?? toNotFoundError(err) ?? err;
  }
}

export async function archiveWorkspace(assignmentId: string): Promise<WorkspaceAssignmentDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);

  try {
    return await client.archiveWorkspace(assignmentId);
  } catch (err) {
    throw toNotFoundError(err) ?? err;
  }
}

export async function restoreWorkspace(assignmentId: string): Promise<WorkspaceAssignmentDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);

  try {
    return await client.restoreWorkspace(assignmentId);
  } catch (err) {
    throw toNotFoundError(err) ?? err;
  }
}

export async function deleteWorkspace(assignmentId: string): Promise<void> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);

  try {
    await client.deleteWorkspace(assignmentId);
  } catch (err) {
    throw toNotFoundError(err) ?? err;
  }
}

export async function resetWorkspaces(): Promise<void> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  await client.resetWorkspaces();
}

export async function reorderWorkspaces(assignmentIds: string[]): Promise<WorkspaceListResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);

  try {
    return await client.reorderWorkspaces({ assignmentIds });
  } catch (err) {
    throw toValidationError(err) ?? err;
  }
}

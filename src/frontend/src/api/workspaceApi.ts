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

export interface WorkspaceApiError {
  type: 'validation';
  errors: WorkspaceValidationErrors;
}

export async function fetchWorkspaces(): Promise<WorkspaceListResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  return client.listWorkspaces();
}

export async function createWorkspace(
  fields: CreateWorkspaceFields,
): Promise<WorkspaceAssignmentDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);

  const request: CreateWorkspaceRequestDto = {
    displayName: fields.displayName || null,
    site: fields.site,
    customerNumber: fields.customerNumber || null,
    productLineFrom: fields.productLineFrom || null,
    productLineTo: fields.productLineTo || null,
    isTemporary: fields.isTemporary,
    coverageEndsOn: fields.coverageEndsOn ?? null,
  };

  try {
    return await client.createWorkspace(request);
  } catch (err) {
    if (err instanceof ApiError && err.status === 400) {
      let parsed: { errors?: WorkspaceValidationErrors } = {};
      try {
        parsed = JSON.parse(err.message);
      } catch {
        /* not parseable */
      }
      const apiErr: WorkspaceApiError = {
        type: 'validation',
        errors: parsed.errors ?? {},
      };
      throw apiErr;
    }
    throw err;
  }
}

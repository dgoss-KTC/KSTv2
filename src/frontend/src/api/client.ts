/**
 * Typed API client wrapping generated OpenAPI types.
 * The base URL is provided at runtime by Tauri after the backend starts.
 */
import type { components } from '../generated/api';

export type SystemStatusResponse = components['schemas']['SystemStatusResponse'];
export type HealthResponse = components['schemas']['HealthResponse'];
export type ReadyResponse = components['schemas']['ReadyResponse'];
export type WorkspaceAssignmentDto = components['schemas']['WorkspaceAssignmentDto'];
export type WorkspaceListResponseDto = components['schemas']['WorkspaceListResponseDto'];
export type CreateWorkspaceRequestDto = components['schemas']['CreateWorkspaceRequestDto'];
export type ReorderWorkspacesRequestDto = components['schemas']['ReorderWorkspacesRequestDto'];
export type UserPreferencesDto = components['schemas']['UserPreferencesDto'];
export type PreferencesResponseDto = components['schemas']['PreferencesResponseDto'];
export type UpdatePreferencesRequestDto = components['schemas']['UpdatePreferencesRequestDto'];
export type MpsDashboardResponseDto = components['schemas']['MpsDashboardResponseDto'];
export type MpsSnapshotMetadataDto = components['schemas']['MpsSnapshotMetadataDto'];
export type MpsPartScheduleDto = components['schemas']['MpsPartScheduleDto'];
export type MpsBucketDto = components['schemas']['MpsBucketDto'];
export type PartDetailResponseDto = components['schemas']['PartDetailResponseDto'];
export type PartPriceBreakDto = components['schemas']['PartPriceBreakDto'];

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly url: string,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export class ApiClient {
  private readonly baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/$/, '');
  }

  async getHealth(): Promise<HealthResponse> {
    return this.get<HealthResponse>('/health');
  }

  async getReady(): Promise<ReadyResponse> {
    return this.get<ReadyResponse>('/ready');
  }

  async getSystemStatus(): Promise<SystemStatusResponse> {
    return this.get<SystemStatusResponse>('/api/v1/system/status');
  }

  async listWorkspaces(): Promise<WorkspaceListResponseDto> {
    return this.get<WorkspaceListResponseDto>('/api/v1/workspaces');
  }

  async createWorkspace(request: CreateWorkspaceRequestDto): Promise<WorkspaceAssignmentDto> {
    return this.post<WorkspaceAssignmentDto>('/api/v1/workspaces', request);
  }

  async updateWorkspace(
    assignmentId: string,
    request: CreateWorkspaceRequestDto,
  ): Promise<WorkspaceAssignmentDto> {
    return this.put<WorkspaceAssignmentDto>(`/api/v1/workspaces/${assignmentId}`, request);
  }

  async archiveWorkspace(assignmentId: string): Promise<WorkspaceAssignmentDto> {
    return this.postEmpty<WorkspaceAssignmentDto>(`/api/v1/workspaces/${assignmentId}/archive`);
  }

  async restoreWorkspace(assignmentId: string): Promise<WorkspaceAssignmentDto> {
    return this.postEmpty<WorkspaceAssignmentDto>(`/api/v1/workspaces/${assignmentId}/restore`);
  }

  async deleteWorkspace(assignmentId: string): Promise<void> {
    return this.delete(`/api/v1/workspaces/${assignmentId}`);
  }

  async resetWorkspaces(): Promise<void> {
    return this.delete('/api/v1/workspaces');
  }

  async reorderWorkspaces(request: ReorderWorkspacesRequestDto): Promise<WorkspaceListResponseDto> {
    return this.put<WorkspaceListResponseDto>('/api/v1/workspaces/order', request);
  }

  async getPreferences(): Promise<PreferencesResponseDto> {
    return this.get<PreferencesResponseDto>('/api/v1/preferences');
  }

  async updatePreferences(request: UpdatePreferencesRequestDto): Promise<PreferencesResponseDto> {
    return this.put<PreferencesResponseDto>('/api/v1/preferences', request);
  }

  async refreshSystem(): Promise<SystemStatusResponse> {
    return this.postEmpty<SystemStatusResponse>('/api/v1/system/refresh');
  }

  async getMpsDashboard(
    assignmentId: string,
    dateBasis: string,
    horizonWeeks: number,
  ): Promise<MpsDashboardResponseDto> {
    const query = `?dateBasis=${encodeURIComponent(dateBasis)}&horizonWeeks=${horizonWeeks}`;
    return this.get<MpsDashboardResponseDto>(`/api/v1/workspaces/${assignmentId}/mps${query}`);
  }

  async refreshMpsDashboard(
    assignmentId: string,
    dateBasis: string,
    horizonWeeks: number,
  ): Promise<MpsDashboardResponseDto> {
    const query = `?dateBasis=${encodeURIComponent(dateBasis)}&horizonWeeks=${horizonWeeks}`;
    return this.postEmpty<MpsDashboardResponseDto>(`/api/v1/workspaces/${assignmentId}/mps/refresh${query}`);
  }

  async getPartDetail(assignmentId: string, partNumber: string): Promise<PartDetailResponseDto> {
    const query = `?partNumber=${encodeURIComponent(partNumber)}`;
    return this.get<PartDetailResponseDto>(`/api/v1/workspaces/${assignmentId}/part-detail${query}`);
  }

  private async get<T>(path: string): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'GET',
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }

    return response.json() as Promise<T>;
  }

  private async post<T>(path: string, body: unknown): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }

    return response.json() as Promise<T>;
  }

  private async postEmpty<T>(path: string): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'POST',
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }

    return response.json() as Promise<T>;
  }

  private async put<T>(path: string, body: unknown): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }

    return response.json() as Promise<T>;
  }

  private async delete(path: string): Promise<void> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'DELETE',
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }
  }
}

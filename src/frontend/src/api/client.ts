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

  private async get<T>(path: string): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'GET',
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) {
      throw new ApiError(response.status, url, `HTTP ${response.status} from ${url}`);
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
}

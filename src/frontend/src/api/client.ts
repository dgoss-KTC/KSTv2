/**
 * Typed API client wrapping generated OpenAPI types.
 * The base URL is provided at runtime by Tauri after the backend starts.
 */
import type { components } from '../generated/api';

export type SystemStatusResponse = components['schemas']['SystemStatusResponse'];
export type HealthResponse = components['schemas']['HealthResponse'];
export type ReadyResponse = components['schemas']['ReadyResponse'];

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
}

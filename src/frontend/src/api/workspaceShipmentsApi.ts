import type { components } from '../generated/api';
import { resolveBackendBaseUrl } from './tauri-bridge';

export type WorkspaceShipmentsRequest = components['schemas']['WorkspaceShipmentsRequestDto'];
export type WorkspaceShipmentsSpec = components['schemas']['WorkspaceShipmentsSpecDto'];
export type WorkspaceShipmentsOperation = components['schemas']['WorkspaceShipmentsOperationDto'];
export type WorkspaceShipmentsAcquisition = components['schemas']['WorkspaceShipmentsOperationDto'];
export type WorkspaceShipmentsPage = components['schemas']['HistoricalShipmentsPageDto'];

/** Local calendar arithmetic (including DST), never UTC ISO truncation. Fresh per screen visit. */
export function initialWorkspaceShipmentsFilters(today = new Date()): WorkspaceShipmentsRequest {
  const end = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  const start = new Date(end); start.setDate(start.getDate() - 89);
  const iso = (date: Date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  return { partNumber: null, shipDateFrom: iso(start), shipDateTo: iso(end) };
}

export async function getWorkspaceShipmentsSpec(assignmentId: string, signal?: AbortSignal): Promise<WorkspaceShipmentsSpec> {
  const response = await fetch(`${await resolveBackendBaseUrl()}/api/v1/workspaces/${encodeURIComponent(assignmentId)}/shipments/spec`, { signal });
  if (!response.ok) throw new Error('Workspace Shipments column catalog unavailable.');
  return response.json() as Promise<WorkspaceShipmentsSpec>;
}

/** One client per assignment visit. Loopback HTTP also works on desktop; no Reports host epoch. */
export class WorkspaceShipmentsClient {
  private readonly session = crypto.randomUUID();
  private readonly baseUrl = resolveBackendBaseUrl();
  private operation: { id: string; abort: AbortController } | null = null;
  private departed = false;

  constructor(private readonly assignmentId: string) {}

  private async url(id: string): Promise<string> {
    return `${await this.baseUrl}/api/v1/workspaces/${encodeURIComponent(this.assignmentId)}/shipments/operations/${id}`;
  }
  private headers() { return { 'Content-Type': 'application/json', 'X-KST-Workspace-Shipments-Session': this.session }; }
  private current() {
    if (!this.operation || this.departed) throw new Error('No active Workspace Shipments result.');
    return this.operation;
  }
  private async call<T>(operation: NonNullable<WorkspaceShipmentsClient['operation']>, method: string, suffix: string,
    signal?: AbortSignal, request?: WorkspaceShipmentsRequest): Promise<T> {
    const combined = signal ? AbortSignal.any([signal, operation.abort.signal]) : operation.abort.signal;
    combined.throwIfAborted();
    const url = await this.url(operation.id);
    combined.throwIfAborted();
    const response = await fetch(url + suffix, { method, headers: this.headers(), signal: combined,
      body: request ? JSON.stringify(request) : undefined });
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { detail?: string } | null;
      throw new Error(problem?.detail ?? 'Workspace Shipments operation failed.');
    }
    const result = await response.json() as T;
    combined.throwIfAborted();
    if (this.departed || this.operation !== operation) throw new Error('Workspace Shipments reply superseded.');
    return result;
  }
  private async deleteOperation(id: string): Promise<void> {
    const response = await fetch(await this.url(id), { method: 'DELETE', headers: this.headers(), keepalive: true });
    if (!response.ok) throw new Error('Workspace Shipments release failed.');
  }
  private releaseOperation(): Promise<void> {
    const old = this.operation; this.operation = null;
    old?.abort.abort();
    return old ? this.deleteOperation(old.id) : Promise.resolve();
  }
  async acquire(request: WorkspaceShipmentsRequest, signal?: AbortSignal): Promise<WorkspaceShipmentsAcquisition> {
    if (this.departed) throw new Error('Workspace Shipments visit has ended.');
    const previousRelease = this.releaseOperation();
    const operation = { id: crypto.randomUUID(), abort: new AbortController() };
    this.operation = operation;
    const cancel = () => {
      operation.abort.abort();
      if (this.operation === operation) this.operation = null;
      void this.deleteOperation(operation.id).catch(() => {});
    };
    signal?.addEventListener('abort', cancel, { once: true });
    try {
      await previousRelease;
      signal?.throwIfAborted();
      await this.call<WorkspaceShipmentsOperation>(operation, 'PUT', '', signal);
      return await this.call<WorkspaceShipmentsAcquisition>(operation, 'POST', '/acquire', signal, request);
    } catch (error) {
      operation.abort.abort();
      if (this.operation === operation) this.operation = null;
      await this.deleteOperation(operation.id).catch(() => {});
      throw error;
    } finally { signal?.removeEventListener('abort', cancel); }
  }
  page(offset = 0, limit = 100, signal?: AbortSignal): Promise<WorkspaceShipmentsPage> {
    return this.call(this.current(), 'GET', `/rows?offset=${offset}&limit=${limit}`, signal);
  }
  status(signal?: AbortSignal): Promise<WorkspaceShipmentsOperation> {
    return this.call(this.current(), 'GET', '', signal);
  }
  async release(): Promise<void> { this.departed = true; await this.releaseOperation(); }
}

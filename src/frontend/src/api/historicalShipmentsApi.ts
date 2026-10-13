import type { components } from '../generated/api';
import { isRunningInTauri, resolveBackendBaseUrl } from './tauri-bridge';
import type { ReportSavePort } from '../components/ReportsPanel';

export type HistoricalShipmentsRequest = components['schemas']['HistoricalShipmentsRequestDto'];
export type HistoricalShipmentsSpec = components['schemas']['HistoricalShipmentsSpecDto'];
export type HistoricalShipmentsOperation = components['schemas']['HistoricalShipmentsOperationDto'];
export type HistoricalShipmentsPage = components['schemas']['HistoricalShipmentsPageDto'];
export type HistoricalShipmentsPrepared = components['schemas']['HistoricalShipmentsPreparedDto'];

export async function getHistoricalShipmentsSpec(signal?: AbortSignal): Promise<HistoricalShipmentsSpec> {
  const response = await fetch(`${await resolveBackendBaseUrl()}/api/v1/reports/historical-shipments/spec`, { signal });
  if (!response.ok) throw new Error('Historical Shipments column catalog unavailable.');
  return response.json() as Promise<HistoricalShipmentsSpec>;
}

/** One instance per report visit. Release on departure; never retain every page. */
export class HistoricalShipmentsClient {
  private readonly session = crypto.randomUUID();
  private epoch: number | null = null;
  private operationId: string | null = null;
  private departed = false;
  private baseUrl: string | null = null;
  private acquiring = false;

  private async call<T>(action: string, id: string, request?: HistoricalShipmentsRequest, signal?: AbortSignal, offset?: number, limit?: number): Promise<T> {
    signal?.throwIfAborted();
    if (this.departed && action !== 'release') throw new Error('Report visit has ended.');
    if (isRunningInTauri()) {
      const { invoke } = await import('@tauri-apps/api/core');
      const result = await invoke<T>('historical_shipments_request', { epoch: this.epoch, action, operationId: id, request, offset, limit });
      signal?.throwIfAborted();
      if (this.departed || this.operationId !== id) throw new Error('Historical Shipments reply superseded.');
      return result;
    }
    this.baseUrl ??= await resolveBackendBaseUrl();
    const suffix = action === 'page' ? `/rows?offset=${offset ?? 0}&limit=${limit ?? 100}`
      : action === 'acquire' || action === 'prepare' ? `/${action}` : '';
    const response = await fetch(`${this.baseUrl}/api/v1/reports/historical-shipments/operations/${id}${suffix}`, {
      method: action === 'register' ? 'PUT' : action === 'release' ? 'DELETE' : action === 'acquire' || action === 'prepare' ? 'POST' : 'GET',
      headers: { 'Content-Type': 'application/json', 'X-KST-Report-Session': this.session },
      body: request ? JSON.stringify(request) : undefined, signal,
    });
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { detail?: string } | null;
      throw new Error(problem?.detail ?? 'Historical Shipments operation failed.');
    }
    if (response.status === 204) return undefined as T;
    const result = await response.json() as T;
    signal?.throwIfAborted();
    if (this.departed || this.operationId !== id) throw new Error('Historical Shipments reply superseded.');
    return result;
  }

  async acquire(request: HistoricalShipmentsRequest, signal?: AbortSignal): Promise<HistoricalShipmentsOperation> {
    if (this.departed) throw new Error('Report visit has ended.');
    if (this.acquiring) throw new Error('Historical Shipments acquisition already active.');
    this.acquiring = true;
    const id = crypto.randomUUID();
    const cancel = () => { void this.releaseOperation().catch(() => {}); };
    try {
      await this.releaseOperation();
      if (isRunningInTauri()) {
        const { invoke } = await import('@tauri-apps/api/core');
        this.epoch = await invoke<number>('component_mrp_begin');
      }
      signal?.throwIfAborted();
      if (this.departed) throw new Error('Report visit has ended.');
      this.operationId = id;
      signal?.addEventListener('abort', cancel, { once: true });
      await this.call<HistoricalShipmentsOperation>('register', id, undefined, signal);
      return await this.call<HistoricalShipmentsOperation>('acquire', id, request, signal);
    } catch (error) {
      await this.releaseOperation().catch(() => {});
      if (!isRunningInTauri()) {
        this.baseUrl ??= await resolveBackendBaseUrl();
        await fetch(`${this.baseUrl}/api/v1/reports/historical-shipments/operations/${id}`, {
          method: 'DELETE', headers: { 'X-KST-Report-Session': this.session }, keepalive: true,
        }).catch(() => {});
      }
      throw error;
    } finally { this.acquiring = false; signal?.removeEventListener('abort', cancel); }
  }
  page(offset = 0, limit = 100, signal?: AbortSignal): Promise<HistoricalShipmentsPage> {
    return this.call('page', this.requireOperation(), undefined, signal, offset, limit);
  }
  status(signal?: AbortSignal): Promise<HistoricalShipmentsOperation> {
    return this.call('status', this.requireOperation(), undefined, signal);
  }
  prepare(signal?: AbortSignal): Promise<HistoricalShipmentsPrepared> {
    return this.call('prepare', this.requireOperation(), undefined, signal);
  }
  async save(prepared: HistoricalShipmentsPrepared, openAfterSave: boolean, saveOperationId: string): Promise<Awaited<ReturnType<ReportSavePort['save']>>> {
    if (!isRunningInTauri()) throw new Error('Protected workbook saving requires the desktop application.');
    if (prepared.operationId !== this.requireOperation()) throw new Error('Preparation belongs to another report result.');
    const { invoke } = await import('@tauri-apps/api/core');
    return invoke('component_mrp_save', { operationId: saveOperationId, preparationId: prepared.preparationId,
      openAfterSave, reportKind: 'HistoricalShipments' });
  }
  async saveStatus(saveOperationId: string): Promise<Awaited<ReturnType<NonNullable<ReportSavePort['status']>>>> {
    const { invoke } = await import('@tauri-apps/api/core');
    return invoke('component_mrp_status', { operationId: saveOperationId, reportKind: 'HistoricalShipments' });
  }
  async open(savedArtifactId: string, requestId: string): Promise<void> {
    const { invoke } = await import('@tauri-apps/api/core');
    await invoke('component_mrp_open', { artifactId: savedArtifactId, requestId, reportKind: 'HistoricalShipments' });
  }
  private requireOperation(): string {
    if (!this.operationId || this.departed) throw new Error('No active Historical Shipments result.');
    return this.operationId;
  }
  private async releaseOperation(): Promise<void> {
    const id = this.operationId; this.operationId = null;
    if (!id) return;
    if (isRunningInTauri()) {
      const { invoke } = await import('@tauri-apps/api/core');
      // Epoch-bound invalidation cannot discard a later report visit.
      if (this.epoch !== null) await invoke('sop_discard_generation', { epoch: this.epoch });
    } else {
      this.baseUrl ??= await resolveBackendBaseUrl();
      await fetch(`${this.baseUrl}/api/v1/reports/historical-shipments/operations/${id}`, {
        method: 'DELETE', headers: { 'X-KST-Report-Session': this.session }, keepalive: true,
      });
    }
  }
  async release(): Promise<void> { this.departed = true; await this.releaseOperation(); }
}

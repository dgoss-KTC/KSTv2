import type { AnalyticalSaveResult } from '../api/analyticalExportApi';
import { invoke } from '@tauri-apps/api/core';
import type { ComponentMrpRequest, ComponentMrpPrepared } from '../api/componentMrpApi';
import type { ShipmentsToGoRequest, ShipmentsToGoPrepared } from '../api/shipmentsToGoApi';
import type { SopRequest, SopPrepared } from '../api/sopApi';
import type { OpenOrderReportRequest, OpenOrderReportPrepared } from '../api/openOrderReportApi';
import type { HistoricalShipmentsClient, HistoricalShipmentsPrepared } from '../api/historicalShipmentsApi';
import type { ExcelExportPort, PreparedExcel, EmptyExcel } from '../shared/excelExportPort';
import type { components } from '../generated/api';

export type WorkbookKind = 'ComponentMrp' | 'ShipmentsToGo' | 'Sop' | 'OpenOrderReport';
export interface ReportRequests { ComponentMrp: ComponentMrpRequest; ShipmentsToGo: ShipmentsToGoRequest; Sop: SopRequest; OpenOrderReport: OpenOrderReportRequest; WorkspaceShortages: components['schemas']['WorkspaceShortagesExportRequestDto'] }
export type ReportPrepared = ComponentMrpPrepared | ShipmentsToGoPrepared | SopPrepared | OpenOrderReportPrepared;
const emptyMessages = { ComponentMrp: 'No Component MRP data found.', ShipmentsToGo: 'No shipments to go found for the selected sites.',
  Sop: 'No S&OP data found for the selected ranges.', OpenOrderReport: 'No open orders found for the specified filters.', WorkspaceShortages: 'No displayed Workspace Shortages components.' };

/** Each visit owns a host generation; cleanup cannot invalidate a later generation. */
export class WorkbookReportExport<K extends keyof ReportRequests> implements ExcelExportPort<ReportRequests[K]> {
  private ended = false;
  private epoch: number | null = null;
  private abort = new AbortController();
  constructor(private kind: K, private generate: (request: ReportRequests[K], signal?: AbortSignal, epoch?: number) => Promise<ReportPrepared>) {}
  async prepare(request: ReportRequests[K]): Promise<PreparedExcel | EmptyExcel> {
    this.abort.signal.throwIfAborted();
    this.epoch = await invoke<number>('component_mrp_begin');
    if (this.ended) { await this.release(); throw new Error('Report visit has ended.'); }
    const result = await this.generate(request, this.abort.signal, this.epoch ?? undefined);
    this.abort.signal.throwIfAborted();
    if ('outcome' in result && result.outcome === 'noData') return { outcome: 'noData', message: emptyMessages[this.kind] };
    if (!result.preparationId || !result.fileName) throw new Error('Invalid report preparation. Nothing was saved.');
    return result as PreparedExcel;
  }
  async save(prepared: PreparedExcel, openAfterSave: boolean, operationId: string): Promise<AnalyticalSaveResult> {
    if (this.ended) return { outcome: 'cancelled', message: 'Report save cancelled before host dispatch.', operationComplete: true };
    return invoke('component_mrp_save', { preparationId: prepared.preparationId, operationId, openAfterSave, reportKind: this.kind });
  }
  async status(operationId: string): Promise<AnalyticalSaveResult> {
    return invoke('component_mrp_status', { operationId, reportKind: this.kind });
  }
  async open(artifactId: string): Promise<void> {
    await invoke('component_mrp_open', { artifactId, requestId: crypto.randomUUID(), reportKind: this.kind });
  }
  async release(): Promise<void> {
    this.ended = true; this.abort.abort();
    if (this.epoch !== null) {
      await invoke('sop_discard_generation', { epoch: this.epoch });
    }
  }
}

/** Borrow the applied result, never own/release it. Host bytes remain bounded by that result's lifetime. */
export class HistoricalReportExport implements ExcelExportPort<{ operationId: string }> {
  private ended = false;
  private prepared: HistoricalShipmentsPrepared | null = null;
  constructor(private client: HistoricalShipmentsClient, private operationId: string) {}
  async prepare(request: { operationId: string }): Promise<PreparedExcel> {
    if (this.ended || request.operationId !== this.operationId) throw new Error('Historical Shipments result changed.');
    const result = await this.client.prepare();
    if (this.ended || result.operationId !== this.operationId) throw new Error('Historical Shipments preparation superseded.');
    this.prepared = result; return result;
  }
  async save(prepared: PreparedExcel, openAfterSave: boolean, operationId: string) {
    if (this.ended || prepared !== this.prepared) return { outcome: 'cancelled', message: 'Report save cancelled before host dispatch.', operationComplete: true };
    return this.client.save(this.prepared!, openAfterSave, operationId);
  }
  status(operationId: string) { return this.client.saveStatus(operationId); }
  open(artifactId: string) { return this.client.open(artifactId, crypto.randomUUID()); }
  async release() { this.ended = true; this.prepared = null; }
}

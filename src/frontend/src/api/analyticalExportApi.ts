import type { components } from '../generated/api';
import { isRunningInTauri } from './tauri-bridge';

export type AnalyticalRequest = components['schemas']['AnalyticalExportRequestDto'];
export type AnalyticalSheet = components['schemas']['AnalyticalSheetDto'];
export type AnalyticalCell = components['schemas']['AnalyticalCellDto'];
export type AnalyticalPrepared = components['schemas']['AnalyticalExportPreparedDto'];
export interface AnalyticalSaveResult { outcome: string; message: string; operationComplete?: boolean; savedArtifactId?: string; openFailed?: boolean }

/** Fixed managed-backend route; all destination/filesystem/open authority stays in the host. */
export class AnalyticalExportClient {
  private epoch: number | null = null;
  private ended = false;
  async prepare(request: AnalyticalRequest): Promise<AnalyticalPrepared> {
    if (!isRunningInTauri()) throw new Error('Export Excel requires the desktop application and its protected Save As.');
    const { invoke } = await import('@tauri-apps/api/core');
    const epoch = await invoke<number>('component_mrp_begin');
    this.epoch = epoch;
    if (this.ended) { await this.release(); throw new Error('Export context changed.'); }
    const result = await invoke<AnalyticalPrepared>('component_mrp_generate', { epoch, reportKind: 'Analytical', analyticalRequest: request,
      site: '', part: '', horizonWeeks: 0, includeUnconfirmed: false, period: '' });
    if (this.ended) throw new Error('Export context changed.');
    return result;
  }
  async save(prepared: AnalyticalPrepared, openAfterSave: boolean, operationId: string): Promise<AnalyticalSaveResult> {
    if (this.ended) return { outcome: 'cancelled', message: 'Excel save cancelled before host dispatch.', operationComplete: true };
    const { invoke } = await import('@tauri-apps/api/core');
    if (this.ended) return { outcome: 'cancelled', message: 'Excel save cancelled before host dispatch.', operationComplete: true };
    return invoke('component_mrp_save', { operationId, preparationId: prepared.preparationId, openAfterSave, reportKind: 'Analytical' });
  }
  async status(operationId: string): Promise<AnalyticalSaveResult> {
    const { invoke } = await import('@tauri-apps/api/core');
    return invoke('component_mrp_status', { operationId, reportKind: 'Analytical' });
  }
  async open(artifactId: string): Promise<void> {
    const { invoke } = await import('@tauri-apps/api/core');
    await invoke('component_mrp_open', { artifactId, requestId: crypto.randomUUID(), reportKind: 'Analytical' });
  }
  async release(): Promise<void> {
    this.ended = true;
    if (this.epoch !== null) {
      const { invoke } = await import('@tauri-apps/api/core');
      await invoke('sop_discard_generation', { epoch: this.epoch });
    }
  }
}

export function analyticalRequest(kind: string, workspaceId: string, mpsSnapshotId: string, site: string, businessDate: string): AnalyticalRequest {
  return { kind, workspaceId, mpsSnapshotId, site, businessDate, horizonWeeks: 0, isStale: false, metadata: [], fiscalLabels: [], sheets: [],
    parent: null, dateBasis: null, bomLoadedAtUtc: null, bomEffectiveDate: null, warning: null, componentFilter: null, descriptionFilter: null,
    pmFilter: null, phantomFilter: null, workbookSnapshotId: null };
}

import { invoke } from '@tauri-apps/api/core';
import type { components } from '../generated/api';
import { WorkbookReportExport } from '../reports/reportExportAdapters';

export type WorkspaceShortagesExportRequest = components['schemas']['WorkspaceShortagesExportRequestDto'];

/** Fixed report kind, not a browser download or renderer-provided filesystem destination. */
export function workspaceShortagesExport() {
  return new WorkbookReportExport('WorkspaceShortages', (request: WorkspaceShortagesExportRequest, _signal, epoch) =>
    invoke('component_mrp_generate', { epoch, reportKind: 'WorkspaceShortages', analyticalRequest: request,
      site: '', part: '', horizonWeeks: 0, includeUnconfirmed: false, period: '' }));
}

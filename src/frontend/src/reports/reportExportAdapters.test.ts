import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { WorkbookReportExport, HistoricalReportExport } from './reportExportAdapters';
import { generateComponentMrp } from '../api/componentMrpApi';
import { generateShipmentsToGo } from '../api/shipmentsToGoApi';
import { generateOpenOrderReport } from '../api/openOrderReportApi';
import { generateSop } from '../api/sopApi';
import { reportFixture } from './reportTestSupport';
import { initialOor } from './openOrderReportPreferences';
import type { HistoricalShipmentsClient } from '../api/historicalShipmentsApi';
const host = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => true }));
beforeEach(() => { localStorage.clear(); host.invoke.mockReset(); host.invoke.mockImplementation(async (n: string) => n === 'component_mrp_begin' ? 11 : n === 'component_mrp_generate' ? reportFixture : { outcome: 'saved', message: 'Workbook saved.', operationComplete: true }); });
afterEach(() => vi.unstubAllGlobals());
describe('typed report-specific production command adapters', () => {
  it('ComponentMrp preparation carries complete request, reuses issuing epoch and never Analytical kind', async () => {
    const request = { site: 'KV', part: 'EXACT', period: 'Month', horizonWeeks: 72, includeUnconfirmed: false };
    const port = new WorkbookReportExport('ComponentMrp', generateComponentMrp); const prepared = await port.prepare(request);
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_generate', { epoch: 11, ...request });
    expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_begin')).toHaveLength(1);
    if (!('preparationId' in prepared)) throw new Error('Expected workbook'); await port.save(prepared, true, 'save'); await port.status('save'); await port.open('artifact'); await port.release();
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_save', { reportKind: 'ComponentMrp', preparationId: reportFixture.preparationId, operationId: 'save', openAfterSave: true });
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_status', { reportKind: 'ComponentMrp', operationId: 'save' }); expect(host.invoke).toHaveBeenCalledWith('component_mrp_open', expect.objectContaining({ reportKind: 'ComponentMrp', artifactId: 'artifact' })); expect(host.invoke).toHaveBeenCalledWith('sop_discard_generation', { epoch: 11 });
  });
  it('ShipmentsToGo native generation preserves site order and next-week parameter', async () => {
    const port = new WorkbookReportExport('ShipmentsToGo', generateShipmentsToGo); await port.prepare({ sites: ['KV', 'SW'], includeNextWeek: true });
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_generate', expect.objectContaining({ epoch: 11, reportKind: 'ShipmentsToGo', sites: ['KV', 'SW'], includeNextWeek: true })); await port.release();
  });
  it('OpenOrderReport keeps flexible-date/filter/optional-fields request intact', async () => {
    const request = { ...initialOor(), customerName: 'ACME%', dueDateFrom: '10-1', dueDateTo: '10.31.26', optionalFields: ['Allocated', 'Unit Price'] };
    const port = new WorkbookReportExport('OpenOrderReport', generateOpenOrderReport); await port.prepare(request);
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_generate', expect.objectContaining({ epoch: 11, reportKind: 'OpenOrderReport', oorRequest: request })); await port.release();
  });
  it('Sop uses acquisition and fiscal presentation on the same issuing epoch', async () => {
    const request = { site: 'SW', groupBy: 'planr', ranges: [{ from: '2142', to: '2140' }] };
    host.invoke.mockImplementation(async (n: string, args?: { sopRequest?: unknown }) => n === 'component_mrp_begin' ? 11 : n === 'component_mrp_generate' ? args?.sopRequest ? { outcome: 'acquired', acquisitionId: 'acq', weekLabels: ['2026-10-19'], warning: null } : { ...reportFixture, outcome: 'prepared' } : undefined);
    const port = new WorkbookReportExport('Sop', generateSop); await port.prepare(request);
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_generate', expect.objectContaining({ epoch: 11, reportKind: 'Sop', sopRequest: request }));
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_generate', expect.objectContaining({ epoch: 11, reportKind: 'Sop', sopPrepare: expect.objectContaining({ acquisitionId: 'acq', presentation: expect.any(Object) }) })); expect(host.invoke).toHaveBeenCalledWith('sop_discard_acquisition', { acquisitionId: 'acq' }); await port.release();
  });
  it.each(['ShipmentsToGo', 'Sop', 'OpenOrderReport'] as const)('%s normalizes noData without ever saving', async kind => {
    const port = new WorkbookReportExport(kind, vi.fn().mockResolvedValue({ outcome: 'noData', preparationId: null, fileName: null })); const request = kind === 'ShipmentsToGo' ? { sites: ['SW'], includeNextWeek: false } : kind === 'Sop' ? { site: 'SW', groupBy: 'planr', ranges: [{ from: '0001', to: '0002' }] } : initialOor();
    expect(await port.prepare(request)).toMatchObject({ outcome: 'noData', message: expect.stringMatching(/^No /) }); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false); await port.release();
  });
  it('Historical adapter borrows only applied operation; remount/release never releases acquisition', async () => {
    const prepared = { operationId: 'applied', preparationId: 'applied', fileName: 'History.xlsx', byteLength: 5, expiresAtUtc: '' };
    const client = { prepare: vi.fn().mockResolvedValue(prepared), release: vi.fn(), save: vi.fn(), saveStatus: vi.fn(), open: vi.fn() };
    const port = new HistoricalReportExport(client as unknown as HistoricalShipmentsClient, 'applied'); expect(await port.prepare({ operationId: 'applied' })).toBe(prepared); await port.save(prepared, false, 'save'); await port.status('save'); await port.open('artifact'); await port.release();
    expect(client.save).toHaveBeenCalledWith(prepared, false, 'save'); expect(client.saveStatus).toHaveBeenCalledWith('save'); expect(client.open).toHaveBeenCalledWith('artifact', expect.any(String)); expect(client.release).not.toHaveBeenCalled();
    const other = new HistoricalReportExport(client as unknown as HistoricalShipmentsClient, 'different'); await expect(other.prepare({ operationId: 'draft' })).rejects.toThrow('result changed'); await expect(other.prepare({ operationId: 'different' })).rejects.toThrow('superseded');
  });
});

import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { StrictMode } from 'react';
import { ReportsPanel } from './ReportsPanel';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';
import { reportFixture as prepared, deferred, exportWorkbook, clearExportTests } from '../reports/reportTestSupport';
import { partInfoFixture, mrpFixture } from '../reports/partInfoMrpTestSupport';
import { rememberAnalyticalOpenPreference } from '../shared/analyticalOpenPreference';
import type { AnalyticalSaveResult } from '../api/analyticalExportApi';
const api = vi.hoisted(() => ({ lookup: vi.fn(), mrp: vi.fn() }));
const host = vi.hoisted(() => ({ invoke: vi.fn(), listener: undefined as undefined | ((e: { payload: { operationId: string; result: AnalyticalSaveResult } }) => void) }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('@tauri-apps/api/event', () => ({ listen: vi.fn(async (_n, fn) => { host.listener = fn; return () => {}; }) }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => true }));
vi.mock('../api/partInfoApi', () => ({ getPartInfoSpec: async () => ({ sites: ['SW', 'KV'], maximumPartLength: 256 }), lookupPartInfo: api.lookup, refreshPartInfoLocations: vi.fn() }));
vi.mock('../api/componentMrpApi', () => ({ readComponentMrp: api.mrp, generateComponentMrp: vi.fn() }));
beforeEach(async () => {
  localStorage.clear(); host.invoke.mockReset(); host.listener = undefined;
  api.lookup.mockReset().mockImplementation(async (site, part) => partInfoFixture(part, site));
  api.mrp.mockReset().mockImplementation(async request => mrpFixture(request));
  host.invoke.mockImplementation(async (name: string) => name === 'component_mrp_begin' ? 7 : name === 'component_mrp_save' ? { outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true } : false);
  await act(async () => rememberAnalyticalOpenPreference(false));
});
afterEach(clearExportTests);
const setup = (generate = vi.fn().mockResolvedValue(prepared), busy = vi.fn()) => ({ generate, busy, ...render(<><AnalyticalSaveStatus /><ReportsPanel generate={generate} onBusyChange={busy} /></>) });
async function part() {
  await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled());
  fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: ' ABC ' } });
  fireEvent.submit(screen.getByLabelText('Part Number').closest('form')!);
  await screen.findByRole('table', { name: 'Component MRP periods' });
  await waitFor(() => expect(screen.getByRole('button', { name: 'Export' })).toBeEnabled());
}
describe('Reports combined Part Info and shared protected export — offline mocked host', () => {
  it('uses combined default, five grouped entries, scope-slot reason and accepted MRP defaults', async () => {
    setup(); expect(screen.getByRole('navigation', { name: 'Report selector' })).toHaveTextContent('Workbooks');
    expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled(); expect(screen.getByText('Look up a part first.')).toHaveClass('analytical-export__scope');
    expect(screen.getByRole('heading', { name: 'Part Info & MRP' })).toBeInTheDocument(); expect(screen.queryByRole('button', { name: 'Component MRP' })).toBeNull();
    expect(screen.getByText('On screen').parentElement).toHaveTextContent('Part Info & MRP'); await part();
    expect(screen.getByLabelText('Site')).toHaveValue('SW'); expect(screen.getByRole('button', { name: '26' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Week' })).toHaveAttribute('aria-pressed', 'true'); expect(screen.getByLabelText('Include Unconfirmed POs')).toBeChecked();
    expect(screen.getByText(/Exports the MRP shown/)).toHaveTextContent('SW · ABC · 26 weeks by Week');
  });
  it('prepares displayed acquisition then automatically saves with ComponentMrp kind; retry retains exact bytes', async () => {
    const { generate } = setup(); await part(); await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' });
    expect(generate).toHaveBeenCalledWith({ site: 'SW', part: 'ABC', horizonWeeks: 26, period: 'Week', includeUnconfirmed: true, acquisitionId: '11111111-1111-4111-8111-111111111111', asOfDate: '2026-09-29' }, expect.any(AbortSignal), 7);
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_save', expect.objectContaining({ reportKind: 'ComponentMrp', preparationId: 'exact-one', openAfterSave: false }));
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Save again' })));
    expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_save')).toHaveLength(2); expect(generate).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole('button', { name: 'Discard workbook' })); expect(screen.queryByRole('button', { name: 'Save again' })).toBeNull();
    await waitFor(() => expect(host.invoke).toHaveBeenCalledWith('sop_discard_generation', { epoch: 7 }));
  });
  it('Enter looks up once and does not export before protected activation', async () => {
    const { generate } = setup(); await part(); expect(generate).not.toHaveBeenCalled(); expect(api.lookup).toHaveBeenCalledTimes(1); expect(api.mrp).toHaveBeenCalledTimes(1);
    await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' }); expect(generate).toHaveBeenCalledTimes(1);
  });
  it('title export distinguishes MRP loading, localized failure and no-data without losing lookup facts', async () => {
    const pending = deferred<ReturnType<typeof mrpFixture>>(); api.mrp.mockReturnValueOnce(pending.promise);
    const { generate } = setup(); await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled());
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Lookup'));
    await screen.findByText('MRP is loading.'); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    await act(async () => pending.resolve({ ...mrpFixture(), periods: [] })); await screen.findByText('No MRP data to export.'); expect(screen.getByText('No Component MRP data found.')).toBeInTheDocument();
    api.mrp.mockRejectedValueOnce(new Error('Synthetic MRP failure')); fireEvent.click(screen.getByRole('button', { name: 'Refresh MRP' }));
    await screen.findByText('MRP unavailable. Retry MRP.'); expect(screen.getByText('Synthetic facts')).toBeInTheDocument(); expect(generate).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Retry MRP' })); await screen.findByRole('table', { name: 'Component MRP periods' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Export' })).toBeEnabled());
  });
  it('discloses loaded data and rejects late preparation after identity edits', async () => {
    const pending = deferred<typeof prepared>(); const generate = vi.fn().mockReturnValue(pending.promise); setup(generate); await part(); await exportWorkbook();
    expect(screen.getByText('Preparing Excel workbook from loaded data…')).toBeInTheDocument(); fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'B' } });
    expect(generate.mock.calls[0][1].aborted).toBe(true); await act(async () => pending.resolve(prepared)); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false);
  });
  it('departure aborts preparation and preserves site preference without accepting late replies', async () => {
    const pending = deferred<typeof prepared>(); const { generate, rerender } = setup(vi.fn().mockReturnValue(pending.promise));
    await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled()); fireEvent.change(screen.getByLabelText('Site'), { target: { value: 'KV' } }); await part(); await exportWorkbook();
    rerender(<><AnalyticalSaveStatus /><ReportsPanel active={false} generate={generate} /></>); expect(generate.mock.calls[0][1].aborted).toBe(true);
    await act(async () => pending.resolve(prepared)); expect(localStorage.getItem('kst.componentMrp.site.v1')).toBe('KV'); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false);
  });
  it('unknown save survives report switch; status is observation-only and terminal outcome wins late replies', async () => {
    const pending = deferred<AnalyticalSaveResult>(); const busy = vi.fn(); host.invoke.mockImplementation(async (n: string) => n === 'component_mrp_begin' ? 7 : n === 'component_mrp_save' ? pending.promise : n === 'component_mrp_status' ? { outcome: 'saved', message: 'Workbook saved.', operationComplete: true } : false);
    setup(undefined, busy); await part(); await exportWorkbook(); await waitFor(() => expect(busy).toHaveBeenLastCalledWith(true));
    const id = host.invoke.mock.calls.find(([n]) => n === 'component_mrp_save')![1].operationId;
    fireEvent.click(screen.getByRole('button', { name: 'Shipments-To-Go' })); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Check save status' })));
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_status', { operationId: id, reportKind: 'ComponentMrp' }); await waitFor(() => expect(busy).toHaveBeenLastCalledWith(false));
    await act(async () => { host.listener?.({ payload: { operationId: id, result: { outcome: 'failed', message: 'obsolete event', operationComplete: true } } }); pending.resolve({ outcome: 'failed', message: 'obsolete invoke', operationComplete: true }); });
    expect(screen.queryByText(/obsolete/)).not.toBeInTheDocument(); expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_save')).toHaveLength(1);
  });
  it('opening failure retains warning and original kind without regeneration', async () => {
    host.invoke.mockImplementation(async (n: string) => n === 'component_mrp_begin' ? 7 : n === 'component_mrp_save' ? { outcome: 'saved', message: 'Workbook saved, but could not be opened.', operationComplete: true, openFailed: true, savedArtifactId: 'artifact' } : false);
    const { generate } = setup(vi.fn().mockResolvedValue({ ...prepared, warning: 'Synthetic warning' })); await part(); await exportWorkbook();
    await screen.findByText('Synthetic warning'); await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Open again' })));
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_open', expect.objectContaining({ artifactId: 'artifact', reportKind: 'ComponentMrp' })); expect(generate).toHaveBeenCalledTimes(1);
  });
  it('StrictMode starts one real preparation and cleanup remains epoch-bound', async () => {
    const generate = vi.fn().mockResolvedValue(prepared); render(<StrictMode><AnalyticalSaveStatus /><ReportsPanel generate={generate} /></StrictMode>); await part(); await exportWorkbook();
    await screen.findByRole('button', { name: 'Save again' }); expect(generate).toHaveBeenCalledTimes(1);
  });
  it('Shipments-To-Go retains defaults, site All/None, kind and noData success', async () => {
    const generate = vi.fn().mockResolvedValueOnce({ ...prepared, outcome: 'noData' }).mockResolvedValueOnce({ ...prepared, outcome: 'prepared' });
    render(<><AnalyticalSaveStatus /><ReportsPanel generateStg={generate} /></>); fireEvent.click(screen.getByRole('button', { name: 'Shipments-To-Go' }));
    for (const s of ['SW', 'NW', 'KV']) expect(screen.getByRole('checkbox', { name: s })).toBeChecked();
    fireEvent.click(screen.getByRole('button', { name: 'None' })); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'All' })); fireEvent.click(screen.getByLabelText('Include Next Week?')); await exportWorkbook();
    expect(screen.getAllByText('No shipments to go found for the selected sites.')).toHaveLength(2); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false);
    expect(generate.mock.calls[0][0]).toEqual({ sites: ['SW', 'NW', 'AR', 'MN', 'MS', 'KV'], includeNextWeek: true });
    await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' }); expect(host.invoke).toHaveBeenCalledWith('component_mrp_save', expect.objectContaining({ reportKind: 'ShipmentsToGo' }));
  });
});

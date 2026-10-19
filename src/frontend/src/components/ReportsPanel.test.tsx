import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { StrictMode } from 'react';
import { ReportsPanel } from './ReportsPanel';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';
import { reportFixture as prepared, deferred, exportWorkbook, clearExportTests } from '../reports/reportTestSupport';
import { rememberAnalyticalOpenPreference } from '../shared/analyticalOpenPreference';
import type { AnalyticalSaveResult } from '../api/analyticalExportApi';
const host = vi.hoisted(() => ({ invoke: vi.fn(), listener: undefined as undefined | ((e: { payload: { operationId: string; result: AnalyticalSaveResult } }) => void) }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('@tauri-apps/api/event', () => ({ listen: vi.fn(async (_n, fn) => { host.listener = fn; return () => {}; }) }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => true }));
beforeEach(async () => {
  localStorage.clear(); host.invoke.mockReset(); host.listener = undefined;
  host.invoke.mockImplementation(async (name: string) => name === 'component_mrp_begin' ? 7 : name === 'component_mrp_save' ? { outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true } : false);
  await act(async () => rememberAnalyticalOpenPreference(false));
});
afterEach(clearExportTests);
const setup = (generate = vi.fn().mockResolvedValue(prepared), busy = vi.fn()) => ({ generate, busy, ...render(<><AnalyticalSaveStatus /><ReportsPanel generate={generate} onBusyChange={busy} /></>) });
function part() { fireEvent.change(screen.getByLabelText('Part'), { target: { value: ' ABC ' } }); }
describe('Reports shared export migration — offline mocked host', () => {
  it('uses grouped selector, scope-slot disabled reason, accepted defaults and secondary Export', () => {
    setup(); expect(screen.getByRole('navigation', { name: 'Report selector' })).toHaveTextContent('Workbooks');
    expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled(); expect(screen.getByText('Enter a part number.')).toHaveClass('analytical-export__scope');
    expect(screen.getByLabelText('Site')).toHaveValue('SW'); expect(screen.getByRole('button', { name: '26' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Week' })).toHaveAttribute('aria-pressed', 'true'); expect(screen.getByLabelText('Include Unconfirmed POs')).toBeChecked();
    expect(screen.queryByText('Generate')).not.toBeInTheDocument(); expect(screen.queryByText('Save As…')).not.toBeInTheDocument();
  });
  it('one activation acquires facts then automatically saves with actual ComponentMrp kind; retry retains exact bytes', async () => {
    const { generate } = setup(); part(); await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' });
    expect(generate).toHaveBeenCalledWith({ site: 'SW', part: ' ABC ', horizonWeeks: 26, period: 'Week', includeUnconfirmed: true }, expect.any(AbortSignal), 7);
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_save', expect.objectContaining({ reportKind: 'ComponentMrp', preparationId: 'exact-one', openAfterSave: false }));
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Save again' })));
    expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_save')).toHaveLength(2); expect(generate).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole('button', { name: 'Discard workbook' })); expect(screen.queryByRole('button', { name: 'Save again' })).toBeNull();
    await waitFor(() => expect(host.invoke).toHaveBeenCalledWith('sop_discard_generation', { epoch: 7 }));
  });
  it('Enter invokes the same guarded action with one source acquisition', async () => {
    const { generate } = setup(); part(); const form = screen.getByLabelText('Part').closest('form')!;
    fireEvent.submit(form); fireEvent.submit(form); await screen.findByRole('button', { name: 'Save again' }); expect(generate).toHaveBeenCalledTimes(1);
  });
  it('retains fresh-facts wording while preparing and prevents late success after input edits', async () => {
    const pending = deferred<typeof prepared>(); const generate = vi.fn().mockReturnValue(pending.promise); setup(generate); part(); await exportWorkbook();
    expect(screen.getByText('Acquiring fresh report facts…')).toBeInTheDocument(); fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'B' } });
    expect(generate.mock.calls[0][1].aborted).toBe(true); await act(async () => pending.resolve(prepared)); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false);
  });
  it('report departure aborts acquisition and preserves input preference without accepting late replies', async () => {
    const pending = deferred<typeof prepared>(); const { generate, rerender } = setup(vi.fn().mockReturnValue(pending.promise)); part();
    fireEvent.change(screen.getByLabelText('Site'), { target: { value: 'KV' } }); await exportWorkbook();
    rerender(<><AnalyticalSaveStatus /><ReportsPanel active={false} generate={generate} /></>); expect(generate.mock.calls[0][1].aborted).toBe(true);
    await act(async () => pending.resolve(prepared)); expect(localStorage.getItem('kst.componentMrp.site.v1')).toBe('KV'); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false);
  });
  it('unknown save survives report switch, status is observation-only and late replies cannot replace terminal event', async () => {
    const pending = deferred<AnalyticalSaveResult>(); const busy = vi.fn(); host.invoke.mockImplementation(async (n: string) => n === 'component_mrp_begin' ? 7 : n === 'component_mrp_save' ? pending.promise : n === 'component_mrp_status' ? { outcome: 'saved', message: 'Workbook saved.', operationComplete: true } : false);
    setup(undefined, busy); part(); await exportWorkbook(); await waitFor(() => expect(busy).toHaveBeenLastCalledWith(true));
    const id = host.invoke.mock.calls.find(([n]) => n === 'component_mrp_save')![1].operationId;
    fireEvent.click(screen.getByRole('button', { name: 'Shipments-To-Go' })); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Check save status' })));
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_status', { operationId: id, reportKind: 'ComponentMrp' }); await waitFor(() => expect(busy).toHaveBeenLastCalledWith(false));
    await act(async () => { host.listener?.({ payload: { operationId: id, result: { outcome: 'failed', message: 'obsolete event', operationComplete: true } } }); pending.resolve({ outcome: 'failed', message: 'obsolete invoke', operationComplete: true }); });
    expect(screen.queryByText(/obsolete/)).not.toBeInTheDocument(); expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_save')).toHaveLength(1);
  });
  it('opening failure keeps warning and uses original report kind for Open again without generation', async () => {
    host.invoke.mockImplementation(async (n: string) => n === 'component_mrp_begin' ? 7 : n === 'component_mrp_save' ? { outcome: 'saved', message: 'Workbook saved, but could not be opened.', operationComplete: true, openFailed: true, savedArtifactId: 'artifact' } : false);
    const { generate } = setup(vi.fn().mockResolvedValue({ ...prepared, warning: 'Synthetic warning' })); part(); await exportWorkbook();
    await screen.findByText('Synthetic warning'); await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Open again' })));
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_open', expect.objectContaining({ artifactId: 'artifact', reportKind: 'ComponentMrp' })); expect(generate).toHaveBeenCalledTimes(1);
  });
  it('StrictMode starts one real generation and never discards a subsequent generation with an old epoch', async () => {
    const generate = vi.fn().mockResolvedValue(prepared); render(<StrictMode><AnalyticalSaveStatus /><ReportsPanel generate={generate} /></StrictMode>); part(); await exportWorkbook();
    await screen.findByRole('button', { name: 'Save again' }); expect(generate).toHaveBeenCalledTimes(1);
  });
  it('Shipments-To-Go retains defaults, site All/None persistence, exact kind and noData success', async () => {
    const generate = vi.fn().mockResolvedValueOnce({ ...prepared, outcome: 'noData' }).mockResolvedValueOnce({ ...prepared, outcome: 'prepared' });
    render(<><AnalyticalSaveStatus /><ReportsPanel generateStg={generate} /></>); fireEvent.click(screen.getByRole('button', { name: 'Shipments-To-Go' }));
    for (const s of ['SW', 'NW', 'KV']) expect(screen.getByRole('checkbox', { name: s })).toBeChecked();
    fireEvent.click(screen.getByRole('button', { name: 'None' })); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'All' })); fireEvent.click(screen.getByLabelText('Include Next Week?')); await exportWorkbook();
    expect(screen.getAllByText('No shipments to go found for the selected sites.')).toHaveLength(2); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false);
    expect(generate.mock.calls[0][0]).toEqual({ sites: ['SW', 'NW', 'AR', 'MN', 'MS', 'KV'], includeNextWeek: true });
    await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' }); expect(host.invoke).toHaveBeenCalledWith('component_mrp_save', expect.objectContaining({ reportKind: 'ShipmentsToGo' }));
    expect(localStorage.getItem('kst.shipmentsToGo.inputs.v1')).toContain('true');
  });
});

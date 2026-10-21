import { act, cleanup, fireEvent, render, renderHook, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { StrictMode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AnalyticalExcelExport } from './AnalyticalExcelExport';
import { analyticalRequest, AnalyticalExportClient, type AnalyticalPrepared, type AnalyticalSaveResult } from '../api/analyticalExportApi';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';
import { dismissAnalyticalSave, recordAnalyticalSave, useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';
import { rememberAnalyticalOpenPreference } from '../shared/analyticalOpenPreference';

const host = vi.hoisted(() => ({ invoke: vi.fn(), desktop: true }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => host.desktop }));
const request = () => analyticalRequest('MPS', 'workspace', 'snapshot', 'SW', '2026-10-15');
const prepared: AnalyticalPrepared = { preparationId: 'exact-workbook', fileName: 'MPS.xlsx', expiresAtUtc: '2026-10-15T12:00:00Z' };
function deferred<T>() { let resolve!: (value: T) => void; let reject!: (error: Error) => void; const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no; }); return { promise, resolve, reject }; }
function port() { return { prepare: vi.fn<AnalyticalExportClient['prepare']>().mockResolvedValue(prepared), save: vi.fn<AnalyticalExportClient['save']>().mockResolvedValue({ outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true }), release: vi.fn<AnalyticalExportClient['release']>().mockResolvedValue(undefined), status: vi.fn<AnalyticalExportClient['status']>().mockResolvedValue({ outcome: 'saved', message: 'Workbook saved.', operationComplete: true }), open: vi.fn<AnalyticalExportClient['open']>().mockResolvedValue(undefined) }; }
function ui(client = port(), key = 'A') {
  const factory = () => client as unknown as AnalyticalExportClient;
  const tree = (contextKey: string) => <><AnalyticalSaveStatus /><AnalyticalExcelExport contextKey={contextKey} label="MPS" scopeLabel="Export includes all 2 parts" request={request} clientFactory={factory} /></>;
  return { client, tree, ...render(tree(key)) };
}
async function activate() { fireEvent.click(screen.getByRole('button', { name: 'Export' })); const format = screen.getByRole('menuitem', { name: /Excel workbook/ }); await act(async () => { fireEvent.click(format); }); }
beforeEach(async () => {
  host.desktop = true; host.invoke.mockReset(); host.invoke.mockImplementation(async (command: string) => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? prepared : command === 'component_mrp_save' ? { outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true } : false);
  await act(async () => rememberAnalyticalOpenPreference(false));
});
afterEach(async () => {
  await act(async () => {});
  cleanup(); const hook = renderHook(useAnalyticalExportStatus);
  act(() => { for (const operation of hook.result.current.operations) { recordAnalyticalSave(operation.id, { outcome: 'failed', message: 'Test cleanup', operationComplete: true }); dismissAnalyticalSave(operation.id); } });
  hook.unmount(); vi.useRealTimers();
});

describe('Stage 22 analytical pilot — mocked host, no filesystem/source access', () => {
  it('screen acquisition expiry blocks new preparation without releasing cancelled exact bytes', async () => {
    const client = port();
    const tree = (expired: boolean) => <><AnalyticalSaveStatus /><AnalyticalExcelExport contextKey="held-acquisition" label="Component MRP" scopeLabel="MRP shown" request={request} clientFactory={() => client as unknown as AnalyticalExportClient} prepareDisabledReason={expired ? 'Refresh MRP before exporting.' : undefined} /></>;
    const view = render(tree(false)); await activate(); await screen.findByRole('button', { name: 'Save again' });
    view.rerender(tree(true)); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled(); expect(client.release).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Save again' })); await waitFor(() => expect(client.save).toHaveBeenCalledTimes(2));
    expect(client.prepare).toHaveBeenCalledTimes(1); expect(client.save.mock.calls[1][0]).toBe(prepared);
  });
  it.each(['MPS', 'BOM'])('acquires a fresh client per Strict Mode setup and automatically saves %s once', async label => {
    render(<StrictMode><AnalyticalSaveStatus /><AnalyticalExcelExport contextKey={label} label={label} scopeLabel="Loaded scope" request={() => analyticalRequest(label, 'workspace', 'snapshot', 'SW', '2026-10-15')} /></StrictMode>);
    await activate(); await screen.findByRole('button', { name: 'Save again' });
    expect(host.invoke.mock.calls.filter(([name]) => name === 'component_mrp_generate')).toHaveLength(1);
    expect(host.invoke.mock.calls.filter(([name]) => name === 'component_mrp_save')).toHaveLength(1);
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_save', expect.objectContaining({ preparationId: prepared.preparationId, reportKind: 'Analytical', openAfterSave: false }));
    expect(screen.queryByText('Export context changed.')).not.toBeInTheDocument();
  });
  it('uses a themed portalled menu with arrow, checkbox, Escape and focus behavior', async () => {
    const user = userEvent.setup(); ui();
    const button = screen.getByRole('button', { name: 'Export' }); button.focus();
    await user.keyboard('{ArrowDown}');
    const menu = screen.getByRole('menu'); expect(menu.parentElement).toBe(document.body);
    expect(screen.getByRole('menuitem', { name: /Excel workbook/ })).toHaveFocus();
    await user.keyboard('{ArrowUp}'); expect(screen.getByRole('menuitemcheckbox')).toHaveFocus();
    await user.keyboard(' '); expect(screen.getByRole('menuitemcheckbox')).toHaveAttribute('aria-checked', 'true');
    await user.keyboard('{Home}'); expect(screen.getByRole('menuitem', { name: /Excel workbook/ })).toHaveFocus();
    await user.keyboard('{Escape}'); expect(screen.queryByRole('menu')).not.toBeInTheDocument(); expect(button).toHaveFocus();
    expect(document.querySelector('.analytical-excel-export input[type="checkbox"]')).toBeNull();
    await waitFor(() => expect(host.invoke).toHaveBeenCalledWith('component_mrp_remember_open_preference', { value: true }));
  });
  it('shows explicit disabled and desktop-only reasons without dispatch', () => {
    host.desktop = false; ui(); expect(screen.getByText(/requires the desktop application/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled(); expect(host.invoke).not.toHaveBeenCalledWith('component_mrp_begin');
  });
  it.each([['light', 'compact'], ['light', 'comfortable'], ['dark', 'compact'], ['dark', 'comfortable']])('keeps menu and feedback inside %s / %s theme inheritance but outside scrolling content', async (theme, density) => {
    const client = port();
    render(<div className="shell" data-theme={theme} data-density={density}><AnalyticalSaveStatus /><main className="shell__content"><AnalyticalExcelExport contextKey="themed" label="MPS" scopeLabel="All parts" request={request} clientFactory={() => client as unknown as AnalyticalExportClient} /></main></div>);
    fireEvent.click(screen.getByRole('button', { name: 'Export' }));
    const menu = screen.getByRole('menu'); expect(menu.parentElement).toBe(document.querySelector('.shell'));
    expect(menu.closest('.shell__content')).toBeNull(); expect(menu.closest('[data-theme]')).toHaveAttribute('data-theme', theme); expect(menu.closest('[data-density]')).toHaveAttribute('data-density', density);
    await act(async () => fireEvent.click(screen.getByRole('menuitem', { name: /Excel workbook/ })));
    const feedback = screen.getByRole('complementary', { name: 'Excel export activity' });
    expect(feedback.parentElement).toBe(document.querySelector('.shell')); expect(feedback.closest('.shell__content')).toBeNull();
  });
  it('deduplicates rapid format activation and retains the exact preparation on Save As cancellation', async () => {
    const { client } = ui(); fireEvent.click(screen.getByRole('button', { name: 'Export' }));
    const format = screen.getByRole('menuitem', { name: /Excel workbook/ }); fireEvent.click(format); fireEvent.click(format);
    await screen.findByRole('button', { name: 'Save again' });
    expect(client.prepare).toHaveBeenCalledTimes(1); expect(client.save).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole('button', { name: 'Save again' }));
    await waitFor(() => expect(client.save).toHaveBeenCalledTimes(2));
    expect(client.save.mock.calls[0][0]).toBe(prepared); expect(client.save.mock.calls[1][0]).toBe(prepared); expect(client.prepare).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('button', { name: 'Discard workbook' })).toBeInTheDocument();
  });
  it('cancellation releases preparation and a late successful reply cannot open Save As', async () => {
    const pending = deferred<AnalyticalPrepared>(); const client = port(); client.prepare.mockReturnValue(pending.promise);
    ui(client); await activate(); fireEvent.click(screen.getByRole('button', { name: 'Cancel preparation' }));
    expect(client.release).toHaveBeenCalled(); await act(async () => pending.resolve(prepared)); expect(client.save).not.toHaveBeenCalled();
  });
  it.each(['context', 'departure', 'disabled'])('rejects late automatic Save As after %s invalidation', async action => {
    const pending = deferred<AnalyticalPrepared>(); const client = port(); client.prepare.mockReturnValue(pending.promise);
    const view = ui(client); await activate();
    if (action === 'departure') view.rerender(<AnalyticalSaveStatus />);
    else if (action === 'disabled') view.rerender(<><AnalyticalSaveStatus /><AnalyticalExcelExport contextKey="A" label="MPS" scopeLabel="All parts" request={request} disabledReason="Refresh in progress" clientFactory={() => client as unknown as AnalyticalExportClient} /></>);
    else view.rerender(view.tree('B'));
    await act(async () => pending.resolve(prepared)); expect(client.save).not.toHaveBeenCalled(); expect(client.release).toHaveBeenCalled();
  });
  it('keeps cancelled bytes on an identical value key and snapshots preference per activation/retry', async () => {
    const pending = deferred<AnalyticalPrepared>(); const client = port(); client.prepare.mockReturnValue(pending.promise);
    const key = JSON.stringify({ snapshot: 'same', fiscal: { year: 2027 } }); const view = ui(client, key);
    act(() => rememberAnalyticalOpenPreference(true)); await activate();
    act(() => rememberAnalyticalOpenPreference(false));
    view.rerender(view.tree(JSON.stringify({ snapshot: 'same', fiscal: { year: 2027 } })));
    await act(async () => pending.resolve(prepared)); await screen.findByRole('button', { name: 'Save again' });
    expect(client.release).not.toHaveBeenCalled(); expect(client.save.mock.calls[0][1]).toBe(true);
    fireEvent.click(screen.getByRole('button', { name: 'Save again' })); await waitFor(() => expect(client.save).toHaveBeenCalledTimes(2));
    expect(client.save.mock.calls[1][1]).toBe(false); expect(client.prepare).toHaveBeenCalledTimes(1);
  });
  it('rechecks the request immediately before automatic continuation, including a clock rollover', async () => {
    const pending = deferred<AnalyticalPrepared>(); const client = port(); client.prepare.mockReturnValue(pending.promise); let date = '2026-10-17';
    render(<><AnalyticalSaveStatus /><AnalyticalExcelExport label="MPS" scopeLabel="All parts" contextKey="clock" clientFactory={() => client as unknown as AnalyticalExportClient} request={() => ({ ...request(), businessDate: date })} /></>);
    await activate(); date = '2026-10-18'; await act(async () => pending.resolve(prepared));
    expect(client.save).not.toHaveBeenCalled(); expect(await screen.findByText(/context changed during preparation/)).toBeInTheDocument();
  });
  it('retains unresolved recovery across departure and reconciles observationally without replay', async () => {
    const client = port(); client.save.mockRejectedValue(new Error('lost response')); const view = ui(client);
    await activate(); await screen.findByRole('button', { name: 'Check save status' });
    view.rerender(<><AnalyticalSaveStatus /><AnalyticalExcelExport label="BOM" scopeLabel="BOM rows" contextKey="B" request={request} /></>);
    expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    client.status.mockResolvedValueOnce({ outcome: 'saved', message: 'Saved; opening still pending', operationComplete: false });
    fireEvent.click(screen.getByRole('button', { name: 'Check save status' })); await screen.findByText('Saved; opening still pending');
    expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Check save status' })); await screen.findByText('Workbook saved.');
    expect(client.save).toHaveBeenCalledTimes(1); expect(client.status).toHaveBeenCalledTimes(2); expect(screen.getByRole('button', { name: 'Export' })).toBeEnabled();
  });
  it('keeps Saved/Open again after departure and ignores late original replies after reconciliation and opening', async () => {
    const pending = deferred<AnalyticalSaveResult>(); const client = port(); client.save.mockReturnValue(pending.promise);
    client.status.mockResolvedValue({ outcome: 'saved', message: 'Workbook saved, but could not be opened.', savedArtifactId: 'host-artifact', openFailed: true, operationComplete: true });
    const view = ui(client); await activate(); await screen.findByRole('button', { name: 'Check save status' });
    view.rerender(<AnalyticalSaveStatus />); fireEvent.click(screen.getByRole('button', { name: 'Check save status' }));
    const open = await screen.findByRole('button', { name: 'Open again' }); fireEvent.click(open); fireEvent.click(open);
    await screen.findByText(/opening request accepted/); expect(client.open).toHaveBeenCalledTimes(1); expect(client.open).toHaveBeenCalledWith('host-artifact');
    await act(async () => pending.resolve({ outcome: 'saved', message: 'Late original open failure', savedArtifactId: 'host-artifact', openFailed: true }));
    expect(screen.queryByText('Late original open failure')).not.toBeInTheDocument(); expect(screen.queryByRole('button', { name: 'Open again' })).not.toBeInTheDocument();
    expect(client.save).toHaveBeenCalledTimes(1); expect(client.prepare).toHaveBeenCalledTimes(1);
  });
  it('does not auto-dismiss the retained Save again and Discard actions', async () => {
    ui(); await activate(); await screen.findByRole('button', { name: 'Save again' });
    vi.useFakeTimers(); await act(async () => vi.advanceTimersByTimeAsync(5000));
    expect(screen.getByRole('button', { name: 'Save again' })).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Discard workbook' })).toBeInTheDocument();
  });
  it('preparation failure never starts Save As and presents a persistent failure reason', async () => {
    const client = port(); client.prepare.mockRejectedValue(new Error('Loaded snapshot is unavailable. Refresh required.'));
    ui(client); await activate(); expect(screen.getByText('Loaded snapshot is unavailable. Refresh required.')).toBeInTheDocument();
    expect(client.save).not.toHaveBeenCalled(); expect(screen.getByRole('button', { name: 'Dismiss' })).toBeInTheDocument();
  });
  it('a definite save failure retains the exact workbook for a deliberate new attempt', async () => {
    const client = port(); client.save.mockResolvedValueOnce({ outcome: 'failed', message: 'Destination unavailable. Nothing was saved.', operationComplete: true });
    ui(client); await activate(); expect(screen.getByText(/Destination unavailable.*exact prepared workbook/)).toBeInTheDocument();
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Save again' })));
    expect(client.save).toHaveBeenCalledTimes(2); expect(client.save.mock.calls[1][0]).toBe(prepared); expect(client.prepare).toHaveBeenCalledTimes(1);
  });
  it('failed opening retries remain Saved and do not regenerate or write again', async () => {
    const client = port(); client.save.mockResolvedValue({ outcome: 'saved', message: 'Workbook saved, but could not be opened.', savedArtifactId: 'saved-artifact', openFailed: true, operationComplete: true });
    client.open.mockRejectedValue(new Error('Host refused changed artifact'));
    ui(client); await activate(); await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Open again' })));
    expect(screen.getByText('MPS — Saved')).toBeInTheDocument(); expect(screen.getByText(/Workbook saved, but could not be opened. The host may refuse/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Open again' })).toBeEnabled(); expect(client.open).toHaveBeenCalledWith('saved-artifact'); expect(client.save).toHaveBeenCalledTimes(1); expect(client.prepare).toHaveBeenCalledTimes(1);
  });
  it('keeps an earlier opening recovery when a later export is cancelled', async () => {
    const client = port(); client.save.mockResolvedValueOnce({ outcome: 'saved', message: 'Saved, opening failed', savedArtifactId: 'first-artifact', openFailed: true, operationComplete: true });
    const view = ui(client); await activate(); view.rerender(view.tree('another-visit')); await activate();
    expect(screen.getByRole('button', { name: 'Save again' })).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Open again' })).toBeInTheDocument();
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Open again' })));
    expect(client.open).toHaveBeenCalledWith('first-artifact'); expect(client.save).toHaveBeenCalledTimes(2);
  });
  it('deduplicates status checks and leaves pending reconciliation observational', async () => {
    const client = port(); client.save.mockRejectedValue(new Error('Lost response'));
    const result = deferred<AnalyticalSaveResult>(); client.status.mockReturnValue(result.promise);
    ui(client); await activate(); const check = screen.getByRole('button', { name: 'Check save status' }); fireEvent.click(check); fireEvent.click(check);
    expect(client.status).toHaveBeenCalledTimes(1); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    await act(async () => result.resolve({ outcome: 'pending', message: 'Still waiting for the captured operation', operationComplete: false }));
    expect(screen.getByText('Still waiting for the captured operation')).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Check save status' })).toBeEnabled(); expect(client.save).toHaveBeenCalledTimes(1);
  });
});

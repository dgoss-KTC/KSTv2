import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ReportsPanel, type ReportSavePort } from './ReportsPanel';
import type { ComponentMrpPrepared } from '../api/componentMrpApi';

const prepared: ComponentMrpPrepared = { preparationId: 'one', fileName: 'ABC_MRP_SW_2026-09-24_01.xlsx',
  asOfDate: '2026-09-24', acquiredAtUtc: '2026-09-24T12:00:00Z', expiresAtUtc: '2026-09-24T12:30:00Z', warning: null,
  period: 'Week', includeUnconfirmed: true, forecastEndExclusive: '2027-03-21' };
const port = (): ReportSavePort => ({ loadOpenPreference: vi.fn().mockResolvedValue(false),
  rememberOpenPreference: vi.fn().mockResolvedValue(undefined), save: vi.fn().mockResolvedValue({ outcome: 'cancelled', message: 'Save cancelled.' }),
  open: vi.fn().mockResolvedValue(undefined) });
const host = vi.hoisted(() => ({ invoke: vi.fn(), listener: undefined as undefined | ((event: { payload: { operationId: string; result: { outcome: string; message: string; operationComplete: boolean; openFailed?: boolean; savedArtifactId?: string } } }) => void) }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('@tauri-apps/api/event', () => ({ listen: vi.fn(async (_name, callback) => { host.listener = callback; return () => {}; }) }));

describe('Reports Component MRP', () => {
  beforeEach(() => { localStorage.clear(); host.invoke.mockReset(); host.listener = undefined; });
  function deferred<T>() {
    let resolve!: (value: T) => void; let reject!: (error: Error) => void;
    const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no; });
    return { promise, resolve, reject };
  }
  type Outcome = Awaited<ReturnType<ReportSavePort['save']>>;
  async function desktopFixture() {
    const saves: { id: string; reply: ReturnType<typeof deferred<Outcome>> }[] = [];
    const statuses: { id: string; reply: ReturnType<typeof deferred<Outcome>> }[] = [];
    const opens: ReturnType<typeof deferred<void>>[] = [];
    host.invoke.mockImplementation((command: string, args?: { operationId: string }) => {
      if (command === 'component_mrp_save') { const reply = deferred<Outcome>(); saves.push({ id: args!.operationId, reply }); return reply.promise; }
      if (command === 'component_mrp_status') { const reply = deferred<Outcome>(); statuses.push({ id: args!.operationId, reply }); return reply.promise; }
      if (command === 'component_mrp_open') { const reply = deferred<void>(); opens.push(reply); return reply.promise; }
      return Promise.resolve(false);
    });
    const busy = vi.fn(); render(<ReportsPanel generate={vi.fn().mockResolvedValue(prepared)} onBusyChange={busy} />);
    await waitFor(() => expect(host.listener).toBeDefined());
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
    const event = (id: string, message: string, failed = false, complete = true) => act(() => host.listener!({ payload: { operationId: id, result: { outcome: 'saved', operationComplete: complete, openFailed: failed, savedArtifactId: `artifact-${id}`, message } } }));
    const startSave = async () => { const count = saves.length; fireEvent.click(screen.getByText('Save As…')); await waitFor(() => expect(saves).toHaveLength(count + 1)); return saves[count]; };
    return { saves, statuses, opens, busy, event, startSave };
  }
  it.each(['success', 'rejection'] as const)('late A %s and finally preserve B activity and unknown reconciliation', async late => {
    const f = await desktopFixture(); const a = await f.startSave(); f.event(a.id, 'A saved');
    const b = await f.startSave();
    await act(async () => { if (late === 'success') a.reply.resolve({ outcome: 'saved', message: 'obsolete A', operationComplete: true }); else a.reply.reject(new Error('obsolete A')); });
    expect(screen.getByText('Save As…')).toBeDisabled(); expect(f.busy).toHaveBeenLastCalledWith(true);
    await act(async () => b.reply.reject(new Error('lost B')));
    const statusMessage = 'Save response unavailable; commitment is unknown. Do not replay.';
    expect(screen.getByText(statusMessage)).toBeInTheDocument();
    fireEvent.click(screen.getByText('Check save status')); await waitFor(() => expect(f.statuses).toHaveLength(1));
    expect(f.statuses[0].id).toBe(b.id);
    await act(async () => f.statuses[0].reply.resolve({ outcome: 'saved', operationComplete: false, message: 'B saved; opening pending' }));
    expect(screen.getByText('B saved; opening pending')).toBeInTheDocument(); expect(screen.getByText('Check save status')).toBeEnabled();
    f.event(b.id, 'B terminal'); f.event(a.id, 'duplicate A'); f.event(b.id, 'duplicate B');
    expect(screen.getByText('B terminal')).toBeInTheDocument(); expect(f.saves).toHaveLength(2); expect(f.opens).toHaveLength(0);
  });
  it('late successful A cannot restore open failure or release an active explicit retry', async () => {
    const f = await desktopFixture(); const a = await f.startSave(); f.event(a.id, 'Workbook saved, but could not be opened.', true);
    fireEvent.click(screen.getByText('Retry Open')); await waitFor(() => expect(f.opens).toHaveLength(1));
    await act(async () => a.reply.resolve({ outcome: 'saved', openFailed: true, savedArtifactId: `artifact-${a.id}`, message: 'obsolete failure', operationComplete: true }));
    expect(screen.getByText('Retry Open')).toBeDisabled(); expect(f.busy).toHaveBeenLastCalledWith(true);
    await act(async () => f.opens[0].resolve());
    f.event(a.id, 'duplicate terminal failure', true);
    expect(screen.getByText('Workbook saved; opening request accepted.')).toBeInTheDocument(); expect(screen.queryByText('Retry Open')).not.toBeInTheDocument();
    expect(f.saves).toHaveLength(1); expect(f.opens).toHaveLength(1);
  });
  it.each(['success', 'rejection'] as const)('stale status %s cannot overwrite B or clear its busy action', async late => {
    const f = await desktopFixture(); const a = await f.startSave(); await act(async () => a.reply.reject(new Error('lost A')));
    fireEvent.click(screen.getByText('Check save status')); await waitFor(() => expect(f.statuses).toHaveLength(1));
    f.event(a.id, 'A terminal'); const b = await f.startSave();
    await act(async () => { if (late === 'success') f.statuses[0].reply.resolve({ outcome: 'failed', operationComplete: true, message: 'old status failure' }); else f.statuses[0].reply.reject(new Error('old status rejection')); });
    expect(screen.getByText('Save As…')).toBeDisabled(); expect(f.busy).toHaveBeenLastCalledWith(true);
    f.event(b.id, 'B latest terminal'); await act(async () => b.reply.resolve({ outcome: 'saved', message: 'late B', operationComplete: true }));
    expect(screen.getByText('B latest terminal')).toBeInTheDocument(); expect(screen.queryByText('old status failure')).not.toBeInTheDocument();
    expect(f.saves).toHaveLength(2); expect(f.opens).toHaveLength(0);
  });
  it('late successful A preserves B unknown recovery after B reply is lost', async () => {
    const f = await desktopFixture(); const a = await f.startSave(); f.event(a.id, 'A terminal'); const b = await f.startSave();
    await act(async () => b.reply.reject(new Error('lost B')));
    await act(async () => a.reply.resolve({ outcome: 'saved', operationComplete: true, message: 'obsolete A' }));
    expect(screen.getByText('Check save status')).toBeEnabled(); expect(screen.getByText('Save As…')).toBeDisabled();
    expect(screen.getByText('Save response unavailable; commitment is unknown. Do not replay.')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Check save status')); await waitFor(() => expect(f.statuses[0].id).toBe(b.id));
    await act(async () => f.statuses[0].reply.resolve({ outcome: 'cancelled', message: 'B cancelled', operationComplete: true }));
    expect(screen.getByText('B cancelled')).toBeInTheDocument(); expect(f.opens).toHaveLength(0);
  });
  it('completed Retry Open survives late successful save reply', async () => {
    const f = await desktopFixture(); const a = await f.startSave(); f.event(a.id, 'open failure', true);
    fireEvent.click(screen.getByText('Retry Open')); await waitFor(() => expect(f.opens).toHaveLength(1)); await act(async () => f.opens[0].resolve());
    await act(async () => a.reply.resolve({ outcome: 'saved', operationComplete: true, openFailed: true, savedArtifactId: `artifact-${a.id}`, message: 'obsolete failure' }));
    expect(screen.getByText('Workbook saved; opening request accepted.')).toBeInTheDocument(); expect(screen.queryByText('Retry Open')).not.toBeInTheDocument();
    expect(f.saves).toHaveLength(1); expect(f.opens).toHaveLength(1);
  });
  it('stale status rejection after terminal settlement preserves successful Retry Open', async () => {
    const f = await desktopFixture(); const a = await f.startSave(); await act(async () => a.reply.reject(new Error('lost')));
    fireEvent.click(screen.getByText('Check save status')); await waitFor(() => expect(f.statuses).toHaveLength(1));
    f.event(a.id, 'open failed', true); fireEvent.click(screen.getByText('Retry Open')); await waitFor(() => expect(f.opens).toHaveLength(1));
    await act(async () => f.opens[0].resolve()); await act(async () => f.statuses[0].reply.reject(new Error('late status')));
    expect(screen.getByText('Workbook saved; opening request accepted.')).toBeInTheDocument(); expect(screen.queryByText('Retry Open')).not.toBeInTheDocument();
    expect(screen.getByText('Save As…')).toBeEnabled(); expect(f.opens).toHaveLength(1); expect(f.saves).toHaveLength(1);
  });
  it('host event retains committed/open-pending and terminal result even when invoke response is lost', async () => {
    let rejectSave!: (reason: Error) => void; let operationId = '';
    host.invoke.mockImplementation((command: string, args?: { operationId: string }) => {
      if (command === 'component_mrp_save') { operationId = args!.operationId; return new Promise((_resolve, reject) => { rejectSave = reject; }); }
      return Promise.resolve(false);
    });
    const generate = vi.fn().mockResolvedValue(prepared);
    const view = render(<div><ReportsPanel generate={generate} /></div>);
    await waitFor(() => expect(host.listener).toBeDefined());
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled()); fireEvent.click(screen.getByText('Save As…'));
    await waitFor(() => expect(operationId).not.toBe(''));
    act(() => host.listener!({ payload: { operationId, result: { outcome: 'saved', operationComplete: false, savedArtifactId: 'artifact', message: 'Workbook saved; opening is in progress.' } } }));
    expect(screen.getByText('Workbook saved; opening is in progress.')).toBeInTheDocument(); expect(screen.getByText('Save As…')).toBeDisabled();
    view.rerender(<div hidden><ReportsPanel generate={generate} /></div>);
    act(() => host.listener!({ payload: { operationId, result: { outcome: 'saved', operationComplete: true, openFailed: true, savedArtifactId: 'artifact', message: 'Workbook saved, but could not be opened.' } } }));
    expect(screen.getByText('Retry Open')).toBeEnabled();
    await act(async () => { rejectSave(new Error('response lost')); });
    view.rerender(<div><ReportsPanel generate={generate} /></div>);
    expect(screen.getByText('Retry Open')).toBeInTheDocument(); expect(screen.queryByText('Check save status')).not.toBeInTheDocument();
    act(() => host.listener!({ payload: { operationId, result: { outcome: 'saved', operationComplete: false, message: 'late intermediate' } } }));
    expect(screen.queryByText('late intermediate')).not.toBeInTheDocument();
    expect(host.invoke.mock.calls.filter(([name]) => name === 'component_mrp_save')).toHaveLength(1);
    expect(host.invoke.mock.calls.filter(([name]) => name === 'component_mrp_open')).toHaveLength(0);
  });
  it.each([true, false])('retained hidden panel reconciles committed/open-pending then final failure=%s', async failure => {
    const savePort = port(); vi.mocked(savePort.save).mockRejectedValue(new Error('op-delayed'));
    savePort.status = vi.fn().mockResolvedValueOnce({ outcome: 'saved', operationComplete: false, savedArtifactId: 'artifact', message: 'Workbook saved; opening is in progress.' })
      .mockResolvedValue({ outcome: 'saved', operationComplete: true, openFailed: failure, savedArtifactId: 'artifact', message: failure ? 'Workbook saved, but could not be opened.' : 'Workbook saved; opening request accepted.' });
    const generate = vi.fn().mockResolvedValue(prepared);
    const view = render(<div hidden={false}><ReportsPanel generate={generate} savePort={savePort} /></div>);
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled()); fireEvent.click(screen.getByText('Save As…'));
    await screen.findByText('Check save status');
    view.rerender(<div hidden><ReportsPanel generate={generate} savePort={savePort} /></div>);
    view.rerender(<div hidden={false}><ReportsPanel generate={generate} savePort={savePort} /></div>);
    fireEvent.click(screen.getByText('Check save status')); await screen.findByText('Workbook saved; opening is in progress.');
    expect(screen.getByText('Save As…')).toBeDisabled(); expect(screen.getByText('Generate')).toBeDisabled();
    fireEvent.click(screen.getByText('Check save status')); await screen.findByText(failure ? 'Workbook saved, but could not be opened.' : 'Workbook saved; opening request accepted.');
    expect(screen.queryByText('Check save status')).not.toBeInTheDocument();
    if (failure) {
      view.rerender(<div hidden><ReportsPanel generate={generate} savePort={savePort} /></div>);
      view.rerender(<div hidden={false}><ReportsPanel generate={generate} savePort={savePort} /></div>);
      fireEvent.click(screen.getByText('Retry Open')); await waitFor(() => expect(savePort.open).toHaveBeenCalledTimes(1));
    } else expect(savePort.open).not.toHaveBeenCalled();
    expect(savePort.save).toHaveBeenCalledTimes(1); expect(savePort.status).toHaveBeenCalledTimes(2);
  });
  it('lost response reconciles saved state without replay or redispatch', async () => {
    const savePort = port(); vi.mocked(savePort.save).mockRejectedValue(new Error('op-1'));
    savePort.status = vi.fn().mockResolvedValue({ outcome: 'saved', savedArtifactId: 'artifact', message: 'Workbook saved.' });
    render(<ReportsPanel generate={vi.fn().mockResolvedValue(prepared)} savePort={savePort} />);
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled()); fireEvent.click(screen.getByText('Save As…'));
    await screen.findByText('Check save status'); fireEvent.click(screen.getByText('Check save status'));
    await screen.findByText('Workbook saved.'); expect(savePort.status).toHaveBeenCalledWith(vi.mocked(savePort.save).mock.calls[0][2]);
    expect(savePort.save).toHaveBeenCalledTimes(1); expect(savePort.open).not.toHaveBeenCalled();
    expect(screen.getByText('Save As…')).toBeEnabled();
  });
  it('unknown commitment preserves preparation but refuses blind save replay', async () => {
    const savePort = port(); vi.mocked(savePort.save).mockRejectedValue(new Error('response lost'));
    render(<ReportsPanel generate={vi.fn().mockResolvedValue(prepared)} savePort={savePort} />);
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
    fireEvent.click(screen.getByText('Save As…'));
    await screen.findByText('Save response unavailable; commitment is unknown. Do not replay.');
    expect(screen.getByText('Save As…')).toBeDisabled(); expect(screen.getByText(/Prepared:/)).toBeInTheDocument();
    expect(savePort.save).toHaveBeenCalledTimes(1);
  });
  it('uses agreed defaults and remembers site without introducing KS', async () => {
    const savePort = port(); render(<ReportsPanel savePort={savePort} />);
    expect(screen.getByLabelText('Site')).toHaveValue('SW'); expect(screen.getByLabelText('Horizon')).toHaveValue('26');
    expect(screen.getByLabelText('Include Unconfirmed POs')).toBeChecked(); expect(screen.getByLabelText('Period')).toHaveValue('Week');
    expect(screen.getByLabelText('Open workbook after saving')).not.toBeChecked();
    expect(screen.queryByRole('option', { name: 'KS' })).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Site'), { target: { value: 'KV' } });
    expect(localStorage.getItem('kst.componentMrp.site.v1')).toBe('KV');
    await waitFor(() => expect(savePort.loadOpenPreference).toHaveBeenCalled());
  });
  it('rejects late acquisition after an input changes and Generate always reacquires', async () => {
    let finish!: (value: ComponentMrpPrepared) => void;
    const generate = vi.fn().mockImplementationOnce(() => new Promise<ComponentMrpPrepared>(resolve => { finish = resolve; }))
      .mockResolvedValue(prepared);
    render(<ReportsPanel generate={generate} savePort={port()} />);
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'OTHER' } });
    await act(async () => finish(prepared));
    expect(screen.getByText('Save As…')).toBeDisabled();
    fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
    fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(generate).toHaveBeenCalledTimes(3));
    expect(generate.mock.calls[1][0]).toMatchObject({ part: 'OTHER', site: 'SW', horizonWeeks: 26 });
  });
  it('period change rejects late preparation and explicit exclusion stays false', async () => {
    let finish!: (value: ComponentMrpPrepared) => void;
    const generate = vi.fn().mockImplementationOnce(() => new Promise<ComponentMrpPrepared>(resolve => { finish = resolve; })).mockResolvedValue(prepared);
    render(<ReportsPanel generate={generate} savePort={port()} />);
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    fireEvent.change(screen.getByLabelText('Period'), { target: { value: 'Month' } }); await act(async () => finish(prepared));
    expect(screen.getByText('Save As…')).toBeDisabled(); fireEvent.click(screen.getByLabelText('Include Unconfirmed POs'));
    fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(generate).toHaveBeenCalledTimes(2));
    expect(generate.mock.calls[1][0]).toMatchObject({ period: 'Month', includeUnconfirmed: false });
  });
  it('cancel/failure retains identical prepared object and preference does not regenerate', async () => {
    const generate = vi.fn().mockResolvedValue(prepared); const savePort = port();
    render(<ReportsPanel generate={generate} savePort={savePort} />);
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
    fireEvent.click(screen.getByLabelText('Open workbook after saving'));
    fireEvent.click(screen.getByText('Save As…')); await screen.findByText('Save cancelled.');
    fireEvent.click(screen.getByText('Save As…')); await waitFor(() => expect(savePort.save).toHaveBeenCalledTimes(2));
    expect(vi.mocked(savePort.save).mock.calls[0][0]).toBe(prepared);
    expect(vi.mocked(savePort.save).mock.calls[1][0]).toBe(prepared);
    expect(vi.mocked(savePort.save).mock.calls[0][1]).toBe(true); expect(generate).toHaveBeenCalledTimes(1);
  });
  it('saved open failure stays saved; retry uses artifact after input invalidation and never resaves', async () => {
    const generate = vi.fn().mockResolvedValue(prepared); const savePort = port();
    vi.mocked(savePort.save).mockResolvedValue({ outcome: 'saved', savedArtifactId: 'artifact-1', message: 'Workbook saved, but could not be opened.' });
    render(<ReportsPanel generate={generate} savePort={savePort} />);
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
    fireEvent.click(screen.getByLabelText('Open workbook after saving')); fireEvent.click(screen.getByText('Save As…'));
    await screen.findByText('Workbook saved, but could not be opened.');
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'OTHER' } });
    fireEvent.click(screen.getByText('Retry Open')); await waitFor(() => expect(savePort.open).toHaveBeenCalledWith('artifact-1'));
    expect(savePort.save).toHaveBeenCalledTimes(1); expect(generate).toHaveBeenCalledTimes(1);
  });
  it('persistence failure retains session intent and does not discard preparation', async () => {
    const savePort = port(); vi.mocked(savePort.rememberOpenPreference).mockRejectedValue(new Error('disk unavailable'));
    render(<ReportsPanel generate={vi.fn().mockResolvedValue(prepared)} savePort={savePort} />);
    fireEvent.change(screen.getByLabelText('Part'), { target: { value: 'ABC' } }); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
    fireEvent.click(screen.getByLabelText('Open workbook after saving'));
    await screen.findByText('Save preference could not be remembered; the session choice is retained.');
    expect(screen.getByLabelText('Open workbook after saving')).toBeChecked(); expect(screen.getByText('Save As…')).toBeEnabled();
  });
});

import { act, fireEvent, render, screen } from '@testing-library/react';
import { StrictMode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { AnalyticalExcelExport } from './AnalyticalExcelExport';
import { analyticalRequest, AnalyticalExportClient, type AnalyticalPrepared } from '../api/analyticalExportApi';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';

const host = vi.hoisted(() => ({ invoke: vi.fn().mockResolvedValue(false) }));
vi.mock('@tauri-apps/api/core', () => host);
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => true }));
const request = () => analyticalRequest('MPS', 'workspace', 'snapshot', 'SW', '2026-10-14');
const prepared: AnalyticalPrepared = { preparationId: 'id', fileName: 'MPS.xlsx', expiresAtUtc: '2026-10-14T12:00:00Z' };
function port() {
  return { prepare: vi.fn().mockResolvedValue(prepared), save: vi.fn().mockResolvedValue({ outcome: 'cancelled', message: 'Save cancelled.' }),
    release: vi.fn().mockResolvedValue(undefined), status: vi.fn().mockResolvedValue({ outcome: 'saved', message: 'Workbook saved.', operationComplete: true }), open: vi.fn() };
}
describe('Analytical Excel captured preparation lifecycle (mocked host)', () => {
  it.each(['MPS', 'BOM'])('prepares %s with the real client under application Strict Mode', async kind => {
    host.invoke.mockImplementation(async (command: string) => {
      if (command === 'component_mrp_begin') return 1;
      if (command === 'component_mrp_generate') return prepared;
      return false;
    });
    render(<StrictMode><AnalyticalExcelExport contextKey={kind}
      request={() => analyticalRequest(kind, 'workspace', 'snapshot', 'SW', '2026-10-14')}
      clientFactory={() => new AnalyticalExportClient()} /></StrictMode>);
    fireEvent.click(screen.getByText('Export Excel'));
    expect(await screen.findByText('Save Excel As…')).toBeEnabled();
    expect(screen.queryByText('Export context changed.')).not.toBeInTheDocument();
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_generate', expect.objectContaining({
      reportKind: 'Analytical', analyticalRequest: expect.objectContaining({ kind }),
    }));
  });
  it('duplicate preparation clicks and late obsolete replies cannot expose Save', async () => {
    const first = port(); let reply!: (p: AnalyticalPrepared) => void;
    first.prepare.mockImplementation(() => new Promise(r => { reply = r; }));
    const next = port(); const factory = vi.fn().mockReturnValueOnce(first).mockReturnValue(next);
    const view = render(<AnalyticalExcelExport contextKey="A" request={request} clientFactory={factory} />);
    fireEvent.click(screen.getByText('Export Excel')); fireEvent.click(screen.getByText('Export Excel'));
    expect(first.prepare).toHaveBeenCalledTimes(1);
    view.rerender(<AnalyticalExcelExport contextKey="B" request={request} clientFactory={factory} />);
    await act(async () => reply(prepared));
    expect(first.release).toHaveBeenCalled(); expect(screen.queryByText('Save Excel As…')).not.toBeInTheDocument();
    fireEvent.click(screen.getByText('Export Excel')); await screen.findByText('Save Excel As…');
  });
  it('cancellation keeps prepared bytes for retry; unknown outcomes require status, never replay', async () => {
    const client = port();
    render(<AnalyticalExcelExport contextKey="A" request={request} clientFactory={() => client as unknown as AnalyticalExportClient} />);
    fireEvent.click(screen.getByText('Export Excel')); fireEvent.click(await screen.findByText('Save Excel As…'));
    await screen.findByText('Save cancelled.'); expect(screen.getByText('Save Excel As…')).toBeEnabled();
    client.save.mockRejectedValueOnce(new Error('lost reply')); fireEvent.click(screen.getByText('Save Excel As…'));
    fireEvent.click(await screen.findByText('Check Excel save status'));
    await screen.findByText('Workbook saved.'); expect(client.save).toHaveBeenCalledTimes(2);
    expect(screen.queryByText('Save Excel As…')).not.toBeInTheDocument();
  });
  it('cancelled preparation releases only its visit and permits a new preparation', async () => {
    const first = port(); first.prepare.mockImplementation(() => new Promise(() => {})); const second = port();
    const factory = vi.fn().mockReturnValueOnce(first).mockReturnValue(second);
    render(<AnalyticalExcelExport contextKey="A" request={request} clientFactory={factory} />);
    fireEvent.click(screen.getByText('Export Excel')); fireEvent.click(await screen.findByText('Cancel preparation'));
    expect(first.release).toHaveBeenCalled(); fireEvent.click(screen.getByText('Export Excel')); await screen.findByText('Save Excel As…');
  });
  it('keeps status reconciliation across workspace departure after an unknown save reply', async () => {
    const client = port(); client.save.mockRejectedValue(new Error('lost reply'));
    const factory = () => client as unknown as AnalyticalExportClient;
    const view = render(<><AnalyticalSaveStatus /><AnalyticalExcelExport contextKey="workspace-A" request={request} clientFactory={factory} /></>);
    fireEvent.click(screen.getByText('Export Excel')); fireEvent.click(await screen.findByText('Save Excel As…'));
    await screen.findByText('Check Excel save status');
    view.rerender(<><AnalyticalSaveStatus /><AnalyticalExcelExport contextKey="workspace-B" request={request} clientFactory={factory} /></>);
    expect(screen.getByText('Export Excel')).toBeDisabled();
    fireEvent.click(screen.getByText('Check captured Excel save status'));
    await screen.findByText('Workbook saved.'); expect(client.status).toHaveBeenCalledTimes(1);
    expect(screen.getByText('Export Excel')).toBeEnabled(); expect(client.save).toHaveBeenCalledTimes(1);
  });
});

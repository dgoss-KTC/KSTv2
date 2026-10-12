import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ReportsPanel, type ReportSavePort } from './ReportsPanel';
import type { OpenOrderReportPrepared } from '../api/openOrderReportApi';

vi.mock('@tauri-apps/api/core', () => ({ invoke: vi.fn(async () => undefined) }));
const prepared: OpenOrderReportPrepared = { outcome: 'prepared', preparationId: 'oor', fileName: 'Open-Orders_2026-10-12_01.xlsx', asOfDate: '2026-10-12', acquiredAtUtc: '2026-10-12T12:00:00Z', expiresAtUtc: '2026-10-12T12:30:00Z' };
const port = (): ReportSavePort => ({ loadOpenPreference: vi.fn(async () => false), rememberOpenPreference: vi.fn(async () => {}),
  save: vi.fn(async () => ({ outcome: 'cancelled' as const, message: 'Save cancelled.' })), open: vi.fn(async () => {}), discard: vi.fn(async () => {}) });
const switchOor = () => fireEvent.click(screen.getByRole('button', { name: 'Open Order Report' }));
const setSite = () => fireEvent.change(screen.getByLabelText('Site'), { target: { value: 'SW' } });

describe('Reports Open Order lifecycle', () => {
  beforeEach(() => localStorage.clear());
  it('requires non-date filter, exact-byte retries, noData and failure never retain readiness', async () => {
    const generate = vi.fn().mockResolvedValueOnce(prepared).mockResolvedValueOnce({ ...prepared, outcome: 'noData', preparationId: null, fileName: null }).mockRejectedValueOnce(new Error('source failed'));
    const savePort = port(); render(<ReportsPanel generateOor={generate} savePort={savePort} />); switchOor();
    expect(screen.getByText('Generate')).toBeDisabled();
    fireEvent.change(screen.getByLabelText('Due Date From'), { target: { value: '10-1-26' } });
    fireEvent.change(screen.getByLabelText('Due Date To'), { target: { value: '10.31.2026' } }); expect(screen.getByText('Generate')).toBeDisabled();
    setSite(); fireEvent.click(screen.getByLabelText('Unit Price')); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
    expect(generate.mock.calls[0][0]).toMatchObject({ site: 'SW', optionalFields: ['Unit Price'], dueDateFrom: '10-1-26' });
    fireEvent.click(screen.getByLabelText('Open workbook after saving')); fireEvent.click(screen.getByText('Save As…'));
    await screen.findByText('Save cancelled.'); fireEvent.click(screen.getByText('Save As…')); await waitFor(() => expect(savePort.save).toHaveBeenCalledTimes(2));
    expect(savePort.save).toHaveBeenLastCalledWith(prepared, true, expect.any(String), 'OpenOrderReport'); expect(generate).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByText('Generate')); await screen.findByText('No open orders found for the specified filters.'); expect(screen.getByText('Save As…')).toBeDisabled();
    fireEvent.click(screen.getByText('Generate')); await screen.findByText('source failed'); expect(screen.getByText('Save As…')).toBeDisabled();
  });
  it('all acquisition fields and columns invalidate, late completion and departure cannot resurrect readiness', async () => {
    let resolve!: (r: OpenOrderReportPrepared) => void;
    const generate = vi.fn().mockImplementationOnce(() => new Promise<OpenOrderReportPrepared>(r => { resolve = r; })).mockResolvedValue(prepared);
    const savePort = port(); const { rerender } = render(<ReportsPanel generateOor={generate} savePort={savePort} />); switchOor(); setSite();
    fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(generate).toHaveBeenCalledTimes(1));
    fireEvent.change(screen.getByRole('textbox', { name: 'Customer Name' }), { target: { value: 'Alpha%' } });
    await act(async () => resolve(prepared)); expect(screen.getByText('Save As…')).toBeDisabled();
    for (const label of ['Customer Number', 'Salesperson ID', 'Product Line From', 'Product Line To', 'IOS', 'Due Date From', 'Due Date To']) {
      fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
      fireEvent.change(screen.getByRole('textbox', { name: label }), { target: { value: label.includes('Date') ? '10/1' : '0001' } }); expect(screen.getByText('Save As…')).toBeDisabled();
    }
    fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
    fireEvent.click(screen.getByLabelText('Allocated')); expect(screen.getByText('Save As…')).toBeDisabled();
    fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
    rerender(<ReportsPanel active={false} generateOor={generate} savePort={savePort} />); await waitFor(() => expect(screen.getByText('Save As…')).toBeDisabled());
    expect(savePort.discard).toHaveBeenCalled(); rerender(<ReportsPanel generateOor={generate} savePort={savePort} />); expect(screen.getByText('Save As…')).toBeDisabled();
  });
  it('unknown departed Open Order operation reconciles its original kind without replay or settled Open retry', async () => {
    const savePort = port(); savePort.save = vi.fn().mockRejectedValue(new Error('lost'));
    savePort.status = vi.fn().mockResolvedValue({ outcome: 'saved', message: 'saved', savedArtifactId: 'a', openFailed: true, operationComplete: true });
    render(<ReportsPanel generateOor={vi.fn().mockResolvedValue(prepared)} savePort={savePort} />); switchOor(); setSite(); fireEvent.click(screen.getByText('Generate'));
    await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled()); fireEvent.click(screen.getByText('Save As…'));
    await screen.findByText('Save response unavailable; commitment is unknown. Do not replay.');
    const id = vi.mocked(savePort.save).mock.calls[0][2]; fireEvent.click(screen.getByRole('button', { name: 'Component MRP' }));
    fireEvent.click(screen.getByText('Check save status')); await waitFor(() => expect(savePort.status).toHaveBeenCalledWith(id, 'OpenOrderReport'));
    expect(savePort.save).toHaveBeenCalledTimes(1); expect(savePort.open).not.toHaveBeenCalled(); expect(screen.queryByText('Retry Open')).not.toBeInTheDocument();
    expect(screen.getByText('Save As…')).toBeDisabled();
  });
});

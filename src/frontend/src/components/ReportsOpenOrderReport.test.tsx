import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ReportsPanel } from './ReportsPanel';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';
import { reportFixture, deferred, exportWorkbook, clearExportTests } from '../reports/reportTestSupport';
const host = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => true }));
const prepared = { ...reportFixture, outcome: 'prepared' };
beforeEach(() => { localStorage.clear(); host.invoke.mockReset(); host.invoke.mockImplementation(async (n: string) => n === 'component_mrp_begin' ? 9 : n === 'component_mrp_save' ? { outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true } : false); });
afterEach(clearExportTests);
const select = () => fireEvent.click(screen.getByRole('button', { name: 'Open Order Report' }));
const site = () => fireEvent.change(screen.getByLabelText('Site'), { target: { value: 'SW' } });
describe('Standalone Open Order Report shared export', () => {
  it('requires non-date filter, retains flexible dates, grouped columns and exact retry', async () => {
    const generate = vi.fn().mockResolvedValue(prepared); render(<><AnalyticalSaveStatus /><ReportsPanel generateOor={generate} /></>); select();
    fireEvent.change(screen.getByLabelText('Due Date From'), { target: { value: '10-1-26' } }); fireEvent.change(screen.getByLabelText('Due Date To'), { target: { value: '10.31.2026' } }); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    site(); fireEvent.click(screen.getByRole('button', { name: /^Columns/ })); expect(screen.getByRole('checkbox', { name: 'Due' })).toBeDisabled();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Unit Price' })); fireEvent.click(screen.getByRole('button', { name: 'Done' })); await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' });
    expect(generate.mock.calls[0][0]).toMatchObject({ site: 'SW', optionalFields: ['Unit Price'], dueDateFrom: '10-1-26', dueDateTo: '10.31.2026' });
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Save again' }))); expect(generate).toHaveBeenCalledTimes(1); expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_save')).toHaveLength(2);
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_save', expect.objectContaining({ preparationId: prepared.preparationId, reportKind: 'OpenOrderReport' }));
    expect(JSON.parse(localStorage.getItem('kst.openOrderReport.optionalFields.v1')!)).toEqual(['Unit Price']);
  });
  it.each(['Customer Name', 'Customer Number', 'Salesperson ID', 'Product Line From', 'Product Line To', 'IOS', 'Due Date From', 'Due Date To'])('invalidates late preparation on %s edits', async label => {
    const pending = deferred<typeof prepared>(); const generate = vi.fn().mockReturnValue(pending.promise); render(<><AnalyticalSaveStatus /><ReportsPanel generateOor={generate} /></>); select(); site(); await exportWorkbook();
    fireEvent.change(screen.getByRole('textbox', { name: label }), { target: { value: label.includes('Date') ? '10/1' : '0001' } }); await act(async () => pending.resolve(prepared));
    expect(generate.mock.calls[0][1].aborted).toBe(true); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false);
  });
  it('keeps noData and actual acquisition failure distinct; input edits clear the empty footer', async () => {
    const generate = vi.fn().mockResolvedValueOnce({ ...prepared, outcome: 'noData', preparationId: null, fileName: null }).mockRejectedValueOnce(new Error('source failed'));
    render(<><AnalyticalSaveStatus /><ReportsPanel generateOor={generate} /></>); select(); site(); await exportWorkbook(); expect(screen.getAllByText('No open orders found for the specified filters.')).toHaveLength(2);
    fireEvent.change(screen.getByLabelText('Customer Number'), { target: { value: '001' } }); expect(screen.queryByText('No open orders found for the specified filters.')).toBeNull();
    await exportWorkbook(); await screen.findByText('source failed'); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false);
  });
  it('reconciles original OpenOrderReport kind after departure without replay', async () => {
    host.invoke.mockImplementation(async (n: string) => n === 'component_mrp_begin' ? 9 : n === 'component_mrp_save' ? Promise.reject(new Error('lost')) : n === 'component_mrp_status' ? { outcome: 'saved', message: 'Workbook saved.', operationComplete: true } : false);
    render(<><AnalyticalSaveStatus /><ReportsPanel generateOor={vi.fn().mockResolvedValue(prepared)} /></>); select(); site(); await exportWorkbook();
    await screen.findByRole('button', { name: 'Check save status' }); fireEvent.click(screen.getByRole('button', { name: 'Part Info & MRP' })); await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Check save status' })));
    await waitFor(() => expect(host.invoke).toHaveBeenCalledWith('component_mrp_status', expect.objectContaining({ reportKind: 'OpenOrderReport' }))); expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_save')).toHaveLength(1);
  });
});

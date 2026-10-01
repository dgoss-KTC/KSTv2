import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { act, useState } from 'react';
import { OpenOrdersPanel } from './OpenOrdersPanel';
import type { OpenOrdersResponseDto } from '../api/client';
import type { OrderLine } from '../openOrders/report';
import { ApiError } from '../api/client';

const get = vi.fn(); const refresh = vi.fn(); const exportReport = vi.fn(); const save = vi.fn(); const restoreDraft = vi.fn(); const draftPresence = vi.fn(); const saveDraft = vi.fn(); const deleteDraft = vi.fn();
vi.mock('../api/tauri-bridge', () => ({ resolveBackendBaseUrl: () => Promise.resolve('http://localhost'), isRunningInTauri: () => false }));
vi.mock('../api/client', async (original) => ({ ...(await original<typeof import('../api/client')>()),
  ApiClient: class { getOpenOrders = get; refreshOpenOrders = refresh; exportOpenOrdersReport = exportReport;
    restoreOpenOrdersDraft = restoreDraft; getOpenOrdersDraftPresence = draftPresence; saveOpenOrdersDraft = saveDraft; deleteOpenOrdersDraft = deleteDraft; },
}));
vi.mock('../longTermShortages/saveLongTermShortagesWorkbook', () => ({ saveLongTermShortagesWorkbook: (...args: unknown[]) => save(...args) }));

function response(count: number, isStale = false): OpenOrdersResponseDto {
  const template: OrderLine = { key: { domain: 'D', salesOrder: 'SO-1', line: 1 }, itemNumber: 'P-1', site: 'SW', purchaseOrder: 'PO', stat: 'A',
    shippedQty: 1, sourceValues: { dueDate: '2027-01-01', performDate: null, requiredDate: null, dockDate: null, orderQty: 3, price: '0.125' },
    planningValues: { dueDate: '2027-01-01', performDate: null, requiredDate: null, dockDate: null, orderQty: '3', price: '0.125' }, shippedQtyText: '1',
    open: 2, extPrice: '0.250', unitPrice: 0, allocated: null, customer: 'C1', customerName: 'Acme', salesperson: 'SP',
    customerPart: null, ios: 'IOS', lineComments: '', lineHold: null, partials: null, picked: null, plnr: null, prodStat: null,
    productLine: 'B', qaHold: null, remarks: null, revision: null, shipAcct: null, shipTo: null, shipVia: null,
    siteQoh: null, soHoldStatus: null, soType: null, consignment: true };
  return { workspaceId: 'A', site: 'SW', mpsSnapshotId: 'snap-1', openOrdersSnapshotId: 'report-1', acquiredAtUtc: '2026-09-30T10:00:00Z',
    isStale, warning: isStale ? 'Source unavailable' : null,
    lines: Array.from({ length: count }, (_, i) => ({ ...template, key: { ...template.key, salesOrder: `SO-${i + 1}` } })) };
}

function applyTextFilter(type: string, value: string) {
  fireEvent.change(screen.getByRole('combobox', { name: 'Filter' }), { target: { value: type } });
  const name = { customerName: 'Customer Name contains', customer: 'Customer # contains', salesperson: 'Salesperson contains', ios: 'IOS contains', so: 'SO contains', po: 'PO contains', itemNumber: 'Item Number contains' }[type]!;
  fireEvent.change(screen.getByRole('searchbox', { name: `${name} value` }), { target: { value } });
  fireEvent.click(screen.getByRole('button', { name: `Apply ${name} filter` }));
}

beforeEach(() => { get.mockReset(); refresh.mockReset(); exportReport.mockReset(); draftPresence.mockReset(); save.mockReset(); restoreDraft.mockReset(); saveDraft.mockReset(); deleteDraft.mockReset();
  draftPresence.mockResolvedValue({ exists: false });
  restoreDraft.mockResolvedValue({ exists: false, restored: false, warning: null, freshReport: null, rows: [] });
  saveDraft.mockResolvedValue({ exists: true, restored: true, warning: null, freshReport: null, rows: [] }); deleteDraft.mockResolvedValue(undefined);
  window.localStorage.clear(); });
afterEach(() => vi.useRealTimers());

describe('Customer Open Orders panel', () => {
  it('stages changed rows across filters and pages, keeps Report Mode layout, previews exact raw-price extension and supports undo and confirmed clear', async () => {
    get.mockResolvedValue(response(101));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByText(/101 scoped lines/);
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    expect(screen.getByRole('columnheader', { name: /Perform Date/ })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: /^Price / })).toBeInTheDocument();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: /3.*Original: 2/ })).toBeInTheDocument(); // Open 4 - 1; extension is optional in Plan Mode
    fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    expect(screen.getByText(/1 outside current filter\/page view/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    expect(screen.getAllByRole('columnheader')).toHaveLength(9);
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear All' }));
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear All' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirm Clear All' }));
    expect(screen.getByText(/0 changed across workspace/)).toBeInTheDocument();
  }, 60_000);

  it('remembers Plan column order and optional Ext Price independently for each workspace, and resets only Plan', async () => {
    get.mockResolvedValue(response(1));
    const { rerender } = render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    const headers = () => within(screen.getByRole('table')).getAllByRole('columnheader').map(h => h.textContent?.replace(/[‹›]/g, '').trim());
    expect(headers().slice(0, 11)).toEqual(['SO', 'PO', 'Line', 'Item Number', 'Open', 'Due Date', 'Perform Date', 'Required Date', 'Dock Date', 'Order Qty', 'Price']);
    expect(headers().slice(11)).toEqual(['Reason Code', 'Actions']);
    fireEvent.click(screen.getByText(/Plan columns \(11 visible\)/));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Ext Price' }));
    fireEvent.click(screen.getByRole('button', { name: 'Move Ext Price Left' }));
    expect(headers()).toContain('Ext Price');
    expect(screen.getByText('0.25')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    expect(headers().slice(0, 9)).toEqual(['Due Date', 'SO', 'PO', 'Line', 'Item Number', 'Site', 'Open', 'Status', 'Ext Price']);
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    expect(headers()).toContain('Ext Price');
    rerender(<OpenOrdersPanel key="B" assignmentId="B" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    expect(headers()).not.toContain('Ext Price');
    rerender(<OpenOrdersPanel key="A-again" assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    expect(headers()).toContain('Ext Price');
    fireEvent.click(screen.getByText(/Plan columns \(12 visible\)/));
    fireEvent.click(screen.getByRole('button', { name: 'Reset columns to default' }));
    expect(headers()).not.toContain('Ext Price');
  });

  it('restores only after fresh validation, retains conflicts, and allows save-off without dropping in-memory edits', async () => {
    const data = response(1);
    get.mockResolvedValue(data);
    const original = data.lines[0].planningValues;
    const staged = { key: data.lines[0].key, site: 'SW', itemNumber: 'P-1', original, proposed: { ...original, orderQty: '4' }, reasonCode: 'Planning' };
    draftPresence.mockResolvedValue({ exists: true });
    restoreDraft.mockResolvedValue({ exists: true, restored: true, warning: null, freshReport: { ...data, openOrdersSnapshotId: 'fresh-2',
      lines: [{ ...data.lines[0], planningValues: { ...original, price: '0.2' } }] }, rows: [{ proposal: staged, issues: ['Source identity or original editable values changed.'] }] });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    expect(await screen.findByRole('button', { name: 'Restore saved draft and continue saving' })).toBeInTheDocument();
    expect(restoreDraft).toHaveBeenCalledWith('A', 'snap-1');
    fireEvent.click(screen.getByRole('button', { name: 'Restore saved draft and continue saving' }));
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    expect(screen.getByText(/1 conflicts/)).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Save Draft' })).toBeChecked();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Save Draft' }));
    await waitFor(() => expect(deleteDraft).toHaveBeenCalledWith('A'));
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    vi.restoreAllMocks();
  });

  it('shows compact draft progress only for an existing draft and clears it at every terminal state', async () => {
    const data = response(1); get.mockResolvedValue(data);
    let finish: (result: unknown) => void = () => {};
    draftPresence.mockResolvedValueOnce({ exists: false }).mockResolvedValueOnce({ exists: true }).mockResolvedValueOnce({ exists: true });
    restoreDraft.mockImplementationOnce(() => new Promise(resolve => { finish = resolve; }))
      .mockRejectedValueOnce(new Error('synthetic offline'));
    const { rerender } = render(<OpenOrdersPanel key="A" assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    await waitFor(() => expect(draftPresence).toHaveBeenCalledTimes(1));
    expect(screen.queryByText('Checking saved draft…')).not.toBeInTheDocument();
    expect(restoreDraft).not.toHaveBeenCalled();
    rerender(<OpenOrdersPanel key="B" assignmentId="B" snapshotId="snap-1" />);
    expect(await screen.findByText('Checking saved draft…')).toBeInTheDocument();
    await act(async () => finish({ exists: true, restored: true, warning: null, freshReport: data, rows: [] }));
    expect(screen.queryByText('Checking saved draft…')).not.toBeInTheDocument();
    rerender(<OpenOrdersPanel key="C" assignmentId="C" snapshotId="snap-1" />);
    expect(await screen.findByText(/Draft check failed/)).toBeInTheDocument();
    expect(screen.queryByText('Checking saved draft…')).not.toBeInTheDocument();
  });

  it('clears obsolete draft progress after an MPS scope change while preserving the new scope', async () => {
    get.mockResolvedValue(response(1)); draftPresence.mockResolvedValue({ exists: true });
    let finish: (value: unknown) => void = () => {};
    restoreDraft.mockImplementationOnce(() => new Promise(resolve => { finish = resolve; }))
      .mockResolvedValueOnce({ exists: false, restored: false, warning: null, freshReport: null, rows: [] });
    const { rerender } = render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    expect(await screen.findByText('Checking saved draft…')).toBeInTheDocument();
    rerender(<OpenOrdersPanel assignmentId="A" snapshotId="snap-2" />);
    await waitFor(() => expect(draftPresence).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(screen.queryByText('Checking saved draft…')).not.toBeInTheDocument());
    await act(async () => finish({ exists: true, restored: true, warning: null, freshReport: response(1), rows: [] }));
    expect(screen.queryByRole('button', { name: /Restore saved draft/ })).not.toBeInTheDocument();
  });

  it('shows a retryable message and blocks Save Draft when draft presence cannot be checked', async () => {
    get.mockResolvedValue(response(1));
    draftPresence.mockRejectedValueOnce(new Error('synthetic file check')).mockResolvedValueOnce({ exists: false });
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    expect(await screen.findByText(/Could not check for a saved draft/)).toBeInTheDocument();
    expect(screen.queryByText('Checking saved draft…')).not.toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Save Draft' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Retry draft check' }));
    await waitFor(() => expect(draftPresence).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(screen.getByRole('checkbox', { name: 'Save Draft' })).toBeEnabled());
  });

  it('types complete dates in one field, preserves partial/invalid text, normalizes no-ops and supports Tab/Enter', async () => {
    get.mockResolvedValue(response(1));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table'); fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    const due = screen.getByRole('textbox', { name: 'Due Date proposal SO-1/1' });
    expect(due).toHaveAttribute('type', 'text');
    fireEvent.change(due, { target: { value: '01/01/2027' } });
    expect(screen.getByText(/0 changed across workspace/)).toBeInTheDocument();
    fireEvent.change(due, { target: { value: '1/8/' } });
    expect(due).toHaveValue('1/8/');
    expect(screen.queryByText(/Enter a valid M\/d\/yyyy/)).not.toBeInTheDocument();
    fireEvent.keyDown(due, { key: 'Enter' });
    expect(screen.getByText(/Enter a valid M\/d\/yyyy/)).toBeInTheDocument();
    fireEvent.change(due, { target: { value: '2/29/2027' } });
    expect(due).toHaveValue('2/29/2027');
    expect(screen.getByText(/0 changed across workspace/)).toBeInTheDocument();
    fireEvent.change(due, { target: { value: '2/29/2028' } });
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    fireEvent.keyDown(due, { key: 'Enter' });
    expect(screen.getByRole('textbox', { name: 'Perform Date proposal SO-1/1' })).toHaveFocus();
    fireEvent.change(due, { target: { value: '' } });
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    expect(due).toHaveValue('');
    fireEvent.change(due, { target: { value: '1/1/2027' } });
    expect(screen.getByText(/0 changed across workspace/)).toBeInTheDocument();
  });

  it('does not save incomplete date text as a blank or stale proposal while Save Draft is enabled', async () => {
    get.mockResolvedValue(response(1));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table'); fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Save Draft' }));
    await waitFor(() => expect(saveDraft).toHaveBeenCalledTimes(1));
    const due = screen.getByRole('textbox', { name: 'Due Date proposal SO-1/1' });
    fireEvent.change(due, { target: { value: '1/8/' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    expect(due).toHaveValue('1/8/');
    expect(screen.getByText(/Not export-ready/)).toBeInTheDocument();
    expect(saveDraft).toHaveBeenCalledTimes(1);
    fireEvent.change(due, { target: { value: '1/8/2027' } });
    await waitFor(() => expect(saveDraft).toHaveBeenCalledTimes(2));
    expect(saveDraft.mock.calls[1][1].proposals[0].proposed.dueDate).toBe('2027-01-08');
    expect(saveDraft.mock.calls[1][1].proposals[0].proposed.orderQty).toBe('4');
  });

  it('preserves unsaved edits on mode switching and blocks Report Mode actions while planning', async () => {
    const first = response(1);
    get.mockResolvedValue(first);
    refresh.mockResolvedValue({ ...first, openOrdersSnapshotId: 'report-2', lines: [{ ...first.lines[0], planningValues: { ...first.lines[0].planningValues, price: '2.5' } }] });
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    expect(screen.getByRole('button', { name: 'Refresh report' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Export filtered rows to XLSX' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    expect(refresh).not.toHaveBeenCalled(); expect(exportReport).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    expect(screen.getByRole('button', { name: 'Refresh report' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Export filtered rows to XLSX' })).toBeEnabled();
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    await waitFor(() => expect(screen.getByText(/1 conflicts/)).toBeInTheDocument());
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('4');
    fireEvent.click(screen.getByText('Issues'));
    expect(screen.getByText(/Original values or source identity changed/)).toBeInTheDocument();
  });

  it('shows loaded empty distinctly and does not fetch without MPS', async () => {
    const { rerender } = render(<OpenOrdersPanel assignmentId="A" snapshotId={null} />);
    expect(screen.getByText(/Load the MPS dashboard/)).toBeInTheDocument(); expect(get).not.toHaveBeenCalled();
    get.mockResolvedValue(response(0));
    rerender(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    expect(await screen.findByText(/No open orders in this workspace scope/)).toBeInTheDocument();
  });

  it('pages large synthetic results without truncating export; filters hidden fields, reorders using keyboard and isolates workspaces', async () => {
    get.mockResolvedValue(response(235)); exportReport.mockResolvedValue({ blob: new Blob(), fileName: 'Open.xlsx' }); save.mockResolvedValue({ kind: 'saved', fileName: 'Open.xlsx' });
    const { rerender } = render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByText(/235 scoped lines/);
    expect(screen.getAllByRole('row')).toHaveLength(101);
    fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    expect(screen.getByText(/Rows 101–200 of 235/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    expect(screen.getByText(/Rows 201–235 of 235/)).toBeInTheDocument();
    fireEvent.click(screen.getByText(/Columns \(9 visible\)/));
    fireEvent.click(screen.getByRole('button', { name: 'Move PO Left' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Due Date' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Customer Name' }));
    const drag = { dataTransfer: { setData: vi.fn(), getData: () => 'customerName', effectAllowed: '' } };
    const headers = within(screen.getByRole('table'));
    fireEvent.dragStart(headers.getByRole('columnheader', { name: /Customer Name/ }), drag);
    fireEvent.dragOver(headers.getByRole('columnheader', { name: /^SO / }), drag);
    fireEvent.drop(headers.getByRole('columnheader', { name: /^SO / }), drag);
    expect(headers.getAllByRole('columnheader')[1]).toHaveTextContent('Customer Name');
    fireEvent.click(screen.getByRole('button', { name: 'Reset columns to default' }));
    fireEvent.click(screen.getByRole('button', { name: 'Move PO Left' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Due Date' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Customer Name' }));
    fireEvent.change(screen.getByRole('searchbox', { name: 'Customer Name contains value' }), { target: { value: 'acm' } });
    expect(screen.getByText(/235 matching filters/)).toBeInTheDocument(); // draft is not applied
    fireEvent.click(screen.getByRole('button', { name: 'Apply Customer Name contains filter' }));
    expect(screen.getByRole('button', { name: 'Remove Customer Name contains filter' })).toBeInTheDocument();
    expect(screen.getByText(/235 matching filters/)).toBeInTheDocument();
    expect(screen.getByText(/Rows 1–100 of 235/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    await waitFor(() => expect(exportReport).toHaveBeenCalledWith('A', expect.objectContaining({ lineKeys: expect.arrayContaining([{ domain: 'D', salesOrder: 'SO-235', line: 1 }]), columns: ['po', 'order', 'line', 'itemNumber', 'site', 'open', 'stat', 'extPrice', 'customerName'] })));
    expect(exportReport.mock.calls[0][1].lineKeys).toHaveLength(235);
    rerender(<OpenOrdersPanel key="B" assignmentId="B" snapshotId="snap-1" />);
    await waitFor(() => expect(get).toHaveBeenCalledWith('B', 'snap-1'));
    await waitFor(() => expect(within(screen.getByRole('table')).getAllByRole('columnheader')[0]).toHaveTextContent('Due Date'));
  }, 15_000);

  it('marks failed refresh stale, retries and reports cancelled or failed saves recoverably', async () => {
    get.mockResolvedValue(response(1)); refresh.mockResolvedValueOnce(response(1, true)).mockResolvedValueOnce(response(1));
    exportReport.mockResolvedValue({ blob: new Blob(), fileName: 'Open.xlsx' }); save.mockResolvedValueOnce({ kind: 'cancelled' }).mockRejectedValueOnce(new Error('disk'));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    expect(await screen.findByText(/STALE report acquired/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    expect(await screen.findByText(/Save cancelled/)).toBeInTheDocument();
    expect(screen.queryByText(/^Saved /)).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    expect(await screen.findByText(/Workbook export or save failed/)).toBeInTheDocument();
    expect(screen.getByRole('status', { name: '' })).toBeInTheDocument();
    save.mockResolvedValueOnce({ kind: 'saved', fileName: 'Recovered.xlsx' });
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    expect(await screen.findByText('Saved Recovered.xlsx')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    await waitFor(() => expect(screen.queryByText(/STALE report acquired/)).not.toBeInTheDocument());
  });

  it('keeps a failed refresh explicitly not current and never shows an old report under a changed MPS snapshot', async () => {
    get.mockResolvedValue(response(1)); refresh.mockRejectedValue(new ApiError(503, 'synthetic', 'Unavailable'));
    const { rerender } = render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    expect(await screen.findByText(/Previous report is retained but not current/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Export filtered rows to XLSX' })).toBeDisabled();
    get.mockRejectedValueOnce(new ApiError(409, 'synthetic', 'Snapshot changed'));
    rerender(<OpenOrdersPanel assignmentId="A" snapshotId="snap-2" />);
    expect(await screen.findByText(/MPS snapshot changed/)).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('applies, edits and removes one of each text filter without changing other chips or querying again', async () => {
    const data = response(3);
    data.lines[0] = { ...data.lines[0], customerName: 'Acme East', customer: 'C1', salesperson: 'S1', ios: 'I1' };
    data.lines[1] = { ...data.lines[1], customerName: 'Acme West', customer: 'C2', salesperson: 'S2', ios: 'I1' };
    data.lines[2] = { ...data.lines[2], customerName: null, customer: null, salesperson: null, ios: null };
    get.mockResolvedValue(data);
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByText(/3 scoped lines/);
    applyTextFilter('customerName', 'Acme');
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    applyTextFilter('customer', 'C1');
    applyTextFilter('salesperson', 'S1');
    applyTextFilter('ios', 'I1');
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /^Remove .* filter$/ })).toHaveLength(4);
    fireEvent.click(screen.getByRole('button', { name: /Edit Customer # contains filter/ }));
    expect(screen.getByRole('searchbox', { name: 'Customer # contains value' })).toHaveValue('C1');
    fireEvent.change(screen.getByRole('searchbox', { name: 'Customer # contains value' }), { target: { value: 'C2' } });
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument(); // still applied C1
    fireEvent.submit(screen.getByRole('form', { name: 'Add or edit report filter' })); // Enter
    expect(screen.getByText(/0 matching filters/)).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /^Remove .* filter$/ })).toHaveLength(4);
    fireEvent.click(screen.getByRole('button', { name: 'Remove Salesperson contains filter' }));
    fireEvent.click(screen.getByRole('button', { name: 'Remove IOS contains filter' }));
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Remove Customer Name contains filter' }));
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Remove Customer # contains filter' }));
    expect(screen.getByText(/3 matching filters/)).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1);
  });

  it('validates empty and range inputs, applies inclusive product/date bounds, filters export rows, and formats display without changing source decimals', async () => {
    const data = response(4);
    data.lines[0] = { ...data.lines[0], productLine: 'A', sourceValues: { ...data.lines[0].sourceValues, dueDate: '2027-01-01' } };
    data.lines[1] = { ...data.lines[1], productLine: 'B', sourceValues: { ...data.lines[1].sourceValues, dueDate: '2027-01-02' } };
    data.lines[2] = { ...data.lines[2], productLine: 'C', sourceValues: { ...data.lines[2].sourceValues, dueDate: '2027-01-03' } };
    data.lines[3] = { ...data.lines[3], productLine: null, sourceValues: { ...data.lines[3].sourceValues, dueDate: null } };
    get.mockResolvedValue(data); exportReport.mockResolvedValue({ blob: new Blob(), fileName: 'Open.xlsx' }); save.mockResolvedValue({ kind: 'saved', fileName: 'Open.xlsx' });
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByText(/4 scoped lines/);
    expect(screen.getAllByText('1/1/2027').length).toBeGreaterThan(0);
    expect(screen.getByText(/Acquired/)).not.toHaveTextContent('2026-09-30T10:00:00Z');
    fireEvent.click(screen.getByRole('button', { name: 'Apply Customer Name contains filter' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Enter a filter value');
    expect(screen.getByText(/4 matching filters/)).toBeInTheDocument();
    fireEvent.change(screen.getByRole('combobox', { name: 'Filter' }), { target: { value: 'productLine' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Product Line range from' }), { target: { value: 'B' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Product Line range to' }), { target: { value: 'A' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply Product Line range filter' }));
    expect(screen.getByRole('alert')).toHaveTextContent('From must be on or before To');
    fireEvent.change(screen.getByRole('textbox', { name: 'Product Line range to' }), { target: { value: 'C' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply Product Line range filter' }));
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    fireEvent.change(screen.getByRole('combobox', { name: 'Filter' }), { target: { value: 'dueDate' } });
    fireEvent.change(screen.getByLabelText('Due Date range from'), { target: { value: '2027-01-02' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply Due Date range filter' }));
    expect(screen.getByRole('alert')).toHaveTextContent('both Due Date boundaries');
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Due Date range to'), { target: { value: '2027-01-02' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply Due Date range filter' }));
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    await waitFor(() => expect(exportReport).toHaveBeenCalledWith('A', expect.objectContaining({ lineKeys: [data.lines[1].key] })));
    expect(data.lines[1].extPrice).toBe('0.250');
    fireEvent.click(screen.getByRole('button', { name: 'Remove Due Date range filter' }));
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Remove Product Line range filter' }));
    expect(screen.getByText(/4 matching filters/)).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1);
  });

  it('persists table-header keyboard and pointer order per workspace including hidden columns', async () => {
    get.mockResolvedValue(response(1));
    const { rerender } = render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    const table = within(await screen.findByRole('table'));
    fireEvent.click(screen.getByRole('button', { name: 'Move PO Left' }));
    expect(table.getAllByRole('columnheader')[1]).toHaveTextContent('PO');
    fireEvent.click(screen.getByText(/Columns \(9 visible\)/));
    fireEvent.click(screen.getByRole('checkbox', { name: 'SO' }));
    fireEvent.click(screen.getByRole('button', { name: 'Move PO Left' }));
    expect(table.getAllByRole('columnheader')[0]).toHaveTextContent('PO');
    fireEvent.click(screen.getByRole('checkbox', { name: 'SO' }));
    expect(table.getAllByRole('columnheader')[1]).toHaveTextContent('Due Date');
    rerender(<OpenOrdersPanel key="B" assignmentId="B" snapshotId="snap-1" />);
    await waitFor(() => expect(get).toHaveBeenCalledWith('B', 'snap-1'));
    expect(within(screen.getByRole('table')).getAllByRole('columnheader')[0]).toHaveTextContent('Due Date');
    rerender(<OpenOrdersPanel key="A2" assignmentId="A" snapshotId="snap-1" />);
    await waitFor(() => expect(within(screen.getByRole('table')).getAllByRole('columnheader')[0]).toHaveTextContent('PO'));
  });

  it('rejects invalid calendar dates and reversed date bounds without adding chips', async () => {
    get.mockResolvedValue(response(1));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.change(screen.getByRole('combobox', { name: 'Filter' }), { target: { value: 'dueDate' } });
    const from = screen.getByLabelText('Due Date range from');
    const to = screen.getByLabelText('Due Date range to');
    fireEvent.change(from, { target: { value: '2027-02-30' } });
    fireEvent.change(to, { target: { value: '2027-03-01' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply Due Date range filter' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Enter both Due Date boundaries'); // browser date control rejects invalid date
    fireEvent.change(from, { target: { value: '2027-03-02' } });
    fireEvent.change(to, { target: { value: '2027-03-01' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply Due Date range filter' }));
    expect(screen.getByRole('alert')).toHaveTextContent('From must be on or before To');
    expect(screen.queryByRole('button', { name: 'Remove Due Date range filter' })).not.toBeInTheDocument();
  });

  it('applies one-sided Product Line bounds and edits a range without affecting an independent filter', async () => {
    const data = response(3);
    data.lines[0] = { ...data.lines[0], productLine: 'A' };
    data.lines[1] = { ...data.lines[1], productLine: 'B' };
    data.lines[2] = { ...data.lines[2], productLine: 'C' };
    get.mockResolvedValue(data);
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByText(/3 scoped lines/);
    applyTextFilter('customer', 'C1');
    fireEvent.change(screen.getByRole('combobox', { name: 'Filter' }), { target: { value: 'productLine' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Product Line range from' }), { target: { value: 'B' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply Product Line range filter' }));
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Edit Product Line range filter/ })).toHaveTextContent('B – Any');
    fireEvent.click(screen.getByRole('button', { name: /Edit Product Line range filter/ }));
    fireEvent.change(screen.getByRole('textbox', { name: 'Product Line range from' }), { target: { value: '' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Product Line range to' }), { target: { value: 'B' } });
    fireEvent.submit(screen.getByRole('form', { name: 'Add or edit report filter' }));
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Edit Product Line range filter/ })).toHaveTextContent('Any – B');
    expect(screen.getByRole('button', { name: 'Remove Customer # contains filter' })).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1);
  });

  it('applies SO, PO and Item Number chips to counts and complete filtered export; editing and removal leave other chips intact', async () => {
    const data = response(3);
    data.lines[0] = { ...data.lines[0], key: { ...data.lines[0].key, salesOrder: 'SO-42a' }, purchaseOrder: 'PO-91a', itemNumber: 'ITEM-77a' };
    data.lines[1] = { ...data.lines[1], key: { ...data.lines[1].key, salesOrder: 'SO-42b' }, purchaseOrder: 'PO-91b', itemNumber: 'ITEM-77b' };
    data.lines[2] = { ...data.lines[2], purchaseOrder: null, itemNumber: 'OTHER' };
    get.mockResolvedValue(data); exportReport.mockResolvedValue({ blob: new Blob(), fileName: null });
    save.mockResolvedValue({ kind: 'saved', fileName: 'renamed-by-owner.xlsx' });
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" workspaceName="Shure SMT" site="SW" />);
    await screen.findByText(/3 scoped lines/);
    applyTextFilter('so', 'o-42');
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    applyTextFilter('po', 'O-91');
    applyTextFilter('itemNumber', 'm-77');
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /^Remove .* filter$/ })).toHaveLength(3);
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    await waitFor(() => expect(exportReport).toHaveBeenCalledWith('A', expect.objectContaining({ lineKeys: [data.lines[0].key, data.lines[1].key] })));
    expect(save).toHaveBeenCalledWith(expect.any(Blob), 'Shure-SMT-Open-Orders-2026-09-30.xlsx', 'Save Customer Open Orders Workbook');
    expect(screen.getByRole('status', { name: '' })).toBeInTheDocument();
    expect(screen.getByText('Saved renamed-by-owner.xlsx')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Edit PO contains filter/ }));
    fireEvent.change(screen.getByRole('searchbox', { name: 'PO contains value' }), { target: { value: 'missing' } });
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    fireEvent.submit(screen.getByRole('form', { name: 'Add or edit report filter' }));
    expect(screen.getByText(/0 matching filters/)).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /^Remove .* filter$/ })).toHaveLength(3);
    fireEvent.click(screen.getByRole('button', { name: 'Remove PO contains filter' }));
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1);
  });

  it('shows successful actual-name feedback near export for five seconds, clears timers on workspace change and unmount', async () => {
    get.mockResolvedValue(response(1)); exportReport.mockResolvedValue({ blob: new Blob(), fileName: 'Advertised.xlsx' });
    save.mockResolvedValue({ kind: 'saved', fileName: 'Chosen.xlsx' });
    const { rerender, unmount } = render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    vi.useFakeTimers();
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    await act(async () => { await Promise.resolve(); await Promise.resolve(); await Promise.resolve(); });
    expect(screen.getByText('Saved Chosen.xlsx')).toBeInTheDocument();
    expect(save).toHaveBeenCalledWith(expect.any(Blob), 'Advertised.xlsx', expect.any(String));
    act(() => vi.advanceTimersByTime(4_999));
    expect(screen.getByText('Saved Chosen.xlsx')).toBeInTheDocument();
    act(() => vi.advanceTimersByTime(1));
    expect(screen.queryByText('Saved Chosen.xlsx')).not.toBeInTheDocument();
    vi.useRealTimers();
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    expect(await screen.findByText('Saved Chosen.xlsx')).toBeInTheDocument();
    rerender(<OpenOrdersPanel assignmentId="B" snapshotId="snap-1" />);
    expect(screen.queryByText('Saved Chosen.xlsx')).not.toBeInTheDocument();
    unmount();
  });

  it('does not display a late save result after switching workspace assignments in the same panel instance', async () => {
    let finishSave!: (result: { kind: 'saved'; fileName: string }) => void;
    get.mockResolvedValue(response(1)); exportReport.mockResolvedValue({ blob: new Blob(), fileName: 'Advertised.xlsx' });
    save.mockImplementation(() => new Promise(resolve => { finishSave = resolve; }));
    function Host() {
      const [workspace, setWorkspace] = useState('A');
      return <><button onClick={() => setWorkspace('B')}>Switch workspace</button>
        <OpenOrdersPanel key={workspace} assignmentId={workspace} snapshotId="snap-1" /></>;
    }
    render(<Host />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'Export filtered rows to XLSX' }));
    await waitFor(() => expect(save).toHaveBeenCalledTimes(1));
    fireEvent.click(screen.getByRole('button', { name: 'Switch workspace' }));
    await waitFor(() => expect(get).toHaveBeenCalledWith('B', 'snap-1'));
    await act(async () => finishSave({ kind: 'saved', fileName: 'Old.xlsx' }));
    expect(screen.queryByText('Saved Old.xlsx')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Export filtered rows to XLSX' })).toBeEnabled();
  });

  it.skipIf(!process.env.KST_STAGE13_POLISH_FIXTURE_PATH)('writes an offline synthetic report view for desktop screenshot evidence', async () => {
    const data = response(35);
    data.lines = data.lines.map((line, index) => ({ ...line, customerName: index % 2 ? 'Acme West' : 'Acme East', itemNumber: `P-${index + 1}`,
      sourceValues: { ...line.sourceValues, dueDate: `2027-01-${String(index % 28 + 1).padStart(2, '0')}` } }));
    get.mockResolvedValue(data);
    const { container } = render(<OpenOrdersPanel assignmentId="synthetic-review" snapshotId="synthetic-mps" />);
    await screen.findByText(/35 scoped lines/);
    applyTextFilter('customerName', 'Acme');
    fireEvent.click(screen.getByText(/Columns \(9 visible\)/));
    const theme = readFileSync(resolve(process.cwd(), 'src/index.css'), 'utf8');
    const css = readFileSync(resolve(process.cwd(), 'src/components/OpenOrdersPanel.css'), 'utf8');
    writeFileSync(process.env.KST_STAGE13_POLISH_FIXTURE_PATH!, `<!doctype html><html><head><meta charset="utf-8"><style>${theme}\n${css}\nbody { margin:0; background:var(--app); } .fixture { display:flex; height:850px; padding-top:16px; box-sizing:border-box; }</style></head><body><div class="fixture">${container.innerHTML}</div></body></html>`);
  });

  it.skipIf(!process.env.KST_STAGE13_PLAN_FIXTURE_PATH)('writes a synthetic planning screen for owner review', async () => {
    get.mockResolvedValue(response(3));
    const { container } = render(<OpenOrdersPanel assignmentId="synthetic-planning" snapshotId="synthetic-mps" />);
    await screen.findByText(/3 scoped lines/);
    fireEvent.click(screen.getByRole('checkbox', { name: 'Plan Mode' }));
    fireEvent.change(screen.getByRole('textbox', { name: 'Due Date proposal SO-1/1' }), { target: { value: '1/8/2027' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    fireEvent.change(screen.getByRole('combobox', { name: 'Reason Code SO-1/1' }), { target: { value: 'Planning' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Price proposal SO-2/1' }), { target: { value: '0.3333' } });
    const theme = readFileSync(resolve(process.cwd(), 'src/index.css'), 'utf8');
    const css = readFileSync(resolve(process.cwd(), 'src/components/OpenOrdersPanel.css'), 'utf8');
    const screenshotMarkup = container.innerHTML.replace(/(<label><input type="checkbox")/, '$1 checked=""'); // controlled DOM state is a property, not serialized by innerHTML
    writeFileSync(process.env.KST_STAGE13_PLAN_FIXTURE_PATH!, `<!doctype html><html><head><meta charset="utf-8"><style>${theme}\n${css}\nbody { margin:0; background:var(--app); } .fixture { display:flex; height:850px; padding-top:16px; box-sizing:border-box; }</style></head><body><div class="fixture">${screenshotMarkup}</div></body></html>`);
  });
});

import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { act, fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { StrictMode } from 'react';
import { HistoricalShipmentsReport } from './HistoricalShipmentsReport';
import { hsColumnsKey, hsPageSizeKey, initialHsPageSize, restoreColumns, validateFilters, blankFilters } from './historicalShipmentsPreferences';
import { activeCustomerFilters, calendarRange } from './historicalShipmentsView';
import { historicalColumnGroups, hsGroupIds } from './reportColumnGroups';
import type { HistoricalShipmentsClient, HistoricalShipmentsSpec, HistoricalShipmentsRequest, HistoricalShipmentsPrepared } from '../api/historicalShipmentsApi';
import { AnalyticalSaveStatus } from '../components/AnalyticalSaveStatus';
import { clearExportTests, deferred, exportWorkbook } from './reportTestSupport';
const host = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => true }));
const spec: HistoricalShipmentsSpec = { sites: ['SW', 'KV'], maximumPageSize: 1000, maximumXlsxDataRows: 1048575, columns: [
  { id: 'site', label: 'Site', kind: 'text', provenance: 'Recorded invoice header', defaultSelected: true, monetary: false },
  { id: 'invoiceNumber', label: 'Invoice Number', kind: 'text', provenance: 'Recorded invoice header', defaultSelected: true, monetary: false },
  { id: 'unitPrice', label: 'Recorded Unit Price', kind: 'decimal', provenance: 'Recorded original invoice currency', defaultSelected: false, monetary: true },
  { id: 'customerName', label: 'Customer Name (current master)', kind: 'text', provenance: 'Current customer master', defaultSelected: false, monetary: false },
] };
function mockClient(rows = 2501) {
  let query: HistoricalShipmentsRequest;
  const op = () => ({ operationId: 'operation', state: 'ready', rowCount: rows, columns: query.columns, expiresAtUtc: '', acquiredAtUtc: '2026-10-19T12:00:00Z', unavailableEnrichmentRows: 2 });
  return { acquire: vi.fn(async (request: HistoricalShipmentsRequest) => { query = request; return op(); }),
    page: vi.fn(async (offset: number, limit: number) => ({ operationId: 'operation', offset, totalRows: rows, columns: query.columns, rows: Array.from({ length: Math.max(0, Math.min(limit, rows - offset)) }, (_, i) => ({ physicalDetailId: `physical-${offset + i}`, cells: query.columns.map(id => id === 'site' ? 'SW' : id === 'invoiceNumber' ? `inv-${offset + i}` : id === 'unitPrice' ? '-2.50' : null) })) })),
    prepare: vi.fn(async () => ({ operationId: 'operation', preparationId: 'exact-historical', fileName: 'History.xlsx', byteLength: 1234, expiresAtUtc: '' })),
    release: vi.fn(async () => {}), save: vi.fn(async (_prepared: HistoricalShipmentsPrepared, _open: boolean, _id: string) => ({ outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true })),
    saveStatus: vi.fn(async () => ({ outcome: 'saved', message: 'Workbook saved.', operationComplete: true })), open: vi.fn(async () => {}) };
}
const loadSpec = async () => spec;
function setup(client = mockClient()) {
  const createClient = () => client as unknown as HistoricalShipmentsClient;
  const tree = (active = true) => <><AnalyticalSaveStatus /><HistoricalShipmentsReport active={active} createClient={createClient} loadSpec={loadSpec} /></>;
  return { client, tree, ...render(tree()) };
}
async function display() { await screen.findByLabelText('Site (required)'); fireEvent.change(screen.getByLabelText('Site (required)'), { target: { value: 'SW' } }); await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Display' }))); }
const edit = () => fireEvent.click(screen.getByRole('button', { name: 'Edit query' }));
const columns = () => fireEvent.click(screen.getByRole('button', { name: /^Columns/ }));
beforeEach(() => { localStorage.clear(); host.invoke.mockReset(); host.invoke.mockResolvedValue(false); });
afterEach(clearExportTests);
describe('Historical preferences and helpers', () => {
  it('migrates old defaults, allowlists selections and retains locked columns in catalog order', () => {
    const old = ['site', 'customerNumber', 'customerName', 'invoiceNumber', 'invoiceDate', 'itemNumber', 'invoicedQuantity', 'salesOrder', 'salesOrderLine', 'customerPo'];
    expect(restoreColumns(spec, JSON.stringify(old))).toEqual(['site', 'invoiceNumber']);
    expect(restoreColumns(spec, JSON.stringify(['unitPrice', 'SQL', 5, 'unitPrice']))).toEqual(['site', 'invoiceNumber', 'unitPrice']); expect(restoreColumns(spec, '{bad')).toEqual(['site', 'invoiceNumber']);
  });
  it.each(['garbage', '200', '0', '100.0', '-100', 'null'])('guards invalid page preference %s', raw => { localStorage.setItem(hsPageSizeKey, raw); expect(initialHsPageSize()).toBe(100); });
  it.each([100, 500, 1000])('restores supported page size %s', n => { localStorage.setItem(hsPageSizeKey, String(n)); expect(initialHsPageSize()).toBe(n); });
  it('retains defensive exclusivity validation but sends only active customer mode', () => {
    const f = { ...blankFilters, site: 'SW', corpGroup: 'ACME', customerFrom: '0001', customerTo: '0099' };
    expect(validateFilters(f)).toHaveLength(1); expect(activeCustomerFilters(f, 'corp')).toMatchObject({ corpGroup: 'ACME', customerFrom: '', customerTo: '' }); expect(activeCustomerFilters(f, 'range')).toMatchObject({ corpGroup: '', customerFrom: '0001' }); expect(activeCustomerFilters(f, 'any')).toMatchObject({ corpGroup: '', customerFrom: '', customerTo: '' });
  });
  it('uses inclusive local-calendar quick ranges across year and leap-day boundaries', () => {
    expect(calendarRange('2026-01-02', 30)).toEqual({ invoiceDateFrom: '2025-12-04', invoiceDateTo: '2026-01-02' }); expect(calendarRange('2024-03-01', 90).invoiceDateFrom).toBe('2023-12-03'); expect(calendarRange('2026-10-19', 'year').invoiceDateFrom).toBe('2026-01-01');
  });
  it('maps each known catalog id exactly once, keeps defaults first and unknowns in Other', () => {
    const ids = Object.values(hsGroupIds).flat(); expect(ids).toHaveLength(41); expect(new Set(ids).size).toBe(41);
    const groups = historicalColumnGroups([...spec.columns, { ...spec.columns[0], id: 'future', defaultSelected: false }]);
    expect(groups[groups.length - 1]?.label).toBe('Other'); expect(groups[groups.length - 1]?.columns[0].id).toBe('future'); expect(groups.flatMap(g => g.columns).map(c => c.id).sort()).toEqual([...spec.columns.map(c => c.id), 'future'].sort());
  });
});
describe('Historical frozen operation, draft and paging', () => {
  it('starts with Any customer/blank dates, valid unchanged Display refreshes and never persists filters', async () => {
    const { client } = setup(); await screen.findByLabelText('Site (required)'); expect(screen.getByRole('button', { name: 'Any customer' })).toHaveAttribute('aria-pressed', 'true'); expect(screen.getByLabelText('Invoice Date From')).toHaveValue(''); expect(screen.getByRole('button', { name: 'Display' })).toBeDisabled();
    await display(); await screen.findByText('inv-0'); expect(screen.getByRole('button', { name: 'Display' })).toBeEnabled(); await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Display' })));
    expect(client.acquire).toHaveBeenCalledTimes(2); expect(client.release).toHaveBeenCalledTimes(1); expect(Object.keys(localStorage)).not.toContain('site'); expect(localStorage.getItem(hsColumnsKey)).toBeNull();
  });
  it('draft edits retain table, applied summary/metadata/export columns and prepared exact retry', async () => {
    const { client } = setup(); await display(); await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' }); edit();
    fireEvent.change(screen.getByLabelText('Item Number From'), { target: { value: 'DRAFT' } }); columns(); fireEvent.click(screen.getByRole('checkbox', { name: 'Recorded Unit Price' })); fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    expect(screen.getByText('inv-0')).toBeInTheDocument(); expect(within(screen.getByRole('region', { name: 'Results table' })).queryByText('Recorded Unit Price')).toBeNull(); expect(screen.getByText('Query changed. Display to update.')).toBeInTheDocument(); expect(screen.getByText('Exports all 2,501 rows · 2 columns')).toBeInTheDocument();
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Save again' }))); expect(client.prepare).toHaveBeenCalledTimes(1); expect(client.save).toHaveBeenCalledTimes(2); expect(client.save.mock.calls[0][0]).toBe(client.save.mock.calls[1][0]); expect(client.release).not.toHaveBeenCalled();
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Display' }))); expect(client.acquire).toHaveBeenLastCalledWith(expect.objectContaining({ itemFrom: 'DRAFT', columns: ['site', 'invoiceNumber', 'unitPrice'] }), expect.any(AbortSignal)); expect(screen.getAllByText('-2.50')).toHaveLength(100);
  });
  it('switching customer mode keeps draft text but submits only active scope', async () => {
    const { client } = setup(); await screen.findByLabelText('Site (required)'); fireEvent.click(screen.getByRole('button', { name: 'Corp Group' })); fireEvent.change(screen.getByLabelText('Corp Group'), { target: { value: 'GROUP' } });
    fireEvent.click(screen.getByRole('button', { name: 'Customer range' })); fireEvent.change(screen.getByLabelText('Customer From'), { target: { value: '001' } }); fireEvent.click(screen.getByRole('button', { name: 'Corp Group' })); expect(screen.getByLabelText('Corp Group')).toHaveValue('GROUP');
    fireEvent.click(screen.getByRole('button', { name: 'Customer range' })); expect(screen.getByLabelText('Customer From')).toHaveValue('001'); await display(); expect(client.acquire).toHaveBeenCalledWith(expect.objectContaining({ customerFrom: '001', corpGroup: null }), expect.anything());
  });
  it('locks defaults, group None affects optional only, search and keyboard focus return work', async () => {
    const user = userEvent.setup(); setup(); await screen.findByLabelText('Site (required)'); const trigger = screen.getByRole('button', { name: /^Columns/ }); fireEvent.click(trigger);
    expect(screen.getByRole('dialog', { name: 'Columns' })).toBeInTheDocument(); expect(screen.getByRole('checkbox', { name: 'Site' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'All Pricing columns' })); fireEvent.click(screen.getByRole('button', { name: 'None Customer and shipping columns' })); expect(screen.getByRole('checkbox', { name: 'Site' })).toBeChecked();
    fireEvent.change(screen.getByLabelText('Find a column'), { target: { value: 'price' } }); expect(screen.queryByRole('checkbox', { name: 'Invoice Number' })).toBeNull(); expect(screen.getByRole('checkbox', { name: 'Recorded Unit Price' })).toBeChecked();
    await user.keyboard('{Escape}'); expect(trigger).toHaveFocus(); expect(localStorage.getItem(hsColumnsKey)).toContain('unitPrice');
    columns(); fireEvent.click(screen.getByRole('button', { name: 'Reset to defaults' })); expect(JSON.parse(localStorage.getItem(hsColumnsKey)!)).toEqual(['site', 'invoiceNumber']);
  });
  it('supports pages beyond 1000 and first-row preserving resizing without reacquisition or workbook invalidation', async () => {
    const { client } = setup(); await display(); await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' });
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Last' }))); expect(client.page).toHaveBeenLastCalledWith(2500, 100); expect(screen.getByText('inv-2500')).toBeInTheDocument();
    await act(async () => fireEvent.change(screen.getByLabelText('Rows per page'), { target: { value: '1000' } })); expect(client.page).toHaveBeenLastCalledWith(2000, 1000); expect(screen.getByText('inv-2000')).toBeInTheDocument(); expect(screen.queryByText('inv-0')).toBeNull();
    await act(async () => fireEvent.change(screen.getByLabelText('Rows per page'), { target: { value: '500' } })); expect(client.page).toHaveBeenLastCalledWith(2000, 500);
    await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Save again' }))); expect(client.acquire).toHaveBeenCalledTimes(1); expect(client.prepare).toHaveBeenCalledTimes(1); expect(client.save).toHaveBeenCalledTimes(2); expect(client.release).not.toHaveBeenCalled(); expect(localStorage.getItem(hsPageSizeKey)).toBe('500');
  });
  it('rejects stale page replies after departure, releases result once and does not republish', async () => {
    const pending = deferred<Awaited<ReturnType<ReturnType<typeof mockClient>['page']>>>(); const client = mockClient(); const original = client.page.getMockImplementation()!;
    client.page.mockImplementationOnce(original).mockReturnValueOnce(pending.promise); const view = setup(client); await display(); fireEvent.click(screen.getByRole('button', { name: 'Next' })); expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
    view.rerender(view.tree(false)); await act(async () => pending.resolve({ operationId: 'operation', offset: 100, totalRows: 2501, columns: ['site', 'invoiceNumber'], rows: [{ physicalDetailId: 'late', cells: ['SW', 'LATE'] }] })); expect(screen.queryByText('LATE')).toBeNull(); expect(client.release).toHaveBeenCalledTimes(1);
  });
  it('rejects wrong page identity and keeps applied results on paging failure', async () => {
    const client = mockClient(); const view = setup(client); await display(); client.page.mockResolvedValueOnce({ operationId: 'other', offset: 100, totalRows: 2501, columns: ['site', 'invoiceNumber'], rows: [] }); await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Next' })));
    expect(screen.getByRole('alert')).toHaveTextContent('identity mismatch'); expect(screen.getByText('inv-0')).toBeInTheDocument(); view.unmount();
  });
  it('acquisition cancellation and late success cannot publish, including StrictMode', async () => {
    const pending = deferred<Awaited<ReturnType<ReturnType<typeof mockClient>['acquire']>>>(); const client = mockClient(); client.acquire.mockReturnValueOnce(pending.promise); const factory = () => client as unknown as HistoricalShipmentsClient;
    render(<StrictMode><HistoricalShipmentsReport active createClient={factory} loadSpec={loadSpec} /></StrictMode>); await display(); fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    await act(async () => pending.resolve({ operationId: 'operation', state: 'ready', columns: ['site', 'invoiceNumber'], rowCount: 10, expiresAtUtc: '', acquiredAtUtc: '', unavailableEnrichmentRows: 0 })); expect(client.page).not.toHaveBeenCalled(); expect(client.release).toHaveBeenCalledTimes(1);
  });
  it('empty results are successful, errors remain failures and spec failure offers Retry', async () => {
    const { client } = setup(mockClient(0)); await display(); expect(screen.getByText('No invoiced shipments matched these filters.')).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    client.acquire.mockRejectedValueOnce('Source unavailable'); await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Display' }))); expect(screen.getByRole('alert')).toHaveTextContent('Source unavailable'); expect(screen.getByLabelText('Site (required)')).toBeInTheDocument();
  });
});

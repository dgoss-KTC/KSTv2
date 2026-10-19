import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { StrictMode } from 'react';
import type { ShipmentColumnFilter, WorkspaceShipmentsAcquisition, WorkspaceShipmentsClient, WorkspaceShipmentsPage, WorkspaceShipmentsSpec, WorkspaceShipmentsView } from '../api/workspaceShipmentsApi';
import { WorkspaceShipmentsPanel } from './WorkspaceShipmentsPanel';
import { formatWorkspaceShipmentsOnTimeRate } from './workspaceShipmentsPresentation';

const labels = ['Ship Date', 'Perform Date', 'Sales Order', 'Customer PO', 'Line', 'Part', 'Qty Shipped', 'Unit Price', 'Ext Price', 'Ship To', 'Ship Via', 'BOL', 'Invoice Number', 'SO Type', 'On Time'];
const ids = ['shipDate', 'originalCommit', 'salesOrder', 'customerPo', 'salesOrderLine', 'itemNumber', 'invoicedQuantity', 'retroUnitPrice', 'retroExtendedPrice', 'shipTo', 'shipVia', 'billOfLading', 'invoiceNumber', 'channel', 'onTime'];
const decimal = new Set(['invoicedQuantity', 'retroUnitPrice', 'retroExtendedPrice']);
const spec: WorkspaceShipmentsSpec = { maximumPageSize: 1000, pageSizes: [100, 500, 1000], maximumValues: 1000, maximumTextLength: 256, maximumDecimalDigits: 76, maximumDecimalScale: 76, columns: ids.map((id, i) => ({ id, label: labels[i], kind: decimal.has(id) ? 'decimal' : id === 'originalCommit' || id === 'shipDate' ? 'date' : 'text', filterKind: id === 'shipDate' ? null : id === 'onTime' ? 'values' : decimal.has(id) ? 'decimal' : id === 'originalCommit' ? 'date' : 'text', provenance: 'Recorded', defaultSelected: true, monetary: false })) };
const op = (rowCount: number, extra: Partial<WorkspaceShipmentsAcquisition> = {}) => ({ operationId: 'o', state: 'ready', rowCount, columns: ids, acquiredAtUtc: '2026-10-13T12:00:00Z', unavailableEnrichmentRows: 0, onTimeNumerator: 0, onTimeDenominator: 0, ...extra }) as unknown as WorkspaceShipmentsAcquisition;
const row = (id: string, onTime: string | null = '1', qty = '-2.5') => ({ physicalDetailId: id, cells: ids.map(c => c === 'onTime' ? onTime : c === 'invoicedQuantity' ? qty : c === 'itemNumber' ? '0000123' : c === 'shipDate' ? '2026-10-13' : null) });
const pageOf = (rows: ReturnType<typeof row>[]) => ({ rows }) as unknown as WorkspaceShipmentsPage;
const viewOf = (version: number, filters: ShipmentColumnFilter[] = [], rowCount = 1204): WorkspaceShipmentsView => ({ operationId: 'o', version, filters, rowCount, onTimeNumerator: 2, onTimeDenominator: 3 });

function makeClient() {
  return { acquire: vi.fn(), page: vi.fn(), release: vi.fn().mockResolvedValue(undefined),
    setFilters: vi.fn(), view: vi.fn(), cancelView: vi.fn().mockResolvedValue(undefined),
    values: vi.fn().mockResolvedValue({ operationId: 'o', viewVersion: 0, column: 'itemNumber', values: [{ value: '0000123', count: 200 }], retainedValues: [], blankCount: 0, distinctTotal: 1, matchingDistinctTotal: 1, truncated: false }) };
}
type Mock = ReturnType<typeof makeClient>;
const loadSpec = vi.fn();
function setup(mock: Mock = makeClient(), assignmentId = 'A') {
  const create = vi.fn(() => mock as unknown as WorkspaceShipmentsClient);
  const view = render(<WorkspaceShipmentsPanel assignmentId={assignmentId} createClient={create} loadSpec={loadSpec} />);
  return { mock, create, view };
}
const deferred = <T,>() => { let resolve!: (v: T) => void; let reject!: (e: unknown) => void; const promise = new Promise<T>((a, b) => { resolve = a; reject = b; }); return { promise, resolve, reject }; };

beforeEach(() => { localStorage.clear(); vi.useFakeTimers({ toFake: ['Date'] }); vi.setSystemTime(new Date(2026, 9, 13, 15, 0)); loadSpec.mockReset(); loadSpec.mockResolvedValue(spec); });
afterEach(() => { vi.useRealTimers(); });

describe('Workspace Shipments panel (mocked generated client only)', () => {
  it('initializes the last ninety days with a blank part, loads immediately and renders the fifteen authoritative columns', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(2)); mock.page.mockResolvedValue(pageOf([row('a'), row('b', '0'), row('c', null)]));
    setup(mock);
    const table = await screen.findByRole('table');
    expect(mock.acquire).toHaveBeenCalledTimes(1);
    expect(mock.acquire.mock.calls[0][0]).toEqual({ partNumber: null, shipDateFrom: '2026-07-16', shipDateTo: '2026-10-13' });
    expect(mock.page.mock.calls[0].slice(0, 2)).toEqual([0, 100]);
    expect(screen.getByLabelText('Ship Date From')).toHaveValue('2026-07-16');
    expect(screen.getByLabelText('Ship Date To')).toHaveValue('2026-10-13');
    expect(screen.getByLabelText('Part Number')).toHaveValue('');
    expect(within(table).getAllByRole('columnheader').map(h => h.textContent)).toEqual(labels);
    expect(within(table).getByRole('img', { name: 'On Time: True' })).toHaveTextContent('✓');
    expect(within(table).getByRole('img', { name: 'On Time: False' })).toHaveTextContent('✕');
    expect(within(table).getAllByText('-2.5')).toHaveLength(3);
    expect(within(table).getAllByText('0000123')).toHaveLength(3);
    expect(within(table).getAllByText('—').length).toBeGreaterThan(3);
    expect(screen.queryByText(/Optional columns|Export|Save/)).not.toBeInTheDocument();
  });

  it('keeps edits as drafts until Apply, and Refresh uses the applied filters', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(1)); mock.page.mockResolvedValue(pageOf([row('a')]));
    setup(mock); await screen.findByRole('table');
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: '  0000123%_ ' } });
    fireEvent.change(screen.getByLabelText('Ship Date From'), { target: { value: '' } });
    expect(mock.acquire).toHaveBeenCalledTimes(1);
    expect(screen.getByText(/Acquisition inputs have unapplied edits/)).toHaveTextContent('Ship Date 2026-07-16 to 2026-10-13 · All parts');
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
    await waitFor(() => expect(mock.acquire).toHaveBeenCalledTimes(2));
    expect(mock.acquire.mock.calls[1][0]).toEqual({ partNumber: null, shipDateFrom: '2026-07-16', shipDateTo: '2026-10-13' });
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    await waitFor(() => expect(mock.acquire).toHaveBeenCalledTimes(3));
    expect(mock.acquire.mock.calls[2][0]).toEqual({ partNumber: '0000123%_', shipDateFrom: null, shipDateTo: '2026-10-13' });
    await screen.findByRole('table');
    expect(screen.queryByText(/Filters changed/)).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
    await waitFor(() => expect(mock.acquire).toHaveBeenCalledTimes(4));
    expect(mock.acquire.mock.calls[3][0]).toEqual(mock.acquire.mock.calls[2][0]);
  });

  it('states that blank dates mean all available history and rejects an inverted range', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(0)); mock.page.mockResolvedValue(pageOf([]));
    setup(mock); await screen.findByText(/No invoiced shipments matched/);
    fireEvent.change(screen.getByLabelText('Ship Date From'), { target: { value: '' } });
    fireEvent.change(screen.getByLabelText('Ship Date To'), { target: { value: '' } });
    expect(screen.getByText(/Retrieved scope:/)).toHaveTextContent('Ship Date 2026-07-16 to 2026-10-13');
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    await waitFor(() => expect(mock.acquire).toHaveBeenCalledTimes(2));
    expect(mock.acquire.mock.calls[1][0]).toEqual({ partNumber: null, shipDateFrom: null, shipDateTo: null });
    expect(screen.getByText(/Retrieved scope: All available history · All parts/)).toBeInTheDocument();
    await screen.findByText(/No invoiced shipments matched/);
    fireEvent.change(screen.getByLabelText('Ship Date From'), { target: { value: '2026-10-02' } });
    fireEvent.change(screen.getByLabelText('Ship Date To'), { target: { value: '2026-10-01' } });
    expect(screen.getByRole('alert')).toHaveTextContent('Ship Date To must be on or after');
    expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
  });

  it('pages one bounded page at a time and keeps the page after a page failure', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(250));
    mock.page.mockImplementation(async (offset: number) => pageOf([row(`r${offset}`)]));
    setup(mock); await screen.findByRole('table');
    expect(await screen.findByText(/Rows 1–1 of 250/)).toBeInTheDocument();
    expect(screen.getByText('Page 1 of 3')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'First' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    await screen.findByText(/Page 2 of 3/);
    expect(mock.page).toHaveBeenLastCalledWith(100, 100, expect.any(AbortSignal), 0);
    fireEvent.click(screen.getByRole('button', { name: 'Last' }));
    await screen.findByText(/Page 3 of 3/);
    expect(mock.page).toHaveBeenLastCalledWith(200, 100, expect.any(AbortSignal), 0);
    expect(screen.getAllByRole('row')).toHaveLength(2);
    mock.page.mockRejectedValueOnce(new Error('The result expired.'));
    fireEvent.click(screen.getByRole('button', { name: 'Previous' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('The result expired.');
    expect(screen.getByText(/Page 3 of 3/)).toBeInTheDocument();
  });

  it('shows the complete acquisition On Time Rate independently of page contents and clears it during replacement', async () => {
    const mock = makeClient();
    mock.acquire.mockResolvedValueOnce(op(250, { onTimeNumerator: 2, onTimeDenominator: 3 }));
    mock.page.mockResolvedValueOnce(pageOf([row('one', '1'), row('zero', '0')]));
    mock.page.mockResolvedValueOnce(pageOf([row('unavailable', null)]));
    const replacement = deferred<WorkspaceShipmentsAcquisition>(); mock.acquire.mockReturnValueOnce(replacement.promise);
    setup(mock); await screen.findByRole('table');
    expect(screen.getByText('On Time Rate').nextElementSibling).toHaveTextContent('66.7%');
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    await screen.findByText(/Page 2 of 3/);
    expect(screen.getByText('On Time Rate').nextElementSibling).toHaveTextContent('66.7%');
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
    await waitFor(() => expect(screen.getByText('On Time Rate').nextElementSibling).toHaveTextContent('—'));
    expect(mock.acquire).toHaveBeenCalledTimes(2);
    await act(async () => { replacement.resolve(op(2, { onTimeNumerator: 0, onTimeDenominator: 2 })); });
    expect(screen.getByText('On Time Rate').nextElementSibling).toHaveTextContent('0.0%');
  });

  it('renders one hundred percent and an unavailable rate for empty eligible denominators', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValueOnce(op(1, { onTimeNumerator: 1, onTimeDenominator: 1 }));
    mock.page.mockResolvedValueOnce(pageOf([row('one')]));
    setup(mock); await screen.findByRole('table');
    expect(screen.getByText('On Time Rate').nextElementSibling).toHaveTextContent('100.0%');
  });

  it('uses one-decimal project formatting and displays unavailable rather than zero for no eligible rows', () => {
    expect(formatWorkspaceShipmentsOnTimeRate(2, 3)).toBe('66.7%');
    expect(formatWorkspaceShipmentsOnTimeRate(0, 4)).toBe('0.0%');
    expect(formatWorkspaceShipmentsOnTimeRate(4, 4)).toBe('100.0%');
    expect(formatWorkspaceShipmentsOnTimeRate(0, 0)).toBe('—');
    expect(formatWorkspaceShipmentsOnTimeRate(null, null)).toBe('—');
  });

  it('shows a safe actionable error and never shows partial results; browser network wording is replaced', async () => {
    const mock = makeClient(); mock.acquire.mockRejectedValueOnce(new Error('An invoice detail has no matching header. No complete report was acquired.'));
    setup(mock);
    expect(await screen.findByRole('alert')).toHaveTextContent('Refresh to try again. An invoice detail has no matching header.');
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
    mock.acquire.mockRejectedValueOnce(new TypeError('Failed to fetch'));
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('could not be reached'));
    expect(screen.getByRole('alert')).not.toHaveTextContent('Failed to fetch');
    mock.acquire.mockResolvedValue(op(1)); mock.page.mockResolvedValue(pageOf([row('a')]));
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
    await screen.findByRole('table'); expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('cancels outstanding retrieval, ignores its late reply and can retry', async () => {
    const mock = makeClient(); const late = deferred<WorkspaceShipmentsAcquisition>(); mock.acquire.mockReturnValueOnce(late.promise);
    setup(mock);
    fireEvent.click(await screen.findByRole('button', { name: 'Cancel' }));
    expect(screen.getByText(/Retrieval cancelled/)).toBeInTheDocument();
    expect(mock.acquire.mock.calls[0][1].aborted).toBe(true);
    await act(async () => { late.resolve(op(5)); });
    expect(mock.page).not.toHaveBeenCalled(); expect(screen.queryByRole('table')).not.toBeInTheDocument();
    mock.acquire.mockResolvedValue(op(1)); mock.page.mockResolvedValue(pageOf([row('a')]));
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' })); await screen.findByRole('table');
  });

  it('rejects the old reply when Refresh supersedes it', async () => {
    const mock = makeClient(); const first = deferred<WorkspaceShipmentsAcquisition>(); mock.acquire.mockReturnValueOnce(first.promise);
    setup(mock);
    fireEvent.click(await screen.findByRole('button', { name: 'Cancel' }));
    mock.acquire.mockResolvedValue(op(1)); mock.page.mockResolvedValue(pageOf([row('new')]));
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' })); await screen.findByRole('table');
    await act(async () => { first.resolve(op(9)); });
    expect(screen.getByText(/· 1 rows/)).toBeInTheDocument();
  });

  it('releases on departure, rejects late replies, and a workspace change starts clean with fresh filters', async () => {
    const first = makeClient(); const late = deferred<WorkspaceShipmentsAcquisition>(); first.acquire.mockReturnValue(late.promise);
    const second = makeClient(); second.acquire.mockResolvedValue(op(1, { onTimeNumerator: 1, onTimeDenominator: 1 })); second.page.mockResolvedValue(pageOf([row('b')]));
    const clients = [first, second]; const create = vi.fn(() => clients.shift() as unknown as WorkspaceShipmentsClient);
    const view = render(<WorkspaceShipmentsPanel assignmentId="A" createClient={create} loadSpec={loadSpec} />);
    await screen.findByRole('button', { name: 'Cancel' });
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'EDITED' } });
    view.rerender(<WorkspaceShipmentsPanel assignmentId="B" createClient={create} loadSpec={loadSpec} />);
    await screen.findByRole('table');
    expect(screen.getByText('On Time Rate').nextElementSibling).toHaveTextContent('100.0%');
    expect(first.release).toHaveBeenCalledTimes(1);
    expect(screen.getByLabelText('Part Number')).toHaveValue('');
    expect(second.acquire.mock.calls[0][0]).toEqual({ partNumber: null, shipDateFrom: '2026-07-16', shipDateTo: '2026-10-13' });
    await act(async () => { late.resolve(op(77)); });
    expect(first.page).not.toHaveBeenCalled(); expect(screen.getByText(/· 1 rows/)).toBeInTheDocument();
    expect(screen.getByText('On Time Rate').nextElementSibling).toHaveTextContent('100.0%');
    view.unmount(); expect(second.release).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('a revisit never reuses results or filters, and StrictMode replay uses a fresh unreleased client', async () => {
    const made: Mock[] = [];
    const create = vi.fn(() => { const m = makeClient(); m.acquire.mockResolvedValue(op(1)); m.page.mockResolvedValue(pageOf([row('a')])); made.push(m); return m as unknown as WorkspaceShipmentsClient; });
    const view = render(<StrictMode><WorkspaceShipmentsPanel assignmentId="A" createClient={create} loadSpec={loadSpec} /></StrictMode>);
    await screen.findByRole('table');
    expect(made.length).toBe(2); expect(made[0].release).toHaveBeenCalled(); expect(made[1].release).not.toHaveBeenCalled();
    expect(made[0].page).not.toHaveBeenCalled(); expect(made[1].page).toHaveBeenCalledTimes(1);
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'KEEP' } });
    view.unmount();
    const again = render(<WorkspaceShipmentsPanel assignmentId="A" createClient={create} loadSpec={loadSpec} />);
    expect(screen.getByLabelText('Part Number')).toHaveValue(''); expect(screen.queryByRole('table')).not.toBeInTheDocument();
    await screen.findByRole('table'); again.unmount();
  });

  it('reports unavailable enrichment rows and offers a retry when column definitions fail', async () => {
    loadSpec.mockRejectedValueOnce(new Error('x'));
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(1, { unavailableEnrichmentRows: 1 })); mock.page.mockResolvedValue(pageOf([row('a')]));
    setup(mock);
    expect(await screen.findByText('Column definitions could not be loaded.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await screen.findByRole('table');
    expect(screen.getByText(/1 rows have unavailable current-master information/)).toBeInTheDocument();
  });

  it('uses one spec-supported 500/1000 call and retains the first visible row in the new page without reacquisition', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(2407)); mock.page.mockImplementation(async (offset: number) => pageOf([row(`r${offset}`)]));
    setup(mock); await screen.findByText(/Rows 1–1 of 2,407/);
    fireEvent.click(screen.getByRole('button', { name: 'Last' })); await screen.findByText('Page 25 of 25');
    fireEvent.click(screen.getByRole('button', { name: '500' })); await screen.findByText('Page 5 of 5');
    expect(mock.page).toHaveBeenLastCalledWith(2000, 500, expect.any(AbortSignal), 0);
    fireEvent.click(screen.getByRole('button', { name: '1000' })); await screen.findByText('Page 3 of 3');
    expect(mock.page).toHaveBeenLastCalledWith(2000, 1000, expect.any(AbortSignal), 0);
    expect(mock.acquire).toHaveBeenCalledTimes(1); expect(localStorage.getItem('kst.workspaceShipments.pageSize.v1')).toBe('1000');
  });

  it('validates persisted page size against the server spec before the first page', async () => {
    localStorage.setItem('kst.workspaceShipments.pageSize.v1', '1000');
    loadSpec.mockResolvedValue({ ...spec, maximumPageSize: 200, pageSizes: [100] });
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(1)); mock.page.mockResolvedValue(pageOf([row('a')]));
    setup(mock); await screen.findByText(/Rows 1–1 of 1/);
    expect(mock.page.mock.calls[0][1]).toBe(100);
    expect(screen.getByRole('button', { name: '1000' })).toBeDisabled();
    expect(screen.getByText(/Not available on this version/)).toBeInTheDocument();
  });

  async function containsPart(text: string) {
    fireEvent.click(screen.getByRole('button', { name: /Filter Part/ }));
    const dialog = screen.getByRole('dialog', { name: 'Filter Part' });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Contains' }));
    fireEvent.change(within(dialog).getByLabelText('Part contains'), { target: { value: text } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Apply' }));
  }

  it('publishes filtered counts and summary from the server, resets paging and carries filters through Apply and Refresh', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(9310)); mock.page.mockResolvedValue(pageOf([row('a')]));
    const filter: ShipmentColumnFilter = { column: 'itemNumber', kind: 'contains', text: '%_', includeBlanks: true };
    mock.setFilters.mockResolvedValue(viewOf(1, [filter]));
    setup(mock); await screen.findByText(/Rows 1–1 of 9,310/);
    fireEvent.click(screen.getByRole('button', { name: 'Last' })); await screen.findByText('Page 94 of 94');
    await containsPart('%_'); await screen.findByText(/filtered from 9,310/);
    expect(screen.getByText('On Time Rate').nextElementSibling).toHaveTextContent('66.7%');
    expect(screen.getByText('filtered', { exact: true })).toBeInTheDocument();
    expect(screen.getByText('Page 1 of 13')).toBeInTheDocument();
    expect(mock.setFilters).toHaveBeenLastCalledWith([filter], 0, 1, expect.any(AbortSignal));
    expect(mock.page).toHaveBeenLastCalledWith(0, 100, undefined, 1);
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'NARROW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    await waitFor(() => expect(mock.setFilters).toHaveBeenCalledTimes(2)); await screen.findByText(/filtered from 9,310/);
    expect(screen.getByText(/Retrieved scope:/)).toHaveTextContent('Part NARROW');
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'UNAPPLIED' } });
    expect(screen.getByText(/Retrieved scope:/)).toHaveTextContent('Part NARROW');
    fireEvent.click(screen.getByRole('button', { name: 'Refresh' }));
    await waitFor(() => expect(mock.setFilters).toHaveBeenCalledTimes(3));
    expect(mock.acquire.mock.calls[2][0].partNumber).toBe('NARROW');
  });

  it('cancels a pending filter without releasing the acquisition and ignores its late reply', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(200)); mock.page.mockResolvedValue(pageOf([row('old')]));
    const late = deferred<WorkspaceShipmentsView>(); mock.setFilters.mockReturnValue(late.promise); mock.view.mockResolvedValue(viewOf(0, [], 200));
    setup(mock); await screen.findByText(/Rows 1–1 of 200/);
    await containsPart('NO'); expect(screen.getByText('Applying filter…')).toBeInTheDocument();
    expect(screen.getByRole('table')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    await screen.findByText('Filter cancelled. Previous view retained.');
    expect(mock.setFilters.mock.calls[0][3].aborted).toBe(true); expect(mock.cancelView).toHaveBeenCalledWith(1);
    await act(async () => late.resolve(viewOf(1, [], 1)));
    expect(screen.getByText(/Rows 1–1 of 200/)).toBeInTheDocument();
    expect(mock.release).not.toHaveBeenCalled(); expect(mock.acquire).toHaveBeenCalledTimes(1);
  });

  it('keeps the prior page after a failed build but reconciles a completed response lost in transport', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(200)); mock.page.mockResolvedValue(pageOf([row('old')]));
    mock.setFilters.mockRejectedValue(new Error('disk unavailable')); mock.view.mockResolvedValue(viewOf(0, [], 200));
    setup(mock); await screen.findByText(/Rows 1–1 of 200/); await containsPart('A');
    await screen.findByText(/Filter could not be applied/); expect(screen.getByText(/Rows 1–1 of 200/)).toBeInTheDocument();
    mock.view.mockResolvedValue(viewOf(1, [{ column: 'itemNumber', kind: 'contains', text: 'B', includeBlanks: false }], 7));
    await containsPart('B'); await screen.findByText(/Rows 1–1 of 7 · filtered from 200/);
    expect(mock.page).toHaveBeenLastCalledWith(0, 100, undefined, 1);
  });

  it('has reachable headers for no-match results, clears filters server-side, and keeps the acquisition total', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(200)); mock.page.mockResolvedValue(pageOf([]));
    mock.setFilters.mockResolvedValueOnce(viewOf(1, [{ column: 'itemNumber', kind: 'contains', text: 'NO', includeBlanks: false }], 0)).mockResolvedValueOnce(viewOf(2, [], 200));
    setup(mock); await screen.findByRole('table'); await containsPart('NO'); await screen.findByText('No rows match the column filters.');
    expect(screen.getByRole('button', { name: 'Filter Part, filter active' })).toBeEnabled();
    expect(screen.getByText('Rows 0 of 0 · filtered from 200')).toBeInTheDocument();
    fireEvent.click(screen.getAllByRole('button', { name: 'Clear filters (1)' })[0]);
    await waitFor(() => expect(mock.setFilters).toHaveBeenCalledTimes(2));
    expect(mock.setFilters.mock.calls[1][0]).toEqual([]); expect(mock.acquire).toHaveBeenCalledTimes(1);
  });

  it('never paints a late old page after a newer filter view is current', async () => {
    const mock = makeClient(); mock.acquire.mockResolvedValue(op(200)); mock.page.mockResolvedValue(pageOf([row('old')]));
    setup(mock); await screen.findByText(/Rows 1–1 of 200/);
    const late = deferred<WorkspaceShipmentsPage>(); mock.page.mockReturnValueOnce(late.promise);
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    mock.setFilters.mockResolvedValue(viewOf(1, [{ column: 'itemNumber', kind: 'contains', text: 'new', includeBlanks: false }], 7));
    mock.page.mockResolvedValue(pageOf([row('new', '0', 'NEW-VALUE')]));
    await containsPart('new'); await screen.findByText('NEW-VALUE');
    await act(async () => late.resolve(pageOf([row('late', '1', 'OLD-VALUE')])));
    expect(screen.queryByText('OLD-VALUE')).not.toBeInTheDocument(); expect(screen.getByText('NEW-VALUE')).toBeInTheDocument();
  });
});

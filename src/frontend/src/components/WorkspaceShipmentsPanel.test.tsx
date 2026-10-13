import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { StrictMode } from 'react';
import type { WorkspaceShipmentsAcquisition, WorkspaceShipmentsClient, WorkspaceShipmentsPage, WorkspaceShipmentsSpec } from '../api/workspaceShipmentsApi';
import { WorkspaceShipmentsPanel } from './WorkspaceShipmentsPanel';
import { formatWorkspaceShipmentsOnTimeRate } from './workspaceShipmentsPresentation';

const labels = ['Ship Date', 'Perform Date', 'Sales Order', 'Customer PO', 'Line', 'Part', 'Qty Shipped', 'Unit Price', 'Ext Price', 'Ship To', 'Ship Via', 'BOL', 'Invoice Number', 'SO Type', 'On Time'];
const ids = ['shipDate', 'performDate', 'salesOrder', 'customerPo', 'salesOrderLine', 'itemNumber', 'invoicedQuantity', 'retroUnitPrice', 'retroExtendedPrice', 'shipTo', 'shipVia', 'billOfLading', 'invoiceNumber', 'soType', 'onTime'];
const decimal = new Set(['invoicedQuantity', 'retroUnitPrice', 'retroExtendedPrice']);
const spec: WorkspaceShipmentsSpec = { maximumPageSize: 200, columns: ids.map((id, i) => ({ id, label: labels[i], kind: decimal.has(id) ? 'decimal' : 'text', provenance: 'Recorded', defaultSelected: true, monetary: false })) } as WorkspaceShipmentsSpec;
const op = (rowCount: number, extra: Partial<WorkspaceShipmentsAcquisition> = {}) => ({ operationId: 'o', state: 'ready', rowCount, columns: ids, acquiredAtUtc: '2026-10-13T12:00:00Z', unavailableEnrichmentRows: 0, onTimeNumerator: 0, onTimeDenominator: 0, ...extra }) as unknown as WorkspaceShipmentsAcquisition;
const row = (id: string, onTime: string | null = '1', qty = '-2.5') => ({ physicalDetailId: id, cells: ids.map(c => c === 'onTime' ? onTime : c === 'invoicedQuantity' ? qty : c === 'itemNumber' ? '0000123' : c === 'shipDate' ? '2026-10-13' : null) });
const pageOf = (rows: ReturnType<typeof row>[]) => ({ rows }) as unknown as WorkspaceShipmentsPage;

function makeClient() {
  return { acquire: vi.fn(), page: vi.fn(), release: vi.fn().mockResolvedValue(undefined) };
}
type Mock = ReturnType<typeof makeClient>;
const loadSpec = vi.fn();
function setup(mock: Mock = makeClient(), assignmentId = 'A') {
  const create = vi.fn(() => mock as unknown as WorkspaceShipmentsClient);
  const view = render(<WorkspaceShipmentsPanel assignmentId={assignmentId} createClient={create} loadSpec={loadSpec} />);
  return { mock, create, view };
}
const deferred = <T,>() => { let resolve!: (v: T) => void; let reject!: (e: unknown) => void; const promise = new Promise<T>((a, b) => { resolve = a; reject = b; }); return { promise, resolve, reject }; };

beforeEach(() => { vi.useFakeTimers({ toFake: ['Date'] }); vi.setSystemTime(new Date(2026, 9, 13, 15, 0)); loadSpec.mockReset(); loadSpec.mockResolvedValue(spec); });
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
    expect(screen.getByText(/Filters changed\. Apply to use them/)).toBeInTheDocument();
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
    expect(screen.getByText(/all available shipment history/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    await waitFor(() => expect(mock.acquire).toHaveBeenCalledTimes(2));
    expect(mock.acquire.mock.calls[1][0]).toEqual({ partNumber: null, shipDateFrom: null, shipDateTo: null });
    expect(screen.getByText(/Applied: All available history · All parts/)).toBeInTheDocument();
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
    expect(screen.getByText(/Rows 1–1 of 250 · Page 1 of 3/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'First' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    await screen.findByText(/Page 2 of 3/);
    expect(mock.page).toHaveBeenLastCalledWith(100, 100);
    fireEvent.click(screen.getByRole('button', { name: 'Last' }));
    await screen.findByText(/Page 3 of 3/);
    expect(mock.page).toHaveBeenLastCalledWith(200, 100);
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
    expect(screen.getByText(/1 invoice detail rows/)).toBeInTheDocument();
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
    expect(first.page).not.toHaveBeenCalled(); expect(screen.getByText(/1 invoice detail rows/)).toBeInTheDocument();
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
});

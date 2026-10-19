import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { act } from 'react';
import userEvent from '@testing-library/user-event';
import { OpenOrdersPanel } from './OpenOrdersPanel';
import type { OpenOrdersResponseDto } from '../api/client';
import type { OrderLine } from '../openOrders/report';
import { ApiError } from '../api/client';
import { columnPointer } from '../openOrders/pointerTestSupport';
import { OPEN_ORDERS_PAGE_SIZE_KEY } from '../openOrders/pageSize';

const get = vi.fn(); const refresh = vi.fn(); const exportReport = vi.fn(); const exportQxtend = vi.fn(); const save = vi.fn(); const saveCsv = vi.fn(); const restoreDraft = vi.fn(); const draftPresence = vi.fn(); const saveDraft = vi.fn(); const deleteDraft = vi.fn();
vi.mock('../api/tauri-bridge', () => ({ resolveBackendBaseUrl: () => Promise.resolve('http://localhost'), isRunningInTauri: () => false }));
vi.mock('../api/client', async (original) => ({ ...(await original<typeof import('../api/client')>()),
  ApiClient: class { getOpenOrders = get; refreshOpenOrders = refresh; exportOpenOrdersReport = exportReport; exportOpenOrdersQxtend = exportQxtend;
    restoreOpenOrdersDraft = restoreDraft; getOpenOrdersDraftPresence = draftPresence; saveOpenOrdersDraft = saveDraft; deleteOpenOrdersDraft = deleteDraft; },
}));
vi.mock('../longTermShortages/saveLongTermShortagesWorkbook', () => ({ saveLongTermShortagesWorkbook: (...args: unknown[]) => save(...args) }));
vi.mock('../openOrders/saveQxtendCsv', () => ({ saveQxtendCsv: (...args: unknown[]) => saveCsv(...args) }));
// Panel tests isolate the existing analytical save state machine; the integration suite uses it for real.
vi.mock('./AnalyticalExcelExport', () => ({ AnalyticalExcelExport: ({ request, disabledReason, scopeLabel }: {
  request: () => import('../api/analyticalExportApi').AnalyticalRequest; disabledReason?: string; scopeLabel: string;
}) => <><span>{scopeLabel}</span><button disabled={!!disabledReason} onClick={() => { const r = request(); void exportReport(r.workspaceId, r.openOrders); }}>Export</button></> }));

function response(count: number, isStale = false): OpenOrdersResponseDto {
  const template: OrderLine = { key: { domain: 'D', salesOrder: 'SO-1', line: 1 }, itemNumber: 'P-1', site: 'SW', purchaseOrder: 'PO', stat: 'A',
    shippedQty: 1, sourceValues: { dueDate: '2027-01-01', performDate: null, requiredDate: null, dockDate: null, orderQty: 3, price: '0.125' },
    planningValues: { dueDate: '2027-01-01', performDate: null, requiredDate: null, dockDate: null, orderQty: '3', price: '0.125' }, shippedQtyText: '1',
    numericText: { open: '2', extPrice: '0.250', unitPrice: '0', allocated: null, picked: null, siteQoh: null },
    open: 2, extPrice: '0.250', unitPrice: 0, allocated: null, customer: 'C1', customerName: 'Acme', salesperson: 'SP',
    customerPart: null, ios: 'IOS', lineComments: '', lineHold: null, partials: null, picked: null, plnr: null, prodStat: null,
    productLine: 'B', qaHold: null, remarks: null, revision: null, shipAcct: null, shipTo: null, shipVia: null,
    siteQoh: null, soHoldStatus: null, soType: null, consignment: true };
  return { workspaceId: 'A', site: 'SW', mpsSnapshotId: 'snap-1', openOrdersSnapshotId: 'report-1', acquiredAtUtc: '2026-09-30T10:00:00Z',
    isStale, warning: isStale ? 'Source unavailable' : null,
    lines: Array.from({ length: count }, (_, i) => ({ ...template, key: { ...template.key, salesOrder: `SO-${i + 1}` } })) };
}

function paginationResponse(count = 1205): OpenOrdersResponseDto {
  const data = response(count);
  data.lines = data.lines.map((line, i) => ({ ...line, key: { ...line.key, salesOrder: `SO-${String(i + 1).padStart(4, '0')}` } }));
  return data;
}
function choosePageSize(size: number) {
  fireEvent.click(within(screen.getByRole('group', { name: 'Rows per page' })).getByRole('button', { name: String(size) }));
}

function openFilter(label: string) {
  const funnel = screen.queryByRole('button', { name: new RegExp(`^Filter ${label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}( \\(active\\))?$`) });
  if (funnel) fireEvent.click(funnel);
  else {
    fireEvent.click(screen.getByRole('button', { name: /^Filters(?: \(\d+\))?$/ }));
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: new RegExp(`^${label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}(?: \\(| •|$)`) }));
  }
}
function applyTextFilter(type: string, value: string) {
  const label = { customerName: 'Customer Name', customer: 'Customer #', salesperson: 'Salesperson', ios: 'IOS', so: 'SO', po: 'PO', itemNumber: 'Item Number' }[type]!;
  openFilter(label); fireEvent.click(screen.getByRole('button', { name: 'Contains' }));
  fireEvent.change(screen.getByLabelText(`${label} contains`), { target: { value } });
  if (screen.getByLabelText('Include blank values').getAttribute('checked') !== null || (screen.getByLabelText('Include blank values') as HTMLInputElement).checked) fireEvent.click(screen.getByLabelText('Include blank values'));
  fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
}
function clearColumnFilter(label: string) {
  openFilter(label); fireEvent.click(screen.getByRole('button', { name: 'Clear filter' }));
}
function togglePlan() {
  fireEvent.click(screen.getByRole('button', { name: screen.getByRole('button', { name: 'Plan' }).getAttribute('aria-pressed') === 'true' ? 'Report' : 'Plan' }));
}

beforeEach(() => { get.mockReset(); refresh.mockReset(); exportReport.mockReset(); exportQxtend.mockReset(); saveCsv.mockReset(); draftPresence.mockReset(); save.mockReset(); restoreDraft.mockReset(); saveDraft.mockReset(); deleteDraft.mockReset();
  draftPresence.mockResolvedValue({ exists: false });
  restoreDraft.mockResolvedValue({ exists: false, restored: false, warning: null, freshReport: null, rows: [] });
  saveDraft.mockResolvedValue({ exists: true, restored: true, warning: null, freshReport: null, rows: [] }); deleteDraft.mockResolvedValue(undefined);
  window.localStorage.clear(); });
const originalHitTest = Object.getOwnPropertyDescriptor(document, 'elementFromPoint');
afterEach(() => {
  vi.useRealTimers();
  if (originalHitTest) Object.defineProperty(document, 'elementFromPoint', originalHitTest);
  else Reflect.deleteProperty(document, 'elementFromPoint');
});

describe('Customer Open Orders panel', () => {
  it('pages the complete 1205-row report at all three sizes, retaining the previous first row and partial-page ranges', async () => {
    const data = paginationResponse(); get.mockResolvedValue(data);
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />); await screen.findByRole('table');
    const sizes = screen.getByRole('group', { name: 'Rows per page' });
    expect(within(sizes).getAllByRole('button').map(b => b.textContent)).toEqual(['100', '500', '1000']);
    expect(within(sizes).getByRole('button', { name: '100' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByText(/Rows 1–100 of 1205/)).toBeInTheDocument();
    for (let i = 0; i < 11; i++) fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    expect(screen.getByText(/Rows 1101–1200 of 1205/)).toBeInTheDocument();
    const fiveHundred = within(sizes).getByRole('button', { name: '500' }); fiveHundred.focus();
    await userEvent.setup().keyboard('{Enter}');
    expect(fiveHundred).toHaveFocus(); expect(fiveHundred).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByText('Page 3 of 3')).toBeInTheDocument();
    expect(screen.getByText(/Rows 1001–1205 of 1205/)).toBeInTheDocument();
    expect(screen.getByText('SO-1101')).toBeInTheDocument();
    expect(within(screen.getByRole('table')).getAllByRole('row')).toHaveLength(206);
    choosePageSize(1000);
    expect(screen.getByText('Page 2 of 2')).toBeInTheDocument();
    expect(screen.getByText(/Rows 1001–1205 of 1205/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Next page' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Previous page' }));
    expect(screen.getByText(/Rows 1–1000 of 1205/)).toBeInTheDocument();
    expect(within(screen.getByRole('table')).getAllByRole('row')).toHaveLength(1001);
    fireEvent.click(screen.getByRole('button', { name: 'Next page' })); choosePageSize(100);
    expect(screen.getByText('Page 11 of 13')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next page' })); fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    expect(screen.getByText(/Rows 1201–1205 of 1205/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Next page' })).toBeDisabled();
    expect(get).toHaveBeenCalledTimes(1); expect(refresh).not.toHaveBeenCalled(); expect(saveDraft).not.toHaveBeenCalled();
  }, 30_000);
  it('clamps later pages for search/filter, empty results, refresh and mode/changed-only changes without losing the selected size', async () => {
    get.mockResolvedValue(paginationResponse()); refresh.mockResolvedValue(paginationResponse(7));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />); await screen.findByRole('table');
    choosePageSize(1000); fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    fireEvent.change(screen.getByRole('searchbox', { name: 'Search open orders' }), { target: { value: 'SO-1205' } });
    expect(screen.getByText(/Rows 1–1 of 1/)).toBeInTheDocument(); expect(screen.getByText('Page 1 of 1')).toBeInTheDocument();
    fireEvent.change(screen.getByRole('searchbox', { name: 'Search open orders' }), { target: { value: 'absent' } });
    expect(screen.getByText('No open orders match the current filters.')).toBeInTheDocument();
    expect(screen.getByText(/Rows 0–0 of 0/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Previous page' })).toBeDisabled(); expect(screen.getByRole('button', { name: 'Next page' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Clear search and filters' }));
    fireEvent.click(screen.getByRole('button', { name: 'Next page' })); applyTextFilter('so', 'SO-120');
    expect(screen.getByText(/Rows 1–6 of 6/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear search and filters' }));
    fireEvent.click(screen.getByRole('button', { name: 'Next page' })); togglePlan();
    expect(screen.getByText('Page 2 of 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show changed rows' }));
    expect(screen.getByText('Page 1 of 1')).toBeInTheDocument(); expect(screen.getByText(/Rows 0–0 of 0/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show changed rows' })); togglePlan();
    choosePageSize(500); fireEvent.click(screen.getByRole('button', { name: 'Next page' })); fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    await screen.findByText(/7 scoped lines/); expect(screen.getByText(/Rows 1–7 of 7/)).toBeInTheDocument();
    expect(screen.getByText('Page 1 of 1')).toBeInTheDocument();
    expect(within(screen.getByRole('group', { name: 'Rows per page' })).getByRole('button', { name: '500' })).toHaveAttribute('aria-pressed', 'true');
    expect(get).toHaveBeenCalledTimes(1); expect(refresh).toHaveBeenCalledTimes(1);
  }, 60_000);
  it('preserves exact proposals, in-progress inputs and drafts, with correct hidden counts and Find row at 500 and 1000', async () => {
    const data = paginationResponse(); data.lines[0].planningValues.price = '12.3456789012';
    get.mockResolvedValue(data); render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />); await screen.findByRole('table'); togglePlan();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-0001/1' }), { target: { value: '4' } });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Save Draft' }));
    await waitFor(() => expect(saveDraft).toHaveBeenCalledTimes(1));
    expect(saveDraft.mock.calls[0][1].proposals[0].proposed.price).toBe('12.3456789012');
    await act(async () => { choosePageSize(500); });
    expect(saveDraft).toHaveBeenCalledTimes(1);
    await act(async () => { choosePageSize(100); });
    expect(saveDraft).toHaveBeenCalledTimes(1);
    fireEvent.change(screen.getByRole('textbox', { name: 'Due Date proposal SO-0001/1' }), { target: { value: '1/' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Price proposal SO-0001/1' }), { target: { value: '.' } });
    choosePageSize(500); fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-0601/1' }), { target: { value: '5' } });
    expect(screen.getByText(/1 outside current filter\/page view/)).toBeInTheDocument();
    choosePageSize(1000);
    expect(screen.getByText(/0 outside current filter\/page view/)).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Due Date proposal SO-0001/1' })).toHaveValue('1/');
    expect(screen.getByRole('textbox', { name: 'Price proposal SO-0001/1' })).toHaveValue('.');
    fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    expect(screen.getByText(/2 outside current filter\/page view/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Show changes outside this view' }));
    fireEvent.click(screen.getAllByRole('button', { name: 'Find row' })[1]);
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument();
    expect(document.activeElement).toHaveAttribute('data-order-row', JSON.stringify(['D', 'SO-0601', 1]));
    choosePageSize(500);
    expect(screen.getByText(/1 outside current filter\/page view/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Find row' }));
    expect(screen.getByText(/Rows 501–1000 of 1205/)).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-0601/1' })).toHaveValue('5');
    choosePageSize(1000);
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-0001/1' })).toHaveValue('4');
    expect(screen.getByRole('textbox', { name: 'Due Date proposal SO-0001/1' })).toHaveValue('1/');
    expect(saveDraft).toHaveBeenCalledTimes(1); expect(deleteDraft).not.toHaveBeenCalled();
    expect(get).toHaveBeenCalledTimes(1); expect(refresh).not.toHaveBeenCalled();
  }, 60_000);
  it('shares its validated preference across Report/Plan and workspaces, and works when preference storage is unavailable', async () => {
    localStorage.setItem(OPEN_ORDERS_PAGE_SIZE_KEY, '500'); get.mockResolvedValue(paginationResponse(2));
    const ui = render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />); await screen.findByRole('table');
    expect(within(screen.getByRole('group', { name: 'Rows per page' })).getByRole('button', { name: '500' })).toHaveAttribute('aria-pressed', 'true');
    togglePlan(); choosePageSize(1000);
    expect(localStorage.getItem(OPEN_ORDERS_PAGE_SIZE_KEY)).toBe('1000');
    ui.rerender(<OpenOrdersPanel key="B" assignmentId="B" snapshotId="snap-1" />); await screen.findByRole('table');
    expect(within(screen.getByRole('group', { name: 'Rows per page' })).getByRole('button', { name: '1000' })).toHaveAttribute('aria-pressed', 'true');
    ui.unmount();
    const read = vi.spyOn(localStorage, 'getItem').mockImplementation(() => { throw new Error('Unavailable'); });
    const write = vi.spyOn(localStorage, 'setItem').mockImplementation(() => { throw new Error('Unavailable'); });
    try {
      render(<OpenOrdersPanel assignmentId="C" snapshotId="snap-1" />); await screen.findByRole('table');
      expect(within(screen.getByRole('group', { name: 'Rows per page' })).getByRole('button', { name: '100' })).toHaveAttribute('aria-pressed', 'true');
      choosePageSize(500);
      expect(within(screen.getByRole('group', { name: 'Rows per page' })).getByRole('button', { name: '500' })).toHaveAttribute('aria-pressed', 'true');
    } finally { read.mockRestore(); write.mockRestore(); }
  });
  it('keeps funnel and popup pointer/keyboard input out of column dragging, with focus containment and return', async () => {
    get.mockResolvedValue(response(1)); render(<div className="shell" data-theme="dark"><OpenOrdersPanel assignmentId="A" snapshotId="snap-1" /></div>);
    await screen.findByRole('table');
    const funnel = screen.getByRole('button', { name: 'Filter SO' });
    columnPointer('pointerdown', funnel, 200, 30); columnPointer('pointermove', document, 400, 30); columnPointer('pointerup', document, 400, 30);
    expect(document.documentElement).not.toHaveClass('orders-column-dragging');
    const before = screen.getAllByRole('columnheader').map(h => h.textContent);
    fireEvent.keyDown(funnel, { altKey: true, key: 'ArrowRight' });
    expect(screen.getAllByRole('columnheader').map(h => h.textContent)).toEqual(before);
    fireEvent.click(funnel);
    const dialog = screen.getByRole('dialog', { name: 'Filter SO' });
    expect(dialog.closest('.shell')).toHaveAttribute('data-theme', 'dark');
    expect(screen.getByRole('region', { name: 'Scrollable Customer Open Orders report' })).not.toContainElement(dialog);
    await screen.findByLabelText('Value: SO-1');
    fireEvent.pointerDown(screen.getByLabelText('Search values'));
    fireEvent.change(screen.getByLabelText('Search values'), { target: { value: 'nothing' } });
    const apply = screen.getByRole('button', { name: 'Apply' }); apply.focus(); fireEvent.keyDown(apply, { key: 'Tab' });
    expect(screen.getByRole('button', { name: 'Close Filter SO' })).toHaveFocus();
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' })); expect(funnel).toHaveFocus();
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    fireEvent.keyDown(screen.getByRole('columnheader', { name: 'SO' }), { altKey: true, key: 'ArrowRight' });
    expect(screen.getAllByRole('columnheader')[2]).toHaveAccessibleName('SO');
  });
  it('filters against retrieved exact Plan values without removing edited rows, and generates all workspace proposals outside filters', async () => {
    const data = response(2); get.mockResolvedValue(data); exportQxtend.mockResolvedValue({ files: [{ kind: 'quantity', fileName: 'UpdateQuantities.csv', contentBase64: 'TQ==' }] });
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />); await screen.findByRole('table'); togglePlan();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    fireEvent.change(screen.getByRole('combobox', { name: 'Reason Code SO-1/1' }), { target: { value: 'Planning' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-2/1' }), { target: { value: '5' } });
    fireEvent.change(screen.getByRole('combobox', { name: 'Reason Code SO-2/1' }), { target: { value: 'Planning' } });
    openFilter('Order Qty'); fireEvent.change(screen.getByLabelText('Value'), { target: { value: '3.0000000000' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('4');
    expect(screen.getByText(/Filters and value counts use retrieved values/)).toBeInTheDocument();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '6' } });
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    applyTextFilter('so', 'SO-2');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show changed rows' }));
    fireEvent.click(screen.getByRole('button', { name: 'Generate files…' }));
    await waitFor(() => expect(exportQxtend).toHaveBeenCalledTimes(1));
    expect(exportQxtend.mock.calls[0][1].proposals.map((p: { key: { salesOrder: string } }) => p.key.salesOrder)).toEqual(['SO-1', 'SO-2']);
    fireEvent.click(screen.getByRole('button', { name: 'Show changes outside this view' }));
    fireEvent.click(screen.getByRole('button', { name: 'Find row' }));
    expect(screen.getByRole('checkbox', { name: 'Show changed rows' })).not.toBeChecked();
    expect(screen.getByRole('button', { name: 'Filters' })).toBeInTheDocument();
    expect(document.activeElement).toHaveAttribute('data-order-row', JSON.stringify(['D', 'SO-1', 1]));
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('6');
  });
  it('retains a filter while hiding its column, switching a saved columns-only view, and clearing columns without clearing search', async () => {
    get.mockResolvedValue(response(2)); render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />); await screen.findByRole('table');
    applyTextFilter('so', 'SO-1');
    fireEvent.click(screen.getByRole('button', { name: /Columns 9 of/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Hide SO' }));
    fireEvent.click(screen.getByRole('button', { name: 'Save as new' }));
    fireEvent.change(screen.getByRole('textbox', { name: 'View name' }), { target: { value: 'Hidden SO' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save view' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    expect(screen.queryByRole('button', { name: 'Filter SO (active)' })).not.toBeInTheDocument();
    fireEvent.change(screen.getByRole('searchbox', { name: 'Search open orders' }), { target: { value: 'SP' } });
    openFilter('SO'); expect(screen.getByLabelText('SO contains')).toHaveValue('SO-1');
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /View: Hidden SO/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Default' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    expect(screen.getByRole('button', { name: 'Filter SO (active)' })).toBeInTheDocument();
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /View: Default/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Hidden SO' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    expect(screen.queryByRole('button', { name: 'Filter SO (active)' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Filters (1)' }));
    fireEvent.click(screen.getByRole('button', { name: 'Clear column filters (keep quick search)' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    expect(screen.getByRole('searchbox', { name: 'Search open orders' })).toHaveValue('SP');
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
  });
  it('keeps the accepted Contains blank default and explicitly blocks numeric filters against an old sidecar contract', async () => {
    const data = response(2); data.lines[1].customerName = null;
    for (const line of data.lines) Reflect.deleteProperty(line, 'numericText');
    get.mockResolvedValue(data); render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />); await screen.findByRole('table');
    openFilter('Customer Name'); fireEvent.click(screen.getByRole('button', { name: 'Contains' }));
    expect(screen.getByLabelText('Include blank values')).not.toBeChecked();
    fireEvent.change(screen.getByLabelText('Customer Name contains'), { target: { value: 'Acme' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    openFilter('Open'); fireEvent.change(screen.getByLabelText('Value'), { target: { value: '2' } });
    expect(screen.getByRole('alert')).toHaveTextContent('Exact report values unavailable');
    expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
  });
  it('reorders Plan headers with pointer capture without changing proposals or generation guards', async () => {
    get.mockResolvedValue(response(1)); render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table'); togglePlan();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    fireEvent.change(screen.getByRole('combobox', { name: 'Reason Code SO-1/1' }), { target: { value: 'Planning' } });
    const source = screen.getByRole('columnheader', { name: 'Price' }); const target = screen.getByRole('columnheader', { name: 'SO' });
    expect(source).toHaveAttribute('draggable', 'false');
    Object.defineProperty(document, 'elementFromPoint', { configurable: true, value: () => target });
    columnPointer('pointerdown', source, 400, 30); columnPointer('pointermove', source, 50, 30); columnPointer('pointerup', source, 50, 30);
    expect(screen.getAllByRole('columnheader')[0]).toHaveTextContent('Price');
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('4');
    expect(screen.getByRole('combobox', { name: 'Reason Code SO-1/1' })).toHaveValue('Planning');
    expect(screen.getByRole('button', { name: 'Generate files…' })).toBeEnabled();
    expect(exportQxtend).not.toHaveBeenCalled();
    fireEvent.keyDown(source, { altKey: true, key: 'ArrowRight' });
    expect(screen.getAllByRole('columnheader')[1]).toHaveTextContent('Price');
  });
  it('preserves proposals through quick search/chip clearing, exposes hidden changes, Find row and Undo', async () => {
    get.mockResolvedValue(response(101)); render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table'); togglePlan();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    fireEvent.change(screen.getByRole('searchbox', { name: 'Search open orders' }), { target: { value: 'no-match' } });
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument(); expect(screen.getByText('No open orders match the current filters.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Show changes outside this view' }));
    fireEvent.click(screen.getByRole('button', { name: 'Find row' }));
    expect(screen.getByRole('searchbox', { name: 'Search open orders' })).toHaveValue('');
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('4');
    applyTextFilter('so', 'SO-10'); expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear search and filters' }));
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('4');
    fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    fireEvent.click(screen.getByRole('button', { name: 'Undo SO-1/1' })); expect(screen.getByText(/0 changed across workspace/)).toBeInTheDocument();
  }, 15_000);
  it('resets/clamps pages on search and returns focus from filter overlay without changing proposals', async () => {
    get.mockResolvedValue(response(205)); render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table'); fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    fireEvent.change(screen.getByRole('searchbox', { name: 'Search open orders' }), { target: { value: 'SO-205' } });
    expect(screen.getByText(/Rows 1–1 of 1/)).toBeInTheDocument(); expect(screen.getByText('Page 1 of 1')).toBeInTheDocument();
    const trigger = screen.getByRole('button', { name: 'Filter SO' }); fireEvent.click(trigger);
    expect(screen.getByRole('dialog', { name: 'Filter SO' })).toBeInTheDocument();
    fireEvent.keyDown(document.activeElement!, { key: 'Escape' }); expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(trigger).toHaveFocus();
    fireEvent.click(trigger); fireEvent.pointerDown(document.body); expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(trigger).toHaveFocus();
  });
  it('deduplicates operational generation, rejects late mode-departure preparation and retains generation guards', async () => {
    let finish!: (value: unknown) => void;
    get.mockResolvedValue(response(1)); exportQxtend.mockImplementation(() => new Promise(resolve => { finish = resolve; }));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />); await screen.findByRole('table'); togglePlan();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    expect(screen.getByRole('button', { name: 'Generate files…' })).toBeDisabled();
    fireEvent.change(screen.getByRole('combobox', { name: 'Reason Code SO-1/1' }), { target: { value: 'Planning' } });
    const generate = screen.getByRole('button', { name: 'Generate files…' }); fireEvent.click(generate); fireEvent.click(generate);
    await waitFor(() => expect(exportQxtend).toHaveBeenCalledTimes(1)); togglePlan();
    await act(async () => finish({ files: [{ kind: 'quantity', fileName: 'UpdateQuantities.csv', contentBase64: 'TQ==' }] }));
    togglePlan(); expect(screen.queryByRole('group', { name: 'Prepared QXtend files' })).not.toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('4'); expect(saveCsv).not.toHaveBeenCalled();
  });
  it('keeps saved draft until explicit discard confirmation, supports Escape/focus return and preserves in-memory proposals', async () => {
    const data = response(1); const original = data.lines[0].planningValues;
    get.mockResolvedValue(data); draftPresence.mockResolvedValue({ exists: true });
    restoreDraft.mockResolvedValue({ exists: true, restored: true, warning: null, freshReport: data,
      rows: [{ proposal: { key: data.lines[0].key, site: 'SW', itemNumber: 'P-1', original, proposed: { ...original, orderQty: '8' }, reasonCode: 'Planning' }, issues: [] }] });
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    const discard = await screen.findByRole('button', { name: 'Discard saved draft' });
    togglePlan(); fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    expect(screen.getByRole('button', { name: 'Generate files…' })).toBeDisabled();
    fireEvent.click(discard); fireEvent.keyDown(document.activeElement!, { key: 'Escape' });
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument(); expect(discard).toHaveFocus(); expect(deleteDraft).not.toHaveBeenCalled();
    fireEvent.click(discard); fireEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Discard saved draft' }));
    await waitFor(() => expect(deleteDraft).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Restore saved draft and continue saving' })).not.toBeInTheDocument());
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('4');
  });
  it('does not revive an operational generation after Plan → Report → Plan before its reply', async () => {
    let finish!: (value: unknown) => void;
    get.mockResolvedValue(response(1)); exportQxtend.mockImplementation(() => new Promise(resolve => { finish = resolve; }));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />); await screen.findByRole('table'); togglePlan();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    fireEvent.change(screen.getByRole('combobox', { name: 'Reason Code SO-1/1' }), { target: { value: 'Planning' } });
    fireEvent.click(screen.getByRole('button', { name: 'Generate files…' })); await waitFor(() => expect(exportQxtend).toHaveBeenCalledTimes(1));
    togglePlan(); togglePlan();
    await act(async () => finish({ files: [{ kind: 'quantity', fileName: 'UpdateQuantities.csv', contentBase64: 'TQ==' }] }));
    expect(screen.queryByRole('group', { name: 'Prepared QXtend files' })).not.toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('4'); expect(saveCsv).not.toHaveBeenCalled();
  });
  it('keeps quantity unpadded and price at four places on focus, preserving typed decimal text and exact originals', async () => {
    const data = response(1);
    data.lines[0].planningValues = { ...data.lines[0].planningValues, orderQty: '3.0000000000', price: '0.1250000000' };
    get.mockResolvedValue(data);
    exportQxtend.mockResolvedValue({ files: [{ kind: 'price', fileName: 'UpdatePrices.csv', contentBase64: 'TQ==' }] });
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    togglePlan();
    const qty = screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' });
    const price = screen.getByRole('textbox', { name: 'Price proposal SO-1/1' });
    expect(qty).toHaveValue('3');
    expect(price).toHaveValue('0.1250');
    fireEvent.focus(qty);
    expect(qty).toHaveValue('3');
    fireEvent.change(qty, { target: { value: '3.' } });
    expect(qty).toHaveValue('3.');
    fireEvent.change(qty, { target: { value: '3.25' } });
    fireEvent.blur(qty);
    expect(qty).toHaveValue('3.25');
    fireEvent.focus(price);
    expect(price).toHaveValue('0.1250');
    fireEvent.change(price, { target: { value: '' } }); expect(price).toHaveValue('');
    fireEvent.change(price, { target: { value: '0.' } }); expect(price).toHaveValue('0.');
    fireEvent.change(price, { target: { value: '0.1256' } });
    fireEvent.blur(price);
    expect(price).toHaveValue('0.1256');
    fireEvent.change(screen.getByRole('combobox', { name: 'Reason Code SO-1/1' }), { target: { value: 'Planning' } });
    fireEvent.click(screen.getByRole('button', { name: 'Generate files…' }));
    await waitFor(() => expect(exportQxtend).toHaveBeenCalledTimes(1));
    expect(exportQxtend.mock.calls[0][1].proposals[0].original).toMatchObject({ orderQty: '3.0000000000', price: '0.1250000000' });
    expect(exportQxtend.mock.calls[0][1].proposals[0].proposed).toMatchObject({ orderQty: '3.25', price: '0.1256' });
  });

  it('shows four places for a ten-decimal source price but sends its untouched original on date-only export', async () => {
    const data = response(1);
    data.lines[0].planningValues = { ...data.lines[0].planningValues, orderQty: '3.0000000000', price: '12.3456789012' };
    get.mockResolvedValue(data);
    exportQxtend.mockResolvedValue({ files: [{ kind: 'date', fileName: 'DateChange.csv', contentBase64: 'TQ==' }] });
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    togglePlan();
    const qty = screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' });
    fireEvent.focus(qty); expect(qty).toHaveValue('3'); fireEvent.blur(qty);
    const price = screen.getByRole('textbox', { name: 'Price proposal SO-1/1' });
    expect(price).toHaveValue('12.3457');
    fireEvent.focus(price);
    expect(price).toHaveValue('12.3457');
    fireEvent.blur(price);
    expect(price).toHaveValue('12.3457');
    fireEvent.change(screen.getByRole('textbox', { name: 'Due Date proposal SO-1/1' }), { target: { value: '1/2/2027' } });
    fireEvent.change(screen.getByRole('combobox', { name: 'Reason Code SO-1/1' }), { target: { value: 'Planning' } });
    fireEvent.click(screen.getByRole('button', { name: 'Generate files…' }));
    await waitFor(() => expect(exportQxtend).toHaveBeenCalledTimes(1));
    expect(exportQxtend.mock.calls[0][1].proposals[0].original.price).toBe('12.3456789012');
    expect(exportQxtend.mock.calls[0][1].proposals[0].proposed.price).toBe('12.3456789012');
    expect(exportQxtend.mock.calls[0][1].proposals[0].proposed.orderQty).toBe('3.0000000000');
  });

  it('does not mark dirty or save a draft merely by focusing numbers; meaningful fractional quantities remain', async () => {
    const data = response(1); const dirty = vi.fn();
    data.lines[0].planningValues = { ...data.lines[0].planningValues, orderQty: '3.2500000000', price: '12.3456789012' };
    get.mockResolvedValue(data); render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" onDirtyChange={dirty} />);
    await screen.findByRole('table'); togglePlan();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Save Draft' }));
    await waitFor(() => expect(saveDraft).toHaveBeenCalledTimes(1));
    const qty = screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' });
    const price = screen.getByRole('textbox', { name: 'Price proposal SO-1/1' });
    fireEvent.focus(qty); expect(qty).toHaveValue('3.25'); fireEvent.blur(qty);
    fireEvent.focus(price); expect(price).toHaveValue('12.3457'); fireEvent.blur(price);
    expect(saveDraft).toHaveBeenCalledTimes(1); expect(dirty).not.toHaveBeenCalledWith(true);
    expect(screen.getByRole('button', { name: 'Generate files…' })).toBeDisabled();
  });

  it('does not let the numeric editing buffer obscure an undone proposal', async () => {
    get.mockResolvedValue(response(1)); render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table'); togglePlan();
    const qty = screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' });
    fireEvent.focus(qty); fireEvent.change(qty, { target: { value: '4.' } }); expect(qty).toHaveValue('4.');
    fireEvent.click(screen.getByRole('button', { name: 'Undo SO-1/1' })); expect(qty).toHaveValue('3');
    expect(screen.getByRole('button', { name: 'Generate files…' })).toBeDisabled();
  });

  it('keeps staged date-only proposal and displays old-backend route failure without guessing a validation conflict', async () => {
    get.mockResolvedValue(response(1));
    exportQxtend.mockRejectedValue(new ApiError(404, '/qxtend-export', ''));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    togglePlan();
    fireEvent.change(screen.getByRole('textbox', { name: 'Due Date proposal SO-1/1' }), { target: { value: '1/2/2027' } });
    fireEvent.change(screen.getByRole('combobox', { name: 'Reason Code SO-1/1' }), { target: { value: 'Planning' } });
    fireEvent.click(screen.getByRole('button', { name: 'Generate files…' }));
    await screen.findByText(/QXtend export endpoint is unavailable in the running backend/);
    expect(exportQxtend.mock.calls[0][1].proposals).toHaveLength(1);
    expect(exportQxtend.mock.calls[0][1].proposals[0].reasonCode).toBe('Planning');
    expect(screen.getByRole('textbox', { name: 'Due Date proposal SO-1/1' })).toHaveValue('1/2/2027');
    expect(screen.queryByRole('group', { name: 'Prepared QXtend files' })).not.toBeInTheDocument();
  });

  it('validates all staged workspace changes and retains separate Save As statuses for cancellation, failure and retry', async () => {
    get.mockResolvedValue(response(2));
    exportQxtend.mockResolvedValue({ files: [
      { kind: 'quantity', fileName: 'UpdateQuantities.csv', contentBase64: 'TQ==' },
      { kind: 'price', fileName: 'UpdatePrices.csv', contentBase64: 'TQ==' },
    ] });
    saveCsv.mockResolvedValueOnce({ kind: 'saved', fileName: 'Chosen.csv' })
      .mockResolvedValueOnce({ kind: 'cancelled' }).mockRejectedValueOnce(new Error('disk full'))
      .mockResolvedValueOnce({ kind: 'saved', fileName: 'Second.csv' });
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByText(/2 scoped lines/);
    togglePlan();
    for (const order of ['SO-1', 'SO-2']) {
      fireEvent.change(screen.getByRole('textbox', { name: `Order Qty proposal ${order}/1` }), { target: { value: '4' } });
      fireEvent.change(screen.getByRole('combobox', { name: `Reason Code ${order}/1` }), { target: { value: 'Planning' } });
    }
    applyTextFilter('so', 'SO-1');
    fireEvent.click(screen.getByRole('button', { name: 'Generate files…' }));
    await waitFor(() => expect(exportQxtend).toHaveBeenCalledTimes(1));
    expect(exportQxtend.mock.calls[0][1].proposals).toHaveLength(2);
    const quantity = await screen.findByRole('button', { name: 'Save As UpdateQuantities.csv' });
    fireEvent.click(quantity);
    await screen.findByText('Saved Chosen.csv');
    const price = screen.getByRole('button', { name: 'Save As UpdatePrices.csv' });
    fireEvent.click(price);
    await screen.findByText('Save cancelled; file remains ready.');
    fireEvent.click(price);
    await screen.findByText('Save failed; retry this file. Other saved files and proposals remain.');
    fireEvent.click(price);
    await screen.findByText('Saved Second.csv');
    expect(screen.getByText('Saved Chosen.csv')).toBeInTheDocument();
    expect(screen.getByText(/2 changed across workspace/)).toBeInTheDocument();
  });

  it('stages changed rows across filters and pages, keeps Report Mode layout, previews exact raw-price extension and supports undo and confirmed clear', async () => {
    get.mockResolvedValue(response(101));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByText(/101 scoped lines/);
    togglePlan();
    expect(screen.getByRole('columnheader', { name: /Perform Date/ })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'Price' })).toBeInTheDocument();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: /3.*Original: 2/ })).toBeInTheDocument(); // Open 4 - 1; extension is optional in Plan Mode
    fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
    expect(screen.getByText(/1 outside current filter\/page view/)).toBeInTheDocument();
    togglePlan();
    expect(screen.getAllByRole('columnheader')).toHaveLength(9);
    togglePlan();
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
    togglePlan();
    const headers = () => within(screen.getByRole('table')).getAllByRole('columnheader').map(h => h.textContent?.replace(/⠿/g, '').trim());
    expect(headers().slice(0, 11)).toEqual(['SO', 'PO', 'Line', 'Item Number', 'Open', 'Due Date', 'Perform Date', 'Required Date', 'Dock Date', 'Order Qty', 'Price']);
    expect(headers().slice(11)).toEqual(['Reason Code', 'Planning status']);
    fireEvent.click(screen.getByRole('button', { name: /Columns 11 of/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Show Ext Price' }));
    fireEvent.click(screen.getByRole('button', { name: 'Move Ext Price up' }));
    fireEvent.click(screen.getByRole('button', { name: 'Save as new' }));
    fireEvent.change(screen.getByRole('textbox', { name: 'View name' }), { target: { value: 'My plan' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save view' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    expect(headers()).toContain('Ext Price');
    expect(screen.getByText('0.25')).toBeInTheDocument();
    togglePlan();
    expect(headers().slice(0, 9)).toEqual(['Due Date', 'SO', 'PO', 'Line', 'Item Number', 'Site', 'Open', 'Status', 'Ext Price']);
    togglePlan();
    expect(headers()).toContain('Ext Price');
    rerender(<OpenOrdersPanel key="B" assignmentId="B" snapshotId="snap-1" />);
    await screen.findByRole('table');
    togglePlan();
    expect(headers()).not.toContain('Ext Price');
    rerender(<OpenOrdersPanel key="A-again" assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    togglePlan();
    expect(headers()).toContain('Ext Price');
    fireEvent.click(screen.getByRole('button', { name: /Columns 12 of/ }));
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
    vi.spyOn(window, 'confirm').mockImplementation((() => Promise.resolve(true)) as unknown as typeof window.confirm);
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    expect(await screen.findByRole('button', { name: 'Restore saved draft and continue saving' })).toBeInTheDocument();
    expect(restoreDraft).toHaveBeenCalledWith('A', 'snap-1');
    fireEvent.click(screen.getByRole('button', { name: 'Restore saved draft and continue saving' }));
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    expect(screen.getByText(/1 conflicts/)).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Save Draft' })).toBeChecked();
    // Desktop-compatible: window.confirm is asynchronous there and must not decide anything.
    fireEvent.click(screen.getByRole('checkbox', { name: 'Save Draft' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Save Draft' })); // repeated request: still one dialog
    expect(screen.getAllByRole('alertdialog')).toHaveLength(1);
    expect(screen.getByRole('alertdialog')).toHaveTextContent('Turn Save Draft off and delete the saved copy? In-memory changes will remain.');
    expect(deleteDraft).not.toHaveBeenCalled();
    // Cancel: nothing deleted, setting unchanged, edits kept.
    fireEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Cancel' }));
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    expect(deleteDraft).not.toHaveBeenCalled();
    expect(screen.getByRole('checkbox', { name: 'Save Draft' })).toBeChecked();
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    // Confirm: deletes once.
    fireEvent.click(screen.getByRole('checkbox', { name: 'Save Draft' }));
    fireEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Turn off and delete' }));
    await waitFor(() => expect(deleteDraft).toHaveBeenCalledWith('A'));
    expect(deleteDraft).toHaveBeenCalledTimes(1);
    await waitFor(() => expect(screen.getByRole('checkbox', { name: 'Save Draft' })).not.toBeChecked());
    expect(screen.getByText(/1 changed across workspace/)).toBeInTheDocument();
    vi.restoreAllMocks();
  });

  it('Save Draft off: a workspace change drops the pending confirmation and nothing is deleted', async () => {
    const data = response(1); get.mockResolvedValue(data);
    draftPresence.mockResolvedValue({ exists: false });
    const { rerender } = render(<OpenOrdersPanel key="A" assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    togglePlan();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Save Draft' })); // turn on
    expect(screen.getByRole('checkbox', { name: 'Save Draft' })).toBeChecked();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Save Draft' })); // ask to turn off
    expect(screen.getByRole('alertdialog')).toBeInTheDocument();
    rerender(<OpenOrdersPanel key="B" assignmentId="B" snapshotId="snap-1" />);
    await screen.findByRole('table');
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    expect(deleteDraft).not.toHaveBeenCalled();
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
    togglePlan();
    expect(screen.queryByText('Checking saved draft…')).not.toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Save Draft' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Retry draft check' }));
    await waitFor(() => expect(draftPresence).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(screen.getByRole('checkbox', { name: 'Save Draft' })).toBeEnabled());
  });

  it('types complete dates in one field, preserves partial/invalid text, normalizes no-ops and supports Tab/Enter', async () => {
    get.mockResolvedValue(response(1));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table'); togglePlan();
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
    await screen.findByRole('table'); togglePlan();
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
    togglePlan();
    fireEvent.change(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' }), { target: { value: '4' } });
    expect(screen.getByRole('button', { name: 'Refresh report' })).toBeDisabled();
    expect(screen.queryByRole('button', { name: 'Export' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    expect(refresh).not.toHaveBeenCalled(); expect(exportReport).not.toHaveBeenCalled();
    togglePlan();
    expect(screen.getByRole('button', { name: 'Refresh report' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Export' })).toBeEnabled();
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    togglePlan();
    await waitFor(() => expect(screen.getByText(/1 conflicts/)).toBeInTheDocument());
    expect(screen.getByRole('textbox', { name: 'Order Qty proposal SO-1/1' })).toHaveValue('4');
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
    fireEvent.click(screen.getByRole('button', { name: /Columns 9 of/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Move PO up' }));
    fireEvent.click(screen.getByRole('button', { name: 'Hide Due Date' }));
    fireEvent.click(screen.getByRole('button', { name: 'Show Customer Name' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    const headers = within(screen.getByRole('table'));
    const source = headers.getByRole('columnheader', { name: /Customer Name/ });
    const target = headers.getByRole('columnheader', { name: 'SO' });
    Object.defineProperty(document, 'elementFromPoint', { configurable: true, value: () => target });
    columnPointer('pointerdown', source, 200, 30); columnPointer('pointermove', document, 50, 30); columnPointer('pointerup', document, 50, 30);
    expect(headers.getAllByRole('columnheader')[1]).toHaveTextContent('Customer Name');
    fireEvent.click(screen.getByRole('button', { name: /Columns 9 of/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Reset columns to default' }));
    fireEvent.click(screen.getByRole('button', { name: 'Move PO up' }));
    fireEvent.click(screen.getByRole('button', { name: 'Hide Due Date' }));
    fireEvent.click(screen.getByRole('button', { name: 'Show Customer Name' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
    openFilter('Customer Name'); fireEvent.click(screen.getByRole('button', { name: 'Contains' }));
    fireEvent.change(screen.getByLabelText('Customer Name contains'), { target: { value: 'acm' } });
    expect(screen.getByText(/235 matching filters/)).toBeInTheDocument(); // draft is not applied
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(screen.getByRole('button', { name: 'Filter Customer Name (active)' })).toBeInTheDocument();
    expect(screen.getByText(/235 matching filters/)).toBeInTheDocument();
    expect(screen.getByText(/Rows 1–100 of 235/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Export' }));
    await waitFor(() => expect(exportReport).toHaveBeenCalledWith('A', expect.objectContaining({ lineKeys: expect.arrayContaining([{ domain: 'D', salesOrder: 'SO-235', line: 1 }]), columns: ['po', 'order', 'line', 'itemNumber', 'site', 'open', 'stat', 'extPrice', 'customerName'] })));
    expect(exportReport.mock.calls[0][1].lineKeys).toHaveLength(235);
    rerender(<OpenOrdersPanel key="B" assignmentId="B" snapshotId="snap-1" />);
    await waitFor(() => expect(get).toHaveBeenCalledWith('B', 'snap-1'));
    await waitFor(() => expect(within(screen.getByRole('table')).getAllByRole('columnheader')[0]).toHaveTextContent('Due Date'));
  }, 15_000);

  it('marks failed refresh stale, disables export and recovers after refresh', async () => {
    get.mockResolvedValue(response(1)); refresh.mockResolvedValueOnce(response(1, true)).mockResolvedValueOnce(response(1));
    exportReport.mockResolvedValue({ blob: new Blob(), fileName: 'Open.xlsx' }); save.mockResolvedValueOnce({ kind: 'cancelled' }).mockRejectedValueOnce(new Error('disk'));
    render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    expect(await screen.findByText(/STALE report acquired/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    expect(exportReport).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    await waitFor(() => expect(screen.queryByText(/STALE report acquired/)).not.toBeInTheDocument());
    expect(screen.getByRole('button', { name: 'Export' })).toBeEnabled();
  });

  it('keeps a failed refresh explicitly not current and never shows an old report under a changed MPS snapshot', async () => {
    get.mockResolvedValue(response(1)); refresh.mockRejectedValue(new ApiError(503, 'synthetic', 'Unavailable'));
    const { rerender } = render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'Refresh report' }));
    expect(await screen.findByText(/Previous report is retained but not current/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
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
    expect(screen.getByRole('button', { name: 'Filters (4)' })).toBeInTheDocument();
    openFilter('Customer #');
    expect(screen.getByLabelText('Customer # contains')).toHaveValue('C1');
    fireEvent.change(screen.getByLabelText('Customer # contains'), { target: { value: 'C2' } });
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument(); // still applied C1
    fireEvent.submit(screen.getByRole('button', { name: 'Apply' }).closest('form')!); // Enter
    expect(screen.getByText(/0 matching filters/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Filters (4)' })).toBeInTheDocument();
    clearColumnFilter('Salesperson'); clearColumnFilter('IOS');
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    clearColumnFilter('Customer Name');
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    clearColumnFilter('Customer #');
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
    openFilter('Customer Name'); fireEvent.click(screen.getByRole('button', { name: 'Contains' }));
    expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
    expect(screen.getByRole('alert')).toHaveTextContent('Enter text to match');
    expect(screen.getByText(/4 matching filters/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' })); openFilter('Product Line');
    fireEvent.click(screen.getByRole('button', { name: 'Range' }));
    fireEvent.change(screen.getByLabelText('From'), { target: { value: 'B' } });
    fireEvent.change(screen.getByLabelText('To'), { target: { value: 'A' } });
    expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
    expect(screen.getByRole('alert')).toHaveTextContent('From must be on or before To');
    fireEvent.change(screen.getByLabelText('To'), { target: { value: 'C' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    openFilter('Due Date');
    fireEvent.change(screen.getByLabelText('From'), { target: { value: '2027-01-02' } });
    expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
    expect(screen.getByRole('alert')).toHaveTextContent('both Due Date boundaries');
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('To'), { target: { value: '2027-01-02' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(screen.getByText(/1 matching filters/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Export' }));
    await waitFor(() => expect(exportReport).toHaveBeenCalledWith('A', expect.objectContaining({ lineKeys: [data.lines[1].key] })));
    expect(data.lines[1].extPrice).toBe('0.250');
    clearColumnFilter('Due Date');
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    clearColumnFilter('Product Line');
    expect(screen.getByText(/4 matching filters/)).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1);
  });

  it('persists table-header keyboard and pointer order per workspace including hidden columns', async () => {
    get.mockResolvedValue(response(1));
    const { rerender } = render(<OpenOrdersPanel assignmentId="A" snapshotId="snap-1" />);
    const table = within(await screen.findByRole('table'));
    fireEvent.keyDown(table.getByRole('columnheader', { name: 'PO' }), { key: 'ArrowLeft', altKey: true });
    expect(table.getAllByRole('columnheader')[1]).toHaveTextContent('PO');
    fireEvent.click(screen.getByRole('button', { name: /Columns 9 of/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Hide SO' }));
    fireEvent.click(screen.getByRole('button', { name: 'Move PO up' }));
    expect(table.getAllByRole('columnheader')[0]).toHaveTextContent('PO');
    fireEvent.click(screen.getByRole('button', { name: 'Show SO' }));
    expect(table.getAllByRole('columnheader')[1]).toHaveTextContent('Due Date');
    fireEvent.click(screen.getByRole('button', { name: 'Save as new' }));
    fireEvent.change(screen.getByRole('textbox', { name: 'View name' }), { target: { value: 'Personal order' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save view' }));
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));
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
    openFilter('Due Date');
    const from = screen.getByLabelText('From'); const to = screen.getByLabelText('To');
    fireEvent.change(from, { target: { value: '2027-02-30' } });
    fireEvent.change(to, { target: { value: '2027-03-01' } });
    expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
    expect(screen.getByRole('alert')).toHaveTextContent('Enter both Due Date boundaries'); // browser date control rejects invalid date
    fireEvent.change(from, { target: { value: '2027-03-02' } });
    fireEvent.change(to, { target: { value: '2027-03-01' } });
    expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
    expect(screen.getByRole('alert')).toHaveTextContent('From must not exceed To');
    expect(screen.queryByRole('button', { name: 'Filter Due Date (active)' })).not.toBeInTheDocument();
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
    openFilter('Product Line'); fireEvent.click(screen.getByRole('button', { name: 'Range' }));
    fireEvent.change(screen.getByLabelText('From'), { target: { value: 'B' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    openFilter('Product Line'); expect(screen.getByLabelText('From')).toHaveValue('B');
    fireEvent.change(screen.getByLabelText('From'), { target: { value: '' } });
    fireEvent.change(screen.getByLabelText('To'), { target: { value: 'B' } });
    fireEvent.submit(screen.getByRole('button', { name: 'Apply' }).closest('form')!);
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    openFilter('Product Line'); expect(screen.getByLabelText('To')).toHaveValue('B');
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.getByRole('button', { name: 'Filters (2)' })).toBeInTheDocument();
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
    expect(screen.getByRole('button', { name: 'Filters (3)' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Export' }));
    await waitFor(() => expect(exportReport).toHaveBeenCalledWith('A', expect.objectContaining({ lineKeys: [data.lines[0].key, data.lines[1].key] })));
    expect(save).not.toHaveBeenCalled(); // no old direct-save helper; shared export is verified separately
    openFilter('PO');
    fireEvent.change(screen.getByLabelText('PO contains'), { target: { value: 'missing' } });
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    fireEvent.submit(screen.getByRole('button', { name: 'Apply' }).closest('form')!);
    expect(screen.getByText(/0 matching filters/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Filters (3)' })).toBeInTheDocument();
    clearColumnFilter('PO');
    expect(screen.getByText(/2 matching filters/)).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1);
  });

  // The retired five-second/direct-write feedback and departure tests are superseded by
  // OpenOrdersExport.test.tsx using the actual shared eight-second protected-save component.

  it.skipIf(!process.env.KST_STAGE13_POLISH_FIXTURE_PATH)('writes an offline synthetic report view for desktop screenshot evidence', async () => {
    const data = response(35);
    data.lines = data.lines.map((line, index) => ({ ...line, customerName: index % 2 ? 'Acme West' : 'Acme East', itemNumber: `P-${index + 1}`,
      sourceValues: { ...line.sourceValues, dueDate: `2027-01-${String(index % 28 + 1).padStart(2, '0')}` } }));
    get.mockResolvedValue(data);
    const { container } = render(<OpenOrdersPanel assignmentId="synthetic-review" snapshotId="synthetic-mps" />);
    await screen.findByText(/35 scoped lines/);
    applyTextFilter('customerName', 'Acme');
    fireEvent.click(screen.getByRole('button', { name: /Columns 9 of/ }));
    const theme = readFileSync(resolve(process.cwd(), 'src/index.css'), 'utf8');
    const css = readFileSync(resolve(process.cwd(), 'src/components/OpenOrdersPanel.css'), 'utf8');
    writeFileSync(process.env.KST_STAGE13_POLISH_FIXTURE_PATH!, `<!doctype html><html><head><meta charset="utf-8"><style>${theme}\n${css}\nbody { margin:0; background:var(--app); } .fixture { display:flex; height:850px; padding-top:16px; box-sizing:border-box; }</style></head><body><div class="fixture">${container.innerHTML}</div></body></html>`);
  });

  it.skipIf(!process.env.KST_STAGE13_PLAN_FIXTURE_PATH)('writes a synthetic planning screen for owner review', async () => {
    get.mockResolvedValue(response(3));
    const { container } = render(<OpenOrdersPanel assignmentId="synthetic-planning" snapshotId="synthetic-mps" />);
    await screen.findByText(/3 scoped lines/);
    togglePlan();
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

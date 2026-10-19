import { act, cleanup, fireEvent, render, renderHook, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { OpenOrdersPanel } from './OpenOrdersPanel';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';
import type { OpenOrdersResponseDto } from '../api/client';
import type { AnalyticalRequest, AnalyticalSaveResult } from '../api/analyticalExportApi';
import { dismissAnalyticalSave, recordAnalyticalSave, useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';
import { rememberAnalyticalOpenPreference } from '../shared/analyticalOpenPreference';

const host = vi.hoisted(() => ({ invoke: vi.fn(), get: vi.fn(), generateCsv: vi.fn() }));
vi.mock('../api/tauri-bridge', () => ({ resolveBackendBaseUrl: async () => 'http://localhost', isRunningInTauri: () => true }));
vi.mock('../api/client', async original => ({ ...await original<typeof import('../api/client')>(), ApiClient: class {
  getOpenOrders = host.get; getOpenOrdersDraftPresence = async () => ({ exists: false }); exportOpenOrdersQxtend = host.generateCsv;
} }));
const workbook = { preparationId: 'exact-open-orders', fileName: 'Synthetic-Open-Orders.xlsx', expiresAtUtc: '2099-01-01T00:00:00Z' };
function response(count = 205): OpenOrdersResponseDto {
  return { workspaceId: 'A', site: 'SW', mpsSnapshotId: 'snap', openOrdersSnapshotId: 'report', acquiredAtUtc: '2026-10-16T12:00:00Z', isStale: false, warning: null,
    lines: Array.from({ length: count }, (_, index) => ({ key: { domain: 'TEST', salesOrder: `SO-${String(index).padStart(3, '0')}`, line: 1 },
      itemNumber: 'ITEM', site: 'SW', purchaseOrder: null, stat: 'A', shippedQty: 1,
      sourceValues: { dueDate: '2026-10-19', performDate: null, requiredDate: null, dockDate: null, orderQty: 3, price: '1.25' },
      planningValues: { dueDate: '2026-10-19', performDate: null, requiredDate: null, dockDate: null, orderQty: '3', price: '1.25' }, shippedQtyText: '1',
      numericText: { open: '2', extPrice: '2.5', unitPrice: '1.25', allocated: null, picked: null, siteQoh: null },
      open: 2, extPrice: '2.5', unitPrice: 1.25, allocated: null, customer: 'C1', customerName: 'Synthetic', salesperson: 'Sam', customerPart: 'MATCH_%',
      ios: null, lineComments: '', lineHold: null, partials: null, picked: null, plnr: null, prodStat: null, productLine: 'B', qaHold: null,
      remarks: null, revision: null, shipAcct: null, shipTo: null, shipVia: null, siteQoh: null, soHoldStatus: null, soType: null, consignment: false })) };
}
function tree(id = 'A') { return <div className="shell" data-theme="light" data-density="comfortable"><AnalyticalSaveStatus /><main><OpenOrdersPanel assignmentId={id} snapshotId="snap" /></main></div>; }
const calls = (name: string) => host.invoke.mock.calls.filter(([command]) => command === name);
async function activate() { fireEvent.click(screen.getByRole('button', { name: 'Export' })); await act(async () => fireEvent.click(screen.getByRole('menuitem', { name: /Excel workbook/ }))); }
beforeEach(async () => {
  // Exercise the real core invoke adapter against a mocked native boundary. Windows path casing
  // can otherwise give lazy imports a distinct module identity from a bare-specifier vi.mock.
  Object.defineProperty(window, '__TAURI_INTERNALS__', { configurable: true, value: { invoke: host.invoke } });
  localStorage.clear(); host.invoke.mockReset(); host.get.mockReset(); host.generateCsv.mockReset(); host.get.mockResolvedValue(response());
  host.invoke.mockImplementation(async (command: string) => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? workbook
    : command === 'component_mrp_save' ? { outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true } : false);
  await act(async () => rememberAnalyticalOpenPreference(false));
});
afterEach(() => {
  cleanup(); const state = renderHook(useAnalyticalExportStatus);
  act(() => { for (const operation of state.result.current.operations) { recordAnalyticalSave(operation.id, { outcome: 'failed', message: 'Test cleanup', operationComplete: true }); dismissAnalyticalSave(operation.id); } });
  state.unmount(); vi.useRealTimers(); Reflect.deleteProperty(window, '__TAURI_INTERNALS__');
});
it('exports all 1205 matching rows and retains exact preparation through every page size and page change', async () => {
  const data = response(1205);
  data.lines = data.lines.map((line, i) => ({ ...line, key: { ...line.key, salesOrder: `SO-${String(i).padStart(4, '0')}` } }));
  host.get.mockResolvedValue(data); render(tree()); await screen.findByRole('table');
  const size = (value: string) => fireEvent.click(within(screen.getByRole('group', { name: 'Rows per page' })).getByRole('button', { name: value }));
  fireEvent.change(screen.getByRole('searchbox', { name: 'Search open orders' }), { target: { value: 'match_%' } });
  fireEvent.keyDown(screen.getByRole('columnheader', { name: 'PO' }), { key: 'ArrowLeft', altKey: true });
  size('500'); fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
  expect(screen.getByText(/Rows 501–1000 of 1205/)).toBeInTheDocument();
  await activate(); await screen.findByRole('button', { name: 'Save again' });
  const request = calls('component_mrp_generate')[0][1].analyticalRequest as AnalyticalRequest;
  expect(request.openOrders?.lineKeys.map(k => k.salesOrder)).toEqual(Array.from({ length: 1205 }, (_, i) => `SO-${String(i).padStart(4, '0')}`));
  expect(request.openOrders?.columns).toEqual(['dueDate', 'po', 'order', 'line', 'itemNumber', 'site', 'open', 'stat', 'extPrice']);
  size('1000');
  expect(screen.getByText(/Rows 1–1000 of 1205/)).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
  expect(screen.getByText(/Rows 1001–1205 of 1205/)).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Save again' }));
  await waitFor(() => expect(calls('component_mrp_save')).toHaveLength(2));
  size('100');
  expect(screen.getByText(/Rows 1001–1100 of 1205/)).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Previous page' }));
  fireEvent.click(screen.getByRole('button', { name: 'Save again' }));
  await waitFor(() => expect(calls('component_mrp_save')).toHaveLength(3));
  expect(calls('component_mrp_generate')).toHaveLength(1);
  expect(calls('component_mrp_save').every(([, args]) => args.preparationId === workbook.preparationId)).toBe(true);
  expect(host.get).toHaveBeenCalledTimes(1); expect(host.generateCsv).not.toHaveBeenCalled();
}, 30_000);
it('uses actual shared menu and exact-preparation retry for every matching page with displayed column order, never legacy save', async () => {
  render(tree()); await screen.findByRole('table');
  fireEvent.change(screen.getByRole('searchbox', { name: 'Search open orders' }), { target: { value: 'match_%' } });
  fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
  fireEvent.click(screen.getByRole('button', { name: 'Customer # (hidden / filter-only)' }));
  fireEvent.click(screen.getByRole('button', { name: 'Contains' }));
  fireEvent.change(screen.getByLabelText('Customer # contains'), { target: { value: 'c1' } });
  fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
  fireEvent.keyDown(screen.getByRole('columnheader', { name: 'PO' }), { key: 'ArrowLeft', altKey: true });
  fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
  expect(screen.getByText(/Rows 101–200 of 205/)).toBeInTheDocument(); expect(screen.getByText('Exports all 205 matching rows across every page')).toBeInTheDocument();
  await activate(); await screen.findByRole('button', { name: 'Save again' });
  const request = calls('component_mrp_generate')[0][1].analyticalRequest as AnalyticalRequest;
  expect(request.kind).toBe('OpenOrders'); expect(request.openOrders?.lineKeys).toHaveLength(205);
  expect(request.openOrders?.lineKeys[0].salesOrder).toBe('SO-000'); expect(request.openOrders?.lineKeys[204].salesOrder).toBe('SO-204');
  expect(request.openOrders?.columns).toEqual(['dueDate', 'po', 'order', 'line', 'itemNumber', 'site', 'open', 'stat', 'extPrice']);
  fireEvent.click(screen.getByRole('button', { name: 'Next page' })); // pagination does not invalidate prepared workbook
  fireEvent.click(screen.getByRole('button', { name: 'Save again' }));
  await waitFor(() => expect(calls('component_mrp_save')).toHaveLength(2));
  expect(calls('component_mrp_generate')).toHaveLength(1); expect(host.get).toHaveBeenCalledTimes(1);
  expect(calls('component_mrp_save').every(([, args]) => args.preparationId === workbook.preparationId)).toBe(true);
});
it.each(['search', 'columns', 'mode', 'workspace', 'filter'])('rejects late preparation continuation after meaningful %s departure', async departure => {
  let finish!: (value: typeof workbook) => void;
  host.invoke.mockImplementation(async (command: string) => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? new Promise(resolve => { finish = resolve; }) : false);
  const ui = render(tree()); await screen.findByRole('table'); await activate();
  if (departure === 'search') fireEvent.change(screen.getByRole('searchbox', { name: 'Search open orders' }), { target: { value: 'Sam' } });
  else if (departure === 'columns') fireEvent.keyDown(screen.getByRole('columnheader', { name: 'PO' }), { key: 'ArrowLeft', altKey: true });
  else if (departure === 'mode') fireEvent.click(screen.getByRole('button', { name: 'Plan' }));
  else if (departure === 'filter') {
    fireEvent.click(screen.getByRole('button', { name: 'Filter SO' })); fireEvent.click(screen.getByRole('button', { name: 'Contains' }));
    fireEvent.change(screen.getByLabelText('SO contains'), { target: { value: 'SO-200' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
  }
  else ui.rerender(tree('B'));
  await act(async () => finish(workbook)); expect(calls('component_mrp_save')).toHaveLength(0); expect(calls('sop_discard_generation').length).toBeGreaterThan(0);
});
it('retains exact prepared bytes through popup drafts, facet searches, focus and cancellation, but invalidates after Apply', async () => {
  render(tree()); await screen.findByRole('table'); await activate(); await screen.findByRole('button', { name: 'Save again' });
  const trigger = screen.getByRole('button', { name: 'Filter SO' }); fireEvent.click(trigger);
  await screen.findByLabelText('Value: SO-204');
  fireEvent.change(screen.getByLabelText('Search values'), { target: { value: 'SO-20' } });
  await screen.findByLabelText('Value: SO-204');
  fireEvent.click(screen.getByLabelText('Value: SO-204'));
  fireEvent.focus(screen.getByLabelText('Search values'));
  fireEvent.click(screen.getByRole('button', { name: 'Cancel' })); expect(trigger).toHaveFocus();
  fireEvent.click(screen.getByRole('button', { name: 'Save again' }));
  await waitFor(() => expect(calls('component_mrp_save')).toHaveLength(2));
  expect(calls('component_mrp_generate')).toHaveLength(1);
  expect(calls('component_mrp_save')[1][1].preparationId).toBe(workbook.preparationId);
  fireEvent.click(trigger); await screen.findByLabelText('Value: SO-204');
  fireEvent.click(screen.getByLabelText('Value: SO-204')); fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
  expect(screen.queryByRole('button', { name: 'Save again' })).not.toBeInTheDocument();
  expect(screen.getByText(/Rows 1–100 of 204/)).toBeInTheDocument();
});
it('exports independently expected matches beyond page one using committed exact source predicates and selected columns', async () => {
  const data = response(305);
  data.lines = data.lines.map((line, i) => ({ ...line, numericText: { ...line.numericText, open: i >= 100 && i % 2 === 0 ? '9007199254740993.0000000002' : '9007199254740993.0000000001' } }));
  host.get.mockResolvedValue(data); render(tree()); await screen.findByRole('table');
  fireEvent.click(screen.getByRole('button', { name: 'Filter Open' }));
  fireEvent.change(screen.getByLabelText('Condition'), { target: { value: 'gt' } });
  fireEvent.change(screen.getByLabelText('Value'), { target: { value: '9007199254740993.0000000001' } });
  fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
  expect(screen.getByText(/Rows 1–100 of 103/)).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Next page' }));
  expect(screen.getByText(/Rows 101–103 of 103/)).toBeInTheDocument();
  await activate(); await screen.findByRole('button', { name: 'Save again' });
  const request = calls('component_mrp_generate')[0][1].analyticalRequest as AnalyticalRequest;
  expect(request.openOrders?.lineKeys.map(k => k.salesOrder)).toEqual(Array.from({ length: 103 }, (_, i) => `SO-${100 + i * 2}`));
  expect(request.openOrders?.columns).toEqual(['dueDate', 'order', 'po', 'line', 'itemNumber', 'site', 'open', 'stat', 'extPrice']);
  expect(host.get).toHaveBeenCalledTimes(1); expect(host.generateCsv).not.toHaveBeenCalled();
});
it('deduplicates activation, retains definite failures, and changing search deliberately invalidates unsaved bytes', async () => {
  host.invoke.mockImplementation(async (command: string) => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? workbook
    : command === 'component_mrp_save' ? { outcome: 'failed', message: 'Destination unavailable.', operationComplete: true } : false);
  render(tree()); await screen.findByRole('table'); fireEvent.click(screen.getByRole('button', { name: 'Export' }));
  const format = screen.getByRole('menuitem', { name: /Excel workbook/ }); fireEvent.click(format); fireEvent.click(format);
  await screen.findByRole('button', { name: 'Save again' }); expect(calls('component_mrp_generate')).toHaveLength(1); expect(calls('component_mrp_save')).toHaveLength(1);
  fireEvent.change(screen.getByRole('searchbox', { name: 'Search open orders' }), { target: { value: 'Sam' } });
  expect(screen.queryByRole('button', { name: 'Save again' })).not.toBeInTheDocument();
});
it('retains uncertain outcome across Plan departure and only observes status; no write replay', async () => {
  host.invoke.mockImplementation(async (command: string) => {
    if (command === 'component_mrp_begin') return 1; if (command === 'component_mrp_generate') return workbook;
    if (command === 'component_mrp_save') throw new Error('Lost response');
    if (command === 'component_mrp_status') return { outcome: 'saved', message: 'Workbook saved, but could not be opened.', savedArtifactId: 'artifact', openFailed: true, operationComplete: true };
    return false;
  });
  render(tree()); await screen.findByRole('table'); await activate(); await screen.findByRole('button', { name: 'Check save status' });
  await screen.findByText('Save outcome uncertain. Check save status before preparing or saving again.');
  fireEvent.click(screen.getByRole('button', { name: 'Plan' }));
  const state = renderHook(useAnalyticalExportStatus);
  expect(state.result.current.operations[state.result.current.operations.length - 1]).toMatchObject({ pending: true, checking: false });
  expect(screen.getByRole('button', { name: 'Check save status' })).toBeEnabled();
  await act(async () => fireEvent.click(screen.getByRole('button', { name: 'Check save status' })));
  await waitFor(() => expect(calls('component_mrp_status')).toHaveLength(1));
  expect(state.result.current.operations[state.result.current.operations.length - 1]).toMatchObject({ pending: false, openFailed: true });
  await screen.findByRole('button', { name: 'Open again' });
  fireEvent.click(screen.getByRole('button', { name: 'Open again' })); await screen.findByText(/opening request accepted/);
  expect(calls('component_mrp_save')).toHaveLength(1); expect(calls('component_mrp_generate')).toHaveLength(1); expect(calls('component_mrp_status')).toHaveLength(1);
  expect(calls('component_mrp_open')[0][1]).toMatchObject({ artifactId: 'artifact', reportKind: 'Analytical' });
});
it('inherits theme/density, menu keyboard/Escape focus return and actual eight-second successful-save feedback', async () => {
  const user = userEvent.setup();
  host.get.mockResolvedValue(response(1));
  host.invoke.mockImplementation(async (command: string): Promise<unknown> => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? workbook
    : command === 'component_mrp_save' ? { outcome: 'saved', message: 'Workbook saved.', operationComplete: true } satisfies AnalyticalSaveResult : false);
  render(tree()); await screen.findByRole('table'); const button = screen.getByRole('button', { name: 'Export' }); button.focus();
  await user.keyboard('{ArrowDown}'); const menu = screen.getByRole('menu'); expect(menu.parentElement).toHaveClass('shell');
  expect(menu.closest('[data-density]')).toHaveAttribute('data-density', 'comfortable'); await user.keyboard('{Escape}'); expect(button).toHaveFocus();
  vi.useFakeTimers(); await activate(); expect(screen.getByText('Workbook saved.')).toBeInTheDocument();
  await act(async () => vi.advanceTimersByTimeAsync(7999)); expect(screen.getByText('Workbook saved.')).toBeInTheDocument();
  await act(async () => vi.advanceTimersByTimeAsync(1)); expect(screen.queryByText('Workbook saved.')).not.toBeInTheDocument();
  expect(calls('component_mrp_save')).toHaveLength(1);
});

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { readFileSync } from 'node:fs';
import { mkdtempSync, rmSync, writeFileSync, existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { act, cleanup, fireEvent, render, renderHook, screen, waitFor, within } from '@testing-library/react';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';
import { dismissAnalyticalSave, recordAnalyticalSave, useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';
import { rememberAnalyticalOpenPreference } from '../shared/analyticalOpenPreference';
import type { LongTermShortageRow, LongTermShortagesScreen as LongTermShortagesResponse } from '../api/longTermShortagesApi';
import { compactFixture, projectionFixture } from '../longTermShortages/compactScreenFixture.test-support';
import { exportLongTermShortages, fetchLongTermShortages } from '../api/longTermShortagesApi';
import { saveLongTermShortagesWorkbook } from '../longTermShortages/saveLongTermShortagesWorkbook';
import { LongTermShortagesPanel } from './LongTermShortagesPanel';
import { fetchComponentDetail } from '../api/componentDetailApi';
import { fetchApprovedVendors } from '../api/approvedVendorsApi';
import { fetchLongTermShortagePurchasing } from '../api/longTermShortagesApi';

const fetchMock = vi.fn();
const exportMock = vi.fn();
const saveWorkbookMock = vi.fn();
const purchasingMock = vi.fn();
const projectionMock = vi.fn();
const host = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => true, resolveBackendBaseUrl: async () => 'http://localhost' }));
const workbook = { preparationId: 'exact-shortages', fileName: 'Workspace-Shortages-2026-10-05.xlsx', expiresAtUtc: '2099-01-01T00:00:00Z' };
const calls = (name: string) => host.invoke.mock.calls.filter(([command]) => command === name);
async function activate() {
  fireEvent.click(screen.getByRole('button', { name: 'Export' }));
  await act(async () => fireEvent.click(screen.getByRole('menuitem', { name: /Excel workbook/ })));
}
function exportTree(assignmentId = 'ws-1') {
  return <div className="shell"><AnalyticalSaveStatus /><LongTermShortagesPanel assignmentId={assignmentId} snapshotId="snap-1" site="SW" /></div>;
}
const EDGE_PATH = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
vi.mock('../api/longTermShortagesApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/longTermShortagesApi')>()),
  fetchLongTermShortages: (...args: Parameters<typeof fetchLongTermShortages>) => fetchMock(...args),
  exportLongTermShortages: (...args: Parameters<typeof exportLongTermShortages>) => exportMock(...args),
  fetchLongTermShortagePurchasing: (...args: Parameters<typeof fetchLongTermShortagePurchasing>) => purchasingMock(...args),
  fetchLongTermShortageProjectionDetail: (...args: unknown[]) => projectionMock(...args),
}));
vi.mock('../longTermShortages/saveLongTermShortagesWorkbook', () => ({
  saveLongTermShortagesWorkbook: (...args: Parameters<typeof saveLongTermShortagesWorkbook>) => saveWorkbookMock(...args),
}));
vi.mock('../api/componentDetailApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/componentDetailApi')>()),
  fetchComponentDetail: vi.fn(),
}));
vi.mock('../api/approvedVendorsApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/approvedVendorsApi')>()),
  fetchApprovedVendors: vi.fn(),
}));

function bucket(weekNumber: number | null, projectedQoh: number | string, severity = 'Clear') {
  return { weekNumber, weekStart: weekNumber === null ? null : '2026-10-04', grossRequirements: '98.2456140337', scheduledReceipts: 0, plannedOrdersDue: 0, plannedOrdersRelease: weekNumber === 1 ? '49.125' : 0, projectedQoh, severity, unconfirmedReceipts: 0, confirmedEnding: projectedQoh, allReceiptsEnding: projectedQoh, planningEnding: projectedQoh, allReceiptsPlanningEnding: projectedQoh, lowestProjectedBalance: projectedQoh, lowestConfirmedBalance: projectedQoh, lowestAllReceiptsBalance: projectedQoh, overdueReceipts: 0, includesUnconfirmed: false };
}

function row(overrides: Partial<LongTermShortageRow> = {}): LongTermShortageRow {
  return {
    componentPart: 'CRITICAL', unitOfMeasure: 'EA', qadStatus: 'P', description: 'Bolt', planner: 'Ann', buyerPlannerCode: 'BUY', openingQoh: '12.125', safetyStockState: 'Resolved', safetyStock: 5,
    severity: 'CriticalShort', firstShortDate: '2026-10-05', demandParentParts: ['PARENT-1'], past: bucket(null, '12.125'),
    weeks: Array.from({ length: 26 }, (_, index) => bucket(index + 1, index === 0 ? '-2.4' : '12.125', index === 0 ? 'CriticalShort' : 'Clear')),
    evidence: [{ evidenceOrdinal: 1, type: 'SUPPLYP', dueDate: '2026-10-05', releaseDate: '2026-09-28', quantity: '49.125', category: 'PlannedOrdersRelease', sourceNumber: null, sourceLine: null, sourceLine2: null, sourceRowId: '0001', isPoReceipt: false, poConfirmed: null }], episodes: [], dataQualityWarning: null,
    presentation: { manufacturerItem: 'MFG-1', poNumber: 'PO-1', poLine: 2, poDueDate: '2026-10-06', poOpenQuantity: '49.125', poConfirmed: true, isKss: true }, firstAtRiskDate: null, effectivePmCode: 'P', partStatusDescription: 'PROTO', orderPeriodDays: 7, safetyTimeWorkingDays: 0, manufacturingLeadWorkingDays: null, purchasingLeadCalendarDays: 14, cumulativeLeadCalendarDays: 21, sitePlanningPresent: true, ...overrides,
  };
}

function response(rows: LongTermShortageRow[], isStale = false, allRows = rows): LongTermShortagesResponse {
  return { snapshotId: 'snap-1', refreshDate: '2026-10-05', isStale, warning: isStale ? 'QAD unavailable; retained result shown.' : null,
    components: rows.map((r, index) => ({ ...compactFixture(r), all: compactFixture(allRows[index]).all })),
    weeks: rows[0]?.weeks.map(w => ({ weekNumber: w.weekNumber!, weekStart: w.weekStart!, labelDate: '2026-10-05' })) ?? [],
    acquiredAtUtc: '2026-10-05T12:00:00Z', consistencyMode: 'PRO2_READ_UNCOMMITTED' };
}

function purchasing(lines: Array<{ poNumber: string; poLine: number; dueDate: string | null; openQuantity: number; confirmed: boolean | null; supplierDisplay: string | null }> = [], currentComment: string | null = null) {
  return { commentAvailable: true, currentComment, openPurchaseOrders: lines.map((line) => ({ componentPart: 'CRITICAL', description: null, leadTimeDays: null, buyerDisplay: null, manufacturerItem: null, isKss: false, trackingInfo: null, isCreditHold: null, isCia: null, currentComments: null, ...line })) };
}

beforeEach(async () => {
  localStorage.clear();
  host.invoke.mockReset().mockImplementation(async (command: string) => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? workbook
    : command === 'component_mrp_save' ? { outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true } : false);
  await act(async () => rememberAnalyticalOpenPreference(false));
  fetchMock.mockReset(); exportMock.mockReset(); saveWorkbookMock.mockReset(); purchasingMock.mockReset().mockResolvedValue(purchasing());
  projectionMock.mockReset().mockImplementation((_workspace, snapshotId, componentPart) => Promise.resolve({ ...projectionFixture(row()), snapshotId, componentPart }));
  vi.mocked(fetchComponentDetail).mockReset().mockResolvedValue({
    site: 'SW', componentPart: 'CRITICAL', description: 'Bolt', partStatusCode: 'P', partStatusDescription: 'PROTO',
    iosCode: null, netQuantityOnHand: 12, nonNetQuantityOnHand: 0, standardCost: null, qctc: null,
    timeFence: null, safetyTime: null, safetyStock: 5, buyerPlanner: 'BUY', purchaseLeadTimeDays: 14,
    inspectionLeadTimeDays: null, cumulativeLeadTimeDays: 21, minimumOrderQuantity: null, orderMultiple: null,
    loadedAtUtc: '2026-10-05T12:00:00Z', isStale: false, warning: null,
  });
  vi.mocked(fetchApprovedVendors).mockReset().mockResolvedValue([]);
});
afterEach(() => {
  cleanup(); const state = renderHook(useAnalyticalExportStatus);
  act(() => { for (const operation of state.result.current.operations) { recordAnalyticalSave(operation.id, { outcome: 'failed', message: 'Test cleanup', operationComplete: true }); dismissAnalyticalSave(operation.id); } });
  state.unmount(); vi.useRealTimers();
});

describe('LongTermShortagesPanel', () => {
  it('counts the loaded population and groups receipt switching with view controls with an accurate Week 1 legend', async () => {
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'HEALTHY', severity: 'Healthy' })]));
    render(exportTree()); await screen.findByRole('table');
    expect(screen.getByText('Showing 1 of 2 components')).not.toHaveAttribute('role');
    expect(screen.getByText('Negative weekly ending balance')).toBeInTheDocument();
    expect(screen.getByText('Part tint: Week 1 critical short')).toBeInTheDocument();
    expect(screen.getByText('Part tint: Week 1 below safety stock')).toBeInTheDocument();
    const view = screen.getByRole('group', { name: 'Loaded-data and view controls' });
    expect(within(view).getByLabelText('Include Unconfirmed Receipts')).toBeInTheDocument();
    expect(within(view).getByLabelText('Sort')).toBeInTheDocument();
    const scope = screen.getByRole('group', { name: 'Projection scope controls' });
    expect(within(scope).getByLabelText('Horizon')).toBeInTheDocument();
    expect(within(scope).getByLabelText('Include Manufactured Parts')).toBeInTheDocument();
    expect(scope).toHaveTextContent('cached when compatible');
    fireEvent.click(screen.getByLabelText('Show All'));
    expect(screen.getByText('Showing 2 of 2 components')).toBeInTheDocument();
  });

  it('one format activation saves the complete sorted selection; cancel retains exact bytes through scroll and component detail', async () => {
    fetchMock.mockResolvedValue(response([row({ componentPart: 'Z' }), row({ componentPart: 'A' })]));
    const ui = render(exportTree()); await screen.findByRole('table');
    fireEvent.change(screen.getByLabelText('Sort'), { target: { value: 'component' } });
    await activate(); await screen.findByRole('button', { name: 'Save again' });
    expect(calls('component_mrp_save')).toHaveLength(1);
    expect(calls('component_mrp_generate')[0][1].analyticalRequest.selection.componentParts).toEqual(['A', 'Z']);
    fireEvent.scroll(screen.getByTestId('workspace-shortages-grid'));
    fireEvent.click(screen.getByRole('button', { name: 'A' }));
    await screen.findByRole('dialog'); fireEvent.keyDown(document, { key: 'Escape' });
    ui.rerender(exportTree());
    fireEvent.click(screen.getByRole('button', { name: 'Save again' }));
    await waitFor(() => expect(calls('component_mrp_save')).toHaveLength(2));
    expect(calls('component_mrp_generate')).toHaveLength(1);
    expect(calls('component_mrp_save').every(([, args]) => args.preparationId === workbook.preparationId && args.reportKind === 'WorkspaceShortages')).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it.each(['filter', 'sort', 'receipts', 'horizon', 'population', 'workspace'])('invalidates retained preparation on meaningful %s change', async change => {
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'OTHER' })]));
    const ui = render(exportTree()); await screen.findByRole('table');
    await activate(); await screen.findByRole('button', { name: 'Save again' });
    if (change === 'filter') fireEvent.change(screen.getByLabelText('Comp'), { target: { value: 'OTHER' } });
    if (change === 'sort') fireEvent.change(screen.getByLabelText('Sort'), { target: { value: 'component' } });
    if (change === 'receipts') fireEvent.click(screen.getByLabelText('Include Unconfirmed Receipts'));
    if (change === 'horizon') fireEvent.change(screen.getByLabelText('Horizon'), { target: { value: '52' } });
    if (change === 'population') fireEvent.click(screen.getByLabelText('Include Manufactured Parts'));
    if (change === 'workspace') ui.rerender(exportTree('ws-2'));
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Save again' })).not.toBeInTheDocument());
    expect(calls('sop_discard_generation').length).toBeGreaterThan(0);
    expect(calls('component_mrp_save')).toHaveLength(1);
  });

  it('blocks retained rows during replacement and uses loaded population/horizon and current receipt mode after completion', async () => {
    let finish!: (value: LongTermShortagesResponse) => void;
    fetchMock.mockResolvedValueOnce(response([row()])).mockImplementationOnce(() => new Promise(resolve => { finish = resolve; }));
    render(exportTree()); await screen.findByRole('table');
    fireEvent.change(screen.getByLabelText('Horizon'), { target: { value: '52' } });
    expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    expect(screen.getByText(/A replacement projection is pending/)).toBeInTheDocument();
    fireEvent.click(screen.getByLabelText('Include Unconfirmed Receipts'));
    await act(async () => finish(response([row({ weeks: Array.from({ length: 52 }, (_, i) => bucket(i + 1, -1)) })])));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Export' })).toBeEnabled());
    await activate(); await screen.findByRole('button', { name: 'Save again' });
    expect(calls('component_mrp_generate')[0][1].analyticalRequest.selection).toMatchObject({ horizonWeeks: 52, includeUnconfirmed: true, includeManufacturedParts: false });
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('guards duplicate activation and late preparation after context change', async () => {
    let finish!: (value: typeof workbook) => void;
    host.invoke.mockImplementation(async command => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? new Promise(resolve => { finish = resolve; }) : false);
    fetchMock.mockResolvedValue(response([row()])); render(exportTree()); await screen.findByRole('table');
    await activate(); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Export' }));
    fireEvent.click(screen.getByLabelText('Include Unconfirmed Receipts'));
    await act(async () => finish(workbook));
    expect(calls('component_mrp_generate')).toHaveLength(1); expect(calls('component_mrp_save')).toHaveLength(0);
  });

  it('reconciles uncertain status observationally and recovers opening failure with the bound kind', async () => {
    host.invoke.mockImplementation(async command => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? workbook
      : command === 'component_mrp_save' ? Promise.reject(new Error('lost response'))
      : command === 'component_mrp_status' ? { outcome: 'saved', message: 'Workbook saved, but opening failed.', operationComplete: true, savedArtifactId: 'artifact', openFailed: true } : false);
    fetchMock.mockResolvedValue(response([row()])); render(exportTree()); await screen.findByRole('table');
    await activate(); fireEvent.click(await screen.findByRole('button', { name: 'Check save status' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Open again' }));
    await waitFor(() => expect(calls('component_mrp_open')).toHaveLength(1));
    expect(calls('component_mrp_status')[0][1]).toMatchObject({ reportKind: 'WorkspaceShortages' });
    expect(calls('component_mrp_open')[0][1]).toMatchObject({ reportKind: 'WorkspaceShortages', artifactId: 'artifact' });
    expect(calls('component_mrp_save')).toHaveLength(1); expect(calls('component_mrp_generate')).toHaveLength(1);
  });

  it('uses the shared open preference, then expires only ordinary success after eight seconds', async () => {
    host.invoke.mockImplementation(async command => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? workbook
      : command === 'component_mrp_save' ? { outcome: 'saved', message: 'Workbook saved.', operationComplete: true } : false);
    fetchMock.mockResolvedValue(response([row()])); render(exportTree()); await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'Export' }));
    fireEvent.click(screen.getByRole('menuitemcheckbox', { name: /Open Excel after saving/ }));
    expect(screen.getByRole('menuitemcheckbox')).toHaveAttribute('aria-checked', 'true');
    fireEvent.click(screen.getByRole('menuitem', { name: /Excel workbook/ }));
    await screen.findByText('Workbook saved.');
    expect(calls('component_mrp_save')[0][1].openAfterSave).toBe(true);
    vi.useFakeTimers(); // Restart the ordinary-success effect with a new known outcome below.
    const state = renderHook(useAnalyticalExportStatus);
    const operation = state.result.current.operations[state.result.current.operations.length - 1];
    // The existing real timer was scheduled before fake timers; instead verify the shared clock
    // in a fresh second activation, without simulating a synchronous desktop confirm.
    act(() => dismissAnalyticalSave(operation.id));
    await act(async () => { fireEvent.click(screen.getByRole('button', { name: 'Export' })); });
    await act(async () => { fireEvent.click(screen.getByRole('menuitem', { name: /Excel workbook/ })); });
    expect(screen.getByText('Workbook saved.')).toBeInTheDocument();
    await act(async () => vi.advanceTimersByTime(7999));
    expect(screen.getByText('Workbook saved.')).toBeInTheDocument();
    await act(async () => vi.advanceTimersByTime(1));
    expect(screen.queryByText('Workbook saved.')).not.toBeInTheDocument();
    state.unmount();
  });

  it('consolidates load failure and stale warning without losing either message', async () => {
    fetchMock.mockResolvedValueOnce(response([row()], true)).mockRejectedValueOnce(new Error('Replacement unavailable.'));
    render(exportTree()); await screen.findByRole('table');
    fireEvent.change(screen.getByLabelText('Horizon'), { target: { value: '52' } });
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Replacement unavailable.'));
    expect(screen.getAllByRole('alert')).toHaveLength(1);
    expect(screen.getByRole('alert')).toHaveTextContent('QAD unavailable; retained result shown.');
    expect(screen.getByRole('alert')).toHaveTextContent('Refresh date: 2026-10-05');
  });

  it('hydrates only the selected snapshot detail and keeps display actions local', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table');
    expect(projectionMock).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    expect(await screen.findByText('PARENT-1')).toBeInTheDocument();
    expect(projectionMock).toHaveBeenCalledWith('ws-1', 'snap-1', 'CRITICAL', expect.objectContaining({ horizonWeeks: 26 }));
    fireEvent.keyDown(document, { key: 'Escape' });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Unconfirmed Receipts' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(projectionMock).toHaveBeenCalledTimes(1);
  });

  it('rejects mismatched detail without showing newer data under the screen snapshot', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    projectionMock.mockResolvedValue({ ...projectionFixture(row()), snapshotId: 'snap-2', demandParentParts: ['NEWER-PARENT'] });
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    expect(await screen.findByText(/Snapshot projection detail unavailable/)).toBeInTheDocument();
    expect(screen.queryByText('NEWER-PARENT')).not.toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('does not fetch before MPS is loaded', () => {
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId={null} />);
    expect(screen.getByText(/Load the MPS dashboard/)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
    expect(purchasingMock).not.toHaveBeenCalled();
  });

  it('renders a 26 Sunday-week grid, defaults to shortages, filters locally, and exposes stale status', async () => {
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'CLEAR', qadStatus: null, planner: 'Bob', severity: 'Healthy', firstShortDate: null, weeks: [bucket(1, '12.125')] })], true));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const metadata = await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    expect(purchasingMock).not.toHaveBeenCalled();
    const weeks = metadata;
    expect(within(weeks).getByRole('columnheader', { name: /Week 26/i })).toBeInTheDocument();
    expect(screen.queryByText(/QADPro2 near-real-time Pro2 reporting replica/)).not.toBeInTheDocument();
    expect(within(metadata).queryByText('CLEAR')).not.toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent(/retained result/);
    const firstWeekCell = weeks.querySelector('tbody td:nth-child(8)');
    expect(firstWeekCell).toHaveTextContent('-2');
    expect(firstWeekCell?.className).toContain('long-term-shortages__balance--negative');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    await screen.findByText('CLEAR');
    fireEvent.change(screen.getByLabelText('Buyer / Planner'), { target: { value: 'Bob' } });
    await waitFor(() => expect(screen.getByRole('table', { name: 'Workspace Shortages weekly balances' })).toHaveTextContent('CLEAR'));
    fireEvent.click(screen.getByRole('checkbox', { name: 'KSS' }));
    expect(screen.getByRole('table', { name: 'Workspace Shortages weekly balances' })).toHaveTextContent('CLEAR');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Status' }));
    fireEvent.change(screen.getByLabelText('Sort'), { target: { value: 'component' } });
    fireEvent.change(screen.getByLabelText('Comp'), { target: { value: 'clear' } });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('shows the specific source failure without repeating unavailable', async () => {
    fetchMock.mockRejectedValue(new Error('QADPro2 returned incomplete component results. Retry the report.'));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    expect(await screen.findByRole('alert')).toHaveTextContent('QADPro2 returned incomplete component results. Retry the report.');
    expect(screen.queryByText(/Workspace Shortages is unavailable: Workspace Shortages Unavailable/i)).not.toBeInTheDocument();
  });

  it('switches receipt mode locally and refreshes manufactured population while retaining the grid', async () => {
    let resolveManufactured!: (result: LongTermShortagesResponse) => void;
    fetchMock.mockResolvedValueOnce(response([row()], false, [row({ componentPart: 'CRITICAL', severity: 'Healthy' })]))
      .mockImplementationOnce(() => new Promise<LongTermShortagesResponse>((resolve) => { resolveManufactured = resolve; }));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Unconfirmed Receipts' }));
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('table', { name: 'Workspace Shortages weekly balances' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    expect(screen.getByRole('table', { name: 'Workspace Shortages weekly balances' })).toHaveTextContent('Healthy');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Manufactured Parts' }));
    await waitFor(() => expect(fetchMock).toHaveBeenLastCalledWith('ws-1', 'snap-1', { includeManufacturedParts: true, includePhantoms: false, includeUnconfirmed: false, horizonWeeks: 26, showAll: true }));
    expect(screen.getByRole('status')).toHaveTextContent('Refreshing…');
    expect(screen.getByRole('table', { name: 'Workspace Shortages weekly balances' })).toBeInTheDocument();
    resolveManufactured(response([row()]));
    await waitFor(() => expect(screen.queryByText('Refreshing…')).not.toBeInTheDocument());
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Manufactured Parts' }));
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('reuses a loaded horizon and avoids duplicate in-flight refreshes', async () => {
    let resolveLonger!: (result: LongTermShortagesResponse) => void;
    fetchMock.mockResolvedValueOnce(response([row()]))
      .mockImplementationOnce(() => new Promise<LongTermShortagesResponse>((resolve) => { resolveLonger = resolve; }));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.change(screen.getByLabelText('Horizon'), { target: { value: '52' } });
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
    fireEvent.change(screen.getByLabelText('Horizon'), { target: { value: '26' } });
    fireEvent.change(screen.getByLabelText('Horizon'), { target: { value: '52' } });
    expect(fetchMock).toHaveBeenCalledTimes(2);
    resolveLonger(response([row({ weeks: Array.from({ length: 52 }, (_, index) => bucket(index + 1, '12.125')) })]));
    await waitFor(() => expect(screen.queryByText('Refreshing…')).not.toBeInTheDocument());
  });

  it('contains the grid in a keyboard-scrollable panel with one aligned matrix and a shared modal', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const grid = await screen.findByTestId('workspace-shortages-grid');
    expect(grid).toHaveAttribute('tabindex', '0');
    expect(within(grid).getByRole('table', { name: 'Workspace Shortages weekly balances' })).toBeInTheDocument();
    expect(within(grid).getAllByRole('table')).toHaveLength(1);
    expect(grid.closest('.long-term-shortages')).toHaveAttribute('aria-labelledby', 'long-term-shortages-heading');
    expect(within(grid).getByRole('columnheader', { name: 'Buyer / Planner' })).toBeInTheDocument();
  });

  it('keeps severity labels compact while exposing full warnings on hover, focus, and in component information', async () => {
    const warning = 'Selected-site planning data missing; safety stock and lead times unknown.';
    fetchMock.mockResolvedValue(response([
      row({ componentPart: 'UNKNOWN', severity: 'SafetyStockUnavailable', safetyStock: null, safetyStockState: 'SelectedSiteValueMissing', dataQualityWarning: warning, weeks: [bucket(1, 12)] }),
      row({ componentPart: 'FUTURE', severity: 'FutureShort', dataQualityWarning: null }),
      row({ componentPart: 'SAFETY', severity: 'SafetyStockShort', dataQualityWarning: null, weeks: [bucket(1, 2, 'SafetyStockShort')] }),
      row(),
    ]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const metadata = await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    const unknown = within(metadata).getByRole('row', { name: 'UNKNOWN: SafetyStockUnavailable' });
    const severity = within(unknown).getByLabelText(`SS Unknown: ${warning}`);
    expect(severity).toHaveTextContent('SS Unknown');
    expect(severity).toHaveAttribute('tabindex', '0');
    expect(unknown.querySelector('td:nth-child(7)')).toHaveTextContent('SS Unknown');
    expect(within(metadata).queryByText(warning)).not.toBeInTheDocument();
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    fireEvent.mouseOver(severity);
    const tooltip = screen.getByRole('tooltip');
    expect(tooltip).toHaveTextContent(warning);
    expect(severity).toHaveAttribute('aria-describedby', tooltip.id);
    expect(tooltip).toHaveClass('long-term-shortages__severity-tooltip');
    expect(metadata.contains(tooltip)).toBe(false);
    fireEvent.mouseOut(severity);
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    severity.blur();
    severity.focus();
    expect(severity).toHaveFocus();
    expect(await screen.findByRole('tooltip')).toHaveTextContent(warning);
    expect(within(metadata).getByRole('row', { name: 'FUTURE: FutureShort' }).querySelector('[data-severity="FutureShort"]')).toHaveTextContent('Future Short');
    expect(within(metadata).getByRole('row', { name: 'SAFETY: SafetyStockShort' }).querySelector('[data-severity="SafetyStockShort"]')).toHaveTextContent('Safety Short');
    expect(within(metadata).getByRole('row', { name: 'CRITICAL: CriticalShort' }).querySelector('[data-severity="CriticalShort"]')).toHaveTextContent('Critical Short');
    expect(within(metadata).getByRole('row', { name: 'SAFETY: SafetyStockShort' }).querySelector('.long-term-shortages__comp')).toHaveClass('long-term-shortages__comp--safety');
    expect(within(metadata).getByRole('row', { name: 'CRITICAL: CriticalShort' }).querySelector('.long-term-shortages__comp')).toHaveClass('long-term-shortages__comp--critical');
    fireEvent.click(within(unknown).getByRole('button', { name: 'UNKNOWN' }));
    expect(within(screen.getByRole('dialog', { name: 'Component Information' })).getByText(warning)).toBeInTheDocument();
  });

  it('uses compact aligned On Hand and wrapped QAD Status headers without changing values', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const metadata = await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    const onHand = within(metadata).getByRole('columnheader', { name: 'On Hand' });
    const qad = within(metadata).getByRole('columnheader', { name: 'QAD Status' });
    expect(onHand).toHaveClass('long-term-shortages__num');
    expect(qad.querySelector('br')).toBeInTheDocument();
    expect(qad.querySelector('[aria-hidden="true"]')).toHaveTextContent('QADStatus');
    const cells = within(metadata).getByRole('row', { name: 'CRITICAL: CriticalShort' }).querySelectorAll('td');
    expect(cells[1]).toHaveTextContent('P');
    expect(cells[5]).toHaveClass('long-term-shortages__num');
    expect(cells[5]).toHaveTextContent('12');
    expect(screen.getByTestId('workspace-shortages-grid')).toHaveAttribute('tabindex', '0');
  });

  it('constrains informational widths while retaining the bounded horizontal projection grid', () => {
    // jsdom does not lay out table columns; verify the CSS rules used by the rendered classes.
    const css = readFileSync(resolve(process.cwd(), 'src/components/LongTermShortagesPanel.css'), 'utf8');
    expect(css).toMatch(/\.long-term-shortages__grid\s*{[^}]*overflow:\s*auto/);
    expect(css).toMatch(/\.long-term-shortages\s*{[^}]*overflow:\s*hidden/);
    expect(css).toMatch(/--comp-width:\s*104px/);
    expect(css).toMatch(/--description-width:\s*180px/);
    expect(css).toMatch(/--qad-status-width:\s*44px/);
    expect(css).toMatch(/--on-hand-width:\s*88px/);
    expect(css).toMatch(/--severity-width:\s*130px/);
    expect(css).toMatch(/--kss-width:\s*58px/);
    expect(css).toMatch(/--planner-width:\s*118px/);
    expect(css).toContain('background-color: var(--row-bg)');
    expect(css).toContain('var(--density-row-height)');
    expect(css).toContain('var(--control-on-accent)');
    expect(css).toMatch(/\.long-term-shortages__severity-tooltip\s*{[^}]*position:\s*fixed/);
  });

  it.skipIf(!existsSync(EDGE_PATH))('keeps the seven corner headers aligned and above weekly cells through scroll, resize, and scaling in Edge', async () => {
    fetchMock.mockResolvedValue(response(Array.from({ length: 35 }, (_, index) => row({ componentPart: `PART-${index}`, description: 'Bolt', planner: 'Ann' }))));
    const { container } = render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    const matrix = container.querySelector('.long-term-shortages__matrix')!;
    const css = readFileSync(resolve(process.cwd(), 'src/components/LongTermShortagesPanel.css'), 'utf8');
    const theme = readFileSync(resolve(process.cwd(), 'src/index.css'), 'utf8');
    const temp = mkdtempSync(resolve(tmpdir(), 'kst-frozen-grid-'));
    try {
      const fixture = `<!doctype html><meta charset="utf-8"><style>${theme}\n${css}\nbody { margin: 0; } .long-term-shortages { width: 1050px; height: 430px; box-sizing: border-box; }</style><section class="long-term-shortages"><div class="long-term-shortages__grid" style="height: 100%; flex: none">${matrix.outerHTML}</div></section><output id="layout-result"></output><script>
        const grid = document.querySelector('.long-term-shortages__grid');
        const table = grid.querySelector('table');
        const headers = [...table.tHead.rows[0].cells];
        const cells = [...table.tBodies[0].rows[0].cells];
        const failures = [];
        const close = (a, b) => Math.abs(a - b) < 1;
        function verify(width, scale) {
          document.querySelector('.long-term-shortages').style.width = width + 'px';
          document.body.style.zoom = String(scale);
          grid.scrollLeft = 380;
          grid.scrollTop = 170;
          const edge = grid.getBoundingClientRect().left + grid.clientLeft;
          const top = grid.getBoundingClientRect().top + grid.clientTop;
          for (let i = 0; i < 7; i++) {
            const h = headers[i].getBoundingClientRect();
            const c = cells[i].getBoundingClientRect();
            if (!close(h.left, c.left) || !close(h.width, c.width) || !close(h.top, top)) failures.push('frozen header ' + i + ' position');
            if (getComputedStyle(headers[i]).backgroundColor === 'rgba(0, 0, 0, 0)' || getComputedStyle(cells[i]).backgroundColor === 'rgba(0, 0, 0, 0)') failures.push('transparent frozen cell ' + i);
          }
          const boundary = headers[6].getBoundingClientRect().right;
          const week = headers[7].getBoundingClientRect();
          const weeklyValue = cells[7].getBoundingClientRect();
          const probe = Math.max(edge + 2, Math.min(boundary - 2, week.right - 2));
          if (week.left >= boundary || weeklyValue.left >= boundary) failures.push('weekly cells did not scroll beneath frozen region');
          if (document.elementFromPoint(probe, headers[0].getBoundingClientRect().top + 10) !== headers.find(h => { const r = h.getBoundingClientRect(); return r.left <= probe && r.right > probe; })) failures.push('weekly header overlaps frozen header');
          const visibleCells = [...table.tBodies[0].rows[8].cells];
          if (document.elementFromPoint(probe, visibleCells[0].getBoundingClientRect().top + 10) !== visibleCells.find(c => { const r = c.getBoundingClientRect(); return r.left <= probe && r.right > probe; })) failures.push('weekly value overlaps frozen cell');
          if (!close(headers[20].getBoundingClientRect().top, top)) failures.push('weekly header lost vertical sticky position');
          if (grid.scrollLeft < 100 || grid.scrollTop < 100) failures.push('bounded two-axis scrolling unavailable');
          if (getComputedStyle(grid).overflowX !== 'auto' || getComputedStyle(grid).overflowY !== 'auto') failures.push('scrollbar missing');
        }
        for (const theme of ['light', 'dark']) for (const density of ['compact', 'comfortable']) {
          document.documentElement.dataset.theme = theme;
          document.documentElement.dataset.density = density;
          for (const [width, scale] of [[1050, 1], [900, 1], [1050, 1.25], [900, 1.5], [740, 1]]) verify(width, scale);
          for (const row of [...table.tBodies[0].rows].slice(0, 2)) for (const cell of [...row.cells].slice(0, 7)) {
            if (!getComputedStyle(cell).backgroundColor.startsWith('rgb(')) failures.push('nonopaque striped/tinted background');
          }
        }
        document.getElementById('layout-result').textContent = JSON.stringify(failures);
      </script>`;
      const file = resolve(temp, 'grid.html');
      writeFileSync(file, fixture);
      const result = spawnSync(EDGE_PATH, ['--headless=new', '--disable-gpu', '--no-first-run', `--user-data-dir=${resolve(temp, 'profile')}`, '--dump-dom', `file:///${file.replace(/\\/g, '/')}`], { encoding: 'utf8', timeout: 30000, windowsHide: true });
      expect(result.error).toBeUndefined();
      expect(result.status).toBe(0);
      const match = result.stdout.match(/<output id="layout-result">([^<]*)<\/output>/);
      expect(match, result.stderr).not.toBeNull();
      expect(JSON.parse(match![1])).toEqual([]);
    } finally {
      rmSync(temp, { recursive: true, force: true });
    }
  }, 45000);

  it('opens selected-component purchasing detail without reloading the shortage matrix, then exports only filtered rows', async () => {
    exportMock.mockResolvedValue({ blob: new Blob(['workbook']), fileName: 'Workspace-Shortages-2026-10-05.xlsx' });
    saveWorkbookMock.mockResolvedValue({ kind: 'saved', fileName: 'Workspace-Shortages-2026-10-05.xlsx' });
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'OTHER', planner: 'Bob' })]));
    render(exportTree());
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.change(screen.getByLabelText('Comp'), { target: { value: 'critical' } });
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    const dialog = screen.getByRole('dialog', { name: 'Component Information' });
    expect(within(dialog).getByRole('heading', { name: 'Past' })).toBeInTheDocument();
    expect(within(dialog).getByRole('heading', { name: 'Shortage Snapshot' })).toBeInTheDocument();
    expect(within(dialog).getByRole('heading', { name: 'Open Purchase Orders' })).toBeInTheDocument();
    expect(within(dialog).queryByText('PlannedOrdersRelease')).not.toBeInTheDocument();
    expect(within(dialog).queryByRole('heading', { name: 'Raw MRP Evidence' })).not.toBeInTheDocument();
    expect(within(dialog).getByRole('complementary', { name: 'Workspace Shortages details' })).toBeInTheDocument();
    await waitFor(() => expect(vi.mocked(fetchComponentDetail)).toHaveBeenCalledWith('ws-1', 'CRITICAL'));
    await waitFor(() => expect(purchasingMock).toHaveBeenCalledWith('ws-1', 'snap-1', 'CRITICAL', expect.objectContaining({ horizonWeeks: 26 })));
    expect(purchasingMock).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(await within(dialog).findByText('Net QOH')).toBeInTheDocument();
    expect(within(dialog).getByText('Inventory / Lot Locations')).toBeInTheDocument();
    expect(within(dialog).queryByText(/KSS is visibility only/)).not.toBeInTheDocument();
    expect(within(dialog).queryByText(/Lead times annotate urgency/)).not.toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'CRITICAL' })).toHaveFocus();
    await activate();
    await screen.findByRole('button', { name: 'Save again' });
    expect(calls('component_mrp_generate')[0][1]).toMatchObject({ reportKind: 'WorkspaceShortages', analyticalRequest: {
      workspaceId: 'ws-1', mpsSnapshotId: 'snap-1', site: 'SW', acquiredAtUtc: '2026-10-05T12:00:00Z',
      selection: { snapshotId: 'snap-1', componentParts: ['CRITICAL'], includeManufacturedParts: false, includePhantoms: false, includeUnconfirmed: false, horizonWeeks: 26, showAll: false } } });
    expect(exportMock).not.toHaveBeenCalled(); expect(saveWorkbookMock).not.toHaveBeenCalled();
  });

  it('keeps the selected shortage snapshot visible when the PO detail request fails', async () => {
    purchasingMock.mockRejectedValue(new Error('PO source unavailable'));
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    const dialog = screen.getByRole('dialog', { name: 'Component Information' });
    expect(within(dialog).getByRole('heading', { name: 'Shortage Snapshot' })).toBeInTheDocument();
    expect(await within(dialog).findByText(/Open purchase orders unavailable/)).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});

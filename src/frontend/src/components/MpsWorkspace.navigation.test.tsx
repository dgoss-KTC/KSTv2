import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { MpsDashboardResponseDto, WorkOrderImmediateMaterialAnalysisResponseDto, WorkOrderImmediateMaterialComponentDto, WorkOrderSummaryDto } from '../api/client';
import { ApiError } from '../api/client';
import { MpsWorkspace } from './MpsWorkspace';
import { fetchPlanningWindow, fetchWorkOrderCandidates, fetchWorkOrderImmediateMaterial, fetchWorkOrderImmediateMaterialSummary } from '../api/workOrdersApi';

vi.mock('../api/workOrdersApi', async (original) => ({ ...await original<typeof import('../api/workOrdersApi')>(), fetchPlanningWindow: vi.fn(), fetchWorkOrderCandidates: vi.fn(), fetchWorkOrderImmediateMaterial: vi.fn(), fetchWorkOrderImmediateMaterialSummary: vi.fn() }));
vi.mock('../hooks/usePartDetail', () => ({ usePartDetail: () => ({ detail: null, isLoading: false, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/useBom', () => ({ useBom: () => ({ bom: null, isLoading: false, error: null, activate: vi.fn(), retry: vi.fn() }) }));
vi.mock('../hooks/useComponentDetail', () => ({ useComponentDetail: () => ({ detail: null, isLoading: false, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/useApprovedVendors', () => ({ useApprovedVendors: () => ({ rows: null, isLoading: false, error: null, activate: vi.fn(), retry: vi.fn() }) }));
vi.mock('./AnalyticalExcelExport', () => ({ AnalyticalExcelExport: () => null }));

const order = (woid = 'ROOT-WO', partNumber = 'PARENT'): WorkOrderSummaryDto => ({ woid, partNumber, status: 'released', orderedQuantity: 12, completedQuantity: 2, openQuantity: 10, releaseDate: '2026-10-05', dueDate: '2026-10-12', salesOrder: 'SO-1', kitting: { applicableLineCount: 2, fullyIssuedLineCount: 1, kittingPercent: 50 } });
const material = (componentPart: string, materialStatus: WorkOrderImmediateMaterialComponentDto['materialStatus']): WorkOrderImmediateMaterialComponentDto => ({ componentPart, materialStatus, description: `Description ${componentPart}`, isManufactured: materialStatus === 'notApplicable', unitOfMeasure: 'EA', requirementSource: 'actualWo', allocationMode: 'committedSequential', requiredQuantity: 12, issuedQuantity: 2, varianceQuantity: -10, issuedPercent: 16.67, remainingRequirement: 10, usableHardAllocationToThisWoComponent: 0, ownHardCoverage: 0, uncoveredRequirement: 10, availableQuantityAtEvaluation: 0, allocatedQuantity: 0, usableOnHand: 0, shortQuantity: materialStatus === 'short' ? 10 : null, isFloorStockOrNonIssued: false, isOverIssued: false, inventoryActivity: { transit: 0, inspection: 0, nonNet: 0, mrb: 0, ncmInspection: 0, expiredExpiring: 0 }, incoming: null, diagnostic: null });
const analysis = (woid = 'ROOT-WO', components = [material('SUB', 'notApplicable'), material('SHORT', 'short'), material('UNKNOWN', 'unknown'), material('AVAILABLE', 'onHand')]): WorkOrderImmediateMaterialAnalysisResponseDto => ({ snapshotId: 'snapshot-A', workOrder: { woid, buildPart: woid === 'ROOT-WO' ? 'PARENT' : 'SUB', status: 'released', workOrderType: null, materialBuildQuantity: 10, releaseDate: '2026-10-05', dueDate: '2026-10-12', planningBucketContext: 'forwardDue' }, components, diagnostic: null });
const dashboard = (): MpsDashboardResponseDto => ({ snapshot: { snapshotId: 'snapshot-A', workspaceId: 'workspace-A', site: 'SW', createdAtUtc: '2026-10-15T12:00:00Z', lastSuccessfulRefreshAtUtc: '2026-10-15T12:00:00Z', status: 'current', resolvedParentPartCount: 1, sourceRowCount: 1, isRefreshInProgress: false, lastRefreshError: null }, dateBasis: 'dueDate', horizonWeeks: 6, parts: [{ parentPart: 'PARENT', description: 'Parent description', buckets: [ { kind: 'falldown', weekLabel: null, quantity: 0, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false }, ...['2026-10-12', '2026-10-19', '2026-10-26', '2026-11-02', '2026-11-09', '2026-11-16'].map((weekLabel, index) => ({ kind: 'weekly' as const, weekLabel, quantity: index + 1, executionStatus: 'none' as const, containsPlannedWork: false, containsExplicitlyScheduledWork: false })) ] }] });
function props() { return { assignmentId: 'workspace-A', dashboard: dashboard(), dateBasis: 'dueDate' as const, horizonWeeks: 6, isLoading: false, isRefreshing: false, error: null, setDateBasis: vi.fn(), setHorizonWeeks: vi.fn(), reload: vi.fn(), refresh: vi.fn() }; }
function deferred<T>() { let resolve!: (value: T) => void; let reject!: (reason: unknown) => void; const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no; }); return { promise, resolve, reject }; }
const trail = () => screen.queryByRole('navigation', { name: 'MPS drill-down trail' });
const crumbs = () => trail() ? Array.from(trail()!.querySelectorAll('li')).map((li) => li.textContent?.replace(/^›/, '')) : [];
const bucket = (index = 1) => document.querySelectorAll<HTMLTableCellElement>('.mps-grid tbody td.mps-cell')[index];
const card = (woid = 'ROOT-WO') => screen.getByRole('button', { name: new RegExp(`${woid} .*Show material`) });
async function openMaterial(user: ReturnType<typeof userEvent.setup>) { await user.click(bucket()); await user.click(await screen.findByRole('button', { name: /ROOT-WO .*Show material/ })); await screen.findByRole('button', { name: 'SUB' }); }

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(fetchPlanningWindow).mockResolvedValue({ snapshotId: 'snapshot-A', workOrders: [order()] });
  vi.mocked(fetchWorkOrderCandidates).mockImplementation(async (_ws, _snapshot, _origin, part) => ({ snapshotId: 'snapshot-A', candidates: [order(part === 'SUB' ? 'SUB-WO' : 'DEEP-WO', part)] }));
  vi.mocked(fetchWorkOrderImmediateMaterial).mockImplementation(async (_ws, _snapshot, woid) => analysis(woid, woid === 'SUB-WO' ? [material('DEEP', 'notApplicable'), material('SUB-SHORT', 'short')] : woid === 'DEEP-WO' ? [material('AT-LIMIT', 'notApplicable')] : undefined));
  vi.mocked(fetchWorkOrderImmediateMaterialSummary).mockResolvedValue({ snapshotId: 'snapshot-A', workOrders: [] });
});
afterEach(() => vi.useRealTimers());

describe('Stage 22 MPS navigation (offline API fixtures)', () => {
  it('uses the same hierarchy for Back, Escape, ancestor jumps and grid focus', async () => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />);
    const row = bucket().parentElement;
    await openMaterial(user);
    expect(crumbs()).toEqual(['MPS grid', 'PARENT', 'Week of Oct 12', 'WO ROOT-WO']);
    const toggle = screen.getByRole('button', { name: /ROOT-WO .*Material shown below/ });
    await user.click(within(trail()!).getByRole('button', { name: '← Back' }));
    await waitFor(() => expect(toggle).toHaveFocus());
    expect(crumbs()).toEqual(['MPS grid', 'PARENT', 'Week of Oct 12']);
    await user.keyboard('{Escape}');
    expect(crumbs()).toEqual(['MPS grid', 'PARENT']);
    expect(screen.getByRole('tab', { name: 'Part Info' })).toHaveAttribute('aria-selected', 'true');
    await waitFor(() => expect(bucket()).toHaveFocus());
    await user.click(bucket());
    await screen.findByRole('button', { name: /ROOT-WO .*Show material/ });
    await user.click(within(trail()!).getByRole('button', { name: 'PARENT' }));
    expect(crumbs()).toEqual(['MPS grid', 'PARENT']);
    await user.click(within(trail()!).getByRole('button', { name: 'MPS grid' }));
    expect(trail()).not.toBeInTheDocument();
    await waitFor(() => expect(row).toHaveFocus());
  });

  it.each([0, 1])('opens eligible bucket %i from its API result even when grid quantity is zero', async (index) => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />);
    bucket(index).focus(); await user.keyboard('{Enter}');
    await screen.findByRole('button', { name: /ROOT-WO .*Show material/ });
    expect(crumbs()).toEqual(['MPS grid', 'PARENT', index === 0 ? 'Falldown' : 'Week of Oct 12']);
    expect(fetchPlanningWindow).toHaveBeenCalledWith('workspace-A', expect.objectContaining({ bucketKind: index === 0 ? 'falldown' : 'weekly' }));
  });

  it('does not activate Week 4 or later as a bucket', async () => {
    render(<MpsWorkspace {...props()} />);
    expect(bucket(5)).not.toHaveAttribute('tabindex');
    fireEvent.keyDown(bucket(5), { key: 'Enter' });
    expect(crumbs()).not.toContain('Week of Nov 9');
    expect(vi.mocked(fetchPlanningWindow).mock.calls.every(([, params]) => !params.bucketKind)).toBe(true);
  });

  it('keeps the previous view and trail for pending and successful empty results', async () => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />);
    await user.click(screen.getByText('PARENT'));
    const pending = deferred<Awaited<ReturnType<typeof fetchPlanningWindow>>>();
    vi.mocked(fetchPlanningWindow).mockImplementation((_id, params) => params.bucketKind ? pending.promise : Promise.resolve({ snapshotId: 'snapshot-A', workOrders: [order()] }));
    await user.click(bucket());
    expect(crumbs()).toEqual(['MPS grid', 'PARENT']);
    expect(screen.getByRole('status')).toHaveTextContent('Loading work orders');
    expect(screen.getByRole('tab', { name: 'Part Info' })).toHaveAttribute('aria-selected', 'true');
    await act(async () => pending.resolve({ snapshotId: 'snapshot-A', workOrders: [] }));
    expect(screen.getByRole('status')).toHaveTextContent('No work orders for this bucket');
    expect(crumbs()).toEqual(['MPS grid', 'PARENT']);
  });

  it.each(['failure', 'stale', 'wrong-snapshot'] as const)('keeps %s distinct from empty without navigating', async (outcome) => {
    const user = userEvent.setup();
    if (outcome === 'wrong-snapshot') vi.mocked(fetchPlanningWindow).mockResolvedValue({ snapshotId: 'obsolete', workOrders: [] });
    else vi.mocked(fetchPlanningWindow).mockRejectedValue(outcome === 'stale' ? new ApiError(409, 'Conflict', 'stale') : new Error('Source unavailable'));
    render(<MpsWorkspace {...props()} />); await user.click(bucket());
    expect(await screen.findByRole('alert')).not.toHaveTextContent('No work orders');
    expect(trail()).not.toBeInTheDocument();
  });

  it('ignores a delayed superseded bucket result, including after another bucket was empty', async () => {
    const pending = deferred<Awaited<ReturnType<typeof fetchPlanningWindow>>>();
    vi.mocked(fetchPlanningWindow).mockImplementation((_id, params) => params.weekLabel === '2026-10-12' ? pending.promise : Promise.resolve({ snapshotId: 'snapshot-A', workOrders: [] }));
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />);
    await user.click(bucket()); await user.click(bucket(2));
    await screen.findByText('No work orders for this bucket');
    await act(async () => pending.resolve({ snapshotId: 'snapshot-A', workOrders: [order('OBSOLETE')] }));
    expect(trail()).not.toBeInTheDocument(); expect(screen.queryByText('OBSOLETE')).not.toBeInTheDocument();
  });

  it('bounds pending feedback and never navigates on a success arriving after the timeout', async () => {
    vi.useFakeTimers();
    const pending = deferred<Awaited<ReturnType<typeof fetchPlanningWindow>>>();
    vi.mocked(fetchPlanningWindow).mockReturnValue(pending.promise);
    render(<MpsWorkspace {...props()} />); fireEvent.click(bucket());
    expect(screen.getByRole('status')).toHaveTextContent('Loading work orders');
    await act(async () => vi.advanceTimersByTimeAsync(15000));
    expect(screen.getByRole('alert')).toHaveTextContent('timed out');
    await act(async () => pending.resolve({ snapshotId: 'snapshot-A', workOrders: [order()] }));
    expect(trail()).not.toBeInTheDocument();
  });

  it('an empty bucket leaves an existing material selection, filter and trail unchanged', async () => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />); await openMaterial(user);
    await user.click(screen.getByRole('button', { name: 'Needs attention 2' }));
    vi.mocked(fetchPlanningWindow).mockResolvedValue({ snapshotId: 'snapshot-A', workOrders: [] });
    await user.click(bucket(2)); await screen.findByText('No work orders for this bucket');
    expect(crumbs()).toEqual(['MPS grid', 'PARENT', 'Week of Oct 12', 'WO ROOT-WO']);
    expect(screen.getByRole('button', { name: 'Needs attention 2' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: /ROOT-WO .*Material shown below/ })).toHaveAttribute('aria-expanded', 'true');
  });

  it('selecting a work order supersedes pending bucket entry and clears its feedback', async () => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />);
    await user.click(bucket()); await screen.findByRole('button', { name: /ROOT-WO .*Show material/ });
    const pending = deferred<Awaited<ReturnType<typeof fetchPlanningWindow>>>();
    vi.mocked(fetchPlanningWindow).mockReturnValue(pending.promise);
    await user.click(bucket(2)); expect(screen.getByRole('status')).toHaveTextContent('Loading work orders');
    await user.click(card()); await screen.findByRole('button', { name: 'SUB' });
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
    await act(async () => pending.resolve({ snapshotId: 'snapshot-A', workOrders: [order('OBSOLETE')] }));
    expect(crumbs()).toEqual(['MPS grid', 'PARENT', 'Week of Oct 12', 'WO ROOT-WO']);
    expect(screen.queryByText('OBSOLETE')).not.toBeInTheDocument();
  });

  it('a late analysis cannot replace the newly selected work order material', async () => {
    const pending = deferred<WorkOrderImmediateMaterialAnalysisResponseDto>();
    vi.mocked(fetchPlanningWindow).mockResolvedValue({ snapshotId: 'snapshot-A', workOrders: [order(), order('OTHER-WO')] });
    vi.mocked(fetchWorkOrderImmediateMaterial).mockImplementation(async (_ws, _snapshot, woid) => woid === 'ROOT-WO' ? pending.promise : analysis('OTHER-WO', [material('OTHER-COMPONENT', 'short')]));
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />);
    await user.click(bucket()); await user.click(await screen.findByRole('button', { name: /ROOT-WO .*Show material/ }));
    await waitFor(() => expect(fetchWorkOrderImmediateMaterial).toHaveBeenCalledTimes(1));
    await user.click(card('OTHER-WO')); await screen.findByRole('button', { name: 'OTHER-COMPONENT' });
    await act(async () => pending.resolve(analysis()));
    expect(screen.queryByRole('button', { name: 'SUB' })).not.toBeInTheDocument();
    expect(crumbs()[crumbs().length - 1]).toBe('WO OTHER-WO');
  });

  it('a late candidate list cannot reopen an abandoned branch or replace another component', async () => {
    const pending = deferred<Awaited<ReturnType<typeof fetchWorkOrderCandidates>>>();
    vi.mocked(fetchWorkOrderImmediateMaterial).mockResolvedValue(analysis('ROOT-WO', [material('SUB', 'notApplicable'), material('SECOND-SUB', 'notApplicable')]));
    vi.mocked(fetchWorkOrderCandidates).mockImplementation(async (_ws, _snapshot, _origin, component) => component === 'SUB' ? pending.promise : { snapshotId: 'snapshot-A', candidates: [order('SECOND-WO', component)] });
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />); await openMaterial(user);
    await user.click(screen.getByRole('button', { name: 'View work orders for SUB' }));
    await waitFor(() => expect(fetchWorkOrderCandidates).toHaveBeenCalledTimes(1));
    await user.keyboard('{Escape}');
    await user.click(screen.getByRole('button', { name: 'View work orders for SECOND-SUB' }));
    await screen.findByRole('button', { name: /SECOND-WO .*Show material/ });
    await act(async () => pending.resolve({ snapshotId: 'snapshot-A', candidates: [order('OBSOLETE-CANDIDATE')] }));
    expect(screen.queryByText('OBSOLETE-CANDIDATE')).not.toBeInTheDocument();
    expect(crumbs()[crumbs().length - 1]).toBe('SECOND-SUB work orders');
  });

  it.each(['snapshot', 'workspace', 'basis', 'departure'] as const)('invalidates loading and navigation on %s change, even after returning', async (change) => {
    const pending = deferred<Awaited<ReturnType<typeof fetchPlanningWindow>>>();
    const user = userEvent.setup(); const original = props(); const view = render(<MpsWorkspace {...original} />);
    await user.click(screen.getByText('PARENT'));
    vi.mocked(fetchPlanningWindow).mockReturnValue(pending.promise);
    await user.click(bucket());
    const next = { ...original, dashboard: change === 'snapshot' ? { ...original.dashboard, snapshot: { ...original.dashboard.snapshot, snapshotId: 'snapshot-B' } } : original.dashboard, assignmentId: change === 'workspace' ? 'workspace-B' : original.assignmentId, dateBasis: change === 'basis' ? 'releaseDate' as const : original.dateBasis, active: change !== 'departure' };
    view.rerender(<MpsWorkspace {...next} />);
    expect(crumbs()).not.toContain('Week of Oct 12');
    view.rerender(<MpsWorkspace {...original} />);
    await act(async () => pending.resolve({ snapshotId: 'snapshot-A', workOrders: [order('OBSOLETE')] }));
    expect(crumbs()).not.toContain('Week of Oct 12'); expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('replaces candidate levels, retains context and return state, and enforces depth 3', async () => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />); await openMaterial(user);
    const filter = screen.getByRole('button', { name: 'All 4' });
    const scroller = document.querySelector<HTMLElement>('.shortages-panel__scroll')!;
    Object.defineProperties(scroller, { scrollWidth: { configurable: true, value: 900 }, clientWidth: { configurable: true, value: 300 } });
    scroller.scrollLeft = 93;
    const detailViewport = scroller.closest<HTMLElement>('.mps-detail__scroll')!;
    Object.defineProperties(detailViewport, { scrollHeight: { configurable: true, value: 1200 }, clientHeight: { configurable: true, value: 400 } });
    detailViewport.scrollTop = 180;
    const opener = screen.getByRole('button', { name: 'View work orders for SUB' });
    await user.click(opener);
    // Simulate a browser clamping the hidden pane's scroll; returning must restore the saved value.
    scroller.scrollLeft = 0;
    detailViewport.scrollTop = 0;
    expect(screen.queryByRole('heading', { name: 'Immediate Material Analysis - ROOT-WO' })).not.toBeInTheDocument();
    expect(await screen.findByText('Select a work order to see its immediate material analysis.')).toBeInTheDocument();
    expect(screen.getByText(/Opened from WO ROOT-WO/)).toHaveTextContent('Line needs 12, issued 2');
    expect(screen.getByText(/not linked in QAD/)).toBeInTheDocument();
    expect(fetchWorkOrderImmediateMaterial).toHaveBeenCalledTimes(1);
    expect(card('SUB-WO')).toHaveAttribute('aria-expanded', 'false');
    await user.click(card('SUB-WO'));
    await user.click(await screen.findByRole('button', { name: 'Needs attention 1' }));
    // All is needed to reveal the manufactured line, without changing classifications.
    await user.click(screen.getByRole('button', { name: 'All 2' }));
    await user.click(screen.getByRole('button', { name: 'View work orders for DEEP' }));
    await user.click(await screen.findByRole('button', { name: /DEEP-WO .*Show material/ }));
    expect(await screen.findByRole('button', { name: 'View work orders for AT-LIMIT' })).toBeDisabled();
    expect(fetchWorkOrderCandidates).toHaveBeenCalledWith('workspace-A', 'snapshot-A', 'ROOT-WO', 'SUB', 2, 'dueDate');
    expect(fetchWorkOrderCandidates).toHaveBeenCalledWith('workspace-A', 'snapshot-A', 'SUB-WO', 'DEEP', 3, 'dueDate');
    expect(fetchWorkOrderCandidates).toHaveBeenCalledTimes(2);
    await user.click(within(trail()!).getByRole('button', { name: 'WO ROOT-WO' }));
    await waitFor(() => expect(opener).toHaveFocus());
    expect(filter).toHaveAttribute('aria-pressed', 'true'); expect(scroller.scrollLeft).toBe(93);
    expect(detailViewport.scrollTop).toBe(180);
    expect(screen.getByRole('button', { name: /ROOT-WO .*Material shown below/ })).toHaveAttribute('aria-expanded', 'true');
    expect(crumbs()).toEqual(['MPS grid', 'PARENT', 'Week of Oct 12', 'WO ROOT-WO']);
  });

  it.each([
    { ancestor: 'Week of Oct 12', expected: ['MPS grid', 'PARENT', 'Week of Oct 12'], focus: 'ROOT-WO' },
    { ancestor: 'SUB work orders', expected: ['MPS grid', 'PARENT', 'Week of Oct 12', 'WO ROOT-WO', 'SUB work orders'], focus: 'SUB-WO' },
  ])('jumps directly from deep material to $ancestor and restores that list', async ({ ancestor, expected, focus }) => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />); await openMaterial(user);
    await user.click(screen.getByRole('button', { name: 'View work orders for SUB' }));
    await user.click(await screen.findByRole('button', { name: /SUB-WO .*Show material/ }));
    await user.click(await screen.findByRole('button', { name: 'View work orders for DEEP' }));
    await user.click(await screen.findByRole('button', { name: /DEEP-WO .*Show material/ }));
    await screen.findByRole('button', { name: 'View work orders for AT-LIMIT' });
    await user.click(within(trail()!).getByRole('button', { name: ancestor }));
    expect(crumbs()).toEqual(expected);
    await waitFor(() => expect(card(focus)).toHaveFocus());
    expect(card(focus)).toHaveAttribute('aria-expanded', 'false');
  });

  it('preserves a candidate attention filter across deeper traversal and restores focus at each level', async () => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />); await openMaterial(user);
    await user.click(screen.getByRole('button', { name: 'View work orders for SUB' }));
    await user.click(await screen.findByRole('button', { name: /SUB-WO .*Show material/ }));
    const deep = await screen.findByRole('button', { name: 'View work orders for DEEP' });
    await user.click(deep);
    await screen.findByRole('heading', { name: 'Work Orders for DEEP' });
    await user.keyboard('{Escape}'); await waitFor(() => expect(deep).toHaveFocus());
    await user.click(screen.getByRole('button', { name: 'Needs attention 1' }));
    await user.click(screen.getByRole('tab', { name: 'Part Info' }));
    expect(crumbs()).toEqual(['MPS grid', 'PARENT']);
    expect(screen.getByRole('heading', { name: 'Part Info — PARENT' })).toBeInTheDocument();
    await user.click(screen.getByRole('tab', { name: 'BOM' })); expect(crumbs()).toEqual(['MPS grid', 'PARENT']);
    await user.click(screen.getByRole('tab', { name: 'Work Orders' }));
    expect(screen.getByRole('button', { name: 'Needs attention 1' })).toHaveAttribute('aria-pressed', 'true');
    expect(crumbs()).toEqual(['MPS grid', 'PARENT', 'Week of Oct 12', 'WO ROOT-WO', 'SUB work orders', 'WO SUB-WO']);
    await user.keyboard('{Escape}');
    await waitFor(() => expect(card('SUB-WO')).toHaveFocus());
  });

  it('keeps the selected MPS grid, trail and tabs outside every scrolling detail viewport', async () => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />); await openMaterial(user);
    const grid = screen.getByRole('table', { name: 'MPS grid' });
    const tablist = screen.getByRole('tablist', { name: 'Part detail' });
    const rootViewport = screen.getByRole('heading', { name: 'Immediate Material Analysis - ROOT-WO' }).closest<HTMLElement>('.mps-detail__scroll')!;
    expect(rootViewport).not.toBeNull();
    expect(rootViewport).toContainElement(screen.getByRole('button', { name: /ROOT-WO .*Material shown below/ }).closest('li'));
    for (const pinned of [grid, trail(), tablist]) expect(pinned?.closest('.mps-detail__scroll')).toBeNull();
    expect(rootViewport.closest('.mps-detail__content')?.previousElementSibling).toBe(tablist);
    rootViewport.scrollTop = 120;

    await user.click(screen.getByRole('button', { name: 'View work orders for SUB' }));
    await screen.findByRole('button', { name: /SUB-WO .*Show material/ });
    const candidateViewport = screen.getByRole('heading', { name: 'Work Orders for SUB' }).closest<HTMLElement>('.mps-detail__scroll')!;
    expect(candidateViewport).not.toBe(rootViewport);
    expect(candidateViewport.scrollTop).toBe(0);
    expect(rootViewport).not.toBeVisible();
    candidateViewport.scrollTop = 80;
    await user.click(screen.getByRole('tab', { name: 'Part Info' }));
    expect(screen.getByRole('heading', { name: 'Part Info — PARENT' }).closest('.mps-detail__scroll')).not.toBeNull();
    await user.click(screen.getByRole('tab', { name: 'BOM' }));
    expect(document.querySelector('.bom-panel')?.closest('.mps-detail__scroll')).not.toBeNull();
    await user.click(screen.getByRole('tab', { name: 'Work Orders' }));
    expect(candidateViewport).toBeVisible();
    expect(candidateViewport.scrollTop).toBe(80);
    for (const pinned of [grid, trail(), tablist]) expect(pinned?.closest('.mps-detail__scroll')).toBeNull();
  });

  it('closes nested Material Detail first, traps Tab and restores its triggering control', async () => {
    const user = userEvent.setup(); render(<MpsWorkspace {...props()} />); await openMaterial(user);
    await user.click(screen.getByRole('button', { name: 'View work orders for SUB' }));
    await user.click(await screen.findByRole('button', { name: /SUB-WO .*Show material/ }));
    const detail = await screen.findByRole('button', { name: 'SUB-SHORT' }); await user.click(detail);
    const close = screen.getByRole('button', { name: 'Close Material Detail' }); expect(close).toHaveFocus();
    await user.tab(); expect(close).toHaveFocus();
    await user.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(detail).toHaveFocus();
    expect(crumbs()[crumbs().length - 1]).toBe('WO SUB-WO');
    expect(screen.queryByRole('tab', { name: 'Shortages' })).not.toBeInTheDocument();
  });

  it('filters only API Short/Unknown, preserving values, reasons and order without claiming a missing part', async () => {
    const user = userEvent.setup();
    const rows = [material('SUB', 'notApplicable'), material('SHORT', 'short'), { ...material('UNKNOWN', 'unknown'), diagnostic: 'Source inventory is unavailable; a missing part was not established.' }, material('AVAILABLE', 'onHand')];
    vi.mocked(fetchWorkOrderImmediateMaterial).mockResolvedValue(analysis('ROOT-WO', rows));
    render(<MpsWorkspace {...props()} />); await openMaterial(user);
    const grid = document.querySelector<HTMLElement>('.shortages-panel table')!;
    expect(within(grid).getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('button')[0].textContent)).toEqual(['UNKNOWN', 'SHORT', 'AVAILABLE', 'SUB']);
    await user.click(screen.getByRole('button', { name: 'Needs attention 2' }));
    expect(within(grid).getAllByRole('row')).toHaveLength(3);
    expect(within(grid).getByText('Unable to confirm availability')).toBeInTheDocument();
    expect(within(grid).getByText(rows[2].diagnostic!)).toBeInTheDocument();
    expect(screen.queryByText('Part not found')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'SUB' })).not.toBeInTheDocument();
    expect(within(grid).getAllByText('-10')).toHaveLength(2);
  });
});

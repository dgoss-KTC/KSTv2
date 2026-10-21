import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ComponentProps } from 'react';
import type { MpsDashboardResponseDto, WorkspaceAssignmentDto, BomResponseDto, WorkOrderImmediateMaterialComponentDto } from '../api/client';
import { ApplicationShell } from './ApplicationShell';
import { partInfoFixture, mrpFixture } from '../reports/partInfoMrpTestSupport';
import { deferred, clearExportTests } from '../reports/reportTestSupport';
import { afterEach } from 'vitest';

const model = vi.hoisted(() => ({ workspaces: [] as WorkspaceAssignmentDto[], dashboard: null as MpsDashboardResponseDto | null,
  spec: vi.fn(), lookup: vi.fn(), mrp: vi.fn(), load: vi.fn(), select: vi.fn(), invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: model.invoke }));
vi.mock('@tauri-apps/api/event', () => ({ listen: vi.fn(async () => () => {}) }));
vi.mock('../api/partInfoApi', async (original) => {
  const actual = await original<typeof import('../api/partInfoApi')>();
  return { ...actual, getPartInfoSpec: model.spec, lookupPartInfo: model.lookup };
});
vi.mock('../api/componentMrpApi', async (original) => {
  const actual = await original<typeof import('../api/componentMrpApi')>();
  return { ...actual, readComponentMrp: model.mrp };
});
vi.mock('../hooks/useBackendStatus', () => ({ useBackendStatus: () => ({ connectionState: 'connected', status: null, triggerRefresh: vi.fn() }) }));
vi.mock('../hooks/usePreferences', () => ({ usePreferences: () => ({ preferences: { accentColor: 'blue', rowDensity: 'compact' }, resolvedTheme: 'light', updatePreferences: vi.fn() }) }));
vi.mock('../hooks/useWorkspaces', () => ({ useWorkspaces: () => ({ state: { activeId: 'origin' }, activeWorkspaces: model.workspaces, archivedWorkspaces: [],
  load: model.load, selectWorkspace: model.select }) }));
vi.mock('../hooks/useMpsDashboard', () => ({ MIN_MPS_HORIZON_WEEKS: 4, MAX_MPS_HORIZON_WEEKS: 72,
  useMpsDashboard: () => ({ dashboard: model.dashboard, dateBasis: 'dueDate', horizonWeeks: 4, isLoading: false, isRefreshing: false,
    error: null, setDateBasis: vi.fn(), setHorizonWeeks: vi.fn(), reload: vi.fn(), refresh: vi.fn() }) }));
vi.mock('../hooks/usePartDetail', () => ({ usePartDetail: () => ({ detail: null, isLoading: false, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/usePlanningWindowWorkOrders', () => ({ usePlanningWindowWorkOrders: () => ({ workOrders: [], isLoading: false, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/useComponentDetail', () => ({ useComponentDetail: () => ({ detail: null, isLoading: true, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/useApprovedVendors', () => ({ useApprovedVendors: () => ({ rows: null, isLoading: false, error: null, activate: vi.fn(), retry: vi.fn() }) }));
vi.mock('../hooks/useWorkOrderImmediateMaterial', () => ({ useWorkOrderImmediateMaterial: () => ({ analysis: null, isLoading: false, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/useWorkOrderImmediateMaterialSummary', () => ({ useWorkOrderImmediateMaterialSummary: () => ({ summary: null, error: null }) }));
vi.mock('../api/workOrdersApi', () => ({ fetchPlanningWindow: vi.fn(async () => ({ snapshotId: 'snapshot-A', workOrders: [{ woid: 'ROOT-WO' }] })), toWorkOrdersApiError: vi.fn() }));
vi.mock('../hooks/useBom', () => ({ useBom: () => ({ bom, isLoading: false, error: null, activate: vi.fn(), retry: vi.fn() }) }));
vi.mock('./WorkOrdersPanel', () => ({ WorkOrdersPanel: (props: ComponentProps<typeof import('./WorkOrdersPanel').WorkOrdersPanel>) =>
  <div><button onClick={() => props.onSelectWoid('ROOT-WO')}>Open root material</button>
    <button onClick={event => props.onViewWorkOrders?.({ componentPart: 'SUB-COMP' } as WorkOrderImmediateMaterialComponentDto, event.currentTarget)}>Open candidate trail</button></div> }));
vi.mock('./WorkOrderCandidatePanel', () => ({ WorkOrderCandidatePanel: (props: ComponentProps<typeof import('./WorkOrderCandidatePanel').WorkOrderCandidatePanel>) =>
  <div><span>Candidate trail {props.componentPart} from {props.immediateParentWoid}</span><input aria-label="Candidate filter" defaultValue="" />
    <button onClick={event => props.onSelectWoid?.('CANDIDATE-WO', event.currentTarget)}>Select candidate</button><span>{props.selectedWoid}</span></div> }));
vi.mock('./PlanningWorkbookPanel', () => ({ PlanningWorkbookPanel: ({ proposals }: ComponentProps<typeof import('./PlanningWorkbookPanel').PlanningWorkbookPanel>) =>
  <div><span>Proposals: {proposals!.stagedCount}</span><button onClick={() => proposals!.setAdjustment('PARENT', '2026-10-19', '7')}>Stage proposal</button></div> }));

const bom: BomResponseDto = { site: 'NW', parentPart: 'PARENT', effectiveDate: '2026-10-21', loadedAtUtc: '2026-10-21T12:00:00Z', isStale: false, warning: null,
  lines: [{ occurrenceKey: 'occurrence-1', level: 1, componentPart: 'SELECTED-COMP', pmCode: 'P', isPhantom: false, description: 'Selected component', quantityPer: 3, scrapPercentage: 0, netQuantityOnHand: 4, nonNetQuantityOnHand: 0 }] };
const specification = { sites: ['SW', 'NW', 'KV'], maximumPartLength: 256 };
beforeEach(() => {
  localStorage.clear(); localStorage.setItem('kst.componentMrp.site.v1', 'KV');
  model.workspaces = [{ assignmentId: 'origin', displayName: 'Origin workspace', site: 'NW', parentParts: ['PARENT'], productLineFrom: null, productLineTo: null,
    isTemporary: false, coverageEndsOn: null, isEnabled: true, sortOrder: 0 }];
  model.dashboard = { snapshot: { snapshotId: 'snapshot-A', site: 'NW', workspaceId: 'origin', createdAtUtc: '2026-10-21T12:00:00Z',
    lastSuccessfulRefreshAtUtc: '2026-10-21T12:00:00Z', status: 'current', resolvedParentPartCount: 1, sourceRowCount: 1, isRefreshInProgress: false, lastRefreshError: null },
    dateBasis: 'dueDate', horizonWeeks: 4, parts: [{ parentPart: 'PARENT', description: 'Parent assembly', buckets: [
      { kind: 'falldown', weekLabel: null, quantity: '5', executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false }] }] };
  model.spec.mockReset().mockResolvedValue(specification); model.lookup.mockReset().mockImplementation(async (site, part) => partInfoFixture(part, site));
  model.mrp.mockReset().mockImplementation(async input => mrpFixture(input)); model.select.mockReset(); model.invoke.mockReset().mockResolvedValue(null);
});
afterEach(clearExportTests);
async function openBom(candidate = false) {
  if (candidate) {
    fireEvent.click(screen.getByText('5', { selector: '.mps-cell--clickable' }));
    fireEvent.click(await screen.findByText('Open root material')); fireEvent.click(screen.getByText('Open candidate trail'));
    fireEvent.change(screen.getByLabelText('Candidate filter'), { target: { value: 'retained candidate filter' } });
    fireEvent.click(screen.getByText('Select candidate'));
  } else fireEvent.click(screen.getByText('PARENT', { selector: '.mps-grid__part-number' }));
  fireEvent.click(screen.getByRole('tab', { name: 'BOM' }));
  fireEvent.change(screen.getByLabelText('Filter by Component Item'), { target: { value: 'SELECTED' } });
  const row = screen.getByText('SELECTED-COMP').closest('tr')!;
  fireEvent.click(row); await screen.findByRole('dialog', { name: 'Component Information' }); return row;
}
async function arrive() { await screen.findByRole('table', { name: 'Component MRP periods' }); }

describe('shell → retained Customer/MPS → Reports real lookup integration (offline ports)', () => {
  it('replaces stale manual Reports selection/inputs only after successful shortcut departure', async () => {
    render(<ApplicationShell />); fireEvent.click(screen.getByRole('tab', { name: 'Reports' }));
    await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled());
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'STALE-REPORT-PART' } });
    fireEvent.click(screen.getByRole('button', { name: 'Shipments-To-Go' }));
    fireEvent.click(screen.getByRole('tab', { name: 'Origin workspace' })); await openBom();
    fireEvent.click(screen.getByRole('button', { name: 'Show MRP' })); await arrive();
    expect(screen.getByRole('heading', { name: 'Part Info & MRP' })).toBeInTheDocument();
    expect(screen.getByLabelText('Part Number')).toHaveValue('SELECTED-COMP'); expect(screen.getByLabelText('Site')).toHaveValue('NW');
    expect(model.lookup).toHaveBeenCalledExactlyOnceWith('NW', 'SELECTED-COMP', expect.any(AbortSignal));
    expect(model.invoke).not.toHaveBeenCalledWith('component_mrp_save', expect.anything());
  });
  it.each(['back', 'workspace'])('preserves parent/bucket/candidate/tab/filters/scrollers/focus by %s return, and Reports owns Escape', async mode => {
    const view = render(<ApplicationShell />); const row = await openBom(true);
    const grid = view.container.querySelector<HTMLElement>('.mps-grid-scroll')!;
    const detail = view.container.querySelector<HTMLElement>('.mps-detail__scroll')!;
    const shell = view.container.querySelector<HTMLElement>('.shell__content')!;
    Object.defineProperty(shell, 'scrollHeight', { configurable: true, value: 1000 });
    grid.scrollLeft = 131; detail.scrollTop = 217; shell.scrollTop = 81;
    const show = screen.getByRole('button', { name: 'Show MRP' });
    fireEvent.click(show); fireEvent.click(show); await arrive();
    expect(model.lookup).toHaveBeenCalledExactlyOnceWith('NW', 'SELECTED-COMP', expect.any(AbortSignal));
    expect(model.mrp).toHaveBeenCalledExactlyOnceWith({ site: 'NW', part: 'SELECTED-COMP', horizonWeeks: 26, period: 'Week', includeUnconfirmed: true }, expect.any(AbortSignal));
    expect(screen.queryByRole('dialog')).toBeNull(); expect(view.container.querySelector('.shell__workspace')).toHaveAttribute('inert');
    fireEvent.keyDown(screen.getByLabelText('Part Number'), { key: 'Escape' });
    view.rerender(<ApplicationShell />); expect(model.lookup).toHaveBeenCalledTimes(1);
    grid.scrollLeft = 0; detail.scrollTop = 0; shell.scrollTop = 0;
    fireEvent.click(mode === 'back' ? screen.getByRole('button', { name: /Back to MPS/ }) : screen.getByRole('tab', { name: 'Origin workspace' }));
    await waitFor(() => expect(row).toHaveFocus());
    expect(grid.scrollLeft).toBe(131); expect(detail.scrollTop).toBe(217); expect(shell.scrollTop).toBe(81);
    expect(screen.getByRole('tab', { name: 'BOM' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByLabelText('Filter by Component Item')).toHaveValue('SELECTED');
    fireEvent.click(screen.getByRole('tab', { name: 'Work Orders' }));
    expect(screen.getByText('Candidate trail SUB-COMP from ROOT-WO')).toBeVisible(); expect(screen.getByText('CANDIDATE-WO')).toBeVisible();
    expect(screen.getByLabelText('Candidate filter')).toHaveValue('retained candidate filter');
    expect(screen.getByRole('navigation', { name: 'MPS drill-down trail' })).toHaveTextContent('Falldown');
    fireEvent.click(screen.getByRole('tab', { name: 'BOM' })); fireEvent.click(screen.getByText('SELECTED-COMP').closest('tr')!); fireEvent.click(screen.getByRole('button', { name: 'Show MRP' }));
    await arrive(); expect(model.lookup).toHaveBeenCalledTimes(2); // same identity, new visit
    fireEvent.click(screen.getByRole('button', { name: /Back to MPS/ }));
    fireEvent.click(screen.getByRole('tab', { name: 'Reports' }));
    await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled()); expect(model.lookup).toHaveBeenCalledTimes(2);
    expect(screen.queryByRole('button', { name: /Back to MPS/ })).toBeNull(); // ordinary manual entry
  });
  it('retains the modal/proposals/focus and issues no request on cancelled dirty confirmation; approved discard is real', async () => {
    vi.spyOn(window, 'confirm').mockImplementation((() => Promise.resolve(false)) as unknown as typeof window.confirm);
    render(<ApplicationShell />);
    fireEvent.click(screen.getByRole('tab', { name: 'Planning' })); fireEvent.click(screen.getByText('Stage proposal'));
    fireEvent.click(screen.getByRole('tab', { name: 'Dashboard' })); const row = await openBom();
    const show = screen.getByRole('button', { name: 'Show MRP' }); show.focus(); fireEvent.click(show);
    const confirm = await screen.findByRole('alertdialog', { name: 'Discard unsaved changes?' });
    expect(screen.getByRole('dialog', { name: 'Component Information' })).toBeInTheDocument(); expect(model.lookup).not.toHaveBeenCalled();
    fireEvent.keyDown(confirm, { key: 'Escape' }); await waitFor(() => expect(show).toHaveFocus());
    expect(screen.getByRole('dialog', { name: 'Component Information' })).toBeInTheDocument();
    fireEvent.click(show); fireEvent.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Discard changes' })); await arrive();
    fireEvent.click(screen.getByRole('button', { name: /Back to MPS/ })); await waitFor(() => expect(row).toHaveFocus());
    fireEvent.click(screen.getByRole('tab', { name: 'Planning' })); expect(screen.getByText('Proposals: 0')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('tab', { name: 'Reports' })); expect(screen.queryByRole('alertdialog')).toBeNull();
  });
  it('waits for supported-site specification and cancels before delayed settings/replies can issue or replay a lookup', async () => {
    const settings = deferred<typeof specification>(); model.spec.mockReturnValue(settings.promise);
    render(<ApplicationShell />); await openBom(); fireEvent.click(screen.getByRole('button', { name: 'Show MRP' }));
    await screen.findByRole('button', { name: /Back to MPS/ }); expect(model.lookup).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: /Back to MPS/ })); await act(async () => settings.resolve(specification));
    expect(model.lookup).not.toHaveBeenCalled(); fireEvent.click(screen.getByRole('tab', { name: 'Reports' }));
    await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled()); expect(model.lookup).not.toHaveBeenCalled();
  });
  it.each(['snapshot', 'workspace', 'archived'])('uses safe fallback and clear feedback for invalidated %s origin', async change => {
    const view = render(<ApplicationShell />); await openBom(); fireEvent.click(screen.getByRole('button', { name: 'Show MRP' })); await arrive();
    if (change === 'snapshot') model.dashboard = { ...model.dashboard!, snapshot: { ...model.dashboard!.snapshot, snapshotId: 'snapshot-B' } };
    if (change === 'workspace') model.workspaces = [{ ...model.workspaces[0], parentParts: ['REPLACEMENT'] }];
    if (change === 'archived') model.workspaces = [];
    view.rerender(<ApplicationShell />); fireEvent.click(screen.getByRole('button', { name: /Back to MPS/ }));
    await screen.findByText('The originating MPS investigation is no longer available. Showing the current workspace instead.');
    expect(screen.queryByRole('navigation', { name: 'MPS drill-down trail' })).toBeNull(); expect(screen.queryByRole('dialog')).toBeNull();
  });
});

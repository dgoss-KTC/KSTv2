import { act, cleanup, fireEvent, render, renderHook, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import type { BomResponseDto, MpsDashboardResponseDto } from '../api/client';
import { MpsWorkspace } from './MpsWorkspace';
import { BomPanel } from './BomPanel';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';
import { dismissAnalyticalSave, recordAnalyticalSave, useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';

const host = vi.hoisted(() => ({ invoke: vi.fn(), fiscal: { anchorFiscalYear: 2026, anchorStartDate: '2025-06-29', exceptions: [] } }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => true }));
vi.mock('../hooks/useFiscalCalendarSettings', () => ({ useFiscalCalendarSettings: () => ({ settings: host.fiscal }) }));
vi.mock('../hooks/usePartDetail', () => ({ usePartDetail: () => ({ detail: null, isLoading: false, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/useBom', () => ({ useBom: () => ({ bom: null, isLoading: false, error: null, activate: vi.fn(), retry: vi.fn() }) }));
vi.mock('../hooks/usePlanningWindowWorkOrders', () => ({ usePlanningWindowWorkOrders: () => ({ workOrders: null, isLoading: false, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/useWorkOrderImmediateMaterial', () => ({ useWorkOrderImmediateMaterial: () => ({ analysis: null, isLoading: false, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/useWorkOrderImmediateMaterialSummary', () => ({ useWorkOrderImmediateMaterialSummary: () => ({ summary: null, error: null }) }));
vi.mock('../hooks/useComponentDetail', () => ({ useComponentDetail: () => ({ detail: null, isLoading: false, error: null, retry: vi.fn() }) }));
vi.mock('../hooks/useApprovedVendors', () => ({ useApprovedVendors: () => ({ rows: null, isLoading: false, error: null, activate: vi.fn(), retry: vi.fn() }) }));

function dashboard(): MpsDashboardResponseDto {
  return { snapshot: { snapshotId: 'snapshot', workspaceId: 'workspace', site: 'SW', createdAtUtc: '2026-10-15T12:00:00Z', lastSuccessfulRefreshAtUtc: '2026-10-15T12:00:00Z', status: 'current', resolvedParentPartCount: 2, sourceRowCount: 2, isRefreshInProgress: false, lastRefreshError: null }, dateBasis: 'dueDate', horizonWeeks: 2,
    parts: ['PARENT', 'OTHER'].map(parentPart => ({ parentPart, description: 'Description', buckets: [{ kind: 'falldown', weekLabel: null, quantity: 0, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false }, ...['2026-10-12', '2026-10-19'].map(weekLabel => ({ kind: 'weekly' as const, weekLabel, quantity: 1, executionStatus: 'none' as const, containsPlannedWork: false, containsExplicitlyScheduledWork: false }))] })) };
}
function props() { return { assignmentId: 'workspace', dashboard: dashboard(), dateBasis: 'dueDate' as const, horizonWeeks: 2, isLoading: false, isRefreshing: false, error: null, setDateBasis: vi.fn(), setHorizonWeeks: vi.fn(), reload: vi.fn(), refresh: vi.fn() }; }
const bom: BomResponseDto = { site: 'SW', parentPart: 'PARENT', effectiveDate: '2026-10-15', loadedAtUtc: '2026-10-15T12:00:00Z', isStale: true, warning: 'Loaded last-known BOM', lines: [
  { occurrenceKey: '1', level: 1, componentPart: 'X', description: 'Other', pmCode: 'M', isPhantom: false, quantityPer: 1, scrapPercentage: 0, netQuantityOnHand: 1, nonNetQuantityOnHand: 0 },
  { occurrenceKey: '2', level: 3, componentPart: 'REPEAT', description: '+PCB', pmCode: 'P', isPhantom: true, quantityPer: null, scrapPercentage: null, netQuantityOnHand: '1296899.0000000000', nonNetQuantityOnHand: 0 },
  { occurrenceKey: '3', level: 5, componentPart: 'REPEAT', description: '+PCB', pmCode: 'P', isPhantom: true, quantityPer: '0.1234567890123456789', scrapPercentage: 2.5, netQuantityOnHand: 12, nonNetQuantityOnHand: 0 },
] };
async function activate(label: string) {
  fireEvent.click(within(screen.getByLabelText(`${label} export`)).getByRole('button', { name: 'Export' }));
  fireEvent.click(screen.getByRole('menuitem', { name: /Excel workbook/ }));
  await screen.findByRole('button', { name: 'Save again' });
}
const generations = () => host.invoke.mock.calls.filter(([command]) => command === 'component_mrp_generate');
beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] }); vi.setSystemTime(new Date(2026, 9, 15, 12));
  host.fiscal = { anchorFiscalYear: 2026, anchorStartDate: '2025-06-29', exceptions: [] };
  host.invoke.mockReset(); host.invoke.mockImplementation(async (command: string) => command === 'component_mrp_begin' ? 1 : command === 'component_mrp_generate' ? { preparationId: 'exact', fileName: 'loaded.xlsx', expiresAtUtc: '2026-10-15T12:30:00Z' } : command === 'component_mrp_save' ? { outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true } : false);
});
afterEach(async () => {
  await act(async () => {}); cleanup(); const hook = renderHook(useAnalyticalExportStatus);
  act(() => { for (const operation of hook.result.current.operations) { recordAnalyticalSave(operation.id, { outcome: 'failed', message: 'Test cleanup' }); dismissAnalyticalSave(operation.id); } });
  hook.unmount(); vi.useRealTimers();
});

it('places MPS export in its caption and exports all parents despite drilldown, cloned data and scrolling', async () => {
  const initial = props(); const view = render(<><AnalyticalSaveStatus /><MpsWorkspace {...initial} /></>);
  const control = screen.getByLabelText('MPS export'); expect(control.closest('.mps-grid-caption')).not.toBeNull(); expect(control.closest('.mps-workspace__header')).toBeNull();
  expect(screen.getByText('Export includes all 2 parts')).toBeInTheDocument();
  await activate('MPS'); const request = generations()[0][1].analyticalRequest;
  expect(request).toMatchObject({ kind: 'MPS', parent: null, businessDate: '2026-10-15', dateBasis: 'dueDate', horizonWeeks: 2, isStale: false, fiscalLabels: ['FY2027 Q2 P4', 'FY2027 Q2 P4'] });
  fireEvent.click(within(screen.getByRole('table', { name: 'MPS grid' })).getByText('PARENT'));
  expect(screen.getByText(/Showing 1 of 2 parts/)).toBeInTheDocument(); expect(screen.getByText('Export includes all 2 parts')).toBeInTheDocument();
  const exportButton = within(screen.getByLabelText('MPS export')).getByRole('button', { name: 'Export' });
  fireEvent.click(exportButton); fireEvent.keyDown(screen.getByRole('menu'), { key: 'Escape' });
  expect(screen.queryByRole('menu')).not.toBeInTheDocument(); expect(exportButton).toHaveFocus();
  expect(screen.getByText(/Showing 1 of 2 parts/)).toBeInTheDocument(); // Menu Escape must not unwind the MPS trail.
  fireEvent.scroll(document.querySelector('.mps-grid-scroll')!);
  view.rerender(<><AnalyticalSaveStatus /><MpsWorkspace {...initial} dashboard={JSON.parse(JSON.stringify(initial.dashboard))} /></>);
  expect(screen.getByRole('button', { name: 'Save again' })).toBeEnabled();
  expect(host.invoke.mock.calls.filter(([name]) => name === 'sop_discard_generation')).toHaveLength(0);
  fireEvent.click(screen.getByRole('button', { name: 'Save again' })); await waitFor(() => expect(host.invoke.mock.calls.filter(([name]) => name === 'component_mrp_save')).toHaveLength(2));
  expect(generations()).toHaveLength(1);
});
it('invalidates retained MPS bytes when fiscal settings change, not when their object is merely cloned', async () => {
  const initial = props(); const view = render(<><AnalyticalSaveStatus /><MpsWorkspace {...initial} /></>); await activate('MPS');
  host.fiscal = { ...host.fiscal, exceptions: [] }; view.rerender(<><AnalyticalSaveStatus /><MpsWorkspace {...initial} /></>);
  expect(screen.getByRole('button', { name: 'Save again' })).toBeInTheDocument();
  host.fiscal = { anchorFiscalYear: 2027, anchorStartDate: '2026-07-05', exceptions: [] }; view.rerender(<><AnalyticalSaveStatus /><MpsWorkspace {...initial} /></>);
  expect(screen.queryByRole('button', { name: 'Save again' })).not.toBeInTheDocument();
  await waitFor(() => expect(host.invoke).toHaveBeenCalledWith('sop_discard_generation', { epoch: 1 }));
});
it.each(['refresh', 'basis', 'horizon', 'snapshot', 'site', 'error', 'departure'] as const)('invalidates MPS preparation on meaningful %s changes', async change => {
  const initial = props(); const view = render(<><AnalyticalSaveStatus /><MpsWorkspace {...initial} /></>); await activate('MPS');
  const changed = change === 'refresh' ? { ...initial, isRefreshing: true } : change === 'basis' ? { ...initial, dateBasis: 'releaseDate' as const } : change === 'horizon' ? { ...initial, horizonWeeks: 4 } : change === 'snapshot' ? { ...initial, dashboard: { ...initial.dashboard, snapshot: { ...initial.dashboard.snapshot, snapshotId: 'replaced' } } } : change === 'site' ? { ...initial, dashboard: { ...initial.dashboard, snapshot: { ...initial.dashboard.snapshot, site: 'NW' } } } : change === 'error' ? { ...initial, error: { type: 'unavailable' as const, detail: 'Refresh failed' } } : { ...initial, active: false };
  view.rerender(<><AnalyticalSaveStatus /><MpsWorkspace {...changed} /></>);
  expect(screen.queryByRole('button', { name: 'Save again' })).not.toBeInTheDocument();
  await waitFor(() => expect(host.invoke).toHaveBeenCalledWith('sop_discard_generation', { epoch: 1 }));
  expect(generations()).toHaveLength(1);
});
it('BOM scope matches all active filters while repeated/gapped rows remain in original order', async () => {
  render(<><AnalyticalSaveStatus /><BomPanel assignmentId="workspace" snapshotId="snapshot" parentPart="PARENT" bom={bom} isLoading={false} error={null} onRetry={vi.fn()} onSelectComponent={vi.fn()} /></>);
  fireEvent.change(screen.getByLabelText('Filter by Component Item'), { target: { value: ' repeat ' } });
  fireEvent.change(screen.getByLabelText('Filter by Description'), { target: { value: 'pcb' } });
  fireEvent.change(screen.getByLabelText('P/M'), { target: { value: 'P' } }); fireEvent.change(screen.getByLabelText('Phantom'), { target: { value: 'yes' } });
  expect(screen.getByText('Export includes 2 of 3 loaded rows · PARENT · Current filters')).toBeInTheDocument();
  expect(screen.getByLabelText('BOM export').closest('.bom-panel__header')).not.toBeNull();
  await activate('BOM'); expect(generations()[0][1].analyticalRequest).toMatchObject({ kind: 'BOM', parent: 'PARENT', componentFilter: ' repeat ', descriptionFilter: 'pcb', pmFilter: 'P', phantomFilter: 'yes', isStale: true, warning: 'Loaded last-known BOM', businessDate: '2026-10-15', bomEffectiveDate: '2026-10-15', bomLoadedAtUtc: bom.loadedAtUtc });
  const table = document.querySelector('.bom-panel table')!; expect(within(table as HTMLElement).getAllByText('REPEAT')).toHaveLength(2);
  fireEvent.change(screen.getByLabelText('Filter by Description'), { target: { value: 'no match' } });
  expect(screen.getByText('Export includes 0 of 3 loaded rows · PARENT · Current filters')).toBeInTheDocument(); expect(screen.queryByRole('button', { name: 'Save again' })).not.toBeInTheDocument();
});
it('a stale BOM business date is unavailable and cannot be bypassed by the menu', () => {
  render(<BomPanel assignmentId="workspace" snapshotId="snapshot" parentPart="PARENT" bom={{ ...bom, effectiveDate: '2026-10-14' }} isLoading={false} error={null} onRetry={vi.fn()} onSelectComponent={vi.fn()} />);
  expect(screen.getByText(/BOM business date changed/)).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled(); expect(generations()).toHaveLength(0);
});

import { describe, expect, it, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import type { LongTermShortageRow, LongTermShortagesResponse } from '../api/longTermShortagesApi';
import { exportLongTermShortages, fetchLongTermShortages } from '../api/longTermShortagesApi';
import { saveLongTermShortagesWorkbook } from '../longTermShortages/saveLongTermShortagesWorkbook';
import { LongTermShortagesPanel } from './LongTermShortagesPanel';

const fetchMock = vi.fn();
const exportMock = vi.fn();
const saveWorkbookMock = vi.fn();
vi.mock('../api/longTermShortagesApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/longTermShortagesApi')>()),
  fetchLongTermShortages: (...args: Parameters<typeof fetchLongTermShortages>) => fetchMock(...args),
  exportLongTermShortages: (...args: Parameters<typeof exportLongTermShortages>) => exportMock(...args),
}));
vi.mock('../longTermShortages/saveLongTermShortagesWorkbook', () => ({
  saveLongTermShortagesWorkbook: (...args: Parameters<typeof saveLongTermShortagesWorkbook>) => saveWorkbookMock(...args),
}));

function row(overrides: Partial<LongTermShortageRow> = {}): LongTermShortageRow {
  return {
    componentPart: 'CRITICAL', unitOfMeasure: 'EA', qadStatus: 'P', description: 'Bolt', isKss: false, leadTimeWeeks: 2,
    planner: 'Ann', openingQoh: 12, displayOpeningQoh: 12, safetyStockState: 'Resolved', safetyStock: 5, displaySafetyStock: 5,
    severity: 'CriticalShort', firstSafetyStockShortWeek: 1, firstCriticalShortWeek: 1,
    demandParentParts: [], otherProgramParentParts: [], purchaseOrders: [], buyerPlannerCode: null,
    weeks: Array.from({ length: 24 }, (_, index) => ({ weekNumber: index + 1, weekStart: '2026-09-13', workOrderDemand: 5, displayWorkOrderDemand: 5, forecastDemand: 0, displayForecastDemand: 0, displayDemand: 5, purchaseOrderSupply: 0, displayPurchaseOrderSupply: 0, balance: index === 0 ? -2 : 12, displayBalance: index === 0 ? -2 : 12, severity: index === 0 ? 'CriticalShort' : 'Clear' })),
    ...overrides,
  };
}

function response(rows: LongTermShortageRow[], isStale = false): LongTermShortagesResponse {
  return { snapshotId: 'snap-1', refreshDate: '2026-09-15', isStale, warning: isStale ? 'QAD unavailable; retained result shown.' : null, rows };
}

beforeEach(() => { fetchMock.mockReset(); exportMock.mockReset(); saveWorkbookMock.mockReset(); });

describe('LongTermShortagesPanel', () => {
  it('does not fetch before MPS is loaded', () => {
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId={null} />);
    expect(screen.getByText(/Load the MPS dashboard/)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('shows default shortage rows, Show All clear rows, filters, stale status, and accessible severity labels', async () => {
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'CLEAR', qadStatus: null, planner: 'Bob', severity: 'Clear', firstSafetyStockShortWeek: null, weeks: [{ weekNumber: 1, weekStart: '2026-09-13', workOrderDemand: 0, displayWorkOrderDemand: 0, forecastDemand: 0, displayForecastDemand: 0, displayDemand: 0, purchaseOrderSupply: 0, displayPurchaseOrderSupply: 0, balance: 12, displayBalance: 12, severity: 'Clear' }] })], true));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });
    const metadataTable = screen.getByRole('table', { name: 'Workspace Shortages component metadata' });
    const weeksTable = screen.getByRole('table', { name: 'Workspace Shortages weekly balances' });
    expect(screen.getByRole('heading', { name: 'Workspace Shortages' })).toBeInTheDocument();
    expect(within(weeksTable).getByRole('columnheader', { name: /^Week 24\b/i })).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent(/retained result/);
    expect(within(metadataTable).queryByText('CLEAR')).not.toBeInTheDocument();
    const critical = screen.getByText('CRITICAL');
    expect(critical.parentElement?.className).toContain('long-term-shortages__comp--critical');
    expect(critical.parentElement).toHaveAccessibleName(/CriticalShort/);
    expect(within(weeksTable).getAllByRole('row')[1].querySelectorAll('td')[0].className).toContain('long-term-shortages__balance--negative');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    expect(within(metadataTable).getByText('CLEAR')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Planner'), { target: { value: 'Bob' } });
    expect(within(metadataTable).queryByText('CRITICAL')).not.toBeInTheDocument();
    expect(within(metadataTable).getByText('CLEAR')).toBeInTheDocument();
  });

  it('keeps metadata separate and aligned while the weekly pane scrolls through all 24 weeks', async () => {
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'SECOND' })]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);

    const metadataTable = await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });
    const weekPane = screen.getByTestId('workspace-shortages-week-scroll');
    const weeksTable = screen.getByRole('table', { name: 'Workspace Shortages weekly balances' });
    expect(within(metadataTable).getAllByRole('row')).toHaveLength(within(weeksTable).getAllByRole('row').length);
    expect(within(metadataTable).getByRole('columnheader', { name: 'On Hand' })).toBeInTheDocument();
    expect(within(weeksTable).getByRole('columnheader', { name: /^Week 1\b/i })).toBeInTheDocument();
    expect(within(weeksTable).getByRole('columnheader', { name: /^Week 24\b/i })).toBeInTheDocument();

    fireEvent.scroll(weekPane, { target: { scrollLeft: 960 } });
    expect(weekPane.scrollLeft).toBe(960);
    expect(within(metadataTable).getByRole('button', { name: 'CRITICAL' })).toBeInTheDocument();
  });

  it('defaults population options off and reloads when either option changes', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);

    await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });
    const manufactured = screen.getByRole('checkbox', { name: 'Include Manufactured Parts' });
    const phantoms = screen.getByRole('checkbox', { name: 'Include Phantoms' });
    expect(manufactured).not.toBeChecked();
    expect(phantoms).not.toBeChecked();
    expect(fetchMock).toHaveBeenCalledWith('ws-1', 'snap-1', {
      includeManufacturedParts: false,
      includePhantoms: false,
    });

    fireEvent.click(manufactured);
    await waitFor(() => expect(fetchMock).toHaveBeenLastCalledWith('ws-1', 'snap-1', {
      includeManufacturedParts: true,
      includePhantoms: false,
    }));

    await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Phantoms' }));
    await waitFor(() => expect(fetchMock).toHaveBeenLastCalledWith('ws-1', 'snap-1', {
      includeManufacturedParts: true,
      includePhantoms: true,
    }));
  });

  it('opens the component detail and exports precisely the filtered rows', async () => {
    exportMock.mockResolvedValue({ blob: new Blob(['workbook']), fileName: 'Workspace-Shortages-2026-09-15.xlsx' });
    saveWorkbookMock.mockResolvedValue({ kind: 'saved', fileName: 'Workspace-Shortages-2026-09-15.xlsx' });
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'OTHER', planner: 'Bob' })]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });
    fireEvent.change(screen.getByLabelText('Comp'), { target: { value: 'critical' } });
    fireEvent.click(screen.getByRole('button', { name: 'Export displayed rows' }));
    await waitFor(() => expect(exportMock).toHaveBeenCalledWith('ws-1', 'snap-1', ['CRITICAL'], {
      includeManufacturedParts: false,
      includePhantoms: false,
    }));
    await waitFor(() => expect(saveWorkbookMock).toHaveBeenCalledWith(expect.any(Blob), 'Workspace-Shortages-2026-09-15.xlsx'));
    expect(await screen.findByRole('status')).toHaveTextContent(/saved Workspace-Shortages/i);
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    expect(screen.getByRole('dialog', { name: 'Workspace Shortages Component Information' })).toBeInTheDocument();
    expect(screen.getByText(/Purchase orders are context only/)).toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('reports a failed native write and restores the export button', async () => {
    exportMock.mockResolvedValue({ blob: new Blob(['workbook']), fileName: 'Workspace-Shortages-2026-09-15.xlsx' });
    saveWorkbookMock.mockRejectedValue(new Error('write failed'));
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });

    fireEvent.click(screen.getByRole('button', { name: 'Export displayed rows' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/workbook could not be prepared/i);
    expect(screen.getByRole('button', { name: 'Export displayed rows' })).toBeEnabled();
  });

  it('leaves the loaded report unchanged when native Save As is cancelled', async () => {
    exportMock.mockResolvedValue({ blob: new Blob(['workbook']), fileName: 'Workspace-Shortages-2026-09-15.xlsx' });
    saveWorkbookMock.mockResolvedValue({ kind: 'cancelled' });
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });

    fireEvent.click(screen.getByRole('button', { name: 'Export displayed rows' }));

    await waitFor(() => expect(saveWorkbookMock).toHaveBeenCalledOnce());
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
    expect(screen.getByRole('table', { name: 'Workspace Shortages component metadata' })).toBeInTheDocument();
  });

  it('labels selected-site null safety stock as unresolved rather than clear', async () => {
    fetchMock.mockResolvedValue(response([row({
      componentPart: 'UNRESOLVED',
      severity: 'SafetyStockUnavailable',
      safetyStockState: 'SelectedSiteValueMissing',
      safetyStock: null,
    })]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);

    const component = await screen.findByRole('button', { name: 'UNRESOLVED' });
    expect(component.parentElement).toHaveAccessibleName(/Safety stock unresolved/);
  });

  it('retains raw-negative styling and exposes raw and rounded EA balance when display rounds to zero', async () => {
    fetchMock.mockResolvedValue(response([row({ weeks: [{ weekNumber: 1, weekStart: '2026-09-13', workOrderDemand: 0.4, displayWorkOrderDemand: 0, forecastDemand: 0, displayForecastDemand: 0, displayDemand: 0, purchaseOrderSupply: 0, displayPurchaseOrderSupply: 0, balance: -0.4, displayBalance: 0, severity: 'CriticalShort' }] })]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);

    const cell = (await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' })).querySelectorAll('tbody td')[0];
    expect(cell).toHaveTextContent('0');
    expect(cell.className).toContain('long-term-shortages__balance--negative');
    expect(cell).toHaveAccessibleName(/raw calculated balance -0.4, displayed balance 0/);
  });
});

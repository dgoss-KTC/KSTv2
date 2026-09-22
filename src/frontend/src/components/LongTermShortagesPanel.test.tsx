import { beforeEach, describe, expect, it, vi } from 'vitest';
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

function bucket(weekNumber: number | null, projectedQoh: number | string, severity = 'Clear') {
  return { weekNumber, weekStart: weekNumber === null ? null : `2026-10-${String(weekNumber + 4).padStart(2, '0')}`, grossRequirements: '98.2456140337', scheduledReceipts: 0, plannedOrdersDue: 0, plannedOrdersRelease: weekNumber === 1 ? '49.125' : 0, projectedQoh, severity };
}

function row(overrides: Partial<LongTermShortageRow> = {}): LongTermShortageRow {
  return {
    componentPart: 'CRITICAL', unitOfMeasure: 'EA', qadStatus: 'P', description: 'Bolt', planner: 'Ann', buyerPlannerCode: 'BUY', openingQoh: '12.125', safetyStockState: 'Resolved', safetyStock: 5,
    severity: 'CriticalShort', firstShortDate: '2026-10-05', demandParentParts: ['PARENT-1'], past: bucket(null, '12.125'),
    weeks: Array.from({ length: 24 }, (_, index) => bucket(index + 1, index === 0 ? '-2.4' : '12.125', index === 0 ? 'CriticalShort' : 'Clear')),
    evidence: [{ evidenceOrdinal: 1, type: 'SUPPLYP', dueDate: '2026-10-05', releaseDate: '2026-09-28', quantity: '49.125', category: 'PlannedOrdersRelease' }],
    presentation: { manufacturerItem: 'MFG-1', poNumber: 'PO-1', poLine: 2, poDueDate: '2026-10-06', poOpenQuantity: '49.125', poConfirmed: true, isKss: true }, ...overrides,
  };
}

function response(rows: LongTermShortageRow[], isStale = false): LongTermShortagesResponse {
  return { snapshotId: 'snap-1', refreshDate: '2026-10-05', isStale, warning: isStale ? 'QAD unavailable; retained result shown.' : null, rows };
}

beforeEach(() => { fetchMock.mockReset(); exportMock.mockReset(); saveWorkbookMock.mockReset(); });

describe('LongTermShortagesPanel', () => {
  it('does not fetch before MPS is loaded', () => {
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId={null} />);
    expect(screen.getByText(/Load the MPS dashboard/)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('renders a 24 Monday-week grid, defaults to shortages, filters locally, and exposes stale status', async () => {
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'CLEAR', qadStatus: null, planner: 'Bob', severity: 'Clear', firstShortDate: null, weeks: [bucket(1, '12.125')] })], true));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const metadata = await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });
    const weeks = screen.getByRole('table', { name: 'Workspace Shortages weekly balances' });
    expect(within(weeks).getByRole('columnheader', { name: /Week 24/i })).toBeInTheDocument();
    expect(within(metadata).queryByText('CLEAR')).not.toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent(/retained result/);
    const firstWeekCell = weeks.querySelector('tbody td');
    expect(firstWeekCell).toHaveTextContent('-2.4');
    expect(firstWeekCell?.className).toContain('long-term-shortages__balance--negative');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    fireEvent.change(screen.getByLabelText('Planner'), { target: { value: 'Bob' } });
    expect(within(metadata).getByText('CLEAR')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'KSS' }));
    expect(within(metadata).getByText('CLEAR')).toBeInTheDocument();
  });

  it('defaults population options off and reloads when either option changes', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Manufactured Parts' }));
    await waitFor(() => expect(fetchMock).toHaveBeenLastCalledWith('ws-1', 'snap-1', { includeManufacturedParts: true, includePhantoms: false }));
  });

  it('opens accessible Past, planned-release, and raw-evidence detail then exports only filtered rows', async () => {
    exportMock.mockResolvedValue({ blob: new Blob(['workbook']), fileName: 'Workspace-Shortages-2026-10-05.xlsx' });
    saveWorkbookMock.mockResolvedValue({ kind: 'saved', fileName: 'Workspace-Shortages-2026-10-05.xlsx' });
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'OTHER', planner: 'Bob' })]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages component metadata' });
    fireEvent.change(screen.getByLabelText('Comp'), { target: { value: 'critical' } });
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    const dialog = screen.getByRole('dialog', { name: 'Workspace Shortages Component Information' });
    expect(within(dialog).getByRole('heading', { name: 'Past' })).toBeInTheDocument();
    expect(within(dialog).getAllByText('Planned Orders Release')).toHaveLength(2);
    expect(within(dialog).getByRole('heading', { name: 'Raw MRP Evidence' })).toBeInTheDocument();
    expect(within(dialog).getByRole('heading', { name: 'PO/KSS Context' })).toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Export displayed rows' }));
    await waitFor(() => expect(exportMock).toHaveBeenCalledWith('ws-1', 'snap-1', ['CRITICAL'], { includeManufacturedParts: false, includePhantoms: false }));
  });
});

import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, within } from '@testing-library/react';
import type { WorkOrderImmediateMaterialAnalysisResponseDto, WorkOrderImmediateMaterialComponentDto } from '../api/client';
import type { WorkOrdersApiError } from '../api/workOrdersApi';
import { ShortagesPanel } from './ShortagesPanel';

function makeAnalysis(overrides: Partial<WorkOrderImmediateMaterialAnalysisResponseDto> = {}): WorkOrderImmediateMaterialAnalysisResponseDto {
  return {
    snapshotId: 'snap-1', diagnostic: null,
    workOrder: { woid: 'WO-1', buildPart: 'PARENT', status: 'released', workOrderType: null, materialBuildQuantity: 10, dueDate: '2026-09-10', releaseDate: '2026-09-01', planningBucketContext: 'forwardDue' },
    components: [], ...overrides,
  };
}

function makeRow(overrides: Partial<WorkOrderImmediateMaterialComponentDto> = {}): WorkOrderImmediateMaterialComponentDto {
  return { componentPart: 'COMP', description: 'Component', isManufactured: false, unitOfMeasure: 'EA', requirementSource: 'actualWo', materialStatus: 'short', allocationMode: 'committedSequential', requiredQuantity: 10, issuedQuantity: 2, varianceQuantity: -8, issuedPercent: 20, remainingRequirement: 8, usableHardAllocationToThisWoComponent: 0, ownHardCoverage: 0, uncoveredRequirement: 8, availableQuantityAtEvaluation: 0, allocatedQuantity: 0, usableOnHand: 0, shortQuantity: 8, isFloorStockOrNonIssued: false, isOverIssued: false, inventoryActivity: { transit: 1, inspection: 2, nonNet: 3, mrb: 4, ncmInspection: 5, expiredExpiring: 6 }, incoming: null, diagnostic: null, ...overrides };
}

describe('ShortagesPanel', () => {
  it('renders Actual issue facts, signed over-issue, and Projected N/A without calculating them', () => {
    render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow({ componentPart: 'ACTUAL', issuedQuantity: 15, varianceQuantity: 5, issuedPercent: 150, shortQuantity: 0, materialStatus: 'onHand', isOverIssued: true }), makeRow({ componentPart: 'PROJECTED', requirementSource: 'projectedBom', issuedQuantity: null, varianceQuantity: null, issuedPercent: null, shortQuantity: 3 })] })} isLoading={false} error={null} onRetry={vi.fn()} detail={null} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);
    const rows = screen.getAllByRole('row');
    expect(within(rows[2]).getByText('15')).toBeInTheDocument();
    expect(within(rows[2]).getByText('5')).toBeInTheDocument();
    expect(within(rows[2]).getByText('150%')).toBeInTheDocument();
    expect(within(rows[1]).getAllByText('N/A')).toHaveLength(3);
  });

  it('orders Unknown, Short, and On Hand by API status rather than shortage quantity', () => {
    render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow({ componentPart: 'ON-HAND', materialStatus: 'onHand', shortQuantity: 0 }), makeRow({ componentPart: 'SMALL-SHORT', shortQuantity: 1 }), makeRow({ componentPart: 'UNKNOWN', materialStatus: 'unknown', shortQuantity: null }), makeRow({ componentPart: 'LARGE-SHORT', shortQuantity: 99 })] })} isLoading={false} error={null} onRetry={vi.fn()} detail={null} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);
    const rows = screen.getAllByRole('row').slice(1);
    expect(rows.map((row) => within(row).getByRole('button').textContent)).toEqual(['UNKNOWN', 'LARGE-SHORT', 'SMALL-SHORT', 'ON-HAND']);
    expect(within(rows[0]).getByText('N/A')).toBeInTheDocument();
    expect(within(rows[3]).getByText('0')).toBeInTheDocument();
  });

  it('renders the exact immediate-material grid headers', () => {
    render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow()] })} isLoading={false} error={null} onRetry={vi.fn()} detail={null} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);

    expect(screen.getAllByRole('columnheader').map((header) => header.textContent)).toEqual([
      'Component', 'Description', 'BOM Qty', 'Issued Qty', 'Variance Qty', 'Issued %', 'Short',
    ]);
  });

  it('shows the successful empty-components message without an alert', () => {
    render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis()} isLoading={false} error={null} onRetry={vi.fn()} detail={null} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);

    expect(screen.getByText('No immediate material components were returned for this Work Order.')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it.each<[WorkOrdersApiError, string]>([
    [{ type: 'error', detail: 'Material service is unavailable.' }, 'Material service is unavailable.'],
    [{ type: 'stale', detail: 'Ignored stale detail.' }, 'This schedule context is out of date. Refresh the MPS grid and reselect the Work Order.'],
  ] as const)('shows %s errors with Retry', (error, message) => {
    const retry = vi.fn();
    render(<ShortagesPanel selectedWoid="WO-1" analysis={null} isLoading={false} error={error} onRetry={retry} detail={null} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);

    expect(screen.getByText(message)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(retry).toHaveBeenCalledOnce();
  });

  it('shows provenance, activity, floor stock, PO context, and closes detail with Escape', () => {
    const close = vi.fn();
    render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow({ isFloorStockOrNonIssued: true, incoming: { isKss: false, poState: 'open', poNumber: 'PO-2', poDueDate: '2026-09-12', poOpenQuantity: 7, poConfirmed: true, trackingInfo: 'TRACK' } })] })} isLoading={false} error={null} onRetry={vi.fn()} detail={makeRow({ isFloorStockOrNonIssued: true, incoming: { isKss: false, poState: 'open', poNumber: 'PO-2', poDueDate: '2026-09-12', poOpenQuantity: 7, poConfirmed: true, trackingInfo: 'TRACK' } })} onSelectDetail={vi.fn()} onCloseDetail={close} />);
    expect(screen.getByText('Actual WO Requirement')).toBeInTheDocument();
    expect(screen.getByText('Floor Stock / Non-Issued')).toBeInTheDocument();
    expect(screen.getByText('Transit')).toBeInTheDocument();
    expect(screen.getByText('PO-2')).toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(close).toHaveBeenCalledOnce();
  });

  it('keeps KSS distinct from NO PO and only shows NO PO for a Short non-KSS row', () => {
    const { rerender } = render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow({ incoming: { isKss: true, poState: null, poNumber: null, poDueDate: null, poOpenQuantity: null, poConfirmed: null, trackingInfo: null } })] })} isLoading={false} error={null} onRetry={vi.fn()} detail={makeRow({ incoming: { isKss: true, poState: null, poNumber: null, poDueDate: null, poOpenQuantity: null, poConfirmed: null, trackingInfo: null } })} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);
    expect(screen.getByText('KSS')).toBeInTheDocument(); expect(screen.queryByText('NO PO')).not.toBeInTheDocument();
    const noPo = { isKss: false, poState: 'NO PO', poNumber: null, poDueDate: null, poOpenQuantity: null, poConfirmed: null, trackingInfo: null };
    rerender(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow({ incoming: noPo })] })} isLoading={false} error={null} onRetry={vi.fn()} detail={makeRow({ incoming: noPo })} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);
    expect(screen.getByText('NO PO')).toBeInTheDocument();
  });

  it('retains conventional PO details for an exceptional KSS purchase', () => {
    const incoming = { isKss: true, poState: 'open', poNumber: 'PO-KSS', poDueDate: '2026-09-12', poOpenQuantity: 7, poConfirmed: true, trackingInfo: 'TRACK' };
    render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow({ incoming })] })} isLoading={false} error={null} onRetry={vi.fn()} detail={makeRow({ incoming })} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);

    expect(screen.getByText('KSS')).toBeInTheDocument();
    expect(screen.getByText('PO-KSS')).toBeInTheDocument();
  });

  it('does not fabricate incoming context for On Hand or Unknown rows', () => {
    const { rerender } = render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow({ materialStatus: 'onHand', shortQuantity: 0, incoming: null })] })} isLoading={false} error={null} onRetry={vi.fn()} detail={makeRow({ materialStatus: 'onHand', shortQuantity: 0, incoming: null })} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);
    expect(screen.queryByText('NO PO')).not.toBeInTheDocument();
    expect(screen.queryByText('Incoming')).not.toBeInTheDocument();
    rerender(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow({ materialStatus: 'unknown', shortQuantity: null, incoming: null })] })} isLoading={false} error={null} onRetry={vi.fn()} detail={makeRow({ materialStatus: 'unknown', shortQuantity: null, incoming: null })} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);
    expect(screen.queryByText('NO PO')).not.toBeInTheDocument();
    expect(screen.queryByText('Incoming')).not.toBeInTheDocument();
  });

  it('shows a manufactured component as a normal non-diagnostic Not Applicable row', () => {
    render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ components: [makeRow({ componentPart: 'SUBASSY', isManufactured: true, materialStatus: 'notApplicable', shortQuantity: null, diagnostic: null })] })} isLoading={false} error={null} onRetry={vi.fn()} detail={null} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Work Orders for SUBASSY' })).toBeInTheDocument();
    expect(screen.queryByText(/do not participate in Stage 9/i)).not.toBeInTheDocument();
  });

  it('renders an analysis diagnostic without a fabricated component row', () => {
    render(<ShortagesPanel selectedWoid="WO-1" analysis={makeAnalysis({ diagnostic: 'Requirement source unavailable.' })} isLoading={false} error={null} onRetry={vi.fn()} detail={null} onSelectDetail={vi.fn()} onCloseDetail={vi.fn()} />);
    expect(screen.getByRole('alert')).toHaveTextContent('Requirement source unavailable.');
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });
});

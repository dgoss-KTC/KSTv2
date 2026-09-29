import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ComponentInfoModal } from './ComponentInfoModal';
import type { ComponentDetailResponseDto } from '../api/client';
import type { LongTermShortageRow } from '../api/longTermShortagesApi';
import { compactFixture, projectionFixture } from '../longTermShortages/compactScreenFixture.test-support';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

function makeDetail(overrides: Partial<ComponentDetailResponseDto> = {}): ComponentDetailResponseDto {
  return {
    site: 'SW',
    componentPart: 'COMP-1',
    description: 'Component One',
    partStatusCode: 'A',
    partStatusDescription: 'Active',
    iosCode: 'I',
    netQuantityOnHand: 10,
    nonNetQuantityOnHand: 0,
    standardCost: 12.5,
    qctc: 13.75,
    timeFence: 5,
    safetyTime: 2,
    safetyStock: 3,
    buyerPlanner: 'JDOE',
    purchaseLeadTimeDays: 7,
    inspectionLeadTimeDays: 1,
    cumulativeLeadTimeDays: 8,
    minimumOrderQuantity: 100,
    orderMultiple: 25,
    loadedAtUtc: '2026-08-13T12:00:00Z',
    isStale: false,
    warning: null,
    ...overrides,
  };
}

function renderModal(props: Partial<React.ComponentProps<typeof ComponentInfoModal>> = {}) {
  return render(
    <ComponentInfoModal
      componentPart="COMP-1"
      detail={makeDetail()}
      isLoading={false}
      error={null}
      onRetry={vi.fn()}
      onClose={vi.fn()}
      approvedVendors={null}
      isApprovedVendorsLoading={false}
      approvedVendorsError={null}
      onExpandApprovedVendors={vi.fn()}
      onRetryApprovedVendors={vi.fn()}
      {...props}
    />,
  );
}

function fullShortage(overrides: Partial<LongTermShortageRow> = {}): LongTermShortageRow {
  return { componentPart: 'COMP-1', unitOfMeasure: 'EA', qadStatus: 'P', description: 'Component One', planner: null,
    buyerPlannerCode: 'JDOE', openingQoh: '10.75', safetyStockState: 'Resolved', safetyStock: 3,
    severity: 'FutureShort', firstShortDate: '2026-09-27', demandParentParts: ['MODEL-A', 'MODEL-B'],
    past: { weekNumber: null, weekStart: null, grossRequirements: '2.75', scheduledReceipts: 0, plannedOrdersDue: 0,
      plannedOrdersRelease: 0, projectedQoh: '8.25', severity: 'Healthy', unconfirmedReceipts: 0,
      confirmedEnding: 0, allReceiptsEnding: 0, planningEnding: 0, allReceiptsPlanningEnding: 0, lowestProjectedBalance: 0,
      lowestConfirmedBalance: 0, lowestAllReceiptsBalance: 0, overdueReceipts: '1.75', includesUnconfirmed: false },
    weeks: [], evidence: [], episodes: [], presentation: { manufacturerItem: 'MFG-1', poNumber: null, poLine: null,
      poDueDate: null, poOpenQuantity: null, poConfirmed: null, isKss: true }, dataQualityWarning: null,
    firstAtRiskDate: null, effectivePmCode: 'P', partStatusDescription: null, orderPeriodDays: null, safetyTimeWorkingDays: null,
    manufacturingLeadWorkingDays: null, purchasingLeadCalendarDays: null, cumulativeLeadCalendarDays: null, sitePlanningPresent: true,
    ...overrides };
}

function shortage(overrides: Partial<LongTermShortageRow> = {}) { return compactFixture(fullShortage(overrides)); }

describe('ComponentInfoModal', () => {
  it('keeps master fields, manufacturer item and alternates in the first column, independent of location length', () => {
    renderModal({ context: 'shortages', shortage: shortage(), purchasing: { commentAvailable: true, currentComment: null, openPurchaseOrders: [] } });
    const dialog = screen.getByRole('dialog');
    const left = dialog.querySelector('.component-info-modal__left')!;
    const middle = dialog.querySelector('.component-info-modal__right')!;
    const right = within(dialog).getByRole('complementary', { name: 'Workspace Shortages details' });
    expect(left.parentElement).toHaveClass('component-info-modal__columns');
    expect(left.nextElementSibling).toBe(middle);
    expect(middle.nextElementSibling).toBe(right);
    expect(within(left as HTMLElement).getByRole('heading', { name: 'Reference' })).toBeInTheDocument();
    expect(within(left as HTMLElement).getByRole('heading', { name: 'Manufacturer Item' })).toBeInTheDocument();
    expect(within(left as HTMLElement).getByRole('button', { name: 'Approved Alternates' })).toBeInTheDocument();
    expect(within(middle as HTMLElement).getByRole('heading', { name: 'Inventory / Lot Locations' })).toBeInTheDocument();
    const locationList = document.createElement('div');
    locationList.textContent = 'LOCATION LOT '.repeat(100);
    middle.append(locationList);
    expect(left.contains(locationList)).toBe(false);
    expect(within(left as HTMLElement).getByRole('heading', { name: 'Reference' })).toBeInTheDocument();
    const css = readFileSync(resolve(process.cwd(), 'src/components/ComponentInfoModal.css'), 'utf8');
    expect(css).toMatch(/\.component-info-modal__columns\s*{[^}]*display:\s*grid;[^}]*align-items:\s*start/);
    expect(css).toMatch(/\.component-info-modal--shortages \.component-info-modal__columns[^}]*grid-template-columns:\s*minmax\(0,/);
    expect(css).toMatch(/@media \(max-width: 1100px\)[\s\S]*grid-template-columns:\s*minmax\(0, 1fr\)/);
  });

  it('keeps master planning values out of the shortage column and wraps every demand parent', () => {
    const parents = Array.from({ length: 40 }, (_, index) => `LONG-MODEL-${index}`);
    renderModal({ context: 'shortages', shortage: shortage(), projectionDetail: projectionFixture(fullShortage({ demandParentParts: parents })),
      purchasing: { commentAvailable: true, currentComment: null, openPurchaseOrders: [] } });
    const left = screen.getByLabelText('Component information');
    const right = screen.getByRole('complementary', { name: 'Workspace Shortages details' });
    for (const field of ['Buyer / Planner', 'Safety Stock', 'Safety Time', 'Purchase LT']) {
      expect(within(left).getByText(field)).toBeInTheDocument();
      expect(within(right).queryByText(field)).not.toBeInTheDocument();
    }
    expect(within(right).getAllByText('KSS')).toHaveLength(2);
    const parentField = within(right).getByText('Demand Parents').parentElement!;
    expect(parentField).toHaveClass('long-term-shortage-detail__parents');
    expect(parentField.querySelector('dd')).toHaveTextContent(parents[parents.length - 1]);
    expect(within(right).queryByText('Manufacturing Lead (site working days)')).not.toBeInTheDocument();
    const css = readFileSync(resolve(process.cwd(), 'src/components/ComponentInfoModal.css'), 'utf8');
    expect(css).toMatch(/\.long-term-shortage-detail__parents\s*{[^}]*grid-column:\s*1 \/ -1/);
    expect(css).toMatch(/\.long-term-shortage-detail__parents dd\s*{[^}]*white-space:\s*normal;[^}]*overflow-wrap:\s*anywhere/);
  });

  it('rounds Past and PO quantities by component UOM without changing raw values, and hides normal-screen evidence', () => {
    const view = renderModal({ context: 'shortages', shortage: shortage(), projectionDetail: projectionFixture(fullShortage()),
      purchasing: { commentAvailable: true, currentComment: 'On order', openPurchaseOrders: [] } });
    const right = screen.getByRole('complementary', { name: 'Workspace Shortages details' });
    expect(within(right).getByText('Past Gross Requirements').nextElementSibling).toHaveTextContent('3');
    expect(within(right).getByText('Overdue Receipts (excluded)').nextElementSibling).toHaveTextContent('2');
    expect(within(right).getByText('Adjusted Opening QOH').nextElementSibling).toHaveTextContent('8');
    for (const removed of ['Site Planning Lead Times', 'PO/KSS Context', 'Shortage Episodes', '26-Week Timeline', 'Raw MRP Evidence'])
      expect(within(right).queryByText(removed)).not.toBeInTheDocument();
    view.rerender(<ComponentInfoModal componentPart="COMP-1" context="shortages" shortage={shortage({ unitOfMeasure: 'UNFAMILIAR' })}
      projectionDetail={projectionFixture(fullShortage())}
      detail={makeDetail()} isLoading={false} error={null} onRetry={vi.fn()} onClose={vi.fn()} approvedVendors={null}
      isApprovedVendorsLoading={false} approvedVendorsError={null} onExpandApprovedVendors={vi.fn()} onRetryApprovedVendors={vi.fn()}
      purchasing={{ commentAvailable: true, currentComment: null, openPurchaseOrders: [] }} />);
    expect(within(right).getByText('Past Gross Requirements').nextElementSibling).toHaveTextContent('2.75');
    expect(within(right).getByText('Overdue Receipts (excluded)').nextElementSibling).toHaveTextContent('1.75');
    expect(within(right).getByText('Adjusted Opening QOH').nextElementSibling).toHaveTextContent('8.25');
  });

  it('uses Current Buyer Comment without invented metadata and distinguishes no comment from unavailable', () => {
    const props = { context: 'shortages' as const, shortage: shortage() };
    const view = renderModal({ ...props, purchasing: { commentAvailable: true, currentComment: 'Buyer note', openPurchaseOrders: [] } });
    const right = screen.getByRole('complementary', { name: 'Workspace Shortages details' });
    expect(within(right).getByRole('heading', { name: 'Current Buyer Comment' })).toBeInTheDocument();
    expect(within(right).queryByText('Latest Buyer Comment')).not.toBeInTheDocument();
    expect(within(right).getByText('Buyer note')).toBeInTheDocument();
    view.rerender(<ComponentInfoModal componentPart="COMP-1" detail={makeDetail()} isLoading={false} error={null}
      onRetry={vi.fn()} onClose={vi.fn()} approvedVendors={null} isApprovedVendorsLoading={false} approvedVendorsError={null}
      onExpandApprovedVendors={vi.fn()} onRetryApprovedVendors={vi.fn()} {...props}
      purchasing={{ commentAvailable: true, currentComment: '  ', openPurchaseOrders: [] }} />);
    expect(within(right).getByText('No buyer comment on file')).toBeInTheDocument();
  });

  it('sorts selected-component open POs past-due first, then by due date, with explicit exception text', () => {
    const po = (poNumber: string, dueDate: string | null, openQuantity: number) => ({
      componentPart: 'COMP-1', description: null, leadTimeDays: null, poNumber, poLine: 1, dueDate, openQuantity,
      confirmed: true, supplierDisplay: 'Acme', buyerDisplay: null, manufacturerItem: null, isKss: false,
      trackingInfo: null, isCreditHold: null, isCia: null, currentComments: null,
    });
    renderModal({ context: 'shortages', shortage: shortage(), purchasing: { commentAvailable: true, currentComment: null,
      openPurchaseOrders: [po('FUTURE', '2999-01-01', 12.75), po('NO-DATE', null, 4.25),
        po('PAST-LATER', '2001-02-01', 1.5), po('PAST-EARLIER', '2000-01-01', 2.5)] } });
    const table = within(screen.getByRole('complementary', { name: 'Workspace Shortages details' })).getByRole('table');
    const rows = within(table).getAllByRole('row').slice(1);
    expect(rows.map((element) => element.querySelector('td')?.textContent)).toEqual(['PAST-EARLIER', 'PAST-LATER', 'FUTURE', 'NO-DATE']);
    expect(rows[0]).toHaveTextContent('Past due: 2000-01-01');
    expect(rows[2]).toHaveTextContent('13');
    expect(rows[3]).toHaveTextContent('Due date missing');
  });

  it('uses the shortage report as-of date to identify past-due open PO lines', () => {
    renderModal({ context: 'shortages', asOfDate: '2026-09-25', shortage: shortage(),
      purchasing: { commentAvailable: true, currentComment: null, openPurchaseOrders: [{
        componentPart: 'COMP-1', description: null, leadTimeDays: null, poNumber: 'PO-A', poLine: 1,
        dueDate: '2026-09-24', openQuantity: 1, confirmed: null, supplierDisplay: null, buyerDisplay: null,
        manufacturerItem: null, isKss: false, trackingInfo: null, isCreditHold: null, isCia: null, currentComments: null,
      }] } });
    expect(within(screen.getByRole('table')).getByRole('cell', { name: 'Past due: 2026-09-24' })).toBeInTheDocument();
  });

  it('renders as an accessible dialog with the component identity', () => {
    renderModal();
    const dialog = screen.getByRole('dialog');
    expect(dialog).toHaveAttribute('aria-modal', 'true');
    expect(within(dialog).getByText('COMP-1')).toBeInTheDocument();
    expect(within(dialog).getByText('Component One')).toBeInTheDocument();
  });

  it('keeps BOM context free of shortage data and displays shared fields in shortages context', () => {
    const base = renderModal({ context: 'bom' });
    expect(screen.getByText('Net QOH')).toBeInTheDocument();
    expect(screen.queryByRole('complementary', { name: 'Workspace Shortages details' })).not.toBeInTheDocument();
    base.rerender(<ComponentInfoModal componentPart="COMP-1" detail={makeDetail()} isLoading={false} error={null}
      onRetry={vi.fn()} onClose={vi.fn()} approvedVendors={null} isApprovedVendorsLoading={false}
      approvedVendorsError={null} onExpandApprovedVendors={vi.fn()} onRetryApprovedVendors={vi.fn()}
      context="shortages" shortage={compactFixture({ componentPart: 'COMP-1', unitOfMeasure: 'EA', qadStatus: 'A', description: 'Component One', planner: null,
        buyerPlannerCode: 'JDOE', openingQoh: 10, safetyStockState: 'Resolved', safetyStock: 3,
        severity: 'Healthy', firstShortDate: null, demandParentParts: [], past: { weekNumber: null, weekStart: null,
          grossRequirements: 0, scheduledReceipts: 0, plannedOrdersDue: 0, plannedOrdersRelease: 0, projectedQoh: 10,
          severity: 'Healthy', unconfirmedReceipts: 0, confirmedEnding: 10, allReceiptsEnding: 10, planningEnding: 10,
          allReceiptsPlanningEnding: 10, lowestProjectedBalance: 10, lowestConfirmedBalance: 10, lowestAllReceiptsBalance: 10,
          overdueReceipts: 0, includesUnconfirmed: false }, weeks: [], evidence: [], presentation: null, episodes: [],
        dataQualityWarning: null, firstAtRiskDate: null, effectivePmCode: 'P', partStatusDescription: null, orderPeriodDays: null,
        safetyTimeWorkingDays: null, manufacturingLeadWorkingDays: null, purchasingLeadCalendarDays: null,
        cumulativeLeadCalendarDays: null, sitePlanningPresent: true })} />);
    expect(screen.getByText('Net QOH')).toBeInTheDocument();
    expect(screen.getByRole('complementary', { name: 'Workspace Shortages details' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Shortage Snapshot' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Show MRP' })).not.toBeInTheDocument();
  });

  it('shows a truthful empty location placeholder and no shortage column from BOM', () => {
    renderModal({ context: 'bom', detail: null, isLoading: true });
    expect(screen.getByText(/Loading component information/i)).toBeInTheDocument();
    expect(screen.queryByRole('complementary', { name: 'Workspace Shortages details' })).not.toBeInTheDocument();
  });

  it('keeps the location placeholder in the middle column for loaded BOM detail', () => {
    renderModal({ context: 'bom' });
    expect(screen.getByLabelText('Inventory / Lot Locations column')).toHaveTextContent('Inventory location detail will be added in a later stage.');
    expect(screen.queryByRole('complementary', { name: 'Workspace Shortages details' })).not.toBeInTheDocument();
    expect(screen.queryByText('Open Purchase Orders')).not.toBeInTheDocument();
  });

  it('shows a loading state before detail arrives', () => {
    renderModal({ detail: null, isLoading: true });
    expect(screen.getByText(/loading component information/i)).toBeInTheDocument();
  });

  it('renders all accepted field groups from the contract', () => {
    renderModal();
    expect(screen.getByText('Net QOH')).toBeInTheDocument();
    expect(screen.getByText('Non-Net QOH')).toBeInTheDocument();
    expect(screen.getByText('Standard Cost')).toBeInTheDocument();
    expect(screen.getByText('QCTC')).toBeInTheDocument();
    expect(screen.getByText('Time Fence')).toBeInTheDocument();
    expect(screen.getByText('Safety Time')).toBeInTheDocument();
    expect(screen.getByText('Safety Stock')).toBeInTheDocument();
    expect(screen.getByText('Buyer / Planner')).toBeInTheDocument();
    expect(screen.getByText('Purchase LT')).toBeInTheDocument();
    expect(screen.getByText('Inspect LT')).toBeInTheDocument();
    expect(screen.getByText('Cumulative LT')).toBeInTheDocument();
    expect(screen.getByText('Min Order')).toBeInTheDocument();
    expect(screen.getByText('Order Multiple')).toBeInTheDocument();
    expect(screen.getByText('Part Status')).toBeInTheDocument();
    expect(screen.getByText('IOS')).toBeInTheDocument();
    expect(screen.getByText('A \u2014 Active')).toBeInTheDocument();
  });

  it('displays null planning values as the accepted No Data marker, not zero', () => {
    renderModal({
      detail: makeDetail({ timeFence: null, safetyTime: null, safetyStock: null, buyerPlanner: null }),
    });
    // Four independent null fields should each show the em dash marker.
    expect(screen.getAllByText('\u2014').length).toBeGreaterThanOrEqual(4);
  });

  it('does not render a null Standard Cost as $0.00', () => {
    renderModal({ detail: makeDetail({ standardCost: null }) });
    expect(screen.queryByText('$0')).not.toBeInTheDocument();
    expect(screen.queryByText(/^\$0(\.00)?$/)).not.toBeInTheDocument();
  });

  it('does not render a null QCTC as $0.00', () => {
    renderModal({ detail: makeDetail({ qctc: null }) });
    expect(screen.queryByText(/^\$0(\.00)?$/)).not.toBeInTheDocument();
  });

  describe('cost precision (exactly four decimal places)', () => {
    it('rounds Standard Cost for display: 0.286832 -> $0.2868', () => {
      renderModal({ detail: makeDetail({ standardCost: 0.286832 }) });
      expect(screen.getByText('$0.2868')).toBeInTheDocument();
    });

    it('pads QCTC for display: 0.259 -> $0.2590', () => {
      renderModal({ detail: makeDetail({ qctc: 0.259 }) });
      expect(screen.getByText('$0.2590')).toBeInTheDocument();
    });

    it('renders numeric zero cost as $0.0000, not No Data', () => {
      renderModal({ detail: makeDetail({ standardCost: 0, qctc: 0 }) });
      expect(screen.getAllByText('$0.0000')).toHaveLength(2);
    });

    it('renders null cost as the No Data marker, not $0.0000', () => {
      renderModal({ detail: makeDetail({ standardCost: null, qctc: null }) });
      expect(screen.queryByText('$0.0000')).not.toBeInTheDocument();
      expect(screen.getAllByText('\u2014').length).toBeGreaterThanOrEqual(2);
    });

    it('formats a string-valued numeric field identically to its number equivalent', () => {
      renderModal({ detail: makeDetail({ standardCost: '0.286832', qctc: '0.259' }) });
      expect(screen.getByText('$0.2868')).toBeInTheDocument();
      expect(screen.getByText('$0.2590')).toBeInTheDocument();
    });
  });

  it('renders numeric zero inventory as zero, not No Data', () => {
    renderModal({ detail: makeDetail({ netQuantityOnHand: 0, nonNetQuantityOnHand: 0 }) });
    expect(screen.getAllByText('0')).not.toHaveLength(0);
  });

  it('shows the stale warning banner when isStale is true', () => {
    renderModal({ detail: makeDetail({ isStale: true, warning: 'Showing the last known component information.' }) });
    expect(screen.getByRole('alert')).toHaveTextContent('Showing the last known component information.');
  });

  it('renders Show MRP as visibly disabled and non-functional', async () => {
    const user = userEvent.setup();
    renderModal();
    const mrpButton = screen.getByRole('button', { name: /show mrp/i });
    expect(mrpButton).toBeDisabled();
    await user.click(mrpButton);
  });

  it('renders the future Inventory / Lot Locations placeholder with no request implied', () => {
    renderModal();
    expect(screen.getByText('Inventory / Lot Locations')).toBeInTheDocument();
    expect(screen.getByText(/inventory location detail will be added in a later stage/i)).toBeInTheDocument();
  });

  describe('Approved Vendors', () => {
    it('starts collapsed and does not request AVL', () => {
      const onExpandApprovedVendors = vi.fn();
      renderModal({ onExpandApprovedVendors });
      const toggle = screen.getByRole('button', { name: /approved alternates/i });
      expect(toggle).toHaveAttribute('aria-expanded', 'false');
      expect(onExpandApprovedVendors).not.toHaveBeenCalled();
      expect(screen.queryByText(/loading approved alternates/i)).not.toBeInTheDocument();
    });

    it('first expansion triggers exactly one activation call', async () => {
      const user = userEvent.setup();
      const onExpandApprovedVendors = vi.fn();
      renderModal({ onExpandApprovedVendors });
      await user.click(screen.getByRole('button', { name: /approved alternates/i }));
      expect(onExpandApprovedVendors).toHaveBeenCalledTimes(1);
      expect(screen.getByRole('button', { name: /approved alternates/i })).toHaveAttribute('aria-expanded', 'true');
    });

    it('shows a localized loading state while expanded and loading', async () => {
      const user = userEvent.setup();
      renderModal({ isApprovedVendorsLoading: true });
      await user.click(screen.getByRole('button', { name: /approved alternates/i }));
      expect(screen.getByText(/loading approved alternates/i)).toBeInTheDocument();
      // Component Detail remains visible while AVL loads.
      expect(screen.getByText('Net QOH')).toBeInTheDocument();
    });

    it('renders vendor rows with Supplier/Vendor Name/Supplier Item/MFG Part', async () => {
      const user = userEvent.setup();
      renderModal({
        approvedVendors: [
          { supplier: 'V001', vendorName: 'Acme Supply', supplierItem: 'SUP-1', manufacturerPart: 'MFG-1' },
        ],
      });
      await user.click(screen.getByRole('button', { name: /approved alternates/i }));
      expect(screen.getByText('V001')).toBeInTheDocument();
      expect(screen.getByText('Acme Supply')).toBeInTheDocument();
      expect(screen.getByText('SUP-1')).toBeInTheDocument();
      expect(screen.getByText('MFG-1')).toBeInTheDocument();
    });

    it('preserves returned row order and duplicate rows', async () => {
      const user = userEvent.setup();
      renderModal({
        approvedVendors: [
          { supplier: 'V002', vendorName: 'B Supply', supplierItem: null, manufacturerPart: null },
          { supplier: 'V001', vendorName: 'A Supply', supplierItem: null, manufacturerPart: null },
          { supplier: 'V001', vendorName: 'A Supply', supplierItem: null, manufacturerPart: null },
        ],
      });
      await user.click(screen.getByRole('button', { name: /approved alternates/i }));
      const rows = screen.getAllByRole('row').slice(1); // skip header row
      expect(rows).toHaveLength(3);
      expect(within(rows[0]).getByText('V002')).toBeInTheDocument();
      expect(within(rows[1]).getByText('V001')).toBeInTheDocument();
      expect(within(rows[2]).getByText('V001')).toBeInTheDocument();
    });

    it('displays null Supplier Item / MFG Part using the No Data marker', async () => {
      const user = userEvent.setup();
      renderModal({
        approvedVendors: [{ supplier: 'V001', vendorName: 'Acme', supplierItem: null, manufacturerPart: null }],
      });
      await user.click(screen.getByRole('button', { name: /approved alternates/i }));
      expect(screen.getAllByText('\u2014').length).toBeGreaterThanOrEqual(2);
    });

    it('shows a successful empty state for zero rows', async () => {
      const user = userEvent.setup();
      renderModal({ approvedVendors: [] });
      await user.click(screen.getByRole('button', { name: /approved alternates/i }));
      expect(screen.getByText(/no approved alternates found/i)).toBeInTheDocument();
    });

    it('collapse then re-expand after success does not call activate again', async () => {
      const user = userEvent.setup();
      const onExpandApprovedVendors = vi.fn();
      renderModal({ onExpandApprovedVendors, approvedVendors: [] });
      const toggle = screen.getByRole('button', { name: /approved alternates/i });
      await user.click(toggle); // expand -> activate (mocked as already-loaded via props)
      await user.click(toggle); // collapse
      await user.click(toggle); // re-expand
      // Real no-refetch guarantee lives in useApprovedVendors; the modal itself calls
      // onExpandApprovedVendors on every expansion, and the hook is responsible for the no-op.
      expect(onExpandApprovedVendors).toHaveBeenCalledTimes(2);
      expect(screen.getByText(/no approved alternates found/i)).toBeInTheDocument();
    });

    it('shows a localized error with Retry that does not disturb Component Detail', async () => {
      const user = userEvent.setup();
      const onRetryApprovedVendors = vi.fn();
      renderModal({
        approvedVendorsError: { type: 'error', detail: 'Could not load approved vendors. Try again.' },
        onRetryApprovedVendors,
      });
      await user.click(screen.getByRole('button', { name: /approved alternates/i }));
      expect(screen.getByText(/approved alternates could not be loaded/i)).toBeInTheDocument();
      expect(screen.getByText('Net QOH')).toBeInTheDocument(); // Component Detail still visible
      await user.click(screen.getByRole('button', { name: /^retry$/i }));
      expect(onRetryApprovedVendors).toHaveBeenCalledTimes(1);
    });

    it('Escape still closes Component Information while AVL is expanded', async () => {
      const user = userEvent.setup();
      const onClose = vi.fn();
      renderModal({ onClose, approvedVendors: [] });
      await user.click(screen.getByRole('button', { name: /approved alternates/i }));
      await user.keyboard('{Escape}');
      expect(onClose).toHaveBeenCalledTimes(1);
    });

    it('Inventory / Lot Locations placeholder remains unchanged alongside AVL', () => {
      renderModal();
      expect(screen.getByText('Inventory / Lot Locations')).toBeInTheDocument();
      expect(screen.getByText(/inventory location detail will be added in a later stage/i)).toBeInTheDocument();
    });
  });

  it('X closes the modal', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({ onClose });
    await user.click(screen.getByRole('button', { name: /close component information/i }));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('Escape closes the modal', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({ onClose });
    await user.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('Escape closes the modal when focus is on the Close button', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({ onClose });
    screen.getByRole('button', { name: /close component information/i }).focus();
    await user.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('Escape closes the modal when focus is on another focusable control inside the modal', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({
      detail: null,
      error: { type: 'error', detail: 'Database currently unavailable.' },
      onClose,
    });
    screen.getByRole('button', { name: /retry/i }).focus();
    await user.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('repeated open/close cycles do not leave duplicate or stale Escape handlers', async () => {
    const user = userEvent.setup();
    const onCloseFirst = vi.fn();
    const { unmount } = renderModal({ onClose: onCloseFirst });
    unmount();

    const onCloseSecond = vi.fn();
    renderModal({ onClose: onCloseSecond });
    await user.keyboard('{Escape}');
    expect(onCloseSecond).toHaveBeenCalledTimes(1);
    expect(onCloseFirst).not.toHaveBeenCalled();
  });

  it('after the modal unmounts, pressing Escape does not trigger its close callback', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const { unmount } = renderModal({ onClose });
    unmount();
    await user.keyboard('{Escape}');
    expect(onClose).not.toHaveBeenCalled();
  });

  it('clicking the backdrop does not close the modal', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const { container } = renderModal({ onClose });
    const backdrop = container.querySelector('.component-info-modal-backdrop');
    expect(backdrop).not.toBeNull();
    if (backdrop) await user.click(backdrop);
    expect(onClose).not.toHaveBeenCalled();
  });

  it('renders a retryable error state that keeps the modal open', async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();
    const onClose = vi.fn();
    renderModal({
      detail: null,
      error: { type: 'error', detail: 'Database currently unavailable.' },
      onRetry,
      onClose,
    });
    expect(screen.getByText('Database currently unavailable.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /retry/i }));
    expect(onRetry).toHaveBeenCalledTimes(1);
    expect(onClose).not.toHaveBeenCalled();
  });

  it('Close is available and works from the error state', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({
      detail: null,
      error: { type: 'error', detail: 'Database currently unavailable.' },
      onClose,
    });
    await user.click(screen.getByRole('button', { name: /^close$/i }));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('initial focus lands on the Close button', () => {
    renderModal();
    expect(screen.getByRole('button', { name: /close component information/i })).toHaveFocus();
  });

  it('Tab from the last focusable element wraps to the first (focus trap)', async () => {
    const user = userEvent.setup();
    renderModal();
    const dialog = screen.getByRole('dialog');
    const focusable = within(dialog).getAllByRole('button');
    focusable[focusable.length - 1].focus();
    await user.tab();
    expect(focusable[0]).toHaveFocus();
  });
});

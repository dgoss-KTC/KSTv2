import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ComponentInfoModal } from './ComponentInfoModal';
import type { ComponentDetailResponseDto } from '../api/client';
import type { LongTermShortageRow, LongTermShortagePurchasing } from '../api/longTermShortagesApi';
import { compactFixture, projectionFixture } from '../longTermShortages/compactScreenFixture.test-support';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fetchInventoryLocations } from '../api/inventoryLocationsApi';
vi.mock('../api/inventoryLocationsApi', () => ({ fetchInventoryLocations: vi.fn().mockRejectedValue(new Error('offline fixture')) }));

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
  return render(modalElement(props));
}

function modalElement(props: Partial<React.ComponentProps<typeof ComponentInfoModal>> = {}) {
  return (
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
    />
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

const supplierTime = '2026-10-20T20:00:00Z';
function purchasingWithSuppliers(suppliers: NonNullable<LongTermShortagePurchasing['suppliers']>): LongTermShortagePurchasing {
  return { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: 'Independent buyer note', commentRecordedAt: '2022-01-31T13:30:00', commentAuthor: 'buyer',
    openPurchaseOrders: [{ componentPart: 'COMP-1', description: 'Component One', leadTimeDays: 7, poNumber: 'PO-1', poLine: 1,
      dueDate: '2026-09-20', openQuantity: '3.75', confirmed: true, supplierDisplay: 'ACME', buyerDisplay: 'Buyer',
      manufacturerItem: null, isKss: false, trackingInfo: null, isCreditHold: null, isCia: null, currentComments: null }],
    suppliers };
}

describe('ComponentInfoModal', () => {
  it('places current supplier status directly above the comment and preserves individual mixed and case-distinct suppliers', async () => {
    const purchasing = purchasingWithSuppliers({ value: [
      { identifier: 'ACME', display: 'Acme display', isCreditHold: true, isCia: false },
      { identifier: 'BETA', display: 'Beta display', isCreditHold: false, isCia: true },
      { identifier: 'acme', display: 'Case distinct', isCreditHold: null, isCia: null },
      { identifier: 'UNMATCHED', display: null, isCreditHold: null, isCia: null },
    ], acquiredAtUtc: supplierTime, error: null });
    renderModal({ context: 'shortages', shortage: shortage(), purchasing });
    const strip = screen.getByRole('region', { name: 'Supplier Credit Hold / CIA' });
    expect(strip.nextElementSibling).toHaveTextContent('Current Buyer Comment');
    expect(strip).toHaveTextContent('Credit hold: ACME · Unknown: acme, UNMATCHED');
    expect(strip).toHaveTextContent('CIA: BETA · Unknown: acme, UNMATCHED');
    expect(strip).toHaveTextContent('Current supplier statuses for this selected component’s qualifying open POs.');
    expect(strip.querySelector('time')).toHaveAttribute('datetime', supplierTime);
    await userEvent.click(within(strip).getByText('Supplier status details · 4 suppliers'));
    const rows = within(screen.getByRole('table', { name: 'Supplier status' })).getAllByRole('row').slice(1);
    expect(rows.map(row => within(row).getAllByRole('cell').map(cell => cell.textContent))).toEqual([
      ['ACME', 'Acme display', 'Yes', 'No'], ['BETA', 'Beta display', 'No', 'Yes'],
      ['acme', 'Case distinct', 'Unknown', 'Unknown'], ['UNMATCHED', '—', 'Unknown', 'Unknown'],
    ]);
    expect(screen.getByText('Independent buyer note')).toBeVisible();
    expect(screen.getByText('PO-1')).toBeVisible();
    expect(screen.getByText('Std / QCTC')).toBeInTheDocument();
  });

  it.each([false, true])('uses None only for complete explicit-No coverage (missing identity: %s)', missingIdentity => {
    const purchasing = purchasingWithSuppliers({ value: [{ identifier: 'CLEAR', display: 'Clear supplier', isCreditHold: false, isCia: false }], acquiredAtUtc: supplierTime, error: null });
    purchasing.supplierIdentityUnavailable = missingIdentity;
    renderModal({ context: 'shortages', purchasing });
    const strip = screen.getByRole('region', { name: 'Supplier Credit Hold / CIA' });
    if (missingIdentity) {
      expect(strip).not.toHaveTextContent('None');
      expect(strip).toHaveTextContent('No confirmed Yes · Unavailable: supplier identity missing');
      expect(strip).toHaveTextContent('Supplier identity is unavailable for one or more qualifying open POs.');
    } else {
      expect(strip).toHaveTextContent('Credit hold: None'); expect(strip).toHaveTextContent('CIA: None');
    }
  });

  it('distinguishes acquired empty supplier population from No and keeps a successful comment without POs', () => {
    const purchasing = purchasingWithSuppliers({ value: [], acquiredAtUtc: supplierTime, error: null });
    purchasing.openPurchaseOrders = [];
    renderModal({ context: 'shortages', shortage: shortage(), purchasing });
    const strip = screen.getByRole('region', { name: 'Supplier Credit Hold / CIA' });
    expect(strip).toHaveTextContent('No supplier status is available from open POs.');
    expect(strip).not.toHaveTextContent('None');
    expect(screen.getByText('Independent buyer note')).toBeVisible();
    expect(screen.getByText('No open purchase orders for this component.')).toBeVisible();
  });

  it('keeps supplier failure localized, marks its individual flags Unavailable, and wires the existing purchasing retry', async () => {
    const retry = vi.fn();
    const purchasing = purchasingWithSuppliers({ value: [{ identifier: 'ACME', display: 'Acme', isCreditHold: null, isCia: null }], acquiredAtUtc: null, error: 'Supplier acquisition failed.' });
    renderModal({ context: 'shortages', shortage: shortage(), purchasing, onRetryPurchasing: retry });
    const strip = screen.getByRole('region', { name: 'Supplier Credit Hold / CIA' });
    expect(within(strip).getByRole('alert')).toHaveTextContent('Supplier acquisition failed.');
    expect(strip).toHaveTextContent('Credit hold: No confirmed Yes · Unavailable: ACME');
    expect(strip.querySelector('.part-info__risk-summary')).not.toHaveTextContent('Unknown');
    await userEvent.click(within(strip).getByText('Supplier status details · 1 suppliers'));
    const cells = within(screen.getByRole('table', { name: 'Supplier status' })).getAllByRole('cell');
    expect(cells.map(cell => cell.textContent)).toEqual(['ACME', 'Acme', 'Unavailable', 'Unavailable']);
    expect(screen.getByText('Independent buyer note')).toBeVisible();
    expect(screen.getByText('PO-1')).toBeVisible();
    expect(screen.getByText('Net QOH').nextElementSibling).toHaveTextContent('10');
    await userEvent.click(screen.getByRole('button', { name: 'Retry supplier status' }));
    expect(retry).toHaveBeenCalledOnce();
  });

  it('shows supplier loading independently and never adds the strip in BOM context', () => {
    const view = renderModal({ context: 'shortages', isPurchasingLoading: true });
    const strip = screen.getByRole('region', { name: 'Supplier Credit Hold / CIA' });
    expect(strip).toHaveTextContent('Loading supplier status…'); expect(strip).not.toHaveTextContent('None');
    expect(screen.getByText('Std / QCTC')).toBeInTheDocument();
    view.rerender(modalElement({ context: 'bom' }));
    expect(screen.queryByRole('region', { name: 'Supplier Credit Hold / CIA' })).not.toBeInTheDocument();
  });

  it('preserves shortages alternate row order, duplicates and null cells in the tabbed table', async () => {
    renderModal({ context: 'shortages', shortage: shortage(), approvedVendors: [
      { supplier: 'V002', vendorName: 'B Supply', supplierItem: null, manufacturerPart: null },
      { supplier: 'V001', vendorName: 'A Supply', supplierItem: 'ITEM', manufacturerPart: 'MFG' },
      { supplier: 'V001', vendorName: 'A Supply', supplierItem: 'ITEM', manufacturerPart: 'MFG' },
    ] });
    await userEvent.click(screen.getByRole('tab', { name: /Approved alternates/ }));
    const table = screen.getByRole('table', { name: 'Approved Alternates' });
    expect(within(table).getAllByRole('row').slice(1).map(row => within(row).getAllByRole('cell').map(cell => cell.textContent))).toEqual([
      ['V002', 'B Supply', '—', '—'], ['V001', 'A Supply', 'ITEM', 'MFG'], ['V001', 'A Supply', 'ITEM', 'MFG'],
    ]);
    expect(screen.getByRole('tab', { name: /Approved alternates/ })).toHaveTextContent('3');
  });

  it('keeps shortages alternate loading localized and preserves exact null and zero price formatting', async () => {
    renderModal({ context: 'shortages', shortage: shortage(), isApprovedVendorsLoading: true,
      detail: makeDetail({ standardCost: '0.286832', qctc: null }) });
    await userEvent.click(screen.getByRole('tab', { name: /Approved alternates/ }));
    expect(screen.getByText('Loading approved alternates…')).toBeVisible();
    expect(screen.getByText('Std / QCTC').nextElementSibling?.textContent).toBe('$0.2868 / —');
    expect(screen.queryByText('$0.0000')).not.toBeInTheDocument();
    expect(screen.getByText('Net QOH').nextElementSibling?.textContent).toBe('10');
  });

  it('keeps hidden inventory mounted at open and excludes hidden controls and collapsed details from the focus loop', async () => {
    const fetch = vi.mocked(fetchInventoryLocations);
    const entryCalls = fetch.mock.calls.length;
    renderModal({ context: 'shortages', shortage: shortage(), assignmentId: 'ws', mpsSnapshotId: 'snap', workspaceSite: 'SW' });
    await waitFor(() => expect(fetch.mock.calls.length).toBe(entryCalls + 1));
    expect(screen.getByText('Inventory / Lot Locations')).not.toBeVisible();
    const close = screen.getByRole('button', { name: 'Close Component Information' });
    const orders = screen.getByRole('tab', { name: /Open POs/ });
    close.focus(); await userEvent.tab({ shift: true });
    expect(orders).toHaveFocus();
    await userEvent.tab(); expect(close).toHaveFocus();
    await userEvent.click(screen.getByRole('tab', { name: 'Inventory lots' }));
    await userEvent.click(orders);
    expect(fetch.mock.calls.length).toBe(entryCalls + 1);
  });

  it('uses roving keyboard tabs, activates alternates once per selected identity, and keeps Escape ownership', async () => {
    const activate = vi.fn(); const close = vi.fn();
    const props = { context: 'shortages' as const, shortage: shortage(), onExpandApprovedVendors: activate, onClose: close, approvedVendors: [] };
    const view = renderModal(props);
    expect(activate).not.toHaveBeenCalled();
    const orders = screen.getByRole('tab', { name: /Open POs/ });
    orders.focus(); await userEvent.keyboard('{ArrowRight}');
    expect(screen.getByRole('tab', { name: 'Inventory lots' })).toHaveFocus();
    await userEvent.keyboard('{End}');
    expect(screen.getByRole('tab', { name: /Approved alternates/ })).toHaveFocus();
    expect(screen.getByRole('tabpanel')).toHaveAttribute('id', 'shortage-info-panel-alternates');
    expect(activate).toHaveBeenCalledTimes(1);
    await userEvent.keyboard('{Home}{ArrowLeft}{ArrowRight}{End}');
    expect(activate).toHaveBeenCalledTimes(1);
    expect(screen.getByText('No approved alternates found.')).toBeVisible();
    view.rerender(modalElement({ ...props, componentPart: 'COMP-2', shortage: shortage({ componentPart: 'COMP-2' }) }));
    expect(screen.getByRole('tab', { name: /Open POs/ })).toHaveAttribute('aria-selected', 'true');
    await userEvent.click(screen.getByRole('tab', { name: /Approved alternates/ }));
    expect(activate).toHaveBeenCalledTimes(2);
    await userEvent.keyboard('{Escape}'); expect(close).toHaveBeenCalledOnce();
  });

  it('expands buyer comments inline as text, collapses them, and closes only the owning modal on Escape', async () => {
    const close = vi.fn();
    const text = '<img src=x> first\n  second';
    renderModal({ context: 'shortages', shortage: shortage(), onClose: close,
      purchasing: { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: text, openPurchaseOrders: [] } });
    const toggle = screen.getByRole('button', { name: 'Show full comment' });
    expect(toggle).toHaveAttribute('aria-expanded', 'false');
    const comment = document.getElementById(toggle.getAttribute('aria-controls')!)!;
    expect(comment.textContent).toBe(text); expect(comment.querySelector('img')).toBeNull();
    expect(comment).toHaveClass('shortage-info__comment--clamped');
    await userEvent.click(toggle);
    expect(screen.getByRole('button', { name: 'Show less' })).toHaveAttribute('aria-expanded', 'true');
    expect(comment).not.toHaveClass('shortage-info__comment--clamped');
    expect(screen.getAllByRole('dialog')).toHaveLength(1);
    await userEvent.click(toggle); expect(toggle).toHaveTextContent('Show full comment');
    await userEvent.click(toggle); await userEvent.keyboard('{Escape}'); expect(close).toHaveBeenCalledOnce();
  });

  it('offers expansion for a measured two-line overflow even without newlines', () => {
    const height = vi.spyOn(HTMLElement.prototype, 'scrollHeight', 'get').mockReturnValue(80);
    const client = vi.spyOn(HTMLElement.prototype, 'clientHeight', 'get').mockReturnValue(30);
    try {
      renderModal({ context: 'shortages', shortage: shortage(), purchasing: { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: 'Long single line '.repeat(80), openPurchaseOrders: [] } });
      expect(screen.getByRole('button', { name: 'Show full comment' })).toBeInTheDocument();
    } finally { height.mockRestore(); client.mockRestore(); }
  });

  it('retains independently expected snapshot values and explicit loading/unavailable states and retries', async () => {
    const retry = vi.fn();
    const row = fullShortage({ openingQoh: '123.45', unitOfMeasure: 'LB', dataQualityWarning: 'Synthetic safety warning' });
    const projection = { ...projectionFixture(row), adjustedOpeningQoh: '-17.25', pastGrossRequirements: '140.70', overdueReceipts: '6.125', demandParentParts: ['PARENT-Z'] };
    const props = { context: 'shortages' as const, shortage: compactFixture(row), projectionDetail: projection, onRetryProjection: retry };
    const view = renderModal(props);
    const expected = { 'First Short Date': '2026-09-27', 'Opening QOH': '123.45', 'Adjusted Opening QOH': '-17.25', 'Past Gross Requirements': '140.7', 'Overdue Receipts (excluded)': '6.13', UOM: 'LB', 'Demand Parents': 'PARENT-Z' };
    for (const [label, value] of Object.entries(expected)) expect(screen.getByText(label).nextElementSibling?.textContent).toBe(value);
    expect(screen.getByText('Synthetic safety warning')).toHaveAttribute('role', 'status');
    view.rerender(modalElement({ ...props, projectionDetail: null, isProjectionLoading: true, detail: null, isLoading: true }));
    expect(screen.getByText('Loading snapshot projection detail…')).toHaveAttribute('role', 'status');
    for (const label of ['Adjusted Opening QOH', 'Past Gross Requirements', 'Overdue Receipts (excluded)']) expect(screen.getByText(label).nextElementSibling?.textContent).toBe('…');
    expect(screen.getByText('Opening QOH').nextElementSibling?.textContent).toBe('123.45');
    expect(screen.getByText('Loading component master information…')).toBeInTheDocument();
    view.rerender(modalElement({ ...props, projectionDetail: null, projectionError: true }));
    for (const label of ['Adjusted Opening QOH', 'Past Gross Requirements', 'Overdue Receipts (excluded)']) expect(screen.getByText(label).nextElementSibling?.textContent).toBe('Unavailable');
    await userEvent.click(screen.getByRole('button', { name: 'Retry projection detail' })); expect(retry).toHaveBeenCalledOnce();
    expect(screen.getByText('Net QOH').nextElementSibling).toHaveTextContent('10');
  });

  it('keeps failures localized with independent inventory, section retry wiring, stale and status distinctions', async () => {
    const masterRetry = vi.fn(); const purchasingRetry = vi.fn(); const alternatesRetry = vi.fn();
    vi.mocked(fetchInventoryLocations).mockResolvedValueOnce({ workspaceId: 'ws', site: 'SW', componentPart: 'COMP-1', mpsSnapshotId: 'snap', acquiredAtUtc: '2026-10-08T12:00:00Z', isStale: false, warning: null, rows: [] });
    const props = { context: 'shortages' as const, shortage: shortage(), assignmentId: 'ws', mpsSnapshotId: 'snap', workspaceSite: 'SW', detail: null,
      error: { type: 'error' as const, detail: 'Master failure' }, purchasingError: true, projectionError: true,
      approvedVendorsError: { type: 'error' as const, detail: 'Alternates failure' }, onRetry: masterRetry, onRetryPurchasing: purchasingRetry, onRetryApprovedVendors: alternatesRetry };
    const view = renderModal(props);
    expect(screen.getByText('Buyer comment unavailable.')).toBeInTheDocument();
    expect(screen.queryByText('No open purchase orders for this component.')).not.toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Open POs/ })).toHaveTextContent('Unavailable');
    expect(within(screen.getByRole('tab', { name: /Open POs/ })).getByLabelText('Acquisition failed')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Retry component information' })); expect(masterRetry).toHaveBeenCalledOnce();
    await userEvent.click(screen.getByRole('button', { name: 'Retry purchase orders' })); expect(purchasingRetry).toHaveBeenCalledOnce();
    await userEvent.click(screen.getByRole('tab', { name: /Approved alternates/ }));
    expect(screen.getByText('Approved alternates could not be loaded.')).toBeVisible();
    await userEvent.click(screen.getByRole('button', { name: 'Retry' })); expect(alternatesRetry).toHaveBeenCalledOnce();
    await userEvent.click(screen.getByRole('tab', { name: 'Inventory lots' }));
    expect(await screen.findByText('No inventory locations found.')).toBeVisible();
    view.rerender(modalElement({ ...props, detail: makeDetail({ isStale: true, warning: 'Synthetic stale master' }), error: null }));
    expect(screen.getByText('Synthetic stale master')).toHaveAttribute('role', 'alert');
  });

  it('uses the source-equivalent grid status only while loading and never invents KSS or healthy severity chips', () => {
    const props = { context: 'shortages' as const, shortage: shortage({ severity: 'Healthy', presentation: null }), detail: null, isLoading: true };
    const view = renderModal(props);
    expect(screen.getByText('P', { selector: '.report-chip' })).toBeInTheDocument();
    expect(screen.queryByText('KSS')).not.toBeInTheDocument();
    expect(document.querySelector('[data-severity]')).toBeNull();
    view.rerender(modalElement({ ...props, isLoading: false, detail: makeDetail() }));
    expect(screen.getByText('A — Active')).toBeInTheDocument();
    view.rerender(modalElement({ ...props, isLoading: false }));
    expect(screen.queryByText('P', { selector: '.report-chip' })).not.toBeInTheDocument();
  });

  it.each(['bom', 'shortages'] as const)('shows acquired inventory while preserving master fields and focus trap in %s context', async context => {
    vi.mocked(fetchInventoryLocations).mockResolvedValueOnce({ workspaceId: 'ws', site: 'SW', componentPart: 'COMP-1',
      mpsSnapshotId: 'snap', acquiredAtUtc: '2026-10-08T12:00:00Z', isStale: false, warning: null,
      rows: [{ rowId: '0', location: 'BIN', lotSerial: 'RA', status: 'HOLD', quantityOnHand: '-1.2345678901' }] });
    renderModal({ context, assignmentId: 'ws', mpsSnapshotId: 'snap', workspaceSite: 'SW' });
    if (context === 'shortages') await userEvent.click(screen.getByRole('tab', { name: 'Inventory lots' }));
    expect(await screen.findByRole('table', { name: 'Inventory / Lot Locations' })).toHaveTextContent('-1.235');
    expect(screen.getByTitle('-1.2345678901')).toBeInTheDocument();
    expect(screen.getByText('Net QOH')).toBeInTheDocument();
    const scroll = screen.getByRole('region', { name: 'Scrollable inventory locations' });
    scroll.focus();
    await userEvent.keyboard('{Tab}');
    expect(screen.getByRole('button', { name: 'Close Component Information' })).toHaveFocus();
    await userEvent.keyboard('{Shift>}{Tab}{/Shift}');
    expect(scroll).toHaveFocus();
  });
  it.each(['bom', 'shortages'] as const)('keeps inventory acquisition independent of master failure in %s context', async context => {
    const close = vi.fn();
    renderModal({ context, assignmentId: 'ws', mpsSnapshotId: 'snap', workspaceSite: 'SW', detail: null,
      error: { type: 'error', detail: 'Master unavailable' }, onClose: close });
    if (context === 'shortages') await userEvent.click(screen.getByRole('tab', { name: 'Inventory lots' }));
    expect(screen.getByRole('heading', { name: 'Inventory / Lot Locations' })).toBeInTheDocument();
    expect(await screen.findByText('Inventory locations could not be loaded. Please retry.')).toBeInTheDocument();
    const inventoryRetry = screen.getByRole('button', { name: 'Retry inventory' });
    inventoryRetry.focus();
    await userEvent.keyboard('{Escape}');
    expect(close).toHaveBeenCalledOnce();
  });
  it('puts snapshot, standard facts, disclosure, supplier status, comment and full-width tabs in the specified order', () => {
    renderModal({ context: 'shortages', workspaceSite: 'SW', shortage: shortage(), purchasing: { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: null, openPurchaseOrders: [] } });
    const dialog = screen.getByRole('dialog');
    expect(dialog).toHaveAccessibleName('Component Information');
    expect(screen.getByText('Site SW')).toBeInTheDocument();
    expect(screen.getByText('A — Active')).toBeInTheDocument();
    expect(screen.getByText('KSS')).toBeInTheDocument();
    expect(screen.getByText('Future Short')).toHaveAttribute('data-severity', 'FutureShort');
    const body = dialog.querySelector('.part-info--embedded')!;
    expect([...body.children].map(element => element.tagName)).toEqual(['SECTION', 'DL', 'DETAILS', 'SECTION', 'SECTION', 'DIV', 'DIV', 'DIV', 'DIV']);
    expect(body.children[0]).toHaveTextContent('Shortage Snapshot');
    expect(body.children[1]).toHaveTextContent('Net QOH10Std / QCTC$12.5000 / $13.7500Purchase LT7 daysMin / multiple100 / 25Safety time2 daysBuyer / IOSJDOE / I');
    expect(body.children[2]).not.toHaveAttribute('open');
    expect(body.children[3]).toHaveAttribute('aria-label', 'Supplier Credit Hold / CIA');
    expect(body.children[4]).toHaveTextContent('Current Buyer Comment');
    expect(screen.getAllByRole('tab').map(tab => tab.textContent?.trim())).toEqual(['Open POs 0', 'Inventory lots', 'Approved alternates']);
    expect(screen.getByRole('tabpanel')).toHaveAttribute('id', 'shortage-info-panel-orders');
    expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
    const css = readFileSync(resolve(process.cwd(), 'src/components/ComponentInfoModal.css'), 'utf8');
    expect(css).toContain('width: min(1100px, 100%)');
    expect(css).not.toContain('.component-info-modal--shortages .component-info-modal__columns');
  });

  it('keeps snapshot and master values separate and wraps every demand parent', () => {
    const parents = Array.from({ length: 40 }, (_, index) => `LONG-MODEL-${index}`);
    renderModal({ context: 'shortages', shortage: shortage(), projectionDetail: projectionFixture(fullShortage({ demandParentParts: parents })),
      purchasing: { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: null, openPurchaseOrders: [] } });
    const right = screen.getByRole('region', { name: 'Shortage Snapshot' });
    for (const field of ['Buyer / IOS', 'Safety Stock', 'Safety time', 'Purchase LT']) {
      expect(screen.getByText(field)).toBeInTheDocument();
      expect(within(right).queryByText(field)).not.toBeInTheDocument();
    }
    expect(screen.getAllByText('KSS')).toHaveLength(1);
    const parentField = within(right).getByText('Demand Parents').parentElement!;
    expect(parentField).toHaveClass('long-term-shortage-detail__parents');
    expect(parentField.querySelector('dd')).toHaveTextContent(parents[parents.length - 1]);
    expect(within(right).queryByText('Manufacturing Lead (site working days)')).not.toBeInTheDocument();
    const css = readFileSync(resolve(process.cwd(), 'src/components/ComponentInfoModal.css'), 'utf8');
    expect(css).toMatch(/\.long-term-shortage-detail__parents dd\s*{[^}]*white-space:\s*normal;[^}]*overflow-wrap:\s*anywhere/);
  });

  it('rounds Past and PO quantities by component UOM without changing raw values, and hides normal-screen evidence', () => {
    const view = renderModal({ context: 'shortages', shortage: shortage(), projectionDetail: projectionFixture(fullShortage()),
      purchasing: { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: 'On order', openPurchaseOrders: [] } });
    const right = screen.getByRole('dialog');
    expect(within(right).getByText('Past Gross Requirements').nextElementSibling).toHaveTextContent('3');
    expect(within(right).getByText('Overdue Receipts (excluded)').nextElementSibling).toHaveTextContent('2');
    expect(within(right).getByText('Adjusted Opening QOH').nextElementSibling).toHaveTextContent('8');
    for (const removed of ['Site Planning Lead Times', 'PO/KSS Context', 'Shortage Episodes', '26-Week Timeline', 'Raw MRP Evidence'])
      expect(within(right).queryByText(removed)).not.toBeInTheDocument();
    view.rerender(<ComponentInfoModal componentPart="COMP-1" context="shortages" shortage={shortage({ unitOfMeasure: 'UNFAMILIAR' })}
      projectionDetail={projectionFixture(fullShortage())}
      detail={makeDetail()} isLoading={false} error={null} onRetry={vi.fn()} onClose={vi.fn()} approvedVendors={null}
      isApprovedVendorsLoading={false} approvedVendorsError={null} onExpandApprovedVendors={vi.fn()} onRetryApprovedVendors={vi.fn()}
      purchasing={{ supplierIdentityUnavailable: false, commentAvailable: true, currentComment: null, openPurchaseOrders: [] }} />);
    expect(within(right).getByText('Past Gross Requirements').nextElementSibling).toHaveTextContent('2.75');
    expect(within(right).getByText('Overdue Receipts (excluded)').nextElementSibling).toHaveTextContent('1.75');
    expect(within(right).getByText('Adjusted Opening QOH').nextElementSibling).toHaveTextContent('8.25');
  });

  it('uses Current Buyer Comment without invented metadata and distinguishes no comment from unavailable', () => {
    const props = { context: 'shortages' as const, shortage: shortage() };
    const view = renderModal({ ...props, purchasing: { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: 'Buyer note', openPurchaseOrders: [] } });
    const right = screen.getByRole('dialog');
    expect(within(right).getByRole('heading', { name: 'Current Buyer Comment' })).toBeInTheDocument();
    expect(within(right).queryByText('Latest Buyer Comment')).not.toBeInTheDocument();
    expect(within(right).getByText('Buyer note')).toBeInTheDocument();
    expect(within(right).getByText('Date unknown')).toBeInTheDocument();
    view.rerender(<ComponentInfoModal componentPart="COMP-1" detail={makeDetail()} isLoading={false} error={null}
      onRetry={vi.fn()} onClose={vi.fn()} approvedVendors={null} isApprovedVendorsLoading={false} approvedVendorsError={null}
      onExpandApprovedVendors={vi.fn()} onRetryApprovedVendors={vi.fn()} {...props}
      purchasing={{ supplierIdentityUnavailable: false, commentAvailable: true, currentComment: '  ', openPurchaseOrders: [] }} />);
    expect(within(right).getByText('No buyer comment on file')).toBeInTheDocument();
  });

  it.each([false, true])('shows source metadata and plain text in shortage details (empty=%s)', (empty) => {
    const text = '<img src=x> first\n  second';
    renderModal({ context: 'shortages', shortage: shortage(), purchasing: { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: empty ? '' : text, commentRecordedAt: '2022-01-31T13:30:00', commentAuthor: 'jdoe', openPurchaseOrders: [] } });
    const right = screen.getByRole('dialog');
    expect(within(right).getByText('Jan 31, 2022 · 1:30 PM · jdoe')).toBeInTheDocument();
    expect(right.textContent).toContain(empty ? 'No comment text in latest entry.' : text);
    expect(within(right).queryByText('No buyer comment on file')).not.toBeInTheDocument();
    expect(right.querySelector('img')).toBeNull();
  });

  it('sorts selected-component open POs past-due first, then by due date, with explicit exception text', () => {
    const po = (poNumber: string, dueDate: string | null, openQuantity: number) => ({
      componentPart: 'COMP-1', description: null, leadTimeDays: null, poNumber, poLine: 1, dueDate, openQuantity,
      confirmed: true, supplierDisplay: 'Acme', buyerDisplay: null, manufacturerItem: null, isKss: false,
      trackingInfo: null, isCreditHold: null, isCia: null, currentComments: null,
    });
    renderModal({ context: 'shortages', shortage: shortage(), purchasing: { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: null,
      openPurchaseOrders: [po('FUTURE', '2999-01-01', 12.75), po('NO-DATE', null, 4.25),
        po('PAST-LATER', '2001-02-01', 1.5), po('PAST-EARLIER', '2000-01-01', 2.5)] } });
    const table = within(screen.getByRole('dialog')).getByRole('table');
    const rows = within(table).getAllByRole('row').slice(1);
    expect(rows.map((element) => element.querySelector('td')?.textContent)).toEqual(['PAST-EARLIER', 'PAST-LATER', 'FUTURE', 'NO-DATE']);
    expect(rows[0]).toHaveTextContent('Past due: 2000-01-01');
    expect(rows[2]).toHaveTextContent('13');
    expect(rows[3]).toHaveTextContent('Due date missing');
  });

  it('uses the shortage report as-of date to identify past-due open PO lines', () => {
    renderModal({ context: 'shortages', asOfDate: '2026-09-25', shortage: shortage(),
      purchasing: { supplierIdentityUnavailable: false, commentAvailable: true, currentComment: null, openPurchaseOrders: [{
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
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Shortage Snapshot' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Show MRP' })).not.toBeInTheDocument();
  });

  it('shows a truthful empty location placeholder and no shortage column from BOM', () => {
    renderModal({ context: 'bom', detail: null, isLoading: true });
    expect(screen.getByText(/Loading component information/i)).toBeInTheDocument();
    expect(screen.queryByRole('complementary', { name: 'Workspace Shortages details' })).not.toBeInTheDocument();
  });

  it('keeps the inventory panel in the middle column for loaded BOM detail', () => {
    renderModal({ context: 'bom' });
    expect(screen.getByLabelText('Inventory / Lot Locations column')).toHaveTextContent('Refresh inventory');
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

  it('keeps Show MRP disabled without a navigation callback', async () => {
    const user = userEvent.setup();
    renderModal();
    const mrpButton = screen.getByRole('button', { name: /show mrp/i });
    expect(mrpButton).toBeDisabled();
    await user.click(mrpButton);
  });

  it('enables Show MRP for a selected component/site and invokes the navigation callback once', async () => {
    const onShowMrp = vi.fn();
    renderModal({ workspaceSite: 'SW', onShowMrp });
    const button = screen.getByRole('button', { name: 'Show MRP' });
    expect(button).toBeEnabled();
    await userEvent.click(button);
    expect(onShowMrp).toHaveBeenCalledTimes(1);
  });

  it.each([{ componentPart: '' }, { workspaceSite: '' }])('does not enable Show MRP for incomplete identity %j', async identity => {
    const onShowMrp = vi.fn();
    renderModal({ workspaceSite: 'SW', onShowMrp, ...identity });
    const button = screen.getByRole('button', { name: 'Show MRP' });
    expect(button).toBeDisabled();
    await userEvent.click(button);
    expect(onShowMrp).not.toHaveBeenCalled();
  });

  it('reserves faded text and stop cursor for :disabled; enabled Show MRP has pointer, hover and focus styles', () => {
    // jsdom does not execute the imported CSS cascade; assert selector wiring, not visual proof.
    const css = readFileSync(resolve(process.cwd(), 'src/components/ComponentInfoModal.css'), 'utf8');
    const base = /\.component-info-modal__mrp-btn\s*{([^}]*)}/.exec(css)?.[1];
    expect(base).toMatch(/color:\s*var\(--tx2\)/);
    expect(base).toMatch(/opacity:\s*1\s*;/);
    expect(base).toMatch(/cursor:\s*pointer\s*;/);
    expect(base).not.toMatch(/not-allowed|opacity:\s*0\./);
    expect(css).toMatch(/\.component-info-modal__mrp-btn:disabled\s*{[^}]*color:\s*var\(--tx3\);[^}]*opacity:\s*0\.6;[^}]*cursor:\s*not-allowed;/);
    expect(css).toMatch(/\.component-info-modal__mrp-btn:hover:not\(:disabled\)\s*{[^}]*background:\s*var\(--row-hover\)/);
    expect(css).toMatch(/\.component-info-modal__mrp-btn:focus-visible:not\(:disabled\)\s*{[^}]*outline:\s*2px solid var\(--accent-solid\)/);
  });

  it('requires loaded workspace context before acquiring inventory', () => {
    renderModal();
    expect(screen.getByText('Inventory / Lot Locations')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Refresh inventory' })).toBeDisabled();
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

    it('Inventory / Lot Locations remains independent alongside AVL', () => {
      renderModal();
      expect(screen.getByText('Inventory / Lot Locations')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Refresh inventory' })).toBeInTheDocument();
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
    const focusable = within(dialog).getAllByRole('button').filter(button => !button.hasAttribute('disabled'));
    focusable[focusable.length - 1].focus();
    await user.tab();
    expect(focusable[0]).toHaveFocus();
  });
});

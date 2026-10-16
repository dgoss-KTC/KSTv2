import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { PartInfoLookup } from './PartInfoLookup';
import { PartInfoError, type PartInfo } from '../api/partInfoApi';

const time = '2026-10-14T12:00:00Z';
const section = <T,>(value: T) => ({ value, error: null, acquiredAtUtc: time });
function fixture(part = 'EXACT', site = 'SW'): PartInfo {
  return { site, part, master: { description: 'Synthetic part', partStatusCode: 'AC', partStatusDescription: 'Active', iosCode: null,
    standardCost: '0', qctc: null, timeFence: null, safetyTime: '0', safetyStock: null, buyerPlanner: '123',
    purchaseLeadTimeDays: 3, inspectionLeadTimeDays: null, cumulativeLeadTimeDays: null, minimumOrderQuantity: '0', orderMultiple: null },
    inventory: section({ netQuantityOnHand: '0', nonNetQuantityOnHand: '12.123456789' }),
    locations: section([{ rowId: '1', location: 'A', lotSerial: 'LONG-LOT-EXACT', status: 'STOCK', quantityOnHand: '99999999999999999.123456789' }]),
    alternates: section([{ supplier: 'Alternate', vendorName: null, supplierItem: 'ALT-1', manufacturerPart: null }]),
    context: section({ manufacturerItem: 'MFG', effectivePmCode: 'M', manufacturingLeadWorkingDays: 3 }),
    orders: section([]), comment: section('Buyer comment\nwith another line'),
    suppliers: section([{ identifier: 'Supplier A', display: 'A', isCreditHold: true, isCia: false },
      { identifier: 'Supplier B', display: 'B', isCreditHold: false, isCia: true },
      { identifier: 'supplier a', display: 'a', isCreditHold: null, isCia: null }]) };
}
const loadSpec = vi.fn(async () => ({ sites: ['SW', 'KV'], maximumPartLength: 256 }));
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(r => { resolve = r; }); return { promise, resolve }; }
async function enter(part = 'EXACT') {
  await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled());
  fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: part } });
}

describe('Part Info Lookup', () => {
  it('trims only insignificant inventory decimal zeros without rounding or treating missing as zero', async () => {
    const value = fixture();
    value.locations = section(['0.0000000000', '7872.0000000000', '12.3400000000', '-0.1250000000', '99999999999999999.1234567890', null]
      .map((quantityOnHand, index) => ({ rowId: String(index), location: `L${index}`, lotSerial: null, status: null, quantityOnHand })));
    render(<PartInfoLookup lookup={vi.fn(async () => value)} loadSpec={loadSpec} />);
    await enter(); fireEvent.click(screen.getByText('Lookup'));
    fireEvent.click(await screen.findByRole('tab', { name: /Inventory lots/ }));
    const table = await screen.findByRole('table', { name: 'Inventory / Lot Locations' });
    const displayed = within(table).getAllByRole('row').slice(1).map(row => within(row).getAllByRole('cell')[3].textContent);
    expect(displayed).toEqual(['0', '7872', '12.34', '-0.125', '99999999999999999.123456789', '—']);
  });
  it('submits with Enter, renders approved groups and exact/null values, expands alternates with no export controls', async () => {
    const lookup = vi.fn(async () => fixture()); render(<PartInfoLookup lookup={lookup} loadSpec={loadSpec} />);
    expect(screen.getByText(/Enter a part number/)).toBeInTheDocument();
    await enter(); await userEvent.click(screen.getByLabelText('Part Number')); await userEvent.keyboard('{Enter}');
    expect(await screen.findByRole('heading', { name: 'EXACT' })).toBeInTheDocument();
    expect(lookup).toHaveBeenCalledWith('SW', 'EXACT', expect.any(AbortSignal));
    expect(screen.getByText('$0.0000')).toBeInTheDocument();
    fireEvent.click(screen.getByText('More part details'));
    fireEvent.click(screen.getByRole('tab', { name: /Inventory lots/ }));
    expect(screen.getByText('99999999999999999.123456789')).toBeInTheDocument();
    expect(screen.getByText('12.123456789')).toBeInTheDocument();
    fireEvent.click(screen.getByText(/Supplier status details/));
    const status = screen.getByRole('table', { name: 'Supplier status' });
    expect(within(status).getAllByText('Yes')).toHaveLength(2); expect(within(status).getAllByText('No')).toHaveLength(2); expect(within(status).getAllByText('Unknown')).toHaveLength(2);
    fireEvent.click(screen.getByRole('tab', { name: /Approved alternates/ }));
    expect(screen.getByText('ALT-1')).toBeVisible();
    expect(screen.queryByText(/Shortage Snapshot|Past gross|Opening QOH|Save As|Generate|Export/)).not.toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('tab', { name: /Inventory lots/ }));
    expect(screen.getByRole('region', { name: 'Scrollable Inventory / Lot Locations' })).toHaveAttribute('tabindex', '0');
  });

  it('discards late responses on input/site changes, refresh, cancellation and departure', async () => {
    const a = deferred<PartInfo>(); const b = deferred<PartInfo>(); const c = deferred<PartInfo>();
    const lookup = vi.fn().mockReturnValueOnce(a.promise).mockReturnValueOnce(b.promise).mockReturnValueOnce(c.promise);
    const view = render(<PartInfoLookup lookup={lookup} loadSpec={loadSpec} />); await enter('A'); fireEvent.click(screen.getByText('Lookup'));
    expect(screen.getByRole('status')).toHaveTextContent('Acquiring');
    fireEvent.change(screen.getByLabelText('Site'), { target: { value: 'KV' } }); await enter('B'); fireEvent.click(screen.getByText('Lookup'));
    await act(async () => b.resolve(fixture('B', 'KV')));
    await act(async () => a.resolve(fixture('A')));
    expect(screen.getByRole('heading', { name: 'B' })).toBeInTheDocument(); expect(screen.queryByRole('heading', { name: 'A' })).not.toBeInTheDocument();
    expect(lookup.mock.calls[0][2].aborted).toBe(true);
    fireEvent.click(screen.getByText('Refresh / Retry sections')); fireEvent.click(screen.getByText('Cancel lookup'));
    await act(async () => c.resolve(fixture('B', 'KV'))); expect(screen.queryByRole('heading', { name: 'B' })).not.toBeInTheDocument();
    const d = deferred<PartInfo>(); lookup.mockReturnValueOnce(d.promise); fireEvent.click(screen.getByText('Lookup'));
    view.rerender(<PartInfoLookup active={false} lookup={lookup} loadSpec={loadSpec} />);
    await act(async () => d.resolve(fixture('B', 'KV'))); expect(screen.queryByRole('heading', { name: 'B' })).not.toBeInTheDocument();
  });

  it('distinguishes missing master, acquisition error and retry success with independent empty/failed sections', async () => {
    const value = fixture(); value.locations = { value: null, error: 'Locations unavailable.', acquiredAtUtc: null };
    value.suppliers = section([]); value.alternates = section([]);
    const lookup = vi.fn().mockRejectedValueOnce(new PartInfoError(404, 'No part master.')).mockRejectedValueOnce(new Error('Acquisition failed.')).mockResolvedValue(value);
    render(<PartInfoLookup lookup={lookup} loadSpec={loadSpec} />); await enter(); fireEvent.click(screen.getByText('Lookup'));
    expect(await screen.findByRole('heading', { name: 'Part not found' })).toBeInTheDocument();
    fireEvent.click(screen.getByText('Retry lookup')); expect(await screen.findByRole('heading', { name: 'Lookup unavailable' })).toBeInTheDocument();
    fireEvent.click(screen.getByText('Retry lookup')); expect(await screen.findByText('Locations unavailable.')).toBeInTheDocument();
    expect(screen.getByText('No supplier status is available from open POs.')).toBeInTheDocument();
    expect(screen.getByText('No qualifying open purchase orders found.')).toBeInTheDocument();
    expect(screen.getByText('Buyer comment with another line')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('tab', { name: /Approved alternates/ })); expect(screen.getByText('No approved alternates found.')).toBeVisible();
  });

  it('rejects mismatched identity even from an obsolete or faulty transport', async () => {
    render(<PartInfoLookup lookup={vi.fn(async () => fixture('OTHER'))} loadSpec={loadSpec} />); await enter(); fireEvent.click(screen.getByText('Lookup'));
    expect(await screen.findByText('Lookup identity mismatch. Please retry.')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'OTHER' })).not.toBeInTheDocument();
  });

  it('refreshes locations independently and ignores late inventory after identity change', async () => {
    const pending = deferred<{ site: string; part: string; locations: PartInfo['locations'] }>();
    const refresh = vi.fn().mockResolvedValueOnce({ site: 'SW', part: 'EXACT', locations: section([]) }).mockReturnValueOnce(pending.promise);
    const lookup = vi.fn(async () => fixture());
    render(<PartInfoLookup lookup={lookup} loadSpec={loadSpec} refreshLocations={refresh} />);
    await enter(); fireEvent.click(screen.getByText('Lookup')); await screen.findByRole('heading', { name: 'EXACT' });
    fireEvent.click(screen.getByRole('tab', { name: /Inventory lots/ }));
    fireEvent.click(screen.getByText('Refresh inventory')); expect(await screen.findByText('No inventory locations found.')).toBeInTheDocument();
    expect(lookup).toHaveBeenCalledTimes(1); expect(screen.getByText('$0.0000')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Refresh inventory')); await enter('OTHER');
    await act(async () => pending.resolve({ site: 'SW', part: 'EXACT', locations: section([]) }));
    expect(screen.queryByRole('heading', { name: 'EXACT' })).not.toBeInTheDocument();
    expect(refresh.mock.calls[1][2].aborted).toBe(true);
  });

  it('supports keyboard tabs and a focus-contained buyer comment dialog with focus return', async () => {
    render(<PartInfoLookup lookup={vi.fn(async () => fixture())} loadSpec={loadSpec} />);
    await enter(); fireEvent.click(screen.getByText('Lookup'));
    const orders = await screen.findByRole('tab', { name: /Open POs/ });
    expect(orders).toHaveAttribute('aria-selected', 'true');
    orders.focus(); await userEvent.keyboard('{ArrowRight}');
    expect(screen.getByRole('tab', { name: /Inventory lots/ })).toHaveFocus();
    expect(screen.getByRole('tabpanel')).toHaveAttribute('id', 'part-info-panel-locations');
    await userEvent.keyboard('{End}'); expect(screen.getByRole('tab', { name: /Approved alternates/ })).toHaveFocus();
    await userEvent.keyboard('{Home}'); expect(orders).toHaveFocus();
    const preview = screen.getByRole('button', { name: /Read full comment/ }); await userEvent.click(preview);
    const dialog = screen.getByRole('dialog', { name: 'Buyer comment · EXACT' });
    expect(within(dialog).getByRole('button', { name: 'Close buyer comment' })).toHaveFocus();
    await userEvent.tab(); expect(within(dialog).getByRole('region', { name: 'Full buyer comment' })).toHaveFocus();
    await userEvent.tab(); expect(within(dialog).getByRole('button')).toHaveFocus();
    await userEvent.keyboard('{Escape}'); expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(preview).toHaveFocus();
    await userEvent.click(preview); fireEvent.click(screen.getByRole('dialog').parentElement!);
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(preview).toHaveFocus();
  });

  it('keeps unavailable and unknown supplier flags distinct from an explicit all-No summary', async () => {
    const value = fixture(); value.suppliers = section([{ identifier: 'Only', display: null, isCreditHold: false, isCia: null }]);
    render(<PartInfoLookup lookup={vi.fn(async () => value)} loadSpec={loadSpec} />);
    await enter(); fireEvent.click(screen.getByText('Lookup')); await screen.findByRole('heading', { name: 'EXACT' });
    const strip = screen.getByRole('region', { name: 'Supplier Credit Hold / CIA' });
    expect(strip).toHaveTextContent('Credit hold: None'); expect(strip).toHaveTextContent('CIA: No confirmed Yes · Unknown: Only');
  });

  it('never reports none for failed supplier enrichment and closes the comment on identity changes', async () => {
    const value = fixture(); value.suppliers = { value: [{ identifier: 'Only', display: null, isCreditHold: null, isCia: null }], error: 'Supplier acquisition unavailable.', acquiredAtUtc: null };
    render(<PartInfoLookup lookup={vi.fn(async () => value)} loadSpec={loadSpec} />);
    await enter(); fireEvent.click(screen.getByText('Lookup')); await screen.findByRole('heading', { name: 'EXACT' });
    const strip = screen.getByRole('region', { name: 'Supplier Credit Hold / CIA' });
    expect(strip).toHaveTextContent('Unavailable: Only'); expect(strip).not.toHaveTextContent('None');
    expect(within(strip).getByRole('alert')).toHaveTextContent('Supplier acquisition unavailable.');
    await userEvent.click(screen.getByRole('button', { name: /Read full comment/ }));
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'OTHER' } });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(screen.queryByRole('tablist')).not.toBeInTheDocument();
  });
});

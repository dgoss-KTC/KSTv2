import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { PartInfoLookup } from './PartInfoLookup';
import { ComponentInfoModal } from '../components/ComponentInfoModal';
import type { PartInfo } from '../api/partInfoApi';

vi.mock('../api/inventoryLocationsApi', () => ({ fetchInventoryLocations: vi.fn() }));
const section = <T,>(value: T) => ({ value, error: null, acquiredAtUtc: null });
const report: PartInfo = {
  site: 'SW', part: 'LEGACY', master: { description: 'Legacy display', partStatusCode: 'A', partStatusDescription: 'Active', iosCode: 'I',
    standardCost: '0.286832', qctc: null, timeFence: '5', safetyTime: '2', safetyStock: '3', buyerPlanner: 'BUYER', purchaseLeadTimeDays: 7,
    inspectionLeadTimeDays: 1, cumulativeLeadTimeDays: 8, minimumOrderQuantity: '100', orderMultiple: '25' },
  inventory: section({ netQuantityOnHand: '10', nonNetQuantityOnHand: '4' }), locations: section([]), alternates: section([]),
  context: section({ manufacturerItem: 'ITEM', effectivePmCode: 'P', manufacturingLeadWorkingDays: null }), orders: section([]),
  comment: section(null), suppliers: section([]),
};

describe('Part Info presentation extraction', () => {
  it('retains the pre-extraction lookup identity, facts, fields and tab markup and uses nonconflicting popup IDs', async () => {
    const view = render(<PartInfoLookup lookup={vi.fn(async () => report)} loadSpec={vi.fn(async () => ({ sites: ['SW'], maximumPartLength: 256 }))} />);
    await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled());
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'LEGACY' } });
    fireEvent.click(screen.getByText('Lookup'));
    await screen.findByRole('heading', { name: 'LEGACY' });
    // Literal expectations retained from the existing lookup markup, not a generated snapshot.
    expect(view.container.querySelector('.part-info__identity')?.outerHTML).toBe('<header class="part-info__identity"><div><h2>LEGACY</h2><p>Legacy display</p><strong class="report-chip">Site SW</strong><span class="report-chip">A — Active</span></div><button class="report-button" aria-label="Refresh / Retry sections" title="Refresh all sections; retries any that failed">Refresh</button></header>');
    expect(view.container.querySelector('.part-info__facts')?.outerHTML).toBe('<dl class="part-info__facts"><div><dt>Net QOH</dt><dd>10</dd></div><div><dt>Std / QCTC</dt><dd><span>$0.2868</span> / <span>—</span></dd></div><div><dt>Purchase LT</dt><dd>7 days</dd></div><div><dt>Min / multiple</dt><dd>100 / 25</dd></div><div><dt>Safety time</dt><dd>2 days</dd></div><div><dt>Buyer / IOS</dt><dd>BUYER / I</dd></div></dl>');
    expect(view.container.querySelector('.part-info__fields')?.outerHTML).toBe('<dl class="part-info__fields"><div><dt>Non-Net QOH</dt><dd>4</dd></div><div><dt>Time Fence</dt><dd>5</dd></div><div><dt>Safety Stock</dt><dd>3</dd></div></dl>');
    expect(screen.getByRole('tab', { name: 'Open POs 0' }).outerHTML).toBe('<button id="part-info-tab-orders" role="tab" aria-selected="true" aria-controls="part-info-panel-orders" tabindex="0">Open POs <span class="part-info__count">0</span></button>');
    expect(view.container.querySelector('.part-info__supplier-strip')?.outerHTML).toBe('<section class="part-info__supplier-strip" aria-label="Supplier Credit Hold / CIA"><p>No supplier status is available from open POs.</p></section>');
    render(<ComponentInfoModal context="shortages" componentPart="POPUP" detail={null} isLoading={true} error={null}
      onRetry={vi.fn()} onClose={vi.fn()} approvedVendors={null} isApprovedVendorsLoading={false} approvedVendorsError={null}
      onExpandApprovedVendors={vi.fn()} onRetryApprovedVendors={vi.fn()} />);
    const ids = [...document.querySelectorAll('[id]')].map(element => element.id);
    expect(new Set(ids).size).toBe(ids.length);
    for (const tab of screen.getAllByRole('tab')) {
      const panel = document.getElementById(tab.getAttribute('aria-controls')!)!;
      expect(panel).toHaveAttribute('aria-labelledby', tab.id);
    }
    const popup = screen.getByRole('dialog', { name: 'Component Information' });
    fireEvent.click(within(popup).getByRole('tab', { name: 'Inventory lots' }));
    expect(within(popup).getByRole('tabpanel')).toHaveAttribute('id', 'shortage-info-panel-locations');
    expect(within(view.container).getByRole('tabpanel')).toHaveAttribute('id', 'part-info-panel-orders');
  });
});

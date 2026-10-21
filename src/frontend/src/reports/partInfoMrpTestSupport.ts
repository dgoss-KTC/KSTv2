import type { PartInfo } from '../api/partInfoApi';
import type { ComponentMrpRequest, ComponentMrpScreen } from '../api/componentMrpApi';
const section = <T,>(value: T) => ({ value, error: null, acquiredAtUtc: null });
export function partInfoFixture(part = 'ABC', site = 'SW'): PartInfo {
  return { site, part, master: { description: 'Synthetic facts', partStatusCode: 'A', partStatusDescription: 'Active', iosCode: null,
    standardCost: '0', qctc: null, timeFence: null, safetyTime: null, safetyStock: null, buyerPlanner: '123',
    purchaseLeadTimeDays: 3, inspectionLeadTimeDays: null, cumulativeLeadTimeDays: null, minimumOrderQuantity: '0', orderMultiple: null },
    inventory: section({ netQuantityOnHand: '0', nonNetQuantityOnHand: '0' }), locations: section([]), alternates: section([]),
    context: section({ manufacturerItem: null, effectivePmCode: null, manufacturingLeadWorkingDays: null }),
    orders: section([]), comment: section(null), suppliers: section([]) };
}
export function mrpFixture(request: ComponentMrpRequest = { site: 'SW', part: 'ABC', horizonWeeks: 26, period: 'Week', includeUnconfirmed: true }): ComponentMrpScreen {
  return { site: request.site, part: request.part, horizonWeeks: request.horizonWeeks ?? 26, period: request.period ?? 'Week',
    includeUnconfirmed: request.includeUnconfirmed ?? true, asOfDate: '2026-09-29', acquiredAtUtc: '2026-09-29T12:00Z',
    forecastEndExclusive: '2027-03-28', acquisitionId: request.acquisitionId ?? '11111111-1111-4111-8111-111111111111',
    expiresAtUtc: new Date(Date.now() + 1_800_000).toISOString(), unitOfMeasure: 'KG', adjustedOpeningQoh: '-0.995', safetyStock: null, dataQualityWarning: null,
    periods: [{ label: 'Past Due', start: null, endExclusive: '2026-09-29', demand: '2', receipts: request.includeUnconfirmed === false ? '0' : '50',
      projectedQoh: '-0.995', plannedDue: '0', plannedRelease: '0', hasUnconfirmed: request.includeUnconfirmed !== false },
    { label: request.period === 'Day' ? '2026-09-29' : request.period === 'Month' ? '2026-09 *' : '2026-09-28', start: '2026-09-29', endExclusive: '2026-10-04',
      demand: '0.008', receipts: '3', projectedQoh: '1.997', plannedDue: '7', plannedRelease: '7', hasUnconfirmed: request.includeUnconfirmed !== false }] };
}

import { describe, expect, it } from 'vitest';
import type { LongTermShortageRow } from '../api/longTermShortagesApi';
import { compactFixture } from './compactScreenFixture.test-support';
import { EMPTY_LONG_TERM_SHORTAGES_FILTERS, filterLongTermShortages, formatLongTermQuantity } from './longTermShortagesPresentation';

function row(overrides: Partial<LongTermShortageRow> = {}) {
  return compactFixture({
    componentPart: 'COMP-A', unitOfMeasure: null, qadStatus: null, description: null, planner: null,
    buyerPlannerCode: null, openingQoh: 0, safetyStockState: 'Resolved', safetyStock: 0,
    severity: 'Healthy', firstShortDate: null, demandParentParts: [],
    past: { weekNumber: null, weekStart: null, grossRequirements: 0, scheduledReceipts: 0, plannedOrdersDue: 0, plannedOrdersRelease: 0, projectedQoh: 0, severity: 'Healthy', unconfirmedReceipts: 0, confirmedEnding: 0, allReceiptsEnding: 0, planningEnding: 0, allReceiptsPlanningEnding: 0, lowestProjectedBalance: 0, lowestConfirmedBalance: 0, lowestAllReceiptsBalance: 0, overdueReceipts: 0, includesUnconfirmed: false },
    weeks: [], evidence: [], presentation: null, episodes: [], dataQualityWarning: null, firstAtRiskDate: null, effectivePmCode: null, partStatusDescription: null, orderPeriodDays: null, safetyTimeWorkingDays: null, manufacturingLeadWorkingDays: null, purchasingLeadCalendarDays: null, cumulativeLeadCalendarDays: null, sitePlanningPresent: null, ...overrides,
  });
}

describe('filterLongTermShortages', () => {
  it('defaults to critical, future, and safety shortage rows, places healthy rows last, and applies loaded-row filters', () => {
    const rows = [
      row({ componentPart: 'CLEAR', planner: 'Ann' }),
      row({ componentPart: 'SAFETY-LATE', qadStatus: 'P', planner: 'Bob', severity: 'SafetyStockShort', firstShortDate: '2026-10-26' }),
      row({ componentPart: 'CRITICAL-EARLY', severity: 'CriticalShort', firstShortDate: '2026-10-05' }),
      row({ componentPart: 'FUTURE', severity: 'FutureShort', firstShortDate: '2026-10-06' }),
    ];
    expect(filterLongTermShortages(rows, EMPTY_LONG_TERM_SHORTAGES_FILTERS).map((item) => item.componentPart)).toEqual(['CRITICAL-EARLY', 'FUTURE', 'SAFETY-LATE']);
    expect(filterLongTermShortages(rows, { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, showAll: true }).map((item) => item.componentPart)).toEqual(['CRITICAL-EARLY', 'FUTURE', 'SAFETY-LATE', 'CLEAR']);
    expect(filterLongTermShortages(rows, { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, statusOnly: true, planner: 'bob' }).map((item) => item.componentPart)).toEqual(['SAFETY-LATE']);
    expect(filterLongTermShortages([row({ componentPart: 'KSS', severity: 'CriticalShort', presentation: { manufacturerItem: null, poNumber: null, poLine: null, poDueDate: null, poOpenQuantity: null, poConfirmed: null, isKss: true } }), row({ componentPart: 'NOT-KSS', severity: 'CriticalShort' })], { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, kssOnly: true }).map((item) => item.componentPart)).toEqual(['KSS']);
  });
  it('orders deepest shortages using precise decimal quantities', () => {
    const shallow = row({ componentPart: 'SMALL', severity: 'CriticalShort', episodes: [{ startDate: '2026-10-05', deepestDate: '2026-10-05', maximumShortage: '9007199254740992.01', firstRecoveryDate: null, stableClearDate: null }] });
    const deep = row({ componentPart: 'LARGE', severity: 'CriticalShort', episodes: [{ startDate: '2026-10-06', deepestDate: '2026-10-06', maximumShortage: '9007199254740992.02', firstRecoveryDate: null, stableClearDate: null }] });
    expect(filterLongTermShortages([shallow, deep], { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, sort: 'deepestShortage' }).map((item) => item.componentPart)).toEqual(['LARGE', 'SMALL']);
  });
});

describe('normalized UOM quantity display', () => {
  it('rounds display only, leaving source decimals in the API', () => {
    expect(formatLongTermQuantity('98.2456140337', ' EA ')).toBe('98');
    expect(formatLongTermQuantity(-73.4437, 'kg')).toBe('-73.44');
    expect(formatLongTermQuantity('9007199254740993.125', ' KG ')).toBe('9007199254740993.13');
    expect(formatLongTermQuantity('-1.5', 'ea')).toBe('-2');
    expect(formatLongTermQuantity('-0.001', 'EA')).toBe('0');
    expect(formatLongTermQuantity('10.00', 'KG')).toBe('10');
    expect(formatLongTermQuantity('1.245', 'unlisted')).toBe('1.25');
    expect(formatLongTermQuantity('1.245', null)).toBe('1.25');
    expect(formatLongTermQuantity('1.245', ' PK ')).toBe('1');
    expect(formatLongTermQuantity('1.245', 'bx')).toBe('1');
  });
});

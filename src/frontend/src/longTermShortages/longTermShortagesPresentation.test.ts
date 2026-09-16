import { describe, expect, it } from 'vitest';
import type { LongTermShortageRow } from '../api/longTermShortagesApi';
import { EMPTY_LONG_TERM_SHORTAGES_FILTERS, filterLongTermShortages, formatLongTermQuantity } from './longTermShortagesPresentation';

function row(overrides: Partial<LongTermShortageRow> = {}): LongTermShortageRow {
  return {
    componentPart: 'COMP-A', unitOfMeasure: null, qadStatus: null, description: null, isKss: false, leadTimeWeeks: null,
    planner: null, openingQoh: 0, displayOpeningQoh: 0, safetyStockState: 'Resolved', safetyStock: 0, displaySafetyStock: 0,
    severity: 'Clear', firstSafetyStockShortWeek: null, firstCriticalShortWeek: null,
    demandParentParts: [], otherProgramParentParts: [], weeks: [], purchaseOrders: [], buyerPlannerCode: null, ...overrides,
  };
}

describe('filterLongTermShortages', () => {
  it('defaults to shortage rows, shows clear rows last with Show All, and applies local filters', () => {
    const rows = [
      row({ componentPart: 'CLEAR', planner: 'Ann', severity: 'Clear' }),
      row({ componentPart: 'KSS-LATE', isKss: true, qadStatus: 'P', planner: 'Bob', severity: 'SafetyStockShort', firstSafetyStockShortWeek: 8 }),
      row({ componentPart: 'EARLY', severity: 'CriticalShort', firstSafetyStockShortWeek: 2 }),
    ];
    expect(filterLongTermShortages(rows, EMPTY_LONG_TERM_SHORTAGES_FILTERS).map((item) => item.componentPart)).toEqual(['EARLY', 'KSS-LATE']);
    expect(filterLongTermShortages(rows, { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, showAll: true }).map((item) => item.componentPart)).toEqual(['EARLY', 'KSS-LATE', 'CLEAR']);
    expect(filterLongTermShortages(rows, { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, kssOnly: true, statusOnly: true, planner: 'bob' }).map((item) => item.componentPart)).toEqual(['KSS-LATE']);
  });
});

describe('Stage 11-A quantity display formatting', () => {
  it('formats API-owned display values without reapplying UOM rounding', () => {
    expect(formatLongTermQuantity(5)).toBe('5');
    expect(formatLongTermQuantity(-73.4437)).toBe('-73.4437');
  });
});

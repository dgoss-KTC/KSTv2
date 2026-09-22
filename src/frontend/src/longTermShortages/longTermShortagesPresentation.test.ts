import { describe, expect, it } from 'vitest';
import type { LongTermShortageRow } from '../api/longTermShortagesApi';
import { EMPTY_LONG_TERM_SHORTAGES_FILTERS, filterLongTermShortages, formatLongTermQuantity } from './longTermShortagesPresentation';

function row(overrides: Partial<LongTermShortageRow> = {}): LongTermShortageRow {
  return {
    componentPart: 'COMP-A', unitOfMeasure: null, qadStatus: null, description: null, planner: null,
    buyerPlannerCode: null, openingQoh: 0, safetyStockState: 'Resolved', safetyStock: 0,
    severity: 'Clear', firstShortDate: null, demandParentParts: [],
    past: { weekNumber: null, weekStart: null, grossRequirements: 0, scheduledReceipts: 0, plannedOrdersDue: 0, plannedOrdersRelease: 0, projectedQoh: 0, severity: 'Clear' },
    weeks: [], evidence: [], presentation: null, ...overrides,
  };
}

describe('filterLongTermShortages', () => {
  it('defaults to critical and safety shortage rows, places clear rows last, and applies loaded-row filters', () => {
    const rows = [
      row({ componentPart: 'CLEAR', planner: 'Ann' }),
      row({ componentPart: 'SAFETY-LATE', qadStatus: 'P', planner: 'Bob', severity: 'SafetyStockShort', firstShortDate: '2026-10-26' }),
      row({ componentPart: 'CRITICAL-EARLY', severity: 'CriticalShort', firstShortDate: '2026-10-05' }),
    ];
    expect(filterLongTermShortages(rows, EMPTY_LONG_TERM_SHORTAGES_FILTERS).map((item) => item.componentPart)).toEqual(['CRITICAL-EARLY', 'SAFETY-LATE']);
    expect(filterLongTermShortages(rows, { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, showAll: true }).map((item) => item.componentPart)).toEqual(['CRITICAL-EARLY', 'SAFETY-LATE', 'CLEAR']);
    expect(filterLongTermShortages(rows, { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, statusOnly: true, planner: 'bob' }).map((item) => item.componentPart)).toEqual(['SAFETY-LATE']);
    expect(filterLongTermShortages([row({ componentPart: 'KSS', severity: 'CriticalShort', presentation: { manufacturerItem: null, poNumber: null, poLine: null, poDueDate: null, poOpenQuantity: null, poConfirmed: null, isKss: true } }), row({ componentPart: 'NOT-KSS', severity: 'CriticalShort' })], { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, kssOnly: true }).map((item) => item.componentPart)).toEqual(['KSS']);
  });
});

describe('raw MRP quantity display', () => {
  it('preserves the API-provided decimal representation without rounding', () => {
    expect(formatLongTermQuantity('98.2456140337')).toBe('98.2456140337');
    expect(formatLongTermQuantity(-73.4437)).toBe('-73.4437');
  });
});

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from '../fiscal/fiscalCalendar';
import { add, equalDecimal, formatDecimal, sumDecimals } from '../shared/exactDecimal';
import {
  DEFAULT_PLANNING_HORIZON_WEEKS,
  MIN_PLANNING_HORIZON_WEEKS,
  formatPlanningDate,
  parseDateOnly,
  partQuarterSummaries,
  planningColumns,
  planningQuarters,
  workspaceTotals,
  type PartQuarterSummary,
  type PlanningWorkbook,
} from './planningWorkbookSummary';
import { fixtureToWorkbook, type FixtureQuarter, type Stage14Fixture } from './stage14FixtureAdapter.testSupport';

// The sensitivity check points KST_STAGE14_FIXTURE at a temporary copy; the accepted file is never edited.
const fixturePath = process.env.KST_STAGE14_FIXTURE
  ?? resolve(process.cwd(), '../../docs/reference/STAGE_14_PLANNING_WORKBOOK_DETERMINISTIC_FIXTURES.json');
const fixture = JSON.parse(readFileSync(fixturePath, 'utf8')) as Stage14Fixture;
const workbook = fixtureToWorkbook(fixture);
const quarterKey = (s: PartQuarterSummary) => `FY${String(s.quarter.fiscalYear).slice(-2)}-Q${s.quarter.fiscalQuarter}`;

function expectQuarter(actual: PartQuarterSummary, expected: FixtureQuarter) {
  expect(quarterKey(actual)).toBe(expected.quarter);
  expect(actual.quarter.includesFalldown).toBe(expected.includesFalldown);
  for (const [field, key] of [['salesOrderQuantity', 'so'], ['salesOrderDollars', 'soDollars'], ['mps', 'mps'], ['estimatedOnHand', 'estOnHand']] as const) {
    expect(equalDecimal(actual[field], expected[key]), `${expected.quarter} ${field}: ${actual[field]} vs ${expected[key]}`).toBe(true);
  }
  if (expected.mpsDollars === null) expect(actual.mpsDollars).toBeNull();
  else expect(equalDecimal(actual.mpsDollars ?? 'x', expected.mpsDollars), `${expected.quarter} mpsDollars: ${actual.mpsDollars} vs ${expected.mpsDollars}`).toBe(true);
}

describe('Planning Workbook horizon defaults (owner-confirmed 14.4)', () => {
  it('defaults to and never goes below 12 weeks', () => {
    expect(DEFAULT_PLANNING_HORIZON_WEEKS).toBe(12);
    expect(MIN_PLANNING_HORIZON_WEEKS).toBe(12);
  });
});

describe('Planning Workbook fiscal quarters against the accepted Stage 14 fixture', () => {
  it('reproduces every part quarter expectation with the default calendar, in retained part order', () => {
    const quarters = planningQuarters(workbook.weekLabels, DEFAULT_FISCAL_CALENDAR_SETTINGS);
    expect(workbook.parts.map(p => p.part)).toEqual(fixture.scope.resolvedParts);
    for (const part of workbook.parts) {
      const expected = fixture.expected.parts.find(p => p.part === part.part)!.quarters;
      const actual = partQuarterSummaries(part, quarters);
      expect(actual).toHaveLength(expected.length);
      actual.forEach((summary, i) => expectQuarter(summary, expected[i]));
    }
  });

  it('applies the configured 53-week exception (FY27 P3) to P-1 quarter expectations', () => {
    const quarters = planningQuarters(workbook.weekLabels, fixture.fiscalExceptionCase.fiscalSettings);
    expect(quarters.map(q => q.weekIndexes.length)).toEqual([3, 1]);
    expect(quarters[0].quarterWeekCount).toBe(14);
    const p1 = workbook.parts.find(p => p.part === 'P-1')!;
    const actual = partQuarterSummaries(p1, quarters);
    fixture.fiscalExceptionCase.expectedP1Quarters.forEach((expected, i) => expectQuarter(actual[i], expected));
  });

  it('marks partial quarters from actual fiscal boundaries; Falldown never completes a quarter', () => {
    const quarters = planningQuarters(workbook.weekLabels, DEFAULT_FISCAL_CALENDAR_SETTINGS);
    expect(quarters.map(q => [q.label, q.weekIndexes.length, q.quarterWeekCount, q.isPartial, q.includesFalldown]))
      .toEqual([['FY27 Q1', 2, 13, true, true], ['FY27 Q2', 2, 13, true, false]]);
    // 13 consecutive weeks covering FY27 Q2 exactly (starts Sunday 2026-09-27) form a complete quarter.
    const fullQ2 = Array.from({ length: 13 }, (_, i) => isoAddDays('2026-09-28', 7 * i));
    const [q2] = planningQuarters(fullQ2, DEFAULT_FISCAL_CALENDAR_SETTINGS);
    expect([q2.label, q2.isPartial]).toEqual(['FY27 Q2', false]);
  });

  it('splits quarters at the fiscal-year boundary, honoring a 53-week year', () => {
    const labels = ['2027-06-14', '2027-06-21', '2027-06-28', '2027-07-05'];
    expect(planningQuarters(labels, DEFAULT_FISCAL_CALENDAR_SETTINGS).map(q => [q.label, q.weekIndexes]))
      .toEqual([['FY27 Q4', [0, 1]], ['FY28 Q1', [2, 3]]]);
    const extra = { ...DEFAULT_FISCAL_CALENDAR_SETTINGS, exceptions: [{ fiscalYear: 2027, extraWeekPeriod: 12 }] };
    const split = planningQuarters(labels, extra);
    expect(split.map(q => [q.label, q.weekIndexes])).toEqual([['FY27 Q4', [0, 1, 2]], ['FY28 Q1', [3]]]);
    expect(split[0].quarterWeekCount).toBe(14);
  });

  it('inserts a quarter summary column after each quarter, including the final partial quarter', () => {
    const quarters = planningQuarters(workbook.weekLabels, DEFAULT_FISCAL_CALENDAR_SETTINGS);
    expect(planningColumns(quarters).map(c => (c.kind === 'week' ? `w${c.index}` : `q${c.position}`)))
      .toEqual(['w0', 'w1', 'q0', 'w2', 'w3', 'q1']);
  });

  it('uses the ending balance, not a sum, for Est. On Hand', () => {
    const quarters = planningQuarters(workbook.weekLabels, DEFAULT_FISCAL_CALENDAR_SETTINGS);
    const p1 = workbook.parts.find(p => p.part === 'P-1')!;
    const [q1] = partQuarterSummaries(p1, quarters);
    expect(q1.estimatedOnHand).toBe(p1.weeks[1].estimatedOnHand);
    expect(equalDecimal(q1.estimatedOnHand, sumDecimals([p1.weeks[0].estimatedOnHand, p1.weeks[1].estimatedOnHand]))).toBe(false);
  });

  it('includes Falldown exactly once, only in the current quarter', () => {
    const quarters = planningQuarters(workbook.weekLabels, DEFAULT_FISCAL_CALENDAR_SETTINGS);
    const p2 = workbook.parts.find(p => p.part === 'P-2')!;
    const [q1, q2] = partQuarterSummaries(p2, quarters);
    expect(q1.salesOrderQuantity).toBe('20'); // Falldown 20 + weeks 0 + 0
    expect(q2.salesOrderQuantity).toBe('30'); // weeks only
    const totals = workspaceTotals(workbook.parts, workbook.weekLabels.length, quarters);
    const allFlows = sumDecimals(workbook.parts.flatMap(p => [p.falldown, ...p.weeks]).map(b => b.salesOrderQuantity));
    expect(sumDecimals(totals.quarters.map(q => q.salesOrderQuantity))).toBe(allFlows);
  });

  it('gives forecast no quarter value and keeps it out of every other total', () => {
    const quarters = planningQuarters(workbook.weekLabels, DEFAULT_FISCAL_CALENDAR_SETTINGS);
    const summaries = workbook.parts.map(p => partQuarterSummaries(p, quarters));
    for (const summary of summaries.flat()) expect(Object.keys(summary)).not.toContain('forecast');
    const altered: PlanningWorkbook = { ...workbook, parts: workbook.parts.map(p => ({
      ...p, falldown: { ...p.falldown, forecast: '999999' }, weeks: p.weeks.map(w => ({ ...w, forecast: '123.45' })) })) };
    expect(altered.parts.map(p => partQuarterSummaries(p, quarters))).toEqual(summaries);
    expect(workspaceTotals(altered.parts, 4, quarters)).toEqual(workspaceTotals(workbook.parts, 4, quarters));
  });
});

describe('Workspace totals (owner-approved no-list-price $0 policy, 2026-10-07)', () => {
  const quarters = planningQuarters(workbook.weekLabels, DEFAULT_FISCAL_CALENDAR_SETTINGS);

  it('sums every part into workspace MPS $: the no-list-price part P-3 contributes exactly $0 and nothing is blank', () => {
    const totals = workspaceTotals(workbook.parts, 4, quarters);
    for (const flows of [totals.falldown, ...totals.weeks, ...totals.quarters]) expect(typeof flows.mpsDollars).toBe('string');
    const pricedOnly = workspaceTotals(workbook.parts.filter(p => p.isPriced), 4, quarters);
    [...totals.weeks, ...totals.quarters].forEach((flows, i) => expect(equalDecimal(flows.mpsDollars, [...pricedOnly.weeks, ...pricedOnly.quarters][i].mpsDollars)).toBe(true));
    expect(workbook.parts.find(p => p.part === 'P-3')!.isPriced).toBe(false);
    // Quantities still include P-3 (week 3 MPS 10) while its dollars are $0.
    expect(totals.weeks[3].mps).toBe('10');
    expect(totals.quarters[1].mps).toBe('210');
    expect(equalDecimal(totals.quarters[0].salesOrderDollars, add('1234.5678', '401.00')!)).toBe(true);
  });

  it('totals MPS $ exactly for priced parts, including a valid zero-price part', () => {
    const priced = workbook.parts.filter(p => p.isPriced);
    expect(priced.map(p => [p.part, p.selectedPrice])).toEqual([['P-2', '0.0000'], ['P-1', '2.5000']]);
    const totals = workspaceTotals(priced, 4, quarters);
    expect(totals.weeks[2].mpsDollars).toBe('250'); // P-2 100 x 0 + P-1 100 x 2.5
    expect(totals.quarters[0].mpsDollars).toBe('513.75');
    expect(totals.quarters[1].mpsDollars).toBe('250');
  });

  it('never sums inventory balances across parts', () => {
    const totals = workspaceTotals(workbook.parts, 4, quarters);
    expect(Object.keys(totals.quarters[0])).toEqual(['salesOrderQuantity', 'salesOrderDollars', 'mps', 'mpsDollars']);
  });

  it('keeps Undated values out of Falldown, weekly and quarter totals', () => {
    const totals = workspaceTotals(workbook.parts, 4, quarters);
    const dated = sumDecimals(workbook.parts.flatMap(p => [p.falldown, ...p.weeks]).map(b => b.salesOrderQuantity));
    expect(sumDecimals(totals.quarters.map(q => q.salesOrderQuantity))).toBe(dated);
    const undated = sumDecimals(workbook.parts.map(p => p.undated.salesOrderQuantity));
    expect(undated).toBe('7');
    expect(dated).toBe('210');
  });
});

describe('exact decimals and dates', () => {
  it('adds without JavaScript Number precision loss', () => {
    expect(0.1 + 0.2).not.toBe(0.3);
    expect(add('0.1', '0.2')).toBe('0.3');
    expect(Number('9007199254740993')).toBe(9007199254740992);
    expect(sumDecimals(['9007199254740993', '0.0000000001', '-1'])).toBe('9007199254740992.0000000001');
    expect(sumDecimals(['188.75000', '250.0000', '75.0000'])).toBe('513.75');
  });

  it('rounds only for presentation', () => {
    expect(formatDecimal('1234.5678', 2)).toBe('1,234.57');
    expect(formatDecimal('-0.004', 2)).toBe('0.00');
    expect(formatDecimal('9007199254740993', null)).toBe('9,007,199,254,740,993');
    expect(formatDecimal('0.0000', 4)).toBe('0.0000');
  });

  it('formats M/d/yyyy without leading zeros or timezone shifts', () => {
    expect(formatPlanningDate('2026-09-07')).toBe('9/7/2026');
    expect(formatPlanningDate('2026-10-05')).toBe('10/5/2026');
    expect(formatPlanningDate('2027-01-01')).toBe('1/1/2027');
    const parsed = parseDateOnly('2027-01-01');
    expect([parsed.getFullYear(), parsed.getMonth(), parsed.getDate()]).toEqual([2027, 0, 1]);
  });
});

function isoAddDays(iso: string, days: number): string {
  const d = parseDateOnly(iso);
  d.setDate(d.getDate() + days);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

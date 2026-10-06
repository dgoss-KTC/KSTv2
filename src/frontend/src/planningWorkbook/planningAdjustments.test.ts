import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from '../fiscal/fiscalCalendar';
import { equalDecimal, sumDecimals } from '../shared/exactDecimal';
import {
  adjustedQuarterSummaries,
  adjustedWorkspaceTotals,
  adjustmentKey,
  evaluatePlanning,
  parseAdjustment,
  type AdjustmentEntries,
} from './planningAdjustments';
import { planningQuarters, type PlanningWorkbook } from './planningWorkbookSummary';
import { fixtureToWorkbook, type Stage14Fixture } from './stage14FixtureAdapter.testSupport';

/*
 * Stage 14.5 frontend adjustment calculations. Every expectation is read from the accepted Stage 14
 * deterministic fixture (docs/reference/STAGE_14_PLANNING_WORKBOOK_DETERMINISTIC_FIXTURES.json), which
 * is independent of this implementation; comparisons use exact decimal equality.
 */
interface AdjustedFixtureBucket { weekLabel?: string; adjustment?: string; proposedMps: string; adjustedMpsDollars: string | null; adjOnHand: string }
interface AdjustedFixtureQuarter { quarter: string; adjustment: string; proposedMps: string; adjustedMpsDollars: string | null; adjOnHand: string; includesFalldown: boolean }
interface FixtureCase { id: string; part?: string; weekLabel?: string; target?: string; adjustment?: string; existingMps?: string; proposedMps?: string; result?: string; issueCode?: string }
type Fixture = Stage14Fixture & {
  adjustmentScenario: { adjustments: { part: string; weekLabel: string; adjustment: string }[] };
  validationCases: { cases: FixtureCase[] };
  draftRestoreCases: { cases: { id: string; saved?: { part: string; weekLabel: string; adjustment: string }; currentMps?: string; proposedMps?: string; issueCode?: string; exportEligible?: boolean;
    before?: { part: string; weekLabel: string; mps: string; adjustment: string; proposedMps: string }; afterMpsRefresh?: { mps: string; adjustment: string; proposedMps: string } }[] };
};

const fixture = JSON.parse(readFileSync(resolve(process.cwd(), '../../docs/reference/STAGE_14_PLANNING_WORKBOOK_DETERMINISTIC_FIXTURES.json'), 'utf8')) as Fixture;
const expectedParts = fixture.expected.parts as unknown as { part: string; priced: boolean; falldown: AdjustedFixtureBucket; weeks: AdjustedFixtureBucket[]; quarters: AdjustedFixtureQuarter[] }[];

function entries(list: { part: string; weekLabel: string; adjustment: string }[]): AdjustmentEntries {
  return Object.fromEntries(list.map(e => [adjustmentKey(e.part, e.weekLabel), { part: e.part, weekLabel: e.weekLabel, text: e.adjustment }]));
}
const scenario = entries(fixture.adjustmentScenario.adjustments);
const same = (actual: string | null, expected: string | null) => (expected === null ? actual === null : actual !== null && equalDecimal(actual, expected));

function withWeekMps(workbook: PlanningWorkbook, part: string, weekLabel: string, mps: string): PlanningWorkbook {
  return { ...workbook, mpsSnapshotId: 'replaced', parts: workbook.parts.map(p => p.part !== part ? p
    : { ...p, weeks: p.weeks.map(w => (w.weekLabel === weekLabel ? { ...w, mps } : w)) }) };
}

describe('parseAdjustment mirrors the accepted Domain adjustment parser', () => {
  it.each(fixture.validationCases.cases.filter(c => ['V08', 'V09', 'V10', 'V11', 'V12', 'V13', 'V14', 'V15'].includes(c.id)))('$id: "$adjustment"', c => {
    const kind = parseAdjustment(c.adjustment!).kind;
    expect(kind).toBe(c.result === 'no-op' ? 'noop' : c.issueCode);
  });

  it.each([
    ['', 'noop'], ['-', 'invalid-number'], ['5.', 'invalid-number'], ['.5', 'invalid-number'], [' 5', 'invalid-number'],
    ['5 ', 'invalid-number'], ['--5', 'invalid-number'], ['\uFF15', 'invalid-number'], ['-10.25', 'not-whole-number'],
    ['79228162514264337593543950336', 'invalid-number'], ['000', 'noop'],
  ])('"%s" -> %s', (text, kind) => expect(parseAdjustment(text).kind).toBe(kind));

  it('canonicalizes valid values without Number conversion', () => {
    expect(parseAdjustment('007')).toEqual({ kind: 'value', value: '7' });
    expect(parseAdjustment('-0042')).toEqual({ kind: 'value', value: '-42' });
    expect(parseAdjustment('79228162514264337593543950335')).toEqual({ kind: 'value', value: '79228162514264337593543950335' });
  });
});

describe('evaluatePlanning reproduces the accepted fixture adjustment scenario', () => {
  const workbook = fixtureToWorkbook(fixture);
  const result = evaluatePlanning(workbook, scenario);

  it('derives Proposed MPS, Adjusted MPS $ and Adj On Hand for every Falldown and weekly cell', () => {
    for (const expected of expectedParts) {
      const part = result.parts.find(p => p.part === expected.part)!;
      for (const [actual, want] of [[part.falldown, expected.falldown] as const, ...part.weeks.map((w, i) => [w, expected.weeks[i]] as const)]) {
        expect(same(actual.adjustment, want.adjustment ?? '0'), `${expected.part} ${want.weekLabel ?? 'Falldown'} adjustment`).toBe(true);
        expect(same(actual.proposedMps, want.proposedMps), `${expected.part} ${want.weekLabel ?? 'Falldown'} proposed`).toBe(true);
        expect(same(actual.adjustedMpsDollars, want.adjustedMpsDollars), `${expected.part} ${want.weekLabel ?? 'Falldown'} adjusted $`).toBe(true);
        expect(same(actual.adjustedOnHand, want.adjOnHand), `${expected.part} ${want.weekLabel ?? 'Falldown'} adj on hand`).toBe(true);
      }
    }
    expect(result.stagedCount).toBe(4);
    expect(result.issueCount).toBe(0);
    expect(result.isValid).toBe(true);
  });

  it('Adj On Hand equals Est. On Hand plus cumulative adjustments (same Falldown seed)', () => {
    for (const part of result.parts) {
      expect(equalDecimal(part.falldown.adjustedOnHand, part.falldown.estimatedOnHand)).toBe(true);
      let cumulative = 0n;
      for (const week of part.weeks) {
        cumulative += BigInt(week.adjustment);
        // exact: adjusted - estimated == cumulative visible adjustments
        const scale = Math.max((week.adjustedOnHand.split('.')[1] ?? '').length, (week.estimatedOnHand.split('.')[1] ?? '').length);
        const toInt = (v: string) => { const [w, f = ''] = v.split('.'); return BigInt(w + f.padEnd(scale, '0')); };
        expect(toInt(week.adjustedOnHand) - toInt(week.estimatedOnHand)).toBe(cumulative * 10n ** BigInt(scale));
      }
    }
  });

  it('produces the fixture quarter flows (Falldown once in the current quarter) and ending balances, with no forecast total', () => {
    const quarters = planningQuarters(workbook.weekLabels, DEFAULT_FISCAL_CALENDAR_SETTINGS);
    for (const expected of expectedParts) {
      const summaries = adjustedQuarterSummaries(result.parts.find(p => p.part === expected.part)!, quarters);
      expect(summaries).toHaveLength(expected.quarters.length);
      summaries.forEach((s, i) => {
        const want = expected.quarters[i];
        expect(s.quarter.includesFalldown).toBe(want.includesFalldown);
        expect(same(s.adjustment, want.adjustment), `${expected.part} ${want.quarter} adjustment`).toBe(true);
        expect(same(s.proposedMps, want.proposedMps), `${expected.part} ${want.quarter} proposed`).toBe(true);
        expect(same(s.adjustedMpsDollars, want.adjustedMpsDollars), `${expected.part} ${want.quarter} adjusted $`).toBe(true);
        expect(same(s.adjustedOnHand, want.adjOnHand), `${expected.part} ${want.quarter} adj on hand`).toBe(true);
        expect('forecast' in s).toBe(false);
      });
    }
  });

  it('totals workspace flows from the fixture parts and blanks adjusted dollars when any part is unpriced', () => {
    const quarters = planningQuarters(workbook.weekLabels, DEFAULT_FISCAL_CALENDAR_SETTINGS);
    const totals = adjustedWorkspaceTotals(result.parts, workbook.weekLabels.length, quarters);
    workbook.weekLabels.forEach((_, i) => {
      const sum = (field: 'adjustment' | 'proposedMps') => sumDecimals(expectedParts.map(p => p.weeks[i][field] ?? '0'));
      expect(equalDecimal(totals.weeks[i].adjustment, sum('adjustment'))).toBe(true);
      expect(equalDecimal(totals.weeks[i].proposedMps, sum('proposedMps'))).toBe(true);
      expect(totals.weeks[i].adjustedMpsDollars).toBeNull(); // P-3 is unpriced
    });
    expect(totals.falldown.adjustment).toBe('0');
    expect('adjustedOnHand' in totals.falldown).toBe(false);

    const priced = adjustedWorkspaceTotals(result.parts.filter(p => p.isPriced), workbook.weekLabels.length, quarters);
    // P-1 adjusted $ week 2026-09-14 = 125, P-2 zero price = 0.
    expect(equalDecimal(priced.weeks[0].adjustedMpsDollars!, '125')).toBe(true);
    expect(equalDecimal(priced.weeks[2].adjustedMpsDollars!, '250')).toBe(true);
  });

  it('keeps forecast display-only: changing forecast changes no adjusted value', () => {
    const changed: PlanningWorkbook = { ...workbook, parts: workbook.parts.map(p => ({ ...p, falldown: { ...p.falldown, forecast: '999' }, weeks: p.weeks.map(w => ({ ...w, forecast: '777' })) })) };
    const other = evaluatePlanning(changed, scenario);
    other.parts.forEach((part, i) => part.weeks.forEach((w, j) => {
      const base = result.parts[i].weeks[j];
      expect([w.proposedMps, w.adjustedMpsDollars, w.adjustedOnHand]).toEqual([base.proposedMps, base.adjustedMpsDollars, base.adjustedOnHand]);
    }));
  });
});

describe('evaluatePlanning validation (fixture V01–V07, V16–V19)', () => {
  const workbook = fixtureToWorkbook(fixture);
  const single = (c: FixtureCase) => evaluatePlanning(workbook, entries([{ part: c.part!, weekLabel: c.weekLabel!, adjustment: c.adjustment! }])).entries[0];

  it.each(fixture.validationCases.cases.filter(c => ['V01', 'V02', 'V03', 'V04', 'V05', 'V06', 'V07'].includes(c.id)))('$id', c => {
    const e = single(c);
    expect(same(e.currentMps, c.existingMps!)).toBe(true);
    expect(same(e.proposedMps, c.proposedMps!)).toBe(true);
    expect(e.issue).toBe(c.result === 'reject' ? c.issueCode : null);
  });

  it('V18 week passed and V19 part out of scope are retained with unavailable baselines (null, not zero)', () => {
    for (const id of ['V18', 'V19']) {
      const c = fixture.validationCases.cases.find(x => x.id === id)!;
      const e = single(c);
      expect(e.issue).toBe(c.issueCode);
      expect(e.currentMps).toBeNull();
      expect(e.proposedMps).toBeNull();
    }
    // A genuine zero baseline (P-1 2026-10-05 has no MPS) is "0", not unavailable.
    expect(single({ id: 'zero', part: 'P-1', weekLabel: '2026-10-05', adjustment: '5' }).currentMps).toBe('0');
  });

  it('V16: Falldown has no adjustment input and its proposed supply equals existing supply', () => {
    const result = evaluatePlanning(workbook, scenario);
    for (const part of result.parts) {
      expect(part.falldown.input).toBe('');
      expect(part.falldown.adjustment).toBe('0');
      expect(part.falldown.proposedMps).toBe(part.falldown.mps);
    }
  });

  it('keeps entries outside the visible horizon or beyond the MPS-derived maximum as hidden, unchecked and non-blocking (owner decision 2026-10-06)', () => {
    const result = evaluatePlanning(workbook, entries([
      { part: 'P-1', weekLabel: '2026-10-19', adjustment: '5' },   // week index 5 < maximum 12
      { part: 'P-1', weekLabel: '2026-12-07', adjustment: '-5' },  // week index 12 >= maximum 12
    ]));
    expect(result.entries.map(e => [e.weekLabel, e.visible, e.hidden, e.issue, e.currentMps, e.proposedMps])).toEqual([
      ['2026-10-19', false, 'outside-horizon', null, null, null], ['2026-12-07', false, 'beyond-maximum', null, null, null],
    ]);
    // Being hidden is not an issue and does not block; the baselines are not claimed validated (null).
    expect(result.issueCount).toBe(0);
    expect(result.hiddenCount).toBe(2);
    expect(result.isValid).toBe(true);
    // Hidden entries never change visible derived values.
    expect(result.parts.find(p => p.part === 'P-1')!.weeks.map(w => w.adjustment)).toEqual(['0', '0', '0', '0']);
  });

  it('still reports business issues for entries that are not shown (passed week, out-of-scope part, invalid text)', () => {
    const result = evaluatePlanning(workbook, entries([
      { part: 'P-1', weekLabel: '2026-09-07', adjustment: '5' },
      { part: 'X-9', weekLabel: '2026-10-19', adjustment: '5' },
      { part: 'P-1', weekLabel: '2026-10-19', adjustment: '1.5' },
    ]));
    expect(result.entries.map(e => [e.part, e.weekLabel, e.issue, e.hidden])).toEqual([
      ['P-1', '2026-09-07', 'week-passed', null], ['P-1', '2026-10-19', 'not-whole-number', null], ['X-9', '2026-10-19', 'part-out-of-scope', null],
    ]);
    expect(result.isValid).toBe(false);
    expect(result.hiddenCount).toBe(0);
  });

  it('treats no-ops as non-proposals and invalid syntax as blocking without applying it', () => {
    const result = evaluatePlanning(workbook, entries([
      { part: 'P-1', weekLabel: '2026-09-14', adjustment: '-0' },
      { part: 'P-1', weekLabel: '2026-09-21', adjustment: '1,000' },
    ]));
    expect(result.stagedCount).toBe(0);
    expect(result.issueCount).toBe(1);
    expect(result.isValid).toBe(false);
    const p1 = result.parts.find(p => p.part === 'P-1')!;
    expect(p1.weeks[1].proposedMps).toBe('75.5');
    expect(p1.weeks[1].inputIssue).toBe('invalid-number');
    expect(p1.weeks[0].input).toBe('-0');
    expect(p1.weeks[0].inputIssue).toBeNull();
  });
});

describe('signed adjustments across snapshot replacement and business-week rollover', () => {
  const workbook = fixtureToWorkbook(fixture);

  it('D06: reapplies the unchanged signed adjustment to a replaced baseline without conversion or acknowledgment', () => {
    const d06 = fixture.draftRestoreCases.cases.find(c => c.id === 'D06')!;
    const before = d06.before!;
    const staged = entries([{ part: before.part, weekLabel: before.weekLabel, adjustment: before.adjustment }]);
    const first = evaluatePlanning(workbook, staged).entries[0];
    expect(same(first.currentMps, before.mps)).toBe(true);
    expect(same(first.proposedMps, before.proposedMps)).toBe(true);
    const replaced = evaluatePlanning(withWeekMps(workbook, before.part, before.weekLabel, d06.afterMpsRefresh!.mps), staged).entries[0];
    expect(replaced.text).toBe(d06.afterMpsRefresh!.adjustment);
    expect(same(replaced.currentMps, d06.afterMpsRefresh!.mps)).toBe(true);
    expect(same(replaced.proposedMps, d06.afterMpsRefresh!.proposedMps)).toBe(true);
    expect(replaced.issue).toBeNull();
  });

  it('flags an entry that becomes negative against a replaced baseline (fixture D03 values) and keeps it', () => {
    const d03 = fixture.draftRestoreCases.cases.find(c => c.id === 'D03')!;
    const staged = entries([d03.saved!]);
    const now = evaluatePlanning(workbook, staged).entries[0];
    expect(same(now.currentMps, d03.currentMps!)).toBe(true);
    expect(same(now.proposedMps, d03.proposedMps!)).toBe(true);
    expect(now.issue).toBe(d03.issueCode);
    expect(now.text).toBe(d03.saved!.adjustment);
  });

  it('after a business-week rollover the former current week becomes week-passed and is retained', () => {
    const rolled: PlanningWorkbook = { ...workbook, today: '2026-09-21', weekLabels: workbook.weekLabels.slice(1),
      parts: workbook.parts.map(p => ({ ...p, weeks: p.weeks.slice(1) })) };
    const result = evaluatePlanning(rolled, scenario);
    const passed = result.entries.find(e => e.weekLabel === '2026-09-14')!;
    expect(passed.issue).toBe('week-passed');
    expect(passed.text).toBe('-50');
    expect(result.entries.filter(e => e.issue === null)).toHaveLength(3);
  });
});

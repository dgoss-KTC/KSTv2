import { describe, expect, it } from 'vitest';
import {
  DEFAULT_FISCAL_CALENDAR_SETTINGS,
  getFiscalDisplayInfo,
  getFiscalPeriod,
  getFiscalQuarter,
  getFiscalWeek,
  getFiscalYear,
  getFiscalYearStart,
  getFiscalYearWeekCount,
  getPeriodWeekCounts,
  getPeriodWeekSpan,
  startOfBusinessWeek,
} from './fiscalCalendar';
import type { FiscalCalendarSettings } from './types';

const settingsWithFy27Exception: FiscalCalendarSettings = {
  ...DEFAULT_FISCAL_CALENDAR_SETTINGS,
  exceptions: [{ fiscalYear: 2027, extraWeekPeriod: 4 }],
};

describe('fiscalCalendar', () => {
  it('resolves FY26 to Sunday, June 29, 2025 (case 1)', () => {
    const start = getFiscalYearStart(DEFAULT_FISCAL_CALENDAR_SETTINGS, 2026);
    expect(start).toEqual(new Date(2025, 5, 29));
    expect(start.getDay()).toBe(0); // Sunday
  });

  it('gives a standard year 52 weeks via the 4-4-5 x 4 pattern (case 2)', () => {
    expect(getFiscalYearWeekCount(DEFAULT_FISCAL_CALENDAR_SETTINGS, 2026)).toBe(52);
    expect(getPeriodWeekCounts(DEFAULT_FISCAL_CALENDAR_SETTINGS, 2026)).toEqual([
      4, 4, 5, 4, 4, 5, 4, 4, 5, 4, 4, 5,
    ]);
  });

  it('gives a configured exception year exactly 53 weeks (case 3)', () => {
    expect(getFiscalYearWeekCount(settingsWithFy27Exception, 2027)).toBe(53);
  });

  it('assigns the extra week to the configured period (case 4)', () => {
    const counts = getPeriodWeekCounts(settingsWithFy27Exception, 2027);
    expect(counts[3]).toBe(5); // period 4, normally 4 weeks
    expect(counts.reduce((a, b) => a + b, 0)).toBe(53);
  });

  it('shifts periods after the extra week by one week within that fiscal year (case 5)', () => {
    const yearStart = getFiscalYearStart(settingsWithFy27Exception, 2027);
    const period5Span = getPeriodWeekSpan(settingsWithFy27Exception, 2027, 5);

    // Standard weeks-before period 5 is 4+4+5+4=17; with the exception at period 4 it's 18.
    const expectedStart = new Date(yearStart);
    expectedStart.setDate(expectedStart.getDate() + 18 * 7);
    expect(period5Span.startDate).toEqual(expectedStart);
    expect(period5Span.weekCount).toBe(4);
  });

  it('shifts the next fiscal year start by one extra week after a 53-week year (case 6)', () => {
    const normalTransition =
      getFiscalYearStart(DEFAULT_FISCAL_CALENDAR_SETTINGS, 2027).getTime() -
      getFiscalYearStart(DEFAULT_FISCAL_CALENDAR_SETTINGS, 2026).getTime();
    expect(normalTransition).toBe(52 * 7 * 86_400_000);

    const exceptionalTransition =
      getFiscalYearStart(settingsWithFy27Exception, 2028).getTime() -
      getFiscalYearStart(settingsWithFy27Exception, 2027).getTime();
    expect(exceptionalTransition).toBe(53 * 7 * 86_400_000);
  });

  it('maps fiscal periods to quarters as P1-P3/P4-P6/P7-P9/P10-P12 (case 7)', () => {
    expect([1, 2, 3].map(getFiscalQuarter)).toEqual([1, 1, 1]);
    expect([4, 5, 6].map(getFiscalQuarter)).toEqual([2, 2, 2]);
    expect([7, 8, 9].map(getFiscalQuarter)).toEqual([3, 3, 3]);
    expect([10, 11, 12].map(getFiscalQuarter)).toEqual([4, 4, 4]);
  });

  it('resolves Sunday and Saturday of the same week to the same business week (case 8)', () => {
    const sunday = new Date(2025, 6, 6); // Sunday, July 6 2025
    const saturday = new Date(2025, 6, 12); // Saturday, July 12 2025

    expect(startOfBusinessWeek(sunday)).toEqual(startOfBusinessWeek(saturday));
    expect(getFiscalDisplayInfo(DEFAULT_FISCAL_CALENDAR_SETTINGS, sunday)).toEqual(
      getFiscalDisplayInfo(DEFAULT_FISCAL_CALENDAR_SETTINGS, saturday),
    );
  });

  it('resolves an MPS Monday label to its Sunday-Saturday business week (case 9)', () => {
    const monday = new Date(2025, 6, 7); // Monday, July 7 2025
    const precedingSunday = new Date(2025, 6, 6);

    expect(getFiscalDisplayInfo(DEFAULT_FISCAL_CALENDAR_SETTINGS, monday)).toEqual(
      getFiscalDisplayInfo(DEFAULT_FISCAL_CALENDAR_SETTINGS, precedingSunday),
    );
  });

  it('remains stable across a 72-week MPS horizon (case 10)', () => {
    const anchor = getFiscalYearStart(DEFAULT_FISCAL_CALENDAR_SETTINGS, 2026);
    let previous = getFiscalDisplayInfo(DEFAULT_FISCAL_CALENDAR_SETTINGS, anchor);

    for (let week = 1; week < 72; week++) {
      const date = new Date(anchor);
      date.setDate(date.getDate() + week * 7);
      const info = getFiscalDisplayInfo(DEFAULT_FISCAL_CALENDAR_SETTINGS, date);

      if (info.fiscalYear === previous.fiscalYear) {
        expect(info.fiscalWeek).toBe(previous.fiscalWeek + 1);
      } else {
        expect(info.fiscalYear).toBe(previous.fiscalYear + 1);
        expect(info.fiscalWeek).toBe(1);
      }
      expect(info.fiscalPeriod).toBeGreaterThanOrEqual(1);
      expect(info.fiscalPeriod).toBeLessThanOrEqual(12);
      expect(info.fiscalQuarter).toBe(getFiscalQuarter(info.fiscalPeriod));

      previous = info;
    }
  });

  it('exposes getFiscalYear/getFiscalWeek/getFiscalPeriod consistently with getFiscalDisplayInfo', () => {
    const date = new Date(2025, 8, 15);
    const info = getFiscalDisplayInfo(DEFAULT_FISCAL_CALENDAR_SETTINGS, date);

    expect(getFiscalYear(DEFAULT_FISCAL_CALENDAR_SETTINGS, date)).toBe(info.fiscalYear);
    expect(getFiscalWeek(DEFAULT_FISCAL_CALENDAR_SETTINGS, date)).toBe(info.fiscalWeek);
    expect(getFiscalPeriod(DEFAULT_FISCAL_CALENDAR_SETTINGS, date)).toBe(info.fiscalPeriod);
  });
});

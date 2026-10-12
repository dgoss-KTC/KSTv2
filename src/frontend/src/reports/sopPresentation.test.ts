import { beforeEach, describe, expect, it } from 'vitest';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from '../fiscal/fiscalCalendar';
import { fiscalStorageKey, sopCalendarSettings, sopPresentation } from './sopPresentation';

describe('S&OP fiscal presentation uses the accepted calendar', () => {
  beforeEach(() => localStorage.clear());
  it('uses actual settings, partial periods and full-horizon year transitions', () => {
    const p = sopPresentation(['2026-06-22', '2026-06-29', '2026-07-06']);
    expect(p.periods.map(x => [x.fiscalYear, x.period, x.weekLabels.length])).toEqual([[2026, 12, 1], [2027, 1, 2]]);
    expect(p.exceptions).toEqual([]);
  });
  it('configured extra week belongs to selected period and shifts next year', () => {
    localStorage.setItem(fiscalStorageKey, JSON.stringify({ ...DEFAULT_FISCAL_CALENDAR_SETTINGS, exceptions: [{ fiscalYear: 2027, extraWeekPeriod: 4 }] }));
    const p = sopPresentation(['2026-10-26', '2026-11-02']);
    expect(p.periods[0].period).toBe(4); expect(p.periods[0].weekCount).toBe(5);
    expect(p.periods[1].period).toBe(5);
    expect(sopPresentation(['2027-06-28', '2027-07-05']).periods.map(x => x.fiscalYear)).toEqual([2027, 2028]);
  });
  it('rejects malformed saved config and malformed/nonconsecutive week identities', () => {
    expect(() => sopPresentation(['2026-10-12', '2026-10-26'])).toThrow();
    expect(() => sopPresentation(['2026-10-11'])).toThrow();
    localStorage.setItem(fiscalStorageKey, JSON.stringify({ ...DEFAULT_FISCAL_CALENDAR_SETTINGS, exceptions: [{ fiscalYear: 2027, extraWeekPeriod: 13 }] }));
    expect(() => sopCalendarSettings()).toThrow();
  });
});

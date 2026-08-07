import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS, getFiscalYearWeekCount } from './fiscalCalendar';
import {
  loadFiscalCalendarSettings,
  saveFiscalCalendarSettings,
  validateFiscalYearException,
} from './fiscalCalendarSettings';

describe('fiscalCalendarSettings', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  afterEach(() => {
    window.localStorage.clear();
  });

  it('falls back to the default anchor when nothing is persisted', () => {
    expect(loadFiscalCalendarSettings()).toEqual(DEFAULT_FISCAL_CALENDAR_SETTINGS);
  });

  it('persists and reloads settings, including exceptions, without any backend involvement (case 11)', () => {
    const withException = {
      ...DEFAULT_FISCAL_CALENDAR_SETTINGS,
      exceptions: [{ fiscalYear: 2027, extraWeekPeriod: 4 }],
    };

    saveFiscalCalendarSettings(withException);
    const reloaded = loadFiscalCalendarSettings();

    expect(reloaded).toEqual(withException);
    // Editing an exception immediately changes generated future fiscal boundaries.
    expect(getFiscalYearWeekCount(reloaded, 2027)).toBe(53);
    expect(getFiscalYearWeekCount(DEFAULT_FISCAL_CALENDAR_SETTINGS, 2027)).toBe(52);
  });

  it('falls back to defaults when persisted data is malformed', () => {
    window.localStorage.setItem('kst.fiscalCalendarSettings.v1', 'not json');
    expect(loadFiscalCalendarSettings()).toEqual(DEFAULT_FISCAL_CALENDAR_SETTINGS);
  });

  it('rejects a second exception for the same fiscal year', () => {
    const existing = [{ fiscalYear: 2027, extraWeekPeriod: 4 }];
    const errors = validateFiscalYearException(existing, { fiscalYear: 2027, extraWeekPeriod: 8 });
    expect(errors).toEqual([{ field: 'fiscalYear', message: 'FY2027 already has an exception.' }]);
  });

  it('rejects an extra-week period outside 1-12', () => {
    const errors = validateFiscalYearException([], { fiscalYear: 2029, extraWeekPeriod: 13 });
    expect(errors).toEqual([
      { field: 'extraWeekPeriod', message: 'Extra week period must be between 1 and 12.' },
    ]);
  });

  it('accepts a valid, unique exception', () => {
    const errors = validateFiscalYearException(
      [{ fiscalYear: 2027, extraWeekPeriod: 4 }],
      { fiscalYear: 2033, extraWeekPeriod: 8 },
    );
    expect(errors).toEqual([]);
  });
});

import type { FiscalCalendarSettings, FiscalDisplayInfo, FiscalPeriodSpan } from './types';

/** Accepted Stage 5A anchor: FY26 begins Sunday, June 29, 2025. */
export const DEFAULT_FISCAL_CALENDAR_SETTINGS: FiscalCalendarSettings = {
  anchorFiscalYear: 2026,
  anchorStartDate: '2025-06-29',
  exceptions: [],
};

/** Standard 4-4-5 x 4 pattern: 12 periods, 52 weeks. */
const STANDARD_PERIOD_WEEKS: readonly number[] = [4, 4, 5, 4, 4, 5, 4, 4, 5, 4, 4, 5];

const MAX_FISCAL_YEAR_SEARCH_ITERATIONS = 1000;

function parseIsoDate(iso: string): Date {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day);
}

function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

function daysBetween(from: Date, to: Date): number {
  return Math.round((to.getTime() - from.getTime()) / 86_400_000);
}

/** Normalizes any date to the Sunday that starts its Sunday-Saturday business week. */
export function startOfBusinessWeek(date: Date): Date {
  const normalized = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  return addDays(normalized, -normalized.getDay());
}

/** Week counts (in period order) for a fiscal year, applying its exception if one is configured. */
export function getPeriodWeekCounts(settings: FiscalCalendarSettings, fiscalYear: number): number[] {
  const counts = [...STANDARD_PERIOD_WEEKS];
  const exception = settings.exceptions.find((e) => e.fiscalYear === fiscalYear);
  if (exception) {
    counts[exception.extraWeekPeriod - 1] += 1;
  }
  return counts;
}

/** Total weeks in a fiscal year: 52, or 53 if that year has a configured exception. */
export function getFiscalYearWeekCount(settings: FiscalCalendarSettings, fiscalYear: number): number {
  return getPeriodWeekCounts(settings, fiscalYear).reduce((sum, weeks) => sum + weeks, 0);
}

/** The Sunday that starts the given fiscal year, walked from the configured anchor. */
export function getFiscalYearStart(settings: FiscalCalendarSettings, fiscalYear: number): Date {
  const anchorStart = parseIsoDate(settings.anchorStartDate);

  if (fiscalYear === settings.anchorFiscalYear) return anchorStart;

  let start = anchorStart;
  if (fiscalYear > settings.anchorFiscalYear) {
    for (let year = settings.anchorFiscalYear; year < fiscalYear; year++) {
      start = addDays(start, getFiscalYearWeekCount(settings, year) * 7);
    }
  } else {
    for (let year = settings.anchorFiscalYear; year > fiscalYear; year--) {
      start = addDays(start, -getFiscalYearWeekCount(settings, year - 1) * 7);
    }
  }
  return start;
}

/** The fiscal year containing the business week that the given date falls in. */
export function getFiscalYear(settings: FiscalCalendarSettings, date: Date): number {
  const week = startOfBusinessWeek(date);
  const anchorStart = parseIsoDate(settings.anchorStartDate);

  // A normal fiscal year is 364 days; use that as a starting estimate, then correct exactly.
  let fiscalYear = settings.anchorFiscalYear + Math.round(daysBetween(anchorStart, week) / 364);

  let iterations = 0;
  while (getFiscalYearStart(settings, fiscalYear + 1) <= week) {
    fiscalYear++;
    if (++iterations > MAX_FISCAL_YEAR_SEARCH_ITERATIONS) {
      throw new Error('Fiscal year resolution did not converge; check fiscal calendar settings.');
    }
  }
  while (getFiscalYearStart(settings, fiscalYear) > week) {
    fiscalYear--;
    if (++iterations > MAX_FISCAL_YEAR_SEARCH_ITERATIONS) {
      throw new Error('Fiscal year resolution did not converge; check fiscal calendar settings.');
    }
  }

  return fiscalYear;
}

/** 1-based fiscal week number within its fiscal year. */
export function getFiscalWeek(settings: FiscalCalendarSettings, date: Date): number {
  const fiscalYear = getFiscalYear(settings, date);
  const yearStart = getFiscalYearStart(settings, fiscalYear);
  const week = startOfBusinessWeek(date);
  return Math.floor(daysBetween(yearStart, week) / 7) + 1;
}

/** 1-based fiscal period (1-12) for the given date. */
export function getFiscalPeriod(settings: FiscalCalendarSettings, date: Date): number {
  const fiscalYear = getFiscalYear(settings, date);
  const fiscalWeek = getFiscalWeek(settings, date);
  const counts = getPeriodWeekCounts(settings, fiscalYear);

  let cumulative = 0;
  for (let period = 0; period < counts.length; period++) {
    cumulative += counts[period];
    if (fiscalWeek <= cumulative) return period + 1;
  }
  return counts.length;
}

/** 1-based fiscal quarter (1-4) for a fiscal period (P1-P3 -> Q1, P4-P6 -> Q2, ...). */
export function getFiscalQuarter(period: number): number {
  return Math.ceil(period / 3);
}

/** The Sunday-Saturday week span covered by one fiscal period within a fiscal year. */
export function getPeriodWeekSpan(
  settings: FiscalCalendarSettings,
  fiscalYear: number,
  period: number,
): FiscalPeriodSpan {
  const counts = getPeriodWeekCounts(settings, fiscalYear);
  const weekCount = counts[period - 1];
  const weeksBefore = counts.slice(0, period - 1).reduce((sum, weeks) => sum + weeks, 0);
  const yearStart = getFiscalYearStart(settings, fiscalYear);
  const startDate = addDays(yearStart, weeksBefore * 7);
  const endDate = addDays(startDate, weekCount * 7 - 1);

  return { period, quarter: getFiscalQuarter(period), weekCount, startDate, endDate };
}

/** Full fiscal placement (year/week/period/quarter) for a single calendar date. */
export function getFiscalDisplayInfo(settings: FiscalCalendarSettings, date: Date): FiscalDisplayInfo {
  const fiscalYear = getFiscalYear(settings, date);
  const fiscalWeek = getFiscalWeek(settings, date);
  const fiscalPeriod = getFiscalPeriod(settings, date);
  const fiscalQuarter = getFiscalQuarter(fiscalPeriod);

  return { fiscalYear, fiscalWeek, fiscalPeriod, fiscalQuarter };
}

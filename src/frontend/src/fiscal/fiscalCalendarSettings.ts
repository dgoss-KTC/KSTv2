import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from './fiscalCalendar';
import type { FiscalCalendarSettings, FiscalYearException } from './types';

const STORAGE_KEY = 'kst.fiscalCalendarSettings.v1';

export interface FiscalExceptionValidationError {
  field: 'fiscalYear' | 'extraWeekPeriod';
  message: string;
}

function isFiscalYearException(value: unknown): value is FiscalYearException {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<FiscalYearException>;
  return Number.isInteger(candidate.fiscalYear) && Number.isInteger(candidate.extraWeekPeriod);
}

function normalizeSettings(value: unknown): FiscalCalendarSettings {
  if (typeof value !== 'object' || value === null) return DEFAULT_FISCAL_CALENDAR_SETTINGS;
  const candidate = value as Partial<FiscalCalendarSettings>;

  if (
    typeof candidate.anchorFiscalYear !== 'number' ||
    !Number.isInteger(candidate.anchorFiscalYear) ||
    typeof candidate.anchorStartDate !== 'string' ||
    !Array.isArray(candidate.exceptions)
  ) {
    return DEFAULT_FISCAL_CALENDAR_SETTINGS;
  }

  return {
    anchorFiscalYear: candidate.anchorFiscalYear,
    anchorStartDate: candidate.anchorStartDate,
    exceptions: candidate.exceptions.filter(isFiscalYearException),
  };
}

/** Loads fiscal calendar settings from local storage, falling back to the accepted default anchor. */
export function loadFiscalCalendarSettings(): FiscalCalendarSettings {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return DEFAULT_FISCAL_CALENDAR_SETTINGS;
    return normalizeSettings(JSON.parse(raw));
  } catch {
    return DEFAULT_FISCAL_CALENDAR_SETTINGS;
  }
}

/** Persists fiscal calendar settings to local storage. Purely local; no backend involvement. */
export function saveFiscalCalendarSettings(settings: FiscalCalendarSettings): void {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
}

/** Validates a candidate exception: exactly one per fiscal year, period must be 1-12. */
export function validateFiscalYearException(
  existing: FiscalYearException[],
  candidate: FiscalYearException,
): FiscalExceptionValidationError[] {
  const errors: FiscalExceptionValidationError[] = [];

  if (!Number.isInteger(candidate.fiscalYear)) {
    errors.push({ field: 'fiscalYear', message: 'Fiscal year is required.' });
  } else if (existing.some((e) => e.fiscalYear === candidate.fiscalYear)) {
    errors.push({ field: 'fiscalYear', message: `FY${candidate.fiscalYear} already has an exception.` });
  }

  if (
    !Number.isInteger(candidate.extraWeekPeriod) ||
    candidate.extraWeekPeriod < 1 ||
    candidate.extraWeekPeriod > 12
  ) {
    errors.push({ field: 'extraWeekPeriod', message: 'Extra week period must be between 1 and 12.' });
  }

  return errors;
}

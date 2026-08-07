/**
 * Fiscal planning/display calendar types. Fiscal semantics are frontend-only per the accepted
 * Stage 5A strategy: the backend and QAD never need fiscal year/week/period/quarter concepts.
 */

/** One user-declared 53-week fiscal year exception. */
export interface FiscalYearException {
  fiscalYear: number;
  /** 1-12: the standard period that receives the extra (53rd) week. */
  extraWeekPeriod: number;
}

/** Persisted fiscal calendar configuration. */
export interface FiscalCalendarSettings {
  anchorFiscalYear: number;
  /** ISO local date, e.g. "2025-06-29". Always a Sunday. */
  anchorStartDate: string;
  exceptions: FiscalYearException[];
}

/** The Sunday-Saturday week span covered by one fiscal period. */
export interface FiscalPeriodSpan {
  period: number;
  quarter: number;
  weekCount: number;
  startDate: Date;
  endDate: Date;
}

/** Derived fiscal placement for a single calendar date. */
export interface FiscalDisplayInfo {
  fiscalYear: number;
  fiscalWeek: number;
  fiscalPeriod: number;
  fiscalQuarter: number;
}

import type { components } from '../generated/api';
import { getFiscalDisplayInfo, getPeriodWeekSpan } from '../fiscal/fiscalCalendar';
import type { FiscalCalendarSettings } from '../fiscal/types';
import { sumDecimals } from '../shared/exactDecimal';

export type PlanningWorkbook = components['schemas']['PlanningWorkbookResponseDto'];
export type PlanningPart = components['schemas']['PlanningPartBlockDto'];
export type PlanningBucket = components['schemas']['PlanningBucketDto'];

/**
 * Owner-confirmed Stage 14.4 horizon (2026-10-02): default and minimum selectable 12 weeks. The maximum
 * is not fixed: the API reports max(12, last nonzero MPS week index + 2) for the loaded MPS snapshot.
 */
export const DEFAULT_PLANNING_HORIZON_WEEKS = 12;
export const MIN_PLANNING_HORIZON_WEEKS = 12;

/** Parses an ISO date-only string as a local calendar date (never through UTC). */
export function parseDateOnly(iso: string): Date {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/** User-visible date text: M/d/yyyy with no leading zeros, derived from the ISO text itself. */
export function formatPlanningDate(iso: string | null | undefined): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso ?? '');
  if (!match) return '';
  return `${Number(match[2])}/${Number(match[3])}/${match[1]}`;
}

/** User-visible local calendar date: M/d/yyyy with no leading zeros. */
export function formatLocalDate(date: Date): string {
  return `${date.getMonth() + 1}/${date.getDate()}/${date.getFullYear()}`;
}

/** User-visible local timestamp: M/d/yyyy h:mm AM/PM. */
export function formatPlanningTimestamp(value: string | null | undefined): string {
  const date = value ? new Date(value) : null;
  if (!date || Number.isNaN(date.getTime())) return '';
  const hours = date.getHours();
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${date.getMonth() + 1}/${date.getDate()}/${date.getFullYear()} ${hours % 12 || 12}:${minutes} ${hours < 12 ? 'AM' : 'PM'}`;
}

export interface PlanningQuarter {
  key: string;
  label: string;
  fiscalYear: number;
  fiscalQuarter: number;
  /** Indexes into the workbook's weekLabels / each part's weeks, in order. */
  weekIndexes: number[];
  /** Business weeks in the full fiscal quarter (13, or 14 with a configured 53-week exception). */
  quarterWeekCount: number;
  /** True when the visible weeks do not cover the whole fiscal quarter. Falldown never completes a quarter. */
  isPartial: boolean;
  /** The quarter containing week zero; Falldown flows are added here exactly once. */
  includesFalldown: boolean;
  startDate: Date;
  endDate: Date;
}

/** Groups the API week labels into fiscal quarters using the accepted frontend fiscal calendar. */
export function planningQuarters(weekLabels: readonly string[], settings: FiscalCalendarSettings): PlanningQuarter[] {
  const quarters: PlanningQuarter[] = [];
  weekLabels.forEach((label, index) => {
    const info = getFiscalDisplayInfo(settings, parseDateOnly(label));
    const key = `${info.fiscalYear}-Q${info.fiscalQuarter}`;
    const last = quarters[quarters.length - 1];
    if (last?.key === key) { last.weekIndexes.push(index); return; }
    const periods = [1, 2, 3].map(p => getPeriodWeekSpan(settings, info.fiscalYear, (info.fiscalQuarter - 1) * 3 + p));
    quarters.push({
      key,
      label: `FY${String(info.fiscalYear).slice(-2)} Q${info.fiscalQuarter}`,
      fiscalYear: info.fiscalYear,
      fiscalQuarter: info.fiscalQuarter,
      weekIndexes: [index],
      quarterWeekCount: periods.reduce((sum, span) => sum + span.weekCount, 0),
      isPartial: false,
      includesFalldown: index === 0,
      startDate: periods[0].startDate,
      endDate: periods[2].endDate,
    });
  });
  for (const quarter of quarters) quarter.isPartial = quarter.weekIndexes.length < quarter.quarterWeekCount;
  return quarters;
}

export interface PartQuarterSummary {
  quarter: PlanningQuarter;
  salesOrderQuantity: string;
  salesOrderDollars: string;
  mps: string;
  /** Null when the part is unpriced. */
  mpsDollars: string | null;
  /** Balance at the quarter's last visible week; never a sum. */
  estimatedOnHand: string;
}

function flows(part: PlanningPart, quarter: PlanningQuarter): PlanningBucket[] {
  const weeks = quarter.weekIndexes.map(i => part.weeks[i]);
  return quarter.includesFalldown ? [part.falldown, ...weeks] : weeks;
}

/** Per-part fiscal-quarter flow totals and ending balance. Forecast deliberately has no quarter value. */
export function partQuarterSummaries(part: PlanningPart, quarters: readonly PlanningQuarter[]): PartQuarterSummary[] {
  return quarters.map(quarter => {
    const buckets = flows(part, quarter);
    const lastWeek = part.weeks[quarter.weekIndexes[quarter.weekIndexes.length - 1]];
    return {
      quarter,
      salesOrderQuantity: sumDecimals(buckets.map(b => b.salesOrderQuantity)),
      salesOrderDollars: sumDecimals(buckets.map(b => b.salesOrderDollars)),
      mps: sumDecimals(buckets.map(b => b.mps)),
      mpsDollars: part.isPriced && buckets.every(b => b.mpsDollars !== null)
        ? sumDecimals(buckets.map(b => b.mpsDollars as string)) : null,
      estimatedOnHand: lastWeek.estimatedOnHand,
    };
  });
}

/**
 * Workspace flow totals. Owner-confirmed policy (2026-10-02): when any retained part is unpriced,
 * the workspace MPS $ total is null (blank) for every weekly and quarterly column; no priced-part
 * subtotal is shown. A valid zero price is priced. SO $ always totals (it comes from sales-order lines).
 */
export interface WorkspaceFlowTotals {
  salesOrderQuantity: string;
  salesOrderDollars: string;
  mps: string;
  mpsDollars: string | null;
}

function workspaceFlows(parts: readonly PlanningPart[], pick: (part: PlanningPart) => PlanningBucket[]): WorkspaceFlowTotals {
  const all = parts.flatMap(pick);
  const anyUnpriced = parts.some(p => !p.isPriced);
  return {
    salesOrderQuantity: sumDecimals(all.map(b => b.salesOrderQuantity)),
    salesOrderDollars: sumDecimals(all.map(b => b.salesOrderDollars)),
    mps: sumDecimals(all.map(b => b.mps)),
    mpsDollars: anyUnpriced || all.some(b => b.mpsDollars === null) ? null : sumDecimals(all.map(b => b.mpsDollars as string)),
  };
}

/** Workspace totals per Falldown, visible week and fiscal quarter. Inventory balances are never summed across parts. */
export function workspaceTotals(parts: readonly PlanningPart[], weekCount: number, quarters: readonly PlanningQuarter[]) {
  return {
    falldown: workspaceFlows(parts, p => [p.falldown]),
    weeks: Array.from({ length: weekCount }, (_, i) => workspaceFlows(parts, p => [p.weeks[i]])),
    quarters: quarters.map(q => workspaceFlows(parts, p => flows(p, q))),
  };
}

/** Week-column positions after which each quarter's summary column is inserted. */
export type PlanningColumn = { kind: 'week'; index: number } | { kind: 'quarter'; quarter: PlanningQuarter; position: number };

export function planningColumns(quarters: readonly PlanningQuarter[]): PlanningColumn[] {
  return quarters.flatMap((quarter, position) => [
    ...quarter.weekIndexes.map(index => ({ kind: 'week' as const, index })),
    { kind: 'quarter' as const, quarter, position },
  ]);
}

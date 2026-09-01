import type { MpsBucketDto, WorkOrderMaterialLineDto } from '../api/client';
import type { FiscalDisplayInfo } from '../fiscal/types';

/** Groups consecutive items sharing the same key, returning each group's key and span length. */
export function groupConsecutive<T, K>(items: T[], keyFn: (item: T) => K): { key: K; span: number }[] {
  const groups: { key: K; span: number }[] = [];
  for (const item of items) {
    const key = keyFn(item);
    const last = groups[groups.length - 1];
    if (last && last.key === key) {
      last.span += 1;
    } else {
      groups.push({ key, span: 1 });
    }
  }
  return groups;
}

/** A quarter (or period) header band annotated with its fiscal-quarter number (1-4), used to pick
 * a color family for `.mps-grid__band-col--q1..4` (see MpsWorkspace.css). */
export interface FiscalBandGroup<K> {
  key: K;
  span: number;
  quarterNum: number;
}

/** A period band additionally flags whether it should use the alternate (dimmer) shade within its
 * parent quarter, mirroring the prototype's `periodBandsV` alpha alternation. */
export interface FiscalPeriodBandGroup<K> extends FiscalBandGroup<K> {
  altShade: boolean;
}

/** Annotates quarter-group spans with the fiscal quarter number (1-4) for header band coloring. */
export function withQuarterColor<K>(
  groups: { key: K; span: number }[],
  infos: (FiscalDisplayInfo | null)[],
): FiscalBandGroup<K>[] {
  const result: FiscalBandGroup<K>[] = [];
  let cursor = 0;
  for (const group of groups) {
    result.push({ ...group, quarterNum: infos[cursor]?.fiscalQuarter ?? 1 });
    cursor += group.span;
  }
  return result;
}

/** Annotates period-group spans with their parent quarter's color and an alternating shade flag
 * that resets at each quarter boundary (mirrors the prototype's `periodBandsV` construction). */
export function withPeriodColor<K>(
  groups: { key: K; span: number }[],
  infos: (FiscalDisplayInfo | null)[],
): FiscalPeriodBandGroup<K>[] {
  const result: FiscalPeriodBandGroup<K>[] = [];
  const notYetSeen = Symbol('not-yet-seen');
  let cursor = 0;
  let indexInQuarter = 0;
  let lastQuarterKey: string | null | typeof notYetSeen = notYetSeen;
  for (const group of groups) {
    const info = infos[cursor];
    const quarterKey = info ? `${info.fiscalYear}-Q${info.fiscalQuarter}` : null;
    indexInQuarter = quarterKey === lastQuarterKey ? indexInQuarter + 1 : 0;
    lastQuarterKey = quarterKey;
    result.push({ ...group, quarterNum: info?.fiscalQuarter ?? 1, altShade: indexInQuarter % 2 === 1 });
    cursor += group.span;
  }
  return result;
}

/** CSS class suffix for a bucket's execution status (see MpsWorkspace.css `.mps-cell--<status>`). */
export function executionStatusClass(status: string): string {
  switch (status) {
    case 'allocating':
    case 'frozen':
    case 'released':
    case 'mixed':
      return status;
    default:
      return 'none';
  }
}

const EXECUTION_STATUS_LABELS: Record<string, string> = {
  allocating: 'Allocating',
  frozen: 'Frozen',
  released: 'Released',
  mixed: 'Mixed (multiple states)',
  none: 'No open supply',
  unknown: 'Unknown',
};

/** Accessible text describing a bucket's full status, for title/aria-label — never color-only. */
export function describeBucket(bucket: MpsBucketDto): string {
  const parts = [EXECUTION_STATUS_LABELS[bucket.executionStatus] ?? bucket.executionStatus];
  if (bucket.containsPlannedWork) parts.push('includes planned work');
  if (bucket.containsExplicitlyScheduledWork) parts.push('explicitly scheduled');
  return `${parts.join(', ')} — ${formatQuantity(bucket.quantity)} units`;
}

/** Full CSS class list for a bucket cell (status fill + planned/explicit markers) — shared by Falldown and weekly cells. */
export function bucketCellClassNames(bucket: MpsBucketDto): string {
  return [
    'mps-cell',
    `mps-cell--${executionStatusClass(bucket.executionStatus)}`,
    bucket.containsPlannedWork ? 'mps-cell--planned' : '',
    bucket.containsExplicitlyScheduledWork ? 'mps-cell--explicit' : '',
  ]
    .filter(Boolean)
    .join(' ');
}

export function formatQuantity(quantity: number | string): string {
  const value = Number(quantity);
  if (Number.isNaN(value)) return String(quantity);
  return value.toLocaleString(undefined, { maximumFractionDigits: 2 });
}

/** Formats an ISO (YYYY-MM-DD) date string as a short label, e.g. "Jun 30". Returns "" for null. */
export function formatWeekLabel(iso: string | null): string {
  if (!iso) return '';
  const [year, month, day] = iso.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

export function parseIsoDateOnly(iso: string): Date {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/**
 * Work Order drill-down (Stage 7R) is only exposed for Falldown plus this many forward weekly
 * buckets (Week 0-3), regardless of the MPS grid's own display horizon. This matches the
 * four-week Work Order planning window and is kept as a single named constant so the horizon can
 * change without restructuring selection/click-handling code. Keep in sync with the backend
 * `Kst.Domain.WorkOrders.WorkOrderPlanningWindow.ForwardWeekCount`.
 */
export const WORK_ORDER_DRILLDOWN_HORIZON_WEEKS = 4;

/** Whether the weekly bucket at this zero-based index (within one part's weekly buckets, i.e.
 * excluding Falldown) exposes the Work Order drill-down action. Falldown is always eligible and
 * is not covered by this helper. */
export function isWeeklyBucketWorkOrderEligible(weeklyIndex: number): boolean {
  return weeklyIndex < WORK_ORDER_DRILLDOWN_HORIZON_WEEKS;
}

/** A selected schedule bucket (Falldown or one weekly bucket) used to scope the Work Orders tab. */
export interface BucketSelection {
  parentPart: string;
  kind: 'falldown' | 'weekly';
  weekLabel: string | null;
}

/** Human-readable label identifying the currently selected schedule context (Falldown or a
 * specific week), so the selection remains visibly identifiable once the Work Orders tab is open. */
export function describeBucketSelection(bucket: BucketSelection): string {
  if (bucket.kind === 'falldown') return 'Falldown';
  return bucket.weekLabel ? `Week of ${formatWeekLabel(bucket.weekLabel)}` : 'Selected week';
}

const WORK_ORDER_STATUS_LABELS: Record<string, string> = {
  allocating: 'Allocating',
  frozen: 'Frozen',
  released: 'Released',
};

/**
 * Accessible semantic label for a Work Order's status. Known codes (A/F/R) arrive as friendly
 * lowercase values and map to a label; any other non-closed raw code (Stage 7R) passes through
 * unchanged so a previously unseen status renders safely rather than being dropped or invented.
 */
export function workOrderStatusLabel(status: string): string {
  return WORK_ORDER_STATUS_LABELS[status] ?? status;
}

const NO_VALUE = '\u2014';

/** Formats an optional ISO date (Release/Due) for a Work Order card; "\u2014" when absent. */
export function formatOptionalDate(value: string | null | undefined): string {
  return value ? formatWeekLabel(value) : NO_VALUE;
}

/** Formats a Kitting % value for display; "N/A" (never "0%") when there are no applicable lines. */
export function formatKittingPercent(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return 'N/A';
  return `${formatQuantity(value)}%`;
}

/** Clamped 0-100 numeric Kitting percent for progress-bar width; null when Kitting is N/A. */
export function kittingPercentValue(value: number | string | null | undefined): number | null {
  if (value === null || value === undefined) return null;
  const numeric = Number(value);
  if (Number.isNaN(numeric)) return null;
  return Math.min(100, Math.max(0, numeric));
}

/** Absolute departure of Issued % from 100; null when Issued % is unknown (defensive — applicable
 * material lines always carry a value since zero-required lines are excluded upstream). */
export function materialLineDeparture(issuedPercent: number | string | null | undefined): number | null {
  if (issuedPercent === null || issuedPercent === undefined) return null;
  const value = Number(issuedPercent);
  return Number.isNaN(value) ? null : Math.abs(value - 100);
}

/** Whether a material line is a variance exception, per the backend's semantic issue-status
 * classification — never recompute the 95/105 thresholds independently on the frontend. */
export function isMaterialLineException(issueStatus: WorkOrderMaterialLineDto['issueStatus']): boolean {
  return issueStatus === 'underIssuedException' || issueStatus === 'overIssuedException';
}

/**
 * Default Stage 7D.8 material-grid sort: larger departures from 100% Issued sort first (which,
 * given the 95/105 exception thresholds, naturally puts every exception ahead of every normal
 * line), then alphabetically by component part, then by original position for true duplicate rows.
 */
export function sortMaterialLines<T extends Pick<WorkOrderMaterialLineDto, 'componentPart' | 'issuedPercent'>>(
  lines: readonly T[],
): T[] {
  return lines
    .map((line, index) => ({ line, index }))
    .sort((a, b) => {
      const departureA = materialLineDeparture(a.line.issuedPercent);
      const departureB = materialLineDeparture(b.line.issuedPercent);
      if (departureA === null && departureB === null) {
        return a.line.componentPart.localeCompare(b.line.componentPart) || a.index - b.index;
      }
      if (departureA === null) return 1;
      if (departureB === null) return -1;
      if (departureA !== departureB) return departureB - departureA;
      return a.line.componentPart.localeCompare(b.line.componentPart) || a.index - b.index;
    })
    .map((entry) => entry.line);
}

/** Case-insensitive partial Part Number filter, scoped to an already-loaded material line list —
 * frontend-local, never re-queries QAD per keystroke. */
export function filterMaterialLinesByPart<T extends Pick<WorkOrderMaterialLineDto, 'componentPart'>>(
  lines: readonly T[],
  query: string,
): T[] {
  const needle = query.trim().toLowerCase();
  if (!needle) return [...lines];
  return lines.filter((line) => line.componentPart.toLowerCase().includes(needle));
}

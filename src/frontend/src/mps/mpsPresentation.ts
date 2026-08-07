import type { MpsBucketDto } from '../api/client';
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

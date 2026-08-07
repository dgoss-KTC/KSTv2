import type { MpsBucketDto } from '../api/client';

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

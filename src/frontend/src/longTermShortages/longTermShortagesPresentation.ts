import type { LongTermShortageScreenRow as LongTermShortageRow } from '../api/longTermShortagesApi';

export interface LongTermShortagesFilters {
  component: string;
  planner: string;
  kssOnly: boolean;
  statusOnly: boolean;
  showAll: boolean;
  sort: 'mostUrgent' | 'firstShortage' | 'deepestShortage' | 'recoveryDate' | 'buyerPlanner' | 'component';
}

export const EMPTY_LONG_TERM_SHORTAGES_FILTERS: LongTermShortagesFilters = {
  component: '', planner: '', kssOnly: false, statusOnly: false, showAll: false, sort: 'mostUrgent',
};

export function isShortage(row: LongTermShortageRow): boolean {
  return row.severity === 'CriticalShort' || row.severity === 'FutureShort' || row.severity === 'SafetyStockShort';
}

/** Filters the loaded projection only; changing filters never fetches or recalculates it. */
export function filterLongTermShortages(
  rows: readonly LongTermShortageRow[],
  filters: LongTermShortagesFilters,
): LongTermShortageRow[] {
  const component = filters.component.trim().toLocaleLowerCase();
  const planner = filters.planner.trim().toLocaleLowerCase();
  return rows
    .filter((row) => filters.showAll || isShortage(row))
    .filter((row) => !component || row.componentPart.toLocaleLowerCase().includes(component))
    .filter((row) => !planner || (row.planner ?? '').toLocaleLowerCase().includes(planner))
    .filter((row) => !filters.kssOnly || row.isKss)
    .filter((row) => !filters.statusOnly || Boolean(row.qadStatus))
    .sort((left, right) => {
      const priority = (severity: string) => severity === 'CriticalShort' ? 0 : severity === 'FutureShort' ? 1 : severity === 'SafetyStockShort' ? 2 : severity === 'SafetyStockUnavailable' ? 3 : 4;
      const first = (row: LongTermShortageRow) => row.firstShortDate ?? row.firstAtRiskDate ?? '9999-12-31';
      const compareQuantity = (leftValue: string, rightValue: string) => {
      const [leftWhole, leftFraction = ''] = leftValue.split('.');
      const [rightWhole, rightFraction = ''] = rightValue.split('.');
        const wholeDifference = leftWhole.length - rightWhole.length || leftWhole.localeCompare(rightWhole);
        return wholeDifference || leftFraction.padEnd(Math.max(leftFraction.length, rightFraction.length), '0')
          .localeCompare(rightFraction.padEnd(Math.max(leftFraction.length, rightFraction.length), '0'));
      };
      const deepest = (row: LongTermShortageRow) => String(row.maximumShortage);
      const recovery = (row: LongTermShortageRow) => row.firstRecoveryDate ?? '9999-12-31';
      const difference = filters.sort === 'mostUrgent' ? priority(left.severity) - priority(right.severity) || first(left).localeCompare(first(right))
        : filters.sort === 'firstShortage' ? first(left).localeCompare(first(right))
        : filters.sort === 'deepestShortage' ? compareQuantity(deepest(right), deepest(left))
        : filters.sort === 'recoveryDate' ? recovery(left).localeCompare(recovery(right))
        : filters.sort === 'buyerPlanner' ? (left.buyerPlannerCode ?? 'ZZZZ').localeCompare(right.buyerPlannerCode ?? 'ZZZZ') : 0;
      return difference || left.componentPart.localeCompare(right.componentPart);
    });
}

/** Display rounding only; raw API decimals remain the projection authority. */
export function formatLongTermQuantity(value: number | string | null | undefined, uom?: string | null): string {
  if (value === null || value === undefined) return '—';
  const decimals = ['BX', 'EA', 'PK'].includes(uom?.trim().toUpperCase() ?? '') ? 0 : 2;
  const match = String(value).match(/^(-?)(\d+)(?:\.(\d+))?$/);
  if (!match) return 'Unknown';
  const [, sign, whole, fraction = ''] = match;
  const kept = fraction.padEnd(decimals + 1, '0');
  const scaled = BigInt(whole) * 10n ** BigInt(decimals) + BigInt(kept.slice(0, decimals) || '0')
    + (kept[decimals] >= '5' ? 1n : 0n);
  if (scaled === 0n) return '0';
  if (decimals === 0) return `${sign}${scaled}`;
  const padded = scaled.toString().padStart(decimals + 1, '0');
  const normalizedFraction = padded.slice(-decimals).replace(/0+$/, '');
  return `${sign}${padded.slice(0, -decimals)}${normalizedFraction ? `.${normalizedFraction}` : ''}`;
}

export function formatLongTermDate(value: string): string {
  const [year, month, day] = value.split('-');
  return year && month && day ? `${month}/${day}` : value;
}

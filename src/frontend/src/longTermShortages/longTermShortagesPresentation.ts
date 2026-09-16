import type { LongTermShortageRow } from '../api/longTermShortagesApi';

export interface LongTermShortagesFilters {
  component: string;
  planner: string;
  kssOnly: boolean;
  statusOnly: boolean;
  showAll: boolean;
}

export const EMPTY_LONG_TERM_SHORTAGES_FILTERS: LongTermShortagesFilters = {
  component: '', planner: '', kssOnly: false, statusOnly: false, showAll: false,
};

export function isShortage(row: LongTermShortageRow): boolean {
  // A selected-site null safety-stock value is not a clean result and must stay visible by default.
  return row.severity === 'CriticalShort' || row.severity === 'SafetyStockShort' || row.severity === 'SafetyStockUnavailable';
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
      const leftWeek = left.firstSafetyStockShortWeek ?? Number.MAX_SAFE_INTEGER;
      const rightWeek = right.firstSafetyStockShortWeek ?? Number.MAX_SAFE_INTEGER;
      return Number(leftWeek) - Number(rightWeek) || left.componentPart.localeCompare(right.componentPart);
    });
}

/** Formats the API-provided Stage 11-A display quantity; the backend owns UOM rounding. */
export function formatLongTermQuantity(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return '—';
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 4 }).format(Number(value));
}

export function formatLongTermDate(value: string): string {
  const [year, month, day] = value.split('-');
  return year && month && day ? `${month}/${day}` : value;
}

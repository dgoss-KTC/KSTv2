import type { components } from '../generated/api';
import type { ColumnFilter, ColumnFacets, FilterColumn, FilterLimits } from '../components/shared/ColumnFilterEditor';
import { decimal, subtract } from '../shared/exactDecimal';

export type FinishedGoodsRow = components['schemas']['FinishedGoodsRowDto'];
export const FINISHED_GOODS_COLUMNS: FilterColumn[] = [
  { id: 'product', label: 'Product', kind: 'text', containsBlanksByDefault: false },
  { id: 'location', label: 'Location', kind: 'text', containsBlanksByDefault: false },
  { id: 'lot', label: 'Lot', kind: 'text', containsBlanksByDefault: false },
  { id: 'onHand', label: 'On Hand', kind: 'decimal' },
  { id: 'value', label: 'Value', kind: 'decimal' },
];
export const FINISHED_GOODS_FILTER_LIMITS: FilterLimits = {
  maximumValues: 1000, maximumTextLength: 256, maximumDecimalDigits: 76, maximumDecimalScale: 76,
};

function filterValue(row: FinishedGoodsRow, column: string): string | null {
  switch (column) {
    case 'product': return row.product;
    case 'location': return row.location ?? null;
    case 'lot': return row.lot ?? null;
    case 'onHand': return row.onHand;
    case 'value': return row.value;
    default: return null;
  }
}

function compareExact(a: string, b: string): number | null {
  const difference = subtract(a, b);
  const n = difference === null ? null : decimal(difference)!.n;
  return n === null ? null : n < 0n ? -1 : n > 0n ? 1 : 0;
}

function matches(row: FinishedGoodsRow, filter: ColumnFilter): boolean {
  const value = filterValue(row, filter.column);
  if (value === null || value === '') return filter.includeBlanks;
  if (filter.kind === 'values') return filter.mode === 'include' ? !!filter.values?.includes(value) : !filter.values?.includes(value);
  if (filter.kind === 'contains') return value.toLowerCase().includes((filter.text ?? '').toLowerCase());
  if (filter.kind === 'compare') {
    const cmp = compareExact(value, filter.value ?? '');
    if (cmp === null) return false;
    return filter.operator === 'eq' ? cmp === 0 : filter.operator === 'gt' ? cmp > 0 : filter.operator === 'lt' ? cmp < 0
      : filter.operator === 'between' && cmp >= 0 && (compareExact(value, filter.upper ?? '') ?? 1) <= 0;
  }
  return false;
}

/** Full loaded population, exact source decimals and the accepted physical-row order. Never deduplicate. */
export function finishedGoodsRows(rows: readonly FinishedGoodsRow[], filters: readonly ColumnFilter[]): FinishedGoodsRow[] {
  const compare = (a: string | null | undefined, b: string | null | undefined) =>
    (a ?? '').localeCompare(b ?? '', 'en', { sensitivity: 'base' }) || (a ?? '').localeCompare(b ?? '', 'en');
  return rows.filter(row => filters.every(filter => matches(row, filter)))
    .sort((a, b) => compare(a.product, b.product) || compare(a.location, b.location) || compare(a.lot, b.lot) ||
      compare(a.reference, b.reference) || compare(a.sourceLocation, b.sourceLocation) || compare(a.rowId, b.rowId));
}

/** Own-column predicate excluded; counts represent the complete loaded physical-row population. */
export function finishedGoodsFacets(rows: readonly FinishedGoodsRow[], filters: readonly ColumnFilter[], column: string,
  search: string, retained: string[]): ColumnFacets {
  const counts = new Map<string, number>(); let blankCount = 0;
  const otherFilters = filters.filter(filter => filter.column !== column);
  for (const row of rows) {
    if (!otherFilters.every(filter => matches(row, filter))) continue;
    const value = filterValue(row, column);
    if (value === null || value === '') blankCount++;
    else counts.set(value, (counts.get(value) ?? 0) + 1);
  }
  const found = [...counts].filter(([value]) => value.toLowerCase().includes(search.toLowerCase()));
  found.sort((a, b) => b[1] - a[1] || (a[0] < b[0] ? -1 : a[0] > b[0] ? 1 : 0));
  const cap = Number(FINISHED_GOODS_FILTER_LIMITS.maximumValues);
  return { values: found.slice(0, cap).map(([value, count]) => ({ value, count })),
    retainedValues: retained.map(value => ({ value, count: counts.get(value) ?? 0 })), blankCount,
    distinctTotal: counts.size, matchingDistinctTotal: found.length, truncated: found.length > cap };
}

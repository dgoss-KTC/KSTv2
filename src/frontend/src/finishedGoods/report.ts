import type { components } from '../generated/api';

export type FinishedGoodsRow = components['schemas']['FinishedGoodsRowDto'];
export interface FinishedGoodsFilters { product: string; location: string; lot: string }
export const emptyFinishedGoodsFilters: FinishedGoodsFilters = { product: '', location: '', lot: '' };

export function finishedGoodsRows(rows: readonly FinishedGoodsRow[], filters: FinishedGoodsFilters): FinishedGoodsRow[] {
  const contains = (value: string | null | undefined, input: string) => input === '' ||
    (value != null && value.toLowerCase().includes(input.toLowerCase()));
  const compare = (a: string | null | undefined, b: string | null | undefined) =>
    (a ?? '').localeCompare(b ?? '', 'en', { sensitivity: 'base' }) || (a ?? '').localeCompare(b ?? '', 'en');
  return rows.filter(row => contains(row.product, filters.product) && contains(row.location, filters.location) && contains(row.lot, filters.lot))
    .sort((a, b) => compare(a.product, b.product) || compare(a.location, b.location) || compare(a.lot, b.lot) ||
      compare(a.reference, b.reference) || compare(a.sourceLocation, b.sourceLocation) || compare(a.rowId, b.rowId));
}

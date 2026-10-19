import { COLUMNS, DATE_COLUMNS, NUMERIC_COLUMNS, compareReportText, emptyFilters, reportRows, type OrderLine } from './report';
import { decimal, subtract } from '../shared/exactDecimal';
import type { ColumnFilter, ColumnFacets, FilterColumn, FilterLimits } from '../components/shared/ColumnFilterEditor';

export const FILTER_LIMITS: FilterLimits = { maximumValues: 1000, maximumTextLength: 256, maximumDecimalDigits: 76, maximumDecimalScale: 76 };
export const FILTER_COLUMNS: FilterColumn[] = [
  ...COLUMNS.map(c => ({ id: c.id, label: c.label,
    kind: DATE_COLUMNS.has(c.id) ? 'date' : NUMERIC_COLUMNS.has(c.id) ? 'decimal' : c.id === 'partials' ? 'values' : 'text',
    ...(c.id === 'partials' ? { valueLabels: { true: 'Yes', false: 'No' } } : {}),
    requireBothDates: c.id === 'dueDate', textRange: c.id === 'productLine', containsBlanksByDefault: false })),
  { id: 'salesperson', label: 'Salesperson', kind: 'text', containsBlanksByDefault: false },
  { id: 'orderQty', label: 'Order Qty', kind: 'decimal' },
  { id: 'price', label: 'Price', kind: 'decimal' },
];

/** Retrieved values only. No proposals, rounded display numbers or new source reads. */
export function filterValue(row: OrderLine, id: string): string | null {
  if (id === 'orderQty') return row.planningValues.orderQty;
  if (id === 'price') return row.planningValues.price;
  if (id === 'salesperson') return row.salesperson;
  if (id === 'line') return String(row.key.line); // SQL int is exactly representable in JS.
  if (['open', 'extPrice', 'unitPrice', 'allocated', 'picked', 'siteQoh'].includes(id)) {
    if (!row.numericText) throw new Error('Exact report values unavailable. Restart the refreshed desktop and reload the report.');
    return row.numericText[id as keyof OrderLine['numericText']];
  }
  const value = COLUMNS.find(c => c.id === id)?.value(row);
  return value === null || value === undefined || value === '' ? null : String(value);
}

function compareExact(a: string, b: string): number | null {
  const difference = subtract(a, b);
  const n = difference === null ? null : decimal(difference)!.n;
  return n === null ? null : n < 0n ? -1 : n > 0n ? 1 : 0;
}

export function matchesColumnFilter(row: OrderLine, f: ColumnFilter): boolean {
  const value = filterValue(row, f.column);
  if (value === null || value === '') return f.kind === 'textRange' || (f.column === 'dueDate' && f.kind === 'dateRange') ? false : f.includeBlanks;
  if (f.kind === 'values') return f.mode === 'include' ? !!f.values?.includes(value) : !f.values?.includes(value);
  if (f.kind === 'contains') return value.toLocaleLowerCase().includes((f.text ?? '').toLocaleLowerCase());
  if (f.kind === 'textRange') return (!f.from || compareReportText(value, f.from) >= 0) && (!f.to || compareReportText(value, f.to) <= 0);
  if (f.kind === 'dateRange') return (!f.from || value >= f.from) && (!f.to || value <= f.to);
  if (f.kind === 'compare') {
    const cmp = compareExact(value, f.value ?? '');
    if (cmp === null) return false;
    return f.operator === 'eq' ? cmp === 0 : f.operator === 'gt' ? cmp > 0 : f.operator === 'lt' ? cmp < 0
      : f.operator === 'between' && cmp >= 0 && (compareExact(value, f.upper ?? '') ?? 1) <= 0;
  }
  return false;
}

export function filteredOrderRows(lines: OrderLine[], filters: ColumnFilter[], search = '', excludeField?: string): OrderLine[] {
  return reportRows(lines, emptyFilters, search).filter(row => filters.every(f => f.column === excludeField || matchesColumnFilter(row, f)));
}

/** Counts cover the complete loaded report, before pages/changed-only. Own-field predicate excluded. */
export function orderFacets(lines: OrderLine[], filters: ColumnFilter[], quickSearch: string, column: string,
  search: string, retained: string[]): ColumnFacets {
  const counts = new Map<string, number>(); let blankCount = 0;
  for (const row of filteredOrderRows(lines, filters, quickSearch, column)) {
    const value = filterValue(row, column);
    if (value === null || value === '') blankCount++;
    else counts.set(value, (counts.get(value) ?? 0) + 1);
  }
  const matches = [...counts].filter(([value]) => value.toLocaleLowerCase().includes(search.toLocaleLowerCase()));
  matches.sort((a, b) => b[1] - a[1] || (a[0] < b[0] ? -1 : a[0] > b[0] ? 1 : 0));
  return { values: matches.slice(0, Number(FILTER_LIMITS.maximumValues)).map(([value, count]) => ({ value, count })),
    retainedValues: retained.map(value => ({ value, count: counts.get(value) ?? 0 })), blankCount,
    distinctTotal: counts.size, matchingDistinctTotal: matches.length, truncated: matches.length > Number(FILTER_LIMITS.maximumValues) };
}

export function filterSummary(f: ColumnFilter): string {
  if (f.kind === 'contains') return `contains “${f.text}”${f.includeBlanks ? ' + blanks' : ''}`;
  if (f.kind === 'compare') return `${f.operator} ${f.value}${f.upper ? ` – ${f.upper}` : ''}${f.includeBlanks ? ' + blanks' : ''}`;
  if (f.kind === 'dateRange' || f.kind === 'textRange') return `${f.from || 'Any'} – ${f.to || 'Any'}`;
  return `${f.mode === 'include' ? 'Include' : 'All except'} ${f.values?.length ?? 0} values${f.includeBlanks ? ' + blanks' : ''}`;
}

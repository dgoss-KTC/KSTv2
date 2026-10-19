import type { HistoricalShipmentsRequest } from '../api/historicalShipmentsApi';
import type { Filters } from './historicalShipmentsPreferences';
import { localIsoDate } from '../shared/localDate';
export type CustomerMode = 'any' | 'corp' | 'range';
export function activeCustomerFilters(filters: Filters, mode: CustomerMode): Filters {
  return { ...filters, corpGroup: mode === 'corp' ? filters.corpGroup : '', customerFrom: mode === 'range' ? filters.customerFrom : '', customerTo: mode === 'range' ? filters.customerTo : '' };
}
export function calendarRange(today: string, days: 30 | 90 | 'year'): { invoiceDateFrom: string; invoiceDateTo: string } {
  const [y, m, d] = today.split('-').map(Number); const from = new Date(y, m - 1, d);
  if (days === 'year') from.setMonth(0, 1); else from.setDate(from.getDate() - days + 1);
  return { invoiceDateFrom: localIsoDate(from), invoiceDateTo: today };
}
export function appliedSummary(query: HistoricalShipmentsRequest): string[] {
  const pair = (label: string, from: string | null, to: string | null) => from || to ? `${label} ${from || 'any'} to ${to || 'any'}` : '';
  return [`Site ${query.site}`, query.corpGroup ? `Corp Group ${query.corpGroup}` : pair('Customers', query.customerFrom, query.customerTo), pair('Items', query.itemFrom, query.itemTo), pair('Product lines', query.productLineFrom, query.productLineTo), pair('Salesperson', query.salespersonFrom, query.salespersonTo), pair('Invoices', query.invoiceDateFrom, query.invoiceDateTo) || 'All invoice history'].filter(Boolean);
}

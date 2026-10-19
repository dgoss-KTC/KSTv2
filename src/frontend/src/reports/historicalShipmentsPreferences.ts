import type { HistoricalShipmentsRequest, HistoricalShipmentsSpec } from '../api/historicalShipmentsApi';

export const hsColumnsKey = 'kst.historicalShipments.columns.v1';
export const hsPageSize = 100;
export const hsPageSizeKey = 'kst.historicalShipments.pageSize.v1';
export const hsPageSizes = [100, 500, 1000] as const;
export function initialHsPageSize(): number {
  try { const raw = localStorage.getItem(hsPageSizeKey); return raw !== null && hsPageSizes.some(n => String(n) === raw) ? Number(raw) : hsPageSize; }
  catch { return hsPageSize; }
}
export type Column = HistoricalShipmentsSpec['columns'][number];

export const blankFilters = {
  site: '', corpGroup: '', customerFrom: '', customerTo: '', itemFrom: '', itemTo: '', productLineFrom: '', productLineTo: '',
  salespersonFrom: '', salespersonTo: '', invoiceDateFrom: '', invoiceDateTo: '',
};
export type Filters = typeof blankFilters;

// Migration fingerprint only: current definitions, labels and order always come from the catalog.
const previousDefaultIds = new Set(['site', 'customerNumber', 'customerName', 'invoiceNumber', 'invoiceDate',
  'itemNumber', 'invoicedQuantity', 'salesOrder', 'salesOrderLine', 'customerPo']);
const queryDefaultIds = new Set(['shipDate', 'dueDate', 'originalCommit', 'requiredDate', 'salesOrder', 'customerPo',
  'salesOrderLine', 'itemNumber', 'invoicedQuantity', 'retroUnitPrice', 'retroExtendedPrice', 'shipTo', 'shipVia',
  'billOfLading', 'invoiceNumber', 'channel', 'onTime']);

/** Defaults are always included; restores recognized optional selections in catalog order. */
export function restoreColumns(spec: HistoricalShipmentsSpec, stored: string | null): string[] {
  const defaults = spec.columns.filter(c => c.defaultSelected).map(c => c.id);
  try {
    const parsed: unknown = JSON.parse(stored ?? 'null');
    if (Array.isArray(parsed)) {
      const ids = new Set(parsed);
      if (ids.size === previousDefaultIds.size && [...ids].every(id => typeof id === 'string' && previousDefaultIds.has(id))) return defaults;
      if (ids.size === queryDefaultIds.size && [...ids].every(id => typeof id === 'string' && queryDefaultIds.has(id))) return defaults;
      const kept = spec.columns.filter(c => c.defaultSelected || ids.has(c.id)).map(c => c.id);
      if (kept.length > 0) return kept;
    }
  } catch { /* corrupt preference: defaults */ }
  return defaults;
}
export function validateFilters(f: Filters): string[] {
  const problems: string[] = [];
  if (!f.site) problems.push('Select a site.');
  if (f.corpGroup.trim() && (f.customerFrom.trim() || f.customerTo.trim())) problems.push('Use either Corp Group or a Customer range, not both.');
  if (f.invoiceDateFrom && f.invoiceDateTo && f.invoiceDateFrom > f.invoiceDateTo) problems.push('Invoice Date To must be on or after Invoice Date From.');
  return problems;
}
export function toRequest(f: Filters, columns: string[]): HistoricalShipmentsRequest {
  const t = (v: string) => v.trim() === '' ? null : v.trim();
  return { site: f.site, corpGroup: t(f.corpGroup), customerFrom: t(f.customerFrom), customerTo: t(f.customerTo),
    itemFrom: t(f.itemFrom), itemTo: t(f.itemTo), productLineFrom: t(f.productLineFrom), productLineTo: t(f.productLineTo),
    salespersonFrom: t(f.salespersonFrom), salespersonTo: t(f.salespersonTo),
    invoiceDateFrom: f.invoiceDateFrom || null, invoiceDateTo: f.invoiceDateTo || null, columns };
}

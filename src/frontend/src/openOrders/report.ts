import type { components } from '../generated/api';

export type OrderLine = components['schemas']['OpenOrderLineDto'];
export type ColumnId = typeof COLUMNS[number]['id'];
export const COLUMNS = [
  { id: 'dueDate', label: 'Due Date', value: (r: OrderLine) => r.sourceValues.dueDate },
  { id: 'order', label: 'SO', value: (r: OrderLine) => r.key.salesOrder },
  { id: 'po', label: 'PO', value: (r: OrderLine) => r.purchaseOrder },
  { id: 'line', label: 'Line', value: (r: OrderLine) => r.key.line },
  { id: 'itemNumber', label: 'Item Number', value: (r: OrderLine) => r.itemNumber },
  { id: 'site', label: 'Site', value: (r: OrderLine) => r.site },
  { id: 'open', label: 'Open', value: (r: OrderLine) => r.open },
  { id: 'stat', label: 'Status', value: (r: OrderLine) => r.stat },
  { id: 'extPrice', label: 'Ext Price', value: (r: OrderLine) => r.extPrice },
  { id: 'allocated', label: 'Allocated', value: (r: OrderLine) => r.allocated },
  { id: 'customer', label: 'Customer #', value: (r: OrderLine) => r.customer },
  { id: 'customerName', label: 'Customer Name', value: (r: OrderLine) => r.customerName },
  { id: 'customerPart', label: 'Customer Part', value: (r: OrderLine) => r.customerPart },
  { id: 'dockDate', label: 'Dock Date', value: (r: OrderLine) => r.sourceValues.dockDate },
  { id: 'ios', label: 'IOS', value: (r: OrderLine) => r.ios },
  { id: 'lineComments', label: 'Line Comments', value: (r: OrderLine) => r.lineComments },
  { id: 'lineHold', label: 'Line Hold', value: (r: OrderLine) => r.lineHold },
  { id: 'partials', label: 'Partials', value: (r: OrderLine) => r.partials },
  { id: 'performDate', label: 'Perform Date', value: (r: OrderLine) => r.sourceValues.performDate },
  { id: 'picked', label: 'Picked', value: (r: OrderLine) => r.picked },
  { id: 'plnr', label: 'Planner', value: (r: OrderLine) => r.plnr },
  { id: 'prodStat', label: 'Prod Stat', value: (r: OrderLine) => r.prodStat },
  { id: 'productLine', label: 'Product Line', value: (r: OrderLine) => r.productLine },
  { id: 'qaHold', label: 'QA Hold', value: (r: OrderLine) => r.qaHold },
  { id: 'remarks', label: 'Remarks', value: (r: OrderLine) => r.remarks },
  { id: 'requiredDate', label: 'Required Date', value: (r: OrderLine) => r.sourceValues.requiredDate },
  { id: 'revision', label: 'Revision', value: (r: OrderLine) => r.revision },
  { id: 'shipAcct', label: 'Ship Acct', value: (r: OrderLine) => r.shipAcct },
  { id: 'shipTo', label: 'Ship To', value: (r: OrderLine) => r.shipTo },
  { id: 'shipVia', label: 'Ship Via', value: (r: OrderLine) => r.shipVia },
  { id: 'siteQoh', label: 'Site QOH', value: (r: OrderLine) => r.siteQoh },
  { id: 'soHoldStatus', label: 'SO Hold Status', value: (r: OrderLine) => r.soHoldStatus },
  { id: 'soType', label: 'SO Type', value: (r: OrderLine) => r.soType },
  { id: 'unitPrice', label: 'Unit Price', value: (r: OrderLine) => r.unitPrice },
] as const;

export const DEFAULT_COLUMNS: ColumnId[] = COLUMNS.slice(0, 9).map(c => c.id);
export interface Layout { order: ColumnId[]; visible: ColumnId[] }
export const defaultLayout = (): Layout => ({ order: COLUMNS.map(c => c.id), visible: [...DEFAULT_COLUMNS] });
const storageKey = (id: string) => `kst.openOrders.layout.v1.${id}`;

export function loadLayout(id: string): Layout {
  try {
    const saved: unknown = JSON.parse(window.localStorage.getItem(storageKey(id)) ?? 'null');
    if (!saved || typeof saved !== 'object') return defaultLayout();
    const record = saved as Record<string, unknown>;
    if (!Array.isArray(record.order) || !Array.isArray(record.visible)) return defaultLayout();
    const valid = (items: unknown[]) => [...new Set(items.filter((item): item is ColumnId =>
      typeof item === 'string' && COLUMNS.some(c => c.id === item)))];
    const order = valid(record.order);
    const visible = valid(record.visible);
    // A saved entry with no known IDs is corrupt/obsolete, not a request to hide everything.
    if (visible.length === 0 && record.visible.length > 0) return defaultLayout();
    return { order: [...order, ...COLUMNS.map(c => c.id).filter(id => !order.includes(id))], visible };
  } catch { return defaultLayout(); }
}

export function saveLayout(id: string, layout: Layout): void {
  window.localStorage.setItem(storageKey(id), JSON.stringify(layout));
}

export interface Filters { customerName: string; customer: string; salesperson: string; so: string; po: string; itemNumber: string; productFrom: string; productTo: string; ios: string; dueFrom: string; dueTo: string }
export const emptyFilters: Filters = { customerName: '', customer: '', salesperson: '', so: '', po: '', itemNumber: '', productFrom: '', productTo: '', ios: '', dueFrom: '', dueTo: '' };

export function compareReportText(a: string | null, b: string | null): number {
  return a === b ? 0 : a === null ? -1 : b === null ? 1 : a.localeCompare(b, 'en', { sensitivity: 'base' });
}

export function formatReportDate(value: string | null | undefined): string {
  if (!value) return '';
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  return match ? `${Number(match[2])}/${Number(match[3])}/${match[1]}` : value;
}

export function formatReportTimestamp(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

export const DATE_COLUMNS: ReadonlySet<ColumnId> = new Set(['dueDate', 'dockDate', 'performDate', 'requiredDate']);
export const NUMERIC_COLUMNS: ReadonlySet<ColumnId> = new Set(['line', 'open', 'extPrice', 'allocated', 'picked', 'siteQoh', 'unitPrice']);

// Owner-amended workspace text filters are literal contains; ranges retain their accepted bounds.
// The date predicate applies only with both boundaries. SQL Server ordering puts NULL first.
export function reportRows(lines: OrderLine[], f: Filters): OrderLine[] {
  const cmp = compareReportText;
  const contains = (value: string | null, expected: string) => !expected ||
    (value !== null && value.toLocaleLowerCase().includes(expected.toLocaleLowerCase()));
  return lines.filter(r =>
    contains(r.customerName, f.customerName) && contains(r.customer, f.customer) &&
    contains(r.salesperson, f.salesperson) && contains(r.ios, f.ios) &&
    contains(r.key.salesOrder, f.so) && contains(r.purchaseOrder, f.po) && contains(r.itemNumber, f.itemNumber) &&
    (!f.productFrom || (r.productLine !== null && cmp(r.productLine, f.productFrom) >= 0)) &&
    (!f.productTo || (r.productLine !== null && cmp(r.productLine, f.productTo) <= 0)) &&
    (!(f.dueFrom && f.dueTo) || (r.sourceValues.dueDate !== null && r.sourceValues.dueDate >= f.dueFrom && r.sourceValues.dueDate <= f.dueTo))
  ).sort((a, b) => cmp(a.customerName, b.customerName) || cmp(a.itemNumber, b.itemNumber) ||
    cmp(a.sourceValues.dueDate, b.sourceValues.dueDate) || cmp(a.key.salesOrder, b.key.salesOrder) || Number(a.key.line) - Number(b.key.line));
}

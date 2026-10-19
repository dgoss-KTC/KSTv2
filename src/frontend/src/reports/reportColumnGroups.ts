import type { Column } from './historicalShipmentsPreferences';
import { oorMandatory, oorOptional } from './openOrderReportPreferences';
export interface PickerColumn { id: string; label: string; locked: boolean; currentMaster?: boolean }
export interface ColumnGroup { label: string; columns: PickerColumn[] }
export const hsGroupIds: Record<string, readonly string[]> = {
  Dates: ['shipDate', 'originalCommit', 'onTime', 'dueDate', 'requiredDate', 'orderDate', 'invoiceDate'],
  'Order and invoice': ['salesOrder', 'salesOrderLine', 'customerPo', 'invoiceNumber', 'channel', 'detailSalesOrder', 'lineType', 'release', 'remarks'],
  'Part and quantity': ['itemNumber', 'invoicedQuantity', 'customerItem', 'description1', 'description2', 'productLine', 'recordedUom'],
  Pricing: ['retroUnitPrice', 'retroExtendedPrice', 'unitPrice', 'extendedPrice', 'currency', 'exchangeRate'],
  'Customer and shipping': ['shipTo', 'shipVia', 'billOfLading', 'site', 'customerNumber', 'customerName', 'shipToName', 'salesperson', 'shippingAccount', 'ncAccount', 'boxes', 'pallets'],
};
const oorGroups: Record<string, readonly string[]> = {
  'Always included': oorMandatory, 'Quantities and price': ['Allocated', 'Picked', 'Site QOH', 'Unit Price'],
  Dates: ['Dock Date', 'Perform Date', 'Required Date'], 'Customer and shipping': ['Customer', 'Customer Name', 'Customer Part', 'Ship Acct', 'Ship To', 'Ship Via'],
  'Part and planning': ['IOS', 'Plnr', 'Prod Stat', 'Product Line', 'Revision'],
  'Holds and notes': ['Line Hold', 'QA Hold', 'SO Hold Status', 'SO Type', 'Partials', 'Line Comments', 'Remarks'],
};
function group(columns: PickerColumn[], ids: Record<string, readonly string[]>): ColumnGroup[] {
  const known = new Set(Object.values(ids).flat());
  return [...Object.entries(ids).map(([label, members]) => ({ label, columns: columns.filter(c => members.includes(c.id)).sort((a, b) => Number(b.locked) - Number(a.locked)) })),
    { label: 'Other', columns: columns.filter(c => !known.has(c.id)) }].filter(g => g.columns.length);
}
export const historicalColumnGroups = (columns: Column[]) => group(columns.map(c => ({ id: c.id, label: c.label, locked: c.defaultSelected, currentMaster: c.provenance.startsWith('Current') })), hsGroupIds);
export const openOrderColumnGroups = () => group([...oorMandatory, ...oorOptional].map(id => ({ id, label: id, locked: oorMandatory.includes(id) })), oorGroups);

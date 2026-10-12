import type { OpenOrderReportRequest } from '../api/openOrderReportApi';

export const oorMandatory = ['Due', 'Order', 'PO', 'Ln', 'Item Number', 'Site', 'Open', 'Stat', 'Ext Price'];
export const oorOptional = ['Allocated', 'Customer', 'Customer Name', 'Customer Part', 'Dock Date', 'IOS', 'Line Comments', 'Line Hold',
  'Partials', 'Perform Date', 'Picked', 'Plnr', 'Prod Stat', 'Product Line', 'QA Hold', 'Remarks', 'Required Date', 'Revision',
  'Ship Acct', 'Ship To', 'Ship Via', 'Site QOH', 'SO Hold Status', 'SO Type', 'Unit Price'];
export const oorPreferenceKey = 'kst.openOrderReport.optionalFields.v1';

export function initialOor(): OpenOrderReportRequest {
  let optionalFields: string[] = [];
  try {
    const stored: unknown = JSON.parse(localStorage.getItem(oorPreferenceKey) ?? '[]');
    if (Array.isArray(stored)) optionalFields = oorOptional.filter(c => stored.includes(c));
  } catch { /* Missing/corrupt preferences start with mandatory columns only. */ }
  return { customerName: '', customerNumber: '', salespersonId: '', productLineFrom: '', productLineTo: '', ios: '',
    dueDateFrom: '', dueDateTo: '', site: '', optionalFields };
}

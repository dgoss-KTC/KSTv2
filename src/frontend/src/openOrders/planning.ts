import type { components } from '../generated/api';
import type { OrderLine } from './report';
import { decimal, equalDecimal, exactText, multiply, subtract } from '../shared/exactDecimal';

// Exact decimal helpers moved to shared/exactDecimal (Stage 14.4); re-exported for existing imports.
export { decimal, equalDecimal, multiply, subtract };

export type Proposal = components['schemas']['OpenOrderProposalDto'];
export type Values = Proposal['proposed'];
export type Editable = keyof Values;
export const EDITABLE: readonly Editable[] = ['dueDate', 'performDate', 'requiredDate', 'dockDate', 'orderQty', 'price'];
export const REASONS = ['Cust/PM', 'Buyers', 'Planning', 'Factory', 'C&R', 'Quality', 'Engineer'] as const;
export const keyOf = (key: Proposal['key']) => JSON.stringify([key.domain, key.salesOrder, key.line]);

// Decimal strings are never converted to JS Number for comparisons or monetary arithmetic.

/** Presentation only: never round or replace the value sent to draft/export validation. */
export function planningNumberDisplay(field: 'orderQty' | 'price', value: string): string {
  const parsed = decimal(value);
  if (!parsed) return value;
  if (field === 'orderQty') return exactText(parsed);
  // Round the visual value only. Focusing does not replace the exact source/proposal value.
  const divisor = 10n ** BigInt(Math.max(0, parsed.scale - 4));
  const absolute = parsed.n < 0n ? -parsed.n : parsed.n;
  const scaled = parsed.scale > 4 ? (absolute + divisor / 2n) / divisor : absolute * 10n ** BigInt(4 - parsed.scale);
  const digits = scaled.toString().padStart(5, '0');
  const sign = parsed.n < 0n && scaled !== 0n ? '-' : '';
  return `${sign}${digits.slice(0, -4)}.${digits.slice(-4)}`;
}
export function validDate(value: string | null): boolean {
  if (value === null) return true;
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return false;
  const year = Number(match[1]); const month = Number(match[2]); const day = Number(match[3]);
  const date = new Date(0);
  date.setUTCFullYear(year, month - 1, day);
  return year >= 1 && date.getUTCFullYear() === year && date.getUTCMonth() + 1 === month && date.getUTCDate() === day;
}

/** Full date text is committed only after calendar validation; null denotes deliberate blank. */
export function parsePlanningDate(text: string): string | null | undefined {
  if (text.trim() === '') return null;
  const match = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec(text.trim());
  if (!match) return undefined;
  const iso = `${match[3]}-${match[1].padStart(2, '0')}-${match[2].padStart(2, '0')}`;
  return validDate(iso) ? iso : undefined;
}
export function valuesOf(row: OrderLine): Values {
  const v = row.planningValues ?? row.sourceValues;
  return { dueDate: v.dueDate, performDate: v.performDate, requiredDate: v.requiredDate, dockDate: v.dockDate,
    orderQty: String(v.orderQty), price: String(v.price) };
}
export function changed(p: Proposal): boolean {
  return EDITABLE.some(field => field === 'price' || field === 'orderQty'
    ? !equalDecimal(p.original[field], p.proposed[field]) : p.original[field] !== p.proposed[field]);
}
export function issues(p: Proposal, row: OrderLine | undefined, stale: boolean): string[] {
  const errors: string[] = [];
  if (stale) errors.push('Report is stale; refresh before planning.');
  if (!row) errors.push('Line missing or no longer open in current report.');
  else {
    const current = valuesOf(row);
    if (p.site !== row.site || p.itemNumber !== row.itemNumber || EDITABLE.some(f => f === 'price' || f === 'orderQty'
      ? !equalDecimal(p.original[f], current[f]) : p.original[f] !== current[f])) errors.push('Original values or source identity changed.');
    const open = subtract(p.proposed.orderQty, row.shippedQtyText ?? String(row.shippedQty));
    if (open !== null && open.startsWith('-')) errors.push('Order Qty cannot be below Shipped Qty.');
  }
  for (const date of ['dueDate', 'performDate', 'requiredDate', 'dockDate'] as const) {
    if (!validDate(p.proposed[date])) errors.push(`Invalid ${date} calendar date.`);
  }
  if (!decimal(p.proposed.orderQty)) errors.push('Order Qty must be plain decimal text.');
  if (!decimal(p.proposed.price)) errors.push('Price must be plain decimal text.');
  if (!p.reasonCode) errors.push('Reason Code required.');
  else if (!(REASONS as readonly string[]).includes(p.reasonCode)) errors.push('Invalid Reason Code.');
  return errors;
}

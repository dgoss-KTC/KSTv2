import type { components } from '../generated/api';
import type { OrderLine } from './report';

export type Proposal = components['schemas']['OpenOrderProposalDto'];
export type Values = Proposal['proposed'];
export type Editable = keyof Values;
export const EDITABLE: readonly Editable[] = ['dueDate', 'performDate', 'requiredDate', 'dockDate', 'orderQty', 'price'];
export const REASONS = ['Cust/PM', 'Buyers', 'Planning', 'Factory', 'C&R', 'Quality', 'Engineer'] as const;
export const keyOf = (key: Proposal['key']) => JSON.stringify([key.domain, key.salesOrder, key.line]);

// Decimal strings are never converted to JS Number for comparisons or monetary arithmetic.
export function decimal(value: string): { n: bigint; scale: number } | null {
  if (!/^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/.test(value)) return null;
  const negative = value.startsWith('-');
  const digits = value.replace(/^[+-]/, '');
  const [whole, fraction = ''] = digits.split('.');
  return { n: BigInt(`${negative ? '-' : ''}${whole || '0'}${fraction}`), scale: fraction.length };
}
function exact(value: { n: bigint; scale: number }): string {
  const sign = value.n < 0n ? '-' : '';
  const digits = (value.n < 0n ? -value.n : value.n).toString().padStart(value.scale + 1, '0');
  if (!value.scale) return sign + digits;
  const fraction = digits.slice(-value.scale).replace(/0+$/, '');
  return `${sign}${digits.slice(0, -value.scale)}${fraction ? `.${fraction}` : ''}`;
}
export function subtract(a: string, b: string): string | null {
  const left = decimal(a); const right = decimal(b);
  if (!left || !right) return null;
  const scale = Math.max(left.scale, right.scale);
  return exact({ n: left.n * 10n ** BigInt(scale - left.scale) - right.n * 10n ** BigInt(scale - right.scale), scale });
}
export function multiply(a: string, b: string): string | null {
  const left = decimal(a); const right = decimal(b);
  return left && right ? exact({ n: left.n * right.n, scale: left.scale + right.scale }) : null;
}
export function equalDecimal(a: string, b: string): boolean {
  return decimal(a) !== null && subtract(a, b) === '0';
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

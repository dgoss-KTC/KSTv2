import { describe, expect, it } from 'vitest';
import { changed, decimal, equalDecimal, issues, multiply, parsePlanningDate, subtract, validDate, type Proposal } from './planning';
import type { OrderLine } from './report';

const original = { dueDate: '2026-10-01', performDate: null, requiredDate: null, dockDate: null,
  orderQty: '2.000', price: '0.0125' };
const proposal: Proposal = { key: { domain: 'D', salesOrder: 'SO-1', line: 1 }, site: 'SW', itemNumber: 'P',
  original, proposed: { ...original, orderQty: '2' }, reasonCode: null };
const row = { key: proposal.key, itemNumber: 'P', site: 'SW', shippedQty: '1.5', shippedQtyText: '1.5', planningValues: original } as OrderLine;

describe('Open Orders planning arithmetic and validation', () => {
  it('handles exact consignment raw prices, equality to shipped and numeric no-ops', () => {
    expect(changed(proposal)).toBe(false);
    const p = { ...proposal, proposed: { ...original, orderQty: '1.5', price: '0.012500000000000000000000001' }, reasonCode: 'Quality' };
    expect(subtract(p.proposed.orderQty, row.shippedQtyText)).toBe('0');
    expect(multiply('0.012500000000000000000000001', '3')).toBe('0.037500000000000000000000003');
    expect(equalDecimal('0.012500', '0.0125')).toBe(true);
    expect(issues(p, row, false)).toEqual([]);
    expect(issues({ ...p, proposed: { ...p.proposed, orderQty: '1.499' } }, row, false)).toContain('Order Qty cannot be below Shipped Qty.');
  });

  it('accepts valid or empty dates and rejects invalid calendar values, grouped/scientific money and missing reasons', () => {
    expect(validDate(null)).toBe(true);
    expect(validDate('2028-02-29')).toBe(true);
    expect(validDate('2027-02-29')).toBe(false);
    expect(decimal('1,000.00')).toBeNull(); expect(decimal('$5')).toBeNull(); expect(decimal('1e3')).toBeNull();
    const p = { ...proposal, proposed: { ...original, dueDate: null, requiredDate: '2027-02-29', price: '$10' } };
    expect(issues(p, row, false)).toEqual(expect.arrayContaining(['Invalid requiredDate calendar date.', 'Price must be plain decimal text.', 'Reason Code required.']));
  });

  it('keeps changed originals for missing and altered source rows', () => {
    const p = { ...proposal, proposed: { ...original, orderQty: '4' }, reasonCode: 'Buyers' };
    expect(issues(p, undefined, false)).toContain('Line missing or no longer open in current report.');
    expect(issues(p, { ...row, planningValues: { ...original, price: '0.02' } }, false)).toContain('Original values or source identity changed.');
    expect(issues(p, row, true)).toContain('Report is stale; refresh before planning.');
  });

  it('parses whole typed dates, deliberate blank and leap days without treating partial or invalid input as blank', () => {
    expect(parsePlanningDate('1/8/2027')).toBe('2027-01-08');
    expect(parsePlanningDate('01/08/2027')).toBe('2027-01-08');
    expect(parsePlanningDate('2/29/2028')).toBe('2028-02-29');
    expect(parsePlanningDate('2/29/2027')).toBeUndefined();
    expect(parsePlanningDate('1/8/')).toBeUndefined();
    expect(parsePlanningDate('')).toBeNull();
    expect(parsePlanningDate('   ')).toBeNull();
    expect(parsePlanningDate('1/1/0099')).toBe('0099-01-01');
    expect(parsePlanningDate('1/1/0000')).toBeUndefined();
  });
});

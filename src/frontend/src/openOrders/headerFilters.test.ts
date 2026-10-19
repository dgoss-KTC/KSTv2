import { describe, expect, it } from 'vitest';
import { FILTER_COLUMNS, filteredOrderRows, filterValue, orderFacets } from './headerFilters';
import type { ColumnFilter } from '../components/shared/ColumnFilterEditor';
import type { OrderLine } from './report';

export function syntheticLine(index: number, overrides: Partial<OrderLine> = {}): OrderLine {
  return { key: { domain: 'TEST', salesOrder: `SO-${String(index).padStart(4, '0')}`, line: 1 },
    itemNumber: `PART-${index}`, site: 'SW', purchaseOrder: 'PO', stat: 'A', shippedQty: 1,
    sourceValues: { dueDate: '2027-01-02', performDate: null, requiredDate: null, dockDate: null, orderQty: 3, price: 1 },
    planningValues: { dueDate: '2027-01-02', performDate: null, requiredDate: null, dockDate: null, orderQty: '3', price: '1' }, shippedQtyText: '1',
    numericText: { open: '2', extPrice: '2', unitPrice: '1', allocated: null, picked: null, siteQoh: null },
    open: 2, extPrice: 2, unitPrice: 1, allocated: null, picked: null, siteQoh: null,
    customer: 'C', customerName: 'Synthetic', salesperson: 'SP', customerPart: null, ios: null,
    lineComments: '', lineHold: null, partials: null, plnr: null, prodStat: null, productLine: 'B',
    qaHold: null, remarks: null, revision: null, shipAcct: null, shipTo: null, shipVia: null,
    soHoldStatus: null, soType: null, consignment: false, ...overrides };
}
const values = (column: string, list: string[], mode = 'include', includeBlanks = false): ColumnFilter =>
  ({ column, kind: 'values', mode, values: list, includeBlanks });

describe('Open Orders full loaded-population header predicates and facets', () => {
  it('finds rows beyond page one, ANDs quick search, preserves existing ordering and independent selected keys', () => {
    const lines = Array.from({ length: 305 }, (_, i) => syntheticLine(i, { itemNumber: 'same', customerPart: i >= 100 ? 'MATCH_%' : null, salesperson: i % 2 ? 'SP2' : 'SP1' }));
    const result = filteredOrderRows(lines, [values('salesperson', ['SP1'])], 'match_%');
    expect(result.map(r => r.key.salesOrder)).toEqual(Array.from({ length: 103 }, (_, i) => `SO-${String(100 + i * 2).padStart(4, '0')}`));
    expect(result.slice(100).map(r => r.key.salesOrder)).toEqual(['SO-0300', 'SO-0302', 'SO-0304']);
  });
  it('counts all eligible rows with quick search/other filters, excludes only its own field, retains zero-count choices', () => {
    const lines = [syntheticLine(1, { customerName: 'keep', salesperson: 'A' }), syntheticLine(2, { customerName: 'keep', salesperson: 'B' }),
      syntheticLine(3, { customerName: 'keep', salesperson: 'B' }), syntheticLine(4, { customerName: 'other', salesperson: 'B' }),
      syntheticLine(5, { customerName: 'keep', salesperson: null }), syntheticLine(6, { customerName: 'keep', salesperson: '' })];
    const filters = [values('salesperson', ['A']), values('site', ['SW'])];
    expect(orderFacets(lines, filters, 'keep', 'salesperson', '', ['A', 'missing'])).toEqual({
      values: [{ value: 'B', count: 2 }, { value: 'A', count: 1 }], retainedValues: [{ value: 'A', count: 1 }, { value: 'missing', count: 0 }],
      blankCount: 2, distinctTotal: 2, matchingDistinctTotal: 2, truncated: false });
    expect(filteredOrderRows(lines, filters, 'keep').map(r => r.key.salesOrder)).toEqual(['SO-0001']);
  });
  it('caps displayed values at 1000, searches the entire population and never excludes hidden values under global-all', () => {
    const lines = Array.from({ length: 1505 }, (_, i) => syntheticLine(i, { itemNumber: `V-${String(i).padStart(4, '0')}` }));
    lines.push(syntheticLine(2000, { itemNumber: '' }), syntheticLine(2001, { itemNumber: '(Blanks)' }), syntheticLine(2002, { itemNumber: ' ' }));
    const facets = orderFacets(lines, [], '', 'itemNumber', '', ['hidden-choice']);
    expect(facets.values).toHaveLength(1000); expect(facets.truncated).toBe(true); expect(facets.matchingDistinctTotal).toBe(1507);
    expect(facets.blankCount).toBe(1); expect(facets.retainedValues).toEqual([{ value: 'hidden-choice', count: 0 }]);
    expect(orderFacets(lines, [], '', 'itemNumber', 'V-1504', []).values).toEqual([{ value: 'V-1504', count: 1 }]);
    expect(filteredOrderRows(lines, [values('itemNumber', [], 'exclude', true)])).toHaveLength(1508);
    expect(filteredOrderRows(lines, [values('itemNumber', ['V-0000'], 'exclude', true)])).toHaveLength(1507);
    expect(filteredOrderRows(lines, [values('itemNumber', ['(Blanks)'])]).map(r => r.key.salesOrder)).toEqual(['SO-2001']);
  });
  it('compares exact decimal text beyond floating-point boundaries, preserving null versus zero and signed values', () => {
    const lines = ['9007199254740993.0000000001', '9007199254740993.0000000002', '-0.00000000000000000001', '0', null]
      .map((allocated, i) => syntheticLine(i, { allocated: allocated === null ? null : Number(allocated),
        numericText: { open: '2', extPrice: '2', unitPrice: '1', allocated, picked: null, siteQoh: null } }));
    const filter: ColumnFilter = { column: 'allocated', kind: 'compare', operator: 'gt', value: '9007199254740993.0000000001', includeBlanks: false };
    expect(filteredOrderRows(lines, [filter]).map(r => r.key.salesOrder)).toEqual(['SO-0001']);
    expect(filteredOrderRows(lines, [{ ...filter, operator: 'between', upper: '9007199254740993.0000000002' }]).map(r => r.key.salesOrder)).toEqual(['SO-0000', 'SO-0001']);
    expect(filteredOrderRows(lines, [{ ...filter, operator: 'lt', value: '0' }]).map(r => r.key.salesOrder)).toEqual(['SO-0002']);
    expect(filteredOrderRows(lines, [{ ...filter, operator: 'eq', value: '-0.0000', includeBlanks: true }]).map(r => r.key.salesOrder)).toEqual(['SO-0003', 'SO-0004']);
  });
  it('retains Product Line text rather than numeric comparison, inclusive Due Date bounds and boolean/blank metadata', () => {
    const lines = [syntheticLine(1, { productLine: '10', partials: true }), syntheticLine(2, { productLine: '2', partials: false }),
      syntheticLine(3, { productLine: 'b', partials: null, sourceValues: { dueDate: null, performDate: null, requiredDate: null, dockDate: null, orderQty: 3, price: 1 } })];
    expect(filteredOrderRows(lines, [{ column: 'productLine', kind: 'textRange', from: '10', to: '2', includeBlanks: false }])).toEqual(lines.slice(0, 2));
    expect(filteredOrderRows(lines, [{ column: 'dueDate', kind: 'dateRange', from: '2027-01-02', to: '2027-01-02', includeBlanks: true }])).toEqual(lines.slice(0, 2));
    expect(filteredOrderRows(lines, [values('partials', ['false'])])).toEqual([lines[1]]);
    expect(FILTER_COLUMNS.find(c => c.id === 'partials')?.valueLabels).toEqual({ true: 'Yes', false: 'No' });
    expect(FILTER_COLUMNS.some(c => ['reasonCode', 'planningStatus'].includes(c.id))).toBe(false);
    expect(filterValue(lines[0], 'orderQty')).toBe('3'); expect(filterValue(lines[0], 'price')).toBe('1');
  });
});

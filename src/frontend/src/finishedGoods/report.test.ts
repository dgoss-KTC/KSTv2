import { describe, expect, it } from 'vitest';
import { finishedGoodsFacets, finishedGoodsRows, type FinishedGoodsRow } from './report';
import type { ColumnFilter } from '../components/shared/ColumnFilterEditor';
import { formatDecimal } from '../shared/exactDecimal';

const fixtureRow = (rowId: string, overrides: Partial<FinishedGoodsRow> = {}): FinishedGoodsRow => ({
  rowId, domain: 'KTC', product: 'P-AbC', sourceLocation: 'ship-West', location: 'SHIP-WEST',
  lot: 'Lot-xy', reference: 'R1', onHand: '3.00001', unitPrice: '1.23456', value: '3.7036923456',
  isMissingPrice: false, isRma: false, ...overrides,
});

const contains = (column: string, text: string): ColumnFilter => ({ column, kind: 'contains', text, includeBlanks: false });
const values = (column: string, entries: string[], includeBlanks = false, mode = 'include'): ColumnFilter => ({ column, kind: 'values', values: entries, includeBlanks, mode });
const numeric = (column: string, operator: string, value: string, upper?: string): ColumnFilter => ({ column, kind: 'compare', operator, value, upper, includeBlanks: false });

describe('Finished Goods full-snapshot header predicates and facets', () => {
  it('combines mixed-case partial predicates with AND, preserves blanks and treats wildcard characters literally', () => {
    const rows = [fixtureRow('a'), fixtureRow('b', { location: 'DOCK' }), fixtureRow('c', { lot: null }),
      fixtureRow('d', { product: 'P-%_[x]*', lot: '' })];
    expect(finishedGoodsRows(rows, [contains('product', 'ab'), contains('location', 'ship'), contains('lot', 'XY')]).map(r => r.rowId)).toEqual(['a']);
    expect(finishedGoodsRows(rows, [])).toHaveLength(4);
    expect(finishedGoodsRows(rows, [contains('product', 'ab'), contains('location', 'ship')]).map(r => r.rowId)).toEqual(['c', 'a']);
    expect(finishedGoodsRows(rows, [contains('product', '%_[x]*')]).map(r => r.rowId)).toEqual(['d']);
    expect(finishedGoodsRows(rows, [contains('product', ' ')])).toEqual([]);
  });
  it('sorts without mutation or collapsing duplicate-looking rows', () => {
    const rows = [fixtureRow('2', { reference: 'R2' }), fixtureRow('1'), fixtureRow('3')];
    expect(finishedGoodsRows(rows, []).map(r => r.rowId)).toEqual(['1', '3', '2']);
    expect(rows[0].rowId).toBe('2');
  });

  it('matches exact case-sensitive values, keeps blanks distinct from literal blank captions and supports exclusion', () => {
    const rows = [fixtureRow('null', { lot: null }), fixtureRow('empty', { lot: '' }),
      fixtureRow('caption', { lot: '(Blanks)' }), fixtureRow('upper', { lot: 'A' }), fixtureRow('lower', { lot: 'a' })];
    expect(finishedGoodsRows(rows, [values('lot', [], true)]).map(r => r.rowId)).toEqual(['empty', 'null']);
    expect(finishedGoodsRows(rows, [values('lot', ['(Blanks)'])]).map(r => r.rowId)).toEqual(['caption']);
    expect(finishedGoodsRows(rows, [values('lot', ['A'])]).map(r => r.rowId)).toEqual(['upper']);
    expect(finishedGoodsRows(rows, [values('lot', ['A'], false, 'exclude')]).map(r => r.rowId)).toEqual(['caption', 'lower']);
    expect(finishedGoodsRows(rows, [{ ...contains('lot', 'A'), includeBlanks: true }])).toHaveLength(5);
  });

  it.each(['onHand', 'value'])('compares %s as exact source text even with identical rounded/Number values', field => {
    const a = '9007199254740992.001'; const b = '9007199254740992.002';
    const rows = [fixtureRow('a', { [field]: a }), fixtureRow('b', { [field]: b }), fixtureRow('negative', { [field]: '-1' })];
    expect(formatDecimal(a, 2)).toBe(formatDecimal(b, 2));
    expect(finishedGoodsRows(rows, [numeric(field, 'eq', `${a}00`)]).map(r => r.rowId)).toEqual(['a']);
    expect(finishedGoodsRows(rows, [numeric(field, 'gt', a)]).map(r => r.rowId)).toEqual(['b']);
    expect(finishedGoodsRows(rows, [numeric(field, 'lt', a)]).map(r => r.rowId)).toEqual(['negative']);
    expect(finishedGoodsRows(rows, [numeric(field, 'between', a, b)]).map(r => r.rowId)).toEqual(['a', 'b']);
    expect(finishedGoodsRows(rows, [numeric(field, 'eq', 'bad')])).toEqual([]);
  });

  it('cascades counts across the complete physical population, excludes only its own predicate and retains zero counts', () => {
    const rows = Array.from({ length: 1205 }, (_, i) => fixtureRow(String(i), { product: i < 1200 ? 'P' : 'Q', lot: i % 2 ? '' : null, location: i < 100 ? 'DOCK' : 'SHIP' }));
    rows.push(fixtureRow('caption', { product: 'P', lot: '(Blanks)', location: 'SHIP' }));
    const filters = [values('product', ['P']), values('location', ['SHIP'])];
    expect(finishedGoodsFacets(rows, filters, 'product', '', ['gone'])).toEqual({
      values: [{ value: 'P', count: 1101 }, { value: 'Q', count: 5 }], retainedValues: [{ value: 'gone', count: 0 }],
      blankCount: 0, distinctTotal: 2, matchingDistinctTotal: 2, truncated: false,
    });
    expect(finishedGoodsFacets(rows, filters, 'lot', '', [])).toMatchObject({ blankCount: 1100, values: [{ value: '(Blanks)', count: 1 }] });
    expect(finishedGoodsFacets(rows, [...filters, numeric('onHand', 'gt', '4')], 'lot', '', [])).toMatchObject({ blankCount: 0, values: [] });
  });

  it('caps frequent values deterministically, searches literally, retains hidden selections and never caps actual filtering', () => {
    const rows = Array.from({ length: 1205 }, (_, i) => fixtureRow(String(i), { product: `P${String(i).padStart(4, '0')}` }));
    rows.push(fixtureRow('literal', { product: '%_[x]*' }));
    const facets = finishedGoodsFacets(rows, [], 'product', '', ['P1204', 'gone']);
    expect(facets).toMatchObject({ truncated: true, distinctTotal: 1206, matchingDistinctTotal: 1206,
      retainedValues: [{ value: 'P1204', count: 1 }, { value: 'gone', count: 0 }] });
    expect(facets.values).toHaveLength(1000);
    expect(finishedGoodsFacets(rows, [], 'product', '%_[x]*', []).values).toEqual([{ value: '%_[x]*', count: 1 }]);
    expect(finishedGoodsRows(rows, [values('product', [], true, 'exclude')])).toHaveLength(1206);
    expect(finishedGoodsRows(rows, [values('product', ['P1204'])]).map(r => r.rowId)).toEqual(['1204']);
  });
});

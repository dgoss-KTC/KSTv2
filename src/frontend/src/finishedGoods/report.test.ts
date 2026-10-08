import { describe, expect, it } from 'vitest';
import { emptyFinishedGoodsFilters, finishedGoodsRows, type FinishedGoodsRow } from './report';

const fixtureRow = (rowId: string, overrides: Partial<FinishedGoodsRow> = {}): FinishedGoodsRow => ({
  rowId, domain: 'KTC', product: 'P-AbC', sourceLocation: 'ship-West', location: 'SHIP-WEST',
  lot: 'Lot-xy', reference: 'R1', onHand: '3.00001', unitPrice: '1.23456', value: '3.7036923456',
  isMissingPrice: false, isRma: false, ...overrides,
});

describe('Finished Goods literal snapshot filters', () => {
  it('combines mixed-case partial predicates with AND, preserves blanks and treats wildcard characters literally', () => {
    const rows = [fixtureRow('a'), fixtureRow('b', { location: 'DOCK' }), fixtureRow('c', { lot: null }),
      fixtureRow('d', { product: 'P-%_[x]*', lot: '' })];
    expect(finishedGoodsRows(rows, { product: 'ab', location: 'ship', lot: 'XY' }).map(r => r.rowId)).toEqual(['a']);
    expect(finishedGoodsRows(rows, emptyFinishedGoodsFilters)).toHaveLength(4);
    expect(finishedGoodsRows(rows, { product: 'ab', location: 'ship', lot: '' }).map(r => r.rowId)).toEqual(['c', 'a']);
    expect(finishedGoodsRows(rows, { product: '%_[x]*', location: '', lot: '' }).map(r => r.rowId)).toEqual(['d']);
    expect(finishedGoodsRows(rows, { product: ' ', location: '', lot: '' })).toEqual([]);
  });
  it('sorts without mutation or collapsing duplicate-looking rows', () => {
    const rows = [fixtureRow('2', { reference: 'R2' }), fixtureRow('1'), fixtureRow('3')];
    expect(finishedGoodsRows(rows, emptyFinishedGoodsFilters).map(r => r.rowId)).toEqual(['1', '3', '2']);
    expect(rows[0].rowId).toBe('2');
  });
});

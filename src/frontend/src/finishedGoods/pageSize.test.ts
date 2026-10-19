import { afterEach, describe, expect, it, vi } from 'vitest';
import { preferredFinishedGoodsPageSize, storeFinishedGoodsPageSize, type FinishedGoodsPageSize } from './pageSize';

const key = 'kst.finishedGoods.pageSize.v1';
afterEach(() => { vi.restoreAllMocks(); localStorage.clear(); });
describe('guarded Finished Goods-only page-size preference', () => {
  it.each([100, 500, 1000] as const)('loads and stores %s without touching filters or other preferences', size => {
    localStorage.setItem('kst.workspaceShipments.pageSize.v1', '500');
    localStorage.setItem('kst.openOrders.pageSize.v1', '1000');
    storeFinishedGoodsPageSize(size);
    expect(localStorage.getItem(key)).toBe(String(size));
    expect(preferredFinishedGoodsPageSize()).toBe(size);
    expect(localStorage.getItem('kst.workspaceShipments.pageSize.v1')).toBe('500');
    expect(localStorage.getItem('kst.openOrders.pageSize.v1')).toBe('1000');
    expect(localStorage.length).toBe(3);
  });
  it.each([null, '', '0', '200', '500.0', ' 500', '1e3', 'Infinity', '{}'])('defaults invalid storage %s to 100', stored => {
    if (stored !== null) localStorage.setItem(key, stored);
    expect(preferredFinishedGoodsPageSize()).toBe(100);
  });
  it('guards unavailable reads and writes, and rejects unsupported writes', () => {
    const get = vi.spyOn(localStorage, 'getItem').mockImplementation(() => { throw new Error('unavailable'); });
    const set = vi.spyOn(localStorage, 'setItem').mockImplementation(() => { throw new Error('unavailable'); });
    expect(preferredFinishedGoodsPageSize()).toBe(100); expect(get).toHaveBeenCalled();
    expect(() => storeFinishedGoodsPageSize(500)).not.toThrow(); expect(set).toHaveBeenCalledWith(key, '500');
    set.mockClear(); storeFinishedGoodsPageSize(200 as FinishedGoodsPageSize); expect(set).not.toHaveBeenCalled();
  });
});

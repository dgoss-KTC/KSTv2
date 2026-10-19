import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OPEN_ORDERS_PAGE_SIZE_KEY, loadOpenOrdersPageSize, saveOpenOrdersPageSize } from './pageSize';

beforeEach(() => localStorage.clear());
afterEach(() => vi.restoreAllMocks());
describe('Open Orders optional global page-size preference', () => {
  it.each([100, 500, 1000] as const)('round-trips the validated %s size without touching Shipments or views', size => {
    localStorage.setItem('kst.workspaceShipments.pageSize.v1', '500');
    localStorage.setItem('kst.openOrders.views.v1.A.report', 'existing-view');
    saveOpenOrdersPageSize(size);
    expect(loadOpenOrdersPageSize()).toBe(size);
    expect(localStorage.getItem(OPEN_ORDERS_PAGE_SIZE_KEY)).toBe(String(size));
    expect(localStorage.getItem('kst.workspaceShipments.pageSize.v1')).toBe('500');
    expect(localStorage.getItem('kst.openOrders.views.v1.A.report')).toBe('existing-view');
  });
  it.each([null, '', '0', '200', '999', '1001', '-100', '1e3', '500.0', ' 500 ', 'null', '{"size":500}'])('falls back to 100 for invalid/missing %s', value => {
    if (value !== null) localStorage.setItem(OPEN_ORDERS_PAGE_SIZE_KEY, value);
    expect(loadOpenOrdersPageSize()).toBe(100);
  });
  it('tolerates unavailable read/write storage and never persists an unsupported runtime value', () => {
    const get = vi.spyOn(localStorage, 'getItem').mockImplementation(() => { throw new Error('Unavailable'); });
    const set = vi.spyOn(localStorage, 'setItem').mockImplementation(() => { throw new Error('Unavailable'); });
    expect(loadOpenOrdersPageSize()).toBe(100);
    expect(get).toHaveBeenCalledWith(OPEN_ORDERS_PAGE_SIZE_KEY);
    expect(() => saveOpenOrdersPageSize(1000)).not.toThrow();
    expect(set).toHaveBeenCalledWith(OPEN_ORDERS_PAGE_SIZE_KEY, '1000');
    set.mockClear();
    saveOpenOrdersPageSize(200 as 100);
    expect(set).not.toHaveBeenCalled();
    get.mockRestore();
  });
});

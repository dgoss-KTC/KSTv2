export const OPEN_ORDERS_PAGE_SIZES = [100, 500, 1000] as const;
export type OpenOrdersPageSize = typeof OPEN_ORDERS_PAGE_SIZES[number];
export const OPEN_ORDERS_PAGE_SIZE_KEY = 'kst.openOrders.pageSize.v1';

/** One optional display preference, independent of assignment, mode, views and Shipments. */
export function loadOpenOrdersPageSize(): OpenOrdersPageSize {
  try {
    const value = localStorage.getItem(OPEN_ORDERS_PAGE_SIZE_KEY);
    return value === '500' ? 500 : value === '1000' ? 1000 : 100;
  } catch { return 100; }
}

export function saveOpenOrdersPageSize(size: OpenOrdersPageSize): void {
  if (!OPEN_ORDERS_PAGE_SIZES.includes(size)) return;
  try { localStorage.setItem(OPEN_ORDERS_PAGE_SIZE_KEY, String(size)); } catch { /* optional display preference */ }
}

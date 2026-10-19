export const FINISHED_GOODS_PAGE_SIZES = [100, 500, 1000] as const;
export type FinishedGoodsPageSize = typeof FINISHED_GOODS_PAGE_SIZES[number];
const key = 'kst.finishedGoods.pageSize.v1';

export function preferredFinishedGoodsPageSize(): FinishedGoodsPageSize {
  try {
    const stored = localStorage.getItem(key);
    return FINISHED_GOODS_PAGE_SIZES.find(size => String(size) === stored) ?? 100;
  } catch { return 100; }
}

export function storeFinishedGoodsPageSize(size: FinishedGoodsPageSize) {
  if (!FINISHED_GOODS_PAGE_SIZES.includes(size)) return;
  try { localStorage.setItem(key, String(size)); } catch { /* Optional display preference; session remains usable. */ }
}

import { useCallback, type RefObject } from 'react';
import { ColumnFilterEditor, type ColumnFilter } from '../components/shared/ColumnFilterEditor';
import { FINISHED_GOODS_COLUMNS, FINISHED_GOODS_FILTER_LIMITS, finishedGoodsFacets, type FinishedGoodsRow } from './report';

/** Local adapter only: no Shipments operations, disk views or additional source acquisition. */
export function FinishedGoodsFilter({ field, rows, filters, anchor, onClose, onApply }: {
  field: string; rows: FinishedGoodsRow[]; filters: ColumnFilter[]; anchor: RefObject<HTMLElement | null>;
  onClose: () => void; onApply: (filter: ColumnFilter | null) => void;
}) {
  const loadFacets = useCallback(async (search: string, retained: string[], signal: AbortSignal) => {
    signal.throwIfAborted();
    return finishedGoodsFacets(rows, filters, field, search, retained);
  }, [rows, filters, field]);
  return <ColumnFilterEditor column={FINISHED_GOODS_COLUMNS.find(c => c.id === field)!} anchor={anchor}
    filter={filters.find(f => f.column === field)} spec={FINISHED_GOODS_FILTER_LIMITS} loadFacets={loadFacets}
    onClose={onClose} onApply={onApply} />;
}

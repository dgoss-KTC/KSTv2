import { useCallback, useRef, useState, type RefObject } from 'react';
import { AnchoredPopover } from '../components/shared/AnchoredPopover';
import { ColumnFilterEditor, type ColumnFilter } from '../components/shared/ColumnFilterEditor';
import { FILTER_COLUMNS, FILTER_LIMITS, filterSummary, orderFacets } from './headerFilters';
import type { OrderLine } from './report';

export function OrderFilterEditor({ field, lines, filters, search, anchor, onClose, onApply }: {
  field: string; lines: OrderLine[]; filters: ColumnFilter[]; search: string;
  anchor: RefObject<HTMLElement | null>; onClose: () => void; onApply: (filter: ColumnFilter | null) => void;
}) {
  const loadFacets = useCallback(async (query: string, retained: string[], signal: AbortSignal) => {
    signal.throwIfAborted();
    return orderFacets(lines, filters, search, field, query, retained);
  }, [lines, filters, search, field]);
  const column = FILTER_COLUMNS.find(c => c.id === field)!;
  const unavailableReason = column.kind === 'decimal' && field !== 'line' && field !== 'price' && field !== 'orderQty' && lines.some(r => !r.numericText)
    ? 'Exact report values unavailable. Restart the refreshed desktop and reload the report.' : undefined;
  return <ColumnFilterEditor column={{ ...column, unavailableReason }} anchor={anchor}
    filter={filters.find(f => f.column === field)} spec={FILTER_LIMITS} loadFacets={loadFacets}
    onClose={onClose} onApply={onApply} />;
}

export function OrderFiltersMenu({ filters, visible, onEdit, onClear }: {
  filters: ColumnFilter[]; visible: readonly string[];
  onEdit: (field: string, anchor: HTMLElement) => void; onClear: () => void;
}) {
  const [open, setOpen] = useState(false); const [find, setFind] = useState('');
  const anchor = useRef<HTMLButtonElement>(null);
  return <>
    <button ref={anchor} type="button" aria-haspopup="dialog" aria-expanded={open} onClick={() => setOpen(!open)}>Filters{filters.length ? ` (${filters.length})` : ''}</button>
    {open && <AnchoredPopover title="Open Orders filters" anchor={anchor} onClose={() => setOpen(false)}>
      <div className="shipment-filter"><p>All fields, including hidden columns. Filters survive column/view changes.</p>
        <label>Find a field<input type="search" value={find} onChange={e => setFind(e.target.value)} /></label>
        <div className="shipment-filter__list">{[...FILTER_COLUMNS].sort((a, b) => Number(filters.some(f => f.column === b.id)) - Number(filters.some(f => f.column === a.id)))
          .filter(c => c.label.toLocaleLowerCase().includes(find.toLocaleLowerCase())).map(c => {
            const filter = filters.find(f => f.column === c.id);
            return <button className="orders-filter-menu__field" type="button" key={c.id} onClick={() => { setOpen(false); onEdit(c.id, anchor.current!); }}>
              {c.label}{!visible.includes(c.id) ? ' (hidden / filter-only)' : ''}{filter ? ` • ${filterSummary(filter)}` : ''}
            </button>;
          })}</div>
        <button type="button" disabled={!filters.length} onClick={onClear}>Clear column filters (keep quick search)</button>
        <button type="button" onClick={() => setOpen(false)}>Done</button>
      </div>
    </AnchoredPopover>}
  </>;
}

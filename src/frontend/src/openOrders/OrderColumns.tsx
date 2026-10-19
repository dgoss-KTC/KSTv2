import { useRef, useState } from 'react';
import { PLAN_REQUIRED, type PlanColumnId } from './planLayout';
import { OrdersPopover } from './OrdersPopover';
import { columnLabel, showColumn, viewColumns, viewDefault, type ViewMode } from './views';
import type { useOrderViews } from './useOrderViews';
import { useColumnDrag } from './useColumnDrag';
import { ColumnDragPreview } from './ColumnDragPreview';

export function OrderColumns({ views, mode, move, announce }: { views: ReturnType<typeof useOrderViews>; mode: ViewMode;
  move: (source: PlanColumnId, target: PlanColumnId) => void; announce: (text: string) => void }) {
  const [open, setOpen] = useState<'views' | 'columns' | null>(null);
  const [find, setFind] = useState('');
  const [name, setName] = useState('');
  const [naming, setNaming] = useState(false);
  const [destination, setDestination] = useState<string | null>(null);
  const viewTrigger = useRef<HTMLButtonElement>(null); const columnTrigger = useRef<HTMLButtonElement>(null);
  const visible = views.layout.order.filter(id => views.layout.visible.includes(id));
  const columnDrag = useColumnDrag(visible, JSON.stringify([mode, open, find, visible]), move, 'y');
  const matches = (id: PlanColumnId) => columnLabel(id).toLowerCase().includes(find.toLowerCase());
  function switchView(name: string) {
    if (name === views.active) return;
    if (views.modified) { setDestination(name); setName(views.active === 'Default' ? '' : views.active); return; }
    views.select(name);
  }
  function nudge(id: PlanColumnId, offset: number) {
    const index = visible.indexOf(id); const target = visible[index + offset];
    if (target) move(id, target);
  }
  return <div className="open-orders__view-controls">
    <ColumnDragPreview feedback={columnDrag.feedback} />
    <button ref={viewTrigger} type="button" aria-haspopup="dialog" aria-expanded={open === 'views'} onClick={() => setOpen(open === 'views' ? null : 'views')}>View: {views.active}{views.modified ? ' •' : ''} ▾</button>
    <button ref={columnTrigger} type="button" aria-haspopup="dialog" aria-expanded={open === 'columns'} onClick={() => setOpen(open === 'columns' ? null : 'columns')}>Columns {visible.length} of {viewColumns(mode).length}</button>
    {open && <OrdersPopover title={open === 'views' ? 'Named views' : 'Columns and views'} anchor={open === 'views' ? viewTrigger : columnTrigger} onClose={() => { setOpen(null); setDestination(null); }}>
      <div className="orders-view-tabs">{views.views.map(view => <button type="button" key={view.name} aria-pressed={views.active === view.name} onClick={() => switchView(view.name)}>{view.name}</button>)}
        <button type="button" onClick={() => { setNaming(true); setName(''); }}>New view</button></div>
      {views.modified && <div className="orders-view-modified"><span>{views.active} has unsaved changes.</span>
        {views.active !== 'Default' && <button type="button" onClick={() => views.save(views.active)}>Update view</button>}
        <button type="button" onClick={() => { setNaming(true); setName(''); }}>Save as new</button>
        <button type="button" onClick={views.discard}>Discard changes</button></div>}
      {destination && <div role="group" aria-label="Unsaved view decision"><p>Save or discard the modified view before switching to {destination}?</p>
        <label>View name<input value={name} maxLength={60} onChange={e => setName(e.target.value)} /></label>
        <button type="button" onClick={() => { if (views.save(name, destination)) { setDestination(null); setNaming(false); announce('View saved and switched.'); } }}>Save view</button>
        <button type="button" onClick={() => { views.select(destination); setDestination(null); }}>Discard and switch</button>
        <button type="button" onClick={() => setDestination(null)}>Cancel switch</button></div>}
      {naming && !destination && <form onSubmit={e => { e.preventDefault(); if (views.save(name)) setNaming(false); }}>
        <label>View name<input autoFocus value={name} maxLength={60} onChange={e => setName(e.target.value)} /></label>
        <button type="submit">Save view</button><button type="button" onClick={() => setNaming(false)}>Cancel</button></form>}
      {views.error && <p role="alert">{views.error}</p>}
      {open === 'columns' && <>
        <label>Find a column<input type="search" value={find} onChange={e => setFind(e.target.value)} /></label>
        <p>Shown · drag or use Alt+↑ / Alt+↓</p>
        <div className="orders-column-list" data-column-drag-surface="columns" aria-label="Shown columns">{visible.filter(matches).map(id => <div key={id} data-order-column={id} draggable={false} tabIndex={0} className="orders-column-row"
          data-drop={columnDrag.feedback?.target === id || undefined} data-drag={columnDrag.feedback?.source === id || undefined}
          aria-label={`${columnLabel(id)} column`} onKeyDown={e => { if (e.altKey && ['ArrowUp', 'ArrowDown'].includes(e.key)) { e.preventDefault(); nudge(id, e.key === 'ArrowUp' ? -1 : 1); } }}
          onPointerDown={e => columnDrag.start(e, id)} onDragStart={e => e.preventDefault()}>
          <span aria-hidden="true">⠿</span><span>{columnLabel(id)}</span>
          <button type="button" aria-label={`Move ${columnLabel(id)} up`} disabled={visible[0] === id} onClick={() => nudge(id, -1)}>↑</button>
          <button type="button" aria-label={`Move ${columnLabel(id)} down`} disabled={visible[visible.length - 1] === id} onClick={() => nudge(id, 1)}>↓</button>
          {mode === 'plan' && PLAN_REQUIRED.includes(id) ? <span title="Required in Plan mode" aria-label="Required">🔒</span> : <button type="button" aria-label={`Hide ${columnLabel(id)}`} disabled={visible.length === 1} onClick={() => views.change({ ...views.layout, visible: views.layout.visible.filter(c => c !== id) })}>−</button>}
        </div>)}</div>
        <p>Hidden · {views.layout.order.length - visible.length}</p>
        <div className="orders-column-list" aria-label="Hidden columns">{views.layout.order.filter(id => !visible.includes(id) && matches(id)).map(id => <div key={id} className="orders-column-row"><span>{columnLabel(id)}</span><button type="button" aria-label={`Show ${columnLabel(id)}`} onClick={() => views.change(showColumn(views.layout, id))}>+</button></div>)}</div>
        <button type="button" onClick={() => views.change(viewDefault(mode))}>Reset columns to default</button>
      </>}
      <button type="button" onClick={() => setOpen(null)}>Done</button>
    </OrdersPopover>}
  </div>;
}

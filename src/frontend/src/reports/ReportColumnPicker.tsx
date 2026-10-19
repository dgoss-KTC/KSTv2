import { useRef, useState } from 'react';
import { AnchoredPopover } from '../components/shared/AnchoredPopover';
import type { ColumnGroup } from './reportColumnGroups';
import './ReportColumnPicker.css';

export function ReportColumnPicker({ groups, selected, onChange, onReset, note, disabled }: {
  groups: ColumnGroup[]; selected: string[]; onChange: (ids: string[]) => void; onReset: () => void; note?: string; disabled?: boolean;
}) {
  const anchor = useRef<HTMLButtonElement>(null); const [open, setOpen] = useState(false); const [search, setSearch] = useState('');
  const columns = groups.flatMap(g => g.columns);
  const checked = (id: string) => columns.some(c => c.id === id && c.locked) || selected.includes(id);
  const choose = (ids: string[], include: boolean) => onChange(columns.filter(c => c.locked || (ids.includes(c.id) ? include : selected.includes(c.id))).map(c => c.id));
  return <><button ref={anchor} type="button" className="report-button" disabled={disabled} aria-haspopup="dialog" aria-expanded={open} onClick={() => { setSearch(''); setOpen(v => !v); }}>Columns {columns.filter(c => checked(c.id)).length} of {columns.length}</button>
    {open && <AnchoredPopover title="Columns" anchor={anchor} onClose={() => setOpen(false)}><div className="report-columns">
      <input aria-label="Find a column" placeholder="Find a column" value={search} onChange={e => setSearch(e.target.value)} />
      <div className="report-columns__list">{groups.map(g => {
        const visible = g.columns.filter(c => c.label.toLocaleLowerCase().includes(search.toLocaleLowerCase()));
        if (!visible.length) return null;
        const optional = g.columns.filter(c => !c.locked).map(c => c.id);
        return <section key={g.label} aria-label={`${g.label} columns`}><header><strong>{g.label} <span>{g.columns.filter(c => checked(c.id)).length} of {g.columns.length}</span></strong>{optional.length > 0 && <div>
          <button type="button" aria-label={`All ${g.label} columns`} onClick={() => choose(optional, true)}>All</button><button type="button" aria-label={`None ${g.label} columns`} onClick={() => choose(optional, false)}>None</button></div>}</header>
          {visible.map(c => <label key={c.id}><input type="checkbox" aria-label={c.label} disabled={c.locked || disabled} checked={checked(c.id)} onChange={e => choose([c.id], e.target.checked)} /><span>{c.label}</span>{c.locked ? <small>Always included <span aria-hidden="true">🔒</span></small> : c.currentMaster && <small title="Current master information (may be unavailable)">Current master</small>}</label>)}</section>;
      })}</div><footer><button type="button" disabled={disabled} onClick={onReset}>Reset to defaults</button>{note && <small>{note}</small>}<button type="button" className="report-button report-button--primary" onClick={() => setOpen(false)}>Done</button></footer>
    </div></AnchoredPopover>}</>;
}

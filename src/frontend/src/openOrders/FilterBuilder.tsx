import { useRef, useState, type FormEvent } from 'react';
import { compareReportText, type Filters } from './report';

type FilterType = 'customerName' | 'customer' | 'salesperson' | 'so' | 'po' | 'itemNumber' | 'productLine' | 'ios' | 'dueDate';
type Draft = { first: string; second: string };
const TYPES: ReadonlyArray<{ id: FilterType; label: string }> = [
  { id: 'customerName', label: 'Customer Name contains' },
  { id: 'customer', label: 'Customer # contains' },
  { id: 'salesperson', label: 'Salesperson contains' },
  { id: 'so', label: 'SO contains' },
  { id: 'po', label: 'PO contains' },
  { id: 'itemNumber', label: 'Item Number contains' },
  { id: 'productLine', label: 'Product Line range' },
  { id: 'ios', label: 'IOS contains' },
  { id: 'dueDate', label: 'Due Date range' },
];

function fromFilters(filters: Filters, type: FilterType): Draft {
  if (type === 'productLine') return { first: filters.productFrom, second: filters.productTo };
  if (type === 'dueDate') return { first: filters.dueFrom, second: filters.dueTo };
  return { first: filters[type], second: '' };
}

function valueLabel(type: FilterType, draft: Draft): string {
  if (type === 'productLine') return `${draft.first || 'Any'} – ${draft.second || 'Any'}`;
  if (type === 'dueDate') return `${draft.first} – ${draft.second}`;
  return draft.first;
}

function active(type: FilterType, draft: Draft): boolean {
  return type === 'dueDate' ? !!draft.first && !!draft.second
    : type === 'productLine' ? !!draft.first || !!draft.second : !!draft.first;
}

function validDate(value: string): boolean {
  const parts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!parts) return false;
  const year = Number(parts[1]); const month = Number(parts[2]); const day = Number(parts[3]);
  const date = new Date(0); date.setFullYear(year, month - 1, day);
  return date.getFullYear() === year && date.getMonth() + 1 === month && date.getDate() === day;
}

export function FilterBuilder({ filters, onChange }: { filters: Filters; onChange: (filters: Filters) => void }) {
  const [type, setType] = useState<FilterType>('customerName');
  const [draft, setDraft] = useState<Draft>(() => fromFilters(filters, 'customerName'));
  const [error, setError] = useState('');
  const formRef = useRef<HTMLFormElement>(null);
  const selected = TYPES.find(item => item.id === type)!;

  const select = (next: FilterType) => { setType(next); setDraft(fromFilters(filters, next)); setError(''); };
  const apply = (event: FormEvent) => {
    event.preventDefault();
    const first = draft.first.trim();
    const second = draft.second.trim();
    if (type === 'dueDate' && (!first || !second)) { setError('Enter both Due Date boundaries before applying.'); return; }
    if (type === 'dueDate' && (!validDate(first) || !validDate(second))) { setError('Enter valid calendar dates.'); return; }
    if (!first && !second) { setError('Enter a filter value before applying.'); return; }
    if (first && second && (type === 'dueDate' ? first > second : type === 'productLine' && compareReportText(first, second) > 0)) {
      setError('From must be on or before To.'); return;
    }
    if (type === 'productLine') onChange({ ...filters, productFrom: first, productTo: second });
    else if (type === 'dueDate') onChange({ ...filters, dueFrom: first, dueTo: second });
    else onChange({ ...filters, [type]: first });
    setDraft({ first, second });
    setError('');
  };
  const remove = (item: FilterType) => {
    if (item === 'productLine') onChange({ ...filters, productFrom: '', productTo: '' });
    else if (item === 'dueDate') onChange({ ...filters, dueFrom: '', dueTo: '' });
    else onChange({ ...filters, [item]: '' });
    if (item === type) setDraft({ first: '', second: '' });
    setError('');
  };

  return <div className="open-orders__filter-area">
    <form ref={formRef} className="open-orders__filter-builder" onSubmit={apply} aria-label="Add or edit report filter">
      <label className="open-orders__filter-field">Filter
        <select value={type} onChange={event => select(event.target.value as FilterType)}>
          {TYPES.map(item => <option key={item.id} value={item.id}>{item.label}{active(item.id, fromFilters(filters, item.id)) ? ' (applied)' : ''}</option>)}
        </select>
      </label>
      {type === 'productLine' || type === 'dueDate' ? <>
        <label className="open-orders__filter-field">{selected.label} from
          <input type={type === 'dueDate' ? 'date' : 'text'} value={draft.first} onChange={event => setDraft({ ...draft, first: event.target.value })} />
        </label>
        <label className="open-orders__filter-field">{selected.label} to
          <input type={type === 'dueDate' ? 'date' : 'text'} value={draft.second} onChange={event => setDraft({ ...draft, second: event.target.value })} />
        </label>
      </> : <label className="open-orders__filter-field">{selected.label} value
        <input type="search" value={draft.first} onChange={event => setDraft({ ...draft, first: event.target.value })} />
      </label>}
      <button type="submit" className="open-orders__filter-add" aria-label={`Apply ${selected.label} filter`} title="Apply filter">+</button>
    </form>
    {error && <p className="open-orders__filter-error" role="alert">{error}</p>}
    <div className="open-orders__chips" aria-label="Applied filters">
      {TYPES.filter(item => active(item.id, fromFilters(filters, item.id))).map(item => <span key={item.id} className="open-orders__chip">
        <button type="button" className="open-orders__chip-value" onClick={() => { select(item.id); requestAnimationFrame(() => formRef.current?.querySelector('input')?.focus()); }} aria-label={`Edit ${item.label} filter: ${valueLabel(item.id, fromFilters(filters, item.id))}`}>
          {item.label}: {valueLabel(item.id, fromFilters(filters, item.id))}
        </button>
        <button type="button" className="open-orders__chip-remove" onClick={() => remove(item.id)} aria-label={`Remove ${item.label} filter`}>−</button>
      </span>)}
    </div>
  </div>;
}

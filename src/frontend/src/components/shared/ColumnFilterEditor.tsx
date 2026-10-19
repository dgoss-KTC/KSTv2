import { useEffect, useRef, useState, type RefObject } from 'react';
import type { components } from '../../generated/api';
import { decimal, subtract } from '../../shared/exactDecimal';
import { localIsoDate } from '../../shared/localDate';
import { useLocalBusinessDate } from '../../hooks/useLocalBusinessDate';
import { AnchoredPopover } from './AnchoredPopover';
import './ColumnFilterEditor.css';

// Reuse the generated predicate shape for presentation only. Open Orders evaluates it locally;
// it never sends Shipments filters/operation requests or frontend cell values to an export endpoint.
export type ColumnFilter = components['schemas']['ShipmentColumnFilterDto'];
export type ColumnFacets = Pick<components['schemas']['WorkspaceShipmentsFacetsDto'],
  'values' | 'retainedValues' | 'blankCount' | 'distinctTotal' | 'matchingDistinctTotal' | 'truncated'>;
export type FilterColumn = { id: string; label: string; kind: string; filterKind?: string | null;
  requireBothDates?: boolean; textRange?: boolean; valueLabels?: Record<string, string>;
  blankLabel?: string; valueHelp?: string; shipmentShortcuts?: boolean;
  containsBlanksByDefault?: boolean; unavailableReason?: string };
export type FilterLimits = Pick<components['schemas']['WorkspaceShipmentsSpecDto'],
  'maximumValues' | 'maximumTextLength' | 'maximumDecimalDigits' | 'maximumDecimalScale'>;
type Props = { column: FilterColumn; anchor: RefObject<HTMLElement | null>; filter?: ColumnFilter;
  loadFacets: (search: string, retained: string[], signal: AbortSignal) => Promise<ColumnFacets>;
  spec: FilterLimits; onClose: () => void; onApply: (filter: ColumnFilter | null) => void };

function ValuesCheckbox({ checked, mixed, ...props }: { checked: boolean; mixed?: boolean; onChange: () => void; label: string }) {
  const input = useRef<HTMLInputElement>(null);
  useEffect(() => { if (input.current) input.current.indeterminate = !!mixed; }, [mixed]);
  return <label className="shipment-filter__value"><input ref={input} type="checkbox" checked={checked} aria-checked={mixed ? 'mixed' : checked} onChange={props.onChange} />{props.label}</label>;
}

export function ColumnFilterEditor({ column, anchor, filter, loadFacets, spec, onClose, onApply }: Props) {
  const kind = column.filterKind ?? (column.id === 'onTime' ? 'values' : column.kind);
  const [mode, setMode] = useState(filter?.kind === 'contains' ? 'contains' : filter?.kind === 'textRange' ? 'range' : 'values');
  const [selectionMode, setSelectionMode] = useState(filter?.mode ?? 'exclude');
  const [values, setValues] = useState<string[]>(filter?.values ?? []);
  const [blanks, setBlanks] = useState(filter?.includeBlanks ?? true);
  const [text, setText] = useState(filter?.text ?? '');
  const [search, setSearch] = useState('');
  const [operator, setOperator] = useState(filter?.operator ?? 'eq');
  const [value, setValue] = useState(filter?.value ?? '');
  const [upper, setUpper] = useState(filter?.upper ?? '');
  const [from, setFrom] = useState(filter?.from ?? '');
  const [to, setTo] = useState(filter?.to ?? '');
  const [loadedFacets, setLoadedFacets] = useState<{ query: string; result: ColumnFacets } | null>(null);
  const facetQuery = mode === 'contains' ? text : search;
  const facets = loadedFacets?.query === facetQuery ? loadedFacets.result : null;
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [selectionError, setSelectionError] = useState('');
  const businessDate = useLocalBusinessDate();
  // Selection changes don't re-query. Retained selections are locally merged with returned counts;
  // a new search also asks for every current exception, including hidden/zero-count values.
  const retained = useRef(values); useEffect(() => { retained.current = values; }, [values]);
  const facetSeq = useRef(0);
  useEffect(() => {
    if ((kind !== 'text' && kind !== 'values') || mode === 'range') return;
    const controller = new AbortController(); const seq = ++facetSeq.current;
    const timer = setTimeout(() => {
      setBusy(true); setFailed(false);
      loadFacets(mode === 'contains' ? text : search, retained.current, controller.signal)
        .then(result => {
          if (controller.signal.aborted || seq !== facetSeq.current) return;
          setLoadedFacets({ query: mode === 'contains' ? text : search, result });
        }).catch(() => { if (!controller.signal.aborted && seq === facetSeq.current) setFailed(true); })
        .finally(() => { if (!controller.signal.aborted && seq === facetSeq.current) setBusy(false); });
    }, 200);
    return () => { controller.abort(); clearTimeout(timer); };
  }, [loadFacets, kind, search, text, mode, attempt]);

  const checked = (v: string) => selectionMode === 'include' ? values.includes(v) : !values.includes(v);
  const toggle = (v: string) => {
    const next = values.includes(v) ? values.filter(x => x !== v) : [...values, v];
    if (next.length > Number(spec.maximumValues)) { setSelectionError(`At most ${spec.maximumValues} value exceptions. Use Contains or refine another column.`); return; }
    setSelectionError(''); setValues(next);
  };
  const countMap = new Map([...(facets?.retainedValues ?? []), ...(facets?.values ?? [])].map(v => [v.value, v.count]));
  const visible = facets?.values ?? [];
  const hidden = values.filter(v => !visible.some(x => x.value === v));
  const all = selectionMode === 'exclude' && values.length === 0 && blanks;
  const none = selectionMode === 'include' && values.length === 0 && !blanks;
  const selectedVisible = visible.filter(v => checked(v.value)).length;
  function bulkVisible() {
    // Intentionally limited to displayed results. Global all is represented by exclude [], not by
    // sending 1000 values and pretending unseen matches were included.
    const select = selectedVisible !== visible.length;
    const next = new Set(values);
    for (const item of visible) {
      if ((selectionMode === 'include') === select) next.add(item.value); else next.delete(item.value);
    }
    if (next.size > Number(spec.maximumValues)) { setSelectionError(`At most ${spec.maximumValues} value exceptions. Clear the search and use Select all for the full population.`); return; }
    setSelectionError(''); setValues([...next]);
  }
  function quickRange(range: string) {
    const [y, m, d] = businessDate.split('-').map(Number); const end = new Date(y, m - 1, d); let start = new Date(end);
    if (range === 'Last 7 days') start.setDate(start.getDate() - 6);
    else if (range === 'This month') start = new Date(y, m - 1, 1);
    else { start = new Date(y, m - 2, 1); end.setDate(0); }
    setFrom(localIsoDate(start)); setTo(localIsoDate(end));
  }
  function validNumber(v: string) {
    return v.length <= 80 && v.replace(/[^0-9]/g, '').length <= Number(spec.maximumDecimalDigits) && (v.split('.')[1]?.length ?? 0) <= Number(spec.maximumDecimalScale) && decimal(v) !== null;
  }
  let invalid = '';
  if (kind === 'decimal') {
    if (!validNumber(value) || (operator === 'between' && !validNumber(upper))) invalid = 'Enter a supported exact decimal (no grouping or exponent).';
    else if (operator === 'between' && (decimal(subtract(value, upper)!)?.n ?? 0n) > 0n) invalid = 'Range start must not exceed end.';
  } else if (kind === 'date' && column.requireBothDates && (!from || !to)) invalid = 'Enter both Due Date boundaries before applying.';
  else if (kind === 'date' && ((from && !validCalendarDate(from)) || (to && !validCalendarDate(to)))) invalid = 'Enter valid calendar dates.';
  else if (kind === 'date' && from && to && from > to) invalid = 'From must not exceed To.';
  else if (mode === 'range' && !from.trim() && !to.trim()) invalid = 'Enter a filter value before applying.';
  else if (mode === 'range' && from.trim() && to.trim() && from.trim().localeCompare(to.trim(), 'en', { sensitivity: 'base' }) > 0) invalid = 'From must be on or before To.';
  else if (kind === 'text' && mode === 'contains' && !text) invalid = 'Enter text to match.';
  else if ((kind === 'values' || (kind === 'text' && mode === 'values')) && none) invalid = 'Select at least one value.';
  if (column.unavailableReason) invalid = column.unavailableReason;
  function apply() {
    if (invalid || selectionError) return;
    const base = { column: column.id, includeBlanks: blanks };
    if (mode === 'range') onApply({ ...base, kind: 'textRange', from: from.trim() || null, to: to.trim() || null, includeBlanks: false });
    else if (kind === 'decimal') onApply({ ...base, kind: 'compare', operator, value, upper: operator === 'between' ? upper : null });
    else if (kind === 'date') onApply({ ...base, kind: 'dateRange', from: from || null, to: to || null, includeBlanks: column.requireBothDates ? false : blanks });
    else if (kind === 'text' && mode === 'contains') onApply({ ...base, kind: 'contains', text });
    else onApply(all ? null : { ...base, kind: 'values', mode: selectionMode, values });
  }
  return <AnchoredPopover title={`Filter ${column.label}`} anchor={anchor} onClose={onClose}>
    <form className="shipment-filter" onSubmit={e => { e.preventDefault(); apply(); }}>
      {kind === 'text' && <div role="group" aria-label="Filter mode" className="shipment-filter__modes">{['values', 'contains', ...(column.textRange ? ['range'] : [])].map(m => <button key={m} type="button" aria-pressed={mode === m} onClick={() => { if (m === 'contains' && mode !== m && column.containsBlanksByDefault === false) setBlanks(false); setMode(m); }}>{m === 'values' ? 'Values' : m === 'contains' ? 'Contains' : 'Range'}</button>)}</div>}
      {(kind === 'values' || (kind === 'text' && mode === 'values')) && <>
        {kind === 'text' && <label>Search values<input autoComplete="off" maxLength={Number(spec.maximumTextLength)} value={search} onChange={e => setSearch(e.target.value)} /></label>}
        <ValuesCheckbox label="(Select all)" checked={all} mixed={!all && !none} onChange={() => { setSelectionMode(all ? 'include' : 'exclude'); setValues([]); setBlanks(!all); setSelectionError(''); }} />
        {search && <ValuesCheckbox label="Select displayed search results only" checked={visible.length > 0 && selectedVisible === visible.length} mixed={selectedVisible > 0 && selectedVisible < visible.length} onChange={bulkVisible} />}
        <div className="shipment-filter__list" role="group" aria-label={`${column.label} values`}>
          {(kind === 'values' ? Object.keys(column.valueLabels ?? {}).map(value => ({ value, count: countMap.get(value) ?? 0 })) : visible).map(item => <div key={item.value} className="shipment-filter__value-row"><ValuesCheckbox checked={checked(item.value)} label={column.valueLabels?.[item.value] ?? `Value: ${item.value}`} onChange={() => toggle(item.value)} /><span>{Number(item.count).toLocaleString()}</span></div>)}
          {kind !== 'values' && hidden.length > 0 && <><strong>Retained selections / exceptions</strong>{hidden.map(v => <div key={v} className="shipment-filter__value-row"><ValuesCheckbox checked={checked(v)} label={`Value: ${v}`} onChange={() => toggle(v)} /><span>{Number(countMap.get(v) ?? 0).toLocaleString()}</span></div>)}</>}
          <div className="shipment-filter__value-row"><ValuesCheckbox checked={blanks} label={column.blankLabel ?? '(Blanks)'} onChange={() => setBlanks(!blanks)} /><span>{Number(facets?.blankCount ?? 0).toLocaleString()}</span></div>
        </div>
        {facets?.truncated && <p>Showing the {spec.maximumValues.toLocaleString()} most frequent of {Number(facets.matchingDistinctTotal).toLocaleString()} matching values. Search to narrow. Select all includes the full eligible population.</p>}
        {column.valueHelp && <p>{column.valueHelp}</p>}
      </>}
      {kind === 'text' && mode === 'contains' && <><label>{column.label} contains<input value={text} maxLength={Number(spec.maximumTextLength)} onChange={e => setText(e.target.value)} /></label><p>Case-insensitive literal match; % and _ are ordinary characters. {facets && `${Number(facets.matchingDistinctTotal).toLocaleString()} of ${Number(facets.distinctTotal).toLocaleString()} values match.`}</p><ValuesCheckbox label="Include blank values" checked={blanks} onChange={() => setBlanks(!blanks)} /></>}
      {kind === 'decimal' && <>
        <label>Condition<select value={operator} onChange={e => setOperator(e.target.value)}><option value="eq">Equals</option><option value="gt">Is greater than</option><option value="lt">Is less than</option><option value="between">Between</option></select></label>
        <label>Value<input inputMode="decimal" value={value} maxLength={80} onChange={e => setValue(e.target.value)} /></label>
        {operator === 'between' && <label>Upper value<input inputMode="decimal" maxLength={80} value={upper} onChange={e => setUpper(e.target.value)} /></label>}
        {column.shipmentShortcuts && <div className="shipment-filter__modes">{[['lt', 'Returns only (below 0)'], ['gt', 'Shipments only (above 0)']].map(([op, label]) => <button key={op} type="button" onClick={() => { setOperator(op); setValue('0'); setBlanks(false); }}>{label}</button>)}</div>}
        <ValuesCheckbox label="Include blank values" checked={blanks} onChange={() => setBlanks(!blanks)} /><p>Compared as exact decimals, without rounding.</p>
      </>}
      {mode === 'range' && <><label>From<input value={from} onChange={e => setFrom(e.target.value)} /></label><label>To<input value={to} onChange={e => setTo(e.target.value)} /></label><p>Inclusive text comparison (English, case-insensitive), not numeric. Either bound may be empty; blank product lines do not match.</p></>}
      {kind === 'date' && <><label>From<input type="date" value={from} onChange={e => setFrom(e.target.value)} /></label><label>To<input type="date" value={to} onChange={e => setTo(e.target.value)} /></label>{column.shipmentShortcuts && <div className="shipment-filter__modes">{['Last 7 days', 'This month', 'Last month'].map(r => <button key={r} type="button" onClick={() => quickRange(r)}>{r}</button>)}</div>}{!column.requireBothDates && <ValuesCheckbox label="Include blank dates" checked={blanks} onChange={() => setBlanks(!blanks)} />}<p>{column.requireBothDates ? 'Inclusive retrieved Due Dates. Both boundaries are required; blank dates do not match.' : 'Inclusive local calendar dates; either bound may be empty.'}</p></>}
      {mode !== 'range' && (busy || ((kind === 'text' || kind === 'values') && !facets && !failed)) && <p role="status">Loading values…</p>}
      {failed && <p role="alert">Values could not be loaded. {kind === 'text' && 'Contains still works.'} <button type="button" onClick={() => setAttempt(n => n + 1)}>Retry</button></p>}
      {(invalid || selectionError) && <p role="alert">{invalid || selectionError}</p>}
      <footer><button type="button" onClick={() => onApply(null)}>Clear filter</button><button type="button" onClick={onClose}>Cancel</button><button type="submit" disabled={!!invalid || !!selectionError}>Apply</button></footer>
    </form>
  </AnchoredPopover>;
}

function validCalendarDate(value: string): boolean {
  const parts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!parts) return false;
  const [, y, m, d] = parts; const date = new Date(0);
  date.setFullYear(Number(y), Number(m) - 1, Number(d));
  return date.getFullYear() === Number(y) && date.getMonth() + 1 === Number(m) && date.getDate() === Number(d);
}

import { useEffect, useRef, useState } from 'react';
import { getHistoricalShipmentsSpec, HistoricalShipmentsClient, type HistoricalShipmentsOperation, type HistoricalShipmentsPage, type HistoricalShipmentsSpec, type HistoricalShipmentsRequest } from '../api/historicalShipmentsApi';
import { blankFilters, hsColumnsKey, hsPageSizeKey, hsPageSizes, initialHsPageSize, restoreColumns, toRequest, validateFilters, type Filters } from './historicalShipmentsPreferences';
import { HistoricalReportExport } from './reportExportAdapters';
import { AnalyticalExcelExport } from '../components/AnalyticalExcelExport';
import { useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';
import { ReportHead, ReportInfo, Segmented } from './ReportChrome';
import { ReportColumnPicker } from './ReportColumnPicker';
import { historicalColumnGroups } from './reportColumnGroups';
import { useLocalBusinessDate } from '../hooks/useLocalBusinessDate';
import { activeCustomerFilters, calendarRange, appliedSummary, type CustomerMode } from './historicalShipmentsView';

const provenanceTitle = (p: string) => p.startsWith('Current') ? 'Current master information (may be unavailable)' : 'Recorded on the invoice';
const createDefaultClient = () => new HistoricalShipmentsClient();
function failureText(error: unknown, fallback: string) { return (error instanceof Error ? error.message : typeof error === 'string' ? error : '').trim() || fallback; }
function safeGet() { try { return localStorage.getItem(hsColumnsKey); } catch { return null; } }

export function HistoricalShipmentsReport({ active, onBusyChange, createClient = createDefaultClient, loadSpec = getHistoricalShipmentsSpec }: {
  active: boolean; onBusyChange?: (busy: boolean) => void;
  createClient?: () => HistoricalShipmentsClient; loadSpec?: typeof getHistoricalShipmentsSpec;
}) {
  const [spec, setSpec] = useState<HistoricalShipmentsSpec | null>(null); const [specError, setSpecError] = useState(''); const [specAttempt, setSpecAttempt] = useState(0);
  const [filters, setFilters] = useState<Filters>(blankFilters); const [mode, setMode] = useState<CustomerMode>('any'); const [selected, setSelected] = useState<string[]>([]);
  const [applied, setApplied] = useState<HistoricalShipmentsRequest | null>(null);
  const [operation, setOperation] = useState<HistoricalShipmentsOperation | null>(null); const [page, setPage] = useState<HistoricalShipmentsPage | null>(null);
  const [pageIndex, setPageIndex] = useState(0); const [pageSize, setPageSize] = useState(initialHsPageSize); const size = useRef(pageSize);
  const [pageBusy, setPageBusy] = useState(false); const [acquiring, setAcquiring] = useState(false); const [collapsed, setCollapsed] = useState(false);
  const [message, setMessage] = useState(''); const [error, setError] = useState(''); const [warning, setWarning] = useState('');
  const client = useRef<HistoricalShipmentsClient | null>(null); const abort = useRef<AbortController | null>(null); const token = useRef(0); const pageSeq = useRef(0);
  const firstField = useRef<HTMLSelectElement>(null); const queryPanel = useRef<HTMLFormElement>(null); const today = useLocalBusinessDate();
  const status = useAnalyticalExportStatus(); const saveBusy = status.operations.some(o => o.pending || o.opening); const exportPreparing = !!status.preparation?.preparing;
  useEffect(() => { onBusyChange?.(saveBusy); }, [saveBusy, onBusyChange]);
  function reset(note = '') {
    token.current++; pageSeq.current++; abort.current?.abort(); abort.current = null;
    const old = client.current; client.current = null; void old?.release().catch(() => {});
    setOperation(null); setApplied(null); setPage(null); setPageIndex(0); setAcquiring(false); setPageBusy(false); setCollapsed(false); setError(''); setMessage(note);
  }
  useEffect(() => {
    const controller = new AbortController(); let live = true;
    loadSpec(controller.signal).then(value => { if (live) { setSpec(value); setSpecError(''); setSelected(restoreColumns(value, safeGet())); } })
      .catch(() => { if (live) setSpecError('The report options could not be loaded. Check that the application is running and try again.'); });
    return () => { live = false; controller.abort(); };
  }, [loadSpec, specAttempt]);
  useEffect(() => () => { token.current++; pageSeq.current++; abort.current?.abort(); const old = client.current; client.current = null; void old?.release().catch(() => {}); }, []);
  const departed = useRef(false);
  useEffect(() => {
    if (!active && !departed.current) { departed.current = true; reset('Report results were released when you left this screen. Display again to reacquire.'); }
    if (active) departed.current = false;
  }, [active]);
  useEffect(() => { if (!collapsed && operation) firstField.current?.focus(); }, [collapsed, operation]);
  const effectiveFilters = activeCustomerFilters(filters, mode);
  const problems = validateFilters(effectiveFilters); const draft = toRequest(effectiveFilters, selected);
  const changed = applied !== null && JSON.stringify(draft) !== JSON.stringify(applied);
  const labels = new Map((spec?.columns ?? []).map(c => [c.id, c] as const)); const effective = operation?.columns ?? [];
  const monetarySelected = effective.some(id => labels.get(id)?.monetary);
  const disabledDisplay = !active || acquiring || pageBusy || saveBusy || exportPreparing || problems.length > 0 || !selected.length;
  function changeColumns(next: string[]) {
    if (!spec) return;
    const ordered = spec.columns.filter(c => c.defaultSelected || next.includes(c.id)).map(c => c.id); setSelected(ordered);
    try { localStorage.setItem(hsColumnsKey, JSON.stringify(ordered)); setWarning(''); } catch { setWarning('Column selections could not be remembered; the session choice is retained.'); }
  }
  async function display() {
    if (disabledDisplay) return;
    const request = draft; reset(); const mine = token.current; const controller = new AbortController(); abort.current = controller;
    setAcquiring(true); setMessage('Retrieving invoice history…');
    try {
      const c = createClient(); client.current = c;
      const op = await c.acquire(request, controller.signal);
      if (token.current !== mine) return;
      const first = await c.page(0, size.current, controller.signal);
      if (token.current !== mine) return;
      if (first.operationId !== op.operationId || Number(first.offset) !== 0 || first.rows.length > size.current || JSON.stringify(first.columns) !== JSON.stringify(op.columns)) throw new Error('Historical Shipments page identity mismatch.');
      setOperation(op); setApplied(request); setPage(first); setPageIndex(0); setCollapsed(true); setMessage(op.rowCount === 0 ? 'No invoiced shipments matched these filters.' : `${Number(op.rowCount).toLocaleString()} rows retrieved.`);
    } catch (failure) { if (token.current === mine && !controller.signal.aborted) { const text = failureText(failure, 'The report could not be retrieved.'); reset(); setError(text); } }
    finally { if (token.current === mine) setAcquiring(false); }
  }
  async function goTo(index: number, requestedSize = pageSize) {
    if (!operation || pageBusy || acquiring || exportPreparing || saveBusy || !client.current) return;
    const mine = token.current; const seq = ++pageSeq.current; setPageBusy(true); setError('');
    const total = Math.max(1, Math.ceil(Number(operation.rowCount) / requestedSize)); const target = Math.max(0, Math.min(index, total - 1));
    try {
      const next = await client.current.page(target * requestedSize, requestedSize);
      if (token.current !== mine || pageSeq.current !== seq) return;
      if (next.operationId !== operation.operationId || Number(next.offset) !== target * requestedSize || next.rows.length > requestedSize || JSON.stringify(next.columns) !== JSON.stringify(operation.columns)) throw new Error('Historical Shipments page identity mismatch.');
      setPage(next); setPageIndex(target); setPageSize(requestedSize); size.current = requestedSize;
      try { localStorage.setItem(hsPageSizeKey, String(requestedSize)); } catch { setWarning('Page size could not be remembered; the session choice is retained.'); }
      setMessage(`Page ${target + 1} of ${total}`);
    } catch (failure) { if (token.current === mine && pageSeq.current === seq) setError(failureText(failure, 'The page could not be loaded.')); }
    finally { if (token.current === mine && pageSeq.current === seq) setPageBusy(false); }
  }
  function resize(value: number) {
    if (!hsPageSizes.some(n => n === value) || value > Number(spec?.maximumPageSize ?? 0)) return;
    if (operation) void goTo(Math.floor(pageIndex * pageSize / value), value);
    else { setPageSize(value); size.current = value; try { localStorage.setItem(hsPageSizeKey, String(value)); } catch { setWarning('Page size could not be remembered; the session choice is retained.'); } }
  }
  const set = (key: keyof Filters) => (e: { target: { value: string } }) => setFilters({ ...filters, [key]: e.target.value });
  const range = (from: keyof Filters, to: keyof Filters, label: string) => <div className="hs-range" role="group" aria-label={label}><span>{label}</span><input aria-label={`${label} From`} value={filters[from]} maxLength={256} onChange={set(from)} /><span aria-hidden="true">to</span><input aria-label={`${label} To`} value={filters[to]} maxLength={256} onChange={set(to)} /></div>;
  const totalPages = Math.max(1, Math.ceil(Number(operation?.rowCount ?? 0) / pageSize));
  const dateClash = !!filters.invoiceDateFrom && !!filters.invoiceDateTo && filters.invoiceDateFrom > filters.invoiceDateTo;
  if (specError) return <div className="report-state" role="alert">{specError}<button className="report-button report-button--primary" onClick={() => { setSpecError(''); setSpecAttempt(n => n + 1); }}>Retry</button></div>;
  if (!spec) return <p className="report-state" role="status">Loading report options…</p>;
  return <div className="hs-report">
    <ReportHead title="Historical Shipments">
      {operation && <span className="report-hint">Retrieved {operation.acquiredAtUtc ? new Date(operation.acquiredAtUtc).toLocaleString() : '—'} · {Number(operation.rowCount).toLocaleString()} rows</span>}
      <ReportInfo title="About these results"><p>Each row is one invoice detail; returns, memo and adjustment activity appear with signed quantities. Invoice Date is the recorded invoice date; Ship Date is shown separately. Values are not net shipments or accounting revenue.</p>{monetarySelected && <p>Amounts use the recorded invoice currency, without conversion. Select Currency in the optional columns to display it.</p>}</ReportInfo>
      <ReportColumnPicker groups={historicalColumnGroups(spec.columns)} selected={selected} disabled={saveBusy} onChange={changeColumns} onReset={() => changeColumns([])} note="Applies the next time you Display." />
      {active && <AnalyticalExcelExport label="Historical Shipments" request={() => ({ operationId: operation?.operationId ?? '' })} contextKey={operation?.operationId ?? 'none'} scopeLabel={`Exports all ${Number(operation?.rowCount ?? 0).toLocaleString()} rows · ${effective.length} columns`} disabledReason={!operation ? 'Display results first.' : !operation.rowCount ? 'No rows to export.' : undefined} busyReason={pageBusy ? 'Wait for the displayed page.' : undefined} clientFactory={() => new HistoricalReportExport(client.current!, operation?.operationId ?? '')} />}
    </ReportHead>
    {collapsed && applied ? <div className="hs-summary">{appliedSummary(applied).map(text => <span className="report-chip" key={text}>{text}</span>)}<div className="hs-summary__actions">{changed && <span className="report-changed">Query changed. Display to update.</span>}<button type="button" className="report-button" onClick={() => setCollapsed(false)}>Edit query</button><button type="button" className="report-button report-button--primary" disabled={disabledDisplay} onClick={() => void display()}>Display</button></div></div> : <form ref={queryPanel} className="hs-query" onSubmit={e => { e.preventDefault(); void display(); }}><fieldset disabled={saveBusy}>
      <section><h2>Customer</h2><label>Site (required)<select ref={firstField} aria-required="true" value={filters.site} onChange={set('site')}><option value="">Select a site</option>{spec.sites.map(s => <option key={s}>{s}</option>)}</select></label>
        <Segmented label="Customer scope" value={mode} choices={[{ value: 'any', label: 'Any customer' }, { value: 'corp', label: 'Corp Group' }, { value: 'range', label: 'Customer range' }]} onChange={v => setMode(v as CustomerMode)} />
        {mode === 'corp' && <label>Corp Group<input value={filters.corpGroup} maxLength={256} onChange={set('corpGroup')} /></label>}{mode === 'range' && range('customerFrom', 'customerTo', 'Customer')}</section>
      <section><h2>Product and sales</h2>{range('itemFrom', 'itemTo', 'Item Number')}{range('productLineFrom', 'productLineTo', 'Product Line')}{range('salespersonFrom', 'salespersonTo', 'Salesperson')}<p className="report-hint">Leave either end blank for no limit.</p></section>
      <section><h2>Invoice dates</h2><div className="report-fields"><label>Invoice Date From<input type="date" value={filters.invoiceDateFrom} aria-invalid={dateClash || undefined} onChange={set('invoiceDateFrom')} /></label><label>Invoice Date To<input type="date" value={filters.invoiceDateTo} aria-invalid={dateClash || undefined} onChange={set('invoiceDateTo')} /></label></div>
        <div className="hs-quick-ranges">{([30, 90, 'year'] as const).map(v => <button type="button" key={v} onClick={() => setFilters({ ...filters, ...calendarRange(today, v) })}>{v === 'year' ? 'Year to date' : `Last ${v} days`}</button>)}</div><p className="report-hint">{!filters.invoiceDateFrom && !filters.invoiceDateTo ? 'All available invoice history' : 'Dates are inclusive.'}</p></section>
      </fieldset><div className="hs-query__actions"><span>{problems.map(p => <span role="alert" key={p}>{p} </span>)}{changed && <span className="report-changed">Query changed. Display to update.</span>}</span><div><button type="submit" className="report-button report-button--primary" disabled={disabledDisplay}>Display</button>{acquiring && <button type="button" className="report-button" onClick={() => reset('Retrieval cancelled.')}>Cancel</button>}</div></div></form>}
    {error && <div className="report-error" role="alert">{error}<button className="report-button report-button--primary" disabled={disabledDisplay} onClick={() => void display()}>Retry</button></div>}{warning && <p className="report-error" role="alert">{warning}</p>}
    {operation && Number(operation.rowCount) > 0 && page ? <section className="hs-results" aria-label="Historical shipments results"><div className="hs-table" tabIndex={0} role="region" aria-label="Results table"><table><thead><tr>{effective.map(id => <th key={id} scope="col" title={provenanceTitle(labels.get(id)?.provenance ?? '')}>{labels.get(id)?.label ?? id}</th>)}</tr></thead><tbody>{page.rows.map(r => <tr key={r.physicalDetailId}>{r.cells.map((v, i) => <td key={i} className={`${labels.get(effective[i])?.kind === 'decimal' ? 'hs-num' : ''}${effective[i] !== 'onTime' && labels.get(effective[i])?.kind === 'decimal' && /^-/.test(v ?? '') ? ' hs-negative' : ''}`}>{effective[i] === 'onTime' && (v === '1' || v === '0') ? <span role="img" aria-label={v === '1' ? 'On Time: True' : 'On Time: False'} className={`hs-on-time hs-on-time--${v === '1' ? 'true' : 'false'}`}>{v === '1' ? '✓' : '✕'}</span> : v ?? '—'}</td>)}</tr>)}</tbody></table></div></section>
      : <div className="report-state">{acquiring ? 'Retrieving invoice history…' : operation ? 'No invoiced shipments matched these filters.' : message || 'Select a site and choose Display.'}</div>}
    <footer className="report-foot"><span role="status">{pageBusy ? 'Loading page…' : acquiring ? 'Retrieving invoice history…' : operation ? `${operation.rowCount ? `Rows ${(pageIndex * pageSize + 1).toLocaleString()} to ${Math.min((pageIndex + 1) * pageSize, Number(operation.rowCount)).toLocaleString()}` : 'Rows 0'} of ${Number(operation.rowCount).toLocaleString()} · Page ${pageIndex + 1} of ${totalPages}${Number(operation.unavailableEnrichmentRows) > 0 ? ` · ${Number(operation.unavailableEnrichmentRows).toLocaleString()} rows have unavailable current-master information (shown as “—”)` : ''}` : message || 'Not retrieved'}</span>
      <nav className="hs-pager" aria-label="Result pages"><label>Rows per page<select value={pageSize} disabled={pageBusy || acquiring || saveBusy || exportPreparing} onChange={e => resize(Number(e.target.value))}>{hsPageSizes.filter(n => n <= Number(spec.maximumPageSize)).map(n => <option key={n}>{n}</option>)}</select></label>{[['First', 0], ['Previous', pageIndex - 1], ['Next', pageIndex + 1], ['Last', totalPages - 1]].map(([label, index]) => <button className="report-button" key={label} disabled={!operation || pageBusy || acquiring || saveBusy || exportPreparing || Number(index) < 0 || Number(index) >= totalPages || Number(index) === pageIndex} onClick={() => void goTo(Number(index))}>{label}</button>)}</nav>
    </footer>
  </div>;
}

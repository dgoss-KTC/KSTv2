import { useEffect, useRef, useState } from 'react';
import {
  getHistoricalShipmentsSpec, HistoricalShipmentsClient,
  type HistoricalShipmentsOperation, type HistoricalShipmentsPage, type HistoricalShipmentsPrepared,
  type HistoricalShipmentsSpec,
} from '../api/historicalShipmentsApi';
import { blankFilters, hsColumnsKey, hsPageSize, restoreColumns, toRequest, validateFilters, type Column, type Filters } from './historicalShipmentsPreferences';

const provenanceTitle = (p: string) => p.startsWith('Current') ? 'Current master information (may be unavailable)' : 'Recorded on the invoice';

type SaveResult = { outcome: string; message: string; savedArtifactId?: string; operationComplete?: boolean; openFailed?: boolean };
/** The desktop host rejects with plain strings; the browser path throws Errors. Both carry a safe, user-facing message. */
function failureText(error: unknown, fallback: string) {
  const text = error instanceof Error ? error.message : typeof error === 'string' ? error : '';
  return text.trim() ? text : fallback;
}
function safeGet() { try { return localStorage.getItem(hsColumnsKey); } catch { return null; } }
export function HistoricalShipmentsReport({ active, onBusyChange, openAfterSave, onOpenAfterSaveChange, createClient = () => new HistoricalShipmentsClient(), loadSpec = getHistoricalShipmentsSpec }: {
  active: boolean; onBusyChange?: (busy: boolean) => void; openAfterSave: boolean; onOpenAfterSaveChange: (value: boolean) => void;
  createClient?: () => HistoricalShipmentsClient; loadSpec?: typeof getHistoricalShipmentsSpec;
}) {
  const [spec, setSpec] = useState<HistoricalShipmentsSpec | null>(null);
  const [specError, setSpecError] = useState('');
  const [filters, setFilters] = useState<Filters>(blankFilters);
  const [selected, setSelected] = useState<string[]>([]);
  const [operation, setOperation] = useState<HistoricalShipmentsOperation | null>(null);
  const [page, setPage] = useState<HistoricalShipmentsPage | null>(null);
  const [pageIndex, setPageIndex] = useState(0);
  const [pageBusy, setPageBusy] = useState(false);
  const [acquiring, setAcquiring] = useState(false);
  const [preparing, setPreparing] = useState(false);
  const [prepared, setPrepared] = useState<HistoricalShipmentsPrepared | null>(null);
  const [saving, setSaving] = useState(false);
  const [pendingSave, setPendingSave] = useState<string | null>(null);
  const [savedArtifact, setSavedArtifact] = useState<string | null>(null);
  const [message, setMessage] = useState('');
  const [warning, setWarning] = useState('');
  const client = useRef<HistoricalShipmentsClient | null>(null);
  const abort = useRef<AbortController | null>(null);
  const token = useRef(0);
  const saveBusy = useRef(false);

  function currentClient() { return client.current ??= createClient(); }
  function reset(note = '') {
    token.current++; abort.current?.abort(); abort.current = null;
    const old = client.current; client.current = null; void old?.release().catch(() => {});
    setOperation(null); setPage(null); setPageIndex(0); setPrepared(null); setAcquiring(false); setPreparing(false); setPageBusy(false);
    setMessage(note);
  }
  useEffect(() => {
    const controller = new AbortController();
    loadSpec(controller.signal).then(value => { setSpec(value); setSelected(restoreColumns(value, safeGet())); })
      .catch(() => { if (!controller.signal.aborted) setSpecError('The report options could not be loaded. Check that the application is running and try again.'); });
    return () => { controller.abort(); };
  }, [loadSpec]);
  useEffect(() => () => { token.current++; abort.current?.abort(); void client.current?.release().catch(() => {}); }, []);
  const departed = useRef(false);
  useEffect(() => {
    if (!active && !departed.current) { departed.current = true; reset('Report results were released when you left this screen. Display again to reacquire.'); }
    if (active) departed.current = false;
  }, [active]);
  useEffect(() => { onBusyChange?.(saving); }, [saving, onBusyChange]);

  function changeFilters(next: Filters) { if (operation || prepared) reset('Filters changed. Display again to refresh the results.'); setFilters(next); }
  function changeColumns(next: string[]) {
    if (!spec) return;
    const ordered = spec.columns.filter(c => c.defaultSelected || next.includes(c.id)).map(c => c.id);
    if (operation || prepared) reset('Columns changed. Display again to refresh the results.');
    setSelected(ordered);
    try { localStorage.setItem(hsColumnsKey, JSON.stringify(ordered)); setWarning(''); }
    catch { setWarning('Column selections could not be remembered; the session choice is retained.'); }
  }

  const problems = validateFilters(filters);
  const noColumns = selected.length === 0;
  const labels = new Map((spec?.columns ?? []).map(c => [c.id, c] as const));
  const monetarySelected = selected.some(id => labels.get(id)?.monetary);

  async function display() {
    if (problems.length || noColumns || acquiring || saveBusy.current || pendingSave) return;
    reset(); const mine = token.current; const controller = new AbortController(); abort.current = controller;
    setAcquiring(true); setMessage('Retrieving invoice history…');
    try {
      const c = currentClient();
      const op = await c.acquire(toRequest(filters, selected), controller.signal);
      if (token.current !== mine) return;
      const first = await c.page(0, hsPageSize, controller.signal);
      if (token.current !== mine) return;
      setOperation(op); setPage(first); setPageIndex(0);
      setMessage(op.rowCount === 0 ? 'No invoiced shipments matched these filters.' : '');
    } catch (error) {
      if (token.current === mine && !controller.signal.aborted) { reset(failureText(error, 'The report could not be retrieved.')); }
    } finally { if (token.current === mine) setAcquiring(false); }
  }
  function cancel() { reset('Retrieval cancelled.'); }
  async function goTo(index: number) {
    if (!operation || pageBusy || !client.current) return;
    const mine = token.current; const wanted = ++pageSeq.current; setPageBusy(true);
    try {
      const next = await client.current.page(index * hsPageSize, hsPageSize);
      if (token.current === mine && pageSeq.current === wanted) { setPage(next); setPageIndex(index); }
    } catch (error) { if (token.current === mine) setMessage(failureText(error, 'The page could not be loaded.')); }
    finally { if (token.current === mine && pageSeq.current === wanted) setPageBusy(false); }
  }
  const pageSeq = useRef(0);

  async function prepareExport() {
    if (!operation || preparing || saveBusy.current) return;
    const mine = token.current; setPreparing(true); setMessage('Preparing workbook…');
    try {
      const result = await client.current!.prepare();
      if (token.current !== mine) return;
      setPrepared(result); setMessage('Workbook ready. Choose Save As to store it.');
    } catch (error) { if (token.current === mine) setMessage(failureText(error, 'The workbook could not be prepared.')); }
    finally { if (token.current === mine) setPreparing(false); }
  }
  function apply(result: SaveResult, operationId: string) {
    const terminal = result.operationComplete !== false && ['saved', 'failed', 'cancelled'].includes(result.outcome);
    setPendingSave(terminal ? null : operationId); setMessage(result.message);
    if (result.outcome === 'saved') setSavedArtifact(result.savedArtifactId ?? null);
    if (result.openFailed) setSavedArtifact(result.savedArtifactId ?? savedArtifact);
  }
  async function save() {
    if (!prepared || saveBusy.current || pendingSave || !client.current) return;
    saveBusy.current = true; setSaving(true); const operationId = crypto.randomUUID();
    try { apply(await client.current.save(prepared, openAfterSave, operationId) as SaveResult, operationId); }
    catch { setPendingSave(operationId); setMessage('Save response unavailable; the outcome is unknown. Check the save status before trying again.'); }
    finally { saveBusy.current = false; setSaving(false); }
  }
  async function checkStatus() {
    if (!pendingSave || saveBusy.current) return;
    saveBusy.current = true; setSaving(true);
    try { apply(await currentClient().saveStatus(pendingSave) as SaveResult, pendingSave); }
    catch { setMessage('Save status is unavailable. Do not save again yet.'); }
    finally { saveBusy.current = false; setSaving(false); }
  }
  async function retryOpen() {
    if (!savedArtifact || saveBusy.current) return;
    saveBusy.current = true; setSaving(true);
    try { await currentClient().open(savedArtifact, crypto.randomUUID()); setSavedArtifact(null); setMessage('Workbook saved; opening request accepted.'); }
    catch { setMessage('Workbook saved, but could not be opened.'); }
    finally { saveBusy.current = false; setSaving(false); }
  }

  const effective = operation?.columns ?? [];
  const totalPages = operation ? Math.max(1, Math.ceil(Number(operation.rowCount) / hsPageSize)) : 0;
  const set = (key: keyof Filters) => (e: { target: { value: string } }) => changeFilters({ ...filters, [key]: e.target.value });
  const range = (from: keyof Filters, to: keyof Filters, label: string, invalid = false) => <div className="hs-range" role="group" aria-label={label}>
    <label>{label} From<input value={filters[from]} maxLength={256} aria-invalid={invalid || undefined} onChange={set(from)} /></label>
    <span aria-hidden="true">to</span>
    <label>{label} To<input value={filters[to]} maxLength={256} aria-invalid={invalid || undefined} onChange={set(to)} /></label></div>;
  const customerClash = !!filters.corpGroup.trim() && !!(filters.customerFrom.trim() || filters.customerTo.trim());
  const dateClash = !!filters.invoiceDateFrom && !!filters.invoiceDateTo && filters.invoiceDateFrom > filters.invoiceDateTo;
  const groups = (spec?.columns ?? []).filter(c => !c.defaultSelected).reduce<Record<string, Column[]>>((acc, c) => { (acc[provenanceTitle(c.provenance)] ??= []).push(c); return acc; }, {});

  if (specError) return <p role="alert">{specError}</p>;
  if (!spec) return <p role="status">Loading report options…</p>;
  return <div className="hs-report">
    <form onSubmit={e => { e.preventDefault(); void display(); }}>
      <fieldset disabled={saving || !!pendingSave}>
        <legend>Filters</legend>
        <label>Site <span className="hs-required">(required)</span>
          <select value={filters.site} onChange={set('site')} aria-required="true"><option value="">Select a site</option>{spec.sites.map(s => <option key={s}>{s}</option>)}</select></label>
        <p className="hs-hint">Results include only invoices for the selected site.</p>
        <div className="hs-row">
          <label>Corp Group<input value={filters.corpGroup} maxLength={256} aria-invalid={customerClash || undefined} onChange={set('corpGroup')} /></label>
          {range('customerFrom', 'customerTo', 'Customer', customerClash)}
        </div>
        <p className="hs-hint">Choose a Corp Group or a Customer range, not both.</p>
        {customerClash && <p role="alert">Use either Corp Group or a Customer range, not both.</p>}
        <div className="hs-row">{range('itemFrom', 'itemTo', 'Item Number')}{range('productLineFrom', 'productLineTo', 'Product Line')}{range('salespersonFrom', 'salespersonTo', 'Salesperson')}</div>
        <p className="hs-hint">Leave either end of a range blank for no limit.</p>
        <div className="hs-row">
          <label>Invoice Date From<input type="date" value={filters.invoiceDateFrom} aria-invalid={dateClash || undefined} onChange={set('invoiceDateFrom')} /></label>
          <label>Invoice Date To<input type="date" value={filters.invoiceDateTo} aria-invalid={dateClash || undefined} onChange={set('invoiceDateTo')} /></label>
        </div>
        <p className="hs-hint">Leave both dates blank to include all available invoice history. Entering one date leaves the other end open; entered dates are inclusive.</p>
        {dateClash && <p role="alert">Invoice Date To must be on or after Invoice Date From.</p>}
      </fieldset>
      <details className="hs-columns"><summary>Optional columns ({spec.columns.filter(c => !c.defaultSelected && selected.includes(c.id)).length} selected)</summary>
        <p className="hs-hint"><strong>Always included:</strong> {spec.columns.filter(c => c.defaultSelected).map(c => c.label).join(', ')}.</p>
        {Object.entries(groups).map(([title, cols]) => <fieldset key={title}><legend>{title}</legend>
          <div className="hs-columns__grid">{cols.map(c => <label key={c.id}><input type="checkbox" checked={selected.includes(c.id)}
            onChange={e => changeColumns(e.target.checked ? [...selected, c.id] : selected.filter(id => id !== c.id))} />{c.label}</label>)}</div></fieldset>)}
        <button type="button" onClick={() => changeColumns(spec.columns.filter(c => c.defaultSelected).map(c => c.id))}>Restore default columns</button>
      </details>
      {monetarySelected && <p className="hs-hint">Amounts use the recorded invoice currency, without conversion. Select Currency in the optional columns to display it.</p>}
      {noColumns && <p role="alert">Select at least one column.</p>}
      <div className="reports-panel__actions">
        <button type="submit" disabled={acquiring || saving || !!pendingSave || problems.length > 0 || noColumns}>Display</button>
        {acquiring && <button type="button" onClick={cancel}>Cancel</button>}
        <button type="button" disabled={!operation || operation.rowCount === 0 || preparing || saving || !!pendingSave || !!prepared} onClick={() => void prepareExport()}>Export</button>
        <button type="button" disabled={!prepared || saving || !!pendingSave} onClick={() => void save()}>Save As…</button>
        {pendingSave && <button type="button" disabled={saving} onClick={() => void checkStatus()}>Check save status</button>}
        {savedArtifact && <button type="button" disabled={saving} onClick={() => void retryOpen()}>Retry Open</button>}
      </div>
    </form>
    <label><input type="checkbox" checked={openAfterSave} onChange={e => onOpenAfterSaveChange(e.target.checked)} />Open workbook after saving</label>
    {message && <p role="status">{message}</p>}
    {warning && <p role="alert">{warning}</p>}
    {operation && Number(operation.rowCount) > 0 && page && <section aria-label="Historical shipments results">
      <p>{Number(operation.rowCount).toLocaleString()} invoice detail rows · retrieved {operation.acquiredAtUtc ? new Date(operation.acquiredAtUtc).toLocaleString() : ''}
        {Number(operation.unavailableEnrichmentRows) > 0 && ` · ${Number(operation.unavailableEnrichmentRows).toLocaleString()} rows have unavailable current-master information (shown as “—”)`}</p>
      <p className="hs-hint">Each row is one invoice detail; returns, memo and adjustment activity appear with signed quantities. Invoice Date is the recorded invoice date; Ship Date is shown separately. Values are not net shipments or accounting revenue.</p>
      <div className="hs-table" tabIndex={0} role="region" aria-label="Results table">
        <table><thead><tr>{effective.map(id => <th key={id} scope="col">{labels.get(id)?.label ?? id}</th>)}</tr></thead>
          <tbody>{page.rows.map(r => <tr key={r.physicalDetailId}>{r.cells.map((v, i) => <td key={i} className={labels.get(effective[i])?.kind === 'decimal' ? 'hs-num' : undefined}>{effective[i] === 'onTime' && (v === '1' || v === '0')
            ? <span role="img" aria-label={v === '1' ? 'On Time: True' : 'On Time: False'} className={`hs-on-time hs-on-time--${v === '1' ? 'true' : 'false'}`}>{v === '1' ? '✓' : '✕'}</span>
            : v ?? '—'}</td>)}</tr>)}</tbody></table>
      </div>
      <nav className="hs-pager" aria-label="Result pages">
        <button disabled={pageBusy || pageIndex === 0} onClick={() => void goTo(0)}>First</button>
        <button disabled={pageBusy || pageIndex === 0} onClick={() => void goTo(pageIndex - 1)}>Previous</button>
        <span>Page {(pageIndex + 1).toLocaleString()} of {totalPages.toLocaleString()}</span>
        <button disabled={pageBusy || pageIndex + 1 >= totalPages} onClick={() => void goTo(pageIndex + 1)}>Next</button>
        <button disabled={pageBusy || pageIndex + 1 >= totalPages} onClick={() => void goTo(totalPages - 1)}>Last</button>
      </nav>
    </section>}
  </div>;
}

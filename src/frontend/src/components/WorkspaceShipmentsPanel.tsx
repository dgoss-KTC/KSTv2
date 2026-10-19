import { useCallback, useEffect, useRef, useState } from 'react';
import {
  getWorkspaceShipmentsSpec, initialWorkspaceShipmentsFilters, WorkspaceShipmentsClient,
  type ShipmentColumnFilter, type WorkspaceShipmentsView, type WorkspaceShipmentsOperation,
  type WorkspaceShipmentsPage, type WorkspaceShipmentsRequest, type WorkspaceShipmentsSpec,
} from '../api/workspaceShipmentsApi';
import { formatWorkspaceShipmentsOnTimeRate } from './workspaceShipmentsPresentation';
import { AnchoredPopover } from './shared/AnchoredPopover';
import { ShipmentHeaderFilter } from './shipments/ShipmentHeaderFilter';
import { isNegativeDecimal } from '../shared/exactDecimal';
import './WorkspaceShipmentsPanel.css';

const pageSizeKey = 'kst.workspaceShipments.pageSize.v1';
function preferredPageSize() {
  try { const size = Number(localStorage.getItem(pageSizeKey)); return [100, 500, 1000].includes(size) ? size : 100; } catch { return 100; }
}
interface FilterDraft { part: string; from: string; to: string }
type CreateClient = (assignmentId: string) => WorkspaceShipmentsClient;
type LoadSpec = (assignmentId: string, signal?: AbortSignal) => Promise<WorkspaceShipmentsSpec>;
interface Props { assignmentId: string; createClient?: CreateClient; loadSpec?: LoadSpec }
const defaultCreateClient: CreateClient = id => new WorkspaceShipmentsClient(id);
function failureText(error: unknown, fallback: string) {
  if (error instanceof TypeError) return `${fallback} The application service could not be reached.`;
  const text = error instanceof Error ? error.message.trim() : '';
  return text ? `${fallback} ${text}` : fallback;
}
function toDraft(request: WorkspaceShipmentsRequest): FilterDraft {
  return { part: request.partNumber ?? '', from: request.shipDateFrom ?? '', to: request.shipDateTo ?? '' };
}
function toRequest(draft: FilterDraft): WorkspaceShipmentsRequest {
  return { partNumber: draft.part.trim() || null, shipDateFrom: draft.from || null, shipDateTo: draft.to || null };
}
function describe(request: WorkspaceShipmentsRequest) {
  const { shipDateFrom: from, shipDateTo: to, partNumber } = request;
  return `${!from && !to ? 'All available history' : `Ship Date ${from ?? 'any'} to ${to ?? 'any'}`} · ${partNumber ? `Part ${partNumber}` : 'All parts'}`;
}
const sameDraft = (a: FilterDraft, b: FilterDraft) => a.part.trim() === b.part.trim() && a.from === b.from && a.to === b.to;

/** A new visit owns a fresh operation, filter state and one bounded page. Only page size persists. */
export function WorkspaceShipmentsPanel(props: Props) {
  return <WorkspaceShipmentsSession key={props.assignmentId} {...props} />;
}
function WorkspaceShipmentsSession({ assignmentId, createClient = defaultCreateClient, loadSpec = getWorkspaceShipmentsSpec }: Props) {
  const [initial] = useState(() => toDraft(initialWorkspaceShipmentsFilters()));
  const [draft, setDraft] = useState(initial);
  const [applied, setApplied] = useState(initial);
  const [spec, setSpec] = useState<WorkspaceShipmentsSpec | null>(null);
  const [specError, setSpecError] = useState(false);
  const [specAttempt, setSpecAttempt] = useState(0);
  const specRequest = useRef<Promise<WorkspaceShipmentsSpec> | null>(null);
  const [operation, setOperation] = useState<WorkspaceShipmentsOperation | null>(null);
  const [view, setView] = useState<WorkspaceShipmentsView | null>(null);
  const [filters, setFilters] = useState<ShipmentColumnFilter[]>([]);
  const filtersRef = useRef<ShipmentColumnFilter[]>([]);
  const [pageSize, setPageSize] = useState(preferredPageSize);
  const pageSizeRef = useRef(pageSize);
  const [page, setPage] = useState<WorkspaceShipmentsPage | null>(null);
  const [pageIndex, setPageIndex] = useState(0);
  const [pageBusy, setPageBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [applying, setApplying] = useState(false);
  const [error, setError] = useState('');
  const [pageError, setPageError] = useState('');
  const [note, setNote] = useState('');
  const [openColumn, setOpenColumn] = useState<string | null>(null);
  const [filterClient, setFilterClient] = useState<WorkspaceShipmentsClient | null>(null);
  const [about, setAbout] = useState(false);
  const anchor = useRef<HTMLElement | null>(null);
  const aboutAnchor = useRef<HTMLButtonElement>(null);
  const client = useRef<WorkspaceShipmentsClient | null>(null);
  const abort = useRef<AbortController | null>(null);
  const filterAbort = useRef<AbortController | null>(null);
  const pageAbort = useRef<AbortController | null>(null);
  const sequence = useRef(0);
  const token = useRef(0);
  const pageSeq = useRef(0);
  const factory = useRef(createClient);
  const invalidate = useCallback(() => {
    token.current++; pageSeq.current++; abort.current?.abort(); filterAbort.current?.abort(); pageAbort.current?.abort(); abort.current = null;
  }, []);

  const acquire = useCallback(async (request: WorkspaceShipmentsRequest) => {
    const current = client.current; if (!current) return;
    const mine = ++token.current; pageSeq.current++;
    abort.current?.abort(); filterAbort.current?.abort(); pageAbort.current?.abort();
    setOpenColumn(null); setApplying(false);
    const controller = new AbortController(); abort.current = controller;
    setLoading(true); setError(''); setPageError(''); setNote('');
    setOperation(null); setView(null); setPage(null); setPageIndex(0); setPageBusy(false);
    try {
      const op = await current.acquire(request, controller.signal);
      if (token.current !== mine) return;
      let derived: WorkspaceShipmentsView = { operationId: op.operationId, version: 0, rowCount: op.rowCount,
        onTimeNumerator: op.onTimeNumerator ?? 0, onTimeDenominator: op.onTimeDenominator ?? 0, filters: [] };
      if (filtersRef.current.length) derived = await current.setFilters(filtersRef.current, 0, ++sequence.current, controller.signal);
      if (token.current !== mine) return;
      const catalog = await specRequest.current?.catch(() => null);
      if (token.current !== mine) return;
      const size = catalog && Number(catalog.maximumPageSize) >= pageSizeRef.current && (catalog.pageSizes ?? [100]).map(Number).includes(pageSizeRef.current) ? pageSizeRef.current : 100;
      pageSizeRef.current = size; setPageSize(size);
      setOperation(op); setView(derived);
      const first = await current.page(0, size, controller.signal, derived.version);
      if (token.current === mine) setPage(first);
    } catch (failure) {
      if (token.current !== mine || controller.signal.aborted) return;
      setError(failureText(failure, 'Workspace shipments could not be retrieved. Refresh to try again.'));
    } finally { if (token.current === mine) setLoading(false); }
  }, []);
  useEffect(() => {
    const visit = factory.current(assignmentId); client.current = visit;
    void Promise.resolve().then(() => { if (client.current === visit) void acquire(toRequest(initial)); });
    return () => { invalidate(); client.current = null; void visit.release().catch(() => {}); };
  }, [assignmentId, acquire, invalidate, initial]);
  useEffect(() => {
    const controller = new AbortController();
    const request = loadSpec(assignmentId, controller.signal); specRequest.current = request;
    request
      .then(value => { if (!controller.signal.aborted) { setSpec(value); setSpecError(false); } })
      .catch(() => { if (!controller.signal.aborted) setSpecError(true); });
    return () => controller.abort();
  }, [assignmentId, loadSpec, specAttempt]);

  const dateClash = !!draft.from && !!draft.to && draft.from > draft.to;
  const dirty = !sameDraft(draft, applied);
  function apply() {
    if (loading || applying || dateClash || !dirty) return;
    const next = { ...draft, part: draft.part.trim() }; setDraft(next); setApplied(next); void acquire(toRequest(next));
  }
  function refresh() { if (!loading && !applying) void acquire(toRequest(applied)); }
  function cancel() { invalidate(); setLoading(false); setPageBusy(false); setNote('Retrieval cancelled. Refresh to retrieve again.'); }
  async function goTo(index: number, size = pageSize) {
    const current = client.current;
    if (!operation || !view || pageBusy || applying || !current) return;
    const mine = token.current; const wanted = ++pageSeq.current;
    pageAbort.current?.abort(); const controller = new AbortController(); pageAbort.current = controller;
    setPageBusy(true); setPageError('');
    try {
      const next = await current.page(index * size, size, controller.signal, view.version);
      if (token.current === mine && pageSeq.current === wanted) {
        setPage(next); setPageIndex(index); setPageSize(size); pageSizeRef.current = size;
        try { localStorage.setItem(pageSizeKey, String(size)); } catch { /* optional display preference */ }
      }
    } catch (failure) {
      if (token.current === mine && pageSeq.current === wanted) setPageError(failureText(failure, 'That page could not be loaded. Retry the page.'));
    } finally { if (token.current === mine && pageSeq.current === wanted) setPageBusy(false); }
  }
  async function publishView(next: WorkspaceShipmentsView, current: WorkspaceShipmentsClient, mine: number) {
    if (token.current !== mine) return;
    setView(next); setFilters(next.filters); filtersRef.current = next.filters;
    setPage(null); setPageIndex(0); setApplying(false); setPageBusy(true);
    try {
      const first = await current.page(0, pageSizeRef.current, undefined, next.version);
      if (token.current === mine) setPage(first);
    } catch (failure) {
      if (token.current === mine) setPageError(failureText(failure, 'That page could not be loaded. Retry the page.'));
    } finally { if (token.current === mine) setPageBusy(false); }
  }
  async function changeFilters(next: ShipmentColumnFilter[]) {
    const current = client.current; if (!operation || !view || applying || loading || !current) return;
    setOpenColumn(null); const mine = ++token.current; pageSeq.current++; pageAbort.current?.abort();
    setPageBusy(false); setApplying(true); setPageError(''); setNote('');
    const controller = new AbortController(); filterAbort.current = controller; const seq = ++sequence.current;
    try {
      const result = await current.setFilters(next, view.version, seq, controller.signal);
      if (token.current === mine && !controller.signal.aborted) await publishView(result, current, mine);
    } catch (failure) {
      if (token.current !== mine || controller.signal.aborted) return;
      // A lost response may already have committed. Reconcile the canonical atomic version.
      try {
        const actual = await current.view();
        if (token.current === mine && actual.version !== view.version) await publishView(actual, current, mine);
      } catch { if (token.current === mine) { setPage(null); setPageError('View status is unavailable. Refresh to retrieve again.'); } }
      if (token.current === mine) setNote(failureText(failure, 'Filter could not be applied. The last completed view is retained.'));
    } finally { if (token.current === mine && !controller.signal.aborted) setApplying(false); }
  }
  async function cancelFilter() {
    const current = client.current; if (!current || !view) return;
    const mine = ++token.current; filterAbort.current?.abort();
    try {
      await current.cancelView(sequence.current); const actual = await current.view();
      if (token.current !== mine) return;
      if (actual.version !== view.version) { await publishView(actual, current, mine); setNote('The filter completed before cancellation. Showing the completed view.'); }
      else setNote('Filter cancelled. Previous view retained.');
    } catch (failure) {
      if (token.current === mine) { setPage(null); setPageError(failureText(failure, 'Cancellation status could not be confirmed. Refresh to retrieve again.')); }
    } finally { if (token.current === mine) setApplying(false); }
  }

  const columns = new Map((spec?.columns ?? []).map(c => [c.id, c] as const));
  const effective = operation?.columns ?? [];
  const acquiredCount = Number(operation?.rowCount ?? 0);
  const rowCount = Number(view?.rowCount ?? 0);
  const totalPages = Math.max(1, Math.ceil(rowCount / pageSize));
  const first = pageIndex * pageSize;
  const unavailable = Number(operation?.unavailableEnrichmentRows ?? 0);
  const open = spec?.columns.find(c => c.id === openColumn);
  const busy = loading || applying;

  return <section className="workspace-shipments" aria-label="Workspace Shipments" aria-busy={busy}>
    <div className="workspace-shipments__toolbar">
      <h3>Workspace Shipments</h3>
      <form className="workspace-shipments__filters" onSubmit={event => { event.preventDefault(); apply(); }}>
        <label>Ship Date From<input type="date" value={draft.from} aria-invalid={dateClash || undefined} onChange={e => setDraft({ ...draft, from: e.target.value })} /></label>
        <label>Ship Date To<input type="date" value={draft.to} aria-invalid={dateClash || undefined} onChange={e => setDraft({ ...draft, to: e.target.value })} /></label>
        <label>Part Number<input type="text" value={draft.part} maxLength={30} autoComplete="off" spellCheck={false} placeholder="Exact part (optional)" onChange={e => setDraft({ ...draft, part: e.target.value })} /></label>
        <button type="submit" disabled={busy || dateClash || !dirty}>Apply</button>
      </form>
      <div className="workspace-shipments__summary" aria-live="polite" aria-atomic="true">
        <span>On Time Rate</span><strong>{formatWorkspaceShipmentsOnTimeRate(view?.onTimeNumerator ?? null, view?.onTimeDenominator ?? null)}</strong>
        {filters.length > 0 && <span className="workspace-shipments__tag">filtered</span>}
      </div>
      {operation && <span className="workspace-shipments__meta">Retrieved {operation.acquiredAtUtc ? new Date(operation.acquiredAtUtc).toLocaleString() : ''} · {acquiredCount.toLocaleString()} rows</span>}
      <button ref={aboutAnchor} type="button" aria-label="About these numbers" aria-haspopup="dialog" aria-expanded={about} onClick={() => { setOpenColumn(null); setAbout(!about); }}>ⓘ</button>
      <button className="workspace-shipments__primary" type="button" disabled={busy} onClick={refresh}>Refresh</button>
    </div>
    {dateClash && <p role="alert" className="workspace-shipments__warning">Ship Date To must be on or after Ship Date From.</p>}
    <p className="workspace-shipments__meta workspace-shipments__scope">Retrieved scope: {describe(toRequest(applied))}{dirty && ' · Acquisition inputs have unapplied edits'}</p>
    {loading && <div role="status" className="workspace-shipments__state">Retrieving workspace shipments…<button type="button" onClick={cancel}>Cancel</button></div>}
    {note && <p role="status" className="workspace-shipments__notice">{note}</p>}
    {error && <p role="alert" className="workspace-shipments__warning">{error} <button disabled={busy} type="button" onClick={refresh}>Retry</button></p>}
    {specError && <p role="alert" className="workspace-shipments__warning">Column definitions could not be loaded. <button type="button" onClick={() => setSpecAttempt(n => n + 1)}>Retry</button></p>}
    {operation && spec && <>
      <div className="workspace-shipments__grid" tabIndex={0} role="region" aria-label="Workspace shipments table">
        <table><thead><tr>{effective.map(id => {
          const c = columns.get(id); const active = filters.some(f => f.column === id);
          return <th key={id} scope="col" title={id === 'shipDate' ? 'Set by Ship Date From and To in the acquisition controls.' : c?.provenance} className={`${c?.kind === 'decimal' ? 'workspace-shipments__number ' : ''}${active ? 'workspace-shipments__active' : ''}`}>
            <span>{c?.label ?? id}</span>{id !== 'shipDate' && <button className="workspace-shipments__funnel" type="button" aria-label={`Filter ${c?.label ?? id}${active ? ', filter active' : ''}`} aria-haspopup="dialog" aria-expanded={openColumn === id} disabled={busy} onClick={e => { anchor.current = e.currentTarget; setFilterClient(client.current); setAbout(false); setOpenColumn(openColumn === id ? null : id); }}>
              <svg width="14" height="14" viewBox="0 0 16 16" fill={active ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="1.4" aria-hidden="true"><path d="M2.5 3.5h11l-4.2 5v4l-2.6-1.4V8.5z" /></svg></button>}
          </th>;
        })}</tr></thead>
          <tbody className={applying ? 'workspace-shipments__dimmed' : undefined}>{page?.rows.map(row => <tr key={row.physicalDetailId}>{row.cells.map((value, i) => <td key={i}
            className={`${columns.get(effective[i])?.kind === 'decimal' ? 'workspace-shipments__number ' : ''}${effective[i] === 'itemNumber' ? 'workspace-shipments__part ' : ''}${value && ['invoicedQuantity', 'retroExtendedPrice'].includes(effective[i]) && isNegativeDecimal(value) ? 'workspace-shipments__negative' : ''}`}>
            {effective[i] === 'onTime' && (value === '1' || value === '0')
              ? <span role="img" aria-label={value === '1' ? 'On Time: True' : 'On Time: False'} className={`workspace-shipments__on-time--${value === '1' ? 'true' : 'false'}`}>{value === '1' ? '✓' : '✕'}</span>
              : value ?? '—'}</td>)}</tr>)}</tbody>
        </table>
        {rowCount === 0 && <div className="workspace-shipments__state" role="status">{filters.length ? 'No rows match the column filters.' : 'No invoiced shipments matched these dates and part in this workspace.'}
          {filters.length > 0 && <button disabled={busy} type="button" onClick={() => void changeFilters([])}>Clear filters ({filters.length})</button>}</div>}
      </div>
      {pageError && <p role="alert" className="workspace-shipments__warning">{pageError} <button disabled={busy || pageBusy} type="button" onClick={() => void goTo(pageIndex)}>Retry page</button></p>}
      <div className="workspace-shipments__footer">
        <span role="status">{rowCount === 0 ? 'Rows 0 of 0' : `Rows ${page ? (first + 1).toLocaleString() : '—'}–${page ? (first + page.rows.length).toLocaleString() : '—'} of ${rowCount.toLocaleString()}`}{filters.length > 0 && ` · filtered from ${acquiredCount.toLocaleString()}`}</span>
        {filters.length > 0 && <button disabled={busy} type="button" onClick={() => void changeFilters([])}>Clear filters ({filters.length})</button>}
        {unavailable > 0 && <span>{unavailable.toLocaleString()} rows have unavailable current-master information (shown as “—”).</span>}
        {applying ? <><span role="status">Applying filter…</span><button type="button" onClick={() => void cancelFilter()}>Cancel</button></> : pageBusy ? <span role="status">Loading page…</span> : null}
        <div role="group" aria-label="Rows per page" className="workspace-shipments__sizes"><span>Rows per page</span>{[100, 500, 1000].map(size => <button key={size} type="button" aria-pressed={pageSize === size} disabled={busy || pageBusy || size > Number(spec.maximumPageSize) || !spec.pageSizes?.includes(size)} onClick={() => void goTo(Math.floor(first / size), size)}>{size}</button>)}</div>
        {Number(spec.maximumPageSize) < 1000 && <span>Not available on this version: larger page sizes</span>}
        <nav className="workspace-shipments__pager" aria-label="Result pages">
          <button type="button" disabled={busy || pageBusy || pageIndex === 0} onClick={() => void goTo(0)}>First</button>
          <button type="button" disabled={busy || pageBusy || pageIndex === 0} onClick={() => void goTo(pageIndex - 1)}>Previous</button>
          <span>Page {(pageIndex + 1).toLocaleString()} of {totalPages.toLocaleString()}</span>
          <button type="button" disabled={busy || pageBusy || pageIndex + 1 >= totalPages} onClick={() => void goTo(pageIndex + 1)}>Next</button>
          <button type="button" disabled={busy || pageBusy || pageIndex + 1 >= totalPages} onClick={() => void goTo(totalPages - 1)}>Last</button>
        </nav>
      </div>
    </>}
    {open && spec && filterClient && operation && view && <ShipmentHeaderFilter key={open.id} column={open} spec={spec} anchor={anchor} filter={filters.find(f => f.column === open.id)} client={filterClient} operationId={operation.operationId} version={view.version} onClose={() => setOpenColumn(null)} onApply={filter => void changeFilters([...filters.filter(f => f.column !== open.id), ...(filter ? [filter] : [])])} />}
    {about && <AnchoredPopover title="About these numbers" anchor={aboutAnchor} onClose={() => setAbout(false)}><p>Each row is one invoice detail; returns, memo and adjustment activity appear with signed quantities. Prices are the retro prices. Values are not net shipments or accounting revenue.</p><p>Acquisition chooses workspace membership, exact Part and inclusive Ship Date bounds. Header filters refine that frozen result without another source read. On Time Rate counts eligible physical rows, not quantities; missing dates or non-standard SO types are excluded.</p></AnchoredPopover>}
  </section>;
}

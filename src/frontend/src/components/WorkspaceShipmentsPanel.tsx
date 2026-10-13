import { useCallback, useEffect, useRef, useState } from 'react';
import {
  getWorkspaceShipmentsSpec, initialWorkspaceShipmentsFilters, WorkspaceShipmentsClient,
  type WorkspaceShipmentsAcquisition, type WorkspaceShipmentsOperation, type WorkspaceShipmentsPage, type WorkspaceShipmentsRequest, type WorkspaceShipmentsSpec,
} from '../api/workspaceShipmentsApi';
import { formatWorkspaceShipmentsOnTimeRate } from './workspaceShipmentsPresentation';
import './WorkspaceShipmentsPanel.css';

/** Rows per display page. Only this one bounded page is held in frontend memory (backend maximum is 200). */
export const workspaceShipmentsPageSize = 100;

interface FilterDraft { part: string; from: string; to: string }
type CreateClient = (assignmentId: string) => WorkspaceShipmentsClient;
type LoadSpec = (assignmentId: string, signal?: AbortSignal) => Promise<WorkspaceShipmentsSpec>;
interface Props { assignmentId: string; createClient?: CreateClient; loadSpec?: LoadSpec }

const defaultCreateClient: CreateClient = id => new WorkspaceShipmentsClient(id);

/** Network-level failures carry browser wording; everything else is a sanitized backend message. */
function failureText(error: unknown, fallback: string) {
  if (error instanceof TypeError) return `${fallback} The application service could not be reached.`;
  const text = error instanceof Error ? error.message.trim() : '';
  return text ? `${fallback} ${text}` : fallback;
}

function toDraft(request: WorkspaceShipmentsRequest): FilterDraft {
  return { part: request.partNumber ?? '', from: request.shipDateFrom ?? '', to: request.shipDateTo ?? '' };
}
function toRequest(draft: FilterDraft): WorkspaceShipmentsRequest {
  const part = draft.part.trim();
  return { partNumber: part === '' ? null : part, shipDateFrom: draft.from || null, shipDateTo: draft.to || null };
}
function describe(request: WorkspaceShipmentsRequest) {
  const { shipDateFrom: from, shipDateTo: to, partNumber } = request;
  const dates = !from && !to ? 'All available history' : `Ship Date ${from ?? 'any'} to ${to ?? 'any'}`;
  return `${dates} · ${partNumber ? `Part ${partNumber}` : 'All parts'}`;
}
const sameDraft = (a: FilterDraft, b: FilterDraft) => a.part.trim() === b.part.trim() && a.from === b.from && a.to === b.to;
// A fresh mounted session per workspace assignment: filters, results and the backend operation never
// carry across workspaces or visits, and late replies from a prior assignment cannot reach the new one.
export function WorkspaceShipmentsPanel(props: Props) {
  return <WorkspaceShipmentsSession key={props.assignmentId} {...props} />;
}

function WorkspaceShipmentsSession({ assignmentId, createClient = defaultCreateClient, loadSpec = getWorkspaceShipmentsSpec }: Props) {
  const [initial] = useState(() => toDraft(initialWorkspaceShipmentsFilters()));
  const [draft, setDraft] = useState<FilterDraft>(initial);
  const [applied, setApplied] = useState<FilterDraft>(initial);
  const [spec, setSpec] = useState<WorkspaceShipmentsSpec | null>(null);
  const [specError, setSpecError] = useState(false);
  const [specAttempt, setSpecAttempt] = useState(0);
  const [operation, setOperation] = useState<WorkspaceShipmentsOperation | null>(null);
  const [page, setPage] = useState<WorkspaceShipmentsPage | null>(null);
  const [pageIndex, setPageIndex] = useState(0);
  const [pageBusy, setPageBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [pageError, setPageError] = useState('');
  const [note, setNote] = useState('');
  const client = useRef<WorkspaceShipmentsClient | null>(null);
  const abort = useRef<AbortController | null>(null);
  const token = useRef(0);
  const pageSeq = useRef(0);
  const factory = useRef(createClient); // fixed per mounted visit; an inline prop must not restart the visit

  // Invalidates every in-flight reply (acquisition and page) and aborts retrieval.
  const invalidate = useCallback(() => { token.current++; pageSeq.current++; abort.current?.abort(); abort.current = null; }, []);

  const acquire = useCallback(async (request: WorkspaceShipmentsRequest) => {
    const current = client.current;
    if (!current) return;
    const mine = ++token.current; pageSeq.current++;
    abort.current?.abort();
    const controller = new AbortController(); abort.current = controller;
    setLoading(true); setError(''); setPageError(''); setNote('');
    setOperation(null); setPage(null); setPageIndex(0); setPageBusy(false);
    try {
      const op: WorkspaceShipmentsAcquisition = await current.acquire(request, controller.signal);
      if (token.current !== mine) return;
      const first = await current.page(0, workspaceShipmentsPageSize, controller.signal);
      if (token.current !== mine) return;
      setOperation(op); setPage(first);
    } catch (failure) {
      if (token.current !== mine || controller.signal.aborted) return;
      setError(failureText(failure, 'Workspace shipments could not be retrieved. Refresh to try again.'));
    } finally {
      if (token.current === mine) setLoading(false);
    }
  }, []);

  // One client per mounted visit (created inside the effect so a StrictMode replay never reuses a released client).
  useEffect(() => {
    const visit = factory.current(assignmentId);
    client.current = visit;
    void Promise.resolve().then(() => { if (client.current === visit) void acquire(toRequest(initial)); });
    return () => {
      invalidate();
      client.current = null;
      void visit.release().catch(() => {});
    };
  }, [assignmentId, acquire, invalidate, initial]);

  useEffect(() => {
    const controller = new AbortController();
    loadSpec(assignmentId, controller.signal)
      .then(value => { if (!controller.signal.aborted) { setSpec(value); setSpecError(false); } })
      .catch(() => { if (!controller.signal.aborted) setSpecError(true); });
    return () => controller.abort();
  }, [assignmentId, loadSpec, specAttempt]);

  const dateClash = !!draft.from && !!draft.to && draft.from > draft.to;
  const dirty = !sameDraft(draft, applied);

  function apply() {
    if (loading || dateClash) return;
    const next = { ...draft, part: draft.part.trim() };
    setDraft(next); setApplied(next);
    void acquire(toRequest(next));
  }
  function refresh() { if (!loading) void acquire(toRequest(applied)); }
  function cancel() {
    invalidate();
    setLoading(false); setPageBusy(false); setNote('Retrieval cancelled. Refresh to retrieve again.');
  }
  async function goTo(index: number) {
    const current = client.current;
    if (!operation || pageBusy || !current) return;
    const mine = token.current; const wanted = ++pageSeq.current;
    setPageBusy(true); setPageError('');
    try {
      const next = await current.page(index * workspaceShipmentsPageSize, workspaceShipmentsPageSize);
      if (token.current === mine && pageSeq.current === wanted) { setPage(next); setPageIndex(index); }
    } catch (failure) {
      if (token.current === mine && pageSeq.current === wanted) setPageError(failureText(failure, 'That page could not be loaded. Refresh to retrieve again.'));
    } finally {
      if (token.current === mine && pageSeq.current === wanted) setPageBusy(false);
    }
  }

  const columns = new Map((spec?.columns ?? []).map(c => [c.id, c] as const));
  const effective = operation?.columns ?? [];
  const rowCount = operation ? Number(operation.rowCount) : 0;
  const totalPages = Math.max(1, Math.ceil(rowCount / workspaceShipmentsPageSize));
  const first = pageIndex * workspaceShipmentsPageSize;
  const unavailable = operation ? Number(operation.unavailableEnrichmentRows) : 0;
  const bothBlank = !draft.from && !draft.to;

  return <section className="workspace-shipments" aria-label="Workspace Shipments" aria-busy={loading}>
    <div className="workspace-shipments__toolbar">
      <h3>Workspace Shipments</h3>
      <div className="workspace-shipments__summary" aria-live="polite" aria-atomic="true">
        <span>On Time Rate</span>
        <strong>{formatWorkspaceShipmentsOnTimeRate(operation?.onTimeNumerator ?? null, operation?.onTimeDenominator ?? null)}</strong>
      </div>
      <button type="button" disabled={loading} onClick={refresh}>Refresh</button>
    </div>
    <form className="workspace-shipments__filters" onSubmit={event => { event.preventDefault(); apply(); }}>
      <label>Ship Date From
        <input type="date" value={draft.from} aria-invalid={dateClash || undefined} onChange={e => setDraft({ ...draft, from: e.target.value })} /></label>
      <label>Ship Date To
        <input type="date" value={draft.to} aria-invalid={dateClash || undefined} onChange={e => setDraft({ ...draft, to: e.target.value })} /></label>
      <label>Part Number
        <input type="text" value={draft.part} maxLength={30} autoComplete="off" spellCheck={false} placeholder="All parts"
          onChange={e => setDraft({ ...draft, part: e.target.value })} /></label>
      <button type="submit" disabled={loading || dateClash}>Apply</button>
    </form>
    <p className="workspace-shipments__hint">
      {bothBlank ? 'Both dates are blank: all available shipment history will be included. ' : 'Entering one date leaves the other end open; dates are inclusive. '}
      Part Number must match exactly; leave it blank for all parts.
    </p>
    {dateClash && <p role="alert" className="workspace-shipments__warning">Ship Date To must be on or after Ship Date From.</p>}
    {dirty && !dateClash && <p role="status" className="workspace-shipments__draft">Filters changed. Apply to use them; Refresh keeps the applied filters.</p>}
    <p className="workspace-shipments__meta">
      Applied: {describe(toRequest(applied))}
      {operation && <> · {rowCount.toLocaleString()} invoice detail rows · retrieved {operation.acquiredAtUtc ? new Date(operation.acquiredAtUtc).toLocaleString() : ''}</>}
    </p>
    {loading && <div role="status" className="workspace-shipments__loading">Retrieving workspace shipments…
      <button type="button" onClick={cancel}>Cancel</button></div>}
    {note && <p role="status">{note}</p>}
    {error && <p role="alert" className="workspace-shipments__warning">{error}</p>}
    {specError && <p role="alert" className="workspace-shipments__warning">Column definitions could not be loaded.{' '}
      <button type="button" onClick={() => setSpecAttempt(n => n + 1)}>Retry</button></p>}
    {operation && rowCount === 0 && <p role="status">No invoiced shipments matched these filters in this workspace.</p>}
    {operation && rowCount > 0 && page && spec && <>
      <p className="workspace-shipments__hint">Each row is one invoice detail; returns, memo and adjustment activity appear with signed quantities.
        Prices are the retro prices. Values are not net shipments or accounting revenue.
        {unavailable > 0 && ` ${unavailable.toLocaleString()} rows have unavailable current-master information (shown as “—”).`}</p>
      <div className="workspace-shipments__grid" tabIndex={0} role="region" aria-label="Workspace shipments table">
        <table>
          <thead><tr>{effective.map(id => <th key={id} scope="col" className={columns.get(id)?.kind === 'decimal' ? 'workspace-shipments__number' : undefined}>{columns.get(id)?.label ?? id}</th>)}</tr></thead>
          <tbody>{page.rows.map(row => <tr key={row.physicalDetailId}>{row.cells.map((value, i) => <td key={i}
            className={columns.get(effective[i])?.kind === 'decimal' ? 'workspace-shipments__number' : undefined}>
            {effective[i] === 'onTime' && (value === '1' || value === '0')
              ? <span role="img" aria-label={value === '1' ? 'On Time: True' : 'On Time: False'}
                className={`workspace-shipments__on-time workspace-shipments__on-time--${value === '1' ? 'true' : 'false'}`}>{value === '1' ? '✓' : '✕'}</span>
              : value ?? '—'}</td>)}</tr>)}</tbody>
        </table>
      </div>
      {pageError && <p role="alert" className="workspace-shipments__warning">{pageError}</p>}
      <nav className="workspace-shipments__pager" aria-label="Result pages">
        <button type="button" disabled={pageBusy || pageIndex === 0} onClick={() => void goTo(0)}>First</button>
        <button type="button" disabled={pageBusy || pageIndex === 0} onClick={() => void goTo(pageIndex - 1)}>Previous</button>
        <span>Rows {(first + 1).toLocaleString()}–{(first + page.rows.length).toLocaleString()} of {rowCount.toLocaleString()} · Page {(pageIndex + 1).toLocaleString()} of {totalPages.toLocaleString()}</span>
        <button type="button" disabled={pageBusy || pageIndex + 1 >= totalPages} onClick={() => void goTo(pageIndex + 1)}>Next</button>
        <button type="button" disabled={pageBusy || pageIndex + 1 >= totalPages} onClick={() => void goTo(totalPages - 1)}>Last</button>
      </nav>
    </>}
  </section>;
}

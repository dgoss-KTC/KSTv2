import { useEffect, useMemo, useRef, useState } from 'react';
import { ApiClient, ApiError, type OpenOrdersResponseDto } from '../api/client';
import { resolveBackendBaseUrl } from '../api/tauri-bridge';
import { saveLongTermShortagesWorkbook } from '../longTermShortages/saveLongTermShortagesWorkbook';
import { COLUMNS, DATE_COLUMNS, NUMERIC_COLUMNS, defaultLayout, emptyFilters, formatReportDate, formatReportTimestamp, loadLayout, reportRows, saveLayout, type Filters, type Layout, type ColumnId } from '../openOrders/report';
import { FilterBuilder } from '../openOrders/FilterBuilder';
import { openOrdersFileName } from '../openOrders/reportFileName';
import './OpenOrdersPanel.css';

type Failure = 'snapshot-changed' | 'unavailable' | 'error';

export function OpenOrdersPanel({ assignmentId, snapshotId, workspaceName, site = '' }: { assignmentId: string; snapshotId: string | null; workspaceName?: string | null; site?: string }) {
  const [layoutState, setLayout] = useState<{ identity: string; value: Layout }>(() => ({ identity: assignmentId, value: loadLayout(assignmentId) }));
  const [filterState, setFilters] = useState<{ identity: string; value: Filters }>(() => ({ identity: assignmentId, value: emptyFilters }));
  const [loaded, setLoaded] = useState<{ identity: string; report: OpenOrdersResponseDto } | null>(null);
  const [pendingIdentity, setPending] = useState<string | null>(null);
  const [failureState, setFailure] = useState<{ identity: string; kind: Failure } | null>(null);
  const [feedback, setFeedback] = useState<{ identity: string; text: string; kind: 'saved' | 'cancelled' | 'error' } | null>(null);
  const [exportingState, setExporting] = useState<{ identity: string; active: boolean } | null>(null);
  const [page, setPage] = useState(0);
  const requestId = useRef(0);
  const feedbackTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const identity = `${assignmentId}:${snapshotId ?? ''}`;
  const exporting = exportingState?.identity === identity && exportingState.active;
  const layout = layoutState.identity === assignmentId ? layoutState.value : loadLayout(assignmentId);
  const filters = filterState.identity === assignmentId ? filterState.value : emptyFilters;
  const pending = pendingIdentity === identity;
  const failure = failureState?.identity === identity ? failureState.kind : null;
  const report = loaded?.identity === identity ? loaded.report : null;
  const rows = useMemo(() => reportRows(report?.lines ?? [], filters), [report, filters]);
  const visible = layout.order.filter(id => layout.visible.includes(id));
  const pageSize = 100;
  const pageCount = Math.max(1, Math.ceil(rows.length / pageSize));
  const currentPage = Math.min(page, pageCount - 1);

  useEffect(() => {
    const timer = feedbackTimer;
    return () => {
      if (timer.current) clearTimeout(timer.current);
    };
  }, [identity]);

  const showFeedback = (text: string, kind: 'saved' | 'cancelled' | 'error') => {
    if (feedbackTimer.current) clearTimeout(feedbackTimer.current);
    setFeedback({ identity, text, kind });
    feedbackTimer.current = kind === 'error' ? null : setTimeout(() => {
      setFeedback(current => current?.identity === identity && current.text === text ? null : current);
      feedbackTimer.current = null;
    }, 5_000);
  };

  useEffect(() => {
    if (!snapshotId) { requestId.current++; return; }
    const current = ++requestId.current;
    queueMicrotask(() => { if (current === requestId.current) { setPending(identity); setFailure(null); } });
    void resolveBackendBaseUrl().then(url => new ApiClient(url).getOpenOrders(assignmentId, snapshotId))
      .then(data => { if (current === requestId.current) { setLoaded({ identity, report: data }); setPage(0); } })
      .catch((error: unknown) => { if (current === requestId.current) setFailure({ identity, kind: error instanceof ApiError && error.status === 409 ? 'snapshot-changed' : error instanceof ApiError && error.status === 503 ? 'unavailable' : 'error' }); })
      .finally(() => { if (current === requestId.current) setPending(null); });
    return () => { if (requestId.current === current) requestId.current = current + 1; };
  }, [assignmentId, snapshotId, identity]);

  const refresh = async () => {
    if (!snapshotId || pending) return;
    const current = ++requestId.current;
    setPending(identity); setFailure(null); setFeedback(null);
    try {
      const data = await new ApiClient(await resolveBackendBaseUrl()).refreshOpenOrders(assignmentId, snapshotId);
      if (current === requestId.current) { setLoaded({ identity, report: data }); setPage(0); }
    } catch (error) {
      if (current === requestId.current) setFailure({ identity, kind: error instanceof ApiError && error.status === 409 ? 'snapshot-changed' : error instanceof ApiError && error.status === 503 ? 'unavailable' : 'error' });
    } finally { if (current === requestId.current) setPending(null); }
  };
  const changeLayout = (next: Layout) => {
    setLayout({ identity: assignmentId, value: next });
    try { saveLayout(assignmentId, next); } catch { showFeedback('Column layout could not be saved. Changes remain available until this session ends.', 'error'); }
  };
  const move = (source: ColumnId, target: ColumnId) => {
    const order = [...layout.order];
    const from = order.indexOf(source); const to = order.indexOf(target);
    if (from < 0 || to < 0 || from === to) return;
    order.splice(from, 1);
    order.splice(to, 0, source);
    changeLayout({ ...layout, order });
  };
  const applyFilters = (next: Filters) => { setFilters({ identity: assignmentId, value: next }); setPage(0); };
  const visibleMove = (source: ColumnId, offset: -1 | 1) => {
    const index = visible.indexOf(source);
    if (index < 0 || index + offset < 0 || index + offset >= visible.length) return;
    move(source, visible[index + offset]);
  };

  if (!snapshotId) return <p className="open-orders__state">Load the MPS dashboard before viewing Customer Open Orders.</p>;
  return <section className="open-orders" aria-label="Customer Open Orders Report Mode">
    <header className="open-orders__toolbar"><h3>Customer Open Orders <span>Report Mode</span></h3>
      <button type="button" disabled={pending} onClick={() => void refresh()}>{pending ? 'Refreshing…' : 'Refresh report'}</button>
      <button type="button" disabled={!report || rows.length === 0 || visible.length === 0 || pending || exporting || !!failure} onClick={async () => {
        if (!report) return;
        if (feedbackTimer.current) clearTimeout(feedbackTimer.current);
        setFeedback(null); setExporting({ identity, active: true });
        try {
          const workbook = await new ApiClient(await resolveBackendBaseUrl()).exportOpenOrdersReport(assignmentId, {
            mpsSnapshotId: snapshotId, openOrdersSnapshotId: report.openOrdersSnapshotId,
            lineKeys: rows.map(r => r.key), columns: visible,
          });
          const result = await saveLongTermShortagesWorkbook(workbook.blob,
            workbook.fileName ?? openOrdersFileName(workspaceName, site || report.site, report.acquiredAtUtc), 'Save Customer Open Orders Workbook');
          showFeedback(result.kind === 'cancelled' ? 'Save cancelled; report remains available.' : `Saved ${result.fileName}`, result.kind === 'cancelled' ? 'cancelled' : 'saved');
        } catch { showFeedback('Workbook export or save failed. Report remains available; please retry.', 'error'); }
        finally { setExporting({ identity, active: false }); }
      }}>{exporting ? 'Preparing workbook…' : 'Export filtered rows to XLSX'}</button>
      {feedback?.identity === identity && <span role="status" className={`open-orders__feedback open-orders__feedback--${feedback.kind}`}>{feedback.text}</span>}
    </header>
    {pending && <p role="status">{report ? 'Refreshing report; displaying previous acquisition until complete.' : 'Loading Customer Open Orders…'}</p>}
    {!pending && !report && !failure && <p role="status">Loading Customer Open Orders…</p>}
    {failure && <p role="alert" className="open-orders__warning">{failure === 'snapshot-changed' ? 'MPS snapshot changed. Reload the workspace MPS dashboard, then reopen this report.' : failure === 'unavailable' ? 'Open Orders source unavailable. Retry refresh.' : 'Open Orders could not be loaded. Retry refresh.'}{report && ' Previous report is retained but not current; do not use for operational/QXtend validation.'}</p>}
    {report?.isStale && <p role="alert" className="open-orders__warning">STALE report acquired {formatReportTimestamp(report.acquiredAtUtc)}. {report.warning} Not ready for operational/QXtend validation.</p>}
    {report && <>
      <p className="open-orders__meta">Acquired {formatReportTimestamp(report.acquiredAtUtc)} · {report.lines.length} scoped lines · {rows.length} matching filters. {report.isStale || failure || pending ? 'Not current; do not use for operational/QXtend validation.' : 'Report only; not operational/QXtend validation.'}</p>
      <FilterBuilder key={assignmentId} filters={filters} onChange={applyFilters} />
      <details className="open-orders__columns"><summary>Columns ({visible.length} visible)</summary>
        <div className="open-orders__columns-actions"><span>Drag table headers to reorder; hidden columns keep their relative order.</span>
          <button type="button" onClick={() => changeLayout(defaultLayout())}>Reset columns to default</button></div>
        <div className="open-orders__column-choices">{layout.order.map(id => { const column = COLUMNS.find(c => c.id === id)!; return <label key={id}>
          <input type="checkbox" checked={layout.visible.includes(id)} disabled={layout.visible.length === 1 && layout.visible.includes(id)} onChange={e => changeLayout({ ...layout, visible: e.target.checked ? [...layout.visible, id] : layout.visible.filter(value => value !== id) })} />{column.label}
        </label>; })}</div>
      </details>
      {rows.length === 0 ? <p className="open-orders__state">{report.lines.length === 0 ? 'No open orders in this workspace scope.' : 'No open orders match the current filters.'}</p> : <>
        <div className="open-orders__page"><span>Rows {currentPage * pageSize + 1}–{Math.min((currentPage + 1) * pageSize, rows.length)} of {rows.length}</span>
          <button type="button" disabled={currentPage === 0} onClick={() => setPage(currentPage - 1)}>Previous page</button>
          <span>Page {currentPage + 1} of {pageCount}</span>
          <button type="button" disabled={currentPage + 1 >= pageCount} onClick={() => setPage(currentPage + 1)}>Next page</button></div>
        <div className="open-orders__grid" role="region" tabIndex={0} aria-label="Scrollable Customer Open Orders report">
          <table><thead><tr>{visible.map(id => <th scope="col" key={id} draggable className={NUMERIC_COLUMNS.has(id) ? 'open-orders__num' : undefined}
            onDragStart={event => { event.dataTransfer.setData('text/plain', id); event.dataTransfer.effectAllowed = 'move'; }}
            onDragOver={event => event.preventDefault()}
            onDrop={event => { event.preventDefault(); const source = event.dataTransfer.getData('text/plain'); if (visible.includes(source as ColumnId)) move(source as ColumnId, id); }}>
            <span>{COLUMNS.find(c => c.id === id)!.label}</span><span className="open-orders__header-moves">
              <button type="button" disabled={visible[0] === id} aria-label={`Move ${COLUMNS.find(c => c.id === id)!.label} Left`} onClick={() => visibleMove(id, -1)}>‹</button>
              <button type="button" disabled={visible[visible.length - 1] === id} aria-label={`Move ${COLUMNS.find(c => c.id === id)!.label} Right`} onClick={() => visibleMove(id, 1)}>›</button>
            </span></th>)}</tr></thead>
            <tbody>{rows.slice(currentPage * pageSize, (currentPage + 1) * pageSize).map(r => <tr className="open-orders__row" key={`${r.key.domain}:${r.key.salesOrder}:${r.key.line}`}>
              {visible.map(id => { const value = COLUMNS.find(c => c.id === id)!.value(r); return <td className={NUMERIC_COLUMNS.has(id) ? 'open-orders__num' : id === 'itemNumber' || id === 'order' ? 'open-orders__identity' : undefined} key={id}>
                {value === null ? '' : DATE_COLUMNS.has(id) ? formatReportDate(String(value)) : typeof value === 'boolean' ? value ? 'Yes' : 'No' : String(value)}
              </td>; })}</tr>)}</tbody></table>
        </div>
      </>}
    </>}
  </section>;
}

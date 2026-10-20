import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ApiClient, ApiError, type FinishedGoodsResponseDto } from '../api/client';
import { resolveBackendBaseUrl } from '../api/tauri-bridge';
import { FINISHED_GOODS_COLUMNS, finishedGoodsRows } from '../finishedGoods/report';
import { FinishedGoodsFilter } from '../finishedGoods/FinishedGoodsFilter';
import type { ColumnFilter } from './shared/ColumnFilterEditor';
import { formatDecimal } from '../shared/exactDecimal';
import { formatReportTimestamp } from '../openOrders/report';
import './FinishedGoodsPanel.css';

interface Props { assignmentId: string; snapshotId: string | null; site: string }

// A new mounted session for each scope protects A→B→A, including requests that ignore cancellation.
export function FinishedGoodsPanel(props: Props) {
  if (!props.snapshotId) return <section className="finished-goods"><div className="finished-goods__toolbar"><h3>Finished Goods</h3></div><p className="finished-goods__state">Load the workspace MPS first to resolve its part scope.</p></section>;
  return <FinishedGoodsSession key={JSON.stringify([props.assignmentId, props.snapshotId, props.site])} {...props} snapshotId={props.snapshotId} />;
}

function FinishedGoodsSession({ assignmentId, snapshotId, site }: Props & { snapshotId: string }) {
  const [report, setReport] = useState<FinishedGoodsResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<ColumnFilter[]>([]);
  const [openColumn, setOpenColumn] = useState<string | null>(null);
  const anchor = useRef<HTMLElement | null>(null);
  const request = useRef(0);
  const active = useRef(false);

  const acquire = useCallback(async (refresh: boolean) => {
    const token = ++request.current;
    setOpenColumn(null);
    setLoading(true); setError(null);
    try {
      const client = new ApiClient(await resolveBackendBaseUrl());
      if (!active.current || token !== request.current) return;
      const loaded = refresh ? await client.refreshFinishedGoods(assignmentId, snapshotId) : await client.getFinishedGoods(assignmentId, snapshotId);
      if (!active.current || token !== request.current) return;
      if (loaded.workspaceId !== assignmentId || loaded.mpsSnapshotId !== snapshotId || loaded.site !== site || loaded.currency !== 'USD')
        throw new ApiError(409, '', 'Scope mismatch');
      setReport(loaded);
    } catch (failure) {
      if (!active.current || token !== request.current) return;
      if (failure instanceof ApiError && (failure.status === 409 || failure.status === 404)) {
        setReport(null);
        setError('Workspace scope changed or is no longer available. Reload the workspace MPS before trying again.');
      } else {
        setError('Finished Goods acquisition failed. Refresh to try again.');
        // Transport failure must not present retained quantities as a current successful refresh.
        setReport(previous => previous ? { ...previous, isStale: true, warning: 'Showing the last known Finished Goods inventory. Refresh could not be completed; these quantities may have changed.' } : null);
      }
    } finally {
      if (active.current && token === request.current) setLoading(false);
    }
  }, [assignmentId, snapshotId, site]);

  useEffect(() => {
    active.current = true;
    // Defer initial acquisition so no synchronous effect state update is required.
    void Promise.resolve().then(() => { if (active.current) void acquire(false); });
    return () => { active.current = false; };
  }, [acquire]);

  const rows = useMemo(() => finishedGoodsRows(report?.rows ?? [], filters), [report, filters]);
  const changeFilters = (next: ColumnFilter[]) => {
    setFilters(next); setOpenColumn(null);
  };

  return <section className="finished-goods" aria-label="Finished Goods inventory" aria-busy={loading}>
    <div className="finished-goods__toolbar"><h3>Finished Goods</h3>
      <span className="finished-goods__meta">{report ? `Acquired: ${formatReportTimestamp(report.acquiredAtUtc)} · USD` : 'Current positive on-hand inventory · USD'}</span>
      <button className="finished-goods__primary" type="button" aria-label="Refresh Finished Goods" disabled={loading} onClick={() => void acquire(true)}>Refresh</button>
    </div>
    {loading && <p className="finished-goods__state" role="status">{report ? 'Refreshing Finished Goods…' : 'Loading Finished Goods…'}</p>}
    {report?.isStale && <p role="alert" className="finished-goods__warning">{report.warning ?? 'Showing stale Finished Goods inventory. Refresh to try again.'}</p>}
    {error && <p role="alert" className="finished-goods__warning">{error}</p>}
    {report && <>
      <div className="finished-goods__grid" tabIndex={0} role="region" aria-label="Finished Goods table">
        <table><thead><tr>{FINISHED_GOODS_COLUMNS.map(column => {
          const filtered = filters.some(f => f.column === column.id);
          return <th key={column.id} scope="col" className={`${column.kind === 'decimal' ? 'finished-goods__number ' : ''}${filtered ? 'finished-goods__active' : ''}`}>
            <span>{column.label}</span><button className="finished-goods__funnel" type="button" aria-label={`Filter ${column.label}${filtered ? ', filter active' : ''}`}
              aria-haspopup="dialog" aria-expanded={openColumn === column.id} disabled={loading}
              onClick={event => { anchor.current = event.currentTarget; setOpenColumn(openColumn === column.id ? null : column.id); }}>
              <svg width="14" height="14" viewBox="0 0 16 16" fill={filtered ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="1.4" aria-hidden="true"><path d="M2.5 3.5h11l-4.2 5v4l-2.6-1.4V8.5z" /></svg>
            </button>
          </th>;
        })}</tr></thead>
          <tbody>{rows.map(row => <tr key={row.rowId}>
            <td className="finished-goods__product" title={row.reference == null ? undefined : `Reference: ${row.reference}`}>{row.product}</td>
            <td>{row.location ?? ''}</td>
            <td>{row.lot ?? ''}{row.isRma && <span className="finished-goods__rma" title="Informational return-material lot; included in on-hand and value">RMA</span>}</td>
            <td className="finished-goods__number" title={row.onHand}>{formatDecimal(row.onHand, null)}</td>
            <td className="finished-goods__number" title={`USD ${row.value}${row.isMissingPrice ? ' · No price records; valued at $0' : ''}`}>${formatDecimal(row.value, 2)}</td>
          </tr>)}</tbody>
        </table>
        {rows.length === 0 && <p className="finished-goods__state" role="status">{report.rows.length === 0 ? 'No positive on-hand inventory in this workspace scope.' : 'No inventory matches these filters.'}</p>}
      </div>
      <div className="finished-goods__footer">
        <span role="status">Rows {rows.length.toLocaleString()}{filters.length > 0 && ` · filtered from ${report.rows.length.toLocaleString()}`}</span>
        {filters.length > 0 && <button type="button" disabled={loading} onClick={() => changeFilters([])}>Clear filters ({filters.length})</button>}
      </div>
    </>}
    {openColumn && report && <FinishedGoodsFilter key={openColumn} field={openColumn} rows={report.rows} filters={filters} anchor={anchor}
      onClose={() => setOpenColumn(null)} onApply={filter => changeFilters([...filters.filter(f => f.column !== openColumn), ...(filter ? [filter] : [])])} />}
  </section>;
}

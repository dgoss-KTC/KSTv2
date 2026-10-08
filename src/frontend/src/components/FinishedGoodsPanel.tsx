import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ApiClient, ApiError, type FinishedGoodsResponseDto } from '../api/client';
import { resolveBackendBaseUrl } from '../api/tauri-bridge';
import { emptyFinishedGoodsFilters, finishedGoodsRows, type FinishedGoodsFilters } from '../finishedGoods/report';
import { formatDecimal } from '../shared/exactDecimal';
import { formatReportTimestamp } from '../openOrders/report';
import './FinishedGoodsPanel.css';

interface Props { assignmentId: string; snapshotId: string | null; site: string }

// A new mounted session for each scope protects A→B→A, including requests that ignore cancellation.
export function FinishedGoodsPanel(props: Props) {
  if (!props.snapshotId) return <section className="finished-goods"><h3>Finished Goods</h3><p>Load the workspace MPS first to resolve its part scope.</p></section>;
  return <FinishedGoodsSession key={JSON.stringify([props.assignmentId, props.snapshotId, props.site])} {...props} snapshotId={props.snapshotId} />;
}

function FinishedGoodsSession({ assignmentId, snapshotId, site }: Props & { snapshotId: string }) {
  const [report, setReport] = useState<FinishedGoodsResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<FinishedGoodsFilters>(emptyFinishedGoodsFilters);
  const [page, setPage] = useState(0);
  const request = useRef(0);
  const active = useRef(false);

  const acquire = useCallback(async (refresh: boolean) => {
    const token = ++request.current;
    setLoading(true); setError(null);
    try {
      const client = new ApiClient(await resolveBackendBaseUrl());
      if (!active.current || token !== request.current) return;
      const loaded = refresh ? await client.refreshFinishedGoods(assignmentId, snapshotId) : await client.getFinishedGoods(assignmentId, snapshotId);
      if (!active.current || token !== request.current) return;
      if (loaded.workspaceId !== assignmentId || loaded.mpsSnapshotId !== snapshotId || loaded.site !== site || loaded.currency !== 'USD')
        throw new ApiError(409, '', 'Scope mismatch');
      setReport(loaded); setPage(0);
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
  const pageSize = 100;
  const pageCount = Math.max(1, Math.ceil(rows.length / pageSize));
  const currentPage = Math.min(page, pageCount - 1);
  const updateFilter = (field: keyof FinishedGoodsFilters, value: string) => {
    setFilters(previous => ({ ...previous, [field]: value })); setPage(0);
  };

  return <section className="finished-goods" aria-label="Finished Goods inventory" aria-busy={loading}>
    <div className="finished-goods__toolbar"><h3>Finished Goods</h3>
      <button type="button" disabled={loading} onClick={() => void acquire(true)}>Refresh Finished Goods</button>
    </div>
    <p className="finished-goods__meta">{report ? `Acquired: ${formatReportTimestamp(report.acquiredAtUtc)} · USD · ${rows.length} of ${report.rows.length} rows` : 'Current positive on-hand inventory · USD'}</p>
    {loading && <p role="status">{report ? 'Refreshing Finished Goods…' : 'Loading Finished Goods…'}</p>}
    {report?.isStale && <p role="alert" className="finished-goods__warning">{report.warning ?? 'Showing stale Finished Goods inventory. Refresh to try again.'}</p>}
    {error && <p role="alert" className="finished-goods__warning">{error}</p>}
    <div className="finished-goods__filters">
      {(['product', 'location', 'lot'] as const).map(field => <label key={field}>{field[0].toUpperCase() + field.slice(1)} filter
        <input value={filters[field]} onChange={event => updateFilter(field, event.target.value)} type="text" />
      </label>)}
    </div>
    {report && <>
      <div className="finished-goods__grid" tabIndex={0} role="region" aria-label="Finished Goods table">
        <table><thead><tr>{['Product', 'Location', 'Lot', 'On Hand', 'Value'].map(label => <th key={label} scope="col">{label}</th>)}</tr></thead>
          <tbody>{rows.slice(currentPage * pageSize, (currentPage + 1) * pageSize).map(row => <tr key={row.rowId}>
            <td title={row.reference == null ? undefined : `Reference: ${row.reference}`}>{row.product}</td>
            <td>{row.location ?? ''}</td>
            <td>{row.lot ?? ''}{row.isRma && <span className="finished-goods__rma" title="Informational return-material lot; included in on-hand and value">RMA</span>}</td>
            <td className="finished-goods__number" title={row.onHand}>{formatDecimal(row.onHand, null)}</td>
            <td className="finished-goods__number" title={`USD ${row.value}${row.isMissingPrice ? ' · No price records; valued at $0' : ''}`}>${formatDecimal(row.value, 2)}</td>
          </tr>)}</tbody>
        </table>
      </div>
      {rows.length === 0 && <p role="status">{report.rows.length === 0 ? 'No positive on-hand inventory in this workspace scope.' : 'No inventory matches these filters.'}</p>}
      {pageCount > 1 && <div className="finished-goods__page">
        <button disabled={currentPage === 0} onClick={() => setPage(currentPage - 1)}>Previous</button>
        <span>Page {currentPage + 1} of {pageCount}</span>
        <button disabled={currentPage + 1 === pageCount} onClick={() => setPage(currentPage + 1)}>Next</button>
      </div>}
    </>}
  </section>;
}

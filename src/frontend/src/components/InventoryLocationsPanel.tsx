import { useInventoryLocations } from '../hooks/useInventoryLocations';
import { decimal, exactText, formatDecimal } from '../shared/exactDecimal';

function formatInventoryQuantity(value: string | null): string {
  if (value === null) return '—';
  const parsed = decimal(value);
  return parsed ? formatDecimal(value, exactText(parsed).includes('.') ? 3 : 0) : value;
}

export function InventoryLocationsPanel({ assignmentId, componentPart, snapshotId, site }: {
  assignmentId: string | null; componentPart: string; snapshotId: string | null; site: string | null;
}) {
  const { report, loading, error, refresh } = useInventoryLocations(assignmentId, componentPart, snapshotId, site);
  return <section className="component-info-modal__section">
    <h3>Inventory / Lot Locations</h3>
    <button type="button" className="component-info-modal__retry-btn" onClick={refresh} disabled={loading || !snapshotId}>
      {error || report?.isStale ? 'Retry inventory' : 'Refresh inventory'}
    </button>
    {loading && <p role="status">Loading inventory locations…</p>}
    {error && <p role="alert">{error}</p>}
    {report?.isStale && <p role="alert" className="component-info-modal__banner">{report.warning}</p>}
    {report && <>
      <p className="component-info-modal__inventory-time">Acquired: <time dateTime={report.acquiredAtUtc}>{report.acquiredAtUtc}</time></p>
      {report.rows.length === 0 ? <p>No inventory locations found.</p> :
        <div className="component-info-modal__inventory-scroll" tabIndex={0} role="region" aria-label="Scrollable inventory locations">
          <table className="component-info-modal__inventory-table" aria-label="Inventory / Lot Locations">
            <thead><tr>{['Location', 'Lot/Serial', 'Status', 'Qty on Hand'].map(label => <th key={label} scope="col">{label}</th>)}</tr></thead>
            <tbody>{report.rows.map(row => <tr key={row.rowId}>
              <td>{row.location ?? '—'}</td><td>{row.lotSerial ?? '—'}</td><td>{row.status ?? '—'}</td>
              <td className="component-info-modal__inventory-qty" title={row.quantityOnHand ?? undefined}>{formatInventoryQuantity(row.quantityOnHand)}</td>
            </tr>)}</tbody>
          </table>
        </div>}
    </>}
  </section>;
}

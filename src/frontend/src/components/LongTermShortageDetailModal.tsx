import type { ComponentOrderLine } from '../api/componentOrdersApi';
import type { LongTermShortagePurchasing, LongTermShortageScreenRow, LongTermShortageProjectionDetail } from '../api/longTermShortagesApi';
import { formatLineConfirmation } from '../componentOrders/componentOrdersPresentation';
import { formatLongTermQuantity } from '../longTermShortages/longTermShortagesPresentation';

/** Selected-component presentation only: the full projection/evidence remains in the API and workbook. */
export function ShortageInformationSections({ row, projection, isProjectionLoading, projectionError, onRetryProjection, asOfDate, purchasing, isLoading, error, onRetry }: {
  row: LongTermShortageScreenRow;
  projection: LongTermShortageProjectionDetail | null;
  isProjectionLoading: boolean;
  projectionError: boolean;
  onRetryProjection: () => void;
  asOfDate?: string;
  purchasing: LongTermShortagePurchasing | null;
  isLoading: boolean;
  error: boolean;
  onRetry: () => void;
}) {
  const today = asOfDate ?? localToday();
  const openLines = purchasing?.openPurchaseOrders ?? [];
  // The source is the Stage 10 conventional-open PO path; no additional PO qualification here.
  const lines = [...openLines].sort((a, b) => {
    const rank = (line: ComponentOrderLine) => line.dueDate && line.dueDate < today ? 0 : line.dueDate ? 1 : 2;
    return rank(a) - rank(b) || (a.dueDate ?? '').localeCompare(b.dueDate ?? '') || a.poNumber.localeCompare(b.poNumber) || Number(a.poLine) - Number(b.poLine);
  });
  return <div className="long-term-shortage-detail">
    <section className="component-info-modal__section"><h3>Shortage Snapshot</h3>
      <dl className="component-info-modal__grid long-term-shortage-detail__snapshot">
        <div className="component-info-modal__field"><dt>Severity</dt><dd>{row.severity === 'SafetyStockUnavailable' ? 'SS Unknown' : row.severity}</dd></div>
        <div className="component-info-modal__field"><dt>First Short Date</dt><dd>{row.firstShortDate ?? '—'}</dd></div>
        <div className="component-info-modal__field"><dt>Opening QOH</dt><dd>{formatLongTermQuantity(row.openingQoh, row.unitOfMeasure)}</dd></div>
        <div className="component-info-modal__field"><dt>UOM</dt><dd>{row.unitOfMeasure ?? 'Unknown'}</dd></div>
        <div className="component-info-modal__field"><dt>KSS</dt><dd>{row.isKss ? 'KSS' : 'No'}</dd></div>
        <div className="component-info-modal__field long-term-shortage-detail__parents"><dt>Demand Parents</dt><dd>{projection ? projection.demandParentParts.join(', ') || 'No workspace parents' : isProjectionLoading ? 'Loading…' : 'Unavailable'}</dd></div>
      </dl>
      {row.dataQualityWarning && <p className="long-term-shortage-detail__warning" role="status">{row.dataQualityWarning}</p>}
    </section>
    <section className="component-info-modal__section"><h3>Past</h3>
      {isProjectionLoading ? <p role="status">Loading snapshot projection detail…</p>
        : projectionError || !projection ? <p role="alert">Snapshot projection detail unavailable. Refresh Workspace Shortages if the snapshot changed. <button type="button" onClick={onRetryProjection}>Retry projection detail</button></p>
          : <dl className="component-info-modal__grid">
            <div className="component-info-modal__field"><dt>Past Gross Requirements</dt><dd>{formatLongTermQuantity(projection.pastGrossRequirements, row.unitOfMeasure)}</dd></div>
            <div className="component-info-modal__field"><dt>Overdue Receipts (excluded)</dt><dd>{formatLongTermQuantity(projection.overdueReceipts, row.unitOfMeasure)}</dd></div>
            <div className="component-info-modal__field"><dt>Adjusted Opening QOH</dt><dd>{formatLongTermQuantity(projection.adjustedOpeningQoh, row.unitOfMeasure)}</dd></div>
          </dl>}
    </section>
    <section className="component-info-modal__section"><h3>Current Buyer Comment</h3>
      {isLoading ? <p className="component-info-modal__placeholder">Loading buyer comment…</p>
        : error || !purchasing?.commentAvailable ? <p role="status">Buyer comment unavailable.</p>
          : <p className="long-term-shortage-detail__comment">{purchasing.currentComment?.trim() || 'No buyer comment on file'}</p>}
    </section>
    <section className="component-info-modal__section"><h3>Open Purchase Orders</h3>
      {isLoading ? <p className="component-info-modal__placeholder">Loading open purchase orders…</p>
        : error ? <p role="alert">Open purchase orders unavailable. <button type="button" onClick={onRetry}>Retry purchase orders</button></p>
          : lines.length === 0 ? <p className="component-info-modal__placeholder">No open purchase orders for this component.</p>
            : <div className="long-term-shortage-detail__po-scroll"><table className="long-term-shortage-detail__table"><thead><tr><th>PO</th><th>Line</th><th>Due</th><th>Open Qty</th><th>Confirmed</th><th>Supplier</th></tr></thead><tbody>
              {lines.map((line, index) => <tr key={`${line.poNumber}-${line.poLine}-${index}`}><td>{line.poNumber}</td><td>{line.poLine}</td><td className={line.dueDate && line.dueDate < today ? 'long-term-shortage-detail__past-due' : ''}>{line.dueDate ? <>{line.dueDate < today && <span>Past due: </span>}{line.dueDate}</> : 'Due date missing'}</td><td className="long-term-shortage-detail__quantity">{formatLongTermQuantity(line.openQuantity, row.unitOfMeasure)}</td><td>{formatLineConfirmation(line.confirmed) || 'Unknown'}</td><td>{line.supplierDisplay ?? '—'}</td></tr>)}
            </tbody></table></div>}
    </section>
  </div>;
}

function localToday(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

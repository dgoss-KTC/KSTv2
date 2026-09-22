import { useCallback, useEffect, useRef } from 'react';
import type { LongTermMrpFact, LongTermShortageBucket, LongTermShortageRow } from '../api/longTermShortagesApi';
import { formatLongTermDate, formatLongTermQuantity } from '../longTermShortages/longTermShortagesPresentation';

export function LongTermShortageDetailModal({ row, onClose }: { row: LongTermShortageRow; onClose: () => void }) {
  const closeButton = useRef<HTMLButtonElement>(null);
  const dialog = useRef<HTMLDivElement>(null);
  useEffect(() => { closeButton.current?.focus(); }, []);
  useEffect(() => {
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => { document.body.style.overflow = previousOverflow; };
  }, []);
  useEffect(() => {
    const escape = (event: KeyboardEvent) => { if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); onClose(); } };
    document.addEventListener('keydown', escape, true);
    return () => document.removeEventListener('keydown', escape, true);
  }, [onClose]);
  const handleKeyDown = useCallback((event: React.KeyboardEvent<HTMLDivElement>) => {
    if (event.key !== 'Tab') return;
    const focusable = dialog.current?.querySelectorAll<HTMLElement>('button:not([disabled]), [tabindex]:not([tabindex="-1"])');
    if (!focusable?.length) return;
    const first = focusable[0]; const last = focusable[focusable.length - 1];
    if ((!event.shiftKey && document.activeElement === last) || (event.shiftKey && document.activeElement === first)) { event.preventDefault(); (event.shiftKey ? last : first).focus(); }
  }, []);
  return <div className="component-info-modal-backdrop">
      <div ref={dialog} className="component-info-modal long-term-shortage-detail" role="dialog" aria-modal="true" aria-labelledby="long-term-shortage-detail-title" onKeyDown={handleKeyDown}>
      <div className="component-info-modal__header">
        <div className="component-info-modal__header-row"><h2 id="long-term-shortage-detail-title" className="component-info-modal__title">Workspace Shortages Component Information</h2><button ref={closeButton} type="button" className="component-info-modal__close-btn" onClick={onClose} aria-label="Close Workspace Shortages Component Information">&#10005;</button></div>
        <div className="component-info-modal__identity-row"><div className="component-info-modal__identity"><span className="component-info-modal__part">{row.componentPart}</span><span className="component-info-modal__description">{row.description ?? 'No description'}</span></div></div>
      </div>
      <div className="component-info-modal__body">
        <section className="component-info-modal__section"><h3>Projection Summary</h3><dl className="component-info-modal__grid"><div className="component-info-modal__field"><dt>B/P</dt><dd>{row.buyerPlannerCode ?? 'Unavailable'}</dd></div><div className="component-info-modal__field"><dt>Demand</dt><dd>{row.demandParentParts.join(', ') || 'No workspace parents'}</dd></div><div className="component-info-modal__field"><dt>Opening QOH</dt><dd>{formatLongTermQuantity(row.openingQoh)}</dd></div><div className="component-info-modal__field"><dt>Safety Stock</dt><dd>{row.safetyStockState === 'SelectedSiteValueMissing' ? 'Selected-site value missing' : formatLongTermQuantity(row.safetyStock)}</dd></div></dl></section>
        <section className="component-info-modal__section"><h3>PO/KSS Context</h3><dl className="component-info-modal__grid"><div className="component-info-modal__field"><dt>Manufacturer Item</dt><dd>{row.presentation?.manufacturerItem ?? 'Unavailable'}</dd></div><div className="component-info-modal__field"><dt>KSS</dt><dd>{row.presentation?.isKss ? 'KSS' : 'No'}</dd></div><div className="component-info-modal__field"><dt>PO</dt><dd>{row.presentation?.poNumber ?? 'Unavailable'}</dd></div><div className="component-info-modal__field"><dt>PO Open Qty</dt><dd>{formatLongTermQuantity(row.presentation?.poOpenQuantity)}</dd></div></dl></section>
        <section className="component-info-modal__section"><h3>Past</h3><BucketDetail bucket={row.past} /></section>
        <section className="component-info-modal__section"><h3>24-Week Timeline</h3><table className="long-term-shortage-detail__table"><thead><tr><th>Week</th><th>Gross Requirements</th><th>Scheduled Receipts</th><th>Planned Orders Due</th><th>Planned Orders Release</th><th>Projected QOH</th></tr></thead><tbody>{row.weeks.map((week) => <tr key={week.weekNumber}><td>W{week.weekNumber} ({formatLongTermDate(week.weekStart ?? '')})</td><td>{formatLongTermQuantity(week.grossRequirements)}</td><td>{formatLongTermQuantity(week.scheduledReceipts)}</td><td>{formatLongTermQuantity(week.plannedOrdersDue)}</td><td>{formatLongTermQuantity(week.plannedOrdersRelease)}</td><td aria-label={`Week ${week.weekNumber}: ${week.severity}, raw projected QOH ${week.projectedQoh}`}>{formatLongTermQuantity(week.projectedQoh)} {week.severity !== 'Clear' && `(${week.severity})`}</td></tr>)}</tbody></table></section>
        <section className="component-info-modal__section"><h3>Raw MRP Evidence</h3>{row.evidence.length === 0 ? <p>No raw MRP evidence returned.</p> : <table className="long-term-shortage-detail__table"><thead><tr><th>Ordinal</th><th>Type</th><th>Due</th><th>Release</th><th>Quantity</th><th>Category</th></tr></thead><tbody>{row.evidence.map((fact) => <EvidenceRow key={fact.evidenceOrdinal} fact={fact} />)}</tbody></table>}</section>
      </div>
    </div>
  </div>;
}

function BucketDetail({ bucket }: { bucket: LongTermShortageBucket }) {
  return <dl className="component-info-modal__grid"><div className="component-info-modal__field"><dt>Gross Requirements</dt><dd>{formatLongTermQuantity(bucket.grossRequirements)}</dd></div><div className="component-info-modal__field"><dt>Scheduled Receipts</dt><dd>{formatLongTermQuantity(bucket.scheduledReceipts)}</dd></div><div className="component-info-modal__field"><dt>Planned Orders Due</dt><dd>{formatLongTermQuantity(bucket.plannedOrdersDue)}</dd></div><div className="component-info-modal__field"><dt>Planned Orders Release</dt><dd>{formatLongTermQuantity(bucket.plannedOrdersRelease)}</dd></div><div className="component-info-modal__field"><dt>Projected QOH</dt><dd>{formatLongTermQuantity(bucket.projectedQoh)}</dd></div></dl>;
}

function EvidenceRow({ fact }: { fact: LongTermMrpFact }) {
  return <tr><td>{fact.evidenceOrdinal}</td><td>{fact.type ?? ''}</td><td>{fact.dueDate ? formatLongTermDate(fact.dueDate) : ''}</td><td>{fact.releaseDate ? formatLongTermDate(fact.releaseDate) : ''}</td><td>{formatLongTermQuantity(fact.quantity)}</td><td>{fact.category}</td></tr>;
}

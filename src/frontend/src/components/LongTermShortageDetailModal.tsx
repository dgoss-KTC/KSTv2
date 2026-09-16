import { useEffect, useRef } from 'react';
import type { LongTermShortageRow } from '../api/longTermShortagesApi';
import { formatLongTermDate, formatLongTermQuantity } from '../longTermShortages/longTermShortagesPresentation';

export function LongTermShortageDetailModal({ row, onClose }: { row: LongTermShortageRow; onClose: () => void }) {
  const closeButton = useRef<HTMLButtonElement>(null);
  const dialog = useRef<HTMLDivElement>(null);
  useEffect(() => { closeButton.current?.focus(); }, []);
  useEffect(() => {
    const escape = (event: KeyboardEvent) => { if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); onClose(); } };
    document.addEventListener('keydown', escape, true);
    return () => document.removeEventListener('keydown', escape, true);
  }, [onClose]);
  return <div className="component-info-modal-backdrop">
    <div ref={dialog} className="component-info-modal long-term-shortage-detail" role="dialog" aria-modal="true" aria-labelledby="long-term-shortage-detail-title" onKeyDown={(event) => {
      if (event.key !== 'Tab') return;
      const focusable = dialog.current?.querySelectorAll<HTMLElement>('button:not([disabled]), [tabindex]:not([tabindex="-1"])');
      if (!focusable?.length) return;
      const first = focusable[0]; const last = focusable[focusable.length - 1];
      if ((!event.shiftKey && document.activeElement === last) || (event.shiftKey && document.activeElement === first)) { event.preventDefault(); (event.shiftKey ? last : first).focus(); }
    }}>
      <div className="component-info-modal__header">
        <div className="component-info-modal__header-row"><h2 id="long-term-shortage-detail-title" className="component-info-modal__title">Workspace Shortages Component Information</h2><button ref={closeButton} type="button" className="component-info-modal__close-btn" onClick={onClose} aria-label="Close Workspace Shortages Component Information">&#10005;</button></div>
        <div className="component-info-modal__identity-row"><div className="component-info-modal__identity"><span className="component-info-modal__part">{row.componentPart}</span><span className="component-info-modal__description">{row.description ?? 'No description'}</span></div></div>
      </div>
      <div className="component-info-modal__body">
        <section className="component-info-modal__section"><h3>Projection Summary</h3><dl className="component-info-modal__grid"><div className="component-info-modal__field"><dt>B/P</dt><dd>{row.buyerPlannerCode ?? 'Unavailable'}</dd></div><div className="component-info-modal__field"><dt>Demand</dt><dd>{row.demandParentParts.join(', ') || 'No workspace parents'}</dd></div><div className="component-info-modal__field"><dt>Safety Stock</dt><dd>{row.safetyStockState === 'SelectedSiteValueMissing' ? 'Selected-site value missing' : formatLongTermQuantity(row.displaySafetyStock)}</dd></div><div className="component-info-modal__field"><dt>KSS</dt><dd>{row.isKss ? 'KSS classification only' : 'Not KSS'}</dd></div></dl></section>
        <section className="component-info-modal__section"><h3>24-Week Timeline</h3><table className="long-term-shortage-detail__table"><thead><tr><th>Week</th><th>Demand</th><th>Supply</th><th>Balance</th></tr></thead><tbody>{row.weeks.map((week) => <tr key={week.weekNumber}><td>W{week.weekNumber} ({formatLongTermDate(week.weekStart)})</td><td>{formatLongTermQuantity(week.displayDemand)}</td><td>{formatLongTermQuantity(week.displayPurchaseOrderSupply)}</td><td aria-label={`Week ${week.weekNumber}: ${week.severity}, raw calculated balance ${week.balance}, displayed balance ${formatLongTermQuantity(week.displayBalance)}`}>{formatLongTermQuantity(week.displayBalance)} {week.severity !== 'None' && `(${week.severity})`}</td></tr>)}</tbody></table><p>Forecast demand is gross and potentially overstated because forecast consumption is not modeled.</p></section>
        <section className="component-info-modal__section"><h3>Demand and Supply Evidence</h3><p>Other-program parent attribution: {row.otherProgramParentParts.join(', ') || 'None'}.</p><p>Purchase orders are context only and do not assert coverage of a work order.</p>{row.purchaseOrders.length === 0 ? <p>No in-horizon PO context.</p> : <ul>{row.purchaseOrders.map((po) => <li key={`${po.poNumber}-${po.poLine}`}>{po.poNumber} line {po.poLine}: {formatLongTermDate(po.dueDate ?? '')}, {formatLongTermQuantity(po.displayOpenQuantity)}, {po.confirmed ? 'confirmed' : 'unconfirmed'}{po.isScheduled ? ', scheduled/KSS' : ''}</li>)}</ul>}</section>
      </div>
    </div>
  </div>;
}

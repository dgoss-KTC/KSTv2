import type { WorkOrderSummaryDto } from '../api/client';
import { formatKittingPercent, formatOptionalDate, formatQuantity, kittingPercentValue, workOrderStatusLabel } from '../mps/mpsPresentation';
import './WorkOrderCard.css';

export type WorkOrderShortageState = 'shortage' | 'dataIssue' | 'clear' | 'unavailable';

interface WorkOrderCardProps {
  workOrder: WorkOrderSummaryDto;
  isOpen?: boolean;
  shortageState?: WorkOrderShortageState;
  onToggle?: (trigger: HTMLButtonElement) => void;
}

/** Compact Stage 7R card; the Stage 9 material detail is deliberately owned by the shared panel below the strip. */
export function WorkOrderCard({ workOrder, isOpen = false, shortageState = 'unavailable', onToggle = () => {} }: WorkOrderCardProps) {
  const kittingPercent = kittingPercentValue(workOrder.kitting.kittingPercent);
  const kittingText = formatKittingPercent(workOrder.kitting.kittingPercent);
  const shortageLabel = shortageState === 'shortage' ? 'Has Shortage' : shortageState === 'dataIssue' ? 'Unknown / Data Issue' : shortageState === 'clear' ? 'No current Shortage' : 'Shortage summary unavailable';
  return (
    <li className={`work-order-card work-order-card--${shortageState}${isOpen ? ' work-order-card--open' : ''}`} aria-label={`Work order ${workOrder.woid}, ${workOrderStatusLabel(workOrder.status)}, ${shortageLabel}`}>
      <div className="work-order-card__summary">
        <div className="work-order-card__header"><span className="work-order-card__id">{workOrder.woid}</span><span className={`work-order-card__status work-order-card__status--${workOrder.status}`}>{workOrderStatusLabel(workOrder.status)}</span>{workOrder.salesOrder && <span className="work-order-card__so">SO {workOrder.salesOrder}</span>}</div>
        <div className="work-order-card__shortage-state">{shortageLabel}</div>
        <div className="work-order-card__fields"><div className="work-order-card__quantity-fields"><span className="work-order-card__field"><label>Ordered</label>{formatQuantity(workOrder.orderedQuantity)}</span><span className="work-order-card__field"><label>Completed</label>{formatQuantity(workOrder.completedQuantity)}</span><span className="work-order-card__field"><label>Open</label>{formatQuantity(workOrder.openQuantity)}</span></div><div className="work-order-card__date-fields"><span className="work-order-card__field"><label>Release</label>{formatOptionalDate(workOrder.releaseDate)}</span><span className="work-order-card__field"><label>Due</label>{formatOptionalDate(workOrder.dueDate)}</span></div></div>
        <div className="work-order-card__kitting"><div className="work-order-card__kitting-label"><span>Kitting</span><span className="work-order-card__kitting-value">{kittingText}</span></div><div className="work-order-card__kitting-track" role="progressbar" aria-label={`Kitting ${kittingText} for ${workOrder.woid}`} aria-valuenow={kittingPercent ?? undefined} aria-valuemin={0} aria-valuemax={100}>{kittingPercent !== null && <div className="work-order-card__kitting-fill" style={{ width: `${kittingPercent}%` }} />}</div><button type="button" className="work-order-card__expand-btn" aria-expanded={isOpen} onClick={(event) => onToggle(event.currentTarget)}>{isOpen ? 'Hide material lines' : 'Show material lines'}</button></div>
      </div>
    </li>
  );
}

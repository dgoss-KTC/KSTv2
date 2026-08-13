import type { WorkOrderSummaryDto } from '../api/client';
import type { WorkOrdersApiError } from '../api/workOrdersApi';
import { WorkOrderCard } from './WorkOrderCard';
import './WorkOrdersPanel.css';

interface WorkOrdersPanelProps {
  parentPart: string;
  bucketLabel: string;
  assignmentId: string;
  snapshotId: string | null;
  workOrders: WorkOrderSummaryDto[] | null;
  isLoading: boolean;
  error: WorkOrdersApiError | null;
  onRetry: () => void;
}

/**
 * Stage 7D.6/7D.7: selection/tab plumbing for the Work Orders investigation context. Renders
 * every field the accepted Stage 7 card contract requires as compact status-badged
 * `WorkOrderCard`s with a Kitting % progress presentation and expand/collapse material drill-down.
 */
export function WorkOrdersPanel({
  parentPart,
  bucketLabel,
  assignmentId,
  snapshotId,
  workOrders,
  isLoading,
  error,
  onRetry,
}: WorkOrdersPanelProps) {
  return (
    <div className="work-orders-panel" aria-label={`Work orders for ${parentPart}, ${bucketLabel}`}>
      <div className="work-orders-panel__header">
        <h3 className="work-orders-panel__title">
          Work Orders &mdash; {parentPart} &middot; {bucketLabel}
        </h3>
      </div>

      {isLoading && <div className="work-orders-panel__state">Loading work orders&hellip;</div>}

      {!isLoading && error?.type === 'stale' && (
        <div className="work-orders-panel__state work-orders-panel__state--error">
          <p>This schedule context is out of date. Refresh the MPS grid and reselect the bucket.</p>
          <button type="button" className="work-orders-panel__retry-btn" onClick={onRetry}>
            Retry
          </button>
        </div>
      )}

      {!isLoading && error?.type === 'error' && (
        <div className="work-orders-panel__state work-orders-panel__state--error">
          <p>{error.detail}</p>
          <button type="button" className="work-orders-panel__retry-btn" onClick={onRetry}>
            Retry
          </button>
        </div>
      )}

      {!isLoading && !error && workOrders && workOrders.length === 0 && (
        <div className="work-orders-panel__state work-orders-panel__state--empty">
          No eligible work orders (Allocating, Frozen, or Released) for this bucket.
        </div>
      )}

      {!isLoading && !error && workOrders && workOrders.length > 0 && (
        <ul className="work-orders-panel__list">
          {workOrders.map((wo) => (
            <WorkOrderCard key={wo.woid} workOrder={wo} assignmentId={assignmentId} snapshotId={snapshotId} />
          ))}
        </ul>
      )}
    </div>
  );
}

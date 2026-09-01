import type { WorkOrderSummaryDto } from '../api/client';
import type { WorkOrdersApiError } from '../api/workOrdersApi';
import { WorkOrderCard } from './WorkOrderCard';
import './WorkOrdersPanel.css';

interface WorkOrdersPanelProps {
  parentPart: string;
  /** 'Planning window' for the parent-level view, or the bucket description (e.g. 'Week of …'). */
  bucketLabel: string;
  assignmentId: string;
  snapshotId: string | null;
  workOrders: WorkOrderSummaryDto[] | null;
  isLoading: boolean;
  error: WorkOrdersApiError | null;
  onRetry: () => void;
  dateBasis: string;
}

/**
 * Stage 7/7R: selection/tab plumbing for the Work Orders investigation context. Renders the
 * planning-window population (Due-Date-based Falldown + Week 0-3 under the active basis, or a
 * single bucket) as compact status-badged `WorkOrderCard`s with a Kitting % progress presentation
 * and expand/collapse material drill-down.
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
  dateBasis,
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
          No work orders in the planning window for this part.
        </div>
      )}

      {!isLoading && !error && workOrders && workOrders.length > 0 && (
        <ul className="work-orders-panel__list">
          {workOrders.map((wo) => (
            <WorkOrderCard key={wo.woid} workOrder={wo} assignmentId={assignmentId} snapshotId={snapshotId} dateBasis={dateBasis} />
          ))}
        </ul>
      )}
    </div>
  );
}

import { useId, useState } from 'react';
import type { WorkOrderSummaryDto } from '../api/client';
import { useWorkOrderMaterialLines } from '../hooks/useWorkOrderMaterialLines';
import { useEscapeLevel } from '../mps/escapeStack';
import {
  formatKittingPercent,
  formatOptionalDate,
  formatQuantity,
  kittingPercentValue,
  workOrderStatusLabel,
} from '../mps/mpsPresentation';
import { WorkOrderMaterialGrid } from './WorkOrderMaterialGrid';
import './WorkOrderCard.css';

interface WorkOrderCardProps {
  workOrder: WorkOrderSummaryDto;
  assignmentId: string;
  snapshotId: string | null;
  /** 1 = top-level bucket Work Order (default). Nested manufactured-candidate cards (Stage 7D.9)
   * pass a higher depth so the material grid can disable further drill at the max depth. */
  depth?: number;
  dateBasis: string;
}

/**
 * Stage 7D.7 compact Work Order card: WOID/status/quantities/dates plus a Kitting % progress
 * presentation and an expand/collapse control that lazily loads material lines, rendered via the
 * Stage 7D.8 sortable/filterable/styled `WorkOrderMaterialGrid`. This component is deliberately
 * generic so Stage 7D.9 candidate subassembly Work Orders reuse it directly (via
 * `WorkOrderCandidatePanel`) at an incremented `depth`, rather than a parallel card component.
 */
export function WorkOrderCard({ workOrder, assignmentId, snapshotId, depth = 1, dateBasis }: WorkOrderCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  // Escape collapses material lines exactly like clicking "Hide material lines" — one drill-down
  // level up, not all the way back to the grid.
  useEscapeLevel(isExpanded, () => setIsExpanded(false));
  const materialSectionId = useId();
  const { lines, isLoading, error, retry } = useWorkOrderMaterialLines(
    assignmentId,
    snapshotId,
    workOrder.woid,
    isExpanded,
  );
  const kittingPercent = kittingPercentValue(workOrder.kitting.kittingPercent);
  const kittingText = formatKittingPercent(workOrder.kitting.kittingPercent);

  return (
    <li
      className={`work-order-card${isExpanded ? ' work-order-card--expanded' : ''}`}
      aria-label={`Work order ${workOrder.woid}, ${workOrderStatusLabel(workOrder.status)}`}
    >
      <div className="work-order-card__summary">
        <div className="work-order-card__header">
          <span className="work-order-card__id">{workOrder.woid}</span>
          <span className={`work-order-card__status work-order-card__status--${workOrder.status}`}>
            {workOrderStatusLabel(workOrder.status)}
          </span>
          {workOrder.salesOrder && (
            <span className="work-order-card__so">SO {workOrder.salesOrder}</span>
          )}
        </div>

        <div className="work-order-card__fields">
          <div className="work-order-card__quantity-fields">
            <span className="work-order-card__field">
              <label>Ordered</label>
              {formatQuantity(workOrder.orderedQuantity)}
            </span>
            <span className="work-order-card__field">
              <label>Completed</label>
              {formatQuantity(workOrder.completedQuantity)}
            </span>
            <span className="work-order-card__field">
              <label>Open</label>
              {formatQuantity(workOrder.openQuantity)}
            </span>
          </div>
          <div className="work-order-card__date-fields">
            <span className="work-order-card__field">
              <label>Release</label>
              {formatOptionalDate(workOrder.releaseDate)}
            </span>
            <span className="work-order-card__field">
              <label>Due</label>
              {formatOptionalDate(workOrder.dueDate)}
            </span>
          </div>
        </div>

        <div className="work-order-card__kitting">
          <div className="work-order-card__kitting-label">
            <span>Kitting</span>
            <span className="work-order-card__kitting-value">{kittingText}</span>
          </div>
          <div
            className="work-order-card__kitting-track"
            role="progressbar"
            aria-label={`Kitting ${kittingText} for ${workOrder.woid}`}
            aria-valuenow={kittingPercent ?? undefined}
            aria-valuemin={0}
            aria-valuemax={100}
          >
            {kittingPercent !== null && (
              <div className="work-order-card__kitting-fill" style={{ width: `${kittingPercent}%` }} />
            )}
          </div>
          <button
            type="button"
            className="work-order-card__expand-btn"
            aria-expanded={isExpanded}
            aria-controls={materialSectionId}
            onClick={() => setIsExpanded((expanded) => !expanded)}
          >
            <span
              className={`work-order-card__chevron${isExpanded ? ' work-order-card__chevron--open' : ''}`}
              aria-hidden="true"
            />
            {isExpanded ? 'Hide material lines' : 'Show material lines'}
          </button>
        </div>
      </div>

      {isExpanded && (
        <div id={materialSectionId} className="work-order-card__material">
          {isLoading && <div className="work-order-card__material-state">Loading material lines&hellip;</div>}

          {!isLoading && error && (
            <div className="work-order-card__material-state work-order-card__material-state--error">
              <p>
                {error.type === 'stale'
                  ? 'This schedule context is out of date. Refresh the MPS grid and reselect the bucket.'
                  : error.detail}
              </p>
              <button type="button" className="work-order-card__retry-btn" onClick={retry}>
                Retry
              </button>
            </div>
          )}

          {!isLoading && !error && lines && lines.length === 0 && (
            <div className="work-order-card__material-state work-order-card__material-state--empty">
              No applicable material lines for this work order.
            </div>
          )}

          {!isLoading && !error && lines && lines.length > 0 && (
            <WorkOrderMaterialGrid
              lines={lines}
              depth={depth}
              woid={workOrder.woid}
              assignmentId={assignmentId}
              snapshotId={snapshotId}
              dateBasis={dateBasis}
            />
          )}
        </div>
      )}
    </li>
  );
}

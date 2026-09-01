import { useWorkOrderCandidates } from '../hooks/useWorkOrderCandidates';
import { WorkOrderCard } from './WorkOrderCard';
import './WorkOrderCandidatePanel.css';

interface WorkOrderCandidatePanelProps {
  assignmentId: string;
  snapshotId: string | null;
  immediateParentWoid: string;
  componentPart: string;
  /** Depth to render the candidate cards at (immediate parent's depth + 1). */
  depth: number;
  dateBasis: string;
}

/**
 * Stage 7D.9: candidate subassembly Work Orders for one manufactured material component. QAD has
 * no reliable parent/subassembly WO relationship, so this uses truthful "Work Orders for <Part>"
 * framing rather than "Child/Linked/Related Work Orders", which would imply proven pegging.
 */
export function WorkOrderCandidatePanel({
  assignmentId,
  snapshotId,
  immediateParentWoid,
  componentPart,
  depth,
  dateBasis,
}: WorkOrderCandidatePanelProps) {
  const { candidates, isLoading, error, retry } = useWorkOrderCandidates(
    assignmentId,
    snapshotId,
    immediateParentWoid,
    componentPart,
    depth,
    dateBasis,
  );

  return (
    <div className="work-order-candidate-panel">
      <h4 className="work-order-candidate-panel__title">Work Orders for {componentPart}</h4>

      {isLoading && <div className="work-order-candidate-panel__state">Loading work orders&hellip;</div>}

      {!isLoading && error && (
        <div className="work-order-candidate-panel__state work-order-candidate-panel__state--error">
          <p>
            {error.type === 'stale'
              ? 'This schedule context is out of date. Refresh the MPS grid and reselect the bucket.'
              : error.detail}
          </p>
          <button type="button" className="work-order-candidate-panel__retry-btn" onClick={retry}>
            Retry
          </button>
        </div>
      )}

      {!isLoading && !error && candidates && candidates.length === 0 && (
        <div className="work-order-candidate-panel__state work-order-candidate-panel__state--empty">
          No work orders in the planning window for this part.
        </div>
      )}

      {!isLoading && !error && candidates && candidates.length > 0 && (
        <>
          <ul className="work-order-candidate-panel__list">
            {candidates.map((candidate) => (
              <WorkOrderCard
                key={candidate.woid}
                workOrder={candidate}
                assignmentId={assignmentId}
                snapshotId={snapshotId}
                depth={depth}
                dateBasis={dateBasis}
              />
            ))}
          </ul>
        </>
      )}
    </div>
  );
}

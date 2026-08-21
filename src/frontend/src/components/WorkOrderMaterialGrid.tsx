import { Fragment, useId, useMemo, useState } from 'react';
import type { WorkOrderMaterialLineDto } from '../api/client';
import { filterMaterialLinesByPart, formatQuantity, isMaterialLineException, sortMaterialLines } from '../mps/mpsPresentation';
import { useEscapeLevel } from '../mps/escapeStack';
import { WorkOrderCandidatePanel } from './WorkOrderCandidatePanel';
import './WorkOrderMaterialGrid.css';

const NO_VALUE = '\u2014';

function formatIssuedPercent(value: WorkOrderMaterialLineDto['issuedPercent']): string {
  if (value === null || value === undefined) return NO_VALUE;
  return `${formatQuantity(value)}%`;
}

interface WorkOrderMaterialGridProps {
  lines: WorkOrderMaterialLineDto[];
  /** 1 = the top-level bucket Work Order; higher values are nested manufactured-candidate drill
   * levels (Stage 7D.9). At the max drill depth, manufactured rows keep their indicator but the
   * drill affordance is disabled. */
  depth: number;
  /** The Work Order this material grid belongs to, needed as the immediate parent for a
   * manufactured row's candidate Work Order lookup. */
  woid: string;
  assignmentId: string;
  snapshotId: string | null;
}

/**
 * Stage 7D.8 Kitting material grid: exception-first default sort, a local Part Number filter, and
 * variance/manufactured row treatment. Stage 7D.9 wires the manufactured-row drill affordance to
 * an expandable candidate Work Orders branch — only one branch may be open per grid at a time
 * (selecting a different manufactured row collapses the prior one), per the accepted contract's
 * "avoid uncontrolled nested expansion" requirement.
 */
export function WorkOrderMaterialGrid({ lines, depth, woid, assignmentId, snapshotId }: WorkOrderMaterialGridProps) {
  const [filterText, setFilterText] = useState('');
  const [expandedRowKey, setExpandedRowKey] = useState<string | null>(null);
  // Escape collapses the open candidate branch one level up, same as clicking its drill button again.
  useEscapeLevel(expandedRowKey !== null, () => setExpandedRowKey(null));
  const filterInputId = useId();
  const baseId = useId();
  const canDrill = depth < 3;

  const sortedLines = useMemo(() => sortMaterialLines(lines), [lines]);
  const visibleLines = useMemo(() => filterMaterialLinesByPart(sortedLines, filterText), [sortedLines, filterText]);

  return (
    <div className="work-order-material-grid">
      <div className="work-order-material-grid__filter">
        <label htmlFor={filterInputId}>Filter by Part Number</label>
        <input
          id={filterInputId}
          type="text"
          value={filterText}
          onChange={(event) => setFilterText(event.target.value)}
          placeholder="e.g. COMP1"
        />
        {filterText.length > 0 && (
          <button
            type="button"
            className="work-order-material-grid__clear-btn"
            onClick={() => setFilterText('')}
          >
            Clear
          </button>
        )}
      </div>

      {visibleLines.length === 0 ? (
        <div className="work-order-material-grid__empty">No material lines match &ldquo;{filterText}&rdquo;.</div>
      ) : (
        <table className="work-order-material-grid__table">
          <thead>
            <tr>
              <th scope="col">Component</th>
              <th scope="col">Description</th>
              <th scope="col">BOM Qty</th>
              <th scope="col">Issued Qty</th>
              <th scope="col">Variance Qty</th>
              <th scope="col">Issued %</th>
            </tr>
          </thead>
          <tbody>
            {visibleLines.map((line, index) => {
              const exception = isMaterialLineException(line.issueStatus);
              const rowKey = `${line.componentPart}-${index}`;
              const isRowExpanded = expandedRowKey === rowKey;
              const candidateSectionId = `${baseId}-candidates-${index}`;
              const rowClassNames = [
                'work-order-material-grid__row',
                line.isManufactured ? 'work-order-material-grid__row--manufactured' : '',
                line.isManufactured && canDrill ? 'work-order-material-grid__row--drillable' : '',
              ]
                .filter(Boolean)
                .join(' ');
              return (
                <Fragment key={rowKey}>
                  <tr className={rowClassNames}>
                    <td className="work-order-material-grid__component">
                      {line.isManufactured && canDrill && (
                        <button
                          type="button"
                          className="work-order-material-grid__drill-btn"
                          aria-expanded={isRowExpanded}
                          aria-controls={candidateSectionId}
                          onClick={() => setExpandedRowKey((prev) => (prev === rowKey ? null : rowKey))}
                        >
                          <span
                            className={`work-order-material-grid__chevron${isRowExpanded ? ' work-order-material-grid__chevron--open' : ''}`}
                            aria-hidden="true"
                          />
                          {line.componentPart}
                        </button>
                      )}
                      {line.isManufactured && !canDrill && (
                        <>
                          <span
                            className="work-order-material-grid__chevron work-order-material-grid__chevron--disabled"
                            aria-hidden="true"
                            title="Maximum drill depth reached"
                          />
                          {line.componentPart}
                        </>
                      )}
                      {!line.isManufactured && line.componentPart}
                    </td>
                    <td>{line.componentDescription ?? NO_VALUE}</td>
                    <td>{formatQuantity(line.requiredQuantity)}</td>
                    <td>{formatQuantity(line.issuedQuantity)}</td>
                    <td>{formatQuantity(line.varianceQuantity)}</td>
                    <td className={exception ? 'work-order-material-grid__issued-pct--exception' : undefined}>
                      {formatIssuedPercent(line.issuedPercent)}
                    </td>
                  </tr>
                  {isRowExpanded && (
                    <tr className="work-order-material-grid__candidate-row">
                      <td id={candidateSectionId} colSpan={6}>
                        <WorkOrderCandidatePanel
                          assignmentId={assignmentId}
                          snapshotId={snapshotId}
                          immediateParentWoid={woid}
                          componentPart={line.componentPart}
                          depth={depth + 1}
                        />
                      </td>
                    </tr>
                  )}
                </Fragment>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}


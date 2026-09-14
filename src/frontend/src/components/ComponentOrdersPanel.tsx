import { useEffect, useRef, useState } from 'react';
import type { ComponentOrderGroup, ComponentOrderLine } from '../api/componentOrdersApi';
import { useComponentOrders } from '../hooks/useComponentOrders';
import {
  classifyPoDueState,
  describeComponentOrderDescription,
  EMPTY_COMPONENT_ORDERS_FILTERS,
  filterComponentOrderGroups,
  formatKssIndicator,
  formatLineConfirmation,
  formatWeeksLeadTime,
  poDueStateClass,
} from '../componentOrders/componentOrdersPresentation';
import { formatOptionalDate, formatQuantity } from '../mps/mpsPresentation';
import './ComponentOrdersPanel.css';

interface ComponentOrdersPanelProps {
  assignmentId: string;
  snapshotId: string | null;
}

/**
 * Stage 10.2 customer-workspace Component Orders module (QAD-only checkpoint). One collapsed row
 * per component shows its earliest qualifying conventional open PO line; an expand arrow appears
 * beside the component number when additional qualifying lines exist, and expanded child rows
 * repeat the component number with their own line values. Components whose earliest due date is
 * missing sort first as yellow exceptions; all other groups sort earliest-due to latest (PO, then
 * Line on ties).
 */
export function ComponentOrdersPanel({ assignmentId, snapshotId }: ComponentOrdersPanelProps) {
  const { data, isLoading, error, retry } = useComponentOrders(assignmentId, snapshotId);
  const [filters, setFilters] = useState(EMPTY_COMPONENT_ORDERS_FILTERS);
  const [expandedParts, setExpandedParts] = useState<ReadonlySet<string>>(new Set());
  const [commentDetail, setCommentDetail] = useState<{ componentPart: string; text: string } | null>(null);
  // Unknown/stale API values are unavailable so risk facts are never presented as current.
  const enrichmentAvailable = data?.enrichmentAvailability === 'Available';

  useEffect(() => {
    if (!enrichmentAvailable) {
      setFilters((current) =>
        current.creditHoldOnly || current.ciaOnly
          ? { ...current, creditHoldOnly: false, ciaOnly: false }
          : current);
    }
  }, [enrichmentAvailable]);

  if (!snapshotId) {
    return <div className="component-orders__state">Load the MPS dashboard before viewing Component Orders.</div>;
  }
  if (isLoading) {
    return <div className="component-orders__state">Loading component orders...</div>;
  }
  if (error) {
    const message =
      error.type === 'mps-not-loaded'
        ? "This workspace's MPS data has not been loaded yet. Load the MPS dashboard before viewing Component Orders."
        : error.type === 'stale'
          ? 'This schedule context is out of date. Refresh the MPS grid and retry.'
          : error.detail;
    return (
      <div className="component-orders__state component-orders__state--error">
        <p>{message}</p>
        <button type="button" onClick={retry}>Retry</button>
      </div>
    );
  }
  if (!data) return null;

  const groups = filterComponentOrderGroups(data.groups, enrichmentAvailable
    ? filters
    : { ...filters, creditHoldOnly: false, ciaOnly: false });
  const today = new Date();

  const toggleExpanded = (part: string) => {
    setExpandedParts((previous) => {
      const next = new Set(previous);
      if (next.has(part)) next.delete(part);
      else next.add(part);
      return next;
    });
  };

  return (
    <section className="component-orders">
      <header className="component-orders__toolbar">
        <h3>Component Orders</h3>
        <div className="component-orders__filters">
          <label className="component-orders__filter">
            Component number
            <input
              type="search"
              value={filters.componentNumber}
              onChange={(event) => setFilters((f) => ({ ...f, componentNumber: event.target.value }))}
              placeholder="Filter by part number"
            />
          </label>
          <label className="component-orders__filter component-orders__filter--checkbox">
            <input
              type="checkbox"
              checked={filters.kssOnly}
              onChange={(event) => setFilters((f) => ({ ...f, kssOnly: event.target.checked }))}
            />
            KSS only
          </label>
          <label className="component-orders__filter component-orders__filter--checkbox">
            <input
              type="checkbox"
              checked={filters.creditHoldOnly}
              disabled={!enrichmentAvailable}
              onChange={(event) => setFilters((f) => ({ ...f, creditHoldOnly: event.target.checked }))}
            />
            Credit Hold
          </label>
          <label className="component-orders__filter component-orders__filter--checkbox">
            <input
              type="checkbox"
              checked={filters.ciaOnly}
              disabled={!enrichmentAvailable}
              onChange={(event) => setFilters((f) => ({ ...f, ciaOnly: event.target.checked }))}
            />
            CIA
          </label>
        </div>
      </header>
      {!enrichmentAvailable && (
        <p className="component-orders__enrichment-notice" role="status">
          Shortages enrichment is unavailable. Credit Hold, CIA, and Current Comments may not be current.
        </p>
      )}
      {groups.length === 0 ? (
        <div className="component-orders__state">No components have qualifying open PO lines.</div>
      ) : (
        <div className="component-orders__scroll">
          <table>
            <thead>
              <tr>
                <th>Comp</th>
                <th>Description</th>
                <th>Weeks LT</th>
                <th>PO</th>
                <th>Line</th>
                <th>PO Due</th>
                <th>Open Qty</th>
                <th>Confirmed</th>
                <th>Supplier</th>
                <th>Buyer</th>
                <th>Mfg Item</th>
                <th>KSS</th>
                <th>Tracking Info</th>
                <th>Credit Hold</th>
                <th>CIA</th>
                <th>Current Comments</th>
              </tr>
            </thead>
            <tbody>
              {groups.map((group) => (
                <GroupRows
                  key={group.componentPart}
                  group={group}
                  today={today}
                  expanded={expandedParts.has(group.componentPart)}
                  onToggle={() => toggleExpanded(group.componentPart)}
                  enrichmentAvailable={enrichmentAvailable}
                  onOpenComment={(text) => setCommentDetail({ componentPart: group.componentPart, text })}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
      {commentDetail && <ComponentCommentDialog detail={commentDetail} onClose={() => setCommentDetail(null)} />}
    </section>
  );
}

interface GroupRowsProps {
  group: ComponentOrderGroup;
  today: Date;
  expanded: boolean;
  onToggle: () => void;
  enrichmentAvailable: boolean;
  onOpenComment: (text: string) => void;
}

function GroupRows({ group, today, expanded, onToggle, enrichmentAvailable, onOpenComment }: GroupRowsProps) {
  const hasMore = group.additionalLines.length > 0;
  return (
    <>
      <tr className="component-orders__row component-orders__row--collapsed">
        <td className="component-orders__comp">
          {hasMore && (
            <button
              type="button"
              className={`component-orders__expand${expanded ? ' component-orders__expand--open' : ''}`}
              onClick={onToggle}
              aria-expanded={expanded}
              aria-label={`${expanded ? 'Collapse' : 'Expand'} ${group.componentPart}`}
            >
              <span className="component-orders__chevron" aria-hidden="true" />
            </button>
          )}
          {group.componentPart}
        </td>
        <LineCells line={group.displayLine} today={today} enrichmentAvailable={enrichmentAvailable} onOpenComment={onOpenComment} />
      </tr>
      {expanded &&
        group.additionalLines.map((line) => (
          <tr key={`${line.poNumber}-${line.poLine}`} className="component-orders__row component-orders__row--child">
            <td className="component-orders__comp">{group.componentPart}</td>
            <LineCells line={line} today={today} enrichmentAvailable={enrichmentAvailable} />
          </tr>
        ))}
    </>
  );
}

function LineCells({ line, today, enrichmentAvailable, onOpenComment }: {
  line: ComponentOrderLine;
  today: Date;
  enrichmentAvailable: boolean;
  onOpenComment?: (text: string) => void;
}) {
  const dueState = classifyPoDueState(line.dueDate, today);
  const comment = line.currentComments;
  return (
    <>
      <td>{describeComponentOrderDescription(line.description)}</td>
      <td className="component-orders__num">{formatWeeksLeadTime(line.leadTimeDays)}</td>
      <td>{line.poNumber}</td>
      <td className="component-orders__num">{line.poLine}</td>
      <td className={poDueStateClass(dueState)}>{formatOptionalDate(line.dueDate)}</td>
      <td className="component-orders__num">{formatQuantity(line.openQuantity)}</td>
      <td>{formatLineConfirmation(line.confirmed)}</td>
      <td>{line.supplierDisplay ?? ''}</td>
      <td>{line.buyerDisplay ?? ''}</td>
      <td>{line.manufacturerItem ?? ''}</td>
      <td className="component-orders__kss">{formatKssIndicator(line.isKss)}</td>
      <td>{line.trackingInfo ?? ''}</td>
      <td className="component-orders__risk">{formatRiskFact(line.isCreditHold, enrichmentAvailable)}</td>
      <td className="component-orders__risk">{formatRiskFact(line.isCia, enrichmentAvailable)}</td>
      <td className="component-orders__comments">
        {enrichmentAvailable && onOpenComment && comment && (
          <button type="button" onClick={() => onOpenComment(comment)} aria-label={`View comments for ${line.componentPart}`}>
            {comment}
          </button>
        )}
        {!enrichmentAvailable ? '—' : ''}
      </td>
    </>
  );
}

function formatRiskFact(value: boolean | null | undefined, enrichmentAvailable: boolean): string {
  if (!enrichmentAvailable) return '—';
  return value === true ? '✓' : '';
}

function ComponentCommentDialog({ detail, onClose }: {
  detail: { componentPart: string; text: string };
  onClose: () => void;
}) {
  const closeRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    closeRef.current?.focus();
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        onClose();
      }
    };
    document.addEventListener('keydown', handleKeyDown, true);
    return () => document.removeEventListener('keydown', handleKeyDown, true);
  }, [onClose]);

  return (
    <div className="component-orders__comment-backdrop">
      <div className="component-orders__comment-dialog" role="dialog" aria-modal="true" aria-labelledby="component-orders-comment-title">
        <header>
          <h2 id="component-orders-comment-title">Current Comments - {detail.componentPart}</h2>
          <button ref={closeRef} type="button" onClick={onClose} aria-label="Close Current Comments">Close</button>
        </header>
        <pre>{detail.text}</pre>
      </div>
    </div>
  );
}

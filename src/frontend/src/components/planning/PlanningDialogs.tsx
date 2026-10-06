import type { PlanningProposals } from '../../hooks/usePlanningProposals';
import { ISSUE_MESSAGES, type EntryIssue } from '../../planningWorkbook/planningAdjustments';
import { formatPlanningDate, formatPlanningTimestamp } from '../../planningWorkbook/planningWorkbookSummary';
import { qty } from '../../planningWorkbook/planningView';
import { PlanningDialog } from './PlanningDialog';
import { ISSUE_LABELS } from './planningLabels';

const optionalQty = (value: string | null) => (value === null ? 'Unavailable' : qty(value));

/** Saved-versus-current comparison shown before an explicit restore confirmation. */
export function PlanningDraftReviewDialog({ proposals, returnFocus }: { proposals: PlanningProposals; returnFocus?: () => HTMLElement | null }) {
  const review = proposals.draft.review;
  if (!review) return null;
  const rows = review.draft.rows;
  const flagged = rows.filter(r => r.issueCode !== null).length;
  return (
    <PlanningDialog titleId="pwb-draft-review-title" title="Saved draft" onClose={proposals.cancelReview} wide returnFocus={returnFocus}
      actions={<>
        <button type="button" className="pwb-btn" onClick={() => void proposals.deleteDraft()} disabled={proposals.draft.busy !== null}>Delete saved draft</button>
        <span className="pwb-dialog__spacer" />
        <button type="button" className="pwb-btn" onClick={proposals.cancelReview}>Cancel</button>
        <button type="button" className="pwb-btn pwb-btn--primary" data-autofocus onClick={() => proposals.restoreDraft()}>Restore saved draft</button>
      </>}>
      <div role="region" aria-label="Saved draft review">
        <p>
          Saved {formatPlanningTimestamp(review.draft.savedAtUtc)}: {rows.length} {rows.length === 1 ? 'adjustment' : 'adjustments'}{flagged ? `, ${flagged} need attention` : ''}.
          {' '}Signed adjustments are kept as saved and applied to the MPS currently loaded; they are not converted to target quantities.
          {' '}Restoring replaces the changes in this session (Undo reverses it).
        </p>
        {review.draft.warning && <p className="pwb-notice pwb-notice--warning">{review.draft.warning}</p>}
        <div className="pwb-dialog__table-scroll">
          <table className="pwb-table" aria-label="Saved draft comparison">
            <thead>
              <tr><th scope="col">Part</th><th scope="col">Week</th><th scope="col">Adjustment</th><th scope="col">Saved MPS</th><th scope="col">Current MPS</th><th scope="col">Proposed MPS</th><th scope="col">Status</th></tr>
            </thead>
            <tbody>
              {rows.map(row => {
                const issue = row.issueCode as EntryIssue | null;
                return (
                  <tr key={`${row.part}|${row.weekLabel}`} className={issue ? 'pwb-table__row--issue' : undefined}>
                    <td className="pwb-mono">{row.part}</td>
                    <td>{formatPlanningDate(row.weekLabel)}</td>
                    <td>{row.adjustment}</td>
                    <td>{optionalQty(row.savedMpsQuantity)}</td>
                    <td>{optionalQty(row.currentMps)}{row.baselineChanged && <span className="pwb-table__changed"> (changed)</span>}</td>
                    <td>{optionalQty(row.proposedMps)}</td>
                    <td>{issue ? <><strong>{ISSUE_LABELS[issue] ?? issue}.</strong> {ISSUE_MESSAGES[issue] ?? ''}</> : 'Valid'}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </PlanningDialog>
  );
}

/**
 * Customer forecast import stub (spec §11.1). Inert: it reads no file, injects no sample forecast and
 * changes no data.
 */
export function PlanningForecastImportDialog({ onClose }: { onClose: () => void }) {
  return (
    <PlanningDialog titleId="pwb-forecast-title" title="Import customer forecast" onClose={onClose}
      tag={<span className="pwb-tag">Future iteration</span>}
      actions={<button type="button" className="pwb-btn" data-autofocus onClick={onClose}>Close</button>}>
      <div className="pwb-dropzone" aria-disabled="true">
        <span className="pwb-dropzone__title">Drop a forecast file here (.xlsx or .csv)</span>
        <span className="pwb-dropzone__sub">File import is not built yet</span>
      </div>
      <p>
        When this ships, the customer&apos;s weekly quantities load into planning mode on a row beside Adjust. Every week
        that differs from the forecast currently loaded in QAD is marked, so you can see where to update. Nothing is
        imported or changed by this dialog.
      </p>
    </PlanningDialog>
  );
}

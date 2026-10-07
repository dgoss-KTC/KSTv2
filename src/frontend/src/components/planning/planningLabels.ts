import type { PlanningWorkbookError } from '../../api/planningWorkbookApi';
import type { ExportFailure } from '../../hooks/usePlanningExport';
import type { EntryIssue } from '../../planningWorkbook/planningAdjustments';
import { formatLocalDate, type PlanningQuarter } from '../../planningWorkbook/planningWorkbookSummary';

export const ISSUE_LABELS: Record<EntryIssue, string> = {
  'invalid-number': 'Invalid number',
  'not-whole-number': 'Not a whole number',
  'negative-result': 'Negative Proposed MPS',
  'week-passed': 'Week passed',
  'part-out-of-scope': 'Part not in loaded MPS',
};

export function errorCopy(error: PlanningWorkbookError): { text: string; retry: boolean } {
  switch (error.type) {
    case 'not-found': return { text: 'This workspace was not found. It may have been deleted.', retry: false };
    case 'mps-not-loaded': return { text: 'MPS data is not loaded for this workspace. Load it from the Dashboard tab, then retry. Planning does not load or refresh MPS.', retry: true };
    case 'snapshot-changed': return { text: 'The workspace MPS data changed since this view was opened. Reload MPS on the Dashboard tab, then retry.', retry: true };
    case 'unavailable': return { text: 'Planning data is currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.', retry: true };
    default: return { text: 'Could not load Planning data. Try again.', retry: true };
  }
}

export const REFRESH_NOTE = "Refresh Planning also refreshes this workspace's Customer Open Orders report, so a prepared Open Orders export may need to be prepared again. MPS is not refreshed. Staged adjustments are kept.";
export const EXPORT_NO_CHANGES = 'Stage at least one adjustment to review and export the MPS change file.';
export const EXPORT_BUTTON_NOTE = 'Validates the staged adjustments against the loaded MPS data, then lets you review and save the QAD Mass Updater file. Nothing is applied to QAD.';
export const EXPORT_QTY_NOTE = 'Qty is the signed adjustment to the existing MPS quantity, not the resulting quantity. Date is the Monday of the week. The file has no header row.';
export const EXPORT_SAVED_NOTE = 'Saving the file does not mean QAD applied it: the file is a proposal for the external QAD Mass Updater, and KST submits and imports nothing.';
export const EXPORT_DRAFT_NOTE = 'Any saved Planning draft is unchanged.';
/** Browser fallback only: the browser cannot confirm that a file was written, so nothing is cleared. */
export const EXPORT_DOWNLOAD_NOTE = (fileName: string) =>
  `A download of ${fileName} was started. This browser cannot confirm that a file was saved, so nothing was cleared and every staged adjustment is kept. `
  + 'Check your downloads before relying on the file. The file is a proposal for the external QAD Mass Updater; nothing is applied to QAD.';

const EXPORT_REASONS: Record<string, string> = {
  'invalid-number': 'an adjustment is not a valid whole number',
  'not-whole-number': 'an adjustment is not a whole number',
  'not-a-week': 'an adjustment is not on a Monday week date',
  'week-passed': 'an adjustment is for a week that has passed',
  'part-out-of-scope': 'an adjustment is for a part that is not in the loaded MPS',
  'duplicate-identity': 'a part and week has more than one adjustment',
  'negative-result': 'an adjustment would make an MPS quantity negative',
  'not-adjustable': 'an adjustment targets Falldown, which cannot be adjusted',
  'no-adjustments': 'there are no nonzero adjustments to export',
  'invalid-request': 'the export request was not valid',
  'snapshot-changed': 'the MPS data changed',
};

/** Sanitized, user-facing explanation of why no export file was prepared. Never shows rows or quantities. */
export function exportFailureCopy(failure: ExportFailure): string {
  const tail = ' No file was created and your adjustments are unchanged.';
  switch (failure.type) {
    case 'rejected': {
      const reason = failure.code ? EXPORT_REASONS[failure.code] : undefined;
      const count = failure.count !== null && failure.count > 0 ? `${failure.count} ${failure.count === 1 ? 'adjustment needs' : 'adjustments need'} attention: ` : 'Export blocked: ';
      return reason ? `${count}${reason}.${tail}` : `The export was rejected.${tail}`;
    }
    case 'not-found': return `This workspace was not found. It may have been deleted.${tail}`;
    case 'mps-not-loaded': return `MPS data is not loaded for this workspace. Load it from the Dashboard tab, then try again.${tail}`;
    case 'snapshot-changed': return `The workspace MPS data changed. Reload MPS on the Dashboard tab, then export again.${tail}`;
    case 'site-changed': return `The workspace site changed. Reopen the workspace, then export again.${tail}`;
    case 'network': return `The export request did not reach the backend. Check the connection and try again.${tail}`;
    case 'file-mismatch': return `The prepared file did not match the staged adjustments, so it was discarded. Try again; if this repeats, contact IT.${tail}`;
    default: return `The export could not be prepared. Try again.${tail}`;
  }
}

export function quarterDetail(quarter: PlanningQuarter): string {
  const coverage = quarter.isPartial
    ? `Partial: ${quarter.weekIndexes.length} of ${quarter.quarterWeekCount} weeks shown.`
    : `All ${quarter.quarterWeekCount} weeks shown.`;
  return `${quarter.label} total, ${formatLocalDate(quarter.startDate)} \u2013 ${formatLocalDate(quarter.endDate)}. ${coverage}`
    + `${quarter.includesFalldown ? ' Includes Falldown once.' : ''} Flows total the weeks shown; projected on hand is the balance at the last shown week; forecast has no quarter total.`;
}

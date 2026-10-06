import type { PlanningWorkbookError } from '../../api/planningWorkbookApi';
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
export const EXPORT_UNAVAILABLE = 'Export is not available yet. Review and export of the MPS change file arrives in Stage 14.6.';

export function quarterDetail(quarter: PlanningQuarter): string {
  const coverage = quarter.isPartial
    ? `Partial: ${quarter.weekIndexes.length} of ${quarter.quarterWeekCount} weeks shown.`
    : `All ${quarter.quarterWeekCount} weeks shown.`;
  return `${quarter.label} total, ${formatLocalDate(quarter.startDate)} \u2013 ${formatLocalDate(quarter.endDate)}. ${coverage}`
    + `${quarter.includesFalldown ? ' Includes Falldown once.' : ''} Flows total the weeks shown; projected on hand is the balance at the last shown week; forecast has no quarter total.`;
}

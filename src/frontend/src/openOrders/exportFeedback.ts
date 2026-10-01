import { ApiError } from '../api/client';

/** Displays only sanitized issue categories and counts, never raw API payloads or line contents. */
export function qxtendExportFeedback(error: unknown): string {
  if (!(error instanceof ApiError)) return 'Export request failed before a response was received. No CSV files prepared; check the backend connection and retry.';
  let code: string | undefined;
  let count: number | undefined;
  try {
    const problem: unknown = JSON.parse(error.message);
    if (problem && typeof problem === 'object') {
      const fields = problem as Record<string, unknown>;
      if (typeof fields.issueCode === 'string') code = fields.issueCode;
      if (typeof fields.affectedRowCount === 'number' && Number.isSafeInteger(fields.affectedRowCount) && fields.affectedRowCount >= 0)
        count = fields.affectedRowCount;
    }
  } catch { /* A route 404 or proxy failure may not return Problem Details. */ }
  const affected = count === undefined ? 'Changed rows' : `${count} changed row${count === 1 ? '' : 's'}`;
  const reasons: Record<string, string> = {
    'invalid-request': 'the export request has invalid or duplicate line identities. Reload the report and retry.',
    'invalid-values-or-reason': 'a value or Reason Code is invalid. Correct the row and retry.',
    'invalid-proposals': 'proposals include duplicates, no-ops, or invalid Reason Codes. Review changes and retry.',
    'unchanged-line': 'a proposed line has no material change. Review staged changes and retry.',
    'workspace-missing': 'the workspace no longer exists. Reload the workspace.',
    'snapshot-changed': 'the MPS or Open Orders snapshot changed. Reload the workspace report and review proposals.',
    'scope-changed': 'the resolved-parent scope changed. Reload the workspace report and review proposals.',
    'report-conflict': 'the staged originals or line scope no longer match the report. Review the changed rows.',
    'duplicate-source': 'the source returned duplicate identities. Contact IT; no files were prepared.',
    'line-missing-or-closed': 'a line is missing or no longer open. Review changed rows against a fresh report.',
    'source-changed': 'original values or line scope changed in QAD. Refresh and review changed rows.',
    'below-shipped': 'proposed Order Qty is below freshly read Shipped Qty. Adjust the proposed total.',
    'source-unavailable': 'QAD is unavailable. Retry when the source is available.',
  };
  if (code && Object.prototype.hasOwnProperty.call(reasons, code)) return `${affected}: ${reasons[code]} No CSV files prepared; proposals remain.`;
  if (error.status === 404) return 'QXtend export endpoint is unavailable in the running backend. Rebuild/restart the desktop sidecar, then retry; no CSV files prepared.';
  if (error.status === 503) return 'QAD or the backend is unavailable. No CSV files prepared; retry after connectivity returns.';
  return `Export failed (HTTP ${error.status}). No CSV files prepared; retry or contact IT. Proposals remain.`;
}

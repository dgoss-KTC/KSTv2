import type { PlanningExport } from '../../hooks/usePlanningExport';
import { ISSUE_MESSAGES, type PlanningEvaluation } from '../../planningWorkbook/planningAdjustments';
import { formatPlanningDate } from '../../planningWorkbook/planningWorkbookSummary';
import { PlanningDialog } from './PlanningDialog';
import { EXPORT_DOWNLOAD_NOTE, EXPORT_DRAFT_NOTE, EXPORT_QTY_NOTE, EXPORT_SAVED_NOTE, ISSUE_LABELS, exportFailureCopy } from './planningLabels';

/**
 * Stage 14.6 "Review and export" dialog: validation feedback, a preview parsed from the actual generated
 * file bytes, an explicit Save As with distinct saved / cancelled / failed outcomes, and the
 * post-export Clear / Keep prompt. The dialog only reflects the export state; it never starts a save
 * for an obsolete preparation and never touches saved drafts.
 */
export function PlanningExportDialog({ exporter, evaluation, returnFocus }: {
  exporter: PlanningExport;
  evaluation: PlanningEvaluation | null;
  returnFocus?: () => HTMLElement | null;
}) {
  const { phase } = exporter;
  if (phase.kind === 'closed') return null;
  const title = 'Review and export';
  // A change of step moves focus to that step's primary control (Keep for the Clear / Keep prompt).
  const focusKey = phase.kind === 'saved' ? `saved-${phase.choice}` : phase.kind;
  const close = <button type="button" className="pwb-btn" onClick={exporter.close}>Close</button>;

  // Browser fallback: the browser cannot report whether a file was written, so this is not a saved outcome.
  // No Clear / Keep prompt is offered and every staged adjustment stays.
  if (phase.kind === 'downloaded') {
    return (
      <PlanningDialog titleId="pwb-export-title" title={title} onClose={exporter.close} returnFocus={returnFocus} focusKey={focusKey}
        actions={<button type="button" className="pwb-btn pwb-btn--primary" data-autofocus onClick={exporter.close}>Close</button>}>
        <p className="pwb-export-status" role="status">{EXPORT_DOWNLOAD_NOTE(phase.fileName)}</p>
      </PlanningDialog>
    );
  }

  if (phase.kind === 'saved') {
    const count = phase.prepared.rows.length;
    const noun = count === 1 ? 'adjustment' : 'adjustments';
    return (
      <PlanningDialog titleId="pwb-export-title" title={title} onClose={exporter.close} returnFocus={returnFocus} focusKey={focusKey}
        actions={phase.choice === 'ask'
          ? <>
              <button type="button" className="pwb-btn" data-autofocus onClick={exporter.keep}>Keep</button>
              <button type="button" className="pwb-btn pwb-btn--planning" onClick={exporter.clear}>Clear</button>
            </>
          : <button type="button" className="pwb-btn pwb-btn--primary" data-autofocus onClick={exporter.close}>Close</button>}>
        <p className="pwb-export-status pwb-export-status--saved" role="status">
          Saved {phase.fileName}. {count} {noun} written. {EXPORT_SAVED_NOTE}
        </p>
        {phase.choice === 'ask' && (
          <div className="pwb-export-prompt" role="group" aria-labelledby="pwb-export-clear-question">
            <p id="pwb-export-clear-question"><strong>Clear the staged adjustments included in this export?</strong></p>
            <p>
              Clear removes these {count} {noun} from this session. Adjustments added or edited since the export was prepared are
              kept, and Undo restores cleared ones. Keep leaves everything staged. {EXPORT_DRAFT_NOTE}
            </p>
          </div>
        )}
        {phase.choice === 'cleared' && (
          <p role="status">
            Cleared {phase.clearedCount} exported {phase.clearedCount === 1 ? 'adjustment' : 'adjustments'} from this session
            (Undo restores them). {EXPORT_DRAFT_NOTE}
          </p>
        )}
        {phase.choice === 'kept' && <p role="status">The staged adjustments were kept. {EXPORT_DRAFT_NOTE}</p>}
      </PlanningDialog>
    );
  }

  if (phase.kind === 'ready') {
    const { prepared, save } = phase;
    const rows = prepared.rows;
    return (
      <PlanningDialog titleId="pwb-export-title" title={title} onClose={exporter.close} wide returnFocus={returnFocus} focusKey={focusKey}
        actions={<>
          {close}
          <button type="button" className="pwb-btn pwb-btn--primary" data-autofocus onClick={() => { if (save.status !== 'saving') void exporter.save(); }} aria-disabled={save.status === 'saving' ? true : undefined}>
            {save.status === 'saving' ? 'Saving\u2026' : save.status === 'failed' || save.status === 'cancelled' ? 'Save As\u2026 again' : 'Save As\u2026'}
          </button>
        </>}>
        <p role="status">
          Validated against the complete loaded MPS data: {rows.length} {rows.length === 1 ? 'row' : 'rows'} ready
          {prepared.hiddenCount > 0 ? `, including ${prepared.hiddenCount} outside the visible horizon` : ''}.
          Suggested file name: <span className="pwb-mono">{prepared.fileName}</span>
        </p>
        <p>{EXPORT_QTY_NOTE} The file is a proposal for the external QAD Mass Updater; nothing is applied to QAD.</p>
        {save.status === 'cancelled' && (
          <p className="pwb-export-status" role="status">Save was cancelled. No file was written and your adjustments are unchanged.</p>
        )}
        {save.status === 'failed' && (
          <p className="pwb-export-status pwb-export-status--error" role="alert">
            The file could not be saved. No adjustments were cleared; choose Save As again or close this dialog.
          </p>
        )}
        <div className="pwb-dialog__table-scroll">
          <table className="pwb-table" aria-label="Mass Updater file preview">
            <thead>
              <tr>
                <th scope="col">Part</th>
                <th scope="col" title="Signed adjustment to the existing MPS quantity">Qty</th>
                <th scope="col" title="Monday of the week">Date</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(row => (
                <tr key={`${row.part}|${row.weekLabel}`}>
                  <td className="pwb-mono">{row.part}</td>
                  <td className="pwb-mono">{row.quantity}</td>
                  <td className="pwb-mono">{row.dateText}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </PlanningDialog>
    );
  }

  const issues = evaluation?.entries.filter(entry => entry.issue !== null) ?? [];
  return (
    <PlanningDialog titleId="pwb-export-title" title={title} onClose={exporter.close} wide={phase.kind === 'blocked'} returnFocus={returnFocus} focusKey={focusKey}
      actions={<>
        {close}
        {(phase.kind === 'obsolete' || (phase.kind === 'rejected' && phase.failure.type !== 'not-found')) && (
          <button type="button" className="pwb-btn pwb-btn--primary" data-autofocus onClick={exporter.open} disabled={!exporter.canExport}>
            {phase.kind === 'obsolete' ? 'Prepare again' : 'Try again'}
          </button>
        )}
      </>}>
      {phase.kind === 'preparing' && (
        <p role="status">Validating the staged adjustments against the loaded MPS data{'\u2026'} Nothing is sent to QAD.</p>
      )}
      {phase.kind === 'blocked' && phase.reason === 'empty' && (
        <p className="pwb-export-status pwb-export-status--error" role="alert">There are no nonzero adjustments to export. Enter at least one adjustment first. No file was created.</p>
      )}
      {phase.kind === 'blocked' && phase.reason === 'issues' && (
        <>
          <p className="pwb-export-status pwb-export-status--error" role="alert">
            {issues.length} {issues.length === 1 ? 'adjustment needs' : 'adjustments need'} attention. Fix or remove {issues.length === 1 ? 'it' : 'them'}, then export again.
            No file was created and your adjustments are unchanged.
          </p>
          <div className="pwb-dialog__table-scroll">
            <table className="pwb-table" aria-label="Adjustments needing attention">
              <thead><tr><th scope="col">Part</th><th scope="col">Week</th><th scope="col">Adjustment</th><th scope="col">Problem</th></tr></thead>
              <tbody>
                {issues.map(entry => (
                  <tr key={entry.key} className="pwb-table__row--issue">
                    <td className="pwb-mono">{entry.part}</td>
                    <td>{formatPlanningDate(entry.weekLabel)}</td>
                    <td className="pwb-mono">{entry.text}</td>
                    <td><strong>{ISSUE_LABELS[entry.issue!]}.</strong> {ISSUE_MESSAGES[entry.issue!]}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
      {phase.kind === 'rejected' && (
        <p className="pwb-export-status pwb-export-status--error" role="alert">{exportFailureCopy(phase.failure)}</p>
      )}
      {phase.kind === 'obsolete' && (
        <p className="pwb-export-status pwb-export-status--error" role="alert">
          The adjustments, MPS data, workspace or business week changed after this export was prepared, so it was not saved.
          Prepare it again to review the current file.
        </p>
      )}
    </PlanningDialog>
  );
}

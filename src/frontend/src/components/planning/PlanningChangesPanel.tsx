import { useState } from 'react';
import type { PlanningProposals } from '../../hooks/usePlanningProposals';
import { HIDDEN_MESSAGES, ISSUE_MESSAGES, type EntryEvaluation, type PlanningEvaluation } from '../../planningWorkbook/planningAdjustments';
import { formatPlanningDate } from '../../planningWorkbook/planningWorkbookSummary';
import { isExactZero, netChange, qty, shortWeekLabel, signedMoney, signedQty, type NetChange } from '../../planningWorkbook/planningView';
import { ISSUE_LABELS } from './planningLabels';

function EntryCard({ entry, modelHidden, onRemove }: { entry: EntryEvaluation; modelHidden: boolean; onRemove: (key: string) => void }) {
  const change = entry.value !== null ? signedQty(entry.value) : entry.text;
  const baseline = entry.value !== null && entry.currentMps !== null && entry.proposedMps !== null
    ? ` \u00b7 MPS ${qty(entry.currentMps)} \u2192 ${qty(entry.proposedMps)}` : '';
  const state = entry.issue ? 'issue' : entry.hidden ? 'hidden' : 'ok';
  return (
    <li className={`pwb-change pwb-change--${state}`} aria-label={`${entry.part}, week of ${formatPlanningDate(entry.weekLabel)}: ${change}`}>
      <div className="pwb-change__top">
        <span className="pwb-change__part">{entry.part}</span>
        <span className="pwb-change__delta">{change}</span>
        <button type="button" className="pwb-icon-btn" onClick={() => onRemove(entry.key)}
          aria-label={`Remove adjustment for ${entry.part}, week of ${formatPlanningDate(entry.weekLabel)}`} title="Remove">{'\u00d7'}</button>
      </div>
      <div className="pwb-change__week">Week of {shortWeekLabel(entry.weekLabel)}{baseline}</div>
      {entry.issue && (
        <p className="pwb-change__note pwb-change__note--issue"><strong>{ISSUE_LABELS[entry.issue]}.</strong> {ISSUE_MESSAGES[entry.issue]}</p>
      )}
      {!entry.issue && entry.hidden && <p className="pwb-change__note">{HIDDEN_MESSAGES[entry.hidden]}</p>}
      {modelHidden && <p className="pwb-change__note">Model hidden in View. The change is kept.</p>}
    </li>
  );
}

function netLine(net: NetChange): { units: string; dollars: string; note: string | null } {
  return {
    units: `${isExactZero(net.units) ? '0' : signedQty(net.units)} units`,
    dollars: net.validEntries - net.unpricedEntries === 0 ? '\u2014' : isExactZero(net.pricedDollars) ? '$0.00' : signedMoney(net.pricedDollars),
    note: net.unpricedEntries > 0
      ? `Dollars cover priced parts only; ${net.unpricedEntries} ${net.unpricedEntries === 1 ? 'change is' : 'changes are'} for unpriced or out-of-scope parts and excluded.`
      : null,
  };
}

/**
 * Changes panel: every staged entry and invalid input (hidden models, hidden weeks, passed weeks and
 * out-of-scope parts included), in workbook part order then week order, with Remove, confirmed
 * Discard all, Save draft and the saved-draft actions.
 */
export function PlanningChangesPanel({ proposals, evaluation, snapshotId, hiddenModels, onClose }: {
  proposals: PlanningProposals;
  evaluation: PlanningEvaluation | null;
  snapshotId: string | null;
  hiddenModels: ReadonlySet<string>;
  onClose: () => void;
}) {
  const [confirmDiscard, setConfirmDiscard] = useState(false);
  const { draft } = proposals;
  const listed = evaluation ? evaluation.entries.filter(e => !e.noop) : [];
  const total = proposals.stagedCount + proposals.invalidInputCount;
  const net = evaluation ? netLine(netChange(evaluation.entries, evaluation.parts)) : null;
  const validity = !evaluation ? (proposals.stagedCount > 0 ? 'Waiting for the workbook to check changes.' : null)
    : evaluation.issueCount > 0 ? `${evaluation.issueCount} ${evaluation.issueCount === 1 ? 'change needs' : 'changes need'} correcting or removing before export.`
      : evaluation.stagedCount > 0
        ? `No problems found in the weeks shown.${evaluation.hiddenCount > 0 ? ` ${evaluation.hiddenCount} not shown at this horizon ${evaluation.hiddenCount === 1 ? 'is' : 'are'} not checked here; export validates every change against the full loaded MPS.` : ''}`
        : null;

  return (
    <aside className="pwb-side pwb-side--changes" aria-labelledby="pwb-changes-title">
      <header className="pwb-side__header">
        <h3 id="pwb-changes-title">Changes <span className="pwb-count">{listed.length}</span></h3>
        <button type="button" className="pwb-icon-btn" aria-label="Close Changes" onClick={onClose}>{'\u00d7'}</button>
      </header>
      <div className="pwb-side__body">
        {listed.length === 0
          ? <p className="pwb-side__empty">Nothing staged yet. Type a signed quantity in any Adjust cell, such as 50 to add or -50 to pull back. Each change lands here.</p>
          : (
            <ul className="pwb-changes" aria-label="Staged changes">
              {listed.map(entry => (
                <EntryCard key={entry.key} entry={entry} modelHidden={hiddenModels.has(entry.part.toUpperCase())} onRemove={proposals.removeEntry} />
              ))}
            </ul>
          )}
      </div>
      <footer className="pwb-side__footer">
        {validity && (
          <p className={`pwb-validity${evaluation?.isValid ? '' : ' pwb-validity--blocked'}`} data-testid="planning-proposal-validity"
            data-valid={evaluation?.isValid ? 'true' : 'false'}>{validity}</p>
        )}
        <div className="pwb-net">
          <span>Net change</span>
          <span className="pwb-net__value">{net && listed.length > 0 ? `${net.units} \u00b7 ${net.dollars}${net.note ? ' (priced parts only)' : ''}` : 'None'}</span>
        </div>
        {net?.note && listed.length > 0 && <p className="pwb-net__note">{net.note}</p>}
        <div className="pwb-side__actions">
          <button type="button" className="pwb-btn pwb-btn--primary" onClick={() => void proposals.saveDraft()}
            disabled={!snapshotId || draft.busy !== null || draft.corrupt || total === 0}
            title={total === 0 ? 'Enter at least one adjustment to save a draft. Use Delete saved draft to remove a saved draft.' : undefined}>
            {draft.busy === 'save' ? 'Saving\u2026' : 'Save draft'}
          </button>
          {!confirmDiscard
            ? <button type="button" className="pwb-btn pwb-btn--danger" onClick={() => setConfirmDiscard(true)} disabled={total === 0}>Discard all</button>
            : (
              <span role="group" aria-label="Confirm Discard all" className="pwb-confirm">
                <span>Discard all {total} {total === 1 ? 'change' : 'changes'}?</span>
                <button type="button" className="pwb-btn pwb-btn--danger" onClick={() => { proposals.clearAll(); setConfirmDiscard(false); }}>Confirm Discard all</button>
                <button type="button" className="pwb-btn" onClick={() => setConfirmDiscard(false)}>Cancel</button>
              </span>
            )}
        </div>
        <p className="pwb-side__note">Drafts stay on this computer. Nothing is sent to QAD; Undo reverses Remove and Discard all.</p>
      </footer>
    </aside>
  );
}


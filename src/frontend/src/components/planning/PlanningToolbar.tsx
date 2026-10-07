import type { PlanningProposals } from '../../hooks/usePlanningProposals';
import type { PlanningEvaluation } from '../../planningWorkbook/planningAdjustments';
import { formatPlanningDate, formatPlanningTimestamp, type PlanningWorkbook } from '../../planningWorkbook/planningWorkbookSummary';
import type { PlanningMode } from '../../planningWorkbook/planningView';
import { EXPORT_BUTTON_NOTE, EXPORT_NO_CHANGES, REFRESH_NOTE } from './planningLabels';

export type SidePanel = 'none' | 'view' | 'changes';

function timeOnly(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  const hours = date.getHours();
  return `${hours % 12 || 12}:${String(date.getMinutes()).padStart(2, '0')} ${hours < 12 ? 'AM' : 'PM'}`;
}

function changeStatus(proposals: PlanningProposals, evaluation: PlanningEvaluation | null): string {
  const total = proposals.stagedCount + proposals.invalidInputCount;
  if (total === 0) return 'No changes yet';
  const issues = evaluation ? evaluation.issueCount : proposals.invalidInputCount;
  const saved = proposals.dirty ? 'not saved'
    : `draft saved${proposals.draft.savedAtUtc ? ` ${formatPlanningTimestamp(proposals.draft.savedAtUtc)}` : ''}`;
  return `${total} ${total === 1 ? 'change' : 'changes'}${issues > 0 ? ` \u00b7 ${issues} need attention` : ''} \u00b7 ${saved}`;
}

/** Planning toolbar: title, mode, status/freshness, horizon stepper, View and mode-specific actions. */
export function PlanningToolbar({ mode, workbook, proposals, evaluation, panel, viewLabel, horizon, canRefresh, isRefreshing, canExport,
  onRefresh, onStartPlanning, onBackToReport, onTogglePanel, onOpenForecast, onOpenExport }: {
  mode: PlanningMode;
  workbook: PlanningWorkbook | null;
  proposals: PlanningProposals;
  evaluation: PlanningEvaluation | null;
  panel: SidePanel;
  viewLabel: string;
  horizon: { value: number; minimum: number; maximum: number | null; set: (value: number) => void };
  canRefresh: boolean;
  isRefreshing: boolean;
  /** Staged adjustments exist and the loaded MPS data is available: export can be reviewed. */
  canExport: boolean;
  onRefresh: () => void;
  onStartPlanning: () => void;
  onBackToReport: () => void;
  onTogglePanel: (panel: Exclude<SidePanel, 'none'>) => void;
  onOpenForecast: () => void;
  onOpenExport: () => void;
}) {
  const total = proposals.stagedCount + proposals.invalidInputCount;
  const range = horizon.maximum !== null ? `${horizon.minimum}\u2013${horizon.maximum} weeks` : `From ${horizon.minimum} weeks`;
  const freshness = workbook
    ? `Acquired ${formatPlanningTimestamp(workbook.acquiredAtUtc)} \u00b7 Prices as of ${formatPlanningDate(workbook.priceEffectiveDate)} \u00b7 Today ${formatPlanningDate(workbook.today)}`
    : '';
  const atMax = horizon.maximum !== null && horizon.value >= horizon.maximum;

  return (
    <div className={`pwb-toolbar pwb-toolbar--${mode}`}>
      <div className="pwb-toolbar__left">
        <h3 id="planning-workbook-heading" className="pwb-toolbar__title">Master plan</h3>
        <span className={`pwb-pill pwb-pill--${mode}`} data-testid="planning-mode">{mode === 'planning' ? 'Planning' : 'Report'}</span>
        <span className="pwb-toolbar__status">
          {workbook && (
            <span className="pwb-freshness" data-testid="planning-freshness" title={freshness} tabIndex={0}>
              Data from {timeOnly(workbook.acquiredAtUtc)}
              <span className="pwb-sr-only">{`. ${freshness}`}</span>
            </span>
          )}
          <button type="button" className="pwb-icon-btn" aria-label="Refresh Planning" title={REFRESH_NOTE}
            aria-describedby="planning-refresh-note" onClick={onRefresh} disabled={!canRefresh}>
            <span aria-hidden="true" className={isRefreshing ? 'pwb-spin' : undefined}>{'\u21bb'}</span>
          </button>
          <span id="planning-refresh-note" className="pwb-sr-only">{REFRESH_NOTE}</span>
          {isRefreshing && <span role="status">Refreshing{'\u2026'}</span>}
          {(mode === 'planning' || total > 0) && (
            <span className="pwb-toolbar__changes" role="status" data-testid="planning-change-count">{changeStatus(proposals, evaluation)}</span>
          )}
        </span>
      </div>
      <div className="pwb-toolbar__right">
        <div className="pwb-stepper" role="group" aria-label="Planning horizon" title={`Planning horizon: ${range}`}>
          <button type="button" className="pwb-stepper__btn" aria-label="Fewer weeks" disabled={horizon.value <= horizon.minimum}
            onClick={() => horizon.set(horizon.value - 1)}>{'\u2212'}</button>
          <span className="pwb-stepper__value" role="spinbutton" tabIndex={0} aria-label="Planning horizon in weeks"
            aria-valuenow={horizon.value} aria-valuemin={horizon.minimum} aria-valuemax={horizon.maximum ?? undefined}
            aria-valuetext={`${horizon.value} weeks`} aria-describedby="planning-horizon-range"
            onKeyDown={e => {
              if (e.key === 'ArrowUp' || e.key === 'ArrowRight') { e.preventDefault(); horizon.set(horizon.value + 1); }
              if (e.key === 'ArrowDown' || e.key === 'ArrowLeft') { e.preventDefault(); horizon.set(horizon.value - 1); }
              if (e.key === 'Home') { e.preventDefault(); horizon.set(horizon.minimum); }
              if (e.key === 'End' && horizon.maximum !== null) { e.preventDefault(); horizon.set(horizon.maximum); }
            }}>
            {horizon.value} wk
          </span>
          <button type="button" className="pwb-stepper__btn" aria-label="More weeks" disabled={atMax}
            onClick={() => horizon.set(horizon.value + 1)}>+</button>
          <span id="planning-horizon-range" className="pwb-sr-only" data-testid="planning-horizon-range">{range}</span>
        </div>
        <button type="button" className={`pwb-btn${panel === 'view' ? ' pwb-btn--active' : ''}`} aria-expanded={panel === 'view'}
          onClick={() => onTogglePanel('view')}>{viewLabel}</button>
        {mode === 'report' ? (
          <button type="button" className="pwb-btn pwb-btn--accent" data-planning-mode-toggle onClick={onStartPlanning}>
            {total > 0 ? `Resume planning \u00b7 ${total}` : 'Start planning'}
          </button>
        ) : (
          <>
            <button type="button" className="pwb-btn" onClick={onOpenForecast}>Import forecast <span className="pwb-tag pwb-tag--soon">Soon</span></button>
            <button type="button" className="pwb-btn" onClick={proposals.undo} disabled={!proposals.canUndo}>Undo</button>
            <button type="button" className={`pwb-btn${panel === 'changes' ? ' pwb-btn--planning-active' : ''}`} aria-expanded={panel === 'changes'}
              onClick={() => onTogglePanel('changes')}>Changes {'\u00b7'} {total}</button>
            <button type="button" className="pwb-btn" data-planning-mode-toggle onClick={onBackToReport}>Back to report</button>
            <button type="button" className="pwb-btn pwb-btn--planning" data-planning-export-trigger disabled={!canExport}
              title={canExport ? EXPORT_BUTTON_NOTE : EXPORT_NO_CHANGES} aria-describedby="planning-export-note" onClick={onOpenExport}>
              Review and export
            </button>
            <span id="planning-export-note" className="pwb-sr-only">{canExport ? EXPORT_BUTTON_NOTE : EXPORT_NO_CHANGES}</span>
          </>
        )}
      </div>
    </div>
  );
}

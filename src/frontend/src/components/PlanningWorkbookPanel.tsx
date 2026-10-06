import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useFiscalCalendarSettings } from '../hooks/useFiscalCalendarSettings';
import { usePlanningWorkbook } from '../hooks/usePlanningWorkbook';
import { usePlanningProposals, type PlanningProposals } from '../hooks/usePlanningProposals';
import { usePlanningViewPreferences } from '../hooks/usePlanningViewPreferences';
import type { FiscalCalendarSettings } from '../fiscal/types';
import {
  formatPlanningDate,
  formatPlanningTimestamp,
  planningColumns,
  planningQuarters,
  type PlanningWorkbook,
} from '../planningWorkbook/planningWorkbookSummary';
import {
  adjustedQuarterSummaries,
  adjustedWorkspaceTotals,
  evaluatePlanning,
  type AdjustmentEntries,
  type PlanningEvaluation,
} from '../planningWorkbook/planningAdjustments';
import {
  changeCountsByPart,
  DEFAULT_VIEW_PREFERENCES,
  filterModels,
  reconcileViewPreferences,
  type ModelFilter,
  type PlanningMode,
  type PlanningViewPreferences,
} from '../planningWorkbook/planningView';
import { PlanningGrid } from './planning/PlanningGrid';
import { PlanningToolbar, type SidePanel } from './planning/PlanningToolbar';
import { PlanningViewPanel } from './planning/PlanningViewPanel';
import { PlanningChangesPanel } from './planning/PlanningChangesPanel';
import { PlanningDraftReviewDialog, PlanningForecastImportDialog } from './planning/PlanningDialogs';
import { errorCopy } from './planning/planningLabels';
import './PlanningWorkbookPanel.css';

const NO_ENTRIES: AdjustmentEntries = {};

/** Quarter grouping, per-part quarter summaries and whole-workspace totals for one evaluation. */
function useGridModel(workbook: PlanningWorkbook, settings: FiscalCalendarSettings, evaluation: PlanningEvaluation) {
  return useMemo(() => {
    const quarters = planningQuarters(workbook.weekLabels, settings);
    return {
      quarters,
      columns: planningColumns(quarters),
      summaries: new Map(evaluation.parts.map(part => [part.part.toUpperCase(), adjustedQuarterSummaries(part, quarters)])),
      // Totals are computed from the complete workbook before any View tailoring or filter is applied.
      totals: adjustedWorkspaceTotals(evaluation.parts, workbook.weekLabels.length, quarters),
    };
  }, [workbook, settings, evaluation]);
}

/**
 * Read-only grid for a workbook (report mode by default) with optional staged entries. Used by tests
 * and anywhere the grid is needed without the Planning toolbar and panels.
 */
export function PlanningWorkbookGrid({ workbook, settings, entries = NO_ENTRIES, mode = 'report', prefs = DEFAULT_VIEW_PREFERENCES }: {
  workbook: PlanningWorkbook;
  settings: FiscalCalendarSettings;
  entries?: AdjustmentEntries;
  mode?: PlanningMode;
  prefs?: PlanningViewPreferences;
}) {
  const evaluation = useMemo(() => evaluatePlanning(workbook, entries), [workbook, entries]);
  const model = useGridModel(workbook, settings, evaluation);
  const changes = useMemo(() => changeCountsByPart(evaluation.entries), [evaluation]);
  const hidden = new Set(prefs.hiddenModels);
  const { shown } = filterModels(evaluation.parts, hidden, 'all', mode, changes);
  return (
    <PlanningGrid workbook={workbook} shown={shown} modelCount={workbook.parts.length} columns={model.columns}
      summaries={model.summaries} totals={model.totals} mode={mode} rows={prefs.rows} totalRows={prefs.totals} tint={prefs.tintRows}
      collapsed={new Set(prefs.collapsed)} changeCounts={changes} onToggleCollapse={() => {}} onSetAllCollapsed={() => {}} />
  );
}

interface PanelProps {
  assignmentId: string;
  site: string;
  snapshotId: string | null;
  mpsLoading?: boolean;
  /** Optional override; defaults to the user's configured fiscal calendar settings. */
  fiscalSettings?: FiscalCalendarSettings;
  /** Workspace-owned proposal state (CustomerWorkspace). When omitted the panel owns its own state. */
  proposals?: PlanningProposals;
}

function StandalonePlanningWorkbookPanel(props: PanelProps) {
  const proposals = usePlanningProposals(props.assignmentId, props.snapshotId);
  return <PlanningWorkbookView {...props} proposals={proposals} />;
}

export function PlanningWorkbookPanel(props: PanelProps) {
  return props.proposals
    ? <PlanningWorkbookView {...props} proposals={props.proposals} />
    : <StandalonePlanningWorkbookPanel {...props} />;
}

interface UiState { assignmentId: string; mode: PlanningMode; panel: SidePanel; filter: ModelFilter; forecastOpen: boolean }
const initialUi = (assignmentId: string): UiState => ({ assignmentId, mode: 'report', panel: 'none', filter: 'all', forecastOpen: false });

function DraftNotices({ proposals, snapshotId }: { proposals: PlanningProposals; snapshotId: string | null }) {
  const { draft } = proposals;
  return (
    <div className="pwb-notices">
      {draft.message && (
        <span className={`pwb-notice${draft.message.tone === 'error' ? ' pwb-notice--error' : ''}`} role={draft.message.tone === 'error' ? 'alert' : 'status'}>
          {draft.message.text}
        </span>
      )}
      {draft.presence === 'error' && (
        <span className="pwb-notice pwb-notice--error" role="alert">
          Could not check for a saved Planning draft.{' '}
          <button type="button" className="pwb-link-btn" onClick={proposals.checkDraft}>Retry</button>
        </span>
      )}
      {draft.corrupt && (
        <span className="pwb-notice pwb-notice--error">
          The saved Planning draft could not be read.{' '}
          <button type="button" className="pwb-link-btn" onClick={() => void proposals.deleteDraft()} disabled={draft.busy !== null}>Delete saved draft</button>
        </span>
      )}
      {draft.presence === 'exists' && !draft.corrupt && !draft.review && (
        <span className="pwb-notice" role="status">
          {draft.isSaved
            ? `This session matches the saved Planning draft${draft.savedAtUtc ? ` (saved ${formatPlanningTimestamp(draft.savedAtUtc)})` : ''}.`
            : 'A saved Planning draft is available.'}{' '}
          <button type="button" className="pwb-link-btn" data-draft-review-trigger onClick={proposals.reviewDraft} disabled={!snapshotId || draft.reviewing || draft.busy !== null}>
            {draft.reviewing ? 'Checking saved draft\u2026' : 'Review saved draft'}
          </button>
          {' \u00b7 '}
          <button type="button" className="pwb-link-btn" onClick={() => void proposals.deleteDraft()} disabled={draft.busy !== null}>
            {draft.busy === 'delete' ? 'Deleting\u2026' : 'Delete saved draft'}
          </button>
        </span>
      )}
    </div>
  );
}

function PlanningWorkbookView({ assignmentId, site, snapshotId, mpsLoading = false, fiscalSettings, proposals }: PanelProps & { proposals: PlanningProposals }) {
  const { settings: storedSettings } = useFiscalCalendarSettings();
  const settings = fiscalSettings ?? storedSettings;
  const planning = usePlanningWorkbook(assignmentId, site, snapshotId);
  const { workbook } = planning;
  const evaluation = useMemo(() => (workbook ? evaluatePlanning(workbook, proposals.entries) : null), [workbook, proposals.entries]);
  const { checkDraft } = proposals;
  const { prefs: storedPrefs, update: updateStoredPrefs } = usePlanningViewPreferences(assignmentId);
  const [rawUi, setUi] = useState(() => initialUi(assignmentId));
  if (rawUi.assignmentId !== assignmentId) setUi(initialUi(assignmentId));
  const ui = rawUi.assignmentId === assignmentId ? rawUi : initialUi(assignmentId);

  // Presence (file existence only) is checked whenever Planning is opened for a workspace.
  useEffect(() => { checkDraft(); }, [assignmentId, checkDraft]);

  const parts = workbook?.parts;
  const prefs = parts ? reconcileViewPreferences(storedPrefs, parts) : storedPrefs;
  const updatePrefs = (fn: (p: PlanningViewPreferences) => PlanningViewPreferences) =>
    updateStoredPrefs(p => fn(parts ? reconcileViewPreferences(p, parts) : p));
  const hidden = useMemo(() => new Set(prefs.hiddenModels), [prefs.hiddenModels]);
  const changes = useMemo(() => changeCountsByPart(evaluation?.entries ?? []), [evaluation]);
  const filtered = evaluation ? filterModels(evaluation.parts, hidden, ui.filter, ui.mode, changes) : null;
  const modelTotal = workbook?.parts.length ?? 0;
  const viewLabel = filtered && filtered.tailored.length < modelTotal ? `View \u00b7 ${filtered.tailored.length} of ${modelTotal} models` : 'View';

  const setUiState = (patch: Partial<UiState>) => setUi(current => ({ ...(current.assignmentId === assignmentId ? current : initialUi(assignmentId)), ...patch }));
  const startPlanning = () => { proposals.endEdit(); setUiState({ mode: 'planning', panel: 'changes' }); };
  const backToReport = () => {
    proposals.endEdit();
    setUiState({ mode: 'report', panel: ui.panel === 'changes' ? 'none' : ui.panel, filter: ui.filter === 'changed' ? 'all' : ui.filter });
  };
  const togglePanel = (panel: Exclude<SidePanel, 'none'>) => setUiState({ panel: ui.panel === panel ? 'none' : panel });

  /*
   * The Review control is replaced while the review dialog is open, so the dialog cannot return focus
   * to the element that opened it. On close, focus goes to the Review control of this workspace when
   * it is available again, otherwise (after deletion or a workspace change) to the mode button. When
   * Planning itself has gone (navigation), nothing in it is focused.
   */
  const sectionRef = useRef<HTMLElement>(null);
  const draftReviewReturnFocus = useCallback(() => {
    const root = sectionRef.current;
    if (!root?.isConnected) return null;
    return root.querySelector<HTMLElement>('[data-draft-review-trigger]:not(:disabled)')
      ?? root.querySelector<HTMLElement>('[data-planning-mode-toggle]:not(:disabled)');
  }, []);

  const chips: { id: ModelFilter; label: string; count: number }[] = filtered ? [
    { id: 'all', label: 'All models', count: filtered.counts.all },
    { id: 'short', label: 'Short', count: filtered.counts.short },
    ...(ui.mode === 'planning' ? [{ id: 'changed' as const, label: 'Changed', count: filtered.counts.changed }] : []),
  ] : [];
  const showGrid = !!(workbook && workbook.parts.length > 0 && evaluation && filtered);

  return (
    <section ref={sectionRef} className={`planning-workbook pwb pwb--${ui.mode}`} aria-labelledby="planning-workbook-heading">
      <PlanningToolbar
        mode={ui.mode}
        workbook={workbook}
        proposals={proposals}
        evaluation={evaluation}
        panel={ui.panel}
        viewLabel={viewLabel}
        horizon={{ value: planning.horizonWeeks, minimum: planning.minimumHorizonWeeks, maximum: planning.maximumHorizonWeeks, set: planning.setHorizonWeeks }}
        canRefresh={planning.canRefresh}
        isRefreshing={planning.isRefreshing}
        onRefresh={() => void planning.refresh()}
        onStartPlanning={startPlanning}
        onBackToReport={backToReport}
        onTogglePanel={togglePanel}
        onOpenForecast={() => setUiState({ forecastOpen: true })}
      />

      <div className="pwb-filterbar">
        {showGrid && (
          <div className="pwb-chips" role="group" aria-label="Filter models">
            {chips.map(chip => (
              <button key={chip.id} type="button" aria-pressed={ui.filter === chip.id}
                className={`pwb-chip${ui.filter === chip.id ? ' pwb-chip--on' : ''}`} onClick={() => setUiState({ filter: chip.id })}>
                {chip.label} <span className={`pwb-chip__count${chip.id === 'short' && chip.count > 0 ? ' pwb-chip__count--short' : ''}`}>{chip.count}</span>
              </button>
            ))}
          </div>
        )}
        <DraftNotices proposals={proposals} snapshotId={snapshotId} />
      </div>

      {workbook?.isStale && (
        <div className="pwb-banner" role="alert">
          {workbook.warning ?? 'Showing the last known Planning Workbook data.'}
          {` Data acquired ${formatPlanningTimestamp(workbook.acquiredAtUtc)}; prices as of ${formatPlanningDate(workbook.priceEffectiveDate)}.`}
        </div>
      )}
      {workbook && planning.refreshError && (
        <div className="pwb-banner" role="alert">
          Refresh Planning could not be completed. Showing the previously loaded workbook acquired {formatPlanningTimestamp(workbook.acquiredAtUtc)}.
        </div>
      )}

      <div className="pwb-body">
        <div className="pwb-main">
          {!snapshotId && (
            <div className="pwb-state">
              {mpsLoading ? 'Waiting for this workspace\u2019s MPS data\u2026'
                : 'MPS data is not loaded for this workspace. Load it from the Dashboard tab; Planning does not load or refresh MPS.'}
            </div>
          )}
          {planning.isLoading && <div className="pwb-state">Loading Planning Workbook&hellip;</div>}
          {!workbook && planning.error && (
            <div className="pwb-state pwb-state--error" role="alert">
              <p>{errorCopy(planning.error).text}</p>
              {errorCopy(planning.error).retry && (
                <button type="button" className="pwb-btn pwb-btn--accent" onClick={planning.retry}>Retry</button>
              )}
            </div>
          )}
          {workbook && workbook.parts.length === 0 && (
            <div className="pwb-state">No parts resolved for this workspace&apos;s current scope.</div>
          )}
          {showGrid && workbook && evaluation && filtered && (
            <GridForView workbook={workbook} settings={settings} evaluation={evaluation} shown={filtered.shown} mode={ui.mode}
              prefs={prefs} changes={changes} proposals={proposals} updatePrefs={updatePrefs}
              emptyMessage={filtered.shown.length > 0 ? null : {
                text: filtered.tailored.length === 0 ? 'All models are hidden in View.' : 'No models match this filter.',
                actionLabel: 'Show all models',
                onAction: () => { setUiState({ filter: 'all' }); if (filtered.tailored.length === 0) updatePrefs(p => ({ ...p, hiddenModels: [] })); },
              }} />
          )}
        </div>
        {ui.panel === 'view' && evaluation && (
          <PlanningViewPanel parts={evaluation.parts} mode={ui.mode} prefs={prefs} onChange={updatePrefs} onClose={() => setUiState({ panel: 'none' })} />
        )}
        {ui.panel === 'changes' && ui.mode === 'planning' && (
          <PlanningChangesPanel proposals={proposals} evaluation={evaluation} snapshotId={snapshotId} hiddenModels={hidden}
            onClose={() => setUiState({ panel: 'none' })} />
        )}
      </div>

      <PlanningDraftReviewDialog proposals={proposals} returnFocus={draftReviewReturnFocus} />
      {ui.forecastOpen && <PlanningForecastImportDialog onClose={() => setUiState({ forecastOpen: false })} />}
    </section>
  );
}

function GridForView({ workbook, settings, evaluation, shown, mode, prefs, changes, proposals, updatePrefs, emptyMessage }: {
  workbook: PlanningWorkbook;
  settings: FiscalCalendarSettings;
  evaluation: PlanningEvaluation;
  shown: ReturnType<typeof filterModels>['shown'];
  mode: PlanningMode;
  prefs: PlanningViewPreferences;
  changes: ReadonlyMap<string, number>;
  proposals: PlanningProposals;
  updatePrefs: (fn: (p: PlanningViewPreferences) => PlanningViewPreferences) => void;
  emptyMessage: { text: string; actionLabel: string; onAction: () => void } | null;
}) {
  const model = useGridModel(workbook, settings, evaluation);
  const collapsed = useMemo(() => new Set(prefs.collapsed), [prefs.collapsed]);
  return (
    <PlanningGrid
      workbook={workbook}
      shown={shown}
      modelCount={workbook.parts.length}
      columns={model.columns}
      summaries={model.summaries}
      totals={model.totals}
      mode={mode}
      rows={prefs.rows}
      totalRows={prefs.totals}
      tint={prefs.tintRows}
      collapsed={collapsed}
      changeCounts={changes}
      onToggleCollapse={part => updatePrefs(p => {
        const key = part.toUpperCase();
        return { ...p, collapsed: p.collapsed.includes(key) ? p.collapsed.filter(k => k !== key) : [...p.collapsed, key] };
      })}
      onSetAllCollapsed={all => updatePrefs(p => {
        const keys = shown.map(s => s.part.toUpperCase());
        return { ...p, collapsed: all ? [...new Set([...p.collapsed, ...keys])] : p.collapsed.filter(k => !keys.includes(k)) };
      })}
      onAdjust={mode === 'planning' ? proposals.setAdjustment : undefined}
      onEndEdit={proposals.endEdit}
      emptyMessage={emptyMessage}
    />
  );
}

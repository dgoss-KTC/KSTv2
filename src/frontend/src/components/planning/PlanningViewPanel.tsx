import { useState } from 'react';
import type { AdjustedPart } from '../../planningWorkbook/planningAdjustments';
import {
  ROW_DEFS, SELECTABLE_ROWS, shortWeekCount, TOTAL_DEFS, TOTAL_ORDER,
  type PlanningMode, type PlanningViewPreferences, type RowId, type TotalId,
} from '../../planningWorkbook/planningView';

const toggle = <T,>(list: readonly T[], item: T): T[] => (list.includes(item) ? list.filter(x => x !== item) : [...list, item]);

/**
 * View tailoring: which models, rows and workspace totals are drawn. Hiding a model never removes its
 * staged changes, draft contents or eventual export scope. Totals always cover the whole workspace.
 * Density follows the application's global preference and is not set here.
 */
export function PlanningViewPanel({ parts, mode, prefs, onChange, onClose }: {
  parts: readonly AdjustedPart[];
  mode: PlanningMode;
  prefs: PlanningViewPreferences;
  onChange: (fn: (prefs: PlanningViewPreferences) => PlanningViewPreferences) => void;
  onClose: () => void;
}) {
  const [query, setQuery] = useState('');
  const hidden = new Set(prefs.hiddenModels);
  const q = query.trim().toLowerCase();
  const matches = parts.filter(p => q === '' || p.part.toLowerCase().includes(q) || (p.description ?? '').toLowerCase().includes(q));
  const shownCount = parts.filter(p => !hidden.has(p.part.toUpperCase())).length;

  return (
    <aside className="pwb-side pwb-side--view" aria-labelledby="pwb-view-title">
      <header className="pwb-side__header">
        <h3 id="pwb-view-title">View</h3>
        <button type="button" className="pwb-icon-btn" aria-label="Close View" onClick={onClose}>{'\u00d7'}</button>
      </header>
      <div className="pwb-side__body">
        <section className="pwb-view__section" aria-labelledby="pwb-view-models">
          <h4 id="pwb-view-models">Models <span className="pwb-view__count" data-testid="planning-view-model-count">{shownCount} of {parts.length} shown</span></h4>
          <input type="search" className="pwb-view__search" placeholder="Search part or description" aria-label="Search models"
            value={query} onChange={e => setQuery(e.target.value)} />
          <div className="pwb-view__buttons">
            <button type="button" className="pwb-btn" onClick={() => onChange(p => ({ ...p, hiddenModels: [] }))}>Show all</button>
            <button type="button" className="pwb-btn"
              onClick={() => onChange(p => ({ ...p, hiddenModels: parts.filter(m => shortWeekCount(m, mode) === 0).map(m => m.part.toUpperCase()) }))}>
              Only short models
            </button>
          </div>
          <ul className="pwb-view__list">
            {matches.map(part => {
              const key = part.part.toUpperCase();
              const short = shortWeekCount(part, mode) > 0;
              return (
                <li key={part.part}>
                  <label className="pwb-view__check">
                    <input type="checkbox" checked={!hidden.has(key)} onChange={() => onChange(p => ({ ...p, hiddenModels: toggle(p.hiddenModels, key) }))} />
                    <span className="pwb-view__part">{part.part}</span>
                    <span className="pwb-view__desc">{part.description}</span>
                    {short && <span className="pwb-badge pwb-badge--short">short</span>}
                  </label>
                </li>
              );
            })}
            {matches.length === 0 && <li className="pwb-view__none">No models match.</li>}
          </ul>
          <p className="pwb-side__note">Hidden models keep their staged changes; they stay in the Changes list and in totals.</p>
        </section>
        <section className="pwb-view__section" aria-labelledby="pwb-view-rows">
          <h4 id="pwb-view-rows">Rows</h4>
          {SELECTABLE_ROWS.map(id => (
            <label key={id} className="pwb-view__check">
              <input type="checkbox" checked={prefs.rows.includes(id)} onChange={() => onChange(p => ({ ...p, rows: toggle<RowId>(p.rows, id) }))} />
              <span className={`pwb-key pwb-key--${ROW_DEFS[id].key}`} aria-hidden="true" />
              {ROW_DEFS[id].label}
            </label>
          ))}
          {mode === 'planning' && <p className="pwb-side__note">Adjust is always shown in planning mode.</p>}
        </section>
        <section className="pwb-view__section" aria-labelledby="pwb-view-totals">
          <h4 id="pwb-view-totals">Workspace totals</h4>
          {TOTAL_ORDER.map(id => (
            <label key={id} className="pwb-view__check">
              <input type="checkbox" checked={prefs.totals.includes(id)} onChange={() => onChange(p => ({ ...p, totals: toggle<TotalId>(p.totals, id) }))} />
              <span className={`pwb-key pwb-key--${TOTAL_DEFS[id].key}`} aria-hidden="true" />
              {TOTAL_DEFS[id].label}{TOTAL_DEFS[id].planningOnly ? ' (planning mode)' : ''}
            </label>
          ))}
          <p className="pwb-side__note">Totals cover every model in the workspace. Inventory is never summed across models.</p>
        </section>
        <section className="pwb-view__section" aria-labelledby="pwb-view-row-color">
          <h4 id="pwb-view-row-color">Row color</h4>
          <label className="pwb-view__check">
            <input type="checkbox" checked={prefs.tintRows} onChange={() => onChange(p => ({ ...p, tintRows: !p.tintRows }))} />
            Tint rows by type
          </label>
          <p className="pwb-side__note">Sales orders green, MPS amber, Adjust purple. Changed and negative values keep their own colors.</p>
        </section>
        <section className="pwb-view__section" aria-labelledby="pwb-view-density">
          <h4 id="pwb-view-density">Density</h4>
          <p className="pwb-side__note">Row density follows the application setting under General.</p>
        </section>
      </div>
    </aside>
  );
}

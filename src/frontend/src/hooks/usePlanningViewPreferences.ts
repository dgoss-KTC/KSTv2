import { useCallback, useState } from 'react';
import {
  parseViewPreferences,
  viewPreferencesKey,
  type PlanningViewPreferences,
} from '../planningWorkbook/planningView';

function load(assignmentId: string): PlanningViewPreferences {
  try {
    return parseViewPreferences(window.localStorage.getItem(viewPreferencesKey(assignmentId)));
  } catch {
    return parseViewPreferences(null);
  }
}

/**
 * Planning view tailoring (hidden models, row and total choices, collapsed models), remembered per
 * workspace assignment on this computer under `kst.planningWorkbook.view.v1.{assignmentId}`. The
 * active filter chip is deliberately not part of this state. Density is never stored here: Planning
 * follows the application's global row-density preference.
 */
export function usePlanningViewPreferences(assignmentId: string) {
  const [state, setState] = useState(() => ({ assignmentId, prefs: load(assignmentId) }));
  // Another assignment reads its own preferences (derived-state-on-prop-change pattern).
  if (state.assignmentId !== assignmentId) setState({ assignmentId, prefs: load(assignmentId) });
  const prefs = state.assignmentId === assignmentId ? state.prefs : load(assignmentId);

  const update = useCallback((fn: (prefs: PlanningViewPreferences) => PlanningViewPreferences) => {
    setState(current => {
      if (current.assignmentId !== assignmentId) return current;
      const next = fn(current.prefs);
      if (next === current.prefs) return current;
      try { window.localStorage.setItem(viewPreferencesKey(assignmentId), JSON.stringify(next)); } catch { /* preference only */ }
      return { assignmentId, prefs: next };
    });
  }, [assignmentId]);

  return { prefs, update };
}

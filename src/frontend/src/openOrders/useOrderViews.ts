import { useState } from 'react';
import { loadViews, persistViews, sameLayout, type ViewMode, type ViewState } from './views';
import type { LayoutForPlan } from './planLayout';

export function useOrderViews(workspace: string, mode: ViewMode) {
  const identity = `${workspace}:${mode}`;
  const [owned, setOwned] = useState(() => ({ identity, value: loadViews(workspace, mode) }));
  const [error, setError] = useState('');
  let state = owned.value;
  if (owned.identity !== identity) {
    state = loadViews(workspace, mode); setOwned({ identity, value: state }); setError('');
  }
  function set(next: ViewState, persist = false) {
    setOwned({ identity, value: next });
    if (persist) {
      try { persistViews(workspace, mode, next); setError(''); }
      catch { setError('Views could not be remembered. Session changes remain available.'); }
    }
  }
  const selected = state.views.find(v => v.name === state.active)!;
  return { ...state, error, modified: !sameLayout(state.layout, selected.layout),
    change: (layout: LayoutForPlan) => set({ ...state, layout }),
    select: (name: string) => { const view = state.views.find(v => v.name === name); if (view) set({ ...state, active: name, layout: view.layout }, true); },
    discard: () => set({ ...state, layout: selected.layout }),
    save: (inputName: string, thenSelect?: string) => {
      const trimmed = inputName.trim();
      if (!trimmed || trimmed.length > 60 || trimmed.toLowerCase() === 'default') { setError('Use a personal view name of 1–60 characters, other than Default.'); return false; }
      const existing = state.views.find(v => v.name.toLowerCase() === trimmed.toLowerCase());
      if (existing && existing.name !== state.active) { setError('That view name already exists.'); return false; }
      if (!existing && state.views.length >= 51) { setError('The local limit is 50 personal views.'); return false; }
      const name = existing?.name ?? trimmed;
      const target = thenSelect ? state.views.find(v => v.name === thenSelect) : null;
      set({ active: target?.name ?? name, layout: target?.layout ?? state.layout,
        views: [...state.views.filter(v => v.name !== name), { name, layout: state.layout }] }, true); return true;
    },
  };
}

import { useEffect, useSyncExternalStore } from 'react';

let snapshot = { value: false, warning: '' };
let revision = 0;
let load = 0;
let queue = Promise.resolve();
let pendingWrites = 0;
const listeners = new Set<() => void>();
const notify = () => listeners.forEach(fn => fn());
const subscribe = (fn: () => void) => { listeners.add(fn); return () => { listeners.delete(fn); }; };

export function useAnalyticalOpenPreference() {
  const preference = useSyncExternalStore(subscribe, () => snapshot);
  useEffect(() => {
    if (pendingWrites) return;
    const token = ++load; const issuedRevision = revision;
    void import('@tauri-apps/api/core').then(({ invoke }) => invoke<boolean>('component_mrp_open_preference')).then(value => {
      if (token === load && revision === issuedRevision) { snapshot = { value: value === true, warning: '' }; notify(); }
    }).catch(() => {
      if (token === load && revision === issuedRevision) { snapshot = { ...snapshot, warning: 'Open preference could not be loaded; the current session choice is retained (unchecked by default).' }; notify(); }
    });
  }, []);
  return preference;
}

export function rememberAnalyticalOpenPreference(value: boolean) {
  const issuedRevision = ++revision; pendingWrites++; snapshot = { value, warning: '' }; notify();
  queue = queue.catch(() => {}).then(async () => {
    const { invoke } = await import('@tauri-apps/api/core'); await invoke('component_mrp_remember_open_preference', { value });
  }).catch(() => { if (revision === issuedRevision) { snapshot = { ...snapshot, warning: 'Open preference could not be remembered; this session still uses your choice.' }; notify(); } }).finally(() => { pendingWrites--; });
}

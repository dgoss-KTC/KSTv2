import { useSyncExternalStore } from 'react';
import type { AnalyticalExportClient, AnalyticalSaveResult } from '../api/analyticalExportApi';

// Application-lifetime observational recovery survives preparation/workspace/module departure.
interface SaveRecovery { id: string; client: AnalyticalExportClient; message: string; pending: boolean; checking: boolean }
let current: SaveRecovery | null = null;
const listeners = new Set<() => void>();
const notify = () => listeners.forEach(fn => fn());
const subscribe = (fn: () => void) => { listeners.add(fn); return () => { listeners.delete(fn); }; };
export const useAnalyticalSaveRecovery = () => useSyncExternalStore(subscribe, () => current);
export function beginAnalyticalSave(id: string, client: AnalyticalExportClient) {
  current = { id, client, message: 'Excel Save As is in progress for the captured export.', pending: true, checking: false }; notify();
}
export function recordAnalyticalSave(id: string, result?: AnalyticalSaveResult) {
  if (current?.id !== id || !current.pending) return;
  const terminal = result?.operationComplete !== false && !!result && ['saved', 'cancelled', 'failed'].includes(result.outcome);
  current = { ...current, pending: !terminal, checking: false, message: result?.message ?? 'Excel save outcome unknown. Check status before preparing or saving again.' }; notify();
}
export async function reconcileAnalyticalSave() {
  if (!current || current.checking) return;
  const operation = current;
  current = { ...operation, checking: true }; notify();
  try { recordAnalyticalSave(operation.id, await operation.client.status(operation.id)); }
  catch { recordAnalyticalSave(operation.id); }
}

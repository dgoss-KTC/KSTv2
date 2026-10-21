import { useSyncExternalStore } from 'react';
import type { AnalyticalSaveResult } from '../api/analyticalExportApi';
import type { ExcelExportPort, ExcelSavePort, PreparedExcel } from './excelExportPort';

// Application-lifetime observational recovery survives preparation/workspace/module departure.
interface SaveRecovery { id: string; owner?: string; label: string; client: ExcelSavePort; message: string; warning?: string | null; pending: boolean; checking: boolean; opening: boolean; result?: AnalyticalSaveResult; openFailed?: boolean; dismissed?: boolean }
interface PreparationFeedback { owner: string; label: string; preparing: boolean; message: string; actions: { label: string; run: () => void }[]; supersede: () => void }
let operations: SaveRecovery[] = [];
let preparation: PreparationFeedback | null = null;
let snapshot: { operations: SaveRecovery[]; preparation: PreparationFeedback | null } = { operations, preparation };
const listeners = new Set<() => void>();
const notify = () => { snapshot = { operations, preparation }; listeners.forEach(fn => fn()); };
const subscribe = (fn: () => void) => { listeners.add(fn); return () => { listeners.delete(fn); }; };
export const useAnalyticalExportStatus = () => useSyncExternalStore(subscribe, () => snapshot);
export const useAnalyticalSaveRecovery = () => useSyncExternalStore(subscribe, () => operations[operations.length - 1] ?? null);
export function claimAnalyticalPreparation(feedback: PreparationFeedback) {
  if (operations.some(o => o.pending || o.opening) || preparation?.preparing) return false;
  const previous = preparation;
  preparation = feedback;
  if (previous?.owner !== feedback.owner) previous?.supersede();
  notify(); return true;
}
export function updateAnalyticalPreparation(feedback: PreparationFeedback) {
  if (preparation?.owner !== feedback.owner) return;
  preparation = feedback; notify();
}
export function clearAnalyticalPreparation(owner: string) {
  if (preparation?.owner !== owner) return;
  preparation = null; notify();
}
export function beginAnalyticalSave(id: string, client: ExcelSavePort, owner?: string, label = 'Excel export', warning?: string | null) {
  if (operations.some(o => o.pending || o.opening)) return false;
  operations = [...operations, { id, client, owner, label, warning, message: 'Waiting for Save As / protected save to finish…', pending: true, checking: false, opening: false }];
  notify(); return true;
}
export function recordAnalyticalSave(id: string, result?: AnalyticalSaveResult) {
  const current = operations.find(o => o.id === id);
  if (!current?.pending) return;
  const terminal = result?.operationComplete !== false && !!result && ['saved', 'cancelled', 'failed'].includes(result.outcome);
  operations = operations.map(o => o.id !== id ? o : { ...o, result, pending: !terminal, checking: false,
    openFailed: terminal && result?.outcome === 'saved' && result.openFailed === true,
    message: result?.message ?? 'Save outcome uncertain. Check save status before preparing or saving again.' }); notify();
}
export async function reconcileAnalyticalSave(id = operations[operations.length - 1]?.id) {
  const operation = operations.find(o => o.id === id);
  if (!operation?.pending || operation.checking) return;
  operations = operations.map(o => o.id === id ? { ...o, checking: true } : o); notify();
  try { recordAnalyticalSave(operation.id, await operation.client.status(operation.id)); }
  catch { recordAnalyticalSave(operation.id); }
}
export async function openAnalyticalArtifact(id: string) {
  const operation = operations.find(o => o.id === id);
  if (!operation || operation.pending || !operation.openFailed || !operation.result?.savedArtifactId || operations.some(o => o.pending || o.opening) || preparation?.preparing) return;
  operations = operations.map(o => o.id === id ? { ...o, opening: true } : o); notify();
  try {
    await operation.client.open(operation.result.savedArtifactId);
    operations = operations.map(o => o.id === id ? { ...o, opening: false, openFailed: false, message: 'Workbook saved; opening request accepted. This does not confirm Excel loaded it.' } : o);
  } catch {
    operations = operations.map(o => o.id === id ? { ...o, opening: false, message: 'Workbook saved, but could not be opened. The host may refuse an unavailable, changed or retry-exhausted artifact.' } : o);
  }
  notify();
}
export function dismissAnalyticalSave(id: string, expected?: SaveRecovery) {
  // Presentation only: retain the terminal outcome, client and host artifact identity. An obsolete
  // timer may not hide a replacement message, even before React commits its updated effect.
  operations = operations.map(o => o.id !== id || o.pending || o.opening || (expected && o !== expected) || o.dismissed ? o : { ...o, dismissed: true }); notify();
}

/** Opt-in Reports recovery: new lookup context is not authority to destroy prepared bytes.
 * The existing single preparation slot bounds retention. Explicit new export supersedes it;
 * no acquisition, preparation, save or open starts here, and host expiry remains authoritative.
 */
export function retainPreparedReportWorkbook<Request>(owner: string, label: string, client: ExcelExportPort<Request>, workbook: PreparedExcel, openAfterSave: boolean): boolean {
  if (preparation?.owner !== owner) return false;
  let ended = false;
  let observed: SaveRecovery | undefined;
  const release = () => {
    if (ended) return;
    ended = true; listeners.delete(observe);
    clearAnalyticalPreparation(owner);
    void client.release().catch(() => {});
  };
  const feedback = (message: string) => updateAnalyticalPreparation({ owner, label, preparing: false,
    message: `${message} The exact prepared workbook is retained independently of the current lookup.`,
    actions: [{ label: 'Save again', run: () => void save() }, { label: 'Discard workbook', run: release }], supersede: release });
  const observe = () => {
    if (ended) return;
    const operation = [...operations].reverse().find(o => o.owner === owner);
    if (observed === operation) return;
    observed = operation;
    if (operation?.pending || operation?.opening) return;
    if (operation?.result?.outcome === 'saved') release();
    else feedback(operation?.message ?? 'Workbook prepared.');
  };
  async function save() {
    if (ended || operations.some(o => o.pending || o.opening) || preparation?.preparing) return;
    const id = crypto.randomUUID();
    if (!beginAnalyticalSave(id, client, owner, label, workbook.warning)) return;
    updateAnalyticalPreparation({ owner, label, preparing: false, message: 'Waiting for Save As / protected save to finish…', actions: [], supersede: release });
    try { recordAnalyticalSave(id, await client.save(workbook, openAfterSave, id)); }
    catch { recordAnalyticalSave(id); }
  }
  listeners.add(observe);
  const operation = [...operations].reverse().find(o => o.owner === owner);
  if (!operation) feedback('Workbook prepared.');
  else observe();
  return true;
}

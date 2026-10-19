import { useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { dismissAnalyticalSave, openAnalyticalArtifact, reconcileAnalyticalSave, recordAnalyticalSave, useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';
import { isRunningInTauri } from '../api/tauri-bridge';
import type { AnalyticalSaveResult } from '../api/analyticalExportApi';
import './AnalyticalExcelExport.css';

/** Persistent application-session recovery, independent of the originating view and toast timer. */
export function AnalyticalSaveStatus() {
  const { operations, preparation } = useAnalyticalExportStatus();
  useEffect(() => {
    if (!isRunningInTauri()) return;
    let alive = true; let dispose: (() => void) | undefined;
    void import('@tauri-apps/api/event').then(({ listen }) => listen<{ operationId: string; result: AnalyticalSaveResult }>('component-mrp-operation', event => {
      if (alive) recordAnalyticalSave(event.payload.operationId, event.payload.result);
    })).then(unlisten => { if (alive) dispose = unlisten; else unlisten(); }).catch(() => {});
    return () => { alive = false; dispose?.(); };
  }, []);
  const visibleOperations = operations.filter(o => !o.dismissed);
  if (!visibleOperations.length && !preparation) return null;
  const blocked = operations.some(o => o.pending || o.opening) || !!preparation?.preparing;
  return createPortal(<aside className="analytical-export-feedback" aria-label="Excel export activity">
    {preparation && !operations.some(o => o.owner === preparation.owner && (o.pending || o.result?.outcome === 'saved')) && <section className="analytical-export-feedback__card" aria-label={`${preparation.label} preparation`}>
      <strong>{preparation.label}</strong><p role="status">{preparation.message}</p>
      <div className="analytical-export-feedback__actions">{preparation.actions.map(action => <button type="button" className="analytical-export__button" key={action.label} onClick={action.run}>{action.label}</button>)}</div>
    </section>}
    {visibleOperations.filter(o => o.pending || o.result?.outcome === 'saved' || o.owner !== preparation?.owner).map(operation => <SaveOutcomeFeedback key={operation.id} operation={operation} blocked={blocked} />)}
  </aside>, document.querySelector('.shell') ?? document.body);
}

type SaveOperation = ReturnType<typeof useAnalyticalExportStatus>['operations'][number];
// Only known, ordinary success notices expire. Warnings/unrecognized failures stay visible.
const ordinarySuccessMessages = new Set(['Workbook saved.', 'Workbook saved; opening request accepted.', 'Workbook saved; opening request accepted. This does not confirm Excel loaded it.']);

function SaveOutcomeFeedback({ operation, blocked }: { operation: SaveOperation; blocked: boolean }) {
  const card = useRef<HTMLElement>(null);
  useEffect(() => {
    if (operation.warning || operation.pending || operation.opening || operation.openFailed || operation.result?.outcome !== 'saved' || operation.result.operationComplete === false || !ordinarySuccessMessages.has(operation.message)) return;
    const element = card.current;
    let active = true; let expired = false;
    const dismissWhenUnfocused = () => {
      if (active && expired && !element?.contains(document.activeElement)) dismissAnalyticalSave(operation.id, operation);
    };
    const timer = setTimeout(() => { expired = true; dismissWhenUnfocused(); }, 8000);
    const focusOut = () => { queueMicrotask(dismissWhenUnfocused); };
    element?.addEventListener('focusout', focusOut);
    return () => { active = false; clearTimeout(timer); element?.removeEventListener('focusout', focusOut); };
  }, [operation]);

  return <section ref={card} className="analytical-export-feedback__card" aria-label={`${operation.label} save outcome`}>
      <strong>{operation.label}{operation.result?.outcome === 'saved' ? ' — Saved' : operation.result?.outcome === 'cancelled' ? ' — Cancelled' : operation.result?.outcome === 'failed' ? ' — Failed' : ''}</strong>
      <p role={operation.pending ? 'alert' : 'status'}>{operation.message}</p>
      {operation.warning && <p>{operation.warning}</p>}
      <div className="analytical-export-feedback__actions">
        {operation.pending && <button type="button" className="analytical-export__button" disabled={operation.checking} onClick={() => void reconcileAnalyticalSave(operation.id)}>{operation.checking ? 'Checking save status…' : 'Check save status'}</button>}
        {!operation.pending && operation.openFailed && <button type="button" className="analytical-export__button" disabled={blocked} onClick={() => void openAnalyticalArtifact(operation.id)}>{operation.opening ? 'Opening…' : 'Open again'}</button>}
        {!operation.pending && !operation.openFailed && <button type="button" className="analytical-export__button" disabled={operation.opening} onClick={() => dismissAnalyticalSave(operation.id)}>Dismiss</button>}
      </div>
    </section>;
}

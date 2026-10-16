import { useEffect, useEffectEvent, useId, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { AnalyticalExportClient, type AnalyticalRequest, type AnalyticalPrepared } from '../api/analyticalExportApi';
import { isRunningInTauri } from '../api/tauri-bridge';
import { beginAnalyticalSave, claimAnalyticalPreparation, clearAnalyticalPreparation, recordAnalyticalSave, updateAnalyticalPreparation, useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';
import './AnalyticalExcelExport.css';
import { rememberAnalyticalOpenPreference, useAnalyticalOpenPreference } from '../shared/analyticalOpenPreference';

interface Props {
  request: () => AnalyticalRequest;
  contextKey: string;
  scopeLabel: string;
  label: string;
  disabledReason?: string;
  clientFactory?: () => AnalyticalExportClient;
}
const defaultFactory = () => new AnalyticalExportClient();

/** Only semantic source/selection changes (not object allocation, scrolling or drilling) end a visit. */
export function AnalyticalExcelExport(props: Props) {
  const [attempt, setAttempt] = useState(0);
  const reason = props.disabledReason || (!isRunningInTauri() ? 'Export requires the desktop application and its protected Save As.' : '');
  return <ExportVisit key={JSON.stringify([props.contextKey, reason, attempt])} {...props} disabledReason={reason} restart={() => setAttempt(n => n + 1)} />;
}

function ExportVisit({ request, scopeLabel, label, disabledReason, clientFactory = defaultFactory, restart }: Props & { restart: () => void }) {
  const [factory] = useState(() => clientFactory);
  const client = useRef<AnalyticalExportClient | null>(null);
  const live = useRef(false);
  const locked = useRef(false);
  const owner = useId();
  const trigger = useRef<HTMLButtonElement>(null);
  const menu = useRef<HTMLDivElement>(null);
  const status = useAnalyticalExportStatus();
  const [menuOpen, setMenuOpen] = useState(false);
  const [position, setPosition] = useState({ top: 0, right: 0 });
  const { value: preference, warning: preferenceWarning } = useAnalyticalOpenPreference();
  const latest = useRef({ request, disabledReason, preference });
  const prepared = useRef<AnalyticalPrepared | null>(null);
  const captured = useRef('');
  const [operationId, setOperationId] = useState<string | null>(null);
  const operation = status.operations.find(o => o.id === operationId);
  const blocked = status.operations.some(o => o.pending || o.opening) || !!status.preparation?.preparing;
  const reason = disabledReason || (blocked ? 'Another export is preparing, saving, opening or awaiting save-status reconciliation.' : '');

  useLayoutEffect(() => { latest.current = { request, disabledReason, preference }; });
  useLayoutEffect(() => {
    const acquired = factory(); client.current = acquired; live.current = true;
    return () => { live.current = false; client.current = null; clearAnalyticalPreparation(owner); void acquired.release().catch(() => {}); };
  }, [factory, owner]);

  function feedback(message: string, preparing: boolean, actions: { label: string; run: () => void }[]) {
    updateAnalyticalPreparation({ owner, label, preparing, message, actions, supersede: restart });
  }
  function retained(message: string) {
    feedback(message, false, [{ label: 'Save again', run: () => void save() }, { label: 'Discard workbook', run: restart }]);
  }
  // Also react to observation-only recovery: it can settle the operation before its invoke returns.
  const settle = useEffectEvent(() => {
    if (!operation || operation.pending) return;
    locked.current = false;
    if (operation.result?.outcome === 'saved') { prepared.current = null; clearAnalyticalPreparation(owner); }
    else if (prepared.current) retained(`${operation.message} The exact prepared workbook is retained; no source reread is needed.`);
  });
  useEffect(() => { settle(); }, [operation]);

  async function save(openIntent = latest.current.preference) {
    const port = client.current; const workbook = prepared.current;
    if (!live.current || !port || !workbook || locked.current || latest.current.disabledReason) return;
    try {
      if (captured.current !== JSON.stringify(latest.current.request())) { prepared.current = null; feedback('Export context changed. Prepare a new workbook.', false, [{ label: 'Discard', run: restart }]); return; }
    } catch (error) { prepared.current = null; feedback(error instanceof Error ? error.message : 'Export is unavailable. Reload the view.', false, [{ label: 'Discard', run: restart }]); return; }
    const id = crypto.randomUUID();
    if (!beginAnalyticalSave(id, port, owner, label)) return;
    locked.current = true; setOperationId(id); feedback('Waiting for Save As / protected save to finish…', false, []);
    try { recordAnalyticalSave(id, await port.save(workbook, openIntent, id)); }
    catch { recordAnalyticalSave(id); }
  }
  async function prepare() {
    const port = client.current;
    if (!live.current || !port || locked.current || reason) return;
    const openIntent = preference; // This activation owns its intent even while preferences are edited.
    if (!claimAnalyticalPreparation({ owner, label, preparing: true, message: 'Preparing Excel workbook from loaded data…', actions: [{ label: 'Cancel preparation', run: restart }], supersede: restart })) return;
    locked.current = true;
    try {
      const inputs = latest.current.request(); captured.current = JSON.stringify(inputs);
      const workbook = await port.prepare(inputs);
      if (!live.current || port !== client.current) return;
      if (latest.current.disabledReason || captured.current !== JSON.stringify(latest.current.request())) {
        locked.current = false; feedback('Export context changed during preparation. Nothing was saved.', false, [{ label: 'Discard', run: restart }]); return;
      }
      prepared.current = workbook; locked.current = false;
      retained('Workbook prepared. Continuing to Save As…');
      await save(openIntent);
    } catch (error) {
      if (live.current && port === client.current) { locked.current = false; feedback(error instanceof Error ? error.message : 'Workbook preparation failed. Nothing was saved.', false, [{ label: 'Dismiss', run: restart }]); }
    }
  }
  function closeMenu(restore = true) { setMenuOpen(false); if (restore) trigger.current?.focus({ preventScroll: true }); }
  useLayoutEffect(() => {
    if (!menuOpen) return;
    const place = () => {
      const rect = trigger.current?.getBoundingClientRect(); if (!rect) return;
      const height = menu.current?.offsetHeight ?? 0;
      setPosition({ top: Math.max(8, Math.min(rect.bottom + 6, window.innerHeight - height - 8)), right: Math.max(8, window.innerWidth - rect.right) });
    };
    place(); menu.current?.querySelector<HTMLElement>('[role="menuitem"]')?.focus();
    const pointer = (e: PointerEvent) => { if (!menu.current?.contains(e.target as Node) && !trigger.current?.contains(e.target as Node)) setMenuOpen(false); };
    const key = (e: KeyboardEvent) => {
      if (e.key === 'Escape') { e.preventDefault(); e.stopPropagation(); closeMenu(); }
      if (e.key === 'Tab') { setMenuOpen(false); trigger.current?.focus({ preventScroll: true }); }
      if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(e.key)) return;
      e.preventDefault(); const items = Array.from(menu.current?.querySelectorAll<HTMLElement>('[role^="menuitem"]') ?? []);
      const index = items.indexOf(document.activeElement as HTMLElement);
      items[e.key === 'Home' ? 0 : e.key === 'End' ? items.length - 1 : (index + (e.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length]?.focus();
    };
    document.addEventListener('pointerdown', pointer); document.addEventListener('keydown', key, true);
    window.addEventListener('resize', place); window.addEventListener('scroll', place, true);
    return () => { document.removeEventListener('pointerdown', pointer); document.removeEventListener('keydown', key, true); window.removeEventListener('resize', place); window.removeEventListener('scroll', place, true); };
  }, [menuOpen]);

  return <div className="analytical-excel-export" aria-label={`${label} export`}>
    <span className="analytical-export__scope">{scopeLabel}</span>
    <button ref={trigger} type="button" className="analytical-export__button" disabled={!!reason} aria-describedby={reason ? `${owner}-reason` : undefined} aria-haspopup="menu" aria-expanded={menuOpen} aria-controls={menuOpen ? `${owner}-menu` : undefined} onClick={() => setMenuOpen(v => !v)} onKeyDown={e => { if (e.key === 'ArrowDown' || e.key === 'ArrowUp') { e.preventDefault(); setMenuOpen(true); } }}>
      <span aria-hidden="true">↓</span> Export <span aria-hidden="true">▾</span>
    </button>
    {reason && <span id={`${owner}-reason`} className="analytical-export__reason">{reason}</span>}
    {menuOpen && createPortal(<div id={`${owner}-menu`} ref={menu} className="analytical-export-menu" data-analytical-export-menu role="menu" aria-label={`${label} export options`} style={position}>
      <button type="button" role="menuitem" className="analytical-export-menu__item" onClick={() => { closeMenu(); void prepare(); }}>Excel workbook (.xlsx)<small>{scopeLabel}</small></button>
      <button type="button" role="menuitemcheckbox" aria-checked={preference} className="analytical-export-menu__item" onClick={() => {
        rememberAnalyticalOpenPreference(!preference);
      }}><span aria-hidden="true">{preference ? '☑' : '☐'}</span> Open Excel after saving</button>
      {preferenceWarning && <p className="analytical-export__reason" role="status">{preferenceWarning}</p>}
    </div>, document.querySelector('.shell') ?? document.body)}
  </div>;
}

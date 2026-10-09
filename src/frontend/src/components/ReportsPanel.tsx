import { useEffect, useEffectEvent, useRef, useState } from 'react';
import { generateComponentMrp, type ComponentMrpPrepared, type ComponentMrpRequest } from '../api/componentMrpApi';
import './ReportsPanel.css';

const sites = ['SW', 'NW', 'AR', 'MN', 'MS', 'KV'] as const;
const siteKey = 'kst.componentMrp.site.v1';
function initialSite(): string {
  try { const value = localStorage.getItem(siteKey); return sites.includes(value as typeof sites[number]) ? value! : 'SW'; }
  catch { return 'SW'; }
}

export interface ReportSavePort {
  loadOpenPreference(): Promise<boolean>;
  rememberOpenPreference(value: boolean): Promise<void>;
  save(prepared: ComponentMrpPrepared, openAfterSave: boolean, operationId?: string): Promise<{ outcome: 'saved' | 'cancelled' | 'failed' | 'unknown'; message: string; savedArtifactId?: string; operationComplete?: boolean; openFailed?: boolean }>;
  open(savedArtifactId: string): Promise<void>;
  status?(operationId: string): Promise<{ outcome: string; message: string; savedArtifactId?: string; openFailed?: boolean; operationComplete?: boolean }>;
}

// No browser download/direct-write fallback: it cannot acknowledge the required protected save.
const desktopPort: ReportSavePort = {
  async loadOpenPreference() { const { invoke } = await import('@tauri-apps/api/core'); return invoke<boolean>('component_mrp_open_preference'); },
  async rememberOpenPreference(value) { const { invoke } = await import('@tauri-apps/api/core'); await invoke('component_mrp_remember_open_preference', { value }); },
  async save(prepared, openAfterSave, suppliedOperationId) {
    const { invoke } = await import('@tauri-apps/api/core');
    const operationId = suppliedOperationId ?? crypto.randomUUID();
    try {
      const result = await invoke<Awaited<ReturnType<ReportSavePort['save']>>>('component_mrp_save', { operationId, preparationId: prepared.preparationId, openAfterSave });
      if (result.outcome === 'unknown') throw new Error(operationId);
      return result;
    }
    catch { throw new Error(operationId); }
  },
  async status(operationId) { const { invoke } = await import('@tauri-apps/api/core'); return invoke('component_mrp_status', { operationId }); },
  async open(savedArtifactId) { const { invoke } = await import('@tauri-apps/api/core'); await invoke('component_mrp_open', { artifactId: savedArtifactId, requestId: crypto.randomUUID() }); },
};

export function ReportsPanel({ generate = generateComponentMrp, savePort = desktopPort, onBusyChange }: {
  generate?: (request: ComponentMrpRequest, signal?: AbortSignal) => Promise<ComponentMrpPrepared>;
  savePort?: ReportSavePort;
  onBusyChange?: (busy: boolean) => void;
}) {
  const [site, setSite] = useState(initialSite);
  const [part, setPart] = useState('');
  const [horizon, setHorizon] = useState(26);
  const [includeUnconfirmed, setIncludeUnconfirmed] = useState(true);
  const [period, setPeriod] = useState('Week');
  const [openAfterSave, setOpenAfterSave] = useState(false);
  const [prepared, setPrepared] = useState<ComponentMrpPrepared | null>(null);
  const [generating, setGenerating] = useState(false);
  const [saving, setSaving] = useState(false);
  const [unknownCommit, setUnknownCommit] = useState(false);
  const [pendingOperation, setPendingOperation] = useState<string | null>(null);
  const [message, setMessage] = useState('Enter a part and Generate a workbook.');
  const [preferenceWarning, setPreferenceWarning] = useState('');
  const [savedArtifact, setSavedArtifact] = useState<string | null>(null);
  const [openFailed, setOpenFailed] = useState(false);
  const [operationActive, setOperationActive] = useState(false);
  const generation = useRef(0);
  const abort = useRef<AbortController | null>(null);
  const saveBusy = useRef(false);
  const preferenceEpoch = useRef(0);
  const preferenceQueue = useRef(Promise.resolve());
  const mounted = useRef(true);
  const currentOperation = useRef<string | null>(null);
  const terminalOperations = useRef(new Set<string>());
  const activeOperation = useRef(false);
  const busyEpoch = useRef(0);
  const currentArtifact = useRef<string | null>(null);

  function isCurrent(operationId: string) {
    return mounted.current && currentOperation.current === operationId && !terminalOperations.current.has(operationId);
  }
  function startBusy() {
    const token = ++busyEpoch.current;
    saveBusy.current = true; setSaving(true); onBusyChange?.(true);
    return token;
  }
  function finishBusy(token: number) {
    if (!mounted.current || busyEpoch.current !== token) return;
    saveBusy.current = false; setSaving(false); onBusyChange?.(activeOperation.current);
  }
  function applyResult(operationId: string, result: Awaited<ReturnType<NonNullable<ReportSavePort['status']>>>) {
    if (!isCurrent(operationId)) return;
    const terminal = result.operationComplete !== false && ['saved', 'failed', 'cancelled'].includes(result.outcome);
    activeOperation.current = result.operationComplete === false;
    setOperationActive(activeOperation.current); setMessage(result.message);
    setPendingOperation(terminal ? null : operationId); setUnknownCommit(!terminal);
    if (result.outcome === 'saved') {
      currentArtifact.current = result.savedArtifactId ?? null;
      setSavedArtifact(currentArtifact.current); setOpenFailed(result.openFailed === true || result.message === 'Workbook saved, but could not be opened.');
    }
    if (terminal) {
      terminalOperations.current.add(operationId);
      finishBusy(busyEpoch.current);
      busyEpoch.current++; // Obsolete finally blocks cannot release a subsequent action.
    }
  }
  const handleOutcomeEvent = useEffectEvent((operationId: string, result: Awaited<ReturnType<NonNullable<ReportSavePort['status']>>>) => applyResult(operationId, result));

  useEffect(() => {
    mounted.current = true;
    const generationState = generation;
    const epoch = preferenceEpoch.current;
    void savePort.loadOpenPreference().then(value => {
      if (mounted.current && epoch === preferenceEpoch.current) setOpenAfterSave(value === true);
    }).catch(() => { if (mounted.current) setPreferenceWarning('Save preference could not be loaded; Open starts unchecked.'); });
    return () => { mounted.current = false; generationState.current++; abort.current?.abort(); };
  }, [savePort]);
  useEffect(() => { onBusyChange?.(saving || operationActive); }, [saving, operationActive, onBusyChange]);
  useEffect(() => {
    if (savePort !== desktopPort) return;
    let alive = true; let dispose: (() => void) | undefined;
    void import('@tauri-apps/api/event').then(({ listen }) => listen<{ operationId: string; result: Awaited<ReturnType<NonNullable<ReportSavePort['status']>>> }>('component-mrp-operation', event => {
      if (!alive) return;
      const { operationId, result } = event.payload;
      handleOutcomeEvent(operationId, result);
    })).then(unlisten => { if (alive) dispose = unlisten; else unlisten(); }).catch(() => {});
    return () => { alive = false; dispose?.(); };
  }, [savePort]);

  function invalidate() {
    generation.current++; abort.current?.abort(); setPrepared(null); setGenerating(false);
    setMessage('Inputs changed. Generate a new workbook.');
    void import('@tauri-apps/api/core').then(({ invoke }) => invoke('component_mrp_begin')).catch(() => {});
    // Saved-artifact retry is independent of prepared report invalidation.
  }
  async function prepare() {
    if (unknownCommit) return;
    const token = ++generation.current;
    abort.current?.abort(); const controller = new AbortController(); abort.current = controller;
    setPrepared(null); setGenerating(true); setMessage('Acquiring fresh report facts…');
    try {
      const result = await generate({ site, part, horizonWeeks: horizon, includeUnconfirmed, period }, controller.signal);
      if (!mounted.current || generation.current !== token) return;
      setPrepared(result); setMessage('Workbook prepared. Save As retains these exact bytes for retry.');
    } catch (error) {
      if (mounted.current && generation.current === token) setMessage(error instanceof Error ? error.message : 'Preparation failed.');
    } finally { if (mounted.current && generation.current === token) setGenerating(false); }
  }
  async function save() {
    if (!prepared || saveBusy.current || unknownCommit) return;
    const busyToken = startBusy();
    const operationId = crypto.randomUUID(); currentOperation.current = operationId;
    const intent = openAfterSave;
    try {
      const result = await savePort.save(prepared, intent, operationId);
      applyResult(operationId, result);
    } catch { if (isCurrent(operationId)) { setPendingOperation(operationId); setUnknownCommit(true); setMessage('Save response unavailable; commitment is unknown. Do not replay.'); } }
    finally { finishBusy(busyToken); }
  }
  function changeOpen(value: boolean) {
    preferenceEpoch.current++; setOpenAfterSave(value);
    // Serialize persistence so a late older write cannot overwrite the newest user choice.
    preferenceQueue.current = preferenceQueue.current.catch(() => {}).then(() => savePort.rememberOpenPreference(value))
      .catch(() => { if (mounted.current) setPreferenceWarning('Save preference could not be remembered; the session choice is retained.'); });
  }
  async function retryOpen() {
    if (!savedArtifact || saveBusy.current) return;
    const artifactId = savedArtifact; const operationId = currentOperation.current; const busyToken = startBusy();
    const ownsRetry = () => mounted.current && busyEpoch.current === busyToken && currentArtifact.current === artifactId && currentOperation.current === operationId;
    try { await savePort.open(artifactId); if (ownsRetry()) { setOpenFailed(false); setMessage('Workbook saved; opening request accepted.'); } }
    catch { if (ownsRetry()) setMessage('Workbook saved, but could not be opened.'); }
    finally { finishBusy(busyToken); }
  }
  async function reconcile() {
    if (!pendingOperation || !savePort.status || saveBusy.current) return;
    const operationId = pendingOperation; const busyToken = startBusy();
    try {
      const result = await savePort.status(operationId);
      applyResult(operationId, result);
    } catch { if (isCurrent(operationId)) setMessage('Operation status unavailable. Do not replay the save.'); }
    finally { finishBusy(busyToken); }
  }
  return <section className="reports-panel" aria-label="Reports">
    <nav className="reports-panel__selector" aria-label="Report selector">
      <button aria-current="page">Component MRP</button>
      {['Shipments-To-Go', 'S&OP', 'Open Order Report'].map(name => <button key={name} disabled>{name} — coming later</button>)}
    </nav>
    <div className="reports-panel__body">
      <h1>Component MRP</h1><p>Direct-part material requirements, independent of workspaces.</p>
      <form onSubmit={event => { event.preventDefault(); void prepare(); }}>
        <fieldset disabled={saving || operationActive || unknownCommit}>
          <label>Site<select value={site} onChange={event => { invalidate(); setSite(event.target.value); try { localStorage.setItem(siteKey, event.target.value); } catch { setPreferenceWarning('Site could not be remembered; the session choice is retained.'); } }}>
            {sites.map(value => <option key={value}>{value}</option>)}
          </select></label>
          <label>Part<input value={part} maxLength={256} onChange={event => { invalidate(); setPart(event.target.value); }} /></label>
          <label>Horizon<select value={horizon} onChange={event => { invalidate(); setHorizon(Number(event.target.value)); }}>
            {[13, 26, 52, 72].map(value => <option key={value} value={value}>{value} weeks</option>)}
          </select></label>
          <label>Period<select value={period} onChange={event => { invalidate(); setPeriod(event.target.value); }}>
            {['Day', 'Week', 'Month'].map(value => <option key={value}>{value}</option>)}
          </select></label>
          <label><input type="checkbox" checked={includeUnconfirmed} onChange={event => { invalidate(); setIncludeUnconfirmed(event.target.checked); }} />Include Unconfirmed POs</label>
          <button type="submit" disabled={generating || unknownCommit || !part.trim()}>Generate</button>
        </fieldset>
      </form>
      <label><input type="checkbox" checked={openAfterSave} onChange={event => changeOpen(event.target.checked)} />Open workbook after saving</label>
      <div className="reports-panel__actions"><button disabled={!prepared || saving || unknownCommit} onClick={() => void save()}>Save As…</button>
        {openFailed && savedArtifact && <button disabled={saving || operationActive} onClick={() => void retryOpen()}>Retry Open</button>}
        {unknownCommit && pendingOperation && savePort.status && <button disabled={saving} onClick={() => void reconcile()}>Check save status</button>}
      </div>
      {prepared && <p>Prepared: {prepared.fileName} · as of {prepared.asOfDate}{prepared.warning && ` · ${prepared.warning}`}</p>}
      <p role="status">{message}</p>{preferenceWarning && <p role="alert">{preferenceWarning}</p>}
    </div>
  </section>;
}

import { useEffect, useEffectEvent, useRef, useState } from 'react';
import { generateComponentMrp, type ComponentMrpPrepared, type ComponentMrpRequest } from '../api/componentMrpApi';
import { generateShipmentsToGo, type ShipmentsToGoPrepared, type ShipmentsToGoRequest } from '../api/shipmentsToGoApi';
import { generateSop, type SopPrepared, type SopRequest } from '../api/sopApi';
import { fiscalChangedEvent, fiscalStorageKey, sopCalendarFingerprint } from '../reports/sopPresentation';
import './ReportsPanel.css';
import { SopInputs } from '../reports/SopInputs';
import { generateOpenOrderReport, type OpenOrderReportPrepared, type OpenOrderReportRequest } from '../api/openOrderReportApi';
import { OpenOrderReportInputs } from '../reports/OpenOrderReportInputs';
import { HistoricalShipmentsReport } from '../reports/HistoricalShipmentsReport';
import { initialOor, oorPreferenceKey } from '../reports/openOrderReportPreferences';

const sites = ['SW', 'NW', 'AR', 'MN', 'MS', 'KV'] as const;
const siteKey = 'kst.componentMrp.site.v1';
type ReportKind = 'ComponentMrp' | 'ShipmentsToGo' | 'Sop' | 'OpenOrderReport' | 'HistoricalShipments';
type PreparedReport = ComponentMrpPrepared | ShipmentsToGoPrepared | SopPrepared | OpenOrderReportPrepared;
const sopPreferenceKey = 'kst.sop.inputs.v1';
function initialSop(): SopRequest {
  try {
    const value = JSON.parse(localStorage.getItem(sopPreferenceKey) ?? 'null') as SopRequest | null;
    if (value && sites.includes(value.site as typeof sites[number]) && ['planr', 'customer'].includes(value.groupBy)
      && Array.isArray(value.ranges) && value.ranges.length >= 1 && value.ranges.length <= 5
      && value.ranges.every(r => r && /^[0-9]{0,4}$/.test(r.from) && /^[0-9]{0,4}$/.test(r.to))) return value;
  } catch { /* established first-use defaults */ }
  return { site: 'SW', groupBy: 'planr', ranges: [{ from: '', to: '' }] };
}
const stgPreferenceKey = 'kst.shipmentsToGo.inputs.v1';
function initialStg() {
  try {
    const raw = JSON.parse(localStorage.getItem(stgPreferenceKey) ?? 'null') as { sites?: unknown; includeNextWeek?: unknown } | null;
    if (raw && Array.isArray(raw.sites) && raw.sites.every(value => sites.includes(value)) && new Set(raw.sites).size === raw.sites.length) {
      return { sites: raw.sites as string[], includeNextWeek: raw.includeNextWeek === true };
    }
  } catch { /* First-use defaults for missing/corrupt preferences. */ }
  return { sites: ['SW', 'NW', 'KV'], includeNextWeek: false };
}
function initialSite(): string {
  try { const value = localStorage.getItem(siteKey); return sites.includes(value as typeof sites[number]) ? value! : 'SW'; }
  catch { return 'SW'; }
}

export interface ReportSavePort {
  loadOpenPreference(): Promise<boolean>;
  rememberOpenPreference(value: boolean): Promise<void>;
  save(prepared: PreparedReport, openAfterSave: boolean, operationId?: string, kind?: ReportKind): Promise<{ outcome: 'saved' | 'cancelled' | 'failed' | 'unknown'; message: string; savedArtifactId?: string; operationComplete?: boolean; openFailed?: boolean }>;
  open(savedArtifactId: string, kind?: ReportKind): Promise<void>;
  status?(operationId: string, kind?: ReportKind): Promise<{ outcome: string; message: string; savedArtifactId?: string; openFailed?: boolean; operationComplete?: boolean }>;
  discard?(): Promise<void>;
}

// No browser download/direct-write fallback: it cannot acknowledge the required protected save.
const desktopPort: ReportSavePort = {
  async loadOpenPreference() { const { invoke } = await import('@tauri-apps/api/core'); return invoke<boolean>('component_mrp_open_preference'); },
  async rememberOpenPreference(value) { const { invoke } = await import('@tauri-apps/api/core'); await invoke('component_mrp_remember_open_preference', { value }); },
  async save(prepared, openAfterSave, suppliedOperationId, kind = 'ComponentMrp') {
    const { invoke } = await import('@tauri-apps/api/core');
    const operationId = suppliedOperationId ?? crypto.randomUUID();
    try {
      const result = await invoke<Awaited<ReturnType<ReportSavePort['save']>>>('component_mrp_save', { operationId, preparationId: prepared.preparationId, openAfterSave, reportKind: kind });
      if (result.outcome === 'unknown') throw new Error(operationId);
      return result;
    }
    catch { throw new Error(operationId); }
  },
  async status(operationId, kind = 'ComponentMrp') { const { invoke } = await import('@tauri-apps/api/core'); return invoke('component_mrp_status', { operationId, reportKind: kind }); },
  async open(savedArtifactId, kind = 'ComponentMrp') { const { invoke } = await import('@tauri-apps/api/core'); await invoke('component_mrp_open', { artifactId: savedArtifactId, requestId: crypto.randomUUID(), reportKind: kind }); },
  async discard() { const { invoke } = await import('@tauri-apps/api/core'); await invoke('report_discard'); },
};

export function ReportsPanel({ generate = generateComponentMrp, generateStg = generateShipmentsToGo, generateSopReport = generateSop, generateOor = generateOpenOrderReport, savePort = desktopPort, onBusyChange, active = true }: {
  generate?: (request: ComponentMrpRequest, signal?: AbortSignal) => Promise<ComponentMrpPrepared>;
  savePort?: ReportSavePort;
  onBusyChange?: (busy: boolean) => void;
  generateStg?: (request: ShipmentsToGoRequest, signal?: AbortSignal) => Promise<ShipmentsToGoPrepared>;
  generateSopReport?: (request: SopRequest, signal?: AbortSignal) => Promise<SopPrepared>;
  generateOor?: (request: OpenOrderReportRequest, signal?: AbortSignal) => Promise<OpenOrderReportPrepared>;
  active?: boolean;
}) {
  const [site, setSite] = useState(initialSite);
  const [kind, setKind] = useState<ReportKind>('ComponentMrp');
  const [stg, setStg] = useState(initialStg);
  const [sop, setSop] = useState(initialSop);
  const [oor, setOor] = useState(initialOor);
  const preparedCalendar = useRef<string | null>(null);
  const [part, setPart] = useState('');
  const [horizon, setHorizon] = useState(26);
  const [includeUnconfirmed, setIncludeUnconfirmed] = useState(true);
  const [period, setPeriod] = useState('Week');
  const [openAfterSave, setOpenAfterSave] = useState(false);
  const [prepared, setPrepared] = useState<PreparedReport | null>(null);
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
  const presentationEpoch = useRef(0);
  const operationEpoch = useRef(0);
  const operationKind = useRef<ReportKind>('ComponentMrp');
  const activeRef = useRef(active); activeRef.current = active;
  const previousActive = useRef(active);
  const discardQueue = useRef(Promise.resolve());

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
    const ownsPresentation = activeRef.current && operationEpoch.current === presentationEpoch.current;
    setOperationActive(activeOperation.current);
    setMessage(ownsPresentation ? result.message : terminal ? 'Generated report discarded.' : 'A departed report save requires status reconciliation. Do not replay.');
    setPendingOperation(terminal ? null : operationId); setUnknownCommit(!terminal);
    if (result.outcome === 'saved' && ownsPresentation) {
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
  const handleDeparture = useEffectEvent(() => discardPresentation());
  const handleCalendarChange = useEffectEvent(() => {
    if (kind === 'Sop' && (preparedCalendar.current !== null || generating)) invalidate();
  });
  useEffect(() => {
    const storage = (event: StorageEvent) => { if (event.key === fiscalStorageKey) handleCalendarChange(); };
    const changed = () => handleCalendarChange();
    window.addEventListener(fiscalChangedEvent, changed); window.addEventListener('storage', storage);
    return () => { window.removeEventListener(fiscalChangedEvent, changed); window.removeEventListener('storage', storage); };
  }, []);
  useEffect(() => {
    if (previousActive.current && !active) handleDeparture();
    previousActive.current = active;
  }, [active]);

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
    preparedCalendar.current = null;
    generation.current++; abort.current?.abort(); setPrepared(null); setGenerating(false);
    setMessage('Inputs changed. Generate a new workbook.');
    void import('@tauri-apps/api/core').then(({ invoke }) => invoke('component_mrp_begin')).catch(() => {});
    // Saved-artifact retry is independent of prepared report invalidation.
  }
  function discardPresentation() {
    preparedCalendar.current = null;
    presentationEpoch.current++; generation.current++; abort.current?.abort();
    setPrepared(null); setGenerating(false); setSavedArtifact(null); currentArtifact.current = null; setOpenFailed(false);
    setMessage('Generated report discarded.');
    discardQueue.current = discardQueue.current.catch(() => {}).then(() => savePort.discard?.())
      .then(() => {}).catch(() => { if (mounted.current) setPreferenceWarning('Host report discard could not be confirmed. Generate again before saving.'); });
  }
  function selectReport(next: ReportKind) { if (next === kind) return; discardPresentation(); setKind(next); }
  function changeStg(value: typeof stg) {
    invalidate(); setStg(value);
    try { localStorage.setItem(stgPreferenceKey, JSON.stringify(value)); }
    catch { setPreferenceWarning('Report inputs could not be remembered; the session choice is retained.'); }
  }
  function changeSop(value: SopRequest) {
    invalidate(); setSop(value);
    try { localStorage.setItem(sopPreferenceKey, JSON.stringify(value)); }
    catch { setPreferenceWarning('Report inputs could not be remembered; the session choice is retained.'); }
  }
  async function prepare() {
    if (unknownCommit || saveBusy.current || activeOperation.current) return;
    const token = ++generation.current;
    abort.current?.abort(); const controller = new AbortController(); abort.current = controller;
    setPrepared(null); setGenerating(true); setMessage('Acquiring fresh report facts…');
    try {
      await discardQueue.current;
      if (controller.signal.aborted || !activeRef.current) return;
      const result = kind === 'ComponentMrp'
        ? await generate({ site, part, horizonWeeks: horizon, includeUnconfirmed, period }, controller.signal)
        : kind === 'ShipmentsToGo' ? await generateStg({ sites: stg.sites, includeNextWeek: stg.includeNextWeek }, controller.signal)
        : kind === 'OpenOrderReport' ? await generateOor(oor, controller.signal)
        : await generateSopReport({ ...sop, ranges: sop.ranges.filter(r => r.from || r.to) }, controller.signal);
      if (!mounted.current || generation.current !== token || !activeRef.current) return;
      if ('outcome' in result && result.outcome === 'noData') { setMessage(kind === 'Sop' ? 'No S&OP data found for the selected ranges.' : kind === 'OpenOrderReport' ? 'No open orders found for the specified filters.' : 'No shipments to go found for the selected sites.'); return; }
      if (kind === 'Sop') preparedCalendar.current = sopCalendarFingerprint();
      setPrepared(result); setMessage('Workbook prepared. Save As retains these exact bytes for retry.');
    } catch (error) {
      if (mounted.current && generation.current === token) setMessage(error instanceof Error ? error.message : 'Preparation failed.');
    } finally { if (mounted.current && generation.current === token) setGenerating(false); }
  }
  async function save() {
    if (!prepared || saveBusy.current || unknownCommit) return;
    if (kind === 'Sop') {
      try { if (preparedCalendar.current !== sopCalendarFingerprint()) { invalidate(); return; } }
      catch { invalidate(); return; }
    }
    const busyToken = startBusy();
    const operationId = crypto.randomUUID(); currentOperation.current = operationId;
    operationEpoch.current = presentationEpoch.current; operationKind.current = kind;
    const intent = openAfterSave;
    try {
      const result = kind === 'ComponentMrp' ? await savePort.save(prepared, intent, operationId)
        : await savePort.save(prepared, intent, operationId, kind);
      applyResult(operationId, result);
    } catch { if (isCurrent(operationId)) { setPendingOperation(operationId); setUnknownCommit(true);
      setMessage(operationEpoch.current === presentationEpoch.current && activeRef.current
        ? 'Save response unavailable; commitment is unknown. Do not replay.' : 'A departed report save requires status reconciliation. Do not replay.'); } }
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
    try { if (kind === 'ComponentMrp') await savePort.open(artifactId); else await savePort.open(artifactId, kind); if (ownsRetry()) { setOpenFailed(false); setMessage('Workbook saved; opening request accepted.'); } }
    catch { if (ownsRetry()) setMessage('Workbook saved, but could not be opened.'); }
    finally { finishBusy(busyToken); }
  }
  async function reconcile() {
    if (!pendingOperation || !savePort.status || saveBusy.current) return;
    const operationId = pendingOperation; const busyToken = startBusy();
    try {
      const result = operationKind.current === 'ComponentMrp' ? await savePort.status(operationId) : await savePort.status(operationId, operationKind.current);
      applyResult(operationId, result);
    } catch { if (isCurrent(operationId)) setMessage('Operation status unavailable. Do not replay the save.'); }
    finally { finishBusy(busyToken); }
  }
  return <section className={`reports-panel${kind === 'Sop' ? ' reports-panel--sop' : kind === 'OpenOrderReport' ? ' reports-panel--oor' : kind === 'HistoricalShipments' ? ' reports-panel--historical' : ''}`} aria-label="Reports">
    <nav className="reports-panel__selector" aria-label="Report selector">
      <button aria-current={kind === 'ComponentMrp' ? 'page' : undefined} onClick={() => selectReport('ComponentMrp')}>Component MRP</button>
      <button aria-current={kind === 'ShipmentsToGo' ? 'page' : undefined} onClick={() => selectReport('ShipmentsToGo')}>Shipments-To-Go</button>
      <button aria-current={kind === 'Sop' ? 'page' : undefined} onClick={() => selectReport('Sop')}>{'S&OP'}</button>
      <button aria-current={kind === 'OpenOrderReport' ? 'page' : undefined} onClick={() => selectReport('OpenOrderReport')}>Open Order Report</button>
      <button aria-current={kind === 'HistoricalShipments' ? 'page' : undefined} onClick={() => selectReport('HistoricalShipments')}>Historical Shipments</button>
    </nav>
    <div className="reports-panel__body">
      <h1>{kind === 'ComponentMrp' ? 'Component MRP' : kind === 'Sop' ? 'S&OP' : kind === 'OpenOrderReport' ? 'Open Order Report' : kind === 'HistoricalShipments' ? 'Historical Shipments' : 'Shipments-To-Go'}</h1><p>Independent report generation outside workspace populations.</p>
      {kind === 'HistoricalShipments' ? <HistoricalShipmentsReport active={active} onBusyChange={onBusyChange} openAfterSave={openAfterSave} onOpenAfterSaveChange={changeOpen} /> : <>
      <form onSubmit={event => { event.preventDefault(); void prepare(); }}>
        <fieldset disabled={saving || operationActive || unknownCommit}>
          {kind === 'ComponentMrp' ? <>
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
           </> : kind === 'Sop' ? <SopInputs value={sop} onChange={changeSop} /> : kind === 'OpenOrderReport' ? <OpenOrderReportInputs value={oor} onChange={value => {
             invalidate(); setOor(value);
             try { localStorage.setItem(oorPreferenceKey, JSON.stringify(value.optionalFields)); }
             catch { setPreferenceWarning('Column selections could not be remembered; the session choice is retained.'); }
           }} /> : <>
            <div role="group" aria-label="Select sites">{sites.map(value => <label key={value}>
              <input type="checkbox" checked={stg.sites.includes(value)} onChange={event => changeStg({ ...stg,
                sites: event.target.checked ? [...stg.sites, value] : stg.sites.filter(site => site !== value) })} />{value}</label>)}</div>
            <label><input type="checkbox" checked={stg.includeNextWeek} onChange={event => changeStg({ ...stg, includeNextWeek: event.target.checked })} />Include Next Week?</label>
          </>}
          <button type="submit" disabled={generating || unknownCommit || (kind === 'ComponentMrp' ? !part.trim() : kind === 'Sop'
            ? !sop.ranges.some(r => r.from || r.to) || sop.ranges.some(r => !!(r.from || r.to) && (!/^[0-9]{4}$/.test(r.from) || !/^[0-9]{4}$/.test(r.to)))
            : kind === 'OpenOrderReport' ? ![oor.customerName, oor.customerNumber, oor.salespersonId, oor.productLineFrom, oor.productLineTo, oor.ios, oor.site].some(v => v?.trim())
            : stg.sites.length === 0)}>Generate</button>
        </fieldset>
      </form>
      <label><input type="checkbox" checked={openAfterSave} onChange={event => changeOpen(event.target.checked)} />Open workbook after saving</label>
      <div className="reports-panel__actions"><button disabled={!prepared || saving || unknownCommit} onClick={() => void save()}>Save As…</button>
        {openFailed && savedArtifact && <button disabled={saving || operationActive} onClick={() => void retryOpen()}>Retry Open</button>}
        {unknownCommit && pendingOperation && savePort.status && <button disabled={saving} onClick={() => void reconcile()}>Check save status</button>}
      </div>
      {prepared && <p>Prepared: {prepared.fileName} · as of {prepared.asOfDate}{'warning' in prepared && prepared.warning && ` · ${prepared.warning}`}</p>}
      <p role="status">{message}</p>{preferenceWarning && <p role="alert">{preferenceWarning}</p>}</>}
    </div>
  </section>;
}

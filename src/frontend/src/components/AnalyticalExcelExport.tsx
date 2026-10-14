import { useEffect, useRef, useState } from 'react';
import { AnalyticalExportClient, type AnalyticalRequest, type AnalyticalPrepared, type AnalyticalSaveResult } from '../api/analyticalExportApi';
import { beginAnalyticalSave, recordAnalyticalSave, useAnalyticalSaveRecovery } from '../shared/analyticalSaveRecovery';

/** Keyed by complete source/selection identity. Collapse/scroll never contribute to that identity. */
export function AnalyticalExcelExport({ request, contextKey, disabled = false, clientFactory = () => new AnalyticalExportClient() }: {
  request: () => AnalyticalRequest; contextKey: string; disabled?: boolean; clientFactory?: () => AnalyticalExportClient;
}) {
  const [attempt, setAttempt] = useState(0);
  return <ExportVisit key={`${contextKey}:${attempt}`} request={request} disabled={disabled} clientFactory={clientFactory} restart={() => setAttempt(n => n + 1)} />;
}

function ExportVisit({ request, disabled, clientFactory, restart }: { request: () => AnalyticalRequest; disabled: boolean; clientFactory: () => AnalyticalExportClient; restart: () => void }) {
  const [createClient] = useState(() => clientFactory);
  const clientRef = useRef<AnalyticalExportClient | null>(null);
  const recovery = useAnalyticalSaveRecovery();
  const mounted = useRef(true);
  const locked = useRef(false);
  const sequence = useRef(0);
  const captured = useRef<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [prepared, setPrepared] = useState<AnalyticalPrepared | null>(null);
  const [pending, setPending] = useState<string | null>(null);
  const [message, setMessage] = useState('');
  const [openAfterSave, setOpenAfterSave] = useState(false);
  const [artifact, setArtifact] = useState<string | null>(null);
  const preferenceEdited = useRef(false);
  useEffect(() => {
    const client = createClient();
    clientRef.current = client;
    mounted.current = true;
    void import('@tauri-apps/api/core').then(({ invoke }) => invoke<boolean>('component_mrp_open_preference')).then(value => {
      if (mounted.current && !preferenceEdited.current) setOpenAfterSave(value === true);
    }).catch(() => {});
    return () => { mounted.current = false; clientRef.current = null; void client.release().catch(() => {}); };
  }, [createClient]);
  function apply(result: AnalyticalSaveResult, id: string) {
    recordAnalyticalSave(id, result);
    if (!mounted.current) return;
    const terminal = result.operationComplete !== false && ['saved', 'cancelled', 'failed'].includes(result.outcome);
    setPending(terminal ? null : id); setMessage(result.message);
    if (result.outcome === 'saved') { setPrepared(null); setArtifact(result.openFailed ? result.savedArtifactId ?? null : null); }
  }
  async function prepare() {
    const client = clientRef.current;
    if (!client) return;
    if (locked.current || disabled || pending) return;
    locked.current = true; setBusy(true); const token = ++sequence.current;
    setMessage('Preparing Excel from loaded data…');
    try {
      const inputs = request(); captured.current = JSON.stringify(inputs);
      const value = await client.prepare(inputs);
      if (mounted.current && sequence.current === token) { setPrepared(value); setMessage('Excel ready. Save As uses these captured values and selections.'); }
    } catch (e) { if (mounted.current && sequence.current === token) setMessage(e instanceof Error ? e.message : 'Workbook preparation failed.'); }
    finally { if (mounted.current && sequence.current === token) { locked.current = false; setBusy(false); } }
  }
  async function save() {
    const client = clientRef.current;
    if (!client) return;
    if (locked.current || !prepared || disabled || pending) return;
    try {
      if (captured.current !== JSON.stringify(request())) { restart(); return; }
    } catch (error) {
      setPrepared(null); setMessage(error instanceof Error ? error.message : 'Export context changed. Prepare again.'); return;
    }
    locked.current = true; setBusy(true); const id = crypto.randomUUID();
    beginAnalyticalSave(id, client);
    try { apply(await client.save(prepared, openAfterSave, id), id); }
    catch { recordAnalyticalSave(id); if (mounted.current) { setPending(id); setMessage('Save outcome unknown. Check save status before trying again.'); } }
    finally { if (mounted.current) { locked.current = false; setBusy(false); } }
  }
  async function check() {
    const client = clientRef.current;
    if (!client) return;
    if (!pending || locked.current) return;
    locked.current = true; setBusy(true);
    try { apply(await client.status(pending), pending); }
    catch { if (mounted.current) setMessage('Save status unavailable. Do not replay this save.'); }
    finally { if (mounted.current) { locked.current = false; setBusy(false); } }
  }
  async function open() {
    const client = clientRef.current;
    if (!client) return;
    if (!artifact || locked.current) return;
    locked.current = true; setBusy(true);
    try { await client.open(artifact); if (mounted.current) { setArtifact(null); setMessage('Workbook saved; opening request accepted.'); } }
    catch { if (mounted.current) setMessage('Workbook saved, but could not be opened.'); }
    finally { if (mounted.current) { locked.current = false; setBusy(false); } }
  }
  return <div className="analytical-excel-export" aria-label="Analytical Excel export">
    <button type="button" disabled={disabled || busy || !!pending || !!prepared || recovery?.pending} onClick={() => void prepare()}>Export Excel</button>
    {busy && !prepared && !pending && <button type="button" onClick={restart}>Cancel preparation</button>}
    {prepared && <button type="button" disabled={disabled || busy || !!pending} onClick={() => void save()}>Save Excel As…</button>}
    {pending && <button type="button" disabled={busy} onClick={() => void check()}>Check Excel save status</button>}
    {artifact && <button type="button" disabled={busy} onClick={() => void open()}>Retry Excel Open</button>}
    <label><input type="checkbox" checked={openAfterSave} onChange={e => {
      preferenceEdited.current = true; setOpenAfterSave(e.target.checked);
      void import('@tauri-apps/api/core').then(({ invoke }) => invoke('component_mrp_remember_open_preference', { value: e.target.checked })).catch(() => {});
    }} />Open Excel after saving</label>
    {message && <span role="status">{message}</span>}
  </div>;
}

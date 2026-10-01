import { useEffect, useMemo, useRef, useState } from 'react';
import { ApiClient, ApiError, type OpenOrdersResponseDto, type OpenOrdersQxtendResponseDto } from '../api/client';
import { resolveBackendBaseUrl } from '../api/tauri-bridge';
import { saveLongTermShortagesWorkbook } from '../longTermShortages/saveLongTermShortagesWorkbook';
import { COLUMNS, DATE_COLUMNS, NUMERIC_COLUMNS, defaultLayout, emptyFilters, formatReportDate, formatReportTimestamp, loadLayout, reportRows, saveLayout, type Filters, type Layout, type ColumnId } from '../openOrders/report';
import { FilterBuilder } from '../openOrders/FilterBuilder';
import { openOrdersFileName } from '../openOrders/reportFileName';
import { changed, equalDecimal, issues, keyOf, multiply, parsePlanningDate, planningNumberDisplay, subtract, valuesOf, REASONS, type Editable, type Proposal } from '../openOrders/planning';
import { loadPlanLayout, planDefaultLayout, PLAN_REQUIRED, savePlanLayout, type LayoutForPlan, type PlanColumnId } from '../openOrders/planLayout';
import { saveQxtendCsv } from '../openOrders/saveQxtendCsv';
import { qxtendExportFeedback } from '../openOrders/exportFeedback';
import './OpenOrdersPanel.css';

type Failure = 'snapshot-changed' | 'unavailable' | 'error';

export function OpenOrdersPanel({ assignmentId, snapshotId, workspaceName, site = '', onDirtyChange }: { assignmentId: string; snapshotId: string | null; workspaceName?: string | null; site?: string; onDirtyChange?: (dirty: boolean) => void }) {
  const [layoutState, setLayout] = useState<{ identity: string; value: Layout }>(() => ({ identity: assignmentId, value: loadLayout(assignmentId) }));
  const [planLayoutState, setPlanLayout] = useState<{ identity: string; value: LayoutForPlan }>(() => ({ identity: assignmentId, value: loadPlanLayout(assignmentId) }));
  const [filterState, setFilters] = useState<{ identity: string; value: Filters }>(() => ({ identity: assignmentId, value: emptyFilters }));
  const [loaded, setLoaded] = useState<{ identity: string; report: OpenOrdersResponseDto } | null>(null);
  const [pendingIdentity, setPending] = useState<string | null>(null);
  const [failureState, setFailure] = useState<{ identity: string; kind: Failure } | null>(null);
  const [feedback, setFeedback] = useState<{ identity: string; text: string; kind: 'saved' | 'cancelled' | 'error' } | null>(null);
  const [exportingState, setExporting] = useState<{ identity: string; active: boolean } | null>(null);
  const [prepared, setPrepared] = useState<{ identity: string; signature: string; files: OpenOrdersQxtendResponseDto['files']; statuses: Record<string, string> } | null>(null);
  const [page, setPage] = useState(0);
  const [planMode, setPlanMode] = useState(false);
  const [saveDraft, setSaveDraft] = useState(false);
  const [proposals, setProposals] = useState<Record<string, Proposal>>({});
  const [proposalAssignment, setProposalAssignment] = useState(assignmentId);
  const [savedState, setSavedState] = useState<'idle' | 'checking' | 'none' | 'restored' | 'unavailable' | 'presence-error'>('idle');
  const [checkingDraftFor, setCheckingDraftFor] = useState<string | null>(null);
  const [lastDraftMps, setLastDraftMps] = useState<string | null>(snapshotId);
  const [restoreCandidate, setRestoreCandidate] = useState<Proposal[] | null>(null);
  const [restoreAttempt, setRestoreAttempt] = useState(0);
  const [restoreIssues, setRestoreIssues] = useState<Record<string, string[]>>({});
  const [draftError, setDraftError] = useState<string | null>(null);
  const [onlyChanged, setOnlyChanged] = useState(false);
  const [clearConfirmation, setClearConfirmation] = useState(false);
  const [saveVersion, setSaveVersion] = useState(0);
  const [saveOffPending, setSaveOffPending] = useState(false);
  const [dateInputs, setDateInputs] = useState<Record<string, string>>({});
  const [focusedNumber, setFocusedNumber] = useState<string | null>(null);
  const [dateTouched, setDateTouched] = useState<Record<string, boolean>>({});
  const invalidDateInputs = useRef(new Set<string>());
  const saveGate = useRef(Promise.resolve());
  const saveGeneration = useRef(0);
  const restoredFor = useRef<string | null>(null);
  if (proposalAssignment !== assignmentId) {
    setProposalAssignment(assignmentId); setProposals({}); setSaveDraft(false); setSavedState('idle');
    setPlanMode(false); setRestoreCandidate(null); setRestoreIssues({}); setLastDraftMps(snapshotId);
    setDateInputs({}); setDateTouched({}); setFocusedNumber(null);
  }
  const requestId = useRef(0);
  const feedbackTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const identity = `${assignmentId}:${snapshotId ?? ''}`;
  useEffect(() => { invalidDateInputs.current.clear(); }, [assignmentId]);
  const exporting = exportingState?.identity === identity && exportingState.active;
  const layout = layoutState.identity === assignmentId ? layoutState.value : loadLayout(assignmentId);
  const planLayout = planLayoutState.identity === assignmentId ? planLayoutState.value : loadPlanLayout(assignmentId);
  const filters = filterState.identity === assignmentId ? filterState.value : emptyFilters;
  const pending = pendingIdentity === identity;
  const failure = failureState?.identity === identity ? failureState.kind : null;
  const report = loaded?.identity === identity ? loaded.report : null;
  const sourceChanged = ((loaded !== null && loaded.identity !== identity) || lastDraftMps !== snapshotId) && proposalAssignment === assignmentId && Object.values(proposals).some(changed);
  const rows = useMemo(() => reportRows(report?.lines ?? [], filters), [report, filters]);
  const reportVisible = layout.order.filter(id => layout.visible.includes(id));
  const activeLayout = planMode ? planLayout : layout;
  const visible = activeLayout.order.filter(id => activeLayout.visible.includes(id));
  const rowByKey = new Map((report?.lines ?? []).map(line => [keyOf(line.key), line]));
  const scopedProposals = proposalAssignment === assignmentId ? proposals : {};
  const staged = Object.values(scopedProposals).filter(changed);
  const exportSignature = JSON.stringify([report?.openOrdersSnapshotId, staged]);
  const conflicts = staged.filter(p => sourceChanged || [...issues(p, rowByKey.get(keyOf(p.key)), !!report?.isStale || !!failure || pending || !report), ...(restoreIssues[keyOf(p.key)] ?? [])].some(i => i !== 'Reason Code required.'));
  const incomplete = staged.filter(p => !p.reasonCode);
  const invalidDateEntries = Object.values(dateInputs).filter(text => parsePlanningDate(text) === undefined).length;
  const displayedRows = planMode && onlyChanged ? rows.filter(r => scopedProposals[keyOf(r.key)] && changed(scopedProposals[keyOf(r.key)])) : rows;
  const pageSize = 100;
  const pageCount = Math.max(1, Math.ceil(displayedRows.length / pageSize));
  const currentPage = Math.min(page, pageCount - 1);
  const hiddenChanges = staged.filter(p => !displayedRows.slice(currentPage * pageSize, (currentPage + 1) * pageSize).some(r => keyOf(r.key) === keyOf(p.key))).length;

  useEffect(() => {
    const timer = feedbackTimer;
    return () => {
      if (timer.current) clearTimeout(timer.current);
    };
  }, [identity]);

  useEffect(() => {
    onDirtyChange?.((staged.length > 0 || invalidDateEntries > 0) && (!saveDraft || !!draftError || saveVersion > 0 || invalidDateEntries > 0));
  }, [staged.length, invalidDateEntries, saveDraft, draftError, saveVersion, onDirtyChange]);

  useEffect(() => {
    const dirty = (staged.length > 0 || invalidDateEntries > 0) && (!saveDraft || !!draftError || saveVersion > 0 || invalidDateEntries > 0);
    if (!dirty) return;
    const warn = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', warn);
    return () => window.removeEventListener('beforeunload', warn);
  }, [staged.length, invalidDateEntries, saveDraft, draftError, saveVersion]);

  useEffect(() => () => { onDirtyChange?.(false); }, [onDirtyChange]);


  const showFeedback = (text: string, kind: 'saved' | 'cancelled' | 'error') => {
    if (feedbackTimer.current) clearTimeout(feedbackTimer.current);
    setFeedback({ identity, text, kind });
    feedbackTimer.current = kind === 'error' ? null : setTimeout(() => {
      setFeedback(current => current?.identity === identity && current.text === text ? null : current);
      feedbackTimer.current = null;
    }, 5_000);
  };

  useEffect(() => {
    if (!snapshotId) { requestId.current++; return; }
    const current = ++requestId.current;
    queueMicrotask(() => { if (current === requestId.current) { setPending(identity); setFailure(null); } });
    void resolveBackendBaseUrl().then(url => new ApiClient(url).getOpenOrders(assignmentId, snapshotId))
      .then(data => { if (current === requestId.current) { setLoaded({ identity, report: data }); setPage(0); } })
      .catch((error: unknown) => { if (current === requestId.current) setFailure({ identity, kind: error instanceof ApiError && error.status === 409 ? 'snapshot-changed' : error instanceof ApiError && error.status === 503 ? 'unavailable' : 'error' }); })
      .finally(() => { if (current === requestId.current) setPending(null); });
    return () => { if (requestId.current === current) requestId.current = current + 1; };
  }, [assignmentId, snapshotId, identity]);

  // A draft is never restored from GET's potentially cached report. The restore endpoint
  // refreshes against QAD and supplies the new report and per-row reconciliation together.
  useEffect(() => {
    if (!snapshotId || restoredFor.current === `${assignmentId}:${restoreAttempt}:${snapshotId}`) return;
    restoredFor.current = `${assignmentId}:${restoreAttempt}:${snapshotId}`;
    let active = true;
    let presenceFound = false;
    void resolveBackendBaseUrl().then(url => new ApiClient(url).getOpenOrdersDraftPresence(assignmentId))
      .then(async presence => {
        if (!active) return;
        if (!presence.exists) { setSavedState('none'); return; }
        presenceFound = true;
        setCheckingDraftFor(identity); setSavedState('checking');
        const result = await new ApiClient(await resolveBackendBaseUrl()).restoreOpenOrdersDraft(assignmentId, snapshotId);
        if (!active) return;
        if (!result.exists) { setSavedState(result.warning ? 'unavailable' : 'none'); if (result.warning) setDraftError(result.warning); return; }
        if (!result.restored || !result.freshReport) {
          if (!sourceChanged) setProposals(Object.fromEntries(result.rows.map(r => [keyOf(r.proposal.key), r.proposal])));
          setRestoreIssues(Object.fromEntries(result.rows.map(r => [keyOf(r.proposal.key), r.issues])));
          setPlanMode(true); setSavedState('unavailable'); setDraftError(result.warning ?? 'Fresh report unavailable; retry draft validation.'); return;
        }
        requestId.current++; // a concurrent cached GET must never replace this fresh validation report
        setPending(null); setFailure(null);
        setLoaded({ identity, report: result.freshReport });
        if (sourceChanged) { setSavedState('unavailable'); setDraftError('MPS snapshot changed. In-memory proposals remain; review conflicts before saving.'); return; }
        setLastDraftMps(snapshotId);
        setRestoreIssues(Object.fromEntries(result.rows.map(r => [keyOf(r.proposal.key), r.issues])));
        setRestoreCandidate(result.rows.map(r => r.proposal));
        setSavedState('restored');
      }).catch(() => { if (active) {
        setSavedState(presenceFound ? 'unavailable' : 'presence-error');
        setDraftError(presenceFound ? 'Draft check failed. Retry fresh validation; saved data remains on disk.' : 'Could not check for a saved draft. Retry the draft check.');
      } })
      .finally(() => { if (active) setCheckingDraftFor(null); });
    return () => { active = false; };
  }, [assignmentId, snapshotId, identity, restoreAttempt, sourceChanged]);

  const persist = (next: Record<string, Proposal>, enabled = saveDraft, against = report, afterRefresh = false,
    invalidInput = invalidDateEntries > 0) => {
    setProposals(next);
    if (enabled && (invalidInput || [...invalidDateInputs.current].some(id => id.startsWith(`${assignmentId}:`)))) {
      setDraftError('Finish or correct the date entry before saving this draft.'); return;
    }
    if (!enabled || saveOffPending || !snapshotId || !against || against.isStale || (!afterRefresh && !!failure) || savedState === 'unavailable' || savedState === 'presence-error' || restoreCandidate || sourceChanged) { if (enabled) setDraftError('Draft cannot be saved until a current report is available and the saved draft is resolved.'); return; }
    setSaveVersion(v => v + 1);
    const generation = saveGeneration.current;
    saveGate.current = saveGate.current.then(async () => {
      const request = { mpsSnapshotId: snapshotId, openOrdersSnapshotId: against.openOrdersSnapshotId, proposals: Object.values(next).filter(changed) };
      await new ApiClient(await resolveBackendBaseUrl()).saveOpenOrdersDraft(assignmentId, request);
      if (generation === saveGeneration.current) { setDraftError(null); setSaveVersion(v => Math.max(0, v - 1)); }
    }).catch(() => { if (generation === saveGeneration.current) { setDraftError('Save Draft failed. In-memory changes remain; retry Save Draft.'); setSaveVersion(v => Math.max(0, v - 1)); } });
  };
  const update = (row: typeof rows[number], field: Editable | 'reasonCode', value: string | null) => {
    const key = keyOf(row.key);
    const original = proposals[key]?.original ?? valuesOf(row);
    const prior = proposals[key] ?? { key: row.key, site: row.site, itemNumber: row.itemNumber, original, proposed: original, reasonCode: null };
    // Clearing a Reason Code on a no-op row does not stage a new change.
    const nextProposal: Proposal = field === 'reasonCode' ? { ...prior, reasonCode: value }
      : { ...prior, proposed: { ...prior.proposed, [field]: value } };
    const next = { ...proposals };
    if (changed(nextProposal)) next[key] = nextProposal; else delete next[key];
    if (!changed(nextProposal)) setRestoreIssues(prev => { const copy = { ...prev }; delete copy[key]; return copy; });
    if (!saveDraft) setProposals(next);
    else persist(next, true, report, false, Object.entries(dateInputs).some(([id, text]) =>
      id !== `${key}:${field}` && parsePlanningDate(text) === undefined));
  };
  const clearDateInputs = (key: string) => {
    for (const id of invalidDateInputs.current) if (id.startsWith(`${assignmentId}:${key}:`)) invalidDateInputs.current.delete(id);
    setDateInputs(previous => Object.fromEntries(Object.entries(previous).filter(([id]) => !id.startsWith(`${key}:`))));
    setDateTouched(previous => Object.fromEntries(Object.entries(previous).filter(([id]) => !id.startsWith(`${key}:`))));
  };
  const undo = (key: string) => { const next = { ...proposals }; delete next[key]; clearDateInputs(key); setRestoreIssues(prev => { const copy = { ...prev }; delete copy[key]; return copy; }); persist(next); };
  const acceptRestoredDraft = () => {
    if (!restoreCandidate) return;
    setProposals(Object.fromEntries(restoreCandidate.map(p => [keyOf(p.key), p])));
    setSaveDraft(true); setPlanMode(true); setRestoreCandidate(null);
  };

  const refresh = async () => {
    if (!snapshotId || pending) return;
    const current = ++requestId.current;
    setPending(identity); setFailure(null); setFeedback(null);
    try {
      const data = await new ApiClient(await resolveBackendBaseUrl()).refreshOpenOrders(assignmentId, snapshotId);
      if (current === requestId.current) { setLoaded({ identity, report: data }); setPage(0); if (saveDraft && !data.isStale && !sourceChanged) persist(proposals, true, data, true); }
    } catch (error) {
      if (current === requestId.current) setFailure({ identity, kind: error instanceof ApiError && error.status === 409 ? 'snapshot-changed' : error instanceof ApiError && error.status === 503 ? 'unavailable' : 'error' });
    } finally { if (current === requestId.current) setPending(null); }
  };
  const exportAll = async () => {
    if (!report || !snapshotId || !staged.length || exporting) return;
    const generation = requestId.current;
    setPrepared(null); setExporting({ identity, active: true }); setFeedback(null);
    try {
      const result = await new ApiClient(await resolveBackendBaseUrl()).exportOpenOrdersQxtend(assignmentId, {
        mpsSnapshotId: snapshotId, openOrdersSnapshotId: report.openOrdersSnapshotId, proposals: staged,
      });
      if (result.files.length === 0) throw new Error('No applicable CSV files');
      // A report/mode/workspace change while validation is pending cannot authorize an old UI context.
      if (requestId.current !== generation) return;
      setPrepared({ identity, signature: exportSignature, files: result.files, statuses: {} });
      showFeedback(`${result.files.length} CSV file(s) prepared after fresh validation. Choose Save As for each file; QXtend has not accepted them.`, 'saved');
    } catch (error) {
      showFeedback(qxtendExportFeedback(error), 'error');
    } finally { setExporting({ identity, active: false }); }
  };
  const savePrepared = async (file: OpenOrdersQxtendResponseDto['files'][number]) => {
    if (exporting || !planMode || prepared?.signature !== exportSignature || report?.isStale || sourceChanged || pending || failure ||
        invalidDateEntries || conflicts.length || incomplete.length || restoreCandidate) return;
    setExporting({ identity, active: true });
    try {
      const result = await saveQxtendCsv(file.contentBase64, file.fileName);
      setPrepared(previous => previous?.identity === identity ? { ...previous, statuses: { ...previous.statuses,
        [file.kind]: result.kind === 'saved' ? `Saved ${result.fileName}` : result.kind === 'downloaded' ? `Download initiated for ${result.fileName}; check your browser downloads.` : 'Save cancelled; file remains ready.' } } : previous);
    } catch {
      setPrepared(previous => previous?.identity === identity ? { ...previous, statuses: { ...previous.statuses,
        [file.kind]: 'Save failed; retry this file. Other saved files and proposals remain.' } } : previous);
    } finally { setExporting({ identity, active: false }); }
  };
  const changeLayout = (next: Layout | LayoutForPlan) => {
    try {
      if (planMode) { setPlanLayout({ identity: assignmentId, value: next as LayoutForPlan }); savePlanLayout(assignmentId, next as LayoutForPlan); }
      else { setLayout({ identity: assignmentId, value: next as Layout }); saveLayout(assignmentId, next as Layout); }
    } catch { showFeedback('Column layout could not be saved. Changes remain available until this session ends.', 'error'); }
  };
  const move = (source: PlanColumnId, target: PlanColumnId) => {
    const order: PlanColumnId[] = [...activeLayout.order];
    const from = order.indexOf(source); const to = order.indexOf(target);
    if (from < 0 || to < 0 || from === to) return;
    order.splice(from, 1);
    order.splice(to, 0, source);
    if (planMode) changeLayout({ ...planLayout, order });
    else changeLayout({ ...layout, order: order as ColumnId[] });
  };
  const applyFilters = (next: Filters) => { setFilters({ identity: assignmentId, value: next }); setPage(0); };
  const visibleMove = (source: PlanColumnId, offset: -1 | 1) => {
    const index = visible.indexOf(source);
    if (index < 0 || index + offset < 0 || index + offset >= visible.length) return;
    move(source, visible[index + offset]);
  };

  if (!snapshotId) return <p className="open-orders__state">Load the MPS dashboard before viewing Customer Open Orders.</p>;
  return <section className="open-orders" aria-label={`Customer Open Orders ${planMode ? 'Plan' : 'Report'} Mode`}>
    <header className="open-orders__toolbar"><h3>Customer Open Orders <span>{planMode ? 'Plan Mode' : 'Report Mode'}</span></h3>
      <label><input type="checkbox" checked={planMode} onChange={e => setPlanMode(e.target.checked)} /> Plan Mode</label>
      <label><input type="checkbox" checked={saveDraft} disabled={!!restoreCandidate || savedState === 'unavailable' || savedState === 'presence-error' || saveOffPending} onChange={e => {
        if (e.target.checked) { setSaveDraft(true); setDraftError(null); persist(proposals, true); }
        else if (window.confirm('Turn Save Draft off and delete the saved copy? In-memory changes will remain.')) {
          setSaveOffPending(true);
          void (async () => { await saveGate.current; await new ApiClient(await resolveBackendBaseUrl()).deleteOpenOrdersDraft(assignmentId); })().then(() => {
            saveGeneration.current++; setSaveDraft(false); setSavedState('none'); setDraftError(null);
          }).catch(() => setDraftError('Saved draft could not be deleted; Save Draft remains on.')).finally(() => setSaveOffPending(false));
        }
      }} /> Save Draft</label>
      {checkingDraftFor === identity && savedState === 'checking' && <span className="open-orders__draft-progress" role="status">Checking saved draft…</span>}
      <button type="button" disabled={planMode || pending} onClick={() => { if (!planMode) void refresh(); }}>{pending ? 'Refreshing…' : 'Refresh report'}</button>
      <button type="button" disabled={planMode || !report || rows.length === 0 || reportVisible.length === 0 || pending || exporting || !!failure} onClick={async () => {
        if (planMode) return;
        if (!report) return;
        if (feedbackTimer.current) clearTimeout(feedbackTimer.current);
        setFeedback(null); setExporting({ identity, active: true });
        try {
          const workbook = await new ApiClient(await resolveBackendBaseUrl()).exportOpenOrdersReport(assignmentId, {
            mpsSnapshotId: snapshotId, openOrdersSnapshotId: report.openOrdersSnapshotId,
            lineKeys: rows.map(r => r.key), columns: reportVisible,
          });
          const result = await saveLongTermShortagesWorkbook(workbook.blob,
            workbook.fileName ?? openOrdersFileName(workspaceName, site || report.site, report.acquiredAtUtc), 'Save Customer Open Orders Workbook');
          showFeedback(result.kind === 'cancelled' ? 'Save cancelled; report remains available.' : `Saved ${result.fileName}`, result.kind === 'cancelled' ? 'cancelled' : 'saved');
        } catch { showFeedback('Workbook export or save failed. Report remains available; please retry.', 'error'); }
        finally { setExporting({ identity, active: false }); }
      }}>{exporting ? 'Preparing workbook…' : 'Export filtered rows to XLSX'}</button>
      {feedback?.identity === identity && <span role="status" className={`open-orders__feedback open-orders__feedback--${feedback.kind}`}>{feedback.text}</span>}
    </header>
    {draftError && <p role="alert" className="open-orders__warning">{draftError}</p>}
    {sourceChanged && <p role="alert" className="open-orders__warning">MPS snapshot changed. {staged.length} planning changes remain in memory; reload the report and review conflicts before relying on them. Not export-ready.</p>}
    {savedState === 'restored' && restoreCandidate && <div role="status" className="open-orders__planning-summary">Saved draft checked against a fresh report ({restoreCandidate.length} changed rows, {Object.values(restoreIssues).filter(v => v.length).length} conflicts). <button type="button" onClick={acceptRestoredDraft}>Restore saved draft and continue saving</button></div>}
    {savedState === 'restored' && !restoreCandidate && <p role="status">Saved draft restored after fresh report validation. Saving remains on.</p>}
    {savedState === 'unavailable' && <div role="alert" className="open-orders__warning">Saved draft exists but has not been restored. {staged.length} proposal(s) retained. {staged.map(p => `${p.key.salesOrder}/${p.key.line} original ${JSON.stringify(p.original)} proposed ${JSON.stringify(p.proposed)}`).join(' · ')} <button type="button" onClick={() => { setSavedState('checking'); setDraftError(null); setRestoreAttempt(n => n + 1); }}>Retry fresh draft validation</button></div>}
    {savedState === 'presence-error' && <button type="button" onClick={() => { setSavedState('idle'); setDraftError(null); setRestoreAttempt(n => n + 1); }}>Retry draft check</button>}
    {pending && <p role="status">{report ? 'Refreshing report; displaying previous acquisition until complete.' : 'Loading Customer Open Orders…'}</p>}
    {!pending && !report && !failure && <p role="status">Loading Customer Open Orders…</p>}
    {failure && <p role="alert" className="open-orders__warning">{failure === 'snapshot-changed' ? 'MPS snapshot changed. Reload the workspace MPS dashboard, then reopen this report.' : failure === 'unavailable' ? 'Open Orders source unavailable. Retry refresh.' : 'Open Orders could not be loaded. Retry refresh.'}{report && ' Previous report is retained but not current; do not use for operational/QXtend validation.'}</p>}
    {report?.isStale && <p role="alert" className="open-orders__warning">STALE report acquired {formatReportTimestamp(report.acquiredAtUtc)}. {report.warning} Not ready for operational/QXtend validation.</p>}
    {report && <>
      {planMode && <div className="open-orders__planning-summary" role="status">
        {staged.length} changed across workspace · {incomplete.length} missing Reason Code · {conflicts.length} conflicts · {hiddenChanges} outside current filter/page view. {report.isStale || failure || pending || conflicts.length || incomplete.length || invalidDateEntries || savedState === 'unavailable' || sourceChanged ? 'Not export-ready.' : 'Export All validates changed lines against QAD before preparing CSVs.'}
        <button type="button" disabled={!staged.length || !!report.isStale || !!failure || pending || exporting || !!conflicts.length || !!incomplete.length || !!invalidDateEntries || savedState === 'unavailable' || !!restoreCandidate || sourceChanged}
          onClick={() => void exportAll()}>{exporting ? 'Working…' : 'Export All QXtend CSVs'}</button>
        {prepared?.identity === identity && prepared.signature === exportSignature && !report.isStale && !failure && !pending && !sourceChanged && !invalidDateEntries && !conflicts.length && !incomplete.length && !restoreCandidate && <div role="group" aria-label="Prepared QXtend files">Validated CSVs: {prepared.files.map(file =>
          <span key={file.kind}><button type="button" disabled={exporting} onClick={() => void savePrepared(file)}>Save As {file.fileName}</button>
            <span role="status">{prepared.statuses[file.kind] ?? 'Not saved'}</span></span>)} <span>External QXtend processing is required; export does not submit changes.</span></div>}
        <label><input type="checkbox" checked={onlyChanged} onChange={e => { setOnlyChanged(e.target.checked); setPage(0); }} /> Show changed rows</label>
        <button type="button" disabled={!staged.length} onClick={() => setClearConfirmation(true)}>Clear All</button>
        {clearConfirmation && <span role="group" aria-label="Confirm Clear All">Clear all {staged.length} changes? <button type="button" onClick={() => { setRestoreIssues({}); setDateInputs({}); setDateTouched({}); invalidDateInputs.current.clear(); persist({}, saveDraft, report, false, false); setClearConfirmation(false); }}>Confirm Clear All</button><button type="button" onClick={() => setClearConfirmation(false)}>Cancel</button></span>}
      </div>}
      {planMode && hiddenChanges > 0 && <div className="open-orders__hidden-changes">Changes outside current results: {staged.filter(p => !displayedRows.slice(currentPage * pageSize, (currentPage + 1) * pageSize).some(r => keyOf(r.key) === keyOf(p.key))).map(p => <div key={keyOf(p.key)}>
        {p.key.salesOrder} / {p.key.line} ({p.itemNumber}) · {rowByKey.has(keyOf(p.key)) ? 'filtered or on another page' : 'missing from report — conflict'} · {[...issues(p, rowByKey.get(keyOf(p.key)), report.isStale || !!failure || pending), ...(restoreIssues[keyOf(p.key)] ?? [])].join(' ')} · Original {JSON.stringify(p.original)} → Proposed {JSON.stringify(p.proposed)} · {p.reasonCode ?? 'Reason Code missing'}
        {rowByKey.has(keyOf(p.key)) && <button type="button" onClick={() => { setOnlyChanged(false); setFilters({ identity: assignmentId, value: emptyFilters }); const index = reportRows(report.lines, emptyFilters).findIndex(r => keyOf(r.key) === keyOf(p.key)); if (index >= 0) setPage(Math.floor(index / pageSize)); }}>Find row</button>}
        <button type="button" onClick={() => undo(keyOf(p.key))}>Undo {p.key.salesOrder}/{p.key.line}</button>
      </div>)}</div>}
      <p className="open-orders__meta">Acquired {formatReportTimestamp(report.acquiredAtUtc)} · {report.lines.length} scoped lines · {rows.length} matching filters. {report.isStale || failure || pending ? 'Not current; do not use for operational/QXtend validation.' : 'Report only; not operational/QXtend validation.'}</p>
      <FilterBuilder key={assignmentId} filters={filters} onChange={applyFilters} />
      <details className="open-orders__columns"><summary>{planMode ? 'Plan columns' : 'Columns'} ({visible.length} visible)</summary>
        <div className="open-orders__columns-actions"><span>Drag table headers to reorder; hidden columns keep their relative order.</span>
          <button type="button" onClick={() => changeLayout(planMode ? planDefaultLayout() : defaultLayout())}>Reset columns to default</button></div>
        <div className="open-orders__column-choices">{activeLayout.order.map(id => { const label = id === 'orderQty' ? 'Order Qty' : id === 'price' ? 'Price' : COLUMNS.find(c => c.id === id)!.label; return <label key={id}>
          <input type="checkbox" checked={activeLayout.visible.includes(id)} disabled={planMode && PLAN_REQUIRED.includes(id) || activeLayout.visible.length === 1 && activeLayout.visible.includes(id)} onChange={e => {
            const selected = e.target.checked ? [...activeLayout.visible, id] : activeLayout.visible.filter(value => value !== id);
            if (planMode) changeLayout({ ...planLayout, visible: selected as PlanColumnId[] });
            else changeLayout({ ...layout, visible: selected as ColumnId[] });
          }} />{label}
        </label>; })}</div>
      </details>
      {displayedRows.length === 0 ? <p className="open-orders__state">{report.lines.length === 0 ? 'No open orders in this workspace scope.' : 'No open orders match the current filters.'}</p> : <>
        <div className="open-orders__page"><span>Rows {currentPage * pageSize + 1}–{Math.min((currentPage + 1) * pageSize, displayedRows.length)} of {displayedRows.length}</span>
          <button type="button" disabled={currentPage === 0} onClick={() => setPage(currentPage - 1)}>Previous page</button>
            <span>Page {currentPage + 1} of {pageCount}</span>
          <button type="button" disabled={currentPage + 1 >= pageCount} onClick={() => setPage(currentPage + 1)}>Next page</button></div>
        <div className="open-orders__grid" role="region" tabIndex={0} aria-label="Scrollable Customer Open Orders report">
          <table><thead><tr>{visible.map(id => <th scope="col" key={id} draggable className={NUMERIC_COLUMNS.has(id as ColumnId) ? 'open-orders__num' : undefined}
            onDragStart={event => { event.dataTransfer.setData('text/plain', id); event.dataTransfer.effectAllowed = 'move'; }}
            onDragOver={event => event.preventDefault()}
             onDrop={event => { event.preventDefault(); const source = event.dataTransfer.getData('text/plain'); if (visible.includes(source as PlanColumnId)) move(source as PlanColumnId, id); }}>
            <span>{id === 'orderQty' ? 'Order Qty' : id === 'price' ? 'Price' : COLUMNS.find(c => c.id === id)!.label}</span><span className="open-orders__header-moves">
              <button type="button" disabled={visible[0] === id} aria-label={`Move ${id === 'orderQty' ? 'Order Qty' : id === 'price' ? 'Price' : COLUMNS.find(c => c.id === id)!.label} Left`} onClick={() => visibleMove(id, -1)}>‹</button>
              <button type="button" disabled={visible[visible.length - 1] === id} aria-label={`Move ${id === 'orderQty' ? 'Order Qty' : id === 'price' ? 'Price' : COLUMNS.find(c => c.id === id)!.label} Right`} onClick={() => visibleMove(id, 1)}>›</button>
            </span></th>)}{planMode && <><th scope="col">Reason Code</th><th scope="col" className="open-orders__actions-heading">Actions</th></>}</tr></thead>
            <tbody>{displayedRows.slice(currentPage * pageSize, (currentPage + 1) * pageSize).map(r => { const key = keyOf(r.key); const p = proposals[key]; const original = p?.original ?? valuesOf(r); const proposed = p?.proposed ?? original;
              const rowIssues = p ? [...new Set([...issues(p, r, report.isStale || !!failure || pending), ...(restoreIssues[key] ?? [])])] : [];
              const editor = (field: Editable, label: string) => {
                const value = proposed[field]; const isDate = field.endsWith('Date');
                const inputKey = `${key}:${field}`;
                const inputText = dateInputs[inputKey] ?? (isDate ? formatReportDate(value as string | null)
                  : focusedNumber === inputKey ? String(value) : planningNumberDisplay(field as 'orderQty' | 'price', String(value)));
                const invalid = isDate && parsePlanningDate(inputText) === undefined;
                const dateError = invalid && (dateTouched[inputKey] || /^\s*\d{1,2}\/\d{1,2}\/\d{4}\s*$/.test(inputText));
                const wasChanged = p && (field === 'price' || field === 'orderQty' ? !equalDecimal(original[field], value as string) : original[field] !== value);
                return <div className={wasChanged ? 'open-orders__changed' : undefined}>
                  <input aria-label={`${label} proposal ${r.key.salesOrder}/${r.key.line}`} type="text" inputMode={isDate ? 'numeric' : 'decimal'} placeholder={isDate ? 'M/d/yyyy' : undefined}
                    data-plan-editor="" aria-invalid={dateError || undefined} value={inputText}
                    onFocus={() => { if (!isDate) setFocusedNumber(inputKey); }}
                    onChange={e => {
                      if (!isDate) { update(r, field, e.target.value); return; }
                      const text = e.target.value;
                      setDateInputs(previous => ({ ...previous, [inputKey]: text }));
                      const parsed = parsePlanningDate(text);
                      if (parsed === undefined) invalidDateInputs.current.add(`${assignmentId}:${inputKey}`);
                      else invalidDateInputs.current.delete(`${assignmentId}:${inputKey}`);
                      if (parsed === undefined && saveDraft) setDraftError('Finish or correct the date entry before saving this draft.');
                      if (parsed !== undefined) update(r, field, parsed);
                    }}
                    onBlur={() => {
                      if (!isDate) { setFocusedNumber(null); return; }
                      const parsed = parsePlanningDate(inputText);
                      if (parsed === undefined) setDateTouched(previous => ({ ...previous, [inputKey]: true }));
                      else { invalidDateInputs.current.delete(`${assignmentId}:${inputKey}`);
                        setDateInputs(previous => { const next = { ...previous }; delete next[inputKey]; return next; });
                        setDateTouched(previous => { const next = { ...previous }; delete next[inputKey]; return next; }); }
                    }}
                    onKeyDown={e => { if (e.key !== 'Enter') return; e.preventDefault();
                      if (isDate && invalid) { setDateTouched(previous => ({ ...previous, [inputKey]: true })); return; }
                      const editors = [...e.currentTarget.closest('table')!.querySelectorAll<HTMLInputElement | HTMLSelectElement>('[data-plan-editor]')];
                      editors[editors.indexOf(e.currentTarget) + 1]?.focus();
                    }} />
                  {dateError && <small className="open-orders__date-error" role="alert">Enter a valid M/d/yyyy date or leave blank.</small>}
                  {wasChanged && <small>Original: {isDate ? formatReportDate(original[field] as string | null) || '(empty)' : planningNumberDisplay(field as 'orderQty' | 'price', String(original[field]))}</small>}
                </div>;
              };
              return <tr className="open-orders__row" key={key}>
              {visible.map(id => { const value = id === 'orderQty' || id === 'price' ? null : COLUMNS.find(c => c.id === id)!.value(r);
                const dateField = id === 'dueDate' || id === 'performDate' || id === 'requiredDate' || id === 'dockDate' ? id : null;
                return <td className={NUMERIC_COLUMNS.has(id as ColumnId) ? 'open-orders__num' : id === 'itemNumber' || id === 'order' ? 'open-orders__identity' : undefined} key={id}>
                {planMode && dateField ? editor(dateField, COLUMNS.find(c => c.id === id)!.label)
                  : planMode && (id === 'orderQty' || id === 'price') ? editor(id, id === 'price' ? 'Price' : 'Order Qty')
                  : planMode && id === 'open' ? <><strong>{subtract(proposed.orderQty, r.shippedQtyText ?? String(r.shippedQty)) ?? '—'}</strong><small>Original: {r.open}</small></>
                  : planMode && id === 'extPrice' ? <><strong>{multiply(proposed.price, subtract(proposed.orderQty, r.shippedQtyText ?? String(r.shippedQty)) ?? '') ?? '—'}</strong><small>Original: {r.extPrice}</small></>
                  : value === null ? '' : DATE_COLUMNS.has(id as ColumnId) ? formatReportDate(String(value)) : typeof value === 'boolean' ? value ? 'Yes' : 'No' : String(value)}
              </td>; })}{planMode && <><td><select data-plan-editor="" aria-label={`Reason Code ${r.key.salesOrder}/${r.key.line}`} value={p?.reasonCode ?? ''} onChange={e => update(r, 'reasonCode', e.target.value || null)}><option value="">Select reason</option>{REASONS.map(reason => <option key={reason} value={reason}>{reason}</option>)}</select></td>
                <td className="open-orders__actions">{p && <><span className={rowIssues.length ? 'open-orders__warning' : 'open-orders__changed'} title={rowIssues.join(' ')}>{rowIssues.length ? 'Review' : 'Changed'}</span><button type="button" onClick={() => undo(key)}>Undo {r.key.salesOrder}/{r.key.line}</button>{rowIssues.length > 0 && <details><summary>Issues</summary><small>{rowIssues.join(' ')}</small></details>}</>}</td></>}</tr>; })}</tbody></table>
        </div>
      </>}
    </>}
  </section>;
}

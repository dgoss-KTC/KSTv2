import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { ApiClient, ApiError, type OpenOrdersResponseDto, type OpenOrdersQxtendResponseDto } from '../api/client';
import { resolveBackendBaseUrl } from '../api/tauri-bridge';
import { COLUMNS, DATE_COLUMNS, NUMERIC_COLUMNS, formatReportDate, formatReportTimestamp, type ColumnId } from '../openOrders/report';
import { OrderFilterEditor, OrderFiltersMenu } from '../openOrders/OrderHeaderFilters';
import { filteredOrderRows, filterSummary } from '../openOrders/headerFilters';
import { OPEN_ORDERS_PAGE_SIZES, loadOpenOrdersPageSize, saveOpenOrdersPageSize, type OpenOrdersPageSize } from '../openOrders/pageSize';
import type { ColumnFilter } from './shared/ColumnFilterEditor';
import { ConfirmDialog } from './ConfirmDialog';
import { changed, equalDecimal, issues, keyOf, multiply, parsePlanningDate, planningNumberDisplay, subtract, valuesOf, REASONS, type Editable, type Proposal } from '../openOrders/planning';
import { type PlanColumnId } from '../openOrders/planLayout';
import { useOrderViews } from '../openOrders/useOrderViews';
import { columnLabel, moveColumn } from '../openOrders/views';
import { OrderColumns } from '../openOrders/OrderColumns';
import { useColumnDrag } from '../openOrders/useColumnDrag';
import { ColumnDragPreview } from '../openOrders/ColumnDragPreview';
import { AnalyticalExcelExport } from './AnalyticalExcelExport';
import { analyticalRequest } from '../api/analyticalExportApi';
import { useLocalBusinessDate } from '../hooks/useLocalBusinessDate';
import { localIsoDate } from '../shared/localDate';
import { saveQxtendCsv } from '../openOrders/saveQxtendCsv';
import { qxtendExportFeedback } from '../openOrders/exportFeedback';
import './OpenOrdersPanel.css';

type Failure = 'snapshot-changed' | 'unavailable' | 'error';

export function OpenOrdersPanel({ assignmentId, snapshotId, site = '', onDirtyChange }: { assignmentId: string; snapshotId: string | null; workspaceName?: string | null; site?: string; onDirtyChange?: (dirty: boolean) => void }) {
  const reportViews = useOrderViews(assignmentId, 'report');
  const planViews = useOrderViews(assignmentId, 'plan');
  const [searchState, setSearch] = useState({ identity: assignmentId, value: '' });
  const search = searchState.identity === assignmentId ? searchState.value : '';
  const [announcement, announce] = useState('');
  const [showHidden, setShowHidden] = useState(false);
  const findTarget = useRef<string | null>(null);
  const grid = useRef<HTMLDivElement>(null);
  useLayoutEffect(() => {
    if (!findTarget.current) return;
    const row = [...(grid.current?.querySelectorAll<HTMLElement>('[data-order-row]') ?? [])].find(r => r.dataset.orderRow === findTarget.current);
    if (row) { row.focus({ preventScroll: true }); row.scrollIntoView?.({ block: 'nearest' }); findTarget.current = null; }
  });
  const businessDate = useLocalBusinessDate();
  const [filterState, setFilters] = useState<{ identity: string; value: ColumnFilter[] }>(() => ({ identity: assignmentId, value: [] }));
  const [filterPopup, setFilterPopup] = useState<{ identity: string; field: string } | null>(null);
  const filterAnchor = useRef<HTMLElement | null>(null);
  const [loaded, setLoaded] = useState<{ identity: string; report: OpenOrdersResponseDto } | null>(null);
  const [pendingIdentity, setPending] = useState<string | null>(null);
  const [failureState, setFailure] = useState<{ identity: string; kind: Failure } | null>(null);
  const [feedback, setFeedback] = useState<{ identity: string; text: string; kind: 'saved' | 'cancelled' | 'error' } | null>(null);
  const [exportingState, setExporting] = useState<{ identity: string; active: boolean } | null>(null);
  const [prepared, setPrepared] = useState<{ identity: string; signature: string; files: OpenOrdersQxtendResponseDto['files']; statuses: Record<string, string> } | null>(null);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(loadOpenOrdersPageSize);
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
  const confirmationReturn = useRef<HTMLElement | null>(null);
  const [saveVersion, setSaveVersion] = useState(0);
  const [saveOffPending, setSaveOffPending] = useState(false);
  /** Assignment whose 'Save Draft off' confirmation is open (the desktop confirm() is asynchronous, so the decision is held in-app). */
  const [saveOffConfirm, setSaveOffConfirm] = useState<string | null>(null);
  useEffect(() => {
    if (!clearConfirmation && saveOffConfirm !== assignmentId) return;
    const opener = confirmationReturn.current;
    const key = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return;
      event.preventDefault(); event.stopPropagation();
      if (!saveOffPending) { setClearConfirmation(false); setSaveOffConfirm(null); }
    };
    document.addEventListener('keydown', key, true);
    return () => { document.removeEventListener('keydown', key, true); if (opener?.isConnected) opener.focus(); };
  }, [clearConfirmation, saveOffConfirm, assignmentId, saveOffPending]);
  const [dateInputs, setDateInputs] = useState<Record<string, string>>({});
  // UI text only: focus must not round the source/proposal or prevent typing a decimal point.
  const [focusedNumber, setFocusedNumber] = useState<{ key: string; text: string; value: string } | null>(null);
  const [dateTouched, setDateTouched] = useState<Record<string, boolean>>({});
  const invalidDateInputs = useRef(new Set<string>());
  const saveGate = useRef(Promise.resolve());
  const saveGeneration = useRef(0);
  const restoredFor = useRef<string | null>(null);
  const proposalAssignmentRef = useRef(assignmentId);
  useEffect(() => { proposalAssignmentRef.current = assignmentId; }, [assignmentId]);
  if (proposalAssignment !== assignmentId) {
    setProposalAssignment(assignmentId); setProposals({}); setSaveDraft(false); setSavedState('idle'); setSaveOffConfirm(null);
    setPlanMode(false); setRestoreCandidate(null); setRestoreIssues({}); setLastDraftMps(snapshotId);
    setDateInputs({}); setDateTouched({}); setFocusedNumber(null);
  }
  const requestId = useRef(0);
  const feedbackTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const identity = `${assignmentId}:${snapshotId ?? ''}`;
  useEffect(() => { invalidDateInputs.current.clear(); }, [assignmentId]);
  const exporting = exportingState?.identity === identity && exportingState.active;
  const layout = reportViews.layout;
  const filters = useMemo(() => filterState.identity === assignmentId ? filterState.value : [], [filterState, assignmentId]);
  const pending = pendingIdentity === identity;
  const failure = failureState?.identity === identity ? failureState.kind : null;
  const report = loaded?.identity === identity ? loaded.report : null;
  const sourceChanged = ((loaded !== null && loaded.identity !== identity) || lastDraftMps !== snapshotId) && proposalAssignment === assignmentId && Object.values(proposals).some(changed);
  const rows = useMemo(() => filteredOrderRows(report?.lines ?? [], filters, search), [report, filters, search]);
  const reportVisible = layout.order.filter(id => layout.visible.includes(id));
  const activeViews = planMode ? planViews : reportViews;
  const activeLayout = activeViews.layout;
  const visible = activeLayout.order.filter(id => activeLayout.visible.includes(id));
  const rowByKey = new Map((report?.lines ?? []).map(line => [keyOf(line.key), line]));
  const scopedProposals = proposalAssignment === assignmentId ? proposals : {};
  const staged = Object.values(scopedProposals).filter(changed);
  const exportSignature = JSON.stringify([report?.openOrdersSnapshotId, staged]);
  const operationGate = useRef(false);
  const operationContext = JSON.stringify([identity, planMode, exportSignature]);
  const currentOperationContext = useRef(operationContext);
  const operationEpoch = useRef(0);
  useLayoutEffect(() => {
    const visitEpoch = ++operationEpoch.current;
    currentOperationContext.current = operationContext;
    return () => { operationEpoch.current = visitEpoch + 1; currentOperationContext.current = 'departed'; };
  }, [operationContext]);
  const conflicts = staged.filter(p => sourceChanged || [...issues(p, rowByKey.get(keyOf(p.key)), !!report?.isStale || !!failure || pending || !report), ...(restoreIssues[keyOf(p.key)] ?? [])].some(i => i !== 'Reason Code required.'));
  const incomplete = staged.filter(p => !p.reasonCode);
  const draftBlocked = savedState === 'checking' || savedState === 'unavailable' || savedState === 'presence-error' || checkingDraftFor === identity || !!restoreCandidate;
  const invalidDateEntries = Object.values(dateInputs).filter(text => parsePlanningDate(text) === undefined).length;
  const displayedRows = planMode && onlyChanged ? rows.filter(r => scopedProposals[keyOf(r.key)] && changed(scopedProposals[keyOf(r.key)])) : rows;
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
    if (!report || !snapshotId || !staged.length || exporting || operationGate.current || !planMode || report.isStale || pending || failure || sourceChanged || conflicts.length || incomplete.length || invalidDateEntries || draftBlocked) return;
    operationGate.current = true;
    const generation = requestId.current;
    const contextEpoch = operationEpoch.current;
    setPrepared(null); setExporting({ identity, active: true }); setFeedback(null);
    try {
      const result = await new ApiClient(await resolveBackendBaseUrl()).exportOpenOrdersQxtend(assignmentId, {
        mpsSnapshotId: snapshotId, openOrdersSnapshotId: report.openOrdersSnapshotId, proposals: staged,
      });
      if (result.files.length === 0) throw new Error('No applicable CSV files');
      // A report/mode/workspace change while validation is pending cannot authorize an old UI context.
      if (requestId.current !== generation || operationEpoch.current !== contextEpoch || currentOperationContext.current !== operationContext) return;
      setPrepared({ identity, signature: exportSignature, files: result.files, statuses: {} });
      showFeedback(`${result.files.length} CSV file(s) prepared after fresh validation. Choose Save As for each file; QXtend has not accepted them.`, 'saved');
    } catch (error) {
      if (operationEpoch.current === contextEpoch && currentOperationContext.current === operationContext) showFeedback(qxtendExportFeedback(error), 'error');
    } finally { operationGate.current = false; setExporting({ identity, active: false }); }
  };
  const savePrepared = async (file: OpenOrdersQxtendResponseDto['files'][number]) => {
    if (exporting || operationGate.current || !planMode || prepared?.signature !== exportSignature || report?.isStale || sourceChanged || pending || failure ||
        invalidDateEntries || conflicts.length || incomplete.length || draftBlocked) return;
    operationGate.current = true; setExporting({ identity, active: true });
    try {
      const result = await saveQxtendCsv(file.contentBase64, file.fileName);
      setPrepared(previous => previous?.identity === identity && previous.signature === exportSignature ? { ...previous, statuses: { ...previous.statuses,
        [file.kind]: result.kind === 'saved' ? `Saved ${result.fileName}` : result.kind === 'downloaded' ? `Download initiated for ${result.fileName}; check your browser downloads.` : 'Save cancelled; file remains ready.' } } : previous);
    } catch {
      setPrepared(previous => previous?.identity === identity && previous.signature === exportSignature ? { ...previous, statuses: { ...previous.statuses,
        [file.kind]: 'Save failed; retry this file. Other saved files and proposals remain.' } } : previous);
    } finally { operationGate.current = false; setExporting({ identity, active: false }); }
  };
  const move = (source: PlanColumnId, target: PlanColumnId) => {
    const next = moveColumn(activeLayout, source, target);
    activeViews.change(next);
    announce(`${columnLabel(source)} moved to position ${next.order.filter(id => next.visible.includes(id)).indexOf(source) + 1}. View modified.`);
  };
  const applyFilters = (next: ColumnFilter[]) => { setFilters({ identity: assignmentId, value: next }); setPage(0); };
  const visibleMove = (source: PlanColumnId, offset: -1 | 1) => {
    const index = visible.indexOf(source);
    if (index < 0 || index + offset < 0 || index + offset >= visible.length) return;
    move(source, visible[index + offset]);
  };
  const columnDrag = useColumnDrag(visible, JSON.stringify([identity, planMode, visible]), move, 'x');
  const dropColumn = columnDrag.feedback?.target;
  const dragColumn = columnDrag.feedback?.source;
  const clearFilters = () => { applyFilters([]); setSearch({ identity: assignmentId, value: '' }); };
  const resizePage = (size: OpenOrdersPageSize) => {
    setPage(Math.floor(currentPage * pageSize / size));
    setPageSize(size);
    saveOpenOrdersPageSize(size);
  };
  const filterContext = `${identity}:${planMode}:${report?.openOrdersSnapshotId ?? ''}`;
  const editFilter = (field: string, anchor: HTMLElement) => {
    columnDrag.cancel(); filterAnchor.current = anchor; setFilterPopup({ identity: filterContext, field });
  };
  const exportReason = !report ? 'Load a current report before exporting.' : pending ? 'Wait for report refresh to finish.' : report.isStale || failure ? 'Refresh the report before exporting.'
    : !rows.length ? 'No matching rows to export.' : !reportVisible.length ? 'Select at least one report column.' : undefined;
  const reportExportRequest = () => {
    if (!report || !snapshotId || exportReason) throw new Error(exportReason ?? 'Load a current report.');
    return { ...analyticalRequest('OpenOrders', assignmentId, snapshotId, site || report.site, localIsoDate(new Date())),
      openOrders: { mpsSnapshotId: snapshotId, openOrdersSnapshotId: report.openOrdersSnapshotId,
        lineKeys: rows.map(r => r.key), columns: reportVisible } };
  };

  if (!snapshotId) return <p className="open-orders__state">Load the MPS dashboard before viewing Customer Open Orders.</p>;
  return <section className="open-orders" aria-label={`Customer Open Orders ${planMode ? 'Plan' : 'Report'} Mode`}>
    <ColumnDragPreview feedback={columnDrag.feedback} />
    <header className="open-orders__toolbar"><h3>Customer Open Orders</h3>
      <div className="segmented" role="group" aria-label="Open Orders mode"><button type="button" aria-pressed={!planMode} className={!planMode ? 'active' : ''} onClick={() => setPlanMode(false)}>Report</button><button type="button" aria-pressed={planMode} className={planMode ? 'active' : ''} onClick={() => setPlanMode(true)}>Plan</button></div>
      {planMode && <label><input type="checkbox" checked={saveDraft} disabled={!!restoreCandidate || savedState === 'unavailable' || savedState === 'presence-error' || saveOffPending || saveOffConfirm !== null} onChange={e => {
        if (e.target.checked) { setSaveDraft(true); setDraftError(null); persist(proposals, true); }
        else if (!saveOffPending) { confirmationReturn.current = e.currentTarget; setSaveOffConfirm(assignmentId); } // the checkbox stays on until the user confirms
      }} /> Save Draft</label>}
      {planMode && saveDraft && <span className="open-orders__muted" role="status">{saveVersion > 0 ? 'Saving draft…' : draftError ? 'Draft not saved' : 'Draft saved locally'}</span>}
      {checkingDraftFor === identity && savedState === 'checking' && <span className="open-orders__draft-progress" role="status">Checking saved draft…</span>}
      <span className="open-orders__acquired">{report && <>Acquired {formatReportTimestamp(report.acquiredAtUtc)} · {report.lines.length} scoped lines</>}</span>
      {planMode && <span className="open-orders__muted">Refresh is off while you plan</span>}
      <button type="button" className="open-orders__primary" disabled={planMode || pending} onClick={() => { if (!planMode) void refresh(); }}>{pending ? 'Refreshing…' : 'Refresh report'}</button>
      {!planMode && <AnalyticalExcelExport label="Customer Open Orders" scopeLabel={`Exports all ${rows.length} matching rows across every page`}
        request={reportExportRequest} disabledReason={exportReason}
        contextKey={JSON.stringify([identity, report?.openOrdersSnapshotId, report?.site, businessDate, filters, search, reportVisible])} />}
      {feedback?.identity === identity && <span role="status" className={`open-orders__feedback open-orders__feedback--${feedback.kind}`}>{feedback.text}</span>}
    </header>
    {draftError && <p role="alert" className="open-orders__warning">{draftError}</p>}
    {sourceChanged && <p role="alert" className="open-orders__warning">MPS snapshot changed. {staged.length} planning changes remain in memory; reload the report and review conflicts before relying on them. Not export-ready.</p>}
    {savedState === 'restored' && restoreCandidate && <div role="status" className="open-orders__planning-summary">Saved draft checked against a fresh report ({restoreCandidate.length} changed rows, {Object.values(restoreIssues).filter(v => v.length).length} conflicts). <button type="button" disabled={saveOffPending} onClick={acceptRestoredDraft}>Restore saved draft and continue saving</button><button type="button" disabled={saveOffPending} onClick={e => { confirmationReturn.current = e.currentTarget; setSaveOffConfirm(assignmentId); }}>Discard saved draft</button></div>}
    {savedState === 'restored' && !restoreCandidate && <p role="status">Saved draft restored after fresh report validation. Saving remains on.</p>}
    {savedState === 'unavailable' && <div role="alert" className="open-orders__warning">Saved draft exists but has not been restored. {staged.length} proposal(s) retained. {staged.map(p => `${p.key.salesOrder}/${p.key.line} original ${JSON.stringify(p.original)} proposed ${JSON.stringify(p.proposed)}`).join(' · ')} <button type="button" onClick={() => { setSavedState('checking'); setDraftError(null); setRestoreAttempt(n => n + 1); }}>Retry fresh draft validation</button></div>}
    {savedState === 'presence-error' && <button type="button" onClick={() => { setSavedState('idle'); setDraftError(null); setRestoreAttempt(n => n + 1); }}>Retry draft check</button>}
    {failure && <p role="alert" className="open-orders__warning">{failure === 'snapshot-changed' ? 'MPS snapshot changed. Reload the workspace MPS dashboard, then reopen this report.' : failure === 'unavailable' ? 'Open Orders source unavailable. Retry refresh.' : 'Open Orders could not be loaded. Retry refresh.'}{report && ' Previous report is retained but not current; do not use for operational/QXtend validation.'}</p>}
    {report?.isStale && <p role="alert" className="open-orders__warning">STALE report acquired {formatReportTimestamp(report.acquiredAtUtc)}. {report.warning} Not ready for operational/QXtend validation.</p>}
    {report && <>
      <div className="open-orders__query">
        <input type="search" aria-label="Search open orders" placeholder="Search SO, PO, item, customer, salesperson…" value={search} onChange={e => { setSearch({ identity: assignmentId, value: e.target.value }); setPage(0); }} />
        <OrderFiltersMenu key={`filters:${assignmentId}:${planMode}`} filters={filters} visible={visible} onEdit={editFilter} onClear={() => applyFilters([])} />
        {(search || filters.length > 0) && <button type="button" onClick={clearFilters}>Clear search and filters</button>}
        <OrderColumns key={`${assignmentId}:${planMode}`} views={activeViews} mode={planMode ? 'plan' : 'report'} move={move} announce={announce} />
      </div>
      {planMode && <p className="open-orders__filter-basis">Filters and value counts use retrieved values, not proposals. Edited cells show their original values; editing does not remove a row from the filter results.</p>}
      {filterPopup?.identity === filterContext && <OrderFilterEditor key={`${filterContext}:${filterPopup.field}`}
        field={filterPopup.field} lines={report.lines} filters={filters} search={search} anchor={filterAnchor}
        onClose={() => setFilterPopup(null)} onApply={filter => {
          const next = filters.filter(f => f.column !== filterPopup.field);
          if (filter) next.push(filter);
          next.sort((a, b) => a.column.localeCompare(b.column));
          if (JSON.stringify(next) !== JSON.stringify(filters)) applyFilters(next);
          setFilterPopup(null);
        }} />}
      <span className="open-orders__sr-only" role="status">{announcement}</span>
      {displayedRows.length === 0 && <div className="open-orders__state">{report.lines.length === 0 ? 'No open orders in this workspace scope.' : 'No open orders match the current filters.'}</div>}
        <div ref={grid} className="open-orders__grid" data-column-drag-surface="headers" role="region" tabIndex={0} aria-label="Scrollable Customer Open Orders report">
          <table><thead><tr>{visible.map(id => <th scope="col" key={id} aria-label={columnLabel(id)} data-order-column={id} draggable={false} tabIndex={0} title="Drag to reorder, or Alt+Left / Alt+Right"
            className={`${NUMERIC_COLUMNS.has(id as ColumnId) ? 'open-orders__num' : ''} ${dropColumn === id ? 'open-orders__drop' : ''} ${dragColumn === id ? 'open-orders__drag' : ''}`}
            onKeyDown={event => { if (event.target === event.currentTarget && event.altKey && ['ArrowLeft', 'ArrowRight'].includes(event.key)) { event.preventDefault(); visibleMove(id, event.key === 'ArrowLeft' ? -1 : 1); } }}
            onPointerDown={event => columnDrag.start(event, id)} onDragStart={event => event.preventDefault()}>
            <span aria-hidden="true" className="open-orders__grip">⠿ </span>{columnLabel(id)}<button type="button" className="open-orders__funnel"
              aria-label={`Filter ${columnLabel(id)}${filters.some(f => f.column === id) ? ' (active)' : ''}`}
              aria-haspopup="dialog" aria-expanded={filterPopup?.identity === filterContext && filterPopup.field === id}
              data-active={filters.some(f => f.column === id) || undefined}
              title={filters.find(f => f.column === id) ? filterSummary(filters.find(f => f.column === id)!) : `Filter ${columnLabel(id)}`}
              onPointerDown={event => event.stopPropagation()} onClick={event => { event.stopPropagation(); editFilter(id, event.currentTarget); }}>
              <svg aria-hidden="true" width="14" height="14" viewBox="0 0 16 16"><path d="M2 3h12L9.5 8v4l-3 1V8Z" fill="none" stroke="currentColor" strokeWidth="1.4" /></svg>
            </button></th>)}{planMode && <><th scope="col" className="open-orders__reason">Reason Code</th><th scope="col" className="open-orders__actions-heading">Planning status</th></>}</tr></thead>
            <tbody>{displayedRows.slice(currentPage * pageSize, (currentPage + 1) * pageSize).map(r => { const key = keyOf(r.key); const p = proposals[key]; const original = p?.original ?? valuesOf(r); const proposed = p?.proposed ?? original;
              const rowIssues = p ? [...new Set([...issues(p, r, report.isStale || !!failure || pending), ...(restoreIssues[key] ?? [])])] : [];
              const editor = (field: Editable, label: string) => {
                const value = proposed[field]; const isDate = field.endsWith('Date');
                const inputKey = `${key}:${field}`;
                const editingNumber = focusedNumber?.key === inputKey &&
                  (focusedNumber.value === String(value) || equalDecimal(focusedNumber.value, String(value)));
                const inputText = dateInputs[inputKey] ?? (isDate ? formatReportDate(value as string | null)
                  : editingNumber ? focusedNumber.text : planningNumberDisplay(field as 'orderQty' | 'price', String(value)));
                const invalid = isDate && parsePlanningDate(inputText) === undefined;
                const dateError = invalid && (dateTouched[inputKey] || /^\s*\d{1,2}\/\d{1,2}\/\d{4}\s*$/.test(inputText));
                const wasChanged = p && (field === 'price' || field === 'orderQty' ? !equalDecimal(original[field], value as string) : original[field] !== value);
                return <div className={wasChanged ? 'open-orders__changed' : undefined}>
                  <input aria-label={`${label} proposal ${r.key.salesOrder}/${r.key.line}`} type="text" inputMode={isDate ? 'numeric' : 'decimal'} placeholder={isDate ? 'M/d/yyyy' : undefined}
                    data-plan-editor="" aria-invalid={dateError || undefined} value={inputText}
                    onFocus={() => { if (!isDate) setFocusedNumber({ key: inputKey, value: String(value),
                      text: planningNumberDisplay(field as 'orderQty' | 'price', String(value)) }); }}
                    onChange={e => {
                      if (!isDate) {
                        const text = e.target.value;
                        setFocusedNumber(current => current?.key === inputKey ? { key: inputKey, text, value: text } : current);
                        update(r, field, text); return;
                      }
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
              return <tr data-order-row={key} tabIndex={-1} className={`open-orders__row${p && changed(p) ? ' open-orders__row--changed' : ''}`} key={key}>
              {visible.map(id => { const value = id === 'orderQty' || id === 'price' ? null : COLUMNS.find(c => c.id === id)!.value(r);
                const dateField = id === 'dueDate' || id === 'performDate' || id === 'requiredDate' || id === 'dockDate' ? id : null;
                return <td data-drop={dropColumn === id || undefined} data-drag={dragColumn === id || undefined} className={NUMERIC_COLUMNS.has(id as ColumnId) ? 'open-orders__num' : id === 'itemNumber' || id === 'order' ? 'open-orders__identity' : undefined} key={id}>
                {planMode && dateField ? editor(dateField, COLUMNS.find(c => c.id === id)!.label)
                  : planMode && (id === 'orderQty' || id === 'price') ? editor(id, id === 'price' ? 'Price' : 'Order Qty')
                  : planMode && id === 'open' ? <><strong>{subtract(proposed.orderQty, r.shippedQtyText ?? String(r.shippedQty)) ?? '—'}</strong><small>Original: {r.open}</small></>
                  : planMode && id === 'extPrice' ? <><strong>{multiply(proposed.price, subtract(proposed.orderQty, r.shippedQtyText ?? String(r.shippedQty)) ?? '') ?? '—'}</strong><small>Original: {r.extPrice}</small></>
                  : value === null ? '' : DATE_COLUMNS.has(id as ColumnId) ? formatReportDate(String(value)) : typeof value === 'boolean' ? value ? 'Yes' : 'No' : String(value)}
              </td>; })}{planMode && <><td className="open-orders__reason"><select data-plan-editor="" aria-label={`Reason Code ${r.key.salesOrder}/${r.key.line}`} value={p?.reasonCode ?? ''} onChange={e => update(r, 'reasonCode', e.target.value || null)}><option value="">Select reason</option>{REASONS.map(reason => <option key={reason} value={reason}>{reason}</option>)}</select></td>
                <td className="open-orders__actions">{p && <><span className={rowIssues.length ? 'open-orders__review' : 'open-orders__changed'} title={rowIssues.join(' ')}>{rowIssues.length ? 'Review' : 'Changed'}</span><button type="button" onClick={() => undo(key)}>Undo {r.key.salesOrder}/{r.key.line}</button>{rowIssues.length > 0 && <small className="open-orders__review">{rowIssues.join(' ')}</small>}</>}</td></>}</tr>; })}</tbody></table>
        </div>
      {planMode && <div className="open-orders__planning-summary open-orders__dock" role="status">
        {staged.length} changed across workspace · {incomplete.length} missing Reason Code · {conflicts.length} conflicts · {hiddenChanges} outside current filter/page view. {report.isStale || failure || pending || conflicts.length || incomplete.length || invalidDateEntries || draftBlocked || sourceChanged ? 'Not export-ready.' : 'Generate files validates changed lines against QAD before preparing CSVs.'}
        <label><input type="checkbox" checked={onlyChanged} onChange={e => { setOnlyChanged(e.target.checked); setPage(0); }} /> Show changed rows</label>
        <button type="button" disabled={!staged.length} onClick={e => { confirmationReturn.current = e.currentTarget; setClearConfirmation(true); }}>Clear All</button>
        {hiddenChanges > 0 && <button type="button" aria-expanded={showHidden} onClick={() => setShowHidden(!showHidden)}>Show changes outside this view</button>}
        <button type="button" disabled={!staged.length || !!report.isStale || !!failure || pending || exporting || !!conflicts.length || !!incomplete.length || !!invalidDateEntries || draftBlocked || sourceChanged}
          className="open-orders__primary" onClick={() => void exportAll()}>{exporting ? 'Working…' : 'Generate files…'}</button>
        <span>External processing required; KST submits nothing.</span>
        {prepared?.identity === identity && prepared.signature === exportSignature && !report.isStale && !failure && !pending && !sourceChanged && !invalidDateEntries && !conflicts.length && !incomplete.length && !draftBlocked && <div role="group" aria-label="Prepared QXtend files">Validated CSVs: {prepared.files.map(file =>
          <span key={file.kind}><button type="button" disabled={exporting} onClick={() => void savePrepared(file)}>Save As {file.fileName}</button>
            <span role="status">{prepared.statuses[file.kind] ?? 'Not saved'}</span></span>)} <span>External QXtend processing is required; export does not submit changes.</span></div>}
      </div>}
      {planMode && showHidden && hiddenChanges > 0 && <div className="open-orders__hidden-changes">Changes outside current results: {staged.filter(p => !displayedRows.slice(currentPage * pageSize, (currentPage + 1) * pageSize).some(r => keyOf(r.key) === keyOf(p.key))).map(p => <div key={keyOf(p.key)}>
        {p.key.salesOrder} / {p.key.line} ({p.itemNumber}) · {rowByKey.has(keyOf(p.key)) ? 'filtered or on another page' : 'missing from report — conflict'} · {[...issues(p, rowByKey.get(keyOf(p.key)), report.isStale || !!failure || pending), ...(restoreIssues[keyOf(p.key)] ?? [])].join(' ')} · Original {JSON.stringify(p.original)} → Proposed {JSON.stringify(p.proposed)} · {p.reasonCode ?? 'Reason Code missing'}
        {rowByKey.has(keyOf(p.key)) && <button type="button" onClick={() => { setOnlyChanged(false); clearFilters(); const index = filteredOrderRows(report.lines, []).findIndex(r => keyOf(r.key) === keyOf(p.key)); if (index >= 0) { findTarget.current = keyOf(p.key); setPage(Math.floor(index / pageSize)); } }}>Find row</button>}
        <button type="button" onClick={() => undo(keyOf(p.key))}>Undo {p.key.salesOrder}/{p.key.line}</button>
      </div>)}</div>}
      <footer className="open-orders__page"><span>Rows {displayedRows.length ? currentPage * pageSize + 1 : 0}–{Math.min((currentPage + 1) * pageSize, displayedRows.length)} of {displayedRows.length} · filtered from {report.lines.length} lines · {rows.length} matching filters</span>
        <span className="open-orders__footer-purpose">{planMode ? 'External QXtend processing required; KST submits nothing.' : report.isStale || failure || pending ? 'Not current; do not use for operational/QXtend validation.' : 'Report only; not operational/QXtend validation.'}</span>
        <div role="group" aria-label="Rows per page" className="open-orders__sizes"><span>Rows per page</span>{OPEN_ORDERS_PAGE_SIZES.map(size => <button key={size} type="button" aria-pressed={pageSize === size} onClick={() => resizePage(size)}>{size}</button>)}</div>
        <button type="button" disabled={currentPage === 0} onClick={() => setPage(currentPage - 1)}>Previous page</button><span>Page {currentPage + 1} of {pageCount}</span><button type="button" disabled={currentPage + 1 >= pageCount} onClick={() => setPage(currentPage + 1)}>Next page</button>
      </footer>
    </>}
    {pending && <p className="open-orders__load-status" role="status">{report ? 'Refreshing report; displaying previous acquisition until complete.' : 'Loading Customer Open Orders…'}</p>}
    {!pending && !report && !failure && <p className="open-orders__load-status" role="status">Loading Customer Open Orders…</p>}
    {clearConfirmation && <ConfirmDialog title="Clear All planning changes?" body={`Clear all ${staged.length} changes? Saved-draft behavior follows the Save Draft setting.`} confirmLabel="Confirm Clear All" destructive onCancel={() => setClearConfirmation(false)} onConfirm={() => {
      setRestoreIssues({}); setDateInputs({}); setDateTouched({}); invalidDateInputs.current.clear(); persist({}, saveDraft, report, false, false); setClearConfirmation(false);
    }} />}
    {saveOffConfirm === assignmentId && <ConfirmDialog title={restoreCandidate ? 'Discard saved draft?' : 'Turn Save Draft off?'} body={restoreCandidate ? 'Delete the saved copy without restoring it? In-memory changes will remain.' : 'Turn Save Draft off and delete the saved copy? In-memory changes will remain.'} confirmLabel={restoreCandidate ? 'Discard saved draft' : 'Turn off and delete'} destructive
      onCancel={() => setSaveOffConfirm(null)}
      onConfirm={() => {
        setSaveOffConfirm(null);
        if (saveOffPending) return;
        setSaveOffPending(true);
        void (async () => { await saveGate.current; await new ApiClient(await resolveBackendBaseUrl()).deleteOpenOrdersDraft(assignmentId); })().then(() => {
          if (proposalAssignmentRef.current !== assignmentId) return;
          saveGeneration.current++; setSaveDraft(false); setSavedState('none'); setDraftError(null); setRestoreCandidate(null);
        }).catch(() => { if (proposalAssignmentRef.current === assignmentId) setDraftError('Saved draft could not be deleted. The saved copy, Save Draft setting and in-memory changes remain.'); }).finally(() => setSaveOffPending(false));
      }} />}
  </section>;
}

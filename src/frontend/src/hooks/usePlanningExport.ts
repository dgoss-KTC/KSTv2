import { useCallback, useEffect, useRef, useState } from 'react';
import {
  prepareMassUpdaterExport,
  toPlanningExportError,
  type PlanningExportError,
} from '../api/planningWorkbookApi';
import {
  clearableKeys,
  decodeMassUpdaterBytes,
  exportedAdjustments,
  exportRequestEntries,
  MassUpdaterFileError,
  parseMassUpdaterCsv,
  verifyAgainstStaged,
  type MassUpdaterRow,
} from '../planningWorkbook/massUpdaterExport';
import type { AdjustmentEntry, PlanningEvaluation } from '../planningWorkbook/planningAdjustments';
import { saveCsvFile } from '../shared/saveCsvFile';
import type { PlanningProposals } from './usePlanningProposals';

/** The export request was prepared, but the file does not match the staged proposals. Never offered for saving. */
export type ExportFailure = PlanningExportError | { type: 'file-mismatch' };

export type SaveState =
  | { status: 'idle' }
  | { status: 'saving' }
  /** The Save As dialog was dismissed: nothing was written. */
  | { status: 'cancelled' }
  | { status: 'failed' };

/** A prepared export, tied to the context it was prepared in. */
export interface PreparedExport {
  fileName: string;
  contentBase64: string;
  /** Parsed from the actual bytes: what the user reviews is what is saved. */
  rows: MassUpdaterRow[];
  /** Cell key → the staged entry (identity) of every proposal included in the file. */
  exported: Readonly<Record<string, AdjustmentEntry>>;
  /** Adjustment entries beyond the visible horizon that were validated against the full MPS snapshot. */
  hiddenCount: number;
  originKey: string;
  sessionId: number;
}

export type ExportPhase =
  | { kind: 'closed' }
  | { kind: 'preparing' }
  /** Local validation problems (or nothing staged): nothing was requested or exported. */
  | { kind: 'blocked'; reason: 'issues' | 'empty' }
  | { kind: 'rejected'; failure: ExportFailure }
  /** The session, adjustments, MPS snapshot or business week changed while preparing or after preparation. */
  | { kind: 'obsolete' }
  | { kind: 'ready'; prepared: PreparedExport; save: SaveState }
  /** A file write was confirmed (native Save As). Success means the file was written, not that QAD applied it. */
  | { kind: 'saved'; prepared: PreparedExport; fileName: string; choice: 'ask' | 'cleared' | 'kept'; clearedCount: number }
  /** Browser fallback: a download was started, but the browser cannot confirm a file was written. Nothing is cleared or offered for clearing. */
  | { kind: 'downloaded'; prepared: PreparedExport; fileName: string };

interface View { sessionId: number; token: number; phase: ExportPhase }

const MASS_UPDATER_FILTER = 'Mass Updater CSV';

/**
 * Stage 14.6 export flow for one workspace session: validate (frontend pre-check, then the backend
 * against the complete loaded MPS snapshot) → review the actual file → explicit Save As → optional
 * clearing of the exported adjustments.
 *
 * A prepared export is tied to the workspace session, the adjustment revision, the MPS snapshot, the
 * site and the business week it was prepared in. Any change makes it obsolete (it is never saved). The
 * state belongs to one workspace session: leaving a workspace (including A → B → A, which starts a new
 * session) discards it, and every asynchronous result is applied only to the view that issued it. A late
 * preparation or save response can therefore never update, or clear the adjustments of, another session.
 * Saved drafts are never read, written or deleted here.
 */
export function usePlanningExport({ assignmentId, site, snapshotId, businessWeek, evaluation, proposals }: {
  assignmentId: string;
  site: string;
  snapshotId: string | null;
  businessWeek: string;
  evaluation: PlanningEvaluation | null;
  proposals: PlanningProposals;
}) {
  const { sessionId, editVersion, entries, clearExported } = proposals;
  const liveKey = `${assignmentId}|${sessionId}|${editVersion}|${snapshotId ?? ''}|${site}|${businessWeek}`;
  const liveKeyRef = useRef(liveKey);
  useEffect(() => { liveKeyRef.current = liveKey; }, [liveKey]);
  const tokens = useRef(0);
  const [stored, setStored] = useState<View>(() => ({ sessionId, token: 0, phase: { kind: 'closed' } }));
  const view: View = stored.sessionId === sessionId ? stored : { sessionId, token: 0, phase: { kind: 'closed' } };

  /** Applies a result only while `token` is still the current view of this workspace session. */
  const apply = useCallback((owner: number, token: number, next: (phase: ExportPhase) => ExportPhase) => {
    setStored(v => (v.sessionId === owner && v.token === token ? { ...v, phase: next(v.phase) } : v));
  }, []);

  const total = proposals.stagedCount + proposals.invalidInputCount;
  const canExport = !!snapshotId && evaluation !== null && total > 0;

  const open = useCallback(() => {
    if (!snapshotId || !evaluation) return;
    const owner = sessionId;
    const token = ++tokens.current;
    const show = (phase: ExportPhase) => setStored({ sessionId: owner, token, phase });
    if (evaluation.issueCount > 0 || proposals.invalidInputCount > 0) { show({ kind: 'blocked', reason: 'issues' }); return; }
    if (evaluation.stagedCount === 0) { show({ kind: 'blocked', reason: 'empty' }); return; }

    const originKey = liveKey;
    const exported = exportedAdjustments(entries);
    show({ kind: 'preparing' });
    prepareMassUpdaterExport(assignmentId, { mpsSnapshotId: snapshotId, site, entries: exportRequestEntries(entries) })
      .then(file => {
        // Anything that changed while the request was running makes this result obsolete.
        if (liveKeyRef.current !== originKey) { apply(owner, token, () => ({ kind: 'obsolete' })); return; }
        let rows: MassUpdaterRow[];
        try {
          rows = parseMassUpdaterCsv(decodeMassUpdaterBytes(file.contentBase64));
          if (file.mpsSnapshotId !== snapshotId || file.site !== site || verifyAgainstStaged(rows, exported, file.rowCount) !== null) {
            throw new MassUpdaterFileError('The prepared file does not match the staged adjustments.');
          }
        } catch (error) {
          if (!(error instanceof MassUpdaterFileError)) throw error;
          apply(owner, token, () => ({ kind: 'rejected', failure: { type: 'file-mismatch' } }));
          return;
        }
        apply(owner, token, () => ({
          kind: 'ready',
          save: { status: 'idle' },
          prepared: { fileName: file.fileName, contentBase64: file.contentBase64, rows, exported, hiddenCount: evaluation.hiddenCount, originKey, sessionId: owner },
        }));
      })
      .catch((error: unknown) => {
        if (liveKeyRef.current !== originKey) { apply(owner, token, () => ({ kind: 'obsolete' })); return; }
        apply(owner, token, () => ({ kind: 'rejected', failure: toPlanningExportError(error) }));
      });
  }, [apply, assignmentId, entries, evaluation, liveKey, proposals.invalidInputCount, sessionId, site, snapshotId]);

  /** Closing is ignored while the Save As is in progress (its outcome must be shown). Dismissing the prompt keeps the adjustments. */
  const close = useCallback(() => {
    setStored(v => (v.sessionId === sessionId && v.phase.kind === 'ready' && v.phase.save.status === 'saving' ? v
      : { sessionId, token: ++tokens.current, phase: { kind: 'closed' } }));
  }, [sessionId]);

  const save = useCallback(async () => {
    const phase = view.phase;
    if (phase.kind !== 'ready' || phase.save.status === 'saving') return;
    // The prepared export must still be current before any save is initiated.
    if (liveKeyRef.current !== phase.prepared.originKey) { apply(sessionId, view.token, () => ({ kind: 'obsolete' })); return; }
    const owner = sessionId;
    const token = view.token;
    const prepared = phase.prepared;
    apply(owner, token, p => (p.kind === 'ready' ? { ...p, save: { status: 'saving' } } : p));
    try {
      const result = await saveCsvFile(prepared.contentBase64, prepared.fileName, MASS_UPDATER_FILTER);
      apply(owner, token, p => {
        if (p.kind !== 'ready') return p;
        if (result.kind === 'cancelled') return { ...p, save: { status: 'cancelled' } };
        if (result.kind === 'downloaded') return { kind: 'downloaded', prepared, fileName: result.fileName };
        return { kind: 'saved', prepared, fileName: result.fileName, choice: 'ask', clearedCount: 0 };
      });
    } catch {
      apply(owner, token, p => (p.kind === 'ready' ? { ...p, save: { status: 'failed' } } : p));
    }
  }, [apply, sessionId, view.phase, view.token]);

  /** Clear: removes the exported entries (unchanged since preparation) from the originating session only. */
  const clear = useCallback(() => {
    const phase = view.phase;
    if (phase.kind !== 'saved' || phase.choice !== 'ask') return;
    const count = clearableKeys(entries, phase.prepared.exported).length;
    clearExported(phase.prepared.sessionId, phase.prepared.exported);
    apply(sessionId, view.token, p => (p.kind === 'saved' ? { ...p, choice: 'cleared', clearedCount: count } : p));
  }, [apply, clearExported, entries, sessionId, view.phase, view.token]);

  const keep = useCallback(() => {
    apply(sessionId, view.token, p => (p.kind === 'saved' && p.choice === 'ask' ? { ...p, choice: 'kept' } : p));
  }, [apply, sessionId, view.token]);

  // A prepared export whose context changed is shown as obsolete and can never be saved. While its Save As
  // is in progress the outcome of that save is still shown (the file may be written).
  const phase: ExportPhase = view.phase.kind === 'ready' && view.phase.save.status !== 'saving' && view.phase.prepared.originKey !== liveKey
    ? { kind: 'obsolete' } : view.phase;

  return { phase, canExport, isOpen: phase.kind !== 'closed', open, close, save, clear, keep };
}

export type PlanningExport = ReturnType<typeof usePlanningExport>;

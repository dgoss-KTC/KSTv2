import { useCallback, useEffect, useRef, useState, useSyncExternalStore } from 'react';
import type { PlanningWorkbookDraftResponseDto } from '../api/client';
import {
  deletePlanningDraft,
  fetchPlanningDraftPresence,
  loadPlanningDraft,
  savePlanningDraft,
  toPlanningDraftError,
  type PlanningDraftError,
} from '../api/planningWorkbookApi';
import { adjustmentKey, parseAdjustment, summarizeEntries, type AdjustmentEntries } from '../planningWorkbook/planningAdjustments';

const HISTORY_LIMIT = 50;

export type DraftPresence = 'unknown' | 'checking' | 'none' | 'exists' | 'error';

export interface DraftReview {
  /** The MPS snapshot the comparison was made against; restoring requires it to still be loaded. */
  snapshotId: string;
  /** Draft epoch the review was read in; restoring requires that no save/delete happened since. */
  epoch: number;
  draft: PlanningWorkbookDraftResponseDto;
}

/**
 * Per-assignment draft epoch, shared by every session of the hook. It advances whenever a save or
 * delete starts or completes (including completions that belong to an earlier workspace session),
 * so a presence or review result read before that point can never resurrect a deleted draft,
 * overwrite newer draft metadata, or be restored and labelled as saved.
 */
const draftEpochs = new Map<string, number>();
const epochListeners = new Set<() => void>();
const currentEpoch = (assignmentId: string) => draftEpochs.get(assignmentId) ?? 0;
const advanceEpoch = (assignmentId: string) => {
  draftEpochs.set(assignmentId, currentEpoch(assignmentId) + 1);
  for (const listener of epochListeners) listener();
};
const subscribeEpochs = (listener: () => void) => { epochListeners.add(listener); return () => { epochListeners.delete(listener); }; };

let nextSessionId = 0;

export interface DraftMessage { tone: 'info' | 'error'; text: string }

interface State {
  assignmentId: string;
  /**
   * Identity of this workspace session. Leaving and returning to the same assignment starts a new
   * session, so results of requests issued by an earlier session are never applied.
   */
  sessionId: number;
  entries: AdjustmentEntries;
  history: AdjustmentEntries[];
  /**
   * Cell key of the current cell-editing session: keystrokes coalesce into one undo step only while
   * this is set. It is cleared when focus leaves the cell and at save/restore/remove/clear/undo/delete.
   */
  lastEditKey: string | null;
  editVersion: number;
  /** editVersion that matches the saved draft; -1 when nothing in this session is known to be saved. */
  savedVersion: number;
  /**
   * Draft epoch at which `savedVersion` was established. Any later save or delete (including one
   * from an earlier visit to this workspace that completes afterwards) may have changed the file,
   * so the session is no longer known to match it.
   */
  savedEpoch: number;
  presence: DraftPresence;
  /**
   * Draft epoch at which `presence` was established (or, while 'checking', at which the check was
   * issued). A later save or delete — including one from an earlier visit that finishes or fails
   * afterwards — makes it obsolete, and the active session checks again once (see the recheck effect).
   */
  presenceEpoch: number;
  corrupt: boolean;
  review: DraftReview | null;
  reviewing: boolean;
  busy: 'save' | 'delete' | null;
  savedAtUtc: string | null;
  message: DraftMessage | null;
}

const initial = (assignmentId: string): State => ({
  assignmentId, sessionId: ++nextSessionId, entries: {}, history: [], lastEditKey: null, editVersion: 0, savedVersion: -1, savedEpoch: -1,
  presence: 'unknown', presenceEpoch: -1, corrupt: false, review: null, reviewing: false, busy: null, savedAtUtc: null, message: null,
});

function draftErrorText(error: PlanningDraftError, action: 'save' | 'load' | 'delete'): string {
  switch (error.type) {
    case 'corrupt': return 'The saved Planning draft could not be read. It was kept on disk for recovery. Delete it before saving a new draft.';
    case 'invalid': return 'Correct or remove invalid adjustments before saving the draft.';
    case 'not-found': return 'This workspace was not found. It may have been deleted.';
    case 'mps-not-loaded': return 'MPS data is not loaded for this workspace. Load it from the Dashboard tab, then try again.';
    case 'snapshot-changed': return 'The workspace MPS data changed. Reload MPS on the Dashboard tab, then try again.';
    default:
      return action === 'save' ? 'Save Draft failed. Your adjustments remain in this session; try again.'
        : action === 'delete' ? 'The saved draft could not be deleted. Try again.'
          : 'The saved draft could not be read. Your current adjustments are unchanged; try again.';
  }
}

/**
 * Stage 14.5 Planning proposal state for one workspace assignment. Lives above the Planning panel
 * (in CustomerWorkspace) so staged adjustments survive module switches, horizon changes, workbook
 * refreshes, business-week rollover and MPS snapshot replacement (signed adjustments are simply
 * re-evaluated against the new baseline). A different assignment always starts a new, empty
 * workspace session (also when returning to an assignment visited earlier). Every asynchronous draft
 * response is ignored unless it belongs to the current workspace session and the latest request of
 * its kind, and presence/review results are also dropped once a later save or delete has started
 * or finished. Planning never loads or refreshes MPS, and drafts never touch QAD.
 */
export function usePlanningProposals(assignmentId: string, snapshotId: string | null) {
  const [raw, setRaw] = useState<State>(() => initial(assignmentId));
  // A different assignment always starts from empty state (React's derived-state-on-prop-change pattern).
  if (raw.assignmentId !== assignmentId) setRaw(initial(assignmentId));
  const state = raw.assignmentId === assignmentId ? raw : initial(assignmentId);
  const session = state.sessionId;
  // `mutation` is shared by save and delete: a newer save or delete supersedes any pending one.
  const tokens = useRef({ presence: 0, review: 0, mutation: 0 });

  /** Applies an update only while `owner` is still the current workspace session; late results are dropped. */
  const update = useCallback((owner: number, fn: (s: State) => State) => {
    setRaw(s => (s.sessionId === owner ? fn(s) : s));
  }, []);

  const change = useCallback((next: (entries: AdjustmentEntries) => AdjustmentEntries, editKey: string | null) => {
    update(session, s => {
      const entries = next(s.entries);
      if (entries === s.entries) return s;
      const coalesce = editKey !== null && s.lastEditKey === editKey;
      return {
        ...s, entries, lastEditKey: editKey,
        history: coalesce ? s.history : [...s.history, s.entries].slice(-HISTORY_LIMIT),
        editVersion: s.editVersion + 1,
      };
    });
  }, [session, update]);

  /** Ends the current cell-editing session (focus left the cell, or an action boundary). */
  const endEdit = useCallback(() => {
    update(session, s => (s.lastEditKey === null ? s : { ...s, lastEditKey: null }));
  }, [session, update]);

  /** Keyed by part and Monday week label. Blank removes the entry (blank means zero). */
  const setAdjustment = useCallback((part: string, weekLabel: string, text: string) => {
    const key = adjustmentKey(part, weekLabel);
    change(entries => {
      if ((entries[key]?.text ?? '') === text) return entries;
      const next = { ...entries };
      if (text === '') delete next[key]; else next[key] = { part, weekLabel, text };
      return next;
    }, key);
  }, [change]);

  const removeEntry = useCallback((key: string) => {
    change(entries => {
      if (!(key in entries)) return entries;
      const next = { ...entries };
      delete next[key];
      return next;
    }, null);
  }, [change]);

  const clearAll = useCallback(() => change(entries => (Object.keys(entries).length ? {} : entries), null), [change]);

  const undo = useCallback(() => {
    update(session, s => (s.history.length === 0 ? s : {
      ...s, entries: s.history[s.history.length - 1], history: s.history.slice(0, -1),
      lastEditKey: null, editVersion: s.editVersion + 1,
    }));
  }, [session, update]);

  const checkDraft = useCallback(() => {
    const owner = session;
    const id = assignmentId;
    const token = ++tokens.current.presence;
    const epoch = currentEpoch(id);
    // A result read before a later save/delete started or finished describes an older draft state.
    const current = () => token === tokens.current.presence && epoch === currentEpoch(id);
    update(owner, s => ({ ...s, presence: 'checking', presenceEpoch: epoch }));
    // A dropped result leaves 'checking' at an obsolete epoch; the recheck effect then checks again.
    fetchPlanningDraftPresence(id)
      .then(exists => update(owner, s => (!current() ? s : { ...s, presence: exists ? 'exists' : 'none', presenceEpoch: epoch })))
      .catch(() => update(owner, s => (!current() ? s : { ...s, presence: 'error', presenceEpoch: epoch })));
  }, [assignmentId, session, update]);

  const reviewDraft = useCallback(() => {
    const owner = session;
    const id = assignmentId;
    if (!snapshotId) {
      update(owner, s => ({ ...s, message: { tone: 'error', text: 'Load MPS data for this workspace before reviewing the saved draft.' } }));
      return;
    }
    const against = snapshotId;
    const token = ++tokens.current.review;
    const epoch = currentEpoch(id);
    update(owner, s => ({ ...s, reviewing: true, review: null, lastEditKey: null, message: null }));
    const stale = (s: State): State => ({
      ...s, reviewing: false, message: { tone: 'error', text: 'The saved draft changed while it was being read. Review it again.' },
    });
    loadPlanningDraft(id, against)
      .then(draft => update(owner, s => {
        if (token !== tokens.current.review) return s;
        if (epoch !== currentEpoch(id)) return stale(s);
        if (!draft.exists) return { ...s, reviewing: false, presence: 'none', presenceEpoch: epoch, message: { tone: 'info', text: 'No saved Planning draft was found.' } };
        if (draft.isCorrupt) return { ...s, reviewing: false, corrupt: true, presence: 'exists', presenceEpoch: epoch, message: { tone: 'error', text: draftErrorText({ type: 'corrupt' }, 'load') } };
        return { ...s, reviewing: false, corrupt: false, presence: 'exists', presenceEpoch: epoch, review: { snapshotId: against, epoch, draft } };
      }))
      .catch((err: unknown) => update(owner, s => (token !== tokens.current.review ? s
        : epoch !== currentEpoch(id) ? stale(s)
          : { ...s, reviewing: false, message: { tone: 'error', text: draftErrorText(toPlanningDraftError(err), 'load') } })));
  }, [assignmentId, session, snapshotId, update]);

  /** Cancelling a review never changes current proposals. */
  const cancelReview = useCallback(() => {
    tokens.current.review++;
    update(session, s => ({ ...s, review: null, reviewing: false }));
  }, [session, update]);

  /** Starts a save or delete: supersedes pending mutations and invalidates reviews and presence reads. */
  const beginMutation = useCallback((owner: number, id: string, busy: 'save' | 'delete') => {
    advanceEpoch(id);
    tokens.current.review++;
    tokens.current.presence++;
    const token = ++tokens.current.mutation;
    update(owner, x => ({ ...x, busy, review: null, reviewing: false, lastEditKey: null, message: null }));
    return token;
  }, [update]);

  /**
   * Explicit restore after review. Replaces the session's adjustments with the saved signed
   * adjustments (flagged entries included); undo returns to the previous session state.
   */
  const restoreDraft = useCallback((): boolean => {
    const s = state;
    const review = s.review;
    if (!review) return false;
    if (review.snapshotId !== snapshotId) {
      update(session, x => ({ ...x, review: null, message: { tone: 'error', text: 'The loaded MPS data changed after the draft was reviewed. Review the saved draft again.' } }));
      return false;
    }
    // The saved draft must still be the one this review represents; otherwise its rows are not "saved".
    if (review.epoch !== currentEpoch(assignmentId)) {
      update(session, x => ({ ...x, review: null, message: { tone: 'error', text: 'The saved draft changed after it was reviewed. Review the saved draft again.' } }));
      return false;
    }
    const { staged, invalid } = summarizeEntries(s.entries);
    if ((staged > 0 || invalid > 0) && !window.confirm(`Replace the ${staged + invalid} adjustment(s) in this session with the saved draft?`)) return false;
    const entries: Record<string, { part: string; weekLabel: string; text: string }> = {};
    for (const row of review.draft.rows) entries[adjustmentKey(row.part, row.weekLabel)] = { part: row.part, weekLabel: row.weekLabel, text: row.adjustment };
    const flagged = review.draft.rows.filter(r => r.issueCode !== null).length;
    update(session, x => ({
      ...x, entries, history: [...x.history, x.entries].slice(-HISTORY_LIMIT), lastEditKey: null,
      editVersion: x.editVersion + 1, savedVersion: x.editVersion + 1, savedEpoch: review.epoch, review: null, savedAtUtc: review.draft.savedAtUtc,
      message: { tone: 'info', text: `Saved draft restored: ${review.draft.rows.length} adjustment(s)${flagged ? `, ${flagged} need attention` : ''}. Nothing has been applied to QAD.` },
    }));
    return true;
  }, [assignmentId, session, snapshotId, state, update]);

  const saveDraft = useCallback(async () => {
    const owner = session;
    const id = assignmentId;
    const s = state;
    if (!snapshotId) { update(owner, x => ({ ...x, message: { tone: 'error', text: 'Load MPS data for this workspace before saving a draft.' } })); return; }
    if (s.corrupt) { update(owner, x => ({ ...x, message: { tone: 'error', text: draftErrorText({ type: 'corrupt' }, 'save') } })); return; }
    const values = Object.values(s.entries).map(e => ({ ...e, parse: parseAdjustment(e.text) }));
    if (values.some(v => v.parse.kind === 'invalid-number' || v.parse.kind === 'not-whole-number')) {
      update(owner, x => ({ ...x, message: { tone: 'error', text: draftErrorText({ type: 'invalid', code: null }, 'save') } }));
      return;
    }
    if (!values.some(v => v.parse.kind === 'value')) {
      update(owner, x => ({ ...x, message: { tone: 'error', text: 'Enter at least one adjustment before saving a draft.' } }));
      return;
    }
    const version = s.editVersion;
    const token = beginMutation(owner, id, 'save');
    try {
      const saved = await savePlanningDraft(id, {
        mpsSnapshotId: snapshotId,
        entries: values.filter(v => v.parse.kind === 'value').map(v => ({ part: v.part, weekLabel: v.weekLabel, adjustment: v.text })),
      });
      advanceEpoch(id);
      const savedEpoch = currentEpoch(id);
      update(owner, x => (token !== tokens.current.mutation ? x : {
        ...x, busy: null, presence: 'exists', presenceEpoch: savedEpoch, corrupt: false, savedAtUtc: saved.savedAtUtc,
        // Only the version that was sent is marked saved; newer edits made during the save stay unsaved.
        savedVersion: version, savedEpoch,
        message: { tone: 'info', text: x.editVersion === version ? 'Draft saved. Nothing has been applied to QAD.' : 'Draft saved; newer changes are not saved yet.' },
      }));
    } catch (err) {
      advanceEpoch(id);
      const error = toPlanningDraftError(err);
      update(owner, x => (token !== tokens.current.mutation ? x : {
        ...x, busy: null, corrupt: x.corrupt || error.type === 'corrupt', message: { tone: 'error', text: draftErrorText(error, 'save') },
      }));
    }
  }, [assignmentId, beginMutation, session, snapshotId, state, update]);

  const deleteDraft = useCallback(async () => {
    const owner = session;
    const id = assignmentId;
    if (!window.confirm('Delete the saved Planning draft? Adjustments in this session are not affected.')) return;
    // A save still in flight must not report this session as saved afterwards.
    const token = beginMutation(owner, id, 'delete');
    try {
      await deletePlanningDraft(id);
      advanceEpoch(id);
      const deletedEpoch = currentEpoch(id);
      update(owner, x => (token !== tokens.current.mutation ? x : {
        ...x, busy: null, presence: 'none', presenceEpoch: deletedEpoch, corrupt: false, review: null, savedAtUtc: null, savedVersion: -1,
        message: { tone: 'info', text: 'Saved draft deleted. Adjustments in this session were kept.' },
      }));
    } catch (err) {
      advanceEpoch(id);
      update(owner, x => (token !== tokens.current.mutation ? x : { ...x, busy: null, message: { tone: 'error', text: draftErrorText(toPlanningDraftError(err), 'delete') } }));
    }
  }, [assignmentId, beginMutation, session, update]);

  const { staged, invalid } = summarizeEntries(state.entries);
  // Re-render when any session's save/delete changes the draft file for this assignment.
  const epoch = useSyncExternalStore(subscribeEpochs, () => currentEpoch(assignmentId));
  const matchesSaved = state.editVersion === state.savedVersion && state.savedEpoch === epoch;
  /** Unsaved: the session holds proposals or invalid input not known to match the saved draft. */
  const dirty = (staged > 0 || invalid > 0) && !matchesSaved;

  /*
   * Saved-draft availability that was read (or is being read) before a later save or delete started,
   * finished or failed no longer describes the file. While this session has no save/delete of its own
   * in progress, it checks presence again: once per epoch change (a check never changes the epoch, so
   * this cannot loop), and a failed check shows the existing Retry. Presence is a file-existence read
   * only: nothing is restored, and Planning and MPS are not refreshed.
   */
  const presenceObsolete = (state.presence === 'checking' || state.presence === 'exists' || state.presence === 'none')
    && state.presenceEpoch !== epoch;
  const recheckPresence = presenceObsolete && state.busy === null;
  useEffect(() => { if (recheckPresence) checkDraft(); }, [recheckPresence, checkDraft]);

  useEffect(() => {
    if (!dirty) return;
    const warn = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', warn);
    return () => window.removeEventListener('beforeunload', warn);
  }, [dirty]);

  return {
    assignmentId,
    entries: state.entries,
    stagedCount: staged,
    invalidInputCount: invalid,
    dirty,
    canUndo: state.history.length > 0,
    setAdjustment,
    endEdit,
    removeEntry,
    clearAll,
    undo,
    draft: {
      // Obsolete availability is never shown as current while it is being checked again.
      presence: recheckPresence ? 'checking' as const : state.presence,
      corrupt: state.corrupt,
      review: state.review,
      reviewing: state.reviewing,
      busy: state.busy,
      savedAtUtc: state.savedAtUtc,
      message: state.message,
      /** The session matches the saved draft (which may itself contain staged proposals). */
      isSaved: matchesSaved && state.presence === 'exists',
    },
    checkDraft,
    reviewDraft,
    cancelReview,
    restoreDraft,
    saveDraft,
    deleteDraft,
  };
}

export type PlanningProposals = ReturnType<typeof usePlanningProposals>;

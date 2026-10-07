import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../api/client';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from '../fiscal/fiscalCalendar';
import type { PlanningWorkbook } from '../planningWorkbook/planningWorkbookSummary';
import { fixtureToWorkbook, type Stage14Fixture } from '../planningWorkbook/stage14FixtureAdapter.testSupport';
import { PlanningWorkbookPanel } from './PlanningWorkbookPanel';

/*
 * Stage 14.6 Mass Updater export (panel level). All API traffic and the Save As are mocked: no backend,
 * MPS, Open Orders, QAD, filesystem or Tauri call is possible. Workbook values come from the accepted
 * Stage 14 fixture; the golden file is the accepted Stage 14.1 fixture and is never edited.
 */
const getWorkbook = vi.fn();
const refreshWorkbook = vi.fn();
const draftPresence = vi.fn();
const loadDraft = vi.fn();
const saveDraft = vi.fn();
const deleteDraft = vi.fn();
const exportMock = vi.fn();
const saveFile = vi.fn();
vi.mock('../api/tauri-bridge', () => ({ resolveBackendBaseUrl: () => Promise.resolve('http://localhost'), isRunningInTauri: () => false }));
vi.mock('../api/client', async (original) => ({ ...(await original<typeof import('../api/client')>()),
  ApiClient: class {
    getPlanningWorkbook = getWorkbook; refreshPlanningWorkbook = refreshWorkbook;
    getPlanningWorkbookDraftPresence = draftPresence; loadPlanningWorkbookDraft = loadDraft;
    savePlanningWorkbookDraft = saveDraft; deletePlanningWorkbookDraft = deleteDraft;
    exportPlanningMassUpdater = exportMock;
  },
}));
vi.mock('../shared/saveCsvFile', () => ({ saveCsvFile: (...args: unknown[]) => saveFile(...args) }));

type Fixture = Stage14Fixture;
const fixture = JSON.parse(readFileSync(resolve(process.cwd(), '../../docs/reference/STAGE_14_PLANNING_WORKBOOK_DETERMINISTIC_FIXTURES.json'), 'utf8')) as Fixture;
const goldenText = readFileSync(resolve(process.cwd(), '../../docs/reference/Stage14Fixtures/MpsAdjustments.expected.csv'), 'utf8');
const golden64 = Buffer.from(goldenText, 'utf8').toString('base64');
const goldenLines = goldenText.split('\r\n').filter(Boolean).map(line => line.split(','));
const ID = fixture.scope.workspaceAssignmentId;
const SNAP = fixture.scope.mpsSnapshotId;
const OTHER = '00000000-0000-0000-0000-00000000bbbb';
const FILE_NAME = 'SW-MPS-Adjustments-2026-09-16.csv';

function iso(base: string, weeks: number) {
  const [y, m, d] = base.split('-').map(Number);
  const date = new Date(y, m - 1, d + 7 * weeks);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

/** The accepted fixture workbook padded with empty weeks to the 12-week Planning minimum. */
function fixtureWorkbook(snapshot = SNAP, horizon = 12, maximum = 20): PlanningWorkbook {
  const base = fixtureToWorkbook(fixture);
  const labels = Array.from({ length: horizon }, (_, i) => iso(base.weekLabels[0], i));
  return {
    ...base, mpsSnapshotId: snapshot, horizonWeeks: horizon, weekLabels: labels, maximumHorizonWeeks: maximum,
    parts: base.parts.map(p => ({ ...p, weeks: labels.map((label, i) => p.weeks[i] ?? {
      ...p.weeks[p.weeks.length - 1], weekLabel: label, salesOrderQuantity: '0', salesOrderDollars: '0', mps: '0',
      mpsDollars: '0', forecast: '0',
    }) })),
  };
}
const PARTS = fixtureWorkbook().parts.map(p => p.part);

const problem = (status: number, body: object) => new ApiError(status, 'http://localhost/x', JSON.stringify(body));
function deferred<T>() {
  let resolveFn!: (value: T) => void; let rejectFn!: (error: unknown) => void;
  const promise = new Promise<T>((res, rej) => { resolveFn = res; rejectFn = rej; });
  return { promise, resolve: resolveFn, reject: rejectFn };
}

/** A stand-in for the backend: only whole-number, nonzero, valid entries become rows, ordered by part then week. */
type Request = { mpsSnapshotId: string; site: string; entries: { part: string; weekLabel: string | null; adjustment: string | null }[] };
function csvFor(request: Request) {
  const rows = request.entries.filter(e => /^-?[1-9][0-9]*$/.test(e.adjustment ?? ''))
    .map(e => ({ part: PARTS.find(p => p.toUpperCase() === e.part.toUpperCase())!, week: e.weekLabel!, qty: String(BigInt(e.adjustment!)) }))
    .sort((a, b) => PARTS.indexOf(a.part) - PARTS.indexOf(b.part) || a.week.localeCompare(b.week));
  const text = rows.map(r => { const [y, m, d] = r.week.split('-').map(Number); return `${r.part},${r.qty},${m}/${d}/${y}\r\n`; }).join('');
  return { fileName: FILE_NAME, contentBase64: Buffer.from(text, 'utf8').toString('base64'), rowCount: rows.length, mpsSnapshotId: request.mpsSnapshotId, site: request.site };
}

const input = (part: string, weekIso: string) => {
  const [y, m, d] = weekIso.split('-').map(Number);
  return screen.getByRole('textbox', { name: `Adjustment for ${part}, week of ${m}/${d}/${y}` }) as HTMLInputElement;
};
const type = (el: HTMLInputElement, value: string) => fireEvent.change(el, { target: { value } });
const showAllRows = (assignmentId = ID) => window.localStorage.setItem(`kst.planningWorkbook.view.v1.${assignmentId}`,
  JSON.stringify({ hiddenModels: [], rows: ['so', 'soDollars', 'mps', 'mpsDollars', 'forecast', 'onHand'], totals: ['so', 'soDollars', 'mps', 'mpsDollars', 'adjust'], collapsed: [] }));
const startPlanning = () => fireEvent.click(screen.getByRole('button', { name: /^(Start|Resume) planning/ }));
const exportButton = () => screen.getByRole('button', { name: 'Review and export' });
const dialog = () => screen.getByRole('dialog', { name: 'Review and export' });
const saveAsButton = () => within(dialog()).getByRole('button', { name: /^Save As/ });
/** The dialog has a header close control and a footer Close button (the footer one is last). */
const headerClose = () => within(dialog()).getAllByRole('button', { name: 'Close' })[0];
const footerClose = () => { const buttons = within(dialog()).getAllByRole('button', { name: 'Close' }); return buttons[buttons.length - 1]; };
const previewTable = () => screen.findByRole('table', { name: 'Mass Updater file preview' });
const previewRows = () => within(screen.getByRole('table', { name: 'Mass Updater file preview' })).getAllByRole('row').slice(1)
  .map(row => within(row).getAllByRole('cell').map(cell => cell.textContent));
const CLEAR_QUESTION = 'Clear the staged adjustments included in this export?';

/** The fixture scenario whose Mass Updater rows are the accepted golden file. */
function stageScenario() {
  type(input('P-1', '2026-09-14'), '-50');
  type(input('P-1', '2026-09-21'), '10');
  type(input('P-1', '2026-10-05'), '20');
  type(input('P-2', '2026-09-28'), '-100');
}
const SCENARIO_ENTRIES = [
  { part: 'P-1', weekLabel: '2026-09-14', adjustment: '-50' },
  { part: 'P-1', weekLabel: '2026-09-21', adjustment: '10' },
  { part: 'P-1', weekLabel: '2026-10-05', adjustment: '20' },
  { part: 'P-2', weekLabel: '2026-09-28', adjustment: '-100' },
];

type View = ReturnType<typeof render>;
async function renderPanel(props: Partial<{ snapshotId: string; assignmentId: string }> = {}) {
  const view = render(<PlanningWorkbookPanel assignmentId={props.assignmentId ?? ID} site="S1" snapshotId={props.snapshotId ?? SNAP} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
  await screen.findByRole('region', { name: 'Planning workbook grid' });
  startPlanning();
  return view;
}
const showWorkspace = (view: View, assignmentId: string, snapshotId = SNAP) =>
  view.rerender(<PlanningWorkbookPanel assignmentId={assignmentId} site="S1" snapshotId={snapshotId} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
/** A → B → A: leaves the workspace and returns, which starts a new workspace session for A. */
async function leaveAndReturn(view: View) {
  showWorkspace(view, OTHER);
  await waitFor(() => expect(draftPresence).toHaveBeenLastCalledWith(OTHER));
  showWorkspace(view, ID);
  await waitFor(() => expect(draftPresence).toHaveBeenLastCalledWith(ID));
  await screen.findByRole('region', { name: 'Planning workbook grid' });
  startPlanning();
  await waitFor(() => expect(input('P-1', '2026-09-14').value).toBe(''));
}
async function openAndPrepare() {
  fireEvent.click(exportButton());
  await previewTable();
}

beforeEach(() => {
  for (const mock of [getWorkbook, refreshWorkbook, draftPresence, loadDraft, saveDraft, deleteDraft, exportMock, saveFile]) mock.mockReset();
  window.localStorage.clear();
  showAllRows(); showAllRows(OTHER);
  getWorkbook.mockImplementation((id: string, snapshot: string, horizon: number) => Promise.resolve({ ...fixtureWorkbook(snapshot, horizon), workspaceId: id }));
  draftPresence.mockResolvedValue({ exists: false });
  exportMock.mockImplementation((_id: string, request: Request) => Promise.resolve(csvFor(request)));
});
afterEach(() => vi.restoreAllMocks());

describe('Planning export: validation feedback and preview', { timeout: 20_000 }, () => {
  it('is enabled only when something is staged, and describes itself without applying anything to QAD', async () => {
    await renderPanel();
    expect(exportButton()).toBeDisabled();
    expect(exportButton()).toHaveAccessibleDescription(/Stage at least one adjustment/);

    type(input('P-1', '2026-09-14'), '-50');
    expect(exportButton()).toBeEnabled();
    expect(exportButton()).toHaveAccessibleDescription(/Nothing is applied to QAD/);
    expect(exportMock).not.toHaveBeenCalled(); // opening Planning and staging make no export request
  });

  it('previews exactly the rows of the generated golden file (Part, Qty, Date) and calls only the export endpoint', async () => {
    exportMock.mockResolvedValue({ fileName: FILE_NAME, contentBase64: golden64, rowCount: 4, mpsSnapshotId: SNAP, site: 'S1' });
    await renderPanel();
    stageScenario();
    const workbookCalls = getWorkbook.mock.calls.length;

    await openAndPrepare();

    expect(exportMock).toHaveBeenCalledTimes(1);
    expect(exportMock).toHaveBeenCalledWith(ID, { mpsSnapshotId: SNAP, site: 'S1', entries: SCENARIO_ENTRIES });
    expect(previewRows()).toEqual(goldenLines);                       // same rows, order and values as the file
    expect(previewRows()[0]).toEqual(['P-2', '-100', '9/28/2026']);   // workspace part order, then week; no leading +
    const headers = within(screen.getByRole('table', { name: 'Mass Updater file preview' })).getAllByRole('columnheader').map(h => h.textContent);
    expect(headers).toEqual(['Part', 'Qty', 'Date']);
    expect(dialog()).toHaveTextContent('Qty is the signed adjustment to the existing MPS quantity, not the resulting quantity');
    expect(dialog()).toHaveTextContent('4 rows ready');
    expect(dialog()).toHaveTextContent(FILE_NAME);
    expect(saveFile).not.toHaveBeenCalled();                          // preparing never saves
    expect(getWorkbook).toHaveBeenCalledTimes(workbookCalls);          // no workbook/MPS reload
    expect(refreshWorkbook).not.toHaveBeenCalled();
  });

  it('lists local problems and makes no request: invalid text, non-whole numbers and negative results', async () => {
    await renderPanel();
    type(input('P-1', '2026-09-14'), '+5');
    type(input('P-1', '2026-09-21'), '1.5');
    type(input('P-1', '2026-09-28'), '-150');   // loaded MPS 100
    type(input('P-2', '2026-09-28'), '-100');   // valid

    fireEvent.click(exportButton());

    const table = await screen.findByRole('table', { name: 'Adjustments needing attention' });
    expect(within(dialog()).getByRole('alert')).toHaveTextContent('3 adjustments need attention');
    const rows = within(table).getAllByRole('row').slice(1);
    expect(rows).toHaveLength(3);
    expect(table).toHaveTextContent('Invalid number');
    expect(table).toHaveTextContent('Not a whole number');
    expect(table).toHaveTextContent('Negative Proposed MPS');
    expect(exportMock).not.toHaveBeenCalled();
    expect(saveFile).not.toHaveBeenCalled();
    expect(within(dialog()).queryByRole('button', { name: /^Save As/ })).not.toBeInTheDocument();
    fireEvent.click(footerClose());
    expect(input('P-1', '2026-09-14').value).toBe('+5'); // nothing was changed or removed
  });

  it('shows a sanitized backend rejection and keeps every adjustment; no preview or Save As exists', async () => {
    exportMock.mockRejectedValue(problem(400, { title: 'Mass Updater export blocked', issueCode: 'negative-result', affectedRowCount: 2 }));
    await renderPanel();
    stageScenario();

    fireEvent.click(exportButton());

    const alert = await within(dialog()).findByRole('alert');
    expect(alert).toHaveTextContent('2 adjustments need attention: an adjustment would make an MPS quantity negative.');
    expect(alert).toHaveTextContent('No file was created and your adjustments are unchanged.');
    expect(alert.textContent).not.toMatch(/P-1|P-2|-50|-100/);
    expect(screen.queryByRole('table', { name: 'Mass Updater file preview' })).not.toBeInTheDocument();
    expect(within(dialog()).queryByRole('button', { name: /^Save As/ })).not.toBeInTheDocument();
    fireEvent.click(footerClose());
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
  });

  it.each([
    [problem(409, { issueCode: 'snapshot-changed', affectedRowCount: 0 }), /MPS data changed/],
    [problem(409, { issueCode: 'mps-not-loaded', affectedRowCount: 0 }), /MPS data is not loaded/],
    [problem(409, { issueCode: 'site-changed', affectedRowCount: 0 }), /site changed/],
    [problem(404, { issueCode: 'workspace-missing', affectedRowCount: 0 }), /workspace was not found/],
    [problem(500, { issueCode: 'export-failed', affectedRowCount: 0 }), /could not be prepared/],
    [new TypeError('Failed to fetch'), /did not reach the backend/],
  ])('reports a failed preparation without exposing details: %#', async (error, copy) => {
    exportMock.mockRejectedValue(error);
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');

    fireEvent.click(exportButton());

    expect(await within(dialog()).findByRole('alert')).toHaveTextContent(copy);
    expect(saveFile).not.toHaveBeenCalled();
    expect(input('P-1', '2026-09-14').value).toBe('-50');
  });

  it('discards a prepared file that does not match the staged adjustments instead of offering it', async () => {
    exportMock.mockResolvedValue({ fileName: FILE_NAME, contentBase64: golden64, rowCount: 4, mpsSnapshotId: SNAP, site: 'S1' });
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50'); // the golden file holds four rows, only one is staged

    fireEvent.click(exportButton());

    expect(await within(dialog()).findByRole('alert')).toHaveTextContent('did not match the staged adjustments, so it was discarded');
    expect(screen.queryByRole('table', { name: 'Mass Updater file preview' })).not.toBeInTheDocument();
    expect(within(dialog()).queryByRole('button', { name: /^Save As/ })).not.toBeInTheDocument();
  });

  it('exports entries outside the visible horizon (restored from a draft) and says so; validation is the backend’s', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    const far = iso('2026-09-14', 14); // beyond the 12-week horizon, within the maximum of 20
    loadDraft.mockResolvedValue({ exists: true, isCorrupt: false, warning: null, savedAtUtc: '2026-09-15T20:00:00Z', savedMpsSnapshotId: SNAP, savedSite: 'S1',
      rows: [{ part: 'P-1', weekLabel: far, adjustment: '5', savedMpsQuantity: '0', currentMps: '0', baselineChanged: false, proposedMps: '5', issueCode: null }] });
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    fireEvent.click(await screen.findByRole('button', { name: 'Review saved draft' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Restore saved draft' }));
    fireEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Replace' }));
    // The restore replaced the session with the hidden entry only.
    await waitFor(() => expect(screen.getByTestId('planning-change-count')).toHaveTextContent('1 change'));

    await openAndPrepare();

    expect(exportMock).toHaveBeenCalledWith(ID, { mpsSnapshotId: SNAP, site: 'S1', entries: [{ part: 'P-1', weekLabel: far, adjustment: '5' }] });
    expect(previewRows()).toEqual([['P-1', '5', `${Number(far.slice(5, 7))}/${Number(far.slice(8, 10))}/${far.slice(0, 4)}`]]);
    expect(dialog()).toHaveTextContent('including 1 outside the visible horizon');
  });

  it('sends every staged entry whatever the View or filter shows', async () => {
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    type(input('P-2', '2026-09-28'), '-100');
    fireEvent.click(screen.getByRole('button', { name: /^Changed/ })); // filter chip: only changed models
    fireEvent.click(screen.getByRole('button', { name: /^Short/ }));    // narrows the visible models further

    await openAndPrepare();

    const request = exportMock.mock.calls[0][1] as Request;
    expect(request.entries.map(e => `${e.part}|${e.weekLabel}|${e.adjustment}`).sort())
      .toEqual(['P-1|2026-09-14|-50', 'P-2|2026-09-28|-100']);
  });
});

describe('Planning export: Save As outcomes and the Clear / Keep prompt', { timeout: 20_000 }, () => {
  beforeEach(() => { exportMock.mockResolvedValue({ fileName: FILE_NAME, contentBase64: golden64, rowCount: 4, mpsSnapshotId: SNAP, site: 'S1' }); });

  it('saves the exact validated bytes through the existing Save As workflow, then states the file is not applied to QAD', async () => {
    saveFile.mockResolvedValue({ kind: 'saved', fileName: 'Chosen.csv' });
    await renderPanel();
    stageScenario();
    await openAndPrepare();

    fireEvent.click(saveAsButton());

    expect(await within(dialog()).findByText(/Saved Chosen\.csv/)).toBeInTheDocument();
    expect(saveFile).toHaveBeenCalledTimes(1);
    expect(saveFile).toHaveBeenCalledWith(golden64, FILE_NAME, 'Mass Updater CSV');
    expect(dialog()).toHaveTextContent('4 adjustments written');
    expect(dialog()).toHaveTextContent('does not mean QAD applied it');
    expect(dialog()).toHaveTextContent(CLEAR_QUESTION);
    expect(within(dialog()).getByRole('button', { name: 'Clear' })).toBeInTheDocument();
    expect(within(dialog()).getByRole('button', { name: 'Keep' })).toBeInTheDocument();
    expect(input('P-1', '2026-09-14').value).toBe('-50'); // nothing is cleared until the user chooses
  });

  it('Clear removes only the exported entries: newer and edited work is kept, Undo restores, saved drafts are untouched', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    const pending = deferred<{ kind: 'saved'; fileName: string }>();
    saveFile.mockReturnValue(pending.promise);
    await renderPanel();
    expect(await screen.findByText(/A saved Planning draft is available/)).toBeInTheDocument();
    stageScenario();
    await openAndPrepare();
    fireEvent.click(saveAsButton());
    expect(within(dialog()).getByRole('button', { name: /^Saving/ })).toHaveAttribute('aria-disabled', 'true'); // one save at a time
    fireEvent.click(within(dialog()).getByRole('button', { name: /^Saving/ }));
    expect(saveFile).toHaveBeenCalledTimes(1);

    // Newer work while the Save As is open: one edited exported entry and one new entry.
    type(input('P-1', '2026-09-21'), '11');
    type(input('P-2', '2026-10-05'), '5');
    expect(within(dialog()).queryByText(/changed after this export was prepared/)).not.toBeInTheDocument(); // the in-flight save outcome is still shown
    await act(async () => { pending.resolve({ kind: 'saved', fileName: 'Chosen.csv' }); await Promise.resolve(); });
    expect(await within(dialog()).findByText(CLEAR_QUESTION)).toBeInTheDocument();

    fireEvent.click(within(dialog()).getByRole('button', { name: 'Clear' }));
    expect(dialog()).toHaveTextContent('Cleared 3 exported adjustments from this session');
    expect(dialog()).toHaveTextContent('Any saved Planning draft is unchanged');
    fireEvent.click(footerClose());

    expect(input('P-1', '2026-09-14').value).toBe('');
    expect(input('P-1', '2026-10-05').value).toBe('');
    expect(input('P-2', '2026-09-28').value).toBe('');
    expect(input('P-1', '2026-09-21').value).toBe('11'); // edited since preparation: kept
    expect(input('P-2', '2026-10-05').value).toBe('5');  // added since preparation: kept
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('2 changes');
    // Saved drafts are never read, written or deleted by exporting or clearing.
    expect(saveDraft).not.toHaveBeenCalled();
    expect(deleteDraft).not.toHaveBeenCalled();
    expect(loadDraft).not.toHaveBeenCalled();
    expect(screen.getByText(/A saved Planning draft is available/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Review saved draft' })).toBeEnabled();

    // Existing undo behavior: one step restores the cleared entries.
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-1', '2026-09-14').value).toBe('-50');
    expect(input('P-2', '2026-09-28').value).toBe('-100');
    expect(input('P-1', '2026-09-21').value).toBe('11'); // the edited entry keeps its newer value
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('5 changes');
  });

  it('Keep, the close button and Escape all leave every staged adjustment in place', async () => {
    saveFile.mockResolvedValue({ kind: 'saved', fileName: 'Chosen.csv' });
    await renderPanel();
    stageScenario();

    // Keep
    await openAndPrepare();
    fireEvent.click(saveAsButton());
    fireEvent.click(await within(dialog()).findByRole('button', { name: 'Keep' }));
    expect(dialog()).toHaveTextContent('The staged adjustments were kept. Any saved Planning draft is unchanged.');
    fireEvent.click(footerClose());
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('not saved'); // exporting a CSV / Keep never marks the draft saved

    // Escape on the prompt dismisses it as Keep
    await openAndPrepare();
    fireEvent.click(saveAsButton());
    await within(dialog()).findByText(CLEAR_QUESTION);
    fireEvent.keyDown(dialog(), { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');

    // The window close control on the prompt dismisses it as Keep
    await openAndPrepare();
    fireEvent.click(saveAsButton());
    await within(dialog()).findByText(CLEAR_QUESTION);
    fireEvent.click(headerClose());
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
    expect(input('P-1', '2026-09-14').value).toBe('-50');
    expect(saveDraft).not.toHaveBeenCalled();
  });

  it('a cancelled Save As writes nothing, shows no prompt, clears nothing and can be retried', async () => {
    saveFile.mockResolvedValueOnce({ kind: 'cancelled' }).mockResolvedValueOnce({ kind: 'saved', fileName: 'Chosen.csv' });
    await renderPanel();
    stageScenario();
    await openAndPrepare();

    fireEvent.click(saveAsButton());

    expect(await within(dialog()).findByText(/Save was cancelled\. No file was written/)).toBeInTheDocument();
    expect(dialog()).not.toHaveTextContent(CLEAR_QUESTION);
    expect(dialog()).not.toHaveTextContent('Saved');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
    expect(previewRows()).toEqual(goldenLines); // still reviewable

    fireEvent.click(saveAsButton());
    expect(await within(dialog()).findByText(CLEAR_QUESTION)).toBeInTheDocument();
    expect(saveFile).toHaveBeenCalledTimes(2);
  });

  it('a failed save is reported without internals, never prompts or clears, and a retry can succeed', async () => {
    saveFile.mockRejectedValueOnce(new Error('EACCES: C:\\Secret\\path denied')).mockResolvedValueOnce({ kind: 'saved', fileName: 'Chosen.csv' });
    await renderPanel();
    stageScenario();
    await openAndPrepare();

    fireEvent.click(saveAsButton());

    const alert = await within(dialog()).findByRole('alert');
    expect(alert).toHaveTextContent('The file could not be saved. No adjustments were cleared');
    expect(alert.textContent).not.toMatch(/EACCES|Secret/);
    expect(dialog()).not.toHaveTextContent(CLEAR_QUESTION);
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');

    fireEvent.click(saveAsButton());
    expect(await within(dialog()).findByText(CLEAR_QUESTION)).toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
  });

  it('a browser download is not a confirmed save: honest wording, no Clear / Keep, nothing cleared, no written claim', async () => {
    saveFile.mockResolvedValue({ kind: 'downloaded', fileName: FILE_NAME });
    await renderPanel();
    stageScenario();
    await openAndPrepare();

    fireEvent.click(saveAsButton());

    expect(await within(dialog()).findByText(/A download of .* was started/)).toBeInTheDocument();
    expect(dialog()).toHaveTextContent('cannot confirm that a file was saved');
    expect(dialog()).toHaveTextContent('every staged adjustment is kept');
    expect(dialog()).not.toHaveTextContent(CLEAR_QUESTION);
    expect(dialog()).not.toHaveTextContent(/adjustments? written/);
    expect(dialog()).not.toHaveTextContent(/Saved /);
    expect(within(dialog()).queryByRole('button', { name: 'Clear' })).not.toBeInTheDocument();
    expect(within(dialog()).queryByRole('button', { name: 'Keep' })).not.toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
    // The only action is Close, which keeps every adjustment.
    expect(within(dialog()).getAllByRole('button').map(b => b.textContent)).toEqual(['\u00d7', 'Close']);
    fireEvent.click(footerClose());
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(input('P-1', '2026-09-14').value).toBe('-50');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
    expect(saveDraft).not.toHaveBeenCalled();
    expect(deleteDraft).not.toHaveBeenCalled();
  });

  it('returns focus to Review and export on close, or to the mode button when Clear leaves nothing to export', async () => {
    saveFile.mockResolvedValue({ kind: 'saved', fileName: 'Chosen.csv' });
    await renderPanel();
    stageScenario();
    exportButton().focus();
    await openAndPrepare();
    fireEvent.click(footerClose());
    expect(exportButton()).toHaveFocus();

    await openAndPrepare();
    fireEvent.click(saveAsButton());
    fireEvent.click(await within(dialog()).findByRole('button', { name: 'Clear' }));
    fireEvent.click(footerClose());
    expect(exportButton()).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Back to report' })).toHaveFocus();
  });
});

describe('Planning export: prepared files are tied to their context', { timeout: 20_000 }, () => {
  beforeEach(() => { exportMock.mockImplementation((_id: string, request: Request) => Promise.resolve(csvFor(request))); });

  it('an adjustment change after preparation makes the file obsolete: Save As is gone, none is initiated, Prepare again works', async () => {
    saveFile.mockResolvedValue({ kind: 'saved', fileName: 'Chosen.csv' });
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    await openAndPrepare();

    type(input('P-1', '2026-09-21'), '10'); // a new adjustment after preparation

    expect(await within(dialog()).findByRole('alert')).toHaveTextContent('changed after this export was prepared, so it was not saved');
    expect(within(dialog()).queryByRole('button', { name: /^Save As/ })).not.toBeInTheDocument();
    expect(saveFile).not.toHaveBeenCalled();

    fireEvent.click(within(dialog()).getByRole('button', { name: 'Prepare again' }));
    await previewTable();
    expect(exportMock).toHaveBeenCalledTimes(2);
    expect(previewRows()).toEqual([['P-1', '-50', '9/14/2026'], ['P-1', '10', '9/21/2026']]);
    fireEvent.click(saveAsButton());
    await waitFor(() => expect(saveFile).toHaveBeenCalledTimes(1));
  });

  it('an MPS snapshot change makes the file obsolete; the new export request names the new snapshot', async () => {
    saveFile.mockResolvedValue({ kind: 'saved', fileName: 'Chosen.csv' });
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    await openAndPrepare();

    showWorkspace(view, ID, 'snap-2');

    expect(await within(dialog()).findByRole('alert')).toHaveTextContent('changed after this export was prepared, so it was not saved');
    expect(within(dialog()).queryByRole('button', { name: /^Save As/ })).not.toBeInTheDocument();
    expect(saveFile).not.toHaveBeenCalled();
    await waitFor(() => expect(within(dialog()).getByRole('button', { name: 'Prepare again' })).toBeEnabled());

    fireEvent.click(within(dialog()).getByRole('button', { name: 'Prepare again' }));
    await previewTable();
    expect(exportMock).toHaveBeenLastCalledWith(ID, { mpsSnapshotId: 'snap-2', site: 'S1', entries: [{ part: 'P-1', weekLabel: '2026-09-14', adjustment: '-50' }] });
  });

  it('a business-week rollover makes the file obsolete (the backend validates against the current week)', async () => {
    vi.useFakeTimers({ toFake: ['Date'] });
    try {
      vi.setSystemTime(new Date(2026, 8, 19, 23, 50));
      await renderPanel();
      type(input('P-1', '2026-09-14'), '-50');
      await openAndPrepare();

      vi.setSystemTime(new Date(2026, 8, 20, 0, 5));
      fireEvent(window, new Event('focus'));

      expect(await within(dialog()).findByRole('alert')).toHaveTextContent('business week changed');
      expect(within(dialog()).queryByRole('button', { name: /^Save As/ })).not.toBeInTheDocument();
      expect(saveFile).not.toHaveBeenCalled();
      expect(refreshWorkbook).not.toHaveBeenCalled();
    } finally { vi.useRealTimers(); }
  });

  it('refreshing Planning data (orders, prices, forecast) does not invalidate the prepared file', async () => {
    saveFile.mockResolvedValue({ kind: 'saved', fileName: 'Chosen.csv' });
    refreshWorkbook.mockImplementation((id: string, snapshot: string, horizon: number) => Promise.resolve({ ...fixtureWorkbook(snapshot, horizon), workspaceId: id, isStale: true, warning: 'stale' }));
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    await openAndPrepare();

    // Workbook staleness never blocks export (ledger section 8.6): a refresh leaves the prepared file valid.
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Planning' }));
    await waitFor(() => expect(refreshWorkbook).toHaveBeenCalledTimes(1));

    expect(saveAsButton()).toBeEnabled();
    fireEvent.click(saveAsButton());
    await waitFor(() => expect(saveFile).toHaveBeenCalledTimes(1));
  });

  it('a late preparation response from a previous workspace session is ignored (A → B → A)', async () => {
    const late = deferred<ReturnType<typeof csvFor>>();
    exportMock.mockReturnValueOnce(late.promise);
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    fireEvent.click(exportButton());
    expect(await within(dialog()).findByText(/Validating the staged adjustments/)).toBeInTheDocument();

    await leaveAndReturn(view);
    type(input('P-2', '2026-09-28'), '-7'); // this session's own work
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); // the earlier session's export is gone

    await act(async () => { late.resolve(csvFor({ mpsSnapshotId: SNAP, site: 'S1', entries: [{ part: 'P-1', weekLabel: '2026-09-14', adjustment: '-50' }] })); await Promise.resolve(); });

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(input('P-2', '2026-09-28').value).toBe('-7');
    expect(exportMock).toHaveBeenCalledTimes(1);
    expect(saveFile).not.toHaveBeenCalled();
  });

  it('a late Save As result from a previous workspace session neither prompts nor clears the current session (A → B → A)', async () => {
    const lateSave = deferred<{ kind: 'saved'; fileName: string }>();
    saveFile.mockReturnValue(lateSave.promise);
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    await openAndPrepare();
    fireEvent.click(saveAsButton());
    expect(saveFile).toHaveBeenCalledTimes(1);

    await leaveAndReturn(view);
    type(input('P-1', '2026-09-14'), '-50'); // identical text in the new session must still survive
    await act(async () => { lateSave.resolve({ kind: 'saved', fileName: 'Chosen.csv' }); await Promise.resolve(); });

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.queryByText(CLEAR_QUESTION)).not.toBeInTheDocument();
    expect(input('P-1', '2026-09-14').value).toBe('-50');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('1 change');
  });
});

describe('Planning export: Clear keeps later work, even when its value equals the exported value', { timeout: 20_000 }, () => {
  beforeEach(() => { exportMock.mockImplementation((_id: string, request: Request) => Promise.resolve(csvFor(request))); });

  /** Stages three adjustments, prepares and starts a Save As that stays pending until `finish` is called. */
  async function pendingSave() {
    const pending = deferred<{ kind: 'saved'; fileName: string }>();
    saveFile.mockReturnValue(pending.promise);
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    type(input('P-1', '2026-09-21'), '10');
    type(input('P-2', '2026-09-28'), '10');
    await openAndPrepare();
    fireEvent.click(saveAsButton());
    const finish = async () => {
      await act(async () => { pending.resolve({ kind: 'saved', fileName: 'Chosen.csv' }); await Promise.resolve(); });
      await within(dialog()).findByText(CLEAR_QUESTION);
    };
    return { finish };
  }

  it('an entry edited 10 -> 20 -> 10 while Save As is pending survives Clear', async () => {
    const { finish } = await pendingSave();
    type(input('P-1', '2026-09-21'), '20');
    type(input('P-1', '2026-09-21'), '10'); // back to the exported text: still later work
    await finish();

    fireEvent.click(within(dialog()).getByRole('button', { name: 'Clear' }));

    expect(dialog()).toHaveTextContent('Cleared 2 exported adjustments');
    fireEvent.click(footerClose());
    expect(input('P-1', '2026-09-21').value).toBe('10');
    expect(input('P-1', '2026-09-14').value).toBe('');
    expect(input('P-2', '2026-09-28').value).toBe('');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('1 change');
  });

  it('an entry removed and entered again as the same value survives Clear', async () => {
    const { finish } = await pendingSave();
    type(input('P-2', '2026-09-28'), '');   // removed
    type(input('P-2', '2026-09-28'), '10'); // entered again
    await finish();

    fireEvent.click(within(dialog()).getByRole('button', { name: 'Clear' }));

    expect(dialog()).toHaveTextContent('Cleared 2 exported adjustments');
    fireEvent.click(footerClose());
    expect(input('P-2', '2026-09-28').value).toBe('10');
    expect(input('P-1', '2026-09-14').value).toBe('');
    expect(input('P-1', '2026-09-21').value).toBe('');
  });

  it('untouched entries still clear, Undo restores them without disturbing later work, and saved drafts are untouched', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    const { finish } = await pendingSave();
    type(input('P-1', '2026-09-21'), '20');
    type(input('P-1', '2026-09-21'), '10'); // later work, same text
    await finish();

    fireEvent.click(within(dialog()).getByRole('button', { name: 'Clear' }));
    fireEvent.click(footerClose());

    expect(input('P-1', '2026-09-14').value).toBe('');   // untouched exported entries are cleared
    expect(input('P-2', '2026-09-28').value).toBe('');
    expect(input('P-1', '2026-09-21').value).toBe('10'); // later work stays
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-1', '2026-09-14').value).toBe('-50');
    expect(input('P-2', '2026-09-28').value).toBe('10');
    expect(input('P-1', '2026-09-21').value).toBe('10');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('3 changes');
    expect(saveDraft).not.toHaveBeenCalled();
    expect(deleteDraft).not.toHaveBeenCalled();
    expect(loadDraft).not.toHaveBeenCalled();
    expect(screen.getByText(/A saved Planning draft is available/)).toBeInTheDocument();
  });
});

describe('Planning export: keyboard focus stays inside the dialog through every action change', { timeout: 20_000 }, () => {
  beforeEach(() => { exportMock.mockImplementation((_id: string, request: Request) => Promise.resolve(csvFor(request))); });
  const insideDialog = () => dialog().contains(document.activeElement) && document.activeElement !== dialog();
  const focused = () => (document.activeElement as HTMLElement | null);
  /** Focus moves after asynchronous transitions, so assert it with retries. */
  const expectFocus = (element: () => HTMLElement) => waitFor(() => expect(focused()).toBe(element()));
  const focusAndClick = (button: HTMLElement) => { button.focus(); fireEvent.click(button); };

  it('keeps focus on a usable control while saving, then moves it to Keep; Keep and Clear land on Close', async () => {
    const pending = deferred<{ kind: 'saved'; fileName: string }>();
    saveFile.mockReturnValue(pending.promise);
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    await openAndPrepare();
    await expectFocus(saveAsButton);                  // opens on the primary action

    focusAndClick(saveAsButton());                    // Save As is now in progress
    expect(insideDialog()).toBe(true);
    await expectFocus(() => within(dialog()).getByRole('button', { name: /^Saving/ }));

    await act(async () => { pending.resolve({ kind: 'saved', fileName: 'Chosen.csv' }); await Promise.resolve(); });
    await within(dialog()).findByText(CLEAR_QUESTION);
    await expectFocus(() => within(dialog()).getByRole('button', { name: 'Keep' })); // Keep is preferred, never Clear

    // Tab containment: Tab from the last control wraps to the first; Shift+Tab from the first wraps to the last.
    const clearButton = within(dialog()).getByRole('button', { name: 'Clear' });
    clearButton.focus();
    fireEvent.keyDown(clearButton, { key: 'Tab' });
    expect(focused()).toBe(headerClose());
    fireEvent.keyDown(headerClose(), { key: 'Tab', shiftKey: true });
    expect(focused()).toBe(clearButton);

    focusAndClick(within(dialog()).getByRole('button', { name: 'Keep' }));
    expect(insideDialog()).toBe(true);
    await expectFocus(footerClose);
    expect(dialog()).toHaveTextContent('The staged adjustments were kept');
  });

  it('after Clear, focus lands on Close inside the dialog (the focused Clear button is gone)', async () => {
    saveFile.mockResolvedValue({ kind: 'saved', fileName: 'Chosen.csv' });
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    await openAndPrepare();
    fireEvent.click(saveAsButton());
    await within(dialog()).findByText(CLEAR_QUESTION);

    focusAndClick(within(dialog()).getByRole('button', { name: 'Clear' }));

    expect(dialog()).toHaveTextContent('Cleared 1 exported adjustment');
    await expectFocus(footerClose);
    expect(insideDialog()).toBe(true);
  });

  it('keeps focus on Save As again after a cancelled or failed save, and Escape still closes', async () => {
    saveFile.mockResolvedValueOnce({ kind: 'cancelled' }).mockRejectedValueOnce(new Error('denied')).mockResolvedValue({ kind: 'saved', fileName: 'Chosen.csv' });
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    await openAndPrepare();

    focusAndClick(saveAsButton());
    await within(dialog()).findByText(/Save was cancelled/);
    await expectFocus(saveAsButton);
    expect(insideDialog()).toBe(true);

    focusAndClick(saveAsButton());
    await within(dialog()).findByText(/could not be saved/);
    await expectFocus(saveAsButton);
    expect(insideDialog()).toBe(true);

    fireEvent.keyDown(dialog(), { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('moves focus to Prepare again when the prepared file becomes obsolete, and to a usable control while preparing', async () => {
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    await openAndPrepare();
    await expectFocus(saveAsButton);

    type(input('P-1', '2026-09-21'), '10'); // the file is now obsolete: Save As disappears
    const again = await within(dialog()).findByRole('button', { name: 'Prepare again' });
    await expectFocus(() => again);
    expect(insideDialog()).toBe(true);

    const late = deferred<ReturnType<typeof csvFor>>();
    exportMock.mockReturnValueOnce(late.promise);
    focusAndClick(again);                              // preparing: Prepare again is removed
    expect(await within(dialog()).findByText(/Validating the staged adjustments/)).toBeInTheDocument();
    await expectFocus(headerClose);                    // first usable control while preparing
    expect(insideDialog()).toBe(true);
    await act(async () => { late.resolve(csvFor({ mpsSnapshotId: SNAP, site: 'S1', entries: [
      { part: 'P-1', weekLabel: '2026-09-14', adjustment: '-50' }, { part: 'P-1', weekLabel: '2026-09-21', adjustment: '10' }] })); await Promise.resolve(); });
    await previewTable();
    await expectFocus(saveAsButton);
    expect(insideDialog()).toBe(true);
  });

  it('a browser download leaves focus on Close inside the dialog', async () => {
    saveFile.mockResolvedValue({ kind: 'downloaded', fileName: FILE_NAME });
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    await openAndPrepare();

    focusAndClick(saveAsButton());

    await within(dialog()).findByText(/was started/);
    await expectFocus(footerClose);
  });
});
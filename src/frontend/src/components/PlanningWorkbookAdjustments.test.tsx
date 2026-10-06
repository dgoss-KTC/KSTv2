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
 * Stage 14.5 Planning adjustment staging and saved drafts (panel level). All API traffic is mocked;
 * no MPS, Open Orders or QAD call is possible. Workbook values come from the accepted Stage 14 fixture,
 * padded to the 12-week minimum Planning horizon.
 */
const getWorkbook = vi.fn();
const refreshWorkbook = vi.fn();
const draftPresence = vi.fn();
const loadDraft = vi.fn();
const saveDraft = vi.fn();
const deleteDraft = vi.fn();
vi.mock('../api/tauri-bridge', () => ({ resolveBackendBaseUrl: () => Promise.resolve('http://localhost'), isRunningInTauri: () => false }));
vi.mock('../api/client', async (original) => ({ ...(await original<typeof import('../api/client')>()),
  ApiClient: class {
    getPlanningWorkbook = getWorkbook; refreshPlanningWorkbook = refreshWorkbook;
    getPlanningWorkbookDraftPresence = draftPresence; loadPlanningWorkbookDraft = loadDraft;
    savePlanningWorkbookDraft = saveDraft; deletePlanningWorkbookDraft = deleteDraft;
  },
}));

type Fixture = Stage14Fixture & { draftRestoreCases: { cases: { id: string; saved?: { part: string; weekLabel: string; originalMpsQty: string; adjustment: string }; currentMps?: string; baselineChanged?: boolean; proposedMps?: string; issueCode?: string; before?: { part: string; weekLabel: string; mps: string; adjustment: string; proposedMps: string }; afterMpsRefresh?: { mps: string; proposedMps: string } }[] } };
const fixture = JSON.parse(readFileSync(resolve(process.cwd(), '../../docs/reference/STAGE_14_PLANNING_WORKBOOK_DETERMINISTIC_FIXTURES.json'), 'utf8')) as Fixture;
const ID = fixture.scope.workspaceAssignmentId;
const SNAP = fixture.scope.mpsSnapshotId;

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
      mpsDollars: p.isPriced ? '0' : null, forecast: '0',
    }) })),
  };
}
function setMps(workbook: PlanningWorkbook, part: string, weekLabel: string, mps: string): PlanningWorkbook {
  return { ...workbook, parts: workbook.parts.map(p => p.part !== part ? p : { ...p, weeks: p.weeks.map(w => w.weekLabel === weekLabel ? { ...w, mps } : w) }) };
}

const problem = (status: number, body: object) => new ApiError(status, 'http://localhost/x', JSON.stringify(body));
function deferred<T>() {
  let resolveFn!: (value: T) => void; let rejectFn!: (error: unknown) => void;
  const promise = new Promise<T>((res, rej) => { resolveFn = res; rejectFn = rej; });
  return { promise, resolve: resolveFn, reject: rejectFn };
}

const input = (part: string, weekIso: string) => {
  const [y, m, d] = weekIso.split('-').map(Number);
  return screen.getByRole('textbox', { name: `Adjustment for ${part}, week of ${m}/${d}/${y}` }) as HTMLInputElement;
};
const partRow = (part: string, metric: RegExp) => within(screen.getByRole('rowgroup', { name: `Part ${part}` })).getByRole('row', { name: metric });
const cells = (row: HTMLElement) => within(row).getAllByRole('cell');
/** The displayed value of a cell (a changed cell also shows the loaded value struck through). */
const val = (cell: HTMLElement) => cell.querySelector('.pwb-v')?.textContent ?? cell.textContent;
/** Cell order: Falldown, then weeks with a quarter column after each quarter (fixture: 2 Q1 weeks, Q1 total, ...). */
const WEEK_CELL = [1, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13];
const type = (el: HTMLInputElement, value: string) => fireEvent.change(el, { target: { value } });
/** Shows every metric row and every workspace total for this assignment (View tailoring, per assignment). */
const showAllRows = (assignmentId = ID) => window.localStorage.setItem(`kst.planningWorkbook.view.v1.${assignmentId}`,
  JSON.stringify({ hiddenModels: [], rows: ['so', 'soDollars', 'mps', 'mpsDollars', 'forecast', 'onHand'], totals: ['so', 'soDollars', 'mps', 'mpsDollars', 'adjust'], collapsed: [] }));
/** Planning opens in report mode; Start/Resume planning switches locally without a request. */
const startPlanning = () => fireEvent.click(screen.getByRole('button', { name: /^(Start|Resume) planning/ }));
const setHorizon = async (from: number, to: number) => {
  for (let h = from; h !== to; h += to > from ? 1 : -1) {
    fireEvent.click(screen.getByRole('button', { name: to > from ? 'More weeks' : 'Fewer weeks' }));
  }
};

async function renderPanel(props: Partial<{ snapshotId: string; assignmentId: string }> = {}) {
  const view = render(<PlanningWorkbookPanel assignmentId={props.assignmentId ?? ID} site="S1" snapshotId={props.snapshotId ?? SNAP} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
  await screen.findByRole('region', { name: 'Planning workbook grid' });
  startPlanning();
  return view;
}

beforeEach(() => {
  for (const mock of [getWorkbook, refreshWorkbook, draftPresence, loadDraft, saveDraft, deleteDraft]) mock.mockReset();
  window.localStorage.clear();
  showAllRows();
  getWorkbook.mockImplementation((_id: string, snapshot: string, horizon: number) => Promise.resolve(fixtureWorkbook(snapshot, horizon)));
  draftPresence.mockResolvedValue({ exists: false });
});
afterEach(() => vi.restoreAllMocks());

describe('Planning adjustments: editing and derived rows', { timeout: 20_000 }, () => {
  it('merges proposed MPS, adjusted MPS $ and adjusted inventory into the MPS, MPS $ and Projected on hand rows (fixture scenario)', async () => {
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    type(input('P-1', '2026-09-21'), '10');
    type(input('P-1', '2026-10-05'), '20');
    type(input('P-2', '2026-09-28'), '-100');

    const proposed = cells(partRow('P-1', /^MPS (?!\$)/));
    expect([0, 1, 2, 3].map(i => val(proposed[WEEK_CELL[i]]))).toEqual(['50', '85.5', '100', '20']);
    // Changed cells show the loaded value struck through; unchanged cells show only the loaded value.
    expect(proposed[WEEK_CELL[0]]).toHaveClass('pwb-cell--changed');
    expect(proposed[WEEK_CELL[0]].querySelector('s')).toHaveTextContent('100');
    expect(proposed[WEEK_CELL[0]]).toHaveAttribute('title', expect.stringContaining('loaded 100'));
    expect(proposed[WEEK_CELL[2]]).not.toHaveClass('pwb-cell--changed');
    expect(val(proposed[0])).toBe('30'); // Falldown proposed supply equals existing supply
    const dollars = cells(partRow('P-1', /^MPS \$/));
    expect([0, 1, 2, 3].map(i => val(dollars[WEEK_CELL[i]]))).toEqual(['$125.00', '$213.75', '$250.00', '$50.00']);
    const adjOnHand = cells(partRow('P-1', /^Projected on hand/));
    expect([0, 1, 2, 3].map(i => val(adjOnHand[WEEK_CELL[i]]))).toEqual(['30', '45.5', '145.5', '165.5']);
    expect(val(adjOnHand[0])).toBe('60');
    // Quarter columns: Q1 adjustment = -50 + 10 (Falldown has none); projected on hand is the ending balance.
    expect(cells(partRow('P-1', /^Adjust/))[3]).toHaveTextContent('-40');
    expect(val(adjOnHand[3])).toBe('45.5');
    // P-2 zero price: a staged change that brings MPS to zero shows 0 explicitly; unchanged zero dollars stay blank.
    const p2Proposed = cells(partRow('P-2', /^MPS (?!\$)/));
    expect(val(p2Proposed[WEEK_CELL[2]])).toBe('0');
    expect(val(cells(partRow('P-2', /^MPS \$/))[WEEK_CELL[2]])).toBe('');
    const p2AdjOnHand = cells(partRow('P-2', /^Projected on hand/))[WEEK_CELL[2]];
    expect(val(p2AdjOnHand)).toBe('-50');
    expect(p2AdjOnHand).toHaveClass('pwb-cell--negative');
    expect(p2AdjOnHand).not.toHaveClass('pwb-cell--invalid'); // negative inventory never blocks
    // P-3 unpriced: dollars show the unpriced indicator; workspace MPS $ totals stay blank.
    expect(cells(partRow('P-3', /^MPS \$/)).every(c => c.textContent === '\u2014')).toBe(true);
    const totals = within(screen.getByRole('rowgroup', { name: 'Workspace totals' }));
    expect(cells(totals.getByRole('row', { name: /^Total MPS \$/ })).every(c => val(c) === '')).toBe(true);
    expect(cells(totals.getByRole('row', { name: /^Total adjust/ }))[WEEK_CELL[2]]).toHaveTextContent('-100');
    expect(val(cells(totals.getByRole('row', { name: /^Total MPS (?!\$)/ }))[WEEK_CELL[0]])).toBe('50');
    expect(totals.queryByRole('row', { name: /on hand/i })).not.toBeInTheDocument();
    // Neutral rows with small colour keys (no row bands).
    expect(partRow('P-1', /^Adjust/).querySelector('.pwb-key--adjust')).not.toBeNull();
    expect(partRow('P-1', /^MPS (?!\$)/).querySelector('.pwb-key--supply')).not.toBeNull();
    expect(partRow('P-1', /^Projected on hand/).querySelector('.pwb-key--onHand')).not.toBeNull();
    expect(partRow('P-1', /^MPS (?!\$)/).className).not.toMatch(/pwb-row--(supply|demand|neutral)/);

    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
    expect(screen.getByTestId('planning-proposal-validity')).toHaveAttribute('data-valid', 'true');
    expect(getWorkbook).toHaveBeenCalledTimes(1); // edits never request data
  });

  it('keeps partial input, explains invalid syntax, and lets the user correct or remove it', async () => {
    await renderPanel();
    const cell = input('P-1', '2026-09-21');
    type(cell, '-');
    expect(cell.value).toBe('-');
    expect(cell).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByTestId('planning-proposal-validity')).toHaveAttribute('data-valid', 'false');
    const review = screen.getByRole('list', { name: 'Staged changes' });
    expect(review).toHaveTextContent('Invalid number');
    for (const bad of ['+5', '10.5', '1e3', '1,000', ' 5']) {
      type(cell, bad);
      expect(cell.value).toBe(bad);
      expect(cell).toHaveAttribute('aria-invalid', 'true');
    }
    // Invalid text is never applied to the MPS row.
    expect(val(cells(partRow('P-1', /^MPS (?!\$)/))[WEEK_CELL[1]])).toBe('75.5');
    type(cell, '-75');
    expect(cell).not.toHaveAttribute('aria-invalid');
    expect(val(cells(partRow('P-1', /^MPS (?!\$)/))[WEEK_CELL[1]])).toBe('0.5'); // fractional source kept exact
    type(cell, '-76');
    expect(screen.getByRole('list', { name: 'Staged changes' })).toHaveTextContent('Negative Proposed MPS');
    expect(cells(partRow('P-1', /^MPS (?!\$)/))[WEEK_CELL[1]]).toHaveClass('pwb-cell--invalid');
    fireEvent.click(screen.getByRole('button', { name: 'Remove adjustment for P-1, week of 9/21/2026' }));
    expect(cell.value).toBe('');
    expect(screen.queryByTestId('planning-proposal-validity')).not.toBeInTheDocument();
  });

  it('accepts a zero resulting MPS (shown as 0), treats 0 and -0 as no-ops, and supports undo and confirmed Discard all', async () => {
    await renderPanel();
    type(input('P-1', '2026-09-14'), '-100');
    expect(val(cells(partRow('P-1', /^MPS (?!\$)/))[WEEK_CELL[0]])).toBe('0');
    expect(screen.getByTestId('planning-proposal-validity')).toHaveAttribute('data-valid', 'true');
    type(input('P-2', '2026-09-14'), '0');
    type(input('P-3', '2026-09-14'), '-0');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('1 change');

    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-3', '2026-09-14').value).toBe('');
    fireEvent.click(screen.getByRole('button', { name: 'Discard all' }));
    fireEvent.click(within(screen.getByRole('group', { name: 'Confirm Discard all' })).getByRole('button', { name: 'Cancel' }));
    expect(input('P-1', '2026-09-14').value).toBe('-100');
    fireEvent.click(screen.getByRole('button', { name: 'Discard all' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirm Discard all' }));
    expect(input('P-1', '2026-09-14').value).toBe('');
    expect(input('P-2', '2026-09-14').value).toBe('');
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-1', '2026-09-14').value).toBe('-100');
  });

  it('keeps entries hidden by a smaller horizon listed, unchecked and not blocking (owner decision), and shows them again when widened', async () => {
    await renderPanel();
    await setHorizon(12, 14);
    await waitFor(() => expect(getWorkbook).toHaveBeenLastCalledWith(ID, SNAP, 14));
    type(await waitFor(() => input('P-1', '2026-12-14')), '25');
    await setHorizon(14, 12);
    await waitFor(() => expect(screen.queryByRole('textbox', { name: /week of 12\/14\/2026/ })).not.toBeInTheDocument());
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    const review = screen.getByRole('list', { name: 'Staged changes' });
    expect(review).toHaveTextContent('Week of Dec 14');
    expect(review).toHaveTextContent('Not shown at the current horizon');
    expect(review).not.toHaveTextContent(/MPS \d/); // no baseline is claimed for an unseen week
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('1 change');
    expect(screen.getByTestId('planning-change-count')).not.toHaveTextContent('need attention');
    const validity = screen.getByTestId('planning-proposal-validity');
    expect(validity).toHaveAttribute('data-valid', 'true');
    expect(validity).toHaveTextContent('not checked here; export validates every change against the full loaded MPS');
    await setHorizon(12, 14);
    await waitFor(() => expect(input('P-1', '2026-12-14').value).toBe('25'));
    expect(screen.getByRole('list', { name: 'Staged changes' })).not.toHaveTextContent('Not shown at the current horizon');
  });
});

describe('Planning adjustments: MPS snapshot replacement and rollover', { timeout: 20_000 }, () => {
  it('D06: reapplies signed adjustments to a replaced snapshot, flags newly invalid entries, no acknowledgment step', async () => {
    const d06 = fixture.draftRestoreCases.cases.find(c => c.id === 'D06')!;
    const before = d06.before!;
    const view = await renderPanel();
    type(input(before.part, before.weekLabel), before.adjustment);
    type(input('P-2', '2026-09-28'), '-100');
    expect(val(cells(partRow(before.part, /^MPS (?!\$)/))[WEEK_CELL[0]])).toBe(before.proposedMps);

    const replaced = deferred<PlanningWorkbook>();
    getWorkbook.mockReturnValueOnce(replaced.promise);
    view.rerender(<PlanningWorkbookPanel assignmentId={ID} site="S1" snapshotId="snap-2" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    // While the new projection loads, the old snapshot's workbook is not shown but entries are kept.
    expect(screen.queryByRole('region', { name: 'Planning workbook grid' })).not.toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('2 changes');
    await act(async () => {
      replaced.resolve(setMps(setMps(fixtureWorkbook('snap-2'), before.part, before.weekLabel, d06.afterMpsRefresh!.mps), 'P-2', '2026-09-28', '60'));
      await Promise.resolve();
    });
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    expect(input(before.part, before.weekLabel).value).toBe(before.adjustment);
    expect(val(cells(partRow(before.part, /^MPS (?!\$)/))[WEEK_CELL[0]])).toBe(d06.afterMpsRefresh!.proposedMps);
    expect(input('P-2', '2026-09-28').value).toBe('-100');
    expect(screen.getByRole('list', { name: 'Staged changes' })).toHaveTextContent('Negative Proposed MPS');
    expect(screen.queryByRole('button', { name: /acknowledge/i })).not.toBeInTheDocument();
    expect(refreshWorkbook).not.toHaveBeenCalled();
  });

  it('a late response for the previous snapshot does not overwrite the current evaluation', async () => {
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    const late = deferred<PlanningWorkbook>();
    getWorkbook.mockReset();
    getWorkbook.mockImplementation((_id: string, snapshot: string) => (snapshot === 'snap-old' ? late.promise : Promise.resolve(setMps(fixtureWorkbook(snapshot), 'P-1', '2026-09-14', '120'))));
    view.rerender(<PlanningWorkbookPanel assignmentId={ID} site="S1" snapshotId="snap-old" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    view.rerender(<PlanningWorkbookPanel assignmentId={ID} site="S1" snapshotId="snap-new" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await waitFor(() => expect(val(cells(partRow('P-1', /^MPS (?!\$)/))[WEEK_CELL[0]])).toBe('70'));
    await act(async () => { late.resolve(fixtureWorkbook('snap-old')); await Promise.resolve(); });
    expect(val(cells(partRow('P-1', /^MPS (?!\$)/))[WEEK_CELL[0]])).toBe('70');
  });
});

describe('Planning saved drafts', { timeout: 20_000 }, () => {
  const draftRows = () => fixture.draftRestoreCases.cases.filter(c => c.saved).map(c => ({
    part: c.saved!.part, weekLabel: c.saved!.weekLabel, adjustment: c.saved!.adjustment, savedMpsQuantity: c.saved!.originalMpsQty,
    currentMps: c.currentMps ?? null, baselineChanged: c.baselineChanged ?? false, proposedMps: c.proposedMps ?? null, issueCode: c.issueCode ?? null,
  }));
  const draft = (rows = draftRows()) => ({ exists: true, isCorrupt: false, warning: null, savedAtUtc: '2026-09-15T20:00:00Z', savedMpsSnapshotId: 'older', savedSite: 'S1', rows });

  it('D01â€“D05: shows saved versus current baselines before restore, cancel keeps current proposals, confirm restores flagged entries', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    loadDraft.mockResolvedValue(draft());
    await renderPanel();
    expect(await screen.findByText(/A saved Planning draft is available/)).toBeInTheDocument();
    type(input('P-3', '2026-09-14'), '7');

    fireEvent.click(screen.getByRole('button', { name: 'Review saved draft' }));
    const table = await screen.findByRole('table', { name: 'Saved draft comparison' });
    // The comparison is a modal dialog; focus moves to the explicit restore action.
    expect(screen.getByRole('dialog', { name: 'Saved draft' })).toContainElement(table);
    expect(document.activeElement).toHaveTextContent('Restore saved draft');
    expect(loadDraft).toHaveBeenCalledWith(ID, SNAP);
    const rows = within(table).getAllByRole('row').slice(1).map(r => within(r).getAllByRole('cell').map(c => c.textContent));
    expect(rows[0].slice(0, 6)).toEqual(['P-1', '9/21/2026', '10', '75.5', '75.5', '85.5']);
    expect(rows[1].slice(3, 6)).toEqual(['80', '100 (changed)', '50']);
    expect(rows[2][6]).toContain('Negative Proposed MPS');
    expect(rows[3].slice(4, 7)).toEqual(['Unavailable', 'Unavailable', expect.stringContaining('Week passed')]);
    expect(rows[4].slice(3, 7)).toEqual(['0', 'Unavailable', 'Unavailable', expect.stringContaining('Part not in loaded MPS')]); // saved zero vs unavailable

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.queryByRole('dialog', { name: 'Saved draft' })).not.toBeInTheDocument();
    expect(input('P-3', '2026-09-14').value).toBe('7');
    expect(input('P-1', '2026-09-21').value).toBe('');

    fireEvent.click(screen.getByRole('button', { name: 'Review saved draft' }));
    await screen.findByRole('table', { name: 'Saved draft comparison' });
    const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true);
    fireEvent.click(screen.getByRole('button', { name: 'Restore saved draft' }));
    expect(input('P-3', '2026-09-14').value).toBe('7'); // declined replacement keeps current proposals
    fireEvent.click(screen.getByRole('button', { name: 'Restore saved draft' }));
    expect(confirm).toHaveBeenCalledTimes(2);
    expect(input('P-3', '2026-09-14').value).toBe('');
    expect(input('P-1', '2026-09-21').value).toBe('10');
    expect(input('P-1', '2026-09-28').value).toBe('-50');
    expect(val(cells(partRow('P-1', /^MPS (?!\$)/))[WEEK_CELL[2]])).toBe('50'); // D02: signed, not target 30
    expect(val(cells(partRow('P-2', /^MPS (?!\$)/))[WEEK_CELL[2]])).toBe('-20'); // D03 retained, flagged
    const review = screen.getByRole('list', { name: 'Staged changes' });
    expect(review).toHaveTextContent('Week passed');
    expect(review).toHaveTextContent('Part not in loaded MPS');
    expect(review).toHaveTextContent('X-9');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('5 changes \u00b7 3 need attention \u00b7 draft saved');
    expect(screen.getByText(/Nothing has been applied to QAD/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-3', '2026-09-14').value).toBe('7');
  });

  it('a failed draft read or a corrupt draft keeps current proposals and blocks saving over the corrupt file', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    loadDraft.mockRejectedValueOnce(problem(503, {})).mockResolvedValueOnce({ exists: true, isCorrupt: true, warning: 'kept', savedAtUtc: null, savedMpsSnapshotId: null, savedSite: null, rows: [] });
    await renderPanel();
    type(input('P-1', '2026-09-14'), '5');
    fireEvent.click(await screen.findByRole('button', { name: 'Review saved draft' }));
    expect(await screen.findByText(/The saved draft could not be read. Your current adjustments are unchanged/)).toBeInTheDocument();
    expect(input('P-1', '2026-09-14').value).toBe('5');
    fireEvent.click(screen.getByRole('button', { name: 'Review saved draft' }));
    expect(await screen.findByText(/kept on disk for recovery/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save draft' })).toBeDisabled();
    expect(input('P-1', '2026-09-14').value).toBe('5');
    expect(saveDraft).not.toHaveBeenCalled();
  });

  it('saves only proposals (no-ops excluded, flagged entries kept), and edits made during the save stay unsaved', async () => {
    const pending = deferred<object>();
    saveDraft.mockReturnValueOnce(pending.promise);
    await renderPanel();
    // Nothing to save: an empty session can never overwrite a saved draft.
    expect(screen.getByRole('button', { name: 'Save draft' })).toBeDisabled();
    type(input('P-1', '2026-09-14'), '-50');
    type(input('P-2', '2026-09-28'), '-120');
    type(input('P-3', '2026-09-14'), '0');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    await waitFor(() => expect(saveDraft).toHaveBeenCalledWith(ID, { mpsSnapshotId: SNAP, entries: [
      { part: 'P-1', weekLabel: '2026-09-14', adjustment: '-50' },
      { part: 'P-2', weekLabel: '2026-09-28', adjustment: '-120' },
    ] }));
    type(input('P-1', '2026-09-21'), '3'); // newer edit while saving
    await act(async () => { pending.resolve(draft([])); await Promise.resolve(); });
    expect(screen.getByText('Draft saved; newer changes are not saved yet.')).toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('not saved');

    saveDraft.mockResolvedValueOnce(draft([]));
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    expect(await screen.findByText('Draft saved. Nothing has been applied to QAD.')).toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('draft saved');
  });

  it('save failures and invalid input keep the session; delete removes only the saved copy', async () => {
    saveDraft.mockRejectedValueOnce(problem(503, {})).mockRejectedValueOnce(problem(409, { code: 'draft-corrupt' }));
    await renderPanel();
    type(input('P-1', '2026-09-14'), '1.5');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    expect(screen.getByText('Correct or remove invalid adjustments before saving the draft.')).toBeInTheDocument();
    expect(saveDraft).not.toHaveBeenCalled();
    type(input('P-1', '2026-09-14'), '2');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    expect(await screen.findByText(/Save Draft failed. Your adjustments remain/)).toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('not saved');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    expect(await screen.findByText(/kept on disk for recovery/)).toBeInTheDocument();
    expect(input('P-1', '2026-09-14').value).toBe('2');

    deleteDraft.mockResolvedValueOnce(undefined);
    const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true);
    fireEvent.click(screen.getByRole('button', { name: 'Delete saved draft' }));
    expect(deleteDraft).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Delete saved draft' }));
    expect(await screen.findByText(/Saved draft deleted. Adjustments in this session were kept/)).toBeInTheDocument();
    expect(confirm).toHaveBeenCalledTimes(2);
    expect(deleteDraft).toHaveBeenCalledWith(ID);
    expect(input('P-1', '2026-09-14').value).toBe('2');
    expect(screen.getByRole('button', { name: 'Save draft' })).toBeEnabled();
  });

  it('isolates workspaces: a late save or review for the previous assignment never affects the new one', async () => {
    const pendingSave = deferred<object>();
    saveDraft.mockReturnValueOnce(pendingSave.promise);
    const OTHER = '00000000-0000-0000-0000-00000000aaaa';
    getWorkbook.mockImplementation((id: string, snapshot: string, horizon: number) => Promise.resolve({ ...fixtureWorkbook(snapshot, horizon), workspaceId: id }));
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    view.rerender(<PlanningWorkbookPanel assignmentId={OTHER} site="S1" snapshotId={SNAP} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    // Another workspace opens in report mode with no staged changes.
    await waitFor(() => expect(screen.getByRole('button', { name: 'Start planning' })).toBeInTheDocument());
    startPlanning();
    await waitFor(() => expect(input('P-1', '2026-09-14').value).toBe(''));
    await act(async () => { pendingSave.resolve(draft([])); await Promise.resolve(); });
    expect(screen.queryByText(/Draft saved/)).not.toBeInTheDocument();
    expect(input('P-1', '2026-09-14').value).toBe('');
    expect(draftPresence).toHaveBeenLastCalledWith(OTHER);
  });

  /* ---- 14.5 review corrections: obsolete reviews, workspace sessions ---- */

  const OTHER_ID = '00000000-0000-0000-0000-00000000bbbb';
  const showWorkspace = (view: ReturnType<typeof render>, assignmentId: string) =>
    view.rerender(<PlanningWorkbookPanel assignmentId={assignmentId} site="S1" snapshotId={SNAP} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
  /** A → B → A: leaves the workspace and returns, which starts a new workspace session for A. */
  async function leaveAndReturn(view: ReturnType<typeof render>) {
    showWorkspace(view, OTHER_ID);
    await waitFor(() => expect(draftPresence).toHaveBeenLastCalledWith(OTHER_ID));
    showWorkspace(view, ID);
    await waitFor(() => expect(draftPresence).toHaveBeenLastCalledWith(ID));
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    startPlanning();
    await waitFor(() => expect(input('P-1', '2026-09-14').value).toBe(''));
  }

  it('saving newer contents invalidates an open review and a pending review; restore is then unavailable', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    loadDraft.mockResolvedValueOnce(draft());
    await renderPanel();
    type(input('P-3', '2026-09-14'), '7');
    fireEvent.click(await screen.findByRole('button', { name: 'Review saved draft' }));
    await screen.findByRole('table', { name: 'Saved draft comparison' });

    // Newer contents are saved while the review of the older draft is open.
    type(input('P-1', '2026-09-14'), '-5');
    saveDraft.mockResolvedValueOnce(draft([]));
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    expect(await screen.findByText('Draft saved. Nothing has been applied to QAD.')).toBeInTheDocument();
    expect(screen.queryByRole('table', { name: 'Saved draft comparison' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Restore saved draft' })).not.toBeInTheDocument();

    // A review requested before another save completes after it: its older contents are dropped.
    const pendingReview = deferred<object>();
    loadDraft.mockReturnValueOnce(pendingReview.promise);
    fireEvent.click(screen.getByRole('button', { name: 'Review saved draft' }));
    type(input('P-1', '2026-09-21'), '4');
    saveDraft.mockResolvedValueOnce(draft([]));
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    expect(await screen.findByText('Draft saved. Nothing has been applied to QAD.')).toBeInTheDocument();
    await act(async () => { pendingReview.resolve(draft()); await Promise.resolve(); });
    expect(screen.queryByRole('table', { name: 'Saved draft comparison' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Restore saved draft' })).not.toBeInTheDocument();
    // Current proposals are untouched and remain saved.
    expect(input('P-3', '2026-09-14').value).toBe('7');
    expect(input('P-1', '2026-09-14').value).toBe('-5');
    expect(input('P-1', '2026-09-21').value).toBe('4');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('draft saved');
  });

  it('restore is refused when the reviewed draft was overwritten afterwards (save from an earlier workspace session)', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    const pendingSave = deferred<object>();
    saveDraft.mockReturnValueOnce(pendingSave.promise);
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    await leaveAndReturn(view);

    loadDraft.mockResolvedValueOnce(draft());
    fireEvent.click(await screen.findByRole('button', { name: 'Review saved draft' }));
    await screen.findByRole('table', { name: 'Saved draft comparison' });
    // The earlier session's save lands after this review was read: the reviewed contents are obsolete.
    await act(async () => { pendingSave.resolve(draft([])); await Promise.resolve(); });
    fireEvent.click(screen.getByRole('button', { name: 'Restore saved draft' }));
    expect(screen.getByText('The saved draft changed after it was reviewed. Review the saved draft again.')).toBeInTheDocument();
    expect(input('P-1', '2026-09-21').value).toBe('');
    expect(screen.queryByText(/draft saved/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Draft saved/)).not.toBeInTheDocument();
  });

  it('a pending review or presence check completing after deletion cannot resurrect the deleted draft', async () => {
    draftPresence.mockResolvedValueOnce({ exists: true });
    const pendingReview = deferred<object>();
    loadDraft.mockReturnValueOnce(pendingReview.promise);
    await renderPanel();
    type(input('P-1', '2026-09-14'), '3');
    fireEvent.click(await screen.findByRole('button', { name: 'Review saved draft' }));
    deleteDraft.mockResolvedValueOnce(undefined);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fireEvent.click(screen.getByRole('button', { name: 'Delete saved draft' }));
    expect(await screen.findByText(/Saved draft deleted/)).toBeInTheDocument();

    await act(async () => { pendingReview.resolve(draft()); await Promise.resolve(); });
    expect(screen.queryByRole('table', { name: 'Saved draft comparison' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Restore saved draft' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Review saved draft' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Delete saved draft' })).not.toBeInTheDocument();
    expect(input('P-1', '2026-09-14').value).toBe('3');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('not saved');
  });

  it('A \u2192 B \u2192 A: a delayed save from the earlier session never marks the new session saved, even with an equal edit counter', async () => {
    const pendingSave = deferred<object>();
    saveDraft.mockReturnValueOnce(pendingSave.promise);
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50'); // edit counter 1 in the first session
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    await leaveAndReturn(view);

    type(input('P-1', '2026-09-14'), '7'); // edit counter 1 again in the new session, different value
    await act(async () => { pendingSave.resolve(draft([])); await Promise.resolve(); });
    expect(screen.queryByText(/Draft saved/)).not.toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('1 change \u00b7 not saved');
    expect(screen.getByRole('button', { name: 'Save draft' })).toHaveTextContent('Save draft'); // not stuck in Saving…
  });

  it('A \u2192 B \u2192 A: an earlier session\'s save completing after a restore leaves the restored session unsaved', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    const pendingSave = deferred<object>();
    saveDraft.mockReturnValueOnce(pendingSave.promise);
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    await leaveAndReturn(view);

    loadDraft.mockResolvedValueOnce(draft());
    fireEvent.click(await screen.findByRole('button', { name: 'Review saved draft' }));
    await screen.findByRole('table', { name: 'Saved draft comparison' });
    fireEvent.click(screen.getByRole('button', { name: 'Restore saved draft' }));
    expect(input('P-1', '2026-09-21').value).toBe('10');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('draft saved');

    // The earlier visit's save now overwrites the file: the restored rows are no longer what is saved.
    await act(async () => { pendingSave.resolve(draft([])); await Promise.resolve(); });
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('not saved');
    expect(input('P-1', '2026-09-21').value).toBe('10');
  });

  it('A \u2192 B \u2192 A: an earlier session\'s delete completing after a restore leaves the restored session unsaved', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    const pendingDelete = deferred<undefined>();
    deleteDraft.mockReturnValueOnce(pendingDelete.promise);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const view = await renderPanel();
    fireEvent.click(await screen.findByRole('button', { name: 'Delete saved draft' }));
    await leaveAndReturn(view);

    loadDraft.mockResolvedValueOnce(draft());
    fireEvent.click(await screen.findByRole('button', { name: 'Review saved draft' }));
    await screen.findByRole('table', { name: 'Saved draft comparison' });
    fireEvent.click(screen.getByRole('button', { name: 'Restore saved draft' }));
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('draft saved');
    await act(async () => { pendingDelete.resolve(undefined); await Promise.resolve(); });
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('not saved');
  });

  it('A \u2192 B \u2192 A: a delayed review from the earlier session is not shown in the new session', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    const pendingReview = deferred<object>();
    loadDraft.mockReturnValueOnce(pendingReview.promise);
    const view = await renderPanel();
    fireEvent.click(await screen.findByRole('button', { name: 'Review saved draft' }));
    await leaveAndReturn(view);
    type(input('P-3', '2026-09-14'), '2');

    await act(async () => { pendingReview.resolve(draft()); await Promise.resolve(); });
    expect(screen.queryByRole('table', { name: 'Saved draft comparison' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Review saved draft' })).toBeEnabled();
    expect(input('P-3', '2026-09-14').value).toBe('2');
    expect(input('P-1', '2026-09-21').value).toBe('');
  });
});

describe('Planning adjustments: undo boundaries', { timeout: 20_000 }, () => {
  it('coalesces keystrokes only within one cell-editing session', async () => {
    await renderPanel();
    const cell = input('P-1', '2026-09-14');
    type(cell, '5');
    type(cell, '57');
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-1', '2026-09-14').value).toBe(''); // one typing session = one undo step

    type(input('P-1', '2026-09-14'), '5');
    fireEvent.blur(input('P-1', '2026-09-14')); // focus leaves the cell: the editing session ends
    type(input('P-1', '2026-09-14'), '57');
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-1', '2026-09-14').value).toBe('5');
  });

  it('entering 10, saving, returning to the same cell and entering 20 then undoing gives 10', async () => {
    saveDraft.mockResolvedValueOnce({ exists: true, isCorrupt: false, warning: null, savedAtUtc: '2026-09-16T17:00:00Z', savedMpsSnapshotId: SNAP, savedSite: 'S1', rows: [] });
    await renderPanel();
    type(input('P-1', '2026-09-14'), '1');
    type(input('P-1', '2026-09-14'), '10');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' })); // no blur in between: save is the boundary
    expect(await screen.findByText('Draft saved. Nothing has been applied to QAD.')).toBeInTheDocument();
    type(input('P-1', '2026-09-14'), '');
    type(input('P-1', '2026-09-14'), '2');
    type(input('P-1', '2026-09-14'), '20');
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-1', '2026-09-14').value).toBe('10');
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-1', '2026-09-14').value).toBe('');
  });

  it('Remove and confirmed Clear All remain separate undo steps after typing', async () => {
    await renderPanel();
    type(input('P-2', '2026-09-28'), '-120');
    const review = await screen.findByRole('list', { name: 'Staged changes' });
    fireEvent.click(within(review).getByRole('button', { name: /Remove/ }));
    expect(input('P-2', '2026-09-28').value).toBe('');
    type(input('P-2', '2026-09-28'), '-1');
    fireEvent.click(screen.getByRole('button', { name: 'Discard all' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirm Discard all' }));
    expect(input('P-2', '2026-09-28').value).toBe('');
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-2', '2026-09-28').value).toBe('-1');
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-2', '2026-09-28').value).toBe('');
    fireEvent.click(screen.getByRole('button', { name: 'Undo' }));
    expect(input('P-2', '2026-09-28').value).toBe('-120');
  });
});

/* ---- 14.5 redesign review corrections (2026-10-06): obsolete presence checks, review focus ---- */

describe('Planning saved drafts: obsolete presence checks are recovered', { timeout: 20_000 }, () => {
  const OTHER = '00000000-0000-0000-0000-00000000cccc';
  const emptyDraft = { exists: true, isCorrupt: false, warning: null, savedAtUtc: '2026-09-15T20:00:00Z', savedMpsSnapshotId: SNAP, savedSite: 'S1', rows: [] };
  const show = (view: ReturnType<typeof render>, assignmentId: string) =>
    view.rerender(<PlanningWorkbookPanel assignmentId={assignmentId} site="S1" snapshotId={SNAP} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
  /** Presence answers for this assignment; `hold` keeps new checks pending until released. */
  let exists: boolean | 'fail';
  let hold: boolean;
  let held: ReturnType<typeof deferred<{ exists: boolean }>>[];
  const presenceCalls = () => draftPresence.mock.calls.filter(([id]) => id === ID).length;

  beforeEach(() => {
    exists = false; hold = false; held = [];
    draftPresence.mockImplementation((id: string) => {
      if (id !== ID) return Promise.resolve({ exists: false });
      if (hold) { const d = deferred<{ exists: boolean }>(); held.push(d); return d.promise; }
      return exists === 'fail' ? Promise.reject(problem(503, {})) : Promise.resolve({ exists });
    });
  });

  /** A → B → A; returning to A leaves its presence checks pending when `holdOnReturn` is set. */
  async function leaveAndReturn(view: ReturnType<typeof render>, holdOnReturn: boolean) {
    show(view, OTHER);
    await waitFor(() => expect(draftPresence).toHaveBeenLastCalledWith(OTHER));
    hold = holdOnReturn;
    show(view, ID);
    await waitFor(() => expect(draftPresence).toHaveBeenLastCalledWith(ID));
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    startPlanning();
    await waitFor(() => expect(input('P-1', '2026-09-14').value).toBe(''));
  }
  const releaseHeld = async (answer: { exists: boolean }) => {
    await act(async () => { for (const d of held) d.resolve(answer); await Promise.resolve(); });
  };

  it('the earlier visit\'s save finishes before the pending presence response: availability is checked again', async () => {
    const pendingSave = deferred<object>();
    saveDraft.mockReturnValueOnce(pendingSave.promise);
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    await leaveAndReturn(view, true);
    type(input('P-2', '2026-09-28'), '-5');
    const workbookCalls = getWorkbook.mock.calls.length;

    hold = false; exists = true; // the earlier save has now written the file
    await act(async () => { pendingSave.resolve(emptyDraft); await Promise.resolve(); });
    expect(await screen.findByRole('button', { name: 'Review saved draft' })).toBeEnabled();
    expect(screen.getByText(/A saved Planning draft is available/)).toBeInTheDocument();
    // The response read before the save finished arrives last and is ignored.
    await releaseHeld({ exists: false });
    expect(screen.getByRole('button', { name: 'Review saved draft' })).toBeEnabled();

    // Nothing restored, no Planning/MPS refresh, current proposals and the unsaved warning kept.
    expect(loadDraft).not.toHaveBeenCalled();
    expect(refreshWorkbook).not.toHaveBeenCalled();
    expect(getWorkbook.mock.calls.length).toBe(workbookCalls);
    expect(input('P-2', '2026-09-28').value).toBe('-5');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('1 change \u00b7 not saved');
  });

  it('the earlier visit\'s delete finishes after presence already reported the old draft: the obsolete notice is replaced', async () => {
    exists = true;
    const pendingDelete = deferred<undefined>();
    deleteDraft.mockReturnValueOnce(pendingDelete.promise);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const view = await renderPanel();
    fireEvent.click(await screen.findByRole('button', { name: 'Delete saved draft' }));
    await leaveAndReturn(view, false);
    // Read while the delete was still in progress: the old draft is reported available.
    expect(await screen.findByRole('button', { name: 'Review saved draft' })).toBeInTheDocument();
    const before = presenceCalls();

    exists = false;
    await act(async () => { pendingDelete.resolve(undefined); await Promise.resolve(); });
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Review saved draft' })).not.toBeInTheDocument());
    expect(screen.queryByText(/A saved Planning draft is available/)).not.toBeInTheDocument();
    expect(presenceCalls()).toBe(before + 1);
    expect(loadDraft).not.toHaveBeenCalled();
  });

  it('a failed earlier save also makes a pending presence check obsolete and is recovered', async () => {
    exists = true;
    const pendingSave = deferred<object>();
    saveDraft.mockReturnValueOnce(pendingSave.promise);
    const view = await renderPanel();
    type(input('P-1', '2026-09-14'), '-50');
    fireEvent.click(screen.getByRole('button', { name: 'Save draft' }));
    await leaveAndReturn(view, true);

    hold = false;
    await act(async () => { pendingSave.reject(problem(503, {})); await Promise.resolve(); });
    expect(await screen.findByRole('button', { name: 'Review saved draft' })).toBeEnabled();
    await releaseHeld({ exists: false });
    expect(screen.getByRole('button', { name: 'Review saved draft' })).toBeEnabled();
    expect(screen.queryByText(/Save Draft failed/)).not.toBeInTheDocument(); // the old visit's error is not shown here
  });

  it('a failed earlier delete with an unavailable recheck offers Retry once, without looping', async () => {
    exists = true;
    const pendingDelete = deferred<undefined>();
    deleteDraft.mockReturnValueOnce(pendingDelete.promise);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const view = await renderPanel();
    fireEvent.click(await screen.findByRole('button', { name: 'Delete saved draft' }));
    await leaveAndReturn(view, false);
    type(input('P-3', '2026-09-14'), '4');
    expect(await screen.findByRole('button', { name: 'Review saved draft' })).toBeInTheDocument();
    const before = presenceCalls();

    exists = 'fail';
    await act(async () => { pendingDelete.reject(problem(503, {})); await Promise.resolve(); });
    const retry = await screen.findByRole('button', { name: 'Retry' });
    expect(screen.getByText(/Could not check for a saved Planning draft/)).toBeInTheDocument();
    await act(async () => { await new Promise(r => setTimeout(r, 50)); });
    expect(presenceCalls()).toBe(before + 1); // one automatic recheck; a failure never retries by itself

    exists = true;
    fireEvent.click(retry);
    expect(await screen.findByRole('button', { name: 'Review saved draft' })).toBeEnabled();
    expect(presenceCalls()).toBe(before + 2);
    expect(input('P-3', '2026-09-14').value).toBe('4');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('not saved');
    expect(loadDraft).not.toHaveBeenCalled();
  });
});

describe('Planning saved-draft review: focus returns to a control in the current workspace', { timeout: 20_000 }, () => {
  const OTHER = '00000000-0000-0000-0000-00000000dddd';
  const reviewDraft = { exists: true, isCorrupt: false, warning: null, savedAtUtc: '2026-09-15T20:00:00Z', savedMpsSnapshotId: SNAP, savedSite: 'S1',
    rows: [{ part: 'P-1', weekLabel: '2026-09-21', adjustment: '10', savedMpsQuantity: '75.5', currentMps: '75.5', baselineChanged: false, proposedMps: '85.5', issueCode: null }] };

  /** Focuses Review saved draft explicitly, opens it, awaits the asynchronous review and returns the dialog. */
  async function openReview() {
    const pending = deferred<object>();
    loadDraft.mockReturnValueOnce(pending.promise);
    const trigger = await screen.findByRole('button', { name: 'Review saved draft' });
    trigger.focus();
    expect(document.activeElement).toBe(trigger);
    fireEvent.click(trigger);
    await act(async () => { pending.resolve(reviewDraft); await Promise.resolve(); });
    const dialog = await screen.findByRole('dialog', { name: 'Saved draft' });
    expect(document.activeElement).toHaveTextContent('Restore saved draft'); // initial focus unchanged
    return dialog;
  }
  const reviewButton = () => screen.getByRole('button', { name: 'Review saved draft' });

  beforeEach(() => { draftPresence.mockResolvedValue({ exists: true }); });

  it.each([
    ['Cancel', (dialog: HTMLElement) => fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }))],
    ['Escape', (dialog: HTMLElement) => fireEvent.keyDown(dialog, { key: 'Escape' })],
    ['Close', (dialog: HTMLElement) => fireEvent.click(within(dialog).getByRole('button', { name: 'Close' }))],
  ])('%s returns focus to Review saved draft and keeps proposals', async (_name, close) => {
    await renderPanel();
    type(input('P-3', '2026-09-14'), '7');
    const dialog = await openReview();
    close(dialog);
    expect(screen.queryByRole('dialog', { name: 'Saved draft' })).not.toBeInTheDocument();
    await waitFor(() => expect(document.activeElement).toBe(reviewButton()));
    expect(input('P-3', '2026-09-14').value).toBe('7');
    expect(input('P-1', '2026-09-21').value).toBe('');
  });

  it('keeps Tab inside the dialog while it is open', async () => {
    await renderPanel();
    const dialog = await openReview();
    const buttons = within(dialog).getAllByRole('button').filter(b => !(b as HTMLButtonElement).disabled);
    buttons[buttons.length - 1].focus();
    fireEvent.keyDown(dialog, { key: 'Tab' });
    expect(document.activeElement).toBe(buttons[0]);
    fireEvent.keyDown(dialog, { key: 'Tab', shiftKey: true });
    expect(document.activeElement).toBe(buttons[buttons.length - 1]);
  });

  it('after Delete saved draft removes the Review control, focus goes to the mode button of this workspace', async () => {
    const pendingDelete = deferred<undefined>();
    deleteDraft.mockReturnValueOnce(pendingDelete.promise);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    await renderPanel();
    type(input('P-3', '2026-09-14'), '7');
    const dialog = await openReview();
    fireEvent.click(within(dialog).getByRole('button', { name: 'Delete saved draft' }));
    expect(screen.queryByRole('dialog', { name: 'Saved draft' })).not.toBeInTheDocument();
    await waitFor(() => expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Back to report' })));
    draftPresence.mockResolvedValue({ exists: false });
    await act(async () => { pendingDelete.resolve(undefined); await Promise.resolve(); });
    expect(await screen.findByText(/Saved draft deleted/)).toBeInTheDocument();
    expect(input('P-3', '2026-09-14').value).toBe('7');
  });

  it('after switching workspace with the review open, focus goes to a control in the new workspace', async () => {
    const view = await renderPanel();
    await openReview();
    draftPresence.mockImplementation((id: string) => Promise.resolve({ exists: id === ID }));
    view.rerender(<PlanningWorkbookPanel assignmentId={OTHER} site="S1" snapshotId={SNAP} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    expect(screen.queryByRole('dialog', { name: 'Saved draft' })).not.toBeInTheDocument();
    await waitFor(() => expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Start planning' })));
  });

  it('the forecast dialog still returns focus to Import forecast', async () => {
    await renderPanel();
    const opener = screen.getByRole('button', { name: /Import forecast/ });
    opener.focus();
    fireEvent.click(opener);
    const dialog = await screen.findByRole('dialog', { name: 'Import customer forecast' });
    expect(document.activeElement).toBe(within(dialog).getAllByRole('button', { name: 'Close' }).find(b => b.textContent === 'Close'));
    fireEvent.keyDown(dialog, { key: 'Escape' });
    expect(document.activeElement).toBe(screen.getByRole('button', { name: /Import forecast/ }));
  });
});

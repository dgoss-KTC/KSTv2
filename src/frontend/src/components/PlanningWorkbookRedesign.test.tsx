import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { Profiler } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { hoverColumnCss, sharedColumnWidth, strikeFits } from './planning/planningColumnSizing';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from '../fiscal/fiscalCalendar';
import type { PlanningWorkbook } from '../planningWorkbook/planningWorkbookSummary';
import { fixtureToWorkbook, type Stage14Fixture } from '../planningWorkbook/stage14FixtureAdapter.testSupport';
import { isExactZero, qty } from '../planningWorkbook/planningView';
import { PlanningWorkbookPanel } from './PlanningWorkbookPanel';

/*
 * Stage 14.5 Master plan redesign (owner design, 2026-10-06): report/planning modes, compact grid,
 * View tailoring, Changes panel, dialogs and the owner's decisions on totals, density and hidden
 * entries. All API traffic is mocked; values come from the accepted Stage 14 fixture padded to 12 weeks.
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

const fixture = JSON.parse(readFileSync(resolve(process.cwd(), '../../docs/reference/STAGE_14_PLANNING_WORKBOOK_DETERMINISTIC_FIXTURES.json'), 'utf8')) as Stage14Fixture;
const ID = fixture.scope.workspaceAssignmentId;
const OTHER = '00000000-0000-0000-0000-00000000cccc';
const SNAP = fixture.scope.mpsSnapshotId;
const VIEW_KEY = (id: string) => `kst.planningWorkbook.view.v1.${id}`;

function iso(base: string, weeks: number) {
  const [y, m, d] = base.split('-').map(Number);
  const date = new Date(y, m - 1, d + 7 * weeks);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}
function fixtureWorkbook(snapshot = SNAP, horizon = 12): PlanningWorkbook {
  const base = fixtureToWorkbook(fixture);
  const labels = Array.from({ length: horizon }, (_, i) => iso(base.weekLabels[0], i));
  return {
    ...base, mpsSnapshotId: snapshot, horizonWeeks: horizon, weekLabels: labels, maximumHorizonWeeks: 20,
    parts: base.parts.map(p => ({ ...p, weeks: labels.map((label, i) => p.weeks[i] ?? {
      ...p.weeks[p.weeks.length - 1], weekLabel: label, salesOrderQuantity: '0', salesOrderDollars: '0', mps: '0',
      mpsDollars: '0', forecast: '0',
    }) })),
  };
}

const input = (part: string, weekIso: string) => {
  const [y, m, d] = weekIso.split('-').map(Number);
  return screen.getByRole('textbox', { name: `Adjustment for ${part}, week of ${m}/${d}/${y}` }) as HTMLInputElement;
};
const block = (part: string) => screen.getByRole('rowgroup', { name: `Part ${part}` });
const rowLabels = (part: string) => within(block(part)).getAllByRole('rowheader').filter(h => h.getAttribute('scope') === 'row').map(h => h.textContent);
const totals = () => within(screen.getByRole('rowgroup', { name: 'Workspace totals' }));
const totalCells = (name: RegExp) => within(totals().getByRole('row', { name })).getAllByRole('cell').map(c => c.querySelector('.pwb-v')?.textContent ?? c.textContent);
const type = (el: HTMLInputElement, value: string) => fireEvent.change(el, { target: { value } });
const click = (name: string | RegExp) => fireEvent.click(screen.getByRole('button', { name }));
const blocks = () => screen.getAllByRole('rowgroup').map(g => g.getAttribute('aria-label')).filter(l => l?.startsWith('Part '));

async function renderPanel(assignmentId = ID) {
  const view = render(<PlanningWorkbookPanel assignmentId={assignmentId} site="S1" snapshotId={SNAP} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
  await screen.findByRole('region', { name: 'Planning workbook grid' });
  return view;
}
const rerenderFor = (view: ReturnType<typeof render>, assignmentId: string) =>
  view.rerender(<PlanningWorkbookPanel assignmentId={assignmentId} site="S1" snapshotId={SNAP} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);

beforeEach(() => {
  for (const mock of [getWorkbook, refreshWorkbook, draftPresence, loadDraft, saveDraft, deleteDraft]) mock.mockReset();
  window.localStorage.clear();
  getWorkbook.mockImplementation((id: string, snapshot: string, horizon: number) => Promise.resolve({ ...fixtureWorkbook(snapshot, horizon), workspaceId: id }));
  draftPresence.mockResolvedValue({ exists: false });
});
afterEach(() => vi.restoreAllMocks());

describe('Master plan: report and planning modes', { timeout: 20_000 }, () => {
  it('opens in report mode with three metric rows and loaded values only', async () => {
    await renderPanel();
    expect(screen.getByTestId('planning-mode')).toHaveTextContent('Report');
    expect(rowLabels('P-1')).toEqual(['Sales orders', 'MPS', 'Projected on hand']);
    expect(screen.queryAllByRole('textbox')).toHaveLength(0);
    expect(screen.queryByRole('button', { name: 'Undo' })).not.toBeInTheDocument();
    expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Start planning' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Export Excel' })).not.toBeInTheDocument();
  });

  it('Start planning switches locally (no request), adds Adjust and opens Changes; Back to report keeps every entry', async () => {
    await renderPanel();
    const requests = getWorkbook.mock.calls.length;
    click('Start planning');
    expect(screen.getByTestId('planning-mode')).toHaveTextContent('Planning');
    expect(screen.queryByRole('button', { name: 'Export Excel' })).not.toBeInTheDocument();
    expect(rowLabels('P-1')).toEqual(['Sales orders', 'MPS', 'Adjust', 'Projected on hand']);
    expect(screen.getByRole('complementary', { name: /^Changes/ })).toBeInTheDocument();
    expect(getWorkbook.mock.calls.length).toBe(requests);
    expect(draftPresence).toHaveBeenCalledTimes(1);

    type(input('P-1', '2026-09-14'), '-50');
    fireEvent.blur(input('P-1', '2026-09-14'));
    const mps = within(within(block('P-1')).getByRole('row', { name: /^MPS/ })).getAllByRole('cell')[1];
    expect(mps).toHaveClass('pwb-cell--changed');

    click('Back to report');
    expect(screen.getByTestId('planning-mode')).toHaveTextContent('Report');
    expect(screen.queryByRole('complementary', { name: /^Changes/ })).not.toBeInTheDocument();
    expect(rowLabels('P-1')).toEqual(['Sales orders', 'MPS', 'Projected on hand']);
    const reportMps = within(within(block('P-1')).getByRole('row', { name: /^MPS/ })).getAllByRole('cell')[1];
    expect(reportMps).not.toHaveClass('pwb-cell--changed');
    expect(reportMps).toHaveTextContent('100'); // loaded value, no struck-through proposal
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('1 change \u00b7 not saved');

    click('Resume planning \u00b7 1');
    expect(input('P-1', '2026-09-14').value).toBe('-50');
    expect(getWorkbook.mock.calls.length).toBe(requests);
  });

  it('keeps export disabled until an adjustment is staged and offers only the inert forecast-import stub', async () => {
    await renderPanel();
    click('Start planning');
    const exportButton = screen.getByRole('button', { name: 'Review and export' });
    expect(exportButton).toBeDisabled();
    expect(exportButton).toHaveAccessibleDescription(/Stage at least one adjustment/);

    const opener = screen.getByRole('button', { name: /Import forecast/ });
    opener.focus();
    fireEvent.click(opener);
    const dialog = screen.getByRole('dialog', { name: 'Import customer forecast' });
    expect(dialog).toHaveTextContent('Future iteration');
    expect(dialog).toHaveTextContent('File import is not built yet');
    expect(dialog.querySelector('input')).toBeNull(); // no file input: nothing can be imported
    expect(document.activeElement).toHaveTextContent('Close');
    fireEvent.keyDown(dialog, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(document.activeElement).toBe(opener);
    expect(rowLabels('P-1')).not.toContain('Customer forecast');
    expect(saveDraft).not.toHaveBeenCalled();
    expect(refreshWorkbook).not.toHaveBeenCalled();
  });

  it('keeps an accessible freshness detail and the guarded Refresh explanation', async () => {
    await renderPanel();
    expect(screen.getByTestId('planning-freshness')).toHaveTextContent(/Prices as of \d+\/\d+\/\d{4} \u00b7 Today \d+\/\d+\/\d{4}/);
    expect(screen.getByTestId('planning-freshness')).toHaveTextContent(/Acquired /);
    expect(screen.getByRole('button', { name: 'Refresh Planning' })).toHaveAccessibleDescription(/Customer Open Orders report.*MPS is not refreshed.*Staged adjustments are kept/);
  });
});

describe('Master plan: whole-workspace totals, filters and collapse', { timeout: 20_000 }, () => {
  it('totals cover the whole workspace regardless of hidden models and filter chips; a hidden no-list-price model still counts at $0 in MPS $ totals', async () => {
    await renderPanel();
    const so = totalCells(/^Total SO \$/);
    const mpsTotals = totalCells(/^Total MPS \$/);
    expect(mpsTotals.some(v => v !== '')).toBe(true); // P-3 has no list price and adds $0: totals are not blank
    click('View');
    const view = screen.getByRole('complementary', { name: 'View' });
    fireEvent.click(within(view).getByRole('checkbox', { name: /P-3/ }));
    fireEvent.click(within(view).getByRole('checkbox', { name: /P-1/ }));
    expect(blocks()).toEqual(['Part P-2']);
    expect(screen.getByRole('button', { name: 'View \u00b7 1 of 3 models' })).toBeInTheDocument();
    expect(totalCells(/^Total SO \$/)).toEqual(so);
    expect(totalCells(/^Total MPS \$/)).toEqual(mpsTotals); // whole-workspace totals, never a subtotal of the visible models
    expect(totals().getByRole('row', { name: /^Total SO \$/ })).toHaveTextContent('all 3 models');

    fireEvent.click(within(view).getByRole('button', { name: 'Show all' }));
    fireEvent.click(screen.getByRole('button', { name: /^Short/ }));
    expect(totalCells(/^Total SO \$/)).toEqual(so);
  });

  it('offers quantity and adjustment totals through View; inventory is never totalled', async () => {
    await renderPanel();
    click('View');
    const view = screen.getByRole('complementary', { name: 'View' });
    fireEvent.click(within(view).getByRole('checkbox', { name: 'Total sales orders' }));
    fireEvent.click(within(view).getByRole('checkbox', { name: 'Total MPS' }));
    fireEvent.click(within(view).getByRole('checkbox', { name: /Total adjust/ }));
    expect(totals().getByRole('row', { name: /^Total sales orders/ })).toBeInTheDocument();
    expect(totals().getByRole('row', { name: /^Total MPS (?!\$)/ })).toBeInTheDocument();
    expect(totals().queryByRole('row', { name: /^Total adjust/ })).not.toBeInTheDocument(); // planning mode only
    const totalOptions = within(within(view).getByRole('region', { name: 'Workspace totals' })).getAllByRole('checkbox');
    expect(totalOptions.map(o => o.parentElement?.textContent)).toEqual(['Total sales orders', 'Total SO $', 'Total MPS', 'Total MPS $', 'Total adjust (planning mode)']);
    click('Start planning');
    type(input('P-1', '2026-09-14'), '-50');
    expect(totalCells(/^Total adjust/)[1]).toBe('-50');
    expect(totals().queryByRole('row', { name: /on hand/i })).not.toBeInTheDocument();
  });

  it('filters, hides and collapses models without changing proposals, the Changes list or totals', async () => {
    await renderPanel();
    click('Start planning');
    type(input('P-1', '2026-09-14'), '-50');
    type(input('P-3', '2026-09-21'), '5');
    fireEvent.blur(input('P-3', '2026-09-21'));
    const before = totalCells(/^Total SO \$/);

    fireEvent.click(screen.getByRole('button', { name: /^Changed/ }));
    expect(screen.getByRole('button', { name: /^Changed/ })).toHaveAttribute('aria-pressed', 'true');
    expect(blocks()).toEqual(['Part P-1', 'Part P-3']);

    fireEvent.click(screen.getByRole('button', { name: 'Collapse P-1' }));
    expect(within(block('P-1')).queryByRole('textbox')).not.toBeInTheDocument();
    expect(screen.getByRole('list', { name: 'Staged changes' }).children).toHaveLength(2);
    fireEvent.click(screen.getByRole('button', { name: 'Expand P-1' }));
    expect(input('P-1', '2026-09-14').value).toBe('-50');

    click('Collapse all');
    expect(screen.queryAllByRole('textbox')).toHaveLength(0);
    click('Expand all');
    expect(input('P-3', '2026-09-21').value).toBe('5');

    click('View');
    fireEvent.click(within(screen.getByRole('complementary', { name: 'View' })).getByRole('checkbox', { name: /P-1/ }));
    expect(blocks()).toEqual(['Part P-3']);
    click('Changes \u00b7 2'); // one right-hand slot: Changes replaces View
    expect(screen.queryByRole('complementary', { name: 'View' })).not.toBeInTheDocument();
    const list = screen.getByRole('list', { name: 'Staged changes' });
    expect(within(list).getByRole('listitem', { name: /^P-1/ })).toHaveTextContent('Model hidden in View. The change is kept.');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('2 changes');
    expect(totalCells(/^Total SO \$/)).toEqual(before);

    // Back to report: the Changed chip is not offered and its filter falls back to all models.
    click('Back to report');
    expect(screen.queryByRole('button', { name: /^Changed/ })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^All models/ })).toHaveAttribute('aria-pressed', 'true');
  });

  it('collapsed blocks show the mode-appropriate projected inventory and health strip; Short counts weekly weeks only', async () => {
    const data = fixtureWorkbook();
    const p2 = data.parts.find(p => p.part === 'P-2')!;
    const reportShort = p2.weeks.filter(w => w.estimatedOnHand.startsWith('-')).length;
    await renderPanel();
    const badge = () => within(block('P-2')).queryByText(/wks? short$/)?.textContent ?? '';
    expect(badge()).toBe(reportShort === 0 ? '' : `${reportShort} ${reportShort === 1 ? 'wk' : 'wks'} short`);
    expect(p2.falldown.estimatedOnHand.startsWith('-')).toBe(true); // Falldown negative, not counted

    fireEvent.click(screen.getByRole('button', { name: 'Collapse P-2' }));
    const header = within(block('P-2')).getAllByRole('row')[0];
    const strip = within(header).getAllByRole('cell');
    const shown = (value: string) => (isExactZero(value) ? '' : qty(value));
    expect(strip[1]).toHaveTextContent(shown(p2.weeks[0].estimatedOnHand));
    expect(within(block('P-2')).getAllByRole('row')).toHaveLength(1);

    // Planning: a -100 change at week 3 lowers adjusted inventory from that week on.
    click('Start planning');
    fireEvent.click(screen.getByRole('button', { name: 'Expand P-2' }));
    type(input('P-2', '2026-09-28'), '-100');
    const planningShort = p2.weeks.filter((w, i) => Number(w.estimatedOnHand) - (i >= 2 ? 100 : 0) < 0).length;
    expect(badge()).toBe(`${planningShort} ${planningShort === 1 ? 'wk' : 'wks'} short`);
    fireEvent.click(screen.getByRole('button', { name: 'Collapse P-2' }));
    const planningStrip = within(within(block('P-2')).getAllByRole('row')[0]).getAllByRole('cell');
    const week3 = planningStrip[4]; // Falldown, 9/14, 9/21, Q1 total, 9/28
    expect(week3).toHaveClass('pwb-health--negative');
    expect(week3).toHaveTextContent(qty(String(Number(p2.weeks[2].estimatedOnHand) - 100)));
    fireEvent.click(screen.getByRole('button', { name: /^Short/ }));
    expect(blocks()).toContain('Part P-2');
  });
});

describe('Master plan: View preferences', { timeout: 20_000 }, () => {
  it('persists hidden models, rows, totals and collapsed models per assignment, isolates assignments and never persists the filter', async () => {
    const view = await renderPanel();
    click('View');
    const panel = within(screen.getByRole('complementary', { name: 'View' }));
    fireEvent.click(panel.getByRole('checkbox', { name: /P-3/ }));
    fireEvent.click(panel.getByRole('checkbox', { name: 'SO $' }));
    fireEvent.click(panel.getByRole('checkbox', { name: 'Total sales orders' }));
    fireEvent.click(screen.getByRole('button', { name: 'Collapse P-2' }));
    fireEvent.click(screen.getByRole('button', { name: /^Short/ }));
    expect(JSON.parse(window.localStorage.getItem(VIEW_KEY(ID))!)).toEqual({
      hiddenModels: ['P-3'], rows: ['so', 'mps', 'onHand', 'soDollars'], totals: ['soDollars', 'mpsDollars', 'so'], collapsed: ['P-2'], tintRows: true,
    });

    rerenderFor(view, OTHER);
    await waitFor(() => expect(blocks()).toEqual(['Part P-2', 'Part P-1', 'Part P-3']));
    expect(rowLabels('P-1')).toEqual(['Sales orders', 'MPS', 'Projected on hand']);
    expect(screen.getByRole('button', { name: /^All models/ })).toHaveAttribute('aria-pressed', 'true');
    expect(window.localStorage.getItem(VIEW_KEY(OTHER))).toBeNull();

    rerenderFor(view, ID);
    await waitFor(() => expect(blocks()).not.toContain('Part P-3'));
    expect(within(block('P-2')).getAllByRole('row')).toHaveLength(1); // still collapsed
    expect(screen.getByRole('button', { name: /^All models/ })).toHaveAttribute('aria-pressed', 'true'); // filter not kept
    expect(rowLabels('P-1')).toEqual(['Sales orders', 'SO $', 'MPS', 'Projected on hand']);
    expect(totals().getByRole('row', { name: /^Total sales orders/ })).toBeInTheDocument();
  });

  it('reconciles stored preferences with the current scope and ignores malformed values', async () => {
    window.localStorage.setItem(VIEW_KEY(ID), JSON.stringify({ hiddenModels: ['GONE-1', 'p-1'], rows: ['so', 'bogus'], totals: 'x', collapsed: ['GONE-2'] }));
    await renderPanel();
    expect(blocks()).toEqual(['Part P-2', 'Part P-3']);
    expect(rowLabels('P-2')).toEqual(['Sales orders']);
    expect(screen.getByRole('button', { name: 'View \u00b7 2 of 3 models' })).toBeInTheDocument();
    expect(totals().getAllByRole('row')).toHaveLength(2); // default dollar totals
    click('View \u00b7 2 of 3 models');
    fireEvent.click(within(screen.getByRole('complementary', { name: 'View' })).getByRole('checkbox', { name: 'MPS' }));
    expect(JSON.parse(window.localStorage.getItem(VIEW_KEY(ID))!).hiddenModels).toEqual(['P-1']);
  });

  it('follows the global density preference: Planning stores no density and offers no density control', async () => {
    await renderPanel();
    click('View');
    const panel = screen.getByRole('complementary', { name: 'View' });
    expect(panel).toHaveTextContent('Row density follows the application setting under General.');
    expect(within(panel).queryByRole('radio')).not.toBeInTheDocument();
    expect(within(panel).queryByRole('button', { name: /compact|comfortable/i })).not.toBeInTheDocument();
    fireEvent.click(within(panel).getByRole('checkbox', { name: 'SO $' }));
    expect(window.localStorage.getItem(VIEW_KEY(ID))).not.toMatch(/density/i);
  });

  it('hides every model without losing changes and offers a way back', async () => {
    await renderPanel();
    click('Start planning');
    type(input('P-2', '2026-09-14'), '3');
    click('View');
    const panel = screen.getByRole('complementary', { name: 'View' });
    fireEvent.click(within(panel).getByRole('button', { name: 'Only short models' }));
    for (const box of panel.querySelectorAll<HTMLInputElement>('.pwb-view__list input:checked')) fireEvent.click(box);
    expect(blocks()).toEqual([]);
    expect(screen.getByText('All models are hidden in View.')).toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('1 change');
    click('Show all models');
    expect(input('P-2', '2026-09-14').value).toBe('3');
  });
});

describe('Master plan: Changes panel', { timeout: 20_000 }, () => {
  it('counts a change for a part with no list price at $0 and says so in the net-dollar figure', async () => {
    await renderPanel();
    click('Start planning');
    type(input('P-1', '2026-09-14'), '-50');
    expect(screen.getByText('Net change').nextSibling).toHaveTextContent('-50 units \u00b7 -$125.00');
    type(input('P-3', '2026-09-14'), '5');
    expect(screen.getByText('Net change').nextSibling).toHaveTextContent('-45 units \u00b7 -$125.00 (some parts at $0)');
    expect(screen.getByText(/Dollars count 1 change for parts with no eligible QAD list price at \$0\./)).toBeInTheDocument();
  });

  it('closes with its button and keeps the session; drafts and Discard all remain available in the panel', async () => {
    await renderPanel();
    click('Start planning');
    type(input('P-1', '2026-09-14'), '-50');
    const panel = within(screen.getByRole('complementary', { name: /^Changes/ }));
    expect(panel.getByRole('button', { name: 'Save draft' })).toBeEnabled();
    expect(panel.getByRole('button', { name: 'Discard all' })).toBeEnabled();
    click('Close Changes');
    expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
    expect(input('P-1', '2026-09-14').value).toBe('-50');
    click('Changes \u00b7 1');
    expect(screen.getByRole('complementary', { name: /^Changes/ })).toBeInTheDocument();
  });

  it('opens the saved-draft comparison as a dialog that Escape cancels without changing proposals', async () => {
    draftPresence.mockResolvedValue({ exists: true });
    loadDraft.mockResolvedValue({ exists: true, isCorrupt: false, warning: null, savedAtUtc: '2026-09-15T20:00:00Z', savedMpsSnapshotId: SNAP, savedSite: 'S1',
      rows: [{ part: 'P-1', weekLabel: '2026-09-21', adjustment: '10', savedMpsQuantity: '75.5', currentMps: '75.5', baselineChanged: false, proposedMps: '85.5', issueCode: null }] });
    await renderPanel();
    // Draft availability and review/delete are reachable in report mode too.
    fireEvent.click(await screen.findByRole('button', { name: 'Review saved draft' }));
    const dialog = await screen.findByRole('dialog', { name: 'Saved draft' });
    expect(within(dialog).getByRole('button', { name: 'Delete saved draft' })).toBeInTheDocument();
    fireEvent.keyDown(dialog, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    await act(async () => { await Promise.resolve(); });
    expect(screen.getByRole('button', { name: 'Start planning' })).toBeInTheDocument();
  });
});

/* ---- Owner amendment 2026-10-06: row tint, balance line, hover crosshair, content-sized columns ---- */

describe('Master plan amendment: row tint preference', { timeout: 20_000 }, () => {
  const table = () => screen.getByTestId('planning-grid-table');
  const tintBox = () => within(screen.getByRole('complementary', { name: 'View' })).getByRole('checkbox', { name: 'Tint rows by type' });
  const requestCounts = () => [getWorkbook, refreshWorkbook, draftPresence, loadDraft, saveDraft, deleteDraft].map(m => m.mock.calls.length);

  it('defaults on, with a Row color section in View', async () => {
    await renderPanel();
    expect(table()).toHaveClass('pwb-grid--tint');
    click('View');
    expect(within(screen.getByRole('complementary', { name: 'View' })).getByRole('heading', { name: 'Row color' })).toBeInTheDocument();
    expect(tintBox()).toBeChecked();
  });

  it('turning it off removes only the washes: proposals, values and data requests are unchanged; other preferences are kept', async () => {
    await renderPanel();
    click('Start planning');
    type(input('P-1', '2026-09-14'), '-50');
    const changeCount = screen.getByTestId('planning-change-count').textContent;
    const mpsRow = within(block('P-1')).getByRole('row', { name: /^MPS (?!\$)/ });
    const before = mpsRow.textContent;
    click('View');
    fireEvent.click(within(screen.getByRole('complementary', { name: 'View' })).getByRole('checkbox', { name: 'SO $' }));
    const requests = requestCounts();

    fireEvent.click(tintBox());
    expect(table()).not.toHaveClass('pwb-grid--tint');
    expect(tintBox()).not.toBeChecked();
    expect(input('P-1', '2026-09-14').value).toBe('-50');
    expect(screen.getByTestId('planning-change-count').textContent).toBe(changeCount);
    expect(within(block('P-1')).getByRole('row', { name: /^MPS (?!\$)/ }).textContent).toBe(before);
    // State classes are independent of the tint.
    expect(within(within(block('P-1')).getByRole('row', { name: /^MPS (?!\$)/ })).getAllByRole('cell')[1]).toHaveClass('pwb-cell--changed');
    await act(async () => { await new Promise(r => setTimeout(r, 20)); });
    expect(requestCounts()).toEqual(requests);
    expect(JSON.parse(window.localStorage.getItem(VIEW_KEY(ID))!)).toMatchObject({ tintRows: false, rows: ['so', 'mps', 'onHand', 'soDollars'] });

    fireEvent.click(tintBox());
    expect(table()).toHaveClass('pwb-grid--tint');
    expect(requestCounts()).toEqual(requests);
  });

  it('persists per workspace across remounts and isolates workspaces', async () => {
    const first = await renderPanel();
    click('View');
    fireEvent.click(tintBox());
    expect(table()).not.toHaveClass('pwb-grid--tint');
    rerenderFor(first, OTHER);
    await waitFor(() => expect(getWorkbook).toHaveBeenLastCalledWith(OTHER, SNAP, expect.any(Number)));
    await waitFor(() => expect(table()).toHaveClass('pwb-grid--tint'));
    first.unmount();

    await renderPanel(ID);
    expect(table()).not.toHaveClass('pwb-grid--tint');
  });

  it('older records without the field, and malformed values, default to on; an explicit false stays false', async () => {
    window.localStorage.setItem(VIEW_KEY(ID), JSON.stringify({ hiddenModels: [], rows: ['so', 'onHand'], totals: ['soDollars'], collapsed: ['P-2'] }));
    const view = await renderPanel();
    expect(table()).toHaveClass('pwb-grid--tint');
    expect(rowLabels('P-1')).toEqual(['Sales orders', 'Projected on hand']); // other saved choices kept
    view.unmount();

    for (const bad of ['no', null, 0, 'false', {}]) {
      window.localStorage.setItem(VIEW_KEY(ID), JSON.stringify({ rows: ['so'], tintRows: bad }));
      const v = await renderPanel();
      expect(table()).toHaveClass('pwb-grid--tint');
      v.unmount();
    }
    window.localStorage.setItem(VIEW_KEY(ID), JSON.stringify({ rows: ['so'], tintRows: false }));
    await renderPanel();
    expect(table()).not.toHaveClass('pwb-grid--tint');
    expect(rowLabels('P-1')).toEqual(['Sales orders']);
  });
});

describe('Master plan amendment: hover crosshair', { timeout: 20_000 }, () => {
  it('records the hovered column on the grid container only, for every row kind, with no React renders', async () => {
    let commits = 0;
    render(
      <Profiler id="planning" onRender={() => { commits++; }}>
        <PlanningWorkbookPanel assignmentId={ID} site="S1" snapshotId={SNAP} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />
      </Profiler>,
    );
    const grid = await screen.findByRole('region', { name: 'Planning workbook grid' });
    await act(async () => { await new Promise(r => setTimeout(r, 20)); });
    const settled = commits;

    const soRow = within(block('P-1')).getByRole('row', { name: /^Sales orders/ });
    const cellsOf = (row: HTMLElement) => [...row.children] as HTMLElement[];
    // Week 9/21 (index 3: label, Falldown, 9/14, 9/21), the Q1 total (index 4), Falldown (index 1).
    fireEvent.pointerOver(cellsOf(soRow)[3]);
    expect(grid).toHaveAttribute('data-hover-col', '3');
    fireEvent.pointerOver(cellsOf(soRow)[4]);
    expect(grid).toHaveAttribute('data-hover-col', '4');
    const header = screen.getAllByRole('columnheader')[1];
    fireEvent.pointerOver(header);
    expect(grid).toHaveAttribute('data-hover-col', '1');
    const modelHeader = within(block('P-2')).getAllByRole('row')[0];
    fireEvent.pointerOver(cellsOf(modelHeader)[5]);
    expect(grid).toHaveAttribute('data-hover-col', '5');
    const totalRow = within(screen.getByRole('rowgroup', { name: 'Workspace totals' })).getAllByRole('row')[0];
    fireEvent.pointerOver(cellsOf(totalRow)[2]);
    expect(grid).toHaveAttribute('data-hover-col', '2');
    // A row label highlights its row only (CSS :hover); leaving the grid clears the column.
    fireEvent.pointerOver(cellsOf(soRow)[0]);
    expect(grid).not.toHaveAttribute('data-hover-col');
    fireEvent.pointerOver(cellsOf(soRow)[6]);
    fireEvent.pointerLeave(grid);
    expect(grid).not.toHaveAttribute('data-hover-col');
    expect(commits).toBe(settled);

    // The generated rule covers interleaved quarter columns by DOM position.
    expect(grid.querySelector('style')?.textContent).toContain('.pwb-grid-scroll[data-hover-col="4"] .pwb-grid tr > :nth-child(5)');
  });
});

describe('Master plan amendment: content-sized value columns', { timeout: 20_000 }, () => {
  it('shared helpers: never below the minimum, rounded up, strike shown only when it fits', () => {
    expect(sharedColumnWidth([], 80)).toBe(80);
    expect(sharedColumnWidth([40, 79.2], 80)).toBe(80);
    expect(sharedColumnWidth([40, 96.1, 90], 80)).toBe(97);
    expect(sharedColumnWidth([Number.NaN, 120], 108)).toBe(120);
    expect(strikeFits(30, 25, 64)).toBe(true);
    expect(strikeFits(40, 30, 64)).toBe(false);
    expect(hoverColumnCss(1)).toBe('');
  });

  it('sizes week and quarter groups from the widest rendered value and drops the struck value when it no longer fits', async () => {
    // Stub text metrics: 7 px per character (jsdom has no layout).
    const width = function (this: Element) { return { width: (this.textContent ?? '').length * 7, height: 10, top: 0, left: 0, right: 0, bottom: 0, x: 0, y: 0, toJSON() {} } as DOMRect; };
    vi.spyOn(Element.prototype, 'getBoundingClientRect').mockImplementation(width);
    getWorkbook.mockImplementation((id: string, snapshot: string, horizon: number) => {
      const wb = fixtureWorkbook(snapshot, horizon);
      return Promise.resolve({ ...wb, workspaceId: id, parts: wb.parts.map(p => p.part !== 'P-1' ? p : {
        ...p, weeks: p.weeks.map((w, i) => i === 0 ? { ...w, mps: '123456789012', salesOrderQuantity: '-98765432.25' } : w),
      }) });
    });
    await renderPanel();
    const table = screen.getByTestId('planning-grid-table');
    const expected = (selector: string, minimum: number) => Math.max(minimum, Math.ceil(Math.max(0, ...[...table.querySelectorAll(selector)]
      .flatMap(cell => [...cell.querySelectorAll('.pwb-v, .pwb-head__label, .pwb-head__sub')].map(el => (el.textContent ?? '').length * 7)))));
    const weekWidth = () => expected('.pwb-head--week, .pwb-head--falldown, .pwb-cell--week, .pwb-cell--falldown', 80);
    const quarterWidth = () => expected('.pwb-head--quarter, .pwb-cell--quarter', 108);
    // Long quantities (and the inventory they drive) widen every week column together.
    expect(weekWidth()).toBeGreaterThan(80);
    await waitFor(() => expect(table.style.getPropertyValue('--pwb-col-w')).toBe(`${weekWidth()}px`));
    expect(table.style.getPropertyValue('--pwb-qtr-w')).toBe(`${quarterWidth()}px`);
    expect(quarterWidth()).toBeGreaterThan(108);

    click('Start planning');
    type(input('P-1', '2026-09-14'), '-1');
    // The adjustment-input row is included; widths are recomputed after the change.
    expect(table.style.getPropertyValue('--pwb-col-w')).toBe(`${weekWidth()}px`);
    const mpsCell = within(within(block('P-1')).getByRole('row', { name: /^MPS (?!\$)/ })).getAllByRole('cell')[1];
    expect(mpsCell.querySelector('.pwb-v')).toHaveTextContent('123,456,789,011'); // complete new value
    expect(mpsCell.querySelector('s')).not.toBeVisible(); // previous value only in the tooltip
    expect(mpsCell).toHaveAttribute('title', expect.stringContaining('loaded 123,456,789,012'));
    // A short change elsewhere keeps its struck previous value.
    type(input('P-1', '2026-09-21'), '1');
    const other = within(within(block('P-1')).getByRole('row', { name: /^MPS (?!\$)/ })).getAllByRole('cell')[2];
    expect(other.querySelector('s')).toBeVisible();
    expect(other.querySelector('s')).toHaveTextContent('75.5');
    expect(input('P-1', '2026-09-21').value).toBe('1');
  });
});

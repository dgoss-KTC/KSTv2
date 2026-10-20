import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS as settings } from '../fiscal/fiscalCalendar';
import type { AdjustmentEntries } from '../planningWorkbook/planningAdjustments';
import type { PlanningPart, PlanningWorkbook } from '../planningWorkbook/planningWorkbookSummary';
import { DEFAULT_VIEW_PREFERENCES, type PlanningMode, type PlanningViewPreferences } from '../planningWorkbook/planningView';
import { PlanningWorkbookGrid, PlanningWorkbookPanel } from './PlanningWorkbookPanel';

const getWorkbook = vi.fn();
const saveDraft = vi.fn();
const exportFile = vi.fn();
const saveFile = vi.fn();
vi.mock('../api/tauri-bridge', () => ({ resolveBackendBaseUrl: () => Promise.resolve('http://localhost'), isRunningInTauri: () => false }));
vi.mock('../api/client', async original => ({ ...(await original<typeof import('../api/client')>()),
  ApiClient: class {
    getPlanningWorkbook = getWorkbook;
    getPlanningWorkbookDraftPresence = () => Promise.resolve({ exists: false });
    savePlanningWorkbookDraft = saveDraft;
    exportPlanningMassUpdater = exportFile;
  },
}));
vi.mock('../shared/saveCsvFile', () => ({ saveCsvFile: (...args: unknown[]) => saveFile(...args) }));

const labels = ['2026-09-14', '2026-09-21', '2026-09-28', '2026-10-05', '2026-10-12', '2026-10-19',
  '2026-10-26', '2026-11-02', '2026-11-09', '2026-11-16', '2026-11-23', '2026-11-30'];
// Hand-specified source flows; no totals helper is used to construct expectations.
function model(name: string, price: string, so: string[], revenue: string[], mps: string[], dollars: string[], short = false): PlanningPart {
  const bucket = (i: number, weekLabel: string | null) => ({ kind: weekLabel ? 'weekly' : 'falldown', weekLabel,
    salesOrderQuantity: so[i] ?? '0', salesOrderDollars: revenue[i] ?? '0', mps: mps[i] ?? '0', mpsDollars: dollars[i] ?? '0',
    forecast: '999', estimatedOnHand: short ? '-1' : '1000' });
  return { part: name, description: `${name} model`, netQuantityOnHand: '1000', selectedPrice: price, isPriced: name !== 'C',
    priceTiers: name === 'C' ? [] : [{ minimumOrderQuantity: '1', unitPrice: price }],
    falldown: bucket(0, null), weeks: labels.map((l, i) => bucket(i + 1, l)),
    undated: { salesOrderQuantity: '9999', salesOrderDollars: '9999', lineCount: 1 } };
}
const workbook: PlanningWorkbook = {
  workspaceId: 'ws', site: 'S1', workbookSnapshotId: 'wb', mpsSnapshotId: 'snap', openOrdersSnapshotId: 'oo',
  acquiredAtUtc: '2026-09-16T12:00:00Z', priceEffectiveDate: '2026-09-16', priceSelectionPolicy: 'firstTier', today: '2026-09-16',
  horizonWeeks: 12, minimumSelectableHorizonWeeks: 12, maximumHorizonWeeks: 12, isStale: false, warning: null, weekLabels: labels,
  parts: [
    model('A', '2', ['1', '2', '3', '4'], ['3', '6', '9', '12'], ['4', '5', '6', '7'], ['8', '10', '12', '14']),
    model('B', '3', ['10', '20', '30', '40'], ['40', '80', '120', '160'], ['20', '30', '40', '50'], ['60', '90', '120', '150'], true),
    model('C', '0', ['100', '200', '300', '400'], ['500', '1000', '1500', '2000'], ['200', '300', '400', '500'], ['0', '0', '0', '0']),
  ],
};
const entries: AdjustmentEntries = {
  a0: { part: 'A', weekLabel: labels[0], text: '2' }, a2: { part: 'A', weekLabel: labels[2], text: '-1' },
  b0: { part: 'B', weekLabel: labels[0], text: '10' }, c1: { part: 'C', weekLabel: labels[1], text: '3' },
};
const prefs: PlanningViewPreferences = { ...DEFAULT_VIEW_PREFERENCES, rows: [], totals: ['so', 'soDollars', 'mps', 'mpsDollars', 'adjust'] };
const totalNames = [/^Total sales orders/, /^Total SO \$/, /^Total MPS (?!\$)/, /^Total MPS \$/, /^Total adjust/];
// Falldown, week 0, week 1, Q1 (Falldown once), week 2, Q2. Remaining weeks are zero.
const cells = (name: RegExp) => within(within(screen.getByRole('rowgroup', { name: 'Selected model totals' })).getByRole('row', { name })).getAllByRole('cell');
const values = (name: RegExp) => { const c = cells(name); return [0, 1, 2, 3, 4, c.length - 1].map(i => c[i].querySelector('.pwb-v')?.textContent); };
const all = {
  so: ['111', '222', '333', '666', '444', '444'], revenue: ['$543.00', '$1,086.00', '$1,629.00', '$3,258.00', '$2,172.00', '$2,172.00'],
  loaded: ['224', '335', '446', '1,005', '557', '557'], proposed: ['224', '347', '449', '1,020', '556', '556'],
  dollars: ['$68.00', '$100.00', '$132.00', '$300.00', '$164.00', '$164.00'], adjustedDollars: ['$68.00', '$134.00', '$132.00', '$334.00', '$162.00', '$162.00'],
  adjust: ['', '+12', '+3', '+15', '-1', '-1'],
};
const subset = {
  so: ['101', '202', '303', '606', '404', '404'], revenue: ['$503.00', '$1,006.00', '$1,509.00', '$3,018.00', '$2,012.00', '$2,012.00'],
  loaded: ['204', '305', '406', '915', '507', '507'], proposed: ['204', '307', '409', '920', '506', '506'],
  dollars: ['$8.00', '$10.00', '$12.00', '$30.00', '$14.00', '$14.00'], adjustedDollars: ['$8.00', '$14.00', '$12.00', '$34.00', '$12.00', '$12.00'],
  adjust: ['', '+2', '+3', '+5', '-1', '-1'],
};
function expectTotals(expected: typeof all, mode: PlanningMode) {
  expect(values(totalNames[0])).toEqual(expected.so);
  expect(values(totalNames[1])).toEqual(expected.revenue);
  expect(values(totalNames[2])).toEqual(mode === 'planning' ? expected.proposed : expected.loaded);
  expect(values(totalNames[3])).toEqual(mode === 'planning' ? expected.adjustedDollars : expected.dollars);
  if (mode === 'planning') expect(values(totalNames[4])).toEqual(expected.adjust);
}
const click = (name: string | RegExp) => fireEvent.click(screen.getByRole('button', { name }));
const viewPanel = () => within(screen.getByRole('complementary', { name: 'View' }));
const toggle = (name: string) => fireEvent.click(viewPanel().getByRole('checkbox', { name: new RegExp(`^${name} ${name} model`) }));
const stage = (part: string, date: string, text: string) => fireEvent.change(screen.getByRole('textbox', { name: `Adjustment for ${part}, week of ${date}` }), { target: { value: text } });
async function panel() {
  render(<PlanningWorkbookPanel assignmentId="ws" site="S1" snapshotId="snap" fiscalSettings={settings} />);
  await screen.findByRole('region', { name: 'Planning workbook grid' });
  click('Start planning');
  stage('A', '9/14/2026', '2'); stage('A', '9/28/2026', '-1'); stage('B', '9/14/2026', '10'); stage('C', '9/21/2026', '3');
}
beforeEach(() => {
  vi.clearAllMocks(); window.localStorage.clear();
  window.localStorage.setItem('kst.planningWorkbook.view.v1.ws', JSON.stringify(prefs));
  getWorkbook.mockResolvedValue(workbook);
});

describe('S22-004 selected model totals', { timeout: 20_000 }, () => {
  it.each(['report', 'planning'] as const)('standalone %s: all, subset, one, none and reselection across every flow/period', mode => {
    const grid = (hiddenModels: string[]) => <PlanningWorkbookGrid workbook={workbook} settings={settings} entries={entries} mode={mode} prefs={{ ...prefs, hiddenModels, collapsed: ['A', 'C'] }} />;
    const view = render(grid([]));
    expectTotals(all, mode);
    view.rerender(grid(['B']));
    expectTotals(subset, mode);
    expect(screen.getByTestId('mps-dollar-note')).toHaveTextContent('1 selected model has');
    if (mode === 'planning') {
      expect(cells(totalNames[2])[1]).toHaveAttribute('title', 'Proposed 307; loaded 305');
      expect(cells(totalNames[2])[1].querySelector('s')).toHaveTextContent('305');
      expect(cells(totalNames[3])[3]).toHaveAttribute('title', 'Proposed $34.00; loaded $30.00');
    }
    view.rerender(grid(['B', 'C']));
    expect(values(totalNames[0])).toEqual(['1', '2', '3', '6', '4', '4']);
    expect(values(totalNames[2])).toEqual(mode === 'planning' ? ['4', '7', '6', '17', '6', '6'] : ['4', '5', '6', '15', '7', '7']);
    expect(screen.queryByTestId('mps-dollar-note')).not.toBeInTheDocument();
    view.rerender(grid(['A', 'B', 'C']));
    for (const row of screen.getByRole('rowgroup', { name: 'Selected model totals' }).querySelectorAll('tr')) {
      expect(row).toHaveTextContent('0 of 3 models selected');
      for (const cell of within(row).getAllByRole('cell')) {
        expect(cell.querySelector('.pwb-v')?.textContent).toBe('');
        expect(cell).toHaveAttribute('title', '0');
        expect(cell.querySelector('s')).toBeNull();
      }
    }
    view.rerender(grid([]));
    expectTotals(all, mode);
  });

  it.each(['report', 'planning'] as const)('main %s: View selection alone controls totals, warnings and zero scope', async mode => {
    await panel();
    if (mode === 'report') click('Back to report');
    expectTotals(all, mode);
    click('View'); toggle('B');
    expectTotals(subset, mode);
    fireEvent.change(viewPanel().getByRole('searchbox', { name: 'Search models' }), { target: { value: 'A model' } });
    expect(viewPanel().getByTestId('planning-view-model-count')).toHaveTextContent('2 of 3 models selected');
    expectTotals(subset, mode); // checkbox-list search does not select/deselect anything
    click('Collapse all');
    expectTotals(subset, mode);
    fireEvent.scroll(screen.getByRole('region', { name: 'Planning workbook grid' }), { target: { scrollTop: 900, scrollLeft: 400 } });
    expectTotals(subset, mode);
    click(/^Short/); // neither A nor C is short; all selected contributions and warning still count
    expect(screen.queryByRole('rowgroup', { name: /^Part / })).not.toBeInTheDocument();
    expectTotals(subset, mode);
    expect(screen.getByTestId('mps-dollar-note')).toHaveTextContent('1 selected model has');
    expect(screen.getByTestId('mps-dollar-note')).toHaveTextContent('including models hidden by Short/Changed filters');
    if (mode === 'planning') {
      click(/^Changed/); expectTotals(subset, mode);
      expect(screen.getAllByRole('rowgroup', { name: /^Part / })).toHaveLength(2);
    }
    // Metric rows and which total rows are drawn do not change the remaining totals.
    fireEvent.click(viewPanel().getByRole('checkbox', { name: 'SO $' }));
    fireEvent.click(viewPanel().getByRole('checkbox', { name: 'Total MPS $' }));
    expect(values(totalNames[1])).toEqual(subset.revenue);
    fireEvent.click(viewPanel().getByRole('checkbox', { name: 'Total MPS $' }));
    expectTotals(subset, mode);
    fireEvent.change(viewPanel().getByRole('searchbox', { name: 'Search models' }), { target: { value: '' } });
    toggle('A'); toggle('C');
    expect(screen.queryByTestId('mps-dollar-note')).not.toBeInTheDocument();
    for (const row of screen.getByRole('rowgroup', { name: 'Selected model totals' }).querySelectorAll('tr')) {
      expect(row).toHaveTextContent('0 of 3 models selected');
      expect(within(row).getAllByRole('cell').every(c => c.title === '0' && c.textContent === '')).toBe(true);
    }
    fireEvent.click(viewPanel().getByRole('button', { name: 'Show all' }));
    expectTotals(all, mode);
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
    expect(getWorkbook).toHaveBeenCalledTimes(1);
  });

  it('standalone selection preserves fractional/large exact values beyond Number precision', () => {
    const huge = model('HUGE', '0.1', ['9007199254740993.125', '0.125'], ['900719925474099.3125', '0.0125'],
      ['9007199254740993.125', '0.125'], ['900719925474099.3125', '0.0125']);
    const tiny = model('TINY', '0.1', ['0.875', '0.875'], ['0.0875', '0.0875'], ['0.875', '0.875'], ['0.0875', '0.0875']);
    const wb = { ...workbook, parts: [huge, tiny] };
    const grid = (hiddenModels: string[]) => <PlanningWorkbookGrid workbook={wb} settings={settings} mode="planning" entries={{ h: { part: 'HUGE', weekLabel: labels[0], text: '1' } }} prefs={{ ...prefs, hiddenModels }} />;
    const view = render(grid(['TINY']));
    expect(values(totalNames[0]).slice(0, 4)).toEqual(['9,007,199,254,740,993.125', '0.125', '', '9,007,199,254,740,993.25']);
    expect(cells(totalNames[2])[3]).toHaveAttribute('title', 'Proposed 9,007,199,254,740,994.25; loaded 9,007,199,254,740,993.25');
    expect(cells(totalNames[1])[3]).toHaveAttribute('title', '900719925474099.325');
    view.rerender(grid([]));
    expect(values(totalNames[0]).slice(0, 4)).toEqual(['9,007,199,254,740,994', '1', '', '9,007,199,254,740,995']);
    expect(cells(totalNames[2])[3]).toHaveAttribute('title', 'Proposed 9,007,199,254,740,996; loaded 9,007,199,254,740,995');
  });

  it('counts an unchanged selected no-list-price model even when Changed hides its block', async () => {
    await panel();
    click('Remove adjustment for C, week of 9/21/2026');
    click(/^Changed/);
    expect(screen.queryByRole('rowgroup', { name: 'Part C' })).not.toBeInTheDocument();
    expect(screen.getByTestId('mps-dollar-note')).toHaveTextContent('1 selected model has');
    expect(values(totalNames[0])).toEqual(all.so);
    // C still contributes loaded MPS in week 1 and Q1; its removed +3 does not.
    expect(values(totalNames[2])).toEqual(['224', '347', '446', '1,017', '556', '556']);
    click('View'); toggle('C');
    expect(screen.queryByTestId('mps-dollar-note')).not.toBeInTheDocument();
    expect(values(totalNames[2])).toEqual(['24', '47', '46', '117', '56', '56']);
  });

  it('unselected proposals remain in Changes/drafts, block export on error and reach full-workspace export', async () => {
    saveDraft.mockResolvedValue({ savedAtUtc: '2026-09-16T12:01:00Z' });
    await panel();
    stage('B', '9/14/2026', '-31'); // negative proposed MPS; hide it, do not hide validation
    click('View'); toggle('B');
    expectTotals(subset, 'planning');
    click(/^Changes/);
    expect(screen.getByRole('list', { name: 'Staged changes' })).toHaveTextContent('Model hidden in View. The change is kept.');
    expect(screen.getByRole('list', { name: 'Staged changes' })).toHaveTextContent('Negative Proposed MPS');
    click('Save draft');
    await waitFor(() => expect(saveDraft).toHaveBeenCalledTimes(1));
    const request = saveDraft.mock.calls[0][1];
    expect(request.entries).toHaveLength(4);
    expect(request.entries).toEqual(expect.arrayContaining([expect.objectContaining({ part: 'B', adjustment: '-31' })]));
    click('Review and export');
    expect(await screen.findByRole('table', { name: 'Adjustments needing attention' })).toHaveTextContent('B');
    expect(exportFile).not.toHaveBeenCalled();
    fireEvent.click(within(screen.getByRole('dialog', { name: 'Review and export' })).getAllByRole('button', { name: 'Close' })[0]);
    click(/View/); toggle('B');
    stage('B', '9/14/2026', '10');
    toggle('B');
    const contentBase64 = Buffer.from('A,2,9/14/2026\r\nA,-1,9/28/2026\r\nB,10,9/14/2026\r\nC,3,9/21/2026\r\n').toString('base64');
    let resolve!: (result: object) => void;
    exportFile.mockReturnValue(new Promise(r => { resolve = r; }));
    click('Review and export');
    await waitFor(() => expect(exportFile).toHaveBeenCalledWith('ws', { mpsSnapshotId: 'snap', site: 'S1', entries: [
      { part: 'A', weekLabel: labels[0], adjustment: '2' }, { part: 'A', weekLabel: labels[2], adjustment: '-1' },
      { part: 'B', weekLabel: labels[0], adjustment: '10' }, { part: 'C', weekLabel: labels[1], adjustment: '3' },
    ] }));
    // Change only View while preparation is pending: no edit/session/context invalidation.
    toggle('A'); toggle('C');
    await act(async () => { resolve({ fileName: 'test.csv', contentBase64, rowCount: 4, mpsSnapshotId: 'snap', site: 'S1' }); });
    expect(await screen.findByRole('table', { name: 'Mass Updater file preview' })).toHaveTextContent('B');
    toggle('B'); // also change View after preparation; the same bytes remain savable
    saveFile.mockResolvedValue({ kind: 'saved', fileName: 'test.csv' });
    click(/^Save As/);
    await waitFor(() => expect(saveFile).toHaveBeenCalledWith(contentBase64, 'test.csv', 'Mass Updater CSV'));
    expect(await screen.findByText('Clear the staged adjustments included in this export?')).toBeInTheDocument();
    click('Keep');
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('4 changes');
    expect(saveDraft).toHaveBeenCalledTimes(1);
  });
});

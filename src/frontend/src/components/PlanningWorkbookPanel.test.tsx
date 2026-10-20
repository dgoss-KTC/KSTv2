import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../api/client';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from '../fiscal/fiscalCalendar';
import type { PlanningPart, PlanningWorkbook } from '../planningWorkbook/planningWorkbookSummary';
import { fixtureToWorkbook, type Stage14Fixture } from '../planningWorkbook/stage14FixtureAdapter.testSupport';
import type { PlanningViewPreferences } from '../planningWorkbook/planningView';
import { PlanningWorkbookGrid, PlanningWorkbookPanel } from './PlanningWorkbookPanel';

const getWorkbook = vi.fn();
const refreshWorkbook = vi.fn();
const draftPresence = vi.fn();
const loadDraft = vi.fn();
const saveDraft = vi.fn();
const deleteDraft = vi.fn();
vi.mock('../api/tauri-bridge', () => ({ resolveBackendBaseUrl: () => Promise.resolve('http://localhost'), isRunningInTauri: () => false }));
// Only the workbook and Planning draft methods exist: any MPS or Open Orders call would throw.
vi.mock('../api/client', async (original) => ({ ...(await original<typeof import('../api/client')>()),
  ApiClient: class {
    getPlanningWorkbook = getWorkbook; refreshPlanningWorkbook = refreshWorkbook;
    getPlanningWorkbookDraftPresence = draftPresence; loadPlanningWorkbookDraft = loadDraft;
    savePlanningWorkbookDraft = saveDraft; deletePlanningWorkbookDraft = deleteDraft;
  },
}));

const fixture = JSON.parse(readFileSync(resolve(process.cwd(), '../../docs/reference/STAGE_14_PLANNING_WORKBOOK_DETERMINISTIC_FIXTURES.json'), 'utf8')) as Stage14Fixture;

function label(index: number): string {
  const d = new Date(2026, 8, 14 + 7 * index);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function part(name: string, overrides: Partial<PlanningPart> = {}, weeks = 12): PlanningPart {
  const bucket = (kind: string, weekLabel: string | null) => ({ kind, weekLabel, salesOrderQuantity: '0', salesOrderDollars: '0',
    mps: '0', mpsDollars: '0', forecast: '0', estimatedOnHand: '5' });
  return {
    part: name, description: `${name} description`, netQuantityOnHand: '5', priceTiers: [{ minimumOrderQuantity: '1', unitPrice: '2.5' }],
    selectedPrice: '2.5', isPriced: true, falldown: bucket('falldown', null),
    weeks: Array.from({ length: weeks }, (_, i) => bucket('weekly', label(i))),
    undated: { salesOrderQuantity: '0', salesOrderDollars: '0', lineCount: 0 }, ...overrides,
  };
}

function workbook(horizon = 12, overrides: Partial<PlanningWorkbook> = {}, snapshot = 'snap-1'): PlanningWorkbook {
  return {
    workspaceId: 'ws-1', site: 'S1', workbookSnapshotId: `wb-${snapshot}-${horizon}`, mpsSnapshotId: snapshot, openOrdersSnapshotId: 'oo-1',
    acquiredAtUtc: '2026-09-16T16:05:00Z', priceEffectiveDate: '2026-09-16', priceSelectionPolicy: 'firstTier', today: '2026-09-16',
    horizonWeeks: horizon, isStale: false, warning: null, weekLabels: Array.from({ length: horizon }, (_, i) => label(i)),
    minimumSelectableHorizonWeeks: 12, maximumHorizonWeeks: 30,
    parts: [part('P-9', {}, horizon), part('P-1', {}, horizon)], ...overrides,
  };
}

function deferred<T>() {
  let resolveFn!: (value: T) => void; let rejectFn!: (error: unknown) => void;
  const promise = new Promise<T>((res, rej) => { resolveFn = res; rejectFn = rej; });
  return { promise, resolve: resolveFn, reject: rejectFn };
}

const problem = (status: number, body: object) => new ApiError(status, 'http://localhost/x', JSON.stringify(body));

/** Every metric row and every workspace total (View tailoring). */
const ALL: PlanningViewPreferences = {
  hiddenModels: [], rows: ['so', 'soDollars', 'mps', 'mpsDollars', 'forecast', 'onHand'], totals: ['so', 'soDollars', 'mps', 'mpsDollars', 'adjust'], collapsed: [], tintRows: true,
};
const horizonValue = () => screen.getByRole('spinbutton', { name: 'Planning horizon in weeks' }).getAttribute('aria-valuenow');
/** Week column headers as m/d/yyyy (headers show abbreviated Monday labels; the full date is in data-week and the accessible name). */
const weekHeaders = () => screen.getAllByRole('columnheader').map(h => h.getAttribute('data-week')).filter((w): w is string => !!w)
  .map(w => { const [y, m, d] = w.split('-').map(Number); return `${m}/${d}/${y}`; });
const quarterHeader = (label: string) => screen.getAllByRole('columnheader').map(h => h.textContent ?? '').find(t => t.includes(`${label} total`)) ?? '';
const moreWeeks = () => fireEvent.click(screen.getByRole('button', { name: 'More weeks' }));

beforeEach(() => {
  getWorkbook.mockReset(); refreshWorkbook.mockReset(); window.localStorage.clear();
  draftPresence.mockReset(); loadDraft.mockReset(); saveDraft.mockReset(); deleteDraft.mockReset();
  draftPresence.mockResolvedValue({ exists: false });
});

describe('Planning Workbook panel: horizon', () => {
  it('requests 12 weeks by default, independent of the Dashboard MPS horizon', async () => {
    window.localStorage.setItem('kst.mpsDisplayPreferences.v1', JSON.stringify({ dateBasis: 'dueDate', horizonWeeks: 40 }));
    getWorkbook.mockImplementation((_id: string, _snap: string, h: number) => Promise.resolve(workbook(h)));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    expect(getWorkbook).toHaveBeenCalledWith('ws-1', 'snap-1', 12);
    expect(horizonValue()).toBe('12');
    expect(screen.getByTestId('planning-horizon-range')).toHaveTextContent('12\u201330 weeks');
  });

  it('bounds the stepper to the reported range and requests the new horizon', async () => {
    getWorkbook.mockImplementation((_id: string, _snap: string, h: number) => Promise.resolve(workbook(h)));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    const stepper = screen.getByRole('spinbutton', { name: 'Planning horizon in weeks' });
    expect(stepper).toHaveAttribute('aria-valuemin', '12');
    expect(stepper).toHaveAttribute('aria-valuemax', '30');
    expect(screen.getByRole('button', { name: 'Fewer weeks' })).toBeDisabled();
    fireEvent.keyDown(stepper, { key: 'ArrowDown' });
    expect(horizonValue()).toBe('12');
    fireEvent.keyDown(stepper, { key: 'End' });
    expect(horizonValue()).toBe('30');
    await waitFor(() => expect(getWorkbook).toHaveBeenLastCalledWith('ws-1', 'snap-1', 30));
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    expect(screen.getByRole('button', { name: 'More weeks' })).toBeDisabled();
    fireEvent.keyDown(stepper, { key: 'ArrowUp' });
    expect(horizonValue()).toBe('30');
    expect(window.localStorage.getItem('kst.planningWorkbook.horizonWeeks.v1')).toBe('30');
  });

  it('clamps a saved selection when the MPS-derived range shrinks', async () => {
    window.localStorage.setItem('kst.planningWorkbook.horizonWeeks.v1', '40');
    getWorkbook.mockImplementation((_id: string, _snap: string, h: number) => h > 20
      ? Promise.reject(problem(400, { title: 'One or more validation errors occurred.', maximumHorizonWeeks: 20 }))
      : Promise.resolve(workbook(h, { maximumHorizonWeeks: 20 })));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    expect(getWorkbook.mock.calls.map(c => c[2])).toEqual([40, 20]);
    expect(horizonValue()).toBe('20');
    expect(weekHeaders()).toHaveLength(20);
  });
});

describe('Planning Workbook panel: grid presentation', () => {
  it('renders the accepted fixture across its quarter boundary in retained part order', () => {
    const data = fixtureToWorkbook(fixture);
    render(<PlanningWorkbookGrid workbook={data} settings={DEFAULT_FISCAL_CALENDAR_SETTINGS} prefs={ALL} />);
    expect(screen.getAllByRole('columnheader')[1]).toHaveTextContent('Falldown');
    expect(weekHeaders()).toEqual(['9/14/2026', '9/21/2026', '9/28/2026', '10/5/2026']);
    // Abbreviated Monday labels with current-week emphasis; the full date stays in the accessible name.
    const first = screen.getAllByRole('columnheader').find(h => h.getAttribute('data-week') === '2026-09-14')!;
    expect(first).toHaveTextContent('Sep 14this week');
    expect(first).toHaveTextContent('week of 9/14/2026');
    expect(first).toHaveClass('pwb-head--current');
    expect(quarterHeader('FY27 Q1')).toContain('Q1 total');
    expect(quarterHeader('FY27 Q1')).toContain('Includes Falldown once');
    expect(quarterHeader('FY27 Q2')).not.toContain('Includes Falldown');
    const blocks = screen.getAllByRole('rowgroup').filter(g => g.getAttribute('aria-label')?.startsWith('Part '));
    expect(blocks.map(b => b.getAttribute('aria-label'))).toEqual(['Part P-2', 'Part P-1', 'Part P-3']);

    const p1 = within(screen.getByRole('rowgroup', { name: 'Part P-1' }));
    const soRow = p1.getByRole('row', { name: /^Sales orders/ });
    // Falldown, 9/14, 9/21, Q1 total (incl. Falldown), 9/28, 10/5, Q2 total. Exact zeros display blank.
    expect(within(soRow).getAllByRole('cell').map(c => c.textContent)).toEqual(['10', '80', '70', '160', '', '', '']);
    const inventory = p1.getByRole('row', { name: /^Projected on hand/ });
    expect(within(inventory).getAllByRole('cell').map(c => c.textContent)).toEqual(['60', '80', '85.5', '85.5', '185.5', '185.5', '185.5']);
    const forecast = p1.getByRole('row', { name: /^Forecast \(QAD\)/ });
    expect(within(forecast).getAllByRole('cell').map(c => c.textContent)).toEqual(['15', '', '40', '', '40', '', '']);
    expect(screen.queryByText(/undated/i)).not.toBeInTheDocument();
  });

  it('a part with no list price shows $0 and a No list price badge; workspace MPS $ totals still sum every part (amendment 2026-10-07)', () => {
    render(<PlanningWorkbookGrid workbook={fixtureToWorkbook(fixture)} settings={DEFAULT_FISCAL_CALENDAR_SETTINGS} prefs={ALL} />);
    const p3 = within(screen.getByRole('rowgroup', { name: 'Part P-3' }));
    expect(p3.getByText('No list price')).toBeInTheDocument();
    expect(p3.queryByText('Unpriced')).not.toBeInTheDocument();
    expect(p3.getByText('$0.0000')).toBeInTheDocument();
    // Exact-zero dollars follow the ordinary zero presentation: blank, never a dash or unpriced styling.
    expect(within(p3.getByRole('row', { name: /^MPS \$/ })).getAllByRole('cell').every(c => c.textContent === '')).toBe(true);
    const p2 = within(screen.getByRole('rowgroup', { name: 'Part P-2' }));
    expect(p2.getByText('$0.0000')).toBeInTheDocument();
    expect(p2.queryByText('No list price')).not.toBeInTheDocument(); // a genuine zero price is a priced part
    const p2MpsDollars = within(p2.getByRole('row', { name: /^MPS \$/ })).getAllByRole('cell');
    expect(p2MpsDollars.every(c => c.textContent === '')).toBe(true);
    const totals = within(screen.getByRole('rowgroup', { name: 'Selected model totals' }));
    // Mixed workspace: P-1 prices normally, P-2 (zero price) and P-3 (no list price) contribute $0.
    const mpsDollarTotals = within(totals.getByRole('row', { name: /^Total MPS \$/ })).getAllByRole('cell');
    expect(mpsDollarTotals[3]).toHaveTextContent('$513.75');
    expect(mpsDollarTotals.every(c => c.textContent !== '\u2014')).toBe(true);
    expect(within(totals.getByRole('row', { name: /^Total SO \$/ })).getAllByRole('cell')[3]).toHaveTextContent('$1,635.57');
    expect(within(totals.getByRole('row', { name: /^Total MPS (?!\$)/ })).getAllByRole('cell')[6]).toHaveTextContent('210');
    expect(totals.queryByRole('row', { name: /on hand/i })).not.toBeInTheDocument();
    expect(screen.getByTestId('mps-dollar-note')).toHaveTextContent('1 at $0');
    expect(screen.getByTestId('mps-dollar-note')).toHaveTextContent('1 selected model has no eligible QAD list price and counts as $0 in selected model MPS $ totals');
  });

  it('totals workspace MPS $ when every part is priced (zero price included) and highlights negative inventory', () => {
    const data = fixtureToWorkbook(fixture);
    render(<PlanningWorkbookGrid workbook={{ ...data, parts: data.parts.filter(p => p.isPriced) }} settings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    const totals = within(screen.getByRole('rowgroup', { name: 'Selected model totals' }));
    // Default totals: the two dollar totals are pinned.
    expect(totals.getAllByRole('row').map(r => r.querySelector('th')?.firstChild?.nextSibling?.textContent)).toEqual(['Total SO $', 'Total MPS $']);
    expect(within(totals.getByRole('row', { name: /^Total MPS \$/ })).getAllByRole('cell')[3]).toHaveTextContent('$513.75');
    expect(screen.queryByTestId('mps-dollar-note')).not.toBeInTheDocument();
    const p2Inventory = within(screen.getByRole('rowgroup', { name: 'Part P-2' })).getByRole('row', { name: /^Projected on hand/ });
    const first = within(p2Inventory).getAllByRole('cell')[0];
    expect(first).toHaveTextContent('-20');
    expect(first).toHaveClass('pwb-cell--negative');
  });

  it('recomputes quarters locally when fiscal settings change, without a source request', async () => {
    getWorkbook.mockImplementation((_id: string, _snap: string, h: number) => Promise.resolve(workbook(h)));
    const { rerender } = render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    expect(quarterHeader('FY27 Q1')).toContain('2 of 13');
    rerender(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={fixture.fiscalExceptionCase.fiscalSettings} />);
    expect(quarterHeader('FY27 Q1')).toContain('3 of 14');
    expect(getWorkbook).toHaveBeenCalledTimes(1);
    expect(refreshWorkbook).not.toHaveBeenCalled();
  });
});

describe('Planning Workbook panel: states and refresh', () => {
  it('refreshes through the workbook refresh endpoint only and explains the Open Orders effect', async () => {
    getWorkbook.mockResolvedValue(workbook());
    refreshWorkbook.mockResolvedValue({ ...workbook(), workbookSnapshotId: 'wb-2', acquiredAtUtc: '2026-09-16T17:00:00Z' });
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    expect(screen.getByText(/also refreshes this workspace's Customer Open Orders report/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Planning' }));
    await waitFor(() => expect(refreshWorkbook).toHaveBeenCalledWith('ws-1', 'snap-1', 12));
    expect(getWorkbook).toHaveBeenCalledTimes(1);
  });

  it('shows stale data with acquisition and price dates', async () => {
    getWorkbook.mockResolvedValue(workbook(12, { isStale: true, warning: 'Showing the last known Planning Workbook data. A newer refresh could not be completed.', priceEffectiveDate: '2026-09-15', acquiredAtUtc: '2026-09-15T16:05:00Z' }));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Showing the last known Planning Workbook data');
    expect(alert).toHaveTextContent('prices as of 9/15/2026');
    expect(alert).toHaveTextContent(/Data acquired 9\/15\/2026/);
  });

  it('keeps compatible data visible with a warning when refresh fails', async () => {
    getWorkbook.mockResolvedValue(workbook());
    refreshWorkbook.mockRejectedValue(problem(503, { title: 'Planning Workbook unavailable' }));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Planning' }));
    expect(await screen.findByText(/Refresh Planning could not be completed/)).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Planning workbook grid' })).toBeInTheDocument();
  });

  it.each([
    [503, {}, /currently unavailable/, true],
    [404, {}, /workspace was not found/, false],
    [409, { title: 'MPS data not loaded' }, /MPS data is not loaded/, true],
    [409, { title: 'MPS snapshot changed' }, /MPS data changed/, true],
  ])('maps HTTP %i to its state', async (status, body, text, retry) => {
    getWorkbook.mockRejectedValueOnce(problem(status, body)).mockResolvedValue(workbook());
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    expect(await screen.findByText(text)).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Planning workbook grid' })).not.toBeInTheDocument();
    if (retry) {
      fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
      await screen.findByRole('region', { name: 'Planning workbook grid' });
    } else {
      expect(screen.queryByRole('button', { name: 'Retry' })).not.toBeInTheDocument();
    }
  });

  it('shows the empty-scope and missing-MPS states without loading MPS', async () => {
    getWorkbook.mockResolvedValue(workbook(12, { parts: [] }));
    const { rerender } = render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    expect(await screen.findByText(/No parts resolved/)).toBeInTheDocument();
    rerender(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId={null} fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    expect(screen.getByText(/MPS data is not loaded for this workspace/)).toBeInTheDocument();
    expect(screen.queryByText(/No parts resolved/)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Refresh Planning' })).toBeDisabled();
  });
});

describe('Planning Workbook panel: refresh while loading (review correction 1)', () => {
  it('does not let Refresh supersede a pending initial load, and a later failed refresh never leaves loading', async () => {
    const initial = deferred<PlanningWorkbook>();
    getWorkbook.mockReturnValueOnce(initial.promise);
    refreshWorkbook.mockRejectedValue(problem(503, { title: 'Planning Workbook unavailable' }));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    const button = screen.getByRole('button', { name: 'Refresh Planning' });
    expect(screen.getByText(/Loading Planning Workbook/)).toBeInTheDocument();
    expect(button).toBeDisabled();
    fireEvent.click(button);
    expect(refreshWorkbook).not.toHaveBeenCalled();

    await act(async () => { initial.resolve(workbook()); await Promise.resolve(); });
    expect(await screen.findByRole('region', { name: 'Planning workbook grid' })).toBeInTheDocument();
    expect(screen.queryByText(/Loading Planning Workbook/)).not.toBeInTheDocument();

    // A failed refresh of a loaded workbook keeps it visible with a warning.
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Planning' }));
    expect(await screen.findByText(/Refresh Planning could not be completed/)).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Planning workbook grid' })).toBeInTheDocument();
    expect(screen.queryByText(/Loading Planning Workbook/)).not.toBeInTheDocument();
  });

  it('disables Refresh while a changed horizon loads and surfaces that load failing instead of loading forever', async () => {
    const thirteen = deferred<PlanningWorkbook>();
    getWorkbook.mockImplementation((_id: string, _snap: string, h: number) => h === 13 ? thirteen.promise : Promise.resolve(workbook(h)));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    moreWeeks();
    expect(screen.getByRole('button', { name: 'Refresh Planning' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Planning' }));
    expect(refreshWorkbook).not.toHaveBeenCalled();
    await act(async () => { thirteen.reject(problem(503, {})); await Promise.resolve(); });
    expect(await screen.findByText(/currently unavailable/)).toBeInTheDocument();
    expect(screen.queryByText(/Loading Planning Workbook/)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument();
  });

  it('ends a refresh superseded by a horizon change in the new load, not in a loading state', async () => {
    const refreshDeferred = deferred<PlanningWorkbook>();
    getWorkbook.mockImplementation((_id: string, _snap: string, h: number) => Promise.resolve(workbook(h)));
    refreshWorkbook.mockReturnValue(refreshDeferred.promise);
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Planning' }));
    moreWeeks();
    await waitFor(() => expect(weekHeaders()).toHaveLength(13));
    await act(async () => { refreshDeferred.reject(problem(503, {})); await Promise.resolve(); });
    expect(screen.queryByText(/Refresh Planning could not be completed/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Loading Planning Workbook/)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Refresh Planning' })).toBeEnabled();
  });
});

describe('Planning Workbook panel: business-week rollover (review correction 2)', () => {
  // Only Date is faked; React Testing Library timers stay real.
  beforeEach(() => { vi.useFakeTimers({ toFake: ['Date'] }); vi.setSystemTime(new Date(2026, 8, 19, 23, 50)); });
  afterEach(() => { vi.useRealTimers(); Reflect.deleteProperty(document, 'visibilityState'); });

  /** Workbook whose week 0 is `startWeek` weeks after 9/14/2026. */
  function weekWorkbook(startWeek: number, horizon: number, maximum: number, partName = 'P-9'): PlanningWorkbook {
    const labels = Array.from({ length: horizon }, (_, i) => label(startWeek + i));
    const base = workbook(horizon, { weekLabels: labels, maximumHorizonWeeks: maximum, today: label(startWeek) });
    const template = part(partName, {}, 1).weeks[0];
    return { ...base, parts: [{ ...part(partName, {}, horizon), weeks: labels.map(l => ({ ...template, weekLabel: l })) }] };
  }
  const rolled = () => Date.now() >= new Date(2026, 8, 20).getTime();

  it('requests the current projection by GET after the business week changes (focus after sleep), never POST refresh', async () => {
    getWorkbook.mockImplementation((_id: string, _snap: string, h: number) => Promise.resolve(weekWorkbook(rolled() ? 1 : 0, h, 30)));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    expect(weekHeaders()[0]).toBe('9/14/2026');

    // Same week: a focus event does not reload.
    fireEvent(window, new Event('focus'));
    expect(getWorkbook).toHaveBeenCalledTimes(1);

    vi.setSystemTime(new Date(2026, 8, 21, 8, 0)); // computer slept over the weekend
    fireEvent(window, new Event('focus'));
    await waitFor(() => expect(weekHeaders()[0]).toBe('9/21/2026'));
    expect(getWorkbook).toHaveBeenCalledTimes(2);
    expect(getWorkbook).toHaveBeenLastCalledWith('ws-1', 'snap-1', 12);
    expect(refreshWorkbook).not.toHaveBeenCalled();
  });

  it('detects rollover on visibility change and clamps the selection when the maximum shrinks', async () => {
    window.localStorage.setItem('kst.planningWorkbook.horizonWeeks.v1', '20');
    getWorkbook.mockImplementation((_id: string, _snap: string, h: number) => {
      const max = rolled() ? 19 : 20;
      return h > max
        ? Promise.reject(problem(400, { maximumHorizonWeeks: max }))
        : Promise.resolve(weekWorkbook(rolled() ? 1 : 0, h, max));
    });
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    expect(screen.getByTestId('planning-horizon-range')).toHaveTextContent('12\u201320 weeks');

    vi.setSystemTime(new Date(2026, 8, 20, 0, 5));
    Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'visible' });
    fireEvent(document, new Event('visibilitychange'));
    await waitFor(() => expect(horizonValue()).toBe('19'));
    await waitFor(() => expect(weekHeaders()).toHaveLength(19));
    expect(weekHeaders()[0]).toBe('9/21/2026');
    expect(screen.getByTestId('planning-horizon-range')).toHaveTextContent('12\u201319 weeks');
    expect(getWorkbook.mock.calls.map(c => c[2])).toEqual([20, 20, 19]);
    expect(refreshWorkbook).not.toHaveBeenCalled();
  });

  it('ignores a late response requested before the rollover', async () => {
    const beforeRollover = deferred<PlanningWorkbook>();
    getWorkbook.mockReturnValueOnce(beforeRollover.promise)
      .mockImplementation((_id: string, _snap: string, h: number) => Promise.resolve(weekWorkbook(1, h, 30, 'AFTER')));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    vi.setSystemTime(new Date(2026, 8, 20, 0, 1));
    fireEvent(window, new Event('focus'));
    await screen.findByRole('rowgroup', { name: 'Part AFTER' });
    await act(async () => { beforeRollover.resolve(weekWorkbook(0, 12, 30, 'BEFORE')); await Promise.resolve(); });
    expect(screen.queryByRole('rowgroup', { name: 'Part BEFORE' })).not.toBeInTheDocument();
    expect(weekHeaders()[0]).toBe('9/21/2026');
  });
});

describe('Planning Workbook panel: neutral rows with colour keys (redesign; supersedes 14.4 row bands)', () => {
  it('keys SO rows as demand and MPS rows as supply in part blocks and workspace totals, without row bands', () => {
    render(<PlanningWorkbookGrid workbook={fixtureToWorkbook(fixture)} settings={DEFAULT_FISCAL_CALENDAR_SETTINGS} prefs={ALL} />);
    const key = (row: HTMLElement) => row.querySelector('.pwb-key')?.className.replace('pwb-key ', '');
    const p3 = within(screen.getByRole('rowgroup', { name: 'Part P-3' }));
    expect(key(p3.getByRole('row', { name: /^Sales orders/ }))).toBe('pwb-key--demand');
    expect(key(p3.getByRole('row', { name: /^SO \$/ }))).toBe('pwb-key--demand');
    expect(key(p3.getByRole('row', { name: /^MPS (?!\$)/ }))).toBe('pwb-key--supply');
    expect(key(p3.getByRole('row', { name: /^MPS \$/ }))).toBe('pwb-key--supply');
    expect(key(p3.getByRole('row', { name: /^Forecast/ }))).toBe('pwb-key--forecast');
    expect(key(p3.getByRole('row', { name: /^Projected on hand/ }))).toBe('pwb-key--onHand');
    const totals = within(screen.getByRole('rowgroup', { name: 'Selected model totals' }));
    expect(key(totals.getByRole('row', { name: /^Total SO \$/ }))).toBe('pwb-key--demand');
    expect(key(totals.getByRole('row', { name: /^Total MPS \$/ }))).toBe('pwb-key--supply');
    for (const row of screen.getAllByRole('row')) expect(row.className).not.toMatch(/pwb-row--(demand|supply|neutral)/);
    // Mixed workspace: the no-list-price part adds $0, so totals are never blank because of it.
    expect(within(totals.getByRole('row', { name: /^Total MPS \$/ })).getAllByRole('cell')[3]).toHaveTextContent('$513.75');
  });
});

describe('Planning Workbook panel: zero-value display (14.4 owner amendment)', () => {
  const flows = (overrides: Partial<PlanningPart['falldown']>) => ({
    kind: 'weekly', weekLabel: null, salesOrderQuantity: '0', salesOrderDollars: '0.00', mps: '0.000', mpsDollars: '0.0000',
    forecast: '-0.000', estimatedOnHand: '0', ...overrides,
  });
  /** Week 0: exact zeros in every form; week 1: nonzero values incl. negatives and a sub-cent dollar. */
  function zeroWorkbook(parts: PlanningPart[]): PlanningWorkbook {
    return workbook(2, { weekLabels: [label(0), label(1)], parts });
  }
  function zeroPart(name: string, priced: boolean, price: string): PlanningPart {
    // `priced: false` = the lookup found no eligible list price, so the price is exactly $0 (amendment 2026-10-07).
    return part(name, {
      netQuantityOnHand: '0', selectedPrice: price, isPriced: priced, priceTiers: priced ? [{ minimumOrderQuantity: '1', unitPrice: price }] : [],
      falldown: { ...flows({ kind: 'falldown', mpsDollars: priced ? '-0.00' : '0.00' }) },
      weeks: [
        { ...flows({ weekLabel: label(0), mpsDollars: priced ? '0' : '0' }) },
        { ...flows({ weekLabel: label(1), salesOrderQuantity: '-2', salesOrderDollars: '0.004', mps: '0.5',
          mpsDollars: priced ? '-0.004' : '0.0', forecast: '3', estimatedOnHand: '-1.5' }) },
      ],
    });
  }
  const row = (group: string, name: RegExp) => within(within(screen.getByRole('rowgroup', { name: group })).getByRole('row', { name }));

  it('blanks exact zeros (0, 0.00, -0.000) in Falldown, weekly, quarter and workspace-total cells; nonzero values keep their formatting', () => {
    render(<PlanningWorkbookGrid workbook={zeroWorkbook([zeroPart('Z-1', true, '1.25')])} settings={DEFAULT_FISCAL_CALENDAR_SETTINGS} prefs={ALL} />);
    // Columns: Falldown, 9/14, 9/21, FY27 Q1 Total (Falldown + both weeks).
    expect(row('Part Z-1', /^Sales orders/).getAllByRole('cell').map(c => c.textContent)).toEqual(['', '', '-2', '-2']);
    expect(row('Part Z-1', /^SO \$/).getAllByRole('cell').map(c => c.textContent)).toEqual(['', '', '$0.00', '$0.00']);
    expect(row('Part Z-1', /^MPS (?!\$)/).getAllByRole('cell').map(c => c.textContent)).toEqual(['', '', '0.5', '0.5']);
    expect(row('Part Z-1', /^MPS \$/).getAllByRole('cell').map(c => c.textContent)).toEqual(['', '', '$0.00', '$0.00']);
    expect(row('Part Z-1', /^Forecast/).getAllByRole('cell').map(c => c.textContent)).toEqual(['', '', '3', '']);
    const inventory = row('Part Z-1', /^Projected on hand/).getAllByRole('cell');
    expect(inventory.map(c => c.textContent)).toEqual(['', '', '-1.5', '-1.5']);
    expect(inventory[0]).not.toHaveClass('pwb-cell--negative'); // -0 is zero, not negative
    expect(inventory[2]).toHaveClass('pwb-cell--negative');
    expect(inventory[3]).toHaveClass('pwb-cell--negative');

    // Workspace totals (all parts priced): zeros blank, nonzero sums shown.
    expect(row('Selected model totals', /^Total sales orders/).getAllByRole('cell').map(c => c.textContent)).toEqual(['', '', '-2', '-2']);
    expect(row('Selected model totals', /^Total MPS \$/).getAllByRole('cell').map(c => c.textContent)).toEqual(['', '', '$0.00', '$0.00']);

    // Underlying exact values are untouched (kept as the cell title).
    expect(row('Part Z-1', /^SO \$/).getAllByRole('cell')[1]).toHaveAttribute('title', '0.00');
    // Part-header metadata still shows explicit zero quantities.
    expect(within(screen.getByRole('rowgroup', { name: 'Part Z-1' })).getByText('0', { selector: 'strong' })).toBeInTheDocument();
  });

  it('keeps zero-price parts priced, no-list-price parts at $0 (badge, blank zero cells, totals included) and cell styling', () => {
    render(<PlanningWorkbookGrid workbook={zeroWorkbook([zeroPart('ZP', true, '0'), zeroPart('UP', false, '0')])} settings={DEFAULT_FISCAL_CALENDAR_SETTINGS} prefs={ALL} />);
    const zp = within(screen.getByRole('rowgroup', { name: 'Part ZP' }));
    expect(zp.getByText('$0.0000')).toBeInTheDocument();
    expect(zp.queryByText('No list price')).not.toBeInTheDocument();
    const up = within(screen.getByRole('rowgroup', { name: 'Part UP' }));
    expect(up.getByText('No list price')).toBeInTheDocument();
    expect(up.getByText('$0.0000')).toBeInTheDocument();
    const upMpsDollars = row('Part UP', /^MPS \$/).getAllByRole('cell');
    expect(upMpsDollars.every(c => c.textContent === '' && !c.classList.contains('pwb-cell--unpriced'))).toBe(true);

    // All models selected: the no-list-price part contributes $0, nothing is a dash.
    const totalsMpsDollars = row('Selected model totals', /^Total MPS \$/).getAllByRole('cell');
    expect(totalsMpsDollars.map(c => c.textContent)).toEqual(['', '', '$0.00', '$0.00']);
    expect(totalsMpsDollars.every(c => !c.classList.contains('pwb-cell--unpriced'))).toBe(true);
    // Blank zero cells keep their row key, Falldown boundary and quarter styling.
    const soCells = row('Part ZP', /^Sales orders/).getAllByRole('cell');
    expect(soCells[0].closest('tr')?.querySelector('.pwb-key--demand')).not.toBeNull();
    expect(soCells[0]).toHaveClass('pwb-cell', 'pwb-cell--falldown');
    expect(soCells[0]).toHaveTextContent('');
    const mpsCells = row('Part ZP', /^MPS (?!\$)/).getAllByRole('cell');
    expect(mpsCells[1].closest('tr')?.querySelector('.pwb-key--supply')).not.toBeNull();
    expect(mpsCells[3]).toHaveClass('pwb-cell--quarter');
  });
});

describe('Planning Workbook panel: late responses', () => {
  it('ignores a late response for a previous MPS snapshot and clears incompatible data immediately', async () => {
    const first = deferred<PlanningWorkbook>();
    getWorkbook.mockImplementation((_id: string, snap: string) => snap === 'snap-1' ? first.promise : Promise.resolve(workbook(12, {}, 'snap-2')));
    const { rerender } = render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    rerender(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-2" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    await act(async () => { first.resolve(workbook(12, { parts: [part('LATE')] }, 'snap-1')); await Promise.resolve(); });
    expect(screen.queryByRole('rowgroup', { name: 'Part LATE' })).not.toBeInTheDocument();
    expect(screen.getByRole('rowgroup', { name: 'Part P-9' })).toBeInTheDocument();
  });

  it('never shows a previous horizon projection for a newly selected horizon', async () => {
    const thirteen = deferred<PlanningWorkbook>();
    getWorkbook.mockImplementation((_id: string, _snap: string, h: number) => h === 13 ? thirteen.promise : Promise.resolve(workbook(h)));
    render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    moreWeeks();
    expect(screen.queryByRole('region', { name: 'Planning workbook grid' })).not.toBeInTheDocument();
    expect(screen.getByText(/Loading Planning Workbook/)).toBeInTheDocument();
    await act(async () => { thirteen.resolve(workbook(13)); await Promise.resolve(); });
    expect(await screen.findByRole('region', { name: 'Planning workbook grid' })).toBeInTheDocument();
  });

  it('ignores a late response after the workspace changes', async () => {
    const first = deferred<PlanningWorkbook>();
    getWorkbook.mockImplementation((id: string) => id === 'ws-1' ? first.promise : Promise.resolve({ ...workbook(), workspaceId: 'ws-2', site: 'S2', parts: [part('WS2-PART')] }));
    const { rerender } = render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    rerender(<PlanningWorkbookPanel assignmentId="ws-2" site="S2" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('rowgroup', { name: 'Part WS2-PART' });
    await act(async () => { first.resolve(workbook()); await Promise.resolve(); });
    expect(screen.queryByRole('rowgroup', { name: 'Part P-9' })).not.toBeInTheDocument();
  });

  it('ignores a late refresh result after the snapshot changes', async () => {
    const refreshDeferred = deferred<PlanningWorkbook>();
    getWorkbook.mockImplementation((_id: string, snap: string) => Promise.resolve(workbook(12, snap === 'snap-2' ? { parts: [part('NEW')] } : {}, snap)));
    refreshWorkbook.mockReturnValue(refreshDeferred.promise);
    const { rerender } = render(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-1" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Planning' }));
    rerender(<PlanningWorkbookPanel assignmentId="ws-1" site="S1" snapshotId="snap-2" fiscalSettings={DEFAULT_FISCAL_CALENDAR_SETTINGS} />);
    await screen.findByRole('rowgroup', { name: 'Part NEW' });
    await act(async () => { refreshDeferred.resolve(workbook(12, { parts: [part('OLD')] })); await Promise.resolve(); });
    expect(screen.queryByRole('rowgroup', { name: 'Part OLD' })).not.toBeInTheDocument();
    expect(screen.getByRole('rowgroup', { name: 'Part NEW' })).toBeInTheDocument();
  });
});

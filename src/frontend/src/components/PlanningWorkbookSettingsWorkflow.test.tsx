import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';

/**
 * Stage 14.4 review correction 3: fiscal-setting changes verified through the real application
 * workflow (General → Fiscal Calendar → Add Exception, persisted by useFiscalCalendarSettings to local
 * storage) without the PlanningWorkbookPanel fiscalSettings test override. All traffic is mocked.
 */
vi.mock('@tauri-apps/api/event', () => ({ listen: vi.fn(async () => () => {}) }));

const status = {
  applicationName: "Keytronic Scheduler's Toolbox", applicationVersion: '0.1.0', backendFramework: '.NET 10',
  backendInstanceId: 'test-id', startedAt: '2026-09-16T12:00:00Z', currentTime: '2026-09-16T12:01:00Z',
  snapshot: { available: false, snapshotId: null, createdAt: null, status: 'notLoaded' }, dataSources: [],
  lastRefreshAttemptAt: null, lastSuccessfulRefreshAt: null,
};
const workspace = {
  assignmentId: 'ws-1', displayName: 'Line 1', site: 'NW', productLineFrom: null, productLineTo: null,
  parentParts: ['ABC100'], isTemporary: false, coverageEndsOn: null, isEnabled: true, sortOrder: 0,
};
const dashboard = {
  snapshot: { snapshotId: 'snap-1', createdAtUtc: '2026-09-16T12:00:00Z', lastSuccessfulRefreshAtUtc: '2026-09-16T12:00:00Z',
    status: 'current', workspaceId: 'ws-1', site: 'NW', resolvedParentPartCount: 1, sourceRowCount: 1,
    isRefreshInProgress: false, lastRefreshError: null },
  dateBasis: 'dueDate', horizonWeeks: 4,
  parts: [{ parentPart: 'ABC100', description: 'Widget Assembly', buckets: [
    { kind: 'falldown', weekLabel: null, quantity: 0, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
  ] }],
};

function label(i: number) {
  const d = new Date(2026, 8, 14 + 7 * i);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
function workbook(horizon: number) {
  const bucket = (kind: string, weekLabel: string | null) => ({ kind, weekLabel, salesOrderQuantity: '1', salesOrderDollars: '2',
    mps: '3', mpsDollars: '4', forecast: '0', estimatedOnHand: '5' });
  return {
    workspaceId: 'ws-1', site: 'NW', workbookSnapshotId: 'wb-1', mpsSnapshotId: 'snap-1', openOrdersSnapshotId: 'oo-1',
    acquiredAtUtc: '2026-09-16T16:00:00Z', priceEffectiveDate: '2026-09-16', priceSelectionPolicy: 'firstTier', today: '2026-09-16',
    horizonWeeks: horizon, isStale: false, warning: null, weekLabels: Array.from({ length: horizon }, (_, i) => label(i)),
    minimumSelectableHorizonWeeks: 12, maximumHorizonWeeks: 20,
    parts: [{ part: 'ABC100', description: 'Widget Assembly', netQuantityOnHand: '5', priceTiers: [{ minimumOrderQuantity: '1', unitPrice: '1.3333' }],
      selectedPrice: '1.3333', isPriced: true, falldown: bucket('falldown', null),
      weeks: Array.from({ length: horizon }, (_, i) => bucket('weekly', label(i))),
      undated: { salesOrderQuantity: '0', salesOrderDollars: '0', lineCount: 0 } }],
  };
}

describe('Planning Workbook fiscal settings through the application settings workflow', { timeout: 20_000 }, () => {
  let fetchMock: ReturnType<typeof vi.fn>;
  const user = userEvent.setup();
  const calls = (predicate: (url: string, method: string) => boolean) =>
    fetchMock.mock.calls.filter(([url, opts]) => predicate(String(url), (opts as RequestInit | undefined)?.method ?? 'GET')).length;
  const workbookGets = () => calls((url, method) => method === 'GET' && url.includes('/planning-workbook') && !url.includes('/planning-workbook/draft'));

  beforeEach(() => {
    window.localStorage.clear();
    fetchMock = vi.fn((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';
      const json = (body: unknown) => Promise.resolve({ ok: true, status: 200, json: async () => body, text: async () => JSON.stringify(body) });
      if (url.includes('/planning-workbook/refresh')) throw new Error('refresh must not be called');
      if (method === 'GET' && url.includes('/planning-workbook')) {
        return json(workbook(Number(new URL(url).searchParams.get('horizonWeeks'))));
      }
      if (url.includes('/mps/refresh')) throw new Error('MPS refresh must not be called');
      if (method === 'GET' && url.includes('/mps')) return json(dashboard);
      if (method === 'GET' && url.includes('/api/v1/workspaces')) return json({ workspaces: [workspace], configurationWarning: null });
      return json(status);
    });
    vi.stubGlobal('fetch', fetchMock);
    Object.defineProperty(window, '__TAURI_INTERNALS__', { configurable: true, writable: true, value: { transformCallback: vi.fn() } });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    Reflect.deleteProperty(window, '__TAURI_INTERNALS__');
    window.localStorage.clear();
  });

  async function openPlanning() {
    await user.click(screen.getByRole('tab', { name: 'Planning' }));
    await screen.findByRole('region', { name: 'Planning workbook grid' });
  }
  /** The Q1 total column header, whose accessible detail states the partial-quarter coverage. */
  const q1Header = () => screen.getAllByRole('columnheader').map(h => h.textContent ?? '').find(t => t.includes('FY27 Q1 total')) ?? '';

  it('reflects a saved 53-week exception when Planning is revisited, with no request caused by the settings change', async () => {
    render(<App />);
    await waitFor(() => expect(screen.getByText('Backend:')).toHaveTextContent('Backend: Connected'));
    await screen.findByText('ABC100');

    await openPlanning();
    expect(q1Header()).toContain('2 of 13');
    expect(workbookGets()).toBe(1);

    // Control: a revisit without any settings change costs exactly one cached workbook GET (remount).
    await user.click(screen.getByRole('tab', { name: /general/i }));
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));
    await screen.findByText('ABC100');
    await openPlanning();
    expect(workbookGets()).toBe(2);
    expect(q1Header()).toContain('2 of 13');

    // Change the fiscal calendar through the real General settings workflow.
    await user.click(screen.getByRole('tab', { name: /general/i }));
    const beforeSave = fetchMock.mock.calls.length;
    await user.click(screen.getByRole('button', { name: '+ Add Exception' }));
    await user.type(screen.getByRole('spinbutton', { name: 'Fiscal year' }), '2027');
    await user.type(screen.getByRole('spinbutton', { name: 'Extra week period' }), '3');
    await user.click(screen.getByRole('button', { name: 'Save' }));
    expect(screen.getByRole('button', { name: 'Remove FY2027 exception' })).toBeInTheDocument();
    expect(JSON.parse(window.localStorage.getItem('kst.fiscalCalendarSettings.v1') ?? '{}').exceptions)
      .toEqual([{ fiscalYear: 2027, extraWeekPeriod: 3 }]);
    expect(fetchMock.mock.calls.length).toBe(beforeSave); // saving settings makes no request at all

    // Revisit: the saved calendar is applied; the only workbook request is the same remount GET as the control.
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));
    await screen.findByText('ABC100');
    await openPlanning();
    expect(q1Header()).toContain('3 of 14');
    expect(workbookGets()).toBe(3);
    expect(calls(url => url.includes('/refresh'))).toBe(0);
  });
});

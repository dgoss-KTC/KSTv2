import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';

/**
 * Stage 14.5 unsaved Planning adjustment protection through the real application workflow (App â†’
 * ApplicationShell â†’ CustomerWorkspace â†’ PlanningWorkbookPanel). All traffic is mocked: there is no MPS
 * refresh, no Planning refresh and no QAD access.
 */
vi.mock('@tauri-apps/api/event', () => ({ listen: vi.fn(async () => () => {}) }));

const PLANNING_MESSAGE = 'Unsaved Planning adjustments will be discarded. Continue?';
const status = {
  applicationName: "Keytronic Scheduler's Toolbox", applicationVersion: '0.1.0', backendFramework: '.NET 10',
  backendInstanceId: 'test-id', startedAt: '2026-09-16T12:00:00Z', currentTime: '2026-09-16T12:01:00Z',
  snapshot: { available: false, snapshotId: null, createdAt: null, status: 'notLoaded' }, dataSources: [],
  lastRefreshAttemptAt: null, lastSuccessfulRefreshAt: null,
};
const ws = (id: string, name: string, order: number) => ({
  assignmentId: id, displayName: name, site: 'NW', productLineFrom: null, productLineTo: null,
  parentParts: ['ABC100'], isTemporary: false, coverageEndsOn: null, isEnabled: true, sortOrder: order,
});
const workspaces = [ws('ws-1', 'Line 1', 0), ws('ws-2', 'Line 2', 1)];
const dashboard = (id: string) => ({
  snapshot: { snapshotId: `snap-${id}`, createdAtUtc: '2026-09-16T12:00:00Z', lastSuccessfulRefreshAtUtc: '2026-09-16T12:00:00Z',
    status: 'current', workspaceId: id, site: 'NW', resolvedParentPartCount: 1, sourceRowCount: 1,
    isRefreshInProgress: false, lastRefreshError: null },
  dateBasis: 'dueDate', horizonWeeks: 4,
  parts: [{ parentPart: 'ABC100', description: 'Widget Assembly', buckets: [
    { kind: 'falldown', weekLabel: null, quantity: 0, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
  ] }],
});
function label(i: number) {
  const d = new Date(2026, 8, 14 + 7 * i);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
function workbook(id: string, horizon: number) {
  const bucket = (kind: string, weekLabel: string | null) => ({ kind, weekLabel, salesOrderQuantity: '1', salesOrderDollars: '2',
    mps: '30', mpsDollars: '40', forecast: '0', estimatedOnHand: '5' });
  return {
    workspaceId: id, site: 'NW', workbookSnapshotId: `wb-${id}`, mpsSnapshotId: `snap-${id}`, openOrdersSnapshotId: 'oo-1',
    acquiredAtUtc: '2026-09-16T16:00:00Z', priceEffectiveDate: '2026-09-16', priceSelectionPolicy: 'firstTier', today: '2026-09-16',
    horizonWeeks: horizon, isStale: false, warning: null, weekLabels: Array.from({ length: horizon }, (_, i) => label(i)),
    minimumSelectableHorizonWeeks: 12, maximumHorizonWeeks: 20,
    parts: [{ part: 'ABC100', description: 'Widget Assembly', netQuantityOnHand: '5', priceTiers: [{ minimumOrderQuantity: '1', unitPrice: '1.3333' }],
      selectedPrice: '1.3333', isPriced: true, falldown: bucket('falldown', null),
      weeks: Array.from({ length: horizon }, (_, i) => bucket('weekly', label(i))),
      undated: { salesOrderQuantity: '0', salesOrderDollars: '0', lineCount: 0 } }],
  };
}

describe('Planning unsaved adjustments through the application workflow', { timeout: 20_000 }, () => {
  let fetchMock: ReturnType<typeof vi.fn>;
  let failSave = false;
  let holdSave: { release: () => void } | null = null;
  const user = userEvent.setup();
  const calls = (predicate: (url: string, method: string) => boolean) =>
    fetchMock.mock.calls.filter(([url, opts]) => predicate(String(url), (opts as RequestInit | undefined)?.method ?? 'GET'));

  beforeEach(() => {
    failSave = false;
    holdSave = null;
    window.localStorage.clear();
    fetchMock = vi.fn((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';
      const json = (body: unknown, statusCode = 200) => Promise.resolve({ ok: statusCode < 400, status: statusCode, json: async () => body, text: async () => JSON.stringify(body) });
      const id = /workspaces\/(ws-\d)/.exec(url)?.[1] ?? 'ws-1';
      if (url.includes('/refresh')) throw new Error('no refresh may be called');
      if (url.includes('/planning-workbook/draft/presence')) return json({ exists: false });
      if (url.includes('/planning-workbook/draft') && method === 'PUT') {
        const body = { exists: true, isCorrupt: false, warning: null, savedAtUtc: '2026-09-16T17:00:00Z', savedMpsSnapshotId: `snap-${id}`, savedSite: 'NW', rows: [] };
        if (holdSave) {
          const held = holdSave;
          return new Promise(resolve => { held.release = () => resolve({ ok: true, status: 200, json: async () => body, text: async () => JSON.stringify(body) }); });
        }
        return failSave ? json({ title: 'Planning draft could not be saved' }, 503) : json(body);
      }
      if (method === 'GET' && url.includes('/planning-workbook')) return json(workbook(id, Number(new URL(url).searchParams.get('horizonWeeks'))));
      if (method === 'GET' && url.includes('/mps')) return json(dashboard(id));
      if (method === 'GET' && url.includes('/api/v1/workspaces')) return json({ workspaces, configurationWarning: null });
      return json(status);
    });
    vi.stubGlobal('fetch', fetchMock);
    Object.defineProperty(window, '__TAURI_INTERNALS__', { configurable: true, writable: true, value: { transformCallback: vi.fn() } });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    Reflect.deleteProperty(window, '__TAURI_INTERNALS__');
    window.localStorage.clear();
  });

  async function start() {
    render(<App />);
    await waitFor(() => expect(screen.getByText('Backend:')).toHaveTextContent('Backend: Connected'));
    await screen.findByText('ABC100');
  }
  /** Opens Planning (report mode) and switches to planning mode locally, as the owner would. */
  async function openPlanning() {
    await user.click(screen.getByRole('tab', { name: 'Planning' }));
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    await user.click(screen.getByRole('button', { name: /^(Start|Resume) planning/ }));
  }
  const cell = () => screen.getByRole('textbox', { name: 'Adjustment for ABC100, week of 9/21/2026' }) as HTMLInputElement;

  it('keeps proposals across module switches; confirms before leaving for General; cancel keeps them, accept discards them', async () => {
    await start();
    await openPlanning();
    await user.type(cell(), '-12');
    const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true);

    await user.click(screen.getByRole('tab', { name: 'Dashboard' }));
    await openPlanning();
    expect(cell().value).toBe('-12');
    expect(confirm).not.toHaveBeenCalled();

    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(confirm).toHaveBeenLastCalledWith(PLANNING_MESSAGE);
    expect(cell().value).toBe('-12');

    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(confirm).toHaveBeenCalledTimes(2);
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));
    await screen.findByText('ABC100');
    await openPlanning();
    expect(cell().value).toBe('');
    expect(calls((url, method) => url.includes('/planning-workbook/draft') && method === 'PUT')).toHaveLength(0);
  });

  it('a saved draft needs no confirmation; newer edits and a failed save do', async () => {
    await start();
    await openPlanning();
    await user.type(cell(), '7');
    await user.click(screen.getByRole('button', { name: 'Save draft' }));
    await screen.findByText('Draft saved. Nothing has been applied to QAD.');
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);

    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(confirm).not.toHaveBeenCalled();
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));
    await screen.findByText('ABC100');
    // The saved draft is on disk; this session's in-memory proposals were kept by the workspace.
    await openPlanning();

    await user.type(cell(), '8');
    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(confirm).toHaveBeenCalledTimes(1);

    failSave = true;
    await user.click(screen.getByRole('button', { name: 'Save draft' }));
    await screen.findByText(/Save Draft failed/);
    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(confirm).toHaveBeenCalledTimes(2);
    expect(confirm).toHaveBeenLastCalledWith(PLANNING_MESSAGE);
  });

  it('isolates proposals per workspace and protects a workspace switch', async () => {
    await start();
    await openPlanning();
    await user.type(cell(), '5');
    const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true);
    await user.click(screen.getByRole('tab', { name: 'Line 2' }));
    expect(confirm).toHaveBeenCalledWith(PLANNING_MESSAGE);
    expect(cell().value).toBe('5');
    await user.click(screen.getByRole('tab', { name: 'Line 2' }));
    await waitFor(() => expect(calls(url => url.includes('/workspaces/ws-2/planning-workbook?')).length).toBeGreaterThan(0));
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    await user.click(screen.getByRole('button', { name: 'Start planning' })); // Line 2 opens in report mode
    expect(cell().value).toBe('');
    const saves = calls((url, method) => url.includes('/planning-workbook/draft') && method === 'PUT');
    expect(saves).toHaveLength(0);
  });

  it('A \u2192 B \u2192 A: a delayed save from the earlier session leaves the new session unsaved and protected', async () => {
    await start();
    await openPlanning();
    const held = { release: () => {} };
    holdSave = held;
    // Edit counter 1 in the first Line 1 session.
    await user.click(cell());
    await user.paste('-4');
    await user.click(screen.getByRole('button', { name: 'Save draft' }));
    await waitFor(() => expect(calls((url, method) => url.includes('/planning-workbook/draft') && method === 'PUT')).toHaveLength(1));

    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    await user.click(screen.getByRole('tab', { name: 'Line 2' })); // the save is still pending: confirmed discard
    expect(confirm).toHaveBeenCalledTimes(1);
    await waitFor(() => expect(calls(url => url.includes('/workspaces/ws-2/planning-workbook?')).length).toBeGreaterThan(0));
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));
    expect(confirm).toHaveBeenCalledTimes(1); // Line 2 had nothing unsaved
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    await user.click(screen.getByRole('button', { name: 'Start planning' })); // a new Line 1 session opens in report mode
    await waitFor(() => expect(cell().value).toBe(''));

    // Edit counter 1 again in the new Line 1 session, with a different value.
    await user.click(cell());
    await user.paste('9');
    await act(async () => { held.release(); await Promise.resolve(); await Promise.resolve(); });
    expect(screen.queryByText(/Draft saved/)).not.toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('not saved');

    confirm.mockReturnValue(false);
    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(confirm).toHaveBeenCalledTimes(2);
    expect(confirm).toHaveBeenLastCalledWith(PLANNING_MESSAGE);
    expect(cell().value).toBe('9');
  });
});

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';

/**
 * Stage 14.5 unsaved Planning adjustment protection through the real application workflow (App â†’
 * ApplicationShell â†’ CustomerWorkspace â†’ PlanningWorkbookPanel). All traffic is mocked: there is no MPS
 * refresh, no Planning refresh and no QAD access.
 */
const { invokeMock, closeHandlers } = vi.hoisted(() => ({ invokeMock: vi.fn(async (..._args: unknown[]) => null as unknown), closeHandlers: new Set<() => void>() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: invokeMock }));
vi.mock('@tauri-apps/api/event', () => ({
  listen: vi.fn(async (event: string, handler: () => void) => {
    if (event !== 'kst-close-requested') return () => {};
    closeHandlers.add(handler);
    return () => { closeHandlers.delete(handler); };
  }),
}));

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
    invokeMock.mockClear();
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

  // The desktop dialog plugin replaces window.confirm with an asynchronous call that returns a Promise
  // (always truthy). Reproduce that so any remaining synchronous `!window.confirm(...)` guard would
  // silently proceed, exactly as the owner observed in the Tauri window.
  function installDesktopConfirm() {
    return vi.spyOn(window, 'confirm').mockImplementation((() => Promise.resolve(false)) as unknown as typeof window.confirm);
  }
  const discardDialog = () => screen.queryByRole('alertdialog', { name: 'Discard unsaved changes?' });
  const dialogButton = (name: string) => within(screen.getByRole('alertdialog', { name: 'Discard unsaved changes?' })).getByRole('button', { name });
  const guardCalls = () => invokeMock.mock.calls.filter(([command]) => command === 'set_close_guard').map(([, args]) => (args as { active: boolean }).active);
  const lastGuard = () => { const all = guardCalls(); return all[all.length - 1]; };
  const closeCalls = () => invokeMock.mock.calls.filter(([command]) => command === 'confirm_window_close');
  const requestNativeClose = async () => { await act(async () => { closeHandlers.forEach(handler => handler()); }); };

  it('keeps proposals across module switches; asks before leaving for General; cancel keeps them, discard drops them', async () => {
    const confirm = installDesktopConfirm();
    await start();
    await openPlanning();
    await user.type(cell(), '-12');

    await user.click(screen.getByRole('tab', { name: 'Dashboard' }));
    await openPlanning();
    expect(cell().value).toBe('-12');
    expect(discardDialog()).not.toBeInTheDocument();

    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(screen.getByRole('alertdialog', { name: 'Discard unsaved changes?' })).toHaveTextContent(PLANNING_MESSAGE);
    await user.click(dialogButton('Cancel'));
    expect(discardDialog()).not.toBeInTheDocument();
    expect(cell().value).toBe('-12');

    await user.click(screen.getByRole('tab', { name: /general/i }));
    await user.click(dialogButton('Discard changes'));
    expect(discardDialog()).not.toBeInTheDocument();
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));
    await screen.findByText('ABC100');
    await openPlanning();
    expect(cell().value).toBe('');
    expect(confirm).not.toHaveBeenCalled();
    expect(calls((url, method) => url.includes('/planning-workbook/draft') && method === 'PUT')).toHaveLength(0);
  });

  it('a saved draft needs no confirmation; newer edits and a failed save do', async () => {
    const confirm = installDesktopConfirm();
    await start();
    await openPlanning();
    await user.type(cell(), '7');
    await user.click(screen.getByRole('button', { name: 'Save draft' }));
    await screen.findByText('Draft saved. Nothing has been applied to QAD.');

    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(discardDialog()).not.toBeInTheDocument();
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));
    await screen.findByText('ABC100');
    // The saved draft is on disk; this session's in-memory proposals were kept by the workspace.
    await openPlanning();

    await user.type(cell(), '8');
    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(discardDialog()).toBeInTheDocument();
    await user.click(dialogButton('Cancel'));

    failSave = true;
    await user.click(screen.getByRole('button', { name: 'Save draft' }));
    await screen.findByText(/Save Draft failed/);
    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(screen.getByRole('alertdialog', { name: 'Discard unsaved changes?' })).toHaveTextContent(PLANNING_MESSAGE);
    expect(confirm).not.toHaveBeenCalled();
  });

  it('isolates proposals per workspace and asks before a workspace switch; cancel keeps the workspace and adjustments', async () => {
    const confirm = installDesktopConfirm();
    await start();
    await openPlanning();
    await user.type(cell(), '5');
    await user.click(screen.getByRole('tab', { name: 'Line 2' }));
    expect(screen.getByRole('alertdialog', { name: 'Discard unsaved changes?' })).toHaveTextContent(PLANNING_MESSAGE);
    await user.click(dialogButton('Cancel'));
    expect(screen.getByRole('tab', { name: 'Line 1' })).toHaveAttribute('aria-selected', 'true');
    expect(cell().value).toBe('5');
    expect(calls(url => url.includes('/workspaces/ws-2/planning-workbook?'))).toHaveLength(0);

    await user.click(screen.getByRole('tab', { name: 'Line 2' }));
    await user.click(dialogButton('Discard changes'));
    await waitFor(() => expect(calls(url => url.includes('/workspaces/ws-2/planning-workbook?')).length).toBeGreaterThan(0));
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    await user.click(screen.getByRole('button', { name: 'Start planning' })); // Line 2 opens in report mode
    expect(cell().value).toBe('');
    expect(discardDialog()).not.toBeInTheDocument();
    expect(confirm).not.toHaveBeenCalled();
    expect(calls((url, method) => url.includes('/planning-workbook/draft') && method === 'PUT')).toHaveLength(0);
  });

  it('A \u2192 B \u2192 A: a delayed save from the earlier session leaves the new session unsaved and protected', async () => {
    installDesktopConfirm();
    await start();
    await openPlanning();
    const held = { release: () => {} };
    holdSave = held;
    // Edit counter 1 in the first Line 1 session.
    await user.click(cell());
    await user.paste('-4');
    await user.click(screen.getByRole('button', { name: 'Save draft' }));
    await waitFor(() => expect(calls((url, method) => url.includes('/planning-workbook/draft') && method === 'PUT')).toHaveLength(1));

    await user.click(screen.getByRole('tab', { name: 'Line 2' })); // the save is still pending: asks before discarding
    await user.click(dialogButton('Discard changes'));
    await waitFor(() => expect(calls(url => url.includes('/workspaces/ws-2/planning-workbook?')).length).toBeGreaterThan(0));
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));
    expect(discardDialog()).not.toBeInTheDocument(); // Line 2 had nothing unsaved
    await screen.findByRole('region', { name: 'Planning workbook grid' });
    await user.click(screen.getByRole('button', { name: 'Start planning' })); // a new Line 1 session opens in report mode
    await waitFor(() => expect(cell().value).toBe(''));

    // Edit counter 1 again in the new Line 1 session, with a different value.
    await user.click(cell());
    await user.paste('9');
    await act(async () => { held.release(); await Promise.resolve(); await Promise.resolve(); });
    expect(screen.queryByText(/Draft saved/)).not.toBeInTheDocument();
    expect(screen.getByTestId('planning-change-count')).toHaveTextContent('not saved');

    await user.click(screen.getByRole('tab', { name: /general/i }));
    expect(screen.getByRole('alertdialog', { name: 'Discard unsaved changes?' })).toHaveTextContent(PLANNING_MESSAGE);
    await user.click(dialogButton('Cancel'));
    expect(cell().value).toBe('9');
  });

  it('modules in one workspace keep adjustments with no prompt, and a repeated switch request shows one dialog', async () => {
    installDesktopConfirm();
    await start();
    await openPlanning();
    await user.type(cell(), '3');
    await user.click(screen.getByRole('tab', { name: 'Finished Goods' }));
    await openPlanning();
    expect(discardDialog()).not.toBeInTheDocument();
    expect(cell().value).toBe('3');

    await user.click(screen.getByRole('tab', { name: 'Line 2' }));
    await user.click(screen.getByRole('tab', { name: 'Line 2' }));
    expect(screen.getAllByRole('alertdialog')).toHaveLength(1);
    await user.click(dialogButton('Cancel'));
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    expect(cell().value).toBe('3');
  });

  describe('native desktop window close', () => {
    it('reports unsaved/saved state to the host; saved draft is clean, editing after saving is unsaved again', async () => {
      installDesktopConfirm();
      await start();
      await openPlanning();
      await waitFor(() => expect(guardCalls()).toEqual([]));
      await user.type(cell(), '7');
      await waitFor(() => expect(lastGuard()).toBe(true));
      await user.click(screen.getByRole('button', { name: 'Save draft' }));
      await screen.findByText('Draft saved. Nothing has been applied to QAD.');
      await waitFor(() => expect(lastGuard()).toBe(false));
      await user.type(cell(), '8');
      await waitFor(() => expect(lastGuard()).toBe(true));
    });

    it('a close request with unsaved adjustments asks first; cancel keeps the window and adjustments, a second request asks again', async () => {
      installDesktopConfirm();
      await start();
      await openPlanning();
      await user.type(cell(), '4');
      await waitFor(() => expect(lastGuard()).toBe(true));

      await requestNativeClose();
      expect(screen.getByRole('alertdialog', { name: 'Discard unsaved changes?' })).toHaveTextContent(PLANNING_MESSAGE);
      await requestNativeClose(); // a repeated request joins the pending prompt
      expect(screen.getAllByRole('alertdialog')).toHaveLength(1);
      await user.click(dialogButton('Cancel'));
      expect(closeCalls()).toHaveLength(0);
      expect(cell().value).toBe('4');

      await requestNativeClose();
      expect(discardDialog()).toBeInTheDocument();
    });

    it('confirming closes exactly once, clearing the guard first, with no further prompt', async () => {
      installDesktopConfirm();
      await start();
      await openPlanning();
      await user.type(cell(), '4');
      await waitFor(() => expect(lastGuard()).toBe(true));
      const before = invokeMock.mock.calls.length;

      await requestNativeClose();
      await user.click(dialogButton('Discard changes'));
      await waitFor(() => expect(closeCalls()).toHaveLength(1));
      const after = invokeMock.mock.calls.slice(before).map(([command, args]) => `${command}:${JSON.stringify(args ?? null)}`);
      expect(after.indexOf('set_close_guard:{"active":false}')).toBeGreaterThanOrEqual(0);
      expect(after.indexOf('set_close_guard:{"active":false}')).toBeLessThan(after.indexOf('confirm_window_close:null'));
      expect(discardDialog()).not.toBeInTheDocument();
    });

    it('a close request with nothing unsaved asks nothing', async () => {
      installDesktopConfirm();
      await start();
      await openPlanning();
      await requestNativeClose();
      expect(discardDialog()).not.toBeInTheDocument();
    });
  });
});

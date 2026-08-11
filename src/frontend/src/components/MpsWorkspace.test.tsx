import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, within, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';
import type {
  SystemStatusResponse,
  WorkspaceListResponseDto,
  WorkspaceAssignmentDto,
  MpsDashboardResponseDto,
  PartDetailResponseDto,
  UserPreferencesDto,
  PreferencesResponseDto,
} from '../api/client';

vi.mock('@tauri-apps/api/event', () => ({
  listen: vi.fn(async () => () => {}),
}));

const mockStatus: SystemStatusResponse = {
  applicationName: "Keytronic Scheduler's Toolbox",
  applicationVersion: '0.1.0',
  backendFramework: '.NET 10',
  backendInstanceId: 'test-id',
  startedAt: '2026-07-28T12:00:00Z',
  currentTime: '2026-07-28T12:01:00Z',
  snapshot: { available: false, snapshotId: null, createdAt: null, status: 'notLoaded' },
  dataSources: [],
  lastRefreshAttemptAt: null,
  lastSuccessfulRefreshAt: null,
};

function makeWorkspace(overrides: Partial<WorkspaceAssignmentDto> = {}): WorkspaceAssignmentDto {
  return {
    assignmentId: 'ws-1',
    displayName: 'Line 1',
    site: 'NW',
    productLineFrom: null,
    productLineTo: null,
    parentParts: ['ABC100'],
    isTemporary: false,
    coverageEndsOn: null,
    isEnabled: true,
    sortOrder: 0,
    ...overrides,
  };
}

const workspaceList: WorkspaceListResponseDto = {
  workspaces: [makeWorkspace()],
  configurationWarning: null,
};

function makeDashboard(overrides: Partial<MpsDashboardResponseDto> = {}): MpsDashboardResponseDto {
  return {
    snapshot: {
      snapshotId: 'snap-1',
      createdAtUtc: '2026-01-01T00:00:00Z',
      lastSuccessfulRefreshAtUtc: '2026-01-01T00:00:00Z',
      status: 'current',
      workspaceId: 'ws-1',
      site: 'NW',
      resolvedParentPartCount: 1,
      sourceRowCount: 10,
      isRefreshInProgress: false,
      lastRefreshError: null,
    },
    dateBasis: 'dueDate',
    horizonWeeks: 4,
    parts: [
      {
        parentPart: 'ABC100',
        description: 'Widget Assembly',
        buckets: [
          {
            kind: 'falldown',
            weekLabel: null,
            quantity: 50,
            executionStatus: 'none',
            containsPlannedWork: false,
            containsExplicitlyScheduledWork: false,
          },
          {
            kind: 'weekly',
            weekLabel: '2025-06-30',
            quantity: 100,
            executionStatus: 'released',
            containsPlannedWork: false,
            containsExplicitlyScheduledWork: true,
          },
          {
            kind: 'weekly',
            weekLabel: '2025-07-07',
            quantity: 200,
            executionStatus: 'allocating',
            containsPlannedWork: true,
            containsExplicitlyScheduledWork: false,
          },
          {
            kind: 'weekly',
            weekLabel: '2025-07-14',
            quantity: 0,
            executionStatus: 'none',
            containsPlannedWork: false,
            containsExplicitlyScheduledWork: false,
          },
          {
            kind: 'weekly',
            weekLabel: '2025-07-21',
            quantity: 300,
            executionStatus: 'frozen',
            containsPlannedWork: false,
            containsExplicitlyScheduledWork: false,
          },
        ],
      },
    ],
    ...overrides,
  };
}

function makePartDetail(overrides: Partial<PartDetailResponseDto> = {}): PartDetailResponseDto {
  return {
    site: 'NW',
    partNumber: 'ABC100',
    plannerCode: 'JSMITH',
    manufacturingLeadTimeDays: 10,
    safetyTimeDays: 2,
    partStatusCode: 'C',
    partStatusDescription: 'CURRENT',
    currentRevision: 'B',
    description: 'Widget Assembly',
    iosCode: '1234',
    safetyStockQuantity: 250,
    quantityOnHand: 1325,
    quantityNonNet: 75,
    quantityRmaOnHand: 25,
    priceBreaks: [{ minimumOrderQuantity: 100, unitPrice: 12.45 }],
    loadedAtUtc: '2026-08-10T22:30:00Z',
    isStale: false,
    warning: null,
    ...overrides,
  };
}

describe('MpsWorkspace', () => {
  let fetchMock: ReturnType<typeof vi.fn>;
  const user = userEvent.setup();

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    Object.defineProperty(window, '__TAURI_INTERNALS__', {
      configurable: true,
      writable: true,
      value: { transformCallback: vi.fn() },
    });
    window.localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    Reflect.deleteProperty(window, '__TAURI_INTERNALS__');
    window.localStorage.clear();
  });

  function setupBackend(handlers: {
    onGetMps?: (url: string) => { ok: boolean; status?: number; json?: () => Promise<unknown>; text?: () => Promise<string> };
    onRefreshMps?: () => { ok: boolean; status?: number; json?: () => Promise<unknown>; text?: () => Promise<string> };
    onGetPartDetail?: (url: string) => { ok: boolean; status?: number; json?: () => Promise<unknown>; text?: () => Promise<string> };
  } = {}) {
    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';
      if (method === 'GET' && url.includes('/part-detail')) {
        const result = handlers.onGetPartDetail?.(url) ?? { ok: true, json: async () => makePartDetail() };
        return Promise.resolve(result);
      }
      if (method === 'POST' && url.includes('/mps/refresh')) {
        const result = handlers.onRefreshMps?.() ?? { ok: true, json: async () => makeDashboard() };
        return Promise.resolve(result);
      }
      if (method === 'GET' && url.includes('/mps')) {
        const result = handlers.onGetMps?.(url) ?? { ok: true, json: async () => makeDashboard() };
        return Promise.resolve(result);
      }
      if (method === 'GET' && url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => workspaceList });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });
  }

  async function waitForConnected() {
    await waitFor(() => {
      expect(screen.getByText(/backend connected/i)).toBeInTheDocument();
    });
  }

  it('renders the MPS grid with fiscal bands, falldown, and weekly cells', async () => {
    setupBackend();
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    expect(screen.getByText('Widget Assembly')).toBeInTheDocument();
    expect(screen.getByText('Q1')).toBeInTheDocument();
    expect(screen.getByText('P1')).toBeInTheDocument();
    expect(screen.getByText('Jun 30')).toBeInTheDocument();
    expect(screen.getByText('Jul 21')).toBeInTheDocument();
    expect(screen.getByText('100')).toBeInTheDocument();
    expect(screen.getByText('300')).toBeInTheDocument();
  });

  it('shows a message when the workspace resolves to zero parts', async () => {
    setupBackend({ onGetMps: () => ({ ok: true, json: async () => makeDashboard({ parts: [] }) }) });
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText(/no parts resolved/i)).toBeInTheDocument();
    });
  });

  it('shows a retry option when MPS data is unavailable', async () => {
    setupBackend({
      onGetMps: () => ({ ok: false, status: 503, text: async () => JSON.stringify({ detail: 'DB is down.' }) }),
    });
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('DB is down.')).toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
  });

  it('switching to Release Date triggers a new request with the updated date basis', async () => {
    setupBackend();
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    fetchMock.mockClear();
    await user.click(within(screen.getByRole('main')).getByRole('button', { name: /release date/i }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('dateBasis=releaseDate')),
      ).toBe(true);
    });
  });

  it('clicking Refresh triggers the POST /mps/refresh endpoint', async () => {
    setupBackend();
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    fetchMock.mockClear();
    await user.click(within(screen.getByRole('main')).getByRole('button', { name: /^refresh$/i }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, opts]) =>
            typeof url === 'string' && url.includes('/mps/refresh') && (opts as RequestInit)?.method === 'POST',
        ),
      ).toBe(true);
    });
  });

  it('horizon selection survives navigating to General and back', async () => {
    setupBackend();
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    const horizonInput = screen.getByLabelText(/horizon in weeks/i);
    fireEvent.change(horizonInput, { target: { value: '24' } });

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('horizonWeeks=24')),
      ).toBe(true);
    });

    await user.click(screen.getByRole('tab', { name: /general/i }));
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /^general$/i })).toBeInTheDocument();
    });

    fetchMock.mockClear();
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('horizonWeeks=24')),
      ).toBe(true);
    });
    await waitFor(() => {
      expect(screen.getByLabelText(/horizon in weeks/i)).toHaveValue(24);
    });
  });

  it('Due/Release selection persists through the same preference mechanism as horizon', async () => {
    setupBackend();
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    await user.click(within(screen.getByRole('main')).getByRole('button', { name: /release date/i }));
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('dateBasis=releaseDate')),
      ).toBe(true);
    });

    await user.click(screen.getByRole('tab', { name: /general/i }));
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /^general$/i })).toBeInTheDocument();
    });

    fetchMock.mockClear();
    await user.click(screen.getByRole('tab', { name: 'Line 1' }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('dateBasis=releaseDate')),
      ).toBe(true);
    });
    expect(
      within(screen.getByRole('main')).getByRole('button', { name: /release date/i }),
    ).toHaveAttribute('aria-pressed', 'true');
  });

  it('comfortable density selected in General remains applied to the shell after returning to the workspace', async () => {
    let preferences: UserPreferencesDto = { theme: 'system', accentColor: 'blue', rowDensity: 'compact' };
    function preferencesResponse(): PreferencesResponseDto {
      return { preferences, configurationWarning: null };
    }
    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';
      if (url.includes('/api/v1/preferences') && method === 'PUT') {
        const body = JSON.parse(String(opts?.body)) as Partial<UserPreferencesDto>;
        preferences = { ...preferences, ...body };
        return Promise.resolve({ ok: true, json: async () => preferencesResponse() });
      }
      if (url.includes('/api/v1/preferences')) {
        return Promise.resolve({ ok: true, json: async () => preferencesResponse() });
      }
      if (method === 'POST' && url.includes('/mps/refresh')) {
        return Promise.resolve({ ok: true, json: async () => makeDashboard() });
      }
      if (method === 'GET' && url.includes('/mps')) {
        return Promise.resolve({ ok: true, json: async () => makeDashboard() });
      }
      if (method === 'GET' && url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => workspaceList });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);
    await waitForConnected();
    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });
    expect(document.querySelector('.shell')).toHaveAttribute('data-density', 'compact');

    await user.click(screen.getByRole('tab', { name: /general/i }));
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /^general$/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /^comfortable$/i }));
    await waitFor(() => {
      expect(document.querySelector('.shell')).toHaveAttribute('data-density', 'comfortable');
    });

    await user.click(screen.getByRole('tab', { name: 'Line 1' }));
    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    expect(document.querySelector('.shell')).toHaveAttribute('data-density', 'comfortable');
  });

  it('clicking a parent part row opens the Part Info panel and loads part detail', async () => {
    setupBackend();
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    await user.click(screen.getByText('ABC100'));

    await waitFor(() => {
      expect(screen.getByText(/part info/i)).toBeInTheDocument();
    });
    await waitFor(() => {
      expect(screen.getByText('10 days')).toBeInTheDocument();
    });
    expect(screen.getByText(/CURRENT/)).toBeInTheDocument();
  });

  it('collapses the grid to the selected parent and restores it via Back to full grid', async () => {
    setupBackend({
      onGetMps: () => ({
        ok: true,
        json: async () =>
          makeDashboard({
            parts: [
              ...makeDashboard().parts,
              { ...makeDashboard().parts[0], parentPart: 'XYZ200', description: 'Other Part' },
            ],
          }),
      }),
    });
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
      expect(screen.getByText('XYZ200')).toBeInTheDocument();
    });

    await user.click(screen.getByText('ABC100'));

    await waitFor(() => {
      expect(screen.getByText(/part info/i)).toBeInTheDocument();
    });
    expect(screen.getByText('ABC100')).toBeInTheDocument();
    expect(screen.queryByText('XYZ200')).not.toBeInTheDocument();

    // Focused mode renders only the selected row (no hidden placeholders for the rest of the grid)
    // and the frame is marked with the modifier that stops it stretching to fill leftover height.
    expect(within(screen.getByRole('main')).getAllByRole('row')).toHaveLength(4); // 3 header rows + 1 body row
    expect(document.querySelector('.mps-grid-frame')).toHaveClass('mps-grid-frame--focused');

    await user.click(screen.getByRole('button', { name: /back to full grid/i }));

    await waitFor(() => {
      expect(screen.queryByText(/part info/i)).not.toBeInTheDocument();
    });
    expect(screen.getByText('ABC100')).toBeInTheDocument();
    expect(screen.getByText('XYZ200')).toBeInTheDocument();
    expect(document.querySelector('.mps-grid-frame')).not.toHaveClass('mps-grid-frame--focused');
  });

  it('clicking the selected parent row again toggles Part Info closed and restores the full grid', async () => {
    setupBackend({
      onGetMps: () => ({
        ok: true,
        json: async () =>
          makeDashboard({
            parts: [
              ...makeDashboard().parts,
              { ...makeDashboard().parts[0], parentPart: 'XYZ200', description: 'Other Part' },
            ],
          }),
      }),
    });
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
      expect(screen.getByText('XYZ200')).toBeInTheDocument();
    });

    await user.click(screen.getByText('ABC100'));
    await waitFor(() => {
      expect(screen.getByText(/part info/i)).toBeInTheDocument();
    });
    expect(screen.queryByText('XYZ200')).not.toBeInTheDocument();

    fetchMock.mockClear();
    await user.click(screen.getByText('ABC100'));

    await waitFor(() => {
      expect(screen.queryByText(/part info/i)).not.toBeInTheDocument();
    });
    expect(screen.getByText('ABC100')).toBeInTheDocument();
    expect(screen.getByText('XYZ200')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('/part-detail'))).toBe(
      false,
    );
  });

  it('keyboard activation of the selected parent row also toggles Part Info closed', async () => {
    setupBackend();
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    const row = screen.getByText('ABC100').closest('tr');
    if (!row) throw new Error('row not found');
    row.focus();
    fireEvent.keyDown(row, { key: 'Enter' });

    await waitFor(() => {
      expect(screen.getByText(/part info/i)).toBeInTheDocument();
    });

    const focusedRow = screen.getByText('ABC100').closest('tr');
    if (!focusedRow) throw new Error('row not found');
    fireEvent.keyDown(focusedRow, { key: 'Enter' });

    await waitFor(() => {
      expect(screen.queryByText(/part info/i)).not.toBeInTheDocument();
    });
  });

  it('shows a not-found message when QAD has no part master record for the selected part', async () => {
    setupBackend({
      onGetPartDetail: () => ({
        ok: false,
        status: 404,
        text: async () => JSON.stringify({ title: 'Part not found', detail: 'No QAD part master record.' }),
      }),
    });
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    await user.click(screen.getByText('ABC100'));

    await waitFor(() => {
      expect(screen.getByText(/no qad part master record was found/i)).toBeInTheDocument();
    });
  });

  it('shows an error with a retry option when the part detail request fails', async () => {
    setupBackend({
      onGetPartDetail: () => ({
        ok: false,
        status: 503,
        text: async () => JSON.stringify({ detail: 'Database currently unavailable.' }),
      }),
    });
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    await user.click(screen.getByText('ABC100'));

    await waitFor(() => {
      expect(screen.getByText('Database currently unavailable.')).toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
  });

  it('does not refetch part detail when changing date basis while a part is selected', async () => {
    setupBackend();
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    await user.click(screen.getByText('ABC100'));
    await waitFor(() => {
      expect(screen.getByText('10 days')).toBeInTheDocument();
    });

    fetchMock.mockClear();
    await user.click(within(screen.getByRole('main')).getByRole('button', { name: /release date/i }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('dateBasis=releaseDate')),
      ).toBe(true);
    });
    expect(fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('/part-detail'))).toBe(
      false,
    );
  });

  it('does not refetch part detail when clicking Back to full grid', async () => {
    setupBackend();
    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    await user.click(screen.getByText('ABC100'));
    await waitFor(() => {
      expect(screen.getByText('10 days')).toBeInTheDocument();
    });

    fetchMock.mockClear();
    await user.click(screen.getByRole('button', { name: /back to full grid/i }));

    await waitFor(() => {
      expect(screen.queryByText(/part info/i)).not.toBeInTheDocument();
    });
    expect(fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('/part-detail'))).toBe(
      false,
    );
  });
});

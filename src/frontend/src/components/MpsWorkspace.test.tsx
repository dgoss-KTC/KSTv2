import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';
import type {
  SystemStatusResponse,
  WorkspaceListResponseDto,
  WorkspaceAssignmentDto,
  MpsDashboardResponseDto,
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
  } = {}) {
    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';
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
});

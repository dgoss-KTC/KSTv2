import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';
import type {
  SystemStatusResponse,
  WorkspaceListResponseDto,
  WorkspaceAssignmentDto,
  PreferencesResponseDto,
  UserPreferencesDto,
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
  dataSources: [
    { name: 'QAD', status: 'notConfigured' },
    { name: 'Shortage Database', status: 'notConfigured' },
  ],
  lastRefreshAttemptAt: null,
  lastSuccessfulRefreshAt: null,
};

const emptyWorkspaceList: WorkspaceListResponseDto = { workspaces: [], configurationWarning: null };

const defaultPreferences: UserPreferencesDto = {
  theme: 'system',
  accentColor: 'blue',
  rowDensity: 'compact',
};

function makeWorkspace(overrides: Partial<WorkspaceAssignmentDto> = {}): WorkspaceAssignmentDto {
  return {
    assignmentId: 'id-1',
    displayName: '1 parent part',
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

function preferencesResponse(preferences: UserPreferencesDto): PreferencesResponseDto {
  return { preferences, configurationWarning: null };
}

describe('General workspace', () => {
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
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    Reflect.deleteProperty(window, '__TAURI_INTERNALS__');
  });

  async function waitForConnected() {
    await waitFor(() => {
      expect(screen.getByText(/backend connected/i)).toBeInTheDocument();
    });
  }

  it('navigating to General shows Appearance, Workspace Management, and Application Status sections', async () => {
    fetchMock.mockImplementation((url: string) => {
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => emptyWorkspaceList });
      }
      if (url.includes('/api/v1/preferences')) {
        return Promise.resolve({ ok: true, json: async () => preferencesResponse(defaultPreferences) });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);
    await waitForConnected();

    await user.click(screen.getByRole('tab', { name: /general/i }));

    expect(screen.getByRole('heading', { name: /^general$/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /appearance/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /workspace management/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /application status/i })).toBeInTheDocument();
  });

  it('selecting a theme option calls PUT /api/v1/preferences with the new value', async () => {
    let preferences = { ...defaultPreferences };
    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => emptyWorkspaceList });
      }
      if (url.includes('/api/v1/preferences') && method === 'PUT') {
        const body = JSON.parse(String(opts?.body)) as UserPreferencesDto;
        preferences = { ...preferences, ...body };
        return Promise.resolve({ ok: true, json: async () => preferencesResponse(preferences) });
      }
      if (url.includes('/api/v1/preferences')) {
        return Promise.resolve({ ok: true, json: async () => preferencesResponse(preferences) });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);
    await waitForConnected();
    await user.click(screen.getByRole('tab', { name: /general/i }));

    const darkButton = await screen.findByRole('button', { name: /^dark$/i });
    await user.click(darkButton);

    await waitFor(() => {
      expect(darkButton).toHaveAttribute('aria-pressed', 'true');
    });

    const putCalls = fetchMock.mock.calls.filter(
      (call) =>
        String(call[0]).includes('/api/v1/preferences') &&
        (call[1] as RequestInit | undefined)?.method === 'PUT',
    );
    expect(putCalls.length).toBeGreaterThan(0);
    const lastPutOpts = putCalls[putCalls.length - 1][1] as RequestInit;
    expect(JSON.parse(String(lastPutOpts.body))).toMatchObject({ theme: 'dark' });
  });

  it('clicking Refresh triggers POST /api/v1/system/refresh', async () => {
    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => emptyWorkspaceList });
      }
      if (url.includes('/api/v1/preferences')) {
        return Promise.resolve({ ok: true, json: async () => preferencesResponse(defaultPreferences) });
      }
      if (url.includes('/api/v1/system/refresh') && method === 'POST') {
        return Promise.resolve({ ok: true, json: async () => mockStatus });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);
    await waitForConnected();
    await user.click(screen.getByRole('tab', { name: /general/i }));

    const refreshButtons = await screen.findAllByRole('button', { name: /^refresh$/i });
    await user.click(refreshButtons[0]);

    await waitFor(() => {
      const refreshCalls = fetchMock.mock.calls.filter(
        (call) =>
          String(call[0]).includes('/api/v1/system/refresh') &&
          ((call[1] as RequestInit | undefined)?.method ?? 'GET') === 'POST',
      );
      expect(refreshCalls.length).toBeGreaterThan(0);
    });
  });

  it('Move Right menu item reorders workspace tabs via PUT /api/v1/workspaces/order', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [
        makeWorkspace({ assignmentId: 'a', displayName: 'Alpha', sortOrder: 0 }),
        makeWorkspace({ assignmentId: 'b', displayName: 'Beta', sortOrder: 1 }),
      ],
      configurationWarning: null,
    };
    let orderedIds: string[] | null = null;

    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';
      if (method === 'GET' && url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => list });
      }
      if (url.includes('/api/v1/preferences')) {
        return Promise.resolve({ ok: true, json: async () => preferencesResponse(defaultPreferences) });
      }
      if (method === 'PUT' && url.includes('/api/v1/workspaces/order')) {
        const body = JSON.parse(String(opts?.body)) as { assignmentIds: string[] };
        orderedIds = body.assignmentIds;
        const reordered = orderedIds.map((id, index) => {
          const w = list.workspaces.find((x) => x.assignmentId === id);
          return { ...w!, sortOrder: index };
        });
        return Promise.resolve({
          ok: true,
          json: async () => ({ workspaces: reordered, configurationWarning: null }),
        });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);
    await waitForConnected();

    await waitFor(() => {
      expect(screen.getByRole('tab', { name: /alpha/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /workspace actions for alpha/i }));
    await waitFor(() => {
      expect(screen.getByRole('menu')).toBeInTheDocument();
    });
    await user.click(screen.getByRole('menuitem', { name: /move right/i }));

    await waitFor(() => {
      expect(orderedIds).toEqual(['b', 'a']);
    });
  });
});

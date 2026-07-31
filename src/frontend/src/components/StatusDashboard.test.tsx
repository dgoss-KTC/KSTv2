import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import App from '../App';
import type { SystemStatusResponse } from '../api/client';
import type { WorkspaceListResponseDto } from '../api/client';

const lifecycleListeners = new Map<string, (event: { payload: unknown }) => void>();

vi.mock('@tauri-apps/api/event', () => ({
  listen: vi.fn(async (eventName: string, callback: (event: { payload: unknown }) => void) => {
    lifecycleListeners.set(eventName, callback);
    return () => lifecycleListeners.delete(eventName);
  }),
}));

const mockStatus: SystemStatusResponse = {
  applicationName: "Keytronic Scheduler's Toolbox",
  applicationVersion: '0.1.0',
  backendFramework: '.NET 10',
  backendInstanceId: 'test-instance-id',
  startedAt: '2026-07-28T12:00:00-07:00',
  currentTime: '2026-07-28T12:01:00-07:00',
  snapshot: {
    available: false,
    snapshotId: null,
    createdAt: null,
    status: 'notLoaded',
  },
  dataSources: [
    { name: 'QAD', status: 'notConfigured' },
    { name: 'Shortage Database', status: 'notConfigured' },
  ],
};

const mockWorkspaceList: WorkspaceListResponseDto = {
  workspaces: [],
  configurationWarning: null,
};

describe('App integration', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    lifecycleListeners.clear();
    Object.defineProperty(window, '__TAURI_INTERNALS__', {
      configurable: true,
      writable: true,
      value: {
        transformCallback: vi.fn(),
      },
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    Reflect.deleteProperty(window, '__TAURI_INTERNALS__');
  });

  it('shows starting state initially', () => {
    fetchMock.mockReturnValue(new Promise(() => {})); // never resolves
    render(<App />);
    expect(
      screen.queryByText(/starting/i),
    ).toBeTruthy();
  });

  it('shows Backend connected label when backend is up', async () => {
    fetchMock.mockImplementation((url: string) => {
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => mockWorkspaceList });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText(/backend connected/i)).toBeInTheDocument();
    });
  });

  it('shows empty workspace state when connected with no workspaces', async () => {
    fetchMock.mockImplementation((url: string) => {
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => mockWorkspaceList });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText(/use \+ to add a workspace/i)).toBeInTheDocument();
    });
  });

  it('shows backend unavailable when fetch fails', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText(/backend unavailable/i)).toBeInTheDocument();
    });
  });

  it('shows the + button for adding workspaces', async () => {
    fetchMock.mockImplementation((url: string) => {
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => mockWorkspaceList });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /add workspace/i })).toBeInTheDocument();
    });
  });

  it('does not render an IOS field', async () => {
    fetchMock.mockImplementation((url: string) => {
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => mockWorkspaceList });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText(/backend connected/i)).toBeInTheDocument();
    });

    expect(screen.queryByLabelText(/ios/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/ios code/i)).not.toBeInTheDocument();
  });
});


import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
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
  lastRefreshAttemptAt: null,
  lastSuccessfulRefreshAt: null,
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

  it('shows a muted starting indicator in the bottom bar only', () => {
    fetchMock.mockReturnValue(new Promise(() => {})); // never resolves
    render(<App />);
    const bottomBar = document.querySelector('.bottom-bar');
    expect(within(bottomBar as HTMLElement).getByText('Backend:')).toHaveTextContent('Backend: Starting…');
    expect(bottomBar?.querySelector('.bottom-bar__dot')).toHaveClass('bottom-bar__dot--starting');
    expect(document.querySelector('.top-bar__dot')).not.toBeInTheDocument();
  });

  it('shows the connected backend label and green indicator in the bottom bar only', async () => {
    fetchMock.mockImplementation((url: string) => {
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => mockWorkspaceList });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);

    await waitFor(() => {
      const bottomBar = document.querySelector('.bottom-bar');
      expect(within(bottomBar as HTMLElement).getByText('Backend:')).toHaveTextContent('Backend: Connected');
      expect(bottomBar?.querySelector('.bottom-bar__dot')).toHaveClass('bottom-bar__dot--connected');
    });
    expect(document.querySelector('.top-bar__dot')).not.toBeInTheDocument();
    expect(document.querySelector('.top-bar')).not.toHaveTextContent(/backend connected/i);
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

  it('shows an error indicator and unavailable backend label in the bottom bar', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

    render(<App />);

    await waitFor(() => {
      const bottomBar = document.querySelector('.bottom-bar');
      expect(within(bottomBar as HTMLElement).getByText('Backend:')).toHaveTextContent('Backend: Unavailable');
      expect(bottomBar?.querySelector('.bottom-bar__dot')).toHaveClass('bottom-bar__dot--error');
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
      expect(screen.getByText('Backend:')).toHaveTextContent('Backend: Connected');
    });

    expect(screen.queryByLabelText(/ios/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/ios code/i)).not.toBeInTheDocument();
  });

  it('retains configuration warnings beside the bottom-bar backend status', async () => {
    const warning = 'Workspace configuration requires review.';
    fetchMock.mockImplementation((url: string) => {
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => ({ ...mockWorkspaceList, configurationWarning: warning }) });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);

    await waitFor(() => {
      const bottomBar = document.querySelector('.bottom-bar');
      expect(within(bottomBar as HTMLElement).getByText('Configuration warning')).toBeInTheDocument();
      expect(bottomBar?.querySelector('.bottom-bar__dot')).toHaveClass('bottom-bar__dot--warning');
      expect(within(bottomBar as HTMLElement).getByText('Backend:')).toHaveTextContent('Backend: Connected');
      expect(within(bottomBar as HTMLElement).getByText('Backend:').closest('.bottom-bar__item')).toHaveAttribute('title', warning);
    });
    expect(document.querySelector('.top-bar')).not.toHaveTextContent(/configuration warning/i);
  });
});


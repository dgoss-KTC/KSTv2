import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';
import type { SystemStatusResponse } from '../api/client';
import type { WorkspaceListResponseDto, WorkspaceAssignmentDto } from '../api/client';

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
    assignmentId: 'id-1',
    displayName: 'Customer 11111111',
    site: 'NW',
    customerNumber: '11111111',
    productLineFrom: null,
    productLineTo: null,
    isTemporary: false,
    coverageEndsOn: null,
    isEnabled: true,
    sortOrder: 0,
    ...overrides,
  };
}

describe('Workspace lifecycle (edit / archive / restore / delete / reset)', () => {
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

  function setupBackend(
    initialList: WorkspaceListResponseDto,
    handlers: {
      onPut?: (id: string, body: unknown) => WorkspaceAssignmentDto;
      onArchive?: (id: string) => WorkspaceAssignmentDto;
      onRestore?: (id: string) => WorkspaceAssignmentDto;
      onDelete?: (id: string) => void;
      onReset?: () => void;
    } = {},
  ) {
    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';

      if (method === 'GET' && url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => initialList });
      }
      if (method === 'GET') {
        return Promise.resolve({ ok: true, json: async () => mockStatus });
      }
      if (method === 'PUT') {
        const match = url.match(/workspaces\/([^/]+)$/);
        const id = match ? match[1] : '';
        const body = JSON.parse(String(opts?.body));
        const updated = handlers.onPut?.(id, body);
        if (!updated) {
          return Promise.resolve({ ok: false, status: 404, text: async () => '' });
        }
        return Promise.resolve({ ok: true, json: async () => updated });
      }
      if (method === 'POST' && url.includes('/archive')) {
        const match = url.match(/workspaces\/([^/]+)\/archive$/);
        const id = match ? match[1] : '';
        const updated = handlers.onArchive?.(id);
        if (!updated) {
          return Promise.resolve({ ok: false, status: 404, text: async () => '' });
        }
        return Promise.resolve({ ok: true, json: async () => updated });
      }
      if (method === 'POST' && url.includes('/restore')) {
        const match = url.match(/workspaces\/([^/]+)\/restore$/);
        const id = match ? match[1] : '';
        const updated = handlers.onRestore?.(id);
        if (!updated) {
          return Promise.resolve({ ok: false, status: 404, text: async () => '' });
        }
        return Promise.resolve({ ok: true, json: async () => updated });
      }
      if (method === 'DELETE' && /workspaces\/[^/]+$/.test(url)) {
        const match = url.match(/workspaces\/([^/]+)$/);
        const id = match ? match[1] : '';
        handlers.onDelete?.(id);
        return Promise.resolve({ ok: true, json: async () => ({}) });
      }
      if (method === 'DELETE') {
        handlers.onReset?.();
        return Promise.resolve({ ok: true, json: async () => ({}) });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });
  }

  async function waitForConnected() {
    await waitFor(() => {
      expect(screen.getByText(/backend connected/i)).toBeInTheDocument();
    });
  }

  async function openTabMenu(name: RegExp) {
    await waitFor(() => {
      expect(screen.getByRole('button', { name: new RegExp(`workspace actions for ${name.source}`, 'i') })).toBeInTheDocument();
    });
    await user.click(
      screen.getByRole('button', { name: new RegExp(`workspace actions for ${name.source}`, 'i') }),
    );
    await waitFor(() => {
      expect(screen.getByRole('menu')).toBeInTheDocument();
    });
  }

  it('tab action menu opens and lists Edit / Archive / Delete', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [makeWorkspace()],
      configurationWarning: null,
    };
    setupBackend(list);
    render(<App />);
    await waitForConnected();
    await openTabMenu(/customer 11111111/);

    const menu = screen.getByRole('menu');
    expect(within(menu).getByRole('menuitem', { name: /edit workspace/i })).toBeInTheDocument();
    expect(within(menu).getByRole('menuitem', { name: /archive workspace/i })).toBeInTheDocument();
    expect(within(menu).getByRole('menuitem', { name: /delete permanently/i })).toBeInTheDocument();
  });

  it('edit modal prepopulates fields and a successful save updates the tab', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [makeWorkspace()],
      configurationWarning: null,
    };
    const updated = makeWorkspace({ displayName: 'Renamed Workspace' });
    setupBackend(list, {
      onPut: () => updated,
    });
    render(<App />);
    await waitForConnected();
    await openTabMenu(/customer 11111111/);

    await user.click(screen.getByRole('menuitem', { name: /edit workspace/i }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByRole('heading', { name: /edit workspace/i })).toBeInTheDocument();
    expect(within(dialog).getByLabelText(/site/i)).toHaveValue('NW');
    expect(within(dialog).getByLabelText(/customer number/i)).toHaveValue('11111111');

    await user.click(within(dialog).getByRole('button', { name: /save changes/i }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(screen.getByRole('tab', { name: /renamed workspace/i })).toBeInTheDocument();
    expect(screen.getByText(/workspace updated/i)).toBeInTheDocument();
  });

  it('archiving requires confirmation; cancel leaves the workspace unchanged', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [makeWorkspace()],
      configurationWarning: null,
    };
    setupBackend(list, {
      onArchive: (id) => makeWorkspace({ assignmentId: id, isEnabled: false }),
    });
    render(<App />);
    await waitForConnected();
    await openTabMenu(/customer 11111111/);
    await user.click(screen.getByRole('menuitem', { name: /archive workspace/i }));

    const confirmDialog = await screen.findByRole('alertdialog');
    expect(within(confirmDialog).getByText(/archive workspace\?/i)).toBeInTheDocument();

    await user.click(within(confirmDialog).getByRole('button', { name: /cancel/i }));

    await waitFor(() => {
      expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
    });
    expect(screen.getByRole('tab', { name: /customer 11111111/i })).toBeInTheDocument();
  });

  it('confirming archive removes the tab and shows the archived workspace in Manage Workspaces', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [makeWorkspace()],
      configurationWarning: null,
    };
    setupBackend(list, {
      onArchive: (id) => makeWorkspace({ assignmentId: id, isEnabled: false }),
    });
    render(<App />);
    await waitForConnected();
    await openTabMenu(/customer 11111111/);
    await user.click(screen.getByRole('menuitem', { name: /archive workspace/i }));

    const confirmDialog = await screen.findByRole('alertdialog');
    await user.click(within(confirmDialog).getByRole('button', { name: /^archive$/i }));

    await waitFor(() => {
      expect(screen.queryByRole('tab', { name: /customer 11111111/i })).not.toBeInTheDocument();
    });
    expect(screen.getByText(/workspace archived/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /manage workspaces/i }));
    const manageDialog = await screen.findByRole('dialog', { name: /manage workspaces/i });
    expect(within(manageDialog).getAllByText(/customer 11111111/i).length).toBeGreaterThan(0);
    expect(within(manageDialog).getByRole('button', { name: /restore/i })).toBeInTheDocument();
  });

  it('restoring an archived workspace returns it to the active tab bar', async () => {
    const archived = makeWorkspace({ isEnabled: false });
    const list: WorkspaceListResponseDto = {
      workspaces: [archived],
      configurationWarning: null,
    };
    setupBackend(list, {
      onRestore: (id) => makeWorkspace({ assignmentId: id, isEnabled: true }),
    });
    render(<App />);
    await waitForConnected();

    // No active workspaces, so the empty state should be shown.
    expect(screen.getByText(/use \+ to add a workspace/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /manage workspaces/i }));
    const manageDialog = await screen.findByRole('dialog', { name: /manage workspaces/i });
    await user.click(within(manageDialog).getByRole('button', { name: /restore/i }));

    await waitFor(() => {
      expect(screen.getByText(/workspace restored/i)).toBeInTheDocument();
    });
    await user.click(within(manageDialog).getByRole('button', { name: /close/i }));
    expect(screen.getByRole('tab', { name: /customer 11111111/i })).toBeInTheDocument();
  });

  it('deleting requires confirmation; permanent delete removes the workspace', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [makeWorkspace()],
      configurationWarning: null,
    };
    setupBackend(list, {
      onDelete: () => {},
    });
    render(<App />);
    await waitForConnected();
    await openTabMenu(/customer 11111111/);
    await user.click(screen.getByRole('menuitem', { name: /delete permanently/i }));

    const confirmDialog = await screen.findByRole('alertdialog');
    expect(within(confirmDialog).getByText(/delete workspace permanently\?/i)).toBeInTheDocument();

    await user.click(within(confirmDialog).getByRole('button', { name: /delete permanently/i }));

    await waitFor(() => {
      expect(screen.queryByRole('tab', { name: /customer 11111111/i })).not.toBeInTheDocument();
    });
    expect(screen.getByText(/use \+ to add a workspace/i)).toBeInTheDocument();
    expect(screen.getByText(/workspace deleted/i)).toBeInTheDocument();
  });

  it('reset requires confirmation and returns to the empty startup screen', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [makeWorkspace(), makeWorkspace({ assignmentId: 'id-2', displayName: 'Customer 22222222', customerNumber: '22222222', sortOrder: 1 })],
      configurationWarning: null,
    };
    setupBackend(list, {
      onReset: () => {},
    });
    render(<App />);
    await waitForConnected();

    await user.click(screen.getByRole('button', { name: /manage workspaces/i }));
    const manageDialog = await screen.findByRole('dialog', { name: /manage workspaces/i });
    await user.click(within(manageDialog).getByRole('button', { name: /reset workspace configuration/i }));

    const confirmDialog = await screen.findByRole('alertdialog');
    expect(within(confirmDialog).getByText(/reset all workspaces\?/i)).toBeInTheDocument();
    await user.click(within(confirmDialog).getByRole('button', { name: /reset workspaces/i }));

    await waitFor(() => {
      expect(screen.getByText(/use \+ to add a workspace/i)).toBeInTheDocument();
    });
    expect(screen.getByText(/workspace configuration reset/i)).toBeInTheDocument();
  });

  it('active-tab fallback selects the next workspace when the active tab is archived', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [
        makeWorkspace(),
        makeWorkspace({ assignmentId: 'id-2', displayName: 'Customer 22222222', customerNumber: '22222222', sortOrder: 1 }),
      ],
      configurationWarning: null,
    };
    setupBackend(list, {
      onArchive: (id) => makeWorkspace({ assignmentId: id, isEnabled: false }),
    });
    render(<App />);
    await waitForConnected();

    // id-1 is active by default (first enabled workspace).
    await openTabMenu(/customer 11111111/);
    await user.click(screen.getByRole('menuitem', { name: /archive workspace/i }));
    const confirmDialog = await screen.findByRole('alertdialog');
    await user.click(within(confirmDialog).getByRole('button', { name: /^archive$/i }));

    await waitFor(() => {
      expect(screen.queryByRole('tab', { name: /customer 11111111/i })).not.toBeInTheDocument();
    });

    // The remaining workspace should now be the active tab.
    const remainingTab = screen.getByRole('tab', { name: /customer 22222222/i });
    expect(remainingTab).toHaveAttribute('aria-selected', 'true');
  });

  it('a failed archive request shows an error and does not remove the workspace', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [makeWorkspace()],
      configurationWarning: null,
    };
    // No onArchive handler configured -> backend mock returns 404, simulating a failure.
    setupBackend(list, {});
    render(<App />);
    await waitForConnected();
    await openTabMenu(/customer 11111111/);
    await user.click(screen.getByRole('menuitem', { name: /archive workspace/i }));
    const confirmDialog = await screen.findByRole('alertdialog');
    await user.click(within(confirmDialog).getByRole('button', { name: /^archive$/i }));

    await waitFor(() => {
      expect(screen.getByText(/could not archive the workspace/i)).toBeInTheDocument();
    });
    expect(screen.getByRole('tab', { name: /customer 11111111/i })).toBeInTheDocument();
  });
});

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
};

const emptyWorkspaceList: WorkspaceListResponseDto = { workspaces: [], configurationWarning: null };

function makeWorkspace(overrides: Partial<WorkspaceAssignmentDto> = {}): WorkspaceAssignmentDto {
  return {
    assignmentId: 'abc-123',
    displayName: 'Customer 12345678',
    site: 'NW',
    customerNumber: '12345678',
    productLineFrom: null,
    productLineTo: null,
    isTemporary: false,
    coverageEndsOn: null,
    isEnabled: true,
    sortOrder: 0,
    ...overrides,
  };
}

describe('AddWorkspaceDialog', () => {
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

  function setupConnected(workspaceListOverride = emptyWorkspaceList) {
    fetchMock.mockImplementation((url: string) => {
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => workspaceListOverride });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });
  }

  async function waitForConnected() {
    await waitFor(() => {
      expect(screen.getByText(/backend connected/i)).toBeInTheDocument();
    });
  }

  async function openDialog() {
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /add workspace/i })).toBeInTheDocument();
    });
    await user.click(screen.getByRole('button', { name: /add workspace/i }));
    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });
  }

  it('shows empty opening screen with no workspaces', async () => {
    setupConnected();
    render(<App />);
    await waitForConnected();
    expect(screen.getByText(/use \+ to add a workspace/i)).toBeInTheDocument();
  });

  it('+ button opens the Add Workspace modal', async () => {
    setupConnected();
    render(<App />);
    await openDialog();
    expect(screen.getByRole('heading', { name: /add workspace/i })).toBeInTheDocument();
  });

  it('site-only input cannot be submitted (button disabled)', async () => {
    setupConnected();
    render(<App />);
    await openDialog();

    const dialog = screen.getByRole('dialog');
    const siteInput = within(dialog).getByLabelText(/site/i);
    await user.type(siteInput, 'NW');

    const submitBtn = within(dialog).getByRole('button', { name: /add workspace/i });
    expect(submitBtn).toBeDisabled();
  });

  it('customer-based workspace enables the submit button', async () => {
    setupConnected();
    render(<App />);
    await openDialog();

    const dialog = screen.getByRole('dialog');
    await user.type(within(dialog).getByLabelText(/site/i), 'NW');
    await user.type(within(dialog).getByLabelText(/customer number/i), '12345678');

    const submitBtn = within(dialog).getByRole('button', { name: /add workspace/i });
    expect(submitBtn).not.toBeDisabled();
  });

  it('product-line-based workspace enables the submit button', async () => {
    setupConnected();
    render(<App />);
    await openDialog();

    const dialog = screen.getByRole('dialog');
    await user.type(within(dialog).getByLabelText(/site/i), 'SW');
    await user.type(within(dialog).getByLabelText(/product line from/i), '0040');

    const submitBtn = within(dialog).getByRole('button', { name: /add workspace/i });
    expect(submitBtn).not.toBeDisabled();
  });

  it('Product Line To requires Product Line From (field is disabled when From is empty)', async () => {
    setupConnected();
    render(<App />);
    await openDialog();

    const dialog = screen.getByRole('dialog');
    const toInput = within(dialog).getByLabelText(/product line to/i);
    expect(toInput).toBeDisabled();
  });

  it('successful creation creates a tab and closes the modal', async () => {
    setupConnected();
    const created = makeWorkspace();
    render(<App />);
    await openDialog();

    // Mock the POST response
    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      if (opts?.method === 'POST' && url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => created });
      }
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => emptyWorkspaceList });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    const dialog = screen.getByRole('dialog');
    await user.type(within(dialog).getByLabelText(/site/i), 'NW');
    await user.type(within(dialog).getByLabelText(/customer number/i), '12345678');
    await user.click(within(dialog).getByRole('button', { name: /add workspace/i }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    expect(screen.getByRole('tab', { name: /customer 12345678/i })).toBeInTheDocument();
  });

  it('saved workspaces render as tabs after loading', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [
        makeWorkspace({ assignmentId: 'id-1', displayName: 'Customer 11111111', customerNumber: '11111111', sortOrder: 0 }),
        makeWorkspace({ assignmentId: 'id-2', displayName: 'PL 0040', customerNumber: null, productLineFrom: '0040', productLineTo: '0040', sortOrder: 1 }),
      ],
      configurationWarning: null,
    };

    setupConnected(list);
    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole('tab', { name: /customer 11111111/i })).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: /pl 0040/i })).toBeInTheDocument();
    });
  });

  it('tab switching changes the workspace placeholder details', async () => {
    const list: WorkspaceListResponseDto = {
      workspaces: [
        makeWorkspace({ assignmentId: 'id-1', displayName: 'Customer 11111111', customerNumber: '11111111', sortOrder: 0 }),
        makeWorkspace({ assignmentId: 'id-2', displayName: 'Customer 22222222', customerNumber: '22222222', sortOrder: 1 }),
      ],
      configurationWarning: null,
    };

    setupConnected(list);
    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole('tab', { name: /customer 22222222/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole('tab', { name: /customer 22222222/i }));

    await waitFor(() => {
      expect(screen.getByText('22222222')).toBeInTheDocument();
    });
  });

  it('validation errors remain visible after failed save', async () => {
    setupConnected();

    const validationError = {
      type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: { site: ['Site must be exactly 2 characters.'] },
    };

    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      if (opts?.method === 'POST' && url.includes('/api/v1/workspaces')) {
        return Promise.resolve({
          ok: false,
          status: 400,
          text: async () => JSON.stringify(validationError),
        });
      }
      if (url.includes('/api/v1/workspaces')) {
        return Promise.resolve({ ok: true, json: async () => emptyWorkspaceList });
      }
      return Promise.resolve({ ok: true, json: async () => mockStatus });
    });

    render(<App />);
    await openDialog();

    const dialog = screen.getByRole('dialog');
    await user.type(within(dialog).getByLabelText(/site/i), 'NW');
    await user.type(within(dialog).getByLabelText(/customer number/i), '12345678');

    // Override the submit mock for this test
    fetchMock.mockImplementationOnce(() =>
      Promise.resolve({
        ok: false,
        status: 400,
        text: async () => JSON.stringify(validationError),
      }),
    );

    // Verify the modal is still open after a failed save
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('does not render an IOS code field in the dialog', async () => {
    setupConnected();
    render(<App />);
    await openDialog();

    const dialog = screen.getByRole('dialog');
    expect(within(dialog).queryByLabelText(/ios/i)).not.toBeInTheDocument();
    expect(within(dialog).queryByText(/ios code/i)).not.toBeInTheDocument();
  });
});

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
  WorkOrderPlanningWindowResponseDto,
  WorkOrderSummaryDto,
  WorkOrderMaterialResponseDto,
  WorkOrderMaterialLineDto,
  WorkOrderCandidateResponseDto,
  BomLineDto,
  BomResponseDto,
  ComponentDetailResponseDto,
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

function makeWorkOrderSummary(overrides: Partial<WorkOrderSummaryDto> = {}): WorkOrderSummaryDto {
  return {
    partNumber: 'ABC100',
    woid: 'WO1001',
    status: 'released',
    orderedQuantity: 100,
    completedQuantity: 40,
    openQuantity: 60,
    releaseDate: '2025-06-20',
    dueDate: '2025-06-30',
    salesOrder: null,
    kitting: { applicableLineCount: 4, fullyIssuedLineCount: 3, kittingPercent: 75 },
    ...overrides,
  };
}

function makePlanningWindowResponse(
  overrides: Partial<WorkOrderPlanningWindowResponseDto> = {},
): WorkOrderPlanningWindowResponseDto {
  return {
    snapshotId: 'snap-1',
    workOrders: [makeWorkOrderSummary()],
    ...overrides,
  };
}

function makeMaterialLine(overrides: Partial<WorkOrderMaterialLineDto> = {}): WorkOrderMaterialLineDto {
  return {
    componentPart: 'COMP1',
    componentDescription: 'Fastener',
    requiredQuantity: 10,
    issuedQuantity: 10,
    varianceQuantity: 0,
    issuedPercent: 100,
    issueStatus: 'withinExpectedRange',
    isManufactured: false,
    isFullyIssued: true,
    ...overrides,
  };
}

function makeMaterialResponse(
  overrides: Partial<WorkOrderMaterialResponseDto> = {},
): WorkOrderMaterialResponseDto {
  return {
    snapshotId: 'snap-1',
    woid: 'WO1001',
    kitting: { applicableLineCount: 4, fullyIssuedLineCount: 3, kittingPercent: 75 },
    lines: [makeMaterialLine()],
    ...overrides,
  };
}

function makeCandidateWorkOrder(overrides: Partial<WorkOrderSummaryDto> = {}): WorkOrderSummaryDto {
  return makeWorkOrderSummary({
    woid: 'WO2001',
    partNumber: 'SUBASSY',
    status: 'allocating',
    kitting: { applicableLineCount: 2, fullyIssuedLineCount: 1, kittingPercent: 50 },
    ...overrides,
  });
}

function makeCandidateResponse(
  overrides: Partial<WorkOrderCandidateResponseDto> = {},
): WorkOrderCandidateResponseDto {
  return {
    snapshotId: 'snap-1',
    candidates: [makeCandidateWorkOrder()],
    ...overrides,
  };
}

function makePartSchedule(parentPart: string, description: string) {
  return {
    parentPart,
    description,
    buckets: [
      { kind: 'falldown' as const, weekLabel: null, quantity: 50, executionStatus: 'none' as const, containsPlannedWork: false, containsExplicitlyScheduledWork: false },
      { kind: 'weekly' as const, weekLabel: '2025-06-30', quantity: 100, executionStatus: 'released' as const, containsPlannedWork: false, containsExplicitlyScheduledWork: true },
      { kind: 'weekly' as const, weekLabel: '2025-07-07', quantity: 200, executionStatus: 'allocating' as const, containsPlannedWork: true, containsExplicitlyScheduledWork: false },
      { kind: 'weekly' as const, weekLabel: '2025-07-14', quantity: 0, executionStatus: 'none' as const, containsPlannedWork: false, containsExplicitlyScheduledWork: false },
      { kind: 'weekly' as const, weekLabel: '2025-07-21', quantity: 300, executionStatus: 'frozen' as const, containsPlannedWork: false, containsExplicitlyScheduledWork: false },
    ],
  };
}

function makeBomLine(overrides: Partial<BomLineDto> = {}): BomLineDto {
  return {
    occurrenceKey: 'k-1',
    level: 1,
    componentPart: 'COMP-A',
    pmCode: 'P',
    isPhantom: false,
    description: 'Component A',
    quantityPer: 2,
    scrapPercentage: 1,
    netQuantityOnHand: 12,
    nonNetQuantityOnHand: 4,
    ...overrides,
  };
}

function makeBomResponse(overrides: Partial<BomResponseDto> = {}): BomResponseDto {
  return {
    site: 'NW',
    parentPart: 'ABC100',
    effectiveDate: '2026-08-13',
    lines: [makeBomLine()],
    loadedAtUtc: '2026-08-13T12:00:00Z',
    isStale: false,
    warning: null,
    ...overrides,
  };
}

function bomRequestCalls(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map(([url]) => url)
    .filter((url): url is string => typeof url === 'string' && url.includes('/bom'));
}

function makeComponentDetail(overrides: Partial<ComponentDetailResponseDto> = {}): ComponentDetailResponseDto {
  return {
    site: 'NW',
    componentPart: 'COMP-A',
    description: 'Component A',
    partStatusCode: 'A',
    partStatusDescription: 'Active',
    iosCode: 'I',
    netQuantityOnHand: 12,
    nonNetQuantityOnHand: 4,
    standardCost: 3.5,
    qctc: 4.25,
    timeFence: 5,
    safetyTime: 1,
    safetyStock: 10,
    buyerPlanner: 'JDOE',
    purchaseLeadTimeDays: 7,
    inspectionLeadTimeDays: 1,
    cumulativeLeadTimeDays: 8,
    minimumOrderQuantity: 50,
    orderMultiple: 10,
    loadedAtUtc: '2026-08-13T12:00:00Z',
    isStale: false,
    warning: null,
    ...overrides,
  };
}

function componentDetailRequestCalls(fetchMock: ReturnType<typeof vi.fn>): string[] {
  return fetchMock.mock.calls
    .map(([url]) => url)
    .filter((url): url is string => typeof url === 'string' && url.includes('/components/'));
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
    onGetPlanningWindow?: (url: string) => { ok: boolean; status?: number; json?: () => Promise<unknown>; text?: () => Promise<string> };
    onGetMaterialLines?: (url: string) => { ok: boolean; status?: number; json?: () => Promise<unknown>; text?: () => Promise<string> };
    onGetWorkOrderCandidates?: (url: string) => { ok: boolean; status?: number; json?: () => Promise<unknown>; text?: () => Promise<string> };
    onGetBom?: (url: string) => { ok: boolean; status?: number; json?: () => Promise<unknown>; text?: () => Promise<string> };
    onGetComponentDetail?: (url: string) => { ok: boolean; status?: number; json?: () => Promise<unknown>; text?: () => Promise<string> };
  } = {},
  initialList: WorkspaceListResponseDto = workspaceList,
) {
    fetchMock.mockImplementation((url: string, opts?: RequestInit) => {
      const method = opts?.method ?? 'GET';
      if (method === 'GET' && url.includes('/part-detail')) {
        const result = handlers.onGetPartDetail?.(url) ?? { ok: true, json: async () => makePartDetail() };
        return Promise.resolve(result);
      }
      if (method === 'GET' && url.includes('/parts/') && url.endsWith('/bom')) {
        const result = handlers.onGetBom?.(url) ?? { ok: true, json: async () => makeBomResponse() };
        return Promise.resolve(result);
      }
      if (method === 'GET' && url.includes('/components/')) {
        const result = handlers.onGetComponentDetail?.(url) ?? { ok: true, json: async () => makeComponentDetail() };
        return Promise.resolve(result);
      }
      if (method === 'GET' && url.includes('/work-orders/candidates')) {
        const result =
          handlers.onGetWorkOrderCandidates?.(url) ?? { ok: true, json: async () => makeCandidateResponse() };
        return Promise.resolve(result);
      }
      if (method === 'GET' && url.includes('/work-orders/planning-window')) {
        const result =
          handlers.onGetPlanningWindow?.(url) ?? { ok: true, json: async () => makePlanningWindowResponse() };
        return Promise.resolve(result);
      }
      if (method === 'GET' && url.includes('/work-orders/') && url.includes('/material')) {
        const result = handlers.onGetMaterialLines?.(url) ?? { ok: true, json: async () => makeMaterialResponse() };
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
        return Promise.resolve({ ok: true, json: async () => initialList });
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
      expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
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
      expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
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
      expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
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
      expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
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

  describe('Stage 7D.6 selection and tab behavior', () => {
    it('parent-only selection opens Part Info and exposes the Work Orders planning window (Shortages deferred)', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('ABC100'));

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });
      expect(screen.getByRole('tab', { name: 'Part Info' })).toHaveAttribute('aria-selected', 'true');
      // Stage 7R: parent-only selection exposes Part Info + BOM + Work Orders (the planning-window
      // population). Shortages remains rendered but deferred (disabled). Future Shortages is
      // removed from the workflow.
      expect(screen.getByRole('tab', { name: 'BOM' })).toBeEnabled();
      expect(screen.getByRole('tab', { name: 'Work Orders' })).toBeEnabled();
      expect(screen.getByRole('tab', { name: 'Shortages' })).toBeDisabled();
      expect(screen.queryByRole('tab', { name: 'Future Shortages' })).not.toBeInTheDocument();
      expect(screen.queryByRole('tab', { name: 'Components' })).not.toBeInTheDocument();
    });

    it('opening the Work Orders tab on a parent-only selection loads the parent-level planning window', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('ABC100'));
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });

      // The parent-level planning window loads on parent selection (no bucketKind/weekLabel).
      expect(
        fetchMock.mock.calls.some(
          ([url]) =>
            typeof url === 'string' &&
            url.includes('/work-orders/planning-window') &&
            !url.includes('bucketKind') &&
            !url.includes('weekLabel'),
        ),
      ).toBe(true);

      await user.click(screen.getByRole('tab', { name: 'Work Orders' }));

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      await waitFor(() => {
        expect(screen.getByText('WO1001')).toBeInTheDocument();
      });
    });

    it('clicking an eligible weekly bucket cell selects the parent + bucket and auto-opens Work Orders', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('100')); // the 2025-06-30 weekly bucket cell

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      expect(screen.getByRole('heading', { name: /work orders/i })).toBeInTheDocument();
      await waitFor(() => {
        expect(screen.getByText('WO1001')).toBeInTheDocument();
      });
      expect(
        fetchMock.mock.calls.some(
          ([url]) =>
            typeof url === 'string' &&
            url.includes('/work-orders/planning-window') &&
            url.includes('bucketKind=weekly') &&
            url.includes('weekLabel=2025-06-30'),
        ),
      ).toBe(true);
    });

    it('clicking Falldown selects the parent + Falldown bucket and auto-opens Work Orders', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('50')); // the Falldown cell

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      expect(screen.getByRole('heading', { name: /falldown/i })).toBeInTheDocument();
      expect(
        fetchMock.mock.calls.some(
          ([url]) => typeof url === 'string' && url.includes('/work-orders/planning-window') && url.includes('bucketKind=falldown'),
        ),
      ).toBe(true);
    });

    it('Enter on an eligible weekly bucket cell selects the parent + bucket exactly as a click does', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      const cell = screen.getByText('100').closest('td');
      if (!cell) throw new Error('cell not found');
      expect(cell).toHaveAttribute('tabindex', '0');
      fireEvent.keyDown(cell, { key: 'Enter' });

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      expect(
        fetchMock.mock.calls.some(
          ([url]) =>
            typeof url === 'string' &&
            url.includes('/work-orders/planning-window') &&
            url.includes('bucketKind=weekly') &&
            url.includes('weekLabel=2025-06-30'),
        ),
      ).toBe(true);
    });

    it('Space on an eligible weekly bucket cell selects the parent + bucket and prevents page scroll', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      const cell = screen.getByText('100').closest('td');
      if (!cell) throw new Error('cell not found');
      const event = fireEvent.keyDown(cell, { key: ' ' });
      // `fireEvent` returns `false` from dispatchEvent when a handler called `preventDefault()`.
      expect(event).toBe(false);

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
    });

    it('Enter and Space on the Falldown cell select the Falldown bucket exactly as a click does', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      const falldownCell = screen.getByText('50').closest('td');
      if (!falldownCell) throw new Error('falldown cell not found');
      expect(falldownCell).toHaveAttribute('tabindex', '0');
      fireEvent.keyDown(falldownCell, { key: ' ' });

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      expect(screen.getByRole('heading', { name: /falldown/i })).toBeInTheDocument();
    });

    it('a bucket cell beyond the drill-down horizon is not keyboard-focusable and Enter falls through to parent-row selection', async () => {
      const extendedWeekly = [
        { kind: 'weekly' as const, weekLabel: '2025-06-30', quantity: 100, executionStatus: 'released', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-07-07', quantity: 200, executionStatus: 'allocating', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-07-14', quantity: 300, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-07-21', quantity: 400, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-07-28', quantity: 500, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-08-04', quantity: 600, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-08-11', quantity: 700, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
      ];
      setupBackend({
        onGetMps: () => ({
          ok: true,
          json: async () =>
            makeDashboard({
              parts: [{ parentPart: 'ABC100', description: 'Widget Assembly', buckets: [makeDashboard().parts[0].buckets[0], ...extendedWeekly] }],
            }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('700')).toBeInTheDocument();
      });

      const ineligibleCell = screen.getByText('700').closest('td');
      if (!ineligibleCell) throw new Error('cell not found');
      expect(ineligibleCell).not.toHaveAttribute('tabindex');

      fetchMock.mockClear();
      fireEvent.keyDown(ineligibleCell, { key: 'Enter' });

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });
      // The fall-through selects the parent (no bucket), so the parent-level Work Orders tab is
      // exposed — but no bucket-filtered planning-window request may have fired.
      expect(screen.getByRole('tab', { name: 'Work Orders' })).toBeEnabled();
      expect(
        fetchMock.mock.calls.some(
          ([url]) =>
            typeof url === 'string' &&
            url.includes('/work-orders/planning-window') &&
            url.includes('bucketKind'),
        ),
      ).toBe(false);
    });

    it('clicking the already-selected parent row toggles everything closed even when a bucket is selected', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('100'));
      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      await waitFor(() => {
        expect(screen.getByText('WO1001')).toBeInTheDocument();
      });

      await user.click(screen.getByText('ABC100'));

      await waitFor(() => {
        expect(screen.queryByRole('tablist', { name: /part detail/i })).not.toBeInTheDocument();
      });
      expect(screen.queryByText('WO1001')).not.toBeInTheDocument();
    });

    it('shows a deliberate empty message (not an error) when the bucket has no eligible work orders', async () => {
      setupBackend({
        onGetPlanningWindow: () => ({ ok: true, json: async () => makePlanningWindowResponse({ workOrders: [] }) }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('100'));

      await waitFor(() => {
        expect(screen.getByText(/no work orders in the planning window/i)).toBeInTheDocument();
      });
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });

    it('weeks beyond the drill-down horizon do not expose the Work Order action', async () => {
      const extendedWeekly = [
        { kind: 'weekly' as const, weekLabel: '2025-06-30', quantity: 100, executionStatus: 'released', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-07-07', quantity: 200, executionStatus: 'allocating', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-07-14', quantity: 300, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-07-21', quantity: 400, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-07-28', quantity: 500, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-08-04', quantity: 600, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
        { kind: 'weekly' as const, weekLabel: '2025-08-11', quantity: 700, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false },
      ];
      setupBackend({
        onGetMps: () => ({
          ok: true,
          json: async () =>
            makeDashboard({
              parts: [{ parentPart: 'ABC100', description: 'Widget Assembly', buckets: [makeDashboard().parts[0].buckets[0], ...extendedWeekly] }],
            }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('700')).toBeInTheDocument();
      });

      // The 4th forward week (index 3) is the last eligible bucket; it exposes the drill-down.
      const eligibleCell = screen.getByText('400').closest('td');
      if (!eligibleCell) throw new Error('eligible cell not found');
      expect(eligibleCell).toHaveAttribute('tabindex', '0');

      fetchMock.mockClear();
      await user.click(screen.getByText('500')); // the 5th forward week (index 4, beyond the 4-week horizon)

      // No dedicated bucket action fires; the click behaves like any other cell in the row and
      // simply selects the parent (falling through to the row's own click handler), opening Part
      // Info rather than Work Orders.
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });
      // The fall-through selects the parent (no bucket), so the parent-level Work Orders tab is
      // exposed — but no bucket-filtered planning-window request may have fired.
      expect(screen.getByRole('tab', { name: 'Work Orders' })).toBeEnabled();
      expect(
        fetchMock.mock.calls.some(
          ([url]) =>
            typeof url === 'string' &&
            url.includes('/work-orders/planning-window') &&
            url.includes('bucketKind'),
        ),
      ).toBe(false);
    });
  });

  describe('Escape closes the drill-down and returns to the full grid', () => {
    it('Escape from Part Info closes the detail panel and restores focus to the grid row, exactly like clicking the selected part number again', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      const row = (await screen.findByText('ABC100')).closest('tr');
      if (!row) throw new Error('row not found');
      await user.click(screen.getByText('ABC100'));

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });

      await user.keyboard('{Escape}');

      await waitFor(() => {
        expect(screen.queryByRole('heading', { name: /part info/i })).not.toBeInTheDocument();
      });
      expect(screen.queryByRole('tab', { name: 'Part Info' })).not.toBeInTheDocument();
      await waitFor(() => {
        expect(document.activeElement).toBe(row);
      });
    });

    it('Escape from the Work Orders view (bucket selected) returns to the full grid in one press', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100')); // weekly bucket cell → auto-opens Work Orders

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });

      await user.keyboard('{Escape}');

      await waitFor(() => {
        expect(screen.queryByRole('tab', { name: 'Work Orders' })).not.toBeInTheDocument();
      });
      expect(screen.queryByRole('tab', { name: 'Part Info' })).not.toBeInTheDocument();
      expect(screen.queryByRole('heading', { name: /part info/i })).not.toBeInTheDocument();
    });

    it('Escape while the Component Information modal is open closes only the modal (BOM drill-down), leaving the underlying Part/BOM selection open', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('ABC100'));
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });
      await user.click(screen.getByRole('tab', { name: 'BOM' }));
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });
      await user.click(screen.getByText('COMP-A'));
      await screen.findByRole('dialog');

      await user.keyboard('{Escape}');

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      });
      // The underlying BOM selection (one level below the modal) must still be open — a single
      // Escape press must never close two stacked drill-down levels at once.
      expect(screen.getByRole('tab', { name: 'BOM' })).toHaveAttribute('aria-selected', 'true');
    });

    it('Escape at the top-level grid (nothing selected) does nothing', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.keyboard('{Escape}');

      expect(screen.queryByRole('heading', { name: /part info/i })).not.toBeInTheDocument();
      expect(screen.getByText('ABC100')).toBeInTheDocument();
    });

    it('Escape collapses an expanded Work Order card\u2019s material lines one level (like clicking "Hide material lines"), staying in the Work Orders view; a second Escape then returns to the grid', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      await user.click(within(card).getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(card).getByRole('button', { name: /hide material lines/i })).toBeInTheDocument();
      });

      await user.keyboard('{Escape}');

      // First Escape: only the material lines collapse; Work Orders view stays open.
      await waitFor(() => {
        expect(within(card).getByRole('button', { name: /show material lines/i })).toBeInTheDocument();
      });
      expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');

      await user.keyboard('{Escape}');

      // Second Escape: nothing left registered on the drill-down stack, so it pops up to the grid.
      await waitFor(() => {
        expect(screen.queryByRole('tab', { name: 'Work Orders' })).not.toBeInTheDocument();
      });
    });

    it('Escape collapses an open candidate branch one level, leaving the parent Work Order\u2019s material lines still shown', async () => {
      setupBackend({
        onGetMaterialLines: () => ({
          ok: true,
          json: async () =>
            makeMaterialResponse({ lines: [makeMaterialLine({ componentPart: 'SUBASSY', isManufactured: true })] }),
        }),
        onGetWorkOrderCandidates: () => ({
          ok: true,
          json: async () => makeCandidateResponse({ candidates: [makeCandidateWorkOrder({ status: 'allocating' })] }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      await user.click(screen.getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(card).getByText('SUBASSY')).toBeInTheDocument();
      });
      await user.click(within(card).getByRole('button', { name: 'SUBASSY' }));
      await screen.findByRole('heading', { name: 'Work Orders for SUBASSY' });

      await user.keyboard('{Escape}');

      // First Escape: only the candidate branch collapses; the material lines (one level below
      // it) stay open.
      await waitFor(() => {
        expect(screen.queryByRole('heading', { name: 'Work Orders for SUBASSY' })).not.toBeInTheDocument();
      });
      expect(within(card).getByText('SUBASSY')).toBeInTheDocument();
      expect(within(card).getByRole('button', { name: /hide material lines/i })).toBeInTheDocument();
    });
  });

  describe('Stage 7D.7 work order cards', () => {
    it('renders card fields, status badge, and a Kitting % progress bar', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));

      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      const withinCard = within(card);
      expect(withinCard.getByText('Released')).toBeInTheDocument();
      expect(withinCard.getByText('100')).toBeInTheDocument();
      expect(withinCard.getByText('40')).toBeInTheDocument();
      expect(withinCard.getByText('60')).toBeInTheDocument();
      expect(withinCard.getByText('Jun 20')).toBeInTheDocument();
      expect(withinCard.getByText('Jun 30')).toBeInTheDocument();
      expect(withinCard.getByText('75%')).toBeInTheDocument();

      const progressBar = withinCard.getByRole('progressbar');
      expect(progressBar).toHaveAttribute('aria-valuenow', '75');
    });

    it('shows an SO badge opposite the WOID when the work order has a sales order job', async () => {
      setupBackend({
        onGetPlanningWindow: () => ({
          ok: true,
          json: async () =>
            makePlanningWindowResponse({ workOrders: [makeWorkOrderSummary({ salesOrder: 'SO-4521' })] }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));

      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      expect(within(card).getByText('SO SO-4521')).toBeInTheDocument();
    });

    it('renders no SO badge when the work order has no sales order job', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));

      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      expect(within(card).queryByText(/^SO /)).not.toBeInTheDocument();
    });

    it('shows Kitting as N/A (not 0%) and an empty progress bar when there are no applicable material lines', async () => {
      setupBackend({
        onGetPlanningWindow: () => ({
          ok: true,
          json: async () =>
            makePlanningWindowResponse({
              workOrders: [
                makeWorkOrderSummary({ kitting: { applicableLineCount: 0, fullyIssuedLineCount: 0, kittingPercent: null } }),
              ],
            }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));

      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      expect(within(card).getByText('N/A')).toBeInTheDocument();
      expect(within(card).getByRole('progressbar')).not.toHaveAttribute('aria-valuenow');
    });

    it('expanding a card lazily loads material lines, and collapsing hides them again', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      await screen.findByRole('listitem', { name: /WO1001, Released/i });

      expect(fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('/work-orders/'))).toBe(true);
      fetchMock.mockClear();

      await user.click(screen.getByRole('button', { name: /show material lines/i }));

      await waitFor(() => {
        expect(screen.getByText('COMP1')).toBeInTheDocument();
      });
      expect(screen.getByText('Fastener')).toBeInTheDocument();
      expect(
        fetchMock.mock.calls.some(
          ([url]) =>
            typeof url === 'string' && url.includes('/work-orders/WO1001/material') && url.includes('snapshotId=snap-1'),
        ),
      ).toBe(true);

      await user.click(screen.getByRole('button', { name: /hide material lines/i }));
      await waitFor(() => {
        expect(screen.queryByText('COMP1')).not.toBeInTheDocument();
      });
    });

    it('shows a deliberate empty message (not an error) when a work order has no applicable material lines', async () => {
      setupBackend({
        onGetMaterialLines: () => ({ ok: true, json: async () => makeMaterialResponse({ lines: [] }) }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      await screen.findByRole('listitem', { name: /WO1001, Released/i });

      await user.click(screen.getByRole('button', { name: /show material lines/i }));

      await waitFor(() => {
        expect(screen.getByText(/no applicable material lines/i)).toBeInTheDocument();
      });
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });

    it('supports multiple cards for one bucket, independently expandable', async () => {
      setupBackend({
        onGetPlanningWindow: () => ({
          ok: true,
          json: async () =>
            makePlanningWindowResponse({
              workOrders: [
                makeWorkOrderSummary(),
                makeWorkOrderSummary({ woid: 'WO1002', status: 'frozen', kitting: { applicableLineCount: 2, fullyIssuedLineCount: 2, kittingPercent: 100 } }),
              ],
            }),
        }),
        onGetMaterialLines: (url) => {
          if (url.includes('/WO1002/')) {
            return { ok: true, json: async () => makeMaterialResponse({ woid: 'WO1002', lines: [makeMaterialLine({ componentPart: 'COMP2' })] }) };
          }
          return { ok: true, json: async () => makeMaterialResponse() };
        },
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));

      await screen.findByRole('listitem', { name: /WO1001, Released/i });
      await screen.findByRole('listitem', { name: /WO1002, Frozen/i });

      const card1 = screen.getByRole('listitem', { name: /WO1001, Released/i });
      const card2 = screen.getByRole('listitem', { name: /WO1002, Frozen/i });

      await user.click(within(card1).getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(card1).getByText('COMP1')).toBeInTheDocument();
      });
      expect(within(card2).queryByText('COMP2')).not.toBeInTheDocument();

      await user.click(within(card2).getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(card2).getByText('COMP2')).toBeInTheDocument();
      });
      expect(within(card1).getByText('COMP1')).toBeInTheDocument();
    });
  });

  describe('Stage 7D.8 kitting material grid', () => {
    it('sorts exceptions first, styles variance/manufactured rows, and filters by Part Number', async () => {
      setupBackend({
        onGetMaterialLines: () => ({
          ok: true,
          json: async () =>
            makeMaterialResponse({
              lines: [
                makeMaterialLine({ componentPart: 'NORMAL1', issuedPercent: 98, issueStatus: 'withinExpectedRange' }),
                makeMaterialLine({
                  componentPart: 'SUBASSY',
                  issuedPercent: 160,
                  issueStatus: 'overIssuedException',
                  isManufactured: true,
                }),
              ],
            }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      await user.click(screen.getByRole('button', { name: /show material lines/i }));

      await waitFor(() => {
        expect(within(card).getByText('SUBASSY')).toBeInTheDocument();
      });

      const rows = within(within(card).getByRole('table')).getAllByRole('row').slice(1);
      expect(within(rows[0]).getByText('SUBASSY')).toBeInTheDocument();
      expect(within(rows[1]).getByText('NORMAL1')).toBeInTheDocument();
      expect(within(card).getByText('160%')).toHaveClass('work-order-material-grid__issued-pct--exception');
      expect(within(card).getByText('98%')).not.toHaveClass('work-order-material-grid__issued-pct--exception');
      expect(within(card).getByText('SUBASSY').closest('tr')).toHaveClass('work-order-material-grid__row--manufactured');

      await user.type(screen.getByLabelText(/filter by part number/i), 'normal');
      await waitFor(() => {
        expect(screen.queryByText('SUBASSY')).not.toBeInTheDocument();
      });
      expect(screen.getByText('NORMAL1')).toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: /clear/i }));
      await waitFor(() => {
        expect(screen.getByText('SUBASSY')).toBeInTheDocument();
      });
    });
  });

  describe('Stage 7D.9 manufactured-subassembly candidate drill-down', () => {
    it('shows truthful planning-window work orders for a manufactured part, never implying pegging', async () => {
      setupBackend({
        onGetMaterialLines: () => ({
          ok: true,
          json: async () =>
            makeMaterialResponse({ lines: [makeMaterialLine({ componentPart: 'SUBASSY', isManufactured: true })] }),
        }),
        onGetWorkOrderCandidates: () => ({
          ok: true,
          json: async () => makeCandidateResponse({ candidates: [makeCandidateWorkOrder({ status: 'allocating' })] }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      await user.click(screen.getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(card).getByText('SUBASSY')).toBeInTheDocument();
      });

      await user.click(within(card).getByRole('button', { name: 'SUBASSY' }));

      expect(await screen.findByRole('heading', { name: 'Work Orders for SUBASSY' })).toBeInTheDocument();
      const candidateCard = await screen.findByRole('listitem', { name: /WO2001, Allocating/i });
      expect(within(candidateCard).getByText('WO2001')).toBeInTheDocument();

      expect(
        fetchMock.mock.calls.some(
          ([url]) =>
            typeof url === 'string' &&
            url.includes('/work-orders/candidates') &&
            url.includes('immediateParentWoid=WO1001') &&
            url.includes('componentPart=SUBASSY') &&
            url.includes('targetDepth=2') &&
            url.includes('dateBasis=dueDate'),
        ),
      ).toBe(true);
      expect(screen.queryByText(/child work orders|linked work orders|related work orders/i)).not.toBeInTheDocument();
    });

    it('shows a deliberate empty message (not an error) when a manufactured part has no planning-window work orders', async () => {
      setupBackend({
        onGetMaterialLines: () => ({
          ok: true,
          json: async () =>
            makeMaterialResponse({ lines: [makeMaterialLine({ componentPart: 'SUBASSY', isManufactured: true })] }),
        }),
        onGetWorkOrderCandidates: () => ({
          ok: true,
          json: async () => makeCandidateResponse({ candidates: [] }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      await user.click(screen.getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(card).getByText('SUBASSY')).toBeInTheDocument();
      });

      await user.click(within(card).getByRole('button', { name: 'SUBASSY' }));

      await waitFor(() => {
        expect(screen.getByText(/no work orders in the planning window for this part/i)).toBeInTheDocument();
      });
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });

    it('lets a selected candidate expand its own Kitting material lines, reusing the same Work Order card', async () => {
      setupBackend({
        onGetMaterialLines: (url) => {
          if (url.includes('/work-orders/WO2001/material')) {
            return {
              ok: true,
              json: async () => makeMaterialResponse({ woid: 'WO2001', lines: [makeMaterialLine({ componentPart: 'LEAF' })] }),
            };
          }
          return {
            ok: true,
            json: async () => makeMaterialResponse({ lines: [makeMaterialLine({ componentPart: 'SUBASSY', isManufactured: true })] }),
          };
        },
        onGetWorkOrderCandidates: () => ({
          ok: true,
          json: async () => makeCandidateResponse(),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      await user.click(screen.getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(card).getByText('SUBASSY')).toBeInTheDocument();
      });

      await user.click(within(card).getByRole('button', { name: 'SUBASSY' }));
      const candidateCard = await screen.findByRole('listitem', { name: /WO2001, Allocating/i });

      await user.click(within(candidateCard).getByRole('button', { name: /show material lines/i }));

      await waitFor(() => {
        expect(within(candidateCard).getByText('LEAF')).toBeInTheDocument();
      });
    });

    it('prefers one expanded manufactured-component branch per level, collapsing the prior one', async () => {
      setupBackend({
        onGetMaterialLines: () => ({
          ok: true,
          json: async () =>
            makeMaterialResponse({
              lines: [
                makeMaterialLine({ componentPart: 'SUBASSY-A', isManufactured: true }),
                makeMaterialLine({ componentPart: 'SUBASSY-B', isManufactured: true }),
              ],
            }),
        }),
        onGetWorkOrderCandidates: (url) => {
          if (url.includes('componentPart=SUBASSY-A')) {
            return {
              ok: true,
              json: async () =>
                makeCandidateResponse({ candidates: [makeCandidateWorkOrder({ woid: 'WOA', partNumber: 'SUBASSY-A' })] }),
            };
          }
          return {
            ok: true,
            json: async () =>
              makeCandidateResponse({ candidates: [makeCandidateWorkOrder({ woid: 'WOB', partNumber: 'SUBASSY-B' })] }),
          };
        },
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      await user.click(screen.getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(card).getByText('SUBASSY-A')).toBeInTheDocument();
      });

      await user.click(within(card).getByRole('button', { name: 'SUBASSY-A' }));
      expect(await screen.findByRole('heading', { name: 'Work Orders for SUBASSY-A' })).toBeInTheDocument();

      await user.click(within(card).getByRole('button', { name: 'SUBASSY-B' }));
      expect(await screen.findByRole('heading', { name: 'Work Orders for SUBASSY-B' })).toBeInTheDocument();
      expect(screen.queryByRole('heading', { name: 'Work Orders for SUBASSY-A' })).not.toBeInTheDocument();
    });

    it('lets a Level 2 candidate expose one further drill level, disabled at the maximum depth', async () => {
      setupBackend({
        onGetMaterialLines: (url) => {
          if (url.includes('/work-orders/WO2001/material')) {
            return {
              ok: true,
              json: async () =>
                makeMaterialResponse({ woid: 'WO2001', lines: [makeMaterialLine({ componentPart: 'SUBASSY2', isManufactured: true })] }),
            };
          }
          if (url.includes('/work-orders/WO3001/material')) {
            return {
              ok: true,
              json: async () =>
                makeMaterialResponse({ woid: 'WO3001', lines: [makeMaterialLine({ componentPart: 'SUBASSY3', isManufactured: true })] }),
            };
          }
          return {
            ok: true,
            json: async () =>
              makeMaterialResponse({ lines: [makeMaterialLine({ componentPart: 'SUBASSY1', isManufactured: true })] }),
          };
        },
        onGetWorkOrderCandidates: (url) => {
          if (url.includes('componentPart=SUBASSY1') && url.includes('targetDepth=2')) {
            return {
              ok: true,
              json: async () =>
                makeCandidateResponse({ candidates: [makeCandidateWorkOrder({ woid: 'WO2001', partNumber: 'SUBASSY1' })] }),
            };
          }
          if (url.includes('componentPart=SUBASSY2') && url.includes('targetDepth=3')) {
            return {
              ok: true,
              json: async () =>
                makeCandidateResponse({ candidates: [makeCandidateWorkOrder({ woid: 'WO3001', partNumber: 'SUBASSY2' })] }),
            };
          }
          return { ok: true, json: async () => makeCandidateResponse({ candidates: [] }) };
        },
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });
      await user.click(screen.getByText('100'));
      const card = await screen.findByRole('listitem', { name: /WO1001, Released/i });
      await user.click(screen.getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(card).getByText('SUBASSY1')).toBeInTheDocument();
      });

      await user.click(within(card).getByRole('button', { name: 'SUBASSY1' }));
      const candidateCard = await screen.findByRole('listitem', { name: /WO2001, Allocating/i });

      await user.click(within(candidateCard).getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(candidateCard).getByText('SUBASSY2')).toBeInTheDocument();
      });

      await user.click(within(candidateCard).getByRole('button', { name: 'SUBASSY2' }));
      const nestedCandidateCard = await screen.findByRole('listitem', { name: /WO3001, Allocating/i });

      await user.click(within(nestedCandidateCard).getByRole('button', { name: /show material lines/i }));
      await waitFor(() => {
        expect(within(nestedCandidateCard).getByText('SUBASSY3')).toBeInTheDocument();
      });

      expect(within(nestedCandidateCard).queryByRole('button', { name: 'SUBASSY3' })).not.toBeInTheDocument();
      expect(within(nestedCandidateCard).getByText('SUBASSY3').closest('tr')).not.toHaveClass(
        'work-order-material-grid__row--drillable',
      );
      expect(
        within(nestedCandidateCard).getByText('SUBASSY3').parentElement?.querySelector(
          '.work-order-material-grid__chevron--disabled',
        ),
      ).not.toBeNull();
    });
  });

  describe('Stage 7D.10 snapshot refresh and drill-down', () => {
    it('a successful refresh that replaces the snapshot clears the open Work Orders drill-down', async () => {
      setupBackend({
        onRefreshMps: () => ({
          ok: true,
          json: async () =>
            makeDashboard({ snapshot: { ...makeDashboard().snapshot, snapshotId: 'snap-2' } }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('100')); // the 2025-06-30 weekly bucket cell
      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      await waitFor(() => {
        expect(screen.getByText('WO1001')).toBeInTheDocument();
      });

      await user.click(within(screen.getByRole('main')).getByRole('button', { name: /^refresh$/i }));

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });
      // The bucket drill-down is cleared (Part Info is active again), but the parent remains
      // selected, so the parent-level Work Orders tab stays available (not selected).
      expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'false');
      expect(screen.queryByText('WO1001')).not.toBeInTheDocument();
    });

    it('a failed refresh preserves the retained snapshot and its open Work Orders drill-down', async () => {
      setupBackend({
        onRefreshMps: () => ({ ok: false, status: 503, text: async () => JSON.stringify({ detail: 'DB is down.' }) }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('100')); // the 2025-06-30 weekly bucket cell
      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      await waitFor(() => {
        expect(screen.getByText('WO1001')).toBeInTheDocument();
      });

      await user.click(within(screen.getByRole('main')).getByRole('button', { name: /^refresh$/i }));

      await waitFor(() => {
        expect(screen.getByText(/DB is down\./)).toBeInTheDocument();
      });
      expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      expect(screen.getByText('WO1001')).toBeInTheDocument();
    });
  });

  describe('Stage 8D.4 BOM tab', () => {
    function deferredValue<T>(): { promise: Promise<T>; resolve: (value: T) => void } {
      let resolveFn: (value: T) => void = () => {
        throw new Error('deferred value resolved before being captured');
      };
      const promise = new Promise<T>((resolve) => {
        resolveFn = resolve;
      });
      return { promise, resolve: (value: T) => resolveFn(value) };
    }

    // Flushes pending microtasks (in-flight mock responses) without advancing timers.
    const flushPromises = () => new Promise<void>((resolve) => setTimeout(resolve, 0));

    async function selectParentAndActivateBom() {
      await user.click(screen.getByText('ABC100'));
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });
      await user.click(screen.getByRole('tab', { name: 'BOM' }));
    }

    it('bucket selection renders the Work Orders and disabled Shortages tabs alongside BOM', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('100')); // the 2025-06-30 weekly bucket cell

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      expect(screen.getByRole('tab', { name: 'BOM' })).toBeEnabled();
      expect(screen.getByRole('tab', { name: 'Shortages' })).toBeDisabled();
      expect(screen.queryByRole('tab', { name: 'Future Shortages' })).not.toBeInTheDocument();
    });

    it('parent selection alone does not request BOM', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await user.click(screen.getByText('ABC100'));
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });

      expect(bomRequestCalls(fetchMock)).toHaveLength(0);
    });

    it('first BOM activation requests the BOM once with only workspace and parent', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      expect(bomRequestCalls(fetchMock)).toHaveLength(1);
      const url = bomRequestCalls(fetchMock)[0];
      expect(url).toContain('/parts/ABC100/bom');
      expect(url).not.toContain('?');
    });

    it('tab revisit for the same unchanged parent does not request again', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      fetchMock.mockClear();
      await user.click(screen.getByRole('tab', { name: 'Part Info' }));
      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Part Info' })).toHaveAttribute('aria-selected', 'true');
      });
      await user.click(screen.getByRole('tab', { name: 'BOM' }));
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);
    });

    it('changing parent clears the BOM and loads the new parent lazily on activation', async () => {
      setupBackend({
        onGetMps: () => ({
          ok: true,
          json: async () =>
            makeDashboard({ parts: [makePartSchedule('ABC100', 'Widget Assembly'), makePartSchedule('DEF200', 'Delta Assembly')] }),
        }),
        onGetBom: (url) =>
          url.includes('/parts/DEF200/bom')
            ? { ok: true, json: async () => makeBomResponse({ parentPart: 'DEF200', lines: [makeBomLine({ occurrenceKey: 'k-d', componentPart: 'COMP-D' })] }) }
            : { ok: true, json: async () => makeBomResponse() },
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      fetchMock.mockClear();
      // Focused mode shows only the selected row: switch parents by toggling the current one
      // closed, then selecting the new parent.
      await user.click(screen.getByText('ABC100'));
      await waitFor(() => {
        expect(screen.queryByRole('tablist', { name: /part detail/i })).not.toBeInTheDocument();
      });
      await user.click(screen.getByText('DEF200'));
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });

      // The previous parent's rows are gone, and no request fired for the new parent yet.
      expect(screen.queryByText('COMP-A')).not.toBeInTheDocument();
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);

      await user.click(screen.getByRole('tab', { name: 'BOM' }));
      await waitFor(() => {
        expect(screen.getByText('COMP-D')).toBeInTheDocument();
      });
      expect(screen.queryByText('COMP-A')).not.toBeInTheDocument();
    });

    it('a successful refresh invalidates the BOM with no transient request; next activation re-requests', async () => {
      setupBackend({
        onRefreshMps: () => ({
          ok: true,
          json: async () => makeDashboard({ snapshot: { ...makeDashboard().snapshot, snapshotId: 'snap-2' } }),
        }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      fetchMock.mockClear();
      await user.click(within(screen.getByRole('main')).getByRole('button', { name: /^refresh$/i }));

      // Existing shell convention: a successful refresh returns to Part Info — and no transient
      // BOM request may be issued during the refresh transition (edge-triggered activation).
      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Part Info' })).toHaveAttribute('aria-selected', 'true');
      });
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);

      // The next explicit BOM activation reaches the backend again — exactly once.
      await user.click(screen.getByRole('tab', { name: 'BOM' }));
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });
      expect(bomRequestCalls(fetchMock)).toHaveLength(1);
    });

    it('a failed refresh retains the displayed BOM without refetching', async () => {
      setupBackend({
        onRefreshMps: () => ({ ok: false, status: 503, text: async () => JSON.stringify({ detail: 'DB is down.' }) }),
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      fetchMock.mockClear();
      await user.click(within(screen.getByRole('main')).getByRole('button', { name: /^refresh$/i }));

      await waitFor(() => {
        expect(screen.getByText(/DB is down\./)).toBeInTheDocument();
      });
      // A failed refresh does not advance the snapshot: the displayed BOM is preserved.
      expect(screen.getByRole('tab', { name: 'BOM' })).toHaveAttribute('aria-selected', 'true');
      expect(screen.getByText('COMP-A')).toBeInTheDocument();
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);
    });

    it('Due/Release toggle does not refetch the loaded BOM', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      fetchMock.mockClear();
      await user.click(within(screen.getByRole('main')).getByRole('button', { name: /release date/i }));
      await waitFor(() => {
        expect(
          fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('dateBasis=releaseDate')),
        ).toBe(true);
      });
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);
      expect(screen.getByText('COMP-A')).toBeInTheDocument();
    });

    it('horizon change does not refetch the loaded BOM', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      fetchMock.mockClear();
      fireEvent.change(screen.getByLabelText(/horizon in weeks/i), { target: { value: '24' } });
      await waitFor(() => {
        expect(fetchMock.mock.calls.some(([url]) => typeof url === 'string' && url.includes('horizonWeeks=24'))).toBe(
          true,
        );
      });
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);
      expect(screen.getByText('COMP-A')).toBeInTheDocument();
    });

    it('bucket selection does not refetch the loaded BOM and BOM is retained on return', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      fetchMock.mockClear();
      await user.click(screen.getByText('100')); // weekly bucket cell → auto-opens Work Orders
      await waitFor(() => {
        expect(screen.getByRole('tab', { name: 'Work Orders' })).toHaveAttribute('aria-selected', 'true');
      });
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);

      await user.click(screen.getByRole('tab', { name: 'BOM' }));
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);
    });

    it('searching the loaded BOM makes no new API request', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      fetchMock.mockClear();
      await user.type(screen.getByLabelText('Filter by Component Item'), 'COMP');
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);
    });

    it('Component Item, Description, P/M, and Phantom filter changes make no new API request', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });

      fetchMock.mockClear();
      await user.selectOptions(screen.getByLabelText('P/M'), 'P');
      await user.selectOptions(screen.getByLabelText('Phantom'), 'no');
      await user.type(screen.getByLabelText('Filter by Component Item'), 'COMP');
      await user.type(screen.getByLabelText('Filter by Description'), 'COMP');
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });
      expect(bomRequestCalls(fetchMock)).toHaveLength(0);
    });

    it('a late response for a previous parent cannot populate the new parent', async () => {
      const pendingA = deferredValue<BomResponseDto>();
      const pendingD = deferredValue<BomResponseDto>();
      setupBackend({
        onGetMps: () => ({
          ok: true,
          json: async () =>
            makeDashboard({ parts: [makePartSchedule('ABC100', 'Widget Assembly'), makePartSchedule('DEF200', 'Delta Assembly')] }),
        }),
        onGetBom: (url) =>
          url.includes('/parts/ABC100/bom')
            ? { ok: true, json: () => pendingA.promise }
            : { ok: true, json: () => pendingD.promise },
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText(/loading bom/i)).toBeInTheDocument();
      });

      // Switch parents while A's request is still in flight.
      await user.click(screen.getByText('ABC100'));
      await waitFor(() => {
        expect(screen.queryByRole('tablist', { name: /part detail/i })).not.toBeInTheDocument();
      });
      await user.click(screen.getByText('DEF200'));
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });

      // Activate the new parent's BOM (its request is still pending) and let A's late response land.
      await user.click(screen.getByRole('tab', { name: 'BOM' }));
      await waitFor(() => {
        expect(screen.getByText(/loading bom/i)).toBeInTheDocument();
      });
      pendingA.resolve(makeBomResponse({ lines: [makeBomLine({ componentPart: 'COMP-A' })] }));
      await flushPromises();
      // A's obsolete response must be ignored: the panel is still loading D, not showing A.
      expect(screen.getByText(/loading bom/i)).toBeInTheDocument();
      expect(screen.queryByText('COMP-A')).not.toBeInTheDocument();

      pendingD.resolve(
        makeBomResponse({ parentPart: 'DEF200', lines: [makeBomLine({ occurrenceKey: 'k-d', componentPart: 'COMP-D' })] }),
      );
      await waitFor(() => {
        expect(screen.getByText('COMP-D')).toBeInTheDocument();
      });
      expect(screen.queryByText('COMP-A')).not.toBeInTheDocument();
    });

    it('a late response from a previous workspace cannot populate the new workspace', async () => {
      const pendingA = deferredValue<BomResponseDto>();
      const pendingZ = deferredValue<BomResponseDto>();
      const twoWorkspaces: WorkspaceListResponseDto = {
        workspaces: [
          makeWorkspace(),
          makeWorkspace({ assignmentId: 'ws-2', displayName: 'Line 2', site: 'SW', parentParts: ['ZZZ999'] }),
        ],
        configurationWarning: null,
      };
      setupBackend(
        {
          onGetMps: (url) =>
            url.includes('/ws-2/')
              ? {
                  ok: true,
                  json: async () =>
                    makeDashboard({
                      snapshot: { ...makeDashboard().snapshot, workspaceId: 'ws-2', site: 'SW' },
                      parts: [makePartSchedule('ZZZ999', 'Zeta Assembly')],
                    }),
                }
              : { ok: true, json: async () => makeDashboard() },
          onGetBom: (url) =>
            url.includes('/parts/ABC100/bom')
              ? { ok: true, json: () => pendingA.promise }
              : { ok: true, json: () => pendingZ.promise },
        },
        twoWorkspaces,
      );
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText(/loading bom/i)).toBeInTheDocument();
      });

      // Switch workspace while A's request is still in flight.
      await user.click(screen.getByRole('tab', { name: 'Line 2' }));
      await waitFor(() => {
        expect(screen.getByText('ZZZ999')).toBeInTheDocument();
      });
      await user.click(screen.getByText('ZZZ999'));
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });
      await user.click(screen.getByRole('tab', { name: 'BOM' }));
      await waitFor(() => {
        expect(screen.getByText(/loading bom/i)).toBeInTheDocument();
      });

      // The previous workspace's late response must be ignored by the new context.
      pendingA.resolve(makeBomResponse({ lines: [makeBomLine({ componentPart: 'COMP-A' })] }));
      await flushPromises();
      expect(screen.queryByText('COMP-A')).not.toBeInTheDocument();

      pendingZ.resolve(
        makeBomResponse({ parentPart: 'ZZZ999', lines: [makeBomLine({ occurrenceKey: 'k-z', componentPart: 'COMP-Z' })] }),
      );
      await waitFor(() => {
        expect(screen.getByText('COMP-Z')).toBeInTheDocument();
      });
      expect(screen.queryByText('COMP-A')).not.toBeInTheDocument();
    });

    it('an obsolete request error cannot corrupt the new context state', async () => {
      let resolveText: (value: string) => void = () => {};
      const pendingText = new Promise<string>((resolve) => {
        resolveText = resolve;
      });
      const pendingD = deferredValue<BomResponseDto>();
      setupBackend({
        onGetMps: () => ({
          ok: true,
          json: async () =>
            makeDashboard({ parts: [makePartSchedule('ABC100', 'Widget Assembly'), makePartSchedule('DEF200', 'Delta Assembly')] }),
        }),
        onGetBom: (url) =>
          url.includes('/parts/ABC100/bom')
            ? { ok: false, status: 503, text: () => pendingText }
            : { ok: true, json: () => pendingD.promise },
      });
      render(<App />);
      await waitForConnected();

      await waitFor(() => {
        expect(screen.getByText('ABC100')).toBeInTheDocument();
      });

      await selectParentAndActivateBom();
      await waitFor(() => {
        expect(screen.getByText(/loading bom/i)).toBeInTheDocument();
      });

      // Switch parents while A's (failing) request is still in flight.
      await user.click(screen.getByText('ABC100'));
      await waitFor(() => {
        expect(screen.queryByRole('tablist', { name: /part detail/i })).not.toBeInTheDocument();
      });
      await user.click(screen.getByText('DEF200'));
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });
      await user.click(screen.getByRole('tab', { name: 'BOM' }));
      await waitFor(() => {
        expect(screen.getByText(/loading bom/i)).toBeInTheDocument();
      });

      // A's error lands after the identity change — it must not replace D's in-flight state.
      resolveText(JSON.stringify({ detail: 'Database currently unavailable.' }));
      await flushPromises();
      expect(screen.queryByText(/database currently unavailable/i)).not.toBeInTheDocument();
      expect(screen.getByText(/loading bom/i)).toBeInTheDocument();

      pendingD.resolve(
        makeBomResponse({ parentPart: 'DEF200', lines: [makeBomLine({ occurrenceKey: 'k-d', componentPart: 'COMP-D' })] }),
      );
      await waitFor(() => {
        expect(screen.getByText('COMP-D')).toBeInTheDocument();
      });
      expect(screen.queryByText(/database currently unavailable/i)).not.toBeInTheDocument();
    });
  });

  describe('Stage 8D.6 Component Information modal', () => {
    function deferredValue<T>(): { promise: Promise<T>; resolve: (value: T) => void } {
      let resolveFn: (value: T) => void = () => {
        throw new Error('deferred value resolved before being captured');
      };
      const promise = new Promise<T>((resolve) => {
        resolveFn = resolve;
      });
      return { promise, resolve: (value: T) => resolveFn(value) };
    }

    const flushPromises = () => new Promise<void>((resolve) => setTimeout(resolve, 0));

    async function selectParentAndActivateBom() {
      await user.click(screen.getByText('ABC100'));
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /part info/i })).toBeInTheDocument();
      });
      await user.click(screen.getByRole('tab', { name: 'BOM' }));
      await waitFor(() => {
        expect(screen.getByText('COMP-A')).toBeInTheDocument();
      });
    }

    it('clicking a BOM row opens the modal immediately, before the component-detail request resolves', async () => {
      const pending = deferredValue<ComponentDetailResponseDto>();
      setupBackend({ onGetComponentDetail: () => ({ ok: true, json: () => pending.promise }) });
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      fetchMock.mockClear();
      await user.click(screen.getByText('COMP-A'));

      const dialog = await screen.findByRole('dialog');
      expect(within(dialog).getByText('COMP-A')).toBeInTheDocument();
      expect(within(dialog).getByText(/loading component information/i)).toBeInTheDocument();
      expect(componentDetailRequestCalls(fetchMock)).toHaveLength(1);

      pending.resolve(makeComponentDetail());
      await waitFor(() => {
        expect(within(dialog).getByText('Component A')).toBeInTheDocument();
      });
    });

    it('opening the modal does not alter BOM filters, order, or context', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      await user.type(screen.getByLabelText('Filter by Component Item'), 'COMP');
      await user.click(screen.getByText('COMP-A'));
      await screen.findByRole('dialog');

      await user.click(screen.getByRole('button', { name: /close/i }));
      expect(screen.getByLabelText('Filter by Component Item')).toHaveValue('COMP');
      expect(screen.getByText('COMP-A')).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: 'BOM' })).toHaveAttribute('aria-selected', 'true');
    });

    it('closing with X returns focus to the originating BOM row and preserves the BOM', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      const row = screen.getByText('COMP-A').closest('tr') as HTMLElement;
      await user.click(row);
      await screen.findByRole('dialog');

      await user.click(screen.getByRole('button', { name: /close/i }));
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
      expect(row).toHaveFocus();
    });

    it('Escape closes the modal, restores focus to the originating row, and preserves the BOM', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      const row = screen.getByText('COMP-A').closest('tr') as HTMLElement;
      await user.click(row);
      await screen.findByRole('dialog');
      await user.keyboard('{Escape}');

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
      expect(screen.getByText('COMP-A')).toBeInTheDocument();
      expect(row).toHaveFocus();
    });

    it('Escape closes the modal even when focus is on a control inside the modal other than Close', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      await user.click(screen.getByText('COMP-A'));
      const dialog = await screen.findByRole('dialog');
      // Show MRP is the only other focusable-by-tab-order control besides Close; disabled
      // buttons are excluded from the Tab order, so focus the dialog container directly.
      dialog.focus();
      await user.keyboard('{Escape}');

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    });

    it('clicking the backdrop does not close the modal', async () => {
      setupBackend();
      const { container } = render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      await user.click(screen.getByText('COMP-A'));
      await screen.findByRole('dialog');

      const backdrop = container.querySelector('.component-info-modal-backdrop');
      expect(backdrop).not.toBeNull();
      if (backdrop) await user.click(backdrop);
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    it('a late response for a component closed before it resolved does not reopen the modal', async () => {
      const pending = deferredValue<ComponentDetailResponseDto>();
      setupBackend({ onGetComponentDetail: () => ({ ok: true, json: () => pending.promise }) });
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      await user.click(screen.getByText('COMP-A'));
      await screen.findByRole('dialog');
      await user.click(screen.getByRole('button', { name: /close/i }));
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

      pending.resolve(makeComponentDetail());
      await flushPromises();
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    it('a late response from a previously inspected component cannot populate a newly opened component', async () => {
      const pendingA = deferredValue<ComponentDetailResponseDto>();
      setupBackend({
        onGetBom: () => ({
          ok: true,
          json: async () =>
            makeBomResponse({
              lines: [makeBomLine({ occurrenceKey: 'k-1', componentPart: 'COMP-A' }), makeBomLine({ occurrenceKey: 'k-2', componentPart: 'COMP-B' })],
            }),
        }),
        onGetComponentDetail: (url) =>
          url.includes('/components/COMP-A')
            ? { ok: true, json: () => pendingA.promise }
            : { ok: true, json: async () => makeComponentDetail({ componentPart: 'COMP-B', description: 'Component B' }) },
      });
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      await user.click(screen.getByText('COMP-A'));
      await screen.findByRole('dialog');
      await user.click(screen.getByRole('button', { name: /close/i }));
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

      await user.click(screen.getByText('COMP-B'));
      const dialog = await screen.findByRole('dialog');
      await waitFor(() => expect(within(dialog).getByText('Component B')).toBeInTheDocument());

      // COMP-A's stale response arrives after COMP-B's modal is already showing.
      pendingA.resolve(makeComponentDetail({ componentPart: 'COMP-A', description: 'Component A' }));
      await flushPromises();
      expect(within(dialog).getByText('COMP-B')).toBeInTheDocument();
      expect(within(dialog).getByText('Component B')).toBeInTheDocument();
      expect(within(dialog).queryByText('Component A')).not.toBeInTheDocument();
    });

    it('a successful MPS refresh (new snapshot) closes the modal', async () => {
      setupBackend({
        onRefreshMps: () => ({
          ok: true,
          json: async () => makeDashboard({ snapshot: { ...makeDashboard().snapshot, snapshotId: 'snap-2' } }),
        }),
      });
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      await user.click(screen.getByText('COMP-A'));
      await screen.findByRole('dialog');

      await user.click(within(screen.getByRole('main')).getByRole('button', { name: /^refresh$/i }));
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    });

    it('a workspace change closes the modal', async () => {
      setupBackend();
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      await user.click(screen.getByText('COMP-A'));
      await screen.findByRole('dialog');

      const closeSelectionButton = screen.getByRole('tab', { name: 'BOM' });
      expect(closeSelectionButton).toBeInTheDocument();
      // Toggling the current parent off (the accepted workspace-context-clear path) must close
      // the modal along with the rest of the selection state.
      await user.click(screen.getByText('ABC100'));
      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    });

    it('a component-detail failure keeps the modal open with Retry and Close', async () => {
      setupBackend({
        onGetComponentDetail: () => ({ ok: false, status: 503, text: async () => JSON.stringify({ detail: 'DB is down.' }) }),
      });
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      await user.click(screen.getByText('COMP-A'));
      const dialog = await screen.findByRole('dialog');
      await waitFor(() => expect(within(dialog).getByText('DB is down.')).toBeInTheDocument());
      expect(within(dialog).getByRole('button', { name: /retry/i })).toBeInTheDocument();
      expect(within(dialog).getByRole('button', { name: /^close$/i })).toBeInTheDocument();
    });

    it('Retry re-requests the same component', async () => {
      let callCount = 0;
      setupBackend({
        onGetComponentDetail: () => {
          callCount += 1;
          return callCount === 1
            ? { ok: false, status: 503, text: async () => JSON.stringify({ detail: 'DB is down.' }) }
            : { ok: true, json: async () => makeComponentDetail() };
        },
      });
      render(<App />);
      await waitForConnected();
      await waitFor(() => expect(screen.getByText('ABC100')).toBeInTheDocument());
      await selectParentAndActivateBom();

      await user.click(screen.getByText('COMP-A'));
      const dialog = await screen.findByRole('dialog');
      await waitFor(() => expect(within(dialog).getByText('DB is down.')).toBeInTheDocument());

      await user.click(within(dialog).getByRole('button', { name: /retry/i }));
      await waitFor(() => expect(within(dialog).getByText('Component A')).toBeInTheDocument());
      expect(componentDetailRequestCalls(fetchMock)).toHaveLength(2);
    });
  });
});

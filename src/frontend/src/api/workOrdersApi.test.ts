import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from './client';
import {
  fetchPlanningWindow,
  fetchWorkOrderCandidates,
  fetchWorkOrderImmediateMaterial,
  fetchWorkOrderImmediateMaterialSummary,
  toWorkOrdersApiError,
} from './workOrdersApi';

describe('fetchPlanningWindow', () => {
  const defaultBackendUrl = window.__KST_BACKEND_URL__;

  afterEach(() => {
    window.__KST_BACKEND_URL__ = defaultBackendUrl;
    vi.unstubAllGlobals();
  });

  it('uses the dynamic Tauri sidecar base URL instead of a relative frontend URL', async () => {
    window.__KST_BACKEND_URL__ = 'http://127.0.0.1:45678';
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ snapshotId: 'snapshot-1', workOrders: [] }),
    });
    vi.stubGlobal('fetch', fetchMock);

    await fetchPlanningWindow('workspace-1', {
      snapshotId: 'snapshot-1',
      parentPart: 'PARENT-1',
      dateBasis: 'releaseDate',
      bucketKind: 'weekly',
      weekLabel: '2026-09-07',
    });

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:45678/api/v1/workspaces/workspace-1/work-orders/planning-window?snapshotId=snapshot-1&parentPart=PARENT-1&dateBasis=releaseDate&bucketKind=weekly&weekLabel=2026-09-07',
      expect.objectContaining({
        method: 'GET',
        headers: { Accept: 'application/json' },
      }),
    );
  });
});

describe('fetchWorkOrderCandidates', () => {
  const defaultBackendUrl = window.__KST_BACKEND_URL__;

  afterEach(() => {
    window.__KST_BACKEND_URL__ = defaultBackendUrl;
    vi.unstubAllGlobals();
  });

  it('passes the active date basis to the authorized manufactured-part planning-window request', async () => {
    window.__KST_BACKEND_URL__ = 'http://127.0.0.1:45678';
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ snapshotId: 'snapshot-1', candidates: [] }),
    });
    vi.stubGlobal('fetch', fetchMock);

    await fetchWorkOrderCandidates('workspace-1', 'snapshot-1', 'WO-1', 'SUBASSY-1', 2, 'releaseDate');

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:45678/api/v1/workspaces/workspace-1/work-orders/candidates?snapshotId=snapshot-1&immediateParentWoid=WO-1&componentPart=SUBASSY-1&targetDepth=2&dateBasis=releaseDate',
      expect.objectContaining({ method: 'GET', headers: { Accept: 'application/json' } }),
    );
  });
});

describe('fetchWorkOrderImmediateMaterial', () => {
  const defaultBackendUrl = window.__KST_BACKEND_URL__;

  afterEach(() => {
    window.__KST_BACKEND_URL__ = defaultBackendUrl;
    vi.unstubAllGlobals();
  });

  it('requests one encoded work order with its snapshot and date basis', async () => {
    window.__KST_BACKEND_URL__ = 'http://127.0.0.1:45678';
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ snapshotId: 'snapshot-1', workOrder: {}, components: [], diagnostic: null }),
    });
    vi.stubGlobal('fetch', fetchMock);

    await fetchWorkOrderImmediateMaterial('workspace-1', 'snapshot-1', 'WO/1', 'releaseDate');

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:45678/api/v1/workspaces/workspace-1/work-orders/WO%2F1/immediate-material?snapshotId=snapshot-1&dateBasis=releaseDate',
      expect.objectContaining({ method: 'GET', headers: { Accept: 'application/json' } }),
    );
  });
});

describe('fetchWorkOrderImmediateMaterialSummary', () => {
  const defaultBackendUrl = window.__KST_BACKEND_URL__;

  afterEach(() => {
    window.__KST_BACKEND_URL__ = defaultBackendUrl;
    vi.unstubAllGlobals();
  });

  it('supports workspace-wide and parent-scoped summary requests', async () => {
    window.__KST_BACKEND_URL__ = 'http://127.0.0.1:45678';
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ snapshotId: 'snapshot-1', workOrders: [] }),
    });
    vi.stubGlobal('fetch', fetchMock);

    await fetchWorkOrderImmediateMaterialSummary('workspace-1', 'snapshot-1', null, 'dueDate');
    await fetchWorkOrderImmediateMaterialSummary('workspace-1', 'snapshot-1', 'PARENT/1', 'releaseDate');

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      'http://127.0.0.1:45678/api/v1/workspaces/workspace-1/work-orders/immediate-material-summary?snapshotId=snapshot-1&dateBasis=dueDate',
      expect.objectContaining({ method: 'GET', headers: { Accept: 'application/json' } }),
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      'http://127.0.0.1:45678/api/v1/workspaces/workspace-1/work-orders/immediate-material-summary?snapshotId=snapshot-1&parentPart=PARENT%2F1&dateBasis=releaseDate',
      expect.objectContaining({ method: 'GET', headers: { Accept: 'application/json' } }),
    );
  });
});

describe('toWorkOrdersApiError', () => {
  it('shows the API Problem Details diagnostic rather than raw JSON', () => {
    const result = toWorkOrdersApiError(new ApiError(503, 'http://localhost', JSON.stringify({ detail: 'Database currently unavailable.' })));

    expect(result).toEqual({ type: 'error', detail: 'Database currently unavailable.' });
  });
});

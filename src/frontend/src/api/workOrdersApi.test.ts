import { afterEach, describe, expect, it, vi } from 'vitest';
import { fetchPlanningWindow, fetchWorkOrderCandidates } from './workOrdersApi';

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

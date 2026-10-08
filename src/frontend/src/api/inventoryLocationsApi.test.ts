import { describe, it, expect, vi, afterEach } from 'vitest';
import { fetchInventoryLocations } from './inventoryLocationsApi';
vi.mock('./tauri-bridge', () => ({ resolveBackendBaseUrl: async () => 'http://127.0.0.1:1234' }));
afterEach(() => vi.unstubAllGlobals());
const valid = { workspaceId: 'ws', site: 'SW', componentPart: 'comp', mpsSnapshotId: 'snap',
  acquiredAtUtc: '2026-10-08T12:00:00Z', isStale: false, warning: null,
  rows: [{ rowId: '0', location: null, lotSerial: null, status: null, quantityOnHand: '999999999999999999.0000000001' }] };
describe('Inventory locations API boundary', () => {
  it('encodes component and snapshot and passes cancellation without numeric coercion', async () => {
    const fetch = vi.fn().mockResolvedValue({ ok: true, json: async () => valid });
    vi.stubGlobal('fetch', fetch);
    const controller = new AbortController();
    const result = await fetchInventoryLocations('ws', 'COMP', 'snap', controller.signal);
    expect(result.rows[0].quantityOnHand).toBe(valid.rows[0].quantityOnHand);
    expect(fetch).toHaveBeenCalledWith('http://127.0.0.1:1234/api/v1/workspaces/ws/components/COMP/inventory-locations?mpsSnapshotId=snap',
      expect.objectContaining({ signal: controller.signal }));
  });
  it.each([
    { ...valid, workspaceId: 'other' }, { ...valid, componentPart: 'other' }, { ...valid, mpsSnapshotId: 'other' },
    { ...valid, rows: [{ ...valid.rows[0], quantityOnHand: 1.25 }] },
  ])('rejects mismatched identities and JSON-number quantities', async response => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => response }));
    await expect(fetchInventoryLocations('ws', 'COMP', 'snap', new AbortController().signal)).rejects.toThrow();
  });
});

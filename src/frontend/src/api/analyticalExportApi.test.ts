import { describe, expect, it, vi } from 'vitest';
import { AnalyticalExportClient } from './analyticalExportApi';

const host = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => host);
describe('Analytical client pre-dispatch cancellation', () => {
  it('returns known cancellation if departure happens during the import boundary without invoking save', async () => {
    const client = new AnalyticalExportClient();
    const save = client.save({ preparationId: 'p', fileName: 'MPS.xlsx', expiresAtUtc: '' }, false, 'never-dispatched');
    await client.release();
    expect(await save).toMatchObject({ outcome: 'cancelled', operationComplete: true });
    expect(host.invoke).not.toHaveBeenCalledWith('component_mrp_save', expect.anything());
  });
});

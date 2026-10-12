import { beforeEach, expect, it, vi } from 'vitest';
import { generateSop } from './sopApi';
const host = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('./tauri-bridge', () => ({ isRunningInTauri: () => true }));
const acquisition = { outcome: 'acquired', acquisitionId: 'id', weekLabels: ['2026-10-12'], asOfDate: '2026-10-12', acquiredAtUtc: '2026-10-12T12:00:00Z', expiresAtUtc: '2026-10-12T12:30:00Z' };
const request = { site: 'SW', groupBy: 'planr', ranges: [{ from: '2140', to: '2142' }] };
beforeEach(() => { localStorage.clear(); host.invoke.mockReset(); });
it('one Generate internally acquires, maps actual calendar, prepares and releases acquisition', async () => {
  host.invoke.mockImplementation(async (command, args) => command === 'component_mrp_begin' ? 7 : command === 'component_mrp_generate' ? args.sopPrepare ? { outcome: 'prepared', preparationId: 'bytes' } : acquisition : undefined);
  await generateSop(request);
  const stages = host.invoke.mock.calls.filter(c => c[0] === 'component_mrp_generate');
  expect(stages).toHaveLength(2); expect(stages[0][1].sopRequest).toEqual(request);
  expect(stages[1][1].sopPrepare.presentation.periods[0]).toMatchObject({ fiscalYear: 2027, period: 4, quarter: 2, weekCount: 4 });
  expect(host.invoke).toHaveBeenCalledWith('sop_discard_acquisition', { acquisitionId: 'id' });
});
it('abort after acquire never prepares and cleanup is unconditional', async () => {
  const controller = new AbortController();
  host.invoke.mockImplementation(async command => command === 'component_mrp_begin' ? 7 : command === 'component_mrp_generate' ? (controller.abort(), acquisition) : undefined);
  await expect(generateSop(request, controller.signal)).rejects.toThrow();
  expect(host.invoke.mock.calls.filter(c => c[0] === 'component_mrp_generate')).toHaveLength(1);
  expect(host.invoke).toHaveBeenCalledWith('sop_discard_acquisition', { acquisitionId: 'id' });
});

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { generateComponentMrp, readComponentMrp } from './componentMrpApi';
import { deferred, reportFixture } from '../reports/reportTestSupport';
import { mrpFixture } from '../reports/partInfoMrpTestSupport';
const host = vi.hoisted(() => ({ invoke: vi.fn(), desktop: true }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('./tauri-bridge', () => ({ isRunningInTauri: () => host.desktop, resolveBackendBaseUrl: async () => 'http://127.0.0.1:12345' }));
const request = { site: 'SW', part: 'ABC', horizonWeeks: 26, period: 'Week', includeUnconfirmed: true };
beforeEach(() => { host.desktop = true; host.invoke.mockReset(); });
afterEach(() => vi.unstubAllGlobals());
describe('Component MRP fixed native and browser transport — offline', () => {
  it('serializes native close leases, cancels issuing epoch, skips obsolete queued controls and sends the newest state', async () => {
    const held = deferred<ReturnType<typeof mrpFixture>>(); let epoch = 0;
    host.invoke.mockImplementation(async (name, args) => name === 'component_mrp_begin' ? ++epoch : name === 'component_mrp_generate' ? args.epoch === 1 ? held.promise : mrpFixture(args) : undefined);
    const a = new AbortController(); const b = new AbortController();
    const first = readComponentMrp(request, a.signal).catch(e => e);
    await vi.waitFor(() => expect(host.invoke).toHaveBeenCalledWith('component_mrp_generate', { epoch: 1, ...request, screenRead: true }));
    a.abort(); const middle = readComponentMrp({ ...request, period: 'Day' }, b.signal).catch(e => e); b.abort();
    const latest = readComponentMrp({ ...request, period: 'Month', includeUnconfirmed: false });
    expect(host.invoke.mock.calls.filter(([name]) => name === 'component_mrp_begin')).toHaveLength(1);
    held.resolve(mrpFixture()); await first; await middle;
    expect(await latest).toMatchObject({ period: 'Month', includeUnconfirmed: false });
    expect(host.invoke).toHaveBeenCalledWith('sop_discard_generation', { epoch: 1 });
    expect(host.invoke.mock.calls.filter(([name]) => name === 'component_mrp_begin')).toHaveLength(2);
    expect(host.invoke.mock.calls.filter(([name]) => name === 'component_mrp_generate')).toHaveLength(2);
  });
  it('forwards acquisition identity/as-of to native preparation without exposing session authority', async () => {
    host.invoke.mockResolvedValue(reportFixture);
    const bound = { ...request, acquisitionId: '11111111-1111-4111-8111-111111111111', asOfDate: '2026-09-29' };
    await generateComponentMrp(bound, undefined, 42);
    expect(host.invoke).toHaveBeenCalledExactlyOnceWith('component_mrp_generate', { epoch: 42, ...bound });
    expect(JSON.stringify(host.invoke.mock.calls)).not.toContain('Session');
  });
  it('a control read waits for a cancelled preparation close lease instead of losing the newest state to native busy', async () => {
    const held = deferred<typeof reportFixture>(); const cancel = new AbortController();
    host.invoke.mockImplementation(async (name, args) => name === 'component_mrp_begin' ? 9 : name === 'component_mrp_generate' ? args.screenRead ? mrpFixture(args) : held.promise : undefined);
    const preparation = generateComponentMrp(request, cancel.signal, 7).catch(e => e);
    await vi.waitFor(() => expect(host.invoke).toHaveBeenCalledWith('component_mrp_generate', { epoch: 7, ...request }));
    cancel.abort(); const newest = readComponentMrp({ ...request, period: 'Month' });
    expect(host.invoke.mock.calls.some(([name]) => name === 'component_mrp_begin')).toBe(false);
    held.resolve(reportFixture); await preparation; expect(await newest).toMatchObject({ period: 'Month' });
    expect(host.invoke).toHaveBeenCalledWith('sop_discard_generation', { epoch: 7 });
  });
  it('browser read and screen prepare share one owner; expired preparation does not reacquire', async () => {
    host.desktop = false;
    const fetch = vi.fn().mockResolvedValueOnce({ ok: true, json: async () => mrpFixture() })
      .mockResolvedValueOnce({ ok: false, json: async () => ({ detail: 'Refresh MRP before exporting.' }) });
    vi.stubGlobal('fetch', fetch);
    const screen = await readComponentMrp(request);
    await expect(generateComponentMrp({ ...request, acquisitionId: screen.acquisitionId, asOfDate: screen.asOfDate })).rejects.toThrow('Refresh MRP');
    expect(fetch).toHaveBeenCalledTimes(2);
    expect(fetch.mock.calls[0][0]).toBe('http://127.0.0.1:12345/api/v1/reports/component-mrp/projection');
    expect(fetch.mock.calls[1][0]).toBe('http://127.0.0.1:12345/api/v1/reports/component-mrp/screen-preparations');
    expect(fetch.mock.calls[0][1].headers['X-KST-Report-Session']).toBe(fetch.mock.calls[1][1].headers['X-KST-Report-Session']);
  });
});

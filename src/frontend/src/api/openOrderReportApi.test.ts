import { afterEach, describe, expect, it, vi } from 'vitest';
import { generateOpenOrderReport } from './openOrderReportApi';
import { initialOor } from '../reports/openOrderReportPreferences';

const host = vi.hoisted(() => ({ invoke: vi.fn(), desktop: false }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('./tauri-bridge', () => ({ isRunningInTauri: () => host.desktop, resolveBackendBaseUrl: async () => 'http://127.0.0.1:1234' }));
describe('Open Order transport', () => {
  afterEach(() => { vi.unstubAllGlobals(); host.desktop = false; host.invoke.mockReset(); });
  it('uses fixed typed endpoint and returns backend input validation', async () => {
    const fetch = vi.fn().mockResolvedValue({ ok: false, json: async () => ({ errors: { inputs: ['Enter both due dates.'] } }) });
    vi.stubGlobal('fetch', fetch);
    await expect(generateOpenOrderReport({ ...initialOor(), site: 'SW' })).rejects.toThrow('Enter both due dates.');
    expect(fetch).toHaveBeenCalledWith('http://127.0.0.1:1234/api/v1/reports/open-orders', expect.objectContaining({ method: 'POST' }));
  });
  it('native route retains kind/epoch and abort never accepts a late preparation', async () => {
    host.desktop = true; let reply!: (value: unknown) => void;
    host.invoke.mockImplementation(async (command: string) => command === 'component_mrp_begin' ? 7 : command === 'component_mrp_generate' ? new Promise(r => { reply = r; }) : undefined);
    const controller = new AbortController(); const task = generateOpenOrderReport({ ...initialOor(), site: 'SW' }, controller.signal);
    await vi.waitFor(() => expect(host.invoke).toHaveBeenCalledWith('component_mrp_generate', expect.objectContaining({ reportKind: 'OpenOrderReport', epoch: 7, oorRequest: expect.objectContaining({ site: 'SW' }) })));
    controller.abort(); reply({ outcome: 'prepared' }); await expect(task).rejects.toThrow();
    expect(host.invoke).toHaveBeenCalledWith('report_discard');
  });
});

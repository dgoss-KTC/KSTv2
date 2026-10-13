import { afterEach, describe, expect, it, vi } from 'vitest';
import { HistoricalShipmentsClient, type HistoricalShipmentsRequest } from './historicalShipmentsApi';

const host = vi.hoisted(() => ({ desktop: false, invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('./tauri-bridge', () => ({ isRunningInTauri: () => host.desktop, resolveBackendBaseUrl: async () => 'http://127.0.0.1:1234' }));
const request: HistoricalShipmentsRequest = { site: 'SW', corpGroup: null, customerFrom: null, customerTo: null,
  itemFrom: null, itemTo: null, productLineFrom: null, productLineTo: null, salespersonFrom: null, salespersonTo: null,
  invoiceDateFrom: null, invoiceDateTo: null, columns: ['invoiceNumber'] };
describe('Historical Shipments operation transport', () => {
  afterEach(() => { vi.unstubAllGlobals(); host.desktop = false; host.invoke.mockReset(); });
  it('registers before acquisition, shares owner, pages bounded data and releases on departure', async () => {
    const fetch = vi.fn().mockImplementation(async (url: string, options: RequestInit) => ({ ok: true, status: options.method === 'DELETE' ? 204 : 200,
      json: async () => url.includes('/rows?') ? { rows: [{ physicalDetailId: 'a', cells: ['00001'] }] } : { operationId: url.split('/operations/')[1]?.split('/')[0], rowCount: 12000 } }));
    vi.stubGlobal('fetch', fetch); const client = new HistoricalShipmentsClient();
    const result = await client.acquire(request); expect(result.rowCount).toBe(12000);
    expect(fetch.mock.calls[0][1].method).toBe('PUT'); expect(fetch.mock.calls[1][1].method).toBe('POST');
    expect(fetch.mock.calls[0][1].headers).toEqual(fetch.mock.calls[1][1].headers);
    const page = await client.page(11999, 100); expect(page.rows).toHaveLength(1);
    expect(fetch.mock.calls[2][0]).toContain('/rows?offset=11999&limit=100');
    await client.release(); expect(fetch.mock.calls[fetch.mock.calls.length - 1][1].method).toBe('DELETE');
    expect(() => client.page()).toThrow('No active');
  });
  it('ignores late native acquisition after departure and uses epoch-bound discard', async () => {
    host.desktop = true; let reply!: (value: unknown) => void;
    host.invoke.mockImplementation(async (command: string, args?: { action?: string }) => command === 'component_mrp_begin' ? 9
      : args?.action === 'acquire' ? new Promise(r => { reply = r; }) : {});
    const client = new HistoricalShipmentsClient(); const pending = client.acquire(request);
    await vi.waitFor(() => expect(reply).toBeTypeOf('function'));
    await client.release(); reply({ rowCount: 2 }); await expect(pending).rejects.toThrow('superseded');
    expect(host.invoke).toHaveBeenCalledWith('sop_discard_generation', { epoch: 9 });
    expect(host.invoke).toHaveBeenCalledWith('historical_shipments_request', expect.objectContaining({ action: 'register', epoch: 9 }));
  });
  it('browser cancellation explicitly releases known operation identity', async () => {
    let reply!: (value: unknown) => void;
    const fetch = vi.fn().mockImplementation(async (_url: string, opts: RequestInit) => opts.method === 'POST'
      ? new Promise(r => { reply = r; }) : { ok: true, status: opts.method === 'DELETE' ? 204 : 200, json: async () => ({}) });
    vi.stubGlobal('fetch', fetch); const client = new HistoricalShipmentsClient(); const controller = new AbortController();
    const pending = client.acquire(request, controller.signal); await vi.waitFor(() => expect(reply).toBeTypeOf('function'));
    controller.abort(); reply({ ok: true, status: 200, json: async () => ({ rowCount: 2 }) }); await expect(pending).rejects.toThrow();
    expect(fetch.mock.calls.filter(c => c[1].method === 'DELETE').length).toBeGreaterThan(0);
  });
});

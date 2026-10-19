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
  it.each([100, 500, 1000])('sends one native bounded page of %s without reacquisition and keeps actual save kind', async limit => {
    host.desktop = true; let id = '';
    host.invoke.mockImplementation(async (command: string, args?: { action?: string; operationId?: string }) => {
      if (command === 'component_mrp_begin') return 9;
      if (args?.action === 'register') id = args.operationId!;
      return { operationId: id };
    });
    const client = new HistoricalShipmentsClient(); await client.acquire(request); const calls = host.invoke.mock.calls.length;
    await client.page(2000, limit); expect(host.invoke.mock.calls).toHaveLength(calls + 1);
    expect(host.invoke).toHaveBeenLastCalledWith('historical_shipments_request', expect.objectContaining({ action: 'page', epoch: 9, operationId: id, offset: 2000, limit }));
    const prepared = { operationId: id, preparationId: id, fileName: 'History.xlsx', byteLength: 1, expiresAtUtc: '' };
    await client.save(prepared, true, 'save'); await client.saveStatus('save'); await client.open('artifact', 'retry');
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_save', { operationId: 'save', preparationId: id, openAfterSave: true, reportKind: 'HistoricalShipments' });
    expect(host.invoke).toHaveBeenCalledWith('component_mrp_status', { operationId: 'save', reportKind: 'HistoricalShipments' }); expect(host.invoke).toHaveBeenCalledWith('component_mrp_open', { artifactId: 'artifact', requestId: 'retry', reportKind: 'HistoricalShipments' });
    await client.release();
  });
  it.each([[-1, 100], [0, 0], [0, 1001], [0, 1.5], [Number.MAX_SAFE_INTEGER + 1, 100]])('rejects invalid frontend page bounds %s/%s before transport', (offset, limit) => {
    const client = new HistoricalShipmentsClient(); expect(() => client.page(offset, limit)).toThrow('offset/limit'); expect(host.invoke).not.toHaveBeenCalled();
  });
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

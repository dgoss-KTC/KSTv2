import { afterEach, describe, expect, it, vi } from 'vitest';
import { WorkspaceShipmentsClient, getWorkspaceShipmentsSpec, initialWorkspaceShipmentsFilters } from './workspaceShipmentsApi';

vi.mock('./tauri-bridge', () => ({ resolveBackendBaseUrl: async () => 'http://127.0.0.1:1234' }));
const request = { partNumber: '0000123%_', shipDateFrom: null, shipDateTo: null };
const response = (value: unknown = {}) => ({ ok: true, json: async () => value });
describe('Workspace Shipments independent transport', () => {
  afterEach(() => { vi.unstubAllGlobals(); });
  it.each([
    [new Date(2026, 9, 13, 23, 59), '2026-07-16', '2026-10-13'],
    [new Date(2024, 2, 1, 0, 1), '2023-12-03', '2024-03-01'],
    [new Date(2026, 0, 1, 12), '2025-10-04', '2026-01-01'],
  ])('initializes ninety local calendar days across boundaries', (today, from, to) => {
    expect(initialWorkspaceShipmentsFilters(today)).toEqual({ partNumber: null, shipDateFrom: from, shipDateTo: to });
  });
  it('uses assignment routes, literal filters, separate visit owners and bounded paging; refresh releases old result', async () => {
    const fetch = vi.fn().mockImplementation(async () => response({ rowCount: 12001, rows: [{ physicalDetailId: 'different' }] }));
    vi.stubGlobal('fetch', fetch);
    const a = new WorkspaceShipmentsClient('assignment-a'); const b = new WorkspaceShipmentsClient('assignment-b');
    await getWorkspaceShipmentsSpec('assignment-a'); await a.acquire(request); await a.page(12000, 100); await b.acquire(request);
    expect(fetch.mock.calls[0][0]).toContain('/workspaces/assignment-a/shipments/spec');
    expect(fetch.mock.calls[1][1].method).toBe('PUT'); expect(fetch.mock.calls[2][1].method).toBe('POST');
    expect(JSON.parse(fetch.mock.calls[2][1].body)).toEqual(request);
    expect(fetch.mock.calls[3][0]).toContain('/rows?offset=12000&limit=100');
    expect(fetch.mock.calls[1][1].headers).toEqual(fetch.mock.calls[3][1].headers);
    expect(fetch.mock.calls[1][1].headers).not.toEqual(fetch.mock.calls[4][1].headers);
    const old = fetch.mock.calls[1][0]; await a.acquire(request);
    expect(fetch.mock.calls[6][0]).toBe(old); expect(fetch.mock.calls[6][1].method).toBe('DELETE');
    expect(fetch.mock.calls[7][0]).not.toBe(old); await a.release(); await b.release();
    expect(() => a.page()).toThrow('No active');
    expect(fetch.mock.calls.every(c => !c[0].includes('/reports/'))).toBe(true);
  });
  it('departure aborts retrieval and rejects a late uncooperative success', async () => {
    let reply!: (value: unknown) => void; let retrieval!: AbortSignal;
    const fetch = vi.fn().mockImplementation(async (_url: string, opts: RequestInit) => {
      if (opts.method === 'POST') { retrieval = opts.signal as AbortSignal; return new Promise(r => { reply = r; }); }
      return response();
    });
    vi.stubGlobal('fetch', fetch); const client = new WorkspaceShipmentsClient('a'); const pending = client.acquire(request);
    const rejected = expect(pending).rejects.toThrow();
    await vi.waitFor(() => expect(reply).toBeTypeOf('function')); await client.release();
    expect(retrieval.aborted).toBe(true); reply(response({ rowCount: 2 })); await rejected;
    expect(fetch.mock.calls.some(c => c[1].method === 'DELETE')).toBe(true);
  });
  it('caller cancellation explicitly releases even before a late registration resolves', async () => {
    let reply!: (value: unknown) => void;
    const fetch = vi.fn().mockImplementation(async (_url: string, opts: RequestInit) => opts.method === 'PUT' ? new Promise(r => { reply = r; }) : response());
    vi.stubGlobal('fetch', fetch); const client = new WorkspaceShipmentsClient('a'); const abort = new AbortController();
    const pending = client.acquire(request, abort.signal); const rejected = expect(pending).rejects.toThrow();
    await vi.waitFor(() => expect(reply).toBeTypeOf('function')); abort.abort(); reply(response()); await rejected;
    expect(fetch.mock.calls.some(c => c[1].method === 'DELETE')).toBe(true);
    expect(fetch.mock.calls.some(c => c[1].method === 'POST')).toBe(false);
  });
  it('replacement rejects old page replies and an old failure cannot release the new operation', async () => {
    let reply!: (value: unknown) => void;
    const fetch = vi.fn().mockImplementation(async (url: string) => url.includes('/rows?') ? new Promise(r => { reply = r; }) : response());
    vi.stubGlobal('fetch', fetch); const client = new WorkspaceShipmentsClient('a'); await client.acquire(request);
    const page = client.page(); const rejected = expect(page).rejects.toThrow();
    await vi.waitFor(() => expect(reply).toBeTypeOf('function')); await client.acquire(request);
    reply(response({ rows: [{ physicalDetailId: 'old' }] })); await rejected;
    await expect(client.status()).resolves.toEqual({}); await client.release();
  });
  it('a late acquisition failure releases only its old identity while a new acquisition remains ready', async () => {
    let reply!: (value: unknown) => void; let posts = 0;
    const fetch = vi.fn().mockImplementation(async (_url: string, opts: RequestInit) => {
      if (opts.method === 'POST' && ++posts === 1) return new Promise(r => { reply = r; });
      return response();
    });
    vi.stubGlobal('fetch', fetch); const client = new WorkspaceShipmentsClient('a');
    const old = client.acquire(request); const rejected = expect(old).rejects.toThrow();
    await vi.waitFor(() => expect(reply).toBeTypeOf('function')); await client.acquire(request);
    const newId = fetch.mock.calls.filter(c => c[1].method === 'POST')[1][0];
    reply(response({ rowCount: 2 })); await rejected;
    await expect(client.status()).resolves.toEqual({});
    expect(fetch.mock.calls.filter(c => c[1].method === 'DELETE').every(c => !newId.startsWith(c[0]))).toBe(true);
    await client.release();
  });
});

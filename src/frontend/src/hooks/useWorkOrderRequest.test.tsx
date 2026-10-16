import { act, renderHook, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { useWorkOrderRequest } from './useWorkOrderRequest';

function deferred<T>() { let resolve!: (value: T) => void; let reject!: (reason: unknown) => void; const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no; }); return { promise, resolve, reject }; }

describe('WO read lifecycle', () => {
  it('masks old data immediately, and rejects late errors and successes across A → B → A', async () => {
    const old = deferred<string>();
    const fresh = deferred<string>();
    const request = vi.fn<() => Promise<string>>().mockReturnValueOnce(old.promise).mockResolvedValueOnce('B result').mockReturnValueOnce(fresh.promise);
    const view = renderHook(({ identity }) => useWorkOrderRequest(identity, true, request), { initialProps: { identity: 'A' } });
    await waitFor(() => expect(request).toHaveBeenCalledTimes(1));
    view.rerender({ identity: 'B' });
    expect(view.result.current.data).toBeNull();
    await waitFor(() => expect(view.result.current.data).toBe('B result'));
    view.rerender({ identity: 'A' });
    expect(view.result.current.data).toBeNull();
    await waitFor(() => expect(request).toHaveBeenCalledTimes(3));
    await act(async () => old.reject(new Error('Old A failed')));
    expect(view.result.current.error).toBeNull(); expect(view.result.current.isLoading).toBe(true);
    await act(async () => fresh.resolve('Fresh A result'));
    expect(view.result.current.data).toBe('Fresh A result');
  });

  it('publishes only the latest retry and never turns a failure into an empty success', async () => {
    const first = deferred<string[]>(); const second = deferred<string[]>();
    const request = vi.fn<() => Promise<string[]>>().mockReturnValueOnce(first.promise).mockReturnValueOnce(second.promise);
    const view = renderHook(() => useWorkOrderRequest('same identity', true, request));
    await waitFor(() => expect(request).toHaveBeenCalledTimes(1));
    let retry!: Promise<void>;
    act(() => { retry = view.result.current.retry(); });
    await act(async () => { second.reject(new Error('Source unavailable')); await retry; });
    expect(view.result.current.data).toBeNull(); expect(view.result.current.error?.detail).toBe('Source unavailable');
    await act(async () => first.resolve(['OBSOLETE']));
    expect(view.result.current.data).toBeNull(); expect(view.result.current.error?.detail).toBe('Source unavailable');
  });

  it('ignores completion after unmount, and starts no disabled read', async () => {
    const pending = deferred<string>(); const request = vi.fn(() => pending.promise);
    const view = renderHook(({ enabled }) => useWorkOrderRequest(JSON.stringify(enabled), enabled, request), { initialProps: { enabled: false } });
    expect(request).not.toHaveBeenCalled(); expect(view.result.current.isLoading).toBe(false);
    view.rerender({ enabled: true }); await waitFor(() => expect(request).toHaveBeenCalledTimes(1));
    view.unmount(); await act(async () => pending.resolve('late'));
  });
});

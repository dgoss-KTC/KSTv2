import { act, cleanup, renderHook, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { rememberAnalyticalOpenPreference, useAnalyticalOpenPreference } from './analyticalOpenPreference';
const host = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
function deferred<T>() { let resolve!: (value: T) => void; let reject!: (error: Error) => void; const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no; }); return { promise, resolve, reject }; }
beforeEach(() => { host.invoke.mockReset(); host.invoke.mockResolvedValue(false); });
afterEach(cleanup);
it('a late load from an earlier consumer cannot replace a newer user choice shared across consumers', async () => {
  const old = deferred<boolean>(); host.invoke.mockReturnValueOnce(old.promise);
  const a = renderHook(useAnalyticalOpenPreference); await waitFor(() => expect(host.invoke).toHaveBeenCalledWith('component_mrp_open_preference'));
  act(() => rememberAnalyticalOpenPreference(true)); const b = renderHook(useAnalyticalOpenPreference);
  await act(async () => old.resolve(false)); expect(a.result.current.value).toBe(true); expect(b.result.current.value).toBe(true);
  await waitFor(() => expect(host.invoke).toHaveBeenCalledWith('component_mrp_remember_open_preference', { value: true }));
});
it('serializes rapid writes and an older failure never overwrites the latest session value/warning', async () => {
  const first = deferred<void>(); const second = deferred<void>();
  host.invoke.mockImplementation((n: string, args?: { value: boolean }) => n === 'component_mrp_remember_open_preference' ? args?.value ? first.promise : second.promise : Promise.resolve(false));
  const view = renderHook(useAnalyticalOpenPreference);
  act(() => { rememberAnalyticalOpenPreference(true); rememberAnalyticalOpenPreference(false); });
  await waitFor(() => expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_remember_open_preference')).toHaveLength(1));
  await act(async () => first.reject(new Error('older write failed')));
  await waitFor(() => expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_remember_open_preference')).toHaveLength(2));
  expect(view.result.current.value).toBe(false); expect(view.result.current.warning).toBe(''); await act(async () => second.resolve());
});
it('ignores an obsolete failed load and retains the latest remembered value for later consumers', async () => {
  const old = deferred<boolean>(); host.invoke.mockReturnValueOnce(old.promise);
  const a = renderHook(useAnalyticalOpenPreference); await waitFor(() => expect(host.invoke).toHaveBeenCalledWith('component_mrp_open_preference'));
  act(() => rememberAnalyticalOpenPreference(true)); await act(async () => old.reject(new Error('obsolete read')));
  expect(a.result.current).toEqual({ value: true, warning: '' });
});

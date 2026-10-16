import { act, renderHook } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { useLocalBusinessDate } from './useLocalBusinessDate';

afterEach(() => vi.useRealTimers());
it('invalidates at local midnight, including Saturday to Sunday, without source acquisition', async () => {
  vi.useFakeTimers(); vi.setSystemTime(new Date(2026, 9, 17, 23, 59, 59, 980));
  const hook = renderHook(useLocalBusinessDate); expect(hook.result.current).toBe('2026-10-17');
  await act(async () => vi.advanceTimersByTimeAsync(60)); expect(hook.result.current).toBe('2026-10-18');
  hook.unmount(); expect(vi.getTimerCount()).toBe(0);
});
it('rechecks after sleep/window focus and releases its timer on unmount', () => {
  vi.useFakeTimers(); vi.setSystemTime(new Date(2026, 9, 17, 12));
  const hook = renderHook(useLocalBusinessDate);
  vi.setSystemTime(new Date(2026, 9, 19, 12)); act(() => window.dispatchEvent(new Event('focus')));
  expect(hook.result.current).toBe('2026-10-19'); hook.unmount(); expect(vi.getTimerCount()).toBe(0);
});

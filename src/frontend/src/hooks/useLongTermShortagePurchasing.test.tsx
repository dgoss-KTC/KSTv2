import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, expect, it, vi } from 'vitest';
import { DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS as options, fetchLongTermShortagePurchasing, type LongTermShortagePurchasing } from '../api/longTermShortagesApi';
import { useLongTermShortagePurchasing } from './useLongTermShortagePurchasing';

vi.mock('../api/longTermShortagesApi', async original => ({
  ...await original<typeof import('../api/longTermShortagesApi')>(), fetchLongTermShortagePurchasing: vi.fn(),
}));
const empty = (comment: string): LongTermShortagePurchasing => ({ commentAvailable: true, currentComment: comment,
  openPurchaseOrders: [], suppliers: { value: [], error: null, acquiredAtUtc: '2026-10-20T20:00:00Z' }, supplierIdentityUnavailable: false });
function deferred() {
  let resolve!: (value: LongTermShortagePurchasing) => void;
  return { promise: new Promise<LongTermShortagePurchasing>(finish => { resolve = finish; }), resolve: (value: LongTermShortagePurchasing) => resolve(value) };
}
beforeEach(() => vi.mocked(fetchLongTermShortagePurchasing).mockReset());

it.each(['component', 'snapshot', 'workspace'] as const)('rejects late supplier/comment acquisition after %s changes and preserves the issuing identity', async dimension => {
  const old = deferred();
  vi.mocked(fetchLongTermShortagePurchasing).mockReturnValueOnce(old.promise).mockResolvedValueOnce(empty('New selected comment'));
  const entry = { workspace: 'WS-A', snapshot: 'SNAP-A', component: 'PART-A' };
  const next = { ...entry, [dimension]: `${entry[dimension]}-NEW` };
  const { result, rerender } = renderHook(({ workspace, snapshot, component }) =>
    useLongTermShortagePurchasing(workspace, snapshot, component, options), { initialProps: entry });
  rerender(next);
  expect(result.current.data).toBeNull();
  await waitFor(() => expect(result.current.data?.currentComment).toBe('New selected comment'));
  const stale = empty('Obsolete comment');
  stale.suppliers!.value = [{ identifier: 'OLD SUPPLIER', display: 'Old', isCreditHold: true, isCia: true }];
  await act(async () => old.resolve(stale));
  expect(result.current.data?.currentComment).toBe('New selected comment');
  expect(result.current.data?.suppliers?.value).toEqual([]);
  expect(fetchLongTermShortagePurchasing).toHaveBeenLastCalledWith(next.workspace, next.snapshot, next.component,
    { ...options, includeUnconfirmed: false, showAll: true });
  expect(fetchLongTermShortagePurchasing).toHaveBeenCalledTimes(2);
});

it('retries localized supplier failure through the existing selected purchasing path without discarding successful comments', async () => {
  const failed = empty('Preserved comment');
  failed.suppliers = { value: [{ identifier: 'ACME', display: 'Acme', isCreditHold: null, isCia: null }], error: 'Unavailable', acquiredAtUtc: null };
  const recovered = empty('Current comment');
  recovered.suppliers!.value = [{ identifier: 'ACME', display: 'Acme', isCreditHold: false, isCia: true }];
  vi.mocked(fetchLongTermShortagePurchasing).mockResolvedValueOnce(failed).mockResolvedValueOnce(recovered);
  const { result } = renderHook(() => useLongTermShortagePurchasing('WS', 'SNAP', 'COMP', options));
  await waitFor(() => expect(result.current.data?.suppliers?.error).toBe('Unavailable'));
  expect(result.current.data?.currentComment).toBe('Preserved comment');
  expect(result.current.error).toBe(false);
  act(() => result.current.retry());
  await waitFor(() => expect(result.current.data?.suppliers?.value?.[0].isCia).toBe(true));
  expect(result.current.data?.currentComment).toBe('Current comment');
  expect(fetchLongTermShortagePurchasing).toHaveBeenCalledTimes(2);
});

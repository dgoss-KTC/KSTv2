import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, expect, it, vi } from 'vitest';
import { DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS as options, fetchLongTermShortageProjectionDetail, type LongTermShortageProjectionDetail } from '../api/longTermShortagesApi';
import { useLongTermShortageProjectionDetail } from './useLongTermShortageProjectionDetail';

vi.mock('../api/longTermShortagesApi', async (original) => ({
  ...await original<typeof import('../api/longTermShortagesApi')>(), fetchLongTermShortageProjectionDetail: vi.fn(),
}));
const detail = (snapshotId: string, componentPart: string): LongTermShortageProjectionDetail => ({ snapshotId, componentPart,
  refreshDate: '2026-09-29', acquiredAtUtc: '2026-09-29T12:00:00Z', consistencyMode: 'PRO2_READ_UNCOMMITTED',
  demandParentParts: ['PARENT'], pastGrossRequirements: 1, overdueReceipts: 2, adjustedOpeningQoh: -3,
  effectivePmCode: 'P', manufacturingLeadWorkingDays: null, manufacturerItem: null });
beforeEach(() => vi.mocked(fetchLongTermShortageProjectionDetail).mockReset());

it('discards a late A response after selection advances to snapshot B', async () => {
  let finishA!: (value: LongTermShortageProjectionDetail) => void;
  vi.mocked(fetchLongTermShortageProjectionDetail).mockImplementationOnce(() => new Promise(resolve => { finishA = resolve; }))
    .mockResolvedValueOnce(detail('B', 'COMP'));
  const { result, rerender } = renderHook(({ snapshot }) => useLongTermShortageProjectionDetail('WS', snapshot, 'COMP', options), { initialProps: { snapshot: 'A' } });
  rerender({ snapshot: 'B' });
  await waitFor(() => expect(result.current.data?.snapshotId).toBe('B'));
  await act(async () => finishA(detail('A', 'COMP')));
  expect(result.current.data?.snapshotId).toBe('B');
  expect(fetchLongTermShortageProjectionDetail).toHaveBeenCalledTimes(2);
});

it('does not reacquire selected detail for receipt-mode and Show All changes', async () => {
  vi.mocked(fetchLongTermShortageProjectionDetail).mockResolvedValue(detail('A', 'COMP'));
  const { result, rerender } = renderHook(({ includeUnconfirmed, showAll }) =>
    useLongTermShortageProjectionDetail('WS', 'A', 'COMP', { ...options, includeUnconfirmed, showAll }),
  { initialProps: { includeUnconfirmed: false, showAll: false } });
  await waitFor(() => expect(result.current.data).not.toBeNull());
  rerender({ includeUnconfirmed: true, showAll: true });
  expect(fetchLongTermShortageProjectionDetail).toHaveBeenCalledTimes(1);
});

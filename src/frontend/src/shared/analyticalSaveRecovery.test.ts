import { act, renderHook } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { beginAnalyticalSave, reconcileAnalyticalSave, recordAnalyticalSave, useAnalyticalSaveRecovery } from './analyticalSaveRecovery';
import type { AnalyticalExportClient, AnalyticalSaveResult } from '../api/analyticalExportApi';

it('terminal operation outcomes cannot be overwritten by an older pending status reply', async () => {
  let reply!: (result: AnalyticalSaveResult) => void;
  const client = { status: vi.fn(() => new Promise(r => { reply = r; })) } as unknown as AnalyticalExportClient;
  const hook = renderHook(useAnalyticalSaveRecovery);
  act(() => beginAnalyticalSave('op', client));
  let check!: Promise<void>;
  act(() => { check = reconcileAnalyticalSave(); });
  act(() => recordAnalyticalSave('op', { outcome: 'saved', message: 'Workbook saved.', operationComplete: true }));
  await act(async () => { reply({ outcome: 'pending', message: 'Earlier pending response', operationComplete: false }); await check; });
  expect(hook.result.current).toMatchObject({ pending: false, message: 'Workbook saved.' });
});

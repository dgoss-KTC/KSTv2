import { act, cleanup, fireEvent, renderHook, screen } from '@testing-library/react';
import { dismissAnalyticalSave, recordAnalyticalSave, useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';
import type { ComponentMrpPrepared } from '../api/componentMrpApi';
export const reportFixture: ComponentMrpPrepared = { preparationId: 'exact-one', fileName: 'Synthetic.xlsx', asOfDate: '2026-10-19', acquiredAtUtc: '2026-10-19T12:00:00Z', expiresAtUtc: '2026-10-19T12:30:00Z', warning: null, period: 'Week', includeUnconfirmed: true, forecastEndExclusive: '2027-04-18' };
export function deferred<T>() { let resolve!: (value: T) => void; let reject!: (error: Error) => void; const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no; }); return { promise, resolve, reject }; }
export async function exportWorkbook() { fireEvent.click(screen.getByRole('button', { name: 'Export' })); await act(async () => fireEvent.click(screen.getByRole('menuitem', { name: /Excel workbook/ }))); }
export function clearExportTests() {
  cleanup(); const hook = renderHook(useAnalyticalExportStatus);
  act(() => { hook.result.current.operations.forEach(o => { recordAnalyticalSave(o.id, { outcome: 'failed', message: 'Test cleanup', operationComplete: true }); dismissAnalyticalSave(o.id); }); }); hook.unmount();
}

import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { ReportsPanel } from './ReportsPanel';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';
import { reportFixture, deferred, exportWorkbook, clearExportTests } from '../reports/reportTestSupport';
import { saveFiscalCalendarSettings } from '../fiscal/fiscalCalendarSettings';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from '../fiscal/fiscalCalendar';
const host = vi.hoisted(() => ({ invoke: vi.fn() }));
vi.mock('@tauri-apps/api/core', () => ({ invoke: host.invoke }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => true }));
const result = { ...reportFixture, outcome: 'prepared' };
beforeEach(() => { localStorage.clear(); host.invoke.mockReset(); host.invoke.mockImplementation(async (n: string) => n === 'component_mrp_begin' ? 5 : n === 'component_mrp_save' ? { outcome: 'cancelled', message: 'Save cancelled.', operationComplete: true } : false); });
afterEach(clearExportTests);
function inputs() { fireEvent.click(screen.getByRole('button', { name: 'S&OP' })); fireEvent.change(screen.getByLabelText('Product Line From 1'), { target: { value: '2142' } }); fireEvent.change(screen.getByLabelText('Product Line To 1'), { target: { value: '2140' } }); }
it('keeps ordered repeated/reversed ranges, five-row limit, persistence and actual Sop save kind', async () => {
  const generate = vi.fn().mockResolvedValue(result); render(<><AnalyticalSaveStatus /><ReportsPanel generateSopReport={generate} /></>); inputs();
  expect(screen.getByRole('button', { name: 'Planner ID' })).toHaveAttribute('aria-pressed', 'true');
  fireEvent.click(screen.getByRole('button', { name: 'Add product line' })); fireEvent.change(screen.getByLabelText('Product Line From 2'), { target: { value: '2142' } }); fireEvent.change(screen.getByLabelText('Product Line To 2'), { target: { value: '2140' } });
  await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' }); expect(generate.mock.calls[0][0].ranges).toEqual([{ from: '2142', to: '2140' }, { from: '2142', to: '2140' }]);
  expect(host.invoke).toHaveBeenCalledWith('component_mrp_save', expect.objectContaining({ reportKind: 'Sop' }));
  for (let i = 0; i < 3; i++) fireEvent.click(screen.getByRole('button', { name: 'Add product line' })); expect(screen.getByRole('button', { name: 'Add product line' })).toBeDisabled();
  expect(JSON.parse(localStorage.getItem('kst.sop.inputs.v1')!).ranges).toHaveLength(5);
});
it.each(['local', 'storage'])('invalidates exact retry and late preparations on %s fiscal change', async source => {
  const pending = deferred<typeof result>(); const generate = vi.fn().mockResolvedValueOnce(result).mockReturnValueOnce(pending.promise);
  render(<><AnalyticalSaveStatus /><ReportsPanel generateSopReport={generate} /></>); inputs(); await exportWorkbook(); await screen.findByRole('button', { name: 'Save again' });
  const change = () => source === 'local' ? saveFiscalCalendarSettings({ ...DEFAULT_FISCAL_CALENDAR_SETTINGS, exceptions: [{ fiscalYear: 2027, extraWeekPeriod: 4 }] }) : window.dispatchEvent(new StorageEvent('storage', { key: 'kst.fiscalCalendar.v1' }));
  // Use the real storage key, retained by the presentation module.
  const { fiscalStorageKey } = await import('../reports/sopPresentation');
  act(() => { if (source === 'local') change(); else window.dispatchEvent(new StorageEvent('storage', { key: fiscalStorageKey })); });
  expect(screen.queryByRole('button', { name: 'Save again' })).toBeNull();
  await exportWorkbook(); await waitFor(() => expect(generate).toHaveBeenCalledTimes(2));
  act(() => window.dispatchEvent(new StorageEvent('storage', { key: fiscalStorageKey })));
  await act(async () => pending.resolve(result)); expect(host.invoke.mock.calls.filter(([n]) => n === 'component_mrp_save')).toHaveLength(1); expect(generate.mock.calls[1][1].aborted).toBe(true);
});
it('keeps row validation/removal and keyboard path through segmented Group By', async () => {
  const user = userEvent.setup(); render(<ReportsPanel />); inputs();
  screen.getByLabelText('Site').focus(); await user.tab(); expect(screen.getByRole('button', { name: 'Planner ID' })).toHaveFocus(); await user.tab(); expect(screen.getByRole('button', { name: 'Customer Name' })).toHaveFocus(); await user.tab(); expect(screen.getByLabelText('Product Line From 1')).toHaveFocus();
  fireEvent.click(screen.getByRole('button', { name: 'Add product line' })); fireEvent.change(screen.getByLabelText('Product Line From 2'), { target: { value: '3100' } });
  expect(screen.getByRole('alert')).toHaveTextContent('Enter both From and To'); expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
  fireEvent.click(screen.getByRole('button', { name: 'Remove range 2' })); expect(screen.queryByLabelText('Product Line From 2')).toBeNull(); expect(screen.getByRole('button', { name: 'Remove range 1' })).toBeDisabled();
});
it('reports S&OP noData as successful empty feedback/footer without Save As', async () => {
  render(<><AnalyticalSaveStatus /><ReportsPanel generateSopReport={vi.fn().mockResolvedValue({ ...result, outcome: 'noData', preparationId: null, fileName: null })} /></>); inputs(); await exportWorkbook();
  expect(screen.getAllByText('No S&OP data found for the selected ranges.')).toHaveLength(2); expect(host.invoke.mock.calls.some(([n]) => n === 'component_mrp_save')).toBe(false);
});

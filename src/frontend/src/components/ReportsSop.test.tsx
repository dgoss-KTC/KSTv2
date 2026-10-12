import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { beforeEach, expect, it, vi } from 'vitest';
import { ReportsPanel, type ReportSavePort } from './ReportsPanel';
import type { SopPrepared } from '../api/sopApi';
import { saveFiscalCalendarSettings } from '../fiscal/fiscalCalendarSettings';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from '../fiscal/fiscalCalendar';

vi.mock('@tauri-apps/api/core', () => ({ invoke: vi.fn().mockResolvedValue(0) }));
const result: SopPrepared = { outcome: 'prepared', preparationId: 'sop', fileName: 'S&OP_SW_2026-10-12_01.xlsx', asOfDate: '2026-10-12', acquiredAtUtc: '2026-10-12T12:00:00Z', expiresAtUtc: '2026-10-12T12:30:00Z' };
const port = (): ReportSavePort => ({ loadOpenPreference: vi.fn().mockResolvedValue(false), rememberOpenPreference: vi.fn().mockResolvedValue(undefined), save: vi.fn().mockResolvedValue({ outcome: 'saved', message: 'Saved', savedArtifactId: 'artifact' }), open: vi.fn().mockResolvedValue(undefined), discard: vi.fn().mockResolvedValue(undefined), status: vi.fn().mockResolvedValue({ outcome: 'saved', message: 'Saved', operationComplete: true }) });
beforeEach(() => localStorage.clear());
function inputs() { fireEvent.click(screen.getByRole('button', { name: 'S&OP' })); fireEvent.change(screen.getByLabelText('Product Line From 1'), { target: { value: '2142' } }); fireEvent.change(screen.getByLabelText('Product Line To 1'), { target: { value: '2140' } }); }

it('defaults, ordered repeated/reversed pairs, five-row limit, remember inputs, kind-bound save and calendar invalidation', async () => {
  const generate = vi.fn().mockResolvedValue(result); const savePort = port();
  render(<ReportsPanel generateSopReport={generate} savePort={savePort} />); inputs();
  expect(screen.getByLabelText('Site')).toHaveValue('SW'); expect(screen.getByLabelText('Group By')).toHaveValue('planr');
  const productLines = screen.getByRole('region', { name: 'Product lines' });
  expect(productLines.querySelectorAll('.sop-inputs__range-row')).toHaveLength(1);
  expect(screen.getByRole('button', { name: 'Generate' })).toBeEnabled();
  expect(screen.getByLabelText('Site').compareDocumentPosition(productLines) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  fireEvent.click(screen.getByRole('button', { name: 'Add product line' }));
  fireEvent.change(screen.getByLabelText('Product Line From 2'), { target: { value: '2142' } }); fireEvent.change(screen.getByLabelText('Product Line To 2'), { target: { value: '2140' } });
  fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
  expect(generate.mock.calls[0][0].ranges).toEqual([{ from: '2142', to: '2140' }, { from: '2142', to: '2140' }]);
  fireEvent.click(screen.getByText('Save As…')); await waitFor(() => expect(savePort.save).toHaveBeenCalledWith(result, false, expect.any(String), 'Sop'));
  act(() => saveFiscalCalendarSettings({ ...DEFAULT_FISCAL_CALENDAR_SETTINGS, exceptions: [{ fiscalYear: 2027, extraWeekPeriod: 4 }] }));
  expect(screen.getByText('Save As…')).toBeDisabled();
  for (let i = 0; i < 3; i++) fireEvent.click(screen.getByRole('button', { name: 'Add product line' }));
  expect(screen.getByRole('button', { name: 'Add product line' })).toBeDisabled();
  expect(JSON.parse(localStorage.getItem('kst.sop.inputs.v1')!).ranges).toHaveLength(5);
  expect(screen.getAllByText(/^Range [1-5]$/)).toHaveLength(5);
  expect(productLines.querySelectorAll('.sop-inputs__range-row')).toHaveLength(5);
  expect(within(productLines).getByRole('button', { name: 'Add product line' })).toBeDisabled();
  expect(screen.getByRole('button', { name: 'Generate' })).toBeEnabled();
});
it('departure cancels acquisition and stale success cannot restore prepared state; prefs survive', async () => {
  let resolve!: (value: SopPrepared) => void;
  const generate = vi.fn((_request, signal: AbortSignal | undefined) => new Promise<SopPrepared>(r => { resolve = r; expect(signal?.aborted).toBe(false); }));
  const savePort = port(); const view = render(<ReportsPanel generateSopReport={generate} savePort={savePort} />); inputs();
  fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(generate).toHaveBeenCalled());
  view.rerender(<ReportsPanel generateSopReport={generate} savePort={savePort} active={false} />);
  expect(generate.mock.calls[0][1]?.aborted).toBe(true); await act(async () => resolve(result));
  expect(screen.getByText('Save As…')).toBeDisabled(); expect(savePort.discard).toHaveBeenCalled();
  expect(localStorage.getItem('kst.sop.inputs.v1')).toContain('2142');
});
it('unknown Sop save reconciles with issuing kind after report switch without replay', async () => {
  const savePort = port(); savePort.save = vi.fn().mockRejectedValue(new Error('lost'));
  render(<ReportsPanel generateSopReport={vi.fn().mockResolvedValue(result)} savePort={savePort} />); inputs();
  fireEvent.click(screen.getByText('Generate')); await waitFor(() => expect(screen.getByText('Save As…')).toBeEnabled());
  fireEvent.click(screen.getByText('Save As…')); await waitFor(() => expect(screen.getByText('Check save status')).toBeEnabled());
  fireEvent.click(screen.getByRole('button', { name: 'Shipments-To-Go' })); fireEvent.click(screen.getByText('Check save status'));
  await waitFor(() => expect(savePort.status).toHaveBeenCalledWith(expect.any(String), 'Sop'));
  expect(savePort.save).toHaveBeenCalledTimes(1); expect(screen.queryByText(/Prepared:/)).not.toBeInTheDocument();
});

it('keeps rows ordered when adding/removing and exposes incomplete-range validation', () => {
  render(<ReportsPanel generateSopReport={vi.fn()} savePort={port()} />); inputs();
  act(() => fireEvent.click(screen.getByRole('button', { name: 'Add product line' })));
  act(() => fireEvent.change(screen.getByLabelText('Product Line From 2'), { target: { value: '3100' } }));
  act(() => fireEvent.change(screen.getByLabelText('Product Line To 2'), { target: { value: '3200' } }));
  act(() => fireEvent.click(screen.getByRole('button', { name: 'Add product line' })));
  act(() => fireEvent.change(screen.getByLabelText('Product Line From 3'), { target: { value: '4100' } }));
  expect(screen.getAllByText(/^Range [1-3]$/)).toHaveLength(3);
  act(() => fireEvent.click(screen.getByRole('button', { name: 'Remove range 2' })));
  expect(screen.queryByLabelText('Product Line From 3')).not.toBeInTheDocument();
  expect(screen.getByLabelText('Product Line From 2')).toHaveValue('4100');
  expect(screen.getByLabelText('Product Line To 2')).toHaveValue('');
  expect(screen.getByRole('alert')).toHaveTextContent('Enter both From and To');
  expect(screen.getByLabelText('Product Line To 2')).toHaveAttribute('aria-invalid', 'true');
});

it('preserves tab order through compact row controls, add control and Generate', async () => {
  const user = (await import('@testing-library/user-event')).default.setup();
  render(<ReportsPanel generateSopReport={vi.fn()} savePort={port()} />); inputs();
  screen.getByLabelText('Site').focus();
  await user.tab(); expect(screen.getByLabelText('Group By')).toHaveFocus();
  await user.tab(); expect(screen.getByLabelText('Product Line From 1')).toHaveFocus();
  await user.tab(); expect(screen.getByLabelText('Product Line To 1')).toHaveFocus();
  await user.tab(); expect(screen.getByRole('button', { name: 'Add product line' })).toHaveFocus();
  await user.tab(); expect(screen.getByRole('button', { name: 'Generate' })).toHaveFocus();
});

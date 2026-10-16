import { act, cleanup, render, renderHook, screen } from '@testing-library/react';
import { StrictMode } from 'react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { AnalyticalSaveStatus } from './AnalyticalSaveStatus';
import { beginAnalyticalSave, claimAnalyticalPreparation, clearAnalyticalPreparation, dismissAnalyticalSave, openAnalyticalArtifact, recordAnalyticalSave, useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';
import type { AnalyticalExportClient, AnalyticalSaveResult } from '../api/analyticalExportApi';

let sequence = 0;
const client = { prepare: vi.fn(), save: vi.fn(), status: vi.fn(), release: vi.fn(), open: vi.fn() };
const port = client as unknown as AnalyticalExportClient;
function start(result?: AnalyticalSaveResult) {
  const id = `feedback-${++sequence}`;
  act(() => { expect(beginAnalyticalSave(id, port, id, id)).toBe(true); if (result) recordAnalyticalSave(id, result); });
  return id;
}
const saved = (message = 'Workbook saved.'): AnalyticalSaveResult => ({ outcome: 'saved', operationComplete: true, message, savedArtifactId: 'host-owned-artifact' });
const tick = async (milliseconds: number) => { await act(async () => vi.advanceTimersByTimeAsync(milliseconds)); };
beforeEach(() => { vi.useFakeTimers(); vi.clearAllMocks(); client.open.mockResolvedValue(undefined); });
afterEach(() => {
  cleanup(); const hook = renderHook(useAnalyticalExportStatus);
  act(() => {
    if (hook.result.current.preparation) clearAnalyticalPreparation(hook.result.current.preparation.owner);
    for (const operation of hook.result.current.operations) { recordAnalyticalSave(operation.id, { outcome: 'failed', message: 'Test cleanup' }); dismissAnalyticalSave(operation.id); }
  });
  hook.unmount(); vi.useRealTimers();
});

it.each(['Workbook saved.', 'Workbook saved; opening request accepted.'])('dismisses ordinary final success at eight seconds: %s, without changing the outcome or native authority', async message => {
  render(<StrictMode><AnalyticalSaveStatus /></StrictMode>); const state = renderHook(useAnalyticalExportStatus);
  const id = start(saved(message)); const outcome = state.result.current.operations.find(o => o.id === id)!.result;
  await tick(7999); expect(screen.getByText(message)).toBeInTheDocument();
  await tick(1); expect(screen.queryByText(message)).not.toBeInTheDocument(); expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
  expect(state.result.current.operations.find(o => o.id === id)).toMatchObject({ dismissed: true, pending: false, result: outcome });
  expect(state.result.current.operations.find(o => o.id === id)!.result).toBe(outcome);
  for (const method of Object.values(client)) expect(method).not.toHaveBeenCalled();
});

it('starts the full timer only after the requested automatic opening has settled', async () => {
  render(<AnalyticalSaveStatus />);
  const id = start({ ...saved('Workbook saved; opening is in progress.'), operationComplete: false });
  await tick(16000); expect(screen.getByRole('button', { name: 'Check save status' })).toBeInTheDocument();
  act(() => recordAnalyticalSave(id, saved('Workbook saved; opening request accepted.')));
  await tick(7999); expect(screen.getByText('Workbook saved; opening request accepted.')).toBeInTheDocument();
  await tick(1); expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
});

it.each([
  ['cancelled', { outcome: 'cancelled', operationComplete: true, message: 'Save cancelled; workbook retained.' }],
  ['failed', { outcome: 'failed', operationComplete: true, message: 'Save failed; workbook retained.' }],
] as const)('keeps %s workbook recovery beyond eight seconds', async (_kind, result) => {
  render(<AnalyticalSaveStatus />); const id = start(result);
  const saveAgain = vi.fn(); const discard = vi.fn();
  act(() => claimAnalyticalPreparation({ owner: id, label: id, preparing: false, message: result.message, supersede: vi.fn(), actions: [{ label: 'Save again', run: saveAgain }, { label: 'Discard workbook', run: discard }] }));
  await tick(16000); expect(screen.getByRole('button', { name: 'Save again' })).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Discard workbook' })).toBeInTheDocument();
  expect(saveAgain).not.toHaveBeenCalled(); expect(discard).not.toHaveBeenCalled();
});

it.each([
  { outcome: 'pending', operationComplete: false, message: 'Save pending.' },
  { outcome: 'unknown', message: 'Save uncertain.' },
  { outcome: 'saved', operationComplete: false, message: 'Workbook saved; opening is in progress.' },
])('keeps unresolved $outcome feedback and its observation-only recovery', async result => {
  render(<AnalyticalSaveStatus />); start(result); await tick(16000);
  expect(screen.getByText(result.message)).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Check save status' })).toBeInTheDocument();
  expect(client.status).not.toHaveBeenCalled(); expect(client.save).not.toHaveBeenCalled();
});

it.each([
  'Workbook saved, but save preferences could not be remembered.',
  'Workbook saved; opening request accepted, but save preferences could not be remembered.',
  'Unresolved failure requiring action.',
])('keeps non-routine failure/warning feedback: %s', async message => {
  render(<AnalyticalSaveStatus />); start(saved(message)); await tick(16000); expect(screen.getByText(message)).toBeInTheDocument();
});

it('keeps Saved/Open again persistent, then times the settled opening acknowledgement rather than the original save', async () => {
  render(<AnalyticalSaveStatus />); const id = start({ ...saved('Workbook saved, but could not be opened.'), openFailed: true });
  await tick(16000); expect(screen.getByRole('button', { name: 'Open again' })).toBeInTheDocument();
  let resolve!: () => void; client.open.mockReturnValue(new Promise<void>(r => { resolve = r; }));
  let opening!: Promise<void>; act(() => { opening = openAnalyticalArtifact(id); });
  await tick(16000); expect(screen.getByRole('button', { name: 'Opening…' })).toBeDisabled();
  await act(async () => { resolve(); await opening; });
  await tick(7999); expect(screen.getByText(/opening request accepted/)).toBeInTheDocument();
  await tick(1); expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
  expect(client.open).toHaveBeenCalledTimes(1); expect(client.save).not.toHaveBeenCalled(); expect(client.release).not.toHaveBeenCalled();
});

it('an older operation timer cannot hide a newer success notice', async () => {
  render(<AnalyticalSaveStatus />); start(saved('Workbook saved.')); await tick(4000);
  const newer = start(saved('Workbook saved; opening request accepted.'));
  await tick(4000); expect(screen.queryByText('Workbook saved.')).not.toBeInTheDocument();
  expect(screen.getByLabelText(`${newer} save outcome`)).toBeInTheDocument();
  await tick(3999); expect(screen.getByText('Workbook saved; opening request accepted.')).toBeInTheDocument();
  await tick(1); expect(screen.queryByRole('complementary')).not.toBeInTheDocument();
});

it('rejects dismissal captured from an obsolete feedback revision', () => {
  render(<AnalyticalSaveStatus />); const state = renderHook(useAnalyticalExportStatus); const id = start();
  const old = state.result.current.operations.find(o => o.id === id)!;
  act(() => recordAnalyticalSave(id, saved())); act(() => dismissAnalyticalSave(id, old));
  expect(screen.getByText('Workbook saved.')).toBeInTheDocument(); expect(state.result.current.operations.find(o => o.id === id)?.dismissed).not.toBe(true);
});

it('defers dismissal while focus is inside, then dismisses after focus leaves', async () => {
  render(<><AnalyticalSaveStatus /><button>Outside feedback</button></>); start(saved());
  const dismiss = screen.getByRole('button', { name: 'Dismiss' }); act(() => dismiss.focus());
  await tick(16000); expect(dismiss).toHaveFocus(); expect(screen.getByText('Workbook saved.')).toBeInTheDocument();
  await act(async () => screen.getByRole('button', { name: 'Outside feedback' }).focus());
  expect(screen.queryByText('Workbook saved.')).not.toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Outside feedback' })).toHaveFocus();
});

it('cancels obsolete timers on unmount and leaves recorded results untouched', async () => {
  const view = render(<AnalyticalSaveStatus />); const state = renderHook(useAnalyticalExportStatus); const id = start(saved());
  await tick(4000); view.unmount(); expect(vi.getTimerCount()).toBe(0); await tick(16000);
  expect(state.result.current.operations.find(o => o.id === id)?.dismissed).not.toBe(true);
  render(<AnalyticalSaveStatus />); await tick(7999); expect(screen.getByText('Workbook saved.')).toBeInTheDocument();
  await tick(1); expect(screen.queryByText('Workbook saved.')).not.toBeInTheDocument();
});

it('manual dismissal still hides ordinary feedback immediately and retains its result', () => {
  render(<AnalyticalSaveStatus />); const state = renderHook(useAnalyticalExportStatus); const id = start(saved());
  act(() => screen.getByRole('button', { name: 'Dismiss' }).click());
  expect(screen.queryByRole('complementary')).not.toBeInTheDocument(); expect(state.result.current.operations.find(o => o.id === id)).toMatchObject({ dismissed: true, result: saved() });
  expect(vi.getTimerCount()).toBe(0);
});

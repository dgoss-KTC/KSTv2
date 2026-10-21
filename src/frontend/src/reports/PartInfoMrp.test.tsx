import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PartInfoLookup } from './PartInfoLookup';
import { PartInfoMrp } from './PartInfoMrp';
import { defaultMrpSettings } from './partInfoMrpContext';
import { mrpFixture, partInfoFixture } from './partInfoMrpTestSupport';
import { deferred } from './reportTestSupport';
import type { ComponentMrpScreen } from '../api/componentMrpApi';
const loadSpec = vi.fn(async () => ({ sites: ['SW', 'KV'], maximumPartLength: 256 }));
beforeEach(() => localStorage.clear());
afterEach(() => vi.useRealTimers());
async function lookup() {
  await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled());
  fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: ' ABC ' } });
  fireEvent.click(screen.getByText('Lookup'));
}
describe('Part Info independent MRP lifecycle', () => {
  it('starts after successful lookup, isolates optional failures, preserves tabs and renders exact UOM values', async () => {
    const info = deferred<ReturnType<typeof partInfoFixture>>(); const loadMrp = vi.fn(async request => mrpFixture(request)); const context = vi.fn();
    render(<PartInfoLookup loadSpec={loadSpec} lookup={() => info.promise} loadMrp={loadMrp} onExportContext={context} />);
    await lookup(); expect(screen.getAllByRole('status')).toHaveLength(1); expect(loadMrp).not.toHaveBeenCalled();
    const facts = partInfoFixture(); facts.orders = { value: null, error: 'PO unavailable', acquiredAtUtc: null };
    await act(async () => info.resolve(facts)); const table = await screen.findByRole('table', { name: 'Component MRP periods' });
    expect(screen.getByText('PO unavailable')).toBeInTheDocument(); expect(screen.getByText('Synthetic facts')).toBeInTheDocument();
    expect(screen.getAllByRole('tabpanel', { hidden: true })).toHaveLength(3); expect(screen.getAllByRole('tablist')).toHaveLength(1);
    expect(within(table).getAllByRole('rowheader').map(h => h.textContent)).toEqual(['Gross Requirements', 'Scheduled Receipts', 'Projected QOH', 'Planned Orders Due', 'Planned Orders Release']);
    expect(within(table).getByRole('columnheader', { name: 'Past Due' })).toBeInTheDocument(); expect(within(table).getByRole('columnheader', { name: '2026-09-28' })).toBeInTheDocument();
    const negative = within(table).getByText('-1'); expect(negative).toHaveClass('part-info-mrp__negative');
    expect(within(within(table).getByRole('row', { name: /^Gross Requirements/ })).getAllByRole('cell').map(c => c.textContent)).toEqual(['2', '0.01']);
    expect(within(within(table).getByRole('row', { name: /^Projected QOH/ })).getAllByRole('cell').map(c => c.textContent)).toEqual(['-1', '2']);
    expect(within(table).getAllByText(/includes unconfirmed receipts/)).toHaveLength(2);
    expect(screen.getByRole('region', { name: 'Scrollable Component MRP periods' })).toHaveAttribute('tabindex', '0');
    expect(context).toHaveBeenLastCalledWith(expect.objectContaining({ state: 'ready', request: expect.objectContaining({ part: 'ABC' }) }));
  });
  it('retains actual old grid identity while loading, coalesces controls and rejects late replies', async () => {
    const late = deferred<ComponentMrpScreen>(); const latest = deferred<ComponentMrpScreen>();
    const loadMrp = vi.fn().mockImplementationOnce(async request => mrpFixture(request)).mockReturnValueOnce(late.promise).mockReturnValueOnce(latest.promise);
    const context = vi.fn(); render(<PartInfoLookup loadSpec={loadSpec} lookup={vi.fn(async () => partInfoFixture())} loadMrp={loadMrp} onExportContext={context} />);
    await lookup(); await screen.findByRole('table', { name: 'Component MRP periods' });
    fireEvent.click(screen.getByRole('button', { name: 'Day' })); await waitFor(() => expect(loadMrp).toHaveBeenCalledTimes(2));
    expect(screen.getByText('26 weeks by Week')).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Month' })).toBeEnabled();
    expect(context).toHaveBeenLastCalledWith(expect.objectContaining({ state: 'loading' }));
    fireEvent.click(screen.getByRole('button', { name: '13' })); fireEvent.click(screen.getByRole('button', { name: '52' })); fireEvent.click(screen.getByRole('button', { name: 'Month' }));
    fireEvent.click(screen.getByLabelText('Include Unconfirmed POs')); await waitFor(() => expect(loadMrp).toHaveBeenCalledTimes(3));
    expect(loadMrp.mock.calls[1][1].aborted).toBe(true);
    expect(loadMrp.mock.calls[2][0]).toEqual({ site: 'SW', part: 'ABC', horizonWeeks: 52, period: 'Month', includeUnconfirmed: false });
    await act(async () => latest.resolve(mrpFixture(loadMrp.mock.calls[2][0]))); await act(async () => late.resolve(mrpFixture({ site: 'SW', part: 'ABC', horizonWeeks: 26, includeUnconfirmed: true, period: 'Day' })));
    expect(screen.getByText('52 weeks by Month')).toBeInTheDocument(); expect(screen.getByRole('columnheader', { name: '2026-09 *' })).toBeInTheDocument();
    expect(screen.queryByText('† includes unconfirmed PO receipts')).toBeNull(); expect(screen.queryByText('26 weeks by Day')).toBeNull();
  });
  it('regroups and selects receipt mode using retained identity, full refresh preserves settings, inventory does not refresh MRP, new lookup resets', async () => {
    const loadMrp = vi.fn(async request => mrpFixture(request)); const refreshLocations = vi.fn(async (site, part) => ({ site, part, locations: { value: [], acquiredAtUtc: null, error: null } }));
    render(<PartInfoLookup loadSpec={loadSpec} lookup={vi.fn(async () => partInfoFixture())} loadMrp={loadMrp} refreshLocations={refreshLocations} />);
    await lookup(); await screen.findByRole('table', { name: 'Component MRP periods' });
    fireEvent.click(screen.getByRole('button', { name: 'Month' })); fireEvent.click(screen.getByLabelText('Include Unconfirmed POs'));
    await waitFor(() => expect(screen.getByText('26 weeks by Month')).toBeInTheDocument());
    expect(loadMrp.mock.calls[1][0]).toMatchObject({ acquisitionId: '11111111-1111-4111-8111-111111111111', asOfDate: '2026-09-29', includeUnconfirmed: false });
    fireEvent.click(screen.getByRole('tab', { name: /Inventory lots/ })); fireEvent.click(screen.getByText('Refresh inventory'));
    await waitFor(() => expect(refreshLocations).toHaveBeenCalledTimes(1)); expect(loadMrp).toHaveBeenCalledTimes(2);
    fireEvent.click(screen.getByRole('button', { name: 'Refresh / Retry sections' })); await waitFor(() => expect(loadMrp).toHaveBeenCalledTimes(3));
    expect(loadMrp.mock.calls[2][0]).toEqual({ site: 'SW', part: 'ABC', horizonWeeks: 26, period: 'Month', includeUnconfirmed: false });
    await screen.findByText('26 weeks by Month'); fireEvent.click(screen.getByText('Lookup')); await waitFor(() => expect(loadMrp).toHaveBeenCalledTimes(4));
    expect(loadMrp.mock.calls[3][0]).toEqual({ site: 'SW', part: 'ABC', horizonWeeks: 26, period: 'Week', includeUnconfirmed: true });
  });
  it('MRP error/mismatch and retry never erase facts; edits, departure and unmount clear context and abort late work', async () => {
    const late = deferred<ComponentMrpScreen>(); const context = vi.fn();
    const loadMrp = vi.fn().mockRejectedValueOnce(new Error('MRP offline')).mockImplementationOnce(async request => ({ ...mrpFixture(request), part: 'OTHER' })).mockReturnValue(late.promise);
    const props = { loadSpec, lookup: vi.fn(async () => partInfoFixture()), loadMrp, onExportContext: context }; const view = render(<PartInfoLookup {...props} />);
    await lookup(); expect(await screen.findByText('MRP offline')).toBeInTheDocument(); expect(screen.getByText('Synthetic facts')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Retry MRP')); await screen.findByText('MRP identity/settings mismatch. Retry MRP.');
    fireEvent.click(screen.getByText('Retry MRP')); await waitFor(() => expect(loadMrp).toHaveBeenCalledTimes(3));
    fireEvent.change(screen.getByLabelText('Part Number'), { target: { value: 'OTHER' } }); expect(loadMrp.mock.calls[2][1].aborted).toBe(true); expect(context).toHaveBeenLastCalledWith(null);
    await act(async () => late.resolve(mrpFixture())); expect(screen.queryByRole('table', { name: 'Component MRP periods' })).toBeNull();
    view.rerender(<PartInfoLookup {...props} active={false} />); view.unmount(); expect(context).toHaveBeenLastCalledWith(null);
  });
  it('retrying a failed full refresh preserves MRP settings rather than turning the retry into a new lookup', async () => {
    const loadMrp = vi.fn(async request => mrpFixture(request));
    const acquire = vi.fn().mockResolvedValueOnce(partInfoFixture()).mockRejectedValueOnce(new Error('Master temporarily unavailable')).mockResolvedValue(partInfoFixture());
    render(<PartInfoLookup loadSpec={loadSpec} lookup={acquire} loadMrp={loadMrp} />);
    await lookup(); await screen.findByRole('table', { name: 'Component MRP periods' });
    fireEvent.click(screen.getByRole('button', { name: 'Month' })); fireEvent.click(screen.getByLabelText('Include Unconfirmed POs'));
    await screen.findByText('26 weeks by Month'); fireEvent.click(screen.getByRole('button', { name: 'Refresh / Retry sections' }));
    await screen.findByText('Master temporarily unavailable'); fireEvent.click(screen.getByText('Retry lookup'));
    await waitFor(() => expect(loadMrp).toHaveBeenCalledTimes(3));
    expect(loadMrp.mock.calls[2][0]).toEqual({ site: 'SW', part: 'ABC', horizonWeeks: 26, period: 'Month', includeUnconfirmed: false });
    await screen.findByText('26 weeks by Month');
  });
  it.each(['KV', 'KS', 'unknown'])('validates saved site %s against the acquired lookup spec', async saved => {
    localStorage.setItem('kst.componentMrp.site.v1', saved); const loadMrp = vi.fn(); render(<PartInfoLookup loadSpec={loadSpec} loadMrp={loadMrp} />);
    await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled()); expect(screen.getByLabelText('Site')).toHaveValue(saved === 'KV' ? 'KV' : 'SW'); expect(loadMrp).not.toHaveBeenCalled();
  });
  it('requires explicit refreshed projection after expiry, without an export-only acquisition', async () => {
    vi.useFakeTimers(); vi.setSystemTime(new Date('2026-09-29T12:00Z'));
    const context = vi.fn(); const loadMrp = vi.fn(async request => ({ ...mrpFixture(request), expiresAtUtc: new Date(Date.now() + 1000).toISOString() }));
    render(<PartInfoMrp site="SW" part="ABC" settings={defaultMrpSettings} onSettings={vi.fn()} onExportContext={context} loadMrp={loadMrp} />);
    await act(async () => vi.advanceTimersByTimeAsync(150)); expect(context).toHaveBeenLastCalledWith(expect.objectContaining({ state: 'ready', expired: false }));
    await act(async () => vi.advanceTimersByTimeAsync(1000)); expect(context).toHaveBeenLastCalledWith(expect.objectContaining({ state: 'ready', expired: true })); expect(loadMrp).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByText('Refresh MRP')); expect(context).toHaveBeenLastCalledWith(expect.objectContaining({ state: 'loading' }));
    await act(async () => vi.advanceTimersByTimeAsync(150)); expect(loadMrp.mock.calls[1][0].acquisitionId).toBeUndefined(); expect(context).toHaveBeenLastCalledWith(expect.objectContaining({ state: 'ready', expired: false }));
  });
});

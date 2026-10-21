import { StrictMode } from 'react';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PartInfoLookup } from './PartInfoLookup';
import { mrpFixture, partInfoFixture } from './partInfoMrpTestSupport';
import { deferred } from './reportTestSupport';
import type { PartInfoShortcut } from './partInfoShortcut';
import type { lookupPartInfo } from '../api/partInfoApi';
import type { readComponentMrp } from '../api/componentMrpApi';

const specification = { sites: ['SW', 'NW', 'KV'], maximumPartLength: 256 };
function request(token = 1, part = 'BOM-COMP', site = 'NW'): PartInfoShortcut {
  let consumed = false;
  return { token, part, site, claim: vi.fn(() => { if (consumed) return false; consumed = true; return true; }) };
}
function ports() {
  return { loadSpec: vi.fn(async () => specification), lookup: vi.fn<typeof lookupPartInfo>(async (site, part) => partInfoFixture(part, site)),
    loadMrp: vi.fn<typeof readComponentMrp>(async input => mrpFixture(input)) };
}
beforeEach(() => { localStorage.clear(); localStorage.setItem('kst.componentMrp.site.v1', 'KV'); });

describe('one-use BOM lookup request through the normal lookup/MRP sequence', () => {
  it('retries failed specification through the existing control and applies only the newest waiting request', async () => {
    const pending = deferred<typeof specification>(); const api = ports();
    api.loadSpec.mockRejectedValueOnce(new Error('settings offline')).mockReturnValueOnce(pending.promise);
    const view = render(<PartInfoLookup {...api} shortcut={request()} />);
    await screen.findByText('Lookup settings unavailable.'); expect(api.lookup).not.toHaveBeenCalled();
    fireEvent.click(screen.getByText('Retry settings')); const newest = request(2, 'LATEST-COMP', 'SW');
    view.rerender(<PartInfoLookup {...api} shortcut={newest} />);
    await act(async () => pending.resolve(specification)); await screen.findByRole('heading', { name: 'LATEST-COMP' });
    expect(api.lookup).toHaveBeenCalledExactlyOnceWith('SW', 'LATEST-COMP', expect.any(AbortSignal));
    expect(screen.getByLabelText('Site')).toHaveValue('SW');
  });
  it('waits for delayed specification, overrides saved site, submits exactly once under StrictMode/rerenders', async () => {
    const pending = deferred<typeof specification>(); const api = ports(); api.loadSpec.mockReturnValue(pending.promise);
    const shortcut = request(); const view = render(<StrictMode><PartInfoLookup {...api} shortcut={shortcut} /></StrictMode>);
    view.rerender(<StrictMode><PartInfoLookup {...api} shortcut={shortcut} /></StrictMode>);
    expect(api.lookup).not.toHaveBeenCalled(); expect(api.loadMrp).not.toHaveBeenCalled();
    await act(async () => pending.resolve(specification));
    await screen.findByRole('table', { name: 'Component MRP periods' });
    expect(screen.getByLabelText('Part Number')).toHaveValue('BOM-COMP'); expect(screen.getByLabelText('Site')).toHaveValue('NW');
    expect(api.lookup).toHaveBeenCalledExactlyOnceWith('NW', 'BOM-COMP', expect.any(AbortSignal));
    expect(api.loadMrp).toHaveBeenCalledExactlyOnceWith({ site: 'NW', part: 'BOM-COMP', horizonWeeks: 26, period: 'Week', includeUnconfirmed: true }, expect.any(AbortSignal));
    view.rerender(<StrictMode><PartInfoLookup {...api} shortcut={shortcut} /></StrictMode>);
    expect(api.lookup).toHaveBeenCalledTimes(1);
  });
  it('treats repeated same-part visits as new lookups and resets all previous MRP controls', async () => {
    const api = ports(); const first = request(); const view = render(<PartInfoLookup {...api} shortcut={first} />);
    await screen.findByRole('table', { name: 'Component MRP periods' });
    fireEvent.click(screen.getByRole('button', { name: '72' })); fireEvent.click(screen.getByRole('button', { name: 'Day' }));
    fireEvent.click(screen.getByLabelText('Include Unconfirmed POs'));
    await screen.findByText('72 weeks by Day');
    api.loadMrp.mockClear(); const next = request(2);
    view.rerender(<PartInfoLookup {...api} shortcut={next} />);
    await waitFor(() => expect(api.lookup).toHaveBeenCalledTimes(2));
    await screen.findByText('26 weeks by Week');
    expect(api.loadMrp).toHaveBeenCalledExactlyOnceWith({ site: 'NW', part: 'BOM-COMP', horizonWeeks: 26, period: 'Week', includeUnconfirmed: true }, expect.any(AbortSignal));
    view.unmount(); render(<PartInfoLookup {...api} shortcut={next} />);
    await waitFor(() => expect(api.loadSpec).toHaveBeenCalledTimes(2));
    expect(api.lookup).toHaveBeenCalledTimes(2); // remount is not a new navigation token
  });
  it('does not replay a request after departure before specification completes', async () => {
    const pending = deferred<typeof specification>(); const api = ports(); api.loadSpec.mockReturnValue(pending.promise);
    const shortcut = request(); const view = render(<PartInfoLookup {...api} shortcut={shortcut} />);
    view.rerender(<PartInfoLookup {...api} shortcut={null} active={false} />);
    await act(async () => pending.resolve(specification)); expect(api.lookup).not.toHaveBeenCalled();
    view.rerender(<PartInfoLookup {...api} shortcut={null} />);
    await waitFor(() => expect(screen.getByLabelText('Site')).toBeEnabled()); expect(api.lookup).not.toHaveBeenCalled();
  });
  it('uses normal identity rejection/Retry without resubmitting automatically or starting MRP on a mismatch', async () => {
    const api = ports(); api.lookup.mockResolvedValueOnce(partInfoFixture('WRONG', 'NW'));
    render(<PartInfoLookup {...api} shortcut={request()} />);
    await screen.findByText('Lookup identity mismatch. Please retry.'); expect(api.lookup).toHaveBeenCalledTimes(1); expect(api.loadMrp).not.toHaveBeenCalled();
    fireEvent.click(screen.getByText('Retry lookup')); await screen.findByRole('table', { name: 'Component MRP periods' });
    expect(api.lookup).toHaveBeenLastCalledWith('NW', 'BOM-COMP', expect.any(AbortSignal)); expect(api.lookup).toHaveBeenCalledTimes(2);
  });
  it('uses normal unsupported-site validation instead of falling back to the preference', async () => {
    const api = ports(); render(<PartInfoLookup {...api} shortcut={request(1, 'BOM-COMP', 'KS')} />);
    await screen.findByText('Select a supported site and a valid part number.'); expect(api.lookup).not.toHaveBeenCalled(); expect(api.loadMrp).not.toHaveBeenCalled();
  });
  it('rejects late replies after departure and superseded identity', async () => {
    const pending = deferred<ReturnType<typeof partInfoFixture>>(); const api = ports(); api.lookup.mockReturnValueOnce(pending.promise);
    const view = render(<PartInfoLookup {...api} shortcut={request()} />);
    await waitFor(() => expect(api.lookup).toHaveBeenCalledTimes(1));
    view.rerender(<PartInfoLookup {...api} active={false} />);
    expect(api.lookup.mock.calls[0][2]?.aborted).toBe(true);
    await act(async () => pending.resolve(partInfoFixture('BOM-COMP', 'NW'))); expect(api.loadMrp).not.toHaveBeenCalled();
    view.rerender(<PartInfoLookup {...api} shortcut={request(2, 'NEW-COMP', 'SW')} />);
    await screen.findByRole('heading', { name: 'NEW-COMP' }); expect(api.lookup).toHaveBeenLastCalledWith('SW', 'NEW-COMP', expect.any(AbortSignal));
  });
});

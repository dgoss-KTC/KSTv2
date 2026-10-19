import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ApiError, type FinishedGoodsResponseDto } from '../api/client';
import { FinishedGoodsPanel } from './FinishedGoodsPanel';

const get = vi.fn(); const refresh = vi.fn();
vi.mock('../api/tauri-bridge', () => ({ resolveBackendBaseUrl: () => Promise.resolve('http://offline.test') }));
vi.mock('../api/client', async original => ({ ...(await original<typeof import('../api/client')>()),
  ApiClient: class { getFinishedGoods = get; refreshFinishedGoods = refresh; },
}));

function response(assignmentId = 'A', snapshot = 'mps1', count = 2): FinishedGoodsResponseDto {
  return { workspaceId: assignmentId, site: 'SW', mpsSnapshotId: snapshot, finishedGoodsSnapshotId: 'fg1',
    acquiredAtUtc: '2026-10-07T12:00:00Z', currency: 'USD', isStale: false, warning: null,
    rows: Array.from({ length: count }, (_, i) => ({ rowId: `fg:${i}`, domain: 'KTC', product: `P-${i}`, sourceLocation: 'ship', location: 'SHIP',
      lot: 'ra1', reference: `R${i}`, onHand: '3.00001', unitPrice: '1.23456', value: '3.7036923456', isMissingPrice: false, isRma: true })) };
}
beforeEach(() => { get.mockReset(); refresh.mockReset(); localStorage.clear(); });
afterEach(() => vi.restoreAllMocks());

async function applyContains(column: string, text: string) {
  fireEvent.click(screen.getByRole('button', { name: new RegExp(`^Filter ${column}`) }));
  const dialog = screen.getByRole('dialog', { name: `Filter ${column}` });
  fireEvent.click(within(dialog).getByRole('button', { name: 'Contains' }));
  fireEvent.change(within(dialog).getByLabelText(`${column} contains`), { target: { value: text } });
  fireEvent.click(within(dialog).getByRole('button', { name: 'Apply' }));
}

describe('Finished Goods panel (mocked only)', () => {
  it('renders exactly five columns, RMA and exact rounded USD without changing raw values', async () => {
    const data = response(); data.rows[1] = { ...data.rows[0], rowId: 'distinct', reference: 'other' };
    get.mockResolvedValue(data);
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    const table = await screen.findByRole('table');
    expect(within(table).getAllByRole('columnheader').map(h => h.textContent)).toEqual(['Product', 'Location', 'Lot', 'On Hand', 'Value']);
    expect(within(table).getAllByText('$3.70')).toHaveLength(2);
    expect(within(table).getAllByText('3.00001')).toHaveLength(2);
    expect(within(table).getAllByText('RMA')).toHaveLength(2);
    expect(screen.getAllByTitle('USD 3.7036923456')).toHaveLength(2);
    const physicalRows = within(table).getAllByRole('row').slice(1);
    expect(physicalRows.map(row => row.querySelector('td')?.title)).toEqual(['Reference: other', 'Reference: R0']);
    await applyContains('Product', 'P-');
    await applyContains('Location', 'sh');
    await applyContains('Lot', 'RA');
    expect(screen.getAllByRole('button', { name: /filter active/ })).toHaveLength(3);
    expect(within(table).getAllByRole('row').slice(1)).toEqual(physicalRows);
    expect(get).toHaveBeenCalledTimes(1); expect(refresh).not.toHaveBeenCalled();
  });

  it('filters the full snapshot beyond DOM pages and does not query sources', async () => {
    get.mockResolvedValue(response('A', 'mps1', 150));
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    const table = await screen.findByRole('table'); expect(within(table).getAllByRole('row')).toHaveLength(101);
    await applyContains('Product', 'P-149');
    expect(within(table).getAllByRole('row')).toHaveLength(2); expect(screen.getByText('P-149')).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1);
  });

  it('keeps original acquisition time on stale refresh and replaces it on success', async () => {
    const first = response(); get.mockResolvedValue(first);
    refresh.mockResolvedValueOnce({ ...first, isStale: true, warning: 'Showing last known inventory; refresh failed.' })
      .mockResolvedValueOnce({ ...first, finishedGoodsSnapshotId: 'fg2', acquiredAtUtc: '2026-10-07T13:00:00Z' });
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByRole('table'); const metadata = screen.getByText(/Acquired:/).textContent;
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Finished Goods' }));
    await screen.findByRole('alert'); expect(screen.getByText(/Acquired:/).textContent).toBe(metadata);
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Finished Goods' }));
    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument());
    expect(screen.getByText(/Acquired:/).textContent).not.toBe(metadata);
  });

  it('distinguishes empty, unavailable and scope-conflict results without displaying source errors', async () => {
    get.mockRejectedValueOnce(new ApiError(503, '', 'secret SQL error')).mockResolvedValueOnce(response('B', 'mps1', 0));
    const { rerender } = render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByRole('alert'); expect(screen.queryByRole('table')).not.toBeInTheDocument();
    expect(screen.queryByText(/secret/)).not.toBeInTheDocument();
    rerender(<FinishedGoodsPanel assignmentId="B" snapshotId="mps1" site="SW" />);
    await screen.findByText('No positive on-hand inventory in this workspace scope.');
    refresh.mockRejectedValue(new ApiError(409, '', 'private conflict'));
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Finished Goods' }));
    await screen.findByText(/Workspace scope changed/); expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('rejects old A→B→A and replaced-snapshot responses even when a request cannot be cancelled', async () => {
    let finishOld!: (value: FinishedGoodsResponseDto) => void;
    get.mockImplementationOnce(() => new Promise(resolve => { finishOld = resolve; }))
      .mockResolvedValueOnce(response('B')).mockResolvedValueOnce({ ...response(), rows: [{ ...response().rows[0], product: 'CURRENT' }] })
      .mockResolvedValueOnce({ ...response('A', 'mps2'), rows: [{ ...response().rows[0], product: 'NEW-SNAPSHOT' }] });
    const { rerender } = render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await waitFor(() => expect(get).toHaveBeenCalledTimes(1));
    rerender(<FinishedGoodsPanel assignmentId="B" snapshotId="mps1" site="SW" />); await screen.findByRole('table');
    rerender(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />); await screen.findByText('CURRENT');
    await act(async () => finishOld(response())); expect(screen.queryByText('P-0')).not.toBeInTheDocument();
    rerender(<FinishedGoodsPanel assignmentId="A" snapshotId="mps2" site="SW" />); await screen.findByText('NEW-SNAPSHOT');
    expect(screen.queryByText('CURRENT')).not.toBeInTheDocument();
  });

  it('marks retained rows stale on transport failure without replacing their acquisition time', async () => {
    get.mockResolvedValue(response()); refresh.mockRejectedValue(new Error('private network details'));
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByRole('table'); const metadata = screen.getByText(/Acquired:/).textContent;
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Finished Goods' }));
    await screen.findByText(/Showing the last known Finished Goods inventory/);
    expect(screen.getByText(/Acquired:/).textContent).toBe(metadata);
    expect(screen.getByRole('table')).toBeInTheDocument();
    expect(screen.queryByText(/private network/)).not.toBeInTheDocument();
  });

  it('ignores a pending refresh after its MPS snapshot is replaced', async () => {
    let finish!: (value: FinishedGoodsResponseDto) => void;
    get.mockResolvedValueOnce(response()).mockResolvedValueOnce({ ...response('A', 'mps2'), rows: [] });
    refresh.mockImplementation(() => new Promise(resolve => { finish = resolve; }));
    const { rerender } = render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByRole('table'); fireEvent.click(screen.getByRole('button', { name: 'Refresh Finished Goods' }));
    await waitFor(() => expect(refresh).toHaveBeenCalledTimes(1));
    rerender(<FinishedGoodsPanel assignmentId="A" snapshotId="mps2" site="SW" />);
    await screen.findByText('No positive on-hand inventory in this workspace scope.');
    await act(async () => finish(response())); expect(screen.queryByText('P-0')).not.toBeInTheDocument();
  });

  it('offers all sizes, preserves the former first row within the new page and keeps counts accurate', async () => {
    get.mockResolvedValue(response('A', 'mps1', 1205));
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    const table = await screen.findByRole('table');
    const sizes = screen.getByRole('group', { name: 'Rows per page' });
    expect(within(sizes).getAllByRole('button').map(b => b.textContent)).toEqual(['100', '500', '1000']);
    expect(within(sizes).getByRole('button', { name: '100', pressed: true })).toBeInTheDocument();
    for (let i = 0; i < 6; i++) fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    expect(screen.getByText('Rows 601–700 of 1,205')).toBeInTheDocument();
    const formerFirst = within(table).getAllByRole('row')[1].textContent;
    fireEvent.click(within(sizes).getByRole('button', { name: '500' }));
    expect(screen.getByText('Rows 501–1,000 of 1,205')).toBeInTheDocument();
    expect(within(table).getAllByRole('row').some(r => r.textContent === formerFirst)).toBe(true);
    fireEvent.click(within(sizes).getByRole('button', { name: '1000' }));
    expect(screen.getByText('Rows 1–1,000 of 1,205')).toBeInTheDocument();
    expect(within(table).getAllByRole('row')).toHaveLength(1001);
    fireEvent.click(screen.getByRole('button', { name: 'Last' }));
    expect(screen.getByText('Rows 1,001–1,205 of 1,205')).toBeInTheDocument();
    fireEvent.click(within(sizes).getByRole('button', { name: '100' }));
    expect(screen.getByText('Rows 1,001–1,100 of 1,205')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'First' }));
    expect(screen.getByText('Page 1 of 13')).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1); expect(refresh).not.toHaveBeenCalled();
    expect(localStorage.getItem('kst.finishedGoods.pageSize.v1')).toBe('100');
  });

  it('resets later pages for filters, clear-all and acquisition, including empty and shrinking reports', async () => {
    get.mockResolvedValue(response('A', 'mps1', 1205));
    refresh.mockResolvedValue(response('A', 'mps1', 1));
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'Last' }));
    await applyContains('Product', 'P-1204');
    expect(screen.getByText('Rows 1–1 of 1 · filtered from 1,205')).toBeInTheDocument();
    expect(screen.getByText('Page 1 of 1')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear filters (1)' }));
    expect(screen.getByText('Page 1 of 13')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Last' }));
    await applyContains('Product', 'absent');
    expect(screen.getByText('Rows 0 of 0 · filtered from 1,205')).toBeInTheDocument();
    for (const name of ['First', 'Previous', 'Next', 'Last']) expect(screen.getByRole('button', { name })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Clear filters (1)' }));
    fireEvent.click(screen.getByRole('button', { name: 'Last' }));
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Finished Goods' }));
    await screen.findByText('Rows 1–1 of 1');
    expect(screen.getByText('Page 1 of 1')).toBeInTheDocument();
  });

  it('keeps size separate from visit-local filters, survives throwing storage and supports keyboard size selection', async () => {
    const read = vi.spyOn(localStorage, 'getItem').mockImplementation(() => { throw new Error('denied'); });
    const write = vi.spyOn(localStorage, 'setItem').mockImplementation(() => { throw new Error('denied'); });
    get.mockResolvedValue(response());
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByRole('table'); const user = userEvent.setup();
    const sizes = screen.getByRole('group', { name: 'Rows per page' });
    expect(within(sizes).getByRole('button', { name: '100', pressed: true })).toBeInTheDocument();
    await applyContains('Lot', 'ra');
    const larger = within(sizes).getByRole('button', { name: '500' }); larger.focus(); await user.keyboard('{Enter}');
    expect(larger).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Filter Lot, filter active' })).toBeInTheDocument();
    expect(write).toHaveBeenCalledWith('kst.finishedGoods.pageSize.v1', '500'); expect(read).toHaveBeenCalled();
    expect(get).toHaveBeenCalledTimes(1);
  });

  it('persists size across workspace visits but never stores or restores filters', async () => {
    get.mockResolvedValueOnce(response()).mockResolvedValueOnce(response('B'));
    const { rerender } = render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByRole('table'); await applyContains('Lot', 'ra');
    fireEvent.click(screen.getByRole('button', { name: '1000' }));
    rerender(<FinishedGoodsPanel assignmentId="B" snapshotId="mps1" site="SW" />);
    await screen.findByRole('table');
    expect(screen.getByRole('button', { name: '1000', pressed: true })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /filter active/ })).not.toBeInTheDocument();
    expect(localStorage.length).toBe(1);
  });

  it('cascades full-report counts, preserves blanks and supports explicit Apply/Cancel/Clear', async () => {
    const data = response('A', 'mps1', 150);
    data.rows.forEach((row, i) => { row.location = i < 140 ? 'SHIP' : 'DOCK'; row.lot = i % 2 ? '' : null; });
    data.rows[149].lot = '(Blanks)'; get.mockResolvedValue(data);
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByRole('table'); await applyContains('Location', 'dock');
    fireEvent.click(screen.getByRole('button', { name: 'Filter Lot' }));
    const dialog = screen.getByRole('dialog', { name: 'Filter Lot' });
    const caption = await within(dialog).findByLabelText('Value: (Blanks)');
    expect(caption.closest('.shipment-filter__value-row')).toHaveTextContent('1');
    const blanks = within(dialog).getByLabelText('(Blanks)');
    expect(blanks.closest('.shipment-filter__value-row')).toHaveTextContent('9');
    fireEvent.click(within(dialog).getByLabelText('(Select all)')); fireEvent.click(blanks);
    // Unapplied selections never change the loaded result.
    expect(screen.getByText('Rows 1–10 of 10 · filtered from 150')).toBeInTheDocument();
    fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }));
    expect(screen.queryByRole('button', { name: 'Filter Lot, filter active' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Filter Lot' }));
    fireEvent.click(screen.getByLabelText('(Select all)')); fireEvent.click(screen.getByLabelText('(Blanks)'));
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(screen.getByText('Rows 1–9 of 9 · filtered from 150')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Filter Lot, filter active' }));
    fireEvent.click(screen.getByRole('button', { name: 'Clear filter' }));
    expect(screen.getByText('Rows 1–10 of 10 · filtered from 150')).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1); expect(refresh).not.toHaveBeenCalled();
  });

  it('filters On Hand and Value using raw exact decimals with identical displayed USD values', async () => {
    const data = response(); data.rows[1].value = '3.7036923457'; data.rows[1].onHand = '3.00002'; get.mockResolvedValue(data);
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    const table = await screen.findByRole('table'); expect(within(table).getAllByText('$3.70')).toHaveLength(2);
    fireEvent.click(screen.getByRole('button', { name: 'Filter Value' }));
    fireEvent.change(screen.getByLabelText('Condition'), { target: { value: 'gt' } });
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '3.7036923456' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(within(table).getAllByText('$3.70')).toHaveLength(1); expect(screen.getByText('P-1')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Filter On Hand' }));
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '3.00001' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(screen.getByText('No inventory matches these filters.')).toBeInTheDocument();
    expect(get).toHaveBeenCalledTimes(1);
  });

  it('contains focus, portals outside scrolling, discards drafts on Escape/outside and restores the funnel focus', async () => {
    get.mockResolvedValue(response());
    render(<div className="shell" data-theme="light"><FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" /></div>);
    await screen.findByRole('table'); const user = userEvent.setup();
    const opener = screen.getByRole('button', { name: 'Filter Product' }); opener.focus(); await user.keyboard('{Enter}');
    const dialog = screen.getByRole('dialog', { name: 'Filter Product' });
    expect(screen.getByRole('region', { name: 'Finished Goods table' })).not.toContainElement(dialog);
    expect(dialog.closest('.shell')).toHaveAttribute('data-theme', 'light');
    expect(dialog).toContainElement(document.activeElement as HTMLElement);
    const close = within(dialog).getByRole('button', { name: 'Close Filter Product' });
    close.focus(); await user.tab({ shift: true }); expect(within(dialog).getByRole('button', { name: 'Apply' })).toHaveFocus();
    await user.tab(); expect(close).toHaveFocus();
    fireEvent.click(within(dialog).getByRole('button', { name: 'Contains' }));
    fireEvent.change(screen.getByLabelText('Product contains'), { target: { value: 'unapplied' } });
    await user.keyboard('{Escape}'); expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(opener).toHaveFocus();
    fireEvent.click(opener); fireEvent.pointerDown(document.body);
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(opener).toHaveFocus();
    expect(screen.queryByRole('button', { name: /filter active/ })).not.toBeInTheDocument();
  });

  it.each(['workspaceId', 'mpsSnapshotId', 'site', 'currency'] as const)('rejects mismatched %s on acquisition', async field => {
    get.mockResolvedValue({ ...response(), [field]: 'WRONG' });
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByText(/Workspace scope changed/); expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('preserves filters on refresh, closes obsolete filter drafts and retains missing-price reference information', async () => {
    const data = response(); data.rows[0].isMissingPrice = true; data.rows[0].value = '0';
    get.mockResolvedValue(data); refresh.mockResolvedValue({ ...data, isStale: true, warning: 'Last good inventory' });
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    await screen.findByRole('table'); await applyContains('Product', 'P-0');
    expect(screen.getByTitle('Reference: R0')).toBeInTheDocument();
    expect(screen.getByTitle('USD 0 · No price records; valued at $0')).toHaveTextContent('$0.00');
    fireEvent.click(screen.getByRole('button', { name: 'Filter Location' }));
    // Refresh is normally reached after dismissing the focus-contained popup.
    fireEvent.keyDown(document, { key: 'Escape' });
    fireEvent.click(screen.getByRole('button', { name: 'Refresh Finished Goods' }));
    await screen.findByText('Last good inventory');
    expect(screen.getByRole('button', { name: 'Filter Product, filter active' })).toBeInTheDocument();
    expect(screen.getByText('Rows 1–1 of 1 · filtered from 2')).toBeInTheDocument();
  });

  it('does not acquire before MPS is loaded', () => {
    render(<FinishedGoodsPanel assignmentId="A" snapshotId={null} site="SW" />);
    expect(screen.getByText(/Load the workspace MPS first/)).toBeInTheDocument(); expect(get).not.toHaveBeenCalled();
  });
});

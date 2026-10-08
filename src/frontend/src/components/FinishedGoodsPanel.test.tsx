import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
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
beforeEach(() => { get.mockReset(); refresh.mockReset(); });

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
    fireEvent.change(screen.getByRole('textbox', { name: 'Product filter' }), { target: { value: 'P-' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Location filter' }), { target: { value: 'sh' } });
    fireEvent.change(screen.getByRole('textbox', { name: 'Lot filter' }), { target: { value: 'RA' } });
    expect(get).toHaveBeenCalledTimes(1); expect(refresh).not.toHaveBeenCalled();
  });

  it('filters the full snapshot beyond DOM pages and does not query sources', async () => {
    get.mockResolvedValue(response('A', 'mps1', 150));
    render(<FinishedGoodsPanel assignmentId="A" snapshotId="mps1" site="SW" />);
    const table = await screen.findByRole('table'); expect(within(table).getAllByRole('row')).toHaveLength(101);
    fireEvent.change(screen.getByRole('textbox', { name: 'Product filter' }), { target: { value: 'P-149' } });
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

  it('does not acquire before MPS is loaded', () => {
    render(<FinishedGoodsPanel assignmentId="A" snapshotId={null} site="SW" />);
    expect(screen.getByText(/Load the workspace MPS first/)).toBeInTheDocument(); expect(get).not.toHaveBeenCalled();
  });
});

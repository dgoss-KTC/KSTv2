import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { InventoryLocationsPanel } from './InventoryLocationsPanel';
import { fetchInventoryLocations } from '../api/inventoryLocationsApi';
import type { InventoryLocationsResponseDto } from '../api/client';

vi.mock('../api/inventoryLocationsApi', () => ({ fetchInventoryLocations: vi.fn() }));
const fetchRows = vi.mocked(fetchInventoryLocations);
const report = (overrides: Partial<InventoryLocationsResponseDto> = {}): InventoryLocationsResponseDto => ({
  workspaceId: 'ws', site: 'SW', componentPart: 'COMP', mpsSnapshotId: 'snap', acquiredAtUtc: '2026-10-08T12:00:00Z',
  isStale: false, warning: null, rows: [
    { rowId: '0', location: ' mRb ', lotSerial: 'RA123', status: 'HOLD', quantityOnHand: '-999999999999999999.1234567890' },
    { rowId: '1', location: ' mRb ', lotSerial: 'RA123', status: 'HOLD', quantityOnHand: '-999999999999999999.1234567890' },
    { rowId: '2', location: null, lotSerial: null, status: null, quantityOnHand: null },
    { rowId: '3', location: '', lotSerial: '', status: '', quantityOnHand: '0.0000000000' },
    { rowId: '4', location: 'Bin', lotSerial: 'serial', status: 'OK', quantityOnHand: '1.1234567890' },
  ], ...overrides,
});
const props = { assignmentId: 'ws', componentPart: 'COMP', snapshotId: 'snap', site: 'SW' };
beforeEach(() => vi.resetAllMocks());

describe('Inventory locations independent acquisition', () => {
  it('loads exact four-column physical rows including duplicates, nulls and signs', async () => {
    fetchRows.mockResolvedValue(report());
    render(<InventoryLocationsPanel {...props} />);
    expect(screen.getByRole('status')).toHaveTextContent('Loading');
    const table = await screen.findByRole('table');
    expect(within(table).getAllByRole('columnheader').map(c => c.textContent)).toEqual(['Location', 'Lot/Serial', 'Status', 'Qty on Hand']);
    expect(within(table).getAllByRole('row')).toHaveLength(6);
    expect(within(table).getAllByText('-999,999,999,999,999,999.123')).toHaveLength(2);
    expect(within(table).getAllByTitle('-999999999999999999.1234567890')).toHaveLength(2);
    expect(within(table).getAllByText('—')).toHaveLength(4);
    expect(within(table).getByText('0')).toBeInTheDocument();
    expect(table.querySelector('tbody td')?.textContent).toBe(' mRb ');
  });

  it('displays whole numbers without decimals and rounds fractions exactly to three places', async () => {
    const values = ['12.0000000000', '-12.0000', '1.2', '1.2345', '-1.2345', '9.9999', '-0.0001', null];
    fetchRows.mockResolvedValue(report({ rows: values.map((quantityOnHand, index) => ({
      rowId: String(index), location: 'Bin', lotSerial: 'Lot', status: 'OK', quantityOnHand,
    })) }));
    render(<InventoryLocationsPanel {...props} />);
    const table = await screen.findByRole('table');
    const quantities = Array.from(table.querySelectorAll('tbody tr')).map(row => row.lastElementChild!);
    expect(quantities.map(cell => cell.textContent)).toEqual(['12', '-12', '1.200', '1.235', '-1.235', '10.000', '0.000', '—']);
    expect(quantities.map(cell => cell.getAttribute('title'))).toEqual(values);
  });

  it('distinguishes empty and failure and retries only inventory', async () => {
    fetchRows.mockRejectedValueOnce(new Error('private')).mockResolvedValueOnce(report({ rows: [] }));
    render(<InventoryLocationsPanel {...props} />);
    expect(await screen.findByRole('alert')).toHaveTextContent('could not be loaded');
    expect(screen.queryByText('No inventory locations found.')).not.toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Retry inventory' }));
    expect(await screen.findByText('No inventory locations found.')).toBeInTheDocument();
    expect(fetchRows).toHaveBeenCalledTimes(2);
  });

  it('keeps server-compatible stale rows with original acquisition time and recovers on refresh', async () => {
    fetchRows.mockResolvedValueOnce(report()).mockResolvedValueOnce(report({ isStale: true, warning: 'Showing the last known inventory locations.' }))
      .mockResolvedValueOnce(report({ acquiredAtUtc: '2026-10-08T13:00:00Z' }));
    render(<InventoryLocationsPanel {...props} />);
    await screen.findByRole('table');
    await userEvent.click(screen.getByRole('button', { name: 'Refresh inventory' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('last known');
    expect(screen.getByText('2026-10-08T12:00:00Z')).toBeInTheDocument();
    expect(screen.getByRole('table')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Retry inventory' }));
    await screen.findByText('2026-10-08T13:00:00Z');
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('cancels and ignores obsolete success and failure after part, workspace, snapshot, site and closure changes', async () => {
    const pending: { resolve: (r: InventoryLocationsResponseDto) => void; reject: (e: Error) => void; signal: AbortSignal }[] = [];
    fetchRows.mockImplementation((_, __, ___, signal) => new Promise((resolve, reject) => pending.push({ resolve, reject, signal })));
    const view = render(<InventoryLocationsPanel {...props} />);
    await waitFor(() => expect(pending).toHaveLength(1));
    view.rerender(<InventoryLocationsPanel {...props} componentPart="OTHER" />);
    await waitFor(() => expect(pending).toHaveLength(2));
    expect(pending[0].signal.aborted).toBe(true);
    pending[0].resolve(report());
    view.rerender(<InventoryLocationsPanel {...props} assignmentId="ws2" />);
    await waitFor(() => expect(pending).toHaveLength(3));
    pending[1].reject(new Error('obsolete'));
    view.rerender(<InventoryLocationsPanel {...props} snapshotId="snap2" />);
    await waitFor(() => expect(pending).toHaveLength(4));
    view.rerender(<InventoryLocationsPanel {...props} site="KV" />);
    await waitFor(() => expect(pending).toHaveLength(5));
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    view.unmount();
    expect(pending.every(p => p.signal.aborted)).toBe(true);
    pending.slice(2).forEach(p => p.resolve(report()));
  });

  it('rejects cross-site responses and does not reuse unverified local rows on transport failure', async () => {
    fetchRows.mockResolvedValueOnce(report()).mockRejectedValueOnce(new Error('offline'))
      .mockResolvedValueOnce(report({ site: 'KV' }));
    render(<InventoryLocationsPanel {...props} />);
    await screen.findByRole('table');
    await userEvent.click(screen.getByRole('button', { name: 'Refresh inventory' }));
    await screen.findByRole('alert');
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Retry inventory' }));
    await waitFor(() => expect(fetchRows).toHaveBeenCalledTimes(3));
    expect(await screen.findByRole('alert')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });
});

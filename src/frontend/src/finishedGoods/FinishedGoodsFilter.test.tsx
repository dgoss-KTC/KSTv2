import { useRef, useState } from 'react';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { FinishedGoodsFilter } from './FinishedGoodsFilter';
import type { FinishedGoodsRow } from './report';
import type { ColumnFilter } from '../components/shared/ColumnFilterEditor';

function setup(count: number, filter?: ColumnFilter) {
  const apply = vi.fn();
  const rows: FinishedGoodsRow[] = Array.from({ length: count }, (_, i) => ({ rowId: String(i), product: `P${String(i).padStart(4, '0')}`,
    domain: 'KTC', location: null, sourceLocation: null, lot: null, reference: null, onHand: '1', unitPrice: '1', value: '1', isMissingPrice: false, isRma: false }));
  function Harness() {
    const anchor = useRef<HTMLButtonElement>(null); const [open, setOpen] = useState(true);
    return <><button ref={anchor} onClick={() => setOpen(true)}>Open</button>{open && <FinishedGoodsFilter field="product" rows={rows}
      filters={filter ? [filter] : []} anchor={anchor} onClose={() => setOpen(false)} onApply={apply} />}</>;
  }
  render(<Harness />); return { apply };
}

afterEach(() => vi.useRealTimers());
describe('Finished Goods shared editor adapter (offline)', () => {
  it('caps the visible list, finds beyond the cap, selects displayed search results only and represents global all compactly', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    const { apply } = setup(1205);
    await act(async () => { await vi.advanceTimersByTimeAsync(200); });
    expect(screen.getByText(/Showing the 1,000 most frequent of 1,205/)).toBeInTheDocument();
    expect(screen.queryByLabelText('Value: P1204')).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Search values'), { target: { value: 'P1204' } });
    await act(async () => { await vi.advanceTimersByTimeAsync(200); });
    expect(screen.getByLabelText('Value: P1204')).toBeInTheDocument();
    expect(screen.queryByLabelText('Value: P0000')).not.toBeInTheDocument();
    fireEvent.click(screen.getByLabelText('(Select all)'));
    fireEvent.click(screen.getByLabelText('Select displayed search results only'));
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(apply).toHaveBeenLastCalledWith({ column: 'product', kind: 'values', mode: 'include', values: ['P1204'], includeBlanks: false });
    fireEvent.click(screen.getByLabelText('(Select all)'));
    fireEvent.click(screen.getByRole('button', { name: 'Apply' })); expect(apply).toHaveBeenLastCalledWith(null);
  });

  it('retains hidden zero-count selections and blocks exceptions beyond the accepted cap', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    const selected = Array.from({ length: 1000 }, (_, i) => `outside-${i}`);
    const { apply } = setup(1, { column: 'product', kind: 'values', mode: 'include', values: selected, includeBlanks: false });
    await act(async () => { await vi.advanceTimersByTimeAsync(200); });
    expect(screen.getByLabelText('Value: P0000')).toBeInTheDocument();
    expect(screen.getByLabelText('Value: outside-999')).toBeChecked();
    fireEvent.click(screen.getByLabelText('Value: P0000'));
    expect(screen.getByText(/At most 1000 value exceptions/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
    expect(apply).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Clear filter' }));
    expect(apply).toHaveBeenCalledWith(null);
  });
});

import { useRef, useState } from 'react';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ShipmentColumnFilter, WorkspaceShipmentsClient, WorkspaceShipmentsFacets, WorkspaceShipmentsSpec } from '../../api/workspaceShipmentsApi';
import { ShipmentHeaderFilter } from './ShipmentHeaderFilter';

const spec: WorkspaceShipmentsSpec = { columns: [], maximumPageSize: 1000, pageSizes: [100, 500, 1000], maximumValues: 1000, maximumTextLength: 256, maximumDecimalDigits: 76, maximumDecimalScale: 76 };
const facets = (extra: Partial<WorkspaceShipmentsFacets> = {}): WorkspaceShipmentsFacets => ({ operationId: 'op', viewVersion: 4, column: 'itemNumber',
  values: [{ value: 'A', count: 30 }, { value: '(Blanks)', count: 2 }, { value: 'a', count: 1 }], retainedValues: [], blankCount: 3, distinctTotal: 3, matchingDistinctTotal: 3, truncated: false, ...extra });
function setup(kind = 'text', filter?: ShipmentColumnFilter, response = facets()) {
  const apply = vi.fn(); const values = vi.fn().mockResolvedValue(response);
  const column = { id: kind === 'text' ? 'itemNumber' : kind === 'values' ? 'onTime' : kind === 'date' ? 'originalCommit' : 'retroExtendedPrice', label: 'Test column', kind, filterKind: kind, provenance: 'Recorded', defaultSelected: true, monetary: false };
  function Harness() {
    const anchor = useRef<HTMLButtonElement>(null); const [open, setOpen] = useState(true);
    return <div className="shell" data-theme="light"><button ref={anchor} type="button" onClick={() => setOpen(true)}>Opener</button><div data-testid="clipping-grid" style={{ overflow: 'hidden' }}>
      {open && <ShipmentHeaderFilter column={column} anchor={anchor} spec={spec} filter={filter} client={{ values } as unknown as WorkspaceShipmentsClient} operationId="op" version={4} onClose={() => setOpen(false)} onApply={apply} />}
    </div></div>;
  }
  const ui = render(<Harness />); return { apply, values, ui };
}
beforeEach(() => { vi.useFakeTimers({ toFake: ['Date'] }); vi.setSystemTime(new Date(2026, 9, 19, 0, 30)); });
afterEach(() => vi.useRealTimers());

describe('Shipment header filters (offline facets, generated contracts)', () => {
  it('portals outside the clipping grid, inherits shell theme, contains keyboard focus and returns it on Escape', async () => {
    setup(); const dialog = screen.getByRole('dialog', { name: 'Filter Test column' });
    expect(screen.getByTestId('clipping-grid')).not.toContainElement(dialog);
    expect(dialog.closest('.shell')).toHaveAttribute('data-theme', 'light');
    expect(dialog).toContainElement(document.activeElement as HTMLElement);
    const user = userEvent.setup(); const last = within(dialog).getByRole('button', { name: 'Apply' }); last.focus();
    await user.tab(); expect(within(dialog).getByRole('button', { name: 'Close Filter Test column' })).toHaveFocus();
    await user.keyboard('{Escape}'); expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Opener' })).toHaveFocus();
  });

  it('discards drafts on outside click and Cancel without applying', () => {
    const { apply } = setup(); fireEvent.click(screen.getByRole('checkbox', { name: '(Select all)' }));
    fireEvent.pointerDown(document.body); expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(apply).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Opener' }));
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' })); expect(apply).not.toHaveBeenCalled();
  });

  it('uses exact values independently from blank captions and applies compact exclude semantics', async () => {
    const { apply, values } = setup(); await screen.findByLabelText('Value: (Blanks)');
    fireEvent.click(screen.getByLabelText('Value: (Blanks)')); fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(apply).toHaveBeenCalledWith({ column: 'itemNumber', kind: 'values', mode: 'exclude', values: ['(Blanks)'], includeBlanks: true });
    expect(screen.getByLabelText('(Blanks)')).toBeChecked();
    expect(values).toHaveBeenCalledWith('itemNumber', '', [], 4, expect.any(AbortSignal));
  });

  it('global Select all represents all eligible values including hidden values, not a capped include list', async () => {
    const response = facets({ distinctTotal: 4120, matchingDistinctTotal: 4120, truncated: true, retainedValues: [{ value: 'outside', count: 0 }] });
    const { apply } = setup('text', { column: 'itemNumber', kind: 'values', mode: 'include', values: ['outside'], includeBlanks: false }, response);
    await screen.findByText(/Showing the 1,000 most frequent of 4,120/);
    expect(screen.getByLabelText('Value: outside')).toBeChecked();
    expect(screen.getByLabelText('(Select all)')).toHaveAttribute('aria-checked', 'mixed');
    fireEvent.click(screen.getByLabelText('(Select all)')); fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(apply).toHaveBeenCalledWith(null);
  });

  it('labels search bulk as displayed only, retains hidden selected values, and never claims hidden matches were selected', async () => {
    const { values, apply } = setup('text', { column: 'itemNumber', kind: 'values', mode: 'include', values: ['outside'], includeBlanks: false }, facets({ retainedValues: [{ value: 'outside', count: 0 }], truncated: true, matchingDistinctTotal: 1500 }));
    await screen.findByLabelText('Value: A');
    fireEvent.change(screen.getByLabelText('Search values'), { target: { value: 'A' } });
    await waitFor(() => expect(values).toHaveBeenLastCalledWith('itemNumber', 'A', ['outside'], 4, expect.any(AbortSignal)));
    fireEvent.click(screen.getByLabelText('Select displayed search results only'));
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(apply).toHaveBeenCalledWith(expect.objectContaining({ mode: 'include', values: ['outside', 'A', '(Blanks)', 'a'] }));
    expect(screen.queryByLabelText('(Select all search results)')).not.toBeInTheDocument();
  });

  it('blocks empty include selection and has an explicit Clear filter action', () => {
    const { apply } = setup(); fireEvent.click(screen.getByLabelText('(Select all)'));
    expect(screen.getByText('Select at least one value.')).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Clear filter' })); expect(apply).toHaveBeenCalledWith(null);
  });

  it('contains submits literal wildcard-looking text and remains usable after values fail', async () => {
    const { values, apply } = setup(); values.mockRejectedValue(new Error('offline'));
    await screen.findByText(/Values could not be loaded/);
    fireEvent.click(screen.getByRole('button', { name: 'Contains' }));
    fireEvent.change(screen.getByLabelText('Test column contains'), { target: { value: '%_' } });
    fireEvent.submit(screen.getByRole('button', { name: 'Apply' }).closest('form')!);
    expect(apply).toHaveBeenCalledWith({ column: 'itemNumber', kind: 'contains', text: '%_', includeBlanks: true });
  });

  it('ignores an out-of-order facet reply and aborted departure', async () => {
    const { values, ui } = setup(); let old!: (v: WorkspaceShipmentsFacets) => void;
    values.mockImplementationOnce(() => new Promise<WorkspaceShipmentsFacets>(resolve => { old = resolve; }));
    await waitFor(() => expect(old).toBeTypeOf('function'));
    fireEvent.change(screen.getByLabelText('Search values'), { target: { value: 'new' } });
    values.mockResolvedValue(facets({ values: [{ value: 'NEW', count: 1 }] })); await screen.findByLabelText('Value: NEW');
    await act(async () => old(facets({ values: [{ value: 'OLD', count: 1 }] })));
    expect(screen.queryByLabelText('Value: OLD')).not.toBeInTheDocument();
    const signal = values.mock.calls[values.mock.calls.length - 1][4]; ui.unmount(); expect(signal.aborted).toBe(true);
  });

  it('rejects wrong operation/version/column facet identities', async () => {
    setup('text', undefined, facets({ operationId: 'wrong', values: [{ value: 'WRONG', count: 1 }] }));
    await new Promise(resolve => setTimeout(resolve, 250));
    expect(screen.queryByLabelText('Value: WRONG')).not.toBeInTheDocument();
  });

  it('uses exact decimal boundaries beyond floating point and refuses unsupported precision or reversed ranges', () => {
    const { apply } = setup('decimal');
    fireEvent.change(screen.getByLabelText('Condition'), { target: { value: 'between' } });
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '12345678901234567890.00000000000000000001' } });
    fireEvent.change(screen.getByLabelText('Upper value'), { target: { value: '12345678901234567890.00000000000000000000' } });
    expect(screen.getByText('Range start must not exceed end.')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Upper value'), { target: { value: '12345678901234567890.00000000000000000002' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(apply).toHaveBeenCalledWith(expect.objectContaining({ value: '12345678901234567890.00000000000000000001', upper: '12345678901234567890.00000000000000000002' }));
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '1'.repeat(77) } }); expect(screen.getByRole('button', { name: 'Apply' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Returns only (below 0)' }));
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(apply).toHaveBeenLastCalledWith(expect.objectContaining({ operator: 'lt', value: '0', includeBlanks: false, upper: null }));
  });

  it('builds inclusive quick ranges from the local business date and supports independent bounds and blanks', () => {
    const { apply } = setup('date');
    fireEvent.click(screen.getByRole('button', { name: 'Last 7 days' })); expect(screen.getByLabelText('From')).toHaveValue('2026-10-13'); expect(screen.getByLabelText('To')).toHaveValue('2026-10-19');
    fireEvent.click(screen.getByRole('button', { name: 'Last month' })); expect(screen.getByLabelText('From')).toHaveValue('2026-09-01'); expect(screen.getByLabelText('To')).toHaveValue('2026-09-30');
    fireEvent.change(screen.getByLabelText('To'), { target: { value: '' } }); fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(apply).toHaveBeenCalledWith({ column: 'originalCommit', kind: 'dateRange', from: '2026-09-01', to: null, includeBlanks: true });
  });

  it('On Time choices use 1/0/blank, carry counts and do not offer numeric conditions', async () => {
    const { apply } = setup('values', undefined, facets({ column: 'onTime', values: [{ value: '1', count: 7 }, { value: '0', count: 2 }], blankCount: 1 }));
    await screen.findByText('7'); fireEvent.click(screen.getByLabelText('✓ On time')); fireEvent.click(screen.getByLabelText('— Not determined'));
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(apply).toHaveBeenCalledWith({ column: 'onTime', kind: 'values', mode: 'exclude', values: ['1'], includeBlanks: false });
    expect(screen.queryByLabelText('Condition')).not.toBeInTheDocument();
  });
});

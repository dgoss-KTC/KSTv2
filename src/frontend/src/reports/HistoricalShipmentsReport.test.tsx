import { describe, expect, it, vi } from 'vitest';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { HistoricalShipmentsReport } from './HistoricalShipmentsReport';
import { hsColumnsKey, restoreColumns, validateFilters } from './historicalShipmentsPreferences';
import type { HistoricalShipmentsClient, HistoricalShipmentsSpec } from '../api/historicalShipmentsApi';

const spec: HistoricalShipmentsSpec = { sites: ['SW', 'KV'], maximumPageSize: 200, maximumXlsxDataRows: 1048575, columns: [
  { id: 'site', label: 'Site', kind: 'text', provenance: 'Recorded invoice header', defaultSelected: true, monetary: false },
  { id: 'invoiceNumber', label: 'Invoice Number', kind: 'text', provenance: 'Recorded invoice header', defaultSelected: true, monetary: false },
  { id: 'unitPrice', label: 'Recorded Unit Price', kind: 'decimal', provenance: 'Recorded original invoice currency', defaultSelected: false, monetary: true },
  { id: 'customerName', label: 'Customer Name (current master)', kind: 'text', provenance: 'Current customer master', defaultSelected: false, monetary: false },
] };
const op = (rows: number) => ({ operationId: 'o', state: 'Ready', rowCount: rows, columns: ['site', 'invoiceNumber'], expiresAtUtc: '', acquiredAtUtc: '2026-01-01T00:00:00Z', unavailableEnrichmentRows: 0 });
const pageOf = (n: number) => ({ operationId: 'o', offset: 0, totalRows: 250, columns: ['site', 'invoiceNumber'], rows: [{ physicalDetailId: `p${n}`, cells: ['SW', `0000${n}`] }] });
function mockClient(over: Partial<Record<keyof HistoricalShipmentsClient, unknown>> = {}) {
  const client = { acquire: vi.fn().mockResolvedValue(op(250)), page: vi.fn().mockImplementation(async (offset: number) => pageOf(offset / 100)),
    prepare: vi.fn(), release: vi.fn().mockResolvedValue(undefined), save: vi.fn(), saveStatus: vi.fn(), open: vi.fn(), ...over };
  return client;
}
function setup(client = mockClient(), active = true) {
  const props = { onBusyChange: vi.fn(), openAfterSave: false, onOpenAfterSaveChange: vi.fn(), loadSpec: async () => spec,
    createClient: () => client as unknown as HistoricalShipmentsClient };
  const view = render(<HistoricalShipmentsReport active={active} {...props} />);
  return { client, view, props };
}

describe('Historical Shipments rules', () => {
  it('migrates the old default set to the catalog defaults and preserves custom selections in catalog order', () => {
    const updated = { ...spec, columns: spec.columns.map(c => ({ ...c, defaultSelected: c.id === 'unitPrice' || c.id === 'customerName' })) };
    const old = ['site', 'customerNumber', 'customerName', 'invoiceNumber', 'invoiceDate', 'itemNumber',
      'invoicedQuantity', 'salesOrder', 'salesOrderLine', 'customerPo'];
    expect(restoreColumns(updated, JSON.stringify(old.reverse()))).toEqual(['unitPrice', 'customerName']);
    expect(restoreColumns(updated, null)).toEqual(['unitPrice', 'customerName']);
    expect(restoreColumns(updated, JSON.stringify(['customerName', 'invoiceNumber', 'obsolete']))).toEqual(['invoiceNumber', 'unitPrice', 'customerName']);
  });
  it('restores only recognized columns and falls back to defaults', () => {
    expect(restoreColumns(spec, JSON.stringify(['unitPrice', 'serial', 5, 'unitPrice']))).toEqual(['site', 'invoiceNumber', 'unitPrice']);
    expect(restoreColumns(spec, JSON.stringify(['serial']))).toEqual(['site', 'invoiceNumber']);
    expect(restoreColumns(spec, '{bad')).toEqual(['site', 'invoiceNumber']);
  });
  it('requires site and enforces Corp Group / Customer exclusivity and date order', () => {
    const f = { site: '', corpGroup: 'G', customerFrom: '1', customerTo: '', itemFrom: '', itemTo: '', productLineFrom: '', productLineTo: '', salespersonFrom: '', salespersonTo: '', invoiceDateFrom: '2026-02-01', invoiceDateTo: '2026-01-01' };
    expect(validateFilters(f)).toHaveLength(3);
    expect(validateFilters({ ...f, site: 'SW', corpGroup: '', invoiceDateFrom: '', invoiceDateTo: '' })).toEqual([]);
  });
});

describe('Historical Shipments report', () => {
  it('renders On Time 1 and 0 as accessible marks and null as unavailable', async () => {
    localStorage.clear();
    const client = mockClient({ acquire: vi.fn().mockResolvedValue({ ...op(3), columns: ['onTime'] }),
      page: vi.fn().mockResolvedValue({ ...pageOf(0), rows: [
        { physicalDetailId: 'a', cells: ['1'] }, { physicalDetailId: 'b', cells: ['0'] }, { physicalDetailId: 'c', cells: [null] },
      ] }) });
    setup(client); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Display' }));
    expect(await screen.findByRole('img', { name: 'On Time: True' })).toHaveTextContent('✓');
    expect(screen.getByRole('img', { name: 'On Time: False' })).toHaveTextContent('✕');
    expect(screen.getByText('—')).toBeInTheDocument();
  });
  it('offers only optional checkboxes and keeps defaults when restoring a custom preference', async () => {
    localStorage.setItem(hsColumnsKey, JSON.stringify(['unitPrice']));
    const { client } = setup(); await screen.findByLabelText(/^Site \(required\)/);
    expect(screen.queryByRole('checkbox', { name: 'Site' })).toBeNull();
    expect(screen.queryByRole('checkbox', { name: 'Invoice Number' })).toBeNull();
    expect(screen.getByRole('checkbox', { name: 'Recorded Unit Price' })).toBeChecked();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Recorded Unit Price' }));
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Display' })); await screen.findByText('Page 1 of 3');
    expect(client.acquire).toHaveBeenCalledWith(expect.objectContaining({ columns: ['site', 'invoiceNumber'] }), expect.anything());
  });
  it('Restore default columns replaces a custom selection, persists the catalog order and reacquires before export', async () => {
    localStorage.setItem(hsColumnsKey, JSON.stringify(['unitPrice']));
    const { client } = setup(); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Display' })); await screen.findByText('Page 1 of 3');
    fireEvent.click(screen.getByRole('button', { name: 'Restore default columns' }));
    expect(JSON.parse(localStorage.getItem(hsColumnsKey)!)).toEqual(['site', 'invoiceNumber']);
    expect(screen.queryByText('Page 1 of 3')).toBeNull();
    expect(screen.getByRole('button', { name: 'Export' })).toBeDisabled();
    expect(client.release).toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Display' })); await screen.findByText('Page 1 of 3');
    expect(client.acquire).toHaveBeenLastCalledWith(expect.objectContaining({ columns: ['site', 'invoiceNumber'] }), expect.anything());
  });
  it('starts with blank dates, explains them, disables Display until a site is chosen, and never persists filters', async () => {
    localStorage.clear(); setup();
    const from = await screen.findByLabelText('Invoice Date From') as HTMLInputElement;
    expect(from.value).toBe(''); expect(screen.getByText(/Leave both dates blank to include all available invoice history/)).toBeTruthy();
    expect((screen.getByRole('button', { name: 'Display' }) as HTMLButtonElement).disabled).toBe(true);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    expect((screen.getByRole('button', { name: 'Display' }) as HTMLButtonElement).disabled).toBe(false);
    expect(Object.keys(localStorage)).not.toContain('site');
    expect(localStorage.getItem(hsColumnsKey)).toBeNull();
  });
  it('sends the validated request with exactly the selected columns, adds monetary notice and persists only columns', async () => {
    localStorage.clear(); const { client } = setup(); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'KV' } });
    fireEvent.change(screen.getByLabelText('Customer From'), { target: { value: '000123' } });
    fireEvent.change(screen.getByLabelText('Invoice Date From'), { target: { value: '2026-01-01' } });
    fireEvent.click(screen.getByLabelText('Recorded Unit Price'));
    expect(screen.getByText(/Select Currency in the optional columns/)).toBeTruthy();
    expect(JSON.parse(localStorage.getItem(hsColumnsKey)!)).toEqual(['site', 'invoiceNumber', 'unitPrice']);
    fireEvent.click(screen.getByRole('button', { name: 'Display' }));
    await screen.findByText('Page 1 of 3');
    expect(client.acquire).toHaveBeenCalledWith(expect.objectContaining({ site: 'KV', customerFrom: '000123', customerTo: null, corpGroup: null,
      invoiceDateFrom: '2026-01-01', invoiceDateTo: null, columns: ['site', 'invoiceNumber', 'unitPrice'] }), expect.anything());
    expect(screen.getByText('00000')).toBeTruthy();
  });
  it('blocks Display on Corp Group with Customer range', async () => {
    setup(); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.change(screen.getByLabelText('Corp Group'), { target: { value: 'ACME' } });
    fireEvent.change(screen.getByLabelText('Customer To'), { target: { value: '9' } });
    expect((screen.getByRole('button', { name: 'Display' }) as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getAllByRole('alert').some(a => /not both/.test(a.textContent ?? ''))).toBe(true);
  });
  it('pages without retaining earlier pages and ignores out-of-order replies', async () => {
    const replies: Array<(v: unknown) => void> = [];
    const client = mockClient({ page: vi.fn().mockImplementationOnce(async () => pageOf(0)).mockImplementation(() => new Promise(r => replies.push(r))) });
    setup(client); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Display' })); await screen.findByText('Page 1 of 3');
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    expect((screen.getByRole('button', { name: 'Next' }) as HTMLButtonElement).disabled).toBe(true);
    await act(async () => replies[0](pageOf(1)));
    await screen.findByText('Page 2 of 3'); expect(screen.queryByText('00000')).toBeNull();
  });
  it('reports empty results and actionable failures, and releases on failure', async () => {
    const client = mockClient({ acquire: vi.fn().mockResolvedValueOnce(op(0)).mockRejectedValueOnce(new Error('An invoice detail has no matching header.')) });
    setup(client); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Display' })); await screen.findByText('No invoiced shipments matched these filters.');
    expect((screen.getByRole('button', { name: 'Export' }) as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(screen.getByRole('button', { name: 'Display' })); await screen.findByText('An invoice detail has no matching header.');
    expect(client.release).toHaveBeenCalled();
  });
  it('shows the host plain-string rejection instead of a generic message', async () => {
    const client = mockClient({ acquire: vi.fn().mockRejectedValue('The source did not answer within the time limit. Narrow the filters and try again.') });
    setup(client); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Display' })); await screen.findByText(/did not answer within the time limit/);
  });
  it('rejects late replies after cancellation', async () => {
    let resolve!: (v: unknown) => void;
    const client = mockClient({ acquire: vi.fn().mockImplementation(() => new Promise(r => { resolve = r; })) });
    setup(client); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Display' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Cancel' }));
    await act(async () => resolve(op(250)));
    expect(screen.queryByText(/Page 1 of/)).toBeNull(); expect(client.release).toHaveBeenCalled();
  });
  it('releases the operation on departure and on unmount', async () => {
    const { client, view, props } = setup(); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Display' })); await screen.findByText('Page 1 of 3');
    view.rerender(<HistoricalShipmentsReport active={false} {...props} />);
    await waitFor(() => expect(client.release).toHaveBeenCalledTimes(1)); expect(screen.queryByText('Page 1 of 3')).toBeNull();
    view.unmount();
  });
  it('exports the acquired result then saves with the protected path', async () => {
    const client = mockClient({ prepare: vi.fn().mockResolvedValue({ operationId: 'o', preparationId: 'p', fileName: 'x.xlsx', byteLength: 10, expiresAtUtc: '' }),
      save: vi.fn().mockResolvedValue({ outcome: 'saved', message: 'Workbook saved', savedArtifactId: 'a' }) });
    const { props } = setup(client); await screen.findByLabelText(/^Site \(required\)/);
    fireEvent.change(screen.getByLabelText(/^Site \(required\)/), { target: { value: 'SW' } });
    fireEvent.click(screen.getByRole('button', { name: 'Display' })); await screen.findByText('Page 1 of 3');
    fireEvent.click(screen.getByRole('button', { name: 'Export' })); await screen.findByText(/Workbook ready/);
    fireEvent.click(screen.getByRole('button', { name: 'Save As…' })); await screen.findByText('Workbook saved');
    expect(client.save).toHaveBeenCalledWith(expect.objectContaining({ preparationId: 'p' }), false, expect.any(String));
    expect(props.onBusyChange).toHaveBeenCalledWith(true);
  });
});

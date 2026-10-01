import { beforeEach, describe, expect, it } from 'vitest';
import { COLUMNS, defaultLayout, emptyFilters, loadLayout, reportRows, saveLayout, type Filters } from './report';
import type { OrderLine } from './report';

const line = (order: string, itemNumber: string, customerName: string | null, dueDate: string | null, overrides: Partial<OrderLine> = {}): OrderLine => ({
  key: { domain: 'TEST', salesOrder: order, line: 1 }, itemNumber, site: 'SW', purchaseOrder: null, stat: null,
  shippedQty: '2', sourceValues: { dueDate, performDate: null, requiredDate: null, dockDate: null, orderQty: '5', price: '0.0125' },
  planningValues: { dueDate, performDate: null, requiredDate: null, dockDate: null, orderQty: '5', price: '0.0125' }, shippedQtyText: '2',
  open: '3', extPrice: '0.0375', unitPrice: '0', allocated: null, customer: 'C1', customerName,
  salesperson: 'SP', customerPart: null, ios: 'IOS', lineComments: '', lineHold: null, partials: null,
  picked: null, plnr: null, prodStat: null, productLine: 'B', qaHold: null, remarks: null, revision: null,
  shipAcct: null, shipTo: null, shipVia: null, siteQoh: null, soHoldStatus: null, soType: null, consignment: true,
  ...overrides,
});

describe('Customer Open Orders report', () => {
  beforeEach(() => window.localStorage.clear());

  it('sorts the complete interleaved parent-batch population by hidden customer, item and due date', () => {
    const batches = [line('4', 'Z', 'Zulu', '2027-01-02'), line('2', 'A', 'Acme', '2027-01-03'),
      line('3', 'A', 'Acme', '2027-01-01'), line('1', 'B', null, null)];
    expect(reportRows(batches, emptyFilters).map(r => r.key.salesOrder)).toEqual(['1', '3', '2', '4']);
    expect(COLUMNS.length).toBe(34); // nine defaults and all 25 accepted optional columns
    expect(defaultLayout().visible).toEqual(['dueDate', 'order', 'po', 'line', 'itemNumber', 'site', 'open', 'stat', 'extPrice']);
    expect(COLUMNS.find(c => c.id === 'unitPrice')!.value(batches[0])).toBe('0');
    expect(COLUMNS.find(c => c.id === 'extPrice')!.value(batches[0])).toBe('0.0375');
  });

  it('AND-combines hidden fields, treats blank source values as nonmatches, bounds product and date ranges', () => {
    const rows = [line('yes', 'A', 'Acme Holdings', '2027-01-06'), line('no', 'A', null, null, { customer: null }),
      line('other', 'B', 'Acme', '2027-02-01', { salesperson: 'OTHER' })];
    expect(reportRows(rows, { ...emptyFilters, customerName: 'hOlD', customer: 'C1', salesperson: 'SP',
      productFrom: 'A', productTo: 'C', ios: 'ios', dueFrom: '2027-01-01', dueTo: '2027-01-06' }).map(r => r.key.salesOrder)).toEqual(['yes']);
    // The legacy query enables date filtering only when both ends are supplied.
    expect(reportRows(rows, { ...emptyFilters, dueFrom: '2027-01-07' })).toHaveLength(3);
    expect(reportRows(rows, { ...emptyFilters, productFrom: 'C' })).toHaveLength(0);
    expect(reportRows(rows, { ...emptyFilters, dueFrom: '2027-01-01', dueTo: '2027-01-31' }).map(r => r.key.salesOrder)).toEqual(['yes']);
  });

  it.each([
    ['customerName', (r: OrderLine) => r.customerName, 'AcMe'],
    ['customer', (r: OrderLine) => r.customer, 'c-19'],
    ['salesperson', (r: OrderLine) => r.salesperson, 'mIt'],
    ['ios', (r: OrderLine) => r.ios, 'nsi'],
    ['so', (r: OrderLine) => r.key.salesOrder, 'o-42'],
    ['po', (r: OrderLine) => r.purchaseOrder, 'o-91'],
    ['itemNumber', (r: OrderLine) => r.itemNumber, 'm-77'],
  ] as const)('matches %s as a case-insensitive literal substring and rejects null, blank and partial nonmatches', (field, value, search) => {
    const matching = line('SO-42', 'ITEM-77', 'Acme Retail', '2027-01-01',
      { customer: 'AC-190', salesperson: 'SMITH', ios: 'INSIDE', purchaseOrder: 'PO-910' });
    const blank = line('SO-blank', '', null, '2027-01-01',
      { customer: null, salesperson: null, ios: null, purchaseOrder: null });
    const nonmatching = line('SO-9', 'ITEM-7', 'Ace Retail', '2027-01-01',
      { customer: 'AC-10', salesperson: 'SMILE', ios: 'OUTSIDE', purchaseOrder: 'PO-90' });
    expect(value(matching)?.toLowerCase()).toContain(search.toLowerCase());
    expect(reportRows([blank, nonmatching, matching], { ...emptyFilters, [field]: search }).map(r => r.key.salesOrder)).toEqual(['SO-42']);
  });

  it('ANDs all seven hidden text predicates across batches without treating wildcard characters specially', () => {
    const exact = line('SO_%42', 'ITEM_77', 'Acme_%', '2027-01-01',
      { customer: 'AC_%19', salesperson: 'SM_%TH', ios: 'IN_%', purchaseOrder: 'PO_%91' });
    const other = line('SO-42', 'ITEM-77', 'Acme', '2027-01-01', { purchaseOrder: 'PO-91' });
    const filters: Filters = { ...emptyFilters, customerName: '_%', customer: '_%', salesperson: '_%',
      ios: '_%', so: '_%', po: '_%', itemNumber: '_77' };
    expect(reportRows([other, exact], filters)).toEqual([exact]);
    expect(reportRows([other, exact], { ...filters, itemNumber: '_78' })).toEqual([]);
  });

  it('isolates layout by assignment ID and recovers malformed or unknown saved IDs without losing hidden order', () => {
    const layout = defaultLayout();
    layout.order = ['customerName', ...layout.order.filter(id => id !== 'customerName')];
    layout.visible = ['order'];
    saveLayout('workspace-A', layout);
    expect(loadLayout('workspace-A')).toEqual(layout);
    expect(loadLayout('workspace-B')).toEqual(defaultLayout());
    window.localStorage.setItem('kst.openOrders.layout.v1.workspace-B', '{corrupt');
    expect(loadLayout('workspace-B')).toEqual(defaultLayout());
    window.localStorage.setItem('kst.openOrders.layout.v1.workspace-B', JSON.stringify({ order: ['unknown', 'po', 'customerName', 'po'], visible: ['unknown', 'po'] }));
    const restored = loadLayout('workspace-B');
    expect(restored.order.slice(0, 2)).toEqual(['po', 'customerName']);
    expect(restored.visible).toEqual(['po']);
    expect(restored.order).toHaveLength(COLUMNS.length);
    window.localStorage.setItem('kst.openOrders.layout.v1.workspace-B', JSON.stringify({ order: ['future-id'], visible: ['future-id'] }));
    expect(loadLayout('workspace-B')).toEqual(defaultLayout());
  });

  it('changes only display labels; keeps stable IDs, default order and exported label counterparts', () => {
    expect(COLUMNS.filter(c => ['order', 'stat', 'customer', 'plnr'].includes(c.id)).map(c => [c.id, c.label]))
      .toEqual([['order', 'SO'], ['stat', 'Status'], ['customer', 'Customer #'], ['plnr', 'Planner']]);
    expect(defaultLayout().visible).toEqual(['dueDate', 'order', 'po', 'line', 'itemNumber', 'site', 'open', 'stat', 'extPrice']);
  });
});

import { act, render, renderHook, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { COLUMNS, defaultLayout, saveLayout } from './report';
import { PLAN_COLUMNS, PLAN_REQUIRED, planDefaultLayout, savePlanLayout } from './planLayout';
import { loadViews, moveColumn, normalizeView, persistViews, sameLayout, showColumn } from './views';
import { useOrderViews } from './useOrderViews';
import { OrderColumns } from './OrderColumns';
import { columnPointer } from './pointerTestSupport';

beforeEach(() => localStorage.clear());
const originalHitTest = Object.getOwnPropertyDescriptor(document, 'elementFromPoint');
afterEach(() => {
  if (originalHitTest) Object.defineProperty(document, 'elementFromPoint', originalHitTest);
  else Reflect.deleteProperty(document, 'elementFromPoint');
});
it('migrates customized legacy Report/Plan layouts to Personal, retaining actual application Default and workspace isolation', () => {
  const report = { order: [...defaultLayout().order].reverse(), visible: ['po', 'order'] as const };
  saveLayout('A', { ...report, visible: [...report.visible] });
  savePlanLayout('A', { order: [...PLAN_COLUMNS].reverse(), visible: [...PLAN_REQUIRED, 'extPrice'] });
  const r = loadViews('A', 'report'); const p = loadViews('A', 'plan');
  expect(r.active).toBe('Personal'); expect(r.layout.order[0]).toBe('unitPrice'); expect(r.layout.visible).toEqual(['po', 'order']);
  expect(r.views[0]).toEqual({ name: 'Default', layout: defaultLayout() });
  expect(p.active).toBe('Personal'); expect(p.layout.visible).toContain('extPrice');
  expect(p.views[0].layout).toEqual(planDefaultLayout());
  persistViews('A', 'report', r); persistViews('A', 'plan', p);
  expect(loadViews('A', 'report')).toEqual(r); expect(loadViews('A', 'plan')).toEqual(p);
  expect(loadViews('B', 'report').active).toBe('Default'); expect(loadViews('B', 'plan').layout).toEqual(planDefaultLayout());
});
it.each([null, {}, { order: ['unknown'], visible: ['unknown'] }, { order: 'bad', visible: [] }])('validates stored layouts without allowing unknown IDs: %j', value => {
  expect(normalizeView(value, 'plan')).toEqual(planDefaultLayout());
  expect(normalizeView(value, 'report')).toEqual(defaultLayout());
});
it('deduplicates IDs, appends newly known IDs and always restores required Plan columns', () => {
  const layout = normalizeView({ order: ['price', 'price', 'unknown'], visible: ['extPrice', 'unknown', 'extPrice'] }, 'plan');
  expect(layout.order[0]).toBe('price'); expect(layout.order).toHaveLength(PLAN_COLUMNS.length);
  expect(layout.visible).toEqual(['extPrice', ...PLAN_REQUIRED]);
  const report = normalizeView({ order: [], visible: [] }, 'report'); expect(report.visible).toEqual([]);
});
it('rejects malformed schema/duplicate Default names and bounds named registry', () => {
  localStorage.setItem('kst.openOrders.views.v1.A.report', JSON.stringify({ version: 1, active: 'dEfAuLt', views: [
    { name: 'Default', layout: { order: [], visible: [] } }, { name: 'One', layout: defaultLayout() },
    { name: 'one', layout: { order: [], visible: [] } }, { name: '', layout: {} }, { name: 'x'.repeat(61), layout: {} }] }));
  const result = loadViews('A', 'report'); expect(result.views.map(v => v.name)).toEqual(['Default', 'One']); expect(result.active).toBe('Default');
  localStorage.setItem('kst.openOrders.views.v1.A.plan', '{bad'); expect(loadViews('A', 'plan').layout).toEqual(planDefaultLayout());
});
it('uses one full-order move and keeps hidden relative order; showing appends to Shown', () => {
  const layout = { order: ['dueDate', 'order', 'po', 'line', 'itemNumber'], visible: ['dueDate', 'po', 'itemNumber'] } as const;
  const moved = moveColumn({ order: [...layout.order], visible: [...layout.visible] }, 'itemNumber', 'dueDate');
  expect(moved.order).toEqual(['itemNumber', 'dueDate', 'order', 'po', 'line']);
  expect(moved.order.filter(id => !moved.visible.includes(id))).toEqual(['order', 'line']);
  expect(showColumn(moved, 'order').order).toEqual(['itemNumber', 'dueDate', 'po', 'line', 'order']);
});
it('does not persist modifications as named definitions until saved, and reports storage failures without dropping session edits', () => {
  const hook = renderHook(() => useOrderViews('A', 'report'));
  act(() => hook.result.current.change(moveColumn(hook.result.current.layout, 'po', 'dueDate')));
  expect(hook.result.current.modified).toBe(true); expect(localStorage.getItem('kst.openOrders.views.v1.A.report')).toBeNull();
  expect(loadViews('A', 'report').layout.order[0]).toBe('dueDate');
  const write = vi.spyOn(localStorage, 'setItem').mockImplementation(() => { throw new Error('quota'); });
  act(() => { hook.result.current.save('Personal'); });
  expect(hook.result.current.error).toMatch(/could not be remembered/); expect(hook.result.current.layout.order[0]).toBe('po');
  write.mockRestore();
});
function UI({ mode = 'report', workspace = 'A' }: { mode?: 'report' | 'plan'; workspace?: string }) {
  const views = useOrderViews(workspace, mode);
  return <><output aria-label="Layout">{views.layout.order.filter(id => views.layout.visible.includes(id)).join(',')}</output>
    <OrderColumns views={views} mode={mode} move={(source, target) => views.change(moveColumn(views.layout, source, target))} announce={() => {}} /></>;
}
it('protects modified views with Cancel, Save and Discard decisions; Default stays immutable', async () => {
  const user = userEvent.setup(); render(<UI />);
  await user.click(screen.getByRole('button', { name: /Columns 9 of/ }));
  await user.click(screen.getByRole('button', { name: 'Hide Site' }));
  await user.click(screen.getByRole('button', { name: 'Save as new' }));
  await user.type(screen.getByLabelText('View name'), 'Scheduling'); await user.click(screen.getByRole('button', { name: 'Save view' }));
  await user.click(screen.getByRole('button', { name: 'Hide PO' }));
  await user.click(screen.getByRole('button', { name: 'Default' }));
  await user.click(screen.getByRole('button', { name: 'Cancel switch' }));
  expect(screen.getByLabelText('Layout')).not.toHaveTextContent('po');
  await user.click(screen.getByRole('button', { name: 'Default' }));
  await user.click(screen.getByRole('button', { name: 'Save view' }));
  expect(screen.getByLabelText('Layout')).toHaveTextContent('dueDate,order,po,line,itemNumber,site,open,stat,extPrice');
  await user.click(screen.getByRole('button', { name: 'Scheduling' }));
  expect(screen.getByLabelText('Layout')).not.toHaveTextContent('po');
  await user.click(screen.getByRole('button', { name: 'Hide Open' }));
  await user.click(screen.getByRole('button', { name: 'Default' }));
  await user.click(screen.getByRole('button', { name: 'Discard and switch' }));
  expect(loadViews('A', 'report').views.find(v => v.name === 'Default')!.layout).toEqual(defaultLayout());
  expect(loadViews('A', 'report').views.find(v => v.name === 'Scheduling')!.layout.visible).toContain('open');
});
it('supports list drag, keyboard moves, column counts from registry, required locks, Escape and focus return', async () => {
  const user = userEvent.setup(); render(<div className="shell" data-theme="light" data-density="comfortable"><UI mode="plan" /></div>);
  const button = screen.getByRole('button', { name: `Columns 11 of ${PLAN_COLUMNS.length}` }); await user.click(button);
  const dialog = screen.getByRole('dialog'); expect(dialog.parentElement).toHaveClass('shell'); expect(dialog.closest('.open-orders__grid')).toBeNull();
  expect(screen.queryByRole('button', { name: 'Hide SO' })).not.toBeInTheDocument(); expect(screen.getAllByLabelText('Required')).toHaveLength(PLAN_REQUIRED.length);
  const row = screen.getByLabelText('Item Number column'); row.focus(); await user.keyboard('{Alt>}{ArrowUp}{/Alt}');
  expect(screen.getByLabelText('Layout')).toHaveTextContent('order,po,itemNumber,line'); expect(row).toHaveFocus();
  const target = screen.getByLabelText('SO column');
  Object.defineProperty(document, 'elementFromPoint', { configurable: true, value: () => target });
  columnPointer('pointerdown', row, 100, 100); columnPointer('pointermove', document, 100, 30); columnPointer('pointerup', document, 100, 30);
  expect(screen.getByLabelText('Layout')).toHaveTextContent(/^itemNumber,order,po/);
  columnPointer('pointerdown', row, 100, 100); columnPointer('pointermove', document, 100, 30);
  await user.keyboard('{Escape}');
  expect(screen.getByRole('dialog')).toBeInTheDocument(); expect(row).toHaveFocus();
  await user.keyboard('{Escape}'); expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(button).toHaveFocus();
  await user.click(button); const d = within(screen.getByRole('dialog')); await user.click(d.getByRole('button', { name: 'Done' })); expect(button).toHaveFocus();
});
it('compares visible sets semantically, but never loses hidden ordering in a named definition', () => {
  const a = defaultLayout(); expect(sameLayout(a, { ...a, visible: [...a.visible].reverse() })).toBe(true);
  expect(sameLayout(a, { ...a, order: [...a.order].reverse() })).toBe(false);
  expect(COLUMNS.length).toBeGreaterThan(a.visible.length);
});

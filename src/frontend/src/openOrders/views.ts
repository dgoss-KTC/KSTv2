import { COLUMNS, defaultLayout, loadLayout } from './report';
import { PLAN_COLUMNS, PLAN_REQUIRED, loadPlanLayout, planDefaultLayout, type LayoutForPlan, type PlanColumnId } from './planLayout';

export type ViewMode = 'report' | 'plan';
export interface NamedView { name: string; layout: LayoutForPlan }
export interface ViewState { active: string; views: NamedView[]; layout: LayoutForPlan }
export const viewDefault = (mode: ViewMode): LayoutForPlan => mode === 'plan' ? planDefaultLayout() : defaultLayout();
export const viewColumns = (mode: ViewMode): readonly PlanColumnId[] => mode === 'plan' ? PLAN_COLUMNS : COLUMNS.map(c => c.id);
const key = (workspace: string, mode: ViewMode) => `kst.openOrders.views.v1.${workspace}.${mode}`;
export function normalizeView(value: unknown, mode: ViewMode): LayoutForPlan {
  const fallback = viewDefault(mode);
  if (!value || typeof value !== 'object') return fallback;
  const record = value as Record<string, unknown>;
  if (!Array.isArray(record.order) || !Array.isArray(record.visible)) return fallback;
  const registry = viewColumns(mode);
  const valid = (list: unknown[]) => [...new Set(list.filter((id): id is PlanColumnId => typeof id === 'string' && registry.includes(id as PlanColumnId)))];
  const order = valid(record.order); const visible = valid(record.visible);
  if ((!order.length && record.order.length) || (!visible.length && record.visible.length)) return fallback;
  return { order: [...order, ...registry.filter(id => !order.includes(id))],
    visible: mode === 'plan' ? [...new Set([...visible, ...PLAN_REQUIRED])] : visible };
}
export function sameLayout(a: LayoutForPlan, b: LayoutForPlan): boolean {
  return JSON.stringify([a.order, a.order.filter(id => a.visible.includes(id))]) === JSON.stringify([b.order, b.order.filter(id => b.visible.includes(id))]);
}
export function loadViews(workspace: string, mode: ViewMode): ViewState {
  const defaults: NamedView = { name: 'Default', layout: viewDefault(mode) };
  try {
    const raw: unknown = JSON.parse(localStorage.getItem(key(workspace, mode)) ?? 'null');
    if (raw && typeof raw === 'object') {
      const record = raw as Record<string, unknown>;
      if (record.version === 1 && Array.isArray(record.views)) {
        const views = [defaults];
        for (const item of record.views.slice(0, 50)) {
          if (!item || typeof item !== 'object' || typeof item.name !== 'string') continue;
          const name = item.name.trim();
          if (!name || name.length > 60 || views.some(v => v.name.toLowerCase() === name.toLowerCase())) continue;
          views.push({ name, layout: normalizeView(item.layout, mode) });
        }
        const active = views.find(v => v.name === record.active) ?? defaults;
        return { views, active: active.name, layout: active.layout };
      }
    }
  } catch { /* Invalid or unavailable local preferences cannot grant authority or hide required columns. */ }
  const legacy = mode === 'plan' ? loadPlanLayout(workspace) : loadLayout(workspace);
  const personal = !sameLayout(legacy, defaults.layout);
  return { views: personal ? [defaults, { name: 'Personal', layout: legacy }] : [defaults],
    active: personal ? 'Personal' : 'Default', layout: legacy };
}
export function persistViews(workspace: string, mode: ViewMode, state: ViewState): void {
  // Save named definitions/selection only, never silently save a modified layout.
  localStorage.setItem(key(workspace, mode), JSON.stringify({ version: 1, active: state.active, views: state.views }));
}
/** Move through the full ordering: hidden columns retain their relative order. All gestures use this. */
export function moveColumn(layout: LayoutForPlan, source: PlanColumnId, target: PlanColumnId): LayoutForPlan {
  const order = [...layout.order]; const from = order.indexOf(source); const to = order.indexOf(target);
  if (from < 0 || to < 0 || from === to) return layout;
  order.splice(from, 1); order.splice(to, 0, source);
  return { ...layout, order };
}
export function showColumn(layout: LayoutForPlan, id: PlanColumnId): LayoutForPlan {
  return { order: [...layout.order.filter(c => c !== id), id], visible: [...new Set([...layout.visible, id])] };
}
export const columnLabel = (id: PlanColumnId) => id === 'orderQty' ? 'Order Qty' : id === 'price' ? 'Price' : COLUMNS.find(c => c.id === id)!.label;

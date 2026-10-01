import { COLUMNS, type ColumnId } from './report';

export type PlanColumnId = ColumnId | 'orderQty' | 'price';
export const PLAN_REQUIRED: readonly PlanColumnId[] = [
  'order', 'po', 'line', 'itemNumber', 'open', 'dueDate', 'performDate',
  'requiredDate', 'dockDate', 'orderQty', 'price',
];
export const PLAN_COLUMNS: readonly PlanColumnId[] = [...PLAN_REQUIRED,
  ...COLUMNS.map(c => c.id).filter(id => !PLAN_REQUIRED.includes(id))];
export const planDefaultLayout = (): LayoutForPlan => ({ order: [...PLAN_COLUMNS], visible: [...PLAN_REQUIRED] });
export interface LayoutForPlan {
  order: PlanColumnId[];
  visible: PlanColumnId[];
}
const storageKey = (id: string) => `kst.openOrders.planLayout.v1.${id}`;

export function loadPlanLayout(id: string): LayoutForPlan {
  try {
    const saved: unknown = JSON.parse(window.localStorage.getItem(storageKey(id)) ?? 'null');
    if (!saved || typeof saved !== 'object') return planDefaultLayout();
    const record = saved as Record<string, unknown>;
    if (!Array.isArray(record.order) || !Array.isArray(record.visible)) return planDefaultLayout();
    const valid = (list: unknown[]) => [...new Set(list.filter((item): item is PlanColumnId =>
      typeof item === 'string' && PLAN_COLUMNS.includes(item as PlanColumnId)))];
    const order = valid(record.order);
    const visible = valid(record.visible);
    if (!order.length && record.order.length || !visible.length && record.visible.length) return planDefaultLayout();
    return { order: [...order, ...PLAN_COLUMNS.filter(id => !order.includes(id))],
      visible: [...new Set([...visible, ...PLAN_REQUIRED])] };
  } catch { return planDefaultLayout(); }
}

export function savePlanLayout(id: string, layout: LayoutForPlan): void {
  window.localStorage.setItem(storageKey(id), JSON.stringify(layout));
}

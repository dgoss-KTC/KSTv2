import { beforeEach, describe, expect, it } from 'vitest';
import { defaultLayout, loadLayout, saveLayout } from './report';
import { loadPlanLayout, planDefaultLayout, PLAN_REQUIRED, savePlanLayout } from './planLayout';

describe('separate Open Orders Plan Mode layout', () => {
  beforeEach(() => window.localStorage.clear());

  it('defaults and resets to the accepted order without altering saved Report Mode columns', () => {
    const report = defaultLayout(); report.visible = ['po']; saveLayout('A', report);
    expect(loadPlanLayout('A').order.slice(0, 11)).toEqual(PLAN_REQUIRED);
    expect(loadPlanLayout('A').visible).toEqual(PLAN_REQUIRED);
    const customized = loadPlanLayout('A'); customized.visible.push('extPrice');
    customized.order = ['extPrice', ...customized.order.filter(id => id !== 'extPrice')];
    savePlanLayout('A', customized);
    expect(loadPlanLayout('A').order[0]).toBe('extPrice');
    expect(loadPlanLayout('A').visible).toContain('extPrice');
    expect(loadLayout('A')).toEqual(report);
    savePlanLayout('A', planDefaultLayout());
    expect(loadPlanLayout('A').order.slice(0, 11)).toEqual(PLAN_REQUIRED);
    expect(loadLayout('A')).toEqual(report);
    expect(loadPlanLayout('B')).toEqual(planDefaultLayout());
  });

  it('recovers malformed and unknown IDs and keeps required fields visible', () => {
    window.localStorage.setItem('kst.openOrders.planLayout.v1.A', '{bad');
    expect(loadPlanLayout('A')).toEqual(planDefaultLayout());
    window.localStorage.setItem('kst.openOrders.planLayout.v1.A', JSON.stringify({ order: ['unknown', 'extPrice'], visible: ['extPrice'] }));
    const restored = loadPlanLayout('A');
    expect(restored.order[0]).toBe('extPrice');
    expect(restored.visible).toEqual(['extPrice', ...PLAN_REQUIRED]);
  });
});

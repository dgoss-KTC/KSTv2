import type { LongTermShortageRow } from '../../src/frontend/src/api/longTermShortagesApi';
import type { LongTermShortagesFilters } from '../../src/frontend/src/longTermShortages/longTermShortagesPresentation';

/** Retained pre-compact filter oracle. Diagnostic only; never imported by the application. */
export function filterLongTermShortages(rows: readonly LongTermShortageRow[], filters: LongTermShortagesFilters): LongTermShortageRow[] {
  const component = filters.component.trim().toLocaleLowerCase();
  const planner = filters.planner.trim().toLocaleLowerCase();
  return rows.filter(row => filters.showAll || ['CriticalShort', 'FutureShort', 'SafetyStockShort'].includes(row.severity))
    .filter(row => !component || row.componentPart.toLocaleLowerCase().includes(component))
    .filter(row => !planner || (row.planner ?? '').toLocaleLowerCase().includes(planner))
    .filter(row => !filters.kssOnly || row.presentation?.isKss === true)
    .filter(row => !filters.statusOnly || Boolean(row.qadStatus))
    .sort((left, right) => {
      const priority = (severity: string) => severity === 'CriticalShort' ? 0 : severity === 'FutureShort' ? 1 : severity === 'SafetyStockShort' ? 2 : severity === 'SafetyStockUnavailable' ? 3 : 4;
      const first = (row: LongTermShortageRow) => row.firstShortDate ?? row.firstAtRiskDate ?? '9999-12-31';
      const compareQuantity = (a: string, b: string) => {
        const [aw, af = ''] = a.split('.'), [bw, bf = ''] = b.split('.');
        return aw.length - bw.length || aw.localeCompare(bw) || af.padEnd(Math.max(af.length, bf.length), '0').localeCompare(bf.padEnd(Math.max(af.length, bf.length), '0'));
      };
      const deepest = (row: LongTermShortageRow) => row.episodes.reduce((largest, e) => compareQuantity(String(e.maximumShortage), largest) > 0 ? String(e.maximumShortage) : largest, '0');
      const recovery = (row: LongTermShortageRow) => row.episodes[0]?.firstRecoveryDate ?? '9999-12-31';
      const difference = filters.sort === 'mostUrgent' ? priority(left.severity) - priority(right.severity) || first(left).localeCompare(first(right))
        : filters.sort === 'firstShortage' ? first(left).localeCompare(first(right))
          : filters.sort === 'deepestShortage' ? compareQuantity(deepest(right), deepest(left))
            : filters.sort === 'recoveryDate' ? recovery(left).localeCompare(recovery(right))
              : filters.sort === 'buyerPlanner' ? (left.buyerPlannerCode ?? 'ZZZZ').localeCompare(right.buyerPlannerCode ?? 'ZZZZ') : 0;
      return difference || left.componentPart.localeCompare(right.componentPart);
    });
}

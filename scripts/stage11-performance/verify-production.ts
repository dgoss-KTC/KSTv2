import { readFileSync, readdirSync } from 'node:fs';
import { strict as assert } from 'node:assert';
import type { LongTermShortageRow, LongTermShortagesScreen } from '../../src/frontend/src/api/longTermShortagesApi';
import { filterLongTermShortages, EMPTY_LONG_TERM_SHORTAGES_FILTERS, formatLongTermQuantity, type LongTermShortagesFilters } from '../../src/frontend/src/longTermShortages/longTermShortagesPresentation';

// Frozen indexed-screen comparator, retained independently of the new production path.
function originalFilter(rows: LongTermShortageRow[], filters: LongTermShortagesFilters) {
  const component = filters.component.trim().toLocaleLowerCase();
  const planner = filters.planner.trim().toLocaleLowerCase();
  const quantity = (a: string, b: string) => {
    const [aw, af = ''] = a.split('.'), [bw, bf = ''] = b.split('.');
    return aw.length - bw.length || aw.localeCompare(bw) || af.padEnd(Math.max(af.length, bf.length), '0').localeCompare(bf.padEnd(Math.max(af.length, bf.length), '0'));
  };
  const deepest = (r: LongTermShortageRow) => r.episodes.reduce((a, e) => quantity(String(e.maximumShortage), a) > 0 ? String(e.maximumShortage) : a, '0');
  const first = (r: LongTermShortageRow) => r.firstShortDate ?? r.firstAtRiskDate ?? '9999-12-31';
  const priority = (s: string) => s === 'CriticalShort' ? 0 : s === 'FutureShort' ? 1 : s === 'SafetyStockShort' ? 2 : s === 'SafetyStockUnavailable' ? 3 : 4;
  return rows.filter(r => filters.showAll || ['CriticalShort', 'FutureShort', 'SafetyStockShort'].includes(r.severity))
    .filter(r => !component || r.componentPart.toLocaleLowerCase().includes(component))
    .filter(r => !planner || (r.planner ?? '').toLocaleLowerCase().includes(planner))
    .filter(r => !filters.kssOnly || r.presentation?.isKss === true)
    .filter(r => !filters.statusOnly || Boolean(r.qadStatus))
    .sort((a, b) => (filters.sort === 'mostUrgent' ? priority(a.severity) - priority(b.severity) || first(a).localeCompare(first(b))
      : filters.sort === 'firstShortage' ? first(a).localeCompare(first(b))
        : filters.sort === 'deepestShortage' ? quantity(deepest(b), deepest(a))
          : filters.sort === 'recoveryDate' ? (a.episodes[0]?.firstRecoveryDate ?? '9999-12-31').localeCompare(b.episodes[0]?.firstRecoveryDate ?? '9999-12-31')
            : filters.sort === 'buyerPlanner' ? (a.buyerPlannerCode ?? 'ZZZZ').localeCompare(b.buyerPlannerCode ?? 'ZZZZ') : 0)
      || a.componentPart.localeCompare(b.componentPart));
}
const directory = 'scripts/stage11-performance/captures';
let comparisons = 0, captures = 0;
for (const file of readdirSync(directory).filter(f => f.endsWith('-production-api.json') && !f.includes('-live-'))) {
  const control = JSON.parse(readFileSync(`${directory}/${file.replace('-production', '-control')}`, 'utf8'));
  const screen: LongTermShortagesScreen = JSON.parse(readFileSync(`${directory}/${file}`, 'utf8'));
  for (const p of ['snapshotId', 'refreshDate', 'isStale', 'warning', 'acquiredAtUtc', 'consistencyMode'] as const) assert.deepEqual(screen[p], control[p]);
  for (const [key, mode] of [['rows', 'confirmed'], ['allReceiptsRows', 'all']] as const) {
    const original: LongTermShortageRow[] = control[key];
    const candidate = screen.components.map(r => ({ ...r, ...r[mode] }));
    assert.equal(candidate.length, original.length);
    for (const row of original) {
      const other = candidate.find(r => r.componentPart === row.componentPart)!;
      for (const p of ['unitOfMeasure', 'qadStatus', 'description', 'planner', 'buyerPlannerCode', 'openingQoh', 'safetyStock', 'severity', 'firstShortDate', 'firstAtRiskDate', 'dataQualityWarning'] as const) assert.deepEqual(other[p], row[p]);
      assert.equal(other.isKss, row.presentation?.isKss ?? false);
      assert.equal(other.openingDisplay, formatLongTermQuantity(row.openingQoh, row.unitOfMeasure));
      assert.deepEqual(other.ending, row.weeks.map(w => w.projectedQoh));
      assert.deepEqual(other.endingDisplay, row.weeks.map(w => formatLongTermQuantity(w.projectedQoh, row.unitOfMeasure)));
      assert.deepEqual(other.weeklySeverity, row.weeks.map(w => w.severity));
      assert.deepEqual(screen.weeks.map(w => w.weekStart), row.weeks.map(w => w.weekStart));
      assert.deepEqual(screen.weeks.map(w => w.weekNumber), row.weeks.map(w => w.weekNumber));
    }
    for (const sort of ['mostUrgent', 'firstShortage', 'deepestShortage', 'recoveryDate', 'buyerPlanner', 'component'] as const)
    for (const showAll of [true, false]) for (const kssOnly of [true, false]) for (const statusOnly of [true, false])
    for (const search of ['', original[0].componentPart.slice(0, 3)]) {
      const filters = { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, sort, showAll, kssOnly, statusOnly, component: search, planner: search ? (original[0].planner ?? '').slice(0, 3) : '' };
      assert.deepEqual(filterLongTermShortages(candidate, filters).map(r => r.componentPart), originalFilter(original, filters).map(r => r.componentPart));
      comparisons++;
    }
  }
  captures++;
}
assert.equal(captures, 12);
console.log(JSON.stringify({ captures, modes: 2, screenFieldsEquivalent: true, filterSortComparisons: comparisons }));

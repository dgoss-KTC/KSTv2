import { readFileSync, readdirSync } from 'node:fs';
import { strict as assert } from 'node:assert';
import { expandScreen } from './compact-adapter';
import type { LongTermShortageRow } from '../../src/frontend/src/api/longTermShortagesApi';
import { EMPTY_LONG_TERM_SHORTAGES_FILTERS } from '../../src/frontend/src/longTermShortages/longTermShortagesPresentation';
import { filterLongTermShortages } from './legacyScreenPresentation';

const directory = 'scripts/stage11-performance/captures';
let comparisons = 0;
for (const file of readdirSync(directory).filter(f => f.endsWith('-compact-api.json'))) {
  const control: { rows: LongTermShortageRow[]; allReceiptsRows: LongTermShortageRow[] } = JSON.parse(readFileSync(`${directory}/${file.replace('-compact', '-control')}`, 'utf8'));
  const expanded = expandScreen(JSON.parse(readFileSync(`${directory}/${file}`, 'utf8')));
  for (const key of ['rows', 'allReceiptsRows'] as const) {
    const original = control[key]; const candidate = expanded[key];
    for (const row of original) {
      const other = candidate.find(r => r.componentPart === row.componentPart)!;
      for (const property of ['unitOfMeasure', 'qadStatus', 'description', 'planner', 'buyerPlannerCode', 'openingQoh', 'safetyStock', 'severity', 'firstShortDate', 'firstAtRiskDate', 'dataQualityWarning'] as const)
        assert.deepEqual(other[property], row[property]);
      assert.equal(other.presentation!.isKss, row.presentation?.isKss ?? false);
      assert.deepEqual(other.weeks, row.weeks.map(w => ({ weekNumber: w.weekNumber, weekStart: w.weekStart, projectedQoh: w.projectedQoh, severity: w.severity })));
    }
    for (const sort of ['mostUrgent', 'firstShortage', 'deepestShortage', 'recoveryDate', 'buyerPlanner', 'component'] as const)
    for (const showAll of [true, false])
    for (const kssOnly of [true, false])
    for (const statusOnly of [true, false])
    for (const search of ['', original[0].componentPart.slice(0, 3)]) {
      const filters = { ...EMPTY_LONG_TERM_SHORTAGES_FILTERS, sort, showAll, kssOnly, statusOnly, component: search, planner: search ? (original[0].planner ?? '').slice(0, 3) : '' };
      assert.deepEqual(filterLongTermShortages(candidate, filters).map(r => r.componentPart), filterLongTermShortages(original, filters).map(r => r.componentPart));
      comparisons++;
    }
  }
}
console.log(JSON.stringify({ completeScreenFieldComparisons: true, filterSortComparisons: comparisons }));

/**
 * TEST SUPPORT ONLY — not imported by application code. Maps the accepted Stage 14 deterministic
 * fixture's adjustment-independent weekly, Falldown and Undated expectations into the 14.3
 * PlanningWorkbookResponseDto shape. Quarter expectations are NOT mapped: tests use them only as
 * assertions against the frontend quarter calculation.
 */
import type { PlanningWorkbook } from './planningWorkbookSummary';

interface FixtureBucket { weekLabel?: string; so: string; soDollars: string; mps: string; mpsDollars: string; forecast: string; estOnHand: string }
interface FixturePart {
  part: string; selectedPrice: string; priced: boolean;
  undated: { so: string; soDollars: string; lineCount: number };
  falldown: FixtureBucket; weeks: FixtureBucket[];
}
export interface Stage14Fixture {
  scope: { workspaceAssignmentId: string; mpsSnapshotId: string; site: string; resolvedParts: string[] };
  calendar: { today: string; horizonWeeks: number; weekLabels: string[] };
  inputs: { netQoh: Record<string, string>; priceTiers: Record<string, { minQty: string; price: string }[]> };
  expected: { parts: (FixturePart & { quarters: FixtureQuarter[] })[] };
  fiscalExceptionCase: { fiscalSettings: { anchorFiscalYear: number; anchorStartDate: string; exceptions: { fiscalYear: number; extraWeekPeriod: number }[] }; expectedP1Quarters: FixtureQuarter[] };
}
export interface FixtureQuarter {
  quarter: string; includesFalldown: boolean; so: string; soDollars: string; mps: string; mpsDollars: string; estOnHand: string;
}

function bucket(kind: 'falldown' | 'weekly', value: FixtureBucket) {
  return {
    kind, weekLabel: value.weekLabel ?? null,
    salesOrderQuantity: value.so, salesOrderDollars: value.soDollars,
    mps: value.mps, mpsDollars: value.mpsDollars,
    forecast: value.forecast, estimatedOnHand: value.estOnHand,
  };
}

export function fixtureToWorkbook(fixture: Stage14Fixture): PlanningWorkbook {
  const byPart = new Map(fixture.expected.parts.map(p => [p.part, p]));
  return {
    workspaceId: fixture.scope.workspaceAssignmentId,
    site: fixture.scope.site,
    workbookSnapshotId: 'fixture-workbook',
    mpsSnapshotId: fixture.scope.mpsSnapshotId,
    openOrdersSnapshotId: 'fixture-open-orders',
    acquiredAtUtc: `${fixture.calendar.today}T16:00:00Z`,
    priceEffectiveDate: fixture.calendar.today,
    priceSelectionPolicy: 'firstTier',
    today: fixture.calendar.today,
    horizonWeeks: fixture.calendar.horizonWeeks,
    isStale: false,
    warning: null,
    weekLabels: fixture.calendar.weekLabels,
    minimumSelectableHorizonWeeks: 12,
    maximumHorizonWeeks: 12,
    parts: fixture.scope.resolvedParts.map(name => {
      const part = byPart.get(name)!;
      return {
        part: name,
        description: null,
        netQuantityOnHand: fixture.inputs.netQoh[name],
        priceTiers: (fixture.inputs.priceTiers[name] ?? []).map(t => ({ minimumOrderQuantity: t.minQty, unitPrice: t.price })),
        selectedPrice: part.selectedPrice,
        isPriced: part.priced,
        falldown: bucket('falldown', part.falldown),
        weeks: part.weeks.map(w => bucket('weekly', w)),
        undated: { salesOrderQuantity: part.undated.so, salesOrderDollars: part.undated.soDollars, lineCount: part.undated.lineCount },
      };
    }),
  };
}

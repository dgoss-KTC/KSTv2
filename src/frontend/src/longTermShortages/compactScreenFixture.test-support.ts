import type { LongTermShortageRow, LongTermShortageScreenRow, LongTermShortageProjectionDetail } from '../api/longTermShortagesApi';
import { formatLongTermQuantity } from './longTermShortagesPresentation';

/** Test-fixture migration only. Production consumes the backend's compact response directly. */
export function compactFixture(row: LongTermShortageRow): LongTermShortageScreenRow {
  const mode = { severity: row.severity, firstShortDate: row.firstShortDate, firstAtRiskDate: row.firstAtRiskDate,
    maximumShortage: row.episodes[0]?.maximumShortage ?? 0, firstRecoveryDate: row.episodes[0]?.firstRecoveryDate ?? null,
    ending: row.weeks.map(w => w.projectedQoh), endingDisplay: row.weeks.map(w => formatLongTermQuantity(w.projectedQoh, row.unitOfMeasure)),
    weeklySeverity: row.weeks.map(w => w.severity) };
  return { componentPart: row.componentPart, unitOfMeasure: row.unitOfMeasure, qadStatus: row.qadStatus,
    description: row.description, planner: row.planner, buyerPlannerCode: row.buyerPlannerCode,
    openingQoh: row.openingQoh, openingDisplay: formatLongTermQuantity(row.openingQoh, row.unitOfMeasure),
    safetyStock: row.safetyStock, dataQualityWarning: row.dataQualityWarning, isKss: row.presentation?.isKss ?? false,
    confirmed: mode, all: mode, ...mode };
}

export function projectionFixture(row: LongTermShortageRow): LongTermShortageProjectionDetail {
  return { snapshotId: 'snap-1', componentPart: row.componentPart, refreshDate: '2026-10-05', acquiredAtUtc: '2026-10-05T12:00:00Z',
    consistencyMode: 'PRO2_READ_UNCOMMITTED', demandParentParts: row.demandParentParts,
    pastGrossRequirements: row.past.grossRequirements, overdueReceipts: row.past.overdueReceipts,
    adjustedOpeningQoh: row.past.projectedQoh, effectivePmCode: row.effectivePmCode,
    manufacturingLeadWorkingDays: row.manufacturingLeadWorkingDays, manufacturerItem: row.presentation?.manufacturerItem ?? null };
}

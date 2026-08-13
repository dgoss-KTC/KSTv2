# KST v2 — Stage 7 Work Orders and Kitting Data Inventory

**Status:** Implementation-confirmed (post 7D.0–7D.12)
**Stage:** 7 — Phase 4: Work Orders and Kitting
**Capability:** MPS-bucket → Work Order drill-down, Kitting %, material issue detail, bounded manufactured-subassembly candidate navigation
**Primary schema reference:** `qadpro2-data-map.md` / `.json` / `.yaml`
**Related documents:** `KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md` (accepted business rules); `KST_v2_STAGE_7_IMPLEMENTATION_PLAN.md` (checkpoint history); `STAGE_7_REAL_DATA_VALIDATION.md` (live-QAD findings)

---

## 1. Purpose

This document lists the QAD source tables/fields Stage 7 reads, their role, and the normalized business concepts they produce. It records implementation-confirmed mappings, not just discovery-phase assumptions — every row below matches the actual SQL in `Kst.Integrations.Qad.WorkOrders`.

---

## 2. Source authority model

| Table | Role | Authority |
|---|---|---|
| `wo_mstr` | Work-order header identity, status, quantities, dates | Authoritative for every Stage 7 work-order card field |
| `wod_det` | Work-order material/issue lines | Authoritative for Kitting % and material grid detail |
| `pt_mstr` | Component description (`pt_desc1`) and manufactured/purchased identity (`pt_pm_code`) | Authoritative informational source; joined only, never queried standalone for Stage 7 |

Stage 7 never writes to QAD. All three tables are read through the existing `QadConnectionFactory.OpenAsync` (Windows-integrated auth, `READ UNCOMMITTED`, matching every other integration reader in this repository).

Stage 7 does not introduce a new site→domain resolution mechanism — it reuses `QadSiteDomainMap.Resolve(site)`, the same one used by MPS and Part Info.

---

## 3. Top-level Work Order membership (bucket → WOs)

Stage 7 does **not** re-derive bucket membership from `wo_due_date`/`wo_rel_date`. The MPS snapshot already retains `MpsWorkOrderRef(WorkOrderId, State)` per bucket (built by `MpsScheduleBuilder.BuildBucket` in Stage 5). `WorkOrderDrilldownService.GetBucketWorkOrdersAsync` reuses those retained WOIDs, filters to `Allocating`/`Frozen`/`Released` states, and only then reads `wo_mstr` for the eligible WOIDs (`QadWorkOrderSummaryReader.ReadByWoidsAsync`).

`Planned` and `Explicitly Scheduled` MPS-only rows never have a corresponding Stage 7 Work Order card — no fetch is attempted for them.

---

## 4. Work Order card field inventory

| Business field | Source | Grain | Notes |
|---|---|---|---|
| `PartNumber` | `wo_mstr.wo_part` | WO | Parent part manufactured by this WO |
| `Woid` | `wo_mstr.wo_lot` | WO | Scheduler-facing WO identity (see §5) |
| `Status` | `wo_mstr.wo_status` | WO | Restricted to `A`/`F`/`R` at the SQL boundary; any other value throws (query already guarantees the set) |
| `OrderedQuantity` | `wo_mstr.wo_qty_ord` | WO | — |
| `CompletedQuantity` | `wo_mstr.wo_qty_comp` | WO | — |
| `OpenQuantity` | Derived: `OrderedQuantity - CompletedQuantity` | WO | No separate QAD field; confirmed with the project owner that none is required |
| `ReleaseDate` | `wo_mstr.wo_rel_date` | WO | Nullable |
| `DueDate` | `wo_mstr.wo_due_date` | WO | Nullable |
| `SalesOrder` | `wo_mstr.wo_so_job` | WO | `nvarchar(80)`; blank/whitespace normalized to `null` |
| `Kitting` | Derived, see §6 | WO | `KittingSummary` |

### Fields deliberately excluded from the card

| QAD field | Reason |
|---|---|
| `wo_nbr` (Work Order Number) | Not unique, reused, not scheduler-relevant for this workflow (see §5) |
| `wo_line` (Production Line) | Not part of the accepted card |
| Start Date (no direct WO-level field used) | Not part of the accepted card |
| PM Code | Normalized to `IsManufactured`; raw code never travels past the QAD integration boundary |

---

## 5. Work Order identity

The scheduler-facing identity is **WOID** (`wo_mstr.wo_lot`), not Work Order Number (`wo_nbr`). `wo_nbr` is not unique and is never displayed or modeled as identity anywhere in the Stage 7 stack (Domain, Application, API, or frontend).

---

## 6. Kitting % field inventory

Kitting is line-based (count of lines), not quantity-weighted. Computed via an `OUTER APPLY` against `wod_det` per WO, without materializing the lines:

| Business field | Source / derivation |
|---|---|
| `ApplicableLineCount` | `COUNT(*)` of `wod_det` rows for the WO where `wod_qty_req <> 0` |
| `FullyIssuedLineCount` | Of those, count where `wod_qty_iss >= wod_qty_req` |
| `KittingPercent` | `FullyIssuedLineCount / ApplicableLineCount × 100`; `null` (not `0`) when `ApplicableLineCount = 0` |

---

## 7. Work Order material line field inventory

Retrieved lazily per WOID (`QadWorkOrderMaterialReader.ReadAsync`), joining `wo_mstr` → `wod_det` (by `wod_domain = wo_domain AND wod_lot = wo_lot`) → `pt_mstr` (by `pt_domain = wod_domain AND pt_part = wod_part`, `LEFT JOIN`).

| Business field | Source | Notes |
|---|---|---|
| `ComponentPart` | `wod_det.wod_part` | — |
| `ComponentDescription` | `pt_mstr.pt_desc1` | Nullable (`LEFT JOIN`) |
| `RequiredQuantity` | `wod_det.wod_qty_req` | Rows where this is `0` are excluded in SQL (`wod_qty_req <> 0`) |
| `IssuedQuantity` | `wod_det.wod_qty_iss` | — |
| `VarianceQuantity` | Derived: `IssuedQuantity - RequiredQuantity` | — |
| `IssuedPercent` | Derived: `IssuedQuantity / RequiredQuantity × 100` | `null` when `RequiredQuantity = 0` (defensive; such rows are already excluded) |
| `IssueStatus` | Derived from `IssuedPercent`, see §8 | `null` when `IssuedPercent` is `null` |
| `IsManufactured` | `pt_mstr.pt_pm_code = 'M'` (case-insensitive, trimmed) | Raw PM Code never exposed |
| `IsFullyIssued` | Derived: `RequiredQuantity <> 0 && IssuedQuantity >= RequiredQuantity` | Exact 100% and over-issued both count as fully issued |

Component rows are never deduplicated — repeated `wod_det` rows for the same component part are preserved as separate material lines, matching the project owner's live-data validation.

---

## 8. Issue status thresholds

| Issued % | `WorkOrderIssueStatus` |
|---|---|
| `<= 95` | `UnderIssuedException` |
| `> 95` and `< 105` | `WithinExpectedRange` |
| `>= 105` | `OverIssuedException` |

Both 95% and 105% boundaries are inclusive to their exception side (an exact 95% or 105% reading is an exception, not within range).

---

## 9. Manufactured-subassembly candidate query fields

Same card projection as §4, queried by component part instead of explicit WOIDs (`QadWorkOrderSummaryReader.ReadCandidatesAsync` / `BuildCandidateQuery`):

```sql
WHERE wo.wo_domain = @Domain
  AND wo.wo_site = @Site
  AND wo.wo_part = @ComponentPart
  AND wo.wo_status IN ('A', 'F', 'R')
  AND ISNULL(wo.wo_bom_code, '') <> 'RMABOM'
ORDER BY wo.wo_due_date DESC, wo.wo_rel_date DESC, wo.wo_lot ASC
```

**Revised during 7D.11 live-QAD validation:** the original discovery-phase rule additionally bounded candidates to `wo_due_date <= immediate parent WO Due Date`. Live validation against real QADPRO2 data confirmed the filter behaved exactly as designed, but the project owner then elected to remove it entirely so schedulers see every eligible A/F/R candidate for the component, not just those that could theoretically complete before the parent's own due date. The `wo_due_date DESC` ordering is retained so the most recently-due candidates surface first. See `STAGE_7_REAL_DATA_VALIDATION.md` for the ground-truth SQL comparison that drove this decision, and `KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md` §7 for the current accepted rule.

`RMABOM` work orders (`wo_bom_code = 'RMABOM'`) are excluded from candidate results, consistent with existing MPS RMA-exclusion behavior.

---

## 10. Exclusions confirmed by implementation

| Exclusion | Where enforced |
|---|---|
| Non-A/F/R work-order statuses (`P`, `e`, `C`, etc.) | SQL `WHERE wo_status IN ('A','F','R')`, both query shapes |
| `RMABOM` work orders | SQL `ISNULL(wo_bom_code,'') <> 'RMABOM'`, both query shapes |
| Zero-required material lines (`wod_qty_req = 0`) | SQL `WHERE wod.wod_qty_req <> 0`, material query and Kitting `OUTER APPLY` |
| Work Order Number as identity | Never modeled past the QAD row itself; not selected into any DTO |

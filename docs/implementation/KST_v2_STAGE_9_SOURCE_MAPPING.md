# KST v2 Stage 9 — Source-Mapping Reconciliation & Validation

**Checkpoint:** 9.1
**Status:** COMPLETE / ACCEPTED — retained Stage 9.1 evidence; superseded current-status summaries are reconciled in `KST_v2_STAGE_9_CLOSEOUT.md`.
**Date:** 2026-09-03 (updated during Stage 9.3 to correct the `lad_det` WO identity mapping from live-QAD evidence)
**Scope:** Documentation-only. No production code, SQL readers, contracts, frontend, build configuration, dependencies, or database objects changed.

## 1. Purpose

This document records the Stage 9.1 source-mapping reconciliation and validation for the
Immediate Work-Order Shortages feature. It reconciles the source-mapping topics identified in the
Stage 9 implementation plan (`KST_v2_STAGE_9_IMPLEMENTATION_PLAN.md`) against:

- The accepted Stage 9A design (owner-provided specification).
- The legacy Progress 4GL reference (`docs/reference/ktshrtge11.txt`).
- The QAD data map (`docs/data/qadpro2-data-map.{md,json,yaml}`).
- Targeted read-only live-QAD validation queries (run 2026-09-02 and 2026-09-03 against `QADPRO2`
  on `KNWVM13`, Windows-integrated auth, SELECT-only).

This document also records **new source evidence** supplied by the owner: a production shortage
query that uses `qadpro2.dbo.lad_det` as firm/detail inventory allocation data. This evidence
exposed a conflict with the accepted Stage 9 allocation design; the owner resolved it on
2026-09-03 with a **hybrid allocation model** (documented in §3.7 and §6).

## 2. Validation Method

- Read-only live-QAD queries (SELECT-only) via a temporary .NET console project
  (`Microsoft.Data.SqlClient`, Windows-integrated auth, `Encrypt=false`,
  `ApplicationName="KST v2 Stage 9.1 validation"`), matching the established
  `QadConnectionStringFactory` conventions.
- No credentials exposed; no writes; no database objects created or modified.
- The temporary console project was created outside the repository and deleted after validation.
- The environment has no `sqlcmd`, `pyodbc`, or `dotnet-script`; the .NET-based path was the
  available read-only validation mechanism.

## 3. Source-Mapping Topics

### 3.1 PO Open Quantity / Cancellation / Confirmation / Tracking

**Status:** CONFIRMED (legacy + live-QAD)

| Mapping | Source | Evidence |
|---|---|---|
| PO open quantity | `pod_qty_ord - pod_qty_rcvd` | legacy `ktshrtge11` + live-QAD field confirmation |
| Cancelled PO-line exclusion | `pod_status <> 'c'` | live-QAD: cancelled = `c` |
| Closed PO-line exclusion | `pod_status <> 'x'` | live-QAD: closed = `X` (uppercase); case-insensitive comparison required |
| PO confirmation | `po_confirm` | legacy + live-QAD field confirmation |
| Tracking | `pod__chr06` | legacy + live-QAD field confirmation |
| KSS source fields | `po_mstr.po_sched`, `pod_det.pod_sched` | Fields confirmed; the final Stage 9 KSS authority is the independent effective `pod_det`/`po_mstr` relationship in §3.2, not `pod_sched` alone |

**Live-QAD evidence:** `pod_status` values are `c` (cancelled), `X` (closed), blank (open). All
required PO fields exist: `po_confirm`, `po_sched`, `po_stat`, `pod__chr06`, `pod__log01`,
`pod_qty_ord`, `pod_qty_rcvd`, `pod_sched`, `pod_status`, `pod_nbr`, `pod_due_date`.

### 3.2 KSS (Kitting / Shortage System)

**Status:** CONFIRMED (legacy + live-QAD; Stage 9.8 blocker correction)

- KSS/vendor-managed classification is an effective part/site supplier-schedule relationship, independent of a qualifying conventional open PO: matching `pod_det.pod_domain`, `pod_site`, and `pod_part`; `(pod_end_eff##1 IS NULL OR pod_end_eff##1 >= Today)`; matching `po_mstr` domain/number; `po_sched = 1`; and `(po_eff_to IS NULL OR po_eff_to >= Today)`.
- In the SQL Server mirror, Progress `pod_end_eff[1]` is `pod_end_eff##1` (`datetime`); `po_eff_to` is `datetime`; `po_sched` is `bit`.
- The conventional next-PO query remains separately qualified by positive open quantity and accepted line state. A KSS component can therefore have no qualifying conventional PO and still be KSS.
- KSS is **context, not shortage coverage** (per the accepted Stage 9A design).

### 3.2A Projected Component UOM

**Status:** CONFIRMED by owner direction — 2026-09-03

- Stage 9 projected requirement UOM is the resulting component's `pt_mstr.pt_um`.
- The Stage 8 `QadBomReader` retains this nullable part-master fact with each structural occurrence.
- It is applied only after BOM path extension and same-component consolidation to determine EA floor
  normalization. `ps_mstr.ps_comp_um` is not a Stage 9 normalization authority; Stage 9 performs no
  UOM conversion or relationship-UOM precedence.
- A missing `pt_mstr` row preserves the structural occurrence and carries null UOM. Later Stage 9
  orchestration must represent that as data trust uncertainty, not invent a unit.

### 3.3 Inventory-Status-to-Bucket Semantics

**Status:** CONFIRMED (live-QAD)

| Bucket | Predicate | Evidence |
|---|---|---|
| Usable on-hand | `is_status` = `Stock`/`stock` AND `is_nettable = 1` | live-QAD `is_mstr` enumeration |
| Transit | `is_status` = `TRAN` | live-QAD `is_mstr` enumeration |
| Inspection | `is_status` = `INSPECT` | live-QAD `is_mstr` enumeration |
| MRB (NCM) | `is_status` = `MRB` | live-QAD `is_mstr` enumeration |
| NCM Inspection | `is_status` = `NCMINSP` | live-QAD `is_mstr` enumeration |
| Non-Net | `is_nettable = 0` | live-QAD `is_mstr` |
| Expired/Expiring | `is_nettable = 1` AND `ld_expire <= today + icc_iss_days` | live-QAD `ld_det` + `icc_ctrl` |

**`MRB` and `INSPECT` are real inventory-status values** (confirmed by live-QAD enumeration;
relevant to the Stage 9 bucket mapping). The legacy reference (`ktshrtge11`) also shows Transit /
Inspection / NCM Inspection codes.

### 3.4 Issue Policy

**Status:** CONFIRMED (legacy + live-QAD) — **master fallback resolved**

The issue policy is a T/F flag resolved in this order (per legacy `ktshrtge11`):

1. Default `yes` (T).
2. If a `ptp_det` row exists for part + site → use `ptp_det.ptp_iss_pol` (site-specific, bit;
   live-QAD: 284,032 true / 1,159 false).
3. Else if a `pt_mstr` row exists for the part → use `pt_mstr.pt_iss_pol` (master fallback, bit;
   live-QAD: 445,510 true / 5,836 false).

**Live-QAD evidence:** Both `ptp_det.ptp_iss_pol` and `pt_mstr.pt_iss_pol` are `bit` columns.
**170,498 parts** have `pt_mstr.pt_iss_pol=1` with no site `ptp_det` row → the master fallback is
meaningful (not dead code). The previously-missing `pt_iss_pol` master-fallback mapping is now
resolved: it is `pt_mstr.pt_iss_pol`, added to the data map.

### 3.5 Issue Days (`icc_ctrl`)

**Status:** CONFIRMED (live-QAD)

- `icc_ctrl.icc_iss_days` exists as an integer (live-QAD: site values 7 and 1).
- Used by the legacy shortage logic (`ktshrtge11`) and the accepted Stage 9 Expired/Expiring rule.
- **`icc_ctrl` was genuinely missing from the data map** (now added to all 3 representations).

### 3.6 `in_mstr` Non-Net / MRB Fields

**Status:** CONFIRMED (live-QAD)

- `in_mstr.in_qty_nonet` exists (live-QAD field confirmation).
- No `in_qty_mrb` field exists (live-QAD: MRB must derive from `ld_det`).

### 3.7 Hard/Detail Allocation (`lad_det`) — NEW EVIDENCE

**Status:** CONFIRMED (live-QAD) — **conflict resolved by owner decision (§6)**

A production shortage query uses `qadpro2.dbo.lad_det` as firm/detail inventory allocation data.
This is new evidence not present in the data map, legacy reference, or backend.

**`lad_det` table purpose and required fields:**

- `lad_det` is the firm/detail inventory allocation table. It tracks which specific lots
  (`lad_lot`) at which locations (`lad_loc`) are allocated to which work order (`lad_nbr`, when
  `lad_dataset='wod_det'`) or sales order (`lad_dataset='sod_det'`) for which component
  (`lad_part`).
- Required fields: `lad_dataset`, `lad_nbr`, `lad_line`, `lad_site`, `lad_loc`, `lad_part`,
  `lad_lot`, `lad_qty_all`, `lad_qty_pick`, `lad_domain`.

**`lad_qty_all` semantics:**

- `lad_qty_all` is the hard/firm allocation quantity (the quantity of the specific lot allocated
  to the WO/PO).
- Sample: WO `32428265` has 4 hard allocations (104, 104, 6.93, 41.6 lots). Picked rows have
  `lad_qty_all = 0` and `lad_qty_pick > 0`.

**`lad_dataset = 'wod_det'` semantics:**

- `lad_dataset = 'wod_det'` means the allocation is for a work order detail (as opposed to
  `sod_det` for sales order detail).
- Live-QAD: `lad_dataset` values are `wod_det` (3629 rows) and `sod_det` (14 rows).

**Authoritative WO/component-operation join keys (corrected by Stage 9.3 live-QAD reconciliation):**

- For `lad_dataset='wod_det'`, `lad_nbr` is the **WOID**, matching `wod_det.wod_lot`, not the
  Work Order Number (`wod_nbr`). `lad_line` is the **operation number**, matching `wod_det.wod_op`.
  The complete join is domain + site + `lad_nbr = wod_lot` + `lad_line = wod_op` +
  `lad_part = wod_part`.
- `wod_det` has **no `wod_line` column** (confirmed by live-QAD: "Invalid column name
  'wod_line'"). `lad_line` must therefore be matched to `wod_op`, not to an invented line field.
- Targeted read-only QAD reconciliation on 2026-09-03 considered all positive hard allocations
  (`lad_dataset='wod_det'`, `lad_qty_all > 0`). The WOID/operation/component mapping matched
  **3,039 of 3,039** allocation rows, with **0 unmatched** and **0 duplicate/ambiguous matches**.
  The prior Work-Order-Number/component mapping (`lad_nbr = wod_nbr`, without `lad_line`) matched
  **0 of 3,039** rows. This establishes meaning rather than selecting a mapping merely by count.
- The WOID/operation mapping matched allocations across sites/statuses/types: AR (25 A, 1,361 R),
  KV (1,599 R), MN (18 R), and SW (36 R). The alternative mapping had no matches in any observed
  site/status/type grouping.

**Relationship between `lad_det` hard allocation and `wod_det.wod_qty_all`:**

- The hard allocation (`lad_det`) exists **independently** of the `wod_det` rows (WO `32428265`
  has hard allocations but NO `wod_det` rows).
- No site-AR WO/component has both `wod_det.wod_qty_all > 0` AND a `lad_det` hard allocation
  (live-QAD: 1334 soft rows, 1348 hard rows, but 0 overlapping).
- This suggests the soft allocation (`wod_qty_all`) and the hard allocation (`lad_qty_all`) are
  **separate mechanisms**, not a soft/hard split of the same quantity.

**Whether hard allocations remain part of physical `ld_qty_oh` until issue:**

- **YES — confirmed.** The physical on-hand (`ld_qty_oh`) for the allocated lots is
  1000 / 2400 / 675 / 1440, which **includes** the hard allocations of 104 / 104 / 6.93 / 41.6.
  The hard allocation is a reserved subset of `ld_qty_oh`, not removed from it.

**Usable hard-allocation classification (confirmed during Stage 9.3 reconciliation):**

- Stage 9.1 explicitly established normal usable inventory as `is_status = Stock` (case-insensitive)
  **and** `is_nettable = 1`; this was not inferred solely from nettable status.
- The 2026-09-03 read-only check of positive hard-allocation lots joined through the accepted
  `ld_det` -> `loc_mstr` -> `is_mstr` route found 2,994 allocation rows / 4,408,663.2007777121
  allocated quantity in `STOCK` + nettable and 22 rows / 5,385 quantity in `RIP` + nettable.
  The reader's exact `Stock` predicate deliberately includes only the former; `RIP` is not silently
  broadened into usable coverage. The established expiration and RMA exclusions also remain applied.

**How hard allocations should be removed from otherwise usable/free inventory:**

- Since the hard allocation is still part of `ld_qty_oh`, to get the "free" inventory, subtract
  the hard allocations from the usable inventory (at the lot level).
- `free = usable - (hard allocations for all WOs)`.

**How a WO's own hard allocation should be credited to that WO's remaining requirement:**

- The WO's own hard allocation should be credited to its remaining requirement.
- `remaining = required - issued - own hard allocation` (clamped; see §6.1).

**Behavior of hard allocations belonging to WOs outside the Stage 9 immediate window:**

- Hard allocations for WOs outside the immediate window are still subtracted from the free
  inventory (because they're reserved for those WOs), but they are **not** credited to the current
  WO's requirement. They remain authoritative regardless of the owning WO's window membership
  (§6.4).

**CONFLICT with the accepted Stage 9 design (resolved):**

- The original Stage 9 design used a **reconstructed** allocation (committed R-then-A sequential
  allocation) based on the WO status and ordering. QAD has **actual hard/detail allocations**
  (`lad_det`) that are more authoritative — the actual lots allocated to each WO.
- **Per the owner's instruction:** "Treat QAD hard/detail allocations as potentially more
  authoritative than reconstructed KST allocation. Do not implement an allocation rule yet if the
  evidence exposes a conflict with the accepted Stage 9 design; document the evidence and stop
  for owner review."
- **Resolution (owner decision, 2026-09-03):** The owner adopted a **hybrid allocation model** —
  QAD hard/detail allocations (`lad_det`) are authoritative and take precedence; KST's
  reconstructed R→A sequential allocation applies only to the residual free inventory after hard
  allocations are honored. See §6 for the full decision. No allocation rule is implemented in this
  checkpoint (documentation-only).

## 4. Data Map Updates

The following were added to `docs/data/qadpro2-data-map.{md,json,yaml}` (all 3 representations):

- **`icc_ctrl`** (new table, 3 fields: `icc_domain`, `icc_iss_days`, `icc_site`) — the QAD
  issue-days source.
- **`lad_det`** (new table, 10 fields) — the firm/detail inventory allocation table.
- **`pt_mstr.pt_iss_pol`** (new field on the existing `pt_mstr` table) — the master/part-level
  issue-policy fallback (bit).

**Metadata correction:** The original data map metadata was off by 1 table and 8 fields
(pre-existing discrepancy). At the conclusion of 9.1 it read **28 tables, 572 fields, 569
validated fields**. Stage 9.8 subsequently added two accepted KSS-effectivity fields; the current
data-map metadata is therefore **28 tables, 574 fields, 571 validated fields**.

## 5. Genuinely Missing Mappings

None remaining. The two previously-identified gaps have been resolved from evidence:

- **`icc_ctrl`** (issue days) — confirmed via live QAD and added to the data map (§3.5).
- **`pt_iss_pol` master fallback** (issue policy) — confirmed as `pt_mstr.pt_iss_pol` (bit) via
  legacy logic + live QAD, and added to the data map (§3.4).

## 6. Owner Decision — Hybrid Allocation Model (recorded 2026-09-03)

The `lad_det` allocation-authority conflict is **resolved** by an owner decision to adopt a
**hybrid allocation model** for Stage 9. The `pt_iss_pol` master-fallback mapping is also resolved
(§3.4). This section records the decision; it governs the Stage 9.2 domain calculations and the
Stage 9.4 orchestration.

### 6.1 QAD hard/detail allocations are authoritative

- `lad_det` hard allocations represent inventory already reserved to specific demand and take
  precedence over KST's reconstructed allocation.
- Hard-allocated inventory remains part of physical QOH until issue but is **not** freely available
  to other Work Orders.
- For a WO/component being evaluated:
  - Calculate Remaining Requirement using the already-accepted requirement rules.
  - Credit that WO's own usable hard allocation against its Remaining Requirement.
  - Clamp hard-allocation coverage so it cannot create negative demand or an inventory credit.

Conceptually:

```text
RemainingRequirement = max(Required - Issued, 0)
OwnHardCoverage      = min(RemainingRequirement, UsableHardAllocationToThisWoComponent)
UncoveredRequirement = max(RemainingRequirement - OwnHardCoverage, 0)
```

### 6.2 Free usable inventory excludes hard allocations

- Calculate physical usable inventory using the accepted Stage 9 inventory rules.
- Remove hard allocations associated with otherwise usable inventory from the shared free pool
  before KST performs any additional allocation.
- Do not double-count hard-allocated inventory as both WO coverage and free shared inventory.
- A hard allocation does **not** override inventory usability: inventory in an excluded/unusable
  state does not become usable merely because it is hard allocated.

### 6.3 Existing R→A allocation remains, but only for residual free inventory

After honoring QAD hard allocations:

1. Qualifying R WOs in the Stage 9 immediate population sequentially consume residual free usable
   inventory;
2. Qualifying A WOs in the Stage 9 immediate population consume next;
3. The resulting residual becomes the common advisory pool for uncommitted WOs;
4. Uncommitted WOs continue to evaluate independently and do not consume inventory from one
   another.

The existing accepted date-basis ordering and WO-ID tie-break rules apply inside the R and A tiers.

### 6.4 Hard allocations are not bounded by the Stage 9 analytical window (amendment)

- KST's reconstructed/sequential R/A allocation is limited to WOs **inside** the accepted Stage 9
  immediate window.
- Actual QAD hard allocations are authoritative current-inventory reservations **regardless of
  whether the owning WO is inside that window**.
- Therefore, moving an A/R WO outside the Stage 9 window prevents KST from analytically reserving
  additional free inventory for it, but does **not** make inventory already hard allocated to it in
  QAD available to other WOs.

### 6.5 Do not add `wod_qty_all` as separate coverage yet

- The current live evidence indicates the existing soft/general allocation quantity
  (`wod_det.wod_qty_all`) and `lad_det` hard/detail allocation are **separate mechanisms**.
- Do not combine `wod_qty_all` with hard allocation or use it as an additional Stage 9 coverage
  source without an explicitly accepted rule (avoids double-counting / invented precedence).

### 6.6 Provenance

- Carry hard-allocation quantity/provenance in the Stage 9 result model sufficiently to verify and
  explain the calculation. Whether `Hard Allocated Qty` becomes a visible detail-card field can be
  finalized during the UI checkpoint (9.6).

## 7. Verification

- No production code changed (no files under `src/`).
- No dependency files changed.
- No SQL/database objects created or modified.
- Data map updated consistently across all 3 representations (MD, JSON, YAML): added `icc_ctrl`,
  `lad_det` tables and the `pt_mstr.pt_iss_pol` field.
- JSON and YAML validated (parse correctly; table/field counts match metadata).
- The 9.1 closeout metadata is retained above as historical evidence; current metadata after the Stage 9.8 KSS field additions is 28 tables, 574 fields, 571 validated.
- Temporary .NET console project and Python scripts created outside the repository and deleted.

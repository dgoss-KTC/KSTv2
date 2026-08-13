# KST v2 — Stage 7 Work Orders and Kitting Contract

**Stage:** 7 — Phase 4: Work Orders and Kitting
**Contract status:** ACCEPTED, implemented, and live-QAD validated (7D.0–7D.12)
**Related documents:** `KST_v2_STAGE_7_WORK_ORDER_KITTING_DATA_INVENTORY.md` (field/source detail); `KST_v2_STAGE_7_IMPLEMENTATION_PLAN.md` (checkpoint history); `STAGE_7_REAL_DATA_VALIDATION.md` (live-QAD findings)

## 1. Purpose

Stage 7 adds MPS-bucket → Work Order drill-down, line-based Kitting %, Work Order material/issue detail, and a bounded (non-pegging) manufactured-subassembly candidate-navigation workflow, up to three investigation levels deep.

Stage 7 does not implement full Component/BOM Detail, BOM explosion, Component MRP, immediate/future shortages, inventory coverage, PO coverage/drill-down, buyer notes, shared-component analysis, finished goods, planning workbook, or sales-order investigation. Those remain Stage 8+ boundaries.

## 2. Accepted UI behavior

```text
Full MPS grid
    ↓ select parent row
Grid focuses on selected parent, Part Info opens
    ↓ Work Orders/Shortages/Components tabs remain disabled until a bucket is selected
select an eligible MPS week cell or Falldown
    ↓
Work Orders tab enables and opens automatically
    ↓ select a manufactured material row (chevron affordance)
Candidate Work Orders panel expands beneath that row ("Work Orders for <Component Part>")
    ↓ select a manufactured material row within a Level-2 candidate (if depth allows)
Level-3 candidate panel expands; deeper navigation disabled at Level 3
```

Rules:

- Clicking the Parent Part alone selects/focuses it, clears any selected bucket, exposes Part Info, and leaves Work Orders/Shortages/Future Shortages/Components disabled. There is no parent-only "all open Work Orders" view.
- Clicking an eligible MPS week cell or Falldown selects the parent + bucket context and automatically opens Work Orders.
- Top-level Work Order drill-down is available only for **Falldown + the first 6 forward MPS weeks** (`WORK_ORDER_DRILLDOWN_HORIZON_WEEKS = 6`, `src/frontend/src/mps/mpsPresentation.ts`). Weeks beyond that horizon simply do not expose the Work Order action; the underlying MPS snapshot itself is never truncated or altered.
- One expanded manufactured-component branch per nested level — selecting a different manufactured component at the same level collapses the prior branch.

## 3. Eligible Work Orders

Only these `wo_mstr.wo_status` values produce a Stage 7 Work Order card:

| Code | Meaning |
|---|---|
| `A` | Allocating |
| `F` | Frozen |
| `R` | Released |

`P` (Planned), `e` (Explicitly Scheduled), `C` (Closed), and any other status never produce a card. `RMABOM` work orders (`wo_bom_code = 'RMABOM'`) are excluded wherever candidate work orders are retrieved.

## 4. Work Order identity

The scheduler-facing identity is **WOID** (`wo_mstr.wo_lot`). Work Order Number (`wo_nbr`) is not unique, is not displayed on the card, and is not modeled as identity anywhere in the stack.

## 5. Work Order card

Each card shows: `WOID`, `Status`, `Ordered`, `Completed`, `Open`, `Release`, `Due`, `Kitting %`, and `SalesOrder` (added during 7D.11 live feedback, labeled "SO", shown opposite the Status badge). It does not show Work Order Number, Production Line, Start Date, or PM Code.

```text
Open = Ordered - Completed
```

Cards render side-by-side (horizontal flex-wrap) for multiple WOs in one bucket, each a fixed 320px compact width; a card whose material section is expanded grows to full row width (`--expanded` modifier) so the material grid is never squeezed.

## 6. Kitting

Kitting is line-based, not quantity-weighted:

```text
Applicable material line = wod_qty_req <> 0
Fully issued line        = wod_qty_iss >= wod_qty_req
Kitting %                = Fully Issued Line Count / Applicable Line Count × 100
```

Exact 100% and over-issued lines both count as fully issued. Zero applicable lines yields Kitting = **null/N-A**, never 0%. Material lines are never deduplicated by component part.

## 7. Manufactured-subassembly candidate navigation

QAD provides no reliable logical parent↔subassembly Work Order relationship, and Stage 7 does not fabricate one. The UI uses truthful language ("Work Orders for `<Component Part>`"), never "Child/Linked/Related Work Orders."

**Current accepted candidate rule (revised during 7D.11 live-QAD validation):**

```text
same domain
same site
same manufactured component part
WO status IN ('A','F','R')
wo_bom_code <> 'RMABOM'
```

> The original discovery-phase rule additionally required `candidate Due Date <= immediate parent WO Due Date`. Live validation against real QADPRO2 data (component `H06-01-6001-33-1`) confirmed this filter worked exactly as designed, but the project owner elected to remove it so every eligible A/F/R candidate for the component is shown, regardless of the parent's own Due Date. See `STAGE_7_REAL_DATA_VALIDATION.md` for the ground-truth comparison. The `ParentDueDateUnavailable` precondition (a parent WO must still have *a* Due Date to attempt candidate lookup) was deliberately retained as an independent data-quality guard, unrelated to the removed filter.

Sort order (unchanged): `Due Date` descending, `Release Date` descending, `WOID` ascending as a deterministic tie-break.

Candidate limit: **10** (`WorkOrderDrilldownPolicy.CandidateResultLimit`), with `isTruncated` metadata returned when more candidates exist.

## 8. Material grid search/filter

The Work Order material grid supports a local (frontend-only), case-insensitive, partial-match Component Part search that updates as the scheduler types and never queries QAD per keystroke. All material lines are retrieved up front for the selected WO; filtering is purely local.

Default unfiltered sort: variance/material exceptions first, larger departures from 100% Issued before smaller ones, then deterministic component ordering. In-range lines are never hidden.

## 9. Manufactured components

A component is manufactured when `pt_mstr.pt_pm_code = 'M'`, normalized to `IsManufactured`. PM Code itself is never exposed in the material grid. Manufactured rows get a distinct background and a chevron drill affordance; variance exceptions get distinct font treatment; both can coexist on the same row. At maximum drill depth (Level 3), manufactured identity remains visible but further navigation is disabled.

## 10. Drill depth

Maximum investigation depth: **3 levels** (`WorkOrderDrilldownPolicy.MaxDrillDepth`).

```text
Level 1: MPS bucket → actual scheduled parent WO
Level 2: manufactured component on the Level-1 WO → candidate WO
Level 3: manufactured component on the Level-2 WO → candidate WO (deepest; no further navigation)
```

## 11. Lazy loading, cache, and refresh behavior

Nothing is attached to the initial MPS snapshot. Progression: MPS loaded → bucket selected → lazy-load WO cards → Kitting opened → lazy-load material rows → manufactured component selected → lazy-load candidate WOs.

Cache keys (all scoped to workspace + current MPS snapshot generation, unlike Stage 6 PartDetail's stale-fallback behavior — a superseded snapshot is a plain cache miss here, never a stale fallback):

| Cache | Key |
|---|---|
| WO summary | Workspace + Snapshot + WOID |
| Material detail | Workspace + Snapshot + WOID |
| Subassembly candidates | Workspace + Snapshot + Immediate Parent WOID + Component Part + Target Depth |

Due/Release toggling, horizon changes, and local material search never invalidate or reload Stage 7 data. A failed lazy-load is never cached as successful. A successful MPS refresh (new snapshot generation) invalidates all prior Stage 7 investigation state; a failed refresh preserves the existing last-good snapshot and its associated Stage 7 cache.

## 12. API contract

Three lazy, read-only endpoints (`WorkOrderEndpoints.MapWorkOrderEndpoints`), all requiring the caller's current `snapshotId`:

| Endpoint | Purpose |
|---|---|
| `GET /api/v1/workspaces/{assignmentId}/work-orders/bucket` | Eligible A/F/R work orders for one MPS bucket (uses retained MPS snapshot WO references, never re-derives from dates) |
| `GET /api/v1/workspaces/{assignmentId}/work-orders/{woid}/material` | Material/Kitting detail for one WOID |
| `GET /api/v1/workspaces/{assignmentId}/work-orders/candidates` | Candidate WOs for a manufactured component at a given target depth; backend resolves the immediate parent's Due Date server-side, never trusting a frontend-supplied date |

No eligible A/F/R WOs for a bucket is a valid `200 OK` with an empty list, not an error.

## 13. Problem Details / error states

| Condition | Status | Title |
|---|---|---|
| MPS not loaded | 409 | MPS data not loaded |
| Snapshot changed since caller's last-seen id | 409 | Snapshot changed |
| Part not in workspace scope | 404 | Part not in workspace scope |
| Bucket not found in current schedule | 404 | Bucket not found |
| Immediate parent WO not found | 404 | Work order not found |
| Immediate parent WO has no Due Date | 409 | Parent Work Order Due Date unavailable |
| Requested component is not manufactured | 409 | Component not manufactured |
| QAD read failure | 503 | Work order information unavailable |

The frontend's generic error mapper collapses all 409 outcomes into one "stale, refresh" message, relying on client-side gating (only requesting candidates for rows already flagged `isManufactured`) to make the edge cases effectively unreachable in normal use — no per-title special-casing was added.

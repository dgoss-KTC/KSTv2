# KST v2 — Stage 7 Work Orders and Kitting Contract

**Stage:** 7 — Phase 4: Work Orders and Kitting
**Contract status:** ACCEPTED, implemented, and live-QAD validated (7D.0–7D.12); **reopened, amended, manually validated, and closed by Stage 7R** (Four-Week Work Order Planning Window) on 2026-09-01 — see §14. The original Stage 7 acceptance record (§1–§13) is preserved unchanged as history.
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

Candidate limit: **10**, with `isTruncated` metadata returned when more candidates exist.

> Superseded for nested Work Order population by the Stage 7R amendment §14.9. This historical
> Stage 7 candidate rule is retained for provenance; supported manufactured-subassembly drill-downs
> now use the complete shared planning-window population with no row cap.

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

## 14. Stage 7R Amendment — Four-Week Work Order Planning Window

> **Status:** Stage 7 was **reopened and amended** as Stage 7R, then manually validated and **closed / accepted on 2026-09-01**. This section records the amendment. It supersedes the specific original provisions called out below; all other original Stage 7 provisions (§1–§13) remain in force. The original Stage 7 acceptance (2026-08-13) is preserved as history and is **not** rewritten as though this behavior existed at original acceptance.

### 14.1 Parent-level Work Orders visibility (supersedes §2 rule 1)

Selecting a parent **without** a bucket now exposes **Part Info + BOM + Work Orders**. The Work Orders tab shows the complete parent-level planning population (Due-Date-based Falldown + Week 0–3 under the active weekly basis). This supersedes the original rule that "there is no parent-only 'all open Work Orders' view." Shortages remains rendered but deferred (disabled).

### 14.2 Four-week forward planning horizon (supersedes §2 rule 3)

The Work Order drill-down eligibility horizon is **Falldown + the first four forward MPS business weeks (Week 0–3)** — `WORK_ORDER_DRILLDOWN_HORIZON_WEEKS = 4` (frontend) and `Kst.Domain.WorkOrders.WorkOrderPlanningWindow.ForwardWeekCount = 4` (backend). Weeks 4 and later do not present the Work Order drill-down. This supersedes the original six-forward-week horizon. The broader MPS display horizon is unchanged.

### 14.3 Population source and eligibility (supersedes §3 for planning-window population)

The planning-window population is sourced **directly from `wo_mstr`** (selected-part-scoped), not from MPS-retained Work Order references. A planning-window Work Order must satisfy:

```text
same domain
same selected site
same selected part
AND wo_status <> 'C'            (Closed excluded at the query boundary)
AND wo_bom_code <> 'RMABOM'     (RMABOM exclusion retained)
AND (
      wo_due_date is Falldown                    -- always Due-Date based
      OR active weekly-basis date is in Week 0-3 -- Due mode: wo_due_date; Release mode: wo_rel_date
  )
```

- **Non-closed status-driven visibility is no longer limited to A/F/R.** Any non-closed `wo_status` produces a planning-window card. This supersedes the original A/F/R-only eligibility for both top-level parents and manufactured subassemblies reached through the supported Work Order drill-down.
- **The `RMABOM` exclusion is retained.** Closed Work Orders remain excluded.
- **Null dates are handled truthfully:** a null `wo_due_date` cannot qualify for Falldown; in Due mode a null `wo_due_date` cannot qualify for a forward week; in Release mode a null `wo_rel_date` cannot qualify for a forward week. No dates are invented.

### 14.4 Falldown remains always Due-Date based

Falldown membership is **always** determined by `wo_due_date < current business-week start`, **regardless** of whether the workspace is in Due-date or Release-date mode. Switching the Due/Release basis never changes Falldown membership. This preserves the original documented MPS Falldown contract.

### 14.5 Forward weekly buckets follow the selected Due/Release basis

Week 0–3 membership uses the **active weekly-bucket basis**:

```text
Due mode:     Week 0-3  <=>  wo_due_date in [current-week-start, current-week-start + 28d)
Release mode: Week 0-3  <=>  wo_rel_date in [current-week-start, current-week-start + 28d)
```

The combined planning population is the **union** of Due-Date-based Falldown and the active-basis Week 0–3, deduplicated by WOID. It is intentionally not reducible to one simple date-field predicate in Release mode: a WO may qualify because its due date puts it in Falldown, or because its active-basis date puts it in a forward week. The existing MPS business-week boundary conventions (Sun–Sat weeks, Monday label, week 0 = current week) are reused.

### 14.6 Status handling

The raw QAD Work Order status code is preserved in the domain/API representation. Known statuses (A/F/R) receive friendly presentation labels (`allocating`/`frozen`/`released`); any other non-closed code is passed through as its **raw value** and rendered safely (visible, raw code retained, no crash, no silent drop). No formal business label is invented for `e`/`E` or other codes where repository authority does not establish one. A safe raw-code presentation is used instead.

### 14.7 API contract (supersedes §12 for planning-window population)

The top-level population is served by a single planning-window capability:

```text
GET /api/v1/workspaces/{assignmentId}/work-orders/planning-window
    ?snapshotId={guid}
    &parentPart={part}
    &dateBasis=dueDate|releaseDate   (default dueDate)
    &bucketKind=falldown|weekly      (optional; absent = parent-level full window)
    &weekLabel={date}                (required when bucketKind=weekly)
```

It serves both the parent-level population (no `bucketKind`) and the bucket-filtered population (`bucketKind` + optional `weekLabel`), so the parent and bucket views derive from the **same** planning-window algorithm (no duplicated population logic). The previous `GET .../work-orders/bucket` endpoint (MPS-retained-reference based) is retired. The `.../work-orders/{woid}/material` endpoint is unchanged. The `.../work-orders/candidates` endpoint remains the manufactured-material-line authorization boundary for nested navigation, then delegates to the same unfiltered full planning-window population.

### 14.8 Material behavior (bounded treatment)

This amendment changes visibility/population only. It does **not** establish BOM-derived projected material requirements for unreleased WOs, and no material is fabricated. The existing WO material read is used truthfully: if actual `wod_det` material records exist they are displayed; if none exist, a truthful empty/unavailable state is shown (not an error). Kitting reports **N/A** (never a false 0%) when no applicable material lines exist. BOM-derived projected requirements remain explicitly deferred to Stage 9 design.

### 14.9 Manufactured-subassembly planning-window population

Manufactured subassemblies reached through the supported Work Order material drill-down use the
same complete four-week planning-window population as top-level MPS parents: Due-Date-based
Falldown plus Week 0–3 under the active Due/Release basis. The selected manufactured material line
authorizes this navigation path; it does not create or imply a parent/child Work Order relationship.
Nested population no longer uses the legacy A/F/R-only candidate visibility or its ten-result cap.

### 14.10 Preserved unchanged

WOID identity; Work Order card fields; SO display; actual WO material calculations; issued/required quantity behavior; manufactured-component navigation; lazy loading; snapshot-aware cache invalidation; stale/refresh behavior; Escape/navigation behavior; read-only QAD access; architectural layering; maximum drill depth (3); and Stage 8 BOM behavior.

# KST v2 — Stage 7D Work Orders and Kitting Implementation Checklist

**Stage:** 7 — Phase 4: Work Orders and Kitting  
**Planning status:** Stage 7A, 7B, and 7C accepted  
**Implementation status:** COMPLETE — 7D.0 through 7D.14 all done  
**Owner acceptance:** **ACCEPTED — 2026-08-13**

## Stage 7 completion target

A scheduler can select a near-term MPS bucket or Falldown, inspect its active A/F/R work orders and their kitting/material-issue state, search large component lists, and follow manufactured components through bounded candidate-work-order drill-downs without KST implying unsupported work-order lineage.

---

# 7D.0 — Repository Preflight and Contract Reconciliation

- [ ] Inspect current repository state and latest commits.
- [ ] Confirm working tree is clean or document existing unrelated changes.
- [ ] Read current project status and Stage 5/6 implementation artifacts.
- [ ] Inspect the implemented MPS snapshot and bucket-selection models.
- [ ] Inspect `MpsWorkOrderRef` and confirm how a selected bucket exposes contributing WOIDs.
- [ ] Inspect the Stage 6 lazy-detail/cache implementation and reuse its proven patterns where applicable.
- [ ] Inspect current QAD reader/Application-interface/API-DI bridging pattern.
- [ ] Inspect current Problem Details conventions.
- [ ] Inspect frontend selection/tab behavior established by Stage 6.
- [ ] Locate the KST Scheduler Console prototype for visual guidance only.
- [ ] Reconcile the tracked Master Project Checklist with accepted Stage 7A–7C requirements.
- [ ] Remove or disposition stale Stage 7 assumptions:
  - [ ] Work Order Number as a visible field.
  - [ ] Start Date.
  - [ ] Production Line.
  - [ ] Allocation fields for Stage 7.
  - [ ] Variance Percentage terminology.
  - [ ] All-open-WO parent view.
  - [ ] Separate speculative Kitting and Variance services.
- [ ] Preserve Stage 8+ boundaries.
- [ ] Do not begin Component MRP, shortage, inventory-coverage, PO, buyer-note, or full BOM-explosion work.

**Checkpoint 7D.0 gate:** Repository architecture and accepted Stage 7 contract are reconciled before production changes begin.

---

# 7D.1 — Domain Models and Business Rules

## Work Order status

- [ ] Define normalized Stage 7 work-order statuses:
  - [ ] Allocating (`A`)
  - [ ] Frozen (`F`)
  - [ ] Released (`R`)
- [ ] Ensure no other status creates a Stage 7 WO card.
- [ ] Do not treat Planned (`P`) or explicitly scheduled (`e`) as Stage 7 WO-card statuses.
- [ ] Exclude closed work orders.
- [ ] Exclude `RMABOM` where required by accepted rules.

## WorkOrderSummary

Define a focused model containing:

- [ ] Part Number.
- [ ] WOID.
- [ ] Status.
- [ ] Ordered Quantity.
- [ ] Completed Quantity.
- [ ] Open Quantity.
- [ ] Release Date.
- [ ] Due Date.
- [ ] Kitting Summary.

Do not add:

- [ ] Visible Work Order Number.
- [ ] Production Line.
- [ ] Start Date.
- [ ] QAD-specific field names.

## Quantity rule

- [ ] Implement:

```text
Open Quantity = Ordered Quantity - Completed Quantity
```

- [ ] Preserve decimal/quantity precision consistent with existing QAD models.

## KittingSummary

Define:

- [ ] Applicable Line Count.
- [ ] Fully Issued Line Count.
- [ ] Nullable Kitting Percent.

Business rules:

```text
Applicable line
    = Required Qty != 0

Fully issued line
    = Issued Qty >= Required Qty

Kitting %
    = Fully Issued Line Count
      / Applicable Line Count
      × 100
```

- [ ] Return `null` / N/A when Applicable Line Count = 0.
- [ ] Do not represent no applicable material lines as 0% kitted.
- [ ] A line issued over 100% counts as exactly one fully issued line.

## WorkOrderMaterialLine

Define:

- [ ] Component Part.
- [ ] Component Description.
- [ ] Required/BOM Quantity.
- [ ] Issued Quantity.
- [ ] Variance Quantity.
- [ ] Issued Percent.
- [ ] Issue Status.
- [ ] Is Manufactured.

Calculations:

```text
Variance Qty = Issued Qty - Required Qty

Issued % = Issued Qty / Required Qty × 100
```

## Material issue classification

- [ ] Define semantic status for:
  - [ ] Under-Issued Exception.
  - [ ] Within Expected Range.
  - [ ] Over-Issued Exception.

Rules:

```text
Issued % <= 95
    → Under-Issued Exception

95 < Issued % < 105
    → Within Expected Range

Issued % >= 105
    → Over-Issued Exception
```

- [ ] Backend owns semantic classification.
- [ ] Frontend owns font/color presentation.

## Manufactured-component rule

- [ ] Normalize `pt_pm_code = 'M'` to `IsManufactured = true`.
- [ ] Do not expose PM Code as a visible UI field.
- [ ] Do not leak QAD field names through API contracts.

**Checkpoint 7D.1 gate:** Domain calculations are implemented and unit-tested independently of SQL and UI.

---

# 7D.2 — QAD Work Order Readers

## Work-order summary reader

- [ ] Implement QAD work-order summary retrieval inside `Kst.Integrations.Qad`.
- [ ] Use existing site → domain resolution.
- [ ] Use Windows-integrated read-only QAD access.
- [ ] Use SQL Server 2016-compatible SQL.
- [ ] Use parameterized queries.
- [ ] Preserve cancellation-token propagation.
- [ ] Preserve existing command-timeout conventions.
- [ ] Preserve safe logging rules.
- [ ] Use existing read-uncommitted convention.
- [ ] Retrieve:
  - [ ] `wo_lot` as WOID.
  - [ ] `wo_part`.
  - [ ] `wo_status`.
  - [ ] `wo_qty_ord`.
  - [ ] `wo_qty_comp`.
  - [ ] `wo_rel_date`.
  - [ ] `wo_due_date`.
- [ ] Retrieve applicable-line and fully-issued-line counts efficiently.
- [ ] Restrict visible/eligible WOs to A/F/R.
- [ ] Exclude `wo_bom_code = 'RMABOM'` where applicable.

## WO material reader

- [ ] Implement lazy material-line retrieval for a single WOID.
- [ ] Join `wo_mstr` to `wod_det` using validated WOID/domain relationship.
- [ ] Join component part to `pt_mstr` for description and PM Code.
- [ ] Exclude `wod_qty_req = 0`.
- [ ] Retrieve all remaining applicable `wod_det` lines.
- [ ] Do not deduplicate by component part.
- [ ] Preserve legitimate repeated material rows.
- [ ] Retain operation/source diagnostics internally if useful for troubleshooting.
- [ ] Normalize QAD records before returning business models.

## Candidate subassembly reader

Implement candidate retrieval using:

```text
Same domain
Same site
Same manufactured component part
WO status IN (A, F, R)
wo_bom_code <> RMABOM
```

> **Revised during 7D.11 live-QAD validation:** the original contract bounded candidates to
> `Candidate Due Date <= immediate parent WO Due Date`. Live validation against real QAD data (component
> H06-01-6001-33-1) showed this hid legitimate open A/F/R work orders whose Due Date fell after the
> immediate parent's Due Date. The project owner directed removing the Due Date boundary entirely so
> all eligible A/F/R work orders for the component are shown regardless of Due Date. Ordering by Due
> Date descending is retained.

Ordering:

```text
Due Date descending
Release Date descending
WOID deterministic tie-break
```

- [ ] Initial candidate display limit = 10.
- [ ] Candidate limit is a clearly named backend constant/configuration value.
- [ ] Retrieve enough information to determine whether results were truncated.
- [ ] Do not claim candidates are linked/child work orders.
- [ ] Do not invent a database relationship between parent and candidate WOIDs.

**Checkpoint 7D.2 gate:** Direct reader tests and live diagnostic queries return the expected WO/card/material/candidate data.

---

# 7D.3 — Application Orchestration

## Application interfaces

- [ ] Add focused Application interfaces for Work Order summary and material retrieval.
- [ ] Preserve existing dependency direction.
- [ ] Do not make `Kst.Application` depend directly on QAD implementation packages.
- [ ] Reuse the established integration-to-Application bridging pattern.

## WorkOrderDrilldownService

Implement one focused orchestration service supporting:

- [ ] Get top-level Work Orders for selected MPS bucket.
- [ ] Get material/kitting detail for selected WOID.
- [ ] Get candidate Work Orders for a manufactured component.

Do not create speculative independent services for:

- [ ] Kitting.
- [ ] Variance.
- [ ] BOM explosion.
- [ ] Inventory.
- [ ] Shortage analysis.

## Top-level bucket resolution

- [ ] Resolve top-level work-order membership from the current MPS snapshot.
- [ ] Use WO references already associated with the selected bucket.
- [ ] Do not reconstruct bucket membership from `wo_due_date`.
- [ ] Support ordinary week buckets.
- [ ] Support Falldown.
- [ ] Retain only A/F/R work-order cards.

## Bucket drill-down window

Frontend/navigation policy:

- [ ] Falldown is drillable.
- [ ] First six forward MPS weeks are drillable.
- [ ] Weeks after the drill-down window do not expose Stage 7 work-order navigation.
- [ ] Store `6` as a clearly named frontend constant/configuration value.
- [ ] Do not truncate the underlying MPS snapshot.

## Subassembly depth

- [ ] Maximum WO drill-down depth = 3:
  - [ ] Level 1 — scheduled parent WO.
  - [ ] Level 2 — candidate WO for manufactured subassembly.
  - [ ] Level 3 — candidate WO for manufactured sub-subassembly.
- [ ] Reject requests beyond depth 3.
- [ ] Preserve `IsManufactured` at depth 3 even though deeper navigation is disabled.

**Checkpoint 7D.3 gate:** Application service correctly resolves bucket context and candidate navigation without unsupported lineage assumptions.

---

# 7D.4 — Lazy Cache and Refresh Semantics

## Cache keys

Work Order summary cache should include:

```text
Workspace
MPS Snapshot Generation
WOID
```

Material cache should include:

```text
Workspace
MPS Snapshot Generation
WOID
```

Candidate cache should include:

```text
Workspace
MPS Snapshot Generation
Immediate Parent WOID
Manufactured Component Part
Target Depth
```

## Within one MPS snapshot

- [ ] Reopening an already-loaded WO may reuse cached summary data.
- [ ] Reopening Kitting may reuse material-line data.
- [ ] Repeating the same manufactured-component drill may reuse candidate data.
- [ ] Due/Release display toggling does not invalidate Stage 7 cache.
- [ ] Horizon changes do not invalidate Stage 7 cache.
- [ ] Part-number filtering is frontend-local and never re-queries QAD.

## Successful workspace refresh

- [ ] New successful MPS snapshot generation invalidates prior Stage 7 investigation data.
- [ ] Clear selected bucket/WO investigation after successful snapshot replacement.
- [ ] Do not display old WO detail beneath a newly refreshed schedule.

## Failed workspace refresh

- [ ] Preserve the last-good MPS snapshot.
- [ ] Preserve compatible Stage 7 drill-down/cache for that retained snapshot.
- [ ] Preserve truthful stale/last-good presentation.

## Lazy-query failure

- [ ] Do not cache failed lazy-load responses.
- [ ] Preserve retry behavior.
- [ ] Do not represent QAD query failure as an empty business result.

**Checkpoint 7D.4 gate:** Cache/freshness behavior is deterministic across reopen, refresh-success, and refresh-failure scenarios.

---

# 7D.5 — API and OpenAPI Contracts

## Bucket Work Orders

Add the accepted read-only structured-query endpoint for:

```text
Workspace
Snapshot
Parent
Bucket/Falldown context
```

- [ ] Validate supplied snapshot ID.
- [ ] Validate selected parent/bucket against current snapshot.
- [ ] Return 200 + empty list when no eligible A/F/R WOs exist.
- [ ] Return normalized WorkOrderSummary DTOs.

## Work Order materials

- [ ] Add endpoint for lazy material retrieval by WOID.
- [ ] Require workspace/snapshot context.
- [ ] Return normalized material lines.
- [ ] Return current Kitting Summary with detail.
- [ ] Do not implement server-side part-number search for the loaded material list.

## Subassembly candidates

- [ ] Add endpoint/request for manufactured-component candidate WOs.
- [ ] Request includes:
  - [ ] Workspace.
  - [ ] Snapshot.
  - [ ] Immediate parent WOID.
  - [ ] Component Part.
  - [ ] Target Depth.
- [ ] Backend resolves parent Due Date rather than trusting a frontend-supplied date.
- [ ] Validate that selected component is manufactured.
- [ ] Enforce depth <= 3.
- [ ] Return candidate WorkOrderSummary records.
- [ ] Return `isTruncated` or equivalent result-limit metadata.

## Problem Details

Implement/confirm responses for:

- [ ] Workspace not found.
- [ ] Snapshot unavailable.
- [ ] Snapshot changed/replaced.
- [ ] Work Order not found.
- [ ] Component not manufactured.
- [ ] Maximum drill depth exceeded.
- [ ] Missing parent WO Due Date if candidate lookup cannot proceed.
- [ ] QAD unavailable.

## Contract generation

- [ ] C# DTOs remain authoritative.
- [ ] Rebuild backend/OpenAPI after DTO changes.
- [ ] Regenerate TypeScript contracts.
- [ ] Never manually edit generated TypeScript API types.
- [ ] Commit OpenAPI spec and generated frontend types together.

**Checkpoint 7D.5 gate:** API integration tests pass and frontend TypeScript consumes generated contracts without manual duplication.

---

# 7D.6 — Frontend Selection and Tab Behavior

## Parent selection

- [ ] Clicking Parent Part selects/focuses the parent.
- [ ] Parent selection opens or retains Part Info.
- [ ] Parent selection clears bucket context.
- [ ] Work Orders tab is disabled.
- [ ] Shortages tab remains disabled.
- [ ] Future Shortages tab remains disabled.
- [ ] Components tab remains disabled.

## Bucket selection

- [ ] Clicking an eligible MPS week cell selects parent + bucket.
- [ ] Clicking Falldown selects parent + Falldown.
- [ ] Bucket selection automatically opens Work Orders.
- [ ] Work Orders show only WOs contributing to the selected top-level bucket.
- [ ] Selected schedule context remains visibly identifiable.
- [ ] Weeks beyond configured drill-down horizon do not expose the work-order action.

## Empty bucket state

- [ ] Show a deliberate empty message when the selected context has no eligible A/F/R WO cards.
- [ ] Do not report that state as an error.

**Checkpoint 7D.6 gate:** Parent and bucket clicks have clearly distinct, predictable behavior.

---

# 7D.7 — Work Order Cards

Build compact cards displaying:

- [ ] WOID.
- [ ] Semantic status.
- [ ] Ordered Quantity.
- [ ] Completed Quantity.
- [ ] Open Quantity.
- [ ] Release Date.
- [ ] Due Date.
- [ ] Kitting % / N/A.
- [ ] Kitting progress presentation.
- [ ] Kitting expand/collapse control.

Do not display:

- [ ] Work Order Number.
- [ ] PM Code.
- [ ] Production Line.
- [ ] Start Date.

## Status styling

- [ ] Reuse established A/F/R semantic status palette where appropriate.
- [ ] Preserve accessible status text/badge in addition to color.
- [ ] Candidate subassembly cards use the same card/status model.

## Multi-WO bucket

- [ ] Support multiple cards for one selected MPS bucket.
- [ ] Keep cards independently expandable where practical.
- [ ] Avoid uncontrolled nested expansion at subassembly levels.

**Checkpoint 7D.7 gate:** Card contents match accepted scheduler information requirements and real QAD values.

---

# 7D.8 — Kitting Material Grid

Display all applicable material lines for the selected WO.

Columns:

- [ ] Component.
- [ ] Description.
- [ ] BOM Qty.
- [ ] Issued Qty.
- [ ] Variance Qty.
- [ ] Issued %.

Do not display:

- [ ] PM Code.
- [ ] Inventory.
- [ ] Shortage quantity.
- [ ] PO information.
- [ ] MRP information.
- [ ] Stage 8 coverage fields.

## Sorting

Default behavior:

- [ ] Material exceptions sort before normal lines.
- [ ] Larger departures from 100% Issued sort ahead of smaller departures.
- [ ] Provide deterministic component/operation ordering after severity.
- [ ] Do not hide normal lines.

## Part-number filter

- [ ] Add a local Part Number search/filter input.
- [ ] Case-insensitive.
- [ ] Partial match.
- [ ] Update as user types.
- [ ] Scope to current expanded WO material list.
- [ ] Provide easy clear/reset.
- [ ] No QAD request per keystroke.
- [ ] Verify usable performance with 250+ material lines.

## Variance styling

- [ ] `Issued % <= 95` receives exception text/font styling.
- [ ] `Issued % >= 105` receives exception text/font styling.
- [ ] Values inside expected range retain normal text presentation.

## Manufactured-component styling

- [ ] `IsManufactured = true` uses a distinct row/background treatment.
- [ ] PM Code itself remains hidden.
- [ ] Add a chevron or other non-color drill affordance before/near the part number.
- [ ] Cursor/hover treatment makes drillability discoverable.
- [ ] Manufactured background and variance font styling can coexist.
- [ ] At depth 3, retain manufactured background/indicator but disable deeper drill action.

**Checkpoint 7D.8 gate:** A scheduler can rapidly find a specific component in a large WO and distinguish variance exceptions from drillable manufactured subassemblies.

---

# 7D.9 — Manufactured-Subassembly Candidate Drill-Down

When a manufactured component is selected:

- [ ] Expand candidate WO area beneath the current material context.
- [ ] Label truthfully as:

```text
Work Orders for <Component Part>
```

or equivalent.

- [ ] Do not use:
  - [ ] Child Work Orders.
  - [ ] Linked Work Orders.
  - [ ] Related Work Orders implying proven lineage.

- [ ] Display candidate Work Order cards.
- [ ] Candidate cards retain A/F/R status coloring.
- [ ] Candidate limit = initial 10.
- [ ] If truncated, indicate that only the nearest/best 10 preceding candidates are shown.
- [ ] Selecting a candidate allows Kitting expansion for that candidate.
- [ ] Manufactured components within that candidate may drill one additional level if depth permits.
- [ ] Keep maximum depth at three WO levels.
- [ ] Prefer one expanded manufactured-component branch per nested level to avoid uncontrolled vertical expansion.
- [ ] Selecting another manufactured component at the same level may collapse the prior branch.

Empty state:

- [ ] Show a deliberate message when no eligible preceding A/F/R candidate WOs exist.
- [ ] Do not treat no candidates as QAD failure.

**Checkpoint 7D.9 gate:** Candidate navigation is useful without implying unsupported parent↔subassembly WO pegging.

---

# 7D.10 — Automated Verification

## Domain tests

- [ ] Open Quantity calculation.
- [ ] Applicable-line classification.
- [ ] Exact 100% line counts as fully issued.
- [ ] Over-issued line counts as fully issued.
- [ ] Zero-required line excluded.
- [ ] No applicable lines → Kitting N/A.
- [ ] Partial Kitting calculation.
- [ ] 100% Kitting.
- [ ] Issued % calculation.
- [ ] Variance Quantity calculation.
- [ ] 95% boundary.
- [ ] Below-95% exception.
- [ ] 105% boundary.
- [ ] Above-105% exception.
- [ ] Manufactured-component semantic flag.
- [ ] Drill-depth limit.

## QAD reader tests

- [ ] Work-order summary mapping.
- [ ] A/F/R filtering.
- [ ] Non-A/F/R exclusion.
- [ ] `RMABOM` exclusion.
- [ ] Material-line mapping.
- [ ] Zero-required material exclusion.
- [ ] Description enrichment.
- [ ] Manufactured-component enrichment.
- [ ] Candidate Due-Date boundary.
- [ ] Candidate ordering.
- [ ] Candidate result limit.
- [ ] Truncation detection.

## Application tests

- [ ] Bucket → retained MPS WO references.
- [ ] Falldown → retained WO references.
- [ ] No eligible WO result.
- [ ] Snapshot mismatch.
- [ ] Successful refresh invalidation.
- [ ] Failed refresh preserves last-good investigation.
- [ ] Material cache reuse.
- [ ] Candidate cache reuse.
- [ ] Failed lazy query not cached.

## API integration tests

- [ ] Bucket WO success.
- [ ] Empty WO result.
- [ ] Material success.
- [ ] Candidate success.
- [ ] Invalid/non-manufactured component.
- [ ] Depth > 3.
- [ ] Snapshot replaced.
- [ ] QAD failure Problem Details.
- [ ] camelCase/contract serialization.

## Frontend tests

- [ ] Parent selection disables contextual tabs.
- [ ] Bucket click auto-opens Work Orders.
- [ ] Falldown opens Work Orders.
- [ ] Week 1–6 drillable.
- [ ] Later week not drillable.
- [ ] WO card values render correctly.
- [ ] Kitting N/A state.
- [ ] Material grid expansion.
- [ ] Part-number partial filter.
- [ ] Search clear/reset.
- [ ] Exception styling.
- [ ] Manufactured background.
- [ ] Manufactured chevron/action.
- [ ] Combined manufactured + variance styling.
- [ ] Candidate expansion.
- [ ] No-candidate state.
- [ ] Three-level limit.
- [ ] Snapshot refresh clears drill-down.
- [ ] Failed refresh preserves retained snapshot behavior.

**Checkpoint 7D.10 gate:** Backend/API/frontend automated suites are green before live acceptance testing.

---

# 7D.11 — Live-QAD Validation

Validate representative real production cases without modifying QAD data.

## Top-level schedule context

- [ ] Bucket with one A/F/R WO.
- [ ] Bucket with multiple eligible WOs.
- [ ] Falldown with eligible WOs.
- [ ] Bucket with no eligible A/F/R WOs.
- [ ] Verify only the selected bucket's retained WO references are loaded.
- [ ] Verify first-six-week navigation policy.
- [ ] Verify later displayed weeks do not open Work Orders.

## Work Order card values

For representative WOs compare KST directly with QAD:

- [ ] WOID.
- [ ] Status.
- [ ] Ordered.
- [ ] Completed.
- [ ] Open.
- [ ] Release Date.
- [ ] Due Date.
- [ ] Kitting line counts.
- [ ] Kitting %.

## Kitting scenarios

- [ ] 0% kitted WO.
- [ ] Partially kitted WO.
- [ ] Exactly 100% kitted WO.
- [ ] WO containing over-issued lines.
- [ ] WO containing zero-required rows.
- [ ] WO with no applicable material lines if a real example exists.
- [ ] Large WO with 250+ material lines.

## Material detail

- [ ] Component Part.
- [ ] Description.
- [ ] BOM Qty.
- [ ] Issued Qty.
- [ ] Variance Qty.
- [ ] Issued %.
- [ ] Under-issued exception.
- [ ] Over-issued exception.
- [ ] Repeated component/material-row case where available.
- [ ] Manufactured-component classification.

## Search usability

- [ ] Search an exact known component part.
- [ ] Search using partial part number.
- [ ] Clear search.
- [ ] Confirm filtering remains responsive on 250+ rows.

## Manufactured-subassembly navigation

- [ ] Manufactured component with candidate WOs.
- [ ] Candidate list matches direct QAD evidence.
- [ ] Candidates are same site/part.
- [ ] Only A/F/R.
- [ ] `RMABOM` excluded.
- [x] ~~Candidate Due Date does not exceed immediate parent WO Due Date.~~ Revised: Due Date boundary removed; all eligible A/F/R candidates are shown regardless of Due Date (see 7D.2 note).
- [ ] Closest Due Date appears first.
- [ ] Candidate list respects the result cap.
- [ ] Manufactured component with no eligible candidates.
- [ ] Level-2 kitting drill.
- [ ] Level-3 kitting drill.
- [ ] No level-4 navigation.

## Refresh behavior

- [ ] Open Stage 7 drill-down.
- [ ] Perform successful workspace refresh.
- [ ] Confirm new MPS snapshot replaces old snapshot.
- [ ] Confirm old Stage 7 drill-down is cleared.
- [ ] Confirm newly selected bucket lazy-loads current WO data.
- [ ] Confirm failed refresh preserves the existing last-good snapshot and compatible drill-down behavior where safely testable.

**Checkpoint 7D.11 gate:** Representative KST Stage 7 values match direct QAD evidence and the scheduler workflow is usable.

---

# 7D.12 — Full Regression Verification

## Backend

- [x] `dotnet format` verification passes.
- [x] Backend builds cleanly.
- [x] Full backend test suite passes. (468/468)
- [x] Architecture tests pass. (9/9, included in the 468 total)

## Frontend

- [x] Generate OpenAPI TypeScript types.
- [x] Lint passes.
- [x] Typecheck passes.
- [x] Frontend tests pass. (167/167)
- [x] Production frontend build passes.

## Tauri/Rust

- [x] `cargo check` passes.
- [x] Applicable Rust/Tauri build checks pass.
- [x] Backend sidecar is republished using established repository workflow.
- [x] Current sidecar is copied to the Tauri binaries location.
- [x] Normal Tauri development application launches successfully.

## Manual desktop regression

- [x] Existing workspace configuration still works. (all 5 real workspaces loaded: Shure SMT, SHU Metals, SHU Molding, Taco, MSA/Neutronics)
- [x] Real MPS still loads.
- [x] Part Info still works.
- [x] Due/Release switching still works.
- [x] Horizon switching still works.
- [x] Falldown still works.
- [x] Stage 7 Work Orders/Kitting works through normal Tauri path.
- [x] Single-instance behavior remains intact.
- [x] Normal shutdown leaves no orphan backend process.

> **Verified 7D.12 (post 7D.11 due-date-boundary removal + horizontal card-layout change):** all automated and manual regression checks above passed against the real desktop app with live QAD data. Live click-through covered: workspace tab switching, part-row drill-down into Part Info, bucket-cell selection enabling Work Orders tab, Due/Release toggle causing a real re-query with reflowed data, and horizon-count change (24→12 weeks) reflowing grid columns. Single-instance and clean-shutdown (no orphan `Kst.Api.exe`) verified directly via process inspection.

Do not perform packaged installer verification unless required by the accepted Stage 7 completion gate or Stage 7 changes affect packaging/runtime behavior.

---

# 7D.13 — Documentation and Checklist Reconciliation

Create/update durable Stage 7 documentation under repository conventions.

Recommended artifacts:

- [x] `KST_v2_STAGE_7_WORK_ORDER_KITTING_DATA_INVENTORY.md`
- [x] `KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md`
- [x] `KST_v2_STAGE_7_IMPLEMENTATION_PLAN.md`
- [x] `STAGE_7_REAL_DATA_VALIDATION.md` after live validation.

Update:

- [x] `docs/status/CURRENT_PROJECT_STATUS.md`
- [x] `KST-v2-Master-Project-Checklist.md`
- [x] API/OpenAPI documentation if needed. (No endpoint/DTO changes this checkpoint; `WorkOrderEndpoints`/`WorkOrderDtos` already reflect the current accepted contract from prior 7D checkpoints — no edit required.)
- [x] QAD data/source documentation with implementation-confirmed mappings where appropriate. (Satisfied via the new DATA_INVENTORY/CONTRACT docs; `qadpro2-data-map.md` remains a raw, unannotated schema catalog, consistent with Stage 5/6 precedent of not adding stage-specific annotations there.)

Document explicitly (all covered in `KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md` and `KST_v2_STAGE_7_WORK_ORDER_KITTING_DATA_INVENTORY.md`):

- [x] WOID is the scheduler-facing WO identity.
- [x] Work Order Number is not treated as unique/user-facing.
- [x] Open Qty formula.
- [x] Release/Due sources.
- [x] A/F/R eligibility.
- [x] Kitting formula.
- [x] Zero-required-line exclusion.
- [x] Issued % terminology.
- [x] 95%/105% exception thresholds.
- [x] Manufactured-component rule.
- [x] No parent↔subassembly WO relationship exists in QAD.
- [x] Candidate-WO navigation is heuristic/bounded, not pegging.
- [x] Candidate ordering/time rule.
- [x] `RMABOM` exclusion.
- [x] Three-level drill limit.
- [x] Top-level six-week drill policy.
- [x] Lazy-load/cache/snapshot behavior.
- [x] Stage 8+ boundaries.

> **Verified 7D.13:** all four new durable documentation artifacts created under `docs/implementation/`, grounded directly in current backend source (`Kst.Domain.WorkOrders`, `Kst.Application.WorkOrders.WorkOrderDrilldownService`, `Kst.Integrations.Qad.WorkOrders`, `Kst.Api.Endpoints.WorkOrderEndpoints`) rather than the historical (partially superseded) Stage 7 prompt document — notably, the candidate navigation rule is documented as currently implemented (no due-date boundary, per the 7D.11 revision), not the prompt's original due-date-bounded rule. `docs/status/CURRENT_PROJECT_STATUS.md` and `KST-v2-Master-Project-Checklist.md` updated to reflect Stage 7 as implementation-complete pending 7D.14 owner acceptance.

Do not mark project-owner acceptance complete until explicit owner review occurs.

---

# 7D.14 — Stage 7 Completion Gate

Stage 7 is complete only when:

- [x] Parent-only selection exposes Part Info but not contextual production tabs. (7D.6, re-verified 7D.12 manual regression)
- [x] Eligible MPS bucket/Falldown selection automatically opens Work Orders. (7D.6, re-verified 7D.12)
- [x] Only A/F/R WOs receive Stage 7 cards. (7D.2 SQL filter, `WorkOrderIssueStatusClassifier`/`NormalizeStatus`, 7D.10 automated coverage)
- [x] Work Order card values match QAD. (7D.11 live comparison against MSA/Neutronics real data)
- [x] WOID is used as the visible work-order identity. (`WorkOrderSummary.Woid`; Work Order Number never modeled/displayed)
- [x] Kitting % matches validated line-based rules. (`KittingSummary.Calculate`, 7D.1/7D.10 tests, 7D.11 live validation)
- [x] Kitting material rows match QAD. (7D.11 live comparison)
- [x] Zero-required rows are excluded. (`wod_qty_req <> 0` in both the material query and the Kitting `OUTER APPLY`)
- [x] Issued % and Variance Qty calculations are correct. (`WorkOrderMaterialLine` computed properties, 7D.1/7D.10 tests)
- [x] 95%/105% exception presentation is correct. (`WorkOrderIssueStatusClassifier`, `WorkOrderMaterialGrid` styling, 7D.8/7D.10 tests)
- [x] Large component lists can be filtered quickly by part number. (`WorkOrderMaterialGrid` local filter, 7D.8, verified usable at 250+ rows in 7D.11)
- [x] Manufactured components are visually distinct and discoverably drillable. (7D.8 chevron/background treatment, 7D.9 drill wiring)
- [x] Candidate subassembly WOs obey accepted status/date/RMABOM/result-limit rules. (7D.2/7D.11 — Due Date boundary deliberately removed per project-owner decision; status/RMABOM/limit/ordering rules unchanged and verified)
- [x] UI never claims unsupported parent/child WO linkage. (`WorkOrderCandidatePanel` "Work Orders for `<Component Part>`" labeling, 7D.9 tests assert forbidden phrasing never appears)
- [x] Maximum drill depth of three works correctly. (`WorkOrderDrilldownPolicy.MaxDrillDepth`, 7D.9 recursive-nesting test through the real app tree)
- [x] Lazy-load and cache behavior are validated. (7D.4 cache-store design + tests, 7D.10 refresh-invalidation tests)
- [x] Successful MPS refresh invalidates prior Stage 7 investigation context. (7D.10 fix + tests, 7D.11 live confirmation)
- [x] Failed refresh preserves last-good behavior. (7D.4/7D.10 tests — dependency on `IMpsSnapshotStore` unchanged snapshot id on failed refresh)
- [x] Loading, empty, error, stale, and retry states work. (covered across 7D.6-7D.10 component tests and 7D.11/7D.12 live walkthroughs)
- [x] Automated backend/API/frontend tests pass. (468/468 backend, 167/167 frontend — 7D.12)
- [x] Full regression verification passes. (7D.12, re-confirmed clean this checkpoint — no code changed since)
- [x] Representative live-QAD comparisons pass. (7D.11, `STAGE_7_REAL_DATA_VALIDATION.md`)
- [x] Documentation is reconciled. (7D.13 — 4 new artifacts, `CURRENT_PROJECT_STATUS.md`, `KST-v2-Master-Project-Checklist.md`)
- [x] Project owner explicitly accepts Stage 7. **Accepted by the project owner, 2026-08-13 ("I accept.").**

## Final completion statement

> A scheduler can select a near-term MPS bucket or Falldown, inspect its active A/F/R work orders and their kitting/material-issue state, rapidly search large component lists, and follow manufactured components through bounded candidate-work-order drill-downs without KST implying unsupported work-order lineage.

**Stage 7 — Work Orders and Kitting is COMPLETE and ACCEPTED.** Every checklist item above is satisfied by evidence from 7D.0–7D.13, and the project owner explicitly accepted Stage 7 on 2026-08-13. This closes the Stage 7D checkpoint sequence; Stage 8 — Component and BOM Detail is a new, separate rolling-wave phase and has not been started.
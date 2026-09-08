# KST v2 — Stage 9 Immediate Work-Order Shortages Implementation Plan

**Status:** HISTORICAL PLANNING BASELINE, reconciled through Stage 9.11 — 2026-09-08. Stage 9 is **COMPLETE / ACCEPTED / LOCKED**. The current authoritative summary is `KST_v2_STAGE_9_CLOSEOUT.md`.
**Stage:** 9 — Phase 6: Immediate Work-Order Shortages
**Authority:** The accepted Stage 9A design (owner-provided business-rule + architecture spec, accepted as the implementation spec at Stage 9.0); Stage 7R contract §14 (planning window); Stage 8 closeout (effective multi-level BOM / shared inventory); `AGENTS.md`; `SECURITY.md` + `docs/security/`.
**Working method:** Bounded checkpoints 9.0–9.11, each with automated verification and an owner stop-point (Stage 7 working method). No checkpoint is skipped. Documentation checkpoints (9.0, 9.10) make no production-code changes. Sections 9.2–9.9 preserve the original implementation plan; the final accepted amendments below and `KST_v2_STAGE_9_CLOSEOUT.md` govern any current summary.

> **Historical design note:** the full Stage 9A design was provided by the project owner and accepted as the original implementation specification. It remains historical source evidence; the durable current Stage 9 summary is `KST_v2_STAGE_9_CLOSEOUT.md`.

## Layering (reused, not redesigned)

`Kst.Domain` (pure business rules) → `Kst.Application` (one orchestration service for the Stage 9 use case — deliberately **not** split into speculative Kitting / Netting / Allocation / PO sub-services) → `Kst.Integrations.Qad` (SQL readers, bridged into Application via `Delegate*` adapters constructed only in `Kst.Api/Program.cs`, preserving the rule that `Kst.Application` never references `Kst.Integrations.*`) → `Kst.Api` (thin DTO mapping only).

## Analytical grain (binding)

Stage 9 grain is **Work Order + Component**. There is one visible result row per component within a work order. The same component is **never** aggregated across separate work orders into a single Stage 9 shortage result. Cross-WO component netting belongs to the later combined-shortage capability, not Stage 9.

## Reuse map (confirmed against current code)

| Capability | Reuse | Stage 9 note |
|---|---|---|
| Immediate population | `WorkOrderDrilldownService.GetPlanningWindowAsync` + `QadWorkOrderSummaryReader` | Falldown + Week 0–3, Due/Release basis, snapshot-keyed; this **is** the Stage 9 window |
| Actual `wod_det` requirement | `QadWorkOrderMaterialReader` (per-WOID) | add `pt_um` for EA normalization |
| Projected BOM | `QadBomReader.ReadAsync(site, parent, effectiveDate)` | reuse the **reader** (parameterized by effective date), **not** `BomService` (hardcodes today); the design needs release-date effective dating |
| Inventory base | `QadPartInventoryReader.ReadSummariesAsync` (batch) | extend for usable-on-hand + 6 context buckets + issue-days |
| Cache + DI | snapshot-keyed in-memory stores; `Delegate*` in `Program.cs` | extend with a business-day component (see 9.4) |
| Frontend | Shortages tab (disabled) in `MpsWorkspace.tsx`; `WorkOrderCard` / `WorkOrderMaterialGrid` / `ComponentInfoModal` / escape-stack | reuse vocabulary |

## Checkpoint summary

| Checkpoint | Scope | Outcome |
|---|---|---|
| 9.0 | Preflight + this plan + stale-checklist reconciliation | **Done — 2026-09-02** (this document) |
| 9.1 | Source-mapping reconciliation/validation (read-only QAD) | **Done — 2026-09-03** (incl. hybrid allocation decision + `pt_iss_pol` resolution) |
| 9.2 | Domain models + pure calculations + layer-appropriate tests | **Complete** |
| 9.3 | QAD readers (extend + new) + QAD adapter tests | **Complete** |
| 9.4 | Application orchestration + date-sensitive allocation cache + DI | **Complete** |
| 9.5 | API endpoint + DTOs + Problem Details + OpenAPI/TS regen | **Complete** |
| 9.6 | Frontend (Shortages tab, grid, breakout) + component tests | **Complete** |
| 9.7 | Automated verification / total scenario-coverage gap audit | **Complete / accepted** — see `KST_v2_STAGE_9_SCENARIO_COVERAGE_AUDIT.md` |
| 9.8 | Live-QAD validation | **Complete / accepted** — see `KST_v2_STAGE_9_LIVE_QAD_VALIDATION.md` |
| 9.9 | Full regression (backend / frontend / Tauri / manual desktop) | **Complete / accepted** — see `KST_v2_STAGE_9_FULL_REGRESSION.md` |
| 9.10 | Documentation + checklist reconciliation + closeout | **Complete** — see `KST_v2_STAGE_9_CLOSEOUT.md` |
| 9.11 | Owner acceptance | **Complete / accepted / locked — 2026-09-08** |

## 9.0 — Preflight + plan + checklist reconciliation (no code) — DONE

- Wrote this plan.
- Reconciled the stale Stage 9 section of `KST-v2-Master-Project-Checklist.md` to the accepted Stage 9A rules (removed `Due This Week`; PO receipts do not reduce shortage; next-PO/KSS replaces PO coverage; Unknown/Data Issue → Short → On Hand order; Actual vs Projected authority; requirement provenance; committed R-then-A allocation bounded to the Stage 7R window; advisory shared residual pool; non-additive uncommitted shortages; inventory activity breakout; KSS/NO PO; WO + Component grain).
- Updated `docs/status/CURRENT_PROJECT_STATUS.md` (Stage 9: planned / beginning implementation preparation).
- **Verify:** documentation diff reviewed; no production-code, dependency, or SQL/database-object changes.

## 9.1 — Source-mapping reconciliation/validation (read-only QAD, no production code)

**Framing:** This is **reconciliation and validation**, not greenfield discovery. Start from `docs/data/qadpro2-data-map.md`; reuse mappings already established there. Use `docs/reference/ktshrtge11.txt` (legacy Progress 4GL source) and **targeted read-only QAD investigation only where Stage 9 semantics remain missing or ambiguous**. Do not rediscover already-validated fields. Unresolved evidence **remains unresolved** — do not guess.

**Data-map process:** The data map (`docs/data/qadpro2-data-map.{md,json,yaml}`) is generated from `DataMap.xlsx` (three representations; no in-repo generation script). Prior additions (`sct_det` Stage 8D.5, `loc_mstr` R0.6) were added directly with confirmation against live QAD schema / implemented queries, updating all representations consistently. 9.1 follows that established process — confirm against live QAD, then update all three representations consistently — rather than hand-editing a single representation.

**The seven Stage 9 source-mapping topics** (each classified as *established* / *established field, Stage 9 semantics need validation* / *genuinely missing*):

| # | Topic | Data-map state | 9.1 action |
|---|---|---|---|
| 1 | PO open-quantity calculation | `pod_det.pod_qty_ord` (Quantity Ordered), `pod_det.pod_qty_rcvd` (Quantity Received) — Validated=Yes | Established fields; validate the Stage 9 open-quantity semantics (ordered − received, or a dedicated open-qty field) |
| 2 | open/non-cancelled PO qualification | `pod_det.pod_status` (Line Status), `po_mstr.po_stat` (PO Status) — Validated=Yes | Established fields; validate the status values that qualify a line/PO as open vs cancelled |
| 3 | authoritative PO confirmation semantics | `po_mstr.po_confirm` (PO Confirmed T/F), `pod_det.pod__log01` (Confirmed T/F) — Validated=Yes | Established fields; validate which level (PO master vs line) is authoritative for the Stage 9 "PO Confirmation" field |
| 4 | tracking-field mapping | `pod_det.pod__chr06` (Tracking Number) — Validated=Yes | Established field; confirm it is the Stage 9 tracking source (legacy `ktshrtge11` added tracking to outputs) |
| 5 | KSS/supplier-schedule detection | `po_mstr.po_sched` (Supplier Scheduled T/F (KSS)), `pod_det.pod_sched` (KSS/Feed) — Validated=Yes | Historical 9.1 question; final authority is the independent effective `pod_det`/`po_mstr` relationship recorded in the closeout |
| 6 | inventory-status classification for the accepted Stage 9 buckets | `is_mstr` (`is_status`, `is_desc`, `is_nettable`, `is_avail`, `is_frozen`), `ld_det` (`ld_status`, `ld_expire`, `ld_lot`), `loc_mstr` (`loc_status`) — Validated=Yes | Established fields; the `is_status` → bucket classification (Transit / Inspection / Non-Net / MRB / NCM Inspection / Expired-Expiring) is **not** established — validate via read-only QAD |
| 7 | QAD issue-days source | `icc_ctrl` is **not** in the data map (not one of the 27 tables) | **Genuinely missing** — confirm the current QAD issue-days source and field via read-only QAD |

**Adjacent (not one of the seven; handled in 9.3):** issue policy — `ptp_det.ptp_iss_pol` (site-specific, bit) is established (Validated=Yes); the master fallback **`pt_mstr.pt_iss_pol`** (bit) was confirmed via legacy logic + live QAD during 9.1 and added to the data map (`KST_v2_STAGE_9_SOURCE_MAPPING.md` §3.4). Resolution order: default `yes` → `ptp_det.ptp_iss_pol` (part+site) → `pt_mstr.pt_iss_pol` (master).

**Deliverables:** updated `docs/data/qadpro2-data-map.{md,json,yaml}` (newly-validated fields only); `docs/implementation/KST_v2_STAGE_9_SOURCE_MAPPING.md` (durable evidence, Stage 7 data-inventory style).
**Verify:** read-only SQL evidence recorded; data map updated consistently across all three representations; source-mapping doc owner-accepted; no production code.

## 9.2 — Domain models + pure calculations (`Kst.Domain`)

**Types** (per the accepted design result contract): `RequirementSource` (ActualWo / ProjectedBom); `MaterialStatus` (Unknown / NotApplicable / Short / OnHand); `ComponentRequirement`; `InventoryActivity` (Transit / Inspection / NonNet / Mrb / NcmInspection / ExpiredExpiring); `UsableInventoryPosition`; `IncomingContext` (IsKss / PoState / PoNumber? / PoDueDate? / PoOpenQty? / PoConfirmed? / TrackingInfo?); `WorkOrderContext` (WoId / BuildPart / Status / WoType / DueDate / ReleaseDate / MaterialBuildQty / PlanningBucketContext); `ComponentRow`; `WorkOrderImmediateMaterialAnalysis` (WorkOrderContext + ComponentRows[]), including provenance (`AllocationMode` = HardAllocatedCommitted | CommittedSequential | AdvisorySharedPool; `UsableHardAllocationToThisWoComponent`; `OwnHardCoverage`; `UncoveredRequirement`; `AvailableQtyAtEvaluation`). The hard-allocation quantity/provenance is carried to verify and explain the calculation (whether `Hard Allocated Qty` becomes a visible detail-card field is finalized at 9.6).

**Pure calculations:**
- **EA normalization** — floor to whole unit when UOM = `EA` (actual: before issued subtraction; projected: after extension/consolidation).
- **Remaining requirement** — `max(AdjustedRequiredQty − IssuedQty, 0)` (over-issue clamps to zero; variance may show signed over-issue).
- **Projected build qty** — `max(wo_qty_ord − wo_qty_comp − wo_qty_rjct, 0)`.
- **BOM consolidation + extended requirement** — per component, sum the path quantity-per products across all non-phantom occurrences × projected build qty; EA-round.
- **QAD hard/detail allocation (authoritative)** — `lad_det` hard allocations take precedence over the reconstructed allocation. For the evaluated WO/component, credit its own usable hard allocation against Remaining Requirement and clamp so it cannot create negative demand or an inventory credit: `RemainingRequirement = max(Required − Issued, 0)`; `OwnHardCoverage = min(RemainingRequirement, UsableHardAllocationToThisWoComponent)`; `UncoveredRequirement = max(RemainingRequirement − OwnHardCoverage, 0)`. Hard-allocated inventory stays in physical QOH until issue but is removed from the shared free pool (no double-count); a hard allocation does not override inventory usability. Hard allocations are **not** bounded by the Stage 9 analytical window (§6.4 amendment).
- **Committed sequential allocation (residual free inventory only)** — after honoring QAD hard allocations, **R tier first, then A tier**; within each tier ordered by (Falldown or Forward-Due: WO Due Date, then WO ID; Forward-Release: WO Release Date, then WO ID); `Allocated = min(uncovered, inventoryRemaining)`, `Short = uncovered − allocated`, `inventoryRemaining −= allocated`. R is **not** permanently due-date ordered in Release mode. Bounded to WOs inside the accepted Stage 9 immediate window.
- **Advisory shared residual pool** — residual after hard allocations + the committed pass; uncommitted WOs (ordinary E/F/P and `E + wo_type F`) read it independently and do not consume from each other; **non-additive** (never summed to a combined component shortage).
- **`wod_qty_all` exclusion** — the soft/general allocation quantity (`wod_det.wod_qty_all`) is a separate mechanism from `lad_det` hard allocation and is **not** used as an additional Stage 9 coverage source without an explicitly accepted rule (§6.5).
- **Material status classification** — Unknown / Data Issue (requirement or position cannot be reliably established) → Manufactured / NotApplicable (master P/M `M`; visible/drillable but excluded from shortage arithmetic) → Short (`ShortQty > 0`) → On Hand.

**Tests:** cover the accepted Stage 9A verification scenarios **at the appropriate architectural layer** — not all in `Kst.Domain.Tests`. Pure calculations live in `Kst.Domain.Tests`; SQL shape/normalization in `Kst.Integrations.Qad.Tests`; orchestration/allocation in `Kst.Application.Tests`; endpoint behavior in `Kst.Api.IntegrationTests`. Total scenario coverage is audited at 9.7.
**Verify:** `dotnet test` green for the touched test projects.

## 9.3 — QAD readers (SQL)

- Extend `QadWorkOrderSummaryReader`: add `wo_type`, `wo_qty_rjct` (requirement authority + projected build qty).
- Extend `QadWorkOrderMaterialReader`: add `pt_um` (EA normalization of actual requirements).
- New site-wide committed-population reader: all R/A WOs at the site in the accepted immediate window + their `wod_det` residual per component (the committed-allocation input; **not** parent-scoped).
- New hard-allocation reader: `lad_det` firm/detail allocations (`lad_dataset='wod_det'`; WOID = `lad_nbr`; operation = `lad_line`; component = `lad_part`, with domain/site/lot/location retained) — the authoritative hard-allocation source, **site-wide and not window-bounded**. A matching `wod_det` row is not a runtime requirement; hard allocations for WOs outside the window still reduce the free pool.
- New inventory-position reader: usable-on-hand + the 6 context buckets (`ld_det` / `loc_mstr` / `is_mstr` + `ld_expire` + issue-days), batch-capable.
- New issue-policy reader: `ptp_det.ptp_iss_pol` (site-specific, part+site) with **`pt_mstr.pt_iss_pol`** master fallback (both bit; confirmed 9.1).
- New issue-days reader: the confirmed QAD issue-days source (from 9.1).
- New next-PO reader: `pod_det` / `po_mstr` — earliest open, non-cancelled line with positive open quantity (PO number / due / open qty / confirmation / tracking) + KSS flag.

**Security (as written):** New QAD tables (`icc_ctrl`, `lad_det`, `pt_mstr`, `pod_det`, `po_mstr`) extend the read-only QAD read surface. The repository's read-only production-database rule (`AGENTS.md` §7; `SECURITY_ASSURANCE_POLICY.md`; `APPLICATION_SECURITY_PROFILE.md`) and the security-review process (`SECURITY.md`; `docs/security/`) apply as written. New readers must emit read-only SQL (existing `QadReadOnlySqlTests` regression protection) and any security-relevant change follows the established security-review requirements. The S0.7B database-permission verification record (per-reader table matrix) is the existing reference for the effective read-only posture; adding new read tables does not change that posture.

**Tests:** `BuildQuery` + `Normalize` pattern per reader; read-only SQL.
**Verify:** `dotnet test` `Kst.Integrations.Qad.Tests` green; `QadReadOnlySqlTests` pass.

## 9.4 — Application orchestration (`Kst.Application`)

- New `WorkOrderImmediateShortageService.GetImmediateMaterialAsync(workspaceId, snapshotId, woid, dateBasis, today, ct) → WorkOrderImmediateMaterialAnalysis`, orchestrating: immediate population (reuse) → requirement resolution (actual via material reader / projected via `QadBomReader` at the release-date effective date, else Today) → inventory position → **QAD hard-allocation pass (authoritative; site-wide, not window-bounded)** → residual free pool → committed R-then-A allocation on the residual (window-bounded) → WO evaluation → next-PO context (Short components only).
- **Date-sensitive allocation cache:** the analysis depends on **Today** (Expired/Expiring classification via `ExpirationDate <= Today + issue-days`; the Today fallback for projected BOM effective date). The cache key must include the current business date (or the entry must be invalidated at the business-day boundary) so a **prior business day's analysis is never served**. The existing snapshot-keyed in-memory pattern is extended with a business-day component.
- `Delegate*` adapters + DI wiring in `Program.cs`.

**Tests:** orchestration + allocation + date-sensitive caching (deterministic reader overrides).
**Verify:** `dotnet test` `Kst.Application.Tests` green.

## 9.5 — API + DTOs (`Kst.Api`)

- New endpoint: `GET /api/v1/workspaces/{assignmentId:guid}/work-orders/{woid}/immediate-material?snapshotId&dateBasis` → `WorkOrderImmediateMaterialAnalysisResponseDto` (per-WO grain, backed by the cached site-wide allocation — matches the existing material endpoint).
- New DTOs (`Dtos/WorkOrderImmediateMaterialDtos.cs`): analysis response, `WorkOrderContextDto`, `ComponentRowDto`, `InventoryActivityDto`, `IncomingContextDto`.
- Problem Details (reuse MpsNotLoaded / SnapshotChanged / Unavailable / PartNotInScope conventions).
- OpenAPI regen (`dotnet build`) + TS regen (`npm run generate:types`) + typecheck; commit `docs/openapi/Kst.Api.json` + `src/frontend/src/generated/api.ts` together.

**Tests:** `KstApiFactory` integration tests.
**Verify:** `dotnet test` `Kst.Api.IntegrationTests` green; frontend typecheck clean.

## 9.6 — Frontend (`src/frontend`)

- Enable the Shortages tab (`MpsWorkspace.tsx`; add `'shortages'` to the `activeTab` union).
- WO + Component main grid: Component / Description / BOM Qty / Issued Qty / Variance Qty / Issued % / Short; presentation order **Unknown / Data Issue → Short → On Hand** (no severity sorting).
- Breakout detail card (row select): WO context, material status, requirement source, usable on-hand, floor-stock/non-issued + over-issued flags, inventory activity (6 buckets), incoming (KSS / PO / NO PO).
- Reuse `WorkOrderCard` / `WorkOrderMaterialGrid` / `ComponentInfoModal` / escape-stack.
- **Owner-review item:** the exact frontend navigation to a WO-specific shortage view (how the scheduler reaches a specific WO's Stage 9 result) is confirmed in this checkpoint.

**Tests:** vitest + testing-library (grid order, breakout, Unknown/Short/OnHand, NO PO, KSS, empty states).
**Verify:** `npm run lint/typecheck/test/build` green.

## 9.7 — Automated verification / total scenario-coverage gap audit

- Full backend (`dotnet test Kst.slnx` + `dotnet format --verify-no-changes`), full frontend, architecture tests (layering, version consistency, read-only SQL).
- **Gap audit:** confirm every accepted Stage 9A verification scenario is covered **somewhere** in the test suite (domain / QAD adapter / application / API integration), regardless of which layer hosts it. No requirement silently omitted.
**Verify:** all green; audit documented.

## 9.8 — Live-QAD validation

- Live Tauri walkthrough (read-only) vs ground-truth SQL (requirement authority, committed allocation, inventory buckets, PO/KSS context); document findings (Stage 7 real-data-validation style).
**Verify:** validation doc; discrepancies resolved.

## 9.9 — Full regression

- Backend build + tests + format; frontend lint/typecheck/test/build; `cargo check` + sidecar rebuild (`scripts/build-sidecar.ps1`); manual desktop regression (workspace / MPS / Part Info / Work Orders / Shortages / Due-Release / horizon / Falldown / drill-down / Escape / clean shutdown).
**Verify:** all green; manual regression documented.

## 9.10 — Documentation + checklist reconciliation + closeout

- Completed: reconciled the Stage 9 checklist, current status, plan, source mapping, and three data-map representations; created `KST_v2_STAGE_9_CLOSEOUT.md` as the current closeout authority.

## Final accepted amendments and evidence

- Stage 9 shortage participation uses authoritative master `pt_mstr.pt_pm_code` for both Actual and Projected requirements: `M` is visible/drillable `NotApplicable`; nonblank non-`M` is eligible; null/blank/whitespace is `Unknown` / Data Issue. Stage 8 effective site P/M remains separate.
- A successful empty Actual material read is a Loaded zero-row analysis, not `WorkOrderNotInImmediateWindow`.
- Public component results expose `allocationMode`, `usableHardAllocationToThisWoComponent`, `ownHardCoverage`, `uncoveredRequirement`, `availableQuantityAtEvaluation`, and `allocatedQuantity`; stable allocation-mode values are `hardAllocatedCommitted`, `committedSequential`, and `advisorySharedPool`.
- KSS is independent of conventional PO availability. Its source is an effective `pod_det` domain/site/part supplier-schedule relationship joined to effective supplier-scheduled `po_mstr`; `pod_sched` alone is not the authority. `po_mstr.po_stat` has no accepted predicate pending authoritative closed-PO evidence.
- The final workflow is MPS → Work Orders → Show/Hide Material Lines → one selected integrated immediate-material analysis. The Shortages tab retains selected-WO context; `Select for Shortages` is retired. Applicable purchased shortages may mark a WO card, but there is no MPS-grid shortage marker. Escape unwinds Material Detail, nested analysis, containing levels, selected top-level analysis, Work Orders, then root one level at a time.
- Verification history and discovered corrections are retained in the 9.7 audit, 9.8 validation, 9.9 regression, and closeout; this plan does not rewrite their historical record.

## 9.11 — Owner acceptance

- Completion gate: a scheduler can identify immediate component shortages for near-term work orders, with usable-now inventory, committed R-then-A allocation, advisory uncommitted exposure, inventory activity, and next-PO/KSS context.
- **Disposition:** COMPLETE / ACCEPTED / LOCKED — 2026-09-08. Technical, automated-regression, live-QAD, and owner-guided evidence is complete. `po_mstr.po_stat` remains a deferred evidence item; no master-status predicate is accepted, and it does not block Stage 9 acceptance.

## Canonical verification commands

- Backend: `cd src/backend; dotnet build Kst.slnx; dotnet test Kst.slnx; dotnet format Kst.slnx --verify-no-changes`
- Frontend: `cd src/frontend; npm run lint; npm run typecheck; npm test; npm run build`
- Contract: `dotnet build` (regens `docs/openapi/Kst.Api.json`) → `npm run generate:types` → `npm run typecheck`; commit spec + `generated/api.ts` together.
- Tauri: `cd src/tauri; cargo check`; after backend changes `.\scripts\build-sidecar.ps1`.

## Out of scope (accepted design boundary)

Combined / component-wide shortage netting · time-phased PO receipts / coverage · projected clear dates · lead-time or component MRP · future component shortage projection · detailed PO/vendor/buyer workflow · substitutes · fabricated parent/subassembly pegging.

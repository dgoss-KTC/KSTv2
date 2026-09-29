# KST v2 Master Project Checklist

**Accepted baseline:** S0 is **COMPLETE / ACCEPTED — 2026-08-31**. Stages 1–11 are complete and
owner-accepted at their documented depth. Stages 9, 10, and 11 are **LOCKED**.

**Current project position:** The owner approved the revised Release 1 scope and stage sequence on
2026-09-29. Stage 12 is retired. Stage 13 Open Orders is the next planned product stage and precedes
Stage 14 Planning Workbook. No implementation stage is authorized by this documentation update.
Stages 16 and 17 are absorbed/retired as recorded below; Stage 20 is retired. Stage 21 Export
Completion and Stage 22 Refinement & Optimization are required before Stage 23 Quality and
Hardening. Release readiness, pilot, rollout, and post-release planning are Stages 24–27.
The supporting disposition matrix and deferred-work registry are in
`docs/status/RELEASE_1_SCOPE_REVIEW_2026-09-29.md`.

**Stage 3 closeout commit:** `6f5644c` — `chore: complete Stage 3 technical foundation closeout`

> This Markdown edition reconciles the original checklist with the completed C#/.NET 10 walking skeleton. The original checklist contained a few stale Python references and several database/export foundation items that the rolling-wave strategy intentionally defers until the first UI phase that requires them.

## Status legend

- `[x]` Complete or formally accepted at the current rolling-wave depth
- `[ ]` Not started or still required
- `[~]` Deferred to the UI phase where the capability is first required

## Project planning model

KST v2 uses rolling-wave planning organized by UI section. Each implementation phase contains its own UI review, field inventory, source-data mapping, business rules, backend design, cache design, API contract, frontend implementation, exports where applicable, automated tests, legacy comparison, and user acceptance.

Later phases may extend or refactor models and services created during earlier phases. The complete application does not need to be specified field by field before implementation begins.

---

## Stage 1 — Project Charter ✅

- [x] Project charter approved and current-state, product vision, users, scope, exclusions, safety boundaries, architecture, and rollout strategy established.
- [x] C#/.NET 10, ASP.NET Core, React/TypeScript, and Tauri/Rust selected as the supported architecture.
- [x] Release 1 and pilot strategy established.

## Stage 2 — Legacy System and Product Inventory ✅

- [x] Legacy capability census completed.
- [x] Capability migration dispositions established.
- [x] Prototype inventory created at the broad-product level.
- [x] Initial dataset and source-system inventory completed.
- [x] Legacy MPS source/procedure behavior inventoried; the final production retrieval strategy was intentionally refined in Stage 5A to a direct QAD-adapter query.
- [x] Rolling-wave, UI-section implementation strategy adopted.
- [x] Detailed field lineage intentionally continues inside each UI phase.

## Stage 3 — Technical Foundation ✅

- [x] Final repository layout established.
- [x] React/TypeScript frontend, Tauri 2 shell, and C#/.NET 10 backend solution established.
- [x] Formatting, linting, type-checking, SDK, analyzer, and package policies established.
- [x] ASP.NET Core loopback API with `/health`, `/ready`, system status, Problem Details, JSON conventions, and OpenAPI established.
- [x] OpenAPI-generated TypeScript contracts established.
- [x] Self-contained `win-x64` single-file backend sidecar publication automated.
- [x] Tauri sidecar discovery, dynamic-port handshake, readiness polling, and frontend URL bridge established.
- [x] Development and packaged CORS policies verified for the observed Tauri origins.
- [x] Explicit sidecar ownership, cleanup, timeout handling, crash notification, and orphan prevention established.
- [x] Single-instance behavior established.
- [x] Development application launches, connects, reports failures truthfully, and shuts down cleanly.
- [x] MSI and NSIS packages build successfully.
- [x] Packaged application launches, connects, prevents duplicate instances, and shuts down without orphan processes.
- [x] Backend, frontend, API, architecture, CORS, and lifecycle-related automated checks pass.
- [x] Tracked setup, lifecycle, troubleshooting, packaging, verification, and current-status documentation established.
- [~] Real QAD/shortage database access, Dapper/SqlClient adapters, production cache models, and export libraries are deferred to the first UI phase that requires them.

---


## Stage 4 — Phase 1: Application Shell and Workspace Configuration

**Status: COMPLETE, including Stage 4B Workspace Scope Extension**

Stage 4 established the application shell, local workspace configuration, workspace tabs, persistence, validation, and the workspace part-scope model used by later capabilities.

Authoritative workspace scope after Stage 4B:

```text
Site
Product Line From?
Product Line To?
Explicit Parent Parts[]
```

Customer code and IOS code are not authoritative workspace-scope inputs. Whole product-line ownership is the normal case; explicit parent parts support exceptional/split responsibility. Domain is inferred later by the QAD integration boundary from Site.

Durable references include `STAGE_4_PHASE_1_PROGRESS.md` and the Stage 4B completion work captured in project history.

Phase 1 completion gate: **PASS** — application shell and workspace configuration are accepted as the foundation for Stage 5.


## Stage 5 — MPS Data Foundation and Dashboard Implementation

The detailed Stage 5 planning/checklist history is also maintained as `KST_v2_Master_Project_Checklist_STAGE_5_REVISION.md`; its current authority and final location will be reviewed during R0.

# Stage 5A — KST v2 Data Inventory and Data Strategy

**Status:** COMPLETE / ACCEPTED — Stage 5A owner acceptance received 2026-08-07; Stage 5B subsequently completed and was accepted

## Purpose

Establish the authoritative MPS data requirements, source/query boundaries, workspace scope mapping, status rules, refresh behavior, snapshot design, frontend fiscal-calendar strategy, data-quality assumptions, and implementation contracts before Stage 5B production implementation begins.

Stage 5A remains documentation/design plus narrowly scoped investigative SQL only.

---

## 5A.1 Reconcile Existing Data Assumptions

- [x] Review prototype field inventory and curated QAD data map.
- [x] Review Legacy KST System Inventory.
- [x] Review Capability Disposition and Migration Map.
- [x] Review Revised Phased Implementation Strategy.
- [x] Review prior Stage 5 assumptions.
- [x] Remove customer number / IOS code as authoritative workspace-scope dependencies.
- [x] Replace customer workspace terminology with workspace part scope.
- [x] Reclassify `sp_QAD_ktmpswkm` as legacy business-rule evidence only.
- [x] Replace the assumed stored-procedure implementation with the accepted direct QAD-adapter query strategy.

---

## 5A.2 Workspace-to-Database Scope Mapping

Accepted workspace inputs:

```text
Site
Product Line From?
Product Line To?
Explicit Parent Parts[]
```

- [x] Site is required.
- [x] Domain is inferred from site in QAD integration.
- [x] Product-line range is inclusive.
- [x] Product-line-derived parent discovery uses qualifying MRP activity.
- [x] Explicit parent parts do not require current MRP activity merely to remain configured.
- [x] Explicit E/O parts are rejected.
- [x] Customer code is not used for workspace scope.
- [x] IOS code is not used for workspace scope.
- [x] Query scope is the already-resolved parent-part list for one site/domain.

---

## 5A.3 MPS Dataset Inventory

- [x] Parent part mapped.
- [x] Part description mapped to `pt_mstr.pt_desc1` only.
- [x] Site/domain mapped.
- [x] Due date mapped.
- [x] Release date mapped.
- [x] Quantity mapped.
- [x] MRP type mapped.
- [x] Source dataset mapped as SQL qualification metadata.
- [x] Work-order ID mapped.
- [x] Work-order status mapped.
- [x] Falldown inputs/rules defined.
- [x] A/F/R/Mixed execution-state inputs defined.
- [x] Planned (`P`) flag defined.
- [x] Explicitly scheduled (`e`) flag defined.
- [x] RMA exclusion defined with `wo_bom_code <> 'RMABOM'`.
- [x] Due/Release view inputs retained in the same snapshot.
- [x] Fiscal year/period/quarter removed from backend contract and assigned to frontend display logic.
- [x] Work-order quantity/start/detail fields dispositioned to later drill-down stages.
- [x] Shortage/kitting inputs dispositioned to later stages.

Durable artifact: `KST_v2_STAGE_5A_MPS_DATA_INVENTORY.md`.

---

## 5A.4 Work-Order Status and MPS Presentation Rules

- [x] `A` = Allocating execution state.
- [x] `F` = Frozen execution state.
- [x] `R` = Released execution state.
- [x] `C` excluded.
- [x] `P` retained as independent planned-work flag.
- [x] `e` retained as independent explicitly-scheduled flag.
- [x] Multiple distinct A/F/R states = Mixed.
- [x] P/e do not create Mixed by themselves.
- [x] Quantity sums all included rows regardless of presentation state.
- [x] Backend returns semantic states/flags rather than colors.
- [x] Frontend owns box fill, planned font treatment, scheduled non-color marker, and mixed presentation.

---

## 5A.5 Legacy `sp_QAD_ktmpswkm` Analysis

- [x] Parameters reviewed.
- [x] Product-line filtering reviewed.
- [x] Buyer/planner filtering reviewed and rejected for KST v2 workspace scope.
- [x] Dynamic week pivot reviewed and rejected for KST v2.
- [x] Quantity/hours behavior reviewed.
- [x] SUPPLY/SUPPLYF/SUPPLYP behavior reviewed.
- [x] Falldown logic reviewed.
- [x] Source tables and joins reviewed.
- [x] Useful business rules preserved.
- [x] Legacy output fields not required for initial MPS identified and removed.

---

## 5A.6 Production MPS Query Strategy

Accepted decision: **direct parameterized SQL owned by `Kst.Integrations.Qad`; no KST-specific stored procedure or TVF for the initial MPS.**

- [x] SQL Server 2016-compatible.
- [x] Read-only.
- [x] Site/domain bounded.
- [x] Resolved parent-part list supplied by the application.
- [x] Part values parameterized; no raw string concatenation.
- [x] Adapter may chunk large part scopes.
- [x] `mrp_dataset = 'wo_mstr'`.
- [x] `mrp_type IN ('supply','supplyf','supplyp')`.
- [x] Safe WO join uses domain + site + part + WO number + WO ID.
- [x] `wo_status <> 'C'`.
- [x] `wo_bom_code <> 'RMABOM'`.
- [x] `rps_mstr` intentionally excluded as pre-MRP repetitive-schedule state.
- [x] No dynamic pivot.
- [x] No SQL weekly aggregation.
- [x] No defensive `DISTINCT`/deduplication without evidence.
- [x] Representative KTC/SW duplicate diagnostic returned no rows at accepted source grain.
- [x] Future retrieval covers the maximum 72-week horizon.
- [x] Historical retrieval has no lower cutoff for qualifying unfinished Falldown work.

Stored-procedure naming/deployment standards are **not required for this MPS slice** and should be defined later only if a capability actually requires a database object.

---

## 5A.7 Data Grain / Source Ownership / SQL-vs-C# Map

- [x] MPS source-row grain documented.
- [x] Parent/week bucket grain documented.
- [x] Work-order reference grain documented.
- [x] Workspace snapshot grain documented.
- [x] `mrp_det` established as authoritative parent-level planning fact source.
- [x] `wo_mstr` established as authoritative WO header/status source.
- [x] `wod_det` established as authoritative component/WO usage source for later stages.
- [x] `pt_mstr` / `ptp_det` classified primarily as informational/filter sources.
- [x] SQL owns source filtering and safe joins.
- [x] C# owns week bucketing, Falldown, aggregation, and MPS semantic classification.
- [x] Frontend owns fiscal planning/display metadata and visual presentation.

---

## 5A.8 Fiscal Calendar Strategy

- [x] Fiscal calendar removed from QAD/backend responsibilities.
- [x] FY26 anchor set to Sunday, June 29, 2025.
- [x] Standard 4-4-5 × 4 pattern defined.
- [x] 53-week years represented as user-maintained exceptions.
- [x] Exception records identify which fiscal period receives the extra week.
- [x] Later fiscal-year starts derive automatically from 52/53-week progression.
- [x] No annual source-code maintenance required.
- [x] Fiscal Calendar section will be added to current Settings surface; final settings navigation may be reorganized later.

Durable artifact: `KST_v2_STAGE_5A_FISCAL_CALENDAR_STRATEGY.md`.

---

## 5A.9 Snapshot and Refresh Strategy

- [x] MPS loads automatically when a workspace opens.
- [x] Workspace shell appears immediately while MPS loads.
- [x] Explicit workspace parents remain visible with zero current MPS rows.
- [x] Snapshot retains both Due Date and Release Date source values.
- [x] Snapshot retains minimal WO references/statuses.
- [x] Snapshot covers the maximum 72-week future horizon.
- [x] Snapshot retains all historical qualifying unfinished work needed for Falldown.
- [x] Due/Release changes are local and do not require QAD re-query.
- [x] Horizon changes up to 72 weeks are local.
- [x] Fiscal-display changes are frontend-local.
- [x] Refresh re-resolves workspace scope and rebuilds the complete MPS snapshot.
- [x] Existing snapshot remains visible while refresh runs.
- [x] Snapshot replacement is atomic after success.
- [x] Failed refresh preserves the last good snapshot.
- [x] Last successful refresh time is shown.
- [x] Initial database failure is not represented as empty data.
- [x] Approved initial database error message documented.
- [x] No persisted/offline MPS snapshot initially.
- [x] No automatic background refresh initially.
- [x] Only one refresh per workspace should run at a time.

Durable artifact: `KST_v2_STAGE_5A_SNAPSHOT_REFRESH_STRATEGY.md`.

---

## 5A.10 Data Quality and Validation Register — Initial MPS

- [x] Customer/IOS scope reliability issue documented and removed from scope model.
- [x] Repetitive-schedule pre-MRP freshness dependency documented.
- [x] RMA work-order exclusion documented.
- [x] Closed-WO exclusion documented.
- [x] Unknown non-C WO status handled defensively.
- [x] Source uniqueness diagnostic completed for representative KTC/SW data.
- [x] Database-unavailable behavior documented.
- [x] Representative mixed-status cases documented.
- [x] Planned/scheduled combined-state cases documented.
- [x] Falldown boundary/old-WO cases documented.
- [x] Due/Release local-rebucket cases documented.
- [x] Fiscal 52/53-week validation cases documented.

Additional representative-site validation remains an implementation verification gate rather than a Stage 5A blocker.

---

## 5A.11 Backend Data Contract

- [x] `MpsSourceRow` defined.
- [x] `MpsSupplyType` defined.
- [x] `MpsWorkOrderState` defined.
- [x] `MpsBucket` defined.
- [x] `MpsExecutionStatus` defined.
- [x] `MpsWorkOrderRef` defined.
- [x] `MpsPartSchedule` defined.
- [x] Backend/frontend fiscal boundary defined.
- [x] Snapshot behavior reflected in contract.
- [x] Define final snapshot metadata/API response candidate for Stage 5B.

Durable artifact: `KST_v2_STAGE_5A_MPS_BACKEND_DATA_CONTRACT.md`.

Snapshot/API candidate: `KST_v2_STAGE_5A_MPS_API_SNAPSHOT_CONTRACT.md`.

---

## 5A.12 Database Access / Security / Performance Closeout

Accepted closeout:

- [x] Reuse existing `QadConnectionOptions` / .NET options mechanism; exact binding path is a Stage 5B repository-inspection task.
- [x] Windows-integrated authentication confirmed by technical-foundation architecture.
- [x] `Microsoft.Data.SqlClient` + Dapper are the preselected QAD integration stack in backend boundaries.
- [x] Initial MPS command timeout strategy: 60 seconds, backend-configurable rather than end-user setting.
- [x] Propagate .NET cancellation tokens through async QAD operations.
- [x] Log counts/timings/failure categories; never expose credentials/full connection strings to logs or UI.
- [x] Initial parameter batch size: 500 parent parts; measure/tune in Stage 5B if needed.
- [x] Stage 5B will measure part count, source-row count, query time, normalization time, and total refresh time; no invented hard SLA in Stage 5A.
- [x] Read-only is required by architecture; actual production/test account privilege verification is a Stage 5B environment gate.

Durable artifact: `KST_v2_STAGE_5A_DATABASE_ACCESS_PERFORMANCE_STRATEGY.md`.

---

## 5A.13 Documentation Reconciliation / Stage 5B Plan

- [x] Update full Master Project Checklist to remove obsolete customer-workspace / legacy-procedure assumptions.
- [x] Update Revised Phased Implementation Strategy for direct MPS query, workspace part scope, frontend fiscal calendar, and snapshot behavior.
- [x] Define final Stage 5B snapshot/API response candidate.
- [x] Produce Stage 5B Implementation Plan.
- [x] Produce Stage 5B VS Code/Copilot implementation prompt after Stage 5A acceptance.

Durable implementation-plan artifact: `KST_v2_STAGE_5B_IMPLEMENTATION_PLAN.md`.

---

## 5A.14 Required Stage 5A Deliverables

- [x] MPS Data Inventory.
- [x] MPS UI-to-Data lineage (contained in inventory).
- [x] MPS Data Grain Map (contained in inventory/contract).
- [x] MPS Source-System Map (contained in inventory).
- [x] MPS Query Strategy.
- [x] Snapshot and Refresh Strategy.
- [x] MPS Data Quality / Reliability Register.
- [x] MPS SQL-versus-C# Responsibility Map.
- [x] MPS Backend Data Contract.
- [x] Frontend Fiscal Calendar Strategy.
- [x] Database Access / Security / Performance closeout.
- [x] Snapshot metadata/API response candidate.
- [x] Stage 5B Implementation Plan.
- [x] Updated full Master Project Checklist.
- [x] Updated Revised Phased Implementation Strategy.

---

## 5A.15 Stage 5A Completion Gate

Stage 5A is complete only when:

- [x] Initial MPS data requirements are inventoried at the current implementation depth.
- [x] Existing `sp_QAD_ktmpswkm` logic has been reviewed.
- [x] Production MPS retrieval strategy has been selected.
- [x] Required WO/status inputs are identified.
- [x] Workspace filters map cleanly to database scope.
- [x] MPS dataset grains are documented.
- [x] MPS source ownership is documented.
- [x] Known initial-MPS data-quality risks are documented.
- [x] Snapshot strategy is defined.
- [x] Refresh strategy is defined.
- [x] SQL/C#/frontend responsibility boundaries are defined.
- [x] Representative validation cases are selected.
- [x] Database access/security/performance assumptions are closed.
- [x] Final Stage 5B API/snapshot metadata candidate is defined.
- [x] Project documentation is reconciled.
- [x] Stage 5B implementation scope/prompt can be written without remaining infrastructure guesses.
- [x] Project owner accepts final Stage 5A closeout.

**Stage 5A completion gate: PASS.** Stage 5B subsequently completed and was accepted.

---

# Stage 5B — MPS Dashboard Implementation

**Status:** COMPLETE / ACCEPTED — Stage 5 closed out before Stage 6 planning

## Purpose

Implement the MPS dashboard vertical slice using the accepted Stage 5A data/query/snapshot contracts.

---

## 5B.1 QAD Database Integration

- [x] Implement the approved direct parameterized MPS query in `Kst.Integrations.Qad`.
- [x] Use approved SQL Server client/database-access mechanism.
- [x] Implement Windows-integrated QAD connectivity.
- [x] Apply resolved workspace site/domain/parent-part scope.
- [x] Implement parameterized part-list batching/chunking.
- [x] Apply `mrp_dataset = 'wo_mstr'`.
- [x] Apply `mrp_type IN ('supply','supplyf','supplyp')`.
- [x] Apply safe WO join on domain + site + part + WO number + WO ID.
- [x] Exclude `wo_status = 'C'`.
- [x] Exclude `wo_bom_code = 'RMABOM'`.
- [x] Retrieve all historical qualifying unfinished work needed for Falldown.
- [x] Retrieve future source facts sufficient for the maximum 72-week Due/Release views.
- [x] Support cancellation and approved command timeout.
- [x] Log execution diagnostics without leaking sensitive information.

Do not implement `sp_QAD_ktmpswkm` or create a new database procedure for the initial MPS.

---

## 5B.2 MPS Source Normalization

- [x] Map QAD query rows into integration records.
- [x] Normalize into `MpsSourceRow`.
- [x] Normalize SUPPLY/SUPPLYF/SUPPLYP.
- [x] Normalize A/F/R/P/e WO states.
- [x] Handle unexpected non-C WO state defensively.
- [x] Preserve both Due Date and Release Date.
- [x] Preserve WO ID/status references.
- [x] Preserve site/domain diagnostics where required.
- [x] Do not introduce source deduplication unless new evidence requires it.

---

## 5B.3 Week Bucketing / Falldown

- [x] Implement Sunday-Saturday business-week boundary.
- [x] Use Monday as visible week label.
- [x] Implement weekly buckets.
- [x] Implement due-date-based Falldown with no historical lower cutoff.
- [x] Implement maximum 72-week horizon.
- [x] Rebuild Due/Release bucket views from the current source snapshot without QAD re-query.
- [x] Test Sunday/Saturday boundaries and year transitions.

Fiscal period/quarter/year mapping is **not backend work**.

---

## 5B.4 MPS Status Classification

- [x] Implement Allocating (`A`).
- [x] Implement Frozen (`F`).
- [x] Implement Released (`R`).
- [x] Implement Mixed for 2+ distinct A/F/R states.
- [x] Implement `ContainsPlannedWork` from `P`.
- [x] Implement `ContainsExplicitlyScheduledWork` from `e`.
- [x] Implement None when no A/F/R state exists.
- [x] Aggregate quantities across all included WOs in a bucket.
- [x] Add unit tests for mixed P/e/A/F/R combinations.

Shortage status is deferred to the later shortages capability and is not an initial MPS execution state.

---

## 5B.5 Snapshot Integration and Refresh

- [x] Start MPS load automatically when a workspace opens.
- [x] Keep workspace shell usable while MPS loads.
- [x] Keep explicit parent rows visible with no MPS activity.
- [x] Populate snapshot ID / timestamps / source state per Stage 5A contract.
- [x] Preserve old snapshot while refresh runs.
- [x] Replace snapshot atomically after successful load.
- [x] Preserve prior snapshot on refresh failure.
- [x] Show last successful refresh time.
- [x] Implement approved initial database-unavailable message and Retry.
- [x] Prevent concurrent refreshes for one workspace.
- [x] Avoid QAD reload on tab switching, Due/Release toggle, fiscal display changes, or horizon changes ≤72 weeks.
- [x] Do not persist MPS snapshot across application sessions initially.

---

## 5B.6 MPS API

- [x] Define workspace MPS endpoint(s) from the accepted snapshot model.
- [x] Return parent schedules and normalized buckets.
- [x] Return MPS semantic status fields.
- [x] Return snapshot/refresh metadata.
- [x] Support Due/Release and horizon view requests without forcing QAD re-query when snapshot coverage is sufficient.
- [x] Do **not** return fiscal year/period/quarter metadata from backend solely for display.
- [x] Update OpenAPI.
- [x] Regenerate TypeScript contracts.

---

## 5B.7 Frontend Fiscal Calendar / Settings

- [x] Add Fiscal Calendar section to Settings.
- [x] Seed FY26 anchor: June 29, 2025.
- [x] Implement standard 4-4-5 generation.
- [x] Implement 53-week exception records with selected extra-week period.
- [x] Validate exception uniqueness and period range.
- [x] Generate fiscal year/week/period/quarter display metadata in frontend.
- [x] Test 52/53-week transitions and 72-week horizon coverage.

---

## 5B.8 Frontend MPS Grid

- [x] Implement MPS grid shell.
- [x] Implement sticky parent-part/description column.
- [x] Implement horizontal scrolling.
- [x] Implement week headers.
- [x] Implement fiscal period bands.
- [x] Implement fiscal quarter bands.
- [x] Implement schedule quantities.
- [x] Implement A/F/R/Mixed box presentation.
- [x] Implement accessible Planned font treatment.
- [x] Implement explicitly-scheduled non-color marker.
- [x] Implement horizon selector up to 72 weeks.
- [x] Implement Due/Release mode.
- [x] Implement loading, empty, unavailable, stale/refresh, and retry states.
- [x] Implement row/week-cell selection only to the extent required by the initial dashboard slice.

---

## 5B.9 Data Validation

- [x] Compare KST v2 source rows to direct database results.
- [x] Compare schedule totals to source evidence / legacy output where applicable.
- [x] Validate representative sites.
- [x] Validate product-line-derived scope.
- [x] Validate explicit-part scope.
- [x] Validate parent with no MPS rows.
- [x] Validate one-WO and multi-WO buckets.
- [x] Validate A/F/R/Mixed/P/e classification.
- [x] Validate Falldown including an old unfinished WO.
- [x] Validate `RMABOM` exclusion.
- [x] Validate repetitive-schedule change after MRP/QADPRO2 sync.
- [x] Validate empty results.
- [x] Validate large-workspace performance / batching.
- [x] Record discrepancies and resolutions.

---

## 5B.10 Automated Verification

- [x] QAD adapter tests.
- [x] Normalization tests.
- [x] Status-rule tests.
- [x] Business-week/Falldown tests.
- [x] Frontend fiscal-calendar tests.
- [x] Snapshot tests.
- [x] API integration tests.
- [x] Frontend component tests.
- [x] Refresh/error-state tests.
- [x] Architecture-boundary tests.
- [x] Full backend build/test.
- [x] Full frontend lint/typecheck/test/build.
- [x] Rust/Tauri verification.
- [x] Sidecar rebuild.
- [x] Live Tauri manual verification.

---

## 5B.11 Documentation

- [x] Document final direct-query contract.
- [x] Document query parameters and batching behavior.
- [x] Document result/source-row columns.
- [x] Document normalization/status rules.
- [x] Document fiscal settings/calculation behavior.
- [x] Document snapshot/refresh behavior.
- [x] Document MRP freshness dependency and RMA exclusion.
- [x] Update project status.
- [x] Update Master Project Checklist.
- [x] Update API documentation.
- [x] Update data inventory with implementation-confirmed mappings.

---

## 5B.12 Stage 5B Completion Gate

Stage 5B is complete only when:

- [x] A configured workspace loads real MPS data from the approved direct QAD source.
- [x] Workspace site/part scope is validated.
- [x] Schedule quantities are validated.
- [x] Work-order associations are validated.
- [x] MPS semantic classification is validated.
- [x] Falldown and RMA exclusion are validated.
- [x] Refresh/snapshot behavior is validated.
- [x] Due/Release and horizon changes reuse the current snapshot appropriately.
- [x] Frontend fiscal bands are validated.
- [x] The real MPS grid is usable.
- [x] No fake production data remains.
- [x] Loading/error/empty/refresh states work.
- [x] Automated verification passes.
- [x] Representative data matches source evidence.
- [x] Owner acceptance passes.

**Completion gate: PASS.** A scheduler can open a configured workspace and use a validated, cached, real-data MPS grid for schedule review.


## Stage 6 — Phase 3: Part Information Drill-Down ✅

**Status:** COMPLETE / ACCEPTED — 2026-08-11

**Accepted contract:** `KST_v2_STAGE_6_PART_INFO_CONTRACT.md`  
**Implementation prompt:** `KST_v2_STAGE_6_VSCODE_IMPLEMENTATION_PROMPT.md`  
**Implementation/validation record:** `KST_v2_STAGE_6D_IMPLEMENTATION_PROGRESS.md`  
**Closeout record:** `KST_v2_STAGE_6_CLOSEOUT.md`

### Purpose

Selecting an MPS parent part collapses/focuses the grid around that parent and displays validated QAD part-master attributes, inventory summaries, and current MOQ/price information through a lazy-loaded Part Info pane.

Part Info is parent-part scoped, not week scoped. Clicking the selected parent again or using `Back to full grid` restores the full MPS view.

---

### 6A — UI Behavior and Field Discovery ✅

- [x] Parent-row selection is the Stage 6 interaction entry point.
- [x] Selected-parent MPS collapse/focus behavior accepted.
- [x] Part Info opens directly beneath the focused parent row.
- [x] Focused grid shrinks to the selected row without retaining blank full-grid height.
- [x] `Back to full grid` restores the normal MPS view.
- [x] Clicking the selected parent again also restores the full MPS view.
- [x] Keyboard activation follows the same toggle behavior.
- [x] Part Info is parent-part scoped rather than week scoped.
- [x] Due/Release, horizon, fiscal, density, and presentation changes do not reload PartDetail.
- [x] Prototype-only UOM, Item Class, Component Count, WIP, and Part-level MPS schedule status were removed from Stage 6 scope.
- [x] Accepted field set includes Safety Time, QAD Part Status code + description, IOS Code, Qty Non-Net, and MOQ/Current Price tier(s).
- [x] Blank/null informational data is accepted as normal Part Info behavior.

### 6B — Source Mapping and Business Rules ✅

#### Part master

- [x] Part Number → `pt_mstr.pt_part` / selected parent identity.
- [x] Planner → `pt_mstr.pt_buyer`; for manufactured parent parts this is the planner code.
- [x] Mfg Lead Time → selected-site `ptp_det.ptp_mfg_lead` (join: `ptp_domain = pt_domain`, `ptp_part = pt_part`, `ptp_site = ` selected site — **not** `pt_mstr.pt_site`), days.
- [x] Safety Time → selected-site `ptp_det.ptp_sfty_tme` (same join), days.
- [x] Part Status → `pt_mstr.pt_status` with backend-owned description mapping.
- [x] Current Revision → `pt_mstr.pt_rev`.
- [x] Description → `pt_mstr.pt_desc1`.
- [x] IOS Code → `pt_mstr.pt_warr_cd`.
- [x] Safety Stock → selected-site `ptp_det.ptp_sfty_stk` (same join), part units.
- [x] Zero has no special missing/not-configured semantics for informational part-master values.
- [x] Blank/null informational fields may display blank or `No Data Found`.
- [x] `ptp_det` is used (via `LEFT JOIN`, no fallback to `pt_mstr` when the selected-site row is missing) for Mfg Lead Time, Safety Time, and Safety Stock only; planner fallback and a pt_mstr-substitution lead-time/safety-stock fallback are not part of Stage 6 (**correction, this pass:** an earlier version of this checklist incorrectly stated `ptp_det` is not part of Stage 6 at all — see `KST_v2_STAGE_6_PART_INFO_CONTRACT.md` §4 for the accepted, implemented mapping, confirmed against `QadPartDetailReader.BuildPartMasterQuery`).

#### Part Status descriptions

- [x] Raw code is preserved and displayed with the human-readable description.
- [x] Accepted mappings: A=AEMR, B=BYPASS, C=CURRENTLY IN PRODUCTION, E=END OF LIFE, F=FORECAST, H=PURCHASING HOLD, I=INACTIVE PURCHASED PARTS, M=MFA, N=NPI, O=OBSOLETE, P=PROTO, Q=QUOTED PARTS, U=UNRELEASED.
- [x] Unknown status codes preserve the raw code without failing PartDetail.

#### Inventory summary

- [x] Inventory sources are `ld_det` + `loc_mstr` + `is_mstr` at domain + site + part grain.
- [x] Stage 6 queries the exact selected parent; the investigative `EligibleParts` CTE is not used.
- [x] Only `ld_qty_oh > 0` contributes to displayed inventory.
- [x] Zero and negative inventory rows are ignored.
- [x] `ld_lot LIKE 'RA%'` is excluded from both Stage 6 displayed totals.
- [x] Qty On Hand = positive, non-RMA, nettable inventory.
- [x] Qty Non-Net = positive, non-RMA, non-nettable inventory.
- [x] No qualifying inventory rows returns 0 / 0 rather than missing data.

#### MOQ / price

- [x] Price-header source is `pi_mstr`; tier source is `pid_det`.
- [x] Current price-list rule is the latest `pi_start <= today` for selected domain + part.
- [x] No end/expiration-date rule is added.
- [x] MOQ → `pid_det.pid_qty`; Unit Price → `pid_det.pid_amt`.
- [x] One or more MOQ/price tiers are supported and normalized in MOQ order.
- [x] No current price is normal missing informational data, not an error.

### 6C — Backend/API Contract ✅

- [x] Normalized `PartDetail` and `PartPriceBreak` contracts accepted.
- [x] QAD-specific SQL/table/column details remain inside the QAD integration boundary.
- [x] Existing Stage 5 site→domain, connection, authentication, read-only, Dapper/SqlClient, cancellation, timeout, and logging patterns are reused.
- [x] PartDetail validates the selected parent against current workspace/MPS parent scope.
- [x] PartDetail is lazy-loaded rather than preloaded with MPS.
- [x] Data identity is Site + Parent Part.
- [x] Cache/freshness identity is Workspace + Parent Part + current MPS snapshot identity/generation.
- [x] Same-parent/same-snapshot detail is reused.
- [x] Successful workspace refresh makes prior detail stale for next access; failed workspace refresh preserves compatible last-good detail.
- [x] Fresh-detail failure may return stale last-good detail with warning when prior detail exists.
- [x] No PartDetail persistence across sessions initially.
- [x] Endpoint: `GET /api/v1/workspaces/{workspaceId}/part-detail?partNumber={partNumber}`.
- [x] Accepted 404/409/503/200 stale/missing-data semantics implemented.
- [x] C# DTO → OpenAPI → generated TypeScript contract workflow retained.
- [x] Stage 6 contract accepted by project owner.

### 6D — Implementation ✅

#### 6D.0 Repository preflight

- [x] Current Stage 5 QAD integration, snapshot identity, API, frontend, CSS, and test patterns inspected.
- [x] Clean baseline automated verification recorded before Stage 6 changes.
- [x] Stage 6 implementation progress artifact created and maintained.

#### 6D.1 Domain/application

- [x] `PartDetail` / `PartPriceBreak` normalized models implemented.
- [x] Part Status mapping and unknown-code behavior implemented.
- [x] Application orchestration, parent-scope validation, in-memory cache, and stale-last-good behavior implemented.
- [x] Domain/application tests added.

#### 6D.2 QAD integration

- [x] Focused part-master retrieval implemented.
- [x] Focused nettable/non-nettable inventory aggregation implemented with positive-only and RMA-exclusion rules.
- [x] Latest `pi_start <= today` price-list selection and MOQ/price-tier retrieval implemented.
- [x] Price tiers normalized in stable MOQ order.
- [x] Stage 5 QAD infrastructure conventions reused.
- [x] QAD integration tests added using repository test seams/fixtures.

#### 6D.3 API / OpenAPI

- [x] Workspace-scoped PartDetail endpoint and DTOs implemented.
- [x] Accepted Problem Details/outcome mappings implemented.
- [x] API integration tests added.
- [x] OpenAPI spec and generated TypeScript contracts regenerated through the canonical pipeline.

#### 6D.4 Frontend

- [x] Parent rows support mouse and keyboard selection.
- [x] Selected parent collapses/focuses the MPS to the actual selected row rather than hidden placeholders.
- [x] Part Info renders immediately below the focused grid with normal spacing.
- [x] PartDetail lazy load integrated through generated API types.
- [x] `Back to full grid` and selected-parent toggle both clear focus and close Part Info.
- [x] Accepted PartDetail fields, status code+description, single/multiple price-tier presentation, and normal no-data behavior implemented.
- [x] Loading, missing-part, error/retry, and stale-last-good states implemented.
- [x] Due/Release/horizon/fiscal presentation changes do not trigger PartDetail refetch.
- [x] Frontend component/state tests added.

### 6E — Validation and Verification ✅

#### Automated verification

- [x] Part Status mappings, unknown status, blank/null values, inventory classification/exclusions, pricing-effective-date rules, single/multiple/no-price cases, cache/refresh/stale behavior, API outcomes, and frontend interactions covered.
- [x] Backend final suite: **316/316 tests passing**.
- [x] Backend format/build verification clean.
- [x] Frontend final suite after owner-review refinements: **119/119 tests passing**.
- [x] Frontend lint, typecheck, and production build clean.
- [x] Rust/Tauri `cargo check` clean.
- [x] Backend sidecar rebuilt after backend changes.

#### Live QAD / manual validation

- [x] Live validation performed against read-only `QADPRO2` across five available development workspaces and 71 representative parent parts.
- [x] Happy-path PartDetail response validated end to end.
- [x] Workspace 404, MPS-not-loaded 409, invalid request, and out-of-scope-part behavior validated safely.
- [x] Repeated same-parent access verified cache reuse through identical `loadedAtUtc`.
- [x] Representative Part Status codes including C/P/B validated.
- [x] Positive nettable and non-nettable quantities cross-checked against direct read-only SQL and matched.
- [x] Current price-list selection cross-checked against direct read-only SQL; latest `pi_start <= today` behavior confirmed.
- [x] Rare live cases not naturally present in the available validation set (multi-tier `pid_det`, RMA exclusion, no-current-price, additional site/domain) were covered by deterministic automated tests; no QAD data was modified to manufacture examples.
- [x] Full Tauri desktop owner-review click-through completed after UI refinements.
- [x] Focused-grid spacing, row-toggle close behavior, `Back to full grid`, and keyboard behavior manually verified.
- [x] Tauri shutdown verified without orphan processes.

### 6F — Documentation and Closeout ✅

- [x] Stage 6 contract documented and accepted.
- [x] Implementation/validation progress recorded.
- [x] Backend boundary and API-contract documentation updated.
- [x] Master Project Checklist reconciled to implemented Stage 6 behavior.
- [x] Current Project Status advanced beyond Stage 6.
- [x] Owner-review UI refinements documented and verified.
- [x] Project-owner acceptance received on **2026-08-11**.
- [x] Record final Stage 6 commit hash after the repository commit is made. Commit: `863a638` (`feat: complete Stage 6 part information drill-down`).

### Stage 6 completion gate

- [x] Selecting an MPS parent collapses/focuses the grid and opens Part Info directly beneath it.
- [x] Clicking the selected parent again or `Back to full grid` restores the full MPS grid.
- [x] Accepted PartDetail fields use validated authoritative QAD sources/rules.
- [x] Part Status code + description is validated.
- [x] Qty On Hand / Qty Non-Net are validated.
- [x] Current MOQ/price selection and multi-tier contract behavior are validated.
- [x] PartDetail contract is typed through C# → OpenAPI → generated TypeScript.
- [x] Lazy-load/cache and stale-last-good behavior are validated.
- [x] Loading, missing, error, retry, and stale states work.
- [x] Automated verification passes.
- [x] Representative live-QAD/direct-SQL comparisons pass.
- [x] Project-owner acceptance received.

**Completion gate: PASS.** Selecting an MPS parent part displays validated QAD part-master attributes, inventory summaries, and current MOQ/price information through an accepted lazy-loaded drill-down workflow.

## Versioning Foundation (inter-stage housekeeping)

This is administrative housekeeping performed between Stage 6 and Stage 7 — it is **not**
part of Stage 7 and does **not** renumber or otherwise affect any stage below. Stage 7 had
not yet begun at the time this housekeeping was performed (Stage 7 is now complete and
accepted — see below).

- [x] Product identity `KST v2` established as distinct from the semantic application version.
- [x] Single authoritative version source: `src/backend/Directory.Build.props`
  (`VersionPrefix`/`VersionSuffix`).
- [x] Initial application version set: `0.1.0-alpha.1` (SemVer 2.0.0).
- [x] Owner adopted stage-aligned alpha numbering on 2026-09-29: Stage `N` uses
      `0.1.0-alpha.N`; the current Stage 13 planning version is `0.1.0-alpha.13`.
- [x] `KstActiveStage` plus the version-consistency guard enforce the active-stage relationship.
- [x] Version propagated to backend assemblies (`InformationalVersion`, system status/health
  endpoints, startup logs, frontend top bar), `src/tauri/Cargo.toml`, `src/frontend/package.json`.
- [x] `src/tauri/tauri.conf.json` kept numeric-only (`0.1.0`) — MSI/WiX installer bundling
  rejects non-numeric SemVer pre-release identifiers (empirically discovered/documented).
- [x] Repeatable sync/check script added: `scripts/check-version.ps1` (supports `-Fix`).
- [x] Automated drift guards added: 2 backend integration tests
  (`SystemStatusEndpointTests`), 3 `Kst.ArchitectureTests` (`VersionConsistencyTests`).
- [x] Full documentation added: `docs/development/VERSIONING.md`.
- [x] Full verification: backend 329/329 tests, frontend 121/121 tests, backend
  format/build clean, frontend lint/typecheck/build clean, `cargo check`/`cargo build`
  clean, sidecar rebuilt, manual `tauri dev` verification, packaged Windows build
  (NSIS + MSI) verified.
- [x] No auto-update, release-channel, or CI/CD infrastructure introduced.
- [x] No configuration-schema migrations introduced (configuration schema versioning
  remains a distinct, not-yet-existing concept — see `docs/development/VERSIONING.md`).

## Stage 7 — Phase 4: Work Orders and Kitting ✅

**Status:** COMPLETE / ACCEPTED — owner acceptance recorded 2026-08-13

### 7.1 Field and rule discovery (accepted/implemented — see `KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md`)

- [x] ~~Map work-order number~~ — deliberately NOT mapped/displayed; WOID (`wo_mstr.wo_lot`) is the scheduler-facing identity instead (Work Order Number is not unique)
- [x] Map ordered quantity — `wo_mstr.wo_qty_ord`
- [x] Map completed quantity — `wo_mstr.wo_qty_comp`
- [x] Map open quantity — derived `Ordered - Completed` (no separate QAD field; confirmed with project owner)
- [x] Map status — `wo_mstr.wo_status`, restricted to eligible `A`/`F`/`R`
- [x] ~~Map start date~~ — not part of the accepted card
- [x] Map due date — `wo_mstr.wo_due_date`
- [x] ~~Map production line~~ — not part of the accepted card
- [x] ~~Identify allocation fields~~ — no allocation fields in the accepted card
- [x] Define kitting percentage — line-based: fully-issued line count / applicable line count × 100, null (not 0%) at zero applicable lines
- [x] Map component requirements — `wod_det.wod_qty_req`
- [x] Map issued quantities — `wod_det.wod_qty_iss`
- [x] Define variance quantity — derived `Issued - Required`
- [x] Define variance percentage — accepted terminology is **Issued %**, not Variance %: `Issued / Required × 100`
- [x] Confirm severity thresholds — `<=95%` under-issued exception, `>=105%` over-issued exception, else within expected range
### 7.2 Backend (implemented — see `KST_v2_STAGE_7_WORK_ORDER_KITTING_DATA_INVENTORY.md`)

- [x] Create work-order adapter — `QadWorkOrderSummaryReader`
- [x] Create WO-material adapter — `QadWorkOrderMaterialReader`
- [x] Define WorkOrderSummary — `Kst.Domain.WorkOrders.WorkOrderSummary`
- [x] Define WorkOrderMaterialLine — `Kst.Domain.WorkOrders.WorkOrderMaterialLine`
- [x] Create work-order service — `WorkOrderDrilldownService` (single orchestration service for all three use cases; no separate Kitting/Variance services, per accepted contract)
- [x] ~~Create kitting service~~ — folded into `WorkOrderDrilldownService`/`KittingSummary`, not a separate service
- [x] ~~Create variance service~~ — folded into `WorkOrderMaterialLine`'s computed properties, not a separate service
- [x] Join work orders to schedule buckets — reuses `MpsWorkOrderRef` already retained per bucket by `MpsScheduleBuilder`; never re-derived from WO dates
- [x] Add work-order summaries to cached MPS data or lazy detail — lazy-loaded and cached by workspace + MPS snapshot generation
- [x] Create work-order endpoints — `WorkOrderEndpoints` (bucket, material, candidates)
### 7.3 Frontend and validation

- [x] Build work-order cards — `WorkOrderCard.tsx`
- [x] Build selected-week filtering — bucket/Falldown selection drives which WOs load
- [x] ~~Build all-open-WO view~~ — not part of the accepted contract; no parent-only "all open WO" view exists
- [x] Build kitting expansion — `WorkOrderMaterialGrid.tsx`
- [x] Build variance sorting — exceptions first, larger departures from 100% Issued before smaller
- [x] Build no-WO state — empty A/F/R result renders a deliberate non-error empty state
- [x] Compare against WO Variance report — see `docs/implementation/STAGE_7_REAL_DATA_VALIDATION.md`
- [x] Validate partial issue — covered by automated `WorkOrderIssueStatusClassifier` tests + live data
- [x] Validate over-issue — over-issued lines confirmed to count as fully issued
- [x] Validate completed work orders — `C` (Closed) WOs confirmed excluded from candidate results against real data
- [x] Owner acceptance — **ACCEPTED, 2026-08-13**
Phase 4 completion gate: **PASS. Stage 7 accepted by the project owner, 2026-08-13.** A scheduler can trace an MPS bucket to its work orders and component issue status, and can navigate bounded manufactured-subassembly candidates up to three levels deep.

---

## Stage 8 — Phase 5: Component and BOM Detail ✅

**Status:** COMPLETE / ACCEPTED — owner acceptance recorded 2026-08-21

Stage 8 is an **informational Component/BOM investigation capability**. It does not implement
material-requirement netting, shortage classification, or PO coverage.

### Accepted BOM behavior

- [x] Current effective multi-level BOM from `ps_mstr`.
- [x] Structural hierarchy/order and actual levels preserved.
- [x] Repeated component occurrences remain distinct; no flattening/deduplication of structural rows.
- [x] Phantoms are shown and exploded through.
- [x] BOM search plus local P/M and Phantom filters implemented with combined AND semantics.
- [x] Effective P/M uses selected-site `ptp_det.ptp_pm_code`, otherwise `pt_mstr.pt_pm_code`
      fallback for P/M only.
- [x] Net QOH / Non-Net QOH reuse the shared Stage 6 inventory semantics.

### Accepted Component Information behavior

- [x] Selecting a BOM row opens a blocking Component Information modal without leaving BOM context.
- [x] Modal closes by explicit close or Escape, not backdrop click, and restores focus to the
      originating BOM row.
- [x] Selected-site planning fields and accepted component attributes are displayed with null/zero
      distinction preserved.
- [x] Standard Cost uses `sct_sim = 'Standard'` and the latest `sct_cst_date`.
- [x] QCTC uses `inp_source = 'qtbom_det'` and the latest `inp_start_date`.
- [x] Approved Alternates is the user-facing term; technical `ApprovedVendor` / `vp_mstr` naming may remain.
- [x] Approved Alternates lazy-load independently, preserve source ordering/multiplicity, and retain
      localized empty/error behavior.
- [x] Integrated automated verification, live read-only validation, sidecar rebuild, and owner-guided
      desktop validation completed.

### Intentionally deferred — not unfinished Stage 8 work

- [~] Show MRP.
- [~] Inventory / Lot Locations.
- [~] Extended Requirement.
- [~] Incoming Supply.
- [~] Coverage / Material Status.
- [~] Component MRP / component supply netting.
- [~] Future Shortages / PO coverage.

**Stage 8 completion gate: PASS / ACCEPTED.**

---

## Cross-Cutting Checkpoint — UI Navigation & Keyboard Ergonomics A ✅

**Status:** COMPLETE / ACCEPTED

The accepted interaction convention is **hierarchical Escape unwind**, not browser-history navigation
and not modal-only dismissal:

1. Topmost blocking modal/dialog → close/cancel only that surface.
2. Nested detail in the current investigation → collapse exactly one level.
3. Main MPS detail/drill-down → return to the Part Matrix.
4. Part Matrix/root → do nothing.

Accepted examples:

- [x] Component Information → BOM.
- [x] BOM → Part Matrix.
- [x] Nested material/candidate detail → prior material level.
- [x] Show Material Lines → Work Order view.
- [x] Work Order view → Part Matrix.
- [x] Part Info → Part Matrix.
- [x] Keyboard activation parity for the accepted interactive MPS bucket behavior.

Ergonomics B items such as arrow-key tab navigation, shared focus-trap extraction, menu roving focus,
and additional shortcut hints remain deferred unless explicitly revisited.

---

## R0 — Repository / Documentation Reconciliation

**Status:** COMPLETE / ACCEPTED — 2026-08-21

R0 establishes the clean accepted repository baseline before Security Foundation enactment. It also
satisfies the Security Foundation draft's S0.0 Repository Reconciliation prerequisite.

### R0.1 — Read-only repository documentation inventory

**Status:** COMPLETE / ACCEPTED

- [x] Inventory repository-controlled documentation and instruction files.
- [x] Inventory relevant documentation/source/test/script directory structure.
- [x] Record tracked/untracked state and useful provenance for ambiguous documents.
- [x] Identify links/references to documents that may later move or be superseded.
- [x] Search deliberately for known stale assumptions and obsolete stage language.
- [x] Produce evidence-backed candidate classifications without moving/deleting/rewriting files.
- [x] Owner review of inventory before disposition work.

### R0.2 — Authority and contradiction map

**Status:** COMPLETE / ACCEPTED

- [x] Classify documents as Canonical, Current Stage/Implementation Artifact, Historical Stage Artifact,
      Superseded, Duplicate, Mislocated, Needs Update, or Candidate for Archive/Removal.
- [x] Identify competing current-status / roadmap / architecture / source-map claims.
- [x] Record what supersedes each stale artifact and whether unique evidence must be retained.
- [x] Establish proposed dispositions before any large moves/deletes.
- [x] Owner review of disposition map.

### R0.3 — Core project-state reconciliation

**Status:** COMPLETE / ACCEPTED

- [x] Reconcile authoritative current project status through Stage 8 + Ergonomics A.
- [x] Reconcile this Master Project Checklist.
- [x] Reconcile phased implementation strategy without casually redesigning Stage 9+.
- [x] Record R0 → S0 → Stage 9 sequencing durably.

### R0.4 — Stage-history reconciliation

**Status:** COMPLETE / ACCEPTED

- [x] Reconcile Stage 5 artifacts.
- [x] Reconcile Stage 6 artifacts.
- [x] Reconcile Stage 7 artifacts.
- [x] Reconcile Stage 8 artifacts.
- [x] Reconcile UI Navigation & Keyboard Ergonomics artifacts.
- [x] Preserve historical evidence while making superseded planning unmistakable.

### R0.5 — Architecture and development documentation reconciliation

**Status:** COMPLETE / ACCEPTED

- [x] Reconcile technical foundation and project boundaries against implementation.
- [x] Reconcile setup/build/test/package/sidecar/OpenAPI workflows.
- [x] Reconcile troubleshooting and local coding-agent workflow.
- [x] Reconcile `AGENTS.md` and platform-specific instruction precedence.
- [x] Avoid churn where documentation is already accurate.

### R0.6 — Data/source documentation reconciliation

**Status:** COMPLETE / ACCEPTED

- [x] Reconcile QAD/source maps against accepted Stage 5–8 implementation evidence.
- [x] Preserve source grain, selected-site behavior, null/zero semantics, and accepted query rules.
- [x] Ensure obsolete Stage 8 netting/coverage assumptions cannot masquerade as unfinished work.

### R0.7 — Documentation navigation and authority

**Status:** COMPLETE / ACCEPTED

- [x] Establish an explicit documentation authority/index model.
- [x] Make canonical current status, roadmap, architecture, source maps, stage evidence, historical
      artifacts, and instruction precedence easy to locate.
- [x] Preserve repository documentation as durable project memory; agent memory remains retrieval assistance only.

### R0.8 — Reconciliation verification / closeout

**Status:** COMPLETE / ACCEPTED

- [x] Run a final stale-assumption / contradiction / broken-reference search.
- [x] Verify no accepted Stage 5–8 or Ergonomics A behavior was regressed in documentation.
- [x] Verify no Stage 9 implementation has begun.
- [x] Establish and record the accepted reconciled baseline commit.
- [x] Owner acceptance of R0.

Durable closeout evidence: `docs/status/R0_REPOSITORY_RECONCILIATION_CLOSEOUT.md`.

---

## S0 — Security Foundation Integration

**Status:** COMPLETE / ACCEPTED — 2026-08-31

Do not mix large documentation reconciliation with security remediation.

### S0.1 — Security Policy Injection

**Status:** COMPLETE / ACCEPTED — 2026-08-21

- [x] Finalize repository security entry point and platform-neutral security policy locations after
      R0 determines the authoritative documentation structure.
- [x] Enact Security Assurance Policy.
- [x] Enact Development Environment Security policy.
- [x] Enact Dependency Admission policy.
- [x] Enact AI Security Review policy.
- [x] Enact KST Application Security Profile.
- [x] Update `AGENTS.md` with concise mandatory agent behavior and links to authoritative policy.
- [ ] Add only thin platform-specific security adapters after verifying supported mechanisms
      (not performed in S0.1 — no supported mechanism was confirmed to exist).
- [x] Do not add a new scanner merely because security work has started.

The policy documents above are enacted, owner-accepted Tier 1 authority as of 2026-08-21.

### S0.2 — Security Baseline Discovery

**Status:** COMPLETE / ACCEPTED — 2026-08-24

- [x] Inventory application dependencies: NuGet, npm, Cargo.
- [x] Inventory development dependencies: SDKs, generators, build/Tauri tooling.
- [x] Inventory active agent platforms, extensions/packages/skills/MCP servers/instruction files as applicable.
- [x] Inventory network listeners, CORS, CSP, Tauri capabilities, subprocesses, filesystem use,
      credential paths, and database access.
- [x] Produce the observed Security Baseline (`docs/security/SECURITY_BASELINE.md`).
- [x] Do not automatically remediate every discovered issue.

Observed against commit `4b4ba3f6089321d5fd1c105c8f5762aed68c303d`. Three observations were
initially recorded (`S0.2-F001`, `S0.2-F002`, `S0.2-F003`); following a 2026-08-24 correction using
project-owner/IT-provided operational authority on QAD authentication/transport/authorization,
`S0.2-F002` is retired and `S0.2-F003` is reclassified to `Confirmed` (configuration does not
accurately express the IT-confirmed required `Encrypt=false` transport; the underlying
unencrypted-transport constraint is not marked `Accepted Risk` — formal IT/security risk
acceptance remains unresolved). `S0.2-F001` remains `Potential / Investigation Required`. The
baseline is observational, not normative policy, and is owner-accepted.

### S0.3 — Existing-Tool Security Checks

**Status:** COMPLETE / ACCEPTED — 2026-08-24

- [x] Determine what .NET/NuGet, npm, Cargo, compiler/analyzer, repository tests, OS inspection, and
      repository search already provide.
- [x] Record useful signal, gaps, false positives, and execution cost before admitting new tooling.

Executed with the repository's existing toolchain only: no tool installation/activation, no
remediation, no dependency manifest/lockfile change, no configuration or security-control
change, no database connection or SQL, and no application/sidecar launch. Evidence:
`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (accepted S0.3 verification/check
evidence; not normative policy). Results: backend analyzer build clean and
656/656 tests passing (incl. `DependencyRuleTests` 6/6, `VersionConsistencyTests` 3/3,
`CorsPolicyTests` 2/2); frontend lint/typecheck clean and 281/281 tests passing; Rust
`cargo clippy --locked --offline` 2 style-only warnings, 0 tests exist; NuGet native advisory
check (incl. transitive, no restore) reported no known advisories for the evaluated graph;
npm native advisory check reported 3 advisories, all development-only — recorded as
**S0.3-F001 (Confirmed)**, not remediated in this checkpoint; no authorized/available Rust
dependency advisory scanner (gap). `S0.2-F001` remains `Potential / Investigation Required`;
`S0.2-F002` remains retired; `S0.2-F003` re-verified still present. Ten coverage gaps
(S0.3-G001–G010) and candidate later capability categories recorded — no product/format/
platform selection made. `SECURITY_BASELINE.md` unchanged.

### S0.4–S0.8 — Remaining S0 Work (approved roadmap)

**Approved Planning Baseline — 2026-08-24.** Scope, boundaries, and the finding/gap-to-
checkpoint mapping live in `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md`
(approved active planning; not normative policy). Roadmap approval does not complete any
checkpoint. No finding has been remediated, no product has been selected, and no tool has
been installed. Existing finding/gap IDs (S0.2-F001/F002/F003, S0.3-F001, S0.3-G001–G010)
are not renumbered.

- [x] S0.4 — Security Finding Disposition & Bounded Remediation (COMPLETE / ACCEPTED — 2026-08-25).
  - [x] S0.4A — QAD SQL Transport Correction (COMPLETE / ACCEPTED — 2026-08-25 — resolves
        `S0.2-F003` at the application-configuration level — see
        `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`).
  - [x] S0.4B — Tauri Shell Capability (COMPLETE / ACCEPTED — 2026-08-25 — resolves
        `S0.2-F001` — see `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`).
  - [x] S0.4C — npm Development-Tooling Advisories (COMPLETE / ACCEPTED — 2026-08-25 — resolves
        `S0.3-F001` — see `docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md`, accepted).
- [x] S0.5 — Security Regression & Architecture Checks (COMPLETE / ACCEPTED — 2026-08-26 — implemented 2026-08-25 — repository regression protection for loopback binding, Tauri CSP, accepted CORS origin set, and read-only QAD SQL; S0.3-G004 covered by accepted S0.4B tests — see `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md`).
- [x] S0.6 — Security Tool Admission (COMPLETE / ACCEPTED — 2026-08-27 — Capability Review 1: Rust Dependency
      Advisory Capability (`S0.3-G001`) — **COMPLETE / ACCEPTED — 2026-08-26** —
      cargo-audit 0.22.2 ADMITTED / ACCEPTED; cargo-deny 0.20.2 DEFERRED — see
      `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`; S0.3-G001 — Covered / Resolved;
      Capability Review 2: Dedicated Secret Scanning (`S0.3-G007`) — **COMPLETE / ACCEPTED —
      2026-08-27** (Gitleaks v8.30.0 installed, release-integrity and
      synthetic-canary verified, scanned current KST content (4 findings) and full Git history
      (8 findings), all rule `private-key`, confirmed documentation false positives;
      `S0.3-G007` — Covered / Resolved) — see
      `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md`;
      `docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`; Gitleaks v8.30.1, TruffleHog v3.97.1,
      detect-secrets v1.5.0 DEFERRED;
      Capability Review 3: Software Bill of Materials (`S0.3-G008`) — **COMPLETE /
      ACCEPTED — 2026-08-27** (Anchore Syft v1.51.1 installed,
      release-integrity verified, run against KST build/repository evidence (SPDX 2.3, 1,027
      packages; CycloneDX 1.6, 1,026 components) and a complementary packaged-artifact view
      (published `Kst.Api` sidecar, 37 NuGet packages recovered directly); six informational
      findings `S0.6-F014`–`S0.6-F019` recorded, none blocking; complete Tauri Windows
      installer/application bundle Unable to Verify / future packaged-release verification
      boundary, not Accepted Risk) — see
      `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md` (neutral research packet, not a
      recommendation or admission decision); `docs/security/S0_6_SBOM_ADMISSION.md` (owner
      decision and implementation evidence; Anchore Syft v1.51.1 — ADMITTED / IMPLEMENTED /
      ACCEPTED; Microsoft sbom-tool v4.1.5 and the CycloneDX
      ecosystem-native approach —
      cyclonedx-dotnet 6.2.0, cyclonedx-npm 6.0.1, cargo-cyclonedx 0.5.9 — DEFERRED);
      `S0.3-G008` — Covered / Resolved;
      Capability Review 4: Dedicated Static Application Security Testing (SAST) (`S0.3-G006`) —
      **COMPLETE / ACCEPTED — 2026-08-27** (owner reviewed the neutral research
      packet comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI
      v1.0.90 and admitted DevSkim CLI v1.0.90, which was installed, self-verified,
      synthetically validated (C#, JavaScript/TypeScript, Rust, SQL), and run against the KST
      source tree (50 findings across 3 bundled rules; `S0.6-F020` reviewed 2026-08-27 and
      reclassified to Informational / Framework-Local Origin / Confirmed DevSkim False
      Positive for plaintext-network interpretation; `S0.6-F021` — Informational / Known
      DevSkim Rule Limitation; neither Accepted Risk)) — see `docs/security/S0_6_SAST_ADMISSION_RESEARCH.md`
      (neutral research packet, not a recommendation or admission decision) and
      `docs/security/S0_6_SAST_ADMISSION.md` (owner decision, full implementation evidence, and
      2026-08-27 project-owner acceptance);
      Semgrep CE v1.175.0 and CodeQL CLI v2.26.4 DEFERRED, not rejected, pending organizational
      licensing/entitlement review; `S0.3-G006` — Covered / Resolved; Microsoft DevSkim CLI
      v1.0.90 — ADMITTED / INSTALLED / VERIFIED / ACCEPTED. All four S0.6-assigned gaps
      (`S0.3-G001`, `S0.3-G006`, `S0.3-G007`, `S0.3-G008`) are Covered / Resolved.
- [x] Third-Party Software & Licensing Governance Foundation (**ENACTED / ACCEPTED — 2026-08-27** —
      see `docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md`; integrated into
      `docs/security/DEPENDENCY_ADMISSION.md` and `AGENTS.md` as a licensing/commercial-use
      admission gate supplementing, not replacing, existing security admission requirements; a
      governing prerequisite for future third-party dependency/tool admission, including the
      SAST candidate admission under S0.3-G006 (since resolved via Capability Review 4); not
      itself an S0 checkpoint or an admission decision for any specific component).
- [x] S0.7 — Runtime & Infrastructure Verification (**COMPLETE / ACCEPTED — 2026-08-28** — S0.7A — Local Release Runtime Verification working pass — **COMPLETE / ACCEPTED — 2026-08-28** — 2026-08-27 release-built runtime evidence VALID / ACCEPTED AS EVIDENCE by owner review: loopback-only `127.0.0.1` sidecar listener on OS-assigned port with no wildcard/LAN listener, clean sidecar lifecycle with no orphans, runtime CORS matching the accepted five-origin allowlist, release-build CSP/capability artifact evidence; safe `ASPNETCORE_URLS` loopback precedence test confirmed the operator environment override alters the effective listener. 2026-08-28: **`S0.5-F001` — Confirmed Runtime Configuration Weakness / REMEDIATED AND VERIFIED BY S0.7** — the sidecar now unconditionally sets an explicit `http://127.0.0.1:<port>` `UseUrls` endpoint (verified on the shipped self-contained .NET 10 release runtime to ignore an inherited `ASPNETCORE_URLS` value after the fix), so inherited hosting configuration no longer takes authority over the listener; failure-safe behavioral regression tests (no test can create a wildcard listener even in its failing state; original wildcard real-process test replaced before acceptance — see evidence §26.3; incl. demonstrated pre-fix failure), 672/672 backend suite, post-fix release-runtime re-verification (env value no longer controls listener selection); **S0.3-G009 — Covered / Resolved** (accepted with S0.7A — 2026-08-28); **`S0.7-F001`** — Operational / Package-Identity Coexistence Issue — Deferred for packaging/deployment decision (KST v1 ↔ KST v2 single-instance interception); S0.7B — database/infrastructure permission verification incl. `S0.3-G010` — **COMPLETE / ACCEPTED — 2026-08-28** (`S0.3-G010` — **Covered / Resolved — 2026-08-28**; **`S0.7-F002`** — **RETIRED** / Application-vs-Enterprise Identity Scope Model Corrected — 2026-08-28 owner scope decision; NOT Accepted Risk; NOT a waived vulnerability; NOT evidence deletion) — see `docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md` + `docs/security/S0_7_DATABASE_INFRASTRUCTURE_PERMISSION_VERIFICATION.md`).
- [x] S0.8 — Independent Assurance & S0 Closeout (**COMPLETE / ACCEPTED — 2026-08-31** — project-owner acceptance recorded; S0 — **COMPLETE / ACCEPTED — 2026-08-31** — see `docs/security/S0_8_INDEPENDENT_ASSURANCE_CLOSEOUT.md` and `docs/security/KST_V2_SECURITY_IMPLEMENTATION_REPORT.md`; Stage 9 — **UNBLOCKED / NOT STARTED**).

### Security decisions intentionally unresolved

- [~] Final severity thresholds.
- [~] Organizational risk-acceptance authority.
- [~] Approved external AI provider list.
- [~] Exact SBOM format.
- [~] Exact vulnerability scanner / SAST product.
- [~] CI/CD platform.
- [~] Final development-environment risk tiers / isolation technology.
- [~] Mandatory frontier-model review triggers.
- [~] Portfolio-wide policy.

---

## Stage 9 — Phase 6: Immediate Work-Order Shortages

**Status:** **COMPLETE / ACCEPTED / LOCKED — 2026-09-08**. Automated audit (9.7), live-QAD validation (9.8), full regression (9.9), documentation reconciliation (9.10), and owner acceptance (9.11) are complete. Current authority: `docs/implementation/KST_v2_STAGE_9_CLOSEOUT.md`.

> Earlier Stage 9 planning language is retained as historical evidence. The original `Due This Week`, receipt-coverage, severity-sorting, and MPS-bucket shortage concepts are superseded; they are not current behavior.

- [x] Immediate planning population: Stage 7R Falldown plus active-basis Weeks 0–3; closed and RMABOM excluded.
- [x] Analytical grain: **Work Order + Component**; no combined/component-wide shortage netting.
- [x] Requirements: Actual `wod_det` for R, A, and `E + wo_type = F`; release-date-effective Projected BOM for ordinary E/F/P; other unfinished status is a loaded Data Issue. Successful empty Actual material is a loaded zero-row analysis, not planning-window absence.
- [x] Purchased-material participation uses authoritative master `pt_mstr.pt_pm_code` for Actual and Projected paths: `M` is visible/drillable `NotApplicable`, nonblank non-`M` is eligible, and null/blank/whitespace is Unknown/Data Issue. Stage 8 effective site P/M remains separate.
- [x] Usable inventory is physical usable-now inventory; activity buckets and issue policy are informational. Issue policy precedence is site `ptp_det.ptp_iss_pol`, master `pt_mstr.pt_iss_pol`, then true default, independent of master-row existence.
- [x] Allocation is hybrid: usable `lad_det` hard reservations reduce shared free inventory and the evaluated WO receives its clamped own hard coverage; committed R then A sequential allocation consumes residual free inventory only inside the immediate window; uncommitted WOs independently read the resulting advisory shared pool. `wod_qty_all` is excluded. Hard allocations are not window-bounded and do not require a matching `wod_det` row.
- [x] PO/KSS is informational only and never reduces Short. KSS is an independent effective supplier-schedule relationship, not a conventional-PO condition. No `po_mstr.po_stat` predicate is accepted pending closed-PO evidence.
- [x] Public component results include allocation provenance: `allocationMode`, hard coverage, uncovered requirement, available-at-evaluation, and allocated quantity.
- [x] Workflow: MPS → Work Orders → Show/Hide Material Lines → one selected integrated analysis beneath the full card strip. The Shortages tab retains selected-WO context; `Select for Shortages` is retired. Applicable purchased shortages may mark a WO card; no MPS-grid shortage marker exists.
- [x] Escape hierarchy: Material Detail → nested manufactured/second-level analysis → containing analysis levels → selected top-level analysis → Work Orders → root, one level per Escape with focus restoration.
- [x] Verification: 51 grouped scenarios (32 DIRECT, 15 COMPOSITE, 1 SUPERSEDED, 3 NOT AUTOMATABLE HERE, 0 MISSING); live-QAD final classifications 1 CONFIRMED, 8 CONFIRMED WITH QUALIFIER, 2 NOT OBSERVED, 1 OWNER/QAD EXPERT DECISION REQUIRED, 0 unresolved CONTRADICTED; full backend/frontend/Tauri/sidecar regression and owner-retested manual families 1–18 passed.
- [x] Stage 9.10 documentation, checklist, current-status, and data-map reconciliation completed; closeout recorded.
- [x] Stage 9.11 — project-owner acceptance recorded 2026-09-08.

**Completion gate:** PASS. Stage 9 is **COMPLETE / ACCEPTED / LOCKED**. A scheduler can identify immediate purchased-material shortages for near-term WOs using authoritative requirements, usable-now inventory, hybrid allocation, and informational PO/KSS context. `po_mstr.po_stat` remains a deferred evidence item and does not block Stage 9 acceptance.

### Accepted inter-stage UI foundation

**Status:** **COMPLETE / ACCEPTED**. This is an inter-stage frontend/UI-shell update, not a numbered
Stage 10 checkpoint. Current authority:
`docs/implementation/KST_v2_PRE_STAGE_10_UI_FOUNDATION_CLOSEOUT.md`.

- [x] Customer/workspace header below workspace tabs with display name and Active Parts from the
      active workspace MPS snapshot's `resolvedParentPartCount`.
- [x] Customer-level Dashboard, Planning, Component Orders, and Finished Goods module navigation;
      future modules are intentional unavailable surfaces without invented data or rules.
- [x] Dashboard MPS, selected-part, Work Orders, Shortages, selected Work Order, Escape, and focus
      behavior preserved while the shared workspace snapshot supplies the header count.
- [x] Bottom status bar is the sole backend connection-status location, with the state-colored dot
      and configuration-warning visibility; duplicate top-bar status and unrelated global Refresh,
      Snapshot, and Last successful refresh controls remain removed.
- [x] Frontend typecheck, lint, full tests, production build, and diff-whitespace validation passed.

Stage 10 field discovery, purchase-order rules, API contracts, QAD mappings, and Component Orders
data population are superseded by the accepted Stage 10 closeout below.


## Stage 10 — Phase 7: Purchase-Order Drill-Down

**Status:** **COMPLETE / ACCEPTED / LOCKED — 2026-09-14**. Current authority:
`docs/implementation/KST_v2_STAGE_10_COMPONENT_ORDERS_CLOSEOUT.md`.

- [x] Active-workspace Component Orders scope: current-effective multi-level BOM components from
      the resolved MPS parents of the current snapshot.
- [x] QAD conventional-open-PO population: raw `pod_qty_ord - pod_qty_rcvd > 0` and
      case-insensitive `pod_status` exclusion of C/X; no UOM conversion and no `po_stat` predicate.
- [x] Component grouping and ordering: earliest due PO collapsed row; expanded rows in due-date,
      PO, then line order; missing due dates are yellow exceptions and dates on/before the applicable
      Friday cutoff are red/late.
- [x] Delivered QAD context: description/master-data fallback, weeks lead time, PO/line/due/open
      quantity, line confirmation, supplier, workspace-domain buyer, manufacturer item, tracking,
      and independent effective KSS indicator.
- [x] Read-only Shortages enrichment: latest active exact site/component Current Comments, Credit
      Hold, and CIA. Source failure keeps QAD rows visible with enrichment unavailable; multiline
      comments are available in an accessible dialog.
- [x] Supplier risk: exact supplier-display-name lookup to `PreferredSuppliers.[Supplier Name]`,
      with `Date DESC, ID DESC` duplicate precedence. Historical `po_vend`/Supplier-Nbr evidence is
      retained but is not the runtime rule.
- [x] API/OpenAPI/generated TypeScript contract, frontend Component Orders panel, focused tests,
      backend solution build, production frontend build, and owner live UAT.
- [x] Owner acceptance recorded 2026-09-14.

### Intentional Stage 10 exclusions

- [~] PO coverage, projection, netting, and projected clear-date calculations are intentionally
      deferred to Stage 11; Stage 10 is informational only.
- [~] Current Comments are read-only. No ShortageMaster write, local note persistence, or
      export-note update was authorized or delivered.
- [ ] Separate PO drill card and previous/next PO navigation are not part of the accepted
      informational Component Orders scope.
- [ ] Write-capable buyer notes are not part of the accepted informational Component Orders scope.
- [ ] A no-open-PO detail is not part of the accepted informational Component Orders scope; the
      delivered empty state truthfully reports no qualifying open PO lines.
- [~] `PERF-001`: defer tuning until a normal shared QAD/MPS baseline and DBA-reviewed plan/index
      evidence are available. The initial cache-miss profile found QAD PO reads dominant; the
      reader-local 500-to-250 batch experiment was only modestly faster.

**Completion gate:** PASS. A scheduler can inspect qualifying open PO supply and read-only current
supplier/comment context for components in the active workspace. Stage 10 makes no coverage or
shortage-clearance claim.


## Stage 11 — Workspace Shortages

**Status: COMPLETE / ACCEPTED / LOCKED — 2026-09-29.** The authoritative algorithm is
`docs/implementation/stage11_component_mrp_algorithm.md`. The owner completed desktop validation
and accepted report accuracy, display/usability, cold-load performance, cached performance, and
overall readiness.

- [x] Acquire QAD facts and calculate daily component projections from the accepted Stage 11 algorithm.
- [x] Present Sunday–Saturday weeks with Monday labels, confirmed-only and include-unconfirmed receipt modes, purchased components, optional manufactured components, and Show All behavior.
- [x] Deliver filtering, sorting, severity classification/display, compact initial-screen contract, frozen columns, sticky headers, and horizontal scrolling.
- [x] Deliver snapshot-scoped component detail and shared Component Information drawer integration, including PO and buyer-comment presentation.
- [x] Deliver cached projection/export behavior and filtered workbook export.
- [x] Preserve locked Stage 9 and Stage 10 implementations and the legacy shortage report.
- [x] Complete automated verification: backend suite, focused Stage 11 frontend tests including sticky-header and snapshot-race regressions, typecheck, changed-file lint, production frontend build, OpenAPI/generated TypeScript synchronization, and sidecar rebuild.
- [x] Complete owner desktop validation and acceptance against the legacy KSTv1 shortage report.

**Acceptance performance evidence:** owner-observed cold-load medians were Shure **72 seconds in
KSTv1 versus 11 seconds in KSTv2**, Taco **54 versus 10**, and MSA/Neutronics **20 versus 6**.
Across nine uncontrolled real-world observations, the overall median was **53 versus 10 seconds**
and the average approximately **55.1 versus 11.7 seconds**. Server load affects these observations;
they are acceptance evidence of consistent material improvement, not controlled SQL benchmarks.

**Deferred future backlog, not incomplete Stage 11 scope:**

- [~] Individual Component MRP belongs to the future legacy-report section and is not implemented by this stage.
- [~] Retain the DBA evidence request for optional future investigation if production use reveals a material concurrency or reliability issue.
- [~] Reconsider row virtualization only after its keyboard-focus/accessibility failure is solved.
- [~] Minor report cosmetic refinements and fine-tuning.


## Stage 12 — Multi-Part Shortage Analysis — RETIRED / SUPERSEDED

Owner disposition — 2026-09-29: **RETIRED / SUPERSEDED**. Accepted Stage 11 Workspace Shortages
provides the multi-parent, component-centric workspace capability that motivated this stage. Do not
recreate the old Stage 12 selection-and-netting design or reinterpret it as permission to modify the
locked Stage 9–11 algorithms. Retained refinements belong in Stage 22; missing exports belong in
Stage 21.


## Stage 13 — Open Orders — PLANNED / RELEASE 1 REQUIRED

Stage 13 combines the former Customer Open Orders and General Open Orders scopes. It must support
both customer-focused date-change work and flexible cross-customer investigation without direct
company-database writes. Planning prompt:
`docs/prompts/STAGE_13_OPEN_ORDERS_PLANNING_PROMPT.md`.

### 13.1 Discovery and accepted-scope decisions

- [ ] Map sales-order number, customer PO, line, item/revision, ship-to, status, quantities, dates,
      on hand, and extended price.
- [ ] Confirm customer, site, part, status, date, and other search filters.
- [ ] Confirm required versus optional filters, default site behavior, result limits, sorting,
      selectable columns, column order, and saved-layout requirements.
- [ ] Confirm editable date fields and date-validation rules.
- [ ] Define QXtend mapping and distinguish the operational change file from analytical exports.
- [ ] Define the Open Order Report relationship with the Stage 18 standalone generator.

### 13.2 Backend and frontend

- [ ] Reuse or create the authoritative sales-order adapter.
- [ ] Define `OpenOrderSearchRequest`, `OpenOrderLine`, and `ProposedOrderChange` contracts.
- [ ] Create one coherent Open Orders service and endpoints for customer and cross-customer use.
- [ ] Add filter validation, safe result limits or pagination, and measured large-result behavior.
- [ ] Build the filterable, sortable Open Orders grid and customer-focused date-edit workflow.
- [ ] Highlight staged changes and provide explicit clearing/confirmation behavior.
- [ ] Create and validate the QXtend-compatible date-change file without direct database writes.
- [ ] Defer ordinary application-view export inventory and consistency to Stage 21.
- [ ] Owner acceptance.

Completion gate: A scheduler can search Open Orders across customers, focus a customer workflow,
stage and validate approved date changes, and generate the external QXtend-compatible change file.


## Stage 14 — Planning Workbook — PLANNED / RELEASE 1 REQUIRED

### 14.1 Field and rule discovery

- [ ] Map sales-order, forecast, and MPS quantities.
- [ ] Map unit price and unit cost; define SO value and MPS value.
- [ ] Define demand selection, estimated on hand, adjusted on hand, and adjustment grain.
- [ ] Define frozen-fence restrictions and validation rules.
- [ ] Define mass-update file mappings and distinguish them from Stage 21 analytical exports.

### 14.2 Backend

- [ ] Define `PlanningBucket` and `ProposedMpsAdjustment`.
- [ ] Create planning-data and inventory-projection services.
- [ ] Create price and cost adapters.
- [ ] Create adjustment staging and validation services.
- [ ] Create planning endpoints and the MPS mass-update exporter.

### 14.3 Frontend and validation

- [ ] Build the Planning Workbook grid and grouped part blocks.
- [ ] Build the editable adjustment row, staged-change highlighting, and clear confirmation.
- [ ] Display last export and validate negative inventory, frozen periods, and invalid adjustments.
- [ ] Validate the exported mass-update file.
- [ ] Owner acceptance.

Completion gate: A scheduler can review supply and demand, stage MPS adjustments, validate them,
and produce a QAD-compatible update file.


## Stage 15 — Finished Goods — PLANNED / RELEASE 1 REQUIRED

### 15.1 Field and rule discovery

- [ ] Define as-of date
- [ ] Map due orders
- [ ] Map due units
- [ ] Map finished-goods on hand
- [ ] Map location
- [ ] Map lot
- [ ] Define shipping locations
- [ ] Define hold locations
- [ ] Define RMA classification
- [ ] Define nettable status
- [ ] Define inventory value
- [ ] Define demand coverage
### 15.2 Backend and frontend

- [ ] Create finished-goods adapter
- [ ] Create lot adapter
- [ ] Create inventory-status adapter
- [ ] Define FinishedGoodsPosition
- [ ] Create coverage service
- [ ] Create Finished Goods endpoint
- [ ] Build summary cards
- [ ] Build location and lot grid
- [ ] Build date selector
- [ ] Provide Stage 21 with the Finished Goods export requirements and disposition.
- [ ] Validate nettable inventory
- [ ] Validate RMA exclusion
- [ ] Owner acceptance
Completion gate: A scheduler can determine whether available finished goods cover immediate
customer demand.


## Stage 16 — General Open Orders — ABSORBED INTO STAGE 13

Owner disposition — 2026-09-29: **ABSORBED**. Customer Open Orders and General Open Orders are
different entry points into the same capability. Their filters, search, results, staged date changes,
and external-file behavior are now planned together in Stage 13. This historical stage number is
retained so earlier references remain understandable.


## Stage 17 — General WO Variance — RETIRED / NO LONGER DESIRED

Owner disposition — 2026-09-29: **RETIRED / NO LONGER DESIRED**. Release 1 will not add a
standalone cross-customer work-order variance search or export. Accepted Stage 7 work-order and
variance behavior remains unchanged. Small filtering, navigation, or usability improvements may be
considered in Stage 22; a future standalone variance capability requires a new owner-approved use
case.


## Stage 18 — Standalone Excel Report Generators — PLANNED / RELEASE 1 REQUIRED

### 18.1 Shared report infrastructure

- [ ] Define report request pattern
- [ ] Define output-directory behavior
- [ ] Define filename conventions
- [ ] Define overwrite behavior
- [ ] Define workbook metadata
- [ ] Define progress reporting
- [ ] Define cancellation behavior
- [ ] Define error cleanup
- [ ] Define workbook validation tests
### 18.2 Component MRP

- [ ] Confirm the report's individual/single-component request scope and inputs.
- [ ] Inventory the legacy inputs, calculations, output columns, and formatting.
- [ ] Identify authoritative source data and accepted business rules.
- [ ] Implement workbook generation without changing locked Stage 9–11 algorithms.
- [ ] Compare with the legacy workbook and validate with stakeholders.
- [ ] Owner acceptance.

### 18.3 Open Order Report

- [ ] Confirm report inputs, filters, output columns, grouping, sorting, and formatting.
- [ ] Reuse accepted Stage 13 Open Orders data contracts and business rules.
- [ ] Implement standalone workbook generation.
- [ ] Compare with the legacy report and validate with stakeholders.
- [ ] Owner acceptance.

### 18.4 Shipments-To-Go

- [ ] Inventory current inputs
- [ ] Inventory current output columns
- [ ] Map every output field
- [ ] Extract business rules
- [ ] Reuse shared order and shipment services
- [ ] Implement workbook generation
- [ ] Compare with legacy workbook
- [ ] Validate with stakeholders
- [ ] Owner acceptance
### 18.5 S&OP

- [ ] Inventory current inputs
- [ ] Inventory current output columns
- [ ] Determine use of MPS procedure data
- [ ] Extract aggregation rules
- [ ] Implement workbook generation
- [ ] Compare with legacy workbook
- [ ] Validate monthly period behavior
- [ ] Owner acceptance
Completion gate: Component MRP, Open Order Report, Shipments-To-Go, and S&OP workbooks can be
generated and validated independently from KST v2. These report generators are distinct from the
application-view export inventory and conformance work in Stage 21.


## Stage 19 — Historical Shipments — PLANNED / RELEASE 1 REQUIRED

### 19.1 Requirements

- [ ] Confirm user questions
- [ ] Confirm retention horizon
- [ ] Confirm customer and site filters
- [ ] Confirm part filters
- [ ] Confirm date-range behavior
- [ ] Confirm order and PO fields
- [ ] Confirm shipment quantity
- [ ] Confirm revenue calculation
- [ ] Confirm returns and reversals
- [ ] Confirm corrections
- [ ] Define the required shipment report/export.
### 19.2 Backend and frontend

- [ ] Investigate tr_hist
- [ ] Identify authoritative shipment transactions
- [ ] Define ShipmentHistoryRow
- [ ] Create transaction normalization
- [ ] Create reversal handling
- [ ] Create shipment-history endpoint
- [ ] Build search and results UI
- [ ] Build drill-downs if needed
- [ ] Build the required shipment report/export.
- [ ] Validate historic totals
- [ ] Owner acceptance
Completion gate: A scheduler can review and report reliable historical shipment activity for a
selected site, customer, part, and date range. Historical Shipments is distinct from the
forward-looking Shipments-To-Go generator in Stage 18.


## Stage 20 — Legacy Simulation — RETIRED / NO LONGER DESIRED

Owner disposition — 2026-09-29: **RETIRED / NO LONGER DESIRED**. The old roadmap did not identify
the business question answered by the proposed KST v1 compatibility migration, and Release 1 does
not require it. Remove simulation compatibility from release-readiness criteria. Stage 18 carries
the required standalone reports. The future Can-Build concept remains separate in Stage 27 and does
not inherit this undefined legacy scope.


## Stage 21 — Cross-Cutting Export Completion

Status: **PLANNED / RELEASE 1 REQUIRED**. Stage 11 delivered and accepted its Workspace Shortages
export ahead of this cross-cutting stage. That export remains accepted functionality; Stage 21
includes it in the inventory and conformance review but does not rebuild it without a demonstrated
defect or inconsistency.

### 21.1 Export inventory and decisions

- [ ] Review every retained application area and identify whether it needs an export.
- [ ] Record required audience, format, columns, filters, selections, date scope, and result limits.
- [ ] Record an explicit no-export disposition where an export is not required.
- [ ] Distinguish analytical exports from QAD/QXtend operational update files.
- [ ] Reconcile completed Stage 11 export behavior and Stage 18 standalone generators with the
      system-wide inventory.

### 21.2 Shared export standards

- [ ] Define consistent filenames, destination handling, overwrite behavior, metadata, formatting,
      success feedback, and error cleanup.
- [ ] Define selected-row, selected-column, selected-part, selected-date, and filtered-result behavior
      where applicable.
- [ ] Define progress, cancellation, and safe large-result behavior.
- [ ] Confirm security, filesystem, dependency-admission, and licensing requirements.

### 21.3 Completion and validation

- [ ] Implement every approved missing application-view export.
- [ ] Validate exported data against the displayed and filtered source data.
- [ ] Validate representative and large datasets and applicable golden masters.
- [ ] Confirm retained operational update files independently of analytical exports.
- [ ] Owner acceptance of the complete export inventory and behavior.

Completion gate: Every retained application area has an explicit export disposition; all required
exports are implemented, consistent, validated, and owner-accepted.

## Stage 22 — Refinement & Optimization — PLANNED / RELEASE 1 REQUIRED

Stage 22 is an intentionally interactive, product-wide refinement stage. It consolidates observed
defects, owner feedback, measured performance work, usability improvements, and deferred Release 1
items that do not naturally belong to a retained feature stage.

Entry rule: an item may enter Stage 22 when it improves or repairs an existing Release 1 capability.
A substantial new standalone capability requires its own explicit roadmap disposition. Locked Stage
9–11 business behavior may change only through an explicit owner-approved amendment.

### 22.1 Backlog consolidation and prioritization

- [ ] Walk through every major workflow with the owner and schedulers.
- [ ] Consolidate known defects, deferred work, usability observations, and performance evidence.
- [ ] Classify each item as release-blocking, important, optional Release 1, trigger-deferred, or
      post-release.
- [ ] Preserve an explicit destination and trigger for work not completed in Stage 22.

### 22.2 Required known intake

- [ ] Repair the main MPS matrix horizontal scrolling: when the matrix exceeds the viewport, its
      horizontal scrollbar must allow access to every column at supported window sizes without
      breaking headers, selection, or layout.
- [ ] Revisit Stage 9 presentation, workflow, explanation, and measured performance without silently
      changing its accepted shortage algorithm.
- [ ] Reassess Stage 10 `PERF-001` using current measurements.
- [ ] Include accepted minor Stage 11 cosmetic refinements and fine-tuning.
- [ ] Resolve UI Navigation & Keyboard Ergonomics B items.
- [ ] Reconcile parent-part CSV import from the Stage 4 backlog.
- [ ] Review deferred Component Information additions: Inventory/Lot Locations, Show MRP, Extended
      Requirement, Incoming Supply, Coverage, and Material Status.
- [ ] Review PO-detail/drill-card behavior, previous/next navigation, and no-open-PO presentation.
- [ ] Keep Current Comments read-only unless a separately approved, security-reviewed write-capable
      notes design is authorized.

### 22.3 Interactive refinement cycles

- [ ] Work in bounded owner-review batches rather than one monolithic change set.
- [ ] Improve cross-view consistency, scrolling, sizing, navigation, keyboard behavior, filters,
      selection, loading states, empty states, errors, and explanations.
- [ ] Measure before optimizing startup, acquisition, queries, caching, rendering, and exports.
- [ ] Optimize only demonstrated bottlenecks; do not create database objects or indexes without the
      required DBA and security authority.
- [ ] Re-run relevant deterministic regression coverage after each accepted batch.

### 22.4 Trigger-deferred items retained through Stage 22

- [~] Stage 11 row virtualization — reconsider only after keyboard-focus and accessibility behavior
      is solved.
- [~] Stage 11 DBA investigation — trigger only if production use reveals a material concurrency or
      reliability problem.
- [~] Stage 9 `po_mstr.po_stat` predicate — do not invent one without accepted source evidence.
- [~] Write-capable buyer notes, local note persistence, or exported note updates — require an
      explicit workflow decision plus security and persistence review.

Completion gate: required defects and refinements are accepted, performance changes are supported by
measurements, retained regressions pass, every deferred item has a destination/trigger, and the
product baseline is frozen for Stage 23.


## Stage 23 — Cross-Cutting Quality and Hardening

### 23.1 Data integrity

- [ ] Verify domain filtering
- [ ] Verify site filtering
- [ ] Verify customer assignments
- [ ] Verify product-line filtering
- [ ] Verify planner filtering
- [ ] Verify date boundaries
- [ ] Verify numeric precision
- [ ] Verify null handling
- [ ] Verify duplicate handling
- [ ] Verify stale-data handling
- [ ] Verify partial-refresh handling
### 23.2 Performance verification

- [ ] Measure startup time
- [ ] Measure initial customer load
- [ ] Measure refresh time
- [ ] Measure drill-down time
- [ ] Measure 72-week MPS rendering
- [ ] Measure large Open Orders search
- [ ] Measure shortage analysis
- [ ] Measure export generation
- [ ] Confirm Stage 22 optimizations meet owner-accepted targets without changing accepted results.
- [ ] Record any residual performance limitation and its explicit release disposition.
### 23.3 Reliability

- [ ] Test QAD unavailable
- [ ] Test shortage DB unavailable
- [ ] Test Analysis DB unavailable
- [ ] Test one source failing during refresh
- [ ] Test backend crash recovery
- [ ] Test corrupted local settings
- [ ] Test interrupted export
- [ ] Test invalid destination
- [ ] Test low disk space
- [ ] Test application update compatibility
### 23.4 Security

- [ ] Verify read-only QAD access
- [ ] Verify read-only shortage access unless an exception is approved
- [ ] Reconcile the post-Stage-11 Tauri capability regression/documentation baseline for
      `dialog:allow-save` and `fs:allow-write-file`; preserve the no-`shell:*` boundary.
- [ ] Complete or reconcile dependency-admission and commercial-use licensing records for ClosedXML
      and the Tauri dialog/filesystem plugins.
- [ ] Complete the retrospective third-party software and license inventory required by governance.
- [ ] Prevent credentials in logs
- [ ] Protect local configuration
- [ ] Bind API only to the local machine
- [ ] Validate all file paths
- [ ] Sanitize filenames
- [ ] Validate all user-entered filters
- [ ] Validate staged update values
- [ ] Ensure no direct company-database writes exist
### 23.5 Accessibility and usability

- [ ] Keyboard navigation
- [ ] Visible focus state
- [ ] Color-independent status indicators
- [ ] Light and dark mode readability
- [ ] Compact and comfortable density
- [ ] Scaling on common Windows resolutions
- [ ] Horizontal-grid usability
- [ ] Loading feedback
- [ ] Clear empty states
- [ ] Clear stale-data warnings
- [ ] Clear export confirmation
- [ ] User testing with schedulers
### 23.6 Documentation

- [ ] Architecture overview
- [ ] Repository guide
- [ ] Developer setup
- [ ] Database-source catalog
- [ ] Business-rule catalog
- [ ] API documentation
- [ ] Cache and refresh documentation
- [ ] Export documentation
- [ ] Deployment guide
- [ ] Troubleshooting guide
- [ ] Scheduler user guide
- [ ] QAD-upgrade migration guide
- [ ] Architecture decision records

## Stage 24 — Release 1 Readiness

### 24.1 Functional readiness

- [ ] All required interactive phases complete
- [ ] Required exports complete
- [ ] Stage 19 Historical Shipments report complete and accepted.
- [ ] Customer/site configuration complete
- [ ] Staged update workflows complete
- [ ] No direct database writes
- [ ] All critical business rules approved
### 24.2 Validation readiness

- [ ] Golden-master comparisons complete
- [ ] Representative customer tests complete
- [ ] Representative site tests complete
- [ ] Scheduler walkthroughs complete
- [ ] Known intentional differences documented
- [ ] Open critical defects resolved
- [ ] Performance targets accepted
- [ ] Error behavior accepted
### 24.3 Packaging and deployment

- [ ] Build signed or approved Windows installer
- [ ] Resolve KST v1/KST v2 package identity and single-instance coexistence before side-by-side deployment.
- [ ] Package .NET sidecar
- [ ] Package runtime dependencies
- [ ] Verify the complete Windows installer/application-bundle SBOM.
- [ ] Configure installation directories
- [ ] Configure local settings migration
- [ ] Configure logging directories
- [ ] Configure update strategy
- [ ] Test clean installation
- [ ] Test upgrade installation
- [ ] Test uninstall
- [ ] Create deployment instructions
- [ ] Verify security requirements before activating any `keytronicshortage` integration.
### 24.4 Operational readiness

- [ ] Identify pilot users
- [ ] Identify support contacts
- [ ] Define defect-reporting process
- [ ] Define fallback to KST v1
- [ ] Define issue severity
- [ ] Define data-validation process
- [ ] Define training
- [ ] Define feedback collection
- [ ] Define release notes
- [ ] Resolve the CI/CD platform, organizational risk-acceptance authority, external-AI-provider
      policy, and release security thresholds when they become required for release operations.
- [ ] Approve pilot launch

## Stage 25 — Pilot

### 25.1 Initial-site pilot

- [ ] Deploy at primary site
- [ ] Keep KST v1 available
- [ ] Monitor data discrepancies
- [ ] Monitor refresh reliability
- [ ] Monitor export compatibility
- [ ] Monitor performance
- [ ] Capture scheduler feedback
- [ ] Capture missed fields and workflows
- [ ] Correct critical business rules
- [ ] Refine UI
- [ ] Refine diagnostics
### 25.2 Pilot exit criteria

- [ ] Core workflows used successfully
- [ ] Required reports accepted
- [ ] Mass-update files accepted
- [ ] No unresolved critical data errors
- [ ] No unresolved direct-write risk
- [ ] Refresh reliability accepted
- [ ] Performance accepted
- [ ] User acceptance received
- [ ] Support process functioning
- [ ] Project owner approves broader rollout

## Stage 26 — Incremental Multi-Site Rollout

- [ ] Select next site
- [ ] Gather site-specific configuration
- [ ] Validate customer assignments
- [ ] Validate planner mappings
- [ ] Validate product-line mappings
- [ ] Validate database access
- [ ] Validate reports
- [ ] Validate local operating practices
- [ ] Train users
- [ ] Deploy
- [ ] Monitor
- [ ] Repeat for each site
- [ ] Retire KST v1 only after approved transition

## Stage 27 — Post-Release Roadmap

### 27.1 QAD upgrade preparation

- [ ] Monitor upgrade timeline
- [ ] Obtain test-schema access
- [ ] Compare QAD table changes
- [ ] Update adapters
- [ ] Preserve domain models
- [ ] Preserve API contracts
- [ ] Run migration fixtures
- [ ] Validate exports
- [ ] Deploy compatibility update
### 27.2 Can-Build tool — FUTURE CONCEPT / NOT AUTHORIZED

- [ ] Preserve the owner concept: calculate how many units of a selected model can be built from
      available materials for a selected date.
- [ ] Include earliest material-ready state with Can-Build, not Workspace Shortages.
- [ ] Define authoritative inventory, demand, supply, BOM, date, and allocation inputs only after a
      separate owner-approved discovery stage is authorized.
- [ ] Define constraints, validation, comparisons, and explainability.
- [ ] Create a separate charter and implementation plan before implementation.

### 27.3 Other scenario-planning concepts — OWNER DECISION REQUIRED

- [ ] Gather scheduler requirements
- [ ] Define simulation questions
- [ ] Define scenario inputs
- [ ] Define authoritative source data
- [ ] Define constraints
- [ ] Define calculation model
- [ ] Define validation model
- [ ] Define comparison views
- [ ] Define saved scenarios
- [ ] Create separate charter and implementation plan
### 27.4 Potential future enhancements

- [ ] More historical analytics
- [ ] Additional configurable exports
- [ ] Additional cross-customer views
- [ ] Improved coverage and risk modeling
- [ ] Alternate-part analysis
- [ ] Expanded supplier-risk integration
- [ ] Additional local-first capabilities
- [ ] Write-capable buyer notes or local note persistence if a future owner-approved workflow and
      security model justify them.
- [ ] Site-requested enhancements

## Current Project Position

### Completed / accepted

- [x] Stage 1 — Project Charter.
- [x] Stage 2 — Broad legacy, UI, and dataset inventory.
- [x] Stage 3 — Technical Foundation.
- [x] Stage 4 / 4B — Application Shell, Workspace Configuration, and Workspace Scope Extension.
- [x] Stage 5 — MPS Data Foundation and Dashboard Implementation.
- [x] Stage 6 — Part Information Drill-Down.
- [x] Stage 7 — Work Orders and Kitting.
- [x] Stage 8 — Component and BOM Detail.
- [x] Stage 9 — Immediate Work-Order Shortages (**COMPLETE / ACCEPTED / LOCKED — 2026-09-08**).
- [x] Stage 10 — Purchase-Order Drill-Down / Component Orders (**COMPLETE / ACCEPTED / LOCKED —
      2026-09-14**).
- [x] Stage 11 — Workspace Shortages (**COMPLETE / ACCEPTED / LOCKED — 2026-09-29**).
- [x] UI Navigation & Keyboard Ergonomics A.

### Approved future roadmap

- [~] Stage 12 — Multi-Part Shortage Analysis — **RETIRED / SUPERSEDED**.
- [ ] Stage 13 — Open Orders — next planned product stage; implementation not yet authorized.
- [ ] Stage 14 — Planning Workbook.
- [ ] Stage 15 — Finished Goods.
- [~] Stage 16 — General Open Orders — **ABSORBED into Stage 13**.
- [~] Stage 17 — General WO Variance — **RETIRED / NO LONGER DESIRED**.
- [ ] Stage 18 — Standalone Excel Report Generators.
- [ ] Stage 19 — Historical Shipments.
- [~] Stage 20 — Legacy Simulation — **RETIRED / NO LONGER DESIRED**.
- [ ] Stage 21 — Cross-Cutting Export Completion.
- [ ] Stage 22 — Refinement & Optimization.
- [ ] Stage 23 — Quality and Hardening.
- [ ] Stage 24 — Release 1 Readiness.
- [ ] Stage 25 — Pilot.
- [ ] Stage 26 — Incremental Multi-Site Rollout.
- [~] Stage 27 — Post-Release Roadmap; future concepts are not implementation-authorized.

### Accepted cross-cutting foundations

- [x] R0 — Repository / Documentation Reconciliation.
- [x] S0 — Security Foundation Integration (**COMPLETE / ACCEPTED — 2026-08-31**; S0.8 — **COMPLETE / ACCEPTED — 2026-08-31**).
  - [x] S0.1 — Security Policy Injection (COMPLETE / ACCEPTED — 2026-08-21 — see `SECURITY.md`,
        `docs/security/`).
  - [x] S0.2 — Security Baseline Discovery (COMPLETE / ACCEPTED — 2026-08-24 — see
        `docs/security/SECURITY_BASELINE.md`).
  - [x] S0.3 — Existing-Tool Security Checks (COMPLETE / ACCEPTED — 2026-08-24 — see
        `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md`).
  - [x] S0.4 — Security Finding Disposition & Bounded Remediation (COMPLETE / ACCEPTED — 2026-08-25).
    - [x] S0.4A — QAD SQL Transport Correction (COMPLETE / ACCEPTED — 2026-08-25 — resolves
          `S0.2-F003` at the application-configuration level — see
          `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`).
    - [x] S0.4B — Tauri Shell Capability (COMPLETE / ACCEPTED — 2026-08-25 — resolves
          `S0.2-F001` — see `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`).
    - [x] S0.4C — npm Development-Tooling Advisories (COMPLETE / ACCEPTED — 2026-08-25 — resolves
          `S0.3-F001` — see `docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md`, accepted).
  - [x] S0.5 — Security Regression & Architecture Checks (COMPLETE / ACCEPTED — 2026-08-26 — see `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md`).
  - [x] S0.6 — Security Tool Admission (COMPLETE / ACCEPTED — 2026-08-27 — Capability Review 1: Rust Dependency
        Advisory Capability (`S0.3-G001`) — **COMPLETE / ACCEPTED — 2026-08-26** —
        cargo-audit 0.22.2 ADMITTED / ACCEPTED; cargo-deny 0.20.2 DEFERRED — see
        `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`; S0.3-G001 — Covered / Resolved;
        Capability Review 2: Dedicated Secret Scanning (`S0.3-G007`) — **COMPLETE / ACCEPTED —
        2026-08-27** (Gitleaks v8.30.0 installed, release-integrity and
        synthetic-canary verified, scanned current KST content (4 findings) and full Git history
        (8 findings), all rule `private-key`, confirmed documentation false positives;
        `S0.3-G007` — Covered / Resolved) — see
        `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md`;
        `docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`; Gitleaks v8.30.1, TruffleHog v3.97.1,
        detect-secrets v1.5.0 DEFERRED;
        Capability Review 3: Software Bill of Materials (`S0.3-G008`) — **COMPLETE /
        ACCEPTED — 2026-08-27** (Anchore Syft v1.51.1 installed,
        release-integrity verified, run against KST build/repository evidence (SPDX 2.3, 1,027
        packages; CycloneDX 1.6, 1,026 components) and a complementary packaged-artifact view
        (published `Kst.Api` sidecar, 37 NuGet packages recovered directly); six informational
        findings `S0.6-F014`–`S0.6-F019` recorded, none blocking; complete Tauri Windows
        installer/application bundle Unable to Verify / future packaged-release verification
        boundary, not Accepted Risk) — see
        `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md` (neutral research packet, not a
        recommendation or admission decision); `docs/security/S0_6_SBOM_ADMISSION.md` (owner
        decision and implementation evidence; Anchore Syft v1.51.1 — ADMITTED / IMPLEMENTED /
        ACCEPTED; Microsoft sbom-tool v4.1.5 and the CycloneDX
        ecosystem-native approach —
        cyclonedx-dotnet 6.2.0, cyclonedx-npm 6.0.1, cargo-cyclonedx 0.5.9 — DEFERRED);
        `S0.3-G008` — Covered / Resolved;
        Capability Review 4: Dedicated Static Application Security Testing (SAST) (`S0.3-G006`) —
        **COMPLETE / ACCEPTED — 2026-08-27** (owner reviewed the neutral research
        packet comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI
        v1.0.90 and admitted DevSkim CLI v1.0.90, which was installed, self-verified,
        synthetically validated (C#, JavaScript/TypeScript, Rust, SQL), and run against the KST
        source tree (50 findings across 3 bundled rules; `S0.6-F020` reviewed 2026-08-27 and
        reclassified to Informational / Framework-Local Origin / Confirmed DevSkim False
        Positive for plaintext-network interpretation; `S0.6-F021` — Informational / Known
        DevSkim Rule Limitation; neither Accepted Risk)) — see `docs/security/S0_6_SAST_ADMISSION_RESEARCH.md`
        (neutral research packet, not a recommendation or admission decision) and
        `docs/security/S0_6_SAST_ADMISSION.md` (owner decision, full implementation evidence, and
        2026-08-27 project-owner acceptance);
        Semgrep CE v1.175.0 and CodeQL CLI v2.26.4 DEFERRED, not rejected, pending organizational
        licensing/entitlement review; `S0.3-G006` — Covered / Resolved; Microsoft DevSkim CLI
        v1.0.90 — ADMITTED / INSTALLED / VERIFIED / ACCEPTED. All four S0.6-assigned gaps
        (`S0.3-G001`, `S0.3-G006`, `S0.3-G007`, `S0.3-G008`) are Covered / Resolved.
  - [x] Third-Party Software & Licensing Governance Foundation (**ENACTED / ACCEPTED — 2026-08-27** —
        see `docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md`; integrated into
        `docs/security/DEPENDENCY_ADMISSION.md` and `AGENTS.md` as a licensing/commercial-use
        admission gate supplementing, not replacing, existing security admission requirements; a
        governing prerequisite for future third-party dependency/tool admission, including the
        SAST candidate admission under S0.3-G006 (since resolved via Capability Review 4); not
        itself an S0 checkpoint or an admission decision for any specific component).
  - [x] S0.7 — Runtime & Infrastructure Verification (**COMPLETE / ACCEPTED — 2026-08-28** — S0.7A — Local Release Runtime Verification working pass — **COMPLETE / ACCEPTED — 2026-08-28** — see `docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md`; 2026-08-27 evidence valid/accepted as evidence; 2026-08-28 `S0.5-F001` loopback-binding remediation implemented + re-verified (sidecar always sets an explicit `http://127.0.0.1:<port>` `UseUrls` endpoint) — **`S0.5-F001` REMEDIATED AND VERIFIED BY S0.7**; **S0.3-G009 — Covered / Resolved** (accepted with S0.7A — 2026-08-28); **`S0.7-F001`** — Operational / Package-Identity Coexistence Issue — Deferred for packaging/deployment decision; S0.7B — **COMPLETE / ACCEPTED — 2026-08-28** (`S0.3-G010` — **Covered / Resolved — 2026-08-28**; **`S0.7-F002`** — **RETIRED** / Application-vs-Enterprise Identity Scope Model Corrected — 2026-08-28 owner scope decision; NOT Accepted Risk; NOT a waived vulnerability; NOT evidence deletion)).
  - [x] S0.8 — Independent Assurance & S0 Closeout (**COMPLETE / ACCEPTED — 2026-08-31** — S0 — **COMPLETE / ACCEPTED — 2026-08-31** — see `docs/security/S0_8_INDEPENDENT_ASSURANCE_CLOSEOUT.md` and `docs/security/KST_V2_SECURITY_IMPLEMENTATION_REPORT.md`).
- [x] Stage 9 — Immediate Work-Order Shortages (**COMPLETE / ACCEPTED / LOCKED — 2026-09-08**; technical, regression, and live-QAD evidence complete; `po_mstr.po_stat` remains a deferred non-blocking evidence item).

### Planning rule going forward

Before beginning each later feature phase:

- [ ] Review that section of the prototype as design evidence, not automatic requirements.
- [ ] Filter/reconcile the field inventory to the phase.
- [ ] Map fields currently known from authoritative source evidence.
- [ ] Add missing fields discovered during review.
- [ ] Confirm business rules with the project owner when evidence is ambiguous.
- [ ] Define the smallest sufficient backend contract.
- [ ] Implement and validate the complete vertical slice in bounded checkpoints.
- [ ] Update shared models only when implemented requirements justify extension.
- [ ] Preserve enacted architecture, security, source, and agent-instruction boundaries.

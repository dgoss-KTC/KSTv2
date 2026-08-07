# KST v2 Master Project Checklist

**Current project position:** Stage 4/4B is complete. Stage 5A — MPS Data Inventory and Data Strategy is complete and accepted. Stage 5B — MPS Dashboard Implementation is ready to begin.

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

The authoritative detailed Stage 5 checklist is also maintained as `KST_v2_Master_Project_Checklist_STAGE_5_REVISION.md`.

# Stage 5A — KST v2 Data Inventory and Data Strategy

**Status:** COMPLETE / ACCEPTED — Stage 5A owner acceptance received 2026-08-07; Stage 5B is ready to begin

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

**Stage 5A completion gate: PASS. Stage 5B may begin.**

---

# Stage 5B — MPS Dashboard Implementation

**Status:** READY TO BEGIN — Stage 5A accepted 2026-08-07

## Purpose

Implement the MPS dashboard vertical slice using the accepted Stage 5A data/query/snapshot contracts.

---

## 5B.1 QAD Database Integration

- [ ] Implement the approved direct parameterized MPS query in `Kst.Integrations.Qad`.
- [ ] Use approved SQL Server client/database-access mechanism.
- [ ] Implement Windows-integrated QAD connectivity.
- [ ] Apply resolved workspace site/domain/parent-part scope.
- [ ] Implement parameterized part-list batching/chunking.
- [ ] Apply `mrp_dataset = 'wo_mstr'`.
- [ ] Apply `mrp_type IN ('supply','supplyf','supplyp')`.
- [ ] Apply safe WO join on domain + site + part + WO number + WO ID.
- [ ] Exclude `wo_status = 'C'`.
- [ ] Exclude `wo_bom_code = 'RMABOM'`.
- [ ] Retrieve all historical qualifying unfinished work needed for Falldown.
- [ ] Retrieve future source facts sufficient for the maximum 72-week Due/Release views.
- [ ] Support cancellation and approved command timeout.
- [ ] Log execution diagnostics without leaking sensitive information.

Do not implement `sp_QAD_ktmpswkm` or create a new database procedure for the initial MPS.

---

## 5B.2 MPS Source Normalization

- [ ] Map QAD query rows into integration records.
- [ ] Normalize into `MpsSourceRow`.
- [ ] Normalize SUPPLY/SUPPLYF/SUPPLYP.
- [ ] Normalize A/F/R/P/e WO states.
- [ ] Handle unexpected non-C WO state defensively.
- [ ] Preserve both Due Date and Release Date.
- [ ] Preserve WO ID/status references.
- [ ] Preserve site/domain diagnostics where required.
- [ ] Do not introduce source deduplication unless new evidence requires it.

---

## 5B.3 Week Bucketing / Falldown

- [ ] Implement Sunday-Saturday business-week boundary.
- [ ] Use Monday as visible week label.
- [ ] Implement weekly buckets.
- [ ] Implement due-date-based Falldown with no historical lower cutoff.
- [ ] Implement maximum 72-week horizon.
- [ ] Rebuild Due/Release bucket views from the current source snapshot without QAD re-query.
- [ ] Test Sunday/Saturday boundaries and year transitions.

Fiscal period/quarter/year mapping is **not backend work**.

---

## 5B.4 MPS Status Classification

- [ ] Implement Allocating (`A`).
- [ ] Implement Frozen (`F`).
- [ ] Implement Released (`R`).
- [ ] Implement Mixed for 2+ distinct A/F/R states.
- [ ] Implement `ContainsPlannedWork` from `P`.
- [ ] Implement `ContainsExplicitlyScheduledWork` from `e`.
- [ ] Implement None when no A/F/R state exists.
- [ ] Aggregate quantities across all included WOs in a bucket.
- [ ] Add unit tests for mixed P/e/A/F/R combinations.

Shortage status is deferred to the later shortages capability and is not an initial MPS execution state.

---

## 5B.5 Snapshot Integration and Refresh

- [ ] Start MPS load automatically when a workspace opens.
- [ ] Keep workspace shell usable while MPS loads.
- [ ] Keep explicit parent rows visible with no MPS activity.
- [ ] Populate snapshot ID / timestamps / source state per Stage 5A contract.
- [ ] Preserve old snapshot while refresh runs.
- [ ] Replace snapshot atomically after successful load.
- [ ] Preserve prior snapshot on refresh failure.
- [ ] Show last successful refresh time.
- [ ] Implement approved initial database-unavailable message and Retry.
- [ ] Prevent concurrent refreshes for one workspace.
- [ ] Avoid QAD reload on tab switching, Due/Release toggle, fiscal display changes, or horizon changes ≤72 weeks.
- [ ] Do not persist MPS snapshot across application sessions initially.

---

## 5B.6 MPS API

- [ ] Define workspace MPS endpoint(s) from the accepted snapshot model.
- [ ] Return parent schedules and normalized buckets.
- [ ] Return MPS semantic status fields.
- [ ] Return snapshot/refresh metadata.
- [ ] Support Due/Release and horizon view requests without forcing QAD re-query when snapshot coverage is sufficient.
- [ ] Do **not** return fiscal year/period/quarter metadata from backend solely for display.
- [ ] Update OpenAPI.
- [ ] Regenerate TypeScript contracts.

---

## 5B.7 Frontend Fiscal Calendar / Settings

- [ ] Add Fiscal Calendar section to Settings.
- [ ] Seed FY26 anchor: June 29, 2025.
- [ ] Implement standard 4-4-5 generation.
- [ ] Implement 53-week exception records with selected extra-week period.
- [ ] Validate exception uniqueness and period range.
- [ ] Generate fiscal year/week/period/quarter display metadata in frontend.
- [ ] Test 52/53-week transitions and 72-week horizon coverage.

---

## 5B.8 Frontend MPS Grid

- [ ] Implement MPS grid shell.
- [ ] Implement sticky parent-part/description column.
- [ ] Implement horizontal scrolling.
- [ ] Implement week headers.
- [ ] Implement fiscal period bands.
- [ ] Implement fiscal quarter bands.
- [ ] Implement schedule quantities.
- [ ] Implement A/F/R/Mixed box presentation.
- [ ] Implement accessible Planned font treatment.
- [ ] Implement explicitly-scheduled non-color marker.
- [ ] Implement horizon selector up to 72 weeks.
- [ ] Implement Due/Release mode.
- [ ] Implement loading, empty, unavailable, stale/refresh, and retry states.
- [ ] Implement row/week-cell selection only to the extent required by the initial dashboard slice.

---

## 5B.9 Data Validation

- [ ] Compare KST v2 source rows to direct database results.
- [ ] Compare schedule totals to source evidence / legacy output where applicable.
- [ ] Validate representative sites.
- [ ] Validate product-line-derived scope.
- [ ] Validate explicit-part scope.
- [ ] Validate parent with no MPS rows.
- [ ] Validate one-WO and multi-WO buckets.
- [ ] Validate A/F/R/Mixed/P/e classification.
- [ ] Validate Falldown including an old unfinished WO.
- [ ] Validate `RMABOM` exclusion.
- [ ] Validate repetitive-schedule change after MRP/QADPRO2 sync.
- [ ] Validate empty results.
- [ ] Validate large-workspace performance / batching.
- [ ] Record discrepancies and resolutions.

---

## 5B.10 Automated Verification

- [ ] QAD adapter tests.
- [ ] Normalization tests.
- [ ] Status-rule tests.
- [ ] Business-week/Falldown tests.
- [ ] Frontend fiscal-calendar tests.
- [ ] Snapshot tests.
- [ ] API integration tests.
- [ ] Frontend component tests.
- [ ] Refresh/error-state tests.
- [ ] Architecture-boundary tests.
- [ ] Full backend build/test.
- [ ] Full frontend lint/typecheck/test/build.
- [ ] Rust/Tauri verification.
- [ ] Sidecar rebuild.
- [ ] Live Tauri manual verification.

---

## 5B.11 Documentation

- [ ] Document final direct-query contract.
- [ ] Document query parameters and batching behavior.
- [ ] Document result/source-row columns.
- [ ] Document normalization/status rules.
- [ ] Document fiscal settings/calculation behavior.
- [ ] Document snapshot/refresh behavior.
- [ ] Document MRP freshness dependency and RMA exclusion.
- [ ] Update project status.
- [ ] Update Master Project Checklist.
- [ ] Update API documentation.
- [ ] Update data inventory with implementation-confirmed mappings.

---

## 5B.12 Stage 5B Completion Gate

Stage 5B is complete only when:

- [ ] A configured workspace loads real MPS data from the approved direct QAD source.
- [ ] Workspace site/part scope is validated.
- [ ] Schedule quantities are validated.
- [ ] Work-order associations are validated.
- [ ] MPS semantic classification is validated.
- [ ] Falldown and RMA exclusion are validated.
- [ ] Refresh/snapshot behavior is validated.
- [ ] Due/Release and horizon changes reuse the current snapshot appropriately.
- [ ] Frontend fiscal bands are validated.
- [ ] The real MPS grid is usable.
- [ ] No fake production data remains.
- [ ] Loading/error/empty/refresh states work.
- [ ] Automated verification passes.
- [ ] Representative data matches source evidence.
- [ ] Owner acceptance passes.

**Completion gate:** A scheduler can open a configured workspace and use a validated, cached, real-data MPS grid for schedule review.


## Stage 6 — Phase 3: Part Information Drill-Down

### 6.1 UI and fields

- [ ] Confirm the Part Info tab
- [ ] Map revision
- [ ] Map planner
- [ ] Map lead time
- [ ] Map UOM
- [ ] Map item class
- [ ] Map description
- [ ] Map component count
- [ ] Map on-hand finished goods
- [ ] Map WIP
- [ ] Map safety stock
- [ ] Map part-level schedule status
### 6.2 Backend

- [ ] Create part-master adapter
- [ ] Create site-planning-parameter adapter
- [ ] Define effective planner fallback
- [ ] Define effective lead-time fallback
- [ ] Define inventory summary
- [ ] Define WIP calculation
- [ ] Define component count
- [ ] Create PartDetail
- [ ] Create part-detail endpoint
- [ ] Cache stable part information where appropriate
### 6.3 Frontend and validation

- [ ] Build Part Info panel
- [ ] Build loading state
- [ ] Build missing-part state
- [ ] Build partial-data warnings
- [ ] Validate against QAD
- [ ] Validate fallback behavior
- [ ] Owner acceptance
Phase 3 completion gate: Selecting an MPS part displays validated part attributes and inventory summaries.


## Stage 7 — Phase 4: Work Orders and Kitting

### 7.1 Field and rule discovery

- [ ] Map work-order number
- [ ] Map ordered quantity
- [ ] Map completed quantity
- [ ] Map open quantity
- [ ] Map status
- [ ] Map start date
- [ ] Map due date
- [ ] Map production line
- [ ] Identify allocation fields
- [ ] Define kitting percentage
- [ ] Map component requirements
- [ ] Map issued quantities
- [ ] Define variance quantity
- [ ] Define variance percentage
- [ ] Confirm severity thresholds
### 7.2 Backend

- [ ] Create work-order adapter
- [ ] Create WO-material adapter
- [ ] Define WorkOrderSummary
- [ ] Define WorkOrderMaterialLine
- [ ] Create work-order service
- [ ] Create kitting service
- [ ] Create variance service
- [ ] Join work orders to schedule buckets
- [ ] Add work-order summaries to cached MPS data or lazy detail
- [ ] Create work-order endpoints
### 7.3 Frontend and validation

- [ ] Build work-order cards
- [ ] Build selected-week filtering
- [ ] Build all-open-WO view
- [ ] Build kitting expansion
- [ ] Build variance sorting
- [ ] Build no-WO state
- [ ] Compare against WO Variance report
- [ ] Validate partial issue
- [ ] Validate over-issue
- [ ] Validate completed work orders
- [ ] Owner acceptance
Phase 4 completion gate: A scheduler can trace an MPS bucket to its work orders and component issue status.


## Stage 8 — Phase 5: Component and BOM Detail

### 8.1 Field and rule discovery

- [ ] Map component part
- [ ] Map component description
- [ ] Map quantity per
- [ ] Define extended requirement
- [ ] Map component on hand
- [ ] Map incoming supply
- [ ] Define coverage percentage
- [ ] Define material status
- [ ] Confirm BOM revision and effective-date behavior
- [ ] Confirm multi-level BOM expectations
- [ ] Confirm phantom and substitute behavior
### 8.2 Backend

- [ ] Create BOM adapter
- [ ] Create BOM-explosion service
- [ ] Define ComponentRequirement
- [ ] Define required quantity grain
- [ ] Add inventory availability service
- [ ] Add component supply summary
- [ ] Create component endpoint
- [ ] Decide when pre-exploded BOM storage is justified
- [ ] Add BOM tests
### 8.3 Frontend and validation

- [ ] Build Components tab
- [ ] Build component selection
- [ ] Build coverage display
- [ ] Build no-components state
- [ ] Compare against Component MRP
- [ ] Validate multi-level quantities
- [ ] Validate duplicate components
- [ ] Owner acceptance
Phase 5 completion gate: A scheduler can inspect the material structure and coverage behind a scheduled parent part.


## Stage 9 — Phase 6: Immediate Shortages

### 9.1 Rule definition

- [ ] Confirm immediate window length
- [ ] Define required quantity
- [ ] Define available quantity
- [ ] Define nettable inventory statuses
- [ ] Define shortage quantity
- [ ] Define On Hand status
- [ ] Define Due This Week status
- [ ] Define Short status
- [ ] Define work-order association
- [ ] Define receipt timing assumptions
- [ ] Define inventory allocation assumptions
- [ ] Define treatment of shared inventory
### 9.2 Backend

- [ ] Define ImmediateShortage
- [ ] Create immediate-requirement service
- [ ] Create inventory-netting service
- [ ] Create immediate-PO-coverage service
- [ ] Create shortage-classification service
- [ ] Add shortage counts to MPS buckets
- [ ] Create immediate-shortage endpoint
- [ ] Add stale-source warnings
### 9.3 Frontend and validation

- [ ] Build Shortages tab
- [ ] Sort components by severity
- [ ] Build status indicators
- [ ] Build component selection
- [ ] Build no-immediate-WO state
- [ ] Compare with existing Shortage Report
- [ ] Validate receipt boundary
- [ ] Validate insufficient incoming supply
- [ ] Validate fully covered requirements
- [ ] Owner acceptance
Phase 6 completion gate: A scheduler can identify immediate component shortages affecting near-term work orders.


## Stage 10 — Phase 7: Purchase-Order Drill-Down

### 10.1 Field discovery

- [ ] Map PO number
- [ ] Map vendor
- [ ] Map ordered quantity
- [ ] Map open quantity
- [ ] Map due date
- [ ] Map confirmed or scheduled status
- [ ] Map buyer
- [ ] Define PO coverage
- [ ] Map shortage comment
- [ ] Map supplier credit-hold flag
- [ ] Map CIA flag
- [ ] Confirm multiple-PO ordering
### 10.2 Buyer-note decision

- [ ] Confirm authoritative note source
- [ ] Determine whether KST may update ShortageMaster
- [ ] If writes are prohibited, define read-only behavior
- [ ] Evaluate local-only note storage
- [ ] Evaluate export-based note updates
- [ ] Define conflict and refresh behavior
- [ ] Document final persistence decision
### 10.3 Backend and frontend

- [ ] Create PO adapter
- [ ] Create vendor adapter
- [ ] Create supplier-risk adapter
- [ ] Create note adapter
- [ ] Define ComponentPurchaseOrder
- [ ] Create PO-coverage service
- [ ] Create PO-detail endpoint
- [ ] Build Component PO Drill card
- [ ] Build previous/next PO navigation
- [ ] Build buyer-note interaction
- [ ] Build no-open-PO state
- [ ] Validate with current shortage output
- [ ] Owner acceptance
Phase 7 completion gate: A scheduler can trace a component shortage to its open purchase orders, vendor, coverage, and current buyer information.


## Stage 11 — Phase 8: Future Shortages and Component MRP

### 11.1 Rule discovery

- [ ] Define projection horizon
- [ ] Define lead-time horizon
- [ ] Define projected balance
- [ ] Define planned-order handling
- [ ] Define covering PO
- [ ] Define projected clear week
- [ ] Define coverage gap
- [ ] Define future-shortage quantity
- [ ] Confirm behavior when no WO exists
- [ ] Confirm forecast treatment
### 11.2 Backend

- [ ] Define ProjectedShortage
- [ ] Create time-phased Component MRP service
- [ ] Create projected-balance service
- [ ] Create future-shortage service
- [ ] Reuse component and PO services
- [ ] Create future-shortage endpoint
- [ ] Create Component MRP endpoint
- [ ] Define Component MRP export dataset
### 11.3 Frontend, export, and validation

- [ ] Build Future Shortages tab
- [ ] Build projection descriptions
- [ ] Build no-future-shortage state
- [ ] Build Component MRP export options
- [ ] Support selected parent parts
- [ ] Support selected components
- [ ] Support date horizon
- [ ] Support selectable columns
- [ ] Compare with existing Component MRP
- [ ] Owner acceptance
Phase 8 completion gate: A scheduler can see future material exposure and export a scoped Component MRP report.


## Stage 12 — Phase 9: Multi-Part Shortage Analysis

### 12.1 Selection behavior

- [ ] Confirm Multi mode
- [ ] Confirm row checkbox behavior
- [ ] Confirm one-part WO-centric view
- [ ] Confirm multi-part component-centric view
- [ ] Confirm affected-parent display
- [ ] Confirm selection clearing
- [ ] Confirm export scope
### 12.2 Rules and backend

- [ ] Define shared-component aggregation
- [ ] Define inventory netting across selected parents
- [ ] Prevent duplicate inventory multiplication
- [ ] Define work-order-specific shortage grain
- [ ] Define component-centric shortage grain
- [ ] Define affected-parent relationships
- [ ] Create selection-analysis endpoint
- [ ] Create shortage export request
- [ ] Create configurable shortage export dataset
### 12.3 Frontend and validation

- [ ] Build Multi selection
- [ ] Build WO-centric table
- [ ] Build part-centric table
- [ ] Build shared-component pills
- [ ] Build export dialog
- [ ] Support selected columns
- [ ] Support selected parts and WOs
- [ ] Validate shared inventory
- [ ] Compare exported results with current Shortage Report
- [ ] Owner acceptance
Phase 9 completion gate: A scheduler can analyze and export shortages for one or several selected MPS parent parts.


## Stage 13 — Phase 10: Planning Workbook

### 13.1 Field and rule discovery

- [ ] Map sales-order quantities
- [ ] Map forecast quantities
- [ ] Map MPS quantities
- [ ] Map unit price
- [ ] Map unit cost
- [ ] Define SO value
- [ ] Define MPS value
- [ ] Define demand selection
- [ ] Define estimated on-hand
- [ ] Define adjusted on-hand
- [ ] Define adjustment grain
- [ ] Define frozen-fence restrictions
- [ ] Define validation rules
- [ ] Define export mappings
### 13.2 Backend

- [ ] Define PlanningBucket
- [ ] Define ProposedMpsAdjustment
- [ ] Create planning-data service
- [ ] Create inventory-projection service
- [ ] Create price and cost adapters
- [ ] Create adjustment staging service
- [ ] Create validation service
- [ ] Create planning endpoints
- [ ] Create MPS mass-update exporter
### 13.3 Frontend and validation

- [ ] Build Planning Workbook grid
- [ ] Build grouped part blocks
- [ ] Build editable adjustment row
- [ ] Highlight staged changes
- [ ] Build clear confirmation
- [ ] Build export behavior
- [ ] Display last export
- [ ] Test negative inventory
- [ ] Test frozen periods
- [ ] Test invalid adjustments
- [ ] Validate exported mass update
- [ ] Owner acceptance
Phase 10 completion gate: A scheduler can review supply and demand, stage MPS adjustments, validate them, and produce a QAD-compatible update file.


## Stage 14 — Phase 11: Customer Open Orders

### 14.1 Field inventory and rules

- [ ] Map sales-order number
- [ ] Map customer PO
- [ ] Map line
- [ ] Map item and revision
- [ ] Map ship date
- [ ] Map perform date
- [ ] Map required date
- [ ] Map dock date
- [ ] Map on hand
- [ ] Map extended price
- [ ] Map ship-to
- [ ] Map order status
- [ ] Confirm editable date fields
- [ ] Define date validation
- [ ] Define QXtend mapping
### 14.2 Backend

- [ ] Create sales-order adapter
- [ ] Define OpenOrderLine
- [ ] Define ProposedOrderChange
- [ ] Create customer Open Orders service
- [ ] Create order-change validation
- [ ] Create Open Orders endpoints
- [ ] Create QXtend-compatible exporter
### 14.3 Frontend and validation

- [ ] Build customer order grid
- [ ] Build editable date cells
- [ ] Highlight staged changes
- [ ] Build clear confirmation
- [ ] Build export behavior
- [ ] Display change count
- [ ] Validate representative orders
- [ ] Validate output file with QXtend requirements
- [ ] Owner acceptance
Phase 11 completion gate: A scheduler can inspect customer orders and generate validated date-change files without direct database writes.


## Stage 15 — Phase 12: Finished Goods

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
- [ ] Build export if retained
- [ ] Validate nettable inventory
- [ ] Validate RMA exclusion
- [ ] Owner acceptance
Phase 12 completion gate: A scheduler can determine whether available finished goods cover immediate customer demand.


## Stage 16 — Phase 13: General Open Orders

### 16.1 Search design

- [ ] Confirm all filters
- [ ] Confirm required versus optional filters
- [ ] Confirm default site behavior
- [ ] Confirm result limits
- [ ] Confirm sorting behavior
- [ ] Confirm selectable columns
- [ ] Confirm column order
- [ ] Confirm saved layouts
- [ ] Confirm export behavior
### 16.2 Backend and frontend

- [ ] Define OpenOrderSearchRequest
- [ ] Define OpenOrderSearchRow
- [ ] Create cross-customer search service
- [ ] Add filter validation
- [ ] Add pagination or safe result limits
- [ ] Create search endpoint
- [ ] Create configurable export
- [ ] Build filter bar
- [ ] Build column builder
- [ ] Build sortable grid
- [ ] Build saved layouts
- [ ] Validate large result sets
- [ ] Owner acceptance
Phase 13 completion gate: A scheduler can perform flexible cross-customer Open Orders searches and exports.


## Stage 17 — Phase 14: General WO Variance

### 17.1 Rules and backend

- [ ] Confirm IOS-code filter
- [ ] Confirm included WO statuses
- [ ] Confirm component inclusion
- [ ] Confirm variance thresholds
- [ ] Confirm negative-variance treatment
- [ ] Define WorkOrderVarianceRow
- [ ] Create cross-customer variance service
- [ ] Create search endpoint
- [ ] Decide whether export remains required
### 17.2 Frontend and validation

- [ ] Build IOS selector
- [ ] Build sortable variance grid
- [ ] Build severity highlighting
- [ ] Build empty state
- [ ] Build export if retained
- [ ] Compare with current WO Variance report
- [ ] Owner acceptance
Phase 14 completion gate: A scheduler can independently investigate work-order material variance by IOS or equivalent scope.


## Stage 18 — Phase 15: Standalone Excel Reports

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
### 18.2 Shipments-To-Go

- [ ] Inventory current inputs
- [ ] Inventory current output columns
- [ ] Map every output field
- [ ] Extract business rules
- [ ] Reuse shared order and shipment services
- [ ] Implement workbook generation
- [ ] Compare with legacy workbook
- [ ] Validate with stakeholders
- [ ] Owner acceptance
### 18.3 S&OP

- [ ] Inventory current inputs
- [ ] Inventory current output columns
- [ ] Determine use of MPS procedure data
- [ ] Extract aggregation rules
- [ ] Implement workbook generation
- [ ] Compare with legacy workbook
- [ ] Validate monthly period behavior
- [ ] Owner acceptance
Phase 15 completion gate: Required Shipments-To-Go and S&OP workbooks can be generated and validated from KST v2.


## Stage 19 — Phase 16: Historical Shipments

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
- [ ] Decide export requirements
### 19.2 Backend and frontend

- [ ] Investigate tr_hist
- [ ] Identify authoritative shipment transactions
- [ ] Define ShipmentHistoryRow
- [ ] Create transaction normalization
- [ ] Create reversal handling
- [ ] Create shipment-history endpoint
- [ ] Build search and results UI
- [ ] Build drill-downs if needed
- [ ] Build export if approved
- [ ] Validate historic totals
- [ ] Owner acceptance
Phase 16 completion gate: A scheduler can review reliable historical shipment activity for a selected site, customer, part, and date range.


## Stage 20 — Phase 17: Legacy Simulation

### 20.1 Compatibility inventory

- [ ] Document current input format
- [ ] Document current calculation process
- [ ] Document current output
- [ ] Identify external file dependencies
- [ ] Identify PO data requirements
- [ ] Identify configuration requirements
- [ ] Identify known limitations
### 20.2 Migration

- [ ] Move existing logic behind the v2 backend
- [ ] Preserve existing inputs
- [ ] Preserve existing outputs
- [ ] Add regression fixtures
- [ ] Build minimal v2 UI integration
- [ ] Add errors and progress reporting
- [ ] Validate against KST v1
- [ ] Owner acceptance
### 20.3 Deferred redesign

- [ ] Record advanced simulation as future scope
- [ ] Create future-requirements placeholder
- [ ] Avoid designing the advanced simulation engine during Release 1
- [ ] Avoid allowing legacy architecture to constrain future simulation design
Phase 17 completion gate: Existing Simulation functionality is available without expanding Release 1 scope.


## Stage 21 — Cross-Cutting Export Completion

Some export work occurs inside feature phases, but this stage verifies the export system as a whole.

- [ ] MPS configurable Excel export
- [ ] Component MRP configurable Excel export
- [ ] Shortage configurable Excel export
- [ ] Open Orders export
- [ ] Finished Goods export if retained
- [ ] WO Variance export if retained
- [ ] MPS mass-update CSV
- [ ] Sales-order mass-update CSV
- [ ] Shipments-To-Go workbook
- [ ] S&OP workbook
- [ ] Historical Shipments export if approved
- [ ] Consistent filenames
- [ ] Consistent destination handling
- [ ] Consistent error handling
- [ ] Selected-column support
- [ ] Selected-part support
- [ ] Selected-date support
- [ ] Workbook formatting standards
- [ ] Export audit metadata
- [ ] Golden-master validation

## Stage 22 — Cross-Cutting Quality and Hardening

### 22.1 Data integrity

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
### 22.2 Performance

- [ ] Measure startup time
- [ ] Measure initial customer load
- [ ] Measure refresh time
- [ ] Measure drill-down time
- [ ] Measure 72-week MPS rendering
- [ ] Measure large Open Orders search
- [ ] Measure shortage analysis
- [ ] Measure export generation
- [ ] Add indexes or query changes where allowed
- [ ] Add in-memory caching where measured
- [ ] Reconsider persistent cache only if justified
- [ ] Reconsider pre-exploded BOM only if justified
### 22.3 Reliability

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
### 22.4 Security

- [ ] Verify read-only QAD access
- [ ] Verify read-only shortage access unless an exception is approved
- [ ] Prevent credentials in logs
- [ ] Protect local configuration
- [ ] Bind API only to the local machine
- [ ] Validate all file paths
- [ ] Sanitize filenames
- [ ] Validate all user-entered filters
- [ ] Validate staged update values
- [ ] Ensure no direct company-database writes exist
### 22.5 Accessibility and usability

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
### 22.6 Documentation

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

## Stage 23 — Release 1 Readiness

### 23.1 Functional readiness

- [ ] All required interactive phases complete
- [ ] Required exports complete
- [ ] Simulation compatibility complete
- [ ] Historical Shipments disposition confirmed
- [ ] Customer/site configuration complete
- [ ] Staged update workflows complete
- [ ] No direct database writes
- [ ] All critical business rules approved
### 23.2 Validation readiness

- [ ] Golden-master comparisons complete
- [ ] Representative customer tests complete
- [ ] Representative site tests complete
- [ ] Scheduler walkthroughs complete
- [ ] Known intentional differences documented
- [ ] Open critical defects resolved
- [ ] Performance targets accepted
- [ ] Error behavior accepted
### 23.3 Packaging and deployment

- [ ] Build signed or approved Windows installer
- [ ] Package .NET sidecar
- [ ] Package runtime dependencies
- [ ] Configure installation directories
- [ ] Configure local settings migration
- [ ] Configure logging directories
- [ ] Configure update strategy
- [ ] Test clean installation
- [ ] Test upgrade installation
- [ ] Test uninstall
- [ ] Create deployment instructions
### 23.4 Operational readiness

- [ ] Identify pilot users
- [ ] Identify support contacts
- [ ] Define defect-reporting process
- [ ] Define fallback to KST v1
- [ ] Define issue severity
- [ ] Define data-validation process
- [ ] Define training
- [ ] Define feedback collection
- [ ] Define release notes
- [ ] Approve pilot launch

## Stage 24 — Pilot

### 24.1 Initial-site pilot

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
### 24.2 Pilot exit criteria

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

## Stage 25 — Incremental Multi-Site Rollout

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

## Stage 26 — Post-Release Roadmap

### 26.1 QAD upgrade preparation

- [ ] Monitor upgrade timeline
- [ ] Obtain test-schema access
- [ ] Compare QAD table changes
- [ ] Update adapters
- [ ] Preserve domain models
- [ ] Preserve API contracts
- [ ] Run migration fixtures
- [ ] Validate exports
- [ ] Deploy compatibility update
### 26.2 Advanced Simulation

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
### 26.3 Potential future enhancements

- [ ] More historical analytics
- [ ] Additional configurable exports
- [ ] Additional cross-customer views
- [ ] Improved coverage and risk modeling
- [ ] Alternate-part analysis
- [ ] Expanded supplier-risk integration
- [ ] Additional local-first capabilities
- [ ] Site-requested enhancements

## Current Project Position

### Completed

- [x] Stage 1 — Project Charter.
- [x] Stage 2 — Broad legacy, UI, and dataset inventory.
- [x] Stage 3 — Technical Foundation.
- [x] Stage 4 / 4B — Application Shell, Workspace Configuration, and Workspace Scope Extension.
- [x] Stage 5A technical/data artifacts, documentation reconciliation, and Stage 5B implementation plan.

### Current focus

- [x] Final project-owner acceptance of Stage 5A.
- [x] After acceptance, generate the Stage 5B VS Code/Copilot implementation prompt.
- [ ] Implement Stage 5B — MPS Dashboard in controlled checkpoints.

### Planning rule going forward

Before beginning each later phase:

- [ ] Review that section of the prototype.
- [ ] Filter the field inventory to that phase.
- [ ] Map the fields currently known.
- [ ] Add missing fields discovered during review.
- [ ] Confirm business rules.
- [ ] Define the smallest sufficient backend contract.
- [ ] Implement and validate the complete vertical slice.
- [ ] Update the shared models only when the implemented phase proves that an extension is needed.

# KST v2 Master Project Checklist — Stage 5 Revision

## Stage 5 — MPS Data Foundation and Dashboard Implementation

Stage 5 is divided into two controlled sub-stages:

- **Stage 5A — KST v2 Data Inventory and Data Strategy**
- **Stage 5B — MPS Dashboard Implementation**

Stage 5B must not begin until Stage 5A is complete and accepted.

---

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

**Status:** VERIFICATION COMPLETE — checkpoints 5B.1–5B.9 complete; 5B.9 owner-accepted 2026-08-10; 5B.10 automated/manual verification passed 2026-08-10, owner acceptance pending

## Purpose

Implement the MPS dashboard vertical slice using the accepted Stage 5A data/query/snapshot contracts.

Stage 5B.9 validation evidence: `STAGE_5B_9_REAL_DATA_VALIDATION.md` — **PASS / OWNER-ACCEPTED 2026-08-10**. The remaining work is Stage 5B.10 closeout only.

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
- [x] Validate repetitive-schedule change after MRP/QADPRO2 sync. (Accepted operational behavior established during Stage 5A live investigation; Stage 5B.9 reconfirmed work-order-backed post-MRP source semantics.)
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
- [ ] Owner acceptance passes.

**Completion gate:** A scheduler can open a configured workspace and use a validated, cached, real-data MPS grid for schedule review.

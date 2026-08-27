# KST v2 Master Project Checklist

**Current project position:** Stages 1–8 are complete and accepted. UI Navigation & Keyboard Ergonomics A is complete and accepted. R0 — Repository / Documentation Reconciliation is complete and accepted. The active cross-cutting effort is S0 — Security Foundation Integration; S0.1 — Security Policy Injection, S0.2 — Security Baseline Discovery, and S0.3 — Existing-Tool Security Checks are complete and owner-accepted (2026-08-24). The remaining S0 work is approved as checkpoints S0.4–S0.8 (Approved Planning Baseline — 2026-08-24 — see `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md`); S0.4 — Security Finding Disposition & Bounded Remediation is COMPLETE / ACCEPTED — 2026-08-25: S0.4A — QAD SQL Transport Correction is COMPLETE / ACCEPTED — 2026-08-25 (resolves `S0.2-F003` at the application-configuration level — `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`); S0.4B — Tauri Shell Capability is COMPLETE / ACCEPTED — 2026-08-25 (resolves `S0.2-F001` — `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`); S0.4C — npm Development-Tooling Advisories is COMPLETE / ACCEPTED — 2026-08-25 (resolves `S0.3-F001` — `docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md`). S0.5 — Security Regression & Architecture Checks is COMPLETE / ACCEPTED — 2026-08-26 (implemented 2026-08-25 — repository regression protection for the accepted S0.3 security gaps — see `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md`). S0.6 — Security Tool Admission is IN PROGRESS: Capability Review 1 (Rust Dependency Advisory Capability, gap `S0.3-G001`) is **COMPLETE / ACCEPTED — 2026-08-26** — cargo-audit 0.22.2 ADMITTED / ACCEPTED; S0.3-G001 — Covered / Resolved (`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`); cargo-deny 0.20.2 DEFERRED; Capability Review 2 (Dedicated Secret Scanning, gap `S0.3-G007`) is **COMPLETE / ACCEPTED — 2026-08-27** (Gitleaks v8.30.0 installed, release-integrity and synthetic-canary verified, scanned current KST content (4 findings) and full Git history (8 findings), all rule `private-key`, confirmed documentation false positives; `S0.3-G007` — Covered / Resolved) (`docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md`; `docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`; Gitleaks v8.30.1, TruffleHog v3.97.1, detect-secrets v1.5.0 DEFERRED); Capability Review 3 (Software Bill of Materials, gap `S0.3-G008`) is **COMPLETE / ACCEPTED — 2026-08-27** (Anchore Syft v1.51.1 installed, release-integrity verified, run against KST build/repository evidence and a complementary packaged-artifact view; six informational findings `S0.6-F014`–`S0.6-F019` recorded, none blocking; complete Tauri Windows installer/application bundle Unable to Verify / future packaged-release verification boundary, not Accepted Risk) (`docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`; `docs/security/S0_6_SBOM_ADMISSION.md`; Anchore Syft v1.51.1 — ADMITTED / IMPLEMENTED / ACCEPTED; Microsoft sbom-tool v4.1.5 and the CycloneDX ecosystem-native approach DEFERRED); `S0.3-G008` — Covered / Resolved; Capability Review 4 (Dedicated Static Application Security Testing (SAST), gap `S0.3-G006`) is **RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW / NO TOOL ADMITTED** (neutral research packet comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90; no tool installed or executed) (`docs/security/S0_6_SAST_ADMISSION_RESEARCH.md`); `S0.3-G006` — UNDER CAPABILITY REVIEW / RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW. Stage 9 begins only after S0 is closed and accepted.

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

**Status:** CURRENT

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
- [ ] S0.6 — Security Tool Admission (IN PROGRESS — Capability Review 1: Rust Dependency
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
      **RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW / NO TOOL ADMITTED** (neutral research
      packet comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90;
      no tool installed or executed) — see `docs/security/S0_6_SAST_ADMISSION_RESEARCH.md`
      (neutral research packet, not a recommendation or admission decision); `S0.3-G006` — UNDER
      CAPABILITY REVIEW / RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW).
- [ ] S0.7 — Runtime & Infrastructure Verification (PLANNED / NOT STARTED).
- [ ] S0.8 — Independent Assurance & S0 Closeout (PLANNED / NOT STARTED).

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

### Completed / accepted

- [x] Stage 1 — Project Charter.
- [x] Stage 2 — Broad legacy, UI, and dataset inventory.
- [x] Stage 3 — Technical Foundation.
- [x] Stage 4 / 4B — Application Shell, Workspace Configuration, and Workspace Scope Extension.
- [x] Stage 5 — MPS Data Foundation and Dashboard Implementation.
- [x] Stage 6 — Part Information Drill-Down.
- [x] Stage 7 — Work Orders and Kitting.
- [x] Stage 8 — Component and BOM Detail.
- [x] UI Navigation & Keyboard Ergonomics A.

### Current focus

- [x] R0 — Repository / Documentation Reconciliation.
- [ ] S0 — Security Foundation Integration (CURRENT).
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
  - [ ] S0.6 — Security Tool Admission (IN PROGRESS — Capability Review 1: Rust Dependency
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
        **RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW / NO TOOL ADMITTED** (neutral research
        packet comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90;
        no tool installed or executed) — see `docs/security/S0_6_SAST_ADMISSION_RESEARCH.md`
        (neutral research packet, not a recommendation or admission decision); `S0.3-G006` — UNDER
        CAPABILITY REVIEW / RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW).
  - [ ] S0.7 — Runtime & Infrastructure Verification (PLANNED / NOT STARTED).
  - [ ] S0.8 — Independent Assurance & S0 Closeout (PLANNED / NOT STARTED).
- [ ] Stage 9 — Immediate Shortages (NOT STARTED / BLOCKED PENDING S0 CLOSEOUT).

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

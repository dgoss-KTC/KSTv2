# KST v2 — Stage 5B VS Code/Copilot Implementation Prompt

You are working inside the KST v2 repository in VS Code on Windows.

Your task is to begin and complete:

# Stage 5B — MPS Dashboard Implementation

Stage 5A — MPS Data Inventory and Data Strategy has been completed and formally accepted by the project owner on 2026-08-07. The Stage 5B entry gate is open.

Do not redesign Stage 5A decisions. Treat the accepted Stage 5A artifacts as authoritative unless repository inspection reveals a concrete implementation conflict. If you find such a conflict, document it clearly and choose the smallest correction that preserves the accepted business behavior.

## 1. Read before changing code

Before making production changes, read the current repository versions of:

- `KST_v2_STAGE_5B_IMPLEMENTATION_PLAN.md`
- `KST_v2_STAGE_5A_MPS_DATA_INVENTORY.md`
- `KST_v2_STAGE_5A_MPS_BACKEND_DATA_CONTRACT.md`
- `KST_v2_STAGE_5A_SNAPSHOT_REFRESH_STRATEGY.md`
- `KST_v2_STAGE_5A_DATABASE_ACCESS_PERFORMANCE_STRATEGY.md`
- `KST_v2_STAGE_5A_MPS_API_SNAPSHOT_CONTRACT.md`
- `KST_v2_STAGE_5A_FISCAL_CALENDAR_STRATEGY.md`
- `qadpro2-data-map.md` or the equivalent `.json` / `.yaml`
- `BACKEND_PROJECT_BOUNDARIES.md`
- `TECHNICAL_FOUNDATION.md`
- `API_CONTRACT_WORKFLOW.md`
- `OPENAPI_CLIENT_GENERATION.md`
- `BUILD_AND_TEST.md`
- `CURRENT_PROJECT_STATUS.md`
- the current Master Project Checklist

Also inspect the actual repository structure and existing implementations for:

- `QadConnectionOptions`
- QAD connectivity abstractions/checks
- workspace configuration and Stage 4B scope-resolution code
- existing snapshot abstractions/store
- API endpoint and Problem Details conventions
- OpenAPI generation workflow
- frontend workspace state
- current Settings UI and persistence mechanism
- existing tests and architecture-boundary tests

Do not create duplicate infrastructure when an existing abstraction already serves the purpose.

## 2. Architectural boundaries that must remain intact

Preserve these project boundaries:

- React/TypeScript frontend owns display behavior and fiscal-calendar logic.
- C# domain/application owns business semantics, MPS normalization, week/Falldown rules, snapshot construction, refresh orchestration, and API-facing models.
- `Kst.Integrations.Qad` owns QAD-specific SQL, `Microsoft.Data.SqlClient`, Dapper, raw QAD records, and source mappings.
- Tauri/Rust remains a thin desktop/process-lifecycle host.
- QAD access is read-only.
- Do not write to QAD.
- Do not create a new stored procedure or TVF for the initial MPS.
- Do not call legacy `sp_QAD_ktmpswkm`.
- Do not leak QAD field names or SQL-specific concepts into frontend contracts.

## 3. Accepted MPS source rules

The workspace is already resolved to a site/domain and parent-part scope before the MPS source query is executed.

The initial MPS source must use a direct parameterized SQL Server 2016-compatible query in `Kst.Integrations.Qad`.

Retrieve the accepted source fields:

```text
Domain
Site
ParentPart
Description = pt_mstr.pt_desc1
DueDate
ReleaseDate
Quantity
MrpType
WorkOrderId
WorkOrderStatus
```

Use these source filters:

```text
mrp_dataset = 'wo_mstr'
mrp_type IN ('supply', 'supplyf', 'supplyp')
wo_status <> 'C'
exclude wo_bom_code = 'RMABOM'
```

Use the accepted MRP → work-order identity:

```text
mrp_domain = wo_domain
mrp_site   = wo_site
mrp_part   = wo_part
mrp_nbr    = wo_nbr
mrp_line   = wo_lot
```

Also join `pt_mstr` by domain + part for `pt_desc1` only.

Important implementation check: the business rule is to exclude `RMABOM`, not to require a non-null BOM code. Inspect representative data/schema behavior and use a null-safe predicate if ordinary qualifying work orders can have `wo_bom_code IS NULL`.

`rps_mstr` is intentionally excluded. Repetitive-schedule changes are expected to become work-order-backed MRP records after the scheduler runs MRP or after overnight MRP processing.

Do not add `DISTINCT`, SQL weekly aggregation, pivoting, or defensive deduplication without new evidence. Representative KTC/SW diagnostics found no duplicate rows at the accepted source grain.

## 4. Part-scope parameterization

Never concatenate raw part numbers into executable SQL.

Use a SQL Server 2016-compatible parameterized set approach, such as a parameterized `VALUES` scope table or the repository-consistent equivalent.

Initial batching rule:

```text
maximum 500 parent parts per query batch
```

Merge batch results before normalization. Do not issue one database query per part.

Ensure configured parent parts remain in the final schedule even when they have zero MPS facts. If `pt_desc1` is not already available from workspace scope metadata, use a bounded metadata lookup/merge rather than losing the row.

## 5. Date coverage and Falldown

Retain both Due Date and Release Date in the same in-memory source snapshot.

Future source coverage must support the maximum 72-week visible horizon for either date basis.

For Falldown, retrieve **all qualifying unfinished work orders with no historical lower date bound**.

Business-week rules:

```text
week = Sunday through Saturday
visible week label/anchor = Monday
Falldown = all qualifying unfinished work whose Due Date is before the current business week
```

Falldown remains Due-Date based even when the visible MPS grid is in Release Date mode.

Due/Release switching and horizon switching up to 72 weeks must be local operations on the current snapshot and must not trigger another QAD query.

## 6. Status semantics

Normalize QAD work-order states as accepted:

```text
A -> Allocating
F -> Frozen
R -> Released
P -> Planned
e -> ExplicitlyScheduled
C -> excluded in SQL
unexpected non-C -> Unknown/diagnostic
```

Execution state considers only distinct A/F/R states in a bucket:

```text
none        -> None
A only      -> Allocating
F only      -> Frozen
R only      -> Released
2+ A/F/R    -> Mixed
```

Independent bucket flags:

```text
any P -> ContainsPlannedWork = true
any e -> ContainsExplicitlyScheduledWork = true
```

P/e do not create Mixed by themselves.

Quantity is the sum of all included source quantities for the parent/bucket.

Retain minimal internal WO references needed for later explanation/drill-down:

```text
WorkOrderId
WorkOrderState
```

Do not expose those references publicly in Stage 5B unless the current UI actually requires them.

## 7. Snapshot and refresh behavior

MPS loads automatically when a workspace opens, but the workspace shell should appear immediately while MPS is loading.

Initial load flow:

```text
open workspace
-> show shell
-> resolve current workspace part scope
-> load MPS source facts
-> normalize/build snapshot
-> atomically publish completed snapshot
```

Refresh rules:

- explicit Refresh re-resolves workspace scope and rebuilds the entire MPS snapshot;
- only one refresh per workspace at a time;
- keep the current good snapshot visible during refresh;
- replace it atomically only after full success;
- on refresh failure, retain the previous good snapshot and its last-successful timestamp;
- show the last successful refresh time;
- no automatic background refresh in Stage 5B;
- no cross-session/offline MPS persistence initially.

Initial database failure with no good snapshot is not an empty result. Present a retryable unavailable state with this user-facing message:

> Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.

A successful query returning zero schedule facts is a valid empty result and must remain distinct from database failure.

## 8. QAD connectivity/performance defaults

Use the established project architecture:

- `Microsoft.Data.SqlClient`
- Dapper
- Windows-integrated authentication
- read-only access
- existing .NET options/configuration mechanism
- existing cancellation/error conventions

Use the accepted initial MPS command timeout of 60 seconds, backend-configurable but not an end-user setting.

Do not add hidden automatic retries.

Record safe diagnostics for operation, elapsed time, row/part counts, and failure category without logging credentials or full connection strings.

Measure at minimum during representative validation:

```text
resolved parent-part count
query batch count
source row count
database elapsed time
normalization/bucketing elapsed time
total refresh elapsed time
```

Do not invent a performance SLA before measuring real workloads.

## 9. API/OpenAPI boundary

Use the accepted Stage 5A snapshot/API model and reconcile exact names/routes to existing repository conventions.

Conceptual response:

```text
MpsDashboardResponse
- Snapshot
- DateBasis
- HorizonWeeks
- Parts[]
```

```text
MpsSnapshotMetadata
- SnapshotId
- CreatedAtUtc
- LastSuccessfulRefreshAtUtc
- Status
- WorkspaceId
- Site
- ResolvedParentPartCount
- SourceRowCount
```

```text
MpsPartScheduleDto
- ParentPart
- Description
- Buckets[]
```

```text
MpsBucketDto
- Kind
- WeekLabel
- Quantity
- ExecutionStatus
- ContainsPlannedWork
- ContainsExplicitlyScheduledWork
```

Do not put fiscal year/week/period/quarter into the backend solely for MPS display.

Follow existing Problem Details/error conventions, update OpenAPI, and regenerate TypeScript contracts using the repository workflow.

## 10. Frontend fiscal calendar

Fiscal semantics are frontend-only planning/display behavior. QAD and the backend do not understand fiscal years.

Accepted settings model:

```text
Anchor Fiscal Year: FY2026
Anchor Start Date: 2025-06-29
Standard Pattern: 4-4-5 x 4
```

Normal years are generated automatically by 52-week progression.

A 53-week exception stores:

```text
FiscalYear
ExtraWeekPeriod (1..12)
```

A 53-week year advances the next fiscal-year start by 53 weeks and assigns the additional week to the configured period.

Add a Fiscal Calendar section to the existing Settings page. Do not redesign the whole Settings navigation in Stage 5B.

Validate:

- one exception per fiscal year;
- period in range 1–12;
- settings persistence/reload;
- standard year;
- 53-week year;
- shifted subsequent year;
- quarter boundaries;
- horizons crossing fiscal years.

## 11. Initial real-data MPS UI

Replace prototype/fake schedule content with the Stage 5B real-data vertical slice.

Required initial UI:

- sticky Parent Part / `pt_desc1` description column;
- Falldown column/bucket;
- horizontally scrollable weekly quantities;
- Monday week labels;
- fiscal period bands;
- fiscal quarter bands;
- horizon selector up to 72 weeks;
- Due Date / Release Date mode;
- last-refresh indicator;
- loading state;
- valid-empty state;
- database-unavailable + Retry state;
- refresh-in-progress state;
- refresh-failed-but-old-data-retained state.

Presentation semantics:

- A/F/R/Mixed -> execution-state box/background treatment;
- Planned P -> distinct accessible foreground/font treatment;
- Explicitly Scheduled e -> non-color marker such as a strong top edge;
- combinations retain all applicable signals;
- do not encode CSS colors or style names in backend DTOs.

Full work-order drill-down remains a later stage.

## 12. Implement in controlled checkpoints

Follow `KST_v2_STAGE_5B_IMPLEMENTATION_PLAN.md` in this order:

```text
5B.0 Repository preflight and contract reconciliation
5B.1 Real QAD connectivity and read boundary
5B.2 Workspace scope to MPS source query
5B.3 Source normalization and typed MPS facts
5B.4 Business-week, Falldown, and status projection
5B.5 Workspace MPS snapshot and refresh orchestration
5B.6 API and OpenAPI contract
5B.7 Frontend fiscal calendar settings and service
5B.8 Initial MPS grid vertical slice
5B.9 Representative data validation and performance pass
5B.10 Full verification, packaging, and documentation closeout
```

Keep the repository buildable and testable at every checkpoint.

Do not skip directly to UI work before the source/query/normalization/application contracts are working and tested.

If a checkpoint exposes a real repository mismatch, resolve the smallest necessary issue and document it before continuing. Do not casually broaden scope.

## 13. Verification expectations

Use the repository's established commands/workflows rather than inventing parallel tooling.

Run the narrowest relevant tests at each checkpoint, and complete the full established verification set before Stage 5B closeout, including as applicable:

- backend build/tests;
- architecture tests;
- API integration tests;
- frontend lint;
- frontend typecheck;
- frontend tests;
- frontend production build;
- Rust/Tauri checks;
- sidecar build/publish verification;
- packaged application validation if required by the Stage 5B gate.

Representative data validation must include, when accessible:

- KTC workspace/site;
- KTV workspace/site;
- KTS/KS when applicable;
- product-line-derived scope;
- explicit-parent scope;
- parent with no MPS facts;
- closed WO exclusion;
- RMABOM exclusion;
- old unfinished Falldown work;
- status combinations including A/F/R/Mixed/P/e;
- repetitive-schedule before/after MRP behavior;
- large workspace requiring multiple 500-part batches.

## 14. Explicit Stage 5B non-goals

Do not implement:

- full work-order drill-down;
- BOM/component drill-down;
- kitting/allocation calculations;
- shortage engine;
- PO drill-down;
- inventory-detail views;
- customer-order features;
- unrelated exports;
- offline/persisted MPS snapshots;
- automatic background refresh;
- QAD writes;
- new stored procedures/TVFs;
- wholesale Settings redesign.

## 15. Working behavior for this coding session

Begin with checkpoint **5B.0**: inspect and reconcile the actual repository before making substantive production changes.

Then proceed checkpoint-by-checkpoint in order. Do not ask for confirmation between ordinary checkpoints if the accepted documents and repository provide enough information. If something is genuinely blocked by unavailable environment access or a repository contradiction, make the maximum safe progress, document the blocker precisely, and continue with independent work where possible.

At the end of the coding session, report:

1. checkpoints completed;
2. files changed;
3. tests/commands run and results;
4. real-data validations performed;
5. unresolved blockers or implementation-time observations;
6. documentation/checklist updates made;
7. exact next checkpoint if Stage 5B is not complete.

Do not begin Stage 6 or later work.

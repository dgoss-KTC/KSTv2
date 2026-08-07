# KST v2 — Stage 5B MPS Dashboard Implementation Plan

**Status:** READY TO BEGIN — Stage 5A accepted 2026-08-07  
**Stage:** 5B — MPS Dashboard Implementation  
**Purpose:** Implement the first real-data scheduling vertical slice using the accepted Stage 5A source, data, snapshot, refresh, API, and fiscal-calendar contracts.

---

## 1. Entry gate

Stage 5A owner acceptance was received on 2026-08-07. The Stage 5B entry gate is open.

Before implementation, the coding agent must read the current repository documentation and inspect the existing code rather than recreating established infrastructure.

Authoritative Stage 5A inputs:

- `KST_v2_STAGE_5A_MPS_DATA_INVENTORY.md`
- `KST_v2_STAGE_5A_MPS_BACKEND_DATA_CONTRACT.md`
- `KST_v2_STAGE_5A_SNAPSHOT_REFRESH_STRATEGY.md`
- `KST_v2_STAGE_5A_DATABASE_ACCESS_PERFORMANCE_STRATEGY.md`
- `KST_v2_STAGE_5A_MPS_API_SNAPSHOT_CONTRACT.md`
- `KST_v2_STAGE_5A_FISCAL_CALENDAR_STRATEGY.md`
- `qadpro2-data-map.md` / `.json` / `.yaml`
- `BACKEND_PROJECT_BOUNDARIES.md`
- `TECHNICAL_FOUNDATION.md`
- current Master Project Checklist and Current Project Status

The accepted business rules in these artifacts take precedence over older assumptions in prototype/legacy documents.

---

## 2. Stage 5B implementation principles

1. Implement in controlled vertical slices; do not attempt the entire MPS dashboard in one coding pass.
2. Preserve existing project boundaries:
   - QAD-specific SQL and raw mappings in `Kst.Integrations.Qad`.
   - business semantics and typed concepts outside SQL.
   - application orchestration in `Kst.Application`.
   - API mapping/OpenAPI in `Kst.Api`.
   - fiscal planning/display logic in the React/TypeScript frontend.
3. Do not call `sp_QAD_ktmpswkm` and do not create a new stored procedure/TVF for the initial MPS.
4. Do not introduce customer code, IOS code, planner, or buyer as workspace-scope dependencies.
5. Do not add SQL pivots, weekly SQL aggregation, `DISTINCT`, or defensive deduplication without evidence.
6. Do not preload later-stage work-order, BOM, kitting, shortage, PO, or inventory-detail capabilities.
7. Keep source retrieval, semantic normalization, snapshot lifecycle, and visual presentation independently testable.
8. At every slice gate, run the narrowest relevant automated checks before proceeding.

---

# 3. Controlled implementation sequence

## 5B.0 — Repository preflight and contract reconciliation

### Goal

Confirm the real code locations, conventions, and existing abstractions before making production changes.

### Tasks

- Inspect the current solution/project tree.
- Locate and read:
  - `QadConnectionOptions`;
  - `IQadConnectivityCheck` / `DisabledQadConnectivityCheck`;
  - workspace configuration/application services;
  - workspace scope-resolution code introduced in Stage 4B;
  - existing snapshot concepts (`SnapshotId`, `SnapshotStatus`, `ISnapshotStore`, etc.);
  - existing API endpoint/Problem Details conventions;
  - OpenAPI client-generation workflow;
  - frontend workspace state and Settings surface.
- Confirm actual NuGet/package state for `Microsoft.Data.SqlClient` and Dapper before adding packages.
- Confirm the exact .NET configuration binding path for QAD connection options.
- Identify the existing test-project layout and architecture tests.
- Identify how the frontend persists ordinary application settings today; reuse that mechanism for fiscal settings where appropriate.
- Confirm how resolved workspace parents are represented. Determine whether `pt_desc1` is already available during scope resolution; if not, plan a bounded part-metadata lookup/merge so parents with zero MPS facts still receive their description.

### Exit gate

- No duplicate infrastructure has been introduced.
- Existing repository abstractions and naming conventions are documented in the implementation notes.
- The implementation locations for QAD adapter, application service, API, frontend settings, and MPS UI are known.

---

## 5B.1 — Real QAD connectivity and read boundary

### Goal

Turn the Stage 3 placeholder QAD boundary into a verified read-only SQL Server connection without implementing MPS behavior yet.

### Tasks

- Wire the existing `QadConnectionOptions` through the established .NET options/configuration mechanism.
- Use Windows-integrated authentication.
- Add/use `Microsoft.Data.SqlClient` and Dapper only inside the QAD integration boundary.
- Implement the real connectivity check behind the existing connectivity abstraction.
- Propagate `CancellationToken` through connection open and async query operations where supported.
- Use the accepted initial MPS command timeout of 60 seconds, backend-configurable but not exposed as an end-user setting.
- Do not add hidden automatic retry loops.
- Add diagnostic logging for operation, elapsed time, failure category, and safe environment context without logging credentials or full connection strings.
- Verify the account/context can read the required QADPRO2 tables and does not depend on write privileges.

### Verification

- Connectivity succeeds against the intended environment.
- A deliberate unavailable/invalid environment produces a controlled backend failure.
- Cancellation/timeout paths are testable.
- Architecture tests confirm SQL client dependencies have not leaked into Domain/Application.

### Exit gate

The backend can establish a real, read-only QAD connection through `Kst.Integrations.Qad` using existing application configuration.

---

## 5B.2 — Workspace scope to MPS source query

### Goal

Return the accepted row-oriented MPS source facts for one resolved workspace.

### Query contract

For one site/domain and the already-resolved parent-part list, retrieve:

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

Use the accepted source rules:

```text
mrp_dataset = 'wo_mstr'
mrp_type IN ('supply', 'supplyf', 'supplyp')
wo_status <> 'C'
exclude RMA BOM code 'RMABOM'
```

Safe MRP → WO identity:

```text
mrp_domain = wo_domain
mrp_site   = wo_site
mrp_part   = wo_part
mrp_nbr    = wo_nbr
mrp_line   = wo_lot
```

Scope the query to the resolved workspace Domain + Site + parent parts.

### Date coverage

- No historical lower bound for qualifying unfinished work needed for Falldown.
- Future coverage must support the maximum 72-week visible horizon for **either** Due Date or Release Date basis.
- Retain both dates in the same source snapshot.

### Part-list parameterization

- Never concatenate raw part values into executable SQL.
- Use a parameterized `VALUES` scope table or equivalent SQL Server 2016-compatible form.
- Initial maximum: 500 parent-part parameters per query batch.
- Merge batch results before application normalization.
- Do not query once per part.

### Implementation checks before freezing SQL

- Verify whether ordinary qualifying WOs can have `wo_bom_code IS NULL`. The business rule is **exclude RMABOM**, not “require a non-null BOM.” If null is valid, use a null-safe predicate that excludes only `RMABOM`.
- Verify site casing behavior; normalize at the integration boundary rather than letting casing create duplicate business identities.
- Preserve the accepted no-deduplication behavior unless representative evidence contradicts it.
- Ensure resolved parents with no MPS facts are not lost from the final schedule. The application must merge source facts back onto the full resolved parent scope; if description is not already present on scope metadata, retrieve `pt_desc1` separately or shape the query so metadata survives an empty fact set.

### Tests

- SQL/adapter parameterization.
- One part.
- Multiple parts.
- >500-part scope batching.
- No-result part.
- Closed WO excluded.
- RMABOM excluded.
- SUPPLY/SUPPLYF/SUPPLYP accepted.
- `rps_mstr` excluded.
- Both due and release dates mapped.
- Cancellation and timeout.

### Exit gate

A representative workspace returns raw source rows that agree with direct Excel/QAD evidence, without pivoting or weekly aggregation.

---

## 5B.3 — Source normalization and typed MPS facts

### Goal

Convert QAD-specific rows into stable application-facing MPS semantics.

### Tasks

- Keep the private integration record QAD-shaped where useful.
- Normalize into the accepted `MpsSourceRow` contract.
- Normalize supply type:
  - `SUPPLY` → Supply
  - `SUPPLYF` → SupplyF
  - `SUPPLYP` → SupplyP
- Normalize work-order state:
  - `A` → Allocating
  - `F` → Frozen
  - `R` → Released
  - `P` → Planned
  - `e` → ExplicitlyScheduled
  - unexpected non-C value → Unknown/diagnostic state
- `C` should already have been removed by SQL.
- Preserve Domain, Site, parent part, description, Due Date, Release Date, quantity, WO ID, and semantic WO state.
- Do not expose QAD field names beyond the integration boundary.

### Tests

- Every accepted MRP type.
- Every accepted WO state.
- Unknown non-C state.
- Null/optional description or release date if observed by source schema/runtime.
- Decimal quantity preservation.

### Exit gate

Downstream application code can operate entirely on typed MPS concepts without interpreting QAD strings.

---

## 5B.4 — Business-week, Falldown, and status projection

### Goal

Build deterministic schedule buckets from normalized source facts.

### Business-week rules

- Business week is Sunday–Saturday.
- Monday is the visible week label/anchor used by the MPS UI.
- Falldown is **all qualifying unfinished WOs whose Due Date is before the current business week**.
- Falldown remains Due-Date based even when the visible grid uses Release Date mode.
- Weekly projection can use Due Date or Release Date for non-Falldown buckets.
- Maximum visible horizon is 72 weeks.

### Quantity/status rules

For each parent + bucket:

```text
Quantity = SUM(all included source quantities)
```

Execution state uses distinct A/F/R states only:

```text
none        -> None
A only      -> Allocating
F only      -> Frozen
R only      -> Released
2+ A/F/R    -> Mixed
```

Independent flags:

```text
any P -> ContainsPlannedWork = true
any e -> ContainsExplicitlyScheduledWork = true
```

P/e do not create Mixed by themselves.

### Internal work-order references

Retain the minimum accepted WO references needed to explain bucket semantics later:

```text
WorkOrderId
WorkOrderState
```

Do not expose them through the initial public API unless the initial UI actually requires them.

### Tests

- Sunday/Saturday boundaries.
- Current week versus prior week.
- Very old unfinished WO in Falldown.
- Due/Release mode rebucketing from the same source facts.
- Year boundary.
- One WO per bucket.
- Multiple WOs with the same state.
- A/F/R mixed combinations.
- P only, e only, P+e.
- A/F/R combined with P/e.
- Quantity aggregation.
- 12/24/52/72-week projections without source reload.

### Exit gate

Pure application tests prove weekly/Falldown/status behavior independently of SQL and frontend styling.

---

## 5B.5 — Workspace MPS snapshot and refresh orchestration

### Goal

Integrate real MPS facts with the workspace lifecycle using the accepted snapshot semantics.

### Initial load

```text
Open workspace
    ↓
show workspace shell immediately
    ↓
resolve current workspace part scope
    ↓
load MPS source facts
    ↓
normalize + build snapshot
    ↓
atomically publish completed snapshot
```

MPS begins loading automatically when the workspace opens.

### Snapshot contents

Retain enough in memory to:

- rebuild Due/Release projections locally;
- change horizon locally up to 72 weeks;
- preserve explicit parents with zero facts;
- retain minimal WO references/statuses internally;
- report snapshot identity/timestamps/source counts.

No cross-session/offline MPS persistence initially.

### Refresh

- Explicit Refresh re-resolves workspace part scope and rebuilds the entire MPS snapshot.
- Only one refresh per workspace may run at a time.
- Keep the current good snapshot visible while refresh runs.
- Publish the replacement atomically only after complete success.
- On refresh failure, retain the old snapshot and its last-successful time.
- Show last successful refresh time.
- No automatic background refresh initially.
- Tab switching, Due/Release toggle, fiscal display changes, and horizon changes ≤72 weeks must not cause a QAD reload.

### Failure states

Initial load failure with no good snapshot must not masquerade as an empty schedule.

Frontend message:

> **Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.**

Provide Retry.

A successful query with zero schedule facts is a valid empty result and remains distinct from database failure.

### Tests

- Initial load success.
- Initial load database failure.
- Retry success.
- Refresh success.
- Refresh failure with old snapshot retained.
- Concurrent refresh prevention.
- Workspace scope changed then Refresh.
- Part with zero facts remains present.
- Snapshot timestamps/counts.

### Exit gate

Workspace MPS state behaves correctly under successful, empty, loading, refreshing, failed, and retry scenarios.

---

## 5B.6 — API and OpenAPI contract

### Goal

Expose the completed MPS snapshot through the existing loopback API without leaking QAD records.

### Candidate response

Use the accepted Stage 5A shape, reconciled to repository naming conventions:

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

Do not expose fiscal year/week/period/quarter solely for MPS display.

### Tasks

- Follow existing endpoint naming, mapping, validation, and Problem Details conventions.
- Choose exact load/read/refresh route shapes after inspecting existing workspace APIs.
- Ensure a local view change can project from current source snapshot without forcing QAD reload.
- Represent database unavailability through the existing API error boundary rather than SQL-specific response details.
- Update OpenAPI.
- Regenerate TypeScript client/contracts using the repository workflow.
- Verify generated client changes are deterministic and checked in according to repo policy.

### Exit gate

The frontend can consume typed MPS data and refresh state entirely through generated API contracts.

---

## 5B.7 — Frontend fiscal calendar settings and service

### Goal

Implement fiscal planning/display independently of QAD/backend scheduling semantics.

### Settings model

Accepted anchor:

```text
Anchor Fiscal Year: FY2026
Anchor Start Date: 2025-06-29
Standard Pattern: 4-4-5 × 4
```

Exception:

```text
FiscalYear
ExtraWeekPeriod (1..12)
```

Normal years require no annual maintenance.

### Tasks

- Add a Fiscal Calendar section to the current Settings page; do not redesign all Settings navigation in this slice.
- Seed FY26 / June 29, 2025 as the initial anchor.
- Generate normal year starts by 52-week progression.
- Generate exceptional years by 53-week progression.
- Assign the extra week to the configured fiscal period.
- Validate one exception per fiscal year and period range 1–12.
- Derive fiscal week, period, and quarter for displayed MPS weeks.
- Keep fiscal semantics entirely frontend-side.
- Add a simple future-calendar preview if it can be implemented without expanding Stage 5B scope; otherwise defer preview polish while retaining validation.

### Tests

- Standard 4-4-5 year.
- FY26 anchor.
- 53-week year with extra week in a selected period.
- Subsequent fiscal-year start shift.
- Quarter boundaries.
- 72-week horizon crossing fiscal years.
- Settings persistence/reload.

### Exit gate

Frontend can derive correct fiscal labels/bands for every displayed MPS week without backend fiscal fields.

---

## 5B.8 — Initial MPS grid vertical slice

### Goal

Replace prototype/fake schedule content with a usable real-data MPS grid.

### Required initial UI

- sticky Parent Part / `pt_desc1` description column;
- Falldown column/bucket;
- horizontally scrollable weekly quantities;
- week labels using Monday anchors;
- fiscal period bands;
- fiscal quarter bands;
- horizon selector up to 72 weeks;
- Due Date / Release Date mode;
- snapshot last-refresh indicator;
- loading state;
- valid-empty state;
- database-unavailable + Retry state;
- refresh-in-progress/failure-with-stale-data state.

### Presentation semantics

- A/F/R/Mixed → execution-state box treatment.
- Planned (`P`) → distinct accessible foreground/font treatment; do not rely on low contrast or color alone.
- Explicitly scheduled (`e`) → non-color marker such as a strong top edge.
- Combined states retain all applicable signals.
- Do not encode colors in backend DTOs.
- Keep row/week selection only to the extent needed for the initial dashboard; full WO drill-down remains a later stage.

### Interaction rules

- Due/Release change is local.
- Horizon change ≤72 is local.
- Fiscal display is local.
- Refresh is the only ordinary action that re-resolves scope and reloads QAD for this slice.

### Exit gate

A scheduler can open a workspace and review real schedule quantities with accepted status and fiscal presentation.

---

## 5B.9 — Representative data validation and performance pass

### Goal

Prove the implementation against real QAD behavior before treating the slice as complete.

### Required validation cases

- representative KTC workspace/site;
- representative KTV workspace/site when environment access permits;
- KTS/KS when applicable and accessible;
- product-line-derived scope;
- explicit-parent scope;
- parent with no current MPS facts;
- one-WO bucket;
- multi-WO bucket;
- A/F/R/Mixed/P/e combinations;
- old unfinished Falldown WO;
- closed WO excluded;
- RMABOM excluded;
- repetitive-schedule change before/after scheduler runs MRP and QADPRO2 syncs;
- empty-result workspace;
- large workspace requiring multiple 500-part batches.

### Compare

- direct Excel/QAD source rows versus adapter rows;
- bucket totals versus source facts;
- selected output versus legacy KST where the legacy output remains behaviorally comparable;
- Due/Release rebucketing against underlying dates;
- fiscal period/quarter headers against known calendar examples.

### Performance measurements

Record at minimum:

```text
resolved parent-part count
query batch count
source row count
database elapsed time
normalization/bucketing elapsed time
total refresh elapsed time
```

Do not invent a hard SLA in advance. Tune batching/queries only from observed evidence.

### Exit gate

Representative data and timing evidence are recorded, discrepancies are explained, and no known correctness issue remains hidden by aggregation or UI formatting.

---

## 5B.10 — Full verification, packaging, and documentation closeout

### Automated verification

Run the repository's established verification set, including as applicable:

```text
Backend build/test
Architecture tests
API integration tests
Frontend lint
typecheck
frontend tests
frontend production build
Rust/Tauri checks
sidecar build/publish verification
```

Add focused tests for:

- QAD adapter/query mapping;
- business-week/Falldown;
- status classification;
- snapshot refresh/error lifecycle;
- frontend fiscal calendar;
- MPS component states.

### Manual verification

- Development launch with real QAD connectivity.
- Workspace initial load.
- Refresh and failure recovery.
- Due/Release and horizon switching.
- Fiscal bands.
- large-workspace behavior.
- packaged Tauri sidecar launch and MPS load, if the Stage 5B completion gate requires the packaged path.
- application close leaves no backend orphan process (Stage 3 regression).

### Documentation

Update:

- Master Project Checklist;
- Current Project Status;
- MPS data inventory with implementation-confirmed observations;
- final direct-query contract;
- API documentation/OpenAPI workflow docs if changed;
- fiscal-calendar Settings documentation;
- MRP freshness/RMA troubleshooting notes;
- any technical-foundation docs whose real QAD configuration changes implementation instructions.

### Exit gate

All automated/manual gates pass and the project owner accepts Stage 5B behavior.

---

# 4. Explicit non-goals for Stage 5B

Do not implement in this stage:

- work-order drill-down details beyond minimal internal references;
- BOM/component drill-down;
- kitting/allocation calculations;
- shortage engine/status;
- purchase-order drill-down;
- inventory-detail views;
- customer-order features;
- exports unrelated to this MPS slice;
- offline/persisted MPS snapshots;
- automatic background refresh;
- QAD writes;
- new stored procedures/TVFs for the initial MPS;
- global Settings redesign merely to host the fiscal section.

---

# 5. Stage 5B implementation checkpoints

Do not proceed blindly through all phases. Recommended checkpoints:

| Checkpoint | Demonstrable result |
|---|---|
| 5B.0 | Repository locations/conventions reconciled |
| 5B.1 | Real read-only QAD connection works |
| 5B.2 | Direct query returns validated source rows |
| 5B.3 | QAD strings normalize to typed MPS facts |
| 5B.4 | Pure tests prove week/Falldown/status logic |
| 5B.5 | Workspace snapshot/load/refresh lifecycle works |
| 5B.6 | Typed API/OpenAPI client works |
| 5B.7 | Frontend fiscal calendar works independently |
| 5B.8 | Real MPS grid is usable |
| 5B.9 | Representative real-data/performance validation passes |
| 5B.10 | Full regression/package/docs/owner acceptance passes |

Each checkpoint should leave the repository buildable and testable.

---

# 6. Stage 5B completion definition

Stage 5B is complete when a scheduler can open a configured workspace and use a validated real-data MPS dashboard that:

- loads the accepted workspace scope from QAD;
- shows all configured parent rows, including those with no current schedule facts;
- shows correct Falldown and weekly quantities;
- excludes closed and RMA work orders;
- reflects MRP-generated work-order-backed schedule state;
- correctly presents Allocating/Frozen/Released/Mixed plus Planned/Scheduled signals;
- switches Due/Release and horizons locally from one snapshot;
- displays frontend-derived fiscal period/quarter bands;
- refreshes atomically without discarding good data on failure;
- reports database-unavailable conditions truthfully;
- passes automated and representative real-data validation;
- is accepted by the project owner.


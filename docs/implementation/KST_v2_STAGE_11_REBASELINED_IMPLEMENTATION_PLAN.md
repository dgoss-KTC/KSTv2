# KST v2 -- Stage 11 Re-Baselined Shared-MRP Implementation Plan

**Status:** Superseded for Stage 11 scheduling by `stage11_component_mrp_algorithm.md` on 2026-09-23 by project-owner decision. Retained as historical implementation evidence; its Monday-to-Sunday buckets, past receipts in projected QOH, and planned due in official balance are not current calculation rules. The prior implementation was not owner-accepted.

## 1. Authority and Scope

This document supersedes `KST_v2_STAGE_11A_IMPLEMENTATION_PLAN.md` and `KST_v2_STAGE_11A_BUILD_PROMPT.md` as instructions for future Stage 11 work. Those documents, the currently checked-in Stage 11-A implementation, and their custom WOD/PO/forecast projection are retained only as historical checkpoint evidence. They are not a scheduling authority and must not be extended.

Stage 11 is a shared, raw-QAD-MRP scheduling foundation with two deliberately separate consumers:

- **Stage 11-A Workspace Shortages:** current MPS-snapshot BOM occurrences choose report components. Each distinct domain/site/component has exactly one site-wide MRP schedule and balance; parent membership is evidence, not an additional calculation input.
- **Stage 11-B Single-Part MRP:** a later, separately authorized capability that uses the same schedule engine for a directly selected part. It is not implemented, specified as a UI/API, or otherwise started by this plan.

Stage 9 and Stage 10 remain COMPLETE / ACCEPTED / LOCKED. Do not modify their readers, calculation semantics, APIs, UI, contracts, or exports to implement Stage 11. All QAD access remains SELECT-only, parameterized, domain/site bounded, cancellation-aware, and contained in `Kst.Integrations.Qad`.

## 2. Replacement Architecture

### 2.1 Retain and reuse

Retain these seams because they are architectural mechanisms rather than the superseded schedule semantics:

| Layer | Retained seam | Required use in the replacement |
|---|---|---|
| Domain | `MpsPartBatcher`; existing immutable records and pure builders | Batch component scope and keep calculation logic pure/testable. Add a dedicated Monday calendar; do not alter the MPS Sunday calendar. |
| Application | MPS snapshot store/state; workspace configuration; BOM reader pattern | Validate workspace and exact snapshot, resolve current-effective BOM occurrences, retain parent attribution, then form one case-insensitive distinct component set. |
| Infrastructure | Snapshot-aware in-memory cache pattern | Cache only immutable shared-MRP projections using a complete identity described below. |
| QAD integration | `QadConnectionFactory`, `QadSiteDomainMap`, `QadConnectionOptions`, Dapper command/cancellation/batching conventions | Add focused raw MRP and opening-QOH readers. Source SQL remains at this boundary. |
| API | Endpoint/DTO/OpenAPI/generated-TypeScript flow and problem-details mapping | Expose projection data with raw evidence needed by UI/export; regenerate contracts through the normal pipeline. |
| Frontend | `CustomerWorkspace`, typed hook/API pattern, blocking modal behavior, horizontal grid pattern | Replace the current Stage 11-A screen with a 24-week Monday-grid consumer of the shared projection. |
| Export | `Kst.Exports` and cached-input-only export boundary | Generate a workbook from the compatible immutable projection, never from a re-query. |

### 2.2 Retire and remove

The following Stage 11-A-specific concepts are obsolete and must not survive as scheduling inputs:

- Sunday-start `MpsBusinessCalendar` use for Stage 11, Sunday Week 1, and the old Week-25 boundary.
- WO residual demand, firm allocations, conventional PO receipt, KSS classification, and `fcs_sum` forecast as the Stage 11 schedule authority.
- `QadLongTermShortageSourceReader.BuildBatchQuery` custom multi-fact query, including its Stage 9 usable-allocation joins and its old opening-QOH exclusions for `INSPECT` and `NCMINSP`.
- Current `LongTermShortagesBuilder` week netting, DTO/API fields, frontend display, detail timeline, export columns, and fixtures that assert the obsolete projection.
- Stage 11-only quantity-display rounding. Raw QAD decimals remain calculation and evidence values; UI/export display formatting must not alter them.

Do not delete any of this during planning. A later authorized build must first introduce the replacement seams and deterministic tests, migrate every Stage 11-A consumer, then remove obsolete code, tests, OpenAPI/generated types, CSS, and documentation references in one reviewed cleanup slice. No compatibility adapter is justified because the checkpoint is explicitly superseded and unaccepted.

## 3. Shared MRP Schedule Model

### 3.1 Calendar and range

Introduce a Stage 11 domain calendar that is distinct from the existing MPS Sunday-through-Saturday business calendar:

- A Stage 11 week begins Monday and ends Sunday.
- `weekOneStart` is the Monday of the refresh date's week.
- The main grid has exactly 24 forward columns, `Week 1` through `Week 24`, with starts `weekOneStart + 7 * (n - 1)`.
- `horizonEnd` is the exclusive start of Week 25: `weekOneStart + 168 days`.
- A calculated `Past` bucket contains qualifying events dated before `weekOneStart`; it is not a main-grid column.

The clock-derived refresh date, calendar, raw MRP fact set, and computed schedule are one snapshot calculation. No frontend clock or local week derivation may alter a backend schedule.

### 3.2 Opening QOH

For every scoped `(domain, site, part)`, read exactly:

```text
OpeningQoh = COALESCE(SUM(ld_det.ld_qty_oh), 0)
```

The dedicated query groups at domain/site/part and uses only these filters:

- `ld_domain = @Domain`, `ld_site = @Site`, and the parameterized component scope;
- `UPPER(ld_status) <> 'MRB'`;
- `UPPER(ld_lot) NOT LIKE 'RMA%'`;
- `UPPER(ld_lot) NOT LIKE 'RA%'`.

This intentionally includes non-null `Stock`, `TRAN`, `INSPECT`, `NCMINSP`, and every other non-MRB status. It intentionally has no `in_mstr`, allocation, positive-quantity, date, expiration, location, status-master, or Stage 9 predicate/join. SQL null predicate behavior excludes a null status or null lot. The validated A107 observation is `12 + 16 + 1 + 3 = 32`, matching the QAD MRP Summary header QOH of `32`.

### 3.3 Raw MRP source facts

For each scoped part, query parameterized raw `mrp_det` rows at domain/site/component grain when either relevant event date is before `@HorizonEnd`:

```text
(mrp_due_date < @HorizonEnd OR mrp_rel_date < @HorizonEnd)
```

There is no arbitrary historical lower date bound: prior events are necessary for QAD Past. SQL must neither pivot nor aggregate/deduplicate/round/convert quantities or signs, and must not filter to guessed `mrp_dataset` values. Preserve every returned raw source row. The unproven physical QAD primary key is not an implementation blocker because this design never deduplicates, collapses, or eliminates source rows.

Classify outside SQL, retaining original type/date/quantity and raw identity:

| Raw condition | Timing | Schedule effect |
|---|---|---|
| `UPPER(mrp_type) LIKE 'DEMAND%'` | `mrp_due_date` | Gross Requirements |
| `UPPER(mrp_type) = 'SUPPLY'` | `mrp_due_date` | Scheduled Receipt |
| `UPPER(mrp_type) = 'SUPPLYP'` | `mrp_due_date` | Planned Orders Due |
| `UPPER(mrp_type) = 'SUPPLYP'` | `mrp_rel_date` | Planned Orders Release evidence only |

Rows outside those categories remain raw evidence if returned but do not acquire an inferred schedule meaning. A `SUPPLYP` quantity contributes once, on its due date; its release date/quantity is evidence only and must never become a second receipt.

### 3.4 Netting and identity rules

Aggregate only after raw classification. For each component:

```text
PastProjectedQoh =
    OpeningQoh
    + Past Scheduled Receipts
    + Past Planned Orders Due
    - Past Gross Requirements

WeekProjectedQoh =
    PriorProjectedQoh
    + Week Scheduled Receipts
    + Week Planned Orders Due
    - Week Gross Requirements
```

No calculation rounding is allowed. The engine retains decimals exactly as supplied by QAD; whole-number QAD display observations are presentation evidence only.

No-double-counting rules:

- One `(domain, site, component)` has one opening QOH and one schedule, regardless of BOM occurrences or workspace parents.
- One raw MRP source row can contribute to at most one due-date schedule category and bucket. `SUPPLYP` release evidence is linked to the same row but contributes zero to balance.
- Multiple workspace parents selecting the same component preserve attribution but never multiply QOH, raw rows, or balances.
- Raw facts are deterministically ordered by their returned source fields, with an application-generated evidence ordinal for stable UI/export identity where needed. The ordinal is a presentation/evidence identifier, not a QAD primary key and not a deduplication key.
- No inferred duplicate suppression is permitted. Equal-looking rows remain separate raw facts and separate calculation inputs.
- Stage 9 hard allocations, Stage 10 PO lines/KSS, WOD demand, forecasts, and old custom inputs do not join the shared schedule.

## 4. Layered Build Design

### 4.1 Domain and application

Add focused scheduling concepts: Monday calendar/range, raw MRP fact, classified event, Past plus 24 forward bucket schedule, and projected-balance result. Keep QAD table names, columns, and SQL out of the domain. The builder accepts a direct opening QOH and raw/classified input facts, preserves evidence references, and is responsible only for classification, bucketing, arithmetic, and the no-double-count rule.

The application service validates the workspace and exact MPS snapshot before and after source retrieval. It obtains current-effective BOM occurrences for resolved parents, records component-to-parent attribution, creates the distinct component scope, obtains QOH and raw MRP facts, assigns deterministic evidence ordinals after retrieval, and invokes the domain builder once per component. Stage 11-A filters may decide which selected components are displayed, but may not alter source scope or balance arithmetic.

### 4.2 Integration

Create separate read-only readers or one composed reader with independently testable query builders for:

- direct `ld_det` opening QOH;
- raw `mrp_det` schedule facts;
- existing BOM/component presentation facts only where an accepted UI field requires them.

All scope parts are bounded parameter values in a `VALUES` CTE/batching pattern. Resolve the QAD domain at the integration boundary. Use `CommandDefinition`, configured timeout, cancellation token, and no interpolated source values. The raw MRP reader returns row-oriented facts, not a pivot or weekly aggregate. It must select only the evidence fields needed to retain identity, type, due/release dates, and unmodified quantity.

### 4.3 API, UI, detail, and export

The Stage 11-A response is a projection for one workspace/snapshot/cache identity. It must include refresh metadata, one component row per site-wide schedule, the calculated Past bucket, exactly 24 Monday-start week descriptors/balances, resolved safety-stock state/value, first-short date, and raw/classified evidence with deterministic evidence ordinals adequate for detail and export. Endpoint status behavior remains consistent with existing workspace surfaces: invalid snapshot input `400`, absent workspace `404`, missing/superseded MPS `409`, and initial source failure `503`.

The visible UI name is `Workspace Shortages`. It remains separate from locked Stage 9 Shortages and Stage 10 Component Orders. It must provide:

- fixed component metadata columns plus a horizontally scrollable 24-column Week 1-24 grid labelled with Monday starts;
- a non-grid Past summary/detail indication, never a hidden arithmetic input;
- Include Manufactured Parts and Include Phantoms component-population options, each defaulting off and included in the cache/snapshot identity;
- frontend-local text filters for component and planner, nonblank filters for KSS and QAD status, and frontend-local Show All. Filters act on loaded rows and do not re-net data;
- current-week Critical Short part cue in muted red, Safety Stock Short part cue in light umber, and subdued-red treatment for every negative balance cell. Color is supplementary to textual/programmatic state;
- a detail modal that exposes raw decimals, Opening QOH, Past totals/projected QOH, each weekly category and projected balance, and Planned Orders Release evidence;
- accessible table headings, horizontal-scroll instructions, non-color textual state, modal label/focus trap/Escape/focus return, and stale-result alert semantics.

Severity, first-short date, default list inclusion, and list ordering evaluate only the 24 displayed Monday-start weeks:

```text
Critical Short: balance < 0
Safety Stock Short: balance >= 0 and balance < resolved safety stock
```

Use the settled selected-site safety-stock precedence: a non-null selected-site value, including zero, wins; only an absent selected-site row falls back to master; a present selected-site row with a null value remains explicit unresolved data and never falls back or becomes zero. A Past-only short that recovers by Week 1 is carry-in evidence only and does not create a current `Workspace Shortages` alert.

Export is generated only from the compatible cached projection and currently displayed/filtered rows. Its normalized filename is `<WorkspaceName>-Shortages-<date>.xlsx`. Preserve the approved legacy metadata, first-short date, QAD status, manufacturer-item context, and PO/KSS context, then add the re-baselined Past/MRP evidence and balance columns. Manufacturer-item and PO/KSS context may come from a presentation-only reader and must never alter the MRP balance. Apply subdued-red formatting to negative balance cells in the workbook as in the application. Raw calculation/evidence values remain unaltered.

### 4.4 Cache, snapshot, and stale behavior

Cache identity must include `(workspace assignment, MPS snapshot ID, refresh date, Stage 11 schedule version, accepted population options that affect component scope)`. Cache the finished immutable projection plus evidence, not mutable query rows.

- A successful load atomically replaces the exact compatible entry.
- A source-read failure may return only the latest successful entry with the same workspace, snapshot, schedule version, and scope options, marked `isStale` with its original refresh date and a visible warning.
- An initial failure is unavailable, not an empty schedule, and cannot export.
- A superseding MPS snapshot invalidates previous Stage 11-A results; never serve an older snapshot as stale.
- A cache hit for the current identity must not re-query QAD. Export never refreshes/re-queries.

## 5. Deterministic Test Plan

No test reads QAD. Fixtures must be raw deterministic facts with full decimal precision and source identities.

| Area | Required coverage |
|---|---|
| Calendar | Monday start for every weekday, exactly 24 displayed weeks, Week 25 exclusive boundary, and Past before Week 1. |
| Opening QOH | Direct-aggregate fixture includes Stock/TRAN/INSPECT/NCMINSP and excludes MRB/RMA/RA/null status/null lot by SQL behavior; prove A107 opening QOH `32`. Assert absence of `in_mstr`, allocation, positive-quantity, expiry/date, and Stage 9 joins in query-shape tests. |
| Raw MRP reader | Parameter/domain/site/component scope; no write verbs; either-date-before-horizon selection; no assumed dataset filter; every raw row returned with deterministic ordering and application evidence ordinal; no SQL pivot/group/round/UOM/sign conversion/deduplication. |
| Classification/netting | Demand/SUPPLY/SUPPLYP category rules; Past arithmetic; sequential weekly arithmetic; planned due contributes once; planned release is evidence only; equal-looking raw rows are never collapsed; raw rows outside recognized categories do not get inferred meaning. |
| Fixture `145MF3010` | Opening QOH `200`; past `wod_det`-origin demand raw `98.2456140337` due `2026-09-16`; whole-number display shows Gross Requirements `98` and Past/Week-1 projected QOH `102`, while calculations preserve the decimal value. |
| Fixture `A107-003A-ECOAT` | Opening `32`; Past Scheduled Receipt `232`; Past Gross Requirements `99`; Past Projected QOH `165`; later weekly roll-forward ends at `64`. |
| Fixture `148721-9` | Opening `0`; Past Planned Orders Release `49` retained as evidence and excluded from balance; Week-1 demand `5` plus planned due `49` yields projected QOH `44`. |
| Identity/attribution | Same component selected by multiple BOM parents creates one schedule; attribution preserves all parents; deterministic evidence ordinal is stable for UI/export without asserting a physical QAD key; source facts are never silently deduplicated; one `SUPPLYP` row cannot create due plus release supply. |
| Service/cache/API | Snapshot validation before/after read; exact identity cache hit; compatible stale-last-good; initial unavailable; superseding snapshot rejection; cache-only export; DTO/OpenAPI/generated-TypeScript synchronization. |
| UI/export/accessibility | `Workspace Shortages` has fixed metadata plus 24 Monday columns, Past visible only in detail/export, default-off Include Manufactured/Phantom options, component/planner text filters, nonblank KSS/QAD-status filters, frontend-local Show All, stale/initial-unavailable states, Critical/Safety/negative cues, and keyboard modal behavior. Assert only displayed/filtered rows export to the normalized filename with legacy metadata, first-short/status/manufacturer-item/PO-KSS context, Past/raw MRP evidence, and subdued-red negative cells. |

## 6. Delivery Sequence and Removal Gate

1. Add domain calendar/raw-event/schedule records and deterministic builder fixtures, without changing the active Stage 11-A endpoint.
2. Add independent QOH and raw-MRP readers plus SQL-shape tests. Preserve every source row; apply deterministic ordering and an application evidence ordinal where UI/export identity needs one, without asserting or requiring a physical QAD key.
3. Add application orchestration, cache identity/stale behavior, and contract/API tests. Regenerate OpenAPI and TypeScript.
4. Replace the Stage 11-A workspace UI, detail, and export against the shared projection. Perform focused accessibility and workbook tests.
5. Run documented backend/frontend/Rust verification and owner-guided desktop validation.
6. Only after the replacement passes and is owner-reviewed, remove the obsolete custom schedule code and its stale tests/contracts in a dedicated cleanup review.

## 7. Limitations and Explicit Boundary

- Stage 11-A is not a change to QAD MRP. It displays a KST calculation derived from raw QAD facts and must preserve traceability.
- This plan does not infer meaning for other `mrp_type` values, assert an unproven QAD physical primary key, establish reservation/pegging, consume forecasts, perform UOM conversion, or adopt custom PO/WO/allocation calculations.
- The historic `component_mrp.py` is reference evidence only. Its dynamic SQL pivot, direct legacy connection, SQL-side rounding, Sunday/date formula assumptions, and Excel formula projection are obsolete custom implementation details and must not be copied.
- Stage 11-B remains out of scope. Reuse is limited to the shared engine after a separate Stage 11-B plan defines its selection workflow, API, UI, permission/snapshot behavior, export needs, and acceptance tests.

## 8. Production Go/No-Go

**Build complete; owner go/no-go pending.** The authorized shared-MRP replacement has been implemented and verified by automated backend, contract, frontend, and Rust checks. Project-owner manual desktop validation and explicit acceptance remain required before this checkpoint is marked accepted. The historical Stage 11-A checkpoint plan and build prompt remain non-executable.

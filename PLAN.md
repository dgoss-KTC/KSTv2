# Stage 8D.1 Plan — Shared Inventory Capability (Site + Part)

> **Approved with owner amendments (applied):**
> 1. Shared model naming makes Net QOH explicit: `NetQuantityOnHand` / `NonNetQuantityOnHand` /
>    `RmaQuantityOnHand` (no ambiguous `QuantityOnHand`). Stage 6's API/frontend contract
>    (`quantityOnHand` etc.) is unchanged and continues to receive the `NetQuantityOnHand` value.
> 2. The batch reader normalizes/deduplicates part-number lookup keys in **C#** before SQL batching
>    (no reliance on SQL DISTINCT); a repeated requested part yields one summary; focused tests added.
> 3. The Application delegate bridge (`IPartInventoryReader` / `DelegatePartInventoryReader`) and
>    `Program.cs` DI wiring are **deferred to Stage 8D.3** (first Application consumer). 8D.1 file
>    scope: 3 files added (Domain record, QAD reader, QAD reader tests), 2 files modified
>    (`QadPartDetailReader`, its tests). No `Program.cs` change.

**Scope:** Extract the accepted Stage 6 inventory classification (Net QOH / Non-Net QOH / RMA) out of the
single-part `QadPartDetailReader` into one reusable, batch-capable `Site + Part` capability that Stage 6
continues to consume with **exactly** the current behavior, and that Stage 8 (BOM / Component Info) can
consume later. Planning-only pass; no implementation yet.

---

## A. Confirmed Current Inventory Implementation

All paths verified against the current working tree (clean; Stage 7 committed at `9afcb00`; only
untracked: `AGENTS.md`, Local Agent Addendum, `docs/prompts/`).

Owner of the Stage 6 inventory calculation:
`src/backend/Kst.Integrations.Qad/PartDetail/QadPartDetailReader.cs`

| Concern | Location |
|---|---|
| Query construction | `QadPartDetailReader.BuildInventoryQuery(domain, site, partNumber)` (public static, pure). `ld_det` **INNER JOIN** `loc_mstr` (domain+site+loc) **INNER JOIN** `is_mstr` (domain + location status); `WHERE ld_domain=@Domain AND ld_site=@Site AND ld_part=@Part AND ld.ld_qty_oh > 0`; three `ISNULL(SUM(CASE ...), 0)` totals: `QuantityOnHand` (non-RMA, `is_nettable = 1`), `QuantityNonNet` (non-RMA, `is_nettable = 0`), `RmaOnHand` (`ld_lot LIKE 'RA%'`). RMA classification happens in the SELECT CASE, not WHERE (dedicated test). |
| Aggregation / execution | `QadPartDetailReader.ReadAsync` — 2nd of 3 queries on **one** `QadConnectionFactory.OpenAsync` connection, via `CommandDefinition(commandTimeout: _options.CommandTimeoutSeconds, cancellationToken)`, `QuerySingleOrDefaultAsync<QadPartInventoryRawRow>`. Domain resolved from site via `QadSiteDomainMap.Resolve(site)` at the integration boundary. |
| Raw row | `QadPartInventoryRawRow` (QAD-shaped record, same file; does not cross the integration boundary). |
| PartDetail mapping | `QadPartDetailReader.Normalize(part, inventory, priceRows)` → `Kst.Domain/PartDetail/PartDetailSourceFacts.cs` (`inventory?.X ?? 0m` zero semantics) → `PartDetailService.GetPartDetailAsync` → `PartDetail` (Application) → API DTO `quantityOnHand` / `quantityNonNet` / `quantityRmaOnHand` (OpenAPI + generated TS). |
| Cache | `InMemoryPartDetailCacheStore`, keyed `(WorkspaceId, ParentPart)` tagged with MPS `SnapshotId` — untouched by this checkpoint. |

**Stage 8D.0 finding: CONFIRMED.** The inventory logic lives only inside `QadPartDetailReader`, is
single-part only (`ld_part = @Part`, no batch form), and is not independently reusable from any other
service.

Repository-evidence note (no action this checkpoint): the 8D.0 preflight **report** is not stored as a
repo artifact — only its prompt exists (`docs/prompts/Stage 8D.0 - Repository Preflight and Implementation
Plan.md`, untracked). Its key findings were re-verified directly against code in this pass, per the
starting-point instructions. Documentation reconciliation remains deferred.

## B. Smallest Safe Shared Extraction

New shared pieces (one authoritative implementation of the classification):

1. **Domain model** — `src/backend/Kst.Domain/Inventory/PartInventorySummary.cs`
   ```csharp
   public sealed record PartInventorySummary(
       string Site,
       string PartNumber,
       decimal NetQuantityOnHand,
       decimal NonNetQuantityOnHand,
       decimal RmaQuantityOnHand);
   ```
   Net QOH is explicit in the shared vocabulary (owner amendment); grain `Site + Part` made
   explicit on the record; no BOM/requirement/shortage concepts. Feature-based `Inventory` folder
   matches Domain's existing folder convention (`Mps`, `PartDetail`, `WorkOrders`, ...).
   Stage 6's existing `quantityOnHand`/`quantityNonNet`/`quantityRmaOnHand` API/frontend names are
   unchanged and map from the Net/NonNet/Rma values respectively.

2. **QAD reader** — `src/backend/Kst.Integrations.Qad/Inventory/QadPartInventoryReader.cs`
   - `Task<IReadOnlyList<PartInventorySummary>> ReadSummariesAsync(string site, IReadOnlyList<string> partNumbers, CancellationToken ct)` — batch entry point (design in §C).
   - `static IReadOnlyList<string> NormalizePartNumbers(IReadOnlyList<string> partNumbers)` — pure C# lookup-key normalization (trim) + case-insensitive dedup, first occurrence wins, blank keys rejected (owner amendment; focused tests).
   - `static (string Sql, DynamicParameters Parameters) BuildBatchQuery(string domain, string site, IReadOnlyList<string> partNumbers)` — public and pure so SQL shape is independently testable (same convention as `QadMpsSourceReader.BuildBatchQuery` / `QadPartDetailReader.Build*Query`).
   - `static PartInventorySummary Normalize(QadPartInventoryRawRow raw)` — raw → Domain mapping.
   - `QadPartInventoryRawRow` moves here (gains the `PartNumber` column the batch query must return).

3. **Application boundary — DEFERRED to Stage 8D.3 (owner amendment).** No
   `IPartInventoryReader` / `DelegatePartInventoryReader` / `Program.cs` DI wiring in 8D.1; the
   bridge will be added when the BOM application service becomes the first Application consumer.
   8D.1 establishes the reusable Domain + QAD capability and makes Stage 6 PartDetail consume it.

**How `QadPartDetailReader` reuses it (no behavior change):**
- `ReadAsync` keeps its single-connection, three-query structure. The inventory step becomes
  `QadPartInventoryReader.BuildBatchQuery(domain, site, [partNumber])` executed on the **same open
  connection** (a one-element scope still returns exactly one row → `QuerySingleOrDefaultAsync` retained).
- The raw row is mapped with the shared `Normalize`, and `QadPartDetailReader.Normalize` takes
  `PartInventorySummary?` instead of the local raw row (same `?? 0m` mapping).
- The private `BuildInventoryQuery` and the local raw-row record are deleted (moved, not duplicated).

**Why this is smaller/safer than the alternatives:**
- Vs. Stage 8 copying the SQL: one classification implementation instead of two diverging copies.
- Vs. `QadPartDetailReader` delegating inventory to `ReadSummariesAsync`: that would open a **second
  connection** (4 queries / 2 connections) for the live-validated Stage 6 flow. Sharing the pure
  builder + normalizer on the existing connection changes zero observable behavior (same query count,
  same connection lifetime, same timeout/cancellation wiring).
- Vs. a generic `MaterialService`/new pattern: rejected by scope; everything here is reuse of the
  established adapter-bridge, `MpsPartBatcher`, `QadConnectionOptions`, and `CommandDefinition`
  conventions. No new architecture.
- Vs. placing the model in `Kst.Domain/PartDetail`: the grain is Site+Part and is shared across
  features; a PartDetail namespace would mislabel it.

## C. Batch Retrieval Design

- **Grain:** input = selected workspace **Site** + collection of part numbers; Domain is resolved
  internally via `QadSiteDomainMap` (callers and frontend never supply QAD Domain — same as Stage 5/6).
  Output = one summary per **distinct** requested part: `Site, PartNumber, QuantityOnHand,
  QuantityNonNet, QuantityRmaOnHand`.
- **SQL/Dapper strategy (no final SQL in this pass):**
  `WITH ScopeParts (PartNumber) AS (SELECT ... FROM (VALUES (@Part0), (@Part1), ...) AS Parts (PartNumber))`
  → an inner aggregate CTE containing the **Stage 6 join/WHERE/CASE body verbatim**
  (`ld_det` INNER JOIN `loc_mstr` INNER JOIN `is_mstr`; `ld_domain = @Domain AND ld_site = @Site AND
  ld.ld_qty_oh > 0`, part restricted to the scope) with `GROUP BY ld.ld_part` → outer
  `SELECT scope.PartNumber, @Site AS Site, ISNULL(inv.X, 0) ...` from `ScopeParts LEFT JOIN` the
  aggregate. Using `@Site` for the Site column avoids inventing any site-case normalization.
  RMA stays in the SELECT CASE. No `DISTINCT`, no defensive dedup/fallback; the outer GROUP BY is the
  grain itself (repeated input parts collapse to one row). SQL Server 2016-compatible constructs only
  (CTE, VALUES constructor, LEFT JOIN — all already used by Stages 5/6).
- **Parameterization:** `@Domain`, `@Site`, one `@Part{i}` per part — the exact MPS convention; no
  string concatenation (injection tests carry over).
- **Zero-row handling:** guaranteed **in SQL** by the outer `LEFT JOIN` + `ISNULL`: a requested part
  with no qualifying rows still receives `0/0/0`. Callers never infer zero from a missing row.
- **Aggregation:** `SUM` per part; duplicate source/location rows aggregate exactly as Stage 6 does
  (same CASE expressions).
- **Chunking:** inside the QAD reader, exactly where MPS does it —
  `MpsPartBatcher.Batch(parts, _options.MaxPartBatchSize)` (pure Domain chunker; default 500 from
  `QadConnectionOptions.MaxPartBatchSize`). One connection, one query per batch, per-batch structured
  log (MPS style). 500 parts → 502 parameters, well under SQL Server's 2100 limit; no invented
  additional chunking. Empty input → empty result, no connection opened (MPS behavior).
- **Cancellation/timeout:** `QadConnectionFactory.OpenAsync(_options, ct)` once; per batch,
  `CommandDefinition(commandTimeout: _options.CommandTimeoutSeconds, cancellationToken: ct)`. A QAD or
  query failure propagates as a real exception — never converted to zeroes.

## D. Stage 6 Compatibility Plan

**Will not change:**
- All three PartDetail queries still run on one connection; part-master and price query text unchanged.
- Stage 6 Net QOH / Non-Net QOH / RMA values, zero semantics, missing-part behavior
  (no `pt_mstr` → 404 "Part not found"), query-failure → stale-last-good fallback,
  cache keying/invalidation (`(WorkspaceId, ParentPart)` + MPS `SnapshotId`),
  PartDetail API DTO shape, OpenAPI, generated TypeScript, frontend, and all
  planner/lead-time/component-count/WIP logic.

**Will change (internal only):**
- The inventory query text becomes the shared batch form with a 1-element scope (VALUES CTE); joins,
  WHERE, CASE expressions, and `ISNULL(..., 0)` semantics are preserved verbatim; still exactly one
  result row for one part.
- `QadPartDetailReader.Normalize` takes `PartInventorySummary?`; `QuantityOnHand` is mapped from
  `NetQuantityOnHand` (Stage 6 DTO/OpenAPI/frontend names unchanged).
- `QadPartInventoryRawRow` record relocates to the new inventory reader file (Net/NonNet/Rma naming).

**Existing tests that protect the behavior:**
- `tests/Kst.Integrations.Qad.Tests/PartDetail/QadPartDetailReaderTests.cs` — inventory SQL shape
  (positive-only, loc/is joins, CASE split, RMA-in-SELECT-not-WHERE, no concatenation) and Normalize
  zero/mapping.
- `tests/Kst.Application.Tests/PartDetail/PartDetailServiceTests.cs` — cache/stale/missing/out-of-scope.
- `tests/Kst.Api.IntegrationTests/PartDetailEndpointTests.cs` — API contract.
- Frontend `PartInfoPanel`/`MpsWorkspace` tests — untouched (no frontend changes expected; if any
  frontend change were needed it would be reported as a conflict).

**Additional regression tests:** the 4 `BuildInventoryQuery_*` tests adapt to assert the shared
`BuildBatchQuery` with a 1-part scope (same assertions, re-pointed — adapted, not rewritten); the 2
Normalize tests adapt to `PartInventorySummary`. New `QadPartInventoryReaderTests` (see §E).

## E. Exact Implementation File Plan

**Add (3, per owner amendment):**
1. `src/backend/Kst.Domain/Inventory/PartInventorySummary.cs`
2. `src/backend/Kst.Integrations.Qad/Inventory/QadPartInventoryReader.cs`
3. `src/backend/tests/Kst.Integrations.Qad.Tests/Inventory/QadPartInventoryReaderTests.cs`
   - batch SQL shape: one `@Part{i}` per part + `@Domain`/`@Site`; no concatenation; positive-only
     filter in the aggregate WHERE; loc_mstr/is_mstr joins; net/non-net/RMA CASE split; RMA in SELECT
     not WHERE; `GROUP BY` part; outer `LEFT JOIN` + `ISNULL` zero-fill (one row per requested part);
     `Normalize` mapping (raw row → summary).
   - C# lookup-key normalization/dedup (owner amendment): trim + case-insensitive dedup, first
     occurrence wins, blank keys rejected — a repeated requested part produces one summary.
   - Covers plan test items: 2–6 (classification/isolation/mixed/non-positive exclusion via the verbatim
     CASE + WHERE assertions), 7/10 (zero-fill structure), 8 (per-part parameters + GROUP BY), 9
     (aggregation via GROUP BY part + SUM). Final numeric proof for multi-part batches comes from the
     live read-only validation (§F), consistent with the repo convention that QAD integration tests
     exercise pure SQL-building + Normalize without a database.

**Modify (2, per owner amendment):**
4. `src/backend/Kst.Integrations.Qad/PartDetail/QadPartDetailReader.cs` — reuse shared builder/normalizer
   on the existing connection; delete `BuildInventoryQuery` + local raw record; `Normalize` signature
   change only (maps `NetQuantityOnHand` → Stage 6 `QuantityOnHand`).
5. `src/backend/tests/Kst.Integrations.Qad.Tests/PartDetail/QadPartDetailReaderTests.cs` — the 4
   inventory SQL-shape tests move to `QadPartInventoryReaderTests` (the builder's new home, asserted
   at 1-part scope for Stage 6 equivalence); the 2 Normalize tests adapt to `PartInventorySummary`.

**Explicitly not touched:** `Kst.Api/Program.cs` (bridge deferred to 8D.3), `PartDetailSourceFacts`,
`PartDetailService`, `PartDetail`/`PartDetailResult`, `IPartDetailCacheStore`/`InMemoryPartDetailCacheStore`,
Kst.Api endpoints/DTOs, OpenAPI spec, generated TypeScript, all frontend code, MPS/WorkOrders/Stage 7
code, `Kst.Domain/Mps/MpsPartBatcher.cs` (reused as-is; no rename even though it lives in the `Mps`
namespace — renaming is unrelated churn).

**Implementation order:** 1 → 2 → 4 → 3 → 5 (domain record, QAD reader, PartDetail repoint, tests
adapted/added) → focused verification → full verification.

## F. Verification Plan

Focused first (from `src/backend`):
```text
dotnet test tests/Kst.Integrations.Qad.Tests --nologo
dotnet test tests/Kst.Application.Tests --nologo
dotnet test tests/Kst.Api.IntegrationTests --nologo
```
Full backend regression before declaring 8D.1 complete:
```text
dotnet format Kst.slnx --verify-no-changes
dotnet build Kst.slnx --nologo
dotnet test Kst.slnx --nologo        # includes Kst.ArchitectureTests
```
Contracts/frontend: no DTO/endpoint changes → no OpenAPI regeneration, no generated-TS edits, no
frontend changes. Confirm `docs/openapi/Kst.Api.json` and `src/frontend/src/generated/api.ts` are
byte-identical (PartDetail schema unchanged).

Live read-only validation (small; after `scripts/build-sidecar.ps1` since Tauri hosts the backend):
1. **Baseline first (pre-implementation):** pick ~4–5 familiar parts — one with Net QOH, one with
   Non-Net QOH, one with RMA if readily available, one with zero inventory, one mixed — and record
   their current Stage 6 Part Info `quantityOnHand` / `quantityNonNet` / `quantityRmaOnHand`.
2. **Post-implementation:** the same parts via Stage 6 Part Info must match the baseline exactly.
3. **Batch check:** run the new shared batch SQL (from `BuildBatchQuery` for those parts) as a
   read-only query against QAD; per-part totals must equal each part's Stage 6 value and the direct
   Stage 6 single-part aggregation.
No production data is altered to create cases.

## G. Risks / Owner Decisions

1. ~~Application boundary in 8D.1 vs 8D.3~~ — **resolved by owner: deferred to 8D.3.**
2. The single-part Stage 6 query text changes to the shared 1-element batch form — provably equivalent
   (same joins/WHERE/CASE/zero semantics), covered by adapted tests + live comparison; flagged because
   it is a live-validated query.
3. Parameter budget: 500-part batch → 502 parameters vs. SQL Server's 2100 limit — same exposure as
   the existing MPS batcher; no action.
4. No other owner decisions are required by repository evidence.

## H. Stop Confirmation

- No production files changed (this pass is read-only; only this plan file was written).
- No tests changed.
- No OpenAPI / generated TypeScript / frontend changes.
- No commits created or pushed.
- **Ready for human approval before implementation.**

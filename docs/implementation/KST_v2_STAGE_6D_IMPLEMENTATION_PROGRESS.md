# KST v2 — Stage 6D Implementation Progress

**Stage:** 6D — Part Information Drill-Down implementation
**Status:** IMPLEMENTATION + AUTOMATED/LIVE VALIDATION COMPLETE — pending project-owner review/acceptance
**Contract:** `docs/implementation/KST_v2_STAGE_6_PART_INFO_CONTRACT.md` (ACCEPTED)

## 1. Repository preflight findings (Stage 6D.0)

### Solution / project structure
`src/backend/Kst.slnx`: `Kst.Domain` → `Kst.Application` → (`Kst.Infrastructure`, `Kst.Integrations.Qad`, `Kst.Integrations.Shortages`, `Kst.Exports`) → `Kst.Api`. Enforced by `Kst.ArchitectureTests/DependencyRuleTests.cs` (Domain/Application must not reference ASP.NET Core/SqlClient/Dapper/Integrations; Integrations must not reference Api).

### Stage 5 MPS pattern (directly reused for Stage 6)
- **Snapshot identity**: `Kst.Application.Mps.MpsSnapshot.Id` is a `Kst.Domain.Common.SnapshotId` (`SnapshotId.New()` on every successful load). `IMpsSnapshotStore.GetState(workspaceId)` returns `MpsWorkspaceState { Snapshot, Status, ... }` without triggering any load — this is the read used by Stage 6 to detect "MPS not loaded" (409) and to read the current snapshot identity, *without* invoking `MpsWorkspaceSnapshotService.GetDashboardAsync` (which would auto-load MPS — explicitly forbidden for PartDetail).
- **QAD adapter bridging pattern** (avoids `Kst.Application` → `Kst.Integrations.Qad` reference, required by `Kst.ArchitectureTests`): interface (e.g. `IMpsSourceReader`) + `Delegate*` Func-adapter live in `Kst.Application`; concrete class (e.g. `QadMpsSourceReader`) lives in `Kst.Integrations.Qad` with **no** interface reference back; `Program.cs` (composition root) wires `services.AddSingleton<IMpsSourceReader>(sp => new DelegateMpsSourceReader((..) => sp.GetRequiredService<QadMpsSourceReader>().ReadAsync(..)))`. Stage 6 replicates this exactly for `IPartDetailSourceReader` / `DelegatePartDetailSourceReader` / `QadPartDetailReader`.
- **QAD connection/read-only conventions**: `Kst.Integrations.Qad.QadConnectionFactory.OpenAsync(options, ct)` opens the connection and immediately issues `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;`. All QAD reads go through Dapper `CommandDefinition` with `commandTimeout: options.CommandTimeoutSeconds` and parameterized SQL (never string-concatenated part/site/domain values). Domain is inferred from Site via `QadSiteDomainMap.Resolve(site)` — never supplied by the caller/frontend.
- **Cache/store pattern**: `IMpsSnapshotStore` (Application interface) + `InMemoryMpsSnapshotStore` (Infrastructure, `ConcurrentDictionary`-backed, no persistence across process restart). Stage 6 replicates this exactly for `IPartDetailCacheStore` / `InMemoryPartDetailCacheStore`.
- **API conventions**: minimal-API static endpoint classes in `Kst.Api/Endpoints/*.cs` (`MapXxxEndpoints(this IEndpointRouteBuilder)`), DTOs as flat `sealed record`s in `Kst.Api/Dtos/*.cs`, workspace-not-found mapped via a dedicated Application exception caught in the endpoint handler (`MpsWorkspaceNotFoundException` → `Results.NotFound()`), other outcomes mapped from a result/outcome object returned by the application service, `Results.Problem(title, detail, statusCode)` for Problem Details, no raw exception/SQL text ever placed in a response.
- **Test conventions**: QAD integration tests (`Kst.Integrations.Qad.Tests`) test only the *pure/static* SQL-building and raw→normalized mapping methods (e.g. `QadMpsSourceReader.BuildBatchQuery`, `.Normalize`) — no real database is used in the automated suite; live QAD validation is a separate manual step. Application-layer tests use `Fake`/`Delegate` seams (`FakeWorkspaceConfigurationService`, `DelegateMpsSourceReader`, `InMemoryMpsSnapshotStore`) with zero real I/O. API integration tests use `KstApiFactory : WebApplicationFactory<Program>` with QAD forced "not configured" via env vars, in-memory workspace/preferences stores.

### QAD schema facts confirmed live (read-only `INFORMATION_SCHEMA.COLUMNS` query against `KNWVM13/QADPRO2`, 2026-08-10)
- `pt_mstr.pt_mfg_lead`, `pt_sfty_time`, `pt_sfty_stk` are all **`decimal(28,0)`** (not integer/smallint) — mapped to nullable C# `decimal?`, not `int?`, per the contract's "do not truncate source precision" rule. (**Note, added during R0.6 correction:** the accepted, implemented query sources these three PartDetail fields from the equivalently-typed selected-site `ptp_det.ptp_mfg_lead`/`ptp_sfty_tme`/`ptp_sfty_stk` columns, not `pt_mstr`'s copies — see `KST_v2_STAGE_6_PART_INFO_CONTRACT.md` §4 and `QadPartDetailReader.BuildPartMasterQuery`. This preflight note recorded the `pt_mstr` column types checked at the time; it does not describe final field sourcing.)
- `pt_buyer`, `pt_status`, `pt_rev`, `pt_desc1`, `pt_warr_cd`, `pt_part`, `pt_domain` are all `nvarchar` — mapped to `string?`/`string`.
- `ld_det.ld_qty_oh` is **`decimal(28,10)`** (fractional-capable) — mapped to `decimal`, summed in SQL with `ISNULL(SUM(...), 0)`.
- `ld_det` join to `loc_mstr`: `loc_domain`/`loc_site`/`loc_loc` (all present, `nvarchar`) — confirms `ld_domain = loc_domain AND ld_site = loc_site AND ld_loc = loc_loc`.
- `loc_mstr` join to `is_mstr`: `is_domain`/`is_status` (both `nvarchar`) match `loc_mstr.loc_domain`/`loc_status`; `is_mstr.is_nettable` is `bit` — this is the nettable/non-nettable classification switch.
- `pi_mstr.pi_domain`/`pi_part_code`/`pi_list_id` are `nvarchar`, `pi_start` is `datetime`.
- `pid_det.pid_qty`/`pid_amt` are **`decimal(28,10)`** — mapped to `decimal`.

## 2. Contract-to-repository naming/placement adjustments (semantics unchanged)

1. **Route parameter name**: contract text shows `/api/v1/workspaces/{workspaceId}/part-detail`; implemented as `{assignmentId:guid}` to match the existing `WorkspaceEndpoints`/`MpsEndpoints` route-parameter naming convention. The actual runtime URL shape is identical (placeholder names never appear in the HTTP path).
2. **`PartDetail` placement**: contract section 9 calls this the "Domain model", but because it carries orchestration/cache metadata (`LoadedAtUtc`, `IsStale`, `Warning`) — a concern the repo consistently keeps in `Kst.Application.*` (e.g. `MpsSnapshot`/`MpsWorkspaceState`/`MpsDashboardResult`), not `Kst.Domain.*` — the final composed `PartDetail` record lives in `Kst.Application.PartDetail`. Pure business-only pieces (`PartPriceBreak`, `PartStatusMap`, and the raw QAD-crossing `PartDetailSourceFacts`) live in `Kst.Domain.PartDetail`, mirroring how `MpsPartSchedule`/`MpsBucket` (pure) sit in `Kst.Domain.Mps` while `MpsSnapshot` (cache-state) sits in `Kst.Application.Mps`.
3. **`PartDetailSourceFacts`**: a single flat crossing-boundary record (part master fields + `QuantityOnHand`/`QuantityNonNet` + `PriceBreaks[]`) analogous to `MpsSourceRow`. This is *not* one of the contract's explicitly-named speculative submodels (`PartAttributes`/`PartPlanningParameters`/`PartInventorySummary`) — it is the minimum mechanical wrapper needed to cross the `Kst.Integrations.Qad` → `Kst.Application` boundary (same reason `MpsSourceRow` exists), and it is never exposed past the Application layer.

## 3. Files created/changed (final)

### Backend
- `Kst.Domain/PartDetail/PartPriceBreak.cs`
- `Kst.Domain/PartDetail/PartDetailSourceFacts.cs`
- `Kst.Domain/PartDetail/PartStatusMap.cs`
- `Kst.Application/PartDetail/IPartDetailSourceReader.cs`
- `Kst.Application/PartDetail/DelegatePartDetailSourceReader.cs`
- `Kst.Application/PartDetail/IPartDetailCacheStore.cs`
- `Kst.Application/PartDetail/PartDetailCacheEntry.cs`
- `Kst.Application/PartDetail/PartDetail.cs`
- `Kst.Application/PartDetail/PartDetailResult.cs`
- `Kst.Application/PartDetail/PartDetailWorkspaceNotFoundException.cs`
- `Kst.Application/PartDetail/PartDetailService.cs`
- `Kst.Infrastructure/PartDetail/InMemoryPartDetailCacheStore.cs`
- `Kst.Integrations.Qad/PartDetail/QadPartDetailReader.cs`
- `Kst.Api/Dtos/PartDetailDtos.cs`
- `Kst.Api/Endpoints/PartDetailEndpoints.cs`
- `Kst.Api/Program.cs` (DI wiring + endpoint mapping)
- Tests: `Kst.Domain.Tests/PartDetail/*`, `Kst.Application.Tests/PartDetail/*`, `Kst.Integrations.Qad.Tests/PartDetail/*`, `Kst.Api.IntegrationTests/PartDetailEndpointTests.cs`

### Frontend
- `src/api/client.ts` (add `PartDetailResponseDto`/`PartPriceBreakDto` type exports + `getPartDetail`)
- `src/api/partDetailApi.ts`
- `src/hooks/usePartDetail.ts`
- `src/components/PartInfoPanel.tsx` / `.css`
- `src/components/MpsWorkspace.tsx` / `.css` (parent-row selection state, reset-on-workspace-change effect, click/keyboard row activation, collapse-to-selected-parent rendering, `<PartInfoPanel>` integration with Back-to-full-grid)
- Tests: `src/components/PartInfoPanel.test.tsx` (new, 10 tests), `src/components/MpsWorkspace.test.tsx` (+5 tests: open panel on row click, collapse/restore via Back to full grid, missing-part message, error+retry, no extra part-detail fetch on date-basis change)

## 4. Cache/freshness behavior implemented

Identity: `(WorkspaceId, ParentPart)` → `PartDetailCacheEntry { LoadedAgainstMpsSnapshotId, Detail }`.

`PartDetailService.GetPartDetailAsync`:
1. Resolve workspace (404 if missing).
2. Read `IMpsSnapshotStore.GetState(workspaceId)` (read-only, never auto-loads MPS). No snapshot → 409.
3. Validate `partNumber` is in `snapshot.ResolvedParts` (case-insensitive) → 404 out-of-scope if not.
4. Cache hit: entry exists **and** `entry.LoadedAgainstMpsSnapshotId == snapshot.Id` → return cached `Detail` (fresh, `IsStale=false`).
5. Otherwise attempt a fresh QAD read:
   - Reader returns `null` → missing `pt_mstr` → 404 missing-part (independent of any cache).
   - Reader throws → if *any* prior cache entry exists (even against an older snapshot id), return it as stale last-good (200, `IsStale=true`, warning set); else 503 unavailable.
   - Reader succeeds → compose new `PartDetail`, overwrite cache keyed to the *current* snapshot id, return fresh (200).

This makes "failed MPS refresh preserves cache" and "successful refresh makes old detail stale-then-refreshed-or-stale-fallback" fall out naturally: a failed MPS refresh never changes `Snapshot.Id` (per `InMemoryMpsSnapshotStore.SetFailed`, which retains the prior good `Snapshot`), so the existing cache entry still matches; a successful refresh always creates a new `SnapshotId`, so the old cache entry no longer matches on next access and is only used as a stale-fallback if the fresh QAD read then fails.

## 5. Automated test results

- **Backend**: `dotnet build` (0 errors, 0 warnings), `dotnet format --verify-no-changes` clean, full solution test run **316/316 passing** (273 baseline + 43 new: `Kst.Domain.Tests` PartStatusMap, `Kst.Application.Tests` PartDetailService, `Kst.Integrations.Qad.Tests` QadPartDetailReader SQL-building/mapping/injection-safety, `Kst.Api.IntegrationTests` PartDetailEndpointTests).
- **OpenAPI/type generation**: `docs/openapi/Kst.Api.json` regenerated (contains `/api/v1/workspaces/{assignmentId}/part-detail`), `npm run generate:types` regenerated `src/generated/api.ts` (contains `PartDetailResponseDto`/`PartPriceBreakDto` schemas).
- **Frontend**: `npm run typecheck` clean, `npm run lint -- --max-warnings 0` clean, `npm run build` clean (production bundle built), full Vitest run **116/116 passing** (101 baseline + 15 new: 10 in `PartInfoPanel.test.tsx`, +5 in `MpsWorkspace.test.tsx` for row-click selection, collapse/restore, missing-part/error states, and confirming no extra `/part-detail` fetch on date-basis toggle).

## 6. Live QAD validation (2026, real `KNWVM13`/`QADPRO2`, Development environment, `dotnet run --no-build`)

Backend launched standalone (not via Tauri) and exercised directly over HTTP against the real dev workspace config, which now has **five** real workspaces: "Shure SMT" (SW/2140, 38 parts), "SHU Metals" (SW/2141, 4 parts), "SHU Molding" (SW/2142, 6 parts), "Taco" (18 parts), "MSA/Neutronics" (5 parts) \u2014 all site `SW` / domain `KTC` (no `KV`/`KTV`-domain workspace was available in this environment to exercise the alternate domain-mapping branch; `QadSiteDomainMap.Resolve` for `KV` remains covered only by the existing Stage 5A unit tests).

Cases exercised and confirmed:
- 404 unknown workspace id.
- 409 MPS not loaded (fresh workspace, no prior `/mps` call in this process).
- 400 blank `partNumber`.
- 200 happy path (`190A48838` under Shure SMT): planner `SMT1SSHU`, lead time 4 days, safety time 0, status `P`\u2192`PROTO`, blank current revision rendered as `""` (not null), description, IOS code `SHU`, safety stock 0, qty on hand/non-net 0/0, one price tier (2700 @ $2.50).
- 404 out-of-scope part (`ZZZ999999`, not in the workspace's resolved parent parts).
- Cache hit: repeating the same request returned an identical `loadedAtUtc` (no re-query against QAD).
- Multiple real Part Status codes observed: `C` (CURRENT), `P` (PROTO), `B` (BYPASS).
- Positive nettable inventory (`53A27586` under SHU Metals: qty on hand 13,132).
- Positive non-net inventory (`53A45822` under SHU Metals: 22; `65A16602` under SHU Molding: 3,810).
- Scanned all 71 resolved parts across all five real workspaces; every part in these live fixtures happened to have exactly one price tier and zero on-hand/non-net inventory otherwise \u2014 multi-tier pricing, the "no current price" (zero tiers) case, and an unknown/unmapped status code were **not** naturally reproducible against live data in this environment and remain covered only by the automated `QadPartDetailReaderTests`/`PartStatusMapTests` unit suites (as anticipated in the contract's \u00a718 wording that some cases are automated-test-only).
- Not exercised live: a QAD outage/stale-cache-fallback scenario (would require deliberately breaking the live connection mid-session) and the full Tauri desktop UI click-through (this validation was done directly over HTTP against the standalone sidecar, not through `tauri dev`) \u2014 both are recommended as a manual follow-up before final owner sign-off.

## 7. Known deviations from contract (all functionally equivalent, documented for owner review)

1. Route parameter named `assignmentId` instead of `workspaceId` (matches existing `WorkspaceEndpoints`/`MpsEndpoints` convention; URL shape unaffected).
2. `PartDetail` (the composed, cache-metadata-bearing record) placed in `Kst.Application.PartDetail` rather than `Kst.Domain`, mirroring `MpsSnapshot`'s existing placement; pure pieces (`PartPriceBreak`, `PartStatusMap`, `PartDetailSourceFacts`) remain in `Kst.Domain.PartDetail`.
3. Frontend `PartDetailApiError` is deliberately coarser than the API's HTTP status distinctions (`missing-part` vs. a single generic `error`), because workspace-not-found/MPS-not-loaded/out-of-scope are unreachable from the UI once a parent row is already selected from a loaded grid.

## 8. Owner-review UI refinement round (frontend-only, no contract/business-rule change)

Two layout/interaction issues found during owner review of the Stage 6D drill-down were corrected:

1. **Focused-grid whitespace**: `.mps-grid-frame` used `flex: 1` unconditionally, so it kept stretching to fill the workspace's remaining vertical space even when `parts` was already filtered down to the single selected row — leaving a large blank area between the selected row and the Part Info panel. Fixed by adding a `mps-grid-frame--focused` modifier class (applied to the frame only while `selectedParent` is set) with `flex: 0 1 auto`, so the frame hugs its header + single-row content instead of stretching. No change was needed to the row-filtering logic itself (`parts = selectedParent ? allParts.filter(...) : allParts`), which was already rendering only the selected row with no hidden placeholders.
2. **Parent-row toggle-to-close**: row selection is now a toggle. `handleParentRowSelect(partNumber)` clears the selection (via the same `clearPartSelection` helper used by the `Back to full grid` button) if the clicked/activated row is already selected, otherwise selects it. Both mouse click and Enter/Space keyboard activation call this same handler, so keyboard toggle-to-close matches mouse behavior. The explicit `Back to full grid` button is unchanged and still calls `clearPartSelection` directly.

No backend, contract, cache/lazy-load, or QAD business-rule code changed. `usePartDetail(assignmentId, selectedParent)` behavior is untouched: setting `selectedParent` back to `null` (via either the toggle-close or Back button) skips the network call entirely, per its existing `partNumber === null` short-circuit.

**Tests added/updated** (`src/components/MpsWorkspace.test.tsx`): extended the existing collapse/restore test with assertions on rendered `row` count (header rows + exactly one body row while focused) and the `mps-grid-frame--focused` class toggling on/off; added a new test for clicking the selected row again (Part Info closes, full grid restored, no extra `/part-detail` fetch); added a new test for keyboard (Enter) toggle-to-close; added a new test confirming `Back to full grid` still issues no extra `/part-detail` fetch. All prior Stage 6D tests preserved unmodified. Frontend suite: **119/119 passing** (was 116); lint/typecheck/build all clean; no backend changes, so no backend/OpenAPI/sidecar rebuild was performed.

**Manual Tauri verification**: ran `npx @tauri-apps/cli dev` against the real "SHU Molding" workspace (live QAD data) and captured window screenshots at each step to confirm visually: selecting a parent row collapses the grid to exactly that one row with normal spacing before Part Info (no blank area); clicking the same selected row again closes Part Info and restores the full 6-row grid. Confirmed via the project owner's own manual test pass as well. Full click-through of every item in the contract's manual-verification checklist (light/dark themes, density, Due/Release and horizon no-refetch behavior) was not re-screenshotted individually since none of that code was touched by this refinement.

Owner acceptance of the corrected UI is still pending explicit sign-off; this section documents the refinement, not a new acceptance milestone.


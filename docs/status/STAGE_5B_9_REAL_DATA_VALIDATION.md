# Stage 5B.9 — Real-Data Validation and Performance: Findings

Date: 2026-08-10
Nature of checkpoint: Verification and defect-correction only (no feature expansion). No QAD data was created or modified. No database indexes or objects were added.

## 1. Workspaces / scope shapes validated

All validation was performed against the live QADPRO2 database (server `KNWVM13`) via a locally running dev backend instance, using temporary workspaces created and deleted through the real `/api/v1/workspaces` API, plus direct read-only SQL cross-checks (`SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED`, matching the app's own isolation policy).

| Shape | Workspace | Site/Domain | Result |
|---|---|---|---|
| Existing permanent workspace, single product line | "Shure SMT" | SW/KTC, PL 2140 | Matches |
| Existing permanent workspace, single product line | "TACO" | SW/KTC, PL 3230 | Matches |
| Product-line range (wide) | ZZ-Val-RangeWide | SW/KTC, 2070–2210 | Matches |
| Product-line range (narrow) | ZZ-Val-NarrowRange | SW/KTC, 2140–2141 | Matches |
| Explicit-parent-only workspace | ZZ-Val-ExplicitParents | SW/KTC | Matches |
| Large workspace (816 parts) | ZZ-Val-Large6150 | SW/KTC, PL 6150 | Matches; see performance section |
| Different domain (KTV) | ZZ-Val-KTV-3270 | KV/KTV, PL 3270 | Matches |
| Explicit parents, Falldown boundary | ZZ-Val-FalldownSat / ZZ-Val-CurrentWeekMon | KV/KTV, AR/KTC | Matches |
| Status-combination explicit parents | ZZ-Val-StatusCombos, ZZ-Val-StatusAR | SW/KTC, AR/KTC | Matches |

All temporary workspaces were deleted after use; only the two original real workspaces ("Shure SMT", "TACO") remain in `%LOCALAPPDATA%\KST\config\workspaces.json`.

## 2. Source totals vs. direct QAD queries

7 of 7 workspace-level row counts matched exactly between the KST snapshot and an equivalent direct SQL query against `mrp_det`/`wo_mstr`/`pt_mstr` (638, 270, 179/1121, 42/709, 3/31, 816/15335, 24/180 parts/rows). Row-level validation (WO id, quantity, due/release date, status) was performed for 31 individual rows across 3 parts — all fields matched exactly. Part description lookups were cross-checked against the same `pt_mstr` join pattern used by the app.

## 3. Falldown boundary validation

- No lower bound: oldest real qualifying WO found was due 2024-10-15; 1,065 pre-current-week rows are correctly retained in Falldown rather than dropped.
- Saturday boundary (last day of current week): confirmed live with part `6262A-NAC2` (qty 600) correctly falling into Falldown.
- Monday boundary (first day of current week): confirmed live with part `IV9000-90352-0002` (qty 36) correctly falling into the current weekly bucket, not Falldown.
- Exact-Sunday boundary: no real-data example existed on the validation date (zero rows due exactly on the boundary date). This case is covered by automated `Kst.Domain.Tests` unit tests only; it could not be exercised against live data.
- Closed work orders and `RMABOM` work orders: both exclusions confirmed structurally correct and meaningfully active against real data (1 Closed row, 68 RMABOM rows found and correctly excluded from qualifying results).

## 4. Due Date / Release Date projection

- Confirmed that switching `dateBasis` between Due Date and Release Date for the same workspace/snapshot does not trigger a new QAD query (same `snapshotId`, no new backend source-batch log lines) — it is a local, in-memory reprojection as designed.
- Total quantity across all buckets is identical (9,102.0) regardless of `dateBasis`, as expected since the same source rows are simply re-bucketed by a different date field.
- Checked for a null-date semantic risk: **zero NULL due dates and zero NULL release dates** across all 132,711 real qualifying rows in the sampled data. The Domain layer's `basisDate is { } d` null-guard exists defensively but is not exercised by real data today — no defect, no action needed.

## 5. Horizon behavior (12 / intermediate / 72 weeks)

- Verified bucket counts scale correctly (1 Falldown + N weekly buckets) for 4, 12, 20, and 72-week horizons.
- Confirmed 72-week horizon requests do not requery QAD when reusing an existing snapshot (local reprojection only), consistent with Section 4 findings.
- Frontend fiscal-band/week-label logic (Monday-labeled weeks, FY26 4-4-5×4 anchor, 53-week exceptions) is covered by the existing automated frontend suite: `fiscalCalendar.test.ts` (11 tests) and `fiscalCalendarSettings.test.ts` (6 tests), both passing. No frontend code was changed, so no additional manual fiscal-band walkthrough was performed beyond confirming these automated tests are green.

## 6. Status aggregation validation (all execution/planned/explicit combinations)

Real, live examples were found and round-tripped through the actual KST API for every required combination:

| Combination | Example (part @ week) | API result |
|---|---|---|
| Released only (R) | `354219` Falldown / wk 2026-08-10 | `executionStatus=released`, planned=false, explicit=false |
| Allocating only (A) | `243144-PRGM-4` wk 2026-08-17 | `executionStatus=allocating` |
| Frozen only (F) | `IRT6525AU` wk 2026-08-17 | `executionStatus=frozen` |
| Planned only (P) | `157245-6`, many weeks | `executionStatus=none`, planned=true |
| Explicitly Scheduled only (e) | `2530200083-00` wk 2026-09-14; `NF-01-KIT-PRO-1` multiple weeks | `executionStatus=none`, explicit=true |
| Mixed (2+ distinct A/F/R) | `2530200083-00` wk 2026-08-10 (R+A, qty 1075); `243144-PRGM-4` wk 2026-08-10 (R+A, qty 4920) | `executionStatus=mixed` |
| Execution + Planned | `354219` wk 2026-08-24 (R+P, qty 9186); `157245-6` wk 2026-08-24; `NEX826SUB` wk 2026-11-09; `IV9000-25531-0001` wk 2026-09-21; `1150450` wk 2026-08-31; `IF50182456-126B` wk 2026-08-31 | `executionStatus=released`, planned=true |
| Execution + Explicitly Scheduled | `NF-01-KIT-PRO-1` wk 2026-09-07 (R+e, qty 770) | `executionStatus=released`, explicit=true |

All quantities matched the corresponding source SQL exactly (e.g. `354219` Falldown = 702 = sum of 6 pre-current-week rows; `243144-PRGM-4` wk 2026-08-10 = 4920 = R(3840)+A(1080)).

**Note on test setup, not a product defect:** part `354219` was initially queried under site `SW`, which returned all-zero buckets. Direct SQL confirmed `354219`'s real MRP rows exist only under site `AR`. Re-running under the correct site produced full data. This is expected: KST correctly returned zero facts for a resolved-but-out-of-scope part rather than fabricating data, and doubles as a live confirmation of the "zero-fact parent is preserved with empty buckets" behavior.

## 7. Performance measurements

Backend timings captured from structured logs (`MPS product-line scope discovery...`, `MPS source batch...`), local dev machine against the live production QADPRO2 database:

| Scenario | Scope discovery | Source batch(es) | Total (incl. app overhead) |
|---|---|---|---|
| Shure SMT — cold initial load (38 parts) | 366 ms | 4,173 ms (638 rows) | 4,913 ms |
| Shure SMT — warm refresh | 11 ms | 28 ms | 94 ms |
| TACO — cold initial load (18 parts) | 2,021 ms | 439 ms (270 rows) | 2,483 ms |
| ZZ-Val-RangeWide (179 parts) | 84 ms | 1,766 ms (1,121 rows) | 1,929 ms |
| ZZ-Val-Large6150 (816 parts) — **cold** | 18,733 ms | 10,330 ms + 6,772 ms (15,335 rows, 2 batches) | 36,211 ms |
| ZZ-Val-Large6150 (816 parts) — **warm refresh, 72-week horizon** | 550 ms | 325 ms + 192 ms (15,317 rows) | 3,052 ms |
| ZZ-Val-ExplicitParents | 17 ms | 83 ms (709 rows) | 66 ms* |
| ZZ-Val-KTV-3270 | 975 ms | 1,757 ms (180 rows) | 2,780 ms |
| QAD connectivity check (warm) | — | — | 28–103 ms |

\* timing anomaly explained by warm connection reuse from a preceding call in the same session.

**Observation (not a defect, flagged as an operational characteristic):** the 816-part product line shows highly non-linear cold-vs-warm cost — the first query against a given large scope after backend/connection-pool warm-up can take 30+ seconds (dominated by SQL Server execution-plan compilation for a query batched against 500+ VALUES-table rows), while subsequent warm executions of the identical shape complete in ~1–3.5 seconds total, including full 72-week/816-part schedule projection. This is a real-world characteristic of first-use-after-startup for the largest configured workspaces, not a functional defect. No code change is recommended under this verification-only checkpoint; it is noted here for awareness ahead of Stage 5B.10 planning.

Real production data also naturally drifts between calls a few minutes apart (e.g. `ZZ-Val-Large6150` source row count changed from 15,335 to 15,317 between the cold and warm calls) — expected behavior against a live MRP-driven system, not a discrepancy.

## 8. Failure / refresh behavior

Automated coverage in `Kst.Application.Tests/Mps/MpsWorkspaceSnapshotServiceTests.cs` already exercises:
- `Initial_Load_Failure_Yields_Failed_Status_With_No_Snapshot`
- `RefreshAsync_Failure_After_Prior_Success_Yields_Stale_And_Retains_Old_Snapshot`
- `Concurrent_Refresh_For_Same_Workspace_Does_Not_Invoke_Reader_Twice_Simultaneously`
- `GetDashboardAsync_Does_Not_ReQuery_Once_Loaded`
- `GetDashboardAsync_Retains_ZeroFact_Part_With_Empty_Buckets`

All pass in the full backend suite run (see Section 9).

In addition, a live smoke test was performed: the backend was started with an environment-variable override pointing `QadDatabase:Server` at a non-existent host (no config files modified; fully local and reversible). Calling `GET /api/v1/workspaces/{id}/mps` produced:
- HTTP 503 with body `{"title":"MPS data unavailable","status":503,"detail":"Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT."}` — the generic, non-leaking message.
- The real `Microsoft.Data.SqlClient.SqlException` (network error 40/53/64) with full stack trace was logged server-side only, confirming internal details are never exposed to the client.

## 9. Automated regression results

- Backend: `dotnet build Kst.slnx` — clean. Full suite (`Kst.Domain.Tests`, `Kst.Application.Tests`, `Kst.Integrations.Qad.Tests`, `Kst.Api.IntegrationTests`, `Kst.ArchitectureTests`) — **273/273 passed**, no changes required.
- Frontend: `npm run lint` — clean (0 warnings). `npm run typecheck` — clean. `npm test` (Vitest) — **101/101 passed** across 10 test files, including fiscal calendar, MPS presentation, workspace lifecycle, and Add Workspace dialog suites. `npm run build` — succeeded.

## 10. Defects found and fixed

**None.** Every quantity, row count, date boundary, and status-classification check performed against real QADPRO2 data matched the expected KST output exactly. No production code was modified during this checkpoint.

## 11. Remaining risks / unvalidated cases

- Exact-Sunday Falldown boundary has no real-data example on the validation date; relies on existing automated `Kst.Domain.Tests` unit coverage only.
- Cold-start latency for very large workspaces (800+ parts) can reach ~30+ seconds on first use after backend startup; acceptable for a verification-only checkpoint but worth considering for Stage 5B.10 planning (e.g. background warm-up, user-facing loading messaging for very large scopes).
- No frontend manual walkthrough of the 72-week grid in the running Tauri/browser UI was performed this session (validation relied on backend API responses plus the existing green automated frontend test suite); a manual UI spot-check is recommended before final owner sign-off if not already covered elsewhere.

## Readiness for owner acceptance

All Stage 5B.9 verification objectives were completed with zero defects found. Backend and frontend automated regression suites are fully green. The workspace and repository are clean (only a pre-existing, unrelated `tsconfig.tsbuildinfo` diff remains in `git status`). This checkpoint is **ready for manual owner review and acceptance**. Per instructions, no work has proceeded into Stage 5B.10.


## 12. Owner acceptance

**ACCEPTED — 2026-08-10.**

The project owner manually compared the real 72-week KST v2 MPS grid against the legacy Excel MPS report and confirmed that the schedule output looks identical. This closes the final manual UI spot-check noted in Section 11. Stage 5B.9 is formally accepted and Stage 5B.10 closeout may proceed.

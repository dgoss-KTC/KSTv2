# Stage 8 follow-up — Inventory / Lot Locations

Date: 2026-10-08. Status: **COMPLETE / OWNER-ACCEPTED, including quantity-display amendment.**

## Authorization and bounded scope

The owner brought the deferred Component Information Inventory / Lot Locations region forward
from Stage 22 as a bounded Stage 8 follow-up before Stage 18. Stage 8's original acceptance and
Stage 15's acceptance remain historical accepted evidence. This follow-up does not start Stage 18.
Version remains `0.1.0-alpha.15`. Finished Goods export will be addressed in Stage 21.

Baseline: Windows PowerShell 5.1, `C:\Dev\kst_v2`, branch `main`, HEAD
`1e678826ab0f60d30beb47b50ea234ce94d51fbe`, tracking `origin/main` with no reported divergence.
No fetch was performed; the owner's manual push/reference-laptop pull was reported by the owner,
not independently observed here. The baseline working tree contained modified frontend build-info,
both modified Tauri schemas, and an untracked empty root Stage 14 plan. They were preserved.

Authority inspected: current `AGENTS.md`, project status, backend boundaries, Stage 8 closeout,
Stage 8D.5 planning provenance, enacted security entry/policy/profile/development-environment
requirements, accepted Component Detail/Approved Alternates implementations, inventory-summary and
Finished Goods adapters, popup callers/tests, and OpenAPI/sidecar workflows.

## Source and contract decisions

The selected component and workspace site scope the read; the existing `QadSiteDomainMap` resolves
domain inside the integration boundary. `QADPRO2.dbo.ld_det` supplies exactly:

| Source | Visible column |
|---|---|
| `ld_loc` | Location |
| `ld_lot` | Lot/Serial |
| `ld_status` | Status |
| `ld_qty_oh` | Qty on Hand |

Part/site/domain are parameterized. Every scoped physical row is retained, including positive,
zero, negative and null quantities, every status/lot, and duplicate-looking rows. There is no join,
aggregation, DISTINCT, inferred deduplication, or positive-stock/RA/RMA/MRB/nettable/readiness/date/
price selection. Source text is preserved, including empty strings and casing. Null displays `—`.
SQL converts the exact numeric quantity to invariant decimal text; backend/API/frontend keep that
text without conversion to binary floating point or a potentially rounding decimal parser.
An acquisition-local ordinal is hidden rendering identity; it is not persistent source identity,
and row order is not guaranteed across acquisitions.

Component requests are not restricted to the workspace's resolved MPS parents or BOM occurrences.
The loaded MPS snapshot supplies compatibility context only; it never supplies the detail population.
The raw rows can legitimately differ from accepted Net/Non-Net summaries. Those readers and
selection/classification rules are unchanged.

`GET /api/v1/workspaces/{assignmentId}/components/{componentPart}/inventory-locations`
requires `mpsSnapshotId`. Every GET is a fresh independent acquisition, including inventory Refresh.
It does not refresh MPS, Component Detail, Approved Alternates or shortages.
200 contains workspace/site/component/MPS identity, original acquisition timestamp, stale warning,
and the four nullable row values plus hidden row id. 400 invalid request; 404 missing workspace;
409 absent/replaced/incompatible MPS context; 503 failed acquisition without compatible last-good.
Failures are sanitized and never masquerade as empty success. C# DTO/OpenAPI/TypeScript generation
is authoritative; no generated type was edited manually.

## Implementation boundaries and interaction

- Domain: `InventoryLocationRow`, informational crossing-boundary row only.
- Application: `InventoryLocationsService`, reader/delegate, report/result/store contracts.
  Per-workspace/component acquisition serialization, cancellation propagation, and workspace/site/
  snapshot checks before acquisition and again before publication or fallback.
- QAD: `QadInventoryLocationReader`, existing connection factory, timeout/cancellation/site mapping.
  Scope is checked case-insensitively in normalization; out-of-scope or malformed numeric results fail.
- Infrastructure: process-local last-good store, workspace/component case-insensitive keys.
- API: DTO, typed endpoint, DI bridge; no QAD knowledge moves into Application or frontend.
- Frontend: `InventoryLocationsPanel`, `useInventoryLocations`, typed API helper; existing reserved
  region in `ComponentInfoModal`, wired from Dashboard/BOM and Workspace Shortages callers.

Panel acquisition starts on popup mount even when master information is loading/unavailable.
It has inventory-only Refresh/retry, loading, empty and localized failure states, a compact
scrollable four-column table with sticky headings, and visible acquisition time.
Successful compatible server fallback retains last-good rows with a stale warning and original time.
Compatibility is deliberately same workspace/component/site **and same MPS snapshot**. A transport
failure cannot establish current server-side compatibility, so local rows are not shown as fallback.
Refresh may retain same-context rows while loading; failure either receives server-validated stale
data or shows localized error. No failed attempt replaces the stored last-good report.
Request cancellation and per-attempt object identity prevent obsolete success/error publication on
component/workspace/site/snapshot replacement or closure, including sources that ignore abort.
Existing popup dimensions, focus trap, Escape/X, backdrop behavior, focus restoration, component
fields, alternates and shortage content remain covered by caller regressions.

No changes to accepted summary readers, Stage 9–11 algorithms, Stage 14/15 behavior, dependencies,
Tauri capabilities/schemas or version. No live QAD queries, connected desktop acquisition, database
objects/writes, submissions, commits or pushes were performed. The published sidecar was not launched.
Build-time OpenAPI discovery and deterministic test hosts ran as part of documented verification.

## Offline verification evidence

- Backend `dotnet build Kst.slnx --no-restore`: pass, 0 warnings/errors; regenerated OpenAPI.
- Final `dotnet test Kst.slnx --no-restore`: **1,197 passed**, no skipped/failed
  (Domain 306, Application 404, QAD 276, architecture 10, API 201).
- New synthetic tests: parameterized SQL-only scope, no filters/joins/aggregation; case variants and
  site/domain/part leak rejection; source text, all signs/nulls, duplicate-looking rows, empty success,
  acquisition failure, exact JSON string contract, unrelated master failure, compatible stale time,
  cross-site/snapshot fallback rejection, mid-read snapshot replacement, cancellation and queued
  cancellation. No expected values were derived from live company data.
- Focused popup/panel/caller suite: pass after replacing obsolete placeholder expectations and
  making master-detail route/retry selectors distinguish inventory acquisition.
- Full default frontend run: **failed** (673 passed, 3 timeout failures, 5 skipped): unchanged Stage 11
  Edge-helper spawn timeout, Open Orders Save As timeout, Planning fixture-merge timeout. Isolated
  Open Orders/Planning reruns passed; the Edge helper timed out again on that rerun.
- Final full run, `node node_modules/vitest/vitest.mjs run --maxWorkers=2`:
  **676 passed, 5 skipped, 45 files passed**, including that Edge helper. Existing test `act` warnings
  remain informational output. Reduced worker count is a verification invocation, not a config change.
- `npm run typecheck`: pass. Changed-scope ESLint with `--max-warnings 0`: pass.
  Full `npm run lint`: **failed** on only the two pre-existing locked Stage 10 synchronous-effect
  errors (`ComponentOrdersPanel.tsx`, `useComponentOrders.ts`); not represented as a pass.
- Full `dotnet format --verify-no-changes --no-restore`: **failed** on broad existing whitespace/EOL
  findings and initially a new record-with formatting issue. New-scope issue corrected. Final scoped
  verification explicitly covering every new/modified backend file: pass. Earlier directory-only
  includes did not catch the new issue; final evidence uses explicit file paths.
- Production frontend build: pass using a scratch config extending the real frontend config and
  redirecting `tsBuildInfoFile` to `scratch/inventory-locations.tsbuildinfo`, then Vite build.
  Direct `tsc -b --tsBuildInfoFile` was rejected by TS5094 without writing the protected file; the
  config-based redirection fixed the command. Scratch is ignored and not shipped. Existing Vite
  mixed static/dynamic Tauri core import warning remains.
- Post-publish `npm run generate:types`: byte-identical to already-generated client; typecheck pass.
- `scripts/build-sidecar.ps1`: pass. Executables both 127,359,620 bytes; published and Tauri copy SHA256:
  `E2CBF2D1DBA03EC2C09E3806AE862F3330C8F16D653793D36189BA9AAEA8969A`.
  Published/copied `appsettings.json` and `appsettings.Development.json` hashes match pairwise.
  No runtime probe/sidecar launch or Rust/Tauri build was needed for unchanged host code/capabilities.
- AFT diagnostic checkpoint: no reported errors in new production scope; a later test-file inspect
  had one named authoritative-diagnostics gap. Compiler/typecheck/test evidence is the real gate.
- `git diff --check`: pass. No commit/staging/push; working-tree changes are this bounded feature,
  generated contract pair, tests/docs, plus the four preserved baseline items.

### Protected baseline and final SHA256

| File | Fresh baseline = final hash |
|---|---|
| `src/frontend/tsconfig.tsbuildinfo` | `B2AF834E40A54C44857C54B9F5129F500474C25EC58F2B0CFEA19F9C1F575842` |
| `src/tauri/gen/schemas/desktop-schema.json` | `4DFAEC14938E91D2772202A11D0D956F7D17FC4F04F1E120BFCB36FD1A7EC770` |
| `src/tauri/gen/schemas/windows-schema.json` | `4DFAEC14938E91D2772202A11D0D956F7D17FC4F04F1E120BFCB36FD1A7EC770` |
| Empty root `STAGE_14_IMPLEMENTATION_PLAN.md` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |

## Owner desktop checkpoint (pending; separate authorization for connected acquisition)

1. Open a component from Dashboard → BOM and from Workspace Shortages. Confirm the existing
   information/alternates/shortage content and four inventory headings, independent loading and scroll.
2. Compare an owner-selected component/site against all scoped physical QAD rows: preserve lots,
   statuses, duplicate-looking rows, negative/zero/fractional quantities and unavailable nulls.
3. Refresh inventory only. Confirm acquisition time advances on success; with a controlled failure,
   compatible rows remain with stale warning/original time, or no compatible data shows retryable error.
4. Switch component/workspace, refresh MPS or close during acquisition; verify no late data leaks.
   Verify X/Escape, Tab/Shift+Tab containment, and return focus to the origin in both callers.
5. Smoke-check Planning, Finished Goods and inventory-summary/shortage displays remain intact.

The owner explicitly accepted desktop checklist items 1–5 on 2026-10-08. This is owner-reported
validation and acceptance, separate from the agent's offline evidence; no further environment,
part list or acquisition measurements are asserted.

### Owner-requested quantity-display amendment — 2026-10-08

The owner observed excess trailing zeroes widening Qty on Hand and wrapping Location/Lot cells.
Whole source quantities now display without decimal places; fractional source quantities are rounded
to three decimal places using the existing exact-decimal formatter (half away from zero), with no
JavaScript floating-point conversion. Exact source strings remain unchanged and available in the
quantity cell's tooltip. Null remains `—`. This is frontend presentation only; no backend/API/sidecar
change. The owner subsequently confirmed "All is working and looks great. Accepted." on
2026-10-08, explicitly accepting this visual amendment and the completed follow-up. This is
owner-reported desktop evidence, separate from automated verification.
Stage 18 remains unstarted.

Amendment verification: 61 focused frontend tests passed (panel, popup in both contexts, API
boundary); typecheck, scoped lint and production build with scratch build-info passed. Tests cover
whole quantities with source trailing zeroes, positive/negative half rounding, carry into the next
integer, tiny negative fractions, nulls, large exact values and unchanged source tooltips.
No backend/contract/sidecar change required a rebuild. Protected baseline hashes remain unchanged.

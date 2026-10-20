# S22-008 Workspace Shortages touch-ups and protected export

Owner-dispatched 2026-10-19; entry HEAD `e655aa433d1812d0f0cb64dd3dbbf71f972c52a3`.
**COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-20.** One complete local checkpoint commit
explicitly authorized; **no push or deployment**.

## Owner acceptance / bounded closeout — 2026-10-20

The owner reports reviewing the updated UI, testing export and accepting this checkpoint, covering
the presentation touch-ups and shared Export menu, protected Save As and recovery integration.
This is owner-reported UI/export testing and acceptance, not evidence that every historical checklist
scenario, additional native fault test, live-query comparison or external submission was performed.
It is not material-risk acceptance or closure of broader Stage 22/performance/deferred work.

Closeout entry HEAD remains `e655aa433d1812d0f0cb64dd3dbbf71f972c52a3`. All 15 recorded handoff
source/document fingerprints matched before closeout edits; accepted implementation, contracts,
generated artifacts and tests are unchanged. Existing verification and the independent scoped
security review below are reused; no repeated suites/builds/review/publication solely for committing.
Closeout changes only acceptance/status/checklist/API semantics and the existing Windows backlog
record. Review exact staged paths and all hunks (including new source), whitespace, normal mandatory
hooks and post-commit contents/index/tree; do not bypass hooks. The pre-existing build-info hash
below remains preserved and excluded. Logs, evidence, caches and published binaries stay outside Git.

Accepted API semantics: `docs/api/WORKSPACE_SHORTAGES.md`. Windows backlog:
`C:\dev\kst_v2_tmp\Stage22\STAGE_22_READ_ONLY_INVENTORY_REVIEW.md`, existing S22-008 only closed;
related shared/legacy workstreams are not blanket-closed. Full local closeout evidence:
`C:\dev\kst_v2_tmp\Stage22\WorkspaceShortages\evidence\commit-closeout\`.

## Bounded checkpoint plan

Authority: AGENTS.md, enacted security policies, current status/checklist, accepted Stage 11
implementation and current Reports/shared-export implementation. Owner dispatch supersedes the
external design's old export button/inline feedback and incorrect receipt/reload grouping.
Design reference: `C:\Dev\kst_v2_tmp\Stage22\Workspace_Shortages_touchups_design_spec.md`.

1. Restyle LongTermShortagesPanel only: caption/counts, grouped controls, accurate Week 1/negative
   balance legend, readable pills, framed states, density/striping and opaque seven-column freezing.
2. Add typed Workspace Shortages preparation/save adapter using existing shared menu/recovery;
   fixed native report kind/routes, cached validation and unchanged workbook generator. Preserve
   requested display ordering after validation, and bind acquisition identity to retained rows.
3. Verify synthetic workbooks, local mode/cache/filter/detail behavior, context cancellation,
   exact retries, duplicate/uncertain/open recovery, shared regressions and Windows layout helper.
4. Perform independent separate-context scoped security review after implementation/evidence;
   run affected types/lint/tests/builds, republish sidecar and rebuild embedded release host.
5. Record evidence, limitations, executable identity and owner checklist; stop for owner review.

No projection algorithms, hooks, source queries/cache keys, workbook calculations/sheets/precision,
modal semantics, dependencies/capabilities/commands, destination authority or retry limits change.
Native WorkspaceShortages uses existing non-Analytical departure/lifetime rules. No live source
queries or application launch by agent. Evidence stays local at
`C:\dev\kst_v2_tmp\Stage22\WorkspaceShortages\evidence\`.
Pre-entry modified build-info backed up there; SHA256
`2B7072CD8435CA881270DD179D438029DC42D8F3685004BCF6348D5FDCCD983A`.

## Decisions / inspection evidence

- useLongTermShortages excludes receipt mode/Show All from acquisition identity, returns both
  projections, and caches/in-flight-deduplicates population/horizon changes. Wording describes
  projection scope with possible cache reuse, not unconditional server requests.
- Part-number tint follows weeklySeverity[0]; negative balance tint follows the raw ending sign.
- Existing cached export validation returns cache order. The new endpoint reorders validated rows
  to the supplied unique displayed order without changing locked service behavior or serialization.
- Prepared context includes acquisition time, loaded population/horizon, receipt mode, filters/sort,
  snapshot/workspace/site and business date. Component detail, scrolling and unrelated renders do not.

## Verification / review / build

Final offline results (full local evidence under the path above):

| Gate | Result / scope |
|---|---|
| Frontend `frontend-final.json` | **98 passed, zero failed/skipped**, ten files. Includes 27 Workspace Shortages tests, typed Reports adapters, shared menu/recovery/preference, Reports UI, MPS/BOM and Open Orders export regressions and unchanged shortage predicates/sorts. |
| Windows layout helper | **PASS**, actual installed headless Edge. Seven pinned headers/cells, first-week overlap/z-order, two-axis scrolling, 740/900/1050px fixtures, 100/125/150% scales, light/dark and compact/comfortable, opaque striped/tinted cell backgrounds. Helper renders the actual matrix/CSS, not the full application chrome or interactive hover. |
| `backend/shortages-api.trx` | **31 passed**. Four new actual-XLSX cases: 13/26/52/72 weeks with confirmed/all-receipts coverage, manufactured inclusion, filtered/reversed selection order, every component sheet's values compared to the original generator, literal formula-like text and current quantity precision. Exact repeated bytes, session/kind isolation and invalid/replaced acquisition/site/date/population/horizon/component selection tested with no extra synthetic source reads. |
| Other backend | **8 Application + 36 Domain + 10 architecture passed**. Locked cache/projection/workbook regressions; **85 backend total** across distinct projects, not including repeated initial four-case runs. |
| Native captured stdout (retained at `commit-closeout/reused-native-final.stdout.log`; `native-final.log` is only the PowerShell transcript envelope) | **16 passed / 2 intentional interactive ignores**. Closed fixed route/kind, non-Analytical departure, held worker/close, exact file publication/content verification, bounded opening retry and existing historical lifetimes. Chooser and real association tests intentionally not run. |
| Compiler/build | Backend solution build, C# → OpenAPI → generated TypeScript, typecheck, frontend build, native offline check, sidecar publication and final embedded release build **PASS**. No manual generated DTO edits. |
| Lint/format | Changed frontend ESLint zero warnings; scoped C# verify (including Program) and changed native module `rustfmt --check --edition 2021 src/component_mrp.rs` **PASS**. `git diff --check` passes. |
| Exceptions | AFT scoped inspection failed twice with filesystem `read_deadline_exceeded`; no fresh diagnostic claim. Whole-native `cargo fmt --all -- --check` exposes pre-existing formatting in unmodified `lib.rs`; changed-module formatting passes. Existing broad C#/frontend lint exceptions were not reopened. Existing Vite mixed static/dynamic core-import and >500kB warnings remain. |

Invocation corrections are not concealed as passes: local npm wrapper rejected test reporter flags,
so the already-installed Vitest CLI was invoked directly (`node node_modules/vitest/vitest.mjs run ...`).
Initial fixture compile/type errors and missing native synthetic-root environment were corrected;
final retained results above supersede them. A plain release Cargo build did not embed assets;
asset-identity verification caught this, and the final build uses the accepted
`cargo build --release --features tauri/custom-protocol --locked --offline` workflow. All three
final asset names are now verified inside the review host (`embedded-assets.json`).

Independent separate-context security review: **no confirmed new material blocker at reviewed
source/offline-test depth**. Full review, path fingerprints, evidence examined, narrow equivalent
format/test delta and limits are in local `independent-security-review.md`. Reviewer did not rerun
suites or exercise native UI. No risk acceptance or exhaustive interleaving/fault claim.
Cancellation prevents continuation/save authority; already-started preparation may finish within
existing host bounds. Remaining bytes retain existing bounded preparation lifetime, not new
destination/open authority. Uncertain outcomes reconcile only; non-Analytical departure may
truthfully refuse a later Open again after its native grant expires/is removed.

No hooks, cache keys/acquisition logic, source queries, classification calculations, calendar,
workbook serializer, filters/sorts or modal implementation were modified. Legacy API/save helpers
are retained, not removed or repurposed; the migrated screen no longer calls them. Implementation added no new command,
capability, dependency, database object, live source query, app launch, QXtend submission, Git
mutation, installer or deployment. Pre-entry build-info hash remains byte-identical and excluded.

### Accepted review build identity

Use **`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`**, alpha.21, **15,306,240 bytes**,
SHA256 **`AC511C449AF8EA12BD399BA11C52594D0C864B185071287563486071B0EAE6DC`**.
Embedded final assets: `index-DoxGCEnJ.js`, `index-BkISbf98.css`, `event-Csx9d_kZ.js`.
Published / Tauri source-binary / release / debug sidecars all match: **127,532,198 bytes**,
SHA256 `A445B5838F3732E59DB18991C47685CCA03E7602A3F488BC988094FA7DB447ED`.
Full paths/timestamps/hashes are in local `ARTIFACTS.json`. Debug host itself is not the review target.

### Historical owner-review handoff checklist / verification boundary

Retained as the implementation handoff, not a newly pending approval gate or a checklist claimed
agent-executed or individually confirmed by the owner. Owner UI/export acceptance is recorded above
at its reported depth; the existing automated/security limits remain unchanged.

1. Open the release target, load MPS and Workspace Shortages. Check caption counts, readable severity
   pills, warnings by keyboard/hover, Comp/Buyer/Sort and toggles; Unconfirmed stays in the view group.
   Change Horizon/Manufactured and return to an earlier selection: retained rows and cache reuse
   should remain familiar, with export blocked until the requested projection is loaded.
2. Light/dark, compact/comfortable, narrow window and scaling: scroll both axes, hover critical/safety
   and alternating rows, check frozen columns stay opaque/aligned, warning text and controls wrap
   readably. Open component detail, inspect purchasing/projection and close with focus return.
3. Filter/sort, Export → Excel: one activation opens protected Save As. Cancel → Save again without
   re-preparing; scrolling/detail navigation must retain it. Change filter/sort/receipt/population or
   horizon and confirm the old prepared workbook is no longer offered. Save and inspect every sheet.
4. Check shared Open Excel preference and eight-second ordinary-success feedback. If an opening
   failure/uncertain outcome is encountered, use Open again / Check save status without replaying the
   save. Check close while save/open is pending is held safely. No external imports/submissions.

Owner UI/export review and acceptance close S22-008 at the reported depth. No additional native
fault-test, exhaustive visual/hover, live-source parity or external-submission evidence is inferred.
Overall Stage 22, broader performance work and unrelated deferred items remain open. Next work
requires a separate dispatch; this closeout authorizes a local commit only, not push/deployment.

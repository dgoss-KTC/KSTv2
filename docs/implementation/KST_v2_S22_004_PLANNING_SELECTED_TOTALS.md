# S22-004 — Planning View-selected bottom totals

2026-10-20 — **COMPLETE / OWNER-ACCEPTED / CLOSED.**
Actual entry Windows HEAD: `d6e0c52edba243bee1004dfa7322028e72f2ed44`, one commit beyond supplied
`214a45a204d1dfbc5a0b7e9d0fcf62a9f52de78f`. Stage 22 remains open. Owner explicitly authorized
acceptance closeout and one complete local checkpoint commit, **not a push**.

## Decided and delivered

Explicit owner-authorized amendment to Stage 14 Q-R1, dated 2026-10-20:

> “Bottom Planning totals cover models selected in View. Short/Changed filters, collapse state and
> viewport position do not change totals. All other calculation and operational-export rules remain unchanged.”

Authority: `AGENTS.md`, current status/checklist, accepted Stage 14 ledger and existing implementation,
plus this owner dispatch. Stage 14 ledger §22 records the amendment; the original Q-R1 population
decision is marked superseded without erasing its history or any acceptance record. Decision approval
is distinct from the subsequent owner desktop acceptance recorded below.

- `PlanningWorkbookPanel.tsx`: both main and standalone paths pass `filterModels(...).tailored`
  (complete evaluated workbook minus View-unchecked models, **before chip filtering**) into unchanged
  `adjustedWorkspaceTotals`. Full evaluation/per-part summaries remain complete-workspace.
- `PlanningGrid.tsx`: same selected collection for missing-price count and selected scope. All bottom
  flow rows/periods and loaded/proposed values share the population. No selection supplies `[]`, not
  old/all-model totals; exact zeros retain normal blank formatting and explicit 0-of-Y selected scope.
- View and totals wording now explains selected-only bottom totals, chip/collapse/viewport independence
  and retained hidden changes/drafts/export eligibility. Scope labels/explanations use the Planning
  labels module; no-list-price text stays with its existing presentation helper. View search alone
  never changes selection. Existing pinned layout/styles and default dollar rows are retained.

Unchanged: exact arithmetic/helper/golden fixtures, pricing/$0/acquisition failures, RA/calendar/horizon/
Falldown-once/Undated, per-part projections, no inventory summing, full-workspace adjustment evaluation,
validation/Changes summaries/draft contents and operational Mass Updater preparation/validation/saving/
clearing. No backend, native source, API/generated contract, sidecar publication, new scope preference,
dependency, capability, database object or locked Stage 9–11 change.

## Verification and local evidence

Full evidence stays on this laptop: `C:\dev\kst_v2_tmp\Stage22\PlanningTotals\evidence\`.

| Final offline check | Result / retained evidence |
|---|---|
| Affected frontend scope | **219 passed / zero failures / zero skips, 10 files**, `frontend-final.json` / `.log`. Seven new S22-004 cases plus existing main/standalone presentation, redesign, drafts/adjustments, Mass Updater, unsaved, settings and calculation/CSV helper regressions. |
| TypeScript | `npm run typecheck` **PASS**, `typecheck.log`; production build also runs `tsc -b`. |
| Changed-file lint | Installed ESLint on all nine changed/new frontend source/test files, `--max-warnings 0`: **PASS**, `lint.log`. |
| Frontend assets | `npm run build` **PASS**, `frontend-build.log`; existing mixed static/dynamic Tauri core import and >500 kB chunk warnings retained. No dependency install. |
| Release desktop | `cargo build --release --features tauri/custom-protocol --locked --offline` **PASS**, `desktop-release-retry.log`; no sidecar republish. Initial attempt exit 101 because the owner had the release app open (`desktop-release-build.log`). Owner closed it normally; no agent process termination. |
| Artifact preservation | `verify-artifacts.ps1` / `ARTIFACTS.json`: final asset names embedded; release newer than assets; all four sidecar copies match preceding accepted hash; original owner build-info bytes restored and verified. |
| Whitespace / scope | `git diff --check` **PASS** (line-ending conversion notices only); final source/test fingerprints retained. No staged content, backend/native/generated/dependency drift or unrelated cleanup. |

The exact final Vitest command ran these paths, with `--maxWorkers=2`, default + JSON reporters and
the local JSON output path: `PlanningSelectedTotals.test.tsx`, `PlanningWorkbookRedesign.test.tsx`,
`PlanningWorkbookPanel.test.tsx`, `PlanningWorkbookAdjustments.test.tsx`,
`PlanningMassUpdaterExport.test.tsx`, `PlanningUnsavedChanges.test.tsx`,
`PlanningWorkbookSettingsWorkflow.test.tsx` (all under `src/frontend/src/components/`), and
`planningAdjustments.test.ts`, `planningWorkbookSummary.test.ts`, `massUpdaterExport.test.ts`
(under `src/frontend/src/planningWorkbook/`). Backend/native suites were not rerun for unchanged sources.
Automated evidence is synthetic/mocked and does not establish live QAD parity, actual native Save As,
real WebView layout/scrolling, installer correctness or owner acceptance.

New `PlanningSelectedTotals.test.tsx` uses hand-specified differing three-model flows and independent
expected values, never another call to the totals implementation. It covers both modes and grid paths,
all/subset/one/none/reselection; Falldown/weeks/two fiscal quarters; SO quantity/$, MPS quantity/$ and
adjustments with selected loaded/proposed tooltips/strikes; unchanged $0 and warning scope, selected
chip-hidden no-list-price models, View-list search, hidden metric rows/total-row choices, collapse and
synthetic scroll events. Large/fractional values exceed Number precision. Hidden proposals remain in
Changes/draft request, hidden negative-result validation blocks export, and full-workspace CSV request/
preview/save bytes remain identical while synthetic View changes occur during/after preparation.
The modal-background synthetic changes are state-isolation tests, not a claim that normal users can
click through the export modal. Existing export/draft/unsaved/settings and helper/golden suites remain
part of scoped regression coverage. Superseded UI assertions are replaced, not helper/golden coverage.

Initial affected run: **87 passed / 2 failed**. New test expectations corrected a scale-only exact
decimal title (`.325`, not `.3250`) and awaited the existing async bridge before asserting the export
request. No production calculation/export change was required. Corrected six-case new-file rerun
passed; final scope also adds a selected unchanged no-list-price model hidden by Changed.
Initial changed-source (5/5) and affected-test (4/4) AFT diagnostics were fresh/zero errors/warnings;
later broader and final scoped requests failed with filesystem `read_deadline_exceeded`. No fresh final
AFT coverage is claimed. Compiler/tests are the gate. PowerShell 5.1 redirects normal native stderr
progress/notices as `NativeCommandError` records in logs; the successful commands above returned exit 0,
distinct from the genuine first release exit 101. Initial artifact timestamp comparison used local-kind
DateTime conversion against UTC; corrected the evidence script to compare DateTimeOffset.UtcDateTime,
then final identity checks passed without any executable/source modification.

Retain baseline exceptions: broader locked Stage 10 full-lint, inherited backend/native formatting,
and existing Vite mixed-import/chunk warnings are not newly passing checks or risk-accepted. No full
repository/security/native/backend suite is required solely for this display-population amendment.

## Scope/security/preservation screening

Bounded self-screen against `SECURITY.md`, Security Assurance Policy §§5–6 and AI Security Review
triggers: local display collection/wording only. No changed export authority, source access, persistence
schema, trust boundary, external input parser, credential, filesystem privilege, networking, capability
or dependency. Existing React text/attributes remain; no HTML/SQL/command injection surface introduced.
No additional full/independent security review triggered; this is not independent assurance or risk
acceptance. Export/draft tests validate preservation, not new authority.

Only pre-entry modification: `src/frontend/tsconfig.tsbuildinfo`, backed up outside Git; entry SHA256
`39F8E34F0169E1D03E3C80A39586ABCBD450B7F1E378FD8EC9270518DB5C80C6`.
Restore **these original bytes**, not HEAD, after build. Evidence/binaries stay outside Git. During
implementation there was no staging, commit, push, install, live database query/write, QXtend
submission, agent app launch or deployment. The subsequent local closeout commit is authorized above;
build-info remains unstaged and excluded. No push or further development is authorized.

## Review build identity

`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe` — **15,306,752 bytes**, alpha.21
(Windows resource version remains `0.1.0`), SHA256:

`0DE435998789D1297655B884B5D7B04C3C6B9FEC9BF8FE65845955237FDE5EB6`

Release last-write UTC `2026-10-20T17:43:15.8844179Z`; final frontend assets (built before release)
`index-WGiLxB_1.js`, `event-DqXyRTpy.js`, `index-SuUpcr74.css` have names verified embedded.
Exact asset/source/test hashes are in local `ARTIFACTS.json`. No subsequent runtime-source changes.
Published/binaries/release/debug sidecars remain **127,543,462 bytes** each, matching preceding accepted
SHA256 `16CE3AC6FB9A05CA8CAF7D8E3696431251194983EBAB322D3D02E2BD5CC9EA6B`.
Owner build-info hash above is restored byte-for-byte, still modified relative to HEAD and unstaged.

## Historical owner review checklist

The implementation handoff requested review of the rebuilt **release** executable identified above,
not a running old/debug app. The checklist is retained as historical review guidance, not an assertion
that each item was individually reported as executed.
Agent offline DOM checks do not prove real WebView layout or actual native interaction.

1. In Report and Planning, select all, a subset, none, then reselect in View. Confirm all bottom rows
   and Falldown/week/quarter columns change together; empty selection shows zero-selected scope and
   ordinary blank-zero cells. In Planning, compare loaded/proposed values on the selected population.
2. Toggle Short/Changed, collapse/expand and scroll both axes; totals and selected no-price warning
   count must not change. Search the View list without checking boxes; totals stay fixed. Toggle metric
   rows/total rows and confirm only their presentation changes. Check light/dark, both densities and
   narrow-window pinned labels/tooltips/zero-selected scope.
3. Hide a model with proposals. Check Changes retains it; draft/review retains it; a known invalid
   proposal still blocks operational export. Review a valid full-workspace Mass Updater preview and
   confirm hidden models remain included. No external submission/import is part of this checklist.
4. Confirm no-list-price selected models contribute $0, with a selected-only warning even when a chip
   hides the block; unchecking removes that warning contribution. Report acceptance or corrections.

## Owner acceptance and bounded closeout — 2026-10-20

The owner reports reviewing the rebuilt desktop, confirming correct behavior and explicitly accepting
S22-004 on 2026-10-20. The owner subsequently authorized acceptance closeout and the complete local
checkpoint commit. This is owner-run/reported desktop acceptance, separate from agent offline
verification; it does not add agent live-query, native Save As, exhaustive checklist, installer,
deployment, external-submission or risk-acceptance evidence.

Closeout fingerprint comparison confirms all nine source/test files, final frontend assets, accepted
release executable, four sidecars and original build-info match retained `ARTIFACTS.json`. There are
no implementation/test changes during closeout. Reuse the 219 frontend passes / zero skips across
10 files, typecheck, changed-file lint, frontend/offline release builds and artifact checks above;
do not repeat suites/builds, republish sidecars or claim fresh final AFT verification. Existing Vite
warnings, AFT deadlines and broader baseline exceptions remain explicit.

Commit scope is the five frontend implementation files, four frontend test files (including the new
selected-totals tests), this record, current status, master checklist and Stage 14 Q-R1 ledger amendment.
Review exact staged paths/full diff/whitespace, honor normal hooks, then compare committed paths/tree/
blobs with reviewed staging and verify the original build-info bytes and empty index. Local closeout
evidence belongs under `evidence/commit-closeout/`; the existing Windows backlog records S22-004 only.
Pre-existing `src/frontend/tsconfig.tsbuildinfo` remains byte-identical, unstaged and outside the commit.
Stage 22 remains open. No push, new checkpoint, live query, install, submission or deployment is
authorized; further development requires a separate owner dispatch.

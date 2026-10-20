# S22-017 — Superseded workbook-save helper retirement

Date: 2026-10-20. **BOUNDED RETIREMENT / S22-036 DISPOSITION — COMPLETE / OWNER-ACCEPTED / CLOSED.**
Owner authorizes acceptance closeout and one complete local commit, not a push. Acceptance is based
on the supplied implementation report and offline evidence, not independent desktop verification.
Entry HEAD: `c787237c1b05d04269a9701640e87c275aeedeec`. Stage 22 remains open.

## Authority and bounded disposition

- Followed AGENTS.md, current status/checklist, enacted security policy and owner-supplied
  `C:\dev\kst_v2_tmp\Stage22\STAGE_22_REMAINING_WORK_REVIEW_2026-10-20.md`.
- Accepted replacements: `KST_v2_S22_005_OPEN_ORDERS_UI.md` (Analytical contract and authority)
  and `KST_v2_S22_008_WORKSPACE_SHORTAGES.md` (protected export). Their acceptance is unchanged.
- Existing Windows backlog: `C:\dev\kst_v2_tmp\Stage22\STAGE_22_READ_ONLY_INVENTORY_REVIEW.md`;
  updated only S22-017's bounded retirement and S22-036's corresponding disposition. No live
  task-database reconciliation or broader cleanup completion is claimed. Owner acceptance is bounded
  to this retirement and the corresponding S22-036 disposition.

Before removal, caller analysis found five calls, all in the helper's own unit tests. Direct and
indexed reference checks found executable references only in that helper/test and the two panel
test files. `WorkbookSaveResult` was local to the helper. CustomerWorkspace mounts the migrated
panels: Workspace Shortages uses the fixed `WorkspaceShortages` typed adapter; Open Orders uses
the shared analytical request/export component. Browser mode disables protected export.
`index.html` loads `main.tsx`; Vite builds that application, TypeScript includes `src`, Vitest
discovers `src/**/*.test.{ts,tsx}`, and Tauri consumes frontend `dist`, not a helper registry.

## Changes and preserved behavior

- Deleted `src/frontend/src/longTermShortages/saveLongTermShortagesWorkbook.ts` and its
  implementation-specific `.test.ts` (four obsolete test cases).
- Removed only its imports/mocks, mock declarations/resets/outcome setup and obsolete
  not-called assertions from LongTermShortagesPanel/OpenOrdersPanel tests.
- Retained existing positive Workspace Shortages native-kind/acquisition/filtered/sorted-population
  assertions and exact-preparation save retry checks. Retained the negative legacy API assertion.
- Retained Open Orders panel selection/column assertions and the unchanged real shared-export
  integration suite: independently enumerated 1,205-row population, 103 exact-decimal matches
  beyond page one, selected columns, exact-preparation retries and no reacquisition.
- Existing coverage already exercises the active export path; no concrete coverage gap required
  new assertions. No independent expected result or active export test was removed or weakened.

S22-036's obsolete browser-initiation-as-saved route is retired, not redesigned. Historical Stage 18
and Stage 21 evidence remains unchanged. S22-017's broader reconciliation remains open; only its
dispatched retirement sub-scope and corresponding S22-036 disposition are now accepted and closed
on 2026-10-20.

No active save/export code, report authority, operational CSV, API/generated client, backend,
native host, plugin, capability, dependency, database object or locked Stage 9–11 algorithm changed.
File-export security screening found no new trust-boundary/privilege change or concrete material
finding requiring independent review; focused implementing-agent checks were proportionate.

## Verification evidence

Full local evidence (outside Git):
`C:\Users\dgoss\AppData\Local\Temp\opencode\S22-017-retirement-20261020\`.

| Check | Result / evidence |
|---|---|
| Focused frontend tests | **115 passed / 0 failed / 2 inherited skips**, eight files; `frontend-tests.json`, `frontend-tests.log`. 27 Workspace Shortages, 40 Open Orders panel, 12 Open Orders export, 23 shared export, 8 report adapters, 3 open preference, 1 recovery and 1 analytical API pass. |
| Skips | Open Orders screenshot-only fixtures require `KST_STAGE13_POLISH_FIXTURE_PATH` / `KST_STAGE13_PLAN_FIXTURE_PATH`; neither supplied. Not counted as passes. Installed headless Edge matrix-layout test ran and passed within the 27 Shortages tests. |
| Changed-file lint | Installed ESLint on the two retained panel tests with `--report-unused-disable-directives --max-warnings 0`: exit 0; `changed-file-lint.log`. |
| Types / build | `npm run typecheck` and `npm run build`: exit 0; `typecheck.log`, `frontend-build.log`. Existing Vite mixed static/dynamic core-import and >500 kB chunk warnings remain, not newly fixed. |
| Scoped diagnostics | Before edit: 4/4 authoritative diagnostic reports, zero errors/warnings; overall PARTIAL due to two unparsed complexity files. After code edits: FRESH, 2/2 authoritative reports, zero errors/warnings. Not whole-repository health assurance. |
| References / diff | Post-removal source search: zero helper/type/mock references. Indexed remaining references were historical documentation only. Reviewed all bounded hunks and new record; `focused.diff`, `backlog.diff`, `final-verification.json`; `git diff --check` exit 0. Git's LF-to-CRLF advisory is not a whitespace failure. |

Tests used the established installed CLI (the accepted S22-008 record documents npm-wrapper
reporter-flag limitations): `node node_modules/vitest/vitest.mjs run` targeting the two panels,
OpenOrdersExport, AnalyticalExcelExport, analyticalExportApi, analyticalSaveRecovery,
analyticalOpenPreference and reportExportAdapters tests, with JSON reporter/output outside Git.
No test/build failure or remediation was required in this checkpoint.
Evidence capture corrected PowerShell 5.1 generic-method syntax/native stderr warning handling
and the new-file no-index diff's expected exit 1 (differences, with no whitespace findings).
These invocation failures were not source/test failures or passing verification gates.

Entry had only pre-existing modified `src/frontend/tsconfig.tsbuildinfo`. Actual entry bytes were
backed up as `entry.tsconfig.tsbuildinfo`, restored after build and compared byte-for-byte at exit;
SHA256 **`2B79FB70977593B1C5D3699EF3C198E6CF92AA27B01EF062B085FE1307497195`**.
The older closeout hash was not restored. Build-info remains unstaged and excluded from the
checkpoint diff. Index/HEAD were unchanged at implementation handoff, before the separately authorized
closeout commit. `entry-backlog.md` preserves the two rows' prior wording.

No database/live query, installation, external submission, application launch, contract regeneration,
sidecar republish, backend/native suite, desktop release, independent review or deployment performed.
No active behavior change warrants those checks solely for this removal. Existing broad lint/.NET/
Rust formatting exceptions were not rerun or relabeled as passing. Offline evidence is not desktop
acceptance evidence; no new desktop checklist is required for a test/dead-helper-only retirement.

## Acceptance and local commit closeout — 2026-10-20

The owner explicitly authorizes acceptance and one local commit of the complete bounded cleanup,
including deletions, retained-test scaffolding changes and this new record. Routine closeout decisions
are delegated to the planning assistant. No additional independent review is required for unchanged
scope. This closes only the retirement and corresponding S22-036 disposition, not broader S22-017
reconciliation or Stage 22. Historical Stage 18/21 and accepted migration evidence remain intact.

Closeout reuses the reported eight-suite **115 passes / two inherited skips**, lint, types/build,
scoped diagnostics and whitespace evidence. The retained test JSON confirms those totals; the actual
entry build-info backup matches the current file byte-for-byte at closeout entry. Only acceptance
documentation/backlog wording changes during closeout; implementation/tests are unchanged. Final
staged paths, complete diff/new record, whitespace, normal hooks and committed contents/post-commit
state are checked without repeating suites/builds solely for committing. Build-info stays unstaged
and excluded; bulky evidence, binaries and logs remain outside Git at the evidence root above.
Known Vite warnings, AFT limitations and inherited lint/format exceptions remain exceptions.
No desktop verification, live access, dependency installation, new capability/database object,
backend/native/contract change, submission/import, deployment or push is authorized by this closeout.

## Next checkpoint

After the authorized local commit and post-commit checks, stop. Further implementation,
desktop/live-source work or push requires separate authorization. Broader S22-017 and Stage 22 stay open.

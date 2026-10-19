# S22-005 — Customer Open Orders UI

Implemented: 2026-10-16; corrections and owner acceptance: 2026-10-19.
Status: **COMPLETE / OWNER-ACCEPTED — 2026-10-19** (S22-005 only; Stage 22 remains open).

## Owner acceptance and authorized closeout (2026-10-19)

The owner explicitly confirmed completion of
`C:\Dev\kst_v2_tmp\Stage22\OpenOrdersUI\evidence\OWNER_REVIEW_CHECKLIST.md`
and accepted all S22-005 session changes on 2026-10-19. All 24 checklist items are marked complete.
Acceptance includes the Report/Plan redesign, search/chips, local named column views, shared
analytical export, planning dock and safeguards, operational lifecycle epoch correction, Windows
pointer-based column dragging, and numeric-focus correction preserving exact values without
focus-only draft saves. Manual evidence is owner-run/reported, not agent-executed desktop testing
or proof of external QXtend acceptance, submission, installer certification or material-risk acceptance.

Chronology is retained below: initial implementation had 206 frontend passes / 2 inherited skips;
the pointer correction had 221 / 2; the final numeric-focus scope had **223 / 2**. Original backend/
architecture evidence is **82 passed**; native evidence is **24 passed / 2 intentional interactive
ignores**. Backend/native suites and the independent security review were not rerun for the later
frontend-only corrections or this documentation/commit closeout. Changed-file lint/type/build and
the actual synthetic workbook evidence are retained; the two locked Stage 10 full-lint errors,
touched-file formatting failure and partial AFT diagnostics remain explicit exceptions, not passes.

The owner separately authorized reconciliation of closeout documentation and one complete local
S22-005 implementation commit. Accepted implementation/tests/generated contracts are unchanged
during this closeout; existing verification is reused, not re-executed. No push, new implementation
item or broader Stage 22 completion is authorized. Large logs, synthetic workbook, published
executables, caches and local evidence remain outside Git. Earlier pending-review/no-commit text
in retained implementation evidence describes its historical handoff, not the current disposition.

Closeout staged whitespace check reports one additional notice:
`src/frontend/src/openOrders/useColumnDrag.ts:83: new blank line at EOF` (exit 2). The accepted
source is preserved under the no-implementation/no-formatting-cleanup scope; this is not a passing
full staged whitespace check. No active Git hooks were found and none are bypassed or weakened.

Latest frontend/release evidence: `evidence\number-format-fix\` under the supplied OpenOrdersUI
directory. Its `ARTIFACTS.json` supersedes earlier release identities: review executable
`src/tauri/target/release/kst-tauri.exe`, **15,296,512 bytes**, SHA256
`3DD63A91B8954B683006849DB5E84F2E27E4FC4E09894A4BA3FAB662FBAF855C`.
Closeout inspection evidence is retained locally in `evidence\commit-closeout\`.

## Owner-review correction — numeric editor focus (2026-10-19)

Owner reports ten fractional places appearing on focus. Focus now starts with the same clean
presentation: whole Order Qty has no decimal places; insignificant fractional zeros are removed
without truncating a meaningful fractional quantity; Price starts with four visual decimal places.
The UI editing buffer preserves typed text/decimal points while editing and does not replace source
values with formatted/rounded text. Only actual change events update proposals. Original exact
values remain authoritative for comparisons, draft persistence, calculations and CSV validation.
This supersedes the earlier 13.5 raw-text-on-focus presentation, not its exact-decimal business rules.

Offline correction: **223 frontend passes / 2 inherited fixture skips** (18 files), changed-file
lint with zero warnings, typecheck and frontend build pass. Assertions cover ten-place source inputs,
typing `3.`/`3.25` and empty/decimal Price text, no focus-only dirty/draft write, meaningful fractional
quantities, Undo while editing, and date-only export retaining untouched full-precision price/quantity.
No backend/source/contract/native code changed. Updated standalone release/evidence identities are
retained under `C:\Dev\kst_v2_tmp\Stage22\OpenOrdersUI\evidence\number-format-fix\`.
These supersede previous frontend/release artifacts for review; original backend/native/security
review evidence is not claimed as rerun. At this correction's handoff, owner desktop confirmation
was pending and the running debug host was not restarted; subsequent owner acceptance is recorded
above. No agent-run live source queries, installs, commit, push or deployment occurred in that correction.

## Owner-review correction — Windows column dragging (2026-10-19)

Owner reports prohibited-drop cursor instead of header movement. The original HTML5 drag handlers
were insufficient on the actual desktop: the installed Tauri configuration documentation states
Windows HTML5 dragging requires disabling the host's native drag/drop handling, which defaults on.
The window-wide host setting is intentionally unchanged. Headers and the Shown Columns list now
use a focused shared pointer-drag hook instead, committing through the existing `moveColumn` path.
No native file-drop behavior, permissions, backend/source code, contracts, proposals or calculations
were changed. This corrects an owner-observed UI defect; prior mocked HTML5 event tests were not
proof of desktop drag functionality.

Pointer handling includes primary-button/threshold checks, capture, coordinate hit testing constrained
to the same column surface and known IDs, themed drag preview/drop indicators, continuous edge
scrolling, and cancellation on Escape/pointer cancellation/lost capture/window blur/context departure/
unmount. Nested list buttons remain clickable. Escape cancels the gesture before closing its popup.
Existing Alt+arrow controls and hidden-column ordering remain. No dependency/tooling was added.

Offline correction verification: **221 frontend passes / 2 inherited fixture skips** in 18 files,
including pointer gesture cases, Report export ordering and Plan proposal/reason/generation preservation.
Changed-file lint (zero warnings), typecheck and frontend/standalone release builds pass. AFT's returned
diagnostics are clean but partial; actual type/build checks are the gate. Backend/native tests from
the original checkpoint were not rerun for this frontend-only correction. Existing full-lint and
formatting exceptions remain; no new clean-full-repository claim is made.

Latest correction evidence: `C:\Dev\kst_v2_tmp\Stage22\OpenOrdersUI\evidence\drag-fix\`.
`regressions.json`, build/type/lint logs and `ARTIFACTS.json` supersede the original frontend/release
evidence for the corrected build. Standalone review executable remains
`src/tauri/target/release/kst-tauri.exe`, now **15,296,000 bytes**, SHA256
`B81F52F2FEC2CB8368E9AE453155E97D3DECD8DD59005673DD011AC4F1AF0C83`;
all current frontend asset names are verified embedded. The sidecar is unchanged. The owner's running
debug instance was not closed/restarted; native pointer confirmation was pending at that handoff
and was subsequently included in the owner acceptance recorded above.
No app launch, live source access, install, commit, push or shared deployment occurred in this correction.

## Bounded implementation plan

Entry HEAD `b1e7947ce175e91f7161725ea4b8befa4b75c0c1`; clean Windows working tree.
Authority: AGENTS.md, enacted security policies, current status, accepted Stage 13 contracts,
Stage 21 and the implemented/owner-accepted MPS/BOM export controls (including eight-second
feedback). Supplied Windows design references: `C:\Dev\kst_v2_tmp\Stage22\OpenOrdersUI\`.
The dispatch resolves the design's open questions: seven-field literal search, local columns-only
views per workspace/mode, no date shortcuts or header sorting. Mockup assets are reference only.

Sequence:
1. Inspect report/planning, layout, filter, shared export/native contracts and affected tests.
2. Add validated local named views and one ordering operation; migrate customized legacy layouts
   to Personal while retaining application Default. Protect modified-view switches.
3. Replace presentation with themed segmented modes, query/popovers, bounded sticky table,
   pagination and planning dock; retain proposal/draft/date/decimal/generation behavior.
4. Extend C# analytical preparation contract with the existing typed Open Orders selection,
   reuse cached-report validation and workbook serialization, regenerate OpenAPI/TypeScript;
   use the actual shared analytical export component. No new native authority.
5. Scoped offline behavior/workbook/regression checks, type/lint/build; independent separate-context
   security review after evidence, fix confirmed in-scope blockers, republish sidecar and desktop.

Original implementation authorization protected locked Stage 9–11 algorithms, source populations,
planning calculations, operational CSV
contents/fresh validation/per-file save/submission boundaries. No dependencies/capabilities, live
queries, installs, commits, pushes or shared deployment. Manual visual/native checks remain owner-run.

Evidence root: `C:\Dev\kst_v2_tmp\Stage22\OpenOrdersUI\evidence\`.

## Implemented scope

- Themed Report/Plan segments, compact query controls, bounded sticky-header table, drag indicators,
  keyboard column movement, counts/pagination footer and loading status below the table. No sorting
  controls, relative-date shortcuts or imported mockup assets.
- Quick search is case-insensitive **literal contains**, OR across SO, PO, item number, customer
  code/name, customer part and salesperson, then AND with existing report predicates. Filtering and
  existing business ordering apply to the complete report before pagination. Search/chip clearing
  never clears planning proposals.
- Columns-only personal views use `kst.openOrders.views.v1.{assignmentId}.{report|plan}`. Schema,
  names, counts and IDs are validated; Default cannot be overwritten; required Plan columns cannot
  be hidden. Valid customized legacy layouts become Personal. New names are 1–60 characters,
  case-insensitively unique (Default reserved), up to 50 personal views. Storage failures are visible
  and retain session state. Named switches require Save/Discard/Cancel if modified. Report and Plan
  working layouts are separate; unsaved modifications are not silently persisted as definitions.
- Headers and Columns list use the same full-order move. Hidden relative order is preserved when
  moving shown columns; showing a hidden column appends it to Shown. Alt+Left/Right on headers and
  Alt+Up/Down in the list provide keyboard alternatives. Overlays are portaled outside table scroll,
  inherit the shell theme/density, consume Escape, contain Tab and return focus.
- Plan retains exact-decimal proposals, explicit dates, reasons, original/proposed comparisons,
  dirty navigation, saved-draft fresh reconciliation, conflicts and undo/clear. Bottom dock presents
  workspace-wide counts/generation, changed-only and hidden-change recovery. Reason Code and
  Planning status are right-pinned at wider sizes and normally scrollable below 1100px. Discarding
  a discovered saved draft is explicitly confirmed, preserves in-memory work and waits for saves.
- Operational generation remains fresh validation → applicable CSV files → independent explicit
  per-file Save As. It is not XLSX export or QXtend submission. Resolved draft state, current source,
  valid proposals/reasons/dates and generation gates remain required. A monotonic context epoch
  prevents an old response reviving after Plan → Report → Plan.

## Analytical contract and authority

The C# analytical request gains an optional existing `ExportOpenOrdersReportRequestDto` selection.
For `Kind = OpenOrders`, the adapter reuses the accepted cached-report selection validation and
workbook generation; it does not acquire QAD data or accept frontend cell values. The existing
report-export endpoint remains compatible. Workspace/site/MPS/report and business-date validation,
duplicate/unknown key/column rejection, server-owned values and post-generation MPS check remain.
OpenAPI and generated TypeScript are synchronized through `npm run generate:types` (not hand-edited).

The actual shared `AnalyticalExcelExport` component/client/recovery is used unchanged: format menu,
automatic protected Save As, retained exact bytes for deliberate retries, observation-only uncertain
status, artifact-ID-only Open again, and ordinary successful-save feedback at **eight seconds**.
Context includes workspace/MPS/report/site/date/search/filters/visible column order, not pagination.
Plan departure invalidates an in-flight analytical preparation. Native chooser/filesystem/grants,
capabilities, dependencies, source queries and locked Stage 9–11 algorithms were not changed.

## Offline verification and independent review

- `frontend-final.json`: **135 passed / 2 inherited screenshot-fixture skips** (13 files).
- `affected-regressions.json`: **71 passed**, no skips (native adapter, workspace lifecycle/navigation,
  existing Planning UI). Combined nonoverlapping frontend total: **206 passed / 2 skipped**.
- Backend filtered solution: **82 passed** across Domain (4), Application (12), QAD (12), architecture
  (10) and API (44). Final API rerun: **44 passed**. All source readers are synthetic/test doubles.
- Actual returned XLSX is reopened with ClosedXML: 205 selected rows across pages, exact row/column
  order, hidden-field filtering at frontend selection, correct numeric values, literal formula-like
  source text (no formula), identical repeated bytes, session isolation and no export-time reread.
  Retained workbook: `Synthetic-Open-Orders-205-rows.xlsx` (synthetic, not a company-data export).
- Native offline tests with bounded `KST_MRP_TEST_ROOT`: **24 passed / 2 explicitly ignored interactive
  chooser/association tests**. Initial root-unset runner failed; corrected final run is retained.
- Changed-file ESLint **zero errors/warnings**, TypeScript, frontend build, contract generation,
  sidecar publish and Cargo debug/release builds pass. Vite retains chunk-size/mixed-import warnings.
  Full lint still fails on two unchanged locked Stage 10 `set-state-in-effect` errors. The touched-file
  `dotnet format --verify-no-changes` check fails on whitespace/line-ending/style findings (including
  existing-file regions); this is **not** a formatting pass and no broad formatting rewrite was made.
  AFT is partial for an unavailable TypeScript diagnostic report; returned reports have no errors,
   but real type/build verification is the gate. The original tracked working-tree `git diff --check`
   passed; the later complete staged check, including previously untracked files, is recorded above.
- Separate-context security-specific review: **no confirmed material blocker remains**. Its one
  low lifecycle finding was reproduced before the fix and resolved using the monotonic epoch plus
  the passing deferred round-trip regression. Report: `INDEPENDENT_SECURITY_REVIEW.md`. This is
  bounded source/offline review, not human acceptance, comprehensive native assurance or risk acceptance.

## Original local artifacts and owner-review handoff (2026-10-16; historical)

Established sidecar workflow ran with installed assets only (`--no-restore`). All four sidecar copies
match: **127,429,798 bytes**, SHA256
`3AB0696E70AE4FD45148858C40D18BEFEF9FCBA4D127F0CF1FA3A7E0A9A00E16`.
Frontend `dist` and Cargo debug/release hosts were built. The standalone review release at
`src/tauri/target/release/kst-tauri.exe` was then built using existing
`cargo build --release --features tauri/custom-protocol --locked --offline`, embedding current assets
without installing a CLI or changing dependency/native configuration files. Current JS/CSS asset
names are present in the binary. Release: **15,295,488 bytes**, SHA256
`5B6223539056DF09566619E71C7F975B09200AA6DBAB182779E7D2590278F858`.
`ARTIFACTS.json` records identities. This is a standalone local review executable, **not** an installer
or proof of native/packaged runtime behavior; the debug development host still uses the configured
frontend dev server. No app/native chooser was launched for this task. Generated
`tsconfig.tsbuildinfo` churn alone was restored to entry content.

Owner checklist and detailed evidence: `OWNER_REVIEW_CHECKLIST.md` and
`S22_005_IMPLEMENTATION_EVIDENCE.md` in the evidence root. At the original handoff, visual layout/
theme/density, pointer drag and native Save As/recovery/open checks awaited owner execution; they
were not certified by unit/component tests. The owner subsequently completed the checklist and
accepted S22-005 on 2026-10-19 as recorded above. External operational processing/QXtend acceptance
is not established by that completion. No agent-run live database queries, installs, operational
submissions, commits, pushes or shared deployment occurred during implementation; the later local
commit authorization is separate. Stage 22 overall remains open.

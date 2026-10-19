# S22-005 — Customer Open Orders UI

Implemented: 2026-10-16; corrections and owner acceptance: 2026-10-19.
Original scope: **COMPLETE / OWNER-ACCEPTED — 2026-10-19**.
Reopened header-filter + pagination follow-up: **COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-19**.
Stage 22 remains open; the follow-up does not undo the original accepted behavior/corrections.

## Reopened follow-up owner acceptance and authorized closeout — 2026-10-19

The owner completed the final desktop checklist, confirmed everything works and explicitly accepted
**both** the header-filter/shared-editor/exact-value work and the subsequent Report/Plan pagination
addition. The owner authorized closeout documentation and one complete local commit, **not a push**.
This closes the reopening from `e3585f141dd67137a757c36de25133a970c8cbc0`; the original S22-005
acceptance (`3de4830`) and accepted S22-010 commit (`e3585f1`) remain preserved history.

Acceptance applies to the final release identity **B599F27E13005836148A47108B9A5E51781A84F11F601C356ECE2D37AA1D53C2**
recorded below, with final frontend assets embedded. Earlier review identities are superseded. Manual
checklist completion is **owner-run/reported**, not agent-executed testing or evidence of additional
live-query parity, native test suites/fault cases, external QXtend acceptance/submission, deployment,
installer certification or material-risk acceptance. Earlier pending statements below are historical.

Closeout changes documentation only and reuses accepted verification: header-filter **232 frontend
passes / 2 inherited skips**, **96 backend passes**, actual synthetic XLSX checks and bounded independent
review; pagination **168 frontend passes / 2 inherited skips**, type/lint/frontend/release builds and
final embedded-asset identity. These frontend scopes **overlap and are not additive**. Pagination reused
unchanged backend/native evidence, with **no sidecar republish** or repeat security review. Existing
C# formatting failures, partial AFT diagnostics, inherited build/test warnings and historical failed
attempts remain explicit below, not new clean-check claims. No suites/builds/review are rerun solely
for this closeout. Exact included paths/hunks, staged whitespace, normal hooks and post-commit state
are the closeout checks; no hook bypass or implementation correction is authorized here.

The commit includes all accepted source, shared controls, tests, DTO/OpenAPI/generated contracts,
pagination and closeout documents. The only unrelated entry modification is
`src/frontend/tsconfig.tsbuildinfo`: preserved byte-for-byte, SHA256
`2B7072CD8435CA881270DD179D438029DC42D8F3685004BCF6348D5FDCCD983A`, excluded from staging/commit.
Large evidence/logs/XLSX/caches/published binaries remain outside Git. Only S22-005 in the local Windows
backlog export `C:\dev\kst_v2_tmp\Stage22\STAGE_22_READ_ONLY_INVENTORY_REVIEW.md` is reconciled;
the export remains outside Git. No new dependency/capability, live source read, database change,
operational submission, deployment or push. **Stage 22 remains open**; no next feature is authorized.

## Owner pagination addition — complete / owner-accepted — 2026-10-19

The owner initially reported the header-filter follow-up looked good and reserved final acceptance
until this addition. The final checklist/acceptance is recorded above. The dispatch explicitly
superseded the fixed 100-row requirement in the earlier follow-up below. Original corrections and
source/export safeguards remain.

### Delivered behavior

- Both Report and Plan footers offer exactly **100 / 500 / 1000**, default **100**. The existing
  Shipments button-group presentation is reproduced narrowly, without refactoring its component:
  `role="group"`, accessible name **Rows per page**, themed tokens, `aria-pressed`, normal Tab,
  Enter and Space button behavior. Table/sticky/scroll rules are unchanged.
- One Open Orders-only preference: `kst.openOrders.pageSize.v1`, shared across modes/workspaces,
  independent of Shipments and saved column views. Only exact supported decimal strings are loaded;
  missing/invalid/failed storage reads yield 100. Writes are validated and guarded; failed storage
  does not block current-session use. No row/filter/proposal/draft data is stored with this preference.
- Size change uses `floor(currentClampedPage * oldSize / newSize)`, keeping the previous first row
  inside the new page. All existing slices, ranges/counts, Previous/Next, hidden-change lists/counts
  and Find row now consume the selected size. Existing filter/search/refresh/changed-only resets and
  effective page clamping remain; empty results keep page 1 of 1 and disabled navigation.
- Size changes do not acquire data, modify exact proposals/in-progress inputs, save an unchanged
  draft, change filters/search/views/required columns or affect pointer dragging. Operational generation
  still covers all staged workspace proposals, not a page. Page size/position are absent from the
  analytical/operational context signatures; full matching cached keys and displayed column order are
  unchanged. Prepared-workbook Save again uses its existing preparation, not a regenerated workbook.

### Pagination verification and evidence

Evidence: `C:\dev\kst_v2_tmp\Stage22\OpenOrdersHeaderFilters\evidence\pagination\`.

- **168 passed / zero failed / 2 inherited optional screenshot skips, 15 files**: affected Open Orders
  predicates/planning/date/view/drag/pagination/panel/actual shared export, CustomerWorkspace,
  AnalyticalExcelExport and analytical recovery. Existing broader header-filter/backend/native evidence
  below is reused, not counted again or represented as newly rerun.
- New tests use **1,205 synthetic rows** for all three sizes, partial pages, first-visible offsets,
  keyboard activation/pressed state, filtering from a later page to fewer/no rows, refresh, mode and
  changed-only clamping, Plan hidden changes and Find row at 500/1000, exact saved price preservation,
  invalid/in-progress inputs and no unchanged-draft save. The real analytical adapter test independently
  checks all 1,205 ordered selected keys/column order and one preparation across repeated size/page
  changes and Save again. Preference tests cover all supported sizes, invalid strings and actual
  throwing storage, plus cross-workspace/mode usage and separation from Shipments/views.
- Initial focused run had one **test mock** failure: this harness uses its own in-memory storage,
  so spying on browser `Storage.prototype` did not intercept it. The test now spies on actual
  `localStorage` and asserts throwing reads/writes are invoked. Production behavior needed no fix.
  `frontend-focused-initial-failed.json` is retained; focused rerun and final affected run pass.
- Final `npm run typecheck`, zero-warning pagination changed-file ESLint, `npm run build`, and
  `cargo build --release --features tauri/custom-protocol --locked --offline` pass. Raw logs/results
  retained. Existing Vite mixed-import/chunk-size warnings remain. No installs or sidecar publish.
- AFT production two-file check was **FRESH** (no errors/warnings, inherited deprecated returnValue
  hint). Final five-file check was **PARTIAL**: three test files lacked authoritative diagnostics and
  two complexity parses were incomplete. No clean whole-scope claim. Earlier C# formatting/AFT
  exceptions remain exceptions; no unchanged backend/native full suites or security review repeated.
  No security-sensitive boundary changed and no concrete correctness blocker remains at tested depth.

### Accepted desktop build — supersedes the preceding review identity

Use **`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`**, version **0.1.0-alpha.21**,
**15,302,656 bytes**, SHA256
`B599F27E13005836148A47108B9A5E51781A84F11F601C356ECE2D37AA1D53C2`.
Final assets verified embedded: `index-DXq7IRB-.js`, `index-DqY8HxHi.css`, `event-D2CGfUk8.js`.
The earlier 15,302,144-byte host/hash 1AC4… below is historical and **superseded** for review.
All four sidecars remain byte-identical to the prior published identity DD92…; none was republished.
`pagination/artifact-identities.json` and `verify-artifacts.ps1` record exact paths/hashes/timestamps.
At implementation handoff, original owner build metadata remained byte-identical (SHA256 2B7072…)
and all work was unstaged. No agent commits/pushes, deployment, app UI/native Save As, live queries,
submissions, dependencies/capabilities or locked-stage changes occurred during implementation.
Header-filter independent review remains scoped to
its reviewed implementation; it is not relabeled as a new pagination security certification.

### Final owner review checklist — completed by owner report, not agent-executed

1. In Report and Plan, use 100/500/1000, including a final partial page and a size change from a later
   page; check range/page labels, first-row retention, pressed/focus states, themes and scrolling.
2. Filter to fewer/no matches, switch mode and Changed rows only; verify valid paging. Change a size,
   switch workspace/relaunch and confirm the shared preference without changing Shipments/views.
3. Edit/save a draft, leave an incomplete input, change size/page, and check retention plus hidden-change
   counts/Find row at 500 and 1000. Do not submit operational files during this review.
4. Prepare a report workbook, change size/page and Save again. It must still cover all matching rows
   in the displayed column order, without regenerating. Spot-check native recovery/eight-second feedback.

The owner reports this final checklist complete and accepts the combined follow-up as recorded above.
Next: only separately dispatched Stage 22 work; no next feature stage is started by this closeout.

## Reopened header-filter follow-up — owner-dispatched 2026-10-19

The original accepted implementation/corrections below remain the baseline. Follow-up entry HEAD
is `e3585f141dd67137a757c36de25133a970c8cbc0`. Index empty; pre-existing modified
`src/frontend/tsconfig.tsbuildinfo` is preserved byte-for-byte. This checkpoint is implementation,
offline verification, separate-context security review and local review-desktop rebuilding only;
no owner acceptance, live source access, installs, commits, pushes or deployment is implied.

Bounded sequence / acceptance targets:
1. Extend only API DTO mapping with invariant decimal-text companions for existing report values;
   regenerate OpenAPI and TypeScript. No source/business calculation changes.
2. Extract the accepted Shipments editor narrowly into shared presentation; keep Shipments identity,
   async ownership and rules in its wrapper. Open Orders supplies facets from the complete loaded
   report, quick search AND other committed predicates, excluding the edited field.
3. Replace chips with header funnels and a compact all-fields/hidden/filter-only review menu.
   Explicit Apply/Cancel/Clear, bounded searchable values, retained exceptions, global-all and
   displayed-search-only bulk behavior; keep both-bound Due Date validation and text Product Line range.
4. Keep source-value filtering in Plan, existing views/pointer/keyboard movement, 100-row pages,
   proposal/draft/epoch guards and all-workspace operational generation. Include committed filters
   (not popup drafts/search/focus) in the existing analytical export context.
5. Verify exact boundaries, full-population/cascade/blank/cap behavior, selection/export across pages,
   Plan/Find row/view/drag/focus and save-context/retry regressions; inspect synthetic XLSX; independent
   security review after implementation/evidence, then sidecar and embedded release rebuild.

Evidence root: `C:\dev\kst_v2_tmp\Stage22\OpenOrdersHeaderFilters\evidence\`.

Delivered decisions and intentional differences are recorded in
`docs/api/CUSTOMER_OPEN_ORDERS.md` under **S22-005 reopened header-filter amendment**. The shared
editor is a narrow presentation extraction, not a shared data-operation architecture. Open Orders
uses its complete retained report; Shipments retains operation/version/column ownership in its wrapper.
The old `FilterBuilder` is removed after its sole production caller migrates, with the existing
behavioral tests adapted to funnels/menu rather than retaining a second chip workflow.

The legacy Contains blank-exclusion default remains; explicit blank inclusion, additional retrieved
field predicates, non-Due date filters and exact numeric predicates are deliberate additions. Due Date,
Product Line text-range, seven-field quick search, sorting and 100-row pages are preserved. Facets
exclude the same field's committed predicate and ignore proposal edits/changed-only/page slicing.
Filters remain visit-local and independent of saved column definitions. The API adds only invariant
text companions for existing decimals; old-sidecar affected numeric editors block rather than round.

Plan source/proposal distinction, hidden-change recovery and Find row focus are explicit. All-workspace
operational generation, fresh-validation gates, draft/dirty/conflict/epoch protections, per-file CSV
saving and analytical native save/recovery/eight-second feedback code remain separate and unchanged.
Only committed predicates feed the existing analytical export context and complete matching line keys.

### Final offline verification and review (follow-up only)

- Final affected frontend: **232 passed / 2 inherited optional screenshot-fixture skips**, 19 files,
  `frontend-final.json`, bounded to two Vitest workers. Covers Open Orders predicates/panel/actual
  analytical export, view/drag/planning/draft/CSV helpers, shared editor, Workspace Shipments panel/client,
  Reports Historical Shipments, CustomerWorkspace and Planning Workbook. An earlier default-concurrency
  run during concurrent checks had four runner `STACK_TRACE_ERROR` failures; it is retained as
  `frontend-concurrent-failed.json`, not a pass or diagnosed application defect. The final bounded run
  changes no assertions/timeouts/implementation to obtain its result. Earlier passing runs overlap;
  they are not added to the final total.
- Final nonoverlapping backend: **96 passed / zero failed/skipped** — Domain 20, Application 13,
  QAD 18, architecture 10 (`backend/` TRX), API 35 (`backend-final/api-final.trx`, superseding its
  earlier 34). Exact DTO strings beyond floating-point precision, nullable/negative values and both
  analytical selection/session validation and operational regressions pass. All readers are synthetic.
- Actual returned XLSX files are reopened with ClosedXML. The 205-row synthetic workbook verifies
  requested column/reverse-row order, literal formula-looking text and stable session-owned bytes.
  New **103 matching rows from 305** verifies every independent expected row/cell and three-column
  order including page-two matches; only cached line keys select cells and no source reread occurs.
  Both synthetic files are retained. Independent ZIP/XML review also verifies every selected row/cell,
  no formulas, external links or macro parts. No installed-Excel/native save or live-source parity claim.
- C# build/OpenAPI generation, `npm run generate:types`, final typecheck, zero-warning changed-file
  ESLint and frontend production build pass. Scoped C# `dotnet format --verify-no-changes` **fails**
  (exit 2): touched existing files retain whitespace/style and working-copy end-of-line findings.
  `format-final.json/.log` records the exception; no broad rewrite. Existing full-solution format,
  locked Stage 10 full-lint and Rust-format exceptions are not passes and were not broadly rerun.
  Existing Vite mixed-import and >500 kB chunk warnings remain.
- AFT's single `headerFilters.ts` inspection was fresh with zero diagnostics; broader TypeScript
  inspection remained **PARTIAL**, with unavailable shared-editor diagnostics and an inherited
  deprecated `beforeunload.returnValue` hint. Later backend inspection hit a filesystem deadline.
  No clean whole-scope AFT claim; actual compiler/type/test/build checks are the gates.
- Separate-context independent security review **and final delta addendum** report **no confirmed new
  material security or filter/export correctness blocker** at reviewed source/offline depth.
  Independent runs were 83 passes / 2 skips, then 64 / 2 (overlapping, not additive), plus typecheck;
  review also inspected retained TRX and both XLSX files. `independent-security-review.md` records
  scope/limits and test `act(...)` warnings. No risk acceptance or native/live security certification.

### Earlier header-filter desktop identity (superseded by pagination above)

Review target: **`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe`**, application
**0.1.0-alpha.21**, installer numeric **0.1.0**; **15,302,144 bytes**, SHA256
`1AC4BFB57FD5BA8FF62AD07DE4C13E46F22F8D3DE1392F02DF165137C5A6CEE7`.
Final embedded assets: `index-BTkvWKd_.js`, `index-CfFJCugG.css`, `event-CfdjI-aT.js` (names checked
inside the executable). The prior intermediate follow-up release is superseded, not the review target.

Established sidecar workflow used installed assets only (`dotnet publish --no-restore` through
`scripts/build-sidecar.ps1`). **All four** published/binaries/release/debug copies now match:
**127,524,006 bytes**, SHA256 `DD92C835760B90D88EB5516E522B0EA15DDD53B663D326A4A09B4ABBCD209FD7`.
Release build uses `cargo build --release --features tauri/custom-protocol --locked --offline`;
`cargo check --offline --locked` passes and refreshes the formerly stale debug sidecar after its
prior lock is absent. Debug configuration still uses `http://localhost:1420`, not embedded assets;
**use release for review**. No app was killed/restarted for artifact refresh. The source-disabled
copied-sidecar probe verifies process/handshake identity, 127.0.0.1-only listener, health/readiness
and unchanged static Workspace/Reports specs (four non-acquiring requests only).

`artifact-identities.json`, `verify-artifacts.ps1`, `sidecar-publish.log`, `cargo-release.log`,
`frontend-build.log`, `sidecar-startup.json/.log` and `verify-sidecar.ps1` retain evidence. Native
protected-save behavior/tests are unchanged and were not rerun; no UI launch, installer, deployment,
source reads, QXtend submission or external processing is claimed. Owner desktop acceptance is pending.
Entry `tsconfig.tsbuildinfo` is restored byte-for-byte, SHA256
`2B7072CD8435CA881270DD179D438029DC42D8F3685004BCF6348D5FDCCD983A`, still pre-existing modified/unstaged.
No staging, commit or push. HEAD remains the entry baseline. No new dependencies/capabilities/DB objects.

### Earlier header-filter checklist (historical; final owner acceptance recorded above)

1. Close any old KST normally; launch the **release executable above**. Check light/dark, densities,
   narrow window, sticky scrolling and header funnels. Verify Apply/Cancel/Escape/outside dismissal
   and focus return; header dragging/Alt-arrow movement must still work independently of funnels.
2. Combine quick search with Values/Contains/exact numeric/boolean/date filters. Check cascading counts,
   blanks, active indicators, zero-result headers and hidden/filter-only fields through Filters.
   Due Date needs both bounds; Product Line Range stays textual. With a large list, search beyond
   the cap; global all includes hidden values and search bulk is displayed-only.
3. Hide a filtered column/switch views; confirm the filter remains. Clear column filters keeps search;
   Clear search and filters clears both. View Save/Discard/Cancel remains columns-only.
4. Plan: edit filtered source rows without losing them, restore/save/undo a draft, check changed-only,
   hidden-change counts and Find row. Verify original/source explanation, numeric-focus correction
   and confirmed clearing/dirty navigation. Filters must not narrow all-workspace generation scope;
   do not submit files into QXtend as part of this review.
5. Report: export matches spanning pages in the selected column order. Cancel Save As, edit/cancel
   a popup and retry exact preparation; Apply must invalidate old prepared bytes. Check successful
   eight-second feedback and existing recovery actions. Spot-check Workspace/Reports Shipments.

At that handoff, the next checkpoint was owner review/acceptance or specific corrections. The final
combined checklist and owner acceptance are now recorded above; this historical list is not a new gate.

## Original owner acceptance and authorized closeout (2026-10-19)

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

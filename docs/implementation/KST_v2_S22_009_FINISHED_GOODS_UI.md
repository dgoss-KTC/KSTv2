# S22-009 — Finished Goods UI consistency and header filters

## Pagination-removal amendment — 2026-10-20

**PAGINATION-REMOVAL FOLLOW-UP COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-20.**
Owner: “Pagination has been removed from the targeted areas and all is working well. Accepted.”
The sole Finished Goods entry now renders every matching acquired physical row in existing sort order
through normal table scrolling. Page controls/state/size preference retired; old stored key is inert.
Filters/cascading facets, acquisition, valuation, identities and no-export scope remain unchanged.
Only pagination is superseded; historical acceptance/checklists below are preserved. Owner-reported
behavior is distinct from independently inspected later artifact identity; guarded rebuild still
blocked before Cargo, later binary build-command provenance unavailable. Evidence/limits/acceptance:
`KST_v2_S22_005_009_UNPAGED_TABLES.md`. One complete bounded local commit authorized, normal hooks,
no push; actual closeout-entry build-info preserved unstaged/excluded. Stage 22 remains open.

2026-10-19 — **COMPLETE / OWNER-ACCEPTED / CLOSED.**
Entry Windows HEAD: `f83c2b524d22df3fceee43489b6cb361608f3d57`.
Stage 22 remains open; this closes S22-009 only.

## Owner acceptance and authorized closeout — 2026-10-19

The owner reviewed the application and explicitly accepted the styling update, all five header
filters, full-report cascading counts, exact-decimal filtering and 100/500/1000 paging. Acceptance
applies to release SHA256 `10B2A4458677DB70E8917BC23685549A00C9B3B88065CDA506DCCD0E8EE1270D`.
This is **owner-run visual/interaction evidence**, not agent-executed desktop testing or additional
live-query, native-suite, exhaustive fault-case, deployment, installer or material-risk acceptance
evidence. The earlier implementation-handoff checklist below is preserved, not newly claimed as an
itemized completed test matrix.

The owner explicitly authorized this documentation closeout and one complete local commit of the
accepted implementation/tests and closeout documentation, **not a push**. Inspection confirms the
five tracked frontend diffs and four new frontend files match the retained accepted evidence;
no implementation changes are made for closeout. Existing **48 tests passed / zero failures/skips**,
typecheck, changed-file lint, frontend build, offline release build and tracked whitespace evidence
below are reused, not rerun. Initial fresh/clean AFT and later filesystem deadlines remain distinct;
broader exceptions are retained. No new suites/builds/AFT requests/security review or sidecar publish.

Closeout verification covers exact paths/hunks, complete staged diff/file list/whitespace, normal
hooks and post-commit contents/index/working tree. Evidence: local `evidence\commit-closeout\`.
Only S22-009 is reconciled in the local Windows backlog export
`C:\dev\kst_v2_tmp\Stage22\STAGE_22_READ_ONLY_INVENTORY_REVIEW.md`; that export stays outside Git.
The pre-existing `src/frontend/tsconfig.tsbuildinfo` remains byte-identical, unstaged and excluded.
Logs, caches, evidence archives, secrets and published binaries are not included. No installs, live
database queries, database changes, QXtend submissions, deployment, push or subsequent feature.

## Scope and decisions

Owner-authorized presentation/filtering amendment to accepted Stage 15. Authority: `AGENTS.md`,
current project status, Stage 15 source/decision ledger and closeout, accepted S22-005 Open Orders
and S22-010 Workspace Shipments implementations. No separate design/discovery checkpoint.

- `FinishedGoodsPanel` adopts the existing caption/Refresh, themed striped table, sticky headers,
  bounded scroll frame, density tokens and counts/paging footer conventions. Exactly five columns.
- Product, Location and Lot use Values / case-insensitive **literal** Contains; no trimming,
  wildcards or regex. On Hand and Value use exact-decimal Equals / Greater than / Less than /
  inclusive Between on their existing source strings, before formatting. No JS floating-point
  conversion of inventory quantities or values. Value remains USD with two displayed places.
- The small `finishedGoods/FinishedGoodsFilter.tsx` adapter reuses **unchanged** shared
  `ColumnFilterEditor` / `AnchoredPopover`, generated predicate shape and existing exact-decimal
  utilities. No duplicated business DTO, shared-component refactor or new dependency/capability.
- Predicates AND across the complete loaded report before pagination. Cascading facets omit only
  their own column predicate, counting physical rows including separate blank counts. Null and
  empty text share the blank choice; literal `(Blanks)` stays an ordinary distinct value. Values
  are exact/case-sensitive selections; Contains remains case-insensitive.
- Existing editor conventions: 1,000 most-frequent values (ordinal tie-break), searchable list,
  retained hidden/zero-count selections, up to 1,000 value exceptions, search bulk action explicitly
  limited to displayed matches. Global Select all is compact exclude-empty, including unseen values,
  not a truncated include list. Explicit Apply / Cancel / Clear, active funnel labels/fill/underline,
  clear-all, portal placement, Tab containment, Escape/outside dismissal and focus return remain.
- Visit-local filters survive Refresh but are not stored. Scope-keyed sessions still isolate
  workspace/MPS/site changes. Only page size persists: `kst.finishedGoods.pageSize.v1`, separate
  from Open Orders/Shipments and filters. Exact supported strings **100 / 500 / 1000** only; default
  100; guarded reads/writes cannot prevent session use. Resizing uses
  `floor(currentClampedPage * oldSize / newSize)` to retain the former first row inside the new page.
  Filter/clear/successful acquisition reset to the first page; effective pages clamp to valid bounds.
  Empty results show Rows 0 of 0 / Page 1 of 1 with disabled navigation.

Unchanged: acquisition/refresh/process cache, stale/error/empty states and acquisition timestamp,
workspace/MPS/site/USD validation, request-token and mounted-session obsolete-response protection,
positive-on-hand population, physical row IDs and accepted Product/Location/Lot/Reference/raw-location/
row-ID ordering, exact quantities/valuation, missing-price tooltip, RMA indication and reference titles.
No grouping/deduplication, new sorting/export, disk view, source read on filtering/faceting/paging,
source SQL, backend/API/generated contract, valuation or locked Stage 9–11 algorithm change.

## Verification and retained evidence

Full local evidence: `C:\dev\kst_v2_tmp\Stage22\FinishedGoods\evidence\`.

- Final **48 passed / zero failures / zero skips, 5 files** (`frontend-final.json`): Finished Goods
  panel, predicates/facets, adapter, page-size preference and CustomerWorkspace lifecycle regressions.
  Synthetic/offline only. Includes 1,205+ rows, matches beyond page/cap, combined predicates,
  own-column-excluded cascading counts, blanks/literal characters, hidden/zero-count/capped
  selections and global-all versus displayed-search semantics; exact boundaries with identical
  rounded display/Number precision, duplicate-looking physical rows/DOM identity and order;
  all sizes/resizing/empty/clamping/refresh, actual throwing storage, preference separation,
  keyboard/focus/portal behavior, stale refresh and obsolete/mismatched workspace/MPS/site/USD replies.
- `npm run typecheck`, zero-warning changed-file ESLint, `npm run build` and
  `cargo build --release --features tauri/custom-protocol --locked --offline` **PASS**.
  Build/type/lint/release logs retained. Final runtime code preceded the frontend/release build;
  subsequent changes were tests/documentation only. Tracked `git diff --check` passes.
- Initial scoped AFT diagnostics: **FRESH**, 8/8 files authoritative, zero errors/warnings. Later
  requests failed with filesystem `read_deadline_exceeded`; no fresh final whole-scope claim.
  Actual compiler/tests/builds are the verification gate. Existing broader C#/Rust format and
  locked Stage 10 full-lint exceptions remain; no unchanged whole-repository suite was rerun.
  Existing Vite mixed-import and >500 kB chunk warnings remain, not build failures.
- Initial npm invocation rejected forwarded runner flags; used installed Vitest directly with the
  same runner/options. Initial high-cardinality test hit its default DOM wait; a later run competing
  with the frontend build hit the 5-second test limit. Both failed JSON reports retained. Final tests
  use deterministic editor debounce timing/reduced redundant large-DOM toggles, with unchanged
  production editor and unchanged test timeouts; final bounded-worker run did not compete with build.
- No shared implementation changed, so no additional shared-component regression suite was required.
  Backend/native test/source-disabled startup evidence from the accepted Stage 15 / S22-005 baseline
  is reused at its original depth, not represented as newly executed. No sidecar republish required.

## Security and preservation

Screened actual changed paths/hunks against `SECURITY.md`, Security Assurance Policy §6 and
AI Security Review triggers. Only local presentation, bounded exact predicates/facets and a
whitelisted optional page-size preference changed. Source text stays React text/attributes, not
HTML, SQL, paths or commands. No trust boundary, network/credentials/database, filesystem privilege,
export/subprocess authority, external service, dependency, CSP/capability, installer or deployment
change. No additional independent/full security review triggered; this is a bounded self-screen,
not independent review or material-risk acceptance.

Only pre-entry modification: `src/frontend/tsconfig.tsbuildinfo`, restored byte-for-byte after build,
SHA256 `2B7072CD8435CA881270DD179D438029DC42D8F3685004BCF6348D5FDCCD983A`.
During implementation, no staging, commits, pushes, installs, agent live database queries, database
changes, submissions, app launch or deployment occurred. The later local commit authorization is
recorded above. Evidence/binaries remain local, outside tracked source. No native manifest,
schema or generated-contract drift. Sidecar source/publication unchanged; binaries/release copies
remain 127,524,006 bytes, SHA256
`DD92C835760B90D88EB5516E522B0EA15DDD53B663D326A4A09B4ABBCD209FD7`.

## Accepted review executable and historical handoff checklist

`C:\dev\kst_v2\src\tauri\target\release\kst-tauri.exe` — **15,302,656 bytes**,
SHA256 **`10B2A4458677DB70E8917BC23685549A00C9B3B88065CDA506DCCD0E8EE1270D`**.
Application manifest remains alpha.21 (Windows executable resource version remains `0.1.0`).
Final assets `index-BlGNTfY1.js`, `index-B7At9X7k.css`, `event-Dat0o0Mk.js` were built before the
offline release compile and their names verified embedded. Exact hashes/sidecar/preservation checks:
`ARTIFACTS.json`. Use this **release** executable, not a running old/debug/dev-server instance.

Implementation-handoff checklist — not agent-executed. Subsequent owner acceptance is recorded above;
it does not assert itemized completion of every case. No new live-query authority is inferred:
1. Open Finished Goods in a normal loaded workspace. Check light/dark, compact/comfortable and
   narrow-window layout: caption/footer, stripes, sticky header and bounded horizontal/vertical scroll.
2. Exercise all five funnels: Values/search/blanks, literal Contains, exact numeric conditions;
   combine columns, check cascading counts and active indicators; Apply/Cancel/Clear/clear-all.
3. Use 100/500/1000 and later pages; resize and confirm the former first row remains within the new
   page. Filter to fewer/no rows, clear, Refresh and revisit; only page size should persist on revisit.
4. Check keyboard Tab/Shift+Tab/Escape/focus return, RMA/reference/missing-price indications and
   unchanged stale/error behavior where safely available. Switch workspace/MPS and verify no old scope.

At implementation handoff, owner review/acceptance was pending and no commit/push was authorized.
The owner has now accepted S22-009 and authorized its local closeout commit as recorded above.
S22-009 is closed; Stage 22 remains open. No push or subsequent feature is authorized by this closeout.

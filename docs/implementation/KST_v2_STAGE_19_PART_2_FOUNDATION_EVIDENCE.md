# Stage 19 Part 2 — implementation and verification evidence

Date: 2026-10-13. **Part 2 and Stage 19 COMPLETE / OWNER-ACCEPTED — 2026-10-13.** The owner accepted
the integrated Workspace Shipments behavior after implementation. Offline tests, owner-run observations,
sidecar metadata and source inspection are recorded separately below; acceptance does not strengthen
those evidence types into exhaustive live verification.

## Delivered and inspected

Workspace Shipments now includes a full-acquisition On Time numerator/denominator summary. The
existing disk-backed acquisition loop counts rows whose existing On Time cell is `1` or `0`, with
each physical invoice detail counted once; unavailable values are excluded. The operation DTO and
generated OpenAPI/TypeScript contract expose counts; the UI formats their ratio to one decimal place,
shows `—` for a zero denominator, retains the value during paging, and clears it when replacing or
leaving the operation. This adds no SQL path, query or frontend history collection.

Owner-run correction observation and exact cause, the reused authoritative rule, parameterization and
large-scope strategy are recorded in `KST_v2_STAGE_19_PART_2_SCOPE_CORRECTION.md`.

C# request/spec/routes → build-time OpenAPI → generated TypeScript preceded production foundation
implementation. Temporary contract-only route placeholders were replaced before verification/handoff.
Shared operation/page/column C# contracts are reused; no handwritten frontend business DTO/catalog.

Scope discovery inspected WorkspaceAssignment, workspace DTOs, configuration service/store, site/domain
map, CustomerWorkspace and ApplicationShell. Current assignments contain no customer/Corp Group
fields. The owner-run scope observation corrected the original assumption that product lines/parent
parts were ignored. Workspace Shipments now reuses `QadMpsScopeResolver` on every acquisition; the
authoritative MPS rule is product-line discovery union explicitly configured parents, without an MPS
snapshot dependency. The resolved set is parameterized and enforced by the QAD query before the frozen
operation result is published. Customer/group and display labels are not inferred.

Dedicated application service and store wrapper reuse Part 1's disk-backed immutable projection,
bounded pages, cancellation/tombstones, expiry and cleanup. Separate workspace root/store plus
assignment/visit owner isolates Reports and other visits. Optional exact recorded part and independent
inclusive Ship Date bounds use distinct query path; historical query still uses Invoice Date and
physical ID ordering. Shared parser/header association/retro values/On Time remain unchanged.
Default projection is precisely the final fifteen columns, with no Currency/Due Date/Required Date.

Client uses established loopback transport on browser/desktop, avoiding native Reports generation
ownership. It explicitly aborts/releases old operations and rejects late replies; no retained history
cache or page collection. Date initialization uses local calendar fields/setDate, not UTC truncation.

## Executed evidence

- Contract build: `dotnet build Kst.slnx --no-restore --nologo` from `src/backend`: passed,
  zero warnings/errors; OpenAPI generated. Initial attempt at repository root failed because the
  solution lives in `src/backend`; corrected without repository changes.
- `npm run generate:types` from `src/frontend`: passed; generated contract synchronized.
- Final solution build after scope correction: `dotnet build Kst.slnx --no-restore --nologo` passed,
  zero warnings/errors; OpenAPI regenerated. `npm run generate:types` rerun after publish-build
  regeneration: passed.
- Final QAD affected filter (`WorkspaceShipments|QadReadOnlySqlTests|QadMpsScopeResolverTests|
  HistoricalShipments`): **34 passed**. Covers parameterized XML parent membership, 1,200-parent
  complete serialization, empty-scope fail-closed behavior, accepted resolver tests, shipment SQL and
  production read-only guard.
- Final API affected filter (`WorkspaceShipmentsTests|HistoricalShipmentsTests|MpsWorkspace|
  LongTermShortagesPanel`): **21 passed**. Includes same-site/different membership, product-line UNION
  explicit, outside exact item, blank and exact member, leading zeros, date boundaries, empty scope,
  config refresh with immutable old pages, unregistered/replayed state guards, concurrent duplicate
  acquisition, Reports/Part 1 isolation and late cancellation.
- Architecture tests: **10 passed**. `dotnet build Kst.slnx --no-restore --nologo`: passed, no warnings/errors.
- Frontend focused suites: **4 files passed; 109 tests passed, 3 skipped** (WorkspaceShipmentsPanel,
  CustomerWorkspace, MpsWorkspace and LongTermShortagesPanel); the two previously timing-out suites
  passed in this run. Frontend typecheck and scoped ESLint passed.
- On Time Rate follow-up: API/store fixtures passed for physical-row 1/0/1/null/null counts, zero and
  100 percent, no eligible rows, unequal quantities, and first-page operation metadata; full scoped
  API shipment filter passed **22 tests**. Frontend WorkspaceShipmentsPanel + CustomerWorkspace passed
  **19 tests** (13 panel, 6 workspace), covering 66.7% independent of page contents, paging retention,
  replacement clearing, zero/100/unavailable formatting and late workspace-change replies. Typecheck
  passed. XML extraction-width safeguard fixture and 1,200-parent query test passed (2 tests).
- Scoped `dotnet format ... --verify-no-changes --include` passed for changed backend files after
  normalizing the new QAD file to repository CRLF. `git diff --check` passed (line-ending notices on
  other pre-existing files only).
- Initial foundation backend filter: **54 passed** (QAD 32, Architecture 6, API 16).
- Final expanded affected filter (`HistoricalShipments|WorkspaceShipments|ReadOnly|DependencyRuleTests|
  WorkspaceConfiguration|FinishedGoods`, fully-qualified names): **106 passed**, no failures/skips
  (Domain 14, Application 25, QAD 42, Architecture 6, API 19). No full-suite claim.
- API fixtures prove server-resolved current persisted scope, no MPS snapshot requirement, exact identifiers/retro/null cells,
  fifteen headers, physical-detail paging, workspace/visit/report isolation, immutable refresh identity,
  bounded pages, unavailable/archived assignments, invalid filters, no export routes, cancellation
  before registration and cancellation with an uncooperative late reader.
- Direct dedicated-store fixture proves row/index files removed on release while same-ID Report
  result remains available. Existing Part 1 tests cover timer expiry and broader disk/workbook limits.
- QAD construction fixtures cover all six exact site/domain pairs, blank/literal/leading-zero/wildcard
  part, Ship versus Invoice Date, independent inclusive bounds, next-midnight end timestamps,
  MaxValue date, all-history/null eligibility, raw descending Ship Date + physical tie-breaker and
  unchanged Reports filter/order. New query is included in the read-only SQL guard.
- Frontend final scoped tests: **26 passed** (Workspace transport 8, Reports transport 3, accepted
  Historical Shipments UI 15). Local date fixture expectations cover year/leap boundaries; lifecycle
  fixtures cover departure, cancellation, replacement, late pages and late old acquisition replies.
- `npm run typecheck`: passed. `npx --no-install eslint` on both new frontend files: passed.
- Scoped new-C# formatting verification passed; API test addition formatted using the same scoped
  workflow. `git diff --check`: passed (existing CRLF conversion notices only).
- Scoped AFT diagnostics: fresh, zero errors/warnings; global analysis categories were stale/not
  computed for scope, not asserted clean. Compiler/tests remain the verification gates.

## Focused change/evidence boundaries

New production files: WorkspaceShipmentsFilter, WorkspaceShipmentsService, FileWorkspaceShipmentsStore,
WorkspaceShipmentsDtos/Endpoints and `workspaceShipmentsApi.ts`; two backend test files and one frontend
test file. Shared edits: query-core reuse in QadHistoricalShipmentsReader, composition/routes in
Program, test factory and read-only guard. OpenAPI/TypeScript regenerated; plan/API/handoff/evidence
and current status updated. Existing uncommitted Part 1/native/schema work retained.

Focused Phase A review packet under `C:\Dev\kst_v2_tmp\Stage 19\Part2-PhaseA` contains snapshot-relative
diffs for shared files and complete new files, avoiding conflation with pre-existing Part 1 changes.
Delivered ZIP is checked below 5 MB. No installs, dependency/capability changes, live reads,
company writes, fetches, commits or pushes.

## Independent review, release and owner checks

Separate-context security review found an acquisition-capacity flaw and then a duplicate-acquisition
release race. Both were corrected: registered owner/state is checked before resolver work; scope
resolution is deferred until the store begins owned acquisition; service-level failure cleanup no longer
releases a competing request; invalid criteria validate before workspace/QAD lookup. The independent
reviewer rechecked the final source and found no remaining blocker in the reviewed path. Focused tests
verify the unknown-ID/no-resolver, invalid filter, and concurrent duplicate cases. Review was offline;
no live SQL Server query was made.

The repository sidecar workflow `scripts/build-sidecar.ps1` completed after the On Time Rate source
change during the alpha.18 implementation phase. That historical artifact identity was superseded
by the final alpha.19 closeout build; see `KST_v2_STAGE_19_CLOSEOUT.md` for the current size/hash.
Offline republish/copy does not establish a Tauri packaged-app or connected QAD run.

Owner-run accepted screen observation: owner reports corrected workspace scoping works and the screen
looks acceptable; owner accepted Stage 19 Part 2 and Stage 19 overall on 2026-10-13. This observation is
not a claim that every filter, site, or source row was live-compared.

Owner retest reference: open Workspace Shipments in two same-site workspaces with distinct product-line and
explicit-parent scopes; verify blank Part Number returns only each resolved population, exact member
works and an out-of-scope exact part is empty, product-line+explicit membership follows the accepted
union, and Ship Date boundary behavior remains unchanged. Change assignment configuration and Refresh;
verify the new operation changes scope while paging the previous completed result stays stable. Include
historical explicit items without current master data, leading-zero IDs, empty product-line membership,
workspace switching during retrieval, Apply vs Refresh, ninety-day new-visit defaults, fixed fifteen
columns and separate Reports ownership. This corrects the earlier navigation/filter handoff: do not
expect all-site rows. Owner-connected QAD and packaged Tauri behavior are separate from offline evidence.

SQL construction/parser/mocked result fixtures do not execute SQL Server or prove XML parameter
execution, live sort/collation,
historical coverage/performance or company database grants. Ordering fixture rows are an independently
stated expected acquired order, not execution of QAD SQL. Immutable consistency starts only after
complete acquisition; it is not a transactional QAD snapshot. Part 1's accepted source/cleanup limits,
known broad formatting, locked Stage 10 lint and incomplete AFT exceptions remain.

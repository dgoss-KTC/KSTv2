# Stage 19 Part 1 foundation — scoped evidence

Date: 2026-10-13. Historical implementation/evidence record. Part 1 and Stage 19 overall were
subsequently COMPLETE / OWNER-ACCEPTED on 2026-10-13; see `KST_v2_STAGE_19_CLOSEOUT.md`. The scoped
offline results below retain their original verification boundaries.

## Implemented

Required backend site/domain and accepted historical filters; parameterized membership EXISTS;
source-supported domain/invoice header association with whole-acquisition zero/multiple failure;
physical detail IDs and signed quantities; unavailable optional master enrichment; 36 selectable
data-bearing fields, accurate recorded/current labels, all removed traceability absent. Recorded
UOM is explicitly literal/informational only. Extended value uses exact arbitrary-precision decimal
quantity × recorded price (not SQL rounded multiplication or revenue). Monetary selection adds currency.

Process-owned disk rows and fixed-width disk offsets, immutable selected projection, bounded pages,
two acquisition/preparation workers and eight active operations. No history row/byte truncation.
Expiry timer, cancellation tombstones (including cancellation before registration), late-publication
rejection, release and failure cleanup with retry of transient file cleanup errors.
Workbook writes to an unpublished pending file, then atomically becomes available after success.

Streaming ZIP/XML XLSX, identifiers/literal strings, no formula cells; exact decimal text beyond
15 significant digits. Worksheet/cell capacity failures are explicit. No quantity/currency totals.
Native fixed-route streaming transfer to delete-on-close held file; worker/grant Arc ownership;
bounded streaming staging/content comparison through accepted native publication/overwrite path.
Host preparation/grant expiry releases the matching payload; departure keeps active save ownership.

## Executed checks

- `dotnet build Kst.slnx --no-restore --nologo`: succeeded, 0 warnings/errors; OpenAPI generated.
- Generated TypeScript using repository `npm run generate:types`.
- Final scoped backend filter `HistoricalShipments|OpenOrderReport|DependencyRuleTests`:
  **44 passed** (Domain 16, QAD 10, Application 1, architecture 6, API 11); no failures/skips.
  Stage 19 itself: 4 QAD parser tests + 9 API/store/workbook tests. Prior report and architecture
  tests remain part of the scoped total; no claim of a full solution test run.
- Synthetic acquired/exported result: 12,001 distinct physical details with equal business identifiers;
  last page and workbook row count verified. Signed RMA/memo/ADJ/null enrichment fixtures retained.
- Another 12,000-row synthetic workbook exceeded 8 MiB; full ZIP worksheet row count and declared
  file length verified. No raised Stage 18 limits.
- Independent inspection: raw ZIP/XML row count and absence of formulas; ClosedXML reopening checked
  text identifiers, signed numeric quantity, precise monetary strings, literal formula-looking text,
  currency, header and last used row.
- Row limit fixture attempts 1,048,576 detail rows; explicit whole-workbook capacity failure.
  32,768-character cell fixture fails without truncation; cancelled/failed preparations remove files.
- Release while an uncooperative fixture produces a late row prevents publication/removes files;
  timer expiry removes files without another client request; cancel-before-register forbids resurrection.
- SQL assertions: mandatory site/domain per all six sites, parameterized blank/single-bound/inclusive
  dates, historical customer and all range fields, global/restricted relation predicate, EXISTS,
  no multiplying enrichment/header tie-breaker, no uninvoiced/legacy channel exclusions.
  These are **SQL construction checks**, not executed SQL Server semantic/live-data validation.
- Frontend transport tests: **5 passed** (Stage 19 3, existing Open Orders 2). Late native replies,
  known-operation browser cancellation/release and owner-consistent bounded paging covered.
- `npm run typecheck` and changed-helper ESLint: passed after correcting test-only ES library mismatch.
- `cargo check --locked --offline`: succeeded; an initial unused import warning was corrected.
- Final `cargo test --locked --offline` with `KST_MRP_TEST_ROOT` under supplied Stage 19 temporary
  directory: **23 passed, 2 intentionally ignored** (interactive chooser/default association).
  Includes >8 MiB file-backed publication/verification/collision/replace and delete-on-close payload
  ownership across departure. Earlier runs failed from missing fixture env and nested-shell expansion;
  resolved with direct native PowerShell syntax. These failures are not counted as passes.
- Scoped new-file `dotnet format ... --verify-no-changes --include ...`: passed.
  Changed three native files `rustfmt --check`: passed. Final generated-type regeneration passed.
  `git diff --check`: passed. No broad formatting/lint pass is claimed.

## Limits and exceptions

## Final integration (2026-10-13)

The owner accepted Part 1 and Stage 19 overall on 2026-10-13 after reviewing the integrated screen.
The closeout record distinguishes this owner acceptance from the offline tests and owner-run UI
observations; acceptance does not assert exhaustive live parity or performance.

### Owner-defined backend default amendment (2026-10-13)

Subsequent owner amendment: Due Date and Required Date are optional, reducing defaults to 15.
Owner explicitly confirmed that Currency is optional even with monetary columns; automatic inclusion
was removed. Prior exact-default preferences migrate to the revised defaults; custom selections retain
optional choices. On Time displays accessible green ✓ / red ✕ for 1 / 0; null remains — and XLSX retains
numeric/null values. Offline: 15 UI tests, 12 API/store/workbook tests passed; 21 reader/read-only tests
passed. Scoped frontend lint/typecheck passed; sidecar republished. No live verification.

- Implemented the 17 ordered defaults in the shared Domain catalog; `/spec` exposes catalog order
  independently of the fixed acquisition schema. Added Required Date, recorded retro price/exact extended
  value, raw-datetime SQL On Time flag and optional detail Sales Order. Original invoice prices remain distinct.
- Filter repair preserved; owner confirms it works. No new filter policy, sample-query restrictions or live reads.
- Offline checks: 21 QAD/parser/read-only tests and 12 API/store/workbook tests passed. Price fixtures use
  original 99 versus retro 2.125, null retro and small exact signed values; independent ZIP/XML inspection
  proves page/workbook default headers, values, full identifiers and Currency agree.
- On Time fixtures cover earlier/equal/later dates, same-date differing times, missing dates and non-Standard
  channels. They are independently stated SQL-result/parser fixtures plus assertions on the emitted CASE;
  they do **not** demonstrate executed SQL Server semantics.
- Separate focused security/access-path review after implementation: no blocking findings. Site/domain,
  parameterization, header-driven filter path, physical/header guards and bounded processing retained.
- Scoped backend formatting verification passed. Sidecar republished via `scripts/build-sidecar.ps1`
  (127,691,396 bytes), copied to `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`.
  OpenAPI/generated TypeScript synchronized; public DTO shapes unchanged; frontend typecheck passed.
  AFT scoped diagnostics clean, complexity incomplete. No live database or connected desktop verification.
- Supersedes the missing-backend-defaults dependency noted in the earlier width amendment below.

### Report-width and default-restoration amendment

- Historical Shipments alone overrides the inherited 780px report body cap; flex parents permit
  shrinkage and the table scrolls internally with readable column minimums. Other reports retain their sizing.
- Column restoration migrates a stored set matching the original ten defaults to the current backend
  catalog defaults; recognized custom selections are retained in catalog order. Restore default columns
  persists the catalog selection and invalidates the old result before reacquisition/export.
- Offline mocked Chrome inspection: at 1920px the body/table both measured 1627px; at 900px both
  measured 806px, with 1410px table content and working keyboard horizontal scrolling. Parent containers
  did not expand to the table's intrinsic width. Screenshots and metrics:
  `C:\dev\kst_v2_tmp\Stage 19\historical-layout-{1920,900}.png` and `historical-layout-metrics.json`.
- 44 affected frontend tests, scoped ESLint and production build passed (existing Vite bundle warnings).
  AFT scoped diagnostics were clean; complexity analysis remained incomplete.
- New query-defined defaults are **not verified/implemented in the backend catalog yet**. Owner supplied
  query includes Required Date, retro price/extended value and On Time, absent from the current catalog;
  current catalog still defaults to the original ten columns and uses `idh_price`. Backend mapping/default
  amendment is required before the frontend can consume and verify the requested new set. No inferred
  backend semantics or workspace lookup were introduced by this presentation amendment.

- UI: `src/frontend/src/reports/HistoricalShipmentsReport.tsx`, `historicalShipmentsPreferences.ts`, tests, ReportsPanel entry.
- Independent security review (separate reviewer agent, after implementation): **no blocking findings** across SQL/read-only,
  result ownership, temp files, native save/open, transport. Non-blocking observation 1 (orphaned backend result
  directories after a hard kill) was corrected: startup sweep of abandoned store roots older than 2 hours, with a test.
  Remaining non-blocking observations: Tauri workbook transfer is bounded only by the XLSX row cap (trusted sidecar);
  `ArgumentException` detail echoed from workbook writer; redundant `remove_file` in `HeldFile::drop`; default ACLs.
- The full backend run exposed the S0.5 read-only SQL guard lacking a Stage 19 filter generator; added one
  (`QadReadOnlySqlTests`). Guard now covers `QadHistoricalShipmentsReader.BuildQuery`.
- Full backend suite after correction: Domain 358, Application 412, API 218, Architecture 10 passed; QAD suite 305 with the one
  failure fixed, then re-run (ReadOnly + Historical Shipments filter: 11 passed).
- Frontend: typecheck passes; full test run had timeout failures in unrelated Planning tests under load; those files
  and the Reports/Stage 19 files pass in isolation. Lint shows only the two known Stage 10 `setState-in-effect` errors.
- Sidecar republished with `scripts/build-sidecar.ps1` (`src/tauri/binaries` is git-ignored); contracts regenerated and in sync.
- Not verified: connected desktop flow, live QAD semantics, live performance/retention, interactive Save As.

## Limits and exceptions

At the original backend-foundation checkpoint there were no live reads, metadata rereads,
retention/performance claims, UI implementation, connected desktop verification, external import,
dependency/capability admission, database objects, commits or pushes. Subsequent integrated UI and
sidecar work is recorded in the Stage 19 closeout and Part 2 evidence.
Source association remains domain/invoice only; any unresolved header in selected detail site/domain
and detail filters fails acquisition, even when header filters cannot be evaluated. This deliberately
avoids silently dropping unresolved details. Optional enrichment ambiguity yields unavailable values.

AFT inspection remained PARTIAL: rust-analyzer absent from installed toolchain, intermittent server/
freshness/analysis-budget gaps. Compiler/tests are the executed gates, not an invented AFT pass.
Known broad .NET/Rust formatting and locked Stage 10 lint exceptions remain exceptions and were not
reclassified or remediated. Accepted Stage 18 closeout limitations stand.
Backend process-crash recovery of previously orphaned process directories is not demonstrated;
normal release, timer expiry, cancellation, failure and graceful disposal are covered. Native held
preparations use Windows delete-on-close. Full-history acquisition is not a transactional QAD snapshot;
immutable consistency begins only when complete acquisition is frozen. No live performance assertion.

Initial modified generated Tauri schemas and supplied untracked discovery/source remain preserved.
Version was alpha.18 during this historical implementation phase; Stage 19 closeout advances the
stage-aligned application version to alpha.19. UI handoff is
`docs/implementation/KST_v2_STAGE_19_PART_1_UI_HANDOFF.md`.

# Stage 21 — analytical exports, bounded inventory and evidence

Status: **COMPLETE / OWNER-ACCEPTED — 2026-10-14.**

Latest presentation amendment below supersedes initial metadata-sheet/layout descriptions in this
checkpoint record: no Export information worksheet; MPS quantity rows only, compact date headers
colored by fiscal period; trailing decimal scale does not force numeric values into text.
Owner subsequently removed Planning analytical Excel export for future iterative improvements outside
Stage 22. Only MPS/BOM analytical exports remain in this implementation; Planning export evidence below
is historical.
Owner expressly accepts Stage 21 with the final delivered scope and retained open items below.
This closeout does not authorize another stage, the Planning redesign or remediation, establish release
readiness, or resolve/accept security risk.

## Owner acceptance and final scope — 2026-10-14

**Owner-reported review:** the owner reports checking the reports and working through several fixes
with the coding agent, and explicitly accepts Stage 21. Earlier owner feedback confirmed saving works.
This is owner-reported review, separate from synthetic/mocked automation and artifact identity checks;
it does not establish a particular desktop interaction matrix, live-query comparison or numerical coverage.

Accepted final delivery: corrected MPS (all workspace parents) and selected-parent BOM analytical XLSX;
Finished Goods requires no export because copying from the report screen is sufficient. Planning
analytical export was removed, and its complete redesign is deferred to **future iterative improvements,
not Stage 22**. The existing Planning Mass Updater CSV is preserved.

**Retained open items and limits:**

* Export-button placement/presentation is an acknowledged usability issue deferred to later optimization;
  no usability repair or later work is authorized by this closeout.
* Inventory rows explicitly marked unresolved below remain unresolved. Acceptance closes the delivered
  Stage 21 scope with those recorded exceptions; it does not imply no-export decisions for other areas.
* Pkcs 9.0.4 dependency advisories remain unresolved and require separate security disposition; Stage 21
  acceptance is not risk acceptance, remediation approval or a change to security policy.
* The Stage 11/13 browser-save feedback discrepancy remains unresolved, as do the separately recorded
  inherited host/save limitations. No finding is closed by overall stage acceptance.
* Automated verification exceptions and unverified behavior remain as documented below. No exhaustive
  desktop, live parity, installer/printing, crash/power-loss or restart-recovery claim is added.

This documentation-only closeout records the owner's decision; it runs no new application tests,
builds, sidecar publication, independent security review or live database queries. Prior amendment
instructions to seek owner review are historical; the above acceptance supersedes their pending status.

## Scope and authority

Authority: `AGENTS.md`, enacted security policies, current status/checklist, Stage 14 source/decision
ledger (including the later $0 missing-list-price amendment), accepted Stage 18 Component MRP/save
foundation and Stage 19 closeouts. Stage 19 is accepted; Stage 20 retired. Active version advances to
`0.1.0-alpha.21` under the existing stage/version policy; numeric installer remains `0.1.0`.

Implementation sequence: inspect loaded views/contracts and accepted save foundation; add authoritative
C# transport DTOs and literal workbook presentation model; build OpenAPI/generate TS; integrate the
initial three actions; inspect synthetic workbooks and lifecycle regressions; independent security review
and correction; scoped checks and matching sidecar publication; later remove Planning per owner direction.
No source acquisition is part of preparation.

## Bounded retained-area inventory

Audience is the scheduler/analyst unless stated otherwise. Operational files require external human
processing; none implies QAD applied changes. “Unresolved” below is an inventory disposition awaiting
the owner, not authorization for additional exports. These retained exceptions do not reopen acceptance
of the final two delivered analytical exports.

| Retained area | Actual coverage and scope | Disposition |
|---|---|---|
| Dashboard / MPS | New analytical XLSX; loaded snapshot, Due/Release basis, current horizon; always all workspace parents (owner amendment 2026-10-14). One quantity row per parent with Description, Falldown and fiscal-period-colored date headers. | Delivered / owner-accepted 2026-10-14 |
| Selected-parent BOM | New analytical XLSX; loaded parent, Component Item + Description + P/M + Phantom filters (AND). Nine displayed fields; order, gaps, repeats and phantoms retained. Whole numbers have no trailing points; Level is left-aligned. | Delivered / owner-accepted 2026-10-14 |
| Planning | Analytical XLSX removed at owner direction; complete redesign deferred to future iterative improvements outside Stage 22. Existing Planning features and operational CSV remain. | Deferred by owner; no analytical export action |
| Planning Mass Updater | Retained operational CSV, Part/Qty/Date without header; signed changes; full-loaded-MPS validation; separate save/Clear-or-Keep workflow. | Accepted Stage 14 retained |
| Customer Open Orders | Retained analytical XLSX of filtered line keys and visible report columns, independent of page; retained operational QXtend date/quantity/price CSVs with fresh targeted validation. | Accepted Stage 13 retained |
| Workspace Shortages | Retained analytical XLSX of displayed filtered/sorted component population, current projection/horizon/receipt choices; locked Stage 11 projection. | Accepted Stage 11 retained |
| Finished Goods | Screen copying meets owner need. | **Owner decision: no export required** |
| Reports: Component MRP | Analytical XLSX, selected direct part/site, horizon, Day/Week/calendar Month and receipt choice; five metric rows. | Accepted Stage 18 retained |
| Reports: Shipments-To-Go | Analytical XLSX, selected sites and optional next week; accepted legacy presentation. | Accepted Stage 18 retained |
| Reports: S&OP | Analytical XLSX, site/ranges/grouping and fiscal presentation from its frozen acquisition. | Accepted Stage 18 retained |
| Reports: Open Order Report | Analytical XLSX, selected filters and columns, complete acquired result. | Accepted Stage 18 retained |
| Reports: Historical Shipments | Analytical XLSX, complete immutable filtered acquisition and selected catalog columns, not the visible page. | Accepted Stage 19 retained |
| Workspace Shipments | Report screen and full-result On Time Rate; no XLSX action in current workspace screen. Reports Historical Shipments is separate scope. | **Unresolved owner export/no-export disposition** |
| Work Orders / kitting / immediate shortages / candidate drilldowns | Screen investigation; no dedicated analytical export action found. Standalone Component MRP and Workspace Shortages are different grains. | **Unresolved owner disposition** |
| Component Orders | Informational PO/receipt/buyer context; no dedicated export action found. | **Unresolved owner disposition** |
| Part Info / Component Information / inventory locations / approved alternates | Informational panels/drawer; no dedicated export actions found. BOM export does not claim to include these independent details. | **Unresolved owner disposition** |
| General/settings, workspace configuration and management | Configuration UI; no user export action. | **Unresolved owner disposition** |

## Bounded consistency assessment

* New exports reuse the concrete Reports host save foundation, admitted ClosedXML, existing kind-bound
  prepared bytes and artifact grants. No webview shell authority, dependency, capability, DB object,
  network listener or source-query change. Literal source text never enters `FormulaA1`.
* Final new filenames: `MPS|BOM_<site>_<yyyy-MM-dd>_01.xlsx`; native chooser retains accepted
  destination selection, filename increments, final-candidate overwrite consent, no-replace new files,
  exact-byte publication/readback and owned cleanup. Remembered successful folder and Open preference
  are shared with Reports. Cancel writes nothing and retains prepared bytes; failure/unknown outcomes
  are distinct; captured operation recovery survives view/workspace departure. Optional opening is
  dispatch acknowledgment, not proof a viewer loaded it.
* No Export information worksheet in final delivered workbooks. Earlier metadata-sheet and Planning
  presentation evidence is historical. Important stale warnings remain on the first header as comments.
* Final workbooks have styled headers, frozen identifiers/header, bounded widths, quantity formats,
  literal nulls and no fabricated empty business rows. MPS weekly headings are date cells displayed as
  `m/d/yyyy`, colored by the frontend-provided fiscal period. One quantity row per parent retains
  Description and Falldown; Metric/execution-state rows are removed. BOM Level is left-aligned.
* Numeric precision: >15-digit values are text; values altered by .NET decimal parsing (including tiny
  exact frontend products beyond decimal scale) are text. Exact source strings are retained; no JS
  Number conversion. Currency formatting is presentation only.
* Supported limits: 250,000 data/header cells, 4 sheets at generic writer boundary (final MPS/BOM each use one),
  Excel row/column and 32,767-character limits, existing 8 MiB workbook cache/host transfer, 16 MiB
  analytical host request, two backend preparations concurrently, 30-minute prepared lifetime. Capacity
  failure rejects the whole preparation; no truncation. Cell text limits remain enforced. Native
  generation has the existing two-minute transport timeout.
* Reports retained differences: report-specific filenames, source metadata, layouts, no-data and
  capacities are preserved. Component MRP/STG/S&OP/OOR use bounded byte preparation; Historical
  Shipments has its separate streaming file-backed result and 1,048,575-detail XLSX limit. It does not
  gain the new analytical formatting through this checkpoint.
* Stage 13/11 analytical saves retain dialog/plugin writes rather than migrating to host-held Reports
  artifacts. Their overwrite/cleanup/reconciliation guarantees are not claimed equivalent. Stage 11
  and Stage 13 share `saveLongTermShortagesWorkbook`: its **browser fallback calls initiation “saved”**.
  Source-inspected legacy feedback discrepancy recorded here, not silently treated as confirmed disk
  saving and not repaired inside locked Stage 11. New exports require protected desktop saving and
  provide no browser-download success fallback. Owner disposition for a bounded legacy-feedback fix
  remains pending; no broad migration is proposed.
* Operational CSVs retain accepted bytes, filenames, validation, dialog writes and truthful
  `downloaded` browser wording; no analytical export clears proposals, changes drafts or triggers CSV
  Clear/Keep. Existing native Reports namespace-manipulation/validation-to-opening race limitations
  remain as documented in the Stage 18 closeout.

## Preparation and source compatibility

`POST /api/v1/reports/analytical` accepts the generated C# contract, checks workspace/site/current
loaded MPS identity and business date, and only reads in-memory stores. MPS reuses the accepted pure
builder; BOM reads the exact loaded cache acquisition and applies the accepted display filters. The
stale same-site/effective-date fallback can be exported when identified as stale. Planning analytical
preparation was removed; `Kind=Planning` is rejected. These exports do not change QAD.

Frontend identity captures source, workspace and relevant selections; before Save
it compares captured selections again. Source/basis/horizon transitions disable export until compatible.
Departure/cancel invalidates only the matching host epoch. Already-started native saves retain captured
bytes/host ownership; application-lifetime status reconciliation is independent of preparation visits.

## Verification / evidence

All automated evidence is **synthetic/offline or mocked**, not live business parity. Local artifacts:
`C:\Dev\kst_v2_tmp\Stage21\samples`, `backend-results` (TRX), `frontend-results.json` and host tests.
Seven early usable XLSX samples cover MPS Due/Release and selected/all scopes, filtered/stale and empty
BOM, Planning Report and staged/invalid input. Sample JSON is a synthetic frontend presentation,
subsequently passed through the real workbook writer and reopened for actual-cell inspection.

* Backend full solution: **1,348 passed** (Domain 358, Application 412, QAD construction/read-only 329,
  API 239, architecture 10); no live QAD/shortage reads. Build regenerated OpenAPI at alpha.21.
* Initial focused affected-screen frontend check: 150 total, 147 passed / 3 skipped across five files.
  Later analytical/lifecycle/Planning draft/Reports/workspace checks: **94 passed** across eight files;
  the nonexistent requested hook filename matched no test file, so no hook-test claim is made.
  Actual retained Mass Updater component/pure export tests subsequently passed **61/61**. Final focused
  analytical workbook checks passed **9/9**, with sample-generation environment enabled.
  Final capture/recovery/presentation rerun passed **10/10**, recorded in `final-frontend-results.json`.
* Host tests: initial run failed nine tests because the required synthetic-root environment variable
  was unset; corrected `KST_MRP_TEST_ROOT` run: **23 passed / 2 explicitly ignored**. These include native
  collision/overwrite/publication/cleanup and controlled open dispatch, not integrated app UI.
* Independent separate-context security review found F1 save-recovery ownership and F2 decimal-scale
  integrity defects. Corrections add application-lifetime recovery, explicit pre-dispatch cancellation,
  terminal settlement latch and lossless decimal fallback. Reviewer adjudicated **F1 and F2 Resolved**;
  final focused independent run 6/6 passed; no remaining supported bounded blocker. No risk acceptance.
* Historical full-solution .NET formatting, broad `cargo fmt --check`, locked Stage 10 lint and partial
  C#/Rust AFT diagnostics remain exceptions, not passes. Scoped introduced-file formatting is checked
  after correcting its initial whitespace/EOL failures. Typecheck/scoped lint and final build/publication
  results are recorded in the checkpoint handoff.

### Initial publication and verification — 2026-10-14 (later identities below supersede this build)

Scoped introduced-file `dotnet format --verify-no-changes` passed after whitespace/EOL correction.
Final changed-file ESLint with `--max-warnings 0` passed. Frontend production build passed (retained
dynamic/static import and >500 kB chunk warnings); its TypeScript build passed. `cargo build --locked
--offline` passed after the new sidecar copy. OpenAPI/TS were regenerated by the established commands.
AFT final inspections were interrupted/partial (including a TypeScript diagnostics timeout); diagnostic
gaps are not a clean-analysis claim. The real TypeScript build and scoped lint passed.

`scripts/build-sidecar.ps1` completed and copied the alpha.21 backend to the actual Tauri sidecar path:

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| `publish/backend-sidecar/Kst.Api.exe` | 127,801,988 | `17E6E5169A86DCEE82926D349EC8D3F5ED388AC6766334A424D3CD2FE6B03E63` |
| `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe` | 127,801,988 | `17E6E5169A86DCEE82926D349EC8D3F5ED388AC6766334A424D3CD2FE6B03E63` |

ProductVersion is `0.1.0-alpha.21`. This verifies copied build identity, not a running desktop process.
No desktop app/sidecar was launched for connected testing in this checkpoint.

**Newly surfaced dependency advisories / owner review required:** publication's existing NuGet restore
reported `System.Security.Cryptography.Pkcs` **9.0.4**, NU1902 moderate
`GHSA-2fmv-rv2c-59x7` and NU1903 high `GHSA-846f-f764-76mx`, in API/QAD/Shortages dependency paths.
These are package-tool findings, not proof of application exploitability or accepted risk. No prior
repository disposition for these advisory IDs was found. Dependency manifests were not upgraded;
security disposition remains unresolved independently of the owner's subsequent report review and
Stage 21 acceptance. At publication, desktop-review progression was held pending that disposition;
owner acceptance does not resolve the advisories or establish release readiness. Any bounded
dependency/reachability review and separately admitted remediation require separate authorization. Export
implementation/offline evidence and the bounded F1/F2 review remain separately recorded.

Pre-existing uncommitted Stage 19 reference files and modified `tsconfig.tsbuildinfo` remain outside the
checkpoint's intended source scope. The production frontend build regenerated buildinfo; it was not
reset to HEAD because it already contained owner work at entry. No staging, commit or push occurred.

Not established by agent-run verification: the full integrated native Save As/cancel/overwrite/lost
reply/navigation matrix, actual Excel viewer opening, exhaustive presentation/live parity,
installer/printing, crash/power loss and application-restart recovery. Owner-reported report review and
acceptance are recorded above without expanding their numerical or interaction scope. Unresolved-area
export decisions remain open.

## Review guidance and evidence limits

The owner has accepted Stage 21 as recorded above. The retained guidance below is not an assertion
that each scenario was performed, nor a new acceptance gate.

1. Use a matching alpha.21 sidecar/host. Open sample workbooks first; compare readable widths, headers,
   literal text, exact-value text cells and clear analytical/staged/status labels.
2. MPS: export all parents, drill into one parent and verify export scope stays all with no scope selector;
   compare Due/Release and horizon, Falldown, bucket dates and fiscal-period header colors.
3. BOM: combine filters; compare repeated components, gaps, nulls and phantom values with the loaded
   parent. Check stale warning and empty-filter-result workbook.
4. Planning: no analytical Export Excel action in either mode. Existing proposals/drafts/Mass Updater
   CSV remain; analytical redesign belongs to future iterative improvements outside Stage 22.
5. Prepare, change relevant context and confirm obsolete Save disappears. Cancel/retry Save As; select
   existing destination and decline/accept overwrite. During already-started Save, leave the view and
   confirm captured outcome/status remains accessible. Check optional Open and save/open failure wording.
6. Unresolved inventory dispositions, the retained legacy browser-feedback discrepancy and export-button
   usability remain separately tracked; overall stage acceptance does not mark them resolved.

## Amendment history (verification and review requests as recorded at each checkpoint)

The following dated entries preserve the correction sequence and prior artifact identities. Their
review/restart requests are historical; final owner acceptance is recorded at the top. Only the last
publication identity represents the final delivered build; no new publication occurs in this closeout.

### Owner-reported export correction — 2026-10-14

Owner requested removal of the MPS Excel scope selector: screen exports now always include all workspace
parents, independent of drilldown selection. Selected-parent sample/API capability is historical test
coverage, not a current screen option.

Owner also reported immediate “Export context changed.” on MPS, BOM and Planning without changing the
view. Root cause: the shared component created a permanently releasable client in React state; application
Strict Mode replayed effect cleanup and reused the released client. Client acquisition now belongs to
each effect setup, with that setup's client released on cleanup. Real client + mocked native IPC
regressions exercise all three kinds inside Strict Mode; departure and unknown-save guards remain.
Integrated desktop confirmation of this correction remains owner-run verification.
Correction checks: 9/9 focused lifecycle/client/recovery tests passed, scoped ESLint passed, frontend
TypeScript/production build passed (same retained bundle warnings), and scoped AFT diagnostics were
fresh with zero errors/warnings. Changes are frontend-only; the published backend sidecar is unchanged.

### Owner presentation amendment — 2026-10-14

Owner confirmed saving now works and provided `C:\Dev\kst_v2_tmp\Stage21\MPS_Example.xlsx` as a
presentation reference. Inspected its worksheet/header styles offline. MPS now has one quantity row per
parent, Description retained, no Metric column or repeated execution-state rows. Weekly headers are
compact Excel date cells (`m/d/yyyy`), colored consistently by the loaded view's fiscal period (P1–P12).
Accepted Monday labels/Falldown rules are preserved; the sample's Sunday labels do not change them.
Parent and Description stay frozen; weekly widths are 13 characters.

The Export information worksheet is removed from all three new analytical exports (MPS, BOM and
Planning). Important existing stale warnings remain as a comment on the first header, without another
worksheet. Existing Reports/Stage 11/Stage 13 layouts are unaffected.

Numeric precision admission now counts canonical decimal digits after redundant fractional zeros are
removed. Thus BOM `1296899.0000000000` is a numeric Excel cell displayed without forced trailing zeros;
genuine fractional values remain and precision-overflow/sub-decimal values still use exact literal text.

Verification: 9/9 analytical workbook/API tests passed, including actual date cells, different fiscal
period fills, single-row parents, absence of information sheets, numeric scaled BOM values and existing
exact precision guards. Samples in the Stage 21 `samples` folder were refreshed. Scoped formatting and
`git diff --check` passed; AFT C# inspection remains partial (server not responding).

Republished/copied sidecars are 127,801,988 bytes each, SHA-256
`2CE5AC69DDE5A6888139E835FA720B62F4664D099919F18A28A427AFCB2AE8A0` (supersedes the earlier hash).
Previously reported Pkcs advisories remain present. No contract shape changed. Restart the desktop
application to load the new backend, then prepare fresh exports for owner visual confirmation.

### Whole-quantity display follow-up

Owner screenshot showed integer quantities rendered as `48.`/`0.` by Excel's optional-fraction format.
Numeric whole quantities now use `#,##0;[Red]-#,##0`; real fractions keep their existing exact display
and currency retains two decimal places. This shared writer correction also removes dangling points
on whole quantities in BOM/Planning. No quantities are rounded or changed.
All 14 analytical tests passed, including zero, positive/negative integers, thousands separators and
fractional display. Scoped AFT diagnostics were fresh/clean. Sidecar republished; published/copied
SHA-256 is `2EC61FB8F5F2C6B9A0CCB765F0CAEBCCE23A26769DBD4700A138D67E817DD167`, superseding the
previous identity. Samples refreshed. Desktop restart/fresh export required for owner confirmation.

### BOM alignment and Planning removal — owner direction 2026-10-14

BOM Level column (including header) is explicitly left-aligned. The shared whole-quantity format
already removes dangling points for BOM; actual BOM tests now assert `3`, `0` and `1,296,899` display,
alongside fractional preservation. Previously prepared/saved files require fresh generation.

Planning analytical Excel UI/helper and its preparation branch were removed; the backend rejects
`Kind=Planning`. Removed corresponding presentation-only tests and added rejection/UI-absence checks.
The existing Mass Updater CSV and accepted proposals/drafts are unchanged. Historical Planning sample
files under the external evidence folder are retained as superseded evidence, not supported output.
Unused generic transport sheet fields remain shape-compatible but no longer enable Planning generation.

Verification for this amendment: 13/13 analytical backend tests and 62/62 frontend tests (22 Planning
view, 34 Mass Updater, 6 analytical lifecycle) passed; scoped lint, frontend TypeScript/production build
and patch whitespace checks passed. C# formatting completed with workspace-load warnings; AFT inspect
was interrupted, so no fresh cross-file diagnostic claim. Matching sidecars republished, 127,797,892 bytes,
SHA-256 `F3F00577DA8D09FB1B3934E5EECDABF657048C772302BE950EF5A290E3CB596F`, superseding previous
identities. Previously surfaced Pkcs advisories persist. Restart the application and export a fresh BOM
to confirm display; Planning must no longer offer Export Excel in either mode.

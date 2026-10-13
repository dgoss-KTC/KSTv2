# Stage 18.4 Shipments-To-Go — I1

Status: **COMPLETE / OWNER-ACCEPTED — CLOSEOUT 2026-10-12**.
Current acceptance/evidence limits: `KST_v2_STAGE_18_STG_CLOSEOUT.md`. Earlier pending statements
below are historical checkpoint observations, superseded by completed owner comparison/F1 resolution.
Stage18 was subsequently recorded COMPLETE / OWNER-ACCEPTED on 2026-10-13; see
`KST_v2_STAGE_18_CLOSEOUT.md`. This I1 packet remains implementation provenance.
Baseline accepted Component MRP/save commit `5c8e73057e19384f9353151f9ea1daac35c149b2`.
Discovery packet `C:\dev\kst_v2_tmp\Stage 18\STG_D1_20261009_144738` remains provenance,
not an accepted specification wherever superseded below. Local Agent Addendum is discarded and
is not applied. Governing authority remains AGENTS.md and enacted security policies.

## Owner decisions — 2026-10-09 I1 instruction

1. Python `C:\dev\KST\features\shipments.py` is authoritative; VBA obsolete, no evidence gate.
   Preserve Python content/layout/formulas/behavior except these explicit amendments.
2. KS/KTS excluded; remove KS Good-To-Go and KTS Shipments rows (not zero placeholders).
3. Supported SW,NW,AR,MN,MS,KV; initially SW,NW,KV selected; remember selections.
   Include Next Week initially off, remembered. No frontend domain or arbitrary SQL text.
4. Preserve initialized zero/already-assigned partial shipped totals on acquisition/processing errors,
   warning/logging only. Complete empty shipments is ordinary zero. Order failure rejects generation;
   empty orders returns clear no-data/no workbook. Discovery fail-closed shipped proposal rejected.
5. Changing report or departing Reports discards prepared bytes, settled presentation and settled
   report retry actions; keep preferences/saved files and minimal active/unresolved recovery.
6. All remaining Python rules baseline: Saturday+7, independent server/local clocks, risk precedence,
   header-site AR routing, main-only summary, ambiguous outer pegging joins/ties/CRC repair unchanged.

## Plan / boundaries before implementation

Contract-first C# DTO/spec → OpenAPI → generated TypeScript, then focused backend, frontend/host,
matching sidecar republish. No new dependency/features/capabilities, general framework, live query,
legacy generator, import/submission, commit/push/fetch or Stage9–11 algorithm change.
Version stays alpha.18, installer0.1.0 under stage version policy.

Domain owns post-query CRC/risk/sort/sheet rules; QAD owns source SQL/date clocks and joins;
Application fresh orchestration; Exports ClosedXML workbook; API DTO/ProblemDetails/preparation;
React inputs/presentation/recovery; Rust known-kind routes, immutable bytes/save authority.
Reuse existing site/domain map/connection and concrete native chooser/staging/publication/open/status.
Keep byte8MiB, lifetime30min and existing capacity limits. Text is always text, never a formula.
No nettable inventory replacement, UOM rounding, dedup/tie-breaker or join correction.

Parity verification: source-derived synthetic adapters/rules/workbooks/API plus unchanged Component
MRP save/close/F1 tests, cross-kind isolation and navigation unknown recovery; generated contracts,
typecheck/scoped lint/format/build/Rust checks; disposable Excel filter/outline/edit probes using
existing isolated tooling if available. No real association dispatch. Desktop without established
synthetic no-live mode remains pending. Sample is structural evidence, not matched numerical proof.

## Source limitations retained

Peg outer join matches number/line/RN without domain/site; tie quantity has no secondary order;
inventory SUM can reflect join multiplication; comment donor repair scans first matching donor across
scope; SOLN concatenation isn't unique; CRC text containing Total is treated as a total by Python.
Shipped tr_hist has no domain predicate, uses source price/sign and independent server date; summary
uses only SW/KV after KS removal, omits AR and labels multi-main as SW. Currency/clock/source
cardinality/live numerical parity remain unverified. Owner explicitly chose these baseline rules;
no silent fixes. Source strings are stored safely as workbook text.

## Implemented flow and parity

- `Kst.Domain/ShipmentsToGo`: post-query observations/CRC repair/risk/sort/routing. Numeric double
  observations match Python's Decimal→float boundary; no UOM/business rounding. Source nullable money
  remains blank; zero remains numeric. Repeated observations are retained, not deduplicated.
- `Kst.Integrations.Qad/ShipmentsToGo`: independently parameterized site/domain pairs, original
  order joins/grouping/comment expressions/server cutoff; separate shipped read and warning-only
  zero/partial accumulator. Cancellation remains operation cancellation, not a successful empty report.
- Application uses no workspace/population/cache. Empty orders bypass shipped read/workbook creation.
- API POST `/api/v1/reports/shipments-to-go`, prepared bytes route, noData DTO, sanitized503 on order/
  preparation failure; existing bounded preparation store tagged by kind. Generated OpenAPI/TS included.
- Exports uses ClosedXML plus bounded standard-library OOXML presentation normalization for theme,
  CF priority/stop flags and absent legacy print setup. Header A:T/hidden A,D; content-sized widths,
  Aptos Narrow11/default anchor theme, live subtotal9/outlines/table/filter, totals/summary colors and
  formulas. Only first sheet freezes row1. Details static; CRC text containing Total gets Python styling.
  Formula caches evaluated without altering live formulas. Summary after grand g: Open g+2, Risk g+3,
  SW G2G g+5, KV G2G g+6, SW shipments g+8, KTV shipments g+9; no KS rows/tab/acquisition.
- Reports selector adds STG multi-select and remembered inputs. Host routes only ComponentMrp or
  ShipmentsToGo; preparations/operations/artifacts carry kind ownership, no arbitrary URL/path authority.
  Existing native chooser/publication/open/preferences remain concrete shared save implementation.
- Report change/departure invalidates acquisition epochs, clears preparation/results/Open retry, drops
  host artifact grants when workers release. Saved files/preferences retained. A retained hidden panel
  tracks active/unresolved operation ID/kind and status only; no replay or late departed presentation.
  Active save/open worker continues safely; normal-close barrier and F1 guards retained.

## Verification and final disposition

Local evidence folder `C:\dev\kst_v2_tmp\Stage 18\STG_I1_20261009_153129`.

- Scoped backend **55/55**: Domain28, QAD8, Application5, API4, architecture10 (includes ComponentMRP
  regressions). Synthetic only; no live read/legacy generator. Source-derived fixture expectations
  supersede D1's rejected KS-placeholder/fail-closed-shipped proposals.
- Reports frontend **23/23**, includes existing F1 async regressions + new STG defaults/noData/kind
  retry/departure/unknown recovery/stale acquisition. Actual application workflows additionally passed
  WorkspaceLifecycle17 + PlanningUnsavedChanges9 (**26/26**), including native-close mock workflow.
  There is no standalone ApplicationShell test file; Reports active-prop departure is explicitly tested
  in ReportsPanel, with shell wiring source-inspected.
- Rust **20 passed,2 ignored** (interactive chooser and real association dispatch deliberately ignored).
  Existing synthetic filesystem publication/replace/collision/cleanup/open/close tests and new bounded
  route/departure ownership check passed. No real association dispatch.
- Actual isolated Excel, disposable synthetic workbook, macros disabled/read-only/no source links:
  initial total350/risk-200/G2G150; filter100/0/100; manual hide/outline retain350/-200/150;
  edited Risk→-50 gives G2G300; QoH edit doesn't regenerate Risk; KV cross-sheet80; source text nonformula.
  `VerifyExcel.ps1`, `excel-results.json`, `workbooks/synthetic-main-kv.xlsx` and mixed-site workbook retained.
- Backend build/OpenAPI, generated TypeScript, typecheck, production frontend build, scoped ESLint/
  backend format, cargo check and modified-module rustfmt passed. Full frontend lint fails only the two
  unchanged locked Stage10 setState-in-effect errors. Broad backend formatting baseline remains a known
  exception (not rerun/claimed passing); no whole-Rust-format claim. AFT had Rust missing rust-analyzer,
  occasional C#/TS diagnostic timeout and unavailable Tier2 gaps; compiler/tests are actual gates.
- Sidecar republished with `scripts/build-sidecar.ps1`; both copies SHA256
  `6879E727EE9F48AFABAA1CC39F40C4707AADC3465274C50B85BA10940BC80583`. Alpha18/installer0.1.0 unchanged.
  No connected/main desktop launch: no established synthetic desktop mode; integrated desktop remains
  owner-pending. Test-host API is mocked/no-live; isolated Excel is separate actual interaction evidence.
- Earlier development failures corrected: CF range consolidation, ambiguous SaveOptions name, tests
  needing awaited async generation, and native run missing required synthetic-root environment. Final
  gates above passed. TRX reused filename across assemblies and retained only last assembly; terminal
  counts are recorded honestly, not a complete retained multi-assembly TRX bundle.

Historical I1 remaining checks (superseded where completed below): focused independent review of SQL parity, workbook presentation and discard/recovery trust
boundaries; owner exact AutoFit/render/theme/printing comparison and integrated synthetic desktop/save
navigation verification; matched raw-input/live numerical parity separately authorized. ClosedXML
content sizing is implementation sizing, not proof identical Excel AutoFit pixels for every font/data.
No missing legacy sample is treated as a new discovery blocker. No new dependencies/capabilities,
live queries, source edits, Stage9–11 changes, commits/pushes/fetches or acceptance.

## F1 correction — 2026-10-09

Owner ran STG and reported output identical to the legacy generator; conditionally accepted 18.4
barring review findings. That completed comparison supersedes owner-comparison-pending statements;
no repeat required. R1 established one blank-CRC subtotal defect, not a contradiction of that owner run.

F1 correction is limited to workbook subtotal generation and regressions. Excel source-observable
Subtotal skips leading blank CRC group totals; an all-blank sheet likewise gets only Grand Total.
Writer omits blank-group totals and tracks last detail explicitly (rather than grand-2) for P/R grand
ranges. Blank detail outlines and all-blank grand outline match actual Excel levels; named-group
outlines/formulas remain intact. Null/empty/whitespace repaired inputs and donor success covered;
CRC Total text styling retained. No acquisition/rules/contracts/frontend/save changes.

Fresh isolated Excel: mixed null10/empty20/Alpha30 → named subtotal5/grand6; all-blank10/20/30 → grand5;
both grand ranges P2:P4/R2:R4, total60. Corrected fixture risk-6/G2G54; filter on null identity total10;
outline collapse retains60; Risk edit propagates G2G50. Zero connections/macros/events/link updates off.
Named-only single/multi expectations independently observed and existing regressions retained.
Scoped Domain/workbook/ComponentMRP31 + API4 passed; backend build0 errors/warnings and scoped format
passed. AFT inspect interrupted, not a pass. Broad pre-existing format, locked Stage10 lint and Rust
LSP gaps retained as known exceptions. No desktop/live or connected legacy generator execution.

Corrected sidecar published using scripts/build-sidecar.ps1; both copies SHA256
`D96A6C36F493F99DF9FF7B08703D1A63C06E44FCEFCF4D72F55BB9D4E6D81390`.
Fresh provenance/log/source identities retained in
`C:\dev\kst_v2_tmp\Stage 18\STG_F1_20261009_1645_evidence`; compact export
`C:\dev\kst_v2_tmp\Stage 18\STG_F1_20261009_1645`. Older sidecar hash difference unexplained;
no historical explanation inferred. Alpha18/installer0.1.0 unchanged. Overall Stage18 status is in
`KST_v2_STAGE_18_CLOSEOUT.md`.
Separate-context focused re-review adjudicated **STG-R1-F1 Resolved**, no concrete blocker in
F1/direct-workbook scope; recommends 18.4 closeout under existing conditional acceptance. Result:
compact export `focused-review.md`. Reviewer checked actual final source/diff/hashes, TRX and workbook
OOXML/Excel observations; retained tests/Excel not rerun. Reviewer fresh C# AFT zero diagnostics with
Tier2 gaps. No universal security/numerical/printing assurance or next-stage authorization inferred.
Owner comparison is completed evidence; no repeat requested. Overall Stage18 status and retained limits
are recorded in `KST_v2_STAGE_18_CLOSEOUT.md`.

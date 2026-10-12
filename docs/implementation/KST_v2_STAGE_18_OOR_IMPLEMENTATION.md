# Stage 18.6 Open Order Report — I1

2026-10-12. **COMPLETE / OWNER-ACCEPTED.** Accepted closeout, presentation follow-up and final
guard review: `KST_v2_STAGE_18_OOR_CLOSEOUT.md`. Stage 18 overall remains IN PROGRESS because
the checklist's S&OP legacy-workbook comparison remains open. I1 evidence below is historical;
its pending review statements do not override the accepted closeout.
Owner-designated18.6 corresponds to historical master checklist18.3, retained without renumbering.
Starting commit6b0bb214d57e416d91702a50900e8bd10ef05b5d/treee8d220498c0da5b9e506cc8f27c3c044a6dc2ffd.
Trial branch stage-18-6/open-order-gpt61-sol; label is not a model/provider assertion.

## Approved scope and source trace

Python `C:\dev\kst\features\oor_sales.py` is authoritative; VBA obsolete. Source inspected directly:
mandatory/optional headers9–21; runner72–124; filters161–195; expressions200–255; joins256–268;
ordering270. UI `app/views/oor_view.py:149–209`, date parser `app/ui/components/date_picker.py:183–208`,
product validator `app/ui/validation.py:9–21`, preferences `app/controllers/oor_controller.py:17–29`,
workbook `utils/excel_ops.py:260–317` and Decimal/float boundary `data/qad_db.py:183–194` were relevant
discovery evidence. No legacy Git checks or execution. No supplied OOR example; no visual/live parity claim.

Owner approved: six accepted pairs only (SW/NW/AR/MN/MS KTC; KV KTV), blank All supported sites
default; explicit site qualifies as required non-date filter, dates/all alone do not. Paired Unicode
decimal four-digit product lines, leading zeros, no ascending restriction; paired valid ascending dates.
Actual Python date formats, separator variants, current-local-year default and68/69 pivot retained;
yearless2/29 fails as Python parses against1900 first. Optional columns remembered only.

Independent nullable cells preserve all34 source aliases, complete enrichment and order without workspace
population, positive-open/status/hold/type/current-date/RA/UOM exclusions. SQL LIKE entered wildcards,
other equality filters, raw Ext Price and consignment Unit Price, raw QOH SUM, nulls and duplicate rows,
unordered comment records/XML escaping retained. No source-result calculation or fallback substitution.
Workspace Stage13 query/model/writer and locked Stage9–11 unchanged.

## Implementation boundaries

Contract-first DTO/endpoint/spec→backend OpenAPI→generated TS, then implementation/backend/frontend/native.
Domain `OpenOrderReport` owns validation and independent nullable typed facts/frozen report. Application
reader/delegate/service fresh acquisition. QAD reader owns parameterized SELECT and site/domain map,
strict ordered alias/type/value validation, cancellation and100s acquisition deadline with configured
command timeout. No procedure/read/write privilege changes. No dedup, tie-breaker, literal-LIKE rewrite
or nettable inventory replacement. Schema errors (including empty schema/type mismatches) fail.

Limits:10,000rows/500,000cells/16MiB estimated source,1024characters per input, Excel32,767characters
per text cell. Oversize fails whole report, never truncate comments/rows. Selected/hidden fields all
acquired once. Nullable SQL numeric/date/text/Boolean facts remain distinct; dates retain source time
in workbook numeric values while displayed m/d/yyyy. Excel serialization is numeric floating boundary,
no business rounding. Text is explicitly nonformula.

Exports ClosedXML: exactly one OOR sheet, A1 OpenOrders table Light9/filter, mandatory nine first then
25 selected optionals in Python alphabetical order; sparse table full dimensions, fitted columns,
first-row freeze/no columns, legacy dates m/d/yyyy and ordinary numeric types. No formulas/totals/
grouping/metadata/protection/custom print setup, extra fields or add-in loading.

API `/api/v1/reports/open-orders` single-step acquire→prepare and owner/kind bytes route; fixed native
OpenOrderReport kind. Reuses existing8MiB/30min/eight-owner/two-generation store. New Generate drops
that owner's old OOR preparation before validation/acquisition, so failed/empty requests cannot expose
old OOR bytes. Report transport DTO does not expose rows or filesystem/query/program authority.
Frontend sections Customer and site/Sales and product/Due dates/Columns; remembered allowlisted
optional fields only. Validation is authoritative backend; paired product/date hints also displayed.

Concrete accepted Reports/save foundation extended narrowly, no generic framework. Input/column changes
invalidate epoch/preparation; Open preference alone does not. Departure discards presentation/prepared
bytes/settled retry grants but retains minimal active/unknown recovery. Save cancel/failure retains exact
bytes; unknown requires status, no replay; committed/open-pending versus terminal guards retained.
Host fixed route/session/kind, candidate-bound consent, publication/readback/flush/owned cleanup,
folder/filename numbering, Open/artifact identity/retry and close coordination unchanged.

## Verification (offline/mock/synthetic)

Local evidence root: `C:\dev\kst_v2_tmp\Stage 18\OOR_I1_gpt61-sol_20261012_141101\evidence`.
Compact review: sibling `transfer`. No application/legacy generator/Excel/live source/real association.

- Contract-first solution build0warnings/0errors; OpenAPI/generated client regenerated. Final publication
  regenerated spec and hash-stable client; frontend typecheck passes. OpenAPI generation uses the
  repository's build-time document generator, not a connected application/listener launch.
- Backend standalone report/architecture regression103passes (Domain53/Application9/QAD25/API6/
  Architecture10), per-assembly timestamped `oor-regression*.trx`. After additional independent header/
  preparation checks, final OOR-only25passes (Domain16/Application1/QAD6/API2), `oor-final*.trx`.
- Frontend61passes across ReportsPanel23/OOR lifecycle3/OOR inputs2/OOR API2/SOP5/WorkspaceLifecycle17/
  PlanningUnsavedChanges9; typecheck/scoped ESLint/build passed. Existing SOP/Planning act warnings and
  Vite mixed static/dynamic-import warning retained; not failures. No interactive visual verification.
- Rust21passes/2ignored (interactive chooser/real association), cargo check offline/locked and scoped
  rustfmt pass. Existing production synthetic publication/replace/close/Open-reconciliation tests used,
  no mechanism reinvestigation. Tests run with KST_MRP_TEST_ROOT under local evidence.
- New C# scoped format passes; git diff --check passes. Full-solution formatting/locked Stage10 lint
  exceptions not rerun/claimed passes. AFT C#/TS scoped0diagnostics where authoritative; initial C#
  timeout, later TS coverage gaps, unavailable rust-analyzer/Tier2 remain explicit limits.
- Initial OOR frontend selector ambiguously matched filter and optional checkbox: corrected to textbox.
  Initial npm reporter/output flags were rejected by installed npm; supported command rerun passed.
  Scoped lint first flagged mixed component/helper exports: split report preference helper, no suppression.

Sidecar republished with `scripts/build-sidecar.ps1` from trial source. Both127,588,996-byte copies SHA256
`562BE9EB9445F96AFD8873275CEEA8C0714F03E88393AFFD09F036B753F1D045`:
`publish/backend-sidecar/Kst.Api.exe` and `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`.
Does not establish currently running owner's process loaded it; owner process never terminated.
Tracked frontend buildinfo restored byte-identically from prebuild backup; generated Tauri schema
line-ending-only side effects reconciled without capability changes. Version alpha18/installer0.1.0.

## Historical I1 review gate

No live SQL schema/value/cardinality/volume, matched legacy numerical, exact AutoFit/theme, actual
desktop Save As/Open/departure/close matrix or physical printing proven. Missing example isn't a blocker
or defect. Source/header type mismatches will fail visibly, not coerce; later evidence requires separately
authorized owner disposition. No dependency/capability/db change, commit/push/fetch/stash/reset or acceptance.

**Done:** authorized implementation and scoped verification. **Decided:** approved Python parity plus
six-site/one-sheet/no-add-in/native-save adaptations. **Open:** comparative review and later separately
authorized owner desktop/live verification. **Next checkpoint:** comparative review; stop before acceptance.

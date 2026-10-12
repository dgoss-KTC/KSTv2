# Stage 18.6 Open Order Report — accepted closeout

Closeout: **2026-10-12**. Status: **COMPLETE / OWNER-ACCEPTED**. Owner designation **18.6**
corresponds to the master checklist's historical Open Order section **18.3**; unrelated numbering is
unchanged. Stage 18 overall remains **IN PROGRESS**: S&OP's explicit legacy-workbook comparison item
is still open, despite acceptance of all four delivered reports. No comparison waiver is inferred.

## Owner acceptance and retained implementation

The owner accepted the revised UI and output report, confirmed report export and saving worked in
the tested cases, and explicitly authorized this closeout, one complete commit on the Sol branch,
integration into main and normal push to the verified origin. No filter set, row count, all-site
coverage, physical printing result or exhaustive desktop matrix is invented.

Sol's independent Domain/Application/QAD/Exports/API implementation is retained with Sonnet's
owner-preferred presentation adaptation in `ReportsPanel.css` and `OpenOrderReportInputs.tsx`.
The accepted set includes nullable independent source facts, fresh parameterized six-site acquisition,
Python-compatible filters/mappings, standalone one-sheet workbook, typed DTO/OpenAPI/generated client,
Reports preferences/lifecycle, fixed native report kind, tests and the narrow shared read-only SQL
guard fixture registration. Stage 13 workspace population and locked Stages 9–11 remain unchanged.
Detailed scope and source decisions remain in `KST_v2_STAGE_18_OOR_IMPLEMENTATION.md` and
`../api/OPEN_ORDER_REPORT.md`; no production or contract correction is part of this closeout.

## Focused review and verification evidence

Final review: `C:\dev\kst_v2_tmp\Stage 18\OOR_FINAL_REVIEW_20261012_161054\REVIEW.md`.
The existing evidence packets were externally relocated during closeout to the `Archive` subdirectory
of that Stage 18 folder; the archived final review matches the preflight copy byte-for-byte. Original
packet paths below are retained provenance; current lookup adds `Archive` before the packet name.
It established no blocking defect at its bounded source-review depth. The shared guard initially
failed because its reflection fixture did not support the new filter type. Six fixture lines now
exercise the report's filter predicates without changing production SQL, discovery or validator.
The fixture correction is **self-reviewed**, not independently adjudicated. The affected shared guard
and OOR query tests then passed **7 tests, exit 0**, with retained TRX evidence.

Retained offline/mock/synthetic evidence, not fresh closeout execution:

- Sol I1: report/architecture regression 103 passes; final OOR 25 passes (zero-test architecture file
  explicitly excluded); frontend 61 passes, typecheck/scoped lint/build; Rust 21 passes/2 intentionally
  ignored; matching trial sidecar publication. Detailed evidence is in
  `OOR_I1_gpt61-sol_20261012_141101/transfer` under the local Stage 18 evidence directory.
- Sonnet presentation follow-up: frontend 31 passes, typecheck/scoped lint/build in
  `OOR_UI_sonnet55_20261012_155639`. This follow-up and final fixture verification are separate from
  the original model-comparison statistics; those statistics and trial packets are preserved.
- Closeout preflight: all 29 final-review source hashes matched byte-for-byte, including the corrected
  fixture. Final review had already distinguished Sol's 10 byte-identical files, 16 line-ending-only
  differences and the two explained presentation changes. Generated Tauri schema working differences
  normalize to HEAD and remain outside the accepted commit.
- Closeout verification is source identity, explicit manifest/staged diff/whitespace audit and Git
  ancestry/remote-tip verification. No build, suite rerun, sidecar publication or application launch.

Owner observations above are separate from synthetic/mocked checks. Exhaustive live QAD schema,
cardinality/volume and matched Python numerical parity, physical printing, exact Excel AutoFit/defaults,
and the full native Save As/Open/departure/close/recovery interaction matrix remain unperformed or
unestablished. A published sidecar identity does not establish which binary an earlier owner process
loaded. Broad pre-existing formatting failures, locked Stage 10 lint errors and AFT C#/TS/Rust/Tier2
coverage gaps remain exceptions, not passes. No universal filesystem guarantee is claimed.

## Stage 18 checklist reconciliation and decisions

All four report deliverables are owner-accepted: Component MRP (2026-10-09), Shipments-To-Go, S&OP
and Open Order Report (closeouts 2026-10-12). Shared 18.1 definitions are now evidenced by actual
report uses rather than speculative abstraction: typed request/fixed kind, host-owned remembered
folder and filename numbering, candidate-bound overwrite, report-specific metadata/layout decisions,
phase/status reporting, acquisition cancellation and bounded save/close cancellation, owned cleanup,
and synthetic workbook validation. OOR deliberately has no extra workbook metadata or print redesign.
These definitions do not assert every desktop sequence was tested or that Stage 21 conformance is done.

The historical S&OP checklist item **Compare with legacy workbook** remains unchecked: accepted output
and synthetic/static workbook evidence do not establish that comparison. Consequently Stage 18 overall
is not marked complete. Component MRP/OOR comprehensive live parity limits remain explicitly qualified;
the accepted STG owner comparison is retained at its documented depth. No new comparison work is
authorized here; separately scope the remaining S&OP comparison or an explicit owner disposition.

Application `0.1.0-alpha.18` and numeric installer `0.1.0` are preserved. All four stashes, comparison
branches, trial packets and parked unrelated work are preserved. The compact local closeout HANDOFF
records the resulting commit/tree, main integration, verified origin/main tip and exact final state.

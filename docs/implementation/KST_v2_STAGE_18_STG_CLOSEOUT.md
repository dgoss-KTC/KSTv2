# Stage 18.4 Shipments-To-Go — accepted closeout

Closeout executed: **2026-10-12**. Status: **COMPLETE / OWNER-ACCEPTED**.
Stage 18 overall is **COMPLETE / OWNER-ACCEPTED — 2026-10-13**. S&OP and Open Order Report were
subsequently accepted; the S&OP legacy-workbook comparison is owner-reported completed, with no repeat
required. Overall limits remain recorded in `KST_v2_STAGE_18_CLOSEOUT.md`.

## Acceptance and scope

On 2026-10-09 the owner reported running the application, generating Shipments-To-Go and observing
output identical to the accepted Python legacy generator, accepting 18.4 barring review findings.
STG-R1-F1 was corrected and separately reviewed as **Resolved** on 2026-10-09. The owner explicitly
confirmed acceptance and authorized documentation closeout and one local commit on 2026-10-12.
The conditional acceptance is satisfied; no repeat of the completed comparison is required.

Authority: `C:\dev\KST\features\shipments.py`; VBA obsolete; rejected Local Agent Addendum not applied.
Approved exceptions: exclude KS/KTS acquisition/tab and both summary rows; six active sites
SW/NW/AR/MN/MS/KV initially SW/NW/KV with remembered selections and default-off remembered Include
Next Week; preserve zero/already-assigned partial shipped totals after errors; discard report
preparation/settled results/retry actions on report/view departure while retaining preferences,
saved files and minimal active/unknown operation recovery. All other Python rules remain baseline.

Accepted delivery includes independent parameterized acquisition and source mappings, domain
transformations/routing, ClosedXML workbook/live formulas, typed API/OpenAPI/generated contracts,
Reports inputs, bounded host report-kind routing and departure recovery, tests and F1 correction.
Component MRP/save foundation at `5c8e73057e19384f9353151f9ea1daac35c149b2` remains accepted.

## Finding resolution and retained verification (2026-10-09)

R1 found extra blank-CRC subtotal rows. F1 now matches observed Excel: mixed null10/empty20/Alpha30
has Alpha subtotal5/grand6; all-blank10/20/30 has only grand5. Both grand ranges P2:P4/R2:R4 include
every detail; total60. Blank/named outline levels, table bounds, summaries, Total-text legacy behavior
and donor repair are covered. Separate-context review inspected actual final source/diff/hashes,
TRX, workbook OOXML and isolated Excel evidence; no remaining scoped blocker established.

- I1: backend55 passes (Domain28/QAD8/Application5/API4/architecture10); Reports23;
  workspace/close workflows26; Rust20 passed/2 deliberately ignored (interactive chooser/real dispatch).
  Historical reused TRX retains only last API assembly, not the complete55-test bundle.
- F1: **31 Domain/workbook/Component MRP + 4 API passes**, separately retained TRX. Backend build
  zero warnings/errors and scoped format passed. Isolated synthetic Excel confirmed row/formula/
  outline/summary behavior, filter10, collapse total60, risk edit Good-To-Go50; macros/events/link
  updates disabled, zero connections, no real association dispatch.
- Retained I1 typecheck/frontend build/scoped lint, backend/OpenAPI/generation and cargo gates passed.
  Closeout audits identities and staged whitespace/coverage; no new build or broad suite rerun.

Corrected sidecar published with `scripts/build-sidecar.ps1` on 2026-10-09; both current copies
confirmed at closeout SHA256 `D96A6C36F493F99DF9FF7B08703D1A63C06E44FCEFCF4D72F55BB9D4E6D81390`:
`publish/backend-sidecar/Kst.Api.exe` and
`src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`. Build provenance in F1 evidence log.
R1's earlier matching sidecars differed from I1's historical recorded hash; cause remains unknown.
No explanation or binary attribution for the owner's earlier run is invented.

## Evidence limits / preserved state

Owner comparison is observed evidence, not exhaustive live numerical, desktop, render or printing
verification. Ambiguous Python joins/ties/CRC lookup and summary/source-clock limitations remain
documented in the implementation ledger. Full formatting failures, two unchanged locked Stage10 lint
errors and AFT/Rust/Tier2 gaps remain known exceptions, not passes. Separate F1 review is bounded,
not universal security assurance. No live queries/connected generator/imports/submissions performed.

Application stays `0.1.0-alpha.18`; installer stays `0.1.0`. No release change or further report work.
Unrelated frontend build info, generated Tauri schemas and Stage14 plan are excluded/preserved.
No reset/clean/stash/amend, dependency/tool install, push or fetch. One local commit only authorized.

Local packets under `C:\dev\kst_v2_tmp\Stage 18`:
`STG_I1_20261009_153129`, `STG_R1_20261009_162104`, `STG_F1_20261009_1645` (separate focused review),
`STG_F1_20261009_1645_evidence` (full local evidence outside export),
`STG_Closeout_20261012_083541` (commit identity, preservation/coverage audit and compact handoff).

# Stage 18-I1 — Component MRP implementation, pending owner verification and acceptance

2026-10-08. Initial baseline main / `05e110912ee2001453e919ac890cdf012ebb27e4`,
alpha.15 / installer0.1.0; no fetch. This implementation starts Stage18 under VERSIONING.md:
application alpha.18, numeric installer unchanged. Stage18 is NOT complete or accepted.

## Implemented

- Independent Reports tab and left selector: Component MRP, Shipments-To-Go, S&OP,
  Open Order Report. Only Component MRP is active.
- Generated C# → OpenAPI → TypeScript contract; POST `/api/v1/reports/component-mrp`.
  Direct domain master, supported sites and horizons, no workspace/BOM/P/M eligibility gate.
- Application fresh acquisition and unchanged Stage11 BuildBoth. Narrow QAD command composes
  accepted raw inventory/schedule query semantics and removes the planner lookup/fallback;
  selected-site Buyer only. Adds metadata projections. Independent QCTC query only: no
  manufacturer, KSS, AVL/Standard Cost or broad purchasing presentation.
- Complete empty MRP valid, missing master404, invalid400, acquisition failure503 with sanitized
  problem. Unknown quantities/PO confirmation/physical identity/conflicting metadata fail.
- Compact calculated numeric workbook with Past Due and selected horizon; all seven ledger rows,
  UOM rounding after full-precision calculation, QCTC separate five-place presentation, no currency
  or invented time-fence/inspection units. Frozen local as-of/acquisition/consistency context.
- Reports UI immutable preparation, latest-generation/abort/unmount guards, exact-object retry,
  save-intent snapshot, serialized preference updates and no blind replay of unknown response.
- Site remembered locally; Open boolean remembered by fixed host preference in current-user
  LocalAppData/KST/config. Session choice survives persistence error; strict bounded load.

## Exact remaining native blocker and incomplete delivery

This is a **partial implementation**, not a working disk-save/open report. Native Save As returns
an explicit fail-closed outcome before selection or any report-file mutation. No direct-write,
download, replacement or alternate publication fallback exists. Prepared bytes remain available.

D5.2 §3.2 requires preventive authorized-parent binding before absolute CREATE_NEW and through
absolute FileRenameInfo. The available chain denies delete-sharing but does not establish reparse/
attribute-mutation prevention; CreateFile sharing does not restrict all attribute access. A native
chooser path also does not bind original selection to the acquired identity. No proved narrower
protocol has been implemented here. Consequently the host has no report destination or artifact
grant, no publication/delete commands and no association dispatch. Collision/cleanup/published
survival/native open-dedup checks cannot be claimed passed by mocks. No new dependency proposed
merely to bypass this gap; no broad webview permissions added.

Remembered-folder requirements D20–D23 are **not implemented**, because no actual committed parent
exists to remember. Configured Documents resolution/chooser/persisted folder are deferred with the
affected native branch. Open retry behavior is covered through an injected port in UI tests only;
production opening is unavailable. The host boolean preference is separate from deferred folder
state. Its fixed-file standard filesystem handling follows per-user conventions but does not claim
hostile namespace protection; actual source/diff needs independent security review.

Backend returns workbook bytes directly in the typed response and keeps no native prepared grant.
UI keeps one result, invalidated by calculation input changes. Before enabling native save, replace
the fail-closed port with host-owned prepared acquisition/grants: do not accept arbitrary workbook
bytes, paths or programs from the page. Lost-response status/dedup and bounded native worker/grant
lifetimes remain integration requirements, not proven by the UI reference guard.

## Verification and review

Meaningful synthetic tests independently assert raw mapping before builder/workbook assertions;
hand-established F02/F03/F06/F07/horizons, null/zero/QCTC and physical identities; real endpoint
with mocked reader returns workbook bytes without company connections. UI tests inject acquisition/
save/open outcomes, never invoke OS association. Scoped native schema test contains no disk writes.
Actual commands/results and preservation are recorded in the external I1 handoff.

No live-source query, desktop launch, real workbook viewer, dependency install, commit/push/fetch.
Known broad .NET/Rust formatting failures, locked Stage10 lint errors and AFT Rust analyzer gaps
remain outstanding. Scoped checks are not acceptance or clearing unrelated failures.

Owner may verify Reports navigation/defaults, input edits and warnings; connected Generate needs
separate live-query authorization. Disk saving/opening cannot yet be owner-verified as working.
Fresh-context source/diff security review is required before acceptance of native changes.

# Customer Open Orders — Stage 13.2 backend/API

**Status:** 13.2 accepted; 13.3 Report Mode implemented for owner desktop review (not yet accepted). Business baseline:
`docs/prompts/STAGE_13_OPEN_ORDERS_PLANNING_PROMPT.md`; source evidence:
`docs/implementation/KST_v2_STAGE_13_SOURCE_CONTRACT_LEDGER.md`.
Exact mechanical contract: `docs/openapi/Kst.Api.json`.

## Scope and response

- `GET /api/v1/workspaces/{assignmentId}/open-orders?mpsSnapshotId={guid}` (`GetOpenOrders`)
- `POST /api/v1/workspaces/{assignmentId}/open-orders/refresh?mpsSnapshotId={guid}` (`RefreshOpenOrders`)

The caller must supply the **current MPS snapshot ID** as a nonempty GUID query value, not the workspace name or Open Orders
snapshot ID. The backend reads the workspace's *already loaded* MPS snapshot; it never
auto-loads or substitutes a different MPS population. QAD resolves the domain from the
workspace site. Exactly the snapshot's resolved parents, including explicit parents without
MRP rows, qualify at that site/domain when ordered minus shipped quantity is positive.
There are no additional status, type, hold, date, RA, or customer restrictions. The read
is batched at the established MPS parent-batch size, without result truncation. A successful
empty report is different from a failed source read.
The API does not guarantee global row ordering across parent batches. The accepted default
Customer Name → Item Number → Due Date order applies to the complete returned population
in the later report UI, not to each batch independently.

Each line has a source key of domain + order + line; site and item retain provenance.
The response includes all accepted report fields **regardless of column visibility**, plus
Salesperson (local filtering), original total Order Qty, Shipped Qty, raw Price, and the
original editable dates. Decimal Open = Order Qty − Shipped Qty, Ext Price = raw Price ×
Open. Consignment affects displayed Unit Price only (zero on a true consignment line),
not raw Price or Ext Price. Site QOH remains nullable or negative. Comment fields within
one record concatenate in `##1`–`##15` order; comment records have no guaranteed order.

## Snapshots and outcomes

Each successful acquisition has its own Open Orders snapshot ID and UTC acquisition time,
associated with a workspace and MPS snapshot. A normal GET uses the matching in-memory
report; POST refreshes it. Failed refresh preserves only a *compatible* last-good report,
sets `isStale=true`, and supplies a warning. A successful retry replaces the stale report
with a new snapshot. Neither a stale report nor a cached read constitutes fresh
operational/QXtend validation; there is no such validation or export endpoint at 13.2.
The cache is process-local.

- 200: complete loaded report, possibly stale-with-warning after a failed refresh.
- 400: missing, malformed, or empty-GUID MPS snapshot ID.
- 404: unknown workspace.
- 409: MPS not loaded or requested/current MPS snapshot mismatch.
- 503: QAD unavailable with no compatible last-good report.

Errors use normal Problem Details. Source exceptions, customer/order contents, and
connection details are not emitted as response text or query logs. This capability
does not write to QAD, submit QXtend updates, save files, or persist report data.

## Limitations and must not infer

Bounded SW source checks confirmed join cardinality for 50 lines, but observed no
comment rows, true consignment values, nonblank holds, or negative QOH. See the ledger
for sanitized evidence. Do not infer stable order among comment records, inventory
usability from Site QOH, successful external updates from a report, or export eligibility
from a stale snapshot. The 13.2 backend delivered none of the desktop report, drafts,
editing, XLSX, or QXtend files. The 13.3 desktop report and XLSX are described below;
drafts, editing, and QXtend files are later checkpoints.

## Checkpoint 13.3 Report Mode

The workspace navigation loads `GET /open-orders` only after the workspace's already-loaded MPS
snapshot exists. The UI retains the complete response and sorts all lines locally by Customer Name,
Item Number, Due Date; page size 100 limits rendered DOM rows only. Filters (AND-combined) and
visible column layout do not query QAD. The Due Date range follows the legacy two-boundary rule:
it is inactive until both dates are entered. A report can be loaded-empty, unavailable, refreshing,
or stale with its original acquisition timestamp; a changed MPS snapshot is a conflict, not an
empty report. Stale content and XLSX output are explicitly unsuitable for operational/QXtend
validation. Layout is stored under the immutable assignment ID in local UI preferences.
The compact filter builder applies a selected predicate on + or Enter and shows one editable,
removable chip per filter type; incomplete drafts do not affect the report or export. Product Line
can use either or both boundaries; Due Date requires both. Visible table headers can be dragged to
reorder, with keyboard-accessible Move Left/Right buttons in each header; the expandable column
selector is a compact grid. Dates and acquisition time are formatted for display only; API values
and workbook values retain their original precision and date types.

**Owner amendment for the workspace Report Mode (13.3 final review):** Customer Name, Customer #,
Salesperson, IOS, SO, PO, and Item Number now use case-insensitive **literal contains** matching,
not the older exact Customer/Salesperson/IOS legacy predicates. The new SO/PO/Item filters operate
only on the complete already-acquired workspace report. All applied filters are AND-combined; a
draft does not change counts, table, or export until + or Enter applies it. Null/blank source values
do not match nonempty text predicates. Product Line and Due Date retain inclusive ranges and Due Date
still needs both bounds. This amends the workspace UI rule only, not QAD SQL or workspace scope.
Visible report/XLSX headers now read **SO** (`order` ID), **Status** (`stat`), **Customer #**
(`customer`), and **Planner** (`plnr`); stable IDs, generated API property names, saved layout keys,
and default visible order are unchanged.

`POST /api/v1/workspaces/{assignmentId}/open-orders/report-export` accepts the current
`mpsSnapshotId`, exact cached `openOrdersSnapshotId`, ordered `lineKeys` and ordered visible column
IDs. The service verifies current workspace/MPS scope and matching cached report **without reading
QAD**. Unknown or duplicate columns and duplicate keys return 400; mismatched identities return
409. Output is an XLSX Open Orders sheet with exactly the requested report columns and
rows in request order, plus a separate Report Metadata sheet disclosing acquisition time and
report-only/stale status. A compatible stale report may be exported with its stale label.
The API's advertised XLSX filename uses the current workspace display name (site fallback) as a
Windows-safe normalized prefix followed by `-Open-Orders-<acquisition-date>.xlsx`; the frontend
passes that same suggested name to desktop Save As or browser download. The operator may rename
the file in Save As. Success feedback near Export announces `Saved <actual filename>` and clears
after approximately five seconds; cancellation and retryable errors remain distinct. This endpoint does not validate or
generate QXtend changes. Planning Mode, drafts and QXtend files remain later checkpoints.

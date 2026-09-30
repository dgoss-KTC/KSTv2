# Customer Open Orders — Stage 13.2 backend/API

**Status:** Implemented at Checkpoint 13.2; project-owner review pending. Business baseline:
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
from a stale snapshot. The desktop report, drafts, editing, XLSX, and QXtend files are
separate later checkpoints; none is delivered here.

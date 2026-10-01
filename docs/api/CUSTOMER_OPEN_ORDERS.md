# Customer Open Orders — Stage 13 capability and API

**Status:** Checkpoints 13.1–13.6 and Stage 13 complete / owner-accepted — 2026-10-01,
at the bounded evidence depth in `docs/implementation/KST_v2_STAGE_13_CLOSEOUT.md`. Business baseline:
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
operational/QXtend validation. The later 13.5 endpoint requires a fresh targeted reread.
The cache is process-local.

- 200: complete loaded report, possibly stale-with-warning after a failed refresh.
- 400: missing, malformed, or empty-GUID MPS snapshot ID.
- 404: unknown workspace.
- 409: MPS not loaded or requested/current MPS snapshot mismatch.
- 503: QAD unavailable with no compatible last-good report.

Errors use normal Problem Details. Source exceptions, customer/order contents, and
connection details are not emitted as response text or query logs. The report-read endpoints
do not write to QAD, submit QXtend updates, save files, or persist report data.

## Limitations and must not infer

Bounded SW source checks confirmed join cardinality for 50 lines, but observed no
comment rows, true consignment values, nonblank holds, or negative QOH. See the ledger
for sanitized evidence. Do not infer stable order among comment records, inventory
usability from Site QOH, successful external updates from a report, or export eligibility
from a stale snapshot. The 13.2 backend delivered none of the desktop report, drafts,
editing, XLSX, or QXtend files. Later accepted checkpoints delivered these capabilities as
described below; none of the 13.2 bounded samples validates full-workspace performance.

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
generate QXtend changes. Planning Mode and drafts are described below; QXtend files remain a later checkpoint.

## Checkpoint 13.4 — Planning Mode and saved drafts (owner accepted)

Plan Mode forces the four editable dates, total Order Qty, raw Price and row Reason Code visible, without changing the Report Mode column layout. Changed rows preserve their source key, site, part, original values and proposal. The seven accepted Reason Codes are enforced; changed rows without one are incomplete. Proposed Open = proposed Order Qty − current Shipped Qty (equality allowed), and proposed Ext Price = proposed raw Price × proposed Open even for consignment lines. Decimal planning inputs and baselines are transferred as plain invariant strings (`planningValues`, `shippedQtyText`), avoiding JS floating-point arithmetic. Valid dates or intentional nulls are allowed; display uses M/d/yyyy. Undo, confirmed Clear All, change counts and hidden/conflicted-row discovery cover the entire workspace population.

`GET /api/v1/workspaces/{assignmentId}/open-orders/draft?mpsSnapshotId={guid}` checks a saved draft against a **newly acquired** Open Orders report, never against a cached GET. It returns `exists`, `restored`, `warning`, `freshReport`, and reconciled `rows` with issues; the UI asks the user to restore and re-enables saving after confirmation. Failed fresh acquisition leaves proposals visible but unrestored; corrupt drafts stay on disk for recovery and are reported rather than silently discarded. Changed or missing source rows preserve originals/proposals and are ineligible for later QXtend export. MPS-scope mismatch is a conflict.

`PUT .../draft` accepts current MPS and report snapshot IDs plus proposals, validates typed identities and plain decimal text, filters no-ops by value, and atomically saves JSON under `%LOCALAPPDATA%\KST\config\open-orders-draft-{assignmentId}.json` through a temporary file and rename. A report that is stale or whose snapshot does not match cannot be used to save. `DELETE .../draft` removes the local copy; turning Save Draft off requires confirmation and leaves memory intact. Workspace archival preserves it, permanent delete and reset remove it. Navigation warns when unsaved in-memory proposals would be discarded. Save failures remain visible. This checkpoint does not generate QXtend files, submit changes, or write to company databases.

**13.4 owner desktop correction (accepted):** Report refresh and filtered XLSX actions are disabled during Plan Mode, returning to their usual state in Report Mode. Plan Mode uses its own assignment-ID-scoped column layout (`kst.openOrders.planLayout.v1.{assignmentId}`), whose first-use/reset order is SO, PO, Line, Item Number, Open, Due Date, Perform Date, Required Date, Dock Date, Order Qty, Price. Reason Code follows Price; compact status and Undo follow as row actions. All eleven planning data columns remain visible; other report columns, including proposed Ext Price, may be selected and reordered. Report Mode layout stays under its accepted separate storage key. `GET .../draft/presence` checks only whether a draft file exists (no QAD read or JSON parse); only a positive result triggers fresh restoration and a compact in-progress indicator near Save Draft. The indicator ends for success, no draft, failure or superseded scope. Plan Mode dates are single text inputs accepting complete `M/d/yyyy` or `MM/dd/yyyy`; valid dates normalize to the existing ISO comparison/persistence value, blank means intentional null, and partial/invalid text remains visible without being staged as a cleared date. An incomplete date blocks saving and is marked not export-ready; Enter advances to the next editor when valid and Tab uses native focus order.

## Checkpoint 13.5 — QXtend export (desktop behavior owner-accepted)

`POST /api/v1/workspaces/{assignmentId}/open-orders/qxtend-export` accepts `mpsSnapshotId`, `openOrdersSnapshotId`, and the complete set of changed proposals for the workspace. It rejects empty/no-op proposals, duplicate source identities, malformed decimal/date inputs, and missing or invalid Reason Codes (400). The backend requires the caller's current loaded MPS scope and matching non-stale Open Orders snapshot (409), checks proposed rows against that snapshot's site and resolved parents, then rereads **only the changed domain/order/line identities** at the workspace site using a bounded parameterized read-only QAD query. Missing/non-open lines or workspaces return 404; changed source values, site/part/scope or insufficient proposed total quantity against freshly read shipped quantity return 409; QAD failure returns 503. No CSV bytes are produced until the whole set passes. This query was offline-tested at 13.5; a separately approved, single-line date-only live exercise at 13.6 is recorded below. The earlier 13.2 live-probe approval alone did not authorize that read.

Successful responses contain one to three typed `files` (`kind`, `fileName`, `contentBase64`), omitting unchanged families. CSV bytes have owner-template headers, sorted order/line and first-row parent grouping, CRLF and UTF-8 without BOM. The date output uses the verified owner-template header (`sodDte01` Dock Date); the local owner reference file is named `QXtend_DateChange.csv`, while the planned output name `DateChange_with_dock.csv` differs, so the generated file is suggested as `DateChange.csv` without claiming the original filenames match. The quantity and price suggestions are `UpdateQuantities.csv` and `UpdatePrices.csv`.

Plan Mode Export All prepares the files after fresh backend validation. The owner-selected UX is **separate user-triggered Save As per prepared file**, with independent saved, cancelled and retryable failure statuses; already-saved files and in-memory/persisted drafts are never removed by export. Changes to a proposal, report identity or workspace invalidate the prepared controls. Browser fallback initiates a user-clicked download per file. Generated files are proposals for external human review, not proof of QXtend acceptance. Report Mode XLSX and Refresh retain their accepted 13.3 behavior. The project owner accepted 13.5 desktop behavior on 2026-10-01; synthetic golden-byte tests passed. External QXtend validation and acceptance are not established by that review.

**13.5 desktop correction (included in owner acceptance):** The desktop sidecar dated 2026-09-30 predates the 13.5 API route while the frontend build included it. The frontend's generic export error previously concealed HTTP status and Problem Details; a date-only proposal with a selected Reason Code was consequently reported as an apparent validation failure without evidence that validation ran. The current endpoint reports only a sanitized `issueCode` and `affectedRowCount` in Problem Details for invalid proposals, missing/non-open lines, stale snapshots/scope, changed originals, below-shipped proposals, duplicate source rows, and source unavailability. The UI maps these codes to actionable feedback and identifies an unrecognized 404 as a possible running-backend/route mismatch; it never renders raw source exceptions or response bodies. A synthetic API-path date-only test derives original values from GET, supplies a Reason Code and freshly read current-line facts, and verifies that only the date CSV is returned. It does not establish live QAD behavior or QXtend acceptance. Desktop retesting requires a republished current sidecar; no new live QAD read is authorized by this documentation.

**13.5 owner display refinement:** In Plan Mode, whole-number Order Qty is shown without trailing decimal positions and fractional quantities retain their precision. Price displays exactly four decimal positions, visually rounded when the source has more; focusing a numeric editor reveals the unchanged raw value for editing. This is presentation-only: proposal originals, comparisons, draft persistence, fresh validation, and QXtend CSV values retain exact decimal values. A displayed four-place Price is not a change to the source value.

## Checkpoint 13.6 — accepted verification boundary

The current packaged Tauri external binary has been republished and checked offline with QAD
disabled: it is byte-identical to the published sidecar and responds from the 13.5
`qxtend-export` route with validation HTTP 400 for an empty request. The frontend production
build includes the generated route client; OpenAPI and generated types are synchronized.
These checks prevent a recurrence of the earlier desktop route mismatch in the tested artifacts,
but the offline smoke itself demonstrates neither a successful QAD transaction nor an external
QXtend import.
An approved one-pass small-workspace refresh and date-only targeted reread succeeded (see
`docs/implementation/KST_v2_STAGE_13_CLOSEOUT.md`); this does not prove identical-scope legacy
parity or broader performance. The owner separately reports that the **external QXtend process
accepted date, quantity and price file types**, without specifying an environment, timestamp,
row count or other test detail. KST did not submit or import files. The owner accepts the current
evidence limits: identical-scope legacy parity was not verified, the live refresh covered one
six-parent/four-row workspace only, and live cache-hit interaction timing was not measured.
The owner explicitly accepted 13.6 and Stage 13 with these evidence limits and the documented
full-lint/Rust-formatting exceptions; failed checks remain failed. The three untracked owner
templates remain reference evidence only; no file contents or customer rows belong in the review packet.

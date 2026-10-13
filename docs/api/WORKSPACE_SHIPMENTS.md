# Workspace Shipments — Stage 19 Part 2

Workspace Shipments listing and On Time Rate are implemented and **OWNER-ACCEPTED as Stage 19 Part 2 —
2026-10-13**. Fixed-column, display-only listing. See
`docs/implementation/KST_v2_STAGE_19_CLOSEOUT.md` and the Part 2 implementation evidence for retained
offline/live-verification boundaries.

Base: `/api/v1/workspaces/{assignmentId}/shipments`. Operation routes require
`X-KST-Workspace-Shipments-Session`: a nonempty canonical UUID, new for each screen visit.
Operation IDs are caller-generated UUIDs, registered first and never reused.

| Method/path | Purpose |
|---|---|
| GET `/spec` | Exactly the fifteen shared Part 1 defaults, ordered; maximum page size 200 |
| PUT `/operations/{id}` | Register an empty owned operation for an enabled configured assignment |
| POST `/operations/{id}/acquire` | Acquire complete immutable listing and full-result On Time counts |
| GET `/operations/{id}` | Owned state, acquisition timestamp, expiry, row count and On Time counts |
| GET `/operations/{id}/rows?offset=0&limit=100` | Bounded page from the same frozen result |
| DELETE `/operations/{id}` | Cancel/release; also forbids late registration of that identity |

Request: `WorkspaceShipmentsRequestDto` has only `partNumber`, `shipDateFrom`, `shipDateTo`.
The service reloads the enabled assignment configuration for every acquisition, then reuses the
authoritative QAD `QadMpsScopeResolver` to resolve its parent population. Its accepted rule is the
union of inclusive product-line discovery (when configured) and explicitly configured parents.
Product-line discovery keeps the existing `pm_code`, item status, site/domain, MRP dataset and supply
type predicates. Explicit parents remain included without current MRP activity or current master
metadata. No MPS snapshot is loaded or required.

The resolved collection is frozen into the operation request and joined server-side against each
invoice detail before acquisition publication or paging. It is carried as one parameterized XML
value table (`nodes()`), avoiding the SQL parameter-count limit and large-population truncation
without requiring a database compatibility-level change. Parent identifiers are XML-escaped; values
never enter executable SQL text. The extracted value's source length is joined to the recorded detail
length so an identifier beyond the SQL XML extraction width cannot be truncated into a false match.
Empty membership short-circuits to a complete empty result without
opening QAD; an empty scope cannot build a query. Site and integration-derived domain remain mandatory.
Blank part includes every member of the workspace parent set; otherwise exact parameterized literal
equality on recorded `idh_part` intersects membership. Outer whitespace is trimmed; leading zeros,
case, embedded characters and wildcard-looking text are not pattern syntax. No additional current
part-master validation is introduced.

There is no client-supplied parent list and no dependency on Finished Goods, an MPS snapshot or
selected rows. Refresh registers a new operation and re-resolves current persisted workspace scope;
an acquired operation's frozen rows and pages do not change if workspace configuration changes later.

The application reads the actual persisted enabled assignment. Current configuration has no
customer/Corp Group fields, so these do not participate in scope. Display labels are not authoritative.

Dates are independent inclusive calendar bounds on `ih_ship_date`; upper bound uses the next day's
exclusive midnight, including end-of-day timestamps. Both blank = all available history, including
null Ship Dates. Newest raw Ship Date first, nulls last, ascending `d.prrowid` breaks ties.
Physical details sharing invoice/order/line identifiers remain separate. No SQL TOP, history cap,
frontend full-history collection or invoice-date substitution.

During the same bounded-memory acquisition pass, the workspace operation counts each physical detail
with On Time `1` as numerator and each `1` or `0` as denominator. Null/unavailable values are excluded.
The counts are stored with the immutable operation and returned only by the Workspace Shipments
operation DTO; they do not depend on the current page and cause no additional QAD query. Zero eligible
rows is represented by numerator/denominator `0/0`; the UI displays `—` rather than a zero rate.

Exactly Part 1's final fifteen defaults are projected server-side; Due Date, Required Date and
Currency are absent. Shared retro-price, exact signed extension, On Time/raw datetime, optional
enrichment and header resolution behavior are retained. Rows include invoice RMA/memo/adjustment
activity; no net-physical-shipment/accounting-revenue claim. Cell values remain exact nullable strings.

400 invalid filters/session/page; 404 assignment missing/archived; 409 reused/busy/wrong-state
operation; 410 absent/expired/released/wrong assignment or session; 503 source/acquisition failure.
No partial or stale result fallback. Thirty-minute expiry and bounded disk operation/page ownership
reuse Part 1's implementation in a separate process-root store under
`%LOCALAPPDATA%\KST\workspace-shipments`. Report ownership is independent. Results and offsets are
removed on release/cancellation/failure/expiry; abandoned-root cleanup retains Part 1 limitations.
The listing has no preparation/export routes or native save ownership.

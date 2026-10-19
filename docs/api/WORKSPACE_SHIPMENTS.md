# Workspace Shipments — Stage 19 Part 2 / S22-010 listing amendment

Workspace Shipments acquisition and On Time rules are **OWNER-ACCEPTED as Stage 19 Part 2 —
2026-10-13**. **S22-010 Stage 22 listing amendment — COMPLETE / OWNER-ACCEPTED — 2026-10-19.**
The owner reviewed and accepted the rebuilt release application. Fixed-column listing supports
full-result header filtering and Workspace-specific paging. This explicitly amends presentation/view capabilities, not acquisition,
population, ordering or shipment calculations. See
`docs/implementation/KST_v2_STAGE_19_CLOSEOUT.md` and the Part 2 implementation evidence for retained
offline/live-verification boundaries.

Base: `/api/v1/workspaces/{assignmentId}/shipments`. Operation routes require
`X-KST-Workspace-Shipments-Session`: a nonempty canonical UUID, new for each screen visit.
Operation IDs are caller-generated UUIDs, registered first and never reused.

| Method/path | Purpose |
|---|---|
| GET `/spec` | Fifteen shared defaults, ordered, with filter metadata and Workspace page sizes 100/500/1000; maximum 1000 |
| PUT `/operations/{id}` | Register an empty owned operation for an enabled configured assignment |
| POST `/operations/{id}/acquire` | Acquire complete immutable listing and full-result On Time counts |
| GET `/operations/{id}` | Owned state, acquisition timestamp, expiry, row count and On Time counts |
| GET `/operations/{id}/rows?offset=0&limit=100&viewVersion=0` | Bounded version-checked page from the same frozen result / derived index |
| GET `/operations/{id}/view` | Current completed view, its filters/counts/On Time counts/version |
| POST `/operations/{id}/view` | Atomically derive a complete filtered view from frozen rows |
| POST `/operations/{id}/view/cancel` | Cancel a sequenced view request, including before it starts; does not release acquisition |
| POST `/operations/{id}/values` | Cascading, searchable full-population facets excluding their own column's filter |
| DELETE `/operations/{id}` | Cancel/release; also forbids late registration of that identity |

Acquisition request: `WorkspaceShipmentsRequestDto` has only `partNumber`, `shipDateFrom`, `shipDateTo`.
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
operation or stale view; 410 absent/expired/released/wrong assignment or session;
503 source/acquisition/view/facet failure or saturated heavy-work workers.
No partial acquisition or stale acquisition fallback. Failed/cancelled view builds preserve the previous
complete view of the same frozen acquisition. Thirty-minute expiry and bounded disk operation/page ownership
reuse Part 1's implementation in a separate process-root store under
`%LOCALAPPDATA%\KST\workspace-shipments`. Report ownership is independent. Results and offsets are
removed on release, acquisition cancellation/failure or expiry; view cancellation/failure preserves
the complete acquisition and last successful view. Abandoned-root cleanup retains Part 1 limitations.
The listing has no preparation/export routes or native save ownership.

## S22-010 accepted listing/filter amendment (2026-10-19)

Source of truth is the C# DTO/spec → OpenAPI → generated TypeScript contract. `/spec` exposes
`filterKind`: text, decimal, date, or values (On Time); Ship Date has null because its only date
controls remain acquisition bounds. Decimal source column metadata is unchanged, including On Time.
The Reports contract/store/routes retain their existing 200 maximum; no client page stitching.
Workspace accepts bounded limits 1–1000, with UI-supported/default sizes 100/500/1000 and 100.

View request: `expectedVersion`, monotonically increasing visit-local `sequence`, `filters`.
At most fourteen distinct supported columns, one filter per column, combined with AND:

- `values`: text or On Time only; `mode=include|exclude`, at most 1000 ordinal-distinct nonempty
  `values`, and `includeBlanks`. Include matches listed exact values; exclude matches all unlisted
  values. Global all is exclude-empty plus includeBlanks=true, **not** a capped include list.
  On Time supports only `1`, `0`, and the separate blank choice.
- `contains`: text only, nonempty literal `text`, ordinal-ignore-case; `%` and `_` are ordinary
  characters. `includeBlanks` independently controls blanks.
- `compare`: decimal only, `operator=eq|gt|lt|between`, exact `value`, plus `upper` only for Between;
  Between is inclusive. Optional blank inclusion. No exponent/grouping/rounding. Requests support
  at most 76 digit characters and 76 fractional places, at most 80 input characters. A bounded
  integer coefficient/scale comparison preserves source extensions exceeding System.Decimal's
  29-digit capacity; excess request precision/scale or reversed bounds is rejected, never rounded.
- `dateRange`: date only, independent inclusive `from`/`to` calendar bounds and blank inclusion.

Null and empty string are the same blank choice; whitespace is an actual value. Values are never
trimmed, case-folded, or interpreted as display captions: actual `(Blanks)` is distinct from the blank
choice. Text is bounded to 256 characters and rejects controls. Duplicate filters/values, unknown
columns, incompatible fields/operators, invalid dates/numbers/ranges and request bounds return 400.
Byte limits are 5 MiB for views, 512 KiB for facets, 1 KiB for cancel; both Content-Length and streamed
bytes are bounded before deserialization. These are **per-request**, not aggregate HTTP memory limits.

Acquisition `rowCount` and its full-result On Time metadata never change. The view separately returns
filtered `rowCount`, numerator/denominator, filters and monotonically increasing `version`, with
`operationId`. UI rate uses those view counts with the accepted eligibility/one-decimal formatting;
active filters show “filtered”. Zero eligible denominator remains unavailable, not zero percent.
Derived offsets preserve original physical-row order and multiplicity; no sorting or QAD read occurs.

Rows require the expected `viewVersion`; omitted version means initial version 0 only, never “latest”.
Mismatch returns 409. Page DTO carries operation identity, version and offset. Facet request carries
`viewVersion`, `column`, optional `search`, up to 1000 retained values; result echoes identity/version/
column and returns values/counts, separate blank count, full distinct total, matching distinct total,
truncation and retained-value counts. Counts honor every other column filter, not their own. Search
scans **all** eligible values. Top 1000 are ordered most frequent then ordinal value; blanks are separate.
Committed and requested retained values remain accessible (including zero counts) outside that cap;
their union is bounded to 2000. UI bulk search action is explicitly “displayed search results only”.

Views stream frozen rows into a pending disk index. Publication replaces the complete index and its
metadata together under the operation gate/lock after final cancellation/version/expiry checks.
Cancellation/failure never publishes partial rows, releases acquisition or extends expiry. A cancel
sequence also prevents a late same/older request from starting. A completion which won before cancel
is a completed view, not an undone build: frontend reconciles canonical state and labels that outcome.
Uncertain transport replies similarly reconcile rather than painting old pages with new metadata.
Client operation identity, view version and acquisition/page/filter/facet sequencing reject late replies.

All heavy acquisition/view/facet work shares the existing **two Workspace workers and eight operation
slots**, with fail-fast busy responses, not an unbounded work queue; Reports has its independent store.
Pages are one bounded result in browser memory. Facets use <=4096 strings / nominal 1 MiB chunks
(one bounded source value can exceed the threshold), 16-reader external merge, fixed sixteen-level
online compaction (<=240 run paths plus in-flight merge), and top 1000/retained bounded dictionaries.
Scratch disk remains linear in the acquired column, derived indexes <=8 bytes per acquired row each;
there is no new total-history quota or truncation. This is bounded-memory/file-count work, **not** a
constant-disk-size history promise. Files stay under the owned operation directory; release/expiry
cancels work and removes all derived artifacts after leases close, with existing best-effort cleanup
and abandoned-root limits. No caller paths, webview filesystem/shell grants or new dependencies.

Visit-local filters survive Apply/Refresh, but not departure. Acquisition edits are separate and the
actual applied dates/exact Part remain visible in retrieved scope. Page size alone may persist under
`kst.workspaceShipments.pageSize.v1`, validated against spec support. Size changes retain the former
first visible row within the new page without reacquisition; filter changes reset to page one.
No Open Orders migration/export is included. Owner-run release desktop acceptance does not imply
additional connected-source parity, exhaustive fault coverage or risk acceptance. Retained evidence,
resource/fault limits, old-debug/new-release identities and accepted closeout:
`docs/implementation/KST_v2_S22_WORKSPACE_SHIPMENTS_REDESIGN.md`.

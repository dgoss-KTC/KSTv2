# Historical Shipments — Stage 19 Part 1 contract

Implemented and **OWNER-ACCEPTED as Stage 19 Part 1 — 2026-10-13**. See
`docs/implementation/KST_v2_STAGE_19_CLOSEOUT.md` for the closeout and
`docs/implementation/KST_v2_STAGE_19_PART_1_FOUNDATION_EVIDENCE.md` for scoped implementation
evidence and limitations. The historical UI handoff records its original implementation sequence.

All operation routes require `X-KST-Report-Session`, a canonical UUID. Operation IDs are
client-generated UUIDs and registered before starting acquisition; they are never reused.
Routes under `/api/v1/reports/historical-shipments`:

| Method/path | Purpose |
|---|---|
| GET `/spec` | Authoritative column catalog, defaults, supported sites and capacities |
| PUT `/operations/{id}` | Register an empty owned operation |
| POST `/operations/{id}/acquire` | Validate filters and freeze complete invoice-detail result |
| GET `/operations/{id}` | Owned status/expiry/result metadata |
| GET `/operations/{id}/rows?offset=0&limit=100` | Bounded selected-column page |
| POST `/operations/{id}/prepare` | Stream XLSX from exactly this frozen selection/result |
| GET `/preparations/{id}/bytes` | Owned file stream; no QAD reacquisition |
| DELETE `/operations/{id}` | Release/cancel; idempotent for absent operations |

400 = invalid filter/session/page; 409 = wrong operation state; 410 = expired/released/not owned;
422 = unsupported workbook capacity; 503 = failed acquisition/preparation. No stale fallback.
Acquisition never publishes a partial result. Null means unavailable, not zero or blank fallback.
Filter dates are calendar dates, open-ended when only one bound is entered. Both blank means all
available invoice history. Corporate membership never widens invoice site/domain scope.

Display/export share effective selected columns. Currency is optional, including when monetary
fields are selected (explicit owner amendment). Values are nullable strings; decimal strings are exact, dates are ISO
calendar dates. There are no aggregate totals, unit normalization or accounting-revenue claims.
The UI persists column IDs only, never filters. It releases its operation on departure/replacement
and ignores replies from previous visits/operation IDs. A native save worker retains its own
immutable local file through completion, even after report departure.

## Owner-amended defaults — 2026-10-13

The authoritative `/spec` catalog supplies these defaults in order:
Ship Date, Due Date, Perform Date, Required Date, Sales Order, Customer PO, Line, Part,
Qty Shipped, Unit Price, Ext Price, Ship To, Ship Via, BOL, Invoice Number, SO Type, On Time.
Owner presentation amendment: Due Date and Required Date are optional rather than defaults.
Currency is also optional, never automatically appended, as explicitly confirmed by the owner.
On Time displays 1 as a green check and 0 as a red X, with accessible True/False labels;
null remains unavailable. XLSX retains the exact numeric 1/0/null values.
Column and On Time definitions are shared with the separate Workspace Shipments capability documented
in `docs/api/WORKSPACE_SHIPMENTS.md`.

- `originalCommit` retains its existing ID but is accurately labeled Perform Date (`idh_per_date`).
- `requiredDate` = `idh_req_date`.
- `invoicedQuantity` is labeled Qty Shipped but remains signed `idh_qty_inv`; it does not establish net physical shipments.
- `retroUnitPrice` = recorded `idh_retro_price`; `retroExtendedPrice` = exact signed quantity × retro price,
  without rounded SQL multiplication. A missing retro price stays unavailable, with no `idh_price` fallback.
- `unitPrice` / `extendedPrice` remain separately selectable original invoice price/value (`idh_price`).
- `salesOrder` = header `ih_nbr`; `detailSalesOrder` = detail `idh_nbr`, labeled Sales Order (invoice detail).
- `onTime`: the SQL CASE yields 1 when `ih_channel = 'Standard'` and `idh_due_date <= idh_per_date`,
  0 for Standard with `idh_due_date > idh_per_date`, otherwise null. Missing comparison dates yield null.
  Raw datetime values (including times) are compared before calendar-date display formatting. This is
  not actual-ship-date punctuality. SQL comparison/collation semantics are retained at the QAD boundary.
- The amended SELECT specification does not impose sample-query customer, 360-day, ship-date,
  header-site, NOLOCK or ship-date ordering restrictions. Accepted filters and physical-detail ordering stand.

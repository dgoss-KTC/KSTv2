# Stage 13.1 — Customer Open Orders source-contract ledger

**Status: 13.1 owner-accepted; 13.2 bounded validation and owner comment-order decision recorded.** This records
repository evidence at the `bd8bfeb` planning baseline and the subsequent owner decision. The
owner confirmed `sod__dte01` for Dock Date and authorized the legacy `so_partial`, `sod_qty_all`,
and `so_hold_stat` mappings and bounded read-only QAD verification. A metadata-only probe using
the configured QAD database, Windows Integrated authentication, and `sys.columns`/`sys.tables`/
`sys.schemas`/`sys.types` returned field names and SQL types only; no transactional rows were read.
This document does not authorize Checkpoint 13.2. The accepted behavior is in
`docs/prompts/STAGE_13_OPEN_ORDERS_PLANNING_PROMPT.md` and
`docs/implementation/KST_v2_STAGE_13_IMPLEMENTATION_PROMPT.md`. Legacy evidence below is from
`C:\Dev\kst\features\oor_sales.py` (`build_oor_query`, lines 127–273), and the older VBA
`F_OORBySalesperson.bas` (lines 214–266). The current documented source inventory is
`docs/data/qadpro2-data-map.md` (especially `so_mstr`, `sod_det`, `ad_mstr`, `pt_mstr`, `cmt_det`,
`ld_det`). The three owner-supplied CSVs in `docs/reference/QXtend/` are **untracked reference
files** (the three pre-existing untracked files observed in this worktree); this checkpoint must
neither modify nor stage them.

## Scope and query contract (accepted rule; no query implemented)

- Input: active workspace assignment ID, its **current** MPS snapshot ID, snapshot site, and the
  complete `MpsSnapshot.ResolvedParts` parent list (product-line-discovered union explicit parents).
  Do not derive the population from MPS source rows, customer number, salesperson, IOS, or BOM.
- Resolve the site's domain via the existing `QadSiteDomainMap` in the QAD boundary. A report line
  must have `sod_domain = @Domain`, `sod_site = @Site`, and `sod_part` in the parameterized,
  batchable resolved-parent set. An empty parent set yields a loaded-empty result without QAD read;
  a source failure does not become an empty result. Use the existing connection/cancellation/timeout
  convention and return *all* qualifying lines without `TOP`, pagination truncation, or per-line
  enrichment queries.
- The **only** open-line predicate is `(sod_qty_ord - sod_qty_ship) > 0`. No order or line status,
  completion, hold, type, RA/RMA, date, or customer filter qualifies/excludes rows. Query grain is
  `sod_domain` + sales order `sod_nbr` + `sod_line` (with `sod_site` and `sod_part` preserved for
  scope); do not silently deduplicate lines. Join order header by domain + order; customer/ship-to
  and part master by domain + their respective codes. Aggregate inventory by domain + site + part
  and line comments by domain + comment index in bounded sets; neither enrichment may multiply or
  remove sales-order lines. Join/cardinality assumptions require verification before production SQL.
- Acquire a full report field set once. Apply Customer Name contains, exact Customer, exact
  Salesperson, inclusive Product Line bounds, exact IOS, and inclusive Due Date bounds locally with
  AND semantics; the mandatory workspace scope replaces the legacy extra-filter requirement.
  Select `sod.sod_slspsn##1 AS Salesperson` in the full source contract as a **hidden local-filter
  source**, even though Salesperson is not an accepted selectable report column. It is never a
  mandatory SQL qualification or workspace-scope predicate.
  Default sorting is Customer Name → Item Number → Due Date. The default visible layout is Due
  Date, Order, PO, Line, Item Number, Site, Open, Stat, Ext Price. Other accepted fields remain
  selectable. The backend read must not discard hidden fields.
- Fresh export validation later rereads **only changed line identities** against the current
  workspace/MPS scope and open predicate, original editable values, and current shipped quantity.
  These are contractual constraints, not implemented 13.1 queries.

**13.3 workspace Report Mode amendment (owner request, 2026-09-30):** the historical exact-match
filter and legacy label statements above remain source evidence for 13.1, but no longer prescribe
the 13.3 UI. Its Customer Name, Customer #, Salesperson, IOS, SO, PO and Item Number text filters
are case-insensitive literal contains matches on the cached scoped report. Product Line and Due Date
retain inclusive bounds (Due Date needs both). The 13.3 report/XLSX labels use SO, Status,
Customer # and Planner for the stable `order`, `stat`, `customer` and `plnr` column IDs; this does
not rename QAD source fields or change the SQL scope/predicates. See `docs/api/CUSTOMER_OPEN_ORDERS.md`
§Checkpoint 13.3 for the implemented UI/export contract.

`Kst.Integrations.Qad.OpenOrders.QadOpenOrderQueryContract.BuildBatchQuery` is the **pure 13.1
query shape**, not an executing reader. It emits a full report field selection and binds domain,
site, and every parent via Dapper parameters; no raw input is interpolated. Empty parent scope is
handled before calling it. The associated offline tests exercise scoped parameters, the sole
positive-open predicate, locally filterable Salesperson without SQL qualification, read-only SQL,
legacy field sources, and nontruncation. The builder has
not been executed against live QAD and does not claim result/cardinality validation. Stage 13.2
still owns the runtime reader, batching orchestration, cancellation, timing, and row normalization.

## Field / source / rule evidence

The names below are the accepted report labels (legacy abbreviations in parentheses). "Map" means
the current data map lists the named field; it does **not** verify the legacy query's execution or
join cardinality. Unverified entries block final source acceptance where noted.

| Report field / source fact | Legacy report source and transformation | Current repository evidence / disposition |
|---|---|---|
| Due Date (Due) | `sod_det.sod_due_date` | Map: same. Editable; QXtend Due Date. |
| Order, Line (Ln), Item Number, Site | `sod_nbr`, `sod_line`, `sod_part`, `sod_site` | Map: same; preserve domain in source identity. |
| PO | `so_mstr.so_po` | Map: same; do not substitute `so_cust_po`. |
| Open, Stat, Ext Price | `sod_qty_ord - sod_qty_ship`, `so_stat`, `sod_price * (sod_qty_ord - sod_qty_ship)` | Map: all operands; retain exact decimal arithmetic and the legacy raw `sod_price` for Ext Price (do not substitute consignment-adjusted Unit Price). |
| Allocated | `sod_qty_all` | Owner-authorized legacy source; metadata-confirmed `decimal`. No allocation substitution. |
| Customer, Customer Name | `so_cust`, `ad_mstr.ad_name` via `ad_domain = sod_domain`, `ad_addr = so_cust` | Map: fields listed; verify cardinality before using enrichment joins. |
| Customer Part | `sod_custpart` | Map: same. |
| Dock Date | `sod__dte01` | Owner confirmed Python/VBA SQL mapping; metadata confirms `datetime`. QXtend targets `sodDte01`. `sod_dock` also exists but is `nvarchar`; do not use as the Dock Date. |
| IOS, Plnr, Prod Stat | `pt_warr_cd`, `pt_buyer`, `pt_status` via part + domain | Map: same; planner is legacy master field, no site override introduced. |
| Line Comments | when `sod_cmtindx <> 0`, concatenate `cmt_cmmt##1`…`##15` per `cmt_domain` + `cmt_indx`, prefix each comment record with `;`, remove first `;`; otherwise empty text | Map: comment fields and keys listed. Preserve concatenation behavior; comment record sequence/order and cardinality need source evidence before implementing set-based aggregation. |
| Line Hold | `sod_hold_stat` | Map: same (also lists `sod__chr03`; do not substitute). |
| Partials | `so_partial` | Owner-authorized legacy header source; metadata-confirmed `bit`. Do not substitute `sod_partial` (also `bit`, detail). |
| Perform Date, Required Date | `sod_per_date`, `sod_req_date` | Map: same. Editable; QXtend date fields. |
| Picked | `sod_qty_pick` | Map: same. |
| Product Line | `sod_prodline` | Map: same; inclusive filter uses line source, not master product line. |
| QA Hold, Revision | `sod__chr06`, `sod__chr05` | Map: same. |
| Remarks, Ship Acct, Ship Via | `so_rmks`, `so__chr01`, `so_shipvia` | Map: same. |
| Ship To | `ad_mstr.ad_sort` via `ad_domain = sod_domain`, `ad_addr = so_ship` | Map: same; do not substitute `ad_name`. |
| Site QOH | `SUM(ld_qty_oh)` over matching `ld_domain`, `ld_site`, `ld_part`; nullable if no rows | Map: `ld_det` keys and quantity listed. No nettable, status, positive-only, RA, or expiration rule for this legacy report field. Set-based aggregation must preserve null/negative behavior. |
| SO Hold Status | `so_hold_stat` | Owner-authorized legacy header source; metadata-confirmed `nvarchar`. Do not substitute `so__chr03`. |
| SO Type | `so_type` | Map: same; display only, not qualification. |
| Unit Price | `CASE WHEN sod_consignment = 'TRUE' THEN 0 ELSE sod_price END` | Map: both fields; preserve legacy display zero while using raw `sod_price` for Ext Price and proposed-price comparison. Verify QAD representation of consignment before normalizing. |
| Order Qty, Shipped Qty, Price (planning source facts) | `sod_qty_ord`, `sod_qty_ship`, `sod_price` | Map: same. Order Qty edits total ordered, proposed Open = proposed Order Qty − current Shipped Qty; proposed Ext Price = proposed Price × proposed Open. |
| Salesperson filter | `sod_slspsn##1` | Map: same, exact match; no mandatory salesperson scope in Stage 13. |

The Python legacy report selects the full set above, while the older VBA report has fewer fields
and differs on inventory (`in_mstr` joined then summed). For the accepted Stage 13 selectable
fields, use the Python report as field-behavior evidence under the Stage 13 plan; do not import the
VBA inventory behavior into Site QOH. The owner resolved the source-name decisions; live metadata
confirmed the disputed columns, but join/cardinality and representative value behavior are not
established by a metadata-only query. No source field is admitted solely because
an older query mentions it.

## QXtend reference contract and deterministic fixture coverage

Owner files are `QXtend_UpdateQuantities.csv`, `QXtend_UpdatePrices.csv`, and
`QXtend_DateChange.csv` (the planning prompt calls the date template
`DateChange_with_dock.csv` — **filename discrepancy**, not a license to change its header). Exact
header bytes and column order must be copied from the supplied files, not normalized or renamed:

1. Quantity: parent Operation, parent Sales Order, detail Operation, detail Sales Order, Line,
   Quantity Ordered, `reasonCode`.
2. Price: same first five, `Reprice/Edit`, `reasonCode`, List Price, Price.
3. Date: same first five, `reasonCode`, Required Date, Due Date, Performance Date, Dock Date
   (`salesOrderDetail.sodDte01`).

The templates contain example/blank rows, which are *not* output data. Deterministic fixture cases
must cover: two lines in one order plus another order (first-row-only A/B parent values, C=`M`
and D=order on **every** row, sorted order/line), mixed family changes and one reason per changed
row, no-op removal, unchanged effective dates in a date change, intentional empty date, quantity
below/equal shipped, consignment Unit Price versus Ext Price, null and negative Site QOH, comments,
AND-combined filters, and raw decimal prices with different scales. Golden outputs must have exact
template headers, CRLF, UTF-8 without BOM, RFC-style CSV escaping, `M/d/yyyy` dates, invariant
non-scientific decimal prices (same edited value in both price fields), `TRUE` for Reprice/Edit,
and no unused template rows. The only valid changed-row reasons are `Cust/PM`, `Buyers`,
`Planning`, `Factory`, `C&R`, `Quality`, and `Engineer`; repeat the row's reason in every
applicable file. These are offline contract cases, not evidence that QAD contains any synthetic
value or that QXtend accepted a generated file.

The synthetic, reproducible report/row fixture is
`docs/reference/STAGE_13_OPEN_ORDERS_DETERMINISTIC_FIXTURES.json`: it holds original/proposed
decimals as text, explicit inclusion and filter expectations, mixed-family output cells, parent
grouping across two lines of the same order, and CSV-escaping expectations. Its
date cases include an intentional empty Dock Date and a nonempty `2027-01-06` Dock Date rendered
as `1/6/2027`. Both are asserted against the expected logical rows and golden CSV. Its
header strings were checked byte-for-byte against the three owner templates during 13.1 review.
Companion synthetic golden CSV files in `docs/reference/Stage13Fixtures/` are CRLF-terminated
UTF-8 without BOM, with no unused template rows. Offline tests check their recorded exact headers
and logical rows without depending on the untracked owner files. The actual serializer
and export-vs-golden byte comparison belong to 13.5; these expectations do not claim QXtend
acceptance or an export implementation at 13.1.

## Minimum separately approved read-only source verification

**Owner authorization received; metadata-only phase executed.** The read-only SQL Server probe
joined `sys.columns`, `sys.tables`, `sys.schemas`, and `sys.types` for `dbo.so_mstr` and
`dbo.sod_det`, restricted to seven relevant column names. It reported column presence and type,
without selecting any transaction data or exposing a connection string. The owner-selected Dock
Date field is typed `datetime`; `sod_dock` is `nvarchar`, so a substitution would be incorrect.
Do not substitute `sod_partial` or `lad_det` for the validated legacy sources. Representative
join/cardinality, comment-order, consignment representation, SO hold value comparisons, and
correlated enrichment performance remain explicit Stage 13.2 verification items; neither this
metadata check nor offline SQL-shape tests prove those live behavior assumptions. Do not fetch
broad customer data or run QXtend.

Executed metadata-only phase (2026-09-30): `so_mstr.so__chr03 nvarchar`,
`so_mstr.so_hold_stat nvarchar`, `so_mstr.so_partial bit`, `sod_det.sod__dte01 datetime`,
`sod_det.sod_dock nvarchar`, `sod_det.sod_partial bit`, `sod_det.sod_qty_all decimal`.
No sample rows or customer/order values were retrieved. The preliminary attempt failed locally
while constructing a PowerShell connection string (no SQL executed); the corrected read-only
metadata query succeeded. No QAD schema or data was changed.

## Gate

The Stage 13 implementation prompt §8 requires source verification **before production SQL**.
The disputed source names and SQL types have been reconciled, and a pure query-shape builder plus
synthetic fixtures were checked offline without opening an order-data connection. At the
13.1 gate, runtime reader, endpoint, and serializers remained later checkpoints. Owner
acceptance of 13.1 and authorization for 13.2 were separate gates.

## Stage 13.2 bounded source observations (2026-09-30)

Owner-approved, parameterized read-only probes ran against the current Shure SMT / SW MPS
snapshot (38 resolved parents) for three confirmed parents. Samples had 10, 22, and 18
positive-open lines respectively, bounded at TOP (100) per part for validation only.
All 50 sampled lines matched exactly one order header, customer, ship-to, and part-master
record. None had comments. All 50 had raw consignment False and blank SO Hold; Site QOH
was null for all 50. Thus multi-row comments, consignment True, nonblank holds, and
negative Site QOH were not witnessed in this bounded sample; the accepted legacy
source mappings and nullable/negative semantics remain in force. Cardinality probes
took 96/16/6 ms, code-value probes 1207/11/14 ms, and correlated comment/QOH
probes 30/5/5 ms, respectively. These are small samples, not a full-workspace
benchmark or evidence for a SQL rewrite. No customer/order/comment text, credentials,
or connection string was retained. No database or QXtend writes occurred.

**Owner comment decision:** preserve the legacy query's unordered *comment-record*
behavior. Within each record, concatenate `cmt_cmmt##1` through `cmt_cmmt##15`
in that order. The owner independently verified this behavior in SSMS. Do not
claim a guaranteed record order or silently impose `cmt_seq` ordering. The
Stage 13.2 acquisition retains the accepted correlated query shape; no performance
rewrite is justified by the small-scope observation.

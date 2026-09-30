# Stage 13 Open Orders Planning Prompt

Continue KST v2 planning for **Stage 13 — Open Orders** in the repository:

`/home/david/dev/KSTv2/`

The Release 1 roadmap review is complete and owner-approved. Stage 13 now precedes Stage 14 Planning
Workbook. The Stage 13 plan was accepted by the project owner on 2026-09-29. This document records
that accepted planning baseline; it is not implementation authorization. The stage-aligned version
is `0.1.0-alpha.13`.

## Authority and preflight

Before proposing Stage 13 implementation:

1. Read every applicable `AGENTS.md`.
2. Inspect the current branch, `git status`, and recent history.
3. Preserve all local and untracked work. In particular, do not silently add, remove, relocate, or
   absorb the existing untracked Stage 11 reference files.
4. Read at minimum:
   - `docs/status/CURRENT_PROJECT_STATUS.md`
   - `docs/status/RELEASE_1_SCOPE_REVIEW_2026-09-29.md`
   - `KST-v2-Master-Project-Checklist.md`
   - `docs/architecture/BACKEND_PROJECT_BOUNDARIES.md`
   - current API conventions and contract workflow documentation
   - current database/source documentation relevant to sales and open orders
   - accepted Stage 4–11 closeouts where their contracts are reused
   - `SECURITY.md` and applicable security, dependency-admission, and licensing policies
   - export and external-update-file documentation relevant to QXtend
5. Inspect the current implementation and legacy/reference evidence before suggesting new models,
   services, queries, endpoints, components, or dependencies.

Apply the authority tiers in `AGENTS.md`. Accepted implementation and closeout evidence outrank
older planning assumptions. Do not reopen locked Stage 9–11 behavior unless current evidence exposes
a genuine conflict and the owner explicitly approves an amendment.

## Authorization boundary

Do not implement Stage 13 until the owner separately authorizes the implementation prompt and its
first checkpoint. Until then, do not:

- implement production code;
- run live QAD or Shortages database investigations;
- install or admit dependencies or tools;
- create or modify database objects, permissions, indexes, schema, or server configuration;
- change established business behavior;
- commit or push.

The accepted implementation prompt is maintained separately at
`docs/implementation/KST_v2_STAGE_13_IMPLEMENTATION_PROMPT.md`.

## Accepted Stage 13 intent

Stage 13 adds a workspace module named **Customer Open Orders** before Component Orders. It is
strictly scoped to the active workspace's site and the exact parent population resolved by the
current MPS snapshot: product-line-discovered parents unioned with explicitly assigned parents.
Cross-customer investigation outside a workspace is not Stage 13; it belongs to Stage 18.

The module has two states:

1. **Report Mode (Planning Mode off)**
   - Read and display every qualifying open sales-order line in the workspace scope.
   - An open line satisfies `sod_qty_ord - sod_qty_ship > 0`. Do not invent additional status,
     hold, completion, order-type, or RA exclusions.
   - Query the complete accepted field contract once, then perform column visibility, column order,
     filtering, and sorting locally without re-querying QAD.
   - Export the filtered workspace report as an Excel workbook using the visible columns in their
     displayed order.

2. **Planning Mode**
   - Stage edits to Due Date, Perform Date, Required Date, Dock Date, Order Qty, and Price.
   - Require a Reason Code on changed rows from the exact accepted list: `Cust/PM`, `Buyers`,
     `Planning`, `Factory`, `C&R`, `Quality`, and `Engineer`.
   - Optionally persist a workspace-specific local draft through the Save Draft toggle.
   - Revalidate every changed line against fresh QAD data before export.
   - Produce human-reviewable QXtend-compatible date, quantity, and price CSV files containing only
     staged changes.
   - Never submit files to QXtend and never write directly to QAD or another company database.

The workspace report approved here belongs to Stage 13. Stage 18 owns the later standalone
cross-customer Open Order Report. Stage 21 still owns product-wide export inventory and consistency,
but must not remove or defer the Stage 13 workspace report or QXtend files.

## Accepted report fields and presentation

Default visible columns are Due Date, Order, PO, Line, Item Number, Site, Open, Stat, and Ext Price.
Every legacy optional column remains selectable: Allocated, Customer, Customer Name, Customer Part,
Dock Date, IOS, Line Comments, Line Hold, Partials, Perform Date, Picked, Plnr, Prod Stat, Product
Line, QA Hold, Remarks, Required Date, Revision, Ship Acct, Ship To, Ship Via, Site QOH, SO Hold
Status, SO Type, and Unit Price.

Visibility and order are remembered per immutable workspace assignment ID across sessions. Pointer
drag-and-drop and accessible Move Left/Move Right controls are both required. Hidden columns retain
their relative position, unknown saved column IDs are ignored, and Reset restores the accepted
default. The query and API always return the full data contract regardless of the visible layout.

Legacy filters are Customer Name contains, exact Customer number, exact Salesperson, inclusive
Product Line range, exact IOS, and inclusive Due Date range. Supplied filters are AND-combined.
Workspace scope itself satisfies the legacy requirement for a bounded query, so no additional
filter is required. Default report sorting remains Customer Name, Item Number, then Due Date.

## Accepted planning rules

- Order Qty is the original total `sod_qty_ord`, not the derived Open quantity. The original remains
  the comparison baseline. A proposal below the freshly read shipped quantity is invalid; equality
  is allowed and may close the line.
- Price accepts a plain decimal-capable numeric value with no currency symbol or grouping. One Price
  edit populates both QXtend List Price and Price. Internally use exact decimal arithmetic rather
  than binary floating-point money calculations.
- Dates accept valid calendar values or an intentional empty value. Do not invent chronological,
  past-date, frozen-date, or status restrictions. QXtend dates use `M/d/yyyy`.
- Reason Code is required only on a row with a material staged change. The same row-level reason is
  repeated in every applicable QXtend file.
- Highlight changes, retain originals, recompute the proposed Open quantity and Extended Price,
  remove no-op edits, and support row undo plus Clear All.
- Export does not clear changes because file generation does not prove successful QXtend processing.

Save Draft is off by default. When enabled, proposed values, originals, reasons, and source identity
are persisted locally for that workspace. Restored drafts must be checked against a fresh read;
conflicting rows remain visible but are blocked from export. Turning Save Draft off removes the
persisted copy after confirmation but does not discard current in-memory edits. Permanent workspace
deletion removes its draft; archival preserves it.

## Accepted QXtend boundary

Use the owner-supplied `UpdateQuantities.csv`, `UpdatePrices.csv`, and
`DateChange_with_dock.csv` templates as the exact column-order/header evidence. Generate up to three
separate CSVs containing only changed rows. A row changed in multiple families appears in each
applicable file.

For every QXtend file:

- sort Sales Order then Line ascending;
- put `M` in detail Operation column C on every row and the Sales Order in detail column D;
- copy C and D into parent columns A and B only on the first exported row for each Sales Order;
- leave A and B blank on later rows in the same Sales Order group;
- preserve exact template headers, CRLF line endings, UTF-8 without BOM, normal CSV escaping, and no
  unused template rows.

Quantity exports set Quantity Ordered and Reason Code. Price exports set Reprice/Edit to `TRUE`,
repeat the same edited decimal into List Price and Price, and include Reason Code. Date exports
include Reason Code and all four effective date values serialized as `M/d/yyyy`.

## Required implementation evidence

Before production work passes its source-contract checkpoint, reconcile the legacy `sod__dte01`
Dock Date source with the documented `sod_dock` field and verify legacy-only references including
`so_partial` and `sod_qty_all`. This is technical source verification, not permission to invent a
business rule or conduct an unbounded live investigation.

Use a workspace- and MPS-snapshot-scoped read contract plus an Open Orders snapshot identity. A
failed refresh may leave a clearly stale report visible, but QXtend generation must perform a fresh,
targeted reread and fail closed when the MPS population changed, a line disappeared or is no longer
open, an original editable value changed, shipped quantity invalidates the proposal, validation
fails, or QAD is unavailable.

Keep SQL/schema knowledge in `Kst.Integrations.Qad`, business rules and orchestration in
Domain/Application, CSV/XLSX creation in `Kst.Exports`, DTO mapping in `Kst.Api`, and UI state in the
frontend. Use the C# DTO → OpenAPI → generated TypeScript flow. Reuse the existing ClosedXML and
Tauri save infrastructure. No new dependency is approved by this plan.

Verification must cover source-reader scope and read-only SQL, domain validation, snapshot and stale
behavior, draft lifecycle, layouts, accessibility, filters/sorting, exact byte-level QXtend golden
files, report export, cancellation/failure, and representative owner comparison with the legacy
report and external QXtend acceptance.

## Non-scope

- Cross-customer/global Open Orders, which belongs to Stage 18.
- Stage 14 Planning Workbook, forecasts, projections, or MPS adjustments.
- Automatic QXtend submission/import or any direct company-database write.
- Named multi-layout presets and speculative advanced grid personalization; these remain Stage 22
  candidates.
- Reopening locked Stage 9–11 algorithms.

Owner disposition — 2026-09-29: **PLAN ACCEPTED; IMPLEMENTATION NOT YET AUTHORIZED.**

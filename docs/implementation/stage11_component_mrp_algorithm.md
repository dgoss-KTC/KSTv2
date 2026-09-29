# Stage 11 Component MRP and Shortage Algorithm

Status: COMPLETE / ACCEPTED / LOCKED — 2026-09-29

## Purpose

Stage 11 produces a site-level component shortage projection from a selected parent/model BOM scope. Calculations are daily. Presentation is weekly.

The BOM scope determines which components are displayed. Once selected, each component uses all site-level inventory, demand, and supply. Shared quantities are not allocated to individual parents or work orders.

## Run controls

- Site and parent/model scope
- Horizon: 13, 26, 52, or 72 weeks; default 26
- Confirmed-only mode by default
- Optional Include Unconfirmed mode
- Show All: include every eligible purchased component, including healthy parts
- Show Manufactured: independently include manufactured components
- Sort selection: most urgent, first shortage, deepest shortage, recovery date, buyer/planner, or component number

The API supplies all eligible purchased rows and both selected-mode projections from one
acquisition. Show All, receipt mode, search, KSS, status, and sort act on that loaded result in
the client; they do not re-read QAD. The backend preserves the same acquisition timestamp when
reprojecting a shorter horizon from retained source facts. Manufactured population changes or a
longer horizon without retained facts require acquisition; the current grid remains visible while
the replacement loads. Export uses the same cached acquisition and selected projection.

Performance amendment (2026-09-29, owner accepted): the screen uses the dedicated immutable
`/long-term-shortages/screen` contract with shared week metadata, component fields, both mode summaries
and backend-calculated raw/display ending vectors. Drawer-only projection information is selected from
the exact complete cache via `/long-term-shortages/projection-detail`; missing/replaced/incomplete
snapshots fail closed without acquisition. The legacy complete API remains available, and cached
projections/workbooks retain every source fact. Production uses the verified dense dual-mode ledger,
preserving evidence/category sum order and indexed weekly-low arithmetic grouping. Stage 11 fact
reads use a reader-local maximum of 250 components per batch on the same READ UNCOMMITTED connection;
presentation reads retain the configured batch size. Owner acceptance covers report accuracy,
display/usability, cold-load performance, cached performance, and overall Stage 11 readiness.
Evidence and retained diagnostic tradeoffs:
`KST_v2_STAGE_11_PERFORMANCE_INVESTIGATION.md`.

## Component population

1. Recursively explode the horizon-effective BOM for the selected parent/model scope.
2. Traverse phantom parts but do not display them.
3. Use site `ptp_pm_code` when nonblank; otherwise use global `pt_pm_code`.
4. Display purchased (`P`) components automatically.
5. Display manufactured (`M`) components only when Show Manufactured is enabled.
6. Traverse other or unknown P/M codes when required to reach lower components, but do not display those parts.
7. Default results contain components that become Short or At Risk during the selected horizon. Show All adds healthy eligible components.

## Opening inventory

Opening inventory is the signed sum of nettable, non-RMA site inventory.

Status resolution order:

1. Nonblank `ld_det.ld_status`
2. Otherwise `loc_mstr.loc_status`

Nettable statuses:

`INSPECT`, `NCMINSP`, `RIP`, `STOCK`, `TRAN`, `VMI`

Non-nettable statuses:

`ADJUST`, `MRB`, `REWORK`, `RTV`, `TEMP`

RMA lots are excluded. External supplier/bond inventory is excluded.

## MRP event routing

Process every distinct `mrp_det` record. Never deduplicate rows based on matching business values. QAD may produce separate records with the same part, source, date, and quantity but different `mrp_line2` and `prrowid` values.

Retain `mrp_nbr`, `mrp_line`, `mrp_line2`, and an exact text representation of `prrowid` for traceability. Internal QAD OID fields are not required for business logic. If a `READ UNCOMMITTED` scan returns the same `prrowid` more than once, collapse only those repeated copies of the same physical source row. Distinct `prrowid` values must always remain separate events.

| Source event | Treatment |
| --- | --- |
| Any `DEMAND` row | Component requirement; subtract on due date |
| PO `SUPPLY` | Confirmed or unconfirmed receipt using the PO-line confirmation flag |
| Non-PO `SUPPLY` | Firm receipt included in both official modes |
| `SUPPLYP` due date | Planned receipt in planning balance only |
| `SUPPLYP` release date | Action signal only; never inventory |
| `SUPPLYF` | Parent forecast signal; ignore for component balance |

All component demand is included. Use the internal classification `COMPONENT_REQUIREMENT`; a component MRP record alone does not prove that its originating demand is firm.

## KSS

- Expose an explicit `isKss` field from scheduled PO header or line indicators.
- KSS is a visibility attribute and does not change the ledger.
- Blanket or scheduled quantities do not count as supply unless QAD produces a dated MRP receipt.

## Past due

`asOfDate` is the requested report date in the application's local-date convention. An event is
past due precisely when its due date is before `asOfDate`; events dated `asOfDate` are processed in
the current-day ledger. The preceding-or-current Sunday (`weekStart`) is only a presentation boundary,
never the cutoff for past due or the start date of a shortage episode.

`AdjustedOpening = OpeningNettableInventory - PastDueComponentDemand`

- Past-due demand consumes opening inventory.
- Past-due receipts remain visible as overdue but do not increase inventory.
- A receipt contributes only after QAD provides a current or revised receipt date.
- A negative adjusted opening starts its episode on `asOfDate` (the ledger does not reconstruct
  historical inventory).

## Daily ledgers

Maintain confirmed-only and include-unconfirmed projections simultaneously.

```text
ConfirmedEnding[d] =
    ConfirmedEnding[d-1]
    - Demand[d]
    + FirmNonPoReceipts[d]
    + ConfirmedPoReceipts[d]
```

```text
AllReceiptsEnding[d] =
    AllReceiptsEnding[d-1]
    - Demand[d]
    + FirmNonPoReceipts[d]
    + ConfirmedPoReceipts[d]
    + UnconfirmedPoReceipts[d]
```

Maintain equivalent planning ledgers with `SUPPLYP` receipts added.

Same-day processing order:

1. Component demand
2. Firm and confirmed receipts
3. Unconfirmed receipts when selected
4. Planned receipts in the planning ledger

Record the post-demand low before applying same-day receipts.

## Status

Use the selected official balance after applying the UOM evaluation rounding rule.

```text
Short:    balance < 0
At Risk:  safety stock > 0 and 0 <= balance < safety stock
Healthy:  balance >= 0 and balance >= safety stock when safety stock applies
```

If safety stock is unknown, hard shortages remain calculable but At Risk is unavailable.

The **summary severity** uses this precedence over the daily ledger: `CriticalShort` for any
negative evaluated balance on `asOfDate` (including adjusted opening or the post-demand low);
`FutureShort` if the first negative balance is later in the selected horizon; `SafetyStockShort`
if never negative but below known safety stock at any point; otherwise `Healthy` when safety stock
is known. Unknown safety stock is `SafetyStockUnavailable`, not proven Healthy. Default display
includes CriticalShort, FutureShort, and SafetyStockShort only; Show All also includes Healthy and
SafetyStockUnavailable. A short that recovers before a weekly ending stays visible.

## Lead time

Lead time annotates urgency; it does not remove future shortages from the selected horizon.

- Order period: calendar days
- Safety time: working days and already reflected in QAD MRP dates; do not reapply
- Manufacturing lead time: site working days
- Purchasing lead time: calendar days
- Cumulative lead time: calendar days and informational
- Purchased effective boundary: site purchasing lead time
- Manufactured effective boundary: site manufacturing lead time

Site `ptp_det` is authoritative for safety stock and lead time. Missing site planning data remains unknown and produces a warning; do not substitute global planning values.

## Component Information presentation (2026-09-25)

The shared Component Information modal keeps component-master planning fields, Reference,
Manufacturer Item, and Approved Alternates in its first independently flowing column. Inventory /
Lot Locations remains the separate middle-column future-stage placeholder. Workspace Shortages adds
a third column for the selected projection snapshot, UOM-rounded Past figures, Current Buyer Comment,
and conventional open purchase orders. BOM launches do not request shortage purchasing detail.
The regular drawer does not show the weekly timeline, episodes, raw MRP evidence, or the duplicate
site-planning and PO/KSS sections; the underlying projection, API evidence and workbook remain.

Selected-component purchasing detail requires a compatible current cached shortage projection and
uses the accepted Stage 10 conventional-open PO reader for only that selected site/component. It
does not rerun the MRP acquisition or bulk BOM scope. Open PO lines are informational, sorted with
past-due lines first and remaining dated lines in ascending due-date order; missing due dates are
explicit exceptions. PO open quantity is ordered minus received in PO units, with no asserted UOM
conversion; the drawer applies the component UOM display-rounding rule only. The buyer comment
uses the existing ShortageMaster site/component enrichment reader independently of whether any PO
exists. Per the owner's clarification, the **latest active record** wins by Modification Date,
Added Record Date, then id even if its comment is blank; blank yields “No buyer comment on file.”
The current Component Orders response exposes no author or comment date, so the drawer labels it
“Current Buyer Comment” and does not invent metadata. Optional comment-source failure is shown as
unavailable while the QAD PO lines remain visible.

## Weekly presentation

Calculations remain daily from `asOfDate`. Presentation buckets run Sunday through Saturday with
Monday as the visible label. The first bucket includes only `asOfDate` through Saturday; events
from that Sunday's start to the day before `asOfDate` belong to Past Due. A separate Past Due
column precedes the weekly buckets. Weekly lowest balance must be evaluated over the actual
ledger days, not reconstructed from pre-report-date events.

Weekly metrics:

- Gross Requirements
- Confirmed Receipts
- Unconfirmed Receipts
- Scheduled Receipts for the selected mode
- Planned Orders Due
- Planned Orders Release
- Lowest Projected Balance
- Ending Projected Balance
- Ending Planning Balance
- Weekly Status

Compact visible layout:

- Gross Requirements
- Scheduled Receipts
- Lowest Projected Balance
- Ending Projected Balance
- Planned Due
- Ending Planning Balance
- Planned Release

## Shortage episodes

Retain for every episode:

- Start date
- Deepest-shortage date
- Maximum shortage quantity
- First recovery date
- Stable-clear date

Maximum shortage is the absolute value of the deepest projected balance. Never sum daily negative balances. Preserve multiple shortage and recovery episodes.

`First Short` remains available internally for sorting but is not required as a visible column.

## Reference fields

Expose component number, descriptions, buyer/planner, effective P/M code, part status code and description, KSS, normalized UOM, safety stock, applicable lead times, and data-quality warnings.

Part status is informational only. Known mappings:

- A: AEMR
- B: BYPASS
- C: CURRENTLY IN PRODUCTION
- E: END OF LIFE
- F: FORECAST OR FAMILY BORN
- H: PURCHASING HOLD
- I: INACTIVE PURCHASED PART
- M: MFA
- N: NPI
- O: OBSOLETE
- P: PROTO
- Q: QUOTED PART
- U: UNRELEASED

## UOM rounding

Normalize by trimming and converting to uppercase. Preserve full precision internally.

Zero decimal places:

`BX`, `EA`, `PK`

All other UOMs, including missing, blank, or unfamiliar values, use two decimal places. Retain
the normalized UOM for informational display; unfamiliar values do not produce a UOM warning.

## Data quality

- Successful query with no rows means zero.
- Failed, partial, or stale retrieval means `UNKNOWN`.
- `UNKNOWN` must never be converted to Healthy.
- QADPro2 is a Progress Pro2 reporting target. Use the vendor-compatible `READ UNCOMMITTED` isolation level, or equivalent `NOLOCK` behavior, so report reads do not impede replication into the SQL target.
- Do not require `SNAPSHOT`, `READ_COMMITTED_SNAPSHOT`, `REPEATABLE READ`, or `SERIALIZABLE` isolation for report availability.
- Keep source acquisition short, narrowly scoped, and on one connection. Avoid long-lived explicit transactions.
- Snapshot isolation is not required; its absence is not an availability error. Do not fall back to `READ COMMITTED`, `REPEATABLE READ`, or `SERIALIZABLE`, or change server options.
- Pro2 exposes `mrp_det.prrowid` as `varchar(36)`; read its exact text without numeric conversion for physical source-row identity. Collapse repeated reads of the same row ID only; never collapse distinct row IDs even if their business fields match.
- Return the report acquisition timestamp and `PRO2_READ_UNCOMMITTED` consistency mode; identify the source as a near-real-time Pro2 reporting replica.
- Fail closed only for actual query failures, timeouts, missing required result sections, invalid classifications, or otherwise demonstrably incomplete retrieval—not merely because snapshot isolation is unavailable.

## Validation conclusions

- Signed nettable opening inventory matched the established KST calculation.
- Confirmed and unconfirmed MRP PO receipts matched direct PO lines on part, PO, line, due date, quantity, and confirmation flag.
- Confirmation is a receipt-level attribute, not a part-level attribute.
- KSS schedule records are visible but are not dated MRP supply.
- `SUPPLYP` provides distinct due and release dates and belongs only in the planning ledger.
- Future component requirements were present around the expected 18-week forecast boundary as component `DEMAND` records.
- Matching demand rows for KSS were verified as distinct QAD records through different `mrp_line2`, row IDs, and keys; both must be counted.

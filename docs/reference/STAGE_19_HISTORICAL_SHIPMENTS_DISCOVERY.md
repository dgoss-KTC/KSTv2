# Stage 19 discovery — static/offline source inspection

**The legacy screen is a customer shipment report generator that combines invoice history with uninvoiced consignment shipments and optionally adds serial/package detail. It is not a simple one-row-per-shipment lookup.**

All findings below are **static/offline source inspection**, not tested or live-validated behavior. References are to `docs/reference/ktmg1160.p` unless otherwise stated. These are planning inputs, not accepted Stage 19 requirements.

Stage 18 remains COMPLETE / OWNER-ACCEPTED with its documented evidence limits preserved, including no claim of exhaustive live parity, printing or desktop coverage (`docs/implementation/KST_v2_STAGE_18_CLOSEOUT.md`, lines 40–54).

## Business purpose and workflow

**Explicit source behavior:** The user selects customers, site/part ranges, dates, order types and an output layout; chooses formatted report or worksheet format; and supplies an email address or configured distribution-list name. The program builds temporary results, writes a file and sends it by email or through configured SFTP distribution. Ordinary emailed files are removed afterward. There is no interactive shipment drill-down in the named file.
**Evidence:** lines 227–319, 540–575, 578–657, 2606–2732.

**Inferred intent:** The numerous customer-specific layouts support shipment notifications and customer receiving/reconciliation processes, alongside general shipment reporting.

## Filters, defaults and validation

| Input | Visible default / validation | Actual use |
|---|---|---|
| Corporate group, customer range, customer type; customer-region range | Customer endpoints and corporate group validated; type validated separately. Group and type cannot both be entered. No explicit requirement for a nonblank customer selector. | Customer population comes from missing `ktso1194.i`; exact precedence, region logic and membership are unresolved. Lines 290–340, 400–414, 544. |
| Site, part, product-line ranges | No explicit initial values or existence validation here. Blank upper endpoints become open-ended. | Inclusive range selection. Invoice product line comes from history; consignment product line comes from current part master. Lines 532–538, 588–597, 636–639. |
| Salesperson range and position | Validation delegated to missing `ktso1200.i`; position default unavailable. | Selected salesperson-array position on invoice/transaction history. Lines 530, 590–591, 615–616. |
| Ship-to | Blank initially; warning unless layout 2. | Exact-match filter only for **invoice rows in layout 2**; no equivalent consignment filter is visible. Lines 494–496, 2906–2907. |
| Report timetable | Blank; accepts D, W or blank. Cannot coexist with entered dates. | Derives selection dates; important differences below. Lines 305–307, 347–397. |
| Invoice-date range | Both endpoints required when timetable is blank; date difference must be ≤90 days. | Inclusive invoice-date selection; consignment uses effective date. No explicit reversed-range rejection. Lines 354–358, 585–587, 633–635. |
| BOL range | Blank upper endpoint becomes open-ended. | Invoice-header filter only; not applied to consignment selection. Lines 538, 613–614. |
| Include memos | **Yes** | Excludes line/ship type M when off. This is separate from RMA selection. Lines 56, 593, 639. |
| Order type | **2: non-RMA**; must exist in domain general-code configuration | 1 = RMA-prefix channels; 2 = other channels; 3 = both. ADJ excluded. Consignment filtering depends on finding the current order. Lines 87–88, 482–492, 618–624, 2773–2786. |
| Currency mode | **Base** | Base-price conversion, or selection of one transaction currency. No explicit currency validation here. Lines 55, 594, 617, 802–807. |
| “Sort By” | No explicit initial value; requires configured code | Selects both ordering **and output layout/content**, not merely sort order. Lines 310–313, 417–423. |
| Worksheet / part descriptions | Both **No** | Worksheet changes file format; descriptions are optional in standard exports. Lines 57, 85, 556–566, 764–785. |
| Email / distribution list | User-record email, otherwise user-derived fallback; nonblank required | Direct email, configured recipient list or configured SFTP route. Lines 116–122, 315–316, 425–478, 2619–2727. |

## Result columns and source meaning

Columns vary by layout; this table covers the principal report/export fields.

| Business field | Concise mapping and qualification |
|---|---|
| Site | Invoice `idh_site`; consignment `tr_site`. |
| Customer and name | Invoice `ih_cust`; consignment `tr_addr`; displayed name from current `cm_sort`. |
| Ship-to | Invoice `ih_ship`; consignment current `so_ship`. Standard CSV exports the **display name**, resolved through current `ad_sort`, with customer-name fallback—not a separate ship-to identifier. |
| Part / customer part / descriptions | Invoice `idh_part`, `idh_custpart`; consignment `tr_part`, current `sod_custpart`. Descriptions from current `pt_desc1/2`. |
| Sales order / line / customer PO | Invoice `ih_nbr`, `idh_line`, `ih_po`; consignment `tr_nbr`, `tr_line`, current `so_po`. |
| Ship date | Invoice `ih_ship_date`; consignment `tr_effdate`. Selection is not necessarily on this displayed date. |
| Other dates | Order date from `ih_ord_date` or current `so_ord_date`; due date from `idh_due_date`/`sod_due_date`; “Original Commit” from `idh_per_date`/`sod_per_date`. Invoice date selects rows but is absent from the standard export. |
| Quantity / UOM | Invoice quantity `idh_qty_inv`; consignment linked receipt quantity `tr_qty_loc`. Labels vary between shipped and invoiced. No general quantity UOM column or UOM conversion appears. Layout 12 labels this quantity **“Qty Ordered”**, although the populated field is invoice/consignment quantity. |
| Unit / extended price | Invoice `idh_price` or consignment `tr_price`; exchange rate from corresponding history. Extended price appears in formatted layouts 4–6. |
| Type | Invoice `idh_type` or consignment `tr_ship_type`; distinct from the order-channel filter. |
| Shipment references | Ship-via and BOL from invoice history or current order; consignment ship-via may be overridden by an order custom field. Invoice reference can be a semicolon-separated list. |
| Boxes / pallets / release | Invoice custom fields `ih__qad03`, `ih__dec01`, `idh__chr09`; consignment release from current order line. |
| Serial / package / serial ship date | `wh_structure.whs_sn`, `whs_type`, `wh_tracking.wht_sn`, `wht_date_shipped`. The same tracking ID is labelled pallet, box or master carton depending on layout. |
| Optional detail | KIT/customer serial; invoice remarks; layout 15 revision, manufacturing date and work-order ID. “Mfg Date” is sourced from **work-order release date** (`wo_rel_date`). |

**Evidence:** population routines `createTrRecord`, lines 2795–2823; `createInvRecord`, lines 2941–2969 and 2996–3052; standard export lines 764–785 and 834–868; special dates/quantity lines 2453–2466.

## Material selection, calculation and correction rules

### Dates and historical coverage

- Invoice selection uses **invoice date**, while consignment selection uses **transaction effective date**. Boundaries are inclusive.
- **D is not reliably “yesterday.”** With the configured batch cutoff, it selects yesterday before/at the cutoff and today afterward. If the control record is absent, this branch supplies no dates.
- **W is not a closed previous calendar week.** It starts 7 plus the current Sunday-based weekday number before today and ends today.
- Explicit ranges allow a **90-day endpoint difference**, potentially 91 included calendar dates. Reversed endpoints are not explicitly rejected.
- No earliest historical date or retention/archive coverage is established. Serial-tracking dates are displayed but not used as date filters.
- **Legacy-only:** CH and VT use today for D, regardless of the normal cutoff result. The screen otherwise allows site ranges; it does not enforce KST’s supported-site list. This does not authorize CH, VT or KS/KTS adoption. Stage 19 remains scoped to SW, NW, AR, MN, MS and KV.

**Evidence:** lines 354–397, 578–640, 2830–2838, 2974–2986.

### Selection, joins and row grain

- Invoice header/detail join: domain plus invoice number; selected customer is the sold-to customer.
- Consignment branch: only **CN-SHIP**, with current part/customer joins; customer types INTR and SUBC are excluded in this branch.
- Invoice rows associated with consignment usage are excluded through `cncu_mstr`; an order found in `cncix_mstr` is also skipped. The comment identifies the latter as undone consignment usage.
- Without matching serial/package structure, invoice quantities are accumulated at the order/line break within each selected header traversal, and invoice references are collected.
- With serial/package structure, one invoice detail can generate **multiple rows carrying the same invoice quantity and price**. Tracking joins use order/line and, when available, a shipment ID taken from `idh__chr08`; zero permits broader matching. Ship-to matching was removed.
- Temporary-row reuse exists, but there is **no general uniqueness constraint or complete deduplication rule**. Consignment serial expansion can create additional rows containing only a subset of the shipment fields.

**Evidence:** lines 578–655; temporary-table indexes 215–217; `createTrRecord`, lines 2828–2893; `createInvRecord`, lines 2918–3057.

### Quantities and totals

Consignment quantity comes from the first linked **RCT-TR** receipt identified through transaction remarks and order number; no match leaves zero. There is no general unit normalization.

Formatted reports total the generated rows: usually by customer, by ship-to in layout 2, and across the report. Layouts 4–6 also total extended price. Consequently, serial-expanded rows can repeat quantities and inflate totals; the source does not separate shipment-level measures from serial detail.

**Evidence:** `get_consign_qty`, lines 2736–2750; totals 921–991, 1128–1196, 1519–1596; serial quantity assignment 3019.

### Pricing and historical value

The screen uses **recorded invoice/transaction prices**, not current price-list lookup. Base mode normally divides unit price by the recorded exchange rate; extended price is quantity × converted unit price.

These are historical-price inputs, but the calculation does **not establish accounting revenue**: no explicit tax, discount, freight or later-adjustment reconciliation is visible. Currency is not a standard CSV column. Layout 2 overwrites its converted price with the original price; layout 13 exports original price directly.

A customer option suppresses price in standard worksheets if any selected customer requests it. It does not consistently suppress formatted-report prices or layout 13 prices.

**Evidence:** lines 547–553, 802–807, 1010–1016, 1410–1414, 1579–1596, 2542–2543.

### Returns, reversals and later corrections

RMA-prefix **order channels** are explicitly selectable; ADJ channels are excluded. Memo-type selection is separate. Negative quantities retain their sign but are **not explicitly classified as returns**. Beyond the consignment-usage exclusions, no general cancellation/reversal pairing, correction netting or “as originally shipped” reconstruction is visible.

Some context comes from current customer, address, part and order records, so the report is not wholly an immutable historical snapshot.

**Evidence:** lines 618–645, 2773–2786, 823–827, 2766–2823.

## Ordering, detail and output options

- **General ordering:** layouts 1/14 customer → ship date → part → serial; 2 ship-to → date → part → serial; 3 customer → custom account → date → serial; 4 customer → part → order date → serial; 5 customer/date with descending order; 6 customer with descending PO, then date/serial/order/line. Layouts 7/9/15 add order/line grouping with descending line. Layout 8 orders PO/customer part/serial; layout 12 uses descending ship date.
  **Evidence:** lines 795–798, 1004–1007, 1209–1212, 1401–1404, 1606–1609, 1807–1811, 2018–2021, 2220–2224, 2440–2441.
- **Details:** packaging serials by default; layout 14 permits other structure types. Layout 9 adds KIT/customer serial, 13 emits parent/sub-item KIT rows, and 15 adds manufacturing/work-order fields. No click-through detail view appears.
- **Outputs:** 132-column paginated text report; standard CSV worksheet; specialized layouts 8 and 10–12; tab-delimited `.txt` layout 13 for longer serials; extended standard layouts 9/14/15. Layouts 10–13 only write detail when worksheet mode is selected.
- **Compatibility:** DOS line-ending conversion, short formatted invoice references, customer-specific column contracts, ISO dates in layout 10, and manually assembled delimiters in several specialized formats. Generic CSV does not include extended-price totals.
- A standard printer-selection include is called, but the visible output is redirected to an attachment file. Exact screen/printer delivery cannot be established without that include.

**Evidence:** lines 540, 556–566, 660–790, 2209–2600, 2606–2727.

## Planning candidates

**Workspace lookup**
- Customer/site/part/date-scoped shipment references.
- Order/line, customer PO, invoice and BOL identification.
- Optional serial/package detail with its own shipment date.
- Clear distinction between invoiced and uninvoiced consignment records.

**Broader Reports feature**
- Customer groups/ranges/types, product-line and salesperson filters.
- Explicit date basis and currency choices.
- RMA and memo inclusion controls.
- Customer/ship-to grouping, totals and selected export layouts.

These candidates require owner decisions; legacy behavior is not automatically a requirement.

## Material open questions and smallest additional evidence

| Unanswered question | Why it changes design / smallest evidence |
|---|---|
| What customer population and precedence should workspace/report selection use? | Determines inclusion and access scope. Missing `ktso1184.i`, `ktso1194.i` and `ktso1200.i` prevent exact tracing. Supply those includes or an owner-approved selection definition. |
| Does “historical shipments” mean physical shipments, invoice activity, or both with consignment? Which date and historical span should govern? | Changes the underlying population. Owner decision plus a bounded retention/archive statement; current live coverage cannot be established statically. |
| What is the primary row identity, and how should serial/package detail affect totals? | Prevents repeated shipment quantities and cross-shipment serial matches. Owner-approved grain plus sanitized examples of split invoices/multiple packages and shipment-ID meaning. |
| Which return, reversal and correction events belong, and how should they affect totals? What UOM applies? | Channel filtering and signed quantities are insufficient to establish net shipments. Approved transaction/channel/UOM definitions and sanitized correction examples. |
| Are monetary values required, and are they shipment value or accounting revenue? | Determines price, currency and correction rules. Owner decision and authoritative financial-field definitions. |
| Which legacy outputs, price restrictions and delivery methods must survive? | Changes export contracts and confidentiality controls. Owner list of required layouts plus sanitized sample outputs; customer-option includes if their exact rules matter. `ktinvformat.p` and printer/SFTP includes are needed only if exact legacy formatting/delivery parity is required. |

Dependent analysis stopped at the unavailable includes/routines; independent main-file analysis continued.

### Done / Findings / Open / Evidence

- **Done:** Read-only inspection; traced inputs, selection, population, calculations and outputs. No files changed or execution performed during discovery; this Markdown file is the subsequently requested export of the conversation report.
- **Findings:** Mixed invoice/consignment population, layout-dependent row grain, serial-related total repetition, and inconsistent date/price/export behavior.
- **Open:** Customer-selection dependencies, shipment definition, grain/UOM/correction rules, monetary meaning and required compatibility.
- **Evidence:** `docs/reference/ktmg1160.p`, lines 1–3071, especially `get_consign_qty`, `createTrRecord`, `createInvRecord`; **static/offline source inspection only**.

# KST v2 — Stage 5A MPS Data Inventory

**Status:** Draft for owner review  
**Stage:** 5A — Data Inventory and Data Strategy  
**Capability:** Master Production Schedule (initial dashboard grid)  
**Related accepted contract:** `KST_v2_MPS_Source_Stored_Procedure_Contract_Accepted.docx`  
**Primary schema reference:** `qadpro2-data-map.md` / `qadpro2-data-map.json` / `qadpro2-data-map.yaml`

---

## 1. Purpose

This document defines the minimum source-data inventory and transformation responsibilities required to implement the initial KST v2 Master Production Schedule grid.

It deliberately separates:

- fields required for the initial MPS grid,
- fields required only as joins, filters, or status inputs,
- fields retained for later drill-down phases,
- source-system facts from derived application concepts.

The goal is to provide an implementation-grade lineage map without expanding Stage 5B into later work-order, part-detail, shortage, or planning features.

---

## 2. Source authority model

### 2.1 Informational and filter-oriented sources

`pt_mstr` and `ptp_det` are primarily informational and filter-oriented sources.

Important uses include:

- part description,
- product-line filtering,
- parent product-line filtering for components,
- manufactured-versus-purchased filtering,
- part status and other informational attributes.

They are not the authoritative source for determining whether a part currently has loaded demand or is operationally being built/used at a site.

### 2.2 Operational usage / demand authority

`mrp_det` and `wod_det` are authoritative for where parts are being used and/or built.

Working business rule:

> If a part is absent from both `mrp_det` and `wod_det`, then no loaded system demand/use exists for that part.

For the initial parent-level MPS, `mrp_det` is the primary schedule source.

### 2.3 Work-order authority

`wo_mstr` is authoritative for work-order header identity and status used by the MPS.

The MPS work-order relationship must be constrained by domain, site, parent part, and work-order ID.

---

## 3. Workspace part scope inputs relevant to MPS

Workspace configuration does not use customer code as an authoritative scheduling scope.

A workspace represents a scheduler-managed set of parent-level parts at a site, derived from:

- site,
- product line or product-line range,
- optional explicit parent-part list.

### 3.1 Site-to-domain mapping

| Site | QAD domain |
|---|---|
| NW | KTC |
| SW | KTC |
| AR | KTC |
| MN | KTC |
| MS | KTC |
| KV | KTV |

Domain is inferred by the QAD integration layer from the workspace site. Domain is not a user-entered workspace field.

### 3.2 Product-line scope discovery

For product-line-derived workspace scope, qualifying parent parts are discovered using `pt_mstr` joined to `mrp_det` with the established rules:

- matching domain and part,
- configured site,
- `pt_prod_line` within configured range,
- `pt_pm_code NOT IN ('p', 'f')`,
- `mrp_dataset <> 'pod_det'`,
- `mrp_type IN ('supply', 'supplyf', 'supplyp')`.

The result is the distinct set of parent-level parts with relevant loaded planning activity.

### 3.3 Explicit parent parts

Explicitly configured parent parts are a scheduler-declared responsibility filter and must not be rejected solely because they currently lack an `mrp_det` row.

Known item-master exclusion rule:

- reject `pt_status IN ('E', 'O')`.

Operational activity, when present, is still determined from `mrp_det` / `wod_det`.

---

## 4. Initial MPS business-field inventory

The table below describes the fields the initial MPS grid needs as business concepts. Some are displayed directly; others are derived before they reach the frontend.

| Business field | Role | Source / derivation | Grain | Authority | Initial MPS? | Notes |
|---|---|---|---|---|---:|---|
| Parent Part | Identity / Display | `mrp_det.mrp_part` | Parent schedule row / source fact | Authoritative operational source | Yes | Primary MPS row identity within site context. |
| Description | Display | `pt_mstr.pt_desc1` | Part | Authoritative informational source | Yes | Use `pt_desc1` only. Do not concatenate `pt_desc2`. |
| Site | Identity / Filter / Metadata | `mrp_det.mrp_site` | Source fact | Authoritative operational source | Yes, backend | Workspace-scoped; may not need repeated display in every row. |
| Domain | Join / Filter / Diagnostic metadata | `mrp_det.mrp_domain` | Source fact | Authoritative source key | Yes, integration | Inferred from site before query; preserve where useful for diagnostics. |
| Due Date | Calculation input | `mrp_det.mrp_due_date` | MRP supply fact | Authoritative operational source | Yes | Input to due-date bucket and Falldown classification. |
| Release Date | Calculation input | `mrp_det.mrp_rel_date` | MRP supply fact | Authoritative operational source | Yes | Candidate date basis for Release Date view. |
| Quantity | Calculation input / Display after aggregation | `mrp_det.mrp_qty` | MRP supply fact | Authoritative operational source | Yes | Summed into parent/week bucket. |
| MRP Type | Filter / Classification input | `mrp_det.mrp_type` | MRP supply fact | Authoritative operational source | Yes, backend | Initial accepted values: `SUPPLY`, `SUPPLYF`, `SUPPLYP`. |
| Source Dataset | Filter / Diagnostic metadata | `mrp_det.mrp_dataset` | MRP supply fact | Authoritative source metadata | Yes, backend | Used to exclude `pod_det`; useful for diagnostics. |
| Work Order ID | Join key / drill-down reference | `mrp_det.mrp_line` -> `wo_mstr.wo_lot` | MRP supply fact / WO | Authoritative relationship input | Yes, backend | Preserve to support later drill-down without changing MPS source semantics. |
| Work Order Status | Status input | `wo_mstr.wo_status` | Work order | Authoritative WO status | Yes, backend | Drives execution status and planned/scheduled flags. |
| Week Start | Derived display / identity | C# calendar service | Parent + weekly bucket | Derived | Yes | Monday is the displayed bucket anchor. |
| Is Falldown | Derived status | C# from selected date basis and current business week | Parent + bucket | Derived | Yes | Unfinished work due before current week. |
| Execution Status | Derived status | C# from WO statuses A/F/R | Parent + bucket | Derived business rule | Yes | None / Allocating / Frozen / Released / Mixed. |
| Contains Planned Work | Derived status flag | C# from presence of `wo_status = 'P'` | Parent + bucket | Derived business rule | Yes | Frontend uses accessible planned-work font treatment. |
| Contains Explicitly Scheduled Work | Derived status flag | C# from presence of `wo_status = 'e'` | Parent + bucket | Derived business rule | Yes | Frontend uses non-color marker such as top-edge line. |
| Bucket Quantity | Derived display | C# sum of included source quantities | Parent + bucket | Derived | Yes | Sum remains independent of status styling. |
| Snapshot Timestamp | Diagnostic / lifecycle metadata | Application snapshot service | Snapshot | Application-owned | Yes | Used for current/stale display. |
| Snapshot ID | Diagnostic / lifecycle metadata | Application snapshot service | Snapshot | Application-owned | Yes | Supports traceability and refresh lifecycle. |

---

## 5. MPS source-field inventory

These are the physical source fields needed to construct the initial MPS contract.

### 5.1 `mrp_det` — Material Requirements Detail

| Field | Business use | Field role | Initial SQL result? | Notes |
|---|---|---|---:|---|
| `mrp_domain` | Domain | Join / Filter | Yes | Join/filter with all applicable domain-bearing tables. |
| `mrp_site` | Production site | Join / Filter | Yes | Workspace site authority for MPS fact. |
| `mrp_part` | Parent part | Identity / Join / Display | Yes | Must match `wo_part` for safe WO association. |
| `mrp_line` | Work-order ID | Join key | Yes | Join to `wo_lot`. |
| `mrp_nbr` | Work-order number | Deferred metadata | No for initial MPS | Available for future drill-down if needed. |
| `mrp_due_date` | Due date | Calculation input | Yes | Due-date mode / Falldown input. |
| `mrp_rel_date` | Release date | Calculation input | Yes | Release-date mode candidate. |
| `mrp_qty` | Schedule quantity | Calculation input | Yes | Aggregated in C#. |
| `mrp_type` | Supply classification | Filter / status input | Yes | Include `SUPPLY`, `SUPPLYF`, `SUPPLYP`. |
| `mrp_dataset` | Source dataset | Filter / diagnostic | Yes | Exclude `pod_det`. |
| `mrp_detail` | Specific planning event | Deferred diagnostic / future drill-down | No for initial MPS | Known useful later; intentionally excluded from initial API surface. |
| `mrp_ord_site` | MRP site candidate | None | No | Not validated in curated map; do not use. |

### 5.2 `wo_mstr` — Work Order Master

| Field | Business use | Field role | Initial SQL result? | Notes |
|---|---|---|---:|---|
| `wo_domain` | Domain | Join / Filter | No if redundant after join | Required in join predicate. |
| `wo_site` | Site | Join / Filter | No if redundant after join | Required in join predicate. |
| `wo_part` | Parent item | Join verification | No if redundant after join | Prevents component/incorrect WO association. |
| `wo_lot` | Work-order ID | Join key | Yes or alias as WorkOrderId | Matches `mrp_line`. |
| `wo_status` | Work-order status | Status input / Filter | Yes | Exclude C; preserve A/F/R/P/e. |
| `wo_nbr` | Work-order number | Deferred display/drill-down | No | Future work-order drill-down. |
| `wo_due_date` | Work-order due date | Deferred validation/drill-down | No | MPS uses MRP date source initially. |
| `wo_rel_date` | Work-order release date | Deferred validation/drill-down | No | MPS uses MRP date source initially. |
| `wo_qty_ord` | Ordered quantity | Deferred drill-down | No | Future work-order cards. |
| `wo_qty_comp` | Completed quantity | Deferred drill-down | No | Future work-order cards. |
| `wo_line` | Production line | Deferred drill-down | No | Future work-order cards. |

### 5.3 `pt_mstr` — Part Master

| Field | Business use | Field role | Initial SQL result? | Notes |
|---|---|---|---:|---|
| `pt_domain` | Domain | Join / Filter | No if redundant after join | Required for safe join. |
| `pt_part` | Part number | Join key | No if redundant after join | Matches MPS part. |
| `pt_desc1` | Part description | Display | Yes | Initial MPS description source. |
| `pt_prod_line` | Product line | Workspace-scope filter | No initial MPS display | Determines product-line-derived workspace scope. |
| `pt_group` | Parent product-line association for components | Future filter input | No | Important for later component workflows when component PL differs. |
| `pt_pm_code` | Manufactured/purchased classification | Filter | No initial display | Frequently used source filter; scope query excludes `p` and `f`. |
| `pt_status` | Part status | Validation / informational | No initial display | Explicit workspace part validation excludes E and O. |

### 5.4 `wod_det` — Work Order Detail

`wod_det` is not required to render the initial MPS schedule grid, but it is an authoritative source for actual component usage by work order and therefore belongs in the Stage 5A source catalog for future drill-down phases.

Key future fields include:

- `wod_domain`
- `wod_site`
- `wod_lot`
- `wod_nbr`
- `wod_part`
- `wod_prod_line`
- `wod_qty_req`
- `wod_qty_all`
- `wod_qty_iss`
- `wod_qty_pick`
- `wod_status`

These are intentionally deferred from the initial MPS API and snapshot unless a later implementation dependency proves otherwise.

---

## 6. Safe join rules

### 6.1 MRP to Work Order

The accepted work-order association rule is:

```text
mrp_det.mrp_domain = wo_mstr.wo_domain
mrp_det.mrp_site   = wo_mstr.wo_site
mrp_det.mrp_part   = wo_mstr.wo_part
mrp_det.mrp_line   = wo_mstr.wo_lot
```

All joins and filters should include `_domain` and `_site` when those fields exist, following IT guidance.

### 6.2 MRP to Part Master

```text
mrp_det.mrp_domain = pt_mstr.pt_domain
mrp_det.mrp_part   = pt_mstr.pt_part
```

`pt_mstr` supplies description and filtering metadata; it does not establish operational use at a site.

---

## 7. Initial SQL filtering rules

The KST-specific MPS source query/procedure should apply the following source-level filters:

- required domain,
- required site,
- workspace-resolved parent scope,
- `mrp_dataset <> 'pod_det'`,
- `mrp_type IN ('supply', 'supplyf', 'supplyp')`,
- `wo_status <> 'C'` after safe WO association,
- appropriate item-master scope qualification when product-line scope is used.

The procedure should return row-oriented facts. It must not dynamically pivot weeks into columns.

---

## 8. Status semantics

### 8.1 Raw WO statuses relevant to MPS

| Raw status | Meaning | Quantity included? | MPS semantic effect |
|---|---|---:|---|
| A | Allocating | Yes | Execution status = Allocating if sole A/F/R state. |
| F | Frozen | Yes | Execution status = Frozen if sole A/F/R state. |
| R | Released | Yes | Execution status = Released if sole A/F/R state. |
| C | Closed | No | Exclude from MPS aggregation. |
| P | Planned | Yes | `ContainsPlannedWork = true`. |
| e | Explicitly scheduled by Master Scheduler; exact QAD code label unresolved | Yes | `ContainsExplicitlyScheduledWork = true`. |

### 8.2 Mixed execution status

For each parent/week bucket:

- one distinct A/F/R state -> that execution state,
- two or more distinct A/F/R states -> `Mixed`,
- no A/F/R states -> `None`.

P and e do not themselves create a Mixed execution state.

Examples:

| Raw statuses in bucket | Execution status | Planned flag | Scheduled flag |
|---|---|---:|---:|
| R | Released | No | No |
| P | None | Yes | No |
| e | None | No | Yes |
| P + e | None | Yes | Yes |
| R + P | Released | Yes | No |
| F + e | Frozen | No | Yes |
| A + F | Mixed | No | No |
| A + F + P + e | Mixed | Yes | Yes |

### 8.3 Frontend presentation ownership

The backend returns semantics, not colors.

Frontend rules:

- `ExecutionStatus` controls box fill/presentation.
- `ContainsPlannedWork` controls a distinct accessible foreground/font treatment; KST v1 light blue is visual guidance, not a hard-coded contract color.
- `ContainsExplicitlyScheduledWork` controls a non-color marker such as a strong top-edge line.
- Mixed A/F/R execution state uses its own distinct presentation; light purple is the current design candidate.

Color-independent status signaling remains required for accessibility.

---

## 9. Calendar and Falldown rules

### 9.1 Business week

- Business weeks are Sunday through Saturday.
- Monday is used as the visible anchor/label for the weekly MPS bucket.

### 9.2 Falldown

Falldown represents unfinished work orders whose applicable MPS date falls before the current business week.

C# owns the calendar calculation rather than reproducing the legacy dynamic-pivot date expression.

### 9.3 Date-basis modes

Candidate authoritative inputs:

- Due Date mode -> `mrp_det.mrp_due_date`
- Release Date mode -> `mrp_det.mrp_rel_date`

These sources are confirmed available. Final UI behavior for switching between the two modes should be validated during Stage 5B against representative data.

---

## 10. Data grain map

| Dataset / model | Grain |
|---|---|
| Workspace configuration | One local assignment/configuration record per workspace |
| Resolved workspace part scope | One row per site + resolved parent part for a refresh |
| MPS source row | One row per retained MRP supply fact / safely associated WO fact at the smallest grain that preserves quantity, date, MRP type, WO ID, and WO status |
| MPS bucket | One row per site + parent part + date basis + weekly bucket, plus Falldown as a special bucket |
| Work-order reference retained with bucket | One reference per distinct work order contributing to the bucket |
| MPS snapshot | One snapshot per workspace refresh |

SQL may perform only aggregation/deduplication that is proven not to destroy work-order identity or status information required for bucket classification.

---

## 11. SQL-versus-C# responsibility map

| Responsibility | Owner |
|---|---|
| Infer QAD domain from site | QAD integration / application boundary |
| Apply domain/site filtering | SQL |
| Apply resolved part scope | SQL |
| Apply product-line/item-master source filters where required | SQL |
| Exclude `pod_det` | SQL |
| Select `SUPPLY`, `SUPPLYF`, `SUPPLYP` | SQL |
| Safely associate MRP facts to `wo_mstr` | SQL |
| Exclude closed WO (`C`) | SQL |
| Return raw due/release dates | SQL |
| Return raw quantity | SQL |
| Return raw WO status | SQL |
| Return part description (`pt_desc1`) | SQL or QAD adapter result mapping |
| Determine Monday weekly anchor | C# |
| Determine current-week boundary / Falldown | C# |
| Aggregate source facts into parent/week bucket | C# |
| Determine A/F/R/Mixed execution status | C# |
| Determine planned flag from P | C# |
| Determine scheduled flag from e | C# |
| Construct snapshot | C# application layer |
| Choose box colors / font colors / markers | Frontend |
| Work-order drill-down data | Deferred later phase |

---

## 12. Initial snapshot classification

### Initial snapshot — include

- workspace identity,
- resolved parent-part scope,
- snapshot ID,
- snapshot creation time,
- source/refresh metadata,
- parent part,
- part description,
- MPS supply facts required to build buckets,
- minimum WO identity/status needed for MPS semantics,
- normalized MPS buckets,
- retained WO references sufficient to explain bucket status later.

### Lazy / future drill-down — defer

- full part-master detail,
- full work-order cards/detail,
- production line,
- ordered/completed/open WO quantities,
- BOM/components,
- allocations/kitting percentages,
- inventory detail,
- shortages,
- purchase orders,
- buyer/planner detail,
- work-order remarks,
- `mrp_detail` presentation,
- component usage from `wod_det`.

The initial snapshot must not become a preloaded copy of all later drill-down datasets.

---

## 13. Deferred known-useful fields

The curated QAD data map intentionally contains fields that may be useful in later stages. Their existence is documented but does not make them part of the initial MPS contract.

Examples:

### Part detail / filtering

- `pt_rev`
- `pt_buyer`
- `pt_cum_lead`
- `pt_mfg_lead`
- `pt_pur_lead`
- `pt_sfty_stk`
- `pt_um`
- `pt_group`

### Site planning detail

- `ptp_buyer`
- `ptp_cum_lead`
- `ptp_mfg_lead`
- `ptp_pur_lead`
- `ptp_sfty_stk`
- `ptp_timefnce`

### Work-order drill-down

- `wo_nbr`
- `wo_line`
- `wo_qty_ord`
- `wo_qty_comp`
- `wo_due_date`
- `wo_rel_date`
- `wo_rmks`

### Component / kitting drill-down

- `wod_part`
- `wod_qty_req`
- `wod_qty_all`
- `wod_qty_iss`
- `wod_qty_pick`
- `wod_prod_line`

These should be activated only when the corresponding UI phase reaches field-inventory review.

---

## 14. Data quality and reliability register — MPS scope

| Item | Status | Treatment |
|---|---|---|
| Customer code -> product line relationship | Unreliable | Removed from workspace scope. |
| IOS code as workspace responsibility | Unreliable for scope | Do not use for workspace membership. |
| Site -> domain mapping | Confirmed business rule | Centralize in QAD integration boundary. |
| `mrp_det` / `wod_det` operational use evidence | Confirmed | Treat as authoritative for loaded demand/use. |
| `pt_mstr` / `ptp_det` operational site-use evidence | Not authoritative | Use for metadata/filtering, not loaded demand truth. |
| `mrp_ord_site` | Not validated | Do not use. |
| `pod_det.pod_domain` | Not validated in curated map | Do not assume; follow source-specific join evidence when PO work begins. |
| `wo_status = e` exact QAD label | Unknown | Preserve raw code; business meaning for MPS is explicitly scheduler-scheduled. |
| Part status D | Unknown | Do not invent meaning. |
| Fiscal calendar source | Open | Must be resolved before fiscal period/quarter bands are implementation-ready. |

---

## 15. Representative validation cases

Stage 5B must validate at least the following against real QAD results:

1. Product-line workspace returns expected parent scope.
2. Product-line range returns expected combined parent scope.
3. Explicit-part workspace includes valid configured parts even when current MRP activity is absent.
4. Site/domain inference produces correct domain for KTC and KTV sites.
5. Same WO ID cannot incorrectly attach to a component because domain/site/part/lot join prevents it.
6. Single A bucket -> Allocating.
7. Single F bucket -> Frozen.
8. Single R bucket -> Released.
9. A + F or other multiple A/F/R states -> Mixed.
10. P-only bucket -> no execution fill, planned flag set.
11. e-only bucket -> no execution fill, scheduled flag set.
12. P + e -> both planned and scheduled indicators preserved.
13. R + P -> Released plus planned indicator.
14. F + e -> Frozen plus scheduled indicator.
15. C WO contributes no quantity.
16. Multiple work orders in same parent/week sum correctly.
17. Falldown includes unfinished work before current business week.
18. Current-week work does not fall into Falldown.
19. Sunday/Saturday boundary and Monday anchor behave as intended.
20. Due-date and release-date modes map to `mrp_due_date` and `mrp_rel_date` respectively.
21. Part description displays `pt_desc1` only.
22. A 72-week horizon remains row-oriented through API contracts; no dynamic week properties are introduced.

---

## 16. Open items before Stage 5A MPS data-contract finalization

### 16.1 Fiscal calendar source

The current curated QAD map does not identify an authoritative fiscal period / quarter / fiscal year calendar source.

This must be resolved before the fiscal period and quarter header bands in the MPS prototype are considered implementation-ready.

### 16.2 Exact MPS source stored-procedure name and deployment ownership

The accepted contract defines behavior, not the final database object name. Final naming and deployment ownership should be agreed with IT / the data analyst before Stage 5B integration work.

### 16.3 Source-row duplicate behavior

Representative execution must confirm whether the raw join can produce duplicate MRP/WO rows and whether any SQL-side deduplication is both safe and necessary. No deduplication rule should be invented before observing real results.

---

## 17. Stage 5A disposition

### Accepted / substantially settled

- workspace scope is site + product-line/range and/or explicit parent parts,
- customer code is not a workspace-scope input,
- domain inference rule,
- parent-scope discovery logic,
- initial MPS source tables,
- safe MRP-to-WO join keys,
- part description source,
- MPS quantity/date/source/status inputs,
- closed-WO exclusion,
- A/F/R/Mixed execution semantics,
- planned and explicitly scheduled flags,
- Falldown business meaning,
- Sunday-Saturday business week with Monday display anchor,
- row-oriented MPS source strategy,
- initial snapshot versus deferred drill-down boundary,
- SQL-versus-C# ownership for the initial MPS.

### Still open

- authoritative fiscal calendar source,
- final stored-procedure object name/deployment ownership,
- observed duplicate behavior under representative production data.

Once these items are resolved or explicitly deferred with owner acceptance, the MPS Data Inventory can be marked **Accepted** and used to derive the backend `MpsSourceRow` / `MpsBucket` contract for Stage 5B planning.

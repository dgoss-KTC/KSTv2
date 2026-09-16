# KST v2 -- Stage 11-A Long-Term Shortages Source Discovery Results

**Status:** Stopped -- evidence-only source discovery; remaining source evidence incomplete; no implementation authorization

## 1. Authority Reconciliation

| Required authority | Result | Reconciliation |
|---|---|---|
| `AGENTS.md` | Read | Governs this documentation-only pass, read-only database boundary, source traceability, and Stage 9/10 lock. |
| `docs/status/CURRENT_PROJECT_STATUS.md` | Read | Stage 9 and Stage 10 are COMPLETE / ACCEPTED / LOCKED. Stage 11 is not implemented. |
| `KST-v2-Master-Project-Checklist.md` | Read | The historical Stage 11 checklist remains unstarted and is superseded for this discovery scope by the accepted 11-A contract in the discovery prompt. |
| `KST_v2_STAGE_9_CLOSEOUT.md` | Read | Confirms accepted usable inventory, hard allocation, and KSS boundaries. |
| `KST_v2_STAGE_9_SOURCE_MAPPING.md` | Read | Confirms hard allocation grain and hybrid allocation evidence. |
| `KST_v2_STAGE_10_COMPONENT_ORDERS_CLOSEOUT.md` | Read | Confirms conventional line qualification and informational-only prior scope. |
| `KST_v2_STAGE_10_SOURCE_MAPPING.md` | Read | Confirms line fields, confirmation, due-date, buyer, vendor, and KSS mappings. |
| `KST_v2_STAGE_11_DISCUSSION_FRAMEWORK.md` | Read | Contains stale claims that the prior-stage authority files were unavailable. Those claims are contradicted by this checkout and are not carried forward. Its Stage 11 rule statements reconcile with the current accepted contract. |
| `KST_v2_STAGE_11_FIELD_DISCOVERY_LEDGER.md` | Read | Revised by this pass. |
| `docs/data/qadpro2-data-map.md` | Read | Supplies schema candidates only, not transactional evidence. |

No required authority document is missing. The legacy sources were read only as supporting evidence: `C:\dev\kst\features\shortage_report.py` and `docs/reference/sp_shortageFinder.txt` (the latter is the repository’s actual filename).

## 2. Pre-Read Source Findings

| Fact class | Source, grain, and safe join | Accepted rule supported or challenged | Status |
|---|---|---|---|
| Opening QOH (Stage 11-A specific) | Direct `ld_det` aggregate at domain/site/part grain. | `OpeningQoh = COALESCE(SUM(ld_qty_oh), 0)` for lots whose non-null `ld_status` is not MRB/INSPECT/NCMINSP and whose non-null `ld_lot` starts with neither RMA nor RA. No positive-quantity filter, date input, `loc_mstr`, `is_mstr`, `lad_det`, or `in_mstr` joins. | Settled owner-defined Stage 11-A source rule, confirmed 2026-09-15. `NOT IN` / `NOT LIKE` exclude null status or lot values under SQL three-valued logic. Transit is included. | Confirmed |
| Hard allocation | `lad_det` grain is domain/site/WOID/operation/component/location/lot; `lad_nbr = wod_lot`, `lad_line = wod_op`, `lad_part = wod_part`; `lad_qty_all` is firm allocation. | Supports the accepted remaining-demand deduction and prohibits use of `wod_qty_all` as extra coverage. | Confirmed by locked Stage 9 evidence |
| Workspace population | Current MPS snapshot retains resolved parent scope; Stage 10 expands each resolved parent through current-effective `ps_mstr`, then deduplicates component parts only for the component report population. | Supports report-component selection. Parent attribution must retain occurrences/parents separately from component deduplication. | Confirmed implementation pattern |
| Conventional PO | `pod_det` line grain, joined to `po_mstr` on domain + PO number; positive `pod_qty_ord - pod_qty_rcvd`, status not C/X case-insensitively, line confirmation `pod__log01`, line due date `pod_due_date`. | Supports 11-A conventional supply rule, including deterministic `DueDate, PoNumber, PoLine` ordering. | Confirmed by locked Stage 10 evidence |
| KSS | Effective `pod_det` relationship by domain/site/part and effective boundary, joined to `po_mstr` with `po_sched = 1` and effective PO boundary. | Supports classification-only rule; scheduled supply remains excluded. | Confirmed by locked Stage 9/10 evidence |
| Planner and lead time | `ptp_det` site-first, `pt_mstr` fallback; buyer display resolves through domain-scoped `code_mstr` and field discriminator. | Supports list display candidates, not projection arithmetic. | Confirmed by accepted Stage 10 runtime reconciliation |
| Safety stock / status / B/P / manufacturer item | Candidate sources are `ptp_det.ptp_sfty_stk`, `pt_mstr.pt_status`, site/master buyer mapping, and `pod_det.pod_vpart`. | Requires a bounded validation that the safety-stock source and fallback are appropriate to 11-A. | Needs bounded evidence |
| WO component demand | `wo_mstr` header joined to `wod_det` by domain + WOID (`wo_lot = wod_lot`); demand identity is distinct `(WOID, component, wod_op)` rows. | Calculate `max(0, wod_qty_req - wod_qty_iss - own firm allocation)` at that grain, without `wod_qty_pick`, then sum the resulting rows into the component's weekly demand. Preserve `wod_op` in joins to prevent duplication; multiple operations are separate explicit requirements. Exclude closed work with `wo_status = 'C'`; `wo_bom_code = 'RMABOM'` is the complete RMA exclusion. | Owner decisions, 2026-09-16; confirmed |
| Forecast | Candidate `mrp_det` rows with dataset `fcs_sum`, keyed at least by domain/site/part/date plus source row identity not present in current data map. | Gross forecast remains interim; source grain/sign/scoping and overlap still need evidence. | Needs bounded evidence |

## 3. Approved Bounded Read Inventory

All reads below are SELECT-only, parameterized, Windows-integrated QAD reads through a disposable repository-root `scratch/` harness. No credential is read, logged, or written. Each query returns aggregates, metadata, or a small TOP-limited sample; the results artifact records no supplier names, user identities, connection details, lot values, or unredacted production quantities.

| ID | Business purpose and validation case | Bounded site/component scope | Exact fields and expected grain | Result |
|---|---|---|---|---|
| Q11-01 | Reconcile the Stage 9 physical usable inventory minus usable hard allocation implementation mapping with the accepted QAD-returned, already-hard-allocation-netted opening-QOH semantic. | KTC/SW; `ICC-00994`, `ICC-01084`, `ICC-01117`. | `ld_det` lot quantity and identity, inventory status/nettable/expiry classification; `lad_det` hard allocation by lot; aggregate one row/component. | All three had qualifying usable inventory; none had a qualifying usable hard allocation; free quantity did not go negative. The result is insufficient to reconcile source implementation with the accepted netted-QOH semantic. |
| Q11-02 | Validate WO header/detail identity, status/type representation, and remaining-demand inputs. | KTC/SW; TOP 20 rows for required samples. | Header WOID/status/type/bom/due; detail operation/component/required/issued; hard allocation aggregate; one row per WOID/operation/component. | Historical read result was partial. Owner decisions now settle picked exclusion, additive distinct component-operation rows, no cancelled status, and `RMABOM` as the complete RMA exclusion. |
| Q11-03 | Validate workspace-parent attribution without using a component as a duplicate calculation input. | One active workspace’s resolved parents, then one shared component selected from its current-effective BOM. | Parent/component occurrence identity and component parent count; site-wide WO component demand grouped by whether WO parent is within resolved scope. | Not run; stop condition reached. |
| Q11-04 | Validate confirmed conventional PO line facts and lifecycle edge cases. | KTC/SW; required samples. | PO/revision/line/part/due/ordered/received/open/status/confirmed/manufacturer item; one row per PO revision/line. | All three samples had confirmed open blank-status lines both in and after the 24-week horizon. C/X lines and historical positive-open anomalies were also observed; no missing-date case was observed. Partial/full receipt, revision identity, and deterministic ordering remain unvalidated. |
| Q11-05 | Validate active, expired, and missing KSS relationship classification only. | Same components plus TOP 20 candidate parts. | `pod_part`, scheduled flags, both effective boundaries; one row per relationship. | Not completed: SQL Server rejected the query’s `RowCount` alias before result retrieval. No retry under this stopped pass. |
| Q11-06 | Validate `fcs_sum` domain/site/part/date/sign/grain and a forecast versus sales-order/MRP overlap candidate. | Same components and current week through week 24; TOP 20 per source-shape query. | Dataset/type/part/site/domain/due/quantity/order fields; one row per source row. | Not run; stop condition reached. |
| Q11-07 | Validate presentation candidates and safety-stock values used in owner timelines. | Required sample components. | `pt_status`, site/master safety stock, purchasing lead, buyer source/display, PM/BP, UOM; one row/component. | Not run; stop condition reached. |

## 4. Rule Evidence Matrix

| Accepted rule | Evidence state | Evidence / disposition |
|---|---|---|
| Site-wide weekly net and 24 Sunday-start buckets | Confirmed | Locked 11-A contract; existing scheduler calendar supports Sunday-start bucketing. |
| Opening balance is already hard-allocation-netted QOH | Confirmed Stage 11-A owner decision | Opening balance uses the settled Stage 11-A direct `ld_det` source rule. KST performs no hard-allocation subtraction. |
| Firm allocation reduces only owning WO remaining demand | Confirmed | Locked Stage 9 hybrid rule and `lad_det` WOID/operation/component grain. |
| Include A/F/R/E/P WOs | Confirmed | A/F/R are established status values. Redesigned Q11-F02 confirmed header status `E`; existing MPS evidence confirms `P`. |
| Exclude closed/RMA/RMABOM work | Confirmed | Owner decisions confirm no separate cancelled status exists; closed work is `wo_status = 'C'`; and `wo_bom_code = 'RMABOM'` is the complete QAD representation of RMA work for this report. |
| Confirmed conventional in-horizon PO line supply | Confirmed | Locked Stage 10 line qualification, confirmation field, and ordering; 11-A timing boundary still needs sample evidence. |
| KSS classification only; no scheduled supply | Confirmed | Locked Stage 9 KSS mapping and Stage 11 contract. |
| Gross `fcs_sum` future forecast | Interim / needs bounded evidence | Accepted only as an explicitly potentially overstated approximation; Q11-06 must validate safe scope/grain. |
| Presentation safety stock and fields | Needs bounded evidence | Candidate fields are known, but 11-A precedence/null behavior is not yet validated. |

## 5. Hand-Worked Validation Packet

The owner-reviewable timeline packet is recorded in Section 9. It shows opening balance, WO demand after own usable firm allocation, gross future forecast, confirmed in-horizon conventional PO receipts, weekly balances, safety stock, severities, and recovery measures.

| Component | Current status | Required evidence remaining |
|---|---|---|
| `ICC-00994` | Critical Short from Week 17 in the recorded snapshot. | Gross forecast consumption/overlap remains unresolved; the accepted gross-forecast interim rule was applied. |
| `ICC-01084` | No shortage in the recorded snapshot. | Gross forecast consumption/overlap remains unresolved; the accepted gross-forecast interim rule was applied. |
| `ICC-01117` | No shortage in the recorded snapshot. | Gross forecast consumption/overlap remains unresolved; the accepted gross-forecast interim rule was applied. |
| Shared workspace/other-program component `115989` | No shortage in the recorded snapshot. | Gross forecast consumption/overlap remains unresolved; the accepted gross-forecast interim rule was applied. |

## 6. Interim Rules

- Gross `mrp_det` `fcs_sum` forecast remains an interim, potentially overstated approximation because forecast consumption against sales orders is not modeled.
- Opening QOH is a settled Stage 11-A-specific direct `ld_det` aggregate. It excludes non-null MRB/INSPECT/NCMINSP status lots and non-null RMA/RA lot prefixes, includes Transit, and requires no date/expiration, positive-quantity, allocation, inventory-master, location, or inventory-status condition. KST performs no hard-allocation subtraction.

## 7. Stop Condition And Go/No-Go

**Stop condition met.** The opening-inventory business rule is not in dispute: QAD nettable QOH is already hard-allocation-netted and KST must not subtract allocations again. The Stage 9 physical usable-inventory-minus-allocation implementation/source mapping has not yet been reconciled to that semantic. The Q11-01 aggregate cannot provide that reconciliation, particularly because the three required samples have no qualifying usable hard allocation.

**No-go.** A later bounded evidence pass must reconcile the Stage 9 implementation/source mapping with the accepted opening-QOH semantic, establish forecast scoping/grain/sign, and complete the hand-worked timelines. The owner decisions below settle picked quantity, component-operation aggregation, cancellation-status handling, and the complete RMA exclusion.

### Q11-N02 Execution Result -- 2026-09-15

**Purpose, parameters, and limit:** Establish current WO lifecycle/source representation and safe header/detail/operation grain. Parameters were QAD domain `KTC`, site `SW`, current-week boundary, and the approved sample parts `ICC-00994`, `ICC-01084`, `ICC-01117`, and `HDW-50M-00060`. The named, parameterized SELECT requested at most 30 source-shape rows containing only status/type, due-date presence, issued/picked/hard-allocation presence, operation presence, and component identity.

**Initial result:** The command exceeded the configured SQL command timeout and returned no rows. A prior simultaneous invocation was also stopped after timeout when shared harness compilation conflicted; the retained harness was corrected to run sequentially, but the sequential `Q11-N02` execution independently timed out.

**Corrected-query authorization:** The original query’s `Hard` CTE aggregated every positive site-wide `lad_det` row before the outer `TOP (30)` filter. The corrected named `Q11-N02` first selects no more than 30 approved WO/component/operation candidates, then uses an `OUTER APPLY` hard-allocation existence probe restricted by the accepted domain, site, WOID, operation, and component allocation grain. It returns the same approved source-shape fields, no allocation quantities, and has no arbitrary-SQL path. It is an execution-bounding correction, not a business-rule or source-mapping change.

**Corrected-query result:** The query completed. All 30 deterministically ordered candidate rows were closed (`C`) rows for `HDW-50M-00060`; all had a due date and operation, 29 had issued quantity, none had picked quantity or a matching hard-allocation probe result, and WO type was blank. The aggregate consequently confirms only that closed records exist and that the bounded hard-allocation probe works. It does not establish eligible A/F/R/E/P representation, exclusions, picked treatment, or safe multi-operation aggregation.

**Next bounded refinement:** `Q11-N02` was split into named, fixed, parameterized `TOP (10)` source-shape samples for each header status A/F/R/E/P and a separately `TOP (10)` sampled multi-operation component condition. Each retained the same approved domain/site/sample-part boundaries and the same point hard-allocation probe; no site-wide allocation aggregation was permitted.

**Refined-query result:** Fixed `TOP (10)` status samples established `A`, `R`, and `P` as observed `wo_status` values for the approved sample set. All observed rows had a due date and nonzero operation; WO type was blank in the samples. Issued quantity was observed for `A` and `R`, not the sampled `P` rows. Picked quantity was not observed. The Released sample included two rows with a positive hard-allocation existence match at the accepted domain/site/WOID/operation/component grain. No `F` or `E` status rows were observed, and the fixed `TOP (10)` multi-operation query returned no rows.

**Disposition:** The result supports header/detail identity, due-date availability, and the operation-level hard-allocation probe. Later bounded reads confirmed F and E header-status representation. The 2026-09-16 owner decisions settle picked exclusion, additive component-operation aggregation, the absence of a cancelled status, and `RMABOM` as the complete RMA exclusion.

### Q11-N03 Execution Result -- 2026-09-15

**Purpose, parameters, and limit:** Identify one shared component in the authorized Taco workspace. Parameters were QAD domain `KTC`, site `SW`, Taco product line `3230`, and current effective date. The query returned one deterministic component aggregate only.

**Result:** Component `115989` was selected. It has one current-effective Taco parent, one distinct Taco parent represented in site-wide WO component events, four distinct other-program WO parents, and 62 site-wide WO component events.

**Disposition:** The result confirms that workspace component membership and site-wide other-program demand can coexist for one component. The balance must be calculated once site-wide; Taco versus other-program is retained solely as detail attribution. It does not establish the exact eligible 11-A WO event population or quantities; that remains dependent on unresolved lifecycle and aggregation evidence.

### Q11-N04 Execution Result -- 2026-09-15

**Purpose, parameters, and limit:** Examine approved PO lifecycle shapes for `KTC` / `SW`, the current-week/week-24 boundaries, and the four approved sample parts. The named query returned at most 30 source-shape rows without quantities or PO identifiers.

**Result:** The deterministic sample was dominated by historical `HDW-50M-00060` cancelled (`c`) and closed (`x`) lines. It observed past-due lines, unreceived and fully received forms, confirmation true and false, and no duplicate PO revision/line flag. No missing due date, in-horizon qualifying blank-status line, unconfirmed qualifying line, partial receipt, or unexpected status was observed in this sample.

**Disposition:** The results support C/X exclusion context and confirm that receipt lifecycle must remain line-specific. They do not complete the accepted PO edge-case evidence packet or validate selection ordering among qualifying in-horizon lines.

### Q11-N05 Execution Result -- 2026-09-15

**Purpose, parameters, and limit:** Validate KSS lifecycle shapes at `KTC` / `SW` using current effective date. The query returned aggregate state/count rows only.

**Result:** 651 active scheduled relationships were observed. All three required timeline components were absent from the active-KSS relationship predicate. No expired relationship row was returned by the bounded query.

**Disposition:** Active KSS and absent-KSS component states are evidenced; expired KSS behavior remains unobserved. This remains classification-only evidence and supplies no projected receipt quantity or timing.

### Q11-N06 Execution Result -- 2026-09-15

**Purpose, parameters, and limit:** Validate `fcs_sum` forecast source shape for `KTC` / `SW`, current-week through week-24 boundaries, and the four approved sample parts. The query allowed at most 20 grouped source-shape rows.

**Result:** No `fcs_sum` rows were returned for the approved sample parts.

**Disposition:** The interim gross-forecast rule remains unvalidated. No conclusion about forecast scoping, grain, sign, due-date behavior, or overlap can be drawn from an empty bounded sample.

### Q11-N07 Execution Result -- 2026-09-15

**Purpose, parameters, and limit:** Validate safety-stock precedence and list presentation-field presence for the three required timeline components at `KTC` / `SW`. The query returned one no-quantity source-presence row per component.

**Result:** `ICC-00994`, `ICC-01084`, and `ICC-01117` all had a master and selected-site part row. For all three, selected-site safety stock was present and nonzero; QAD status, purchasing lead time, and buyer source values were present.

**Disposition:** The observed samples support selected-site `ptp_det.ptp_sfty_stk` as the safety-stock source for their 11-A severity calculations. This is not yet a general fallback/null precedence rule; an explicit missing-site and missing/zero safety-stock case remains unobserved.

**Disposition:** Required WO source evidence is unavailable within the approved read boundary. No query predicate, sample scope, command timeout, SQL plan, index, or database setting was changed. `Q11-N03` through `Q11-N07` were not run because the Stage 11 stopping condition requires this pass to stop when a required database read boundary is unavailable. They remain approved-query proposals only and require a separate approved pass after owner direction on a safely bounded WO evidence retrieval approach.

## 8. Proposed Next Bounded Evidence Queries

Separate approval is required before any of the following SELECT-only, parameterized, bounded reads:

| ID | Purpose | Bounded scope and output |
|---|---|---|
| Q11-N01 | Reconcile the Stage 9 physical usable inventory and hard-allocation source mapping to the accepted QAD nettable-QOH-already-netted semantic. | One site; one component with a known usable hard allocation; lot/location-level source identities and aggregate comparison only. |
| Q11-N02 | Establish A/F/R/E/P representation plus closed, cancelled, RMA, RMABOM, and safe component/operation aggregation. | One site; TOP-limited WO/header-detail samples selected by each lifecycle representation; return source-shape flags and aggregate quantities only. |
| Q11-N03 | Establish one shared component’s workspace-parent versus other-program attribution without double counting the site-wide component balance. | One active workspace, its resolved parents, and one selected shared component; distinct parent identifiers/counts and event counts only. |
| Q11-N04 | Complete conventional PO lifecycle evidence: confirmed/unconfirmed, partial/full receipt, missing due date, past due, C/X/blank/unexpected status, revision identity, and deterministic ordering. | One site; selected components and TOP-limited line samples; source-shape flags and ordering keys only. |
| Q11-N05 | Complete KSS classification lifecycle evidence. | One site; active, expired, and absent relationship candidates; effective-boundary and scheduled-state flags only. |
| Q11-N06 | Establish `fcs_sum` forecast source scope, row grain, sign, due-date behavior, and a forecast-overlap candidate. | One site; selected components/current-to-week-24 horizon and TOP-limited overlap sample; source identifiers, dataset/type/date/sign flags, and counts only. |
| Q11-N07 | Establish safety-stock source precedence and remaining presentation candidates. | Required timeline components; site/master presence, null/zero, and precedence flags plus non-sensitive codes only. |

### Q11-N02 Through Q11-N07 -- Approved Query Records

All six reads use the retained `scratch/stage11-qad-validation/` named-query harness, Windows-integrated QAD access, parameter binding, and SELECT-only statements. They accept no arbitrary SQL and return no credentials, supplier names, user identities, lot identifiers, or raw quantity values.

| ID | Purpose | Parameters | Limit and returned summary |
|---|---|---|---|
| Q11-N02 | Establish current WO lifecycle/source representation and safe header-detail-operation grain. | `KTC`, `SW`, current-week start; sample parts `ICC-00994`, `ICC-01084`, `ICC-01117`, `HDW-50M-00060`. | At most 30 grouped status/type/flag rows; status/type, due-date presence, multi-operation, issued/picked/hard-allocation presence, and row counts only. |
| Q11-N03 | Establish one Taco workspace shared component and workspace-parent versus other-program demand attribution. | `KTC`, `SW`, Taco product line `3230`, current effective date. | One selected component aggregate; resolved Taco-parent count, workspace-parent count, other-parent count, and event counts only. |
| Q11-N04 | Complete PO lifecycle edge evidence. | `KTC`, `SW`, current-week start and week-24 exclusive end; sample parts above. | At most 30 grouped lifecycle rows; part, status, confirmation, date/horizon, receipt-shape, revision/line duplicate flags, and counts only. |
| Q11-N05 | Validate KSS active, expired, and absent relationship shapes. | `KTC`, `SW`, current effective date. | At most 3 aggregate rows for active/expired/absent states and counts only. |
| Q11-N06 | Validate `fcs_sum` source scope/grain/sign/date and forecast-overlap candidate shape. | `KTC`, `SW`, current-week start and week-24 exclusive end; sample parts above. | At most 20 grouped forecast rows plus one overlap aggregate; part, type, due-date/horizon, sign, source-key duplicate flag, and counts only. |
| Q11-N07 | Validate site/master safety-stock precedence and presentation-field presence. | `KTC`, `SW`; `ICC-00994`, `ICC-01084`, `ICC-01117`. | At most 3 rows; source-presence, null/zero, status/lead/buyer presence flags, and safety-stock precedence category only. |

### Follow-Up Approved Query Records -- 2026-09-15

The following fixed, named, parameterized SELECT-only reads are separately approved for the retained harness. Discovery queries select at most one deterministic candidate per required state; inspection queries use that candidate inside the same fixed statement and return source-shape flags/counts only. No query accepts a component, WO, PO, or SQL value at runtime.

| ID | Purpose | Parameters and limit | Output |
|---|---|---|---|
| Q11-F01 | Reconcile candidate QAD inventory-master quantities with the locked Stage 9 physical usable-minus-hard-allocation mapping for the known usable-hard-allocation component. | `KTC`, `SW`, `HDW-50M-00060`, current date and issue-day cutoff; one aggregate row. | Presence/equality/difference flags only for `in_mstr` candidate fields versus physical and physical-minus-hard totals. No quantity retained. |
| Q11-F02 | Find and inspect F/E WO-status source cases. | `KTC`, `SW`; each state selected by fixed `TOP (1)` candidate and inspected as at most one grouped row. | Status/WO-type/due-date/issued/hard-allocation presence flags only. Historical picked and multi-operation evidence is superseded by owner decision. |
| Q11-F04 | Find one PO example for each missing lifecycle edge. | `KTC`, `SW`, current-week and week-24 bounds; fixed `TOP (1)` candidate per edge. | Edge category, status/confirmation/date/receipt/revision-line flags only. No PO number or quantity retained. |
| Q11-F05 | Find and inspect expired KSS relationship examples. | `KTC`, `SW`, current date; fixed `TOP (3)`. | Expired effective-boundary and scheduled-PO flags only. |
| Q11-F06 | First find one future `fcs_sum` candidate, then inspect its source shape and MRP sales-order overlap. | `KTC`, `SW`, current-week and week-24 bounds; one candidate aggregate. | Future-forecast candidate presence, type/sign/date/key-duplication flags, and sales-order-overlap row presence only. |
| Q11-F07 | Find one selected-site missing, null, and zero safety-stock case to test fallback/state behavior. | `KTC`, `SW`; fixed `TOP (1)` candidate per state. | Source-presence, master fallback presence, and safety-stock state categories only. |

### Q11-F02 Redesign -- 2026-09-15

**Why the prior combined query could time out:** Its four independent `TOP (1)` branches were combined by `UNION ALL`; each joined `wo_mstr` and `wod_det` before candidate-key reduction. Most importantly, its multi-operation branch grouped the entire KTC/SW `wod_det` population before `TOP (1)` could apply. Output limits never bound that input work.

**Redesigned independent reads:** Each fact executes sequentially with a 60-second command timeout. F and E candidate reads access only `wo_mstr` and return at most five WOIDs. Picked-quantity candidate read accesses only `wod_det` and returns at most five WOIDs. Each returned WOID then drives an exact-key header/detail read at WOID/component/operation grain, returning at most 50 rows and only status, due-date presence, component, operation, and required/issued/picked/hard-allocation presence flags. The multi-operation candidate read is bounded to the authorized Taco product-line-3230 resolved-parent scope, returns at most five WOIDs, and then uses the same exact-key detail read. No candidate read joins allocation, BOM, PO, MRP, or inventory tables; the hard-allocation existence test occurs only after a WOID/component/operation key is known.

### Q11-F02 Redesigned Execution Result -- 2026-09-15

**F-status candidate and detail:** The `wo_mstr`-only candidate read returned five bounded candidate keys. Exact-WOID detail reads returned 3 to 8 component-operation rows per key. All returned detail rows had header status `F`, a present due date, nonzero operation `50`, required quantity presence except one row, no issued quantity, no picked quantity, and no positive operation-grain hard-allocation match. **Status representation confirmed.**

**E-status candidate and detail:** The `wo_mstr`-only candidate read returned five bounded candidate keys. Exact-WOID detail reads returned nine component-operation rows for the observed candidate. All returned detail rows had header status `E`, a present due date, nonzero operation `50`, required quantity presence, no issued quantity, no picked quantity, and no positive operation-grain hard-allocation match. **Status representation confirmed:** the accepted 11-A `E` is observed as `wo_mstr.wo_status = 'E'`; it is not evidenced only through the MPS explicit-schedule marker.

**Picked-quantity candidate:** The independent `wod_det`-only candidate-key read exceeded its 60-second command timeout before returning a key. No detail read was executed. This historical evidence gap is superseded by the 2026-09-16 owner decision that `wod_qty_pick` is not used in this QAD process.

**Stop condition:** The timed-out candidate read required this pass to stop immediately. The multi-operation fact was not run. No retry, widening, timeout change, query hint, plan inspection, or database change was performed. The former picked and multi-operation evidence gaps are now superseded by the 2026-09-16 owner decisions.

### Q11-F02 Taco Exact-WOID Read -- Approved Query Record

**Purpose:** Inspect the project-owner supplied Taco WOID `33398154` as a bounded replacement for the unavailable multi-operation candidate search, and determine whether it also supplies picked-quantity evidence.

**Candidate-key read:** Parameterized `wo_mstr`-only lookup at `KTC` / `SW` / WOID `33398154`, `TOP (1)`. It returns only whether the exact header key exists.

**Detail read:** Runs only if the candidate exists. Parameterized `wo_mstr` plus `wod_det` detail lookup restricted to the exact domain/site/WOID; `TOP (50)`, ordered by component and operation. It returns only status, due-date presence, component, operation, required/issued/picked presence, and hard-allocation presence. The hard-allocation test is a point existence probe at the accepted domain/site/WOID/operation/component grain, after the detail key is established.

**Command limit:** 60 seconds for each read.

**Result:** The exact `wo_mstr` candidate key was found. The detail read returned 19 component-operation rows. All rows had header status `A`, a present due date, and operation `50`. Required and issued quantities were present on a subset of rows; no picked quantity or positive operation-grain hard-allocation match was observed.

**Disposition:** The supplied Taco WOID confirms that one Work Order can contain multiple component-operation detail rows: 19 rows in this bounded detail read. The evidence does not show multiple distinct operation numbers. The subsequent 2026-09-16 owner decision settles aggregation at the distinct `(WOID, component, wod_op)` grain and excludes `wod_qty_pick` from demand calculations. No timeout or stopping condition occurred on either exact-key read.

### Q11-F02 Taco Exact-WOID Reads -- Approved Query Record

**Purpose:** Inspect project-owner supplied Taco candidate WOIDs `32365817` and `32365818` for multiple distinct component-operation rows and picked-quantity evidence.

**Candidate-key read:** Each WOID is an independently named, fixed harness command. A parameterized `wo_mstr`-only lookup at `KTC` / `SW` returns at most one header-presence row per supplied WOID.

**Detail read:** Runs only for a found candidate. The parameterized `wo_mstr` + `wod_det` exact-WOID detail read returns at most 50 rows and only status, due-date presence, component, operation, required/issued/picked presence, and operation-grain hard-allocation presence. Command timeout is 60 seconds per read. **Result:** Pending execution.

### Q11-F02 Taco Exact-WOID `32365817` Result -- 2026-09-15

**Candidate-key result:** The parameterized KTC/SW `wo_mstr` lookup found the supplied WOID.

**Detail result:** The exact-key detail read returned its capped 50 component-operation rows within the 60-second limit. All observed rows had header status `R` and a present due date. Distinct operation numbers `20`, `35`, `50`, and `110` occurred in the bounded detail set. Required and issued quantities were present on a subset of rows; picked quantity and positive operation-grain hard allocation were not observed.

**Disposition:** **Confirmed.** A single Work Order can contain component-detail rows at multiple distinct operations. Under the 2026-09-16 owner decision, calculate remaining demand at the distinct `(WOID, component, wod_op)` row grain and sum the results into the component's weekly demand; separate operation rows are explicit requirements and must not be deduplicated. `wod_qty_pick` is excluded from the calculation. The alternative supplied WOID `32365818` was not queried because `32365817` satisfied the bounded validation objective.

### Q11-F04 Follow-Up Result -- 2026-09-15

**Purpose, parameters, and limit:** Deliberately select one KTC/SW PO line for each missing lifecycle edge using fixed `TOP (1)` predicates and current-week/week-24 boundaries. The query returned status/confirmation/date/receipt-state flags only.

**Result:** Four candidates were found: a missing-due line with confirmation present; a partial-receipt line that was unconfirmed and past due; a confirmed, qualifying, blank-status in-horizon line with no receipt; and an unconfirmed, qualifying, blank-status in-horizon line with no receipt. No unexpected-status candidate was returned.

**Disposition:** Missing-due, partial-receipt, confirmed qualifying in-horizon, and unconfirmed qualifying in-horizon source shapes are confirmed. The contract’s rules remain unchanged: only confirmed qualifying in-horizon open quantity may become supply; missing due, past due, and unconfirmed lines remain context. Unexpected status is not observed in this bounded sample and remains an evidence gap.

### Q11-F05 Follow-Up Result -- 2026-09-15

**Purpose, parameters, and limit:** Select up to three expired KTC/SW scheduled-PO relationships using the accepted KSS tables and effective boundaries. The read returned boundary-state flags only.

**Result:** Three scheduled relationships were returned. In each, `po_sched` was true and the PO effective-to boundary was expired; the PO-detail effective boundary was not expired.

**Disposition:** **Confirmed.** A scheduled relationship with an expired `po_eff_to` is not an active KSS relationship even where `po_sched = 1` and the PO-detail boundary is current. KSS remains classification only and contributes no projected supply.

### Q11-F06 Follow-Up Result -- 2026-09-15

**Purpose, parameters, and limit:** Select one future KTC/SW `fcs_sum` candidate inside the current-week through week-24 horizon, then inspect its forecast source shape and whether the same component has an `sod_det` MRP record. The query returned one aggregate flag row without retaining the candidate component or quantities.

**Result:** A future forecast candidate exists. Its `fcs_sum` records had no missing due date, no negative quantity, positive quantity present, and no duplicate `(mrp_nbr, mrp_line)` source-key condition in the sampled candidate’s record set. No `sod_det` MRP overlap was present for that selected candidate.

**Disposition:** The evidence supports positive, future-dated `fcs_sum` facts at KTC/SW and confirms the prior timeline sample’s empty forecast result was sample-specific. It does not establish forecast consumption, the general forecast row identity, or the relationship to firm demand: the selected future candidate had no sales-order MRP overlap. Gross `fcs_sum` remains interim and explicitly potentially overstated.

### Q11-F07 Follow-Up Result -- 2026-09-15

**Purpose, parameters, and limit:** Select one KTC/SW component for each selected-site missing, selected-site null safety stock, and selected-site zero safety-stock state. Each fixed `TOP (1)` candidate returns source-presence and zero-state flags only.

**Result:** Two candidates were returned. One had no selected-site `ptp_det` row while a master safety-stock value was present and zero. One had a selected-site row with zero safety stock and a master safety-stock value present. No selected-site null safety-stock candidate was returned.

**Disposition:** Selected-site absence, selected-site zero, and master presence are real source states. The subsequent owner decision below resolves zero-site and absent-site precedence; selected-site null remains an explicit unresolved data state and does not trigger fallback. The missing-null source sample remains an evidence gap, but it is not a rule ambiguity.

### Owner Decision -- Safety-Stock Precedence -- 2026-09-15

**Accepted rule:** A selected-site `ptp_det` row with non-null `ptp_sfty_stk` is authoritative for that site, including zero. Only when the selected-site row is absent may 11-A fall back to `pt_mstr.pt_sfty_stk`. When the selected-site row exists but `ptp_sfty_stk` is null, preserve an unresolved source-data state; do not silently treat it as zero and do not use the master value.

**Evidence reconciliation:** Q11-N07 confirms nonzero site values for the required timeline components. Q11-F07 confirms that selected-site absence and selected-site zero are real source states. The rule above governs their handling; no additional QAD read is required to choose the precedence.

**Forecast rule retained:** Gross future-dated `mrp_det` `fcs_sum` remains the accepted interim forecast-demand rule, expressly carrying the potentially-overstated caveat because forecast consumption is not modeled.

## 9. Owner-Reviewable 24-Week Validation Timelines

**Snapshot and calculation boundary:** QAD domain `KTC`, site `SW`, refreshed 2026-09-15. The displayed horizon is Sunday 2026-09-13 through Saturday 2027-02-27 (Weeks 1-24). Each projected balance is opening QOH plus confirmed conventional, non-scheduled PO supply minus eligible WO remaining demand minus gross `fcs_sum` forecast demand. WO remaining demand is calculated at the distinct `(WOID, component, wod_op)` grain as `max(0, wod_qty_req - wod_qty_iss - own usable firm allocation)`, then summed by component week. Closed (`C`) and `RMABOM` WOs are excluded. `wod_qty_pick` is excluded.

**Forecast caveat:** Gross `fcs_sum` is the accepted interim demand rule but remains potentially overstated because forecast consumption is not modeled. No in-horizon `fcs_sum` rows were returned for these four components in this snapshot, so every displayed forecast-demand value is zero; this is not evidence that forecast consumption is resolved.

**Display precision:** Inputs and balances below are rounded to two decimal places for owner review. Calculations used QAD-returned decimal quantities without a KST unit conversion.

**Execution record:** Named harness read `Q11-Timeline` returned 96 rows: four fixed components multiplied by 24 Sunday-start weeks. It used the settled direct `ld_det` opening rule, selected-site safety-stock precedence, Stage 9 usable-lot qualification for own firm allocations, gross `fcs_sum` forecast, and confirmed non-scheduled conventional PO supply. No arbitrary SQL input, source write, or RMA discovery read was permitted.

### `ICC-00994`

Opening QOH: `120721.00`; safety stock: `14838.00` from selected-site `ptp_det`. First Safety Stock Short: Week 16. First Critical Short: Week 17. No recovery occurs in the displayed horizon.

| Week | Start | WO Demand | PO Supply | Gross Forecast | Projected Balance |
|---:|---|---:|---:|---:|---:|
| 1 | 2026-09-13 | 18796.99 | 0.00 | 0.00 | 101924.01 |
| 2 | 2026-09-20 | 8956.39 | 0.00 | 0.00 | 92967.62 |
| 3 | 2026-09-27 | 20462.66 | 0.00 | 0.00 | 72504.96 |
| 4 | 2026-10-04 | 6618.55 | 0.00 | 0.00 | 65886.41 |
| 5 | 2026-10-11 | 1616.04 | 0.00 | 0.00 | 64270.37 |
| 6 | 2026-10-18 | 6027.07 | 0.00 | 0.00 | 58243.31 |
| 7 | 2026-10-25 | 12356.89 | 0.00 | 0.00 | 45886.41 |
| 8 | 2026-11-01 | 6329.82 | 0.00 | 0.00 | 39556.59 |
| 9 | 2026-11-08 | 3665.16 | 0.00 | 0.00 | 35891.43 |
| 10 | 2026-11-15 | 14817.04 | 0.00 | 0.00 | 21074.38 |
| 11 | 2026-11-22 | 894.24 | 0.00 | 0.00 | 20180.15 |
| 12 | 2026-11-29 | 0.00 | 0.00 | 0.00 | 20180.15 |
| 13 | 2026-12-06 | 0.00 | 0.00 | 0.00 | 20180.15 |
| 14 | 2026-12-13 | 0.00 | 0.00 | 0.00 | 20180.15 |
| 15 | 2026-12-20 | 3663.16 | 0.00 | 0.00 | 16516.99 |
| 16 | 2026-12-27 | 5802.51 | 0.00 | 0.00 | 10714.48 |
| 17 | 2027-01-03 | 15837.59 | 0.00 | 0.00 | -5123.11 |
| 18 | 2027-01-10 | 916.29 | 0.00 | 0.00 | -6039.40 |
| 19 | 2027-01-17 | 2584.46 | 0.00 | 0.00 | -8623.86 |
| 20 | 2027-01-24 | 3584.96 | 0.00 | 0.00 | -12208.82 |
| 21 | 2027-01-31 | 5497.74 | 0.00 | 0.00 | -17706.57 |
| 22 | 2027-02-07 | 15957.89 | 0.00 | 0.00 | -33664.46 |
| 23 | 2027-02-14 | 14249.62 | 0.00 | 0.00 | -47914.09 |
| 24 | 2027-02-21 | 18359.90 | 0.00 | 0.00 | -66273.99 |

### `ICC-01084`

Opening QOH: `63974.00`; safety stock: `36568.00` from selected-site `ptp_det`. No Safety Stock Short or Critical Short occurs in the displayed horizon. The lowest balance is `37703.57` in Weeks 11-12.

| Week | Start | WO Demand | PO Supply | Gross Forecast | Projected Balance |
|---:|---|---:|---:|---:|---:|
| 1 | 2026-09-13 | 9398.50 | 0.00 | 0.00 | 54575.50 |
| 2 | 2026-09-20 | 4478.20 | 0.00 | 0.00 | 50097.31 |
| 3 | 2026-09-27 | 10231.33 | 4000.00 | 0.00 | 43865.98 |
| 4 | 2026-10-04 | 3309.27 | 0.00 | 0.00 | 40556.71 |
| 5 | 2026-10-11 | 808.02 | 4000.00 | 0.00 | 43748.69 |
| 6 | 2026-10-18 | 3013.53 | 4000.00 | 0.00 | 44735.15 |
| 7 | 2026-10-25 | 6178.45 | 12000.00 | 0.00 | 50556.71 |
| 8 | 2026-11-01 | 3164.91 | 0.00 | 0.00 | 47391.79 |
| 9 | 2026-11-08 | 1832.58 | 0.00 | 0.00 | 45559.21 |
| 10 | 2026-11-15 | 7408.52 | 0.00 | 0.00 | 38150.69 |
| 11 | 2026-11-22 | 447.12 | 0.00 | 0.00 | 37703.57 |
| 12 | 2026-11-29 | 0.00 | 0.00 | 0.00 | 37703.57 |
| 13 | 2026-12-06 | 0.00 | 0.00 | 0.00 | 37703.57 |
| 14 | 2026-12-13 | 0.00 | 12000.00 | 0.00 | 49703.57 |
| 15 | 2026-12-20 | 1831.58 | 0.00 | 0.00 | 47871.99 |
| 16 | 2026-12-27 | 2901.25 | 0.00 | 0.00 | 44970.74 |
| 17 | 2027-01-03 | 7918.80 | 4000.00 | 0.00 | 41051.94 |
| 18 | 2027-01-10 | 458.15 | 0.00 | 0.00 | 40593.80 |
| 19 | 2027-01-17 | 1292.23 | 0.00 | 0.00 | 39301.57 |
| 20 | 2027-01-24 | 1792.48 | 8000.00 | 0.00 | 45509.09 |
| 21 | 2027-01-31 | 2748.87 | 8000.00 | 0.00 | 50760.22 |
| 22 | 2027-02-07 | 7978.95 | 16000.00 | 0.00 | 58781.27 |
| 23 | 2027-02-14 | 7124.81 | 0.00 | 0.00 | 51656.46 |
| 24 | 2027-02-21 | 9179.95 | 0.00 | 0.00 | 42476.51 |

### `ICC-01117`

Opening QOH: `37056.00`; safety stock: `13761.00` from selected-site `ptp_det`. No Safety Stock Short or Critical Short occurs in the displayed horizon. The lowest balance is `51623.81` in Week 3. The current opening balance was reconfirmed immediately after the timeline read as `37056.00` across 12 qualifying lots; it supersedes the earlier live T01 observation of `46056.00` across 14 qualifying lots.

| Week | Start | WO Demand | PO Supply | Gross Forecast | Projected Balance |
|---:|---|---:|---:|---:|---:|
| 1 | 2026-09-13 | 7954.25 | 39000.00 | 0.00 | 68101.75 |
| 2 | 2026-09-20 | 8078.20 | 0.00 | 0.00 | 60023.56 |
| 3 | 2026-09-27 | 8399.75 | 0.00 | 0.00 | 51623.81 |
| 4 | 2026-10-04 | 3309.27 | 12000.00 | 0.00 | 60314.53 |
| 5 | 2026-10-11 | 808.02 | 0.00 | 0.00 | 59506.51 |
| 6 | 2026-10-18 | 3013.53 | 9000.00 | 0.00 | 65492.98 |
| 7 | 2026-10-25 | 6178.45 | 0.00 | 0.00 | 59314.53 |
| 8 | 2026-11-01 | 3164.91 | 12000.00 | 0.00 | 68149.62 |
| 9 | 2026-11-08 | 936.34 | 0.00 | 0.00 | 67213.28 |
| 10 | 2026-11-15 | 7408.52 | 12000.00 | 0.00 | 71804.76 |
| 11 | 2026-11-22 | 447.12 | 0.00 | 0.00 | 71357.64 |
| 12 | 2026-11-29 | 0.00 | 0.00 | 0.00 | 71357.64 |
| 13 | 2026-12-06 | 0.00 | 69000.00 | 0.00 | 140357.64 |
| 14 | 2026-12-13 | 0.00 | 0.00 | 0.00 | 140357.64 |
| 15 | 2026-12-20 | 529.32 | 0.00 | 0.00 | 139828.32 |
| 16 | 2026-12-27 | 2901.25 | 18000.00 | 0.00 | 154927.07 |
| 17 | 2027-01-03 | 7918.80 | 0.00 | 0.00 | 147008.27 |
| 18 | 2027-01-10 | 458.15 | 0.00 | 0.00 | 146550.12 |
| 19 | 2027-01-17 | 274.69 | 12000.00 | 0.00 | 158275.44 |
| 20 | 2027-01-24 | 773.93 | 0.00 | 0.00 | 157501.50 |
| 21 | 2027-01-31 | 2748.87 | 9000.00 | 0.00 | 163752.63 |
| 22 | 2027-02-07 | 7978.95 | 0.00 | 0.00 | 155773.68 |
| 23 | 2027-02-14 | 5862.66 | 0.00 | 0.00 | 149911.03 |
| 24 | 2027-02-21 | 9179.95 | 0.00 | 0.00 | 140731.08 |

### Shared Taco Component `115989`

Opening QOH: `0.96`; safety stock: `0.00` from selected-site `ptp_det`. No Safety Stock Short or Critical Short occurs in the displayed horizon. One eligible WO demand of `0.90` occurs in Week 12, leaving a balance of `0.06`; there is no projected supply or gross forecast demand.

| Week | Start | WO Demand | PO Supply | Gross Forecast | Projected Balance |
|---:|---|---:|---:|---:|---:|
| 1 | 2026-09-13 | 0.00 | 0.00 | 0.00 | 0.96 |
| 2 | 2026-09-20 | 0.00 | 0.00 | 0.00 | 0.96 |
| 3 | 2026-09-27 | 0.00 | 0.00 | 0.00 | 0.96 |
| 4 | 2026-10-04 | 0.00 | 0.00 | 0.00 | 0.96 |
| 5 | 2026-10-11 | 0.00 | 0.00 | 0.00 | 0.96 |
| 6 | 2026-10-18 | 0.00 | 0.00 | 0.00 | 0.96 |
| 7 | 2026-10-25 | 0.00 | 0.00 | 0.00 | 0.96 |
| 8 | 2026-11-01 | 0.00 | 0.00 | 0.00 | 0.96 |
| 9 | 2026-11-08 | 0.00 | 0.00 | 0.00 | 0.96 |
| 10 | 2026-11-15 | 0.00 | 0.00 | 0.00 | 0.96 |
| 11 | 2026-11-22 | 0.00 | 0.00 | 0.00 | 0.96 |
| 12 | 2026-11-29 | 0.90 | 0.00 | 0.00 | 0.06 |
| 13 | 2026-12-06 | 0.00 | 0.00 | 0.00 | 0.06 |
| 14 | 2026-12-13 | 0.00 | 0.00 | 0.00 | 0.06 |
| 15 | 2026-12-20 | 0.00 | 0.00 | 0.00 | 0.06 |
| 16 | 2026-12-27 | 0.00 | 0.00 | 0.00 | 0.06 |
| 17 | 2027-01-03 | 0.00 | 0.00 | 0.00 | 0.06 |
| 18 | 2027-01-10 | 0.00 | 0.00 | 0.00 | 0.06 |
| 19 | 2027-01-17 | 0.00 | 0.00 | 0.00 | 0.06 |
| 20 | 2027-01-24 | 0.00 | 0.00 | 0.00 | 0.06 |
| 21 | 2027-01-31 | 0.00 | 0.00 | 0.00 | 0.06 |
| 22 | 2027-02-07 | 0.00 | 0.00 | 0.00 | 0.06 |
| 23 | 2027-02-14 | 0.00 | 0.00 | 0.00 | 0.06 |
| 24 | 2027-02-21 | 0.00 | 0.00 | 0.00 | 0.06 |

**Owner-review disposition:** `ICC-00994` is Critical Short beginning Week 17. `ICC-01084`, `ICC-01117`, and `115989` remain at or above their selected-site safety-stock thresholds in this snapshot. These are validation timelines for owner review, not implementation authorization. Gross forecast remains an accepted but potentially overstated interim rule.

## 9A. Part `145MF3010` Workspace-versus-QAD Discrepancy Diagnostic -- 2026-09-16

**Scope:** Diagnose only. No Stage 11-A calculation, source reader, cache, API, UI, export, Stage 9, Stage 10, or QAD query behavior was changed.

**Requested reconciliation boundary:** The requested comparison requires the active workspace's site, the exact in-memory MPS snapshot ID and its refresh date, the Sunday-start Week 1 boundary derived from that refresh date, the currently displayed Workspace Shortages row, and the corresponding QAD shortage result/fact that reportedly shows `145MF3010` short this week.

| Required evidence | Result | Consequence |
|---|---|---|
| Active workspace/site | Not available in the repository or local KST data directory. | The part cannot be safely assigned a domain/site or report population. |
| Active MPS snapshot ID and exact app refresh date | Not available. No `Kst.Api` process was running during the diagnostic. MPS snapshots are in-memory and are not persisted across application sessions. | The requested snapshot and exact Sunday-start Week 1 cannot be reproduced. |
| Displayed Workspace Shortages row | Not available from an active backend/API response or retained diagnostic artifact. | Opening QOH, weekly balances, and severity actually displayed by the application cannot be compared. |
| QAD shortage result/fact | Not available in supplied repository evidence. The configured Shortages integration is disabled/not configured, and no result identifier, timestamp, or source output was supplied. | The asserted QAD "short this week" state cannot be attributed to a source row or daily event. |
| Part-specific QAD source facts for `145MF3010` | No approved named fixed-scope validation query covers this part. Existing timeline evidence is limited to `ICC-00994`, `ICC-01084`, `ICC-01117`, and `115989`. | No new QAD query was introduced or run during this diagnostic-only pass. |

**Stage 11-A rules confirmed from the implemented reader and current plan:** The direct opening-QOH aggregate excludes only `MRB`/`INSPECT`/`NCMINSP` statuses and `RMA%`/`RA%` lots, includes Transit, and does not subtract allocations. Eligible WO residual demand is at `(WOID, component, operation)` grain, includes A/F/R/E/P and excludes closed/RMABOM work, removes only owning usable firm allocation, and rolls past-due residual demand into Week 1. Gross future `fcs_sum` forecast is separate demand. Only confirmed, non-scheduled, conventional, in-horizon PO open quantity is supply; past-due, unconfirmed, missing-due, KSS/scheduled, and post-horizon PO lines are not supply. Same-week inputs are netted; no daily event order is represented.

**Disposition -- stop for owner direction:** No evidence supports a conclusion that weekly netting alone explains the discrepancy, and no source/bucket/quantity/eligibility mismatch can be identified without the missing runtime and QAD-shortage evidence. Provide either (1) the live Workspace Shortages response/row together with its workspace, snapshot ID, and refresh date, plus the QAD shortage result with source/time context, or (2) explicit authorization for a new fixed, parameterized, SELECT-only part-specific diagnostic read at the active site. Do not infer or change any rule from the reported shortage state.

## 10. Proposed Next Technical Evidence Reads

Each requires separate approval, must remain parameterized and SELECT-only, use the retained `scratch/stage11-qad-validation/` harness, set command timeout to 60 seconds or less, and return only source-shape flags/counts plus the minimum required key for follow-up detail.

| ID | Candidate-key read | Exact-key detail read | Expected evidence |
|---|---|---|---|
| Q11-T01 Opening QOH | Complete. | Complete. | Settled direct `ld_det` source and bounded four-part confirmation are recorded below. |
| Q11-T02 Picked quantity | Superseded by owner decision. | No further read required. | `wod_qty_pick` is excluded from Stage 11-A demand calculations. |
| Q11-T03 RMA representation | Complete by owner decision. | No QAD read required. | `wo_bom_code = 'RMABOM'` is the complete report-level RMA predicate. |
| Q11-T04 Multi-operation aggregation | Superseded by owner decision. | No further read required. | Distinct `(WOID, component, wod_op)` demand rows are additive after joins preserve that identity. |

### Q11-T02 Picked-Quantity Exact-WOID Result -- 2026-09-15

**Parameters and limit:** Fixed, parameterized, SELECT-only exact-key read for domain `KTC`, site `SW`, and owner-supplied WOID `32365903`. One grouped header/detail result was returned; no component, lot, raw quantity, or allocation value was retained.

**Result:** The header is status `R` and is not `RMABOM`. It has 79 detail rows. Required, issued, and picked fields are present on all 79 rows. All picked values are zero; there are no positive own hard allocations at the operation/component grain. Fifteen rows have picked equal to issued. The all-zero picked and allocation values explain the 79 picked-equals-own-hard-allocation comparisons.

**Disposition:** Superseded by the 2026-09-16 owner decision. `wod_qty_pick` is not used for this QAD process and is excluded from Stage 11-A demand calculations. Remaining demand is `max(0, wod_qty_req - wod_qty_iss - own firm allocation)`; no further picked-quantity evidence is required.

### Q11-T04 Multi-Operation Candidate Result -- 2026-09-15

**Parameters and limit:** Fixed, parameterized, SELECT-only `TOP (5)` candidate-key read for domain `KTC`, site `SW`, and owner-supplied part `188A671`. It returned only WOIDs having more than one distinct `wod_op`.

**Result:** No candidate WOID was returned.

**Disposition:** Superseded by the 2026-09-16 owner decision. No separate multi-operation validation case is required. Preserve distinct WOD component rows, including `wod_op`, in the demand identity; prevent joins from duplicating those rows; calculate remaining demand for each row; then sum row demand into the component's weekly demand.

### Owner Decisions -- WO Demand And Eligibility -- 2026-09-16

**Picked quantity:** `wod_qty_pick` is not used in this QAD process. Exclude it from Stage 11-A demand calculations and from implementation-blocker tracking. Remaining demand is `max(0, wod_qty_req - wod_qty_iss - own firm allocation)`.

**Distinct component-operation rows:** Calculate remaining demand at the distinct WOD `(WOID, component, wod_op)` grain. Preserve `wod_op` in the identity and all joins to prevent row duplication. Sum the resulting remaining-demand rows into the component's weekly demand. A component represented at more than one operation has separate explicit requirement rows; do not omit or deduplicate them.

**Cancellation status:** There is no separate cancelled work-order status. Exclude closed work with `wo_status = 'C'` and retain the established `wo_bom_code = 'RMABOM'` exclusion.

**RMA representation:** `wo_bom_code = 'RMABOM'` is the complete QAD representation of RMA work for this report. There is no separate non-`RMABOM` RMA classification to discover, and no QAD read is authorized for it.

### Q11-T03 RMA Representation Disposition

**Owner decision, 2026-09-16:** `wo_bom_code = 'RMABOM'` is the complete QAD representation of RMA work for this report. There is no separate non-`RMABOM` RMA work-order classification.

**Disposition:** **Confirmed.** The Stage 11-A WO exclusion is `wo_status = 'C'` or `wo_bom_code = 'RMABOM'`. No QAD read was run or is authorized to discover an additional RMA representation.

### Q11-T01 Opening-QOH Confirmation -- 2026-09-15

**Settled rule:** For each reported Stage 11-A component, `OpeningQoh` is `COALESCE(SUM(ld_det.ld_qty_oh), 0)` at QAD domain/site/part grain. Qualifying rows have `UPPER(ld_status) NOT IN ('MRB', 'INSPECT', 'NCMINSP')`, `UPPER(ld_lot) NOT LIKE 'RMA%'`, and `UPPER(ld_lot) NOT LIKE 'RA%'`. Transit is included. Null `ld_status` and null `ld_lot` values are excluded by the specified SQL predicates. There is no positive-quantity filter and no current-date input. KST does not subtract hard allocations.

**Execution inventory:** The four fixed, parameterized, SELECT-only commands each returned one aggregate row: `Q11-T01-HDW` (`HDW-50M-00060`), `Q11-T01-ICC-00994`, `Q11-T01-ICC-01084`, and `Q11-T01-ICC-01117`. No additional read or query change was made.

| Part | OpeningQoh | QualifiedLotCount |
|---|---:|---:|
| `HDW-50M-00060` | 180016 | 8 |
| `ICC-00994` | 120721 | 21 |
| `ICC-01084` | 63974 | 13 |
| `ICC-01117` | 46056 | 14 |

**Disposition:** **Confirmed at the time read.** The later same-day timeline refresh returned `ICC-01117` opening QOH of `37056` across 12 qualifying lots, which was immediately reconfirmed by the unchanged named T01 query and is the value used in the timeline. This reflects live-source movement, not a source-rule change.

### Q11-F01 Execution Result -- 2026-09-15

**Purpose, parameters, and limit:** Compare the candidate `in_mstr` fields with the locked Stage 9 physical usable and usable-hard-allocation calculation for `HDW-50M-00060` at `KTC` / `SW`. The single aggregate row used the current date and Stage 9 issue-day expiration cutoff. It retained equality/presence flags only; no quantity was retained.

**Result:** All three `in_mstr` candidate fields (`in_qty_oh`, `in_qty_avail`, and `in_qty_all`) were present. `in_qty_oh` matched physical usable quantity. `in_qty_avail` did not match physical usable less usable hard allocation. `in_qty_all` did not match usable hard allocation.

**Disposition:** The candidate comparison does not identify an `in_mstr` field that proves the accepted already-hard-allocation-netted opening-QOH semantic. `in_qty_oh` is physical usable for this sample and remains prohibited as a substitute. The accepted business rule remains unchanged.

### Q11-F02 Execution Result -- 2026-09-15

**Purpose, parameters, and limit:** Find fixed `TOP (1)` source examples for F status, E status, nonzero picked quantity, and a multi-operation WO/component at `KTC` / `SW`, then inspect source-shape flags and a point hard-allocation-existence probe.

**Result:** The command exceeded the configured SQL command timeout and returned no rows.

**Disposition:** The required targeted WO evidence is unavailable within the approved read boundary. No query predicate, command timeout, plan, index, or database setting was changed. Per the Stage 11 stopping condition, Q11-F04 through Q11-F07 were not run in this pass. They remain separately approved, fixed query definitions for a later pass after owner direction on a safely bounded retrieval approach.

### Q11-N01 Execution Result -- 2026-09-15

**Business purpose:** Reconcile the locked Stage 9 physical usable inventory and hard-allocation mapping with the accepted semantic that QAD nettable QOH is already hard-allocation-netted; KST must not subtract allocations again.

**Approved bounded scope:** QAD domain `KTC`, site `SW`, component `297255-5`, current report date and Stage 9 expiration cutoff. The parameterized, SELECT-only aggregate read joined usable `ld_det` lots through `loc_mstr`/`is_mstr` and matched `lad_det` hard allocations by domain, site, part, location, and lot. No source values, lot identifiers, allocations, credentials, or connection details were retained.

**Result:** One usable lot was found. Zero usable lots had a matching positive `lad_det` `wod_det` hard allocation; the computed free inventory was nonnegative. Therefore the supplied component did **not** provide the required known-usable-hard-allocation validation case under the locked Stage 9 predicates.

**Disposition:** The result neither contradicts nor changes the accepted business rule. It does not reconcile the Stage 9 source mapping with the accepted already-netted-QOH semantic. No broadened, replacement, or follow-up QAD query was run in this session. A separately approved replacement `Q11-N01` case must identify a component that has a hard allocation matching a lot that qualifies as usable under the locked Stage 9 predicates.

### Q11-N01 Retry Result -- 2026-09-15

**Business purpose and approved scope:** The same parameterized, SELECT-only aggregate was re-run for QAD domain `KTC`, site `SW`, component `150QC150JA`, using the same current-date Stage 9 expiration cutoff and the same physical usable-lot and `lad_det` matching predicates.

**Result:** Nine usable lots were found. Zero usable lots had a matching positive `lad_det` `wod_det` hard allocation; the computed free inventory was nonnegative. This second supplied component also does not provide the required known-usable-hard-allocation validation case under the locked Stage 9 predicates.

**Disposition:** The result neither contradicts nor changes the accepted already-netted-QOH business rule and does not reconcile the Stage 9 implementation/source mapping to that semantic. No broader discovery query was run. Any next candidate must be separately approved and should be independently identified as having a positive hard allocation that matches a lot qualifying as usable under all locked Stage 9 predicates.

### Q11-N01 Retry 2 -- Approved Query Record

**Purpose:** Re-run the approved Stage 9 physical usable-inventory and hard-allocation reconciliation aggregate for a user-supplied candidate, without changing any locked predicate or accepted opening-QOH semantic.

**Harness:** `scratch/stage11-qad-validation/` is the single reusable, untracked harness for this approved Stage 11 evidence pass. It contains only named query dispatch, parameters supplied by the command invocation, and no arbitrary-SQL input path. It stores no credentials and writes no results outside `scratch/`. It remains available for separately approved Stage 11 evidence reads during this pass and will be removed only at the approved pass closeout.

**Named query:** `Q11-N01` only. **Parameters:** domain `KTC`; site `SW`; component `3-03-3022-00-0`; runtime current date; locked Stage 9 expiration cutoff = current date + issue days. **Row limit:** one aggregate row. **Selected output:** counts/boolean reconciliation flags only; no lot, allocation quantity, supplier, user, credential, or connection data.

**Result:** Six usable lots were found. Zero usable lots had a matching positive `lad_det` `wod_det` hard allocation; the computed free inventory was nonnegative. This candidate does not provide the required known-usable-hard-allocation validation case under the locked Stage 9 predicates.

**Disposition:** The result neither contradicts nor changes the accepted already-netted-QOH business rule and does not reconcile the Stage 9 implementation/source mapping to that semantic. The reusable harness remains retained, unchanged, for any later separately approved named Stage 11 evidence reads in this pass. No broader discovery query was run.

### Q11-N01 Retry 3 Result -- 2026-09-15

**Business purpose:** Re-run the approved physical usable-inventory and hard-allocation reconciliation aggregate using a user-identified component with positive `lad_qty_all`, while retaining all locked Stage 9 predicates.

**Parameters and limit:** named query `Q11-N01`; domain `KTC`; site `SW`; component `HDW-50M-00060`; runtime current date; expiration cutoff = current date + Stage 9 issue days. One aggregate row; counts and Boolean flags only.

**Result:** Eight usable lots were found. Two usable lots had matching positive `lad_det` `wod_det` hard allocations. No usable lot was overallocated, and the computed free inventory was nonnegative.

**Disposition:** This is the first approved sample to confirm that the Stage 9 physical usable-lot mapping can match hard allocations at the same domain/site/component/location/lot grain without overallocating a usable lot. It supports the Stage 9 implementation mapping and the rule that allocations are removed exactly once from the physical free pool. It does **not** change or reopen the fixed Stage 11-A business rule that QAD nettable QOH is already hard-allocation-netted and KST must not subtract it again. The separate reconciliation to the exact QAD-returned opening-QOH source remains incomplete.

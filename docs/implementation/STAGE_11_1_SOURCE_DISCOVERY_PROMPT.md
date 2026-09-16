# KST v2 — Stage 11-A Long-Term Shortages: Source Discovery Prompt

## Mission

Perform the bounded, evidence-only source-discovery phase for **Stage 11-A — Long-Term
Shortages**. The target is a workspace-scoped report of material exposure using a site-wide,
weekly projection. This phase validates accepted rules and produces planning evidence; it does
**not** implement the report.

## Authorization and boundaries

This prompt authorizes only repository inspection, source mapping, and separately approved,
read-only, parameterized, bounded QAD evidence collection. Do not access the Shortages database,
secrets, or the desktop app. Do not change production code, existing stages, APIs, UI, exports,
DTOs, tests, database objects, indexes, or configuration. Do not run broad/unbounded queries or
use raw string-concatenated values. Do not use data-gathering results to revise an accepted rule
without recording the conflict for owner direction.

Create or update only these planning artifacts:

- `docs/implementation/KST_v2_STAGE_11_SOURCE_DISCOVERY_RESULTS.md`
- `docs/implementation/KST_v2_STAGE_11_FIELD_DISCOVERY_LEDGER.md`

No source query may run until you have documented its business purpose, bounded component/site
scope, exact fields, expected grain, and validation case. Redact all sensitive values from the
results artifact; use part numbers and non-sensitive business facts only where appropriate.

## Read authority first, in this order

1. `AGENTS.md`
2. `docs/status/CURRENT_PROJECT_STATUS.md`
3. `KST-v2-Master-Project-Checklist.md`, especially Stages 9–11
4. `docs/implementation/KST_v2_STAGE_9_CLOSEOUT.md`
5. `docs/implementation/KST_v2_STAGE_9_SOURCE_MAPPING.md`
6. `docs/implementation/KST_v2_STAGE_10_COMPONENT_ORDERS_CLOSEOUT.md`
7. `docs/implementation/KST_v2_STAGE_10_SOURCE_MAPPING.md`
8. `docs/implementation/KST_v2_STAGE_11_DISCUSSION_FRAMEWORK.md`
9. `docs/implementation/KST_v2_STAGE_11_FIELD_DISCOVERY_LEDGER.md`
10. `docs/data/qadpro2-data-map.md`

If an authority document is missing, record that exact gap. If recovered Stage 9/10 authority
conflicts with this prompt, stop and report the conflict; Stage 9 and 10 remain locked.

Legacy evidence is limited to:

- `C:\Dev\kst\features/shortage_report.py`
- the owner-supplied `C:\Dev\kst_v2\docs\reference\sp_shortageFinder.txt` text.

Legacy code may support a field or rule question. It is never authority by itself.

## Accepted Stage 11-A contract — do not redesign it

### Scope, calendar, and balance

- Report components are selected from the active workspace’s resolved parent scope and its
  current-effective BOM component population.
- For every reported component, net all eligible site-wide demand and supply exactly once. Other
  product lines/programs compete for the same inventory and receipts. Detail must retain
  workspace-parent versus other-program attribution.
- The projection is date-only, as of the report refresh date.
- It has exactly 24 Sunday-start weekly buckets, including the current week as week 1.
- Same-day events are a single weekly net; no receipt-before-demand, demand-before-receipt,
  reservation, or PO-to-WO allocation claim is made.
- `B(w) = B(previous week) + accepted supply in week w − accepted demand in week w`.

### Opening inventory and work-order demand

- Opening balance is the locked Stage 9 site-wide nettable usable-now quantity as QAD returns it.
  QAD has already removed hard allocations. It excludes non-nettable, MRB, and inspection
  inventory. KST must not subtract hard allocations from opening balance. Do not substitute raw `in_qty_oh`,
  `in_qty_avail`, or supplier-inventory workflow values.
- Include A, F, R, E, and P work orders. Validate the actual source representation of E/P; the
  earlier MPS contract may model those independently of A/F/R status.
- Exclude closed, cancelled, RMA, and `RMABOM` work.
- Work-order component demand is `max(0, required − issued − firm-allocated quantity)`.
  Since QAD has already removed hard allocations from opening nettable QOH, deduct a hard
  allocation only from its work order's remaining demand.
  A fully hard-allocated component contributes zero remaining demand.
- Bucket remaining demand by WO due date. Residual eligible demand before current week rolls into
  current week after firm allocation is removed.

### Forecast demand

- Interim accepted rule: include gross `mrp_det` `fcs_sum` demand in the Sunday-start week
  containing `mrp_due_date`.
- Exclude `fcs_sum` rows before the current week as past forecast.
- This is explicitly a potentially overstated gross forecast approximation because forecast
  consumption against sales orders is not currently modeled. Preserve this caveat in results and
  any future UI/export contract.

### Supply and KSS

- Only confirmed, Stage 10-qualifying conventional PO lines may increase projected supply:
  positive open quantity and line status other than C/X, case-insensitively.
- Bucket conventional PO supply by PO **line** due date.
- Do not count unconfirmed lines, open lines due before current week, or lines without a due date.
  Retain the first two as `Past Due PO`/`Missing PO Due` context where applicable.
- Lines due after week 24 are out of scope for 11-A; no calculation or display. Component Orders
  remains their PO surface.
- KSS is a component classification only. Use the legacy active scheduled-PO identification rule
  after validation: scheduled `pod_det` relationship plus a non-expired effective boundary. Do
  not credit KSS scheduled supply or `SUPPLYP` to projected balance.
- Planned orders, supplier-inventory workflow quantities, transfers, and production receipts are
  excluded from initial 11-A.
- QAD performs conversions; KST performs none. UOM is informational.

### Shortage, recovery, UI, and export semantics

- `Critical Short`: projected balance < 0.
- `Safety Stock Short`: projected balance >= 0 and < approved safety stock.
- Both count as shortages. Default report rows are components reaching either severity during the
  horizon; **Show All** can include otherwise clean components.
- Sort by earliest First Safety Stock Short; clean Show All rows sort last.
- Critical Clear Week is the first later week with balance >= 0. Safety Stock Recovery Week is
  the first later week with balance >= safety stock. They remain calculation/detail values, not
  compact-list columns.
- Do not claim a covering PO. Show confirmed conventional receipts in a recovery week as
  evidence only.
- Compact list: Comp, optional QAD Status, Description, KSS, Leadtime (weeks), Planner, On Hand,
  then 24 weekly balances. Text filters: Comp, Planner. Nonblank checkbox filters: KSS, Status.
- Critical Short in current week shades Comp muted red. Safety Stock Short shades Comp light
  umber. Every negative weekly balance is shaded subdued red in the future app and export.
- Selecting Comp will use the existing BOM-style Component Information card. Add Demand (distinct
  workspace parents using the component); Description and B/P already exist. Planner in the list
  is the readable name.
- A later 11-B owns single-part MRP. Do not build it in this phase.
- The 11-A export will be a filtered flat `.xlsx`: export exactly displayed rows, including Show
  All results. Filename: `<normalized-workspace-name>-Shortages-<refresh-date>.xlsx`, replacing
  each run of spaces/special characters with one hyphen.
- Required export order: Comp, Comments, QAD Status, Shortage Severity, Description,
  Manufacturer Item, Demand, KSS, PO Due, PO Qty, Confirmed, Wks LT, Planner, B/P, First Short,
  On Hand, 24 weekly balances.
- Export Comments: `NO PO PLACED` if no qualifying confirmed conventional PO exists; `PAST DUE`
  if an unreceived confirmed PO is due before current week; otherwise blank.
- Component-level export PO fields use earliest-due qualifying confirmed in-horizon PO; ties use
  PO number then line. Detail retains all in-horizon PO evidence.
- On refresh failure, preserve the previous report as stale and show its refresh date; it remains
  exportable. An unavailable initial load is not an empty result and is not exportable.

## Discovery tasks and required evidence

Document each result with source, key/grain, date meaning, quantity meaning, status lifecycle,
null behavior, and the accepted-rule it supports or challenges.

1. **Recover locked inventory mapping.** Locate and validate the Stage 9 usable-now and hard-
   allocation behavior, including firm-allocation grain and the candidate `lad_det` evidence from
   the coworker query. Confirm the exact source of site-wide returned nettable usable-now QOH;
   evidence that QAD has already removed hard allocations; and nettable, MRB, inspection, and
   issue-policy treatment. Confirm that the allocation grain used for remaining WO demand is safe.
2. **Validate WO source shape.** Establish safe `wo_mstr`/`wod_det` identity and operation
   aggregation. Validate A/F/R/E/P representation; exclusion behavior; required, issued,
   picked, and firm-allocated fields; WO due date; and duplicate/multi-operation cases.
3. **Validate workspace attribution.** Trace how the active workspace resolved parent scope and
   current-effective BOM distinguish its parent usage from site-wide other-program demand without
   double counting a component.
4. **Validate conventional PO facts.** Confirm Stage 10 line qualification/confirmation mapping,
   due-date grain, open quantity, partial/full receipt, missing date, past due, C/X/blank status,
   multiple lines/revisions, manufacturer item, and deterministic line ordering.
5. **Validate KSS classification only.** Confirm active/non-expired/expired/missing scheduled
   line examples. Do not develop scheduled receipt logic.
6. **Validate interim forecast facts.** Confirm `fcs_sum` row grain, sign, quantity, site/domain,
   `mrp_due_date`, future/past filtering, and an overlap example documenting the known gross-
   forecast caveat.
7. **Validate presentation candidates.** Map QAD Status, lead time in weeks, Planner readable
   name, B/P code, Manufacturer Item, Safety Stock, and workspace-parent Demand field. Do not
   infer precedence if source results conflict.
8. **Hand-worked timelines.** Prepare owner-reviewable weekly timelines for `ICC-00994` (expected
   Critical Short), `ICC-01084`, and `ICC-01117`, plus one component shared with another site
   product line/program. Include opening balance, WO demand, forecast demand, PO receipts, weekly
   balances, safety stock, severity, and recovery measures. Mark any missing evidence as unknown;
   do not invent values.

## Required results document

`KST_v2_STAGE_11_SOURCE_DISCOVERY_RESULTS.md` must contain:

1. authority/repository reconciliation and missing-document inventory;
2. a rule-by-rule evidence matrix: confirmed, contradicted, unavailable, or needs owner decision;
3. source-grain and join findings for every included fact class;
4. approved bounded query/read inventory, with actual result summaries but no credentials;
5. the hand-worked validation packet and unresolved data cases;
6. an explicit list of rules that are still interim, especially gross `fcs_sum` forecast;
7. a revised field ledger; and
8. a go/no-go recommendation for a separate implementation-planning session.

## Required stopping conditions

Stop and report rather than making a product or source assumption if:

- Stage 9 usable-now/hard-allocation mapping, including QAD's already-netted QOH treatment, cannot be recovered;
- E/P work-order representation is inconsistent with the accepted inclusion rule;
- forecast rows cannot be safely scoped by site/component/date;
- KSS classification cannot be mapped without guessing;
- quantities or field meanings conflict with the accepted contract; or
- a required database permission/read boundary is unavailable.

## Completion response

Return only:

1. authority reconciled and planning files changed;
2. evidence obtained and exact unresolved items;
3. hand-worked validation status for each sample component;
4. confirmation that no production code, desktop app, secrets, Shortages data, or schema/index
   work was touched; and
5. whether Stage 11-A is ready for a separate implementation-planning prompt.

# KST v2 -- Stage 11-A Long-Term Shortages Production-Build Prompt

Implement only the owner-approved Stage 11-A Long-Term Shortages plan in `docs/implementation/KST_v2_STAGE_11A_IMPLEMENTATION_PLAN.md`.

## Mandatory authority and boundaries

Read first, in order: `AGENTS.md`, `docs/status/CURRENT_PROJECT_STATUS.md`, `KST-v2-Master-Project-Checklist.md` Stages 9-11, Stage 9/10 closeouts and source mappings, the Stage 11 discussion framework, source discovery results, field discovery ledger, and the Stage 11-A implementation plan.

Stage 9 and Stage 10 are COMPLETE / ACCEPTED / LOCKED. Do not change their business rules, source readers, API contracts, UI behavior, tests, or exports. Do not implement Stage 11-B Single-Part MRP. Do not add dependencies, database objects, indexes, configuration, secrets, or Shortages-database behavior. All QAD access remains read-only, parameterized, domain/site bounded, cancellation-aware, and inside `Kst.Integrations.Qad`.

Follow C# DTO -> OpenAPI -> generated TypeScript -> frontend client. Never manually edit `src/frontend/src/generated/api.ts`.

## Required delivery

Add a distinct `Long-Term Shortages` workspace module. It is a component-level, site-wide, 24-week projection for components selected from the active workspace's resolved parents and current-effective BOM. Do not replace the locked Stage 9 Shortages module.

Use `MpsBusinessCalendar`: Week 1 begins on the Sunday containing the date-only refresh date; return exactly 24 Sunday-start weeks through the Week 25 exclusive boundary. Same-week supply and demand are one net. For each week: `prior balance + accepted supply - accepted demand`.

Implement one balance per domain/site/component, not per workspace parent. Preserve workspace-parent BOM relationships and WO-parent attribution as evidence only. All eligible site-wide demand and supply net once. Do not derive BOM demand in addition to WOD demand.

Implement the approved opening-QOH rule in a dedicated Stage 11-A reader: `COALESCE(SUM(ld_det.ld_qty_oh), 0)` at domain/site/part, excluding `ld_status` MRB/INSPECT/NCMINSP and lot prefixes RMA/RA. Include Transit. Intentionally let null status/lot fail the SQL predicates. Do not add positive-quantity, date, expiry, `loc_mstr`, `is_mstr`, `lad_det`, or `in_mstr` conditions/joins. Never subtract hard allocations from Stage 11-A opening QOH.

Implement eligible demand from A/F/R/E/P WOs only, excluding `wo_status='C'` and `wo_bom_code='RMABOM'`. At distinct `(WOID, component, wod_op)` grain calculate `max(0, wod_qty_req - wod_qty_iss - own usable firm allocation)`. Preserve WOID, component, and operation in all joins. Ignore `wod_qty_pick` and `wod_qty_all`. Bucket by WO due date; roll past-due eligible residual demand into Week 1. Include gross future `mrp_det.fcs_sum` forecast by due date, excluding past forecast, and label it as an interim potentially overstated rule because forecast consumption is not modeled.

Implement accepted supply only from confirmed, non-scheduled, Stage 10-qualifying conventional PO lines: positive raw open quantity, `pod_status` other than C/X case-insensitively, line confirmation, and due date within the 24-week horizon. Do not use `po_stat`. Do not supply from unconfirmed, past-due, missing-due, after-horizon, KSS/scheduled, planned, supplier-inventory, transfer, or production-receipt rows. KSS uses the established active effective scheduled-PO classification and is display-only.

Severity is Critical Short when balance `< 0`, Safety Stock Short when balance is `>= 0` and `<` approved safety stock. Both are shortages. Selected-site non-null `ptp_det.ptp_sfty_stk`, including zero, wins; only selected-site-row absence falls back to `pt_mstr.pt_sfty_stk`; a present selected-site null is explicitly unresolved and must not become zero or fall back.

Default to components reaching either shortage; Show All includes clear components. Sort by first Safety Stock Short with clean Show All rows last. Compact columns: Comp, optional QAD Status, Description, KSS, Leadtime weeks, Planner, On Hand, then 24 weekly balances. Add Comp/Planner text filters and nonblank KSS/Status filters. Current-week Critical Short Comp is muted red, current-week Safety Stock Short Comp light umber, and every negative balance subdued red, with non-color text/programmatic cues.

Open component detail using the established blocking Component Information modal interaction pattern. Preserve existing card behavior and add Stage 11-A evidence content: Description, B/P, distinct workspace-parent Demand, 24-week timeline, demand/supply evidence, workspace versus other-program attribution, and in-horizon PO context. Do not claim a PO covers a particular WO.

Provide stale-last-good behavior keyed by workspace, MPS snapshot, and refresh date. On failure, retain a compatible successful result, visibly mark it stale with its refresh date, and permit stale export. Initial unavailable is not empty and cannot export. A superseding MPS snapshot invalidates the prior result.

Export only currently displayed filtered rows to `<normalized-workspace-name>-Shortages-<refresh-date>.xlsx`. Column order: Comp, Comments, QAD Status, Shortage Severity, Description, Manufacturer Item, Demand, KSS, PO Due, PO Qty, Confirmed, Wks LT, Planner, B/P, First Short, On Hand, 24 weekly balances. Style negative balance cells subdued red. Comments: `NO PO PLACED` with no qualifying confirmed conventional PO; `PAST DUE` with an unreceived confirmed PO before Week 1; otherwise blank. Component-level PO fields use the earliest qualifying confirmed in-horizon line, ties by PO then line. The export uses the loaded filtered projection and never re-queries.

## Required tests and verification

Add focused domain, application, QAD-query-shape, API integration, frontend, modal accessibility, and workbook tests described in the implementation plan. Include deterministic approved timeline fixtures, with no live QAD dependency:

- `ICC-00994`: Safety Stock Short Week 16, Critical Short Week 17, no recovery through Week 24.
- `ICC-01084`, `ICC-01117`, and `115989`: no shortage through Week 24.
- `115989`: prove one site-wide balance and retained workspace/other-program attribution.

Run the repository's normal backend format/build/test, frontend lint/typecheck/test/build, OpenAPI and TypeScript generation, relevant export tests, `cargo check`, and documented sidecar rebuild when backend changes. Run `git diff --check`. Do not claim manual desktop validation without performing it; provide concise manual validation steps instead.

Update durable status/checklist/API documentation only where implementation establishes accepted Stage 11-A behavior. Preserve unrelated worktree changes. Before final response, inspect the complete diff and report tests run, known limitations, and the exact files changed.

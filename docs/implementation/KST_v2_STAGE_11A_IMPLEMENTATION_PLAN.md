# KST v2 -- Stage 11-A Long-Term Shortages Implementation Plan

**Status:** Implemented / owner review pending (2026-09-16). Backend, generated contract, frontend typecheck/build, and Tauri verification passed; repository-wide frontend test/lint baseline failures in locked Stage 10 files remain outside this scope.

## 1. Authority, scope, and fixed boundaries

This plan implements only the owner-approved Stage 11-A Long-Term Shortages workspace tab. It is governed by `AGENTS.md`, `docs/status/CURRENT_PROJECT_STATUS.md`, `KST-v2-Master-Project-Checklist.md`, the locked Stage 9 and Stage 10 closeouts/source mappings, and the approved Stage 11 discovery packet:

- `docs/implementation/KST_v2_STAGE_11_DISCUSSION_FRAMEWORK.md`
- `docs/implementation/KST_v2_STAGE_11_SOURCE_DISCOVERY_RESULTS.md`
- `docs/implementation/KST_v2_STAGE_11_FIELD_DISCOVERY_LEDGER.md`

Stage 9 and Stage 10 remain complete, accepted, and locked. Stage 11-A must add a distinct `Long-Term Shortages` workspace module; it must not replace, alter, reinterpret, or share mutable state with the locked `Shortages` or `Component Orders` surfaces. Stage 11-B Single-Part MRP is out of scope.

The projection uses a date-only refresh snapshot and exactly 24 Sunday-start business weeks. Week 1 starts on `MpsBusinessCalendar.GetBusinessWeekStart(refreshDate)` and Week 24 ends at the following Week 25 start. For each component and week:

```text
balance(week) = prior balance + accepted supply - accepted demand
```

All same-day events are a single weekly net. No daily event order, covering-PO assertion, reservation, UOM conversion, planned-order supply, scheduled/KSS supply, transfer, production-receipt, supplier-inventory, or Shortages-database behavior is introduced.

## 2. Verified implementation seams

The implementation should follow the existing vertical-slice structure, creating a focused `LongTermShortages` namespace rather than expanding the locked Stage 9 `Shortages` namespace.

| Layer | Existing seam to reuse | Planned Stage 11-A addition |
|---|---|---|
| Domain | `Kst.Domain.Mps.MpsBusinessCalendar`; `Kst.Domain.ComponentOrders.ComponentOrdersBuilder` | Pure projection records and `LongTermShortagesBuilder` for 24-week netting, severity, first-short calculations, deterministic display ordering, and export row composition. |
| Application | `Kst.Application.ComponentOrders.ComponentOrdersService`, `IComponentOrdersCacheStore`; `Kst.Application.Bom.IBomSourceReader`; `Kst.Application.Mps.IMpsSnapshotStore` | `LongTermShortagesService`, result/outcome records, focused source-reader interfaces, and a snapshot-aware in-memory cache contract. |
| Infrastructure | `Kst.Infrastructure.ComponentOrders.InMemoryComponentOrdersCacheStore` | `InMemoryLongTermShortagesCacheStore`, keyed by workspace, MPS snapshot, and refresh business date. |
| QAD | `Kst.Integrations.Qad.Bom.QadBomReader`; `ComponentOrders.QadComponentOrderReader`; `Shortages.QadHardAllocationReader` | Dedicated batchable Stage 11-A readers. Do not modify Stage 9 or Stage 10 readers to force a different semantic. |
| API | `Kst.Api.Endpoints.ComponentOrderEndpoints`, `Kst.Api.Dtos.ComponentOrderDtos`, `Program.cs` registrations | `LongTermShortagesEndpoints` and DTOs, registered in `Program.cs`; regenerate OpenAPI and TypeScript through the canonical pipeline. |
| Frontend | `CustomerWorkspace.tsx`; `ComponentOrdersPanel.tsx`; `ComponentInfoModal.tsx`; `useComponentOrders.ts`; `componentOrdersPresentation.ts` | `LongTermShortagesPanel.tsx`, CSS, hook/API/presentation helpers, and a Stage 11-A detail modal or a deliberate extension point in the existing component-information modal. |
| Export | `Kst.Exports.Contracts.IExportService` and `PlaceholderExportService` are placeholders only | First real export implementation in `Kst.Exports`, with a narrow Stage 11-A export contract and no generic export framework. |

`CustomerWorkspace.tsx` currently owns the `CustomerModule` union and module list. Add `longTermShortages` there, rendering the new panel with `workspace.assignmentId` and the current MPS `snapshotId`.

`ComponentOrdersService.ResolveComponentPartsAsync` is the confirmed population pattern: read current-effective BOM occurrences once per resolved parent, retain parent association before component deduplication, then form the distinct component request scope. Stage 11-A must preserve that distinction instead of reusing only the Stage 10 deduplicated list.

## 3. Backend model and request contract

### 3.1 Result identity, freshness, and outcomes

The report identity is `(workspaceId, mpsSnapshotId, refreshDate, includeManufacturedParts, includePhantoms)`. `refreshDate` is supplied by the API from `IClock.LocalNow.Date`; all query date parameters derive from it. The MPS snapshot provides resolved workspace parents and validates that the request is not silently combined with a superseding workspace scope. A cached projection, including a stale result and export lookup, is compatible only with the exact population-option values that produced it.

The GET request must require `snapshotId`, matching `ComponentOrderEndpoints`. Proposed route:

```text
GET /api/v1/workspaces/{assignmentId}/long-term-shortages?snapshotId={guid}
```

`includeManufacturedParts` and `includePhantoms` are optional Boolean query parameters, both defaulting to `false`. They are report-population options, not frontend display filters. After the resolved-parent, current-effective-BOM occurrences have established the component set and parent attribution, a component enters the site-wide projection only when each applicable classification is enabled: normal components are included by default; components with the established `pt_mstr.pt_pm_code = 'M'` manufactured indicator require `includeManufacturedParts`; components with the established `pt_mstr.pt_phantom` indicator require `includePhantoms`; a component with both classifications requires both options. Excluded components are removed before Stage 11-A opening-QOH, demand, supply, forecast, KSS, presentation, or projection retrieval/calculation.

Return the same outcome classes as the established workspace-detail capabilities:

- `200`: loaded result, including `isStale`, `refreshDate`, 24 week descriptors, and rows.
- `400`: missing or invalid `snapshotId`.
- `404`: workspace absent.
- `409`: MPS not loaded or supplied snapshot superseded.
- `503`: no prior successful result and live retrieval failed.

On successful refresh, atomically replace the cache entry for that identity. On a failed refresh, return the latest compatible cached result for the same workspace and snapshot as `isStale: true`, retain its original refresh date, include an explicit warning, and make it exportable. An initial failure remains unavailable and has no rows to export. A new MPS snapshot makes older report data incompatible; it must not be served as stale.

Proposed normalized response types:

```text
LongTermShortagesResponse
  snapshotId, refreshDate, isStale, warning
  weeks[24]: weekNumber, weekStart
  rows[]: component-level compact-list rows

LongTermShortageRow
  componentPart, qadStatus, description, isKss, leadTimeWeeks, planner
  openingQoh, safetyStockState, safetyStock
  severity, firstSafetyStockShortWeek, firstCriticalShortWeek
  demandParentParts, balances[24]
  hasQualifyingConfirmedConventionalPo, hasPastDueConfirmedPo
  earliestQualifyingConfirmedPo (optional PO context)

LongTermShortageDetail
  row summary and 24-week timeline
  weekly demand/supply evidence
  workspace-parent attribution and other-program attribution
  in-horizon PO context, including non-supplying context states
```

Use a safety-stock state rather than representing selected-site null as `0` or silently applying master fallback. A suggested wire enum is `resolved` and `selectedSiteValueMissing`; components with the latter remain loaded data with an explicit unresolved-data presentation and no invented severity threshold. The build slice must select the final UI treatment before merging; it cannot render a false clean result.

### 3.2 Source readers and aggregation sequence

Use separate, testable interfaces/readers for source facts. Application contracts carry scheduling concepts, never QAD table-shaped rows.

1. Validate workspace and requested MPS snapshot.
2. Derive `refreshDate`, `weekOneStart`, and exclusive `horizonEnd = weekOneStart + 24 weeks` through `MpsBusinessCalendar`.
3. Read current-effective BOM occurrences for every resolved parent, preserving `(resolvedParentPart, occurrence)` attribution. Derive a case-insensitive distinct component scope only after retaining the parent map.
4. Batch-read component presentation and safety-stock facts at domain/site/part grain. Resolve site safety stock as: non-null selected-site `ptp_det.ptp_sfty_stk` including zero; master `pt_mstr.pt_sfty_stk` only if no selected-site row; a present selected-site null as an explicit unresolved state. Resolve planner site-first/master-second with the Stage 10 domain-scoped `code_mstr` discriminators. Derive informational lead-time weeks from the established Stage 10 purchase-lead mapping; preserve a null display state.
5. Batch-read direct Stage 11-A opening QOH for every scoped component. This dedicated reader must not call or adapt `QadInventoryPositionReader`: its Stage 9 positive/Stock/nettable/expiration/location semantics are intentionally different.
6. Batch-read eligible WO component demand at the explicit `(WOID, component, wod_op)` grain, including the WO parent part needed for attribution and header due date. Batch-read or join only the owning usable firm allocation at `(WOID, component, operation)` grain. Calculate each row's `max(0, required - issued - ownUsableFirmAllocation)` before grouping; roll past-due eligible demand into Week 1, group in-horizon demand by due-date week, and exclude future demand at or after `horizonEnd`.
7. Batch-read in-horizon, future-dated gross `mrp_det.fcs_sum` forecast demand. Do not roll past forecast forward. Keep forecast evidence distinct from WO demand even when the final week total is combined.
8. Batch-read all relevant conventional PO lines plus independent effective KSS classification for the scoped components. Only confirmed, non-scheduled, Stage 10-qualifying lines with due date in `[weekOneStart, horizonEnd)` contribute their positive raw open quantity as supply. Retain unconfirmed, past-due, missing-due, and in-horizon non-supplying lines as context only where required. Exclude after-horizon lines from this report entirely.
9. Build one site-wide projection per distinct component. The builder aggregates weekly accepted demand as WO residual plus forecast and accepted supply as confirmed conventional PO open quantity, then calculates balances sequentially for exactly 24 weeks.
10. Classify each balance: Critical Short if `< 0`; Safety Stock Short if `>= 0 && < resolved safety stock`; otherwise clear. A component's list severity is its highest reached severity, with Critical outranking Safety Stock. `firstSafetyStockShortWeek` is the first week below safety stock; for unresolved safety stock it is unknown, not a fabricated clean value.
11. Filter default output to components reaching either shortage severity; Show All is frontend-local filtering over the fully returned loaded set. Sort by first Safety Stock Short ascending; rows with no shortage last, then deterministic component-part order.

### 3.3 Parameterized QAD query shapes

Every reader resolves domain through `QadSiteDomainMap.Resolve(site)`, uses `QadConnectionFactory`, `QadConnectionOptions.CommandTimeoutSeconds`, `CommandDefinition`, cancellation tokens, and bounded `MpsPartBatcher`-style `VALUES` component parameters. No database object, dynamic pivot, raw value interpolation, or source write is permitted.

**Opening QOH:** `ScopeParts` values CTE, left joined to an `ld_det` aggregate grouped by `ld_domain`, `ld_site`, `ld_part`; predicate `ld_domain=@Domain`, `ld_site=@Site`, scope part, `UPPER(ld_status) NOT IN ('MRB','INSPECT','NCMINSP')`, `UPPER(ld_lot) NOT LIKE 'RMA%'`, and `UPPER(ld_lot) NOT LIKE 'RA%'`. Return `COALESCE(SUM(ld_qty_oh),0)`. Do not add positive quantity, date/expiry, `loc_mstr`, `is_mstr`, `lad_det`, or `in_mstr` predicates/joins. SQL null behavior is intentional.

**WO demand:** join `wo_mstr` to `wod_det` by domain and WOID (`wo_lot = wod_lot`), restricted by domain/site and scoped component. Filter `wo_status IN ('A','F','R','E','P')` and `ISNULL(wo_bom_code,'') <> 'RMABOM'`. Aggregate only allocation rows matching `lad_dataset='wod_det'`, domain, site, `lad_nbr=wod_lot`, `lad_line=wod_op`, `lad_part=wod_part`, and the accepted usable firm-allocation predicate. The SQL may return source rows and firm-allocation aggregate, but the residual calculation belongs in the domain builder. It must retain WOID, component, operation, parent, due date, required, issued, and allocation fields until row-level residual calculation is complete.

**Forecast:** scope `mrp_det` to domain/site/component, the accepted `fcs_sum` dataset representation, and `[weekOneStart,horizonEnd)`. Return stable source identity fields available from the accepted evidence (`mrp_nbr`, `mrp_line`) with due date and quantity; do not deduplicate without a documented source-key reason. Gross future forecast remains a separately labeled input.

**Conventional PO/KSS:** keep Stage 10's PO identity and qualification semantics: `pod_det` joined to `po_mstr` by domain + PO number; positive `pod_qty_ord-pod_qty_rcvd`; `LOWER(ISNULL(pod_status,'')) NOT IN ('c','x')`; line confirmation from `pod__log01`; no `po_stat` predicate. Add the report-only due/horizon and confirmed/non-scheduled supply predicates without changing the Stage 10 reader. KSS is the existing independent effective `pod_det`/`po_mstr` relationship and is classification only. Order selected qualifying PO context by due date, PO number, then PO line.

## 4. Identity and no-double-counting rules

| Fact | Required identity/grain | Rule |
|---|---|---|
| Report component | Domain + Site + component part | One projection and one opening QOH per reported component, no matter how many workspace parents use it. |
| BOM population | MPS snapshot + resolved parent + BOM occurrence | Preserve occurrences/parents for attribution. Form a separate case-insensitive distinct component set only for site-wide source reads. |
| WO demand | Domain + Site + WOID + component + `wod_op` | Preserve all keys across joins. Each component-operation row is a separate explicit requirement; compute the residual once, then sum it into its component/week. |
| Firm allocation | Domain + Site + WOID + component + operation + location + lot | Aggregate only an owning WOD row's usable firm allocations. Subtract only from that row's demand residual. Do not add `wod_qty_all`. |
| Opening QOH | Domain + Site + component | Use the approved direct `ld_det` aggregate once. Do not subtract hard allocation from opening QOH. |
| Forecast | Source row identity plus domain/site/component/due date | Add gross `fcs_sum` only once per source fact. Do not derive or subtract BOM demand for the same component requirement. |
| PO supply | Domain + PO revision/number + line plus component | A qualifying line contributes its raw positive open quantity once to its due-date week. It is not assigned to an individual WO. |
| Attribution | Component event plus WO parent membership relative to resolved parent set | Classify a demand row as workspace-parent or other-program detail after it has been admitted once into the component's site-wide balance. Attribution must never create a second balance input. |

Case-insensitive QAD storage requires case-insensitive component identity handling in C# matching the existing Component Orders approach. Preserve raw returned display/identity values for output; do not introduce broad trimming or cross-domain matching rules.

## 5. Frontend, detail, export, and accessibility

### 5.4 Owner-authorized UOM-specific quantity display and export rounding (2026-09-16)

Keep Stage 11-A source values, calculations, comparisons, and severity classifications at full QAD precision. Apply the shared Stage 11-A presentation policy only to all displayed/exported part quantities: trimmed case-insensitive `EA`/`EACH` quantities round to the nearest whole number with midpoints away from zero; every other UOM floors to four decimal places. This includes opening QOH/on hand, safety stock, weekly balances, weekly demand/supply evidence, PO quantity, and detail quantities. The API supplies component UOM only as a presentation field. Negative raw balances retain shortage styling even where EA/EACH display rounding yields zero, and accessible labels expose both raw and displayed values. Workbook quantity cells remain numeric rounded values.

### 5.1 List and states

`LongTermShortagesPanel` should load only with a current MPS snapshot ID. It displays a neutral load-MPS-first state without calling the API when no snapshot exists, a loading state, an initial unavailable state with Retry, and loaded results. When a loaded result is stale, keep rows visible, announce the stale warning with `role="alert"`, show the retained refresh date, and enable export. The top-of-page Include Manufactured Parts and Include Phantoms controls are both initially unchecked and reload the report under their new population identity; they remain distinct from the local Comp, Planner, KSS, Status, and Show All display filters.

The compact, horizontally scrollable table contains, in order: Comp, optional QAD Status, Description, KSS, Leadtime (weeks), Planner, On Hand, and 24 weekly balance columns. The list has Comp and Planner text filters, nonblank KSS and Status checkbox filters, and a Show All toggle. Filters are applied client-side to the loaded response; export receives exactly the current filtered component set, including Show All behavior, not merely the visible virtual viewport.

Rows sort by first Safety Stock Short, then component part. Rows which remain clear sort last only when Show All is enabled. Current-week Critical Short shades the Comp cell muted red. Current-week Safety Stock Short shades it light umber. Every negative balance cell has subdued-red styling. Color is supplemental: visible text/labels and programmatic row/cell context must convey severity.

### 5.2 Detail card

Selecting a component opens the existing blocking Component Information modal interaction pattern: focus moves to the close button, Escape closes only the modal, Tab is trapped, the backdrop does not close it, and focus returns to the originating list control. To preserve the Stage 8 card, prefer a Stage 11-A detail content section/composable child rather than changing its established data meaning.

The Stage 11-A detail content includes Description and B/P, distinct workspace-parent Demand, summary plus the 24-week balance timeline, demand/supply evidence, workspace-parent versus other-program attribution, and in-horizon PO context. It must label KSS as classification-only and show gross forecast as an interim potentially overstated input. PO entries are evidence, not a promise that a line covers an individual WO or clears the shortage.

### 5.3 Export

The Export button is disabled for no initial successful result and enabled for fresh or stale loaded data. It sends the active population-option values with the currently filtered component set and exports exactly those rows from the compatible cached projection to:

```text
<normalized-workspace-name>-Shortages-<refresh-date>.xlsx
```

Normalize runs of spaces/special characters to one hyphen. The flat sheet columns, in fixed order, are: Comp, Comments, QAD Status, Shortage Severity, Description, Manufacturer Item, Demand, KSS, PO Due, PO Qty, Confirmed, Wks LT, Planner, B/P, First Short, On Hand, and Week 1 through Week 24 balances. Apply subdued-red formatting to every negative weekly balance.

Comments are `NO PO PLACED` when no qualifying confirmed conventional PO exists, `PAST DUE` when an unreceived confirmed PO is due before Week 1, otherwise blank. Component-level PO columns use the earliest-due qualifying confirmed in-horizon conventional line, with PO number then PO line ties. The export service must receive the already filtered, immutable projection result and must not re-query or recompute it.

## 6. Implementation slices

1. **Domain and application contracts:** Add pure 24-week calendar/projection/severity builder tests, new application result/source/cache contracts, and a cache implementation. No API/UI yet.
2. **QAD source boundary:** Add dedicated batch readers and SQL-shape tests for Stage 11-A opening QOH, presentation/safety-stock, WO residual inputs, forecast, PO context/supply, and KSS classification. Register adapters using the existing configured/not-configured DI pattern. Do not modify locked readers.
3. **Orchestration and API contract:** Implement snapshot-aware service, stale-last-good rules, endpoint/DTO mapping, API integration tests, OpenAPI generation, and generated TypeScript regeneration.
4. **Long-Term Shortages workspace UI:** Add the new module tab, typed API client/hook, filters, 24-week table, severity presentation, loading/unavailable/stale states, and focused frontend tests.
5. **Detail and export:** Add Stage 11-A component detail evidence presentation and focused `.xlsx` export implementation/tests. Run full regression and provide manual desktop validation steps. Do not begin Stage 11-B.

Each slice should be separately reviewable. Do not combine unrelated cleanup, Stage 9/10 changes, schema/index work, or security/configuration changes.

## 7. Test plan

### Domain and application tests

- Sunday-start Week 1 and exclusive Week 25 boundary; exactly 24 balances; same-week supply/demand net behavior.
- Direct opening-QOH values include negative/zero/Transit lots; exclude MRB/INSPECT/NCMINSP and RMA/RA prefixes; exclude null status/lot under SQL semantics; prove no Stage 9 inventory predicate leaks in.
- WO residual at `(WOID, part, operation)`: required minus issued minus own usable firm allocation, clamped at zero; picked and `wod_qty_all` ignored; multiple operations additive; duplicate join rows rejected by identity-preserving query shape.
- Closed and RMABOM WOs excluded; A/F/R/E/P admitted; past-due residual rolls only into Week 1; post-horizon demand excluded.
- Forecast future `fcs_sum` admitted once, past forecast excluded, and explicit interim-caveat state retained.
- Confirmed conventional in-horizon positive open PO supplies once; unconfirmed, scheduled/KSS, past-due, missing-due, C/X, and post-horizon rows do not supply; KSS still classifies the component.
- Site-wide shared-component test: workspace and other-program demand both affect one balance; parent attribution does not multiply QOH, PO supply, or demand.
- Safety-stock precedence: selected-site non-null including zero wins; absent selected-site uses master; selected-site null returns unresolved rather than zero/fallback. Test both severity thresholds and first-short ordering.
- Stale cache behavior: failed refresh returns compatible prior result with original refresh date; initial failure unavailable/no export; new MPS snapshot invalidates prior result.

### QAD, API, and contract tests

- Parameterization/batching/domain/site predicates for every new reader; assert no raw component values in SQL and no write verbs.
- Assert exact opening-QOH predicate exclusions and intentional absence of `loc_mstr`, `is_mstr`, `lad_det`, `in_mstr`, expiry, and positive-quantity logic.
- Assert WO joins include WOID/component/operation and allocation joins include `lad_nbr`, `lad_line`, and `lad_part`; assert `wod_qty_pick` and `wod_qty_all` are absent from calculations.
- Assert PO query preserves Stage 10 line qualification and omits `po_stat`, while applying Stage 11-A confirmation, non-scheduled, and 24-week timing rules.
- Endpoint tests for 400/404/409/503, loaded shape, stale loaded response, snapshot mismatch, and DTO/OpenAPI/generated-client synchronization.

### UI and export tests

- New module navigation leaves Dashboard, Stage 9 Shortages, and Component Orders behavior unchanged.
- Load-first, loading, initial unavailable/retry, fresh, stale, empty-shortage default, and Show All states.
- Comp/Planner text filters, KSS/Status filters, first-safety-short ordering, clear-row-last ordering, and export scope exactly matching filtered rows.
- Critical/Safety Stock Comp-cell treatment and negative-balance cells have labels/classes in addition to color.
- Keyboard-opened detail modal has correct dialog label, focus trap, Escape ownership, and focus restoration; evidence sections distinguish source categories and attribution.
- Workbook headers/order, normalized file name, fresh/stale export enablement, comments precedence, earliest qualifying PO selection/ties, and negative weekly cell style.

### Approved timeline regression fixtures

Create deterministic fixture data from the owner-approved 2026-09-15 KTC/SW snapshot timelines; do not query QAD during automated tests. Assert:

- `ICC-00994`: Safety Stock Short in Week 16, Critical Short in Week 17, and no recovery through Week 24.
- `ICC-01084`: no Safety Stock Short or Critical Short through Week 24.
- `ICC-01117`: no Safety Stock Short or Critical Short through Week 24.
- `115989`: one site-wide balance, workspace versus other-program attribution retained, and no shortage through Week 24.

## 8. Remaining limitations and build gate

- Gross future `mrp_det.fcs_sum` forecast is an approved interim rule and can overstate demand because forecast consumption is not modeled. It must stay visibly caveated and must not be silently promoted to final net demand.
- A selected-site `ptp_det` row with null `ptp_sfty_stk` is an unresolved source-data state. It must not fall back to master, become zero, or silently appear clean; the production build must make its list/detail/export treatment explicit.
- Unexpected PO status remains an unobserved source shape. Retain the approved C/X-only exclusion and do not infer new lifecycle predicates.
- Stage 11-A does not identify a covering PO, reserve supply to a WO, infer parent/subassembly pegging, apply UOM conversion, consume forecasts, or add planned/scheduled/transfer/production/supplier-inventory supply.
- Stage 10 `PERF-001` remains deferred. Do not tune query batching/indexes without the separate shared baseline and DBA-reviewed evidence required by its closeout.

**Go/no-go:** Go. The approved discovery timelines and owner decisions provide sufficient authority for a separate, bounded production-build prompt, provided implementation preserves the limitations above and treats selected-site-null safety stock as an explicit unresolved state.

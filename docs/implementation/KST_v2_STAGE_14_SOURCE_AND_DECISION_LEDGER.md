# Stage 14 — Planning Workbook: Source and Decision Ledger

**Status: Checkpoint 14.1 — COMPLETE / OWNER-ACCEPTED — 2026-10-02** (accepted with one amendment:
forecast quarter sums are not required; see §12). **Checkpoint 14.2 — COMPLETE / OWNER-ACCEPTED —
2026-10-02** (§13). **Checkpoint 14.3 — COMPLETE / OWNER-ACCEPTED — 2026-10-02** (§14).
**Checkpoint 14.4 — OWNER-ACCEPTED with zero-value display amendment — 2026-10-02** (§15).
**Checkpoint 14.5 — OWNER-ACCEPTED with Master plan redesign and presentation amendments —
2026-10-06** (authorized 2026-10-02; §16, §17). The acceptance covers the Planning adjustment
staging and saved drafts, the Master plan redesign, the review corrections and the four
presentation amendments (row tint, balance line, hover crosshair, content-sized columns). Owner
review is recorded separately from the automated, mocked/offline evidence in §16–§17; the
live-source (E1–E5), desktop/native-confirmation and 14.6 decisions stay open.
This ledger records the owner's Stage 14 business baseline and the six decisions given on
2026-10-02, the repository evidence they rest on, proposed technical choices, and the remaining
unverified source evidence. Checkpoint 14.6 requires separate authorization. No live QAD query was run for Stage 14. The owner's decisions do **not** establish
that any live source check passed. The application version advanced to `0.1.0-alpha.14` at
Checkpoint 14.2 (owner decision 2026-10-02, §10).

Synthetic fixtures: `docs/reference/STAGE_14_PLANNING_WORKBOOK_DETERMINISTIC_FIXTURES.json` and
`docs/reference/Stage14Fixtures/MpsAdjustments.expected.csv`.

## 0. Labels

| Label | Meaning |
|---|---|
| **OC** | Owner-confirmed — Stage 14 planning request and owner decisions, 2026-10-02. |
| **RV** | Repository-verified in `C:\Dev\kst_v2` at `686a031`. *RV-legacy* = read in the legacy reference repository `C:\Dev\kst` (Tier 5 evidence; adopted only where the owner directed). |
| **OC-14.1** | Proposed at Checkpoint 14.1 and accepted by the owner on 2026-10-02. |
| **P** | Proposed — still to be decided at the named later checkpoint. |
| **U** | Unresolved / unverified. |

## 1. Scope decisions

| # | Decision | Label | Evidence |
|---|---|---|---|
| 1.1 | Workspace-scoped Planning Workbook in the existing `planning` module placeholder. | OC / RV | `src/frontend/src/components/CustomerWorkspace.tsx:10-26` |
| 1.2 | Sales price and revenue only. "Unit cost" in the earlier checklist was a terminology error; no cost or margin. | OC | — |
| 1.3 | No frozen or lead-time fence; adjustments are not rejected because work orders are released. External QAD Mass Updater reports released-WO restrictions. | OC | — |
| 1.4 | Negative projected inventory and SO/MPS mismatches are displayed and never block export. | OC | — |
| 1.5 | Fiscal roadmap excluded (legacy roadmap rows are not carried forward). Customer planned-order import deferred. | OC / RV-legacy | `C:\Dev\kst\features\master_plan.py:900-911` |
| 1.6 | Stage 13 Customer Open Orders is reused; its behavior is unchanged. The legacy Open Orders detail sheet and sales-order date export are not rebuilt. | OC / RV | `docs/implementation/KST_v2_STAGE_13_CLOSEOUT.md` |
| 1.7 | Fresh QAD rereads before export are **out of Stage 14**; export validates against the workbook's loaded MPS data. May be reconsidered in a future version. | OC (decision 6) | — |
| 1.8 | Prototype demand `max(SO, Forecast)` is not carried forward. | OC / RV | `docs/reference/prototype/KST Scheduler Console.dc.html:1520-1525` |

## 2. Sources and grain

### 2.1 MPS supply — reuse, no new SQL

| Item | Detail | Label | Evidence |
|---|---|---|---|
| Source | `mrp_det` ⋈ `wo_mstr`; `mrp_dataset='wo_mstr'`; `mrp_type IN (supply, supplyf, supplyp)`; `wo_status <> 'C'`; `wo_bom_code <> 'RMABOM'`; parameterized domain/site/parts; no date filter. | RV | `src/backend/Kst.Integrations.Qad/Mps/QadMpsSourceReader.cs:93-132` |
| Grain | One `MpsSourceRow` per supply fact (part, due date, release date, quantity, supply type, WO id, WO state). | RV | `Kst.Domain/Mps/MpsSourceRow.cs` |
| Retention | Per-workspace `MpsSnapshot` (snapshot ID, resolved parts, all source rows); local re-projection. | RV | `Kst.Application/Mps/MpsSnapshot.cs` |
| Workbook quantity | Existing MPS per part/week = Due-Date projection of retained rows (`MpsScheduleBuilder`, `MpsDateBasis.DueDate`), independent of the dashboard's Due/Release toggle. | OC (Due Date) / P (reuse) | `Kst.Domain/Mps/MpsScheduleBuilder.cs` |
| No double count | No separate `wo_mstr` open-quantity read and no MRP "Repetitive Schedule"/"Planned Order" or `rps_mstr` rows are added (legacy added both). | RV / P | RV-legacy `master_plan.py:146-175,329-351`; `KST_v2_STAGE_5A_MPS_DATA_INVENTORY.md:154,541-547` |
| Part order | Retained snapshot `ResolvedParts` order (product-line discovery sorted case-insensitively, then explicit parents). Implementation must materialize this order explicitly. | RV / P | `Kst.Integrations.Qad/Mps/QadMpsScopeResolver.cs:39-63` |

### 2.2 Sales orders — Stage 13 lines, RA% excluded inside the workbook

| Item | Detail | Label | Evidence |
|---|---|---|---|
| Lines | Stage 13 `OpenOrderLine`s for the same MPS snapshot via `OpenOrdersService`; no second `sod_det` adapter. | OC / P | `Kst.Application/OpenOrders/OpenOrdersService.cs` |
| Scope | Workspace site/domain, MPS resolved parents, `sod_qty_ord - sod_qty_ship > 0`. | RV | `Kst.Integrations.Qad/OpenOrders/QadOpenOrderQueryContract.cs:94-107` |
| RA exclusion | Lines whose sales order number (`OpenOrderLineKey.SalesOrder`, from `sod_nbr`) begins with `RA` are excluded from workbook demand, SO $ and projections. Stage 13 Open Orders behavior is unchanged. | OC (decision 1) | legacy `sd.sod_nbr NOT LIKE 'RA%'` RV-legacy `master_plan.py:324` |
| RA case handling | Ordinal prefix `RA` on the sales order number as returned by Stage 13. Legacy relied on SQL `LIKE` under the QAD collation; whether lower/mixed-case `ra` prefixes exist is **U** (E3). | OC-14.1 / U | — |
| SO qty | `Open` per line, summed by Due-Date week. | OC / RV | `Kst.Domain/OpenOrders/OpenOrderLine.cs` |
| SO $ | Stage 13 `ExtPrice` = raw `sod_price × Open` (consignment unchanged). | OC / RV | same |
| Due Date | `SourceValues.DueDate` (`sod_due_date`). | RV | — |
| Undated lines | A non-RA line with no Due Date is retained as **Undated**: separate per-part SO quantity and SO $ totals, excluded from Falldown, weekly and fiscal-quarter totals and from inventory projections. The RA exclusion is applied first, so RA lines never reach Undated. | OC (Q-N1, 2026-10-02) | fixture `undated` expectations |
| Item-to-part matching | Use the MPS comparer (`OrdinalIgnoreCase`). | P | `MpsScheduleBuilder.cs` |
| Refresh coupling | A workbook refresh that refreshes Open Orders replaces the shared Stage 13 report; a prepared Open Orders export then fails closed (accepted Stage 13 behavior). | P (documented consequence) | `OpenOrdersQxtendService` |

### 2.3 Opening inventory — reuse

| Item | Detail | Label | Evidence |
|---|---|---|---|
| Rule | Net QOH: positive, nettable, non-RMA lots at site + part. | OC / RV | `Kst.Domain/Inventory/PartInventorySummary.cs` |
| Reader | Batched `IPartInventoryReader.ReadSummariesAsync`; authoritative zeros; failures propagate (never zero). | RV | `Kst.Integrations.Qad/Inventory/QadPartInventoryReader.cs:42-82` |
| Legacy difference | Legacy on hand = all non-`RA%` `ld_qty_oh` (including negative/non-nettable). Not used. | RV-legacy | `master_plan.py:267-275` |

### 2.4 Pricing — new batched read applying the accepted Part Info rule

| Item | Detail | Label | Evidence |
|---|---|---|---|
| Rule | Domain + part; latest `pi_mstr` with `pi_start <= today`; its `pid_det` tiers ordered by `pid_qty` ascending. | RV | `Kst.Integrations.Qad/PartDetail/QadPartDetailReader.cs:132-160`; `KST_v2_STAGE_6_PART_INFO_CONTRACT.md` §7–8 |
| Selection | First tier in that list. All tiers retained; one `PlanningPriceSelection` policy point (`FirstTier` only). No MOQ UI or automatic tier switching. | OC | — |
| Missing vs zero | No current list or no tiers ⇒ **unpriced** (null dollars, flagged). Tier price 0 ⇒ valid $0. | OC (distinction) / OC-14.1 (null representation) | — |
| One price per part | The selected price applies to every week. | OC | — |
| Effective date | Application local date at workbook acquisition, recorded on the workbook snapshot. | P | — |
| Batch reader | Required because the existing reader is single-part. Must reproduce the accepted selection exactly; parameterized, batched, read-only. | P | — |
| Ties | Existing `TOP (1) … ORDER BY pi_start DESC` is nondeterministic for equal `pi_start`. No tie-breaker may be invented. | RV / **U (E2)** | `QadPartDetailReader.cs:140-148` |
| Currency/UoM | Not filtered by the accepted rule; SO $ (order price) and MPS $ (list price) currency equality unverified. Differences are not labelled pricing errors. | RV / OC / **U (E2)** | — |

### 2.5 Forecast — MRP `FORECAST` rows, display-only

| Item | Detail | Label | Evidence |
|---|---|---|---|
| Source | `mrp_det` rows with `mrp_detail = 'FORECAST'` and `mrp_qty <> 0`, as the legacy generator uses, scoped in v2 by workspace domain, site and resolved parts (legacy scoped by product line). | OC (decision 2) / RV-legacy | `master_plan.py:205-253` |
| Date | The row's own `mrp_due_date`, bucketed into the v2 week containing it. Forecast dates are **not** derived from MPS and Order Period / Safety Time are **not** applied again; a forecast in a later week than its resulting MPS is expected. | OC | — |
| Quantity | Legacy displays `ABS(mrp_qty)`; adopted as the display transform. Source sign is unverified. | RV-legacy / OC-14.1 / **U (E1)** | `master_plan.py:216` |
| Role | Not added to supply, not subtracted from demand, not in projections, revenue, validation or CSV. | OC | — |
| Weekly display | Forecast quantity in each visible week. | OC |
| Falldown display | Forecast dated before the current week shows in Falldown (legacy did). | OC-14.1 | `master_plan.py:616-617` |
| Quarter totals | **Not required.** Forecast has no fiscal-quarter sum (owner amendment at 14.1 acceptance). | OC | — |
| Separation | v2 MPS (`mrp_dataset='wo_mstr'`) does not contain `FORECAST` rows, so there is no overlap. Which `mrp_dataset`/`mrp_type` the rows carry is unverified. | RV / **U (E1)** | `docs/data/qadpro2-data-map.md:306-317` |
| Reader | New parameterized read-only `QadForecastReader` (new SQL builder, covered by `QadReadOnlySqlTests`). | P | `tests/Kst.Integrations.Qad.Tests/ReadOnly/QadReadOnlySqlTests.cs` |

## 3. Calendar, Falldown and fiscal quarters

| # | Rule | Label | Evidence |
|---|---|---|---|
| 3.1 | Sunday–Saturday business weeks with Monday labels; week 0 = current week; "today" = application local date. | RV | `Kst.Domain/Mps/MpsBusinessCalendar.cs`; `Kst.Api/Endpoints/MpsEndpoints.cs:51,74` |
| 3.2 | Falldown = dated before the current week's Sunday, for MPS, SO and forecast alike. | RV / P | — |
| 3.3 | Projection seed = **Net QOH + Falldown MPS − Falldown SO**. Week 0 rolls from it. | OC (decision 3) | — |
| 3.4 | Falldown SO, SO $, MPS and MPS $ (and Adjusted MPS / Adjusted MPS $, equal to MPS since Falldown is not adjustable) are included **once** in the current fiscal quarter's totals. | OC (decision 3) / P (Adjusted rows) | — |
| 3.5 | Falldown is excluded from the adjustment CSV and is not adjustable. | OC (CSV) / P (not editable) | — |
| 3.6 | Fiscal calendar is frontend-owned (4-4-5 × 4, anchor FY26 = 2025-06-29, user 53-week exceptions); a week's quarter is `getFiscalDisplayInfo(settings, weekLabel)`. | RV | `src/frontend/src/fiscal/fiscalCalendar.ts`; `components/MpsWorkspace.tsx:310-314` |
| 3.7 | Current fiscal quarter = quarter of the week-0 label. | P | — |
| 3.8 | Every quarter with at least one visible week gets a summary column; partial quarters are marked. | P | — |
| 3.9 | Legacy differences, not reproduced: Monday–Sunday weeks (Sunday rows land one week later in v2); Falldown cutoff at the first MRP week; silently dropped rows beyond its computed horizon; no summary for the final partial quarter. | RV-legacy | `master_plan.py:97-122,132-153` |
| 3.10 | Workbook horizon: default and minimum selectable 12 weeks; maximum `max(12, lastMpsWeekIndex + 2)` from the complete loaded MPS snapshot (Due Date, Sunday–Saturday weeks); no fixed 72-week cap. The Dashboard horizon is unchanged. | OC (Q4, 2026-10-02) | `Kst.Domain/PlanningWorkbook/PlanningHorizonRange.cs` |

## 4. Calculations

All backend arithmetic is `decimal`; API quantities/dollars are invariant decimal text (Stage 13
`OpenOrderDraftValuesDto` precedent); frontend arithmetic uses exact decimal-string helpers (extracted
from `src/frontend/src/openOrders/planning.ts` on this second use). Display rounding is
presentation-only. **P** for representation; formulas below as labelled.

| Value | Definition | Label |
|---|---|---|
| SO | Σ `Open` of non-RA lines by Due-Date bucket. | OC |
| SO $ | Σ Stage 13 `ExtPrice` for the same lines. | OC |
| MPS | Existing Due-Date MPS bucket quantity. | OC |
| MPS $ | MPS × selected price; null if unpriced. | OC |
| Forecast | Display-only (2.5). | OC |
| Adjustment | Signed whole number per part/week; blank = 0. | OC |
| Proposed MPS | Existing MPS + Adjustment. | OC |
| Adjusted MPS $ | Proposed MPS × the same selected price; null if unpriced. | OC |
| Est On Hand | `Est[w] = Est[w−1] + MPS[w] − SO[w]`, seeded per 3.3. | OC |
| Adj On Hand | `Adj[w] = Adj[w−1] + MPS[w] + Adjustment[w] − SO[w]`, same seed; equals `Est[w] + Σ_{k≤w} Adjustment[k]`. | OC / P (identity) |
| Quarter flows | Σ weekly SO, SO $, MPS, MPS $, Adjustment, Proposed MPS and Adjusted MPS $ (+ Falldown in current quarter). Forecast is excluded from quarter sums. | OC |
| Quarter inventory | Balance of the quarter's last visible week; never summed. | OC |
| Unpriced quarter $ | Null for that part. | OC-14.1 |
| Workspace totals | Weekly/quarter cross-part totals for SO, SO $, MPS, MPS $ (Adjustment, Proposed MPS, Adjusted MPS $ added at 14.5); balances not summed. If any retained part is unpriced, every workspace MPS $ total is blank (no priced-part subtotal); SO $ and quantity totals remain. | OC (Q-N2, 2026-10-02) |
| Difference wording | SO $ vs MPS $ differences may arise from quantity, timing or price; not labelled pricing errors. | OC |

## 5. Adjustments and validation

| # | Rule | Label |
|---|---|---|
| 5.1 | Identity: workspace + part + week label (Monday `DateOnly`). One adjustment per identity. | P (part/week grain OC) |
| 5.2 | Input is a whole number. Source MPS quantities and prices are never rounded to satisfy it. | OC (decision 5) |
| 5.3 | Accepted text: optional `-` then ASCII digits. `+5`, `10.0`, exponent, grouping and whitespace are rejected. Zero (`0`, `-0`) is a no-op, not a proposal. | OC-14.1 |
| 5.4 | Blocking: Proposed MPS < 0 (`negative-result`). Zero is valid. | OC |
| 5.5 | Structural blocking codes: `not-whole-number`, `invalid-number`, `duplicate-identity`, `not-a-week`, `week-passed`, `not-adjustable` (Falldown), `part-out-of-scope`, `snapshot-changed`. | OC-14.1 |
| 5.6 | Never blocking: negative Est/Adj On Hand, SO/MPS quantity or $ mismatch, released/firm WOs, lead time. | OC |
| 5.7 | A changed baseline keeps the signed adjustment; it is never converted to a target quantity. During an active session an MPS refresh simply re-applies the adjustment to the new baseline — **no concurrent-edit acknowledgment workflow**. | OC (decision 4) |
| 5.8 | Export validation uses only the loaded data: the request names the current MPS snapshot ID (`snapshot-changed` otherwise); the backend recomputes each part/week baseline from that snapshot's retained rows and today's date and applies 5.2–5.5. **No QAD access during export.** Any invalid entry rejects the whole export; no bytes are produced. | OC (decision 6) / OC-14.1 (codes, whole-file rejection) |
| 5.9 | Scope check: snapshot site equals the workspace site and each part is in the snapshot's resolved parts. | P |
| 5.10 | Problem Details carry only an issue code and affected-row count (Stage 13 precedent). | P / RV | `Kst.Api/Endpoints/OpenOrdersEndpoints.cs:120-163` |

## 6. Saved drafts

| # | Rule | Label | Evidence |
|---|---|---|---|
| 6.1 | Optional, per workspace; local JSON `%LOCALAPPDATA%\KST\config\planning-workbook-draft-{assignmentId}.json`, atomic temp-file + rename; corrupt files retained and reported. | OC (optional) / P (mechanics) | `Kst.Infrastructure/OpenOrders/JsonOpenOrdersDraftStore.cs` |
| 6.2 | Entries: part, week label, signed adjustment, and the MPS quantity it was last saved against; plus MPS snapshot ID and saved-at time. | OC / P | — |
| 6.3 | Restoration compares each saved baseline with the currently loaded baseline, shows changed rows, keeps the signed adjustment, and enables saving after the user confirms the restore (Stage 13 restore pattern). Failures preserve proposals. | OC (decision 4) / P (mechanics) | `Kst.Application/OpenOrders/OpenOrdersDraftService.cs` |
| 6.4 | Restoration reconciles against the workspace's current loaded MPS snapshot; it does not force an MPS refresh or an extra QAD read. | OC-14.1 | — |
| 6.5 | Entries now invalid (negative result, week passed, part out of scope) are retained and flagged; they block export until edited or removed. | OC (preserve) / OC-14.1 (flags) | — |
| 6.6 | Presence check is file-existence only. Workspace delete/reset removes the workbook draft; archive preserves it. | P | `Kst.Api/Endpoints/WorkspaceEndpoints.cs:157,167` |
| 6.7 | Saving or exporting never means QAD applied a change. Export never deletes a draft. | OC / P | — |
| 6.8 | Post-export lifecycle (clear exported rows, "last export" record) is **U (Q-N3)**, decide by 14.6. After an external update and MRP, refreshed baselines will differ from saved ones; that is expected. | U | — |

## 7. QAD Mass Updater CSV

**Confirmed (OC):** columns Part, signed adjustment, weekly date; no header; `M/d/yyyy` without
leading zeros; signed adjustment rather than resulting MPS; one row per part/week with a nonzero
adjustment; positive and negative rows; Falldown and quarter columns excluded; part order then week
order; explicit user save; human-reviewable; no database write, submission or import.

**Legacy evidence (RV-legacy):** `F_MasterPlanExport.bas:33-112` builds an export *worksheet*
with a `Part, Qty, Date` header, scans part blocks top-down and weekly columns left-to-right from
the first weekly column, skips "Summary" columns, exports nonzero numeric cells, and uses the
weekly header date. It specifies no CSV serialization.

**Accepted at 14.1 (OC-14.1):** date = Monday week label; UTF-8 without BOM; CRLF after every row
including the last; invariant integer text with `-` only for negatives; RFC 4180 quoting only when
needed; no spreadsheet formula prefixing (it would corrupt negative quantities).

**Still proposed (P, decide at 14.6):** suggested filename
`{Workspace}-MPS-Adjustments-{yyyy-MM-dd}.csv`; backend returns `{fileName, contentBase64}` after
validation; frontend Save As through the existing Tauri `dialog:allow-save`/`fs:allow-write-file`
capabilities (no new capability).

**Unverified (U, E5):** which QAD object Mass Updater changes and when KST sees it (after MRP);
Mass Updater tolerance of BOM, trailing CRLF and the Monday date convention. The owner-run trial is
external evidence, not a KST action.

The Mass Updater file is an operational proposal file; analytical workbook exports belong to Stage 21.

**Open for 14.6 (recorded 2026-10-06, not decided):** the owner's Master plan redesign
(`docs/reference/MP_UI_update/DESIGN_SPEC.md`) shows the export column order as *part number, due
date, change quantity*, whereas the confirmed order above and the golden fixture
`docs/reference/Stage14Fixtures/MpsAdjustments.expected.csv` use *Part, signed adjustment, weekly
date*. This must be reconciled with the owner before any 14.6 implementation; neither is changed
here. See §17 for the other export requirements recorded for 14.6.

## 8. Workspace scope, freshness and isolation

| # | Rule | Label |
|---|---|---|
| 8.1 | All inputs share the workspace site and `QadSiteDomainMap` domain; prices are domain + part (no site). | RV / P |
| 8.2 | Workbook snapshot = {workbook ID, MPS snapshot ID, Open Orders snapshot ID, acquired-at, price effective date, inventory, prices, forecast}; per workspace; last-good retained as stale on refresh failure; source failures never become zeros. | P |
| 8.3 | MPS snapshot change ⇒ workbook reload (409 `snapshot-changed` for an outdated request). | P |
| 8.4 | Workbook refresh re-acquires Open Orders (via Stage 13), inventory, prices and forecast; it does not refresh MPS. | P |
| 8.5 | Stores, drafts and UI state are keyed by assignment ID; tests cover two workspaces with overlapping parts. | P |
| 8.6 | Workbook staleness (SO/inventory/price/forecast) does not block export; export validity depends only on the loaded MPS snapshot identity. | OC-14.1 |

## 9. Discrepancy register

| ID | Discrepancy | Disposition |
|---|---|---|
| D1 | Checklist "unit cost" vs price-only. | OC — checklist corrected at 14.1. |
| D2 | Checklist "frozen-fence"/"frozen periods" vs no fence. | OC — corrected. |
| D3 | Legacy Mon–Sun vs v2 Sun–Sat weeks. | RV — v2 calendar; documented. |
| D4 | Legacy Falldown/horizon defects. | RV — not reproduced. |
| D5 | Legacy on hand vs Net QOH. | OC — Net QOH. |
| D6 | Legacy `MAX(pid_amt)` without `<= today` vs first tier with `<= today`. | OC — v2 rule. |
| D7 | Legacy MPS composition vs single v2 MPS source. | RV — v2 only; legacy parity not expected. |
| D8 | Legacy excluded `RA%` SOs; Stage 13 includes them. | OC — excluded in workbook only. |
| D9 | Legacy product-line scope vs v2 resolved parents. | RV — v2 scope; identical-scope legacy parity not achievable unmodified. |
| D10 | Prototype `max(SO, Forecast)`. | OC — display-only forecast. |
| D11 | `VERSIONING.md` advances the version when a stage is planned; owner kept version changes separate during 14.1. | OC — applied at 14.2 (`0.1.0-alpha.14`). |
| D12 | Legacy export worksheet has a header; owner CSV has none. | OC — no header. |
| D13 | Earlier plan proposed a fresh targeted QAD reread before export (Stage 13 pattern). | OC — removed from Stage 14. |

## 10. Decision register

| ID | Question | Decision (2026-10-02) | Label |
|---|---|---|---|
| Q1 | RA% sales orders | Exclude from workbook demand, SO $ and projections; Stage 13 unchanged. | OC |
| Q2 | Forecast source | Legacy MRP `FORECAST` rows at their own dates; no Order Period/Safety Time offsets; display-only. | OC |
| Q3 | Falldown | Seed = Net QOH + Falldown MPS − Falldown SO; Falldown flows once in current-quarter totals; excluded from CSV. | OC |
| Q5 | Changed baseline | Keep signed adjustment; no in-session acknowledgment workflow; restoration comparison retained. | OC |
| Q6 | Adjustment values | Whole numbers only; source quantities/prices not rounded. | OC |
| Q7 | Fresh export check | Not in Stage 14; validate against loaded MPS data including negative-result rejection. | OC |
| Q-N1 | Sales-order lines with no Due Date | Non-RA lines retained as Undated with separate quantity and SO $ totals; excluded from Falldown, weekly/fiscal-quarter totals and projections; RA exclusion applied first. | OC |
| Q-N5 | Version timing | Advance to `0.1.0-alpha.14` with Checkpoint 14.2 using the documented versioning workflow. | OC |
| Q4 | Planning horizon (14.4) | Default and minimum selectable 12 weeks. Maximum = `max(12, lastMpsWeekIndex + 2)`: through one week after the last Due-Date business week (current week = 0) with a nonzero existing MPS quantity across the resolved parts of the complete loaded MPS snapshot. Sales orders, forecast and adjustments never extend it; no fixed 72-week cap; recalculated on MPS-snapshot or business-week change, clamping a larger selection. Dashboard horizon unchanged. | OC |
| Q-N2 | Unpriced workspace totals (14.4) | If any retained part is unpriced, workspace MPS $ totals are blank for every weekly and quarterly summary; no priced-part subtotal. Priced parts (including valid zero price) still show their own MPS $; quantity and SO $ totals remain. | OC |
| Q-N6 | Undated display (14.4) | Due Date is required when entering a sales order, so the Planning view shows no per-part or workspace Undated display. Backend defensive Undated handling and fixtures stay unchanged; undated values are never given a date or folded into dated totals. | OC |
| Q-R1 | Workspace totals in the redesigned view (2026-10-06) | Totals cover the whole workspace, computed from the complete workbook independently of View model visibility and filter chips. The two dollar totals (SO $, MPS $) are pinned by default; quantity (sales orders, MPS) and adjustment totals are available through View. Q-N2 still applies: a hidden unpriced model keeps workspace MPS $ blank. Inventory is never summed. | OC |
| Q-R2 | Planning density (2026-10-06) | Planning follows the application's global row-density preference; no separate Planning density preference. | OC |
| Q-R3 | Entries outside the visible horizon (2026-10-06) | Remain listed in Changes. Being hidden does not by itself prevent export; validation against the full loaded MPS snapshot (14.6) determines eligibility. Supersedes the 14.5 `outside-horizon`/`beyond-maximum` blocking issues (§16). | OC |

Open, non-blocking (decide by the named checkpoint): Q-N3 post-export lifecycle and last-export
record (14.6); Q-N4 price ties/currency after E2; RA prefix case variants (evidence E3). Q4, Q-N2
and Q-N6 were decided by the owner on 2026-10-02 for 14.4. Q-R1–Q-R3 were decided by the owner on
2026-10-06 for the 14.5 Master plan redesign (§17).

## 11. Unverified source evidence (not run; each needs separate authorization)

| ID | Gap | Bounded request shape (read-only, parameterized, counts/metadata only) |
|---|---|---|
| E1 | `FORECAST` row sign, `mrp_dataset`/`mrp_type`, presence for workspace parents. | Row counts grouped by `mrp_dataset`, `mrp_type`, sign of `mrp_qty` for `mrp_detail='FORECAST'` at one site for 2–3 owner-selected parents. |
| E2 | Price ties at max `pi_start <= today`, unpriced parts, zero prices, currencies. | Counts for one workspace's resolved parents; distinct `pi_curr`. |
| E3 | `RA` prefix case variants; open lines with null `sod_due_date`. | Counts only for in-scope positive-open lines. |
| E4 | Sunday-dated rows (optional calendar impact). | Counts only. |
| E5 | Mass Updater acceptance of the proposed file format. | Owner-run external trial with an approved file. |

Until these are run, the corresponding behavior is verified only against synthetic fixtures.

## 12. Checkpoint 14.1 owner acceptance — 2026-10-02

The owner accepted Checkpoint 14.1 on 2026-10-02: this ledger, the checklist corrections and the
synthetic fixtures, and the eight proposed choices (CSV date/encoding/line-ending/quoting/no formula
prefix; whole-number text form; structural validation codes with whole-file rejection; forecast
`ABS` display and Falldown forecast display; null dollars for unpriced parts; restoration against the
current loaded MPS snapshot; export independent of workbook staleness; ordinal `RA` prefix), with
one amendment: **forecast quarter sums are not required**. Weekly forecast quantities and the
Falldown forecast display remain; all other quarter totals are unchanged. The fixtures and the
independent verifier were corrected accordingly and rerun before acceptance was recorded.

Acceptance does not verify any live source behavior (E1–E5 remain unverified), does not change
version metadata (`0.1.0-alpha.13`; timing pending owner decision), and does not authorize
Checkpoint 14.2.

## 13. Checkpoint 14.2 — pure Domain calculation core — COMPLETE / OWNER-ACCEPTED — 2026-10-02

Implemented 2026-10-02 in `src/backend/Kst.Domain/PlanningWorkbook/` with fixture-driven tests in
`src/backend/tests/Kst.Domain.Tests/PlanningWorkbook/` that read the accepted JSON fixture and golden
CSV. No QAD adapter, application service, endpoint, persistence, CSV serializer, UI or backend
fiscal-calendar logic was added.

| Type | Behavior |
|---|---|
| `PlanningWorkbookBuilder` | Due-Date bucketing on `MpsBusinessCalendar` (Sunday–Saturday, Monday labels) for MPS, SO and forecast; MPS from `MpsScheduleBuilder` (Due-Date basis) only; workbook-only ordinal `RA` exclusion applied before every SO total; Undated per-part totals (§2.2); seed = Net QOH + Falldown MPS − Falldown SO; Est/Adj On Hand roll-forward; MPS $ / Adjusted MPS $ with the same selected price, null when unpriced; absolute display-only forecast including Falldown; Falldown not adjustable; rows beyond the horizon not shown; missing Net QOH or price-tier entries rejected rather than zeroed; duplicate display adjustments rejected. |
| `PlanningPriceSelection` | `FirstTier` = lowest MOQ (stable for equal MOQs); empty tiers = unpriced; zero price valid. |
| `MpsAdjustmentInput` | Whole-number text rules (§5.3); blank/`0`/`-0` no-op. |
| `MpsAdjustmentValidator` | Whole-request validation against the loaded MPS snapshot only: snapshot identity, structural codes (§5.5), duplicate identity (including no-op entries), negative resulting MPS (zero valid); baseline = Due-Date week quantity of the retained rows independent of the visible horizon; any issue rejects the whole request with no rows; primary issue = first in the `MpsAdjustmentIssue` precedence order with the affected-entry count; valid nonzero rows ordered by retained part order then week, using the retained part spelling. |
| `PlanningDraftReconciler` | Saved-draft comparison against the loaded baseline using the same rules; signed adjustment always retained; invalid entries retained and flagged; no acknowledgment step. An unavailable baseline reports null `CurrentMps`/`ProposedMps` with an issue code; a valid zero baseline reports numeric zero. |

Not added (by decision): fresh QAD export reread, frozen/lead-time fence, released-WO restriction,
in-session acknowledgment workflow. Fiscal-quarter expectations in the fixture remain for the
frontend checkpoint (14.4).

**Owner acceptance — 2026-10-02.** The owner accepted Checkpoint 14.2: the pure Domain core, the
fixture-driven tests, the version advance to `0.1.0-alpha.14`, and the post-review test corrections.
Those corrections are: the D06 in-session test reads its values from the fixture and asserts the
before and after states; the D04/D05 tests assert null `CurrentMps`/`ProposedMps`, `BaselineChanged`
false, a retained entry and adjustment, the issue code and blocked export; and a new test reports a
valid zero baseline as numeric zero. No `BaselineAvailable` flag was added. Recorded verification:
backend build passed; backend tests 1,009/1,009; the D06 mutation was detected; the scoped format
check passed; `git diff --check`, `check-version.ps1` and the independent fixture verifier (456
checks) passed.

Verification limits remain: the evidence is synthetic-fixture only. Live source evidence E1–E5
(§11) is unverified. Full-solution `dotnet format --verify-no-changes` still fails on 62 untouched
existing files. Frontend checks, `cargo check` and the sidecar rebuild were not run, so there is no
packaged `alpha.14` evidence. The open questions in §10 are unchanged. Acceptance does not authorize
Checkpoint 14.3.

## 14. Checkpoint 14.3 — read-only adapters, composition and workbook API — COMPLETE / OWNER-ACCEPTED — 2026-10-02

Implemented 2026-10-02. API reference: `docs/api/PLANNING_WORKBOOK.md`.

| Layer | Addition | Behavior |
|---|---|---|
| Kst.Integrations.Qad | `PlanningWorkbook/QadPartPriceReader` | Batched, parameterized, read-only form of the accepted Part Info price rule (§2.4). For each requested part: domain + part, the latest `pi_mstr` with `pi_start <=` the acquisition's local date, chosen by `ROW_NUMBER() … ORDER BY pi_start DESC` (equivalent to `TOP (1) … ORDER BY pi_start DESC`); only that list's `pid_det` tiers, ordered by MOQ ascending. Every requested part is returned: an empty tier list means unpriced, and a zero price is valid. No currency or unit-of-measure filter and no tie-breaker are added. Malformed rows throw. |
| Kst.Integrations.Qad | `PlanningWorkbook/QadForecastReader` | Batched, parameterized, read-only reader: `mrp_det` with `mrp_detail = 'FORECAST'` and `mrp_qty <> 0` (NULL kept so it fails visibly), scoped by domain, site and resolved parts. Each row keeps its own `mrp_due_date` and signed quantity. No `mrp_dataset`/`mrp_type` or date filter. Malformed rows throw. |
| Kst.Application | `PlanningWorkbook/PlanningWorkbookContracts`, `PlanningWorkbookService` | Reader contracts with delegate bridges, a snapshot record, a store interface and result kinds. The service composes the accepted Domain builder from the loaded MPS snapshot (never acquired or refreshed), Stage 13 `OpenOrdersService`, `IPartInventoryReader` Net QOH, prices and forecast. |
| Kst.Infrastructure | `PlanningWorkbook/InMemoryPlanningWorkbookSnapshotStore` | Store keyed by workspace assignment ID. |
| Kst.Api | `PlanningWorkbookEndpoints`, `PlanningWorkbookDtos`, DI in `Program.cs` | `GET …/planning-workbook` and `POST …/planning-workbook/refresh`, both requiring `mpsSnapshotId` and `horizonWeeks` (marked required in OpenAPI; 1–72, the MPS API bounds; no default). Decimal strings; `null` dollars only when unpriced. Status codes: 400, 404, 409 (MPS not loaded or changed, or site changed), 503 (unavailable with no last good). |

**Source identities recorded per workbook snapshot (§8.2):** workbook ID, MPS snapshot ID, Open
Orders snapshot ID, acquisition time (UTC), price effective date (local acquisition date), Net QOH,
price tiers and forecast rows.

**Cache and freshness (proposed technical choices, P):**

- **Reuse:** cached source facts are reused only for the same workspace, site and MPS snapshot, and
  only on the same local date as their price effective date. Changing only the horizon reuses them.
  Crossing a local date, including into a new business week, reacquires inventory, prices and
  forecast, because a different price list may now be effective.
- **Per-request projection:** the projection is recalculated for every request's horizon and
  business week.
- **`GET` and Open Orders:** `GET` uses Stage 13 `GetAsync`, so a compatible cached Stage 13 report
  is reused.
- **Refresh:** refresh uses Stage 13 `RefreshAsync` and then rereads inventory, prices and forecast.
  This replaces the shared Stage 13 report, so an export prepared against the previous report ID is
  rejected as changed under the existing Stage 13 rules.

**Failure behavior:**

- **Publication:** a snapshot is published only after all reads succeed and the workspace
  assignment, its site and the MPS identity are rechecked (after waiting for the per-workspace gate,
  after the reads, and before publication). An MPS or site change returns 409 and a deleted
  workspace returns 404; neither publishes anything.
- **Failed source:** a failed source, or a stale Stage 13 report, keeps the compatible last good
  snapshot as stale with a sanitized warning, or returns Unavailable (503) when there is none. The
  workspace and MPS are rechecked first, so a deleted or re-sited workspace gets 404/409 instead of
  the previous-context workbook.
- **No substitutes:** a reader result that omits a requested part is a failure, never zero or
  unpriced. The price query must return at least one row per requested part; only an explicit
  no-tier row means unpriced.
- **Empty scope:** an empty resolved scope performs no inventory, price or forecast read.
- **Cancellation:** cancellation propagates without publishing.

**Unchanged:** the 14.1/14.2 Domain rules: MPS-only supply, Sunday–Saturday weeks with Monday
labels, ordinal `RA` exclusion, Undated handling, the Falldown seed, exact decimals, display-only
forecast, and unpriced kept distinct from zero price.

**Not added:** fiscal-quarter calculation, adjustment endpoints, drafts, CSV, UI, fresh QAD export
validation, fence, released-WO restriction, acknowledgment. The version stays `0.1.0-alpha.14`.

**Unresolved (unchanged):**

- **E1–E5** (§11) remain unverified. The forecast sign, dataset and type, price ties, currency and
  `RA` case have not been checked against live QAD.
- **Q-N4 price ties:** the batch reader reproduces the accepted rule's nondeterminism for equal
  `pi_start` and does not claim deterministic parity for ties.

**Owner acceptance — 2026-10-02.** The owner accepted Checkpoint 14.3: the read-only price and
forecast adapters, `PlanningWorkbookService` composition, the in-memory snapshot store, the workbook
`GET` and refresh endpoints with regenerated contracts, and `docs/api/PLANNING_WORKBOOK.md`. The
acceptance includes the four post-review corrections:

- a requested part with no price row is a failed read (only an explicit no-tier row is unpriced);
- the workspace assignment is re-read after the acquisition gate and before publication or stale
  fallback (deleted → 404, site changed → 409, nothing published);
- `mpsSnapshotId` and `horizonWeeks` are required in OpenAPI on both routes;
- corrected cache documentation (horizon-only changes reuse facts; crossing a local date
  reacquires inventory, prices and forecast).

It also includes the documented cache, stale-data and shared Open Orders refresh behavior above.
Recorded verification: backend build passed with 0 warnings; backend tests 1,053/1,053; mutation
checks detected (stale Stage 13 report; workspace re-read); contract generation and frontend
typecheck, scoped format check, `check-version.ps1`, `git diff --check` and the fixture verifier
(456 checks) passed; Gitleaks found no leaks; DevSkim findings were only in existing `Program.cs`
lines.

Verification limits remain: the evidence is offline and synthetic only. Live source evidence E1–E5
(§11) and Q-N4 are unverified. Full-solution `dotnet format --verify-no-changes` still fails on 62
untouched existing files. No connected application run, sidecar rebuild, `cargo check` or desktop
check was performed. Acceptance does not authorize Checkpoint 14.4.

## 15. Checkpoint 14.4 — read-only Planning Workbook view and fiscal-quarter summaries — OWNER-ACCEPTED with zero-value display amendment — 2026-10-02

**Status:** implemented and verified offline on 2026-10-02; **owner-accepted 2026-10-02** with the
zero-value display amendment recorded below (fully accepted, including the four review corrections).
Checkpoint 14.5 was authorized separately on 2026-10-02 (§16).

**Owner decisions applied (2026-10-02, §10 Q4, Q-N2, Q-N6):**

- **Horizon.** Default and minimum selectable 12 weeks. Maximum = `max(12, lastMpsWeekIndex + 2)`
  from the complete loaded MPS snapshot (Due Date, Sunday–Saturday weeks, current week = 0; a week
  counts when its aggregated part/week quantity is nonzero). Falldown, sales orders, forecast and
  adjustments never extend it. Implemented by the pure Domain `PlanningHorizonRange` and applied by
  `PlanningWorkbookService` from already-loaded facts (no QAD read). The fixed 72-week workbook cap
  was removed; a request above the maximum returns 400 with a `maximumHorizonWeeks` extension and
  reads nothing. The response adds `minimumSelectableHorizonWeeks` (12) and `maximumHorizonWeeks`.
  The API still accepts 1–11 so the accepted 4-week fixture projections remain testable; the
  Planning view enforces the 12-week minimum, keeps its own preference independent of the Dashboard
  horizon, and clamps a larger selection when the reported range shrinks (new MPS snapshot or
  business week). The Dashboard horizon is unchanged.
- **Unpriced parts.** If any retained part is unpriced, every workspace MPS $ total (Falldown,
  weekly, quarterly) is blank with a short explanation. Priced parts, including valid zero-price
  parts, show their own MPS $; quantity and SO $ totals remain.
- **Undated.** No per-part or workspace Undated display. Backend defensive handling, the
  `undated` response field and the fixtures are unchanged; undated values never receive a date and
  never enter dated totals.

**Implemented (frontend, presentation only):**

- Planning tab → `PlanningWorkbookPanel` (`usePlanningWorkbook`, `planningWorkbookApi`): loads only
  with an assignment and loaded MPS snapshot; never loads or refreshes MPS. Refresh Planning calls
  only the workbook refresh endpoint and explains that it also refreshes the shared Customer Open
  Orders report (a prepared Open Orders export may need to be prepared again). Results are keyed by
  workspace, site, MPS snapshot and horizon; late responses and mismatched identities are ignored,
  and incompatible data clears immediately. Compatible data stays visible with a warning after a
  failed refresh; stale responses show acquisition time and price date. States: loading/retry,
  empty scope, stale, unavailable (503), unknown workspace (404), MPS not loaded / changed (409).
  The existing Open Orders unsaved-change confirmation is unchanged.
- Grid (`PlanningWorkbookGrid`): retained part order including no-activity parts; per part Net QOH
  and selected price or **Unpriced**; rows SO Qty, SO $, MPS, MPS $, Forecast (display only), Est.
  On Hand; Falldown first, then weekly `M/d/yyyy` columns; a distinct quarter summary column after
  each quarter's last visible week, including the final partial quarter; sticky headers and
  part/metric labels; negative inventory highlighted as display-only; scoped CSS.
- Fiscal quarters (`planningWorkbookSummary.ts`): accepted frontend fiscal calendar and configured
  53-week exceptions, from API week labels; partial quarters from actual fiscal boundaries; Falldown
  flows added once to the quarter of week 0; Est. On Hand = last visible week's balance; unpriced
  part MPS $ stays null; no forecast quarter value; recomputed locally when fiscal settings change.
- Exact decimals: Stage 13 helpers extracted to `src/frontend/src/shared/exactDecimal.ts` (adds
  `add`, `sumDecimals`, presentation-only `formatDecimal`); `openOrders/planning.ts` re-exports
  `decimal`, `subtract`, `multiply`, `equalDecimal` so Stage 13 imports are unchanged.

**Verification (offline only):** backend build 0 warnings; backend tests 1,066/1,066 (13 new);
frontend typecheck passed; full frontend tests 469 passed, 5 skipped (474; one unchanged
`MpsWorkspace` timing test failed once under full-suite load and passed alone and on four further
full runs); production build passed; `npm run lint` reports only the two known Stage 10
`setState-in-effect` errors; scoped lint of changed files clean; scoped `dotnet format` clean;
`check-version.ps1` at `0.1.0-alpha.14`; `git diff --check` clean; Gitleaks no leaks; DevSkim two
DS162092 notes (mock `localhost` URLs in a test). Fixture sensitivity: changing P-1 FY27-Q1
`soDollars` in a temporary copy failed exactly the corresponding quarter test; the accepted fixture
was never edited (SHA-256 unchanged). Visual review used headless Chrome against a temporary
mocked-response harness (removed afterwards) at 1600 px and 1024 px widths. No live QAD call, sidecar
rebuild, `cargo check` or connected desktop run was performed. E1–E5 and Q-N4 remain unverified.

**Review corrections (2026-10-02, before acceptance).** The owner
accepted keeping API horizons of 1–11 weeks available (the view's default/minimum stays 12). Four
frontend-only corrections followed; no backend file changed:

1. Refresh Planning is enabled only when a workbook for the current context is loaded, so it can no
   longer supersede a pending initial/changed-context/horizon load and leave an indefinite loading
   state; a refresh failure without compatible retained data is shown explicitly with Retry, while
   compatible data is still kept with a warning.
2. Results are also keyed by the local business week. The open view re-checks the week every 60 s
   and on focus/visibility (covering return from sleep) and, on a new week, issues a normal GET (new
   labels and maximum; a shrinking maximum clamps the selection). It never calls POST refresh or
   MPS; late pre-rollover responses are ignored.
3. Fiscal settings were verified through the real General settings workflow (no test override):
   Planning reflects the saved 53-week exception when revisited; saving makes no request; the revisit
   makes only the same single remount GET as a revisit without a settings change. No production fix
   was needed.
4. Legacy colour separation restored: SO Qty/SO $ rows green, MPS/MPS $ rows yellow across pinned
   labels, Falldown, weekly, quarter-summary and workspace-total cells; Forecast and Est. On Hand stay
   neutral; quarter borders, Falldown boundary and negative-inventory highlighting are preserved;
   blank workspace MPS $ cells keep the supply background.

Verification after the corrections: frontend 477 passed, 5 skipped (482) on three consecutive full
runs with no failure (the earlier one-off Dashboard timing failure did not recur); typecheck, scoped
lint, production build and `git diff --check` passed; full lint still shows only the two known Stage
10 errors; mutation checks confirmed the new refresh and rollover tests fail without the fixes.
Screenshots were retaken at 1600 px and 1024 px, including scrolled views. Limits are unchanged.

**Owner acceptance — 2026-10-02: Checkpoint 14.4 — OWNER-ACCEPTED with zero-value display
amendment.**

- **Owner review (dev mode, separate from automated evidence):** the owner rebuilt and launched the
  application in dev mode and reviewed the Planning view. The owner accepted the checkpoint,
  including the four review corrections, with one final amendment. The agent did not run that
  session; it is not automated or live-source verification evidence.
- **Amendment implemented (display only):** in Planning grid cells (Falldown, weekly,
  quarter-summary and workspace-total cells for SO Qty, SO $, MPS, MPS $, Forecast and Est. On Hand),
  an exact numeric zero displays blank instead of `0` or `$0.00`.
  - Zero is decided on the exact value (`equalDecimal(value, '0')`, so `0`, `0.00` and `-0.000` are
    all zero) and never on rounded text. A nonzero value that rounds to `$0.00` still shows.
  - Underlying values, calculations, totals, API contracts and cached data are unchanged. Most cells
    keep the exact value in their tooltip; some cells instead keep an explanatory tooltip (Forecast
    "Display only", quarter Est. On Hand "Balance at the last shown week…", unpriced and blank
    workspace MPS $ explanations) or, for negative inventory, the negative-inventory tooltip.
  - Kept as before: nonzero and negative formatting, unpriced `—` indicators, blank workspace MPS $
    when any part is unpriced, zero-price parts treated as priced, row colours on blank cells,
    quarter borders, the Falldown boundary and negative-inventory highlighting.
  - Part-header Net QOH and selected price still show zero explicitly. Shared formatters were not
    changed, so other screens are unaffected.
  - Changed files: `src/frontend/src/components/PlanningWorkbookPanel.tsx` and
    `PlanningWorkbookPanel.test.tsx`. No backend or contract change.
- **Verification (offline):**
  - Affected tests passed: PlanningWorkbookPanel 28, settings workflow 1, CustomerWorkspace 4,
    summary 16.
  - One full frontend run: 479 passed, 0 failed, 5 skipped (484).
  - Typecheck, scoped lint, production build and `git diff --check` passed.
  - Full `npm run lint` still reports only the two known Stage 10 errors.
  - Backend checks were not repeated (last run 1,066/1,066).
- **Limits unchanged:** E1–E5 and Q-N4 (price ties/currency) remain unverified. No live QAD query,
  dependency install, sidecar/Rust work or agent-run connected desktop session occurred.
  At the time of acceptance Checkpoint 14.5 was not yet authorized; it was authorized later on
  2026-10-02 (§16).

## 16. Checkpoint 14.5 — Planning adjustment staging and saved drafts — OWNER-ACCEPTED (with the §17 redesign and amendments) — 2026-10-06

**Authorization:** the owner fully accepted 14.4 and authorized 14.5 only on 2026-10-02. 14.5 is
implemented and verified offline; it is **not owner-accepted**. Checkpoint 14.6 (Mass Updater CSV
serializer, export endpoint, Save As, post-export lifecycle Q-N3) is not authorized and was not
started. No accepted business rule was changed; no contradiction with §4–§6 was found.

**Adjustment staging (frontend; `planningWorkbook/planningAdjustments.ts`, `usePlanningProposals`).**

- Per-part weekly **Adjustment** inputs and read-only **Proposed MPS**, **Adjusted MPS $** and **Adj
  On Hand** rows. Entries are keyed by part (case-insensitive) and Monday week label, never by column.
- Parsing mirrors the Domain `MpsAdjustmentInput` (5.3): optional `-` then ASCII digits, up to the
  .NET decimal maximum; blank/`0`/`-0` are no-ops and never proposals; `+5`, decimals, exponents,
  grouping, whitespace and non-ASCII digits are rejected. Partially typed text (`-`) stays in the
  input and is flagged, never erased or applied.
- Proposed MPS = MPS + adjustment; Adjusted MPS $ = Proposed MPS × the same selected price (null when
  unpriced); Adj On Hand = Est. On Hand + cumulative visible adjustments (same Falldown seed). Exact
  decimal arithmetic throughout; source quantities and prices are never rounded.
- Falldown has no input; its proposed supply equals existing supply. Quarter columns sum visible weeks
  plus Falldown once in the current quarter; Adj On Hand in a quarter is the ending balance; Forecast
  still has no quarter total. Workspace flow totals add Adjustment, Proposed MPS and Adjusted MPS $;
  inventory is never summed; Adjusted MPS $ totals are blank when any part is unpriced (same rule as
  MPS $). Derived exact zeros display blank (§15 amendment). Adjustment/Proposed/Adjusted $ rows use
  the supply (yellow) band; Adj On Hand is neutral with negative-inventory highlighting.
- Blocking issues shown per cell and in a review list with **Remove**: `invalid-number`,
  `not-whole-number`, `negative-result` (zero is valid), `week-passed`, `part-out-of-scope`, and two
  frontend view states — `outside-horizon` (entry kept but not shown at the current horizon) and
  `beyond-maximum` (week at or beyond the MPS-derived maximum). *Superseded 2026-10-06 by Q-R3 (§17):
  the two view states are no longer issues and no longer block; they are reported as hidden, with
  their baseline unchecked by the frontend.* Negative inventory, forecast,
  demand/supply differences, WO status, lead time and fences never block.
- **Proposal validity** (`evaluatePlanning(...).isValid`, shown as the validity line) is true only
  when at least one proposal exists and no entry has an issue, so 14.6 can fail closed.
- Undo (one step per cell-editing session, restore, remove or Clear All), confirmed Clear All, staged
  and needs-attention counts, and saved/unsaved status.
- State lives in `CustomerWorkspace` per assignment, so it survives module switches, horizon changes,
  Refresh Planning, business-week rollover and MPS snapshot replacement. A new snapshot re-applies the
  same signed adjustments (no target conversion, no acknowledgment) and flags newly invalid entries.
  Hidden entries are retained for review/removal and reappear when the horizon is widened;
  adjustments never extend the horizon. Workbook results remain keyed by context (14.4), so an old
  response cannot overwrite the current evaluation. Planning never loads or refreshes MPS; edits make
  no request.

**Saved drafts (backend + frontend).**

- `PlanningWorkbookDraftService` (Application), `JsonPlanningWorkbookDraftStore` (Infrastructure),
  four endpoints (presence, load/reconcile, save, delete; see `docs/api/PLANNING_WORKBOOK.md`),
  generated OpenAPI/TypeScript. File:
  `%LOCALAPPDATA%\KST\config\planning-workbook-draft-{assignmentId}.json`; atomic temp-file + rename;
  corrupt/invalid files reported and retained (save over a corrupt file is refused with
  `draft-corrupt`). Per-assignment gate serializes save/delete/load.
- Entries store the signed adjustment and the loaded MPS quantity it was saved against (null when
  unavailable), plus snapshot ID, site and saved-at time. No-ops are never saved; flagged entries are.
- Load reconciles against the workspace's **currently loaded** MPS snapshot with the accepted Domain
  `PlanningDraftReconciler` (D01–D06 behavior); unavailable baselines are null, distinct from 0; a
  previously unavailable baseline that is now available is reported as changed. A site change adds a
  warning.
- Frontend: presence is checked when Planning opens; a saved draft is never auto-restored. **Review
  Saved Draft** shows saved vs current MPS, proposed result and status; **Cancel** keeps current
  proposals; **Restore saved draft** asks before replacing existing session adjustments (undo
  available) and retains flagged entries. Failed reads, corrupt drafts and save/delete failures keep
  the session. Save Draft is disabled with no proposals (an empty session can never overwrite a
  draft); invalid syntax must be corrected first. Only the edit version that was sent is marked saved,
  so edits made during a save remain unsaved. Late save/review/delete/presence responses for another
  assignment, or superseded requests, are ignored.
- Workspace delete/reset remove the Planning draft (Stage 13 failure convention: 503, workspace kept);
  archive preserves it.

**Unsaved-change protection.** `CustomerWorkspace` reports Open Orders dirtiness (unchanged Stage 13
behavior and message) and Planning dirtiness to `ApplicationShell`, which confirms before switching
workspace, opening General, archiving/deleting the active workspace and resetting workspaces. Planning
is dirty when the session holds proposals or invalid input that differ from the saved draft (a saved
draft may still contain staged proposals and is not dirty). Moving between modules of the same
workspace keeps Planning proposals and needs no confirmation; leaving Open Orders keeps its existing
confirmation. A `beforeunload` warning matches the Stage 13 panel.

**Verification (offline, automated/mock only):**

- Backend: build 0 warnings (`--no-incremental`); tests **1,084/1,084** (Domain 262, Application 364,
  QAD 260, API integration 188, Architecture 10; 18 new: 7 draft-service tests including D01–D06 from
  the fixture, 8 JSON-store tests, 3 endpoint tests). OpenAPI regenerated by the build and TypeScript
  regenerated with `npm run generate:types`.
- Frontend: typecheck passed; **532 passed, 5 skipped (537)** on the final two consecutive full runs
  (53 new: 39 calculation tests, 11 panel tests, 3 App-level workflow tests); production build passed;
  scoped lint clean; full `npm run lint` still reports only the two known Stage 10 errors.
  Describe-level 20 s timeouts were added to the new UI-heavy tests and to the 14.4 settings-workflow
  test, whose taller grid exceeded the 5 s default once under full-suite load (Stage 13 precedent).
- Mutation checks (restored byte-for-byte): marking the newest edit as saved, not reporting Planning
  dirtiness, and not deleting the Planning draft on workspace delete each failed the intended tests.
- Scoped `dotnet format` clean; full-solution format failures remain the known pre-existing set;
  `check-version.ps1` at `0.1.0-alpha.14`; `git diff --check` clean; Gitleaks no leaks; DevSkim only
  the existing `Program.cs` localhost/CORS notes and mock-localhost test URLs.
- Offline screenshots (headless Chrome, temporary mocked harness removed from the repository).

**Review corrections (2026-10-02, still pending owner review).** Three draft-state findings from
the Stage14_5 review packet were confirmed in the checkout and corrected in the frontend only
(`usePlanningProposals.ts`, `PlanningWorkbookPanel.tsx`; no backend, contract or business-rule
change; no hidden-entry eligibility policy change):

1. **Obsolete draft reviews.** A per-assignment draft epoch advances when a save or delete starts
   and when it completes (including completions from an earlier workspace session). Starting a
   save/delete closes any open review and supersedes pending review, presence and older save/delete
   results (save and delete share one operation token). Presence/review results read before a later
   save/delete are dropped (a stale review reports "changed while it was being read"). Restore
   requires the review's epoch to still be current; otherwise it is refused and the rows are not
   labelled saved. Current proposals are kept on every failure and cancellation.
2. **Workspace sessions.** Each visit to an assignment starts a new workspace session; every
   asynchronous result is applied only to the session that issued it (plus its operation token), so
   A → B → A cannot let an earlier request mark the new session saved or show an old review.
3. **Undo boundaries.** Keystrokes coalesce only within one cell-editing session, which ends when
   focus leaves the cell and at save, delete, review, restore, Remove, Clear All and Undo.

Verification after the corrections (offline): 9 new frontend tests (8 panel, 1 App-level A → B → A
delayed save with an equal edit counter and a different value, confirming the new edits stay
unsaved and navigation is still protected); the 8 that target the findings fail against the
pre-correction hook, and removing only the restore epoch check fails the overwritten-review test.
Full frontend 541 passed, 5 skipped (546) on three of six full runs; the other three each had
failures only in unchanged locked tests (a Stage 13 `OpenOrdersPanel` Save As test timing out at
5 s under load, twice; a Stage 11 `LongTermShortagesPanel` Edge-spawn scaling test, once, which also
failed once when run alone). Typecheck, scoped lint, production build and `git diff --check` passed;
full lint shows only the two known Stage 10 errors. Backend checks were not repeated (no backend or
contract change; last 1,084/1,084).

**Not verified / limits.** No owner review yet. No live QAD, sidecar republish, Rust work or connected
desktop run; `beforeunload` behavior in the Tauri window is unverified. E1–E5 and Q-N4 (price
ties/currency) remain unverified. Post-export lifecycle (Q-N3) belongs to 14.6.

## 17. Checkpoint 14.5 Master plan UI redesign — OWNER-ACCEPTED with presentation amendments — 2026-10-06

**Authorization and inputs.** The owner directed replacement of the 14.5 Planning presentation with
the owner's Master plan design: `docs/reference/MP_UI_update/DESIGN_SPEC.md` (governs visual
differences), `master-plan-prototype.html` and `KSTv2_Planning_Redesign_Export.zip` (design
references only; the prototype's data, arithmetic and operations are simulated). 14.5 remains
**IMPLEMENTED / PENDING OWNER REVIEW**; 14.6 is not started. A pre-redesign tree hash, status and
copies of the affected frontend files were captured outside the repository first.

**Owner decisions (2026-10-06; supersede the specification's open questions).** Q-R1 whole-workspace
totals with the two dollar totals pinned and quantity/adjustment totals through View; Q-R2 Planning
follows the global density preference; Q-R3 entries outside the visible horizon remain listed and are
not by themselves export-blocking (§10).

**What changed (frontend only; no backend, contract, calculation-rule or fixture change).**

- `PlanningWorkbookPanel.tsx` now composes separate components under `components/planning/`:
  `PlanningToolbar`, `PlanningGrid` (model blocks), `PlanningViewPanel`, `PlanningChangesPanel`,
  `PlanningDialog`/`PlanningDialogs` (saved-draft comparison, inert forecast stub) and
  `planningLabels`. The ten-row layout and its row bands were removed, not hidden. New pure
  presentation helpers are in `planningWorkbook/planningView.ts`; per-assignment View preferences in
  `hooks/usePlanningViewPreferences.ts`. `PlanningWorkbookPanel.css` was rewritten on the existing
  theme, accent and global density tokens.
- **Modes.** Planning opens in report mode (loaded values only; rows Sales orders, MPS, Projected on
  hand = Est. On Hand). *Start planning* switches locally with no request, adds the weekly Adjust row
  and opens Changes; MPS shows proposed MPS and Projected on hand shows adjusted inventory (no extra
  rows). *Back to report* keeps every entry; *Resume planning · n* returns. SO $, MPS $ and Forecast
  (QAD) are optional rows through View. Draft availability, review and delete are reachable in both
  modes; unsaved-navigation protection is unchanged.
- **Grid.** One sticky column-header row with abbreviated Monday labels (full date in the accessible
  name/`data-week`) and current-week emphasis; a fiscal-quarter total after each quarter's last
  visible week, whose tooltip/accessible text gives dates, partial coverage and Falldown inclusion;
  pinned model column; sticky model headers; collapsible blocks with Collapse all/Expand all;
  collapsed blocks show the mode's projected inventory with a negative-inventory health strip; neutral
  rows with small colour keys; purple for staged changes, red for invalid proposals and negative
  inventory. Changed cells show the proposed value with the loaded value struck through, and the
  loaded value in the tooltip. Unchanged exact zeros stay blank (§15); a displayed result brought to
  zero by a staged change shows `0`. Money precision, valid zero prices, unpriced indicators and
  negative-inventory semantics are unchanged. Short counts use visible weekly inventory only
  (excluding Falldown): Est. On Hand in report mode, Adj On Hand in planning mode.
- **Totals.** Computed with the existing exact-decimal functions from the complete workbook before
  any View/filter is applied, labelled "all n models"; MPS $ totals stay blank (with the unpriced
  count and explanation) when any workspace part is unpriced, even if it is hidden. Inventory is never
  totalled. Totals are pinned to the bottom of the grid.
- **View.** Model search/checkboxes (Show all, Only short models), row and total choices, and a note
  that density follows General. Chips All models/Short (+ Changed in planning mode) filter the
  tailored set without changing it. Hidden models, rows, totals and collapsed models persist per
  assignment (`kst.planningWorkbook.view.v1.{assignmentId}`), are reconciled to the current scope on
  load and ignore malformed values; the active chip is not persisted. Hide, collapse and filter never
  change proposals, drafts or the eventual export scope. The horizon input became a stepper with the
  same API-derived maximum, 12-week minimum and stored preference.
- **Changes panel** (one right-hand slot shared with View; consumes width, never pushes the grid
  down). Lists every staged entry and invalid input in the existing order, including hidden models,
  hidden weeks, passed weeks and out-of-scope parts, with issue explanations, Remove, Undo, confirmed
  Discard all and save state. Entries not shown at the current horizon are labelled as such with their
  baseline unchecked; the panel never describes them as invalid and never claims they were validated.
  The net figure (signed units, and adjustment × selected price in dollars, hidden entries included,
  entries with issues excluded) is labelled "(priced parts only)" and explains the exclusion whenever
  any entry is for an unpriced or out-of-scope part. Saved-versus-current comparison moved into a modal dialog that keeps explicit
  restore confirmation, Cancel/Escape, corruption handling and Delete saved draft.
- **Calculation/state changes (narrow, owner-directed).** `planningAdjustments.ts`: `outside-horizon`
  and `beyond-maximum` became a separate `hidden` state with `hiddenCount`; they no longer count as
  issues or make `isValid` false (Q-R3). `usePlanningProposals.ts`: closed the remaining reported
  safety gap — a save/delete started in an earlier visit that completes after a restore in the current
  session now leaves that session unsaved and navigation-protected (a `savedEpoch` recorded with the
  saved version must equal the assignment's current draft epoch).
- **Forecast stub and export boundary.** *Import forecast* opens an inert dialog (Soon/Future
  iteration wording; no file input, parsing, sample injection or automatic adjustment). *Review and
  export* is disabled with an explanation that export arrives in 14.6; no export is simulated, no
  timestamp is manufactured and no file is written.
- **Not implemented (outside this checkpoint):** the prototype's shell compaction, roadmap block,
  planned-only MPS marking and off-Monday MPS detection. The existing application shell is retained,
  so the grid is shorter than in the prototype.
- **Fonts.** The prototype's embedded IBM Plex WOFF2 files are unknown-origin subsets without license
  text; under OFL-1.1 (Reserved Font Name "Plex") they would be Modified Versions, so provenance and
  license could not be verified and they were **not** bundled. No font files or dependencies were
  added; the existing font configuration is unchanged.

**Recorded for the 14.6 export prompt (not decided here):** (1) the redesign's column order *part
number, due date, change quantity* must be reconciled with the confirmed §7 order and the golden
fixture before implementation; (2) the preview must match the actual generated file; (3) accepted
encoding, line endings and explicit Save As stay unless the owner changes them; (4) the post-export
lifecycle (Q-N3) remains undecided; (5) planned-only and off-Monday behavior require real backend data
and separate authorization.

**Verification (offline, automated/mock only).** Frontend typecheck passed; full suite **559 passed,
5 skipped (564)** on three of four full runs, including the first two consecutively (the other run
failed only the unchanged Stage 11 `LongTermShortagesPanel` Edge-spawn scaling test, which also
fails when run alone, as recorded in §16) (18 new: 15 redesign tests, 2 A → B → A
save/delete-after-restore regressions, 1 hidden-entry calculation test; existing layout-dependent
Planning tests were updated to the new interface with fixture arithmetic expectations unchanged);
production build passed; scoped lint clean; full lint shows only the two known Stage 10 errors;
`check-version.ps1` at `0.1.0-alpha.14`; `git diff --check` clean. Mutation checks (restored
byte-for-byte): ignoring the saved epoch fails exactly the two new restore regressions; computing
totals from visible models fails the two whole-workspace-total tests. Backend checks were not repeated
(no backend or contract change; last 1,084/1,084). Offline mocked screenshots at 1440 × 900 and
1024 × 900 (dark and light, both modes, panels, invalid entries, draft review, scrolling, comfortable
density) were captured from the real application shell with a temporary harness that was removed.

**Not verified / limits.** No owner review of the redesign yet. No live QAD, sidecar republish, Rust
work or connected desktop run; native confirmation dialogs, the Tauri window and the packaged font
are unverified. E1–E5 and Q-N4 remain unverified.

**Review corrections (2026-10-06, still pending owner review).** Two findings were confirmed in the
checkout and corrected in the frontend only (`usePlanningProposals.ts`, `PlanningWorkbookPanel.tsx`,
`planning/PlanningDialog.tsx`, `PlanningDialogs.tsx`, `PlanningToolbar.tsx`; no backend, contract,
arithmetic or fixture change):

1. **Obsolete draft-presence checks.** A presence response invalidated by a draft-epoch change was
   discarded and left the session in "checking" with no Review and no Retry; a response read before an
   earlier visit's save/delete finished could also leave obsolete availability displayed. The session
   now records the epoch at which presence was established (`presenceEpoch`). When it no longer matches
   the assignment's epoch and the session has no save/delete of its own in progress, presence is shown
   as checking and checked again once per epoch change (a check never changes the epoch, so it cannot
   loop); a failed recheck shows the existing Retry and never retries by itself. Both orderings and
   failed earlier saves/deletes are covered. Nothing is restored automatically, Planning and MPS are not
   refreshed, and session/request/epoch guards, proposals and unsaved-change warnings are unchanged.
2. **Focus after saved-draft review.** The Review control is replaced while the dialog is open, so the
   dialog captured no useful opener. `PlanningDialog` accepts a `returnFocus` resolver evaluated at
   close; the review dialog returns focus to this workspace's Review saved draft control, or when it is
   unavailable (deletion in progress, workspace change) to the mode button (Start/Resume planning or
   Back to report). Initial focus, Tab trapping, Escape and the forecast dialog's return focus are
   unchanged.

Verification: 11 new tests (4 presence, 7 focus/dialog); with the previous production files the 9
targeted tests fail (4 with only the previous hook, 5 with only the previous dialog/panel files) and
the Tab-trap and forecast-focus preservation tests pass; making a failed check retry by itself fails
the no-loop test. Full frontend **570 passed, 5 skipped (575)** on two consecutive runs; typecheck,
scoped lint, production build, `check-version.ps1` (`0.1.0-alpha.14`) and `git diff --check` passed;
full lint only the two known Stage 10 errors. Backend not repeated (no backend/contract change).

**Owner Master plan amendment (2026-10-06, still pending owner review of the amended screen).** The
owner tested the Planning sheet and reported that it looks and works well, subject to four
presentation amendments, implemented in the frontend only (`PlanningWorkbookPanel.css`,
`planning/PlanningGrid.tsx`, `planning/PlanningViewPanel.tsx`, new `planning/planningColumnSizing.ts`,
`planningWorkbook/planningView.ts`, `PlanningWorkbookPanel.tsx`; no hook, draft, calculation, API,
contract or fixture change):

1. **Row tint.** Planning-scoped tokens wash Sales orders/SO $ green (rgb 58,160,114), MPS/MPS $ amber
   (200,150,72) and Adjust purple (139,92,246) at 7.5% in value cells and 15% in pinned labels, and the
   matching workspace total rows; Forecast (muted text) and Projected on hand stay neutral. Cell
   backgrounds are composited layers (base → column band → balance lift → tint → state → hover), so
   Falldown, current-week and quarter shading stay underneath and changed/negative states stay above
   the wash. View has a **Row color → Tint rows by type** checkbox (default on), stored as `tintRows` in
   the existing per-assignment key `kst.planningWorkbook.view.v1.{assignmentId}`; records without the
   field or with a non-boolean value read as on, an explicit `false` stays off, and other saved choices
   are kept. Off removes only the row washes.
2. **Balance line.** Projected on hand has a 1 px rule above it (#44546c dark), primary semi-bold text and
   a 3.5% neutral lift (light-theme equivalents); ordinary Sales orders/MPS values are regular weight in
   #d7dee8 (dark). Quarter totals, workspace totals and state colours keep their emphasis.
3. **Hover crosshair.** CSS `:hover` lights the row; one delegated pointer handler on the grid container
   writes `data-hover-col` (no React state), and generated `nth-child` rules light the column in the
   header, model headers, data rows and totals, including Falldown and interleaved quarter columns. Row
   labels light their row only; leaving the grid clears it.
4. **Content-sized value columns.** Minimums follow the global density (week 80/88 px compact/
   comfortable, quarter 108/116 px). After every grid commit, and on density/theme attribute or font
   loading changes, `sizePlanningColumns` measures the rendered text (headers, values, totals, optional
   rows, collapsed inventory, proposed results and adjustment-input text via a hidden measuring span)
   plus padding and borders, and sets one shared width for all week columns (Falldown included) and one
   for all quarter columns. The struck loaded value is shown only when it fits beside the complete new
   value; otherwise it is hidden and stays in the tooltip. The fixed character-width estimate was
   removed. Formats, font size and values are unchanged; the grid scrolls horizontally.

To keep normal grid text at ≥ 4.5:1 on every composited background, grid-scoped text tokens were added
(changed, negative, struck, muted, warning and current-week label colours per theme); state colours
keep their hues.

Verification (offline, mocked): 9 tests added (tint default, toggling without request/proposal change,
persistence across remounts and workspaces, older/malformed records, crosshair attribute with zero
React commits, sizing helpers and measured widths/strike fallback). Mutations (stored tint ignored,
malformed read as off, hover handler removed, sizing only on mount) each fail their targeted tests.
Full frontend **577 passed, 5 skipped (582)** on three consecutive runs (a net increase of 7 over
the prior 570 passed / 575 total: 9 tests were added by the amendment; the pre-amendment file copies
were not retained, so the two-test difference cannot be attributed to named tests from retained
evidence — the suite totals, not the count of added tests, are the evidence of record), after one earlier run had two
load-related timeouts (Stage 13 Save As and one Planning test) that led to a cached-style measuring
optimisation; typecheck, scoped lint, production build, `check-version.ps1` (`0.1.0-alpha.14`) and
`git diff --check` passed; full lint only the two known Stage 10 errors. Headless Chrome with IBM Plex
loaded (40 scenario/viewport combinations; both themes, densities and modes, optional rows/totals,
panels, x/y scrolling, long quantities and cents): 0 clipped primary values of 5,928 checked, uniform
widths, no width below its minimum, no struck-value overlap, minimum contrast 5.41:1 (4.66:1 with the
hover wash), crosshair 0 missing/0 extra cells with header alignment 0 px, 0 React commits during
pointer movement, input focus kept while columns widen. These are mocked browser renders; real-data,
Tauri window and desktop-font review remain owner checks.

**Owner acceptance (2026-10-06).** The project owner accepted Checkpoint 14.5 — Planning adjustment
staging and saved drafts — including the Master plan redesign, the review corrections and the four
presentation amendments: **Checkpoint 14.5 — OWNER-ACCEPTED with Master plan redesign and
presentation amendments — 2026-10-06.** This records the owner's review; the evidence above is
automated, offline and mocked (no live QAD, no sidecar/Rust, no connected desktop run). Still open and
not established by this acceptance: live-source evidence E1–E5; desktop/native (Tauri window,
desktop font) confirmation; the Stage 14.6 decisions, including reconciling the redesign's CSV
column order (part, due date, quantity) with the §7 golden order; and licensing/provenance of IBM
Plex assets (not bundled; fonts still load from Google Fonts at runtime). Checkpoint 14.6 is not
started and requires separate authorization. The owner design inputs
`docs/reference/MP_UI_update/master-plan-prototype.html` and `KSTv2_Planning_Redesign_Export.zip` are
retained locally, **untracked** (prototype fonts of unverified provenance/licence; review archive);
`DESIGN_SPEC.md` is tracked.

# Stage 18 Component MRP — source and decision ledger

**Component MRP COMPLETE / OWNER-ACCEPTED — 2026-10-09. Stage18 IN PROGRESS.**
See [closeout](KST_v2_STAGE_18_COMPONENT_MRP_CLOSEOUT.md) for owner generation/saving observations,
independent F1 resolution and retained verification limits. Historical pending/blocker statements
below are preserved provenance. Next report order: Shipments-To-Go, S&OP, Open Order Report.

**I3.3 current implementation:** D34 — owner authorizes bounded host-native Windows Save As
candidate integration for D32's single confirmation; no new dependency/features/capabilities.
Single Windows prompt implemented; I3.2 blocked statements below are historical. See
[I3.3 record](KST_v2_STAGE_18_I33_IMPLEMENTATION.md) for actual isolated interaction evidence,
candidate binding and pending independent/owner desktop verification. Stage18unaccepted.

## I3.2 owner amendments — 2026-10-09

- **D31:** Remove Inspection Leadtime, Cumulative Leadtime and Time Fence from exported metadata
  only. Repack remaining fields compactly; source fields/calculations/contracts retained.
- **D32:** One overwrite confirmation inside Save As, conditional on establishing controlled dialog
  completion and distinguishing approved replacement from a new-file late collision. Desired behavior
  authorized; **not delivered in I3.2** because installed path-only interface cannot establish that
  distinction. Existing second confirmation retained. See I3.2 record for exact limitation/remedy.
- **D33:** Safety Stock highlighting remains undecided; retain negative-only red Projected QOH rule.

Owner reports rebuilt sidecar/app, improved formatting, successful saves and automatic filename
increments, with two prompts on manually selected existing filename. These are owner-run observations,
not comprehensive lifecycle/numerical verification or acceptance. I3.1 independent adjudication pending.

**Current I3 owner amendments:** D26–D30 in [I3 implementation](KST_v2_STAGE_18_I3_IMPLEMENTATION.md)
supersede earlier seven-row/default-off/new-file-only presentation: five rows, no panes, default-on
POs/amber, Day/Week/calendarMonth, host-confirmed overwrite and Letter contiguous printing.
Historical IDs/evidence/calculation arithmetic retained. Implementation pending independent review
and owner verification/acceptance; Stage18 not complete.

Date: 2026-10-08. Current checkpoint: **18-I2 implemented, pending independent review and owner verification/acceptance**.
Current scope/production behavior: [I2 record](KST_v2_STAGE_18_I2_IMPLEMENTATION.md).
I1 partial/blocker statements below are historical; owner removed continuous preventive namespace
binding as prerequisite, retaining documented local-program manipulation limitation. Native save/open
is now implemented, not yet accepted. Original decisions, arithmetic and evidence remain intact.
**Owner provenance:** amended D2 documentation, D3 evidence review and D4 design review accepted;
initial delivery is **new files only**, overwrite **DEFERRED**. This acceptance is documentation/
review acceptance, not implementation or mechanism admission. D5/D5.1 are supplied partial/diagnostic
evidence; no acceptance is inferred. D19–D23 record separate owner direction below.
Implementation authorized by owner's replacement I1 prompt. Report/backend/workbook/UI implemented;
native report save/open fail closed with an exact path-binding blocker. See
[I1 implementation record](KST_v2_STAGE_18_I1_IMPLEMENTATION.md). Application alpha.18; installer0.1.0.
Stage18 incomplete; no owner acceptance or live-source evidence inferred. Prior design below remains
proposed wherever not established by the I1 record.
Companion: [Safe-save design](KST_v2_STAGE_18_SAFE_SAVE_DESIGN.md).

**I2F owner-approved presentation amendment (separate provenance, 2026-10-08): D25** — compact
single-sheet grouped metadata above the seven-row weekly grid; readable numeric/UOM formats,
negative balance highlighting, compact freezes and horizontal landscape continuation. Required
fields/warnings and unchanged calculated/stored values retained. See
[I2F formatting record](KST_v2_STAGE_18_I2F_FORMATTING.md). This is presentation authorization,
not I2 security acceptance or Stage18 completion. Earlier layout evidence remains historical.

## 1. Authority, baseline and provenance

Authority reloaded: `AGENTS.md`; `docs/development/KST v2 Project Instructions — Local Agent
Addendum.md`; `docs/status/CURRENT_PROJECT_STATUS.md:104–132,318–350,885–907`;
`KST-v2-Master-Project-Checklist.md:1561–1617,1662–1695`;
`docs/status/RELEASE_1_SCOPE_REVIEW_2026-09-29.md:44–125`; architecture boundaries and API contract
workflow; `SECURITY.md`, Security Assurance, Development Environment Security, Application
Security Profile, Dependency Admission and AI Security Review policies; Stage 8 accepted
component mappings; accepted Stage 11 algorithm; export/save source; versioning documentation.

The 18-D1 conversation packet is **Windows offline inspection evidence**, supplied and reviewed
by the owner. Material locators below were re-inspected in 18-D2. The owner's 18-D2 instruction
explicitly supplies the accepted requirements in §2; review of 18-D1 is not converted into
unspecified acceptance of every proposal in that packet.

| Baseline item | Fresh observation |
|---|---|
| Repository / branch | `C:\Dev\kst_v2`, `main` |
| HEAD | `05e110912ee2001453e919ac890cdf012ebb27e4` |
| Accepted Stage 15 commit | `1e678826ab0f60d30beb47b50ea234ce94d51fbe`, present in local history |
| Recorded upstream | `origin/main`; local comparison `0 / 0` |
| Remote status | **Not refreshed**; no fetch/pull; tracking equality is not fresh remote evidence |
| Current accepted state | Stage 14, Stage 15 and Inventory / Lot Locations follow-up complete/owner-accepted; Stages 9–11 locked |
| Version | `0.1.0-alpha.15`; numeric installer `0.1.0`; no transition at 18-D2 |
| Existing target files | Neither authorized document existed before this checkpoint |

Initial working tree: modified `src/frontend/tsconfig.tsbuildinfo`,
`src/tauri/gen/schemas/desktop-schema.json`, `src/tauri/gen/schemas/windows-schema.json`, and
untracked empty root `STAGE_14_IMPLEMENTATION_PLAN.md`. These are pre-existing work.
Current Git content hashes (`git hash-object`, without writing objects), not historical restore targets:

| Preserved file | Initial content hash |
|---|---|
| `src/frontend/tsconfig.tsbuildinfo` | `7921279a77839ed818626b2bf20f1b22e19a341d` |
| `src/tauri/gen/schemas/desktop-schema.json` | `33f3c488a744df770279b86c1d89ce7f2d4aa78b` |
| `src/tauri/gen/schemas/windows-schema.json` | `33f3c488a744df770279b86c1d89ce7f2d4aa78b` |
| `STAGE_14_IMPLEMENTATION_PLAN.md` | `e69de29bb2d1d6434b8b29ae775ad8c2e48c5391` (empty) |

Evidence vocabulary:

- **Owner-decided:** explicit requirements supplied in the 18-D2 instruction.
- **Repository/offline:** current file inspection, including existing test definitions; no execution.
- **Historical live-read:** earlier documented source observations, with their scope/date limits.
- **Owner-run:** earlier owner observations recorded in accepted closeouts; not agent-run here.
- **Synthetic/proposed** or **mocked/proposed:** fixtures/checks below, not executed.
- **Unverified:** business/source or platform guarantee not established by this inspection.

Stage 8 QCTC schema/source evidence is historical KTC/SW evidence, not a new cross-site probe.
Older Stage 11-A plans/ledgers contain superseded calendars, inventory and netting rules.
`docs/implementation/stage11_component_mrp_algorithm.md` and accepted code govern calculation.
Stage 14/15 price-currency decisions do not establish QCTC currency.

## 2. Accepted owner decisions

Decisions D01–D14 have **owner provenance: supplied original 18-D2 instruction, 2026-10-08**.

| ID | Accepted requirement |
|---|---|
| D01 | Stage 18 sequence: Component MRP → Shipments-To-Go → S&OP → Open Order Report. S&OP includes fiscal monthly/period totals; detailed S&OP design is later scope. |
| D02 | Independent Reports entry beside General, left-hand selector. Layout may be refined in desktop review. |
| D03 | Supported sites: SW, NW, AR, MN, MS, KV. KS/KTS has closed and is **excluded, not deferred**. Default SW on first use; remember selection between sessions. |
| D04 | One directly entered part. Any valid master in the selected site's domain is eligible, including purchased, manufactured, healthy and unknown-P/M parts. P/M is informational. No workspace/BOM or shortage-only gate. |
| D05 | Horizons 13/26/52/72 weeks, default 26. Include Unconfirmed Receipts available, default off. |
| D06 | Reuse Stage 11 calculations unchanged; numerical legacy departures are intentional. |
| D07 | Compact workbook: component information above metric rows; weeks across columns; Past Due first. Seven rows in the exact order in §4. |
| D08 | Calculated values, no editable projection formulas. Main quantity cells store UOM-rounded **numeric values**, matching Stage 11 export presentation; calculation retains full decimal precision. QCTC uses separate cost precision. |
| D09 | Header: component, description, site, UOM, status, selected-site Buyer/Planner, IOS, nettable opening, adjusted opening, agreed planning fields and report context. |
| D10 | Planning fields: time fence, safety time, safety stock, purchase/inspection/cumulative lead times, minimum order and order multiple. Site planning keyed by domain/site/part; no invented master fallback. Header Buyer/Planner is selected-site `ptp_buyer` only. |
| D11 | Drop AVL Cost; retain informational QCTC with meaningful precision. Currency remains unverified; no currency symbol or USD assumption. |
| D12 | Generate → Save As. Every Generate acquires fresh data and freezes request context. Save cancellation/failure retains the exact prepared workbook for retry with no source read. Input changes clear it and require regeneration. |
| D13 | Distinct prepared/saved/cancelled/failed feedback. Missing master → clear not-found. Missing site planning → report retained with unavailable fields flagged. Complete empty MRP → zero activity/calculated balances. Failed/incomplete acquisition → error, never zero fallback. |
| D14 | Failed save must preserve existing destination. Operation-owned temporary creation/cleanup and reviewed native safe-save design required. Existing direct-write behavior does not satisfy this requirement. |

### 2.1 Owner amendment — Open workbook after saving

**Separate provenance:** owner's subsequent documentation-only amendment instruction, 2026-10-08.
This is not acceptance of the whole safe-save proposal or implementation authorization. D01–D14
and existing fixture IDs remain intact.

| ID | Accepted amendment |
|---|---|
| D15 | Reports-form checkbox labeled **Open workbook after saving**. Initially unchecked; remembers choice between sessions. Standard native Save As remains. This is a save preference, not a calculation input: changing it does not invalidate prepared bytes, regenerate or read QAD. |
| D16 | Capture intent per save attempt. Checked intent requests OS-default-associated-application opening only after host-confirmed successful save commitment. Preparation, cancellation, precommit failure and unresolved save outcome never trigger opening. |
| D17 | Open failure leaves save successful. Show **“Workbook saved, but could not be opened”** and bounded **Open** retry for the already saved artifact; no resave, regeneration, source read or destination replacement. Successful launch request is not proof Excel/another application loaded the workbook. |
| D18 | Outcome polling/repeated completion handling must not duplicate automatic launches. No automatic opening after application restart; no general shell/path/URL-opening authority for the webview. |

Proposed lifecycle detail, consistent with companion §3.3: snapshot checkbox intent when each
Save As attempt starts. Changes during an active attempt are remembered for the next attempt;
they do not retroactively change that attempt, cancel it or alter B. Storage mechanism and invalid
stored-value policy are proposals (§9 O09), not owner decisions. Report input changes still revoke
preparation; this checkbox is explicitly outside D12/S03's invalidating input set.

Host creates a separately authorized saved-artifact record only after commitment, bound to the
save operation, main window and application session. It outlives report-input/prepared-generation
changes within that session so Open retry never requires regeneration. Retry revalidates the
committed file identity/content; disappeared/changed files do not get opened under the old grant.
Proposed lifetime ends at session/window close or host exit, without restart restoration. Mechanism,
identity/race guarantees, lifetime details and security admission remain proposed/unverified in
companion §3.3. Save/open outcomes are independent; save protection and owned-temp cleanup unchanged.

No accepted decision above authorizes implementation, capabilities/dependencies, live reads,
version changes, commits/pushes, Stage 21 completion or other report design.

### 2.2 Initial-delivery amendment — new files only

**Separate provenance:** owner direction recorded in accepted external D4 review, reaffirmed in
the 18-D5.2 instruction on 2026-10-08. Prior D01–D18 IDs/provenance are preserved.

| ID | Accepted amendment |
|---|---|
| D19 | Initial Component MRP delivery creates new files only. Existing-name collisions require another explicitly confirmed destination, retaining exact B and reading no source. Overwrite is deferred, not an initial-delivery verification requirement. D14 preservation still applies. |

### 2.3 Save-folder amendment — OWNER-APPROVED, distinct D5.2 provenance

**Separate provenance:** explicit owner's 18-D5.2 documentation amendment, 2026-10-08.
Requirements below are accepted; storage, interop and path-binding mechanisms are proposals.

| ID | Accepted amendment |
|---|---|
| D20 | First Save As starts in the current user's Windows-configured **Documents** folder, including configured redirection; never hardcode `C:\Users\…\Documents`. Every attempt lets the user choose another location. |
| D21 | Only a host-confirmed successful commit remembers the **actual committed parent** for that user across sessions. Subsequent Save As starts there with a fresh filename suggestion. Cancellation, failed/precommit saves and unresolved outcomes do not update it; opening failure does not undo saving or the folder update. |
| D22 | An unavailable last-successful folder falls back to configured Documents. If Documents is unavailable, open the chooser for explicit location selection; no autosave, auto-created report folder or silent relocation. Defaults/remembered paths are hints, never save authorization; host-mediated selection/validation occurs on every save. |
| D23 | Folder state is internal to the host: no arbitrary-path/directory IPC or frontend-storage authority. Folder and Open checkbox are not calculation inputs; retain B, no regeneration/source read, snapshot state per attempt. Persistence failure keeps session behavior and reports inability to remember without downgrading Saved. Site remains an invalidating report input. |

Recommended lifecycle/storage and Windows configured-folder resolution: companion §3.4.
Existing initially-unchecked remembered checkbox and independent saved-artifact retry D15–D18
remain intact. A previous folder is neither a persistent artifact-open grant nor a directory grant.

### 2.4 Owner-approved I2 scope amendment — separate provenance

| ID | Accepted amendment |
|---|---|
| D24 | Owner's explicit I2 implementation authorization removes continuous preventive selection/ancestor/reparse binding as prerequisite. Deliberate folder/link manipulation by another local program is documented limitation. Host selection, no-replace, owned cleanup, exact-byte retry, truthful unknown state, bounded grants and confirmed-save-only opening remain required. |

Implemented limits and route checks are technical I2 controls, not invented calculation/business rules;
see I2 record. Default Documents may be redirected/synced and does not establish support.

## 3. Source ledger — current locators

Locators are repository-relative, verified at the HEAD in §1. Aliases:

- **ALG:** `docs/implementation/stage11_component_mrp_algorithm.md`.
- **FACT:** `src/backend/Kst.Integrations.Qad/LongTermShortages/QadLongTermShortageSourceReader.cs`.
- **META:** `src/backend/Kst.Integrations.Qad/ComponentDetail/QadComponentSourceReader.cs`.
- **BUILD:** `src/backend/Kst.Domain/LongTermShortages/LongTermShortagesBuilder.cs`.
- **MODEL:** `src/backend/Kst.Domain/LongTermShortages/LongTermShortageModels.cs`.
- **CAL:** `src/backend/Kst.Domain/Mps/MpsBusinessCalendar.cs`.
- **XLSX:** `src/backend/Kst.Exports/PlaceholderExportService.cs`.

### 3.1 Identity and header

| Field/behavior | File/function/lines | Source/grain and transformation | Missing/failure behavior; provenance |
|---|---|---|---|
| Site/domain | `QadSiteDomainMap.cs` under `Kst.Integrations.Qad/Mps`, 9–35, `Resolve` | NW/SW/AR/MN/MS → KTC; KV → KTV; integration-owned, no frontend domain override | Invalid site rejected; KS excluded. Repository/offline + D03 |
| Component/master existence | META `BuildMasterPlanningQuery` 96–133; `ReadAsync` 53–65 | `pt_mstr.pt_domain + pt_part`; no master site filter | No master → not-found. Query failure → error, not not-found. Repository/offline |
| Description | META 106–107; `CombineDescription` 220–232 | `pt_desc1/pt_desc2`, domain/part; trim, omit blanks, single-space combine | Both empty → null; no invented text. Repository/offline |
| UOM | FACT `BuildBatchQuery` 63; BUILD 85; MODEL 31–35 | `pt_um`; trim/uppercase; EA/BX/PK zero places, other/blank/missing/unfamiliar two places, away-from-zero midpoint | Missing UOM uses accepted two-place rule, not acquisition failure. ALG 262–271 |
| Status | META 108; FACT 63; BUILD `DescribeStatus` 192–197 | `pt_status`, domain/part; informational raw code; proposed Stage 11 wording for description | Unknown description remains unavailable; no eligibility exclusion. ALG 242–260 |
| Effective P/M | FACT 68 | Nonblank `ptp_pm_code` else master `pt_pm_code`; P/M-only fallback | Unknown retained, report still eligible. Stage 8 closeout 67 + D04 |
| IOS | META 109, `Normalize` 200 | `pt_warr_cd`, domain/part; informational | Null retained |
| Buyer/Planner header | META 114,206 | `ptp_buyer`, domain/site/part; code retained, no master fallback or compulsory display lookup | Null/blank flagged unavailable, not substituted. D10; Stage 8 closeout 70–71 |
| Site planning presence | FACT 65–66,147; BUILD 175 | Explicit site-row presence via `ptp_part`; do not infer absence from all-null values | Absent row retained/warned. Present null field remains unknown. ALG 172 |
| Time fence | META 111,203 | `ptp_timefnce`, nullable integer, domain/site/part | Null unavailable; unit label open |
| Safety time | META 112,204; ALG 165 | `ptp_sfty_tme`, decimal working days; already reflected in QAD MRP dates | Null unavailable; never shift events again |
| Safety stock | META 113,205; FACT 65–67; BUILD 87–89 | `ptp_sfty_stk`, site authoritative; decimal evaluation threshold | Zero genuine; null unknown; no master fallback, no proven Healthy from unknown |
| Purchase lead time | META 115,207; ALG 167 | `ptp_pur_lead`, integer calendar days | Null unavailable; informational, not date transformation |
| Inspection lead time | META 116,208 | `ptp_ins_lead`, nullable integer | Null unavailable; working/calendar-day label open |
| Cumulative lead time | META 117,209; ALG 168 | `ptp_cum_lead`, integer calendar days, informational | Null unavailable |
| Minimum / multiple | META 118–119,210–211 | `ptp_ord_min/ptp_ord_mult`, nullable decimal | Null unavailable; zero retained; do not synthesize planned-order quantities |
| QCTC | META `BuildQctcQuery` 171–190, `Normalize` 202 | `Analysis.dbo.in_price.inp_qctc`; domain/site/part; source `qtbom_det`; latest `inp_start_date`; no effective-date bound | No qualifying row → null; genuine zero → zero; failed query → acquisition error. Equally latest conflicting rows unresolved |
| QCTC precision | Stage 8 backend plan 115–158; accepted Stage 8 closeout 74–75 | Historical `decimal(18,5)`; separate cost precision, e.g. 0.00152 remains meaningful | Currency unverified; no symbol. Historical live-read KTC/SW only; uniqueness then is not perpetual guarantee |
| Opening/adjusted quantities | FACT 55–62; BUILD 96–106 | Signed nettable aggregate and opening minus Past demand; header shows UOM presentation without changing raw calculation | Complete empty inventory → zero; failed inventory retrieval → error |
| Report context | MODEL 7–10; ALG 95–98,199–203,283; XLSX 14–17 | Frozen local as-of date, horizon/mode, acquisition UTC, `PRO2_READ_UNCOMMITTED`, near-real-time replica | No transactional-snapshot guarantee. Placement/extra metadata proposed |

All site planning joins use `ptp_domain + ptp_part + selected ptp_site` (META 123–129,
FACT 78–80). P/M-specific fallback is not a general planning-field fallback.
The Stage 11 fact query's planner/master fallback at FACT 64/80 must not supply the Stage 18 header.

### 3.2 Raw facts and integrity

| Field/behavior | File/function/lines | Source/grain and transformation | Missing/failure behavior; provenance |
|---|---|---|---|
| Signed nettable opening | FACT `BuildBatchQuery` 55–62; ALG 51–68 | `SUM(ld_qty_oh)` at domain/site/part; nonblank trimmed lot status else `loc_status`; nettable INSPECT/NCMINSP/RIP/STOCK/TRAN/VMI; location join domain/site/location; RA/RMA excluded | No positive-only restriction, hard-allocation subtraction, `in_mstr`, expiry or supplier/bond additions. Null lot fails existing SQL predicates. Preserve accepted query semantics |
| MRP acquisition | FACT 72–88 | Raw `mrp_det` at domain/site/part; due **or release** before exclusive horizon end; no lower bound eliminates Past | Completed empty result valid only after master existence established; incomplete/failed section is error |
| Quantity/date | FACT `ToInput` 134–148, `ToDateOnly` 173; BUILD 91–95 | Decimal quantity unrounded; source timestamps become dates; due/release distinct | Null quantity for MRP row, required demand/SUPPLY due date missing → error; no plausible zero/date substitution |
| Source identity | FACT 73–74; `SelectEvidence` 150–172 | Exact text `prrowid`, number, line, line2; preserve distinct IDs; deterministic ordinal/order | Blank ID or conflicting observations for same ID → error; identical repeated same-ID read collapses only that physical observation |
| PO identification | FACT 75 | `mrp_dataset='pod_det'`; receipt-level attribute, not part classification | Do not infer from PO-looking order text |
| Confirmation | FACT 76,84–87,137–138; BUILD 91–92 | Match domain/site/part/PO/line; `pod__log01` only when MatchCount=1 | Missing/ambiguous/null confirmation fails PO SUPPLY projection; never silently firm/unconfirmed |
| Completeness | FACT 39–42; service input guards at `LongTermShortagesService.cs:142–143` | Existing source requires every scope part and presentation section; Stage 18 needs equivalent required-section/count/scope guards for its narrower sections | Do not require omitted PO/KSS presentation. Do not reinterpret unavailable sections as empty success |
| Connection/calendar | `QadConnectionFactory.cs:13–33`; CAL 9–15; BUILD 82–84 | Parameterized, configured timeout/cancellation, READ UNCOMMITTED, short single-connection acquisition; Sunday start, Monday label | No database object/probe, isolation amendment or long transaction; replica consistency limitations disclosed |

### 3.3 Calculation and prepared-result behavior

| Behavior | Locator | Transformation / missing or failure meaning |
|---|---|---|
| Past Due cutoff | BUILD 96–106; ALG 93–106 | Due < local as-of; release < as-of for release evidence. Adjusted opening = signed opening − Past gross requirements. Past receipts visible but never relief |
| Demand routing | BUILD `Classify` 184–191, `Totals.Add` 32–45 | All `DEMAND*` with due date; site-wide, no parent allocation; SUPPLYF/other unclassified facts do not alter ledger |
| Daily modes/order | BUILD 121–160 | Demand first; firm/confirmed, then selected unconfirmed; planned receipts only planning. Post-demand low retained even if recovered same day |
| Planned events | MODEL 20; BUILD 103,118–119,132–133 | Due adds planning only; release is independently bucketed action evidence, never supply; lead/safety/order fields do not regenerate plans |
| Precision | MODEL 31–35; BUILD 28,85–89,110,139–151 | Full decimal precision and accepted expression/evidence sum order; round only for evaluation/presentation |
| Missing planning warning | BUILD 175; ALG 150,172 | Report retained, null threshold unknown; do not equate unknown with Healthy |
| Prepared lifecycle | D12–D14; companion design §§3–7 | New acquisition per Generate; immutable context/bytes; input changes invalidate; obsolete responses cannot publish/save; cancelled/failed save retains bytes; no QAD reread for save retry |
| Open-after-save preference / saved artifact | D15–D18; companion §3.3 | Attempt-frozen preference, default unchecked/persisted; postcommit opening only. Saved-artifact Open retry is independent of prepared invalidation; launch-request acknowledgement is not workbook-loaded proof |
| Existing export presentation | XLSX 22,30–32,55 | UOM-round numeric values before assigning cells; MemoryStream workbook bytes; no calculation ownership in frontend |

## 4. Seven-row workbook mapping

Main quantity cells store `LongTermQuantityPresentation.Round(raw, normalizedUom)` as numeric
cells, not string values or live projection formulas. This is D08, not the earlier D1 raw-cell
proposal. Rounding each event before summation is forbidden. Header quantity formatting should
follow the same UOM convention (proposal); QCTC is separate.

| Row order / label | Raw field before presentation rounding |
|---|---|
| 1 Gross Requirements | `bucket.GrossRequirements` |
| 2 Scheduled Receipts (selected mode) | `bucket.ScheduledReceipts + (includeUnconfirmed ? bucket.UnconfirmedReceipts : 0)` |
| 3 Lowest Projected Balance | `bucket.LowestProjectedBalance` |
| 4 Ending Projected Balance | `bucket.ProjectedQoh` |
| 5 Planned Orders Due | `bucket.PlannedOrdersDue` |
| 6 Ending Planning Balance | `includeUnconfirmed ? bucket.AllReceiptsPlanningEnding : bucket.PlanningEnding` |
| 7 Planned Orders Release | `bucket.PlannedOrdersRelease` |

Apply this mapping to Past and every week. Past low/ending/planning fields are adjusted opening;
Past scheduled/planned quantities are overdue information, not a recomputation of that balance.
Weekly lows use actual as-of-through-Saturday ledger days. Final boundary is exclusive.

## 5. Reuse and direct-acquisition design — proposals

Reuse unchanged: `BuildBoth`, quantity presentation, business-calendar helpers, accepted query and
physical-evidence semantics, QAD connection/parameter/timeout conventions, ClosedXML in Exports.
Do not alter Stage 9–11 algorithms, services, population or caches.

| Candidate | Why whole-reader/service reuse is unsuitable |
|---|---|
| `LongTermShortagesService.GetAsync:98–149`, export selection 169–197, eligibility 200–207 | Workspace/MPS/BOM population, P/M gates, cache/reprojection and cache-only export rules do not fit D04/D12 |
| FACT `ReadAsync:17–44` | Also acquires manufacturer/PO/KSS presentation (30–42,97–129); scope LEFT JOIN (77–83) can emit a row without master. Private `ToInput` is not a public reuse seam |
| META `ReadAsync:67–75` | Always acquires unrelated Standard Cost before QCTC; no UOM/P/M/explicit site-presence fields in that header query |
| `ComponentDetailService.ComposeAsync:121–143` | Adds Stage 6/8 positive-only inventory, which is not Stage 11 opening |
| `QadPartDetailReader.ReadAsync:67–83` | Adds unrelated inventory and price tiers; different header/planner semantics |

Proposed bounded adapter: on one connection, establish domain-level master; read selected-site
header/planning, accepted signed opening/MRP/confirmation facts and QCTC. Compose existing
query/evidence semantics narrowly; add only needed report metadata projections. No silent
refactor of locked readers, source-population change or new database object. Header description
and site-only planner are separate from calculation facts; empty parent attribution is legitimate.

**Adapter verification precedes builder comparison:** inspect mocked raw result sections,
scope/identity/date/quantity/confirmation mapping and explicit master/site presence independently.
Compare the produced `LongTermShortageInput` against hand-specified expectations, including every
evidence key and raw decimal. Passing identical inputs twice through the same builder proves
neither SQL correctness nor raw-row-to-input correctness.

Application owns acquisition/outcomes/frozen request; QAD owns SQL; Domain owns calculation;
Exports owns workbook; API owns authoritative DTO/OpenAPI; frontend owns controls and feedback;
native host owns the proposed protected file save. No Shortages DB, workspace cache, BOM read,
Stage 13 operational-validation requirement or automatic MPS load is inherited.

## 6. Intentional legacy differences

Read-only Windows legacy reference: `C:\Dev\kst\features\component_mrp.py`; archived VBA
`C:\Dev\Archived\vault\Projects\kst\raw\Modules\F_ComponentMRP.bas`. No Linux path assumed.

| Legacy locator / behavior | Stage 18 disposition |
|---|---|
| Python 435–474: `in_mstr` opening; planning join without selected site; master buyer; QCTC join without site/source/latest | Accepted Stage 11 signed opening and Stage 8 header/QCTC scope |
| 550–593: weekly pivot, all SUPPLY, per-fact integer rounding | Daily modes/lows, exact-date Past, sum full decimals then UOM presentation |
| 351–365: single Projected QOH formula row | Seven calculated metric rows; no editable projection formulas |
| 478–522: horizon derived from last due week | Selected 13/26/52/72 buckets |
| 281–283: two-place currency QCTC/AVL | AVL omitted; QCTC meaningful cost precision, currency unverified |
| 195–200: failed MRP can become empty DataFrame | Acquisition error, no misleading empty workbook |
| 76–98: workbook/Excel visible | Generate bytes then protected native Save As |

Numerical parity with this legacy is not required. Compare identical accepted Stage 11 inputs
for arithmetic; legacy workbook supports layout/metadata review and explanation of departures.
Representative owner-supplied legacy workbook/screenshots remain an evidence request.

## 7. Deterministic fixture specifications — synthetic/proposed, not executed

Unless stated otherwise: as-of **2026-09-24 (Thursday)**, Sunday start 2026-09-20, Monday label
2026-09-21, first active ledger days September 24–26; EA, safety stock 0, site planning present,
valid master, 26 weeks. Every event has a distinct valid physical ID except identity fixtures.
Quantities are synthetic. Tuple notation follows the seven rows: **(GR, SR, Low, End, Due,
PlanningEnd, Release)**. Stored tuple is numeric `Round(raw,UOM)` per cell. Unspecified flows 0.

### 7.1 Calculation fixtures: raw and stored values separately

| ID / input | Expected raw calculation | Expected stored UOM-rounded numeric values |
|---|---|---|
| F01 P/M/blank/unknown P/M, healthy; opening 10, no events, no workspace/BOM membership | One report each; Past and all weeks `(0,0,10,10,0,10,0)` | Same tuple; all requests eligible |
| F02 opening 10; Sep22 demand3; Sep23 confirmed PO100; Sep24 demand8 and confirmed PO5 | Adjusted opening7; Past `(3,100,7,7,0,7,0)`; W1 `(8,5,-1,4,0,4,0)`; recovery Sep24 after demand low | Same Past/W1 tuples; header opening10/adjusted7 |
| F03 opening5; Sep24 demand8, confirmed PO2, unconfirmed PO4 | Confirmed W1 `(8,2,-3,-1,0,-1,0)`; all-receipts W1 `(8,6,-3,3,0,3,0)` | Same tuples; all-receipts recovers Sep24, confirmed remains short |
| F04 opening0; Sep24 non-PO SUPPLY2; Sep25 demand1 | Both W1 `(1,2,0,1,0,1,0)`; initial0 is weekly minimum | Same tuple; non-PO confirmation does not gate supply |
| F05 opening2; Sep24 demand5; Sep25 confirmed receipt6 | W1 `(5,6,-3,3,0,3,0)`; shortage Sep24/recovery Sep25 | Same tuple; positive ending must not hide within-week shortage |
| F06 KG opening1; Sep24 two demand facts0.004 each, no receipts | W1 `(0.008,0,0.992,0.992,0,0.992,0)` | `(0.01,0,0.99,0.99,0,0.99,0)`; not a sum of two rounded zero demands |
| F07 EA/BX/PK opening +1.5 or -1.5, empty MRP | Each Past/week balance +1.5 or -1.5; all flows0 | Balance +2 or -2; midpoint away from zero |
| F08 KG/`unlisted`/blank/null UOM opening +1.245 or -1.245, empty MRP | Raw balances +1.245/-1.245 throughout; normalized unfamiliar UOM retained | +1.25/-1.25; missing/unfamiliar UOM uses2 places, no invented warning |
| F09 nettable STOCK+10, TRAN-12, excluded MRB+100 and RA-lot+50; empty MRP | Signed opening/adjusted -2; Past/all weeks `(0,0,-2,-2,0,-2,0)` | Same tuple; excludes positive-only inventory substitution |
| F10 opening0; SUPPLYP qty6 releaseSep23 dueSep25; SUPPLYP qty4 releaseSep24 dueSep28 | Past `(0,0,0,0,0,0,6)`; W1 `(0,0,0,0,6,6,4)`; W2 `(0,0,0,0,4,10,0)` | Same tuples; releases never inventory; due/release different buckets |
| F11 opening0; overdue SUPPLYP7 dueSep23/releaseSep22 | Past `(0,0,0,0,7,0,7)`; W1 balances0, flows0 | Same; overdue planned receipt not carried into planning inventory |
| F12 valid master, KG opening12.5, complete empty MRP | Past/all weeks `(0,0,12.5,12.5,0,12.5,0)` | Same numeric values (two-place formatting may show12.50); no missing-part inference |
| F13 missing site row, opening5, empty MRP; master planning values deliberately nonzero | Balance5, planning/header nulls; SafetyStockUnavailable, warning | Main balances5; unavailable fields not0 and not substituted master values |
| F14 SUPPLYF100 plus demand2, opening10 | SUPPLYF retained evidence but no relief; W1 `(2,0,8,8,0,8,0)` | Same tuple |

### 7.2 Mapping, integrity and error fixtures

| ID / raw acquisition or request | Expected raw mapping / outcome | Stored workbook outcome |
|---|---|---|
| A01 identical observation of demand4 ID A twice, ID B demand4 with identical business fields, opening10 | Evidence A+B (2 facts), demand8, low/end2 | GR8, low/end/planning2; never demand4 or12 |
| A02 same ID A observed qty4 and5, or missing ID | Integrity error, no projection | No prepared workbook |
| A03 PO SUPPLY2; MatchCount0/2, or MatchCount1 confirmation null | Unknown/ambiguous confirmation error in both modes; no conversion to unconfirmed | No workbook |
| A04 opening0; Sep24 confirmed PO0.125, unconfirmed PO0.125, non-PO0.125; exact number/line/line2/text IDs | Assert field-for-field mapping first. Confirmed SR/end0.25, all SR/end0.375, both lows0; no numeric ID conversion or quantity rounding | EA SR/end0 in both modes; KG confirmed0.25, all0.38; low0. Rounding occurs after aggregation |
| A05 no master but scope placeholder row with opening0 | Not-found, not empty MRP success | No workbook |
| A06 timeout, failed QCTC query, missing required section, null MRP quantity, DEMAND/SUPPLY missing due | Failed/incomplete acquisition; cancellation distinct; no fabricated inputs | No workbook, never zero-filled success |
| A07 complete inventory no qualifying rows with master present | Opening0; empty MRP balance0 | Numeric0 balances, valid report |
| A08 selected site/domain mismatch, missing/duplicate ambiguous component input | Narrow scope/completeness rejection proposed; no first-row arbitrary metadata selection | No workbook pending correctness resolution |
| Q01 no qualifying QCTC row | QCTC null | Explicit unavailable, not numeric0 |
| Q02 qualifying QCTC0 | QCTC decimal0 | Numeric0, no currency symbol |
| Q03 qualifying0.00152 plus later `pid_det`9 and other-site `qtbom_det`8 | QCTC0.00152; nonqualifying feeds/sites excluded |0.00152, not UOM-rounded or displayed as0.00 |
| Q04 qualifying start2026-10-01 value0.00234 vs earlier qualifying0.00152 | Latest qualifying0.00234; no added as-of bound |0.00234; no currency assertion |
| Q05 equally latest qualifying rows with different costs | **Expected disposition unresolved**; no arbitrary tie-breaker introduced | Fixture held for owner/source decision, not given a fabricated passing result |

### 7.3 Horizon/calendar fixtures

For each H in 13/26/52/72 define E = 2026-09-20 + 7H days. Input opening0;
SUPPLYP2 due E−1/release E−8; SUPPLYP3 due E/release E−1.
Expected raw final week: `(0,0,0,0,2,2,3)`; previous week Release2. Due3 at E excluded;
its release3 retained because release<E. Stored EA tuples identical. Exactly H week buckets.

Also specify:

- As-of Sunday 2026-09-20, demand1 that day, opening2: W1 GR1/low1/end1; labelSep21.
  Stored EA GR1/low1/end1.
- As-of Saturday 2026-09-26, Sep25 demand1 and Sep26 demand1, opening3: PastGR1/adjusted2;
  one-day W1 GR1/low1/end1. Stored EA values identical.
- As-of Thursday 2026-12-31, opening2/demand1 that day: weekStartDec27, labelDec28,
  end-of-first-bucket2027-01-02 balance1. Raw/storedEA1.
- As-of Tuesday 2028-02-29, opening2/demand1: startFeb27, labelFeb28, SaturdayMar4 balance1.
  Raw/storedEA1. No fiscal-calendar transformation in Component MRP.
- Events exactly at as-of are current, not Past; exactly E never contribute due/release metrics.
  Include midnight/year rollover in frozen-request tests, not a moving-clock acquisition.

### 7.4 Prepared bytes and save fixtures — mocked/proposed

| ID | Expected outcome |
|---|---|
| S01 Generate freezes site/part/H/mode/date, complete preparation B | Prepared state references exactly B; no Saved feedback yet |
| S02 Save As cancelled or precommit save failure | Prepared B retained; retry uses byte-identical B, source-read count unchanged |
| S03 Change any report input, including receipt mode, while prepared | Prepared invalidated; old token cannot save; regeneration acquires fresh facts |
| S04 Generate A then B; A completes late / input changes during acquisition | A cannot publish prepared state or open dialog; B context only |
| S05 Commit succeeds but response lost | Host operation outcome queried by operation ID; no blind second replacement; failure/cancelled not asserted for already committed file |
| S06 Existing destination locked/denied/write or replacement failure | Preservation of prior contents must be demonstrated before safe-save design admission; see companion checks |
| S07 Retry after cancellation chooses another destination | New host-mediated destination grant, same B; no reusable arbitrary path authority |
| S08 Unchecked save succeeds | Saved B, no automatic launch; initial preference false, no source reread |
| S09 Checked save succeeds | After host confirms commit, one automatic request to default associated application for saved XLSX; no launch before commit, no workbook-loaded claim |
| S10 Checked preparation/cancel/precommit failure/unresolved outcome | Zero launch requests; cancelled/failed save retains B; outcome unresolved stays distinct |
| S11 Save succeeds, opening fails | Save remains successful; “Workbook saved, but could not be opened”; bounded Open retry enabled only for saved-artifact grant |
| S12 Open retry after report inputs change/preparation cleared | Same saved artifact verified/open requested; zero saves/replacements/regenerations/source reads; saved grant not coupled to current prepared ID |
| S13 Commit response lost, outcome polled/repeated completions | Host operation has at most one claimed automatic launch. Read-only polling never launches; uncertain launch not auto-replayed; no auto-opening on restart |
| S14 Checkbox toggled before/during save | B/context/digest unchanged; no QAD read; current attempt uses start snapshot, later attempt uses new persisted preference |
| S15 Saved artifact disappears/changes, wrong window/session or duplicate Open request | No unauthorized launch or resave; old grant rejected/revoked, save history stays successful; retry request ID deduplicated and one open attempt in flight |

**S06 initial-scope explanatory note (original ID/specification above preserved):** replacement
failure belongs to deferred overwrite. Initial verification exercises existing-name collision,
locked/denied destinations and no-replace refusal with original contents AND original pathname
unchanged; no overwrite implementation/admission is required for initial delivery.

New folder fixtures — synthetic/mocked/proposed, not executed:

| ID | Expected outcome |
|---|---|
| S16 First save / configured redirected Documents | Host resolves current user's configured Documents, suggests fresh name, chooser allows another location; no hardcoded profile path, no write before selection/validation. |
| S17 Successful save chooses different directory | Actual committed parent becomes session hint and persistent per-user hint; restart uses it with fresh suggestion; B/context/source count unchanged. |
| S18 Cancel, precommit failure, collision or OutcomeUnknown | Prior last-successful folder unchanged. Collision retry confirms new destination and retains exact B, no new source acquisition. |
| S19 Last folder unavailable, Documents available/unavailable | Fall back to configured Documents; if unavailable chooser asks explicit location. No autosave, silent relocation or report-directory creation. Unsupported selected target rejected by save envelope independently of default. |
| S20 Commit succeeds then Open fails | Saved and actual-parent update remain; accepted open-failure message and independent artifact retry; no resave or folder rollback. |
| S21 Missing/malformed/wrong-version/oversized preference, persistence unavailable/write failure | Strict host load validation, safe default/hint fallback; session choice kept; bounded notice that preference cannot be remembered; successful commit remains Saved. No path from page/storage becomes grant. |
| S22 Preference changes during attempt / concurrent writes / duplicate completion | Attempt's hint and checkbox intent frozen; later attempts see latest session state. Serialize host updates; one update per committed operation, duplicate completion/poll causes no launch or stale folder rollback. |
| S23 Folder/checkbox change versus site input change | Folder/checkbox leave immutable B and generation intact; site change still invalidates preparation. Saved artifact retry stays independent and cannot set folder from a frontend path. |

## 8. Validation proposal and inspected existing evidence

No tests/builds/diagnostics were run in 18-D2. Inspected definitions:

- `src/backend/tests/Kst.Domain.Tests/LongTermShortages/LongTermShortagesBuilderTests.cs:64–205`:
  demand-first modes, overdue/SUPPLYF, episodes, unknown confirmation/date, missing planning/UOM.
- `.../Kst.Domain.Tests/LongTermShortages/DenseLedgerEquivalenceTests.cs:13–40`:
  all horizons, preserved indexed comparison and decimal operation order.
- `.../Kst.Integrations.Qad.Tests/LongTermShortages/QadLongTermShortageSourceReaderTests.cs:21–100`:
  query shape, identity, confirmation and presentation separation.
- `.../Kst.Integrations.Qad.Tests/ComponentDetail/QadComponentSourceReaderTests.cs:94–108`:
  accepted QCTC filter/order shape.
- `.../Kst.Domain.Tests/LongTermShortages/LongTermShortagesWorkbookTests.cs:9–35`:
  values/types and existing report metadata conventions.

Later sequence: independent raw mapping fixtures → hand-worked arithmetic → same-input Stage 11
projection comparison → stored rounded cell/type/no-formula inspection → lifecycle mock tests →
separately authorized native save and owner desktop checks. ZIP byte equality is appropriate for
retry of one prepared artifact; workbook regeneration comparisons should use semantic cells/types,
not assume archive timestamps make bytes deterministic. Live comparisons require separate approval.

Known exceptions preserved for future scoped verification: broad .NET/Rust formatting failures,
two locked Stage 10 lint errors, earlier frontend timeouts before reduced-contention successful
reruns, and reported AFT diagnostic gaps. None is a clean pass or corrected by this checkpoint.

## 9. Open questions and implementation proposals

| ID | Open/proposed disposition |
|---|---|
| O01 | QCTC currency and conflicting equally latest qualifying rows remain unresolved. No silent tie-breaker/ambiguity change to shared behavior. |
| O02 | Time-fence and inspection-lead-time unit labels unresolved; proposed unit-neutral labels until evidence. |
| O03 | Historical D2 filename proposal: `Component-MRP-{site}-{safe-part}-{asOf-yyyy-MM-dd}-{H}w-{Confirmed-or-AllReceipts}.xlsx`. Current D4/D5.2 recommendation: `{safe-part}_MRP_{site}_{frozen-asof-yyyy-MM-dd}_{sequence}.xlsx`. Not an accepted business identity. Sanitize filename only; retain full part/context in header. Collisions require explicit confirmation of changed destination and exact-B retry, never automatic sequence publication/replacement. |
| O04 | Metadata/warnings proposal: compact header with as-of/partial week/horizon/mode/acquisition UTC/replica consistency and unavailable flags; optional separate evidence sheet undecided. |
| O05 | Phase progress and cancellation boundaries proposed in safe-save design; no fictitious percentage. |
| O06 | Preference storage proposed global feature-specific localStorage key; backend JSON alternative exists. Invalid/removed saved site handling proposed SW plus visible reset notice; owner choice pending. KS must never become an eligible site. |
| O07 | Excel numeric precision is finite; UOM rounding does not guarantee arbitrary decimal representability. Later round-trip/significant-digit checks required; exact-text companion or limit policy unresolved. Do not change accepted numeric main cells silently. |
| O08 | Native safe-save design, trust boundary and supported destination guarantees require review/admission; existing path cannot meet D14. |
| O09 | Checkbox persistence is owner-approved, storage is not. Historical D2 alternatives: separate feature save-preference key/backend preference. Current D5.2 recommendation: host fixed-name versioned JSON alongside host-only last-successful-parent hint (§3.4 companion/O12); strict boolean, malformed/missing → unchecked, nonblocking storage failure. Site preference/calculation identity stays separate. |
| O10 | Native associated-application opening mechanism, saved-artifact identity/race/lifetime and deduplication design require security/platform review. Companion §3.3 proposes host-only ShellExecuteExW with fixed open verb; existing shell plugin does not establish safe local-file opening. |
| O11 | Current new-file candidate: ordinary FileRenameInfo, NULL RootDirectory, host-resolved fully qualified destination, ReplaceIfExists FALSE; no CWD dependence. Retained source/ancestor protections must bind absolute creation AND publication before any write. D5.1 controls do not establish race/collision/cleanup safety; companion §§2,3.2,4–8. |
| O12 | D20–D23 folder behavior is accepted; recommend fixed-name versioned host JSON under current-user LocalAppData/KST/config using existing Rust std::fs/serde_json, no path IPC/JS store plugin. Strict schema/access/bounds/session/failure policy in companion §3.4 remains proposed; new native authority/API still needs review. |

Bounded later slices (separate authorization each): resolve decisions/native mechanism evidence;
offline direct adapter + independent mapping validation; report orchestration + compact workbook;
Reports controls/preference/prepared lifecycle; reviewed native safe-save; integrated verification;
separately authorized source/owner desktop acceptance. Extract shared output helpers only after
the next report demonstrates a real second use. Do not rebuild accepted exports or create a
speculative report framework. Stage 21 inventory remains separate.

Future contract work: authoritative C# DTO/OpenAPI specification → regenerated TypeScript client
→ matching backend → frontend → sidecar republish before desktop testing. No such work now.
Propose alpha.18/activeStage18 only at a separately authorized write/implementation checkpoint,
following `docs/development/VERSIONING.md:154–169`; installer stays0.1.0. Current alpha.15 remains.

## 10. 18-D2 review boundary

**Historical checkpoint record retained:** the baseline/provenance below describes D2, not current
pending acceptance. D5.2 amended only these two documents, with external baseline copies/offline
diff and fresh preservation evidence. Current new-file and folder requirements are §§2.2–2.3;
native mechanism readiness remains unverified. Original §7.1–§7.3 arithmetic/calendar text is
preserved exactly; new S16–S23 fixtures have no executed result. No stage/status/version transition.

Amendment fresh offline baseline: same main/HEAD as §1; recorded origin/main comparison 0/0,
without fetch. Both review documents existed and matched completed D2 contents before edits:
ledger Git content hash `ba9523fb3ffdaf1bf5b7d50f5066ab3cb28031a7`, safe-save
`4648d51d24c35d921a51ebf6ec4626b8c9b16075`. Four unrelated protected items matched fresh
hashes in §1; these are preservation observations, not restoration targets. New S08–S15 fixtures
are proposed, not executed. Native opening inspection is offline evidence only.

Only this ledger and its companion are authorized writes. Content/locators/arithmetic and final
Git status/diff/protected hashes are reviewed offline; no builds/tests/diagnostics or filesystem
save experiments. Agent completion is not owner acceptance. Stop for owner review; acceptance
does not authorize implementation.

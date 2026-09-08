# KST v2 -- Stage 9.7 Scenario Coverage Audit

**Checkpoint:** 9.7 -- Automated Verification / Total Scenario-Coverage Gap Audit  
**Date:** 2026-09-04  
**Status:** COMPLETE / ACCEPTED — retained automated-audit evidence.  
**Scope:** Automated evidence only. No live-QAD validation, manual desktop regression, closeout, or owner acceptance is claimed by this document.

## 1. Audit Basis

This audit reconciles the accepted Stage 9A scenario baseline with subsequent accepted decisions and corrections. The authoritative current implementation evidence is the Stage 9 Domain, Application, QAD, API, and frontend code and its deterministic tests. The original baseline contained 35 scenarios; later amendments split that baseline into the current rule groups below rather than preserving the historical count.

Authority used:

- `AGENTS.md`.
- `docs/implementation/KST_v2_STAGE_9_IMPLEMENTATION_PLAN.md`.
- `docs/implementation/KST_v2_STAGE_9_SOURCE_MAPPING.md`, including the hybrid allocation decision.
- Accepted Stage 7R planning-window contract and accepted Stage 8 BOM behavior.
- Owner-accepted Stage 9.7 blocker corrections #1 through #7.

Current layers: Domain calculation and classifications; Application requirement resolution, allocation orchestration, cache, and result construction; QAD read-only source readers; API DTO/result projection; generated OpenAPI/TypeScript contract; frontend presentation and workflow tests.

### Classification

- **DIRECT**: an automated test explicitly proves the accepted rule.
- **COMPOSITE**: a small, appropriate combination of direct tests proves the rule.
- **SUPERSEDED**: an original rule was replaced by a later accepted decision.
- **NOT AUTOMATABLE HERE**: requires live-QAD facts or owner/manual evidence assigned to a later checkpoint.
- **MISSING**: no adequate automated evidence. There are no unexplained MISSING rows at this checkpoint.

## 2. Amendments and Reconciliation

| Amendment | Current binding behavior | Audit treatment |
| --- | --- | --- |
| Stage 7R | Population is Falldown plus active-basis Weeks 0-3; Falldown is always due-date based; parent and nested WO population use the four-week planning window. | Current rule. |
| Stage 9.1 hard/detail amendment | `lad_det` hard reservations are authoritative; reconstructed R-to-A applies only to residual free inventory. | Current rule. |
| Stage 9.3 projected UOM correction | Projected UOM is `pt_mstr.pt_um`, applied after extension/consolidation. | Current rule. |
| Stage 9.4 public-result correction | Allocation provenance is a public immediate-material result fact. | Current rule. |
| Manufactured-subassembly correction | Navigation is truthful candidate-WO navigation; no parent/subassembly pegging is fabricated. | Current rule. |
| Stage 9.6 UX amendments | One selected-WO analysis under the card strip and a retained WO-specific Shortages tab; hierarchical Escape. | Current rule. |
| Purchased-material amendment | Master P/M `M` is visible/drillable NotApplicable; only nonblank master non-M participates; unavailable master P/M is Unknown/Data Issue. | Current rule. |
| Original manufactured-shortage scenario | Manufactured components participate in shortage arithmetic like ordinary components. | **SUPERSEDED**. Do not restore it. |
| Original `Due This Week`, receipt coverage, severity, and MPS-bucket shortage scenarios | These were replaced or excluded by Stage 9A/current amendments. | **SUPERSEDED**. |

The Master Checklist's older wording that manufactured subassemblies are ordinary shortage components and its Stage 9 progress markers are stale descriptive material. Reconcile that wording during Stage 9.10; it was not silently used as the current rule here.

## 3. Scenario Coverage Matrix

| ID / rule | Accepted behavior | Status | Test project and evidence |
| --- | --- | --- | --- |
| S9-001 Grain | One visible row per WO + component; no combined cross-WO result. | DIRECT | Application: `Uncommitted_WorkOrders_In_The_Same_Analysis_Read_The_Same_AdvisoryPool_Without_Consuming_It`. |
| S9-002 Window | Falldown plus Weeks 0-3, exclusive end, closed/RMABOM excluded. | COMPOSITE | QAD planning-window reader tests; `CommittedPopulation_Is_SiteWide_WindowBounded_And_Limited_To_R_And_A`. |
| S9-003 Basis | Falldown uses due date; forward placement follows Due/Release basis. | DIRECT | QAD planning-window query tests; Application: `Falldown_Is_DueDate_Sorted_In_Release_Basis`. |
| S9-004 Nested WO | Candidate subassembly WO is valid in its own planning population, without pegging. | DIRECT | Application: `Manufactured_Subassembly_WorkOrder_Is_Analyzed_Without_TopLevel_MpsParentMembership`; API nested-subassembly test. |
| S9-005 Navigation validation | Only a genuinely absent selected WO returns NotInImmediateWindow. | DIRECT | Application: `Missing_WorkOrder_And_PlanningWindow_Membership_Are_Rejected`. |
| S9-006 Actual authority | R, A, and E + type F use actual `wod_det`. | DIRECT | Application: `RequirementAuthority_Follows_Accepted_Status_Rule`. |
| S9-007 Projected authority | Ordinary E, F, P, including lowercase e, use projected BOM. | DIRECT | Application: authority theory and `Lowercase_E_Status_Uses_Projected_Bom_Requirements`. |
| S9-008 Unsupported state | Unsupported unfinished state is loaded analysis-level Data Issue. | DIRECT | Application: `Unsupported_Unfinished_Status_Returns_Loaded_Analysis_DataIssue`. |
| S9-009 Source exclusivity | Actual and projected requirements are not mixed for one WO analysis. | COMPOSITE | Application authority theory asserts one source/read path per status; API source projection tests. |
| S9-010 Actual EA | Each actual EA line floors before issued subtraction/consolidation. | DIRECT | Application: `Actual_Ea_Requirements_Are_Floored_Per_Line_Before_Issued_Subtraction`. |
| S9-011 Actual issue facts | Remaining clamps; signed variance and >100% issued remain visible; zero required percent is null. | DIRECT | Domain: remaining/issue-presentation/zero-percent tests. |
| S9-012 Projected build | Ordered minus completed minus rejected, clamped at zero. | DIRECT | Domain: `ProjectedBuildQuantity_Subtracts_Completed_And_Rejected_Then_Clamps`. |
| S9-013 Effective BOM | Release date is effective date, otherwise Today; presentation basis does not redefine it. | DIRECT | Application: projected release-date and Today-fallback tests. |
| S9-014 BOM expansion | Phantom explosion, hidden phantom output, path extension, consolidation, then EA floor. | COMPOSITE | Domain projected requirement test; Application projected phantom/consolidation test; Stage 8 BOM tests. |
| S9-015 Projected UOM | `pt_mstr.pt_um` is projected UOM authority; non-EA remains decimal; missing/inconsistent UOM is Unknown. | DIRECT | QAD BOM normalization tests; Application projected non-EA/null-UOM tests. |
| S9-016 Projected issue facts | Projected issued, variance, percent, and over-issued fields are null/N/A. | DIRECT | Domain issue-presentation test; API projected mapping test; frontend grid test. |
| S9-017 Actual empty | Successful zero-row Actual material result is Loaded, empty, and non-diagnostic. | DIRECT | Application: `Actual_Empty_Material_Read_Returns_Loaded_Empty_Analysis_Without_DataIssue`. |
| S9-018 Master P/M scope | Nonblank master non-M is eligible; master M is manufactured; null/blank/whitespace is Unknown. | COMPOSITE | QAD actual normalization reliability test; Application Actual and Projected master-P/M reliability tests. |
| S9-019 Effective versus master P/M | Stage 8 effective site P/M is retained separately; Stage 9 projected classification uses master P/M only. | DIRECT | QAD BOM effective/master preservation tests; Application contradictory-override tests. |
| S9-020 Manufactured result | Manufactured row remains visible/drillable, NotApplicable, null Short, normal null diagnostic, and no PO inference. | COMPOSITE | Domain manufactured test; Application manufactured-only test; API manufactured mapping; frontend manufactured navigation test. |
| S9-021 Hard identity | `lad_det` provides WOID/operation/component directly; no `wod_det` existence predicate. | DIRECT | QAD: `HardAllocation_Uses_Lad_Identity_Without_A_Wod_Existence_Predicate_And_Has_No_Window_Predicate`. |
| S9-022 Hard usability | Only usable hard reservations count; hard reservations do not override inventory usability. | DIRECT | QAD hard reader predicate test and Domain hard-coverage evaluation tests. |
| S9-023 Hard coverage | Own hard coverage is clamped; not double-counted; multiple operations aggregate. | COMPOSITE | Domain hard coverage tests; Application hard aggregation test. |
| S9-024 External hard | Other-WO and outside-window hard reservations reduce free inventory. | DIRECT | Application: `HardAllocation_Is_Credited_Once_And_External_OutsideWindow_Reservation_Reduces_FreePool`. |
| S9-025 Soft allocation exclusion | `wod_qty_all` is not a second Stage 9 coverage source. | DIRECT | QAD hard-reader query test asserts its absence. |
| S9-026 R-to-A allocation | Residual inventory is site-wide, committed R then A, sequential. | DIRECT | Domain committed allocation tests; QAD committed-population query test. |
| S9-027 Ordering | Falldown/Forward-Due order by due then WOID; Forward-Release by release then WOID. | DIRECT | Domain ordering tests; Application Release and Falldown regression tests. |
| S9-028 Advisory pool | Uncommitted E/F/P and E+F inspect one residual pool independently; no combined shortage. | COMPOSITE | Domain advisory calculation test; Application independent-pool tests. |
| S9-029 Statuses | Unknown precedes Short, then On Hand; Unknown Short is null; calculated zero is On Hand. | COMPOSITE | Domain Unknown/status tests; frontend ordering test; API Unknown mapping. |
| S9-030 Issue policy | Site policy, then master, then true default; informational only. | COMPOSITE | QAD independent site-first SQL topology test; Application floor-stock informational test. |
| S9-031 Inventory | Positive Stock/nettable/non-RMA/non-expiring is usable; six activity buckets remain separate context. | DIRECT | QAD inventory predicate and normalization tests. |
| S9-032 Issue days | Expiry cutoff uses Today plus site/domain issue days. | DIRECT | QAD inventory cutoff and `IssueDays_Is_Scoped_To_Domain_And_Site`. |
| S9-033 PO qualification | PO is context only; earliest positive-open line, line c/x exclusion, confirmation informational, tracking/KSS retained. | DIRECT | QAD next-PO query/normalization tests; Application KSS/NO-PO test. |
| S9-034 PO applicability | Only valid Short purchased rows receive PO/NO-PO/KSS; On Hand, Unknown, and manufactured do not. | COMPOSITE | Application no-PO tests; API Unknown/manufactured mapping; frontend KSS/NO-PO tests. |
| S9-035 Data issue vs unavailable | Data issues are loaded 200 results; technical source failure is Unavailable/503, not a component Unknown. | DIRECT | Application source/data-issue tests; API status outcome tests. |
| S9-036 Cache | Cache identity includes workspace, snapshot, parent, basis, and business date; no prior-day stale result. | DIRECT | Application cache snapshot/date/basis/no-stale tests. |
| S9-037 Provenance | Allocation mode and all accepted hard/residual provenance fields reach public result without API recalculation. | DIRECT | API: `GetImmediateMaterial_Maps_HardAllocation_And_Advisory_Provenance` and committed mapping assertions. |
| S9-038 API | Route validates query inputs; 200/404/409/503 outcomes and nullable fields project correctly. | COMPOSITE | API detail and summary outcome, Validation Problem Details, actual/projected/unknown/manufactured mapping tests. |
| S9-039 Generated contract | C# DTO drives OpenAPI then generated TypeScript, including allocation provenance. | COMPOSITE | Backend OpenAPI generation, `npm run generate:types`, frontend typecheck. |
| S9-040 Architecture/read-only | Application stays behind interfaces; API is projection/DI; emitted QAD SQL is parameterized/read-only. | COMPOSITE | Architecture `DependencyRuleTests`; QAD `QadReadOnlySqlTests`. |
| S9-041 Card summary | WO card state derives from accepted summary flags: Short, Data Issue, clear, unavailable. | DIRECT | API summary tests; frontend `WorkOrderCard` state matrix. |
| S9-042 Workflow | Show/Hide Material Lines opens one selected-WO analysis beneath the strip; retired Select for Shortages is absent. | DIRECT | Frontend Stage 9.6 integrated workflow test. |
| S9-043 Shortages tab | Work Orders to Shortages to Work Orders retains selected WO and analysis identity. | DIRECT | Frontend integrated workflow test. |
| S9-044 Grid/detail | Required grid values/order, N/A, empty/error/retry, detail, activity, and PO context render truthfully. | COMPOSITE | `ShortagesPanel` tests. |
| S9-045 Candidate UX | Manufactured candidates are drillable, can be empty normally, and one branch is open at a time. | DIRECT | Frontend manufactured candidate, empty-state, and one-branch tests. |
| S9-046 Escape | Detail, candidate, material panel, Work Orders, and root unwind one level at a time with card-toggle focus restoration. | DIRECT | Frontend component-detail and two Escape hierarchy tests. |
| S9-047 Exclusions | No combined netting, receipt coverage, clear dates, MRP, substitutes, pegging, or MPS-bucket shortage claim. | COMPOSITE | Plan out-of-scope boundary, one-WO-component model, API/frontend result shapes, and no MPS marker behavior. |
| S9-048 Live source reconciliation | Real QAD Actual/Projected authority, effective dates, allocation, inventory state, and issue-policy results match ground truth. | NOT AUTOMATABLE HERE | Requires read-only Stage 9.8 QAD validation. |
| S9-049 PO master status | Qualifying and excluded `po_mstr.po_stat` values are established before a master-status predicate is added. | NOT AUTOMATABLE HERE | Existing evidence does not establish qualifying values; requires Stage 9.8 live-QAD plus owner evidence. |
| S9-050 Desktop workflow | Tauri sidecar lifecycle, visual layout, and scheduler guided workflow are validated. | NOT AUTOMATABLE HERE | Assigned to Stage 9.9 owner-guided desktop regression. |
| S9-051 Original manufactured-shortage scenario | Manufactured components participate in purchased-material shortage arithmetic like ordinary components. | SUPERSEDED | Replaced by the owner-accepted master-P/M purchased-material rule; manufactured rows are visible/drillable NotApplicable results. |

## 4. Frontend Skipped-Test Disposition

All remaining skips are in `src/frontend/src/components/MpsWorkspace.test.tsx` and are explained below. No Stage 9-relevant current behavior is left unexplained.

| Skipped test/group | Disposition | Current evidence |
| --- | --- | --- |
| `expanding a card lazily loads material lines, and collapsing hides them again` | Obsolete. It asserts the retired standalone `/work-orders/{woid}/material` grid. | Stage 9.6 integrated Show/Hide immediate-material workflow test. |
| `shows a deliberate empty message ... no applicable material lines` | Obsolete legacy endpoint wording. | `ShortagesPanel` successful empty-components test. |
| `Stage 7D.8 kitting material grid` group | Unrelated to Stage 9. It remains legacy Stage 7 component coverage. | `WorkOrderMaterialGrid.test.tsx` has direct coverage; it is not counted as Stage 9 evidence. |

The former multiple-independently-expanded-card skip was replaced by the passing current-flow test `keeps one immediate-material analysis open when another Work Order is selected`.

## 5. Stage 9.7 Defects Found and Corrected

The correction history has seven numbered blocker corrections but eight discrete defects because correction #5 contained independent Shortages-tab and master-P/M corrections.

| Correction | Defect and accepted correction evidence |
| --- | --- |
| #1 | Hard reader required `wod_det` existence; corrected to direct usable `lad_det` reservations. |
| #2 | Successful empty Actual material analysis was mistaken for absence from the planning window; corrected to Loaded empty analysis. |
| #3 | Site issue policy depended on master-row existence; corrected to independent site/master joins. |
| #4 | Selected immediate-material panel skipped directly to root on Escape; corrected with the existing LIFO Escape stack and focus restoration. |
| #5A | Shortages tab cleared its selected analysis; corrected to retain the same selected identity in Work Orders and Shortages. |
| #5B | Projected classification used Stage 8 effective P/M; corrected to use retained master P/M and classify unavailable master P/M as Unknown. |
| #6 | Allocation provenance was dropped at the API boundary; corrected by public DTO/OpenAPI/generated TypeScript projection. |
| #7 | Actual reader converted missing master P/M into purchased eligibility; corrected with an explicit reliability signal and Unknown result behavior. |

## 6. Gaps and Deferred Evidence

### Missing automated coverage

None. The matrix has **zero unexplained MISSING** scenarios.

### Historical Stage 9.8 handoff

- Establish qualifying/excluded `po_mstr.po_stat` values before adding a master-status predicate. Current line-status evidence remains implemented; no master-status values are inferred.
- Compare Actual/Projected authority, effective BOM dates, hard allocations, Stock/activity/expiration classification, R-to-A ordering, issue policy, and PO/KSS results with read-only ground-truth QAD queries.
- Confirm cross-site/domain operational data and reader performance characteristics.

### Historical Stage 9.10 handoff

- Reconcile stale Stage 9 implementation-plan/checklist progress markers with accepted implementation/Stage 9.7 evidence.
- Replace stale manufactured-shortage wording with the binding purchased-material-only rule.
- Reconcile the historical baseline count and amendments in formal closeout documentation.

## 7. Audit Result

The current accepted scenario inventory contains **51** grouped rules: **32 DIRECT**, **15 COMPOSITE**, **1 SUPERSEDED**, **3 NOT AUTOMATABLE HERE**, and **0 MISSING**. The three non-automatable rows were assigned to later checkpoints at the time of this audit; their final dispositions are recorded in `KST_v2_STAGE_9_CLOSEOUT.md`.

No accepted Stage 9 production-behavior defects remain identified by the completed automated gap audit.

# Release 1 Scope Review and Revised Roadmap

Date: 2026-09-29

Status: **OWNER-APPROVED PLANNING BASELINE**

Implementation authorization: **NONE**

Current planning version: **`0.1.0-alpha.13`**. During numbered alpha development, Stage `N` uses
`0.1.0-alpha.N`; this version identifier does not itself authorize Stage 13 implementation.

This document records the full-scope review performed after accepted Stage 11 closure. It updates
future-stage disposition without rewriting accepted implementation history. The master checklist
contains the executable roadmap detail; `docs/status/CURRENT_PROJECT_STATUS.md` states the current
position.

### Current milestone amendment — 2026-10-22

**Stage 22 COMPLETE / OWNER-ACCEPTED; delivered baseline frozen for Stage 23.** Owner: “Very well.
Stage 22 is accepted.” This approves delivered Ergonomics B depth (S4 closed at accepted scope;
S1–S3/S5 deferred to Stage 27 with triggers below), the documented bounded workflow coverage amendment
to master checklist §22.1 without claiming an exhaustive scheduler walkthrough, and completed/deferred/
later-stage dispositions. Frozen accepted source: `889175611d49a7975f30cccc0de7f9a61919a17d`.
The final four-file documentation commit is the Stage 23 starting reference, not a changed source
baseline. Earlier delivery/disposition dates are preserved. Stage 23 is **ready for planning / not
started**; execution requires separate dispatch. Deployment remains **owner/IT-paused**, with applicable
Stage 24–26 gates intact. Failures/warnings/skips/partial diagnostics and security/licensing obligations
remain exceptions/obligations, not passes, entitlement or security-risk acceptance.

The original 2026-09-29 planning version, map and non-Stage-22 stage rows below are historical planning
context, not a reset of subsequent accepted deliveries; current status and the master checklist govern
present state. No version bump or new implementation authorization is made here.

## Current-state map

```text
Accepted foundations and product capability
  Stages 1–8 + R0 + S0 + Navigation A
                    |
                    v
  Stage 9 Immediate Shortages ----+
  Stage 10 Component Orders       +-- COMPLETE / ACCEPTED / LOCKED
  Stage 11 Workspace Shortages ---+
                    |
                    v
Remaining Release 1 product work
  13 Open Orders -> 14 Planning Workbook -> 15 Finished Goods
  -> 18 Standalone Reports -> 19 Historical Shipments
                    |
                    v
Cross-cutting completion and release gates
  21 Export Completion -> 22 Refinement & Optimization
  -> 23 Quality & Hardening -> 24 Readiness -> 25 Pilot -> 26 Rollout

Post-release: Stage 27
```

Stages 12, 17, and 20 are retired. Stage 16 is absorbed into Stage 18. Their numbers remain visible
for provenance; later historical references do not need reinterpretation.

## Stage-disposition matrix

| Stage | Disposition | Release 1 outcome |
|---|---|---|
| 12 — Multi-Part Shortage Analysis | **RETIRED / SUPERSEDED** | Accepted Stage 11 supplies the relevant workspace capability. |
| 13 — Customer Open Orders | **PLAN ACCEPTED / REQUIRED** | Workspace-scoped report and Planning Mode for staged date, quantity, and price changes. Implementation is not yet authorized. |
| 14 — Planning Workbook | **PLANNED / REQUIRED** | Reordered behind Open Orders. |
| 15 — Finished Goods | **REQUIRED; 15.1 ACCEPTED / 15.2 AUTHORIZED — 2026-10-07 amendment** | Workspace-scoped current-inventory inspection/filtering; replaces original immediate-demand coverage. Five columns, informational RMA, exact USD MAX valuation. Hold deferred to Open Orders; no date/demand/readiness/coverage. |
| 16 — General Open Orders | **ABSORBED** | Its cross-customer report outcome is included in Stage 18, not workspace Stage 13. |
| 17 — General WO Variance | **RETIRED / NO LONGER DESIRED** | Existing Stage 7 capability remains; no standalone search/report. |
| 18 — Standalone Excel Report Generators | **PLANNED / REQUIRED** | Component MRP, cross-customer Open Order Report, Shipments-To-Go, and S&OP. |
| 19 — Historical Shipments | **PLANNED / REQUIRED** | Historical shipment search/report, distinct from Shipments-To-Go. |
| 20 — Legacy Simulation | **RETIRED / NO LONGER DESIRED** | No compatibility migration; remove from readiness criteria. |
| 21 — Cross-Cutting Export Completion | **PLANNED / REQUIRED** | Inventory all application areas, decide export need, implement gaps, and validate consistency. |
| 22 — Refinement & Optimization | **COMPLETE / OWNER-ACCEPTED — 2026-10-22** | Delivered refinement baseline frozen at `889175611d49a7975f30cccc0de7f9a61919a17d`; bounded workflow coverage and Ergonomics B depth/dispositions approved. |
| 23 — Quality and Hardening | **READY FOR PLANNING / REQUIRED / NOT STARTED** | Validate and harden the frozen Stage 22 product baseline after separate execution dispatch; no deployment authorization. |
| 24 — Release 1 Readiness | **PLANNED / REQUIRED** | Packaging, deployment, operational, security, and release decisions. |
| 25 — Pilot | **PLANNED / REQUIRED** | Controlled initial-site pilot with KST v1 fallback. |
| 26 — Incremental Multi-Site Rollout | **PLANNED / REQUIRED** | Site-by-site validation and deployment. |
| 27 — Post-Release Roadmap | **POST-RELEASE** | QAD upgrade preparation, Can-Build discovery, and separately authorized future concepts. |

### Concrete Component Orders layout follow-up — 2026-10-21

Existing **S22-007**, with **S22-018/030** scroll/focus cross-reference: owner reports horizontal
scrollbar access fails at smaller windows. This is a current bounded layout defect, **not** the deferred
Component Orders workflow/redesign/performance proposals below. Local panel/table flex containment,
wrapping controls and named keyboard scrolling are **COMPLETE / OWNER-ACCEPTED / BOUNDED CLOSED
— 2026-10-21**. Owner: “I checked it. The scrollbar works. Accepted.” This records reported scrolling
acceptance, not every suggested desktop check or exact-hash confirmation. The scrolling hold is removed.
No ancestor/shared styles, KSS-only semantics, source/cache/locked algorithms or broader scope change.
Verification/artifact/limits: `docs/implementation/KST_v2_S22_007_COMPONENT_ORDERS_SCROLL.md`.
Other dated dispositions and existing in-progress reconciliation remain intact and separate from the
authorized local fix commit/necessary acceptance records; no Stage 22 closure or baseline freeze.

## Deferred-work registry

| Item | Source/evidence | Disposition and destination | Trigger | Release blocking? |
|---|---|---|---|---|
| Individual / Single-Part Component MRP | Stage 11 closeout backlog | **Stage 18 — required report generator** | Stage 18 authorization | Yes, as part of approved Stage 18 report set |
| Cross-customer Open Order Report | Owner decision amended during Stage 13 planning | **Stage 18 — required report generator** | Stage 18 authorization | Yes |
| Shipments-To-Go and S&OP | Existing master checklist + owner confirmation | **Stage 18 — required report generators** | Stage 18 authorization | Yes |
| Historical Shipments report | Existing Stage 19 + owner confirmation | **Stage 19 — required** | Stage 19 authorization | Yes |
| Inventory/Lot Locations in Component Information | Stage 8 deferred scope; owner brought forward 2026-10-08 | **Bounded Stage 8 follow-up before Stage 18 — COMPLETE / OWNER-ACCEPTED, including quantity-display amendment** | Owner acceptance 2026-10-08; `KST_v2_STAGE_8_INVENTORY_LOCATIONS_FOLLOWUP.md` | Accepted |
| Show MRP; Extended Requirement; Incoming Supply; Coverage / Material Status | Stage 8 deferred scope; accepted S22-033 | **Review/disposition complete**; Show MRP/navigation accepted; remaining analytical proposals retained separately below | Concrete workflow plus approved source/rule/fixture triggers in `KST_v2_S22_BOM_SHOW_MRP.md` | No current Stage 22 blocker; deferred proposals not implemented |
| Remaining component supply/PO coverage | Stage 8/9/10 deferred scope; S22-007/032/033 | **Disposition complete**; post-publication workflow feedback / separately scoped analytical proposal | Demonstrated workflow gap and approved bounded scope/source/rules; no silent locked-algorithm changes | No current Stage 22 blocker; future material defect explicitly triaged |
| Configurable application-view exports | Accepted Stage 21 and S22-035 inventory/dispositions | **Review/disposition complete**; accepted exports retained; remaining proposals feedback-deferred | Unmet downstream artifact need plus separately scoped export decision | No current mandatory export implementation; populations not equated |
| Stage 11 minor cosmetic refinements | Stage 11 closeout; accepted S22-008/popup/supplier/Include KSS | **Delivered scope accepted; disposition complete** | Further enhancement only on concrete post-publication feedback and approved scope | No current Stage 22 blocker |
| Stage 11 row virtualization | Stage 11 closeout | **DEFERRED in Stage 22 registry** | Keyboard-focus and accessibility solution exists | No unless production scale proves otherwise |
| Optional Stage 11 DBA investigation | Stage 11 performance record | **TRIGGER-DEFERRED** | Material production concurrency or reliability problem | No before trigger |
| Stage 10 `PERF-001`; S22-006/016/023 | Stage 10 closeout; accepted S22-006 diagnostics; owner 2026-10-21 | **Stage 27 / post-publication optimization**; owner reports both screens perform well; diagnostics preserved | Concrete performance objective or reported bottleneck; comparable shared QAD/MPS phase/cache/population measurements and DBA plan/index/read/wait evidence before SQL/batch/index tuning | No current Stage 22 tuning/baseline obligation; Stage 23 verification and Stage 24 target disposition remain |
| Stage 9 `po_mstr.po_stat` | Stage 9 validation evidence | **TRIGGER-DEFERRED**; no predicate may be invented | Accepted source evidence becomes available and a retained feature needs it | No before trigger |
| Current Comments | Accepted behavior | **Remain read-only for Release 1** | None | No |
| Write-capable buyer notes/local persistence/export-note updates | Deferred workflow concept | **Stage 27 / separate owner decision** | Approved workflow plus security, persistence, and licensing review | No |
| PO detail/drill card, previous/next navigation, no-open-PO detail; Component Orders flow/sorting/filtering | Stage 10 deferred backlog; S22-007/032 feedback disposition; owner 2026-10-21 | **Post-publication user-feedback enhancements / Stage 27**; accepted comments and read-only behavior preserved | Concrete user workflow feedback identifying desired behavior, then approved bounded specification | No current Stage 22 implementation blocker; future material defect triaged separately |
| Parent-part CSV import / S22-031 | Stage 4 backlog; owner 2026-10-21 | **Stage 27 / post-release enhancement** | Concrete scheduler file-import need plus approved input/error/duplicate-handling workflow; scoped parsing/security review before implementation | Not a current Stage 22 or Release 1 requirement |
| WO drill-down Short versus reported inventory coverage / S22-019 / WO-SHORT-01 | Owner 2026-10-21: scheduler sees a component marked Short despite believing inventory covers approximately three months; cause unconfirmed | **Later investigation / Stage 27 post-release enhancement backlog**, distinct from corrected S22-022 / GRID-001 | Owner direction or actionable user feedback; collect site, WO ID, component, displayed requirement/issued/short/usable quantities, comparison inventory source/quantity and data freshness. Reconcile usability exclusions, hard reservations, sequential allocation, requirement source and freshness before proposing an algorithm change | Deferred investigation, not a new completion blocker or finding of correctness/defect/allocation causation; future confirmed material defect requires explicit triage. Locked Stage 9–11 rules and architecture/security boundaries preserved; intentional algorithm change requires explicit owner-approved amendment |
| UI Navigation & Keyboard Ergonomics B | Original ergonomics plan §K/§O; accepted S22-018/030 menu work and local scrolling/focus; milestone 2026-10-22 | **Delivered Stage 22 depth accepted as sufficient**; S4 closed at accepted scope; S1/S2 tab arrows, S3 shared focus-trap extraction and S5 hints owner-deferred to Stage 27, not implemented | Future concrete keyboard/discoverability/shared-focus need plus approved bounded scope (individual triggers in current status) | Disposition complete; optional SHOULDs not mandatory implementation; no universal accessibility claim |
| Main MPS matrix horizontal scrollbar formerly visible but nonfunctional / S22-029 | Owner confirmation 2026-10-20; current status and accepted popup record | **OWNER-CONFIRMED RESOLVED** | No new implementation; future reproducible defect receives bounded triage | No known current blocker; no new exhaustive geometry claim |
| Can-Build tool and earliest material-ready state | Owner future concept | **Stage 27 — post-release / not authorized** | Separate owner-approved discovery charter | No |
| KST v1/v2 package identity and single-instance coexistence | `S0.7-F001` | **Stage 24** | Before side-by-side deployment | Yes before that deployment model |
| `keytronicshortage` security verification | S0 security closeout | **Stage 24 / external authority** | Before integration activation | Yes before activation only |
| Retrospective third-party software/license reconciliation | Governance/security closeout | **Stage 23** | Quality/security hardening | Yes before Release 1 closeout if still incomplete |
| Complete Windows installer/application-bundle SBOM | S0.6/S0.8 | **Stage 24** | Packaged release candidate exists | Yes for packaged release |
| cargo-deny, alternate secret/SBOM tools, Semgrep, CodeQL | Security admission records | **DEFERRED OPTIONS, not requirements** | A demonstrated coverage gap plus licensing/admission approval | No before trigger |
| CI/CD platform, risk-acceptance authority, external-AI-provider policy, release thresholds | Governance/security closeout | **Stage 24 / external decision as applicable** | Required by actual release operations | Conditional |
| Signing, updates, deployment, migration, support, KST v1 retirement | Release planning | **Stages 24–26** | Readiness, pilot, and rollout checkpoints | Yes at applicable gate |

### Stage 22 remaining dispositions — 2026-10-21

These dated decisions supersede only the corresponding unresolved registry dispositions, not historical
acceptance. S22-022 MPS quantity discrepancies are **corrected per owner confirmation**; no specific
unverified change is credited and no exhaustive numerical comparison is claimed. Further investigation
goes to **post-publication user-feedback triage**, triggered by a new reproducible part/site/snapshot/
week/date-basis discrepancy with displayed quantity and WO rows. No known current blocker under this
item; any new material data issue must receive explicit release triage.

| Other retained later work | Destination / trigger | Applicable release impact |
|---|---|---|
| Historical Shipments header filters/sorting (S22-015) | Optional later enhancement / Stage 27; concrete user need and approved filter/sort scope | Outside accepted Reports scope; no current Stage 22 blocker |
| Second Planning window (S22-024) | Optional later enhancement / Stage 27; explicit owner priority and approved shared-state/draft/focus/close-ownership design | No accepted current requirement; not a Stage 22 blocker |
| Remaining informational analytical proposals (S22-033) | Separate build-quantity, supply-aggregation or domain/rule proposal only for the concrete workflow and approved source/rule/fixture triggers in `KST_v2_S22_BOM_SHOW_MRP.md` | Existing contextual analyses retained; no present calculation/reacceptance gate; locked-rule changes require explicit authorization |
| Remaining export proposals (S22-035) | Post-publication feedback / separately scoped export decision when a downstream artifact need is unmet by accepted functionality | Disposition complete; no current mandatory export; Finished Goods no-export and accepted MRP preserved |
| Virtualization; Stage 11 DBA investigation; Stage 9 PO-status predicate | Retained trigger-deferred registry: focus/accessibility solution (and demonstrated scale need); material production concurrency/reliability problem; accepted source evidence plus retained feature need, respectively | Not blockers before trigger; no benchmark-driven virtualization or invented predicate |
| Formatting/lint exceptions (S22-027) | Stage 23 hardening/exception reconciliation; any repair needs bounded authorization and relevant locked-area approval | Remain failures/exceptions, not passes or risk acceptance; applicable release disposition still required |
| Deployment/identity/versioning (S22-025/028/038) | **Owner/IT-paused**; resume only on explicit owner/IT dispatch at Stage 24 readiness, then Stages 25–26 pilot/rollout | Not a current Stage 22 implementation gate; remains blocking at applicable deployment/compatibility gate |
| Licensing/dependency/capability reconciliation (S22-039) | Stage 23 security/governance hardening, including ClosedXML, Tauri dialog/filesystem and retained Fluent Assertions entitlement notice; package SBOM and release infrastructure at Stage 24 | No entitlement/risk acceptance here; required at applicable release gate; no new dependency/use-model authorization |
| Planning analytical export | Future iterative improvements outside Stage 22; separately approved redesign scope | Excluded from Stage 22; existing operational Mass Updater CSV unchanged |

The **owner-supplied accepted baseline expressly includes MPS drill-down/navigation acceptance**;
older pending checkpoint statements remain historical, not a renewed original-MPS acceptance prerequisite.
The concrete **Component Orders scrolling correction is owner-accepted — 2026-10-21**, locally committed
as `8891756`; its hold is removed. **Combined milestone decision approved — 2026-10-22:** delivered
Ergonomics B depth sufficient; S4 closed, S1–S3/S5 owner-deferred to Stage 27 as in the row above;
documented bounded workflow coverage accepted as sufficient and master checklist §22.1 amended to
that depth; Stage 22 complete/owner-accepted and baseline frozen. No exhaustive scheduler walkthrough
is claimed. No new UI work, independent review or serial delivery reacceptance gate is required solely
for milestone recording. Current status contains workflow evidence, individual SHOULD triggers and
the approved combined decision; generic broader-work labels add no requirement.
Accepted MPS/BOM export pilot/eight-second feedback, Part Info/MRP and BOM Show MRP/return stand;
unexecuted fault scenarios remain limitations, not fresh acceptance gates. WO-SHORT-01 remains a distinct
deferred investigation, not a reopening of corrected GRID-001. The 2026-10-22 milestone authorizes
completion recording and baseline freeze, **not Stage 23 execution or deployment**.

### External backlog reconciliation at milestone — 2026-10-22

Reviewed existing Windows inventory:
`C:\dev\kst_v2_tmp\Stage22\STAGE_22_READ_ONLY_INVENTORY_REVIEW.md` (2026-10-21 opening amendments
and retained ID rows). This committed registry/current status supersedes its older pending-milestone,
open/unfrozen and generic broader-work labels for the dispositions below. The external file and its
historical rows/backups are preserved, not silently rewritten or treated as current implementation gates.

| Existing ID / obligation | Milestone reconciliation / retained destination |
|---|---|
| S22-001/002 MPS; S22-010 Workspace Shipments; S22-011–015 Reports | Accepted deliveries govern over stale intake/specification-needed rows; scope/evidence and earlier acceptance dates remain in current status. S22-015 optional header filters/sorting stays Stage 27 on concrete user need/approved scope. |
| S22-003/017/018/020/021/037 broad review/cleanup/handoff | Accepted delivered scope and this consolidated documentation/handoff satisfy Stage 22 disposition; no unspecified refactor or new acceptance gate. Concrete future maintainability proposals need demonstrated need and bounded approval; full hardening remains Stage 23. Accepted helper retirement and Part Info/navigation remain closed at their scopes. |
| S22-019 integrity/recovery; WO-SHORT-01 | Stage 23 targeted hardening/recovery obligations retained; distinct WO-SHORT-01 stays later investigation / Stage 27 with the evidence/rule safeguards above, cause unconfirmed. Not implemented, diagnosed or merged into corrected GRID-001. |
| S22-006/016/023; S22-007/032; S22-022/024/030/031/033/035 | Approved completed/deferred/later-stage dispositions and all specific destinations/triggers are retained in the registry/addendum and current status; no deferred proposal relabeled implemented. S22-030 depth/optional deferrals now approved, not pending. |
| S22-025/028/038 deployment/identity/version; S22-026 package SBOM; S22-027/039 exceptions/licensing | Owner/IT pause and Stage 24–26 release gates retained; Stage 23 exception/licensing reconciliation not completed or waived. No release, entitlement or security-risk acceptance. |

Stage 23 planning must preserve this crosswalk, deferred proposals and applicable release impacts;
execution/deployment still need separate dispatch. Known failures/warnings/ignores/diagnostic limits and
unexecuted fault scenarios remain visible in accepted evidence. No new tests/builds/live reads or
performance claim is added by reconciliation.

## Explicitly removed or consolidated capability

- Old Stage 12 is not deferred; it is superseded and retired.
- Former Stage 16 is not an independent stage; its cross-customer report outcome is part of Stage 18.
- Stage 17 is not retained for Release 1 or automatically moved into Stage 18.
- Stage 20 simulation compatibility is not retained and is not a Stage 18 report.
- Stage 11's accepted export is not unfinished Stage 21 work. Stage 21 may check conformance but may
  not rebuild it without evidence.

## Stage 22 operating boundary

Stage 22 is expected to be highly interactive. Work should proceed through bounded owner-review
batches: consolidate evidence, reproduce the issue, agree on acceptance behavior, implement only
after authorization, validate, and accept or return the item to the registry. Stage 22 must not
become an unbounded feature bucket.

## Documentation reconciliation ledger

| Contradiction/staleness | Resolution |
|---|---|
| Current status said Stage 11 still awaited validation | Corrected in `CURRENT_PROJECT_STATUS.md`. |
| Master-checklist introduction described unfinished S0 and Stage 9 blocking | Corrected to the accepted Stage 1–11 baseline. |
| Old Stage 12 duplicated/superseded Workspace Shortages | Marked retired/superseded. |
| Stage 21 listed accepted or retired exports as uniformly unfinished | Replaced with a fresh system-wide inventory and conformance stage. |
| Quality/Readiness/Pilot/Rollout/Post-Release used old Stage 22–26 numbers | Renumbered to Stages 23–27. |
| Stage 11 historical plans contain pre-acceptance language | Preserved as historical evidence; current status and checklist are authoritative for present state. |
| Architecture documentation stopped at Stage 8 | Current accepted Stage 9–11 boundaries added to the architecture summary. |
| S0 security report described a `core:default`-only Tauri capability before Stage 11 export | Current-state addendum records the user-mediated save/write capability and preserves S0 evidence as historical. |
| Dependency/admission evidence for Stage 11 export components requires reconciliation | Retained as Stage 23 governance/security work; no admission is inferred by this review. |

## Remaining authorization boundary

**Current — 2026-10-22:** Stage 23 is ready for planning, not started. Separate dispatch is required
for execution and deployment remains owner/IT-paused. Stage 22 acceptance/freeze and one local final
documentation commit do not approve release, push, implementation, live reads or dependency changes.

**Historical — 2026-09-29:** the next planned capability was Stage 13 Customer Open Orders. Its bounded plan was owner-accepted
on 2026-09-29. That acceptance does not authorize implementation, live QAD/Shortages investigation,
dependency installation, database changes, commit, or push. The project owner must separately
authorize the first implementation checkpoint.

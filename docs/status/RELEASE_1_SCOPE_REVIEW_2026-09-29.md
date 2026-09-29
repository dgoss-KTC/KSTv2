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

Stages 12, 17, and 20 are retired. Stage 16 is absorbed into Stage 13. Their numbers remain visible
for provenance; later historical references do not need reinterpretation.

## Stage-disposition matrix

| Stage | Disposition | Release 1 outcome |
|---|---|---|
| 12 — Multi-Part Shortage Analysis | **RETIRED / SUPERSEDED** | Accepted Stage 11 supplies the relevant workspace capability. |
| 13 — Open Orders | **PLANNED / REQUIRED** | Combined customer-specific date-change and cross-customer search workflow. Next planned product stage; not implementation-authorized. |
| 14 — Planning Workbook | **PLANNED / REQUIRED** | Reordered behind Open Orders. |
| 15 — Finished Goods | **PLANNED / REQUIRED** | Immediate-demand coverage using accepted source rules established during the stage. |
| 16 — General Open Orders | **ABSORBED** | Included in Stage 13. |
| 17 — General WO Variance | **RETIRED / NO LONGER DESIRED** | Existing Stage 7 capability remains; no standalone search/report. |
| 18 — Standalone Excel Report Generators | **PLANNED / REQUIRED** | Component MRP, Open Order Report, Shipments-To-Go, and S&OP. |
| 19 — Historical Shipments | **PLANNED / REQUIRED** | Historical shipment search/report, distinct from Shipments-To-Go. |
| 20 — Legacy Simulation | **RETIRED / NO LONGER DESIRED** | No compatibility migration; remove from readiness criteria. |
| 21 — Cross-Cutting Export Completion | **PLANNED / REQUIRED** | Inventory all application areas, decide export need, implement gaps, and validate consistency. |
| 22 — Refinement & Optimization | **PLANNED / REQUIRED** | Interactive product-wide defect, refinement, deferred-work, and measured-performance stage. |
| 23 — Quality and Hardening | **PLANNED / REQUIRED** | Validate and harden the frozen Stage 22 product baseline. |
| 24 — Release 1 Readiness | **PLANNED / REQUIRED** | Packaging, deployment, operational, security, and release decisions. |
| 25 — Pilot | **PLANNED / REQUIRED** | Controlled initial-site pilot with KST v1 fallback. |
| 26 — Incremental Multi-Site Rollout | **PLANNED / REQUIRED** | Site-by-site validation and deployment. |
| 27 — Post-Release Roadmap | **POST-RELEASE** | QAD upgrade preparation, Can-Build discovery, and separately authorized future concepts. |

## Deferred-work registry

| Item | Source/evidence | Disposition and destination | Trigger | Release blocking? |
|---|---|---|---|---|
| Individual / Single-Part Component MRP | Stage 11 closeout backlog | **Stage 18 — required report generator** | Stage 18 authorization | Yes, as part of approved Stage 18 report set |
| Open Order Report | Owner decision in scope review | **Stage 18 — required report generator** | Stage 18 authorization | Yes |
| Shipments-To-Go and S&OP | Existing master checklist + owner confirmation | **Stage 18 — required report generators** | Stage 18 authorization | Yes |
| Historical Shipments report | Existing Stage 19 + owner confirmation | **Stage 19 — required** | Stage 19 authorization | Yes |
| Inventory/Lot Locations in Component Information | Stage 8 deferred scope | **Stage 22 — review and disposition** | Stage 22 interactive walkthrough | Owner decides within Stage 22 |
| Show MRP; Extended Requirement; Incoming Supply; Coverage / Material Status | Stage 8 deferred scope | **Stage 22 — review and disposition** | Stage 22 interactive walkthrough | Owner decides within Stage 22 |
| Remaining component supply/PO coverage | Stage 8/9/10 deferred scope | **Stage 22 — review**; do not change locked algorithms silently | Demonstrated workflow gap | Owner decides within Stage 22 |
| Configurable application-view exports | Older Stage 21 checklist | **Stage 21 — fresh inventory** | Stage 21 authorization | Yes where inventory approves an export |
| Stage 11 minor cosmetic refinements | Stage 11 closeout | **Stage 22** | Stage 22 walkthrough | No individually; aggregate owner disposition |
| Stage 11 row virtualization | Stage 11 closeout | **DEFERRED in Stage 22 registry** | Keyboard-focus and accessibility solution exists | No unless production scale proves otherwise |
| Optional Stage 11 DBA investigation | Stage 11 performance record | **TRIGGER-DEFERRED** | Material production concurrency or reliability problem | No before trigger |
| Stage 10 `PERF-001` | Stage 10 closeout | **Stage 22** | Current measurement confirms need | Owner decides within Stage 22 |
| Stage 9 `po_mstr.po_stat` | Stage 9 validation evidence | **TRIGGER-DEFERRED**; no predicate may be invented | Accepted source evidence becomes available and a retained feature needs it | No before trigger |
| Current Comments | Accepted behavior | **Remain read-only for Release 1** | None | No |
| Write-capable buyer notes/local persistence/export-note updates | Deferred workflow concept | **Stage 27 / separate owner decision** | Approved workflow plus security, persistence, and licensing review | No |
| PO detail/drill card, previous/next navigation, no-open-PO detail | Stage 10 deferred backlog | **Stage 22** | Interactive walkthrough | Owner decides within Stage 22 |
| Parent-part CSV import | Stage 4 backlog | **Stage 22** | Interactive walkthrough | Owner decides within Stage 22 |
| UI Navigation & Keyboard Ergonomics B | Existing deferred navigation scope | **Stage 22** | Stage 22 authorization | Yes to the accepted Stage 22 depth |
| Main MPS matrix horizontal scrollbar is visible but nonfunctional | Owner-observed defect | **Stage 22 — required** | Stage 22 authorization | Yes |
| Can-Build tool and earliest material-ready state | Owner future concept | **Stage 27 — post-release / not authorized** | Separate owner-approved discovery charter | No |
| KST v1/v2 package identity and single-instance coexistence | `S0.7-F001` | **Stage 24** | Before side-by-side deployment | Yes before that deployment model |
| `keytronicshortage` security verification | S0 security closeout | **Stage 24 / external authority** | Before integration activation | Yes before activation only |
| Retrospective third-party software/license reconciliation | Governance/security closeout | **Stage 23** | Quality/security hardening | Yes before Release 1 closeout if still incomplete |
| Complete Windows installer/application-bundle SBOM | S0.6/S0.8 | **Stage 24** | Packaged release candidate exists | Yes for packaged release |
| cargo-deny, alternate secret/SBOM tools, Semgrep, CodeQL | Security admission records | **DEFERRED OPTIONS, not requirements** | A demonstrated coverage gap plus licensing/admission approval | No before trigger |
| CI/CD platform, risk-acceptance authority, external-AI-provider policy, release thresholds | Governance/security closeout | **Stage 24 / external decision as applicable** | Required by actual release operations | Conditional |
| Signing, updates, deployment, migration, support, KST v1 retirement | Release planning | **Stages 24–26** | Readiness, pilot, and rollout checkpoints | Yes at applicable gate |

## Explicitly removed or consolidated capability

- Old Stage 12 is not deferred; it is superseded and retired.
- Former Stage 16 is not an independent stage; its functionality is part of Stage 13.
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

The next planned capability is Stage 13 Open Orders. This planning baseline does not authorize
implementation, live QAD/Shortages investigation, dependency installation, database changes,
commit, or push. The project owner must separately authorize the next bounded checkpoint.

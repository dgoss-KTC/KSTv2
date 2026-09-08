# KST v2 -- Stage 9 Immediate Work-Order Shortages Closeout

**Technical status:** COMPLETE -- 2026-09-08  
**Owner acceptance:** COMPLETE / ACCEPTED / LOCKED -- 2026-09-08  
**Current authority:** This closeout summarizes the accepted Stage 9 implementation and verification evidence. Historical planning and checkpoint evidence remains preserved in the documents linked below.

## A. Stage Objective

Stage 9 gives the scheduler an immediate, Work Order + Component analysis for near-term work. It identifies applicable purchased-material shortages using authoritative requirements, usable-now inventory, allocation provenance, and informational incoming-supply context without changing QAD.

## B. Final Accepted Business Behavior

- **Planning population:** Stage 7R Falldown plus active-basis Weeks 0-3. Falldown is due-date based; forward membership follows Due or Release basis. The result grain is one component within one Work Order, never a combined component-wide shortage.
- **Requirements:** R, A, and `E + wo_type = F` use Actual `wod_det` requirements. Ordinary E/F/P use the release-date-effective Projected BOM, falling back to Today only when release date is unusable. A successful empty Actual material read is a Loaded zero-component analysis, not `WorkOrderNotInImmediateWindow`.
- **Purchased-material participation:** Both Actual and Projected paths use authoritative master `pt_mstr.pt_pm_code`. `M` is Manufactured / `NotApplicable`: visible and drillable, but null/N-A Short, no PO/NO PO inference, and no WO shortage indication. Nonblank non-`M` is eligible. Null, blank, or whitespace master P/M is Unknown / Data Issue. Stage 8 site-effective P/M is a separate informational/BOM concept.
- **Material statuses:** Applicable rows classify Unknown / Data Issue, then Short, then On Hand. Unknown is loaded data-trust uncertainty, not zero requirement or empty data.
- **Inventory:** Physical usable inventory is positive, usable Stock/nettable inventory after accepted RMA and expiration treatment. Transit, Inspection, Non-Net, MRB, NCM Inspection, and Expired/Expiring are context only. Issue policy is informational only: site `ptp_det.ptp_iss_pol`, then master `pt_mstr.pt_iss_pol`, then true default; site policy is independent of master-row existence.
- **Hard allocations:** `lad_det` is authoritative hard/detail allocation. For `lad_dataset = 'wod_det'`, WOID is `lad_nbr`, operation is `lad_line`, component is `lad_part`, and `lad_qty_all` is allocation quantity; domain/site/location/lot are retained. A matching `wod_det` row is not required. Usable hard reservations are removed from shared free inventory, and the evaluated WO receives its own clamped hard coverage. Hard allocations never override usability and are not limited to the Stage 9 window.
- **Residual allocation:** After hard reservations, committed R WOs consume residual free inventory sequentially before A WOs, within the immediate window and accepted date/WOID ordering. Uncommitted WOs independently evaluate the resulting common advisory pool and do not consume from each other. `wod_det.wod_qty_all` is excluded; no allocation is double-counted.
- **PO/KSS:** Incoming PO/KSS context is informational and never reduces Short. Conventional PO context requires positive open line quantity with accepted line-level exclusion. KSS is independent of conventional PO availability: it is an effective domain/site/part `pod_det` supplier-schedule relationship joined to effective `po_mstr` with `po_sched = 1`. Thus KSS without conventional PO is KSS, not NO PO; an exceptional conventional PO remains visible with KSS. No `po_mstr.po_stat` predicate is accepted.
- **Diagnostics:** Business/data reliability states are returned as loaded analysis results. Technical reader failure remains unavailable behavior rather than a fabricated component status.

## C. Architecture Delivered

- **Domain:** Immediate-material models, requirement/source classifications, purchased-material applicability, inventory and allocation calculations, and result provenance.
- **Application:** Snapshot-aware selected-WO orchestration, business-date-sensitive allocation caching, requirement resolution, hybrid allocation, and incoming-context composition.
- **QAD integration:** Read-only, parameterized readers for requirements, committed population, inventory, issue policy/days, hard `lad_det` allocation, conventional PO, and independent KSS schedule evidence.
- **API:** Immediate-material endpoint and DTO/OpenAPI/generated TypeScript projection. Public component provenance includes `allocationMode`, `usableHardAllocationToThisWoComponent`, `ownHardCoverage`, `uncoveredRequirement`, `availableQuantityAtEvaluation`, and `allocatedQuantity`. Wire values are `hardAllocatedCommitted`, `committedSequential`, and `advisorySharedPool`.
- **Frontend:** Integrated selected-WO immediate-material panel, Shortages tab, component detail/candidate navigation, card indication for applicable purchased shortages, and focus-aware Escape handling.
- **Tauri/desktop:** Existing sidecar and desktop workflow continue to host and verify the Stage 9 API/UI without a new desktop architecture.

## D. Public and UI Workflow

The normal scheduler path is:

```text
MPS -> Work Orders -> Show/Hide Material Lines -> integrated WO immediate-material analysis
```

The full Work Order card strip remains above the selected analysis. Only one selected-WO analysis is open at a time. The retired `Select for Shortages` workflow is not part of Stage 9. The Shortages tab remains usable for the selected analysis, and switching between Work Orders and Shortages preserves selected-WO context.

Applicable purchased-material Short results can mark a Work Order card. Manufactured components cannot create that indication. No MPS-grid shortage marker exists.

Escape unwinds exactly one level: Material Detail, nested manufactured/second-level immediate-material analysis, containing candidate/analysis levels, selected top-level analysis, Work Orders, then root. Material Detail owns the first Escape while open and focus restoration follows the accepted initiating-control behavior.

## E. Verification Evidence

- **9.7 automated scenario audit:** `KST_v2_STAGE_9_SCENARIO_COVERAGE_AUDIT.md` records 51 grouped current rules: 32 DIRECT, 15 COMPOSITE, 1 SUPERSEDED, 3 NOT AUTOMATABLE HERE, and 0 MISSING.
- **9.8 live-QAD validation:** `KST_v2_STAGE_9_LIVE_QAD_VALIDATION.md` records final classifications of 1 CONFIRMED, 8 CONFIRMED WITH QUALIFIER, 2 NOT OBSERVED, 1 OWNER/QAD EXPERT DECISION REQUIRED, and 0 unresolved CONTRADICTED. The KSS contradiction was corrected in a bounded task and then validated.
- **9.9 full regression:** `KST_v2_STAGE_9_FULL_REGRESSION.md` records passing full backend, frontend, Tauri, sidecar, contract, and manual desktop regression. Manual families 1-18 ultimately passed; M001 was corrected and owner-retested before the final gate passed.

## F. Production Defects Discovered During Verification

Ten discrete production defects were discovered and corrected across accepted verification checkpoints.

### Stage 9.7

1. Hard allocation incorrectly required `wod_det`.
2. Empty Actual material was treated as planning-window absence.
3. Site issue-policy availability depended on a master row.
4. Immediate-material Escape skipped directly to root.
5. Shortages tab cleared selected analysis.
6. Projected classification used Stage 8 effective P/M.
7. Allocation provenance was omitted from the public API.
8. Actual missing master P/M became purchased eligibility.

### Stage 9.8

9. KSS was incorrectly coupled to conventional open-PO availability rather than the independent effective supplier-schedule source.

### Stage 9.9

10. **Stage 9.9-M001:** nested Material Detail Escape unwound the second-level analysis before closing Material Detail.

The checkpoint records preserve the original observations, bounded corrections, and final evidence; this closeout does not erase that history.

## G. Known Deferred and Evidence Items

- `po_mstr.po_stat` master-status semantics remain unresolved. Clearly open supplied PO examples had blank `po_stat`; no authoritative closed-PO comparison exists. Current line-level positive-open-quantity/status qualification remains in force, and no master-status predicate is accepted.
- The two Stage 9.8 NOT OBSERVED families are evidence limits, not known defects or mandatory Stage 10 work: effective BOM/phantom live sampling and uncommitted/advisory source records were not observed in the bounded sample.

## H. Out of Scope and Future Boundary

Stage 9 does not provide combined/component-wide shortage netting, time-phased PO receipts or coverage, projected shortage clear dates, lead-time or component MRP, future component shortage projection, detailed PO/vendor/buyer workflow, substitutes, or fabricated parent/subassembly pegging.

## I. Completion Gate

Stage 9.11 owner acceptance was recorded on **2026-09-08**. Stage 9 is **COMPLETE / ACCEPTED / LOCKED**. Technical, automated-regression, live-QAD, and owner-guided evidence is complete.

`po_mstr.po_stat` master-status semantics remain a deferred evidence item. No master-status predicate is accepted, and the item does not block Stage 9 acceptance.

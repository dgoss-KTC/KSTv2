You are working inside the KST v2 repository in VS Code.

Your task is:

# Stage 8D.0 — Component/BOM Detail Repository Preflight and Implementation Plan

This is a PLANNING-ONLY checkpoint.

Do not implement Stage 8 yet.
Do not modify production code.
Do not modify tests.
Do not modify OpenAPI/generated TypeScript.
Do not update project documentation yet.
Do not commit or push changes.

Your job is to inspect the current repository, reconcile the accepted Stage 8 design with the implementation that already exists from Stages 5–7, and produce a precise repository-aware implementation plan for human review.

Follow the repository's local-agent workflow:

Explore → Plan → Human Review

Stop after Plan.

Do not proceed to implementation until the project owner explicitly approves the plan.

---

# 1. Repository instructions are authoritative

Before analyzing Stage 8, locate and read the repository instructions and relevant project documentation.

At minimum inspect:

- AGENTS.md and any nested/local agent instruction files that apply
- KST v2 Project Instructions — Local Agent Addendum.md
- CURRENT_PROJECT_STATUS.md
- KST-v2-Master-Project-Checklist.md
- BACKEND_PROJECT_BOUNDARIES.md
- API_CONTRACT_WORKFLOW.md
- OPENAPI_CLIENT_GENERATION.md
- BUILD_AND_TEST.md

Also locate and inspect the durable Stage 6 and Stage 7 documentation, implementation reports, accepted plans, tests, or other repository artifacts that describe:

- Stage 6 Part Information
- Stage 6 inventory classification / Net QOH / Non-Net QOH / RMA
- Stage 6 pt_mstr / ptp_det site-specific planning behavior
- Stage 7 Work Orders
- Stage 7 Kitting/component search behavior
- Stage 7 lazy-loading/cache patterns
- Stage 7 selected-part/detail-pane behavior

Do not assume filenames. Find the relevant files from the actual repository.

If repository documentation disagrees with the accepted Stage 8 rules below, do not silently choose one.

Report the discrepancy clearly in your plan.

The accepted Stage 8 rules in this prompt are newer project-owner decisions and should be treated as requirements that may not yet have been written into repository documentation.

---

# 2. Current project boundary

Stages 1–7 are complete and accepted.

Stage 8 is:

# Component and BOM Detail

Stage 8 has been redesigned from the older checklist.

Its purpose is now:

A scheduler can select a parent part, inspect and search its current-effective multi-level BOM, select a relevant purchased or manufactured component, and inspect validated site-specific inventory, planning, cost, and Approved Vendor List information without navigating to QAD.

Stage 8 is informational.

Do NOT introduce material-requirement or shortage calculations in this stage.

Specifically, Stage 8 does NOT implement:

- Extended Requirement
- Incoming Supply
- Coverage %
- Material Status
- Short Quantity
- Projected QOH
- time-phased Component MRP
- PO coverage
- Future Shortages

Those belong to later stages.

Do not create ComponentRequirement or another requirement/coverage model merely because the older checklist expected one.

---

# 3. Accepted Stage 8 UI behavior

The current detail-pane tab behavior should become:

When a parent part is selected:

- Part Info
- BOM

When an MPS bucket / Falldown context is selected:

- Part Info
- BOM
- Work Orders
- Shortages

Future Shortages is removed from the current workflow.

The former prototype "Components" tab should be called:

BOM

The BOM is parent-contextual.

It must not depend on:

- selected MPS week quantity
- Due/Release display mode
- MPS horizon

The BOM tab includes a component-number search box similar to the accepted Stage 7 Kitting search interaction.

Search behavior:

- client-side after the BOM has loaded
- substring match against Component Item
- exact matches work naturally
- partial matches work
- clearing the search restores the complete BOM
- do not add description search unless existing shared UI behavior already makes that unavoidable; report this if so

Selecting a BOM component opens a Component Info card to the right.

Selecting another component immediately replaces the selected component card.

AVL is collapsed by default.

Selecting another component resets AVL to collapsed.

AVL should not be queried until expanded.

A future MRP Schedule drilldown may eventually launch from Component Info, but Stage 8 must not implement a dead button or Component MRP behavior.

---

# 4. Accepted BOM business rules

## 4.1 Source

The legacy BOM stored procedure was reviewed.

The useful authoritative source is:

QADPro2.dbo.ps_mstr

The stored procedure performs a true multi-level BOM traversal.

Stage 8 should preserve the proven semantics while implementing them behind the KST QAD integration boundary.

Do not assume the legacy stored procedure itself must be called.

During preflight, determine whether repository patterns and SQL constraints favor:

- adapting the proven traversal into KST-owned SQL, or
- another equivalent read-only implementation

Recommend the safest implementation based on repository reality.

Do not redesign the BOM algorithm merely for elegance.

SQL Server 2016 compatibility remains required.

## 4.2 Effective date

Stage 8 displays the BOM effective on the current business date.

A BOM relationship is effective when:

ps_start IS NULL OR ps_start <= effective date

AND

ps_end IS NULL OR ps_end >= effective date

The frontend does not provide an effective-date picker in Stage 8.

The application/backend should obtain the effective date using the existing clock abstraction if appropriate.

The response should retain/report the effective date for traceability.

## 4.3 Multi-level traversal

Traverse the complete effective BOM.

Preserve:

- actual structural hierarchy
- actual level
- actual traversal/order
- every BOM occurrence

Do NOT consolidate repeated components.

Do NOT use SELECT DISTINCT as a defensive deduplication mechanism.

The same component may legitimately appear:

- more than once under one parent
- at multiple levels
- beneath different subassemblies

The legacy BOM relationship identity is oid_ps_mstr.

Investigate how best to preserve this internally.

The frontend should receive an opaque occurrence identity rather than a QAD-specific field name if that matches current project conventions.

## 4.4 P/M

Stage 8 scheduler-visible BOM rows are limited to:

P
M

Primary P/M source:

ptp_det.ptp_pm_code

using:

domain + component + selected workspace site

Fallback rule:

If the selected-site ptp_det row/value is unavailable, use:

pt_mstr.pt_pm_code

for P/M classification only.

This fallback must NOT be generalized to the other ptp_det planning fields.

The BOM traversal itself must continue through all effective structural rows, including rows whose P/M is not P or M.

Only presentation/output filtering is limited to P and M.

Known other PM codes include:

2, 3, 4, C, D, N, S

These are not scheduler-relevant for this Stage 8 BOM view and their detailed meanings do not need to be solved now.

Do not stop recursion because one of these records is encountered.

Do not cosmetically re-level descendants after hidden rows are filtered.

Preserve the actual BOM level.

## 4.5 Phantom

Source:

pt_mstr.pt_phantom

Do not flatten phantom structure.

If a phantom row qualifies for scheduler display, show it and identify it as Phantom.

Continue traversing beneath it normally.

## 4.6 Description

Use component master description based on:

pt_desc1
pt_desc2

The Stage 8 BOM should use null-safe combination/formatting.

Do not allow SQL NULL concatenation to wipe out an otherwise valid description.

## 4.7 Quantity Per

Source:

ps_mstr.ps_qty_per

This is relationship/occurrence-level Qty Per.

Do not multiply Qty Per through the hierarchy.

Do not calculate Extended Requirement.

## 4.8 Scrap

Source:

ps_mstr.ps_scrp_pct

This is occurrence/relationship-level data.

## 4.9 Inventory

The BOM displays:

- Net QOH
- Non-Net QOH

Do NOT use a raw in_mstr QOH sum.

Stage 8 must reuse the accepted Stage 6 inventory calculation/classification.

RMA is not shown and does not participate in the Stage 8 Net/Non-Net totals.

Accepted semantics:

Net QOH:
- positive qualifying inventory
- non-RMA
- nettable according to accepted Stage 6 rules

Non-Net QOH:
- positive qualifying inventory
- non-RMA
- non-nettable according to accepted Stage 6 rules

No qualifying inventory means numeric zero.

The Stage 6 implementation is now the first major repository item you must inspect.

Stage 8 is the second real use of this same inventory meaning.

Determine whether the current Stage 6 implementation:

- is already reusable as-is
- needs a minimal extraction/refactor
- supports batch lookup
- is currently single-part only

Stage 8 BOM should preferably obtain inventory for distinct component parts in a batch.

Do NOT execute an inventory query once per BOM occurrence.

Do NOT duplicate the Stage 6 classification SQL/rules inside a BOM query.

---

# 5. Accepted BOM response concept

The exact C# names may be reconciled with repository naming conventions, but the intended business shape is:

BomResponse
- parentPart
- effectiveDate
- lines[]

BomLine
- occurrenceKey
- level
- componentPart
- pmCode
- isPhantom
- description
- quantityPer
- scrap
- netQoh
- nonNetQoh

BOM structural data and component inventory have different grains.

A BOM occurrence owns:

- level
- structural occurrence
- quantity per
- scrap
- hierarchy/order

Inventory owns:

Site + Component

The application may compose these into the API response, but do not model Net QOH as if it were unique inventory belonging to a specific BOM occurrence.

Repeated component occurrences may therefore display repeated QOH values.

That is correct for this informational view.

Later shortage logic must not interpret repeated displayed inventory as multiple inventory pools.

---

# 6. Accepted Component Info business rules

Component Info grain:

Workspace Site + Component Part

The card header should identify:

- Component Item
- Description

Scalar fields:

- Part Status
- Net QOH
- Non-Net QOH
- Std Cost
- QCTC
- Time Fence
- Safety Time
- Safety Stock
- Buyer / Planner
- Purchase LT
- Inspect LT
- Cumulative LT
- Min Order
- Order Multiple
- IOS

## 6.1 Part master fields

Part Status:

pt_mstr.pt_status

Description:

pt_mstr descriptions

IOS:

pt_mstr.pt_warr_cd

## 6.2 Site planning fields

All of these must come from ptp_det for:

domain + part + selected workspace site

Fields:

Time Fence
= ptp_timefnce

Safety Time
= ptp_sfty_tme

Safety Stock
= ptp_sfty_stk

Buyer / Planner
= ptp_buyer

Purchase LT
= ptp_pur_lead

Inspect LT
= ptp_ins_lead

Cumulative LT
= ptp_cum_lead

Min Order
= ptp_ord_min

Order Multiple
= ptp_ord_mult

Do NOT use pt_site to establish the pt_mstr → ptp_det relationship.

Do NOT silently substitute global pt_mstr planning fields when the site-specific ptp_det record is missing.

If the site-specific planning record/value is unavailable, return No Data/null for the affected planning field.

The P/M fallback described earlier is the exception and applies only to P/M classification.

## 6.3 Inventory

Component Info uses the same shared Stage 6:

- Net QOH
- Non-Net QOH

RMA excluded.

Zero inventory is numeric zero, not missing data.

## 6.4 Standard Cost

Source:

sct_det.sct_cst_tot

Match:

sct_domain = workspace domain
sct_site = workspace site
sct_part = selected component

Choose the record with the most recent:

sct_cst_date

Do not average or sum cost rows.

Do not substitute vendor pricing.

If no valid record exists:

standardCost = null / No Data

If multiple rows share the same latest date, do not invent a tie-break rule during implementation without evidence.

Treat equal-latest-date behavior as a validation item and report what the repository/live-data implementation needs.

## 6.5 QCTC

Source:

Analysis.dbo.in_price.inp_qctc

Match:

inp_domain = workspace domain
inp_site = workspace site
inp_part = selected component

Choose the record with the most recent:

inp_start_date

If no record exists:

qctc = null / No Data

Do not use the old KSTv1 domain+part-only lookup.

Again, if equal latest dates exist, report the condition rather than inventing business meaning.

## 6.6 Missing-data semantics

Important:

0 = authoritative numeric zero

null / No Data = no authoritative source value available

A missing ptp_det row must not make the whole Component Info request fail.

The application should still return whatever valid master, inventory, cost, QCTC, or IOS information exists.

A database/query failure is different and must surface as an error rather than fake partial/empty data.

---

# 7. Accepted AVL behavior

AVL is a separate zero-to-many child resource.

Sources:

vp_mstr
ad_mstr

Relationships:

component domain + part → vp_mstr

vp domain + vendor → ad_mstr

Required AVL columns:

- Supplier = vp_vend
- Vendor Name = ad_name
- Supplier Item = vp_vend_part
- MFG Part = vp_mfgr_part

Do not include the larger legacy procedure's:

- Gray Market display
- inspection-required field
- ROHS
- REACH

unless repository evidence demonstrates an already-required Stage 8 behavior.

Those were intentionally excluded from Stage 8.

Rules:

- AVL collapsed by default
- query only when expanded
- all legitimate matching rows retained
- order primarily by Supplier
- no records = successful empty list
- no defensive DISTINCT without evidence
- selecting another component collapses AVL
- AVL failure must not destroy an already-loaded Component Info card

The old KSTv1 scalar vendor price labeled "AVL" is NOT the AVL list and is NOT Standard Cost.

Do not revive that field.

---

# 8. Intended backend separation

Inspect repository architecture and determine the exact interfaces/files, but preserve this conceptual separation:

1. BOM structure reader / QAD adapter
   - effective ps_mstr traversal
   - occurrence identity/order
   - master enrichment required for structural rows

2. Shared component inventory capability
   - reuse/minimally refactor Stage 6
   - Net/Non-Net
   - ideally batch-capable

3. Component Detail reader
   - pt_mstr
   - selected-site ptp_det
   - latest sct_det Standard Cost
   - latest site-specific QCTC

4. AVL reader
   - vp_mstr + ad_mstr

5. Application services
   - orchestration/composition
   - business filtering
   - cache/freshness rules
   - DTO mapping outside the integration layer as appropriate

QAD-specific SQL must stay inside the QAD integration boundary.

Do not expose QAD table-shaped models directly to the React frontend.

Respect the existing Kst.Domain / Kst.Application / Kst.Integrations.Qad / Kst.Api dependency rules.

---

# 9. Intended lazy-loading sequence

Expected workflow:

Select parent
    ↓
BOM loads lazily

BOM search
    ↓
frontend/client-side only

Select BOM component
    ↓
Component Info loads lazily

AVL stays collapsed
    ↓
no AVL query yet

Expand AVL
    ↓
AVL loads lazily

Inspect the existing Stage 6/7 caching and lazy-detail patterns and recommend how Stage 8 should fit them rather than inventing a parallel cache architecture.

Due/Release toggling must not reload BOM.

MPS horizon changes must not reload BOM.

Ordinary bucket selection must not alter the BOM.

---

# 10. Intended API shape

Treat these as accepted conceptual resources, but reconcile exact route/file conventions with the existing API implementation.

Candidate resources:

GET /api/v1/workspaces/{workspaceId}/parts/{parentPart}/bom

GET /api/v1/workspaces/{workspaceId}/components/{componentPart}

GET /api/v1/workspaces/{workspaceId}/components/{componentPart}/approved-vendors

Important:

The frontend should identify the workspace and requested part/component.

The frontend should not independently supply authoritative:

- site
- domain
- inventory-status logic
- P/M filter
- BOM effectivity rules

Workspace resolution determines Site.

The QAD boundary resolves Domain using existing project behavior.

Determine whether these candidate routes align with current Stage 6/7 conventions.

If not, recommend the smallest consistent adjustment and explain it.

Do NOT implement routes in 8D.0.

---

# 11. API and frontend contract workflow

Inspect and preserve the current contract workflow.

C# API DTOs are authoritative.

Expected pipeline:

C# DTOs
    ↓
OpenAPI generation
    ↓
generated TypeScript types
    ↓
frontend API client
    ↓
React components

Generated TypeScript must never be manually edited.

No API or generated files should be changed in this planning checkpoint.

---

# 12. Error and empty-state semantics

The implementation plan must preserve these distinctions:

Valid parent with no scheduler-visible P/M BOM lines:
- success
- empty lines[]

Component has no qualifying inventory:
- success
- netQoh = 0
- nonNetQoh = 0

Component missing optional site planning field:
- success
- nullable field / No Data

No Standard Cost:
- success
- null / No Data

No QCTC:
- success
- null / No Data

No AVL rows:
- success
- empty vendors[]

Unknown workspace:
- not found according to established API conventions

Unknown component:
- not found according to established API conventions

QAD unavailable/query failure:
- actual API/service error
- never represent database failure as an empty BOM, zero inventory, or empty AVL

AVL query failure:
- AVL request fails independently
- loaded Component Info remains usable

Component Info query failure:
- Component card can show error
- loaded BOM remains usable

Inspect current Problem Details and stale/last-good patterns and explain how Stage 8 should conform.

---

# 13. Cache/freshness design to inspect

Business identities:

BOM:
Site + Parent + Effective Date

Component Info:
Site + Component

AVL:
Domain + Component, although workspace-scoped cache ownership is acceptable if consistent with repository architecture

Inventory:
Site + Component

The current successful workspace/MPS refresh may be used as Stage 8 cache freshness/invalidation context, but MPS snapshot identity is not part of the fundamental BOM business grain.

A successful workspace refresh should make subsequent Stage 8 reads obtain fresh data.

A failed workspace refresh should not destroy compatible last-good Stage 8 information.

Due/Release, MPS horizon, tab switching, and bucket selection do not invalidate Stage 8.

BOM effective date must be included in its identity so an app left open overnight does not indefinitely present yesterday's BOM as today's current-effective BOM.

Inspect the actual Stage 6/7 cache implementation before recommending exact cache records or keys.

Do not invent a parallel cache system without need.

---

# 14. Performance constraints to inspect

The implementation plan must avoid N+1 database access.

Specifically investigate how to support:

BOM traversal
    ↓
distinct visible component parts
    ↓
batch inventory lookup
    ↓
join inventory summaries back to individual BOM occurrences

If 400 displayed BOM occurrences contain 310 distinct parts, do not propose 400 independent inventory requests.

Likewise, avoid one database connection/query per scalar Component Info field.

Determine a sensible QAD read shape based on existing Dapper/SqlClient conventions.

Do not optimize prematurely beyond evidence.

Prefer known-correct source behavior over clever SQL.

---

# 15. Stage 8 implementation checkpoint target

Do NOT perform these checkpoints yet.

Your preflight plan should assess and refine this proposed decomposition:

8D.1 — Shared Stage 6 inventory capability
- inspect/reuse/refactor
- batch support if appropriate
- prove no Stage 6 regression

8D.2 — BOM QAD adapter / normalization
- effective multi-level traversal
- occurrence identity/order
- site P/M + fallback
- phantom
- description
- Qty Per
- Scrap
- automated adapter/domain tests

8D.3 — BOM application service / API
- P/M scheduler filtering
- distinct-component inventory enrichment
- DTOs
- endpoint
- Problem Details
- OpenAPI/generated frontend contracts
- tests

8D.4 — BOM frontend
- Components → BOM
- revised tab visibility
- BOM grid
- search
- loading / empty / error
- component row selection

8D.5 — Component Info backend
- master data
- selected-site planning values
- Standard Cost
- QCTC
- shared inventory
- null/partial-data behavior
- API/tests

8D.6 — Component Info frontend
- right-side card
- selection/reselection
- loading / partial / error behavior

8D.7 — AVL backend/frontend
- lazy query
- collapsed behavior
- reset on component change
- empty/error handling
- tests

8D.8 — Integrated validation / documentation / closeout
- automated test suite
- full build verification
- real-data validation
- manual guided owner test
- documentation reconciliation
- acceptance

You may recommend modest checkpoint boundary changes if repository reality makes them safer or more coherent.

Do not collapse everything into one implementation task.

---

# 16. Manual verification strategy

The final Stage 8 implementation should use:

1. automated tests and build/type/lint checks
2. agent-produced numbered manual guided testing instructions
3. project owner executes those steps and reports observations
4. agent fixes discrepancies
5. final owner acceptance

Do not plan repeated automated screenshot/navigation loops as the primary validation strategy.

---

# 17. Specific repository questions this preflight must answer

Your report must explicitly answer:

1. Where is the accepted Stage 6 inventory calculation currently implemented?

2. What exact rules/data structures does it currently use for:
   - Net QOH
   - Non-Net QOH
   - RMA

3. Is Stage 6 inventory currently reusable from another application service?
   If not, what is the smallest safe extraction?

4. Does it support multi-part/batch inventory retrieval?
   If not, where should batch support live?

5. Where and how does current code resolve:
   - workspace Site
   - QAD Domain

6. Where are current Stage 6/7 lazy-detail caches stored and keyed?

7. What current repository pattern should Stage 8 follow for:
   - QAD adapters
   - integration records
   - application services
   - API DTOs
   - endpoints
   - OpenAPI generation
   - frontend API client calls
   - loading/error states

8. How is Part Info currently loaded and cached?

9. How are Work Orders/Kitting currently loaded and cached?

10. Where is the Stage 7 component-number search implemented?
    Is there a reusable search/input component or should Stage 8 simply reproduce the interaction locally?

11. What detail-pane/tab component currently owns:
    - Part Info
    - Work Orders
    - other contextual tabs

12. What files would likely need modification for the BOM tab?

13. Does the repository already contain a BOM-related model/query/service from previous stages that must be reused or explicitly kept separate?

14. Are there any architecture tests that will constrain where new interfaces/models can live?

15. Are there current tests whose behavior Stage 8 could accidentally regress?

16. Do the proposed API resources fit current endpoint conventions?

17. What is the safest SQL Server 2016-compatible approach for reproducing the accepted legacy BOM traversal in the existing repository?
    Do not implement it yet.

18. Are there any repository facts that contradict or complicate the accepted Stage 8 design?

---

# 18. Required output

Return a report with exactly these major sections:

## A. Repository Baseline

Summarize:
- current branch/status if available
- current Stage 6/7 architecture relevant to Stage 8
- relevant project/file structure
- important existing patterns

Do not dump the full tree.

## B. Existing Capabilities Stage 8 Can Reuse

Cover:
- inventory
- workspace/site/domain resolution
- cache/snapshot behavior
- API patterns
- frontend detail-pane/tab patterns
- search behavior
- test/build infrastructure

For each item, cite exact repository file paths and important classes/functions.

## C. Stage 8 Design Reconciliation

Compare the accepted Stage 8 rules in this prompt against current repository documentation/code.

Classify each significant item:

- Already supported/reusable
- Requires extension
- Requires new implementation
- Repository conflict / needs owner decision

Do not invent resolutions to genuine conflicts.

## D. Proposed Backend Design

Provide repository-aware recommendations for:

- business/domain/application models
- QAD adapters
- shared inventory extraction/reuse
- BOM orchestration
- Component Info orchestration
- AVL
- caching/freshness
- error semantics

Name likely existing/new files where reasonably clear, but do not create them.

## E. Proposed API / Contract Design

Assess the candidate endpoints and DTO shapes against current conventions.

List:
- proposed routes
- request inputs
- response DTOs
- nullable/zero semantics
- Problem Details behavior
- OpenAPI/type-generation impact

No implementation.

## F. Proposed Frontend Design

Identify:
- current components likely affected
- tab changes
- BOM grid placement
- search implementation
- component-card placement
- AVL expansion behavior
- state/caching interactions

No implementation.

## G. Revised Stage 8D Implementation Checkpoints

For each recommended checkpoint provide:

- purpose
- files/areas likely affected
- dependencies
- acceptance criteria
- focused automated verification
- human review/stop point

Keep checkpoints bounded for a local coding model.

## H. Risks, Unknowns, and Required Owner Decisions

Separate:

- implementation risk
- data risk
- architecture risk
- genuinely unresolved business decisions

Do not list hypothetical worries without repository or source evidence.

## I. Recommended Next Prompt

Recommend the exact scope of Stage 8D.1 after human review.

Do not write the full Stage 8D.1 implementation prompt yet unless specifically asked.

## J. Confirmation of No Implementation

Explicitly state:

- whether any files were changed
- whether any commands with side effects were run
- whether any commits were created

Expected result for this checkpoint is no production changes.

---

# 19. Constraints

Preserve all existing project constraints, including:

- .NET 10 backend
- React 19 / TypeScript frontend
- Tauri 2 host
- SQL Server 2016 compatibility
- Windows Integrated Authentication for QAD
- read-only company database access
- QAD SQL isolated behind Kst.Integrations.Qad
- backend-owned business rules
- stable normalized application/API contracts
- C# DTOs authoritative for API contracts
- generated TypeScript types
- current project dependency boundaries
- existing snapshot/last-good-data philosophy
- no speculative architecture for future stages
- no direct database writes

Do not implement:

- Stage 9 Immediate Shortages
- PO drilldown
- Future Shortages
- Component MRP
- component requirement calculations
- coverage calculations
- exports
- unrelated refactors

Do not broaden Stage 8 because additional QAD fields are convenient to retrieve.

---

Perform the repository preflight now and return the requested planning report.

Stop after the plan for human review.
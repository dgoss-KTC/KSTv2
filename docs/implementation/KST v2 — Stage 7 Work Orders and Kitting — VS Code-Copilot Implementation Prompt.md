# KST v2 — Stage 7 Work Orders and Kitting — Implementation

You are working inside the **KST v2 repository in VS Code**.

Your task is to implement:

# Stage 7 — Phase 4: Work Orders and Kitting

Stages 1 through 6 are complete and accepted.

Stage 7 requirements discovery, source mapping, business rules, and backend/API contract design have been completed and accepted by the project owner.

This prompt is the implementation authority for Stage 7 unless current repository reality reveals a conflict.

Do **not** begin Stage 8 or any later phase.

---

# 1. Working method

Implement Stage 7 in controlled checkpoints.

Do **not** attempt the entire stage in one large change.

At the end of every checkpoint:

1. run the applicable automated verification;
2. summarize:
   - files changed;
   - behavior implemented;
   - tests added/changed;
   - test/build results;
   - unresolved issues;
3. stop and wait for project-owner review before proceeding to the next checkpoint.

If current repository structure differs from assumptions in this prompt:

- inspect the actual implementation;
- preserve the existing accepted architecture;
- make the smallest compatible change;
- report the discrepancy;
- do not redesign unrelated working infrastructure.

Do not modify production behavior merely to make a test easier.

---

# 2. Current accepted architecture

KST v2 currently uses:

- React 19 / TypeScript / Vite frontend
- Tauri 2 / Rust desktop host
- C# / .NET 10 / ASP.NET Core loopback backend
- OpenAPI-generated TypeScript contracts
- Windows-integrated read-only QAD access
- persistent site-specific scheduler workspaces
- real QAD-backed MPS
- atomic workspace MPS snapshots
- stale-last-good refresh behavior
- lazy-loaded Part Information
- PartDetail cache/freshness tied to workspace + selected parent + MPS snapshot generation

Preserve the existing project dependency direction.

Conceptually:

```text
Kst.Domain
    ↑
Kst.Application
    ↑
Kst.Infrastructure

Kst.Integrations.Qad
    → stable/domain-facing models

Kst.Integrations.Shortages
    → stable/domain-facing models

Kst.Exports
    → stable/domain-facing models

Kst.Api
    → application/infrastructure/integrations/exports
```

Important boundaries:

- QAD SQL belongs only inside `Kst.Integrations.Qad`.
- `Kst.Application` must not depend on SQL Server implementation packages.
- API endpoints should remain thin.
- Frontend must never know QAD table/field names.
- Stable internal models represent business concepts, not QAD schemas.
- C# DTOs are authoritative API contracts.
- TypeScript contracts are generated from OpenAPI.
- Never manually edit generated TypeScript API types.
- QAD access is read-only.
- Domain is resolved from selected Site by the backend/QAD integration layer.
- Do not create speculative shared abstractions for future stages.

Inspect the actual Stage 5/6 implementation before creating new patterns.

Reuse existing integration-reader → Application-interface → API/DI bridging behavior where appropriate.

---

# 3. Stage 7 scope

Stage 7 implements:

- MPS-bucket → Work Order drill-down
- Falldown → Work Order drill-down
- Work Order cards
- Work Order quantities and dates
- line-based Kitting percentage
- Work Order material/issue lines
- Variance Quantity
- Issued Percentage
- material-exception highlighting semantics
- local component-part search
- manufactured-component identification
- bounded candidate Work Order drill-down for manufactured subassemblies
- maximum three Work Order investigation levels
- Stage 7 lazy loading/cache/freshness behavior

Stage 7 does **not** implement:

- full Component/BOM Detail
- BOM explosion
- Component MRP
- immediate shortages
- future shortages
- inventory coverage
- purchase-order coverage
- PO drill-down
- buyer notes
- shared-component analysis
- finished goods
- planning workbook
- sales-order investigation

Do not preload Stage 8+ data merely because relevant QAD tables are accessible.

---

# 4. Accepted Stage 7 UI behavior

## Parent selection

Clicking the Parent Part:

- selects/focuses the parent;
- clears any selected schedule bucket;
- exposes Part Info;
- leaves Work Orders disabled;
- leaves later contextual tabs such as Shortages, Future Shortages, and Components disabled.

No parent-only “all open Work Orders” view is required.

## Schedule bucket selection

Clicking an eligible MPS week cell or Falldown:

- selects the parent + schedule context;
- automatically opens Work Orders;
- enables appropriate contextual investigation behavior.

Top-level Work Order drill-down is available for:

```text
Falldown
+
first 6 forward MPS weeks
```

The value `6` must be a clearly named frontend constant/configuration value so it can be changed later without restructuring UI code.

Do not truncate or change the underlying MPS snapshot.

Weeks after the Stage 7 drill-down horizon simply do not expose the Work Order action.

## Top-level Work Order membership

Do **not** reconstruct selected-bucket WO membership from Work Order dates.

The existing MPS snapshot already retains Work Order references contributing to each bucket.

Use those retained references as the authoritative top-level Stage 7 context.

---

# 5. Eligible Work Orders

Only these statuses receive Stage 7 Work Order cards:

```text
A = Allocating
F = Frozen
R = Released
```

No other Work Order status should produce a Stage 7 card.

Do not treat:

```text
P
e
C
```

as Stage 7 Work Order-card statuses.

RMA Work Orders using:

```text
wo_bom_code = 'RMABOM'
```

must be excluded where candidate Work Orders are retrieved.

Preserve existing MPS RMA behavior.

---

# 6. User-facing Work Order identity

The scheduler-facing Work Order identity is:

```text
WOID
```

QAD source:

```text
wo_mstr.wo_lot
```

Important:

- Work Order Number is **not** unique.
- Work Order Number is reused.
- Schedulers do not care about it for this workflow.
- Do not display it on the Work Order card.
- Do not model it as the user-facing Work Order identity merely because source joins may contain it elsewhere.

Do not break any existing Stage 5 source join merely to enforce this UI rule.

Database relationship keys and user-facing business identity are separate concepts.

---

# 7. Work Order card

Each Stage 7 Work Order card displays:

```text
WOID
Status
Ordered
Completed
Open
Release
Due
Kitting %
```

Do not display:

```text
Work Order Number
Production Line
Start Date
PM Code
```

Accepted QAD sources:

```text
WOID       = wo_mstr.wo_lot
Status     = wo_mstr.wo_status
Ordered    = wo_mstr.wo_qty_ord
Completed  = wo_mstr.wo_qty_comp
Release    = wo_mstr.wo_rel_date
Due        = wo_mstr.wo_due_date
```

Accepted derived rule:

```text
Open = Ordered - Completed
```

The project owner validated against QAD that there is no separate authoritative Open Quantity field required for this workflow.

Cards must support multiple WOs in one selected MPS bucket.

Use semantic A/F/R presentation consistent with existing application conventions.

---

# 8. Kitting definition

Kitting is **line-based**, not quantity-weighted.

Material source:

```text
wod_det
```

Accepted rule:

```text
Applicable material line
    = wod_qty_req <> 0

Fully issued line
    = wod_qty_iss >= wod_qty_req

Kitting %
    = Fully Issued Line Count
      / Applicable Line Count
      × 100
```

Important:

- exact 100% issue counts as fully issued;
- over-issued lines count as fully issued;
- one material line contributes at most one line to the numerator;
- `wod_qty_req = 0` lines are excluded because they may represent phantoms or otherwise non-meaningful issue requirements;
- if there are zero applicable material lines, Kitting is **N/A / null**, not 0%.

Do not calculate Kitting as total issued quantity divided by total required quantity.

Do not deduplicate material lines by component part unless real evidence contradicts the already validated source behavior.

The project owner tested representative real WOs and confirmed the current `wod_det` row behavior is suitable for the line-based calculation.

---

# 9. Work Order material detail

Opening Kitting for a WOID lazy-loads all applicable material lines for that Work Order.

Source relationship is conceptually:

```text
wo_mstr
    wo_domain + wo_lot
        ↓
wod_det
    wod_domain + wod_lot
```

Material fields:

```text
Component
Description
BOM Qty
Issued Qty
Variance Qty
Issued %
```

Sources:

```text
Component
    = wod_det.wod_part

Description
    = pt_mstr.pt_desc1
      joined by domain + component part

BOM Qty
    = wod_det.wod_qty_req

Issued Qty
    = wod_det.wod_qty_iss
```

Derived fields:

```text
Variance Qty
    = Issued Qty - BOM Qty

Issued %
    = Issued Qty / BOM Qty × 100
```

Do not call `Issued %` “Variance %”.

The older KST query used that nomenclature, but the value being displayed is percentage issued.

The project owner explicitly prefers seeing values such as:

```text
120% issued
```

rather than translating that into:

```text
20% over-issued
```

---

# 10. Material issue classification

Backend/domain semantics:

```text
Issued % <= 95
    → Under-Issued Exception

95 < Issued % < 105
    → Within Expected Range

Issued % >= 105
    → Over-Issued Exception
```

This corresponds to the accepted expectation that approximately 2–4% variance may occur due to scrap and normal production effects, while 5% or more warrants attention.

The backend should return semantic status.

The frontend owns visual font/color treatment.

Do not duplicate threshold calculations independently in multiple frontend components.

---

# 11. Manufactured components

A material component is considered manufactured when:

```text
pt_mstr.pt_pm_code = 'M'
```

Normalize that to a business flag such as:

```text
IsManufactured = true
```

Do not expose PM Code itself in the visible Work Order material grid.

Visual behavior:

- manufactured material rows receive a distinct background treatment;
- manufactured rows receive a non-color drill affordance such as a chevron before/near the component part;
- variance exception changes font/text treatment;
- manufactured background and variance font styling must coexist.

At maximum drill depth, manufactured identity remains visible, but deeper navigation is disabled.

---

# 12. Material grid search/filter

Some real Work Orders contain 250+ material lines.

The Work Order material grid requires a local Component Part search.

Behavior:

- partial-match;
- case-insensitive;
- updates as the scheduler types;
- scoped to the currently expanded WO material list;
- easy to clear;
- does not query QAD on every keystroke.

All material lines must initially be retrieved for the selected WO.

Filtering is frontend-local.

Default unfiltered sorting:

1. variance/material exceptions first;
2. larger departures from 100% Issued before smaller departures;
3. deterministic component/operation ordering after severity.

Do not hide lines that fall inside the expected range.

---

# 13. Manufactured subassembly candidate navigation

QAD does **not** provide a reliable logical parent↔subassembly Work Order relationship.

Do not fabricate one.

The Stage 7 workflow instead uses:

```text
Manufactured BOM component
    ↓
candidate Work Orders for that component
```

The UI must use truthful language such as:

```text
Work Orders for <Component Part>
```

Do not label candidates:

```text
Child Work Orders
Linked Work Orders
Related Work Orders
```

in a way that implies proven pegging.

Accepted candidate rule:

```text
same domain
same site
same manufactured component part
WO status IN ('A','F','R')
wo_bom_code <> 'RMABOM'
candidate Due Date <= immediate parent WO Due Date
```

Sort candidates:

```text
Due Date descending
Release Date descending
WOID deterministic tie-break
```

The intent is to show the closest preceding eligible candidate WOs first.

Initial candidate limit:

```text
10
```

Keep this as a clearly named backend constant/configuration value.

Retrieve enough information to determine whether additional candidates existed beyond the displayed limit.

Return truncation metadata such as:

```text
isTruncated
```

so the UI can truthfully communicate that only the closest candidates are shown.

The project owner tested the candidate strategy against live QAD and accepted it as suitable.

---

# 14. Drill depth

Maximum Work Order investigation depth:

```text
3 levels
```

Conceptually:

```text
Level 1
MPS bucket
    ↓
actual scheduled parent WO

Level 2
manufactured material component
    ↓
candidate WO for that component

Level 3
manufactured component within Level-2 WO
    ↓
candidate WO for that component
```

At Level 3:

- material details remain available;
- manufactured rows remain visually identifiable;
- deeper Work Order navigation is disabled.

Do not implement Level 4.

Keep the maximum depth in a clearly named backend/domain policy value.

Prefer one expanded manufactured-component branch per nested level so the UI does not grow without bound.

---

# 15. Normalized domain concepts

Create the smallest domain model necessary for Stage 7.

Likely concepts:

```text
WorkOrderStatus
WorkOrderSummary
KittingSummary
WorkOrderMaterialLine
WorkOrderIssueStatus
```

A likely conceptual shape is:

```text
WorkOrderSummary
    PartNumber
    Woid
    Status
    OrderedQuantity
    CompletedQuantity
    OpenQuantity
    ReleaseDate
    DueDate
    Kitting
```

```text
KittingSummary
    ApplicableLineCount
    FullyIssuedLineCount
    Percent?
```

```text
WorkOrderMaterialLine
    ComponentPart
    ComponentDescription?
    RequiredQuantity
    IssuedQuantity
    VarianceQuantity
    IssuedPercent
    IssueStatus
    IsManufactured
```

Do not include QAD-specific names in stable business models.

Do not create broad generic BOM/material/inventory frameworks for future phases.

---

# 16. QAD reader boundaries

Create focused QAD retrieval behavior inside `Kst.Integrations.Qad`.

Likely responsibilities:

## Work Order summary reader

Retrieves:

- WO header/card values;
- applicable material-line count;
- fully issued-line count;
- candidate subassembly WOs.

## Work Order material reader

Retrieves:

- all applicable material issue rows for one WOID;
- component description;
- hidden manufactured-component source information.

The exact class/interface names should follow existing repository conventions after inspection.

Use:

- existing site→domain resolution;
- existing Windows-integrated QAD connection behavior;
- Dapper/SqlClient conventions already present;
- parameterized SQL;
- SQL Server 2016-compatible syntax;
- cancellation-token propagation;
- existing command-timeout strategy;
- existing read-uncommitted strategy;
- safe diagnostic logging.

No database writes.

---

# 17. Application orchestration

Do not create independent speculative services merely because the old checklist mentioned:

```text
KittingService
VarianceService
```

A focused Work Order drill-down orchestration service is preferred if it fits current repository conventions.

Conceptual use cases:

```text
Get Work Orders for selected MPS bucket
Get material lines for selected WOID
Get candidate WOs for manufactured component
```

Pure calculations should live with appropriate domain/business-rule code rather than being scattered across API and frontend layers.

---

# 18. Lazy loading and cache behavior

Stage 7 must remain lazy-loaded.

Do not attach all Work Order details and material lines to the initial MPS snapshot.

Conceptual progression:

```text
MPS loaded
    ↓
bucket selected
    ↓
lazy-load WO cards
    ↓
Kitting opened
    ↓
lazy-load material rows
    ↓
manufactured component selected
    ↓
lazy-load candidate WOs
```

Cache/freshness is tied to:

```text
workspace
+
MPS snapshot generation
```

Conceptual cache keys:

```text
WO summary
    Workspace + Snapshot + WOID

Material detail
    Workspace + Snapshot + WOID

Subassembly candidates
    Workspace + Snapshot
    + Immediate Parent WOID
    + Component Part
    + Target Depth
```

Within one unchanged MPS snapshot:

- reopening already-loaded WO detail may reuse cache;
- reopening Kitting may reuse material data;
- repeating the same manufactured-component drill may reuse candidate results;
- Due/Release UI toggling does not invalidate Stage 7 cache;
- horizon changes do not invalidate Stage 7 cache;
- local material search does not invalidate/reload anything.

Failed lazy-load queries must not be cached as successful data.

---

# 19. Workspace refresh behavior

## Successful MPS refresh

After atomic replacement with a new MPS snapshot:

- prior Stage 7 investigation context is invalid;
- clear selected bucket/WO drill-down state;
- old WO/material/candidate cache must not appear beneath the new snapshot;
- future selection lazy-loads data against the new snapshot generation.

## Failed MPS refresh

The existing last-good MPS snapshot remains authoritative.

Preserve compatible Stage 7 investigation/cache associated with that retained snapshot.

Do not clear working last-good Stage 7 data merely because a refresh attempt failed.

---

# 20. API contract

Follow existing repository conventions after preflight.

The accepted conceptual API surface contains three capabilities.

## A. Selected bucket → top-level Work Orders

Structured read-only query using:

```text
Workspace
Snapshot ID
Parent Part
Bucket/Falldown context
```

Use the existing MPS snapshot to determine contributing WO references.

Do not use frontend-provided WO lists as authoritative membership.

No eligible A/F/R WOs is a valid:

```text
200 + empty list
```

not an API error.

## B. WOID → material detail

Lazy-load material rows for one WOID.

Return:

- snapshot context;
- WOID;
- Kitting Summary;
- all applicable WorkOrderMaterialLine DTOs.

Do not implement server-side part-number filtering for the already loaded list.

## C. Manufactured component → candidate WOs

Request includes conceptually:

```text
Workspace
Snapshot
Immediate Parent WOID
Component Part
Target Depth
```

The backend resolves the immediate parent's Due Date.

Do not trust a frontend-supplied date as the candidate boundary.

Validate:

- component is manufactured;
- target depth is allowed;
- current snapshot is still the requested snapshot.

Return candidate `WorkOrderSummary` DTOs plus result-limit/truncation metadata.

---

# 21. Problem Details / error states

Follow current API Problem Details conventions.

Differentiate legitimate empty states from failures.

Expected cases include:

```text
workspace not found
snapshot unavailable
snapshot changed/replaced
work order not found
component not manufactured
maximum drill depth exceeded
parent Work Order Due Date unavailable
QAD unavailable
```

If snapshot context changes between selection and lazy request, do not silently combine old UI context with a new snapshot.

Return a suitable conflict/error response so the frontend can clear the stale drill-down.

Do not expose SQL, credentials, connection strings, or raw database exceptions to the frontend.

---

# 22. Frontend presentation

Use the current application/prototype styling as visual guidance, but implement the accepted business workflow rather than copying prototype mock logic.

## Work Order card

Display:

```text
WOID
Status
Ordered
Completed
Open
Release
Due
Kitting %
```

Provide Kitting expand/collapse behavior.

## Material grid

Display:

```text
Component
Description
BOM Qty
Issued Qty
Variance Qty
Issued %
```

Requirements:

- all applicable lines visible;
- local Component Part filter;
- exception-first sorting;
- manufactured background;
- variance text styling;
- manufactured chevron/action;
- combined styling supported.

## Nested candidate cards

Display the same Work Order-card data model.

Use truthful heading:

```text
Work Orders for <Part>
```

When candidate results were truncated, communicate that only the nearest/bounded candidates are shown.

Do not imply database pegging.

---

# 23. Empty/loading/error states

Implement deliberate states for:

## No eligible top-level WOs

Example meaning:

```text
No active A/F/R work orders found for this schedule bucket.
```

## WO has no applicable material lines

Kitting:

```text
N/A
```

Material detail should explain that no applicable material requirements were found.

## Manufactured part has no candidate WOs

Example meaning:

```text
No active preceding work orders found for this part.
```

## Lazy QAD request fails

Show an error/retry state.

Do not represent failure as empty data.

## Stage 7 data invalidated by successful MPS refresh

Clear drill-down and return user to current refreshed schedule context.

---

# 24. Checkpoint implementation sequence

Follow these checkpoints in order.

# Checkpoint 7D.0 — Repository Preflight

Before editing production code:

1. inspect repository status;
2. inspect current Stage 5/6 MPS models;
3. inspect `MpsWorkOrderRef` and bucket representation;
4. inspect Stage 6 lazy PartDetail/cache implementation;
5. inspect QAD reader/Application interface pattern;
6. inspect current API/Problem Details conventions;
7. inspect frontend parent-selection and detail-tab behavior;
8. inspect existing tests;
9. reconcile the Stage 7 section of the tracked Master Project Checklist.

Report findings.

Do not write Stage 7 production code during preflight unless a trivial documentation-only reconciliation is clearly necessary.

**STOP after Checkpoint 7D.0 and wait for owner approval.**

---

# Checkpoint 7D.1 — Domain and Application Business Rules

Implement:

- normalized WO status;
- WorkOrderSummary;
- KittingSummary;
- WorkOrderMaterialLine;
- WorkOrderIssueStatus;
- Open Quantity calculation;
- Kitting calculations;
- Issued %;
- Variance Qty;
- exception thresholds;
- manufactured semantic flag/policy;
- drill-depth policy.

Add focused unit tests.

Do not add SQL or frontend UI yet.

Run relevant backend tests.

Report results.

**STOP after Checkpoint 7D.1.**

---

# Checkpoint 7D.2 — QAD Readers

Implement:

- Work Order summary retrieval;
- Kitting line-count retrieval;
- material-line retrieval;
- component description enrichment;
- manufactured-component enrichment;
- candidate Work Order retrieval;
- A/F/R filtering;
- `RMABOM` exclusion;
- candidate ordering/limit/truncation behavior.

Add adapter/reader tests consistent with current repository testing strategy.

Do not add frontend UI yet.

Run backend build/tests.

Perform narrowly scoped live-QAD validation if environment access permits.

Report actual query/result findings without exposing sensitive data.

**STOP after Checkpoint 7D.2.**

---

# Checkpoint 7D.3 — Application Orchestration and Cache

Implement:

- bucket → retained MPS WO reference resolution;
- Falldown resolution;
- lazy Work Order summary orchestration;
- material-detail orchestration;
- manufactured candidate orchestration;
- Stage 7 cache keys/freshness;
- successful MPS-refresh invalidation behavior;
- failed-refresh last-good preservation;
- lazy-query retry semantics.

Add application tests.

Do not build full frontend yet.

Run backend tests.

**STOP after Checkpoint 7D.3.**

---

# Checkpoint 7D.4 — API / OpenAPI

Implement accepted Stage 7 API endpoints and DTOs.

Add/extend:

- Problem Details;
- snapshot validation;
- empty-result behavior;
- candidate-depth validation;
- candidate truncation metadata.

Then:

```powershell
cd src/backend
dotnet build Kst.slnx
```

Regenerate TypeScript contracts:

```powershell
cd ../frontend
npm run generate:types
npm run typecheck
```

Never manually edit generated API types.

Add API integration tests.

Report endpoint shapes and verification results.

**STOP after Checkpoint 7D.4.**

---

# Checkpoint 7D.5 — Frontend Selection and WO Cards

Implement:

- parent-only context;
- contextual tabs disabled without bucket;
- eligible bucket click;
- Falldown click;
- automatic Work Orders tab activation;
- first-six-week drill policy;
- Work Order loading/empty/error states;
- Work Order cards;
- A/F/R presentation;
- Kitting summary/progress;
- multiple-WO bucket behavior.

Do not implement nested manufactured-component drill-down yet.

Add frontend tests.

Run:

```powershell
npm run lint
npm run typecheck
npm test
npm run build
```

**STOP after Checkpoint 7D.5.**

---

# Checkpoint 7D.6 — Kitting Material Grid

Implement:

- lazy Kitting expansion;
- material grid;
- all applicable material rows;
- Component Part search;
- partial/case-insensitive local filtering;
- clear/reset;
- exception-first sorting;
- variance font styling;
- manufactured background;
- manufactured chevron/non-color affordance;
- combined manufactured + variance presentation;
- no-applicable-material state.

Validate responsiveness using representative 250+ line data if available.

Add frontend tests.

Run frontend verification.

**STOP after Checkpoint 7D.6.**

---

# Checkpoint 7D.7 — Manufactured Candidate Drill-Down

Implement:

- manufactured row selection;
- candidate WO lazy load;
- truthful candidate heading;
- candidate cards;
- candidate truncation presentation;
- candidate Kitting expansion;
- Level-2 and Level-3 navigation;
- maximum three levels;
- disabled deeper navigation at Level 3;
- one expanded manufactured branch per level where practical;
- no-candidate state.

Do not implement true BOM explosion or shortage functionality.

Add frontend/API/application tests as required.

Run full relevant verification.

**STOP after Checkpoint 7D.7.**

---

# Checkpoint 7D.8 — Live-QAD Validation

Create a tracked Stage 7 real-data validation record.

Validate:

## Top-level

- one-WO bucket;
- multi-WO bucket;
- Falldown;
- no eligible A/F/R WO;
- first-six-week behavior.

## Card values

Compare directly with QAD:

- WOID;
- status;
- ordered;
- completed;
- open;
- release;
- due;
- Kitting line counts;
- Kitting %.

## Material detail

Validate:

- Component;
- Description;
- BOM Qty;
- Issued Qty;
- Variance Qty;
- Issued %;
- zero-required exclusion;
- under-issue;
- over-issue;
- manufactured classification;
- representative repeated material rows.

## Large BOM

Validate:

- 250+ line material set;
- partial Part Number search;
- clear/reset;
- acceptable responsiveness.

## Candidate subassembly

Validate:

- same site;
- same manufactured part;
- A/F/R only;
- `RMABOM` exclusion;
- Due <= immediate parent Due;
- closest Due first;
- result limit;
- truncation behavior;
- no-candidate case;
- Level 2;
- Level 3;
- no Level 4.

## Refresh

Validate:

- successful refresh clears old Stage 7 investigation context;
- newly selected bucket loads against new snapshot;
- failed refresh preserves last-good compatible behavior.

Record discrepancies and resolutions.

Do not silently change accepted rules to fit unexpected data.

Report any discrepancy before modifying business meaning.

**STOP after Checkpoint 7D.8 for owner validation.**

---

# Checkpoint 7D.9 — Full Regression and Documentation

After owner confirms live-data behavior:

Run backend verification appropriate to the repository, including:

```powershell
cd src/backend
dotnet format Kst.slnx --verify-no-changes
dotnet build Kst.slnx --nologo
dotnet test Kst.slnx --nologo
```

Frontend:

```powershell
cd ../frontend
npm run generate:types
npm run lint
npm run typecheck
npm test
npm run build
```

Tauri/Rust:

```powershell
cd ../tauri
cargo check
```

If backend source changed, rebuild the Tauri sidecar using the repository's established script:

```powershell
cd C:\Dev\kst_v2
.\scripts\build-sidecar.ps1
```

Then launch normal Tauri development mode and perform manual regression:

- workspace configuration;
- MPS;
- Due/Release mode;
- horizon changes;
- Falldown;
- Part Info;
- Stage 7;
- single-instance behavior;
- clean shutdown.

Do not perform packaged-installer validation unless Stage 7 changes affect packaged/runtime behavior or current repository completion policy requires it.

Update durable documentation, including:

- Stage 7 data inventory;
- Stage 7 Work Order/Kitting contract;
- Stage 7 validation report;
- current project status;
- Master Project Checklist;
- API/OpenAPI documentation;
- relevant QAD field/source documentation.

Document explicitly that:

> QAD does not provide a reliable parent-to-subassembly Work Order relationship. Stage 7 manufactured-component navigation shows bounded candidate Work Orders based on part, site, status, and preceding Due Date. It must not be described as Work Order pegging.

**STOP after Checkpoint 7D.9 and request final Stage 7 owner acceptance.**

---

# 25. Required automated business-rule tests

At minimum cover:

## Domain

- Open = Ordered - Completed.
- exact 100% issue = fully issued.
- over-issued line = fully issued.
- partial line = not fully issued.
- zero-required line excluded.
- zero applicable lines → Kitting null/N/A.
- partial Kitting %.
- 100% Kitting.
- Variance Qty.
- Issued %.
- 95% boundary.
- under 95%.
- 105% boundary.
- above 105%.
- manufactured flag.
- maximum drill depth.

## QAD/integration behavior

- A/F/R accepted.
- other statuses excluded.
- `RMABOM` candidate excluded.
- material-line mapping.
- component description.
- PM Code normalization.
- candidate Due-Date boundary.
- candidate ordering.
- candidate limit.
- candidate truncation detection.

## Application

- selected bucket uses retained MPS WO references.
- Falldown uses retained MPS WO references.
- no eligible WO.
- snapshot mismatch.
- cache reuse.
- successful-refresh invalidation.
- failed-refresh retention.
- failed lazy load not cached.

## API

- bucket success.
- bucket empty.
- material success.
- candidates success.
- invalid manufactured-component request.
- max-depth failure.
- snapshot conflict.
- QAD unavailable response.
- serialization/OpenAPI contract.

## Frontend

- parent disables Work Orders.
- bucket automatically opens Work Orders.
- Falldown opens Work Orders.
- week 1–6 drill behavior.
- later week disabled.
- card rendering.
- Kitting N/A.
- material expansion.
- Part Number filtering.
- clear search.
- exception styling.
- manufactured styling.
- combined styling.
- candidate expansion.
- no-candidate state.
- maximum depth.
- successful snapshot refresh clears drill-down.
- failed refresh preserves retained-state behavior.

---

# 26. Stage 7 completion gate

Do not declare Stage 7 complete until all of the following are true:

- Parent-only selection exposes Part Info but not Work Order drill-down.
- Eligible MPS bucket/Falldown automatically opens Work Orders.
- Only A/F/R Work Orders receive cards.
- WOID is the visible Work Order identity.
- Card quantities/dates match live QAD.
- Open Quantity matches validated formula.
- Kitting matches the accepted line-based rule.
- zero-required rows are excluded.
- no applicable lines produce N/A.
- material detail matches live QAD.
- Issued % and Variance Qty are correct.
- 95%/105% exception semantics work.
- large material lists can be rapidly filtered by Part Number.
- manufactured components are visually identifiable and drillable.
- candidate WOs satisfy accepted site/part/status/date/RMABOM rules.
- candidate navigation never implies unsupported Work Order lineage.
- candidate result limits/truncation behave truthfully.
- maximum drill depth is three.
- lazy cache/freshness behavior works.
- successful MPS refresh invalidates prior Stage 7 context.
- failed refresh retains last-good behavior.
- empty/loading/error/retry states work.
- backend/API/frontend automated verification passes.
- representative live-QAD validation passes.
- existing Stage 1–6 functionality does not regress.
- documentation is reconciled.
- project owner explicitly accepts Stage 7.

Final Stage 7 completion statement:

> A scheduler can select a near-term MPS bucket or Falldown, inspect its active A/F/R work orders and their kitting/material-issue state, rapidly search large component lists, and follow manufactured components through bounded candidate-work-order drill-downs without KST implying unsupported work-order lineage.

---

# 27. Begin now

Begin only with:

# Checkpoint 7D.0 — Repository Preflight

Inspect the actual repository and report the findings.

Do not begin Stage 7 production implementation until the preflight findings have been reviewed by the project owner.
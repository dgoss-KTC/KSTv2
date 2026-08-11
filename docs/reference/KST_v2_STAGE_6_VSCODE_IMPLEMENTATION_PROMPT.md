# KST v2 — Stage 6 Part Information Drill-Down — VS Code/Copilot Implementation Prompt

You are working inside the KST v2 repository in VS Code.

Your task is to implement:

# Stage 6 — Phase 3: Part Information Drill-Down

Stages 1 through 5 are complete and accepted. Stage 6A (UI/field discovery), Stage 6B (source mapping/business rules), and Stage 6C (backend/API contract) are accepted.

The authoritative Stage 6 contract is:

```text
docs/implementation/KST_v2_STAGE_6_PART_INFO_CONTRACT.md
```

If the repository uses a slightly different documentation path, locate the accepted Stage 6 contract by filename/content and follow repository conventions.

Do not redesign the accepted contract unless current repository evidence makes a specific item impossible or contradictory. If a contradiction is found, stop that specific work item, document the evidence, and report it rather than silently changing business behavior.

---

## 1. Stage 6 completion target

Implement this end-to-end behavior:

> Selecting an MPS parent part collapses/focuses the MPS around that parent and displays validated QAD part-master attributes, inventory summaries, and current MOQ/price information in a lazy-loaded Part Info pane.

The user can return to the full MPS grid with a clear `Back to full grid` action.

Part Info is part-scoped, not week-scoped.

---

## 2. Hard scope boundaries

Implement only Stage 6.

Do **not** begin Stage 7 — Work Orders and Kitting.

Do not implement or preload:

- Work Order cards/details
- Kitting percentage
- WIP
- component count
- BOM explosion
- component detail
- shortage calculations
- purchase-order detail/coverage
- buyer comments
- sales-order demand
- Finished Goods lot/location/RMA detail
- Part Info MPS schedule-status rollups
- UOM or Item Class merely because they existed in the prototype
- direct database writes
- a generic cross-workspace part browser
- speculative shared services for future stages

The prototype is visual/product evidence only. The accepted Stage 6 contract supersedes its mock field list where they differ.

---

## 3. First action: repository preflight

Before editing production code, inspect the current repository and reconcile this prompt with what Stage 5 actually implemented.

At minimum inspect:

- solution/project structure under `src/backend`
- current project dependency/architecture tests
- current QAD integration patterns from Stage 5
- QAD connection/options/domain-resolution behavior
- current `READ UNCOMMITTED` / read-only SQL conventions
- Stage 5 MPS source reader/query patterns
- current MPS application/snapshot service and snapshot identity/generation model
- current workspace MPS API route and Problem Details conventions
- current API DTO/OpenAPI generation workflow
- current frontend API client and generated types
- current MPS grid component/state structure
- current workspace refresh behavior
- current styling/CSS patterns used by the accepted Stage 5 grid
- current test organization

Also read:

- `docs/implementation/KST_v2_STAGE_6_PART_INFO_CONTRACT.md`
- `KST-v2-Master-Project-Checklist.md`
- `BACKEND_PROJECT_BOUNDARIES.md` or its current canonical replacement
- `API_CONTRACT_WORKFLOW.md`
- `OPENAPI_CLIENT_GENERATION.md`
- current Stage 5 MPS contract/snapshot/closeout docs

Do not assume historical paths are still exact. Locate the canonical current files.

### Preflight output

Before large implementation changes, create/update a concise Stage 6 implementation progress document under the repository's existing status/implementation conventions recording:

- files/patterns discovered
- current Stage 5 snapshot identity mechanism
- current QAD adapter/interface pattern
- exact files expected to change
- any contract-to-repository naming adjustments that do not change semantics

Then proceed with implementation. Do not ask the project owner for repository information that can be discovered locally.

---

## 4. Accepted PartDetail fields

Implement only these Stage 6 fields.

### Part master

Source: `pt_mstr`

| KST concept | QAD field | Rule |
|---|---|---|
| Part Number | `pt_part` | selected parent identity |
| Planner | `pt_buyer` | manufactured parent → planner code |
| Mfg Lead Time | `pt_mfg_lead` | days |
| Safety Time | `pt_sfty_time` | days |
| Part Status | `pt_status` | return raw code + backend description |
| Current Revision | `pt_rev` | string |
| Description | `pt_desc1` | string; same source accepted for Stage 5 MPS |
| IOS Code | `pt_warr_cd` | string |
| Safety Stock | `pt_sfty_stk` | part units |

Blank/null informational values are allowed. Zero has no special missing/not-configured meaning for these informational fields.

Use actual SQL metadata/current repository conventions to choose CLR numeric types. Do not truncate source precision merely because a value is usually integral.

### Part Status descriptions

Backend owns this mapping and returns both code and description:

```text
A → AEMR
B → BYPASS
C → CURRENT
E → END OF LIFE
F → FORECAST
H → PURCHASING HOLD
I → INACTIVE PURCHASED PARTS
M → MFA
N → NPI
O → OBSOLETE
P → PROTO
Q → QUOTED PARTS
U → UNRELEASED
```

Unknown codes must not fail PartDetail. Preserve the raw code and do not invent a description.

---

## 5. Inventory summary

The investigative query supplied during planning was evidence of the source and rules, not a verbatim implementation requirement. Build a focused query for the exact selected domain + site + part.

Source relationships:

```text
ld_det
  → loc_mstr using domain + site + location
  → is_mstr using domain + location-status classification
```

Only consider:

```text
ld_qty_oh > 0
```

Zero and negative balances are intentionally ignored as the accepted business rule.

RMA lots are identified by:

```text
ld_lot LIKE 'RA%'
```

RMA is excluded from both Stage 6 display totals.

Calculate:

```text
Qty On Hand = positive, non-RMA, nettable inventory
Qty Non-Net = positive, non-RMA, non-nettable inventory
```

No qualifying rows must produce:

```text
quantityOnHand = 0
quantityNonNet = 0
```

Do not recreate the old `EligibleParts` CTE. The current workspace/MPS context already supplies the selected parent.

---

## 6. MOQ / price

The planning SQL was source evidence, not a requirement to copy it verbatim.

For the exact selected part/domain:

1. Find `pi_mstr` rows for the part/domain where:

```text
pi_start <= today
```

2. Choose the row with the most recent `pi_start`.
3. Join its `pi_list_id` + domain to `pid_det`.
4. Return all current price tiers:

```text
MinimumOrderQuantity = pid_qty
UnitPrice            = pid_amt
```

5. Normalize tiers in MOQ-ascending order for stable presentation.

The accepted business rule intentionally uses only `pi_start <= today`; do not add an end/expiration-date rule.

Most parts have one MOQ/price pair. Some have multiple price breaks. Model the response as a collection in both cases.

No current price is not an error; return an empty collection.

---

## 7. Domain / source context

PartDetail begins from:

```text
WorkspaceId + selected Parent Part
```

Use the current workspace to obtain Site and the existing Stage 5 QAD integration behavior to infer Domain.

The frontend must not provide Domain.

Customer code is not part of the authoritative lookup context.

Verify the selected part belongs to the current resolved workspace/MPS parent scope. Explicit configured parents with zero current MPS facts remain valid parents if Stage 5's accepted snapshot/scope model includes them.

---

## 8. Backend model and boundaries

Implement the smallest sufficient normalized contract.

Conceptually:

```text
PartDetail
  Site
  PartNumber
  PlannerCode
  ManufacturingLeadTimeDays
  SafetyTimeDays
  PartStatusCode
  PartStatusDescription
  CurrentRevision
  Description
  IosCode
  SafetyStockQuantity
  QuantityOnHand
  QuantityNonNet
  PriceBreaks[]
  LoadedAtUtc
  IsStale
  Warning?
```

```text
PartPriceBreak
  MinimumOrderQuantity
  UnitPrice
```

Do not create speculative `PartAttributes`, `PartPlanningParameters`, `PartInventorySummary`, BOM, WIP, or future-stage models unless current repository architecture truly requires a mechanical wrapper.

### Integration responsibility

QAD-specific SQL, table names, and row records remain inside `Kst.Integrations.Qad` (or the current canonical Stage 5 integration boundary).

Reuse existing:

- QAD connection options
- Windows Integrated authentication
- site/domain mapping
- Dapper / `Microsoft.Data.SqlClient`
- parameterization conventions
- cancellation propagation
- timeout conventions
- read-only / `READ UNCOMMITTED` behavior
- diagnostics/logging conventions

Do not add credentials, connection strings, or raw SQL exception content to user-visible responses/log output beyond existing safe diagnostics conventions.

### Application responsibility

Create the smallest Stage 6 application use case/service needed to:

- resolve workspace and current MPS snapshot
- validate parent scope
- lazy-load PartDetail
- map Part Status code → description
- compose stable PartDetail
- manage cache/freshness
- return missing/stale/error outcomes

Follow the current Stage 5 interface/dependency pattern. Do not alter the solution dependency graph simply to match names in this prompt.

---

## 9. Cache and refresh behavior

PartDetail must not preload with the MPS.

### Data identity

```text
Site + Parent Part
```

### Freshness/cache identity

Use:

```text
Workspace + Parent Part + current MPS snapshot identity/generation
```

or the exact repository equivalent discovered during preflight.

Do not invent a TTL system.

Required behavior:

```text
MPS loads
  → no PartDetail query

Select uncached parent
  → query QAD
  → cache detail against current MPS snapshot identity

Reopen same parent against same snapshot
  → reuse cache

Due/Release change
  → no PartDetail query

Horizon change
  → no PartDetail query

Fiscal display change
  → no PartDetail query
```

Workspace refresh behavior:

- failed MPS refresh preserves existing compatible PartDetail cache
- successful MPS refresh creates/uses a new snapshot identity
- prior PartDetail becomes stale for the next access
- next access attempts a fresh PartDetail query
- successful query replaces cache
- if that fresh query fails and older detail exists, return the older detail as stale last-good with a warning
- if no previous PartDetail exists, return the normal unavailable error

Do not persist PartDetail across sessions initially.

---

## 10. API

Implement:

```http
GET /api/v1/workspaces/{workspaceId}/part-detail?partNumber={partNumber}
```

Follow current endpoint registration, DTO, validation, logging, and Problem Details patterns.

### Response semantics

Return normalized PartDetail including:

- site
- part number
- planner code
- manufacturing lead time days
- safety time days
- status code
- status description
- current revision
- description
- IOS code
- safety stock quantity
- Qty On Hand
- Qty Non-Net
- price-break collection
- loaded timestamp
- stale flag
- optional warning

### Error semantics

Implement/reconcile with current API conventions:

- workspace missing → `404`
- workspace MPS not loaded → `409`
- part not in current workspace parent scope → `404`
- parent in scope but `pt_mstr` missing → `404` missing-part state
- initial QAD failure with no cached PartDetail → `503` generic user-facing Problem Details
- refresh-generation PartDetail failure when older detail exists → `200` stale last-good response with warning
- blank/null ordinary fields → `200`
- no price → `200`, empty collection
- no qualifying inventory → `200`, zero totals

Do not make the PartDetail endpoint secretly load the initial MPS.

---

## 11. OpenAPI / generated TypeScript

C# DTOs are the source of truth.

After API/DTO work:

1. build backend so OpenAPI is regenerated
2. run the repository's `npm run generate:types`
3. use generated PartDetail types through the existing frontend API client

Never hand-edit `src/frontend/src/generated/api.ts`.

Commit the generated OpenAPI spec and generated TypeScript changes together according to repository convention.

---

## 12. Frontend interaction

Implement the accepted Stage 6 UI behavior using the current Stage 5 grid/styling conventions.

### Selection behavior

Parent-row click:

1. sets selected parent
2. collapses/focuses the MPS around that parent
3. opens Part Info beneath the focused row
4. starts lazy PartDetail load if backend cache cannot serve it

Provide a clear:

```text
Back to full grid
```

action that restores the normal MPS grid and clears/exits the detail focus as appropriate to the current UI structure.

Part Info is not week-dependent.

Do not make Due/Release/horizon/fiscal changes issue PartDetail requests.

### Part Info presentation

Show:

- Part Number
- Planner
- Mfg Lead Time (days)
- Safety Time (days)
- Part Status as `CODE — DESCRIPTION`
- Current Revision
- Description
- IOS Code
- Safety Stock
- Qty On Hand
- Qty Non-Net
- MOQ / Current Price

Common pricing case:

- one MOQ/price pair should remain compact

Exceptional pricing case:

- multiple price tiers should render clearly without changing the API contract

Missing informational values:

- blank or a simple `No Data Found`

Do not create warning noise for normal blank QAD fields.

### Required states

Implement:

- loading
- loaded
- missing part
- initial QAD error with Retry
- stale last-good warning

Preserve selected parent/focused context while a detail request is loading or retrying unless the user leaves the detail view.

Use current accessible light/dark theme, density, spacing, typography, and grid visual language. Do not copy obsolete inline prototype styling if Stage 5 has already established production CSS patterns.

---

## 13. Automated tests

Add focused automated coverage in the repository's existing test suites.

At minimum cover:

### Domain/application

- every accepted Part Status mapping
- unknown Part Status preserves raw code without failure
- blank/null informational fields allowed
- cache hit against same workspace/parent/snapshot identity
- successful MPS snapshot generation change makes old detail stale for next access
- failed workspace refresh does not invalidate compatible detail
- stale-last-good PartDetail returned when fresh query fails and old detail exists
- initial PartDetail QAD failure with no cache returns unavailable outcome
- part outside workspace parent scope rejected
- missing `pt_mstr` distinguished from database failure

### QAD integration

Use test seams/fixtures consistent with Stage 5; do not require production QAD for the normal automated test suite.

Cover normalization/business behavior for:

- positive nettable inventory → Qty On Hand
- positive non-nettable inventory → Qty Non-Net
- RA lot excluded
- zero inventory ignored
- negative inventory ignored
- no qualifying inventory → zero totals
- current price uses latest `pi_start <= today`
- future price is not selected early
- single price tier
- multiple price tiers ordered by MOQ
- no current price → empty collection

### API

- happy-path DTO shape
- workspace 404
- MPS-not-loaded 409
- out-of-scope part 404
- missing QAD part 404
- initial QAD failure 503 with non-leaking Problem Details
- stale last-good returns 200 + stale metadata
- OpenAPI contains PartDetail endpoint/schema

### Frontend

- parent row selection collapses/focuses grid and opens Part Info
- loading state
- fields render from generated API type
- status code + description render together
- blank/no-data rendering
- single MOQ/price rendering
- multiple price tiers rendering
- missing-part state
- error/retry state
- stale warning
- Back to full grid restores grid
- Due/Release and horizon/fiscal presentation changes do not trigger a PartDetail refetch

Keep tests deterministic. Do not use wall-clock `today` directly when a repository clock abstraction/test seam is available; use existing time abstraction or introduce the smallest appropriate seam consistent with architecture.

---

## 14. Live QAD validation

After automated implementation passes, validate against read-only QAD using representative parent parts where available.

Capture evidence for:

- ordinary manufactured parent
- blank/null informational attributes
- several Part Status codes
- nettable inventory
- non-net inventory
- zero/negative inventory ignored
- RMA lot excluded
- no inventory
- single MOQ/price
- multiple MOQ/price tiers
- latest effective price using `pi_start <= today`
- no current price if a representative case exists
- explicit/configured parent with little or no current MPS activity
- more than one supported site/domain where practical
- missing-part behavior using a safe invalid request rather than changing QAD data

Compare calculated inventory/pricing results with direct read-only SQL.

Do not modify QAD data to manufacture test cases.

Record the exact validation cases, direct-query evidence, discrepancies, and resolutions in a Stage 6 validation/closeout document.

---

## 15. Verification commands

Use the repository's current canonical commands. At minimum, after implementation:

### Backend

```powershell
cd C:\Dev\kst_v2\src\backend
dotnet format Kst.slnx --verify-no-changes
dotnet build Kst.slnx --nologo
dotnet test Kst.slnx --nologo
```

### OpenAPI / frontend

```powershell
cd C:\Dev\kst_v2\src\frontend
npm run generate:types
npm run lint
npm run typecheck
npm test
npm run build
```

### Tauri/Rust

```powershell
cd C:\Dev\kst_v2\src\tauri
cargo check
cargo build
```

### Refresh sidecar after backend changes

```powershell
cd C:\Dev\kst_v2
.\scripts\build-sidecar.ps1
```

Then run the development app and perform manual Stage 6 UI/QAD validation:

```powershell
cd C:\Dev\kst_v2\src\tauri
npx @tauri-apps/cli dev
```

Do not claim packaged-runtime verification unless it was actually run. A full installer rebuild is not automatically required merely to complete this feature unless the repository's current release/checklist policy requires it.

---

## 16. Documentation updates

As implementation facts become true, update repository durable memory.

At minimum maintain:

- Stage 6 Part Info contract
- Stage 6 implementation/progress or validation document
- `KST-v2-Master-Project-Checklist.md`
- `docs/status/CURRENT_PROJECT_STATUS.md` or its current canonical replacement
- API/OpenAPI documentation generated by the normal pipeline
- relevant data/source inventory if implementation discovers source metadata worth retaining

Do not mark live validation or owner acceptance complete before it actually happens.

---

## 17. Implementation sequence / checkpoints

Work in these checkpoints so failures are attributable.

### Stage 6D.0 — Repository preflight

- inspect current patterns
- reconcile exact files/interfaces/snapshot identity
- record implementation touchpoints
- run baseline tests before changes

### Stage 6D.1 — Domain/application contract

- PartDetail / PartPriceBreak normalized models
- status mapping
- application outcomes/service
- cache/freshness abstraction using existing snapshot generation
- unit tests

### Stage 6D.2 — QAD integration

- focused part-master retrieval
- focused inventory aggregation
- current price/tiers retrieval
- source record mapping
- cancellation/timeout/logging
- integration-unit tests using existing seams/fixtures

### Stage 6D.3 — API/OpenAPI

- workspace-scoped PartDetail endpoint
- DTOs
- Problem Details outcomes
- API integration tests
- regenerate OpenAPI and TypeScript types

### Stage 6D.4 — Frontend Part Info

- row selection state
- collapse/focus behavior
- lazy API call
- Part Info panel
- price-tier presentation
- loading/missing/error/stale states
- Back to full grid
- frontend tests

### Stage 6D.5 — Cache/refresh verification

Prove:

- same parent + same MPS snapshot reuses PartDetail
- Due/Release and horizon/fiscal changes do not requery PartDetail
- failed MPS refresh preserves usable detail
- successful MPS refresh causes fresh PartDetail on next access
- failed fresh detail query returns stale last-good when available

### Stage 6D.6 — Live QAD validation and closeout packet

- direct-query comparisons
- multi-site/domain checks where practical
- manual UI acceptance evidence
- complete verification commands
- update docs/checklist/status truthfully

Do not start Stage 7.

---

## 18. Required final report back to project owner

When Stage 6 implementation work is complete, report:

1. files changed
2. final model/API shape
3. final QAD query/source rules actually implemented
4. cache/snapshot behavior implemented
5. automated test results
6. live-QAD validation cases/results
7. any deviations from the accepted contract and why
8. remaining manual acceptance items
9. exact checklist/status documentation updated
10. commit hash if changes were committed

Do not mark Stage 6 complete until project-owner acceptance is received.

# KST v2 — Stage 6 Part Information Drill-Down Contract

**Stage:** 6 — Phase 3: Part Information Drill-Down  
**Contract status:** ACCEPTED  
**Accepted by project owner:** 2026-08-10  
**Implementation status:** Ready for Stage 6D implementation

## 1. Purpose

Stage 6 adds the first lazy-loaded detail drill-down from the accepted MPS dashboard.

Completion intent:

> Selecting an MPS parent part displays validated QAD part-master attributes, inventory summaries, and current MOQ/price information.

Stage 6 is intentionally parent-part scoped. It does not implement Work Orders, Kitting, BOM/component detail, shortages, PO coverage, sales-order demand, or Finished Goods lot/location detail.

## 2. Accepted UI behavior

The accepted interaction is:

```text
Full MPS grid
    ↓ select parent row
Grid collapses/focuses on selected parent
    ↓
Part Info opens beneath the focused MPS row
    ↓
Back to full grid restores the normal MPS view
```

Rules:

- Parent selection is transient UI state; it is not persisted as workspace configuration.
- Part Info is **part-scoped**, not week-scoped.
- Selecting a parent loads PartDetail lazily.
- Due Date / Release Date changes do not reload PartDetail.
- MPS horizon changes do not reload PartDetail.
- Fiscal display changes do not reload PartDetail.
- Theme/density/presentation changes do not reload PartDetail.
- A selected parent remains the context for the Part Info pane until selection is cleared, the user returns to the full grid, or workspace context changes.

## 3. Authoritative context

PartDetail is resolved from the current workspace and selected parent.

Authoritative context:

```text
Workspace
  → Site
  → QAD domain inferred using existing site/domain behavior
  → Parent Part
```

Customer code is not part of the Stage 6 lookup contract.

The frontend must not supply QAD domain directly.

## 4. Accepted PartDetail field set

The prototype field list was intentionally refined during Stage 6 discovery. The following is the accepted parent-level PartDetail field set.

| Normalized KST Field | Authoritative Source / Rule | Grain | Business Meaning / Transformation | Missing Rule |
|---|---|---|---|---|
| `partNumber` | `pt_mstr.pt_part` / selected MPS parent | Domain + Part | Parent-part identity | Missing `pt_mstr` row = missing-part state |
| `plannerCode` | `pt_mstr.pt_buyer` | Domain + Part | For manufactured parent parts, `pt_buyer` is the planner code | Blank/null allowed |
| `manufacturingLeadTimeDays` | `pt_mstr.pt_mfg_lead` | Domain + Part | Manufacturing lead time in days | Blank/null allowed; zero has no special missing semantics |
| `safetyTimeDays` | `pt_mstr.pt_sfty_time` | Domain + Part | Safety time in days | Blank/null allowed; zero has no special missing semantics |
| `partStatusCode` | `pt_mstr.pt_status` | Domain + Part | Preserve raw QAD part-status code | Blank/null allowed |
| `partStatusDescription` | Backend status mapping | Domain + Part | Human-readable meaning of `pt_status` | Unknown code keeps raw code and may have blank/unknown description |
| `currentRevision` | `pt_mstr.pt_rev` | Domain + Part | Current revision string | Blank/null allowed |
| `description` | `pt_mstr.pt_desc1` | Domain + Part | Current part description; same accepted description source as Stage 5 MPS | Blank/null allowed |
| `iosCode` | `pt_mstr.pt_warr_cd` | Domain + Part | IOS code, displayed raw | Blank/null allowed |
| `safetyStockQuantity` | `pt_mstr.pt_sfty_stk` | Domain + Part | Safety stock in part units | Blank/null allowed; zero is valid informational data |
| `quantityOnHand` | `ld_det` + `loc_mstr` + `is_mstr` | Domain + Site + Part | Positive, non-RMA, nettable inventory | No qualifying rows = `0` |
| `quantityNonNet` | `ld_det` + `loc_mstr` + `is_mstr` | Domain + Site + Part | Positive, non-RMA, non-nettable inventory | No qualifying rows = `0` |
| `priceBreaks[]` | `pi_mstr` + `pid_det` | Domain + Part + Price Tier | Current effective MOQ/price tier(s) | No current pricing = empty collection / UI `No Data Found` |

### Fields removed from Stage 6

The following prototype/checklist concepts are **not** part of the accepted Stage 6 contract:

- UOM
- Item Class
- Component Count
- WIP
- Part-level MPS schedule status
- `ptp_det` planning-parameter lookup
- planner fallback
- lead-time fallback

Later stages may introduce related concepts when their workflows require them.

## 5. Part Status mapping

The UI must show both the raw code and its description. The backend owns the mapping so the frontend does not duplicate QAD business semantics.

| Code | Description |
|---|---|
| `A` | AEMR |
| `B` | BYPASS |
| `C` | CURRENT |
| `E` | END OF LIFE |
| `F` | FORECAST |
| `H` | PURCHASING HOLD |
| `I` | INACTIVE PURCHASED PARTS |
| `M` | MFA |
| `N` | NPI |
| `O` | OBSOLETE |
| `P` | PROTO |
| `Q` | QUOTED PARTS |
| `U` | UNRELEASED |

Presentation may use a compact form such as:

```text
C — CURRENT
```

If an unrecognized status code is encountered, PartDetail must not fail. Preserve and display the raw code; do not invent a description.

## 6. Inventory business rule

Stage 6 uses a focused inventory summary for the exact selected site + parent part.

Source relationships:

```text
ld_det
  → loc_mstr on domain + site + location
  → is_mstr on domain + location-status classification
```

Only inventory rows with:

```text
ld_qty_oh > 0
```

are considered.

Zero and negative location balances are intentionally ignored as an accepted Stage 6 business rule because they are treated as transactional/data artifacts rather than usable positive inventory.

### RMA exclusion

A lot is treated as RMA when:

```text
ld_lot LIKE 'RA%'
```

RMA quantities are excluded from both Stage 6 display totals.

### Qty On Hand

```text
quantityOnHand =
  sum positive ld_qty_oh
  where lot is not RMA
  and location status is nettable
```

### Qty Non-Net

```text
quantityNonNet =
  sum positive ld_qty_oh
  where lot is not RMA
  and location status is non-nettable
```

The original investigative `EligibleParts` CTE is not part of Stage 6. Workspace/MPS parent resolution has already identified the parent to query.

## 7. Pricing / MOQ business rule

Pricing is informational Stage 6 data.

Current price-list rule:

> For the selected part and domain, choose the most recent `pi_mstr` record whose `pi_start <= today`.

No price-end/expiration rule is used in Stage 6.

Source relationship:

```text
selected domain + part
    ↓
pi_mstr
  pi_part_code = part
  pi_domain = domain
  pi_start <= today
  latest pi_start wins
    ↓
pid_det
  pid_domain = pi_domain
  pid_list_id = selected pi_list_id
```

Each applicable `pid_det` row provides:

- MOQ from `pid_qty`
- Price from `pid_amt`

Most parent parts are expected to have a single MOQ/price pair. Multiple MOQ/price tiers are supported as an exception and should be returned as a collection ordered by MOQ ascending for deterministic presentation.

No current price list is not an error. Return an empty price-break collection and allow the UI to show `No Data Found`.

## 8. Missing and partial data behavior

Stage 6 is informational and should show what QAD provides.

Accepted rules:

- If `pt_mstr` exists, Part Info loads even when individual fields are blank/null.
- Blank/null informational values may render blank or `No Data Found`.
- Ordinary missing field values do not require partial-data warnings.
- No qualifying inventory rows means `quantityOnHand = 0` and `quantityNonNet = 0`.
- No current pricing means `priceBreaks = []`.
- A missing `pt_mstr` record is a true missing-part state.
- A database/query failure is not the same as missing data and must surface through the error/stale-data behavior below.

## 9. Domain model

Stage 6 should use the smallest sufficient normalized model.

Conceptual model:

```text
PartDetail
────────────────────────────────
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

Child model:

```text
PartPriceBreak
────────────────────────
MinimumOrderQuantity
UnitPrice
```

Do not create speculative submodels such as `PartAttributes`, `PartPlanningParameters`, or `PartInventorySummary` unless current repository patterns require them for a concrete reason.

### Numeric types

Use SQL metadata/current repository conventions to choose final CLR types. Do not truncate numeric QAD values merely because they usually look integral. Quantity and price fields should preserve source precision.

## 10. SQL / integration versus application responsibility

### QAD integration owns

- parameterized SQL
- site/domain-bounded retrieval
- exact QAD table/field joins
- positive-inventory filtering
- RMA exclusion
- net/non-net aggregation
- current `pi_mstr` selection using `pi_start <= today`
- `pid_det` price-tier retrieval
- mapping QAD rows into integration/source records

### Application/domain owns

- orchestration from workspace + selected parent
- Part Status code-to-description normalization
- composing stable `PartDetail`
- cache/freshness behavior
- stale-last-good behavior
- missing-part and error outcomes
- stable semantics independent of QAD table names

### API owns

- HTTP route
- DTO mapping
- Problem Details mapping
- OpenAPI contract

### Frontend owns

- transient parent selection
- grid collapse/focus behavior
- loading/error/missing/stale presentation
- rendering values and price tiers
- `No Data Found` presentation

QAD-specific table/column names must not leak into frontend contracts.

## 11. QAD reader / source records

Implementation should follow the repository's current Stage 5 QAD integration pattern rather than introducing a new dependency shape.

Conceptual source records:

```text
QadPartMasterRecord
  PartNumber
  PlannerCode
  ManufacturingLeadTimeDays
  SafetyTimeDays
  PartStatusCode
  CurrentRevision
  Description
  IosCode
  SafetyStockQuantity
```

```text
QadPartInventoryRecord
  PartNumber
  QuantityOnHand
  QuantityNonNet
```

```text
QadPartPriceBreakRecord
  MinimumOrderQuantity
  UnitPrice
```

A focused QAD reader/service should retrieve only the selected part's Stage 6 data. It may use one or several SQL result sets/commands according to the current repository pattern; minimizing database round trips is useful, but not a contract requirement.

Reuse existing:

- `QadConnectionOptions`
- `Microsoft.Data.SqlClient` / Dapper conventions already established by Stage 5
- Windows Integrated authentication
- site → domain resolution
- cancellation-token propagation
- command-timeout/logging conventions
- read-only / `READ UNCOMMITTED` behavior

Do not introduce database writes.

## 12. Application service

Conceptual use case:

```text
GetPartDetail(workspaceId, partNumber)
```

Responsibilities:

1. Resolve workspace.
2. Obtain the current MPS snapshot/context.
3. Normalize requested parent part.
4. Verify the parent belongs to the current resolved workspace/MPS parent scope.
5. Resolve site/domain using existing QAD integration behavior.
6. Check PartDetail cache.
7. Lazy-load QAD when required.
8. Normalize status.
9. Compose `PartDetail`.
10. Save/serve cache according to snapshot freshness.
11. Return application-level missing/error/stale outcomes for API mapping.

PartDetail must not silently trigger the initial MPS load. The drill-down begins from an already-loaded MPS workspace.

## 13. Cache and refresh contract

### Data identity

```text
Site + Parent Part
```

### Cache / freshness identity

```text
Workspace + Parent Part + current MPS snapshot generation/identity
```

Use the existing Stage 5 snapshot identity/generation mechanism or its current repository equivalent. Do not create an unrelated TTL system.

Conceptual cache entry:

```text
PartDetailCacheEntry
  WorkspaceId
  ParentPart
  LoadedAgainstMpsSnapshotId
  PartDetail
  LoadedAtUtc
```

Behavior:

| Event | PartDetail behavior |
|---|---|
| MPS initially loads | No PartDetail queries |
| Select uncached parent | Lazy QAD load |
| Reopen same parent against same MPS snapshot | Reuse cached detail |
| Due/Release toggle | No reload |
| Horizon change | No reload |
| Fiscal display change | No reload |
| Workspace refresh fails | Existing MPS snapshot and compatible PartDetail cache remain usable |
| Workspace refresh succeeds | New MPS snapshot generation makes old PartDetail cache stale for next access |
| Next access after successful workspace refresh | Attempt fresh PartDetail load |
| Fresh PartDetail load succeeds | Replace cached detail |
| Fresh PartDetail load fails but older detail exists | Return older detail as stale with warning |
| Initial PartDetail load fails with no cached detail | Return unavailable/error outcome |

No persisted/offline PartDetail cache is required initially.

## 14. API contract

Accepted route:

```http
GET /api/v1/workspaces/{workspaceId}/part-detail?partNumber={partNumber}
```

The frontend does not provide domain, customer, Due/Release basis, horizon, or fiscal parameters.

Conceptual response:

```json
{
  "site": "SW",
  "partNumber": "ABC-123",
  "plannerCode": "JSMITH",
  "manufacturingLeadTimeDays": 10,
  "safetyTimeDays": 2,
  "partStatusCode": "C",
  "partStatusDescription": "CURRENT",
  "currentRevision": "B",
  "description": "WIDGET CONTROL ASSEMBLY",
  "iosCode": "1234",
  "safetyStockQuantity": 250,
  "quantityOnHand": 1325,
  "quantityNonNet": 75,
  "priceBreaks": [
    {
      "minimumOrderQuantity": 100,
      "unitPrice": 12.45
    }
  ],
  "loadedAtUtc": "2026-08-10T22:30:00Z",
  "isStale": false,
  "warning": null
}
```

C# API DTOs remain authoritative. OpenAPI and TypeScript types must be regenerated through the existing repository workflow; generated TypeScript must never be hand-edited.

## 15. HTTP/error semantics

| Situation | Result |
|---|---|
| Workspace does not exist | `404` Problem Details |
| Workspace MPS is not loaded | `409` Problem Details |
| Requested part is not in current workspace parent scope | `404` Problem Details |
| Parent is in scope but `pt_mstr` does not exist | `404` missing-part Problem Details |
| QAD failure and no cached detail exists | `503` generic user-facing Problem Details; technical detail logged server-side |
| QAD failure after a newer MPS snapshot but older PartDetail exists | `200` stale last-good PartDetail with `isStale = true` and warning |
| Individual QAD fields blank/null | `200`; blank / `No Data Found` presentation |
| No pricing | `200`; empty `priceBreaks` |
| No qualifying inventory | `200`; on-hand and non-net are `0` |

Do not leak connection strings, credentials, raw SQL exception detail, or internal database topology to the UI.

## 16. Frontend behavior contract

Part Info UI state machine:

```text
No selection
  ↓ parent row click
Selected / loading
  ├─→ loaded
  ├─→ missing part
  ├─→ error
  └─→ stale last-good
```

Required presentation:

- focused/collapsed selected MPS parent row
- Part Info panel beneath the MPS row
- clear `Back to full grid` action
- Part Number
- Planner
- Mfg Lead Time
- Safety Time
- Part Status code + description
- Current Revision
- Description
- IOS Code
- Safety Stock
- Qty On Hand
- Qty Non-Net
- current MOQ / Price
- multi-tier MOQ/price presentation when applicable
- loading state
- missing-part state
- QAD error/retry state
- stale-data warning when last-good detail is returned
- blank or `No Data Found` for ordinary missing values

The common pricing case should remain visually compact when only one MOQ/price tier exists. Multiple tiers should expand naturally without requiring a different API shape.

## 17. Explicit non-goals

Stage 6 must not implement:

- Work Order cards/details
- Kitting percentage
- WIP calculations
- BOM/component count
- BOM explosion
- component inventory/coverage
- shortages
- purchase-order coverage
- buyer comments
- sales-order demand
- Finished Goods location/lot/RMA detail
- MPS schedule-status rollups in Part Info
- direct database writes
- new generic part browser/search capability
- speculative shared services solely for future stages

## 18. Validation requirements

Before Stage 6 acceptance, validate where representative live data is available:

- ordinary manufactured parent
- blank/null informational attributes
- multiple QAD Part Status codes including code + description mapping
- unknown status code behavior in automated tests
- positive nettable inventory
- positive non-net inventory
- RMA lot exclusion
- zero inventory rows ignored
- negative inventory rows ignored
- no qualifying inventory → 0 / 0
- single MOQ/price pair
- multiple MOQ/price tiers
- latest `pi_start <= today` selected
- no current price
- configured parent with little/no MPS activity
- more than one supported site/domain
- missing `pt_mstr` case where feasible
- first-load QAD failure
- stale-last-good behavior after successful MPS refresh followed by detail-query failure
- cache reuse when reopening same parent
- no PartDetail requery for Due/Release or horizon/fiscal presentation changes

For calculated/aggregated values, compare KST results with direct read-only QAD queries.

## 19. Completion gate

Stage 6 is complete only when:

- parent selection/collapse/focus behavior works end-to-end
- all accepted PartDetail fields use authoritative QAD sources/rules
- Part Status code + description mapping is validated
- net/non-net inventory summaries are validated
- current MOQ/price selection and multi-tier behavior are validated
- PartDetail contract is typed through C# → OpenAPI → generated TypeScript
- lazy-load/cache behavior is verified
- loading, missing, error, and stale-last-good states work
- automated verification passes
- representative live-QAD comparisons pass
- project-owner acceptance is received

Completion statement:

> Selecting an MPS parent part displays validated QAD part-master attributes, inventory summaries, and current MOQ/price information.

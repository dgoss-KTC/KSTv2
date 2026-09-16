# FILE: docs/api/MPS.md

# Master Production Schedule (MPS)

## Status and Authority

**Implementation status:** Implemented / Accepted
**Documentation status:** Implemented, except the combined workspace-scope issue identified in `WORKSPACES.md`
**Verification baseline:** `main` at `3e2805d70e5797b37317857c54d1184feda8e12c`

**Mechanical contract authority:** `docs/openapi/Kst.Api.json`

Primary semantic evidence:

- `docs/implementation/KST_v2_STAGE_5A_MPS_DATA_INVENTORY.md`
- `docs/implementation/KST_v2_STAGE_5A_MPS_BACKEND_DATA_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_5A_SNAPSHOT_REFRESH_STRATEGY.md`
- `docs/implementation/KST_v2_STAGE_5A_MPS_API_SNAPSHOT_CONTRACT.md`
- `docs/implementation/STAGE_5B_9_REAL_DATA_VALIDATION.md`
- accepted Stage 5 closeout/current-state documentation

---

## Purpose

The MPS capability provides a workspace-scoped planning projection of qualifying QAD work-order supply facts.

It resolves the configured workspace into a parent-part population, retrieves the accepted MPS source facts, retains them in an in-memory workspace snapshot, and projects them into business-week buckets for scheduler review.

MPS is a planning view.

It is not a live Work Order population API, inventory-coverage calculation, or material-shortage calculation.

---

## Questions Answered

MPS answers questions such as:

- Which parent parts are currently resolved into this scheduler workspace?
- What qualifying MPS supply quantity falls into each visible planning week?
- What is the execution-state classification of a bucket?
- Does a bucket contain planned or explicitly scheduled work?
- Which historical unfinished supply should appear in Falldown?
- How does the current snapshot project by Due Date versus Release Date?
- When was the workspace MPS snapshot loaded successfully?
- Did the last refresh fail while an older good snapshot remained available?

---

# Contract Reference

```text
GET /api/v1/workspaces/{assignmentId}/mps
Operation: GetMpsDashboard

POST /api/v1/workspaces/{assignmentId}/mps/refresh
Operation: RefreshMpsDashboard
```

Current view parameters include:

```text
dateBasis
horizonWeeks
```

Exact request/response schemas: `docs/openapi/Kst.Api.json`

Current accepted horizon:

```text
1–72 weeks
```

---

# Request Scope and Identity

The request is scoped by:

```text
workspace assignment
date basis
display horizon
```

The workspace provides the configured site and scope inputs.

The QAD integration boundary resolves domain from site.

The client does not supply QAD domain directly.

---

# Response Grain

The top-level response represents one projected MPS view for one workspace snapshot.

It contains:

```text
Snapshot metadata
Date basis
Horizon
Parent schedules[]
    Buckets[]
```

A parent schedule is scoped to one resolved parent part.

A bucket represents one planning bucket for that parent under the current projection.

The current API deliberately does not expose the internal MPS work-order references as the primary public response grain.

---

# Source / Provenance

## Parent scope

Workspace configuration is resolved before MPS source facts are loaded.

Product-line-derived parents and explicitly configured parent parts are combined into the workspace's resolved parent population.

When only Explicit Parent Parts are configured, those parts define the requested workspace population without requiring Product Line scope.

See `WORKSPACES.md` for the authoritative workspace configuration semantics.

---

## MPS planning facts

The accepted production MPS source is direct, parameterized, read-only QAD SQL owned by:

```text
Kst.Integrations.Qad
```

The MPS does not depend on the legacy KST stored procedure as its production implementation.

Accepted source authority includes:

```text
mrp_det
    authoritative parent-level MPS planning facts

wo_mstr
    authoritative work-order header/status facts used to qualify/interpret those MPS facts

pt_mstr
    supporting parent description/filter information
```

The curated source metadata is maintained in:

```text
docs/data/qadpro2-data-map.md
```

---

## Accepted source qualification

The accepted MPS source strategy includes:

```text
mrp_dataset = 'wo_mstr'

mrp_type IN (
    'supply',
    'supplyf',
    'supplyp'
)

wo_status <> 'C'

wo_bom_code <> 'RMABOM'
```

The accepted Work Order join uses the required domain/site/part/WO-number/WO-ID identity.

`rps_mstr` is intentionally not the production MPS fact source for this capability.

No defensive deduplication should be inferred or introduced without evidence.

---

# Business-Week Semantics

KST MPS uses a Sunday-through-Saturday business week.

The visible week label is Monday.

Week bucketing is a C# business-rule responsibility rather than SQL presentation logic.

---

# Falldown

Falldown represents qualifying unfinished historical work that would otherwise fall before the current visible planning window.

Accepted behavior:

- Falldown is based on Due Date.
- Historical qualifying unfinished work has no arbitrary lower cutoff.
- It is retained in the source snapshot so Falldown can be reconstructed correctly.

Falldown is not simply “Week 0” and should not be treated as an ordinary dated weekly bucket.

---

# Due Date and Release Date

Both Due Date and Release Date source values are retained in the same MPS source snapshot.

Changing between Due and Release views reprojects the existing snapshot.

It does not, by itself, require a QAD reload.

This is a presentation/projection choice over retained source facts, not a request for a different authoritative source dataset.

---

# Horizon

The API currently supports a planning horizon from:

```text
1 through 72 weeks
```

The MPS snapshot is built with source coverage sufficient for the accepted maximum future horizon.

Changing the visible horizon within that supported range reprojects the current snapshot rather than automatically requerying QAD.

---

# Execution-State Semantics

The MPS backend returns semantic states rather than UI colors.

Accepted execution-state inputs are:

```text
A = Allocating
F = Frozen
R = Released
```

If a bucket contains two or more distinct A/F/R execution states, its execution status is:

```text
Mixed
```

Planned and explicitly scheduled states are represented independently:

```text
P = planned-work flag
e = explicitly-scheduled-work flag
```

P and e do not create Mixed by themselves.

Bucket quantity sums all qualifying included source rows regardless of presentation state.

Frontend presentation may represent these semantics visually, but color is not the backend business rule.

---

# Snapshot and Refresh Behavior

## First access

The MPS GET capability can auto-load the workspace MPS snapshot when required.

This differs from later detail capabilities that require MPS to have already loaded.

---

## Snapshot contents

The in-memory MPS snapshot retains enough source information to support:

- the resolved parent population;
- Due/Release reprojection;
- the maximum 72-week future horizon;
- historical unfinished Falldown;
- downstream freshness generation.

---

## Explicit refresh

```text
POST /api/v1/workspaces/{assignmentId}/mps/refresh
```

forces a new MPS source load for the workspace.

A refresh:

1. re-resolves workspace scope;
2. reloads the complete accepted MPS source dataset;
3. builds a replacement snapshot;
4. replaces the old snapshot atomically only after success.

---

## Failed refresh

A failed refresh preserves the last good snapshot.

The failure is not represented as an empty successful schedule.

This distinction allows the application to continue showing valid older planning data while reporting the failed refresh truthfully.

---

## Process lifetime

MPS snapshots are in-memory.

They are not an offline/persisted MPS store across application sessions.

A later application session must reestablish its business-data snapshot.

---

# Error and Data-Quality Behavior

Current endpoint behavior includes:

### 400

Invalid request parameters, including invalid date basis or horizon.

### 404

Unknown workspace.

### 503

QAD/source data required for MPS cannot be obtained.

Initial source failure is not represented as an empty successful MPS.

Exact Problem Details fields and current mechanical status codes remain authoritative in OpenAPI.

---

# Empty and Zero Semantics

An explicitly configured valid parent may legitimately appear with no current MPS rows.

That does not mean:

- the workspace is invalid;
- the parent configuration was deleted;
- the QAD source necessarily failed.

A valid empty schedule and a source-unavailable response are different states.

---

# Fiscal Calendar Boundary

Fiscal year, fiscal period, fiscal quarter, and 4-4-5 presentation logic are frontend-owned display behavior.

They are not QAD MPS source fields and are not part of the backend MPS business-data contract merely for display purposes.

The backend provides business-week planning semantics; the frontend maps those weeks into the configured fiscal display.

---

# Limitations

The MPS capability does not calculate:

- inventory coverage;
- component shortages;
- Work Order kitting percentage;
- PO coverage;
- Component MRP;
- substitute availability.

Those belong to later capabilities.

The MPS response is also not intended to be an exhaustive live `wo_mstr` population.

---

# Must Not Infer

Do not infer that:

- every live Work Order appears in MPS;
- the MPS parent/bucket population is identical to the Stage 7R live Work Order planning-window population;
- a bucket quantity represents available inventory;
- a bucket quantity means material is covered;
- P is an A/F/R execution state;
- e is an A/F/R execution state;
- P or e alone creates Mixed;
- changing Due/Release caused a QAD refresh;
- changing horizon within 72 weeks caused a QAD refresh;
- fiscal period/quarter values came from QAD;
- an explicit parent with zero current MPS rows is invalid;
- a successful `/api/v1/system/refresh` rebuilt this workspace's MPS snapshot.

---

# Related Capabilities

- `WORKSPACES.md` — configuration from which MPS scope is resolved
- `PARTS.md` — lazy parent detail tied to MPS snapshot generation
- `WORK_ORDERS.md` — live four-week Work Order population and material drilldown
- `API_CONVENTIONS.md` — snapshot/error conventions

---

# Usage Guidance

Use GET when the caller needs the current projected workspace MPS and automatic initial load is acceptable.

Use explicit MPS refresh when the caller intentionally wants to re-read authoritative MPS source data.

Do not use refresh merely because:

- the horizon changed;
- Due/Release changed;
- fiscal display changed;
- the frontend remounted.

---

# Verification / Evidence

Relevant durable evidence includes:

- `docs/implementation/KST_v2_STAGE_5A_MPS_DATA_INVENTORY.md`
- `docs/implementation/KST_v2_STAGE_5A_MPS_BACKEND_DATA_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_5A_SNAPSHOT_REFRESH_STRATEGY.md`
- `docs/implementation/KST_v2_STAGE_5A_MPS_API_SNAPSHOT_CONTRACT.md`
- `docs/implementation/STAGE_5B_9_REAL_DATA_VALIDATION.md`
- `src/backend/Kst.Api/Endpoints/MpsEndpoints.cs`
- `src/backend/Kst.Api/Dtos/MpsDtos.cs`
- `src/backend/Kst.Application/Mps/`
- `src/backend/Kst.Domain/Mps/`
- `src/backend/Kst.Integrations.Qad/Mps/`
- `src/backend/tests/Kst.Api.IntegrationTests/MpsEndpointTests.cs`
- `src/backend/tests/Kst.Application.Tests/Mps/`
- `src/backend/tests/Kst.Domain.Tests/Mps/`
- `src/backend/tests/Kst.Integrations.Qad.Tests/Mps/`
- `docs/data/qadpro2-data-map.md`
- `docs/openapi/Kst.Api.json`

---

# Open / Provisional Items

The meaning of combined Product Line + Explicit Parent Parts during final MPS scope resolution must be reconciled as documented in `WORKSPACES.md`.

No other provisional MPS contract item was identified in D1.

---

---

# FILE: docs/api/PARTS.md

# Part Detail

## Status and Authority

**Implementation status:** Implemented / Accepted
**Documentation status:** Implemented
**Verification baseline:** `main` at `3e2805d70e5797b37317857c54d1184feda8e12c`

**Mechanical contract authority:** `docs/openapi/Kst.Api.json`

Primary semantic evidence:

- `docs/implementation/KST_v2_STAGE_6_PART_INFO_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_6D_IMPLEMENTATION_PROGRESS.md`
- `docs/implementation/KST_v2_STAGE_6_CLOSEOUT.md`
- current Part Detail implementation/tests
- `docs/data/qadpro2-data-map.md`

---

## Purpose

Part Detail provides lazy-loaded informational data for a parent part already resolved into the current workspace MPS scope.

It combines accepted part-master, selected-site planning, inventory, and current-price information into one backend capability.

Part Detail is parent-part scoped.

It is not week-scoped.

---

## Questions Answered

Part Detail answers questions such as:

- What descriptive/master information does QAD currently have for this parent?
- Who is the planner?
- What are the selected-site manufacturing lead time and safety time?
- What is the QAD Part Status?
- What revision and description are recorded?
- What IOS code is recorded?
- What selected-site safety stock is recorded?
- What qualifying nettable and non-nettable inventory exists?
- What RMA inventory exists separately?
- What current MOQ/price tier information is available?
- Is the returned detail fresh or a compatible stale-last-good result?

---

# Contract Reference

```text
GET /api/v1/workspaces/{assignmentId}/part-detail?partNumber={partNumber}
Operation: GetPartDetail
```

Exact schema: `docs/openapi/Kst.Api.json`

Current response contains semantic groups for:

- site/part identity;
- planner/planning attributes;
- part status/revision/description/IOS;
- inventory;
- price breaks;
- load/freshness metadata.

Do not reproduce the complete DTO here; OpenAPI is the mechanical schema authority.

---

# Request Scope and Identity

The request identifies:

```text
workspace assignment
parent part number
```

The workspace determines site context.

Part Detail does not accept QAD domain from the client.

The requested parent must belong to the current resolved MPS parent scope for that workspace.

---

# MPS Prerequisite

Part Detail deliberately does **not** auto-load MPS.

Before Part Detail can be served, the workspace must already have a loaded MPS snapshot.

This establishes:

- workspace/site context;
- resolved parent scope;
- freshness generation.

If MPS has not loaded:

```text
HTTP 409
```

The consumer should establish MPS state rather than treating the result as a missing part.

---

# Response Grain

One response represents one parent part in one workspace/site context.

It is not:

- one MPS bucket;
- one Work Order;
- one BOM occurrence;
- one component requirement.

Part Detail remains the same part-level capability when Due/Release, horizon, fiscal display, density, or other presentation state changes.

---

# Source / Provenance

## Part master

Accepted Part Detail sources include:

```text
pt_mstr.pt_part
    Part Number

pt_mstr.pt_buyer
    Planner

pt_mstr.pt_status
    Part Status Code

pt_mstr.pt_rev
    Current Revision

pt_mstr.pt_desc1
    Description

pt_mstr.pt_warr_cd
    IOS Code
```

Part Status description is mapped by backend-owned KST logic from the QAD status code.

---

## Selected-site planning values

Accepted selected-site sources include:

```text
ptp_det.ptp_mfg_lead
    Manufacturing Lead Time

ptp_det.ptp_sfty_tme
    Safety Time

ptp_det.ptp_sfty_stk
    Safety Stock
```

The accepted relationship uses:

```text
domain
part
selected workspace site
```

The selected-site lookup does **not** use `pt_mstr.pt_site` as an authoritative join shortcut.

The planning relationship is effectively optional for Part Detail.

If no selected-site planning row exists, these selected-site values may be null.

There is no accepted fallback to the similarly named `pt_mstr` values for these Stage 6 fields.

---

# Inventory Semantics

Part Detail exposes separate inventory concepts.

The shared accepted inventory implementation uses:

```text
ld_det
    location-detail quantity

loc_mstr
    location → inventory-status relationship

is_mstr
    nettable/non-nettable classification
```

Inventory is bounded by the resolved:

```text
domain + site + part
```

Accepted common rules include:

- positive inventory only;
- RMA lots are excluded from the normal Net/Non-Net totals;
- location status determines Net versus Non-Net;
- no qualifying inventory produces numeric zero.

The current Part Detail contract also exposes RMA quantity separately.

Therefore:

```text
QuantityOnHand
    qualifying nettable, non-RMA quantity

QuantityNonNet
    qualifying non-nettable, non-RMA quantity

QuantityRmaOnHand
    separately reported RMA quantity
```

RMA quantity must not be silently added into normal usable/nettable inventory.

Detailed shared inventory semantics are documented in the future `INVENTORY_SEMANTICS.md` checkpoint.

---

# Pricing

Part Detail exposes current price-break information rather than one assumed universal price.

Accepted pricing uses QAD price-list information from:

```text
pi_mstr
pid_det
```

The accepted current-price selection uses the most recent applicable price-list start date not later than the current business date.

One or more MOQ/unit-price tiers may be returned.

A legitimate absence of current price rows is not automatically an error.

It may result in an empty price-break collection.

---

# Null and Zero Semantics

Part Detail intentionally distinguishes missing optional information from measured zero.

Examples:

```text
Selected-site planning row absent
    → selected-site planning values may be null

No qualifying Net inventory
    → numeric zero

No qualifying Non-Net inventory
    → numeric zero

No qualifying RMA inventory
    → numeric zero

No current price tier
    → empty price-break collection
```

Blank or null informational QAD values may be valid partial data.

They do not automatically make the entire Part Detail response unavailable.

---

# Part Status

QAD Part Status is informational part-master status.

The backend returns both:

```text
PartStatusCode
PartStatusDescription
```

where the description is KST/backend-owned mapping.

Part Status is not the same concept as MPS execution status.

---

# Cache and Freshness Behavior

Part Detail is lazy-loaded.

It participates in the workspace MPS snapshot-generation model for freshness.

The Part Detail facts themselves are read from their authoritative QAD sources; they are not stored inside the MPS schedule as Part Detail data.

The MPS snapshot ID acts as the established workspace freshness generation.

---

## Same MPS generation

A compatible cached Part Detail may be reused without another source query.

---

## Successful MPS refresh

A successful MPS refresh establishes a new snapshot generation.

The next Part Detail request must reevaluate its authoritative sources rather than blindly treating the prior generation as fresh.

---

## Reload failure after newer MPS generation

If a compatible last-good Part Detail exists and a fresh source load fails, the service may return that previous detail as:

```text
200 OK
isStale = true
warning != null
```

The stale response remains actual previously loaded data; it is not fabricated replacement data.

---

## Failed MPS refresh

A failed MPS refresh preserves the prior good MPS snapshot generation.

Because the generation did not change, a compatible Part Detail cached against that retained generation remains fresh for that generation.

A failed MPS refresh does not by itself force Part Detail to become stale.

---

## Process lifetime

The Part Detail cache is in-memory.

It is not an offline persistent Part Detail database.

---

# Error and Data-Quality Behavior

Current endpoint behavior includes:

### 400

Missing or blank `partNumber`.

### 404

Used for conditions including:

- unknown workspace;
- parent not in the workspace's current resolved MPS scope;
- no matching authoritative part-master record.

The endpoint's Problem Details distinguishes relevant conditions; consumers should not assume all 404s mean the same thing.

### 409

MPS data has not yet been loaded for the workspace.

### 503

Required QAD information cannot be obtained and no compatible valid cached fallback exists.

### 200 with partial values

Valid when optional informational source values legitimately do not exist.

### 200 stale

Valid when the capability serves a compatible last-known-good result under the accepted stale-fallback rules.

Consumers must inspect freshness metadata rather than equating `200` with “freshly queried.”

---

# Limitations

Part Detail does not provide:

- BOM structure;
- component detail for arbitrary components;
- Work Order material requirements;
- Component MRP;
- shortage classification;
- material coverage;
- PO coverage;
- inventory location/lot drilldown.

Those are separate or future capabilities.

---

# Must Not Infer

Do not infer that:

- Part Detail is a generic unrestricted QAD part-search API;
- any arbitrary part can be requested merely because it exists in `pt_mstr`;
- Part Detail is week-scoped;
- a Due/Release change requires Part Detail reload;
- a horizon change requires Part Detail reload;
- a null selected-site planning value means zero;
- a missing selected-site `ptp_det` row should fall back to `pt_mstr`;
- zero qualifying inventory means the source query failed;
- `QuantityRmaOnHand` is usable/nettable inventory;
- an empty price-break collection means unit price is zero;
- Part Status is an MPS execution state;
- `200 OK` guarantees fresh data;
- Part Detail automatically loads MPS;
- Part Detail proves material availability or shortage coverage.

---

# Related Capabilities

- `MPS.md` — establishes the parent scope and freshness generation required by Part Detail
- `WORKSPACES.md` — provides workspace/site configuration
- `INVENTORY_SEMANTICS.md` — shared inventory rules, to be added in D2
- `COMPONENTS.md` — later component/BOM detail semantics
- `API_CONVENTIONS.md` — null/zero/stale/error conventions

---

# Usage Guidance

Use Part Detail after the workspace MPS has loaded and the caller has selected a parent from that resolved planning context.

Do not call it as an application-wide part search.

When a response is successful:

1. consume the informational values according to their nullability;
2. preserve the distinction among Net, Non-Net, and RMA inventory;
3. inspect `isStale` and `warning`;
4. treat price breaks as tiers rather than assuming one price.

---

# Verification / Evidence

Relevant durable evidence includes:

- `docs/implementation/KST_v2_STAGE_6_PART_INFO_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_6D_IMPLEMENTATION_PROGRESS.md`
- `docs/implementation/KST_v2_STAGE_6_CLOSEOUT.md`
- `src/backend/Kst.Api/Endpoints/PartDetailEndpoints.cs`
- `src/backend/Kst.Api/Dtos/PartDetailDtos.cs`
- `src/backend/Kst.Application/PartDetail/`
- `src/backend/Kst.Domain/PartDetail/`
- `src/backend/Kst.Integrations.Qad/PartDetail/QadPartDetailReader.cs`
- `src/backend/Kst.Integrations.Qad/Inventory/QadPartInventoryReader.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/PartDetailEndpointTests.cs`
- `src/backend/tests/Kst.Application.Tests/PartDetail/PartDetailServiceTests.cs`
- `src/backend/tests/Kst.Integrations.Qad.Tests/PartDetail/QadPartDetailReaderTests.cs`
- `src/backend/tests/Kst.Integrations.Qad.Tests/Inventory/QadPartInventoryReaderTests.cs`
- `docs/data/qadpro2-data-map.md`
- `docs/openapi/Kst.Api.json`

Stage 6 accepted live validation covered multiple configured workspaces and representative parent parts, with direct read-only SQL comparison of representative inventory and pricing results.

---

# Open / Provisional Items

None identified for the implemented Part Detail capability at D1.

Shared inventory rules will receive a dedicated cross-capability semantic document in D2; that future document may replace duplicated explanatory inventory text here with a shorter cross-reference, but it must not change the accepted Stage 6 meanings without evidence.

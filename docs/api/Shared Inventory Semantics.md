# Shared Inventory Semantics

## Status and Authority

**Implementation status:** Implemented / Accepted
**Documentation status:** Implemented
**Verification baseline:** `main` at `3e2805d70e5797b37317857c54d1184feda8e12c`

This document describes a shared backend semantic capability.

It does **not** describe a standalone HTTP inventory endpoint.

Primary implementation:

```text
Kst.Domain.Inventory.PartInventorySummary

Kst.Application.Inventory.IPartInventoryReader

Kst.Integrations.Qad.Inventory.QadPartInventoryReader
```

Primary source metadata:

```text
docs/data/qadpro2-data-map.md
```

---

## Purpose

KST uses one shared Site + Part inventory interpretation for multiple backend capabilities.

The purpose of this abstraction is to ensure that Part Detail, BOM, Component Detail, and future consumers do not independently invent different meanings for:

- Net quantity on hand;
- Non-Net quantity on hand;
- RMA quantity on hand;
- zero qualifying inventory.

The shared inventory reader represents current physical QAD inventory by accepted status classification.

It does not calculate material coverage or shortage.

---

## Current Consumers

The shared inventory semantics are currently used by:

- Part Detail;
- BOM;
- Component Detail.

The underlying shared result also contains RMA quantity, although not every public API exposes that quantity.

Current public exposure differs by capability:

```text
Part Detail
    Net
    Non-Net
    RMA

BOM
    Net
    Non-Net

Component Detail
    Net
    Non-Net
```

A consumer-specific API omitting RMA does not change the underlying shared classification rule.

---

# Grain

The shared inventory grain is:

```text
Site + Part
```

The reader returns one complete inventory summary for every requested normalized part.

It does not return:

- one row per location;
- one row per lot;
- one row per inventory status;
- one row per BOM occurrence.

Location and status detail is aggregated into the accepted semantic quantities.

---

# Authoritative Source

The current implementation uses:

```text
ld_det
    location-level inventory quantity and lot

loc_mstr
    location → inventory-status relationship

is_mstr
    inventory-status nettable flag
```

The accepted join path is:

```text
ld_det
    ld_domain
    ld_site
    ld_loc

        ↓

loc_mstr
    loc_domain
    loc_site
    loc_loc
    loc_status

        ↓

is_mstr
    is_domain
    is_status
    is_nettable
```

`in_mstr` is not used as the authoritative shared QOH calculation.

---

# Positive Inventory Only

Only inventory with:

```text
ld_qty_oh > 0
```

participates in the current shared totals.

Zero and negative location quantities do not reduce or offset the positive inventory summarized by this capability.

This is the accepted Stage 6/Stage 8 rule.

Do not change this behavior merely because another QAD inventory field or report uses a different netting model.

---

# RMA Identification

RMA inventory is identified from the accepted lot convention:

```text
ld_lot LIKE 'RA%'
```

RMA quantities are separated from normal Net/Non-Net inventory.

Conceptually:

```text
RMA
    positive quantity
    lot begins RA%

Normal inventory
    positive quantity
    lot does not begin RA%
```

RMA inventory must not be included in ordinary Net or Non-Net totals.

---

# Net Quantity On Hand

Net quantity represents positive, non-RMA inventory whose location status is nettable:

```text
ld_qty_oh > 0
AND ld_lot NOT LIKE 'RA%'
AND is_nettable = true
```

This is the accepted usable/nettable classification exposed by the Stage 6/8 shared inventory capability.

It does not by itself mean material is available to a particular Work Order.

---

# Non-Net Quantity On Hand

Non-Net quantity represents positive, non-RMA inventory whose location status is non-nettable:

```text
ld_qty_oh > 0
AND ld_lot NOT LIKE 'RA%'
AND is_nettable = false
```

Non-Net quantity is reported separately rather than silently added into Net QOH.

---

# RMA Quantity On Hand

The shared internal result also retains:

```text
RmaQuantityOnHand
```

representing positive inventory associated with the accepted `RA%` lot convention.

Part Detail currently exposes this quantity.

BOM and Component Detail deliberately do not expose it.

---

# Zero Semantics

The inventory reader guarantees a summary for every requested part.

When no qualifying inventory exists for a requested part:

```text
Net = 0
Non-Net = 0
RMA = 0
```

This is a valid measured result.

It is not represented as null.

It is not represented as a missing inventory summary.

---

# Reader Completeness Contract

The shared reader contract is intentionally strict.

For every requested normalized part, callers expect exactly one inventory summary.

Therefore:

```text
No qualifying inventory
    → one summary containing numeric zeroes

Missing requested summary
    → integration/composition failure

Duplicate summary for one requested part
    → integration/composition failure
```

Callers must not silently convert a missing result into zero.

The reader itself is responsible for providing authoritative zero-filled results.

---

# Part Normalization and Batching

Requested part numbers are normalized before querying.

Current behavior includes:

- trim;
- reject blanks;
- case-insensitive deduplication.

The reader supports batched multi-part access.

Batching is an implementation/performance mechanism and does not change the Site + Part result grain.

---

# Relationship to BOM Occurrences

Inventory is Part-scoped, not BOM-occurrence-scoped.

If one component appears more than once in a BOM:

```text
Occurrence A ─┐
              ├── same Site + Part inventory summary
Occurrence B ─┘
```

The structural occurrences remain separate.

The inventory quantity is not divided among those occurrences merely because the BOM contains duplicates.

---

# Failure Semantics

Database/query failures are not converted into zero inventory.

A failed source read propagates through the consuming capability's normal failure/stale-data behavior.

This distinction is critical:

```text
Successful read + no qualifying inventory
    = zero

Failed inventory read
    ≠ zero
```

Depending on the consuming capability, a failed read may result in:

- a stale compatible last-known-good response;
- `503 Service Unavailable`.

See the corresponding capability document.

---

# What This Capability Does Not Calculate

The current shared inventory abstraction does not calculate:

- material requirements;
- Work Order allocation;
- shortage quantity;
- projected QOH;
- PO coverage;
- incoming supply;
- inspection availability;
- transit availability;
- expiration risk;
- MRB;
- NCM Inspection;
- future MRP availability.

Those require additional business semantics.

Some are expected to be added or composed during later material-analysis stages, but they are not part of the current Stage 6/8 inventory contract.

---

# Must Not Infer

Do not infer that:

- Net QOH is automatically available to a specific Work Order;
- Non-Net inventory is usable merely because quantity exists;
- RMA inventory is ordinary usable inventory;
- zero inventory means a source read failed;
- missing inventory data should be converted to zero by the caller;
- repeated BOM occurrences each own a separate copy of the inventory quantity;
- Net + Non-Net + RMA constitutes a shortage calculation;
- current Net QOH includes incoming PO supply;
- this capability performs allocation across Work Orders;
- this capability identifies expiring, transit, inspection, MRB, or other future Stage 9 inventory-position categories.

---

# Related Capabilities

- `PARTS.md` — exposes Net, Non-Net, and RMA inventory for a selected MPS parent
- `COMPONENTS.md` — BOM and Component Detail expose Net/Non-Net
- `WORK_ORDERS.md` — does not currently use inventory for Kitting or variance
- `IMMEDIATE_MATERIAL_ANALYSIS.md` — future/Stage 9 semantics will compose and extend inventory information
- `API_CONVENTIONS.md` — zero/null/unavailable conventions

---

# Verification / Evidence

Relevant implementation/evidence includes:

- `src/backend/Kst.Domain/Inventory/PartInventorySummary.cs`
- `src/backend/Kst.Application/Inventory/IPartInventoryReader.cs`
- `src/backend/Kst.Application/Inventory/DelegatePartInventoryReader.cs`
- `src/backend/Kst.Integrations.Qad/Inventory/QadPartInventoryReader.cs`
- `src/backend/tests/Kst.Integrations.Qad.Tests/Inventory/QadPartInventoryReaderTests.cs`
- Part Detail inventory tests
- BOM service/integration tests
- Component Detail service/integration tests
- `docs/implementation/KST_v2_STAGE_8_CLOSEOUT.md`
- `docs/data/qadpro2-data-map.md`

---

# Open / Provisional Items

The current Stage 6/8 Net/Non-Net/RMA semantics are implemented and accepted.

Later material-analysis work may introduce additional inventory-position categories and calculations.

Those future categories must be documented as extensions or separate semantics rather than silently redefining the established Net/Non-Net/RMA meanings.

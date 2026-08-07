# KST v2 — Stage 5A MPS Backend Data Contract

**Status:** Draft for owner review  
**Stage:** 5A — Data Inventory and Data Strategy  
**Capability:** Master Production Schedule  
**Depends on:** Accepted MPS Data Inventory and accepted direct QAD-adapter source-query strategy

---

## 1. Purpose

This document defines the backend-facing contract for the initial KST v2 Master Production Schedule.

It separates four concerns:

1. the QAD adapter query result,
2. normalized MPS source rows,
3. parent/week bucket aggregation,
4. API/snapshot data presented to the frontend.

Fiscal year, fiscal period, fiscal quarter, and fiscal-week display metadata are intentionally excluded. Those are frontend-only planning/display concepts.

---

## 2. Accepted source boundary

The initial MPS does **not** use a KST-owned stored procedure or table-valued function.

`Kst.Integrations.Qad` executes a direct, parameterized SQL Server 2016-compatible query against QADPRO2 using:

- workspace domain,
- workspace site,
- the already-resolved workspace parent-part list.

The MPS query does not resolve product-line/range scope itself. Workspace resolution happens before the MPS source read.

### 2.1 Required source qualification

A qualifying MPS source fact must satisfy:

```text
mrp_det.mrp_domain = requested domain
mrp_det.mrp_site   = requested site
mrp_det.mrp_part   = one of the resolved workspace parent parts
mrp_det.mrp_dataset = 'wo_mstr'
mrp_det.mrp_type IN ('supply', 'supplyf', 'supplyp')
wo_mstr.wo_status <> 'C'
```

### 2.2 Safe MRP-to-work-order association

```text
mrp_det.mrp_domain = wo_mstr.wo_domain
mrp_det.mrp_site   = wo_mstr.wo_site
mrp_det.mrp_part   = wo_mstr.wo_part
mrp_det.mrp_nbr    = wo_mstr.wo_nbr
mrp_det.mrp_line   = wo_mstr.wo_lot
```

`pt_mstr` joins by domain + part only to supply approved informational metadata such as `pt_desc1`.

### 2.3 Why `rps_mstr` is excluded

`SUPPLYF / rps_mstr` rows are legitimate repetitive-schedule intent, but they represent pre-work-order state. After MRP is run, the schedule is reflected as `wo_mstr`-backed MRP supply.

Initial KST behavior therefore intentionally consumes only `mrp_dataset = 'wo_mstr'`.

Operational consequence:

> After changing a repetitive schedule, the scheduler should run MRP for the affected parts before expecting KST to reflect the change. Overnight MRP provides eventual reconciliation if manual MRP is not run.

---

## 3. Adapter query shape

The query should remain row-oriented and parameterized. It must not pivot weeks, aggregate weekly quantities, or apply defensive deduplication.

Conceptual SQL shape:

```sql
WITH ScopeParts (ParentPart) AS
(
    SELECT ParentPart
    FROM
    (
        VALUES
            (@Part0),
            (@Part1),
            (@Part2)
            -- generated parameter rows
    ) AS Parts (ParentPart)
)
SELECT
    mrp.mrp_domain   AS Domain,
    UPPER(mrp.mrp_site) AS Site,
    mrp.mrp_part     AS ParentPart,
    pt.pt_desc1      AS Description,
    mrp.mrp_due_date AS DueDate,
    mrp.mrp_rel_date AS ReleaseDate,
    mrp.mrp_qty      AS Quantity,
    mrp.mrp_type     AS MrpType,
    mrp.mrp_line     AS WorkOrderId,
    wo.wo_status     AS WorkOrderStatus
FROM ScopeParts AS scope
INNER JOIN qadpro2.dbo.mrp_det AS mrp
    ON mrp.mrp_part = scope.ParentPart
INNER JOIN qadpro2.dbo.pt_mstr AS pt
    ON pt.pt_part = mrp.mrp_part
    AND pt.pt_domain = mrp.mrp_domain
INNER JOIN qadpro2.dbo.wo_mstr AS wo
    ON mrp.mrp_dataset = 'wo_mstr'
    AND mrp.mrp_nbr = wo.wo_nbr
    AND mrp.mrp_line = wo.wo_lot
    AND mrp.mrp_domain = wo.wo_domain
    AND mrp.mrp_site = wo.wo_site
    AND mrp.mrp_part = wo.wo_part
WHERE
    mrp.mrp_domain = @Domain
    AND mrp.mrp_site = @Site
    AND mrp.mrp_type IN ('supply', 'supplyf', 'supplyp')
    AND wo.wo_status <> 'C'
ORDER BY
    mrp.mrp_part,
    mrp.mrp_due_date,
    mrp.mrp_line;
```

The exact generated scope-list syntax is an adapter implementation detail. All part values must be parameters; do not concatenate raw part numbers into SQL text. If a workspace contains more parts than are practical in one parameterized statement, the adapter should batch/chunk the resolved scope and merge the row results before normalization.

---

## 4. `MpsSourceRow`

`MpsSourceRow` is the normalized application-facing representation produced by the QAD adapter. QAD-specific raw strings may exist in a private integration DTO, but application code should consume typed semantics.

Conceptual contract:

```text
MpsSourceRow
- Domain: string
- Site: string
- ParentPart: string
- Description: string?
- DueDate: DateOnly
- ReleaseDate: DateOnly?
- Quantity: decimal
- SupplyType: MpsSupplyType
- WorkOrderId: string
- WorkOrderState: MpsWorkOrderState
```

### 4.1 `MpsSupplyType`

```text
MpsSupplyType
- Supply    // QAD SUPPLY
- SupplyF   // QAD SUPPLYF
- SupplyP   // QAD SUPPLYP
```

Do not derive display labels or colors from this enum in the integration layer.

### 4.2 `MpsWorkOrderState`

```text
MpsWorkOrderState
- Allocating            // A
- Frozen                // F
- Released              // R
- Planned               // P
- ExplicitlyScheduled   // e
- Unknown               // defensive normalization for unexpected non-C values
```

`C` is excluded by SQL and should not normally reach `MpsSourceRow`.

An unexpected non-C raw status should not crash a refresh. Normalize it to `Unknown`, preserve enough diagnostic context for logging, and do not invent an execution color/state for it.

---

## 5. Source-row grain

One `MpsSourceRow` represents one qualifying `wo_mstr`-backed MRP supply fact after safe WO association and before weekly/Falldown aggregation.

The accepted source grain preserves:

- domain,
- site,
- parent part,
- due date,
- release date,
- quantity,
- MRP supply type,
- work-order identity,
- work-order state.

Representative KTC/SW testing found no duplicate rows at the tested grouping of domain + site + part + WO ID + due date + release date + MRP type.

Therefore the initial query must **not** add `DISTINCT`, `GROUP BY`, or SQL-side deduplication. If later evidence shows duplicate source facts, investigate the source behavior before changing the contract.

---

## 6. Date basis and week semantics

The MPS source retains both dates:

```text
Due Date     -> mrp_det.mrp_due_date
Release Date -> mrp_det.mrp_rel_date
```

The MPS supports a schedule date-basis concept:

```text
MpsDateBasis
- DueDate
- ReleaseDate
```

Final UI behavior for switching the basis remains a Stage 5B validation item. The backend should retain both source dates so the application can rebuild buckets from the current snapshot without re-querying QAD solely to change the date basis.

Business weeks are Sunday through Saturday. Monday is the visible MPS week label/anchor.

The legacy Falldown definition is unfinished work whose due date is before the current business week. Do not silently redefine Falldown during Release Date mode; Stage 5B must verify that behavior explicitly.

---

## 7. `MpsBucket`

`MpsBucket` represents the normalized business state for a parent-part schedule cell after C# aggregation.

Conceptual contract:

```text
MpsBucket
- Kind: MpsBucketKind
- WeekLabel: DateOnly?          // Monday label for weekly buckets; null for Falldown
- Quantity: decimal
- ExecutionStatus: MpsExecutionStatus
- ContainsPlannedWork: bool
- ContainsExplicitlyScheduledWork: bool
- WorkOrders: IReadOnlyList<MpsWorkOrderRef>
```

### 7.1 `MpsBucketKind`

```text
MpsBucketKind
- Falldown
- Weekly
```

Falldown is a first-class bucket rather than a fabricated historical week date.

### 7.2 `MpsExecutionStatus`

```text
MpsExecutionStatus
- None
- Allocating
- Frozen
- Released
- Mixed
```

The execution status is derived only from distinct contributing A/F/R states:

```text
no A/F/R       -> None
A only         -> Allocating
F only         -> Frozen
R only         -> Released
2+ of A/F/R    -> Mixed
```

`P` and `e` never create `Mixed` by themselves.

### 7.3 Independent planned / scheduled flags

```text
any P -> ContainsPlannedWork = true
any e -> ContainsExplicitlyScheduledWork = true
```

Examples:

| Contributing WO states | Execution | Planned | Explicitly scheduled |
|---|---|---:|---:|
| P | None | Yes | No |
| e | None | No | Yes |
| P + e | None | Yes | Yes |
| R + P | Released | Yes | No |
| F + e | Frozen | No | Yes |
| A + F + P | Mixed | Yes | No |
| A + F + P + e | Mixed | Yes | Yes |

### 7.4 Bucket quantity

```text
Bucket Quantity = SUM(Quantity of all included MpsSourceRows in the bucket)
```

Quantity aggregation is independent of visual/status treatment.

---

## 8. `MpsWorkOrderRef`

The bucket may retain a minimal internal reference for each distinct contributing work order:

```text
MpsWorkOrderRef
- WorkOrderId: string
- State: MpsWorkOrderState
```

This is sufficient to explain how the bucket state was derived and provides a stable handoff point for later drill-down work. Full work-order details remain deferred.

The initial public API does not have to expose these references unless the current UI needs them; they may remain application/snapshot internals.

---

## 9. `MpsPartSchedule`

The parent-level schedule shape should avoid repeating identity metadata in every bucket:

```text
MpsPartSchedule
- ParentPart: string
- Description: string?
- Buckets: IReadOnlyList<MpsBucket>
```

The surrounding view/snapshot context owns:

- site,
- workspace identity,
- snapshot identity/time,
- selected date basis,
- horizon.

---

## 10. Snapshot implications

The initial MPS snapshot should retain enough normalized source data to:

- render the current bucket view,
- rebuild buckets after a Due Date / Release Date basis change without re-querying QAD,
- explain A/F/R/Mixed/P/e bucket semantics,
- support stale/current snapshot state.

It should not preload later-stage data such as:

- full WO detail,
- BOM/components,
- kitting/allocation detail,
- inventory detail,
- shortages,
- purchase orders,
- planner/buyer data,
- WO remarks.

---

## 11. Backend / frontend boundary

Backend/application owns:

- QAD querying,
- source normalization,
- business-week bucketing,
- Falldown,
- quantity aggregation,
- A/F/R/Mixed classification,
- P/e semantic flags,
- snapshot lifecycle.

Frontend owns:

- box-fill presentation,
- planned-work font treatment,
- explicitly-scheduled non-color marker,
- fiscal year/week/period/quarter planning metadata,
- fiscal period/quarter header bands.

The backend must never return CSS colors or fiscal display fields as part of the MPS domain contract.

---

## 12. Stage 5B verification gates

Before the contract is considered implementation-verified, Stage 5B should confirm:

1. SQL Server 2016 parameterized part-list batching at realistic workspace sizes.
2. Due Date and Release Date views against representative production data.
3. Falldown behavior at Sunday/Saturday boundaries.
4. Unknown WO-status handling without refresh failure.
5. Multiple WOs in one parent/week produce correct quantity and Mixed/P/e semantics.
6. `rps_mstr` schedule changes appear after manual MRP / QADPRO2 sync as expected.
7. No unexpected duplicate source rows appear in additional representative sites.


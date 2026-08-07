# KST v2 — Stage 5A MPS Snapshot / API Contract Candidate

**Status:** Accepted Stage 5A candidate  
**Stage:** 5A — Data Inventory and Data Strategy  
**Purpose:** Give Stage 5B a concrete backend-to-frontend shape without making database rows equal API DTOs.

---

## 1. Contract layers

```text
QAD SQL result
    ↓
QAD integration record
    ↓
MpsSourceRow
    ↓
Workspace MPS snapshot
    ↓
MPS view/bucket projection
    ↓
API DTO
    ↓
Frontend
```

The frontend does not receive raw QAD records.

---

## 2. Snapshot metadata

Conceptual snapshot metadata:

```text
MpsSnapshotMetadata
- SnapshotId: string
- CreatedAtUtc: DateTimeOffset
- LastSuccessfulRefreshAtUtc: DateTimeOffset
- Status: SnapshotStatus
- WorkspaceId: string
- Site: string
- ResolvedParentPartCount: int
- SourceRowCount: int
```

`SnapshotStatus` should reuse the existing domain/application snapshot concept rather than introduce a duplicate MPS-only lifecycle enum.

A refresh attempt that fails does not create a replacement snapshot; therefore `LastSuccessfulRefreshAtUtc` remains the time of the retained good snapshot.

---

## 3. MPS dashboard response candidate

Conceptual response:

```text
MpsDashboardResponse
- Snapshot: MpsSnapshotMetadata
- DateBasis: MpsDateBasis
- HorizonWeeks: int
- Parts: IReadOnlyList<MpsPartScheduleDto>
```

```text
MpsPartScheduleDto
- ParentPart: string
- Description: string?
- Buckets: IReadOnlyList<MpsBucketDto>
```

```text
MpsBucketDto
- Kind: MpsBucketKind
- WeekLabel: DateOnly?
- Quantity: decimal
- ExecutionStatus: MpsExecutionStatus
- ContainsPlannedWork: bool
- ContainsExplicitlyScheduledWork: bool
```

The initial API does **not** need to expose the internal `MpsWorkOrderRef` collection until a drill-down capability requires it. The snapshot/application layer may retain those references internally.

---

## 4. View parameters

The MPS view supports:

```text
DateBasis: DueDate | ReleaseDate
HorizonWeeks: supported range up to 72
```

Changing these values should project/rebucket from the current in-memory source snapshot when coverage is sufficient. It must not force a QAD query solely because the display basis or visible horizon changed.

Falldown remains due-date based regardless of visible DateBasis.

---

## 5. Fiscal metadata exclusion

Do not add these to the backend MPS response solely for display:

```text
FiscalYear
FiscalWeek
FiscalPeriod
FiscalQuarter
FiscalPeriodSpan
```

The frontend derives them from the accepted fiscal-calendar settings/service.

---

## 6. Initial load / unavailable behavior

When no usable MPS snapshot exists and the QAD load fails, the API should return an appropriate failure/Problem Details response rather than a successful empty MPS payload.

The frontend presents:

> **Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.**

A Retry action initiates a new load attempt.

---

## 7. Refresh behavior

Refresh is an explicit operation against the workspace MPS snapshot. Stage 5B may choose the exact REST shape consistent with existing API conventions, for example a refresh command endpoint plus a dashboard read endpoint.

Required semantics are fixed:

- one refresh per workspace at a time,
- old snapshot remains readable while refresh runs,
- success atomically publishes the replacement snapshot,
- failure leaves the old snapshot untouched,
- frontend can show refresh-in-progress/failure state without losing current data.

Transient `IsRefreshing` state may be represented by the refresh operation/view model rather than persisted inside an immutable completed snapshot.

---

## 8. Empty schedule behavior

A successful QAD query with no MPS facts is different from database failure.

- The response is successful.
- Explicitly configured workspace parent parts remain in `Parts` with empty/zero buckets as appropriate.
- The UI can show a legitimate empty/no-schedule state.

---

## 9. Error-detail boundary

API/UI errors must not expose:

- connection strings,
- SQL text,
- server credentials,
- raw database stack traces.

Backend logs retain diagnostic detail subject to the database-access logging strategy.

---

## 10. Stage 5A disposition

The snapshot/API candidate is sufficiently defined for Stage 5B implementation planning. Exact route names and DTO class names should follow existing repository/API conventions when the implementation agent inspects the codebase.

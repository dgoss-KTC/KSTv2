# KST v2 — Stage 5A Snapshot and Refresh Strategy

**Status:** Accepted  
**Stage:** 5A — Data Inventory and Data Strategy  
**Initial capability:** MPS dashboard workspace snapshot

---

## 1. Purpose

Define the initial workspace snapshot lifecycle, refresh semantics, failure behavior, local-view transformations, and cache boundary before Stage 5B implementation.

---

## 2. Initial workspace load

When a workspace opens:

1. Open the workspace shell immediately.
2. Resolve the current workspace parent-part scope.
3. Begin the MPS QAD query automatically.
4. Show an MPS loading state while the snapshot is built.
5. Build the normalized source rows and MPS buckets.
6. Publish the completed snapshot atomically.

Explicitly configured parent parts remain part of the workspace even if they have no current MPS source rows; they may display a blank schedule.

---

## 3. Initial snapshot contents

Include:

- workspace identity,
- resolved parent-part scope,
- snapshot ID,
- snapshot creation timestamp,
- last successful refresh timestamp,
- source/refresh status,
- normalized MPS source rows covering all historical Falldown requirements and the maximum 72-week forward horizon,
- both Due Date and Release Date values,
- parent part and `pt_desc1`,
- minimal WO identity/status references required for MPS semantics,
- normalized MPS part schedules and buckets.

Do not preload full WO cards, BOM/components, kitting, inventory detail, shortages, purchase orders, buyer/planner detail, or other later-phase data.

---

## 4. Retrieval horizon

Future schedule retrieval must cover the maximum supported 72-week horizon.

Historical retrieval has **no lower date cutoff** for qualifying unfinished work because Falldown includes all non-closed qualifying work orders whose due date is before the current business week.

The source read retains both Due Date and Release Date so either schedule view can be built locally from the same snapshot.

---

## 5. Local view changes

These actions do **not** require a QAD re-query when the current snapshot already covers the requested view:

- Due Date ↔ Release Date toggle,
- horizon changes up to 72 weeks,
- fiscal period/quarter/year display calculations,
- tab switching.

Fiscal metadata is frontend-owned and does not participate in snapshot refresh.

---

## 6. Manual refresh

Refresh means:

1. re-resolve workspace parent-part scope,
2. re-query the accepted MPS QAD source,
3. rebuild normalized source rows and buckets,
4. construct a complete replacement snapshot,
5. atomically replace the current snapshot only after successful completion.

The prior good snapshot remains visible while refresh runs.

The UI displays the last successful refresh time.

Only one refresh for a workspace should run at a time. Additional refresh actions while one is active should not create concurrent QAD reads.

---

## 7. Failure behavior

### Initial load with no usable snapshot

If the database query fails, do not represent the result as an empty schedule. Keep the MPS in an unavailable/error state and present:

> **Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.**

Provide a Retry path.

### Refresh failure with a prior snapshot

- Keep the previous snapshot visible.
- Do not partially replace it.
- Preserve the last successful refresh timestamp.
- Surface a refresh failure notification/status.
- Allow the user to retry.

---

## 8. Persistence and background behavior

For the initial implementation:

- snapshots are in-memory session state only,
- application startup restores configuration, not stale MPS data,
- no automatic background refresh is required,
- inactive workspaces do not refresh automatically.

Persistent/offline snapshots may be reconsidered only if an operational need is demonstrated later.

---

## 9. Source freshness dependency

`SUPPLYF / rps_mstr` is intentionally excluded from the initial MPS because it is pre-MRP repetitive-schedule intent. Schedulers are expected to run MRP for affected parts after schedule changes. Overnight MRP provides eventual reconciliation.

A schedule change that has not yet been exploded by MRP may therefore not appear in KST immediately; this is a QAD/MRP freshness dependency, not a KST cache rule.

---

## 10. RMA exclusion

Work orders with `wo_bom_code = 'RMABOM'` are excluded at the SQL source boundary so RMA demand does not influence the production MPS.

---

## 11. Stage 5B acceptance tests

Stage 5B should verify at minimum:

1. Workspace shell opens while MPS loads.
2. Explicit parent with no MPS activity remains visible.
3. Successful initial load publishes one coherent snapshot.
4. Refresh preserves old data until replacement succeeds.
5. Failed refresh leaves old data intact.
6. Initial DB failure shows the approved unavailable message and Retry.
7. Last-successful refresh timestamp changes only after success.
8. Due/Release toggle does not query QAD.
9. Horizon changes ≤72 weeks do not query QAD.
10. Fiscal-display changes do not query QAD.
11. A historical non-closed WO remains in Falldown regardless of age.
12. `RMABOM` work orders never enter the MPS snapshot.
13. A repetitive-schedule change appears after the expected MRP/QADPRO2 propagation.

---

## 12. Stage 5A disposition

Snapshot and refresh behavior is accepted for the initial MPS vertical slice. Stage 5B may implement these semantics without inventing cache, refresh, or failure rules.

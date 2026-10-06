# Planning Workbook API (Stage 14.3–14.5)

**Status:** workbook endpoints implemented at Checkpoint 14.3 and owner-accepted; saved-draft
endpoints added at Checkpoint 14.5 (**owner-accepted 2026-10-06** with Checkpoint 14.5). Adjustment staging is
frontend state; the Mass Updater CSV export belongs to Checkpoint 14.6. The decisions
behind this API are in `docs/implementation/KST_v2_STAGE_14_SOURCE_AND_DECISION_LEDGER.md`. The exact
schema is in `docs/openapi/Kst.Api.json` (tag `PlanningWorkbook`).

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/v1/workspaces/{assignmentId}/planning-workbook?mpsSnapshotId={guid}&horizonWeeks={n}` | Returns the workbook, reusing compatible cached source facts or acquiring them when none are available. |
| `POST` | `/api/v1/workspaces/{assignmentId}/planning-workbook/refresh?mpsSnapshotId={guid}&horizonWeeks={n}` | Reacquires Open Orders, inventory, prices and forecast. **Does not refresh MPS.** |

Both parameters are required:

- `mpsSnapshotId` must be the workspace's currently loaded MPS snapshot (from the MPS API).
- `horizonWeeks` must be at least 1 and no more than the workspace's **MPS-derived maximum**
  (Stage 14.4 owner decision, 2026-10-02). There is no fixed 72-week cap for this API.
  - Maximum = `max(12, lastMpsWeekIndex + 2)`, where `lastMpsWeekIndex` is the index (current
    business week = 0) of the last Sunday–Saturday week, on **Due Date**, whose aggregated part/week
    quantity is nonzero, across the resolved parts of the **complete loaded MPS snapshot** (not the
    requested horizon). Falldown, sales orders, forecast and adjustments never extend it; with no
    future MPS the maximum is 12. It is derived from already-loaded facts; no QAD read is made.
  - It is recalculated on every request, so it changes with a new MPS snapshot or business week.
  - A request above the maximum returns 400 with `errors.horizonWeeks` and a
    `maximumHorizonWeeks` extension; no source is read.
  - The API accepts horizons from 1 so synthetic short projections remain available. The owner-confirmed
    **selectable minimum and default of 12 weeks** is applied by the Planning view, which also clamps
    a saved selection when the range shrinks. The Dashboard horizon is unchanged and independent.

## Sources

| Fact | Source | Notes |
|---|---|---|
| Parts, part order, MPS quantities | The workspace's loaded MPS snapshot (`IMpsSnapshotStore`) | Never acquired or refreshed by this API. No other supply source. |
| Sales orders | Stage 13 `OpenOrdersService` (the shared workspace Open Orders report) | `RA`-prefixed orders are excluded in the workbook only; lines with no Due Date are reported as Undated. |
| Net QOH | Existing batched `IPartInventoryReader` | Authoritative zeros come from the reader; a failure is never turned into zero. |
| Price tiers | New `QadPartPriceReader` (batched accepted Part Info rule) | Domain + part. Latest `pi_mstr` with `pi_start <=` the acquisition's local date; only that list's `pid_det` tiers, ordered by MOQ ascending. The first tier is selected. |
| Forecast | New `QadForecastReader` | `mrp_det` with `mrp_detail = 'FORECAST'` and `mrp_qty <> 0`, workspace domain/site/parts, at each row's own `mrp_due_date`. Display-only absolute value, including Falldown; it changes no other value and has no quarter total. |

All values are weekly (Sunday–Saturday weeks with Monday labels) or Falldown. Fiscal-quarter
grouping is done by the frontend, not by this API.

## Response

- **Identity and freshness:**
  - `workbookSnapshotId`, `mpsSnapshotId`, `openOrdersSnapshotId`
  - `acquiredAtUtc`, `priceEffectiveDate`, `priceSelectionPolicy` (`firstTier`)
  - `today`, `horizonWeeks`, `isStale`, `warning`
  - `minimumSelectableHorizonWeeks` (12) and `maximumHorizonWeeks` (the MPS-derived maximum above)
- **`weekLabels`:** the visible Monday labels for the requested horizon.
- **`parts`:** one block per part, in the MPS snapshot's retained order, each with:
  - `netQuantityOnHand`, `priceTiers`, `selectedPrice`, `isPriced`;
  - `falldown` and `weeks` buckets, each with `salesOrderQuantity`, `salesOrderDollars`, `mps`,
    `mpsDollars`, `forecast` and `estimatedOnHand`;
  - `undated` (`salesOrderQuantity`, `salesOrderDollars`, `lineCount`).

**Number and date formats:**

- Quantities, prices and dollars are **invariant decimal strings**, never rounded. JavaScript numbers
  cannot hold every .NET decimal.
- `mpsDollars` and `selectedPrice` are `null` for an unpriced part. A valid zero price gives a zero
  string, not `null`.
- Dates are ISO `yyyy-MM-dd` (`DateOnly`), as in other KST APIs.

## Caching and refresh

- **Per-workspace storage:** source facts (sales-order lines, Net QOH, price tiers, forecast rows)
  are kept per workspace assignment ID, in memory, and are not persisted across restarts.
- **Projection on every request:** the projection is recalculated from those facts and the current
  MPS snapshot on every request, so a projection can never be returned for the wrong horizon or
  business week.
- **Changing only the horizon** (same local date) reuses the cached facts and makes no QAD read.
- **Crossing a local date** (including into a new business week) makes `GET` reacquire inventory,
  prices and forecast, because cached facts are reused only when the cache's price effective date
  equals today's local date and a different price list may now be effective. This daily price
  policy is deliberate.
- **When `GET` reuses cached facts:** only when the workspace, site and MPS snapshot match **and** the
  cache's price effective date equals today's local date.
- **Open Orders on `GET`:** `GET` uses Stage 13's `GetAsync`, so a compatible cached Stage 13 report
  is reused rather than re-read.
- **Refresh:** `refresh` calls Stage 13 `RefreshAsync`, then rereads inventory, prices and forecast.
- **Concurrency:** acquisitions for a workspace run one at a time. After waiting for its turn, and
  again before publication or stale fallback, a request re-reads the workspace assignment: a deleted
  workspace returns 404, and a site that changed or no longer matches the loaded MPS returns 409.
  Neither publishes nor returns the previous-context workbook.

### Shared Open Orders effect

Refreshing the workbook refreshes the workspace's **shared Stage 13 Open Orders report**: the
workbook and the Customer Open Orders tab use the same report. The new report gets a new
`openOrdersSnapshotId`. Under the existing Stage 13 rules, a report export or QXtend export
prepared against the previous report ID is then rejected as changed (409) and must be prepared
again.

## Failure and stale behavior

- **Publication:** a new workbook snapshot is stored only after every source read succeeds. The MPS
  snapshot is rechecked after the reads and again before publication. If the MPS changes during
  acquisition, nothing is published and the request returns 409.
- **Source failure with a compatible last good snapshot:** the last good snapshot (same workspace,
  site and MPS snapshot) is kept and returned with `isStale: true` and a sanitized `warning`.
- **Source failure without a compatible last good snapshot:** 503.
- **Stage 13 stale report:** a stale Open Orders report (Stage 13 kept its last good report after a
  failed read) counts as a source failure. It is never presented as a current workbook.
- **No substitutes:** a failed source is never replaced by zeros, by "unpriced", or by an empty
  forecast. A reader result that leaves out a requested part, or a malformed row (for example a
  price tier without a price, or a forecast without a date), is a failure. The price query returns
  at least one row per requested part; only an explicit no-tier row means unpriced, and a missing
  row is a failed read.
- **Empty scope:** a resolved scope with no parts makes no inventory, price or forecast read. Stage 13
  also makes no QAD read for an empty scope.
- **Cancellation:** cancellation propagates and publishes nothing.

## Errors

| Status | When |
|---|---|
| 400 | `mpsSnapshotId` missing, empty or malformed; `horizonWeeks` missing, non-numeric or below 1. Both are marked required in OpenAPI; a missing or malformed value is rejected by parameter binding. Also returned (after workspace and MPS checks) when `horizonWeeks` exceeds the MPS-derived maximum, with a `maximumHorizonWeeks` extension. |
| 404 | Unknown workspace assignment ID, including one deleted during acquisition. |
| 409 | MPS not loaded, or `mpsSnapshotId` is not the workspace's current snapshot (including a change during acquisition), or the workspace site changed during acquisition. |
| 503 | A source is unavailable and there is no compatible last good workbook. |

Problem Details and logs are sanitized. Logs contain the workspace ID, the exception type name,
batch counts and timings only. They never contain business rows, part lists, prices, connection
details or SQL exception text.

## Unverified source evidence

Live QAD behavior is **not verified** (Stage 14 ledger §11, E1–E5):

- forecast sign, dataset and type (E1);
- price-list ties for equal `pi_start` or equal MOQ, and currency or unit of measure (E2). Like the
  accepted Part Info query, the batch reader adds no tie-breaker, so ties are not deterministic.
- `RA` prefix case variants and open lines with no Due Date (E3);
- Sunday-dated rows (E4) and Mass Updater acceptance of the proposed file format (E5), which do not
  affect this read-only API.

No live check has been run.

## Saved Planning drafts (Stage 14.5)

Optional, local, per workspace assignment. A draft holds **signed whole-number adjustments** and is
never applied to QAD; saving, restoring or deleting a draft makes no QAD read or write. Draft
operations never load or refresh MPS.

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/v1/workspaces/{assignmentId}/planning-workbook/draft/presence` | `{ exists }` from file existence only (no parsing, no MPS access). |
| `GET` | `/api/v1/workspaces/{assignmentId}/planning-workbook/draft?mpsSnapshotId={guid}` | Reads the draft and compares every saved entry with the workspace's **currently loaded** MPS snapshot (accepted Domain `PlanningDraftReconciler`). |
| `PUT` | `/api/v1/workspaces/{assignmentId}/planning-workbook/draft` | Body `{ mpsSnapshotId, entries: [{ part, weekLabel, adjustment }] }`. Saves proposals atomically. |
| `DELETE` | `/api/v1/workspaces/{assignmentId}/planning-workbook/draft` | Removes the draft file (204; missing file is not an error). |

**Storage.** `%LOCALAPPDATA%\KST\config\planning-workbook-draft-{assignmentId}.json`, written to a
temporary file and renamed into place. A file that cannot be read or fails validation is reported
(GET returns `exists: true, isCorrupt: true` with a warning; PUT returns 409 with `code:
draft-corrupt`) and is **retained unchanged** — it is never replaced with empty data. Delete removes
it. Workspace delete and reset remove the Planning draft (failure returns 503 and leaves the
workspace in place); archive preserves it. Drafts of different assignments never share a file.

**Saved content.** Workspace ID, site, MPS snapshot ID, saved-at time (UTC) and, per entry, part,
Monday week label, canonical signed adjustment text and the loaded MPS quantity it was saved against
(`null` when unavailable — week passed, part out of scope — distinct from a numeric `0`). No-op
entries (blank, `0`, `-0`) are dropped and never saved. Entries that are syntactically valid but
currently invalid (negative result, week passed, part out of scope) **are** saved so the user can
correct them later.

**Save validation (400, nothing written).** Invalid adjustment text (`invalid-number`,
`not-whole-number`), a non-Monday week (`not-a-week`), a duplicate part/week (`duplicate-identity`),
a missing/overlong part (`invalid-part`) or more than 10,000 entries (`too-many-entries`), reported as
the Problem Details `code`. A missing/changed MPS snapshot returns 409; an unknown workspace 404.

**Load response.** `exists`, `isCorrupt`, `warning` (also used when the draft was saved for a
different site), `savedAtUtc`, `savedMpsSnapshotId`, `savedSite`, and `rows[]` with `part`,
`weekLabel`, `adjustment`, `savedMpsQuantity`, `currentMps`, `baselineChanged`, `proposedMps` and
`issueCode`. `currentMps`/`proposedMps` are `null` when the baseline is unavailable. A changed
baseline keeps the signed adjustment — it is never converted into a target quantity — and there is no
acknowledgment step. Restoration is a frontend action after the user reviews this comparison.

**Errors.** 400 malformed request; 404 unknown workspace; 409 MPS not loaded / snapshot changed /
existing draft corrupt (PUT); 503 local file failure. Problem Details never include draft content,
part numbers or file paths.

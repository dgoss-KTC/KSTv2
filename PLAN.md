# Stage 8D.4 Plan — BOM Frontend (Scheduler Console BOM Tab)

> **STATUS: IMPLEMENTED — awaiting owner review.** Owner approved the plan with three
> amendments (below); all other plan requirements remain in force. Implementation is complete
> and all frontend checks are green (196/196 tests, typecheck/lint/build clean); no backend or
> generated-contract files changed. One implementation note: `useBom` is command-driven —
> the BOM-tab click (`activate()`) is the only path that starts a request, which makes the
> Amendment 2 no-transient-request guarantee structural (and satisfies the repo's
> `react-hooks/set-state-in-effect` lint rule that the effect-based draft violated).
>
> **Approved amendments:**
> 1. **Exact tab visibility** — Part Info and BOM are always visible/enabled when a parent detail
>    context exists; Work Orders is rendered ONLY when a bucket/Falldown context exists;
>    Shortages is rendered (disabled) ONLY when a bucket context exists; Future Shortages is
>    removed entirely; no disabled Work Orders/Shortages placeholders for parent-only selection.
> 2. **Edge-triggered activation** — BOM activation happens only on an explicit `enabled`
>    false→true transition (previous-enabled ref + `activatedIdentity` token). An identity change
>    while `enabled` is still true clears state but does NOT activate the new identity, so a
>    successful refresh issues no transient BOM request; the next explicit BOM-tab click loads.
> 3. **Obsolete async responses never commit** — `useBom` captures the request identity at fetch
>    start and ignores any response/error whose identity no longer matches the current one
>    (minimal identity-token guard; no shared request-management abstraction, no query library).
>
> This planning pass touched no production code, tests, generated files, or backend files.
>
> Stages 8D.1 (`bf89c60`), 8D.2 (`624e353`), and 8D.3 (`23fe252`) are complete, validated,
> accepted, committed, and pushed. The 8D.3 BOM API is live-validated (Site SW, parent
> `00-00013761-00`: 101 structural occurrences → 69 P/M-visible lines, 64 P / 5 M / 4 phantom,
> order/OccurrenceKey/Level/Qty/Scrap exact, sampled Net/Non-Net exact).
>
> Frontend baseline (verified this pass): **167/167 tests passing** (12 files),
> typecheck/lint/build clean per Stage 8D.3 closeout. Working tree clean on `main`.
>
> Scope: the smallest repository-consistent frontend implementation that lazy-loads the accepted
> 8D.3 BOM contract on first BOM-tab activation, preserves structural truth and repeated
> occurrences, provides local Component Item search, integrates with the existing MPS refresh
> invalidation, and leaves a clean seam for the 8D.6 Component Info card without building it.

---

## A. Repository Fit

All pieces below verified in the current tree (clean, `main`).

| Concern | Existing pattern (exact reference) | Reused as-is |
|---|---|---|
| Parent-detail shell | `src/frontend/src/components/MpsWorkspace.tsx` — owns transient `selectedParent`, `selectedBucket`, `activeTab` (`'partInfo' \| 'workOrders'`); renders `mps-detail__tabs` tablist + exactly one panel; 0 ms effect resets all three on `workspace.assignmentId` change; `previousSnapshotIdRef` effect clears bucket + resets tab to Part Info when `snapshot.snapshotId` changes (successful refresh) | shell unchanged in structure; gains BOM tab + `useBom` call |
| Part Info tab (parent-scoped sibling) | `hooks/usePartDetail.ts` (local state + `load` callback + `setTimeout(0)` effect; `partNumber = null` clears without calling backend), `api/partDetailApi.ts` (`fetch*` + `to*ApiError` + response shape guard), `components/PartInfoPanel.tsx` (purely presentational; loading / error+Retry / stale banner / `—` nulls), `PartInfoPanel.css` | pattern only; new `useBom` + `bomApi.ts` + `BomPanel` |
| Work Orders tab (snapshot-scoped sibling) | `hooks/useBucketWorkOrders.ts` — takes `snapshotId` as an input (sent to its backend, and also refetches on change); state cleared when selection becomes null; refetches when inputs change | pattern only — BOM takes the same `snapshotId` input but uses it as **frontend identity only** (the accepted BOM request carries no other parameters) |
| Kitting search convention | `components/WorkOrderMaterialGrid.tsx` — `filterText` local state; label + text input + conditional **Clear** button; empty-filter message `No material lines match “x”.`; `mps/mpsPresentation.ts` → `filterMaterialLinesByPart` (trim + case-insensitive substring on `componentPart`, **order-preserving**, duplicates kept, never re-queries) | **reuse `filterMaterialLinesByPart` as-is** (its generic constraint `T extends Pick<WorkOrderMaterialLineDto, 'componentPart'>` is structurally satisfied by `BomLineDto`) |
| Table/grid convention | Plain HTML `<table>` + BEM CSS (`WorkOrderMaterialGrid.css`): 10 px uppercase headers, 12 px body, `font-variant-numeric: tabular-nums`, part numbers in IBM Plex Mono, no sorting UI anywhere in the app (the only "sort" in Stage 7 is the deliberate kitting exception-first presentation sort) | plain table, **no sort affordance, no reordering call** — API order is structural truth |
| Numeric / null display | `mpsPresentation.formatQuantity` (`Number()` + `toLocaleString`, max 2 decimals) used for every quantity; `NO_VALUE = '\u2014'` for null; percent values rendered as `${formatQuantity(v)}%` (kitting convention) | unchanged |
| Stale treatment | `PartInfoPanel` banner: `detail.isStale` → `<div role="alert">` with `warning ?? 'Showing the last known part information.'` (`.part-info-panel__banner` tokens: `--danger-bg/--danger-tx/--danger-bd`) | same treatment in `BomPanel` with feature-local CSS class |
| MPS refresh mechanism | `hooks/useMpsDashboard.ts` — `refresh()` POSTs `/mps/refresh`; on success `setDashboard(result)` replaces the snapshot; `dateBasis`/`horizonWeeks` changes only re-GET (local re-projection). Backend verified: `MpsWorkspaceSnapshotService.GetDashboardAsync` auto-loads only when no snapshot exists; **re-projection keeps the same `SnapshotId`**; only `RefreshAsync` mints `SnapshotId.New()`. Failed refresh: `InMemoryMpsSnapshotStore.SetFailed` retains the prior snapshot + id → `snapshotId` unchanged in the UI | BOM keys off `snapshotId` change — the existing invalidation signal; no new framework |
| BOM freshness (backend, accepted) | `Kst.Application/Bom/BomService.cs` — a fresh cache hit additionally requires `cached.LoadedAgainstMpsSnapshotId == currentSnapshotId`; same-site/same-effective-date stale-last-good otherwise. Therefore **after a new snapshot generation the frontend must re-request** for the backend to re-evaluate | BOM request carries no parameters beyond workspace + parent (per accepted 8D.3 contract) |
| Generated BOM contract | `src/frontend/src/generated/api.ts` (from 8D.3): `BomLineDto { occurrenceKey, level, componentPart, pmCode, isPhantom, description, quantityPer, scrapPercentage, netQuantityOnHand, nonNetQuantityOnHand }`, `BomResponseDto { site, parentPart, effectiveDate, lines, loadedAtUtc, isStale, warning }`; route `GET /api/v1/workspaces/{assignmentId}/parts/{parentPart}/bom` (op `GetBom`) | types only; `api/client.ts` currently has **no** BOM aliases/`getBom` method — 8D.4 adds them (no generated-file edits) |
| Endpoint error semantics | `Kst.Api/Endpoints/BomEndpoints.cs` — 200 with `lines: []` for a valid in-scope parent with no BOM (empty ≠ error); 400 blank parent; 404 workspace-not-found / `"Part not in workspace scope"`; 409 `"MPS data not loaded"`; 503 `"BOM information unavailable"`. All 404/409/400 cases are effectively unreachable from the UI (parents are selectable only from loaded MPS grid rows) | collapsed to one retryable `{ type: 'error'; detail }` shape, mirroring how Stage 6 collapsed its unreachable cases |
| Prototype reference | `docs/reference/prototype/KST Scheduler Console.dc.html` — former "Components" tab = flat component list + right-side drill card in a two-column grid; **no** multi-level BOM grid, no Level/P/M/Phantom/Scrap/QOH columns exist there. The accepted 9-column BOM layout comes from the 8D.4 contract itself; visual language comes from the existing detail panels | the left-list/right-card split concept is the 8D.6 target; 8D.4 renders the full-width table only |
| Test conventions | Component/integration tests: `MpsWorkspace.test.tsx` renders `<App />` with a stubbed global `fetch` (per-endpoint handlers); presentational panels are unit-tested directly (`PartInfoPanel.test.tsx`, `WorkOrderMaterialGrid.test.tsx`) with prop-driven state. **No isolated hook tests exist** in the repo | lazy-lifecycle tests in `MpsWorkspace.test.tsx` (new describe block); grid/search/state tests in a new `BomPanel.test.tsx` |

**No repository gap requires a new abstraction.** The only new pieces are the BOM hook, the BOM
API module, and the BOM panel — each a 1:1 clone of an accepted Stage 6/7 sibling.

---

## B. Proposed Component Structure

Smallest set (4 new frontend source files, 2 modified; no other abstractions):

1. **`src/frontend/src/api/client.ts` (modify)** — add the established aliases
   `BomLineDto` / `BomResponseDto` from `components['schemas'][...]` and
   `ApiClient.getBom(assignmentId, parentPart)` →
   `GET /api/v1/workspaces/{assignmentId}/parts/{parentPart}/bom`.
   No request parameters other than the two path values (no site/domain/effective-date/bucket/
   basis/horizon/search). No handwritten DTOs — generated types only.

2. **`src/frontend/src/api/bomApi.ts` (add)** — sibling of `partDetailApi.ts`:
   - `BomApiError = { type: 'error'; detail: string }` (single variant — 404/409/400 are
     unreachable edge cases per the endpoint contract, so there is no 'missing' or 'stale'
     variant to model, unlike PartInfo/WO);
   - `toBomApiError(err)` — 404 → detail w/ fallback `'The requested part could not be found.'`;
     409 → `'This workspace\u2019s MPS data has not been loaded yet.'`; 503 → the established
     `'Database currently unavailable. Please try again in a few minutes.'`; 400 →
     `'The BOM request was invalid.'`; else `null` (hook supplies the generic fallback);
   - `isBomResponseDto` shape guard (`site`/`parentPart` strings, `lines` array, `isStale`
     boolean) + `fetchBom(assignmentId, parentPart)` via `resolveBackendBaseUrl()` + `ApiClient`,
     identical to `fetchPartDetail`.

3. **`src/frontend/src/hooks/useBom.ts` (add)** — the lazy/invalidation state machine (section C).
   Signature:
   ```ts
   useBom(assignmentId: string, parentPart: string | null, snapshotId: string | null,
          enabled: boolean): { bom: BomResponseDto | null; isLoading: boolean;
                               error: BomApiError | null; retry: () => void }
   ```
   Pure local React state (no query library, no shared cache — consistent with `usePartDetail` /
   `useBucketWorkOrders`; the backend owns authoritative freshness).

4. **`src/frontend/src/components/BomPanel.tsx` + `BomPanel.css` (add)** — purely presentational
   (props in, events out), sibling of `PartInfoPanel`/`WorkOrdersPanel`:
   panel header `BOM — {parentPart}` (no Back button — same as Work Orders panel), search row,
   BOM table, loading/empty/error/stale states. CSS mirrors `WorkOrderMaterialGrid.css`
   (filter row + table) and `WorkOrdersPanel.css` (panel shell, states, retry) using the same
   design tokens. No new alert/styling framework.

5. **`src/frontend/src/components/MpsWorkspace.tsx` (modify)** — the only shell changes:
   - `activeTab` union gains `'bom'`;
   - tab bar becomes **Part Info | BOM | Work Orders | Shortages**:
     - the existing disabled **Components** placeholder is replaced by a **BOM** tab placed
       directly after Part Info, **enabled whenever the detail panel is open** (parent selected)
       — BOM is parent-contextual, not bucket-contextual;
     - the disabled **Future Shortages** placeholder is **removed** (per accepted 8D.0:
       "Future Shortages is removed from the current workflow");
     - **Work Orders** keeps its existing `!selectedBucket` gating; **Shortages** remains the
       disabled Stage 9 placeholder;
   - calls `useBom(workspace.assignmentId, selectedParent,
     dashboard?.snapshot.snapshotId ?? null, activeTab === 'bom')` unconditionally (hook at shell
     level so data survives panel unmount, exactly as `usePartDetail`/`useBucketWorkOrders`);
   - renders `{activeTab === 'bom' && <BomPanel … onRetry={() => void retryBom()} />}`;
   - **no changes** to `handleParentRowSelect`, `handleBucketSelect`, `clearSelection`, the
     workspace-change reset, or the snapshot-change reset (all already route to
     `'partInfo'`/`'workOrders'` and are BOM-compatible as documented in C/D).

Search state lives in `BomPanel` (panel-local `useState`, lost when the panel unmounts on tab
switch — the existing detail-panel convention, since every detail panel is conditionally
rendered). Changing parent/workspace clears it implicitly because the panel remounts. No shared
search component is extracted (single real use; the *matching function* is reused, the *control*
is not).

---

## C. Lazy-Loading Lifecycle

`useBom` internal state: `bom`, `isLoading`, `error`, plus an **activation latch**
`activated: boolean`. Two effects, mirroring the repo's `setTimeout(0)` + `load`-callback style:

1. **Identity reset effect** — deps `[assignmentId, parentPart, snapshotId]`:
   `setBom(null); setError(null); setIsLoading(false); setActivated(false);`
2. **Activation effect** — deps `[enabled]`: `if (enabled) setActivated(true);`
3. **Load effect** — deps `[activated, load, parentPart, snapshotId]` where
   `load = useCallback(…, [assignmentId, parentPart, snapshotId])`:
   `if (!activated || parentPart === null || snapshotId === null) return;` else fetch via
   `fetchBom` and set state (`bom = null` + error on failure, as in the siblings).

Resulting behavior:

| Scenario | What happens |
|---|---|
| Parent selected (Part Info default) | `enabled = false` → load effect no-ops. **No BOM request.** Hook state stays null. |
| First BOM activation | `enabled` → true → latch set → load effect fires → **one request** for `(workspace, parent)`. |
| Switch to Part Info / Work Orders and back | `enabled` toggles but the latch and identity are unchanged → load effect does not re-fire. **Data retained, no repeat request.** |
| Parent changes | Shell always resets `activeTab` to `'partInfo'` (`handleParentRowSelect`) or `'workOrders'` (`handleBucketSelect`) — **the BOM tab is never active across a parent change in the current shell.** Identity effect clears `bom` + latch, so the new parent's BOM is lazy again; previous parent's rows can never render under the new parent (panel only renders non-null `bom`, and it's unmounted meanwhile). |
| Workspace changes | Shell 0 ms effect clears parent/bucket/tab **and** identity effect (assignmentId) clears state. No prior-workspace rows can appear; search text vanishes with the remounted panel. |
| Successful MPS refresh (BOM active or not) | See section D. |

No eager loading, no preload on tab existence, no parallel load with Part Info.

---

## D. Refresh / Invalidation Behavior

The mechanism is exactly the Stage 7 convention: **`snapshotId` is a frontend identity input to
the BOM hook** (it is *not* sent in the request). Backend verified: re-projections (Due/Release,
horizon) keep the same `SnapshotId`; only a successful refresh mints a new one; a failed refresh
retains the last-good snapshot + id.

| Event | Frontend effect | BOM request? |
|---|---|---|
| **Successful MPS refresh** | `dashboard` replaced → `snapshotId` changes → (a) existing shell effect clears bucket + resets `activeTab` to `'partInfo'` (unchanged behavior, Stage 7 §19); (b) BOM identity effect clears `bom`/error/latch | **Invalidated, not immediately refetched.** The shell already returns the user to Part Info (same as today's Work Orders drill-down), so "immediate refetch" would only flash data the user isn't looking at. The **next BOM activation** fires the request, which lets the backend evaluate the new freshness generation (fresh hit only when `LoadedAgainstMpsSnapshotId == new snapshot id`; otherwise same-date stale-last-good). This is the "invalidate and reload through the existing pattern" outcome — no new mechanism. |
| **Failed MPS refresh** | `snapshotId` unchanged (backend last-good retention) → no identity change; shell shows its existing warning banner over the retained grid | **None. The displayed BOM (if any) is untouched** — the backend's last-good semantics already cover load failures; the frontend does not discard compatible data on a failed refresh. |
| Bucket selected / changed / cleared | Not a hook input | None |
| Due/Release toggle | `GET /mps` re-projection, same `snapshotId` | None |
| Horizon / selected-week change | same `snapshotId` | None |
| Tab switching | `enabled` only; latch + identity unchanged | None |
| BOM search typing | panel-local filter only | None |

---

## E. Grid + Search Behavior

**Columns (exact, in order):**

| # | Header | Source | Rendering |
|---|---|---|---|
| 1 | Level | `line.level` | `formatQuantity(line.level)` — actual backend value, **gaps preserved** (e.g. 1 then 3), no renumbering, **no indentation** (the prototype's component list has no Level/indent concept, so no derived indent is introduced) |
| 2 | Component Item | `line.componentPart` | plain text, IBM Plex Mono (existing part-number styling) |
| 3 | P/M | `line.pmCode` | as returned (`P`/`M`); `—` if null (defensive only — contract is P/M-only) |
| 4 | Phantom | `line.isPhantom` | **`Yes` / `No`** — the app has no prior text-boolean column convention (kitting uses a row style + chevron, dialogs use checkboxes); explicit both-state text is the smallest accessible choice. Rows are never flattened. |
| 5 | Description | `line.description` | as returned; `—` when null |
| 6 | Qty Per | `line.quantityPer` | `formatQuantity`; `—` when null. No multiplication, no extended requirement. |
| 7 | Scrap | `line.scrapPercentage` | `${formatQuantity(v)}%` (kitting-percent convention); `—` when null |
| 8 | Net QOH | `line.netQuantityOnHand` | `formatQuantity` — direct display value |
| 9 | Non-Net QOH | `line.nonNetQuantityOnHand` | `formatQuantity` — direct display value; repeated occurrences legitimately repeat identical values |

**Structure:**
- Rows render `bom.lines` **in exact API order** — no sort call, no regrouping, no dedup;
  repeated `componentPart` occurrences remain separate rows.
- **Row identity: `key={line.occurrenceKey}`** on every row. The key is opaque — never parsed,
  displayed, or used to derive hierarchy.
- No sorting UI (the app has no sortable table anywhere; none is invented here).
- Not present: Reference, Operation, Effective Start/End, RMA, Extended Requirement, Incoming
  Supply, Coverage, Material Status, Short Qty, Projected QOH, PO quantities, MRP values.

**Search (top of BOM content, client-side only):**
- Control mirrors the accepted Stage 7 kitting search: `label` + text input + conditional
  **Clear** button. Label `Filter by Component Item`, placeholder `e.g. 00-0001`.
- Matching: **reuse `filterMaterialLinesByPart(bom.lines, query)` as-is** — trim +
  case-insensitive substring against `componentPart` only; **relative structural order
  preserved; repeated matches stay repeated**. No API call, no server parameter.
- Not searched: Description, P/M, Level, Phantom, inventory.
- Clearing (button or empty input) restores the complete returned sequence.
- Zero matches with data loaded: `No BOM components match “{query}”.` (mirrors
  `No material lines match “{query}”.`).

---

## F. Row Selection Seam — **recommendation: defer to 8D.6 (option B)**

Inspected reality:

- `.mps-detail` renders **one full-width panel**; the right-side card region does not exist yet.
  The prototype's two-column `list | drill-card` split (its "Components" tab) is precisely what
  8D.6 will introduce.
- The only existing row-selection in the app (`WorkOrderMaterialGrid`'s `expandedRowKey`) exists
  because it drives a **visible** drill-down. Selection with no consumer would be a dead
  affordance — and 8D.0 explicitly forbids dead buttons/behavior in Stage 8.
- 8D.4 already establishes everything 8D.6 needs: every row has stable `occurrenceKey` identity,
  `BomPanel` is purely presentational and prop-driven, and the hook retains the full `lines`
  array (component part + all card-relevant data) in shell-level state. 8D.6 adds selection state
  + click handling + the card + the layout split **without refactoring any 8D.4 code**.

Therefore 8D.4 adds **no selection state, no row click handlers, and no placeholder card**.

---

## G. State Handling

| State | Presentation (all feature-local, mirroring existing panels) |
|---|---|
| Loading (first activation, request pending) | `Loading BOM…` in the panel state area. **No empty-grid message during loading** (table renders only from non-null data). |
| Empty (successful, `lines = []`) | `No BOM components found for {parentPart}.` — deliberate non-error state, no `role="alert"` (same treatment as the Stage 7 no-WO state, which tests assert renders no alert). |
| Error (404/409/503/500/other) | `error.detail` + **Retry** button (calls `retry` → `load()`), `.state--error` styling — same as Part Info / Work Orders. A QAD failure is never rendered as an empty BOM. |
| Stale (`isStale = true`) | Banner above the table with `role="alert"`: `warning ?? 'Showing the last known BOM information.'` — the Part Info banner treatment, feature-local CSS class with the same `--danger-*` tokens. (Backend wording: "Showing the last known BOM information. A newer refresh could not be completed.") |
| Parent/workspace transition | Identity effect nulls state before/while the new context renders; the panel only renders non-null data, so **no previous-context rows can appear**; during the follow-up load the loading state shows instead. |

---

## H. Exact Implementation File Plan

**Add (frontend only):**
- `src/frontend/src/api/bomApi.ts`
- `src/frontend/src/hooks/useBom.ts`
- `src/frontend/src/components/BomPanel.tsx`
- `src/frontend/src/components/BomPanel.css`
- `src/frontend/src/components/BomPanel.test.tsx`

**Modify (frontend only):**
- `src/frontend/src/api/client.ts` — 2 type aliases + `getBom` method
- `src/frontend/src/components/MpsWorkspace.tsx` — tab state union, BOM tab button, remove
  Future Shortages placeholder, `useBom` call, conditional `<BomPanel>` render
- `src/frontend/src/components/MpsWorkspace.test.tsx` — 2 existing assertions updated (the
  "Future Shortages" disabled-tab assertion becomes an absence assertion; the "Components"
  disabled-tab assertion becomes a "BOM" enabled-tab assertion) + new lazy/refresh tests

**Tests to add** (conventions: `MpsWorkspace.test.tsx` = `<App/>` + stubbed fetch with a new
`onGetBom` handler and a `makeBomResponse()` fixture; `BomPanel.test.tsx` = prop-driven,
`PartInfoPanel.test.tsx` style):

*`MpsWorkspace.test.tsx` — new `describe('Stage 8D.4 BOM tab')` (tab/lazy + refresh):*
1. Parent selection alone does **not** request BOM (no `/bom` call in `fetchMock`).
2. First BOM-tab activation requests `GET …/parts/{parent}/bom` (and only that — assert URL has
   no other query params).
3. Leaving and returning to BOM for the same unchanged parent makes **no second** request.
4. Changing parent: prior rows disappear; no request until BOM re-activated; activation then
   requests the **new** parent.
5. Workspace switch: prior-workspace BOM rows cannot appear (new workspace starts unloaded).
6. Successful refresh (mock returns new `snapshotId`): displayed BOM is cleared, tab returns to
   Part Info (existing convention), and the **next** BOM activation re-requests.
7. Failed refresh (503): a loaded, displayed BOM remains rendered and on the BOM tab.
8. Due/Release toggle and horizon change with BOM loaded: **no** new `/bom` request.
9. Bucket select/clear with BOM loaded: **no** new `/bom` request.
10. Search typing with BOM loaded: **no** new `/bom` request.
11. Tab bar: parent-only selection → BOM tab present + enabled; Work Orders disabled;
    Shortages disabled placeholder; **no Future Shortages tab in the document**.

*`BomPanel.test.tsx` (grid + search + states):*
12. Column headers render in the exact accepted order (Level, Component Item, P/M, Phantom,
    Description, Qty Per, Scrap, Net QOH, Non-Net QOH).
13. Rows preserve API order (fixture deliberately not alphabetical by component part).
14. Actual Level values display unchanged, including a 1 → 3 gap.
15. Repeated `componentPart` occurrences remain separate rows (row count matches).
16. Rows are keyed by `occurrenceKey` (e.g. re-render with same data = no key warnings; DOM row
    order stable; two identical-part rows distinguishable).
17. P/M displayed; Phantom displayed (`Yes`/`No`); Net and Non-Net QOH displayed with
    `formatQuantity` formatting.
18. **RMA absent** (no RMA header/label anywhere in the panel).
19. Search: substring match on component part (case-insensitive); a description-only string
    matches nothing; relative order preserved; repeated matches stay repeated.
20. Clearing search restores the complete sequence; zero-match message renders (not an alert).
21. Loading state (no empty message); successful empty BOM message; error state + Retry
    callback; stale banner shows the backend `warning` text with `role="alert"`; null
    description/qty/scrap render `—`.

**No changes to:** backend, `docs/openapi/Kst.Api.json`, `src/frontend/src/generated/api.ts`,
other components, or unrelated tests.

---

## I. Verification Plan

**Automated (repository-documented commands):**
```powershell
cd src/frontend
npm run typecheck
npm run lint
npm test          # baseline 167 + ~20 new; 2 existing assertions updated (tab restructure)
npm run build
```
- Backend: **no build/test required** (no backend changes). Contract presence is already
  established by accepted 8D.3; `git status` must confirm `generated/api.ts` and
  `docs/openapi/Kst.Api.json` are **untouched**.
- Tauri: **no sidecar rebuild required** (frontend-only change; the sidecar is the backend).
  Live verification runs through the normal dev flow (`npx @tauri-apps/cli dev`) or the
  owner's usual desktop session.

**Manual owner-guided validation** (real app, read-only, QAD untouched; owner performs, agent
does not screenshot-loop). Use **Site SW / parent `00-00013761-00`** (the 8D.3-validated parent:
69 visible lines, 64 P / 5 M / 4 phantom, levels with gaps, repeated components):

- [ ] Select the parent → Part Info is the active tab; no BOM traffic until the BOM tab is clicked.
- [ ] Click BOM → loads; columns and their order match the accepted design.
- [ ] Rows match the 8D.3-validated 69-line result (order, repeated components separate).
- [ ] Level gaps (e.g. 1 → 3) are visible and not renumbered/indented away.
- [ ] P/M visible on every row; the 4 phantom rows flagged.
- [ ] Net / Non-Net QOH populate; repeated occurrences show identical values.
- [ ] Search `00-0001` (partial) filters by Component Item only; a description-only term
      matches nothing; Clear restores all 69 rows.
- [ ] Tab away and back → BOM retained, no new request (devtools network).
- [ ] Successful MPS Refresh → returns to Part Info; next BOM activation re-requests.
- [ ] Bucket select, Due/Release toggle, horizon change → no spurious BOM reload.
- [ ] Switch workspace / select another parent → no old BOM rows leak.
- [ ] (Only if naturally encountered) stale banner shows the backend warning — do **not**
      deliberately break QAD to force it.

---

## J. Risks / Owner Decisions

Three items warrant explicit owner confirmation; none blocks a repository-consistent
implementation, and each has a recommended default:

1. **Successful-refresh behavior while BOM is active (recommended default: invalidate +
   return to Part Info, refetch on next activation).** This follows the existing Stage 7 §19
   convention verbatim (the shell already resets to Part Info on every successful refresh, and
   Work Orders drill-down is discarded the same way). The alternative — keeping the BOM tab
   active and refetching in place — would change established shell behavior for every tab and is
   not recommended. *If the owner prefers in-place refetch, the change is confined to the
   snapshot-reset effect + one hook input; flag it before implementation.*
2. **Row selection (recommended default: defer entirely to 8D.6, section F).** 8D.4 provides the
   stable `occurrenceKey` row identity and presentational seam; adding selection state now would
   be dead infrastructure with no consumer until 8D.6.
3. **`filterMaterialLinesByPart` reuse as-is vs neutral rename.** Reusing it as-is is the
   smallest change (zero Stage 7 code/test churn) and its generic constraint already accepts
   `BomLineDto`; the only wart is the "MaterialLines" name in a BOM call site. A rename
   (`filterPartLinesByPart` etc.) is a one-line cross-feature change with test-name churn.
   Default: **reuse as-is**; rename only if the owner objects.

Parent/tab-state behavior required **no** decision: the current shell unambiguously resets to
Part Info on parent change (BOM tab is never active across a parent change) and clears all
detail state on workspace change, so sections C/D follow established behavior exactly.

No other genuine open issues were found; every accepted 8D.4 rule maps onto an inspected,
accepted repository pattern.

---

## K. Stop Confirmation

- **No production files changed** (this planning pass wrote/updated only `PLAN.md`).
- **No tests changed or added.**
- **No generated files changed** (`src/frontend/src/generated/api.ts`, `docs/openapi/Kst.Api.json` untouched; working tree clean).
- **No backend files changed.**
- **No commits created or pushed.**
- **Ready for human review/approval.** Implementation starts only after explicit owner
  approval of this plan (with or without the section J defaults overridden).

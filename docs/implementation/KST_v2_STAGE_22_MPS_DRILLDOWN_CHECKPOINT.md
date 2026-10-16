# Stage 22 — MPS drill-down, first redesign checkpoint

Date: 2026-10-15
Status: **IMPLEMENTED / OFFLINE-VERIFIED — OWNER DESKTOP ACCEPTANCE PENDING**

This is one bounded frontend checkpoint, not acceptance or completion of Stage 22. No commit,
push, live QAD read, dependency installation, backend/native change or sidecar publication was
performed. Application version remains `0.1.0-alpha.21` in this frontend-only checkpoint.

## Starting state and authority

- Verified laptop HEAD: `1831fb7` — accepted Stage 21 MPS/BOM exports. The uncommitted Reports
  Part Info Lookup implementation and documentation remained pending owner acceptance; they were
  not accepted, reverted or incorporated into a Stage 22 backend change.
- `AGENTS.md`, current status, master checklist, the accepted
  `KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md` (including the Stage 7R amendment), and Stage 9
  implementation/regression evidence define the retained behavior. Stage 9–11 algorithms remain
  locked. Stage 7R retains Falldown plus Week 0–3 eligibility and the existing candidate population.
- Design references: `C:\Dev\kst_v2_tmp\Stage22\MPS Drill-Down\` — the design spec, six numbered
  HTML boards and index. The owner's 2026-10-15 checkpoint requirements supersede conflicting
  examples: **All / Needs attention**, three-level limit, retain both card dates, keep the two
  modals distinct, and no export redesign. Boards are not calculation fixtures or admitted assets.
- Bounded implementation sequence: unify navigation; gate bucket entry on the existing result;
  control candidate replacement/depth; update cards/material controls/styles; exercise offline
  transitions, races and preservation regressions; build/type/lint; screen the actual diff.

## Delivered behavior

`MpsWorkspace` owns a single ordered `MpsNavigationLevel[]` for selection, rendering and commands:

```
MPS grid → parent → bucket (when selected) → WO material
         → manufactured-component candidate list → candidate WO material → … (depth ≤ 3)
```

- Back and Escape truncate the same active trail; ancestor breadcrumbs jump directly. A bucket
  list returns to parent-only Part Info before the full grid. Parent-only Work Orders remains
  available without a bucket. MPS grid returns focus to the originating parent row.
- Parent Info/BOM tabs describe only the parent in the trail, never a hidden candidate branch.
  Returning to Work Orders restores a still-valid branch. Neither tab retargets to a candidate.
  Basis changes invalidate WO investigation but retain parent-scoped Part Info/BOM; a replacement
  snapshot retains only a still-present parent and returns to Part Info. Workspace/site changes
  clear selection. Old branch scroll/focus records do not survive those invalidations.
- Prior list/material panes stay mounted but hidden while investigating deeper candidates.
  Valid selected cards, filters and scroll positions are retained; ancestor commands restore the
  opening control and captured ancestor scroll positions after the DOM update. Focus callbacks
  are themselves superseded by later navigation/context changes. Inactive MPS panes do not handle
  document Escape. Module departure cancels pending bucket entry, not valid Dashboard selection.
- A bucket click/Enter/Space uses the existing scoped planning-window API before committing any
  parent, bucket or tab change. Quantity is not an emptiness test. Pending feedback leaves the
  prior view visible. A successful empty result leaves the trail unchanged and announces
  “No work orders for this bucket” for four seconds. Errors/stale/mismatched-snapshot results are
  errors, not empty success. A 15-second UI timeout ends pending feedback and rejects later
  publication; it does not claim to cancel the underlying transport.
- Identity/attempt guards reject superseded bucket results, including after another selection,
  ancestor/tab navigation, module departure, workspace/snapshot/basis changes and timeout. The
  shared WO request helper masks mismatched state immediately and publishes only the newest
  live attempt for parent lists, candidates, material and summary indicators. Existing API clients
  do not expose transport cancellation; no new API/contract or request target was introduced.
- Whole cards are real buttons with `aria-expanded`, selected treatment and Show material /
  Material shown below. Existing quantities, kitting, SO, Release and Due remain. Material nests
  below the current list; manufactured “View work orders” replaces that list and adds a trail level.
- Candidate entry retains the originating WO and material required/issued quantities. It says
  Work Orders for the component and “not linked in QAD”; it does not claim pegging. No candidate
  is automatically selected. The selection instruction and Shortage summary unavailable remain.
- The accepted maximum is **three investigation levels**, counting the root WO as level 1.
  Candidate requests carry 2, then 3; level-3 manufactured rows remain manufactured with the
  action disabled and an explanatory title. The active immediate-material path no longer sends
  a constant depth 2. `WorkOrderMaterialGrid` was inspected as a separate caller, not repaired or
  deleted. Its existing escape-stack compatibility path remains available.
- Only the obsolete Shortages tab/state/render branch was removed. `ShortagesPanel` and Material
  Detail remain under Work Orders. All/Needs attention counts and membership use returned statuses:
  `short` or `unknown` only, without recalculation; `onHand` and manufactured `notApplicable` do not
  become attention items. The existing unknown/short/on-hand/manufactured sort and values remain.
- Broad labels now say Unable to confirm availability. Server reasons remain available; no
  diagnostic-prose parsing or inferred Part not found claim was introduced.
- Material Detail and Component Information keep their separate content and blocking backdrops.
  Escape closes the active modal first. Material Detail contains Tab/Shift+Tab at its sole control
  and restores its trigger; Component Information retains its existing focus handling.
- Trail/card/filter styling uses existing theme and density tokens, wrapping trails, constrained
  card widths, visible keyboard focus and horizontally scrollable material tables. Grid fiscal/status
  styling, sticky columns/headers and scroll ownership remain. Caption guidance explicitly describes
  Falldown and the next four weeks. No board fonts, global overrides, fixed canvas or dependencies.

## Verification evidence and limitations

Evidence root: `C:\Dev\kst_v2_tmp\Stage22\`.

- Final mocked/synthetic suite: **220 passed, 3 inherited skips, 0 failed across 10 files** —
  `frontend-final.json`. Of these, 24 navigation tests and 3 shared request-lifecycle tests are new;
  the card suite also adds whole-button keyboard behavior. Independently specified API fixtures exercise
  weekly/Falldown and later-week eligibility, hierarchy/ancestor jumps, focus and scroll return,
  pending/empty/error/stale/mismatch/timeout, superseded selections and A→B→A identities, candidate
  replacement/selection/depth, material filtering and reasons, tab transitions and modal precedence.
  Existing MPS, density, material-grid, Component Information, Reports and pending lookup regressions
  are included. The inherited three skipped Stage 7 legacy-grid cases remain skipped, not passes.
- Type checking and changed-file ESLint pass (`frontend-typecheck.log`, `frontend-scoped-lint.log`).
  Production frontend build passes (`frontend-build.log`) with the existing
  dynamic/static Tauri import and >500 kB chunk warnings. Owner `tsconfig.tsbuildinfo` is restored
  byte-for-byte after builds. `focused.diff` includes the changed frontend paths and relevant new
  files, including this implementation record. `shared-documents.diff` separately includes current
  status/checklist diffs against HEAD; those two shared files also contain the **pre-existing lookup
  documentation**, not solely Stage 22 work. `verification-summary.json` records final gates/limits.
- Full lint still **FAILS** (`frontend-full-lint.log`) only on the two unchanged locked Stage 10 setState-in-effect errors:
  `ComponentOrdersPanel.tsx:40` and `useComponentOrders.ts:27`. These are exceptions, not passes.
  Full-solution `dotnet format` and broad `cargo fmt --check` were not rerun in this frontend-only
  checkpoint; their existing documented failures remain exceptions, not passing evidence.
- Scoped AFT inspection was **PARTIAL** (missing diagnostic publications/incomplete analysis), not
  a clean assurance result. Successful compiler/build and tests are the authoritative offline gates.
- The existing archived visual fixtures under `C:\Dev\kst_v2_tmp\Old\Stage14\` are Planning
  scenarios with stale import/path assumptions, not an applicable runnable MPS synthetic entry point.
  No new harness was created, no browser/desktop UI was launched, and no live source was read.
  Simulated-DOM focus/scroll tests and CSS wiring are **not** Windows/webview or visual proof.
- Actual diff screened against `SECURITY.md`, `SECURITY_ASSURANCE_POLICY.md` and
  `AI_SECURITY_REVIEW.md`: frontend navigation, timing/publication of existing scoped read requests,
  ordinary text/status display, accessibility and CSS only. No new network target/listener/transport,
  trust boundary, credential handling, external input format, dependency, filesystem privilege,
  subprocess, export/file parsing, native capability or security-profile change. No applicable
  independent security-review trigger was identified for this bounded diff; no full independent AI
  security review or material-risk acceptance is claimed. Pre-existing Pkcs advisories remain open.

Earlier failing runs in the evidence folder are retained as correction history, not final successes.
They caught obsolete test expectations (Shortages tab, one-step bucket Escape, nested candidate
visibility), test queries now ambiguous with visible breadcrumbs/the pending lookup, and an overly
broad invalidation that discarded valid parent/BOM context. Final runs separately verify corrections.

## Pending owner desktop checklist

**Not executed by the agent.** Starting/loading or refreshing actual source data requires explicit
owner authorization for live read-only access. This checkpoint needs no new backend/native code or
sidecar republish; it uses the already published accepted API plus current pending lookup work.

1. Use an owner-selected workspace/snapshot. Check a nonempty Falldown and each eligible week;
   inspect an empty bucket without changing the prior view/trail. Confirm later weeks do not open
   a bucket. Exercise rapid choices and unavailable/stale feedback only where safely available.
2. Walk bucket → root material → component list → candidate material → next list → depth-3 material.
   Confirm no automatic candidate selection, retained origin quantities, truthful QAD wording,
   unavailable candidate summary, and disabled further traversal. Compare values with the loaded view.
3. Use Back, Escape and every ancestor breadcrumb. Confirm bucket → part → grid, correct focus,
   valid selections/filters and scroll return. Change basis, refresh the snapshot and switch workspace
   during loading/investigation; no obsolete branch should reappear. Switch modules and return.
4. Open Material Detail at root and candidate levels and Component Information from parent BOM.
   Use Tab/Shift+Tab/Escape/Close. One Escape closes only the modal and returns its triggering control.
   Part Info/BOM remain parent-scoped; their trail must not describe hidden candidate content.
5. Check All/Needs attention counts for short, unknown, on-hand and manufactured rows; inspect reasons
   without assuming a missing part. Confirm Shortages is absent and embedded material/detail remains.
6. Check light/dark × compact/comfortable at normal and narrow widths, including long part/WO trails.
   Verify no inaccessible/clipped controls; scroll the MPS and material grids and check sticky/fiscal
   presentation. Spot-check unchanged export/save behavior without treating it as redesigned here.

Record owner acceptance or corrections separately. Reports Part Info Lookup acceptance remains its
own open review. The next export checkpoint (shared protected save mechanism, retained-workbook
retry/unresolved-save actions, context-key/business-week corrections) is **not implemented here**.

## Owner-review correction — fixed MPS header stack (2026-10-15)

Owner reported that vertical scrolling moved the week labels up one row and hid the quarter band.
The CSS positioned every header cell as `sticky` at `top: 0`, so the three rows competed for the
same top edge. `MpsWorkspace.css` now pins the complete `thead` at `top: 0` with an opaque theme
background above body cells. Quarter/period/week rows retain their natural density-dependent
heights; the Parent Part/Falldown cells keep their existing horizontal sticky positioning.
No fixed offsets, calculation, navigation, export or source-access changes are involved.

`MpsWorkspace.density.test.ts` adds three CSS-wiring regressions for the grouped header, absence
of per-cell vertical sticking and preserved frozen-column layering. Focused header/MPS/navigation
tests: **113 passed, 3 inherited skips, 0 failed**. Build (including TypeScript) and scoped lint pass.
Follow-up evidence is `frontend-sticky-header-tests.json`,
`frontend-sticky-header-build.log` and `frontend-sticky-header-lint.log` in the same evidence root.
Owner build-info bytes were restored after the build. CSS wiring/simulated-DOM checks do not prove
actual webview layout. Owner subsequently confirmed “The scroll is working now.” This establishes
the reported header-scroll correction, not every density/theme combination or checkpoint acceptance.
No live reads, commits or pushes were performed for this correction.

## Owner-review correction — scroll only below the detail tabs (2026-10-15)

Owner reported that opening WO material made the selected MPS row disappear as the entire main
content scrolled. The detail area had `flex-shrink: 0` and grew to its complete content height,
overflowing into the shell scroller. The MPS workspace now contains its own overflow, retains the
focused grid without vertical shrinking, and gives the detail area the bounded remaining height.
The trail and Part Info/BOM/Work Orders tabs stay outside the scrolling content. Part Info/BOM have
bounded viewports; root and candidate WO lists each have a retained viewport that scrolls only
below the tabs. Hidden branches stay hidden, and existing navigation capture/restore includes
these viewports. Overscroll chaining is contained. No global shell/other-module scrolling rule,
business calculation, source request, navigation hierarchy or export behavior changed.

Follow-up: **179 passed, 3 inherited skips, 0 failed across five focused test files** (MPS behavior,
navigation/DOM containment/return scroll, density/header wiring, Component Information and Material
Detail). Frontend build/TypeScript and scoped ESLint pass. Evidence in the same root:
`frontend-detail-scroll-tests.json`, `frontend-detail-scroll-build.log` and
`frontend-detail-scroll-lint.log`. Owner `tsconfig.tsbuildinfo` was restored after the build.
As before, AFT inspection was partial and DOM/CSS tests are not actual webview layout proof.

Owner confirmation pending: open a bucket and WO with enough material to scroll; verify the selected
MPS row, trail and detail tabs stay visible while only the body below the tabs scrolls. Traverse a
manufactured component, return, switch the three detail tabs, and check retained list/filter/scroll
state plus modal focus return. The earlier accepted grid header behavior should remain intact.
No live source reads, dependency changes, commits or pushes were performed.

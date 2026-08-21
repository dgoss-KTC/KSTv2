# KST v2 — UI Navigation & Keyboard Ergonomics Plan

**Status:** Ergonomics A COMPLETE / ACCEPTED (owner manual validation PASS, 2026-08-21) — see §R.
Ergonomics B remains PLANNING ONLY — awaiting owner review.
**Date:** 2026-08-21
**Preceding stage:** Stage 8 — Component/BOM Detail (COMPLETE / ACCEPTED, `87bc6b1`)
**Sequence position:** Post-Stage-8 checkpoint 1 of 3 (UI Navigation & Keyboard Ergonomics → Documentation
Reconciliation → Stage 9)

No production code, tests, or documentation other than this artifact were modified while producing
the original plan below (§A–§Q). Ergonomics A (§K MUST list) has since been implemented and accepted
— see §R for the accepted outcome. Ergonomics B remains unimplemented planning only.

---

## A. Repository Baseline

- Branch: `main`. HEAD: `87bc6b1` (`feat: complete Stage 8D.7 Approved Alternates (AVL)`), which is
  also `origin/main`/`origin/HEAD` — fully pushed.
- Working tree: two untracked, unrelated items only — `docs/implementation/KST_v2_STAGE_8_CLOSEOUT.md`
  and `docs/reference/security/` (pre-existing, explicitly noted as untouched in the Stage 8 closeout).
  No modified tracked files.
- Stage 8 closeout (`KST_v2_STAGE_8_CLOSEOUT.md`) is present and records COMPLETE/ACCEPTED with owner
  manual validation PASS.
- Frontend test baseline per that closeout: **260/260 passing across 14 test files**, `npm run
  typecheck` clean, `npm run lint --max-warnings 0` clean, `npm run build` succeeded. No frontend
  source has changed since that pass (HEAD unchanged), so this baseline is still current.

---

## B. Existing Interaction Inventory

| Surface | Mouse behavior | Keyboard behavior | Escape | Focus behavior | Inconsistency / concern |
|---|---|---|---|---|---|
| `WorkspaceTabBar` — workspace tabs | Click selects tab; drag-and-drop reorders | Native tab order only; no arrow-key tab switching; `role="tab"`/`aria-selected` present but no `role="tablist"` roving-tabindex pattern | n/a (no dismissible surface at tab level) | No explicit focus management | Usability: drag-reorder has a keyboard equivalent (menu "Move Left/Right") — good — but arrow-key tab switching absent |
| `WorkspaceTabBar` — kebab action menu (`role="menu"`) | Click opens/toggles; click-outside closes | Menu items are native `<button role="menuitem">`, individually tabbable; no arrow-key menu navigation | Closes menu (document `keydown` listener) | No focus moved into menu on open; no focus restoration to the kebab button on close | Usability: no arrow-key roving focus inside an explicit `role="menu"`; minor — menu is small (5 items) |
| `AddWorkspaceDialog` (add/edit) | Backdrop click closes (guarded by `!saving`) | Local `Tab` focus trap on the dialog div; first field autofocused | Closes (guarded by `!saving`), bubble-phase local handler | Initial focus set; **no focus restoration** to the "+"/edit-menu trigger on close | **Inconsistency**: backdrop-closes here vs. Component Info's blocking no-close-on-backdrop; no focus restoration (Component Info has it) |
| `ManageWorkspacesDialog` | Backdrop click **always** closes (no busy guard — dialog has no busy state) | Local `Tab` focus trap; dialog container itself autofocused (`tabIndex={-1}`) rather than a control | Closes, bubble-phase local handler | No focus restoration to the gear-icon trigger on close | **Inconsistency**: same backdrop/focus-restoration gaps as above |
| `ConfirmDialog` | Backdrop click closes (guarded by `!busy`) | Local `Tab` focus trap; initial focus is Cancel when destructive, Confirm otherwise (safety-conscious) | Closes (guarded by `!busy`), bubble-phase local handler | No focus restoration to whatever triggered it (kebab menu item, Manage dialog button, etc.) | **Inconsistency**: no focus restoration; can be opened **on top of** `ManageWorkspacesDialog` (nested dialog-over-dialog with two independently-mounted backdrops) |
| `ComponentInfoModal` (Stage 8D.6) | Backdrop click does **not** close (intentional, blocking) | Local `Tab` focus trap; Close button autofocused | Closes via a **document-level, capture-phase** listener (works regardless of internal focus location) | Explicit focus restoration to the originating BOM `<tr>` (owned by `MpsWorkspace`) | Strongest pattern in the app; only surface with document-level Escape and focus restoration |
| MPS grid — parent part rows | Click selects/toggles parent | `tabIndex={0}` + `onKeyDown` Enter/Space activation — full keyboard parity with mouse | n/a | Selection state only; no explicit focus management needed (native) | None — good reference pattern |
| MPS grid — falldown/weekly bucket cells | Click selects a bucket (only when `isEligible`/falldown present) | **No `tabIndex`, no `onKeyDown`** — mouse-only | n/a | n/a | **Defect**: bucket cells have no keyboard equivalent although they are a primary interaction (opens Work Orders tab) |
| MPS detail tabs (Part Info / BOM / Work Orders) | Click switches tab | Native tab order between tab buttons; no arrow-key switching; disabled placeholder tab present for a future tab | n/a (no close concept) | No explicit focus management on tab switch | Usability: standard `role="tablist"` convention would suggest arrow-left/right switching, which is absent |
| `BomPanel` — BOM rows | Click opens Component Info modal | `tabIndex={0}` + `onKeyDown` Enter/Space — full keyboard parity | n/a (opens modal, which owns its own Escape) | Row element captured (`e.currentTarget`) and stored by `MpsWorkspace` for later focus restoration | None — good reference pattern, feeds Component Info's focus restoration |
| `WorkOrderCard` expand/collapse | Click toggles | Native `<button aria-expanded>` — Enter/Space work by default | n/a | No focus restoration needed (in-place disclosure, not a dialog) | None |
| `WorkOrderMaterialGrid` row expand | Click toggles | Native `<button aria-expanded>` — Enter/Space work by default | n/a | Same as above | None |
| Toasts (`ToastStack`) | Dismiss button click | Not inventoried in depth (low navigation risk); no Escape binding | n/a | n/a | Out of scope — non-blocking, auto-dismissing notifications |

---

## C. Existing Shared Patterns

- **Focus-trap pattern**: four independent, near-identical hand-rolled implementations
  (`AddWorkspaceDialog`, `ManageWorkspacesDialog`, `ConfirmDialog`, `ComponentInfoModal`), each
  re-querying a focusable-selector string and wrapping Tab/Shift+Tab manually. No shared helper exists.
- **Escape pattern**: three dialogs bind `Escape` via a local `onKeyDown` on the dialog `<div>`
  (bubble phase, dependent on focus being inside the subtree); `ComponentInfoModal` instead binds a
  document-level, capture-phase listener. These are functionally different mechanisms solving the
  same problem.
- **Row-activation pattern**: `tabIndex={0}` + `onKeyDown` Enter/Space is used consistently for BOM
  rows and MPS parent rows — this is a good, repeatable convention — but is missing on MPS bucket
  cells.
- **Disclosure pattern**: `aria-expanded` + native `<button>` is used consistently for Work Order
  card/material-grid expansion and the Approved Alternates section and the Add-Workspace "Limit to
  specific parent parts" collapsible — this is a solid, repeated convention needing no change.
- **Focus restoration**: implemented exactly once (Component Info → originating BOM row), owned by
  the parent component (`MpsWorkspace`) rather than the modal itself. No equivalent exists for any
  workspace-lifecycle dialog.
- **No shared modal/dialog abstraction** (e.g., a `useModal`/`useFocusTrap` hook or common `Dialog`
  wrapper) exists anywhere in `src/frontend/src/components` or `src/frontend/src/hooks`.

---

## D. Inconsistencies

| # | Description | Classification | Evidence |
|---|---|---|---|
| D1 | Backdrop click closes `AddWorkspaceDialog`, `ManageWorkspacesDialog`, `ConfirmDialog` but not `ComponentInfoModal` | Intentional difference (Component Info was explicitly designed blocking-only per Stage 8D.6) — **not** a defect, but the *other three* are inconsistent *with each other* only in busy-state guarding (see D2) | `ComponentInfoModal.tsx` header comment; closeout text §"Component Information" |
| D2 | `ManageWorkspacesDialog` backdrop closes unconditionally; `AddWorkspaceDialog`/`ConfirmDialog` guard backdrop-close against an in-flight operation (`saving`/`busy`) | Defect risk (low severity — `ManageWorkspacesDialog` has no async busy state today, so currently harmless, but the pattern diverges) | `ManageWorkspacesDialog.tsx` line ~66 (`onClick={onClose}`) vs. `AddWorkspaceDialog.tsx`/`ConfirmDialog.tsx` |
| D3 | No focus restoration to the triggering control for any workspace-lifecycle dialog (Add/Edit, Manage, Confirm) | Usability improvement (Component Info already proves the pattern and value) | `ApplicationShell.tsx` — dialogs opened with no captured trigger ref |
| D4 | Escape implemented via local bubble-phase `onKeyDown` (3 dialogs) vs. document-level capture-phase (`ComponentInfoModal`) | Usability/defect risk — bubble-phase approach is fragile if focus ever escapes the subtree (e.g., programmatic blur); capture-phase is more robust and is the more recently written pattern | Direct code comparison, section C |
| D5 | `ConfirmDialog` can stack on top of an already-open `ManageWorkspacesDialog` (two independent backdrops mounted) | Intentional-but-unreviewed — functions correctly today because focus stays within the topmost dialog, but was not something Stage 8 evaluated for a general convention | `ApplicationShell.tsx` renders both conditionally, simultaneously possible via `handleResetRequest`/`handleDeleteRequest` called from within `ManageWorkspacesDialog` |
| D6 | MPS bucket cells (falldown + weekly) have no `tabIndex`/`onKeyDown`, unlike MPS parent rows and BOM rows | **Defect** — this is a primary interaction (opens Work Orders detail) that is mouse-only | `MpsWorkspace.tsx` lines ~395–430 |
| D7 | No arrow-key switching for MPS detail tabs (`role="tab"`/`role="tablist"` present but incomplete per WAI-ARIA authoring practice) | Usability improvement, not a defect (native Tab order still reaches every tab) | `MpsWorkspace.tsx` lines ~440–478 |
| D8 | No arrow-key switching for workspace tabs (`WorkspaceTabBar`, also `role="tablist"`) | Usability improvement, same reasoning as D7 | `WorkspaceTabBar.tsx` |
| D9 | Kebab (`role="menu"`) has no arrow-key roving focus among menu items | Usability improvement (low priority — 5 short-lived items, Tab still works) | `WorkspaceTabBar.tsx` lines ~131–176 |
| D10 | Four independent hand-rolled focus-trap implementations with no shared helper | Usability/maintainability improvement, not a functional defect today | Section C |

No back-navigation gap was found: KST v2 has no browser-style navigation history today, and none of
the inventoried surfaces implies an unmet need for one (see §F/Back navigation below).

---

## E. Recommended Global Conventions

1. **Escape** — adopt the `ComponentInfoModal` document-level, capture-phase pattern as the standard
   for every blocking dialog. Escape closes/cancels only the topmost dismissible dialog. If a busy
   operation is in flight (save/delete in progress), Escape is suppressed exactly as today.
2. **Close/Cancel parity** — every dialog's X (if present), Escape, and Cancel/Close button must
   invoke the same callback and be guarded identically against busy state.
3. **Backdrop** — keep the existing, intentional split:
   - Blocking, information-only surfaces (Component Info) — backdrop does **not** close.
   - Form/action dialogs (Add/Edit Workspace, Manage Workspaces) — backdrop **does** close, always
     guarded against an in-flight busy state (fixes D2).
   - Destructive/confirmation dialogs (`ConfirmDialog`) — backdrop **does** close (cancels), guarded
     against busy state (already correct).
4. **Focus restoration** — every dialog/modal must restore focus to its triggering control (or the
   nearest still-connected reasonable fallback) on close, mirroring the Component Info pattern.
5. **Enter/Space** — every row-like or cell-like control that has a mouse `onClick` handler for a
   primary action must also expose `tabIndex={0}` + Enter/Space `onKeyDown` (fixes D6).
6. **Tabs** — native Tab order remains sufficient for reaching every tab; arrow-key left/right
   switching between tabs within an already-focused tablist is a SHOULD, not a MUST (see K).
7. **No global shortcut layer** — do not introduce a command-palette or hotkey library. Escape is the
   only cross-cutting keyboard convention; everything else remains local to its own widget.

---

## F. Proposed Shortcut Set

| Shortcut | Action | Context | Collision risk | Discoverability | Recommendation |
|---|---|---|---|---|---|
| `Escape` | Close topmost dialog/modal | Any open dialog | None (already implemented in 4 places) | Implicit/universal desktop convention | **Adopt as the only cross-cutting shortcut**; standardize mechanism (E1) |
| `Ctrl+R` / `F5` | Refresh current workspace/MPS | Workspace active | High — Tauri/webview may intercept `F5`/`Ctrl+R` as a page reload rather than the app's own refresh; behavior not verified in this pass | Existing refresh button already discoverable | **DEFER** — requires verifying Tauri webview key interception before recommending; not evaluated in this planning pass, do not implement without that verification |
| `Ctrl+F` | Focus BOM/Part Info search/filter field | Context-dependent (BOM tab only has a filter today) | Medium — meaning would differ per tab, and `Ctrl+F` is a strong existing browser convention users expect to mean "find on page" | Would need explicit tooltip | **DEFER** — no single unambiguous target field exists across tabs yet; revisit only if a future stage adds a workspace-wide search |
| `Alt+Left` (Back) | n/a | No back-navigation concept exists | n/a | n/a | **Do not implement** — no genuine back-navigation need was found (§Back navigation) |
| `Ctrl+1`/`Ctrl+2`/`Ctrl+3` | Switch Part Info / BOM / Work Orders tab | Parent selected | Low, but adds a shortcut for something already reachable in one Tab keystroke or one click | Would need a persistent hint | **DEFER** — cognitive cost outweighs benefit for 3 tabs reachable by native Tab/click; revisit only if detail tabs grow substantially |
| Arrow-key tab switching (not a "shortcut" per se) | Move focus between adjacent tabs when a tab is already focused | MPS detail tabs, workspace tabs | None — standard WAI-ARIA tablist behavior | Standard, expected by any user of a native tab widget once one tab has focus | **SHOULD implement** (see K) — this is a widget-conformance behavior, not a new keyboard shortcut, and has no collision risk |

**Recommendation:** no new global shortcuts beyond the existing/standardized Escape. `Ctrl+R`/`F5`
and `Ctrl+F` are explicitly deferred pending owner input and/or future need, not silently rejected.

---

## G. Modal / Dialog Standard

| Behavior | `ComponentInfoModal` (reference) | `ConfirmDialog` | `AddWorkspaceDialog` | `ManageWorkspacesDialog` | Recommended target |
|---|---|---|---|---|---|
| X close | Yes | n/a (no X; Cancel/Confirm buttons) | No X (Cancel via Escape/backdrop only — no explicit Cancel button either) | Yes (Close button, not an X) | Keep each dialog's existing close affordance style; do not force a uniform X — differing affordances (X vs. labeled Cancel/Close button) are appropriate to differing dialog types |
| Escape | Document-level capture | Local bubble | Local bubble | Local bubble | Standardize all four on document-level capture (E1) |
| Backdrop | No close (intentional) | Closes (guarded) | Closes (guarded) | Closes (**un**guarded) | Guard `ManageWorkspacesDialog` backdrop against future busy state for consistency (currently harmless but should match the pattern) |
| Focus trap | Yes (hand-rolled) | Yes (hand-rolled) | Yes (hand-rolled) | Yes (hand-rolled) | Extract to one shared trap implementation (K/DEFER — SHOULD) |
| Initial focus | Close button | Safe action (Cancel if destructive) | First input | Dialog container itself | Keep per-dialog semantics (safety-first for destructive, first-field for forms) — do not force uniform initial-focus target |
| Focus restoration | Yes, to originating BOM row | No | No | No | Add to all three (E4/K — MUST) |

**Recommendation:** `ComponentInfoModal`'s Escape mechanism and focus-restoration discipline should
become the shared standard; its backdrop-blocking behavior remains intentionally unique to it as an
information-only, non-form surface.

---

## H. Grid / Row Navigation

| Grid | Current behavior | Recommended minimum keyboard behavior | Arrow-key navigation worth adding now? |
|---|---|---|---|
| MPS parent rows | Full keyboard parity (`tabIndex`, Enter/Space) | No change needed | Not needed — native Tab order through rows is adequate for the current row counts; owner has not requested spreadsheet-style arrow movement |
| MPS falldown/weekly bucket cells | Mouse-only | Add `tabIndex={0}` + Enter/Space `onKeyDown` mirroring the parent-row pattern exactly (MUST — this is a correctness/consistency fix, not new capability) | Arrow-key movement between cells: DEFER — would require a larger custom-grid keyboard model (`role="grid"`/`gridcell` with roving tabindex) disproportionate to this checkpoint |
| BOM rows | Full keyboard parity | No change needed | Not needed |
| Work Order cards / material grid rows | Native `<button>` expand affordance | No change needed | Not applicable — these are disclosure toggles, not a navigable grid |

**Recommendation:** the only MUST here is bringing bucket cells to parity with the already-accepted
row-activation convention (D6). Full ARIA-grid arrow-key navigation is explicitly out of scope for
this checkpoint.

---

## I. Tab Navigation

- **MPS detail tabs** (Part Info / BOM / Work Orders) and **workspace tabs** (`WorkspaceTabBar`) both
  use `role="tab"`/`aria-selected` but rely solely on native document Tab order to move between tabs;
  neither implements the WAI-ARIA tabs pattern's expected Left/Right (and Home/End) roving-tabindex
  arrow key behavior.
- Recommendation: add standard roving-tabindex arrow-key switching (Left/Right moves focus and
  activates the adjacent tab; Home/End jump to first/last) to both tab groups. This is a SHOULD, not a
  MUST — current behavior is not broken, merely non-conformant with the pattern users may expect from
  a native-feeling tab widget.
- No additional shortcuts (e.g., `Ctrl+1..3`) are recommended for tab switching (see F).

---

## J. Focus Management

Focus restoration should be added for:

1. `AddWorkspaceDialog` (add mode) → the `WorkspaceTabBar` "+" button.
2. `AddWorkspaceDialog` (edit mode) → the kebab menu's "Edit Workspace" menu item's owning tab (the
   menu will already be closed; restoring to the tab button itself, not the transient menu item, is
   the correct target since the menu item is unmounted by the time the dialog opens).
3. `ManageWorkspacesDialog` → the gear ("Manage workspaces") button, *or*, if opened via
   `GeneralWorkspace`'s "Manage Workspaces" entry point, that originating control.
4. `ConfirmDialog` → whichever control opened it (kebab "Archive"/"Delete" menu item's owning tab
   button, or the `ManageWorkspacesDialog` "Restore"/"Delete Permanently"/"Reset" button if `Manage
   Workspaces` remains open beneath it).
5. `ComponentInfoModal` → already implemented; no change.

For the nested case (`ConfirmDialog` opened from within `ManageWorkspacesDialog`), the recommended
target is: closing `ConfirmDialog` restores focus into `ManageWorkspacesDialog` (to the button that
triggered it, if still present in the list; otherwise to the dialog's own Close button), not out to
the underlying application shell — `ManageWorkspacesDialog` is still open and owns focus context.

---

## K. Exact Implementation Scope

**MUST — correctness/consistency**
- M1. Standardize Escape handling on the document-level capture-phase pattern in `ConfirmDialog`,
  `AddWorkspaceDialog`, `ManageWorkspacesDialog` (fixes D4).
- M2. Add focus restoration to the triggering control for `ConfirmDialog`, `AddWorkspaceDialog`,
  `ManageWorkspacesDialog` (fixes D3), including the nested Confirm-over-Manage case (§J).
- M3. Add `tabIndex={0}` + Enter/Space keyboard activation to MPS falldown and weekly bucket cells
  (fixes D6).
- M4. Guard `ManageWorkspacesDialog`'s backdrop-close against a future busy state, matching
  `AddWorkspaceDialog`/`ConfirmDialog` (fixes D2; currently latent since no busy state exists yet, but
  should be added defensively as part of this consistency pass since it's a one-line change alongside
  M1/M2 touching the same file).

**SHOULD — high-value ergonomics**
- S1. Add roving-tabindex Left/Right (and Home/End) arrow-key switching to MPS detail tabs (fixes D7).
- S2. Add the same arrow-key switching to `WorkspaceTabBar` (fixes D8).
- S3. Extract the four duplicated focus-trap implementations into one shared `useFocusTrap`/`Dialog`
  helper (fixes D10) — a genuine second/third/fourth real use already exists, satisfying the
  Shared-Abstraction Rule in `AGENTS.md` §5.
- S4. Add arrow-key roving focus to the workspace kebab `role="menu"` (fixes D9).
- S5. Add discoverability hints (`title`/tooltip text such as "Close (Esc)") to close affordances that
  don't already have one.

**DEFER — optional complexity**
- D-1. `Ctrl+R`/`F5` refresh shortcut — pending Tauri webview key-interception verification.
- D-2. `Ctrl+F` contextual search focus — pending a single unambiguous target existing.
- D-3. `Ctrl+1..3` tab-switching shortcuts — cognitive cost not justified for 3 tabs.
- D-4. Full ARIA `role="grid"` arrow-key cell navigation for the MPS grid.
- D-5. A Keyboard Shortcuts help dialog — not justified until more shortcuts are accepted.
- D-6. Any `Alt+Left`/back-navigation concept — no genuine need identified.

---

## L. File Plan

Frontend only; no backend/business changes.

| File | Change |
|---|---|
| `src/frontend/src/components/ConfirmDialog.tsx` | M1 (document-capture Escape), M2 (focus restoration prop/callback) |
| `src/frontend/src/components/AddWorkspaceDialog.tsx` | M1, M2 |
| `src/frontend/src/components/ManageWorkspacesDialog.tsx` | M1, M2, M4 |
| `src/frontend/src/components/ApplicationShell.tsx` | M2 wiring — capture/pass trigger refs into the three dialogs above; nested Confirm-over-Manage focus target (§J) |
| `src/frontend/src/components/MpsWorkspace.tsx` | M3 (bucket cell keyboard activation); S1 (tab arrow-key switching) if accepted |
| `src/frontend/src/components/WorkspaceTabBar.tsx` | S2 (tab arrow-key switching), S4 (menu roving focus) if accepted |
| `src/frontend/src/hooks/` (new file, e.g. `useFocusTrap.ts`) | S3 shared focus-trap extraction, if accepted, consumed by the four dialogs above and `ComponentInfoModal.tsx` |
| `src/frontend/src/components/*.css` (as needed) | S5 minor tooltip/hint text only — no visual redesign |

No API/DTO/OpenAPI changes. No backend changes. No Rust/Tauri changes anticipated (pending D-1
verification, which is deferred and out of this implementation pass regardless).

---

## M. Tests

| Change | Test file(s) |
|---|---|
| M1 (Escape mechanism) | `ConfirmDialog` currently has coverage inside `WorkspaceLifecycle.test.tsx`; add/extend cases there for Escape-closes-regardless-of-focus-location, matching the existing `ComponentInfoModal.test.tsx` cases. Add equivalent cases to an `AddWorkspaceDialog.test.tsx` (exists) and a new/extended test for `ManageWorkspacesDialog` (currently no dedicated `ManageWorkspacesDialog.test.tsx` — add one, or extend `WorkspaceLifecycle.test.tsx` coverage) |
| M2 (focus restoration) | Extend `WorkspaceLifecycle.test.tsx` and `AddWorkspaceDialog.test.tsx` to assert `document.activeElement` returns to the originating trigger button after each dialog closes, mirroring the existing `MpsWorkspace.test.tsx` Component Info focus-restoration assertions |
| M3 (bucket cell keyboard activation) | Add cases to `MpsWorkspace.test.tsx` — Enter/Space on a falldown/weekly cell opens Work Orders exactly as a click does; ineligible cells remain non-activatable |
| M4 (backdrop guard) | Extend `WorkspaceLifecycle.test.tsx` — backdrop click during a (future) busy state does not close `ManageWorkspacesDialog` |
| S1/S2 (arrow-key tabs) | Extend `MpsWorkspace.test.tsx` and a `WorkspaceTabBar` test (add if none exists) — Left/Right/Home/End move focus and activate the adjacent tab; inactive tabs are not reachable by plain Tab from within the tablist (roving tabindex) |
| S3 (shared focus-trap extraction) | Existing dialog test files should continue to pass unmodified in behavior (regression-only; no new test file required beyond re-running M1/M2 coverage against the refactored implementation) |
| S4 (menu roving focus) | Extend `WorkspaceTabBar` test — arrow keys move among menu items; Escape/click-outside still closes |

Do not add tests for internal focus-trap implementation details; test observable keyboard behavior
only, per repository test-discipline conventions.

---

## N. Manual Owner Validation

Short desktop walkthrough after implementation (no screenshot-loop automation):

1. Open a workspace → confirm Escape does nothing when no dialog is open.
2. Click "+" to add a workspace → press Escape → confirm dialog closes and focus returns to "+".
3. Open the kebab menu on a workspace tab → Archive → confirm dialog appears → press Escape →
   confirm it cancels, focus returns to the kebab/tab area, and the workspace was not archived.
4. Open Manage Workspaces → click Delete Permanently on an archived workspace → confirm dialog
   appears on top → press Escape → confirm only the confirmation closes (Manage Workspaces remains
   open) and focus lands back inside Manage Workspaces.
5. Select a parent part → use Tab to reach a falldown/weekly cell → press Enter/Space → confirm Work
   Orders opens exactly as it would with a click.
6. In MPS detail tabs, focus a tab and press Right/Left (if S1 accepted) → confirm focus and active
   tab move together predictably.
7. Open a BOM row's Component Information → press Escape → confirm focus returns to that BOM row
   (already-accepted behavior — confirm no regression).
8. Confirm no keyboard shortcut introduced in this pass conflicts with ordinary Windows habits
   (Alt+Tab, Ctrl+Tab, F5/Ctrl+R browser-reload muscle memory) — since none beyond Escape/arrow-keys
   are recommended, this should require no special attention.

---

## O. Risks / Owner Decisions

Genuine choices requiring owner input:

1. **Arrow-key tab switching (S1/S2)** — adds a small amount of new interaction code to two already
   ーworking tab widgets for a conformance/ergonomics benefit rather than a reported problem. Owner
   should confirm this is worth doing now vs. deferring.
2. **Shared focus-trap extraction (S3)** — a refactor of four working, tested implementations.
   Low risk technically (behavior-preserving), but touches multiple files; owner should confirm
   priority relative to M1–M4.
3. **`Ctrl+R`/`F5` refresh shortcut** — deliberately deferred pending verification of Tauri webview
   key interception; if the owner wants this shortcut, a short technical spike (not part of this
   plan) should precede any implementation decision.
4. **`Ctrl+F` contextual search** — deliberately deferred; no single obvious target field exists yet.
   Owner should say whether this is worth revisiting once a future stage adds more search surfaces.

No other owner decisions are required — established conventions (Escape as universal dismiss,
Enter/Space row activation, safety-first destructive-dialog focus) already answer the remaining
questions from repository evidence.

---

## P. Recommended Implementation Decomposition

Given the MUST list (M1–M4) is small and touches a bounded, well-understood set of files, and the
SHOULD list (S1–S5) is separable and lower urgency, recommend splitting into two checkpoints rather
than three — a third checkpoint is not warranted by the evidence gathered:

- **Ergonomics A — Modal/dialog and row-activation consistency (MUST: M1–M4)**
  Standardizes Escape, backdrop guarding, and focus restoration across all four dialogs/modals, and
  brings MPS bucket cells to keyboard parity with parent rows. This is the smallest coherent
  correctness pass and should ship first.

- **Ergonomics B — Tabs, menu, and shared focus-trap ergonomics (SHOULD: S1–S5)**
  Arrow-key tab/menu navigation, the shared focus-trap extraction, and discoverability hints. Lower
  urgency, larger surface area, and benefits from A already being stable underneath it.

No "Ergonomics C" (global shortcuts) checkpoint is recommended — the shortcut evaluation in this plan
concluded that no new global shortcuts beyond the already-implemented/standardized Escape are
currently justified (§F).

---

## Q. Stop Confirmation

- This was a planning-only pass. No production code was modified.
- No tests were modified.
- No documentation other than this planning artifact was modified.
- No Stage 9 work was started.
- No broader Project Documentation Reconciliation / Repository Memory Cleanup was performed (that
  remains the next checkpoint per the Stage 8 closeout handoff).
- No commits were made and nothing was pushed.
- Ready for owner review of §K (MUST/SHOULD/DEFER scope) and §O (owner decisions) before any
  implementation checkpoint begins.

---

## R. Accepted Implementation Outcome (Ergonomics A)

**Status: COMPLETE / ACCEPTED — owner manual validation PASS (2026-08-21).**

This plan's §D/§F conclusion that "no genuine back-navigation need was found" is **superseded** by
owner manual validation. Live use of the MPS Work Orders drill-down (Show Material Lines, nested
manufactured-candidate branches, the Work Order view itself) demonstrated a real, multi-level
back-navigation need that the original inventory did not surface, because it only covered
dialog/modal dismissal and had not yet exercised the deeper Work Orders drill-down interactively.

The authoritative Escape/navigation convention, as implemented and owner-validated, is:

1. **Topmost blocking modal/dialog** → Escape closes/cancels that modal only.
2. **Nested detail within the current investigation** → Escape collapses the deepest open detail
   exactly one level.
3. **Main MPS detail/drill-down** → Escape returns to the Part Matrix.
4. **Part Matrix/root level** → Escape does nothing.

Examples exercised and confirmed:

- Component Information → BOM → Part Matrix
- Show Material Lines → Work Order view; deeper material/candidate detail → previous material
  level; Work Order view → Part Matrix
- Part Info → Part Matrix

Implementation:

- `ApplicationShell.tsx` centralizes Escape arbitration for the three workspace dialogs
  (Confirm/Add/Manage) with busy/saving-gated precedence; `ComponentInfoModal.tsx` keeps its
  existing independent document-level Escape (unchanged).
- `MpsWorkspace.tsx` owns a single document-level, capture-phase Escape handler for the MPS
  drill-down: it defers to `ComponentInfoModal` when open, then pops one level of a LIFO "escape
  stack" (`src/frontend/src/mps/escapeStack.ts` — `EscapeStackContext` + `useEscapeLevel` hook) if
  any nested Work Orders expansion is registered, and otherwise closes the whole detail panel back
  to the Part Matrix.
- `WorkOrderCard.tsx` (material lines) and `WorkOrderMaterialGrid.tsx` (candidate branch) each
  register their local expansion state on the escape stack via `useEscapeLevel`, so Escape collapses
  exactly the most-recently-opened nested level first — including when multiple sibling Work Order
  cards are independently expanded at once.

Verification:

- Frontend final test suite: **281/281 passing**.
- `npm run typecheck`, `npm run lint --max-warnings 0`, and `npm run build`: clean.
- `git diff --check` / `git status --short`: clean at time of commit.
- Owner manual validation of the live desktop app: **PASS**.

Deferred:

- Ergonomics B (arrow-key tab/menu navigation, shared focus-trap extraction, discoverability hints —
  §P) remains deferred, unchanged from this plan's original recommendation.
- No global shortcut layer was added, consistent with §F.

Next: Documentation Reconciliation / Repository Memory Cleanup (per the Stage 8 closeout handoff).

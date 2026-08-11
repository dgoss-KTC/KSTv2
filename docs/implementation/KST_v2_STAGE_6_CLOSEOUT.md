# KST v2 — Stage 6 Part Information Drill-Down Closeout

**Status:** COMPLETE / ACCEPTED  
**Owner acceptance date:** 2026-08-11  
**Next stage:** Stage 7 — Work Orders and Kitting (discovery/planning)

## Completion statement

Selecting an MPS parent part now collapses/focuses the schedule around that parent and displays validated QAD part-master attributes, inventory summaries, and current MOQ/price information through a lazy-loaded Part Info drill-down. Clicking the selected parent again or using `Back to full grid` restores the full MPS view.

## Accepted Stage 6 behavior

- Part Info is parent-part scoped, not week scoped.
- The focused MPS renders only the selected parent row and shrinks naturally; it does not retain blank height from the full grid.
- Parent-row mouse and keyboard activation are supported.
- Selecting an unselected parent opens Part Info; selecting the same parent again closes Part Info and restores the full grid.
- `Back to full grid` remains the explicit discoverable exit action.
- Due/Release, horizon, fiscal, density, and presentation changes do not trigger PartDetail reloads.
- PartDetail is lazy-loaded and cached against workspace + parent + current MPS snapshot identity/generation.
- Successful workspace refresh makes prior detail stale for next access; failed refresh preserves compatible last-good detail.
- Fresh PartDetail failure can return stale last-good data with warning when prior data exists.

## Implemented PartDetail data

Part master: Part Number, Planner, Mfg Lead Time, Safety Time, Part Status code + description, Current Revision, Description, IOS Code, Safety Stock.

Inventory: Qty On Hand = positive non-RMA nettable inventory; Qty Non-Net = positive non-RMA non-nettable inventory. Zero and negative balances are ignored.

Pricing: the current price list is the most recent `pi_mstr` row with `pi_start <= today`; all associated `pid_det` MOQ/price tiers are returned in MOQ order. No current price is normal missing informational data.

## Verification evidence

- Backend final suite: **316/316 tests passing**.
- Frontend final suite after owner-review refinements: **119/119 tests passing**.
- Backend formatting/build checks clean.
- Frontend lint/typecheck/build clean.
- Rust/Tauri `cargo check` clean.
- Backend sidecar rebuilt after backend changes.
- Live read-only QAD validation performed against five available development workspaces and 71 representative parents.
- Representative Part Status codes, happy-path PartDetail, cache-hit behavior, API edge cases, and positive net/non-net inventory were validated.
- Inventory and pricing examples were cross-checked against direct read-only SQL and matched.
- Latest `pi_start <= today` price-list selection was confirmed with real QAD evidence.
- Rare cases not naturally present in the available live data set (multi-tier price breaks, RMA exclusion, no-current-price, additional site/domain) remain covered by deterministic automated tests rather than manufactured QAD data.
- Final Tauri owner-review verified focused-grid spacing, selected-row toggle-to-close, explicit Back-to-full-grid behavior, keyboard behavior, and clean shutdown with no orphan processes.

## Scope preserved

Stage 6 did not begin Stage 7 work. Work orders, kitting, WIP, BOM/component detail, shortages, PO detail, buyer comments, and other later-stage concepts remain outside PartDetail.

## Documentation closeout

Stage 6 contract, implementation progress, backend-boundary/API documentation, master checklist, and current project status have been reconciled to the implemented behavior.

## Remaining administrative action

Record the final Stage 6 commit hash in the checklist/status documentation after the accepted changes are committed. This is repository bookkeeping and does not block functional owner acceptance.

## Final decision

**Stage 6 completion gate: PASS. Stage 6 is COMPLETE / ACCEPTED.**

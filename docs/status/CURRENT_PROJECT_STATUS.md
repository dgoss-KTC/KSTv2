# Current Project Status

Date: 2026-08-11  
Workstation: Windows (`C:\Dev\kst_v2`)  
Current stage: **Stage 7 — Work Orders and Kitting — discovery/planning ready**  
Stage 6 status: **COMPLETE / ACCEPTED — 2026-08-11**

## Current Position

Stages 1 through 6 are complete and accepted. Stage 6 delivered the first production Part Information drill-down from the real MPS dashboard and validated the lazy-loaded PartDetail architecture against live read-only QAD data.

The next rolling-wave phase is Stage 7 — Work Orders and Kitting. Do not begin Stage 7 production implementation until its UI behavior, field/source inventory, business rules, and backend/API contract are reviewed and accepted.

## Stage 6 Accepted Behavior

- Selecting an MPS parent focuses the grid to that parent and opens Part Info directly beneath it.
- Clicking the selected parent again or `Back to full grid` restores the full MPS grid.
- Focused mode renders only the selected row and no longer retains excessive blank full-grid height.
- Part Info is part-scoped, not week-scoped.
- Due/Release, horizon, fiscal, density, and presentation changes do not reload PartDetail.
- PartDetail is lazy-loaded and cached by workspace/parent/current MPS snapshot identity.
- Stale-last-good behavior is preserved across relevant refresh/query failure scenarios.
- QAD Part Status is shown as code + backend-owned description.
- Qty On Hand and Qty Non-Net use positive-only, non-RMA nettable/non-nettable inventory rules.
- Pricing uses the most recent `pi_start <= today` and supports one or more MOQ/price tiers.
- Blank/null informational QAD values are acceptable and do not create false error states.

## Stage 6 Verification

- Backend: **316/316 tests passing**.
- Frontend: **119/119 tests passing** after final UI refinements.
- Backend format/build clean.
- Frontend lint/typecheck/build clean.
- Rust/Tauri `cargo check` clean.
- Sidecar rebuilt after backend changes.
- Live read-only QAD validation completed across five available development workspaces and 71 representative parent parts.
- Direct read-only SQL comparisons matched representative PartDetail inventory and pricing results.
- Final Tauri owner-review verified focused-grid spacing, selection-toggle close behavior, Back-to-full-grid behavior, keyboard interaction, and clean shutdown without orphan processes.
- Rare live scenarios not naturally present (multi-tier price breaks, RMA exclusion, no-current-price, additional site/domain) remain covered by deterministic automated tests.

## Stage 6 Durable Artifacts

- `docs/implementation/KST_v2_STAGE_6_PART_INFO_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_6D_IMPLEMENTATION_PROGRESS.md`
- `docs/implementation/KST_v2_STAGE_6_CLOSEOUT.md`
- `KST-v2-Master-Project-Checklist.md`
- `docs/status/CURRENT_PROJECT_STATUS.md`
- updated backend-boundary and API-contract workflow documentation

## Administrative Follow-Up

After committing the accepted Stage 6 changes, record the final Stage 6 commit hash in the checklist/status documentation.

## Next Action

Begin Stage 7 rolling-wave discovery with prototype behavior and scheduler workflow review for Work Orders and Kitting. Keep Stage 7 planning separate from later BOM/component/shortage phases unless evidence demonstrates a required Stage 7 dependency.

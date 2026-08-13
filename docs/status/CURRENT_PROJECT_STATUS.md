# Current Project Status

Date: 2026-08-13
Workstation: Windows (`C:\Dev\kst_v2`)
Current stage: **Stage 7 — Work Orders and Kitting — COMPLETE / ACCEPTED — 2026-08-13**
Stage 7 status: **COMPLETE / ACCEPTED — 2026-08-13** (closeout commit not yet made — working tree still holds the full accumulated Stage 7D changes)
Stage 6 status: **COMPLETE / ACCEPTED — 2026-08-11 — commit `863a638`**
Application version: **`0.1.0-alpha.2`** (see [Versioning Foundation](#versioning-foundation) below)

## Current Position

Stages 1 through 7 are complete and accepted. Stage 7 — Work Orders and Kitting completed its full controlled-checkpoint implementation (7D.0–7D.12), documentation reconciliation (7D.13), and the 7D.14 completion-gate review, and the project owner explicitly accepted Stage 7 on 2026-08-13.

Stage 7 adds MPS-bucket → Work Order drill-down, line-based Kitting %, Work Order material/issue detail, and a bounded (non-pegging) manufactured-subassembly candidate-navigation workflow up to three investigation levels deep, all lazily loaded and cached against the existing MPS snapshot generation.

## Stage 7 Accepted Behavior

- Selecting an MPS parent focuses the grid and opens Part Info; Work Orders/Shortages/Future Shortages/Components tabs remain disabled until a bucket is selected.
- Selecting an eligible MPS week cell or Falldown automatically opens Work Orders. Top-level drill-down is limited to Falldown + the first 6 forward MPS weeks.
- Only `Allocating`/`Frozen`/`Released` work orders receive a Stage 7 card; `RMABOM` work orders are excluded from candidate results.
- WOID (`wo_mstr.wo_lot`) is the scheduler-facing identity; Work Order Number is not unique and is never shown.
- Kitting % is line-based (fully-issued line count / applicable line count), never quantity-weighted; zero applicable lines yields null/N-A, never 0%.
- Manufactured-subassembly candidates use a truthful "Work Orders for `<Component Part>`" navigation model — QAD has no reliable parent↔subassembly WO relationship, and none is fabricated. Candidates are all eligible A/F/R work orders for the component (the original due-date-boundary rule was live-validated as correct, then removed by project-owner decision — see `docs/implementation/STAGE_7_REAL_DATA_VALIDATION.md`).
- Maximum drill depth is 3 levels; lazy-load/cache is keyed by workspace + MPS snapshot generation and is invalidated outright (not stale-fallback) on a new successful MPS refresh.

## Stage 7 Verification

- Backend: **468/468 tests passing**, `dotnet format`/build clean.
- Frontend: **167/167 tests passing**, lint/typecheck/build clean.
- Rust/Tauri `cargo check` clean; sidecar rebuilt after every backend change.
- Live QAD validation (7D.11) across the MSA/Neutronics workspace and component `H06-01-6001-33-1`, matching direct ground-truth SQL exactly.
- Full regression verification (7D.12): all of the above re-confirmed after the due-date-boundary removal and horizontal card-layout change, plus live manual desktop regression (workspace load, MPS load, Part Info, Due/Release toggle, horizon change, Falldown, Work Orders drill-down, single-instance enforcement, clean shutdown) verified directly against the running Tauri app with real QAD data.

## Stage 7 Durable Artifacts

- `docs/implementation/KST_v2_STAGE_7_WORK_ORDER_KITTING_DATA_INVENTORY.md`
- `docs/implementation/KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_7_IMPLEMENTATION_PLAN.md`
- `docs/implementation/STAGE_7_REAL_DATA_VALIDATION.md`
- `KST v2 — Stage 7D Work Orders and Kitting Implementation Checklist.md` (repo root; the authoritative checkpoint-by-checkpoint record)
- `KST-v2-Master-Project-Checklist.md`
- `docs/status/CURRENT_PROJECT_STATUS.md`

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

Stage 6 final commit hash: `863a638` (`feat: complete Stage 6 part information drill-down`).

## Versioning Foundation

Between Stage 6 and Stage 7, a lightweight application versioning foundation was
established (inter-stage housekeeping, not a Stage 7 activity):

- Product identity `KST v2` remains distinct from the semantic application version.
- Current application version: **`0.1.0-alpha.2`** ([SemVer 2.0.0](https://semver.org/)) — bumped from `0.1.0-alpha.1` at Stage 7 closeout (session housekeeping, not a Stage 7 behavior change).
- Authoritative source: `src/backend/Directory.Build.props` (`VersionPrefix`/`VersionSuffix`),
  propagated to the backend (assembly `InformationalVersion`, system status/health endpoints,
  frontend top bar), `src/tauri/Cargo.toml`, `src/frontend/package.json` (full version), and
  `src/tauri/tauri.conf.json` (numeric-only, for MSI/WiX installer compatibility - see
  `docs/development/VERSIONING.md`).
- New tests: 2 backend integration tests (`SystemStatusEndpointTests`), 3
  `Kst.ArchitectureTests` (`VersionConsistencyTests`) guarding future drift. Full backend suite:
  329/329 passing.
- New repeatable sync/check script: `scripts/check-version.ps1` (`-Fix` to auto-correct drift).
- Full documentation: `docs/development/VERSIONING.md`.
- Packaged Windows build (NSIS + MSI) verified successfully with the new version metadata.
- This work does not begin, renumber, or otherwise affect Stage 7.

## Next Action

Stage 7 is complete and accepted. Begin Stage 8 — Component and BOM Detail discovery/planning only when explicitly directed — do not assume Stage 7 acceptance authorizes starting Stage 8 work automatically.

# Current Project Status

Date: 2026-08-21
Workstation: Windows (`C:\Dev\kst_v2`)
Current stage: **Stage 8 — Component and BOM Detail — COMPLETE / ACCEPTED — 2026-08-21**
UI Navigation & Keyboard Ergonomics A: **COMPLETE / ACCEPTED — 2026-08-21**
Active cross-cutting effort: **R0 — Repository / Documentation Reconciliation — COMPLETE /
ACCEPTED — 2026-08-21** (see `R0 — Repository / Documentation Reconciliation Status` below and
`docs/status/R0_REPOSITORY_RECONCILIATION_CLOSEOUT.md`)
Next: **S0 — Security Foundation Integration** (not yet started), then **Stage 9** (not yet started)
Stage 7 status: **COMPLETE / ACCEPTED — 2026-08-13**
Stage 6 status: **COMPLETE / ACCEPTED — 2026-08-11 — commit `863a638`**
Application version: **`0.1.0-alpha.2`** (see [Versioning Foundation](#versioning-foundation) below)

## Current Position

Stages 1 through 8 are complete and accepted, and UI Navigation & Keyboard Ergonomics A is complete
and accepted. The project is not yet working on Stage 9. The current cross-cutting effort is
**R0 — Repository / Documentation Reconciliation**, followed by **S0 — Security Foundation
Integration**; Stage 9 begins only after both are accepted.

## Stage 8 — Component and BOM Detail

**Status:** COMPLETE / ACCEPTED — 2026-08-21

Stage 8 is an **informational Component/BOM investigation capability**. It does not implement
material-requirement netting, shortage classification, or PO coverage.

Accepted behavior includes the current effective multi-level BOM (`ps_mstr`) with structural
hierarchy/order and repeated occurrences preserved, phantoms shown and exploded, BOM search plus
P/M and Phantom filters, the shared Stage 6 Net / Non-Net inventory semantics, a blocking Component
Information modal with selected-site planning fields, Standard Cost (`sct_sim = 'Standard'`, latest
`sct_cst_date`), QCTC (`inp_source = 'qtbom_det'`, latest `inp_start_date`), and Approved Alternates
as the user-facing term (technical `ApprovedVendor` / `vp_mstr` naming is retained).

The following are **intentionally deferred future capability, not unfinished Stage 8 acceptance
requirements**: Show MRP, Inventory / Lot Locations, Extended Requirement, Incoming Supply,
Coverage / Material Status, component MRP / component supply netting, and Future Shortages / PO
coverage.

Full delivered capability, source decisions, and verification evidence (backend 656/656 tests,
frontend 260/260 tests, architecture tests, live QAD validation, owner acceptance) are recorded in
`docs/implementation/KST_v2_STAGE_8_CLOSEOUT.md` — see that document rather than this summary for
detailed acceptance evidence.

## UI Navigation & Keyboard Ergonomics A

**Status:** COMPLETE / ACCEPTED — owner manual validation PASS, 2026-08-21

The accepted interaction convention is **hierarchical Escape unwind** — not browser-history
navigation and not modal-only dismissal:

1. Topmost blocking modal/dialog → close/cancel only that surface.
2. Nested detail in the current investigation → collapse exactly one level.
3. Main MPS detail/drill-down → return to the Part Matrix.
4. Part Matrix/root → do nothing.

Examples: Component Information → BOM; BOM → Part Matrix; nested material/candidate detail → prior
material level; Show Material Lines → Work Order view; Work Order view → Part Matrix; Part Info →
Part Matrix.

Ergonomics B (arrow-key tab navigation, shared focus-trap extraction, menu roving focus, and
additional shortcut hints) remains deferred/planning-only. Full detail, investigation, and the
accepted outcome are recorded in
`docs/implementation/KST_v2_UI_NAVIGATION_KEYBOARD_ERGONOMICS_PLAN.md`.

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
- `docs/reference/KST v2 — Stage 7D Work Orders and Kitting Implementation Checklist.md` (the authoritative checkpoint-by-checkpoint record)
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

## R0 — Repository / Documentation Reconciliation Status

- R0.1 — Read-only repository documentation inventory: **COMPLETE / ACCEPTED**
- R0.2 — Authority and contradiction map: **COMPLETE / ACCEPTED**
- R0.3 — Core project-state reconciliation: **COMPLETE / ACCEPTED**
- R0.4 — Stage-history reconciliation: **COMPLETE / ACCEPTED**
- R0.5 — Architecture and development documentation reconciliation: **COMPLETE / ACCEPTED**
- R0.6 — Data/source documentation reconciliation: **COMPLETE / ACCEPTED**
- R0.7 — Documentation navigation and authority: **COMPLETE / ACCEPTED**
- R0.8 — Reconciliation verification / closeout: **COMPLETE / ACCEPTED**

R0 overall: **COMPLETE / ACCEPTED — 2026-08-21.** Full detail:
`KST-v2-Master-Project-Checklist.md` (`## R0 — Repository / Documentation Reconciliation`) and
`docs/status/R0_REPOSITORY_RECONCILIATION_CLOSEOUT.md`.

## Next Action

Stage 8, UI Navigation & Keyboard Ergonomics A, and R0 — Repository / Documentation
Reconciliation are complete and accepted. The next authorized phase is **S0 — Security
Foundation Integration**, which has not yet started. Stage 9 — Immediate Shortages begins only
after the appropriate S0 foundation work is accepted.

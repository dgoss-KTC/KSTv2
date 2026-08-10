# Current Project Status

Date: 2026-08-10  
Workstation: Windows (`C:\Dev\kst_v2`)  
Current stage: Stage 5B.10 — MPS Dashboard Full Verification, Packaging, and Documentation Closeout  
Stage 5A status: **COMPLETE / ACCEPTED — 2026-08-07**  
Stage 5B.9 status: **COMPLETE / ACCEPTED — 2026-08-10**  
Stage 5B status: **5B.10 VERIFICATION COMPLETE — AWAITING PROJECT-OWNER ACCEPTANCE**

## Current Position

Stages 1–4B and Stage 5A are complete. Stage 5B implementation through checkpoint 5B.9 is complete and owner-accepted. The real-data MPS dashboard is functional, visually reconciled to the scheduler prototype, and validated against live QADPRO2 data and the legacy Excel MPS output.

Stage 5B.10 full regression, Rust/Tauri verification, sidecar rebuild, live desktop verification, and documentation closeout are complete with no regressions found (see below). Do not begin Stage 6 until the project owner reviews this report and accepts Stage 5B.10.

## Stage 5B Verified Baseline

- Real QAD connectivity is verified against `KNWVM13 / QADPRO2` using Windows Integrated authentication.
- QAD reads use `READ UNCOMMITTED` through the centralized QAD connection factory and remain read-only.
- Workspace product-line/range and explicit-parent scope resolve correctly, including KTC and KTV domain mappings validated with live data.
- The direct parameterized MPS query uses work-order-backed `mrp_det` rows, strong MRP-to-WO identity, Closed exclusion, and `RMABOM` exclusion.
- All historical qualifying unfinished work is retained for due-date Falldown; future projection supports the 72-week horizon.
- Due Date / Release Date and horizon changes reproject the existing source snapshot without unnecessary QAD reload.
- A/F/R/Mixed execution states plus Planned and Explicitly Scheduled signals were all validated against live QAD examples.
- Configured parents with no current MPS facts remain visible with descriptions preserved.
- Snapshot refresh retains the last good snapshot on failure and replaces it atomically on success.
- Frontend fiscal calendar, 4-4-5 bands, 53-week exceptions, density preferences, horizon/date-basis persistence, and the MPS grid are implemented.
- The project owner manually compared the 72-week KST v2 MPS grid with the legacy Excel MPS report and confirmed the output looks identical.
- Stage 5B.9 found zero correctness defects.

## Stage 5B.9 Validation Summary

Durable evidence: `STAGE_5B_9_REAL_DATA_VALIDATION.md`.

Highlights:

- 7/7 workspace-level source counts matched direct read-only QAD SQL.
- 31 individually checked WO rows matched identity, quantity, due date, release date, and status.
- Product-line, product-line-range, explicit-parent, large-workspace, KTC, and KTV scope shapes were validated.
- Falldown retained qualifying historical work with no lower date cutoff; Closed and `RMABOM` exclusions were confirmed against live rows.
- Due/Release reprojection preserved total quantity and reused the same snapshot.
- Every required status combination was found in live data and matched the accepted semantics.
- Backend regression: 273/273 tests passed.
- Frontend regression: lint/typecheck/build clean; 101/101 tests passed.

## Known Operational Characteristic

A very large 816-part product-line workspace showed non-linear cold-start database cost: approximately 36 seconds total on the first cold load, compared with approximately 3 seconds for a warm 72-week refresh. The validation found no correctness defect. Carry this forward as an operational/performance characteristic; do not redesign the query without new evidence.

## Stage 5B.10 Closeout Results (2026-08-10)

All automated regression and live-desktop verification passed with zero regressions and zero code defects found. No production code changed during this closeout; only documentation/checklist reconciliation was needed.

- Backend: `dotnet build` clean; `dotnet test Kst.slnx` **273/273 passed** (matches Stage 5B.9 baseline exactly).
- Frontend: `npm run lint` / `npm run typecheck` clean; `npm test` **101/101 passed** (matches Stage 5B.9 baseline exactly); `npm run build` clean.
- Rust/Tauri: `cargo check` and `cargo build` in `src/tauri` both clean.
- Sidecar: rebuilt/republished via `scripts/build-sidecar.ps1` and copied to `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`.
- Live desktop verification via the real `npx @tauri-apps/cli dev` path: sidecar started correctly (Production hosting environment, correct content root), auto-loaded real MPS data for all three configured workspaces, Due/Release and horizon switching reprojected the same snapshot without a new QAD query, an explicit refresh produced a new snapshot ID and updated `lastSuccessfulRefreshAtUtc`, a second app instance was blocked by the single-instance plugin with no duplicate sidecar spawned, and shutdown left no orphan `Kst.Api`/`kst-tauri` process.
- Literal GUI mouse/keyboard interaction with the native desktop window (visual grid check, button clicks, tab navigation) could not be automated in this environment; these UI code paths were unchanged since their Stage 5B.8/5B.9 owner-accepted manual verification, and the API-level checks above exercise the same backend contracts. A final owner click-through spot-check is recommended before acceptance.
- Packaged (MSI/NSIS) verification was not performed: not required by the Stage 5B completion gate, and no installer-affecting code changed.

Durable evidence: `KST_v2_STAGE_5B_IMPLEMENTATION_PLAN.md` (Section 5B.10).

## Durable Stage 5 Artifacts

- `KST_v2_STAGE_5A_MPS_DATA_INVENTORY.md`
- `KST_v2_STAGE_5A_MPS_BACKEND_DATA_CONTRACT.md`
- `KST_v2_STAGE_5A_SNAPSHOT_REFRESH_STRATEGY.md`
- `KST_v2_STAGE_5A_DATABASE_ACCESS_PERFORMANCE_STRATEGY.md`
- `KST_v2_STAGE_5A_MPS_API_SNAPSHOT_CONTRACT.md`
- `KST_v2_STAGE_5A_FISCAL_CALENDAR_STRATEGY.md`
- `KST_v2_STAGE_5B_IMPLEMENTATION_PLAN.md`
- `STAGE_5B_9_REAL_DATA_VALIDATION.md`
- `KST_v2_Master_Project_Checklist_STAGE_5_REVISION.md`
- `KST-v2-Master-Project-Checklist.md`

## Next Action

1. Project owner reviews the Stage 5B.10 closeout report and accepts or requests changes.
2. Only after acceptance, prepare the Stage 6 — Part Information Drill-Down kickoff prompt.

## Previous Verified Foundation

Stage 3 Technical Foundation remains PASS. Stage 4 / 4B application shell, workspace configuration, and workspace scope extension remain the accepted foundation for Stage 5.

Reference historical commits:

- `d0e592b` — initial technical foundation
- `6f5644c` — Stage 3 final closeout
- `ce717a1` — Stage 4 initial implementation slice

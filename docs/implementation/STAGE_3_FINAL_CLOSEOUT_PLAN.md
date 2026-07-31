# Stage 3 Final Closeout Plan

Date: 2026-07-31  
Workspace: C:\Dev\kst_v2  
Scope: Stage 3 technical foundation closeout only (no Phase 1 feature work)

## Objectives

1. Finalize explicit Tauri-owned backend sidecar lifecycle management.
2. Enforce reliable shutdown and orphan-process prevention across success/failure paths.
3. Add single-instance behavior to prevent duplicate sidecar startup.
4. Validate packaged build pipeline with repeatable sidecar publish/copy automation.
5. Reconcile CORS behavior and Stage 3 status/checklist documentation in tracked docs.

## Implementation Steps

1. Tauri lifecycle refactor (`src/tauri/src/lib.rs`)
- Introduce managed backend process state that retains child handle and PID.
- Serialize startup/shutdown transitions to prevent spawn/teardown races.
- Track expected vs unexpected termination and clear state on exit.
- Enforce readiness timeout failure semantics (no false backend-ready emission).
- Emit frontend event for backend termination/unavailability.
- Add explicit app-exit shutdown flow with bounded timeout and forced kill fallback.

2. Single-instance policy (`src/tauri/Cargo.toml`, `src/tauri/src/lib.rs`)
- Add Tauri 2 single-instance plugin dependency.
- Register plugin first.
- On second launch, log event and restore/show/focus existing main window.

3. Frontend status truthfulness (`src/frontend/src/hooks/useBackendStatus.ts`, tests)
- Handle backend termination/unavailability event from Tauri.
- Move UI state from connected to unavailable with reason.
- Add practical test coverage for event-driven degradation behavior.

4. Build/publish automation (`scripts/build-sidecar.ps1`)
- Add deterministic script to publish self-contained single-file backend for `win-x64`.
- Validate output and copy to Tauri binary naming convention.
- Print resolved paths and file size; fail hard on missing/stale outputs.

5. Repository policy files
- Check for root `.editorconfig`, `global.json`, and analyzer configuration.
- Create minimal files only if absent and based on actual installed SDK version.

6. Documentation and tracked status reconciliation
- Update architecture/development/deployment docs to match lifecycle behavior.
- Add/update tracked status artifacts under `docs/status/` for authoritative closeout records.
- Reconcile Stage 3 checklist statuses using required status values.

7. Verification execution
- Run required backend/frontend/tauri/sidecar/packaged build commands.
- Capture pass/fail output and classify any blocked/unverified/manual-gate items.

## Out-of-Scope Guardrails

- No Stage 4 / Phase 1 application-shell or workspace features.
- No customer workspaces, MPS logic, business workflows, exports, or production scheduling features.
- No speculative production CORS broadening.
- No system-wide prerequisite installations without owner approval.

## Completion Criteria for This Task

- Required code/doc changes implemented and compiled.
- Required automated commands run and results recorded from this workstation.
- Manual acceptance checklist prepared and marked as awaiting owner execution unless owner provides results.
- Stage 3 gate status reported as PASS or NOT PASSED based on evidence.

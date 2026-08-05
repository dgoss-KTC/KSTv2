# Current Project Status

Date: 2026-08-10
Workstation: Windows (C:\Dev\kst_v2)
Phase: Stage 4 - Phase 1 Application Shell and Workspace Configuration
Stage 4 / Phase 1: IMPLEMENTATION_COMPLETE_PENDING_OWNER_ACCEPTANCE (third implementation slice complete)

## Stage 4 Current Position

Status: IMPLEMENTATION_COMPLETE_PENDING_OWNER_ACCEPTANCE

Reference implementation commits:
- `ce717a1` - Stage 4 Phase 1 initial slice (application shell, workspace tabs, modal, backend workspace config, local persistence, workspace list/create API, frontend restore, tests)
- Second slice (uncommitted at time of writing) - full workspace lifecycle (edit/archive/restore/delete/reset), confirmation dialogs, toast notifications.
- Third slice (uncommitted at time of writing) - snapshot/data-source lifecycle expansion, refresh coordinator and `POST /api/v1/system/refresh`, local user preferences (theme/accent/density) with `GET`/`PUT /api/v1/preferences`, General workspace tab, `BottomStatusBar`, workspace tab reordering (drag-and-drop + Move Left/Right) via `PUT /api/v1/workspaces/order`, and duplicate-workspace-scope validation.
- Stage 4B slice (uncommitted at time of writing) - Workspace Scope Extension: removed `CustomerNumber` as an authoritative scope field, added optional `ParentParts[]` explicit parent-part-number collection, new scope rule `Site AND (ProductLineFrom OR at least one explicit ParentPart)`, backward-compatible loading of legacy `workspaces.json` files (missing `parentParts` or containing obsolete `customerNumber`), Add/Edit Workspace UI collapsible "Limit to specific parent parts" section, updated duplicate-scope detection and display-name derivation, and full backend/frontend automated test coverage.

Detailed Stage 4 progress and remaining scope are tracked in `docs/status/STAGE_4_PHASE_1_PROGRESS.md`.

## Stage 3 Overall

Status: PASS (previously completed)

Reason:
- Core implementation tasks for sidecar lifecycle ownership, shutdown behavior, orphan prevention paths, termination signaling, single-instance enforcement, sidecar automation, and repo policy files were completed.
- Development manual runtime checks have been completed and recorded.
- Packaged startup verification now succeeded in owner manual testing.
- Packaged shutdown and packaged second-launch verifications are now completed.
- Log review was completed with acceptable results.

## Implementation Highlights Completed

- Tauri host now retains and manages owned backend process handle and PID in runtime state.
- Startup/teardown race prevention added (launch and shutdown guards).
- Readiness timeout now fails closed (no false backend-ready state; failed sidecar is terminated).
- Explicit shutdown with timeout and force-kill fallback for owned PID added to app/window exit paths.
- Unexpected backend termination now clears state and emits frontend lifecycle events.
- Frontend now responds to backend termination/unavailable lifecycle events.
- Single-instance policy added with Tauri single-instance plugin and focus/restore behavior.
- Repeatable sidecar publish/copy script added at scripts/build-sidecar.ps1.
- Root .editorconfig, global.json, and explicit built-in .NET analyzer policy are now present.

## CORS Status

- Development CORS policy remains narrow and explicit.
- No AllowAnyOrigin was introduced.
- Existing CORS integration test is preserved.
- Packaged runtime CORS behavior was manually validated after adding explicit support for `http://tauri.localhost`.

## Manual Validation State

- Development startup/shutdown/crash/second-launch checks: VERIFIED.
	- Startup connected successfully.
	- Normal close removed owned backend process.
	- Forced backend kill moved UI to Backend unavailable with no auto-restart observed.
	- Second launch exited immediately without creating another active backend.
- Packaged startup/shutdown/second-launch checks: PARTIALLY_VERIFIED.
	- Packaged startup verified (NSIS install and launch successful).
	- Packaged shutdown verified (no lingering app/backend processes after close).
	- Packaged second-launch behavior verified (no duplicate active instance).
- Log inspection checklist: VERIFIED (owner reviewed latest logs and reported acceptable results).

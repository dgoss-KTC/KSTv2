# Stage 4 / Phase 1 Progress

Date: 2026-08-10
Status: IMPLEMENTATION_COMPLETE_PENDING_OWNER_ACCEPTANCE
Scope: Initial implementation slice, a second slice adding the full workspace lifecycle (edit/archive/restore/delete/reset), and a third slice adding the snapshot/data-source lifecycle expansion, refresh coordinator, local preferences, General workspace tab, workspace reordering, and duplicate-workspace validation. Owner acceptance is still pending.

## Completed in Initial Slice

Reference commit:
- `ce717a1` - `feat: Stage 4 Phase 1 — application shell, workspace tabs, and backend configuration`

Implemented:
- Persistent top application bar with KST mark, product title, version, backend/configuration status, and theme toggle.
- Workspace tab bar below the top bar.
- Empty opening shell when no saved workspaces exist.
- `+` workspace action and Add Workspace modal.
- Backend-owned workspace configuration model and validation.
- Local JSON persistence for workspaces under `%LOCALAPPDATA%\KST\config\workspaces.json`.
- Corrupt-file recovery by rename-to-invalid and nonfatal empty-state fallback.
- Workspace list and create API operations.
- Frontend load/restore of saved workspace tabs ordered by `sortOrder`.
- Active workspace placeholder with truthful scope-only display (no MPS/business data).
- Automated tests for backend and frontend slice behavior.

## Completed in Second Slice (this change)

Implemented:
- Edit Workspace: `PUT /api/v1/workspaces/{assignmentId}` updates an existing workspace's scope/name/coverage fields while preserving `AssignmentId`, `SortOrder`, and `IsEnabled`.
- Archive Workspace: `POST /api/v1/workspaces/{assignmentId}/archive` sets `IsEnabled=false`; the workspace is removed from the active tab bar but retained in storage.
- Restore Archived Workspace: `POST /api/v1/workspaces/{assignmentId}/restore` sets `IsEnabled=true`, returning the workspace to the active tab bar.
- Permanent Delete: `DELETE /api/v1/workspaces/{assignmentId}` removes the workspace assignment entirely (active or archived).
- Reset All Workspace Configuration: `DELETE /api/v1/workspaces` clears all saved workspaces and returns the shell to the empty startup screen.
- Active-tab fallback: archiving or deleting the active workspace automatically selects the next enabled workspace (by `sortOrder`), falling back to the previous one, or to no selection (empty state) if none remain.
- Frontend: per-tab "⋯" action menu (Edit Workspace / Archive Workspace / Delete Permanently), a "Manage Workspaces" dialog listing Active and Archived workspaces with Restore / Delete Permanently / Reset Workspace Configuration actions, a reusable `ConfirmDialog` for destructive/non-trivial actions, and a toast notification system for success/error feedback.
- `AddWorkspaceDialog` extended with an optional edit mode (prepopulated fields, "Edit Workspace" title, "Save Changes" submit label) without introducing a parallel component.

Explicitly not implemented in this slice (deferred):
- Drag-and-drop tab reordering.
- Any Stage 5 / Phase 2 (MPS Dashboard Grid) functionality.

## Implemented API Surface

- `GET /api/v1/workspaces`
  - Returns normalized saved workspaces plus optional nonfatal configuration warning.
- `POST /api/v1/workspaces`
  - Validates, normalizes, assigns ID and sort order, persists, and returns created workspace.
  - Validation failures return Problem Details.
- `PUT /api/v1/workspaces/{assignmentId}`
  - Validates and updates an existing workspace, preserving its `AssignmentId`, `SortOrder`, and `IsEnabled`. Returns `404` if the ID is unknown; `400` (Problem Details) on validation failure.
- `POST /api/v1/workspaces/{assignmentId}/archive`
  - Sets `IsEnabled=false` and persists. Returns `404` if the ID is unknown.
- `POST /api/v1/workspaces/{assignmentId}/restore`
  - Sets `IsEnabled=true` and persists. Returns `404` if the ID is unknown.
- `DELETE /api/v1/workspaces/{assignmentId}`
  - Permanently removes the workspace assignment. Returns `204` on success, `404` if the ID is unknown.
- `DELETE /api/v1/workspaces`
  - Permanently removes all workspace assignments (idempotent; `204` even when already empty).

## Workspace Validation Rules Implemented

- Site:
  - Required, trimmed, uppercased, exactly 2 letters.
- Customer number:
  - Optional, exactly 8 digits, stored as string.
- Product Line From:
  - Optional, exactly 4 digits, stored as string.
- Product Line To:
  - Optional, requires Product Line From, exactly 4 digits, must be >= Product Line From.
- Scope rule:
  - Site AND (Customer number OR Product Line From).
- Single product-line normalization:
  - If Product Line From is set and Product Line To is blank, Product Line To is normalized to Product Line From.
- Update (edit) uses the same validation rules as create; `AssignmentId`, `SortOrder`, and `IsEnabled` are always preserved regardless of submitted values.

## Explicitly Out of Scope for This Slice

Not implemented in this slice:
- Stage 5 / Phase 2 MPS Dashboard Grid.
- MPS, QAD/shortage query logic, or business-data loading.
- Drag-and-drop workspace reorder.
- Temporary-coverage expiration logic.
- General tab and broader shell controls not required for this slice.

## Automated Verification Snapshot

Backend:
- `dotnet format Kst.slnx --verify-no-changes` PASS
- `dotnet build Kst.slnx --nologo` PASS
- `dotnet test Kst.slnx --nologo` PASS — 112/112 tests passed (3 Domain, 62 Application, 6 ArchitectureTests, 41 Api.IntegrationTests)

Frontend:
- `npm run lint` PASS
- `npm run typecheck` PASS
- `npm test` PASS — 26/26 tests passed
- `npm run build` PASS

Rust/Tauri:
- `cargo check` PASS
- `cargo build` PASS
- Sidecar rebuilt via `scripts/build-sidecar.ps1` and smoke-tested via `npx @tauri-apps/cli dev` (app launched, Vite dev server and Rust shell started without errors).

## Remaining Stage 4 Work

All items in the Stage 4 checklist section are now implemented except owner acceptance (which requires human sign-off and cannot be marked complete by an automated change). The following are intentionally deferred to later rolling-wave phases rather than fabricated:
- Real QAD-backed planner display data, lead-time display data, active-part counts, and shortage counts (require live QAD/shortage adapters introduced in Stage 5 onward).
- Per-source detailed warning/error text (current implementation surfaces a per-source status enum only, not a free-text warning message).
- Any Stage 5 / Phase 2 (MPS Dashboard Grid) functionality — explicitly out of scope and not started.

## Completed in Third Slice (this change)

Backend:
- `SnapshotStatus` (Kst.Domain) expanded from `{NotLoaded, Loading, Loaded, Failed}` to `{NotLoaded, Loading, Current, Stale, Partial, Failed}`; `SnapshotInfo.IsAvailable` now true for `Current`, `Stale`, or `Partial`.
- `DataSourceStatus` (Kst.Application.SystemStatus) expanded to `{NotConfigured, Loading, Current, Stale, Failed, Unavailable}` and is now backed by a stateful `IDataSourceStatusStore` (in-memory) instead of a static list.
- New `Kst.Application.Refresh` namespace: `IRefreshProvider`/`DelegateRefreshProvider` (generic Func-based adapter, keeps Application decoupled from Integrations per the architecture tests), `IRefreshHistoryStore`/`RefreshHistory`, and `RefreshCoordinator`, which runs all registered providers, derives the new snapshot status (Current/Partial/Stale/Failed/NotLoaded), updates data-source statuses, and records attempt/success timestamps.
- `GetSystemStatusQuery`/`SystemStatusResult`/`SystemStatusResponse` extended with `LastRefreshAttemptAt` and `LastSuccessfulRefreshAt`.
- New `POST /api/v1/system/refresh` endpoint invokes `RefreshCoordinator` then returns the same shape as `GET /api/v1/system/status`.
- New `Kst.Domain.Preferences.UserPreferences` (Theme: System/Light/Dark, AccentColor: Blue/Teal/Amber, RowDensity: Compact/Comfortable) with `Kst.Application.Preferences.PreferencesService`/`IPreferencesStore` and `Kst.Infrastructure.Preferences.JsonPreferencesStore` (same corrupt-rename-aside persistence convention as `JsonWorkspaceConfigurationStore`, stored at `%LOCALAPPDATA%\KST\config\preferences.json`).
- New `GET`/`PUT /api/v1/preferences` endpoints with case-insensitive enum validation and Problem Details on invalid values.
- Workspace reorder: `IWorkspaceConfigurationService.ReorderWorkspacesAsync` validates the submitted ID set exactly matches the currently-enabled workspace IDs (no duplicates, no missing/unknown/archived IDs), renumbers enabled workspaces per the given order, and preserves archived workspaces' relative order after them. Exposed via `PUT /api/v1/workspaces/order`.
- Duplicate-scope validation: `CreateWorkspaceAsync`/`UpdateWorkspaceAsync` now reject a workspace whose site/customer number/product-line range matches another currently-enabled workspace (archived workspaces are excluded; a workspace may keep its own scope on update).

Frontend:
- Theme toggle removed from `TopApplicationBar`; the top bar now shows only the KST mark/title/subtitle/version and backend/configuration status.
- New `GeneralWorkspace` component/tab (permanent, non-draggable "General" tab in `WorkspaceTabBar`) with three sections: Appearance (Theme/Accent Color/Row Density segmented controls wired to `usePreferences`), Workspace Management (opens the existing `ManageWorkspacesDialog`), and Application Status (Backend/Snapshot/QAD/Shortage Database/Last successful refresh plus a Refresh button).
- New `BottomStatusBar` component rendered for both the active-workspace view and the General page: a left-aligned Refresh button and right-aligned Backend/Snapshot/Last-successful-refresh labels.
- New `usePreferences` hook (`hooks/usePreferences.ts`) and `api/preferencesApi.ts` load/persist theme, accent color, and row density; the hook is defensive against a malformed/mismatched GET response shape (falls back to local defaults silently) and resolves the effective light/dark theme live via `matchMedia` when the theme preference is `system`.
- `useBackendStatus` gained a `triggerRefresh` function that calls `POST /api/v1/system/refresh` and updates the displayed status from the response (distinct from the existing `refresh`, which only re-fetches `GET /api/v1/system/status`).
- `WorkspaceTabBar` reordering: native HTML5 drag-and-drop on active tabs, plus "Move Left"/"Move Right" items in the existing "⋯" per-tab menu; both paths call the new `reorderWorkspaces` action in `useWorkspaces` (optimistic update, reverts on failure) which calls `PUT /api/v1/workspaces/order`.
- `index.css` gained `--accent-soft` and `--density-row-height`/`--density-control-height`/`--density-spacing` custom properties, plus `[data-accent='teal'|'amber']` and `[data-density='comfortable']` overrides (dark and light theme variants); compact density and blue accent remain the visual defaults (no regression for existing users).
- `ApplicationShell` now sources `data-theme`/`data-accent`/`data-density` from `usePreferences` instead of local component state, and renders `GeneralWorkspace`/`BottomStatusBar` alongside the existing workspace views.

## Automated Verification Snapshot (Third Slice)

Backend:
- `dotnet format Kst.slnx --verify-no-changes` PASS
- `dotnet build Kst.slnx --nologo` PASS
- `dotnet test Kst.slnx --nologo` PASS — 157/157 tests passed (3 Domain, 95 Application, 6 ArchitectureTests, 53 Api.IntegrationTests)

Frontend:
- `npm run lint` PASS
- `npm run typecheck` PASS
- `npm test` PASS — 30/30 tests passed (26 pre-existing + 4 new `GeneralWorkspace.test.tsx` tests)
- `npm run build` PASS

Rust/Tauri:
- `cargo check` PASS
- Sidecar rebuilt via `scripts/build-sidecar.ps1` (backend/API changed)
- `npx @tauri-apps/cli dev` compiled and launched the app shell (`Finished dev profile` / `Running target\debug\kst-tauri.exe`) without Rust-level errors; a full manual click-through of the new General tab/Appearance controls in the running window is recommended as a follow-up since the agent has no GUI interaction capability for the native window.

## OpenAPI / Generated Client

- `docs/openapi/Kst.Api.json` regenerated (via `OpenApiGenerateDocumentsOnBuild=true` on `dotnet build`) and confirmed to include the new preferences and reorder schemas/paths and the extended `SystemStatusResponse`.
- `src/frontend/src/generated/api.ts` regenerated via `npm run generate:types` (never hand-edited).

## Completed in Stage 4B Slice — Workspace Scope Extension (this change)

Scope: Remove `CustomerNumber` as an authoritative workspace-scope field; introduce an optional
`ParentParts[]` explicit-parent-part-number collection. New scope rule:
`Site AND (ProductLineFrom OR at least one explicit ParentPart)`.

Implemented:
- `WorkspaceAssignment` domain record: `CustomerNumber` removed, `IReadOnlyList<string> ParentParts` added.
- New `ParentPartNormalizer` (Domain layer): trims, drops blanks, dedupes ordinally while preserving
  first-occurrence order; `SetEquals` provides order-independent set comparison for duplicate-scope detection.
- `WorkspaceConfigurationService`: validation now requires a product-line range and/or at least one
  parent part; duplicate-scope detection compares Site + ProductLineFrom/To + normalized parent-part set;
  display-name derivation covers PL-only, parts-only (singular/plural), and PL+parts combinations
  (e.g. `"PL 2380 · 3 parts"`, `"1 parent part"`).
- API DTOs/endpoints (`CreateWorkspaceRequestDto`, `WorkspaceAssignmentDto`, `WorkspaceEndpoints`) updated
  to drop `customerNumber` and add `parentParts`.
- `JsonWorkspaceConfigurationStore`: backward compatible with legacy `workspaces.json` files — missing
  `parentParts` normalizes to an empty list on load; obsolete `customerNumber` properties are silently
  ignored by the default JSON deserialization behavior (no migration infrastructure required).
- Frontend: `workspaceApi.ts` (`CreateWorkspaceFields`/`toRequestDto`), `AddWorkspaceDialog` (collapsible
  "Limit to specific parent parts" section with add/remove rows, replacing the customer-number field),
  `ManageWorkspacesDialog` (`describeScope` now reports PL range and/or parent-part count),
  `WorkspacePlaceholder` (parent-parts summary replacing the customer-number block).
- Backend test coverage: `WorkspaceValidationTests`, `WorkspaceReorderAndDuplicateTests`,
  `JsonWorkspaceConfigurationStoreTests` (including new backward-compatibility tests for legacy
  `customerNumber`/missing `parentParts` files), `WorkspaceEndpointTests`, `WorkspaceReorderEndpointTests`,
  and a new `ParentPartNormalizerTests` (Domain.Tests).
- Frontend test coverage: `AddWorkspaceDialog.test.tsx`, `GeneralWorkspace.test.tsx`,
  `WorkspaceLifecycle.test.tsx` updated for the new scope model, plus new parent-part row
  add/remove/expand tests.

Explicitly not implemented in this slice (deferred, per task scope):
- CSV import of parent parts (future work).
- Stage 5A (QAD data investigation) and Stage 5B (MPS implementation).

Verification:
- Backend: `dotnet restore`/`format --verify-no-changes`/`build`/`test` on `Kst.slnx` — all clean
  (188/188 tests passing across `Kst.Domain.Tests`, `Kst.Application.Tests`, `Kst.ArchitectureTests`,
  `Kst.Api.IntegrationTests`).
- Frontend: `npm run generate:types` (confirmed `customerNumber` gone, `parentParts` present),
  `npm run lint`, `npm run typecheck`, `npm test` (31/31 passing), `npm run build` — all clean.
- Rust/Tauri: `cargo check` succeeded (no business-logic changes required in the Tauri shell).
- Sidecar rebuilt via `scripts/build-sidecar.ps1` to pick up the updated backend contract.



# Backend Project Boundaries

## Dependency Rules

```
Kst.Domain
    ↑
Kst.Application
    ↑
Kst.Infrastructure ─── (implements interfaces from Application)
Kst.Integrations.Qad ─── (depends on Domain only)
Kst.Integrations.Shortages ─── (depends on Domain only)
Kst.Exports ─── (depends on Domain only)
    ↑
Kst.Api ─── (depends on Application, Infrastructure, Integrations, Exports)
```

## Projects

### Kst.Domain
- **Purpose:** Pure business concepts and business rules.
- **Must not reference:** ASP.NET Core, Dapper, SQL Server, Infrastructure, Integrations, Exports, or API.
- **Contains:** `IClock`, `SnapshotId`, `SnapshotStatus`, `WorkspaceAssignment`

### Kst.Application
- **Purpose:** Application use cases and orchestration.
- **Must not reference:** ASP.NET Core or SQL Server implementation packages, or any `Kst.Integrations.*`/`Kst.Exports` project.
- **Contains:** `GetSystemStatusQuery`, `ISnapshotStore`, `SnapshotInfo`, `ApplicationInfo`, workspace configuration service and contracts, `IDataSourceStatusStore`, the `Kst.Application.Refresh` namespace (`IRefreshProvider`, `DelegateRefreshProvider`, `RefreshHistory`/`IRefreshHistoryStore`, `RefreshCoordinator`), and the `Kst.Application.Preferences` namespace (`IPreferencesStore`, `IPreferencesService`, `PreferencesService`)

### Kst.Infrastructure
- **Purpose:** Shared technical implementations.
- **Contains:** `SystemClock`, `InMemorySnapshotStore`, `LocalAppDataPaths`, `ApplicationInstanceId`, JSON workspace configuration store, `InMemoryDataSourceStatusStore`, `InMemoryRefreshHistoryStore`, `JsonPreferencesStore`

### Kst.Integrations.Qad
- **Purpose:** QAD ERP database integration boundary.
- **Future:** Will use `Microsoft.Data.SqlClient`, Dapper, Windows-integrated auth, explicit SQL adapters.
- **Now:** `QadConnectionOptions`, `IQadConnectivityCheck`, `DisabledQadConnectivityCheck`

### Kst.Integrations.Shortages
- **Purpose:** Internal shortage database integration boundary.
- **Now:** `ShortagesConnectionOptions`, `IShortagesConnectivityCheck`, `DisabledShortagesConnectivityCheck`

### Kst.Exports
- **Purpose:** Export service boundary.
- **Future:** Excel, CSV, QXtend file exports.
- **Now:** `IExportService`, `PlaceholderExportService`

### Kst.Api
- **Purpose:** ASP.NET Core local API.
- **Responsibilities:** DI wiring, endpoint definitions, DTO mapping, OpenAPI, logging setup.
- **Binds to:** `127.0.0.1` only (loopback).

## Stage 4 Workspace Configuration Boundary

- Workspace business rules and validation are backend-owned and implemented in `Kst.Application` (`WorkspaceConfigurationService`), including create, update (edit), archive, restore, delete, reset-all, and reorder operations, plus duplicate-scope validation (rejects a new/edited enabled workspace whose site/customer number/product-line range matches another currently-enabled workspace).
- Local file persistence is infrastructure-owned and implemented in `Kst.Infrastructure` using `LocalAppDataPaths`; every lifecycle operation (edit/archive/restore/delete/reset/reorder) persists atomically through the same `IWorkspaceConfigurationStore` used by create.
- HTTP request/response mapping and Problem Details formatting are API-owned in `Kst.Api`; update reuses `CreateWorkspaceRequestDto` rather than introducing a parallel DTO, and archive/restore/delete/reset/reorder are thin endpoint handlers with no business logic.
- Frontend owns presentation state and modal interaction only (tab action menu, Manage Workspaces dialog, confirmation dialogs, toasts, active-tab fallback selection, drag-and-drop/Move Left/Right reorder gestures); it does not read or write workspace files directly.

## Stage 4 Refresh and Data-Source Status Boundary

- Snapshot lifecycle (`Kst.Domain.Snapshots.SnapshotStatus`: NotLoaded/Loading/Current/Stale/Partial/Failed) and data-source status (`Kst.Application.SystemStatus.DataSourceStatus`: NotConfigured/Loading/Current/Stale/Failed/Unavailable) are domain/application-owned concepts; `IDataSourceStatusStore` replaces the earlier static data-source list with a stateful store.
- `Kst.Application.Refresh.RefreshCoordinator` orchestrates a full refresh cycle (mark snapshot Loading, run all registered `IRefreshProvider`s, derive the new snapshot status, update data-source statuses, record attempt/success timestamps) without depending on any concrete integration. `IRefreshProvider` is a minimal `(SourceName, RefreshAsync)` contract; `DelegateRefreshProvider` is a generic `Func`-based adapter used so `Kst.Api` (the composition root) can wire concrete QAD/Shortage connectivity checks as refresh providers without `Kst.Application` ever referencing `Kst.Integrations.*` (this pattern exists specifically to satisfy `Kst.ArchitectureTests`).
- `POST /api/v1/system/refresh` is a thin `Kst.Api` endpoint that calls `RefreshCoordinator` then reuses the same `GetSystemStatusQuery` response mapping as `GET /api/v1/system/status`.

## Stage 4 Local Preferences Boundary

- `Kst.Domain.Preferences.UserPreferences` (Theme: System/Light/Dark, AccentColor: Blue/Teal/Amber, RowDensity: Compact/Comfortable) is a pure domain record with a `Default` value.
- `Kst.Application.Preferences.PreferencesService` validates and persists preference updates (case-insensitive enum parsing with Problem Details-friendly validation errors) through `IPreferencesStore`.
- `Kst.Infrastructure.Preferences.JsonPreferencesStore` persists to `%LOCALAPPDATA%\KST\config\preferences.json` using the same camelCase JSON, temp-file-and-move, and corrupt-file-rename-aside conventions as `JsonWorkspaceConfigurationStore`.
- `GET`/`PUT /api/v1/preferences` in `Kst.Api` map `UserPreferences` to/from `UserPreferencesDto`; preferences are local-only application state, not synchronized with QAD or any external system.

## Stage 6 Part Detail Boundary

- Pure business pieces are Domain-owned: `Kst.Domain.PartDetail.PartPriceBreak` (MOQ/unit-price tier), `Kst.Domain.PartDetail.PartStatusMap` (QAD Part Status code → description lookup, never throws for unknown/blank codes), and `Kst.Domain.PartDetail.PartDetailSourceFacts` (the raw QAD-crossing record analogous to `Kst.Application.Mps.MpsSourceRow`).
- The composed, cache-metadata-bearing `Kst.Application.PartDetail.PartDetail` record (adds `LoadedAtUtc`/`IsStale`/`Warning` to the source facts) is Application-owned, mirroring `MpsSnapshot`'s placement rather than `Kst.Domain` — orchestration/cache state is consistently kept out of Domain in this codebase.
- `Kst.Application.PartDetail.PartDetailService` is the single orchestrator: it never triggers an MPS auto-load (reads `IMpsSnapshotStore.GetState` only), validates the requested part is in the workspace's already-resolved MPS scope, and serves from `IPartDetailCacheStore` (`Kst.Infrastructure.PartDetail.InMemoryPartDetailCacheStore`, keyed by `(WorkspaceId, ParentPart)` and tagged with the MPS `SnapshotId` it was loaded against) whenever the cache still matches the current snapshot.
- `Kst.Integrations.Qad.PartDetail.QadPartDetailReader` follows the same adapter-bridging pattern as `QadMpsSourceReader`: `IPartDetailSourceReader`/`DelegatePartDetailSourceReader` live in `Kst.Application`, the concrete QAD-backed class lives in `Kst.Integrations.Qad` with no back-reference, and `Kst.Api/Program.cs` wires the two together as the composition root. It issues three parameterized, `READ UNCOMMITTED` queries per part (via `QadConnectionFactory.OpenAsync`) — part master (`pt_mstr`), on-hand inventory split into nettable/non-nettable via `is_mstr.is_nettable` (excluding RMA lots and non-positive quantities), and the current price list's tiers (`pi_mstr`/`pid_det`, latest list whose `pi_start <= today`).
- `GET /api/v1/workspaces/{assignmentId}/part-detail?partNumber=...` in `Kst.Api` maps `PartDetailService`'s outcome to HTTP: 200 (loaded, possibly stale-with-warning), 404 (unknown workspace, part out of MPS scope, or no QAD part-master record — the latter carries the exact Problem Details title `"Part not found"` that the frontend uses to distinguish it from other errors), 409 (MPS not loaded yet), 503 (QAD unavailable and no cached fallback exists), 400 (blank `partNumber`).


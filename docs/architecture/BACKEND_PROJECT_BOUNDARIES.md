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
- **Contains:** `IClock`, `SnapshotId`, `SnapshotStatus`

### Kst.Application
- **Purpose:** Application use cases and orchestration.
- **Must not reference:** ASP.NET Core or SQL Server implementation packages.
- **Contains:** `GetSystemStatusQuery`, `ISnapshotStore`, `SnapshotInfo`, `ApplicationInfo`

### Kst.Infrastructure
- **Purpose:** Shared technical implementations.
- **Contains:** `SystemClock`, `InMemorySnapshotStore`, `LocalAppDataPaths`, `ApplicationInstanceId`

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

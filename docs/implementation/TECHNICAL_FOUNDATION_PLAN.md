# KST v2 Technical Foundation – Implementation Plan

## Architecture Decisions

### Port Handshake
**Decision: Backend writes startup JSON to stdout; Tauri reads and parses it.**

Rationale: Avoids TOCTOU race condition of pre-selecting a port. The backend binds to
`127.0.0.1:0`, letting the OS assign a free port, then immediately writes a JSON line
to stdout before accepting requests. Tauri reads the process stdout line, extracts the
port, then polls `GET /health` until ready or timeout expires.

Startup JSON format (single line, terminated with newline):
```json
{"port":PORT,"instanceId":"GUID","status":"starting"}
```

### Frontend Framework
React 19 + TypeScript + Vite. Standard Tauri 2 setup.

### OpenAPI / TypeScript Client
- `Microsoft.AspNetCore.OpenApi` (built-in .NET 10) for OpenAPI spec generation
- `Microsoft.Extensions.ApiDescription.Server` for build-time spec export to file
- `openapi-typescript` npm package to generate `src/frontend/src/generated/api.ts`
- Thin `ApiClient` wrapper in `src/frontend/src/api/client.ts` uses generated types

### CSS Strategy
Plain CSS with CSS custom properties. No framework dependencies.

### Architecture Test Framework
`NetArchTest.Rules` for .NET architecture boundary enforcement.

### Logging
Serilog with console + rolling file sinks. Log directory: `%LOCALAPPDATA%\KST\logs\`.

### Tauri Version
Tauri 2.x (tauri-cli 2.11.4 available).

## Project Dependency Rules

```
Kst.Domain          → (no project references)
Kst.Application     → Kst.Domain
Kst.Infrastructure  → Kst.Domain, Kst.Application (interfaces)
Kst.Integrations.*  → Kst.Domain
Kst.Exports         → Kst.Domain
Kst.Api             → Kst.Application, Kst.Infrastructure, Kst.Integrations.*, Kst.Exports
```

## File Layout

```
src/
├── frontend/
│   ├── src/
│   │   ├── generated/          # DO NOT EDIT - openapi-typescript output
│   │   ├── api/                # Thin typed client wrapper
│   │   ├── components/         # React components
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── tauri/
│   ├── src/
│   │   ├── main.rs
│   │   └── lib.rs
│   ├── capabilities/
│   │   └── default.json
│   ├── Cargo.toml
│   ├── build.rs
│   └── tauri.conf.json
└── backend/
    ├── Kst.sln
    ├── Directory.Build.props
    ├── Directory.Packages.props
    ├── Kst.Domain/
    ├── Kst.Application/
    ├── Kst.Infrastructure/
    ├── Kst.Integrations.Qad/
    ├── Kst.Integrations.Shortages/
    ├── Kst.Exports/
    ├── Kst.Api/
    └── tests/
        ├── Kst.ArchitectureTests/
        ├── Kst.Api.IntegrationTests/
        ├── Kst.Application.Tests/
        └── Kst.Domain.Tests/
```

## Implementation Order
1. Backend solution scaffold (solution + all projects)
2. Domain, Application, Infrastructure layer implementation
3. Integration boundary projects
4. API project with endpoints
5. Tests
6. Frontend + Tauri scaffold
7. OpenAPI generation + TypeScript client
8. Frontend implementation
9. Frontend tests
10. Documentation
11. Verification

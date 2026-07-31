# KST v2 Technical Foundation – Implementation Plan

## Architecture Decisions

### Port Handshake
**Decision: Backend writes startup JSON to stdout; Tauri reads and parses it.**

Rationale: Avoids TOCTOU race condition of pre-selecting a port. The backend binds to
`127.0.0.1:0`, letting the OS assign a free port, then immediately writes a JSON line
to stdout before accepting requests. Tauri reads the process stdout line, extracts the
port, then polls `GET /ready` until ready or timeout expires.

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

### Frontend Backend URL Resolution and Retry
- Frontend resolves backend URL by querying Tauri host (`get_backend_url`) and caching the latest URL.
- Frontend startup/retry polling continues until connected.
- Event-based update (`backend-ready`) is used when available, with polling as resilient fallback.
- Non-Tauri and test contexts retain static fallback behavior.

### Development CORS Requirement
Frontend and backend use different local origins in development (different ports/schemes).
Backend must allow intended frontend origins through a narrowly scoped CORS policy.

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
    ├── Kst.slnx
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

## Implementation Note (2026-07-31)

Integration troubleshooting in Tauri development mode confirmed a real cross-origin requirement.

- Backend and frontend run on separate local origins in dev (`localhost:1420` and `127.0.0.1:<dynamic-port>`).
- Backend startup and HTTP 200 logs alone are not sufficient to prove frontend consumption.
- A backend CORS policy was required for intended local origins before frontend connection stabilized.
- Sidecar refresh workflow is required after backend changes:
    1. publish `Kst.Api`
    2. copy to `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`
    3. rerun `npx @tauri-apps/cli dev`

## Stage 3 Closeout Note (2026-07-31)

- Tauri host now retains explicit ownership of the active backend child process handle and PID in managed state.
- Readiness timeout no longer emits a false-ready signal; failed sidecars are terminated and reported unavailable.
- Backend termination after readiness is propagated to frontend via `backend-terminated` / `backend-unavailable` events.
- App exit path now performs explicit shutdown with timeout and forced-kill fallback for the owned PID.
- Single-instance policy is enforced through the Tauri 2 single-instance plugin.
- Sidecar publication/copy is automated with `scripts/build-sidecar.ps1`.

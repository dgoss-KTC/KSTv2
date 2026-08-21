# Build and Test

Security-relevant changes must also satisfy the repository security policy (`SECURITY.md`,
`docs/security/`). Automated security baseline/release gates are introduced by later S0 work and
must not be assumed to exist yet.

## Stage 3 Required Verification Sequence

Run this exact sequence after lifecycle or packaging changes.

```powershell
cd src/backend
dotnet restore Kst.slnx
dotnet format Kst.slnx --verify-no-changes
dotnet build Kst.slnx --nologo
dotnet test Kst.slnx --nologo --logger "console;verbosity=detailed"

cd ../frontend
npm install
npm run generate:types
npm run lint
npm run typecheck
npm test
npm run build

cd ../tauri
cargo check
cargo build

cd ../..
.\scripts\build-sidecar.ps1

cd src/tauri
npx @tauri-apps/cli build
```

## .NET Backend

```powershell
cd src/backend

# Restore
dotnet restore Kst.slnx

# Build (also regenerates OpenAPI spec)
dotnet build Kst.slnx

# Run all tests
dotnet test Kst.slnx

# Run specific test project
dotnet test tests/Kst.Api.IntegrationTests

# Start backend (port auto-assigned, logs handshake to stdout)
dotnet run --project Kst.Api/Kst.Api.csproj

# Start backend on specific port
dotnet run --project Kst.Api/Kst.Api.csproj -- --port 15402

# Publish self-contained win-x64 single-file
dotnet publish Kst.Api/Kst.Api.csproj `
  -c Release -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:PublishTrimmed=false `
  -o ../../publish/backend
```

## Frontend

```powershell
cd src/frontend

# Install
npm install

# Type check
npm run typecheck

# Lint
npm run lint

# Run tests
npm test

# Run tests in watch mode
npm run test:watch

# Build
npm run build

# Generate TypeScript types from OpenAPI spec
npm run generate:types
```

`generate:types` resolves to `../../docs/openapi/Kst.Api.json` from `src/frontend`.

## Tauri

```powershell
cd src/tauri

# Build Rust code (debug)
cargo build

# Build release
cargo build --release

# Start dev mode (spins up frontend dev server + Tauri window)
npx @tauri-apps/cli dev

# Build packaged installer
npx @tauri-apps/cli build
```

## Canonical Dev Rebuild Sequence (After Backend Changes)

Rebuilding `Kst.Api` alone does not update the executable launched by Tauri.
The published sidecar in `src/tauri/binaries` must also be replaced.

1. Build and test backend.
2. Publish/copy backend sidecar binary with script.
4. Run frontend checks.
5. Run Rust check/build.
6. Start Tauri development mode.

```powershell
cd src/backend
dotnet build Kst.slnx --nologo
dotnet test Kst.slnx --nologo

cd ../..
.\scripts\build-sidecar.ps1

cd src/frontend
npm run typecheck
npm run lint
npm test
npm run build

cd ../tauri
cargo check
npx @tauri-apps/cli dev
```

## All Tests (combined)

```powershell
# Backend
cd src/backend ; dotnet test Kst.slnx

# Frontend
cd src/frontend ; npm test
```

## Test Coverage Summary

| Suite | Tests | Coverage area |
|---|---|---|
| `Kst.Domain.Tests` | 3 | SnapshotId value type |
| `Kst.Application.Tests` | 14 | SnapshotStore, SnapshotInfo, SystemStatus use case |
| `Kst.ArchitectureTests` | 6 | Project dependency rules |
| `Kst.Api.IntegrationTests` | 15 | All endpoints, camelCase, DTOs, CORS origin behavior |
| Frontend | 7 | UI states, retry, refresh, data rendering |

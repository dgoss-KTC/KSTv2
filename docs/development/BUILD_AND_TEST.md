# Build and Test

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
| `Kst.Api.IntegrationTests` | 14 | All endpoints, camelCase, DTOs |
| Frontend | 7 | UI states, retry, refresh, data rendering |

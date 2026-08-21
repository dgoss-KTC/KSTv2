# KST v2 – Keytronic Scheduler's Toolbox

A modern Windows 11 desktop application for production scheduling.

## Documentation Map

| Question | Start here | Purpose |
|---|---|---|
| What must I obey? | [AGENTS.md](AGENTS.md) | Repository rules and agent operating constraints |
| What security rules apply? | [SECURITY.md](SECURITY.md) | Security entry point; detailed policy under `docs/security/` |
| Where are we now? | [docs/status/CURRENT_PROJECT_STATUS.md](docs/status/CURRENT_PROJECT_STATUS.md) | Short canonical current-project state |
| What is the roadmap? | [KST-v2-Master-Project-Checklist.md](KST-v2-Master-Project-Checklist.md) | Canonical project roadmap/checklist |
| What is the current architecture? | [docs/architecture/](docs/architecture/) — start with [Technical Foundation](docs/architecture/TECHNICAL_FOUNDATION.md) and [Backend Project Boundaries](docs/architecture/BACKEND_PROJECT_BOUNDARIES.md) | Current accepted architecture |
| Where are the QAD/source mappings? | [docs/data/qadpro2-data-map.md](docs/data/qadpro2-data-map.md) | Canonical QAD schema/source map (the `.yaml`/`.json` representations are kept in sync alongside it) |
| What proves a stage was actually implemented? | Accepted closeouts, contracts, and validation reports under `docs/implementation/` (for example `KST_v2_STAGE_8_CLOSEOUT.md`) | Accepted implementation evidence for completed stages |
| What is historical/reference material? | [docs/reference/](docs/reference/) | Reference/provenance material (original charter, superseded checklists, prototypes). Authority is determined by a document's stated role, not by being in this folder — see `AGENTS.md` §1. |

For the full documentation authority model (which document wins when material appears to disagree), see `AGENTS.md` §1.

## Architecture

KST v2 is a desktop application with three layers:

| Layer | Technology | Responsibility |
|---|---|---|
| **Frontend** | React 19 + TypeScript + Vite | UI rendering, interaction state, API calls |
| **Desktop host** | Tauri 2 + Rust | Process management, lifecycle, permissions |
| **Backend** | C# / .NET 10 / ASP.NET Core | Business logic, data, local API |

The backend runs as a Tauri **sidecar** — a managed subprocess that starts with the application and stops when it exits.

## Repository Structure

```
src/
├── frontend/          # React + TypeScript + Vite
├── tauri/             # Rust / Tauri desktop host
└── backend/
    ├── Kst.slnx
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
docs/
├── architecture/
├── development/
├── deployment/
└── implementation/
```

## Quick Start (Development)

See [docs/development/SETUP.md](docs/development/SETUP.md) for prerequisites and first-time setup.

```powershell
# 1. Restore and build .NET backend
cd src/backend
dotnet restore Kst.slnx
dotnet build Kst.slnx

# 2. Run tests
dotnet test Kst.slnx

# 3. Start the backend alone (for frontend development)
dotnet run --project Kst.Api/Kst.Api.csproj -- --port 15402

# 4. Install frontend dependencies
cd ../frontend
npm install

# 5. Generate TypeScript types from OpenAPI
npm run generate:types

# 6. Start the Tauri dev app (starts everything)
cd ../tauri
npx @tauri-apps/cli dev
```

## Key Commands

See [docs/development/BUILD_AND_TEST.md](docs/development/BUILD_AND_TEST.md) for the full command reference.

## API Endpoints

| Endpoint | Description |
|---|---|
| `GET /health` | Backend liveness check |
| `GET /ready` | Backend readiness check |
| `GET /api/v1/system/status` | Typed system status for frontend |
| `GET /openapi/v1.json` | OpenAPI specification |

## Documentation Index

- [Security](SECURITY.md) — entry point; policy area under [docs/security/](docs/security/)
- [Current Project Status](docs/status/CURRENT_PROJECT_STATUS.md)
- [Master Project Checklist](KST-v2-Master-Project-Checklist.md)
- [QAD Data Map](docs/data/qadpro2-data-map.md)
- [Technical Foundation](docs/architecture/TECHNICAL_FOUNDATION.md)
- [Backend Project Boundaries](docs/architecture/BACKEND_PROJECT_BOUNDARIES.md)
- [Sidecar Lifecycle](docs/architecture/SIDECAR_LIFECYCLE.md)
- [API Contract Workflow](docs/architecture/API_CONTRACT_WORKFLOW.md)
- [Development Setup](docs/development/SETUP.md)
- [Build and Test](docs/development/BUILD_AND_TEST.md)
- [Troubleshooting](docs/development/TROUBLESHOOTING.md)
- [OpenAPI Client Generation](docs/development/OPENAPI_CLIENT_GENERATION.md)
- [Windows Packaging](docs/deployment/WINDOWS_PACKAGING.md)

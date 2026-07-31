# Development Setup

## Prerequisites

| Tool | Required Version | Installation |
|---|---|---|
| .NET SDK | 10.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18+ | https://nodejs.org/ |
| Rust + Cargo | 1.77+ | https://rustup.rs/ |
| Tauri CLI | 2.x | `npm install -g @tauri-apps/cli` |

### Windows-specific (for building)

- Visual Studio Build Tools 2022 (C++ workload)
- WebView2 Runtime (installed automatically on Windows 11)

## First-Time Setup

```powershell
# 1. Clone or open the repository
cd C:\Dev\kst_v2

# 2. Restore .NET dependencies
cd src/backend
dotnet restore Kst.slnx

# 3. Build the backend
dotnet build Kst.slnx

# 4. Install frontend dependencies
cd ../frontend
npm install

# 5. Generate TypeScript API types
npm run generate:types

# 6. Install Tauri Rust dependencies (first build only — takes several minutes)
cd ../tauri
cargo build
```

## Running in Development

### Port Model (Important)

- `http://localhost:1420` is the frontend Vite development server.
- The .NET sidecar backend binds to a separate dynamic loopback port, for example `http://127.0.0.1:62115`.
- Different frontend and backend ports are expected in development.
- Do not treat this as a port mismatch.

### Backend only (for testing endpoints directly)

```powershell
cd src/backend
dotnet run --project Kst.Api/Kst.Api.csproj -- --port 15402
# Endpoints available at http://127.0.0.1:15402
```

### Full app (frontend + backend via Tauri)

```powershell
# Ensure the backend sidecar exists at:
# src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe
#
# If backend source changed, rebuild sidecar first:
cd C:\Dev\kst_v2
.\scripts\build-sidecar.ps1

# Then from the tauri directory:
cd src/tauri
npx @tauri-apps/cli dev
```

### Expected Startup Sequence

1. Vite starts on `http://localhost:1420`.
2. Tauri launches the desktop host.
3. Tauri starts the `Kst.Api` sidecar.
4. Backend binds to `http://127.0.0.1:<dynamic-port>` and prints startup handshake JSON.
5. Tauri reads the handshake and polls `GET /ready`.
6. Tauri exposes the active backend URL to the frontend.
7. Frontend fetches `GET /api/v1/system/status`.
8. UI transitions to Connected and displays backend status fields.

Expected connected-state fields include:

- Application name and version
- Backend framework
- Backend instance ID
- Started-at timestamp and current backend time
- Snapshot availability/status
- Data source status entries

### DEV Window Identification

- Use the `[DEV]` title marker to confirm you are in the Tauri development window.
- Do not confuse installed app windows with the development window.

### Single-Instance Behavior

- Only one KST app instance runs at a time.
- Launching KST a second time keeps the first instance running.
- Existing main window is restored/focused and second launch exits without spawning another backend.

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Production` |
| `ASPNETCORE_URLS` | Override binding URL | `http://127.0.0.1:0` |
| `KST_PORT` | Override backend port | 0 (OS-assigned) |
| `VITE_BACKEND_URL` | Override backend URL for frontend dev | `http://127.0.0.1:15402` |

## Log Location

Logs are written to: `%LOCALAPPDATA%\KST\logs\kst-YYYYMMDD.log`

On Windows: `C:\Users\<username>\AppData\Local\KST\logs\`

## Troubleshooting Process Cleanup (Intentional, Forced)

Use only while troubleshooting stale or orphaned processes. This is not normal shutdown behavior.

```powershell
Get-Process kst-tauri,KST,Kst.Api -ErrorAction SilentlyContinue

Get-Process kst-tauri,KST,Kst.Api -ErrorAction SilentlyContinue |
	Stop-Process -Force
```

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for the full "Backend unavailable" checklist.

Normal app shutdown now uses explicit owned sidecar termination with timeout and forced-kill fallback for the owned PID only.

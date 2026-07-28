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

### Backend only (for testing endpoints directly)

```powershell
cd src/backend
dotnet run --project Kst.Api/Kst.Api.csproj -- --port 15402
# Endpoints available at http://127.0.0.1:15402
```

### Full app (frontend + backend via Tauri)

```powershell
# Ensure the backend binary is in the Tauri binaries directory:
# src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe

# Then from the tauri directory:
cd src/tauri
npx @tauri-apps/cli dev
```

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

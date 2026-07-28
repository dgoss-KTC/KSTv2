# Windows Packaging

## Overview

KST v2 is packaged as a Windows installer using Tauri's built-in bundler.
The backend is embedded as a self-contained win-x64 single-file executable.

## Prerequisites

- Windows 11
- Rust toolchain (stable)
- .NET 10 SDK
- Node.js 18+
- Tauri CLI: `npm install -g @tauri-apps/cli`
- Visual Studio Build Tools 2022 (C++ Desktop workload)

## Build Steps

### 1. Publish the .NET Backend

```powershell
cd src/backend
dotnet publish Kst.Api/Kst.Api.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:PublishTrimmed=false `
  /p:PublishAot=false `
  -o ../../publish/backend
```

Output: `publish/backend/Kst.Api.exe` (~100MB, includes .NET runtime)

### 2. Copy Backend to Tauri Binaries

```powershell
Copy-Item publish/backend/Kst.Api.exe `
  src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe
```

The platform triple suffix (`-x86_64-pc-windows-msvc`) is required by Tauri's external binary system.

### 3. Build Frontend

```powershell
cd src/frontend
npm run build
```

### 4. Package with Tauri

```powershell
cd src/tauri
npx @tauri-apps/cli build
```

Output: `src/tauri/target/release/bundle/`
- `msi/` — MSI installer
- `nsis/` — NSIS installer

## Publication Settings

| Setting | Value |
|---|---|
| Target Framework | `net10.0` |
| Runtime Identifier | `win-x64` |
| Self-Contained | `true` |
| PublishSingleFile | `true` |
| PublishTrimmed | `false` (trimming not used) |
| PublishAot | `false` (Native AOT not used) |

**Note on Single-File:** Serilog, ASP.NET Core, and all other packages used are
compatible with single-file deployment. `PublishTrimmed=false` is intentional —
trimming was not enabled because it would require extensive testing of all
reflection-based features in the stack.

## Sidecar Registration

The backend is registered in `src/tauri/tauri.conf.json`:
```json
"bundle": {
  "externalBin": ["binaries/Kst.Api"]
}
```

At build time, Tauri looks for `binaries/Kst.Api-x86_64-pc-windows-msvc.exe`.
At runtime, the backend is extracted to a temp directory and spawned.

## Troubleshooting

| Problem | Solution |
|---|---|
| `icon.ico not found` | Ensure `src/tauri/icons/icon.ico` exists |
| Backend binary missing | Run the publish step and copy to `binaries/` |
| Tauri build fails on WebView2 | Install [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) |
| Backend won't start | Check `%LOCALAPPDATA%\KST\logs\` for error logs |

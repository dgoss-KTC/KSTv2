# Windows Packaging

## Overview

KST v2 is packaged as a Windows installer using Tauri's built-in bundler.
The backend is embedded as a self-contained win-x64 single-file executable.

Important:
- Backend networking and CORS changes require a new backend publish and sidecar copy before packaging.
- Development-mode CORS origins and packaged-runtime origins must be reviewed separately.

## Prerequisites

- Windows 11
- Rust toolchain (stable)
- .NET 10 SDK
- Node.js 18+
- Tauri CLI: `npm install -g @tauri-apps/cli`
- Visual Studio Build Tools 2022 (C++ Desktop workload)

## Build Steps

### 1. Publish and Copy the .NET Sidecar

```powershell
cd C:\Dev\kst_v2
.\scripts\build-sidecar.ps1
```

Output includes:

- `publish/backend-sidecar/Kst.Api.exe`
- `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`

The platform triple suffix (`-x86_64-pc-windows-msvc`) is required by Tauri's external binary system.

If backend code changes (including CORS policy changes), rerun the script so the sidecar binary matches current source.

### 2. Build Frontend

```powershell
cd src/frontend
npm run build
```

### 3. Package with Tauri

```powershell
cd src/tauri
npx @tauri-apps/cli build
```

Output: `src/tauri/target/release/bundle/`
- `msi/` — MSI installer
- `nsis/` — NSIS installer

Record bundle paths, bundle types, and file sizes after each packaging run.

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

## CORS Policy Scope

- Current development verification uses a narrowly scoped CORS policy for known local origins.
- Do not broaden to `AllowAnyOrigin` without an explicit security decision.
- Packaged runtime origin behavior requires separate verification and should not be inferred from development-only runs.

## Verification Scope Note

The troubleshooting and connectivity fixes recorded in this repository were verified in Tauri development mode.
Packaged installer build and packaged runtime behavior should be tracked as separate verification items.

## Troubleshooting

| Problem | Solution |
|---|---|
| `icon.ico not found` | Ensure `src/tauri/icons/icon.ico` exists |
| Backend binary missing | Run the publish step and copy to `binaries/` |
| Tauri build fails on WebView2 | Install [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) |
| Backend won't start | Check `%LOCALAPPDATA%\KST\logs\` for error logs |

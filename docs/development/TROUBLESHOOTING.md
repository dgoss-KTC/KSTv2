# Development Troubleshooting

## App opens but shows "Backend unavailable"

Follow these checks in order.

1. Confirm the window is the development app.
- Title should include `[DEV]`.
- If title does not include `[DEV]`, close it and relaunch from `src/tauri` with `npx @tauri-apps/cli dev`.

2. Confirm the `tauri dev` terminal is still active.
- If the terminal exits, the Tauri window likely closed and sidecar lifecycle changed.

3. Confirm process count is sane.
- Expect one `kst-tauri` process and one `Kst.Api` process for a clean dev run.

```powershell
Get-Process kst-tauri,KST,Kst.Api -ErrorAction SilentlyContinue
```

4. Confirm sidecar file exists at the target-qualified path.
- `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`

5. Confirm backend is listening on loopback.
- Expected address pattern: `127.0.0.1:<dynamic-port>`

```powershell
Get-NetTCPConnection -State Listen |
  Where-Object { $_.OwningProcess -in (Get-Process Kst.Api -ErrorAction SilentlyContinue).Id }
```

6. Confirm port expectations.
- `1420` is Vite frontend dev server.
- Backend sidecar uses a different dynamic loopback port.
- Different ports are expected and correct.

7. Check logs.
- `%LOCALAPPDATA%\KST\logs\`

8. In logs, verify these events.
- Backend startup line
- Handshake and listening URL
- `/ready` success
- `/api/v1/system/status` requests
- CORS policy success or failure lines

9. If backend source changed, republish and copy sidecar.

```powershell
cd C:\Dev\kst_v2
.\scripts\build-sidecar.ps1
```

10. Run a clean troubleshooting restart.

```powershell
Get-Process kst-tauri,KST,Kst.Api -ErrorAction SilentlyContinue |
  Stop-Process -Force

cd src/tauri
npx @tauri-apps/cli dev
```

## Why a TypeError can still mean CORS

In this architecture the frontend and backend are separate origins when ports differ. The backend can log HTTP 200 responses while the webview/frontend still reports a browser-style network failure (`TypeError`) if CORS policy does not allow that origin to consume the response.

## Backend terminated after startup

When the sidecar exits unexpectedly after readiness:

- Tauri emits `backend-terminated` and `backend-unavailable`.
- Frontend transitions from Connected to Backend unavailable.
- No automatic restart loop is performed in Stage 3.

Action:

1. Check `%LOCALAPPDATA%\KST\logs\` for the termination reason and exit code/signal.
2. Restart the application.
3. If reproducible, keep logs and PID evidence for debugging.
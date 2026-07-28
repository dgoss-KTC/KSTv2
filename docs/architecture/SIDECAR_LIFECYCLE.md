# Sidecar Lifecycle

## Startup Sequence

```
Tauri app starts
    │
    ▼
lib.rs: launch_backend() spawns Kst.Api.exe
    │
    ▼
Backend starts, binds 127.0.0.1:0
    │
    ▼
Backend writes JSON to stdout:
  {"port":PORT,"instanceId":"GUID","status":"starting"}
    │
    ▼
Tauri reads stdout line, parses port + instanceId
    │
    ▼
Tauri polls GET /ready (up to 30 attempts, 1s interval)
    │
    ▼
/ready returns 200
    │
    ▼
Tauri injects window.__KST_BACKEND_URL__ into webview
Tauri emits "backend-ready" event
    │
    ▼
Frontend reads URL, creates ApiClient, fetches /api/v1/system/status
    │
    ▼
UI shows "Connected" with live backend status
```

## Shutdown Sequence

When the Tauri window closes:
1. Tauri's runtime kills child processes (including the backend sidecar).
2. The backend receives SIGTERM / process kill.
3. ASP.NET Core handles graceful shutdown automatically.

## Development Mode

In development (`cargo tauri dev`):
- Tauri resolves the sidecar from `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`.
- The frontend dev server runs separately on port 1420.
- The Vite HMR server is used instead of the built frontend dist.

## Production Mode

In production (packaged installer):
- The backend executable is embedded in the Tauri bundle.
- No additional runtime (like the .NET SDK) is required because the backend is self-contained.

## Error Handling

| Scenario | Behavior |
|---|---|
| Backend fails to start | Tauri logs error; frontend shows "Backend unavailable" |
| Handshake timeout (30s) | Tauri logs error; frontend remains in "waiting" state |
| Backend crashes after start | Tauri logs termination; frontend shows error on next API call |
| Multiple app launches | OS prevents second instance via single-window focus (planned) |

# Sidecar Lifecycle

## Startup and Discovery Sequence

```text
Tauri app starts
    |
    v
lib.rs launch_backend() resolves and spawns Kst.Api sidecar
    |
    v
Backend binds loopback on dynamic port: 127.0.0.1:<ephemeral>
    |
    v
Backend writes startup JSON to stdout:
  {"port":PORT,"instanceId":"GUID","status":"starting"}
    |
    v
Tauri parses handshake and stores base URL in shared state
    |
    v
Tauri polls GET /ready (up to 30 attempts)
    |
    v
If /ready never succeeds:
    - backend-ready is NOT emitted
    - backend URL is NOT stored as connected
    - owned backend process is terminated
    - backend-unavailable event is emitted
        |
        v
If /ready succeeds:
Tauri exposes backend URL to frontend:
  - command: get_backend_url
  - window injection: window.__KST_BACKEND_URL__
  - event: backend-ready
    |
    v
Frontend resolves backend URL through Tauri bridge
and retries/polls until connected
    |
    v
Frontend calls GET /api/v1/system/status and renders live status
```

## Ownership Model

- Tauri keeps explicit ownership of the active backend child process in managed runtime state.
- Managed state tracks the active child handle, PID, readiness state, and expected-shutdown flag.
- Launch requests are serialized to prevent duplicate sidecar spawns from one app instance.
- Single-instance plugin blocks second application launches from creating another backend.
- Runtime state is cleared when backend termination is observed.

## Development Port Model

- Vite frontend server runs on `http://localhost:1420`.
- Backend sidecar runs on a different dynamic loopback port such as `http://127.0.0.1:62115`.
- Different ports are expected and are not a mismatch.

## CORS and Response Consumption

Startup and readiness are separate from browser/webview origin policy.

- The backend can be fully alive and logging HTTP 200 responses.
- The frontend can still report a network-style failure if CORS does not allow the frontend origin to consume the response.
- Current backend policy allows these development/runtime origins:
  - `http://localhost:1420`
  - `http://127.0.0.1:1420`
  - `tauri://localhost`
  - `https://tauri.localhost`

## Sidecar Binary Refresh Requirement

After backend source changes, especially networking/CORS changes:

1. Republish `Kst.Api`.
2. Replace `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`.

Rebuilding the backend project alone does not refresh the sidecar binary launched by Tauri.

## Shutdown Sequence

When the Tauri app exits (window close or runtime exit):

1. Tauri requests shutdown for the owned backend PID.
2. Shutdown request is logged with reason and PID.
3. Tauri waits up to 5 seconds for backend exit.
4. If timeout is reached, Tauri forces termination by PID.
5. Graceful/timeout/forced results are logged.
6. Managed backend state is cleared.

This flow is also used for startup failure cleanup (handshake/readiness failures) so failed launches do not orphan sidecar processes.

## Error Handling

| Scenario | Behavior |
|---|---|
| Backend fails to start | Tauri logs error, emits `backend-unavailable`, no active backend URL |
| Handshake timeout | Tauri logs error, terminates owned backend, emits `backend-unavailable` |
| `/ready` timeout | Tauri logs timeout, terminates owned backend, emits `backend-unavailable` |
| Backend crashes after ready | Tauri clears active URL/process state and emits `backend-terminated` + `backend-unavailable` |
| CORS policy missing/incorrect | Backend may log 200 while frontend reports fetch-style failure |

## Single-Instance Behavior

- Tauri single-instance plugin is registered first in app startup.
- On second launch attempt, the existing main window is unminimized, shown, and focused.
- The second process exits without spawning another backend sidecar.

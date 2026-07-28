# Technical Foundation

## Overview

KST v2 uses a three-layer desktop architecture:

1. **C# / .NET 10 backend** — owns all business logic, data access, and the local HTTP API.
2. **Tauri 2 / Rust layer** — starts and monitors the backend, bridges process lifecycle.
3. **React / TypeScript frontend** — renders UI and calls the typed API client.

## Component Interaction

```
┌─────────────────────────────────────────────────┐
│                  Tauri Window                    │
│  ┌──────────────────────────────────────────┐   │
│  │           React Frontend                 │   │
│  │  (TypeScript, generated API types)       │   │
│  └──────────────┬───────────────────────────┘   │
│                 │ HTTP (loopback only)            │
│  ┌──────────────▼───────────────────────────┐   │
│  │        Rust Sidecar Manager              │   │
│  │  starts / monitors / stops backend       │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
                   │ spawn / pipe
┌──────────────────▼──────────────────────────────┐
│          .NET 10 Backend Process                 │
│  ASP.NET Core API — 127.0.0.1:<dynamic port>     │
└─────────────────────────────────────────────────┘
```

## Port Handshake

**Decision: Backend writes startup JSON to stdout; Tauri reads and parses it.**

1. Tauri spawns the backend executable with no port argument.
2. The backend binds to `127.0.0.1:0` (OS-assigned port).
3. Once bound, the backend writes a single JSON line to stdout:
   ```json
   {"port":PORT,"instanceId":"GUID","status":"starting"}
   ```
4. Tauri reads this line, extracts the port, and begins polling `/ready`.
5. Once `/ready` returns success, Tauri injects `window.__KST_BACKEND_URL__` into the webview.
6. The frontend reads this variable through `getBackendBaseUrl()` and starts calling the API.

**Rationale:** Avoids TOCTOU race condition of pre-selecting a port. No fixed port is needed.

## Security Constraints

- Backend binds **only** to `127.0.0.1` (loopback).
- No LAN interfaces are bound.
- Content Security Policy restricts API connections to loopback.
- No authentication — the application is single-user, local only.

## Technology Versions

| Component | Version |
|---|---|
| .NET | 10.0 (net10.0) |
| ASP.NET Core | 10.0 |
| Tauri | 2.x |
| Rust | 1.77+ |
| React | 19.x |
| TypeScript | 5.x |
| Vite | 6.x |

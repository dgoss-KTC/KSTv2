# Application Security Profile

**Status:** Enacted / Accepted — 2026-08-21

This document declares KST v2's KST-specific required security properties, built from the accepted
repository architecture (`docs/architecture/TECHNICAL_FOUNDATION.md`,
`docs/architecture/BACKEND_PROJECT_BOUNDARIES.md`, `docs/architecture/SIDECAR_LIFECYCLE.md`). It
does not perform the read-only attack-surface inventory planned for **S0.2 — Baseline Discovery**.

Each property below is marked as either a **Declared / Required Security Property** (an accepted
architectural requirement that must not be silently weakened) or an item whose current
implementation state is **Baseline verification pending S0.2** (to be measured, not invented, by
the next checkpoint).

## Platform

**Declared / Required Security Property**

- Windows desktop application.
- React / TypeScript frontend.
- Tauri 2 / Rust desktop host.
- Local .NET / C# ASP.NET Core backend, run as a Tauri-managed sidecar subprocess.

## Backend Networking

**Declared / Required Security Property**

- The backend binds only to `127.0.0.1` (loopback). No LAN interfaces are bound.
- This is a security property, not a development convenience, and must not be broadened merely to
  simplify development or troubleshooting.
- The backend binds to an OS-assigned dynamic port (`127.0.0.1:0`) rather than a fixed port; the
  port is communicated to Tauri via a startup handshake (see `docs/architecture/SIDECAR_LIFECYCLE.md`).

**Baseline verification pending S0.2:** exact listener behavior in the packaged runtime (as opposed
to development mode) has not yet been independently re-verified as part of this security track.

## Frontend/Backend Communication

**Declared / Required Security Property**

- Frontend and backend communicate over local HTTP (loopback) only.
- Content Security Policy is intended to restrict API connections to loopback.

**Baseline verification pending S0.2:** the exact current CORS/origin allow-list and CSP
configuration should be inventoried and verified against `docs/architecture/SIDECAR_LIFECYCLE.md`'s
documented development-mode origins during baseline discovery, rather than assumed correct here.
Development-mode CORS behavior is not assumed to represent packaged-runtime behavior (see
`docs/deployment/WINDOWS_PACKAGING.md` §"Verification Scope Note").

## Production Databases

**Declared / Required Security Property**

- Production database access is read-only.
- Direct `INSERT`/`UPDATE`/`DELETE`/`MERGE` or other database-side operational changes from the
  application are prohibited.
- QAD and other authoritative company systems remain systems of record.
The application must not submit changes into QAD or QXtend, automatically trigger an import, or
perform direct write-back to production databases.

Least privilege applies to credentials, service accounts, permissions, capabilities, and identities
that KST provisions or controls: for those, KST must use least privilege appropriate to application
need. For an existing enterprise human identity authenticated through Windows Integrated
authentication (for example, the operator's QAD identity), KST must not elevate, broaden,
provision, or otherwise modify that identity's enterprise authority and must operate only within the
permissions assigned by the authoritative enterprise system; KST is not responsible for documenting
the business rationale behind the user's broader enterprise access.

## Credentials

**Declared / Required Security Property**

- No production credentials may be committed, hard-coded, or logged.

**Baseline verification pending S0.2:** exact credential supply/storage paths for QAD and other
integration connections are an inventory item for baseline discovery unless already explicitly
established in current integration documentation.

## Process Model

**Declared / Required Security Property**

- Tauri owns and coordinates the local backend sidecar process (spawn, readiness handshake,
  shutdown) per `docs/architecture/SIDECAR_LIFECYCLE.md`.
- A single-instance mechanism prevents a second application launch from spawning a second backend.

**Baseline verification pending S0.2:** exact subprocess/process-tree behavior beyond what is
already documented in `SIDECAR_LIFECYCLE.md` is an inventory/verification item, not re-derived here.

## Filesystem

**Baseline verification pending S0.2:** this profile does not invent an allowed-directory list.
Known architectural expectations (e.g. local application data under `%LOCALAPPDATA%\KST\config\`,
per `docs/architecture/BACKEND_PROJECT_BOUNDARIES.md`) are recorded there; the full actual
file-access surface is an S0.2 inventory item.

## CSP / CORS / Tauri Capabilities

**Baseline verification pending S0.2** for exact current values. Where current architecture
documentation has not fully reconciled these values against post-Stage-8 implementation, they are
recorded here as security-sensitive profile areas to be verified, not inspected or "fixed" during
S0.1.

## External Integrations

**Declared / Required Security Property**

- QAD is the known authoritative external integration; the application is a read-only consumer of
  QAD data via `Kst.Integrations.Qad`.
- A shortages system integration boundary exists (`Kst.Integrations.Shortages`).
- Export/file-generation functionality exists as a controlled outbound boundary
  (`Kst.Exports`), used for reviewable operational output rather than direct database write-back.

**Baseline verification pending S0.2:** actual outbound destinations, connection details, and any
additional external services are an inventory item.

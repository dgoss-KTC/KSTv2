# S0.7 — Runtime & Infrastructure Verification

**Status:** IN PROGRESS
**S0.7A — Local Release Runtime Verification:** COMPLETE / ACCEPTED — 2026-08-28
(2026-08-27 evidence pass: COMPLETE / ACCEPTED AS EVIDENCE by project-owner review; S0.5-F001 narrow
remediation implemented and re-verified 2026-08-28; failure-safe regression-test correction per
steering, 2026-08-28 — see §26; project-owner final acceptance of S0.7A — 2026-08-28)
**S0.7B — Database / Infrastructure Permission Verification:** COMPLETE / ACCEPTED — 2026-08-28 (companion evidence: `docs/security/S0_7_DATABASE_INFRASTRUCTURE_PERMISSION_VERIFICATION.md`; `S0.3-G010` — Covered / Resolved — 2026-08-28; **`S0.7-F002`** — RETIRED / Application-vs-Enterprise Identity Scope Model Corrected — 2026-08-28 owner scope decision; NOT Accepted Risk; NOT a waived vulnerability; NOT evidence deletion)

**Date:** 2026-08-27 (evidence pass); 2026-08-28 (remediation, re-verification, steering correction, and project-owner acceptance — §26)
**Starting commit:** `00dcd11dd75722d26dbea753aea24579eb40e42d` (`docs: accept DevSkim SAST capability`)

This document is **evidence, not normative policy**. It records the actual
release-build and runtime-observed behavior of the KST v2 desktop application
during the **S0.7A — Local Release Runtime Verification** pass. Required
security properties remain defined by `SECURITY.md` and `docs/security/`
(especially `SECURITY_ASSURANCE_POLICY.md` and `APPLICATION_SECURITY_PROFILE.md`).

Evidence classes used throughout:

- **Repository evidence** — inspected from the working tree at the starting commit.
- **Release-build evidence** — produced by running the established release build commands.
- **Runtime-observed evidence** — observed while the release-built executable was running on
  this workstation.
- **Unable to Verify** — could not be established in this pass without actions outside the
  authorized scope; never marked `Accepted Risk`.

S0.7A is a **bounded local pass**: it verifies local release-runtime properties only.
S0.7B (server-side QAD database-grant verification and other IT/infrastructure-dependent
work) is **not** started by this pass. S0.7 overall remains **IN PROGRESS** after S0.7A;
S0.7 is **not** accepted.

---

## 1. Authority / Scope

**Governing authority read before acting (enacted / current):**

- `AGENTS.md` (enacted repository rules, Tier 1).
- `SECURITY.md` (enacted security entry point, Tier 1).
- `docs/security/SECURITY_ASSURANCE_POLICY.md` (primary normative policy).
- `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`.
- `docs/security/DEPENDENCY_ADMISSION.md` (incorporating
  `docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md`).
- `docs/security/AI_SECURITY_REVIEW.md`.
- `docs/security/APPLICATION_SECURITY_PROFILE.md` (declared required security properties).
- `docs/security/SECURITY_BASELINE.md` (S0.2 accepted observational baseline — historical
  evidence, not modified).
- `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (S0.3 accepted evidence; gap
  definitions S0.3-G001–G010).
- `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md` (S0.5 accepted evidence;
  findings S0.5-F001/F002).
- `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`,
  `docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`,
  `docs/security/S0_6_SBOM_ADMISSION.md`,
  `docs/security/S0_6_SAST_ADMISSION.md` (S0.6 accepted admission evidence).
- `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` (approved planning,
  Tier 4 — defines S0.7 scope and the G009/G010 assignments).
- `docs/status/CURRENT_PROJECT_STATUS.md`, `KST-v2-Master-Project-Checklist.md`
  (accepted current project state).
- `docs/deployment/WINDOWS_PACKAGING.md`, `docs/development/SETUP.md`,
  `docs/architecture/SIDECAR_LIFECYCLE.md` (established build/runtime procedures).

**Scope of S0.7A (this pass):**

- Build the current release artifact using **only** the established repository build path.
- Launch the **release-built** Tauri executable (not dev mode, not `dotnet run`, not an npm
  dev server) and observe:
  - actual process tree (Tauri host / .NET sidecar / WebView children);
  - actual backend listener binding (S0.3-G009 core evidence);
  - a **safe loopback-only** `ASPNETCORE_URLS` precedence test (S0.5-F001 runtime
    disposition);
  - sidecar lifecycle (does host close terminate the sidecar; orphan check);
  - runtime CORS behavior against the actual release sidecar;
  - runtime network connections during idle/startup;
  - runtime logging and safe error-response behavior.
- Release-build artifact evidence for:
  - the effective CSP the release build was constructed with;
  - the effective Tauri capability/ACL surface;
  - the residual `@tauri-apps/plugin-shell` frontend dependency state (S0.4B-F001);
  - application identity, single-instance configuration, and installer scope (see §13,
    §18, and finding `S0.7-F001`).

**Explicitly out of scope (not performed):**

- S0.7B: server-side QAD login/group grant verification, `keytronicshortage`
  hosting/permission details, any live QAD SQL connection initiated by this pass, or live
  SQL execution.
- Any non-loopback network exposure test (no `0.0.0.0`, no LAN-address listener test).
- Installing or executing the generated Windows installer; installed-package behavior
  remains a recorded boundary (§18).
- CSP devtools enabling, instrumentation, or any alteration of the release artifact to
  observe dynamic enforcement.
- New tool installation, dependency changes, lockfile changes, source/config changes,
  signing, or updater configuration.
- Inspecting or modifying KST v1 (legacy application) files; no undocumented KST v1
  configuration was inferred (§18, `S0.7-F001`).
- S0.8 work. Stage 9 work.

**Carried runtime/infrastructure verification concerns (derived from repository evidence,
not from the task prompt):**

| Item | Source | Addressed in S0.7A |
|---|---|---|
| Packaged/runtime listener verification | S0.3-G009 (S0.3 §11); S0.2 §9/§20; S0.5 §14 | Yes (§7, §8) |
| `ASPNETCORE_URLS` operator-override runtime behavior | S0.5-F001 (S0.5 §12/§14) | Yes (§8) — safe loopback-only test |
| Packaged CORS behavior | S0.2 §10/§20; S0.3 §11 (CORS secondary observation); S0.5 §14 | Yes (§10) |
| Packaged CSP behavior | S0.2 §20; S0.3-G003/S0.5 §6 residual; S0.5 §14 | Release-build evidence only (§12) |
| Effective Tauri capability behavior | S0.2 §20; S0.3-G004/S0.5 §7 residual; S0.5 §14 | Release-build evidence only (§13) |
| Sidecar process lifecycle | S0.2 §20 (process-tree behavior); `SIDECAR_LIFECYCLE.md` | Yes (§9) |
| Runtime outbound network destinations beyond the configured QAD server | S0.2 §20 | Yes, idle/startup observation only (§15) |
| Whether any exception path could incidentally log connection-string details | S0.2 §16/§20 | Bounded (§16, §17) |
| Unused `@tauri-apps/plugin-shell` frontend dependency | S0.4B-F001 (Informational) | Re-observed only (§14) |
| Actual QAD login/group grants (server-side) | S0.3-G010 | **No — S0.7B (PENDING)** |
| `keytronicshortage` hosting/permission details | S0.2 §13.3/§20 | **No — S0.7B (PENDING)** |

## 2. Starting Repository State

**Preflight (executed before any build or launch):**

| Check | Result |
|---|---|
| `git branch --show-current` | `main` |
| `git rev-parse HEAD` (initial) | `2ca60f38335061223a32235c20cddf8616f7de99` — 9 commits **behind** `origin/main`; local `main` a strict ancestor; working tree clean |
| `git rev-parse origin/main` | `00dcd11dd75722d26dbea753aea24579eb40e42d` |
| Divergence handling | Per the S0.7A stop rule, the pass **stopped** on the unexpected local/remote skew and surfaced it to the project owner. The 9 missing commits were all accepted S0.6 documentation-acceptance commits (docs + `AGENTS.md` only; no source). With explicit project-owner authorization, a **fast-forward** (`git merge --ff-only origin/main`) was performed — non-destructive (clean tree, strict ancestor; nothing discarded). No pull/merge-with-content/rebase/reset/clean/stash/discard/force was used. |
| `git rev-parse HEAD` (after ff) | `00dcd11dd75722d26dbea753aea24579eb40e42d` == `origin/main` — **matches the expected accepted baseline** |
| `git status --short` | empty (clean) |
| `git diff --name-status` / `git diff --cached` | empty (nothing staged, nothing unstaged) |
| `git log -8 --oneline` | `00dcd11 docs: accept DevSkim SAST capability`; `b9b005d docs: admit DevSkim SAST capability`; `171fb1a docs: enact third-party licensing governance`; `464cde0 docs: record SAST capability research`; `fb5b6c9 docs: accept SBOM capability`; `51a9b39 docs: admit Syft SBOM capability`; `2579368 docs: accept secret scanning capability`; `a9bb2a7 docs: record Gitleaks secret-scanning implementation evidence` |

**Worktree note:** the repository has several pre-existing agent worktrees under
`C:/Dev/kst_v2.worktrees/` (from earlier S0.6 agent runs); none were used for this pass.
All S0.7A work was performed in the canonical worktree `C:/dev/kst_v2` on branch `main`.

**Existing ignored build-artifact directories present at start** (recorded, not cleaned):
`publish/backend`, `publish/backend-sidecar`, `src/tauri/binaries`, `src/tauri/target`,
`src/frontend/dist`, `src/frontend/node_modules`, and per-project `src/backend/**/bin`
+ `obj` directories. All are git-ignored (`.gitignore` lines 9–17); none were modified by
the preflight and none are committed.

**Finding-ID integrity (inspected before creating any new ID):**

- Existing assigned security findings across all accepted evidence:
  `S0.2-F001`–`S0.2-F003`, `S0.3-F001`, `S0.4B-F001`, `S0.5-F001`, `S0.5-F002`,
  `S0.6-F001`–`S0.6-F021`.
- **Highest previously assigned S0.6 finding: `S0.6-F021`** (verified; matches expectation).
- Repository convention is a **per-checkpoint finding namespace** (S0.2/S0.3/S0.4B/S0.5/
  S0.6 each own their `Fxxx` sequence). S0.7 therefore uses a **new namespace
  `S0.7-Fxxx`**, continuing the established per-checkpoint pattern. No ID is reused;
   two findings were created in S0.7: `S0.7-F001` (§20, genuinely observed behavior — Deferred) and
   `S0.7-F002` (QAD least-privilege gap — **RETIRED** 2026-08-28, Application-vs-Enterprise Identity
   Scope Model Corrected; NOT Accepted Risk; NOT a waived vulnerability; NOT evidence deletion).

**Toolchain observed at start (read-only; nothing installed):**

| Tool | Version |
|---|---|
| .NET SDK | 10.0.301 |
| Node.js | v26.5.0 |
| npm | 11.17.0 |
| rustc / cargo | 1.97.1 (x86_64-pc-windows-msvc) |
| `@tauri-apps/cli` (via `npx`, local npm-cache entry) | 2.11.4 (pre-cached from the previously accepted packaging runs; no new download required — see §4) |

**Operating environment:** Windows 11 workstation (`E3007445`, MINGW64_NT-10.0-26200),
local time zone PDT (UTC-7). QAD is configured on this machine
(`QadDatabase.Server` set in `appsettings.json`); the accepted application startup
behavior performs a read-only QAD auto-load when a previously configured workspace is
restored (see §15 note). No database activity was initiated by this pass; all QAD traffic
observed was the application's own accepted read-only startup behavior.

## 3. Release Build Path

**Established repository build path** (per `docs/deployment/WINDOWS_PACKAGING.md`,
unchanged by this pass):

1. **Publish and copy the .NET sidecar:** `scripts\build-sidecar.ps1` from the repository
   root — runs
   `dotnet publish src/backend/Kst.Api/Kst.Api.csproj -c Release -r win-x64
   --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false
   /p:PublishAot=false /p:DebugType=None /p:DebugSymbols=false
   /p:IncludeNativeLibrariesForSelfExtract=true -o publish/backend-sidecar`,
   then copies the result to
   `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe` plus the
   `appsettings*.json` resource files.
2. **Build the frontend:** `npm run build` in `src/frontend`
   (`tsc -b && vite build`) — executed automatically by the Tauri build as
   `beforeBuildCommand` (`npm --prefix ./frontend run build`).
3. **Package with Tauri:** `npx @tauri-apps/cli build` in `src/tauri` — produces the
   release host executable and the `bundle/` installers (NSIS + MSI, since
   `bundle.targets: "all"`).

No new build process was invented. No dependency, lockfile, build-configuration, Rust
target, signing, or updater change was made.

**Tool-availability determination:** `@tauri-apps/cli` is not a global install and not a
project dependency; the established command invokes it through `npx`, and the local npm
cache (`C:\Users\dgoss\AppData\Local\npm-cache\_npx`) already contained
`@tauri-apps/cli` **v2.11.4** (with the `cli-win32-x64-msvc` platform binary) from the
previously accepted packaging runs recorded in
`docs/development/VERSIONING.md` ("Packaged Windows build (NSIS + MSI) verified
successfully with the new version metadata"). **No new tool was installed or downloaded
in this pass** — the npm log for the build shows exactly one registry interaction:
`GET https://registry.npmjs.org/@tauri-apps%2fcli 200 (cache revalidated)` (a metadata
revalidation by `npx`; no package was fetched).

## 4. Build Integrity

**Commands executed (in order), with results:**

| # | Command (cwd) | Result |
|---|---|---|
| 1 | `powershell -NoProfile -File "C:\dev\kst_v2\scripts\build-sidecar.ps1"` (repo root) | **Exit 0.** Restore of 7 backend projects (warm, ~0.5 s each); Release build of all 7 projects (0 errors); `GenerateOpenApiDocuments` regenerated `docs/openapi/Kst.Api.json`; `dotnet publish` → `publish/backend-sidecar/Kst.Api.exe` (**116,315,801 bytes**); copied to `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe` + `appsettings.json` + `appsettings.Development.json` |
| 2 | `npx @tauri-apps/cli build` (`src/tauri`) | **Exit 0.** `beforeBuildCommand` → frontend `tsc -b && vite build` (vite v6.4.3, 98 modules, `dist/` output); Rust release compile of `kst-tauri v0.1.0-alpha.2` (incremental, 1m 18 s) → `src/tauri/target/release/kst-tauri.exe`; MSI bundling (candle/light) → `bundle/msi/KST_0.1.0_x64_en-US.msi`; NSIS bundling (makensis) → `bundle/nsis/KST_0.1.0_x64-setup.exe`. `npx` used cached `@tauri-apps/cli` 2.11.4 (single metadata revalidation, no download — see §3) |

**Build duration:** ~6 minutes total (sidecar publish started 15:20:58 local; final NSIS
bundle complete by ~15:26 local; Rust step reported 1m 18 s incremental).

**Major output paths produced:**

- `publish/backend-sidecar/Kst.Api.exe` (sidecar publish output)
- `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe` (Tauri externalBin input)
- `src/frontend/dist/` (frontend production output)
- `src/tauri/target/release/kst-tauri.exe` (**release application executable — runtime
  subject of this pass**)
- `src/tauri/target/release/Kst.Api.exe` (sidecar staged next to the host for the
  externalBin launch path — byte-identical to the publish output, §5)
- `src/tauri/target/release/bundle/nsis/KST_0.1.0_x64-setup.exe`
- `src/tauri/target/release/bundle/msi/KST_0.1.0_x64_en-US.msi`

**Post-build mutation gate (immediately after build):**

| Check | Result |
|---|---|
| `git status --short` | `?? docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md` only (the new evidence document) |
| `git diff --name-status` / `git diff --stat` | empty — **no tracked source/config/dependency/build metadata changed** |
| `git diff --check` | clean |
| `docs/openapi/Kst.Api.json` (regenerated by `GenerateOpenApiDocuments`) | byte-identical to committed version (established repository behavior, per accepted S0.3 §7.1); no diff |

The build left the tracked tree unmodified.

## 5. Artifact Identity

Identity metadata for the actual release-built executables used during verification
(hashes recorded in this document only; no binary or hash file committed):

| Artifact | Path | Size (bytes) | SHA-256 | Built (local time) |
|---|---|---|---|---|
| Tauri host executable (runtime subject) | `src/tauri/target/release/kst-tauri.exe` | 12,647,936 | `869c1c204c01a0ec6e8cb2fb33aa8e7d9c6a9b9e55d2ed7a27960e358e859a18` | 2026-08-27 15:25:23 -07:00 |
| .NET sidecar (publish output) | `publish/backend-sidecar/Kst.Api.exe` | 116,315,801 | `c1dccfa558d6a23480293362edc2a7c86c49395f20fe989dcd81c31dcc0a6b8c` | 2026-08-27 15:21:14 -07:00 |
| .NET sidecar (Tauri externalBin) | `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe` | 116,315,801 | `c1dccfa558d6a23480293362edc2a7c86c49395f20fe989dcd81c31dcc0a6b8c` (identical) | 2026-08-27 15:21:14 -07:00 |
| .NET sidecar (staged next to release host; **the copy actually executed at runtime**) | `src/tauri/target/release/Kst.Api.exe` | 116,315,801 | `c1dccfa558d6a23480293362edc2a7c86c49395f20fe989dcd81c31dcc0a6b8c` (identical) | 2026-08-27 15:21 (staged at bundle time) |
| NSIS installer (produced; **not executed**) | `src/tauri/target/release/bundle/nsis/KST_0.1.0_x64-setup.exe` | 38,812,299 | `4a5b7e2a3311474f6bf6c765ef0a07fd58212a47e5856a8c1559132d9dc988df` | 2026-08-27 15:25:23 -07:00 |
| MSI installer (produced; **not executed**) | `src/tauri/target/release/bundle/msi/KST_0.1.0_x64_en-US.msi` | 53,698,560 | `aa4690b2f3fbe64b52c6d2e2259682f28e4f2ae311813c740566c81559948599` | 2026-08-27 15:24:20 -07:00 |

Host executable version metadata: ProductName `KST`, ProductVersion `0.1.0`
(`tauri.conf.json` numeric-only version, per `docs/development/VERSIONING.md`).
All three sidecar copies are byte-identical, so runtime behavior was exercised by the
exact publish output.

## 6. Release Process Tree

**Controlled baseline launch** (release host started 2026-08-27 16:20:26 local via
`Start-Process` of `src/tauri/target/release/kst-tauri.exe`; observed ~25 s after
launch, i.e., after sidecar handshake, `/ready`, and frontend startup):

| Process | PID | Parent | Role / notes |
|---|---|---|---|
| `kst-tauri.exe` | 9348 | launcher (agent shell) | **Tauri host** (release executable, §5); main window title `Keytronic Scheduler's Toolbox` |
| `Kst.Api.exe` | 15720 | 9348 (host) | **.NET sidecar** — spawned directly by the host per `launch_backend` (`app.shell().sidecar("Kst.Api")`); executed from `src/tauri/target/release/Kst.Api.exe` (byte-identical to publish output); content root `src/tauri\target\release\binaries` (bundled `appsettings*.json` resources) |
| `msedgewebview2.exe` | 27112 | 9348 (host) | **WebView2** (GPU/browser process) |
| `msedgewebview2.exe` | 17812, 32984, 23972, 21944, 35312 | 27112 | WebView2 renderer/utility children (distinguished by parent chain) |
| `conhost.exe` | 35916 | 15720 (sidecar) | Sidecar console host (stdout pipe to the Tauri host) |

**Actual backend port: 51598** — an OS-assigned dynamic port (consistent with the
repository default `127.0.0.1:0`; the Tauri host does not set `ASPNETCORE_URLS`, so the
`Program.cs` fallback path was taken, confirmed by the handshake-driven flow and by §8).

**Release-artifact / source separation confirmed:** all runtime observations in this
document are from the release-built executable launched from
`src/tauri/target/release/`. No `npm run dev`, Tauri dev mode, `dotnet run`, or IDE
launch profile was used. The 08:33 (local) log entries earlier in the day are from a
separate pre-existing development-mode run (content root `src\tauri\binaries`, no
`target\release`) and are **not** used as evidence for this pass.

## 7. Runtime Listener (S0.3-G009)

**Actual listening socket owned by the release sidecar (PID 15720), observed via
`Get-NetTCPConnection` while the release application was running:**

| PID (role) | Protocol | Local address | Local port | State |
|---|---|---|---|---|
| 15720 (sidecar) | TCP | `127.0.0.1` | 51598 | Listen |

Corroborated by the sidecar's own runtime log line:
`Now listening on: http://127.0.0.1:51598` (16:20:28 local, instance
`ced29818-…`).

**Determination (runtime-observed):**

- The release sidecar listens on **`127.0.0.1` (IPv4 loopback) only**, on an OS-assigned
  dynamic port — exactly matching the declared property in
  `APPLICATION_SECURITY_PROFILE.md` ("binds only to `127.0.0.1` … OS-assigned dynamic
  port").
- **No unexpected listener exists for any KST process:** the Tauri host (PID 9348) owns
  no listening socket; no WebView2 child owns a listening socket.
- **No `0.0.0.0`, `::`, or LAN-interface listener is owned by any KST process.**
  (The full system listener table was captured; all wildcard/LAN listeners on the
  machine belong to pre-existing non-KST PIDs — Windows services such as RPC/SMB
  (PIDs 4, 2024, 5504) and local development tooling — and were present in the
  pre-launch baseline or are attributable to non-KST processes by PID.)
- The sidecar's complete TCP table during idle contained exactly one entry: its own
  `127.0.0.1:51598` listener.

This is **actual runtime evidence from the release-built executable**, not inference from
`Program.cs`.

## 8. ASPNETCORE_URLS Safe Precedence Test (S0.5-F001)

**Method (safe, loopback-only):** a fresh release application process tree was launched
with a single environment variable set only for the child launch session:

```
ASPNETCORE_URLS=http://127.0.0.1:47311
```

`47311` was verified unused immediately before launch. The value is a **loopback** URL —
no `0.0.0.0`, LAN, or public-interface value was ever used or tested. No other
environment or configuration was changed.

**Observed result (runtime-observed, 2026-08-27 16:44 local):**

- Host PID 30920 (`kst-tauri.exe`), sidecar PID 3328 (`Kst.Api.exe`).
- Actual sidecar listener: **`127.0.0.1:47311`** (the environment-provided URL) —
  confirmed via `Get-NetTCPConnection` (Listen) and the sidecar log line
  `KST backend listening on http://127.0.0.1:47311 (port 47311)`.
- The startup handshake, `/ready` polling, and frontend connection all succeeded on the
  environment-provided port (an `Established` loopback connection on 47311 was observed
  from the page), and the accepted read-only QAD auto-load completed.

**Outcome: B — `ASPNETCORE_URLS` alters the actual listener/port used by the KST
release sidecar.** This confirms the runtime behavior flagged by `S0.5-F001`
(environment override takes precedence over the repository-controlled
`127.0.0.1:{OS-assigned}` fallback in `Program.cs`). In the tested configuration the
binding remained loopback because the environment value itself was loopback; the test
establishes that operator-supplied environment configuration **can** alter runtime
binding, and was **not** escalated to any non-loopback exposure test (per the S0.7A
stop rule). No remediation was performed.

**Disposition:** see §21 (S0.5-F001 retained / escalated for human review) and §22
(G009 gate consequence).

## 9. Sidecar Lifecycle

**Graceful shutdown method used for the controlled cycles:** the main window was closed
by sending `WM_CLOSE` (0x0010) to the window handle via the standard Win32 `SendMessage`
API — the same event path as a user closing the application window (triggers Tauri's
`CloseRequested` → `shutdown_active_backend` → PID-scoped `child.kill()` with 5 s wait,
then PID-scoped `taskkill /PID … /T /F` fallback per `lib.rs`). No broad
process-name kill was ever used.

| Cycle | Host PID | Sidecar PID | Shutdown method | Host exited | Sidecar exited | Listener released | Orphan remaining |
|---|---|---|---|---|---|---|---|
| Baseline (16:20–16:38) | 9348 | 15720 | `WM_CLOSE` to main window | Yes (observed ≤8 s after close) | Yes | Yes (`127.0.0.1:51598` gone) | **None** |
| ASPNETCORE_URLS test (16:44–16:49) | 30920 | 3328 | `WM_CLOSE` to main window | Yes (observed ≤8 s after close) | Yes | Yes (`127.0.0.1:47311` gone) | **None** |
| Coincidental human session (15:33–15:37) | 24340 | (sidecar per log instance `282ee014-…`) | Project-owner closed the window in the UI | Yes | Yes | Yes (`127.0.0.1:53669` gone, observed 15:37) | **None** |

Notes:

- In all three close events, both the host and its sidecar exited and the listener port
  was released; no orphan `Kst.Api.exe` process or orphan listener remained.
- The sidecar log contains no "shutting down" line after the window close, consistent
  with termination via the host's `child.kill()` path rather than a full .NET graceful
  host-shutdown flush; the process exit, listener release, and orphan absence were all
  directly observed.
- The single-instance mechanism (`tauri-plugin-single-instance` 2.4.3) also prevented
  duplicate sidecars from duplicate launches during the pass (see §18 / `S0.7-F001`).

## 10. Runtime CORS

**Configured origin allowlist (verified from current source, `Kst.Api/Program.cs`):**
exactly five literal origins — `http://localhost:1420`, `http://127.0.0.1:1420`,
`tauri://localhost`, `http://tauri.localhost`, `https://tauri.localhost` — with
`AllowAnyHeader()` + `AllowAnyMethod()`, and **neither** `AllowAnyOrigin()` nor
`AllowCredentials()`. (Matches the S0.5-accepted CORS surface.)

**Runtime requests against the actual release sidecar** (baseline cycle, port 51598,
endpoint `GET /health` — safe, read-only, no database access, no state mutation):

| Origin (request) | HTTP status | `Access-Control-Allow-Origin` | Other material CORS headers |
|---|---|---|---|
| `http://localhost:1420` (allowed) | 200 | `http://localhost:1420` (exact echo) | `Vary: Origin` |
| `http://127.0.0.1:1420` (allowed) | 200 | `http://127.0.0.1:1420` (exact echo) | `Vary: Origin` |
| `tauri://localhost` (allowed) | 200 | `tauri://localhost` (exact echo) | `Vary: Origin` |
| `http://tauri.localhost` (allowed) | 200 | `http://tauri.localhost` (exact echo) | `Vary: Origin` |
| `https://tauri.localhost` (allowed) | 200 | `https://tauri.localhost` (exact echo) | `Vary: Origin` |
| `https://example.invalid` (disallowed) | 200 | **absent** | none |
| `http://localhost.example.invalid` (disallowed) | 200 | **absent** | none |
| `null` (disallowed) | 200 | **absent** | none |
| no `Origin` header (baseline) | 200 | **absent** | none |

**Preflight (OPTIONS, `Access-Control-Request-Method: GET`):**

| Origin | HTTP status | `Access-Control-Allow-Origin` | Other material CORS headers |
|---|---|---|---|
| `http://localhost:1420` (allowed) | 204 | `http://localhost:1420` (exact echo) | `Access-Control-Allow-Methods: GET` |
| `https://example.invalid` (disallowed) | 204 | **absent** | none |

**Verification result (runtime-observed):**

- Each allowed origin receives **only its intended exact-origin** response.
- Disallowed origins (including `null`) receive **no** allow-origin response.
- **No `Access-Control-Allow-Origin: *`** observed in any response.
- **No `Access-Control-Allow-Credentials`** observed in any response (credentials not
  allowed, consistent with the enacted architecture).
- The `GET /health` response body contains only: status, application name, backend
  version, process ID, instance ID (GUID), timestamp — no sensitive data.

This is runtime behavior evidence from the release sidecar, not structural-test
inference (the S0.5 `CorsPolicyTests` remain the durable repository check; both agree).

## 11. CORS vs Network-Binding Architecture

Recorded explicitly, per S0.7A requirement:

- **CORS** controls browser/WebView cross-origin access: it governs which *origins* the
  webview is allowed to read responses from. It is enforced in the KST sidecar's HTTP
  response headers (§10).
- **Socket binding** controls which network *interfaces/ports* accept connections at all
  (§7: `127.0.0.1` dynamic port only).
- A CORS allowlist does **not** itself create a network listener, and a loopback
  listener does **not** itself prove correct CORS behavior.
- S0.7A evaluated **both independently** with runtime evidence: loopback-only binding
  (§7) and per-origin CORS enforcement (§10) are separate, each verified on the running
  release sidecar.

## 12. CSP Release Evidence

**Configured CSP** (`src/tauri/tauri.conf.json` → `app.security.csp`, single value, no
dev/prod distinction):

```
default-src 'self'; connect-src http://127.0.0.1:* 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com
```

- `connect-src` is restricted to **loopback (`http://127.0.0.1:*`, any port) plus
  `'self'`** — loopback API access is intentionally permitted; no remote API origin.
- `default-src` is exactly `'self'`; the effective script policy (no explicit
  `script-src`) is therefore `'self'` — **no `unsafe-inline`, `unsafe-eval`, bare
  wildcard, or remote script source** in the script policy.
- Accepted, non-`connect` relaxations (not flagged): `style-src 'unsafe-inline'` and the
  Google Fonts origins for `style-src`/`font-src` (intentionally outside the
  S0.5 `csp_guard` test scope, per accepted S0.5 §6).

**Release-build artifact evidence (preferred over source alone):** the **exact CSP string
above was found embedded in the release-built host executable**
(`src/tauri/target/release/kst-tauri.exe`, string search of the built binary). Tauri
bakes `app.security.csp` into the application context at compile time, so this
establishes that **the release build was constructed with this CSP**.

**Dynamic WebView CSP enforcement: Unable to Verify in this pass** — directly observing
enforcement in the running webview would require enabling devtools, injecting
instrumentation, or altering the admitted release artifact, all outside the authorized
S0.7A scope. No dynamic enforcement evidence is claimed.

## 13. Tauri Capability / ACL Release Evidence

**Effective release capability set, from build-generated artifacts:**

| Evidence | Location | Result |
|---|---|---|
| Build-generated capability file (regenerated by the release build via `tauri-build`; tracked in git and byte-identical to the committed version after the build) | `src/tauri/gen/schemas/capabilities.json` | `{"default":{"identifier":"default","local":true,"windows":["main"],"permissions":["core:default"]}}` — exactly `core:default`, window `main` only |
| Build-generated ACL manifests (compile-time resolved plugin permission manifests) | `src/tauri/gen/schemas/acl-manifests.json` | Contains **only `core:*` plugin manifests** (`core:app`, `core:event`, `core:image`, `core:menu`, `core:path`, `core:resources`, `core:tray`, `core:webview`, `core:window`); **no shell-plugin permission manifest appears in the build-resolved ACL at all** |
| Release binary string inspection | `src/tauri/target/release/kst-tauri.exe` | No `shell:allow-execute` and no `shell:allow-open` literal present; app identifier `com.keytronic.kst` embedded |
| Source capability (corroboration only) | `src/tauri/capabilities/default.json` | `permissions: ["core:default"]`, `windows: ["main"]` — no `shell:*` permission (accepted S0.4B state) |

**Determination:** the effective release capability set includes **only `core:default`**.
The previously removed broad shell permissions **`shell:allow-execute` and
`shell:allow-open` are absent** from the build-generated capability file, from the
build-resolved ACL manifests, and from the release binary string surface. No
other shell/plugin permission is granted.

**Host-side note (accepted S0.4B architecture, not a webview grant):** the
`tauri-plugin-shell` **crate** is compiled into the host (`tauri_plugin_shell` symbol
present in the binary) because the **Rust host itself** uses it for `Kst.Api` sidecar
spawn/termination and PID-scoped lifecycle handling, outside the webview IPC surface.
Presence of the crate is not a webview capability grant; the capability file above is
authoritative for the webview.

**Dynamic capability enforcement: Unable to Verify in this pass** — proving the negative
dynamically would require enabling devtools or injecting IPC, which is outside the
authorized scope. The build-generated artifacts are the effective build-time ACL
evidence; runtime enforcement is not claimed as tested.

**Application identity / single-instance (narrow inspection per S0.7A steering
correction; no changes made):**

| Property | Value | Evidence |
|---|---|---|
| Tauri application identifier | `com.keytronic.kst` | `tauri.conf.json` (`identifier`); embedded in release binary; NSIS `BUNDLEID` define |
| Product / application name | `KST` | `tauri.conf.json` (`productName`); NSIS `PRODUCTNAME` define; host PE ProductName |
| Release executable name | `kst-tauri.exe` (repo build output); installed name would be `KST.exe` (NSIS `PRODUCTNAME`) | build output; `installer.nsi` |
| Single-instance mechanism | `tauri-plugin-single-instance` v2.4.3, registered first at startup; second launch restores/focuses the existing window and exits without spawning a second backend | `src/tauri/Cargo.lock`; `src/tauri/src/lib.rs`; `docs/architecture/SIDECAR_LIFECYCLE.md` §"Single-Instance Behavior"; `docs/development/SETUP.md` |
| Identifier-scoped data directory | `%LOCALAPPDATA%\com.keytronic.kst` exists (contains `EBWebView` WebView2 data; last written by this pass's release runs); NSIS uninstaller removes `%APPDATA%\com.keytronic.kst` and `%LOCALAPPDATA%\com.keytronic.kst` | observed directory listing (names only); `installer.nsi` |

## 14. Plugin-Shell Residual Dependency (S0.4B-F001)

**Current state (repository evidence, re-verified this pass):** `@tauri-apps/plugin-shell`
remains declared at `^2.2.0` in `src/frontend/package.json` dependencies. Per accepted
S0.4B evidence (re-affirmed by current source search), no import or call of the plugin
exists in `src/frontend/src`; the built frontend bundle contains no shell-plugin usage.

- **No remediation was performed** (removal requires separate authorization, per
  S0.4B-F001).
- **No new issue:** the dependency's presence grants no webview IPC authority — the
  capability file is authoritative and grants no `shell:*` permission (§13), so presence
  of the unused dependency is not converted here into proof of active capability
  exposure. S0.4B is not reopened.

## 15. Runtime Network Connections

**Observation method:** `Get-NetTCPConnection` by owning process, during controlled
idle/startup of the release application (baseline cycle). Windows-native tooling only;
no packet capture, no new monitoring software.

| Process role | Observation (idle, after startup settled) | Classification |
|---|---|---|
| Sidecar `Kst.Api.exe` (15720) | Exactly one TCP entry: its own `127.0.0.1:51598` listener. No established/outbound connections at idle. | Expected: loopback listener only. QAD connections are **per-query and transient** (opened for the accepted read-only startup auto-load, closed after the query completes; not visible at idle). |
| Tauri host `kst-tauri.exe` (9348) | No TCP connections of any kind (no listener, no outbound). | Expected: the host communicates with the sidecar via a stdout pipe, not TCP. |
| WebView2 (27112 + children) | No TCP connections at observation time (page→sidecar XHRs are short-lived loopback connections that close between requests). | OS/WebView2-runtime traffic distinguished from KST host/sidecar traffic by process ownership; nothing attributed to KST application code was non-loopback. |

**Determination:** during idle/startup, **no KST process (host, sidecar, or WebView2
children) created any non-loopback outbound connection, and no persistent outbound
connection of any kind was observed.** No packet-level proof is claimed.

**Note on startup QAD activity (attribution, not a finding):** normal release startup on
this machine restores the operator's previously configured workspace, and the accepted
MPS behavior is to **auto-load from QAD on first access** (read-only; `MpsEndpoints.cs`
"auto-loading from QAD on first access"; Stage 5/7 accepted behavior). Read-only QAD
queries occurred as part of every launch (startup scope discovery + source batch). This
is the application's own accepted startup behavior on a machine where QAD is configured
and a workspace is restored — it was not deliberately triggered by this pass, and no
refresh endpoint or other database activity was initiated. Live observation of the
transient QAD connection in flight was intentionally not pursued (would require
triggering additional database activity).

## 16. Runtime Logging

**Actual log destination (runtime-observed + repository evidence):**
`%LOCALAPPDATA%\KST\logs\kst-YYYYMMDD.log` (Serilog file sink, daily rolling, 14-file
retention; enriched with `InstanceId` only). The console sink's output (including ASP.NET
request logging) is piped to the Tauri host's sidecar stdout reader, which forwards it to
the host's `log` crate — the release host registers **no log subscriber**, so console
output is not persisted; the file sink is the durable runtime log. (Release host PE
metadata also confirms no console window: `windows_subsystem = "windows"` in release.)

**Entries generated by this pass's three release sessions were reviewed in full
(startup, safe API access, safe error requests).** Representative lines (values as
logged; no secrets present):

```
KST backend starting. Version=0.1.0-alpha.2 InstanceId=<GUID>
Now listening on: http://127.0.0.1:<port>
Application started. Press Ctrl+C to shut down.
Hosting environment: Production
Content root path: \\?\C:\Dev\kst_v2\src\tauri\target\release\binaries
KST backend listening on http://127.0.0.1:<port> (port <port>)
MPS product-line scope discovery returned 38 parts in <N>ms.
MPS source batch 1/1 (38 parts) returned 617 rows in <N>ms.
```

**Sensitive-category review (bounded conclusion):**

| Category | Found in this pass's runtime log entries? |
|---|---|
| Connection strings | **No** |
| SQL credentials / tokens / secrets | **No** |
| Customer data | **No** |
| Database/server identifiers (e.g., server host names) | **No** — query log lines contain part counts and timings only, no server name |
| Absolute developer paths | **Yes** — the `Content root path` line contains the local build path (expected on this development machine; not customer/secret data) |
| Stack traces / exception internals | **No** |
| Unexpected environment dumps | **No** |

HTTP requests made during this pass (`/health`, `/ready`, 404/405/400 probes) produced
**no request-level log entries** (ASP.NET Core request logging is at `Warning` level per
`appsettings.json` Serilog override) — so no request-path/query sensitive data is logged
for ordinary traffic. No STOP condition (credentials/secrets present) was triggered.

## 17. Safe Error Handling

**Safe malformed/nonexistent requests against the release sidecar** (no state mutation,
no database failure forced):

| Request | Status | Response body | Material headers | Sensitive exposure |
|---|---|---|---|---|
| `GET /definitely-not-a-route` | 404 | RFC 7807 Problem Details: `{"type":"…/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"…"}` | `Content-Type: application/problem+json`, `Server: Kestrel`, `Vary: Origin`; with an allowed origin also the exact-origin CORS echo | **None** — no stack trace, exception internals, filesystem paths, or connection details |
| `GET /api/v1/nope` | 404 | same Problem Details shape | as above (no Origin sent → no CORS headers) | **None** |
| `POST /health` (GET-only route) | 405 | Problem Details: `…15.5.6 … "Method Not Allowed"` + `Allow: GET` | as above | **None** |
| Malformed request line (`GARBAGE NOT HTTP`) | 400 | empty body | `Connection: close` | **None** |

These error responses generated **no new log entries** (no exception dump captured in the
runtime log).

**Boundary:** a true server-side 500 exception path (e.g., an unhandled exception during
a live database operation) was **not** exercised — forcing a production database failure
is out of scope and unsafe here. Deeper exception-response behavior (including whether an
exotic `SqlException` message could surface data-source details through Problem Details
or logs) therefore remains **Unable to Verify** in this pass (see §23).

## 18. Full Installer Boundary

- **Producers produced:** both the NSIS (`KST_0.1.0_x64-setup.exe`) and MSI
  (`KST_0.1.0_x64_en-US.msi`) installers were generated by the established release build
  (paths/hashes in §5).
- **Not executed/installed:** neither installer was run or installed in this pass, per
  the S0.7A boundary.
- **Release executable tested:** yes — all runtime evidence in this pass is from the
  release-built `kst-tauri.exe` + staged `Kst.Api.exe` (§5–§10, §15–§17).
- **Installed-package behavior:** **Unable to Verify in this pass** (no safe existing
  KST v2 installation environment was used, and installing merely to eliminate this label
  is prohibited by S0.7A). This boundary may be dispositioned during later
  S0.7/S0.8/release-hardening work.
- **Installer scope evidence (from the generated build artifacts — read-only inspection,
  nothing installed):** the generated NSIS script (`target/release/nsis/x64/installer.nsi`)
  uses per-user install by default (`RequestExecutionLevel user`; default
  `InstallDir $LOCALAPPDATA\KST`; per-machine option `$PROGRAMFILES64\KST`), installs the
  executable as `KST.exe` (`PRODUCTNAME "KST"`), carries `BUNDLEID "com.keytronic.kst"`,
  and the uninstaller removes the identifier-scoped data directories
  (`%APPDATA%\com.keytronic.kst`, `%LOCALAPPDATA%\com.keytronic.kst`). No signing
  configuration is present (consistent with accepted S0.2 §17).

**KST v1 / KST v2 coexistence observation (per project-owner steering correction):**

- **Correction of record:** the application observed running at
  `C:\Users\dgoss\AppData\Local\Keytronic\KST\KST.exe` during the early part of this pass
  (processes started 15:39 local) is **KST v1** (the legacy production application),
  **not** an installed KST v2 package. It was closed by the project owner before the
  controlled cycles ran. No KST v1 files were inspected beyond the process path/name
  already observed; no KST v1 configuration was inferred.
- **Preserved observed fact (project-owner observation):** a running **KST v1** instance
  prevented a **KST v2** release-build instance from launching due to
  single-instance / application-identity behavior. The v2-side mechanism is established
  by repository/build evidence: the single-instance plugin (v2.4.3) is registered first
  and keys on application identity (`com.keytronic.kst`); a second launch of an app with
  the same identity exits without spawning a backend (documented in
  `SIDECAR_LIFECYCLE.md` / `SETUP.md`). The KST v1 side of the identity match (its
  identifier) **cannot be established from KST v2 repository evidence** and is recorded
  as owner observation only.
- **User observation (recorded, not treated as proven):** KST v1 and KST v2 appear to
  target the same per-user installation area. KST v2 build evidence shows the v2
  per-user default is `%LOCALAPPDATA%\KST`; KST v1 was observed at
  `%LOCALAPPDATA%\Keytronic\KST` — adjacent per-user locations, with the v1 side not
  established from v2 evidence.
- The full installed-package/coexistence behavior remains **Unable to Verify** unless
  later established safely (e.g., with IT/v1 evidence or an authorized installation
  environment). **No remediation, no identifier/name/path change, and no behavior change
  was performed** in this pass. This is recorded as finding `S0.7-F001` (§20).

## 19. Repository Integrity (After Runtime Tests)

Executed after all runtime tests and before documentation updates:

| Check | Result |
|---|---|
| `git status --short` | `?? docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md` only |
| `git diff --name-status` | empty |
| `git diff --stat` | empty |
| `git diff --check` | clean |

**No runtime operation modified any tracked application source/config/dependency/build
metadata.** The regenerated `docs/openapi/Kst.Api.json` and the build-regenerated
`src/tauri/gen/schemas/*` artifacts were byte-identical to their committed versions.
Build outputs and logs remain in git-ignored locations; no binary, installer, log,
process dump, or temporary HTTP output is tracked.

**Temporary artifact cleanup:** the disposable helper script
(`%TEMP%\s07a_wmclose.ps1`) and temporary build-timestamp files were removed. Normal
ignored release build outputs were left in place (no project practice requires their
cleanup). No admitted security tool (DevSkim, cargo-audit, Gitleaks, Syft) was touched.

## 20. Findings

Exactly one new finding is created (per-checkpoint namespace `S0.7-Fxxx`, §2; no IDs
reused; no findings pre-created; no severity assigned — none authorized for this track;
nothing marked `Accepted Risk`):

### S0.7-F001 — Operational / Package-Identity Coexistence Issue / Needs Project-Owner Review

- **State:** Potential / Investigation Required (operational; **not** classified as a
  security vulnerability; **not** `Accepted Risk`).
- **Observation:** KST v1 (legacy production app, observed running from
  `%LOCALAPPDATA%\Keytronic\KST\KST.exe`) and KST v2 (this repository's application,
  identifier `com.keytronic.kst`, product/executable name `KST`,
  `tauri-plugin-single-instance` 2.4.3 registered first) **do not coexist as separate
  running applications**: per project-owner observation, a running KST v1 instance
  prevented a KST v2 launch (the second launch is intercepted by the single-instance
  behavior and exits). The identifier-scoped data directory
  `%LOCALAPPDATA%\com.keytronic.kst` is shared application identity for v2 (and, if v1
  uses the same identifier, would be shared by v1 as well — v1 side not established from
  v2 evidence).
- **Why it matters (bounded):** operators working in KST v1 (production scheduling work)
  cannot simultaneously run a KST v2 instance on the same workstation; launch attempts
  can silently focus the wrong application's window. This is an
  operational/package-identity coexistence issue affecting day-to-day use during the v1→v2
  transition, and it also shapes how future installed-v2 verification must be scheduled
  (v1 must be closed).
- **Established (v2 side, repository/build evidence):** identifier `com.keytronic.kst`;
  product name `KST`; release exe `kst-tauri.exe` / installed name `KST.exe`;
  single-instance plugin v2.4.3; NSIS per-user default `%LOCALAPPDATA%\KST` (per-machine
  `%PROGRAMFILES64\KST`); identifier-scoped data dirs `%APPDATA%\com.keytronic.kst` /
  `%LOCALAPPDATA%\com.keytronic.kst`.
- **Not established:** KST v1's identifier/configuration (not inspected, not inferred);
  whether the interception is caused by identifier equality, executable-name equality, or
  another identity mechanism shared by both apps.
- **Recommended next step (not performed in S0.7A):** project-owner/IT review to decide
  the desired v1/v2 coexistence behavior (e.g., distinct v2 application identity, or
  documented one-at-a-time operation during the transition), under a separately
  authorized change. No change was made in this pass.

No other new findings were created. In particular: the `ASPNETCORE_URLS` runtime result
is a **disposition of the existing `S0.5-F001`** (§21), not a duplicate new finding; the
loopback listener, CORS, lifecycle, logging, and error-handling observations all
confirmed the declared properties and produced no new security-relevant behavior.

## 21. S0.5-F001 Disposition

**S0.5-F001** (accepted S0.5 evidence): "operator `ASPNETCORE_URLS` override is outside
repository regression protection … Carry to S0.7."

**S0.7A runtime disposition:** the safe loopback-only precedence test (§8) **did
reproduce** the override's effect under the actual release host: with
`ASPNETCORE_URLS=http://127.0.0.1:47311` set only for the launch session, the release
sidecar's actual listener became `127.0.0.1:47311` instead of the OS-assigned dynamic
loopback port the repository-controlled path would select. In the absence of the variable
(the normal desktop path — the Tauri host sets only `ASPNETCORE_CONTENTROOT`), the
release sidecar bound to `127.0.0.1:<OS-assigned>` as designed (§7).

- The override is a **documented** environment-configuration mechanism
  (`docs/development/SETUP.md`: `ASPNETCORE_URLS` — "Override binding URL", default
  `http://127.0.0.1:0`), and the tested value remained loopback; no non-loopback
  exposure was created or tested.
- The S0.7 release-runtime evidence **confirms** that operator environment configuration
  can alter the runtime listener, so `S0.5-F001` is **retained and escalated for human
  review** (project owner / IT): the declared property "binds only to `127.0.0.1`"
  (`APPLICATION_SECURITY_PROFILE.md`) holds for the repository-controlled default path,
  but the effective binding in a deployment follows any operator-supplied
  `ASPNETCORE_URLS`. Whether that override surface is acceptable as documented, or should
  be constrained (e.g., host-side enforcement), is a human decision — **not decided and
  not remediated in S0.7A**.
- This pass does **not** claim every possible hosting override is impossible, and does
  not modify the accepted S0.5 historical evidence.

## 22. G009 Disposition

**S0.3-G009** — "Packaged (installed) runtime listener/network behavior not verifiable by
existing repository tests."

S0.7A runtime evidence established, on the **release-built (unpacked) executable**:

| G009 gate element | Result |
|---|---|
| Actual Tauri release host launched the sidecar | **Yes** (§6: host 9348 spawned sidecar 15720; handshake + `/ready` flow observed) |
| Actual sidecar listener is loopback-only | **Yes** for the default (no-env) release path (§7: `127.0.0.1:51598`, OS-assigned port) |
| No unexpected wildcard/LAN listener | **Yes** (§7: no `0.0.0.0`/`::`/LAN listener owned by any KST process) |
| Normal shutdown leaves no orphan listener | **Yes** (§9: 3/3 close events — host + sidecar exited, port released, no orphan) |
| Safe `ASPNETCORE_URLS` precedence test does not alter the intended runtime listener | **No** (§8: outcome B — the environment variable **did** alter the effective listener/port) |

**Disposition: `Partially Verified / Needs Human Review`.** The default release-runtime
listener behavior is fully evidenced as loopback-only with clean lifecycle, but because
the safe `ASPNETCORE_URLS` test demonstrated that operator environment configuration
alters the effective runtime listener, G009 is **not** marked `Covered / Resolved` in
this pass. Human review items: (a) disposition of the `ASPNETCORE_URLS` override surface
(`S0.5-F001`, §21); (b) whether the installed-package (packaged-installer) variant needs
separate listener verification before G009 can be closed for the installed form (§18
boundary). No remediation was performed.

## 23. Unable-to-Verify

The following remain **Unable to Verify** in this pass (none is `Accepted Risk`; none is
silently accepted):

1. **Dynamic WebView CSP enforcement** — the release build was constructed with the
   loopback-restricted CSP (§12, embedded-binary evidence), but live enforcement in the
   webview could not be observed without devtools/instrumentation/artifact alteration
   (out of scope).
2. **Dynamic Tauri capability enforcement** — the effective build-time ACL is
   `core:default`-only with no shell permissions (§13, build-generated artifacts), but
   runtime enforcement was not dynamically tested (would require devtools/IPC injection).
3. **Complete installed Windows-package behavior** — installers were produced but not
   executed/installed (§18); installed-package listener/CORS/CSP/capability/coexistence
   behavior is unverified.
4. **KST v1 ↔ KST v2 coexistence root cause** — the blocking behavior is observed
   (owner), the v2-side identity/plugin evidence is established, but the v1-side
   identifier/configuration was not and is not inferable from v2 evidence
   (`S0.7-F001`).
5. **True server-exception (500) response behavior** — no safe path to force a real
   server exception without production database failure; bounded error review (§17)
   covers 404/405/400 only. Whether an exotic exception message could surface
   data-source details remains untested at runtime (carried from S0.2 §20).
6. **Packet-level network behavior** — connection-level observation only (no packet
   capture, per scope).
7. **In-flight QAD connection observation** — QAD connections are per-query transient;
   not observed live to avoid initiating additional database activity.
8. **S0.7B items (not started):** actual QAD SQL Server login/group grants (server-side;
   S0.3-G010); whether `keytronicshortage` is hosted on the same legacy SQL
   infrastructure as QAD and its permission details (S0.2 §13.3/§20).

## 24. Remaining S0.7B Infrastructure Work

**S0.7B — COMPLETE / ACCEPTED — 2026-08-28** (companion document:
`docs/security/S0_7_DATABASE_INFRASTRUCTURE_PERMISSION_VERIFICATION.md`). Using the existing
safe Windows-Integrated connection path and read-only metadata queries, the pass established the
QAD runtime identity (Windows Integrated; no SQL credential path), the effective permissions of the
current principal (server role `public` only; database role `db_datareader` only; `SELECT`-only on
all 14 KST QAD tables; **no** write/DDL/admin/ownership/impersonate authority), the client-requested
transport (`Encrypt=false` legacy constraint), and the read-only / KST-owned permission-boundary
assessment (read-only **verified**; KST neither provisions nor broadens the operator's enterprise
identity authority). The broad read scope belongs to the operator's pre-existing enterprise Windows
Integrated identity (governed outside KST); the original `S0.7-F002` least-privilege-gap
interpretation is **RETIRED** (Application-vs-Enterprise Identity Scope Model Corrected; NOT
Accepted Risk; NOT a waived vulnerability; NOT evidence deletion). The keytronicshortage surface is
confirmed **unconfigured/disabled** (no current connection).

**G010 — Covered / Resolved — 2026-08-28.** `S0.3-G010` is resolved on the verified runtime
evidence (auth identity class, effective database role, effective read-only permission posture,
absence of mutation/admin authority) plus the authoritative enterprise QAD / SQL Server
configuration, which is infrastructure outside KST administration (2026-08-28 owner scope decision).
Exact administrative grant-chain reconstruction and organizational rationale are not required KST
evidence.

**Remaining (non-blocking / carried; not S0.7 work):**

- **`keytronicshortage` hosting/permission details:** Unable to Verify at runtime (integration not
  connected); to be verified when the integration is implemented and connected. Does not block S0.7.
- **Installed-package verification** if the owner authorizes a safe installation
  environment (disposition of the §18 boundary and the installed-form half of G009). Non-blocking.
- **`S0.7-F001`:** Deferred for packaging/deployment decision / Non-blocking.
- **Organizational surfaces for S0.8 (not S0.7 work):** formal IT/security disposition of
  the legacy unencrypted QAD transport (behind S0.2-F003) and the
  `ASPNETCORE_URLS` override decision (§21) will be surfaced at S0.8 closeout.

## 25. Conclusion

**S0.7A — Local Release Runtime Verification is COMPLETE / AWAITING PROJECT-OWNER
REVIEW.**

Using only the established repository build path, the current release artifact was built
(clean mutation gate; no tracked changes), and the release-built executable was launched
in three sessions (one coincidental human-operated, two controlled). Runtime-observed
evidence establishes:

- the release sidecar's actual listener is **`127.0.0.1` loopback-only** on an
  OS-assigned dynamic port, with no wildcard/LAN listener owned by any KST process;
- runtime CORS enforcement matches the accepted five-origin allowlist exactly (exact
  echo, no wildcard, no credentials, disallowed origins rejected, correct preflight
  behavior);
- normal window close terminates the sidecar with **no orphan process or listener**
  (3/3 close events, including one real user close);
- runtime logging contains no connection strings, credentials, tokens, customer data,
  server identifiers, stack traces, or environment dumps (one expected absolute
  developer path in the content-root line);
- safe error responses are Problem Details with no stack traces, paths, or connection
  internals;
- the release build was constructed with the loopback-restricted CSP (embedded-binary
  evidence) and a `core:default`-only capability/ACL surface with no shell permissions
  (build-generated artifact evidence);
- the `@tauri-apps/plugin-shell` frontend dependency remains declared but unused
  (S0.4B-F001 unchanged; no remediation).

Two items require project-owner/IT review and prevent full closure in this pass:

1. **`S0.5-F001` / G009:** the safe `ASPNETCORE_URLS` loopback test **confirmed** the
   operator environment override alters the release sidecar's effective listener
   (outcome B). G009 is therefore **Partially Verified / Needs Human Review** — not
   `Covered / Resolved`. No non-loopback exposure was tested; no remediation performed.
2. **`S0.7-F001` (new):** KST v1 / KST v2 package-identity coexistence issue
   (single-instance interception; shared application identity) — operational, needs
   project-owner review; no changes made.

**S0.7 — COMPLETE / ACCEPTED — 2026-08-28.** S0.7A (local release runtime verification) is
**COMPLETE / ACCEPTED — 2026-08-28** (`S0.5-F001` loopback-binding remediation implemented and
re-verified; `S0.3-G009` Covered / Resolved). S0.7B (database / infrastructure permission
verification) is **COMPLETE / ACCEPTED — 2026-08-28**: the local/runtime permission evidence is
complete, `S0.3-G010` is **Covered / Resolved** (verified runtime evidence + the authoritative
enterprise QAD / SQL Server configuration, which is infrastructure outside KST administration per the
2026-08-28 owner scope decision), and the original `S0.7-F002` least-privilege-gap interpretation is
**RETIRED** (Application-vs-Enterprise Identity Scope Model Corrected; NOT Accepted Risk; NOT a
waived vulnerability; NOT evidence deletion). Companion document:
`docs/security/S0_7_DATABASE_INFRASTRUCTURE_PERMISSION_VERIFICATION.md`. Remaining non-blocking /
carried items: `S0.7-F001` (Deferred for packaging/deployment decision), installed Windows-package
behavior (Unable to Verify), the QAD legacy `Encrypt=false` transport organizational disposition
(carried to S0.8), and keytronicshortage permission verification (deferred until that integration
exists). No S0.8 or Stage 9 work was performed. No security tool was installed; no dependency,
source, config, or build metadata changed; no QAD write occurred; no non-loopback exposure test was
performed; no installer was installed; no CSP/devtools instrumentation was added. (The S0.7B pass
performed read-only, metadata-only QAD permission inspection scoped to the current principal; it
changed no permissions, grants, logins, or configuration.)

## 26. Remediation — Enforce Loopback-Only Backend Binding (S0.5-F001 / S0.3-G009) — 2026-08-28

**Context.** The project owner reviewed the §1–§25 evidence (2026-08-27) and disposed:

- **S0.7A evidence — VALID / ACCEPTED AS EVIDENCE** (the original runtime results, including the
  §8 `ASPNETCORE_URLS` reproduction, are preserved unmodified as genuine historical evidence);
- **S0.5-F001 — CONFIRMED / REMEDIATION REQUIRED**;
- **S0.3-G009 — PARTIALLY VERIFIED / REMEDIATION REQUIRED BEFORE RESOLUTION**;
- **S0.7-F001 — DEFERRED for packaging/deployment decision / NON-BLOCKING** (see §26.8).

A follow-up steering correction (2026-08-28, same day) accepted the remediation design and the
post-fix runtime evidence but held formal S0.7A acceptance for two narrow corrections, both
applied and recorded here: (1) the regression test that launched the real process with an
inherited `ASPNETCORE_URLS=http://0.0.0.0:<port>` value was replaced **before acceptance**
because a future regression could have made the failing security test itself create a real
wildcard listener on the workstation — permanent security regression tests must remain safe even
when the property under test is broken (§26.3); and (2) wording stating `UseUrls` is
categorically the highest-precedence source above every ASP.NET Core URL mechanism was reduced
to what is actually established (§26.2 and the code/test comments). S0.5-F001 remediation
code, the post-fix runtime cycles (§26.6/§26.7), and all dispositions are unchanged by the
correction.

This section records the narrow security remediation and its re-verification. It supersedes the
§21/§22 *dispositions* only; the historical evidence in §7–§9, §16–§17, and §21–§22 stands as
recorded. S0.7 overall remains **IN PROGRESS**. Following the steering correction (now applied)
and final review, **S0.7A is COMPLETE / ACCEPTED — 2026-08-28** (project-owner final acceptance),
with the §26.8 dispositions recorded as accepted.

### 26.1 Root mechanism (from current source inspection)

`Kst.Api/Program.cs` (pre-fix) created the host via `WebApplication.CreateBuilder(args)` and
called `builder.WebHost.UseUrls($"http://127.0.0.1:{listenPort}")` **only when the
`ASPNETCORE_URLS` environment variable was absent**. When the variable was present, no explicit
endpoint was set, so Kestrel's URL resolution fell back to inherited hosting configuration
(`ASPNETCORE_URLS` environment variable, then the `--urls` command-line argument, then the
`urls` configuration key) — i.e., the invoker of the process took full authority over the
listener address and port. The dynamic port itself came from `--port` / `KST_PORT` / 0
(OS-assigned), and the actual bound port was communicated to the Tauri host via the documented
stdout handshake (`{port, instanceId, status}`) read from `app.Urls` after binding — a flow the
remediation does not change.

### 26.2 Code remediation (smallest authoritative change)

**File:** `src/backend/Kst.Api/Program.cs` (binding block only).

The `if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))`
guard was removed so that the explicit loopback endpoint is **always** set:

```csharp
builder.WebHost.UseUrls($"http://127.0.0.1:{listenPort}");
```

**What is established (recorded without broader framework-precedence claims):**

- KST now **unconditionally supplies its own explicit `127.0.0.1` endpoint** through
  `UseUrls` (IPv4 loopback — the existing architectural contract; no `localhost` dual-stack
  ambiguity, no IPv6 listener added), instead of the pre-fix fallback that supplied it only
  when `ASPNETCORE_URLS` happened to be absent.
- On the **actual shipped/self-contained .NET 10 release runtime**, the explicit endpoint was
  **behaviorally verified** to take effect over an inherited `ASPNETCORE_URLS` value after the
  fix (§26.7: the environment port was never the effective listener), and the repository
  regression tests (§26.3) continuously protect the property.
- Normal release behavior (§26.6) and the regression tests together establish KST's intended
  loopback invariant.

No categorical claim is made that `UseUrls` outranks every ASP.NET Core URL mechanism in all
circumstances; the invariant is established for this application, on the shipped runtime,
by verification and by continuous regression coverage.

**Preserved behavior:** port selection is unchanged (`--port` / `KST_PORT` / 0 OS-assigned
dynamic port — the application is not frozen to one port); the Tauri-host ↔ sidecar port
discovery/communication (stdout handshake → `/ready` polling → frontend URL injection) is
unchanged; CORS, CSP, capabilities, lifecycle, QAD configuration, identity, and all other
behaviors are untouched (see §26.9 non-work).

### 26.3 Regression tests (existing S0.5 binding-test area extended)

**File:** `src/backend/tests/Kst.Api.IntegrationTests/LoopbackBindingTests.cs` (the accepted
S0.5 S0.3-G002 test class — updated in place, not a disconnected convention). The launch
mechanism was factored into a shared helper that can optionally set an inherited
`ASPNETCORE_URLS` on the child process. **Failure-safe by construction** (steering
correction, 2026-08-28): no test in the class may create a wildcard or externally reachable
listener — not even in its failing state. Tests (A) and (B) launch the **real** self-contained
`Kst.Api.exe` with `--port=0`, QAD/Shortages forced unconfigured, and inspect the actual OS
TCP listener table; test (C) is a non-listening configuration-level check on the in-memory test
host (no socket is opened in any code path):

| Test | Property asserted |
|---|---|
| `Backend_Process_Binds_To_Loopback_Only` (existing) | (A) Normal effective configuration (no inherited `ASPNETCORE_URLS`): the effective listener is loopback-only on the OS-assigned dynamic port |
| `Backend_Inherited_AspnetcoreUrls_LoopbackPort_Does_Not_Take_Authority` (new) | (B) An inherited `ASPNETCORE_URLS` using a **different loopback port** does not take authority: the effective listener port is not the environment port, and the listener is loopback. The inherited value is a **loopback-only sentinel**, so even a broken build would at worst bind loopback — the test cannot expose the workstation |
| `Host_Endpoint_Selection_Supplies_Only_Explicit_Loopback_Endpoint` (new, replaces the original wildcard test) | (C) **Non-listening** configuration/endpoint-selection check: with an inherited loopback-only sentinel `ASPNETCORE_URLS` present, the KST host (in-memory `TestServer`; no socket ever opened) configures **exactly one** server address, it is the **explicit `127.0.0.1`** endpoint, and it is **not** the inherited sentinel — i.e., KST unconditionally supplies its own explicit loopback endpoint |

**Wildcard test replacement (recorded before acceptance, per steering correction):** the
original version of (C) launched the real process with an inherited
`ASPNETCORE_URLS=http://0.0.0.0:<port>` value. Although the fixed build does not create a
wildcard listener, a **future regression** could have caused the *failing test itself* to
expose a real `0.0.0.0` listener on this workstation. Permanent security regression tests must
remain safe even when the security property under test is broken, so the test was replaced
before S0.7A acceptance by the non-listening configuration-level check above. The behavioral
coverage is preserved by the safe alternate-loopback real-process test (B) — inherited URL
configuration cannot take authority over KST's selected endpoint — combined with (C)'s
assertion that the host supplies only the explicit `127.0.0.1` endpoint. No test in the
current suite requests a wildcard or non-loopback address in any code path.

**Pre-remediation failure relationship:**

- Test (B) — **demonstrated, recorded, not repeated**: with the pre-fix `Program.cs`
  temporarily restored, `Backend_Inherited_AspnetcoreUrls_LoopbackPort_Does_Not_Take_Authority`
  **FAILED** — "the effective listener port 53830 equals the inherited ASPNETCORE_URLS port
  53830" — while `Backend_Process_Binds_To_Loopback_Only` still passed (the default path was
  already loopback). The pre-fix file was then restored exactly and the tests re-passed. Per
  the steering correction, this real-process pre-fix experiment was **not repeated** when the
  test suite was corrected.
- Test (C) — **safe sensitivity check (new, 2026-08-28)**: with the pre-fix `Program.cs`
  temporarily restored, only the new non-listening test was run (in-memory host; no socket
  opened in any code path; loopback-only sentinel). It **FAILED** as intended — the test host
  reported the inherited sentinel `http://127.0.0.2:19999` as the configured endpoint, i.e.,
  the pre-fix host supplied no explicit endpoint. The pre-fix file was restored exactly and the
  tests re-passed.

### 26.4 Test results (post-fix; re-run after the 2026-08-28 steering correction)

| Command (cwd `src/backend`) | Result |
|---|---|
| `dotnet test tests/Kst.Api.IntegrationTests --filter "FullyQualifiedName~LoopbackBindingTests"` | **3/3 passed** (A, B real-process; C non-listening configuration-level) |
| `dotnet test Kst.slnx` (full backend suite) | **672/672 passed** (Domain 118, Qad 179, Application 242, Architecture 9, Api.Integration 124 — the prior 670 plus the 2 new binding tests; WebApplicationFactory-based CORS/system tests unaffected) |

Both lines were re-executed on the final corrected suite (2026-08-28, after the wildcard test
replacement); the counts are unchanged because the replacement is one-for-one (3 binding
tests before and after).

### 26.5 Rebuilt release artifact identity

Rebuilt with the established path (`scripts\build-sidecar.ps1` + `npx @tauri-apps/cli build`),
post-fix. Post-build mutation gate: only the intended tracked changes (see §26.9); the
regenerated `docs/openapi/Kst.Api.json` was byte-identical.

| Artifact | Path | Size (bytes) | SHA-256 |
|---|---|---|---|
| Tauri host (post-fix) | `src/tauri/target/release/kst-tauri.exe` | 12,647,936 | `e3a75031c245f2627fb30e36d7c6fa2beae498e2dc2cdba4c5d2004d77ae1546` |
| .NET sidecar (post-fix; publish output, externalBin copy, and staged runtime copy all byte-identical) | `publish/backend-sidecar/Kst.Api.exe` / `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe` / `src/tauri/target/release/Kst.Api.exe` | 116,315,801 | `f5b12d28368839a4b54029db75e10062e04c9666caca7c7ac0acf8110f5c3700` |

(The pre-fix identities from §5 are preserved for provenance: host `869c1c20…`, sidecar
`c1dccfa5…`.)

### 26.6 Runtime re-verification — Cycle A: normal release launch (2026-08-28 09:09 local)

Release host `kst-tauri.exe` PID 6864 launched sidecar `Kst.Api.exe` PID 23956 (handshake +
`/ready` flow observed).

- **Actual listener: `127.0.0.1:60940`** (TCP, Listen, PID 23956) — loopback-only, OS-assigned
dynamic port; corroborated by the sidecar log line `Now listening on: http://127.0.0.1:60940`.
- **No wildcard/LAN listener** owned by any KST process (full listener table inspected).
- **Lifecycle:** `WM_CLOSE` to the main window (same method as §9) → host exited, sidecar
  exited, `127.0.0.1:60940` released, **no orphan process or listener**.

### 26.7 Runtime re-verification — Cycle B: ASPNETCORE_URLS precedence (2026-08-28 09:14 local)

A fresh release process tree was launched with a **different unused loopback-only** value,
child-session scoped:

```
ASPNETCORE_URLS=http://127.0.0.1:48222
```

Port 48222 was verified free before launch; no non-loopback value was used or tested.

- Host PID 24380, sidecar PID 26032.
- **Actual listener: `127.0.0.1:54338`** (TCP, Listen, PID 26032) — the KST-selected
  OS-assigned loopback port; corroborated by the sidecar log line `Now listening on:
  http://127.0.0.1:54338`.
- **No listener on port 48222** — the inherited environment value **did not take authority**
  over KST's listener selection.
- **Result: the environment override is no longer effective** (contrast with the pre-fix
  §8 outcome B, which is preserved as historical evidence).
- **Lifecycle:** `WM_CLOSE` → host exited, sidecar exited, `127.0.0.1:54338` released, port
  48222 never bound, **no orphan process or listener**.

### 26.8 Dispositions (post-remediation)

**S0.5-F001 — Confirmed Runtime Configuration Weakness / REMEDIATED AND VERIFIED BY S0.7.**
The historical fact is preserved: pre-fix, an operator-set `ASPNETCORE_URLS` demonstrably
altered the release sidecar's effective listener (§8, reproduced at release runtime on
2026-08-27). The weakness (loss of authoritative application control over the listener
boundary) is remediated by the §26.2 change and verified by (a) the §26.3/§26.4 regression
tests — including a demonstrated failure against the pre-fix build — and (b) the §26.7
post-fix release-runtime cycle. It is not a false positive and not `Accepted Risk`.

**S0.3-G009 — Covered / Resolved** (post-remediation evidence; accepted with S0.7A —
2026-08-28). The §12-equivalent success gate is met on every element:

| Gate element | Post-remediation evidence |
|---|---|
| Release host launches the intended sidecar | Yes — Cycle A host 6864 → sidecar 23956 (§26.6) |
| Sidecar is loopback-only | Yes — `127.0.0.1:60940` (Cycle A); `127.0.0.1:54338` (Cycle B) |
| No wildcard/LAN listener exists | Yes — full listener table inspected in both cycles |
| Inherited `ASPNETCORE_URLS` no longer controls effective listener selection | Yes — Cycle B: environment port 48222 ignored; KST bound its own port; plus repository regression tests (B) and (C) |
| Normal shutdown terminates the sidecar | Yes — both cycles, `WM_CLOSE` window close |
| Listener released | Yes — both cycles |
| Repository regression tests protect the invariant | Yes — 3/3 `LoopbackBindingTests` (failure-safe by construction: no test can create a wildcard/externally reachable listener even in its failing state); pre-fix failure demonstrated for test (B); pre-fix sensitivity of the non-listening test (C) confirmed without any listener exposure |

**S0.7-F001 — Deferred (operational / package-identity coexistence issue; no remediation in
this pass).** Preserved record: KST v1 (the legacy production application at
`%LOCALAPPDATA%\Keytronic\KST\KST.exe`) was the previously running application; the owner
closed it manually before each runtime cycle; a running KST v1 instance prevented the KST v2
release-build instance from launching under the observed single-instance / application-identity
behavior. Complete v1/v2 coexistence/install behavior remains **Unable to Verify** (v1 files
not inspected or modified; v2 installer not installed; identifiers unchanged). Disposition is
deferred to a packaging/deployment decision; it does not block the G009 remediation.

**Installed-package boundary (unchanged):** complete installed Windows-package behavior —
**Unable to Verify** (no v2 NSIS/MSI execution; the owner has not authorized that risk; a
separate safe strategy/environment would be required). The G009 resolution above is for the
release-built (unpacked) executable, which is the S0.7A runtime subject.

### 26.9 Non-work (confirmed, this remediation pass)

- No CORS change. No Tauri CSP/capability change. No sidecar-lifecycle code change. No QAD
  configuration or query change. No application identity / single-instance / installer-location /
  product-name change. No frontend change. No dependency or lockfile change. No tool installed.
- No KST v1 file inspected or modified. No KST v2 installer executed or installed.
- No S0.7B work (server-side grants, `keytronicshortage` details). No S0.8 work. No Stage 9
  work.
- The only source/test change is the narrow binding fix (§26.2) and its regression tests
  (§26.3); the only documentation changes are this evidence document, `SECURITY.md`,
  `docs/status/CURRENT_PROJECT_STATUS.md`, `KST-v2-Master-Project-Checklist.md`, and the
  directly-related one-line environment-variable correction in `docs/development/SETUP.md`
  (which documented the pre-fix override as current behavior).
- No non-loopback exposure was created at any point in this remediation or correction pass.
  The original real-process wildcard test was executed once, only against the post-fix build
  (passing state — no wildcard listener was created), and was never executed against the
  pre-fix build; it was then replaced by the non-listening test before acceptance, so no
  future regression run can create a wildcard listener through the test suite. The pre-fix
  sensitivity check of the replacement test (C) was non-listening (in-memory host,
  loopback-only sentinel).

### 26.10 Conclusion (remediation pass)

The loopback-only listener is now an application-enforced invariant: the release-built sidecar
unconditionally supplies its own explicit `127.0.0.1` endpoint (dynamic or operator-selected
port) — verified on the shipped self-contained .NET 10 release runtime to take effect over an
inherited `ASPNETCORE_URLS` value, and continuously protected by the failure-safe regression
suite (real-process precedence test with a demonstrated pre-fix failure, plus a non-listening
endpoint-selection test with a confirmed pre-fix sensitivity failure) and by post-fix
release-runtime re-verification. **S0.5-F001 is remediated and verified by S0.7; S0.3-G009 is
Covered / Resolved on the post-remediation evidence. S0.7A — Local Release Runtime
Verification: COMPLETE / ACCEPTED — 2026-08-28** (project-owner final acceptance, following the
§26.3 test correction — now applied). S0.7 overall remains **IN PROGRESS** (S0.7B — PENDING /
NOT STARTED); S0.7 is **not** complete and **not** accepted.

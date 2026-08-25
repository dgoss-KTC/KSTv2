# S0.4B — Tauri Shell Capability Remediation

**Status:** COMPLETE / ACCEPTED — 2026-08-25

**Implementation date:** 2026-08-25
**Acceptance date:** 2026-08-25 (project-owner acceptance of the implemented remediation,
including owner-guided live sidecar lifecycle verification)
**Starting commit:** `a1c199d7d338817e58431b175cdbf11e96f1dbc1`
(`fix: correct QAD SQL transport configuration`)
**Finding addressed:** `S0.2-F001` (Tauri capability surface — Potential / Investigation
Required)

This document is **remediation evidence**, not normative policy. It does not replace the accepted
S0.2 baseline (`docs/security/SECURITY_BASELINE.md`) or the accepted S0.3 evidence
(`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md`). Required security properties remain
defined by `SECURITY.md` and `docs/security/`.

---

## 1. Purpose

S0.2-F001 (accepted S0.2 baseline, `SECURITY_BASELINE.md` §10/§11 and findings table) recorded:

> `shell:allow-execute` and `shell:allow-open` are granted as flat permission identifiers with no
> accompanying `scope`/allow-list entry observed in `capabilities/default.json` restricting them to
> the `Kst.Api` sidecar specifically … S0.2 did not verify (and the repository evidence alone does
> not establish) whether the `shell:allow-execute` permission as configured additionally permits
> arbitrary command execution beyond the sidecar from any frontend-reachable Tauri command.

S0.3 gate `S0.3-G004` carried the finding forward, noting that no existing test/tool/config
validation independently verifies the granted surface.

The least-privilege objective of S0.4B:

> **KST may execute only the specific external processes and operations required by its accepted
> desktop architecture** (Kst.Api sidecar lifecycle per `docs/architecture/SIDECAR_LIFECYCLE.md`),
> and no general-purpose shell/process authority is exposed by the application's Tauri capability
> configuration.

S0.4B establishes the actual shell-use inventory, determines what the exact installed
Tauri/plugin-shell version's permission model does and does not govern, reduces the granted
capability to the minimum, and adds automated regression coverage for the resulting boundary.

## 2. Starting State

### 2.1 Pre-remediation capability (`src/tauri/capabilities/default.json` at starting commit)

```json
{
  "identifier": "default",
  "description": "Default Tauri app capabilities",
  "windows": ["main"],
  "permissions": [
    "core:default",
    "shell:allow-execute",
    "shell:allow-open"
  ]
}
```

Resolved effective capability (`src/tauri/gen/schemas/capabilities.json` at starting commit):
identical permission list.

### 2.2 Installed versions (from `src/tauri/Cargo.lock`, unmodified by S0.4B)

| Crate | Version |
|---|---|
| `tauri` | 2.11.5 |
| `tauri-plugin-shell` | 2.3.5 |
| `tauri-plugin-single-instance` | 2.4.3 |

Frontend (`src/frontend/package.json`, unmodified): `@tauri-apps/api` `^2.5.0`,
`@tauri-apps/plugin-shell` `^2.2.0`.

### 2.3 Runtime shell/process call sites (complete inventory)

A targeted repository search (`shell(`, `sidecar(`, `command(`, `open(`, `spawn(`, `execute(`,
`powershell`, `pwsh`, `cmd.exe`, `taskkill`, `Command::`, `std::process`, `tokio::process`,
`Process.Start`, `ProcessStartInfo`, `child_process`, `exec`) over `src/tauri/src`,
`src/frontend/src`, and `src/backend/**/*.cs` found **exactly** the following runtime
call sites; all process execution lives in `src/tauri/src/lib.rs`. No `.NET` process-start
code exists (the `System.Diagnostics` usages in `Kst.Integrations.Qad` are `Stopwatch`).
No `child_process`/`exec` usage exists in the frontend (the only `.exec` matches are
`RegExp.exec` in a test).

| # | Call site (`src/tauri/src/lib.rs`) | API invoked | Executable | Arguments / argument source | Purpose |
|---|---|---|---|---|---|
| 1 | `launch_backend` — `app.shell().sidecar("Kst.Api")` → `.env("ASPNETCORE_CONTENTROOT", …)` → `.spawn()` | `tauri-plugin-shell` Rust API (`ShellExt::sidecar`, `process::Command::spawn`) | `Kst.Api` sidecar (declared in `tauri.conf.json` `bundle.externalBin` as `binaries/Kst.Api`; resolves to `Kst.Api-x86_64-pc-windows-msvc.exe`) | No arguments. One environment variable `ASPNETCORE_CONTENTROOT` set from `CARGO_MANIFEST_DIR` (compile-time constant, dev) or `app.path().resource_dir()` (Tauri-resolved app resource path, packaged) — both KST-controlled | Start the backend sidecar (accepted primary executable) |
| 2 | `is_process_running` (Windows `cfg`) — `tokio::process::Command::new("powershell").arg("-NoProfile").arg("-Command").arg(format!("if (Get-Process -Id {pid} -ErrorAction SilentlyContinue) {{ exit 0 }} else {{ exit 1 }}"))` | Rust direct process spawn (outside the Tauri shell plugin) | `powershell` (Windows system binary) | `-NoProfile -Command "<fixed script with {pid} interpolated>"`. `pid: u32` obtained from `child.pid()` of the sidecar Tauri itself spawned — never from frontend/user input | Poll whether the owned backend process has exited during shutdown (5 s graceful window) |
| 3 | `force_kill_process` (Windows `cfg`) — `tokio::process::Command::new("taskkill").arg("/PID").arg(pid.to_string()).arg("/T").arg("/F")` | Rust direct process spawn (outside the Tauri shell plugin) | `taskkill` (Windows system binary) | `/PID <pid> /T /F`, each a separate argument (no shell interpretation). `pid: u32` from `child.pid()` as above | Forced termination when the backend does not exit within the 5 s shutdown timeout |
| 4 | `is_process_running` / `force_kill_process` (non-Windows `cfg`) — `tokio::process::Command::new("kill").arg("-0"/"-9").arg(pid.to_string())` | Rust direct process spawn | `kill` (POSIX) | `-0`/`-9 <pid>`; `pid: u32` from `child.pid()` | POSIX equivalents (Windows is the only currently packaged target) |

Frontend (webview) Tauri usage — the complete set, from `src/frontend/src`:

- `invoke('get_backend_url')` — the app's own `#[tauri::command]`
  (`src/frontend/src/api/tauri-bridge.ts`).
- `listen('backend-ready' | 'backend-unavailable' | 'backend-terminated')`
  (`src/frontend/src/hooks/useBackendStatus.ts`).

Both are `core:*` IPC covered by the retained `core:default` permission set.

**The frontend invokes no `@tauri-apps/plugin-shell` function anywhere.** Although
`@tauri-apps/plugin-shell` is listed in `src/frontend/package.json`, a full search of
`src/frontend/src` found no import or call of it (see `S0.4B-F001` in §12).

## 3. Required Shell Operations

| Operation | Call Site | Executable/Sidecar | Argument Source | Required? |
|---|---|---|---|---|
| Spawn backend sidecar | `lib.rs::launch_backend` (Rust host) | `Kst.Api` (sole `bundle.externalBin` entry) | None (env var from KST-controlled paths only) | Yes — core of the accepted desktop architecture |
| Poll backend process exit | `lib.rs::is_process_running` (Rust host) | `powershell` | Fixed `-NoProfile -Command` script; PID interpolated as `u32` from Tauri's own spawned child | Yes — shutdown correctness (orphan prevention) |
| Force-kill backend | `lib.rs::force_kill_process` (Rust host) | `taskkill` | `/PID <u32> /T /F` as discrete arguments | Yes — shutdown fallback on timeout |
| Frontend `shell.execute` / `shell.spawn` / `shell.open` | **none** | — | — | **No** — no such call exists |

## 4. Tauri Permission Model Evidence

Determined from the exact installed versions' local evidence (no Internet documentation relied
on):

**a) Capabilities govern webview (frontend) IPC only.** In the locally cached
`tauri-plugin-shell` 2.3.5 source (`%USERPROFILE%\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\tauri-plugin-shell-2.3.5\`):

- The frontend-facing commands `execute`, `spawn`, `open`, `kill`, `stdin_write` are
  `#[tauri::command]` handlers (`src/commands.rs`). They receive the capability-injected
  `CommandScope`/`GlobalScope` and validate every call against it:
  - `prepare_cmd` → `ShellScope::_prepare` (`src/scope.rs`): the called program must match a
    configured scope entry by `name` — **no matching entry means `Error::NotFound` (denied)**.
    An empty/absent scope therefore denies all frontend `execute`/`spawn` calls, for every
    program, including sidecars. Sidecar calls additionally require the program to be present in
    `bundle.externalBin` and a scope entry with `sidecar: true`; `args: false` (the default)
    admits no arguments.
  - `open` is validated against `OpenScope` (`src/scope.rs`, `src/open.rs`); with no
    `plugins > shell > open` configuration (KST's `tauri.conf.json` has no `plugins` section),
    the default regex `^((mailto:\w+)|(tel:\w+)|(https?://\w+)).+` applies.
- **Rust-side API calls do not go through the capability scope.** `Shell::sidecar()`
  (`src/lib.rs`) constructs `Command::new_sidecar(program)` with no scope validation, and
  Rust-side `Shell::open()` calls `open::open(None, …)` — the source comment states: "when
  running directly from Rust code we don't need to validate the path" (`src/open.rs`).

Consequence for KST: call sites 1–4 in §2.3 (sidecar launch, powershell, taskkill, POSIX kill)
all execute from the Rust host and are **not** gated — and cannot be scoped — by
`capabilities/default.json`. The capability file exclusively controls what the webview may invoke
over IPC.

**b) Available shell permissions** (installed version, per
`src/tauri/gen/schemas/acl-manifests.json` and the plugin's
`permissions/autogenerated/commands/*.toml`): `shell:allow-execute`, `shell:allow-spawn`,
`shell:allow-open`, `shell:allow-kill`, `shell:allow-stdin-write` (and `deny-*` counterparts).
A permission may be granted as a bare identifier or as an object extending its scope
(`{"identifier": "…", "allow": [ …scope entries… ]}`), as defined by the generated capability
schema (`src/tauri/gen/schemas/desktop-schema.json`, definitions `Capability`/`PermissionEntry`,
which include `ShellScopeEntryAllowedArg(s)` for `name`/`cmd`/`args`/`sidecar` entries).

**c) The pre-remediation state therefore meant:** the webview was *invocably permitted* to call
the shell plugin's `execute` and `open` IPC commands, but (i) `execute`/`spawn` would have been
denied for every program anyway (no scope entries configured → `NotFound`), and (ii) `open`
would have been limited to `mailto:`/`tel:`/`http(s)://` targets by the default regex. Either
way, no application code invokes any shell-plugin IPC, so both grants were unused webview
authority.

**d) Build-time validation.** `tauri-build`/`tauri::generate_context!` parse and ACL-resolve
`capabilities/*.json` at compile time and regenerate `src/tauri/gen/schemas/*`; an invalid
permission identifier or scope fails `cargo check`/`cargo build`.

## 5. Remediation

Guided by the S0.4B priority order (remove unused permissions first; narrow scopes second;
lifecycle code changes only if required), the actual usage pattern in §2.3 means **priority 1
fully resolves the finding**: the only granted shell permissions were unused frontend IPC
grants, and the real sidecar/lifecycle execution happens in Rust host code that the capability
file does not (and cannot) scope.

Changes:

1. **`src/tauri/capabilities/default.json`** — removed `shell:allow-execute` and
   `shell:allow-open`; the capability now grants exactly `core:default` to the `main` window.
   The description was updated to state the intent (core IPC only; process lifecycle runs from
   the Rust host) so the minimal surface is self-explanatory from inspection.
   - Why removal is safe: the webview never invokes any shell-plugin command (§2.3); the
     retained `core:default` still covers the webview's actual IPC (`get_backend_url` invoke +
     lifecycle event `listen`); the Rust host's sidecar launch is unaffected because it does not
     pass through the capability ACL (§4a).
   - Why *not* a scoped `shell:allow-execute` sidecar entry: that would grant the webview a
     (narrow) frontend ability to launch the sidecar that no frontend code uses. Granting unused
     authority — even scoped — is the opposite of least privilege. The sidecar boundary is
     instead fixed where the execution actually happens (§5.2, §5.3).
2. **`src/tauri/src/lib.rs`** — smallest necessary source adjustment:
   - extracted the literal sidecar name into `const BACKEND_SIDECAR_NAME: &str = "Kst.Api"` and
     used it in `launch_backend` (`app.shell().sidecar(BACKEND_SIDECAR_NAME)`). Behavior is
     unchanged; it gives the regression test (§8) the *actual* runtime value to compare against
     `bundle.externalBin` instead of duplicating a string.
   - added `#[cfg(test)] mod capability_guard` (two regression tests, §8). Placed in the Tauri
     crate because the capability file is the IPC contract consumed by this crate and validated
     by its build; the crate already depends on `serde_json` (no new dependency). It is not
     placed in a .NET test project because it validates Tauri desktop-host configuration, not
     backend business behavior.
3. **`src/tauri/gen/schemas/capabilities.json`** — regenerated by `tauri-build` during
   `cargo check`; now records the new effective capability (permissions: `["core:default"]`).

No lifecycle redesign was made or needed (§11 of the S0.4B prompt): the existing
`SIDECAR_LIFECYCLE.md` behavior (spawn → handshake → readiness → 5 s graceful shutdown →
forced termination) is preserved exactly.

## 6. Effective Post-Remediation Capability

Checked-in capability (`src/tauri/capabilities/default.json`) and regenerated effective
capability (`src/tauri/gen/schemas/capabilities.json`) both grant:

```text
windows:    ["main"]
permissions: ["core:default"]
```

Concretely:

**The KST webview (frontend) can:**

- invoke the app's own registered commands (currently `get_backend_url`);
- listen to app events (`backend-ready`, `backend-unavailable`, `backend-terminated`)
  and use the remaining `core:default` surface — unchanged from before S0.4B.

**The KST webview can no longer:**

- invoke any Tauri shell-plugin command: `execute`, `spawn`, `open`, `kill`, `stdin_write`
  are all ACL-denied for the window. No sidecar scope, no open scope, no scope objects of any
  kind are configured.

**The KST Rust host executes (unchanged, outside the webview IPC surface):**

- exactly one sidecar: `Kst.Api` (hardcoded `BACKEND_SIDECAR_NAME`, matching the sole
  `bundle.externalBin` entry `binaries/Kst.Api`; Tauri's `new_sidecar` resolution is limited to
  declared external binaries);
- the PID-scoped `powershell` liveness poll and `taskkill /PID <pid> /T /F` fallback on
  Windows (`kill -0`/`-9` under non-Windows cfg), with PIDs derived only from the child process
  Tauri itself spawned.

## 7. Argument-Safety Review

Permitted/reviewed non-sidecar executables and argument provenance:

| Executable | Argument construction | User/frontend-controllable content? |
|---|---|---|
| `Kst.Api` (sidecar) | No arguments. `ASPNETCORE_CONTENTROOT` env var from `CARGO_MANIFEST_DIR` (compile-time) or `app.path().resource_dir()` (Tauri-resolved app resource dir) | No — KST-controlled paths only |
| `powershell` | `-NoProfile -Command "if (Get-Process -Id {pid} -ErrorAction SilentlyContinue) {{ exit 0 }} else {{ exit 1 }}"` — fixed script text; `{pid}` is a Rust `u32` (digits only) from `child.pid()` | No — PID is a process identifier owned by KST's own spawned child; no string interpolation of external data |
| `taskkill` | `/PID`, `<pid>`, `/T`, `/F` as separate `std::process` arguments (no shell interpretation); `<pid>` is `u32 → to_string()` (digits only) | No — same trusted PID source |
| `kill` (non-Windows cfg) | `-0`/`-9`, `<pid>` as separate arguments | No — same trusted PID source |

The sidecar's stdout handshake and `/ready` responses are parsed as JSON (port/instanceId) and
never enter any command/argument path. No user input, file content, database-returned string,
frontend IPC payload, or environment-controlled command text reaches any command or argument
position. **No unsafe interpolation path was identified; no user-controlled arbitrary command
execution path exists.**

## 8. Regression Coverage

New tests in `src/tauri/src/lib.rs` (`#[cfg(test)] mod capability_guard`), structured JSON
parsing (no brittle text matching), run by `cargo test` in `src/tauri`:

| Test | Properties protected |
|---|---|
| `default_capability_grants_core_default_only` | Reads and parses the checked-in `capabilities/default.json`. Fails if: any `shell:*` permission is reintroduced (bare or scoped/object form — object entries panic explicitly); any permission beyond `core:default` is added (exact-set assertion); the permission entries stop being plain identifiers; the capability stops targeting exactly the `main` window. Failure messages direct the reviewer to this document. |
| `sidecar_boundary_is_exactly_kst_api` | Reads and parses the checked-in `tauri.conf.json`. Fails if `bundle.externalBin` declares anything other than exactly `binaries/Kst.Api`, or if the runtime sidecar name (`BACKEND_SIDECAR_NAME`, the value `launch_backend` actually uses) drifts from the declared external binary. |

Mutation check performed during implementation: temporarily re-adding `shell:allow-open` to the
capability makes `default_capability_grants_core_default_only` fail with
`capability grants webview shell IPC authority KST does not use: ["shell:allow-open"]`; the
file was then restored and all tests re-passed.

This addresses S0.3 gap `S0.3-G004` (no independent verification of the shell capability's
least-privilege scope).

## 9. Verification Results

All commands run from the repository at the starting commit; no dependencies added or updated
(`--locked` where applicable; `Cargo.lock` and `package-lock.json` verified unchanged).

| # | Command (cwd) | Result |
|---|---|---|
| 1 | `cargo check --locked` (`src/tauri`) | PASS — compiles; Tauri 2.11.5 / plugin-shell 2.3.5 accept the new capability (config/ACL resolved at build time); `gen/schemas/capabilities.json` regenerated to `permissions: ["core:default"]` |
| 2 | `cargo test --locked` (`src/tauri`) | PASS — 2/2 new `capability_guard` tests pass (no pre-existing Rust tests) |
| 3 | `cargo build` (`src/tauri`) | PASS — debug build of the Tauri host succeeds with the new capability |
| 4 | `npm run typecheck` (`src/frontend`) | PASS |
| 5 | `npm run lint` (`src/frontend`) | PASS |
| 6 | `npm test` (`src/frontend`) | PASS — 281/281 tests, 14 files |
| 7 | `npm run build` (`src/frontend`) | PASS |
| 8 | Mutation check (temporary `shell:allow-open` reintroduction) | Targeted test FAILED as designed; file restored; suite re-PASS |

Not run (with reason): .NET backend `dotnet test` — no `.cs` file or backend configuration was
changed; `npx @tauri-apps/cli build` (packaged installer) — not required for this change and the
packaged-runtime sidecar lifecycle is covered by the owner-guided steps in §10; production
integration — prohibited by S0.4B scope.

Working-tree integrity (`git status --short`) after all build/test commands: only
`src/tauri/capabilities/default.json`, `src/tauri/gen/schemas/capabilities.json`,
`src/tauri/src/lib.rs` modified (plus the documentation files of S0.4B itself); no
manifest/lockfile changes.

## 10. Manual Verification

**Owner-guided — COMPLETED — 2026-08-25 — PASS.**

The project owner performed the live sidecar lifecycle verification on 2026-08-25 through the
normal development command (no production database operation was involved — backend
startup/readiness does not depend on a QAD connection). Accepted manual evidence:

1. No KST or `Kst.Api` processes were running before launch.
2. KST was launched through the normal development command. The project rebuilt and launched
   successfully.
3. The desktop window opened normally and the backend status reached Connected.
4. KST closed normally.
5. `Kst.Api` terminated with the desktop application. No orphaned `Kst.Api` process remained.
6. KST was relaunched while Task Manager was open. Both KST and `Kst.Api` were visible while
   running. Both terminated when the application was closed.

Result: **PASS**. This completes the live sidecar lifecycle acceptance gate for S0.4B. It
verifies the accepted sidecar start/stop lifecycle only; it does not verify unrelated
security/runtime properties.

## 11. Finding Disposition

**`S0.2-F001` — Resolved by accepted S0.4B remediation — 2026-08-25.**

Justification against the acceptance gates:

1. *Unused shell authority removed* — `shell:allow-execute` and `shell:allow-open` removed; the
   complete call-site inventory (§2.3/§3) shows no frontend shell-plugin usage exists or was
   needed.
2. *Required process execution explicitly restricted* — the webview has zero shell authority;
   the Rust host executes exactly the `Kst.Api` sidecar (hardcoded name, sole `bundle.externalBin`
   entry) plus the two PID-scoped Windows lifecycle commands.
3. *Argument surface bounded* — no sidecar arguments; lifecycle command arguments are fixed
   structures with a `u32` PID from KST's own child process (§7).
4. *No general arbitrary shell-execution path remains* — verified by inventory + source review;
   the webview can no longer invoke `execute`/`spawn`/`open` IPC at all.
5. *Automated configuration/build tests pass* — §8/§9 (new regression tests, `cargo
   check --locked`, `cargo test --locked`, `cargo build`).
6. *Accepted lifecycle behavior remains supported* — lifecycle code is functionally unchanged
   (name extraction only); sidecar launch does not pass through the capability ACL (§4a);
   frontend contract unchanged (frontend suite green); live end-to-end owner-guided verification
   passed (launch → connected → clean shutdown with no orphan process, plus repeat relaunch
   with both processes observed in Task Manager) — §10.

## 12. Residual Limitations and New Findings

**Residual limitations (documented, not gaps in this remediation):**

- Rust-host process launches (`sidecar("Kst.Api")`, `powershell`, `taskkill`) sit *outside* the
  Tauri capability/ACL model by design of Tauri 2; the boundary for them is the Rust source
  itself (fixed program names, fixed argument shapes, trusted `u32` PIDs), protected by the
  `sidecar_boundary_is_exactly_kst_api` test and by code review — not by a machine-enforced
  scope. This is the strongest boundary the installed Tauri security model offers for
  Rust-originated processes.
- Packaged-release runtime behavior of the sidecar lifecycle was not re-executed in S0.4B
  (owner-guided §10 covers dev mode; `npx @tauri-apps/cli build` was not part of this change's
  required verification).
- The `powershell`/`taskkill` invocations rely on the presence of standard Windows system
  binaries (unchanged pre-existing behavior; Windows is the only packaged target).

**New findings:**

- **`S0.4B-F001` — Informational.** `@tauri-apps/plugin-shell` remains an unused dependency in
  `src/frontend/package.json` (no import or call in `src/frontend/src`; the built bundle
  contains no shell-plugin usage). It grants **no** runtime IPC authority — the capability file
  is authoritative and now grants no `shell:*` permission, so even an added import could not
  invoke shell commands — but a shell-capable library in the webview dependency graph is
  unnecessary surface. Removing it requires a dependency-manifest change, which S0.4B is
  prohibited from making; its disposition (removal or explicit retention) requires separate
  authorization (e.g. an npm-dependency checkpoint or a small cleanup) and is not assigned to a
  checkpoint by this document. No severity assigned (none authorized for this track); not
  `Accepted Risk`.

## 13. Non-Work

Confirmed no out-of-scope work occurred in S0.4B:

- No QAD transport/authentication or connection-configuration change (S0.4A/S0.2-F003 territory
  untouched).
- No `keytronicshortage` change. No database query change. No `.cs` file change; .NET test suite
  not run (nothing to verify).
- No CORS/CSP change. No Tauri permission change beyond removing the two unused shell
  permissions. No frontend source change. No backend business logic change.
- No S0.4C work (npm development-tooling advisories not started). No Stage 9 work.
- No dependency manifest or lockfile change (`src/frontend/package.json`,
  `src/frontend/package-lock.json`, `src/tauri/Cargo.toml`, `src/tauri/Cargo.lock` all
  unchanged). No package/tool/plugin/tooling installed.
- Accepted evidence snapshots unmodified: `docs/security/SECURITY_BASELINE.md`,
  `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md`,
  `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md` (and other accepted S0.1/S0.2/S0.3
  documents) were not edited; their historical observations remain accurate as recorded.
- Only the authorized S0.4B file set (7 paths: capability, Rust regression support + tests,
  regenerated effective capability, this evidence document, `SECURITY.md`, canonical status
  documents) was staged, committed, and pushed in the S0.4B acceptance commit (2026-08-25).

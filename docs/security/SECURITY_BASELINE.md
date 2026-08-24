# Security Baseline

**Status:** Accepted — 2026-08-24
**Observed repository baseline:** `4b4ba3f6089321d5fd1c105c8f5762aed68c303d`
**Observation date:** 2026-08-21
**Findings correction date:** 2026-08-24 — QAD authentication/transport/authorization corrected
using project-owner/IT-provided operational authority (see §13, §14, §15, §19). No new repository
discovery was performed for this correction; the observed commit and observation date above are
unchanged.

## 1. Purpose and Scope

This document is an **observational baseline** — a record of what was directly evidenced in the
repository and local development environment on the observation date. It is not policy and not a
remediation report. Required security properties are defined by
[SECURITY.md](../../SECURITY.md) and [APPLICATION_SECURITY_PROFILE.md](APPLICATION_SECURITY_PROFILE.md);
this document records what was actually observed, which may differ from those declared
requirements. Differences are recorded as findings, not corrected here.

No application code, tests, dependencies, or configuration were changed to produce this baseline.
No vulnerability/security scanner, SBOM tool, or advisory lookup was run or performed.

## 2. Baseline Identity

| Property | Value |
|---|---|
| Branch | `main` |
| Commit | `4b4ba3f6089321d5fd1c105c8f5762aed68c303d` (`docs: enact KST security foundation`) |
| Observation date | 2026-08-21 |
| Platform observed | Windows 10.0.26200 (development workstation) |
| Working-tree state at discovery start | Clean (`git status --short` empty), local `main` == `origin/main` |

## 3. Evidence and Classification Model

Per the enacted `SECURITY_ASSURANCE_POLICY.md`, every statement below is classified as one of:

- **Declared / Required** — a property required by enacted policy or accepted architecture.
- **Observed** — directly evidenced from repository files, native tool output, or local environment
  metadata, with a citation.
- **Potential / Investigation Required** — something security-relevant was observed but its
  significance is not yet established.
- **Unable to Verify** — the property could not safely or reasonably be verified within S0.2
  boundaries.
- **Historical / Context** — evidence that explains provenance but does not describe current state.

Where observations rise to the level of a tracked finding, the enacted finding-state vocabulary
(`Confirmed`, `Potential / Investigation Required`, `Resolved`, `False Positive`, `Accepted Risk`,
`Unable to Verify`, `Informational`) is used. No item in this baseline is marked `Accepted Risk` —
AI agents cannot accept risk (see §29 of the governing prompt and `SECURITY_ASSURANCE_POLICY.md`
§12).

## 4. Application Architecture Summary

**Observed** (consistent with `docs/architecture/TECHNICAL_FOUNDATION.md`,
`BACKEND_PROJECT_BOUNDARIES.md`, `SIDECAR_LIFECYCLE.md` — not duplicated here):

- React 19 / TypeScript frontend (Vite build).
- Tauri 2 / Rust desktop host, owning the backend sidecar process.
- .NET 10 / C# ASP.NET Core backend, run as a Tauri external-binary sidecar
  (`src/tauri/tauri.conf.json` → `bundle.externalBin: ["binaries/Kst.Api"]`).
- OpenAPI-generated TypeScript client (`src/frontend/src/generated/api.ts`, generated from
  `docs/openapi/Kst.Api.json`).
- QAD SQL Server integration boundary (`Kst.Integrations.Qad`) and a Shortages integration boundary
  (`Kst.Integrations.Shortages`), both isolated from `Kst.Domain`/`Kst.Application` per
  `DependencyRuleTests.cs`.

## 5. Dependency Baseline

### 5.1 NuGet / .NET

- Manifest: `src/backend/Directory.Packages.props` (central package management,
  `ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true`).
- SDK pin: `global.json` → SDK `10.0.301`, `rollForward: latestPatch`, `allowPrerelease: false`.
- **Observed:** 15 direct `PackageVersion` entries, all exact-pinned (no version ranges):
  `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`,
  `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `Microsoft.AspNetCore.OpenApi`,
  `Microsoft.Extensions.ApiDescription.Server`, `Scalar.AspNetCore`, `Microsoft.OpenApi`,
  `Microsoft.Data.SqlClient`, `Dapper`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`,
  `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing`, `NetArchTest.Rules`, `FluentAssertions`
  (18 counting each `PackageVersion` line; several are test-only).
- **Observed:** 12 `.csproj` projects (7 main projects, 5 test projects). `Kst.Domain`, `Kst.Exports`,
  and `Kst.Integrations.Shortages` declare **zero** `PackageReference` entries (only
  `ProjectReference`), consistent with the domain-purity architecture boundary.
- No `NuGet.Config` file is present in the repository (default NuGet source configuration applies).
- No `packages.lock.json` or committed `obj/project.assets.json` exists.
- **Unable to Verify:** exact resolved/transitive dependency count and full dependency tree. This
  requires `dotnet restore`, which was not performed during S0.2 per the no-installation/no-mutation
  boundary. The original lockfile/restore output (once generated through normal development work)
  remains the authoritative source for the exact resolved tree.

### 5.2 npm / Node

- Manifest: `src/frontend/package.json`. Lockfile: `src/frontend/package-lock.json`
  (`lockfileVersion: 3`). No `.npmrc` is present.
- **Observed:** 4 direct runtime dependencies (`@tauri-apps/api`, `@tauri-apps/plugin-shell`,
  `react`, `react-dom`) and 18 direct devDependencies (build/lint/test tooling: Vite, Vitest,
  TypeScript, ESLint plugins, Testing Library, `openapi-typescript`, etc.) — all version ranges
  (caret `^`), resolved/pinned via the lockfile.
- **Observed:** `package-lock.json` contains 329 entries under `packages` (read directly from the
  existing lockfile via `node -e "require(...)"`; no install/restore performed).
- **Observed:** `package.json` contains an `allowScripts` block explicitly scoping npm's
  install-script permission to a single package (`esbuild@0.25.12: true`) rather than allowing
  install scripts broadly — a narrower-than-default posture.
- No lifecycle scripts (`preinstall`/`postinstall`) are declared directly in `package.json` itself.

### 5.3 Cargo / Rust

- Manifest: `src/tauri/Cargo.toml`. Lockfile: `src/tauri/Cargo.lock`. No `.cargo/config*` file is
  present.
- **Observed:** 6 direct `[dependencies]` (`tauri` with `tray-icon` feature, `tauri-plugin-shell`,
  `tauri-plugin-single-instance`, `serde`, `serde_json`, `tokio` with the `full` feature, `reqwest`
  with `default-features = false` and only the `json` feature enabled, `log`) plus 1
  `[build-dependencies]` entry (`tauri-build`).
- **Observed:** `Cargo.lock` contains 480 `name = "..."` entries (counted directly from the existing
  lockfile via `Select-String`; no `cargo update`/build performed).
- `reqwest` is configured with `default-features = false` + `json` only, consistent with its sole
  observed use (polling the loopback `/ready` endpoint over plain HTTP — see §9); no TLS backend
  feature is explicitly enabled.

## 6. SDK / Build / Generation Tooling

Observed via non-mutating version/info commands on the development workstation:

| Tool | Observed version | Role |
|---|---|---|
| Git | 2.52.0.windows.1 | Source control |
| .NET SDK | 10.0.301 (matches `global.json`) | Backend build/test |
| Node.js | v26.5.0 | Frontend build/test/tooling |
| npm | 11.17.0 | Frontend package management |
| Rust (`rustc`) | 1.97.1 | Tauri host build |
| Cargo | 1.97.1 | Rust dependency/build management |

Additional installed .NET runtimes (6.0.35, 8.0.30, 9.0.19, 10.0.9 — `Microsoft.NETCore.App`,
`Microsoft.AspNetCore.App`, `Microsoft.WindowsDesktop.App`) were observed via `dotnet --info`; these
are ambient workstation tooling, not repository-declared dependencies. `Cargo.toml` declares
`rust-version = "1.77.2"` as the minimum; the observed workstation `rustc` (1.97.1) exceeds it.

Repository scripts/generators identified (read-only inspection, not executed for this baseline):
`scripts/build-sidecar.ps1` (publishes the backend as a self-contained single-file sidecar; no
external network downloads observed in the script), `scripts/check-version.ps1`, npm
`generate:types` (`openapi-typescript` against `docs/openapi/Kst.Api.json`), and the Stage 3
verification sequence documented in `docs/development/BUILD_AND_TEST.md`.

## 7. Development / AI-Agent Environment

- **IDE:** VS Code 1.134.0 (`code --version`), commit `110a328ea54b42367b803ec53ee0bf52ef26b419`.
- **Observed installed extensions** (`code --list-extensions --show-versions`): `dbaeumer.vscode-eslint`,
  `esbenp.prettier-vscode`, `ms-dotnettools.vscode-dotnet-runtime`, `ms-python.debugpy`,
  `ms-python.python`, `ms-python.vscode-pylance`, `ms-python.vscode-python-envs`,
  `usernamehw.errorlens`, `yzane.markdown-pdf`.
- **Observed:** GitHub Copilot / Copilot Chat is present as a **built-in application extension**
  (`.../resources/app/extensions/copilot`) rather than a user-installed marketplace extension, which
  is why it does not appear in the `--list-extensions` output above. This is the active AI coding
  agent for this session.
- **Observed:** a `builtin` VS Code user profile directory exists
  (`%APPDATA%\Code\User\profiles\builtin`); the extension inventory above reflects the default
  profile actually in use for this session.
- **AI coding agent (this session):** GitHub Copilot Chat, operating under this repository's
  `AGENTS.md` as authoritative project instructions, consistent with
  `docs/development/KST v2 Project Instructions — Local Agent Addendum.md`'s local-agent workflow
  description (package/mode activation is human-controlled).
- **MCP servers:** the user-level MCP configuration file (`%APPDATA%\Code\User\mcp.json`) exists but
  is **empty** — no MCP servers are currently configured. No workspace-level `.vscode/mcp.json`
  exists.
- **User-level settings** (`%APPDATA%\Code\User\settings.json`, inspected structurally, no secrets
  present): notable keys include `chat.tools.urls.autoApprove` (a small allow-list of documentation
  URLs the chat tool may fetch without per-request confirmation — `code.visualstudio.com`,
  `github.com/microsoft/vscode/wiki/*`, and several `docs.rs` pages) and `gitlens.ai.vscode.model:
  "copilot:gpt-4.1"` (a GitLens AI-feature model setting). **Potential / Investigation Required:**
  the GitLens extension itself was not found in the installed-extensions list above; the setting's
  presence without a currently-installed extension is noted but not further investigated in S0.2.
- **Unable to Verify:** VS Code extensions/settings for other developers' workstations; this
  baseline reflects only the single workstation observed.

## 8. Repository Instruction / Agent Surface

- **Observed:** exactly one `AGENTS.md`, at the repository root (confirmed via workspace-wide file
  search — no nested `AGENTS.md` files exist). This matches R0's prior finding; re-verified current
  as of this baseline.
- **Observed:** no `.github/copilot-instructions.md` and no repository-level `*.instructions.md`
  files exist in the repository (confirmed via workspace-wide file search).
- **Observed:** `docs/development/KST v2 Project Instructions — Local Agent Addendum.md` — repository
  documentation describing the local-agent workflow; not an executable instruction file for any
  specific platform.
- **Observed (user-level, outside this repository):** a personal prompts folder
  (`%APPDATA%\Code\User\prompts`) contains several custom agent/prompt files (e.g.
  `debugger.agent.md`, `planner.agent.md`, `implementation-analyst.agent.md`,
  `release-captain.agent.md`, `test-engineer.agent.md`, `documentation-write.agent.md`, and a
  `sql.instructions.md` scoped to `**/*.sql`, plus several `*.prompt.md` files). These are
  **user/global** customizations, not part of this repository, and were not activated or modified
  during this baseline — recorded only for environment-surface completeness.
- No repository-level agent packages, skills, or MCP configuration were found.

## 9. Networking / Listener Baseline

- **Declared / Required:** backend must remain loopback-only (`APPLICATION_SECURITY_PROFILE.md`).
- **Observed:** `src/backend/Kst.Api/Program.cs` binds explicitly to
  `http://127.0.0.1:{port}` via `builder.WebHost.UseUrls(...)` when `ASPNETCORE_URLS` is not already
  set; the Tauri sidecar manager sets `ASPNETCORE_URLS=http://127.0.0.1:<port>` per
  `docs/development/SETUP.md`'s documented environment-variable defaults
  (`ASPNETCORE_URLS` default `http://127.0.0.1:0`). No wildcard (`0.0.0.0`/`*`) or non-loopback
  binding was found anywhere in `Program.cs` or `appsettings*.json`.
  `appsettings.json`'s `AllowedHosts` is `"localhost;127.0.0.1"`.
- **Observed:** the startup handshake (`Console.WriteLine` of `{port, instanceId, status}`) and
  `src/tauri/src/lib.rs`'s `launch_backend`/handshake-parsing logic match
  `docs/architecture/SIDECAR_LIFECYCLE.md` exactly.
- **Unable to Verify:** packaged (installed, non-development) runtime listener behavior. This
  baseline is based on static source/configuration inspection; the app/sidecar was not launched for
  this observation (per the runtime-verification boundary in the governing prompt). Packaged-runtime
  observation remains pending future, explicitly-scoped runtime verification.

## 10. CORS / CSP / Tauri Capability Baseline

### CORS

- **Observed:** `Program.cs` configures a single named CORS policy (`FrontendPolicy`) with
  `WithOrigins("http://localhost:1420", "http://127.0.0.1:1420", "tauri://localhost",
  "http://tauri.localhost", "https://tauri.localhost")`, `.AllowAnyHeader()`, `.AllowAnyMethod()`.
  `AllowAnyOrigin()` is **not** used, and `AllowCredentials()` is **not** configured. This matches
  `docs/architecture/SIDECAR_LIFECYCLE.md`'s documented origin list exactly.
- **Observed:** existing test coverage — `src/backend/tests/Kst.Api.IntegrationTests/CorsPolicyTests.cs`
  asserts the `Access-Control-Allow-Origin` response header for `http://localhost:1420` and
  `http://tauri.localhost`.
- **Unable to Verify:** packaged-runtime origin behavior (see `docs/deployment/WINDOWS_PACKAGING.md`
  §"Verification Scope Note", which already flags this as a separate, not-yet-completed verification
  item).

### CSP

- **Observed:** `src/tauri/tauri.conf.json` → `app.security.csp`:
  `default-src 'self'; connect-src http://127.0.0.1:* 'self'; style-src 'self' 'unsafe-inline'
  https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com`. `connect-src` is
  restricted to loopback (`http://127.0.0.1:*`) plus `'self'`; no remote API origins are permitted.
  `style-src 'unsafe-inline'` is present (commonly required for CSS-in-JS/inline style patterns) and
  two Google Fonts origins are allow-listed for `style-src`/`font-src`.
- No dev-vs-production CSP distinction was found in `tauri.conf.json` (a single `security.csp` value
  applies).

### Tauri Capabilities

- **Observed:** `src/tauri/capabilities/default.json` grants: `core:default`,
  `shell:allow-execute`, `shell:allow-open` (window scope: `["main"]`).
- **Potential / Investigation Required — S0.2-F001:** `shell:allow-execute` and `shell:allow-open`
  are granted as flat permission identifiers with no accompanying `scope`/allow-list entry observed
  in `capabilities/default.json` restricting them to the `Kst.Api` sidecar specifically. The
  application's actual use of shell execution (`src/tauri/src/lib.rs`'s `app.shell().sidecar("Kst.Api")`)
  appears intentionally scoped to the one declared sidecar (`externalBin` in `tauri.conf.json`), but
  S0.2 did not verify (and the repository evidence alone does not establish) whether the
  `shell:allow-execute` permission as configured additionally permits arbitrary command execution
  beyond the sidecar from any frontend-reachable Tauri command. This should be a candidate for S0.3 or
  a targeted follow-up review of Tauri v2 shell-plugin scoping semantics rather than a self-declared
  finding of vulnerability.
  Note: distinguish *dependency installed* (the `tauri-plugin-shell` crate) from *capability granted*
  (the permissions above) — both are recorded, and only the latter reflects granted capability.
  Neither Tauri updater, dialog, nor filesystem-plugin permissions were found configured in
  `capabilities/default.json`.

## 11. Process / Subprocess Baseline

- **Observed:** `src/tauri/src/lib.rs` spawns the backend exclusively via
  `app.shell().sidecar("Kst.Api")` (Tauri's shell-plugin sidecar API), setting only
  `ASPNETCORE_CONTENTROOT` (a path, not a secret) as an environment variable on the child process.
  No other environment variables are passed to the sidecar from the Tauri host in the reviewed code.
- **Observed:** three additional native subprocess calls exist for process lifecycle management,
  independent of the Tauri shell plugin/capability surface: `tokio::process::Command::new("powershell")`
  (liveness check via `Get-Process -Id <pid>`) and `Command::new("taskkill")`
  (force-termination via `/PID <pid> /T /F`), both parameterized only by the backend's own
  already-tracked process ID (a `u32` obtained from the spawned child, never derived from
  frontend/user input) — plus POSIX `kill` equivalents under non-Windows `cfg` blocks (Windows is the
  only currently packaged target). These calls are not exposed as Tauri IPC commands invocable from
  the webview/frontend.
- **Observed:** shutdown sequencing (5-second graceful wait, then forced termination) matches
  `docs/architecture/SIDECAR_LIFECYCLE.md` exactly.
- No other subprocess/shell-execution code paths were found in `src/backend/**/*.cs` (targeted
  search for `Process.Start`/`ProcessStartInfo` returned no matches).

## 12. Filesystem / Import / Export Surface

- **Observed:** `Kst.Infrastructure.Configuration.LocalAppDataPaths` resolves
  `%LOCALAPPDATA%\KST\logs` and `%LOCALAPPDATA%\KST\config` (housing `workspaces.json` and
  `preferences.json`), matching `docs/architecture/BACKEND_PROJECT_BOUNDARIES.md`.
- **Observed:** `Kst.Exports.PlaceholderExportService` is the only implementation of `IExportService`
  and performs no actual file I/O — export/import functionality (CSV/Excel/QXtend) is not yet
  implemented (Declared future capability per `BACKEND_PROJECT_BOUNDARIES.md`; nothing to observe at
  the file-I/O level yet).
- **Observed:** `.gitignore` explicitly excludes `**/appsettings.*.local.json` and `**/*.secrets.json`
  in addition to standard `bin/`/`obj/`/`node_modules/`/`target/`/`publish/` build-output
  directories — an established convention for keeping local secret overrides out of version control.
- No user-selected file/directory access (open/save dialogs, drag-drop import) was found in the
  current frontend or backend source; this is consistent with Stage 8's informational-only scope.

## 13. Credential / Configuration Baseline

Mechanisms and key/setting **names** only — no values were read, printed, or recorded.

- QAD connection: `Kst.Integrations.Qad.Options.QadConnectionOptions`, bound from configuration
  section `QadDatabase` (keys: `Server`, `Database`, `ConnectTimeoutSeconds`,
  `CommandTimeoutSeconds`, `Encrypt`, `TrustServerCertificate`, `MaxPartBatchSize`).
  `Kst.Integrations.Qad.QadConnectionStringFactory` builds the connection string using
  `SqlConnectionStringBuilder` with **`IntegratedSecurity = true`** — Windows-integrated
  authentication; no username/password fields exist anywhere in this path (confirmed by source
  inspection and by a targeted search for `Password=`/`User ID=`, which found none).
- Shortages connection: `Kst.Integrations.Shortages.Options.ShortagesConnectionOptions`
  (currently unconfigured/disabled — `DisabledShortagesConnectivityCheck` is wired by default).
- Configuration source: standard ASP.NET Core configuration layering
  (`appsettings.json` → `appsettings.{Environment}.json` → environment variables), per
  `Kst.Api/appsettings.json`/`appsettings.Development.json`.
- `appsettings.json`'s committed `QadDatabase` section contains only a server hostname and database
  name (`Server`/`Database`), not credentials; this is an already-tracked, non-secret configuration
  value under the existing repository convention (`*.local.json` is the designated override path
  for anything that should not be committed).
- No environment-variable dump, credential-store inspection, or secret value of any kind was
  performed or recorded, per the governing prompt's secret-handling rule.

### 13.1 QAD Database — Authentication and Access (Operator/IT-Provided, 2026-08-24)

The following operational authority was provided by the project owner/IT and is recorded here as
authoritative context, not independently re-derived from repository inspection:

- **Authentication:** Windows Integrated Authentication only. Credentials are the logged-in
  user/domain identity.
- **SQL username/password authentication is prohibited** for QAD access (`KNWVM13`/`QADPRO2` and
  related QAD databases).
- **Access:** read-only / least privilege is the required and asserted operational posture for
  ordinary QAD access.
- This is consistent with the repository-observed `IntegratedSecurity = true` configuration in
  `QadConnectionStringFactory` (see above) — the operator-provided authentication policy and the
  observed code configuration agree.
- Exact database-level grants for the QAD login were not independently inspected during S0.2 (see
  §20 Unable-to-Verify) — this operator statement establishes the *required* and *asserted* access
  level; it does not substitute for an independent grant inspection.

### 13.2 QAD Database — Transport (Operator/IT-Provided, 2026-08-24) — Confirmed S0.2-F003

- **Confirmed:** the current QAD SQL infrastructure does not support encrypted SQL connections from
  existing supported clients. The required current connection behavior is therefore
  **`Encrypt=false`**, stated explicitly rather than left to client-library defaults.
- **Confirmed:** `TrustServerCertificate=true` is **not** the expected configuration and must not be
  treated as a substitute for disabling encryption. With `Encrypt=false`, certificate trust is not
  applicable.
- This is a legacy infrastructure constraint, not the desired security end state. Target future
  state, if QAD SQL infrastructure gains TLS support: `Encrypt=true` / `TrustServerCertificate=false`.
  Use of `TrustServerCertificate=true` under an encrypted configuration would require a separately
  documented exception.
- **Repository-observed configuration** (`QadConnectionOptions.cs` defaults: `Encrypt=true`,
  `TrustServerCertificate=true`) does not match this operator-confirmed required configuration
  (`Encrypt=false`). See finding `S0.2-F003` in §19 for the reconciled finding record.
- **Formal IT/security risk acceptance of the current unencrypted QAD SQL transport has not yet been
  established.** This is not marked `Accepted Risk` in this baseline.
- QAD access must remain restricted to the internal corporate network (see §15).

### 13.3 keytronicshortage Database (Operator/IT-Provided, 2026-08-24)

Recorded separately from QAD per operator/IT authority — **not inspected, connected to, or verified
during S0.2**:

- **Authentication:** SQL authentication (distinct from QAD's Windows Integrated Authentication).
- **Credentials:** a dedicated KST application account.
- **Secret handling:** external configuration / secret storage (not committed to the repository).
- **Access:** explicitly scoped application permissions.
- **Hosting relationship to QAD's legacy SQL infrastructure (`KNWVM13`) is not established by S0.2
  evidence.** If `keytronicshortage` is hosted on the same legacy infrastructure, the same transport
  limitation (§13.2) may apply — this is stated conditionally and is **not** asserted as a transport
  conclusion here. `Kst.Integrations.Shortages` is currently unconfigured/disabled in the repository
  (see above), so there is no current connection-string configuration to compare against.

## 14. Database / Authoritative-System Baseline

- **Declared / Required:** production database access must remain read-only; no direct
  `INSERT`/`UPDATE`/`DELETE`/`MERGE` (`APPLICATION_SECURITY_PROFILE.md`, `SECURITY_ASSURANCE_POLICY.md`).
- **Observed:** a targeted source search across `src/backend/**/*.cs` for
  `INSERT INTO|UPDATE ... SET|DELETE FROM|MERGE` found **zero** matches other than a single
  unrelated code comment (`QadBomReaderTests.cs`, the English word "merge" in prose). No
  write-verb SQL was found anywhere in `Kst.Integrations.Qad` or `Kst.Integrations.Shortages`.
- **Observed:** all QAD queries use `Microsoft.Data.SqlClient`/Dapper via
  `QadConnectionFactory.OpenAsync`, consistent with the architecture-documented adapter pattern.
- **Operator/IT-provided authority (2026-08-24):** ordinary QAD access uses Windows Integrated
  Authentication and is required/asserted to be read-only / least privilege; SQL-authenticated
  access is prohibited for QAD (§13.1). This corroborates, at the authentication-policy level, that
  read-only access is an operationally-enforced expectation and not merely an application-code
  convention.
- **Retired — S0.2-F002 (previously "Potential / Investigation Required"):** the prior draft of this
  baseline recorded a gap on the basis that read-only behavior was evidenced only at the
  application-code level. That implication is retired: operator/IT authority (§13.1) confirms QAD
  access is required to be read-only/least-privilege and that SQL-authenticated access (which could
  otherwise carry broader write privilege) is prohibited for QAD. This finding is not preserved
  merely to keep the numbering occupied — see §19.
- **Unable to Verify:** the exact database-level grants held by the specific QAD Windows-integrated
  login/group used by KST (server-side configuration outside the repository and outside safe S0.2
  scope — no live database connection was made). The operator-provided access policy in §13.1
  establishes the *required* posture; it does not substitute for an independent grant inspection.
- Shortages integration: currently disabled (`DisabledShortagesConnectivityCheck`); no query surface
  exists yet to evaluate. See §13.3 for operator-provided `keytronicshortage` authentication/access
  context.

## 15. External Destination Baseline

- **Runtime application destinations (Observed):** the QAD SQL Server configured in
  `Kst.Api/appsettings.json` (`QadDatabase:Server`/`QadDatabase:Database` — an internal hostname and
  database name, not reproduced repeatedly here); no other outbound runtime destination was found in
  application source (Shortages is currently disabled/unconfigured).
- **Operator/IT-provided authority (2026-08-24):** QAD access must remain restricted to the internal
  corporate network. The current QAD SQL infrastructure does not support encrypted client
  connections (§13.2); network-level restriction to the internal corporate network is therefore a
  required compensating control, not an optional convenience.
- **Conditional, unconfirmed:** if `keytronicshortage` is hosted on the same legacy SQL
  infrastructure (`KNWVM13`) as QAD, the same unencrypted-transport limitation described in §13.2 may
  apply to it as well. This hosting relationship has **not** been established by S0.2 evidence
  (`Kst.Integrations.Shortages` is currently unconfigured/disabled) and no transport conclusion is
  drawn for `keytronicshortage` in this baseline.
- **Development/package-manager destinations (Observed, by ecosystem convention, not directly
  queried):** npm's default registry, NuGet's default feed (nuget.org), and crates.io are the
  implied package sources given the absence of `.npmrc`, `NuGet.Config`, or `.cargo/config*`
  overrides. No live network requests were made to verify reachability, per the runtime-verification
  boundary.
- **Unable to Verify:** any additional outbound destinations that might only manifest at packaged
  runtime; whether `keytronicshortage` shares QAD's SQL infrastructure.

## 16. Logging / Diagnostics Baseline

- **Observed:** Serilog (`Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`)
  writes to the console and to `%LOCALAPPDATA%\KST\logs\kst-.log` (daily rolling, 14-file retention),
  enriched only with `InstanceId` (an application-generated GUID) — not with request bodies,
  headers, or connection strings.
- **Observed:** a targeted search for logging statements referencing "Connection" found no matches;
  no log call was found that appears to log a connection string, credential, or authorization
  header.
- **Observed:** sidecar stdout/stderr lines are forwarded into the Tauri host's `log` crate output
  (`info!("KST Tauri [backend stdout]: {line}")` etc.) — i.e., anything the backend process writes to
  stdout/stderr is also captured by the Tauri host's logging. The backend's only intentional stdout
  write is the single JSON startup handshake line (port/instanceId/status); ASP.NET Core's own
  request logging goes through Serilog, not raw stdout.
- **Unable to Verify:** whether any currently-unexercised code path could log a full exception
  containing connection-string details (e.g., a `SqlException.Message` in some driver versions can
  include the data source). No such occurrence was found in the reviewed source, but this was not
  exhaustively tested at runtime.

## 17. Packaging / Deployment Baseline

- **Observed:** `src/tauri/tauri.conf.json` → `bundle.externalBin: ["binaries/Kst.Api"]` and
  `bundle.resources: ["binaries/appsettings.json", "binaries/appsettings.Development.json"]`;
  `bundle.targets: "all"`.
- **Observed:** `scripts/build-sidecar.ps1` publishes the backend as a self-contained,
  single-file win-x64 executable (`dotnet publish ... --self-contained true /p:PublishSingleFile=true
  ... -r win-x64`) into `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`; no external network
  downloads occur in this script.
- No code-signing configuration was found in `tauri.conf.json` (no `bundle.windows.certificateThumbprint`
  or equivalent signing key present).
- No updater plugin/configuration was found (no `tauri-plugin-updater` dependency, no `updater`
  section in `tauri.conf.json`, and no updater permission in `capabilities/default.json`).
- `docs/deployment/WINDOWS_PACKAGING.md` already documents that packaged installer/runtime behavior
  is a separate, not-yet-completed verification item — consistent with this baseline's own
  packaged-runtime "Unable to Verify" items above.

## 18. Existing Security-Relevant Verification

Existing tests/checks that already exercise security-relevant properties (inventory only — none were
re-run as a "security check" for this baseline; normal repository test execution is part of the
already-authorized development workflow and is not itself S0.2/S0.3 work):

| Test / Check | Location | Property protected |
|---|---|---|
| `DependencyRuleTests` (`Domain_Does_Not_Reference_Infrastructure`, `..._AspNetCore`, `..._SqlServer`, `..._Api`, and related) | `src/backend/tests/Kst.ArchitectureTests/DependencyRuleTests.cs` | Domain/Application isolation from ASP.NET Core, SQL Server client libraries, and the API host — enforces the architecture boundary that keeps SQL/infrastructure concerns out of business-rule code. |
| `VersionConsistencyTests` | `src/backend/tests/Kst.ArchitectureTests/VersionConsistencyTests.cs` | Version-string consistency across backend/Tauri/frontend (supply-chain/release hygiene, not a direct security control). |
| `CorsPolicyTests` (`GetHealth_WithAllowedOrigin_ReturnsCorsHeader`, `..._WithPackagedTauriOrigin_...`) | `src/backend/tests/Kst.Api.IntegrationTests/CorsPolicyTests.cs` | CORS allowed-origin behavior for the frontend/Tauri origins. |
| .NET built-in analyzers (`EnableNETAnalyzers=true`, `AnalysisLevel=latest` in `Directory.Build.props`) | `src/backend/Directory.Build.props` | Roslyn's built-in analyzer set (includes some CA-prefixed diagnostics; not a dedicated SAST/security tool). |
| ESLint (`@eslint/js` recommended + `typescript-eslint` recommended + React hooks/refresh rules) | `src/frontend/eslint.config.js` | General code-quality lint; **no dedicated security-lint plugin** (e.g. `eslint-plugin-security`) is configured. |

## 19. Findings / Observations

| ID | State | Area | Observation | Evidence | Required Property | Next Step |
|----|-------|------|-------------|----------|--------------------|-----------|
| S0.2-F001 | Potential / Investigation Required | Tauri capability surface | `shell:allow-execute`/`shell:allow-open` granted without an observed scope restricting execution to the `Kst.Api` sidecar | `src/tauri/capabilities/default.json`; usage site `src/tauri/src/lib.rs` (`app.shell().sidecar("Kst.Api")`) | Shell/process execution capability should be no broader than the application's actual sidecar-execution need | S0.3 or a targeted follow-up: confirm Tauri v2 shell-plugin scoping semantics for these permissions and add an explicit scope if the default is broader than intended |
| ~~S0.2-F002~~ | Retired (2026-08-24) | Database access | *Retired.* Previously recorded a gap on the basis that read-only DB access was evidenced only at the application-code level. Operator/IT-provided authority (§13.1) confirms QAD access is required to be read-only/least-privilege and that SQL-authenticated access is prohibited for QAD, corroborating read-only behavior at the authentication-policy level. Not preserved merely to keep the numbering occupied. | §13.1 (operator/IT-provided authority, 2026-08-24) | n/a (retired) | None — exact database-level grants remain a separate Unable-to-Verify item (§20), independent of this retirement |
| S0.2-F003 | **Confirmed** | QAD SQL transport configuration | The current KST QAD connection configuration uses `TrustServerCertificate=true` (with `Encrypt=true`). Verified required configuration is `Encrypt=false`, because the current QAD SQL infrastructure does not support encrypted SQL connections from supported clients; `TrustServerCertificate=true` is not the expected substitute and is unnecessary when encryption is disabled. Current application configuration does not accurately express the verified legacy transport requirement and could obscure the actual security state of the connection. | `src/backend/Kst.Integrations.Qad/Options/QadConnectionOptions.cs` (`Encrypt=true`, `TrustServerCertificate=true` defaults); operator/IT-provided authority §13.2 (2026-08-24) | Connection configuration should accurately express the verified transport requirement (`Encrypt=false`); Windows Integrated Authentication, internal-network restriction, and read-only/least-privilege access must be preserved | In an explicitly authorized remediation checkpoint: configure QAD connections with `Encrypt=false` and remove/disable `TrustServerCertificate=true`. Future target if QAD SQL gains TLS support: `Encrypt=true` / `TrustServerCertificate=false`. Not implemented during S0.2. Severity not assigned; the underlying unencrypted-transport constraint is not classified `Accepted Risk` — formal IT/security risk acceptance remains unresolved. |

`S0.2-F002` is retired per operator/IT-provided authority (§13.1/§14) and is not carried forward as
an open finding. `S0.2-F003` is reclassified from `Informational` to `Confirmed` per operator/IT
correction (§13.2) — the *configuration-vs-requirement mismatch* is confirmed; the underlying
unencrypted-transport constraint itself remains an unresolved IT/security matter, not an
`Accepted Risk`. `S0.2-F001` is unchanged.

## 20. Unable-to-Verify Items

- Exact resolved/transitive NuGet dependency tree (no lockfile/restore artifact; `dotnet restore` not
  performed during S0.2).
- Packaged (installed, non-development) runtime listener/CORS/CSP/Tauri-capability behavior — the
  app/sidecar was not launched for this baseline.
- Actual QAD SQL Server account/login permissions and grants (server-side configuration, outside
  repository scope; no live database connection made). Operator/IT-provided authority (§13.1)
  establishes the *required* read-only/least-privilege posture; it does not substitute for an
  independent grant inspection.
- Whether `keytronicshortage` is hosted on the same legacy SQL infrastructure (`KNWVM13`) as QAD, and
  therefore whether the same unencrypted-transport constraint (§13.2) applies to it (§13.3/§15).
- Machine-level credential protection (Windows Credential Manager, disk encryption, etc.) — outside
  the scope of a repository/source-code security baseline and not inspected per the secret-handling
  boundary.
- Provider-side AI data retention/handling behavior for GitHub Copilot or any other AI service used
  in development — an organizational/vendor question, not something observable from this repository.
- Runtime outbound network destinations beyond the statically-configured QAD server (no live traffic
  was generated or observed).
- Whether any exception path could incidentally log connection-string details at runtime (source
  review found no such call; not exhaustively runtime-tested).

## 21. Candidate S0.3 Existing-Tool Checks

Based strictly on tooling already present (nothing new proposed for installation):

- `dotnet build`/`dotnet test` with the existing `EnableNETAnalyzers`/`AnalysisLevel=latest`
  Roslyn analyzer configuration — already runs today as part of the Stage 3 verification sequence;
  S0.3 could evaluate what security-relevant diagnostics this already surfaces.
- `npm run lint` (ESLint) — already part of the verification sequence; S0.3 could evaluate current
  coverage/gaps for security-relevant JS/TS patterns.
- `cargo check`/`cargo build` — already part of the verification sequence; no Cargo-specific audit
  tool is currently installed, so nothing beyond compilation is currently evaluated for the Rust
  crate graph.
- The existing `CorsPolicyTests`/`DependencyRuleTests` architecture/integration tests — S0.3 could
  formally register these as part of a "security-relevant test" category rather than ordinary
  functional tests.

No ecosystem-native audit command (`dotnet list package --vulnerable`, `npm audit`, `cargo audit`)
was run during S0.2. `cargo audit` is not currently installed (no `cargo-audit` binary observed and
not installed for this baseline). S0.3 should decide, under the enacted dependency-admission policy,
which of these to actually execute and how to interpret results.

## 22. Baseline Conclusion

Well-evidenced from static repository inspection: backend loopback binding, the configured CORS
origin list, the Tauri CSP `connect-src` restriction, the absence of write-verb SQL in the QAD/
Shortages integration layers, Windows-integrated (credential-less) QAD authentication, the
established local-secrets `.gitignore` convention, the single-root `AGENTS.md`/absence of other
instruction-file surfaces, and the currently-empty MCP configuration.

Requires S0.3 verification: which existing ecosystem/analyzer/lint capability already provides
useful security signal, and how the remaining `Potential / Investigation Required` finding
(S0.2-F001 Tauri shell-capability scope) should be followed up.

Remains unable to verify from this repository alone: packaged-runtime network/listener behavior,
actual QAD account permissions, whether `keytronicshortage` shares QAD's legacy SQL infrastructure,
and any AI-provider data-handling/retention posture.

Operator/IT-provided authority (2026-08-24) reconciled two items in this baseline:
`S0.2-F002` (database read-only enforcement) is retired — QAD's Windows Integrated Authentication
plus a prohibition on SQL-authenticated access corroborates read-only/least-privilege access at the
authentication-policy level. `S0.2-F003` (QAD transport configuration) is reclassified to
`Confirmed` — the repository-observed `TrustServerCertificate=true` configuration does not
accurately express the verified legacy-infrastructure requirement (`Encrypt=false`), which is a
configuration-accuracy finding, not a newly-discovered vulnerability. The underlying constraint
(QAD SQL infrastructure does not support encrypted connections) is a legacy-infrastructure fact, not
an `Accepted Risk` — formal IT/security risk acceptance of the unencrypted transport has not yet
been established, and no severity is assigned pending that review. No material finding in this
baseline is treated as urgent owner/IT-security escalation on its own beyond what is already flagged
above; this baseline does not declare the application secure — it records what could currently be
observed plus the operator/IT-provided operational authority incorporated on 2026-08-24.

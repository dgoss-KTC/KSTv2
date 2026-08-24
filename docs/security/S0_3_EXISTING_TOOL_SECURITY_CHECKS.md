# S0.3 — Existing-Tool Security Checks

**Status:** COMPLETE / ACCEPTED — 2026-08-24
**Check date:** 2026-08-24
**Check execution commit:** `18fdc8471e25122daa278b00fc03c681161a0330`
**S0.2 observational application baseline:** `4b4ba3f6089321d5fd1c105c8f5762aed68c303d`

This document is **evidence, not policy**. It records what the repository's existing toolchain
could check on the check date, what it found, and what it cannot check. Required security
properties remain defined by `SECURITY.md` and `docs/security/`
(especially `SECURITY_ASSURANCE_POLICY.md` and `APPLICATION_SECURITY_PROFILE.md`); the
observational baseline remains `docs/security/SECURITY_BASELINE.md`, which was **not modified**
by this pass.

## 1. Purpose and Scope

S0.3 answers one question:

> What can the KST repository's existing toolchain independently check right now?

It does **not** answer which new security products KST should adopt, and it does **not**
authorize fixing what the checks find. For this pass:

- **Existing tools only.** Checks were limited to (a) existing repository tests, (b) existing
  project compilers/analyzers/linters, (c) ecosystem-native package-manager advisory
  functionality of the already-installed toolchain (the only authorized external advisory
  queries — these may contact the already-configured package registries' advisory sources), and
  (d) simple read-only Git/native repository checks.
- **No tool installation.** No NuGet/npm/Cargo packages, scanners, secret scanners, SAST tools,
  SBOM generators, agent packages, skills, MCP servers, binaries, or helper scripts were
  installed or activated. PATH was not modified.
- **No remediation.** No finding was remediated. No package was updated. No configuration or
  security control was changed. No `npm audit fix`, `npm update`/`npm install`, `dotnet add
  package`, `cargo update`/`cargo install`, or version edit was performed.
- **No production access.** No database connection was made, no SQL was executed, and no
  application/sidecar was launched for runtime checking.
- **No Stage 9 work.**

Findings, dispositions, and remediation are kept separate: a finding can be verified without
being remediated, and a passing check does not prove general security.

## 2. Check Identity

| Property | Value |
|---|---|
| Branch | `main` |
| Check execution commit | `18fdc8471e25122daa278b00fc03c681161a0330` (`docs: accept S0.2 security baseline`) |
| Check execution date | 2026-08-24 |
| S0.2 observational application baseline | `4b4ba3f6089321d5fd1c105c8f5762aed68c303d` (observation date 2026-08-21 — preserved, not rewritten) |
| Working-tree state at start | Clean (`git status --short` empty); local `main` == `origin/main` |
| Working-tree state at end | Clean — no check modified tracked project state (verified after each check group) |

Environment/tool versions observed at check time (no new tooling installed):

| Tool | Version | Role in this pass |
|---|---|---|
| Git | 2.52.0.windows.1 | Sentinel search, working-tree verification |
| .NET SDK | 10.0.301 | Build/analyzers, tests, `dotnet package list --vulnerable` |
| Node.js | v26.5.0 | Frontend tooling host |
| npm | 11.17.0 | `npm audit`, lint/typecheck/test scripts |
| rustc / Cargo | 1.97.1 | Tauri crate compile/lint/test, lockfile integrity |
| Clippy (rustup component `clippy-x86_64-pc-windows-msvc`) | present (rustc 1.97.1 toolchain) | `cargo clippy --locked --offline` |
| rustfmt (rustup component) | present | not executed (not a security check) |

Existing build state was warm at check start (from prior normal development work at this
commit): `src/backend/**/obj/project.assets.json` and build binaries present for all 12 .NET
projects, `src/frontend/node_modules` installed, Cargo registry cache and `src/tauri/target`
present. This is why the authorized checks below could run with `--no-restore`/`--locked
--offline` (no restore, no fetch, no update).

**Execution cost:** low — all checks completed in minutes on the warm build state; the heaviest
steps were the full backend test run (~1 minute after a warm build) and the frontend Vitest run
(~35 seconds). The only outbound network access performed was by the two authorized
ecosystem-native advisory queries (the NuGet vulnerability data via `dotnet package list
--vulnerable`, and the npm advisory database via `npm audit`), each against the
ever-already-configured registry. No other external service was contacted.

## 3. Check Classification Model

Every executed check is classified by security strength:

- **Direct security check** — the check's primary purpose is a security property (dependency
  advisory status, CORS behavior, secret presence, architectural isolation that enforces a
  security-relevant boundary).
- **Security-supporting check** — the check protects general correctness/hygiene that security
  relies on but is not itself a dedicated security control (version/packaging consistency,
  compiler/analyzer/lint static checks, parameterized-SQL shape tests).
- **General quality check** — the check's purpose is code quality with no dedicated security
  aim (UI component behavior tests, type checking).

Pass/fail wording is bounded throughout:

- A native advisory check reporting "no advisories" means *no known advisories for the
  dependency graph it was able to evaluate at the time of execution* — not that dependencies are
  secure.
- A passing test proves the tested behavior, not the general security of the property.
- Tool/upstream-reported severities are quoted as **npm/tool-reported severity**, never as KST
  risk severity. No KST risk-severity framework exists or was invented by this pass (final
  severity thresholds remain an intentionally unresolved policy area per
  `SECURITY_ASSURANCE_POLICY.md` §"Intentionally Unresolved Policy Areas").

Finding states use the enacted vocabulary of `SECURITY_ASSURANCE_POLICY.md` §"Security Finding
States" (`Confirmed`, `Potential / Investigation Required`, `Resolved`, `False Positive`,
`Accepted Risk`, `Unable to Verify`, `Informational`). No item in this document is marked
`Accepted Risk`; AI agents cannot accept material security risk.

## 4. Existing Check Capability Matrix

| Area | Existing capability | Executed? | Result | Security strength | Gap |
|---|---|---|---|---|---|
| NuGet advisory | .NET 10 SDK `dotnet package list --vulnerable --include-transitive --no-restore` (evaluates existing `project.assets.json` against the NuGet vulnerability data of the already-configured feed) | Yes | No vulnerable packages reported for any of the 12 projects, including transitive dependencies | Direct security check | No committed NuGet lockfile — the checked graph is the one last restored on this workstation |
| npm advisory | npm 11 `npm audit --json` (evaluates `package-lock.json` against the npm advisory database) | Yes | **3 advisories** (npm-reported: 2 high, 1 moderate) — all in development-only packages. See §5.2 and finding `S0.3-F001` | Direct security check | — |
| Cargo advisory | None installed (`cargo-audit`/`cargo-deny` absent; confirmed read-only). `cargo check`/`clippy`/`tree`/`metadata` are not vulnerability-database checks | No (no authorized/available scanner) | Not executed — recorded as gap `S0.3-G001` | Gap | No currently authorized/available Rust dependency advisory scanner was found |
| Architecture boundaries | `DependencyRuleTests` (NetArchTest) in `Kst.ArchitectureTests` | Yes | 6/6 passed | Security-supporting / architecture boundary check | — |
| Version/packaging consistency | `VersionConsistencyTests` (3) + `scripts/check-version.ps1` | Yes (tests) | 3/3 passed | Security-supporting (packaging/version consistency; **not** a vulnerability check) | — |
| CORS | `CorsPolicyTests` (2) in `Kst.Api.IntegrationTests` (in-memory `WebApplicationFactory`) | Yes | 2/2 passed — verifies origin echo for 2 of the 5 configured origins only. See §6.3 | Direct security check | Partial origin coverage; `AllowAnyOrigin`-absence and no-credentials properties are not asserted by tests |
| Loopback binding | Statically observed only (`Program.cs` `UseUrls("http://127.0.0.1:{port}")`); no test exists | No (no test to run) | Coverage gap `S0.3-G002` | — | Loopback binding is statically observed but lacks identified independent test coverage |
| CSP | Statically observed only (`tauri.conf.json` `app.security.csp`); no test/script exists | No (no check to run) | Coverage gap `S0.3-G003` | — | CSP is statically observed but lacks identified independent automated verification |
| Tauri capabilities / shell least privilege | Statically observed only (`capabilities/default.json`); no test or config-validation tool exists | No (no check to run) | Coverage gap `S0.3-G004`; finding `S0.2-F001` remains `Potential / Investigation Required` | — | No identified independent least-privilege verification for the granted shell capabilities |
| Database write safety (SQL) | `Kst.Integrations.Qad.Tests` (173 tests) assert parameterized SQL *shape*; no test asserts absence of write-verb SQL | Yes (as part of full suite) | 173/173 passed | Security-supporting (parameterized-SQL shape; not a read-only enforcement test) | Gap `S0.3-G005`: no existing test enforces read-only SQL behavior |
| Database grants (server-side) | None in repository tooling; no live connection authorized | No | Unable to verify (also in S0.2 §20) | — | Gap `S0.3-G010`: grant verification requires server-side/IT action outside repository tooling |
| .NET analyzers | Built-in Roslyn analyzers: `EnableNETAnalyzers=true`, `AnalysisLevel=latest` in `src/backend/Directory.Build.props` | Yes (via `dotnet build --no-restore`) | 0 warnings, 0 errors. No security-specific diagnostic appeared in the build output | Security-supporting (built-in analyzer set; **not** full SAST) | Gap `S0.3-G006`: no dedicated SAST capability exists |
| Frontend lint / typecheck | ESLint (`@eslint/js` recommended + `typescript-eslint` recommended + React hooks/refresh; **no security plugin**) + `tsc --noEmit` | Yes | Lint clean (0 errors/0 warnings under `--max-warnings 0`); typecheck exit 0 | General quality check (ESLint without a security plugin is **not** a dedicated security scanner); typecheck is security-supporting / general correctness | No dedicated security-lint plugin configured |
| Frontend tests | `npm test` (Vitest, 14 test files) | Yes | 281/281 passed. No security-relevant assertions identified (no loopback/CORS/CSP/secret coverage in test names/content) | General quality check | — |
| Rust lint / tests | `cargo clippy --locked --offline`, `cargo test --locked --offline` (Clippy installed as a rustup component) | Yes | Clippy: 2 warnings, both `clippy::needless_return` (style; `src/lib.rs:605`, `src/lib.rs:631`) — not security diagnostics. Tests: 0 tests exist in the Tauri crate | Security-supporting (compiler/static correctness; **not** dependency vulnerability scanning) | No Rust unit tests exist; no supply-chain advisory coverage |
| Secret scanning | No dedicated scanner installed. Limited Git tracked-file sentinel search (high-confidence patterns, path-only) | Yes (sentinel search only) | No matches. See §8 | Direct security check, **limited scope** | Gap `S0.3-G007`: no dedicated secret scanner |
| SBOM | No SBOM generator exists in the toolchain | No | Not executed | Gap | `S0.3-G008`: no SBOM capability (exact format also an unresolved policy decision) |
| SAST | No dedicated SAST tool exists (built-in Roslyn analyzers only) | No | Not executed | Gap | `S0.3-G006` |
| Runtime (packaged) listener verification | No repository test; launching the app/sidecar is outside S0.3 scope | No | Unable to verify (also in S0.2 §20) | — | `S0.3-G009`: runtime/packaged network verification remains separate future work |

## 5. Dependency Advisory Results

### 5.1 NuGet

**Command:** `dotnet package list --vulnerable --include-transitive --no-restore` (from
`src/backend`; command syntax confirmed against `dotnet package list --help` for the installed
.NET 10 SDK). The check evaluated the 12 existing `project.assets.json` files (no restore, no
manifest change, no source added) against the vulnerability data of the already-configured
sources (`https://api.nuget.org/v3/index.json` and the SDK fallback folder
`C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\`).

**Result:** the native NuGet check reported **no known advisories** for the dependency graph it
was able to evaluate at the time of execution, for all 12 projects including transitive
dependencies (104 unique resolved package entries across the 12 project asset files).

This is not a statement that NuGet dependencies are secure. The evaluated graph is the one last
restored on this workstation; there is no committed NuGet lockfile pinning it (see S0.2 §5.1),
so a future restore could resolve a different graph.

### 5.2 npm

**Command:** `npm audit --json` (from `src/frontend`; non-mutating; exit code 1 represents
reported findings, not an execution failure). The check evaluated the committed
`package-lock.json` (lockfileVersion 3, 329 `packages` entries) against the npm advisory
database. No lockfile or manifest change occurred (working tree verified clean afterwards).

**Result:** npm reported **3 advisories** — npm-reported severity counts:
`{info: 0, low: 0, moderate: 1, high: 2, critical: 0, total: 3}`. Dependency counts per npm
metadata: `{prod: 6, dev: 323, optional: 52, peer: 8, total: 328}`.

All three affected packages are **development-only** (none is in the application runtime
graph):

| Package (locked) | Direct/transitive | Runtime/dev | npm-reported severity | Advisory reference(s) supplied by npm | Fix offered by npm |
|---|---|---|---|---|---|
| `openapi-typescript@6.7.6` | direct devDependency | development-only | moderate (affected range 5.1.1–6.7.6; exposure via its `undici` dependency) | via `undici` advisories below | `openapi-typescript@7.13.0` (major) |
| `undici@5.29.0` | transitive (via `openapi-typescript`) | development-only | high (affected range ≤6.27.0) | 12 GitHub advisories, incl. GHSA-g9mf-h72j-4rw9, GHSA-2mjp-6q6p-2qxm, GHSA-vrm6-8vpv-qv8q, GHSA-v9p9-hfj2-hcw8, GHSA-4992-7rv2-5pvq, GHSA-p88m-4jfj-68fv, GHSA-vxpw-j846-p89q, GHSA-g8m3-5g58-fq7m, GHSA-8xcm-r25x-g524, GHSA-m8rv-5g2x-5cg5, GHSA-v3r7-h72x-cjcm, GHSA-35p6-xmwp-9g52 | `openapi-typescript@7.13.0` (major) |
| `nanoid@3.3.16` | transitive (via `postcss`) | development-only | high (affected range <3.3.18) | GHSA-2v37-7h3g-55p8 ("custom generators can loop indefinitely when size is zero") | fix available per npm |

Notes, per the result-handling rules:

- **npm-reported severity ≠ final KST risk severity.** No KST risk severity is assigned here.
- **Confirmed advisory ≠ confirmed exploitability.** These are tool-confirmed presence in the
  current resolved graph; reachability/exposure within KST's actual use of these dev tools
  (e.g. `openapi-typescript` run locally by `npm run generate:types`, `nanoid` used inside
  `postcss` during Vite builds) has **not** been analyzed in S0.3.
- All three sit in development tooling, not in the shipped application's runtime dependencies.
  That reduces practical exposure but does **not** by itself dispose of the advisories —
  development tooling is part of the supply chain under `DEPENDENCY_ADMISSION.md`, and a known
  affected dependency must not be silently dismissed.
- **No remediation was executed.** No `npm audit fix`, no version change, no lockfile change.
  The offered fix for two of the three is a **major** version bump of `openapi-typescript`
  (6.7.6 → 7.13.0), which would be a dependency change requiring the dependency-admission
  process and contract-regeneration validation, not an automatic audit fix.

Recorded as tracked finding **`S0.3-F001` (Confirmed)** — see §10.

### 5.3 Cargo

**Command:** none executed for advisories. Read-only presence checks (`command -v`) found
**neither `cargo-audit` nor `cargo-deny`** installed on this workstation.

**Gap:** No currently authorized/available Rust dependency advisory scanner was found.

`cargo clippy --locked --offline` and `cargo test --locked --offline` **were** executed, but
they are compiler/static-correctness and test checks — not vulnerability-database checks — and
they provide no supply-chain advisory coverage for the 480-entry `Cargo.lock`. Recording the
advisory capability absence is `S0.3-G001`.

## 6. Existing Repository Security Tests

All backend tests were run with the repository-documented command plus `--no-restore` (build
state was warm; no restore, no production DB, no packaged app, no network mutation — the
integration tests use an in-memory `WebApplicationFactory` with QAD explicitly unconfigured).

Full suite: **656/656 passed, 0 failed, 0 skipped**
(`Kst.Domain.Tests` 118, `Kst.Integrations.Qad.Tests` 173, `Kst.Application.Tests` 242,
`Kst.ArchitectureTests` 9, `Kst.Api.IntegrationTests` 114).

### 6.1 DependencyRuleTests

- **Location:** `src/backend/tests/Kst.ArchitectureTests/DependencyRuleTests.cs`
- **Property protected:** preserves architectural dependency direction — `Kst.Domain` and
  `Kst.Application` stay free of ASP.NET Core, SQL-client (Microsoft.Data.SqlClient/Dapper),
  infrastructure, and API-host coupling; integration projects never depend on `Kst.Api`. This is
  the executable enforcement of the boundary that keeps SQL/infrastructure concerns out of
  business-rule code.
- **Tests:** 6 (6/6 passed): `Domain_Does_Not_Reference_Infrastructure`,
  `Domain_Does_Not_Reference_AspNetCore`, `Application_Does_Not_Reference_AspNetCore`,
  `Application_Does_Not_Reference_SqlServer`, `Domain_Does_Not_Reference_Api`,
  `Integration_Projects_Do_Not_Reference_Api`.
- **Scope limitation:** static assembly-reference rules only; it does not inspect SQL content,
  networking, or runtime behavior.

### 6.2 VersionConsistencyTests

- **Location:** `src/backend/tests/Kst.ArchitectureTests/VersionConsistencyTests.cs`
- **Property protected:** version-string consistency across `Directory.Build.props` (authoritative),
  `src/tauri/Cargo.toml`, `src/tauri/tauri.conf.json`, and `src/frontend/package.json`
  (supply-chain/release hygiene; **security-supporting, not a vulnerability check**).
- **Tests:** 3 (3/3 passed).
- **Scope limitation:** guards version drift only.

### 6.3 CorsPolicyTests

- **Location:** `src/backend/tests/Kst.Api.IntegrationTests/CorsPolicyTests.cs`
- **Property protected:** CORS allowed-origin behavior for the frontend/Tauri origins.
- **Tests:** 2 (2/2 passed): `GetHealth_WithAllowedOrigin_ReturnsCorsHeader`
  (`Origin: http://localhost:1420` → echoed `Access-Control-Allow-Origin`), and
  `GetHealth_WithPackagedTauriOrigin_ReturnsCorsHeader` (`Origin: http://tauri.localhost`).
- **Scope limitation vs. the accepted baseline:** the configured policy
  (`Kst.Api/Program.cs`) declares **5 named origins** and uses neither `AllowAnyOrigin()` nor
  `AllowCredentials()`. The tests verify the header for only **2 of the 5** origins
  (`http://localhost:1420`, `http://tauri.localhost`) and do **not** assert the absence of
  `AllowAnyOrigin`, the absence of credentials, or the behavior for the other three origins
  (`http://127.0.0.1:1420`, `tauri://localhost`, `https://tauri.localhost`). The 5-origin /
  no-`AllowAnyOrigin` / no-credentials configuration was re-verified by direct read-only source
  inspection at this commit (matches S0.2 §10).
- These tests exercise the in-memory test host, not the packaged runtime; a passing CORS test
  is not packaged-runtime verification.

### 6.4 Other existing checks searched for and their security relevance

A read-only search of all test names/content (backend, frontend, Rust) for the concepts
loopback/localhost/127.0.0.1/Cors/CSP/capability/shell/read-only/SQL write/connection
string/secret/credential/architecture/dependency/sidecar/process found:

- **QAD reader tests** (`Kst.Integrations.Qad.Tests`, 173 tests): assert parameterized SQL
  shape (parameters, predicates, joins) for every QAD query. Security-relevant as
  parameterization/shape evidence; **no test asserts the absence of write-verb SQL**, so
  read-only behavior is not enforced by a test (gap `S0.3-G005`). No write-verb SQL
  (`INSERT INTO`/`UPDATE … SET`/`DELETE FROM`/`MERGE`) was found in any test file either.
- **`KstApiFactory`** (integration test host): in-memory stores, QAD configuration explicitly
  forced empty — tests cannot reach the production database.
- **No test or script** verifies backend loopback binding, the Tauri CSP, Tauri
  capabilities/shell scoping, or QAD connection-string/transport configuration (gaps
  `S0.3-G002`, `S0.3-G003`, `S0.3-G004`, and part of `S0.3-G010`).
- **Frontend tests** (281 tests, 14 files): UI states, filtering, request lifecycle behavior.
  No security-relevant assertions identified.
- **Rust:** the Tauri crate contains **no** `#[test]`/`#[cfg(test)]` tests (`cargo test` ran 0
  tests).

No new tests were created in S0.3.

## 7. Compiler / Analyzer / Lint Results

### 7.1 .NET (backend)

- **Analyzer configuration (observed in repository):** `src/backend/Directory.Build.props`
  sets `EnableNETAnalyzers=true` and `AnalysisLevel=latest` for all backend projects;
  `TreatWarningsAsErrors=false`; no additional analyzer packages or editorconfig-based rule
  sets beyond these were found.
- **Check command:** `dotnet build Kst.slnx --no-restore --nologo` (repository-documented
  build path; `--no-restore` because build state was warm).
- **Result:** Build succeeded — **0 Warning(s), 0 Error(s)**. No diagnostic in the build
  output is specifically security-related.
- **Classification:** the built-in Roslyn analyzer set is a **security-supporting** static
  check. It is **not** full SAST and its pass does not certify the absence of
  vulnerability-class defects. No security-specific diagnostic appeared.
- Side effect note: the build also regenerates `docs/openapi/Kst.Api.json` (established
  repository behavior). The regenerated file was byte-identical; the working tree remained
  clean.

### 7.2 Frontend

- **Lint:** `npm run lint` → `eslint . --ext ts,tsx --report-unused-disable-directives
  --max-warnings 0`. Result: **clean (0 errors, 0 warnings)**.
  **Classification:** ESLint with the recommended `@eslint/js` + `typescript-eslint` + React
  hooks/refresh rule sets — **general static-quality check; no dedicated security-lint plugin
  is configured, so this is not a dedicated security scan.**
- **Typecheck:** `npm run typecheck` → `tsc --noEmit`. Result: **exit 0**.
  **Classification:** security-supporting / general correctness; not a security scanner.
- **Tests:** `npm test` → Vitest run: **281/281 passed** (14 files). General quality check;
  security relevance separately assessed in §6.4 (none identified).
- No dependencies were installed; all ran against the existing `node_modules`.

### 7.3 Rust (Tauri)

- **Components (read-only, `rustup component list --installed`):** cargo, clippy, rust-docs,
  rust-std, rustc, rustfmt — all for `x86_64-pc-windows-msvc`. No components installed.
- **Lint:** `cargo clippy --locked --offline`. Result: **2 warnings**, both
  `clippy::needless_return` (style) at `src/tauri/src/lib.rs:605` and `src/tauri/src/lib.rs:631`
  (in the backend-liveness check code paths). **These are not security diagnostics** and are
  recorded as observations, not findings. 0 errors.
- **Tests:** `cargo test --locked --offline`. Result: compiled and ran **0 tests** (none exist
  in the crate).
- **Classification:** compiler/static correctness, security-supporting; **not** dependency
  vulnerability scanning and no supply-chain advisory coverage.

## 8. Secret-Sentinel Result

**Scope (explicit limitation):** this sentinel search is **not equivalent to a dedicated secret
scanner**. No dedicated secret scanner exists in the current toolchain (see §12), and S0.3 did
not install one. The search was limited to a small number of high-confidence patterns against
**tracked files only**, using Git's built-in search with **path-only output** (no matching
content was printed, and no values were recorded).

**Commands (read-only, tracked files only):**

- `git grep -l` for the private-key block sentinels: `-----BEGIN PRIVATE KEY-----`,
  `-----BEGIN RSA PRIVATE KEY-----`, `-----BEGIN OPENSSH PRIVATE KEY-----`,
  `-----BEGIN EC PRIVATE KEY-----`, `-----BEGIN DSA PRIVATE KEY-----`,
  `-----BEGIN PGP PRIVATE KEY-----` → **no matches**.
- Tracked files with key/certificate file extensions (`.pem`, `.pfx`, `.p12`, `.key`, `.cer`,
  `.crt`, `.keystore`, `.jks`) → **none tracked**.
- Tracked files matching credential-bearing naming conventions (local/secrets appsettings
  patterns, `credentials*`, `.env`) → **none tracked**.

**Result:** no likely committed secret was identified by the sentinel patterns. The repository
convention keeps local secret overrides out of version control: `.gitignore` excludes
`**/appsettings.*.local.json` and `**/*.secrets.json`, and no tracked file violates those
patterns. This does not prove the absence of all secret exposure (e.g. entropy-based patterns,
historical commits, or non-tracked working files were not scanned).

## 9. S0.2 Finding Verification

| ID | S0.2 state | S0.3 verification (read-only, at commit `18fdc84`) | Current state |
|---|---|---|---|
| S0.2-F001 (Tauri shell-capability scope) | Potential / Investigation Required | `src/tauri/capabilities/default.json` re-inspected: `shell:allow-execute`/`shell:allow-open` still granted without an observed scope restricting execution to the `Kst.Api` sidecar. A search of all existing tests, scripts, and repository tooling found **no existing test/tool/config validation that independently constrains or verifies this capability's least-privilege scope**. No Tauri v2 scoping-semantics determination was made in S0.3 (no new tooling, no web research authorized). | **S0.2-F001 remains Potential / Investigation Required.** Existing toolchain has no identified independent least-privilege verification for this property. Not upgraded to Confirmed merely because no test exists. Not remediated. |
| ~~S0.2-F002~~ (database read-only enforcement) | Retired (2026-08-24) | No new observation. Retirement basis (operator/IT authority, S0.2 §13.1/§14) unchanged. | **Remains Retired.** No remediation occurred in S0.3. |
| S0.2-F003 (QAD SQL transport configuration) | Confirmed | Simple read-only configuration re-verification at the check-execution commit: `QadConnectionOptions.cs` defaults remain `Encrypt = true` / `TrustServerCertificate = true`; `appsettings.json` `QadDatabase` section still contains only `Server`/`Database`/`ConnectTimeoutSeconds` (no transport overrides); `QadConnectionStringFactory` still uses `IntegratedSecurity = true`. No configuration was modified, no connection was made, no SQL transport was tested. | **Confirmed present** (configuration-vs-requirement mismatch still exists at this commit). Not remediated. Severity not assigned. The underlying legacy unencrypted-transport constraint is **not** marked `Accepted Risk`. |

## 10. New Findings

| ID | State | Area | Evidence | Tool/Check | Next Step |
|---|---|---|---|---|---|
| S0.3-F001 | **Confirmed** | npm dependency advisories (development tooling) | `npm audit` identified, in the current resolved lockfile graph: `openapi-typescript@6.7.6` (direct devDependency; npm-reported moderate; affected range 5.1.1–6.7.6), `undici@5.29.0` (transitive via `openapi-typescript`; npm-reported high; range ≤6.27.0; 12 GitHub advisories incl. GHSA-vrm6-8vpv-qv8q, GHSA-v9p9-hfj2-hcw8, GHSA-vxpw-j846-p89q), and `nanoid@3.3.16` (transitive via `postcss`; npm-reported high; range <3.3.18; GHSA-2v37-7h3g-55p8). All three are development-only; none is in the runtime application dependency graph. | `npm audit --json` (ecosystem-native advisory check; authorized by S0.3 prompt §7) | In a later, explicitly authorized checkpoint: reachability/exposure analysis of the dev-tooling paths (`generate:types`, Vite build), and a dependency-admission/remediation decision per `DEPENDENCY_ADMISSION.md` (note: npm's offered fix for two of the three is a major version bump, `openapi-typescript` 6.7.6 → 7.13.0, which would require contract-regeneration validation). **Not remediated in S0.3.** npm-reported severities are quoted as such; no KST risk severity is assigned; confirmed advisory ≠ confirmed exploitability. |

No other new tracked findings were created. The two Clippy `needless_return` warnings are
style observations (§7.3), not security findings. No finding in this document is marked
`Accepted Risk`, and no KST risk severity was assigned to any item.

## 11. Existing-Tool Coverage Gaps

Gaps below are evidenced by this pass's execution (and by the S0.2 baseline where noted). They
are capability gaps, not findings of vulnerability:

| ID | Gap | Evidence |
|---|---|---|
| S0.3-G001 | **Rust dependency advisories:** no authorized/available Rust advisory scanner (`cargo-audit`/`cargo-deny` absent). `Cargo.lock` (480 entries) has no native `cargo` advisory-database check. | §5.3, §12 |
| S0.3-G002 | **Loopback binding:** backend loopback binding (`127.0.0.1`) is statically observed in `Program.cs` but has no identified independent test coverage; runtime/packaged listener verification also outstanding (with G009). | §4, §6.4, S0.2 §9 |
| S0.3-G003 | **CSP verification:** the Tauri CSP in `tauri.conf.json` is statically observed but has no identified independent automated verification (no test, no config-validation script). | §4, §6.4 |
| S0.3-G004 | **Tauri least-privilege verification:** the granted `shell:allow-execute`/`shell:allow-open` capabilities have no identified independent verification that the granted surface is no broader than the single sidecar execution need (carries S0.2-F001). | §4, §9 |
| S0.3-G005 | **Read-only SQL enforcement:** QAD tests assert SQL shape/parameterization but no existing test asserts the absence of write-verb SQL; read-only behavior rests on code convention + operator/IT authority, not an executable check. | §6.4 |
| S0.3-G006 | **SAST:** no dedicated SAST capability exists; only built-in Roslyn analyzers (which produced no security-specific diagnostic in this pass). | §4, §7.1 |
| S0.3-G007 | **Dedicated secret scanning:** no secret scanner exists; only the limited high-confidence sentinel search of §8 was possible. Historical commits were not scanned. | §8, §12 |
| S0.3-G008 | **SBOM:** no SBOM generation capability exists in the current toolchain (exact SBOM format is also an unresolved policy decision). | §4, policy §"Intentionally Unresolved Policy Areas" |
| S0.3-G009 | **Runtime listener verification:** packaged (installed) runtime listener/network behavior is not verifiable by existing repository tests; requires explicitly scoped runtime verification (app launch) that S0.3 did not perform. | §4, S0.2 §9/§20 |
| S0.3-G010 | **Database-grant verification:** actual QAD login/group grants are server-side; no repository tool can verify them and no live connection was authorized in S0.3. Operator/IT authority establishes the required posture; independent grant inspection remains outstanding. | §4, S0.2 §13.1/§20 |

Secondary (non-tracked) observations: CORS tests cover only 2 of 5 configured origins and do
not assert `AllowAnyOrigin`-absence/no-credentials (§6.3); ESLint has no security plugin
(§7.2); the Tauri crate has no Rust unit tests (§7.3); no committed NuGet lockfile pins the
advisory-checked graph (§5.1).

## 12. Specialized Tool Availability

Read-only command-presence checks (`command -v`, the equivalent of `Get-Command`) for a bounded
set of common specialized security tools:

| Tool | Present? | Note |
|---|---|---|
| `cargo-audit` | No | Rust dependency advisory scanning |
| `cargo-deny` | No | Rust dependency advisory/policy checking |
| `gitleaks` | No | Secret scanning |
| `semgrep` | No | SAST |
| `trivy` | No | Multi-ecosystem scanning |
| `osv-scanner` | No | Advisory scanning (OSV) |
| `syft` | No | SBOM generation |
| `grype` | No | Vulnerability matching |

**None of the above was executed.** Presence (or absence) here is an environment observation
only: existing installation does not imply KST approval, and no admission decision is made in
this document. Any later use of such tools would go through the enacted dependency-admission
process (`docs/security/DEPENDENCY_ADMISSION.md`).

## 13. Candidate Later Tool/Process Needs

Capability categories only — **no product, format, or platform selection is made here**; those
remain intentionally unresolved per `SECURITY_ASSURANCE_POLICY.md` §"Intentionally Unresolved
Policy Areas" and would each require a separate, explicitly authorized decision:

- **Rust dependency advisory checking** (closes G001).
- **Dedicated secret scanning**, including history where organizationally appropriate (closes
  G007).
- **SAST** beyond built-in compiler analyzers (closes G006).
- **SBOM generation and release integration** (closes G008; exact format unresolved).
- **Tauri capability/least-privilege verification** — a test or config-validation mechanism for
  granted permissions (closes G004; supports F001 disposition).
- **Runtime network/listener verification** of the packaged application (closes G009).
- **Server-side database-grant verification process** with IT (closes G010).
- **Executable enforcement of read-only SQL behavior** (closes G005) and **loopback-binding
  test coverage** (closes G002) — candidate additions to the existing test suite, to be
  planned as test development, not in S0.3.
- **CSP configuration verification** (closes G003).
- **Disposal decision for the S0.3-F001 npm advisories** (remediation via dependency admission
  vs. documented explicit disposition).

## 14. S0.3 Conclusion

**What the existing toolchain successfully verifies at this commit:**

- Backend solution builds with built-in Roslyn analyzers at 0 warnings/0 errors; full backend
  suite 656/656 green, including the 6 `DependencyRuleTests` (architecture boundary
  enforcement), 3 `VersionConsistencyTests`, and 2 `CorsPolicyTests` (partial CORS coverage).
- Frontend lint clean, typecheck clean, 281/281 frontend tests green.
- Rust crate compiles offline against the locked dependency graph; Clippy reports 2 style-only
  warnings; 0 Rust tests exist.
- NuGet: the native check found no known advisories for the evaluated (last-restored) graph,
  direct and transitive.
- Lock/resolution artifacts (`package-lock.json`, `Cargo.lock`, all 12 `project.assets.json`)
  are structurally consumable without modification.
- High-confidence tracked-file secret sentinels: no matches; secret-file naming conventions are
  respected by the tracked tree.
- S0.2-F003 (QAD transport configuration mismatch) is confirmed still present; S0.2-F002
  remains retired; S0.2-F001 remains `Potential / Investigation Required` with no existing
  least-privilege verification identified.

**Package advisory results:** NuGet — none reported; npm — 3 advisories, all development-only
(`S0.3-F001`, Confirmed; unremediated by design of this pass); Cargo — not checkable with
existing authorized tooling.

**Security properties that remain unverified by existing tools:** packaged-runtime loopback
listener behavior, CORS for 3 of 5 origins and its absence-of-`AllowAnyOrigin`/no-credentials
properties, Tauri CSP, Tauri shell-capability least privilege, read-only SQL enforcement,
server-side QAD grants, and all Rust supply-chain advisory status. A clean audit, a green
compiler, or a passing test in any of the rows above does not close these.

**New items requiring owner attention:** one — `S0.3-F001` (Confirmed npm advisories in
development tooling; disposition decision needed, not performed here). S0.2-F001 and
S0.2-F003 carry forward unchanged in state (F003 re-verified present). No item was marked
`Accepted Risk`.

**Capability gaps requiring later tool-admission decisions:** G001–G010 (§11), plus the
test-development candidates listed in §13.

**This pass does not declare KST secure.** It records what the existing toolchain actually
checks, what it found, and what it cannot check.

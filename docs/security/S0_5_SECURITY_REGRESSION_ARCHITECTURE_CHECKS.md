# S0.5 — Security Regression & Architecture Checks

**Status:** COMPLETE / ACCEPTED — 2026-08-26

**Implementation date:** 2026-08-25
**Acceptance date:** 2026-08-26 (project-owner acceptance of the implemented regression protection)
**Starting commit:** `f784c835a507b9fd4a67c95c46ffef9ab308304c` (`chore: remediate npm development advisories`)

This document is **evidence, not normative policy**. It records the repository-native
regression protection added by S0.5 for the accepted security properties identified in the
accepted S0.3 coverage gaps (`S0.3-G002`–`S0.3-G005`) and the accepted S0.3 secondary CORS
observation. Required security properties remain defined by `SECURITY.md` and `docs/security/`
(especially `SECURITY_ASSURANCE_POLICY.md` and `APPLICATION_SECURITY_PROFILE.md`). A passing
check proves the asserted property for the inspected configuration/code only — it is not a
security certification, and it does not prove general exploit resistance or runtime/infrastructure
behavior.

---

## 1. Purpose and Status

S0.5 turns important security properties already known to KST into inexpensive, durable
repository checks **where practical**. The goal is **architecture/security regression
protection**, not test-count maximization.

This checkpoint adds tests only; it remediates no production defect, installs no tools, changes
no dependency, and performs no runtime/infrastructure verification (S0.7) or security-tool
admission (S0.6).

**Status: COMPLETE / ACCEPTED — 2026-08-26.**

## 2. Governing Scope

- Canonical remaining-S0 plan: `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md`
  (§7 — S0.5 Security Regression & Architecture Checks).
- Enacted policy: `SECURITY.md`, `docs/security/SECURITY_ASSURANCE_POLICY.md`,
  `docs/security/APPLICATION_SECURITY_PROFILE.md`, `AGENTS.md` (§7 read-only database access,
  §8 security requirements).
- Accepted evidence consulted (unmodified): `docs/security/SECURITY_BASELINE.md` (S0.2),
  `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (S0.3),
  `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`,
  `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`,
  `docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md`.

## 3. Starting State

- **Commit:** `f784c835a507b9fd4a67c95c46ffef9ab308304c` on branch `main`; local `main` ==
  `origin/main`; working tree clean at start.
- **Accepted security state:** S0.1–S0.3 COMPLETE / ACCEPTED; S0.4 (A/B/C) COMPLETE / ACCEPTED
  — 2026-08-25. S0.5 was NEXT / NOT STARTED; S0.6–S0.8 PLANNED / NOT STARTED; Stage 9 NOT
  STARTED / blocked pending S0 closeout.
- **Accepted properties protected here:** loopback-only backend binding; the Tauri webview
  `connect-src` loopback restriction; the S0.4B Tauri least-privilege capability surface;
  read-only QAD SQL; and the accepted frontend CORS origin set.

## 4. Gap Reconciliation

| Gap | Starting State (accepted evidence) | S0.5 Action | Result | Remaining Verification |
| --- | --- | --- | --- | --- |
| S0.3-G002 (loopback binding) | Statically observed in `Program.cs`; no independent test | Added behavioral binding test (`LoopbackBindingTests`) | **Covered** (repository) | Packaged/installed runtime listener → S0.7 (G009) |
| S0.3-G003 (CSP) | Statically observed in `tauri.conf.json`; no automated verification | Added structural CSP tests (`csp_guard` in Rust) | **Covered** (repository) | Packaged/runtime CSP behavior → S0.7 |
| S0.3-G004 (Tauri least privilege) | Carried S0.2-F001; S0.4B added `capability_guard` tests | Verified S0.4B tests already close the gap; **no new code** | **Covered** (by S0.4B) | Packaged capability behavior → S0.7 |
| S0.3-G005 (read-only QAD SQL) | Tests assert SQL shape/parameterization only; none asserts absence of write-verb SQL | Added read-only SQL tests (`QadReadOnlySqlTests`) | **Partially Covered** (application-emitted SQL) | Server-side account grants → S0.7 (G010) |
| CORS (S0.3 secondary observation) | `CorsPolicyTests` covered 2 of 5 origins; no `AllowAnyOrigin`/credentials assertion | Extended `CorsPolicyTests` (all 5 origins + untrusted-origin + structural policy assertions) | **Covered** (repository) | Packaged-runtime CORS behavior → S0.7 |

## 5. Loopback-Binding Protection (S0.3-G002)

- **Production authority inspected:** `src/backend/Kst.Api/Program.cs`. The startup path reads
  `ASPNETCORE_URLS`; when absent (the desktop/sidecar path — the Tauri host in
  `src/tauri/src/lib.rs` launches the sidecar setting only `ASPNETCORE_CONTENTROOT`, not
  `ASPNETCORE_URLS`), it binds `builder.WebHost.UseUrls($"http://127.0.0.1:{listenPort}")`
  with `listenPort` from `--port`/`KST_PORT`/0 (OS-assigned). The binding is **IPv4 loopback
  only** (`127.0.0.1`); no wildcard or non-loopback form exists in the repository-controlled
  path.
- **Test added:** `src/backend/tests/Kst.Api.IntegrationTests/LoopbackBindingTests.cs` —
  `Backend_Process_Binds_To_Loopback_Only`.
- **Exact invariant:** the effective TCP listener for the launched backend's handshake port is
  bound to `127.0.0.1` (loopback), not to `0.0.0.0`, `::`, `*`/`+`, or a LAN address.
- **Mechanism:** launches the real, self-contained `Kst.Api.exe` (the same shape the Tauri
  sidecar executes) with `--port=0` and no `ASPNETCORE_URLS`, forces QAD/Shortages
  unconfigured so no database connection occurs, reads the documented stdout handshake
  (`{port,instanceId,status}`), then inspects the OS TCP listener table
  (`IPGlobalProperties.GetActiveTcpListeners`) and asserts the endpoint address is loopback.
- **Why durable:** behavioral, not source-text; it fails on any actual bind broadening and
  survives harmless refactoring of how the URL string is constructed.
- **What it does not prove:** packaged/installed runtime listener behavior (S0.7, G009); and it
  does not govern an operator-set `ASPNETCORE_URLS` environment override (documented in
  `docs/development/SETUP.md`, default `http://127.0.0.1:0`) — that override is outside
  repository configuration and is recorded as finding **S0.5-F001**.

## 6. CSP Protection (S0.3-G003)

- **Production authority inspected:** `src/tauri/tauri.conf.json` → `app.security.csp`:
  `default-src 'self'; connect-src http://127.0.0.1:* 'self'; style-src 'self' 'unsafe-inline'
  https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com`.
- **Tests added:** `src/tauri/src/lib.rs` → `#[cfg(test)] mod csp_guard` (three tests), placed
  beside the accepted S0.4B `capability_guard` tests and using the crate's existing
  `serde_json` dependency (no new dependency).
- **Exact invariants:**
  - `csp_connect_src_is_restricted_to_loopback_and_self` — `connect-src` is present and
    non-empty; contains no bare `*`; and every source is either `'self'` or an `http(s)` origin
    whose host is exactly `127.0.0.1` (any port, including a port wildcard). No remote or
    arbitrary backend destination.
  - `csp_default_src_remains_self_only` — `default-src` is exactly `'self'`.
  - `csp_effective_script_sources_have_no_unsafe_or_remote_sources` — the effective script
    policy (explicit `script-src`, else `default-src`) contains no `'unsafe-inline'`,
    `'unsafe-eval'`, `*`, or unquoted remote (`scheme://…`) source.
- **Why durable:** parses the configuration structurally into directive → sources and asserts
  semantic properties, not the exact CSP string, so harmless reordering or unrelated directive
  changes do not break the security test.
- **What it does not prove:** packaged/runtime webview CSP enforcement (S0.7); general
  browser/webview exploit resistance. The accepted `style-src 'unsafe-inline'` and the two
  Google Fonts origins are intentionally **not** restricted by these tests (they are accepted,
  non-`connect` surface).

## 7. Tauri Least-Privilege Protection (S0.3-G004)

- **Determination:** the accepted S0.4B regression tests in `src/tauri/src/lib.rs`
  (`#[cfg(test)] mod capability_guard`) **fully satisfy S0.3-G004**. They assert:
  - `default_capability_grants_core_default_only` — the checked-in capability grants exactly
    `core:default` to exactly the `main` window, with **no** `shell:*` permission (bare or
    scoped/object form).
  - `sidecar_boundary_is_exactly_kst_api` — `bundle.externalBin` is exactly
    `binaries/Kst.Api` and the runtime sidecar name matches it.
- **Accepted state confirmed:** webview capability = `core:default` only; `shell:allow-execute`
  and `shell:allow-open` absent; `bundle.externalBin` = `binaries/Kst.Api` only; runtime sidecar
  identity = `Kst.Api`.
- **S0.5 action:** **none** (no redundant tests added; the S0.4B capability surface was not
  altered). Gap recorded as **covered by the accepted S0.4B regression tests.**

## 8. Read-Only QAD SQL Protection (S0.3-G005)

- **Query architecture inspected:** `src/backend/Kst.Integrations.Qad`. Every production QAD
  reader builds SQL through a **public, pure, static** builder returning
  `(string Sql, DynamicParameters Parameters)` (13 builders across `ApprovedVendors`, `Bom`,
  `ComponentDetail`, `Inventory`, `Mps`, `PartDetail`, `WorkOrders`), so the generated SQL is
  independently testable without a connection. Two additional inline statements exist:
  `QadConnectionFactory`'s `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` (an accepted,
  documented, non-mutating session setting) and the connectivity check's `SELECT 1`.
- **Production queries covered:** all 13 `Build*` query builders (invoked via reflection with
  representative arguments) plus the two inline statements (covered by the source-literal scan).
- **Method used:** `src/backend/tests/Kst.Integrations.Qad.Tests/ReadOnly/QadReadOnlySqlTests.cs`
  (two tests):
  - `All_Production_Query_Builders_Emit_ReadOnly_Sql` — enumerates, by reflection, every public
    static method in the production assembly returning `ValueTuple<string, …>` (the builder
    convention), invokes each with generated representative arguments, normalizes the generated
    SQL (strips comments, string-literal contents, and bracketed identifiers), and asserts no
    mutating verb token is present. New builders are covered automatically; a minimum-count
    guard fails loudly if the convention silently disappears.
  - `Production_Qad_Source_Contains_No_Mutating_Sql_Literals` — a small C# string-literal
    extractor (regular/verbatim/raw, with `$`/`@` prefixes) scans the production
    `Kst.Integrations.Qad` source and asserts no literal contains a statement beginning with a
    mutating verb. This catches the two inline statements and any future inline mutating SQL.
- **Mutation categories covered (statement-start or token):** `INSERT`, `UPDATE`, `DELETE`,
  `MERGE`, `TRUNCATE`, `DROP`, `ALTER`, `CREATE`, `EXEC`, `EXECUTE`. Session/setting statements
  such as `SET` are intentionally **not** treated as mutating (the accepted
  `READ UNCOMMITTED` isolation setting is non-mutating).
- **False-positive / false-negative considerations:** normalization blanks comments, string
  literals, and `[bracketed identifiers]`, so a verb appearing only in a comment, literal, or
  identifier (e.g. `AS Update`, `[update date]`) does not trip the check. The source scan keys
  on statement-start verbs to avoid flagging ordinary C# strings/log messages. This is a
  lexical/structural check, **not a SQL parser**; it does not model every conceivable
  obfuscation, and it does not execute SQL.
- **Runtime grant limitation:** these tests prove **application code emits read-only queries**.
  They do **not** prove the QAD database account is technically incapable of writes. Server-side
  login/group grant verification remains **S0.7** (S0.3-G010) and requires IT/server access.

## 9. CORS Protection (S0.3 secondary observation)

- **Current accepted allowed-origin set (recovered from `Kst.Api/Program.cs`, matching S0.2
  baseline §10):** `http://localhost:1420`, `http://127.0.0.1:1420`, `tauri://localhost`,
  `http://tauri.localhost`, `https://tauri.localhost`. The policy uses `AllowAnyHeader()` and
  `AllowAnyMethod()`, and uses **neither** `AllowAnyOrigin()` **nor** `AllowCredentials()`.
- **Pre-existing coverage:** `CorsPolicyTests` verified the echoed header for only 2 of the 5
  origins and did not assert `AllowAnyOrigin`-absence / no-credentials.
- **Broadening protections added** (in `src/backend/tests/Kst.Api.IntegrationTests/CorsPolicyTests.cs`):
  - `GetHealth_WithEveryAcceptedOrigin_ReturnsCorsHeaderEchoingThatOrigin` (Theory, all 5
    accepted origins) — each accepted origin is echoed.
  - `GetHealth_WithUntrustedOrigin_DoesNotReceiveAllowOriginHeader` — an untrusted origin
    (`https://untrusted.example.com`) receives **no** `Access-Control-Allow-Origin` header
    (catches `AllowAnyOrigin` broadening behaviorally).
  - `Effective_Cors_Configuration_Matches_Accepted_S0_Surface` — structural assertion on the
    effective registered policy (`CorsOptions.GetPolicy("FrontendPolicy")`): the origin set is
    exactly the five accepted origins (set equality, so a sixth/removed origin fails),
    `AllowAnyOrigin` is false, `SupportsCredentials` is false, and `AllowAnyHeader`/
    `AllowAnyMethod` are true.
- **Credentials:** the accepted architecture does **not** require CORS credentials (no
  cookie/session cross-origin flow is part of the desktop model); the test asserts they remain
  disabled and is written to surface — not silently forbid — any deliberate future change.
- **Working CORS behavior unchanged;** the pre-existing two echo tests were preserved, not
  weakened.

## 10. Regression / Mutation Verification

Each protection was deliberately violated in a local working copy, confirmed to fail, then the
accepted state was restored exactly (no production data touched; no mutation committed).

| # | Mutation (temporary, local) | Affected check | Observed result |
|---|---|---|---|
| 1 | Added a throwaway `MutationProbeTemp.BuildProbeQuery` emitting `UPDATE …` to `Kst.Integrations.Qad` | `QadReadOnlySqlTests` (both) | **FAILED** — flagged the `UPDATE` verb (builder + source scans); probe deleted, suite re-passed |
| 2 | Changed `UseUrls` host `127.0.0.1` → `0.0.0.0` in `Program.cs` | `LoopbackBindingTests` | **FAILED** — "backend bound to 0.0.0.0"; restored, re-passed |
| 3 | Added `.AllowAnyOrigin()` to the CORS policy | `CorsPolicyTests` (behavioral + structural) | **FAILED** — untrusted origin received `Access-Control-Allow-Origin=*`; structural flagged `AllowAnyOrigin`; restored, re-passed |
| 4 | Added a sixth origin `https://attacker.example.com` | `CorsPolicyTests` (structural) | **FAILED** — origin-set drift reported (behavioral echo/rejection tests correctly still passed for the five real origins); restored, re-passed |
| 5 | Added `https://api.example.com` to CSP `connect-src` | `csp_connect_src_is_restricted_to_loopback_and_self` | **FAILED** — non-loopback destination flagged; restored, re-passed |
| 6 | Replaced `connect-src` origins with `*` | `csp_connect_src_is_restricted_to_loopback_and_self` | **FAILED** — bare `*` wildcard flagged; restored, re-passed |

## 11. Automated Verification Results

All commands run from the starting commit's working tree; `--no-restore` (warm build) where
noted; no live database, no packaged app, no network mutation.

| # | Command (cwd) | Result |
|---|---|---|
| 1 | `dotnet build Kst.slnx --no-restore --nologo` (`src/backend`) | Build succeeded — 0 Warning(s), 0 Error(s) |
| 2 | `dotnet test tests/Kst.Integrations.Qad.Tests --no-restore --nologo --filter "FullyQualifiedName~QadReadOnlySqlTests"` (`src/backend`) | **2/2 passed** |
| 3 | `dotnet test tests/Kst.Api.IntegrationTests --no-restore --nologo --filter "FullyQualifiedName~CorsPolicyTests\|FullyQualifiedName~LoopbackBindingTests"` (`src/backend`) | **10/10 passed** |
| 4 | `dotnet test Kst.slnx --no-restore --nologo` (`src/backend`) — full backend suite | **670/670 passed** (Domain 118, Qad 179, Application 242, Architecture 9, Api.Integration 122) |
| 5 | `cargo test --locked` (`src/tauri`) | **5 passed, 0 failed** (capability_guard 2, csp_guard 3) |
| 6 | `cargo check --locked` (`src/tauri`) | Success (config/ACL resolved at build time) |

Not run (with reason): frontend `npm test`/`lint`/`typecheck`/`build` — no frontend code or
configuration relevant to this checkpoint changed (CSP is Tauri config, CORS/loopback/QAD are
backend/Rust); the S0.4C-accepted frontend state was untouched. Packaged-app and live-database
verification is out of S0.5 scope (S0.7).

## 12. Finding Disposition

| ID | State | Area | Notes |
|---|---|---|---|
| S0.5-F001 | Informational | `ASPNETCORE_URLS` is an operator-controlled environment override that can alter the effective backend listener outside the repository-controlled `127.0.0.1` fallback (documented override in `SETUP.md`, default `http://127.0.0.1:0`). S0.5 proved the repository-controlled path; the environment override is outside repository configuration. | **Carry to S0.7 — Runtime & Infrastructure Verification** (S0.7 examines the effective deployed/runtime environment). No severity assigned (none authorized for this track). Not `Accepted Risk`. The override is not removed and the production binding behavior is not modified by finalization. |
| S0.5-F002 | Informational | The QAD read-only SQL regression check is a lexical/structural repository check, not a full SQL parser and not evidence of server-side database grants; it covers the QAD integration boundary. | Primarily a boundary/limitation record — not a production defect, and not a candidate for a SQL-parser dependency. No severity assigned. Not `Accepted Risk`. Server-side grant verification remains S0.7 (S0.3-G010). |

No material security risk is accepted by S0.5. No finding is marked `Accepted Risk`.

## 13. Gap Disposition

Final accepted dispositions (project-owner acceptance, 2026-08-26):

- **S0.3-G002 — Covered by S0.5 repository regression protection**
  (`LoopbackBindingTests`). Remaining runtime/packaged listener proof → S0.7.
- **S0.3-G003 — Covered by S0.5 repository regression protection** (`csp_guard` tests).
  Remaining runtime/packaged CSP behavior → S0.7.
- **S0.3-G004 — Covered by accepted S0.4B regression protection** (`capability_guard` tests);
  no new S0.5 code was required.
- **S0.3-G005 — Partially Covered.** Application-emitted QAD SQL is repository-regression
  protected as read-only (`QadReadOnlySqlTests`); actual server-side database grant
  enforcement remains **S0.7** (S0.3-G010). The distinction is preserved: *repository code
  emits read-only SQL* ≠ *database account is technically incapable of writes* — the second
  question belongs to S0.7.
- **CORS secondary observation — Covered by S0.5 regression protection** (extended
  `CorsPolicyTests`).

## 14. Residual Runtime / Infrastructure Work (→ S0.7)

Repository regression protection **≠** runtime/infrastructure verification. The following remain
for S0.7 (Runtime & Infrastructure Verification), item-by-item, when explicitly authorized:

- Packaged (installed, non-development) backend **listener** binding (S0.3-G009) — the S0.5 test
  covers the repository startup path, not the installed sidecar.
- Operator-set `ASPNETCORE_URLS` override behavior in a real deployment (S0.5-F001).
- Packaged **CORS** behavior (origin set as enforced by the installed app).
- Packaged **CSP** / webview enforcement behavior.
- Effective packaged **Tauri capability** behavior at runtime.
- **Server-side QAD login/group grants** — whether the account is technically incapable of
  writes (S0.3-G010); requires IT/server-side inspection.
- `keytronicshortage` hosting/permission details (unchanged by S0.5).

## 15. Non-Work (Confirmed)

- **No S0.6 / security tool admission occurred.** No scanner, SAST, secret scanner, SBOM
  tooling, or any other tool was installed, evaluated, or integrated.
  `docs/security/S0_6_SECURITY_TOOL_ADMISSION.md` was **not** created.
- **No new dependency** (NuGet/npm/Cargo) and no lockfile/manifest change.
- **No QAD or keytronicshortage connection** was made; no live SQL executed; no grants
  inspected.
- **No Tauri capability change** (the S0.4B surface was preserved, not broadened).
- **No S0.7 runtime verification**, no port probing, no network scanning, no packet capture,
  no production TLS test, no credential inspection, no installer/signing exercise.
- **No Stage 9 work.** No production business-logic change. Accepted S0.2/S0.3/S0.4 evidence
  snapshots were not modified.

## 16. Conclusion

S0.5 adds durable, repository-native regression protection for the loopback-only backend
binding, the Tauri webview `connect-src` loopback restriction, the accepted frontend CORS origin
set (including `AllowAnyOrigin`/credentials absence), and application-emitted read-only QAD SQL;
the Tauri least-privilege surface was already covered by the accepted S0.4B tests. Each check is
evidenced to fail when its protected property is deliberately violated. This is **durable
evidence that the specific architecture properties chosen to protect still hold** — not a
security certification. Runtime/infrastructure verification and server-side grant confirmation
remain explicitly routed to S0.7. **Status: COMPLETE / ACCEPTED — 2026-08-26.**

# S0.4A — QAD SQL Transport Remediation

**Status:** COMPLETE / ACCEPTED — 2026-08-25
**Implementation date:** 2026-08-24
**Acceptance date:** 2026-08-25 (project-owner acceptance of the implemented remediation)
**Starting commit:** `fa045ebc3507f17382fbd85b09059df9cc2196b0`
(`docs: plan remaining S0 security work`)
**Finding addressed:** `S0.2-F003`

This document is **implementation / remediation evidence**, not normative policy. It does not
replace the accepted S0.2 baseline (`docs/security/SECURITY_BASELINE.md`) or the accepted S0.3
evidence (`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md`). Required security properties
remain defined by `SECURITY.md` and `docs/security/`.

---

## 1. Purpose

S0.4A corrects the KST-side representation of the verified legacy QAD SQL transport constraint so
that the effective QAD connection configuration accurately states the verified infrastructure
reality:

> **The current QAD SQL endpoint does not support encrypted SQL connections from existing
> supported clients, so the effective QAD connection is unencrypted (`Encrypt=false`).**

Before this remediation the application instead expressed `Encrypt=true` /
`TrustServerCertificate=true` — i.e., it *pretended* encryption was enabled while bypassing
certificate validation, which misrepresents the actual (unencrypted) transport. This correction
makes the application say the truthful thing without claiming the organization has accepted the
underlying legacy-infrastructure risk.

This is a **bounded, QAD-only, application-configuration** change. It preserves Windows Integrated
Authentication, read-only / least-privilege access, and the internal-network restriction. It does
not touch `keytronicshortage`, Tauri capabilities, or any npm dependency. It is not a re-opening of
the accepted S0.2/S0.3 evidence, which continue to record the configuration as it was observed.

## 2. Starting Finding

**`S0.2-F003 — Confirmed`** (QAD SQL transport configuration mismatch).

Chronology of the evidence (each stage recorded what was true at its accepted checkpoint; none is
rewritten by this remediation):

- **S0.2** (observed at commit `4b4ba3f`, `docs/security/SECURITY_BASELINE.md` §13.2/§19): the
  repository-observed `QadConnectionOptions.cs` defaults were `Encrypt=true` /
  `TrustServerCertificate=true`, which does not express the IT-confirmed required current
  transport (`Encrypt=false`). Reclassified `Informational` → `Confirmed`.
- **S0.3** (check-execution commit `18fdc84`,
  `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §9): re-verified read-only that the
  defaults were *still* `Encrypt=true` / `TrustServerCertificate=true`; **confirmed still present**;
  not remediated.
- **S0.4A** (this remediation, starting commit `fa045ebc`): implemented the correction below.

The previously observed mismatch: the effective QAD connection was built with `Encrypt=true` and
`TrustServerCertificate=true` — an "encrypted but certificate-validation-bypassed" representation
of a connection that the verified infrastructure actually requires to be unencrypted.

## 3. Verified Requirement

Verified current QAD infrastructure requirement (per S0.2 §13.1/§13.2/§15, operator/IT-provided
authority, 2026-08-24):

- **Authentication:** Windows Integrated Authentication (logged-in Windows/domain identity).
- **SQL username/password:** prohibited for QAD access.
- **Transport:** `Encrypt=false`, stated explicitly (the current QAD SQL endpoint does not support
  encrypted client connections from existing supported clients).
- **Certificate trust:** `TrustServerCertificate=true` is **not** used and must not be used as a
  substitute for disabling encryption; with `Encrypt=false`, certificate trust is not applicable.
- **Access:** read-only / least privilege.
- **Network:** internal corporate network only (a required compensating control for the
  unencrypted transport).

**Future target** (not implemented now — it would break the currently supported connection path):
when the QAD SQL infrastructure later supports TLS, the target is `Encrypt=true` /
`TrustServerCertificate=false`. An encrypted configuration using `TrustServerCertificate=true`
would require a separately documented exception.

## 4. Implementation

The single authoritative QAD connection-string construction point is
`QadConnectionStringFactory.Build` (`src/backend/Kst.Integrations.Qad/QadConnectionStringFactory.cs`).
It builds a `Microsoft.Data.SqlClient.SqlConnectionStringBuilder` from `QadConnectionOptions`,
explicitly assigning `IntegratedSecurity = true`, `Encrypt = options.Encrypt`, and
`TrustServerCertificate = options.TrustServerCertificate` (no client-library default reliance; no
`User ID`/`Password` fields exist in this path). All QAD readers and the
`SqlServerQadConnectivityCheck` open connections exclusively through
`QadConnectionFactory.OpenAsync` → `QadConnectionStringFactory.Build`.

The transport values originate from the `QadConnectionOptions` record
(`src/backend/Kst.Integrations.Qad/Options/QadConnectionOptions.cs`), bound in `Kst.Api/Program.cs`
from the `QadDatabase` configuration section. The checked-in `Kst.Api/appsettings.json`
`QadDatabase` section supplies only `Server`/`Database`/`ConnectTimeoutSeconds` (no transport
overrides), so the effective values resolve to the record defaults.

**The change** (single authoritative configuration point — the `QadConnectionOptions` defaults):

| Property | Before | After |
|---|---|---|
| `QadConnectionOptions.Encrypt` | `true` | **`false`** |
| `QadConnectionOptions.TrustServerCertificate` | `true` | **`false`** |

Accompanying doc comments were corrected to state the verified legacy transport constraint and the
future target, with a pointer to this document.

**Resulting effective QAD behavior** (built by the unchanged factory from the corrected options):

- `Encrypt=false` is **explicit** in the effective connection string — not a client-library default.
  (In `Microsoft.Data.SqlClient` 6.1.1, a boolean `Encrypt=false` maps to
  `SqlConnectionEncryptOption.Optional`; the client-library *default* is `Mandatory` — i.e.,
  encryption enforced. So the corrected value differs from the default and is emitted
  explicitly.)
- `IntegratedSecurity=true` — Windows Integrated Authentication (unchanged).
- `TrustServerCertificate` is **not** `true` in the effective configuration (explicitly `false`);
  no certificate-trust bypass.
- No `User ID` / `Password` — no SQL credentials introduced.

**Source/config paths changed:**

- `src/backend/Kst.Integrations.Qad/Options/QadConnectionOptions.cs` (defaults + doc comments).

No change was required to `QadConnectionStringFactory`, `QadConnectionFactory`, any QAD reader, the
connectivity check, `Program.cs` DI wiring, or `appsettings.json` (which never set the transport
values). QAD SQL queries, reader interfaces, database permissions, and production access behavior
are unchanged. No `keytronicshortage` path was touched (see §4.1).

### 4.1 `keytronicshortage` — separate, unchanged

`keytronicshortage` uses a **distinct** options record,
`Kst.Integrations.Shortages.Options.ShortagesConnectionOptions`, which has **no**
`Encrypt` / `TrustServerCertificate` fields and is bound from a separate `ShortagesDatabase`
configuration section. It is wired as `DisabledShortagesConnectivityCheck` (currently
unconfigured/disabled) and has no `SqlConnectionStringBuilder` path. The QAD correction changes
only `QadConnectionOptions`, so **no `keytronicshortage` behavior changed.**

## 5. Regression Coverage

**New test file:** `src/backend/tests/Kst.Integrations.Qad.Tests/Connection/QadConnectionStringFactoryTests.cs`
(4 tests, namespace `Kst.Integrations.Qad.Tests.Connection`).

The tests assert the **effective, generated** connection string — `QadConnectionStringFactory.Build`
output parsed back through `SqlConnectionStringBuilder` — **without opening any connection**. This
reaches the closest practical deterministic boundary to the actual `SqlConnection`/connection string,
so it fails if the confirmed mismatch (`Encrypt=true` / `TrustServerCertificate=true`) is restored via
the options defaults *or* the factory. The options are constructed the way the app effectively
resolves them (Server/Database set, transport at defaults — mirroring the checked-in `appsettings.json`
which supplies no transport overrides).

| Test | Asserts |
|---|---|
| `Effective_Qad_ConnectionString_Explicitly_Disables_Encryption` | `builder.Encrypt == SqlConnectionEncryptOption.Optional` (i.e. `Encrypt=false`, explicit — not the `Mandatory` client-library default) |
| `Effective_Qad_ConnectionString_Uses_Windows_Integrated_Authentication` | `builder.IntegratedSecurity == true` |
| `Effective_Qad_ConnectionString_Does_Not_Trust_Server_Certificate` | `builder.TrustServerCertificate == false` (i.e. not `true`) |
| `Effective_Qad_ConnectionString_Carries_No_Sql_User_Id_Or_Password` | `builder.UserID` empty and `builder.Password` empty (no SQL credentials) |

No secret values are exposed and no database is connected to. The coverage is deliberately focused on
this one remediation; it is not a general secret scanner, SQL-write scanner, DB-permission test, or
runtime TLS test (those belong to S0.5/S0.6/S0.7).

## 6. Verification Results

Repository-established commands from `docs/development/BUILD_AND_TEST.md`, run with `--no-restore`
(warm build state; no restore, no fetch, no network mutation, no live database).

**Targeted regression test:**

```
dotnet test tests/Kst.Integrations.Qad.Tests --no-restore --nologo \
  --filter "FullyQualifiedName~QadConnectionStringFactoryTests"
```
→ **4/4 passed, 0 failed, 0 skipped.**

**Relevant QAD test suite** (existing + new; in-memory / static — no live QAD connection):

```
dotnet test tests/Kst.Integrations.Qad.Tests --no-restore --nologo
```
→ **177/177 passed, 0 failed, 0 skipped.** (173 pre-existing QAD reader/SQL-shape tests + 4 new.)

**Full .NET verification:**

```
dotnet build Kst.slnx --no-restore --nologo
```
→ **Build succeeded — 0 Warning(s), 0 Error(s).** The build-time OpenAPI regeneration
(`docs/openapi/Kst.Api.json`) produced a **byte-identical** file (no tracked-file change).

```
dotnet test Kst.slnx --no-restore --nologo
```
→ **660/660 passed, 0 failed, 0 skipped.**
Breakdown: `Kst.Integrations.Qad.Tests` 177, `Kst.Domain.Tests` 118, `Kst.Application.Tests` 242,
`Kst.ArchitectureTests` 9, `Kst.Api.IntegrationTests` 114.

The change is backend-only (one options record + one test file); it does not cross the frontend or
Rust/Tauri boundary, so no frontend/Rust suites were run (per the scope guidance to avoid ritual
completeness).

**Working-tree check** (after each build/test group): only the two intended changes were present
(`QadConnectionOptions.cs` modified; new `Connection/QadConnectionStringFactoryTests.cs`); no
unexpected generated or tracked-file changes.

## 7. Remaining Uncertainty

- **External runtime configuration override — Unable to Verify.** The corrected repository
  behavior relies on the `QadConnectionOptions` defaults because the checked-in
  `appsettings.json` `QadDatabase` section sets no transport values. However, an
  environment-level runtime source could still override the effective transport at runtime —
  e.g., a local `appsettings.*.local.json` (gitignored by convention) or an environment variable
  such as `QadDatabase__Encrypt` / `QadDatabase__TrustServerCertificate`. Verifying that no such
  override exists on a given workstation/production deployment requires access to that
  workstation/production configuration and is **Unable to Verify** from the repository alone
  (and was not attempted, per the no-secret-inspection boundary). The regression test protects the
  repository-defined behavior; a runtime override outside the repository is out of its reach.
- **Packaged runtime / infrastructure** (S0.7): the actual effective transport on a packaged,
  running instance against the real QAD endpoint is runtime/infrastructure verification, not
  performed here.
- **Server-side grants** (S0.3-G010 → S0.7): unchanged and out of scope; no grant inspection was
  performed.

## 8. Risk-Acceptance Boundary

> **Formal IT/security acceptance of unencrypted QAD transport remains unresolved. This remediation
> does not constitute `Accepted Risk`.**

Resolving the application-configuration finding (`S0.2-F003`) does **not** mean the organization
has accepted unencrypted QAD SQL transport as risk. The following is preserved as a **separate
residual infrastructure issue**, distinct from the corrected application finding:

> **Legacy infrastructure constraint.** The current supported QAD SQL endpoint requires
> `Encrypt=false`. Formal IT/security disposition or risk acceptance of that unencrypted transport
> has not yet been established. This condition is **not** `Accepted Risk`.

The unencrypted QAD SQL transport is a **legacy infrastructure constraint**, not a chosen security
posture and not the desired future state (which is `Encrypt=true` / `TrustServerCertificate=false`
when the infrastructure supports TLS). S0.4A corrects the *application configuration* so it
accurately represents the verified infrastructure reality. It does **not** make the organizational
risk-acceptance decision; that remains an IT/security matter outside engineering authority
(remains unresolved; to be surfaced at S0.8). No severity is assigned and nothing here is marked
`Accepted Risk`.

Runtime confirmation of the effective transport (packaged runtime behavior, external runtime
configuration overrides, infrastructure behavior, and server-side database grants) remains future
work in **S0.7 — Runtime & Infrastructure Verification**, not part of this application remediation.

## 9. Finding Disposition

```
S0.2-F003:
    Resolved — at the KST application-configuration level
Resolution:
    S0.4A QAD SQL Transport Correction, accepted 2026-08-25
```

The project owner accepted the S0.4A implementation on **2026-08-25**. `S0.2-F003` is therefore
**resolved at the KST application-configuration level**: the effective QAD connection configuration
now accurately represents the verified requirement (`Encrypt=false`, `TrustServerCertificate` not
enabled, Windows Integrated Authentication, no SQL credentials), and focused regression coverage
(`QadConnectionStringFactoryTests`) protects it from silently returning.

Resolving the application finding does **not** resolve the separate residual infrastructure issue in
§8 (the legacy unencrypted-transport constraint remains an unresolved IT/security matter, **not**
`Accepted Risk`), nor does it perform runtime/infrastructure verification (**S0.7**). Those distinct
questions remain open for their respective later checkpoints.

# KST v2 Security Implementation Report

**Audience:** engineering management, IT leadership, security reviewers, future maintainers.
**Date:** 2026-08-31
**Basis:** independent S0.8 assurance review at commit `370c1c1ad2999eb9e8406cc1118bac957407a3dc`.
**Companion evidence:** `docs/security/S0_8_INDEPENDENT_ASSURANCE_CLOSEOUT.md` (detailed
traceability). This report is a management summary, not a dump of the S0 evidence corpus.

This report states what is **tested**, what is **inspected**, what was **runtime-observed**,
and what is **Unable to Verify (UTV)**. It does not claim KST is "100% secure," does not claim
exhaustive penetration testing or formal certification (none was performed or obtained), does
not use scanner counts as a security score, does not call any UTV an "Accepted Risk," and does
not imply any AI-accepted risk or any IT/security approval that was not actually obtained.

---

## 1. Executive Summary

**Purpose.** KST v2 is a Windows desktop scheduling application that reads production data from
QAD (the authoritative system of record) and presents it for scheduling work. Its security
foundation (Stage 0, "S0") establishes the controls, verification, and governance that keep the
application safe to operate against production data and safe to develop.

**Current assurance status.** The S0 security foundation is **COMPLETE / ACCEPTED — 2026-08-31**
(S0.8 — Independent Assurance & S0 Closeout: COMPLETE / ACCEPTED — 2026-08-31). An independent
review reconciled the complete S0 evidence, re-ran the repository's security regression tests at
the current commit (all passing), and confirmed that every material security claim is supported
by evidence, every limitation is visible, and no material risk was accepted by an AI agent. The
project owner accepted the S0.8 result and the S0 Security Foundation on 2026-08-31.

**Major architectural protections.**

- The backend listens **only on loopback** (`127.0.0.1`); it is not reachable from the network.
- QAD is a **read-only** system of record; KST **never writes back** to QAD or triggers imports.
- The desktop host (Tauri) runs the backend as a **local sidecar** with a **least-privilege**
  capability surface (no shell/execute capability in the release build).
- A **Content Security Policy** restricts the webview to loopback connections; **CORS** is
  limited to a fixed set of local origins.
- **No secrets** are stored in the repository, logs, or configuration.

**Major verification mechanisms.**

- Repository **security regression tests** (loopback binding, CORS, CSP, Tauri capabilities,
  read-only QAD SQL, architecture boundaries) — re-run at the current commit: **672/672 backend
  and 5/5 Rust tests passing**.
- **Release-runtime verification** of the built application (loopback listener, clean sidecar
  lifecycle, CORS behavior, no non-loopback outbound, no secrets in logs).
- **Database effective-permission verification** (the QAD account is read-only: `db_datareader`,
  SELECT-only, no write/DDL/admin authority).
- **Supply-chain tooling** (admitted and provenance-verified): Rust advisory scanning
  (cargo-audit), secret scanning (Gitleaks), SBOM generation (Syft), static analysis (DevSkim).

**Significant residual / deferred boundaries (all non-blocking).**

- **QAD legacy transport** uses `Encrypt=false` because the current QAD SQL endpoint does not
  support the required TLS. This is a documented residual/external boundary; the formal
  IT/security disposition is an **organizational decision still open** (not accepted by KST or
  by any AI agent). Compensating controls: Windows Integrated authentication, read-only access,
  internal corporate network.
- **KST v1 ↔ v2 package-identity coexistence** (S0.7-F001) is deferred to a packaging/deployment
  decision; it must be resolved before any side-by-side v1/v2 deployment.
- **Installed-package behavior** is UTV (the release executable was verified; the installed
  Windows package was not).
- **keytronicshortage** integration is not connected; its security verification is deferred until
  it exists.

**Is S0 closed out?** **Yes** — the S0 Security Foundation is **COMPLETE / ACCEPTED —
2026-08-31**. The remaining items are documented residual/external boundaries with owners and
triggers, not blocking defects; S0 acceptance does not erase them. Stage 9 is now **UNBLOCKED /
NOT STARTED** (permitted to begin after this finalization is committed and pushed).

---

## 2. Security Architecture

- **Windows desktop architecture.** KST v2 is a Windows desktop application. The user-facing
  layer is a React/TypeScript frontend hosted in a Tauri (Rust) webview.
- **Tauri host.** The Tauri host manages the desktop window and the lifecycle of a local backend
  process. It runs with a **least-privilege** capability set: the release build grants only the
  default core capabilities and **no shell/execute capability**.
- **Local .NET sidecar.** The backend is a .NET (C#) API that the Tauri host launches as a local
  "sidecar" process. It binds **only to loopback** (`127.0.0.1`) on an OS-assigned port, so it
  is not reachable from other machines.
- **Loopback-only boundary.** The loopback binding is enforced in code (the backend unconditionally
  sets an explicit `127.0.0.1` endpoint), protected by regression tests, and was runtime-observed
  on the built application. An operator environment variable can no longer override it.
- **QAD system-of-record relationship.** QAD is the authoritative operational system of record.
  KST is a **read-only consumer** of QAD. It retrieves, analyzes, displays, validates, stages
  locally, and exports proposed changes — but it **never writes back** to QAD, never submits
  changes into QAD/QXtend, and never triggers an import. Operational changes leave KST as
  human-reviewable files for external processing.
- **No direct production QAD write-back.** Verified at three levels: the application emits only
  read-only SQL (regression-tested); the effective QAD account has no write/DDL/admin authority
  (verified against the live database); and the architecture assigns no write path.
- **Current database authentication model.** QAD access uses **Windows Integrated
  authentication** (the logged-in Windows/domain identity). There is **no SQL credential** path.
  The effective account is read-only (`db_datareader`, SELECT-only). The connection is on an
  internal corporate network.

---

## 3. Development and AI Security Controls

- **Controlled agent workflow.** Development is governed by enacted repository policy
  (`AGENTS.md`, `SECURITY.md`, and the `docs/security/` policy set). Work is scoped to accepted
  stages/checkpoints; the repository is the authoritative project memory.
- **Human approval boundaries.** New dependencies, tools, and security-relevant changes require
  explicit human approval through the dependency-admission process. An AI agent **cannot** accept
  material security risk, install tooling silently, or make organizational risk-acceptance
  decisions.
- **No silent dependencies/tools.** Every security tool used in S0 was explicitly admitted
  (human-approved) before installation. No extension, plugin, package, skill, or MCP server was
  installed silently.
- **AI risk-acceptance prohibition.** No S0 finding is marked "Accepted Risk." Unresolved
  material risk is documented and surfaced, never silently accepted.
- **Secret/data-handling expectations.** No secrets, credentials, or connection strings with
  credentials are stored in source, logs, tests, or documentation. Local secret files are
  excluded from version control. The S0 evidence corpus was reviewed and no real secret was
  found.
- **External AI/provider boundaries.** The list of approved external AI providers is an
  organizational decision that remains open; no unapproved external provider was relied on for a
  security determination.

---

## 4. Dependency and Supply-Chain Controls

- **Dependency admission.** All third-party dependencies and tools follow the dependency-admission
  process (provenance, integrity, licensing, human approval) before use.
- **Exact/pinned dependency evidence.** The npm `package-lock.json` and Rust `Cargo.lock` are
  committed, pinning the resolved dependency graph. (The .NET graph is not committed as a lockfile
  — a documented boundary; the resolved graph is the last-restored state.)
- **Rust advisory scanning.** cargo-audit 0.22.2 (admitted, provenance-verified) scans the Rust
  dependency graph against the RustSec advisory database.
- **Secret scanning.** Gitleaks v8.30.0 (admitted, provenance-verified, synthetic-canary
  validated) scans current content and full Git history. All matches were confirmed
  documentation false positives (literal example key headers quoted in security docs), not real
  secrets.
- **SBOM.** Anchore Syft v1.51.1 (admitted, provenance-verified) generates a Software Bill of
  Materials from the repository/build dependency evidence (SPDX and CycloneDX). The full
  installer/application-bundle SBOM is UTV (deferred to packaging).
- **SAST.** Microsoft DevSkim CLI v1.0.90 (admitted, provenance-verified, self-verified rule
  corpus) performs local static security linting. It is **rule/pattern-based**, not deep
  cross-file semantic/taint analysis — its limitations are stated accurately.
- **Tool provenance/admission.** Each tool's release integrity was verified (official checksums,
  signatures where available) before use.
- **Third-party licensing governance.** An enacted licensing policy governs all third-party
  software. Every admitted tool has a recorded licensing disposition (cargo-audit: Apache-2.0 OR
  MIT; Gitleaks: MIT; Syft: Apache-2.0; DevSkim: MIT). A broader retrospective license
  inventory/reconciliation is a deferred post-S0 governance follow-up.

**Tool limitations (stated accurately):** the scanners are rule/pattern-based (not deep semantic
analysis); the SBOM is a dependency inventory, not a statement that all license obligations are
resolved; the Rust advisory check does not by itself remediate or set a dependency-health policy.

---

## 5. Application Hardening

- **Enforced loopback binding.** The backend binds only to `127.0.0.1`; an operator environment
  override can no longer change the listener (remediated and regression-protected).
- **Runtime override remediation.** The pre-fix weakness where an inherited `ASPNETCORE_URLS`
  could alter the listener was confirmed, remediated, and re-verified on the release runtime.
- **CORS.** Limited to a fixed set of five local origins; no `AllowAnyOrigin` and no
  `AllowCredentials`. Regression-protected and runtime-observed.
- **CSP.** The webview Content Security Policy restricts connections to loopback and uses a
  `default-src 'self'` script policy (no `unsafe-inline`/`unsafe-eval`/remote scripts).
  Release-build artifact evidence; dynamic webview enforcement is UTV.
- **Tauri least privilege.** The release capability set is `core:default` only, with no
  shell/execute capability. Build-artifact evidence; dynamic enforcement is UTV.
- **Sidecar lifecycle.** The backend is launched and stopped cleanly with no orphan process or
  listener (runtime-observed).
- **Logging/error handling.** Logs contain no connection strings, credentials, tokens, customer
  data, or stack traces; error responses are safe Problem Details. (The true server-exception
  path is UTV.)
- **Database read-only architecture.** The application emits only read-only SQL (regression-
  tested) and the effective QAD account is read-only (verified). No write-back path exists.

---

## 6. Verification Performed

| Area | Method | Result |
|---|---|---|
| Loopback binding | Regression tests + release-runtime observation | **Tested + runtime-observed** (672/672 backend incl. `LoopbackBindingTests`; release sidecar on `127.0.0.1` only, no wildcard/LAN) |
| CORS | Regression tests + release-runtime observation | **Tested + runtime-observed** (exact 5-origin set; no `AllowAnyOrigin`/credentials) |
| CSP | Regression tests + release-build artifact | **Tested + inspected** (release artifact); **dynamic enforcement UTV** |
| Tauri capabilities | Regression tests + build-artifact inspection | **Tested + inspected** (build artifacts); **dynamic enforcement UTV** |
| Read-only QAD SQL | Regression tests | **Tested** (application-emitted SQL) |
| QAD effective permissions | Live read-only metadata + runtime evidence | **Runtime-observed** (read-only: `db_datareader`, SELECT-only, no write/DDL/admin) |
| Sidecar lifecycle | Release-runtime observation | **Runtime-observed** (clean, no orphans) |
| Logging / error handling | Release-runtime observation | **Runtime-observed** (no secrets; safe errors); true 500 path **UTV** |
| Security tools (cargo-audit, Gitleaks, Syft, DevSkim) | Admitted, provenance-verified, run in S0.6 | **Verified** (accepted S0.6 evidence; not re-run in S0.8) |
| Independent S0.8 reconciliation | Evidence review + regression-test re-run at HEAD | **Performed** (this pass) |

**Distinguished:** *tested* (regression tests), *inspected* (source/artifact review),
*runtime-observed* (built application behavior), and *Unable to Verify* (installed-package
behavior, dynamic CSP/capability enforcement, true 500 path, keytronicshortage, full-installer
SBOM, server-side Kerberos/NTLM and transport topology).

---

## 7. Findings and Dispositions

Management-level summary. Detailed traceability is in the S0.8 evidence document (§6). **No
finding is an "Accepted Risk."**

- **Remediated (1):** the operator `ASPNETCORE_URLS` override weakness (S0.5-F001) — confirmed,
  remediated (the backend now unconditionally binds loopback), and re-verified on the release
  runtime with failure-safe regression tests.
- **Resolved (3):** the Tauri shell-capability scope (S0.2-F001, resolved by least-privilege
  remediation); the QAD SQL transport configuration mismatch (S0.2-F003, resolved at the
  application-configuration level); and the npm development-tooling advisories (S0.3-F001,
  resolved by dependency remediation).
- **Retired (2):** the database read-only enforcement finding (S0.2-F002, retired per
  operator/IT authority; grant verification is represented separately and was completed); and
  the QAD read-scope "least-privilege gap" (S0.7-F002, retired after the
  application-vs-enterprise identity scope model was corrected — the broad read scope belongs to
  the operator's pre-existing enterprise identity, governed outside KST).
- **Deferred (1):** KST v1 ↔ v2 package-identity coexistence (S0.7-F001) — an operational
  packaging/deployment decision, non-blocking, to be resolved before any side-by-side v1/v2
  deployment.
- **Informational (23):** dependency-health observation, the Gitleaks documentation false
  positives, the Syft SBOM capability boundaries, and the DevSkim rule-limitation/false-positive
  findings. None is blocking; none is a suppressed or waived vulnerability.
- **UTV / external-organizational boundaries:** installed-package behavior, dynamic
  CSP/capability enforcement, the true 500 path, keytronicshortage, the QAD legacy transport
  disposition, and the intentionally unresolved organizational decisions (risk-acceptance
  authority, severity thresholds, external AI providers).

---

## 8. Residual / Deferred Boundaries

| Boundary | What it is | Blocking? | Owner / trigger |
|---|---|---|---|
| **QAD legacy `Encrypt=false` transport** | The current QAD SQL endpoint does not support the required TLS, so the connection is unencrypted. Compensating controls: Windows Integrated auth, read-only, internal corporate network. Future target: encrypted transport when infrastructure permits. | **No** (documented residual/external boundary) | **IT/security** — formal disposition or risk acceptance (organizational; not accepted by KST or any AI agent) |
| **S0.7-F001 package-identity coexistence** | KST v1 and v2 share an application identity, so a single-instance check may intercept the other. Operational, not a security vulnerability. | **No** (deferred) | Packaging/deployment decision; **must be resolved before any side-by-side v1/v2 deployment** |
| **Installed-package UTV** | The release executable was verified; the installed Windows package was not. | **No** (UTV) | Safe installation environment if the owner authorizes (not improvised) |
| **keytronicshortage future verification** | The integration is not connected. Before activation, verify dedicated identity, credential storage, permission scope, transport/topology, and logging/secret handling. | **No** (deferred) | Before the integration becomes active |
| **Licensing inventory/reconciliation** | A broader retrospective third-party license inventory. | **No** (deferred governance work) | Post-S0 governance follow-up |
| **Organizational AI/provider decisions** | Approved external AI provider list; organizational risk-acceptance authority; final severity thresholds. | **No** (intentionally unresolved) | Organizational decision (owner/IT) |

**Blocking vs non-blocking:** none of the above is blocking for S0 closeout. Each has an owner
and a trigger. The QAD legacy transport is the most significant residual boundary and is the
primary item for IT/security attention.

---

## 9. Security Posture Going Forward

A re-review is triggered by any of the following:

- **New dependency or tool** (any ecosystem) — dependency-admission + licensing gate.
- **Database write capability** (any path) — re-review of the read-only architecture.
- **New external/network service** (beyond the configured QAD server) — re-review of the network
  boundary.
- **keytronicshortage activation** — verify identity, credential storage, permission scope,
  transport/topology, and logging/secret handling before go-live.
- **Packaging/deployment changes** — resolve S0.7-F001 coexistence; verify installed-package
  behavior; produce a full-installer SBOM.
- **Identity/authentication model changes** — re-verify QAD effective permissions and the
  enterprise-identity scope model.
- **AI/tool-provider changes** — re-run the dependency-admission + licensing gate; external AI
  provider decisions remain organizational.
- **Material Tauri/backend security architecture changes** — re-run the security regression
  tests and the independent review.
- **QAD infrastructure enables TLS** — move to `Encrypt=true` / `TrustServerCertificate=false`
  and close the legacy-transport residual boundary.

**Who owns the remaining decisions:** the project owner (stage acceptance, packaging/deployment,
severity thresholds) and IT/security (QAD transport disposition, database grant confirmation,
organizational risk-acceptance authority, external AI provider list). An AI agent does not own
any of these.

**What causes another security review:** any of the triggers above, or any material change to the
security architecture, dependencies, database access, or integration surface.

---

## 10. Evidence Appendix

Canonical repository evidence (no secrets or raw infrastructure data reproduced):

- `SECURITY.md` — security entry point and current S0 status.
- `docs/security/SECURITY_ASSURANCE_POLICY.md` — primary normative security policy.
- `docs/security/APPLICATION_SECURITY_PROFILE.md` — declared required security properties.
- `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md` — development-environment controls.
- `docs/security/DEPENDENCY_ADMISSION.md` — dependency/tool admission process.
- `docs/security/AI_SECURITY_REVIEW.md` — AI security review model.
- `docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md` — licensing governance.
- `docs/security/SECURITY_BASELINE.md` — S0.2 baseline and findings.
- `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` — S0.3 checks, gaps G001–G010.
- `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md` — QAD transport remediation.
- `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md` — Tauri capability remediation.
- `docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md` — npm dev-advisory remediation.
- `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md` — security regression tests.
- `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`, `S0_6_SECRET_SCANNING_ADMISSION.md`,
  `S0_6_SBOM_ADMISSION.md`, `S0_6_SAST_ADMISSION.md` — S0.6 tool admissions.
- `docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md` — S0.7A runtime verification.
- `docs/security/S0_7_DATABASE_INFRASTRUCTURE_PERMISSION_VERIFICATION.md` — S0.7B database
  permission verification.
- `docs/security/S0_8_INDEPENDENT_ASSURANCE_CLOSEOUT.md` — this closeout's detailed evidence.
- `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` — canonical S0.8 scope.
- `docs/status/CURRENT_PROJECT_STATUS.md`, `KST-v2-Master-Project-Checklist.md` — current status.

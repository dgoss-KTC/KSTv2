# Current Project Status

Date: 2026-08-21
Workstation: Windows (`C:\Dev\kst_v2`)
Current stage: **Stage 8 — Component and BOM Detail — COMPLETE / ACCEPTED — 2026-08-21**
UI Navigation & Keyboard Ergonomics A: **COMPLETE / ACCEPTED — 2026-08-21**
Active cross-cutting effort: **R0 — Repository / Documentation Reconciliation — COMPLETE /
ACCEPTED — 2026-08-21** (see `R0 — Repository / Documentation Reconciliation Status` below and
`docs/status/R0_REPOSITORY_RECONCILIATION_CLOSEOUT.md`)
Current cross-cutting effort: **S0 — Security Foundation Integration — COMPLETE / ACCEPTED — 2026-08-31** (S0.1 — Security
Policy Injection — **COMPLETE / ACCEPTED — 2026-08-21**; S0.2 — Security Baseline Discovery —
**COMPLETE / ACCEPTED — 2026-08-24**; S0.3 — Existing-Tool Security Checks — **COMPLETE /
ACCEPTED — 2026-08-24**; see `SECURITY.md` and `docs/security/`)
Current: **S0.4 — Security Finding Disposition & Bounded Remediation — COMPLETE / ACCEPTED —
2026-08-25** (S0.4A — QAD SQL Transport Correction — **COMPLETE / ACCEPTED — 2026-08-25**; S0.4B —
Tauri Shell Capability — **COMPLETE / ACCEPTED — 2026-08-25**; S0.4C — npm Development-Tooling
Advisories — **COMPLETE / ACCEPTED — 2026-08-25**; see
docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md,
docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md,
docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md and "Remaining S0 Work" below)
Current: **S0.6 — Security Tool Admission — COMPLETE / ACCEPTED — 2026-08-27** (Capability Review 1 — Rust
Dependency Advisory Capability, gap `S0.3-G001` — **COMPLETE / ACCEPTED — 2026-08-26**:
cargo-audit 0.22.2 ADMITTED / ACCEPTED; cargo-deny 0.20.2 DEFERRED; S0.3-G001 — Covered /
Resolved — see
docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md; Capability Review 2 — Dedicated Secret
Scanning, gap `S0.3-G007` — **COMPLETE / ACCEPTED — 2026-08-27** (Gitleaks
v8.30.0 installed, release-integrity and synthetic-canary verified, scanned current KST content
(4 findings) and full Git history (8 findings), all rule `private-key`, confirmed documentation
false positives; `S0.3-G007` — Covered / Resolved) — research at
docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md, owner decision and implementation
evidence at docs/security/S0_6_SECRET_SCANNING_ADMISSION.md (Gitleaks v8.30.1, TruffleHog
v3.97.1, detect-secrets v1.5.0 DEFERRED); Capability Review 3 — Software Bill of Materials
(SBOM), gap `S0.3-G008` — **COMPLETE / ACCEPTED — 2026-08-27** —
Anchore Syft v1.51.1 installed and release-integrity verified, run against KST build/repository
evidence (SPDX 2.3, 1,027 packages; CycloneDX 1.6, 1,026 components) and a complementary
packaged-artifact view (published `Kst.Api` sidecar, 37 NuGet packages recovered directly from
the single-file executable) — research at docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md (neutral
research packet, not a recommendation or admission decision) and
docs/security/S0_6_SBOM_ADMISSION.md (owner decision and implementation evidence; Anchore Syft
v1.51.1 — ADMITTED / IMPLEMENTED / ACCEPTED; Microsoft sbom-tool v4.1.5 and the CycloneDX
ecosystem-native approach DEFERRED; six informational findings `S0.6-F014`–`S0.6-F019`, none
blocking; complete Tauri Windows installer/application bundle Unable to Verify / future
packaged-release verification boundary, not Accepted Risk); `S0.3-G008` — Covered / Resolved;
Capability Review 4 — Dedicated Static Application Security Testing (SAST), gap `S0.3-G006` —
**COMPLETE / ACCEPTED — 2026-08-27** — owner reviewed the neutral research packet
comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90 and admitted
DevSkim CLI v1.0.90, which was installed, self-verified, synthetically validated, and
run against the KST source tree (50 findings across 3 bundled rules; `S0.6-F020` reviewed
2026-08-27 and reclassified to Informational / Framework-Local Origin / Confirmed DevSkim False
Positive for plaintext-network interpretation; `S0.6-F021` — Informational / Known DevSkim Rule
Limitation; neither Accepted Risk); Semgrep CE v1.175.0 and CodeQL CLI v2.26.4 DEFERRED (not
rejected) pending organizational licensing/entitlement review — see
docs/security/S0_6_SAST_ADMISSION_RESEARCH.md and
docs/security/S0_6_SAST_ADMISSION.md; `S0.3-G006` — Covered / Resolved; Microsoft DevSkim CLI
v1.0.90 — ADMITTED / INSTALLED / VERIFIED / ACCEPTED. With `S0.3-G001`, `S0.3-G006`, `S0.3-G007`,
and `S0.3-G008` all Covered / Resolved, **S0.6 — Security Tool Admission is COMPLETE / ACCEPTED —
2026-08-27**; S0.7 —
Runtime & Infrastructure Verification
(**COMPLETE / ACCEPTED — 2026-08-28** — S0.7A — Local Release Runtime Verification working pass —
**COMPLETE / ACCEPTED — 2026-08-28**: 2026-08-27 evidence pass VALID /
ACCEPTED AS EVIDENCE by owner review, and 2026-08-28 `S0.5-F001` loopback-binding remediation
implemented + release-runtime re-verified — `S0.3-G009` Covered / Resolved; S0.7B — Database /
Infrastructure
Permission Verification — **COMPLETE / ACCEPTED — 2026-08-28**
(`S0.3-G010` — **Covered / Resolved — 2026-08-28**; **`S0.7-F002`** — **RETIRED** / Application-vs-Enterprise Identity Scope Model Corrected — 2026-08-28 owner scope decision; NOT Accepted Risk; NOT a waived vulnerability; NOT evidence deletion) — see
docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md +
docs/security/S0_7_DATABASE_INFRASTRUCTURE_PERMISSION_VERIFICATION.md); S0.8 — Independent Assurance & S0
Closeout (**COMPLETE / ACCEPTED — 2026-08-31** — project-owner acceptance recorded; S0 — **COMPLETE / ACCEPTED — 2026-08-31** — see docs/security/S0_8_INDEPENDENT_ASSURANCE_CLOSEOUT.md and docs/security/KST_V2_SECURITY_IMPLEMENTATION_REPORT.md)
Cross-cutting governance foundation: **Third-Party Software & Licensing Governance — ENACTED /
ACCEPTED — 2026-08-27** — see
docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md (governs license/commercial-use
admission for third-party dependencies, developer tooling, and services; integrated into
docs/security/DEPENDENCY_ADMISSION.md and AGENTS.md as a supplement to, not a replacement for,
existing security admission requirements); now a governing prerequisite for future third-party
dependency/tool admission. S0.6 is now COMPLETE / ACCEPTED — 2026-08-27; S0.7 is now COMPLETE /
ACCEPTED — 2026-08-28 (S0.7A — Local Release Runtime Verification — COMPLETE / ACCEPTED —
2026-08-28: 2026-08-27 evidence valid/accepted as evidence; 2026-08-28 S0.5-F001 remediation
implemented and re-verified, S0.3-G009 Covered / Resolved; S0.7B — database/infrastructure
permission verification — COMPLETE / ACCEPTED — 2026-08-28, S0.3-G010 Covered / Resolved,
S0.7-F002 RETIRED);
S0.8 is now COMPLETE / ACCEPTED — 2026-08-31. S0 is now COMPLETE / ACCEPTED — 2026-08-31.
Stage 9: **UNBLOCKED / NOT STARTED**
Stage 7 status: **COMPLETE / ACCEPTED — 2026-08-13**
Stage 6 status: **COMPLETE / ACCEPTED — 2026-08-11 — commit `863a638`**
Application version: **`0.1.0-alpha.2`** (see [Versioning Foundation](#versioning-foundation) below)

## Current Position

**Final S0 closeout:** S0 — **COMPLETE / ACCEPTED — 2026-08-31**; S0.8 — **COMPLETE / ACCEPTED — 2026-08-31**; S0.7 — **COMPLETE / ACCEPTED — 2026-08-28**. Stage 9 — **UNBLOCKED / NOT STARTED**.

Stages 1 through 8 are complete and accepted, and UI Navigation & Keyboard Ergonomics A is complete
and accepted. The project is not yet working on Stage 9. R0 — Repository / Documentation
Reconciliation is complete and accepted. The current cross-cutting effort is **S0 — Security
Foundation Integration**; S0.1 — Security Policy Injection and S0.2 — Security Baseline Discovery
are complete and owner-accepted, and S0.3 — Existing-Tool Security Checks is complete and
owner-accepted (see `SECURITY.md`, `docs/security/`, and the S0 section of
`KST-v2-Master-Project-Checklist.md`). The remaining S0 work is approved as checkpoints
S0.4–S0.8 (see "Remaining S0 Work" below and
docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md). S0.4 — Security Finding
Disposition & Bounded Remediation is **COMPLETE / ACCEPTED — 2026-08-25**: S0.4A — QAD SQL
Transport Correction is **COMPLETE / ACCEPTED — 2026-08-25** (accepted remediation evidence:
`docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`; resolves `S0.2-F003` at the
application-configuration level); S0.4B — Tauri Shell Capability is **COMPLETE / ACCEPTED —
2026-08-25** (resolves `S0.2-F001`; accepted remediation evidence:
`docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`); S0.4C — npm
Development-Tooling Advisories is **COMPLETE / ACCEPTED — 2026-08-25** (resolves `S0.3-F001`;
accepted remediation evidence:
`docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md`). All three S0.4 sub-checkpoints are
owner-accepted. S0.5 — Security Regression & Architecture Checks is **COMPLETE / ACCEPTED —
2026-08-26** (accepted implementation evidence:
docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md). S0.6 — Security Tool
Admission is **COMPLETE / ACCEPTED — 2026-08-27**: Capability Review 1 (Rust Dependency Advisory Capability, gap
`S0.3-G001`) is **COMPLETE / ACCEPTED — 2026-08-26** — cargo-audit 0.22.2 ADMITTED /
ACCEPTED (installed, verified, owner-accepted) per
docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md (S0.3-G001 — Covered / Resolved;
cargo-deny 0.20.2 DEFERRED); Capability Review 2 — Dedicated Secret Scanning (gap
`S0.3-G007`) is **COMPLETE / ACCEPTED — 2026-08-27** (Gitleaks v8.30.0 installed,
release-integrity and synthetic-canary verified, scanned current KST content (4 findings) and full
Git history (8 findings), all rule `private-key`, confirmed documentation false positives;
`S0.3-G007` — Covered / Resolved) — see docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md
(neutral research packet, not a recommendation or admission decision) and
docs/security/S0_6_SECRET_SCANNING_ADMISSION.md (owner decision and implementation evidence;
Gitleaks v8.30.0 — ADMITTED / IMPLEMENTED / ACCEPTED; Gitleaks v8.30.1, TruffleHog v3.97.1,
detect-secrets v1.5.0 DEFERRED); Capability Review 3 —
Software Bill of Materials (SBOM), gap `S0.3-G008`, is **COMPLETE / ACCEPTED
— 2026-08-27** — Anchore Syft v1.51.1 installed (a pre-existing binary was
independently verified byte-identical to a freshly verified official release), release-integrity
verified, and run against KST build/repository evidence (SPDX 2.3, 1,027 packages; CycloneDX 1.6
via the explicit `@1.6` selector, 1,026 components) and a complementary packaged-artifact view
(the published self-contained single-file `Kst.Api` sidecar, from which 37 NuGet packages were
recovered directly) — see
docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md (neutral research packet, not a recommendation or
admission decision) and docs/security/S0_6_SBOM_ADMISSION.md (owner decision and implementation
evidence; Anchore Syft v1.51.1 — ADMITTED / IMPLEMENTED / ACCEPTED; Microsoft sbom-tool v4.1.5
and the CycloneDX ecosystem-native approach — cyclonedx-dotnet
6.2.0, cyclonedx-npm 6.0.1, cargo-cyclonedx 0.5.9 — remain DEFERRED; six informational findings
`S0.6-F014` through `S0.6-F019` recorded, none blocking; complete Tauri Windows
installer/application bundle Unable to Verify / future packaged-release verification boundary, not
Accepted Risk); `S0.3-G008` is Covered /
Resolved; Capability Review 4 — Dedicated Static Application Security Testing (SAST), gap
`S0.3-G006`, is **COMPLETE / ACCEPTED — 2026-08-27** — the owner reviewed the
neutral research packet comparing
Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90 (see
docs/security/S0_6_SAST_ADMISSION_RESEARCH.md; not a recommendation or admission decision) and
admitted Microsoft DevSkim CLI v1.0.90, which was installed, self-verified,
synthetically validated (C#, JavaScript/TypeScript, Rust, SQL), and run against the KST source
tree (50 findings across 3 bundled rules; `S0.6-F020` reviewed 2026-08-27 and reclassified to
Informational / Framework-Local Origin / Confirmed DevSkim False Positive for
plaintext-network interpretation; `S0.6-F021` — Informational / Known DevSkim Rule Limitation,
neither Accepted
Risk) (see docs/security/S0_6_SAST_ADMISSION.md); Semgrep CE v1.175.0 and CodeQL CLI v2.26.4
DEFERRED (not rejected) pending organizational licensing/entitlement review; `S0.3-G006` is
Covered / Resolved; Microsoft DevSkim CLI v1.0.90 — ADMITTED / INSTALLED / VERIFIED / ACCEPTED.
With `S0.3-G001`, `S0.3-G006`, `S0.3-G007`, and `S0.3-G008` all Covered / Resolved, **S0.6 —
Security Tool Admission is COMPLETE / ACCEPTED — 2026-08-27**. The
cross-cutting **Third-Party
Software & Licensing Governance** foundation is **ENACTED / ACCEPTED — 2026-08-27** — see
docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md, integrated into
docs/security/DEPENDENCY_ADMISSION.md and AGENTS.md as a licensing/commercial-use admission gate
supplementing (not replacing) existing security admission requirements; this is now a governing
prerequisite for future third-party dependency/tool admission. Stage 9 begins only after S0 is formally
closed and accepted.

## S0 — Security Foundation Integration

**Status:** COMPLETE / ACCEPTED — 2026-08-31

### S0.1 — Security Policy Injection

**Status:** COMPLETE / ACCEPTED — 2026-08-21

This checkpoint converted the Security Foundation working draft
(`docs/reference/security/KST v2 Security Foundation — Initial Policy and Enactment Draft.md`,
retained as historical design provenance) into a durable, repository-integrated, enacted security
policy framework:

- `SECURITY.md` — repository security entry point.
- `docs/security/SECURITY_ASSURANCE_POLICY.md` — primary normative platform-neutral policy.
- `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md` — coding-environment/agent expectations.
- `docs/security/DEPENDENCY_ADMISSION.md` — dependency and development-tool admission rules.
- `docs/security/AI_SECURITY_REVIEW.md` — independent AI security-review model.
- `docs/security/APPLICATION_SECURITY_PROFILE.md` — KST-specific declared security properties.
- `AGENTS.md` §8 — concise mandatory agent security behavior, pointing to the policy above.

This checkpoint was documentation/policy work only: no security baseline discovery (S0.2), no
existing-tool security checks (S0.3), and no security remediation were performed. No application
code, tests, or dependency manifests changed. `docs/security/SECURITY_BASELINE.md` did not exist as
of this checkpoint — it was produced by the subsequent S0.2 checkpoint (see below).

The policy documents above are enacted, owner-accepted Tier 1 authority as of 2026-08-21.

### S0.2 — Security Baseline Discovery

**Status:** COMPLETE / ACCEPTED — 2026-08-24

This checkpoint produced an observational (not normative) security baseline:
`docs/security/SECURITY_BASELINE.md`, covering repository dependencies (NuGet/npm/Cargo), SDK/build
tooling, the development/AI-agent environment, and the application's networking, CORS/CSP/Tauri
capability, subprocess, filesystem, credential, database, external-destination, logging, and
packaging surfaces, observed against commit `4b4ba3f`.

Three observations were initially recorded (`S0.2-F001` Tauri shell-capability scope,
`S0.2-F002` database-level read-only enforcement, `S0.2-F003` QAD `TrustServerCertificate` default).
Following a 2026-08-24 correction incorporating project-owner/IT-provided operational authority on
QAD authentication, transport, and authorization:

- `S0.2-F001` (Tauri shell-capability scope) remains `Potential / Investigation Required`.
- `S0.2-F002` (database-level read-only enforcement) is **retired** — QAD's Windows Integrated
  Authentication plus a prohibition on SQL-authenticated access corroborates read-only/least-
  privilege access at the authentication-policy level.
- `S0.2-F003` (QAD transport configuration) is **reclassified to `Confirmed`** — the
  repository-observed `TrustServerCertificate=true` configuration does not accurately express the
  IT-confirmed required transport configuration (`Encrypt=false`, because the current QAD SQL
  infrastructure does not support encrypted client connections). No severity is assigned and the
  underlying unencrypted-transport constraint is explicitly **not** marked `Accepted Risk` —
  formal IT/security risk acceptance remains unresolved. Recommended remediation
  (`Encrypt=false`, remove `TrustServerCertificate=true`) is deferred to an explicitly authorized
  future remediation checkpoint.

No remediation, tool installation, vulnerability scanning, or SBOM generation was performed.
`SECURITY.md` was updated to link the accepted baseline.

This checkpoint was documentation/observation work only: no application code, tests, dependency
manifests, or security controls changed.

**Next:** S0.3 — Existing-Tool Security Checks (see below).

### S0.3 — Existing-Tool Security Checks

**Status:** COMPLETE / ACCEPTED — 2026-08-24

This checkpoint executed security-relevant checks using only the repository's existing
toolchain: existing tests, project compilers/analyzers/linters, ecosystem-native
package-manager advisory functionality of the already-installed toolchain (the only authorized
external advisory queries), and read-only Git/native checks. No tool was installed or
activated, no remediation was performed, no dependency manifest or lockfile changed, no
configuration or security control changed, no database connection was made, no SQL was
executed, and no application/sidecar was launched. Evidence:
`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (accepted verification/check
evidence; not normative policy).

Key results:

- Backend: analyzer-enabled build 0 warnings/0 errors; **656/656 tests passing**, including
  `DependencyRuleTests` (6/6), `VersionConsistencyTests` (3/3), and `CorsPolicyTests` (2/2 —
  partial origin coverage; 3 of 5 configured origins and the no-`AllowAnyOrigin`/
  no-credentials properties are not test-verified).
- Frontend: lint clean, typecheck clean, **281/281 tests passing** (general quality; no
  security-relevant assertions identified).
- Rust: `cargo clippy --locked --offline` — 2 style-only warnings (`needless_return`), no
  security diagnostics; **0 tests exist** in the Tauri crate.
- NuGet advisory check (native, transitive included, no restore): **no known advisories**
  reported for the evaluated (last-restored) dependency graph.
- npm advisory check (native): **3 advisories**, all in development-only packages
  (`openapi-typescript`, transitive `undici`, transitive `nanoid`) — recorded as
  **S0.3-F001 (Confirmed)**; unremediated by design of this checkpoint; disposition
  (reachability analysis + dependency-admission decision) remains for a later explicitly
  authorized checkpoint. npm-reported severities are not KST risk severities.
- Cargo advisory: **no authorized/available Rust dependency advisory scanner exists**
  (`cargo-audit`/`cargo-deny` absent) — recorded as a gap.
- S0.2 finding re-verification: **S0.2-F001** remains `Potential / Investigation Required`
  (no existing least-privilege verification identified); **S0.2-F002** remains retired;
  **S0.2-F003** re-verified **still present** at the check-execution commit (read-only
  configuration check only; not remediated; not `Accepted Risk`).
- Ten existing-tool coverage gaps recorded (**S0.3-G001** through **S0.3-G010**), including
  Rust advisories, dedicated secret scanning, SAST, SBOM, runtime listener verification, CSP
  verification, Tauri least-privilege verification, and database-grant verification. Candidate
  later needs are listed as capability categories only — no scanner/SAST/SBOM/CI selection is
  made.

`SECURITY_BASELINE.md` was **not modified**; the accepted S0.2 baseline remains the snapshot.

**Remaining S0 work:** approved as checkpoints S0.4–S0.8 in the remaining-S0 work plan (see
"Remaining S0 Work" below).

### S0.4 — Security Finding Disposition & Bounded Remediation

**Status:** COMPLETE / ACCEPTED — 2026-08-25

S0.4 addressed the open findings established by accepted S0.2/S0.3 evidence, organized as three
bounded sub-checkpoints (full scope/boundaries in
docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md). All three sub-checkpoints
(S0.4A, S0.4B, S0.4C) are owner-accepted; S0.4 is complete.

- **S0.4A — QAD SQL Transport Correction: COMPLETE / ACCEPTED — 2026-08-25** (implemented and
  verified 2026-08-24; owner-accepted 2026-08-25). Corrects the confirmed `S0.2-F003`
  configuration mismatch — the effective QAD connection now establishes `Encrypt=false` (explicit)
  with `TrustServerCertificate` not set to `true`, preserving Windows Integrated Authentication,
  read-only / least-privilege access, and the internal-network restriction. **Resolves
  `S0.2-F003` at the KST application-configuration level.** No `keytronicshortage`, Tauri-capability,
  or npm-dependency change. Accepted remediation evidence (**not normative policy**):
  `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`. Regression coverage:
  `QadConnectionStringFactoryTests`; full backend suite 660/660 passing at implementation. The
  underlying unencrypted-transport constraint remains a **separate residual infrastructure issue —
  not** `Accepted Risk`; formal IT/security risk acceptance and runtime/infrastructure
  verification remain unresolved (S0.7/S0.8).
- **S0.4B — Tauri Shell Capability: COMPLETE / ACCEPTED — 2026-08-25** (implemented and
  verified 2026-08-25, including owner-guided live sidecar lifecycle verification; owner-accepted
  2026-08-25). **Resolves `S0.2-F001`:** the webview capability now grants exactly
  `core:default` (the unused `shell:allow-execute` / `shell:allow-open` grants are removed; the
  `Kst.Api` sidecar and the PID-scoped powershell/taskkill shutdown handling run from the Rust
  host, outside the webview IPC surface). Regression coverage: `capability_guard` tests in
  `src/tauri/src/lib.rs` (`cargo test` in `src/tauri`); mutation check confirmed
  shell-permission reintroduction is caught. Accepted remediation evidence (**not normative
  policy**): `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`. New finding:
  `S0.4B-F001` (Informational — unused `@tauri-apps/plugin-shell` frontend dependency; removal
  requires separate authorization).
- **S0.4C — npm Development-Tooling Advisories: COMPLETE / ACCEPTED — 2026-08-25** (implemented
  and owner-accepted 2026-08-25). **Resolves `S0.3-F001`:** controlled `openapi-typescript`
  6.7.6 → 7.13.0 major update plus a targeted `nanoid` 3.3.16 → 3.3.18 transitive re-resolution;
  `undici` removed from the graph; `npm audit` total 0; OpenAPI generation and the full frontend
  typecheck/lint/test/build verified (281/281 tests). Accepted remediation evidence (**not
  normative policy**): `docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md`.

### S0.5 — Security Regression & Architecture Checks

**Status:** COMPLETE / ACCEPTED — 2026-08-26 (implemented 2026-08-25; owner-accepted
2026-08-26; starting commit `f784c835a507b9fd4a67c95c46ffef9ab308304c`)

S0.5 added durable, repository-native regression protection for the accepted security
properties identified in the accepted S0.3 evidence: loopback-only backend binding
(`LoopbackBindingTests` — behavioral OS-level bind check), Tauri CSP `connect-src` loopback
restriction (`csp_guard` tests in `src/tauri/src/lib.rs`), the accepted frontend CORS origin
set plus `AllowAnyOrigin`/credentials absence (extended `CorsPolicyTests`), and
application-emitted read-only QAD SQL (`QadReadOnlySqlTests`). S0.3-G004 (Tauri
least-privilege) was confirmed already covered by the accepted S0.4B `capability_guard` tests;
no new S0.4B-scope code was added. No production behavior change, no new dependency, no tool
installation, no runtime/infrastructure verification (routed to S0.7), and no S0.6 tool
admission. Informational findings: `S0.5-F001` (operator `ASPNETCORE_URLS` override is
outside repository regression protection — carried to S0.7), `S0.5-F002` (read-only SQL check
is lexical/structural; server-side grants remain S0.7). Final accepted gap dispositions:
S0.3-G002 — Covered by S0.5 repository regression protection; S0.3-G003 — Covered by S0.5
repository regression protection; S0.3-G004 — Covered by accepted S0.4B regression
protection; S0.3-G005 — Covered / Resolved (application-emitted QAD SQL is
repository-regression protected as read-only by S0.5 `QadReadOnlySqlTests`, and effective
production QAD permissions independently enforce read-only per S0.7B/G010; the application-level
guard remains lexical/structural per S0.5-F002); CORS secondary observation — Covered by S0.5
regression protection. Evidence
(**not normative policy**):
docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md.

### Remaining S0 Work — Approved Roadmap (S0.4–S0.8)

**Status:** Approved Planning Baseline — 2026-08-24 (active planning / Tier 4; not normative
policy)

The project owner approved the following structure for the remaining S0 work, derived from the
accepted S0.2/S0.3 evidence:

| Checkpoint | Name | Status |
|---|---|---|
| S0.4 | Security Finding Disposition & Bounded Remediation | **COMPLETE / ACCEPTED — 2026-08-25** (S0.4A COMPLETE / ACCEPTED; S0.4B COMPLETE / ACCEPTED; S0.4C COMPLETE / ACCEPTED) |
| S0.5 | Security Regression & Architecture Checks | **COMPLETE / ACCEPTED — 2026-08-26** (implemented 2026-08-25 — see docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md) |
| S0.6 | Security Tool Admission | **COMPLETE / ACCEPTED — 2026-08-27** — Capability Review 1 (Rust dependency advisory, `S0.3-G001`) **COMPLETE / ACCEPTED — 2026-08-26** — cargo-audit 0.22.2 ADMITTED / ACCEPTED; cargo-deny 0.20.2 DEFERRED — see docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md (S0.3-G001 — Covered / Resolved); Capability Review 2 (dedicated secret scanning, `S0.3-G007`) **COMPLETE / ACCEPTED — 2026-08-27** (Gitleaks v8.30.0 ADMITTED / IMPLEMENTED / ACCEPTED) — see docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md and docs/security/S0_6_SECRET_SCANNING_ADMISSION.md; Gitleaks v8.30.1/TruffleHog v3.97.1/detect-secrets v1.5.0 DEFERRED; `S0.3-G007` — Covered / Resolved; Capability Review 3 (SBOM, `S0.3-G008`) **COMPLETE / ACCEPTED — 2026-08-27** — Anchore Syft v1.51.1 ADMITTED / IMPLEMENTED / ACCEPTED — see docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md and docs/security/S0_6_SBOM_ADMISSION.md; Microsoft sbom-tool v4.1.5 and the CycloneDX ecosystem-native approach DEFERRED; six informational findings `S0.6-F014`–`S0.6-F019`, none blocking; complete Tauri Windows installer/application bundle Unable to Verify / future packaged-release verification boundary, not Accepted Risk; `S0.3-G008` — Covered / Resolved; Capability Review 4 (dedicated SAST, `S0.3-G006`) **COMPLETE / ACCEPTED — 2026-08-27** — neutral research packet comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90 — see docs/security/S0_6_SAST_ADMISSION_RESEARCH.md (not a recommendation or admission decision) and docs/security/S0_6_SAST_ADMISSION.md (owner decision, full implementation evidence, and 2026-08-27 project-owner acceptance); Microsoft DevSkim CLI v1.0.90 ADMITTED / INSTALLED / VERIFIED / ACCEPTED (50 findings across 3 bundled rules from the KST source-tree scan; `S0.6-F020` — Informational / Framework-Local Origin / Confirmed DevSkim False Positive for plaintext-network interpretation; `S0.6-F021` — Informational / Known DevSkim Rule Limitation; neither Accepted Risk); Semgrep CE v1.175.0 and CodeQL CLI v2.26.4 DEFERRED (not rejected); `S0.3-G006` — Covered / Resolved. All four S0.6-assigned gaps (`S0.3-G001`, `S0.3-G006`, `S0.3-G007`, `S0.3-G008`) are Covered / Resolved. |
| S0.7 | Runtime & Infrastructure Verification | **COMPLETE / ACCEPTED — 2026-08-28** — S0.7A — Local Release Runtime Verification (working pass) — **COMPLETE / ACCEPTED — 2026-08-28** (2026-08-27 evidence pass VALID / ACCEPTED AS EVIDENCE by owner review: release-built runtime evidence — loopback-only `127.0.0.1` sidecar listener on OS-assigned port, no wildcard/LAN listener, clean sidecar lifecycle with no orphans, runtime CORS matching the accepted five-origin allowlist, release-build CSP/capability artifact evidence; safe `ASPNETCORE_URLS` loopback precedence test confirmed the operator environment override alters the effective listener. 2026-08-28: `S0.5-F001` loopback-binding remediation implemented — the sidecar now unconditionally sets an explicit `http://127.0.0.1:<port>` `UseUrls` endpoint (verified on the shipped self-contained .NET 10 release runtime to ignore an inherited `ASPNETCORE_URLS` value after the fix), so inherited hosting configuration no longer takes authority over the listener — with failure-safe behavioral regression tests (no test can create a wildcard listener even in its failing state; original wildcard real-process test replaced before acceptance — see evidence §26.3; incl. demonstrated pre-fix failure), 672/672 backend suite, and post-fix release-runtime re-verification (env value no longer controls listener selection) — **`S0.5-F001` — REMEDIATED AND VERIFIED BY S0.7**; **S0.3-G009 — Covered / Resolved** (accepted with S0.7A — 2026-08-28); **`S0.7-F001`** — Operational / Package-Identity Coexistence Issue — Deferred for packaging/deployment decision; S0.7B — database/infrastructure permission verification incl. `S0.3-G010` — **COMPLETE / ACCEPTED — 2026-08-28** (`S0.3-G010` — **Covered / Resolved — 2026-08-28**; **`S0.7-F002`** — **RETIRED** / Application-vs-Enterprise Identity Scope Model Corrected — 2026-08-28 owner scope decision; NOT Accepted Risk; NOT a waived vulnerability; NOT evidence deletion) — see docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md + docs/security/S0_7_DATABASE_INFRASTRUCTURE_PERMISSION_VERIFICATION.md |
| S0.8 | Independent Assurance & S0 Closeout | **COMPLETE / ACCEPTED — 2026-08-31** (project-owner acceptance recorded; S0 — **COMPLETE / ACCEPTED — 2026-08-31** — see docs/security/S0_8_INDEPENDENT_ASSURANCE_CLOSEOUT.md and docs/security/KST_V2_SECURITY_IMPLEMENTATION_REPORT.md) |

**Cross-cutting governance foundation:** Third-Party Software & Licensing Governance is **ENACTED /
ACCEPTED — 2026-08-27** — see docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md
(integrated into docs/security/DEPENDENCY_ADMISSION.md and AGENTS.md); a governing prerequisite for
future third-party dependency/tool admission, not itself an S0 checkpoint and not an admission
decision for any specific component.

Full scope, boundaries, and the finding/gap-to-checkpoint mapping are in
docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md (approved active planning; not
normative policy). The roadmap is historical planning evidence; S0.4–S0.8 are now complete and
accepted. Stage 9 — Immediate Shortages is **UNBLOCKED / NOT STARTED**.

## Stage 8 — Component and BOM Detail

**Status:** COMPLETE / ACCEPTED — 2026-08-21

Stage 8 is an **informational Component/BOM investigation capability**. It does not implement
material-requirement netting, shortage classification, or PO coverage.

Accepted behavior includes the current effective multi-level BOM (`ps_mstr`) with structural
hierarchy/order and repeated occurrences preserved, phantoms shown and exploded, BOM search plus
P/M and Phantom filters, the shared Stage 6 Net / Non-Net inventory semantics, a blocking Component
Information modal with selected-site planning fields, Standard Cost (`sct_sim = 'Standard'`, latest
`sct_cst_date`), QCTC (`inp_source = 'qtbom_det'`, latest `inp_start_date`), and Approved Alternates
as the user-facing term (technical `ApprovedVendor` / `vp_mstr` naming is retained).

The following are **intentionally deferred future capability, not unfinished Stage 8 acceptance
requirements**: Show MRP, Inventory / Lot Locations, Extended Requirement, Incoming Supply,
Coverage / Material Status, component MRP / component supply netting, and Future Shortages / PO
coverage.

Full delivered capability, source decisions, and verification evidence (backend 656/656 tests,
frontend 260/260 tests, architecture tests, live QAD validation, owner acceptance) are recorded in
`docs/implementation/KST_v2_STAGE_8_CLOSEOUT.md` — see that document rather than this summary for
detailed acceptance evidence.

## UI Navigation & Keyboard Ergonomics A

**Status:** COMPLETE / ACCEPTED — owner manual validation PASS, 2026-08-21

The accepted interaction convention is **hierarchical Escape unwind** — not browser-history
navigation and not modal-only dismissal:

1. Topmost blocking modal/dialog → close/cancel only that surface.
2. Nested detail in the current investigation → collapse exactly one level.
3. Main MPS detail/drill-down → return to the Part Matrix.
4. Part Matrix/root → do nothing.

Examples: Component Information → BOM; BOM → Part Matrix; nested material/candidate detail → prior
material level; Show Material Lines → Work Order view; Work Order view → Part Matrix; Part Info →
Part Matrix.

Ergonomics B (arrow-key tab navigation, shared focus-trap extraction, menu roving focus, and
additional shortcut hints) remains deferred/planning-only. Full detail, investigation, and the
accepted outcome are recorded in
`docs/implementation/KST_v2_UI_NAVIGATION_KEYBOARD_ERGONOMICS_PLAN.md`.

## Stage 7 Accepted Behavior

- Selecting an MPS parent focuses the grid and opens Part Info; Work Orders/Shortages/Future Shortages/Components tabs remain disabled until a bucket is selected.
- Selecting an eligible MPS week cell or Falldown automatically opens Work Orders. Top-level drill-down is limited to Falldown + the first 6 forward MPS weeks.
- Only `Allocating`/`Frozen`/`Released` work orders receive a Stage 7 card; `RMABOM` work orders are excluded from candidate results.
- WOID (`wo_mstr.wo_lot`) is the scheduler-facing identity; Work Order Number is not unique and is never shown.
- Kitting % is line-based (fully-issued line count / applicable line count), never quantity-weighted; zero applicable lines yields null/N-A, never 0%.
- Manufactured-subassembly candidates use a truthful "Work Orders for `<Component Part>`" navigation model — QAD has no reliable parent↔subassembly WO relationship, and none is fabricated. Candidates are all eligible A/F/R work orders for the component (the original due-date-boundary rule was live-validated as correct, then removed by project-owner decision — see `docs/implementation/STAGE_7_REAL_DATA_VALIDATION.md`).
- Maximum drill depth is 3 levels; lazy-load/cache is keyed by workspace + MPS snapshot generation and is invalidated outright (not stale-fallback) on a new successful MPS refresh.

## Stage 7 Verification

- Backend: **468/468 tests passing**, `dotnet format`/build clean.
- Frontend: **167/167 tests passing**, lint/typecheck/build clean.
- Rust/Tauri `cargo check` clean; sidecar rebuilt after every backend change.
- Live QAD validation (7D.11) across the MSA/Neutronics workspace and component `H06-01-6001-33-1`, matching direct ground-truth SQL exactly.
- Full regression verification (7D.12): all of the above re-confirmed after the due-date-boundary removal and horizontal card-layout change, plus live manual desktop regression (workspace load, MPS load, Part Info, Due/Release toggle, horizon change, Falldown, Work Orders drill-down, single-instance enforcement, clean shutdown) verified directly against the running Tauri app with real QAD data.

## Stage 7 Durable Artifacts

- `docs/implementation/KST_v2_STAGE_7_WORK_ORDER_KITTING_DATA_INVENTORY.md`
- `docs/implementation/KST_v2_STAGE_7_WORK_ORDER_KITTING_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_7_IMPLEMENTATION_PLAN.md`
- `docs/implementation/STAGE_7_REAL_DATA_VALIDATION.md`
- `docs/reference/KST v2 — Stage 7D Work Orders and Kitting Implementation Checklist.md` (the authoritative checkpoint-by-checkpoint record)
- `KST-v2-Master-Project-Checklist.md`
- `docs/status/CURRENT_PROJECT_STATUS.md`

## Stage 6 Accepted Behavior

- Selecting an MPS parent focuses the grid to that parent and opens Part Info directly beneath it.
- Clicking the selected parent again or `Back to full grid` restores the full MPS grid.
- Focused mode renders only the selected row and no longer retains excessive blank full-grid height.
- Part Info is part-scoped, not week-scoped.
- Due/Release, horizon, fiscal, density, and presentation changes do not reload PartDetail.
- PartDetail is lazy-loaded and cached by workspace/parent/current MPS snapshot identity.
- Stale-last-good behavior is preserved across relevant refresh/query failure scenarios.
- QAD Part Status is shown as code + backend-owned description.
- Qty On Hand and Qty Non-Net use positive-only, non-RMA nettable/non-nettable inventory rules.
- Pricing uses the most recent `pi_start <= today` and supports one or more MOQ/price tiers.
- Blank/null informational QAD values are acceptable and do not create false error states.

## Stage 6 Verification

- Backend: **316/316 tests passing**.
- Frontend: **119/119 tests passing** after final UI refinements.
- Backend format/build clean.
- Frontend lint/typecheck/build clean.
- Rust/Tauri `cargo check` clean.
- Sidecar rebuilt after backend changes.
- Live read-only QAD validation completed across five available development workspaces and 71 representative parent parts.
- Direct read-only SQL comparisons matched representative PartDetail inventory and pricing results.
- Final Tauri owner-review verified focused-grid spacing, selection-toggle close behavior, Back-to-full-grid behavior, keyboard interaction, and clean shutdown without orphan processes.
- Rare live scenarios not naturally present (multi-tier price breaks, RMA exclusion, no-current-price, additional site/domain) remain covered by deterministic automated tests.

## Stage 6 Durable Artifacts

- `docs/implementation/KST_v2_STAGE_6_PART_INFO_CONTRACT.md`
- `docs/implementation/KST_v2_STAGE_6D_IMPLEMENTATION_PROGRESS.md`
- `docs/implementation/KST_v2_STAGE_6_CLOSEOUT.md`
- `KST-v2-Master-Project-Checklist.md`
- `docs/status/CURRENT_PROJECT_STATUS.md`
- updated backend-boundary and API-contract workflow documentation

## Administrative Follow-Up

Stage 6 final commit hash: `863a638` (`feat: complete Stage 6 part information drill-down`).

## Versioning Foundation

Between Stage 6 and Stage 7, a lightweight application versioning foundation was
established (inter-stage housekeeping, not a Stage 7 activity):

- Product identity `KST v2` remains distinct from the semantic application version.
- Current application version: **`0.1.0-alpha.2`** ([SemVer 2.0.0](https://semver.org/)) — bumped from `0.1.0-alpha.1` at Stage 7 closeout (session housekeeping, not a Stage 7 behavior change).
- Authoritative source: `src/backend/Directory.Build.props` (`VersionPrefix`/`VersionSuffix`),
  propagated to the backend (assembly `InformationalVersion`, system status/health endpoints,
  frontend top bar), `src/tauri/Cargo.toml`, `src/frontend/package.json` (full version), and
  `src/tauri/tauri.conf.json` (numeric-only, for MSI/WiX installer compatibility - see
  `docs/development/VERSIONING.md`).
- New tests: 2 backend integration tests (`SystemStatusEndpointTests`), 3
  `Kst.ArchitectureTests` (`VersionConsistencyTests`) guarding future drift. Full backend suite:
  329/329 passing.
- New repeatable sync/check script: `scripts/check-version.ps1` (`-Fix` to auto-correct drift).
- Full documentation: `docs/development/VERSIONING.md`.
- Packaged Windows build (NSIS + MSI) verified successfully with the new version metadata.
- This work does not begin, renumber, or otherwise affect Stage 7.

## R0 — Repository / Documentation Reconciliation Status

- R0.1 — Read-only repository documentation inventory: **COMPLETE / ACCEPTED**
- R0.2 — Authority and contradiction map: **COMPLETE / ACCEPTED**
- R0.3 — Core project-state reconciliation: **COMPLETE / ACCEPTED**
- R0.4 — Stage-history reconciliation: **COMPLETE / ACCEPTED**
- R0.5 — Architecture and development documentation reconciliation: **COMPLETE / ACCEPTED**
- R0.6 — Data/source documentation reconciliation: **COMPLETE / ACCEPTED**
- R0.7 — Documentation navigation and authority: **COMPLETE / ACCEPTED**
- R0.8 — Reconciliation verification / closeout: **COMPLETE / ACCEPTED**

R0 overall: **COMPLETE / ACCEPTED — 2026-08-21.** Full detail:
`KST-v2-Master-Project-Checklist.md` (`## R0 — Repository / Documentation Reconciliation`) and
`docs/status/R0_REPOSITORY_RECONCILIATION_CLOSEOUT.md`.

## Next Action

Stage 8, UI Navigation & Keyboard Ergonomics A, and R0 — Repository / Documentation
Reconciliation are complete and accepted. The current effort is **S0 — Security Foundation
Integration**: S0.1–S0.3 are complete and owner-accepted, and S0.4 — Security Finding
Disposition & Bounded Remediation is **COMPLETE / ACCEPTED — 2026-08-25**: S0.4A — QAD SQL
Transport Correction is **COMPLETE / ACCEPTED — 2026-08-25** (resolves `S0.2-F003` at the
application-configuration level — see
`docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`); S0.4B — Tauri Shell Capability is
**COMPLETE / ACCEPTED — 2026-08-25** (resolves `S0.2-F001`; accepted remediation evidence:
`docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`); S0.4C — npm
Development-Tooling Advisories is **COMPLETE / ACCEPTED — 2026-08-25** (resolves `S0.3-F001`;
accepted remediation evidence:
`docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md`). S0.5 — Security Regression &
Architecture Checks is **COMPLETE / ACCEPTED — 2026-08-26**; S0.6 — Security Tool Admission is
**COMPLETE / ACCEPTED — 2026-08-27** (Capability Review 1 — Rust dependency advisory, `S0.3-G001` —
**COMPLETE / ACCEPTED — 2026-08-26**: cargo-audit 0.22.2 ADMITTED / ACCEPTED — see
`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`; cargo-deny 0.20.2 DEFERRED; Capability
Review 2 — dedicated secret scanning, `S0.3-G007` — **COMPLETE / ACCEPTED — 2026-08-27**
(Gitleaks v8.30.0 installed, release-integrity and synthetic-canary verified,
scanned current KST content (4 findings) and full Git history (8 findings), all rule
`private-key`, confirmed documentation false positives; `S0.3-G007` — Covered / Resolved) — see
`docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` and
`docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`; Gitleaks v8.30.1, TruffleHog v3.97.1,
detect-secrets v1.5.0 DEFERRED; Capability Review 3 — Software Bill of Materials, `S0.3-G008` —
**COMPLETE / ACCEPTED — 2026-08-27** (Anchore Syft v1.51.1 installed,
release-integrity verified, run against build/repository evidence and a complementary
packaged-artifact view; six informational findings `S0.6-F014`–`S0.6-F019` recorded, none
blocking; complete Tauri Windows installer/application bundle Unable to Verify / future
packaged-release verification boundary, not Accepted Risk) — see
`docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md` and
`docs/security/S0_6_SBOM_ADMISSION.md`; Anchore Syft v1.51.1 — ADMITTED / IMPLEMENTED /
ACCEPTED; `S0.3-G008` — Covered / Resolved; Capability Review 4 — Dedicated Static Application
Security Testing (SAST), `S0.3-G006` — **COMPLETE / ACCEPTED — 2026-08-27** (owner
reviewed the neutral research
packet comparing Semgrep CE v1.175.0, CodeQL CLI v2.26.4, and Microsoft DevSkim CLI v1.0.90 and
admitted DevSkim CLI v1.0.90, which was installed, self-verified, synthetically
validated, and run against the KST source tree — 50 findings across 3 bundled rules; `S0.6-F020`
reviewed 2026-08-27 and reclassified to Informational / Framework-Local Origin / Confirmed
DevSkim False Positive for plaintext-network interpretation; `S0.6-F021` — Informational / Known
DevSkim Rule Limitation; neither Accepted Risk; Semgrep CE v1.175.0 and CodeQL CLI
v2.26.4 DEFERRED, not rejected) —
see `docs/security/S0_6_SAST_ADMISSION_RESEARCH.md` and `docs/security/S0_6_SAST_ADMISSION.md`;
`S0.3-G006` — Covered / Resolved; Microsoft DevSkim CLI v1.0.90 — ADMITTED / INSTALLED / VERIFIED /
ACCEPTED). All four S0.6-assigned gaps (`S0.3-G001`, `S0.3-G006`, `S0.3-G007`, `S0.3-G008`) are
Covered / Resolved. S0.7 — Runtime & Infrastructure Verification is **COMPLETE / ACCEPTED — 2026-08-28**: the S0.7A —
Local Release Runtime Verification working pass is **COMPLETE / ACCEPTED — 2026-08-28**
(2026-08-27 release-built runtime evidence — VALID / ACCEPTED AS EVIDENCE by owner
review: loopback-only sidecar listener, clean sidecar lifecycle, runtime CORS verified against
the accepted five-origin allowlist, release-build CSP/capability artifact evidence; the
`ASPNETCORE_URLS` loopback precedence test confirmed the operator environment override alters
the effective listener. On 2026-08-28 the `S0.5-F001` weakness was remediated — the sidecar now
unconditionally sets an explicit `http://127.0.0.1:<port>` `UseUrls` endpoint (verified on the
shipped self-contained .NET 10 release runtime to ignore an inherited `ASPNETCORE_URLS` value
after the fix, so inherited hosting configuration no longer takes authority over the
listener), with failure-safe behavioral regression tests (no test can create a wildcard
listener even in its failing state; the original wildcard real-process test was replaced
before acceptance — see evidence §26.3 — including a demonstrated pre-fix failure), a 672/672
backend suite, and post-fix release-runtime re-verification showing the environment value no
longer controls listener selection — **`S0.5-F001` — Confirmed Runtime Configuration Weakness /
REMEDIATED AND VERIFIED BY S0.7**; **S0.3-G009 — Covered / Resolved** on the post-remediation
evidence (accepted with S0.7A — 2026-08-28); **`S0.7-F001`** — Operational / Package-Identity
Coexistence Issue (KST v1 ↔ KST v2 single-instance interception) — Deferred for a
packaging/deployment decision, non-blocking; S0.7B — database/infrastructure permission
verification incl. `S0.3-G010` — **COMPLETE / ACCEPTED — 2026-08-28**
(`S0.3-G010` — **Covered / Resolved — 2026-08-28**; **`S0.7-F002`** — **RETIRED** /
Application-vs-Enterprise Identity Scope Model Corrected — 2026-08-28 owner scope decision; NOT
Accepted Risk; NOT a waived vulnerability; NOT evidence deletion) — see
docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md. S0.8 — Independent Assurance & S0
Closeout is **COMPLETE / ACCEPTED — 2026-08-31**; S0 — **COMPLETE / ACCEPTED — 2026-08-31** — see
docs/security/S0_8_INDEPENDENT_ASSURANCE_CLOSEOUT.md and
docs/security/KST_V2_SECURITY_IMPLEMENTATION_REPORT.md. The cross-cutting Third-Party
Software & Licensing Governance foundation is **ENACTED / ACCEPTED — 2026-08-27** — see
`docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md`, integrated into
`docs/security/DEPENDENCY_ADMISSION.md` and `AGENTS.md`; it is a governing prerequisite for future
third-party admission and is
not itself an S0 checkpoint or an admission decision for any component. Stage 9 — Immediate Shortages
is **UNBLOCKED / NOT STARTED**.

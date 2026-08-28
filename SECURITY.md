# Security

**Status:** Enacted / Accepted — 2026-08-21

This file is the repository's security entry point. It is intentionally short — detailed rules live
in `docs/security/`.

## What security rules apply?

KST v2's security requirements apply consistently regardless of which development environment or
AI coding agent is used. The authoritative platform-neutral rules are:

- [docs/security/SECURITY_ASSURANCE_POLICY.md](docs/security/SECURITY_ASSURANCE_POLICY.md) —
  primary normative cross-cutting security policy.
- [docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md](docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md) —
  security expectations for coding environments/agents, by capability.
- [docs/security/DEPENDENCY_ADMISSION.md](docs/security/DEPENDENCY_ADMISSION.md) —
  admission rules for third-party dependencies and development tooling.
- [docs/security/AI_SECURITY_REVIEW.md](docs/security/AI_SECURITY_REVIEW.md) —
  independent AI security review model.
- [docs/security/APPLICATION_SECURITY_PROFILE.md](docs/security/APPLICATION_SECURITY_PROFILE.md) —
  KST-specific declared security properties.

Third-party software admission also has a licensing/commercial-use dimension distinct from
security: see
[docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md](docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md)
(Enacted / Accepted — 2026-08-27), which `DEPENDENCY_ADMISSION.md` incorporates as an admission
gate. Licensing/commercial governance is **not** itself a vulnerability class or a security-severity
finding — a component can be technically secure yet unacceptable under its license or commercial
terms, and vice versa; both dimensions must be satisfied independently.

`docs/security/SECURITY_BASELINE.md` is the **observational** S0.2 security baseline (accepted —
see "Current security-policy status" below). It records what was observed in the repository and
development environment; it is not itself normative policy. Required properties remain defined by
the documents above, especially `APPLICATION_SECURITY_PROFILE.md`.

## What must a developer/agent do before security-relevant work?

Before implementing a security-relevant change, retrieve and follow the applicable policy above.
See `AGENTS.md` for the concise mandatory agent behavior summary and see
`SECURITY_ASSURANCE_POLICY.md` §"Security-Relevant Change Triggers" for the categories of change
that require this.

At minimum, do not silently:

- introduce a new third-party dependency or executable development tool;
- install or activate new agent extensions/packages/plugins/skills/MCP servers;
- weaken an established security boundary (loopback networking, read-only database access, CORS,
  credential handling, etc.);
- expose credentials or production data;
- suppress a material security finding.

## How are material security findings handled?

- Findings must be surfaced, not silently absorbed or worked around.
- Evidence should be recorded where practical (see `SECURITY_ASSURANCE_POLICY.md` §"Security
  Findings Require Evidence").
- AI agents cannot accept material security risk on the project's behalf.
- Unresolved material risk requires escalation to the project owner; final organizational
  risk-acceptance authority remains to be established with IT/security (see
  `SECURITY_ASSURANCE_POLICY.md` §"Intentionally Unresolved Policy Areas").

## Current security-policy status

- **S0.1 — Security Policy Injection:** COMPLETE / ACCEPTED — 2026-08-21. The documents linked
  above are the enacted, owner-accepted security policy.
- **S0.2 — Baseline Discovery:** COMPLETE / ACCEPTED — 2026-08-24. See
  [docs/security/SECURITY_BASELINE.md](docs/security/SECURITY_BASELINE.md) (accepted observational
  baseline; still not normative policy — required properties remain defined by the policy documents
  above).
- **S0.3 — Existing-Tool Security Checks:** COMPLETE / ACCEPTED — 2026-08-24. See
  [docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md](docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md)
  (accepted S0.3 verification/check evidence; evidence, not normative policy).
- **Remaining S0 work (S0.4–S0.8):** Approved Planning Baseline — 2026-08-24. See
  [docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md](docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md)
  (approved active planning; **not normative policy**). S0.4 — Security Finding Disposition &
  Bounded Remediation is **COMPLETE / ACCEPTED — 2026-08-25**: S0.4A — QAD SQL Transport Correction is **COMPLETE /
  ACCEPTED — 2026-08-25** (resolves `S0.2-F003` at the KST application-configuration level) — see
  [docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md](docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md)
  (**accepted remediation evidence**, not normative policy). S0.4B — Tauri Shell Capability
  Remediation is **COMPLETE / ACCEPTED — 2026-08-25** (resolves `S0.2-F001`) — see
  [docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md](docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md)
  (S0.4B remediation evidence, not normative policy). S0.4C — npm Development-Tooling Advisories
  is **COMPLETE / ACCEPTED — 2026-08-25** (resolves `S0.3-F001`) — see
  [docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md](docs/security/S0_4C_NPM_DEV_DEPENDENCY_REMEDIATION.md)
  (accepted S0.4C remediation evidence, not normative policy). S0.5 — Security Regression &
  Architecture Checks is **COMPLETE / ACCEPTED — 2026-08-26** — see
  [docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md](docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md)
  (accepted S0.5 evidence, not normative policy). S0.6 — Security Tool Admission is **COMPLETE /
  ACCEPTED — 2026-08-27**: Capability Review 1 — Rust Dependency Advisory Capability (gap `S0.3-G001`) —
  **COMPLETE / ACCEPTED — 2026-08-26**: **cargo-audit 0.22.2 — ADMITTED / ACCEPTED**;
  **cargo-deny 0.20.2 — DEFERRED**; **S0.3-G001 — Covered / Resolved** — see
  [docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md](docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md)
  (accepted admission + implementation evidence, **not** normative policy). Capability Review 2 —
  Dedicated Secret Scanning (gap `S0.3-G007`) is **COMPLETE / ACCEPTED — 2026-08-27**: Gitleaks
  v8.30.0 was installed, release-integrity verified, synthetic-canary verified, and run against
  current KST content (4 findings) and full Git history (8 findings), all rule `private-key`;
  all scanner matches were literal PEM-header sentinel strings intentionally present as
  documentation prose, and review found no private-key body or credential material — research at
  [docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md](docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md)
  (neutral, evidence-backed research packet; **not** a tool recommendation or admission decision),
  owner decision and implementation evidence at
  [docs/security/S0_6_SECRET_SCANNING_ADMISSION.md](docs/security/S0_6_SECRET_SCANNING_ADMISSION.md).
  **Gitleaks v8.30.0 — ADMITTED / IMPLEMENTED / ACCEPTED**; **S0.3-G007 — Covered / Resolved**;
  **S0.6-F002 through S0.6-F013 — Informational / Confirmed Documentation False Positives** (no
  suppression, no baseline, no Accepted Risk, no severity assignment).
  Gitleaks v8.30.1, TruffleHog v3.97.1, and detect-secrets v1.5.0 are **DEFERRED** (not rejected).
  Capability Review 3 — Software Bill of Materials (gap `S0.3-G008`) is **COMPLETE / ACCEPTED —
  2026-08-27**: Anchore Syft v1.51.1 was installed (a pre-existing binary was independently
  verified byte-identical to a freshly verified official release rather than trusted),
  release-integrity verified, and run against KST build/repository evidence (`dir:src`; SPDX 2.3
  JSON, 1,027 packages; CycloneDX 1.6 JSON via the explicit `@1.6` selector, 1,026 components) and
  a complementary packaged-artifact view (the published self-contained single-file `Kst.Api`
  sidecar; 37 NuGet packages recovered directly from the executable) — research at
  [docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md](docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md)
  (neutral, evidence-backed research packet; **not** a tool recommendation or admission decision),
  owner decision and implementation evidence at
  [docs/security/S0_6_SBOM_ADMISSION.md](docs/security/S0_6_SBOM_ADMISSION.md). **Anchore Syft
  v1.51.1 — ADMITTED / IMPLEMENTED / ACCEPTED**; **Microsoft sbom-tool v4.1.5** and the
  **CycloneDX ecosystem-native approach** (cyclonedx-dotnet 6.2.0, cyclonedx-npm 6.0.1,
  cargo-cyclonedx 0.5.9) remain **DEFERRED** (not rejected); **`S0.3-G008` — Covered / Resolved**;
  six informational findings `S0.6-F014` through `S0.6-F019` were recorded (cataloger/config
  default behavior, duplicate/noisy representation, first-party representation, and
  license-metadata limitations; none blocks `S0.3-G008`, none is Accepted Risk). The complete Tauri
  Windows installer/application bundle remains **Unable to Verify / future packaged-release
  verification boundary** (not Accepted Risk; does not block `G008`). Capability Review 4 —
  Dedicated Static Application Security Testing (SAST), gap `S0.3-G006`, is **COMPLETE / ACCEPTED —
  2026-08-27**: the project owner independently reviewed the neutral
  research packet at
  [docs/security/S0_6_SAST_ADMISSION_RESEARCH.md](docs/security/S0_6_SAST_ADMISSION_RESEARCH.md)
  (neutral research packet; **not** a tool recommendation or admission decision) comparing
  Semgrep CE (v1.175.0), CodeQL CLI (v2.26.4), and Microsoft DevSkim CLI (v1.0.90), and admitted
  **Microsoft DevSkim CLI v1.0.90**, which was installed, self-verified, synthetically
  validated (C#, JavaScript/TypeScript, Rust, SQL), and run against the KST source tree (50
  findings across 3 bundled rules; `S0.6-F020` reviewed 2026-08-27 and reclassified to
  Informational / Framework-Local Origin / Confirmed DevSkim False Positive for
  plaintext-network interpretation (Tauri's locked version is outside the affected range of the
  reviewed advisory GHSA-7gmj-67g7-phm9; `http://tauri.localhost` is the expected Windows Tauri
  custom-protocol WebView origin, not internet-routable HTTP; the backend remains loopback-only
  independent of CORS); `S0.6-F021` remains Informational / Known DevSkim Rule Limitation; neither
  is Accepted Risk) — see
  [docs/security/S0_6_SAST_ADMISSION.md](docs/security/S0_6_SAST_ADMISSION.md) (owner decision,
  full implementation evidence, and 2026-08-27 project-owner acceptance). **Microsoft DevSkim CLI
  v1.0.90 — ADMITTED / INSTALLED / VERIFIED / ACCEPTED**; **Semgrep CE v1.175.0 — DEFERRED** pending
  organizational licensing review; **CodeQL CLI v2.26.4 — DEFERRED** pending confirmed applicable
  private-repository entitlement and organizational authorization (neither deferred candidate is
  rejected). `S0.3-G006` disposition is now **Covered / Resolved**. With all four S0.6-assigned
  gaps now Covered / Resolved (`S0.3-G001`, `S0.3-G006`, `S0.3-G007`, `S0.3-G008`), **S0.6 —
  Security Tool Admission is COMPLETE / ACCEPTED — 2026-08-27**. **S0.7 — Runtime &
  Infrastructure Verification is IN PROGRESS**: working pass **S0.7A — Local Release Runtime
  Verification is COMPLETE / ACCEPTED — 2026-08-28** (2026-08-27 evidence pass —
  VALID / ACCEPTED AS EVIDENCE by owner review: release-built runtime evidence of loopback-only
  `127.0.0.1` sidecar listener, clean sidecar lifecycle, runtime CORS matching the accepted
  five-origin allowlist, and release-build CSP/capability artifact evidence; the safe
  `ASPNETCORE_URLS` loopback precedence test confirmed the operator environment override alters
  the effective listener — **`S0.5-F001` — Confirmed Runtime Configuration Weakness / REMEDIATED
  AND VERIFIED BY S0.7** on 2026-08-28: `Program.cs` now unconditionally sets its own explicit `127.0.0.1`
  `UseUrls` endpoint (verified on the shipped self-contained .NET 10 release runtime to ignore
  an inherited `ASPNETCORE_URLS` value after the fix, so inherited hosting configuration no
  longer takes authority over the listener), with failure-safe behavioral regression tests (no
  test can create a wildcard listener even in its failing state; the original wildcard
  real-process test was replaced before acceptance — see evidence §26.3) — including a
  demonstrated pre-fix failure — and post-fix release-runtime re-verification showing the
  environment value no longer controls listener selection; **S0.3-G009 — Covered / Resolved** on
  the post-remediation evidence (accepted with S0.7A — 2026-08-28); **`S0.7-F001`** — Operational /
  Package-Identity Coexistence Issue (KST v1 ↔ KST v2 single-instance interception) — Deferred
  for a packaging/deployment decision, non-blocking; S0.7B — database / infrastructure
  permission verification incl. `S0.3-G010` — PENDING, not started) — see
  [docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md](docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md)
  (S0.7A evidence + remediation record, **not** normative policy). S0.8 remains PLANNED / NOT
  STARTED. Stage 9 is blocked pending S0 closeout.

The original design source for this policy set is retained for provenance at
`docs/reference/security/KST v2 Security Foundation — Initial Policy and Enactment Draft.md` and is
not itself current policy.

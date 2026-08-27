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
  (accepted S0.5 evidence, not normative policy). S0.6 — Security Tool Admission is **IN
  PROGRESS**: Capability Review 1 — Rust Dependency Advisory Capability (gap `S0.3-G001`) —
  **COMPLETE / ACCEPTED — 2026-08-26**: **cargo-audit 0.22.2 — ADMITTED / ACCEPTED**;
  **cargo-deny 0.20.2 — DEFERRED**; **S0.3-G001 — Covered / Resolved** — see
  [docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md](docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md)
  (accepted admission + implementation evidence, **not** normative policy). Capability Review 2 —
  Dedicated Secret Scanning (gap `S0.3-G007`) is **OWNER DECISION RECORDED / GITLEAKS v8.30.0
  ADMITTED FOR INSTALLATION AND VERIFICATION / IMPLEMENTATION PENDING** — research at
  [docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md](docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md)
  (neutral, evidence-backed research packet; **not** a tool recommendation or admission decision),
  owner decision and implementation evidence at
  [docs/security/S0_6_SECRET_SCANNING_ADMISSION.md](docs/security/S0_6_SECRET_SCANNING_ADMISSION.md).
  Gitleaks v8.30.1, TruffleHog v3.97.1, and detect-secrets v1.5.0 are **DEFERRED** (not rejected).
  The remaining S0.6 capability reviews (G006, G008) are NOT STARTED. Stage 9 is blocked pending
  S0 closeout.

The original design source for this policy set is retained for provenance at
`docs/reference/security/KST v2 Security Foundation — Initial Policy and Enactment Draft.md` and is
not itself current policy.

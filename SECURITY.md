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

`docs/security/SECURITY_BASELINE.md` does not exist yet. It is produced by **S0.2 — Baseline
Discovery**, a later checkpoint that inventories actual dependencies, development tooling, and
attack surface. Do not treat its absence as a gap in this checkpoint.

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
- **S0.2 — Baseline Discovery:** NEXT / NOT STARTED. Will produce `docs/security/SECURITY_BASELINE.md`.
- **S0.3 — Existing-Tool Security Checks:** NOT STARTED.

The original design source for this policy set is retained for provenance at
`docs/reference/security/KST v2 Security Foundation — Initial Policy and Enactment Draft.md` and is
not itself current policy.

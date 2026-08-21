# Security Assurance Policy

**Status:** Enacted / Accepted — 2026-08-21

This is the primary platform-neutral, normative security policy for KST v2. It applies regardless
of which development environment or AI coding agent performs the work. Specialized policies
(`DEVELOPMENT_ENVIRONMENT_SECURITY.md`, `DEPENDENCY_ADMISSION.md`, `AI_SECURITY_REVIEW.md`,
`APPLICATION_SECURITY_PROFILE.md`) elaborate specific areas; this document establishes the
cross-cutting principles that they must not contradict.

## Relationship to Existing KST v2 Rules

This policy extends existing project principles; it does not replace them. It remains consistent
with `AGENTS.md`, including: the repository is durable project memory; agent-generated work should
be independently verified; compiler/tests/architecture tests/guided human verification are external
correctness mechanisms; production database access is read-only; KST does not directly write
operational changes to QAD; credentials must not be hard-coded; important operational values remain
traceable to their source; architectural boundaries remain explicit and testable.

## 1. Security Is an Architectural Requirement

Security constraints are not optional implementation guidance. Implementation difficulty does not
justify silently weakening:

- networking restrictions;
- database restrictions;
- credential protections;
- input validation;
- filesystem controls;
- process controls;
- dependency controls;
- other established security boundaries.

When an implementation conflicts with a security requirement, the conflict must be surfaced rather
than silently resolved by weakening the boundary.

## 2. Implementation Decisions vs. Trust Decisions

Routine implementation decisions (method structure, local refactoring, UI component organization,
test implementation, use of already-approved dependencies) may remain autonomous within established
boundaries.

Trust-changing actions require additional review. Examples:

- new third-party dependencies;
- new executable tooling;
- new agent extensions/plugins/packages/skills/MCP servers;
- new network exposure;
- new or broadened credential handling;
- broader filesystem access;
- new subprocess execution;
- database privilege changes;
- new external services;
- new external data transfer.

## 3. Independent Verification

An AI agent declaring its own work secure is not sufficient evidence. Security assurance should rely
on independent mechanisms appropriate to the change, such as: compiler/type system, tests,
architecture tests, dependency analysis, secret analysis, static analysis, runtime verification,
independent AI review, and targeted human review.

Not all of these mechanisms currently exist for KST. Some are introduced by later S0 checkpoints
(see `docs/security/AI_SECURITY_REVIEW.md` and the master checklist's S0.2/S0.3 items). No single
mechanism is authoritative on its own.

## 4. Security Findings Require Evidence

Material security findings should identify, where practical:

1. what is wrong;
2. where it is;
3. why it is security relevant;
4. supporting evidence;
5. a reproduction/verification method;
6. the expected security property it violates;
7. recommended remediation.

Findings that cannot yet be demonstrated must be labeled as unverified/potential, not confirmed.

## 5. Risk-Proportional Controls

Not every code change requires a full security review. Additional security review is triggered by
changes to security-sensitive areas rather than applied indiscriminately to every change.

## 6. Security-Relevant Change Triggers

Additional security attention is warranted when a change touches:

- dependency manifests or lockfiles;
- agent extensions or tooling;
- network listeners or outbound networking behavior;
- CORS or CSP;
- database connection behavior;
- credentials;
- subprocess execution;
- file import/export;
- filesystem permissions;
- shell commands;
- parsing of external/untrusted input;
- installer or deployment behavior;
- Tauri capabilities;
- new external services.

This may become partially automated through repository-diff analysis in a later S0 checkpoint; it
is not automated today.

## 7. Security Control Preservation

Agents and developers must not silently solve implementation problems by:

- weakening loopback binding to broader network binding;
- broadening CORS unnecessarily;
- disabling certificate, input, or other security checks;
- granting excessive filesystem or process access;
- weakening database restrictions;
- hard-coding credentials;
- suppressing material findings;
- disabling a security check simply to make verification pass;
- replacing an affected dependency with an obscure, unreviewed dependency merely to remove a
  warning.

When such a change is believed necessary, it is a security-design decision requiring explicit
review, not a routine implementation choice.

## 8. Production-System Restrictions (Security-Normative)

The following existing KST requirements are hereby made explicitly security-normative:

- Production database access remains read-only.
- Direct QAD/database write-back is prohibited (no `INSERT`/`UPDATE`/`DELETE`/`MERGE` or other
  database-side operational changes from the application).
- QAD and other authoritative company systems remain systems of record.
- Operational modifications leave KST through controlled, human-reviewable export/import workflows,
  not direct production database mutation.

## 9. Credentials and Secrets

Credentials must not be:

- committed to source control;
- embedded in source;
- embedded in documentation or test fixtures;
- written to normal logs;
- exposed through API responses;
- unnecessarily sent to AI systems.

Full production connection strings must not be logged.

## 10. Development Tooling Is Part of the Software Supply Chain

Development tooling — IDE extensions, AI-agent extensions, packages, skills, MCP servers, hooks,
scripts, build tools, formatters, linters, generators, and executables invoked by agents — is
executable third-party software and must be treated as part of the supply chain. Risk is determined
by capability, not by product terminology; a "skill," "extension," or "MCP server" that can execute
code or expose additional capability is in scope regardless of what the platform calls it. Detailed
expectations live in `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`.

## 11. Security Finding States

Finding state and severity are separate concepts (e.g. `Severity: High` / `State: Potential —
Investigation Required` is different from `Severity: High` / `State: Confirmed`).

Finding states:

- **Confirmed**
- **Potential / Investigation Required**
- **Resolved**
- **False Positive**
- **Accepted Risk**
- **Unable to Verify**
- **Informational**

## 12. Risk Acceptance

- AI systems do not possess authority to accept material security risk.
- Unresolved material security risk must not be silently accepted.
- Explicit security-boundary exceptions require documentation.
- For the initial KST pilot, routine Low/Informational findings may be dispositioned at project
  level; unresolved High/Critical findings must not be silently accepted.
- Final organizational risk-acceptance authority remains to be established with IT/security. This
  is not decided by this policy.

## 13. Initial Enforcement Vocabulary

Not every rule below has an automated mechanism today. Where no automated gate exists yet, the
level below describes a policy acceptance/blocking condition — not a claim that automation is
already installed.

| Control | Initial Level |
|---|---|
| New dependency disclosure | Silent agent addition prohibited (policy, not yet automated) |
| Secret committed to repository | Block |
| Production database write capability | Block |
| Known malicious dependency | Block |
| New network exposure | Human review |
| New agent extension/package | Human approval |
| SBOM | Documented initially; later release integration |

`Documented`, `Warn`, and `Block` are the three enforcement levels used across the security policy
set:

- **Documented** — the requirement exists but is not yet automated.
- **Warn** — the system detects a deviation and requires disposition.
- **Block** — the condition prevents acceptance/release unless formally overridden.

## 14. Intentionally Unresolved Policy Areas

The following are intentionally not decided by S0.1. They are not gaps to be silently filled with
reasonable-sounding assumptions — they require dedicated future work and, in several cases,
IT/security consultation:

- final security severity thresholds;
- final organizational risk-acceptance authority;
- approved external AI providers;
- exact SBOM format;
- exact vulnerability scanner;
- exact SAST platform;
- CI/CD implementation;
- centralized portfolio inventory;
- final development-environment risk tiers;
- isolation technology for adversarial/malicious-sample work;
- mandatory frontier-model review triggers;
- organization-wide adoption rules.

## Immediate Mandatory Requirements

1. No new third-party dependency may be silently introduced.
2. No new agent extension/package/plugin/skill/MCP server may be silently introduced.
3. Established loopback networking must not be broadened without explicit review.
4. Production databases remain read-only.
5. Direct QAD/database write-back remains prohibited.
6. Credentials and production secrets must not be committed, logged, or supplied unnecessarily to
   AI systems.
7. Existing security checks must not be disabled merely to make implementation pass.
8. Security-relevant architectural changes must be identified as such.
9. Material security findings require evidence.
10. AI-generated security findings are not automatically confirmed.
11. AI agents cannot accept material security risk.
12. External AI use must respect company data-handling requirements (see
    `docs/security/AI_SECURITY_REVIEW.md` and `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`).

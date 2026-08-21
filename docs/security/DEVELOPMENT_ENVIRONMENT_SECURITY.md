# Development Environment Security

**Status:** Enacted / Accepted — 2026-08-21

This policy defines security expectations for coding environments and AI development agents,
regardless of vendor. KST v2 does not require one approved coding environment. Instead, each
environment is evaluated by capability and risk. See
`docs/security/SECURITY_ASSURANCE_POLICY.md` for the cross-cutting principles this document
elaborates.

## Capability Areas

### Execution

What commands or programs can the environment execute (shell, PowerShell, Python, compilers,
package managers, arbitrary executables, build scripts)? This determines the environment's blast
radius independent of what the agent intends to do.

### Filesystem Access

What can the environment read or write (repository, user profile, shared drives, credential stores,
other local files)?

### Credential Access

What credentials, environment variables, or credential stores may flow into processes the
environment spawns?

### Network Access

What network destinations can the environment reach?

### External Data Transfer

What repository or company data may leave the workstation or execution environment (e.g. sent to an
external AI service)?

### Extension Surface

What third-party executable components extend the environment (IDE extensions, agent
extensions/packages/skills, MCP servers, hooks, plugins)? These are development dependencies — see
`docs/security/DEPENDENCY_ADMISSION.md`.

## Installation and Activation

AI agents must not autonomously install or activate new development-environment extensions,
packages, plugins, skills, MCP servers, binaries, or equivalent executable components. New
development tooling requires explicit human awareness and appropriate review before use.

Existing installed tools are not automatically approved for every use merely because they are
already installed, but this policy does not invent a centralized allowlist mechanism — one does not
currently exist.

The existing project convention already treats activation of installed Pi packages/modes as
human-controlled (see `docs/development/KST v2 Project Instructions — Local Agent Addendum.md`).
This policy generalizes that principle to all development environments and AI agent platforms.

## Local AI Is Not Automatically Safe

Local execution does not automatically mean safe execution. A local model avoids some data-transfer
concerns but can still execute commands, access credentials or files, and reach networks. Local
execution and safe execution are separate questions and must be evaluated independently.

## Adversarial and Risky Execution

Routine supervised coding does not automatically require isolation. Work that intentionally
executes suspicious, untrusted, or adversarial software — e.g. installing suspected-malicious
packages, executing exploit samples, dynamic malware analysis, testing untrusted binaries, or
intentionally exercising known remote-code-execution paths — should use appropriate isolation when
that work is eventually authorized. The required level of isolation should be proportional to the
activity.

A specific sandbox or VM technology is not selected by this policy. That is a later, explicitly
unresolved decision (see `docs/security/SECURITY_ASSURANCE_POLICY.md` §"Intentionally Unresolved
Policy Areas").

## Future Baseline Inventory

**S0.2 — Baseline Discovery** will inventory the actual development environments and tooling in
current use, including agent platform/version, installed extensions/packages/skills, configured MCP
servers, project/global instruction files, build tools, and package managers, and will produce
`docs/security/SECURITY_BASELINE.md`. That inventory is not performed by this document or by S0.1.

# KST v2 Security Foundation — Initial Policy and Enactment Draft

**Status:** Initial working draft  
**Purpose:** Define the first cross-cutting security policies for KST v2 and the implementation steps required to make them durable repository controls.

> **Design Source — Not Normative Policy**
>
> This document is the initial Security Foundation working draft retained for design history and
> provenance.
>
> Current enacted KST v2 security policy is defined by `SECURITY.md` and the accepted policy
> documents under `docs/security/`.

---

# 1. Purpose

KST v2 increasingly relies on AI-assisted software development.

This changes the practical software-assurance problem.

> **Software can now be produced at machine speed, while much of software security review still assumes human-speed development.**

KST v2 will not respond to this by restricting AI-assisted development or requiring developers to use one specific coding environment.

Instead, the project will establish security requirements that apply consistently regardless of whether implementation is performed using:

- Pi;
- GitHub Copilot;
- a frontier coding model;
- a local coding model;
- another AI development environment;
- conventional human development.

The framework governs **security properties, trust decisions, and evidence requirements**, not preferred development products.

The immediate objective is to establish a security foundation that can later support automated security screening, dependency monitoring, independent AI review, and release assurance.

---

# 2. Relationship to Existing KST v2 Rules

The new security policies extend existing project principles rather than replacing them.

KST v2 already establishes that:

- the repository is durable project memory;
- agent-generated work should be independently verified;
- compiler, tests, architecture tests, and guided human verification are external correctness mechanisms;
- production database access is read-only;
- KST does not directly write operational changes to QAD;
- credentials must not be hard-coded;
- important operational values should remain traceable to their source;
- architectural boundaries should remain explicit and testable. 
These principles remain authoritative.

Security controls should strengthen them without creating conflicting project rules.

---

# 3. Security Foundation Principles

## 3.1 Security Is an Architectural Requirement

Security constraints are not optional implementation guidance.

An implementation difficulty does not justify silently weakening:

- network restrictions;
- database permissions;
- credential protections;
- input validation;
- process isolation;
- filesystem restrictions;
- security scanning;
- dependency policy;
- other established controls.

When an implementation conflicts with a security requirement, the conflict must be surfaced.

---

## 3.2 Implementation Decisions and Trust Decisions Are Different

AI agents may normally make routine implementation decisions autonomously.

Examples include:

- method structure;
- local refactoring;
- UI component organization;
- test implementation;
- use of already-approved project dependencies.

Some actions create or change a trust relationship and therefore require additional scrutiny.

Examples include:

- adding a third-party dependency;
- enabling a new plugin or extension;
- adding an MCP server or agent skill;
- introducing a new executable or script dependency;
- creating network exposure;
- handling new credentials;
- expanding filesystem access;
- introducing subprocess execution;
- changing database privilege expectations;
- sending project information to a new external service.

These are **trust decisions**, not ordinary coding decisions.

---

## 3.3 Verification Must Be Independent of Generation

A coding agent's assertion that its own work is secure is not sufficient evidence.

Security confidence should come from independent forms of verification, including:

- compilers and type systems;
- automated tests;
- architecture tests;
- dependency analysis;
- secret scanning;
- static security analysis;
- runtime verification;
- independent AI review;
- targeted human review.

No single mechanism is authoritative.

---

## 3.4 Security Findings Require Evidence

Material security findings should be reproducible whenever practical.

A finding should identify:

1. What is wrong?
2. Where is it?
3. Why is it security relevant?
4. What evidence supports the claim?
5. How can another reviewer reproduce or verify it?
6. Which expected security property does it violate?
7. What remediation is recommended?

Potential findings that cannot yet be demonstrated should be labeled accordingly.

---

## 3.5 Security Controls Should Be Proportional to Risk

Not every code change requires a full security review.

Routine implementation should remain fast.

Stronger review should be triggered when software changes:

- dependencies;
- trust boundaries;
- network behavior;
- credentials;
- production-system access;
- process execution;
- filesystem privileges;
- external integrations;
- security-sensitive parsing or input handling;
- deployment behavior.

---

# 4. Policy 1 — Third-Party Dependency Admission

## 4.1 General Rule

No AI coding agent should silently introduce a new third-party software dependency.

This applies to:

- NuGet packages;
- npm packages;
- Cargo crates;
- Python packages;
- executable tools;
- downloaded binaries;
- Git-hosted dependencies;
- build tools;
- development dependencies;
- generated-code tooling;
- package-manager plugins.

The policy applies even when the dependency is only used during development or build.

---

## 4.2 Dependency Preference Order

When solving a problem, prefer:

1. Existing project functionality
2. Existing approved dependency
3. Standard library or platform capability
4. Established third-party dependency
5. New, obscure, or specialized dependency

A new package should not be introduced merely because it saves a small amount of code.

This is not a prohibition against dependencies.

It is a requirement to make dependency trust deliberate.

---

## 4.3 Dependency Proposal

Before adding a new direct dependency, the agent should identify:

- package name;
- intended version or version range;
- ecosystem;
- purpose;
- whether it is runtime, build, test, or development-only;
- why existing capabilities are insufficient;
- known alternatives where relevant.

The dependency should then pass the project's available admission checks.

---

## 4.4 Dependency Admission Evidence

As the security tooling matures, admission should consider:

- known security advisories;
- known malicious-package reports;
- package provenance;
- upstream project activity;
- maintainer information where useful;
- recent ownership or publishing anomalies;
- package age;
- release history;
- install/build scripts;
- dependency-tree impact;
- lockfile changes;
- upstream security posture;
- licensing where organizationally required.

Not all evidence will initially be automated.

The policy may exist before every enforcement mechanism exists.

---

## 4.5 Transitive Dependencies

Direct dependencies alone are insufficient for security inventory.

Lockfiles and resolved dependency trees must be retained and treated as security-relevant artifacts.

For KST this currently includes at least:

- NuGet resolved dependencies;
- npm dependency lock state;
- Cargo dependency lock state.

---

## 4.6 Security Advisory Handling

A known affected package must not be ignored merely because:

- the vulnerable functionality is believed to be unused;
- the package is transitive;
- the application is internal;
- endpoint security is installed;
- the dependency is difficult to replace.

The actual exposure may ultimately be judged acceptable, but that decision must be explicit and evidence-based.

---

# 5. Policy 2 — Development-Tool Supply Chain

Application dependencies are not the only executable third-party software involved in AI-assisted development.

The development environment itself is part of the software supply chain.

This includes:

- IDE extensions;
- AI-agent extensions;
- agent packages;
- skills;
- MCP servers;
- hooks;
- scripts;
- plugins;
- build tools;
- formatters;
- linters;
- generators;
- locally installed executables invoked by an agent.

---

## 5.1 General Rule

Third-party components that extend an AI development environment must be treated as executable dependencies.

Their risk is determined by what they can do, not by what the platform calls them.

A "skill," "extension," "plugin," or "MCP server" may execute code or expose additional capabilities and must therefore be considered part of the development trust boundary.

---

## 5.2 Installation and Activation

AI agents must not autonomously install new development-environment extensions, packages, plugins, skills, MCP servers, binaries, or equivalent executable components.

New development tooling requires explicit human awareness and appropriate review.

Existing project instructions already treat activation of installed Pi packages as human-controlled behavior. The security policy generalizes that principle to all development environments.

---

## 5.3 Environment Inventory

The future security baseline should inventory relevant development tooling, including:

- agent platform;
- agent version;
- installed extensions;
- installed packages;
- installed skills;
- configured MCP servers;
- project instruction files;
- globally applied instruction files where relevant;
- build tools;
- package managers.

The objective is visibility, not centralized control over developer preferences.

---

# 6. Policy 3 — Development Environment Security

The project will not require one approved coding environment.

Instead, development environments should be evaluated by capability and risk.

Each environment should be considered in terms of:

## Execution

What can the agent execute?

Examples:

- shell;
- PowerShell;
- Python;
- compilers;
- package managers;
- arbitrary executables;
- build scripts.

## Access

What can the agent read or modify?

Examples:

- repository;
- user profile;
- shared drives;
- credential stores;
- environment variables;
- network resources;
- production systems.

## Network

What network destinations can the environment reach?

## Credentials

Which credentials are inherited by agent-created processes?

## External Data Transfer

What project information can leave the workstation or execution environment?

## Extension Surface

What third-party code can extend the environment?

---

# 7. Policy 4 — AI Data Handling

Model capability does not override company data-handling requirements.

Before project material is sent to an external AI service, consideration must be given to whether it contains:

- credentials;
- API keys;
- passwords;
- connection strings;
- production data;
- customer data;
- confidential operational information;
- internal addresses or infrastructure;
- proprietary source code;
- database schemas;
- security configuration;
- logs containing sensitive data.

---

## 7.1 Secrets

Actual secrets must not be intentionally supplied to AI models unless an explicitly approved organizational requirement exists.

Use:

- redaction;
- placeholders;
- sanitized logs;
- structural descriptions;

where actual values are unnecessary.

---

## 7.2 External AI Services

Security review may use frontier models when their additional capability is justified, but only through services permitted to receive the information involved.

When external processing is prohibited:

- use an approved local/on-premises model;
- sanitize the material;
- provide reduced excerpts;
- or do not use the external model.

---

## 7.3 Local Does Not Automatically Mean Safe

A local model avoids some data-transfer concerns but may still:

- execute commands;
- install dependencies;
- access credentials;
- modify files;
- reach network resources.

Local execution and safe execution are separate questions.

---

# 8. Policy 5 — Application Attack-Surface Declaration

KST should maintain a machine- and human-readable description of its expected security posture.

This becomes the baseline against which implementation is tested.

Initial KST properties already established by project documentation include:

- Windows desktop application;
- local Tauri host;
- .NET backend;
- local frontend/backend communication;
- QAD remains authoritative;
- production database access must remain read-only;
- direct production database write-back is prohibited;
- operational changes leave KST as reviewable files rather than direct database modifications.

The current Tauri architecture also includes explicit content-security and local networking constraints that should be reconciled against the post-Stage-8 implementation during the security baseline.

---

# 9. Policy 6 — Network Security

Network security must be designed and verified even for applications that are not internet-facing.

The application security profile should identify:

- intended listening interfaces;
- intended listening ports or dynamic-port behavior;
- expected inbound clients;
- expected outbound destinations;
- CORS/origin policy where applicable;
- packaged-runtime differences;
- development-runtime differences.

Testing should verify the implementation rather than assuming configuration is correct.

For KST, the existing loopback-only backend requirement is a security property and must not be broadened merely to simplify development.

---

# 10. Policy 7 — Database and Production-System Access

Existing KST restrictions remain mandatory.

Production database access must remain read-only.

Application functionality must not:

- execute `INSERT`;
- execute `UPDATE`;
- execute `DELETE`;
- directly submit operational modifications;
- silently bypass controlled export/update processes.

Database-level read-only enforcement is preferred over relying solely on application behavior.

The application should continue to treat QAD and other authoritative company systems as systems of record.

---

# 11. Policy 8 — Credential and Secret Management

Credentials must not be:

- committed to source control;
- embedded in source;
- embedded in test fixtures;
- written to documentation;
- written to normal logs;
- exposed through API responses;
- copied into AI prompts unnecessarily.

Full production connection strings should not be logged.

Configuration documentation should explain how credentials are supplied without including actual values.

---

# 12. Policy 9 — Security Control Preservation

AI agents and developers must not fix implementation problems by silently:

- disabling a security scanner;
- suppressing a material finding;
- disabling certificate validation;
- broadening CORS unnecessarily;
- changing loopback binding to broad network binding;
- granting excessive filesystem access;
- granting excessive process permissions;
- weakening database restrictions;
- hard-coding credentials;
- disabling input validation;
- replacing an affected dependency with an obscure unreviewed dependency merely to remove a warning.

When such a change is believed necessary, it becomes a security-design decision requiring explicit review.

---

# 13. Policy 10 — Security-Relevant Change Triggers

Changes should receive additional security attention when they modify:

- dependency manifests;
- lockfiles;
- agent extensions or tooling;
- network listeners;
- outbound network behavior;
- CORS or CSP;
- database connection behavior;
- credentials;
- subprocess execution;
- file import;
- file export;
- filesystem permissions;
- shell commands;
- parsing of external/untrusted input;
- installer behavior;
- Tauri capabilities;
- new external services.

This should eventually become partially automated through repository-diff analysis.

---

# 14. Policy 11 — Independent AI Security Review

Routine coding-agent review is not equivalent to independent security review.

Independent AI review should use:

- a separate context;
- a security-specific objective;
- the application's declared security profile;
- relevant source or diff;
- dependency changes;
- scanner results;
- test evidence.

For higher-risk changes, use a different model or specialized security model where practical and permitted.

Security model selection should be based on demonstrated capability and data-handling suitability rather than whether a model is local, open, or proprietary.

---

# 15. Policy 12 — Security Finding Classification

Initial finding states:

- **Confirmed**
- **Potential / Investigation Required**
- **Resolved**
- **False Positive**
- **Accepted Risk**
- **Unable to Verify**
- **Informational**

Severity and finding state are separate concepts.

For example:

```text
Severity: High
State: Potential / Investigation Required
```

is different from:

```text
Severity: High
State: Confirmed
```

---

# 16. Policy 13 — Risk Acceptance

AI systems do not possess authority to accept security risk.

The developer who introduced or owns an implementation should not unilaterally accept unresolved material security risk without an appropriate escalation path.

For the initial KST pilot:

- routine Low/Informational findings may be dispositioned at project level;
- unresolved High/Critical findings should not be silently accepted;
- explicit security-boundary exceptions should be documented;
- final organizational risk-acceptance authority remains to be determined with IT/security.

---

# 17. Policy 14 — Risky and Adversarial Execution

Development work that intentionally executes suspicious, untrusted, or adversarial software should not automatically occur on the normal development workstation.

Examples include:

- intentionally installing suspected malicious packages;
- executing exploit samples;
- running hostile fuzz inputs against unsafe targets;
- dynamic malware analysis;
- testing untrusted binaries;
- intentionally exercising known remote-code-execution paths.

Such work should eventually use an appropriately isolated disposable environment.

Routine supervised coding does not automatically require isolation.

The required level of isolation should be proportional to the activity.

---

# 18. Policy 15 — Security Baseline and Release Evidence

KST should maintain a security baseline containing, at minimum:

- application security profile;
- dependency inventory;
- development-tool inventory;
- known security findings;
- unresolved assumptions;
- current security controls;
- available automated security checks;
- SBOM when implemented;
- most recent security-review date;
- risk exceptions.

A release security check should eventually compare current state against that baseline.

---

# 19. Repository Structure

The final paths should be reconciled with the repository scan before implementation.

A provisional structure is:

```text
/
├── AGENTS.md
├── SECURITY.md
│
├── docs/
│   └── security/
│       ├── SECURITY_ASSURANCE_POLICY.md
│       ├── DEVELOPMENT_ENVIRONMENT_SECURITY.md
│       ├── DEPENDENCY_ADMISSION.md
│       ├── AI_SECURITY_REVIEW.md
│       ├── APPLICATION_SECURITY_PROFILE.md
│       └── SECURITY_BASELINE.md
│
└── <platform-specific agent configuration>
```

---

# 20. Document Responsibilities

## `SECURITY.md`

Short repository entry point.

Contains:

- security principles;
- where policy lives;
- how to report/find security issues;
- current security-document index.

It should remain short.

---

## `SECURITY_ASSURANCE_POLICY.md`

Authoritative platform-neutral security rules.

Contains the cross-project policies defined in this document.

This should be the primary normative source.

---

## `DEVELOPMENT_ENVIRONMENT_SECURITY.md`

Defines security expectations for coding environments regardless of vendor.

Contains:

- execution model;
- filesystem access;
- credential access;
- external data handling;
- third-party extensions;
- unattended execution;
- isolation expectations.

---

## `DEPENDENCY_ADMISSION.md`

Defines:

- what constitutes a dependency;
- when review is triggered;
- required evidence;
- Accept / Review / Block decisions;
- exceptions;
- future automation.

---

## `AI_SECURITY_REVIEW.md`

Defines:

- independent AI review;
- review triggers;
- model/data-handling constraints;
- evidence requirements;
- finding states.

---

## `APPLICATION_SECURITY_PROFILE.md`

Defines KST's expected security boundaries.

Examples:

- loopback backend;
- permitted network behavior;
- read-only production databases;
- subprocess architecture;
- filesystem locations;
- credential mechanisms;
- external integrations.

Unlike general policy, this file is explicitly KST-specific.

---

## `SECURITY_BASELINE.md`

Records observed state rather than policy.

Examples:

- resolved dependency ecosystems;
- security tools currently available;
- installed development-agent components;
- open findings;
- security assumptions not yet tested;
- baseline date and commit.

This document should evolve from the repository/security scan.

---

# 21. `AGENTS.md` Changes

`AGENTS.md` should not duplicate the complete security policy.

Instead it should establish mandatory agent behavior and point to the authoritative documents.

Suggested content:

### Security Requirements

Security constraints are architectural requirements.

Before implementation, retrieve and follow the repository security policies relevant to the task.

Do not:

- silently introduce new third-party dependencies;
- install new development-agent packages or extensions;
- weaken existing security boundaries;
- expose credentials or production data;
- suppress material security findings merely to make verification pass.

New third-party dependencies require the dependency-admission process.

Changes involving networking, credentials, database access, subprocesses, filesystem access, external services, security-sensitive input handling, or agent tooling are security-relevant changes and require appropriate review.

When implementation conflicts with an established security requirement, preserve the security boundary and surface the conflict.

Material security findings must include reproducible evidence whenever practical.

---

# 22. Platform-Specific Agent Instructions

Platform-specific instructions should remain thin.

Their job is to reinforce the repository policy, not redefine it.

For any agent platform that supports project-level system or instruction augmentation, the adapter should communicate approximately:

> This repository has mandatory security requirements. Retrieve and follow the authoritative repository security policy before making security-relevant changes. Do not silently add executable third-party dependencies, weaken established security boundaries, expose secrets, or suppress material security findings. Surface conflicts instead of bypassing controls.

Specific mechanisms should be determined separately for each development platform.

Possible targets may include:

- Pi;
- GitHub Copilot;
- other local coding agents;
- future development environments.

The platform-neutral repository security policy remains authoritative if platform instructions disagree or become stale.

---

# 23. Initial Enactment Plan

Security implementation should occur after the Stage 8 repository/documentation reconciliation establishes the new authoritative repository state.

## S0.0 — Repository Reconciliation

Complete the planned full repository scan.

Do not mix large documentation reconciliation with security remediation.

Establish clean accepted baseline.

---

## S0.1 — Security Policy Injection

Create:

- `SECURITY.md`;
- `docs/security/SECURITY_ASSURANCE_POLICY.md`;
- `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`;
- `docs/security/DEPENDENCY_ADMISSION.md`;
- `docs/security/AI_SECURITY_REVIEW.md`;
- `docs/security/APPLICATION_SECURITY_PROFILE.md`.

Update:

- `AGENTS.md`;
- project documentation index;
- master checklist/status documents;
- appropriate build/release documentation.

Add platform-specific security instruction shims only after verifying their current supported mechanisms.

No new security scanner is required during this step.

---

## S0.2 — Baseline Discovery

Perform a read-only security inventory.

Inventory:

### Application dependencies

- NuGet;
- npm;
- Cargo.

### Development dependencies

- SDKs;
- build tools;
- generators;
- Tauri tooling.

### Agent environment

- agent platforms in current use;
- extensions;
- packages;
- skills;
- MCP servers where applicable;
- instruction files.

### Attack surface

- network listeners;
- CORS;
- CSP;
- Tauri capabilities;
- subprocess behavior;
- filesystem use;
- credential paths;
- database access.

Produce `SECURITY_BASELINE.md`.

Do not automatically remediate everything found during discovery.

---

## S0.3 — Existing-Tool Security Checks

Use existing ecosystem capabilities before adding new tools.

Determine what can already be checked through:

- .NET/NuGet tooling;
- npm;
- Cargo;
- compiler/analyzer infrastructure;
- existing repository tests;
- operating-system network/process inspection;
- repository search.

Record:

- useful signal;
- gaps;
- false positives;
- execution time.

---

## S0.4 — Security Tool Admission

Evaluate additional security tooling through the dependency-admission process itself.

Potential categories:

- cross-ecosystem vulnerability scanner;
- SBOM generator;
- secret scanner;
- static application security testing;
- Cargo-specific audit tooling;
- upstream project-health analysis.

Do not install a large security toolchain simply because tools are available.

Each tool should solve a measured gap.

---

## S0.5 — Automate Dependency Admission

Detect changes to dependency manifests and lockfiles.

Produce a machine-readable dependency-change report.

Initial result:

```text
No dependency change
    → continue

Existing dependency version change
    → security evaluation

New direct dependency
    → admission evaluation

Unexpected executable/tool dependency
    → human review
```

---

## S0.6 — Attack-Surface Verification

Convert important KST security assumptions into executable checks.

Initial candidates:

- backend remains loopback-only;
- expected CORS policy;
- expected CSP;
- no unexpected listeners;
- read-only database architecture;
- no credentials in normal logs;
- owned sidecar behavior;
- expected subprocesses only;
- packaged behavior matches intended architecture.

The current architecture already provides several of these as declared constraints; the security track adds deliberate adversarial verification.

---

## S0.7 — Independent AI Review Pilot

Select one meaningful completed KST capability.

Run:

1. conventional security tools;
2. local-model security review;
3. approved frontier/specialized review where permissible.

Require reproducible findings.

Compare:

- confirmed findings;
- false positives;
- unique findings;
- review cost;
- review time;
- evidence quality.

Do not assume the strongest model is the most useful until measured.

---

## S0.8 — Release Security Gate

After the pilot establishes useful checks, integrate a security gate into release verification.

The first version should answer:

```text
Dependency state known?
Security scans executed?
Material findings unresolved?
Security profile deviations?
Required evidence available?
Risk exceptions documented?
```

This does not become a claim that the application is vulnerability-free.

It establishes evidence that the defined security process was completed.

---

# 24. Initial Enforcement Levels

Not every policy needs immediate automated blocking.

Use three levels:

## Documented

The requirement exists but is not yet automated.

## Warn

The system detects deviation and requires disposition.

## Block

The condition prevents acceptance/release unless formally overridden.

Initial likely examples:

| Control | Initial Level |
|---|---|
| New dependency disclosure | Block agent from silent addition |
| Secret committed to repository | Block |
| Production DB write capability | Block |
| Known malicious dependency | Block |
| New network exposure | Human review |
| Security scanner warning | Review based on severity/evidence |
| New agent extension/package | Human approval |
| Missing SBOM | Documented initially; release gate later |
| Independent AI security review | Security-change/release trigger |
| Security-profile mismatch | Review; critical boundaries may block |

The pilot should determine where automation is reliable enough to justify stronger enforcement.

---

# 25. Immediate KST Security Requirements

The following should become mandatory immediately when the security policy is enacted:

1. No new third-party dependency may be silently introduced.
2. No new agent extension/package/plugin/skill may be silently introduced.
3. Established loopback networking must not be broadened without explicit review.
4. Production databases remain read-only.
5. Direct QAD/database write-back remains prohibited.
6. Credentials and production secrets must not be committed, logged, or supplied unnecessarily to AI systems.
7. Existing security checks must not be disabled merely to make implementation pass.
8. Security-relevant architectural changes must be identified as such.
9. Material security findings require evidence.
10. AI-generated security findings are not automatically considered confirmed.
11. AI agents cannot accept material security risk.
12. External AI use must respect company data-handling constraints.

---

# 26. Items Intentionally Not Finalized Yet

The initial policy should not pretend we already know:

- final security severity thresholds;
- organizational risk-acceptance authority;
- approved external AI providers;
- exact SBOM format;
- exact vulnerability scanner;
- exact SAST platform;
- CI/CD implementation;
- centralized portfolio inventory;
- final development-environment risk tiers;
- isolation technology;
- mandatory frontier-model review triggers;
- organization-wide adoption rules.

These should emerge from evidence gathered during the KST pilot and consultation with IT/security.

---

# 27. First Completion Gate

The Security Foundation initial implementation is complete when:

- authoritative platform-neutral security policy exists;
- `AGENTS.md` references and enforces it;
- development-environment policy exists;
- dependency-admission policy exists;
- AI security-review policy exists;
- KST application security profile exists;
- platform-specific instructions reinforce rather than redefine policy;
- existing dependencies and development tools have been inventoried;
- current security assumptions and unverified gaps are recorded;
- no security tool was introduced without going through the new admission logic;
- subsequent coding-agent sessions automatically encounter the security requirements.

The next step after this gate is not automatically more tooling.

It is to review the baseline and determine which controls provide the greatest additional security value per unit of complexity.
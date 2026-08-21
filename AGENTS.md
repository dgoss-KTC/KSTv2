# KST v2 — Agent Operating Instructions

These instructions apply to all agent-assisted work in the KST v2 repository.

KST v2 is a long-lived internal scheduling application. Correctness, traceability, maintainability, and preservation of established business rules are more important than implementation speed.

## 1. Authority and Project Memory

The repository is the authoritative project memory.

Before making implementation decisions:

1. Inspect the current repository state.
2. Read the current project status and the documentation relevant to the active stage.
3. Inspect the existing implementation before proposing changes.
4. Prefer current stage-specific documentation over older general planning documents when they conflict.
5. Prefer implemented and accepted behavior over stale planning assumptions unless current authoritative documentation explicitly changes it.
6. Treat prototypes and legacy KST artifacts as evidence and design references, not automatically as authoritative implementation specifications.

Do not rely on remembered conversation history when repository evidence is available.

If agent memory or prior-session recall conflicts with current repository documentation, the repository wins.

Do not silently reconcile conflicting authoritative sources. Identify the conflict and ask the project owner when it affects correctness.

### Documentation Authority Tiers

The rules above are formalized as six tiers of documentation authority, used to resolve "which
document wins" when repository material appears to disagree:

1. **Enacted repository rules** — this file (`AGENTS.md`) and any future enacted security policy.
   Normative; future work must obey these.
2. **Accepted current project state** — `docs/status/CURRENT_PROJECT_STATUS.md`,
   `KST-v2-Master-Project-Checklist.md`, current architecture documentation, and current
   QAD/source documentation. Describes what is true now and what comes next.
3. **Accepted implementation evidence** — accepted stage closeouts, contracts, validation reports,
   and implementation/checkpoint records (tests or code only where documentation itself needs
   evidentiary clarification). Proves how the accepted current state was implemented, and resolves
   a stale descriptive claim in Tier 2/4 material when the two conflict.
4. **Active planning artifacts** — plans describe intended work. A plan is never evidence that the
   work has been completed.
5. **Reference / Historical / Superseded Evidence** — original charter material, superseded
   checklists/plans/prompts, prototype/design references, review packets, legacy-system evidence,
   and earlier decisions retained for provenance. Remains legitimate project evidence but cannot
   silently override accepted current state.
6. **Ephemeral working material** — session/agent memory and chat-only investigation not promoted
   into repository documentation. Retrieval assistance only, never durable project authority.

Additional rules:

* Accepted implementation evidence (Tier 3) resolves stale descriptive planning claims (Tier 2/4),
  but implemented behavior never silently overrides an explicit normative requirement in Tier 1.
* Newer file dates, filenames, or directory locations do not by themselves establish higher
  authority. In particular, `docs/reference/` is a mixed reference/provenance area — most material
  there is Tier 5, but at least one accepted implementation artifact also resides there. File
  location is a navigation aid, not an authority guarantee; determine authority from the document's
  stated role, the accepted current project state, and this tier model.
* When authority remains genuinely ambiguous after applying this model, surface the ambiguity to
  the project owner rather than guessing.

## 2. Inspect Before Editing

Do not begin implementation from the task description alone.

Before modifying production code:

* inspect the relevant architecture and stage documentation;
* inspect existing implementations of similar capabilities;
* inspect affected tests;
* inspect API and domain contracts where relevant;
* inspect the current Git working tree;
* identify existing abstractions that should be reused.

For substantial or cross-layer work, use planning mode before implementation.

Planning should identify:

* requirements and acceptance criteria;
* authoritative project documents;
* affected layers and likely files;
* existing patterns to reuse;
* uncertainties or conflicting evidence;
* proposed implementation sequence;
* required verification.

Do not modify production code while performing a planning-only task.

## 3. Do Not Guess Business Rules

Never invent or infer a business rule, QAD field meaning, source mapping, calculation, fallback, precedence rule, or operational workflow merely to complete an implementation.

When evidence is insufficient:

1. Search repository documentation and existing implementation.
2. Search relevant source/query evidence.
3. If the answer remains uncertain and affects correctness, ask the project owner.

Prefer a focused question over a plausible assumption.

Do not add a field to an application contract merely because the field exists in QAD, a legacy query, prototype, report, or database table.

## 4. Architecture Boundaries

Preserve the established backend dependency model.

```text
Kst.Domain
    ↑
Kst.Application
    ↑
Kst.Infrastructure
Kst.Integrations.Qad
Kst.Integrations.Shortages
Kst.Exports
    ↑
Kst.Api
```

General responsibilities:

* **Kst.Domain** — pure business concepts, value objects, calculations, classifications, and business rules. No database, HTTP, UI, or infrastructure dependencies.
* **Kst.Application** — use cases, orchestration, service interfaces, and application contracts.
* **Kst.Infrastructure** — shared technical implementations and local persistence.
* **Kst.Integrations.Qad** — QAD-specific SQL, schema knowledge, source adapters, and translation at the QAD boundary.
* **Kst.Integrations.Shortages** — shortage-system integration boundary.
* **Kst.Exports** — export-generation boundary.
* **Kst.Api** — dependency injection, HTTP endpoints, DTO mapping, Problem Details, OpenAPI, and API-host concerns.
* **React/TypeScript frontend** — presentation, interaction state, and frontend-owned display logic.
* **Tauri/Rust** — desktop host and sidecar/process lifecycle responsibilities.

Do not bypass these boundaries for convenience.

Keep QAD table structures and source-specific details inside the integration boundary where practical. Domain and frontend models should represent scheduling concepts rather than reproducing database schemas.

## 5. Shared-Abstraction Rule

Do not create abstractions solely because a future stage might need them.

Preferred progression:

```text
First real use
    ↓
Small focused implementation
    ↓
Second real use
    ↓
Compare actual requirements
    ↓
Extract shared abstraction when justified
```

Reuse an existing abstraction when it already represents the required business meaning.

Do not duplicate an existing calculation, source adapter, or business rule merely because creating another implementation is easier locally.

## 6. API Contract Rules

C# API DTOs are authoritative.

The contract flow is:

```text
C# DTOs
    ↓
OpenAPI
    ↓
Generated TypeScript types
    ↓
Frontend API client
    ↓
React components
```

Rules:

* Never manually edit `src/frontend/src/generated/api.ts`.
* After changing API DTOs, endpoints, or response shapes, rebuild the backend to regenerate OpenAPI.
* Regenerate TypeScript contracts using the repository's established generation command.
* Fix resulting TypeScript errors at their real source.
* Commit the OpenAPI specification and generated TypeScript contract together when changed.
* Do not duplicate backend business rules in the frontend unless the architecture explicitly assigns that rule to the frontend.

## 7. Database Safety

All company database access is read-only.

KST v2 must never directly:

* INSERT;
* UPDATE;
* DELETE;
* MERGE;
* execute database-side operational changes;
* submit changes into QAD or QXtend;
* automatically trigger an import.

The application may retrieve, analyze, display, validate, stage locally, and export proposed changes.

Operational changes leave KST as human-reviewable files for external processing.

QAD remains the authoritative operational system of record.

Never expose credentials, passwords, complete connection strings, or other secrets in source code, logs, tests, documentation, agent memory, or responses.

## 8. Security Requirements

Security constraints are architectural requirements, not optional guidance.

Before security-relevant implementation, retrieve and follow the applicable repository security
policy: start with `SECURITY.md` and `docs/security/SECURITY_ASSURANCE_POLICY.md`, plus
`docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`, `docs/security/DEPENDENCY_ADMISSION.md`,
`docs/security/AI_SECURITY_REVIEW.md`, and `docs/security/APPLICATION_SECURITY_PROFILE.md` as
relevant to the change.

Do not silently:

* introduce a third-party executable dependency;
* install or activate new agent tooling (extensions, packages, plugins, skills, MCP servers);
* weaken an established security boundary;
* expose credentials or production data;
* suppress a material security finding.

New dependencies follow the dependency-admission process in
`docs/security/DEPENDENCY_ADMISSION.md`.

Networking, credentials, database access, subprocess execution, filesystem access, external
services, security-sensitive input handling, deployment behavior, and development-agent tooling are
security-relevant changes.

When implementation conflicts with an established security requirement, preserve the security
boundary and surface the conflict rather than silently weakening it.

Material security findings require evidence where practical. AI agents cannot accept material
security risk.

`SECURITY.md` and the policy documents under `docs/security/` are enacted, owner-accepted Tier 1
authority (S0.1 — COMPLETE / ACCEPTED — 2026-08-21).

## 9. SQL and Source-Data Rules

Use source-system facts deliberately.

* Preserve site and domain context where required.
* Maintain source traceability for important values and transformations.
* Prefer parameterized SQL.
* Maintain compatibility with the currently supported SQL Server environment.
* Do not introduce newer SQL Server features without confirming compatibility.
* Do not introduce defensive deduplication, fallback logic, or inferred joins without evidence that they are required.
* Do not move business classification into SQL merely because SQL can perform it when the accepted architecture assigns that rule to C#.
* Do not move source-specific QAD knowledge into Domain or frontend code.

When source data contradicts an existing assumption, stop and surface the evidence rather than silently changing the business rule.

## 10. Stage and Scope Discipline

KST v2 uses rolling-wave implementation organized by UI capability and stage.

Work only within the requested stage, checkpoint, or task.

Do not:

* begin the next stage early;
* implement speculative future capabilities;
* expand scope because nearby code appears incomplete;
* refactor unrelated code without demonstrated need;
* reopen previously accepted decisions without new evidence.

If implementation evidence reveals a genuine problem with an accepted decision, report it explicitly before changing the decision.

Stage-specific implementation checklists and accepted decision documents take precedence over older general checklist assumptions.

## 11. Change Discipline

Prefer the smallest coherent change that satisfies the accepted requirement.

Before creating something new, search for an existing:

* domain model;
* service;
* adapter;
* query;
* endpoint pattern;
* DTO;
* frontend component;
* preference mechanism;
* test fixture;
* utility.

Preserve established naming and structural conventions.

Avoid opportunistic cleanup unrelated to the active task.

Do not rewrite working code solely to make it stylistically preferable.

Do not delete apparently unused code until its purpose and references have been investigated.

## 12. Tests Are Required Evidence, Not Proof of Business Correctness

Add or update automated tests when behavior changes.

Relevant verification may include:

* domain unit tests;
* application tests;
* QAD adapter tests;
* API integration tests;
* architecture tests;
* frontend component tests;
* TypeScript type checking;
* linting;
* frontend build;
* Rust/Tauri checks;
* sidecar build;
* manual guided application testing;
* comparison against QAD or accepted legacy evidence.

Passing automated tests demonstrates that tested behavior works. It does not prove that a business rule or source mapping is correct.

Never invent expected business values merely to make a test pass.

## 13. Verification Before Completion

Run verification appropriate to the layers changed.

Use repository-documented build and test commands rather than inventing alternate verification workflows without reason.

When backend/API contracts change, include contract regeneration and frontend type verification.

When backend code used by Tauri changes, remember that rebuilding the .NET project alone does not necessarily refresh the published sidecar used by Tauri. Follow the documented sidecar rebuild workflow when live application verification requires it.

For UI behavior requiring visual or interaction confirmation, provide concise manual guided-testing steps to the project owner rather than pretending automated inspection proves the behavior.

Report failed verification truthfully.

## 14. Human Review and Acceptance

Agent completion is not project-owner acceptance.

When a checkpoint requires owner review:

1. Complete only the agreed checkpoint.
2. Run its required verification.
3. Summarize what changed and the evidence.
4. Identify unresolved questions, risks, or deviations.
5. Stop for review.

Do not mark project-owner acceptance complete until the owner explicitly accepts it.

Do not interpret "proceed" on implementation work as retroactive acceptance unless the context clearly establishes acceptance.

When requested to prepare a commit for review, do not commit or push until explicitly authorized.

## 15. Git Safety

Before substantial work, inspect:

```text
git status
git log --oneline
```

Do not discard, overwrite, reset, or silently absorb pre-existing uncommitted work.

Do not use destructive Git operations unless explicitly authorized.

Do not commit or push unless requested or explicitly authorized.

Keep commits aligned with accepted project checkpoints where practical.

Before proposing a commit, inspect the actual diff and summarize what will be included.

## 16. Documentation

Update durable repository documentation when implementation establishes or changes:

* business rules;
* authoritative source mappings;
* architecture decisions;
* API behavior;
* operational procedures;
* verification requirements;
* known limitations;
* stage status.

Do not use agent memory as a substitute for repository documentation.

Temporary implementation thoughts, session state, and exploratory hypotheses do not automatically belong in durable project documentation.

## 17. Agent Memory Policy

Agent memory is secondary, non-authoritative working assistance.

Good memory candidates include:

* recurring repository-navigation lessons;
* known development-environment gotchas;
* corrections to recurring agent behavior;
* useful procedural reminders;
* locations of authoritative documentation;
* previously encountered troubleshooting patterns.

Business rules, source mappings, accepted contracts, and architecture decisions must ultimately live in repository documentation and/or tests.

When recalling memory:

1. Treat it as a retrieval hint.
2. Verify important claims against the current repository.
3. Discard stale memory when repository evidence disagrees.
4. Never allow memory to silently override the current task or authoritative documentation.

Do not store credentials or sensitive configuration in agent memory.

## 18. Uncertainty Policy

Distinguish among:

* **Known** — supported by current repository evidence.
* **Inferred** — strongly suggested by implementation or evidence but not explicitly established.
* **Unknown** — insufficient evidence exists.

Do not present inferred or unknown information as established fact.

If an unknown materially affects implementation correctness, ask before proceeding.

Minor implementation choices that do not affect business behavior, architecture, public contracts, data correctness, or user workflow may be resolved using established repository conventions.

## 19. Definition of Agent Completion

A task is complete only when:

* requested scope is implemented;
* relevant tests/checks pass;
* generated artifacts are synchronized where required;
* no known requirement was silently omitted;
* no unrelated scope was introduced;
* documentation is updated where required;
* manual verification needs are clearly identified;
* remaining uncertainties or risks are reported;
* the working-tree state is understood;
* required human review or acceptance has not been falsely claimed.

Optimize for correctness and maintainability, not for appearing finished.

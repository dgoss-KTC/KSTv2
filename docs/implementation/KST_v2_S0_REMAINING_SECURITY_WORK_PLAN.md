# KST v2 — Remaining S0 Security Foundation Work Plan

**Status:** Approved Planning Baseline — 2026-08-24
**Authority:** Active Planning / Tier 4
**Starting repository commit:** `29141d2789646d5fe00894df5fe7200161e8fe77`
(`docs: accept S0.3 existing-tool security checks`)

> This document is the project-owner-approved roadmap for the remaining work of
> **S0 — Security Foundation Integration**. It is **active planning (Tier 4)**: it is not
> normative policy, not evidence, and not a completion record. Required security properties
> remain defined by `SECURITY.md` and `docs/security/` (especially
> `SECURITY_ASSURANCE_POLICY.md` and `APPLICATION_SECURITY_PROFILE.md`).
>
> **S0.1–S0.3 are accepted evidence/history.** This plan does not reopen, renumber, or
> restate their findings; every finding/gap state below is quoted from the accepted S0.2/S0.3
> evidence documents. **S0.4–S0.8 are approved future checkpoints.** Approval of this roadmap
> does **not** mean that any future checkpoint is complete, and no finding is dispositioned by
> this document.

## 1. Purpose

This plan formalizes the remaining S0 work that the accepted S0.2/S0.3 evidence shows is still
outstanding, so that:

1. the canonical project status can stop pointing at Stage 9 as the immediate next work while
   S0 is only partially complete;
2. each open finding and capability gap has exactly one named checkpoint responsible for it;
3. the order and boundaries of the work are owner-approved before any implementation begins;
4. organizational decisions that are outside engineering authority stay explicitly unresolved.

This planning pass changed documentation only. It performed **no** remediation, no security
checks, no tool installation or evaluation, no application code/configuration change, no
runtime verification, and no database access. It did not begin S0.4.

## 2. Accepted Starting State

Accepted checkpoints (evidence of record, not modified by this plan):

- **S0.1 — Security Policy Injection:** COMPLETE / ACCEPTED — 2026-08-21. Enacted policy set:
  `SECURITY.md`, `docs/security/SECURITY_ASSURANCE_POLICY.md`,
  `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`, `docs/security/DEPENDENCY_ADMISSION.md`,
  `docs/security/AI_SECURITY_REVIEW.md`, `docs/security/APPLICATION_SECURITY_PROFILE.md`,
  `AGENTS.md` §8.
- **S0.2 — Security Baseline Discovery:** COMPLETE / ACCEPTED — 2026-08-24. Observational
  baseline at commit `4b4ba3f`: `docs/security/SECURITY_BASELINE.md` (evidence, not policy).
- **S0.3 — Existing-Tool Security Checks:** COMPLETE / ACCEPTED — 2026-08-24. Check-execution
  commit `18fdc84`: `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (evidence, not
  policy).

Open tracked findings at this commit (states as recorded in the accepted evidence):

| ID | State (per accepted evidence) | Area |
|---|---|---|
| S0.2-F001 | Potential / Investigation Required | Tauri shell-capability scope (`shell:allow-execute`/`shell:allow-open` granted without an observed scope restricting execution to the `Kst.Api` sidecar) |
| S0.2-F002 | Retired (2026-08-24) | Database-level read-only enforcement — retired per operator/IT authority; no longer an open finding |
| S0.2-F003 | Confirmed | QAD SQL transport configuration mismatch (repository configuration does not accurately express the IT-confirmed `Encrypt=false` requirement) |
| S0.3-F001 | Confirmed | npm advisories in development-only tooling (`openapi-typescript`, transitive `undici`, transitive `nanoid`) |

Accepted capability/verification gaps: `S0.3-G001` through `S0.3-G010`
(`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §11).

Facts that frame the remaining work:

- No item in the accepted S0.2/S0.3 evidence is marked `Accepted Risk`. No KST risk severity is
  assigned to any item; final severity thresholds remain intentionally unresolved.
- S0.1–S0.3 installed no tools, remediated nothing, and changed no dependency manifest,
  lockfile, configuration, or security control.
- The underlying unencrypted QAD SQL transport constraint (behind S0.2-F003) is a legacy
  infrastructure fact with formal IT/security risk acceptance **not yet established**.
- Stage 9 — Immediate Shortages is NOT STARTED and is sequenced after S0 closeout (see §13).

## 3. Planning Principles

The following principles govern S0.4–S0.8. They restate enacted policy for the benefit of the
roadmap and do not modify it:

1. **Policy before implementation.** Every checkpoint works under the enacted
   `docs/security/` policy set and `AGENTS.md` §8; a checkpoint that conflicts with enacted
   policy stops and surfaces the conflict.
2. **Evidence before remediation.** Findings are dispositioned from recorded evidence. No
   speculative fix, fallback, or "reasonable" assumption is introduced to complete a
   checkpoint (AGENTS.md §3, §18).
3. **Least privilege.** Remediation and verification target the minimum capability and
   surface required by the accepted application (sidecar execution, loopback networking,
   read-only database access).
4. **Independent verification.** Checkpoints verify through mechanisms independent of the
   agent performing the work (tests, configuration inspection, runtime observation,
   owner review, independent AI review per `AI_SECURITY_REVIEW.md`). Self-declared security
   is not evidence.
5. **No silent tool/dependency admission.** Any new tool, package, or dependency — including
   security tooling and version upgrades of existing dependencies — enters only through the
   dependency-admission process in `DEPENDENCY_ADMISSION.md` with the required human
   approval. Existing installation does not imply approval.
6. **Organizational risk decisions remain human/IT authority.** AI agents cannot accept
   material security risk. Items such as risk-acceptance authority, severity thresholds, and
   formal transport-risk disposition are identified, tracked, and escalated — never decided —
   by engineering checkpoints.
7. **No renumbering, no history rewriting.** Existing finding/gap IDs are preserved. Accepted
   S0.2/S0.3 evidence documents are not modified to mention future checkpoint work.
8. **No premature selection.** This roadmap selects no scanner/SAST/SBOM product or format,
   no CI integration, and does not pre-decide the implementation shape of any remediation
   (e.g., the exact Tauri permission model).
9. **Checkpoint discipline.** Each checkpoint follows the completion model in §14 and is
   owner-accepted separately.

## 4. Open Findings

What the roadmap assigns to each open finding — and what it deliberately does **not** decide.

### S0.2-F001 — Tauri shell-capability scope (Potential / Investigation Required)

- **Accepted evidence:** `src/tauri/capabilities/default.json` grants `core:default`,
  `shell:allow-execute`, `shell:allow-open` (window scope `["main"]`) with no observed scope
  restricting execution to the `Kst.Api` sidecar. Actual use is `app.shell().sidecar("Kst.Api")`
  in `src/tauri/src/lib.rs` with `externalBin` declared in `tauri.conf.json`. S0.3 confirmed
  that no existing test, script, or config-validation tool independently constrains or verifies
  this capability's least-privilege scope, and made no determination of Tauri v2 scoping
  semantics.
- **Assigned to S0.4:** determine the minimum Tauri shell capability required for (a) `Kst.Api`
  sidecar execution, (b) sidecar lifecycle handling, and (c) any legitimate shell/open
  behavior the application actually uses — then, **only if supported by evidence**, implement
  an explicitly scoped least-privilege configuration. The exact Tauri permission
  implementation (which permission identifiers, which scope entries) is **not** pre-decided
  here; it is an evidence-based implementation decision inside S0.4.
- **Assigned to S0.5 (regression protection):** once S0.4 settles the accepted
  least-privilege surface, S0.5 adds durable verification that Tauri capabilities remain
  within that accepted surface (gap S0.3-G004).
- **Not decided here:** whether the current configuration is actually exploitable/broader than
  intended, and what final state the finding reaches (`Resolved`, `Accepted Risk` requires
  human authority, etc.).

### S0.2-F002 — Database read-only enforcement (Retired)

- **Accepted evidence:** retired on 2026-08-24 per operator/IT authority
  (`SECURITY_BASELINE.md` §13.1/§14): QAD access is required to be read-only/least-privilege
  with Windows Integrated Authentication, and SQL-authenticated access is prohibited.
- **No remediation.** The retirement stands. The related independent verification need
  (actual server-side grants) is represented separately as gap **S0.3-G010** and assigned to
  **S0.7**.

### S0.2-F003 — QAD SQL transport configuration mismatch (Confirmed)

- **Accepted evidence:** `QadConnectionOptions.cs` defaults (`Encrypt=true`,
  `TrustServerCertificate=true`) and the committed `appsettings.json` `QadDatabase` section do
  not express the IT-confirmed required configuration: the current QAD SQL infrastructure does
  not support encrypted client connections, so the required current behavior is
  **`Encrypt=false`**, stated explicitly; `TrustServerCertificate=true` is **not** the expected
  substitute and must not be used as one (`SECURITY_BASELINE.md` §13.2, finding record §19).
- **Assigned to S0.4 (KST-side application remediation):** correct the KST-side
  configuration so the legacy QAD SQL transport requirement is represented explicitly:
  `Encrypt=false`, and do **not** use `TrustServerCertificate=true` as a substitute. The
  remediation must preserve: Windows Integrated Authentication; the internal corporate
  network restriction; and read-only / least-privilege access. The future infrastructure
  target remains `Encrypt=true` / `TrustServerCertificate=false` when QAD SQL supports TLS
  (an encrypted configuration with `TrustServerCertificate=true` would require a separately
  documented exception).
- **Not part of application remediation, remains unresolved:** formal organizational
  acceptance/disposition of the legacy unencrypted QAD transport. That is an IT/security
  decision outside engineering authority; it must remain unresolved until IT/security acts
  and is surfaced at S0.8 (see §12).

### S0.3-F001 — npm advisories in development-only tooling (Confirmed)

- **Accepted evidence:** `npm audit` (S0.3) reported 3 advisories, all in development-only
  packages: `openapi-typescript@6.7.6` (direct devDependency; npm-reported moderate),
  `undici@5.29.0` (transitive via `openapi-typescript`; npm-reported high; 12 GitHub
  advisories), and `nanoid@3.3.16` (transitive via `postcss`; npm-reported high). npm-reported
  severities are not KST risk severities; confirmed advisory ≠ confirmed exploitability;
  reachability was not analyzed in S0.3. npm's offered fix for two of the three is a **major**
  version bump of `openapi-typescript` (6.7.6 → 7.13.0).
- **Assigned to S0.4:** perform bounded exposure/reachability analysis of `openapi-typescript`,
  `undici`, and `nanoid` **in their actual development-tooling roles** (e.g.,
  `openapi-typescript` run locally by the contract-generation workflow, `nanoid` inside
  `postcss` during Vite builds). Then determine the appropriate dependency remediation — or an
  explicit evidence-backed disposition — under `docs/security/DEPENDENCY_ADMISSION.md`.
- **Not assumed here:** that `npm audit fix`, a major version upgrade, or any specific
  package/version is appropriate. Any dependency change follows the admission process,
  requires human approval, and (for `openapi-typescript`) would need contract-regeneration
  validation.

## 5. Accepted Capability / Verification Gaps

States and wording per `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §11 (accepted).
This plan assigns each gap to a checkpoint; it does not re-open or restate the gap evidence.

| ID | Gap (accepted description, abbreviated) | Assigned checkpoint |
|---|---|---|
| S0.3-G001 | No authorized/available Rust dependency advisory scanner; `Cargo.lock` (480 entries) has no native advisory-database check | S0.6 |
| S0.3-G002 | Backend loopback binding statically observed in `Program.cs` but no identified independent test coverage | S0.5 |
| S0.3-G003 | Tauri CSP in `tauri.conf.json` statically observed but no identified independent automated verification | S0.5 |
| S0.3-G004 | No identified independent verification that the granted Tauri shell capabilities are no broader than the sidecar execution need (carries S0.2-F001) | S0.5 |
| S0.3-G005 | QAD tests assert SQL shape/parameterization but no test asserts the absence of write-verb SQL | S0.5 |
| S0.3-G006 | No dedicated SAST capability; built-in Roslyn analyzers only | S0.6 |
| S0.3-G007 | No dedicated secret scanner; only the limited high-confidence sentinel search was possible | S0.6 |
| S0.3-G008 | No SBOM generation capability (exact format also an unresolved policy decision) | S0.6 |
| S0.3-G009 | Packaged (installed) runtime listener/network behavior not verifiable by existing repository tests | S0.7 |
| S0.3-G010 | Actual QAD login/group grants are server-side; no repository tool can verify them; no live connection was authorized in S0.3 | S0.7 |

Secondary accepted observation carried into S0.5: existing `CorsPolicyTests` verify the
`Access-Control-Allow-Origin` header for only **2 of the 5** configured origins and do not
assert the absence of `AllowAnyOrigin`/`AllowCredentials` (`S0_3_EXISTING_TOOL_SECURITY_CHECKS.md`
§6.3/§11).

## 6. S0.4 — Security Finding Disposition & Bounded Remediation

**Status at planning time:** NEXT / NOT STARTED.

**Purpose:** address the findings already established by accepted S0.2/S0.3 evidence —
S0.2-F001, S0.2-F003, and S0.3-F001 — with evidence-based disposition and bounded remediation.

**Scope of work:**

- **F001 (Tauri shell capability):** investigate the minimum Tauri shell capability required
  for `Kst.Api` sidecar execution, sidecar lifecycle handling, and any legitimate shell/open
  behavior; then implement an explicitly scoped least-privilege configuration only if
  supported by evidence. No pre-decided permission model.
- **F003 (QAD transport configuration):** correct the KST-side configuration to explicitly
  represent `Encrypt=false` without `TrustServerCertificate=true` as a substitute, preserving
  Windows Integrated Authentication, the internal corporate network restriction, and
  read-only/least-privilege access. Record that the future target remains
  `Encrypt=true` / `TrustServerCertificate=false` when QAD SQL supports TLS.
- **S0.3-F001 (npm dev-tooling advisories):** bounded exposure/reachability analysis of
  `openapi-typescript`, `undici`, and `nanoid` in their actual development-tooling roles, then
  a dependency remediation or explicit disposition decision under
  `docs/security/DEPENDENCY_ADMISSION.md`.

**Boundaries:**

- No organizational transport-risk acceptance (remains unresolved; see §12).
- No `npm audit fix` or major upgrade by default; any dependency change requires the
  admission process and human approval, with contract-regeneration validation where the
  affected tool is part of the C# → OpenAPI → TypeScript pipeline.
- No re-opening of S0.2-F002 (retired) and no renumbering of any finding/gap ID.
- If implementation evidence conflicts with an accepted decision or enacted policy, stop and
  surface it rather than silently changing the rule.

**Expected outputs (for S0.4 to produce, not pre-written here):** per-finding disposition
records with evidence; any implemented remediation; updated canonical status upon owner
acceptance.

## 7. S0.5 — Security Regression & Architecture Checks

**Status at planning time:** PLANNED / NOT STARTED.

**Purpose:** turn important security properties already known to KST into inexpensive durable
repository checks where practical. The goal is architecture/security **regression
protection**, not test-count maximization.

**Primary accepted gaps addressed:**

- **S0.3-G002** — loopback-binding test coverage.
- **S0.3-G003** — CSP automated verification.
- **S0.3-G004** — Tauri least-privilege verification (against the accepted surface settled in
  S0.4, if S0.4 changes it).
- **S0.3-G005** — read-only-SQL enforcement test.

**Also in scope:** evaluation of the existing partial CORS coverage — `CorsPolicyTests`
currently verify the allowed-origin header for only 2 of the 5 configured origins, and do not
assert the absence of `AllowAnyOrigin` or credentials.

**Potential test-development work, where technically appropriate:**

- loopback binding remains explicit;
- CSP retains the required restrictions;
- Tauri capabilities remain within the accepted least-privilege surface;
- production/QAD query definitions do not introduce write SQL;
- all intended CORS origins and the prohibited broad/credential behavior are covered.

**Boundaries:**

- No brittle test implementation details are specified in this planning pass; the
  checkpoint plans the mechanics from the repository state at its start.
- Existing tests are not weakened or deleted to make new checks easier.
- A passing check proves the tested behavior, not general security (accepted S0.3
  pass/fail vocabulary applies).

## 8. S0.6 — Security Tool Admission

**Status at planning time:** PLANNED / NOT STARTED.

**Purpose:** evaluate the missing security-tool capabilities **one at a time** under the
enacted dependency-admission policy
(`docs/security/DEPENDENCY_ADMISSION.md`), in an order decided at checkpoint start.

**Primary accepted gaps addressed:**

- **S0.3-G001** — Rust dependency advisories.
- **S0.3-G006** — dedicated SAST.
- **S0.3-G007** — dedicated secret scanning.
- **S0.3-G008** — SBOM.

**Process per capability (one at a time):**

1. define the actual security need (what gap it closes, which declared property it protects);
2. determine whether existing/native functionality is already sufficient (e.g., ecosystem
   native advisory checks, built-in analyzers);
3. if not sufficient, evaluate candidate tools;
4. document trust/supply-chain implications (provenance, install scripts, lockfile impact,
   maintenance);
5. obtain the required human approval **before** installation/admission;
6. only then integrate an approved tool and record the decision.

**Boundaries:**

- **No product is selected by this roadmap.** Nothing in the accepted S0.3 tool-availability
  table (`cargo-audit`, `cargo-deny`, `gitleaks`, `semgrep`, `trivy`, `osv-scanner`, `syft`,
  `grype`, or any other product) is approved by virtue of having been checked for presence.
- Unresolved decisions preserved: exact scanner, SAST platform, SBOM format, CI integration
  (all remain in `SECURITY_ASSURANCE_POLICY.md` §"Intentionally Unresolved Policy Areas").
- No tool is installed or activated during this planning pass or without the human approval
  step above.
- A capability may legitimately be closed by a decision that existing functionality is
  sufficient; not every gap requires a new tool.

## 9. S0.7 — Runtime & Infrastructure Verification

**Status at planning time:** PLANNED / NOT STARTED.

**Purpose:** verify security properties that static repository evidence could not fully
establish.

**Primary accepted gaps addressed:**

- **S0.3-G009** — packaged/runtime listener verification.
- **S0.3-G010** — server-side database-grant verification.

**Incorporated accepted "Unable to Verify" items (S0.2 §20), where still applicable:**

- packaged (installed, non-development) runtime listener/CORS/CSP/Tauri-capability behavior;
- runtime outbound network destinations beyond the statically-configured QAD server;
- actual QAD SQL Server account/login permissions and grants (server-side);
- whether `keytronicshortage` is hosted on the same legacy SQL infrastructure as QAD (and the
  conditional transport consequence), plus its permission details where appropriate;
- whether any exception path could incidentally log connection-string details at runtime.

**Potential verification areas:** packaged backend listener binding; packaged CORS behavior;
packaged CSP behavior; effective Tauri capability behavior; sidecar process lifecycle;
runtime filesystem behavior; runtime outbound destinations; server-side QAD database grants;
`keytronicshortage` permission/hosting details where appropriate.

**Stronger execution boundaries (this checkpoint may involve real execution):**

- launching packaged/runtime software;
- observing local listeners;
- controlled interaction with infrastructure;
- IT participation;
- permission inspection.

Accordingly, S0.7 must be scoped and explicitly authorized at its start, item by item, before
any of that work is performed. **Production access is not assumed to be automatically
authorized**; access that requires IT involvement is coordinated with IT, and out-of-reach
items are recorded as such rather than guessed.

**Out of S0.7's repository scope (organizational/machine-level, not verifiable by repository
work):** machine-level credential protection (Windows Credential Manager, disk encryption)
and AI-provider-side data retention/handling behavior. These remain organizational matters
(see §12). The exact resolved NuGet dependency tree (no committed NuGet lockfile) is a
supply-chain artifact observation relevant to S0.6's advisory evaluation, not a runtime
verification task.

## 10. S0.8 — Independent Assurance & S0 Closeout

**Status at planning time:** PLANNED / NOT STARTED.

**Purpose:** perform independent review and determine whether the Security Foundation can be
formally closed.

**Expected work:**

- reconcile all S0.2/S0.3 findings (S0.2-F001, S0.2-F002, S0.2-F003, S0.3-F001) and confirm
  their final states are evidenced, not asserted;
- reconcile S0.3-G001 through S0.3-G010 and confirm each is closed or explicitly carried with
  an owner-approved reason;
- confirm remediation evidence for S0.4 work;
- confirm admitted-tool decisions (or documented sufficiency decisions) from S0.6;
- confirm runtime/infrastructure verification results from S0.7;
- perform an independent AI/security review under `docs/security/AI_SECURITY_REVIEW.md`
  (separate context, security-specific objective, declared security profile, data-handling
  rules; approved external AI providers remain an organizational decision);
- verify no material unresolved finding has been silently ignored;
- identify the organizational decisions that remain outside engineering authority (see §12)
  and surface them for owner/IT action;
- determine the appropriate continuing/release security gate for KST (definition of that gate
  is **not** made prematurely by this roadmap);
- prepare final S0 closeout evidence;
- update canonical status only after owner acceptance.

**Boundaries:**

- S0.8 may identify residual risk, but **an AI agent may not accept material risk**; no
  finding is marked `Accepted Risk` without the required human/organizational authority.
- No final severity thresholds or risk-acceptance authority are invented by S0.8; those
  remain intentionally unresolved policy areas.
- Exact release-security automation is not defined by this roadmap; S0.8 proposes what the
  evidence supports and leaves the rest to a later owner decision.

## 11. Finding/Gap-to-Checkpoint Mapping

| Evidence | Current state (per accepted evidence) | Planned checkpoint |
|---|---|---|
| S0.2-F001 | Potential / Investigation Required | S0.4, regression protection S0.5 |
| S0.2-F002 | Retired | No remediation; grant verification represented separately (S0.3-G010 → S0.7) |
| S0.2-F003 | Confirmed | S0.4; organizational transport disposition later (remains unresolved; surfaced at S0.8) |
| S0.3-F001 | Confirmed | S0.4 |
| S0.3-G001 | Capability gap | S0.6 |
| S0.3-G002 | Verification gap | S0.5 |
| S0.3-G003 | Verification gap | S0.5 |
| S0.3-G004 | Verification gap | S0.5 |
| S0.3-G005 | Verification gap | S0.5 |
| S0.3-G006 | Capability gap | S0.6 |
| S0.3-G007 | Capability gap | S0.6 |
| S0.3-G008 | Capability gap | S0.6 |
| S0.3-G009 | Runtime verification gap | S0.7 |
| S0.3-G010 | Infrastructure verification gap | S0.7 |
| CorsPolicyTests partial origin coverage (accepted S0.3 secondary observation) | Partial | S0.5 |

**S0.8 is the reconciliation/assurance gate across the complete set.** No finding/gap ID is
renumbered by this plan.

## 12. Organizational Dependencies

The following decisions are **not** ordinary application implementation tasks and are not made
by any S0.4–S0.8 engineering checkpoint. The remaining-S0 work may identify where they are
needed (S0.8 surfaces their status at closeout); it must not make them:

1. **Formal acceptance/disposition of the legacy unencrypted QAD transport** (the constraint
   behind S0.2-F003) — IT/security.
2. **Final organizational risk-acceptance authority** — intentionally unresolved policy area.
3. **Final severity thresholds** — intentionally unresolved policy area.
4. **Independent production DB grant confirmation where IT participation is needed**
   (relates to S0.3-G010; S0.7 coordinates the inspection, IT owns the access/authorization).
5. **Approved external AI providers / data-handling decisions** (relevant to the S0.8
   independent review and to general development use).

Related intentionally unresolved policy areas that remain untouched by this plan: exact
vulnerability scanner, exact SAST platform, exact SBOM format, CI/CD implementation, final
development-environment risk tiers, mandatory frontier-model review triggers, and
portfolio-wide policy.

## 13. Stage 9 Gate

**Stage 9 — Immediate Shortages must not begin until S0 has been formally closed and
accepted** (S0.8 complete and owner-accepted), **unless the project owner explicitly changes
that sequencing decision.** Nothing in this plan authorizes starting Stage 9 early, and no
checkpoint above includes Stage 9 scope.

## 14. Completion Model

Each checkpoint (S0.4–S0.8) follows the same model, in order, where applicable:

1. **Explore / inspect** — current repository state, accepted evidence, and the checkpoint's
   governing policy documents.
2. **Plan where needed** — a checkpoint-specific implementation/execution plan reviewed by the
   owner before risky or trust-changing work.
3. **Human review** — owner review at the points the model requires (scope, plans, results).
4. **Implement / execute** — bounded work within the approved checkpoint scope only.
5. **Verify** — independent verification appropriate to the layers touched (tests, builds,
   contract regeneration, runtime observation where the checkpoint is scoped to it, guided
   manual steps where required).
6. **Owner acceptance** — the project owner accepts the checkpoint evidence.
7. **Commit / push** — documentation and implementation changes committed and pushed as
   authorized, with canonical status updated only to reflect accepted state.

A checkpoint that cannot complete a step truthfully reports the blocker instead of
representing the step as done.

---

*This plan was produced by a documentation-only planning pass at commit
`29141d2789646d5fe00894df5fe7200161e8fe77`. It contains no remediation, no check results,
no tool selection, and no organizational decisions. S0.4 has not been started.*

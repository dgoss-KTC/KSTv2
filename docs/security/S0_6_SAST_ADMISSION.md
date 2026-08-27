# S0.6 — Security Tool Admission: Capability Review 4 — Dedicated Static Application Security Testing (SAST)

**S0.6 Capability Review 4 — Dedicated Static Application Security Testing (SAST)**
**Status: OWNER DECISION RECORDED / DEVSKIM CLI v1.0.90 ADMITTED FOR INSTALLATION AND VERIFICATION / IMPLEMENTATION PENDING**

| Item | Value |
|---|---|
| Gap | `S0.3-G006` |
| Tool (admitted) | Microsoft DevSkim CLI v1.0.90 |
| Owner admission decision | ADMITTED for installation and verification — 2026-08-27 |
| Implementation | **PENDING** |
| Project-owner acceptance | Not yet sought |
| Microsoft DevSkim CLI v1.0.90 disposition | ADMITTED (installation and verification pending) |
| Semgrep CE v1.175.0 disposition | DEFERRED pending organizational licensing review |
| CodeQL CLI v2.26.4 disposition | DEFERRED pending confirmed applicable private-repository entitlement and organizational authorization |
| `S0.3-G006` disposition | UNDER IMPLEMENTATION (not resolved) |
| Overall S0.6 status | **IN PROGRESS** (this review closes one capability only; S0.6 as a whole is **not** complete) |
| Research evidence | `docs/security/S0_6_SAST_ADMISSION_RESEARCH.md` |
| Licensing authority | `docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md` |

This document is **evidence, not normative policy**. It records the S0.6 Capability Review 4
owner admission decision and (as implementation proceeds) installation, verification, and scan
evidence for the SAST capability (accepted S0.3 gap `S0.3-G006`). Required security properties and
tool-admission governance remain defined by `SECURITY.md`,
`docs/security/SECURITY_ASSURANCE_POLICY.md`, and `docs/security/DEPENDENCY_ADMISSION.md`. This
document is separate from, and does not modify, the neutral research packet at
`docs/security/S0_6_SAST_ADMISSION_RESEARCH.md`.

## 1. Purpose and Status

S0.6 evaluates missing security-tool capabilities **one at a time** under the enacted
dependency-admission process (`docs/security/DEPENDENCY_ADMISSION.md`), per the accepted
remaining-S0 plan (`docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §8).

Capability Review 4 addresses:

> **S0.3-G006** — no dedicated SAST tool exists in the toolchain.

Capability Review 1 (Rust dependency advisories, `S0.3-G001`), Capability Review 2 (dedicated
secret scanning, `S0.3-G007`), and Capability Review 3 (SBOM, `S0.3-G008`) are separately
COMPLETE / ACCEPTED — see `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`,
`docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`, and `docs/security/S0_6_SBOM_ADMISSION.md`.
This document does not modify that evidence.

## 2. Governing Scope

- Canonical remaining-S0 plan: `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md`
  (§8 — S0.6 Security Tool Admission).
- Enacted policy: `SECURITY.md`, `docs/security/SECURITY_ASSURANCE_POLICY.md`,
  `docs/security/DEPENDENCY_ADMISSION.md`, `AGENTS.md` (§8 security requirements).
- Enacted licensing governance: `docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md`.
- Research packet consulted (unmodified by this document):
  `docs/security/S0_6_SAST_ADMISSION_RESEARCH.md`. That packet made **no tool recommendation and
  no admission decision**; this document records the human admission decision and subsequent
  implementation evidence separately, preserving that boundary.

## 3. Starting State

- **Commit:** `171fb1a22c69d25a1f8c93eda5f19cc3a05a756d` (`docs: enact third-party licensing
  governance`); `HEAD == origin/main` at the start of this pass; working tree clean; nothing
  staged.
- **Accepted security state:** S0.1–S0.5 COMPLETE / ACCEPTED; S0.6 Capability Reviews 1–3
  COMPLETE / ACCEPTED; S0.6 Capability Review 4 (this document) research complete, owner decision
  now recorded; `S0.3-G006` UNDER CAPABILITY REVIEW at start of this pass; S0.7/S0.8 NOT STARTED;
  Stage 9 blocked pending S0 closeout.
- **Finding-ID integrity:** the highest previously assigned S0.6 finding ID is `S0.6-F019`
  (`docs/security/S0_6_SBOM_ADMISSION.md` §9). Any new Capability Review 4 finding begins at
  `S0.6-F020`.

## 4. Owner Admission Decision

The project owner independently reviewed the Capability Review 4 SAST research
(`docs/security/S0_6_SAST_ADMISSION_RESEARCH.md`) under the enacted Third-Party Software &
Licensing Governance Policy and made the following explicit human decision on 2026-08-27:

### 4.1 Microsoft DevSkim CLI v1.0.90 — ADMITTED

> **Microsoft DevSkim CLI v1.0.90 ADMITTED for installation and verification — 2026-08-27.**
>
> Purpose: dedicated local static security analysis for `S0.3-G006`.
>
> Classification: developer-only security tooling.
>
> Admitted use: local static security linting against KST source using the exact v1.0.90
> bundled/default rule corpus.
>
> Known capability boundary (recorded at admission time): DevSkim uses security-linting
> rules/pattern matching and does not establish deep cross-file semantic/interprocedural
> taint-analysis coverage. This admission is intentionally bounded: standalone CLI; local source
> analysis; bundled/default Microsoft DevSkim rules; no IDE extension; no cloud service; no
> custom rule pack; no suppression/baseline; no automatic fixes; no CI integration.

### 4.2 Semgrep CE v1.175.0 — DEFERRED

> Requires organizational licensing review under the enacted licensing governance because the
> reviewed engine/rules licensing model triggers the project's escalation path. Not a rejection.

### 4.3 CodeQL CLI v2.26.4 — DEFERRED

> Requires confirmation of an applicable private-repository GitHub security entitlement and
> organizational authorization under the enacted licensing governance. Not a rejection.

Neither deferred candidate is rejected; both remain valid future candidates pending the
identified organizational review steps. Their license terms are not independently reinterpreted
here — the committed research packet and enacted licensing governance are the decision evidence.

## 5. Pre-Installation Status

At the time this document was first committed, no SAST tool had yet been installed or executed.
Implementation (licensing-gate verification, package acquisition, signature verification,
installation, rule-corpus verification, synthetic validation, and the local KST scan) proceeds in
a subsequent pass and is recorded below without modifying this section's admission-decision
record.

`S0.3-G006` disposition at this point: **UNDER IMPLEMENTATION** (not resolved).

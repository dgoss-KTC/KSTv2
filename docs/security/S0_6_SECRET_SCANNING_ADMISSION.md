# S0.6 — Security Tool Admission: Capability Review 2 — Dedicated Secret Scanning

**S0.6 Capability Review 2 — Dedicated Secret Scanning**
**Status: OWNER DECISION RECORDED / INSTALLATION AUTHORIZED / IMPLEMENTATION PENDING**

| Item | Value |
|---|---|
| Capability | Dedicated local secret detection |
| Gap | `S0.3-G007` |
| Tool | Gitleaks v8.30.0 |
| Owner admission decision | ADMITTED for installation and verification — 2026-08-27 |
| Implementation | PENDING |
| Project-owner acceptance | NOT YET ACCEPTED |
| Overall S0.6 status | **IN PROGRESS** (this review closes one capability only; S0.6 as a whole is **not** complete) |

This document is **evidence, not normative policy**. It records the S0.6 Capability Review 2
owner admission decision and (as implementation proceeds) installation, verification, and scan
evidence for the dedicated secret-scanning capability (accepted S0.3 gap `S0.3-G007`). Required
security properties and tool-admission governance remain defined by `SECURITY.md`,
`docs/security/SECURITY_ASSURANCE_POLICY.md`, and `docs/security/DEPENDENCY_ADMISSION.md`. This
document is separate from, and does not modify, the neutral research packet at
`docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md`.

---

## 1. Purpose and Status

S0.6 evaluates missing security-tool capabilities **one at a time** under the enacted
dependency-admission process (`docs/security/DEPENDENCY_ADMISSION.md`), per the accepted
remaining-S0 plan (`docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §8).

Capability Review 2 addresses:

> **S0.3-G007** — no dedicated local secret-detection scanner for current repository content
> and Git history (accepted S0.3 evidence).

Capability Review 1 (Rust dependency advisories, `S0.3-G001`) is separately COMPLETE / ACCEPTED
— see `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`. This document does not modify that
evidence.

## 2. Governing Scope

- Canonical remaining-S0 plan: `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md`
  (§8 — S0.6 Security Tool Admission).
- Enacted policy: `SECURITY.md`, `docs/security/SECURITY_ASSURANCE_POLICY.md`,
  `docs/security/DEPENDENCY_ADMISSION.md`, `AGENTS.md` (§8 security requirements).
- Research packet consulted (unmodified by this document):
  `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md`. That packet made **no tool
  recommendation and no admission decision**; this document records the human admission
  decision and subsequent implementation evidence separately, preserving that boundary.

## 3. Starting State

- **Commit:** `2ca60f38335061223a32235c20cddf8616f7de99` (`Updated AGENTS.md to address path
  formatting issues during generation.`); `HEAD == origin/main` at the time this document was
  authored.
- **Accepted security state:** S0.1–S0.5 COMPLETE / ACCEPTED; S0.6 Capability Review 1
  COMPLETE / ACCEPTED; S0.6 Capability Review 2 (this document) research complete, owner
  decision now recorded; G006/G008 NOT STARTED; S0.7/S0.8 NOT STARTED; Stage 9 blocked pending
  S0 closeout.
- **Machine state (at owner-decision time):** no dedicated secret scanner installed on the
  workstation (confirmed in the accepted S0.3 tool-availability pass and the Capability Review
  2 research packet).

## 4. Owner Admission Decision

The project owner reviewed the independent Capability Review 2 research and made the following
explicit human decision on 2026-08-27:

### 4.1 Gitleaks v8.30.0 — ADMITTED

> **Gitleaks v8.30.0 ADMITTED for installation and verification — 2026-08-27.**
>
> Purpose: dedicated local secret detection for current KST repository content and Git history
> under `S0.3-G007`.

### 4.2 Gitleaks v8.30.1 — DEFERRED

> The v8.30.1 release has an unresolved upstream release-provenance defect: its tag-defining
> commit is diverged from the normal master lineage and the maintainer acknowledged the release
> mistake. No clean successor release was available during the 2026-08-27 review.

This is not a statement that v8.30.1 is malicious or defective as a scanner — it is a
provenance/release-process deferral.

### 4.3 TruffleHog v3.97.1 — DEFERRED

> It is a credible secret-scanning capability, but its broader verified-secret/provider-
> interaction model introduces additional external-network and credential-verification trust
> surface not required to close G007.

### 4.4 detect-secrets v1.5.0 — DEFERRED

> Its baseline/pre-commit-oriented design is useful but is less directly aligned with KST's
> requirement for straightforward current-content plus complete Git-history scanning.

None of the deferred candidates (v8.30.1, TruffleHog, detect-secrets) are rejected; they remain
valid future candidates.

## 5. Admitted Operating Boundary

The admitted capability is **local secret detection** using Gitleaks v8.30.0 against current
repository content and Git history.

The admitted capability is explicitly **not**:

- credential validity verification;
- remote scanning;
- source upload;
- GitHub secret scanning (the hosted service);
- pre-commit enforcement;
- CI enforcement;
- automatic remediation.

Gitleaks must operate locally against repository data. No KST repository content or detected
value may be intentionally sent to an external scanning service.

## 6. Maintenance Observation

Recorded as an observation, not a blocking risk:

> Gitleaks v8 is feature-complete and expected to receive security fixes rather than ongoing
> feature development. Betterleaks has been named by the upstream maintainer as a
> successor/future focus but was not evaluated under this checkpoint.

Future review triggers include: a corrected post-v8.30.1 Gitleaks release; Gitleaks archival;
security-fix support cessation; material Windows-support change; material successor transition.
Betterleaks is not evaluated now.

## 7. Implementation Evidence

_Populated during installation and verification. Until then, the sections below remain
placeholders reflecting PENDING status._

### 7.1 Release Integrity Verification

_PENDING — to be recorded after download and checksum verification._

### 7.2 Installation

_PENDING — to be recorded after installation._

### 7.3 Synthetic Canary Verification

_PENDING — to be recorded after canary tests A (current-file) and B (Git-history)._

### 7.4 Current-Content KST Scan

_PENDING._

### 7.5 Git-History KST Scan

_PENDING._

### 7.6 Structured-Output Verification

_PENDING._

### 7.7 Network/Data-Handling Observation

_PENDING._

### 7.8 Repository-Integrity Verification

_PENDING._

### 7.9 Findings

_PENDING._

### 7.10 Trust Limitations

_PENDING._

## 8. S0.3-G007 Disposition

**Status: NOT YET RESOLVED.** `S0.3-G007` remains open until implementation and verification
complete and the project owner accepts the implemented capability. This document will be
updated to **Capability Implemented / Awaiting Project-Owner Acceptance** upon successful
installation and verification, and only marked resolved after explicit owner acceptance.

**Working principle:** the project owner has admitted one narrowly bounded security
capability — Gitleaks v8.30.0 may be installed and used locally to detect likely secrets in
KST's current repository content and Git history. The admission does not authorize proving
that discovered credentials still work, sending them externally, suppressing them, rewriting
history, or automatically remediating them.

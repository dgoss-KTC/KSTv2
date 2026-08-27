# S0.6 — Security Tool Admission: Capability Review 2 — Dedicated Secret Scanning

**S0.6 Capability Review 2 — Dedicated Secret Scanning**
**Status: IMPLEMENTED / AWAITING PROJECT-OWNER ACCEPTANCE**

| Item | Value |
|---|---|
| Capability | Dedicated local secret detection |
| Gap | `S0.3-G007` |
| Tool | Gitleaks v8.30.0 |
| Owner admission decision | ADMITTED for installation and verification — 2026-08-27 |
| Implementation | COMPLETE — installed, verified, current-content and Git-history scans run |
| Project-owner acceptance | NOT YET ACCEPTED |
| Overall S0.6 status | **IN PROGRESS** (this review closes one capability only; S0.6 as a whole is **not** complete) |
| S0.3-G007 disposition | **Capability Implemented / Awaiting Project-Owner Acceptance** |

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

### 7.1 Release Integrity Verification

| Item | Value |
|---|---|
| Official release | `v8.30.0`, published 2025-11-26T16:31:23Z, `gitleaks/gitleaks` |
| Release/tag-defining commit | `6eaad039603a4de39fddd1cf5f727391efe9974e` (matches independently recorded evidence) |
| Commit signature | GPG-signed, GitHub API reports `verified: true`, `reason: valid` |
| Annotated tag object signature | Unsigned (`reason: unsigned`) — expected; the tag object itself carries no independent GPG signature, only the underlying commit does |
| Windows x64 asset | `gitleaks_8.30.0_windows_x64.zip` |
| Official checksum-file entry (SHA-256) | `54fe94f644b832dd08e8c3a5915efb3bfa862386d59fb27ca0792cb687a83573` |
| GitHub asset digest (SHA-256) | `sha256:54fe94f644b832dd08e8c3a5915efb3bfa862386d59fb27ca0792cb687a83573` |
| Locally computed SHA-256 | `54fe94f644b832dd08e8c3a5915efb3bfa862386d59fb27ca0792cb687a83573` |
| Comparison result | **All three sources match exactly** |
| Checksum-file integrity | `gitleaks_8.30.0_checksums.txt` GitHub asset digest `sha256:78e53de2429bde6500a6f22793546babe6ae75634a0c250c37e3a07703856a90` matches locally computed SHA-256 of the downloaded checksum file |

No mismatch was observed. The download was not quarantined/deleted for integrity reasons.

### 7.2 Installation

| Item | Value |
|---|---|
| Exact version | 8.30.0 (confirmed via `gitleaks.exe version`) |
| Absolute install path | `%LOCALAPPDATA%\KST\SecurityTools\gitleaks\8.30.0\gitleaks.exe` |
| Admin elevation | No |
| PATH modified | No — invoked via absolute path only |
| Machine-wide install | No — user-local only |
| Retained alongside binary | `gitleaks_8.30.0_checksums.txt` (provenance), `LICENSE`, `README.md` |

### 7.3 Synthetic Canary Verification

| Test | Result |
|---|---|
| Canary A — current-file detection | **PASS** — rule `generic-api-key` fired against a locally generated 64-hex-character random value (never issued by any provider); 100% redaction confirmed; nonzero exit code (2) |
| Canary B — Git-history (deleted-material) detection | **PASS** — a synthetic value was committed then removed from HEAD in a disposable local repository outside KST; `gitleaks git --log-opts="--all"` detected the historical finding, correctly attributed to the introducing commit, while the working tree/HEAD remained clean |

No canary value is recorded in this document. Both disposable directories/repositories and
their JSON reports were deleted after verification.

### 7.4 Current-Content KST Scan

| Item | Value |
|---|---|
| Command | `gitleaks.exe dir . --redact=100 -f json -r <report> --exit-code 2` (executed from repository root) |
| Rules | Default built-in rules only; no custom config, baseline, allowlist, or suppression |
| Exit code | 2 (leaks found) |
| Finding count | 4 |

### 7.5 Git-History KST Scan

| Item | Value |
|---|---|
| Command | `gitleaks.exe git <repo-path> --log-opts="--all" --redact=100 -f json -r <report> --exit-code 2` |
| Ref scope | All local refs reachable via `git log --all` in this worktree |
| Commits scanned | 44 |
| Exit code | 2 (leaks found) |
| Finding count | 8 |

### 7.6 Structured-Output Verification

| Format | Result |
|---|---|
| JSON | Verified valid; parsed correctly; finding count consistent with report |
| SARIF | Verified valid SARIF 2.1.0; `runs[0].results` count (4) consistent with the JSON current-content finding count |
| Report disposition | All JSON/SARIF reports were written to a temporary directory outside the repository and deleted after redacted metadata was extracted into this document; none were committed or uploaded |

### 7.7 Network/Data-Handling Observation

Observed behavior during scanning: Gitleaks operated entirely against local repository data
(current working tree and local `.git` history); no intentional source upload, rule-download
prompt, telemetry transmission, or provider-verification request was observed in tool output.
This is an operational observation from tool output and documented behavior, not packet-level
forensic proof. No live credential verification was performed at any point.

### 7.8 Repository-Integrity Verification

`git status --short` and `git diff --name-status`/`git diff --stat` were run before and after
all scans. The KST working tree remained clean (no scanner-created or scanner-modified
repository files) throughout installation, canary testing, and both KST scans.

### 7.9 Findings

All findings across both the current-content and Git-history scans are the **same
documentation content**: literal PEM private-key block header sentinel strings (e.g.
`-----BEGIN PRIVATE KEY-----`, `-----BEGIN RSA PRIVATE KEY-----`, etc.) quoted as prose inside
two security-documentation files that describe which sentinel patterns an earlier manual
`git grep` search used. These are not private-key material: there is no key body, no
base64-encoded payload, and entropy is low (~3.6–3.7) and consistent with plain documentation
text rather than random key content.

**Current-content scan (4 findings):**

| Finding | Rule | File | Line(s) |
|---|---|---|---|
| S0.6-F002 | `private-key` | `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` | 330–331 |
| S0.6-F003 | `private-key` | `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` | 332–333 |
| S0.6-F004 | `private-key` | `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` | 108–109 |
| S0.6-F005 | `private-key` | `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` | 110–111 |

**Git-history scan (8 findings — same two documentation locations, present in 4 historical
commits since each file was introduced/amended):**

| Finding | Rule | File | Commit |
|---|---|---|---|
| S0.6-F006 | `private-key` | `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` (lines 108–109) | `1f14fa8d7c87a3db397c99c0a9dd4f7d52df57ee` |
| S0.6-F007 | `private-key` | `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` (lines 110–111) | `1f14fa8d7c87a3db397c99c0a9dd4f7d52df57ee` |
| S0.6-F008 | `private-key` | `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` (lines 108–109) | `ea61453402dc9296c246b28f50ba170963456c7c` |
| S0.6-F009 | `private-key` | `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` (lines 110–111) | `ea61453402dc9296c246b28f50ba170963456c7c` |
| S0.6-F010 | `private-key` | `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (lines 330–331) | `2760478a25f68b2927d2b2d55b7d7e4e638a439e` |
| S0.6-F011 | `private-key` | `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (lines 332–333) | `2760478a25f68b2927d2b2d55b7d7e4e638a439e` |
| S0.6-F012 | `private-key` | `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (lines 330–331) | `29141d2789646d5fe00894df5fe7200161e8fe77` |
| S0.6-F013 | `private-key` | `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (lines 332–333) | `29141d2789646d5fe00894df5fe7200161e8fe77` |

**Disposition of S0.6-F002 through S0.6-F013:** Informational / documentation-text false
positive. No secret value was reproduced in this document, chat, or elsewhere. No KST severity
was assigned; no Accepted Risk was recorded; no suppression, allowlist, baseline, or
`.gitleaksignore` was added; no remediation, edit, or history rewrite was performed. Final
disposition (including whether any wording adjustment is warranted to reduce future false
positives) is deferred to project-owner review.

No finding plausibly represents a real current or historical credential.

### 7.10 Trust Limitations

- Gitleaks' rule-matching is pattern/entropy-based; it can produce false positives (as observed
  here) and, in principle, false negatives for secret formats not covered by its default rule
  set.
- The scan covered refs reachable via `git log --all` in this local worktree/clone; it does not
  independently prove completeness against every ref that might exist on the remote or in other
  clones/worktrees.
- No live credential verification was performed or authorized; a clean scan result is a
  detection-tool observation, not proof that KST is free of undisclosed secrets.
- The annotated release tag object is unsigned; provenance trust for this admission rests on
  the underlying release-defining commit's verified GPG signature plus matching checksums from
  two independent GitHub-hosted sources (checksum file and asset digest) plus local
  recomputation.

## 8. S0.3-G007 Disposition

**Status: Capability Implemented / Awaiting Project-Owner Acceptance.**

Basis: Gitleaks v8.30.0 was explicitly owner-admitted, installed at the exact pinned version in
a user-local versioned directory, integrity-verified via three independent checksum sources,
version-confirmed, and proven via synthetic canaries to detect both current-file and
historical-deleted secrets before any KST scan was trusted. Current-content and Git-history
scans of KST were completed using default rules only, with 100% redaction and reports kept
outside the repository. All findings were documentation-text false positives, recorded above
without secret-value reproduction, severity assignment, suppression, or remediation. The
repository working tree was verified clean before and after all operations.

`S0.3-G007` is **not yet resolved**; it remains **Capability Implemented / Awaiting
Project-Owner Acceptance** until the project owner explicitly accepts this implementation.

**Working principle:** the project owner has admitted one narrowly bounded security
capability — Gitleaks v8.30.0 may be installed and used locally to detect likely secrets in
KST's current repository content and Git history. The admission does not authorize proving
that discovered credentials still work, sending them externally, suppressing them, rewriting
history, or automatically remediating them.

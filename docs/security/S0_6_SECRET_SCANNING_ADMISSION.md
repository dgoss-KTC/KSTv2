# S0.6 — Security Tool Admission: Capability Review 2 — Dedicated Secret Scanning

**S0.6 Capability Review 2 — Dedicated Secret Scanning**
**Status: IMPLEMENTED / AWAITING PROJECT-OWNER ACCEPTANCE**

| Item | Value |
|---|---|
| Capability | Dedicated local secret detection |
| Gap | `S0.3-G007` |
| Tool | Gitleaks v8.30.0 |
| Owner admission decision | ADMITTED for installation and verification — 2026-08-27 |
| Implementation | COMPLETE — 2026-08-27 (see §7) |
| Project-owner acceptance | NOT YET ACCEPTED — awaiting explicit owner review of the implementation evidence below |
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

Recorded 2026-08-27 against starting commit `ea61453402dc9296c246b28f50ba170963456c7c`
(`docs: admit Gitleaks secret scanning capability`), the commit at which the owner admission
decision above was recorded.

### 7.1 Release Integrity Verification

| Item | Value |
|---|---|
| Release | Gitleaks v8.30.0 |
| Release date | 2025-11-26 |
| Tag-target commit | `6eaad039603a4de39fddd1cf5f727391efe9974e` |
| Commit signature | GPG-signed, verified by GitHub (`verified: true`, `reason: valid`) |
| Annotated tag object | **unsigned** (`verified: false`, `reason: unsigned`) |
| Windows asset | `gitleaks_8.30.0_windows_x64.zip` |
| Official checksum (release `gitleaks_8.30.0_checksums.txt`) | `54fe94f644b832dd08e8c3a5915efb3bfa862386d59fb27ca0792cb687a83573` |
| GitHub release-asset digest (via GitHub API) | `sha256:54fe94f644b832dd08e8c3a5915efb3bfa862386d59fb27ca0792cb687a83573` — matches |
| Locally retained checksums file | Present at the installation path and matches both values above |

The annotated tag object itself is not signed; only the underlying commit carries a verified
signature. This distinction is preserved deliberately and is not overstated. The original
release `.zip` was already extracted at install time and is no longer present in this session to
independently recompute a fresh local hash of the archive itself; the retained checksums file
and the independently-fetched GitHub asset digest were cross-checked instead and agree.

These are preserved as separate pieces of evidence (release checksum file, GitHub asset digest,
commit signature) rather than conflated into a single claim.

### 7.2 Installation

Confirmed present at the accepted, admitted path:

```text
%LOCALAPPDATA%\KST\SecurityTools\gitleaks\8.30.0\gitleaks.exe
```

Properties confirmed: user-local (under the current Windows user's `LOCALAPPDATA`),
version-pinned (`8.30.0` directory), not on `PATH` (invoked in this evidence pass via its
absolute path only), no repository dependency (no reference added to any KST project/build
file). `gitleaks version` reports `8.30.0`.

### 7.3 Synthetic Canary Verification

Both required pre-KST checks were performed against a disposable temporary Git repository
(created under the OS temp directory, unrelated to the KST repository, and deleted after the
test):

| Check | Result |
|---|---|
| Current-file synthetic canary | **PASS** — detected under rule `generic-api-key`, exit code `1` (leaks found) |
| Historical/deleted synthetic canary | **PASS** — removed from current content (fresh scan: no leaks, exit code `0`); full-history scan still detected it (rule `generic-api-key`, exit code `1`) |

A synthetic, non-functional API-key-shaped string (not a real credential, not reused anywhere)
was committed to a disposable temporary Git repository (created under the OS temp directory,
unrelated to the KST repository). A `gitleaks dir` scan of that repo detected it, with output
redaction enabled (`--redact`, no full match value retained in this evidence). The string was
then removed in a follow-up commit; a fresh `gitleaks dir` scan of the working tree found no
leaks, confirming it was gone from current content, while a `gitleaks git` scan of the same
disposable repository's full history still detected it in the earlier historical commit. The
disposable repository was deleted after the test.

No real credential was used at any point. No canary value is retained in this document or in
any other repository evidence; only the rule name and pass/fail outcome are recorded.

### 7.4 Current-Content KST Scan

| Item | Value |
|---|---|
| Mode | `gitleaks dir` (current working-tree content), default rules |
| Redaction | 100% (`--redact`) |
| Output | JSON, written to a temporary file and deleted after evidence extraction |
| Exit code | `1` (leaks found; Gitleaks v8.30.0 defaults `--exit-code` to `1`) |
| Finding count | 4 |
| Rule | `private-key` |
| Locations | `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (2 matches), `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` (2 matches) |

Gitleaks reported four current-content matches to the `private-key` rule. Review established
that the matches were literal PEM-header sentinel strings (e.g. `-----BEGIN PRIVATE KEY-----`
style markers) quoted as prose in security documentation, listing the patterns an earlier
manual `git grep` sentinel check searched for. They did not include private-key bodies or
credential material. The repository does **not** contain four private keys.

### 7.5 Git-History KST Scan

| Item | Value |
|---|---|
| Mode | `gitleaks git`, `--log-opts="--all"` (full history across all refs) |
| Commits scanned | 47 on this session's working branch |
| Exit code | `1` (leaks found) |
| Finding count | 8 |
| Rule | `private-key` |

The commit count differs from the 44 recorded for the expected starting branch
(`agents/pasted-text-processing`) in §1 only because this worktree's branch carries additional
session-management checkpoint commits; it does not reflect any application, dependency, or
security-relevant change.

These eight findings resolve to the same two documentation locations and the same intentional,
PEM-header sentinel content described in §7.4 — not historical credential leaks.

### 7.6 Structured-Output Verification

| Item | Value |
|---|---|
| JSON | Verified — parsed successfully as a JSON array of finding objects |
| SARIF | Verified — `$schema` resolves to `https://json.schemastore.org/sarif-2.1.0.json`, `version: 2.1.0` |
| Temporary scan reports | Written to the OS temp directory during evidence collection and deleted after extraction |
| Raw scanner output committed to the repository | No |

### 7.7 Network/Data-Handling Observation

Gitleaks operated entirely locally against the local KST working tree and local Git object
database (via absolute-path invocation). No intentional upload of repository source or scan
results to any external service occurred during this evidence pass. This is an observational
statement about the tool's local operating mode, not a packet-level forensic proof, and it does
not establish credential-validity verification — Gitleaks's admitted capability is **detection**,
not verification that any matched string is a live, working credential.

### 7.8 Repository-Integrity Verification

Running Gitleaks did not mutate the KST repository. No source, dependency, test,
configuration, hook, CI, baseline, or suppression file was created or changed by the scans
themselves. All temporary scan-report files were created outside the repository (OS temp
directory) and removed after evidence extraction.

### 7.9 Findings

`S0.6-F002` through `S0.6-F013` (twelve findings total: 4 current-content + 8 history, all rule
`private-key`) are dispositioned as:

> Informational / Confirmed Documentation False Positive

Rationale: all matches use the `private-key` detector; all resolve to two security-documentation
locations; matched text consists of literal PEM-header sentinel strings quoted as prose; no
private-key body is present; no credential material was identified; this is expected detector
behavior against sentinel text used to document what a manual scan looks for.

No `.gitleaksignore`, no `gitleaks.toml`, no allowlist, and no baseline were created, and the
underlying documentation was not modified merely to silence the scanner. Disposition: retain as
known informational detector matches; reassess suppression only if repeated operational
scanning creates sufficient burden to justify a separately reviewed suppression-control
decision. These are not classified as Accepted Risk.

### 7.10 Trust Limitations

The admitted capability is local secret **detection** against current repository content and
Git history using Gitleaks v8.30.0's default rule set. It is explicitly not: credential-validity
verification, remote/hosted scanning, pre-commit enforcement, CI enforcement, or automatic
remediation. A "no findings" result is bounded by Gitleaks's default rule coverage and does not
constitute a guarantee that no secret exists in any form the default rules do not target.

## 8. S0.3-G007 Disposition

**Status: Capability Implemented / Awaiting Project-Owner Acceptance.** The dedicated
secret-detection capability described above (Gitleaks v8.30.0, installed, release-integrity
verified, synthetic-canary verified, and run against current KST content and full Git history)
has been implemented and is recorded here as evidence. `S0.3-G007` is **not yet marked
resolved** — that determination, and formal acceptance of Capability Review 2 as
COMPLETE/ACCEPTED, requires explicit project-owner review of the evidence in §7 and is not
self-certified by this implementation pass.

**Working principle:** the project owner has admitted one narrowly bounded security
capability — Gitleaks v8.30.0 may be installed and used locally to detect likely secrets in
KST's current repository content and Git history. The admission does not authorize proving
that discovered credentials still work, sending them externally, suppressing them, rewriting
history, or automatically remediating them.

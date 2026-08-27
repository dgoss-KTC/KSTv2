# S0.6 Capability Review 2 — Dedicated Secret Scanning

**Status: RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW**
**NO TOOL RECOMMENDATION OR ADMISSION DECISION**

| Item | Value |
|---|---|
| Gap addressed | `S0.3-G007` — dedicated secret scanning |
| Research date | 2026-08-27 |
| Starting commit | `2ca60f38335061223a32235c20cddf8616f7de99` (subject: "Updated AGENTS.md to address path formatting issues during generation.") |
| Overall S0.6 status | **IN PROGRESS** (Capability Review 1 — Rust Dependency Advisory Capability — is COMPLETE / ACCEPTED; this review closes no capability by itself) |

This document is **research evidence only**. It does **not** recommend or admit a secret-scanning
tool. It does not install, download, or execute any candidate scanner. The actual admission
decision belongs to the project owner, informed by an independent review of this packet.

---

## 1. Purpose and Authority Boundary

> This document provides research evidence only. It does not recommend or admit a secret-scanning
> tool.

Per the project owner's explicit authorization, three roles are intentionally separated for this
capability review:

- **Research agent (this document):** collect and organize evidence.
- **Independent review:** compare the evidence and formulate a recommendation.
- **Project owner:** make the actual admission decision.

No `ADMIT`/`DEFER`/`REJECT` disposition, preferred candidate, or "winner" is stated anywhere in
this document.

## 2. Governing Authority

- `AGENTS.md` (Tier 1 — enacted repository rules).
- `SECURITY.md` (Tier 1 — enacted security policy entry point).
- `docs/security/SECURITY_ASSURANCE_POLICY.md`, `docs/security/DEPENDENCY_ADMISSION.md`,
  `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`, `docs/security/AI_SECURITY_REVIEW.md`,
  `docs/security/APPLICATION_SECURITY_PROFILE.md` (Tier 1 normative policy documents).
- `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §8 (S0.6 — Security Tool
  Admission): approved active planning (Tier 4) establishing the one-capability-at-a-time process
  and the six-step admission sequence (define need → determine native sufficiency → evaluate
  candidates → document trust/supply-chain implications → obtain human approval before
  installation → integrate and record).
- `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (Tier 3 — accepted implementation
  evidence): source of gap `S0.3-G007` and the existing sentinel-search baseline.
- `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md` (Tier 3 — accepted Capability Review 1
  evidence, used here only as a structural/process template; not modified).
- `docs/status/CURRENT_PROJECT_STATUS.md` and `KST-v2-Master-Project-Checklist.md` (Tier 2 —
  accepted current project state).

**Repository Observation:** the remaining-S0 plan does not define permanent `S0.6A`/`S0.6B`-style
labels for individual capability reviews; it lists the four gaps (`G001`, `G006`, `G007`, `G008`)
under one S0.6 heading. Per the task instructions, this document therefore uses the working
description **"S0.6 — Capability Review 2: Dedicated Secret Scanning"** only, consistent with how
Capability Review 1 was named in `SECURITY.md` and `S0_6_RUST_DEPENDENCY_ADMISSION.md`. No
canonical roadmap numbering was changed.

**Repository Observation — one-capability-at-a-time / research-recommendation separation
confirmed:** `KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §8 states S0.6 evaluates missing
security-tool capabilities "one at a time" and requires human approval **before** installation.
This mirrors the Capability Review 1 pattern already accepted in
`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`.

## 3. Starting Repository State

- **Branch:** `agents/pasted-text-processing` (this is a session/worktree branch of the KST v2
  repository, not `main`). **Repository Observation:** the branch name differs from the
  `main`-branch expectation in the task template; however, the branch's `HEAD` commit
  (`2ca60f38335061223a32235c20cddf8616f7de99`) is byte-identical to `origin/main`'s current commit
  (`git rev-parse origin/main` also resolves to `2ca60f38335061223a32235c20cddf8616f7de99`), so no
  content divergence exists between this session and `origin/main`. This is recorded transparently
  rather than silently reconciled, per `AGENTS.md` §1.
- **Working tree:** clean at the start of this pass (`git status --short` returned no output).
- **Recent history (`git log -5 --oneline`):**
  - `2ca60f3` — Updated AGENTS.md to address path formatting issues during generation.
  - `fabdcf4` — docs: restore Rust advisory admission evidence
  - `94022fc` — docs: accept Rust dependency advisory capability
  - `f6d09b9` — test: add security architecture regression checks
  - `f784c83` — chore: remediate npm development advisories
- The two commits the task instructions asked to confirm are present
  (`94022fcf26a99ab0a0752979cde86dcc076497b8` — "docs: accept Rust dependency advisory capability"
  and `fabdcf4477e15454f1c2109760463ed586565d61` — "docs: restore Rust advisory admission
  evidence"), plus the subsequent `AGENTS.md` commit (`2ca60f3`).
- **`SECURITY.md` accepted status at start:** S0.1–S0.5 COMPLETE/ACCEPTED; S0.6 IN PROGRESS with
  Capability Review 1 (`S0.3-G001`, cargo-audit 0.22.2) COMPLETE/ACCEPTED — 2026-08-26; remaining
  S0.6 capability reviews (G006, G007, G008) NOT STARTED; Stage 9 blocked pending S0 closeout.

## 4. S0.3-G007 Gap

From accepted S0.3 evidence (`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §11, §12):

> `S0.3-G007` — **Dedicated secret scanning:** no secret scanner exists; only the limited
> high-confidence sentinel search of §8 was possible. Historical commits were not scanned.

This is a **capability gap**, not a verification gap: no dedicated secret-scanning tool is
currently authorized or present in the KST development environment.

## 5. Existing KST Secret-Detection Capability

Recovered from `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §8, §12 and repository
inspection performed for this review:

**What exists today (native/manual, not a dedicated scanner):**

- `git grep -l` sentinel search, limited to a small set of high-confidence, literal patterns:
  PEM-style private-key block headers (`-----BEGIN PRIVATE KEY-----`,
  `-----BEGIN RSA PRIVATE KEY-----`, `-----BEGIN OPENSSH PRIVATE KEY-----`,
  `-----BEGIN EC PRIVATE KEY-----`, `-----BEGIN DSA PRIVATE KEY-----`,
  `-----BEGIN PGP PRIVATE KEY-----`).
- A check for tracked files with key/certificate extensions (`.pem`, `.pfx`, `.p12`, `.key`,
  `.cer`, `.crt`, `.keystore`, `.jks`).
- A check for tracked files matching credential-bearing naming conventions
  (`credentials*`, `.env`, local/secrets `appsettings` patterns).
- `.gitignore` conventions that exclude local secret-override files from being tracked at all:
  `**/appsettings.*.local.json` and `**/*.secrets.json` (**Repository Observation**, confirmed by
  direct inspection of `.gitignore` lines 35–37 during this review).
- All of the above were **path-only** outputs (no matched content was printed), scoped to
  **currently tracked files only**.

**Known limitations of the existing mechanism (Repository Observation, from S0.3 §8/§12 and this
review):**

- No provider-specific token recognition (e.g., AWS access keys, GitHub PATs, Slack tokens,
  Stripe keys) — only literal private-key headers and filename/extension conventions.
- No entropy-based detection (high-entropy strings embedded in code, config, or connection
  strings would not be caught).
- No Git history scanning — secrets committed and later removed would not be detected.
- No staged-content/pre-commit inspection capability exists or is configured.
- Not repeatable as a structured, machine-readable check (ad hoc `git grep` invocations, not a
  scripted/CI-integrated tool).
- No automatic developer-facing execution (no pre-commit hook, no CI job).
- `command -v`/`Get-Command` presence checks in S0.3 (§12) found no dedicated secret scanner
  (`gitleaks`) installed on the development workstation used for that pass.

**Confirmed for this review (read-only, no values reproduced):** re-running equivalent
presence checks (`Get-Command gitleaks`, `Get-Command trufflehog`, `Get-Command detect-secrets`)
in the current session's environment found none of the three candidates below installed. No
scan was executed and no repository content was searched for secret values in this pass beyond
what S0.3 already recorded.

The distinction the task requires is preserved: **manual/native repository search** (the above)
is **not** equivalent to a **dedicated secret-scanning capability** (the subject of this review).

## 6. Capability Requirements (factual, unweighted)

Derived from accepted KST policy/architecture, without selecting a tool:

- **Current repository content:** ability to detect secret-like values in tracked/current source.
- **Git history:** ability to detect secrets that were committed and later removed (the sentinel
  search in §5 explicitly did not cover this).
- **Future staged/pre-commit inspection:** a potential future developer guard (not implemented by
  this review; hooks are out of scope here).
- **Local execution:** KST source and any findings should remain on the developer machine unless
  separately authorized, consistent with KST's general posture of not uploading source or
  findings to third parties (`SECURITY.md`, `DEVELOPMENT_ENVIRONMENT_SECURITY.md`).
- **Credential safety:** a scanner should not unnecessarily transmit candidate credential values
  to external services.
- **Structured evidence:** machine-readable output (JSON/SARIF/etc.) useful for a future
  repeatable check.
- **Windows support:** KST development is Windows-based (this session runs on Windows with
  PowerShell); a candidate must have a genuine Windows-supported execution path.
- **Reproducible installation:** exact-version pinning and a controlled update/removal path,
  consistent with the admission pattern already used for `cargo-audit` in Capability Review 1.

These are stated as factual requirements only; no weighting or scoring is applied.

## 7. Research Method / Source Classification

Every substantive claim below is labeled as one of: **Repository Observation**, **Public Upstream
Evidence**, **Inference**, or **Unable to Verify**. Public Upstream Evidence is drawn from each
project's own GitHub repository (README, `SECURITY.md`, `LICENSE`, release metadata via the
GitHub REST API, and default configuration files) — no blogs, comparison articles, or third-party
summaries were used as a primary source. All public queries used generic tool/technology terms
only (e.g., "gitleaks current release Windows SARIF documentation"); no KST source, filenames,
hostnames, database names, or internal details were sent externally. Research date: **2026-08-27**.

## 8. Candidate Set

Per the task's required minimum set and the third-candidate justification test:

- **Gitleaks** (`gitleaks/gitleaks`).
- **TruffleHog** (`trufflesecurity/trufflehog`).
- **detect-secrets** (`Yelp/detect-secrets`) — included as the third candidate because it
  represents a materially different approach: a Python package (not a standalone Go binary), built
  around an explicit **baseline file** workflow and a pluggable detector model, historically aimed
  at pre-commit prevention rather than ad hoc/CI scanning or live credential verification. This
  contrasts meaningfully with both Gitleaks (regex/entropy, Go binary, git-history-native) and
  TruffleHog (detector + live-verification model, Go binary). No further candidates were evaluated
  — broader SAST platforms with incidental secret rules were intentionally excluded per task scope
  (G007 is about a *dedicated* secret scanner, not SAST, which is `S0.3-G006`, out of scope here).

## 9. Gitleaks Evidence

| Attribute | Value | Source |
|---|---|---|
| Current stable version (at research date) | `v8.30.1` | Public Upstream Evidence — GitHub Releases API (`api.github.com/repos/gitleaks/gitleaks/releases/latest`) |
| Release date | Published 2026-03-21 (created 2026-03-12) | Public Upstream Evidence — same release metadata |
| Project/publisher | `gitleaks/gitleaks` GitHub org; primary maintainer `zricethezav` (Zachary Rice) | Public Upstream Evidence — GitHub repo/release metadata |
| License | MIT | Public Upstream Evidence — `LICENSE` file, gitleaks master branch |
| Windows support | Yes — prebuilt release assets `gitleaks_8.30.1_windows_x64.zip`, `..._windows_x32.zip`, `..._windows_arm64.zip` | Public Upstream Evidence — release asset list |
| Maintenance/activity posture | **Explicitly declared "feature complete."** README (master branch, retrieved 2026-08-27) states: *"Gitleaks is feature complete. I'm not merging new features into Gitleaks. Future releases will be security patches only. I'm shifting my focus to Betterleaks."* Most recent commit observed: 2026-07-22 (a dependency-bump PR merge). | Public Upstream Evidence — README banner; GitHub Commits API |
| Security reporting mechanism | GitHub private vulnerability reporting (`gitleaks/gitleaks/security/advisories/new`); documented `SECURITY.md` | Public Upstream Evidence — `SECURITY.md` |

**Installation mechanisms (Public Upstream Evidence, README "Getting Started"):** Homebrew
(`brew install gitleaks`), Docker (Docker Hub `zricethezav/gitleaks` and `ghcr.io/gitleaks/gitleaks`
images), prebuilt binaries per OS/arch from the GitHub Releases page, or build from source
(`git clone` + `make build`, requires Go toolchain).

- **Version pinning:** exact release tags/binaries are available per version; the documented
  pre-commit integration example itself pins an exact tag (`rev: v8.24.2` in the README example).
- **Published checksums:** each release publishes a `..._checksums.txt` file (SHA-256 digests per
  asset, confirmed via the Releases API `digest` fields). **Unable to Verify / notable gap:** no
  separate cryptographic **signature** file (e.g., `.sig`/`.pem`) accompanies the checksums file in
  the release asset list inspected for `v8.30.1` — only the checksums text file itself is
  published as a release asset. This is a difference from TruffleHog (§10) and was not resolved by
  further digging into CI-level provenance/SLSA attestations, which were out of scope for this
  research pass.
- **Source install:** Go toolchain (`make build`) — introduces a Go build-tool trust footprint if
  used; not evaluated further here (no judgment made about relative language-based safety, per
  task instruction §15).

**Scanning modes (Public Upstream Evidence, README "Usage"/"Commands"):**

- `gitleaks git` — scans a Git repository, including history, via `git log -p` under the hood
  (operates on the local Git object database; `--log-opts` lets callers scope the commit range).
- `gitleaks dir` (aliases `files`/`directory`) — scans a directory or file tree without requiring
  Git history (current/working-tree content).
- `gitleaks stdin` — scans arbitrary piped input.
- No explicit built-in "staged files only" mode is documented in the README; the documented
  pre-commit integration relies on the external `pre-commit` framework invoking `gitleaks` (see
  below), not a dedicated `gitleaks`-native staged-diff subcommand.

**Detection model (Public Upstream Evidence, README + `config/gitleaks.toml`):** primarily
regex-rule-based, with an extendable default rule set covering many named providers/services
(e.g., examples shown in documentation: AWS, GitHub/GitLab tokens, Sidekiq, Stripe-style secrets)
plus Shannon-entropy thresholds usable per rule (`entropy = 3.5` example in the documented config
schema) and keyword pre-filtering. **Unable to Verify:** the exact current total rule count in the
shipped default `config/gitleaks.toml` was not exhaustively tallied for this packet (the file is
large and auto-generated); only its existence, schema, and representative rule types were
confirmed.

**Configuration/suppression (Public Upstream Evidence, README "Configuration"):**

- Custom `.toml` rule files, extendable from the default config (`useDefault = true`) or fully
  replaceable.
- `[[rules.allowlists]]` blocks support per-rule allowlisting by commit SHA, path regex, or
  "stopwords" matched against the extracted secret value, with `AND`/`OR` condition composition.
- A default global `[allowlist]` in the shipped config already excludes common non-source paths
  (binary/image extensions, `go.mod`/`go.sum`, vendored dependency directories, Gradle/Maven
  wrapper scripts).
- Inline `gitleaks:allow` comments are supported (overridable via `--ignore-gitleaks-allow`).
- `.gitleaksignore` file support (`--gitleaks-ignore-path`, defaults to `.`).
- **Baseline model:** `--baseline-path`, generated from any prior gitleaks JSON report
  (`--report-path`); re-running with `--baseline-path` limits report output to only new findings.
  The baseline file is a full gitleaks finding report (JSON), which — per the documented Git
  scanning output shown in the README example — includes the plaintext `Secret:` field for each
  finding. **This means a Gitleaks baseline/report file, as documented, contains the actual
  discovered secret values, not just hashes or fingerprints, unless redaction (`--redact`) is
  applied at scan time.**

**Structured output (Public Upstream Evidence, `--help` flags in README):** `--report-format`
supports `json`, `csv`, `junit`, `sarif`, and `template`; `--exit-code` is configurable (default
`1` when leaks are found). A `--redact[=N]` flag can redact 0–100% of secret values from logs and
stdout/report output.

**Network/data-handling behavior (Public Upstream Evidence + Inference):** Gitleaks' documented
command set (`git`, `dir`, `stdin`) contains no flags or documented behavior indicating outbound
network calls, telemetry, or "verification" of discovered secrets against external services —
detection is purely local pattern/entropy matching against local Git objects or filesystem
content. **Inference:** normal scanning (excluding the optional Homebrew/Docker/binary *download*
step itself) appears to remain fully local, based on the absence of any documented
network/verification flags. This was not independently confirmed by execution (prohibited by
task scope) and is not a Public Upstream Evidence claim about the compiled binary's actual runtime
behavior — no explicit "no telemetry" statement was found in the README or `SECURITY.md`. Recorded
as **Unable to Verify (explicit no-telemetry statement)**, with the above **Inference** as the
best available basis at this evidence tier.

**Git-history capability (Public Upstream Evidence):** `gitleaks git` walks Git log/patch data
(`git log -p`-equivalent) and can target ranges via `--log-opts` (e.g.,
`--log-opts="--all commitA..commitB"`); this operates against the local `.git` object database and
does not require network access for a local repository. **Unable to Verify:** exact behavior for
very large repositories, binary files, and renamed files was not independently confirmed beyond
what the README states in passing (a `--max-target-megabytes` flag exists to skip large files).

**Staged/pre-commit capability (Public Upstream Evidence):** documented via the external
`pre-commit` framework (`.pre-commit-config.yaml` referencing the `gitleaks/gitleaks` repo at a
pinned `rev`), or a `gitleaks-docker` pre-commit hook ID for container-based execution. No native
`gitleaks pre-commit`-specific subcommand distinct from `git`/`dir`/`stdin` is documented.

## 10. TruffleHog Evidence

| Attribute | Value | Source |
|---|---|---|
| Current stable version (at research date) | `v3.97.1` | Public Upstream Evidence — GitHub Releases API (`api.github.com/repos/trufflesecurity/trufflehog/releases/latest`) |
| Release date | Published 2026-08-24 (created 2026-08-24) — three days before this research pass | Public Upstream Evidence — same release metadata |
| Project/publisher | `trufflesecurity/trufflehog`; commercial backing by Truffle Security Co. (README references a paid "TruffleHog Enterprise" product) | Public Upstream Evidence — README |
| License | **AGPL-3.0** | Public Upstream Evidence — README license badge (`license-AGPL--3.0-brightgreen`), linking to `/LICENSE` |
| Windows support | Yes — prebuilt release assets `trufflehog_3.97.1_windows_amd64.tar.gz`, `..._windows_arm64.tar.gz`; README also documents a Windows Command Prompt / PowerShell Docker invocation | Public Upstream Evidence — release asset list; README "Docker" section |
| Maintenance/activity posture | Actively maintained: most recent commit observed 2026-08-26 (one day before this research pass), i.e., commit activity postdates the latest tagged release | Public Upstream Evidence — GitHub Commits API |
| Security reporting mechanism | Email (`security@trufflesec.com`), plus a documented, detailed **Blind SSRF & Outbound Request** disclosure policy specifically because the tool makes outbound verification requests | Public Upstream Evidence — `SECURITY.md` |

**Installation mechanisms (Public Upstream Evidence, README "Installation"/"Verifying the
artifacts"):** Homebrew (macOS), Docker (`trufflesecurity/trufflehog` image, with documented
Windows Command Prompt and PowerShell invocation forms), prebuilt binary releases, build from
source (`go install`), or a shell install script
(`curl ... install.sh | sh -s -- -b /usr/local/bin [version]`) that can install a specific pinned
version tag.

- **Version pinning:** the install script explicitly supports pinning to an exact release tag
  (e.g., `v3.56.0` shown as an example); binary releases are also tag-specific.
- **Published checksums and signatures:** each release publishes
  `trufflehog_{version}_checksums.txt`, a `.sig` signature, and a `.pem` certificate, verifiable
  with **Cosign** (Sigstore) against a documented `certificate-identity-regexp` tied to the
  project's own GitHub Actions OIDC issuer. This is a stronger documented supply-chain
  verification story than Gitleaks' checksum-only release assets (§9).
- **Source install:** Go toolchain (`go install`) — same category of build-tool trust footprint as
  Gitleaks; not judged as inherently safer or less safe per task instruction §15.

**Scanning modes (Public Upstream Evidence, README "Quick Start" + `--help` output):**

- `trufflehog git <uri>` — scans a Git repository (local `file://` path or remote URL).
- `trufflehog filesystem <path...>` — scans individual files/directories without Git.
- `trufflehog stdin` — scans stdin.
- Many additional source-specific subcommands exist (`github`, `gitlab`, `s3`, `gcs`, `docker`,
  `postman`, `jenkins`, `elasticsearch`, `huggingface`, CI helpers, etc.) — these are outside the
  local-repository-scanning scope relevant to KST's stated need and are not evaluated further here
  beyond noting their existence.
- **Local git safety note (Public Upstream Evidence):** to guard against malicious Git configs in
  local scanning (referenced as CVE-2025-41390), TruffleHog **clones local Git repositories to a
  temporary directory before scanning** by default; a `--trust-local-git-config` flag can skip this
  for trusted repos, and `--clone-path` can redirect the temp clone location. This clone-before-scan
  behavior is itself fully local (no network required for a `file://` source).

**Detection vs. verification model (Public Upstream Evidence — this is TruffleHog's most
distinguishing characteristic relative to Gitleaks/detect-secrets):**

- README states TruffleHog "classifies over 800 secret types" and, "for every secret TruffleHog can
  classify, it can also log in to confirm if that secret is live or not" (**Validation**), plus an
  "Analysis" mode for ~20 of the most commonly leaked credential types that issues multiple
  requests to learn more about the credential's access/permissions.
- `--results=RESULTS` flag: accepted values are `verified` (confirmed valid via a live API call),
  `unknown` (verification failed due to an error, not a definitive validity statement), `unverified`
  (detected but not verified), and `filtered_unverified`. **Default behavior is
  `verified,unverified,unknown`** — i.e., TruffleHog attempts verification by default unless told
  not to.
- **`--no-verification` flag: "Don't verify the results."** This is the documented mechanism to
  obtain a fully local, non-verifying scan (detection only, no outbound verification calls for
  discovered candidate secrets).
- **`--no-verification-cache` flag:** disables verification result caching (implying verification
  results are cached locally by default when verification is enabled).
- `--verifier=VERIFIER` / `--custom-verifiers-only`: lets an operator point verification at custom
  endpoints instead of (or in addition to) the built-in provider endpoints.
- **Does disabling verification reduce detection coverage, or only validity confirmation?** Based
  on the documented `--results` semantics, disabling verification (`--no-verification`) does not
  remove *detection* — pattern/entropy-based candidates are still found and can still be reported
  as `unverified` — it removes the live confirm-with-provider step. This is an **Inference** from
  the documented flag semantics, not independently executed/confirmed (execution is prohibited by
  task scope).
- **`SECURITY.md`'s Blind-SSRF policy is direct Public Upstream Evidence that verification
  performs genuine outbound network requests** to third-party/provider endpoints as part of normal
  (default) operation, and that the project treats uncontrolled outbound request behavior as a
  security-relevant surface with its own disclosure policy.

**Structured output (Public Upstream Evidence, `--help`/README):** `--json` (current format) and
`--json-legacy` (pre-v3.0 format, limited to `git`/`gitlab`/`github` sources), `--sarif` (buffered
in memory for the whole scan, since SARIF requires one JSON document), `--github-actions` format.
`--fail` exits with code `183` if results are found; `--fail-on-scan-errors` exits non-zero on scan
errors.

**Configuration/suppression (Public Upstream Evidence):** `--include-detectors` /
`--exclude-detectors` (by protobuf name/ID/range), `--filter-unverified` (only first unverified
result per chunk per detector), `--filter-entropy` (entropy floor for unverified results),
`--config=CONFIG` (a configuration file path — exact schema not further inspected in this pass).
**Unable to Verify:** a dedicated "baseline" concept equivalent to Gitleaks'
`--baseline-path`/detect-secrets' `.secrets.baseline` was not found documented in the README; this
was not exhaustively searched beyond the top-level README and `--help` output reproduced there.

**Git-history capability (Public Upstream Evidence):** `trufflehog git <uri>` scans full commit
history by default; `--since-commit`/`--branch` flags scope CI-style incremental scans (e.g., scan
only a PR branch's new commits vs. a base branch). A separate `github-experimental --object-discovery`
feature can enumerate **deleted/hidden commits** via GitHub's Cross-Fork Object Reference technique
— explicitly documented as taking "between 20 minutes and a few hours" depending on repository size
and requiring network access to GitHub's hosted API (this is a GitHub-hosted-specific,
alpha-labeled feature, not applicable to a purely local/on-prem Git history scan).

## 11. Third Candidate Evidence — detect-secrets

| Attribute | Value | Source |
|---|---|---|
| Current tagged release | `v1.5.0` (2024-05-06) | Public Upstream Evidence — GitHub Releases API |
| Most recent commit observed | 2026-04-02 ("Add security review workflow") | Public Upstream Evidence — GitHub Commits API |
| Project/publisher | Yelp (`Yelp/detect-secrets`) | Public Upstream Evidence — repository ownership |
| License | Apache License 2.0 | Public Upstream Evidence — `LICENSE` file |
| Windows support | **Unable to Verify (native Windows) —** distributed as a Python package (PyPI, Homebrew formula badge shown in README); no platform-specific compiled binary releases are published (the `v1.5.0` GitHub release has no binary assets, only source tarball/zipball). Running on Windows would depend on a Windows-compatible Python environment, not a dedicated native Windows build. |
| Runtime dependency | Python (README shows CI badges for multiple Python versions; the `v1.5.0` release notes mention adding 3.10–3.12 support and dropping 3.6/3.7) | Public Upstream Evidence — README, release notes |
| Installation trust/locality | `pip install detect-secrets` (PyPI) or Homebrew formula | Public Upstream Evidence — README badges |
| Detection model | Pluggable **plugins**, not a single regex engine: dedicated detectors for many providers (`AWSKeyDetector`, `AzureStorageKeyDetector`, `GitHubTokenDetector`, `GitLabTokenDetector`, `SlackDetector`, `StripeDetector`, `TwilioKeyDetector`, `PrivateKeyDetector`, etc.) plus generic entropy detectors (`Base64HighEntropyString`, `HexHighEntropyString`) and a `KeywordDetector`; plugins can be individually disabled (`--disable-plugin`) | Public Upstream Evidence — README "Viewing All Enabled Plugins" |
| Baseline model (central design concept) | `detect-secrets scan > .secrets.baseline` creates a baseline of currently-present potential secrets; `detect-secrets scan --baseline .secrets.baseline` updates it (adds new findings, removes stale ones, preserves human-applied labels); `detect-secrets audit .secrets.baseline` is used to label true/false positives | Public Upstream Evidence — README "Examples"; `docs/design.md` |
| **Baseline value storage** | The baseline stores a **`hashed_secret`** (a hash keyed on the secret value + filepath + detection method) alongside `type`, `filename`, `line_number`, `is_secret`, `is_verified` — **not the raw secret value**. Per design docs, this is explicitly so "we didn't want the baseline to be the single file that contained all the secrets in a given repository." | Public Upstream Evidence — `docs/design.md` "PotentialSecret" section |
| Pre-commit integration | First-class: ships a `detect-secrets-hook` entry point intended for use with staged files (`git diff --staged --name-only -z \| xargs -0 detect-secrets-hook --baseline .secrets.baseline`) or all tracked files | Public Upstream Evidence — README "Alerting off newly added secrets" |
| Git-history scanning | **Not a first-class documented capability.** The README explicitly frames the tool's model as diffing against a periodically-updated baseline "to identify whether any *new* secret has been committed," specifically **"avoid[ing] the overhead of digging through all git history"** as a stated design goal. | Public Upstream Evidence — README "About" |
| Network behavior / credential verification | No verification/live-credential-check concept was found documented in the README or design docs; the tool's design is detection + baseline/audit workflow only. **Unable to Verify** beyond the absence of any documented verification feature (absence of a feature in top-level docs is not proof of absence in the codebase). |
| Structured output | JSON is the native baseline format; also usable programmatically as a Python library (`SecretsCollection`, `.json()`) | Public Upstream Evidence — README "Usage in Other Python Scripts" |
| Maintenance evidence | No tagged release since 2024-05-06 (over two years before this research date), but repository commit activity (including a security-workflow commit) as recently as 2026-04-02 — i.e., the project appears maintained at the commit level without recent version tags. **This is a materially different maintenance signal from Gitleaks/TruffleHog and should be weighed as such by independent review**, without this document drawing a conclusion about its acceptability. | Public Upstream Evidence — Releases + Commits API |
| Security reporting mechanism | **Unable to Verify** — no `SECURITY.md` file was located at the repository root during this pass. |

This candidate materially differs from Gitleaks and TruffleHog in ecosystem (Python vs. Go binary),
governing workflow (baseline-and-audit vs. ad hoc/CI scan or verify-by-default), and explicit
non-goal (git-history depth), which is why it was retained per task §12's justification test.

## 12. Credential Verification / Network Analysis

This is the sharpest factual differentiator among the three candidates:

- **Gitleaks:** no documented verification/live-credential-check feature. Detection only.
  (**Inference** that normal scanning is fully local, per §9; no explicit "no telemetry"
  statement found — recorded as **Unable to Verify** for an explicit claim.)
- **TruffleHog:** verification is a **named, default-on** capability (`--results` defaults to
  include `verified`; verification is only suppressed with `--no-verification`). Verification
  contacts external provider APIs to confirm secret validity. The project's own `SECURITY.md`
  documents a dedicated policy for outbound-request/SSRF-adjacent behavior, which is direct
  Public Upstream Evidence that outbound network calls to arbitrary/attacker-influenced endpoints
  are an acknowledged, real operational characteristic of the tool, not merely a hypothetical.
  A fully local, non-verifying scan is achievable via `--no-verification` (detection-only mode).
- **detect-secrets:** no documented verification/live-credential-check feature at all. Detection
  and baseline/audit only.

Per task §14, KST's measured gap (`S0.3-G007`) is **detection**, not live credential verification.
This packet does not conclude whether TruffleHog's verification capability is a benefit KST should
accept, a data-handling risk KST should avoid, or an optional feature KST should disable — that
judgment is explicitly reserved for independent review (§22 below has the specific question).

## 13. Git-History Capability (comparative)

| Candidate | History scanning | Network required for local history scan | Notes |
|---|---|---|---|
| Gitleaks | Native, via `gitleaks git` walking local `git log -p` data; commit-range scoping via `--log-opts` | No (operates on local `.git` objects) | Documented `--max-target-megabytes` flag to skip oversized files; large-repo/binary-file/rename behavior beyond that not independently confirmed (**Unable to Verify**) |
| TruffleHog | Native, via `trufflehog git <uri>`; scans full history by default, `--since-commit`/`--branch` for incremental/CI scoping; local repos are cloned to a temp dir first (safety measure) | No for `file://` sources (local clone-then-scan); yes if scanning a remote URL directly | `github-experimental --object-discovery` can find **deleted/hidden** commits, but this is GitHub-hosted-specific and network-dependent — not evaluated as a local-history feature |
| detect-secrets | **Not a first-class capability** — explicit design goal is to avoid full-history scanning in favor of an evolving baseline | N/A | A one-time deep history scan is not the tool's documented model; would likely require a different/manual approach not evidenced here (**Unable to Verify**) |

## 14. Current/Staged-Content Capability (comparative)

| Candidate | Working tree / current files | Staged/pre-commit | stdin |
|---|---|---|---|
| Gitleaks | `gitleaks dir` | Via external `pre-commit` framework (`.pre-commit-config.yaml`, pinned `rev`), or a documented `pre-commit.py` script placed in `.git/hooks/` — not a dedicated native staged-diff subcommand | `gitleaks stdin` |
| TruffleHog | `trufflehog filesystem <path...>` | Not documented as a first-class subcommand in the README reviewed; `--since-commit`/`--branch` support incremental CI scans of committed history, which is a different mechanism from a staged/uncommitted diff | `trufflehog stdin` |
| detect-secrets | `detect-secrets scan --all-files` (or default, tracked files) | First-class: `detect-secrets-hook` designed specifically for staged-file scanning via `git diff --staged --name-only -z \| xargs -0 detect-secrets-hook` | **Unable to Verify** (no stdin subcommand found documented in the README reviewed) |

No hook, pre-commit configuration, or CI integration was created for any candidate; this is
future-capability research only, per task scope.

## 15. Installation / Supply-Chain Analysis

| Attribute | Gitleaks | TruffleHog | detect-secrets |
|---|---|---|---|
| Official install methods | Homebrew, Docker (2 registries), prebuilt binaries, build from source (Go) | Homebrew, Docker, prebuilt binaries, install script (with optional Cosign verification), build from source (Go) | pip (PyPI), Homebrew |
| Exact-version pinning | Yes — versioned release assets / pinned pre-commit `rev` | Yes — versioned release assets; install script accepts an explicit version argument | Yes — `pip install detect-secrets==<version>`; no compiled binary versioning |
| Prebuilt vs. source | Both | Both | Source/bytecode only (Python package); no compiled binary |
| Published checksums | Yes — per-release `checksums.txt` (SHA-256, confirmed via Releases API `digest` fields) | Yes — per-release `checksums.txt` | **Unable to Verify** — not confirmed for this pass; PyPI packages are typically hash-verified by pip against the package index rather than a project-published checksum file |
| Release/artifact signatures | **Unable to Verify / apparent gap** — no signature asset found alongside the release checksums file for `v8.30.1` | Yes — Cosign/Sigstore signature (`.sig`) + certificate (`.pem`) over the checksums file, verifiable against the project's own GitHub Actions OIDC identity | **Unable to Verify** — not confirmed for this pass |
| Install scripts | None required (binary/Homebrew/Docker); source build uses `make` | Optional shell install script (`install.sh`), with an explicit Cosign-verification code path | None beyond `pip`/Homebrew's own mechanisms |
| Runtime requirements | None (statically built Go binary, per typical Go release practice — **Inference**, not independently confirmed by inspecting the binary) | Same as Gitleaks (**Inference**) | Requires a Python interpreter and its own dependency tree (`pip` resolves transitive Python packages) — a categorically different runtime footprint than a single static binary |
| Administrator requirement | Not documented as required; user-directory installs are typical for Homebrew/binary use | Same | Same as any `pip`/Homebrew install |
| Updates / rollback / removal | Re-run install method for a different pinned version/tag; remove the binary or `brew uninstall gitleaks` | Same pattern; install script supports installing a specific version directly | `pip install detect-secrets==<version>` to change version; `pip uninstall detect-secrets` to remove |

No judgment is made that one language/runtime (Go static binary vs. Python package) is inherently
safer; the above records the differing trust/dependency footprint only, per task §15.

## 16. False-Positive / Baseline Analysis

| Mechanism | Gitleaks | TruffleHog | detect-secrets |
|---|---|---|---|
| Allowlists | `[[rules.allowlists]]` per rule (paths/commits/regexes/stopwords, AND/OR composition); global `[allowlist]` in default config | `--include-detectors`/`--exclude-detectors`; `--filter-unverified`; `--filter-entropy` | Per-plugin disable (`--disable-plugin`); baseline audit labels (`is_secret: false`) |
| Path exclusions | Via allowlist `paths` regex | Not explicitly confirmed beyond detector-level excludes (**Unable to Verify** for a dedicated path-exclude flag) | Implicit via what is scanned (`scan <path>`); no dedicated exclude flag confirmed in this pass (**Unable to Verify**) |
| Inline ignores | `gitleaks:allow` comment (overridable via `--ignore-gitleaks-allow`) | **Unable to Verify** — not found in the README sections reviewed | **Unable to Verify** — not found in the README sections reviewed |
| Fingerprints | Yes — documented `Fingerprint` field in finding output (commit:file:rule:line) | **Unable to Verify** — not confirmed as a named concept in this pass | Baseline entries are effectively fingerprints (hash of secret+file+detector) |
| Baselines | `--baseline-path` (a full prior JSON report; **contains plaintext secret values as documented**, unless `--redact` was used when the baseline was generated) | **Unable to Verify** — no equivalent baseline concept found documented | `.secrets.baseline` (JSON; stores **hashed** secret values plus per-finding audit labels) — explicitly designed to avoid storing raw secrets in a source-controllable file |
| Context preserved for human review | Yes (finding metadata: rule, file, commit, line) | Yes (finding metadata: detector, decoder, source, verified flag) | Yes (`type`, `filename`, `line_number`, `is_secret`/`is_verified` labels via `audit`) |

**Important asymmetry for KST's future-baseline questions (task §20):** if KST later considers a
committed baseline file, Gitleaks' baseline format (a full report) is documented to include
plaintext secret text unless redaction was applied at generation time, whereas detect-secrets'
baseline format is designed specifically to store only a keyed hash. This is a factual design
difference with data-handling implications for any future decision to commit a baseline file to
source control — no such decision is made or recommended here.

## 17. Structured Output / Automation Capability

| Format | Gitleaks | TruffleHog | detect-secrets |
|---|---|---|---|
| JSON | Yes (`--report-format json`) | Yes (`--json`, plus legacy `--json-legacy` for some sources) | Yes (native baseline format; also a Python API) |
| SARIF | Yes (`--report-format sarif`) | Yes (`--sarif`; documented as buffered fully in memory, larger memory use for very large result sets) | **Unable to Verify** — not found documented in the README reviewed |
| CSV | Yes (`--report-format csv`) | **Unable to Verify** — not found documented | **Unable to Verify** — not found documented |
| Other | `junit`, `template` report formats | `--github-actions` format | — |
| Exit codes | `--exit-code` configurable (default `1` on findings) | `--fail` → exit `183` on findings found; `--fail-on-scan-errors` for scan errors | **Unable to Verify** — exit-code semantics not confirmed in this pass |

All three formats are theoretically compatible with future developer-local execution, a future
pre-commit hook, or a future CI job — no such integration is created here (task §21 explicitly
prohibits CI/severity-threshold work in this pass).

## 18. Neutral Comparative Table

| Criterion | Gitleaks | TruffleHog | detect-secrets |
|---|---|---|---|
| Current stable version | v8.30.1 (2026-03-21) | v3.97.1 (2026-08-24) | v1.5.0 (2024-05-06; commit activity to 2026-04-02) |
| License | MIT | AGPL-3.0 | Apache-2.0 |
| Windows support | Yes (native prebuilt binary) | Yes (native prebuilt binary) | Unable to Verify (native); Python-environment dependent |
| Local current-tree scan | Yes (`dir`) | Yes (`filesystem`) | Yes (`scan`, tracked or `--all-files`) |
| Git-history scan | Yes, native, local | Yes, native, local for `file://` | Not a first-class capability (explicit non-goal) |
| Staged/pre-commit capability | Via external `pre-commit` framework | Not confirmed as first-class | First-class (`detect-secrets-hook`) |
| Provider-specific rules | Yes (large default rule set) | Yes ("800+" classified secret types) | Yes (dedicated plugin per provider) |
| Entropy/general detection | Yes (`entropy` field per rule) | Detector-based; entropy filter (`--filter-entropy`) for unverified results | Yes (`Base64HighEntropyString`, `HexHighEntropyString`) |
| Credential verification | No | Yes, default-on (disable via `--no-verification`) | No |
| Verification network behavior | N/A (no verification feature) | Live outbound calls to provider APIs by default; documented Blind-SSRF disclosure policy | N/A |
| Fully local mode | Yes (no verification feature to disable) | Yes, via `--no-verification` | Yes |
| Source upload required | No | No (verification calls provider APIs about the discovered value; does not upload the surrounding source) | No |
| JSON | Yes | Yes | Yes |
| SARIF | Yes | Yes | Unable to Verify |
| Allowlist/suppression | Yes (`[[rules.allowlists]]`, inline `gitleaks:allow`) | Yes (detector include/exclude, entropy/unverified filters) | Yes (plugin disable, baseline audit labels) |
| Baseline/fingerprint | Yes (`--baseline-path`; **stores plaintext secret text** per documented report format) | Unable to Verify (no equivalent found) | Yes (`.secrets.baseline`; **stores hashed secret value**) |
| Version pinning | Yes | Yes | Yes |
| Published checksum/signature | Checksum yes; signature Unable to Verify / apparent gap | Checksum + Cosign signature | Unable to Verify |
| Runtime/install footprint | Static Go binary (Inference) | Static Go binary (Inference) | Python interpreter + package dependencies |
| Maintenance evidence | Publisher states feature-complete; security-patches-only going forward; last commit 2026-07-22 | Last commit 2026-08-26 (1 day before research date); active tagged releases | No new tag since 2024-05-06; commit activity to 2026-04-02 |
| Removal complexity | Delete binary / `brew uninstall` | Same | `pip uninstall` |
| Unable-to-Verify items | Exact rule count; explicit no-telemetry statement; release signature (apparent absence, not confirmed via deeper CI/provenance inspection) | Baseline-equivalent feature; CSV output; path-exclude flag; inline-ignore mechanism | Windows-native support; checksums/signatures; SARIF; exit-code semantics; security-reporting mechanism |

No "Winner"/"Rank"/"Score"/"Recommendation"/"Preferred"/"Best" column is included, per task §23.

## 19. Candidate Tradeoff Summaries

### Gitleaks

**Strengths supported by evidence:**
- MIT license; simple, single static-binary installation with official Windows binaries.
- Native local Git-history scanning against the local object database, with commit-range scoping.
- Rich, extensible rule/allowlist configuration model, multiple structured output formats
  (JSON/SARIF/CSV/JUnit).
- No verification/network-call feature to reason about — detection-only by design.

**Additional trust/complexity supported by evidence:**
- Maintainer has explicitly declared the project "feature complete," with future releases
  limited to security patches, and has stated a public shift of focus to a different,
  newer project ("Betterleaks"). Most recent commit predates TruffleHog's by roughly five weeks
  at the time of this research.
- No release-artifact signature was found alongside the published checksums file (checksum-only
  supply-chain verification, versus TruffleHog's Cosign-signed checksums).
- The documented baseline/report format includes plaintext secret values unless the operator
  applies redaction at generation time — a data-handling consideration for any future baseline
  adoption.

**Potential fit considerations for KST:** aligns with KST's stated preference for fully local,
non-verifying detection and Windows compatibility; the "feature complete / security patches
only" maintenance posture and the plaintext-baseline behavior are factual characteristics
independent review may weigh against KST's reproducible-installation and data-handling
requirements (§6).

**Unable to Verify:** exact current default rule count; explicit statement (or absence) of
telemetry/network calls during normal scanning; whether a release-signing/provenance mechanism
exists outside the release-asset list itself (e.g., SLSA attestations at the CI level).

### TruffleHog

**Strengths supported by evidence:**
- Actively maintained with very recent commit activity; broad detector coverage (800+ classified
  secret types); native Windows binaries; Cosign-signed release checksums provide a stronger
  documented supply-chain verification path than Gitleaks' checksum-only assets.
- `--no-verification` provides a documented way to obtain a fully local, detection-only scan.
- Native local Git-history scanning, with an explicit local-clone safety measure for local
  repository paths (referencing a specific CVE).

**Additional trust/complexity supported by evidence:**
- AGPL-3.0 license — a materially different (copyleft) license category from Gitleaks' MIT and
  detect-secrets' Apache-2.0.
- Live credential **verification is default-on** unless explicitly disabled; verification
  performs outbound calls to third-party/provider endpoints, and the project maintains a
  dedicated, detailed Blind-SSRF/outbound-request disclosure policy — direct evidence that
  outbound network behavior is a genuine, acknowledged characteristic of default operation.
- No baseline-equivalent mechanism was found documented, unlike Gitleaks and detect-secrets.

**Potential fit considerations for KST:** the detection-only mode (`--no-verification`) may
align with KST's local-execution/credential-safety requirements (§6), while the default-on
verification behavior and its outbound-request implications are a factual characteristic that
independent review may need to weigh, particularly given KST's stated preference that findings
and source remain local unless separately authorized, and given the AGPL-3.0 license category.

**Unable to Verify:** a baseline-equivalent suppression mechanism; CSV output support; a
dedicated path-exclusion flag; an inline-ignore-comment mechanism.

### detect-secrets

**Strengths supported by evidence:**
- Apache-2.0 license; explicit design goal of avoiding raw-secret storage in its baseline file
  (hash-based), and a first-class staged/pre-commit hook entry point
  (`detect-secrets-hook`) built specifically for a prevention workflow.
- Broad provider-specific plugin coverage plus generic entropy plugins; plugins are individually
  toggleable.
- No live-verification/network-call feature was found documented — detection and
  baseline/audit workflow only.

**Additional trust/complexity supported by evidence:**
- Distributed as a Python package rather than a single static binary — a different install/runtime
  footprint (interpreter + transitive Python dependencies) than Gitleaks/TruffleHog.
- No new tagged release since 2024-05-06 (over two years before this research date), although
  repository commit activity continues into 2026; native Windows support, checksum/signature
  publication, and a `SECURITY.md` reporting channel were not confirmed in this pass.
- Explicitly **not** designed for deep Git-history scanning — its documented model is a
  periodically-updated baseline of *current* secrets, not a historical-commit scan.

**Potential fit considerations for KST:** the hash-based baseline format may be more consistent
with KST's stated preference to avoid storing raw secret values, and the native pre-commit-hook
design may be relevant to KST's stated interest in a future developer guard; the lack of a recent
tagged release and the absence of confirmed native Windows/signature/security-reporting evidence
are factual gaps independent review may need to resolve or accept as-is.

**Unable to Verify:** native Windows execution path; published checksums/signatures; SARIF output;
exit-code semantics; a security vulnerability-reporting mechanism.

## 20. Unable-to-Verify Items (consolidated)

- **Gitleaks:** exact current total rule count in `config/gitleaks.toml`; an explicit
  no-telemetry/no-network statement for normal scanning; whether any release-signing or SLSA-style
  provenance mechanism exists beyond the published checksums file; exact large-repository/binary/
  rename behavior during history scans beyond the documented `--max-target-megabytes` flag.
- **TruffleHog:** a baseline-equivalent suppression/carry-forward mechanism; CSV output support; a
  dedicated path-exclusion flag; an inline-ignore-comment mechanism; exact behavior/limits for
  very large repositories during history scans.
- **detect-secrets:** native Windows execution path (vs. Python-environment-dependent); published
  checksums/signatures for PyPI artifacts; SARIF output; CLI exit-code semantics; a documented
  security vulnerability-reporting channel (`SECURITY.md` was not found at the repository root).
- **General:** none of the three candidates' actual compiled/packaged runtime behavior was directly
  observed (execution was prohibited by task scope); all detection-model and network-behavior
  claims rely on each project's own documentation, not on this session's direct observation.

## 21. Candidate-Neutral Safe First-Scan Procedure (PLAN ONLY — not executed)

Whichever tool is later admitted by the project owner, a first scan should, at minimum:

1. Confirm the exact admitted version (matching the version recorded in the eventual admission
   decision).
2. Confirm a clean working tree (`git status --short` empty) before scanning.
3. Run the tool's local-only / non-verifying mode where available (e.g., Gitleaks has no
   verification feature to disable; TruffleHog would use `--no-verification`; detect-secrets has
   no verification feature).
4. Scan current/tracked repository content (e.g., Gitleaks `dir`, TruffleHog `filesystem`,
   detect-secrets `scan`).
5. Scan Git history where the tool supports it (Gitleaks `git`, TruffleHog `git file://<path>`);
   record explicitly if the admitted tool (e.g., detect-secrets) does not support this and note the
   resulting coverage limitation.
6. Capture machine-readable output (JSON/SARIF, as available) to a local file only.
7. Do **not** upload the report to any external service.
8. Disable credential verification unless the project owner has separately and explicitly
   authorized it (relevant to TruffleHog only).
9. Do **not** automatically rotate, remediate, or modify any discovered credential.
10. Do **not** suppress any finding as part of this first pass (no allowlist/baseline entries
    created solely to "clean" the report).
11. **Redact detected secret values from any evidence artifact** that is retained or shared —
    record only rule/detector ID, file path, commit SHA (for history findings), line number, and a
    fingerprint/hash, never the raw value.
12. If a finding appears to be a **plausible live credential**, stop and escalate to the project
    owner for handling rather than proceeding with further automated analysis, verification, or
    remediation.
13. Verify the repository working tree is unchanged after the scan completes
    (`git status --short` still clean; no report file left in a tracked location unless
    intentionally added and reviewed).

This procedure is not executed by this document and does not itself constitute an admission
decision.

## 22. Questions for Independent Review / Project Owner

- How much value does TruffleHog's live credential verification add for KST, given that KST's
  measured gap (`S0.3-G007`) is detection, and given the outbound-network implications documented
  in its `SECURITY.md` Blind-SSRF policy?
- Should KST require fully local scanning (no default-on network calls) as an admission property,
  which would favor Gitleaks or detect-secrets over TruffleHog's default configuration (though
  TruffleHog can be run in a fully local mode via `--no-verification`)?
- Is full Git-history scanning required on every run, only for a one-time baseline/release check,
  or not required at all — given that detect-secrets does not treat history scanning as a
  first-class capability while Gitleaks and TruffleHog do?
- Does KST want future pre-commit enforcement, which would favor a tool with a first-class staged
  workflow (detect-secrets' `detect-secrets-hook`, or Gitleaks via the external `pre-commit`
  framework)?
- What suppression/baseline governance would KST require if a baseline file is later committed to
  source control, given the documented difference between Gitleaks' plaintext-secret baseline
  format and detect-secrets' hashed-secret baseline format?
- Does Gitleaks' publicly declared "feature complete / security-patches-only" maintenance posture,
  and the newer "Betterleaks" project referenced by its own maintainer, matter to KST's
  reproducible-installation and long-term-maintenance requirements (§6)?
- Does TruffleHog's AGPL-3.0 license require any organizational review before use as a
  development-only tool (as opposed to embedding/distributing it), given KST's existing licensing
  considerations noted generally in `docs/security/DEPENDENCY_ADMISSION.md`?
- Is detect-secrets' Python runtime/install footprint (vs. a single static Go binary) an acceptable
  tradeoff for its hash-based baseline model and native pre-commit-hook support?
- Is the roughly two-year gap since detect-secrets' last tagged release (despite continued commit
  activity) an acceptable maintenance signal, or does it warrant treating detect-secrets as a
  lower-priority candidate pending further evidence?

This document does not answer these questions on the project owner's behalf.

## 23. Future Capability Boundary

- `S0.3-G006` (dedicated SAST) — **not evaluated** in this pass.
- `S0.3-G008` (SBOM generation) — **not evaluated** in this pass.
- No work was performed toward S0.7 or S0.8, and no Stage 9 work was performed.

## 24. Non-Work

The following were explicitly **not** done, consistent with task scope:

- No candidate scanner was installed, downloaded, or executed.
- No candidate scanner scanned any KST repository content.
- No live credential verification was attempted against any provider.
- No KST source, filenames beyond what was necessary, hostnames, database/schema names, or
  security findings were sent to any external service; public research queries used only generic
  tool/technology terms.
- No suppression rule, allowlist, baseline file, or `.gitleaksignore`/`.secrets.baseline`-style
  artifact was created.
- No Git hook, pre-commit configuration, or CI integration was created.
- No severity/risk-acceptance policy or numeric scoring was established.
- No tool was recommended, admitted, or disposed of (`ADMIT`/`DEFER`/`REJECT`).
- `G006` and `G008` capability reviews were not started.
- `docs/security/SECURITY_BASELINE.md`, `S0_3_EXISTING_TOOL_SECURITY_CHECKS.md`,
  `S0_4A/B/C_*`, `S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md`, and
  `S0_6_RUST_DEPENDENCY_ADMISSION.md` were read but not modified.

## 25. Conclusion

This document records evidence for three candidate dedicated secret-scanning tools (Gitleaks,
TruffleHog, detect-secrets) against KST's existing (limited, sentinel-only) secret-detection
capability and the accepted `S0.3-G007` gap, covering version/license/maintenance, Windows
support, local vs. history vs. staged scanning capability, detection-versus-verification behavior
and its network/data-handling implications, installation/supply-chain characteristics,
suppression/baseline mechanisms, and structured-output support. All substantive claims are
labeled Repository Observation, Public Upstream Evidence, Inference, or Unable to Verify.

**No admission recommendation was made. No tool has been admitted.** S0.6 remains **IN PROGRESS**;
Capability Review 2 (`S0.3-G007`) is **RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW**. The
actual admission decision, including whether any candidate is admitted, deferred, or rejected,
belongs to the project owner following independent review of this packet.

---

# Appendix — Independent Review Follow-Up: Gitleaks v8.30.1 Release Integrity and Correctness

**Research date: 2026-08-27**

**Status: NARROW SUPPLEMENTAL RESEARCH — NO TOOL RECOMMENDATION OR ADMISSION DECISION**

This appendix answers three specific upstream questions the independent reviewer raised about the
current Gitleaks `v8.30.1` release before an admission decision is made. It does **not** repeat the
Gitleaks/TruffleHog/detect-secrets comparison above, does not recommend a tool (including a
downgrade), and was produced without installing, downloading, executing, or using Gitleaks against
any KST content. All evidence below is **Public Upstream Evidence** drawn directly from the
`gitleaks/gitleaks` GitHub repository (issues, issue comments, commits, compare API, and release
metadata) unless otherwise labeled. No KST-specific information was used in any query.

## A.1 — Issue #2086: "Tag v8.30.1 not an ancestor of master, breaks pre-commit autoupdate"

- **Current state (as of 2026-08-27): OPEN.** Labeled `bug`. Filed 2026-04-16; last updated
  2026-07-29 (a "please fix this" comment from a third party, not from the maintainer). No `closed_by`,
  no `state_reason`. **No v8.30.2 (or any later tag) has been published** — `v8.30.1` remains the
  repository's most recent tag and its `Latest` GitHub release as of this research date.
- **Independent confirmation via the GitHub compare API** (`compare/master...83d9cd6...`, the exact
  commit the `v8.30.1` tag points to): `status: "diverged"`, `ahead_by: 1`, `behind_by: 24`, with a
  `merge_base_commit` dated **2026-02-21** — i.e., master and the `v8.30.1` tag commit share a
  common ancestor from nearly a month before `v8.30.1` was tagged/released (2026-03-12/03-21), and
  have since diverged. This independently reproduces the reporter's claim using the project's own
  API, without needing a local clone.
  - By contrast, the immediately preceding tag `v8.30.0`'s commit (`6eaad03...`) compares to master
    as `status: "behind"`, `behind_by: 27` — i.e., it **is** a clean ancestor of current master.
  - The single commit unique to the `v8.30.1` tag path relative to its shared history with master is
    `83d9cd6` ("update goreleaser", `.goreleaser.yml` only, **not GPG-signed/verified** per the
    commit's own `verification.verified: false`), built on top of parent commit `8d1f98c` — which
    *is* an ancestor of current master (`behind_by: 24`). In other words: the release's parent
    history was on `master` at the time, but the actual tagging commit itself was never merged back
    into `master`, leaving `v8.30.1` as an orphaned side-commit.
- **Maintainer response:** the repository owner (`zricethezav`, `author_association: COLLABORATOR`)
  responded directly in the issue thread: **"B/c I messed up the release"** (2026-05-12). This is a
  direct maintainer acknowledgment of a release-process error, not a denial or a claim that the
  report is invalid.
- **Whether a fix occurred:** **No.** As of 2026-08-27, the tag has not been rewritten, no
  replacement release has been cut, and the maintainer has not implemented either of the two fixes
  the reporter proposed (rewrite history and force-push, or merge the tag commit into master and
  cut `v8.30.2`). A third-party commenter (`jkreileder`) explicitly objected to any history-rewriting
  fix, citing supply-chain-security concerns about mutable release tags.
- **Disposition: v8.30.1 remains affected — orphaned/unreachable-from-master tag, confirmed both by
  the maintainer's own admission and independently reproduced via the GitHub compare API.** This is
  not merely a cosmetic/tooling inconvenience: it means the `v8.30.1` release artifacts cannot be
  verified as having been built from a commit that is part of the project's current `master` commit
  history/lineage, which is a genuine provenance gap, not an invalid or unreproducible report.

## A.2 — Issue #2164: "gitleaks_8.30.1_windows_x64.zip checksum does not validate"

- **Current state: CLOSED** (`closed_at: 2026-06-10`, `state_reason: "completed"`), **closed by the
  original reporter** (`jkbszpg`), not by a maintainer unilaterally closing it.
- **Maintainer response:** a project collaborator (`bryanbeverly`, `author_association:
  COLLABORATOR`) investigated directly and reported (2026-06-10) that they independently verified
  the Windows x64 asset from three sources — the published `gitleaks_8.30.1_checksums.txt`, a fresh
  local download, and GitHub's own stored asset digest via the API — and all three agreed. The
  maintainer stated the release assets had not been modified since publication (2026-03-21) and
  asked the reporter to re-download and compare exact file size/hash.
- **Independent confirmation for this research pass:** re-querying the live GitHub Releases API for
  `v8.30.1` today shows the `gitleaks_8.30.1_windows_x64.zip` asset digest as
  `sha256:d29144deff3a68aa93ced33dddf84b7fdc26070add4aa0f4513094c8332afc4e` (matching the `d29144...`
  prefix the collaborator quoted) with `size: 8438883` bytes (matching the collaborator's expected
  size exactly), and `created_at`/`updated_at` both timestamped 2026-03-21 (roughly one minute
  apart, consistent with normal GitHub release-asset processing, not a later silent replacement).
- **Whether an asset was replaced:** **No evidence of any asset replacement.** Timestamps and digest
  are consistent with a single, unmodified publication.
- **Whether the report was invalid or unreproducible:** **The reporter confirmed it was invalid on
  their end** — their final comment (2026-06-10) states: *"You are right, when I curled it, the
  win64 was modified 'in flight' on my end. But only that particular win x64 8.30.1, everything else
  including windows 8.30.0 was fine. Sorry for the trouble."* This is a first-party retraction, not
  an inference on our part.
- **Disposition: v8.30.1 Windows x64 is NOT affected by a genuine checksum/asset-integrity defect.**
  The original report was a client-side/download-corruption artifact, confirmed both by the
  reporter's own retraction and by this session's independent re-verification of the current
  published digest and size.

## A.3 — Issue #2170: "v8.30.1 detects nothing: default rules never match (canonical GitHub PAT → 'no leaks found', exit 0)"

- **Current state: CLOSED** (`closed_at: 2026-07-28`, `state_reason: "completed"`), **closed by the
  original reporter** (`mohan-n-swamy`). **No maintainer (`zricethezav` or other collaborator)
  commented on this issue at all** — it was investigated and resolved entirely by third-party
  community members (`author_association: "NONE"` for both commenters).
- **What the investigation found:** a community member (`jkreileder`) reproduced the reporter's
  example and showed the specific test token used in the report matched an existing **global
  allowlist stopword** in the shipped default config, `config/gitleaks.toml`:
  ```toml
  stopwords = [
      "014df517-39d1-4453-b7b3-9930c563627c",
      "abcdefghijklmnopqrstuvwxyz",
  ]
  ```
  A second community member (`RajeshRajendiran`) confirmed the root cause precisely: the reporter's
  example token, when lowercased, contains the literal substring `abcdefghijklmnopqrstuvwxyz` (a
  sequential-alphabet placeholder pattern), which the shipped default config explicitly allowlists
  to filter out common placeholder/example secrets. Both commenters demonstrated that substituting a
  genuinely random (non-placeholder) token of the same shape is detected correctly, and one traced
  the `checkFindingAllowed` allowlist logic directly against current `master` to confirm the finding
  is intentionally suppressed by design, not silently dropped by a broken detection engine.
- **Whether v8.30.1 remains affected:** **No corroborated defect remains.** The behavior was
  reproducible only because the reporter's test input happened to collide with a documented,
  intentional false-positive-suppression rule in the shipped default configuration — not a
  regression in detection generally. The reporter's own closure (`state_reason: "completed"`)
  is consistent with this explanation being accepted.
- **Classification per the task's required categories:** this is best classified as **incorrect
  test input** (the reproduction case collided with an existing, intentional stopword allowlist
  entry), not a confirmed defect, not a packaging/build defect, not a deprecated-command issue, not
  platform-specific, not Homebrew-specific, and not an unresolved-asset issue. **No maintainer
  explanation was posted**, so the *authoritative* (maintainer-sourced) confirmation of this
  classification is **Unable to Verify** — the disposition above rests on unrebutted, technically
  detailed community analysis (including a walkthrough against current `master` source) that the
  original reporter accepted by closing their own issue, not on a maintainer statement.
- **Relationship to #2086:** the reporter speculated a link to the orphaned-tag issue (#2086); the
  community investigation found the behavior reproducible on current `master` as well (i.e.,
  independent of which exact commit was tagged), which weighs against — but does not, absent a
  maintainer statement, definitively rule out — any connection between the two reports.

## A.4 — v8.30.1 Integrity Model (consolidated, not to be treated as equivalent mechanisms)

| Mechanism | Available for v8.30.1? | Detail |
|---|---|---|
| Project-published checksum file | Yes | `gitleaks_8.30.1_checksums.txt` (SHA-256 per asset), published as a release asset alongside the binaries |
| GitHub release-asset digest | Yes | GitHub independently computes and stores a `digest` (SHA-256) for each uploaded asset, retrievable via the Releases API; for `windows_x64.zip` this is `sha256:d29144deff3a68aa93ced33dddf84b7fdc26070add4aa0f4513094c8332afc4e`, confirmed consistent with the published checksums file per the maintainer's investigation in #2164 |
| Signed checksum / provenance (e.g., Cosign/Sigstore, SLSA attestation) | **Not found** | No signature file (`.sig`/`.pem`) accompanies the `v8.30.1` release assets, unlike TruffleHog's Cosign-signed checksums (see main packet §9/§18). **Unable to Verify** whether any CI-level provenance attestation (e.g., GitHub Artifact Attestations) exists outside the release-asset list itself; this was not separately queried via the Attestations API in this pass. |
| Signed tag / signed release commit | **Not found for the release-defining commit** | The commit the `v8.30.1` tag points to (`83d9cd6`, "update goreleaser") shows `verification.verified: false` via the Commits API — i.e., it is **not** GPG-signed, in contrast to numerous other repository commits (e.g., `b58d3f1`, `8d1f98c`) which do show `verification.verified: true` with a valid GPG signature. The tag's un-merged/orphaned relationship to `master` (per #2086 and the compare-API evidence in §A.1) is itself a form of unresolved provenance gap distinct from checksum integrity. |

**These four mechanisms are materially different and must not be treated as equivalent:** a
published checksum file and a matching GitHub asset digest together demonstrate that the artifact
you download matches what the project uploaded (integrity-in-transit), but neither one proves the
artifact was built from a commit that is verifiably part of the project's authoritative branch
history (provenance) or that the tagging action itself was cryptographically signed. For `v8.30.1`,
integrity-in-transit is well-supported (checksum + GitHub digest agree, per #2164's resolution and
this session's independent re-check); build/tag provenance is the weaker, still-open element (per
#2086, unresolved as of this research date).

**No evidence of Windows x64 asset replacement after initial publication** was found; the asset's
`created_at`/`updated_at` timestamps and digest are consistent with a single, unmodified upload on
2026-03-21, corroborated by the maintainer's direct statement in #2164 and this session's
independent re-verification of the currently live asset metadata.

## A.5 — v8.30.0 Comparison (release-integrity/correctness facts only — no downgrade recommendation)

| Fact | v8.30.1 | v8.30.0 |
|---|---|---|
| Release/tag provenance relative to current `master` | **Diverged** — tag commit `83d9cd6` is not reachable from `master` (`status: diverged`, confirmed via compare API) | **Clean ancestor** — tag commit `6eaad03` is reachable from `master` (`status: behind`, `behind_by: 27`) |
| Tag/release-defining commit signed? | **No** — `83d9cd6` ("update goreleaser") shows `verification.verified: false` | **Unable to Verify in this pass** — the `v8.30.0` tag commit's own signature status was not separately queried; other repository commits generally show GPG signing is used, but this specific commit was not individually checked |
| Windows x64 release artifact availability | Yes — `gitleaks_8.30.1_windows_x64.zip` | Yes — `gitleaks_8.30.0_windows_x64.zip` (8,519,574 bytes; digest `sha256:54fe94f644b832dd08e8c3a5915efb3bfa862386d59fb27ca0792cb687a83573`, retrieved directly from the current live Releases API) |
| Checksum publication | Yes — `gitleaks_8.30.1_checksums.txt` | Yes — `gitleaks_8.30.0_checksums.txt` |
| Known checksum discrepancy? | **Reported once (#2164), retracted by the reporter as a client-side download corruption; not reproducible against currently published/live assets** | **None found** — no equivalent issue was located for `v8.30.0` in this research pass |
| Known broad detection regression? | **Reported once (#2170), closed by the reporter after community analysis attributed it to a pre-existing, intentional stopword allowlist entry matching the reporter's specific test input, not a general regression** | **None found** — no equivalent issue was located for `v8.30.0` in this research pass |
| Relationship to master | Diverged (unresolved as of 2026-08-27) | Ancestor / clean |
| Material security fixes present only in v8.30.1? | **None found.** The full commit diff between the two tags (`compare/v8.30.0...v8.30.1`) contains exactly 4 commits: a Go-toolchain version bump (#2002), a small codec/encoding fix (#2020, 4 lines across 2 files), a report-template cleanup (#2040), and the un-merged "update goreleaser" tagging commit (`.goreleaser.yml` only). None of these touches `config/gitleaks.toml` (the detection rule set) or is described as a security fix in its commit message. | N/A (baseline) |
| Material detector/rule additions present only in v8.30.1? | **None found** — same 4-commit diff; no rule-file (`config/gitleaks.toml`) changes are present between the two tags | N/A (baseline) |

**Summary of this comparison (facts only, no recommendation):** the only code-level difference
between `v8.30.0` and `v8.30.1` is a Go-version bump, a 4-line codec fix, a report-template cleanup,
and a build/release-tooling change — none of which are security fixes or detection-rule changes.
`v8.30.0`'s tag is a clean, verifiable ancestor of current `master`, with no open integrity or
detection-correctness issues located in this research pass, whereas `v8.30.1`'s tag has an
open, maintainer-acknowledged, unresolved provenance gap (#2086) as of 2026-08-27. This is recorded
as a factual comparison only; per task scope, **no downgrade is recommended**.

## A.6 — Functional Correctness (Issue #2170) — Required Classification

Per the task's required category list, the reported non-detection in #2170 is classified as:

> **Incorrect test input** — the reporter's canonical example token happened to match an existing,
> intentional global-allowlist stopword (`abcdefghijklmnopqrstuvwxyz`, a sequential-alphabet
> placeholder-detection safeguard already present in the shipped default `config/gitleaks.toml`),
> causing an expected suppression rather than a genuine detection-engine failure.

This is **not**: a confirmed defect, a packaging/build defect, a deprecated-command issue, a
platform-specific issue, a Homebrew-only issue, or an unresolved/asset-replacement issue. It also
does not appear to be resolved *by a fix* (no code change was made) — it was resolved by
*explanation*, accepted by the original reporter. As noted in §A.3, no maintainer explanation was
posted for this specific issue, so the classification above rests on unrebutted, source-level
community analysis rather than an authoritative maintainer statement; this residual gap is recorded
as **Unable to Verify (maintainer-sourced confirmation)** even though the reporter's own closure is
consistent with the community's explanation.

## A.7 — Maintenance-Transition Clarification

- **Whether v8 remains supported for security fixes:** the Gitleaks README (master branch, both at
  the time of the original research pass and re-confirmed for this appendix) states: *"Gitleaks is
  feature complete. I'm not merging new features into Gitleaks. Future releases will be security
  patches only."* This is a direct statement that v8 (the current major version line, which
  includes `v8.30.1`) is expected to continue receiving **security patches**, but not new features
  or, by clear implication, routine non-security bug fixes such as the ones raised in #2086/#2170.
  This is consistent with — and provides additional context for — the observation that the
  orphaned-tag issue (#2086) has remained open and unfixed for several months without a maintainer
  commitment to a timeline.
- **Whether the maintainer identifies a replacement/successor:** yes — the same README banner names
  **"Betterleaks"** (linked to `github.com/betterleaks/betterleaks`) as where the maintainer is
  "shifting focus." Independently querying that repository's own GitHub metadata for this appendix
  confirms it exists, is public, and is described by its own maintainers as "Find leaked secrets
  everywhere" — i.e., a same-domain (secret-scanning) successor project. Per task scope, this
  successor is **not** evaluated as a new G007 candidate in this pass.
- **Whether this materially changes the maintenance assumptions already recorded:** **yes, this
  appendix adds material, more concrete evidence than the original packet's general "feature
  complete" observation.** The original research packet (§9, §19) already flagged the "feature
  complete / security-patches-only" posture as a maintenance consideration for independent review.
  This appendix adds two concrete, dated data points consistent with that posture in practice: (1) a
  maintainer-acknowledged release-process defect (#2086) has remained unfixed for over four months
  without a committed timeline, and (2) two of the three issues examined here were resolved entirely
  by unpaid third-party community members without any maintainer involvement, rather than by the
  maintainer. This is factual reinforcement of the existing maintenance-posture observation, not a
  new or different conclusion, and it does not by itself establish whether v8.30.1 is or is not
  admissible — that judgment remains for independent review and the project owner.

## A.8 — Unable-to-Verify Items (this appendix)

- Whether any CI-level provenance mechanism (e.g., GitHub Artifact Attestations, SLSA provenance)
  exists for `v8.30.1` outside the release-asset checksum file itself; the Attestations API was not
  separately queried in this pass.
- Whether the `v8.30.0` tag/release-defining commit is itself GPG-signed (only `v8.30.1`'s tag
  commit was individually checked and found unsigned).
- An authoritative, maintainer-sourced explanation for issue #2170's closure; the classification in
  §A.3/§A.6 rests on unrebutted community technical analysis and the reporter's own acceptance of it
  via closing the issue, not a maintainer statement.
- Whether any connection genuinely exists between #2086 (orphaned tag) and #2170 (reported
  non-detection); the reporter speculated a link, and community analysis reproduced the same
  stopword-driven behavior on current `master` (independent of which commit was tagged), which
  weighs against a causal link but was not addressed by a maintainer statement either way.

## A.9 — Recommendation Boundary (this appendix)

**No tool recommendation made. No tool admitted.** This appendix does not recommend admitting,
deferring, or rejecting Gitleaks at any version, and does not recommend a downgrade to `v8.30.0`. It
supplies additional factual, source-cited evidence for the project owner and independent reviewer to
weigh alongside the original neutral comparison packet.

## A.10 — Scope Confirmation (this appendix)

- No download of Gitleaks (any version) occurred.
- No installation of Gitleaks occurred.
- No execution of Gitleaks occurred.
- No KST repository content was scanned by Gitleaks or any other candidate.
- No credential verification was attempted against any provider.
- No `S0.3-G006` (SAST) work was performed.
- No `S0.3-G008` (SBOM) work was performed.
- All queries in this appendix used only public, generic GitHub API endpoints for the
  `gitleaks/gitleaks` and `betterleaks/betterleaks` repositories (issues, comments, commits, tags,
  releases, and compare endpoints); no KST source, filenames, hostnames, or internal details were
  sent externally.

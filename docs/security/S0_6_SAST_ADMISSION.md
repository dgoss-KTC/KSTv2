# S0.6 — Security Tool Admission: Capability Review 4 — Dedicated Static Application Security Testing (SAST)

**S0.6 Capability Review 4 — Dedicated Static Application Security Testing (SAST)**
**Status: COMPLETE / ACCEPTED — 2026-08-27**

| Item | Value |
|---|---|
| Gap | `S0.3-G006` |
| Tool (admitted) | Microsoft DevSkim CLI v1.0.90 |
| Owner admission decision | ADMITTED for installation and verification — 2026-08-27 |
| Implementation | **COMPLETE — 2026-08-27** (see §6 onward) |
| Project-owner acceptance | **ACCEPTED — 2026-08-27** |
| Microsoft DevSkim CLI v1.0.90 disposition | **ADMITTED / INSTALLED / VERIFIED / ACCEPTED** |
| Semgrep CE v1.175.0 disposition | DEFERRED pending organizational licensing review |
| CodeQL CLI v2.26.4 disposition | DEFERRED pending confirmed applicable private-repository entitlement and organizational authorization |
| `S0.3-G006` disposition | **Covered / Resolved** |
| Overall S0.6 status | **COMPLETE / ACCEPTED — 2026-08-27** — all four S0.6-assigned gaps (`S0.3-G001`, `S0.3-G006`, `S0.3-G007`, `S0.3-G008`) are now Covered / Resolved |
| Research evidence | `docs/security/S0_6_SAST_ADMISSION_RESEARCH.md` |
| Licensing authority | `docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md` |
| New S0.6 findings this pass | `S0.6-F020`, `S0.6-F021` (see §21); both Informational, neither Accepted Risk |

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

## 5. Pre-Installation Status (as of the committed admission-decision pass)

At the time the admission-decision commit (`b9b005dfe575ea7ae0d6c42766f652d95ff4f36f`, `docs:
admit DevSkim SAST capability`) was pushed, no SAST tool had yet been installed or executed on
the workstation. Implementation (licensing-gate verification, package acquisition, signature
verification, installation, rule-corpus verification, synthetic validation, and the local KST
scan) was then carried out in this same session and is recorded in §6 onward below, without
modifying §4's admission-decision record.

## 6. Licensing Gate (independently verified before installation)

Before installation, the exact admitted artifact's licensing evidence was independently confirmed
directly from authoritative NuGet.org sources (not merely from the research packet's prompt text):

| Attribute | Verified value | Source |
|---|---|---|
| Package ID | `Microsoft.CST.DevSkim.CLI` | NuGet V3 registration API (`registration5-semver1`) |
| Version | `1.0.90` | Same; also `nuspec` inside the downloaded package |
| Authors | Microsoft | NuGet catalog entry |
| License | **MIT** (SPDX `licenseExpression: "MIT"`) | NuGet catalog entry **and** `LICENSE.txt` bundled inside the verified `.nupkg` (Microsoft Corporation MIT License text) |
| Project/repository | `https://github.com/Microsoft/DevSkim` | NuGet catalog entry (`projectUrl`) |
| `requireLicenseAcceptance` | `false` (no click-through EULA) | NuGet catalog entry |
| Listed / delisted | `listed: true` | NuGet catalog entry |
| Package type | `DotnetTool` (developer CLI tool, not a library dependency) | `nuspec` `<packageTypes>` |
| Commercial/business-use restriction | None identified — permissive MIT license | `LICENSE.txt` verbatim text |
| Paid/seat/subscription requirement | None identified | NuGet distribution; no account/login required for install or use |
| Private-repository restriction | None identified | MIT license has no field-of-use restriction |
| NOTICE/attribution material | `LICENSE.txt` present in package root; no separate `NOTICE` file present | Package content inspection (§8) |

**Governance disposition:** MIT is a permissive OSI-approved license with no commercial-use
restriction, no paid/seat/subscription requirement, and no private-repository restriction — the
licensing gate **passes** under `docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md`.
This independently reproduces (does not merely trust) the research packet's MIT finding.

## 7. Package Provenance and Signature Verification

- **Official source used:** NuGet.org only, via `https://api.nuget.org/v3-flatcontainer/microsoft.cst.devskim.cli/1.0.90/microsoft.cst.devskim.cli.1.0.90.nupkg`.
- **Download location:** a disposable directory under the workstation `%TEMP%`, outside the KST
  repository (deleted after use — see §14).
- **File size:** 57,197,991 bytes.
- **Locally computed SHA-256 (of the downloaded `.nupkg`):** `4EECE7EBF6523C07EAD12C97E3F7539CBB6D6933CF267949DCB6728B23C9202C`.
- **Official NuGet `.nupkg.sha512` sidecar:** requested and returned HTTP 404 (not published for
  this package/version at the flat-container endpoint). Per governing instructions, no hash
  comparison was invented in the absence of an authoritative published hash in a reliably
  interpretable form — this is recorded as **Unable to Verify** for that specific
  independent-hash-comparison mechanism. Package integrity was instead established through the
  cryptographic signature verification below, which is a stronger, standard mechanism.
- **NuGet package signature verification** — `dotnet nuget verify <nupkg>` (installed .NET 10 SDK
  `dotnet nuget verify`, actual supported syntax used):
  - **Author signature:** present and valid — `CN=Microsoft Corporation, O=Microsoft Corporation,
    L=Redmond, S=Washington, C=US`, chained to DigiCert Trusted G4 Code Signing RSA4096 SHA384
    2021 CA1 → DigiCert Trusted Root G4; timestamped 2026-07-17.
  - **Repository countersignature:** present and valid — `CN=NuGet.org Repository by Microsoft`,
    service index `https://api.nuget.org/v3/index.json`, owners `Microsoft, nugettwcsecurity`;
    timestamped 2026-07-17. This is evidence **distinct** from the author signature (a genuine
    NuGet.org repository signature was actually present, not merely assumed).
  - **Result:** `Successfully verified package 'Microsoft.CST.DevSkim.CLI.1.0.90'.` (exit
    success). Both the author signature and the repository countersignature verified; no fallback
    to an unverified package was used or would have been used had verification failed.

## 8. Package Content Review

The verified `.nupkg` was inspected (not executed) by copying it to a `.zip` extension and
expanding it in the disposable download directory:

- **Identity/version:** `nuspec` confirms `Microsoft.CST.DevSkim.CLI` `1.0.90`, `license
  type="expression"` `MIT`, `packageType DotnetTool`, `repository` commit
  `fb2d676ce475a47c0338966bebd97a47ae566572` (matches the installed CLI's reported build id,
  `1.0.90+fb2d676ce4`).
- **No `<dependencies>` element in the `nuspec`** — the package does not declare external NuGet
  package dependencies to be resolved separately; it is fully self-contained. This was verified,
  not assumed. The bundled `tools/net10.0/any/devskim.deps.json` runtime manifest lists the
  application's own vendored/bundled component set (e.g. `Microsoft.CST.ApplicationInspector.*`,
  `Microsoft.CST.OAT`, `Sarif.Sdk`, `LibGit2Sharp`, `YamlDotNet`, `NLog`, `Serilog`, `Newtonsoft.Json`,
  and others) shipped inside the same package, not fetched separately at install time.
- **Bundled executable/runtime contents:** multi-target-framework tool binaries for `net8.0`,
  `net9.0`, and `net10.0`, each with per-RID native runtime assets (`win-x64`, `win-x86`,
  `win-arm64`, `linux-*`, `osx-*`) — a standard cross-platform .NET global tool layout.
  `devskim.exe` (162,816 bytes) is the entry point actually invoked.
  `Sarif.dll`/`Sarif.Sdk` is present, corroborating SARIF output support.
- **License material:** `LICENSE.txt` present at package root — verbatim Microsoft Corporation MIT
  License text, matching the `nuspec` license expression.
- **NOTICE material:** no separate `NOTICE` file was found in the package; none is required under
  the MIT license.
- **No install scripts requiring execution** were found (no `.ps1`/`init.ps1`/`install.ps1`
  content-execution hooks present in the package for `DotnetTool`-type packages of this kind).
- **Bundled/default rules:** not present as loose files in the package; see §9 — they are
  **embedded resources inside `Microsoft.DevSkim.dll`**, discovered via .NET reflection
  (`Assembly.GetManifestResourceNames()`), not invented or assumed from documentation.

## 9. Versioned Local Installation

- **Package source control:** a temporary `nuget.config` was created containing **only** a single
  local, file-system package source pointing at a disposable folder holding the one verified
  `.nupkg` (`<clear/>` followed by exactly one local `<add>`), so installation could not resolve
  or substitute any other remote artifact.
- **Install command (actual, using installed SDK help syntax):**
  `dotnet tool install Microsoft.CST.DevSkim.CLI --version 1.0.90 --tool-path
  "%LOCALAPPDATA%\KST\SecurityTools\devskim\1.0.90" --configfile <temp nuget.config>`
- **Result:** `Tool 'microsoft.cst.devskim.cli' (version '1.0.90') was successfully installed.`
- **Installed path (absolute):**
  `C:\Users\dgoss\AppData\Local\KST\SecurityTools\devskim\1.0.90\devskim.exe`
- **Version gate:** `<absolute-devskim> --version` → `devskim 1.0.90+fb2d676ce4` — matches the
  admitted/verified version exactly.
- **Administrator elevation:** **No** — `--tool-path` installs to a user-writable directory; no
  elevation was requested or required.
- **PATH modification:** **No** — the installed tool directory was not added to `PATH`; the tool
  was invoked throughout by its absolute path.
- **Repository tool-manifest changes:** **No** — `.config/dotnet-tools.json` does not exist in the
  KST repository (verified before and after installation); no project `PackageReference` was
  added; DevSkim is developer tooling, not an application dependency.
- **Installed package match:** the installed tool required exactly the verified package
  (`Microsoft.CST.DevSkim.CLI` `1.0.90`) from the single-source local feed; no substitution
  occurred.

## 10. Bundled Rule-Corpus Provenance and Self-Verification

- **Rule source:** the default/bundled DevSkim rules are **embedded resources inside
  `Microsoft.DevSkim.dll`** (shipped inside the same verified `.nupkg`), not a remote rule
  registry, not a downloaded rule pack, and not a KST-custom rule pack. This was determined by
  loading the installed, unmodified assembly via .NET reflection and enumerating
  `GetManifestResourceNames()` — **46 rule-content JSON resources** (plus
  `Microsoft.DevSkim.resources.comments.json` and `Microsoft.DevSkim.resources.languages.json`,
  48 embedded resources total), all named under the
  `Microsoft.DevSkim.rules.default.security.*` / `Microsoft.DevSkim.rules.default.correctness.*`
  namespace.
- **Rule versioning:** the rule resources are **not independently versioned** from the tool
  package — they are compiled into the same `Microsoft.DevSkim.dll` shipped in
  `Microsoft.CST.DevSkim.CLI` `1.0.90`; admitting the pinned tool version pins the rule corpus.
- **Rule count:** the 46 rule-content files contain **123 individual rule/language-variant
  entries** (per `devskim verify` below); of these, **91 have vendor-authored `must-match`
  self-tests** and **31 have vendor-authored `must-not-match` self-tests**.
- **Language coverage represented:** the bundled `languages.json` resource explicitly enumerates
  37 recognized languages, including `csharp`, `javascript`, `javascriptreact`, `typescript`,
  `typescriptreact`, `rust`, `sql`, `powershell`, `json`, `yaml`, and others — genuinely confirmed
  from the shipped resource, not assumed from README prose.
- **Rule hash inventory:** a local SHA-256 hash was computed for all 48 extracted embedded
  resource files during this pass (disposable evidence; not retained — see §14). This
  demonstrates the exact rule content used in this pass is reproducible/auditable from the
  installed package, without asserting a specific hash value as durable evidence beyond this run.
- **DevSkim's own rule self-verification (`devskim verify`):**
  - Command: `devskim verify` (no `-r`) — result: `Error: No rules were loaded.` The CLI's
    `verify` command requires an explicit `-r <rules-path>` and does **not** load the embedded
    default rules implicitly. This is recorded as a genuine capability observation: **Unable to
    Verify via built-in rule self-test without first extracting the rules**, per the governing
    instructions' anticipated outcome.
  - The 46 rule-content resources were then extracted **unmodified** (via reflection, byte-exact
    copies) to a disposable local folder and fed back to `devskim verify -r <folder>` — this
    constitutes genuine self-verification of the actual shipped rule content, without
    modification or custom-rule creation.
  - Result: `91 of 123 rules have must-match self-tests.` / `31 of 123 rules have must-not-match
    self-tests.`; exit code `0`; no failure or error lines reported for any rule's self-test.

## 11. Synthetic Validation (disposable, outside the KST repository)

Before scanning KST, the exact installed v1.0.90 scanner was validated against disposable
synthetic examples in `%TEMP%`, using **only** the bundled/default rules (no custom rules). Test
constructs were derived directly from each selected rule's own vendor-authored `must-match` /
`must-not-match` fields (genuine rule content, not invented), one representative rule per
required language:

| Language | Rule ID | Rule name | Source of expected pattern | Vulnerable canary | Safe control |
|---|---|---|---|---|---|
| C# | `DS106864` | Do not use the DES symmetric block cipher | rule's own `must-match`/`must-not-match` | **PASS** (detected) | **PASS** (not detected) |
| JavaScript/TypeScript | `DS189424` | Review `eval` for untrusted data | rule's own `must-match` (`c = eval(a+b)`); rule ships no `must-not-match`, so a minimal non-`eval` safe control was constructed for this pass | **PASS** (detected) | **PASS** (not detected) |
| Rust | `DS440030` | Rust — failure to specify a minimum TLS version | rule's own `must-match`/`must-not-match` (`native_tls::TlsAcceptor::builder` with/without a `Tlsv12`+ minimum) | **PASS** (detected) | **PASS** (not detected for the targeted rule; see note below) |
| SQL | `DS224000` | Dangerous T-SQL command (e.g. `xp_cmdshell`) | rule's own `must-match`/`must-not-match` | **PASS** (detected) | **PASS** (not detected) |

All four required languages (C#, JavaScript/TypeScript, Rust, SQL) produced genuine built-in-rule
detections in this exact installation; no capability gap requiring a STOP was identified for any
of the four required languages. PowerShell was not additionally tested (not currently a
significant KST first-party source surface; C#/TS/Rust/SQL are the primary KST languages per
repository inspection).

**Rust clean-control note:** the *targeted* rule (`DS440030`) correctly did **not** fire on the
safe control (`Tlsv12` minimum specified). A separate, broader **generic parent rule**
(`DS440000`, "Generic: Do not hardcode SSL/TLS versions", which `DS440030`/`DS440073` both
`override`) matched **both** the vulnerable canary and the safe control, because it pattern-matches
on any use of the TLS-acceptor-builder API regardless of version. This is recorded as observed
tool behavior (a broader, language-agnostic rule co-firing alongside a more specific rule), not a
failure of the targeted rule's clean-control test.

All disposable synthetic files, the two SARIF outputs from this validation, and the extracted
rule-content folder were deleted after evidence was extracted (§14); no vulnerable snippets are
reproduced above beyond the minimal canary constructs already present verbatim in the shipped
rule definitions themselves.

## 12. First KST Scan

- **Pre-scan repository state:** `git status --short` — clean tracked working tree (verified
  immediately before scanning).
- **Exact command:**
  `devskim analyze -I <repo-root> -O <outside-repo>\kst_run1.sarif -f sarif
  --skip-git-ignored-files --skip-excerpts -x Information`
- **Scan boundary:** the full KST repository tree, rooted at the repository root, restricted by
  `--skip-git-ignored-files` (requires and used the locally installed `git`; confirmed no
  git-ignored paths such as `bin/`, `obj/`, `node_modules/`, `target/` were analyzed) plus the
  CLI's own default ignore globs (`**/.git/**`, `**/bin/**`).
- **Exit code:** `0`.
- **Duration:** ~47.3 seconds (wall-clock, this workstation).
- **Total finding count:** **50** (identical on both runs — see §15 repeatability).
- **Count by rule ID:**

  | Rule ID | Rule name | Count | Vendor severity | Vendor confidence |
  |---|---|---|---|---|
  | `DS162092` | Do not leave debug code in production (accessing `localhost`/`127.0.0.1`) | 31 | ManualReview | High |
  | `DS172411` | Review `setTimeout` for untrusted data | 11 | ManualReview | High |
  | `DS137138` | Insecure URL (`http://` without TLS) | 8 | Moderate | High |

Vendor severity/confidence are DevSkim's own tool metadata (`properties.DevSkimSeverity` /
`properties.DevSkimConfidence` in the SARIF); they are **not** translated into a KST
organizational severity by this document.

## 13. KST Findings — Safe Metadata and Evidence-Based Classification

Rule patterns were independently inspected (from the extracted, unmodified rule JSON, §10) to
support evidence-based classification rather than assumption. File paths below are
repository-relative as emitted in the SARIF (`--skip-excerpts` was used; no source snippets are
present in the SARIF and none are reproduced here).

**`DS162092` (31 findings) — plain string match on the literals `"localhost"` / `"127.0.0.1"`**
anywhere in code (no data-flow awareness). Affected files: `src/backend/Kst.Api/Program.cs`,
`src/backend/Kst.Api/appsettings.json`, `src/backend/tests/Kst.Api.IntegrationTests/
CorsPolicyTests.cs`, `src/backend/tests/Kst.Api.IntegrationTests/LoopbackBindingTests.cs`,
`src/frontend/src/api/tauri-bridge.ts`, `src/frontend/src/test-setup.ts`, `src/tauri/src/lib.rs`,
`src/tauri/tauri.conf.json`. **Classification: Likely False Positive / Informational Scanner
Behavior** — KST's Tauri sidecar architecture intentionally binds its local API to loopback only
(a security control, previously exercised by the accepted S0.4B/S0.5 loopback-binding regression
tests referenced by these same file names); the rule is a bare string-literal match with no
awareness of that intentional design. This is not reclassified as safe merely because DevSkim is
regex-based — it is classified based on the actual rule pattern (plain string match) and the
files' known role in KST's accepted architecture.

**`DS137138` (8 findings) — regex match on `http://` URLs**, with the rule's own negate-conditions
already excluding `http://localhost` and `http://127.0.0.1` substrings and common XML-namespace
contexts. Affected: `src/backend/Kst.Api/Program.cs` (1, line 301), `src/backend/tests/
Kst.Api.IntegrationTests/CorsPolicyTests.cs` (4, lines 24/54/60/69), and three Tauri-generated
schema files under `src/tauri/gen/schemas/` (1 each: `windows-schema.json`,
`desktop-schema.json`, `acl-manifests.json`).
  - The three `src/tauri/gen/schemas/*.json` findings were confirmed (by direct inspection) to be
    the standard `"$schema": "http://json-schema.org/draft-07/schema#"` JSON Schema meta-schema
    URI emitted by Tauri's own schema-generation tooling — a spec-mandated, non-TLS URI by JSON
    Schema draft-07 convention. **Classification: Generated-Code Finding / Informational** (not
    excluded from the scan boundary per governing instructions; classified, not silently dropped).
  - `Program.cs` line 301 and `CorsPolicyTests.cs` line 24 both contain the literal CORS-allowed
    origin `"http://tauri.localhost"` (a Tauri-internal desktop-webview custom-scheme origin,
    distinct from `localhost`/`127.0.0.1` and therefore not excluded by the rule's own negate
    conditions). **Classification (updated 2026-08-27, see §25): Informational / Framework-Local
    Origin / Confirmed DevSkim False Positive for plaintext-network interpretation** — recorded as
    `S0.6-F020` (see §21 and §25); no remediation performed or required.

**`DS172411` (11 findings) — regex `\bsetTimeout\(([^,]+)\)`** (matches only when no comma
appears between `setTimeout(` and its closing `)`). Affected: 9 files under
`src/frontend/src/hooks/*.ts`. Direct inspection of a representative match
(`src/frontend/src/hooks/useToasts.ts`, `setTimeout(() => dismissToast(id), AUTO_DISMISS_MS);`)
shows the rule's naive regex does not track balanced parentheses: it matches through the nested
call `dismissToast(id)`'s closing `)` as if it were `setTimeout`'s own closing paren, so it
misidentifies an ordinary two-argument `setTimeout(callback, delayMs)` call (with a numeric/
internal delay constant, not untrusted external data) as a single-argument call. **Classification:
Likely False Positive**, evidence-based (confirmed by direct inspection of the matched code, not
asserted merely because the rule is regex-based) — recorded as a durable capability observation,
`S0.6-F021` (see §21), because this pattern is expected to recur on essentially all standard
two-argument `setTimeout` calls in the KST frontend.

No finding above contains or appears to contain credential material; none was withheld from
reproduction here on that basis (all are URLs, string literals, or code structure — see §16).

## 14. Material Finding Gate

Per governing instructions, no finding above was remediated in this pass. The `S0.6-F020`
(`http://tauri.localhost` non-TLS CORS origin) finding was initially recorded as a plausible,
low-materiality item warranting owner review of whether the existing CORS allowlist should also
carry an HTTPS variant or whether the Tauri custom-scheme origin already provides equivalent
protection. A subsequent narrow evidence-review pass (§25, 2026-08-27) independently established
the locked Tauri version, upstream advisory applicability, Windows custom-protocol origin
semantics, the exact KST CORS scope, and the separateness of the backend network listener, and
reclassified the finding as Informational (see §25). No remediation was performed or is required
as a result of that review.

## 15. SARIF Verification

- **Validity:** both `kst_run1.sarif` and `kst_run2.sarif` parsed as valid JSON.
- **SARIF version:** `2.1.0` (`$schema`:
  `https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.6.json`).
- **Tool driver name/version:** `devskim` / `1.0.90+fb2d676ce4`.
- **Rule IDs present:** `DS162092`, `DS172411`, `DS137138` (of 123 loaded rule definitions; most
  produced zero KST matches).
- **Result count:** 50 (both runs).
- **Path behavior:** relative, repository-relative paths (e.g.
  `src\backend\Kst.Api\Program.cs`); no absolute developer path was present in the SARIF (verified
  by direct string search for `C:\Users`/`C:\Dev`-style absolute paths — none found outside this
  evidence document itself).
- **Source excerpt/snippet behavior:** `--skip-excerpts` was supported and used; each result's
  `region.snippet.text` was the empty string — confirmed no source excerpts were embedded.
- **Fingerprints:** DevSkim v1.0.90 does **not** emit SARIF `partialFingerprints` for these
  results (absent from every inspected result object) — recorded as a capability limitation
  relevant to future repeat-scan diffing (no fingerprint-based tracking is available; only
  rule ID + path + line can be used as a natural key, as done in §16 below).
- **Formal schema validation status:** **syntactic/structural verification only** — no
  already-admitted formal SARIF-schema validator was available/used in this pass; this is not
  claimed as formal schema validation.

## 16. Sensitive-Output Review

The SARIF outputs (generated outside the repository, in a disposable `%TEMP%` folder, never
uploaded) were inspected for:

- **Absolute username paths:** none found in the SARIF content itself (a match on the workstation
  username occurred only in the *file system path* of the disposable output folder used to store
  the report, not inside the SARIF content).
- **Machine hostname:** none found.
- **Internal repository URLs:** the SARIF's standard `versionControlProvenance` block includes the
  actual git remote (`https://github.com/dgoss-KTC/KSTv2.git`), current commit, and branch — this
  is the repository's own already-public GitHub identity (consistent with the environment's known
  repository identity), not an internal/sensitive value; recorded as a non-sensitive category, not
  a finding.
- **Source snippets:** none (see §15, `--skip-excerpts`).
- **Hardcoded credential-like strings:** a direct search for `password`/`apikey`/`secret` (case
  -insensitive) in the SARIF content returned no matches.
- **Customer identifiers/data, internal database/server names:** none observed among the 50
  findings (all are code-structure/URL/localhost matches — see §13).

No sensitive value was reproduced in this document beyond what is already summarized above
(categories and bounded counts only). The report was not uploaded.

## 17. Repeatability

- **Run 1 count:** 50. **Run 2 count:** 50 (identical command/options, same repository state).
- **Same finding set:** **Yes** — a set comparison keyed on `ruleId|relativePath|startLine`
  produced **zero** differences between the two runs.
- **Fingerprint stability:** **Unavailable** — DevSkim v1.0.90 does not emit
  `partialFingerprints` for these results (see §15); stability was instead confirmed via the
  rule/path/line natural key above.
- **Material differences:** none.

## 18. Network / Data-Handling Observation

- **Package acquisition (separate from scan execution):** required one HTTPS download from
  `api.nuget.org` (official NuGet.org infrastructure) to obtain the verified `.nupkg`; no
  account/login was used or required.
- **Scanner execution (`devskim analyze` / `devskim verify`):** required no login, no account, no
  token. The only external-process interaction observed was the CLI's own use of the locally
  installed `git` executable (for `--skip-git-ignored-files`) — a local, already-installed tool,
  not a network call. No evidence of rule download, cloud analysis, or source/SARIF upload was
  observed during scan execution. Bounded wording: **no intentional source upload; no cloud
  analysis configured; bundled/default rules used locally.** No packet-level forensic capture was
  performed, and no claim beyond ordinary process-level observation is made.

## 19. Repository Integrity

`git status --short`, `git diff --name-status`, and `git diff --stat` were run immediately before
and immediately after both KST scans; the tracked working tree was clean before and after in both
cases. DevSkim did not modify any tracked KST file.

## 20. Temporary Artifact Cleanup

The following disposable artifacts were deleted after evidence was extracted into this document:
the downloaded `.nupkg` and its extracted contents, the temporary single-source `nuget.config`,
the extracted rule-content folder and its SHA-256 hash inventory, all synthetic
vulnerable/clean test files and their SARIF outputs, and both KST-scan SARIF reports (`kst_run1
.sarif`, `kst_run2.sarif`). Only the installed, versioned DevSkim CLI directory
(`%LOCALAPPDATA%\KST\SecurityTools\devskim\1.0.90\`) was retained, per governing instructions.

## 21. New S0.6 Findings

The highest previously assigned S0.6 finding ID was `S0.6-F019`
(`docs/security/S0_6_SBOM_ADMISSION.md` §9). Two genuinely observed findings from this pass
warrant durable tracking:

| Finding | Description | KST-blocking? | Disposition |
|---|---|---|---|
| `S0.6-F020` | The KST CORS allowlist (`src/backend/Kst.Api/Program.cs`, mirrored in `src/backend/tests/Kst.Api.IntegrationTests/CorsPolicyTests.cs`) includes the non-TLS origin `http://tauri.localhost` alongside its HTTPS counterpart `https://tauri.localhost`; DevSkim's `DS137138` (Insecure URL) rule correctly does not exclude this Tauri custom-scheme origin (only `http://localhost`/`http://127.0.0.1` are excluded by the rule's own conditions) | No (reviewed 2026-08-27 — see §25; not confirmed vulnerable — `http://tauri.localhost` is Tauri's Windows-mapped framework-local WebView origin, not internet-routable HTTP traffic) | **Informational / Framework-Local Origin / Confirmed DevSkim False Positive for plaintext-network interpretation** — see §25 for full evidence; not remediated (none required); not Accepted Risk |
| `S0.6-F021` | DevSkim's bundled `DS172411` (Review `setTimeout` for untrusted data) rule uses a naive regex (`\bsetTimeout\(([^,]+)\)`) that does not track balanced parentheses in its argument capture; on ordinary two-argument `setTimeout(callback, delayMs)` calls whose callback itself contains a nested function call (a common KST frontend pattern in `src/frontend/src/hooks/*.ts`), the rule's capture group extends through the nested call's closing parenthesis and produces a match even though the call has a literal/internal second argument, not untrusted external data | No | Informational / Known Capability Limitation — not an Accepted Risk; a durable interpretation note for future DevSkim scans of KST (unchanged by the 2026-08-27 F020 review pass, §25) |

Neither finding was assigned an organizational severity (enacted policy does not currently
authorize this document to assign one); neither is Accepted Risk; no suppression, baseline, or
custom rule was created to address either.

## 22. Capability Boundary (preserved)

Consistent with the owner's bounded admission (§4.1), this implementation evidence does **not**
establish: deep semantic analysis; cross-file taint tracking; interprocedural source-to-sink
proof; complete vulnerability detection; Tauri-specific security assurance beyond what the
existing accepted S0.4B/S0.5 evidence already covers; a replacement for security architecture
tests or code review; or a replacement for Gitleaks/cargo-audit/Syft. DevSkim v1.0.90 is a
pattern/regex-based security-linting capability, demonstrated in this pass to genuinely detect
representative bundled-rule canaries across C#, JavaScript/TypeScript, Rust, and SQL, and to
produce stable, repeatable, structurally valid local SARIF output against KST's own tracked
source tree.

## 23. Unable-to-Verify Items

- Exact-version NuGet `.nupkg.sha512` sidecar hash — endpoint returned 404 for this
  package/version; integrity was instead established via `dotnet nuget verify`'s dual
  author+repository signature verification (§7).
- `devskim verify`'s applicability to the embedded default rules **without** first extracting
  them — the command requires an explicit `-r` rules path and does not implicitly self-test the
  embedded default corpus (§10).
- Formal SARIF 2.1.0 JSON-schema validation — only syntactic/structural verification was
  performed; no already-admitted formal validator was used (§15).
- Packet-level network forensics during scan execution — only ordinary process-level observation
  was performed (§18).

## 24. `S0.3-G006` Disposition

**Covered / Resolved.** The project owner has explicitly reviewed and accepted the genuine
Microsoft DevSkim CLI v1.0.90 implementation evidence in this document and the completed
`S0.6-F020` narrow evidence review (§25) — see §26. Capability Review 4 is **COMPLETE /
ACCEPTED — 2026-08-27**.

## 25. `S0.6-F020` Narrow Human-Review Evidence Pass (2026-08-27)

This section records a subsequent, narrowly scoped evidence-review pass for `S0.6-F020` only. No
DevSkim scan was rerun; no source, configuration, or test file was modified; no remediation was
performed. This pass is evidence review, not remediation. At the time this pass was performed, it
did not itself change Capability Review 4's status or `S0.3-G006`'s disposition; both were
subsequently updated to COMPLETE / ACCEPTED and Covered / Resolved respectively by the explicit
project-owner acceptance decision recorded in §26.

### 25.1 Locked Tauri Version

From `src/tauri/Cargo.lock` (the actual resolved/locked dependency graph, not `Cargo.toml`'s
version ranges):

| Crate | Locked version |
|---|---|
| `tauri` | **2.11.5** |
| `tauri-build` | 2.6.3 |
| `tauri-runtime` | 2.11.3 |
| `tauri-runtime-wry` | 2.11.4 |
| `tauri-utils` | 2.9.3 |
| `wry` | 0.55.1 |

### 25.2 GHSA-7gmj-67g7-phm9 Applicability

Independently retrieved (not merely trusted from prompt text) from the OSV.dev structured
vulnerability database (`https://api.osv.dev/v1/vulns/GHSA-7gmj-67g7-phm9`, GitHub-reviewed,
aliased `CVE-2026-42184`):

- **Summary:** a flaw in Tauri's `is_local_url()` function on Windows/Android incorrectly
  classifies certain remote URLs as trusted local origins, because the check only inspects the
  first label of the domain (e.g. a registered `app` protocol would incorrectly trust
  `http://app.evil.com`, not only the genuine `http://app.localhost`).
- **Affected range (structured SEMVER data):** introduced `2.0.0`, fixed `2.11.1`
  (`last_known_affected_version_range: "<= 2.11.0"`).
- **KST's locked version (`tauri` `2.11.5`) is greater than or equal to the fixed version
  (`2.11.1`) and is therefore OUTSIDE the affected range** — the locked Tauri build already
  contains the upstream fix for this advisory.
- Per the governing task instructions, if the locked version had been in the affected range this
  review would have stopped and classified `S0.6-F020` as requiring security remediation review.
  That branch does not apply here.
- This does not prove the absence of any other or future Tauri origin-handling vulnerability; it
  establishes only that this specific, named, upstream-reviewed advisory does not apply to the
  currently locked version.

### 25.3 Windows Custom-Protocol / Origin Semantics

The same GitHub-reviewed advisory (§25.2) directly documents the general Tauri mechanism relevant
here, independent of the vulnerability itself: **on Windows and Android, Tauri maps a registered
custom URI scheme protocol to an `http://<scheme>.localhost/` WebView origin**, because those
platforms' WebView implementations cannot serve arbitrary custom URI schemes directly (example
given in the advisory: a registered `app://` scheme becomes `http://app.localhost/` on
Windows/Android). This is the same mapping mechanism Tauri's own official CSP documentation
illustrates with the built-in `ipc`/`asset` protocols (`http://ipc.localhost`,
`http://asset.localhost`).

KST's `src/tauri/tauri.conf.json` and `src/tauri/src/lib.rs` do not register any custom URI scheme
protocol (no `register_uri_scheme_protocol` call and no scheme override present) — the application
therefore uses Tauri's own default IPC scheme, whose name is `tauri`. Applying the documented
mapping, the default scheme's Windows WebView origin is `http://tauri.localhost`, which is exactly
the string flagged by DevSkim. This is corroborated by KST's own source: `src/backend/Kst.Api/
Program.cs` (CORS policy) and `src/backend/tests/Kst.Api.IntegrationTests/CorsPolicyTests.cs`
already list `tauri://localhost` (the non-Windows/non-Android form of the same default-scheme
origin), `http://tauri.localhost`, and `https://tauri.localhost` together as the same logical
Tauri-frontend origin across platforms/build configurations — internally consistent evidence that
this is a framework-mapped origin, not an arbitrary or ad hoc string.

**Explicitly distinguished, per task instructions:**
- **Not an HTTP server listening on a network interface** — `tauri.localhost` is not a real,
  network-routable DNS name; `.localhost` is a reserved special-use TLD (RFC 6761) that does not
  resolve over the public Internet. The `http://tauri.localhost` string is consumed entirely
  within the local WebView's own origin/protocol-handling logic on the same machine, not dispatched
  to any network socket.
- **Not ordinary plaintext Internet HTTP** — no genuine remote plaintext HTTP transport was
  identified for this origin; DevSkim's `DS137138` rule pattern-matches the literal `http://`
  prefix without capability to distinguish a framework-local WebView origin from a real remote
  endpoint.
- **Not the ASP.NET Core loopback API listener** — this origin is a **frontend/WebView-side**
  identifier used by the browser/WebView engine for same-origin and CORS decisions. It is entirely
  distinct from the **backend's** own network listener address (§25.5), which is a separate,
  independently configured mechanism.

### 25.4 KST CORS Scope

`src/backend/Kst.Api/Program.cs` (`AddCors` / `FrontendCorsPolicy`) uses `.WithOrigins(...)` with
exactly five literal origin strings:

```
http://localhost:1420
http://127.0.0.1:1420
tauri://localhost
http://tauri.localhost
https://tauri.localhost
```

- **Exact named origins** — `WithOrigins` requires exact string matches; there is no `*`,
  `http://*`, `*.localhost`, or other wildcard/pattern origin anywhere in the policy.
- **No `AllowAnyOrigin()`** and **no `AllowCredentials()`** call present in `Program.cs`.
- This is independently confirmed by the already-accepted S0.5 regression evidence:
  `src/backend/tests/Kst.Api.IntegrationTests/CorsPolicyTests.cs` asserts, structurally
  (`Effective_Cors_Configuration_Matches_Accepted_S0_Surface`), that the effective registered
  policy contains exactly these five origins, `AllowAnyOrigin == false`,
  `SupportsCredentials == false`, `AllowAnyHeader == true`, `AllowAnyMethod == true` — and,
  behaviorally, that `http://tauri.localhost` is one of five accepted origins that correctly
  receives an echoed `Access-Control-Allow-Origin` header while an untrusted origin
  (`https://untrusted.example.com`) does not. `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md`
  §"Mutation Testing" records that deliberately adding `AllowAnyOrigin()` or a sixth origin to this
  policy was mutation-tested and correctly caused these tests to fail — i.e. this is a genuinely
  enforced regression boundary, not merely a documentation claim.
- All previously accepted S0.2/S0.3/S0.5 CORS properties remain true: no `AllowAnyOrigin`, no
  wildcard origin, no credential allowance, exact expected origin set of five.

### 25.5 Backend Binding Remains Separate

`src/backend/Kst.Api/Program.cs` binds the backend via
`builder.WebHost.UseUrls($"http://127.0.0.1:{listenPort}")` whenever `ASPNETCORE_URLS` is not set
(the effective desktop launch path — the Tauri sidecar manager does not set that variable). This
is independently, behaviorally verified (not merely statically observed) by
`src/backend/tests/Kst.Api.IntegrationTests/LoopbackBindingTests.cs`
(`Backend_Process_Binds_To_Loopback_Only`), which launches the actual built `Kst.Api.exe` the same
way the repository starts it and inspects the OS TCP listener table to assert the socket is bound
to `127.0.0.1`, failing if the effective binding becomes `0.0.0.0`, `::`, a wildcard, or a LAN
address. `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md` records this as
mutation-tested: changing the bind host to `0.0.0.0` was deliberately introduced and correctly
failed this test.

**Answer to the governing question — does allowing the Tauri WebView origin in CORS expose the
backend to a remote network origin?** **No.** CORS and network binding are two independent
mechanisms in this architecture:
- **Network binding** (`UseUrls`) controls which network interface(s) the OS socket accepts
  connections on. It is unaffected by the CORS policy's allowed-origin list; adding, removing, or
  changing `WithOrigins(...)` entries does not change what interface the Kestrel listener binds
  to.
- **CORS** (`AddCors` / `WithOrigins`) is a **WebView/browser-enforced, response-header-based**
  access-control mechanism that governs whether a WebView's own script code is permitted to read a
  cross-origin response. It does not itself accept or route network connections, and it does not
  widen the backend's listening interface.
- Because the backend remains bound to `127.0.0.1` regardless of the CORS allowlist, a genuinely
  remote network origin cannot reach the backend socket at all merely because its origin string
  happens to appear (or not appear) in the CORS allowlist; the loopback binding is the actual
  network-exposure control, and it is unchanged by, and independent of, the CORS entries reviewed
  here.
- Preserved per instructions: an operator-set `ASPNETCORE_URLS` environment-variable override
  remains a separate, already-documented S0.7 (packaged/installed runtime listener) concern, not
  addressed or altered by this review.

### 25.6 Recency Note

Because KST's locked `tauri` (`2.11.5`) is `>= 2.11.1`, the specific, named
`GHSA-7gmj-67g7-phm9` / `CVE-2026-42184` affected-range issue does **not** apply to the locked
version (§25.2). This is recorded as applicable to this one named advisory only; it does not
constitute a general claim that no future Tauri origin-handling vulnerability could exist, and is
not a substitute for the ordinary cargo-audit/dependency-advisory capability already admitted
under `S0.3-G001` (`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`).

### 25.7 `S0.6-F020` Disposition (Updated)

All five conditions required by the governing task for reclassification are established by §25.1–
§25.5 above:

1. Locked Tauri version (`2.11.5`) is outside the known `GHSA-7gmj-67g7-phm9` affected range
   (`<= 2.11.0`) — established, §25.2.
2. `http://tauri.localhost` is the expected Windows Tauri custom-protocol WebView origin for KST's
   (default, unregistered-custom-scheme) `tauri` scheme — established, §25.3.
3. CORS permits that exact origin only, as one of five exact named origins — established, §25.4.
4. No wildcard or credential expansion exists in the CORS policy — established, §25.4.
5. The backend listener remains loopback-only (`127.0.0.1`), independent of the CORS allowlist —
   established, §25.5.

**Updated disposition:** `S0.6-F020` is reclassified from *Needs Human Review* to:

> **Informational / Framework-Local Origin / Confirmed DevSkim False Positive for
> plaintext-network interpretation**
>
> DevSkim DS137138 flags the literal HTTP scheme in `http://tauri.localhost`. On Windows, Tauri
> maps its local custom protocol to an `http://<scheme>.localhost` WebView origin. KST permits this
> exact framework-local origin in CORS while the backend remains loopback-only. The locked Tauri
> version is outside the reviewed affected range for GHSA-7gmj-67g7-phm9. No ordinary plaintext
> remote HTTP transport was identified by this finding.

This is explicitly **not** classified as Accepted Risk (no risk was accepted; the finding is
determined, on the evidence above, not to represent the plaintext-remote-HTTP condition the
underlying DevSkim rule is designed to detect). No suppression, baseline entry, or code change was
made. This disposition does not itself constitute project-owner acceptance of Capability Review 4
or of `S0.3-G006`.

## 26. Project-Owner Acceptance — 2026-08-27

The project owner has reviewed and explicitly **ACCEPTS** the genuine Microsoft DevSkim CLI
v1.0.90 implementation evidence recorded in §6 through §23 above, and the completed `S0.6-F020`
narrow evidence review recorded in §25. No implementation evidence was rerun, reconstructed, or
altered to obtain this acceptance; the acceptance applies to the evidence as genuinely established
in the prior implementation and evidence-review passes.

| Item | Disposition |
|---|---|
| **S0.6 Capability Review 4 — Dedicated Static Application Security Testing (SAST)** | **COMPLETE / ACCEPTED — 2026-08-27** |
| Microsoft DevSkim CLI v1.0.90 | **ADMITTED / INSTALLED / VERIFIED / ACCEPTED** |
| `S0.3-G006` | **Covered / Resolved** |
| `S0.6-F020` | Informational / Framework-Local Origin / Confirmed DevSkim False Positive for plaintext-network interpretation — **not** Accepted Risk |
| `S0.6-F021` | Informational / Known DevSkim Rule Limitation — **not** Accepted Risk |
| Semgrep CE v1.175.0 | **DEFERRED** pending organizational licensing review (not rejected; not reconsidered in this pass) |
| CodeQL CLI v2.26.4 | **DEFERRED** pending confirmed applicable private-repository entitlement and organizational authorization (not rejected; not reconsidered in this pass) |

### 26.1 S0.6 Overall Closeout

With Capability Review 4 now COMPLETE / ACCEPTED, all four S0.6-assigned gaps are Covered /
Resolved:

| Gap | Capability Review | Disposition |
|---|---|---|
| `S0.3-G001` (Rust dependency advisory) | Capability Review 1 | Covered / Resolved (`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`) |
| `S0.3-G006` (Dedicated SAST) | Capability Review 4 | Covered / Resolved (this document) |
| `S0.3-G007` (Dedicated secret scanning) | Capability Review 2 | Covered / Resolved (`docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`) |
| `S0.3-G008` (SBOM) | Capability Review 3 | Covered / Resolved (`docs/security/S0_6_SBOM_ADMISSION.md`) |

Accordingly: **S0.6 — Security Tool Admission is COMPLETE / ACCEPTED — 2026-08-27.**

`S0.7` (Runtime & Infrastructure Verification) and `S0.8` (Independent Assurance & S0 Closeout)
remain **NOT STARTED**; **Stage 9 remains BLOCKED PENDING S0 CLOSEOUT**. No S0.7, S0.8, or Stage 9
work was performed or begun by this document or this acceptance decision.

### 26.2 DevSkim Capability Boundary (preserved at acceptance)

This acceptance explicitly preserves, and does not expand, the capability boundary recorded in
§22: DevSkim provides dedicated static security linting using the admitted bundled/default rule
corpus. It does **not** establish deep semantic analysis, cross-file taint tracking,
interprocedural source-to-sink proof, complete vulnerability detection, complete Tauri-specific
security assurance, or a replacement for architecture regression tests, code review, cargo-audit,
Gitleaks, or the SBOM capability. These are capability boundaries, not findings, and are not
classified as Accepted Risk.

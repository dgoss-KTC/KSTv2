# S0.6 Capability Review 4 — Dedicated Static Application Security Testing (SAST)

**Status: RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW / NO TOOL ADMITTED**

| Item | Value |
|---|---|
| Gap addressed | `S0.3-G006` — dedicated SAST capability |
| Research date | 2026-08-27 |
| Starting commit | `fb5b6c98f26dd7f5d5c13527695cb317a9e87843` (subject: "docs: accept SBOM capability") |
| Overall S0.6 status | **IN PROGRESS** (Capability Review 1 — Rust Dependency Advisory — COMPLETE/ACCEPTED; Capability Review 2 — Dedicated Secret Scanning — COMPLETE/ACCEPTED; Capability Review 3 — SBOM — COMPLETE/ACCEPTED; this review closes no capability by itself) |

This document is **research evidence only**. It does not recommend, admit, install, download, or
execute any candidate SAST tool. The actual admission decision belongs to the project owner,
informed by an independent review of this packet. No `ADMIT`/`REJECT`, preferred candidate, or
"winner" is stated anywhere in this document.

---

## 1. Purpose / Authority Boundary

Per the established S0.6 process (see `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`,
`docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md`,
`docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`), three roles remain intentionally separated:

- **Research agent (this document):** establish current facts and compare credible capabilities.
- **Independent reviewer:** evaluate KST fit and formulate a recommendation.
- **Project owner:** make the actual admission decision.

This document does not write `ADMIT`, `REJECT`, "preferred candidate", "recommended tool", "best
option", "KST should use", "winner", or any ranking. No SAST tool was installed or executed while
preparing this packet.

## 2. Governing Authority

- `AGENTS.md` (Tier 1), `SECURITY.md` (Tier 1).
- `docs/security/SECURITY_ASSURANCE_POLICY.md`, `docs/security/DEPENDENCY_ADMISSION.md`,
  `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`, `docs/security/AI_SECURITY_REVIEW.md`,
  `docs/security/APPLICATION_SECURITY_PROFILE.md` (Tier 1 normative policy documents).
- `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §8 (S0.6 — Security Tool
  Admission): one-capability-at-a-time process, human approval required before installation.
- `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (Tier 3): source of gap `S0.3-G006`.
- `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md` (Tier 3): describes the existing
  lexical/structural read-only SQL regression tests, used here to distinguish existing coverage
  from dedicated SAST.
- `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`,
  `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` / `..._ADMISSION.md`,
  `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md` / `..._ADMISSION.md` (Tier 3 — accepted
  Capability Review 1–3 evidence, used here only as a structural/process template; **not
  modified**).
- `docs/status/CURRENT_PROJECT_STATUS.md`, `KST-v2-Master-Project-Checklist.md` (Tier 2).

## 3. Starting Repository State

- **Branch:** `agents/pasted-text-processing-5ee4af24`. `HEAD` (`fb5b6c98f26dd7f5d5c13527695cb317a9e87843`)
  is byte-identical to `origin/main` at the start of this session (`git rev-parse HEAD` ==
  `git rev-parse origin/main`).
- **Working tree:** clean at the start of this pass (`git status --short` empty).
- **Accepted state confirmed:** S0.1–S0.5 COMPLETE/ACCEPTED; S0.6 IN PROGRESS; Capability Review 1
  (cargo-audit 0.22.2) COMPLETE/ACCEPTED — `S0.3-G001` Covered/Resolved; Capability Review 2
  (Gitleaks v8.30.0) COMPLETE/ACCEPTED — `S0.3-G007` Covered/Resolved; Capability Review 3 (Anchore
  Syft v1.51.1) COMPLETE/ACCEPTED — `S0.3-G008` Covered/Resolved; remaining Capability Review 4
  (`S0.3-G006`, dedicated SAST) **NOT STARTED**; S0.7 and S0.8 **PLANNED / NOT STARTED**; Stage 9
  **BLOCKED PENDING S0 CLOSEOUT**. No material discrepancy with the expected canonical position was
  found, so no STOP condition applies.
- No `S0_6_SAST_ADMISSION_RESEARCH.md` (or any other SAST-related document) existed anywhere in
  `docs/security/` before this pass.

## 4. `S0.3-G006` Gap

From accepted S0.3 evidence (`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md`), KST has no
dedicated static-analysis capability whose purpose is to identify security-relevant source-code
defects (as distinct from general lint/compiler diagnostics, dependency-vulnerability scanning,
secret scanning, or SBOM generation, all of which are separately covered by other accepted
capabilities). This is a **capability gap**: no SAST tool is currently authorized, installed, or
present anywhere in the KST development environment.

**Research question (per task authorization):** What credible, maintainable static-analysis
capability can inspect KST's actual source architecture for security-relevant defects without
introducing unnecessary source disclosure, execution, network, or supply-chain risk? This document
does **not** answer "which tool should KST admit" — that is reserved for independent review and the
project owner.

## 5. KST Architecture (derived from repository inspection)

Repository inspection (not assumed from the task prompt) confirms the following major code
surfaces exist under [src/](/C:/Dev/kst_v2.worktrees/pasted-text-processing-5ee4af24/src):

| Surface | Location | Language / Framework |
|---|---|---|
| Domain | `src/backend/Kst.Domain` | C# / .NET 10 |
| Application | `src/backend/Kst.Application` | C# / .NET 10 |
| Infrastructure | `src/backend/Kst.Infrastructure` | C# / .NET 10 |
| QAD integration (SQL query-building) | `src/backend/Kst.Integrations.Qad` (13 `Build*` SQL query builders across `ApprovedVendors`, `Bom`, `ComponentDetail`, `Inventory`, `Mps`, `PartDetail`, `WorkOrders`; plus `QadConnectionFactory`, `QadConnectionStringFactory`) | C# / .NET 10, SQL Server (read-only) |
| Shortages integration | `src/backend/Kst.Integrations.Shortages` | C# / .NET 10 |
| Exports | `src/backend/Kst.Exports` | C# / .NET 10 |
| API host | `src/backend/Kst.Api` | C# / ASP.NET Core (net10.0) |
| Backend tests | `src/backend/tests/*` | C# / xUnit |
| React/TypeScript frontend | `src/frontend/src` (`api`, `components`, `fiscal`, `generated`, `hooks`, `mps`) | TypeScript / React / Vite |
| OpenAPI-generated TS contracts | `src/frontend/src/generated` (ESLint-excluded) | Generated TypeScript |
| Tauri 2 / Rust desktop host | `src/tauri/src` (`main.rs`, `lib.rs`), `src/tauri/capabilities`, `src/tauri/gen` | Rust 2021 edition, `rust-version = "1.77.2"` |
| Build/config files with security relevance | `src/backend/Directory.Build.props` (`.NET analyzers`), `src/frontend/eslint.config.js`, `src/tauri/Cargo.toml`, `src/tauri/capabilities/*` (Tauri capability/permission config) | Various |

This confirms the architecture described in the task prompt is accurate for the current repository
state: a genuinely multi-ecosystem application (C#/.NET, TypeScript/React, Rust/Tauri) with a
read-only QAD SQL boundary and a Rust host that performs process-lifecycle management.

## 6. Existing KST Analysis Capability (repository inspection)

| Existing tool | Scope | What it detects | Security-specific? |
|---|---|---|---|
| ESLint (`js.configs.recommended` + `typescript-eslint` recommended, `react-hooks`, `react-refresh`) | `src/frontend/src/**/*.{ts,tsx}` (generated code excluded) | Correctness/style lint, unused vars, hook rules | **No** — no `eslint-plugin-security`, `eslint-plugin-no-unsanitized`, or similar security plugin configured |
| TypeScript compiler (`tsc`) | Frontend | Type errors | No — type safety only |
| Rust compiler + (presumed) `cargo clippy` | `src/tauri` | Compile errors, idiomatic-code lints; clippy has some lints that overlap with defensive coding (e.g., `unwrap_used` if enabled) but no dedicated security ruleset was found configured (no `clippy.toml` present in the repository) | No — general lint/compiler diagnostics, not a security ruleset |
| .NET analyzers (`EnableNETAnalyzers=true`, `AnalysisLevel=latest` in `src/backend/Directory.Build.props`) | All C# projects | Roslyn/`Microsoft.CodeAnalysis.NetAnalyzers` general-purpose + some security-category (`CAxxxx`) diagnostics ship by default at `AnalysisLevel=latest`, but this is the general-purpose analyzer set, not a security-focused product, and no additional security-analyzer package (e.g., a dedicated Roslyn security-analyzer NuGet package) was found referenced in the backend project files | Partial — some built-in `CA` rules are security-relevant, but this is not an admitted, dedicated SAST capability |
| Security regression / architecture tests (S0.5, accepted) | `src/backend/tests/Kst.Api.IntegrationTests/LoopbackBindingTests.cs`, `CorsPolicyTests.cs`; `src/backend/tests/Kst.Integrations.Qad.Tests/ReadOnly/QadReadOnlySqlTests.cs` | Loopback binding, CSP, CORS origin allow-list, and a **lexical/structural** (not SAST-engine) check that no QAD SQL literal or query-builder output contains a mutating verb | Yes, but narrowly scoped to specific accepted S0.3 gaps, hand-written per gap, not general-purpose |
| cargo-audit (Capability Review 1, accepted) | Rust dependency graph | Known-vulnerability advisories (RustSec) | Dependency vulnerability scanning, **not SAST** |
| Gitleaks (Capability Review 2, accepted) | Git history / working tree | Secret/credential pattern matching | Secret scanning, **not SAST** |
| Anchore Syft (Capability Review 3, accepted) | Build/repository/packaged artifacts | Component/package inventory | SBOM generation, **not SAST** |

**Conclusion:** KST has general-purpose lint/compiler diagnostics and narrowly-scoped, hand-written
security regression tests, but **no dedicated, general-purpose static-analysis engine with a
maintained security-rule catalog, data-flow/taint capability, and stable finding model**. This
confirms `S0.3-G006` remains an open gap distinct from existing capability, consistent with
`AGENTS.md` §1's instruction to preserve "general lint/typecheck/compiler analysis != dedicated
accepted SAST capability" unless policy says otherwise (it does not).

## 7. Derived SAST Requirement Categories (from the gap, not vendor marketing)

A dedicated SAST capability for KST should be assessed, at minimum, on ability to identify (or
explicitly not claim to identify) each of: injection risks, unsafe SQL construction, path
traversal, command/process execution risks, unsafe deserialization, credential/secret handling
misuse, weak cryptography usage, network/TLS misuse, authorization/authentication mistakes where
statically inferable, CORS/security-header issues where statically inferable, unsafe file
handling, dangerous subprocess APIs, frontend DOM/XSS sinks, Rust unsafe-code/security patterns,
dependency misuse distinct from dependency vulnerabilities, source-to-sink data-flow defects, and
configuration security issues. Findings that duplicate `S0.3-G001`/`G007`/`G008` (already covered)
are credited here only if the candidate also provides genuine SAST-specific value (e.g., detecting
*misuse* of a dependency's API, not merely its presence or known CVEs).

## 8. Candidate Set

A small, credible set was researched — not an exhaustive catalog:

1. **Semgrep Community Edition (CE) / Semgrep CLI** — open-source, pattern + intraprocedural
   taint-analysis engine, multi-language.
2. **CodeQL CLI / CodeQL database analysis** — GitHub's semantic code-analysis engine with
   compiled query packs, multi-language including Rust and C#.
3. **Microsoft DevSkim (CLI)** — Microsoft-maintained, pattern/rule-based lightweight scanner,
   materially different operating model (no data-flow, no build/compile requirement, extremely low
   execution-trust surface, actively maintained, Windows-native, MIT-licensed).

DevSkim was selected as the third candidate because it broadens the comparison along a genuinely
different axis than Semgrep/CodeQL: it is explicitly *not* a data-flow engine (pure lexical/regex
pattern matching across many text-based languages, including SQL-adjacent and config files), has
no build/compile/database-generation step at all, and is a currently-active Microsoft OSS project
(latest release 2026-07-17) rather than a stale one. A prior candidate — **Security Code Scan**, a
Roslyn (C#-only) analyzer that would have been a strong fit for the C#/QAD-SQL surface — was
considered but is **not** included as the third candidate because its most recent release
(`5.6.7`) was published **2022-09-05** (see §9), which does not meet the "credible, maintainable"
bar established by the research question; it is recorded here for transparency but not
carried forward into the comparison table.

## 9. Candidate 1 Research — Semgrep CE / Semgrep CLI

| Attribute | Value | Source |
|---|---|---|
| Current stable CLI version | `v1.175.0` | `github.com/semgrep/semgrep` releases |
| Release date | 2026-08-26 | GitHub Releases API (`published_at`) |
| License (engine) | LGPL 2.1 | `github.com/semgrep/semgrep/blob/develop/LICENSE`; confirmed on `docs.semgrep.dev/licensing` |
| License (Registry rules) | "Semgrep Rules License v.1.0" — internal business use permitted; vendors may not use Semgrep-maintained rules in competing products/SaaS; third-party-authored registry rules (e.g., Trail of Bits rules) inherit their own source license (example given: AGPL-3.0) | `docs.semgrep.dev/licensing` |
| Maintainer | Semgrep, Inc. (commercial company; CE engine remains open source) | Same |
| Windows support | **Beta** (official quickstart documents a distinct "Windows (beta)" install path requiring Python 3.10+ and a UTF-8 console-encoding workaround) | `docs.semgrep.dev/getting-started/quickstart` |
| Maintenance/activity | Very active — weekly-or-faster tagged releases (e.g., `v1.173.0`, `v1.174.0`, `v1.175.0` within the same month) | GitHub Releases |
| Installation | `pipx install semgrep` / `uv tool install semgrep` (preferred); Homebrew "best-effort" (macOS); requires Python 3.10+ runtime | Quickstart docs |
| Login/account requirement (core scanning) | **Not required** for `semgrep scan` with local/registry configs; `semgrep ci` and Pro-engine features (`install-semgrep-pro`, cross-file analysis) require `semgrep login` | `docs.semgrep.dev/cli-reference` |
| Network/telemetry | `semgrep scan --config <local-file>` sends **no** metrics; pulling a ruleset from the Semgrep Registry (`--config p/...` or `auto`) enables pseudonymous metrics by default (`--metrics auto`); can be forced off (`--metrics off` / `SEMGREP_SEND_METRICS=off`); logging in enables metrics unconditionally | `docs.semgrep.dev/metrics` |
| Rule/query provenance | Default `auto`/registry rules are fetched over the network from the Semgrep Registry at scan time unless a local rule file is pinned instead; rules are YAML pattern/taint specs (declarative), not executable code | `docs.semgrep.dev/cli-reference`, `docs.semgrep.dev/licensing` |
| Data-flow / taint model | **CE**: intraprocedural (single-function) taint analysis only, by design, for speed/openness. **Cross-function (interprocedural)** and **cross-file (interfile)** analysis are Pro-engine features requiring `semgrep login` and (for interfile) downloading a proprietary binary | `docs.semgrep.dev/semgrep-code/semgrep-pro-engine-intro` |
| Language coverage (per official "Supported languages" matrix) | C#: GA, "cross-file dataflow analysis" (Pro-tier feature name as listed), 170+ Pro rules. TypeScript/JavaScript: GA, cross-file dataflow analysis, framework-specific control flow, 230–250+ Pro rules. Rust: GA, **cross-function** (not cross-file) dataflow analysis, 40+ Pro rules | `docs.semgrep.dev/supported-languages` |
| SQL-specific analysis | No dedicated "SQL" language target; SQL-injection-style findings are expressed as taint rules within the *host* language (e.g., C# rules whose sink is a SQL-execution API) | Inferred from rule model; Unable to Verify exact KST-applicable rule availability without executing the tool (not done) |
| Execution/build model | Source-parse based; does **not** require a successful build, package restore, or compiled assemblies for CE intraprocedural analysis | `docs.semgrep.dev/cli-reference` ("searches TARGET paths") |
| Output formats | Human-readable (default), JSON, SARIF (documented `--sarif` output), JUnit-XML (documented CI option) | CLI reference / general Semgrep docs |
| Suppression model | Inline `# nosemgrep` comments and `.semgrepignore` path-exclusion file (documented mechanism; not created during this research) | General Semgrep documentation (not independently re-verified line-by-line in this pass; **Unable to Verify exact current syntax without consulting the dedicated suppression doc page**, which returned a fetch error during this research pass) |

## 10. Candidate 2 Research — CodeQL CLI

| Attribute | Value | Source |
|---|---|---|
| Current stable CLI version | `v2.26.4` | GitHub Releases API, `github/codeql-cli-binaries` |
| Release date | 2026-08-26 | Releases API `published_at` |
| Compatible query-pack tag | `codeql-cli/v2.26.3` (language packs versioned separately from the CLI) | Release notes body |
| License / use restrictions | Proprietary **"GitHub CodeQL Terms and Conditions"** (not OSI-approved for the CLI itself, though it embeds some OSI-licensed components). Ordinary use is restricted to: academic research; demonstrating the software; testing OSI-licensed CodeQL queries; and — **only for an "Open Source Codebase"** (a codebase released under an OSI-approved license) — performing analysis, or (if hosted/maintained on GitHub.com) generating databases during automated CI/CD. **KST v2 is a private, non-open-source codebase**, so ordinary standalone CLI use for this repository is **not** authorized under these Terms unless GitHub Advanced Security (a paid customer license) applies, which removes the automated-analysis/non-open-source restriction | `github.com/github/codeql-cli-binaries/blob/main/LICENSE.md` (fetched directly, quoted verbatim above) |
| Maintainer | GitHub, Inc. (Microsoft subsidiary) | Same |
| Windows support | Yes — the CLI release explicitly ships per-platform zips including `codeql-win64.zip`, and the fetched changelog for the current release specifically mentions a Windows `subst`-drive path-canonicalization fix, confirming active Windows maintenance | Releases page / changelog |
| Maintenance/activity | Very active — CLI and query packs both tagged frequently (current release `v2.26.4`, 2026-08-26) | GitHub Releases |
| Language support relevant to KST | C#: built-in query/library packs (`codeql/csharp-queries`, `codeql/csharp-all`) with explicit ASP.NET/ASP.NET Core/EF/EF Core/Dapper framework modeling. JavaScript/TypeScript: built-in query/library packs with broad framework modeling. **Rust**: listed as a supported CodeQL language on the current official GitHub docs page (`about-code-scanning-with-codeql`), alongside C/C++, Go, Java/Kotlin, Python, Ruby, Swift; this document's research pass could not authoritatively confirm from official sources whether Rust support currently carries a "public preview"/beta qualifier or is fully GA — **Unable to Verify** the exact maturity label as of the research date (several attempted GitHub-docs and changelog fetches for this specific detail returned 404/empty content during this research pass) | `docs.github.com` "Code scanning with CodeQL" page (language list fetched and quoted); `codeql.github.com/docs/codeql-overview/supported-languages-and-frameworks/` (frameworks detail); `codeql.github.com/docs/codeql-language-guides/codeql-for-rust/` (page exists, confirming a distinct Rust query-writing guide exists) |
| Database creation requirement | **Yes** — CodeQL requires creating a CodeQL database per language before querying. For compiled/build-integrated languages this can involve invoking the build system (build-mode `autobuild`/`manual`/`none` depending on language and CodeQL version); for some languages a build-free extraction mode exists. This document did not create a database and did not determine KST-specific build-mode requirements for C#/Rust/TypeScript by execution — **Unable to Verify** exact KST-specific build-mode behavior without running the tool (not done, per hard prohibition) | General CodeQL architecture (database-then-query model), confirmed conceptually via `docs.github.com` overview text fetched above; exact current build-mode flag behavior per language not independently re-verified against the current CLI manual in this pass |
| Query-pack acquisition | Query packs are versioned/distributed separately from the CLI (as shown by the CLI release note pinning a compatible `codeql-cli/v2.26.3` tag) and are typically fetched via `codeql pack download` from the GitHub Container Registry or bundled in the "CodeQL bundle" download, which pairs a CLI version with matching query packs for offline use | Release-notes cross-reference; general CodeQL packaging knowledge — **Unable to Verify** the precise current bundle-download URL/behavior without navigating further GitHub docs pages that returned errors in this pass |
| SARIF output | Yes — CodeQL's standard output format for `codeql database analyze` is SARIF (well-established, used directly by GitHub code scanning) | Well-documented CodeQL architecture; consistent with GitHub's code-scanning SARIF-upload workflow described in the fetched "Code scanning with CodeQL" page |
| Source upload / network behavior | Standalone CLI analysis is local; no source upload is required to run `codeql database create` / `codeql database analyze` locally. Auto-update behavior: the CodeQL Terms document explicitly discusses an "Auto-Updates" clause (an update-check/auto-update service), which is a network-touching behavior distinct from source upload — **Unable to Verify** the exact default on/off state of this auto-update behavior for the standalone CLI package (the Terms text describing it was truncated in the fetched page before the exact default could be confirmed) | `LICENSE.md` (Terms) fetched above (heading "Auto-Updates" present, full clause text not fully retrieved in this pass) |
| Login/cloud requirement | Not required for local `codeql` CLI database creation/analysis (distinct from GitHub-hosted code scanning, which is tied to a GitHub repository/Actions and does require a GitHub account/repository) | Same license/Terms fetch; general architecture |

## 11. Candidate 3 Research — Microsoft DevSkim (CLI)

| Attribute | Value | Source |
|---|---|---|
| Current stable version | DevSkim CLI `v1.0.90` | GitHub Releases API, `microsoft/DevSkim` |
| Release date | 2026-07-17 | Releases API `published_at` |
| License | MIT | `raw.githubusercontent.com/microsoft/DevSkim/main/LICENSE.txt` (fetched and quoted verbatim: Microsoft Corporation, MIT License) |
| Maintainer | Microsoft (Microsoft Customer Security and Trust / `microsoft/DevSkim` org repository) | GitHub repository metadata |
| Windows support | Yes — cross-platform CLI built on .NET; ships a Visual Studio extension in addition to the CLI and VS Code plugin | `github.com/microsoft/DevSkim` README (fetched) |
| Maintenance/activity | Actively maintained — latest tagged release 2026-07-17, distributed via NuGet (`Microsoft.CST.DevSkim.CLI`) and Visual Studio/VS Code marketplaces | README badges/links (fetched) |
| Installation | NuGet package (`Microsoft.CST.DevSkim.CLI`, a .NET global tool), or VS/VS Code extension install | README (fetched) |
| Operating model | "Framework of IDE extensions and language analyzers that provide inline security analysis... flexible rule model." Explicitly a **pattern-matching** rule engine (regex/JSONPath/XPath/YmlPath-based rules per the README), **not** a data-flow/taint engine | README (fetched, quoted) |
| Language coverage (README-stated) | Explicitly lists: C, C++, C#, Cobol, Go, Java, JavaScript/TypeScript, Python, "and more" (a supported-languages reference page is linked but was not reachable in this research pass — GitHub wiki page returned a redirect/blocked-action response) | README (fetched); wiki page fetch failed (`github.com/microsoft/DevSkim/wiki/Supported-Languages`) — **Unable to Verify** the complete/current language list, in particular whether Rust and/or SQL are explicitly enumerated as supported languages in the current default ruleset |
| Data-flow / taint capability | **None claimed.** DevSkim's own description frames it as inline, pattern-based, developer-facing "as you type" analysis — not a source-to-sink taint engine | README |
| Execution/build model | Pure source/text scan; no build, restore, or compilation is required | README / architecture (CLI operates on file text) |
| Network/telemetry | Rules ship with the tool (bundled default ruleset); no login/account requirement is documented for CLI/IDE use in the README | README; **Unable to Verify** exact telemetry/update-check behavior of the CLI binary itself without consulting additional docs not reached in this pass |
| Output formats | JSON and other machine-readable formats are supported by the CLI (general DevSkim CLI capability); SARIF support specifically was **not independently confirmed** in this pass — **Unable to Verify** | README high-level description only; exact CLI `--help` output was not captured (would require execution, which is prohibited for this research pass) |
| Suppression model | README states "Optional suppression of unwanted findings" as a built-in feature (inline suppression comments, per general DevSkim design) | README |

## 12. Third-Candidate Justification

DevSkim materially broadens the comparison versus Semgrep/CodeQL because it represents a
genuinely different model along several axes simultaneously: (a) no data-flow/taint claims at all
(pure lexical pattern matching, so its false-positive/false-negative profile and trust surface are
qualitatively different), (b) no build/compile/database-generation step under any circumstance,
(c) MIT-licensed and Microsoft-maintained with no commercial/Pro tier or login-gated features, and
(d) a currently-active release cadence (2026-07-17) that is far more recent than the previously
considered Security Code Scan candidate (2022-09-05, see §8), which was set aside specifically for
staleness rather than technical unsuitability.

## 13. Language-Coverage Matrix

| Language | Semgrep CE (local, no login) | CodeQL CLI | DevSkim CLI |
|---|---|---|---|
| TypeScript/JavaScript | Parsed; intraprocedural pattern + taint (CE); interprocedural/interfile is Pro-gated | Built-in query/library pack, framework modeling (React not independently confirmed as an explicit built-in framework in the fetched excerpt) | Pattern/regex rules (README-confirmed) |
| C# | Parsed; intraprocedural pattern + taint (CE); interfile is Pro-gated | Built-in query/library pack; ASP.NET/ASP.NET Core/EF/EF Core/Dapper explicitly modeled | Pattern/regex rules (README-confirmed) |
| Rust | Parsed; GA per Semgrep's own language table, but limited to **cross-function** (not cross-file) dataflow even in Pro tier | Listed as a supported CodeQL language on current official docs; **Unable to Verify** exact maturity/beta qualifier | Not explicitly confirmed in the reachable README excerpt — **Unable to Verify** |
| SQL (as its own analysis target, distinct from SQL-sink modeling inside C#) | No distinct "SQL" language target found; SQL-injection findings are host-language taint rules with a SQL-execution sink | No distinct "SQL" CodeQL language; same host-language-taint-to-SQL-sink model | Pattern rules could in principle match raw `.sql` text or embedded SQL string literals, but this is lexical, not semantic — **Unable to Verify** whether default rules cover this for KST's C# QAD readers specifically |
| Tauri configuration / capability JSON | Not a distinct "language"; generic JSON/config pattern rules could apply if written, none confirmed as default | Not a distinct "language"; no CodeQL Tauri modeling found | Rule model explicitly supports JSONPath-based rules per README, so config-file pattern rules are architecturally plausible; no default Tauri-specific ruleset confirmed |

Distinguishing "language parsed" from "semantic analysis" from "data-flow/taint": for all three
candidates, KST's Rust surface receives, at most, intraprocedural/cross-function analysis (no
tool confirmed to offer cross-file Rust taint without a paid/Pro tier), while C#/TypeScript can
reach deeper (cross-file) analysis only through Semgrep's Pro engine (login-gated) or CodeQL's
full-database semantic model (license-gated for this private repository, per §10).

## 14. Data-Flow / Taint Capability Comparison

| Capability | Semgrep CE | CodeQL | DevSkim |
|---|---|---|---|
| Intra-file data flow | Yes (CE) | Yes | No (pattern-based, not data-flow) |
| Interprocedural (cross-function) | Pro-gated for most languages except Rust, where it is explicitly the GA ceiling | Yes (semantic database model reasons across procedures) | No |
| Cross-file (interfile) | Pro-gated, requires login + proprietary binary | Yes (whole-database analysis is inherently cross-file) | No |
| Source-to-sink taint analysis | Yes, expressed as declarative taint rules (mode depends on CE vs Pro scope) | Yes, CodeQL's core strength (dataflow/taint-tracking libraries) | No |
| Framework-specific modeling | Yes for JS/TS/Python/Java per official table (React/Django/Spring-style awareness); not confirmed for KST's exact ASP.NET Minimal API style | Yes, explicit ASP.NET/ASP.NET Core/EF Core packs listed | No |
| Custom taint rules | Yes (YAML rule authoring) | Yes (custom QL queries) | No (pattern rules only, not taint rules) |

**KST-relevant limitation:** none of the three candidates was confirmed, from official sources
alone, to provide free/local cross-file taint tracking for all three of KST's languages
simultaneously. Semgrep CE and DevSkim are both bounded below CodeQL's cross-file semantic model
for C#/TypeScript, but CodeQL's standalone-CLI licensing question (§10) is a first-class caveat
for a private codebase like KST.

## 15. SQL / Database Analysis Comparison

None of the three candidates was confirmed to have a distinct "SQL" analysis target; all express
SQL-injection-class findings as taint rules in the *host* language (C#) whose sink is a
SQL-execution API (e.g., `SqlCommand`, `IDbConnection.Query`). This means detection quality for
KST's actual QAD SQL surface (13 pure, static `Build*` query builders returning
`(string Sql, DynamicParameters Parameters)`, per `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md`
§8) depends on each candidate's C#/Dapper-aware rule coverage, not on any SQL-specific engine.
Existing KST regression tests (§6 above) are a **lexical/structural** check (string-literal and
reflection-based verb scan), explicitly **not** a SAST engine and not a substitute for one — this
distinction is preserved rather than blurred in this research.

Whether any candidate's *default* free ruleset actually contains a rule that fires on KST's
specific `Build*` builder pattern (parameterized `DynamicParameters`, not raw string
concatenation) was **not tested** (execution is prohibited for this research pass) and is recorded
as a future verification item (§32).

## 16. Process / Shell-Execution Coverage

- **Semgrep:** Registry includes generic rules for dangerous subprocess APIs across several
  languages (e.g., shell-injection-style rules for JS/Python are well known in the public
  registry); Rust-specific process/command-construction rule depth was **not verified** for the
  free/CE tier during this research pass.
- **CodeQL:** Has general "command injection"/"uncontrolled command line" query categories in its
  standard security query suites for supported languages; whether an equivalent query exists and
  is mature for **Rust** specifically was **not verified** (Rust support is newer than C#/JS/Python
  in CodeQL's history) — **Unable to Verify**.
- **DevSkim:** Pattern rules can match known-dangerous API names (e.g., `Process.Start`,
  `Command::new` style call sites) lexically, but this is a direct API-name pattern match, not
  data-flow analysis of whether an argument is attacker-influenced — the distinction the task asks
  to preserve.

All three would need independent, future confirmation (not performed here) of exactly what they
flag for KST's actual Rust process-lifecycle code before any claim of "covers `S0.3-G006`" could
be made for that surface.

## 17. Frontend / TypeScript Security Coverage

- **Semgrep:** Official docs list JS/TS as GA with framework-specific control-flow analysis and a
  large Pro rule count (230+); free/CE rules for common DOM-XSS patterns
  (`dangerouslySetInnerHTML`, `eval`, unsafe URL handling) are commonly present in the public
  registry, but exact current rule IDs applicable to KST's specific React version were not
  enumerated in this pass.
- **CodeQL:** Has a long-standing, mature JavaScript/TypeScript security query suite (DOM-XSS,
  prototype pollution, open redirect, and similar categories are part of CodeQL's original/founding
  language support), though — per §10 — standalone non-GitHub-Actions use against this private
  repository's TypeScript would fall under the same licensing restriction as the C# surface.
- **DevSkim:** JS/TS explicitly listed as a supported language in the README; coverage is
  pattern-based (e.g., matching `dangerouslySetInnerHTML`, `eval(` literally) rather than
  data-flow, so it would not distinguish a safely-sanitized use from an unsafe one.

Tauri-specific note: none of the three tools was confirmed to have first-class awareness that
KST's frontend runs inside a Tauri webview rather than a general browser; ordinary browser-DOM
threat-model rules (e.g., generic `postMessage` origin-check rules) may not map cleanly onto
Tauri's IPC model, and this limitation applies to all three candidates equally as far as could be
determined from public documentation in this pass.

## 18. Rust Security Coverage

| Consideration | Semgrep CE | CodeQL | DevSkim |
|---|---|---|---|
| `unsafe` block/API detection | Rust rules exist in the public registry conceptually; CE scope limited to cross-function, not cross-file | Rust listed as supported language (maturity qualifier Unable to Verify); CodeQL's core strength is data-flow, which would need Rust-specific query maturity not independently confirmed here | Pattern-based; presence of Rust in the default ruleset not independently confirmed (§11) |
| Command execution patterns | Same limitation as §16 | Same as §16 | Same as §16, if Rust is covered at all |
| Filesystem/path misuse | Not independently confirmed for KST's Rust surface | Not independently confirmed | Not independently confirmed |
| FFI/native-boundary concerns | Not confirmed | CodeQL has historically strong C/C++ FFI-adjacent modeling; Rust-side FFI query maturity not confirmed | Not applicable (pattern engine) |
| Distinct from `cargo clippy`/compiler | Yes — none of the three tools is the Rust compiler or clippy; this research explicitly does **not** count clippy/compiler findings as SAST coverage, per task instruction | Same | Same |
| Distinct from `cargo-audit` (RustSec) | Yes — dependency-advisory matching (already admitted, Capability Review 1) is not counted as SAST coverage here | Same | Same |

**Rust remains the least-mature surface for all three candidates** based on what could be
confirmed from official sources in this research pass; this is recorded as a genuine open
question for independent review (§34), not resolved here.

## 19. C# / ASP.NET Core Security Coverage

- **Semgrep:** C# listed GA with cross-file dataflow (Pro-gated) and 170+ Pro rules; CE-tier free
  rule depth specifically for ASP.NET Core / Dapper / EF Core patterns was not enumerated
  rule-by-rule in this pass.
- **CodeQL:** Explicit built-in framework packs for ASP.NET, ASP.NET Core, ASP.NET Razor templates,
  Dapper, EntityFramework, EntityFramework Core, Json.NET, NHibernate, WinForms (§10, fetched
  verbatim from `codeql.github.com`). This is the most explicitly documented C#/ASP.NET framework
  coverage of the three candidates.
- **DevSkim:** C# is an explicitly listed supported language; coverage is pattern-based, not
  framework-semantic-model-based.

Whether analysis requires a successful build: Semgrep and DevSkim are source-parse tools (no
build/restore required); CodeQL requires database creation, which for compiled languages including
C# typically requires either a successful build pass (`autobuild`/manual build-mode) or, depending
on CodeQL CLI capabilities and language, a build-free extraction path — the current exact
requirement for a .NET 10 multi-project solution like KST's was **not independently verified by
execution** in this pass (execution prohibited) and is recorded as a future verification item.

## 20. Tauri-Specific Analysis

No candidate was found, from official public documentation reachable in this research pass, to
have explicit, named rule support for: Tauri commands, Tauri capabilities/permissions files, IPC
exposure, frontend-to-Rust invocation boundaries, Tauri CSP/configuration, or sidecar usage.
**No dedicated Tauri rule support confirmed** for any of the three candidates. Generic
Rust/TypeScript/JSON-config rules from each engine could, in principle, still provide partial,
incidental coverage (e.g., a generic "unsafe API use" Rust rule firing inside a Tauri `#[command]`
function, or a generic JSON schema/pattern rule matching `src/tauri/capabilities/*.json`), but this
would be incidental rather than purpose-built, for all three candidates equally.

## 21. Generated-Code Handling

- KST's generated OpenAPI TypeScript artifacts live at `src/frontend/src/generated` and are already
  excluded from ESLint (`ignores: ['dist', 'build', 'coverage', 'node_modules', 'src/generated']`
  in `src/frontend/eslint.config.js`, confirmed by direct inspection).
- **Semgrep:** supports path-exclusion via `.semgrepignore` and `--exclude` CLI flags (general,
  documented Semgrep capability); this is **configurable**, not automatic.
- **CodeQL:** database creation can be scoped to specific source paths/projects at database-create
  time, and/or generated files can be excluded via `.codeqlignore`-style exclusion where supported;
  exact current mechanism for a mixed C#/TS solution was **not independently re-verified by
  execution** in this pass.
- **DevSkim:** supports path/file exclusion via CLI options (general DevSkim capability); exact
  current flag syntax was **not independently confirmed** in this pass.

No exclusion policy, ignore file, or suppression was created during this research pass, per the
hard prohibition in the source task.

## 22. Rule / Query Provenance

| Consideration | Semgrep | CodeQL | DevSkim |
|---|---|---|---|
| Default rule source | Registry rules download from `semgrep.dev` at scan time unless a local rule file is used instead | Query packs are separately versioned from the CLI (this release pins compatible tag `codeql-cli/v2.26.3`) and are typically obtained via `codeql pack download` or a bundled "CodeQL bundle" | Rules ship bundled with the DevSkim package/release itself (no separate registry step confirmed) |
| Maintainer of default rules | Semgrep, Inc. (Community rules) plus third-party contributors (e.g., Trail of Bits) whose rules retain their own license | GitHub / CodeQL team | Microsoft DevSkim team |
| Executable code in rules? | No — Semgrep rules are declarative YAML patterns, not executable code | No — CodeQL queries are declarative QL, compiled and run inside CodeQL's own sandboxed evaluator, not arbitrary host-executed code | No — DevSkim rules are declarative pattern/regex/JSONPath/XPath/YmlPath specs |
| Signed/checksummed rule packages | **Unable to Verify** — not confirmed from the docs pages reachable in this pass for any of the three candidates | Same — **Unable to Verify** | Same — **Unable to Verify** |
| Can rules update independently of the CLI/executable? | **Yes** for registry-sourced configs (`--config p/...` or `auto`) — a pinned CLI version can still receive different rule content on a later scan unless a local, pinned rule file is used instead | **Yes** — query-pack versions are explicitly decoupled from the CLI version per the release notes (a CLI update need not accompany a query-pack update, and vice versa) | **No separate rule-update channel was confirmed** — rules appear to ship as part of the same release artifact, which would make DevSkim's rule pinning coincide with its executable pinning, but this was **not independently confirmed** by inspecting a release asset list in this pass |

This confirms the task's framing directly: "a security scanner whose executable is pinned but
whose rules silently update is not truly version-pinned" applies most clearly to Semgrep's
`--config auto`/registry mode and to CodeQL's separately-versioned query packs, and applies least
(as far as could be determined) to DevSkim, whose rules appear bundled with the release — though
this last point carries an explicit Unable-to-Verify qualifier above.

## 23. Network / Data-Handling Analysis

| Question | Semgrep | CodeQL | DevSkim |
|---|---|---|---|
| Can core analysis run fully locally? | Yes, with local rule files (`--config <local-file>`) | Yes, for local database creation/analysis with a pre-downloaded bundle | Yes (no login mechanism documented at all) |
| Requires login/account for ordinary scanning? | No for `semgrep scan` with local/registry configs; yes for `semgrep ci`, Pro engine, cross-file analysis | No for standalone CLI use (distinct from GitHub-hosted code scanning) | No (not documented) |
| Uploads source? | No — CE/local scanning does not upload source | No — standalone CLI does not upload source | No |
| Uploads snippets/findings? | Not for local scans; the Semgrep AppSec Platform (separate product) does aggregate findings if used, which is out of scope here | Not for standalone local use | Not documented |
| Contacts a rules registry? | Yes, if `--config` references the Semgrep Registry (`p/...`, `auto`) | Only if query packs are fetched via `codeql pack download` from a registry; not if using a pre-downloaded bundle | Not documented as contacting an external registry (bundled rules) |
| Telemetry / metrics | `--metrics auto` (default) sends metrics only when registry rules are used or the user is logged in; can be set to `off` | **Unable to Verify** the exact current default (the Terms document mentions an "Auto-Updates" clause but full text was not retrieved in this pass) | **Unable to Verify** |
| Fully offline after initial install? | Yes, if local rule files are used and `--metrics off` is set | Yes, if a full offline bundle (CLI + matching query packs) is pre-downloaded | Likely yes (bundled rules, no login), but not independently confirmed by execution |

No KST source was uploaded, and no candidate was executed, during this research pass.

## 24. Login / Cloud / Identity Requirements

- **Semgrep:** no login for local/registry `semgrep scan`; login required for `semgrep ci`,
  `install-semgrep-pro`, and cross-file (interfile) analysis.
- **CodeQL:** no login for standalone local CLI use; a GitHub account/repository is required only
  for GitHub-hosted code scanning (Actions-based), which is a distinct product surface from the
  standalone CLI and was not evaluated as part of local KST analysis in this research.
- **DevSkim:** no login/account requirement found in any reachable documentation.

No account was created and no authentication was performed during this research pass.

## 25. Installation / Supply-Chain Trust

| Consideration | Semgrep | CodeQL | DevSkim |
|---|---|---|---|
| Official distribution | PyPI (via `pipx`/`uv`), GitHub releases (source), best-effort Homebrew | GitHub Releases (`codeql-cli-binaries`), per-platform zips including `codeql-win64.zip` | NuGet (`Microsoft.CST.DevSkim.CLI`), VS/VS Code marketplace extensions, GitHub releases |
| Runtime dependency | Python 3.10+ (the CLI itself is an OCaml-core binary wrapped by a Python launcher/package) | None beyond the CLI's own bundled runtime (no external interpreter documented as required) | .NET (the CLI is a .NET global tool) |
| Prebuilt Windows binary | Yes, via the Python package's platform wheel; Windows flagged as "beta" in the official quickstart | Yes, explicit `codeql-win64.zip` | Yes, via NuGet .NET global tool mechanism |
| Checksums / release signatures / attestations | **Unable to Verify** — not independently confirmed for any of the three candidates in this pass (would require inspecting each release's asset list / SLSA-provenance metadata in more depth than this pass reached) | Same — **Unable to Verify** | Same — **Unable to Verify** |
| Admin requirement | Not documented as required for any of the three (user-scoped installs via `pipx`/NuGet/tool install are the documented paths) | Not documented as required | Not documented as required |
| Rollback/removal | Standard package-manager uninstall (`pipx uninstall`, NuGet tool uninstall) is the expected mechanism for all three; not independently exercised in this pass | Same expectation (delete/replace the extracted CLI bundle) | Same expectation (`dotnet tool uninstall`) |

## 26. Execution / Build Trust

| Candidate | Operating model |
|---|---|
| Semgrep CE | Pure source parse; does not invoke the target project's build system, package manager, or build scripts to perform its core (intraprocedural) analysis |
| CodeQL | Requires **database creation**, which for some languages/build-modes can involve invoking the project's own build system (compiler, and potentially package restore) to observe compiled semantics; the exact KST-specific build-mode requirement for the C# solution and the Rust crate was **not independently verified by execution** in this pass — recorded as a first-class open question (§34) |
| DevSkim | Pure source/text scan; no build, restore, or compilation invoked under any documented mode |

None of the three tools was executed, and no build was invoked, during this research pass.

## 27. Finding / Output Model

| Consideration | Semgrep | CodeQL | DevSkim |
|---|---|---|---|
| Stable rule ID | Yes (rule ID is part of every finding) | Yes (query ID) | Yes (rule ID, per README's rule-model description) |
| File/line | Yes | Yes | Yes |
| Data-flow trace | Yes, for taint rules (CE intraprocedural only unless Pro) | Yes (CodeQL surfaces the full data-flow path for `path-problem` queries) | No (pattern rules have no trace to show) |
| Severity/confidence | Yes (rule metadata) | Yes (query metadata, e.g., `@security-severity`) | Yes (rule metadata, per README) |
| CWE / OWASP category | Registry rules commonly carry CWE/OWASP metadata | CodeQL query metadata commonly carries CWE tags (`@tags security ...`) | **Unable to Verify** whether the default ruleset attaches CWE/OWASP metadata |
| SARIF | Yes | Yes (CodeQL's canonical output format) | **Unable to Verify** |
| Fingerprint for repeat-run correlation | Semgrep supports a documented "fingerprint" concept for tracking findings across runs (general Semgrep capability) | CodeQL/SARIF supports result "partial fingerprints" as part of the SARIF spec | **Unable to Verify** |

A vendor severity label is tool metadata only; this document does not treat any candidate's
severity scheme as an automatic KST organizational risk rating.

## 28. Suppression / Baseline Model (researched, not configured)

- **Semgrep:** inline `# nosemgrep` (or language-equivalent) comments and a `.semgrepignore`
  path-exclusion file are the documented mechanisms; both would be local, reviewable,
  version-controlled files if ever adopted.
- **CodeQL:** suppression is typically handled via SARIF-level triage (dismissing an alert in
  GitHub's code-scanning UI) when used with GitHub-hosted scanning, or via query/path-filter
  configuration for standalone use; a purely local, version-controlled suppression file model was
  **not independently confirmed** for the standalone CLI path in this pass.
- **DevSkim:** README states "optional suppression of unwanted findings" as a built-in feature
  (commonly implemented as inline suppression comments in DevSkim's design); exact current syntax
  was **not independently re-verified** in this pass.

No suppression, baseline, ignore file, or configuration was created during this research pass.

## 29. Sensitive Output

SAST reports for all three candidates can be expected to reproduce **source snippets, file paths,
and potentially variable/literal contents** at a matched location, since this is inherent to how
static-analysis findings are normally presented (all three tools' finding models include file/line
context, per §27). None of the three tools' redaction/snippet-suppression controls were
independently tested in this pass. No KST report was generated, and no output was produced, during
this research (the hard prohibition in the source task explicitly forbids running any candidate
against KST).

## 30. Operational Complexity

| Candidate | KST would require |
|---|---|
| Semgrep CE | One CLI invocation could plausibly cover TypeScript/JS, C#, and Rust in a single run if rules for each are specified, but Rust and C# are limited to intraprocedural/cross-function analysis at the CE tier; a single Python-based tool install |
| CodeQL | Separate database creation **per language** (C#, JavaScript/TypeScript, Rust) is inherent to CodeQL's architecture, i.e., multiple databases even for one invocation model; plus the licensing question in §10 that would need resolution before any standalone use against this private repository |
| DevSkim | One CLI invocation, one bundled ruleset, no per-language database or build-mode configuration — the least operationally complex of the three by design, at the cost of no data-flow depth |

## 31. CI / IDE Capabilities (researched only; none configured, installed, or enabled)

- **Semgrep:** documented VS Code extension, pre-commit hook support (`pre-commit` framework
  integration is commonly documented for Semgrep), and CI integrations (including a documented
  distinction between `semgrep scan` and `semgrep ci`).
- **CodeQL:** documented VS Code extension (`CodeQL` extension for interactive query development),
  first-class GitHub Actions integration (`github/codeql-action`), and "advanced setup"/"default
  setup" CI paths documented on `docs.github.com`.
- **DevSkim:** documented Visual Studio extension and VS Code extension (both linked directly from
  the README, with marketplace badges), positioned primarily as an in-IDE, as-you-type tool rather
  than a CI-first tool.

No extension was installed, no hook was created, and no CI workflow was created during this
research pass.

## 32. Independent Verification Possibility (future, not performed)

A future admission test could plausibly validate each candidate using small, disposable synthetic
code samples created **outside** the KST repository, tailored to the admitted candidate's claimed
support — for example: a C# SQL-injection sample (string-concatenated `SqlCommand`) to test the
C#/SQL-sink rule path; a C# process-argument-injection sample; a TypeScript DOM-XSS sample
(`dangerouslySetInnerHTML` with unsanitized input); and a Rust command-construction sample
(`Command::new` with an unsanitized argument). No such synthetic case was created during this
research pass.

## 33. KST-Specific Future Verification Targets (identified, not scanned)

Representative real code areas for **future** coverage verification (not scanned or classified as
vulnerable in this pass):

- C# QAD SQL reader/query-builder code:
  [QadWorkOrderMaterialReader.cs](/C:/Dev/kst_v2.worktrees/pasted-text-processing-5ee4af24/src/backend/Kst.Integrations.Qad/WorkOrders/QadWorkOrderMaterialReader.cs),
  [QadBomReader.cs](/C:/Dev/kst_v2.worktrees/pasted-text-processing-5ee4af24/src/backend/Kst.Integrations.Qad/Bom/QadBomReader.cs)
- C# SQL connection construction:
  [QadConnectionFactory.cs](/C:/Dev/kst_v2.worktrees/pasted-text-processing-5ee4af24/src/backend/Kst.Integrations.Qad/QadConnectionFactory.cs),
  [QadConnectionStringFactory.cs](/C:/Dev/kst_v2.worktrees/pasted-text-processing-5ee4af24/src/backend/Kst.Integrations.Qad/QadConnectionStringFactory.cs)
- Rust sidecar/process-lifecycle and Tauri host commands:
  [main.rs](/C:/Dev/kst_v2.worktrees/pasted-text-processing-5ee4af24/src/tauri/src/main.rs),
  [lib.rs](/C:/Dev/kst_v2.worktrees/pasted-text-processing-5ee4af24/src/tauri/src/lib.rs)
- TypeScript frontend data-fetch/API handling:
  [src/frontend/src/api](/C:/Dev/kst_v2.worktrees/pasted-text-processing-5ee4af24/src/frontend/src/api)
- Tauri security configuration:
  [src/tauri/capabilities](/C:/Dev/kst_v2.worktrees/pasted-text-processing-5ee4af24/src/tauri/capabilities)

These are future coverage targets only; nothing above is classified as vulnerable, and none was
scanned by any candidate in this pass.

## 34. Neutral Comparison Table

| Dimension | Semgrep CE | CodeQL CLI | DevSkim CLI |
|---|---|---|---|
| Current version | v1.175.0 (2026-08-26) | v2.26.4 (2026-08-26) | v1.0.90 (2026-07-17) |
| License / use restrictions | LGPL 2.1 (engine); Registry rules under Semgrep Rules License v.1.0 (internal-business-use permitted) | Proprietary GitHub CodeQL Terms; ordinary standalone use restricted for non-open-source/private codebases absent a GitHub Advanced Security license | MIT |
| Windows support | Beta (documented distinct Windows install path) | Yes (dedicated `codeql-win64.zip`, Windows-specific bug fixes shipped) | Yes |
| Local/offline support | Yes, with local rule files | Yes, with a pre-downloaded CLI+query-pack bundle | Yes |
| Account/login requirement | No (core `scan`); Yes (`ci`, Pro engine, cross-file) | No (standalone CLI); Yes (GitHub-hosted code scanning) | No |
| Source upload required? | No | No | No |
| Telemetry/update behavior | `--metrics auto`/`on`/`off`, documented and controllable | Auto-update clause exists in Terms; exact default Unable to Verify | Unable to Verify |
| Rule/query source | Local file (pinned) or Semgrep Registry (network, unpinned unless local) | Local bundle (pinned) or `codeql pack download` (network, versioned separately from CLI) | Bundled with release (apparent; not independently confirmed) |
| Rule/query pinning | Possible via local rule files | Possible via a pinned offline bundle | Apparent by default (Unable to Verify separate rule-update channel) |
| Rule/query provenance/integrity | Declarative YAML; no code execution; signature/checksum status Unable to Verify | Declarative QL, compiled and sandboxed; signature/checksum status Unable to Verify | Declarative pattern rules; signature/checksum status Unable to Verify |
| TypeScript support | GA, intraprocedural (CE) / interfile (Pro) | GA, built-in query pack | Listed, pattern-based |
| C# support | GA, intraprocedural (CE) / interfile (Pro), 170+ Pro rules | GA, explicit ASP.NET/EF/Dapper framework modeling | Listed, pattern-based |
| Rust support | GA, cross-function ceiling (CE and Pro alike per official table) | Listed as supported; maturity qualifier Unable to Verify | Unable to Verify (language list not reachable) |
| SQL security analysis | Via host-language taint rules only, no distinct SQL target | Via host-language taint rules only, no distinct SQL target | Via lexical pattern matching only, if rules exist for the pattern |
| Taint/data-flow support | Yes, intraprocedural (CE) | Yes, full database-model data flow | No |
| Interprocedural analysis | Rust: yes (GA ceiling); others: Pro-gated | Yes (inherent to database model) | No |
| Tauri-specific coverage | None confirmed | None confirmed | None confirmed |
| Requires build? | No | Database creation may require build/extraction depending on language; not independently verified for KST | No |
| Executes project/build code? | No | Possibly, depending on build-mode/language; not independently verified for KST | No |
| Package-manager invocation? | No (Semgrep itself is installed via a package manager, but scanning does not invoke KST's package managers) | Possibly during database creation, not independently verified | No |
| Output formats | Text, JSON, SARIF, JUnit-XML (documented) | Text, JSON, SARIF (canonical) | Text, JSON (SARIF status Unable to Verify) |
| SARIF | Yes | Yes | Unable to Verify |
| Finding fingerprints | Yes (documented concept) | Yes (SARIF partial fingerprints) | Unable to Verify |
| Source snippets in output | Expected (inherent to file/line finding model) | Expected (inherent to file/line finding model) | Expected (inherent to file/line finding model) |
| Suppression model | Inline comment + `.semgrepignore` | SARIF-level triage / path-filter (standalone-CLI local-file suppression model Unable to Verify) | Inline suppression (per README; exact syntax Unable to Verify) |
| Generated-code handling | Configurable exclusion (`.semgrepignore`, `--exclude`) | Configurable at database-create/path-filter level (exact current mechanism Unable to Verify) | Configurable exclusion (exact flag syntax Unable to Verify) |
| Installation footprint | Python 3.10+ runtime + `pipx`/`uv` package | Self-contained per-platform CLI zip | .NET global tool via NuGet, or IDE extension |
| Maintenance/activity | Very active (near-weekly releases) | Very active (frequent CLI + query-pack releases) | Active (most recent release 2026-07-17) |
| CI/IDE options | VS Code extension, pre-commit, CI (`ci` subcommand) | VS Code extension, GitHub Actions (`codeql-action`), CI advanced/default setup | Visual Studio extension, VS Code extension |
| Unable-to-Verify items | `.semgrepignore` exact current syntax; release-signature/checksum status | Rust support maturity label; auto-update default; database/build-mode requirement for KST's .NET 10 solution; release-signature/checksum status | Complete language list (Rust/SQL inclusion); SARIF support; telemetry/update behavior; release-signature/checksum status |

No score, ranking, winner, or recommendation is present in this table.

## 35. Candidate Tradeoff Summaries (no recommendation)

### Semgrep CE

- **Evidence-supported strengths:** actively maintained (near-weekly releases); no login required
  for local scanning with local rule files; documented, controllable telemetry (`--metrics off`);
  multi-language (TS/C#/Rust) with a declarative (non-executable) rule format; SARIF/JSON output.
- **Evidence-supported limitations:** Windows support is explicitly "beta" per official docs;
  deepest analysis (cross-file/interfile taint) requires the proprietary Pro engine and login;
  Registry-sourced rules are unpinned by default and licensed for internal-business-use only (not
  a fully open OSI license for the ruleset, though the engine itself is LGPL 2.1); Rust is capped
  at cross-function analysis even under Pro.
- **Language-coverage characteristics:** broad; GA for TS/C#/Rust at the CE intraprocedural tier.
- **Data-flow depth:** intraprocedural only at CE tier; interprocedural/interfile Pro-gated.
- **Rule/provenance model:** local files are fully pinned; registry configs are not pinned by
  version unless a specific commit/version is referenced.
- **Network/data-handling model:** local-only if local rule files and `--metrics off` are used;
  otherwise contacts the Semgrep Registry and sends pseudonymous metrics.
- **Installation/supply-chain characteristics:** depends on a Python 3.10+ runtime in addition to
  the tool itself; official install methods documented (`pipx`, `uv`).
- **Execution/build trust surface:** pure source parse; does not invoke KST's build system.
- **Operational complexity:** low-to-moderate; one CLI, one invocation could span all three
  languages, subject to per-language CE analysis-depth limits noted above.
- **Potential KST fit considerations:** the Windows-beta status and CE-tier interprocedural/interfile
  gating are direct considerations given KST is a Windows-hosted, multi-language application; the
  Registry rule license (internal-business-use) appears compatible with KST's internal, non-SaaS
  use, but this is a licensing question for independent/owner review, not a conclusion of this
  document.
- **Unable-to-Verify items:** exact current `.semgrepignore`/suppression syntax; release-signature
  or checksum verification process for the PyPI/GitHub-released artifacts.

### CodeQL CLI

- **Evidence-supported strengths:** deep, mature, cross-file (whole-database) data-flow/taint
  analysis; explicit built-in ASP.NET/ASP.NET Core/EF Core/Dapper C# framework modeling (the most
  detailed framework-awareness confirmed among the three candidates for KST's C#/QAD surface); no
  login required for standalone local CLI use; canonical SARIF output; very actively maintained by
  GitHub.
- **Evidence-supported limitations:** the standalone-CLI Terms explicitly restrict ordinary use
  against a **private, non-open-source codebase** like KST unless a paid GitHub Advanced Security
  license applies — this is a first-class licensing gate, not merely a technical one; requires
  per-language database creation (operationally heavier than the other two candidates); Rust
  support's exact maturity label could not be confirmed from the sources reached in this pass;
  build-mode/database-creation requirements for KST's specific .NET 10 solution were not verified
  by execution.
- **Language-coverage characteristics:** GA for C#/TS with deep framework modeling; Rust listed as
  supported with maturity Unable to Verify.
- **Data-flow depth:** the deepest of the three candidates by design (whole-database, cross-file,
  cross-function).
- **Rule/provenance model:** query packs are versioned independently of the CLI; a pinned offline
  bundle (CLI + matching query packs) is the documented path to full version pinning.
- **Network/data-handling model:** no source upload for standalone use; an "Auto-Updates" clause
  exists in the Terms whose exact default behavior was not fully retrieved in this pass.
- **Installation/supply-chain characteristics:** self-contained per-platform zip, explicit Windows
  build with a Windows-specific bug fix in the current release.
- **Execution/build trust surface:** potentially the highest of the three, since database creation
  for compiled languages can require invoking the project's own build system, depending on
  language and build-mode — not resolved for KST by execution in this pass.
- **Operational complexity:** highest of the three (per-language databases; licensing gate to
  resolve before any use against this repository).
- **Potential KST fit considerations:** the private-codebase licensing restriction is the most
  material fact this research surfaced for CodeQL specifically, and would need explicit resolution
  (e.g., confirming whether KST/Keytronic holds or would need a GitHub Advanced Security
  entitlement) before any further evaluation could proceed; this document does not resolve that
  question.
- **Unable-to-Verify items:** Rust support maturity label; exact database/build-mode requirement
  for a .NET 10 multi-project solution; auto-update default behavior; release-signature/checksum
  verification process.

### DevSkim CLI

- **Evidence-supported strengths:** MIT-licensed; no login/account mechanism at all; no
  build/compile/database-creation step under any mode; actively maintained by Microsoft (release
  2026-07-17); broad stated language list including C#, JS/TS, and several others; lowest
  execution-trust surface of the three (pure text/pattern scan); IDE-first design (VS/VS Code
  extensions) could complement, not replace, a CI-oriented tool.
- **Evidence-supported limitations:** explicitly not a data-flow/taint engine — cannot distinguish
  a sanitized use of a risky API from an unsanitized one; the complete/current supported-language
  list (in particular Rust and SQL) could not be confirmed in this pass because the GitHub wiki
  page was not reachable; SARIF output support was not independently confirmed.
- **Language-coverage characteristics:** README explicitly confirms C, C++, C#, Cobol, Go, Java,
  JS/TS, Python; Rust/SQL inclusion Unable to Verify.
- **Data-flow depth:** none (by design).
- **Rule/provenance model:** rules appear bundled with the release rather than fetched separately,
  though this was not independently confirmed against a release asset listing.
- **Network/data-handling model:** no login, no documented source upload; exact telemetry/update
  behavior Unable to Verify.
- **Installation/supply-chain characteristics:** NuGet .NET global tool or marketplace extension;
  no admin requirement documented.
- **Execution/build trust surface:** lowest of the three (pure text scan, no build/compile step
  under any mode).
- **Operational complexity:** lowest of the three (single invocation, single bundled ruleset, no
  per-language database).
- **Potential KST fit considerations:** as a Microsoft-maintained, MIT-licensed, Windows-friendly,
  no-login tool with no build requirement, DevSkim's operational-complexity and trust-surface
  profile is the simplest of the three; its complete absence of data-flow analysis is the direct
  tradeoff against that simplicity, and would be a genuine question for independent review to
  weigh against KST's stated SAST requirement categories (§7), several of which (source-to-sink
  data-flow defects) DevSkim does not claim to address at all.
- **Unable-to-Verify items:** complete current supported-language list (Rust/SQL inclusion); SARIF
  output support; telemetry/update-check behavior; release-signature/checksum verification
  process.

## 36. Owner / Independent-Review Questions (not answered here)

- Must one admitted SAST tool cover all three KST languages (C#, TypeScript, Rust), or is a
  combination of tools (or a tool plus continued reliance on compiler/clippy/manual review for the
  weakest-covered language) acceptable?
- Is a strong C#/TypeScript tool acceptable if Rust remains primarily protected by
  compiler/clippy/manual review/security regression tests, given all three researched candidates
  showed Rust as their least-mature surface?
- Must SAST run fully offline, or may a scanner download pinned rule/query packs from a vendor
  registry as part of normal operation?
- Must rules/query packs be version-pinned independently of the executable (as CodeQL's packaging
  model already structurally requires, and as Semgrep supports via local rule files but does not
  enforce by default)?
- Is any build-system execution acceptable during SAST analysis (relevant specifically to CodeQL's
  database-creation model), or must analysis remain strictly source-parse-only?
- For CodeQL specifically: does KST/Keytronic hold, or intend to obtain, a GitHub Advanced Security
  entitlement that would remove the private-codebase restriction identified in §10 and §35, or
  should CodeQL be evaluated only under the narrower Terms that currently apply to a private
  repository?
- Should generated OpenAPI TypeScript code (`src/frontend/src/generated`) be scanned at all, given
  it is already excluded from ESLint?
- Should vendor/tool severity labels be retained only as metadata, consistent with how KST already
  treats other admitted tools' severity output?
- Should SAST findings be stored in SARIF outside Git, inside Git, or elsewhere, and who owns
  triage of any findings?
- What level of source-snippet exposure in SAST reports is acceptable, given all three candidates'
  finding models inherently include file/line/snippet context?
- Is Semgrep Registry rule content (licensed for "internal business use" under the Semgrep Rules
  License v.1.0, not a fully open OSI license) an acceptable rule-provenance model for KST, or does
  policy require only OSI-licensed rule content?

This document does not answer these questions; enacted policy does not currently resolve them.

## 37. Future Capability Boundary

Nothing in this document constitutes admission, installation, execution, configuration, or
scanning of any SAST candidate against KST. A future admission pass (following independent review
and an explicit project-owner decision) would be expected to: verify exact installed
version/provenance; verify rule/query-pack version/provenance; disable network/cloud behavior
where supported; confirm no source upload occurs; confirm the repository remains clean before and
after; test the candidate on disposable synthetic vulnerable examples created outside this
repository; verify at least one supported security rule per KST language where the candidate
claims support; scan KST locally; store output outside the repository; inspect the report for
source/sensitive-data exposure; record findings without reproducing secrets; verify SARIF/JSON
structure; repeat the scan to check finding stability; confirm the repository is unchanged; not
suppress or remediate automatically; and stop for owner review. This is a plan outline only — no
step in it was executed during this research pass.

## 38. Non-Work (Confirmed)

During this research pass, the following were **not** done, consistent with the hard prohibition
in the source task:

- No SAST tool (Semgrep, CodeQL, DevSkim, Sonar tools, or any Roslyn security-analyzer package) was
  installed, downloaded, or executed.
- No VS Code or Visual Studio extension was installed.
- No query pack, rule pack, or ruleset was downloaded for execution.
- KST was not scanned by any candidate.
- No synthetic vulnerable project was created.
- No SAST configuration, baseline, suppression, or ignore file was created.
- No CI workflow or Git hook was created.
- No KST source or findings were uploaded to any external service.
- No login to any scanner service occurred.
- No dependency, source, build, or configuration file was changed.
- No recommendation, ranking, or admission decision was made.

## 39. Conclusion

**No SAST tool recommendation was made. No SAST tool was admitted. No SAST tool was installed or
executed.** This document is research evidence only, prepared for independent review and an
eventual project-owner admission decision for `S0.3-G006`. `S0.6 — Security Tool Admission` remains
**IN PROGRESS**, with Capability Reviews 1–3 COMPLETE/ACCEPTED and Capability Review 4 (this
document) now **RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW / NO TOOL ADMITTED**. `S0.3-G006`
itself remains open — it is **not** marked implemented or resolved by this document.

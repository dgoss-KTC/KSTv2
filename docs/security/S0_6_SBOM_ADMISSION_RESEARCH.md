# S0.6 Capability Review 3 — Software Bill of Materials (SBOM)

**Status: RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW AND OWNER DECISION**
**NO TOOL RECOMMENDATION OR ADMISSION DECISION**

| Item | Value |
|---|---|
| Gap addressed | `S0.3-G008` — "no SBOM capability (exact format also an unresolved policy decision)" |
| Research date | 2026-08-27 |
| Starting commit | `2579368fecca4c85b6fa4a757d62a2fa157b60d7` (subject: "docs: accept secret scanning capability") |
| Overall S0.6 status | **IN PROGRESS** (Capability Review 1 — Rust Dependency Advisory Capability — COMPLETE/ACCEPTED; Capability Review 2 — Dedicated Secret Scanning — COMPLETE/ACCEPTED; this review closes no capability by itself) |

This document is **research evidence only**. It does not recommend or admit an SBOM tool. It does
not install, download, or execute any candidate tool. The actual admission decision belongs to the
project owner, informed by an independent review of this packet.

**Repository Observation (important, recorded transparently per `AGENTS.md` §1):** a prior working
session apparently intended to hand this repository a completed SBOM research packet for
independent review — see the pasted task text that initiated this session, which refers to
correcting an inaccurate ClearlyDefined statement in an existing `S0_6_SBOM_ADMISSION_RESEARCH.md`
and recording an owner admission decision for Syft v1.51.1 already made from that packet. No such
research document existed anywhere in this repository or its history at the start of this session
(`docs/security/` contained no SBOM-related file, and `git log` shows no commit that ever added
one). `docs/status/CURRENT_PROJECT_STATUS.md` and `KST-v2-Master-Project-Checklist.md` both
confirm the accepted authoritative state: Capability Review 3 (`S0.3-G008`) remains **NOT
STARTED**. Per `AGENTS.md` §3 ("Do Not Guess Business Rules") and §18 ("Uncertainty Policy"), this
session did not fabricate the missing research packet's assumed prior content, did not treat the
pasted "owner decision" as genuine (it explicitly represents itself as a reaction to research that
does not exist), and did not install, download, or admit any SBOM tool. Instead, this document is a
newly produced, first-pass research packet for the gap, so that a genuine independent
review/admission decision can occur next. The ClearlyDefined correction requested in the pasted
task has been incorporated directly into §6 below (as accurate, sourced information) rather than
applied as an edit to a document that never existed.

---

## 1. Purpose and Authority Boundary

Per the established S0.6 process (see `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md` and
`docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md`), three roles are intentionally
separated:

- **Research agent (this document):** collect and organize evidence.
- **Independent review:** compare the evidence and formulate a recommendation.
- **Project owner:** make the actual admission decision.

No `ADMIT`/`DEFER`/`REJECT` disposition, preferred candidate, or "winner" is stated anywhere in
this document.

## 2. Governing Authority

- `AGENTS.md` (Tier 1), `SECURITY.md` (Tier 1).
- `docs/security/SECURITY_ASSURANCE_POLICY.md`, `docs/security/DEPENDENCY_ADMISSION.md`,
  `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`, `docs/security/AI_SECURITY_REVIEW.md`,
  `docs/security/APPLICATION_SECURITY_PROFILE.md` (Tier 1 normative policy documents).
- `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §8 (S0.6 — Security Tool
  Admission): one-capability-at-a-time process, human approval required before installation.
- `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §11 (Tier 3): source of gap `S0.3-G008`.
- `docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md` and
  `docs/security/S0_6_SECRET_SCANNING_ADMISSION_RESEARCH.md` / `..._ADMISSION.md` (Tier 3 — accepted
  Capability Review 1/2 evidence, used here only as a structural template; not modified).
- `docs/status/CURRENT_PROJECT_STATUS.md`, `KST-v2-Master-Project-Checklist.md` (Tier 2).

## 3. Starting Repository State

- **Branch:** `agents/pasted-text-processing-692b9d0d`. `HEAD` (`2579368f...`) is byte-identical to
  `origin/main` at the start of this session.
- **Working tree:** clean at the start of this pass.
- **`SECURITY.md` / status accepted state at start:** S0.1–S0.5 COMPLETE/ACCEPTED; S0.6 IN
  PROGRESS; Capability Review 1 (cargo-audit 0.22.2) COMPLETE/ACCEPTED; Capability Review 2
  (Gitleaks v8.30.0) COMPLETE/ACCEPTED; remaining S0.6 capability reviews `G006` (SAST) and `G008`
  (SBOM) **NOT STARTED**.
- No SBOM-related document existed in `docs/security/` before this document was created.

## 4. S0.3-G008 Gap

From accepted S0.3 evidence (`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §11, row
"SBOM"):

> No SBOM generator exists in the toolchain. Not executed. Gap `S0.3-G008`: no SBOM capability
> (exact format also an unresolved policy decision).

This is a **capability gap**: no SBOM generation tool is currently authorized, installed, or
present anywhere in the KST development environment, and no SBOM output format has been adopted as
policy.

## 5. KST Multi-Ecosystem Shape (relevant to SBOM tool selection)

Repository inspection confirms KST v2 is a genuinely multi-ecosystem Windows application:

- **npm/frontend** — `src/frontend` (React/TypeScript), governed by `package.json` /
  `package-lock.json`.
- **NuGet/.NET** — `src/backend` (`Kst.Domain`, `Kst.Application`, `Kst.Infrastructure`,
  `Kst.Integrations.Qad`, `Kst.Integrations.Shortages`, `Kst.Exports`, `Kst.Api`), published as a
  self-contained single-file `Kst.Api.exe` sidecar per `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`
  / existing publish tooling.
- **Cargo/Rust** — the Tauri desktop host, governed by `Cargo.toml` / `Cargo.lock`, including
  platform-conditional (e.g., Linux-only GTK-family) dependencies in the lock graph that do not
  ship on Windows.

Any SBOM approach must be evaluated against all three ecosystems plus the single-file publish
boundary, not just one.

## 6. Candidate 1 — Anchore Syft

| Field | Value |
|---|---|
| Project | `anchore/syft` |
| Candidate version referenced by owner pre-decision | v1.51.1 (confirmed as a real, current GitHub release: tag `v1.51.1`, published 2026-08-27, Windows/Linux/macOS release assets present) |
| License | Apache-2.0 |
| Maintainer | Anchore, Inc. (commercially backed OSS) |
| Distribution | Prebuilt binaries (GitHub Releases), plus Homebrew/Scoop/Chocolatey/Nix/Docker; install script (`get.anchore.io/syft`) also offered |
| Ecosystem coverage | Broad multi-ecosystem cataloger: npm/Node, NuGet/.NET, Cargo/Rust, plus dozens more (Alpine, Debian, RPM, Go, Python, Java, Ruby, PHP, and others) |
| Scan targets | Container images, filesystems/directories, archives — i.e., can scan a source/build tree directly, not only a packaged image |
| Output formats | SPDX (multiple versions, JSON/tag-value), CycloneDX (multiple versions, JSON/XML), Syft's own native JSON, and format conversion between them |
| Vulnerability scanning | Not built in; designed to pair with a separate tool (Grype) — **out of scope for this admission**, not requested |

**License-enrichment / network-behavior correction (per project-owner-provided instruction to
correct one factual research point):** the pasted task described a claim that *Microsoft sbom-tool
v4.1.5 makes an external ClearlyDefined license-enrichment call by default*, and stated this wording
was inaccurate. Because no research document existed here to literally edit, the corrected,
sourced position is recorded directly:

> Microsoft `sbom-tool`'s own `README.md` states it "uses the [Component Detection] libraries to
> detect components and the [ClearlyDefined] API to populate license information for these
> components." This documents ClearlyDefined as the tool's license-information mechanism, but the
> reviewed public documentation does not establish that this external enrichment call is
> unconditionally made on every ordinary run irrespective of configuration — some sbom-tool CLI
> surfaces (e.g., dedicated license-fetch subcommands/flags in newer releases) suggest license
> enrichment can be a distinguishable, optionally-invoked step rather than an unconditional default
> of the core `generate` path. This distinction (**optional/configurable external enrichment** vs.
> **unconditional default behavior of ordinary SBOM generation**) could not be fully resolved from
> documentation alone and would need to be confirmed by inspecting the actual installed CLI's
> `--help` output and default behavior before any implementation-verification claim is made. This
> is recorded as an **Unable-to-Verify-from-documentation-alone** item, not a settled fact in either
> direction.

## 7. Candidate 2 — Microsoft `sbom-tool`

| Field | Value |
|---|---|
| Project | `microsoft/sbom-tool` |
| Candidate version referenced by owner pre-decision | v4.1.5 (confirmed as a real GitHub release, published 2025-12-15) |
| License | MIT |
| Maintainer | Microsoft |
| Distribution | GitHub Releases (Windows/Linux/macOS executables), WinGet, Homebrew, `dotnet tool install --global Microsoft.Sbom.DotNetTool`, Docker |
| Ecosystem coverage | Delegates component detection to `microsoft/component-detection`, which supports npm, NuGet, Cargo, and other ecosystems |
| Output formats | SPDX 2.2 (default) and SPDX 3.0 (`-mi SPDX:3.0`) — **no CycloneDX output** |
| Notable design point | Requires explicit `-b` (drop/build path), `-bc` (build-components path), `-pn`/`-pv`/`-ps` (package name/version/supplier), and `-nsb` (namespace base URI) — i.e., first-party identity fields must be supplied explicitly by the caller rather than inferred |
| Known concerns raised in the pasted owner-decision context | reported .NET 10 compatibility and npm dependency-detection issues, and maintenance-status uncertainty — these were **not independently re-verified in this pass** (no research packet existed to verify them against); they should be treated as **unconfirmed** until checked against the actual upstream issue tracker if this candidate is revisited |

Because sbom-tool does not emit CycloneDX, and KST's task explicitly wants a CycloneDX 1.6 output
verified alongside SPDX 2.3, sbom-tool alone cannot satisfy both admitted output formats without a
second, separate conversion or generation step.

## 8. Candidate 3 — CycloneDX ecosystem-native tools

| Field | Value |
|---|---|
| `cyclonedx-dotnet` | v6.2.0 confirmed current GitHub release; Apache-2.0; official CycloneDX .NET generator (NuGet-focused, works from `.csproj`/`.sln`/`packages.lock.json` evidence) |
| `cyclonedx-npm` | v6.0.1 confirmed current GitHub release; Apache-2.0; official CycloneDX Node/npm generator (works from `package-lock.json`) |
| `cargo-cyclonedx` | v0.5.9 confirmed current GitHub release; Apache-2.0/MIT dual; official CycloneDX Rust/Cargo generator (works from `Cargo.lock`/`cargo metadata`) |

This is a three-tool, ecosystem-native approach: one specialized generator per ecosystem, each
producing a separate CycloneDX document that would need to be merged/aggregated to represent KST as
a whole. It does not natively emit SPDX. It introduces three separate admitted executables/update
surfaces instead of one.

## 9. Comparison Summary (descriptive only — no scoring, no recommendation)

| Dimension | Syft | Microsoft sbom-tool | CycloneDX-native (3 tools) |
|---|---|---|---|
| Single tool for npm+NuGet+Cargo | Yes | Yes (via component-detection) | No — three separate tools |
| SPDX output | Yes (multiple versions) | Yes (2.2 default, 3.0 optional) | No |
| CycloneDX output | Yes (multiple versions) | No | Yes (native per-ecosystem) |
| Requires explicit first-party identity args | No (infers from scan target; limited first-party metadata) | Yes (`-pn`/`-pv`/`-ps`/`-nsb` required) | Varies per tool |
| Distribution to Windows | Prebuilt binary, no admin required | Prebuilt binary, WinGet, .NET global tool | Per-tool: NuGet global tool / npm global package / cargo subcommand |
| Supply-chain surface | One binary, one update cadence | One binary, one update cadence, plus a runtime dependency on the separate `component-detection` library and (per README) ClearlyDefined for license data | Three binaries/packages, three update cadences, three maintainers within one CycloneDX org |
| Vulnerability scanning | No (pairs with Grype — not requested here) | No | No |
| License | Apache-2.0 | MIT | Apache-2.0 (dotnet, npm) / dual Apache-2.0+MIT (cargo) |

## 10. KST-Specific Open Questions (for owner/independent review, not resolved here)

1. **Two-view model.** Does the owner want a single "build/repository evidence" SBOM generator run
   once across the whole repo (Syft's model), or three ecosystem-native invocations aggregated
   together (CycloneDX-native model)? This materially affects tool count and update-surface count.
2. **Single-file `Kst.Api.exe` sidecar.** None of the three candidates has been verified in this
   pass against KST's actual single-file self-contained publish output; that verification requires
   installing a tool, which is outside this research-only document's scope.
3. **Windows-only shipped surface vs. full lockfile graph.** KST's `Cargo.lock` is known (from prior
   accepted evidence) to include platform-conditional dependencies not shipped on Windows. Whether
   Syft, sbom-tool, or the CycloneDX Cargo generator distinguish platform-specific lock entries from
   shipped-on-Windows entries was **not verified** in this research pass and would require an actual
   installed-tool test.
4. **SPDX vs. CycloneDX as KST policy.** Not decided by this document. The pasted task text asked to
   generate both SPDX 2.3 JSON and CycloneDX 1.6 JSON "for implementation verification," explicitly
   without setting permanent policy — that framing is preserved here as an open question, not a
   decision.
5. **sbom-tool .NET/npm compatibility concerns.** Referenced in the pasted context but not
   independently re-verified against upstream issue trackers in this pass.

## 11. Explicit Non-Decisions

This document does **not**:

- recommend Syft, sbom-tool, or the CycloneDX-native approach over one another;
- resolve whether SPDX or CycloneDX (or both) becomes KST's SBOM policy format;
- authorize installing, downloading, or executing any of the tools above;
- claim to have verified sbom-tool's ClearlyDefined enrichment is or is not invoked by default —
  see §6, recorded as Unable-to-Verify-from-documentation-alone;
- alter `S0.3-G008`'s status (it remains **NOT STARTED** until a genuine owner admission decision
  is recorded against real research and, subsequently, real installation/verification evidence).

## 12. Next Step

Per the established S0.6 pattern, the next step is an **independent review** of this packet
followed by a **project-owner admission decision** (`ADMIT`/`DEFER`/`REJECT` per candidate,
recorded in a separate `docs/security/S0_6_SBOM_ADMISSION.md` document, mirroring
`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md` and
`docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`). Only after that genuine decision exists should
any tool be downloaded, integrity-verified, and installed.

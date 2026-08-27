# S0.6 — Security Tool Admission: Capability Review 3 — Software Bill of Materials (SBOM)

**S0.6 Capability Review 3 — Software Bill of Materials (SBOM)**
**Status: COMPLETE / ACCEPTED — 2026-08-27**

| Item | Value |
|---|---|
| Gap | `S0.3-G008` |
| Tool | Anchore Syft v1.51.1 |
| Owner admission decision | ADMITTED for installation and verification — 2026-08-27 |
| Implementation | **COMPLETE — 2026-08-27** (see §9) |
| Project-owner acceptance | **ACCEPTED — 2026-08-27** |
| Anchore Syft v1.51.1 disposition | ADMITTED / IMPLEMENTED / ACCEPTED |
| `S0.3-G008` disposition | Covered / Resolved |
| Overall S0.6 status | **IN PROGRESS** (this review closes one capability only; S0.6 as a whole is **not** complete; `G006` remains NOT STARTED) |
| Research evidence | `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md` |

This document is **evidence, not normative policy**. It records the S0.6 Capability Review 3
owner admission decision and (as implementation proceeds) installation, verification, and scan
evidence for the SBOM capability (accepted S0.3 gap `S0.3-G008`). Required security properties and
tool-admission governance remain defined by `SECURITY.md`,
`docs/security/SECURITY_ASSURANCE_POLICY.md`, and `docs/security/DEPENDENCY_ADMISSION.md`. This
document is separate from, and does not modify, the neutral research packet at
`docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`.

---

## 1. Purpose and Status

S0.6 evaluates missing security-tool capabilities **one at a time** under the enacted
dependency-admission process (`docs/security/DEPENDENCY_ADMISSION.md`), per the accepted
remaining-S0 plan (`docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §8).

Capability Review 3 addresses:

> **S0.3-G008** — no SBOM generator exists in the toolchain; no SBOM output format has been
> adopted as policy (accepted S0.3 evidence).

Capability Review 1 (Rust dependency advisories, `S0.3-G001`) and Capability Review 2 (dedicated
secret scanning, `S0.3-G007`) are separately COMPLETE / ACCEPTED — see
`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md` and
`docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`. This document does not modify that evidence.

## 2. Governing Scope

- Canonical remaining-S0 plan: `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md`
  (§8 — S0.6 Security Tool Admission).
- Enacted policy: `SECURITY.md`, `docs/security/SECURITY_ASSURANCE_POLICY.md`,
  `docs/security/DEPENDENCY_ADMISSION.md`, `AGENTS.md` (§8 security requirements).
- Research packet consulted (unmodified by this document):
  `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`. That packet made **no tool recommendation and
  no admission decision**; this document records the human admission decision and subsequent
  implementation evidence separately, preserving that boundary.

## 3. Starting State

- **Session provenance:** an earlier pass in this session correctly discovered that no SBOM
  research artifact actually existed in the repository (despite an initiating prompt assuming one
  did), and — rather than fabricate it — produced a genuine neutral research packet
  (`docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`) and stopped without installing anything. That
  packet remained uncommitted at the start of this pass.
- **Commit:** `2579368fecca4c85b6fa4a757d62a2fa157b60d7` (`docs: accept secret scanning
  capability`); `HEAD == origin/main` at the start of this pass.
- **Working tree at start of this pass:** the single untracked path
  `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`, no other changes — as expected.
- **Accepted security state:** S0.1–S0.5 COMPLETE / ACCEPTED; S0.6 Capability Review 1 and
  Capability Review 2 COMPLETE / ACCEPTED; S0.6 Capability Review 3 (this document) research
  complete, owner decision now recorded; `G006` NOT STARTED; S0.7/S0.8 NOT STARTED; Stage 9
  blocked pending S0 closeout.
- **Machine state (at owner-decision time):** no SBOM generator installed on the workstation
  (confirmed in the accepted S0.3 tool-availability pass and the Capability Review 3 research
  packet).

## 4. Owner Admission Decision

The project owner and independent reviewer reviewed the Capability Review 3 research and the
project owner made the following explicit human decision on 2026-08-27:

### 4.1 Anchore Syft v1.51.1 — ADMITTED

> **Anchore Syft v1.51.1 ADMITTED for installation and verification — 2026-08-27.**
>
> Purpose: local generation of KST Software Bills of Materials using repository/build dependency
> evidence, with complementary packaged-artifact inspection.

### 4.2 Microsoft sbom-tool v4.1.5 — DEFERRED

> Credible capability, but independent review identified current KST-relevant
> compatibility/correctness and maintenance uncertainties.

This is not a rejection.

### 4.3 CycloneDX ecosystem-native approach — DEFERRED

> Credible approach (`cyclonedx-dotnet` 6.2.0, `cyclonedx-npm` 6.0.1, `cargo-cyclonedx` 0.5.9),
> but it introduces three tool admissions, three maintenance/supply-chain surfaces, and an
> aggregation strategy. Retained as a fallback if Syft cannot meet KST's empirically verified
> coverage requirements.

None of the deferred candidates are rejected; they remain valid future candidates.

## 5. Accepted SBOM Model

### 5.1 Build/repository evidence view

Purpose: npm dependency inventory, Cargo dependency inventory, NuGet/.NET dependency inventory,
first-party/build context. This is the primary, most-complete view.

### 5.2 Packaged-artifact view

Purpose: shipped files, shipped binaries, native/bundled material, `Kst.Api` sidecar presence.
Complementary to, not a replacement for, the build/repository view.

The owner-approved model explicitly does **not** claim that the final executable alone
reconstructs the complete dependency graph — this must be measured empirically during
implementation, not assumed.

## 6. Admitted Operating Boundary

The admitted capability is **local SBOM generation** from KST build/repository evidence, with a
complementary packaged-artifact scan.

The admitted capability is explicitly **not**:

- vulnerability scanning (Syft has none built in; pairs with the separate tool Grype, which is
  **not admitted**);
- SBOM publication, upload, or signing;
- a permanent choice of SPDX vs. CycloneDX as KST policy;
- CI integration or a release gate;
- a claim that every lockfile component ships on Windows.

## 7. Output Formats for Verification

Implementation verification will attempt both:

```text
SPDX 2.3 JSON
CycloneDX 1.6 JSON
```

if installed Syft v1.51.1 supports those exact versions/formats — the installed CLI will be used
as syntax authority; format flags will not be guessed in advance. This checkpoint does not choose
a permanent organizational SBOM standard.

## 8. Implementation Starting State

Recorded 2026-08-27 against starting commit `51a9b39de89611b997805acc7acd9712738c7beb` (`docs:
admit Syft SBOM capability`), `HEAD == origin/main`, working tree clean, nothing staged — the
commit at which the owner admission decision above was recorded.

## 9. Implementation Evidence

### 9.1 Existing-Binary Discovery and Independent Verification

An executable already existed at the admitted installation path:

```text
%LOCALAPPDATA%\KST\SecurityTools\syft\1.51.1\syft.exe
```

Per the governing task, this was **not** trusted merely because it existed. Its provenance was
independently established against a freshly downloaded, freshly verified official release before
any use against KST (see §9.2–§9.3).

### 9.2 Release Integrity Verification

| Item | Value |
|---|---|
| Release | Anchore Syft v1.51.1 |
| Published | 2026-08-27T17:01:15Z |
| Tag | `v1.51.1`, target commitish `main` |
| Tag object | `d749d4242889aa3c9cf6e43376626a9a4943066c` — **unsigned** (`verified: false`, `reason: unsigned`) |
| Target commit | `91a0032987d91b7411b52f6f5c185c5e7f775495` — GPG-signed, verified by GitHub (`verified: true`, `reason: valid`) |
| Windows artifact | `syft_1.51.1_windows_amd64.zip` (29,907,177 bytes) |
| Official checksum (`syft_1.51.1_checksums.txt`) | `5e4bc3e6b6344b4625de0f7aa5351aaa72856d11d78462972de0a101ee2c1c8f` |
| GitHub release-asset digest (via GitHub API) | `sha256:5e4bc3e6b6344b4625de0f7aa5351aaa72856d11d78462972de0a101ee2c1c8f` — matches |
| Locally computed SHA-256 of downloaded `.zip` | `5e4bc3e6b6344b4625de0f7aa5351aaa72856d11d78462972de0a101ee2c1c8f` — matches both values above |
| Published signature/attestation artifacts observed | `syft_1.51.1_checksums.txt.pem`, `syft_1.51.1_checksums.txt.sig` (Sigstore-style checksum-file signing material) — observed as present; not independently verified with Cosign, which was **not** installed per the governing task's scope boundary |

As with the prior Gitleaks admission, the annotated tag object itself is unsigned; only the
underlying target commit carries a verified GPG signature. This distinction is preserved
deliberately. Checksum, GitHub asset digest, and commit signature are recorded as separate,
non-conflated pieces of evidence.

### 9.3 Existing-Installed-Binary Verification and Installation

The official `.zip` was downloaded to a temporary directory (outside the repository) and
extracted. SHA-256 of the freshly extracted `syft.exe` and the pre-existing installed `syft.exe`
were computed and compared:

| Item | Value |
|---|---|
| Freshly extracted `syft.exe` SHA-256 | `5a95b689def43f26de1505ed43dc76306cded7923140b349619e21ddc2ac5ce4` |
| Pre-existing installed `syft.exe` SHA-256 | `5a95b689def43f26de1505ed43dc76306cded7923140b349619e21ddc2ac5ce4` — **identical** |

Because the hashes matched exactly, the pre-existing installed binary was retained in place
(not overwritten) at `%LOCALAPPDATA%\KST\SecurityTools\syft\1.51.1\syft.exe`. No administrator
elevation was used. The executable was not added to `PATH`; it was invoked by absolute path
throughout this evidence pass. No copy was made into the KST repository.

### 9.4 Version Gate

`syft version` (absolute path) reported:

```text
Application:    syft
Version:        1.51.1
BuildDate:       2026-08-27T16:55:02Z
GitCommit:       91a0032987d91b7411b52f6f5c185c5e7f775495
Platform:        windows/amd64
```

`GitCommit` matches the independently verified, GPG-signed target commit in §9.2. Version and
commit gates both pass.

### 9.5 Network / Update-Check Behavior

`syft config` (installed v1.51.1 configuration-help output) documents:

```text
check-for-app-update: true   # env: SYFT_CHECK_FOR_APP_UPDATE
```

This confirms the exact mechanism named by the governing task before use. `SYFT_CHECK_FOR_APP_UPDATE=false`
was set as a **process environment variable for this evidence-collection session only**; no
repository configuration file was created or changed to hold this setting.

Observed behavior during this pass: Syft performed local directory scanning against on-disk
build/repository and packaged-artifact evidence only. No source upload, SBOM upload,
vulnerability-service query, or license-enrichment query was intentionally invoked (Syft has no
built-in vulnerability matching; `enrich` defaulted to `[]` and was not changed; `search-remote-licenses`
was not enabled). This is an observational statement about this run's configuration and invocation,
not an independently packet-captured, forensic proof of zero network activity.

### 9.6 Build Evidence Collected

Established repository build/publish commands (`docs/development/BUILD_AND_TEST.md`) were used;
no new build workflow was invented and no dependency/build-configuration file was changed:

| Ecosystem | Command | Result |
|---|---|---|
| .NET (NuGet) | `dotnet restore Kst.slnx` (from `src/backend`) | Restored 12 projects; produced `obj/project.assets.json` per project |
| .NET (packaged) | `dotnet publish Kst.Api/Kst.Api.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false -o ../../publish/backend` (from `src/backend`) | Produced the self-contained single-file `publish/backend/Kst.Api.exe` and companion files |
| npm | `npm install` (from `src/frontend`) | 278 packages added, 0 vulnerabilities reported by `npm`; `package-lock.json` unchanged |
| Cargo | none run (`cargo build`/`cargo check` not required) | Used the existing `Cargo.lock`/`Cargo.toml` directly; `cargo metadata --format-version 1` was run read-only to inspect target-conditional dependency structure (§9.11); no `cargo update` |

`git status --short` was recorded before and after each command. The `dotnet publish` step
regenerated `docs/openapi/Kst.Api.json` (an existing `OpenApiGenerateDocumentsOnBuild` side
effect of building `Kst.Api`); `git diff` showed **zero changed lines** for that file (a pure
LF→CRLF line-ending normalization artifact, not a content change), so the path was restored with
`git checkout -- docs/openapi/Kst.Api.json` after semantic inspection confirmed no content
difference. No other tracked file was touched by any build/publish/restore/install command. All
generated build outputs (`bin/`, `obj/`, `node_modules/`, `publish/`) are already covered by
`.gitignore`.

### 9.7 Main Build/Repository SBOM Generation

**Scan boundary:** `dir:src` (the repository's `src/` tree — `src/frontend`, `src/backend`,
`src/tauri` — run from the repository root after the restore/install steps in §9.6). This
boundary was chosen to retain npm (`package-lock.json`, `node_modules`), Cargo
(`Cargo.toml`/`Cargo.lock`), and NuGet/.NET (`obj/project.assets.json`, `bin/**/*.deps.json`,
`bin/**/*.dll`) build metadata together, while excluding `.git` noise.

**Commands** (all with `SYFT_CHECK_FOR_APP_UPDATE=false` set per §9.5):

```text
syft scan "dir:src" -o "spdx-json=<tmp>/repo-spdx.json" -o "cyclonedx-json@1.6=<tmp>/repo-cyclonedx.json"
```

**Format-version note (genuine finding, see §9.20 `S0.6-F014`):** the unqualified `cyclonedx-json`
format defaults to **CycloneDX 1.7** in Syft v1.51.1, not 1.6. The admitted CycloneDX 1.6 output
requires the explicit version-qualified selector `cyclonedx-json@1.6`, which was confirmed (via
CLI help and by generating and inspecting a test document) to correctly emit `specVersion: "1.6"`.
The unqualified `spdx-json` format was confirmed to default to **SPDX 2.3** (`spdxVersion:
"SPDX-2.3"`), matching the admitted target without a version qualifier.

**Results:**

| Format | Value |
|---|---|
| SPDX | `spdxVersion: SPDX-2.3`; 1,027 packages; 3,761 relationships; creators: `Organization: Anchore, Inc`, `Tool: syft-1.51.1` |
| CycloneDX | `bomFormat: CycloneDX`, `specVersion: 1.6`; 1,026 components |

Component purl-scheme breakdown (SPDX, 1,027 packages): `nuget` 514, `cargo` 479, `npm` 6,
`github` 5 (GitHub Actions usages found in workflow YAML, not application dependencies),
`golang` 2 (an embedded Go binary — `esbuild`, a native tool bundled by an npm package — detected
by Syft's binary classifier, plus its embedded Go stdlib version), 21 with no purl (Syft
first-party/local-source packages such as `kst-frontend` and `kst-tauri`, which have no registry
source).

### 9.8 npm Representative Coverage

| Field | Direct example | Transitive example |
|---|---|---|
| Package | `react` | `scheduler` |
| Version | `19.2.8` | `0.27.0` |
| Basis | Direct `dependencies` entry in `src/frontend/package.json` | Dependency of `react-dom` (`"dependencies": {"scheduler": "^0.27.0"}` in `package-lock.json`) |
| SPDX presence | Yes — `pkg:npm/react@19.2.8` | Yes — `pkg:npm/scheduler@0.27.0` |
| CycloneDX presence | Yes — `pkg:npm/react@19.2.8` | Yes — `pkg:npm/scheduler@0.27.0` |
| Relationship | — | SPDX relationship recorded: `scheduler` `DEPENDENCY_OF` `react-dom` |
| purl | `pkg:npm/react@19.2.8` | `pkg:npm/scheduler@0.27.0` |

**Genuine finding (see `S0.6-F015`):** only 6 npm packages (of 278 physically installed under
`node_modules`) were catalogued in this default-configuration run — `kst-frontend` (root),
`react`, `react-dom`, `scheduler`, `@tauri-apps/api`, `@tauri-apps/plugin-shell`. `syft config`
confirms a documented default `javascript.include-dev-dependencies: false` (env:
`SYFT_JAVASCRIPT_INCLUDE_DEV_DEPENDENCIES`); KST's frontend `devDependencies` (eslint, vite,
vitest, typescript, and 25 others physically present in `node_modules`) were excluded from this
default-configuration scan. Per the governing task, Syft configuration was **not** modified to
force additional components to appear — this default-scope boundary is recorded as a finding
instead.

### 9.9 NuGet Representative Coverage

| Field | Direct example | Transitive example |
|---|---|---|
| Package | `Serilog.AspNetCore` | `Serilog` |
| Version | `9.0.0` (also `9.0.0-main-bf7a68d` from one DLL-evidence entry, see finding) | `4.2.0` (also `4.2.0.0` from one DLL-evidence entry) |
| Basis | Direct `PackageReference` in `src/backend/Kst.Api/Kst.Api.csproj` | Dependency of `Serilog.AspNetCore` |
| Source evidence used | `obj/project.assets.json`, `bin/**/*.deps.json`, `bin/**/*.dll` (each independently catalogued) | Same three evidence classes |
| SPDX presence | Yes — 3 entries, `pkg:nuget/Serilog.AspNetCore@9.0.0` (x2) and `pkg:nuget/Serilog.AspNetCore@9.0.0-main-bf7a68d` (x1) | Yes — 3 entries, analogous pattern |
| CycloneDX presence | Yes — same 3-entry pattern | Yes — same 3-entry pattern |
| purl | `pkg:nuget/Serilog.AspNetCore@9.0.0` | `pkg:nuget/Serilog@4.2.0` |

**Genuine finding (see `S0.6-F016`):** the same logical NuGet package is reported multiple times
— once per independent evidence artifact Syft's .NET catalogers examine (`project.assets.json`,
`*.deps.json`, and DLL assembly-version metadata). One of the three `Serilog.AspNetCore` entries
carries the assembly file version (`9.0.0-main-bf7a68d`, read from the DLL) rather than the NuGet
package version (`9.0.0`), which differs from the other two entries for the same physical
package. This is genuine cataloger behavior, not a fabricated example.

### 9.10 Cargo Representative Coverage

| Field | Direct example | Transitive example |
|---|---|---|
| Package | `tokio` | `mio` |
| Version | `1.53.1` | `1.2.2` |
| Basis | Direct `[dependencies]` entry in `src/tauri/Cargo.toml` (`tokio = { version = "1", features = ["full"] }`) | Dependency of `tokio` (`Cargo.lock` `dependencies = [..., "mio", ...]` under the `tokio` package entry) |
| SPDX presence | Yes — `pkg:cargo/tokio@1.53.1` | Yes — `pkg:cargo/mio@1.2.2` |
| CycloneDX presence | Yes — `pkg:cargo/tokio@1.53.1` | Yes — `pkg:cargo/mio@1.2.2` |
| purl | `pkg:cargo/tokio@1.53.1` | `pkg:cargo/mio@1.2.2` |

Unlike the NuGet case, each Cargo package appeared exactly once (single evidence source:
`Cargo.lock`).

### 9.11 Cargo Platform-Specific (Windows-Boundary) Result

`cargo metadata --format-version 1` (read-only; no lockfile change) was used to inspect
target-conditional dependency structure. A clean, unambiguous non-Windows example was identified:

> `zbus` (v5.18.0) — resolved only under `cfg(target_os = "linux")`, pulled in transitively by
> Tauri's tray-icon feature for Linux D-Bus integration. It has no Windows applicability.

`zbus` **is present** in the `dir:src` main-scan SBOM (`pkg:cargo/zbus@5.18.0`, both formats)
with **no** target/platform-conditionality marker distinguishing it from Windows-relevant crates.
This reconfirms — now specifically measured for the SBOM capability — the boundary already
established in accepted S0.3 evidence: `Cargo.lock` represents the **full resolved dependency
graph** across all platforms Tauri supports, not the **actual Windows-shipped inventory**. Syft's
Cargo lock cataloger does not filter or annotate by target platform. No exclusion was created;
this is recorded as an observation (`S0.6-F018`, see §9.20).

### 9.12 NuGet Single-File Sidecar Result

`Kst.Api` is published as a self-contained single-file (`publish/backend/Kst.Api.exe`).
Contrary to an a priori assumption that a single-file executable would be an unrecoverable
capability boundary, this was **measured directly**: scanning the packaged artifact (§9.13)
recovered **37 distinct NuGet package identities** (type `dotnet`) directly from within
`Kst.Api.exe` alone — including `Kst.Api` itself, the full `Serilog.*` family,
`Microsoft.Data.SqlClient`, `Azure.Identity`, the `Microsoft.IdentityModel.*` family, and others
— all reported with the single evidence location `\Kst.Api.exe`. Syft's .NET single-file/bundle
cataloger reads the embedded runtime bundle manifest (the single-file host's internal equivalent
of `*.deps.json`) to recover package identities without needing the separate build-tree evidence
used in §9.7/§9.9. The final executable **does** reconstruct a usable NuGet package graph on its
own for this artifact; this is not claimed to generalize to every possible single-file publish
configuration.

### 9.13 First-Party/Root-Component Representation

- `Kst.Api`, `Kst.Domain`, `Kst.Application` (and, by the same pattern, the other first-party
  KST .NET projects) are represented identically to external NuGet dependencies — each carries a
  synthesized `pkg:nuget/<ProjectName>@0.1.0-alpha.2` purl with no supplier, organization, or
  first-party marker, and (in the main build-view scan) is duplicated across multiple evidence
  sources exactly as described for `Serilog.AspNetCore` in §9.9.
- `kst-frontend` (root npm package) appears exactly once, correctly reflecting the `name`/`version`
  fields from `src/frontend/package.json`, with no purl (private/local package, no registry
  source — expected, not an error).
- `kst-tauri` (root Rust crate) appears exactly once, with no purl (local path source — expected).
- No supplier, namespace, organization ID, purl, or version was invented for any of these; this
  section records Syft's actual, observed default representation only (`S0.6-F017`, see §9.20).

### 9.14 Packaged-Artifact Boundary

No complete Tauri Windows installer/packaged application existed in this workspace at the start
of this pass (`src/tauri/target` did not exist), and building one was **not** performed, because
doing so solely to satisfy this security checkpoint would materially expand build/tooling scope
beyond what the governing task authorized. Per the governing task's explicit fallback, the
established `dotnet publish` self-contained single-file output (`publish/backend/`) was used as
the bounded packaged-artifact verification view instead.

**What was scanned:** `publish/backend/` (the published `Kst.Api` sidecar: `Kst.Api.exe` and
companion native/config files).

**What was not scanned:** a full Tauri Windows installer/bundle (`.msi`/`.exe` installer,
bundled webview assets, bundled Tauri sidecar layout) — classified **Unable to Verify / future
packaged-release verification boundary**, not Syft failure and not Accepted Risk.

### 9.15 Packaged-Artifact Scan

```text
syft scan "dir:publish/backend" -o "syft-json=<tmp>/packaged-syft.json" -o "spdx-json=<tmp>/packaged-spdx.json" -o "cyclonedx-json@1.6=<tmp>/packaged-cyclonedx.json"
```

| Item | Value |
|---|---|
| Scan boundary | `publish/backend/` (self-contained single-file `Kst.Api` publish output) |
| Component count | 39 (syft-json artifacts): 37 `dotnet`, 2 `binary` |
| SPDX packages | 40 |
| CycloneDX components | 39 |
| `Kst.Api` presence | Yes — single `dotnet`-type entry, evidence location `\Kst.Api.exe` |
| Native/binary observations | `aspnetcorev2_inprocess.dll` → "IIS ASP.NET Core Module V2 Request Handler" (Commit `20.0.26140.9`); `Microsoft.Data.SqlClient.SNI.dll` → "Microsoft.Data.SqlClient.SNI" (`6.02.0`) — both identified from embedded native file-version metadata |
| npm visibility | None (expected — .NET-only sidecar artifact, not the Tauri host or frontend bundle) |
| Cargo visibility | None (expected, same reason) |
| NuGet visibility | 37 packages, all recovered from the single embedded bundle manifest inside `Kst.Api.exe` (see §9.12) |

This scan alone is not treated as the authoritative dependency graph — see the build-vs-packaged
comparison below.

### 9.16 Build-View vs Packaged-View Comparison

| Dimension | Build/repo view (`dir:src`) | Packaged view (`dir:publish/backend`) |
|---|---|---|
| SPDX packages | 1,027 | 40 |
| CycloneDX components | 1,026 | 39 |
| npm visibility | 6 packages (direct+transitive runtime deps only; devDependencies excluded by default, §9.8) | none |
| Cargo visibility | 479 packages (full `Cargo.lock` graph, including non-Windows crates, §9.11) | none |
| NuGet visibility | 514 packages, multiple duplicated evidence sources (§9.9) | 37 packages, single embedded-bundle-manifest evidence source (§9.12) |
| Native/binary | not separately prominent (dominated by managed-code evidence) | 2 native DLLs identified by embedded file-version metadata |
| `Kst.Api` | present — multiple duplicated `dotnet`/`binary` entries from build-tree evidence | present — single `dotnet` entry from the embedded bundle manifest |
| Platform-specific dependencies | present without platform qualification (e.g. `zbus`, §9.11) | not applicable (no Cargo visibility in this artifact) |

Raw component count alone is not used as proof of completeness for either view; each view's
scope and limitations are recorded above.

### 9.17 SPDX 2.3 Verification

- JSON validity: valid (parsed successfully with a standard JSON parser).
- `spdxVersion == SPDX-2.3`: confirmed.
- Creation/tool metadata: present (`creators`: `Organization: Anchore, Inc`, `Tool: syft-1.51.1`;
  `licenseListVersion: 3.28`).
- Packages present: 1,027 (main scan).
- Representative npm/NuGet/Cargo components present: confirmed (§9.8–§9.10).
- Relationships: present (3,761 in the main scan), including the `scheduler` → `react-dom`
  `DEPENDENCY_OF` relationship and numerous `evident-by` file relationships.
- purls/checksums/licenses: purls present for all registry-sourced packages; per-package
  checksums present but not independently re-verified in this pass; licenses present but
  frequently `NOASSERTION` (§9.19).
- Formal independent SPDX schema validation: **Unable to Verify during this checkpoint** — no
  already-admitted formal validator exists in KST's toolchain, and none was installed, per the
  governing task's scope boundary.

### 9.18 CycloneDX 1.6 Verification

- JSON validity: valid.
- `bomFormat == CycloneDX`: confirmed.
- `specVersion == 1.6`: confirmed (only when the explicit `@1.6` selector is used — see §9.7
  finding).
- Components present: 1,026 (main scan).
- Representative npm/NuGet/Cargo components present: confirmed (§9.8–§9.10).
- Dependency structure: present in the document's `dependencies` array (not independently
  re-tabulated beyond the SPDX relationship-count parity noted in §9.17).
- purls/hashes/licenses: purls present; licenses present only for the npm examples (`MIT`),
  absent for the NuGet/Cargo examples (§9.19).
- Formal independent CycloneDX schema validation: **Unable to Verify during this checkpoint** —
  no already-admitted formal validator exists; none was installed.

### 9.19 Sensitive-Metadata Review

Both generated main-scan documents were searched for the current Windows username, the machine
hostname, and `C:\Users\` path prefixes — **no matches**. Recorded file-location paths are
relative to the `src` scan root (e.g. `\backend\Kst.Api\bin\Release\net10.0\win-x64\...`), not
absolute developer paths. A keyword search for credential-shaped strings (`password`,
`api_key`/`apikey`, PEM private-key headers) found **no matches** in either generated document.
No genuine credential or material secret was identified in any generated SBOM; no redaction
beyond routine scope selection was required.

### 9.20 Semantic Repeatability

The main `dir:src` scan was generated twice with identical options (`spdx-json` +
`cyclonedx-json@1.6`).

| Check | Result |
|---|---|
| Byte-identical | No |
| Semantically consistent component inventory | **Yes** — identical (name, version) set across all 1,027 packages in both runs |
| Semantically consistent relationships | **Yes** — identical set of 3,761 relationship triples in both runs |

The only observed differences between the two runs were the SPDX `creationInfo.created`
timestamp and the `documentNamespace` UUID — both are expected non-semantic variation per the
governing task's own guidance (timestamps, serial numbers/document IDs). No finding was raised
for repeatability; no STOP condition was triggered.

### 9.21 License Metadata Observation

- npm representative packages (`react`, `scheduler`): declared SPDX license `MIT` present in both
  SPDX (`licenseDeclared: MIT`) and CycloneDX (`licenses: [{license: {id: "MIT"}}]`) output,
  sourced from each package's `package.json` `license` field.
- NuGet representative packages (`Serilog`, `Serilog.AspNetCore`) and Cargo representative
  packages (`tokio`, `mio`): `NOASSERTION` (SPDX) / no `licenses` field (CycloneDX) in this
  generation.
- No external license enrichment was performed (`enrich` remained at its default `[]`;
  `search-remote-licenses` was not enabled).
- License-metadata completeness materially varies by ecosystem under Syft's default
  configuration in this repository; recorded as `S0.6-F019` (§9.22).

### 9.22 Findings

Before assigning any new finding ID, existing accepted S0.6 finding IDs were inspected: the
highest previously assigned ID is `S0.6-F013` (`docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`
§7.9). New Capability Review 3 findings therefore begin at `S0.6-F014`. All six findings below
arise genuinely from this implementation pass; none is pre-created from prior chat text.

| ID | Summary | Evidence | Scope | Blocks G008? | Disposition |
|---|---|---|---|---|---|
| `S0.6-F014` | Unqualified `cyclonedx-json` output defaults to CycloneDX **1.7**, not the admitted **1.6**; the explicit `cyclonedx-json@1.6` selector is required and was confirmed to work correctly. | §9.7 — measured via CLI help and generated test documents | Format-version default behavior | No | Informational / Confirmed workaround in use — always invoke with the explicit `@1.6` selector for this admitted capability |
| `S0.6-F015` | Default-configuration `dir` scans exclude npm `devDependencies` (`javascript.include-dev-dependencies` defaults to `false`); KST's substantial frontend dev-tooling footprint (eslint, vite, vitest, typescript, and 25 others physically present in `node_modules`) is not represented in this default scan. | §9.8 — 6 of 278 installed npm packages catalogued | Cataloger/config default limitation, npm ecosystem coverage | No | Informational / cataloger-default limitation — not remediated by forcing config; documented as a scope boundary |
| `S0.6-F016` | The same logical NuGet package (first-party and third-party) is reported multiple times — once per independent evidence artifact (`project.assets.json`, `*.deps.json`, DLL assembly-version metadata) — sometimes with a divergent version string (NuGet package version vs. embedded assembly file version). | §9.9, §9.13 — `Serilog.AspNetCore`/`Serilog` and first-party project examples | Duplicate/noisy package representation, NuGet ecosystem | No | Informational — SBOM consumers should deduplicate by purl, not raw component count |
| `S0.6-F017` | First-party KST .NET projects are represented identically to external NuGet dependencies, with no supplier/organization/first-party distinguishing attribute. | §9.13 | First-party component representation limitation | No | Informational — no supplier/namespace value was invented to compensate |
| `S0.6-F018` | The main build/repository Cargo scan includes non-Windows/platform-conditional crates (e.g. `zbus`, Linux-only) without any target/platform qualifier distinguishing them from Windows-relevant crates, reconfirming (now specifically for this SBOM capability) the previously known S0.3 lock-graph-vs-shipped-inventory boundary. | §9.11 | Platform-condition limitation, Cargo ecosystem | No | Informational — no exclusion created; documents an existing, previously known boundary |
| `S0.6-F019` | License-metadata completeness varies materially by ecosystem under default Syft configuration: npm representative packages carry SPDX license IDs from `package.json`; representative NuGet and Cargo packages show `NOASSERTION`/no license value. | §9.21 | License-metadata limitation | No | Informational observation — no external enrichment performed or planned under this checkpoint |

None of the above are classified Accepted Risk; no organizational severity is assigned (none is
authorized under enacted policy for this checkpoint); none blocks `S0.3-G008`.

### 9.23 Network / Data-Handling Result

This run intentionally performed: **none** of source upload, SBOM upload, vulnerability-service
query, license-enrichment query, or credential transmission. This is an observational statement
about this run's configuration and invocation (§9.5), not an independently packet-captured,
forensic proof.

### 9.24 Repository-Integrity Result

`git status --short`, `git diff --name-status`, and `git diff --stat` were run before
documentation updates. The only tracked-file effect of the entire implementation pass was the
transient, content-identical regeneration of `docs/openapi/Kst.Api.json` (§9.6), which was
restored via `git checkout --` after semantic inspection confirmed zero content difference. The
tracked working tree was otherwise clean throughout. All generated SBOM/report output was written
under the OS temporary directory (outside the repository) and deleted after evidence extraction
(§9.25). No generated SBOM was committed.

### 9.25 Temporary Output Cleanup

All temporary SPDX/CycloneDX/syft-json output files, the downloaded release archive and its
extraction, and the temporary comparison files used to produce this evidence were created under
the OS temporary directory and deleted after evidence extraction. The admitted installed Syft
executable remains at `%LOCALAPPDATA%\KST\SecurityTools\syft\1.51.1\syft.exe`.

### 9.26 Trust Limitations and Unable-to-Verify Items

The admitted capability, as implemented, is **local SBOM generation** from KST build/repository
evidence (`dir:src`) plus a complementary packaged-artifact scan (`dir:publish/backend`) using
Syft v1.51.1. It is explicitly not: vulnerability scanning, SBOM publication/upload/signing, a
permanent SPDX-vs-CycloneDX policy choice, CI integration, or a claim that every catalogued
component ships on Windows (§9.11). Unable-to-Verify items: formal independent SPDX schema
validation (§9.17); formal independent CycloneDX schema validation (§9.18); full Tauri Windows
packaged-application coverage (§9.14).

## 10. Implementation Status

**S0.6 Capability Review 3 — Software Bill of Materials:**
**COMPLETE / ACCEPTED — 2026-08-27.**

**Anchore Syft v1.51.1:**
**ADMITTED / IMPLEMENTED / ACCEPTED.**

**`S0.3-G008`:**
**Covered / Resolved.**

Microsoft sbom-tool v4.1.5 and the CycloneDX ecosystem-native approach (cyclonedx-dotnet,
cyclonedx-npm, cargo-cyclonedx) remain **DEFERRED** — evaluated during S0.6 research
(`docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`) but not adopted, per the owner admission
decision to proceed with Syft alone.

Findings `S0.6-F014` through `S0.6-F019` retain their genuine, evidence-backed meanings recorded
above (§9.19) and are finalized as **Informational / Known Capability Boundaries — Non-blocking**.
None of `S0.6-F014`–`S0.6-F019` is Accepted Risk, and none received an organizational severity
rating. Prior findings `S0.6-F001` through `S0.6-F013` (Capability Reviews 1 and 2) are unchanged
by this acceptance.

Full Tauri Windows installer/application-bundle SBOM coverage was not verified during Capability
Review 3; the complementary packaged-artifact verification covered only the published
self-contained single-file `Kst.Api.exe` sidecar (§9.14, §9.26). This remains classified **Unable
to Verify / future packaged-release verification boundary** — it is not a Syft failure, not
Accepted Risk, and not proof of complete final-installer coverage. This boundary does not block
`S0.3-G008` acceptance.

## 11. Final Acceptance

**Authoritative owner decision — 2026-08-27:**

> S0.6 Capability Review 3 — Software Bill of Materials — **COMPLETE / ACCEPTED**.
> Anchore Syft v1.51.1 — **ADMITTED / IMPLEMENTED / ACCEPTED**.
> `S0.3-G008` — **Covered / Resolved**.
> Microsoft sbom-tool v4.1.5 — **DEFERRED**. CycloneDX ecosystem-native approach — **DEFERRED**.
> `S0.6-F014` through `S0.6-F019` — **Informational / Known Capability Boundaries —
> Non-blocking**; none is Accepted Risk.
> The complete Tauri Windows installer/application bundle remains **Unable to Verify / future
> packaged-release verification boundary**; this does not block `G008` acceptance.
> Overall: **S0.6 — IN PROGRESS**; `S0.3-G006` (dedicated SAST) — **NOT STARTED**.

The project owner reviewed the genuine Syft v1.51.1 implementation evidence recorded in §8–§9 of
this document and explicitly accepted Capability Review 3 without requesting rework, without
altering any recorded implementation fact, and without reclassifying any finding as Accepted Risk.
No implementation evidence was regenerated, rerun, or rewritten to record this acceptance; only the
status/disposition fields above were updated. Acceptance of `S0.3-G008` does not begin, and does not
imply progress on, `S0.3-G006` (dedicated SAST), `S0.7`, `S0.8`, or Stage 9.

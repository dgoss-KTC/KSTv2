# Application Versioning

This document describes how KST v2 tracks and propagates its application version. It
is a lightweight, durable foundation — not a release/CI-CD system, not an
auto-updater. While KST v2 remains in numbered alpha-stage development, the alpha
prerelease number is tied to the current project Stage.

## Product identity vs. application version

These are two separate concepts and must not be conflated:

- **Product identity**: `KST v2` (also shown as "Keytronic Scheduler's Toolbox"). This
  is the name of the product. It does not change with every release.
- **Application version**: a [Semantic Versioning 2.0.0](https://semver.org/) string,
  e.g. `0.1.0-alpha.1`. This identifies a specific build/release of the product.

The project's Stage number is an internal roadmap concept, but during alpha development it is also
encoded in the prerelease suffix: work on Stage `N` uses `0.1.0-alpha.N`. This identifies the active
stage represented by a build; it does not make a build accepted, authorize implementation, or turn
the stage number into part of the permanent product identity.

## Current application version

```
0.1.0-alpha.15
```

This is a pre-1.0 build associated with Stage 15 work. The version advances to `alpha.N` when Stage
`N` becomes the current stage being planned or implemented. It remains at that value through the
stage until the project moves to another numbered stage.

**Stage 15 version change (2026-10-07):** owner-authorized with 15.2; active stage 15 and suffix
alpha.15 propagated through `scripts/check-version.ps1 -Fix`, with own-package lock metadata only.
Numeric installer version stays `0.1.0`. Offline sidecar republish produced matching published/Tauri
executables with ProductVersion `0.1.0-alpha.15`; neither executable nor desktop was launched.
Runtime/health/desktop version confirmation remains separately authorized 15.3 scope.

**Stage 14 version change (2026-10-02, historical):** the owner directed the advance to `0.1.0-alpha.14` with
Checkpoint 14.2 (`KstActiveStage` 14). `Directory.Build.props` was updated, `check-version.ps1 -Fix`
updated `Cargo.toml` and `package.json`, and the application's own version entries in
`src/tauri/Cargo.lock` and `src/frontend/package-lock.json` were edited directly (no dependency
change). `tauri.conf.json` stays numeric-only `0.1.0`. The sidecar was not republished at 14.2, so
no packaged-binary `/health` or ProductVersion evidence exists yet for `alpha.14`.

**13.6 verification (2026-10-01, historical):** `scripts/check-version.ps1` passed with active Stage 13;
the republished Tauri external binary returned `0.1.0-alpha.13` from `/health` and its Windows
ProductVersion was `0.1.0-alpha.13`. The frontend production build uses the same package version.
This is build/version evidence, not by itself evidence of QAD/QXtend success. The
separate bounded live-read observations and owner-reported external QXtend outcomes are recorded
in `docs/implementation/KST_v2_STAGE_13_CLOSEOUT.md`. The owner separately and explicitly accepted
Checkpoint 13.6 and Stage 13 completion on 2026-10-01; the version stays `0.1.0-alpha.13` until a
later separately authorized stage. Stage 14 is not begun or versioned by this closeout.

## Version format

KST v2 uses [SemVer 2.0.0](https://semver.org/): `MAJOR.MINOR.PATCH[-PRERELEASE]`.

- `MAJOR.MINOR.PATCH` — incremented per normal SemVer rules once the project starts
  shipping real releases.
- `-alpha.N` — during the current stage-based alpha period, `N` must equal the active numbered
  project Stage.
- Other prerelease families such as `beta.N` or `rc.N` require a later explicit release decision;
  they are not governed by the stage-number rule.
- The prerelease suffix is dropped once/if a stable release is cut.

Retired or absorbed stage numbers do not need synthetic releases. Administrative work between
stages retains the current stage version unless the owner explicitly changes the active stage.

## Authoritative source

The single authoritative source of truth for the application version is
[`src/backend/Directory.Build.props`](../../src/backend/Directory.Build.props):

```xml
<PropertyGroup>
  <KstActiveStage>15</KstActiveStage>
  <VersionPrefix>0.1.0</VersionPrefix>
  <VersionSuffix>alpha.15</VersionSuffix>
  <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
</PropertyGroup>
```

`KstActiveStage` is the machine-readable stage marker. During numbered alpha work,
`VersionSuffix` must equal `alpha.<KstActiveStage>`; the version-check script and architecture test
enforce that relationship. This file is automatically imported by every backend `.csproj` under `src/backend/`,
so no per-project edits are needed. The .NET SDK combines `VersionPrefix` and
`VersionSuffix` into:

- `Version` / `InformationalVersion` / `PackageVersion` → `0.1.0-alpha.15` (full SemVer
  string, including the pre-release suffix).
- `AssemblyVersion` / `FileVersion` → `0.1.0.0` (numeric-only — Windows assembly/file
  version fields do not support a pre-release suffix; the SDK's default behavior of
  dropping the suffix for these two fields is used as-is, not worked around).

`IncludeSourceRevisionInInformationalVersion` is explicitly set to `false`. Without
this, the .NET SDK automatically appends `+<git-commit-sha>` to `InformationalVersion`
when building inside a Git repository, which would make the reported/displayed version
drift from the plain SemVer string tracked in `Cargo.toml`/`tauri.conf.json`/
`package.json`. Capturing a commit hash for build diagnostics is optional and not
required by this project; the plain SemVer string is the required identifier.

**Empirically verified** at Stage 13 (published sidecar `Kst.Api.exe`, via
`[System.Diagnostics.FileVersionInfo]::GetVersionInfo(...)`):

| Field            | Value           |
|------------------|-----------------|
| `FileVersion`    | `0.1.0.0`       |
| `ProductVersion` | `0.1.0-alpha.13` |

## Propagation

| File | Expected value | What it drives |
|------|-----------------|----------------|
| `src/backend/Directory.Build.props` | `VersionPrefix`-`VersionSuffix` (currently `0.1.0-alpha.15`) plus `KstActiveStage` | Authoritative version and active-stage source. All backend assemblies' `Version`/`InformationalVersion`/`AssemblyVersion`/`FileVersion`. |
| `src/tauri/Cargo.toml` (`[package].version`) | Full authoritative version (e.g. `0.1.0-alpha.15`) | The Tauri/Rust desktop host crate version. Cargo/SemVer has no problem with pre-release identifiers. |
| `src/tauri/tauri.conf.json` (`.version`) | **Numeric-only** `VersionPrefix` (e.g. `0.1.0`, no suffix) | The packaged installer's product version (NSIS/MSI). See [Windows MSI/WiX numeric-only constraint](#windows-msiwix-numeric-only-constraint) below. |
| `src/frontend/package.json` (`.version`) | Full authoritative version (e.g. `0.1.0-alpha.15`) | The frontend package version (also shown via `npm run`/build tooling). |

### Windows MSI/WiX numeric-only constraint

**Empirically discovered** while verifying a packaged build: Tauri's Windows MSI/WiX bundler
rejects a non-numeric SemVer pre-release identifier in `tauri.conf.json`'s `version` field:

```
failed to bundle project: `optional pre-release identifier in app version must be numeric-only
and cannot be greater than 65535 for msi target`
```

The NSIS bundler does not have this restriction, but since `tauri.conf.json` has a single
`version` field shared by both installer targets, `tauri.conf.json`'s version is kept
numeric-only (just `VersionPrefix`, e.g. `0.1.0`) so both installers can be built. This affects
only the Tauri host binary's own file-version metadata and the installer's product version - it
does **not** affect the application's actual reported/displayed version, which always comes from
the backend's `InformationalVersion` (full `0.1.0-alpha.15`, see [Propagation](#propagation)
above) via the system-status API, independent of Tauri's own app/package version.

Verified at Stage 13 (release build, `target/release/kst-tauri.exe`):

| Source | Value |
|--------|-------|
| `kst-tauri.exe` FileVersion/ProductVersion (from `tauri.conf.json`) | `0.1.0` |
| Backend `applicationVersion` (from `Directory.Build.props`, shown in the app's top bar) | `0.1.0-alpha.13` |

At runtime, the backend derives its reported version directly from the built
assembly's `AssemblyInformationalVersionAttribute` (see `src/backend/Kst.Api/Program.cs`)
— **not** from configuration (`appsettings.json`) and **not** by shelling out to `git`
on the end user's workstation. This value flows into:

- `GET /api/v1/system/status` (`applicationVersion` field).
- `GET /health` (`backendVersion` field).
- Structured startup log line: `KST backend starting. Version={Version} ...`.
- The frontend top bar (`v{version}`), which reads the same system-status response.

## Updating the version

1. When Stage `N` becomes the current stage being worked, set `KstActiveStage` to `N` and
   `VersionSuffix` to `alpha.N` in `src/backend/Directory.Build.props`. Planning approval and
   implementation authorization remain separate project decisions.
2. Run `scripts/check-version.ps1 -Fix` from the repository root. This reads the new
   authoritative version and rewrites `Cargo.toml`, `tauri.conf.json`, and
   `package.json` to match.
3. Run `scripts/check-version.ps1` (without `-Fix`) to confirm all files now agree
   (exit code `0`).
4. Rebuild/re-test as normal (backend `dotnet build`/`dotnet test`, frontend
   lint/typecheck/test/build, `cargo check`), then rebuild the sidecar via
   `scripts/build-sidecar.ps1`.
5. Update the root package lock and Cargo lock metadata if their workspace package versions changed.
6. The `Kst.ArchitectureTests` project's `VersionConsistencyTests` will fail the build if
   `KstActiveStage` and `VersionSuffix` disagree or if propagated files drift out of sync,
   independent of the script.

## Git tags

Meaningful versions (e.g. release candidates, notable milestones) may be tagged with
an annotated Git tag of the form `v<version>` (e.g. `v0.1.0-alpha.13`), pointing at the
commit that introduced/matches that version. Not every commit needs a tag.

## Configuration schema

Application **version** (this document) is a distinct concept from any future
**configuration schema version** (e.g. a version field inside a persisted config file
format, to support migrations between incompatible config layouts). No configuration
schema versioning currently exists anywhere in this repository, and none is
introduced by this document. If/when persisted configuration needs its own
migration story, it should get its own explicit versioning scheme rather than
reusing the application version.

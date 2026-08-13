# Application Versioning

This document describes how KST v2 tracks and propagates its application version. It
is a lightweight, durable foundation — not a release/CI-CD system, not an
auto-updater, and not tied to project "Stage" numbers.

## Product identity vs. application version

These are two separate concepts and must not be conflated:

- **Product identity**: `KST v2` (also shown as "Keytronic Scheduler's Toolbox"). This
  is the name of the product. It does not change with every release.
- **Application version**: a [Semantic Versioning 2.0.0](https://semver.org/) string,
  e.g. `0.1.0-alpha.1`. This identifies a specific build/release of the product.

The project's "Stage" numbers (Stage 5, Stage 6, etc., tracked in
[KST-v2-Master-Project-Checklist.md](../../KST-v2-Master-Project-Checklist.md)) are an
internal implementation-roadmap concept and are intentionally **not** encoded in the
application version.

## Current application version

```
0.1.0-alpha.2
```

This is a pre-1.0, pre-release build. The version will be incremented deliberately as
the project matures (see [Updating the version](#updating-the-version) below).

## Version format

KST v2 uses [SemVer 2.0.0](https://semver.org/): `MAJOR.MINOR.PATCH[-PRERELEASE]`.

- `MAJOR.MINOR.PATCH` — incremented per normal SemVer rules once the project starts
  shipping real releases.
- `-PRERELEASE` (e.g. `-alpha.1`, `-beta.2`) — used while the product is still under
  active pre-release development. Dropped once/if a stable `1.0.0` is cut.

## Authoritative source

The single authoritative source of truth for the application version is
[`src/backend/Directory.Build.props`](../../src/backend/Directory.Build.props):

```xml
<PropertyGroup>
  <VersionPrefix>0.1.0</VersionPrefix>
  <VersionSuffix>alpha.1</VersionSuffix>
  <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
</PropertyGroup>
```

This file is automatically imported by every backend `.csproj` under `src/backend/`,
so no per-project edits are needed. The .NET SDK combines `VersionPrefix` and
`VersionSuffix` into:

- `Version` / `InformationalVersion` / `PackageVersion` → `0.1.0-alpha.1` (full SemVer
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

**Empirically verified** (published sidecar `Kst.Api.exe`, via
`[System.Diagnostics.FileVersionInfo]::GetVersionInfo(...)`):

| Field            | Value           |
|------------------|-----------------|
| `FileVersion`    | `0.1.0.0`       |
| `ProductVersion` | `0.1.0-alpha.1` |

## Propagation

| File | Expected value | What it drives |
|------|-----------------|----------------|
| `src/backend/Directory.Build.props` | `VersionPrefix`-`VersionSuffix` (e.g. `0.1.0-alpha.1`) | Authoritative source. All backend assemblies' `Version`/`InformationalVersion`/`AssemblyVersion`/`FileVersion`. |
| `src/tauri/Cargo.toml` (`[package].version`) | Full authoritative version (e.g. `0.1.0-alpha.1`) | The Tauri/Rust desktop host crate version. Cargo/SemVer has no problem with pre-release identifiers. |
| `src/tauri/tauri.conf.json` (`.version`) | **Numeric-only** `VersionPrefix` (e.g. `0.1.0`, no suffix) | The packaged installer's product version (NSIS/MSI). See [Windows MSI/WiX numeric-only constraint](#windows-msiwix-numeric-only-constraint) below. |
| `src/frontend/package.json` (`.version`) | Full authoritative version (e.g. `0.1.0-alpha.1`) | The frontend package version (also shown via `npm run`/build tooling). |

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
the backend's `InformationalVersion` (full `0.1.0-alpha.1`, see [Propagation](#propagation)
above) via the system-status API, independent of Tauri's own app/package version.

Verified (release build, `target/release/kst-tauri.exe`):

| Source | Value |
|--------|-------|
| `kst-tauri.exe` FileVersion/ProductVersion (from `tauri.conf.json`) | `0.1.0` |
| Backend `applicationVersion` (from `Directory.Build.props`, shown in the app's top bar) | `0.1.0-alpha.1` |

At runtime, the backend derives its reported version directly from the built
assembly's `AssemblyInformationalVersionAttribute` (see `src/backend/Kst.Api/Program.cs`)
— **not** from configuration (`appsettings.json`) and **not** by shelling out to `git`
on the end user's workstation. This value flows into:

- `GET /api/v1/system/status` (`applicationVersion` field).
- `GET /health` (`backendVersion` field).
- Structured startup log line: `KST backend starting. Version={Version} ...`.
- The frontend top bar (`v{version}`), which reads the same system-status response.

## Updating the version

1. Edit `VersionPrefix`/`VersionSuffix` in `src/backend/Directory.Build.props`.
2. Run `scripts/check-version.ps1 -Fix` from the repository root. This reads the new
   authoritative version and rewrites `Cargo.toml`, `tauri.conf.json`, and
   `package.json` to match.
3. Run `scripts/check-version.ps1` (without `-Fix`) to confirm all files now agree
   (exit code `0`).
4. Rebuild/re-test as normal (backend `dotnet build`/`dotnet test`, frontend
   lint/typecheck/test/build, `cargo check`), then rebuild the sidecar via
   `scripts/build-sidecar.ps1`.
5. The `Kst.ArchitectureTests` project's `VersionConsistencyTests` will fail the build
   if any of these files drift out of sync in the future, independent of the script.

## Git tags

Meaningful versions (e.g. release candidates, notable milestones) may be tagged with
an annotated Git tag of the form `v<version>` (e.g. `v0.1.0-alpha.1`), pointing at the
commit that introduced/matches that version. Not every commit needs a tag.

## Configuration schema

Application **version** (this document) is a distinct concept from any future
**configuration schema version** (e.g. a version field inside a persisted config file
format, to support migrations between incompatible config layouts). No configuration
schema versioning currently exists anywhere in this repository, and none is
introduced by this document. If/when persisted configuration needs its own
migration story, it should get its own explicit versioning scheme rather than
reusing the application version.

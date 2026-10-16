# S22-034 — bounded Pkcs dependency remediation

**Status: COMPLETE / OWNER-ACCEPTED / CLOSED — 2026-10-16.**

Current outcome: owner-authorized, admitted **Pkcs 10.0.13** replaces the effective affected asset;
published/bundled/debug/release identities match. The initial 9.0.21 failure is retained below as
historical evidence. Current verification, owner-reported desktop acceptance and retained limits are
at the end. Closing S22-034 does not close Stage 22.

Date: 2026-10-16. Owner-dispatched implementation checkpoint, now explicitly owner-accepted with a
focused commit authorized. Owner-reported desktop acceptance is separate from detailed live
authentication/query evidence. No push, shared deployment or new live source access is authorized.

## Entry baseline and plan

- Windows checkout `C:\Dev\kst_v2`, HEAD `1831fb7`; pre-existing uncommitted MPS navigation/export,
  eight-second success feedback, Part Info and reference work are protected against checkpoint entry,
  not overwritten with HEAD. Full entry hashes, tracked diff, status and project assets are retained at
  `C:\Dev\kst_v2_tmp\Stage22\S22-034\` (the evidence directory below).
- SDK/global.json: 10.0.401; target net10.0; API win-x64 self-contained single-file, untrimmed,
  non-AOT, native-library self-extraction. Existing central transitive pinning is enabled.
- Before: API → QAD / Shortages → Microsoft.Data.SqlClient **6.1.1** →
  System.Security.Cryptography.Pkcs **9.0.4**, for net10.0 and net10.0/win-x64.
  Both integrations directly reference SqlClient. No package-management restructuring is needed.
- Sequence: exact-version admission → one central transitive pin → restore/graph comparison → existing
  offline QAD/Shortages/API and workbook regressions → separate-context security review → established
  sidecar publication/copy and payload identity → safe local probe if source-free → final evidence.
- Keep driver, SDK/framework configuration, source queries/populations, locked Stage 9–11 rules,
  authentication configuration, contracts, frontend and native source unchanged.

## Initial exact-version admission — 9.0.21 ACCEPT (historical attempt)

Owner authorization covers Pkcs **9.0.21** and necessary admitted compatible patch closure only.
Normal permissive-license path under DEPENDENCY_ADMISSION and licensing policy §9 is supported:

| Required field | Evidence / determination |
|---|---|
| Component, exact version, ecosystem | System.Security.Cryptography.Pkcs 9.0.21; NuGet |
| Purpose / alternative | Replace affected existing transitive runtime cryptography assets; no new capability. Retaining 9.0.4 is not an acceptable remediation; driver/framework upgrade is unnecessary. |
| Official source / publisher | Existing NuGet.org feed; Microsoft / dotnet/runtime. Exact nupkg URL below. |
| Provenance | `dotnet nuget verify <nupkg> --all` succeeds: Microsoft author and NuGet.org repository signatures. Package nuspec repository commit `d280e4fb388c6deed22d480daada060b6b456008`. |
| Release history / activity | Official registration: listed, published 2026-10-13T16:58:10.57Z; current Microsoft servicing release. No publisher/ownership anomaly or malicious-package evidence identified in inspected authoritative metadata/signature evidence; this is not a comprehensive malware claim. |
| Artifact integrity | Nupkg SHA256 `A8A473808EE7B4A29EB4C917022D946D677A9AFECD565D176EB719FFE286AB9B`; verification content SHA512 retained in package-signature.log. |
| Exact authoritative license | Signed package `LICENSE.TXT`: MIT, copyright .NET Foundation and Contributors; nuspec MIT expression, not merely a registry badge. |
| Use classification | Class C runtime / internally bundled desktop sidecar; existing use model, not a developer-only dependency. No external/customer distribution authorized. |
| Commercial/business use / redistribution | MIT explicitly permits use, copy, modification, distribution, sublicensing and sale, subject to notice retention. |
| Attribution / notices | Retain copyright and permission notice and upstream THIRD-PARTY-NOTICES.TXT with distributed copies/substantial portions. Preserve exact package texts in local publication evidence. Final fleet/installer notice reconciliation remains the existing release gate, not waived here. |
| Source disclosure / copyleft / network copyleft | MIT imposes no source-disclosure, copyleft or network-copyleft requirement. |
| Custom/proprietary / private repository / field of use | No new restriction identified for this MIT package. Bundled runtime-wide third-party notice is byte-identical to 9.0.4 (SHA256 `40686C6447A7D5B5D3693068E4571B5F483D7ED335AEEE773EF662440DE4C5D5`); no changed third-party terms inferred or waived. |
| Seats / subscription / usage fees / trial | None imposed by the reviewed package license; no hosted service or paid capability introduced. |
| Transitives / compatibility | net9.0 package group has **no package dependencies**, compatible nearest assets for net10.0. Other framework groups are not selected. Expected graph delta: Pkcs only, subject to restore verification before build. |
| Install/build behavior | Static package inspection: managed reference/runtime libraries, nuspec/notices, compatibility buildTransitive assets; no install script/tool. Compatibility warning target only affects older unsupported TFMs; no suppression is introduced. |
| Terms independent of version / human escalation | Package license is included in versioned signed artifact. No new service terms; no material licensing ambiguity or escalation condition identified for this bounded unchanged use model. No legal/commercial risk accepted. |
| Decision / evidence date | **ACCEPT**, 2026-10-16, within explicit owner patch/restore authorization. No other version or identity admitted by this record. |

Exact artifact: https://api.nuget.org/v3-flatcontainer/system.security.cryptography.pkcs/9.0.21/system.security.cryptography.pkcs.9.0.21.nupkg

Authoritative registration: https://api.nuget.org/v3/registration5-gz-semver2/system.security.cryptography.pkcs/9.0.21.json

Rechecked Microsoft-published GitHub advisories (JSON snapshots retained):
- https://github.com/advisories/GHSA-2fmv-rv2c-59x7 — moderate, Windows EnvelopedCms.Decode
  attacker-supplied attributes can cause quadratic pre-authentication CPU exhaustion.
- https://github.com/advisories/GHSA-846f-f764-76mx — high, SignedCms.CheckSignature reparsing
  attacker-controlled X.509 data can cause quadratic CPU exhaustion.
- Both list 9.x **9.0.0 through 9.0.20 affected; 9.0.21 patched** at this evidence date.

## Exposure boundaries

No direct SignedCms, EnvelopedCms or Pkcs calls were found in application C# source. QAD readers
use QadConnectionFactory.OpenAsync → SqlConnection.OpenAsync; QAD connection construction retains
IntegratedSecurity=true. Shortages enrichment independently calls SqlConnection.OpenAsync using its
existing secret-loader/connection-string boundary. Driver internals/transitive crypto reachability
were not established as exploitable or unreachable; the observed dependency paths are sufficient to
require patching. This does not claim Windows authentication or SQL TLS is the advisory trigger.
No production secrets/configuration values were inspected or printed.

## Initial implementation and verification (historical attempt)

The only build configuration change is one central `PackageVersion` for Pkcs 9.0.21, using existing
transitive pinning. No direct PackageReference, new feed, audit suppression or other dependency pin.

Executed before independent review:
- Restore succeeds, zero warnings/errors. Resolved package lists/audits and selected assets retained.
  API, QAD, Shortages and three referencing test projects change Pkcs 9.0.4 → 9.0.21. No new closure
  package is required. SqlClient stays 6.1.1, SNI.runtime 6.0.2; other direct versions are untouched.
- Normal solution restore removes entry publish-context `Microsoft.NET.ILLink.Tasks/10.0.12` and
  additional win-x64 targets from the six library projects. This is an SDK-generated build-only
  context difference, not a dependency upgrade/removal in project configuration. Publish-context
  equivalence must be checked after the established publish pipeline.
- Compile asset: `lib/net9.0/System.Security.Cryptography.Pkcs.dll`; NuGet API win-x64 runtime candidate:
  `runtimes/win/lib/net9.0/System.Security.Cryptography.Pkcs.dll` from package 9.0.21. NuGet cache nupkg
  SHA256 matches the separately signature-verified admission artifact.
- API/QAD/Shortages `dotnet list <project> package --include-transitive --vulnerable --no-restore
  --format json` all succeed and report no vulnerable packages at this evidence date. Before reports
  list the two reported advisories on Pkcs 9.0.4. Feed/audit failures are not suppressed.
- `dotnet build src/backend/Kst.slnx --no-restore --nologo`: PASS, zero warnings/errors.
- `dotnet test src/backend/Kst.slnx --no-build --no-restore --nologo --logger trx`: **1,371 passed,
  zero failed/skipped**: Domain 358, Application 419, QAD/Shortages 331, architecture 10, API 253.
  This includes existing workbook/export, connection construction, SQL/read-only and API regressions.
  QAD tests use closed connections, synthetic tables and missing/unconfigured options; Shortages
  tests use placeholder temp fixtures/missing paths. API factory disables QAD and substitutes fixtures.
  Commands clear `KST_SECRETS_FILE` and QAD source settings; no live reads are authorized or executed.
- AFT inspection of the props file is PARTIAL (no LSP producer for that type), not a compiler pass.
  Successful restore/build/tests are the authoritative automated checks. No new version-assertion or
  exploit/stress test was added.

## Initial effective-payload blocker — publication stopped at 9.0.21

The NuGet candidate above is **not the effective self-contained runtime asset**. After the successful
build, `Kst.Api/bin/Debug/net10.0/win-x64/Kst.Api.deps.json` attributes Pkcs to
`runtimepack.Microsoft.AspNetCore.App.Runtime.win-x64/10.0.12`; the package runtime candidate does not
win framework conflict resolution. Byte evidence (not AssemblyVersion alone):

| Asset | Bytes | Product version | SHA256 |
|---|---:|---|---|
| Effective API build Pkcs DLL | 771,880 | `10.0.12+95017c711e6afc1085133d440e42b4bd78155701` | `5D537DC5A92C4624E030BB5A453849287076E8BC9011ED77C007E3D2E089880D` |
| ASP.NET win-x64 runtime pack 10.0.12 Pkcs DLL | 771,880 | same 10.0.12 | same `5D537D…880D` |
| Admitted package 9.0.21 Windows/net9.0 DLL | 319,784 | `9.0.21+d280e4fb388c6deed22d480daada060b6b456008` | `996B209DB304F9B623A5DBAE541D533401941AD6DADCD6C491E80C09AA0AFE1B` |

Effective file version is `10.0.1226.42308`; package DLL file version is `9.0.2126.50404`.
Both current advisories explicitly affect **10.0.0–10.0.12**, patched **10.0.13**. Thus the clean NuGet
audits do **not** resolve both advisories for the effective runtime. This is a confirmed blocker, not
an exploitability claim and not a finding accepted as risk.

No publication/copy/startup was run after discovering this blocker. The existing published artifact
was inspected using **already admitted Syft 1.51.1**, verified against its admitted executable SHA256
`5A95B689DEF43F26DE1505ED43DC76306CDED7923140B349619E21DDC2AC5CE4`. Its embedded single-file dependency
metadata reports ASP.NET and .NETCore runtime packs **10.0.12**, SqlClient **6.1.1**, SNI **6.0.2**;
there is no separate Pkcs package identity in that effective bundle inventory. This supports the
framework-supplied payload distinction; no scan badge is treated as a vulnerability fix.

All four existing sidecar files at entry have the following identical identity, **not a new patched
publication**:

- `publish/backend-sidecar/Kst.Api.exe`
- `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe`
- `src/tauri/target/debug/Kst.Api.exe`
- `src/tauri/target/release/Kst.Api.exe`
- Each: **127,855,236 bytes**, product `0.1.0-alpha.21`, file `0.1.0.0`, SHA256
  `7E7195F4451A04D57E6FB7A953A8F860026BD52B40F0C4EBE245FF6A934F05F8`.

No running application was terminated; no already-running process is claimed refreshed. No locks
were observed in the KST process-name check. The established sidecar script/settings remain unchanged,
including native SNI extraction. Final publish-context graph comparison is **not executed**, because
publication is blocked; normal-restore ILLink/RID context differences remain explicitly recorded.

### Smallest proposed scope decision — not applied

The independent review identifies an exact **Pkcs 10.0.13 central pin** as the smallest configuration
candidate: the same assembly major as the effective framework asset, with a patched servicing version.
Request fresh owner authorization to evaluate/admit it and its necessary exact closure, retaining
SDK 10.0.401, SqlClient 6.1.1, net10.0 and all application/native settings. It is **not admitted, installed,
or proven to win effective selection** by this checkpoint. Package-only success must not be assumed.

If that normal supported resolution cannot replace the effective asset, evaluate a coherent patched
self-contained ASP.NET/.NET 10 runtime-pack servicing selection under separate owner scope approval.
Its precise closure must be inspected/admitted before restore/install. Neither alternative is in the
authorized 9.0.21 net9.0 dependency group. No machine-wide SDK/runtime install is proposed as a
prerequisite without evidence. Do not force the older net9 package DLL over the framework's net10 DLL,
disable framework/pruning controls, or silently substitute Pkcs 10.x as a shortcut.

Continue **this same S22-034 checkpoint** after the owner scope decision: validate effective fixed
selection, rerun affected offline checks/review, publish through `scripts/build-sidecar.ps1`, verify
bundle/payload and all consumed copies, then source-disabled health/readiness/spec probe. Live
authentication/native export checks require owner execution/authorization; no QAD reads are authorized.

### Retained verification limits

- No changed authentication/source queries/business rules/DTO/frontend/native source. Offline tests
  do not prove live integrated authentication, SQL transport or real workbook/navigation/native behavior.
- Safe startup verifier and `/health`, `/ready`, static Part Info `/spec` source were inspected and
  make no source-system reads; execution is deferred with publication, not counted as a pass.
- Known full-solution dotnet-format, broad Rust-format and locked Stage 10 lint failures remain inherited
  exceptions, not passes. No unrelated frontend/native suites rerun. Existing S0 transport/host/save
  limitations and later release/license gates are unchanged and not risk-accepted.
- No unrelated advisories appeared in the three current NuGet audit reports; this is not a scan of
  all ecosystems/framework runtime vulnerabilities. Both effective-payload advisories remain open.
- Detailed logs, before/after assets, signature/license package, audit JSON, graph delta, TRX, effective
  DLL identity JSON and unchanged-bundle Syft inventory are retained in the evidence directory.
  Initial baseline capture encountered Git quoted-path and unsupported `nuget why --no-restore`
  errors; both were corrected before editing (core.quotePath=false and direct assets input). The first
  graph-only assertion stopped on ILLink context removal; full delta is retained and explained above.

## Initial independent review and checkpoint preservation

Separate-context, security-specific review completed through the existing agent review mechanism.
Full record: evidence-directory `independent-security-review.md`. It independently rehashed admission
artifacts, checked DLL/deps/runtime-pack ownership and advisory ranges, and summed all five TRX files.
Conclusion: **BLOCKED — one confirmed High finding S22-034-ISR-001**, reflecting the underlying High
advisory, not demonstrated KST exploitability. Admission and scoped/offline checks are supported, but
effective remediation and publication closeout are not. No risk accepted. No blocking finding is hidden
or treated as resolved by a clean package audit.

The reviewer rehashed all **943 checkpoint-entry files** before the final status/checklist updates;
only the central props file differed then. Final entry comparison additionally permits only the two
intentional status/checklist documentation edits. All other entry bytes, including pre-existing
modified/untracked source, OpenAPI/generated contracts, frontend build-info, pending Part Info,
navigation/export/eight-second correction and native work, are preserved. All four sidecar hashes
remain the entry identities above. No stage/acceptance advancement, reset, stash, branch switch,
commit, push, shared deployment, live source read or operational submission occurred.

Focused delivery: `src/backend/Directory.Packages.props` (comment + central Pkcs pin), this new
admission/checkpoint record, bounded additions to current status/master checklist, and only the
S22-034 row in the existing local Windows backlog export. Full entry-relative diffs and logs stay on
the laptop. `git diff --check` passes for the changed repository scope; AFT remains PARTIAL for these
non-LSP file types, not an additional clean-diagnostics claim.

## Owner-authorized continuation — exact 10.0.13 evaluation

The owner explicitly authorized the proposed exact Pkcs **10.0.13** admission/effective-selection
evaluation after the initial blocked delivery. This continues the same S22-034 checkpoint, with
unchanged prohibitions on live reads, SDK/driver/framework upgrades, tools/feeds, commit/push and shared
deployment. Runtime-pack servicing remains a fallback requiring its own scope decision if package-only
remediation cannot safely replace the effective asset.

### Exact 10.0.13 admission — ACCEPT before restore/application

Normal-path technical admission is supported within the owner's expanded exact-version authorization.
The initial blocked evidence above remains historical, not erased or claimed resolved by admission.

| Required field | Exact 10.0.13 evidence / determination |
|---|---|
| Identity / purpose / alternatives | System.Security.Cryptography.Pkcs **10.0.13**, NuGet; same existing runtime capability, patched assembly major matching the selected net10 runtime. 9.0.21 failed effective selection; broader framework servicing is unnecessary unless this candidate fails. |
| Official source / publisher | Microsoft; existing NuGet.org feed, exact nupkg URL below. Signed nuspec source repository dotnet/dotnet commit `4099f83977f4d29604a9aeb971fb0a7bc1d5f699`. |
| Integrity / provenance / release history | `dotnet nuget verify --all` succeeds, Microsoft author and NuGet.org repository signatures; SHA256 **54ED24578EAD34B5B50D259084243432F11733E2754B8B196E799D3418D304A7**. Official registration lists publication 2026-10-13T16:56:30.46Z. No publisher/ownership anomaly or malicious-package evidence identified in reviewed authoritative metadata; not a comprehensive malware assurance. |
| Exact authoritative license | Signed nuspec MIT expression plus **exact source commit** root/runtime LICENSE.TXT (links below). Unlike 9.0.21, package has no LICENSE.TXT; no registry badge is substituted for authoritative terms. |
| Intended use / commercial / redistribution | Class C internally bundled Windows desktop runtime, unchanged use model. MIT expressly permits use/copy/modification/distribution/sublicensing/sale with copyright/permission notice. No external/customer distribution authorized. |
| Notices / attribution | Preserve full MIT copyright/license and complete package THIRD-PARTY-NOTICES.TXT intact (SHA256 **6D15E10A101C6BFFF2AB4429ED061BF76C456FC4B23AD6B03E0D0F8377148A21**), including Unicode, Apache/OpenTelemetry and NIST sections. Retain texts with local publication evidence; eventual installer/fleet notice inclusion remains a release gate, not presumed complete. |
| Custom/proprietary/source-available / transitive-license assessment | Separate-context pre-admission review traced new NIST ACVP notice to explicitly included upstream **test vectors**, not the production Pkcs project; default source items disabled, no packaged test assembly/ACVP corpus. This is applicability evidence, not acceptance of custom terms. Runtime-wide notices also update permissive Unicode text and Apache identification. No newly identified custom/proprietary production terms or material licensing ambiguity for selected DLL. Full evidence below. |
| Source disclosure / copyleft / network copyleft | MIT imposes no source-publication, source-offer, copyleft or network-copyleft requirement for selected DLL. |
| Private-repository / field-of-use / trial | No such restriction in selected MIT grant. NIST custom terms are not waived; adding its test data or other custom-covered content requires Review/Hold. |
| Seat / subscription / payment / usage costs | None imposed by selected MIT terms; no paid service or hosted capability introduced. |
| Dependency tree / compatibility | Native net10.0 package group is **empty**; no additional NuGet closure needed. Expected only Pkcs 9.0.4 → 10.0.13 (intermediate 9.0.21 retained in history). No direct driver/framework upgrade or new identity. Restore/effective verification required before declaring success. |
| Install/build behavior | Static ZIP/source inspection: reference/runtime DLLs/XML, metadata/notices and compatibility-warning buildTransitive assets; no install script, executable tool or upstream test/data package introduced. No audit/framework/pruning suppression. |
| Terms independent of version / escalation | Version/commit-bound software terms, no new service contract. Pre-admission review found no applicable unresolved escalation condition for selected production input; no legal/commercial risk accepted. Re-review if evidence/use model changes. |
| Decision / evidence date | **ACCEPT — 2026-10-16**, before applying 10.0.13 to the build, within owner expanded authorization. |

Exact package:
https://api.nuget.org/v3-flatcontainer/system.security.cryptography.pkcs/10.0.13/system.security.cryptography.pkcs.10.0.13.nupkg

Exact source license:
https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/LICENSE.TXT

Pre-admission independent evidence: `pkcs-10.0.13-pre-admission-review.md` in the evidence directory.
It identifies `Common/tests/.../SlhDsa/SlhDsaTestData.cs`, explicit Pkcs **test** project inclusion,
production project exclusions and `EnableDefaultItems=false` at the same exact commit. Shared
`PackageThirdPartyNoticesFile` explains runtime-wide notice inclusion. No NIST test-data/custom-term
admission is implied. The refreshed advisory snapshots still list **10.0.13 patched for both reports**.

### 10.0.13 implementation and offline verification

- The central pin is now 10.0.13; no other project/dependency/authentication/framework configuration edit.
  Solution restore succeeds, zero warnings/errors. Comparison to intermediate 9.0.21 assets shows
  **only Pkcs 9.0.21 → 10.0.13** across API/QAD/Shortages and the three referencing test projects;
  no additional package identity/closure. Against original entry, Pkcs 9.0.4 → 10.0.13 plus the same
  documented normal-restore ILLink/RID context normalization. Full graph evidence retained.
- Restored nupkg SHA256 matches the admitted artifact. All three refreshed `--include-transitive
  --vulnerable --no-restore --format json` audits succeed with no vulnerable NuGet packages.
- Debug build succeeds, **zero warnings/errors**. Effective API Pkcs is now **342,352 bytes**, file
  version `10.0.1326.50504`, product version
  `10.0.13+4099f83977f4d29604a9aeb971fb0a7bc1d5f699`, SHA256
  **7BCE9317B8ECCD6B261B1AF3321AC4A5FF4E770A2C6C666F133F0108C5D066F4**.
  This matches the admitted package `runtimes/win/lib/net10.0` DLL exactly. Debug `.deps.json` now
  lists the runtime asset under `System.Security.Cryptography.Pkcs/10.0.13`, not the ASP.NET runtime
  pack. The surrounding runtime packs remain 10.0.12; no framework servicing upgrade was applied.
- Full existing offline backend rerun: **1,371 passed, zero failed/skipped** (Domain 358,
  Application 419, QAD/Shortages 331, architecture 10, API 253). Same source-disabled environment,
  synthetic test boundaries and commands as the first pass. No new tests/exploit/stress harness.
- Source/API-generated contract preservation, independent correction review, Release embedded
  payload verification and desktop sidecar identity checks remain required before completion.

### Embedded-payload verification method

For actual artifact proof, the evidence directory contains a read-only PowerShell/.NET-standard-library
bundle inspector (`inspect-sidecar-bundle.ps1`), not a downloaded/installed tool or executable dependency.
It reads bundle header/entry offsets, verifies bounds, hashes selected payload bytes and records the
embedded `.deps.json` Pkcs owner. It never loads/executes DLLs or reads embedded appsettings/secrets.
Format references inspected: dotnet/runtime `Microsoft.NET.HostModel/Bundle/Bundler.cs` signature and
`IsBundle`, `Manifest.Write`, `FileEntry.Write` (bundle 6.0). Compression/bounds/unique-Pkcs checks fail
closed. The initial one-megabyte prefix assumption failed; it was replaced with whole-file signature
search using standard-library code before any result was accepted.

Baseline validation against the unchanged entry executable directly recovered one Pkcs payload,
SHA256 `5D537DC5A92C4624E030BB5A453849287076E8BC9011ED77C007E3D2E089880D`, owned by ASP.NET runtime pack
10.0.12, and native SqlClient SNI SHA256
`7E37C5BBA3DA06A955C122E3515B2980E2F1B8732EC0297F0A7E77E79B4DE331`. This agrees with the separate Syft
inventory and DLL/deps evidence; final published-byte comparison remains required. Initial
10.0.13 admission capture also stopped on its absent LICENSE.TXT; exact-commit license retrieval and
independent applicability review resolved that evidence gap before restore.

### Final publication, effective assets and local startup

- `scripts/build-sidecar.ps1` succeeds with its unchanged self-contained **win-x64**, single-file,
  untrimmed/non-AOT settings and **IncludeNativeLibrariesForSelfExtract=true**. No new feed/tool or
  machine-wide SDK/runtime installation. Full publish log retained; no dependency warnings/conflicts.
- Publish-context asset comparison now matches **all original target contexts and all third-party
  package identities except Pkcs 9.0.4 → 10.0.13**. The initial normal-restore ILLink/RID context
  differences disappear in the matched publication context. No unexplained graph change remains.
  SqlClient 6.1.1, SNI.runtime 6.0.2 and framework runtime packs 10.0.12 stay unchanged.
- Direct inspection of the **actual published single-file executable** finds exactly one embedded
  Pkcs DLL: 342,352 bytes, file `10.0.1326.50504`, product
  `10.0.13+4099f83977f4d29604a9aeb971fb0a7bc1d5f699`, SHA256
  **7BCE9317B8ECCD6B261B1AF3321AC4A5FF4E770A2C6C666F133F0108C5D066F4**. It exactly matches the
  independently admitted/restored package Windows/net10.0 DLL. Embedded deps assigns that asset
  solely to **System.Security.Cryptography.Pkcs/10.0.13**, not the older framework pack. No separate
  affected Pkcs DLL is bundled. This proves the selected servicing payload without AssemblyVersion
  inference. Existing admitted **Syft 1.51.1** independently inventories package 10.0.13 in the exe.
- Embedded native `Microsoft.Data.SqlClient.SNI.dll`: **566,832 bytes**, SHA256
  `7E37C5BBA3DA06A955C122E3515B2980E2F1B8732EC0297F0A7E77E79B4DE331`, **identical to entry**.
- Final three API/QAD/Shortages NuGet audits (including transitives) succeed, no vulnerable packages.
  Both refreshed reported advisories list 10.0.13 patched; the effective Pkcs payload is outside both
  affected ranges. Other runtime/framework/ecosystem vulnerabilities are not certified absent by a
  package-only audit, and surrounding framework servicing is not broadened into this checkpoint.
- `cargo build --locked --offline` and `cargo build --release --locked --offline` succeed solely to
  refresh the existing debug/release sidecar copies using `tauri-build::copy_binaries`. Native source,
  Cargo.lock, Tauri capabilities/configuration and frontend source/build-info remain byte-preserved.
  No frontend/native test suites or installer build was needed. PowerShell renders Cargo progress
  stderr as NativeCommandError records in logs; both actual Cargo exit statuses are **0**, not failed
  builds. No locked application was killed; preflight found no KST artifact lock.

All refreshed executables share **127,425,702 bytes**, file `0.1.0.0`, product `0.1.0-alpha.21`, SHA256
**CC76D047E5A403EE7C194CC204F16606195D15C827D25B1A2CAC9AC7147F038E**:

| Copy | Identity result |
|---|---|
| `publish/backend-sidecar/Kst.Api.exe` | Verified published bundle above |
| `src/tauri/binaries/Kst.Api-x86_64-pc-windows-msvc.exe` | Same full executable hash |
| `src/tauri/target/debug/Kst.Api.exe` | Same full executable hash, Tauri debug pipeline refreshed |
| `src/tauri/target/release/Kst.Api.exe` | Same full executable hash, Tauri release pipeline refreshed |

Full MIT text and intact exact-package third-party notices are preserved beside the local publication
under `publish/backend-sidecar/third-party-notices/System.Security.Cryptography.Pkcs-10.0.13/`, and in
the admission evidence. This does not assert installer/fleet resource inclusion: no installer/shared
distribution was built, and final packaged-notice verification remains the existing release gate.

The existing safe startup probe was adapted only to write this checkpoint's evidence and explicitly
set content root to its isolated directory. It launches the **copied** sidecar with blank QAD source
settings, a confirmed nonexistent secrets path and dynamic loopback port. Only `/health`, `/ready`,
`/api/v1/reports/part-info/spec` are requested. All pass with matching handshake/process identity and
only `127.0.0.1` listeners. Probe cleanup terminates **only its own child process**. No source-system
reads, live authentication, real report generation/export, desktop interaction or installed-fleet
behavior is inferred. Copying files is not claimed to refresh any already-running application.

Independent corrected-build review and final publication adjudication
(`independent-security-review-10.0.13.md`) support admission, the graph, 1,371 tests and actual
published/copy byte evidence. The reviewer independently parsed the embedded manifest/selected bytes,
rehashes all four actual executables, compares all seven publish-context asset files and checks local
notices/probe evidence. **S22-034-ISR-001: RESOLVED for this local publication and four matching copies;
no confirmed blocking finding remains in this bounded review.** No risk accepted. Original blocked
`independent-security-review.md` remains unchanged, SHA256
`F8C7A16FBF09383F172AEA32EF8FA913062C740EC5EF03251FEED3F76EBDFED4`.

### Implementation delivery and owner gate (historical)

At implementation delivery, offline verification was complete and **owner acceptance was pending**. Final
checkpoint-entry comparison preserves all **943 original files**, except the intended central props
and two status/checklist edits; this new admission/checkpoint record is the only new repository file.
The S22-034 row alone changes in the local Windows backlog. Focused entry-relative diffs, final
verification summary, full logs/advisory snapshots/TRX and independent reviews remain on the laptop.
No source/contract/frontend/native/lockfile change was introduced; OpenAPI and generated TypeScript
remain at the entry bytes including the separate pending Part Info work. No reset/stash/branch switch,
staging, commit/push, shared deployment, operational import/submission or live source access occurred.

Suggested owner-run follow-up at delivery (not executed/accepted by the agent):
1. Restart the intended desktop review build so the refreshed sidecar is actually loaded; copying
   files alone does not update an already-running process.
2. When owner-authorized live access is available, confirm existing Windows-integrated QAD
   authentication and separately configured Shortages access. Do not change transport/credential
   settings or source data to manufacture a passing check.
3. Spot-check accepted MPS navigation/export/eight-second feedback and an existing workbook/native
   save/open workflow. Their source/tests are preserved; offline backend checks do not prove native
   interaction, live parity or viewer behavior. Part Info acceptance remains separate.
4. Record explicit S22-034 acceptance or a concrete regression. Installer/fleet notice inclusion and
   general release readiness remain their existing later gates, not new work authorized here.

## Owner acceptance and closure — 2026-10-16

The owner reports that **everything is working fine**, explicitly **accepts S22-034**, and authorizes
the focused remediation/closeout commit. **S22-034 is COMPLETE / OWNER-ACCEPTED / CLOSED.** This records
owner-reported desktop acceptance, not agent-observed desktop testing or a newly supplied detailed
authentication/live-query validation record. No particular test identities, queries, authentication
matrix or coverage of each suggested follow-up above is inferred from the owner's feedback.

The accepted effective version remains **System.Security.Cryptography.Pkcs 10.0.13**. The initial
ineffective 9.0.21 attempt, affected framework 10.0.12 selection and initial blocked review remain
historical evidence. The corrected independent finding **S22-034-ISR-001 remains RESOLVED** for the
reviewed local publication and all four matching copies, with no confirmed bounded blocking finding.
This is remediation, not risk acceptance.

Closeout checks confirm the reviewed implementation/source and four published/copied executable
identities are unchanged. Existing 1,371 passing offline tests, audits, payload inspection, safe startup
and independent review are reused; no full suites, security review, build, install or live query is
repeated for this documentation-only closeout. Focused staged diff/path and whitespace review are
required before the authorized commit. The index was empty at closeout entry; shared status/checklist
changes are isolated at hunk level so unrelated pending work is not included.

The focused scope is the existing central package pin, this self-contained admission/closeout record,
and S22-034-only current-status/checklist hunks. The local Windows backlog entry is closed outside
Git. Large evidence, caches, generated output, published binaries and local reviews remain at
`C:\Dev\kst_v2_tmp\Stage22\S22-034\`, outside the commit. No push is authorized.

**Retained gates/limits:** installer/fleet notice inclusion remains a later release gate; inherited
formatting/lint exceptions remain exceptions. No shared deployment, database/QXtend action, detailed
live authentication/query proof, general framework security clearance or Stage 22 closure is claimed.
Pending Part Info, navigation, export and eight-second save-status work remains separately preserved.

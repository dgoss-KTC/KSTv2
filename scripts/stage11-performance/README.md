# Stage 11 performance investigation

## Production bounded pass (2026-09-29)

Current validation scopes: Shure SMT/2140, Taco/3230, MSA/Neutronics/1391. The 12 original
2140/2141/2142 captures remain the fixed offline equivalence set; no original capture is overwritten.
Production now consumes its typed compact DTO directly and uses dense `BuildBoth`.
`IndexedProjectionReference.cs` retains the pre-install indexed algorithm for paired checks; it is
never part of the runtime application. The older architecture modes now use that explicit reference
when labeling a variant indexed. The original compact/virtual browser entry requires its matching
pre-production frontend source (retained checkpoint); it is historical diagnostic code, not a valid
way to benchmark the new typed screen. Use `*-production` replay for current implementation.

```powershell
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- production-contract
node scripts/stage11-performance/verify-production.mjs
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- production-contract-workbooks
# Only after recording the contract gates and installing the dense implementation:
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- production-dense
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- algorithm-regressions
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- production-dense-workbooks
node scripts/stage11-performance/frontend-profile.mjs 2140-production
node scripts/stage11-performance/production-summary.mjs
```

Production workbook checks run all 24 comparisons afresh against retained successful hashes;
they do not resume prototype successes as if those proved the installed build. `runtime-profile.ps1`
now measures the three current scopes, compact cold/cache-hit and cached projection detail separately,
and retains timestamped compact/control response pairs for browser replay. Run live measurement alone,
after builds/tests/workbook work finish. It cannot currently isolate Stage 11 acquisition subphases.
Raw captures remain ignored, local-only. See the investigation's two production-pass sections for
exact results, operation-order review and owner/DBA acceptance boundaries.

## Architecture/prototype pass (2026-09-29)

Primary owner-validation/stress workspace: Shure SMT / 2140 (38 parents). Mid-sized benchmark:
SHU Molding / 2142 (8 parents). Small regression: SHU Metals / 2141 (3 parents).

Offline modes added: `split-prototype`, `contract-prototype`, `algorithm-prototypes`,
`algorithm-regressions`, `algorithm-workbooks`. They enumerate all retained `*-inputs.json`
captures, compare complete dual-mode JSON, and write timestamped `architecture-*.jsonl` records.
`algorithm-workbooks` resumes previously recorded successful workspace/variant/mode checks;
only resume within the same unchanged prototype/export implementation. Start a separate capture
directory/evidence set if either implementation changes. The original captures are never replaced.

```powershell
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- split-prototype
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- algorithm-prototypes
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- algorithm-regressions
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- algorithm-workbooks
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- contract-prototype
node scripts/stage11-performance/verify-compact.mjs
node scripts/stage11-performance/architecture-summary.mjs
node scripts/stage11-performance/frontend-profile.mjs 2140-control
node scripts/stage11-performance/frontend-profile.mjs 2140-compact
node scripts/stage11-performance/frontend-profile.mjs 2140-compact virtual
```

Use 2141/2142 for smaller browser scopes. Browser comparisons now use a bounded viewport-height
panel for both control and candidate. Keep one browser run active at a time. The virtual mode uses
an asserted build-time substitution in the diagnostic bundle, not an edit to production code.
It has a demonstrated offscreen-focus failure and is not suitable for shipment. Compact adapter
casts are diagnostic compatibility only; it fetches selected detail from an in-memory captured
control response before opening the existing modal. This is not production cache/endpoint proof.
The C# compact DTO is immutable; prototype browser hydration is not the proposed production design.

No new production contract or SQL implementation is installed by these modes. Read the follow-up
section of `docs/implementation/KST_v2_STAGE_11_PERFORMANCE_INVESTIGATION.md` and the DBA request
before selecting an implementation. Live runtime logs now have unique filenames to preserve evidence.

Opt-in local diagnostics, using existing project references only. Production code does not load
this executable. Run from the repository root in PowerShell:

```powershell
$env:KST_PERF_QAD_SERVER = '<configured QAD reporting server>'
dotnet run --project scripts/stage11-performance/Stage11Performance.csproj -c Release -- discover
```

Other explicit modes: `capture`, `capture-after`, `sql`, `sql-after`, `metadata`, `plans`,
`batch`, `compare`, `replay`, `compare-projection`, `normalize`, `api`, `api-screen`, and `workbook`.
`capture`/`capture-after` resolve the configured SW product-line workspaces 2140/2141/2142.
Review those scopes before running on a different workstation. `replay`, `api`, `api-screen`,
`compare-projection`, `normalize`, and `workbook` are offline. `compare-projection` requires the retained
pre-change `bin/api/Kst.Domain.dll`; it checks that assembly against the captured output before
using its timing. It cannot be reproduced from a clean checkout without first building the baseline.

`capture` writes original inputs and output; do not repeat it over evidence you intend to preserve.
The API mapper is linked from production source for exact DTO measurements. Production API hosting
is not started. `api-screen` writes separate reduced payloads. For browser measurements:

```powershell
node scripts/stage11-performance/frontend-profile.mjs 2140
node scripts/stage11-performance/frontend-profile.mjs 2140-screen
node scripts/stage11-performance/summarize.mjs
.\scripts\stage11-performance\runtime-profile.ps1
```

The browser runner uses the existing esbuild dependency, installed Edge, production React profiling
build, and a loopback-only ephemeral server. It serves local captured payloads, stubs drawer failures,
and writes timings locally. It is not an end-to-end live Tauri or successful drawer measurement.
Use one browser profile at a time. Do not rebuild the diagnostic executable while it is running:
Windows locks the executable/assemblies. Stop/wait for it, or use a distinct `OutputPath`.

`runtime-profile.ps1` launches the already-built published sidecar on loopback port 0, reads existing
workspace configuration, preloads MPS, measures cold/cache-hit screen requests and selected purchasing,
then stops only its owned process. It uses live read-only application paths and does not change local
workspace configuration. Response bodies stay local; output contains timing/size/status only.

Important evidence correction: the first parameterized SHOWPLAN attempt returned no result sets,
and its `available=true` output was invalid. Native text compilation exposed SQL error 262 (permission
denied). Empty `.sqlplan` files are not execution-plan evidence. No permissions were changed.

SQL command timeouts do not reliably bound total streaming wall time. Each run also has cancellation
and an external foreground/background hard timeout. The 500-component interleaved run was stopped
at the external 600-second deadline; no diagnostic descendants remained afterward.

Reads use the production QAD connection factory (`READ UNCOMMITTED`, no explicit transaction).
No cache flush, index/schema change, or server configuration change is authorized. A cold load
means an application cache miss, not a cold SQL Server buffer cache. Captures belong only in the
gitignored `captures/` directory; do not commit or print raw source data or connection settings.

Investigation sequence: capture fixed parent scopes and source inputs; measure production reader,
individual production SQL queries, normalization, dual projection, DTO mapping/serialization,
payload and UI; rank measured costs; benchmark one bounded change at a time against the same
inputs; compare all projection fields and workbook cell values. Broader algorithm and acquisition
designs require a separate evidence-backed design review. The current algorithm authority is
`docs/implementation/stage11_component_mrp_algorithm.md`; all its rules remain in force.

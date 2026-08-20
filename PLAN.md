# Stage 8D.3 Plan — BOM Application Service / API Composition

> **STATUS: APPROVED WITH AMENDMENTS — implementing.** Owner approved the plan with three
> amendments (below); all other plan requirements remain in force.
>
> **Approved amendments:**
> 1. **Site is explicit cache compatibility** — `BomCacheEntry` explicitly contains `Site`.
>    Both the fresh-hit and stale-last-good compatibility checks require `cached.Site` to match
>    the current workspace Site (robust OrdinalIgnoreCase comparison) in addition to the
>    effective-date match; a cached BOM from another Site is NEVER returned as fresh or stale.
>    The physical cache key remains `(WorkspaceId, ParentPart)`, mirroring the PartDetail cache.
> 2. **Inventory result completeness** — the accepted reader contract (exactly one summary per
>    requested distinct part) is validated in the Application after the single batch read: a
>    requested part missing from the returned summaries, or a duplicate returned summary, is a
>    load failure (same-site/same-effective-date stale-last-good or Unavailable) — never an
>    inferred zero, never a cached partial composition.
> 3. **API test-host reader overrides** — `KstApiFactory` gains optional deterministic
>    overrides for `IBomSourceReader` / `IPartInventoryReader` using the existing
>    descriptor-removal / replacement pattern (properties, because xunit class fixtures require
>    exactly one public constructor). Endpoint success/stale/503 tests seed the existing
>    singleton `IMpsSnapshotStore` at runtime after workspace creation; no live QAD is required.
>
> Stages 8D.1 (`bf89c60`) and 8D.2 (`624e353`) are complete, validated, accepted, committed, and
> pushed. Backend baseline: **524/524 tests passing** (Domain 118, Qad 144, Application 167,
> Architecture 9, Api.Integration 86).
>
> Scope: the smallest safe Application + API implementation that composes the accepted
> 8D.2 structural `BomOccurrence` stream with the accepted 8D.1 shared `Site + Part`
> inventory into scheduler-visible BOM lines, enforces workspace/MPS scope + effective-date
> freshness, and exposes the authoritative BOM contract. No BOM frontend (8D.4), no
> requirement math, no 8D.1/8D.2 redesign.

---

## A. Repository Fit

All pieces below verified in the current tree (clean, `main`).

| Concern | Existing pattern (exact reference) | Reused as-is |
|---|---|---|
| Application service | `Kst.Application/PartDetail/PartDetailService.cs` — lazy-loaded parent-scoped orchestrator: workspace lookup, MPS state read (never triggers MPS load), scope validation, cache, clock, reader, logger. Stage 7's `WorkOrderDrilldownService` is the sibling. | pattern only; new `BomService` |
| Workspace/MPS parent scope | `IWorkspaceConfigurationService.GetWorkspacesAsync()` → first-match on `AssignmentId`, else `PartDetailWorkspaceNotFoundException` → 404. `IMpsSnapshotStore.GetState(ws)` → `Snapshot is null` → 409 `MpsNotLoaded`. Scope = `Snapshot.ResolvedParts` case-insensitive `ParentPart` match, else 404 `"Part not in workspace scope"` (title+detail identical in Stage 6 and Stage 7). | `IWorkspaceConfigurationService`, `IMpsSnapshotStore`, `MpsSnapshot.ResolvedParts` |
| Freshness identity | `MpsSnapshot.Id` (`SnapshotId`, new GUID per successful load). `InMemoryMpsSnapshotStore.SetFailed` **retains** the prior good snapshot + id (failed refresh never advances freshness). | unchanged |
| Clock | `Kst.Domain.Common.IClock` / `SystemClock`; convention `DateOnly.FromDateTime(_clock.LocalNow.Date)` (`PartDetailService`), `LoadedAtUtc = _clock.UtcNow`. | unchanged |
| Reader/delegate bridge | `IPartDetailSourceReader`/`DelegatePartDetailSourceReader` (`Kst.Application/PartDetail/`), `IMpsSourceReader`/`DelegateMpsSourceReader`, `IWorkOrder*Reader`/`DelegateWorkOrder*Reader` — Application interface + `Func` delegate; concrete QAD reader in `Kst.Integrations.Qad`; wired in `Program.cs` with configured/unconfigured branches. Guarded by `Kst.ArchitectureTests` (Application must not reference SqlClient/Dapper/AspNetCore). | pattern only; two new bridges |
| Cache | `InMemoryPartDetailCacheStore` — key `(WorkspaceId, ParentPart)` (case-insensitive key), entry tagged `LoadedAgainstMpsSnapshotId`, stale-last-good fallback on load failure. (Stage 7's stores key by snapshot id and never stale-fallback — that is the investigation-data model, not the right model here.) | pattern only; new `InMemoryBomCacheStore` |
| Endpoint/Problem Details | `PartDetailEndpoints.cs` — route style, `Results.Problem` stable titles, `catch ...WorkspaceNotFoundException → Results.NotFound()`, `Results.ValidationProblem` for blank input. | pattern only; new `BomEndpoints` |
| OpenAPI | `Kst.Api.csproj` `OpenApiGenerateDocumentsOnBuild` → `docs/openapi/Kst.Api.json`; `npm run generate:types` (openapi-typescript) → `src/frontend/src/generated/api.ts`; never hand-edited; committed together. | unchanged |
| Upstream capabilities | `QadBomReader.ReadAsync(site, parent, effectiveDate, ct)` → `IReadOnlyList<BomOccurrence>`; `QadPartInventoryReader.ReadSummariesAsync(site, partNumbers, ct)` → one summary per distinct requested part (zeroes when no rows; internal batching via `MpsPartBatcher` + `MaxPartBatchSize`). | unchanged — the bridges delegate straight to these methods |

No `Bom`/`Inventory` namespace exists yet in `Kst.Application`/`Kst.Api` (checked — no name
collisions; only doc-comment mentions in `WorkOrderDrilldownService`).

## B. Proposed Application Model

New feature folder `Kst.Application/Bom/` (mirrors `PartDetail` placement; composed
records with cache/freshness metadata live in Application, per the accepted `PartDetail`
precedent). Domain `BomOccurrence` and `PartInventorySummary` remain structurally separate
and are **not** modified.

```csharp
// Kst.Application/Bom/BomLine.cs — one scheduler-visible BOM presentation line.
// Grain: structural occurrence + composed Site+Part inventory. Deliberately absent:
// RMA, Extended Requirement, Incoming Supply, Coverage, Material Status, Short Qty,
// Projected QOH, PO/MRP quantities — no requirement math in 8D.3.
public sealed record BomLine(
    string OccurrenceKey,        // opaque expanded-occurrence identity (from BomOccurrence)
    int Level,                   // actual structural level (gaps preserved; never renumbered)
    string ComponentPart,
    string? PmCode,              // effective P/M code (always P or M after visibility filter)
    bool IsPhantom,
    string? Description,
    decimal? QuantityPer,        // relationship-level, verbatim
    decimal? ScrapPercentage,    // relationship-level, verbatim
    decimal NetQuantityOnHand,   // composed from PartInventorySummary (0 = authoritative zero)
    decimal NonNetQuantityOnHand);

// Kst.Application/Bom/Bom.cs — complete successful composition + cache/freshness metadata.
public sealed record Bom(
    string Site,
    string ParentPart,
    DateOnly EffectiveDate,      // the effective date actually used (reported in the API)
    IReadOnlyList<BomLine> Lines,// empty list is a valid successful result
    DateTimeOffset LoadedAtUtc,
    bool IsStale,
    string? Warning);

// Kst.Application/Bom/BomResult.cs — outcome wrapper (mirrors PartDetailResult).
public enum BomOutcomeKind { Loaded, MpsNotLoaded, OutOfScope, Unavailable }
public sealed record BomResult(BomOutcomeKind Kind, Bom? Bom = null)
{
    public static BomResult Loaded(Bom bom) => new(BomOutcomeKind.Loaded, bom);
    public static BomResult MpsNotLoaded { get; } = new(BomOutcomeKind.MpsNotLoaded);
    public static BomResult OutOfScope { get; } = new(BomOutcomeKind.OutOfScope);
    public static BomResult Unavailable { get; } = new(BomOutcomeKind.Unavailable);
}
```

Deliberately **no** `MissingParent` outcome (unlike PartDetail): a valid in-scope parent with
no effective structural rows — or with no P/M-visible rows — is `Loaded` with `Lines = []`
(200). MPS scope already answers the API's parent question; no `pt_mstr` existence query is
added (this also resolves the 8D.2 "unknown-parent deferred to 8D.3" item).

Service inputs/outputs: `BomService.GetBomAsync(Guid workspaceId, string parentPart,
CancellationToken) → Task<BomResult>`. The frontend supplies only workspace identity +
parent part — no site, domain, effective date, or P/M rules.

## C. Reader Bridges and DI

Deferred from the accepted 8D.1/8D.2 plans; added now at first real Application consumer.
Names follow the repository's existing `*SourceReader` / `Delegate*SourceReader` pattern and
the 8D.1 deferral note (`IPartInventoryReader`/`DelegatePartInventoryReader`).

```csharp
// Kst.Application/Bom/IBomSourceReader.cs  (+ DelegateBomSourceReader, Func-backed,
// exactly mirroring DelegatePartDetailSourceReader's shape)
public interface IBomSourceReader
{
    Task<IReadOnlyList<BomOccurrence>> ReadAsync(
        string site, string parentPart, DateOnly effectiveDate,
        CancellationToken cancellationToken = default);
}

// Kst.Application/Inventory/IPartInventoryReader.cs  (+ DelegatePartInventoryReader).
// Feature folder mirrors Kst.Domain.Inventory / Kst.Integrations.Qad.Inventory; the shared
// capability will also be consumed by 8D.5 Component Info, so it is not placed under Bom.
public interface IPartInventoryReader
{
    Task<IReadOnlyList<PartInventorySummary>> ReadSummariesAsync(
        string site, IReadOnlyList<string> partNumbers,
        CancellationToken cancellationToken = default);
}
```

`Program.cs` — new `// -- BOM (Stage 8D.3) --` section after the Work Orders section,
following the exact existing shape (singletons; `sp.GetRequiredService` delegates;
`throw new InvalidOperationException("QAD connection is not configured.")` in the
unconfigured branch):

```csharp
builder.Services.AddSingleton<IBomCacheStore, InMemoryBomCacheStore>();

if (qadOptions.IsConfigured)
{
    builder.Services.AddSingleton<QadBomReader>();
    builder.Services.AddSingleton<QadPartInventoryReader>();
    builder.Services.AddSingleton<IBomSourceReader>(sp => new DelegateBomSourceReader(
        (site, parentPart, effectiveDate, ct) =>
            sp.GetRequiredService<QadBomReader>().ReadAsync(site, parentPart, effectiveDate, ct)));
    builder.Services.AddSingleton<IPartInventoryReader>(sp => new DelegatePartInventoryReader(
        (site, partNumbers, ct) =>
            sp.GetRequiredService<QadPartInventoryReader>().ReadSummariesAsync(site, partNumbers, ct)));
}
else
{
    const string notConfiguredMessage = "QAD connection is not configured.";
    builder.Services.AddSingleton<IBomSourceReader>(_ => new DelegateBomSourceReader(
        (_, _, _, _) => throw new InvalidOperationException(notConfiguredMessage)));
    builder.Services.AddSingleton<IPartInventoryReader>(_ => new DelegatePartInventoryReader(
        (_, _, _) => throw new InvalidOperationException(notConfiguredMessage)));
}

builder.Services.AddSingleton<BomService>();
```

plus `app.MapBomEndpoints();` after `app.MapWorkOrderEndpoints();`. No new project
references (Application already references Domain; Architecture tests stay green by
construction — delegates use only `Func`/Domain types).

## D. Application Service Flow

`BomService` ctor: `(IWorkspaceConfigurationService, IMpsSnapshotStore, IBomSourceReader,
IPartInventoryReader, IBomCacheStore, IClock, ILogger<BomService>)` — mirrors
`PartDetailService` exactly.

```
GetBomAsync(workspaceId, parentPart, ct)
 1. ct.ThrowIfCancellationRequested()
 2. workspace = GetWorkspacesAsync() → FirstOrDefault(AssignmentId == workspaceId)
        ?? throw BomWorkspaceNotFoundException            → 404 (Results.NotFound())
 3. mpsState = _mpsSnapshotStore.GetState(workspaceId)     (never triggers an MPS load)
        Snapshot is null → BomResult.MpsNotLoaded          → 409
 4. normalizedParent = parentPart.Trim()
        in scope = Snapshot.ResolvedParts.Any(p => string.Equals(p.ParentPart,
                     normalizedParent, OrdinalIgnoreCase))
        else → BomResult.OutOfScope                        → 404 "Part not in workspace scope"
 5. effectiveDate = DateOnly.FromDateTime(_clock.LocalNow.Date)
 6. currentSnapshotId = Snapshot.Id
        cached = _cache.Get(workspaceId, normalizedParent)
        fresh hit iff cached != null
                   && cached.LoadedAgainstMpsSnapshotId == currentSnapshotId
                   && cached.EffectiveDate == effectiveDate
        → BomResult.Loaded(cached.Bom)
 7. (miss/stale) occurrences = _bomSourceReader.ReadAsync(workspace.Site, normalizedParent,
                    effectiveDate, ct)
        on exception: log; same-date stale fallback (step 10a) or BomResult.Unavailable
 8. visible = occurrences.Where(BomSchedulerVisibility.IsSchedulerVisible).ToList()
        — order-preserving filter of the flat structural list; hidden intermediates are
          simply omitted; their descendants already carry their own P/M codes and remain
          independently eligible; Level values are untouched (gaps preserved); no
          re-sort, no consolidation, no phantom flattening.
 9. if visible.Count > 0:
        keys = visible.Select(v => v.ComponentPart.Trim()).Distinct(OrdinalIgnoreCase)
        summaries = _inventoryReader.ReadSummariesAsync(workspace.Site, keys, ct)
            — ONE batch-capable call (reader chunks internally); empty visible skips it.
            on exception: log; same-date stale fallback or Unavailable (a partial
            structural-only result is NEVER cached or returned with invented zeros).
        byPart = Dictionary<string, PartInventorySummary>(OrdinalIgnoreCase)
            keyed by summary.PartNumber (reader echoes normalized keys; exactly one summary
            per distinct requested key, zeroes when no qualifying inventory).
        composition maps BY PART NUMBER — repeated occurrences of the same component repeat
        the same Site+Part values (same display, one shared pool; NOT independent pools).
 10. bom = new Bom(workspace.Site, normalizedParent, effectiveDate, lines,
                   _clock.UtcNow, IsStale: false, Warning: null)
 10a stale fallback (both failure paths): cached != null && cached.EffectiveDate == effectiveDate
        → Loaded(cached.Bom with { IsStale = true,
             Warning = "Showing the last known BOM information. A newer refresh could not be completed." })
        — a different-date cached Bom is NEVER served (see E).
 11. _cache.Set(workspaceId, normalizedParent,
               new BomCacheEntry(workspaceId, normalizedParent, effectiveDate,
                                 currentSnapshotId, bom))   — only after BOTH reads succeed
     return BomResult.Loaded(bom)
```

`BomSchedulerVisibility` (new `Kst.Application/Bom/BomSchedulerVisibility.cs` — small pure
static so the P/M comparison is unit-testable in isolation):

```csharp
public static class BomSchedulerVisibility
{
    // Robust: trim + case-insensitive; null and every non-P/M code (N, S, 2, 3, 4, C, D, ...)
    // are not visible. Consistent with the repo's trim/OrdinalIgnoreCase comparison convention.
    public static bool IsSchedulerVisible(string? pmCode)
    {
        var code = pmCode?.Trim();
        return code is not null
            && (code.Equals("P", StringComparison.OrdinalIgnoreCase)
                || code.Equals("M", StringComparison.OrdinalIgnoreCase));
    }
}
```

## E. Cache / Freshness Design

New `Kst.Application/Bom/IBomCacheStore.cs` + `BomCacheEntry.cs`:

```csharp
public sealed record BomCacheEntry(
    Guid WorkspaceId,
    string ParentPart,
    DateOnly EffectiveDate,                 // business-identity part (cross-date gate)
    SnapshotId LoadedAgainstMpsSnapshotId,  // freshness tag (MPS successful-refresh generation)
    Bom Bom);                               // complete successful composition only

public interface IBomCacheStore
{
    BomCacheEntry? Get(Guid workspaceId, string parentPart);
    void Set(Guid workspaceId, string parentPart, BomCacheEntry entry);
}
```

`Kst.Infrastructure/Bom/InMemoryBomCacheStore.cs` — thread-safe `ConcurrentDictionary`,
in-memory only, key `(WorkspaceId, ParentPart)` with `Trim().ToUpperInvariant()` — a verbatim
structural mirror of `InMemoryPartDetailCacheStore` (no parallel infrastructure).

Mapping of the accepted Stage 8 rules onto the actual repository objects:

| Accepted rule | Mechanism |
|---|---|
| Business identity Site + Parent + EffectiveDate | Site is fixed by the resolved workspace (same as PartDetail); key `(WorkspaceId, ParentPart)`; `EffectiveDate` stored on the entry and checked in **both** the fresh-hit and stale-eligible tests |
| Freshness = successful-refresh generation | `MpsSnapshot.Id` (`SnapshotId` — `MpsWorkspaceSnapshotService.LoadAsync` calls `SnapshotId.New()` on every successful load) — the repository's existing refresh identity; no second refresh system |
| Fresh hit | entry.SnapshotId == current && entry.EffectiveDate == today |
| Successful MPS refresh | new `SnapshotId` → fresh check fails → a new load is attempted; the prior entry is retained until a successful load replaces it |
| Failed MPS refresh | `InMemoryMpsSnapshotStore.SetFailed` retains the prior snapshot **and its id** → entry stays a fresh hit; compatible last-good data is never spuriously invalidated |
| Same-date stale-last-good | on structural **or** inventory read failure: entry with `EffectiveDate == today` (any snapshot id) is served with `IsStale = true` + warning — same signaling convention as Stage 6 PartDetail |
| **Cross-date fallback forbidden** | any entry with `EffectiveDate != today` is usable for neither a fresh hit nor a stale fallback → load is attempted with today's date; if it fails → `Unavailable` (503), never yesterday's BOM |
| Failed partial loads | `_cache.Set` runs only after structural + inventory reads both succeed; a failed reload never overwrites the last-good complete entry |
| Non-invalidating UI state | MPS bucket, Due/Release mode, horizon, tab, and search string appear nowhere in the service signature, the cache key, or the entry — they are structurally unable to affect BOM identity/freshness |

Inherited accepted trait (no action, same as Stage 6): the cache is workspace-scoped by
`AssignmentId`; a workspace-site edit does not by itself invalidate lazy entries — the next
successful MPS load advances the snapshot id, which re-qualifies the data.

## F. API Contract

**Route** (existing `{assignmentId:guid}` workspace convention; parent-contextual path per the
accepted semantic route; no bucket/due-release/horizon/week encoded):

```
GET /api/v1/workspaces/{assignmentId:guid}/parts/{parentPart}/bom
```

New `Kst.Api/Endpoints/BomEndpoints.cs` (thin handler + `ToResult` switch, mirroring
`PartDetailEndpoints`): `WithName("GetBom")`, `WithTags("Bom")`,
`Produces<BomResponseDto>(200)` + `ProducesProblem` 400/404/409/503.

New `Kst.Api/Dtos/BomDtos.cs` (feature-specific DTO file convention; camelCase JSON via the
existing `ConfigureHttpJsonOptions`):

```csharp
public sealed record BomLineDto(
    string OccurrenceKey,
    int Level,
    string ComponentPart,
    string? PmCode,
    bool IsPhantom,
    string? Description,
    decimal? QuantityPer,
    decimal? ScrapPercentage,
    decimal NetQuantityOnHand,
    decimal NonNetQuantityOnHand);          // RMA deliberately absent

public sealed record BomResponseDto(
    string Site,                            // existing convention exposes workspace-scope
    string ParentPart,                      // metadata (PartDetailResponseDto.Site,
    DateOnly EffectiveDate,                 // MpsSnapshotMetadataDto.Site)
    IReadOnlyList<BomLineDto> Lines,
    DateTimeOffset LoadedAtUtc,
    bool IsStale,
    string? Warning);                       // existing stale-signaling convention
```

Not exposed: QAD Domain, `oid_ps_mstr`, MPS snapshot id, cache keys, RMA QOH, or any
requirement math. The caller sends only `{assignmentId}` + `{parentPart}`.

**Result mapping** (stable titles reused verbatim where already load-bearing):

| Service outcome / exception | HTTP | Problem Details |
|---|---|---|
| `Loaded` (fresh) | 200 | `BomResponseDto`, `isStale: false`, `warning: null` |
| `Loaded` (stale-last-good) | 200 | `isStale: true` + warning |
| `MpsNotLoaded` | 409 | title `"MPS data not loaded"` (exact existing title); detail `"This workspace's MPS data has not been loaded yet. Load the MPS dashboard before viewing the BOM."` |
| `OutOfScope` | 404 | title `"Part not in workspace scope"` (exact existing title); detail `"The requested part is not in this workspace's current MPS parent scope."` (exact existing detail) |
| `Unavailable` | 503 | title `"BOM information unavailable"` (mirrors `"Part information unavailable"` / `"Work order information unavailable"`); shared detail `"Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT."` |
| `BomWorkspaceNotFoundException` | 404 | `Results.NotFound()` (existing unknown-workspace semantics) |
| blank `parentPart` path value | 400 | `Results.ValidationProblem(["parentPart"] = ["parentPart is required."])` |

Valid parent, no structural rows → **200, `lines: []`**. Valid BOM, no P/M-visible rows →
**200, `lines: []`**. A QAD failure is never converted to an empty BOM.

**Contract workflow** (repository-standard, no hand-written TS):
1. `cd src/backend && dotnet build Kst.slnx` — spec auto-regenerates to `docs/openapi/Kst.Api.json` (new path + `BomResponseDto`/`BomLineDto` schemas).
2. `cd src/frontend && npm run generate:types` — regenerates `src/frontend/src/generated/api.ts` (additive only).
3. `npm run typecheck` — must pass with zero hand-written frontend changes.
4. Commit `Kst.Api.json` + `api.ts` together at the implementation checkpoint.

## G. Exact Implementation File Plan

**Add — Application (10):**
1. `src/backend/Kst.Application/Bom/BomLine.cs`
2. `src/backend/Kst.Application/Bom/Bom.cs`
3. `src/backend/Kst.Application/Bom/BomResult.cs` (enum + record)
4. `src/backend/Kst.Application/Bom/BomSchedulerVisibility.cs`
5. `src/backend/Kst.Application/Bom/IBomSourceReader.cs`
6. `src/backend/Kst.Application/Bom/DelegateBomSourceReader.cs`
7. `src/backend/Kst.Application/Bom/IBomCacheStore.cs`
8. `src/backend/Kst.Application/Bom/BomCacheEntry.cs`
9. `src/backend/Kst.Application/Bom/BomService.cs`
10. `src/backend/Kst.Application/Bom/BomWorkspaceNotFoundException.cs`

**Add — Application, shared inventory bridge (2):**
11. `src/backend/Kst.Application/Inventory/IPartInventoryReader.cs`
12. `src/backend/Kst.Application/Inventory/DelegatePartInventoryReader.cs`

**Add — Infrastructure (1):**
13. `src/backend/Kst.Infrastructure/Bom/InMemoryBomCacheStore.cs`

**Add — API (2):**
14. `src/backend/Kst.Api/Dtos/BomDtos.cs`
15. `src/backend/Kst.Api/Endpoints/BomEndpoints.cs`

**Add — Tests (2):**
16. `src/backend/tests/Kst.Application.Tests/Bom/BomServiceTests.cs`
17. `src/backend/tests/Kst.Api.IntegrationTests/BomEndpointTests.cs`

**Modify (1):**
18. `src/backend/Kst.Api/Program.cs` — DI section + `MapBomEndpoints()` + usings (additions only)

**Regenerated by the contract workflow (2):**
19. `docs/openapi/Kst.Api.json`
20. `src/frontend/src/generated/api.ts`

**Deliberately untouched:** `QadBomReader`, `QadPartInventoryReader`, `BomOccurrence`,
`PartInventorySummary`, `QadPartDetailReader`, all existing caches/services/endpoints,
hand-written frontend code, all documentation (documentation reconciliation is a later
checkpoint).

## H. Verification Plan

**H.1 — Application tests** (`BomServiceTests`, reusing `FakeWorkspaceConfigurationService`
(`Kst.Application.Tests.Mps`), `FakeClock` (`Kst.Application.Tests.PartDetail`),
`InMemoryMpsSnapshotStore`, `InMemoryBomCacheStore`, and `Delegate*Reader` fakes — no new
infrastructure). Maps 1:1 to prompt §18:

- *Composition:* (1) full structural input filtered to P/M only; (2) robust P/M comparison
  (`"p"`, `" M "`, `null`, `"N"`, `"S"`, `"2"` …); (3) hidden intermediate omitted, visible
  descendant keeps actual Level (1 + 3, gap preserved); (4) structural order preserved after
  filter/composition; (5) repeated component occurrences remain repeated; (6) repeated
  components receive identical Net/Non-Net values; (7) inventory requested exactly once with
  the distinct visible part keys (fake captures call count + key set, incl. dedup); (8)
  association by PartNumber (fake returns summaries in shuffled order); (9) Net QOH maps;
  (10) Non-Net QOH maps; (11) RMA values in summaries never reach `BomLine`; (12) no
  P/M-visible rows → `Loaded`, `Lines == []`, inventory reader never called; (13) structural
  reader returns empty → `Loaded`, `Lines == []`; (14) structural reader throws → truthful
  (`Unavailable` with no cache; same-date stale with cache — never an empty BOM); (15)
  inventory reader throws → truthful (same stale/Unavailable semantics; no zero-invention);
  (16) zero summaries → numeric 0/0 on the line.
- *Scope:* (17) unknown workspace → `BomWorkspaceNotFoundException`; (18) parent not in
  `ResolvedParts` → `OutOfScope`; (19) in-scope parent proceeds to load; (20) scope matching
  reuses the case-insensitive `ResolvedParts` convention (`"abc100"` matches `"ABC100"`).
- *Effective date:* (21) date from injected `FakeClock`; (22) exact date passed to the
  structural reader; (23) `Bom.EffectiveDate` reported in the result.
- *Cache/freshness:* (24) same identity + same generation → 1 reader call for 3 requests;
  (25) new `MpsSnapshot` (successful refresh) → fresh load attempt; (26) `SetFailed`
  (failed refresh, id retained) → still a fresh hit, no spurious reload; (27) same-date
  stale-last-good on load failure (`IsStale` + warning, old payload intact); (28) different
  effective date (clock advanced) + load failure → `Unavailable`, yesterday's entry never
  served (fresh or stale); (29) failed reload never overwrites last-good (store entry
  unchanged after a failed attempt); (30) service signature/key/entry contain no
  bucket/date-basis/horizon/tab/search inputs (structurally absent; documented by the test
  fixture).

**H.2 — API integration tests** (`BomEndpointTests`, `KstApiFactory`; QAD never configured in
tests — same reachable-path pattern as `PartDetailEndpointTests`):
- (33) unknown workspace → 404;
- (34-ish) MPS not loaded → 409 with title `"MPS data not loaded"`;
- blank `parentPart` → 400;
- (503 mapping and 200 shape are not reachable without a live QAD — identical to the Stage 6
  precedent; covered by H.1 service tests + H.5 live verification).

**H.3 — Contract checks:** after `dotnet build`, confirm `docs/openapi/Kst.Api.json` contains
the new path `/api/v1/workspaces/{assignmentId}/parts/{parentPart}/bom` (operation `GetBom`)
and both new schemas; `npm run generate:types` → `api.ts` diff is **additive only** (no
handwritten edits); `npm run typecheck` clean. (No repo precedent exists for spec-content
unit tests — verified by inspection; the documented build-regenerate-typecheck workflow is
the established check, so no parallel test infrastructure is added.)

**H.4 — Full regression** (repository-documented commands):
```powershell
cd src/backend
dotnet restore Kst.slnx
dotnet format Kst.slnx --verify-no-changes
dotnet build Kst.slnx --nologo
dotnet test Kst.slnx --nologo        # baseline 524 + new ≈ 45–55
cd ../frontend
npm run typecheck ; npm run lint ; npm test ; npm run build   # additive types only; no UI changes
```
No Tauri/sidecar rebuild is required for this checkpoint's own verification (no UI work in
8D.3; live checks below go through the API directly). Note for the owner: any later full-app
session still follows `.\scripts\build-sidecar.ps1` after backend changes.

**H.5 — Small live read-only API verification** (owner-run, QAD untouched, after
implementation): run the backend with real QAD config; use a workspace containing parent
`00-00013761-00` (site SW — the already-validated 8D.2 parent, 101 structural occurrences,
max level 4); load MPS, then `GET /api/v1/workspaces/{id}/parts/00-00013761-00/bom`. Verify:
effective date = current local business date and reported in the response; visible lines are
only P/M; actual Level gaps preserved; line order = P/M-filtered order of the accepted
structural sequence (compare against the 8D.2-validated result); duplicate occurrences remain
separate; Net/Non-Net QOH match Stage 6/shared-inventory values for selected components
(cross-check via part-detail and/or direct read-only SQL); repeated component occurrences
show identical inventory; no RMA property in the DTO; no hidden (N/S/…) rows present; a
forced QAD failure (e.g., temporarily invalid QAD server in dev config) returns the 503
Problem Details — never an empty 200 BOM.

## I. Risks / Owner Decisions

No genuine unresolved issues were found: every accepted rule in the 8D.3 prompt maps 1:1 onto
an established, accepted repository pattern. Notes for awareness (not decisions):

1. **MPS-not-loaded → 409** — the prompt's failure list omits it, but scope validation
   requires a loaded MPS snapshot, and both Stage 6 (`PartDetail`) and Stage 7 (Work Orders)
   return 409 `"MPS data not loaded"` for exactly this state. Adopted per §5's
   "inspect the existing Stage 6 PartDetail and Stage 7 … patterns and recommend reuse."
2. **`site` in the response** — included because the existing convention already exposes
   workspace-scope metadata (`PartDetailResponseDto.Site`, `MpsSnapshotMetadataDto.Site`),
   which is the prompt §10's stated exception. It also disambiguates which site's inventory
   the QOH values belong to.
3. **Stale warning wording** — `"Showing the last known BOM information. A newer refresh
   could not be completed."` follows the load-bearing PartDetail wording pattern.
4. **No `pt_mstr` existence query** — per prompt §5 and the approved 8D.2 deferral: MPS scope
   answers the API question; in-scope parent with no BOM → 200 `[]`.
5. Inherited accepted trait: lazy cache is keyed by workspace `AssignmentId` (a workspace-site
   edit does not directly invalidate; the next successful MPS load re-qualifies) — identical
   to the accepted Stage 6 behavior; no new mechanism introduced.

**If no owner decision is required: confirmed — none is.** Approval of this plan is the only
gate before implementation.

## J. Stop Confirmation

- **No production files changed** (this planning pass wrote only this plan file, `PLAN.md`,
  per the 8D.1/8D.2 planning-pass convention; no source files touched).
- **No tests changed or added.**
- **No generated files changed** — a baseline `dotnet test` run regenerated
  `docs/openapi/Kst.Api.json` at build time; `git status` confirms the working tree is
  **clean** (byte-identical output).
- **No commits created or pushed.**
- **Ready for human review/approval.** Implementation starts only after explicit owner
  approval of this plan.

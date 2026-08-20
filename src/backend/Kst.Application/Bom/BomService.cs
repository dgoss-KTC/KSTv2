using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.Inventory;
using Microsoft.Extensions.Logging;

namespace Kst.Application.Bom;

/// <summary>
/// Resolves lazily-loaded Stage 8 scheduler-visible BOM for a workspace's selected MPS parent
/// part: complete effective structural traversal (accepted 8D.2) first, then P/M visibility
/// filtering (Application-owned), then one batch-capable shared Site + Part inventory read
/// (accepted 8D.1) for the distinct visible components, composed into
/// <see cref="BomLine"/>s in original structural order.
///
/// Never triggers an MPS load itself — it only reads the current <see cref="IMpsSnapshotStore"/>
/// state. Business identity is Site + ParentPart + EffectiveDate; freshness is the workspace's
/// current MPS snapshot identity. A cached entry is compatible only when its Site and effective
/// date match the current ones — a fresh hit additionally requires the same MPS snapshot
/// generation, while a same-site/same-effective-date entry may be served as stale last-good when
/// a load fails. A cached BOM from another Site or another effective date is never served. A
/// failed partial load (structural or inventory) never replaces the last-good complete entry and
/// is never reported as an empty BOM. Cancellation from either reader propagates as-is — it is
/// neither a load failure nor a stale-fallback opportunity.
/// </summary>
public sealed class BomService
{
    private const string StaleWarning =
        "Showing the last known BOM information. A newer refresh could not be completed.";

    private readonly IWorkspaceConfigurationService _workspaces;
    private readonly IMpsSnapshotStore _mpsSnapshotStore;
    private readonly IBomSourceReader _bomSourceReader;
    private readonly IPartInventoryReader _inventoryReader;
    private readonly IBomCacheStore _cache;
    private readonly IClock _clock;
    private readonly ILogger<BomService> _logger;

    public BomService(
        IWorkspaceConfigurationService workspaces,
        IMpsSnapshotStore mpsSnapshotStore,
        IBomSourceReader bomSourceReader,
        IPartInventoryReader inventoryReader,
        IBomCacheStore cache,
        IClock clock,
        ILogger<BomService> logger)
    {
        _workspaces = workspaces;
        _mpsSnapshotStore = mpsSnapshotStore;
        _bomSourceReader = bomSourceReader;
        _inventoryReader = inventoryReader;
        _cache = cache;
        _clock = clock;
        _logger = logger;
    }

    public async Task<BomResult> GetBomAsync(
        Guid workspaceId,
        string parentPart,
        CancellationToken cancellationToken = default)
    {
        if (parentPart is null)
            throw new ArgumentNullException(nameof(parentPart));

        cancellationToken.ThrowIfCancellationRequested();
        var workspaces = await _workspaces.GetWorkspacesAsync();
        var workspace = workspaces.Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new BomWorkspaceNotFoundException(workspaceId);

        var mpsState = _mpsSnapshotStore.GetState(workspaceId);
        if (mpsState.Snapshot is null)
            return BomResult.MpsNotLoaded;

        var normalizedParent = parentPart.Trim();
        var inScope = mpsState.Snapshot.ResolvedParts.Any(
            p => string.Equals(p.ParentPart, normalizedParent, StringComparison.OrdinalIgnoreCase));
        if (!inScope)
            return BomResult.OutOfScope;

        var effectiveDate = DateOnly.FromDateTime(_clock.LocalNow.Date);
        var currentSnapshotId = mpsState.Snapshot.Id;
        var cached = _cache.Get(workspaceId, normalizedParent);
        if (cached is not null && IsFreshHit(cached, workspace.Site, currentSnapshotId, effectiveDate))
            return BomResult.Loaded(cached.Bom);

        Bom composed;
        try
        {
            composed = await ComposeAsync(workspace.Site, normalizedParent, effectiveDate, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is control flow, not a load failure: it must never be converted
            // into a stale last-good result or an Unavailable outcome.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "BOM load failed for workspace {WorkspaceId} part {ParentPart} (effective {EffectiveDate}).",
                workspaceId, normalizedParent, effectiveDate);

            if (cached is not null && IsStaleEligible(cached, workspace.Site, effectiveDate))
                return BomResult.Loaded(cached.Bom with { IsStale = true, Warning = StaleWarning });

            return BomResult.Unavailable;
        }

        _cache.Set(workspaceId, normalizedParent, new BomCacheEntry(
            workspaceId, workspace.Site, normalizedParent, effectiveDate, currentSnapshotId, composed));

        return BomResult.Loaded(composed);
    }

    /// <summary>
    /// Composes one complete successful BOM: the full structural read is filtered to
    /// scheduler-visible P/M rows (order-preserving; hidden rows' descendants stay eligible;
    /// levels untouched), the distinct visible component parts are requested in ONE batch-capable
    /// inventory read, and inventory is associated by normalized PartNumber. A missing or
    /// duplicate returned summary is a reader-contract failure and throws — it is never silently
    /// filled with zeros.
    /// </summary>
    private async Task<Bom> ComposeAsync(
        string site,
        string parentPart,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        var occurrences = await _bomSourceReader.ReadAsync(site, parentPart, effectiveDate, cancellationToken);

        // Scheduler visibility is a presentation filter applied AFTER the complete traversal:
        // an order-preserving Where on the flat structural list. Omitting a hidden intermediate
        // does not remove its descendants (they carry their own P/M codes) and never renumbers
        // levels — the visible line order is the structural order restricted to P/M rows.
        var visible = occurrences.Where(o => BomSchedulerVisibility.IsSchedulerVisible(o.PmCode)).ToList();

        Dictionary<string, PartInventorySummary> inventoryByPart =
            new(StringComparer.OrdinalIgnoreCase);
        if (visible.Count > 0)
        {
            var partKeys = visible
                .Select(o => o.ComponentPart.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var summaries = await _inventoryReader.ReadSummariesAsync(site, partKeys, cancellationToken);
            inventoryByPart = IndexSummariesByPart(partKeys, summaries);
        }

        var lines = visible
            .Select(o =>
            {
                var summary = inventoryByPart[o.ComponentPart.Trim()];
                return new BomLine(
                    OccurrenceKey: o.OccurrenceKey,
                    Level: o.Level,
                    ComponentPart: o.ComponentPart,
                    PmCode: o.PmCode,
                    IsPhantom: o.IsPhantom,
                    Description: o.Description,
                    QuantityPer: o.QuantityPer,
                    ScrapPercentage: o.ScrapPercentage,
                    NetQuantityOnHand: summary.NetQuantityOnHand,
                    NonNetQuantityOnHand: summary.NonNetQuantityOnHand);
            })
            .ToList();

        return new Bom(
            Site: site,
            ParentPart: parentPart,
            EffectiveDate: effectiveDate,
            Lines: lines,
            LoadedAtUtc: _clock.UtcNow,
            IsStale: false,
            Warning: null);
    }

    /// <summary>
    /// Indexes returned inventory summaries by normalized PartNumber and verifies the accepted
    /// reader contract: exactly one summary for every requested distinct part. An explicit
    /// zero/zero summary is a valid data result; a missing or duplicate summary is a
    /// reader/integration contract failure and throws (the caller treats it as a normal BOM
    /// load failure — same-site/same-date stale last-good or Unavailable — never an invented
    /// zero and never a cached partial composition).
    /// </summary>
    private static Dictionary<string, PartInventorySummary> IndexSummariesByPart(
        IReadOnlyList<string> requestedKeys,
        IReadOnlyList<PartInventorySummary> summaries)
    {
        var byPart = new Dictionary<string, PartInventorySummary>(StringComparer.OrdinalIgnoreCase);
        foreach (var summary in summaries)
        {
            var key = summary.PartNumber.Trim();
            if (!byPart.TryAdd(key, summary))
                throw new InvalidOperationException(
                    $"Inventory reader contract violation: more than one summary was returned for part '{key}'.");
        }

        foreach (var requestedKey in requestedKeys)
        {
            if (!byPart.ContainsKey(requestedKey))
                throw new InvalidOperationException(
                    $"Inventory reader contract violation: no summary was returned for requested part '{requestedKey}'.");
        }

        return byPart;
    }

    /// <summary>
    /// Fresh hit: compatible entry (current site + current effective date) loaded against the
    /// workspace's current MPS snapshot generation.
    /// </summary>
    private static bool IsFreshHit(
        BomCacheEntry cached,
        string site,
        SnapshotId currentSnapshotId,
        DateOnly effectiveDate) =>
        IsStaleEligible(cached, site, effectiveDate) && cached.LoadedAgainstMpsSnapshotId == currentSnapshotId;

    /// <summary>
    /// Stale-last-good eligibility (also the compatibility half of a fresh hit): same site and
    /// same effective date. Any other snapshot generation is acceptable for stale fallback; a
    /// different site or different effective date is NEVER eligible (cross-site and cross-date
    /// fallback are both forbidden).
    /// </summary>
    private static bool IsStaleEligible(BomCacheEntry? cached, string site, DateOnly effectiveDate) =>
        cached is not null
        && string.Equals(cached.Site, site, StringComparison.OrdinalIgnoreCase)
        && cached.EffectiveDate == effectiveDate;
}

using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.ComponentDetail;
using Kst.Domain.Inventory;
using Kst.Domain.PartDetail;
using Microsoft.Extensions.Logging;

namespace Kst.Application.ComponentDetail;

/// <summary>
/// Resolves lazily-loaded Stage 8D.5 Component Detail for a workspace-selected component part:
/// master/selected-site planning/Standard Cost/QCTC source facts (<see cref="IComponentSourceReader"/>)
/// composed with one shared Site + Part inventory read (accepted 8D.1, <see cref="IPartInventoryReader"/>).
///
/// Never triggers an MPS load itself — it only reads the current <see cref="IMpsSnapshotStore"/>
/// state. Component identity is Site + ComponentPart, established solely by a <c>pt_mstr</c> row
/// (never BOM occurrence or the workspace's resolved MPS parent scope — see the accepted §14
/// not-found semantics). Freshness participates in the existing MPS snapshot generation model
/// because <c>MpsSnapshot.Id</c> is the only workspace freshness-generation identity in the
/// repository: a fresh hit additionally requires the current MPS snapshot generation, while a
/// same-site entry may be served as stale last-good when a reload fails. A cached detail from
/// another Site is never served, fresh or stale. A failed composition never replaces the
/// last-good complete entry. Cancellation from either reader propagates as-is — it is neither a
/// load failure nor a stale-fallback opportunity.
/// </summary>
public sealed class ComponentDetailService
{
    private const string StaleWarning =
        "Showing the last known component information. A newer refresh could not be completed.";

    private readonly IWorkspaceConfigurationService _workspaces;
    private readonly IMpsSnapshotStore _mpsSnapshotStore;
    private readonly IComponentSourceReader _sourceReader;
    private readonly IPartInventoryReader _inventoryReader;
    private readonly IComponentDetailCacheStore _cache;
    private readonly IClock _clock;
    private readonly ILogger<ComponentDetailService> _logger;

    public ComponentDetailService(
        IWorkspaceConfigurationService workspaces,
        IMpsSnapshotStore mpsSnapshotStore,
        IComponentSourceReader sourceReader,
        IPartInventoryReader inventoryReader,
        IComponentDetailCacheStore cache,
        IClock clock,
        ILogger<ComponentDetailService> logger)
    {
        _workspaces = workspaces;
        _mpsSnapshotStore = mpsSnapshotStore;
        _sourceReader = sourceReader;
        _inventoryReader = inventoryReader;
        _cache = cache;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ComponentDetailResult> GetComponentDetailAsync(
        Guid workspaceId,
        string componentPart,
        CancellationToken cancellationToken = default)
    {
        if (componentPart is null)
            throw new ArgumentNullException(nameof(componentPart));

        cancellationToken.ThrowIfCancellationRequested();
        var workspaces = await _workspaces.GetWorkspacesAsync();
        var workspace = workspaces.Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new ComponentWorkspaceNotFoundException(workspaceId);

        var mpsState = _mpsSnapshotStore.GetState(workspaceId);
        if (mpsState.Snapshot is null)
            return ComponentDetailResult.MpsNotLoaded;

        var normalizedPart = componentPart.Trim();
        var currentSnapshotId = mpsState.Snapshot.Id;
        var cached = _cache.Get(workspaceId, normalizedPart);
        if (cached is not null && IsFreshHit(cached, workspace.Site, currentSnapshotId))
            return ComponentDetailResult.Loaded(cached.Detail);

        ComponentDetail? composed;
        try
        {
            composed = await ComposeAsync(workspace.Site, normalizedPart, cancellationToken);
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
                "Component Detail load failed for workspace {WorkspaceId} component {ComponentPart}.",
                workspaceId, normalizedPart);

            if (cached is not null && IsStaleEligible(cached, workspace.Site))
                return ComponentDetailResult.Loaded(cached.Detail with { IsStale = true, Warning = StaleWarning });

            return ComponentDetailResult.Unavailable;
        }

        if (composed is null)
            return ComponentDetailResult.NotFound;

        _cache.Set(workspaceId, normalizedPart, new ComponentDetailCacheEntry(
            workspaceId, workspace.Site, normalizedPart, currentSnapshotId, composed));

        return ComponentDetailResult.Loaded(composed);
    }

    /// <summary>
    /// Composes one complete successful Component Detail, or returns null when the source reader
    /// reports no <c>pt_mstr</c> row (a legitimate not-found result, not a failure — the caller
    /// never treats a null return from here as an exception-equivalent). A missing Standard
    /// Cost/QCTC/planning value inside a found component's facts is likewise not a failure; it
    /// surfaces as a null field on the composed record.
    /// </summary>
    private async Task<ComponentDetail?> ComposeAsync(
        string site,
        string componentPart,
        CancellationToken cancellationToken)
    {
        var facts = await _sourceReader.ReadAsync(site, componentPart, cancellationToken);
        if (facts is null)
            return null;

        var summaries = await _inventoryReader.ReadSummariesAsync(site, [componentPart], cancellationToken);
        var summary = IndexSingleSummary(componentPart, summaries);

        return new ComponentDetail(
            Site: site,
            ComponentPart: facts.ComponentPart,
            Description: facts.Description,
            PartStatusCode: facts.PartStatusCode,
            PartStatusDescription: PartStatusMap.Describe(facts.PartStatusCode),
            IosCode: facts.IosCode,
            NetQuantityOnHand: summary.NetQuantityOnHand,
            NonNetQuantityOnHand: summary.NonNetQuantityOnHand,
            StandardCost: facts.StandardCost,
            Qctc: facts.Qctc,
            TimeFence: facts.TimeFence,
            SafetyTime: facts.SafetyTime,
            SafetyStock: facts.SafetyStock,
            BuyerPlanner: facts.BuyerPlanner,
            PurchaseLeadTimeDays: facts.PurchaseLeadTimeDays,
            InspectionLeadTimeDays: facts.InspectionLeadTimeDays,
            CumulativeLeadTimeDays: facts.CumulativeLeadTimeDays,
            MinimumOrderQuantity: facts.MinimumOrderQuantity,
            OrderMultiple: facts.OrderMultiple,
            LoadedAtUtc: _clock.UtcNow,
            IsStale: false,
            Warning: null);
    }

    /// <summary>
    /// Verifies the accepted reader contract for the single requested part: exactly one summary,
    /// matched by normalized PartNumber (not position). A missing or duplicate summary is a
    /// reader/integration contract failure and throws — the caller treats it as a normal
    /// Component Detail load failure (same-site stale last-good or Unavailable), never an
    /// invented zero and never a cached partial composition.
    /// </summary>
    private static PartInventorySummary IndexSingleSummary(
        string componentPart,
        IReadOnlyList<PartInventorySummary> summaries)
    {
        PartInventorySummary? match = null;
        foreach (var summary in summaries)
        {
            if (!string.Equals(summary.PartNumber.Trim(), componentPart, StringComparison.OrdinalIgnoreCase))
                continue;

            if (match is not null)
                throw new InvalidOperationException(
                    $"Inventory reader contract violation: more than one summary was returned for part '{componentPart}'.");

            match = summary;
        }

        return match ?? throw new InvalidOperationException(
            $"Inventory reader contract violation: no summary was returned for requested part '{componentPart}'.");
    }

    /// <summary>
    /// Fresh hit: a same-site entry loaded against the workspace's current MPS snapshot generation.
    /// </summary>
    private static bool IsFreshHit(ComponentDetailCacheEntry cached, string site, SnapshotId currentSnapshotId) =>
        IsStaleEligible(cached, site) && cached.LoadedAgainstMpsSnapshotId == currentSnapshotId;

    /// <summary>
    /// Stale-last-good eligibility (also the compatibility half of a fresh hit): same site. Any
    /// other snapshot generation is acceptable for stale fallback; a different site is NEVER
    /// eligible (cross-site fallback is forbidden).
    /// </summary>
    private static bool IsStaleEligible(ComponentDetailCacheEntry? cached, string site) =>
        cached is not null && string.Equals(cached.Site, site, StringComparison.OrdinalIgnoreCase);
}

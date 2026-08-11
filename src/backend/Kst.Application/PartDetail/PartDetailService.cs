using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.PartDetail;
using Microsoft.Extensions.Logging;

namespace Kst.Application.PartDetail;

/// <summary>
/// Resolves lazily-loaded Stage 6 PartDetail for a workspace's selected MPS parent part. Never
/// triggers an MPS load itself — it only reads the current <see cref="IMpsSnapshotStore"/> state,
/// which auto-loading MPS access (<see cref="MpsWorkspaceSnapshotService"/>) elsewhere is responsible
/// for populating. See the accepted Stage 6 contract §9/§13 for the full behavior this implements.
/// </summary>
public sealed class PartDetailService
{
    private readonly IWorkspaceConfigurationService _workspaces;
    private readonly IMpsSnapshotStore _mpsSnapshotStore;
    private readonly IPartDetailSourceReader _reader;
    private readonly IPartDetailCacheStore _cache;
    private readonly IClock _clock;
    private readonly ILogger<PartDetailService> _logger;

    public PartDetailService(
        IWorkspaceConfigurationService workspaces,
        IMpsSnapshotStore mpsSnapshotStore,
        IPartDetailSourceReader reader,
        IPartDetailCacheStore cache,
        IClock clock,
        ILogger<PartDetailService> logger)
    {
        _workspaces = workspaces;
        _mpsSnapshotStore = mpsSnapshotStore;
        _reader = reader;
        _cache = cache;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PartDetailResult> GetPartDetailAsync(
        Guid workspaceId,
        string partNumber,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workspaces = await _workspaces.GetWorkspacesAsync();
        var workspace = workspaces.Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new PartDetailWorkspaceNotFoundException(workspaceId);

        var mpsState = _mpsSnapshotStore.GetState(workspaceId);
        if (mpsState.Snapshot is null)
            return PartDetailResult.MpsNotLoaded;

        var normalizedPart = partNumber.Trim();
        var inScope = mpsState.Snapshot.ResolvedParts.Any(
            p => string.Equals(p.ParentPart, normalizedPart, StringComparison.OrdinalIgnoreCase));
        if (!inScope)
            return PartDetailResult.OutOfScope;

        var currentSnapshotId = mpsState.Snapshot.Id;
        var cached = _cache.Get(workspaceId, normalizedPart);
        if (cached is not null && cached.LoadedAgainstMpsSnapshotId == currentSnapshotId)
            return PartDetailResult.Loaded(cached.Detail);

        var today = DateOnly.FromDateTime(_clock.LocalNow.Date);
        PartDetailSourceFacts? facts;
        try
        {
            facts = await _reader.ReadAsync(workspace.Site, normalizedPart, today, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PartDetail QAD read failed for workspace {WorkspaceId} part {PartNumber}.",
                workspaceId, normalizedPart);

            if (cached is null)
                return PartDetailResult.Unavailable;

            var staleDetail = cached.Detail with
            {
                IsStale = true,
                Warning = "Showing the last known part information. A newer refresh could not be completed."
            };
            return PartDetailResult.Loaded(staleDetail);
        }

        if (facts is null)
            return PartDetailResult.MissingPart;

        var detail = new PartDetail(
            Site: workspace.Site,
            PartNumber: facts.PartNumber,
            PlannerCode: facts.PlannerCode,
            ManufacturingLeadTimeDays: facts.ManufacturingLeadTimeDays,
            SafetyTimeDays: facts.SafetyTimeDays,
            PartStatusCode: facts.PartStatusCode,
            PartStatusDescription: PartStatusMap.Describe(facts.PartStatusCode),
            CurrentRevision: facts.CurrentRevision,
            Description: facts.Description,
            IosCode: facts.IosCode,
            SafetyStockQuantity: facts.SafetyStockQuantity,
            QuantityOnHand: facts.QuantityOnHand,
            QuantityNonNet: facts.QuantityNonNet,
            QuantityRmaOnHand: facts.QuantityRmaOnHand,
            PriceBreaks: facts.PriceBreaks,
            LoadedAtUtc: _clock.UtcNow,
            IsStale: false,
            Warning: null);

        _cache.Set(workspaceId, normalizedPart, new PartDetailCacheEntry(
            workspaceId, normalizedPart, currentSnapshotId, detail));

        return PartDetailResult.Loaded(detail);
    }
}

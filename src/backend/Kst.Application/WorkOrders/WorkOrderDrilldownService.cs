using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.WorkOrders;
using Kst.Domain.Workspaces;
using Microsoft.Extensions.Logging;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Single Stage 7 orchestration service, supporting exactly three operations: top-level Work Orders
/// for a selected MPS bucket, material/kitting detail for a selected WOID, and candidate Work Orders
/// for a manufactured component. Deliberately not split into separate Kitting/Variance/BOM-explosion/
/// Inventory/Shortage services, per the accepted Stage 7 contract. All three are lazily cached per
/// (workspace, MPS snapshot generation, ...) key: unlike Stage 6 PartDetail, a cache lookup against a
/// superseded MPS snapshot id is a plain miss, never a stale fallback (a new successful MPS refresh
/// must invalidate prior Stage 7 investigation data outright). A failed lazy QAD read is never cached
/// and never reported as an empty business result, so retry stays available and honest. Every method
/// also requires the caller's last-seen snapshot id and returns <c>SnapshotChanged</c> if the workspace
/// has since moved to a different MPS snapshot generation, so stale UI selections are never silently
/// answered against new data.
/// </summary>
public sealed class WorkOrderDrilldownService
{
    private readonly IWorkspaceConfigurationService _workspaces;
    private readonly IMpsSnapshotStore _mpsSnapshotStore;
    private readonly IWorkOrderSummaryReader _summaryReader;
    private readonly IWorkOrderMaterialReader _materialReader;
    private readonly IWorkOrderSummaryCacheStore _summaryCache;
    private readonly IWorkOrderMaterialCacheStore _materialCache;
    private readonly IWorkOrderCandidateCacheStore _candidateCache;
    private readonly ILogger<WorkOrderDrilldownService> _logger;

    public WorkOrderDrilldownService(
        IWorkspaceConfigurationService workspaces,
        IMpsSnapshotStore mpsSnapshotStore,
        IWorkOrderSummaryReader summaryReader,
        IWorkOrderMaterialReader materialReader,
        IWorkOrderSummaryCacheStore summaryCache,
        IWorkOrderMaterialCacheStore materialCache,
        IWorkOrderCandidateCacheStore candidateCache,
        ILogger<WorkOrderDrilldownService> logger)
    {
        _workspaces = workspaces;
        _mpsSnapshotStore = mpsSnapshotStore;
        _summaryReader = summaryReader;
        _materialReader = materialReader;
        _summaryCache = summaryCache;
        _materialCache = materialCache;
        _candidateCache = candidateCache;
        _logger = logger;
    }

    /// <summary>
    /// Resolves top-level (depth 1) Work Orders for one MPS bucket, reusing the WO references already
    /// retained on that bucket by <see cref="MpsScheduleBuilder"/> — never re-derived from
    /// <c>wo_due_date</c> here. Only Allocating/Frozen/Released work orders are eligible; Planned and
    /// Explicitly Scheduled MPS-only rows never have a corresponding WO card. Already-cached summaries
    /// (within this MPS snapshot generation) are reused; only missing WOIDs are read from QAD.
    /// </summary>
    public async Task<WorkOrderBucketResult> GetBucketWorkOrdersAsync(
        Guid workspaceId,
        SnapshotId requestedSnapshotId,
        string parentPart,
        MpsBucketKind bucketKind,
        DateOnly? weekLabel,
        MpsDateBasis dateBasis,
        int horizonWeeks,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workspace = await FindWorkspaceAsync(workspaceId);

        var mpsState = _mpsSnapshotStore.GetState(workspaceId);
        if (mpsState.Snapshot is null)
            return WorkOrderBucketResult.MpsNotLoaded;

        var snapshotId = mpsState.Snapshot.Id;
        if (snapshotId != requestedSnapshotId)
            return WorkOrderBucketResult.SnapshotChanged;

        var normalizedPart = parentPart.Trim();
        var schedules = MpsScheduleBuilder.Build(
            mpsState.Snapshot.ResolvedParts, mpsState.Snapshot.SourceRows, dateBasis, horizonWeeks, today);

        var schedule = schedules.FirstOrDefault(
            s => string.Equals(s.ParentPart, normalizedPart, StringComparison.OrdinalIgnoreCase));
        if (schedule is null)
            return WorkOrderBucketResult.PartNotInScope;

        var bucket = schedule.Buckets.FirstOrDefault(b => b.Kind == bucketKind && b.WeekLabel == weekLabel);
        if (bucket is null)
            return WorkOrderBucketResult.BucketNotFound;

        var eligibleWoids = bucket.WorkOrders
            .Where(w => w.State is MpsWorkOrderState.Allocating or MpsWorkOrderState.Frozen or MpsWorkOrderState.Released)
            .Select(w => w.WorkOrderId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (eligibleWoids.Count == 0)
            return WorkOrderBucketResult.Loaded(snapshotId, []);

        var resolved = new Dictionary<string, WorkOrderSummary>(StringComparer.OrdinalIgnoreCase);
        var missingWoids = new List<string>();
        foreach (var woid in eligibleWoids)
        {
            var cached = _summaryCache.Get(workspaceId, snapshotId, woid);
            if (cached is not null)
                resolved[woid] = cached.Summary;
            else
                missingWoids.Add(woid);
        }

        if (missingWoids.Count > 0)
        {
            IReadOnlyList<WorkOrderSummary> fetched;
            try
            {
                fetched = await _summaryReader.ReadByWoidsAsync(workspace.Site, missingWoids, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Work order summary read failed for workspace {WorkspaceId} bucket part {ParentPart}.",
                    workspaceId, normalizedPart);
                return WorkOrderBucketResult.Unavailable;
            }

            foreach (var summary in fetched)
            {
                _summaryCache.Set(workspaceId, snapshotId, summary.Woid,
                    new WorkOrderSummaryCacheEntry(workspaceId, snapshotId, summary.Woid, summary));
                resolved[summary.Woid] = summary;
            }
        }

        var workOrders = eligibleWoids.Where(resolved.ContainsKey).Select(woid => resolved[woid]).ToList();
        return WorkOrderBucketResult.Loaded(snapshotId, workOrders);
    }

    /// <summary>Reads material/kitting lines for one WOID, reusing a cached read within this MPS snapshot generation.</summary>
    public async Task<WorkOrderMaterialResult> GetMaterialLinesAsync(
        Guid workspaceId,
        SnapshotId requestedSnapshotId,
        string woid,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workspace = await FindWorkspaceAsync(workspaceId);

        var mpsState = _mpsSnapshotStore.GetState(workspaceId);
        if (mpsState.Snapshot is null)
            return WorkOrderMaterialResult.MpsNotLoaded;

        var snapshotId = mpsState.Snapshot.Id;
        if (snapshotId != requestedSnapshotId)
            return WorkOrderMaterialResult.SnapshotChanged;

        var normalizedWoid = woid.Trim();

        var cached = _materialCache.Get(workspaceId, snapshotId, normalizedWoid);
        if (cached is not null)
            return WorkOrderMaterialResult.Loaded(snapshotId, cached.Lines);

        IReadOnlyList<WorkOrderMaterialLine> lines;
        try
        {
            lines = await _materialReader.ReadAsync(workspace.Site, normalizedWoid, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Work order material read failed for workspace {WorkspaceId} WOID {Woid}.", workspaceId, normalizedWoid);
            return WorkOrderMaterialResult.Unavailable;
        }

        _materialCache.Set(workspaceId, snapshotId, normalizedWoid,
            new WorkOrderMaterialCacheEntry(workspaceId, snapshotId, normalizedWoid, lines));
        return WorkOrderMaterialResult.Loaded(snapshotId, lines);
    }

    /// <summary>
    /// Reads candidate subassembly work orders for a manufactured component at
    /// <paramref name="targetDepth"/> (2 = candidates under a depth-1 scheduled parent, 3 = candidates
    /// under a depth-2 candidate). Depth 1 never goes through this method — it is resolved by
    /// <see cref="GetBucketWorkOrdersAsync"/>. Rejects any depth outside [2, MaxDrillDepth]. The
    /// immediate parent's Due Date is always resolved server-side (never trusts a caller-supplied
    /// date) and the requested component must be a manufactured line on the immediate parent's
    /// material list. Cached per (workspace, MPS snapshot generation, immediate parent WOID, component
    /// part, target depth).
    /// </summary>
    public async Task<WorkOrderCandidateResult> GetCandidatesAsync(
        Guid workspaceId,
        SnapshotId requestedSnapshotId,
        string immediateParentWoid,
        string componentPart,
        int targetDepth,
        CancellationToken cancellationToken = default)
    {
        if (targetDepth < 2 || targetDepth > WorkOrderDrilldownPolicy.MaxDrillDepth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetDepth),
                targetDepth,
                $"Candidate depth must be between 2 and {WorkOrderDrilldownPolicy.MaxDrillDepth}.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var workspace = await FindWorkspaceAsync(workspaceId);

        var mpsState = _mpsSnapshotStore.GetState(workspaceId);
        if (mpsState.Snapshot is null)
            return WorkOrderCandidateResult.MpsNotLoaded;

        var snapshotId = mpsState.Snapshot.Id;
        if (snapshotId != requestedSnapshotId)
            return WorkOrderCandidateResult.SnapshotChanged;

        var normalizedParentWoid = immediateParentWoid.Trim();
        var normalizedComponent = componentPart.Trim();

        var (parentSummary, parentReadFailed) = await ResolveSingleSummaryAsync(
            workspaceId, workspace.Site, snapshotId, normalizedParentWoid, cancellationToken);
        if (parentReadFailed)
            return WorkOrderCandidateResult.Unavailable;
        if (parentSummary is null)
            return WorkOrderCandidateResult.WorkOrderNotFound;
        if (parentSummary.DueDate is null)
            return WorkOrderCandidateResult.ParentDueDateUnavailable;

        var materialResult = await GetMaterialLinesAsync(workspaceId, requestedSnapshotId, normalizedParentWoid, cancellationToken);
        switch (materialResult.Kind)
        {
            case WorkOrderMaterialOutcomeKind.SnapshotChanged:
                return WorkOrderCandidateResult.SnapshotChanged;
            case WorkOrderMaterialOutcomeKind.MpsNotLoaded:
                return WorkOrderCandidateResult.MpsNotLoaded;
            case WorkOrderMaterialOutcomeKind.Unavailable:
                return WorkOrderCandidateResult.Unavailable;
        }

        var componentLine = materialResult.Lines!.FirstOrDefault(
            l => string.Equals(l.ComponentPart, normalizedComponent, StringComparison.OrdinalIgnoreCase));
        if (componentLine is null || !componentLine.IsManufactured)
            return WorkOrderCandidateResult.ComponentNotManufactured;

        var cached = _candidateCache.Get(workspaceId, snapshotId, normalizedParentWoid, normalizedComponent, targetDepth);
        if (cached is not null)
            return WorkOrderCandidateResult.Loaded(snapshotId, cached.Result);

        CandidateWorkOrdersResult fetched;
        try
        {
            fetched = await _summaryReader.ReadCandidatesAsync(
                workspace.Site,
                normalizedComponent,
                WorkOrderDrilldownPolicy.CandidateResultLimit,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Candidate work order read failed for workspace {WorkspaceId} component {ComponentPart} depth {TargetDepth}.",
                workspaceId, normalizedComponent, targetDepth);
            return WorkOrderCandidateResult.Unavailable;
        }

        _candidateCache.Set(workspaceId, snapshotId, normalizedParentWoid, normalizedComponent, targetDepth,
            new WorkOrderCandidateCacheEntry(
                workspaceId, snapshotId, normalizedParentWoid, normalizedComponent, targetDepth, fetched));

        return WorkOrderCandidateResult.Loaded(snapshotId, fetched);
    }

    /// <summary>Resolves one WOID's summary via cache-or-read, without the batching used by <see cref="GetBucketWorkOrdersAsync"/>.</summary>
    private async Task<(WorkOrderSummary? Summary, bool Failed)> ResolveSingleSummaryAsync(
        Guid workspaceId, string site, SnapshotId snapshotId, string woid, CancellationToken cancellationToken)
    {
        var cached = _summaryCache.Get(workspaceId, snapshotId, woid);
        if (cached is not null)
            return (cached.Summary, false);

        IReadOnlyList<WorkOrderSummary> fetched;
        try
        {
            fetched = await _summaryReader.ReadByWoidsAsync(site, [woid], cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Work order summary read failed for workspace {WorkspaceId} WOID {Woid}.", workspaceId, woid);
            return (null, true);
        }

        var summary = fetched.FirstOrDefault(s => string.Equals(s.Woid, woid, StringComparison.OrdinalIgnoreCase));
        if (summary is not null)
        {
            _summaryCache.Set(workspaceId, snapshotId, summary.Woid,
                new WorkOrderSummaryCacheEntry(workspaceId, snapshotId, summary.Woid, summary));
        }

        return (summary, false);
    }

    private async Task<WorkspaceAssignment> FindWorkspaceAsync(Guid workspaceId)
    {
        var workspaces = await _workspaces.GetWorkspacesAsync();
        return workspaces.Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new WorkOrderDrilldownWorkspaceNotFoundException(workspaceId);
    }
}

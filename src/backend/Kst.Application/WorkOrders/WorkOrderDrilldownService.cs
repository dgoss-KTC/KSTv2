using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.WorkOrders;
using Kst.Domain.Workspaces;
using Microsoft.Extensions.Logging;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Single Stage 7/7R orchestration service, supporting exactly three operations: the parent-scoped
/// four-week Work Order planning window (Stage 7R — serving both the parent-level population and the
/// optional bucket-filtered population from one algorithm), material/kitting detail for a selected
/// WOID, and candidate Work Orders for a manufactured component. Deliberately not split into separate
/// Kitting/Variance/BOM-explosion/Inventory/Shortage services, per the accepted Stage 7 contract. All
/// are lazily cached per (workspace, MPS snapshot generation, ...) key: unlike Stage 6 PartDetail, a
/// cache lookup against a superseded MPS snapshot id is a plain miss, never a stale fallback (a new
/// successful MPS refresh must invalidate prior Stage 7 investigation data outright). A failed lazy
/// QAD read is never cached and never reported as an empty business result, so retry stays available
/// and honest. Every method also requires the caller's last-seen snapshot id and returns
/// <c>SnapshotChanged</c> if the workspace has since moved to a different MPS snapshot generation, so
/// stale UI selections are never silently answered against new data.
/// </summary>
public sealed class WorkOrderDrilldownService
{
    private readonly IWorkspaceConfigurationService _workspaces;
    private readonly IMpsSnapshotStore _mpsSnapshotStore;
    private readonly IWorkOrderSummaryReader _summaryReader;
    private readonly IWorkOrderMaterialReader _materialReader;
    private readonly IWorkOrderSummaryCacheStore _summaryCache;
    private readonly IWorkOrderMaterialCacheStore _materialCache;
    private readonly IWorkOrderPlanningWindowCacheStore _planningWindowCache;
    private readonly ILogger<WorkOrderDrilldownService> _logger;

    public WorkOrderDrilldownService(
        IWorkspaceConfigurationService workspaces,
        IMpsSnapshotStore mpsSnapshotStore,
        IWorkOrderSummaryReader summaryReader,
        IWorkOrderMaterialReader materialReader,
        IWorkOrderSummaryCacheStore summaryCache,
        IWorkOrderMaterialCacheStore materialCache,
        IWorkOrderPlanningWindowCacheStore planningWindowCache,
        ILogger<WorkOrderDrilldownService> logger)
    {
        _workspaces = workspaces;
        _mpsSnapshotStore = mpsSnapshotStore;
        _summaryReader = summaryReader;
        _materialReader = materialReader;
        _summaryCache = summaryCache;
        _materialCache = materialCache;
        _planningWindowCache = planningWindowCache;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the parent-scoped four-week Work Order planning window (Stage 7R), sourced directly
    /// from <c>wo_mstr</c> — the MPS retained Work Order references are no longer the authority for
    /// whether an upcoming Work Order exists. The population is Due-Date-based Falldown plus Week 0..3
    /// under the active weekly-bucket basis, for every non-closed, non-RMABOM work order on the parent
    /// part (not limited to A/F/R). <paramref name="bucketKind"/>/<paramref name="weekLabel"/> narrow
    /// the result to a single bucket (Falldown, or one forward week); both null returns the full
    /// parent-level window. The MPS snapshot remains the authority for parent scope and bucket
    /// selection/display, so a requested bucket is validated against the projected schedule. Falldown
    /// is always Due-Date based regardless of <paramref name="dateBasis"/>.
    /// </summary>
    public async Task<WorkOrderPlanningWindowResult> GetPlanningWindowAsync(
        Guid workspaceId,
        SnapshotId requestedSnapshotId,
        string parentPart,
        MpsDateBasis dateBasis,
        MpsBucketKind? bucketKind,
        DateOnly? weekLabel,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workspace = await FindWorkspaceAsync(workspaceId);

        var mpsState = _mpsSnapshotStore.GetState(workspaceId);
        if (mpsState.Snapshot is null)
            return WorkOrderPlanningWindowResult.MpsNotLoaded;

        var snapshotId = mpsState.Snapshot.Id;
        if (snapshotId != requestedSnapshotId)
            return WorkOrderPlanningWindowResult.SnapshotChanged;

        var normalizedPart = parentPart.Trim();

        // The MPS snapshot remains the authority for parent scope and bucket selection/display.
        // Reconstruct the schedule over the planning-window horizon so a requested bucket can be
        // validated against the same weekly-bucket basis the grid displays.
        var schedules = MpsScheduleBuilder.Build(
            mpsState.Snapshot.ResolvedParts, mpsState.Snapshot.SourceRows, dateBasis,
            WorkOrderPlanningWindow.ForwardWeekCount, today);

        var schedule = schedules.FirstOrDefault(
            s => string.Equals(s.ParentPart, normalizedPart, StringComparison.OrdinalIgnoreCase));
        if (schedule is null)
            return WorkOrderPlanningWindowResult.PartNotInScope;

        var weekStart = MpsBusinessCalendar.GetBusinessWeekStart(today);
        var windowEndExclusive = WorkOrderPlanningWindow.GetWindowEndExclusive(weekStart);
        DateOnly? bucketWeekStart = null;

        if (bucketKind is { } kind)
        {
            var bucket = schedule.Buckets.FirstOrDefault(b => b.Kind == kind && b.WeekLabel == weekLabel);
            if (bucket is null)
                return WorkOrderPlanningWindowResult.BucketNotFound;
            if (kind == MpsBucketKind.Weekly)
            {
                if (weekLabel is not { } label)
                    return WorkOrderPlanningWindowResult.BucketNotFound;
                bucketWeekStart = MpsBusinessCalendar.GetBusinessWeekStart(label);
            }
        }

        return await ReadPlanningWindowForAuthorizedPartAsync(
            workspaceId, workspace.Site, snapshotId, normalizedPart, dateBasis, weekStart, windowEndExclusive,
            bucketKind, bucketWeekStart, weekLabel, cancellationToken);
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
    /// <see cref="GetPlanningWindowAsync"/>. Rejects any depth outside [2, MaxDrillDepth]. The
    /// requested component must be a manufactured line on the immediate parent's material list.
    /// Once that navigation authorization succeeds, the component receives the same full Stage 7R
    /// planning window as an MPS parent. No parent/child WO relationship is inferred.
    /// </summary>
    public async Task<WorkOrderCandidateResult> GetCandidatesAsync(
        Guid workspaceId,
        SnapshotId requestedSnapshotId,
        string immediateParentWoid,
        string componentPart,
        int targetDepth,
        MpsDateBasis dateBasis,
        DateOnly today,
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

        var weekStart = MpsBusinessCalendar.GetBusinessWeekStart(today);
        var planningWindow = await ReadPlanningWindowForAuthorizedPartAsync(
            workspaceId, workspace.Site, snapshotId, normalizedComponent, dateBasis, weekStart,
            WorkOrderPlanningWindow.GetWindowEndExclusive(weekStart), null, null, null, cancellationToken);
        return planningWindow.Kind switch
        {
            WorkOrderPlanningWindowOutcomeKind.Loaded => WorkOrderCandidateResult.Loaded(
                snapshotId, planningWindow.WorkOrders!),
            WorkOrderPlanningWindowOutcomeKind.Unavailable => WorkOrderCandidateResult.Unavailable,
            _ => WorkOrderCandidateResult.SnapshotChanged
        };
    }

    /// <summary>
    /// Resolves one WOID's summary via cache-or-read, without the batching used by the planning
    /// window. Uses the status-agnostic single-WOID read (Stage 7R): a planning-window parent may
    /// carry any non-closed status, so the A/F/R eligibility filter must not apply here.
    /// </summary>
    private async Task<(WorkOrderSummary? Summary, bool Failed)> ResolveSingleSummaryAsync(
        Guid workspaceId, string site, SnapshotId snapshotId, string woid, CancellationToken cancellationToken)
    {
        var cached = _summaryCache.Get(workspaceId, snapshotId, woid);
        if (cached is not null)
            return (cached.Summary, false);

        WorkOrderSummary? summary;
        try
        {
            summary = await _summaryReader.ReadByWoidAsync(site, woid, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Work order summary read failed for workspace {WorkspaceId} WOID {Woid}.", workspaceId, woid);
            return (null, true);
        }

        if (summary is not null)
        {
            _summaryCache.Set(workspaceId, snapshotId, summary.Woid,
                new WorkOrderSummaryCacheEntry(workspaceId, snapshotId, summary.Woid, summary));
        }

        return (summary, false);
    }

    private async Task<WorkOrderPlanningWindowResult> ReadPlanningWindowForAuthorizedPartAsync(
        Guid workspaceId, string site, SnapshotId snapshotId, string part, MpsDateBasis dateBasis,
        DateOnly weekStart, DateOnly windowEndExclusive, MpsBucketKind? bucketKind, DateOnly? bucketWeekStart,
        DateOnly? weekLabel, CancellationToken cancellationToken)
    {
        var cached = _planningWindowCache.Get(workspaceId, snapshotId, part, dateBasis, bucketKind, weekLabel);
        if (cached is not null)
            return WorkOrderPlanningWindowResult.Loaded(snapshotId, cached.WorkOrders);

        IReadOnlyList<WorkOrderSummary> workOrders;
        try
        {
            workOrders = await _summaryReader.ReadPlanningWindowAsync(
                site, part, dateBasis, weekStart, windowEndExclusive, bucketKind, bucketWeekStart, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Planning-window work order read failed for workspace {WorkspaceId} part {Part}.", workspaceId, part);
            return WorkOrderPlanningWindowResult.Unavailable;
        }

        _planningWindowCache.Set(workspaceId, snapshotId, part, dateBasis, bucketKind, weekLabel,
            new WorkOrderPlanningWindowCacheEntry(workspaceId, snapshotId, part, dateBasis, bucketKind, weekLabel, workOrders));
        return WorkOrderPlanningWindowResult.Loaded(snapshotId, workOrders);
    }

    private async Task<WorkspaceAssignment> FindWorkspaceAsync(Guid workspaceId)
    {
        var workspaces = await _workspaces.GetWorkspacesAsync();
        return workspaces.Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new WorkOrderDrilldownWorkspaceNotFoundException(workspaceId);
    }
}

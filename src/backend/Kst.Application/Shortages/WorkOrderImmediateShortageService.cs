using Kst.Application.Bom;
using Kst.Application.Mps;
using Kst.Application.WorkOrders;
using Kst.Application.Workspaces;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.Shortages;
using Kst.Domain.WorkOrders;
using Microsoft.Extensions.Logging;

namespace Kst.Application.Shortages;

/// <summary>Composes the bounded Stage 9 WO + Component analysis from accepted source facts and domain rules.</summary>
public sealed class WorkOrderImmediateShortageService
{
    private readonly IWorkspaceConfigurationService _workspaces;
    private readonly IMpsSnapshotStore _mpsSnapshots;
    private readonly IWorkOrderSummaryReader _summaries;
    private readonly IWorkOrderMaterialReader _materials;
    private readonly IBomSourceReader _bom;
    private readonly ICommittedWorkOrderPopulationReader _committed;
    private readonly IHardAllocationReader _hardAllocations;
    private readonly IInventoryPositionReader _inventory;
    private readonly IIssuePolicyReader _issuePolicy;
    private readonly IIssueDaysReader _issueDays;
    private readonly INextPurchaseOrderReader _purchaseOrders;
    private readonly IKssScheduleReader _kssSchedules;
    private readonly IWorkOrderImmediateMaterialCacheStore _cache;
    private readonly ILogger<WorkOrderImmediateShortageService> _logger;

    public WorkOrderImmediateShortageService(
        IWorkspaceConfigurationService workspaces, IMpsSnapshotStore mpsSnapshots,
        IWorkOrderSummaryReader summaries, IWorkOrderMaterialReader materials, IBomSourceReader bom,
        ICommittedWorkOrderPopulationReader committed, IHardAllocationReader hardAllocations,
        IInventoryPositionReader inventory, IIssuePolicyReader issuePolicy, IIssueDaysReader issueDays,
        INextPurchaseOrderReader purchaseOrders, IKssScheduleReader kssSchedules, IWorkOrderImmediateMaterialCacheStore cache,
        ILogger<WorkOrderImmediateShortageService> logger)
    {
        (_workspaces, _mpsSnapshots, _summaries, _materials, _bom, _committed, _hardAllocations, _inventory,
            _issuePolicy, _issueDays, _purchaseOrders, _kssSchedules, _cache, _logger) =
            (workspaces, mpsSnapshots, summaries, materials, bom, committed, hardAllocations, inventory,
              issuePolicy, issueDays, purchaseOrders, kssSchedules, cache, logger);
    }

    public async Task<WorkOrderImmediateMaterialResult> GetImmediateMaterialAsync(
        Guid workspaceId, SnapshotId requestedSnapshotId, string woid, MpsDateBasis dateBasis,
        DateOnly today, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workspace = (await _workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new WorkOrderDrilldownWorkspaceNotFoundException(workspaceId);
        var state = _mpsSnapshots.GetState(workspaceId);
        if (state.Snapshot is null) return WorkOrderImmediateMaterialResult.MpsNotLoaded;
        if (state.Snapshot.Id != requestedSnapshotId) return WorkOrderImmediateMaterialResult.SnapshotChanged;

        var normalizedWoid = woid.Trim();
        try
        {
            var selected = await _summaries.ReadByWoidAsync(workspace.Site, normalizedWoid, cancellationToken);
            if (selected is null) return WorkOrderImmediateMaterialResult.WorkOrderNotInImmediateWindow;
            var summary = await GetSummaryAsync(workspaceId, requestedSnapshotId, selected.PartNumber, dateBasis, today, cancellationToken, requireParentInSnapshot: false);
            if (summary.Kind != WorkOrderImmediateMaterialSummaryOutcomeKind.Loaded)
                return summary.Kind switch
                {
                    WorkOrderImmediateMaterialSummaryOutcomeKind.MpsNotLoaded => WorkOrderImmediateMaterialResult.MpsNotLoaded,
                    WorkOrderImmediateMaterialSummaryOutcomeKind.SnapshotChanged => WorkOrderImmediateMaterialResult.SnapshotChanged,
                    _ => WorkOrderImmediateMaterialResult.Unavailable
                };
            var cached = summary.Analyses!.FirstOrDefault(analysis => string.Equals(analysis.WorkOrder.WoId, normalizedWoid, StringComparison.OrdinalIgnoreCase));
            if (cached is null)
                return WorkOrderImmediateMaterialResult.WorkOrderNotInImmediateWindow;
            var analysis = await EnrichDetailAsync(workspace.Site, cached, today, cancellationToken);
            return WorkOrderImmediateMaterialResult.Loaded(requestedSnapshotId, analysis);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stage 9 immediate material analysis failed for workspace {WorkspaceId} WOID {Woid}.", workspaceId, normalizedWoid);
            return WorkOrderImmediateMaterialResult.Unavailable;
        }
    }

    /// <summary>Calculates the Stage 9 immediate window once for one MPS parent; callers derive card and bucket markers from these rows.</summary>
    public async Task<WorkOrderImmediateMaterialSummaryResult> GetSummaryAsync(
        Guid workspaceId, SnapshotId requestedSnapshotId, string buildPart, MpsDateBasis dateBasis, DateOnly today,
        CancellationToken cancellationToken = default, bool requireParentInSnapshot = true)
    {
        var workspace = (await _workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new WorkOrderDrilldownWorkspaceNotFoundException(workspaceId);
        var state = _mpsSnapshots.GetState(workspaceId);
        if (state.Snapshot is null) return WorkOrderImmediateMaterialSummaryResult.MpsNotLoaded;
        if (state.Snapshot.Id != requestedSnapshotId) return WorkOrderImmediateMaterialSummaryResult.SnapshotChanged;
        if (requireParentInSnapshot && !state.Snapshot.ResolvedParts.Any(part => string.Equals(part.ParentPart, buildPart, StringComparison.OrdinalIgnoreCase)))
            return WorkOrderImmediateMaterialSummaryResult.PartNotInScope;

        var cached = _cache.GetBatch(workspaceId, requestedSnapshotId, buildPart, dateBasis, today);
        if (cached is not null) return WorkOrderImmediateMaterialSummaryResult.Loaded(requestedSnapshotId, cached.Analyses);
        try
        {
            var weekStart = MpsBusinessCalendar.GetBusinessWeekStart(today);
            var windowEnd = WorkOrderPlanningWindow.GetWindowEndExclusive(weekStart);
            var population = await _summaries.ReadPlanningWindowAsync(workspace.Site, buildPart, dateBasis, weekStart, windowEnd, null, null, cancellationToken);
            var requirements = new Dictionary<string, IReadOnlyList<ComponentRequirement>>(StringComparer.OrdinalIgnoreCase);
            var analyses = new List<WorkOrderImmediateMaterialAnalysis>();
            foreach (var summary in population)
            {
                var context = CreateContext(summary, dateBasis, weekStart);
                var read = await ReadRequirementsAsync(workspace.Site, summary, context, today, cancellationToken);
                if (read.AnalysisDiagnostic is not null)
                    analyses.Add(new WorkOrderImmediateMaterialAnalysis(context, [], read.AnalysisDiagnostic));
                else
                    requirements[summary.Woid] = read.Requirements!;
            }
            var parts = requirements.Values.SelectMany(rows => rows).Where(row => row.IsReliable && !row.IsManufactured).Select(row => row.ComponentPart).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var positionsByPart = new Dictionary<string, UsableInventoryPosition>(StringComparer.OrdinalIgnoreCase);
            var hardByPart = new Dictionary<string, List<HardAllocation>>(StringComparer.OrdinalIgnoreCase);
            var allocations = new Dictionary<string, ComponentAllocation>(StringComparer.OrdinalIgnoreCase);
            if (parts.Count > 0)
            {
                var issueDays = await _issueDays.ReadAsync(workspace.Site, cancellationToken);
                if (issueDays is null) return WorkOrderImmediateMaterialSummaryResult.Unavailable;
                var positions = await _inventory.ReadAsync(workspace.Site, parts, today, issueDays.Value, cancellationToken);
                positionsByPart = positions.ToDictionary(position => position.ComponentPart, position => position.Position, StringComparer.OrdinalIgnoreCase);
                if (parts.Any(part => !positionsByPart.ContainsKey(part))) return WorkOrderImmediateMaterialSummaryResult.Unavailable;
                var hard = await _hardAllocations.ReadAsync(workspace.Site, today, issueDays.Value, cancellationToken);
                hardByPart = hard.GroupBy(allocation => allocation.ComponentPart, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
                var committed = await _committed.ReadAsync(workspace.Site, dateBasis, weekStart, windowEnd, cancellationToken);
                allocations = BuildCommittedAllocations(committed, hardByPart, positionsByPart, parts, dateBasis, weekStart);
            }
            foreach (var summary in population.Where(summary => requirements.ContainsKey(summary.Woid)))
                analyses.Add(CreateAnalysis(summary, requirements[summary.Woid], hardByPart, positionsByPart, allocations, dateBasis, weekStart));
            if (_mpsSnapshots.GetState(workspaceId).Snapshot?.Id != requestedSnapshotId) return WorkOrderImmediateMaterialSummaryResult.SnapshotChanged;
            var ordered = analyses.OrderBy(analysis => population.ToList().FindIndex(summary => string.Equals(summary.Woid, analysis.WorkOrder.WoId, StringComparison.OrdinalIgnoreCase))).ToList();
            _cache.SetBatch(new WorkOrderImmediateMaterialBatchCacheEntry(workspaceId, requestedSnapshotId, buildPart, dateBasis, today, ordered));
            return WorkOrderImmediateMaterialSummaryResult.Loaded(requestedSnapshotId, ordered);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stage 9 immediate material summary failed for workspace {WorkspaceId} build part {BuildPart}.", workspaceId, buildPart);
            return WorkOrderImmediateMaterialSummaryResult.Unavailable;
        }
    }

    /// <summary>Combines the authoritative parent-window calculations for every parent represented by the current snapshot.</summary>
    public async Task<WorkOrderImmediateMaterialSummaryResult> GetWorkspaceSummaryAsync(
        Guid workspaceId, SnapshotId requestedSnapshotId, MpsDateBasis dateBasis, DateOnly today, CancellationToken cancellationToken = default)
    {
        var state = _mpsSnapshots.GetState(workspaceId);
        if (state.Snapshot is null) return WorkOrderImmediateMaterialSummaryResult.MpsNotLoaded;
        if (state.Snapshot.Id != requestedSnapshotId) return WorkOrderImmediateMaterialSummaryResult.SnapshotChanged;
        var analyses = new List<WorkOrderImmediateMaterialAnalysis>();
        foreach (var part in state.Snapshot.ResolvedParts)
        {
            var result = await GetSummaryAsync(workspaceId, requestedSnapshotId, part.ParentPart, dateBasis, today, cancellationToken);
            if (result.Kind != WorkOrderImmediateMaterialSummaryOutcomeKind.Loaded) return result;
            analyses.AddRange(result.Analyses!);
        }
        return WorkOrderImmediateMaterialSummaryResult.Loaded(requestedSnapshotId, analyses);
    }

    private static WorkOrderImmediateMaterialAnalysis CreateAnalysis(WorkOrderSummary summary, IReadOnlyList<ComponentRequirement> requirements,
        Dictionary<string, List<HardAllocation>> hard, Dictionary<string, UsableInventoryPosition> positions,
        Dictionary<string, ComponentAllocation> allocations, MpsDateBasis basis, DateOnly weekStart)
    {
        var context = CreateContext(summary, basis, weekStart);
        var rows = requirements.Select(requirement =>
        {
            var position = requirement.IsManufactured || !requirement.IsReliable
                ? new UsableInventoryPosition(0m, new Dictionary<InventoryActivity, decimal>())
                : positions[requirement.ComponentPart];
            var partHard = requirement.IsReliable && !requirement.IsManufactured ? hard.GetValueOrDefault(requirement.ComponentPart) ?? [] : [];
            var ownHard = partHard.Where(allocation => string.Equals(allocation.Woid, context.WoId, StringComparison.OrdinalIgnoreCase)).Sum(allocation => allocation.AllocatedQuantity);
            var allocation = requirement.IsReliable && !requirement.IsManufactured ? allocations.GetValueOrDefault(requirement.ComponentPart) : null;
            var committed = allocation?.ByWoid.GetValueOrDefault(context.WoId);
            var available = committed is { } current ? current.InventoryRemaining + current.AllocatedQuantity
                : allocation?.AdvisoryPool ?? ImmediateMaterialCalculator.CalculateResidualFreeInventory(position.UsableQuantity, partHard.Sum(allocation => allocation.AllocatedQuantity));
            return ImmediateMaterialCalculator.Evaluate(requirement, position, context, ownHard, available);
        }).ToList();
        return new WorkOrderImmediateMaterialAnalysis(context, rows);
    }

    private async Task<WorkOrderImmediateMaterialAnalysis> EnrichDetailAsync(string site, WorkOrderImmediateMaterialAnalysis analysis, DateOnly today, CancellationToken ct)
    {
        var rows = new List<ComponentRow>(analysis.ComponentRows.Count);
        foreach (var row in analysis.ComponentRows)
        {
            var floorStock = await _issuePolicy.ReadAsync(site, row.Requirement.ComponentPart, ct);
            var incoming = row.MaterialStatus == MaterialStatus.Short ? await ReadIncomingAsync(site, row.Requirement.ComponentPart, today, ct) : null;
            rows.Add(row with { IsFloorStockOrNonIssued = !floorStock, Incoming = incoming });
        }
        return analysis with { ComponentRows = rows };
    }

    private async Task<RequirementReadResult> ReadRequirementsAsync(string site, WorkOrderSummary summary, WorkOrderContext context, DateOnly today, CancellationToken ct)
    {
        var actual = IsStatus(summary.Status, "R", "A") || (IsStatus(summary.Status, "E") && string.Equals(summary.WorkOrderType, "F", StringComparison.OrdinalIgnoreCase));
        if (actual)
        {
            var lines = await _materials.ReadAsync(site, summary.Woid, ct);
            return new RequirementReadResult(lines.GroupBy(line => line.ComponentPart, StringComparer.OrdinalIgnoreCase).Select(group =>
            {
                var units = group.Select(line => line.UnitOfMeasure?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var unit = units.Count == 1 ? units[0] : null;
                return new ComponentRequirement(group.Key, group.First().ComponentDescription, RequirementSource.ActualWo,
                    group.Sum(line => ImmediateMaterialCalculator.NormalizeQuantity(line.RequiredQuantity, line.UnitOfMeasure)),
                    group.Sum(line => line.IssuedQuantity), unit,
                    !string.IsNullOrEmpty(unit) && group.All(line => line.IsMasterPmCodeReliable),
                    group.Any(line => line.IsManufactured));
            }).ToList(), null);
        }

        if (!IsStatus(summary.Status, "E", "F", "P"))
            return new RequirementReadResult(null, "The work order status does not have an established requirement source.");
        var effectiveDate = summary.ReleaseDate ?? today;
        var occurrences = await _bom.ReadAsync(site, summary.PartNumber, effectiveDate, ct);
        var requirements = ProjectRequirements(occurrences, context.MaterialBuildQuantity);
        return requirements.Count == 0
            ? new RequirementReadResult(null, "No effective BOM requirements could be resolved for the projected work order.")
            : new RequirementReadResult(requirements, null);
    }

    private static IReadOnlyList<ComponentRequirement> ProjectRequirements(IReadOnlyList<BomOccurrence> occurrences, decimal buildQuantity)
    {
        var path = new List<decimal>();
        var items = new List<(BomOccurrence Occurrence, decimal PathQuantity)>();
        foreach (var occurrence in occurrences)
        {
            while (path.Count >= occurrence.Level) path.RemoveAt(path.Count - 1);
            path.Add(occurrence.QuantityPer ?? 0m);
            if (!occurrence.IsPhantom) items.Add((occurrence, path.Aggregate(1m, (product, quantity) => product * quantity)));
        }
        return items.GroupBy(item => item.Occurrence.ComponentPart, StringComparer.OrdinalIgnoreCase).Select(group =>
        {
            var occurrencesForPart = group.ToList();
            var units = occurrencesForPart.Select(item => item.Occurrence.UnitOfMeasure?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var unit = units.Count == 1 ? units[0] : null;
            var masterPmCodes = occurrencesForPart.Select(item => item.Occurrence.MasterPmCode?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var masterPmCode = masterPmCodes.Count == 1 ? masterPmCodes[0] : null;
            var quantity = ImmediateMaterialCalculator.CalculateProjectedRequirement(
                occurrencesForPart.Select(item => new BomRequirementPath([item.PathQuantity], false)), buildQuantity, unit);
            return new ComponentRequirement(group.Key, occurrencesForPart[0].Occurrence.Description, RequirementSource.ProjectedBom, quantity, 0m, unit,
                !string.IsNullOrEmpty(unit) && !string.IsNullOrEmpty(masterPmCode), string.Equals(masterPmCode, "M", StringComparison.OrdinalIgnoreCase));
        }).ToList();
    }

    private static WorkOrderContext CreateContext(WorkOrderSummary summary, MpsDateBasis basis, DateOnly weekStart) => new(
        summary.Woid, summary.PartNumber, summary.Status, summary.WorkOrderType, summary.DueDate, summary.ReleaseDate,
        ImmediateMaterialCalculator.CalculateProjectedBuildQuantity(summary.OrderedQuantity, summary.CompletedQuantity, summary.RejectedQuantity),
        summary.DueDate < weekStart ? PlanningBucketContext.Falldown : basis == MpsDateBasis.ReleaseDate ? PlanningBucketContext.ForwardRelease : PlanningBucketContext.ForwardDue);

    private static bool IsStatus(string status, params string[] expected) =>
        expected.Any(value => string.Equals(status, value, StringComparison.OrdinalIgnoreCase));

    private static Dictionary<string, ComponentAllocation> BuildCommittedAllocations(
        IReadOnlyList<CommittedWorkOrderComponent> source, Dictionary<string, List<HardAllocation>> hard,
        Dictionary<string, UsableInventoryPosition> positions, IReadOnlyList<string> parts,
        MpsDateBasis basis, DateOnly weekStart)
    {
        var result = new Dictionary<string, ComponentAllocation>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            var partGroup = source.Where(row => string.Equals(row.ComponentPart, part, StringComparison.OrdinalIgnoreCase)).ToList();
            var position = positions[part];
            var byWorkOrder = partGroup.GroupBy(row => row.Woid, StringComparer.OrdinalIgnoreCase).Select(group =>
            {
                var first = group.First();
                var requirement = group.Sum(row => ImmediateMaterialCalculator.NormalizeQuantity(row.RequiredQuantity, row.UnitOfMeasure));
                var issued = group.Sum(row => row.IssuedQuantity);
                var ownHard = (hard.GetValueOrDefault(part) ?? []).Where(a => string.Equals(a.Woid, first.Woid, StringComparison.OrdinalIgnoreCase)).Sum(a => a.AllocatedQuantity);
                var remaining = ImmediateMaterialCalculator.CalculateRemainingRequirement(requirement, issued);
                return new CommittedAllocationInput(new WorkOrderContext(first.Woid, string.Empty, first.Status, first.WorkOrderType, first.DueDate, first.ReleaseDate, 0m,
                    first.DueDate < weekStart ? PlanningBucketContext.Falldown : basis == MpsDateBasis.ReleaseDate ? PlanningBucketContext.ForwardRelease : PlanningBucketContext.ForwardDue),
                    ImmediateMaterialCalculator.CalculateUncoveredRequirement(remaining, ImmediateMaterialCalculator.CalculateOwnHardCoverage(remaining, ownHard)));
            }).ToList();
            var free = ImmediateMaterialCalculator.CalculateResidualFreeInventory(position.UsableQuantity, (hard.GetValueOrDefault(part) ?? []).Sum(a => a.AllocatedQuantity));
            var allocations = ImmediateMaterialCalculator.AllocateCommittedSequentially(byWorkOrder, free);
            result[part] = new ComponentAllocation(allocations.ToDictionary(a => a.WoId, StringComparer.OrdinalIgnoreCase), allocations.LastOrDefault()?.InventoryRemaining ?? free);
        }
        return result;
    }

    private async Task<IncomingContext> ReadIncomingAsync(string site, string componentPart, DateOnly today, CancellationToken ct)
    {
        var purchaseOrder = _purchaseOrders.ReadAsync(site, componentPart, ct);
        var kssSchedule = _kssSchedules.IsKssAsync(site, componentPart, today, ct);
        await Task.WhenAll(purchaseOrder, kssSchedule);
        var po = await purchaseOrder;
        var isKss = await kssSchedule;
        return po is null
            ? new IncomingContext(isKss, isKss ? null : "NO PO", null, null, null, null, null)
            : new IncomingContext(isKss, po.PoState, po.PoNumber, po.DueDate, po.OpenQuantity, po.IsConfirmed, po.TrackingInfo);
    }

    private sealed record ComponentAllocation(IReadOnlyDictionary<string, CommittedAllocation> ByWoid, decimal AdvisoryPool);
    private sealed record RequirementReadResult(IReadOnlyList<ComponentRequirement>? Requirements, string? AnalysisDiagnostic);
}

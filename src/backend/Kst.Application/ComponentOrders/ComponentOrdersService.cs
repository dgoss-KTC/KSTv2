using Kst.Application.Bom;
using Kst.Application.Mps;
using Kst.Domain.Bom;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.ComponentOrders;
using Kst.Domain.Mps;
using Kst.Domain.Workspaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Kst.Application.ComponentOrders;

/// <summary>
/// Stage 10 Component Orders orchestration for the active workspace: resolves the current MPS
/// snapshot, expands every resolved parent's current-effective multi-level BOM at the business
/// date, deduplicates component parts case-insensitively, batch-reads the qualifying conventional
/// open PO lines for those components, and composes the grouped presentation result via
/// <see cref="ComponentOrdersBuilder"/>. The workspace scope is always derived from the snapshot's
/// resolved parents — an unscoped site-wide PO query is never used as workspace scope, and the
/// lazy selected-parent BOM cache is deliberately not reused here (it serves one parent at a time;
/// this is the first real bulk use).
///
/// The result is cached against (workspace, MPS snapshot id, business date): a lookup against a
/// superseded snapshot id is a plain miss, never a stale fallback — a new successful MPS refresh
/// must invalidate prior Component Orders results outright. A failed QAD read is Unavailable: it
/// is never cached and never reported as an empty list, so retry stays available and honest. An
/// empty successful result means no active-workspace component has qualifying conventional open PO
/// lines (a legitimate loaded-empty outcome). Every request requires the caller's last-seen
/// snapshot id and returns <c>SnapshotChanged</c> if the workspace has since moved to a different
/// MPS snapshot generation, so a stale UI context is never silently answered against new data.
/// </summary>
public sealed class ComponentOrdersService
{
    private readonly IWorkspaceConfigurationService _workspaces;
    private readonly IMpsSnapshotStore _mpsSnapshots;
    private readonly IBomSourceReader _bom;
    private readonly IComponentOrderSourceReader _componentOrders;
    private readonly IComponentOrderEnrichmentReader _enrichment;
    private readonly IComponentOrdersCacheStore _cache;
    private readonly ILogger<ComponentOrdersService> _logger;

    public ComponentOrdersService(
        IWorkspaceConfigurationService workspaces,
        IMpsSnapshotStore mpsSnapshots,
        IBomSourceReader bom,
        IComponentOrderSourceReader componentOrders,
        IComponentOrderEnrichmentReader enrichment,
        IComponentOrdersCacheStore cache,
        ILogger<ComponentOrdersService> logger)
    {
        _workspaces = workspaces;
        _mpsSnapshots = mpsSnapshots;
        _bom = bom;
        _componentOrders = componentOrders;
        _enrichment = enrichment;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ComponentOrdersResult> GetComponentOrdersAsync(
        Guid workspaceId,
        SnapshotId requestedSnapshotId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation(
            "Stage 10 component orders service started. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId}",
            workspaceId, requestedSnapshotId.Value);
        var workspace = await FindWorkspaceAsync(workspaceId);

        var state = _mpsSnapshots.GetState(workspaceId);
        if (state.Snapshot is null)
        {
            _logger.LogInformation(
                "Stage 10 component orders service completed. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} Outcome={Outcome} ElapsedMs={ElapsedMs}",
                workspaceId, requestedSnapshotId.Value, ComponentOrdersOutcomeKind.MpsNotLoaded, stopwatch.ElapsedMilliseconds);
            return ComponentOrdersResult.MpsNotLoaded;
        }
        if (state.Snapshot.Id != requestedSnapshotId)
        {
            _logger.LogInformation(
                "Stage 10 component orders service completed. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} Outcome={Outcome} ElapsedMs={ElapsedMs}",
                workspaceId, requestedSnapshotId.Value, ComponentOrdersOutcomeKind.SnapshotChanged, stopwatch.ElapsedMilliseconds);
            return ComponentOrdersResult.SnapshotChanged;
        }

        _logger.LogInformation(
            "Stage 10 component orders resolved current snapshot. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} ResolvedParentCount={ResolvedParentCount} ElapsedMs={ElapsedMs}",
            workspaceId, requestedSnapshotId.Value, state.Snapshot.ResolvedParts.Count, stopwatch.ElapsedMilliseconds);

        var cached = _cache.Get(workspaceId, requestedSnapshotId, today);
        if (cached is not null)
        {
            _logger.LogInformation(
                "Stage 10 component orders cache hit. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} GroupCount={GroupCount} ElapsedMs={ElapsedMs}",
                workspaceId, requestedSnapshotId.Value, cached.Groups.Count, stopwatch.ElapsedMilliseconds);
            return ComponentOrdersResult.Loaded(requestedSnapshotId, cached.Groups);
        }

        _logger.LogInformation(
            "Stage 10 component orders cache miss. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} ElapsedMs={ElapsedMs}",
            workspaceId, requestedSnapshotId.Value, stopwatch.ElapsedMilliseconds);

        try
        {
            _logger.LogInformation(
                "Stage 10 component orders BOM scope phase started. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} ResolvedParentCount={ResolvedParentCount} ElapsedMs={ElapsedMs}",
                workspaceId, requestedSnapshotId.Value, state.Snapshot.ResolvedParts.Count, stopwatch.ElapsedMilliseconds);
            var components = await ResolveComponentPartsAsync(
                workspaceId, requestedSnapshotId, workspace.Site, state.Snapshot.ResolvedParts, today, stopwatch, cancellationToken);

            _logger.LogInformation(
                "Stage 10 component orders BOM scope phase completed. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} ComponentCount={ComponentCount} ElapsedMs={ElapsedMs}",
                workspaceId, requestedSnapshotId.Value, components.Count, stopwatch.ElapsedMilliseconds);

            IReadOnlyList<ComponentOrderLine> lines = components.Count == 0
                ? []
                : await ReadComponentOrdersAsync(workspaceId, requestedSnapshotId, workspace.Site, components, today, stopwatch, cancellationToken);

            var enrichmentAvailability = ComponentOrdersEnrichmentAvailability.Available;
            try
            {
                lines = await EnrichAsync(workspace.Site, lines, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                // QAD lines remain useful on an optional-source failure, but are never cached as complete enrichment.
                enrichmentAvailability = ComponentOrdersEnrichmentAvailability.Unavailable;
            }

            var groups = ComponentOrdersBuilder.BuildGroups(lines);

            // A refresh that completed while we were reading supersedes this result; never cache or serve it.
            if (_mpsSnapshots.GetState(workspaceId).Snapshot?.Id != requestedSnapshotId)
            {
                _logger.LogInformation(
                    "Stage 10 component orders service completed. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} Outcome={Outcome} ElapsedMs={ElapsedMs}",
                    workspaceId, requestedSnapshotId.Value, ComponentOrdersOutcomeKind.SnapshotChanged, stopwatch.ElapsedMilliseconds);
                return ComponentOrdersResult.SnapshotChanged;
            }

            if (enrichmentAvailability == ComponentOrdersEnrichmentAvailability.Available)
                _cache.Set(new ComponentOrdersCacheEntry(workspaceId, requestedSnapshotId, today, groups));
            _logger.LogInformation(
                "Stage 10 component orders service completed. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} Outcome={Outcome} GroupCount={GroupCount} ElapsedMs={ElapsedMs}",
                workspaceId, requestedSnapshotId.Value, ComponentOrdersOutcomeKind.Loaded, groups.Count, stopwatch.ElapsedMilliseconds);
            return ComponentOrdersResult.Loaded(requestedSnapshotId, groups, enrichmentAvailability);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Stage 10 component orders service completed. WorkspaceId={WorkspaceId} Site={Site} SnapshotId={SnapshotId} Outcome={Outcome} ElapsedMs={ElapsedMs}",
                workspaceId, workspace.Site, requestedSnapshotId.Value, ComponentOrdersOutcomeKind.Unavailable, stopwatch.ElapsedMilliseconds);
            return ComponentOrdersResult.Unavailable;
        }
    }

    private async Task<IReadOnlyList<ComponentOrderLine>> EnrichAsync(
        string site,
        IReadOnlyList<ComponentOrderLine> lines,
        CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
            return lines;

        var request = new ComponentOrderEnrichmentRequest(
            site,
            DistinctExact(lines.Select(line => line.ComponentPart)),
            DistinctExact(lines.Select(line => line.SupplierIdentifier)));
        var enrichment = await _enrichment.ReadAsync(request, cancellationToken);
        return lines.Select(line =>
        {
            bool? isCreditHold = null;
            bool? isCia = null;
            if (line.SupplierIdentifier is not null
                && enrichment.RisksBySupplier.TryGetValue(line.SupplierIdentifier, out var risk))
            {
                isCreditHold = risk.IsCreditHold;
                isCia = risk.IsCia;
            }
            return line with
            {
                IsCreditHold = isCreditHold,
                IsCia = isCia,
                CurrentComments = enrichment.CommentsByComponent.TryGetValue(line.ComponentPart, out var comment) ? comment : null
            };
        }).ToList();
    }

    private static IReadOnlyList<string> DistinctExact(IEnumerable<string?> values)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
            if (value is not null && seen.Add(value)) result.Add(value);
        return result;
    }

        /// <summary>
    /// Derives the workspace component scope: every resolved MPS parent's current-effective
    /// multi-level BOM at the business date, with component parts deduplicated case-insensitively
    /// (first occurrence kept, in resolved-parent order — independent of which parent's read
    /// completes first). All distinct components are included — the accepted scope text has no P/M
    /// or phantom filter. A BOM read failure propagates to the caller's Unavailable handling; it is
    /// never swallowed into a partial component set.
    ///
    /// Reads run with bounded concurrency (<see cref="BomReadConcurrency"/>) rather than one at a
    /// time: this is the first real bulk use of <see cref="IBomSourceReader"/> (previously only one
    /// selected parent was read at a time), and a workspace can resolve to dozens or hundreds of MPS
    /// parents. Reading them fully sequentially — each read opening its own QAD connection — was
    /// observed to take minutes and could exhaust the caller's patience or a downstream timeout
    /// before completing. Bounded parallelism keeps wall-clock time proportional to the slowest
    /// batch rather than the sum of every parent's read, without materially increasing peak QAD load.
    /// </summary>
    private const int BomReadConcurrency = 8;

    private async Task<IReadOnlyList<string>> ResolveComponentPartsAsync(
        Guid workspaceId,
        SnapshotId snapshotId,
        string site,
        IReadOnlyList<MpsResolvedPart> resolvedParts,
        DateOnly today,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var occurrencesByParent = new IReadOnlyList<BomOccurrence>[resolvedParts.Count];

        using var gate = new SemaphoreSlim(BomReadConcurrency);
        var reads = new Task[resolvedParts.Count];
        for (var i = 0; i < resolvedParts.Count; i++)
        {
            var index = i;
            var parent = resolvedParts[index];
            reads[index] = ReadOneAsync(index, parent);
        }
        await Task.WhenAll(reads);

        var components = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < resolvedParts.Count; i++)
        {
            foreach (var occurrence in occurrencesByParent[i]!)
            {
                if (seen.Add(occurrence.ComponentPart))
                    components.Add(occurrence.ComponentPart);
            }
        }

        return components;

        async Task ReadOneAsync(int index, MpsResolvedPart parent)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation(
                    "Stage 10 component orders BOM read started. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} ParentIndex={ParentIndex} ParentCount={ParentCount} ElapsedMs={ElapsedMs}",
                    workspaceId, snapshotId.Value, index + 1, resolvedParts.Count, stopwatch.ElapsedMilliseconds);
                var occurrences = await _bom.ReadAsync(site, parent.ParentPart, today, cancellationToken);
                _logger.LogInformation(
                    "Stage 10 component orders BOM read completed. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} ParentIndex={ParentIndex} ParentCount={ParentCount} OccurrenceCount={OccurrenceCount} ElapsedMs={ElapsedMs}",
                    workspaceId, snapshotId.Value, index + 1, resolvedParts.Count, occurrences.Count, stopwatch.ElapsedMilliseconds);
                occurrencesByParent[index] = occurrences;
            }
            finally
            {
                gate.Release();
            }
        }
    }

    private async Task<IReadOnlyList<ComponentOrderLine>> ReadComponentOrdersAsync(
        Guid workspaceId,
        SnapshotId snapshotId,
        string site,
        IReadOnlyList<string> components,
        DateOnly today,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stage 10 component orders PO source phase started. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} ComponentCount={ComponentCount} ElapsedMs={ElapsedMs}",
            workspaceId, snapshotId.Value, components.Count, stopwatch.ElapsedMilliseconds);
        var lines = await _componentOrders.ReadAsync(site, components, today, cancellationToken);
        _logger.LogInformation(
            "Stage 10 component orders PO source phase completed. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} ReturnedRowCount={ReturnedRowCount} ElapsedMs={ElapsedMs}",
            workspaceId, snapshotId.Value, lines.Count, stopwatch.ElapsedMilliseconds);
        return lines;
    }

    private async Task<WorkspaceAssignment> FindWorkspaceAsync(Guid workspaceId)
    {
        var workspaces = await _workspaces.GetWorkspacesAsync();
        return workspaces.Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new ComponentOrdersWorkspaceNotFoundException(workspaceId);
    }
}

using Kst.Application.Bom;
using Kst.Application.ComponentOrders;
using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;
using Kst.Domain.Mps;
using Microsoft.Extensions.Logging;

namespace Kst.Application.LongTermShortages;

public sealed class LongTermShortagesService(
    IWorkspaceConfigurationService workspaces,
    IMpsSnapshotStore snapshots,
    IBomSourceReader bom,
    ILongTermShortageSourceReader source,
    ILongTermShortagesCacheStore cache,
    IComponentOrderSourceReader componentOrders,
    IComponentOrderEnrichmentReader enrichment,
    IClock clock,
    ILogger<LongTermShortagesService> logger)
{
    public const string ScheduleVersion = "component-as-of-uom-v4";
    /// <summary>Selects complete cached detail only. No acquisition or projection is permitted here.</summary>
    public async Task<LongTermShortagesExportResult> GetProjectionDetailAsync(Guid workspaceId,
        SnapshotId snapshotId, string componentPart, LongTermShortagePopulationOptions options)
    {
        var selected = await GetCachedForExportAsync(workspaceId, snapshotId, [componentPart],
            options with { ShowAll = true, IncludeUnconfirmed = false });
        if (selected.Kind != LongTermShortagesOutcomeKind.Loaded) return selected;
        // Both modes and all retained facts must still belong to the exact cache entry.
        var entry = cache.Get(workspaceId, snapshotId, selected.RefreshDate!.Value, ScheduleVersion,
            options with { ShowAll = false, IncludeUnconfirmed = false });
        var row = selected.Rows is { Count: 1 } ? selected.Rows[0] : null;
        var all = entry?.AllReceiptsRows.FirstOrDefault(r => string.Equals(r.ComponentPart, componentPart, StringComparison.OrdinalIgnoreCase));
        var input = entry?.Inputs.FirstOrDefault(r => string.Equals(r.ComponentPart, componentPart, StringComparison.OrdinalIgnoreCase));
        if (entry is null || row is null || all is null || row.Weeks.Count != options.HorizonWeeks
            || all.Weeks.Count != options.HorizonWeeks || row.Past is null || row.DemandParentParts is null
            || input is null || row.Evidence.Count != input.Evidence.Count || all.Evidence.Count != input.Evidence.Count
            || !entry.Rows.Any(r => ReferenceEquals(r, row)) || entry.AcquiredAtUtc != selected.AcquiredAtUtc)
            return new(LongTermShortagesOutcomeKind.Unavailable);
        return snapshots.GetState(workspaceId).Snapshot?.Id == snapshotId
            ? selected : new(LongTermShortagesOutcomeKind.SnapshotChanged);
    }
    /// <summary>Reads one selected component's conventional open PO lines and current comment.
    /// The selected component must already belong to the current cached shortage projection;
    /// this path never reacquires MRP or expands the workspace BOM.</summary>
    public async Task<LongTermShortagePurchasingResult> GetPurchasingAsync(Guid workspaceId, SnapshotId snapshotId,
        string componentPart, LongTermShortagePopulationOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(componentPart) || options.HorizonWeeks is not (13 or 26 or 52 or 72))
            return new(LongTermShortagesOutcomeKind.Unavailable);
        var selected = await GetCachedForExportAsync(workspaceId, snapshotId, [componentPart], options with { ShowAll = true });
        if (selected.Kind != LongTermShortagesOutcomeKind.Loaded) return new(selected.Kind);
        if (selected.Rows is not { Count: 1 }) return new(LongTermShortagesOutcomeKind.Unavailable);
        var row = selected.Rows[0];

        var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new LongTermShortagesWorkspaceNotFoundException(workspaceId);
        var today = DateOnly.FromDateTime(clock.LocalNow.Date);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var lines = await componentOrders.ReadAsync(workspace.Site, [row.ComponentPart], today, cancellationToken);
            if (lines.Any(line => !string.Equals(line.ComponentPart, row.ComponentPart, StringComparison.OrdinalIgnoreCase)
                || line.OpenQuantity <= 0))
                throw new InvalidOperationException("Selected-component purchase order result was outside scope.");
            if (snapshots.GetState(workspaceId).Snapshot?.Id != snapshotId)
                return new(LongTermShortagesOutcomeKind.SnapshotChanged);
            try
            {
                // The comment is site/component-scoped, independent of whether a conventional PO exists.
                var facts = await enrichment.ReadAsync(new ComponentOrderEnrichmentRequest(workspace.Site, [row.ComponentPart], []), cancellationToken);
                if (snapshots.GetState(workspaceId).Snapshot?.Id != snapshotId)
                    return new(LongTermShortagesOutcomeKind.SnapshotChanged);
                return new(LongTermShortagesOutcomeKind.Loaded, lines, true,
                    facts.CommentsByComponent.TryGetValue(row.ComponentPart, out var comment) ? comment : null);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                if (snapshots.GetState(workspaceId).Snapshot?.Id != snapshotId)
                    return new(LongTermShortagesOutcomeKind.SnapshotChanged);
                logger.LogWarning("Buyer comment unavailable for selected workspace component. FailureType={FailureType}", ex.GetType().Name);
                return new(LongTermShortagesOutcomeKind.Loaded, lines, false);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            if (snapshots.GetState(workspaceId).Snapshot?.Id != snapshotId)
                return new(LongTermShortagesOutcomeKind.SnapshotChanged);
            logger.LogWarning("Open purchase orders unavailable for selected workspace component. FailureType={FailureType}", ex.GetType().Name);
            return new(LongTermShortagesOutcomeKind.Unavailable);
        }
    }
    public async Task<LongTermShortagesResult> GetAsync(Guid workspaceId, SnapshotId requestedSnapshotId, LongTermShortagePopulationOptions options, CancellationToken cancellationToken = default)
    {
        if (options.HorizonWeeks is not (13 or 26 or 52 or 72)) throw new ArgumentOutOfRangeException(nameof(options));
        var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new LongTermShortagesWorkspaceNotFoundException(workspaceId);
        var state = snapshots.GetState(workspaceId);
        if (state.Snapshot is null) return LongTermShortagesResult.MpsNotLoaded;
        if (state.Snapshot.Id != requestedSnapshotId) return LongTermShortagesResult.SnapshotChanged;

        var refreshDate = DateOnly.FromDateTime(clock.LocalNow.Date);
        // Show All is a presentation filter. Cache the complete eligible component population.
        var projectionOptions = options with { ShowAll = false, IncludeUnconfirmed = false };
        var cached = cache.Get(workspaceId, requestedSnapshotId, refreshDate, ScheduleVersion, projectionOptions);
        if (cached is not null)
            return snapshots.GetState(workspaceId).Snapshot?.Id == requestedSnapshotId
                ? LongTermShortagesResult.Loaded(cached, options.IncludeUnconfirmed)
                : LongTermShortagesResult.SnapshotChanged;

        try
        {
            var reusable = cache.GetReusableSource(workspaceId, requestedSnapshotId, refreshDate, ScheduleVersion, projectionOptions);
            LongTermShortageAcquisition acquisition;
            if (reusable is not null)
            {
                acquisition = new LongTermShortageAcquisition(reusable.AcquiredAtUtc, reusable.Inputs);
            }
            else
            {
                var componentParents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var parent in state.Snapshot.ResolvedParts)
                    foreach (var occurrence in await bom.ReadAsync(workspace.Site, parent.ParentPart, refreshDate, cancellationToken))
                    {
                        if (occurrence.IsPhantom || !IsEligible(occurrence, options)) continue;
                        if (!componentParents.TryGetValue(occurrence.ComponentPart, out var parents))
                        {
                            parents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            componentParents.Add(occurrence.ComponentPart, parents);
                        }
                        parents.Add(parent.ParentPart);
                    }

                acquisition = await source.ReadAsync(workspace.Site,
                    componentParents.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<string>)entry.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.OrdinalIgnoreCase),
                    refreshDate, LongTermShortagesBuilder.GetWeekOneStart(refreshDate).AddDays(options.HorizonWeeks * 7), cancellationToken);
                if (acquisition.Inputs.Count != componentParents.Count || acquisition.Inputs.Select(input => input.ComponentPart).ToHashSet(StringComparer.OrdinalIgnoreCase).Count != componentParents.Count || acquisition.Inputs.Any(input => !componentParents.ContainsKey(input.ComponentPart)))
                    throw new InvalidOperationException("Component source read was incomplete or ambiguous.");
            }
            if (snapshots.GetState(workspaceId).Snapshot?.Id != requestedSnapshotId) return LongTermShortagesResult.SnapshotChanged;
            var (rows, allReceiptsRows) = LongTermShortagesBuilder.BuildBoth(refreshDate, acquisition.Inputs, options.HorizonWeeks);
            var entry = new LongTermShortagesCacheEntry(workspaceId, requestedSnapshotId, refreshDate, ScheduleVersion, projectionOptions, rows, acquisition.AcquiredAtUtc, acquisition.Inputs, allReceiptsRows);
            cache.Set(entry);
            return LongTermShortagesResult.Loaded(entry, options.IncludeUnconfirmed);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stage 11 Component MRP load failed for workspace {WorkspaceId}.", workspaceId);
            if (snapshots.GetState(workspaceId).Snapshot?.Id != requestedSnapshotId) return LongTermShortagesResult.SnapshotChanged;
            // A failed source read cannot authorize a current Healthy classification.
            var detail = ex switch
            {
                TimeoutException => "QADPro2 reporting query timed out. Retry the report.",
                InvalidOperationException invalid when invalid.Message.Contains("incomplete", StringComparison.OrdinalIgnoreCase) => "QADPro2 returned incomplete component results. Retry the report.",
                InvalidOperationException invalid when invalid.Message.Contains("prrowid", StringComparison.OrdinalIgnoreCase) => "QADPro2 returned invalid MRP row identity. Retry the report.",
                InvalidOperationException invalid when invalid.Message.Contains("confirmation", StringComparison.OrdinalIgnoreCase) => "QADPro2 PO receipt confirmation is unavailable. Retry the report.",
                _ => "Component MRP source acquisition failed. Check the QADPro2 connection and retry."
            };
            return LongTermShortagesResult.Unavailable with { FailureDetail = detail };
        }
    }

    /// <summary>Returns a cached projection only. Exporting must never trigger a source refresh.</summary>
    public async Task<LongTermShortagesExportResult> GetCachedForExportAsync(
        Guid workspaceId,
        SnapshotId requestedSnapshotId,
        IReadOnlyCollection<string> displayedComponentParts,
        LongTermShortagePopulationOptions options)
    {
        var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new LongTermShortagesWorkspaceNotFoundException(workspaceId);
        var state = snapshots.GetState(workspaceId);
        if (state.Snapshot is null) return new(LongTermShortagesOutcomeKind.MpsNotLoaded);
        if (state.Snapshot.Id != requestedSnapshotId) return new(LongTermShortagesOutcomeKind.SnapshotChanged);

        // The export must match the projection produced under exactly these population options.
        var cached = cache.Get(workspaceId, requestedSnapshotId, DateOnly.FromDateTime(clock.LocalNow.Date), ScheduleVersion, options with { ShowAll = false, IncludeUnconfirmed = false });
        if (cached is null) return new(LongTermShortagesOutcomeKind.Unavailable);

        if (displayedComponentParts.Count == 0)
            return new(LongTermShortagesOutcomeKind.Loaded, workspace.DisplayName, cached.RefreshDate, [], cached.AcquiredAtUtc, LongTermShortageAcquisition.ConsistencyMode);

        var selectedRows = options.IncludeUnconfirmed ? cached.AllReceiptsRows : cached.Rows;
        var cachedParts = selectedRows.Select(row => row.ComponentPart).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (displayedComponentParts.Any(part => string.IsNullOrWhiteSpace(part) || !cachedParts.Contains(part)) ||
            !options.ShowAll && displayedComponentParts.Any(part => !selectedRows.First(row => string.Equals(row.ComponentPart, part, StringComparison.OrdinalIgnoreCase)).HasShortage))
            return new(LongTermShortagesOutcomeKind.Unavailable);

        var requested = displayedComponentParts.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = selectedRows.Where(row => requested.Contains(row.ComponentPart)).ToList();
        return new(LongTermShortagesOutcomeKind.Loaded, workspace.DisplayName, cached.RefreshDate, rows, cached.AcquiredAtUtc, LongTermShortageAcquisition.ConsistencyMode);
    }

    /// <summary>Effective selected-site P/M classification is resolved by the BOM reader.</summary>
    private static bool IsEligible(BomOccurrence occurrence, LongTermShortagePopulationOptions options) =>
        occurrence.PmCode?.Trim().ToUpperInvariant() switch
        {
            "P" => true,
            "M" => options.IncludeManufacturedParts,
            _ => false
        };
}

public sealed class LongTermShortagesWorkspaceNotFoundException(Guid workspaceId) : Exception($"Workspace {workspaceId} was not found.");

public sealed record LongTermShortagePurchasingResult(LongTermShortagesOutcomeKind Kind,
    IReadOnlyList<Kst.Domain.ComponentOrders.ComponentOrderLine>? Lines = null,
    bool CommentAvailable = false, string? CurrentComment = null);

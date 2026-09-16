using Kst.Application.Bom;
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
    IClock clock,
    ILogger<LongTermShortagesService> logger)
{
    public async Task<LongTermShortagesResult> GetAsync(Guid workspaceId, SnapshotId requestedSnapshotId, LongTermShortagePopulationOptions options, CancellationToken cancellationToken = default)
    {
        var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new LongTermShortagesWorkspaceNotFoundException(workspaceId);
        var state = snapshots.GetState(workspaceId);
        if (state.Snapshot is null) return LongTermShortagesResult.MpsNotLoaded;
        if (state.Snapshot.Id != requestedSnapshotId) return LongTermShortagesResult.SnapshotChanged;

        var refreshDate = DateOnly.FromDateTime(clock.LocalNow.Date);
        var cached = cache.Get(workspaceId, requestedSnapshotId, refreshDate, options);
        if (cached is not null) return LongTermShortagesResult.Loaded(requestedSnapshotId, refreshDate, cached.Rows);

        try
        {
            var parentParts = state.Snapshot.ResolvedParts.Select(p => p.ParentPart).ToHashSet(StringComparer.OrdinalIgnoreCase);
            // Population options apply after resolved-parent/current-effective-BOM component selection and before any
            // opening-QOH, demand, supply, forecast, KSS, presentation, or projection retrieval: excluded components
            // never reach the source read. Classification uses the established part-master indicators carried by the
            // BOM occurrence (pt_mstr.pt_pm_code = 'M' manufactured; pt_phantom phantom, null = not phantom).
            var componentParents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var parent in state.Snapshot.ResolvedParts)
                foreach (var occurrence in await bom.ReadAsync(workspace.Site, parent.ParentPart, refreshDate, cancellationToken))
                {
                    if (!options.IsIncluded(IsManufactured(occurrence), occurrence.IsPhantom)) continue;
                    if (!componentParents.TryGetValue(occurrence.ComponentPart, out var parents))
                    {
                        parents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        componentParents.Add(occurrence.ComponentPart, parents);
                    }
                    parents.Add(parent.ParentPart);
                }

            var weekOne = MpsBusinessCalendar.GetBusinessWeekStart(refreshDate);
            var inputs = await source.ReadAsync(workspace.Site,
                componentParents.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<string>)entry.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.OrdinalIgnoreCase),
                refreshDate, weekOne.AddDays(LongTermShortagesBuilder.WeekCount * 7), parentParts, cancellationToken);
            if (snapshots.GetState(workspaceId).Snapshot?.Id != requestedSnapshotId) return LongTermShortagesResult.SnapshotChanged;
            var rows = LongTermShortagesBuilder.Build(refreshDate, inputs);
            cache.Set(new LongTermShortagesCacheEntry(workspaceId, requestedSnapshotId, refreshDate, options, rows));
            return LongTermShortagesResult.Loaded(requestedSnapshotId, refreshDate, rows);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stage 11-A Long-Term Shortages load failed for workspace {WorkspaceId}.", workspaceId);
            var stale = cache.GetLatest(workspaceId, requestedSnapshotId, options);
            return stale is null ? LongTermShortagesResult.Unavailable : LongTermShortagesResult.Loaded(requestedSnapshotId, stale.RefreshDate, stale.Rows, true);
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
        var cached = cache.GetLatest(workspaceId, requestedSnapshotId, options);
        if (cached is null) return new(LongTermShortagesOutcomeKind.Unavailable);

        if (displayedComponentParts.Count == 0)
            return new(LongTermShortagesOutcomeKind.Loaded, workspace.DisplayName, cached.RefreshDate, []);

        var cachedParts = cached.Rows.Select(row => row.ComponentPart).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (displayedComponentParts.Any(part => string.IsNullOrWhiteSpace(part) || !cachedParts.Contains(part)))
            return new(LongTermShortagesOutcomeKind.Unavailable);

        var requested = displayedComponentParts.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = cached.Rows.Where(row => requested.Contains(row.ComponentPart)).ToList();
        return new(LongTermShortagesOutcomeKind.Loaded, workspace.DisplayName, cached.RefreshDate, rows);
    }

    /// <summary>Established part-master manufactured indicator: trimmed, case-insensitive <c>pt_pm_code = 'M'</c>.</summary>
    private static bool IsManufactured(BomOccurrence occurrence) =>
        string.Equals(occurrence.MasterPmCode?.Trim(), "M", StringComparison.OrdinalIgnoreCase);
}

public sealed class LongTermShortagesWorkspaceNotFoundException(Guid workspaceId) : Exception($"Workspace {workspaceId} was not found.");

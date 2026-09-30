using System.Collections.Concurrent;
using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Kst.Application.OpenOrders;

/// <summary>Reads only the already-loaded MPS scope. Refresh never substitutes another population.</summary>
public sealed class OpenOrdersService(
    IWorkspaceConfigurationService workspaces, IMpsSnapshotStore mps,
    IOpenOrdersSourceReader reader, IOpenOrdersSnapshotStore store, IClock clock,
    ILogger<OpenOrdersService> logger)
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();
    private const string StaleWarning = "Showing the last known Open Orders report. A newer refresh could not be completed. This report cannot be used for operational validation.";

    public async Task<OpenOrdersResult> GetAsync(Guid assignmentId, SnapshotId mpsSnapshotId, CancellationToken ct) =>
        await ResolveAsync(assignmentId, mpsSnapshotId, false, ct);

    public async Task<OpenOrdersResult> RefreshAsync(Guid assignmentId, SnapshotId mpsSnapshotId, CancellationToken ct) =>
        await ResolveAsync(assignmentId, mpsSnapshotId, true, ct);

    private async Task<OpenOrdersResult> ResolveAsync(Guid id, SnapshotId expected, bool refresh, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var assignments = await workspaces.GetWorkspacesAsync();
        var workspace = assignments.Workspaces.FirstOrDefault(w => w.AssignmentId == id);
        if (workspace is null) return new(OpenOrdersOutcomeKind.UnknownWorkspace);
        var state = mps.GetState(id);
        if (state.Snapshot is null) return new(OpenOrdersOutcomeKind.MpsNotLoaded);
        if (state.Snapshot.Id != expected || state.Snapshot.Site != workspace.Site)
            return new(OpenOrdersOutcomeKind.MpsSnapshotChanged);

        var cached = store.Get(id);
        if (!refresh && cached is not null && cached.Snapshot.MpsSnapshotId == expected && cached.Snapshot.Site == workspace.Site)
        {
            var latest = mps.GetState(id).Snapshot;
            if (latest is null || latest.Id != expected || latest.Site != workspace.Site)
                return new(OpenOrdersOutcomeKind.MpsSnapshotChanged);
            return OpenOrdersResult.Loaded(cached);
        }

        var gate = _gates.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            state = mps.GetState(id);
            if (state.Snapshot is null) return new(OpenOrdersOutcomeKind.MpsNotLoaded);
            if (state.Snapshot.Id != expected || state.Snapshot.Site != workspace.Site)
                return new(OpenOrdersOutcomeKind.MpsSnapshotChanged);
            cached = store.Get(id);
            if (!refresh && cached is not null && cached.Snapshot.MpsSnapshotId == expected && cached.Snapshot.Site == workspace.Site)
                return OpenOrdersResult.Loaded(cached);

            try
            {
                var parents = state.Snapshot.ResolvedParts.Select(p => p.ParentPart).ToArray();
                IReadOnlyList<Kst.Domain.OpenOrders.OpenOrderLine> lines = parents.Length == 0
                    ? [] : await reader.ReadAsync(workspace.Site, parents, ct);
                ct.ThrowIfCancellationRequested();
                var latest = mps.GetState(id).Snapshot;
                if (latest is null || latest.Id != expected || latest.Site != workspace.Site)
                    return new(OpenOrdersOutcomeKind.MpsSnapshotChanged);
                var snapshot = new OpenOrdersSnapshot(SnapshotId.New(), expected, clock.UtcNow, id, workspace.Site, lines);
                var report = new OpenOrdersReport(snapshot, false, null);
                store.Set(id, report);
                return OpenOrdersResult.Loaded(report);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning("Open Orders source read failed for workspace {WorkspaceId}: {ExceptionType}", id, ex.GetType().Name);
                var latest = mps.GetState(id).Snapshot;
                if (latest is null || latest.Id != expected || latest.Site != workspace.Site)
                    return new(OpenOrdersOutcomeKind.MpsSnapshotChanged);
                if (cached is not null && cached.Snapshot.MpsSnapshotId == expected && cached.Snapshot.Site == workspace.Site)
                {
                    var stale = cached with { IsStale = true, Warning = StaleWarning };
                    store.Set(id, stale);
                    return OpenOrdersResult.Loaded(stale);
                }
                return new(OpenOrdersOutcomeKind.Unavailable);
            }
        }
        finally { gate.Release(); }
    }
}

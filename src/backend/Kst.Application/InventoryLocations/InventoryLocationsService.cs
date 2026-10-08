using System.Collections.Concurrent;
using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Kst.Application.InventoryLocations;

/// <summary>Independent fresh physical-detail acquisition; never changes summary or shortage data.</summary>
public sealed class InventoryLocationsService(
    IWorkspaceConfigurationService workspaces, IMpsSnapshotStore snapshots,
    IInventoryLocationReader reader, IInventoryLocationsStore store, IClock clock,
    ILogger<InventoryLocationsService> logger)
{
    private readonly ConcurrentDictionary<(Guid, string), SemaphoreSlim> _gates = new();

    public async Task<InventoryLocationsResult> GetAsync(Guid workspaceId, string componentPart, string mpsSnapshotId, CancellationToken ct)
    {
        var part = componentPart.Trim();
        var gate = _gates.GetOrAdd((workspaceId, part.ToUpperInvariant()), _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId);
            if (workspace is null) return new(InventoryLocationsOutcome.WorkspaceMissing);
            var site = workspace.Site;
            if (!MatchesSnapshot(workspaceId, site, mpsSnapshotId)) return new(InventoryLocationsOutcome.SnapshotChanged);
            var cached = store.Get(workspaceId, part);
            InventoryLocationsReport? report = null;
            try
            {
                var rows = await reader.ReadAsync(site, part, ct);
                ct.ThrowIfCancellationRequested();
                report = new(workspaceId, site, part, mpsSnapshotId, clock.UtcNow, false, null, rows.ToArray());
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                // Do not log source exception text (it may contain connection or source details).
                logger.LogWarning("Inventory locations acquisition failed for workspace {WorkspaceId}.", workspaceId);
            }

            workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId);
            ct.ThrowIfCancellationRequested();
            if (workspace is null) return new(InventoryLocationsOutcome.WorkspaceMissing);
            if (!string.Equals(workspace.Site, site, StringComparison.OrdinalIgnoreCase) || !MatchesSnapshot(workspaceId, site, mpsSnapshotId))
                return new(InventoryLocationsOutcome.SnapshotChanged);
            if (report is not null)
            {
                store.Set(report);
                return new(InventoryLocationsOutcome.Loaded, report);
            }
            if (cached is not null && string.Equals(cached.Site, site, StringComparison.OrdinalIgnoreCase) && cached.MpsSnapshotId == mpsSnapshotId)
                return new(InventoryLocationsOutcome.Loaded, cached with
                {
                    IsStale = true,
                    Warning = "Showing the last known inventory locations. A newer refresh could not be completed."
                });
            return new(InventoryLocationsOutcome.Unavailable);
        }
        finally { gate.Release(); }
    }

    private bool MatchesSnapshot(Guid workspaceId, string site, string snapshotId)
    {
        var snapshot = snapshots.GetState(workspaceId).Snapshot;
        return snapshot is not null && snapshot.Id.ToString() == snapshotId && string.Equals(snapshot.Site, site, StringComparison.OrdinalIgnoreCase);
    }
}

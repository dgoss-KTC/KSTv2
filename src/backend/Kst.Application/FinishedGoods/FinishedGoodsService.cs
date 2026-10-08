using System.Collections.Concurrent;
using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.FinishedGoods;
using Microsoft.Extensions.Logging;

namespace Kst.Application.FinishedGoods;

public sealed class FinishedGoodsService(
    IWorkspaceConfigurationService workspaces, IMpsSnapshotStore mps,
    IFinishedGoodsSourceReader reader, IFinishedGoodsSnapshotStore store, IClock clock,
    ILogger<FinishedGoodsService> logger)
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();
    private const string StaleWarning = "Showing the last known Finished Goods inventory. Refresh failed; these quantities may have changed. Refresh to try again.";

    public Task<FinishedGoodsResult> GetAsync(Guid id, SnapshotId expected, CancellationToken ct) => ResolveAsync(id, expected, false, ct);
    public Task<FinishedGoodsResult> RefreshAsync(Guid id, SnapshotId expected, CancellationToken ct) => ResolveAsync(id, expected, true, ct);

    private async Task<FinishedGoodsResult> ResolveAsync(Guid id, SnapshotId expected, bool refresh, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == id);
        if (workspace is null) return new(FinishedGoodsOutcomeKind.UnknownWorkspace);
        if (await CheckContextAsync(id, expected, workspace.Site, ct) is { } initial) return new(initial);
        var gate = _gates.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (await CheckContextAsync(id, expected, workspace.Site, ct) is { } changed) return new(changed);
            var cached = store.Get(id);
            if (!refresh && Compatible(cached, id, expected, workspace.Site))
            {
                if (await CheckContextAsync(id, expected, workspace.Site, ct) is { } moved) return new(moved);
                return new(FinishedGoodsOutcomeKind.Loaded, cached);
            }
            try
            {
                var parts = mps.GetState(id).Snapshot!.ResolvedParts.Select(p => p.ParentPart).ToArray();
                IReadOnlyList<FinishedGoodsSourceRow> source = parts.Length == 0 ? [] : await reader.ReadAsync(workspace.Site, parts, ct);
                ct.ThrowIfCancellationRequested();
                if (source is null || source.Any(r => r is null || r.Site != workspace.Site || r.OnHand <= 0 ||
                    string.IsNullOrWhiteSpace(r.Domain) || !parts.Contains(r.Product, StringComparer.OrdinalIgnoreCase) ||
                    (r.IsMissingPrice && r.UnitPrice != 0)))
                    throw FinishedGoodsFailure.Create(FinishedGoodsFailureReason.ApplicationSourceInvalid);
                var snapshotId = SnapshotId.New();
                var rows = source.Select((r, index) => new FinishedGoodsPosition($"{snapshotId}:{index}", r)).ToArray();
                var report = new FinishedGoodsReport(new(snapshotId, expected, clock.UtcNow, id, workspace.Site, rows), false, null);
                if (await CheckContextAsync(id, expected, workspace.Site, ct) is { } moved) return new(moved);
                store.Set(id, report);
                return new(FinishedGoodsOutcomeKind.Loaded, report);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning("Finished Goods acquisition failed for workspace {WorkspaceId}: {ExceptionType} Reason={FailureReason} TraceId={TraceId}",
                    id, ex.GetType().Name, FinishedGoodsFailure.Reason(ex), System.Diagnostics.Activity.Current?.TraceId.ToString());
                if (await CheckContextAsync(id, expected, workspace.Site, ct) is { } moved) return new(moved);
                if (!Compatible(cached, id, expected, workspace.Site)) return new(FinishedGoodsOutcomeKind.Unavailable);
                var stale = cached! with { IsStale = true, Warning = StaleWarning };
                store.Set(id, stale);
                return new(FinishedGoodsOutcomeKind.Loaded, stale);
            }
        }
        finally { gate.Release(); }
    }

    private async Task<FinishedGoodsOutcomeKind?> CheckContextAsync(Guid id, SnapshotId expected, string site, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var current = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == id);
        ct.ThrowIfCancellationRequested();
        if (current is null) return FinishedGoodsOutcomeKind.UnknownWorkspace;
        if (current.Site != site) return FinishedGoodsOutcomeKind.MpsSnapshotChanged;
        var snapshot = mps.GetState(id).Snapshot;
        if (snapshot is null) return FinishedGoodsOutcomeKind.MpsNotLoaded;
        return snapshot.Id != expected || snapshot.Site != site ? FinishedGoodsOutcomeKind.MpsSnapshotChanged : null;
    }

    private static bool Compatible(FinishedGoodsReport? report, Guid id, SnapshotId expected, string site) =>
        report is not null && report.Snapshot.WorkspaceId == id && report.Snapshot.MpsSnapshotId == expected && report.Snapshot.Site == site;
}

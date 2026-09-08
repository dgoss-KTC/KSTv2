using System.Collections.Concurrent;
using Kst.Application.Shortages;
using Kst.Domain.Common;
using Kst.Domain.Mps;

namespace Kst.Infrastructure.Shortages;

/// <summary>Snapshot- and business-date-keyed Stage 9 analysis cache. No stale fallback or persistence.</summary>
public sealed class InMemoryWorkOrderImmediateMaterialCacheStore : IWorkOrderImmediateMaterialCacheStore
{
    private readonly ConcurrentDictionary<(Guid, SnapshotId, string, MpsDateBasis, DateOnly), WorkOrderImmediateMaterialCacheEntry> _entries = new();
    private readonly ConcurrentDictionary<(Guid, SnapshotId, string, MpsDateBasis, DateOnly), WorkOrderImmediateMaterialBatchCacheEntry> _batches = new();

    public WorkOrderImmediateMaterialCacheEntry? Get(Guid workspaceId, SnapshotId snapshotId, string woid, MpsDateBasis dateBasis, DateOnly businessDate) =>
        _entries.TryGetValue((workspaceId, snapshotId, Key(woid), dateBasis, businessDate), out var entry) ? entry : null;

    public void Set(WorkOrderImmediateMaterialCacheEntry entry) =>
        _entries[(entry.WorkspaceId, entry.SnapshotId, Key(entry.Woid), entry.DateBasis, entry.BusinessDate)] = entry;

    public WorkOrderImmediateMaterialBatchCacheEntry? GetBatch(Guid workspaceId, SnapshotId snapshotId, string buildPart, MpsDateBasis dateBasis, DateOnly businessDate) =>
        _batches.TryGetValue((workspaceId, snapshotId, Key(buildPart), dateBasis, businessDate), out var entry) ? entry : null;

    public void SetBatch(WorkOrderImmediateMaterialBatchCacheEntry entry) =>
        _batches[(entry.WorkspaceId, entry.SnapshotId, Key(entry.BuildPart), entry.DateBasis, entry.BusinessDate)] = entry;

    private static string Key(string woid) => woid.Trim().ToUpperInvariant();
}

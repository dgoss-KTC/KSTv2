using System.Collections.Concurrent;
using Kst.Application.WorkOrders;
using Kst.Domain.Common;

namespace Kst.Infrastructure.WorkOrders;

/// <summary>
/// Thread-safe in-memory <see cref="IWorkOrderMaterialCacheStore"/>. No persistence across process
/// restart. Mirrors <c>InMemoryPartDetailCacheStore</c>.
/// </summary>
public sealed class InMemoryWorkOrderMaterialCacheStore : IWorkOrderMaterialCacheStore
{
    private readonly ConcurrentDictionary<(Guid WorkspaceId, SnapshotId MpsSnapshotId, string Woid), WorkOrderMaterialCacheEntry> _entries = new();

    public WorkOrderMaterialCacheEntry? Get(Guid workspaceId, SnapshotId mpsSnapshotId, string woid) =>
        _entries.TryGetValue((workspaceId, mpsSnapshotId, Key(woid)), out var entry) ? entry : null;

    public void Set(Guid workspaceId, SnapshotId mpsSnapshotId, string woid, WorkOrderMaterialCacheEntry entry) =>
        _entries[(workspaceId, mpsSnapshotId, Key(woid))] = entry;

    private static string Key(string woid) => woid.Trim().ToUpperInvariant();
}

using System.Collections.Concurrent;
using Kst.Application.ComponentOrders;
using Kst.Domain.Common;

namespace Kst.Infrastructure.ComponentOrders;

/// <summary>
/// Thread-safe in-memory <see cref="IComponentOrdersCacheStore"/>. No persistence across process
/// restart. Mirrors <c>InMemoryWorkOrderPlanningWindowCacheStore</c>.
/// </summary>
public sealed class InMemoryComponentOrdersCacheStore : IComponentOrdersCacheStore
{
    private readonly ConcurrentDictionary<
        (Guid WorkspaceId, SnapshotId MpsSnapshotId, DateOnly BusinessDate),
        ComponentOrdersCacheEntry> _entries = new();

    public ComponentOrdersCacheEntry? Get(Guid workspaceId, SnapshotId mpsSnapshotId, DateOnly businessDate) =>
        _entries.TryGetValue(Key(workspaceId, mpsSnapshotId, businessDate), out var entry)
            ? entry
            : null;

    public void Set(ComponentOrdersCacheEntry entry) =>
        _entries[Key(entry.WorkspaceId, entry.MpsSnapshotId, entry.BusinessDate)] = entry;

    private static (Guid WorkspaceId, SnapshotId MpsSnapshotId, DateOnly BusinessDate) Key(
        Guid workspaceId, SnapshotId mpsSnapshotId, DateOnly businessDate) =>
        (workspaceId, mpsSnapshotId, businessDate);
}

using System.Collections.Concurrent;
using Kst.Application.WorkOrders;
using Kst.Domain.Common;

namespace Kst.Infrastructure.WorkOrders;

/// <summary>
/// Thread-safe in-memory <see cref="IWorkOrderSummaryCacheStore"/>. No persistence across process
/// restart. Mirrors <c>InMemoryPartDetailCacheStore</c>.
/// </summary>
public sealed class InMemoryWorkOrderSummaryCacheStore : IWorkOrderSummaryCacheStore
{
    private readonly ConcurrentDictionary<(Guid WorkspaceId, SnapshotId MpsSnapshotId, string Woid), WorkOrderSummaryCacheEntry> _entries = new();

    public WorkOrderSummaryCacheEntry? Get(Guid workspaceId, SnapshotId mpsSnapshotId, string woid) =>
        _entries.TryGetValue((workspaceId, mpsSnapshotId, Key(woid)), out var entry) ? entry : null;

    public void Set(Guid workspaceId, SnapshotId mpsSnapshotId, string woid, WorkOrderSummaryCacheEntry entry) =>
        _entries[(workspaceId, mpsSnapshotId, Key(woid))] = entry;

    private static string Key(string woid) => woid.Trim().ToUpperInvariant();
}

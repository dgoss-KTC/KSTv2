using System.Collections.Concurrent;
using Kst.Application.WorkOrders;
using Kst.Domain.Common;
using Kst.Domain.Mps;

namespace Kst.Infrastructure.WorkOrders;

/// <summary>
/// Thread-safe in-memory <see cref="IWorkOrderPlanningWindowCacheStore"/>. No persistence across
/// process restart. Mirrors <c>InMemoryWorkOrderSummaryCacheStore</c>.
/// </summary>
public sealed class InMemoryWorkOrderPlanningWindowCacheStore : IWorkOrderPlanningWindowCacheStore
{
    private readonly ConcurrentDictionary<
        (Guid WorkspaceId, SnapshotId MpsSnapshotId, string ParentPart, MpsDateBasis DateBasis, MpsBucketKind? BucketKind, DateOnly? WeekLabel),
        WorkOrderPlanningWindowCacheEntry> _entries = new();

    public WorkOrderPlanningWindowCacheEntry? Get(
        Guid workspaceId,
        SnapshotId mpsSnapshotId,
        string parentPart,
        MpsDateBasis dateBasis,
        MpsBucketKind? bucketKind,
        DateOnly? weekLabel) =>
        _entries.TryGetValue(Key(workspaceId, mpsSnapshotId, parentPart, dateBasis, bucketKind, weekLabel), out var entry)
            ? entry
            : null;

    public void Set(
        Guid workspaceId,
        SnapshotId mpsSnapshotId,
        string parentPart,
        MpsDateBasis dateBasis,
        MpsBucketKind? bucketKind,
        DateOnly? weekLabel,
        WorkOrderPlanningWindowCacheEntry entry) =>
        _entries[Key(workspaceId, mpsSnapshotId, parentPart, dateBasis, bucketKind, weekLabel)] = entry;

    private static (Guid, SnapshotId, string, MpsDateBasis, MpsBucketKind?, DateOnly?) Key(
        Guid workspaceId,
        SnapshotId mpsSnapshotId,
        string parentPart,
        MpsDateBasis dateBasis,
        MpsBucketKind? bucketKind,
        DateOnly? weekLabel) =>
        (workspaceId, mpsSnapshotId, parentPart.Trim().ToUpperInvariant(), dateBasis, bucketKind, weekLabel);
}

using System.Collections.Concurrent;
using Kst.Application.LongTermShortages;
using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;

namespace Kst.Infrastructure.LongTermShortages;

public sealed class InMemoryLongTermShortagesCacheStore : ILongTermShortagesCacheStore
{
    private readonly ConcurrentDictionary<(Guid WorkspaceId, SnapshotId SnapshotId, DateOnly RefreshDate, LongTermShortagePopulationOptions Options), LongTermShortagesCacheEntry> _entries = new();
    public LongTermShortagesCacheEntry? Get(Guid workspaceId, SnapshotId snapshotId, DateOnly refreshDate, LongTermShortagePopulationOptions options) =>
        _entries.TryGetValue((workspaceId, snapshotId, refreshDate, options), out var value) ? value : null;
    public LongTermShortagesCacheEntry? GetLatest(Guid workspaceId, SnapshotId snapshotId, LongTermShortagePopulationOptions options) =>
        _entries.Values.Where(x => x.WorkspaceId == workspaceId && x.SnapshotId == snapshotId && x.Options.Equals(options)).OrderByDescending(x => x.RefreshDate).FirstOrDefault();
    public void Set(LongTermShortagesCacheEntry entry) => _entries[(entry.WorkspaceId, entry.SnapshotId, entry.RefreshDate, entry.Options)] = entry;
}

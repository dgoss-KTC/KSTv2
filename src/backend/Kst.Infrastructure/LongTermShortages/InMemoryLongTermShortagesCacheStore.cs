using System.Collections.Concurrent;
using Kst.Application.LongTermShortages;
using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;

namespace Kst.Infrastructure.LongTermShortages;

public sealed class InMemoryLongTermShortagesCacheStore : ILongTermShortagesCacheStore
{
    private readonly ConcurrentDictionary<(Guid WorkspaceId, SnapshotId SnapshotId, DateOnly RefreshDate, string ScheduleVersion, LongTermShortagePopulationOptions Options), LongTermShortagesCacheEntry> _entries = new();
    public LongTermShortagesCacheEntry? Get(Guid workspaceId, SnapshotId snapshotId, DateOnly refreshDate, string scheduleVersion, LongTermShortagePopulationOptions options) =>
        _entries.TryGetValue((workspaceId, snapshotId, refreshDate, scheduleVersion, options), out var value) ? value : null;
    public LongTermShortagesCacheEntry? GetLatest(Guid workspaceId, SnapshotId snapshotId, string scheduleVersion, LongTermShortagePopulationOptions options) =>
        _entries.Values.Where(x => x.WorkspaceId == workspaceId && x.SnapshotId == snapshotId && x.ScheduleVersion == scheduleVersion && x.Options.Equals(options)).OrderByDescending(x => x.RefreshDate).FirstOrDefault();
    public LongTermShortagesCacheEntry? GetReusableSource(Guid workspaceId, SnapshotId snapshotId, DateOnly refreshDate, string scheduleVersion, LongTermShortagePopulationOptions options) =>
        _entries.Values.Where(x => x.WorkspaceId == workspaceId && x.SnapshotId == snapshotId && x.RefreshDate == refreshDate && x.ScheduleVersion == scheduleVersion
            && x.Options.IncludeManufacturedParts == options.IncludeManufacturedParts && x.Options.IncludePhantoms == options.IncludePhantoms
            && x.Options.HorizonWeeks >= options.HorizonWeeks)
            .OrderBy(x => x.Options.HorizonWeeks).FirstOrDefault();
    public void Set(LongTermShortagesCacheEntry entry) => _entries[(entry.WorkspaceId, entry.SnapshotId, entry.RefreshDate, entry.ScheduleVersion, entry.Options)] = entry;
}

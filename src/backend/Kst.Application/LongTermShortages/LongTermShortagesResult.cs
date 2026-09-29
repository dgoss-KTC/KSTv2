using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;

namespace Kst.Application.LongTermShortages;

public enum LongTermShortagesOutcomeKind { Loaded, MpsNotLoaded, SnapshotChanged, Unavailable }

public sealed record LongTermShortagesResult(
    LongTermShortagesOutcomeKind Kind,
    SnapshotId? SnapshotId = null,
    DateOnly? RefreshDate = null,
    IReadOnlyList<LongTermShortageRow>? Rows = null,
    bool IsStale = false,
    string? Warning = null,
    DateTimeOffset? AcquiredAtUtc = null,
    string? ConsistencyMode = null,
    string? FailureDetail = null,
    IReadOnlyList<LongTermShortageRow>? AllReceiptsRows = null)
{
    public static LongTermShortagesResult Loaded(LongTermShortagesCacheEntry entry, bool includeUnconfirmed = false) =>
        new(LongTermShortagesOutcomeKind.Loaded, entry.SnapshotId, entry.RefreshDate,
            includeUnconfirmed ? entry.AllReceiptsRows : entry.Rows, false, null, entry.AcquiredAtUtc,
            LongTermShortageAcquisition.ConsistencyMode, null, entry.AllReceiptsRows);
    public static readonly LongTermShortagesResult MpsNotLoaded = new(LongTermShortagesOutcomeKind.MpsNotLoaded);
    public static readonly LongTermShortagesResult SnapshotChanged = new(LongTermShortagesOutcomeKind.SnapshotChanged);
    public static readonly LongTermShortagesResult Unavailable = new(LongTermShortagesOutcomeKind.Unavailable);
}

public sealed record LongTermShortagesCacheEntry(Guid WorkspaceId, SnapshotId SnapshotId, DateOnly RefreshDate, string ScheduleVersion, LongTermShortagePopulationOptions Options, IReadOnlyList<LongTermShortageRow> Rows, DateTimeOffset AcquiredAtUtc, IReadOnlyList<LongTermShortageInput> Inputs, IReadOnlyList<LongTermShortageRow> AllReceiptsRows);

/// <summary>
/// Snapshot-aware Stage 11-A projection cache. The population options are part of the result
/// identity: a projection produced under one option set is never served for another.
/// </summary>
public interface ILongTermShortagesCacheStore
{
    LongTermShortagesCacheEntry? Get(Guid workspaceId, SnapshotId snapshotId, DateOnly refreshDate, string scheduleVersion, LongTermShortagePopulationOptions options);
    LongTermShortagesCacheEntry? GetLatest(Guid workspaceId, SnapshotId snapshotId, string scheduleVersion, LongTermShortagePopulationOptions options);
    LongTermShortagesCacheEntry? GetReusableSource(Guid workspaceId, SnapshotId snapshotId, DateOnly refreshDate, string scheduleVersion, LongTermShortagePopulationOptions options);
    void Set(LongTermShortagesCacheEntry entry);
}

public sealed record LongTermShortagesExportResult(
    LongTermShortagesOutcomeKind Kind,
    string? WorkspaceName = null,
    DateOnly? RefreshDate = null,
    IReadOnlyList<LongTermShortageRow>? Rows = null,
    DateTimeOffset? AcquiredAtUtc = null,
    string? ConsistencyMode = null);

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
    string? Warning = null)
{
    public static LongTermShortagesResult Loaded(SnapshotId snapshotId, DateOnly refreshDate, IReadOnlyList<LongTermShortageRow> rows, bool isStale = false) =>
        new(LongTermShortagesOutcomeKind.Loaded, snapshotId, refreshDate, rows, isStale, isStale ? "Showing the last successful Long-Term Shortages refresh." : null);
    public static readonly LongTermShortagesResult MpsNotLoaded = new(LongTermShortagesOutcomeKind.MpsNotLoaded);
    public static readonly LongTermShortagesResult SnapshotChanged = new(LongTermShortagesOutcomeKind.SnapshotChanged);
    public static readonly LongTermShortagesResult Unavailable = new(LongTermShortagesOutcomeKind.Unavailable);
}

public sealed record LongTermShortagesCacheEntry(Guid WorkspaceId, SnapshotId SnapshotId, DateOnly RefreshDate, LongTermShortagePopulationOptions Options, IReadOnlyList<LongTermShortageRow> Rows);

/// <summary>
/// Snapshot-aware Stage 11-A projection cache. The population options are part of the result
/// identity: a projection produced under one option set is never served for another.
/// </summary>
public interface ILongTermShortagesCacheStore
{
    LongTermShortagesCacheEntry? Get(Guid workspaceId, SnapshotId snapshotId, DateOnly refreshDate, LongTermShortagePopulationOptions options);
    LongTermShortagesCacheEntry? GetLatest(Guid workspaceId, SnapshotId snapshotId, LongTermShortagePopulationOptions options);
    void Set(LongTermShortagesCacheEntry entry);
}

public sealed record LongTermShortagesExportResult(
    LongTermShortagesOutcomeKind Kind,
    string? WorkspaceName = null,
    DateOnly? RefreshDate = null,
    IReadOnlyList<LongTermShortageRow>? Rows = null);

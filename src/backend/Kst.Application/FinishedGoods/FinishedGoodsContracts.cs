using Kst.Domain.Common;
using Kst.Domain.FinishedGoods;

namespace Kst.Application.FinishedGoods;

public sealed record FinishedGoodsSnapshot(
    SnapshotId Id, SnapshotId MpsSnapshotId, DateTimeOffset AcquiredAtUtc,
    Guid WorkspaceId, string Site, IReadOnlyList<FinishedGoodsPosition> Rows);
public sealed record FinishedGoodsReport(FinishedGoodsSnapshot Snapshot, bool IsStale, string? Warning);
public enum FinishedGoodsOutcomeKind { Loaded, UnknownWorkspace, MpsNotLoaded, MpsSnapshotChanged, Unavailable }
public sealed record FinishedGoodsResult(FinishedGoodsOutcomeKind Kind, FinishedGoodsReport? Report = null);

public interface IFinishedGoodsSourceReader
{
    Task<IReadOnlyList<FinishedGoodsSourceRow>> ReadAsync(string site, IReadOnlyList<string> parts, CancellationToken ct);
}

public sealed class DelegateFinishedGoodsSourceReader(
    Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<FinishedGoodsSourceRow>>> read) : IFinishedGoodsSourceReader
{
    public Task<IReadOnlyList<FinishedGoodsSourceRow>> ReadAsync(string site, IReadOnlyList<string> parts, CancellationToken ct) => read(site, parts, ct);
}

public interface IFinishedGoodsSnapshotStore
{
    FinishedGoodsReport? Get(Guid workspaceId);
    void Set(Guid workspaceId, FinishedGoodsReport report);
}

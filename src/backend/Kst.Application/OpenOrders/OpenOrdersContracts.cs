using Kst.Domain.Common;
using Kst.Domain.OpenOrders;

namespace Kst.Application.OpenOrders;

public sealed record OpenOrdersSnapshot(
    SnapshotId Id, SnapshotId MpsSnapshotId, DateTimeOffset AcquiredAtUtc,
    Guid WorkspaceId, string Site, IReadOnlyList<OpenOrderLine> Lines);

public sealed record OpenOrdersReport(OpenOrdersSnapshot Snapshot, bool IsStale, string? Warning);

public enum OpenOrdersOutcomeKind { Loaded, UnknownWorkspace, MpsNotLoaded, MpsSnapshotChanged, Unavailable }

public sealed record OpenOrdersResult(OpenOrdersOutcomeKind Kind, OpenOrdersReport? Report = null, string? ExportWorkspaceName = null)
{
    public static OpenOrdersResult Loaded(OpenOrdersReport report) => new(OpenOrdersOutcomeKind.Loaded, report);
}

public interface IOpenOrdersSourceReader
{
    Task<IReadOnlyList<OpenOrderLine>> ReadAsync(string site, IReadOnlyList<string> parents, CancellationToken cancellationToken);
}

public sealed class DelegateOpenOrdersSourceReader(
    Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<OpenOrderLine>>> read) : IOpenOrdersSourceReader
{
    public Task<IReadOnlyList<OpenOrderLine>> ReadAsync(string site, IReadOnlyList<string> parents, CancellationToken cancellationToken) =>
        read(site, parents, cancellationToken);
}

public interface IOpenOrdersSnapshotStore
{
    OpenOrdersReport? Get(Guid workspaceId);
    void Set(Guid workspaceId, OpenOrdersReport report);
}

using Kst.Domain.Inventory;

namespace Kst.Application.InventoryLocations;

public interface IInventoryLocationReader
{
    Task<IReadOnlyList<InventoryLocationRow>> ReadAsync(string site, string part, CancellationToken ct);
}

public sealed class DelegateInventoryLocationReader(
    Func<string, string, CancellationToken, Task<IReadOnlyList<InventoryLocationRow>>> read) : IInventoryLocationReader
{
    public Task<IReadOnlyList<InventoryLocationRow>> ReadAsync(string site, string part, CancellationToken ct) => read(site, part, ct);
}

public sealed record InventoryLocationsReport(
    Guid WorkspaceId, string Site, string ComponentPart, string MpsSnapshotId,
    DateTimeOffset AcquiredAtUtc, bool IsStale, string? Warning, IReadOnlyList<InventoryLocationRow> Rows);

public enum InventoryLocationsOutcome { Loaded, WorkspaceMissing, SnapshotChanged, Unavailable }
public sealed record InventoryLocationsResult(InventoryLocationsOutcome Outcome, InventoryLocationsReport? Report = null);

public interface IInventoryLocationsStore
{
    InventoryLocationsReport? Get(Guid workspaceId, string part);
    void Set(InventoryLocationsReport report);
}

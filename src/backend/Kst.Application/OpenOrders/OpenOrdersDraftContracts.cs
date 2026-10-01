using Kst.Domain.OpenOrders;

namespace Kst.Application.OpenOrders;

public sealed record OpenOrdersDraft(Guid WorkspaceId, string Site, string MpsSnapshotId,
    string OpenOrdersSnapshotId, DateTimeOffset SavedAtUtc, IReadOnlyList<OpenOrderProposal> Proposals);

public interface IOpenOrdersDraftStore
{
    Task<bool> ExistsAsync(Guid workspaceId, CancellationToken ct);
    Task<OpenOrdersDraft?> LoadAsync(Guid workspaceId, CancellationToken ct);
    Task SaveAsync(OpenOrdersDraft draft, CancellationToken ct);
    Task DeleteAsync(Guid workspaceId, CancellationToken ct);
}

public sealed record OpenOrderReconciliation(OpenOrderProposal Proposal, IReadOnlyList<string> Issues);

public sealed record OpenOrdersDraftResult(OpenOrdersDraft? Draft, OpenOrdersReport? Report,
    IReadOnlyList<OpenOrderReconciliation> Rows, string? Warning);

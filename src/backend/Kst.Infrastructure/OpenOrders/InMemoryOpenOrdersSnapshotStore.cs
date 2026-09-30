using System.Collections.Concurrent;
using Kst.Application.OpenOrders;

namespace Kst.Infrastructure.OpenOrders;

public sealed class InMemoryOpenOrdersSnapshotStore : IOpenOrdersSnapshotStore
{
    private readonly ConcurrentDictionary<Guid, OpenOrdersReport> _reports = new();
    public OpenOrdersReport? Get(Guid workspaceId) =>
        _reports.TryGetValue(workspaceId, out var report) ? report : null;
    public void Set(Guid workspaceId, OpenOrdersReport report) => _reports[workspaceId] = report;
}

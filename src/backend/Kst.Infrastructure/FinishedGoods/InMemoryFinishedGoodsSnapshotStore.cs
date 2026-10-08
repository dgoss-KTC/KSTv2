using System.Collections.Concurrent;
using Kst.Application.FinishedGoods;

namespace Kst.Infrastructure.FinishedGoods;

public sealed class InMemoryFinishedGoodsSnapshotStore : IFinishedGoodsSnapshotStore
{
    private readonly ConcurrentDictionary<Guid, FinishedGoodsReport> _reports = new();
    public FinishedGoodsReport? Get(Guid workspaceId) => _reports.GetValueOrDefault(workspaceId);
    public void Set(Guid workspaceId, FinishedGoodsReport report) => _reports[workspaceId] = report;
}

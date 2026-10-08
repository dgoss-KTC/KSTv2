using System.Collections.Concurrent;
using Kst.Application.InventoryLocations;

namespace Kst.Infrastructure.InventoryLocations;

public sealed class InMemoryInventoryLocationsStore : IInventoryLocationsStore
{
    private readonly ConcurrentDictionary<(Guid, string), InventoryLocationsReport> _reports = new();
    public InventoryLocationsReport? Get(Guid workspaceId, string part) => _reports.GetValueOrDefault((workspaceId, part.ToUpperInvariant()));
    public void Set(InventoryLocationsReport report) => _reports[(report.WorkspaceId, report.ComponentPart.ToUpperInvariant())] = report;
}

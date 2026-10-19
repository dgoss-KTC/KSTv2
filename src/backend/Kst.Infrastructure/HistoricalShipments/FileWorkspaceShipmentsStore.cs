using Kst.Application.HistoricalShipments;
using Kst.Domain.HistoricalShipments;

namespace Kst.Infrastructure.HistoricalShipments;

public sealed class FileWorkspaceShipmentsStore(string root, TimeProvider? clock = null, TimeSpan? lifetime = null) : IWorkspaceShipmentsStore, IDisposable
{
    private readonly FileHistoricalShipmentsStore _results = new(root, clock, lifetime);
    public IHistoricalShipmentsStore Results => _results;
    public ShipmentView GetView(Guid id, string owner) => _results.GetView(id, owner);
    public Task<ShipmentView> SetViewAsync(Guid id, string owner, long expectedVersion, long sequence, IReadOnlyList<ShipmentViewFilter> filters, CancellationToken ct) =>
        _results.SetViewAsync(id, owner, expectedVersion, sequence, filters, ct);
    public void CancelView(Guid id, string owner, long sequence) => _results.CancelView(id, owner, sequence);
    public Task<WorkspaceShipmentPage> PageAsync(Guid id, string owner, long version, long offset, int limit, CancellationToken ct) =>
        _results.WorkspacePageAsync(id, owner, version, offset, limit, ct);
    public Task<ShipmentFacets> FacetsAsync(Guid id, string owner, long version, string column, string? search, IReadOnlyList<string> retainedValues, CancellationToken ct) =>
        _results.FacetsAsync(id, owner, version, column, search, retainedValues, ct);
    public void Dispose() => _results.Dispose();
}

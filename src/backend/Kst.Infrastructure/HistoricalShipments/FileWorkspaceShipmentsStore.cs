using Kst.Application.HistoricalShipments;

namespace Kst.Infrastructure.HistoricalShipments;

public sealed class FileWorkspaceShipmentsStore(string root) : IWorkspaceShipmentsStore, IDisposable
{
    private readonly FileHistoricalShipmentsStore _results = new(root);
    public IHistoricalShipmentsStore Results => _results;
    public void Dispose() => _results.Dispose();
}

using Kst.Domain.HistoricalShipments;

namespace Kst.Application.HistoricalShipments;

public interface IHistoricalShipmentsReader
{
    IAsyncEnumerable<ShipmentRow> ReadAsync(HistoricalShipmentsFilter filter, CancellationToken ct);
}
public sealed class DelegateHistoricalShipmentsReader(Func<HistoricalShipmentsFilter, CancellationToken, IAsyncEnumerable<ShipmentRow>> read) : IHistoricalShipmentsReader
{
    public IAsyncEnumerable<ShipmentRow> ReadAsync(HistoricalShipmentsFilter filter, CancellationToken ct) => read(filter, ct);
}
public sealed record ShipmentOperation(Guid Id, string State, long RowCount, IReadOnlyList<string> Columns,
    DateTimeOffset Expires, DateTimeOffset? AcquiredAtUtc, long UnavailableEnrichmentRows,
    long? OnTimeNumerator = null, long? OnTimeDenominator = null);
public sealed record ShipmentPage(ShipmentOperation Operation, long Offset, IReadOnlyList<ShipmentRow> Rows);
public sealed record ShipmentPreparation(Guid Id, long ByteLength, DateTimeOffset Expires);
public interface IHistoricalShipmentsStore
{
    ShipmentOperation Register(Guid id, string owner);
    ShipmentOperation Get(Guid id, string owner);
    Task<ShipmentOperation> AcquireAsync(Guid id, string owner, HistoricalShipmentsFilter filter,
        Func<CancellationToken, IAsyncEnumerable<ShipmentRow>> read, CancellationToken ct, bool includeOnTimeSummary = false);
    Task<ShipmentPage> PageAsync(Guid id, string owner, long offset, int limit, CancellationToken ct);
    Task<ShipmentPreparation> PrepareAsync(Guid id, string owner,
        Func<IAsyncEnumerable<ShipmentRow>, IReadOnlyList<string>, Stream, CancellationToken, Task> write, CancellationToken ct);
    Stream OpenPreparation(Guid id, string owner);
    void Release(Guid id, string owner);
}
public sealed class ShipmentOperationException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
public sealed class HistoricalShipmentsService(IHistoricalShipmentsReader reader, IHistoricalShipmentsStore store)
{
    public Task<ShipmentOperation> AcquireAsync(Guid id, string owner, HistoricalShipmentsFilter filter, CancellationToken ct)
    {
        var validated = HistoricalShipmentsRules.Validate(filter);
        return store.AcquireAsync(id, owner, validated, token => reader.ReadAsync(validated, token), ct);
    }
}

namespace Kst.Application.Shortages;

public interface INextPurchaseOrderReader
{
    Task<NextPurchaseOrder?> ReadAsync(string site, string partNumber, CancellationToken cancellationToken = default);
}

public sealed record NextPurchaseOrder(
    string PoNumber, DateOnly? DueDate, decimal OpenQuantity, bool IsConfirmed,
    string? TrackingInfo, bool IsKss, string? PoState);

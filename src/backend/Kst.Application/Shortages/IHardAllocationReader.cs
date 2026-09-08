namespace Kst.Application.Shortages;

public interface IHardAllocationReader
{
    Task<IReadOnlyList<HardAllocation>> ReadAsync(
        string site, DateOnly today, int issueDays, CancellationToken cancellationToken = default);
}

public sealed record HardAllocation(
    string Woid, string OperationNumber, string ComponentPart, string Location, string Lot, decimal AllocatedQuantity);

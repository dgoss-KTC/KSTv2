using Kst.Domain.Mps;
using Kst.Domain.Shortages;

namespace Kst.Application.Shortages;

public sealed class DelegateCommittedWorkOrderPopulationReader(
    Func<string, MpsDateBasis, DateOnly, DateOnly, CancellationToken, Task<IReadOnlyList<CommittedWorkOrderComponent>>> read)
    : ICommittedWorkOrderPopulationReader
{
    public Task<IReadOnlyList<CommittedWorkOrderComponent>> ReadAsync(string site, MpsDateBasis dateBasis, DateOnly weekStart, DateOnly windowEndExclusive, CancellationToken cancellationToken = default) =>
        read(site, dateBasis, weekStart, windowEndExclusive, cancellationToken);
}

public sealed class DelegateHardAllocationReader(Func<string, DateOnly, int, CancellationToken, Task<IReadOnlyList<HardAllocation>>> read) : IHardAllocationReader
{
    public Task<IReadOnlyList<HardAllocation>> ReadAsync(string site, DateOnly today, int issueDays, CancellationToken cancellationToken = default) => read(site, today, issueDays, cancellationToken);
}

public sealed class DelegateInventoryPositionReader(Func<string, IReadOnlyList<string>, DateOnly, int, CancellationToken, Task<IReadOnlyList<InventoryPosition>>> read) : IInventoryPositionReader
{
    public Task<IReadOnlyList<InventoryPosition>> ReadAsync(string site, IReadOnlyList<string> partNumbers, DateOnly today, int issueDays, CancellationToken cancellationToken = default) => read(site, partNumbers, today, issueDays, cancellationToken);
}

public sealed class DelegateIssuePolicyReader(Func<string, string, CancellationToken, Task<bool>> read) : IIssuePolicyReader
{
    public Task<bool> ReadAsync(string site, string partNumber, CancellationToken cancellationToken = default) => read(site, partNumber, cancellationToken);
}

public sealed class DelegateIssueDaysReader(Func<string, CancellationToken, Task<int?>> read) : IIssueDaysReader
{
    public Task<int?> ReadAsync(string site, CancellationToken cancellationToken = default) => read(site, cancellationToken);
}

public sealed class DelegateNextPurchaseOrderReader(Func<string, string, CancellationToken, Task<NextPurchaseOrder?>> read) : INextPurchaseOrderReader
{
    public Task<NextPurchaseOrder?> ReadAsync(string site, string partNumber, CancellationToken cancellationToken = default) => read(site, partNumber, cancellationToken);
}

public sealed class DelegateKssScheduleReader(Func<string, string, DateOnly, CancellationToken, Task<bool>> read) : IKssScheduleReader
{
    public Task<bool> IsKssAsync(string site, string partNumber, DateOnly today, CancellationToken cancellationToken = default) => read(site, partNumber, today, cancellationToken);
}

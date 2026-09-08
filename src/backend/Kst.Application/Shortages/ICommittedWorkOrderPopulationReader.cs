using Kst.Domain.Mps;

namespace Kst.Application.Shortages;

public interface ICommittedWorkOrderPopulationReader
{
    Task<IReadOnlyList<CommittedWorkOrderComponent>> ReadAsync(
        string site, MpsDateBasis dateBasis, DateOnly weekStart, DateOnly windowEndExclusive,
        CancellationToken cancellationToken = default);
}

public sealed record CommittedWorkOrderComponent(
    string Woid, string Status, string? WorkOrderType, DateOnly? DueDate, DateOnly? ReleaseDate,
    string ComponentPart, decimal RequiredQuantity, decimal IssuedQuantity, string? UnitOfMeasure);

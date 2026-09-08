using Kst.Domain.Shortages;

namespace Kst.Application.Shortages;

public interface IInventoryPositionReader
{
    Task<IReadOnlyList<InventoryPosition>> ReadAsync(
        string site, IReadOnlyList<string> partNumbers, DateOnly today, int issueDays,
        CancellationToken cancellationToken = default);
}

public sealed record InventoryPosition(string ComponentPart, UsableInventoryPosition Position);

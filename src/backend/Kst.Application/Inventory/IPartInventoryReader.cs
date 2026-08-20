using Kst.Domain.Inventory;

namespace Kst.Application.Inventory;

/// <summary>
/// Reads shared Site + Part inventory summaries for a set of part numbers. Implementations live
/// in Kst.Integrations.Qad; Kst.Api bridges the concrete adapter into this interface via
/// <see cref="DelegatePartInventoryReader"/> so Kst.Application never references
/// Kst.Integrations.Qad. The accepted reader contract returns exactly one summary per
/// requested distinct part — with an authoritative numeric-zero summary for a part with no
/// qualifying inventory rows — so callers never infer zero from a missing result row. A query
/// failure propagates as an exception; it is never converted to zeroes.
/// </summary>
public interface IPartInventoryReader
{
    Task<IReadOnlyList<PartInventorySummary>> ReadSummariesAsync(
        string site,
        IReadOnlyList<string> partNumbers,
        CancellationToken cancellationToken = default);
}

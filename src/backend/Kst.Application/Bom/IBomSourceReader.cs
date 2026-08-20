using Kst.Domain.Bom;

namespace Kst.Application.Bom;

/// <summary>
/// Reads the complete current-effective multi-level structural BOM for one parent part at one
/// site. Implementations live in Kst.Integrations.Qad; Kst.Api bridges the concrete adapter
/// into this interface via <see cref="DelegateBomSourceReader"/> so Kst.Application never
/// references Kst.Integrations.Qad. Returns the complete structural occurrence set in
/// traversal order (no P/M visibility filtering, no inventory enrichment); an empty
/// collection is a successful "no effective relationships" result, and a query failure
/// propagates as an exception rather than a faked empty BOM.
/// </summary>
public interface IBomSourceReader
{
    Task<IReadOnlyList<BomOccurrence>> ReadAsync(
        string site,
        string parentPart,
        DateOnly effectiveDate,
        CancellationToken cancellationToken = default);
}

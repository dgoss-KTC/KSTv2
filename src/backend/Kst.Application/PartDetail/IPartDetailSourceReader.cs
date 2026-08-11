using Kst.Domain.PartDetail;

namespace Kst.Application.PartDetail;

/// <summary>
/// Reads Stage 6 PartDetail source facts for one part. Implementations live in Kst.Integrations.Qad;
/// Kst.Api bridges the concrete adapter into this interface via <see cref="DelegatePartDetailSourceReader"/>
/// so Kst.Application never references Kst.Integrations.Qad. Returns null when <c>pt_mstr</c> does not
/// exist for the part/domain (a true missing-part state, distinct from a query failure).
/// </summary>
public interface IPartDetailSourceReader
{
    Task<PartDetailSourceFacts?> ReadAsync(
        string site,
        string partNumber,
        DateOnly today,
        CancellationToken cancellationToken = default);
}

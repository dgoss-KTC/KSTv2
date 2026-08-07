using Kst.Domain.Mps;

namespace Kst.Application.Mps;

/// <summary>
/// Reads MPS source facts for a workspace's resolved parent parts. Implementations live in
/// Kst.Integrations.Qad; Kst.Api bridges the concrete adapter into this interface via
/// <see cref="DelegateMpsSourceReader"/> so Kst.Application never references Kst.Integrations.Qad.
/// </summary>
public interface IMpsSourceReader
{
    Task<IReadOnlyList<MpsSourceRow>> ReadAsync(
        string site,
        IReadOnlyList<string> parentParts,
        CancellationToken cancellationToken = default);
}

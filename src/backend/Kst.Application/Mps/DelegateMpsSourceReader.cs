using Kst.Domain.Mps;

namespace Kst.Application.Mps;

/// <summary>
/// Generic delegate-backed <see cref="IMpsSourceReader"/>. Lets the composition root (Kst.Api) adapt
/// the concrete QAD adapter without Kst.Application referencing Kst.Integrations.Qad.
/// </summary>
public sealed class DelegateMpsSourceReader : IMpsSourceReader
{
    private readonly Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<MpsSourceRow>>> _read;

    public DelegateMpsSourceReader(
        Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<MpsSourceRow>>> read)
    {
        _read = read;
    }

    public Task<IReadOnlyList<MpsSourceRow>> ReadAsync(
        string site,
        IReadOnlyList<string> parentParts,
        CancellationToken cancellationToken = default) => _read(site, parentParts, cancellationToken);
}

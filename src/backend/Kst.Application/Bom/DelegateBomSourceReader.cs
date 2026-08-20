using Kst.Domain.Bom;

namespace Kst.Application.Bom;

/// <summary>
/// Generic delegate-backed <see cref="IBomSourceReader"/>. Lets the composition root (Kst.Api)
/// adapt the concrete QAD adapter without Kst.Application referencing Kst.Integrations.Qad.
/// </summary>
public sealed class DelegateBomSourceReader : IBomSourceReader
{
    private readonly Func<string, string, DateOnly, CancellationToken, Task<IReadOnlyList<BomOccurrence>>> _read;

    public DelegateBomSourceReader(
        Func<string, string, DateOnly, CancellationToken, Task<IReadOnlyList<BomOccurrence>>> read)
    {
        _read = read;
    }

    public Task<IReadOnlyList<BomOccurrence>> ReadAsync(
        string site,
        string parentPart,
        DateOnly effectiveDate,
        CancellationToken cancellationToken = default) => _read(site, parentPart, effectiveDate, cancellationToken);
}

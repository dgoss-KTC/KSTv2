using Kst.Domain.PartDetail;

namespace Kst.Application.PartDetail;

/// <summary>
/// Generic delegate-backed <see cref="IPartDetailSourceReader"/>. Lets the composition root (Kst.Api)
/// adapt the concrete QAD adapter without Kst.Application referencing Kst.Integrations.Qad.
/// </summary>
public sealed class DelegatePartDetailSourceReader : IPartDetailSourceReader
{
    private readonly Func<string, string, DateOnly, CancellationToken, Task<PartDetailSourceFacts?>> _read;

    public DelegatePartDetailSourceReader(
        Func<string, string, DateOnly, CancellationToken, Task<PartDetailSourceFacts?>> read)
    {
        _read = read;
    }

    public Task<PartDetailSourceFacts?> ReadAsync(
        string site,
        string partNumber,
        DateOnly today,
        CancellationToken cancellationToken = default) => _read(site, partNumber, today, cancellationToken);
}

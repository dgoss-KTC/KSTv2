using Kst.Domain.ComponentDetail;

namespace Kst.Application.ComponentDetail;

/// <summary>
/// Generic delegate-backed <see cref="IComponentSourceReader"/>. Lets the composition root
/// (Kst.Api) adapt the concrete QAD adapter without Kst.Application referencing
/// Kst.Integrations.Qad.
/// </summary>
public sealed class DelegateComponentSourceReader : IComponentSourceReader
{
    private readonly Func<string, string, CancellationToken, Task<ComponentSourceFacts?>> _read;

    public DelegateComponentSourceReader(
        Func<string, string, CancellationToken, Task<ComponentSourceFacts?>> read)
    {
        _read = read;
    }

    public Task<ComponentSourceFacts?> ReadAsync(
        string site,
        string componentPart,
        CancellationToken cancellationToken = default) => _read(site, componentPart, cancellationToken);
}

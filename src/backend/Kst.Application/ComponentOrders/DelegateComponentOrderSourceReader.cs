using Kst.Domain.ComponentOrders;

namespace Kst.Application.ComponentOrders;

/// <summary>
/// Generic delegate-backed <see cref="IComponentOrderSourceReader"/>. Lets the composition root
/// (Kst.Api) adapt the concrete QAD adapter without Kst.Application referencing
/// Kst.Integrations.Qad.
/// </summary>
public sealed class DelegateComponentOrderSourceReader : IComponentOrderSourceReader
{
    private readonly Func<string, IReadOnlyList<string>, DateOnly, CancellationToken, Task<IReadOnlyList<ComponentOrderLine>>> _read;

    public DelegateComponentOrderSourceReader(
        Func<string, IReadOnlyList<string>, DateOnly, CancellationToken, Task<IReadOnlyList<ComponentOrderLine>>> read)
    {
        _read = read;
    }

    public Task<IReadOnlyList<ComponentOrderLine>> ReadAsync(
        string site,
        IReadOnlyList<string> componentParts,
        DateOnly today,
        CancellationToken cancellationToken = default) =>
        _read(site, componentParts, today, cancellationToken);
}

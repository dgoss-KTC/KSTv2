using Kst.Domain.ComponentOrders;

namespace Kst.Application.ComponentOrders;

/// <summary>
/// Reads every qualifying conventional open PO line for a bounded set of workspace-scoped
/// component parts at one site, as of the business date (the effective-KSS relationship is
/// date-sensitive). Implementations live in Kst.Integrations.Qad; Kst.Api bridges the concrete
/// adapter into this interface via <see cref="DelegateComponentOrderSourceReader"/> so
/// Kst.Application never references Kst.Integrations.Qad. An empty collection is a successful
/// "no qualifying conventional open PO lines" result, and a query failure propagates as an
/// exception rather than a faked empty list.
/// </summary>
public interface IComponentOrderSourceReader
{
    Task<IReadOnlyList<ComponentOrderLine>> ReadAsync(
        string site,
        IReadOnlyList<string> componentParts,
        DateOnly today,
        CancellationToken cancellationToken = default);
}

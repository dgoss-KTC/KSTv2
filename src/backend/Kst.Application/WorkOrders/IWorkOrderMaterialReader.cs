using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Reads Stage 7 material/kitting lines for one work order. Implementations live in Kst.Integrations.Qad;
/// Kst.Api bridges the concrete adapter into this interface via <see cref="DelegateWorkOrderMaterialReader"/>
/// so Kst.Application never references Kst.Integrations.Qad.
/// </summary>
public interface IWorkOrderMaterialReader
{
    Task<IReadOnlyList<WorkOrderMaterialLine>> ReadAsync(
        string site,
        string woid,
        CancellationToken cancellationToken = default);
}

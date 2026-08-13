using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Generic delegate-backed <see cref="IWorkOrderMaterialReader"/>. Lets the composition root (Kst.Api)
/// adapt the concrete QAD adapter without Kst.Application referencing Kst.Integrations.Qad.
/// </summary>
public sealed class DelegateWorkOrderMaterialReader : IWorkOrderMaterialReader
{
    private readonly Func<string, string, CancellationToken, Task<IReadOnlyList<WorkOrderMaterialLine>>> _read;

    public DelegateWorkOrderMaterialReader(
        Func<string, string, CancellationToken, Task<IReadOnlyList<WorkOrderMaterialLine>>> read)
    {
        _read = read;
    }

    public Task<IReadOnlyList<WorkOrderMaterialLine>> ReadAsync(
        string site,
        string woid,
        CancellationToken cancellationToken = default) => _read(site, woid, cancellationToken);
}

using Kst.Domain.Mps;
using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Generic delegate-backed <see cref="IWorkOrderSummaryReader"/>. Lets the composition root (Kst.Api)
/// adapt the concrete QAD adapter without Kst.Application referencing Kst.Integrations.Qad.
/// </summary>
public sealed class DelegateWorkOrderSummaryReader : IWorkOrderSummaryReader
{
    private readonly Func<string, string, MpsDateBasis, DateOnly, DateOnly, MpsBucketKind?, DateOnly?, CancellationToken, Task<IReadOnlyList<WorkOrderSummary>>> _readPlanningWindow;
    private readonly Func<string, string, CancellationToken, Task<WorkOrderSummary?>> _readByWoid;

    public DelegateWorkOrderSummaryReader(
        Func<string, string, MpsDateBasis, DateOnly, DateOnly, MpsBucketKind?, DateOnly?, CancellationToken, Task<IReadOnlyList<WorkOrderSummary>>> readPlanningWindow,
        Func<string, string, CancellationToken, Task<WorkOrderSummary?>> readByWoid)
    {
        _readPlanningWindow = readPlanningWindow;
        _readByWoid = readByWoid;
    }

    public Task<IReadOnlyList<WorkOrderSummary>> ReadPlanningWindowAsync(
        string site,
        string parentPart,
        MpsDateBasis dateBasis,
        DateOnly weekStart,
        DateOnly windowEndExclusive,
        MpsBucketKind? bucketKind,
        DateOnly? bucketWeekStart,
        CancellationToken cancellationToken = default) =>
        _readPlanningWindow(site, parentPart, dateBasis, weekStart, windowEndExclusive, bucketKind, bucketWeekStart, cancellationToken);

    public Task<WorkOrderSummary?> ReadByWoidAsync(
        string site,
        string woid,
        CancellationToken cancellationToken = default) => _readByWoid(site, woid, cancellationToken);
}

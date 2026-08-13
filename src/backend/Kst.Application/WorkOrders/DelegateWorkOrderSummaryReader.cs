using Kst.Domain.WorkOrders;

namespace Kst.Application.WorkOrders;

/// <summary>
/// Generic delegate-backed <see cref="IWorkOrderSummaryReader"/>. Lets the composition root (Kst.Api)
/// adapt the concrete QAD adapter without Kst.Application referencing Kst.Integrations.Qad.
/// </summary>
public sealed class DelegateWorkOrderSummaryReader : IWorkOrderSummaryReader
{
    private readonly Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<WorkOrderSummary>>> _readByWoids;
    private readonly Func<string, string, int, CancellationToken, Task<CandidateWorkOrdersResult>> _readCandidates;

    public DelegateWorkOrderSummaryReader(
        Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<WorkOrderSummary>>> readByWoids,
        Func<string, string, int, CancellationToken, Task<CandidateWorkOrdersResult>> readCandidates)
    {
        _readByWoids = readByWoids;
        _readCandidates = readCandidates;
    }

    public Task<IReadOnlyList<WorkOrderSummary>> ReadByWoidsAsync(
        string site,
        IReadOnlyList<string> woids,
        CancellationToken cancellationToken = default) => _readByWoids(site, woids, cancellationToken);

    public Task<CandidateWorkOrdersResult> ReadCandidatesAsync(
        string site,
        string componentPart,
        int limit,
        CancellationToken cancellationToken = default) =>
        _readCandidates(site, componentPart, limit, cancellationToken);
}

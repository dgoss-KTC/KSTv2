using Kst.Application.Workspaces;
using Kst.Domain.HistoricalShipments;
using System.Runtime.CompilerServices;

namespace Kst.Application.HistoricalShipments;

public interface IWorkspaceShipmentsScopeResolver
{
    Task<IReadOnlyList<string>> ResolveAsync(Kst.Domain.Workspaces.WorkspaceAssignment workspace, CancellationToken ct);
}

public sealed class DelegateWorkspaceShipmentsScopeResolver(
    Func<Kst.Domain.Workspaces.WorkspaceAssignment, CancellationToken, Task<IReadOnlyList<string>>> resolve) : IWorkspaceShipmentsScopeResolver
{
    public Task<IReadOnlyList<string>> ResolveAsync(Kst.Domain.Workspaces.WorkspaceAssignment workspace, CancellationToken ct) => resolve(workspace, ct);
}

public interface IWorkspaceShipmentsReader
{
    IAsyncEnumerable<ShipmentRow> ReadAsync(WorkspaceShipmentsFilter filter, CancellationToken ct);
}

public sealed class DelegateWorkspaceShipmentsReader(Func<WorkspaceShipmentsFilter, CancellationToken, IAsyncEnumerable<ShipmentRow>> read) : IWorkspaceShipmentsReader
{
    public IAsyncEnumerable<ShipmentRow> ReadAsync(WorkspaceShipmentsFilter filter, CancellationToken ct) => read(filter, ct);
}

/// <summary>A dedicated store instance isolates workspace listing ownership from Reports.</summary>
public interface IWorkspaceShipmentsStore
{
    IHistoricalShipmentsStore Results { get; }
    ShipmentView GetView(Guid id, string owner);
    Task<ShipmentView> SetViewAsync(Guid id, string owner, long expectedVersion, long sequence, IReadOnlyList<ShipmentViewFilter> filters, CancellationToken ct);
    void CancelView(Guid id, string owner, long sequence);
    Task<WorkspaceShipmentPage> PageAsync(Guid id, string owner, long version, long offset, int limit, CancellationToken ct);
    Task<ShipmentFacets> FacetsAsync(Guid id, string owner, long version, string column, string? search, IReadOnlyList<string> retainedValues, CancellationToken ct);
}

public sealed record ShipmentView(Guid OperationId, long Version, long RowCount, long OnTimeNumerator, long OnTimeDenominator, IReadOnlyList<ShipmentViewFilter> Filters);
public sealed record WorkspaceShipmentPage(ShipmentOperation Operation, ShipmentView View, long Offset, IReadOnlyList<ShipmentRow> Rows);
public sealed record ShipmentFacetValue(string Value, long Count);
public sealed record ShipmentFacets(Guid OperationId, long Version, string Column, IReadOnlyList<ShipmentFacetValue> Values,
    IReadOnlyList<ShipmentFacetValue> RetainedValues, long BlankCount, long DistinctTotal, long MatchingDistinctTotal, bool Truncated);

public sealed class WorkspaceShipmentsService(IWorkspaceConfigurationService workspaces, IWorkspaceShipmentsScopeResolver scopeResolver,
    IWorkspaceShipmentsReader reader, IWorkspaceShipmentsStore store)
{
    public static string Owner(Guid assignmentId, string session) => $"workspace-shipments:{assignmentId:D}:{session}";

    private async Task<Kst.Domain.Workspaces.WorkspaceAssignment> WorkspaceAsync(Guid assignmentId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var configuration = await workspaces.GetWorkspacesAsync();
        ct.ThrowIfCancellationRequested();
        var workspace = configuration.Workspaces.SingleOrDefault(w => w.AssignmentId == assignmentId && w.IsEnabled)
            ?? throw new ShipmentOperationException(404, "Workspace is unavailable or archived.");
        return workspace;
    }

    public async Task<ShipmentOperation> RegisterAsync(Guid assignmentId, Guid id, string session, CancellationToken ct)
    {
        _ = await WorkspaceAsync(assignmentId, ct);
        return store.Results.Register(id, Owner(assignmentId, session));
    }

    public async Task<ShipmentOperation> AcquireAsync(Guid assignmentId, Guid id, string session,
        string? partNumber, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var owner = Owner(assignmentId, session);
        // Prove operation ownership/state before performing an expensive QAD scope resolution. In
        // particular, an unregistered client-supplied UUID must not reach QAD or be tombstoned by
        // cleanup; otherwise arbitrary acquisition requests could consume permanent store capacity.
        var registered = store.Results.Get(id, owner);
        if (registered.State != "registered")
            throw new ShipmentOperationException(409, "Acquisition already started. Use a new operation.");
        var validatedPartNumber = WorkspaceShipmentsFilter.ValidateRequest(partNumber, from, to);
        var currentWorkspace = await WorkspaceAsync(assignmentId, ct);
        var request = new WorkspaceShipmentsFilter(currentWorkspace.Site, [], validatedPartNumber, from, to);
        // The store transitions the owned operation to acquiring before it enumerates this
        // deferred source. Resolve current configuration and QAD membership only inside that
        // owner-checked acquisition callback, so a raced release cannot trigger QAD work. The
        // store also exclusively owns failure cleanup: this service must not release a competing
        // request's operation if it loses an acquisition race.
        return await store.Results.AcquireAsync(id, owner, request.Projection(),
            token => ReadCurrentScopeAsync(assignmentId, validatedPartNumber, from, to, token), ct, includeOnTimeSummary: true);
    }

    private async IAsyncEnumerable<ShipmentRow> ReadCurrentScopeAsync(Guid assignmentId, string? partNumber,
        DateOnly? from, DateOnly? to, [EnumeratorCancellation] CancellationToken ct)
    {
        var workspace = await WorkspaceAsync(assignmentId, ct);
        var parentParts = await scopeResolver.ResolveAsync(workspace, ct);
        ct.ThrowIfCancellationRequested();
        var filter = new WorkspaceShipmentsFilter(workspace.Site,
            Array.AsReadOnly(parentParts.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()), partNumber, from, to).Validate();
        if (filter.ParentParts.Count == 0) yield break;
        await foreach (var row in reader.ReadAsync(filter, ct).WithCancellation(ct)) yield return row;
    }

}

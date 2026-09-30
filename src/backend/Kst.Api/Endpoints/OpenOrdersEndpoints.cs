using Kst.Api.Dtos;
using Kst.Application.OpenOrders;
using Kst.Domain.Common;
using Kst.Domain.OpenOrders;

namespace Kst.Api.Endpoints;

public static class OpenOrdersEndpoints
{
    public static void MapOpenOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/open-orders", Get)
            .WithName("GetOpenOrders").WithTags("OpenOrders")
            .WithSummary("Returns the full workspace Open Orders report for the supplied current MPS snapshot.")
            .Produces<OpenOrdersResponseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        app.MapPost("/api/v1/workspaces/{assignmentId:guid}/open-orders/refresh", Refresh)
            .WithName("RefreshOpenOrders").WithTags("OpenOrders")
            .WithSummary("Refreshes the workspace Open Orders report against the supplied current MPS snapshot.")
            .Produces<OpenOrdersResponseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> Get(Guid assignmentId, Guid mpsSnapshotId,
        OpenOrdersService service, CancellationToken ct) =>
        await Handle(assignmentId, mpsSnapshotId, service, false, ct);

    private static async Task<IResult> Refresh(Guid assignmentId, Guid mpsSnapshotId,
        OpenOrdersService service, CancellationToken ct) =>
        await Handle(assignmentId, mpsSnapshotId, service, true, ct);

    private static async Task<IResult> Handle(Guid id, Guid mpsSnapshotId, OpenOrdersService service, bool refresh, CancellationToken ct)
    {
        if (mpsSnapshotId == Guid.Empty)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["mpsSnapshotId"] = ["A valid current MPS snapshot ID is required."]
            });
        var result = refresh ? await service.RefreshAsync(id, new SnapshotId(mpsSnapshotId), ct)
                             : await service.GetAsync(id, new SnapshotId(mpsSnapshotId), ct);
        return result.Kind switch
        {
            OpenOrdersOutcomeKind.Loaded => Results.Ok(ToDto(result.Report!)),
            OpenOrdersOutcomeKind.UnknownWorkspace => Results.Problem(
                title: "Workspace not found", statusCode: StatusCodes.Status404NotFound),
            OpenOrdersOutcomeKind.MpsNotLoaded => Results.Problem(
                title: "MPS data not loaded", detail: "Load this workspace's MPS data before viewing Open Orders.",
                statusCode: StatusCodes.Status409Conflict),
            OpenOrdersOutcomeKind.MpsSnapshotChanged => Results.Problem(
                title: "MPS snapshot changed", detail: "Reload the workspace's current MPS snapshot before viewing Open Orders.",
                statusCode: StatusCodes.Status409Conflict),
            OpenOrdersOutcomeKind.Unavailable => Results.Problem(
                title: "Open Orders unavailable", detail: "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
                statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static OpenOrdersResponseDto ToDto(OpenOrdersReport report) => new(
        report.Snapshot.WorkspaceId, report.Snapshot.Site, report.Snapshot.MpsSnapshotId.ToString(),
        report.Snapshot.Id.ToString(), report.Snapshot.AcquiredAtUtc, report.IsStale,
        report.Warning, report.Snapshot.Lines.Select(ToDto).ToArray());

    private static OpenOrderLineDto ToDto(OpenOrderLine line) => new(
        new(line.Key.Domain, line.Key.SalesOrder, line.Key.Line), line.ItemNumber, line.Site,
        line.PurchaseOrder, line.Stat, line.ShippedQty,
        new(line.SourceValues.DueDate, line.SourceValues.PerformDate, line.SourceValues.RequiredDate,
            line.SourceValues.DockDate, line.SourceValues.OrderQty, line.SourceValues.Price),
        line.Open, line.ExtPrice, line.UnitPrice, line.Allocated, line.Customer,
        line.CustomerName, line.Salesperson, line.CustomerPart, line.Ios, line.LineComments,
        line.LineHold, line.Partials, line.Picked, line.Plnr, line.ProdStat, line.ProductLine,
        line.QaHold, line.Remarks, line.Revision, line.ShipAcct, line.ShipTo, line.ShipVia,
        line.SiteQoh, line.SoHoldStatus, line.SoType, line.Consignment);
}

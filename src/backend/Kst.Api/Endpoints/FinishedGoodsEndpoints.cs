using Kst.Api.Dtos;
using System.Globalization;
using Kst.Application.FinishedGoods;
using Kst.Domain.Common;

namespace Kst.Api.Endpoints;

public static class FinishedGoodsEndpoints
{
    public static void MapFinishedGoodsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/finished-goods", Get)
            .WithName("GetFinishedGoods").WithTags("FinishedGoods")
            .WithSummary("Returns current inventory for the supplied loaded MPS scope, reusing a compatible process-local snapshot.")
            .Produces<FinishedGoodsResponseDto>()
            .ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503);
        app.MapPost("/api/v1/workspaces/{assignmentId:guid}/finished-goods/refresh", Refresh)
            .WithName("RefreshFinishedGoods").WithTags("FinishedGoods")
            .WithSummary("Reacquires Finished Goods detail and MAX prices only; never refreshes MPS or Open Orders.")
            .Produces<FinishedGoodsResponseDto>()
            .ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503);
    }

    private static Task<IResult> Get(Guid assignmentId, Guid mpsSnapshotId, FinishedGoodsService service, CancellationToken ct) =>
        Read(assignmentId, mpsSnapshotId, service, false, ct);
    private static Task<IResult> Refresh(Guid assignmentId, Guid mpsSnapshotId, FinishedGoodsService service, CancellationToken ct) =>
        Read(assignmentId, mpsSnapshotId, service, true, ct);

    private static async Task<IResult> Read(Guid id, Guid expected, FinishedGoodsService service, bool refresh, CancellationToken ct)
    {
        if (expected == Guid.Empty) return Results.Problem(statusCode: 400, title: "Invalid MPS snapshot");
        var result = refresh ? await service.RefreshAsync(id, new SnapshotId(expected), ct) : await service.GetAsync(id, new SnapshotId(expected), ct);
        return result.Kind switch
        {
            FinishedGoodsOutcomeKind.Loaded => Results.Ok(Dto(result.Report!)),
            FinishedGoodsOutcomeKind.UnknownWorkspace => Results.Problem(statusCode: 404, title: "Workspace not found"),
            FinishedGoodsOutcomeKind.MpsNotLoaded => Results.Problem(statusCode: 409, title: "Load workspace MPS first"),
            FinishedGoodsOutcomeKind.MpsSnapshotChanged => Results.Problem(statusCode: 409, title: "Workspace scope changed"),
            _ => Results.Problem(statusCode: 503, title: "Finished Goods unavailable")
        };
    }

    private static FinishedGoodsResponseDto Dto(FinishedGoodsReport report)
    {
        var snapshot = report.Snapshot;
        return new(snapshot.WorkspaceId, snapshot.Site, snapshot.MpsSnapshotId.ToString(), snapshot.Id.ToString(),
            snapshot.AcquiredAtUtc, "USD", report.IsStale, report.Warning,
            snapshot.Rows.Select(row => new FinishedGoodsRowDto(row.RowId, row.Source.Domain, row.Source.Product,
                row.Source.Location, row.Location, row.Source.Lot, row.Source.Reference, Number(row.Source.OnHand),
                Number(row.Source.UnitPrice), Number(row.Value), row.Source.IsMissingPrice, row.IsRma)).ToArray());
    }

    private static string Number(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);
}

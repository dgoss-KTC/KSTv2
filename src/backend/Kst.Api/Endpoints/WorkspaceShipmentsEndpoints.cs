using Kst.Api.Dtos;
using Kst.Application.HistoricalShipments;
using Kst.Domain.HistoricalShipments;

namespace Kst.Api.Endpoints;

public static class WorkspaceShipmentsEndpoints
{
    public static void MapWorkspaceShipmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/workspaces/{assignmentId:guid}/shipments").WithTags("Workspace Shipments");
        group.MapGet("/spec", () => new WorkspaceShipmentsSpecDto(
            HistoricalShipmentsRules.Catalog.Where(c => c.DefaultSelected).Select(c => new HistoricalShipmentsColumnDto(
                c.Id, c.Label, c.Kind, c.Provenance, c.DefaultSelected, c.Monetary)).ToArray(), HistoricalShipmentsRules.MaximumPageSize))
            .WithName("GetWorkspaceShipmentsSpec");
        group.MapPut("/operations/{id:guid}", (Guid assignmentId, Guid id, HttpRequest r, WorkspaceShipmentsService s, CancellationToken ct) => Run(async () =>
            Results.Ok(MapWorkspace(await s.RegisterAsync(assignmentId, id, Session(r), ct)))))
            .WithName("RegisterWorkspaceShipmentsOperation").Produces<WorkspaceShipmentsOperationDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503);
        group.MapPost("/operations/{id:guid}/acquire", (Guid assignmentId, Guid id, WorkspaceShipmentsRequestDto request, HttpRequest r, WorkspaceShipmentsService s, CancellationToken ct) => Run(async () =>
            Results.Ok(MapWorkspace(await s.AcquireAsync(assignmentId, id, Session(r), request.PartNumber, request.ShipDateFrom, request.ShipDateTo, ct)))))
            .WithName("AcquireWorkspaceShipments").Produces<WorkspaceShipmentsOperationDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(410).ProducesProblem(503);
        group.MapGet("/operations/{id:guid}", (Guid assignmentId, Guid id, HttpRequest r, IWorkspaceShipmentsStore s) => Run(() => Task.FromResult(
            Results.Ok(MapWorkspace(s.Results.Get(id, WorkspaceShipmentsService.Owner(assignmentId, Session(r))))))))
            .WithName("GetWorkspaceShipmentsOperation").Produces<WorkspaceShipmentsOperationDto>().ProducesProblem(400).ProducesProblem(410);
        group.MapGet("/operations/{id:guid}/rows", (Guid assignmentId, Guid id, long offset, int limit, HttpRequest r, IWorkspaceShipmentsStore s, CancellationToken ct) => Run(async () =>
        {
            var page = await s.Results.PageAsync(id, WorkspaceShipmentsService.Owner(assignmentId, Session(r)), offset, limit, ct);
            return Results.Ok(new HistoricalShipmentsPageDto(id, offset, page.Operation.RowCount, page.Operation.Columns,
                page.Rows.Select(row => new HistoricalShipmentsRowDto(row.PhysicalDetailId, row.Cells)).ToArray()));
        }))
            .WithName("GetWorkspaceShipmentsPage").Produces<HistoricalShipmentsPageDto>().ProducesProblem(400).ProducesProblem(409).ProducesProblem(410);
        group.MapDelete("/operations/{id:guid}", (Guid assignmentId, Guid id, HttpRequest r, IWorkspaceShipmentsStore s) => Run(() =>
        {
            s.Results.Release(id, WorkspaceShipmentsService.Owner(assignmentId, Session(r)));
            return Task.FromResult(Results.NoContent());
        }))
            .WithName("ReleaseWorkspaceShipmentsOperation").Produces(204).ProducesProblem(400);
    }
    private static string Session(HttpRequest r) => Guid.TryParseExact(r.Headers["X-KST-Workspace-Shipments-Session"], "D", out var id) && id != Guid.Empty
        ? id.ToString("D") : throw new ArgumentException("A Workspace Shipments visit session UUID is required.");
    private static WorkspaceShipmentsOperationDto MapWorkspace(ShipmentOperation o) => new(o.Id, o.State, o.RowCount, o.Columns,
        o.Expires, o.AcquiredAtUtc, o.UnavailableEnrichmentRows, o.OnTimeNumerator, o.OnTimeDenominator);
    private static async Task<IResult> Run(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ArgumentException e) { return Results.Problem(e.Message, statusCode: 400); }
        catch (ShipmentOperationException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        catch (ShipmentSourceException e) { return Results.Problem(e.Message, statusCode: 503); }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return Results.Problem("Workspace Shipments acquisition failed. No complete result was published. Retry with a new operation.", statusCode: 503); }
    }
}

using Kst.Api.Dtos;
using Kst.Application.HistoricalShipments;
using Kst.Domain.HistoricalShipments;

namespace Kst.Api.Endpoints;

public static class HistoricalShipmentsEndpoints
{
    private static ILogger? _log;
    public static void MapHistoricalShipmentsEndpoints(this IEndpointRouteBuilder app)
    {
        _log = (app as IApplicationBuilder)?.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger("HistoricalShipments");
        var group = app.MapGroup("/api/v1/reports/historical-shipments").WithTags("Reports");
        group.MapGet("/spec", () => new HistoricalShipmentsSpecDto(HistoricalShipmentsRules.Sites,
            HistoricalShipmentsRules.Catalog.Select(c => new HistoricalShipmentsColumnDto(c.Id, c.Label, c.Kind, c.Provenance, c.DefaultSelected, c.Monetary)).ToArray(),
            HistoricalShipmentsRules.MaximumPageSize, HistoricalShipmentsRules.MaximumXlsxDataRows)).WithName("GetHistoricalShipmentsSpec");
        group.MapPut("/operations/{id:guid}", (Guid id, HttpRequest r, IHistoricalShipmentsStore s) => Run(() => Task.FromResult(Results.Ok(Map(s.Register(id, Owner(r)))))))
            .WithName("RegisterHistoricalShipmentsOperation").Produces<HistoricalShipmentsOperationDto>().ProducesProblem(400).ProducesProblem(409);
        group.MapGet("/operations/{id:guid}", (Guid id, HttpRequest r, IHistoricalShipmentsStore s) => Run(() => Task.FromResult(Results.Ok(Map(s.Get(id, Owner(r)))))))
            .WithName("GetHistoricalShipmentsOperation").Produces<HistoricalShipmentsOperationDto>().ProducesProblem(410);
        group.MapPost("/operations/{id:guid}/acquire", Acquire).WithName("AcquireHistoricalShipments")
            .Produces<HistoricalShipmentsOperationDto>().ProducesProblem(400).ProducesProblem(409).ProducesProblem(410).ProducesProblem(503);
        group.MapGet("/operations/{id:guid}/rows", (Guid id, long offset, int limit, HttpRequest r, IHistoricalShipmentsStore s, CancellationToken ct) => Run(async () =>
        {
            var page = await s.PageAsync(id, Owner(r), offset, limit, ct);
            return Results.Ok(new HistoricalShipmentsPageDto(id, offset, page.Operation.RowCount, page.Operation.Columns,
                page.Rows.Select(row => new HistoricalShipmentsRowDto(row.PhysicalDetailId, row.Cells)).ToArray()));
        })).WithName("GetHistoricalShipmentsPage").Produces<HistoricalShipmentsPageDto>().ProducesProblem(400).ProducesProblem(410);
        group.MapPost("/operations/{id:guid}/prepare", (Guid id, HttpRequest r, IHistoricalShipmentsStore s, CancellationToken ct) => Run(async () =>
        {
            var prepared = await s.PrepareAsync(id, Owner(r), Kst.Exports.HistoricalShipmentsWorkbook.WriteAsync, ct);
            return Results.Ok(new HistoricalShipmentsPreparedDto(id, id.ToString("D"), "Historical-Shipments_01.xlsx", prepared.ByteLength, prepared.Expires));
        })).WithName("PrepareHistoricalShipmentsWorkbook").Produces<HistoricalShipmentsPreparedDto>().ProducesProblem(422).ProducesProblem(410).ProducesProblem(503);
        group.MapGet("/preparations/{id:guid}/bytes", (Guid id, HttpRequest r, IHistoricalShipmentsStore s) => Run(() => Task.FromResult(
            Results.Stream(s.OpenPreparation(id, Owner(r)), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))))
            .WithName("GetHistoricalShipmentsPreparedBytes").Produces(200, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").ProducesProblem(410);
        group.MapDelete("/operations/{id:guid}", (Guid id, HttpRequest r, IHistoricalShipmentsStore s) => Run(() =>
        {
            s.Release(id, Owner(r)); return Task.FromResult(Results.NoContent());
        })).WithName("ReleaseHistoricalShipmentsOperation").Produces(204);
    }
    private static Task<IResult> Acquire(Guid id, HistoricalShipmentsRequestDto request, HttpRequest r, HistoricalShipmentsService s, CancellationToken ct) => Run(async () =>
    {
        var result = await s.AcquireAsync(id, Owner(r), new(request.Site, request.CorpGroup, request.CustomerFrom, request.CustomerTo,
            request.ItemFrom, request.ItemTo, request.ProductLineFrom, request.ProductLineTo, request.SalespersonFrom, request.SalespersonTo,
            request.InvoiceDateFrom, request.InvoiceDateTo, request.Columns), ct);
        return Results.Ok(Map(result));
    });
    private static string Owner(HttpRequest r) => Guid.TryParseExact(r.Headers["X-KST-Report-Session"], "D", out var id)
        ? id.ToString("D") : throw new ArgumentException("A report transport session UUID is required.");
    private static HistoricalShipmentsOperationDto Map(ShipmentOperation o) => new(o.Id, o.State, o.RowCount, o.Columns, o.Expires, o.AcquiredAtUtc, o.UnavailableEnrichmentRows);
    private static async Task<IResult> Run(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ArgumentException e) { _log?.LogWarning("Historical Shipments request rejected: {Message}", e.Message); return Results.Problem(e.Message, statusCode: 400); }
        catch (ShipmentOperationException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        catch (ShipmentSourceException e) { _log?.LogWarning("Historical Shipments source failure: {Message}", e.Message); return Results.Problem(e.Message, statusCode: 503); }
        catch (ShipmentWorkbookCapacityException e) { return Results.Problem(e.Message, statusCode: 422); }
        catch (OperationCanceledException) { throw; }
        catch (Exception e) { _log?.LogError(e, "Historical Shipments acquisition/preparation failed ({Type}).", e.GetType().Name); return Results.Problem("Historical Shipments acquisition/preparation failed. No complete result was published. Retry with a new operation.", statusCode: 503); }
    }
}

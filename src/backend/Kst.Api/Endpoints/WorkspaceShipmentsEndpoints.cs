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
            HistoricalShipmentsRules.Catalog.Where(c => c.DefaultSelected).Select(c => new WorkspaceShipmentsColumnDto(
                c.Id, c.Label, c.Kind, c.Provenance, c.DefaultSelected, c.Monetary,
                c.Id == "shipDate" ? null : c.Id == "onTime" ? "values" : c.Kind)).ToArray(), ShipmentViewRules.MaximumPageSize,
            ShipmentViewRules.PageSizes, ShipmentViewRules.MaximumValues, ShipmentViewRules.MaximumTextLength,
            ShipmentViewRules.MaximumDecimalDigits, ShipmentViewRules.MaximumDecimalScale))
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
        group.MapGet("/operations/{id:guid}/rows", (Guid assignmentId, Guid id, long offset, int limit, long? viewVersion, HttpRequest r, IWorkspaceShipmentsStore s, CancellationToken ct) => Run(async () =>
        {
            var page = await s.PageAsync(id, WorkspaceShipmentsService.Owner(assignmentId, Session(r)), viewVersion ?? 0, offset, limit, ct);
            return Results.Ok(new WorkspaceShipmentsPageDto(id, offset, page.View.RowCount, page.Operation.Columns,
                page.Rows.Select(row => new HistoricalShipmentsRowDto(row.PhysicalDetailId, row.Cells)).ToArray(), page.View.Version));
        }))
            .WithName("GetWorkspaceShipmentsPage").Produces<WorkspaceShipmentsPageDto>().ProducesProblem(400).ProducesProblem(409).ProducesProblem(410);
        group.MapGet("/operations/{id:guid}/view", (Guid assignmentId, Guid id, HttpRequest r, IWorkspaceShipmentsStore s) => Run(() => Task.FromResult(
            Results.Ok(MapView(s.GetView(id, WorkspaceShipmentsService.Owner(assignmentId, Session(r))))))))
            .WithName("GetWorkspaceShipmentsView").Produces<WorkspaceShipmentsViewDto>().ProducesProblem(400).ProducesProblem(409).ProducesProblem(410);
        group.MapPost("/operations/{id:guid}/view", (Guid assignmentId, Guid id, HttpRequest r, IWorkspaceShipmentsStore s, CancellationToken ct) => Run(async () =>
        {
            var owner = WorkspaceShipmentsService.Owner(assignmentId, Session(r));
            _ = s.Results.Get(id, owner); // ownership before mapping or predicate work
            var request = await ReadBounded<WorkspaceShipmentsViewRequestDto>(r, 5 * 1024 * 1024, ct);
            if (request.Filters is null || request.Filters.Count > 14 || request.Filters.Any(f => f is null)) throw new ArgumentException("Invalid column filters.");
            return Results.Ok(MapView(await s.SetViewAsync(id, owner, request.ExpectedVersion, request.Sequence,
                request.Filters.Select(f => new ShipmentViewFilter(f.Column, f.Kind, f.Mode, f.Values, f.IncludeBlanks,
                    f.Text, f.Operator, f.Value, f.Upper, f.From, f.To)).ToArray(), ct)));
        })).Accepts<WorkspaceShipmentsViewRequestDto>("application/json")
            .WithName("SetWorkspaceShipmentsView").Produces<WorkspaceShipmentsViewDto>().ProducesProblem(400).ProducesProblem(409).ProducesProblem(410).ProducesProblem(503);
        group.MapPost("/operations/{id:guid}/view/cancel", (Guid assignmentId, Guid id, HttpRequest r, IWorkspaceShipmentsStore s, CancellationToken ct) => Run(async () =>
        {
            var owner = WorkspaceShipmentsService.Owner(assignmentId, Session(r)); _ = s.Results.Get(id, owner);
            var request = await ReadBounded<WorkspaceShipmentsCancelViewDto>(r, 1024, ct);
            s.CancelView(id, owner, request.Sequence);
            return Results.NoContent();
        })).Accepts<WorkspaceShipmentsCancelViewDto>("application/json")
            .WithName("CancelWorkspaceShipmentsView").Produces(204).ProducesProblem(400).ProducesProblem(410);
        group.MapPost("/operations/{id:guid}/values", (Guid assignmentId, Guid id, HttpRequest r, IWorkspaceShipmentsStore s, CancellationToken ct) => Run(async () =>
        {
            var owner = WorkspaceShipmentsService.Owner(assignmentId, Session(r)); _ = s.Results.Get(id, owner);
            var request = await ReadBounded<WorkspaceShipmentsFacetRequestDto>(r, 512 * 1024, ct);
            var f = await s.FacetsAsync(id, owner, request.ViewVersion,
                request.Column, request.Search, request.RetainedValues, ct);
            return Results.Ok(new WorkspaceShipmentsFacetsDto(f.OperationId, f.Version, f.Column,
                f.Values.Select(v => new WorkspaceShipmentsFacetValueDto(v.Value, v.Count)).ToArray(),
                f.RetainedValues.Select(v => new WorkspaceShipmentsFacetValueDto(v.Value, v.Count)).ToArray(),
                f.BlankCount, f.DistinctTotal, f.MatchingDistinctTotal, f.Truncated));
        })).Accepts<WorkspaceShipmentsFacetRequestDto>("application/json")
            .WithName("GetWorkspaceShipmentsValues").Produces<WorkspaceShipmentsFacetsDto>().ProducesProblem(400).ProducesProblem(409).ProducesProblem(410).ProducesProblem(503);
        group.MapDelete("/operations/{id:guid}", (Guid assignmentId, Guid id, HttpRequest r, IWorkspaceShipmentsStore s) => Run(() =>
        {
            s.Results.Release(id, WorkspaceShipmentsService.Owner(assignmentId, Session(r)));
            return Task.FromResult(Results.NoContent());
        }))
            .WithName("ReleaseWorkspaceShipmentsOperation").Produces(204).ProducesProblem(400);
    }
    private static string Session(HttpRequest r) => Guid.TryParseExact(r.Headers["X-KST-Workspace-Shipments-Session"], "D", out var id) && id != Guid.Empty
        ? id.ToString("D") : throw new ArgumentException("A Workspace Shipments visit session UUID is required.");
    private static async Task<T> ReadBounded<T>(HttpRequest r, int maximumBytes, CancellationToken ct)
    {
        if (r.ContentLength > maximumBytes) throw new ArgumentException("Request exceeds its byte limit.");
        using var body = new MemoryStream(); var buffer = new byte[8192];
        int count;
        while ((count = await r.Body.ReadAsync(buffer, ct)) != 0)
        {
            if (body.Length + count > maximumBytes) throw new ArgumentException("Request exceeds its byte limit.");
            body.Write(buffer, 0, count);
        }
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(body.GetBuffer().AsSpan(0, (int)body.Length), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
            ?? throw new ArgumentException("Request body is required.");
        }
        catch (System.Text.Json.JsonException) { throw new ArgumentException("Invalid request JSON."); }
    }
    private static WorkspaceShipmentsOperationDto MapWorkspace(ShipmentOperation o) => new(o.Id, o.State, o.RowCount, o.Columns,
        o.Expires, o.AcquiredAtUtc, o.UnavailableEnrichmentRows, o.OnTimeNumerator, o.OnTimeDenominator);
    private static WorkspaceShipmentsViewDto MapView(ShipmentView v) => new(v.OperationId, v.Version, v.RowCount, v.OnTimeNumerator, v.OnTimeDenominator,
        v.Filters.Select(f => new ShipmentColumnFilterDto(f.Column, f.Kind, f.Mode, f.Values, f.IncludeBlanks, f.Text, f.Operator, f.Value, f.Upper, f.From, f.To)).ToArray());
    private static async Task<IResult> Run(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ArgumentException e) { return Results.Problem(e.Message, statusCode: 400); }
        catch (ShipmentOperationException e) { return Results.Problem(e.Message, statusCode: e.StatusCode); }
        catch (ShipmentSourceException e) { return Results.Problem(e.Message, statusCode: 503); }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return Results.Problem("Workspace Shipments work failed. No partial result was published. Retry; the last complete view is preserved when filtering fails.", statusCode: 503); }
    }
}

using Kst.Api.Dtos;
using Kst.Application.ShipmentsToGo;
using Kst.Domain.ShipmentsToGo;
using Kst.Exports;

namespace Kst.Api.Endpoints;

public static class ShipmentsToGoEndpoints
{
    public static void MapShipmentsToGoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/reports/shipments-to-go", Generate)
            .WithName("GenerateShipmentsToGo").WithTags("Reports")
            .Produces<ShipmentsToGoPreparedDto>().ProducesProblem(400).ProducesProblem(503);
        app.MapGet("/api/v1/reports/shipments-to-go/preparations/{id:guid}/bytes", Bytes)
            .WithName("GetShipmentsToGoPreparedBytes").WithTags("Reports")
            .Produces<byte[]>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .ProducesProblem(410).ProducesProblem(400);
    }

    private static async Task<IResult> Generate(ShipmentsToGoRequestDto request, HttpRequest httpRequest,
        ShipmentsToGoService service, ComponentMrpPreparations preparations, CancellationToken ct)
    {
        if (!ShipmentsToGoRules.ValidSites(request.Sites))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["sites"] = ["Select at least one supported site, without duplicates."] });
        var owner = httpRequest.Headers["X-KST-Report-Session"].ToString();
        if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A report transport session is required.", statusCode: 400);
        if (!await preparations.Generations.WaitAsync(0, ct)) return Results.Problem("Report generation is busy. Retry.", statusCode: 503);
        try
        {
            var report = await service.GenerateAsync(request.Sites, request.IncludeNextWeek, () => DateOnly.FromDateTime(DateTime.Now), ct);
            if (report is null) return Results.Ok(new ShipmentsToGoPreparedDto("noData", null, null, DateOnly.FromDateTime(DateTime.Now), DateTimeOffset.UtcNow, null));
            var bytes = ShipmentsToGoWorkbook.Create(report);
            var stored = preparations.Add(owner, bytes, "ShipmentsToGo");
            return Results.Ok(new ShipmentsToGoPreparedDto("prepared", stored.Id.ToString(),
                $"Shipments-To-Go_{report.LabelDate:yyyy-MM-dd}_01.xlsx", report.LabelDate, report.AcquiredAtUtc, stored.Expires));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem("Shipments-To-Go orders acquisition or preparation failed. Retry Generate.", statusCode: 503); }
        finally { preparations.Generations.Release(); }
    }

    private static IResult Bytes(Guid id, HttpRequest request, ComponentMrpPreparations preparations)
    {
        var owner = request.Headers["X-KST-Report-Session"].ToString();
        if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A report transport session is required.", statusCode: 400);
        var bytes = preparations.Get(id, owner, "ShipmentsToGo");
        return bytes is null ? Results.Problem("Preparation expired or is not owned by this session/report. Generate again.", statusCode: 410)
            : Results.Bytes(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}

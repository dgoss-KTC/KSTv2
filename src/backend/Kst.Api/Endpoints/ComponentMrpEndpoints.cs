using Kst.Api.Dtos;
using Kst.Application.ComponentMrp;
using Kst.Exports;

namespace Kst.Api.Endpoints;

public static class ComponentMrpEndpoints
{
    public static void MapComponentMrpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/reports/component-mrp", Generate)
            .WithName("GenerateComponentMrp").WithTags("Reports")
            .Produces<ComponentMrpPreparedDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(503);
        app.MapGet("/api/v1/reports/component-mrp/preparations/{id:guid}/bytes", Bytes)
            .WithName("GetComponentMrpPreparedBytes").WithTags("Reports")
            .Produces<byte[]>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").ProducesProblem(410).ProducesProblem(400);
    }

    private static IResult Bytes(Guid id, HttpRequest request, ComponentMrpPreparations preparations)
    {
        var owner = request.Headers["X-KST-Report-Session"].ToString();
        if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A report transport session is required.", statusCode: 400);
        var bytes = preparations.Get(id, owner);
        return bytes is null ? Results.Problem("Preparation expired or is not owned by this session. Generate again.", statusCode: 410)
            : Results.Bytes(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static async Task<IResult> Generate(ComponentMrpRequestDto request, HttpRequest httpRequest, ComponentMrpService service,
        ComponentMrpPreparations preparations, CancellationToken ct)
    {
        if (!ComponentMrpService.ValidSite(request.Site) || string.IsNullOrWhiteSpace(request.Part) || request.Part.Length > 256 || request.HorizonWeeks is not (13 or 26 or 52 or 72) || request.Period is not ("Day" or "Week" or "Month"))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["Enter a part, supported site and horizon."] });
        var owner = httpRequest.Headers["X-KST-Report-Session"].ToString();
        if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A report transport session is required.", statusCode: 400);
        if (!await preparations.Generations.WaitAsync(0, ct)) return Results.Problem("Report generation is busy. Retry.", statusCode: 503);
        try
        {
            var report = await service.GenerateAsync(request.Site, request.Part, request.HorizonWeeks, request.IncludeUnconfirmed,
                DateOnly.FromDateTime(DateTime.Now), ct);
            if (report is null) return Results.Problem("No part master exists in the selected site's domain.", title: "Part not found", statusCode: 404);
            var bytes = ComponentMrpWorkbook.Create(report.Site, report.AsOfDate, report.IncludeUnconfirmed, report.Source, report.Projection, request.Period);
            var stored = preparations.Add(owner, bytes);
            return Results.Ok(new ComponentMrpPreparedDto(stored.Id.ToString(),
                ComponentMrpWorkbook.SuggestedFileName(report.Source.Input.ComponentPart, report.Site, report.AsOfDate),
                report.AsOfDate, report.Source.AcquiredAtUtc, stored.Expires, report.Projection.DataQualityWarning, request.Period, report.IncludeUnconfirmed,
                Kst.Domain.LongTermShortages.LongTermShortagesBuilder.GetWeekOneStart(report.AsOfDate).AddDays(report.HorizonWeeks * 7)));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem("Component MRP acquisition or preparation failed. Retry Generate; no empty fallback was produced.", statusCode: 503); }
        finally { preparations.Generations.Release(); }
    }
}

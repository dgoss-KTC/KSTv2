using Kst.Api.Dtos;
using Kst.Application.OpenOrderReport;
using Kst.Domain.OpenOrderReport;
using Kst.Exports;

namespace Kst.Api.Endpoints;

public static class OpenOrderReportEndpoints
{
    public static void MapOpenOrderReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/reports/open-orders", Generate)
            .WithName("GenerateOpenOrderReport").WithTags("Reports")
            .Produces<OpenOrderReportPreparedDto>().ProducesProblem(400).ProducesProblem(503);
        app.MapGet("/api/v1/reports/open-orders/preparations/{id:guid}/bytes", Bytes)
            .WithName("GetOpenOrderReportPreparedBytes").WithTags("Reports")
            .Produces<byte[]>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .ProducesProblem(410).ProducesProblem(400);
    }

    private static async Task<IResult> Generate(OpenOrderReportRequestDto request, HttpRequest httpRequest,
        OpenOrderReportService service, ComponentMrpPreparations preparations, CancellationToken ct)
    {
        var owner = httpRequest.Headers["X-KST-Report-Session"].ToString();
        if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A report transport session is required.", statusCode: 400);
        preparations.Discard(owner, "OpenOrderReport");
        var date = DateOnly.FromDateTime(DateTime.Now);
        OpenOrderReportFilter filter;
        try
        {
            filter = OpenOrderReportRules.Validate(new(request.CustomerName, request.CustomerNumber, request.SalespersonId,
                request.ProductLineFrom, request.ProductLineTo, request.Ios, request.DueDateFrom, request.DueDateTo, request.Site, request.OptionalFields), date);
        }
        catch (ArgumentException e) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["inputs"] = [e.Message] }); }
        if (!await preparations.Generations.WaitAsync(0, ct)) return Results.Problem("Report generation is busy. Retry.", statusCode: 503);
        try
        {
            var report = await service.GenerateAsync(filter, date, ct);
            if (report is null) return Results.Ok(new OpenOrderReportPreparedDto("noData", null, null, date, DateTimeOffset.UtcNow, null));
            var bytes = OpenOrderReportWorkbook.Create(report);
            ct.ThrowIfCancellationRequested();
            var stored = preparations.Add(owner, bytes, "OpenOrderReport");
            return Results.Ok(new OpenOrderReportPreparedDto("prepared", stored.Id.ToString(),
                $"Open-Orders_{date:yyyy-MM-dd}_01.xlsx", date, report.AcquiredAtUtc, stored.Expires));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem("Open Order Report acquisition/preparation failed, has unsupported source data or exceeds capacity. No workbook was prepared. Retry Generate.", statusCode: 503); }
        finally { preparations.Generations.Release(); }
    }

    private static IResult Bytes(Guid id, HttpRequest request, ComponentMrpPreparations preparations)
    {
        var owner = request.Headers["X-KST-Report-Session"].ToString();
        if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A report transport session is required.", statusCode: 400);
        var bytes = preparations.Get(id, owner, "OpenOrderReport");
        return bytes is null ? Results.Problem("Preparation expired or is not owned by this session/report. Generate again.", statusCode: 410)
            : Results.Bytes(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}

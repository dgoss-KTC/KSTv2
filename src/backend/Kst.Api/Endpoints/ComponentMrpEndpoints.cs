using Kst.Api.Dtos;
using Kst.Application.ComponentMrp;
using Kst.Exports;
using Kst.Domain.Common;
using Kst.Domain.ComponentMrp;
using Kst.Domain.LongTermShortages;
using System.Globalization;

namespace Kst.Api.Endpoints;

public static class ComponentMrpEndpoints
{
    public static void MapComponentMrpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/reports/component-mrp", Generate)
            .WithName("GenerateComponentMrp").WithTags("Reports")
            .Produces<ComponentMrpPreparedDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(503);
        app.MapPost("/api/v1/reports/component-mrp/projection", Screen)
            .WithName("ReadComponentMrpProjection").WithTags("Reports")
            .Produces<ComponentMrpScreenDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(410).ProducesProblem(503);
        app.MapPost("/api/v1/reports/component-mrp/screen-preparations", PrepareScreen)
            .WithName("PrepareComponentMrpScreen").WithTags("Reports")
            .Produces<ComponentMrpPreparedDto>().ProducesProblem(400).ProducesProblem(410).ProducesProblem(503);
        app.MapGet("/api/v1/reports/component-mrp/preparations/{id:guid}/bytes", Bytes)
            .WithName("GetComponentMrpPreparedBytes").WithTags("Reports")
            .Produces<byte[]>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").ProducesProblem(410).ProducesProblem(400);
    }

    private static bool Valid(ComponentMrpRequestDto request) => ComponentMrpService.ValidSite(request.Site)
        && !string.IsNullOrWhiteSpace(request.Part) && request.Part.Length <= 256 && !request.Part.Any(char.IsControl)
        && request.HorizonWeeks is 13 or 26 or 52 or 72 && request.Period is "Day" or "Week" or "Month";
    private static IResult Expired() => Results.Problem("MRP acquisition expired, changed or is not owned by this session. Refresh MRP before exporting.", statusCode: 410);
    private static ComponentMrpAcquisitions.Entry? Held(ComponentMrpRequestDto request, string owner, ComponentMrpAcquisitions acquisitions) =>
        Guid.TryParseExact(request.AcquisitionId, "D", out var id) && request.AsOfDate is { } asOf
            ? acquisitions.Get(id, owner, request.Site, request.Part, request.HorizonWeeks, asOf) : null;

    private static async Task<IResult> Screen(ComponentMrpRequestDto request, HttpRequest http, ComponentMrpService service,
        ComponentMrpAcquisitions acquisitions, ComponentMrpPreparations preparations, IClock clock, CancellationToken ct)
    {
        if (!Valid(request)) return Results.Problem("Enter a part, supported site, horizon and period.", statusCode: 400);
        var owner = http.Headers["X-KST-Report-Session"].ToString();
        if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A report transport session is required.", statusCode: 400);
        if (!await preparations.Generations.WaitAsync(0, ct)) return Results.Problem("Report generation is busy. Retry MRP.", statusCode: 503);
        try
        {
            ComponentMrpAcquisitions.Entry entry; string id;
            if (request.AcquisitionId is not null)
            {
                var held = Held(request, owner, acquisitions);
                if (held is null) return Expired();
                entry = held; id = request.AcquisitionId;
            }
            else
            {
                var report = await service.GenerateAsync(request.Site, request.Part, request.HorizonWeeks, request.IncludeUnconfirmed,
                    DateOnly.FromDateTime(clock.LocalNow.DateTime), ct);
                if (report is null) return Results.Problem("No part master exists in the selected site's domain.", title: "Part not found", statusCode: 404);
                ct.ThrowIfCancellationRequested();
                var stored = acquisitions.Add(owner, report); entry = stored.Entry; id = stored.Id.ToString();
            }
            return Results.Ok(MapScreen(entry.Report.Select(request.IncludeUnconfirmed), request.Period, id, entry.Expires));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem("Component MRP acquisition failed or exceeds capacity. Retry MRP; no empty fallback was produced.", statusCode: 503); }
        finally { preparations.Generations.Release(); }
    }

    internal static ComponentMrpScreenDto MapScreen(ComponentMrpReport report, string period, string id, DateTimeOffset expires)
    {
        static string Text(decimal value) => value.ToString(CultureInfo.InvariantCulture);
        return new(report.Site, report.Part, report.AsOfDate, report.HorizonWeeks, period, report.IncludeUnconfirmed,
            LongTermShortagesBuilder.GetWeekOneStart(report.AsOfDate).AddDays(report.HorizonWeeks * 7), report.Source.AcquiredAtUtc, id, expires,
            report.Source.Input.UnitOfMeasure, Text(report.Projection.Past.ProjectedQoh), report.Source.Input.SafetyStock is { } safety ? Text(safety) : null,
            report.Projection.DataQualityWarning, ComponentMrpPeriods.Build(report.AsOfDate, report.Projection, report.IncludeUnconfirmed, period)
                .Select(p => new ComponentMrpPeriodDto(p.Label, p.Start, p.EndExclusive, Text(p.Demand), Text(p.Receipts), Text(p.ProjectedQoh),
                    Text(p.PlannedDue), Text(p.PlannedRelease), p.HasUnconfirmed)).ToArray());
    }

    private static async Task<IResult> PrepareScreen(ComponentMrpRequestDto request, HttpRequest http,
        ComponentMrpAcquisitions acquisitions, ComponentMrpPreparations preparations, CancellationToken ct)
    {
        if (!Valid(request)) return Results.Problem("Invalid screen MRP settings.", statusCode: 400);
        var owner = http.Headers["X-KST-Report-Session"].ToString();
        if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A report transport session is required.", statusCode: 400);
        if (!await preparations.Generations.WaitAsync(0, ct)) return Results.Problem("Report generation is busy. Retry.", statusCode: 503);
        try
        {
            var held = Held(request, owner, acquisitions);
            if (held is null) return Expired();
            var report = held.Report.Select(request.IncludeUnconfirmed);
            ct.ThrowIfCancellationRequested();
            var bytes = ComponentMrpWorkbook.Create(report.Site, report.AsOfDate, report.IncludeUnconfirmed, report.Source, report.Projection, request.Period);
            ct.ThrowIfCancellationRequested();
            // Expiry/business-date changes during byte generation are not a successful preparation.
            if (Held(request, owner, acquisitions) is null) return Expired();
            var stored = preparations.Add(owner, bytes);
            return Results.Ok(new ComponentMrpPreparedDto(stored.Id.ToString(),
                ComponentMrpWorkbook.SuggestedFileName(report.Source.Input.ComponentPart, report.Site, report.AsOfDate),
                report.AsOfDate, report.Source.AcquiredAtUtc, stored.Expires, report.Projection.DataQualityWarning, request.Period, report.IncludeUnconfirmed,
                LongTermShortagesBuilder.GetWeekOneStart(report.AsOfDate).AddDays(report.HorizonWeeks * 7)));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem("Component MRP preparation failed. No source refresh was performed.", statusCode: 503); }
        finally { preparations.Generations.Release(); }
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
        // Legacy direct generation is intentionally separate: screen tokens must never fall through to a read.
        if (request.AcquisitionId is not null) return Results.Problem("Use screen-bound preparation for retained MRP.", statusCode: 400);
        if (!Valid(request))
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

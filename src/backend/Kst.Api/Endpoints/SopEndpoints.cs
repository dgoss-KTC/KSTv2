using Kst.Api.Dtos;
using Kst.Application.Sop;
using Kst.Domain.Sop;
using Kst.Exports;

namespace Kst.Api.Endpoints;

public static class SopEndpoints
{
    public static void MapSopEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/reports/sop/acquisitions", Acquire)
            .WithName("AcquireSop").WithTags("Reports").Produces<SopAcquiredDto>().ProducesProblem(400).ProducesProblem(503);
        app.MapPost("/api/v1/reports/sop/preparations", Prepare)
            .WithName("PrepareSop").WithTags("Reports").Produces<SopPreparedDto>().ProducesProblem(400).ProducesProblem(410).ProducesProblem(503);
        app.MapDelete("/api/v1/reports/sop/acquisitions/{id:guid}", Discard)
            .WithName("DiscardSopAcquisition").WithTags("Reports").Produces(204).ProducesProblem(400);
        app.MapGet("/api/v1/reports/sop/preparations/{id:guid}/bytes", Bytes)
            .WithName("GetSopPreparedBytes").WithTags("Reports")
            .Produces<byte[]>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").ProducesProblem(410).ProducesProblem(400);
    }

    private static string? Owner(HttpRequest request)
    {
        var owner = request.Headers["X-KST-Report-Session"].ToString();
        return Guid.TryParseExact(owner, "D", out _) ? owner : null;
    }
    private static async Task<IResult> Acquire(SopRequestDto request, HttpRequest http, SopService service,
        SopAcquisitions acquisitions, ComponentMrpPreparations preparations, CancellationToken ct)
    {
        var owner = Owner(http);
        if (owner is null) return Results.Problem("A report transport session is required.", statusCode: 400);
        var ranges = request.Ranges?.Select(r => r is null ? null! : new SopRange(r.From, r.To)).ToArray();
        if (!SopRules.ValidRequest(request.Site, request.GroupBy, ranges)) return Results.Problem("Select a supported site, grouping and one to five four-digit product-line pairs.", statusCode: 400);
        if (!await preparations.Generations.WaitAsync(0, ct)) return Results.Problem("Report generation is busy. Retry.", statusCode: 503);
        try
        {
            acquisitions.DiscardOwner(owner);
            var source = await service.AcquireAsync(request.Site, request.GroupBy, ranges!, ct);
            if (source.Rows.Count == 0) return Results.Ok(new SopAcquiredDto("noData", null, [], DateOnly.FromDateTime(DateTime.Now), DateTimeOffset.UtcNow, null));
            var held = acquisitions.Add(owner, request.Site, request.GroupBy, source);
            return Results.Ok(new SopAcquiredDto("acquired", held.Id.ToString(), source.WeekLabels, held.Entry.AsOfDate, held.Entry.AcquiredAtUtc, held.Entry.Expires));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (InvalidOperationException e) when (e.Message.StartsWith("S&OP procedure output", StringComparison.Ordinal))
        { return Results.Problem("S&OP procedure output has an unsupported schema or value. No workbook was prepared.", statusCode: 503); }
        catch (Exception) { return Results.Problem("S&OP acquisition failed or exceeded report capacity. Retry Generate.", statusCode: 503); }
        finally { preparations.Generations.Release(); }
    }
    private static async Task<IResult> Prepare(SopPrepareDto request, HttpRequest http, SopAcquisitions acquisitions,
        ComponentMrpPreparations preparations, CancellationToken ct)
    {
        var owner = Owner(http);
        if (owner is null || !Guid.TryParseExact(request.AcquisitionId, "D", out var id)) return Results.Problem("Invalid report session or acquisition.", statusCode: 400);
        if (!await preparations.Generations.WaitAsync(0, ct)) return Results.Problem("Report preparation is busy. Retry Generate.", statusCode: 503);
        try
        {
            var held = acquisitions.Take(id, owner);
            if (held is null) return Results.Problem("Acquisition expired, discarded or is not owned by this session/report. Generate again.", statusCode: 410);
            var p = request.Presentation;
            SopPresentation? presentation = p is null || p.Periods is null || p.Exceptions is null ? null : new(p.AnchorFiscalYear, p.AnchorStartDate,
                p.Exceptions.Select(e => e is null ? null! : new SopCalendarException(e.FiscalYear, e.ExtraWeekPeriod)).ToArray(),
                p.Periods.Select(e => e is null ? null! : new SopPeriod(e.FiscalYear, e.Period, e.Quarter, e.StartSunday, e.WeekCount, e.WeekLabels)).ToArray());
            if (!SopRules.ValidPresentation(held.Source.WeekLabels, presentation)) return Results.Problem("Invalid fiscal presentation coverage. Generate again with valid calendar settings.", statusCode: 400);
            ct.ThrowIfCancellationRequested();
            var bytes = SopWorkbook.Create(new(held.Site, held.GroupBy, held.Source, held.AsOfDate, held.AcquiredAtUtc, presentation!));
            ct.ThrowIfCancellationRequested();
            var stored = preparations.Add(owner, bytes, "Sop");
            return Results.Ok(new SopPreparedDto("prepared", stored.Id.ToString(), $"S&OP_{held.Site}_{held.AsOfDate:yyyy-MM-dd}_01.xlsx", held.AsOfDate, held.AcquiredAtUtc, stored.Expires));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem("S&OP preparation failed or exceeded capacity. Retry Generate.", statusCode: 503); }
        finally { preparations.Generations.Release(); }
    }
    private static IResult Discard(Guid id, HttpRequest http, SopAcquisitions acquisitions)
    {
        var owner = Owner(http);
        if (owner is null) return Results.Problem("A report transport session is required.", statusCode: 400);
        acquisitions.Discard(id, owner); return Results.NoContent();
    }
    private static IResult Bytes(Guid id, HttpRequest http, ComponentMrpPreparations preparations)
    {
        var owner = Owner(http);
        if (owner is null) return Results.Problem("A report transport session is required.", statusCode: 400);
        var bytes = preparations.Get(id, owner, "Sop");
        return bytes is null ? Results.Problem("Preparation expired or is not owned by this session/report. Generate again.", statusCode: 410)
            : Results.Bytes(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}

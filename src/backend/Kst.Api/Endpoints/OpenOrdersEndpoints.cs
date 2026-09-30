using Kst.Api.Dtos;
using Kst.Application.OpenOrders;
using Kst.Domain.Common;
using Kst.Domain.OpenOrders;
using Kst.Exports;
using Kst.Exports.Contracts;
using System.Text.RegularExpressions;

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
        app.MapPost("/api/v1/workspaces/{assignmentId:guid}/open-orders/report-export", ExportReport)
            .WithName("ExportOpenOrdersReport").WithTags("OpenOrders")
            .WithSummary("Exports selected rows and visible columns from a matching cached workspace report; never validates QXtend changes.")
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> ExportReport(Guid assignmentId, ExportOpenOrdersReportRequestDto request,
        OpenOrdersService service, IExportService exports, CancellationToken ct)
    {
        if (!Guid.TryParse(request.MpsSnapshotId, out var mpsId) || mpsId == Guid.Empty ||
            !Guid.TryParse(request.OpenOrdersSnapshotId, out var reportId) || reportId == Guid.Empty ||
            request.LineKeys is null || request.Columns is null || request.Columns.Count == 0 ||
            request.Columns.Distinct(StringComparer.Ordinal).Count() != request.Columns.Count ||
            request.Columns.Any(c => c is null || !OpenOrdersReportColumns.All.ContainsKey(c)) ||
            request.LineKeys.Any(k => k is null || string.IsNullOrEmpty(k.Domain) || string.IsNullOrEmpty(k.SalesOrder)) ||
            request.LineKeys.Distinct().Count() != request.LineKeys.Count)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["Valid snapshot IDs, unique line keys and known unique columns are required."] });

        // No new source read: export only the exact compatible last-good report.
        var result = await service.GetCachedForReportExportAsync(assignmentId, new SnapshotId(mpsId), new SnapshotId(reportId), ct);
        if (result.Kind != OpenOrdersOutcomeKind.Loaded)
            return result.Kind == OpenOrdersOutcomeKind.Unavailable
                ? Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable)
                : Results.Problem(statusCode: StatusCodes.Status409Conflict);
        var report = result.Report!;
        var byKey = report.Snapshot.Lines.ToDictionary(line => line.Key);
        if (request.LineKeys.Count > byKey.Count)
            return Results.Problem(title: "Open Orders report changed", statusCode: StatusCodes.Status409Conflict);
        var rows = new List<OpenOrderLine>(request.LineKeys.Count);
        foreach (var key in request.LineKeys)
        {
            if (!byKey.TryGetValue(new OpenOrderLineKey(key.Domain, key.SalesOrder, key.Line), out var line))
                return Results.Problem(title: "Open Orders report changed", statusCode: StatusCodes.Status409Conflict);
            rows.Add(line);
        }
        return Results.File(exports.CreateOpenOrdersWorkbook(rows, request.Columns, report.Snapshot.AcquiredAtUtc, report.IsStale),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{SafeFilePrefix(result.ExportWorkspaceName, report.Snapshot.Site)}-Open-Orders-{report.Snapshot.AcquiredAtUtc:yyyy-MM-dd}.xlsx");
    }

    private static string SafeFilePrefix(string? name, string site)
    {
        static string Clean(string value)
        {
            var cleaned = Regex.Replace(value, @"[<>:""/\\|?*\p{Cc}]", " ");
            return Regex.Replace(cleaned, @"[\s_-]+", "-").Trim(' ', '.', '-', '_');
        }
        var prefix = Clean(name ?? "");
        return prefix.Length > 0 ? prefix : Clean(site) is { Length: > 0 } fallback ? fallback : "Workspace";
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

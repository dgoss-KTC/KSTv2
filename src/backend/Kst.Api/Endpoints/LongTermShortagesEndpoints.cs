using Kst.Api.Dtos;
using Kst.Application.LongTermShortages;
using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;
using Kst.Exports.Contracts;
namespace Kst.Api.Endpoints;
public static class LongTermShortagesEndpoints
{
    public static void MapLongTermShortagesPurchasingEndpoints(this IEndpointRouteBuilder app) => app.MapGet("/api/v1/workspaces/{assignmentId:guid}/long-term-shortages/purchasing", Purchasing).WithName("GetLongTermShortagesPurchasing").WithTags("LongTermShortages").Produces<LongTermShortagePurchasingDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503);
    private static async Task<IResult> Purchasing(Guid assignmentId, string? snapshotId, string? componentPart, LongTermShortagesService service, CancellationToken ct, bool includeManufacturedParts = false, bool includePhantoms = false, int horizonWeeks = 26)
    {
        if (!Guid.TryParse(snapshotId, out var id) || string.IsNullOrWhiteSpace(componentPart) || horizonWeeks is not (13 or 26 or 52 or 72))
            return Results.ValidationProblem(new Dictionary<string, string[]> { { "request", ["Valid snapshotId, componentPart and horizonWeeks are required."] } });
        try
        {
            var result = await service.GetPurchasingAsync(assignmentId, new SnapshotId(id), componentPart,
                new LongTermShortagePopulationOptions(includeManufacturedParts, includePhantoms, false, horizonWeeks, true), ct);
            return result.Kind switch
            {
                LongTermShortagesOutcomeKind.Loaded => Results.Ok(new LongTermShortagePurchasingDto(result.CommentAvailable,
                    result.CurrentComment, result.Lines!.Select(line => new ComponentOrderLineDto(line.ComponentPart, line.Description,
                        line.LeadTimeDays, line.PoNumber, line.PoLine, line.DueDate, line.OpenQuantity, line.Confirmed,
                        line.SupplierDisplay, line.BuyerDisplay, line.ManufacturerItem, line.IsKss, line.TrackingInfo,
                        line.IsCreditHold, line.IsCia, line.CurrentComments)).ToList())),
                LongTermShortagesOutcomeKind.MpsNotLoaded => Results.Problem("MPS data has not been loaded.", statusCode: 409),
                LongTermShortagesOutcomeKind.SnapshotChanged => Results.Problem("Snapshot changed.", statusCode: 409),
                _ => Results.Problem("Selected component purchasing information is unavailable. Retry the drawer.", statusCode: 503)
            };
        }
        catch (LongTermShortagesWorkspaceNotFoundException) { return Results.NotFound(); }
    }
    public static void MapLongTermShortagesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/long-term-shortages", Get).WithName("GetLongTermShortages").WithTags("LongTermShortages").Produces<LongTermShortagesResponseDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503);
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/long-term-shortages/screen", Screen).WithName("GetLongTermShortagesScreen").WithTags("LongTermShortages").Produces<LongTermShortagesScreenDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503);
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/long-term-shortages/projection-detail", ProjectionDetail).WithName("GetLongTermShortageProjectionDetail").WithTags("LongTermShortages").Produces<LongTermShortageProjectionDetailDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503);
    }

    private static async Task<IResult> Screen(Guid assignmentId, string? snapshotId, LongTermShortagesService service,
        CancellationToken ct, bool includeManufacturedParts = false, bool includePhantoms = false, int horizonWeeks = 26)
    {
        if (!Guid.TryParse(snapshotId, out var id) || horizonWeeks is not (13 or 26 or 52 or 72))
            return Results.ValidationProblem(new Dictionary<string, string[]> { { "request", ["Valid snapshotId and horizonWeeks are required."] } });
        try
        {
            var result = await service.GetAsync(assignmentId, new SnapshotId(id),
                new(includeManufacturedParts, includePhantoms, false, horizonWeeks, true), ct);
            return result.Kind == LongTermShortagesOutcomeKind.Loaded ? Results.Ok(MapScreen(result))
                : Results.Problem(result.FailureDetail ?? "Requested shortage snapshot is unavailable.",
                    statusCode: result.Kind == LongTermShortagesOutcomeKind.Unavailable ? 503 : 409);
        }
        catch (LongTermShortagesWorkspaceNotFoundException) { return Results.NotFound(); }
    }

    private static async Task<IResult> ProjectionDetail(Guid assignmentId, string? snapshotId, string? componentPart,
        LongTermShortagesService service, bool includeManufacturedParts = false, bool includePhantoms = false, int horizonWeeks = 26)
    {
        if (!Guid.TryParse(snapshotId, out var id) || string.IsNullOrWhiteSpace(componentPart) || horizonWeeks is not (13 or 26 or 52 or 72))
            return Results.ValidationProblem(new Dictionary<string, string[]> { { "request", ["Valid snapshotId, componentPart and horizonWeeks are required."] } });
        try
        {
            var result = await service.GetProjectionDetailAsync(assignmentId, new SnapshotId(id), componentPart,
                new(includeManufacturedParts, includePhantoms, false, horizonWeeks, true));
            if (result.Kind != LongTermShortagesOutcomeKind.Loaded)
                return Results.Problem("The requested component projection is missing, replaced, or incomplete. Refresh Workspace Shortages.",
                    title: "Projection detail unavailable", type: "urn:kst:shortages:projection-detail-unavailable",
                    statusCode: result.Kind == LongTermShortagesOutcomeKind.Unavailable ? 503 : 409);
            var row = result.Rows![0];
            return Results.Ok(new LongTermShortageProjectionDetailDto(id.ToString(), row.ComponentPart,
                result.RefreshDate!.Value, result.AcquiredAtUtc!.Value, result.ConsistencyMode!,
                Array.AsReadOnly(row.DemandParentParts.ToArray()), row.Past.GrossRequirements,
                row.Past.OverdueReceipts, row.Past.ProjectedQoh, row.EffectivePmCode,
                row.ManufacturingLeadWorkingDays, row.Presentation?.ManufacturerItem));
        }
        catch (LongTermShortagesWorkspaceNotFoundException) { return Results.NotFound(); }
    }

    internal static LongTermShortagesScreenDto MapScreen(LongTermShortagesResult result)
    {
        var all = result.AllReceiptsRows!.ToDictionary(r => r.ComponentPart, StringComparer.OrdinalIgnoreCase);
        static string Display(decimal value, string? uom) => LongTermQuantityPresentation.Round(value, uom)
            .ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        static LongTermShortageScreenModeDto Mode(LongTermShortageRow row) => new(row.Severity.ToString(),
            row.FirstShortDate, row.FirstAtRiskDate, row.Episodes.Count == 0 ? 0 : row.Episodes.Max(e => e.MaximumShortage),
            row.Episodes.FirstOrDefault()?.FirstRecoveryDate, Array.AsReadOnly(row.Weeks.Select(w => w.ProjectedQoh).ToArray()),
            Array.AsReadOnly(row.Weeks.Select(w => Display(w.ProjectedQoh, row.UnitOfMeasure)).ToArray()),
            Array.AsReadOnly(row.Weeks.Select(w => w.Severity.ToString()).ToArray()));
        return new(result.SnapshotId!.Value.ToString(), result.RefreshDate!.Value, result.IsStale, result.Warning,
            result.AcquiredAtUtc!.Value, result.ConsistencyMode!,
            Array.AsReadOnly(result.Rows!.FirstOrDefault()?.Weeks.Select(w => new LongTermShortageWeekDto(
                w.WeekNumber!.Value, w.WeekStart!.Value, w.WeekStart.Value.AddDays(1))).ToArray() ?? []),
            Array.AsReadOnly(result.Rows!.Select(r => new LongTermShortageScreenComponentDto(r.ComponentPart,
                r.UnitOfMeasure, r.QadStatus, r.Description, r.Planner, r.BuyerPlannerCode, r.OpeningQoh,
                Display(r.OpeningQoh, r.UnitOfMeasure), r.SafetyStock, r.DataQualityWarning, r.Presentation?.IsKss == true,
                Mode(r), Mode(all[r.ComponentPart]))).ToArray()));
    }
    public static void MapLongTermShortagesExportEndpoints(this IEndpointRouteBuilder app) => app.MapPost("/api/v1/workspaces/{assignmentId:guid}/long-term-shortages/export", Export).WithName("ExportLongTermShortages").WithTags("LongTermShortages").Produces(StatusCodes.Status200OK).ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503);
    private static async Task<IResult> Get(Guid assignmentId, string? snapshotId, LongTermShortagesService service,
        CancellationToken ct, bool includeManufacturedParts = false, bool includePhantoms = false,
        bool includeUnconfirmed = false, int horizonWeeks = 26, bool showAll = false, bool includeEvidence = true)
    {
        if (!Guid.TryParse(snapshotId, out var id) || horizonWeeks is not (13 or 26 or 52 or 72))
            return Results.ValidationProblem(new Dictionary<string, string[]> { { "request", ["Valid snapshotId and horizonWeeks (13, 26, 52, 72) are required."] } });
        try
        {
            var r = await service.GetAsync(assignmentId, new SnapshotId(id),
                new LongTermShortagePopulationOptions(includeManufacturedParts, includePhantoms, includeUnconfirmed, horizonWeeks, showAll), ct);
            return r.Kind switch
            {
                LongTermShortagesOutcomeKind.Loaded => Results.Ok(new LongTermShortagesResponseDto(
                    r.SnapshotId!.Value.ToString(), r.RefreshDate!.Value, r.IsStale, r.Warning,
                    r.Rows!.Select(row => MapForResponse(row, includeEvidence)).ToList(), r.AcquiredAtUtc!.Value,
                    r.ConsistencyMode!, r.AllReceiptsRows!.Select(row => MapForResponse(row, includeEvidence)).ToList())
                    { EvidenceIncluded = includeEvidence }),
                LongTermShortagesOutcomeKind.MpsNotLoaded => Results.Problem("MPS data has not been loaded.", statusCode: 409),
                LongTermShortagesOutcomeKind.SnapshotChanged => Results.Problem("Snapshot changed.", statusCode: 409),
                _ => Results.Problem(r.FailureDetail ?? "Component MRP data could not be acquired. Retry the report.", statusCode: 503)
            };
        }
        catch (LongTermShortagesWorkspaceNotFoundException) { return Results.NotFound(); }
    }
    private static LongTermShortageRowDto MapForResponse(LongTermShortageRow row, bool includeEvidence) =>
        Map(includeEvidence ? row : row with { Evidence = [] });
    private static LongTermShortageRowDto Map(LongTermShortageRow row) => new(row.ComponentPart, row.UnitOfMeasure, row.QadStatus, row.Description, row.Planner, row.BuyerPlannerCode, row.OpeningQoh, row.SafetyStockState.ToString(), row.SafetyStock, row.Severity.ToString(), row.FirstShortDate, row.DemandParentParts, Map(row.Past), row.Weeks.Select(Map).ToList(), row.Evidence.Select(f => new LongTermMrpFactDto(f.EvidenceOrdinal, f.Type, f.DueDate, f.ReleaseDate, f.Quantity, f.Category.ToString(), f.SourceNumber, f.SourceLine, f.SourceLine2, f.IsPoReceipt, f.PoConfirmed, f.SourceRowId)).ToList(), row.Presentation is null ? null : new(row.Presentation.ManufacturerItem, row.Presentation.PoNumber, row.Presentation.PoLine, row.Presentation.PoDueDate, row.Presentation.PoOpenQuantity, row.Presentation.PoConfirmed, row.Presentation.IsKss), row.Episodes.Select(e => new LongTermShortageEpisodeDto(e.StartDate, e.DeepestDate, e.MaximumShortage, e.FirstRecoveryDate, e.StableClearDate)).ToList(), row.DataQualityWarning, row.FirstAtRiskDate, row.EffectivePmCode, row.PartStatusDescription, row.OrderPeriodDays, row.SafetyTimeWorkingDays, row.ManufacturingLeadWorkingDays, row.PurchasingLeadCalendarDays, row.CumulativeLeadCalendarDays, row.SitePlanningPresent);
    private static LongTermShortageBucketDto Map(LongTermShortageBucket bucket) => new(bucket.WeekNumber, bucket.WeekStart, bucket.GrossRequirements, bucket.ScheduledReceipts, bucket.PlannedOrdersDue, bucket.PlannedOrdersRelease, bucket.ProjectedQoh, bucket.Severity.ToString(), bucket.UnconfirmedReceipts, bucket.ConfirmedEnding, bucket.AllReceiptsEnding, bucket.PlanningEnding, bucket.AllReceiptsPlanningEnding, bucket.LowestProjectedBalance, bucket.OverdueReceipts, bucket.IncludesUnconfirmed, bucket.LowestConfirmedBalance, bucket.LowestAllReceiptsBalance);
    private static async Task<IResult> Export(Guid assignmentId, ExportLongTermShortagesRequestDto request, LongTermShortagesService service, IExportService exports) { if (!Guid.TryParse(request.SnapshotId, out var id) || request.ComponentParts is null || request.HorizonWeeks is not (13 or 26 or 52 or 72)) return Results.ValidationProblem(new Dictionary<string, string[]> { { "request", ["Valid snapshotId, componentParts and horizonWeeks are required."] } }); try { var result = await service.GetCachedForExportAsync(assignmentId, new SnapshotId(id), request.ComponentParts, new LongTermShortagePopulationOptions(request.IncludeManufacturedParts, request.IncludePhantoms, request.IncludeUnconfirmed, request.HorizonWeeks, request.ShowAll)); if (result.Kind != LongTermShortagesOutcomeKind.Loaded) return Results.Problem(statusCode: result.Kind is LongTermShortagesOutcomeKind.MpsNotLoaded or LongTermShortagesOutcomeKind.SnapshotChanged ? 409 : 503); var name = NormalizeFileName(result.WorkspaceName); return Results.File(exports.CreateLongTermShortagesWorkbook(name, result.RefreshDate!.Value, result.Rows!, result.AcquiredAtUtc, result.ConsistencyMode), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{name}-Shortages-{result.RefreshDate:yyyy-MM-dd}.xlsx"); } catch (LongTermShortagesWorkspaceNotFoundException) { return Results.NotFound(); } }
    private static string NormalizeFileName(string? workspaceName) { var normalized = System.Text.RegularExpressions.Regex.Replace(workspaceName ?? "workspace", "[^A-Za-z0-9]+", "-").Trim('-'); return string.IsNullOrEmpty(normalized) ? "workspace" : normalized; }
}

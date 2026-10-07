using System.Globalization;
using Kst.Api.Dtos;
using Kst.Application.PlanningWorkbook;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.PlanningWorkbook;
using Kst.Exports;

namespace Kst.Api.Endpoints;

/// <summary>
/// Stage 14.3 Planning Workbook read endpoints, Stage 14.5 saved-draft endpoints and the Stage 14.6
/// Mass Updater export endpoint. No adjustment is ever applied to QAD; the export validates against the
/// loaded MPS snapshot only (no QAD access) and returns a human-reviewable file proposal.
/// Problem Details are sanitized; no business rows, part lists, prices or SQL errors are exposed.
/// </summary>
public static class PlanningWorkbookEndpoints
{
    /// <summary>
    /// API lower bound. The owner-confirmed selectable minimum/default (12, <see cref="PlanningHorizonRange"/>)
    /// is enforced by the Planning UI; the API keeps 1 so synthetic short-horizon projections remain
    /// available. The upper bound is the dynamic MPS-derived maximum, not a fixed 72-week cap.
    /// </summary>
    public const int MinHorizonWeeks = 1;

    public static void MapPlanningWorkbookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/planning-workbook", Get)
            .WithName("GetPlanningWorkbook").WithTags("PlanningWorkbook")
            .WithSummary("Returns the workspace Planning Workbook for the supplied current MPS snapshot and horizon, reusing compatible cached source facts.")
            .Produces<PlanningWorkbookResponseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        app.MapPost("/api/v1/workspaces/{assignmentId:guid}/planning-workbook/refresh", Refresh)
            .WithName("RefreshPlanningWorkbook").WithTags("PlanningWorkbook")
            .WithSummary("Reacquires Open Orders, inventory, prices and forecast (not MPS) for the supplied current MPS snapshot.")
            .Produces<PlanningWorkbookResponseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        // Stage 14.5 saved drafts: local files only; no MPS refresh, QAD read or QAD write.
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/planning-workbook/draft/presence", DraftPresence)
            .WithName("GetPlanningWorkbookDraftPresence").WithTags("PlanningWorkbook")
            .WithSummary("Reports whether a saved Planning draft file exists for the workspace (file existence only).")
            .Produces<PlanningWorkbookDraftPresenceDto>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/planning-workbook/draft", LoadDraft)
            .WithName("LoadPlanningWorkbookDraft").WithTags("PlanningWorkbook")
            .WithSummary("Reads the saved Planning draft and compares each saved baseline with the workspace's currently loaded MPS snapshot.")
            .Produces<PlanningWorkbookDraftResponseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        app.MapPut("/api/v1/workspaces/{assignmentId:guid}/planning-workbook/draft", SaveDraft)
            .WithName("SavePlanningWorkbookDraft").WithTags("PlanningWorkbook")
            .WithSummary("Saves signed Planning adjustments with their loaded MPS baselines. Does not apply anything to QAD.")
            .Produces<PlanningWorkbookDraftResponseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        app.MapDelete("/api/v1/workspaces/{assignmentId:guid}/planning-workbook/draft", DeleteDraft)
            .WithName("DeletePlanningWorkbookDraft").WithTags("PlanningWorkbook")
            .WithSummary("Deletes the saved Planning draft file. In-memory proposals are not affected.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        // Stage 14.6: validates against the workspace's loaded MPS snapshot only. No QAD read or write,
        // no MPS refresh, no submission or import; saved drafts are never read or changed.
        app.MapPost("/api/v1/workspaces/{assignmentId:guid}/planning-workbook/mass-updater-export", ExportMassUpdater)
            .WithName("ExportPlanningMassUpdaterCsv").WithTags("PlanningWorkbook")
            .WithSummary("Validates every staged adjustment against the complete loaded MPS snapshot and returns the QAD Mass Updater CSV (Part, signed adjustment, Monday date; no header). Any invalid entry rejects the whole export. Nothing is applied to QAD.")
            .Produces<PlanningMassUpdaterExportResponseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> ExportMassUpdater(Guid assignmentId, ExportPlanningMassUpdaterRequestDto request,
        MpsMassUpdateService service, CancellationToken ct)
    {
        if (request is null || request.MpsSnapshotId == Guid.Empty || string.IsNullOrWhiteSpace(request.Site) ||
            request.Entries is null || request.Entries.Any(e => e is null))
            return MassUpdaterProblem(StatusCodes.Status400BadRequest, "invalid-request", 0);
        try
        {
            var entries = request.Entries.Select(e => new MpsAdjustmentEntry(e.Part, e.WeekLabel, e.Adjustment)).ToList();
            var result = await service.PrepareAsync(assignmentId, request.Site, new SnapshotId(request.MpsSnapshotId), entries, ct);
            if (result.Outcome != MpsMassUpdateOutcome.Ready)
                return MassUpdaterProblem(result.Outcome switch
                {
                    MpsMassUpdateOutcome.UnknownWorkspace => StatusCodes.Status404NotFound,
                    MpsMassUpdateOutcome.MpsNotLoaded or MpsMassUpdateOutcome.SnapshotChanged or MpsMassUpdateOutcome.SiteChanged =>
                        StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status400BadRequest
                }, result.IssueCode ?? "export-blocked", result.AffectedRowCount);

            // No CSV bytes exist until every staged entry passed validation against the loaded snapshot.
            var bytes = MpsMassUpdateCsv.Create(result.Rows!);
            return Results.Ok(new PlanningMassUpdaterExportResponseDto(
                MpsMassUpdateCsv.SuggestedFileName(result.WorkspaceLabel, result.BusinessDate!.Value),
                Convert.ToBase64String(bytes), result.Rows!.Count, result.MpsSnapshotId!.Value.ToString(), request.Site));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return MassUpdaterProblem(StatusCodes.Status500InternalServerError, "export-failed", 0); }
    }

    // Only issue categories and counts cross the boundary; never return rows, parts or quantities.
    private static IResult MassUpdaterProblem(int status, string code, int affectedRows) =>
        Results.Problem(title: "Mass Updater export blocked", statusCode: status,
            extensions: new Dictionary<string, object?> { ["issueCode"] = code, ["affectedRowCount"] = affectedRows });

    private static async Task<IResult> DraftPresence(Guid assignmentId, PlanningWorkbookDraftService drafts, CancellationToken ct)
    {
        try { return Results.Ok(new PlanningWorkbookDraftPresenceDto(await drafts.ExistsAsync(assignmentId, ct))); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem(title: "Draft presence could not be checked", statusCode: StatusCodes.Status503ServiceUnavailable); }
    }

    private static async Task<IResult> LoadDraft(Guid assignmentId, Guid mpsSnapshotId, PlanningWorkbookDraftService drafts, CancellationToken ct)
    {
        if (mpsSnapshotId == Guid.Empty)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["mpsSnapshotId"] = ["A valid current MPS snapshot ID is required."] });
        try { return DraftResult(await drafts.LoadAsync(assignmentId, new SnapshotId(mpsSnapshotId), ct)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem(title: "Planning draft could not be read", statusCode: StatusCodes.Status503ServiceUnavailable); }
    }

    private static async Task<IResult> SaveDraft(Guid assignmentId, SavePlanningWorkbookDraftRequestDto request,
        PlanningWorkbookDraftService drafts, CancellationToken ct)
    {
        if (request is null || request.MpsSnapshotId == Guid.Empty || request.Entries is null || request.Entries.Any(e => e is null))
            return Results.Problem(title: "Invalid draft", statusCode: StatusCodes.Status400BadRequest);
        try
        {
            var entries = request.Entries.Select(e => new PlanningDraftSaveEntry(e.Part, e.WeekLabel, e.Adjustment)).ToList();
            return DraftResult(await drafts.SaveAsync(assignmentId, new SnapshotId(request.MpsSnapshotId), entries, ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem(title: "Planning draft could not be saved", statusCode: StatusCodes.Status503ServiceUnavailable); }
    }

    private static async Task<IResult> DeleteDraft(Guid assignmentId, PlanningWorkbookDraftService drafts, CancellationToken ct)
    {
        try { await drafts.DeleteAsync(assignmentId, ct); return Results.NoContent(); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return Results.Problem(title: "Planning draft could not be removed", statusCode: StatusCodes.Status503ServiceUnavailable); }
    }

    private static IResult DraftResult(PlanningDraftResult result) => result.Kind switch
    {
        PlanningDraftOutcomeKind.Loaded => Results.Ok(new PlanningWorkbookDraftResponseDto(
            true, false, result.Warning, result.Draft!.SavedAtUtc, result.Draft.MpsSnapshotId, result.Draft.Site,
            result.Rows!.Select(r => new PlanningDraftRowDto(
                r.Saved.Part, r.Saved.WeekLabel, r.Saved.Adjustment, Text(r.Saved.SavedMpsQuantity),
                Text(r.CurrentMps), r.BaselineChanged, Text(r.ProposedMps), r.IssueCode)).ToArray())),
        PlanningDraftOutcomeKind.NoDraft => Results.Ok(new PlanningWorkbookDraftResponseDto(false, false, null, null, null, null, [])),
        PlanningDraftOutcomeKind.Corrupt => Results.Ok(new PlanningWorkbookDraftResponseDto(true, true, result.Warning, null, null, null, [])),
        PlanningDraftOutcomeKind.UnknownWorkspace => Results.Problem(title: "Workspace not found", statusCode: StatusCodes.Status404NotFound),
        PlanningDraftOutcomeKind.MpsNotLoaded => Results.Problem(
            title: "MPS data not loaded", detail: "Load this workspace's MPS data before using Planning drafts.",
            statusCode: StatusCodes.Status409Conflict),
        PlanningDraftOutcomeKind.MpsSnapshotChanged => Results.Problem(
            title: "MPS snapshot changed", detail: "Reload the workspace's current MPS snapshot before using Planning drafts.",
            statusCode: StatusCodes.Status409Conflict),
        PlanningDraftOutcomeKind.ExistingDraftCorrupt => Results.Problem(
            title: "Saved draft is corrupt", detail: "The existing saved draft could not be read and was kept for recovery. Delete it before saving a new draft.",
            statusCode: StatusCodes.Status409Conflict,
            extensions: new Dictionary<string, object?> { ["code"] = "draft-corrupt" }),
        PlanningDraftOutcomeKind.Invalid => Results.Problem(
            title: "Invalid draft", statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?> { ["code"] = result.InvalidReason }),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    // Both query parameters are non-nullable so OpenAPI marks them required. A missing or
    // malformed value is rejected by parameter binding with 400 before the handler runs.
    private static Task<IResult> Get(Guid assignmentId, Guid mpsSnapshotId, int horizonWeeks,
        PlanningWorkbookService service, CancellationToken ct) =>
        Handle(assignmentId, mpsSnapshotId, horizonWeeks, service, false, ct);

    private static Task<IResult> Refresh(Guid assignmentId, Guid mpsSnapshotId, int horizonWeeks,
        PlanningWorkbookService service, CancellationToken ct) =>
        Handle(assignmentId, mpsSnapshotId, horizonWeeks, service, true, ct);

    private static async Task<IResult> Handle(Guid id, Guid mpsSnapshotId, int horizonWeeks,
        PlanningWorkbookService service, bool refresh, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (mpsSnapshotId == Guid.Empty)
            errors["mpsSnapshotId"] = ["A valid current MPS snapshot ID is required."];
        if (horizonWeeks < MinHorizonWeeks)
            errors["horizonWeeks"] = [$"horizonWeeks is required and must be at least {MinHorizonWeeks}."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        var expected = new SnapshotId(mpsSnapshotId);
        var result = refresh
            ? await service.RefreshAsync(id, expected, horizonWeeks, ct)
            : await service.GetAsync(id, expected, horizonWeeks, ct);
        return result.Kind switch
        {
            PlanningWorkbookOutcomeKind.Loaded => Results.Ok(ToDto(result)),
            PlanningWorkbookOutcomeKind.UnknownWorkspace => Results.Problem(
                title: "Workspace not found", statusCode: StatusCodes.Status404NotFound),
            PlanningWorkbookOutcomeKind.MpsNotLoaded => Results.Problem(
                title: "MPS data not loaded", detail: "Load this workspace's MPS data before viewing the Planning Workbook.",
                statusCode: StatusCodes.Status409Conflict),
            PlanningWorkbookOutcomeKind.MpsSnapshotChanged => Results.Problem(
                title: "MPS snapshot changed", detail: "Reload the workspace's current MPS snapshot before viewing the Planning Workbook.",
                statusCode: StatusCodes.Status409Conflict),
            PlanningWorkbookOutcomeKind.HorizonOutOfRange => Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["horizonWeeks"] = [$"horizonWeeks must not exceed {result.MaxHorizonWeeks} for the loaded MPS snapshot."]
                },
                extensions: new Dictionary<string, object?> { ["maximumHorizonWeeks"] = result.MaxHorizonWeeks }),
            PlanningWorkbookOutcomeKind.Unavailable => Results.Problem(
                title: "Planning Workbook unavailable",
                detail: "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
                statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static PlanningWorkbookResponseDto ToDto(PlanningWorkbookResult result)
    {
        var entry = result.Entry!;
        var snapshot = entry.Snapshot;
        var workbook = result.Workbook!;
        return new(
            snapshot.WorkspaceId, snapshot.Site,
            snapshot.Id.ToString(), snapshot.MpsSnapshotId.ToString(), snapshot.OpenOrdersSnapshotId.ToString(),
            snapshot.AcquiredAtUtc, snapshot.PriceEffectiveDate, "firstTier",
            workbook.Today, result.HorizonWeeks, entry.IsStale, entry.Warning,
            workbook.WeekLabels, workbook.Parts.Select(ToDto).ToArray(),
            PlanningHorizonRange.MinimumWeeks, result.MaxHorizonWeeks);
    }

    private static PlanningPartBlockDto ToDto(PlanningPartBlock block) => new(
        block.Part, block.Description, Text(block.NetQuantityOnHand),
        block.PriceTiers.Select(t => new PlanningPriceTierDto(Text(t.MinimumOrderQuantity), Text(t.UnitPrice))).ToArray(),
        Text(block.SelectedPrice), block.IsPriced,
        ToDto(block.Falldown), block.Weeks.Select(ToDto).ToArray(),
        new PlanningUndatedDto(Text(block.Undated.SalesOrderQuantity), Text(block.Undated.SalesOrderDollars), block.Undated.LineCount));

    private static PlanningBucketDto ToDto(PlanningBucket bucket) => new(
        bucket.Kind == MpsBucketKind.Falldown ? "falldown" : "weekly", bucket.WeekLabel,
        Text(bucket.SalesOrderQuantity), Text(bucket.SalesOrderDollars),
        Text(bucket.Mps), Text(bucket.MpsDollars),
        Text(bucket.Forecast), Text(bucket.EstimatedOnHand));

    private static string Text(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string? Text(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);
}

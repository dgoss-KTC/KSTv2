using Kst.Api.Dtos;
using Kst.Application.WorkOrders;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.WorkOrders;

namespace Kst.Api.Endpoints;

/// <summary>
/// Lazy-loaded Stage 7/7R Work Orders and Kitting drill-down endpoints. Never triggers an MPS load;
/// the workspace's MPS snapshot must already be current, and every request must supply the snapshot id
/// it was shown so a stale UI context is never silently combined with a newer snapshot (accepted
/// contract §17-21).
/// </summary>
public static class WorkOrderEndpoints
{
    public static void MapWorkOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/work-orders/planning-window", GetPlanningWindow)
            .WithName("GetPlanningWindowWorkOrders")
            .WithSummary("Returns the parent-scoped four-week Work Order planning window (Due-Date-based Falldown plus Week 0-3 under the active Due/Release basis), optionally narrowed to one bucket.")
            .WithTags("WorkOrders")
            .Produces<WorkOrderPlanningWindowResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/work-orders/{woid}/material", GetMaterialLines)
            .WithName("GetWorkOrderMaterialLines")
            .WithSummary("Returns lazily-loaded material/kitting lines and Kitting Summary for one work order.")
            .WithTags("WorkOrders")
            .Produces<WorkOrderMaterialResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/work-orders/candidates", GetCandidates)
            .WithName("GetWorkOrderCandidates")
            .WithSummary("Returns the complete Stage 7R planning-window population for a manufactured component authorized through the Work Order drill-down.")
            .WithTags("WorkOrders")
            .Produces<WorkOrderCandidateResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> GetPlanningWindow(
        Guid assignmentId,
        string? snapshotId,
        string? parentPart,
        string? dateBasis,
        string? bucketKind,
        DateOnly? weekLabel,
        WorkOrderDrilldownService service,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        var parsedSnapshotId = TryParseSnapshotId(snapshotId, errors);
        if (string.IsNullOrWhiteSpace(parentPart))
            errors["parentPart"] = ["parentPart is required."];
        var parsedDateBasis = TryParseDateBasis(dateBasis, errors);
        var (parsedBucketKind, parsedWeekLabel) = TryParseBucket(bucketKind, weekLabel, errors);

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var today = DateOnly.FromDateTime(clock.LocalNow.Date);
        try
        {
            var result = await service.GetPlanningWindowAsync(
                assignmentId, parsedSnapshotId, parentPart!, parsedDateBasis, parsedBucketKind, parsedWeekLabel, today, cancellationToken);
            return ToResult(result);
        }
        catch (WorkOrderDrilldownWorkspaceNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetMaterialLines(
        Guid assignmentId,
        string woid,
        string? snapshotId,
        WorkOrderDrilldownService service,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var parsedSnapshotId = TryParseSnapshotId(snapshotId, errors);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        try
        {
            var result = await service.GetMaterialLinesAsync(assignmentId, parsedSnapshotId, woid, cancellationToken);
            return ToResult(result, woid);
        }
        catch (WorkOrderDrilldownWorkspaceNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetCandidates(
        Guid assignmentId,
        string? snapshotId,
        string? immediateParentWoid,
        string? componentPart,
        int? targetDepth,
        string? dateBasis,
        WorkOrderDrilldownService service,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var parsedSnapshotId = TryParseSnapshotId(snapshotId, errors);
        if (string.IsNullOrWhiteSpace(immediateParentWoid))
            errors["immediateParentWoid"] = ["immediateParentWoid is required."];
        if (string.IsNullOrWhiteSpace(componentPart))
            errors["componentPart"] = ["componentPart is required."];
        if (targetDepth is null)
            errors["targetDepth"] = ["targetDepth is required."];
        var parsedDateBasis = TryParseDateBasis(dateBasis, errors);

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        try
        {
            var result = await service.GetCandidatesAsync(
                assignmentId, parsedSnapshotId, immediateParentWoid!, componentPart!, targetDepth!.Value, parsedDateBasis,
                DateOnly.FromDateTime(clock.LocalNow.Date), cancellationToken);
            return ToResult(result);
        }
        catch (WorkOrderDrilldownWorkspaceNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["targetDepth"] = [ex.Message]
            });
        }
    }

    private static SnapshotId TryParseSnapshotId(string? snapshotId, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(snapshotId) || !Guid.TryParse(snapshotId, out var parsed))
        {
            errors["snapshotId"] = ["snapshotId is required and must be a valid GUID."];
            return default;
        }

        return new SnapshotId(parsed);
    }

    /// <summary>
    /// Parses the optional bucket filter. Absent <c>bucketKind</c> means the full parent-level
    /// planning window. <c>weekly</c> requires a <c>weekLabel</c>; <c>falldown</c> takes none.
    /// </summary>
    private static (MpsBucketKind? BucketKind, DateOnly? WeekLabel) TryParseBucket(
        string? bucketKind, DateOnly? weekLabel, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(bucketKind))
            return (null, null);

        var kind = bucketKind.Trim().ToLowerInvariant() switch
        {
            "falldown" => MpsBucketKind.Falldown,
            "weekly" => MpsBucketKind.Weekly,
            _ => (MpsBucketKind?)null
        };

        if (kind is null)
        {
            errors["bucketKind"] = ["bucketKind must be 'falldown' or 'weekly'."];
            return (MpsBucketKind.Weekly, null);
        }

        if (kind == MpsBucketKind.Weekly)
        {
            if (weekLabel is null)
            {
                errors["weekLabel"] = ["weekLabel is required when bucketKind is 'weekly'."];
                return (kind, null);
            }
            return (kind, weekLabel);
        }

        return (kind, null);
    }

    private static MpsDateBasis TryParseDateBasis(string? dateBasis, Dictionary<string, string[]> errors)
    {
        var parsed = string.IsNullOrWhiteSpace(dateBasis)
            ? MpsDateBasis.DueDate
            : dateBasis.Trim().ToLowerInvariant() switch
            {
                "duedate" => MpsDateBasis.DueDate,
                "releasedate" => MpsDateBasis.ReleaseDate,
                _ => (MpsDateBasis?)null
            };

        if (parsed is null)
            errors["dateBasis"] = ["dateBasis must be 'dueDate' or 'releaseDate'."];

        return parsed ?? MpsDateBasis.DueDate;
    }

    private static IResult ToResult(WorkOrderPlanningWindowResult result) => result.Kind switch
    {
        WorkOrderPlanningWindowOutcomeKind.Loaded => Results.Ok(new WorkOrderPlanningWindowResponseDto(
            result.SnapshotId!.Value.ToString(),
            result.WorkOrders!.Select(ToDto).ToList())),

        WorkOrderPlanningWindowOutcomeKind.MpsNotLoaded => MpsNotLoadedProblem(),
        WorkOrderPlanningWindowOutcomeKind.SnapshotChanged => SnapshotChangedProblem(),

        WorkOrderPlanningWindowOutcomeKind.PartNotInScope => Results.Problem(
            title: "Part not in workspace scope",
            detail: "The requested part is not in this workspace's current MPS parent scope.",
            statusCode: StatusCodes.Status404NotFound),

        WorkOrderPlanningWindowOutcomeKind.BucketNotFound => Results.Problem(
            title: "Bucket not found",
            detail: "The requested bucket was not found in the current MPS schedule for this part.",
            statusCode: StatusCodes.Status404NotFound),

        WorkOrderPlanningWindowOutcomeKind.Unavailable => UnavailableProblem(),

        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static IResult ToResult(WorkOrderMaterialResult result, string woid) => result.Kind switch
    {
        WorkOrderMaterialOutcomeKind.Loaded => Results.Ok(new WorkOrderMaterialResponseDto(
            result.SnapshotId!.Value.ToString(),
            Woid: woid,
            ToDto(result.Kitting!),
            result.Lines!.Select(ToDto).ToList())),

        WorkOrderMaterialOutcomeKind.MpsNotLoaded => MpsNotLoadedProblem(),
        WorkOrderMaterialOutcomeKind.SnapshotChanged => SnapshotChangedProblem(),
        WorkOrderMaterialOutcomeKind.Unavailable => UnavailableProblem(),

        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static IResult ToResult(WorkOrderCandidateResult result) => result.Kind switch
    {
        WorkOrderCandidateOutcomeKind.Loaded => Results.Ok(new WorkOrderCandidateResponseDto(
            result.SnapshotId!.Value.ToString(),
            result.Candidates!.Select(ToDto).ToList())),

        WorkOrderCandidateOutcomeKind.MpsNotLoaded => MpsNotLoadedProblem(),
        WorkOrderCandidateOutcomeKind.SnapshotChanged => SnapshotChangedProblem(),

        WorkOrderCandidateOutcomeKind.WorkOrderNotFound => Results.Problem(
            title: "Work order not found",
            detail: "The immediate parent work order was not found.",
            statusCode: StatusCodes.Status404NotFound),

        WorkOrderCandidateOutcomeKind.ComponentNotManufactured => Results.Problem(
            title: "Component not manufactured",
            detail: "The requested component is not a manufactured part, so it has no subassembly work orders to drill into.",
            statusCode: StatusCodes.Status409Conflict),

        WorkOrderCandidateOutcomeKind.Unavailable => UnavailableProblem(),

        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static IResult MpsNotLoadedProblem() => Results.Problem(
        title: "MPS data not loaded",
        detail: "This workspace's MPS data has not been loaded yet. Load the MPS dashboard before viewing work orders.",
        statusCode: StatusCodes.Status409Conflict);

    private static IResult SnapshotChangedProblem() => Results.Problem(
        title: "Snapshot changed",
        detail: "This workspace's MPS snapshot has changed since the requested snapshot id was shown. Refresh and retry.",
        statusCode: StatusCodes.Status409Conflict);

    private static IResult UnavailableProblem() => Results.Problem(
        title: "Work order information unavailable",
        detail: "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
        statusCode: StatusCodes.Status503ServiceUnavailable);

    private static WorkOrderSummaryDto ToDto(WorkOrderSummary summary) => new(
        summary.PartNumber,
        summary.Woid,
        ToStatusString(summary.Status),
        summary.OrderedQuantity,
        summary.CompletedQuantity,
        summary.OpenQuantity,
        summary.ReleaseDate,
        summary.DueDate,
        summary.SalesOrder,
        ToDto(summary.Kitting));

    private static KittingSummaryDto ToDto(KittingSummary kitting) => new(
        kitting.ApplicableLineCount,
        kitting.FullyIssuedLineCount,
        kitting.KittingPercent);

    private static WorkOrderMaterialLineDto ToDto(WorkOrderMaterialLine line) => new(
        line.ComponentPart,
        line.ComponentDescription,
        line.RequiredQuantity,
        line.IssuedQuantity,
        line.VarianceQuantity,
        line.IssuedPercent,
        ToIssueStatusString(line.IssueStatus),
        line.IsManufactured,
        line.IsFullyIssued);

    /// <summary>
    /// Maps a raw QAD status code to its API presentation value. Known codes (A/F/R) receive
    /// friendly lowercase labels; any other non-closed code passes through as its raw value so a
    /// previously unseen status renders safely instead of failing (Stage 7R).
    /// </summary>
    private static string ToStatusString(string rawStatus)
    {
        var trimmed = rawStatus.Trim();
        return trimmed.ToLowerInvariant() switch
        {
            "a" => "allocating",
            "f" => "frozen",
            "r" => "released",
            _ => trimmed
        };
    }

    private static string? ToIssueStatusString(WorkOrderIssueStatus? status) => status switch
    {
        WorkOrderIssueStatus.UnderIssuedException => "underIssuedException",
        WorkOrderIssueStatus.WithinExpectedRange => "withinExpectedRange",
        WorkOrderIssueStatus.OverIssuedException => "overIssuedException",
        null => null,
        _ => "unknown"
    };
}

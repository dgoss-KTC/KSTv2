using Kst.Api.Dtos;
using Kst.Application.Mps;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.Snapshots;

namespace Kst.Api.Endpoints;

/// <summary>
/// Per-workspace MPS dashboard endpoints. Never returns raw QAD records; unavailable QAD access
/// with no usable prior snapshot surfaces as Problem Details rather than an empty 200 payload.
/// </summary>
public static class MpsEndpoints
{
    private const int MinHorizonWeeks = 1;
    private const int MaxHorizonWeeks = 72;
    private const int DefaultHorizonWeeks = 12;

    public static void MapMpsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/mps", GetMpsDashboard)
            .WithName("GetMpsDashboard")
            .WithSummary("Returns the projected MPS dashboard for a workspace, auto-loading from QAD on first access.")
            .WithTags("Mps")
            .Produces<MpsDashboardResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost("/api/v1/workspaces/{assignmentId:guid}/mps/refresh", RefreshMpsDashboard)
            .WithName("RefreshMpsDashboard")
            .WithSummary("Forces one reload of MPS source facts from QAD for the workspace and returns the projected dashboard.")
            .WithTags("Mps")
            .Produces<MpsDashboardResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> GetMpsDashboard(
        Guid assignmentId,
        string? dateBasis,
        int? horizonWeeks,
        MpsWorkspaceSnapshotService service,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryParseParameters(dateBasis, horizonWeeks, out var basis, out var horizon, out var validationError))
            return validationError!;

        var today = DateOnly.FromDateTime(clock.LocalNow.Date);
        try
        {
            var result = await service.GetDashboardAsync(assignmentId, basis, horizon, today, cancellationToken);
            return ToResult(result, assignmentId, basis, horizon);
        }
        catch (MpsWorkspaceNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> RefreshMpsDashboard(
        Guid assignmentId,
        string? dateBasis,
        int? horizonWeeks,
        MpsWorkspaceSnapshotService service,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryParseParameters(dateBasis, horizonWeeks, out var basis, out var horizon, out var validationError))
            return validationError!;

        var today = DateOnly.FromDateTime(clock.LocalNow.Date);
        try
        {
            var result = await service.RefreshAsync(assignmentId, basis, horizon, today, cancellationToken);
            return ToResult(result, assignmentId, basis, horizon);
        }
        catch (MpsWorkspaceNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static bool TryParseParameters(
        string? dateBasis,
        int? horizonWeeks,
        out MpsDateBasis basis,
        out int horizon,
        out IResult? validationError)
    {
        var errors = new Dictionary<string, string[]>();

        MpsDateBasis? parsedBasis = string.IsNullOrWhiteSpace(dateBasis)
            ? MpsDateBasis.DueDate
            : dateBasis.Trim().ToLowerInvariant() switch
            {
                "duedate" => MpsDateBasis.DueDate,
                "releasedate" => MpsDateBasis.ReleaseDate,
                _ => null
            };

        if (parsedBasis is null)
            errors["dateBasis"] = ["dateBasis must be 'dueDate' or 'releaseDate'."];

        horizon = horizonWeeks ?? DefaultHorizonWeeks;
        if (horizon < MinHorizonWeeks || horizon > MaxHorizonWeeks)
            errors["horizonWeeks"] = [$"horizonWeeks must be between {MinHorizonWeeks} and {MaxHorizonWeeks}."];

        if (errors.Count > 0)
        {
            basis = MpsDateBasis.DueDate;
            validationError = Results.ValidationProblem(errors);
            return false;
        }

        basis = parsedBasis!.Value;
        validationError = null;
        return true;
    }

    private static IResult ToResult(MpsDashboardResult result, Guid assignmentId, MpsDateBasis basis, int horizon)
    {
        if (result.Snapshot is null)
        {
            return Results.Problem(
                title: "MPS data unavailable",
                detail: "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var snapshotDto = new MpsSnapshotMetadataDto(
            SnapshotId: result.Snapshot.Id.ToString(),
            CreatedAtUtc: result.Snapshot.LoadedAt,
            LastSuccessfulRefreshAtUtc: result.Snapshot.LoadedAt,
            Status: ToStatusString(result.Status),
            WorkspaceId: assignmentId,
            Site: result.Snapshot.Site,
            ResolvedParentPartCount: result.Snapshot.ResolvedParts.Count,
            SourceRowCount: result.Snapshot.SourceRows.Count,
            IsRefreshInProgress: result.IsRefreshInProgress,
            LastRefreshError: result.ErrorMessage);

        return Results.Ok(new MpsDashboardResponseDto(
            snapshotDto,
            ToDateBasisString(basis),
            horizon,
            result.Schedules.Select(ToDto).ToList()));
    }

    private static MpsPartScheduleDto ToDto(MpsPartSchedule schedule) => new(
        schedule.ParentPart,
        schedule.Description,
        schedule.Buckets.Select(ToDto).ToList());

    private static MpsBucketDto ToDto(MpsBucket bucket) => new(
        bucket.Kind switch
        {
            MpsBucketKind.Falldown => "falldown",
            MpsBucketKind.Weekly => "weekly",
            _ => "unknown"
        },
        bucket.WeekLabel,
        bucket.Quantity,
        bucket.ExecutionStatus switch
        {
            MpsExecutionStatus.None => "none",
            MpsExecutionStatus.Allocating => "allocating",
            MpsExecutionStatus.Frozen => "frozen",
            MpsExecutionStatus.Released => "released",
            MpsExecutionStatus.Mixed => "mixed",
            _ => "unknown"
        },
        bucket.ContainsPlannedWork,
        bucket.ContainsExplicitlyScheduledWork);

    private static string ToDateBasisString(MpsDateBasis basis) => basis switch
    {
        MpsDateBasis.DueDate => "dueDate",
        MpsDateBasis.ReleaseDate => "releaseDate",
        _ => "unknown"
    };

    private static string ToStatusString(SnapshotStatus status) => status switch
    {
        SnapshotStatus.NotLoaded => "notLoaded",
        SnapshotStatus.Loading => "loading",
        SnapshotStatus.Current => "current",
        SnapshotStatus.Stale => "stale",
        SnapshotStatus.Partial => "partial",
        SnapshotStatus.Failed => "failed",
        _ => "unknown"
    };
}

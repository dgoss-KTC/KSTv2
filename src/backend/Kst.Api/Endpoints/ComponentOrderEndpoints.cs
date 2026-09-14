using Kst.Api.Dtos;
using Kst.Application.ComponentOrders;
using Kst.Domain.Common;
using Kst.Domain.ComponentOrders;
using System.Diagnostics;

namespace Kst.Api.Endpoints;

/// <summary>
/// Stage 10 Component Orders endpoint. Never triggers an MPS load: the workspace's MPS snapshot
/// must already be current, and every request must supply the snapshot id it was shown so a stale
/// UI context is never silently combined with a newer snapshot (accepted contract §17-21 pattern).
/// </summary>
public static class ComponentOrderEndpoints
{
    public static void MapComponentOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/component-orders", GetComponentOrders)
            .WithName("GetComponentOrders")
            .WithSummary("Returns the active workspace's component-grouped conventional open PO lines for its MPS snapshot's BOM-derived component scope.")
            .WithTags("ComponentOrders")
            .Produces<ComponentOrdersResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> GetComponentOrders(
        Guid assignmentId,
        string? snapshotId,
        ComponentOrdersService service,
        IClock clock,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Kst.Api.Endpoints.ComponentOrderEndpoints");
        var errors = new Dictionary<string, string[]>();
        var parsedSnapshotId = TryParseSnapshotId(snapshotId, errors);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Stage 10 component-orders request accepted. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId}",
            assignmentId, parsedSnapshotId.Value);

        try
        {
            var result = await service.GetComponentOrdersAsync(
                assignmentId, parsedSnapshotId, DateOnly.FromDateTime(clock.LocalNow.Date), cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "Stage 10 component-orders request completed. WorkspaceId={WorkspaceId} SnapshotId={SnapshotId} Outcome={Outcome} ElapsedMs={ElapsedMs}",
                assignmentId, parsedSnapshotId.Value, result.Kind, stopwatch.ElapsedMilliseconds);
            return ToResult(result);
        }
        catch (ComponentOrdersWorkspaceNotFoundException)
        {
            return Results.NotFound();
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

    private static IResult ToResult(ComponentOrdersResult result) => result.Kind switch
    {
        ComponentOrdersOutcomeKind.Loaded => Results.Ok(new ComponentOrdersResponseDto(
            result.SnapshotId!.Value.ToString(),
            result.EnrichmentAvailability.ToString(),
            result.Groups!
                .Select(group => new ComponentOrderGroupDto(
                    group.ComponentPart,
                    ToDto(group.DisplayLine),
                    group.AdditionalLines.Select(ToDto).ToList()))
                .ToList())),

        ComponentOrdersOutcomeKind.MpsNotLoaded => MpsNotLoadedProblem(),
        ComponentOrdersOutcomeKind.SnapshotChanged => SnapshotChangedProblem(),
        ComponentOrdersOutcomeKind.Unavailable => UnavailableProblem(),

        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static IResult MpsNotLoadedProblem() => Results.Problem(
        title: "MPS data not loaded",
        detail: "This workspace's MPS data has not been loaded yet. Load the MPS dashboard before viewing component orders.",
        statusCode: StatusCodes.Status409Conflict);

    private static IResult SnapshotChangedProblem() => Results.Problem(
        title: "Snapshot changed",
        detail: "This workspace's MPS snapshot has changed since the requested snapshot id was shown. Refresh and retry.",
        statusCode: StatusCodes.Status409Conflict);

    private static IResult UnavailableProblem() => Results.Problem(
        title: "Component orders unavailable",
        detail: "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
        statusCode: StatusCodes.Status503ServiceUnavailable);

    private static ComponentOrderLineDto ToDto(ComponentOrderLine line) => new(
        line.ComponentPart,
        line.Description,
        line.LeadTimeDays,
        line.PoNumber,
        line.PoLine,
        line.DueDate,
        line.OpenQuantity,
        line.Confirmed,
        line.SupplierDisplay,
        line.BuyerDisplay,
        line.ManufacturerItem,
        line.IsKss,
        line.TrackingInfo,
        line.IsCreditHold,
        line.IsCia,
        line.CurrentComments);
}

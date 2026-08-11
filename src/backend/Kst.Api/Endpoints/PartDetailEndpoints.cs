using Kst.Api.Dtos;
using Kst.Application.PartDetail;

namespace Kst.Api.Endpoints;

/// <summary>
/// Lazy-loaded Part Info drill-down endpoint for a workspace's selected MPS parent part. Never
/// triggers an MPS load; the workspace's MPS snapshot must already be current (see
/// <see cref="PartDetailOutcomeKind.MpsNotLoaded"/>). See the accepted Stage 6 contract §14-15 for
/// the full route/response/error shape this implements.
/// </summary>
public static class PartDetailEndpoints
{
    public static void MapPartDetailEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/part-detail", GetPartDetail)
            .WithName("GetPartDetail")
            .WithSummary("Returns lazily-loaded Part Info for a workspace's selected MPS parent part.")
            .WithTags("PartDetail")
            .Produces<PartDetailResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> GetPartDetail(
        Guid assignmentId,
        string? partNumber,
        PartDetailService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(partNumber))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["partNumber"] = ["partNumber is required."]
            });
        }

        try
        {
            var result = await service.GetPartDetailAsync(assignmentId, partNumber, cancellationToken);
            return ToResult(result);
        }
        catch (PartDetailWorkspaceNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static IResult ToResult(PartDetailResult result) => result.Kind switch
    {
        PartDetailOutcomeKind.Loaded => Results.Ok(ToDto(result.Detail!)),

        PartDetailOutcomeKind.MpsNotLoaded => Results.Problem(
            title: "MPS data not loaded",
            detail: "This workspace's MPS data has not been loaded yet. Load the MPS dashboard before viewing part information.",
            statusCode: StatusCodes.Status409Conflict),

        PartDetailOutcomeKind.OutOfScope => Results.Problem(
            title: "Part not in workspace scope",
            detail: "The requested part is not in this workspace's current MPS parent scope.",
            statusCode: StatusCodes.Status404NotFound),

        PartDetailOutcomeKind.MissingPart => Results.Problem(
            title: "Part not found",
            detail: "No QAD part master record was found for the requested part.",
            statusCode: StatusCodes.Status404NotFound),

        PartDetailOutcomeKind.Unavailable => Results.Problem(
            title: "Part information unavailable",
            detail: "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
            statusCode: StatusCodes.Status503ServiceUnavailable),

        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static PartDetailResponseDto ToDto(Kst.Application.PartDetail.PartDetail detail) => new(
        Site: detail.Site,
        PartNumber: detail.PartNumber,
        PlannerCode: detail.PlannerCode,
        ManufacturingLeadTimeDays: detail.ManufacturingLeadTimeDays,
        SafetyTimeDays: detail.SafetyTimeDays,
        PartStatusCode: detail.PartStatusCode,
        PartStatusDescription: detail.PartStatusDescription,
        CurrentRevision: detail.CurrentRevision,
        Description: detail.Description,
        IosCode: detail.IosCode,
        SafetyStockQuantity: detail.SafetyStockQuantity,
        QuantityOnHand: detail.QuantityOnHand,
        QuantityNonNet: detail.QuantityNonNet,
        QuantityRmaOnHand: detail.QuantityRmaOnHand,
        PriceBreaks: detail.PriceBreaks.Select(b => new PartPriceBreakDto(b.MinimumOrderQuantity, b.UnitPrice)).ToList(),
        LoadedAtUtc: detail.LoadedAtUtc,
        IsStale: detail.IsStale,
        Warning: detail.Warning);
}

using Kst.Api.Dtos;
using Kst.Application.ComponentDetail;

namespace Kst.Api.Endpoints;

/// <summary>
/// Lazy-loaded Stage 8D.5 Component Detail endpoint for a workspace-selected component part.
/// Never triggers an MPS load; the workspace's MPS snapshot must already be current (see
/// <see cref="ComponentDetailOutcomeKind.MpsNotLoaded"/>). The caller supplies only the workspace
/// identity and the component part — never site, domain, or any source date. Component identity
/// is established solely by a <c>pt_mstr</c> row; the endpoint is never gated by BOM occurrence
/// or the workspace's resolved MPS parent scope.
/// </summary>
public static class ComponentDetailEndpoints
{
    public static void MapComponentDetailEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/components/{componentPart}", GetComponentDetail)
            .WithName("GetComponentDetail")
            .WithSummary("Returns lazily-loaded Component Detail (master, selected-site planning, Standard Cost, QCTC, shared Site + Part inventory) for a workspace-selected component part.")
            .WithTags("ComponentDetail")
            .Produces<ComponentDetailResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> GetComponentDetail(
        Guid assignmentId,
        string componentPart,
        ComponentDetailService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(componentPart))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["componentPart"] = ["componentPart is required."]
            });
        }

        try
        {
            var result = await service.GetComponentDetailAsync(assignmentId, componentPart, cancellationToken);
            return ToResult(result);
        }
        catch (ComponentWorkspaceNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static IResult ToResult(ComponentDetailResult result) => result.Kind switch
    {
        ComponentDetailOutcomeKind.Loaded => Results.Ok(ToDto(result.Detail!)),

        ComponentDetailOutcomeKind.MpsNotLoaded => Results.Problem(
            title: "MPS data not loaded",
            detail: "This workspace's MPS data has not been loaded yet. Load the MPS dashboard before viewing component information.",
            statusCode: StatusCodes.Status409Conflict),

        ComponentDetailOutcomeKind.NotFound => Results.Problem(
            title: "Component not found",
            detail: "No QAD part master record was found for the requested component.",
            statusCode: StatusCodes.Status404NotFound),

        ComponentDetailOutcomeKind.Unavailable => Results.Problem(
            title: "Component information unavailable",
            detail: "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
            statusCode: StatusCodes.Status503ServiceUnavailable),

        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static ComponentDetailResponseDto ToDto(ComponentDetail detail) => new(
        Site: detail.Site,
        ComponentPart: detail.ComponentPart,
        Description: detail.Description,
        PartStatusCode: detail.PartStatusCode,
        PartStatusDescription: detail.PartStatusDescription,
        IosCode: detail.IosCode,
        NetQuantityOnHand: detail.NetQuantityOnHand,
        NonNetQuantityOnHand: detail.NonNetQuantityOnHand,
        StandardCost: detail.StandardCost,
        Qctc: detail.Qctc,
        TimeFence: detail.TimeFence,
        SafetyTime: detail.SafetyTime,
        SafetyStock: detail.SafetyStock,
        BuyerPlanner: detail.BuyerPlanner,
        PurchaseLeadTimeDays: detail.PurchaseLeadTimeDays,
        InspectionLeadTimeDays: detail.InspectionLeadTimeDays,
        CumulativeLeadTimeDays: detail.CumulativeLeadTimeDays,
        MinimumOrderQuantity: detail.MinimumOrderQuantity,
        OrderMultiple: detail.OrderMultiple,
        LoadedAtUtc: detail.LoadedAtUtc,
        IsStale: detail.IsStale,
        Warning: detail.Warning);
}

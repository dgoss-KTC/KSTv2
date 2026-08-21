using Kst.Api.Dtos;
using Kst.Application.ApprovedVendors;

namespace Kst.Api.Endpoints;

/// <summary>
/// Lazy-loaded Stage 8D.7 Approved Vendors endpoint for a workspace-selected component part.
/// Independent of Component Detail: never gated by MPS-loaded state, no cache/freshness coupling.
/// The caller supplies only the workspace identity and the component part — never Site or Domain.
/// A nonexistent component part naturally returns 200 with an empty collection, identical to a
/// real zero-AVL component (see the accepted Stage 8D.7 grain/existence decision).
/// </summary>
public static class ApprovedVendorEndpoints
{
    public static void MapApprovedVendorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/v1/workspaces/{assignmentId:guid}/components/{componentPart}/approved-vendors",
                GetApprovedVendors)
            .WithName("GetApprovedVendors")
            .WithSummary("Returns the lazily-loaded Approved Vendor List (AVL) for a workspace-selected component part.")
            .WithTags("ApprovedVendors")
            .Produces<IReadOnlyList<ApprovedVendorDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> GetApprovedVendors(
        Guid assignmentId,
        string componentPart,
        ApprovedVendorService service,
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
            var result = await service.GetApprovedVendorsAsync(assignmentId, componentPart, cancellationToken);
            return ToResult(result);
        }
        catch (ApprovedVendorWorkspaceNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static IResult ToResult(ApprovedVendorResult result) => result.Kind switch
    {
        ApprovedVendorOutcomeKind.Loaded => Results.Ok(result.Vendors!.Select(ToDto).ToList()),

        ApprovedVendorOutcomeKind.Unavailable => Results.Problem(
            title: "Approved vendors unavailable",
            detail: "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
            statusCode: StatusCodes.Status503ServiceUnavailable),

        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static ApprovedVendorDto ToDto(Kst.Domain.ApprovedVendors.ApprovedVendor vendor) => new(
        Supplier: vendor.Supplier,
        VendorName: vendor.VendorName,
        SupplierItem: vendor.SupplierItem,
        ManufacturerPart: vendor.ManufacturerPart);
}

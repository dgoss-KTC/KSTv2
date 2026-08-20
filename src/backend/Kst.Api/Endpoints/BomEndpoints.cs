using Kst.Api.Dtos;
using Kst.Application.Bom;

namespace Kst.Api.Endpoints;

/// <summary>
/// Lazy-loaded Stage 8 scheduler-visible BOM endpoint for a workspace's selected MPS parent
/// part. Never triggers an MPS load; the workspace's MPS snapshot must already be current (see
/// <see cref="BomOutcomeKind.MpsNotLoaded"/>). The caller supplies only the workspace identity
/// and the parent part — never site, domain, effective date, or P/M rules. A valid in-scope
/// parent with no effective structural rows (or no P/M-visible rows) returns 200 with an empty
/// lines array; a QAD failure never masquerades as an empty BOM.
/// </summary>
public static class BomEndpoints
{
    public static void MapBomEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/parts/{parentPart}/bom", GetBom)
            .WithName("GetBom")
            .WithSummary("Returns the scheduler-visible current-effective BOM for a workspace's selected MPS parent part, enriched with shared Site + Part inventory.")
            .WithTags("Bom")
            .Produces<BomResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> GetBom(
        Guid assignmentId,
        string parentPart,
        BomService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parentPart))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["parentPart"] = ["parentPart is required."]
            });
        }

        try
        {
            var result = await service.GetBomAsync(assignmentId, parentPart, cancellationToken);
            return ToResult(result);
        }
        catch (BomWorkspaceNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static IResult ToResult(BomResult result) => result.Kind switch
    {
        BomOutcomeKind.Loaded => Results.Ok(ToDto(result.Bom!)),

        BomOutcomeKind.MpsNotLoaded => Results.Problem(
            title: "MPS data not loaded",
            detail: "This workspace's MPS data has not been loaded yet. Load the MPS dashboard before viewing the BOM.",
            statusCode: StatusCodes.Status409Conflict),

        BomOutcomeKind.OutOfScope => Results.Problem(
            title: "Part not in workspace scope",
            detail: "The requested part is not in this workspace's current MPS parent scope.",
            statusCode: StatusCodes.Status404NotFound),

        BomOutcomeKind.Unavailable => Results.Problem(
            title: "BOM information unavailable",
            detail: "Database currently unavailable. Please try again in a few minutes. If the problem continues, please contact IT.",
            statusCode: StatusCodes.Status503ServiceUnavailable),

        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static BomResponseDto ToDto(Bom bom) => new(
        Site: bom.Site,
        ParentPart: bom.ParentPart,
        EffectiveDate: bom.EffectiveDate,
        Lines: bom.Lines.Select(ToDto).ToList(),
        LoadedAtUtc: bom.LoadedAtUtc,
        IsStale: bom.IsStale,
        Warning: bom.Warning);

    private static BomLineDto ToDto(BomLine line) => new(
        OccurrenceKey: line.OccurrenceKey,
        Level: line.Level,
        ComponentPart: line.ComponentPart,
        PmCode: line.PmCode,
        IsPhantom: line.IsPhantom,
        Description: line.Description,
        QuantityPer: line.QuantityPer,
        ScrapPercentage: line.ScrapPercentage,
        NetQuantityOnHand: line.NetQuantityOnHand,
        NonNetQuantityOnHand: line.NonNetQuantityOnHand);
}

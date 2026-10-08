using Kst.Api.Dtos;
using Kst.Application.InventoryLocations;

namespace Kst.Api.Endpoints;

public static class InventoryLocationsEndpoints
{
    public static void MapInventoryLocationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces/{assignmentId:guid}/components/{componentPart}/inventory-locations",
            async (Guid assignmentId, string componentPart, string mpsSnapshotId, InventoryLocationsService service, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(componentPart) || !Guid.TryParse(mpsSnapshotId, out _))
                    return Results.Problem(title: "Invalid inventory locations request", statusCode: 400);
                var result = await service.GetAsync(assignmentId, componentPart, mpsSnapshotId, ct);
                if (result.Report is { } report)
                    return Results.Ok(new InventoryLocationsResponseDto(report.WorkspaceId, report.Site, report.ComponentPart,
                        report.MpsSnapshotId, report.AcquiredAtUtc, report.IsStale, report.Warning,
                        report.Rows.Select(r => new InventoryLocationRowDto(r.RowId, r.Location, r.LotSerial, r.Status, r.QuantityOnHand)).ToArray()));
                return result.Outcome switch
                {
                    InventoryLocationsOutcome.WorkspaceMissing => Results.Problem(title: "Workspace not found", statusCode: 404),
                    InventoryLocationsOutcome.SnapshotChanged => Results.Problem(title: "MPS snapshot changed", detail: "Reload inventory locations against the current workspace snapshot.", statusCode: 409),
                    _ => Results.Problem(title: "Inventory locations unavailable", detail: "Inventory locations could not be acquired. Please retry.", statusCode: 503)
                };
            }).WithName("GetInventoryLocations").WithTags("ComponentDetail")
            .WithSummary("Acquires all physical inventory rows for the selected component and workspace site.")
            .Produces<InventoryLocationsResponseDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503);
    }
}

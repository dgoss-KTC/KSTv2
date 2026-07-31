using Kst.Api.Dtos;
using Kst.Application.Workspaces;
using Kst.Domain.Workspaces;

namespace Kst.Api.Endpoints;

/// <summary>
/// Workspace configuration endpoints.
/// </summary>
public static class WorkspaceEndpoints
{
    public static void MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/workspaces", ListWorkspaces)
            .WithName("ListWorkspaces")
            .WithSummary("Returns the saved workspace list and any nonfatal configuration warning.")
            .WithTags("Workspaces")
            .Produces<WorkspaceListResponseDto>();

        app.MapPost("/api/v1/workspaces", CreateWorkspace)
            .WithName("CreateWorkspace")
            .WithSummary("Creates and persists a new workspace configuration.")
            .WithTags("Workspaces")
            .Produces<WorkspaceAssignmentDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ListWorkspaces(IWorkspaceConfigurationService service)
    {
        var result = await service.GetWorkspacesAsync();
        return Results.Ok(new WorkspaceListResponseDto(
            result.Workspaces.Select(ToDto).ToList(),
            result.ConfigurationWarning));
    }

    private static async Task<IResult> CreateWorkspace(
        CreateWorkspaceRequestDto request,
        IWorkspaceConfigurationService service)
    {
        var command = new CreateWorkspaceCommand(
            DisplayName: request.DisplayName,
            Site: request.Site,
            CustomerNumber: request.CustomerNumber,
            ProductLineFrom: request.ProductLineFrom,
            ProductLineTo: request.ProductLineTo,
            IsTemporary: request.IsTemporary,
            CoverageEndsOn: request.CoverageEndsOn);

        var result = await service.CreateWorkspaceAsync(command);

        if (!result.IsSuccess)
        {
            return Results.ValidationProblem(
                result.ValidationErrors!
                    .GroupBy(e => e.Field)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.Message).ToArray()));
        }

        return Results.Created(
            $"/api/v1/workspaces/{result.Workspace!.AssignmentId}",
            ToDto(result.Workspace));
    }

    private static WorkspaceAssignmentDto ToDto(WorkspaceAssignment w) =>
        new(w.AssignmentId, w.DisplayName, w.Site, w.CustomerNumber,
            w.ProductLineFrom, w.ProductLineTo, w.IsTemporary,
            w.CoverageEndsOn, w.IsEnabled, w.SortOrder);
}

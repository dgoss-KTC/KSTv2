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

        app.MapPut("/api/v1/workspaces/{assignmentId:guid}", UpdateWorkspace)
            .WithName("UpdateWorkspace")
            .WithSummary("Updates and persists an existing workspace configuration.")
            .WithTags("Workspaces")
            .Produces<WorkspaceAssignmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("/api/v1/workspaces/{assignmentId:guid}/archive", ArchiveWorkspace)
            .WithName("ArchiveWorkspace")
            .WithSummary("Archives a workspace so it no longer appears as an active tab (isEnabled = false).")
            .WithTags("Workspaces")
            .Produces<WorkspaceAssignmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("/api/v1/workspaces/{assignmentId:guid}/restore", RestoreWorkspace)
            .WithName("RestoreWorkspace")
            .WithSummary("Restores a previously archived workspace (isEnabled = true).")
            .WithTags("Workspaces")
            .Produces<WorkspaceAssignmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapDelete("/api/v1/workspaces/{assignmentId:guid}", DeleteWorkspace)
            .WithName("DeleteWorkspace")
            .WithSummary("Permanently deletes a workspace assignment from configuration.")
            .WithTags("Workspaces")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapDelete("/api/v1/workspaces", ResetWorkspaces)
            .WithName("ResetWorkspaces")
            .WithSummary("Removes all workspace configuration and returns to the empty startup state.")
            .WithTags("Workspaces")
            .Produces(StatusCodes.Status204NoContent);

        app.MapPut("/api/v1/workspaces/order", ReorderWorkspaces)
            .WithName("ReorderWorkspaces")
            .WithSummary("Persists a new tab order for the currently active (enabled) workspaces.")
            .WithTags("Workspaces")
            .Produces<WorkspaceListResponseDto>(StatusCodes.Status200OK)
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

    private static async Task<IResult> UpdateWorkspace(
        Guid assignmentId,
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

        var result = await service.UpdateWorkspaceAsync(assignmentId, command);

        if (result.NotFound)
            return Results.NotFound();

        if (!result.IsSuccess)
        {
            return Results.ValidationProblem(
                result.ValidationErrors!
                    .GroupBy(e => e.Field)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.Message).ToArray()));
        }

        return Results.Ok(ToDto(result.Workspace!));
    }

    private static async Task<IResult> ArchiveWorkspace(Guid assignmentId, IWorkspaceConfigurationService service)
    {
        var result = await service.ArchiveWorkspaceAsync(assignmentId);
        return result.NotFound ? Results.NotFound() : Results.Ok(ToDto(result.Workspace!));
    }

    private static async Task<IResult> RestoreWorkspace(Guid assignmentId, IWorkspaceConfigurationService service)
    {
        var result = await service.RestoreWorkspaceAsync(assignmentId);
        return result.NotFound ? Results.NotFound() : Results.Ok(ToDto(result.Workspace!));
    }

    private static async Task<IResult> DeleteWorkspace(Guid assignmentId, IWorkspaceConfigurationService service)
    {
        var result = await service.DeleteWorkspaceAsync(assignmentId);
        return result.NotFound ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> ResetWorkspaces(IWorkspaceConfigurationService service)
    {
        await service.ResetWorkspacesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ReorderWorkspaces(
        ReorderWorkspacesRequestDto request,
        IWorkspaceConfigurationService service)
    {
        var command = new ReorderWorkspacesCommand(request.AssignmentIds);
        var result = await service.ReorderWorkspacesAsync(command);

        if (!result.IsSuccess)
        {
            return Results.ValidationProblem(
                result.ValidationErrors!
                    .GroupBy(e => e.Field)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.Message).ToArray()));
        }

        return Results.Ok(new WorkspaceListResponseDto(
            result.Workspaces!.Select(ToDto).ToList(),
            ConfigurationWarning: null));
    }

    private static WorkspaceAssignmentDto ToDto(WorkspaceAssignment w) =>
        new(w.AssignmentId, w.DisplayName, w.Site, w.CustomerNumber,
            w.ProductLineFrom, w.ProductLineTo, w.IsTemporary,
            w.CoverageEndsOn, w.IsEnabled, w.SortOrder);
}

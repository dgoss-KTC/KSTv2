using Kst.Api.Dtos;
using Kst.Application.LongTermShortages;
using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;
using Kst.Exports.Contracts;

namespace Kst.Api.Endpoints;

/// <summary>Protected preparation over cached projection rows only. No source acquisition.</summary>
public static class WorkspaceShortagesExportEndpoints
{
    private const string Kind = "WorkspaceShortages";

    public static void MapWorkspaceShortagesExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/reports/workspace-shortages", Prepare).WithName("PrepareWorkspaceShortagesWorkbook")
            .WithTags("Exports").Produces<AnalyticalExportPreparedDto>().ProducesProblem(400)
            .ProducesProblem(409).ProducesProblem(422).ProducesProblem(503);
        app.MapGet("/api/v1/reports/workspace-shortages/preparations/{id:guid}/bytes", (Guid id, HttpRequest http, ComponentMrpPreparations store) =>
        {
            var owner = http.Headers["X-KST-Report-Session"].ToString();
            if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A transport session is required.", statusCode: 400);
            var bytes = store.Get(id, owner, Kind);
            return bytes is null ? Results.Problem("Preparation expired. Prepare again.", statusCode: 410)
                : Results.Bytes(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }).WithName("GetWorkspaceShortagesWorkbookBytes").WithTags("Exports")
            .Produces(200, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").ProducesProblem(410);
    }

    private static async Task<IResult> Prepare(WorkspaceShortagesExportRequestDto request, HttpRequest http,
        IWorkspaceConfigurationService workspaces, IMpsSnapshotStore snapshots, LongTermShortagesService service,
        IExportService exports, ComponentMrpPreparations preparations, IClock clock, CancellationToken ct)
    {
        var owner = http.Headers["X-KST-Report-Session"].ToString();
        var selection = request.Selection;
        if (!Guid.TryParseExact(owner, "D", out _) || selection is null ||
            !Guid.TryParse(selection.SnapshotId, out var snapshotId) || snapshotId != request.MpsSnapshotId ||
            selection.HorizonWeeks is not (13 or 26 or 52 or 72) || selection.ComponentParts is null ||
            selection.ComponentParts.Count == 0 || selection.ComponentParts.Any(string.IsNullOrWhiteSpace) ||
            selection.ComponentParts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != selection.ComponentParts.Count)
            return Results.Problem("A valid unique displayed component selection is required.", statusCode: 400);
        if (!await preparations.Generations.WaitAsync(0, ct)) return Results.Problem("Workbook preparation is busy. Retry.", statusCode: 503);
        try
        {
            var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == request.WorkspaceId);
            var snapshot = snapshots.GetState(request.WorkspaceId).Snapshot;
            if (workspace is null || snapshot?.Id.Value != snapshotId || workspace.Site != request.Site || snapshot.Site != request.Site ||
                request.BusinessDate != DateOnly.FromDateTime(clock.LocalNow.Date))
                return Results.Problem("Workspace, snapshot or business date changed. Reload the view.", statusCode: 409);
            var result = await service.GetCachedForExportAsync(request.WorkspaceId, new SnapshotId(snapshotId), selection.ComponentParts,
                new LongTermShortagePopulationOptions(selection.IncludeManufacturedParts, selection.IncludePhantoms,
                    selection.IncludeUnconfirmed, selection.HorizonWeeks, selection.ShowAll));
            if (result.Kind != LongTermShortagesOutcomeKind.Loaded || result.AcquiredAtUtc != request.AcquiredAtUtc)
                return Results.Problem("The loaded projection is missing, replaced or incompatible. Reload Workspace Shortages.", statusCode: 409);
            var byPart = result.Rows!.ToDictionary(r => r.ComponentPart, StringComparer.OrdinalIgnoreCase);
            var rows = selection.ComponentParts.Select(part => byPart[part]).ToArray();
            var name = System.Text.RegularExpressions.Regex.Replace(result.WorkspaceName ?? "workspace", "[^A-Za-z0-9]+", "-").Trim('-');
            if (name.Length == 0) name = "workspace";
            ct.ThrowIfCancellationRequested();
            var bytes = exports.CreateLongTermShortagesWorkbook(name, result.RefreshDate!.Value, rows, result.AcquiredAtUtc, result.ConsistencyMode);
            ct.ThrowIfCancellationRequested();
            var current = await service.GetCachedForExportAsync(request.WorkspaceId, new SnapshotId(snapshotId), selection.ComponentParts,
                new LongTermShortagePopulationOptions(selection.IncludeManufacturedParts, selection.IncludePhantoms,
                    selection.IncludeUnconfirmed, selection.HorizonWeeks, selection.ShowAll));
            if (snapshots.GetState(request.WorkspaceId).Snapshot?.Id != snapshot.Id || request.BusinessDate != DateOnly.FromDateTime(clock.LocalNow.Date) ||
                current.Kind != LongTermShortagesOutcomeKind.Loaded || current.AcquiredAtUtc != request.AcquiredAtUtc)
                return Results.Problem("Source or business date changed while preparing. Prepare again.", statusCode: 409);
            var stored = preparations.Add(owner, bytes, Kind);
            return Results.Ok(new AnalyticalExportPreparedDto(stored.Id.ToString(), $"{name}-Shortages-{result.RefreshDate:yyyy-MM-dd}.xlsx", stored.Expires));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (LongTermShortagesWorkspaceNotFoundException) { return Results.Problem("Workspace changed. Reload the view.", statusCode: 409); }
        catch (ArgumentException) { return Results.Problem("Workbook inputs exceed supported capacity. Nothing was truncated.", statusCode: 422); }
        catch (InvalidOperationException) { return Results.Problem("Workbook exceeds preparation capacity.", statusCode: 422); }
        catch (Exception) { return Results.Problem("Workspace Shortages preparation failed. No file was saved.", statusCode: 503); }
        finally { preparations.Generations.Release(); }
    }
}

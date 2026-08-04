using Kst.Api.Dtos;
using Kst.Application.Refresh;
using Kst.Application.SystemStatus;
using Kst.Domain.Snapshots;

namespace Kst.Api.Endpoints;

/// <summary>
/// System status and refresh endpoints consumed by the frontend.
/// </summary>
public static class SystemEndpoints
{
    public static void MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/system/status", GetSystemStatus)
            .WithName("GetSystemStatus")
            .WithSummary("Returns typed system status for the frontend walking skeleton.")
            .WithTags("System")
            .Produces<SystemStatusResponse>();

        app.MapPost("/api/v1/system/refresh", PostRefresh)
            .WithName("PostSystemRefresh")
            .WithSummary("Triggers a refresh cycle across all registered data sources and returns the resulting status.")
            .WithTags("System")
            .Produces<SystemStatusResponse>();
    }

    private static IResult GetSystemStatus(GetSystemStatusQuery query)
    {
        var result = query.Execute();
        return Results.Ok(ToResponse(result));
    }

    private static async Task<IResult> PostRefresh(
        RefreshCoordinator refreshCoordinator,
        GetSystemStatusQuery query,
        CancellationToken cancellationToken)
    {
        await refreshCoordinator.RefreshAsync(cancellationToken);
        var result = query.Execute();
        return Results.Ok(ToResponse(result));
    }

    private static SystemStatusResponse ToResponse(SystemStatusResult result)
    {
        var snapshotDto = new SnapshotStatusDto(
            Available: result.Snapshot.IsAvailable,
            SnapshotId: result.Snapshot.IsAvailable
                ? result.Snapshot.Id.ToString()
                : null,
            CreatedAt: result.Snapshot.IsAvailable
                ? result.Snapshot.CreatedAt
                : null,
            Status: result.Snapshot.Status switch
            {
                SnapshotStatus.NotLoaded => "notLoaded",
                SnapshotStatus.Loading => "loading",
                SnapshotStatus.Current => "current",
                SnapshotStatus.Stale => "stale",
                SnapshotStatus.Partial => "partial",
                SnapshotStatus.Failed => "failed",
                _ => "unknown"
            }
        );

        var dataSourceDtos = result.DataSources
            .Select(ds => new DataSourceDto(
                Name: ds.Name,
                Status: ds.Status switch
                {
                    DataSourceStatus.NotConfigured => "notConfigured",
                    DataSourceStatus.Loading => "loading",
                    DataSourceStatus.Current => "current",
                    DataSourceStatus.Stale => "stale",
                    DataSourceStatus.Failed => "failed",
                    DataSourceStatus.Unavailable => "unavailable",
                    _ => "unknown"
                }
            ))
            .ToList();

        return new SystemStatusResponse(
            ApplicationName: result.ApplicationName,
            ApplicationVersion: result.ApplicationVersion,
            BackendFramework: result.BackendFramework,
            BackendInstanceId: result.BackendInstanceId,
            StartedAt: result.StartedAt,
            CurrentTime: result.CurrentTime,
            Snapshot: snapshotDto,
            DataSources: dataSourceDtos,
            LastRefreshAttemptAt: result.LastRefreshAttemptAt,
            LastSuccessfulRefreshAt: result.LastSuccessfulRefreshAt
        );
    }
}

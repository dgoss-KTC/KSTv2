using Kst.Api.Dtos;
using Kst.Application.SystemStatus;
using Kst.Domain.Snapshots;

namespace Kst.Api.Endpoints;

/// <summary>
/// System status endpoint consumed by the frontend.
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
    }

    private static IResult GetSystemStatus(GetSystemStatusQuery query)
    {
        var result = query.Execute();

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
                SnapshotStatus.Loaded => "loaded",
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
                    DataSourceStatus.Connecting => "connecting",
                    DataSourceStatus.Connected => "connected",
                    DataSourceStatus.Unavailable => "unavailable",
                    _ => "unknown"
                }
            ))
            .ToList();

        return Results.Ok(new SystemStatusResponse(
            ApplicationName: result.ApplicationName,
            ApplicationVersion: result.ApplicationVersion,
            BackendFramework: result.BackendFramework,
            BackendInstanceId: result.BackendInstanceId,
            StartedAt: result.StartedAt,
            CurrentTime: result.CurrentTime,
            Snapshot: snapshotDto,
            DataSources: dataSourceDtos
        ));
    }
}

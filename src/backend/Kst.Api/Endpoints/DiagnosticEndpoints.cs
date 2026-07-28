using Microsoft.AspNetCore.Mvc;
using Kst.Api.Dtos;
using Kst.Application.Snapshots;
using Kst.Infrastructure.Identity;

namespace Kst.Api.Endpoints;

/// <summary>
/// Health and readiness endpoints. These are intentionally lightweight
/// and do not depend on full application initialization.
/// </summary>
public static class DiagnosticEndpoints
{
    public static void MapDiagnosticEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", GetHealth)
            .WithName("GetHealth")
            .WithSummary("Returns liveness status of the backend process.")
            .WithTags("Diagnostics")
            .Produces<HealthResponse>();

        app.MapGet("/ready", GetReady)
            .WithName("GetReady")
            .WithSummary("Returns readiness status once initialization is complete.")
            .WithTags("Diagnostics")
            .Produces<ReadyResponse>();
    }

    private static IResult GetHealth(
        ISnapshotStore snapshotStore,
        IConfiguration configuration)
    {
        var version = configuration["AppVersion"] ?? "0.1.0";

        return Results.Ok(new HealthResponse(
            Status: "healthy",
            Application: "KST",
            BackendVersion: version,
            ProcessId: Environment.ProcessId,
            InstanceId: ApplicationInstanceId.Value,
            Timestamp: DateTimeOffset.Now
        ));
    }

    private static IResult GetReady(
        ISnapshotStore snapshotStore,
        IHostApplicationLifetime lifetime)
    {
        var snapshot = snapshotStore.GetCurrentSnapshot();

        return Results.Ok(new ReadyResponse(
            Status: "ready",
            Initialized: true,
            SnapshotAvailable: snapshot.IsAvailable,
            Timestamp: DateTimeOffset.Now
        ));
    }
}

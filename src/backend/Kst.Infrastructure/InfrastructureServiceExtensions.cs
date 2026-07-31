using Microsoft.Extensions.DependencyInjection;
using Kst.Domain.Common;
using Kst.Application.Snapshots;
using Kst.Application.Workspaces;
using Kst.Infrastructure.Clock;
using Kst.Infrastructure.Snapshots;
using Kst.Infrastructure.Configuration;
using Kst.Infrastructure.Workspaces;

namespace Kst.Infrastructure;

/// <summary>
/// Registers all infrastructure services with the DI container.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? localAppDataOverride = null)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
        services.AddSingleton(new LocalAppDataPaths(localAppDataOverride));
        services.AddSingleton<IWorkspaceConfigurationStore, JsonWorkspaceConfigurationStore>();
        return services;
    }
}

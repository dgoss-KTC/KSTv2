using Microsoft.Extensions.DependencyInjection;
using Kst.Domain.Common;
using Kst.Application.Preferences;
using Kst.Application.Refresh;
using Kst.Application.Snapshots;
using Kst.Application.SystemStatus;
using Kst.Application.Workspaces;
using Kst.Infrastructure.Clock;
using Kst.Infrastructure.Preferences;
using Kst.Infrastructure.Snapshots;
using Kst.Infrastructure.SystemStatus;
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
        services.AddSingleton<IDataSourceStatusStore, InMemoryDataSourceStatusStore>();
        services.AddSingleton<IRefreshHistoryStore, InMemoryRefreshHistoryStore>();
        services.AddSingleton(new LocalAppDataPaths(localAppDataOverride));
        services.AddSingleton<IWorkspaceConfigurationStore, JsonWorkspaceConfigurationStore>();
        services.AddSingleton<IPreferencesStore, JsonPreferencesStore>();
        return services;
    }
}

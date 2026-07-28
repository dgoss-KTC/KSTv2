using Microsoft.Extensions.DependencyInjection;
using Kst.Domain.Common;
using Kst.Application.Snapshots;
using Kst.Infrastructure.Clock;
using Kst.Infrastructure.Snapshots;
using Kst.Infrastructure.Configuration;

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
        return services;
    }
}

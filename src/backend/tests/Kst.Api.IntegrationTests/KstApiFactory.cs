using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kst.Application.Workspaces;
using Kst.Domain.Workspaces;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Test factory that configures the API to use a random loopback port,
/// suppresses external dependencies, and runs without a real database.
/// </summary>
public sealed class KstApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the JSON file store with an in-memory store for tests.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IWorkspaceConfigurationStore));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IWorkspaceConfigurationStore, InMemoryWorkspaceStore>();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }
}

/// <summary>
/// In-memory workspace store for integration tests. Isolates tests from the file system.
/// </summary>
internal sealed class InMemoryWorkspaceStore : IWorkspaceConfigurationStore
{
    private List<WorkspaceAssignment> _workspaces = [];

    public Task<WorkspaceLoadResult> LoadAsync() =>
        Task.FromResult(new WorkspaceLoadResult(_workspaces, null));

    public Task SaveAsync(IReadOnlyList<WorkspaceAssignment> workspaces)
    {
        _workspaces = [..workspaces];
        return Task.CompletedTask;
    }
}


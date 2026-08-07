using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kst.Application.Preferences;
using Kst.Application.Workspaces;
using Kst.Domain.Preferences;
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

        // Program.cs reads QadDatabase into a local variable at the top level, before the
        // test host's Build() runs - so ConfigureAppConfiguration/UseSetting overrides arrive
        // too late to affect it. Environment variables are read by WebApplication.CreateBuilder
        // itself, so setting them here (before the factory invokes Program's entry point)
        // reliably forces "not configured" regardless of what real dev values later land in
        // appsettings.json.
        Environment.SetEnvironmentVariable("QadDatabase__Server", "");
        Environment.SetEnvironmentVariable("QadDatabase__Database", "");

        builder.ConfigureServices(services =>
        {
            // Replace the JSON file stores with in-memory stores for tests.
            var workspaceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IWorkspaceConfigurationStore));
            if (workspaceDescriptor is not null)
                services.Remove(workspaceDescriptor);

            services.AddSingleton<IWorkspaceConfigurationStore, InMemoryWorkspaceStore>();

            var preferencesDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IPreferencesStore));
            if (preferencesDescriptor is not null)
                services.Remove(preferencesDescriptor);

            services.AddSingleton<IPreferencesStore, InMemoryPreferencesStore>();
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
        _workspaces = [.. workspaces];
        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory preferences store for integration tests. Isolates tests from the file system.
/// </summary>
internal sealed class InMemoryPreferencesStore : IPreferencesStore
{
    private UserPreferences _preferences = UserPreferences.Default;

    public Task<PreferencesLoadResult> LoadAsync() =>
        Task.FromResult(new PreferencesLoadResult(_preferences, null));

    public Task SaveAsync(UserPreferences preferences)
    {
        _preferences = preferences;
        return Task.CompletedTask;
    }
}


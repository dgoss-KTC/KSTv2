using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kst.Application.ApprovedVendors;
using Kst.Application.Bom;
using Kst.Application.ComponentDetail;
using Kst.Application.Inventory;
using Kst.Application.Preferences;
using Kst.Application.Workspaces;
using Kst.Domain.Preferences;
using Kst.Domain.Workspaces;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Test factory that configures the API to use a random loopback port,
/// suppresses external dependencies, and runs without a real database.
///
/// Stage 8D.3: optional deterministic overrides for the BOM reader bridges (set the properties
/// before creating the client). QAD is never configured in tests, so Program.cs registers
/// throwing delegates for IBomSourceReader / IPartInventoryReader; setting a fake here replaces
/// them (same descriptor removal/replacement pattern as the workspace/preferences stores) so
/// endpoint tests can exercise the 200/empty/503/stale paths without a live QAD.
/// The factory must keep exactly one public (parameterless) constructor because xunit
/// constructs IClassFixture&lt;KstApiFactory&gt; through it.
/// </summary>
public sealed class KstApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Optional deterministic <see cref="IBomSourceReader"/> override (Stage 8D.3).</summary>
    public IBomSourceReader? BomSourceReader { get; set; }

    /// <summary>Optional deterministic <see cref="IPartInventoryReader"/> override (Stage 8D.3).</summary>
    public IPartInventoryReader? PartInventoryReader { get; set; }

    /// <summary>Optional deterministic <see cref="IComponentSourceReader"/> override (Stage 8D.5).</summary>
    public IComponentSourceReader? ComponentSourceReader { get; set; }

    /// <summary>Optional deterministic <see cref="IApprovedVendorSourceReader"/> override (Stage 8D.7).</summary>
    public IApprovedVendorSourceReader? ApprovedVendorSourceReader { get; set; }

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

            // Stage 8D.3: optional deterministic BOM reader overrides for endpoint tests.
            var bomSourceReader = BomSourceReader;
            if (bomSourceReader is not null)
            {
                var bomReaderDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IBomSourceReader));
                if (bomReaderDescriptor is not null)
                    services.Remove(bomReaderDescriptor);
                services.AddSingleton(bomSourceReader);
            }

            var partInventoryReader = PartInventoryReader;
            if (partInventoryReader is not null)
            {
                var inventoryReaderDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IPartInventoryReader));
                if (inventoryReaderDescriptor is not null)
                    services.Remove(inventoryReaderDescriptor);
                services.AddSingleton(partInventoryReader);
            }

            // Stage 8D.5: optional deterministic Component Detail source reader override for endpoint tests.
            var componentSourceReader = ComponentSourceReader;
            if (componentSourceReader is not null)
            {
                var componentSourceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IComponentSourceReader));
                if (componentSourceDescriptor is not null)
                    services.Remove(componentSourceDescriptor);
                services.AddSingleton(componentSourceReader);
            }

            // Stage 8D.7: optional deterministic Approved Vendor source reader override for endpoint tests.
            var approvedVendorSourceReader = ApprovedVendorSourceReader;
            if (approvedVendorSourceReader is not null)
            {
                var approvedVendorSourceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IApprovedVendorSourceReader));
                if (approvedVendorSourceDescriptor is not null)
                    services.Remove(approvedVendorSourceDescriptor);
                services.AddSingleton(approvedVendorSourceReader);
            }
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


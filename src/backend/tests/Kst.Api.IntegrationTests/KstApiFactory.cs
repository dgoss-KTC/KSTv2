using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kst.Application.ApprovedVendors;
using Kst.Application.Bom;
using Kst.Application.ComponentOrders;
using Kst.Application.ComponentDetail;
using Kst.Application.Inventory;
using Kst.Application.InventoryLocations;
using Kst.Application.FinishedGoods;
using Kst.Application.LongTermShortages;
using Kst.Application.OpenOrders;
using Kst.Application.PlanningWorkbook;

using Kst.Application.Preferences;
using Kst.Application.Workspaces;
using Kst.Application.Shortages;
using Kst.Application.WorkOrders;
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
    public IInventoryLocationReader? InventoryLocationReader { get; set; }

    /// <summary>Optional deterministic <see cref="IComponentSourceReader"/> override (Stage 8D.5).</summary>
    public IComponentSourceReader? ComponentSourceReader { get; set; }

    /// <summary>Optional deterministic <see cref="IApprovedVendorSourceReader"/> override (Stage 8D.7).</summary>
    public IApprovedVendorSourceReader? ApprovedVendorSourceReader { get; set; }
    public ICommittedWorkOrderPopulationReader? CommittedWorkOrderPopulationReader { get; set; }
    public IHardAllocationReader? HardAllocationReader { get; set; }
    public IInventoryPositionReader? InventoryPositionReader { get; set; }
    public IIssuePolicyReader? IssuePolicyReader { get; set; }
    public IIssueDaysReader? IssueDaysReader { get; set; }
    public INextPurchaseOrderReader? NextPurchaseOrderReader { get; set; }
    public IKssScheduleReader? KssScheduleReader { get; set; }
    public IWorkOrderSummaryReader? WorkOrderSummaryReader { get; set; }
    public IWorkOrderMaterialReader? WorkOrderMaterialReader { get; set; }

    /// <summary>Optional deterministic <see cref="IComponentOrderSourceReader"/> override (Stage 10.2).</summary>
    public IComponentOrderSourceReader? ComponentOrderSourceReader { get; set; }

    /// <summary>Optional deterministic enrichment reader override for Component Orders endpoint tests.</summary>
    public IComponentOrderEnrichmentReader? ComponentOrderEnrichmentReader { get; set; }

    /// <summary>Optional deterministic Stage 11-A source reader override for endpoint tests.</summary>
    public ILongTermShortageSourceReader? LongTermShortageSourceReader { get; set; }
    public IOpenOrdersSourceReader? OpenOrdersSourceReader { get; set; }
    public IFinishedGoodsSourceReader? FinishedGoodsSourceReader { get; set; }
    public Kst.Application.ComponentMrp.IComponentMrpReader? ComponentMrpReader { get; set; }
    public Kst.Application.ShipmentsToGo.IShipmentsToGoReader? ShipmentsToGoReader { get; set; }
    public Kst.Application.Sop.ISopReader? SopReader { get; set; }
    public Kst.Application.OpenOrderReport.IOpenOrderReportReader? OpenOrderReportReader { get; set; }
    public Kst.Application.HistoricalShipments.IHistoricalShipmentsReader? HistoricalShipmentsReader { get; set; }
    public Kst.Application.HistoricalShipments.IWorkspaceShipmentsReader? WorkspaceShipmentsReader { get; set; }
    public Kst.Application.HistoricalShipments.IWorkspaceShipmentsScopeResolver? WorkspaceShipmentsScopeResolver { get; set; }
    public IOpenOrderCurrentLineReader? OpenOrderCurrentLineReader { get; set; }
    public IPlanningPriceReader? PlanningPriceReader { get; set; }
    /// <summary>Stage 14.5: in-memory Planning draft store (never the developer's real local app data).</summary>
    internal InMemoryPlanningWorkbookDraftStore PlanningDraftStore { get; } = new();
    public IPlanningForecastReader? PlanningForecastReader { get; set; }


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
            if (WorkspaceShipmentsReader is not null)
            {
                services.Remove(services.Single(d => d.ServiceType == typeof(Kst.Application.HistoricalShipments.IWorkspaceShipmentsReader)));
                services.AddSingleton(WorkspaceShipmentsReader);
            }
            services.Remove(services.Single(d => d.ServiceType == typeof(Kst.Application.HistoricalShipments.IWorkspaceShipmentsStore)));
            services.AddSingleton<Kst.Application.HistoricalShipments.IWorkspaceShipmentsStore>(_ =>
                new Kst.Infrastructure.HistoricalShipments.FileWorkspaceShipmentsStore(Path.Combine(Path.GetTempPath(), "kst-stage19-workspace-tests")));
            var workspaceShipmentsScope = services.Single(d => d.ServiceType == typeof(Kst.Application.Mps.IMpsScopeResolver));
            services.Remove(workspaceShipmentsScope);
            services.AddSingleton<Kst.Application.Mps.IMpsScopeResolver>(sp => new Kst.Application.Mps.DelegateMpsScopeResolver(
                (workspace, _) => Task.FromResult<IReadOnlyList<Kst.Domain.Mps.MpsResolvedPart>>(
                    workspace.ParentParts.Select(part => new Kst.Domain.Mps.MpsResolvedPart(part, null)).ToArray())));
            if (WorkspaceShipmentsScopeResolver is not null)
            {
                services.Remove(services.Single(d => d.ServiceType == typeof(Kst.Application.Mps.IMpsScopeResolver)));
                services.AddSingleton(WorkspaceShipmentsScopeResolver);
            }
            if (HistoricalShipmentsReader is not null)
            {
                services.Remove(services.Single(d => d.ServiceType == typeof(Kst.Application.HistoricalShipments.IHistoricalShipmentsReader)));
                services.AddSingleton(HistoricalShipmentsReader);
            }
            services.Remove(services.Single(d => d.ServiceType == typeof(Kst.Application.HistoricalShipments.IHistoricalShipmentsStore)));
            services.AddSingleton<Kst.Application.HistoricalShipments.IHistoricalShipmentsStore>(_ =>
                new Kst.Infrastructure.HistoricalShipments.FileHistoricalShipmentsStore(Path.Combine(Path.GetTempPath(), "kst-stage19-tests")));
            if (OpenOrderReportReader is not null)
            {
                services.Remove(services.Single(d => d.ServiceType == typeof(Kst.Application.OpenOrderReport.IOpenOrderReportReader)));
                services.AddSingleton(OpenOrderReportReader);
            }
            if (SopReader is not null)
            {
                services.Remove(services.Single(d => d.ServiceType == typeof(Kst.Application.Sop.ISopReader)));
                services.AddSingleton(SopReader);
            }
            if (ShipmentsToGoReader is not null)
            {
                services.Remove(services.Single(d => d.ServiceType == typeof(Kst.Application.ShipmentsToGo.IShipmentsToGoReader)));
                services.AddSingleton(ShipmentsToGoReader);
            }
            if (ComponentMrpReader is not null)
            {
                var descriptor = services.Single(d => d.ServiceType == typeof(Kst.Application.ComponentMrp.IComponentMrpReader));
                services.Remove(descriptor);
                services.AddSingleton(ComponentMrpReader);
            }
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

            // Draft routes must never write to the developer's real local application data in tests.
            var draftDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOpenOrdersDraftStore));
            if (draftDescriptor is not null) services.Remove(draftDescriptor);
            services.AddSingleton<IOpenOrdersDraftStore, InMemoryOpenOrdersDraftStore>();
            var planningDraftDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPlanningWorkbookDraftStore));
            if (planningDraftDescriptor is not null) services.Remove(planningDraftDescriptor);
            services.AddSingleton<IPlanningWorkbookDraftStore>(PlanningDraftStore);

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

            ReplaceIfProvided(services, CommittedWorkOrderPopulationReader);
            ReplaceIfProvided(services, HardAllocationReader);
            ReplaceIfProvided(services, InventoryPositionReader);
            ReplaceIfProvided(services, IssuePolicyReader);
            ReplaceIfProvided(services, IssueDaysReader);
            ReplaceIfProvided(services, NextPurchaseOrderReader);
            ReplaceIfProvided(services, KssScheduleReader);
            ReplaceIfProvided(services, WorkOrderSummaryReader);
            ReplaceIfProvided(services, WorkOrderMaterialReader);
            ReplaceIfProvided(services, ComponentOrderSourceReader);
            ReplaceIfProvided(services, ComponentOrderEnrichmentReader);
            ReplaceIfProvided(services, LongTermShortageSourceReader);
            ReplaceIfProvided(services, OpenOrdersSourceReader);
            ReplaceIfProvided(services, FinishedGoodsSourceReader);
            ReplaceIfProvided(services, InventoryLocationReader);
            ReplaceIfProvided(services, OpenOrderCurrentLineReader);
            ReplaceIfProvided(services, PlanningPriceReader);
            ReplaceIfProvided(services, PlanningForecastReader);

        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }

    private static void ReplaceIfProvided<T>(IServiceCollection services, T? replacement) where T : class
    {
        if (replacement is null) return;
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null) services.Remove(descriptor);
        services.AddSingleton(replacement);
    }
}

internal sealed class InMemoryPlanningWorkbookDraftStore : IPlanningWorkbookDraftStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, PlanningWorkbookDraft> _drafts = new();
    public HashSet<Guid> Corrupt { get; } = [];
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(_drafts.ContainsKey(id) || Corrupt.Contains(id));
    public Task<PlanningWorkbookDraft?> LoadAsync(Guid id, CancellationToken ct) =>
        Corrupt.Contains(id) ? throw new InvalidDataException("corrupt") : Task.FromResult(_drafts.GetValueOrDefault(id));
    public Task SaveAsync(PlanningWorkbookDraft draft, CancellationToken ct) { _drafts[draft.WorkspaceId] = draft; return Task.CompletedTask; }
    public Task DeleteAsync(Guid id, CancellationToken ct) { _drafts.TryRemove(id, out _); Corrupt.Remove(id); return Task.CompletedTask; }
    public PlanningWorkbookDraft? Get(Guid id) => _drafts.GetValueOrDefault(id);
}

internal sealed class InMemoryOpenOrdersDraftStore : IOpenOrdersDraftStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, OpenOrdersDraft> _drafts = new();
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(_drafts.ContainsKey(id));
    public Task<OpenOrdersDraft?> LoadAsync(Guid id, CancellationToken ct) => Task.FromResult(_drafts.GetValueOrDefault(id));
    public Task SaveAsync(OpenOrdersDraft draft, CancellationToken ct) { _drafts[draft.WorkspaceId] = draft; return Task.CompletedTask; }
    public Task DeleteAsync(Guid id, CancellationToken ct) { _drafts.TryRemove(id, out _); return Task.CompletedTask; }
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


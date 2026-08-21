using Kst.Application.ApprovedVendors;
using Kst.Application.Tests.Mps;
using Kst.Domain.ApprovedVendors;
using Kst.Domain.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.ApprovedVendors;

/// <summary>
/// Stage 8D.7 composition tests: workspace resolution (no MPS gating, no cache), success/empty/
/// failure mapping, and cancellation propagation. Reuses the established
/// <see cref="FakeWorkspaceConfigurationService"/> test fake — no new shared test infrastructure
/// beyond the AVL-specific reader fake below.
/// </summary>
public sealed class ApprovedVendorServiceTests
{
    private static readonly WorkspaceAssignment Workspace = new(
        AssignmentId: Guid.NewGuid(),
        DisplayName: "Test Workspace",
        Site: "SW",
        ProductLineFrom: null,
        ProductLineTo: null,
        ParentParts: ["ABC100"],
        IsTemporary: false,
        CoverageEndsOn: null,
        IsEnabled: true,
        SortOrder: 0);

    private static ApprovedVendor Vendor(
        string supplier = "V001",
        string? vendorName = "Acme Supply",
        string? supplierItem = "SUP-1",
        string? manufacturerPart = "MFG-1") =>
        new(supplier, vendorName, supplierItem, manufacturerPart);

    private static (ApprovedVendorService Service, ApprovedVendorSourceFake Source) BuildService(
        ApprovedVendorSourceFake? source = null)
    {
        var src = source ?? new ApprovedVendorSourceFake();
        var service = new ApprovedVendorService(
            new FakeWorkspaceConfigurationService(Workspace),
            src.Reader,
            NullLogger<ApprovedVendorService>.Instance);

        return (service, src);
    }

    // ---------- Scope / workspace ----------

    [Fact]
    public async Task GetApprovedVendorsAsync_Throws_For_Unknown_Workspace()
    {
        var (service, _) = BuildService();

        await Assert.ThrowsAsync<ApprovedVendorWorkspaceNotFoundException>(() =>
            service.GetApprovedVendorsAsync(Guid.NewGuid(), "COMP1"));
    }

    [Fact]
    public async Task GetApprovedVendorsAsync_Does_Not_Require_Mps_Loaded()
    {
        // Deliberately unlike Component Detail: AVL is reference data and is never MPS-gated.
        var (service, source) = BuildService(new ApprovedVendorSourceFake([Vendor()]));

        var result = await service.GetApprovedVendorsAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ApprovedVendorOutcomeKind.Loaded, result.Kind);
        Assert.Equal(1, source.CallCount);
    }

    [Fact]
    public async Task GetApprovedVendorsAsync_Passes_Trimmed_Component_Part_And_Workspace_Site_To_Reader()
    {
        var (service, source) = BuildService();

        await service.GetApprovedVendorsAsync(Workspace.AssignmentId, "  COMP1  ");

        Assert.Equal("SW", source.LastSite);
        Assert.Equal("COMP1", source.LastComponentPart);
    }

    // ---------- Success / empty ----------

    [Fact]
    public async Task GetApprovedVendorsAsync_Returns_Loaded_Empty_When_Reader_Returns_No_Rows()
    {
        var (service, _) = BuildService(new ApprovedVendorSourceFake([]));

        var result = await service.GetApprovedVendorsAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ApprovedVendorOutcomeKind.Loaded, result.Kind);
        Assert.NotNull(result.Vendors);
        Assert.Empty(result.Vendors!);
    }

    [Fact]
    public async Task GetApprovedVendorsAsync_Returns_Loaded_Empty_For_Nonexistent_Component()
    {
        // Accepted grain/existence decision: a direct request for a nonexistent component part is
        // indistinguishable from a real zero-AVL result at this layer.
        var (service, _) = BuildService(new ApprovedVendorSourceFake([]));

        var result = await service.GetApprovedVendorsAsync(Workspace.AssignmentId, "NOSUCHPART");

        Assert.Equal(ApprovedVendorOutcomeKind.Loaded, result.Kind);
        Assert.Empty(result.Vendors!);
    }

    [Fact]
    public async Task GetApprovedVendorsAsync_Returns_Loaded_One_Vendor()
    {
        var (service, _) = BuildService(new ApprovedVendorSourceFake([Vendor()]));

        var result = await service.GetApprovedVendorsAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ApprovedVendorOutcomeKind.Loaded, result.Kind);
        Assert.Single(result.Vendors!);
    }

    [Fact]
    public async Task GetApprovedVendorsAsync_Returns_Loaded_Many_Vendors_Preserving_Order()
    {
        var vendors = new List<ApprovedVendor> { Vendor("V001"), Vendor("V002"), Vendor("V003") };
        var (service, _) = BuildService(new ApprovedVendorSourceFake(vendors));

        var result = await service.GetApprovedVendorsAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(["V001", "V002", "V003"], result.Vendors!.Select(v => v.Supplier));
    }

    [Fact]
    public async Task GetApprovedVendorsAsync_Result_Excludes_Unrelated_Component_Detail_Fields()
    {
        var (service, _) = BuildService(new ApprovedVendorSourceFake([Vendor()]));

        var result = await service.GetApprovedVendorsAsync(Workspace.AssignmentId, "COMP1");

        // Type-level check: ApprovedVendor only exposes Supplier/VendorName/SupplierItem/ManufacturerPart.
        var vendor = result.Vendors!.Single();
        Assert.Equal("V001", vendor.Supplier);
        Assert.Equal("Acme Supply", vendor.VendorName);
        Assert.Equal("SUP-1", vendor.SupplierItem);
        Assert.Equal("MFG-1", vendor.ManufacturerPart);
    }

    // ---------- Failure ----------

    [Fact]
    public async Task GetApprovedVendorsAsync_Returns_Unavailable_When_Reader_Fails()
    {
        var source = new ApprovedVendorSourceFake { Error = new InvalidOperationException("QAD database connectivity failed.") };
        var (service, _) = BuildService(source);

        var result = await service.GetApprovedVendorsAsync(Workspace.AssignmentId, "COMP1");

        Assert.Equal(ApprovedVendorOutcomeKind.Unavailable, result.Kind);
    }

    // ---------- Cancellation ----------

    [Fact]
    public async Task GetApprovedVendorsAsync_Source_Reader_Cancellation_Propagates()
    {
        var source = new ApprovedVendorSourceFake { Error = new OperationCanceledException() };
        var (service, _) = BuildService(source);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetApprovedVendorsAsync(Workspace.AssignmentId, "COMP1"));
    }

    [Fact]
    public async Task GetApprovedVendorsAsync_Cancellation_Does_Not_Become_Unavailable()
    {
        var source = new ApprovedVendorSourceFake { Error = new OperationCanceledException() };
        var (service, _) = BuildService(source);

        var exception = await Record.ExceptionAsync(() =>
            service.GetApprovedVendorsAsync(Workspace.AssignmentId, "COMP1"));

        Assert.IsType<OperationCanceledException>(exception);
    }

    /// <summary>
    /// Deterministic <see cref="IApprovedVendorSourceReader"/> fake recording calls. Returns
    /// <see cref="Vendors"/> (defaults to an empty collection) or throws <see cref="Error"/>.
    /// </summary>
    private sealed class ApprovedVendorSourceFake
    {
        public int CallCount { get; private set; }
        public string? LastSite { get; private set; }
        public string? LastComponentPart { get; private set; }
        public IReadOnlyList<ApprovedVendor> Vendors { get; set; }
        public Exception? Error { get; set; }
        public IApprovedVendorSourceReader Reader { get; }

        public ApprovedVendorSourceFake(IReadOnlyList<ApprovedVendor>? vendors = null)
        {
            Vendors = vendors ?? [];
            Reader = new DelegateApprovedVendorSourceReader((site, componentPart, _) =>
            {
                CallCount++;
                LastSite = site;
                LastComponentPart = componentPart;
                if (Error is not null)
                    throw Error;
                return Task.FromResult(Vendors);
            });
        }
    }
}

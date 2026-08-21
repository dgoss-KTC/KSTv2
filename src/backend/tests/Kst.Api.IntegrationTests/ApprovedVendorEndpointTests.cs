using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.ApprovedVendors;
using Kst.Domain.ApprovedVendors;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Stage 8D.7 Approved Vendors endpoint integration tests. QAD is never configured in the test
/// host, so the source reader bridge is replaced with a deterministic fake through
/// <see cref="KstApiFactory"/> — no live QAD is required for any path here. Deliberately does not
/// seed an MPS snapshot for the success/empty paths: AVL is never MPS-gated.
/// </summary>
public sealed class ApprovedVendorEndpointTests
{
    private const string Component = "COMP1";

    private static ApprovedVendor Vendor(
        string supplier = "V001",
        string? vendorName = "Acme Supply",
        string? supplierItem = "SUP-1",
        string? manufacturerPart = "MFG-1") =>
        new(supplier, vendorName, supplierItem, manufacturerPart);

    private static async Task<Guid> CreateWorkspaceAsync(HttpClient client, params string[] parentParts)
    {
        var request = new { site = "SW", parentParts, isTemporary = false };
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("assignmentId").GetGuid();
    }

    private static IApprovedVendorSourceReader ReaderReturning(IReadOnlyList<ApprovedVendor> vendors) =>
        new DelegateApprovedVendorSourceReader((_, _, _) => Task.FromResult(vendors));

    private static IApprovedVendorSourceReader ReaderThrowing(Exception ex) =>
        new DelegateApprovedVendorSourceReader((_, _, _) => throw ex);

    [Fact]
    public async Task GetApprovedVendors_Returns404_For_Unknown_Workspace()
    {
        await using var factory = new KstApiFactory { ApprovedVendorSourceReader = ReaderReturning([]) };
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}/components/{Component}/approved-vendors");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetApprovedVendors_Returns400_When_ComponentPart_Blank()
    {
        await using var factory = new KstApiFactory { ApprovedVendorSourceReader = ReaderReturning([]) };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, "ABC100");

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/%20/approved-vendors");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetApprovedVendors_Returns200_Empty_Collection_For_Zero_Rows_Without_Mps_Loaded()
    {
        await using var factory = new KstApiFactory { ApprovedVendorSourceReader = ReaderReturning([]) };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, "ABC100");

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}/approved-vendors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var vendors = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object?>>>();
        Assert.NotNull(vendors);
        Assert.Empty(vendors!);
    }

    [Fact]
    public async Task GetApprovedVendors_Returns200_With_One_Vendor()
    {
        await using var factory = new KstApiFactory { ApprovedVendorSourceReader = ReaderReturning([Vendor()]) };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, "ABC100");

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}/approved-vendors");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = doc.RootElement[0];
        Assert.Equal("V001", row.GetProperty("supplier").GetString());
        Assert.Equal("Acme Supply", row.GetProperty("vendorName").GetString());
        Assert.Equal("SUP-1", row.GetProperty("supplierItem").GetString());
        Assert.Equal("MFG-1", row.GetProperty("manufacturerPart").GetString());
    }

    [Fact]
    public async Task GetApprovedVendors_Returns200_With_Many_Vendors_In_Reader_Order()
    {
        var vendors = new List<ApprovedVendor> { Vendor("V001"), Vendor("V002"), Vendor("V003") };
        await using var factory = new KstApiFactory { ApprovedVendorSourceReader = ReaderReturning(vendors) };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, "ABC100");

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}/approved-vendors");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        var suppliers = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("supplier").GetString()).ToList();
        Assert.Equal(["V001", "V002", "V003"], suppliers);
    }

    [Fact]
    public async Task GetApprovedVendors_Response_Excludes_Unrelated_Component_Detail_Fields()
    {
        await using var factory = new KstApiFactory { ApprovedVendorSourceReader = ReaderReturning([Vendor()]) };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, "ABC100");

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}/approved-vendors");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        var propertyNames = doc.RootElement[0].EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(["supplier", "vendorName", "supplierItem", "manufacturerPart"], propertyNames);
    }

    [Fact]
    public async Task GetApprovedVendors_Returns503_When_Source_Read_Fails()
    {
        await using var factory = new KstApiFactory
        {
            ApprovedVendorSourceReader = ReaderThrowing(new InvalidOperationException("QAD database connectivity failed."))
        };
        using var client = factory.CreateClient();
        var assignmentId = await CreateWorkspaceAsync(client, "ABC100");

        var response = await client.GetAsync($"/api/v1/workspaces/{assignmentId}/components/{Component}/approved-vendors");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}

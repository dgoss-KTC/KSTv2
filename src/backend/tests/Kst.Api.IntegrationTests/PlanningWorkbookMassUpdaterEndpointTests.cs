using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Kst.Application.Inventory;
using Kst.Application.Mps;
using Kst.Application.OpenOrders;
using Kst.Application.PlanningWorkbook;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Stage 14.6 Mass Updater export endpoint tests. Every QAD-facing reader is replaced with a fake that
/// fails the test if invoked, proving the export route validates against the loaded MPS snapshot only.
/// </summary>
public sealed class PlanningWorkbookMassUpdaterEndpointTests
{
    private int _qadReads;

    private KstApiFactory Factory() => new()
    {
        OpenOrdersSourceReader = new DelegateOpenOrdersSourceReader((_, _, _) => { _qadReads++; throw new InvalidOperationException("Open Orders read"); }),
        PartInventoryReader = new DelegatePartInventoryReader((_, _, _) => { _qadReads++; throw new InvalidOperationException("inventory read"); }),
        PlanningPriceReader = new DelegatePlanningPriceReader((_, _, _, _) => { _qadReads++; throw new InvalidOperationException("price read"); }),
        PlanningForecastReader = new DelegatePlanningForecastReader((_, _, _) => { _qadReads++; throw new InvalidOperationException("forecast read"); })
    };

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    private static DateOnly WeekLabel(int index) => Today.AddDays(-(int)Today.DayOfWeek).AddDays(1 + 7 * index);
    private static string Text(DateOnly d) => d.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
    private static string Route(Guid id) => $"/api/v1/workspaces/{id}/planning-workbook/mass-updater-export";

    [Fact]
    public async Task Export_Returns_Exact_Csv_Bytes_In_Part_Then_Week_Order_Without_Any_Qad_Read()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client, "Acme/Q3");
        var mps = Seed(factory, id);

        using var response = await client.PostAsJsonAsync(Route(id), new
        {
            mpsSnapshotId = mps.Id.Value,
            site = "SW",
            entries = new object[]
            {
                new { part = "p-2", weekLabel = WeekLabel(0), adjustment = "-2" },
                new { part = "P-1", weekLabel = WeekLabel(3), adjustment = "20" },
                new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "-12" },
                new { part = "P-1", weekLabel = WeekLabel(1), adjustment = "0" },
                new { part = "P-2", weekLabel = WeekLabel(1), adjustment = "-0" }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var bytes = Convert.FromBase64String(json.GetProperty("contentBase64").GetString()!);
        var expected = $"P-1,-12,{Text(WeekLabel(0))}\r\nP-1,20,{Text(WeekLabel(3))}\r\nP-2,-2,{Text(WeekLabel(0))}\r\n";
        Assert.Equal(new UTF8Encoding(false).GetBytes(expected), bytes);
        Assert.Equal(3, json.GetProperty("rowCount").GetInt32());
        Assert.Equal(mps.Id.ToString(), json.GetProperty("mpsSnapshotId").GetString());
        Assert.Equal("SW", json.GetProperty("site").GetString());
        Assert.Equal($"Acme-Q3-MPS-Adjustments-{DateTime.Now:yyyy-MM-dd}.csv", json.GetProperty("fileName").GetString());
        Assert.Equal(0, _qadReads);
    }

    [Fact]
    public async Task Export_Never_Reads_Or_Changes_Saved_Drafts_Or_Refreshes_The_Loaded_Snapshot()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory, id);
        var draftRoute = $"/api/v1/workspaces/{id}/planning-workbook/draft";
        (await client.PutAsJsonAsync(draftRoute, new { mpsSnapshotId = mps.Id.Value, entries = new[] { new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "-3" } } }))
            .EnsureSuccessStatusCode();
        var draft = factory.PlanningDraftStore.Get(id);

        (await client.PostAsJsonAsync(Route(id), new
        {
            mpsSnapshotId = mps.Id.Value,
            site = "SW",
            entries = new[] { new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "-12" } }
        })).EnsureSuccessStatusCode();

        Assert.Same(draft, factory.PlanningDraftStore.Get(id)); // not overwritten
        Assert.Same(mps, factory.Services.GetRequiredService<IMpsSnapshotStore>().GetState(id).Snapshot);
        Assert.Equal(0, _qadReads);
    }

    [Fact]
    public async Task Invalid_Entries_Reject_The_Whole_Export_With_Only_A_Code_And_A_Count()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory, id);

        using var response = await client.PostAsJsonAsync(Route(id), new
        {
            mpsSnapshotId = mps.Id.Value,
            site = "SW",
            entries = new[]
            {
                new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "-12" },
                new { part = "P-2", weekLabel = WeekLabel(0), adjustment = "-3" } // 2 - 3 < 0
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal("negative-result", json.RootElement.GetProperty("issueCode").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("affectedRowCount").GetInt32());
        Assert.DoesNotContain("contentBase64", body);
        Assert.DoesNotContain("P-1", body);
        Assert.DoesNotContain("P-2", body);
        Assert.DoesNotContain("-12", body);
        Assert.Equal(0, _qadReads);
    }

    [Theory]
    [InlineData("+5", "invalid-number")]
    [InlineData("10.5", "not-whole-number")]
    public async Task Whole_Number_Violations_Are_Rejected(string adjustment, string code)
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory, id);

        using var response = await client.PostAsJsonAsync(Route(id), new
        {
            mpsSnapshotId = mps.Id.Value,
            site = "SW",
            entries = new[] { new { part = "P-1", weekLabel = WeekLabel(0), adjustment } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("issueCode").GetString());
    }

    [Fact]
    public async Task Falldown_Entries_And_Nothing_To_Export_Are_Rejected()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var mps = Seed(factory, id);

        using var falldown = await client.PostAsJsonAsync(Route(id), new
        {
            mpsSnapshotId = mps.Id.Value,
            site = "SW",
            entries = new[] { new { part = "P-1", weekLabel = (DateOnly?)null, adjustment = "5" } }
        });
        using var empty = await client.PostAsJsonAsync(Route(id), new
        {
            mpsSnapshotId = mps.Id.Value,
            site = "SW",
            entries = new[] { new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "0" } }
        });

        Assert.Equal("not-adjustable", (await falldown.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("issueCode").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal("no-adjustments", (await empty.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("issueCode").GetString());
    }

    [Fact]
    public async Task Workspace_Site_And_Snapshot_Identity_Conflicts_Are_Sanitized()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var entries = new[] { new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "-1" } };

        async Task<(HttpStatusCode, string?)> Post(Guid workspace, object body)
        {
            using var r = await client.PostAsJsonAsync(Route(workspace), body);
            var doc = await r.Content.ReadFromJsonAsync<JsonElement>();
            return (r.StatusCode, doc.TryGetProperty("issueCode", out var c) ? c.GetString() : null);
        }

        Assert.Equal((HttpStatusCode.Conflict, "mps-not-loaded"), await Post(id, new { mpsSnapshotId = Guid.NewGuid(), site = "SW", entries }));
        var mps = Seed(factory, id);
        Assert.Equal((HttpStatusCode.Conflict, "snapshot-changed"), await Post(id, new { mpsSnapshotId = Guid.NewGuid(), site = "SW", entries }));
        Assert.Equal((HttpStatusCode.Conflict, "site-changed"), await Post(id, new { mpsSnapshotId = mps.Id.Value, site = "XX", entries }));
        Assert.Equal((HttpStatusCode.NotFound, "workspace-missing"), await Post(Guid.NewGuid(), new { mpsSnapshotId = mps.Id.Value, site = "SW", entries }));
        Assert.Equal((HttpStatusCode.BadRequest, "invalid-request"), await Post(id, new { mpsSnapshotId = Guid.Empty, site = "SW", entries }));
        Assert.Equal((HttpStatusCode.BadRequest, "invalid-request"), await Post(id, new { mpsSnapshotId = mps.Id.Value, site = "", entries }));
        Assert.Equal((HttpStatusCode.BadRequest, "invalid-request"), await Post(id, new { mpsSnapshotId = mps.Id.Value, site = "SW", entries = (object?)null }));
        Assert.Equal((HttpStatusCode.BadRequest, "invalid-request"), await Post(id, new { mpsSnapshotId = mps.Id.Value, site = "SW", entries = new[] { new { part = "", weekLabel = WeekLabel(0), adjustment = "1" } } }));
        Assert.Equal(0, _qadReads);
    }

    [Fact]
    public async Task Overlapping_Parts_In_Two_Workspaces_Are_Validated_Against_Their_Own_Snapshots()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var a = await CreateAsync(client);
        var b = await CreateAsync(client, extraPart: "P-3");
        var mpsA = Seed(factory, a);
        var mpsB = Seed(factory, b, p1: 100m);
        var body = (SnapshotId snapshot) => new
        {
            mpsSnapshotId = snapshot.Value,
            site = "SW",
            entries = new[] { new { part = "P-1", weekLabel = WeekLabel(0), adjustment = "-50" } }
        };

        using var ok = await client.PostAsJsonAsync(Route(b), body(mpsB.Id));
        using var negative = await client.PostAsJsonAsync(Route(a), body(mpsA.Id)); // 12.5 baseline
        using var crossed = await client.PostAsJsonAsync(Route(a), body(mpsB.Id));

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, crossed.StatusCode);
    }

    [Fact]
    public async Task Openapi_Describes_The_Export_Route_And_Contract()
    {
        await Task.CompletedTask;
        var path = FindRepositoryFile("docs/openapi/Kst.Api.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var operation = document.RootElement.GetProperty("paths")
            .GetProperty("/api/v1/workspaces/{assignmentId}/planning-workbook/mass-updater-export").GetProperty("post");
        Assert.Equal("ExportPlanningMassUpdaterCsv", operation.GetProperty("operationId").GetString());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("ExportPlanningMassUpdaterRequestDto", out _));
        Assert.True(schemas.TryGetProperty("PlanningMassUpdaterExportResponseDto", out _));
    }

    private static string FindRepositoryFile(string relative)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Generated OpenAPI document not found.", relative);
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string? displayName = null, string extraPart = "P-2")
    {
        var response = await client.PostAsJsonAsync("/api/v1/workspaces",
            new { displayName, site = "SW", parentParts = new[] { "P-1", extraPart }, isTemporary = false });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("assignmentId").GetGuid();
    }

    private static MpsSnapshot Seed(KstApiFactory factory, Guid id, decimal p1 = 12.5m)
    {
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW",
            [new MpsResolvedPart("P-1", null), new MpsResolvedPart("P-2", null)],
            [
                new MpsSourceRow("TEST", "SW", "P-1", null, Today, null, p1, MpsSupplyType.Supply, "WO-1", MpsWorkOrderState.Planned),
                new MpsSourceRow("TEST", "SW", "P-2", null, Today, null, 2m, MpsSupplyType.Supply, "WO-2", MpsWorkOrderState.Planned)
            ]);
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(id, snapshot);
        return snapshot;
    }
}

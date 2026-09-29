using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kst.Application.Bom;
using Kst.Application.ComponentOrders;
using Kst.Application.LongTermShortages;
using Kst.Application.Mps;
using Kst.Domain.Bom;
using Kst.Domain.ComponentOrders;
using Kst.Domain.LongTermShortages;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;
namespace Kst.Api.IntegrationTests;
public sealed class LongTermShortagesEndpointTests
{
    [Fact]
    public async Task CompactScreenAndDetail_PreserveSnapshotIdentityAndNeverAcquireOnCacheFailures()
    {
        var bomReads = 0; var mrpReads = 0; var poReads = 0;
        var acquiredAt = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        await using var factory = new KstApiFactory
        {
            BomSourceReader = new DelegateBomSourceReader((_, _, _, _) =>
            {
                bomReads++;
                return Task.FromResult<IReadOnlyList<BomOccurrence>>([new("1", 1, "COMP", "P", false, null, 1m, null)]);
            }),
            LongTermShortageSourceReader = new DelegateLongTermShortageSourceReader((_, _, date, _, _) =>
            {
                mrpReads++;
                return Task.FromResult(new LongTermShortageAcquisition(acquiredAt,
                    [new("COMP", " EA ", "P", "Part", "Buyer", "BUY", -1.25m, SafetyStockState.Resolved, 2m, ["PARENT"],
                        [new LongTermMrpFact(1, "DEMAND", date.AddDays(-1), null, 3m, MrpScheduleCategory.Unclassified)
                            { SourceRowId = "row-1", SourceLine2 = "line-2" }])]));
            }),
            ComponentOrderSourceReader = new DelegateComponentOrderSourceReader((_, _, _, _) =>
            {
                poReads++;
                throw new InvalidOperationException("Projection detail must not read purchasing.");
            })
        };
        using var client = factory.CreateClient();
        var workspace = (await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false }));
        var workspaceId = (await workspace.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("assignmentId").GetGuid();
        var snapshots = factory.Services.GetRequiredService<IMpsSnapshotStore>();
        var a = Kst.Domain.Common.SnapshotId.New();
        var root = $"/api/v1/workspaces/{workspaceId}/long-term-shortages";
        var detailPath = $"{root}/projection-detail?snapshotId={a}&componentPart=COMP";
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(detailPath)).StatusCode);
        snapshots.SetLoaded(workspaceId, new MpsSnapshot(a, acquiredAt, "SW", [new MpsResolvedPart("PARENT", "")], []));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(detailPath)).StatusCode);
        Assert.Equal(0, mrpReads);
        var screen = await client.GetFromJsonAsync<JsonElement>($"{root}/screen?snapshotId={a}");
        Assert.Equal(a.ToString(), screen.GetProperty("snapshotId").GetString());
        Assert.Equal(26, screen.GetProperty("weeks").GetArrayLength());
        var component = screen.GetProperty("components")[0];
        Assert.Equal("EA", component.GetProperty("unitOfMeasure").GetString());
        Assert.Equal(-1.25m, component.GetProperty("openingQoh").GetDecimal());
        Assert.Equal("-1", component.GetProperty("openingDisplay").GetString());
        foreach (var omitted in new[] { "evidence", "episodes", "demandParentParts", "past", "presentation", "weeks" })
            Assert.False(component.TryGetProperty(omitted, out _));
        Assert.Equal(26, component.GetProperty("confirmed").GetProperty("ending").GetArrayLength());
        Assert.Equal(26, component.GetProperty("all").GetProperty("ending").GetArrayLength());
        var detail = await client.GetFromJsonAsync<JsonElement>(detailPath);
        Assert.Equal(a.ToString(), detail.GetProperty("snapshotId").GetString());
        Assert.Equal(acquiredAt, detail.GetProperty("acquiredAtUtc").GetDateTimeOffset());
        Assert.Equal(-4.25m, detail.GetProperty("adjustedOpeningQoh").GetDecimal());
        Assert.Equal("PARENT", detail.GetProperty("demandParentParts")[0].GetString());
        // Export directly after compact screen/detail; full response hydration is never needed.
        var export = await client.PostAsJsonAsync(root + "/export", new { snapshotId = a.ToString(), componentParts = new[] { "COMP" } });
        export.EnsureSuccessStatusCode();
        using (var stream = new MemoryStream(await export.Content.ReadAsByteArrayAsync()))
        using (var book = new ClosedXML.Excel.XLWorkbook(stream))
            Assert.Equal("row-1", book.Worksheet("Raw MRP Evidence").Cell(2, 13).GetString());
        Assert.Equal((1, 1, 0), (bomReads, mrpReads, poReads));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(detailPath.Replace("componentPart=COMP", "componentPart=ABSENT"))).StatusCode);
        var cache = factory.Services.GetRequiredService<ILongTermShortagesCacheStore>();
        var date = DateOnly.Parse(screen.GetProperty("refreshDate").GetString()!);
        var options = new LongTermShortagePopulationOptions();
        var complete = cache.Get(workspaceId, a, date, LongTermShortagesService.ScheduleVersion, options)!;
        cache.Set(complete with { AllReceiptsRows = [] });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(detailPath)).StatusCode);
        cache.Set(complete);
        var b = Kst.Domain.Common.SnapshotId.New();
        snapshots.SetLoaded(workspaceId, new MpsSnapshot(b, acquiredAt.AddMinutes(1), "SW", [new MpsResolvedPart("PARENT", "")], []));
        await client.GetAsync($"{root}/screen?snapshotId={b}");
        var beforeFailure = (bomReads, mrpReads, poReads);
        var conflict = await client.GetAsync(detailPath);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("urn:kst:shortages:projection-detail-unavailable", (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString());
        Assert.Equal(beforeFailure, (bomReads, mrpReads, poReads));
        Assert.Equal((2, 2, 0), beforeFailure);
    }

    [Fact]
    public async Task Get_ScreenOmitsEvidenceWithoutChangingProjectionCacheOrWorkbook()
    {
        var reads = 0;
        await using var factory = new KstApiFactory
        {
            BomSourceReader = new DelegateBomSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<BomOccurrence>>([new("1", 1, "COMP", "P", false, null, 1m, null)])),
            LongTermShortageSourceReader = new DelegateLongTermShortageSourceReader((_, _, date, _, _) =>
            {
                reads++;
                return Task.FromResult(new LongTermShortageAcquisition(DateTimeOffset.UtcNow,
                    [new("COMP", "EA", "P", "Part", null, null, 0m, SafetyStockState.Resolved, 0m, ["PARENT"],
                        [new LongTermMrpFact(1, "DEMAND", date, null, 10m, MrpScheduleCategory.Unclassified) { SourceRowId = "distinct-row-1", SourceLine2 = "2" }])]));
            })
        };
        using var client = factory.CreateClient();
        var workspace = JsonDocument.Parse(await (await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false })).Content.ReadAsStringAsync()).RootElement.GetProperty("assignmentId").GetGuid();
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(workspace,
            new MpsSnapshot(Kst.Domain.Common.SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], []));
        var snapshot = factory.Services.GetRequiredService<IMpsSnapshotStore>().GetState(workspace).Snapshot!.Id.Value;
        var path = $"/api/v1/workspaces/{workspace}/long-term-shortages?snapshotId={snapshot}";
        var full = await client.GetFromJsonAsync<JsonElement>(path);
        var screen = await client.GetFromJsonAsync<JsonElement>(path + "&includeEvidence=false");
        Assert.True(full.GetProperty("evidenceIncluded").GetBoolean());
        Assert.False(screen.GetProperty("evidenceIncluded").GetBoolean());
        foreach (var mode in new[] { "rows", "allReceiptsRows" })
        {
            var fullRow = full.GetProperty(mode)[0];
            var screenRow = screen.GetProperty(mode)[0];
            Assert.Single(fullRow.GetProperty("evidence").EnumerateArray());
            Assert.Empty(screenRow.GetProperty("evidence").EnumerateArray());
            foreach (var property in fullRow.EnumerateObject().Where(p => p.Name != "evidence"))
                Assert.Equal(property.Value.GetRawText(), screenRow.GetProperty(property.Name).GetRawText());
        }
        var export = await client.PostAsJsonAsync(path.Split('?')[0] + "/export", new { snapshotId = snapshot, componentParts = new[] { "COMP" } });
        export.EnsureSuccessStatusCode();
        using var stream = new MemoryStream(await export.Content.ReadAsByteArrayAsync());
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        Assert.Equal("distinct-row-1", workbook.Worksheet("Raw MRP Evidence").Cell(2, 13).GetString());
        Assert.Equal(1, reads);
    }

    [Fact]
    public async Task Purchasing_UsesCachedProjectionScopeAndSelectedComponentPoPath_WithoutAnotherMrpRead()
    {
        var mrpReads = 0;
        var poReads = 0;
        await using var factory = new KstApiFactory
        {
            BomSourceReader = new DelegateBomSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<BomOccurrence>>([new("1", 1, "COMP", "P", false, null, 1m, null)])),
            LongTermShortageSourceReader = new DelegateLongTermShortageSourceReader((_, _, _, _, _) =>
            {
                mrpReads++;
                return Task.FromResult(new LongTermShortageAcquisition(DateTimeOffset.UtcNow,
                    [new("COMP", "EA", "P", "Part", null, null, 1m, SafetyStockState.Resolved, 0m, ["PARENT"], [])]));
            }),
            ComponentOrderSourceReader = new DelegateComponentOrderSourceReader((_, parts, _, _) =>
            {
                poReads++;
                Assert.Equal(["COMP"], parts);
                return Task.FromResult<IReadOnlyList<ComponentOrderLine>>([new("COMP", "Part", null, "PO-1", 2,
                    new DateOnly(2026, 9, 20), 3.75m, true, "Supplier", null, null, false, null)]);
            }),
            ComponentOrderEnrichmentReader = new DelegateComponentOrderEnrichmentReader((_, _) =>
                Task.FromResult(new ComponentOrderEnrichmentResult(new Dictionary<string, string?> { ["COMP"] = "Current buyer note" },
                    new Dictionary<string, ComponentOrderSupplierRisk>())))
        };
        using var client = factory.CreateClient();
        var workspace = JsonDocument.Parse(await (await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false })).Content.ReadAsStringAsync()).RootElement.GetProperty("assignmentId").GetGuid();
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(workspace,
            new MpsSnapshot(Kst.Domain.Common.SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], []));
        var snapshot = factory.Services.GetRequiredService<IMpsSnapshotStore>().GetState(workspace).Snapshot!.Id.Value;
        var path = $"/api/v1/workspaces/{workspace}/long-term-shortages/purchasing?snapshotId={snapshot}&componentPart=COMP";
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(path)).StatusCode);
        await client.GetAsync($"/api/v1/workspaces/{workspace}/long-term-shortages?snapshotId={snapshot}");
        var response = await client.GetAsync(path);
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Current buyer note", root.GetProperty("currentComment").GetString());
        Assert.Equal("PO-1", root.GetProperty("openPurchaseOrders")[0].GetProperty("poNumber").GetString());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(path.Replace("componentPart=COMP", "componentPart=OTHER"))).StatusCode);
        Assert.Equal(1, mrpReads);
        Assert.Equal(1, poReads);
    }

    [Fact]
    public async Task Purchasing_RejectsInvalidSnapshotAndMissingComponentBeforeSourceAccess()
    {
        await using var factory = new KstApiFactory();
        using var client = factory.CreateClient();
        var workspace = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/workspaces/{workspace}/long-term-shortages/purchasing?componentPart=COMP")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/workspaces/{workspace}/long-term-shortages/purchasing?snapshotId={Guid.NewGuid()}")).StatusCode);
    }
    [Fact]
    public async Task Get_ExposesRawProjectionAndCachedExport()
    {
        var acquiredAt = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        await using var factory = new KstApiFactory { BomSourceReader = new DelegateBomSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<BomOccurrence>>([new("1", 1, "COMP", "P", false, null, 1m, null)])), LongTermShortageSourceReader = new DelegateLongTermShortageSourceReader((_, _, _, _, _) => Task.FromResult(new LongTermShortageAcquisition(acquiredAt, [new("COMP", "EA", "P", "Component", null, null, -1m, SafetyStockState.Resolved, 0m, ["PARENT"], [])]))) };
        using var client = factory.CreateClient(); var workspace = JsonDocument.Parse(await (await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false })).Content.ReadAsStringAsync()).RootElement.GetProperty("assignmentId").GetGuid();
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(workspace, new MpsSnapshot(Kst.Domain.Common.SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], [])); var snapshot = factory.Services.GetRequiredService<IMpsSnapshotStore>().GetState(workspace).Snapshot!.Id.Value;
        var response = await client.GetAsync($"/api/v1/workspaces/{workspace}/long-term-shortages?snapshotId={snapshot}"); var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var export = await client.PostAsJsonAsync($"/api/v1/workspaces/{workspace}/long-term-shortages/export", new { snapshotId = snapshot, componentParts = new[] { "COMP" } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(26, body.GetProperty("rows")[0].GetProperty("weeks").GetArrayLength()); Assert.True(body.GetProperty("rows")[0].TryGetProperty("past", out _)); Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal(LongTermShortageAcquisition.ConsistencyMode, body.GetProperty("consistencyMode").GetString());
        Assert.Equal(acquiredAt, body.GetProperty("acquiredAtUtc").GetDateTimeOffset());
        Assert.Equal(body.GetProperty("rows").GetArrayLength(), body.GetProperty("allReceiptsRows").GetArrayLength());
    }

    [Fact]
    public async Task Get_ShowAllIsScopedToCacheAndExportSelection()
    {
        await using var factory = new KstApiFactory { BomSourceReader = new DelegateBomSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<BomOccurrence>>([new("1", 1, "HEALTHY", "P", false, null, 1m, null)])), LongTermShortageSourceReader = new DelegateLongTermShortageSourceReader((_, _, _, _, _) => Task.FromResult(new LongTermShortageAcquisition(DateTimeOffset.UtcNow, [new("HEALTHY", "EA", "P", "Part", null, null, 10m, SafetyStockState.Resolved, 2m, ["PARENT"], [])]))) };
        using var client = factory.CreateClient();
        var workspace = JsonDocument.Parse(await (await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false })).Content.ReadAsStringAsync()).RootElement.GetProperty("assignmentId").GetGuid();
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(workspace, new MpsSnapshot(Kst.Domain.Common.SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], []));
        var snapshot = factory.Services.GetRequiredService<IMpsSnapshotStore>().GetState(workspace).Snapshot!.Id.Value;
        var defaultResponse = await client.GetFromJsonAsync<JsonElement>($"/api/v1/workspaces/{workspace}/long-term-shortages?snapshotId={snapshot}");
        Assert.Equal("Healthy", defaultResponse.GetProperty("rows")[0].GetProperty("severity").GetString());
        var showAllResponse = await client.GetFromJsonAsync<JsonElement>($"/api/v1/workspaces/{workspace}/long-term-shortages?snapshotId={snapshot}&showAll=true");
        Assert.Equal("Healthy", showAllResponse.GetProperty("rows")[0].GetProperty("severity").GetString());
        Assert.Equal(defaultResponse.GetProperty("acquiredAtUtc").GetString(), showAllResponse.GetProperty("acquiredAtUtc").GetString());
        var export = await client.PostAsJsonAsync($"/api/v1/workspaces/{workspace}/long-term-shortages/export", new { snapshotId = snapshot, componentParts = new[] { "HEALTHY" }, showAll = true });
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
    }

    [Fact]
    public async Task Get_SourceFailureReturnsSpecificSafeProblemDetail()
    {
        await using var factory = new KstApiFactory
        {
            BomSourceReader = new DelegateBomSourceReader((_, _, _, _) => Task.FromResult<IReadOnlyList<BomOccurrence>>([new("1", 1, "COMP", "P", false, null, 1m, null)])),
            LongTermShortageSourceReader = new DelegateLongTermShortageSourceReader((_, _, _, _, _) => throw new InvalidOperationException("Component source read was incomplete or ambiguous."))
        };
        using var client = factory.CreateClient();
        var workspace = JsonDocument.Parse(await (await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false })).Content.ReadAsStringAsync()).RootElement.GetProperty("assignmentId").GetGuid();
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(workspace, new MpsSnapshot(Kst.Domain.Common.SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("PARENT", "")], []));
        var snapshot = factory.Services.GetRequiredService<IMpsSnapshotStore>().GetState(workspace).Snapshot!.Id.Value;
        var response = await client.GetAsync($"/api/v1/workspaces/{workspace}/long-term-shortages?snapshotId={snapshot}");
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("QADPro2 returned incomplete component results. Retry the report.", problem.GetProperty("detail").GetString());
    }
}

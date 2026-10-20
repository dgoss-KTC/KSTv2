using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Kst.Api.Dtos;
using Kst.Application.Bom;
using Kst.Application.LongTermShortages;
using Kst.Application.Mps;
using Kst.Domain.Bom;
using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;
using Kst.Domain.Mps;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

public sealed class WorkspaceShortagesExportTests
{
    [Theory]
    [InlineData(13, false)]
    [InlineData(26, true)]
    [InlineData(52, false)]
    [InlineData(72, true)]
    public async Task Protected_preparation_preserves_actual_workbook_selection_order_precision_and_never_rereads(int horizon, bool allReceipts)
    {
        var reads = 0;
        var acquired = DateTimeOffset.UtcNow;
        await using var factory = new KstApiFactory
        {
            BomSourceReader = new DelegateBomSourceReader((_, _, _, _) =>
            {
                reads++;
                return Task.FromResult<IReadOnlyList<BomOccurrence>>([
                    new("1", 1, "A", "P", false, null, 1m, null), new("2", 1, "B", "P", false, null, 1m, null),
                    new("3", 1, "M", "M", false, null, 1m, null)]);
            }),
            LongTermShortageSourceReader = new DelegateLongTermShortageSourceReader((_, parts, date, _, _) =>
            {
                reads++;
                return Task.FromResult(new LongTermShortageAcquisition(acquired, parts.Keys.Select(part =>
                    new LongTermShortageInput(part, "KG", "P", "=literal", "Planner", "BUY", -1.234567m,
                        SafetyStockState.Resolved, 2m, ["PARENT"], [
                            new LongTermMrpFact(1, "DEMAND", date, null, 3.123456m, MrpScheduleCategory.GrossRequirement),
                            new LongTermMrpFact(2, "SUPPLY", date, null, 9.876543m, MrpScheduleCategory.ScheduledReceipt) { IsPoReceipt = true, PoConfirmed = false }
                        ])).ToArray()));
            })
        };
        using var client = factory.CreateClient();
        var session = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-KST-Report-Session", session);
        var workspaceResponse = await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "PARENT" }, isTemporary = false });
        var workspace = (await workspaceResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("assignmentId").GetGuid();
        var snapshot = SnapshotId.New();
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(workspace, new MpsSnapshot(snapshot, acquired, "SW", [new("PARENT", "")], []));
        var root = $"/api/v1/workspaces/{workspace}/long-term-shortages";
        (await client.GetAsync($"{root}/screen?snapshotId={snapshot}&includeManufacturedParts=true&horizonWeeks={horizon}")).EnsureSuccessStatusCode();
        var before = reads;
        var selection = new ExportLongTermShortagesRequestDto(snapshot.ToString(), ["M", "A"], true, false, allReceipts, horizon, true);
        var request = new WorkspaceShortagesExportRequestDto(workspace, snapshot.Value, "SW", DateOnly.FromDateTime(DateTime.Now), acquired, selection);
        var response = await client.PostAsJsonAsync("/api/v1/reports/workspace-shortages", request);
        response.EnsureSuccessStatusCode();
        var prepared = (await response.Content.ReadFromJsonAsync<AnalyticalExportPreparedDto>())!;
        var bytesPath = $"/api/v1/reports/workspace-shortages/preparations/{prepared.PreparationId}/bytes";
        var bytes = await client.GetByteArrayAsync(bytesPath);
        Assert.Equal(bytes, await client.GetByteArrayAsync(bytesPath)); // exact retry, no regeneration
        using var actual = new XLWorkbook(new MemoryStream(bytes));
        using var legacy = new XLWorkbook(new MemoryStream(await (await client.PostAsJsonAsync(root + "/export", selection)).Content.ReadAsByteArrayAsync()));
        Assert.Equal(legacy.Worksheets.Select(s => s.Name), actual.Worksheets.Select(s => s.Name));
        var sheet = actual.Worksheet("Shortages");
        Assert.Equal(3, sheet.LastRowUsed()!.RowNumber());
        Assert.Equal("M", sheet.Cell(2, 1).GetString()); Assert.Equal("A", sheet.Cell(3, 1).GetString());
        Assert.Equal(-1.23m, sheet.Cell(2, 16).GetValue<decimal>());
        Assert.Equal("=literal", sheet.Cell(2, 4).GetString()); Assert.False(sheet.Cell(2, 4).HasFormula);
        Assert.Equal(horizon + 21, sheet.LastColumnUsed()!.ColumnNumber());
        // Every original workbook value follows its component; no sheet/precision/content redesign.
        foreach (var name in new[] { "Shortages", "Weekly Projection", "Raw MRP Evidence" })
        {
            var old = legacy.Worksheet(name);
            foreach (var newRow in actual.Worksheet(name).RowsUsed().Skip(1))
            {
                var part = newRow.Cell(1).GetString();
                Assert.DoesNotContain("B", part);
                var values = newRow.Cells(1, old.LastColumnUsed()!.ColumnNumber()).Select(c => c.Value.ToString()).ToArray();
                Assert.Contains(old.RowsUsed().Skip(1), oldRow => oldRow.Cell(1).GetString() == part &&
                    oldRow.Cells(1, old.LastColumnUsed()!.ColumnNumber()).Select(c => c.Value.ToString()).SequenceEqual(values));
            }
        }
        var folder = Environment.GetEnvironmentVariable("KST_S22_SHORTAGES_SAMPLES");
        if (folder is not null) { Directory.CreateDirectory(folder); await File.WriteAllBytesAsync(Path.Combine(folder, $"shortages-{horizon}-{allReceipts}.xlsx"), bytes); }
        Assert.Equal(before, reads);
        // Session and kind binding prevent another report or session from fetching these bytes.
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/reports/analytical/preparations/{prepared.PreparationId}/bytes")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-KST-Report-Session");
        client.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(bytesPath)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-KST-Report-Session");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/reports/workspace-shortages", request)).StatusCode);
        client.DefaultRequestHeaders.Add("X-KST-Report-Session", session);
        foreach (var invalid in new[] {
            request with { AcquiredAtUtc = acquired.AddSeconds(-1) }, request with { Site = "NW" },
            request with { BusinessDate = request.BusinessDate.AddDays(-1) },
            request with { Selection = selection with { ComponentParts = ["ABSENT"] } },
            request with { Selection = selection with { HorizonWeeks = horizon == 13 ? 26 : 13 } },
            request with { Selection = selection with { IncludeManufacturedParts = false } } })
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/reports/workspace-shortages", invalid)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/reports/workspace-shortages", request with { Selection = selection with { ComponentParts = ["A", "a"] } })).StatusCode);
        Assert.Equal(before, reads);
    }
}

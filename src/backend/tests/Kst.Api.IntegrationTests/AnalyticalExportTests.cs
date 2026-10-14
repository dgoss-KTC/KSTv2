using System.Net;
using System.Globalization;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Kst.Api.Dtos;
using Kst.Application.Bom;
using Kst.Application.Mps;
using Kst.Domain.Common;
using Kst.Domain.Exports;
using Kst.Domain.Mps;
using Kst.Exports;
using Microsoft.Extensions.DependencyInjection;

namespace Kst.Api.IntegrationTests;

/// <summary>Independent synthetic expectations inspect actual XLSX cells, not generator internals.</summary>
public sealed class AnalyticalExportTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    private static AnalyticalExportRequestDto Request(Guid workspace, Guid snapshot, string kind = "MPS") => new(
        kind, workspace, snapshot, "SW", Today, null, "dueDate", 2, null, null, false, null, "", "", "all", "all", null, [], ["FY27 Q2 P5", "FY27 Q2 P6"], []);

    [Theory]
    [InlineData("dueDate", null, 2, 7)]
    [InlineData("releaseDate", null, 2, 3)]
    [InlineData("dueDate", "P-2", 1, 0)]
    public async Task Mps_loaded_snapshot_basis_scope_order_and_exact_precision(string basis, string? parent, int rows, int first)
    {
        await using var factory = new KstApiFactory(); using var client = factory.CreateClient();
        var id = await Workspace(client); var snapshot = Seed(factory, id);
        var request = Request(id, snapshot.Id.Value) with { DateBasis = basis, Parent = parent };
        using var book = await Export(client, request);
        var sheet = book.Worksheet("MPS");
        Assert.Equal(rows + 1, sheet.LastRowUsed()!.RowNumber());
        Assert.Equal(parent ?? "P-1", sheet.Cell(2, 1).GetString());
        Assert.Equal(first, sheet.Cell(2, 4).GetDouble());
        Assert.Equal(first.ToString(), sheet.Cell(2, 4).GetFormattedString());
        Assert.Equal("1234567890123456.123456789", sheet.Cell(2, 3).GetString());
        Assert.Equal(XLDataType.Text, sheet.Cell(2, 3).DataType);
        Assert.Equal(5, sheet.LastColumnUsed()!.ColumnNumber());
        Assert.DoesNotContain(sheet.Row(1).CellsUsed(), c => c.GetString() == "Metric");
        Assert.Equal(MpsBusinessCalendar.GetWeekLabel(MpsBusinessCalendar.GetBusinessWeekStart(Today)).ToDateTime(TimeOnly.MinValue), sheet.Cell(1, 4).GetDateTime());
        Assert.NotEqual(sheet.Cell(1, 4).Style.Fill.BackgroundColor, sheet.Cell(1, 5).Style.Fill.BackgroundColor);
        Assert.Single(book.Worksheets);
        Assert.Equal("=HYPERLINK(\"x\")", sheet.Cell(2, 2).GetString()); Assert.False(sheet.Cell(2, 2).HasFormula);
        if (parent is null) Assert.Equal("P-2", sheet.Cell(3, 1).GetString());
        Samples(book, $"MPS-{basis}-{(parent is null ? "all" : "selected")}.xlsx");
    }

    [Fact]
    public async Task Bom_combined_filters_preserve_repeats_gaps_nulls_and_stale_metadata()
    {
        await using var factory = new KstApiFactory(); using var client = factory.CreateClient();
        var id = await Workspace(client); var snapshot = Seed(factory, id); var acquired = DateTimeOffset.UtcNow;
        var bom = new Bom("SW", "P-1", Today, [
            new("a", 1, "X", "M", false, "Other", 1, 0, 1, 0),
            new("b", 3, "REPEAT", "P", true, "+PCB", null, null, 1296899.0000000000m, 0),
            new("c", 5, "REPEAT", "P", true, "+PCB", 0.1234567890123456789m, 2.5m, 12.125m, 0)], acquired, false, null);
        factory.Services.GetRequiredService<IBomCacheStore>().Set(id, "P-1", new(id, "SW", "P-1", Today, snapshot.Id, bom));
        using var book = await Export(client, Request(id, snapshot.Id.Value, "BOM") with
        {
            Parent = "P-1",
            BomLoadedAtUtc = acquired,
            BomEffectiveDate = Today,
            IsStale = true,
            Warning = "Synthetic stale warning",
            ComponentFilter = " repeat ",
            DescriptionFilter = "pcb",
            PmFilter = "P",
            PhantomFilter = "yes"
        });
        var sheet = book.Worksheet("BOM");
        Assert.Equal(3, sheet.LastRowUsed()!.RowNumber()); Assert.Equal(3, sheet.Cell(2, 1).GetDouble()); Assert.Equal(5, sheet.Cell(3, 1).GetDouble());
        Assert.Equal("REPEAT", sheet.Cell(2, 2).GetString()); Assert.Equal("REPEAT", sheet.Cell(3, 2).GetString());
        Assert.True(sheet.Cell(2, 6).IsEmpty()); Assert.True(sheet.Cell(2, 7).IsEmpty());
        Assert.Equal("0.1234567890123456789", sheet.Cell(3, 6).GetString()); Assert.Equal(2.5, sheet.Cell(3, 7).GetDouble());
        Assert.Equal(XLDataType.Number, sheet.Cell(2, 8).DataType);
        Assert.Equal(1296899, sheet.Cell(2, 8).GetDouble());
        Assert.Equal("1,296,899", sheet.Cell(2, 8).GetFormattedString(CultureInfo.InvariantCulture));
        Assert.Equal("3", sheet.Cell(2, 1).GetFormattedString(CultureInfo.InvariantCulture));
        Assert.Equal("0", sheet.Cell(2, 9).GetFormattedString(CultureInfo.InvariantCulture));
        Assert.Equal(XLAlignmentHorizontalValues.Left, sheet.Cell(2, 1).Style.Alignment.Horizontal);
        Assert.Equal(XLAlignmentHorizontalValues.Left, sheet.Cell(1, 1).Style.Alignment.Horizontal);
        Assert.Contains("Synthetic stale warning", sheet.Cell(1, 1).GetComment().Text);
        Assert.Single(book.Worksheets);
        Samples(book, "BOM-filtered-stale.xlsx");
        using var empty = await Export(client, Request(id, snapshot.Id.Value, "BOM") with { Parent = "P-1", BomLoadedAtUtc = acquired, BomEffectiveDate = Today, ComponentFilter = "no-match" });
        Assert.Equal(1, empty.Worksheet("BOM").LastRowUsed()!.RowNumber()); Samples(empty, "BOM-empty.xlsx");
        var stale = await client.PostAsJsonAsync("/api/v1/reports/analytical", Request(id, Guid.NewGuid())); Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Planning_analytical_export_is_not_available()
    {
        await using var factory = new KstApiFactory(); using var client = factory.CreateClient();
        var id = await Workspace(client); var snapshot = Seed(factory, id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/reports/analytical", Request(id, snapshot.Id.Value, "Planning"))).StatusCode);
    }

    [Fact]
    public void Literal_cells_dates_empty_wide_and_capacity_failures()
    {
        var rows = new List<IReadOnlyList<AnalyticalCell>> { new AnalyticalCell[] { new("=1+1"), new("2026-10-12", "date"), new("123456789012345.1", "quantity"), new("-1.25", "currency"), new(null) } };
        var bytes = AnalyticalWorkbook.Create(new("Synthetic", [new("Warning", "Synthetic/offline")], [new("Values", ["Text", "Date", "Exact", "Money", "Null"], rows, 1)]));
        using var book = new XLWorkbook(new MemoryStream(bytes)); var s = book.Worksheet("Values");
        Assert.Single(book.Worksheets);
        Assert.False(s.Cell(2, 1).HasFormula); Assert.Equal("=1+1", s.Cell(2, 1).GetString()); Assert.Equal(new DateTime(2026, 10, 12), s.Cell(2, 2).GetDateTime());
        Assert.Equal(XLDataType.Text, s.Cell(2, 3).DataType); Assert.Equal(-1.25, s.Cell(2, 4).GetDouble()); Assert.True(s.Cell(2, 5).IsEmpty());
        var wide = new AnalyticalSheet("Wide", Enumerable.Range(0, 300).Select(i => "Week " + i).ToArray(), Enumerable.Range(0, 500).Select(_ => (IReadOnlyList<AnalyticalCell>)Enumerable.Repeat(new AnalyticalCell("1.125", "quantity"), 300).ToArray()).ToArray());
        var wideBytes = AnalyticalWorkbook.Create(new("Wide", [], [wide])); using var large = new XLWorkbook(new MemoryStream(wideBytes));
        Assert.Equal(300, large.Worksheet("Wide").LastColumnUsed()!.ColumnNumber()); Assert.Equal(501, large.Worksheet("Wide").LastRowUsed()!.RowNumber());
        Assert.Throws<ArgumentException>(() => AnalyticalWorkbook.Create(new("Oversize", [], [wide with { Rows = Enumerable.Repeat(wide.Rows[0], 1000).ToArray() }])));
        Assert.Throws<ArgumentException>(() => AnalyticalWorkbook.Create(new("Oversize", [], [new("Text", ["Text"], [new AnalyticalCell[] { new(new string('x', 32768)) }])])));
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("48.0000000000", "48")]
    [InlineData("-48", "-48")]
    [InlineData("1008", "1,008")]
    [InlineData("12.125", "12.125")]
    public void Quantity_display_has_no_decimal_point_for_whole_values(string value, string expected)
    {
        using var book = new XLWorkbook(new MemoryStream(AnalyticalWorkbook.Create(new("Synthetic", [], [new("MPS", ["Quantity"], [new AnalyticalCell[] { new(value, "quantity") }])]))));
        Assert.Equal(expected, book.Worksheet("MPS").Cell(2, 1).GetFormattedString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("0.000000000000000000000000000001")]
    [InlineData("0.00000000000000000000000000012345")]
    public void Sub_decimal_scale_values_remain_exact_literal_text(string value)
    {
        using var book = new XLWorkbook(new MemoryStream(AnalyticalWorkbook.Create(new("Exact", [], [new("Exact", ["Value"], [new AnalyticalCell[] { new(value, "currency") }])]))));
        Assert.Equal(value, book.Worksheet("Exact").Cell(2, 1).GetString()); Assert.Equal(XLDataType.Text, book.Worksheet("Exact").Cell(2, 1).DataType);
    }

    private static async Task<Guid> Workspace(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-KST-Report-Session", Guid.NewGuid().ToString());
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", new { site = "SW", parentParts = new[] { "P-1", "P-2" }, isTemporary = false }); response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("assignmentId").GetGuid();
    }
    private static MpsSnapshot Seed(KstApiFactory factory, Guid id)
    {
        var monday = MpsBusinessCalendar.GetWeekLabel(MpsBusinessCalendar.GetBusinessWeekStart(Today));
        var snapshot = new MpsSnapshot(SnapshotId.New(), DateTimeOffset.UtcNow, "SW", [new("P-1", "=HYPERLINK(\"x\")"), new("P-2", "=HYPERLINK(\"x\")")], [
            new("TEST", "SW", "P-1", null, monday.AddDays(-8), monday, 1234567890123456.123456789m, MpsSupplyType.Supply, "old", MpsWorkOrderState.Released),
            new("TEST", "SW", "P-2", null, monday.AddDays(-8), null, 1234567890123456.123456789m, MpsSupplyType.Supply, "old2", MpsWorkOrderState.Released),
            new("TEST", "SW", "P-1", null, monday, monday.AddDays(7), 7m, MpsSupplyType.Supply, "new", MpsWorkOrderState.Planned),
            new("TEST", "SW", "P-1", null, monday.AddDays(7), monday, 3m, MpsSupplyType.Supply, "new2", MpsWorkOrderState.Frozen)]);
        // The release-date current bucket also includes the past-due released row by accepted MPS rules.
        snapshot = snapshot with { SourceRows = snapshot.SourceRows.Select(r => r.WorkOrderId == "old" ? r with { ReleaseDate = null } : r).ToArray() };
        factory.Services.GetRequiredService<IMpsSnapshotStore>().SetLoaded(id, snapshot); return snapshot;
    }
    private static async Task<XLWorkbook> Export(HttpClient client, AnalyticalExportRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/reports/analytical", request); response.EnsureSuccessStatusCode();
        var prepared = (await response.Content.ReadFromJsonAsync<AnalyticalExportPreparedDto>())!;
        var bytes = await client.GetByteArrayAsync($"/api/v1/reports/analytical/preparations/{prepared.PreparationId}/bytes");
        return new XLWorkbook(new MemoryStream(bytes));
    }
    private static void Samples(XLWorkbook book, string name)
    {
        var folder = Environment.GetEnvironmentVariable("KST_STAGE21_SAMPLES");
        if (folder is not null) { Directory.CreateDirectory(folder); book.SaveAs(Path.Combine(folder, name)); }
    }
}

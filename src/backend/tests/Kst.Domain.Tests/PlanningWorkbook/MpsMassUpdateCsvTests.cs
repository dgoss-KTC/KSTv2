using System.Text;
using Kst.Domain.PlanningWorkbook;
using Kst.Exports;

namespace Kst.Domain.Tests.PlanningWorkbook;

/// <summary>
/// Stage 14.6 Mass Updater CSV serialization. The expected bytes come from the accepted golden fixture
/// (<c>docs/reference/Stage14Fixtures/MpsAdjustments.expected.csv</c>); expected quantities and dates are
/// never edited to make a test pass.
/// </summary>
public sealed class MpsMassUpdateCsvTests
{
    private static readonly Stage14Fixture Fixture = Stage14Fixture.Load();

    private static byte[] Golden() => File.ReadAllBytes(Path.Combine(
        Fixture.RepositoryRoot, "docs", "reference", "Stage14Fixtures", "MpsAdjustments.expected.csv"));

    private static List<MpsAdjustmentEntry> ScenarioEntries() =>
        Fixture.Root.GetProperty("adjustmentScenario").GetProperty("adjustments").EnumerateArray()
            .Select(a => new MpsAdjustmentEntry(a.GetProperty("part").GetString()!, Stage14Fixture.Date(a.GetProperty("weekLabel")), a.GetProperty("adjustment").GetString()))
            .ToList();

    [Fact]
    public void Accepted_Scenario_Produces_The_Golden_File_Byte_For_Byte()
    {
        var result = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(Fixture.LoadedSnapshotId, ScenarioEntries()), Fixture.Context());
        Assert.True(result.IsValid);

        var bytes = MpsMassUpdateCsv.Create(result.Rows);

        Assert.Equal(Golden(), bytes);
    }

    [Fact]
    public void Golden_File_Has_No_Header_No_Bom_And_Crlf_After_Every_Row_Including_The_Last()
    {
        var bytes = MpsMassUpdateCsv.Create(
            MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(Fixture.LoadedSnapshotId, ScenarioEntries()), Fixture.Context()).Rows);

        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        var text = new UTF8Encoding(false, true).GetString(bytes);
        Assert.Equal("P-2,-100,9/28/2026\r\nP-1,-50,9/14/2026\r\nP-1,10,9/21/2026\r\nP-1,20,10/5/2026\r\n", text);
        Assert.EndsWith("\r\n", text);
        Assert.Equal(4, text.Split("\r\n").Length - 1);
        Assert.DoesNotContain("\n", text.Replace("\r\n", ""));
        Assert.DoesNotContain("+", text);
        Assert.DoesNotContain("Part", text); // no header row
    }

    [Fact]
    public void Rows_Are_Written_In_The_Order_Supplied_And_Entry_Order_Does_Not_Change_The_File()
    {
        var entries = ScenarioEntries();
        var forward = MpsMassUpdateCsv.Create(MpsAdjustmentValidator.Validate(
            new MpsAdjustmentRequest(Fixture.LoadedSnapshotId, entries), Fixture.Context()).Rows);
        var reversed = MpsMassUpdateCsv.Create(MpsAdjustmentValidator.Validate(
            new MpsAdjustmentRequest(Fixture.LoadedSnapshotId, entries.AsEnumerable().Reverse().ToList()), Fixture.Context()).Rows);

        Assert.Equal(forward, reversed);

        // The serializer itself never reorders what the validator supplied.
        var supplied = new List<MpsMassUpdateRow> { new("B", 1m, new DateOnly(2026, 9, 21)), new("A", 2m, new DateOnly(2026, 9, 14)) };
        Assert.Equal("B,1,9/21/2026\r\nA,2,9/14/2026\r\n", Encoding.UTF8.GetString(MpsMassUpdateCsv.Create(supplied)));
    }

    [Fact]
    public void Zero_No_Op_Entries_Falldown_And_Quarter_Summaries_Never_Produce_Rows()
    {
        var entries = ScenarioEntries();
        entries.Add(new MpsAdjustmentEntry("P-2", new DateOnly(2026, 9, 21), "0"));
        entries.Add(new MpsAdjustmentEntry("P-2", new DateOnly(2026, 10, 5), "-0"));
        entries.Add(new MpsAdjustmentEntry("P-2", new DateOnly(2026, 10, 12), ""));
        var result = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(Fixture.LoadedSnapshotId, entries), Fixture.Context());
        Assert.True(result.IsValid);

        Assert.Equal(Golden(), MpsMassUpdateCsv.Create(result.Rows));

        // Falldown (no week) is not adjustable: the entry rejects the request, so no file can exist.
        var falldown = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(Fixture.LoadedSnapshotId,
            [.. entries, new MpsAdjustmentEntry("P-1", null, "5")]), Fixture.Context());
        Assert.False(falldown.IsValid);
        Assert.Empty(falldown.Rows);
    }

    [Fact]
    public void Serializer_Refuses_Zero_Fractional_Empty_Or_Non_Monday_Rows_Instead_Of_Rounding()
    {
        var monday = new DateOnly(2026, 9, 14);
        Assert.Throws<ArgumentException>(() => MpsMassUpdateCsv.Create([]));
        Assert.Throws<ArgumentException>(() => MpsMassUpdateCsv.Create([new("P-1", 0m, monday)]));
        Assert.Throws<ArgumentException>(() => MpsMassUpdateCsv.Create([new("P-1", 1.5m, monday)]));
        Assert.Throws<ArgumentException>(() => MpsMassUpdateCsv.Create([new("P-1", 10.0000001m, monday)]));
        Assert.Throws<ArgumentException>(() => MpsMassUpdateCsv.Create([new("P-1", 5m, monday.AddDays(1))]));
        Assert.Throws<ArgumentException>(() => MpsMassUpdateCsv.Create([new("", 5m, monday)]));
    }

    [Fact]
    public void Quantities_Use_Invariant_Whole_Number_Text_With_No_Leading_Plus_Or_Grouping()
    {
        var bytes = MpsMassUpdateCsv.Create(
        [
            new("P-1", 5m, new DateOnly(2026, 9, 14)),
            new("P-1", -1234567m, new DateOnly(2026, 10, 5)),
            new("P-1", 10.0m, new DateOnly(2026, 11, 2)), // scale 1 but whole: written without decimals
            new("P-1", 79228162514264337593543950335m, new DateOnly(2027, 1, 4)),
        ]);

        Assert.Equal(
            "P-1,5,9/14/2026\r\nP-1,-1234567,10/5/2026\r\nP-1,10,11/2/2026\r\nP-1,79228162514264337593543950335,1/4/2027\r\n",
            Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Dates_Are_M_d_yyyy_Without_Leading_Zeros_Whatever_The_Current_Culture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var text = Encoding.UTF8.GetString(MpsMassUpdateCsv.Create([new("P-1", -1000m, new DateOnly(2026, 3, 2))]));
            Assert.Equal("P-1,-1000,3/2/2026\r\n", text);
        }
        finally { Thread.CurrentThread.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("P,1", "\"P,1\"")]
    [InlineData("P\"1", "\"P\"\"1\"")]
    [InlineData("P\r1", "\"P\r1\"")]
    [InlineData("P\n1", "\"P\n1\"")]
    [InlineData("P 1", "P 1")]
    [InlineData("=SUM(A1)", "=SUM(A1)")]   // no spreadsheet-formula prefixing
    [InlineData("-X1", "-X1")]
    [InlineData("é-ü", "é-ü")]
    public void Part_Numbers_Are_Quoted_Only_When_Needed_And_Never_Formula_Prefixed(string part, string expectedField)
    {
        var bytes = MpsMassUpdateCsv.Create([new(part, -7m, new DateOnly(2026, 9, 14))]);

        Assert.Equal($"{expectedField},-7,9/14/2026\r\n", new UTF8Encoding(false, true).GetString(bytes));
    }

    [Theory]
    [InlineData("Acme Corp", "Acme Corp-MPS-Adjustments-2026-09-14.csv")]
    [InlineData("Plant: A/B?", "Plant- A-B-MPS-Adjustments-2026-09-14.csv")]
    [InlineData("   ", "Workspace-MPS-Adjustments-2026-09-14.csv")]
    [InlineData(null, "Workspace-MPS-Adjustments-2026-09-14.csv")]
    [InlineData("..", "Workspace-MPS-Adjustments-2026-09-14.csv")]
    public void Suggested_File_Name_Is_Safe_And_Dated(string? label, string expected)
    {
        Assert.Equal(expected, MpsMassUpdateCsv.SuggestedFileName(label, new DateOnly(2026, 9, 14)));
    }

    [Fact]
    public void Suggested_File_Name_Is_Length_Bounded()
    {
        var name = MpsMassUpdateCsv.SuggestedFileName(new string('x', 500), new DateOnly(2026, 9, 14));
        Assert.True(name.Length < 140);
        Assert.EndsWith("-MPS-Adjustments-2026-09-14.csv", name);
    }
}

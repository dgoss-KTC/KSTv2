using System.Globalization;
using System.Text;
using System.Text.Json;
using Kst.Domain.PlanningWorkbook;

namespace Kst.Domain.Tests.PlanningWorkbook;

public sealed class MpsAdjustmentValidatorTests
{
    private static readonly Stage14Fixture Fixture = Stage14Fixture.Load();

    public static TheoryData<string> ValidationCaseIds()
    {
        var data = new TheoryData<string>();
        foreach (var c in Fixture.Root.GetProperty("validationCases").GetProperty("cases").EnumerateArray())
            data.Add(c.GetProperty("id").GetString()!);
        return data;
    }

    public static TheoryData<string> DraftCaseIds()
    {
        var data = new TheoryData<string>();
        foreach (var c in Fixture.Root.GetProperty("draftRestoreCases").GetProperty("cases").EnumerateArray())
        {
            if (!c.TryGetProperty("inSession", out _))
                data.Add(c.GetProperty("id").GetString()!);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ValidationCaseIds))]
    public void Fixture_Validation_Case(string id)
    {
        var c = Case("validationCases", id);
        var snapshot = c.TryGetProperty("requestMpsSnapshotId", out var requested) ? requested.GetGuid() : Fixture.LoadedSnapshotId;
        var result = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(snapshot, Entries(c)), Fixture.Context());
        var expectedResult = c.GetProperty("result").GetString();

        switch (expectedResult)
        {
            case "valid":
                Assert.True(result.IsValid);
                var entry = Assert.Single(result.Entries);
                Assert.Equal(Stage14Fixture.Dec(c, "existingMps"), entry.ExistingMps);
                Assert.Equal(Stage14Fixture.Dec(c, "proposedMps"), entry.ProposedMps);
                Assert.Single(result.Rows);
                break;
            case "no-op":
                Assert.True(result.IsValid);
                Assert.True(Assert.Single(result.Entries).IsNoOp);
                Assert.Empty(result.Rows);
                break;
            case "reject":
            case "reject-whole-export":
                Assert.False(result.IsValid);
                Assert.Equal(c.GetProperty("issueCode").GetString(), result.Issue!.Value.ToCode());
                Assert.Empty(result.Rows);
                if (c.TryGetProperty("affectedRowCount", out var count))
                    Assert.Equal(count.GetInt32(), result.AffectedRowCount);
                if (c.TryGetProperty("proposedMps", out _))
                {
                    var rejected = Assert.Single(result.Entries);
                    Assert.Equal(Stage14Fixture.Dec(c, "existingMps"), rejected.ExistingMps);
                    Assert.Equal(Stage14Fixture.Dec(c, "proposedMps"), rejected.ProposedMps);
                }
                break;
            default:
                throw new InvalidOperationException($"Unknown fixture result {expectedResult}.");
        }
    }

    [Fact]
    public void Scenario_Produces_Expected_Mass_Updater_Rows_In_Part_Then_Week_Order()
    {
        var scenario = Fixture.Root.GetProperty("adjustmentScenario");
        var expected = scenario.GetProperty("expectedCsvRows").EnumerateArray()
            .Select(r => string.Join(',', r.EnumerateArray().Select(x => x.GetString())))
            .ToList();

        var entries = ScenarioEntries();
        var result = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(Fixture.LoadedSnapshotId, entries), Fixture.Context());
        Assert.True(result.IsValid);
        Assert.Equal(expected, result.Rows.Select(Logical));

        // Deterministic: entry order does not change output order.
        var reversed = MpsAdjustmentValidator.Validate(
            new MpsAdjustmentRequest(Fixture.LoadedSnapshotId, entries.AsEnumerable().Reverse().ToList()), Fixture.Context());
        Assert.Equal(result.Rows, reversed.Rows);

        // The accepted golden file carries the same logical rows (serialization is a later checkpoint).
        var golden = Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(
            Fixture.RepositoryRoot, "docs", "reference", "Stage14Fixtures", "MpsAdjustments.expected.csv")));
        Assert.Equal(expected, golden.Split("\r\n", StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void Output_Uses_Retained_Part_Spelling_And_Order()
    {
        var entries = new List<MpsAdjustmentEntry>
        {
            new("p-1", new DateOnly(2026, 9, 21), "3"),
            new("p-2", new DateOnly(2026, 9, 14), "4")
        };
        var result = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(Fixture.LoadedSnapshotId, entries), Fixture.Context());
        Assert.Equal(["P-2", "P-1"], result.Rows.Select(r => r.Part));
    }

    [Fact]
    public void Forecast_Cannot_Affect_Validation_Or_Output()
    {
        // Export validation receives only the request and the loaded MPS context; neither carries
        // forecast facts. Workbook-side non-participation is asserted in PlanningWorkbookBuilderTests.
        var carriesForecast = new[] { typeof(MpsAdjustmentContext), typeof(MpsAdjustmentRequest), typeof(MpsAdjustmentEntry) }
            .SelectMany(t => t.GetProperties())
            .Any(p => p.PropertyType == typeof(PlanningForecastRow)
                || p.PropertyType.GenericTypeArguments.Contains(typeof(PlanningForecastRow)));
        Assert.False(carriesForecast);
    }

    [Fact]
    public void Baseline_Is_Independent_Of_The_Visible_Horizon()
    {
        // P-1 has 60 due 2026-10-15 (week 2026-10-12, beyond the 4-week fixture horizon).
        var result = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(Fixture.LoadedSnapshotId,
            [new MpsAdjustmentEntry("P-1", new DateOnly(2026, 10, 12), "-60")]), Fixture.Context());
        Assert.True(result.IsValid);
        Assert.Equal(60m, result.Entries[0].ExistingMps);
        Assert.Equal(0m, result.Entries[0].ProposedMps);
    }

    [Fact]
    public void Any_Invalid_Entry_Rejects_The_Whole_Request_And_Reports_All_Affected_Entries()
    {
        var entries = ScenarioEntries().Concat(
        [
            new MpsAdjustmentEntry("P-1", new DateOnly(2026, 9, 28), "-150"),
            new MpsAdjustmentEntry("P-3", new DateOnly(2026, 9, 21), "1.5")
        ]).ToList();
        var result = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(Fixture.LoadedSnapshotId, entries), Fixture.Context());

        Assert.False(result.IsValid);
        Assert.Empty(result.Rows);
        Assert.Equal(2, result.AffectedRowCount);
        Assert.Equal(MpsAdjustmentIssue.NotWholeNumber, result.Issue);
        Assert.Equal(4, result.Entries.Count(e => e.Issue is null));
    }

    [Fact]
    public void Released_Work_Orders_And_Sales_Order_Shortfalls_Do_Not_Block()
    {
        // Week 2026-09-14 for P-1 contains a released work order and SO 80; reducing MPS to 0 is valid.
        var result = MpsAdjustmentValidator.Validate(new MpsAdjustmentRequest(Fixture.LoadedSnapshotId,
            [new MpsAdjustmentEntry("P-1", new DateOnly(2026, 9, 14), "-100")]), Fixture.Context());
        Assert.True(result.IsValid);
        Assert.Equal(new MpsMassUpdateRow("P-1", -100m, new DateOnly(2026, 9, 14)), Assert.Single(result.Rows));
    }

    [Theory]
    [InlineData("25", MpsAdjustmentInputKind.Value, "25")]
    [InlineData("-25", MpsAdjustmentInputKind.Value, "-25")]
    [InlineData("007", MpsAdjustmentInputKind.Value, "7")]
    [InlineData("0", MpsAdjustmentInputKind.NoOp, null)]
    [InlineData("-0", MpsAdjustmentInputKind.NoOp, null)]
    [InlineData("", MpsAdjustmentInputKind.NoOp, null)]
    [InlineData(null, MpsAdjustmentInputKind.NoOp, null)]
    [InlineData("10.5", MpsAdjustmentInputKind.NotWholeNumber, null)]
    [InlineData("10.0", MpsAdjustmentInputKind.NotWholeNumber, null)]
    [InlineData("-0.5", MpsAdjustmentInputKind.NotWholeNumber, null)]
    [InlineData("+5", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData("1e3", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData("1,000", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData(" 5", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData("5 ", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData("-", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData("10.", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData(".5", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData("--5", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData("\u0665", MpsAdjustmentInputKind.InvalidNumber, null)]
    [InlineData("99999999999999999999999999999999", MpsAdjustmentInputKind.InvalidNumber, null)]
    public void Whole_Number_Input_Rules(string? text, MpsAdjustmentInputKind kind, string? value)
    {
        var result = MpsAdjustmentInput.Parse(text);
        Assert.Equal(kind, result.Kind);
        Assert.Equal(value is null ? null : decimal.Parse(value, CultureInfo.InvariantCulture), result.Value);
    }

    [Theory]
    [MemberData(nameof(DraftCaseIds))]
    public void Fixture_Draft_Restore_Case(string id)
    {
        var c = Case("draftRestoreCases", id);
        var saved = c.GetProperty("saved");
        var entry = new PlanningDraftEntry(
            saved.GetProperty("part").GetString()!, Stage14Fixture.Date(saved.GetProperty("weekLabel")),
            Stage14Fixture.Dec(saved, "originalMpsQty"), saved.GetProperty("adjustment").GetString()!);

        var comparison = Assert.Single(PlanningDraftReconciler.Compare([entry], Fixture.Context()));

        // The saved entry and its signed adjustment are always retained.
        Assert.Same(entry, comparison.Saved);
        Assert.Equal(saved.GetProperty("part").GetString(), comparison.Saved.Part);
        Assert.Equal(Stage14Fixture.Date(saved.GetProperty("weekLabel")), comparison.Saved.WeekLabel);
        Assert.Equal(Stage14Fixture.Dec(saved, "originalMpsQty"), comparison.Saved.OriginalMpsQuantity);
        Assert.Equal(saved.GetProperty("adjustment").GetString(), comparison.Saved.AdjustmentText);
        Assert.Equal(Stage14Fixture.Dec(saved, "adjustment"), comparison.RestoredAdjustment);
        Assert.Equal(c.GetProperty("exportEligible").GetBoolean(), comparison.ExportEligible);

        if (c.TryGetProperty("currentMps", out _))
        {
            // Baseline evaluated (D01-D03): numeric values are reported even with a negative-result issue.
            Assert.NotNull(comparison.CurrentMps);
            Assert.NotNull(comparison.ProposedMps);
            Assert.Equal(Stage14Fixture.Dec(c, "currentMps"), comparison.CurrentMps);
            Assert.Equal(c.GetProperty("baselineChanged").GetBoolean(), comparison.BaselineChanged);
            Assert.Equal(Stage14Fixture.Dec(c, "restoredAdjustment"), comparison.RestoredAdjustment);
            Assert.Equal(Stage14Fixture.Dec(c, "proposedMps"), comparison.ProposedMps);
        }
        else
        {
            // Baseline unavailable (D04-D05): retained and flagged, with no baseline values.
            Assert.True(c.GetProperty("retained").GetBoolean());
            Assert.Null(comparison.CurrentMps);
            Assert.Null(comparison.ProposedMps);
            Assert.False(comparison.BaselineChanged);
            Assert.True(c.TryGetProperty("issueCode", out _), "An unavailable baseline must carry an issue code.");
            Assert.False(comparison.ExportEligible);
        }

        if (c.TryGetProperty("issueCode", out var issue))
        {
            Assert.NotNull(comparison.Issue);
            Assert.Equal(issue.GetString(), comparison.Issue!.Value.ToCode());
            Assert.False(comparison.ExportEligible);
        }
        else
        {
            Assert.Null(comparison.Issue);
        }
    }

    [Fact]
    public void Draft_With_Valid_Zero_Baseline_Reports_Numeric_Zero_Not_Unavailable()
    {
        // P-1 week 2026-10-05 has no MPS rows: the baseline is an evaluated zero, distinct from null.
        var entry = new PlanningDraftEntry("P-1", new DateOnly(2026, 10, 5), 0m, "20");
        var comparison = Assert.Single(PlanningDraftReconciler.Compare([entry], Fixture.Context()));

        Assert.Equal(0m, comparison.CurrentMps);
        Assert.Equal(20m, comparison.ProposedMps);
        Assert.False(comparison.BaselineChanged);
        Assert.Null(comparison.Issue);
        Assert.True(comparison.ExportEligible);
    }

    [Fact]
    public void Issue_Codes_Are_Stable()
    {
        Assert.Equal(
            ["snapshot-changed", "not-adjustable", "invalid-number", "not-whole-number", "not-a-week",
             "week-passed", "part-out-of-scope", "duplicate-identity", "negative-result"],
            Enum.GetValues<MpsAdjustmentIssue>().Select(i => i.ToCode()));
    }

    private static List<MpsAdjustmentEntry> ScenarioEntries() =>
        Fixture.Root.GetProperty("adjustmentScenario").GetProperty("adjustments").EnumerateArray()
            .Select(SingleEntry).ToList();

    private static List<MpsAdjustmentEntry> Entries(JsonElement c)
    {
        if (!c.TryGetProperty("entries", out var entries))
            return [SingleEntry(c)];
        if (entries.ValueKind == JsonValueKind.Array)
            return entries.EnumerateArray().Select(SingleEntry).ToList();
        // V22: "adjustmentScenario.adjustments plus V01".
        Assert.Equal("adjustmentScenario.adjustments plus V01", entries.GetString());
        return [.. ScenarioEntries(), SingleEntry(Case("validationCases", "V01"))];
    }

    private static MpsAdjustmentEntry SingleEntry(JsonElement e)
    {
        DateOnly? week = e.TryGetProperty("target", out var target) && target.GetString() == "Falldown"
            ? null
            : Stage14Fixture.Date(e.GetProperty("weekLabel"));
        return new MpsAdjustmentEntry(e.GetProperty("part").GetString()!, week, e.GetProperty("adjustment").GetString());
    }

    private static JsonElement Case(string section, string id) =>
        Fixture.Root.GetProperty(section).GetProperty("cases").EnumerateArray()
            .Single(c => c.GetProperty("id").GetString() == id);

    private static string Logical(MpsMassUpdateRow row) => string.Join(',',
        row.Part,
        row.Adjustment.ToString("0", CultureInfo.InvariantCulture),
        $"{row.WeekLabel.Month}/{row.WeekLabel.Day}/{row.WeekLabel.Year}");
}

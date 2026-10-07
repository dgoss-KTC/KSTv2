using System.Globalization;
using Kst.Domain.Mps;
using Kst.Domain.PartDetail;
using Kst.Domain.PlanningWorkbook;

namespace Kst.Domain.Tests.PlanningWorkbook;

public sealed class PlanningWorkbookBuilderTests
{
    private static readonly Stage14Fixture Fixture = Stage14Fixture.Load();
    private static readonly string[] BucketFields =
        ["so", "soDollars", "mps", "mpsDollars", "forecast", "proposedMps", "adjustedMpsDollars", "estOnHand", "adjOnHand"];

    [Fact]
    public void Fixture_Falldown_And_Weekly_Expectations_Are_Reproduced_Exactly()
    {
        var workbook = PlanningWorkbookBuilder.Build(Fixture.Input());

        Assert.Equal(Fixture.WeekLabels, workbook.WeekLabels);
        var expectedParts = Fixture.Root.GetProperty("expected").GetProperty("parts").EnumerateArray().ToList();
        Assert.Equal(expectedParts.Select(p => p.GetProperty("part").GetString()), workbook.Parts.Select(p => p.Part));

        foreach (var (expected, block) in expectedParts.Zip(workbook.Parts))
        {
            Assert.Equal(Stage14Fixture.NullableDec(expected, "selectedPrice"), block.SelectedPrice);
            Assert.Equal(expected.GetProperty("priced").GetBoolean(), block.IsPriced);
            AssertBucket(block.Part, "Falldown", expected.GetProperty("falldown"), block.Falldown);
            Assert.Equal(0m, block.Falldown.Adjustment);

            var weeks = expected.GetProperty("weeks").EnumerateArray().ToList();
            Assert.Equal(weeks.Count, block.Weeks.Count);
            foreach (var (expectedWeek, bucket) in weeks.Zip(block.Weeks))
            {
                Assert.Equal(Stage14Fixture.Date(expectedWeek.GetProperty("weekLabel")), bucket.WeekLabel);
                AssertBucket(block.Part, bucket.WeekLabel!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), expectedWeek, bucket);
                Assert.Equal(Stage14Fixture.Dec(expectedWeek, "adjustment"), bucket.Adjustment);
            }
        }
    }

    [Fact]
    public void Undated_NonRa_Lines_Are_Kept_Separately_And_Excluded_From_Dated_Calculations()
    {
        var workbook = PlanningWorkbookBuilder.Build(Fixture.Input());
        foreach (var expected in Fixture.Root.GetProperty("expected").GetProperty("parts").EnumerateArray())
        {
            var block = workbook.Parts.Single(p => p.Part == expected.GetProperty("part").GetString());
            var undated = expected.GetProperty("undated");
            Assert.Equal(Stage14Fixture.Dec(undated, "so"), block.Undated.SalesOrderQuantity);
            Assert.Equal(Stage14Fixture.Dec(undated, "soDollars"), block.Undated.SalesOrderDollars);
            Assert.Equal(undated.GetProperty("lineCount").GetInt32(), block.Undated.LineCount);
        }

        // Removing every undated line changes nothing except the Undated totals.
        var datedOnly = Fixture.SalesOrderLines.Where(l => l.SourceValues.DueDate is not null).ToList();
        var withoutUndated = PlanningWorkbookBuilder.Build(Fixture.Input(salesOrders: datedOnly));
        foreach (var (a, b) in workbook.Parts.Zip(withoutUndated.Parts))
        {
            Assert.Equal(a.Falldown, b.Falldown);
            Assert.Equal(a.Weeks, b.Weeks);
            Assert.Equal(new PlanningUndatedDemand(0m, 0m, 0), b.Undated);
        }
    }

    [Fact]
    public void Ra_Orders_Are_Excluded_Before_All_Workbook_Totals_Including_Undated()
    {
        var workbook = PlanningWorkbookBuilder.Build(Fixture.Input());
        var nonRa = Fixture.SalesOrderLines.Where(l => !l.Key.SalesOrder.StartsWith("RA", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, Fixture.SalesOrderLines.Count - nonRa.Count);

        var withoutRa = PlanningWorkbookBuilder.Build(Fixture.Input(salesOrders: nonRa));
        Assert.Equal(workbook.Parts.Select(Snapshot), withoutRa.Parts.Select(Snapshot));

        // The exclusion is operative: the same lines without the RA prefix would count.
        var renamed = Fixture.SalesOrderLines
            .Select(l => l with { Key = l.Key with { SalesOrder = l.Key.SalesOrder.Replace("RA-", "SO-RA-", StringComparison.Ordinal) } })
            .ToList();
        var counted = PlanningWorkbookBuilder.Build(Fixture.Input(salesOrders: renamed)).Parts.Single(p => p.Part == "P-1");
        Assert.Equal(85m, counted.Weeks[0].SalesOrderQuantity);
        Assert.Equal(5m, counted.Undated.SalesOrderQuantity);
        Assert.Equal(12.50m, counted.Undated.SalesOrderDollars);
    }

    [Theory]
    [InlineData("RA-")]
    [InlineData("ra-")]
    [InlineData("Ra-")]
    [InlineData("rA-")]
    public void Return_Order_Prefix_Is_Case_Insensitive_Across_Quantities_And_Dollars(string prefix)
    {
        var baseline = PlanningWorkbookBuilder.Build(Fixture.Input());
        var variant = Fixture.SalesOrderLines
            .Select(l => l with { Key = l.Key with { SalesOrder = l.Key.SalesOrder.Replace("RA-", prefix, StringComparison.Ordinal) } })
            .ToList();
        Assert.Equal(2, variant.Count(l => l.Key.SalesOrder.StartsWith(prefix, StringComparison.Ordinal)));
        var workbook = PlanningWorkbookBuilder.Build(Fixture.Input(salesOrders: variant));
        // Excluded before Falldown, weekly, undated and every dollar/quantity figure: identical to the uppercase result.
        Assert.Equal(baseline.Parts.Select(Snapshot), workbook.Parts.Select(Snapshot));
        Assert.Equal(baseline.Parts.Select(p => p.Undated), workbook.Parts.Select(p => p.Undated));
        Assert.Equal(baseline.Parts.Select(p => p.Falldown.SalesOrderQuantity), workbook.Parts.Select(p => p.Falldown.SalesOrderQuantity));
        Assert.Equal(baseline.Parts.Select(p => p.Falldown.SalesOrderDollars), workbook.Parts.Select(p => p.Falldown.SalesOrderDollars));
    }

    [Theory]
    [InlineData("SO-RA-")]
    [InlineData("XRA-")]
    [InlineData(" RA-")]   // no trimming: a leading space is not the RA prefix
    [InlineData("R-A-")]
    public void Orders_That_Do_Not_Start_With_Ra_Remain_Included(string prefix)
    {
        var variant = Fixture.SalesOrderLines
            .Select(l => l with { Key = l.Key with { SalesOrder = l.Key.SalesOrder.Replace("RA-", prefix, StringComparison.Ordinal) } })
            .ToList();
        var counted = PlanningWorkbookBuilder.Build(Fixture.Input(salesOrders: variant)).Parts.Single(p => p.Part == "P-1");
        Assert.Equal(85m, counted.Weeks[0].SalesOrderQuantity);
        Assert.Equal(5m, counted.Undated.SalesOrderQuantity);
        Assert.Equal(12.50m, counted.Undated.SalesOrderDollars);
    }
    [Fact]
    public void Forecast_Is_Absolute_Display_Only_Including_Falldown()
    {
        var withForecast = PlanningWorkbookBuilder.Build(Fixture.Input());
        var withoutForecast = PlanningWorkbookBuilder.Build(Fixture.Input(forecast: []));

        var p1 = withForecast.Parts.Single(p => p.Part == "P-1");
        Assert.Equal(15m, p1.Falldown.Forecast);
        Assert.Equal([0m, 40m, 40m, 0m], p1.Weeks.Select(w => w.Forecast));

        foreach (var (a, b) in withForecast.Parts.Zip(withoutForecast.Parts))
        {
            Assert.Equal(a.Falldown with { Forecast = 0m }, b.Falldown);
            Assert.Equal(a.Weeks.Select(w => w with { Forecast = 0m }), b.Weeks);
            Assert.Equal(a.Undated, b.Undated);
        }
    }

    [Fact]
    public void Projection_Seed_Includes_Falldown_And_Adjusted_On_Hand_Equals_Estimate_Plus_Cumulative_Adjustments()
    {
        var workbook = PlanningWorkbookBuilder.Build(Fixture.Input());
        foreach (var block in workbook.Parts)
        {
            var seed = block.NetQuantityOnHand + block.Falldown.Mps - block.Falldown.SalesOrderQuantity;
            Assert.Equal(seed, block.Falldown.EstimatedOnHand);
            Assert.Equal(seed, block.Falldown.AdjustedOnHand);

            var cumulative = 0m;
            foreach (var week in block.Weeks)
            {
                cumulative += week.Adjustment;
                Assert.Equal(week.EstimatedOnHand + cumulative, week.AdjustedOnHand);
            }
        }

        // Negative projected inventory is calculated and shown, not blocked (P-2).
        Assert.Contains(workbook.Parts.Single(p => p.Part == "P-2").Weeks, w => w.AdjustedOnHand < 0m);
    }

    [Fact]
    public void Part_Without_List_Price_Uses_Exact_Zero_Dollars_And_Genuine_Zero_Price_Stays_Priced()
    {
        var workbook = PlanningWorkbookBuilder.Build(Fixture.Input());
        var zero = workbook.Parts.Single(p => p.Part == "P-2");
        var unpriced = workbook.Parts.Single(p => p.Part == "P-3");

        Assert.Equal(0m, zero.SelectedPrice);
        Assert.True(zero.IsPriced);
        Assert.All(zero.Weeks, w => { Assert.Equal(0m, w.MpsDollars); Assert.Equal(0m, w.AdjustedMpsDollars); });
        Assert.Equal(0m, unpriced.SelectedPrice);
        Assert.False(unpriced.IsPriced); // no eligible list price found: $0 is used (amendment 2026-10-07)
        Assert.All(unpriced.Weeks.Append(unpriced.Falldown), w => { Assert.Equal(0m, w.MpsDollars); Assert.Equal(0m, w.AdjustedMpsDollars); });
        Assert.Contains(unpriced.Weeks, w => w.Mps != 0m); // non-zero quantity still prices at exactly zero
        Assert.All(unpriced.Weeks.Where(w => w.Mps != 0m), w => Assert.Equal(0m, w.AdjustedMpsDollars));
        Assert.Equal(10.00m, unpriced.Undated.SalesOrderDollars);
    }

    [Fact]
    public void Fractional_Source_Quantities_And_Price_Precision_Are_Not_Rounded()
    {
        var p1 = PlanningWorkbookBuilder.Build(Fixture.Input()).Parts.Single(p => p.Part == "P-1");
        var week = p1.Weeks[1];
        Assert.Equal(75.5m, week.Mps);
        Assert.Equal(188.75m, week.MpsDollars);
        Assert.Equal(85.5m, week.ProposedMps);
        Assert.Equal(213.75m, week.AdjustedMpsDollars);

        var precise = PlanningWorkbookBuilder.Build(Fixture.Input() with
        {
            PriceTiers = new Dictionary<string, IReadOnlyList<PartPriceBreak>>(Fixture.PriceTiers)
            {
                ["P-1"] = [new PartPriceBreak(1m, 0.0000001m)]
            }
        }).Parts.Single(p => p.Part == "P-1");
        Assert.Equal(0.00000755m, precise.Weeks[1].MpsDollars);
    }

    [Fact]
    public void First_Tier_Is_Lowest_Moq_Regardless_Of_Input_Order()
    {
        IReadOnlyList<PartPriceBreak> tiers = [new(100m, 2.25m), new(1m, 2.5m), new(50m, 2.4m)];
        Assert.Equal(2.5m, PlanningPriceSelection.Select(tiers));
        Assert.Equal(0m, PlanningPriceSelection.Select([])); // successful absence of a list price
        Assert.Equal(0m, PlanningPriceSelection.Select([new PartPriceBreak(1m, 0m)]));
    }

    [Fact]
    public void D06_Signed_Adjustment_Is_Kept_When_The_Loaded_Baseline_Changes_In_Session()
    {
        var d06 = Fixture.Root.GetProperty("draftRestoreCases").GetProperty("cases").EnumerateArray()
            .Single(c => c.GetProperty("id").GetString() == "D06");
        Assert.True(d06.GetProperty("inSession").GetBoolean());
        Assert.False(d06.GetProperty("acknowledgmentRequired").GetBoolean());
        var before = d06.GetProperty("before");
        var after = d06.GetProperty("afterMpsRefresh");
        var part = before.GetProperty("part").GetString()!;
        var weekLabel = Stage14Fixture.Date(before.GetProperty("weekLabel"));
        var adjustment = Stage14Fixture.Dec(before, "adjustment");
        IReadOnlyList<ProposedMpsAdjustment> adjustments = [new ProposedMpsAdjustment(part, weekLabel, adjustment)];

        // Before: the loaded snapshot as supplied.
        var beforeWeek = Cell(PlanningWorkbookBuilder.Build(Fixture.Input(adjustments: adjustments)), part, weekLabel);
        Assert.Equal(Stage14Fixture.Dec(before, "mps"), beforeWeek.Mps);
        Assert.Equal(Stage14Fixture.Dec(before, "adjustment"), beforeWeek.Adjustment);
        Assert.Equal(Stage14Fixture.Dec(before, "proposedMps"), beforeWeek.ProposedMps);

        // After: an MPS refresh changes this part/week baseline to the fixture's refreshed quantity.
        var template = Fixture.MpsRows[0];
        var refreshRow = new MpsSourceRow(
            template.Domain, template.Site, part, null, weekLabel, null,
            Stage14Fixture.Dec(after, "mps") - Stage14Fixture.Dec(before, "mps"),
            MpsSupplyType.Supply, "WO-D06-REFRESH", MpsWorkOrderState.Planned);
        IReadOnlyList<MpsSourceRow> refreshedRows = [.. Fixture.MpsRows, refreshRow];
        var afterWeek = Cell(
            PlanningWorkbookBuilder.Build(Fixture.Input(adjustments: adjustments, mpsRows: refreshedRows)), part, weekLabel);

        Assert.Equal(Stage14Fixture.Dec(after, "mps"), afterWeek.Mps);
        Assert.Equal(Stage14Fixture.Dec(after, "adjustment"), afterWeek.Adjustment);
        Assert.Equal(Stage14Fixture.Dec(after, "proposedMps"), afterWeek.ProposedMps);

        // The signed adjustment is preserved unchanged, not converted to a target quantity.
        Assert.Equal(beforeWeek.Adjustment, afterWeek.Adjustment);
        Assert.NotEqual(beforeWeek.ProposedMps, afterWeek.ProposedMps);

        // The same signed entry validates against the refreshed loaded snapshot without acknowledgment.
        var validation = MpsAdjustmentValidator.Validate(
            new MpsAdjustmentRequest(Fixture.LoadedSnapshotId,
                [new MpsAdjustmentEntry(part, weekLabel, before.GetProperty("adjustment").GetString())]),
            Fixture.Context(refreshedRows));
        Assert.True(validation.IsValid);
        Assert.Equal(Stage14Fixture.Dec(after, "mps"), validation.Entries[0].ExistingMps);
        Assert.Equal(Stage14Fixture.Dec(after, "proposedMps"), validation.Entries[0].ProposedMps);
        Assert.Equal(new MpsMassUpdateRow(part, adjustment, weekLabel), Assert.Single(validation.Rows));
    }

    private static PlanningBucket Cell(Kst.Domain.PlanningWorkbook.PlanningWorkbook workbook, string part, DateOnly weekLabel) =>
        workbook.Parts.Single(p => p.Part == part).Weeks.Single(w => w.WeekLabel == weekLabel);

    [Fact]
    public void Missing_Inventory_Or_Price_Facts_Are_Rejected_Not_Zeroed()
    {
        var noInventory = Fixture.Input() with { NetQuantityOnHand = new Dictionary<string, decimal> { ["P-1"] = 1m, ["P-2"] = 1m } };
        Assert.Throws<ArgumentException>(() => PlanningWorkbookBuilder.Build(noInventory));

        var noPrice = Fixture.Input() with { PriceTiers = new Dictionary<string, IReadOnlyList<PartPriceBreak>> { ["P-1"] = [] } };
        Assert.Throws<ArgumentException>(() => PlanningWorkbookBuilder.Build(noPrice));
    }

    [Fact]
    public void Empty_Tier_Lists_For_Every_Part_Succeed_With_Zero_Dollars_And_Unchanged_Quantities()
    {
        var baseline = PlanningWorkbookBuilder.Build(Fixture.Input());
        var allEmpty = Fixture.Input() with { PriceTiers = Fixture.ResolvedParts.ToDictionary(p => p.ParentPart, _ => (IReadOnlyList<PartPriceBreak>)[]) };
        var workbook = PlanningWorkbookBuilder.Build(allEmpty);
        Assert.All(workbook.Parts, p => { Assert.False(p.IsPriced); Assert.Equal(0m, p.SelectedPrice); });
        Assert.All(workbook.Parts.SelectMany(p => p.Weeks.Append(p.Falldown)), b => { Assert.Equal(0m, b.MpsDollars); Assert.Equal(0m, b.AdjustedMpsDollars); });
        // Quantities, inventory and sales-order dollars are unaffected by the price amendment.
        Assert.Equal(baseline.Parts.SelectMany(p => p.Weeks.Select(w => (w.Mps, w.ProposedMps, w.EstimatedOnHand, w.AdjustedOnHand, w.SalesOrderDollars))),
            workbook.Parts.SelectMany(p => p.Weeks.Select(w => (w.Mps, w.ProposedMps, w.EstimatedOnHand, w.AdjustedOnHand, w.SalesOrderDollars))));
    }

    [Fact]
    public void Duplicate_Display_Adjustments_Are_Rejected()
    {
        var duplicate = Fixture.Input(adjustments:
        [
            new ProposedMpsAdjustment("P-1", new DateOnly(2026, 9, 21), 5m),
            new ProposedMpsAdjustment("p-1", new DateOnly(2026, 9, 21), 6m)
        ]);
        Assert.Throws<ArgumentException>(() => PlanningWorkbookBuilder.Build(duplicate));
    }

    [Fact]
    public void Sunday_Rows_Use_The_V2_Sunday_To_Saturday_Week()
    {
        var p1 = PlanningWorkbookBuilder.Build(Fixture.Input()).Parts.Single(p => p.Part == "P-1");
        // 2026-09-20 (Sunday, 50) and 2026-09-24 (25.5) fall in the week labelled 2026-09-21.
        Assert.Equal(new DateOnly(2026, 9, 21), p1.Weeks[1].WeekLabel);
        Assert.Equal(75.5m, p1.Weeks[1].Mps);
        Assert.Equal(MpsBucketKind.Weekly, p1.Weeks[1].Kind);
    }

    private static object Snapshot(PlanningPartBlock block) => (block.Falldown, string.Join('|', block.Weeks), block.Undated);

    private static void AssertBucket(string part, string label, System.Text.Json.JsonElement expected, PlanningBucket actual)
    {
        var values = new Dictionary<string, decimal?>
        {
            ["so"] = actual.SalesOrderQuantity,
            ["soDollars"] = actual.SalesOrderDollars,
            ["mps"] = actual.Mps,
            ["mpsDollars"] = actual.MpsDollars,
            ["forecast"] = actual.Forecast,
            ["proposedMps"] = actual.ProposedMps,
            ["adjustedMpsDollars"] = actual.AdjustedMpsDollars,
            ["estOnHand"] = actual.EstimatedOnHand,
            ["adjOnHand"] = actual.AdjustedOnHand
        };
        foreach (var field in BucketFields)
            Assert.True(Stage14Fixture.NullableDec(expected, field) == values[field],
                $"{part} {label} {field}: expected {expected.GetProperty(field)} actual {values[field]}");
    }
}

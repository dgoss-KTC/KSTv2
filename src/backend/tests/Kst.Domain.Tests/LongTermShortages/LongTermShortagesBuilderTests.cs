using Kst.Domain.LongTermShortages;
namespace Kst.Domain.Tests.LongTermShortages;
public sealed class LongTermShortagesBuilderTests
{
    [Fact]
    public void Build_UsesMondayWeeks_PastCarryIn_AndReleaseOnlyEvidence()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(200m, 0m,
            new(1, "DEMAND", new(2026, 9, 10), null, 98.2456140337m, MrpScheduleCategory.Unclassified),
            new(2, "SUPPLYP", new(2026, 9, 22), new(2026, 9, 18), 49m, MrpScheduleCategory.Unclassified),
            new(3, "SUPPLYP", null, new(2026, 9, 18), 11m, MrpScheduleCategory.Unclassified))]));

        Assert.Equal(new DateOnly(2026, 9, 14), row.Weeks[0].WeekStart);
        Assert.Equal(24, row.Weeks.Count);
        Assert.Equal(new DateOnly(2027, 2, 22), row.Weeks[^1].WeekStart);
        Assert.Equal(101.7543859663m, row.Past.ProjectedQoh);
        Assert.Equal(49m, row.Weeks[1].PlannedOrdersDue);
        Assert.Equal(60m, row.Weeks[0].PlannedOrdersRelease);
        Assert.Contains(row.Evidence, fact => fact.IsPlannedOrderReleaseEvidence && fact.Quantity == 11m);
    }

    [Fact]
    public void Build_DoesNotDoubleCountEqualFacts_AndEvaluatesSeverityOnlyInForwardWeeks()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(10m, 5m,
            new(1, "DEMAND", new(2026, 9, 1), null, 20m, MrpScheduleCategory.Unclassified),
            new(2, "SUPPLY", new(2026, 9, 15), null, 20m, MrpScheduleCategory.Unclassified),
            new(3, "DEMAND", new(2026, 9, 16), null, 2m, MrpScheduleCategory.Unclassified),
            new(4, "DEMAND", new(2026, 9, 16), null, 2m, MrpScheduleCategory.Unclassified))]));

        Assert.Equal(-10m, row.Past.ProjectedQoh);
        Assert.Equal(6m, row.Weeks[0].ProjectedQoh);
        Assert.Equal(LongTermShortageSeverity.None, row.Severity);
        Assert.Equal(4m, row.Weeks[0].GrossRequirements);
        Assert.Equal(4, row.Evidence.Count);
    }

    [Fact]
    public void Build_SelectedSiteNullSafetyDoesNotFallback()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [new LongTermShortageInput("COMP", null, null, null, null, null, 0m, SafetyStockState.SelectedSiteValueMissing, null, [], [])]));
        Assert.Equal(LongTermShortageSeverity.SafetyStockUnavailable, row.Severity);
    }

    [Fact]
    public void Build_PresentationContextDoesNotAlterRawMrpBalance()
    {
        var facts = new[] { new LongTermMrpFact(1, "DEMAND", new(2026, 9, 15), null, 4m, MrpScheduleCategory.Unclassified) };
        var raw = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(10m, 0m, facts)]));
        var contextual = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(10m, 0m, facts) with
        {
            Presentation = new LongTermShortagePresentationContext("MFG-1", "PO-1", 2, new DateOnly(2026, 9, 16), 999m, true, true)
        }]));

        Assert.Equal(raw.Past, contextual.Past);
        Assert.Equal(raw.Weeks, contextual.Weeks);
        Assert.Equal(raw.Evidence, contextual.Evidence);
        Assert.Equal(6m, contextual.Weeks[0].ProjectedQoh);
        Assert.True(contextual.Presentation!.IsKss);
    }

    private static LongTermShortageInput Input(decimal qoh, decimal safety, params LongTermMrpFact[] facts) => new("COMP", "EA", null, null, null, null, qoh, SafetyStockState.Resolved, safety, ["PARENT"], facts);
}

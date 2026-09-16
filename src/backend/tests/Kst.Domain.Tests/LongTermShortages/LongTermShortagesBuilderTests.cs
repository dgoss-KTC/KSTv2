using Kst.Domain.LongTermShortages;

namespace Kst.Domain.Tests.LongTermShortages;

public sealed class LongTermShortagesBuilderTests
{
    [Fact]
    public void Build_UsesTwentyFourSundayStartWeeks_AndNetsSameWeekSupplyAndDemand()
    {
        var rows = LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(
            openingQoh: 10m,
            safetyStock: 5m,
            demands: [new(new DateOnly(2026, 9, 16), 8m, false)],
            purchaseOrders: [new("100", 1, new DateOnly(2026, 9, 17), 3m, true, null, false)])]);

        var row = Assert.Single(rows);
        Assert.Equal(24, row.Weeks.Count);
        Assert.Equal(new DateOnly(2026, 9, 13), row.Weeks[0].WeekStart);
        Assert.Equal(new DateOnly(2027, 2, 21), row.Weeks[^1].WeekStart);
        Assert.Equal(5m, row.Weeks[0].Balance);
        Assert.Equal(LongTermShortageSeverity.None, row.Weeks[0].Severity);
    }

    [Fact]
    public void Build_RollsPastDueWorkOrderDemandIntoWeekOne_ButExcludesPastForecast()
    {
        var row = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(
            openingQoh: 20m,
            safetyStock: 0m,
            demands: [new(new DateOnly(2026, 9, 1), 4m, false), new(new DateOnly(2026, 9, 1), 8m, true)])]));

        Assert.Equal(4m, row.Weeks[0].WorkOrderDemand);
        Assert.Equal(0m, row.Weeks[0].ForecastDemand);
        Assert.Equal(16m, row.Weeks[0].Balance);
    }

    [Fact]
    public void Build_UsesSafetyStockSeverityAndSelectedSiteMissingState()
    {
        var safetyShort = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(10m, 15m)]));
        var unavailable = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(10m, null, SafetyStockState.SelectedSiteValueMissing)]));

        Assert.Equal(LongTermShortageSeverity.SafetyStockShort, safetyShort.Severity);
        Assert.Equal(1, safetyShort.FirstSafetyStockShortWeek);
        Assert.Equal(LongTermShortageSeverity.SafetyStockUnavailable, unavailable.Severity);
        Assert.Null(unavailable.FirstSafetyStockShortWeek);
    }

    [Fact]
    public void Build_ApprovedIcc00994Timeline_FirstSafetyShortIsWeek16_AndCriticalShortIsWeek17()
    {
        var weeklyDemand = new decimal[] { 18796.99m, 8956.39m, 20462.66m, 6618.55m, 1616.04m, 6027.07m, 12356.89m, 6329.82m, 3665.16m, 14817.04m, 894.24m, 0m, 0m, 0m, 3663.16m, 5802.51m, 15837.59m, 916.29m, 2584.46m, 3584.96m, 5497.74m, 15957.89m, 14249.62m, 18359.90m };
        var weekOne = new DateOnly(2026, 9, 13);
        var demands = weeklyDemand.Select((quantity, index) => new LongTermDemandEvent(weekOne.AddDays(index * 7), quantity, false)).ToList();

        var row = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [Input(120721m, 14838m, demands: demands)]));

        Assert.Equal(LongTermShortageSeverity.SafetyStockShort, row.Weeks[15].Severity);
        Assert.Equal(LongTermShortageSeverity.CriticalShort, row.Weeks[16].Severity);
        Assert.Equal(16, row.FirstSafetyStockShortWeek);
        Assert.Equal(17, row.FirstCriticalShortWeek);
        Assert.All(row.Weeks.Skip(16), week => Assert.Equal(LongTermShortageSeverity.CriticalShort, week.Severity));
    }

    [Theory]
    [InlineData("ICC-01084", 63974, 36568)]
    [InlineData("ICC-01117", 37056, 13761)]
    [InlineData("115989", 0.96, 0)]
    public void Build_ApprovedCleanTimelineFixtures_HaveNoShortage(string part, double opening, double safety)
    {
        var demands = part == "115989" ? new[] { new LongTermDemandEvent(new DateOnly(2026, 11, 29), 0.90m, false) } : Array.Empty<LongTermDemandEvent>();
        var input = Input((decimal)opening, (decimal)safety, demands: demands) with { ComponentPart = part, OtherProgramParentParts = part == "115989" ? ["OTHER-PARENT"] : [] };
        var row = Assert.Single(LongTermShortagesBuilder.Build(new DateOnly(2026, 9, 15), [input]));

        Assert.Equal(LongTermShortageSeverity.None, row.Severity);
        Assert.Null(row.FirstSafetyStockShortWeek);
        Assert.Null(row.FirstCriticalShortWeek);
        if (part == "115989") Assert.Equal(0.06m, row.Weeks[11].Balance);
    }

    private static LongTermShortageInput Input(decimal openingQoh, decimal? safetyStock, SafetyStockState state = SafetyStockState.Resolved, IReadOnlyList<LongTermDemandEvent>? demands = null, IReadOnlyList<LongTermPurchaseOrder>? purchaseOrders = null) =>
        new("COMP", "EA", null, null, false, null, null, null, openingQoh, state, safetyStock, ["PARENT"], [], demands ?? [], purchaseOrders ?? []);
}

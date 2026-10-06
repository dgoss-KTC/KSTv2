using Kst.Domain.Mps;
using Kst.Domain.PlanningWorkbook;

namespace Kst.Domain.Tests.PlanningWorkbook;

/// <summary>Owner-confirmed Stage 14.4 horizon range: max(12, lastMpsWeekIndex + 2), Due Date only.</summary>
public sealed class PlanningHorizonRangeTests
{
    // Wednesday; its business week starts Sunday 2026-09-13 (index 0, Monday label 2026-09-14).
    private static readonly DateOnly Today = new(2026, 9, 16);
    private static readonly DateOnly WeekZeroSunday = new(2026, 9, 13);
    private static readonly IReadOnlyList<MpsResolvedPart> Parts = [new("P-1", null), new("P-2", null)];

    private static MpsSourceRow Row(string part, DateOnly due, decimal qty = 10m, DateOnly? release = null) =>
        new("D", "S1", part, null, due, release ?? due, qty, MpsSupplyType.Supply, $"WO-{part}-{due}", MpsWorkOrderState.Planned);

    private static DateOnly Week(int index, int dayOffset = 0) => WeekZeroSunday.AddDays(7 * index + dayOffset);

    [Fact]
    public void No_Future_Mps_Keeps_The_Twelve_Week_Minimum()
    {
        Assert.Equal(12, PlanningHorizonRange.MinimumWeeks);
        Assert.Equal(12, PlanningHorizonRange.MaximumWeeks(Parts, [], Today));
        // Falldown-only MPS does not extend the range.
        Assert.Equal(12, PlanningHorizonRange.MaximumWeeks(Parts, [Row("P-1", Today.AddDays(-30))], Today));
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(9, 12)]
    [InlineData(10, 12)]
    [InlineData(11, 13)]
    [InlineData(20, 22)]
    [InlineData(80, 82)]
    public void Maximum_Runs_Through_One_Week_After_The_Last_Nonzero_Mps_Week(int lastIndex, int expected)
    {
        var rows = new[] { Row("P-1", Week(0, 1)), Row("P-2", Week(lastIndex, 6)) };
        Assert.Equal(expected, PlanningHorizonRange.MaximumWeeks(Parts, rows, Today));
    }

    [Fact]
    public void Uses_Due_Date_Not_Release_Date_And_Sunday_To_Saturday_Weeks()
    {
        // Due Saturday of week 19 (index 19), released earlier: index 19 -> 21.
        Assert.Equal(21, PlanningHorizonRange.MaximumWeeks(Parts, [Row("P-1", Week(19, 6), release: Week(2))], Today));
        // Due Sunday of week 20 (index 20): 22.
        Assert.Equal(22, PlanningHorizonRange.MaximumWeeks(Parts, [Row("P-1", Week(20), release: Week(30))], Today));
    }

    [Fact]
    public void Zero_Net_Weeks_And_Parts_Outside_Resolved_Scope_Do_Not_Extend_The_Range()
    {
        var rows = new[]
        {
            Row("P-1", Week(15)),
            Row("P-1", Week(40, 1), 5m), Row("P-1", Week(40, 2), -5m), // nets to zero in week 40
            Row("OTHER", Week(60)),
            Row("p-2", Week(18, 3), 0.5m), // case-insensitive part match
        };
        Assert.Equal(20, PlanningHorizonRange.MaximumWeeks(Parts, rows, Today));
    }

    [Fact]
    public void Range_Shrinks_As_The_Business_Week_Advances()
    {
        var rows = new[] { Row("P-1", Week(20, 2)) };
        Assert.Equal(22, PlanningHorizonRange.MaximumWeeks(Parts, rows, Today));
        Assert.Equal(21, PlanningHorizonRange.MaximumWeeks(Parts, rows, Today.AddDays(7)));
        Assert.Equal(12, PlanningHorizonRange.MaximumWeeks(Parts, rows, Today.AddDays(7 * 21)));
    }
}

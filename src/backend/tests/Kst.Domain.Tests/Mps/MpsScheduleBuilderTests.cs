using Kst.Domain.Mps;

namespace Kst.Domain.Tests.Mps;

public sealed class MpsScheduleBuilderTests
{
    // Wednesday, so the current business week starts Sunday 2026-08-02 (Monday label 2026-08-03).
    private static readonly DateOnly Today = new(2026, 8, 5);
    private static readonly DateOnly CurrentWeekStart = new(2026, 8, 2);

    private static MpsSourceRow Row(
        string parentPart = "ABC100",
        DateOnly? dueDate = null,
        DateOnly? releaseDate = null,
        decimal quantity = 10m,
        MpsWorkOrderState state = MpsWorkOrderState.Released,
        string workOrderId = "WO1",
        MpsSupplyType supplyType = MpsSupplyType.Supply) =>
        new(
            Domain: "KTC",
            Site: "SW",
            ParentPart: parentPart,
            Description: "Widget",
            DueDate: dueDate ?? CurrentWeekStart,
            ReleaseDate: releaseDate,
            Quantity: quantity,
            SupplyType: supplyType,
            WorkOrderId: workOrderId,
            WorkOrderState: state);

    private static MpsBucket Falldown(IReadOnlyList<MpsPartSchedule> schedules, string part) =>
        schedules.Single(s => s.ParentPart == part).Buckets.Single(b => b.Kind == MpsBucketKind.Falldown);

    private static MpsBucket Week(IReadOnlyList<MpsPartSchedule> schedules, string part, int index) =>
        schedules.Single(s => s.ParentPart == part).Buckets.Where(b => b.Kind == MpsBucketKind.Weekly).ElementAt(index);

    [Fact]
    public void Resolved_Part_With_No_Source_Rows_Remains_Present_With_Zero_Buckets()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", "Widget") };
        var result = MpsScheduleBuilder.Build(resolvedParts, [], MpsDateBasis.DueDate, horizonWeeks: 4, Today);

        var schedule = Assert.Single(result);
        Assert.Equal("ABC100", schedule.ParentPart);
        Assert.Equal(5, schedule.Buckets.Count); // Falldown + 4 weeks
        Assert.All(schedule.Buckets, b =>
        {
            Assert.Equal(0m, b.Quantity);
            Assert.Equal(MpsExecutionStatus.None, b.ExecutionStatus);
            Assert.Empty(b.WorkOrders);
        });
    }

    [Fact]
    public void Old_Unfinished_WorkOrder_Appears_In_Falldown_Not_In_Weekly_Buckets()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", "Widget") };
        var rows = new List<MpsSourceRow> { Row(dueDate: new DateOnly(2015, 1, 1)) };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 4, Today);

        Assert.Equal(10m, Falldown(result, "ABC100").Quantity);
        Assert.All(Enumerable.Range(0, 4), i => Assert.Equal(0m, Week(result, "ABC100", i).Quantity));
    }

    [Fact]
    public void DueDate_In_Current_Week_Lands_In_First_Weekly_Bucket_Not_Falldown()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow> { Row(dueDate: CurrentWeekStart) };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 2, Today);

        Assert.Equal(0m, Falldown(result, "ABC100").Quantity);
        Assert.Equal(10m, Week(result, "ABC100", 0).Quantity);
        Assert.Equal(new DateOnly(2026, 8, 3), Week(result, "ABC100", 0).WeekLabel);
    }

    [Fact]
    public void DueDate_On_Last_Day_Of_Week_Saturday_Still_Lands_In_Same_Week()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var saturday = CurrentWeekStart.AddDays(6);
        var rows = new List<MpsSourceRow> { Row(dueDate: saturday) };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 2, Today);

        Assert.Equal(10m, Week(result, "ABC100", 0).Quantity);
    }

    [Fact]
    public void DueDate_On_Next_Sunday_Lands_In_Second_Week_Not_First()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var nextSunday = CurrentWeekStart.AddDays(7);
        var rows = new List<MpsSourceRow> { Row(dueDate: nextSunday) };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 2, Today);

        Assert.Equal(0m, Week(result, "ABC100", 0).Quantity);
        Assert.Equal(10m, Week(result, "ABC100", 1).Quantity);
    }

    [Fact]
    public void Same_Source_Rows_Rebucket_Differently_By_DateBasis()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow>
        {
            Row(dueDate: CurrentWeekStart, releaseDate: CurrentWeekStart.AddDays(7), quantity: 25m)
        };

        var dueResult = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 2, Today);
        var releaseResult = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.ReleaseDate, horizonWeeks: 2, Today);

        Assert.Equal(25m, Week(dueResult, "ABC100", 0).Quantity);
        Assert.Equal(0m, Week(dueResult, "ABC100", 1).Quantity);

        Assert.Equal(0m, Week(releaseResult, "ABC100", 0).Quantity);
        Assert.Equal(25m, Week(releaseResult, "ABC100", 1).Quantity);
    }

    [Fact]
    public void Falldown_Remains_DueDate_Based_Even_In_ReleaseDate_Mode()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        // Old due date (falldown by due date) but a future release date.
        var rows = new List<MpsSourceRow>
        {
            Row(dueDate: new DateOnly(2015, 1, 1), releaseDate: CurrentWeekStart, quantity: 15m)
        };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.ReleaseDate, horizonWeeks: 2, Today);

        Assert.Equal(15m, Falldown(result, "ABC100").Quantity);
        Assert.Equal(15m, Week(result, "ABC100", 0).Quantity);
    }

    [Fact]
    public void Null_ReleaseDate_Row_Is_Not_Placed_In_Any_Weekly_Bucket_Under_ReleaseDate_Mode()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow> { Row(dueDate: CurrentWeekStart, releaseDate: null) };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.ReleaseDate, horizonWeeks: 2, Today);

        Assert.All(Enumerable.Range(0, 2), i => Assert.Equal(0m, Week(result, "ABC100", i).Quantity));
    }

    [Fact]
    public void Multiple_Rows_In_Same_Bucket_Sum_Quantity()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow>
        {
            Row(dueDate: CurrentWeekStart, quantity: 5m, workOrderId: "WO1"),
            Row(dueDate: CurrentWeekStart, quantity: 7m, workOrderId: "WO2"),
        };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);

        Assert.Equal(12m, Week(result, "ABC100", 0).Quantity);
    }

    [Theory]
    [InlineData(MpsWorkOrderState.Allocating, MpsExecutionStatus.Allocating)]
    [InlineData(MpsWorkOrderState.Frozen, MpsExecutionStatus.Frozen)]
    [InlineData(MpsWorkOrderState.Released, MpsExecutionStatus.Released)]
    public void Single_AFR_State_Produces_Matching_Execution_Status(MpsWorkOrderState state, MpsExecutionStatus expected)
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow> { Row(dueDate: CurrentWeekStart, state: state) };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);

        Assert.Equal(expected, Week(result, "ABC100", 0).ExecutionStatus);
    }

    [Fact]
    public void Repeated_Same_AFR_State_Does_Not_Produce_Mixed()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow>
        {
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Released, workOrderId: "WO1"),
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Released, workOrderId: "WO2"),
        };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);

        Assert.Equal(MpsExecutionStatus.Released, Week(result, "ABC100", 0).ExecutionStatus);
    }

    [Fact]
    public void Two_Distinct_AFR_States_Produce_Mixed()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow>
        {
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Allocating, workOrderId: "WO1"),
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Frozen, workOrderId: "WO2"),
        };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);

        Assert.Equal(MpsExecutionStatus.Mixed, Week(result, "ABC100", 0).ExecutionStatus);
    }

    [Theory]
    [InlineData(MpsWorkOrderState.Planned, false, true, false)]
    [InlineData(MpsWorkOrderState.ExplicitlyScheduled, false, false, true)]
    public void Planned_And_Scheduled_Flags_Do_Not_Create_Mixed_By_Themselves(
        MpsWorkOrderState state, bool unused, bool expectPlanned, bool expectScheduled)
    {
        _ = unused;
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow> { Row(dueDate: CurrentWeekStart, state: state) };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);
        var bucket = Week(result, "ABC100", 0);

        Assert.Equal(MpsExecutionStatus.None, bucket.ExecutionStatus);
        Assert.Equal(expectPlanned, bucket.ContainsPlannedWork);
        Assert.Equal(expectScheduled, bucket.ContainsExplicitlyScheduledWork);
    }

    [Fact]
    public void Planned_Plus_ExplicitlyScheduled_Sets_Both_Flags_Without_Mixed()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow>
        {
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Planned, workOrderId: "WO1"),
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.ExplicitlyScheduled, workOrderId: "WO2"),
        };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);
        var bucket = Week(result, "ABC100", 0);

        Assert.Equal(MpsExecutionStatus.None, bucket.ExecutionStatus);
        Assert.True(bucket.ContainsPlannedWork);
        Assert.True(bucket.ContainsExplicitlyScheduledWork);
    }

    [Fact]
    public void Released_Plus_Planned_Produces_Released_With_Planned_Flag()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow>
        {
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Released, workOrderId: "WO1"),
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Planned, workOrderId: "WO2"),
        };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);
        var bucket = Week(result, "ABC100", 0);

        Assert.Equal(MpsExecutionStatus.Released, bucket.ExecutionStatus);
        Assert.True(bucket.ContainsPlannedWork);
        Assert.False(bucket.ContainsExplicitlyScheduledWork);
    }

    [Fact]
    public void AFR_Combined_With_Planned_And_Scheduled_Produces_Mixed_With_Both_Flags()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow>
        {
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Allocating, workOrderId: "WO1"),
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Frozen, workOrderId: "WO2"),
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Planned, workOrderId: "WO3"),
            Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.ExplicitlyScheduled, workOrderId: "WO4"),
        };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);
        var bucket = Week(result, "ABC100", 0);

        Assert.Equal(MpsExecutionStatus.Mixed, bucket.ExecutionStatus);
        Assert.True(bucket.ContainsPlannedWork);
        Assert.True(bucket.ContainsExplicitlyScheduledWork);
        Assert.Equal(4, bucket.WorkOrders.Count);
    }

    [Fact]
    public void Unknown_State_Contributes_Quantity_But_No_Status_Signal()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var rows = new List<MpsSourceRow> { Row(dueDate: CurrentWeekStart, state: MpsWorkOrderState.Unknown, quantity: 9m) };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);
        var bucket = Week(result, "ABC100", 0);

        Assert.Equal(9m, bucket.Quantity);
        Assert.Equal(MpsExecutionStatus.None, bucket.ExecutionStatus);
        Assert.False(bucket.ContainsPlannedWork);
        Assert.False(bucket.ContainsExplicitlyScheduledWork);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(24)]
    [InlineData(52)]
    [InlineData(72)]
    public void Horizon_Produces_Falldown_Plus_Expected_Weekly_Bucket_Count(int horizonWeeks)
    {
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };
        var result = MpsScheduleBuilder.Build(resolvedParts, [], MpsDateBasis.DueDate, horizonWeeks, Today);

        Assert.Equal(horizonWeeks + 1, result[0].Buckets.Count);
        var weeklyLabels = result[0].Buckets
            .Where(b => b.Kind == MpsBucketKind.Weekly)
            .Select(b => b.WeekLabel!.Value)
            .ToList();

        for (var i = 1; i < weeklyLabels.Count; i++)
        {
            Assert.Equal(7, weeklyLabels[i].DayNumber - weeklyLabels[i - 1].DayNumber);
        }
    }

    [Fact]
    public void Horizon_Crossing_Year_Boundary_Keeps_Sequential_Weekly_Labels()
    {
        var decemberToday = new DateOnly(2026, 12, 20);
        var resolvedParts = new List<MpsResolvedPart> { new("ABC100", null) };

        var result = MpsScheduleBuilder.Build(resolvedParts, [], MpsDateBasis.DueDate, horizonWeeks: 6, decemberToday);

        var labels = result[0].Buckets.Where(b => b.Kind == MpsBucketKind.Weekly).Select(b => b.WeekLabel!.Value).ToList();
        Assert.Contains(labels, l => l.Year == 2027);
        for (var i = 1; i < labels.Count; i++)
        {
            Assert.Equal(7, labels[i].DayNumber - labels[i - 1].DayNumber);
        }
    }

    [Fact]
    public void Multiple_Resolved_Parts_Preserve_Input_Order_And_Independent_Buckets()
    {
        var resolvedParts = new List<MpsResolvedPart> { new("B100", null), new("A100", null) };
        var rows = new List<MpsSourceRow>
        {
            Row(parentPart: "A100", dueDate: CurrentWeekStart, quantity: 3m),
        };

        var result = MpsScheduleBuilder.Build(resolvedParts, rows, MpsDateBasis.DueDate, horizonWeeks: 1, Today);

        Assert.Equal(["B100", "A100"], result.Select(r => r.ParentPart));
        Assert.Equal(0m, Week(result, "B100", 0).Quantity);
        Assert.Equal(3m, Week(result, "A100", 0).Quantity);
    }

    [Fact]
    public void Rejects_NonPositive_Horizon()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MpsScheduleBuilder.Build([], [], MpsDateBasis.DueDate, horizonWeeks: 0, Today));
    }
}

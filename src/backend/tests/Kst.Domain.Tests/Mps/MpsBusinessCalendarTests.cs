using Kst.Domain.Mps;

namespace Kst.Domain.Tests.Mps;

public sealed class MpsBusinessCalendarTests
{
    [Fact]
    public void GetBusinessWeekStart_Sunday_Returns_Same_Date()
    {
        var sunday = new DateOnly(2026, 8, 2); // Sunday
        Assert.Equal(sunday, MpsBusinessCalendar.GetBusinessWeekStart(sunday));
    }

    [Fact]
    public void GetBusinessWeekStart_Saturday_Returns_Preceding_Sunday()
    {
        var saturday = new DateOnly(2026, 8, 8); // Saturday
        var expectedSunday = new DateOnly(2026, 8, 2);
        Assert.Equal(expectedSunday, MpsBusinessCalendar.GetBusinessWeekStart(saturday));
    }

    [Fact]
    public void GetBusinessWeekStart_MidWeek_Returns_Same_Sunday_As_Saturday()
    {
        var wednesday = new DateOnly(2026, 8, 5);
        var saturday = new DateOnly(2026, 8, 8);
        Assert.Equal(
            MpsBusinessCalendar.GetBusinessWeekStart(saturday),
            MpsBusinessCalendar.GetBusinessWeekStart(wednesday));
    }

    [Fact]
    public void GetBusinessWeekStart_Crosses_Year_Boundary_Correctly()
    {
        // Jan 1 2027 is a Friday; its business week starts Sunday Dec 27 2026.
        var newYearsDay = new DateOnly(2027, 1, 1);
        var expectedSunday = new DateOnly(2026, 12, 27);
        Assert.Equal(expectedSunday, MpsBusinessCalendar.GetBusinessWeekStart(newYearsDay));
    }

    [Fact]
    public void GetWeekLabel_Returns_Monday_After_WeekStartSunday()
    {
        var sunday = new DateOnly(2026, 8, 2);
        var expectedMonday = new DateOnly(2026, 8, 3);
        Assert.Equal(expectedMonday, MpsBusinessCalendar.GetWeekLabel(sunday));
    }

    [Fact]
    public void IsFalldown_DueDate_Before_Current_Week_Is_True()
    {
        var today = new DateOnly(2026, 8, 5); // Wednesday, week starts Aug 2
        var oldDueDate = new DateOnly(2026, 8, 1); // prior Saturday
        Assert.True(MpsBusinessCalendar.IsFalldown(oldDueDate, today));
    }

    [Fact]
    public void IsFalldown_DueDate_Within_Current_Week_Is_False()
    {
        var today = new DateOnly(2026, 8, 5); // Wednesday
        var dueDateThisWeek = new DateOnly(2026, 8, 2); // Sunday of the same week
        Assert.False(MpsBusinessCalendar.IsFalldown(dueDateThisWeek, today));
    }

    [Fact]
    public void IsFalldown_Very_Old_DueDate_Remains_True_Regardless_Of_Age()
    {
        var today = new DateOnly(2026, 8, 5);
        var veryOldDueDate = new DateOnly(2015, 1, 1);
        Assert.True(MpsBusinessCalendar.IsFalldown(veryOldDueDate, today));
    }
}

namespace Kst.Domain.Mps;

/// <summary>
/// Pure business-week calculations for the MPS grid. Business weeks run Sunday through Saturday;
/// Monday is the visible week label/anchor. Contains no I/O and no QAD/frontend concepts.
/// </summary>
public static class MpsBusinessCalendar
{
    /// <summary>Returns the Sunday that begins the business week containing <paramref name="date"/>.</summary>
    public static DateOnly GetBusinessWeekStart(DateOnly date) =>
        date.AddDays(-(int)date.DayOfWeek);

    /// <summary>Returns the Monday visible-label date for the business week starting on <paramref name="weekStartSunday"/>.</summary>
    public static DateOnly GetWeekLabel(DateOnly weekStartSunday) =>
        weekStartSunday.AddDays(1);

    /// <summary>
    /// A row is Falldown when its Due Date falls before the current business week, regardless of
    /// the visible Due/Release date-basis mode.
    /// </summary>
    public static bool IsFalldown(DateOnly dueDate, DateOnly today) =>
        dueDate < GetBusinessWeekStart(today);
}

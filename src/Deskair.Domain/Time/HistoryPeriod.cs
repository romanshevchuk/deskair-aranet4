namespace Deskair.Domain.Time;

/// <summary>
/// One period of the History screen: a range and how many periods back from the latest (D-0019, behaviour-rules.md "History periods").
/// <see cref="Start"/> is inclusive and <see cref="End"/> exclusive. The latest period ends now; an earlier one ends where the next one starts.
/// </summary>
public sealed record HistoryPeriod(Period Range, int Offset, DateTimeOffset Start, DateTimeOffset End)
{
    private static readonly TimeSpan day = TimeSpan.FromHours(24);

    /// <summary>The latest Day is the last 24 hours ending now; every earlier Day is a calendar day.</summary>
    public bool IsRolling => Range == Period.Day && Offset == 0;

    /// <summary>
    /// The period <paramref name="offset"/> steps back from the latest at <paramref name="now"/>. Day: the last 24 hours, then calendar days.
    /// Week and Month: 7 or 30 local days ending today, then the 7 or 30 days before. Year: 12 calendar months ending with the current month.
    /// </summary>
    public static HistoryPeriod For(Period range, int offset, DateTimeOffset now, LocalCalendar calendar)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentNullException.ThrowIfNull(calendar);

        var today = calendar.LocalDateOf(now);

        switch (range)
        {
            case Period.Day when offset == 0:
                return new HistoryPeriod(range, 0, now - day, now);
            case Period.Day:
                var date = today.AddDays(-offset);
                return new HistoryPeriod(range, offset, calendar.StartOfLocalDay(date), calendar.StartOfLocalDay(date.AddDays(1)));
            case Period.Week:
                return Days(range, offset, 7, today, now, calendar);
            case Period.Month:
                return Days(range, offset, 30, today, now, calendar);
            default:
                var first = new DateOnly(today.Year, today.Month, 1).AddMonths(-11 - (12 * offset));
                var end = offset == 0 ? now : calendar.StartOfLocalDay(first.AddMonths(12));
                return new HistoryPeriod(range, offset, calendar.StartOfLocalDay(first), end);
        }
    }

    /// <summary>
    /// How far ‹ may go: to the period before the one that holds the first reading, which then shows the empty state. Zero when nothing is stored;
    /// Year stays at the latest period in v1.
    /// </summary>
    public static int MaxOffset(Period range, DateTimeOffset now, DateTimeOffset? firstReading, LocalCalendar calendar)
    {
        if (firstReading is not { } first || range == Period.Year)
        {
            return 0;
        }

        // About ten years of days at most, so a wrong clock or a reading from 1970 cannot make this loop for long.
        const int limit = 4000;
        var offset = 0;

        while (offset < limit && For(range, offset, now, calendar).Start > first)
        {
            offset++;
        }

        return offset + 1;
    }

    /// <summary>
    /// The period to compare with (behaviour-rules.md "Aggregation" rule 3): for the last 24 hours, the 24 hours before; otherwise the period ‹ would show.
    /// </summary>
    public HistoryPeriod Before(DateTimeOffset now, LocalCalendar calendar)
        => IsRolling ? this with { Start = Start - day, End = Start } : For(Range, Offset + 1, now, calendar);

    /// <summary>The first and last local date the period covers (the last 24 hours cover two; the latest Week ends today).</summary>
    public (DateOnly First, DateOnly Last) Dates(LocalCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        return (calendar.LocalDateOf(Start), calendar.LocalDateOf(End.AddTicks(-1)));
    }

    private static HistoryPeriod Days(Period range, int offset, int length, DateOnly today, DateTimeOffset now, LocalCalendar calendar)
    {
        var last = today.AddDays(-length * offset);
        var start = calendar.StartOfLocalDay(last.AddDays(1 - length));
        var end = offset == 0 ? now : calendar.StartOfLocalDay(last.AddDays(1));

        return new HistoryPeriod(range, offset, start, end);
    }
}

using Deskair.Domain.Time;

namespace Deskair.Domain.Analytics.HoursFilter;

/// <summary>
/// The hours that count as "night" in local time: the Night filter in History and the sleep card use it (Settings → Sleep). The window may cross midnight (22:00 to 07:00)
/// or not (02:00 to 10:00). Start is included, End is not. A window whose start equals its end is empty.
/// </summary>
public sealed record NightWindow(TimeOnly Start, TimeOnly End)
{
    public static NightWindow Default { get; } = new(new TimeOnly(22, 0), new TimeOnly(7, 0));

    /// <summary>True when the window runs past midnight, so it starts on one day and ends on the next.</summary>
    public bool CrossesMidnight => Start > End;

    public bool IsEmpty => Start == End;

    /// <summary>The length on a normal day. A night with a clock change is an hour longer or shorter; use the real instants for that.</summary>
    public TimeSpan Length => IsEmpty ? TimeSpan.Zero : CrossesMidnight ? TimeSpan.FromHours(24) - (Start - End) : End - Start;

    /// <summary>
    /// The most recent night that has ended by <paramref name="now"/>, as instants: the night in progress is not "last night". Works with clock changes,
    /// so a night can be an hour longer or shorter than <see cref="Length"/>.
    /// </summary>
    public (DateTimeOffset Start, DateTimeOffset End) LastFinished(DateTimeOffset now, LocalCalendar calendar)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("An empty night window has no nights.");
        }

        var endDate = calendar.LocalDateOf(now);

        if (now < calendar.InstantAt(endDate, End))
        {
            endDate = endDate.AddDays(-1);
        }

        var startDate = CrossesMidnight ? endDate.AddDays(-1) : endDate;

        return (calendar.InstantAt(startDate, Start), calendar.InstantAt(endDate, End));
    }

    public bool Includes(TimeSpan timeOfDay)
    {
        if (IsEmpty)
        {
            return false;
        }

        var start = Start.ToTimeSpan();
        var end = End.ToTimeSpan();

        return CrossesMidnight ? timeOfDay >= start || timeOfDay < end : timeOfDay >= start && timeOfDay < end;
    }

    /// <summary>How much of the range from <paramref name="start"/> up to <paramref name="end"/> is night. A bucket "expects" readings for this much time when only the night is counted.</summary>
    public TimeSpan TimeWithin(DateTimeOffset start, DateTimeOffset end, LocalCalendar calendar)
    {
        if (IsEmpty || end <= start)
        {
            return TimeSpan.Zero;
        }

        var total = TimeSpan.Zero;
        var last = calendar.LocalDateOf(end - TimeSpan.FromTicks(1));

        // Start one day early: the night that began the evening before may still be going on at the start of the range.
        for (var date = calendar.LocalDateOf(start).AddDays(-1); date <= last; date = date.AddDays(1))
        {
            var from = Latest(calendar.InstantAt(date, Start), start);
            var to = Earliest(calendar.InstantAt(CrossesMidnight ? date.AddDays(1) : date, End), end);

            if (to > from)
            {
                total += to - from;
            }
        }

        return total;
    }

    private static DateTimeOffset Latest(DateTimeOffset first, DateTimeOffset second) => first > second ? first : second;

    private static DateTimeOffset Earliest(DateTimeOffset first, DateTimeOffset second) => first < second ? first : second;
}

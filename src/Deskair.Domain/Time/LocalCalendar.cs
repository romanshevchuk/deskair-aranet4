namespace Deskair.Domain.Time;

/// <summary>Calendar maths in the user's time zone. Days are 23 or 25 hours when daylight saving changes.</summary>
public sealed class LocalCalendar
{
    public LocalCalendar(TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        Zone = zone;
    }

    public TimeZoneInfo Zone { get; }

    public DateOnly LocalDateOf(DateTimeOffset instant)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);

    public DateTimeOffset StartOfLocalDay(DateTimeOffset instant)
        => StartOfLocalDay(LocalDateOf(instant));

    public DateTimeOffset StartOfLocalDay(DateOnly date) => InstantAt(date, TimeOnly.MinValue);

    /// <summary>
    /// The instant of a local date and time. A time that does not exist (the clock jumped over it) moves forward to the first time that does;
    /// a time that happens twice is the first one.
    /// </summary>
    public DateTimeOffset InstantAt(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);

        while (Zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        var offset = Zone.IsAmbiguousTime(local)
            ? Zone.GetAmbiguousTimeOffsets(local).Max()
            : Zone.GetUtcOffset(local);

        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    public TimeSpan DayLength(DateOnly date)
        => StartOfLocalDay(date.AddDays(1)) - StartOfLocalDay(date);

    public static int ExpectedSlots(DateTimeOffset start, DateTimeOffset end, TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        var elapsed = end - start;
        return elapsed <= TimeSpan.Zero ? 0 : (int)(elapsed.Ticks / interval.Ticks);
    }
}

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

    public DateTimeOffset StartOfLocalDay(DateOnly date)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue);

        while (Zone.IsInvalidTime(midnight))
        {
            midnight = midnight.AddMinutes(1);
        }

        var offset = Zone.IsAmbiguousTime(midnight)
            ? Zone.GetAmbiguousTimeOffsets(midnight).Max()
            : Zone.GetUtcOffset(midnight);

        return new DateTimeOffset(midnight, offset).ToUniversalTime();
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

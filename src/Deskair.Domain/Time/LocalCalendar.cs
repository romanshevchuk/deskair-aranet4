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

    // TODO(TIME-01a) [S, sonnet]
    // Spec: docs/product/behaviour-rules.md "Aggregation" rule 2 and "Semantics".
    // Input : a UTC instant.
    // Output: the UTC instant at which that instant's local calendar day starts.
    // Rules : use the zone's offset at that day's local midnight (not at the instant).
    // Edge  : if local midnight does not exist, the day starts at the first valid instant.
    // Done  : unskip tests Domain/Time/LocalCalendarTests.StartOfLocalDay_* and make them pass.
    public DateTimeOffset StartOfLocalDay(DateTimeOffset instant)
        => throw new NotImplementedException("TODO(TIME-01a)");

    // TODO(TIME-01b) [S, sonnet]
    // Spec: docs/product/behaviour-rules.md "Semantics" (a day is 23 or 25 hours at a daylight-saving change).
    // Input : a local calendar date.
    // Output: the real length of that day.
    // Rules : next local midnight in UTC minus this local midnight in UTC.
    // Edge  : spring-forward day = 23 h, fall-back day = 25 h, normal day = 24 h.
    // Done  : unskip tests Domain/Time/LocalCalendarTests.DayLength_* and make them pass.
    public TimeSpan DayLength(DateOnly date)
        => throw new NotImplementedException("TODO(TIME-01b)");

    // TODO(TIME-01c) [S, haiku]
    // Spec: docs/engineering/data-model.md "Interval changes"; docs/product/behaviour-rules.md "Aggregation" rule 5.
    // Input : range start (inclusive), range end (exclusive), sensor interval.
    // Output: the number of expected slots in the range.
    // Rules : elapsed time divided by interval, rounded down; uses real elapsed time so DST days differ.
    // Edge  : empty or reversed range returns 0; interval of zero or less throws ArgumentOutOfRangeException.
    // Done  : unskip tests Domain/Time/LocalCalendarTests.ExpectedSlots_* and make them pass.
    public static int ExpectedSlots(DateTimeOffset start, DateTimeOffset end, TimeSpan interval)
        => throw new NotImplementedException("TODO(TIME-01c)");
}

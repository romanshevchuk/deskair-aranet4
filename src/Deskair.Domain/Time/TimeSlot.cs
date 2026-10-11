namespace Deskair.Domain.Time;

/// <summary>One expected reading position on the sensor's interval grid. Half-open: Start inclusive, End exclusive.</summary>
public readonly record struct TimeSlot(DateTimeOffset Start, TimeSpan Interval)
{
    public DateTimeOffset End => Start + Interval;

    public bool Contains(DateTimeOffset timestamp) => timestamp >= Start && timestamp < End;
}

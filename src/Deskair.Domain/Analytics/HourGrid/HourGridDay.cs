using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Zones;

namespace Deskair.Domain.Analytics.HourGrid;

/// <summary>
/// One row of the History grid (D-0018): a local day with the mean of each local hour (null without readings), the day's mean, and how its CO₂ readings
/// spread over the zones (null for other metrics). <see cref="HoursShown"/> is 24, or for today the hours up to now: later hours are not drawn.
/// </summary>
public sealed record HourGridDay(DateOnly Date, IReadOnlyList<double?> Hours, int HoursShown, double? Average, ZoneShare? Share)
{
    /// <summary>How the readings spread below, inside and above the comfortable range (Temperature and Humidity); null for other metrics or without readings.</summary>
    public ComfortShare? Comfort { get; init; }

    /// <summary>How many of the hours have readings.</summary>
    public int HoursWithReadings => Hours.Count(hour => hour is not null);

    /// <summary>False for today: its later hours have not happened yet, so its mean is not comparable with a whole day (no best or worst tag).</summary>
    public bool IsComplete => HoursShown >= 24;

    public bool IsEmpty => HoursWithReadings == 0;
}

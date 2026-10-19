using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Zones;

namespace Deskair.Domain.Analytics.HourGrid;

/// <summary>
/// One row of the Year grid (D-0018): a local month with the mean of each day (null when the day has readings in too few hours), the month's mean, and how its CO₂ readings
/// spread over the zones (null for other metrics). <see cref="Days"/> always has 31 entries; the ones after the month's last day, or after today, are not drawn
/// (<see cref="DaysShown"/>). <see cref="HoursWithReadings"/> counts the distinct local hours that have a reading, for the best and worst month.
/// </summary>
public sealed record HourGridMonth(DateOnly Month, IReadOnlyList<double?> Days, int DaysShown, double? Average, ZoneShare? Share, int HoursWithReadings)
{
    /// <summary>How the readings spread below, inside and above the comfortable range (Temperature and Humidity); null for other metrics or without readings.</summary>
    public ComfortShare? Comfort { get; init; }

    /// <summary>False for the month in progress: its later days have not happened yet, so it is not comparable with a whole month (no best or worst tag).</summary>
    public bool IsComplete => DaysShown >= DateTime.DaysInMonth(Month.Year, Month.Month);

    public bool IsEmpty => HoursWithReadings == 0;
}

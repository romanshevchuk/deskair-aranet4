using Deskair.Domain.Analytics.Sleep;

namespace Deskair.Domain.Analytics.Pressure;

/// <summary>The biggest change in pressure over 24 hours (behaviour-rules.md "Aggregation" rule 12): pressure follows the weather, so the shape over a day matters more than any hour.</summary>
public static class PressureChangeCalculator
{
    /// <summary>The length of the comparison.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromHours(24);

    /// <summary>How far from exactly 24 hours the later value may be: a day with a clock change is 23 or 25 hours long, and a daily mean sits at noon on both days. Never more than half the spacing of the values, so hourly values are matched exactly.</summary>
    public static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(90);

    /// <summary>
    /// The pair of values about 24 hours apart whose difference is the largest, up or down (the earliest on a tie). Each value is compared with the one nearest to 24 hours after it.
    /// Null when no two values are that far apart.
    /// </summary>
    public static PressureChange? Biggest(IReadOnlyList<TimedValue> series)
    {
        ArgumentNullException.ThrowIfNull(series);

        var ordered = series.Where(value => !double.IsNaN(value.Value)).OrderBy(value => value.Time).ToList();
        PressureChange? biggest = null;
        var tolerance = ToleranceFor(ordered);

        foreach (var from in ordered)
        {
            var target = from.Time + Window;
            var to = ordered
                .Where(value => Math.Abs((value.Time - target).Ticks) <= tolerance.Ticks)
                .OrderBy(value => Math.Abs((value.Time - target).Ticks))
                .Cast<TimedValue?>()
                .FirstOrDefault();

            if (to is not { } later)
            {
                continue;
            }

            var change = new PressureChange(from.Time, later.Time, from.Value, later.Value);

            if (biggest is null || Math.Abs(change.Delta) > Math.Abs(biggest.Delta))
            {
                biggest = change;
            }
        }

        return biggest;
    }

    private static TimeSpan ToleranceFor(List<TimedValue> ordered)
    {
        var spacing = ordered.Zip(ordered.Skip(1), (first, second) => second.Time - first.Time).Where(gap => gap > TimeSpan.Zero).DefaultIfEmpty(Window).Min();

        return spacing / 2 < Tolerance ? spacing / 2 : Tolerance;
    }
}

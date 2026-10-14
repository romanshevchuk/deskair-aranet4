using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;

namespace Deskair.Domain.Analytics.HourGrid;

/// <summary>The rows of the History grid for Week and Month (D-0018, behaviour-rules.md "Aggregation" rules 4 and 5).</summary>
public static class HourGridCalculator
{
    /// <summary>A day needs readings in at least this many hours to be the best or the worst one.</summary>
    public const int MinimumHoursForTag = 12;

    /// <summary>At least this many days must qualify before one is called best and one worst.</summary>
    public const int MinimumDaysForTags = 3;

    /// <summary>
    /// One row per local day from <paramref name="start"/> to <paramref name="end"/> (exclusive). Each hour is the mean of the readings in that local
    /// hour; nothing is interpolated. On a 25-hour day the repeated hour joins the first; on a 23-hour day the skipped hour stays empty.
    /// </summary>
    public static IReadOnlyList<HourGridDay> Build(IEnumerable<Reading> readings, Metric metric, DateTimeOffset start, DateTimeOffset end, LocalCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentNullException.ThrowIfNull(calendar);

        if (end <= start)
        {
            return [];
        }

        var byDay = readings
            .Where(reading => reading.Timestamp >= start && reading.Timestamp < end && !double.IsNaN(reading.ValueOf(metric)))
            .GroupBy(reading => calendar.LocalDateOf(reading.Timestamp))
            .ToDictionary(group => group.Key, group => group.ToList());

        var first = calendar.LocalDateOf(start);
        var last = calendar.LocalDateOf(end.AddTicks(-1));
        var days = new List<HourGridDay>();

        for (var date = first; date <= last; date = date.AddDays(1))
        {
            var inDay = byDay.GetValueOrDefault(date) ?? [];
            days.Add(new HourGridDay(
                date,
                HourlyMeans(inDay, metric, calendar),
                HoursShown(date, end, calendar),
                inDay.Count == 0 ? null : inDay.Average(reading => reading.ValueOf(metric)),
                metric == Metric.Co2 ? ZoneShareCalculator.Calculate(inDay) : null));
        }

        return days;
    }

    /// <summary>
    /// The best (lowest mean) and the worst (highest mean) day among those with readings in at least <see cref="MinimumHoursForTag"/> hours,
    /// when at least <see cref="MinimumDaysForTags"/> qualify. Lower is better for CO₂, the only metric the grid tags.
    /// </summary>
    public static (DateOnly? Best, DateOnly? Worst) Extremes(IReadOnlyList<HourGridDay> days)
    {
        ArgumentNullException.ThrowIfNull(days);

        var eligible = days.Where(day => day.HoursWithReadings >= MinimumHoursForTag && day.Average is not null).ToList();

        if (eligible.Count < MinimumDaysForTags)
        {
            return (null, null);
        }

        return (eligible.MinBy(day => day.Average)!.Date, eligible.MaxBy(day => day.Average)!.Date);
    }

    private static double?[] HourlyMeans(List<Reading> inDay, Metric metric, LocalCalendar calendar)
    {
        var sums = new double[24];
        var counts = new int[24];

        foreach (var reading in inDay)
        {
            var hour = TimeZoneInfo.ConvertTime(reading.Timestamp, calendar.Zone).Hour;
            sums[hour] += reading.ValueOf(metric);
            counts[hour]++;
        }

        return Enumerable.Range(0, 24).Select(hour => counts[hour] == 0 ? (double?)null : sums[hour] / counts[hour]).ToArray();
    }

    /// <summary>24 for a whole day; for the day that holds <paramref name="end"/>, the hours that have started by then.</summary>
    private static int HoursShown(DateOnly date, DateTimeOffset end, LocalCalendar calendar)
    {
        if (calendar.StartOfLocalDay(date.AddDays(1)) <= end)
        {
            return 24;
        }

        var local = TimeZoneInfo.ConvertTime(end, calendar.Zone).TimeOfDay;

        return (int)Math.Ceiling(local.TotalHours);
    }
}

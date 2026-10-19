using Deskair.Domain.Analytics.Comfort;
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

    /// <summary>A month needs readings in at least this many hours (ten days' worth) to be the best or the worst one.</summary>
    public const int MinimumHoursForMonthTag = 240;

    /// <summary>A day needs readings in at least this many different hours for its square on the Year grid to have a value.</summary>
    public const int MinimumHoursForDay = 6;

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
                metric == Metric.Co2 ? ZoneShareCalculator.Calculate(inDay) : null)
            {
                Comfort = ComfortShareOf(inDay, metric)
            });
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

        return Extremes(days, day => day.Date, day => day.IsComplete, day => day.HoursWithReadings, day => day.Average, MinimumHoursForTag);
    }

    /// <summary>The best and the worst month of the Year grid: the same rule as for days, with at least <see cref="MinimumHoursForMonthTag"/> hours of readings.</summary>
    public static (DateOnly? Best, DateOnly? Worst) Extremes(IReadOnlyList<HourGridMonth> months)
    {
        ArgumentNullException.ThrowIfNull(months);

        return Extremes(months, month => month.Month, month => month.IsComplete, month => month.HoursWithReadings, month => month.Average, MinimumHoursForMonthTag);
    }


    /// <summary>
    /// The worst day for Temperature and Humidity: the one with the least time inside the comfortable range, among complete days with readings in at least
    /// <see cref="MinimumHoursForTag"/> hours, when at least <see cref="MinimumDaysForTags"/> qualify. There is no best day: when most days are comfortable,
    /// ranking near-identical days is noise. Null when no day has any time outside the range or every day is equally far from it (behaviour-rules.md "Aggregation" rule 10).
    /// </summary>
    public static DateOnly? ComfortWorst(IReadOnlyList<HourGridDay> days)
    {
        ArgumentNullException.ThrowIfNull(days);

        return ComfortWorst(days, day => day.Date, day => day.IsComplete, day => day.HoursWithReadings, day => day.Comfort, MinimumHoursForTag);
    }

    /// <summary>The worst month by time inside the comfortable range: the same rule as for days, with complete months and at least <see cref="MinimumHoursForMonthTag"/> hours of readings.</summary>
    public static DateOnly? ComfortWorst(IReadOnlyList<HourGridMonth> months)
    {
        ArgumentNullException.ThrowIfNull(months);

        return ComfortWorst(months, month => month.Month, month => month.IsComplete, month => month.HoursWithReadings, month => month.Comfort, MinimumHoursForMonthTag);
    }
    /// <summary>
    /// One row per local month from the month of <paramref name="start"/> to the month of <paramref name="end"/> (exclusive). A day's square is the mean of its readings when they cover
    /// at least <see cref="MinimumHoursForDay"/> different hours, otherwise null. Days after the end are not shown.
    /// </summary>
    public static IReadOnlyList<HourGridMonth> BuildMonths(IEnumerable<Reading> readings, Metric metric, DateTimeOffset start, DateTimeOffset end, LocalCalendar calendar)
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

        var firstDay = calendar.LocalDateOf(start);
        var lastDay = calendar.LocalDateOf(end.AddTicks(-1));
        var months = new List<HourGridMonth>();

        for (var month = new DateOnly(firstDay.Year, firstDay.Month, 1); month <= lastDay; month = month.AddMonths(1))
        {
            var length = DateTime.DaysInMonth(month.Year, month.Month);
            var inMonth = new List<Reading>();
            var hoursWithReadings = 0;
            var days = new double?[31];

            for (var number = 1; number <= length; number++)
            {
                var date = month.AddDays(number - 1);

                if (date < firstDay || date > lastDay || !byDay.TryGetValue(date, out var inDay))
                {
                    continue;
                }

                inMonth.AddRange(inDay);

                var hours = DistinctHours(inDay, calendar);
                hoursWithReadings += hours;
                days[number - 1] = hours >= MinimumHoursForDay ? inDay.Average(reading => reading.ValueOf(metric)) : null;
            }

            var shown = lastDay < month.AddDays(length - 1) ? lastDay.Day : length;

            months.Add(new HourGridMonth(
                month,
                days,
                shown,
                inMonth.Count == 0 ? null : inMonth.Average(reading => reading.ValueOf(metric)),
                metric == Metric.Co2 ? ZoneShareCalculator.Calculate(inMonth) : null,
                hoursWithReadings)
            {
                Comfort = ComfortShareOf(inMonth, metric)
            });
        }

        return months;
    }

    private static (DateOnly? Best, DateOnly? Worst) Extremes<T>(IReadOnlyList<T> rows, Func<T, DateOnly> date, Func<T, bool> complete, Func<T, int> hours, Func<T, double?> average, int minimumHours)
    {
        var eligible = rows.Where(row => complete(row) && hours(row) >= minimumHours && average(row) is not null).ToList();

        if (eligible.Count < MinimumDaysForTags)
        {
            return (null, null);
        }

        return (date(eligible.MinBy(row => average(row))!), date(eligible.MaxBy(row => average(row))!));
    }

    private static DateOnly? ComfortWorst<T>(IReadOnlyList<T> rows, Func<T, DateOnly> date, Func<T, bool> complete, Func<T, int> hours, Func<T, ComfortShare?> comfort, int minimumHours)
    {
        var eligible = rows.Where(row => complete(row) && hours(row) >= minimumHours && comfort(row) is not null).ToList();

        if (eligible.Count < MinimumDaysForTags)
        {
            return null;
        }

        var worst = eligible.MinBy(row => comfort(row)!.Inside)!;
        var best = eligible.MaxBy(row => comfort(row)!.Inside)!;

        // A row that is entirely inside the range is not "worst", and rows equally far from it have no worst.
        var allInside = comfort(worst)!.Inside >= 1;
        var allEqual = comfort(worst)!.Inside == comfort(best)!.Inside;

        return allInside || allEqual ? null : date(worst);
    }

    private static ComfortShare? ComfortShareOf(IEnumerable<Reading> readings, Metric metric)
        => ComfortScale.For(metric)?.Share(readings.Select(reading => reading.ValueOf(metric)));

    private static int DistinctHours(List<Reading> inDay, LocalCalendar calendar)
        => inDay.Select(reading => TimeZoneInfo.ConvertTime(reading.Timestamp, calendar.Zone).Hour).Distinct().Count();

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

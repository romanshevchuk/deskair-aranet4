using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;

namespace Deskair.Domain.Analytics.TypicalDay;

public static class TypicalDayCalculator
{
    /// <summary>The mean per local hour of day of the metric (CO₂ unless another is given). Readings without a value for the metric are left out.</summary>
    public static IReadOnlyList<HourOfDayAverage> HourlyAverages(
        IReadOnlyList<Reading> readings,
        LocalCalendar calendar,
        Metric metric = Metric.Co2)
    {
        var byHour = readings
            .Where(reading => !double.IsNaN(reading.ValueOf(metric)))
            .ToLookup(reading => TimeZoneInfo.ConvertTime(reading.Timestamp, calendar.Zone).Hour, reading => reading.ValueOf(metric));

        return Enumerable.Range(0, 24)
            .Select(hour => byHour[hour].Any()
                ? new HourOfDayAverage(hour, byHour[hour].Average(), byHour[hour].Count())
                : new HourOfDayAverage(hour, null, 0))
            .ToList();
    }

    public static int? WorstWindowStartHour(IReadOnlyList<HourOfDayAverage> hours) => WorstWindow(hours)?.StartHour;

    /// <summary>The three hours in a row with the highest mean (the earliest on a tie), or null when no three hours in a row all have data.</summary>
    public static HourWindow? WorstWindow(IReadOnlyList<HourOfDayAverage> hours) => Window(hours, mean => mean);

    /// <summary>
    /// The three hours in a row whose mean is furthest outside the comfortable range (the earliest on a tie), or null when no three hours in a row all have data
    /// or every window is inside the range (behaviour-rules.md "Aggregation" rule 11).
    /// </summary>
    public static HourWindow? FurthestOutsideWindow(IReadOnlyList<HourOfDayAverage> hours, ComfortScale scale)
    {
        ArgumentNullException.ThrowIfNull(scale);

        var window = Window(hours, scale.Outside);

        return window is not null && scale.Outside(window.Average) > 0 ? window : null;
    }

    private static HourWindow? Window(IReadOnlyList<HourOfDayAverage> hours, Func<double, double> score)
    {
        var averages = new double?[24];

        foreach (var hour in hours)
        {
            averages[hour.Hour] = hour.Average;
        }

        HourWindow? worst = null;
        var worstScore = double.MinValue;

        for (var start = 0; start < 24; start++)
        {
            var first = averages[start];
            var second = averages[(start + 1) % 24];
            var third = averages[(start + 2) % 24];

            if (first is null || second is null || third is null)
            {
                continue;
            }

            var mean = (first.Value + second.Value + third.Value) / 3;

            if (score(mean) > worstScore)
            {
                worstScore = score(mean);
                worst = new HourWindow(start, mean);
            }
        }

        return worst;
    }
}

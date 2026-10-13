using Deskair.Domain.Readings;
using Deskair.Domain.Time;

namespace Deskair.Domain.Analytics.TypicalDay;

public static class TypicalDayCalculator
{
    public static IReadOnlyList<HourOfDayAverage> HourlyAverages(
        IReadOnlyList<Reading> readings,
        LocalCalendar calendar)
    {
        var byHour = readings
            .ToLookup(reading => TimeZoneInfo.ConvertTime(reading.Timestamp, calendar.Zone).Hour, reading => reading.Co2Ppm);

        return Enumerable.Range(0, 24)
            .Select(hour => byHour[hour].Any()
                ? new HourOfDayAverage(hour, byHour[hour].Average(), byHour[hour].Count())
                : new HourOfDayAverage(hour, null, 0))
            .ToList();
    }

    public static int? WorstWindowStartHour(IReadOnlyList<HourOfDayAverage> hours) => WorstWindow(hours)?.StartHour;

    /// <summary>The three hours in a row with the highest mean (the earliest on a tie), or null when no three hours in a row all have data.</summary>
    public static HourWindow? WorstWindow(IReadOnlyList<HourOfDayAverage> hours)
    {
        var averages = new double?[24];

        foreach (var hour in hours)
        {
            averages[hour.Hour] = hour.Average;
        }

        HourWindow? worst = null;
        var worstMean = double.MinValue;

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

            if (mean > worstMean)
            {
                worstMean = mean;
                worst = new HourWindow(start, mean);
            }
        }

        return worst;
    }
}

using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;

namespace Deskair.Domain.Analytics.Sleep;

/// <summary>
/// Summarises the nights that start inside a period (docs/product/behaviour-rules.md "Aggregation" rule 8). A night belongs to the evening it starts on,
/// and counts when at least <see cref="MinimumRecorded"/> of it has readings. The readings may run past the end of the period to cover the last night.
/// </summary>
public static class NightsCalculator
{
    public static readonly TimeSpan MinimumRecorded = TimeSpan.FromHours(6);

    /// <returns>The summary, or null when the night window is empty or no night in the period counts.</returns>
    public static NightsSummary? Summarise(
        IReadOnlyList<Reading> readings,
        Metric metric,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan interval,
        NightWindow night,
        LocalCalendar calendar,
        ZoneThresholds thresholds)
    {
        if (night.IsEmpty)
        {
            return null;
        }

        var results = new List<NightResult>();
        var last = calendar.LocalDateOf(end - TimeSpan.FromTicks(1));

        for (var date = calendar.LocalDateOf(start); date <= last; date = date.AddDays(1))
        {
            var nightStart = calendar.InstantAt(date, night.Start);
            var nightEnd = calendar.InstantAt(night.CrossesMidnight ? date.AddDays(1) : date, night.End);

            if (nightStart < start || nightStart >= end)
            {
                continue;
            }

            var summary = SleepCalculator.Summarise(readings, metric, nightStart, nightEnd, interval, calendar, thresholds);

            if (summary.Average is { } average && summary.Recorded >= MinimumRecorded)
            {
                results.Add(new NightResult(nightStart, nightEnd, average));
            }
        }

        if (results.Count == 0)
        {
            return null;
        }

        var withExtremes = metric is Metric.Co2 or Metric.Temperature or Metric.Humidity;
        var lowest = withExtremes ? results.MinBy(result => result.Average) : null;
        var highest = withExtremes ? results.MaxBy(result => result.Average) : null;

        return new NightsSummary(metric, results.Count, results.Average(result => result.Average), lowest, highest);
    }
}

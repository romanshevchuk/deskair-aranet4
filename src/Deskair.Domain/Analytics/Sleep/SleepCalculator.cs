using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;

namespace Deskair.Domain.Analytics.Sleep;

/// <summary>
/// Summarises one night (docs/product/behaviour-rules.md "Sleep"). The readings are the ones taken from <c>start</c> up to <c>end</c>; each stands for one sensor interval.
/// Deep sleep is the average between 02:00 and 05:00 local time, whatever the night window is; a night that does not reach those hours has none.
/// </summary>
public static class SleepCalculator
{
    private static readonly TimeSpan deepSleepFrom = TimeSpan.FromHours(2);
    private static readonly TimeSpan deepSleepTo = TimeSpan.FromHours(5);

    public static SleepSummary Summarise(
        IReadOnlyList<Reading> readings,
        Metric metric,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan interval,
        LocalCalendar calendar,
        ZoneThresholds thresholds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        var inNight = readings
            .Where(reading => reading.Timestamp >= start && reading.Timestamp < end && !double.IsNaN(reading.ValueOf(metric)))
            .OrderBy(reading => reading.Timestamp)
            .ToList();
        var expected = end - start;

        if (inNight.Count == 0)
        {
            return new SleepSummary(metric, start, end, 0, TimeSpan.Zero, expected, null, null, null, null, null, null);
        }

        var values = inNight.Select(reading => new TimedValue(reading.ValueOf(metric), reading.Timestamp)).ToList();
        var average = values.Average(value => value.Value);
        var lowest = values.MinBy(value => value.Value);
        var peak = values.MaxBy(value => value.Value);
        var deep = values.Where(value => IsDeepSleep(value.Time, calendar)).Select(value => value.Value).ToList();

        TimeSpan? elevated = null;
        SleepVerdict? verdict = null;

        if (metric == Metric.Co2)
        {
            elevated = interval * values.Count(value => value.Value >= thresholds.ElevatedFromPpm);
            verdict = SleepVerdictRule.Classify(average, elevated.Value, thresholds);
        }

        return new SleepSummary(
            metric,
            start,
            end,
            values.Count,
            interval * values.Count,
            expected,
            average,
            lowest,
            peak,
            deep.Count == 0 ? null : deep.Average(),
            elevated,
            verdict);
    }

    private static bool IsDeepSleep(DateTimeOffset instant, LocalCalendar calendar)
    {
        var timeOfDay = TimeZoneInfo.ConvertTime(instant, calendar.Zone).TimeOfDay;

        return timeOfDay >= deepSleepFrom && timeOfDay < deepSleepTo;
    }
}

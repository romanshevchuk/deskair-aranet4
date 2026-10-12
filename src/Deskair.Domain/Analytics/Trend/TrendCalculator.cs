using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;

namespace Deskair.Domain.Analytics.Trend;

public static class TrendCalculator
{
    private static readonly TimeSpan co2Window = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan co2Sample = TimeSpan.FromMinutes(6);
    private const double Co2SteadyBelowPpm = 30;

    public static TrendResult Co2(IReadOnlyList<Reading> readings, DateTimeOffset now)
    {
        var recent = AverageCo2(readings, now - co2Sample, now);
        var earlier = AverageCo2(readings, now - co2Window - co2Sample, now - co2Window);

        if (recent is null || earlier is null)
        {
            return new TrendResult(TrendDirection.Steady, 0, co2Window);
        }

        var change = recent.Value - earlier.Value;
        var direction = Math.Abs(change) < Co2SteadyBelowPpm
            ? TrendDirection.Steady
            : change > 0 ? TrendDirection.Rising : TrendDirection.Falling;

        return new TrendResult(direction, change, co2Window);
    }

    private static double? AverageCo2(IReadOnlyList<Reading> readings, DateTimeOffset from, DateTimeOffset to)
    {
        var inWindow = readings.Where(reading => reading.Timestamp >= from && reading.Timestamp <= to).ToList();
        return inWindow.Count == 0 ? null : inWindow.Average(reading => reading.Co2Ppm);
    }

    private static readonly TimeSpan otherWindow = TimeSpan.FromHours(1);
    private static readonly TimeSpan otherMinimumSpan = TimeSpan.FromMinutes(30);

    public static TrendResult Other(IReadOnlyList<Reading> readings, Metric metric, DateTimeOffset now)
    {
        var limit = metric switch
        {
            Metric.Temperature => 0.5,
            Metric.Humidity => 3,
            Metric.Pressure => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Use Co2() for CO₂.")
        };

        var inWindow = readings
            .Where(reading => reading.Timestamp >= now - otherWindow && reading.Timestamp <= now)
            .OrderBy(reading => reading.Timestamp)
            .ToList();

        if (inWindow.Count < 2 || inWindow[^1].Timestamp - inWindow[0].Timestamp < otherMinimumSpan)
        {
            return new TrendResult(TrendDirection.Steady, 0, otherWindow);
        }

        var change = inWindow[^1].ValueOf(metric) - inWindow[0].ValueOf(metric);
        var direction = Math.Abs(change) < limit
            ? TrendDirection.Steady
            : change > 0 ? TrendDirection.Rising : TrendDirection.Falling;

        return new TrendResult(direction, change, otherWindow);
    }
}

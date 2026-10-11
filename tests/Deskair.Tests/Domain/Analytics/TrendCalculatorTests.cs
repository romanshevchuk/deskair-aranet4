using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class TrendCalculatorTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 9, 12);

    // docs/product/behaviour-rules.md: "30 minutes earlier" is the 6 minute window that ends 30 minutes before now.
    private static List<Reading> Co2Readings(double earlier, double latest)
    {
        var readings = new List<Reading>();

        for (var minute = 0; minute <= 6; minute++)
        {
            readings.Add(Samples.Co2(now.AddMinutes(-36 + minute), earlier));
            readings.Add(Samples.Co2(now.AddMinutes(-6 + minute), latest));
        }

        return readings;
    }

    [Fact(Skip = "TODO(LIVE-01a)")]
    public void Co2_is_Rising_when_the_average_grows_by_30_ppm_or_more()
    {
        var result = TrendCalculator.Co2(Co2Readings(1000, 1100), now);

        Assert.Equal(TrendDirection.Rising, result.Direction);
        Assert.Equal(100, result.Change, 6);
        Assert.Equal(TimeSpan.FromMinutes(30), result.Window);
    }

    [Fact(Skip = "TODO(LIVE-01a)")]
    public void Co2_is_Falling_when_the_average_drops_by_30_ppm_or_more()
    {
        var result = TrendCalculator.Co2(Co2Readings(1100, 1000), now);

        Assert.Equal(TrendDirection.Falling, result.Direction);
        Assert.Equal(-100, result.Change, 6);
    }

    [Fact(Skip = "TODO(LIVE-01a)")]
    public void Co2_is_Steady_under_30_ppm()
    {
        var result = TrendCalculator.Co2(Co2Readings(1000, 1020), now);

        Assert.Equal(TrendDirection.Steady, result.Direction);
    }

    [Fact(Skip = "TODO(LIVE-01a)")]
    public void Co2_is_Steady_with_no_change_when_there_is_nothing_to_compare()
    {
        var result = TrendCalculator.Co2([], now);

        Assert.Equal(TrendDirection.Steady, result.Direction);
        Assert.Equal(0, result.Change);
    }

    [Theory(Skip = "TODO(LIVE-01b)")]
    [InlineData(Metric.Temperature, 21.0, 21.6, TrendDirection.Rising)]
    [InlineData(Metric.Temperature, 21.0, 21.3, TrendDirection.Steady)]
    [InlineData(Metric.Humidity, 45, 41, TrendDirection.Falling)]
    [InlineData(Metric.Humidity, 45, 47, TrendDirection.Steady)]
    [InlineData(Metric.Pressure, 1010, 1011.5, TrendDirection.Rising)]
    [InlineData(Metric.Pressure, 1010, 1010.5, TrendDirection.Steady)]
    public void Other_is_Steady_unless_the_hourly_change_reaches_the_limit(
        Metric metric,
        double hourAgo,
        double latest,
        TrendDirection expected)
    {
        var readings = new List<Reading>
        {
            Samples.Climate(now.AddMinutes(-60), hourAgo, hourAgo, hourAgo),
            Samples.Climate(now, latest, latest, latest)
        };

        var result = TrendCalculator.Other(readings, metric, now);

        Assert.Equal(expected, result.Direction);
    }

    [Fact(Skip = "TODO(LIVE-01b)")]
    public void Other_rejects_Co2()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TrendCalculator.Other([], Metric.Co2, now));
    }
}

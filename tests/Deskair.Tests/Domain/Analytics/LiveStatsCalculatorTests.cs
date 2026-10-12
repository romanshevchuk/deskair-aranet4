using Deskair.Domain.Analytics.Live;
using Deskair.Domain.Readings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class LiveStatsCalculatorTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);

    private static List<Reading> At(params (int MinutesAgo, double Ppm)[] values)
        => values.Select(value => Samples.Co2(now.AddMinutes(-value.MinutesAgo), value.Ppm)).OrderBy(reading => reading.Timestamp).ToList();

    [Fact]
    public void Good_share_is_the_fraction_of_readings_under_the_elevated_limit()
    {
        var readings = At((40, 600), (30, 700), (20, 1000), (10, 1200));

        Assert.Equal(0.5, LiveStatsCalculator.GoodShare(readings));
    }

    [Fact]
    public void Exactly_1000_ppm_is_not_good()
    {
        Assert.Equal(0, LiveStatsCalculator.GoodShare(At((10, 1000))));
    }

    [Fact]
    public void Good_share_of_no_readings_is_unknown()
    {
        Assert.Null(LiveStatsCalculator.GoodShare([]));
    }

    [Fact]
    public void Peak_is_the_highest_value_and_when_it_happened()
    {
        var readings = At((60, 800), (50, 1800), (40, 1200), (10, 900));

        var peak = LiveStatsCalculator.Peak(readings);

        Assert.Equal(1800, peak!.Value.Ppm);
        Assert.Equal(now.AddMinutes(-50), peak.Value.At);
    }

    [Fact]
    public void Peak_prefers_the_most_recent_of_equal_values()
    {
        var readings = At((60, 1500), (30, 1500), (10, 900));

        Assert.Equal(now.AddMinutes(-30), LiveStatsCalculator.Peak(readings)!.Value.At);
    }

    [Fact]
    public void Peak_of_no_readings_is_unknown()
    {
        Assert.Null(LiveStatsCalculator.Peak([]));
    }

    [Fact]
    public void Time_in_the_zone_counts_back_to_the_last_change()
    {
        var readings = At((60, 600), (50, 1200), (40, 1250), (30, 1100), (0, 1300));

        Assert.Equal(TimeSpan.FromMinutes(50), LiveStatsCalculator.TimeInCurrentZone(readings, now));
    }

    [Fact]
    public void A_zone_held_for_the_whole_period_counts_from_the_first_reading()
    {
        var readings = At((120, 500), (60, 600), (0, 650));

        Assert.Equal(TimeSpan.FromMinutes(120), LiveStatsCalculator.TimeInCurrentZone(readings, now));
    }

    [Fact]
    public void Time_in_the_zone_runs_to_now_even_when_the_last_reading_is_a_bit_old()
    {
        var readings = At((10, 1200), (4, 1300));

        Assert.Equal(TimeSpan.FromMinutes(10), LiveStatsCalculator.TimeInCurrentZone(readings, now));
    }

    [Fact]
    public void Time_in_the_zone_of_no_readings_is_unknown()
    {
        Assert.Null(LiveStatsCalculator.TimeInCurrentZone([], now));
    }

    [Fact]
    public void A_change_to_a_new_zone_resets_the_time()
    {
        var readings = At((30, 500), (20, 600), (10, 1100), (0, 1150));

        Assert.Equal(TimeSpan.FromMinutes(10), LiveStatsCalculator.TimeInCurrentZone(readings, now));
    }
}

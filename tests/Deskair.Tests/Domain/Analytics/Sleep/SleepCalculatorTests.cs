using Deskair.Domain.Analytics.Sleep;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics.Sleep;

public class SleepCalculatorTests
{
    private static readonly LocalCalendar utc = new(TimeZoneInfo.Utc);
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 12, 22);
    private static readonly DateTimeOffset end = Samples.Utc(2026, 10, 13, 7);
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(30);

    /// <summary>One reading every 30 minutes through the night, each with the value the function gives for its time.</summary>
    private static List<Reading> Night(Func<DateTimeOffset, double> ppm)
    {
        var readings = new List<Reading>();

        for (var time = start; time < end; time += interval)
        {
            readings.Add(Samples.Co2(time, ppm(time)));
        }

        return readings;
    }

    private static SleepSummary Summarise(IReadOnlyList<Reading> readings, Metric metric = Metric.Co2, LocalCalendar? calendar = null)
        => SleepCalculator.Summarise(readings, metric, start, end, interval, calendar ?? utc, ZoneThresholds.Default);

    [Fact]
    public void A_fresh_night_is_good()
    {
        var summary = Summarise(Night(_ => 600));

        Assert.Equal(SleepVerdict.Good, summary.Verdict);
        Assert.Equal(600, summary.Average);
        Assert.Equal(TimeSpan.Zero, summary.TimeElevated);
        Assert.Equal(18, summary.ReadingCount);
        Assert.False(summary.IsPartial);
    }

    [Fact]
    public void Time_above_the_limit_counts_every_reading_at_or_over_it_as_one_interval()
    {
        // 1,000 counts as above; 999 does not. Two readings (00:00 and 00:30) at 1,000 make one hour.
        var readings = Night(time => time.Day == 13 && time.Hour == 0 ? 1000 : 999);

        Assert.Equal(TimeSpan.FromHours(1), Summarise(readings).TimeElevated);
    }

    [Fact]
    public void Peak_and_lowest_say_when_they_happened_and_the_earlier_one_wins_a_tie()
    {
        var readings = Night(time => time.Hour == 1 ? 1500 : time.Hour == 4 ? 400 : 800);

        var summary = Summarise(readings);

        Assert.Equal(new TimedValue(1500, Samples.Utc(2026, 10, 13, 1)), summary.Peak);
        Assert.Equal(new TimedValue(400, Samples.Utc(2026, 10, 13, 4)), summary.Lowest);
    }

    [Fact]
    public void Deep_sleep_is_the_average_between_two_and_five_in_the_morning()
    {
        // 02:00, 02:30 ... 04:30 are deep sleep. 05:00 is not.
        var readings = Night(time => time.Day == 13 && time.Hour is >= 2 and < 5 ? 700 : 1100);

        Assert.Equal(700, Summarise(readings).DeepSleepAverage);
    }

    [Fact]
    public void A_night_that_ends_before_two_has_no_deep_sleep_average()
    {
        var earlyEnd = Samples.Utc(2026, 10, 13, 1);
        var readings = Night(_ => 700).Where(reading => reading.Timestamp < earlyEnd).ToList();

        var summary = SleepCalculator.Summarise(readings, Metric.Co2, start, earlyEnd, interval, utc, ZoneThresholds.Default);

        Assert.Null(summary.DeepSleepAverage);
        Assert.NotNull(summary.Average);
    }

    [Fact]
    public void Deep_sleep_follows_local_time()
    {
        // In Kyiv (UTC+3 on 13 October) 02:00 to 05:00 local is 23:00 to 02:00 UTC.
        var kyiv = new LocalCalendar(Samples.Kyiv);
        var readings = Night(time => time >= Samples.Utc(2026, 10, 12, 23) && time < Samples.Utc(2026, 10, 13, 2) ? 700 : 1100);

        Assert.Equal(700, Summarise(readings, calendar: kyiv).DeepSleepAverage);
    }

    [Fact]
    public void Readings_outside_the_night_are_ignored()
    {
        var readings = Night(_ => 600);
        readings.Add(Samples.Co2(start.AddMinutes(-30), 2000));
        readings.Add(Samples.Co2(end, 2000));

        var summary = Summarise(readings);

        Assert.Equal(18, summary.ReadingCount);
        Assert.Equal(600, summary.Peak!.Value.Value);
    }

    [Fact]
    public void Readings_without_a_value_are_left_out()
    {
        var readings = Night(_ => 600);
        readings[3] = Samples.Co2(readings[3].Timestamp, double.NaN);

        Assert.Equal(17, Summarise(readings).ReadingCount);
    }

    [Fact]
    public void A_night_with_gaps_is_partial_under_eighty_percent_of_the_hours()
    {
        var most = Summarise(Night(_ => 600).Take(15).ToList());
        var half = Summarise(Night(_ => 600).Take(9).ToList());

        Assert.False(most.IsPartial);
        Assert.True(half.IsPartial);
        Assert.Equal(TimeSpan.FromHours(4.5), half.Recorded);
        Assert.Equal(TimeSpan.FromHours(9), half.Expected);
    }

    [Fact]
    public void A_night_without_readings_has_nothing_made_up()
    {
        var summary = Summarise([]);

        Assert.False(summary.HasData);
        Assert.Null(summary.Average);
        Assert.Null(summary.Peak);
        Assert.Null(summary.Lowest);
        Assert.Null(summary.DeepSleepAverage);
        Assert.Null(summary.Verdict);
        Assert.Null(summary.TimeElevated);
        Assert.True(summary.IsPartial);
    }

    [Fact]
    public void Other_metrics_have_values_but_no_verdict_and_no_time_above()
    {
        var readings = Night(_ => 600).Select(reading => reading with { TemperatureC = 18 + (reading.Timestamp.Hour % 3) }).ToList();

        var summary = Summarise(readings, Metric.Temperature);

        Assert.Null(summary.Verdict);
        Assert.Null(summary.TimeElevated);
        Assert.Equal(18.0, summary.Lowest!.Value.Value, 6);
        Assert.Equal(20.0, summary.Peak!.Value.Value, 6);
    }

    [Fact]
    public void The_interval_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SleepCalculator.Summarise([], Metric.Co2, start, end, TimeSpan.Zero, utc, ZoneThresholds.Default));
    }
}

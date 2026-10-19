using Deskair.Domain.Analytics.HourGrid;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class HourGridMonthsTests
{
    private static readonly LocalCalendar utc = new(TimeZoneInfo.Utc);

    private static List<Reading> Day(int year, int month, int day, int hours, double ppm)
        => [.. Enumerable.Range(0, hours).Select(hour => Samples.Co2(Samples.Utc(year, month, day, hour, 10), ppm))];

    [Fact]
    public void There_is_a_row_for_every_month_from_the_start_to_the_end()
    {
        var months = HourGridCalculator.BuildMonths([], Metric.Co2, Samples.Utc(2025, 11, 1), Samples.Utc(2026, 10, 14, 13), utc);

        Assert.Equal(12, months.Count);
        Assert.Equal(new DateOnly(2025, 11, 1), months[0].Month);
        Assert.Equal(new DateOnly(2026, 10, 1), months[^1].Month);
        Assert.All(months, month => Assert.True(month.IsEmpty));
        Assert.All(months, month => Assert.Equal(31, month.Days.Count));
    }

    [Fact]
    public void A_day_needs_readings_in_at_least_six_different_hours_for_a_value()
    {
        var readings = Day(2026, 10, 5, 6, 800).Concat(Day(2026, 10, 6, 5, 1200)).ToList();

        var october = HourGridCalculator.BuildMonths(readings, Metric.Co2, Samples.Utc(2025, 11, 1), Samples.Utc(2026, 10, 14), utc)[^1];

        Assert.Equal(800, october.Days[4]);
        Assert.Null(october.Days[5]);
        Assert.Equal(11, october.HoursWithReadings);
    }

    [Fact]
    public void Many_readings_in_few_hours_do_not_make_a_day()
    {
        var readings = Enumerable.Range(0, 60).Select(minute => Samples.Co2(Samples.Utc(2026, 10, 5, 9, minute), 900)).ToList();

        var october = HourGridCalculator.BuildMonths(readings, Metric.Co2, Samples.Utc(2026, 10, 1), Samples.Utc(2026, 10, 14), utc)[^1];

        Assert.Null(october.Days[4]);
        Assert.Equal(1, october.HoursWithReadings);
    }

    [Fact]
    public void The_month_mean_uses_every_reading_and_the_zone_share_is_given_for_co2()
    {
        var readings = Day(2026, 10, 5, 6, 800).Concat(Day(2026, 10, 6, 6, 1200)).ToList();

        var october = HourGridCalculator.BuildMonths(readings, Metric.Co2, Samples.Utc(2026, 10, 1), Samples.Utc(2026, 10, 14), utc)[^1];

        Assert.Equal(1000, october.Average);
        Assert.NotNull(october.Share);
    }

    [Fact]
    public void Days_after_the_end_and_after_the_month_are_not_shown()
    {
        var months = HourGridCalculator.BuildMonths([], Metric.Co2, Samples.Utc(2026, 2, 1), Samples.Utc(2026, 10, 14, 13), utc);

        Assert.Equal(28, months[0].DaysShown);
        Assert.Equal(30, months[2].DaysShown);
        Assert.Equal(14, months[^1].DaysShown);
    }

    [Fact]
    public void Readings_before_the_start_or_from_other_metrics_are_ignored()
    {
        var readings = Day(2026, 9, 28, 8, 2000);

        var months = HourGridCalculator.BuildMonths(readings, Metric.Co2, Samples.Utc(2026, 10, 1), Samples.Utc(2026, 10, 14), utc);

        Assert.All(months, month => Assert.True(month.IsEmpty));
    }

    [Fact]
    public void The_best_and_worst_month_need_240_hours_and_three_months()
    {
        HourGridMonth Month(int number, double average, int hours)
            => new(new DateOnly(2026, number, 1), new double?[31], 31, average, null, hours);

        var enough = new[] { Month(1, 900, 300), Month(2, 1300, 260), Month(3, 1100, 500), Month(4, 400, 100) };

        Assert.Equal((new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)), HourGridCalculator.Extremes(enough));

        var tooFew = new[] { Month(1, 900, 300), Month(2, 1300, 260), Month(4, 400, 239) };

        Assert.Equal((null, null), HourGridCalculator.Extremes(tooFew));
    }

    [Fact]
    public void The_month_in_progress_is_never_best_or_worst()
    {
        HourGridMonth Month(int number, double average, int daysShown)
            => new(new DateOnly(2026, number, 1), new double?[31], daysShown, average, null, 400);

        var months = new[] { Month(1, 900, 31), Month(2, 1300, 28), Month(3, 1100, 31), Month(4, 300, 14) };

        Assert.False(months[^1].IsComplete);
        Assert.Equal((new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)), HourGridCalculator.Extremes(months));
    }

    [Fact]
    public void Months_use_local_days()
    {
        var kyiv = new LocalCalendar(Samples.Kyiv);
        var start = kyiv.StartOfLocalDay(new DateOnly(2026, 10, 1));
        var end = kyiv.StartOfLocalDay(new DateOnly(2026, 10, 14));

        // 21:30 UTC on 4 October is 00:30 on 5 October in Kyiv (UTC+3), so all six readings belong to the 5th.
        var readings = Enumerable.Range(0, 6).Select(hour => Samples.Co2(Samples.Utc(2026, 10, 4, 21, 30).AddHours(hour), 700)).ToList();

        var october = HourGridCalculator.BuildMonths(readings, Metric.Co2, start, end, kyiv)[^1];

        Assert.Null(october.Days[3]);
        Assert.Equal(700, october.Days[4]);
    }
}

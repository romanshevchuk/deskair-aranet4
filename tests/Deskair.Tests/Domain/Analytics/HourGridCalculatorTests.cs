using Deskair.Domain.Analytics.HourGrid;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class HourGridCalculatorTests
{
    private static readonly LocalCalendar utc = new(TimeZoneInfo.Utc);

    [Fact]
    public void Each_hour_is_the_mean_of_its_readings_and_an_hour_without_readings_is_empty()
    {
        var readings = new[]
        {
            Samples.Co2(Samples.Utc(2026, 10, 12, 9, 10), 900),
            Samples.Co2(Samples.Utc(2026, 10, 12, 9, 40), 1100),
            Samples.Co2(Samples.Utc(2026, 10, 12, 16, 0), 1500)
        };

        var day = Assert.Single(HourGridCalculator.Build(readings, Metric.Co2, Samples.Utc(2026, 10, 12), Samples.Utc(2026, 10, 13), utc));

        Assert.Equal(1000, day.Hours[9]);
        Assert.Equal(1500, day.Hours[16]);
        Assert.Null(day.Hours[10]);
        Assert.Equal(2, day.HoursWithReadings);
        Assert.Equal(24, day.HoursShown);
        Assert.Equal((900 + 1100 + 1500) / 3.0, day.Average);
    }

    [Fact]
    public void There_is_a_row_for_every_day_even_without_readings_and_today_stops_at_now()
    {
        var days = HourGridCalculator.Build([], Metric.Co2, Samples.Utc(2026, 10, 8), Samples.Utc(2026, 10, 14, 13, 20), utc);

        Assert.Equal(7, days.Count);
        Assert.All(days, day => Assert.True(day.IsEmpty));
        Assert.Equal(14, days[^1].HoursShown);
        Assert.Equal(24, days[^2].HoursShown);
    }

    [Fact]
    public void Hours_are_local_and_the_repeated_autumn_hour_joins_the_first()
    {
        var kyiv = new LocalCalendar(Samples.Kyiv);
        var start = kyiv.StartOfLocalDay(new DateOnly(2026, 10, 25));

        // 03:30 local happens twice on 25 October 2026 (00:30 and 01:30 UTC).
        var readings = new[] { Samples.Co2(Samples.Utc(2026, 10, 25, 0, 30), 800), Samples.Co2(Samples.Utc(2026, 10, 25, 1, 30), 1200) };

        var day = Assert.Single(HourGridCalculator.Build(readings, Metric.Co2, start, kyiv.StartOfLocalDay(new DateOnly(2026, 10, 26)), kyiv));

        Assert.Equal(1000, day.Hours[3]);
        Assert.Equal(1, day.HoursWithReadings);
    }

    [Fact]
    public void Co2_rows_carry_the_zone_split_and_other_metrics_do_not()
    {
        var readings = new[] { Samples.Co2(Samples.Utc(2026, 10, 12, 9), 800), Samples.Co2(Samples.Utc(2026, 10, 12, 10), 1200) };

        var co2 = HourGridCalculator.Build(readings, Metric.Co2, Samples.Utc(2026, 10, 12), Samples.Utc(2026, 10, 13), utc)[0];
        var temperature = HourGridCalculator.Build(readings, Metric.Temperature, Samples.Utc(2026, 10, 12), Samples.Utc(2026, 10, 13), utc)[0];

        Assert.Equal(0.5, co2.Share!.Good);
        Assert.Equal(0.5, co2.Share.Elevated);
        Assert.Null(temperature.Share);
    }

    private static HourGridDay Day(int date, double average, int hours)
        => new(new DateOnly(2026, 10, date), Enumerable.Range(0, 24).Select(hour => hour < hours ? (double?)average : null).ToArray(), 24, average, null);

    [Fact]
    public void Best_and_worst_need_twelve_hours_and_three_qualifying_days()
    {
        var days = new[] { Day(8, 900, 24), Day(9, 1300, 12), Day(10, 600, 11), Day(11, 1100, 20) };

        // 10 October has the lowest mean but only 11 hours, so 8 October is the best.
        Assert.Equal((new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 9)), HourGridCalculator.Extremes(days));
        Assert.Equal((null, null), HourGridCalculator.Extremes(days.Take(2).ToList()));
    }
}

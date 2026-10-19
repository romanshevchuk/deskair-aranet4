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

    private static IEnumerable<Reading> ClimateDay(int day, double celsius)
        => Enumerable.Range(0, 24).Select(hour => Samples.Climate(Samples.Utc(2026, 10, day, hour), celsius, 45, 1013));

    [Fact]
    public void A_temperature_day_has_a_comfort_share_and_no_zone_share()
    {
        var readings = ClimateDay(12, 22).Take(12).Concat(ClimateDay(12, 18).Skip(12).Take(6)).Concat(ClimateDay(12, 25).Skip(18));

        var day = Assert.Single(HourGridCalculator.Build(readings, Metric.Temperature, Samples.Utc(2026, 10, 12), Samples.Utc(2026, 10, 13), utc));

        Assert.Null(day.Share);
        Assert.NotNull(day.Comfort);
        Assert.Equal(0.25, day.Comfort.Low);
        Assert.Equal(0.5, day.Comfort.Inside);
        Assert.Equal(0.25, day.Comfort.High);
    }

    [Fact]
    public void Co2_and_pressure_days_have_no_comfort_share()
    {
        var readings = ClimateDay(12, 22);

        Assert.All(HourGridCalculator.Build(readings, Metric.Co2, Samples.Utc(2026, 10, 12), Samples.Utc(2026, 10, 13), utc), day => Assert.Null(day.Comfort));
        Assert.All(HourGridCalculator.Build(readings, Metric.Pressure, Samples.Utc(2026, 10, 12), Samples.Utc(2026, 10, 13), utc), day => Assert.Null(day.Comfort));
    }

    [Fact]
    public void The_worst_comfort_day_is_the_one_with_the_least_time_inside_the_range_the_earliest_on_a_tie()
    {
        var readings = ClimateDay(10, 22).Concat(ClimateDay(11, 19)).Concat(ClimateDay(12, 22.5)).Concat(ClimateDay(13, 27));
        var days = HourGridCalculator.Build(readings, Metric.Temperature, Samples.Utc(2026, 10, 10), Samples.Utc(2026, 10, 14), utc);

        // 11 and 13 October are both entirely outside the range; the first of them is the worst.
        Assert.Equal(new DateOnly(2026, 10, 11), HourGridCalculator.ComfortWorst(days));
    }

    [Fact]
    public void The_worst_comfort_day_is_named_when_one_day_is_clearly_further_out()
    {
        var readings = ClimateDay(10, 22).Concat(ClimateDay(11, 19)).Concat(ClimateDay(12, 22.5));
        var days = HourGridCalculator.Build(readings, Metric.Temperature, Samples.Utc(2026, 10, 10), Samples.Utc(2026, 10, 13), utc);

        Assert.Equal(new DateOnly(2026, 10, 11), HourGridCalculator.ComfortWorst(days));
    }

    [Fact]
    public void Comfort_worst_needs_three_days_with_enough_hours()
    {
        var days = HourGridCalculator.Build(ClimateDay(10, 22).Concat(ClimateDay(11, 19)), Metric.Temperature, Samples.Utc(2026, 10, 10), Samples.Utc(2026, 10, 12), utc);

        Assert.Null(HourGridCalculator.ComfortWorst(days));
    }

    [Fact]
    public void Comfort_worst_is_empty_when_every_day_is_entirely_inside_the_range()
    {
        var readings = ClimateDay(10, 22).Concat(ClimateDay(11, 22)).Concat(ClimateDay(12, 22.5));
        var days = HourGridCalculator.Build(readings, Metric.Temperature, Samples.Utc(2026, 10, 10), Samples.Utc(2026, 10, 13), utc);

        Assert.Null(HourGridCalculator.ComfortWorst(days));
    }

    [Fact]
    public void The_day_in_progress_is_never_best_or_worst()
    {
        // The readings of 14 October end at 16:00, before its evening peak: its mean is lower than a whole day's would be.
        var readings = Enumerable.Range(11, 3).SelectMany(day => Enumerable.Range(0, 24).Select(hour => Samples.Co2(Samples.Utc(2026, 10, day, hour), 1000 + (day * 10))))
            .Concat(Enumerable.Range(0, 16).Select(hour => Samples.Co2(Samples.Utc(2026, 10, 14, hour), 500)));

        var days = HourGridCalculator.Build(readings, Metric.Co2, Samples.Utc(2026, 10, 11), Samples.Utc(2026, 10, 14, 16), utc);
        var (best, worst) = HourGridCalculator.Extremes(days);

        Assert.False(days[^1].IsComplete);
        Assert.Equal(new DateOnly(2026, 10, 11), best);
        Assert.Equal(new DateOnly(2026, 10, 13), worst);
    }

    [Fact]
    public void A_complete_day_is_complete()
    {
        var day = Assert.Single(HourGridCalculator.Build([], Metric.Co2, Samples.Utc(2026, 10, 12), Samples.Utc(2026, 10, 13), utc));

        Assert.True(day.IsComplete);
    }

    [Fact]
    public void The_comfort_worst_day_skips_the_day_in_progress()
    {
        var readings = ClimateDay(10, 22).Concat(ClimateDay(11, 19)).Concat(ClimateDay(12, 22.5)).Concat(ClimateDay(13, 27).Take(10));
        var days = HourGridCalculator.Build(readings, Metric.Temperature, Samples.Utc(2026, 10, 10), Samples.Utc(2026, 10, 13, 10), utc);

        Assert.Equal(new DateOnly(2026, 10, 11), HourGridCalculator.ComfortWorst(days));
    }
}

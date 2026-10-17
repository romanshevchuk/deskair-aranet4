using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.Sleep;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Domain.Zones;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics.Sleep;

public class NightsCalculatorTests
{
    private static readonly LocalCalendar utc = new(TimeZoneInfo.Utc);
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(30);
    private static readonly DateTimeOffset weekStart = Samples.Utc(2026, 10, 8);
    private static readonly DateTimeOffset weekEnd = Samples.Utc(2026, 10, 15);

    /// <summary>A whole night (22:00 to 07:00) that starts on the given day, every reading at the same value.</summary>
    private static IEnumerable<Reading> Night(int day, double ppm, int hours = 9)
    {
        var start = Samples.Utc(2026, 10, day, 22);

        for (var time = start; time < start.AddHours(hours); time += interval)
        {
            yield return Samples.Co2(time, ppm);
        }
    }

    private static NightsSummary? Summarise(IEnumerable<Reading> readings, Metric metric = Metric.Co2, NightWindow? night = null)
        => NightsCalculator.Summarise([.. readings], metric, weekStart, weekEnd, interval, night ?? NightWindow.Default, utc, ZoneThresholds.Default);

    [Fact]
    public void The_average_is_the_mean_of_the_nights_and_best_and_worst_name_their_evenings()
    {
        var summary = Summarise(Night(8, 700).Concat(Night(9, 1300)).Concat(Night(10, 1000)));

        Assert.NotNull(summary);
        Assert.Equal(3, summary.Count);
        Assert.Equal(1000, summary.Average);
        Assert.Equal(Samples.Utc(2026, 10, 8, 22), summary.Lowest!.Start);
        Assert.Equal(700, summary.Lowest.Average);
        Assert.Equal(Samples.Utc(2026, 10, 9, 22), summary.Highest!.Start);
        Assert.Equal(1300, summary.Highest.Average);
    }

    [Fact]
    public void A_night_with_fewer_than_six_recorded_hours_does_not_count()
    {
        var summary = Summarise(Night(8, 700).Concat(Night(9, 2000, hours: 5)));

        Assert.NotNull(summary);
        Assert.Equal(1, summary.Count);
        Assert.Equal(700, summary.Average);
    }

    [Fact]
    public void Exactly_six_recorded_hours_count()
    {
        var summary = Summarise(Night(8, 700, hours: 6));

        Assert.NotNull(summary);
        Assert.Equal(1, summary.Count);
    }

    [Fact]
    public void No_counted_night_gives_nothing()
    {
        Assert.Null(Summarise([]));
        Assert.Null(Summarise(Night(8, 700, hours: 3)));
    }

    [Fact]
    public void A_night_belongs_to_the_evening_it_starts_on()
    {
        // The night of 7 October ends on the 8th, inside the period, but started before it. The night of 14 October starts on the last day and runs past the end.
        var summary = Summarise(Night(7, 2000).Concat(Night(14, 600)));

        Assert.NotNull(summary);
        Assert.Equal(1, summary.Count);
        Assert.Equal(Samples.Utc(2026, 10, 14, 22), summary.Lowest!.Start);
    }

    [Fact]
    public void An_empty_night_window_gives_nothing()
    {
        var empty = new NightWindow(new TimeOnly(7, 0), new TimeOnly(7, 0));

        Assert.Null(Summarise(Night(8, 700), night: empty));
    }

    [Fact]
    public void Pressure_has_an_average_but_no_lowest_and_highest()
    {
        var readings = Night(8, 700).Concat(Night(9, 900)).Select(reading => reading with { PressureHpa = 1010 });

        var summary = Summarise(readings, Metric.Pressure);

        Assert.NotNull(summary);
        Assert.Equal(1010, summary.Average);
        Assert.Null(summary.Lowest);
        Assert.Null(summary.Highest);
    }

    [Fact]
    public void Temperature_has_its_coolest_and_warmest_night()
    {
        var readings = Night(8, 700).Concat(Night(9, 700)).Select(reading => reading with { TemperatureC = reading.Timestamp < Samples.Utc(2026, 10, 9, 12) ? 17 : 21 });

        var summary = Summarise(readings, Metric.Temperature);

        Assert.NotNull(summary);
        Assert.Equal(19, summary.Average);
        Assert.Equal(Samples.Utc(2026, 10, 8, 22), summary.Lowest!.Start);
        Assert.Equal(Samples.Utc(2026, 10, 9, 22), summary.Highest!.Start);
    }
}

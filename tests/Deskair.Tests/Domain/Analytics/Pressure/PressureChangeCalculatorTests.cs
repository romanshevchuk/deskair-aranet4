using Deskair.Domain.Analytics.Pressure;
using Deskair.Domain.Analytics.Sleep;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics.Pressure;

public class PressureChangeCalculatorTests
{
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 8);

    private static List<TimedValue> Hourly(Func<int, double> value, int hours)
        => [.. Enumerable.Range(0, hours).Select(hour => new TimedValue(value(hour), start.AddHours(hour)))];

    [Fact]
    public void A_steady_fall_is_the_biggest_change_and_it_names_where_it_started()
    {
        // Flat through hour 24, then 0.5 hPa an hour down for 24 hours: the biggest 24 hour change starts at hour 24.
        var series = Hourly(hour => hour <= 24 ? 1015 : 1015 - ((hour - 24) * 0.5), 49);

        var change = PressureChangeCalculator.Biggest(series);

        Assert.NotNull(change);
        Assert.Equal(-12, change.Delta, 6);
        Assert.Equal(start.AddHours(24), change.Start);
        Assert.Equal(start.AddHours(48), change.End);
    }

    [Fact]
    public void A_rise_counts_as_much_as_a_fall()
    {
        var series = Hourly(hour => hour < 24 ? 1000 : 1000 + ((hour - 23) * 0.4), 49);

        var change = PressureChangeCalculator.Biggest(series);

        Assert.NotNull(change);
        Assert.True(change.Delta > 0);
    }

    [Fact]
    public void Less_than_a_day_of_values_has_no_change()
    {
        Assert.Null(PressureChangeCalculator.Biggest(Hourly(hour => 1010 + hour, 24)));
        Assert.Null(PressureChangeCalculator.Biggest([]));
    }

    [Fact]
    public void A_missing_hour_is_never_filled_in()
    {
        // The hour 24 hours after hour 0 is missing, and nothing else is within 90 minutes of it.
        var series = Hourly(hour => 1010 + hour, 49).Where(value => value.Time != start.AddHours(24)).Where(value => value.Time != start.AddHours(25)).Where(value => value.Time != start.AddHours(23)).ToList();

        var change = PressureChangeCalculator.Biggest(series);

        Assert.NotNull(change);
        Assert.NotEqual(start, change.Start);
    }

    [Fact]
    public void Daily_values_a_day_apart_are_compared_even_over_a_clock_change()
    {
        var series = new List<TimedValue>
        {
            new(1010, Samples.Utc(2026, 10, 24, 9)),
            new(1004, Samples.Utc(2026, 10, 25, 10))
        };

        var change = PressureChangeCalculator.Biggest(series);

        Assert.NotNull(change);
        Assert.Equal(-6, change.Delta);
    }
}

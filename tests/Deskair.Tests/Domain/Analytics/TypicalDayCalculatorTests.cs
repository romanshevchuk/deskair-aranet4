using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class TypicalDayCalculatorTests
{
    private static readonly LocalCalendar utc = new(TimeZoneInfo.Utc);

    private static List<HourOfDayAverage> Day(params (int Hour, double Average)[] values)
    {
        var hours = Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, null, 0)).ToList();

        foreach (var (hour, average) in values)
        {
            hours[hour] = new HourOfDayAverage(hour, average, 1);
        }

        return hours;
    }

    [Fact]
    public void Hourly_averages_have_24_entries_and_use_only_existing_readings()
    {
        var readings = new List<Reading>
        {
            Samples.Co2(Samples.Utc(2026, 10, 8, 10), 800),
            Samples.Co2(Samples.Utc(2026, 10, 9, 10, 30), 1000)
        };

        var hours = TypicalDayCalculator.HourlyAverages(readings, utc);

        Assert.Equal(24, hours.Count);
        Assert.Equal(900, hours[10].Average);
        Assert.Equal(2, hours[10].ReadingCount);
        Assert.Null(hours[11].Average);
        Assert.Equal(0, hours[11].ReadingCount);
    }

    [Fact]
    public void Hourly_averages_use_local_time()
    {
        var readings = new List<Reading> { Samples.Co2(Samples.Utc(2026, 1, 15, 10), 800) };

        var hours = TypicalDayCalculator.HourlyAverages(readings, new LocalCalendar(Samples.Kyiv));

        Assert.Equal(800, hours[12].Average);
    }

    [Fact]
    public void Worst_window_wraps_past_midnight()
    {
        var hours = Day((22, 1500), (23, 1400), (0, 1300), (12, 500), (13, 500), (14, 500));

        Assert.Equal(22, TypicalDayCalculator.WorstWindowStartHour(hours));
    }

    [Fact]
    public void Worst_window_picks_the_earliest_start_on_a_tie()
    {
        var hours = Day((3, 900), (4, 900), (5, 900), (15, 900), (16, 900), (17, 900));

        Assert.Equal(3, TypicalDayCalculator.WorstWindowStartHour(hours));
    }

    [Fact]
    public void Worst_window_is_null_with_fewer_than_three_hours_of_data()
    {
        var hours = Day((8, 900), (9, 900));

        Assert.Null(TypicalDayCalculator.WorstWindowStartHour(hours));
    }

    [Fact]
    public void The_worst_window_has_its_average()
    {
        var hours = Day((22, 1500), (23, 1400), (0, 1300), (12, 500));

        var window = TypicalDayCalculator.WorstWindow(hours);

        Assert.Equal(new HourWindow(22, 1400), window);
    }

    [Fact]
    public void No_worst_window_means_no_average_either()
    {
        Assert.Null(TypicalDayCalculator.WorstWindow(Day((8, 900))));
    }
}

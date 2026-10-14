using System.Windows;
using Deskair.App.Ui.Charts;
using Deskair.App.Ui.Charts.Layers;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Ui;

public class ChartTicksTests
{
    [Fact]
    public void Even_spaces_values_with_both_ends_included()
    {
        Assert.Equal([400.0, 1200.0, 2000.0], ChartTicks.Even(400, 2000, 3));
        Assert.Equal([400.0], ChartTicks.Even(400, 2000, 1));
        Assert.Empty(ChartTicks.Even(400, 2000, 0));
    }

    [Theory]
    [InlineData(19.5, 26.5, 5, new[] { 20.0, 22.0, 24.0, 26.0 })]
    [InlineData(28.0, 56.0, 5, new[] { 30.0, 40.0, 50.0 })]
    [InlineData(974.0, 976.2, 5, new[] { 974.0, 974.5, 975.0, 975.5, 976.0 })]
    public void Nice_gives_round_values_inside_the_range(double min, double max, int count, double[] expected)
    {
        Assert.Equal(expected, ChartTicks.Nice(min, max, count));
    }

    [Fact]
    public void Nice_never_gives_more_than_the_maximum()
    {
        Assert.True(ChartTicks.Nice(0, 1000, 5).Count <= 5);
        Assert.Empty(ChartTicks.Nice(10, 10, 5));
    }

    private static ChartArea TimeArea(DateTimeOffset start, TimeSpan span, double width)
        => ChartArea.Create(new Size(width + 50, 100), new Thickness(40, 0, 10, 0), start.UtcTicks, (start + span).UtcTicks, 0, 1);

    [Fact]
    public void An_hour_gets_ten_minute_labels_on_round_local_times()
    {
        var start = new DateTimeOffset(2026, 10, 12, 19, 33, 0, TimeSpan.Zero);
        var layer = new AxisLabelLayer { TimeZone = TimeZoneInfo.Utc };

        var ticks = layer.TimeTicks(TimeArea(start, TimeSpan.FromHours(1), 600)).ToList();

        Assert.Equal(new DateTimeOffset(2026, 10, 12, 19, 40, 0, TimeSpan.Zero), ticks[0]);
        Assert.All(ticks, tick => Assert.Equal(0, tick.Minute % 10));
        Assert.InRange(ticks.Count, 3, 6);
    }

    [Fact]
    public void A_day_gets_labels_a_few_hours_apart()
    {
        var start = new DateTimeOffset(2026, 10, 12, 8, 17, 0, TimeSpan.Zero);
        var layer = new AxisLabelLayer { TimeZone = TimeZoneInfo.Utc };

        var ticks = layer.TimeTicks(TimeArea(start, TimeSpan.FromHours(24), 600)).ToList();

        Assert.All(ticks, tick => Assert.Equal(0, tick.Minute));
        Assert.True((ticks[1] - ticks[0]).TotalHours >= 2);
        Assert.InRange(ticks.Count, 3, 9);
    }

    [Fact]
    public void Round_times_are_in_the_local_time_zone_not_utc()
    {
        var kyiv = Samples.Kyiv;
        var start = new DateTimeOffset(2026, 10, 12, 8, 3, 0, TimeSpan.Zero);
        var layer = new AxisLabelLayer { TimeZone = kyiv };

        var first = layer.TimeTicks(TimeArea(start, TimeSpan.FromHours(1), 600)).First();
        var local = TimeZoneInfo.ConvertTime(first, kyiv);

        Assert.Equal(0, local.Minute % 10);
        Assert.Equal(11, local.Hour);
    }

    [Fact]
    public void A_narrow_plot_gets_fewer_labels()
    {
        var start = new DateTimeOffset(2026, 10, 12, 8, 0, 0, TimeSpan.Zero);
        var layer = new AxisLabelLayer { TimeZone = TimeZoneInfo.Utc };

        var wide = layer.TimeTicks(TimeArea(start, TimeSpan.FromHours(1), 800)).Count();
        var narrow = layer.TimeTicks(TimeArea(start, TimeSpan.FromHours(1), 200)).Count();

        Assert.True(narrow < wide);
    }

    [Theory]
    // The last hour in the screenshots: 998 to 1151 spans 153, so steps of 100 from 900 to 1200 (behaviour-rules.md "Live values").
    [InlineData(998, 1151, 900, 1200, 100)]
    // A wide range (over 300) steps by 200.
    [InlineData(712, 1080, 600, 1200, 200)]
    [InlineData(1369, 1620, 1300, 1700, 100)]
    // A flat line is still at least 200 tall.
    [InlineData(1040, 1050, 1000, 1200, 100)]
    // Never below zero.
    [InlineData(10, 40, 0, 200, 100)]
    public void The_co2_axis_is_fitted_to_the_readings(double low, double high, double min, double max, double step)
    {
        Assert.Equal((min, max, step), ChartTicks.FitCo2(low, high));
    }

    [Fact]
    public void Steps_lists_every_multiple_from_the_minimum_to_the_maximum()
    {
        Assert.Equal([900.0, 1000, 1100, 1200], ChartTicks.Steps(900, 1200, 100));
        Assert.Empty(ChartTicks.Steps(900, 1200, 0));
    }
}

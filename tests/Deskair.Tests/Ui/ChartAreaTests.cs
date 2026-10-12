using System.Windows;
using Deskair.App.Ui.Charts;

namespace Deskair.Tests.Ui;

public class ChartAreaTests
{
    private static readonly Thickness margin = new(40, 10, 20, 30);

    private static ChartArea Create(double xMin = 0, double xMax = 100, double yMin = 400, double yMax = 2000)
        => ChartArea.Create(new Size(400, 200), margin, xMin, xMax, yMin, yMax);

    [Fact]
    public void The_plot_leaves_room_for_the_margins()
    {
        var area = Create();

        Assert.Equal(new Rect(40, 10, 340, 160), area.Plot);
    }

    [Fact]
    public void X_maps_the_range_onto_the_plot_width()
    {
        var area = Create();

        Assert.Equal(40, area.X(0));
        Assert.Equal(210, area.X(50));
        Assert.Equal(380, area.X(100));
    }

    [Fact]
    public void Y_grows_upward()
    {
        var area = Create();

        Assert.Equal(170, area.Y(400));
        Assert.Equal(90, area.Y(1200));
        Assert.Equal(10, area.Y(2000));
        Assert.True(area.Y(1500) < area.Y(1000));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37.5)]
    [InlineData(100)]
    public void X_round_trips(double value)
    {
        var area = Create();

        Assert.Equal(value, area.DataX(area.X(value)), 9);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(1068)]
    [InlineData(2000)]
    public void Y_round_trips(double value)
    {
        var area = Create();

        Assert.Equal(value, area.DataY(area.Y(value)), 9);
    }

    [Fact]
    public void Times_map_through_utc_ticks_and_round_trip()
    {
        var start = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);
        var area = Create(start.UtcTicks, end.UtcTicks);
        var middle = start.AddMinutes(30);

        Assert.Equal(210, area.X(middle), 6);
        Assert.Equal(middle, area.TimeAt(area.X(middle)));
    }

    [Fact]
    public void A_flat_range_is_widened_so_the_line_is_not_on_the_border()
    {
        var area = Create(yMin: 22, yMax: 22);

        Assert.True(area.YMax > area.YMin);
        Assert.InRange(area.Y(22), area.Plot.Top + 1, area.Plot.Bottom - 1);
    }

    [Fact]
    public void A_flat_range_at_zero_is_still_widened()
    {
        var area = Create(yMin: 0, yMax: 0);

        Assert.Equal(-1, area.YMin);
        Assert.Equal(1, area.YMax);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 20)]
    [InlineData(400, 40)]
    public void A_size_smaller_than_the_margins_gives_an_empty_plot(double width, double height)
    {
        var area = ChartArea.Create(new Size(width, height), margin, 0, 1, 0, 1);

        Assert.True(area.IsEmpty);
        Assert.True(area.Plot.Width >= 0);
        Assert.True(area.Plot.Height >= 0);
    }

    [Fact]
    public void An_unknown_brush_key_is_transparent()
    {
        var area = Create();

        Assert.Same(System.Windows.Media.Brushes.Transparent, area.Brush("No.Such.Key"));
    }

    [Theory]
    [InlineData(1.0, 10.3, 10.5)]
    [InlineData(2.0, 10.3, 10.25)]
    [InlineData(1.5, 10.6, 10.333333)]
    public void Snap_puts_a_line_in_the_middle_of_a_device_pixel(double dip, double coordinate, double expected)
    {
        var area = Create() with { PixelsPerDip = dip };

        Assert.Equal(expected, area.Snap(coordinate), 4);
    }
}

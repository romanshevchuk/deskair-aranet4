using System.Windows;
using Deskair.App.Ui.Charts;
using Deskair.App.ViewModels.ChartModels;

namespace Deskair.Tests.Ui;

public class ChartHitTestTests
{
    private static readonly DateTimeOffset start = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    // 60 minutes over a 600 px plot (10 px per minute), plot left = 0.
    private static readonly ChartArea area = ChartArea.Create(new Size(600, 100), new Thickness(0), start.UtcTicks, start.AddMinutes(60).UtcTicks, 0, 100);

    private static ChartPoint At(int minute, double? value) => new(start.AddMinutes(minute), value);

    [Fact]
    public void The_nearest_real_point_is_reported_with_its_pixel_position()
    {
        var points = new[] { At(0, 10), At(10, 50), At(20, 90) };

        var hover = ChartHitTest.NearestPoint(points, area, mouseX: 110);

        Assert.NotNull(hover);
        Assert.Equal(50, hover.Value);
        Assert.Equal(100, hover.X, 6);
        Assert.Equal(50, hover.Y!.Value, 6);
        Assert.Equal(start.AddMinutes(10), hover.Time);
    }

    [Theory]
    [InlineData(40, 10)]
    [InlineData(60, 50)]
    [InlineData(149, 50)]
    [InlineData(151, 90)]
    public void The_closer_of_the_two_neighbours_wins(double mouseX, double expected)
    {
        var points = new[] { At(0, 10), At(10, 50), At(20, 90) };
        var hover = ChartHitTest.NearestPoint(points, area, mouseX);

        Assert.Equal(expected, hover!.Value);
    }

    [Fact]
    public void Inside_a_gap_the_hover_says_no_data_not_the_nearest_value()
    {
        // 700 at minute 0 and 2, a gap marker at minute 4, the next real reading at minute 30.
        var points = new[] { At(0, 700), At(2, 710), At(4, null), At(30, 900) };

        var inGap = ChartHitTest.NearestPoint(points, area, mouseX: 200);
        var beforeGap = ChartHitTest.NearestPoint(points, area, mouseX: 25);

        Assert.NotNull(inGap);
        Assert.Null(inGap.Value);
        Assert.Null(inGap.Y);
        Assert.Equal(710, beforeGap!.Value);
    }

    [Fact]
    public void After_the_last_gap_marker_the_next_reading_is_found_again()
    {
        var points = new[] { At(0, 700), At(4, null), At(30, 900), At(32, 910) };

        var hover = ChartHitTest.NearestPoint(points, area, mouseX: 305);

        Assert.Equal(900, hover!.Value);
    }

    [Fact]
    public void Before_the_first_point_reports_the_first_point()
    {
        var points = new[] { At(10, 50), At(20, 60) };

        var hover = ChartHitTest.NearestPoint(points, area, mouseX: 5);

        Assert.Equal(50, hover!.Value);
    }

    [Fact]
    public void After_the_last_point_reports_the_last_point()
    {
        var points = new[] { At(10, 50), At(20, 60) };

        var hover = ChartHitTest.NearestPoint(points, area, mouseX: 500);

        Assert.Equal(60, hover!.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(601)]
    public void Outside_the_plot_there_is_no_hover(double mouseX)
    {
        var points = new[] { At(0, 10), At(30, 20) };

        Assert.Null(ChartHitTest.NearestPoint(points, area, mouseX));
    }

    [Fact]
    public void No_points_means_no_hover()
    {
        Assert.Null(ChartHitTest.NearestPoint(null, area, 100));
        Assert.Null(ChartHitTest.NearestPoint([], area, 100));
    }

    [Fact]
    public void An_empty_plot_means_no_hover()
    {
        var empty = ChartArea.Create(new Size(10, 10), new Thickness(20), 0, 1, 0, 1);

        Assert.Null(ChartHitTest.NearestPoint([At(0, 1)], empty, 5));
    }
}

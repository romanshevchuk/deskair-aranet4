using System.Windows;
using System.Windows.Controls;
using Deskair.App.Ui.Controls;

namespace Deskair.Tests.Ui;

public class GaugeTests
{
    private static double Star(Gauge gauge, string part)
        => ((ColumnDefinition)gauge.Template.FindName(part, gauge)).Width.Value;

    private const double Width = 400;

    private static Gauge Create(double? value)
    {
        var gauge = UiHost.Prepare(new Gauge { Value = value, Width = Width });
        gauge.Measure(new Size(Width, 100));
        gauge.Arrange(new Rect(0, 0, Width, gauge.DesiredSize.Height));
        gauge.UpdateLayout();

        return gauge;
    }

    private static double MarkerCentre(Gauge gauge)
    {
        var dot = (FrameworkElement)gauge.Template.FindName("PART_MarkerDot", gauge);

        return Canvas.GetLeft(dot) + (dot.Width / 2);
    }

    [Fact]
    public void Segments_are_proportional_to_the_scale()
    {
        UiHost.Run(() =>
        {
            var gauge = Create(1000);

            Assert.Equal(0.375, Star(gauge, "PART_Good"), 6);
            Assert.Equal(0.25, Star(gauge, "PART_Elevated"), 6);
            Assert.Equal(0.375, Star(gauge, "PART_High"), 6);
        });
    }

    [Theory]
    [InlineData(400, 0.0)]
    [InlineData(1000, 0.375)]
    [InlineData(1200, 0.5)]
    [InlineData(2000, 1.0)]
    [InlineData(100, 0.0)]
    [InlineData(3000, 1.0)]
    public void The_marker_sits_at_the_value_and_clamps_to_the_ends(double value, double fraction)
    {
        UiHost.Run(() =>
        {
            var gauge = Create(value);

            Assert.Equal(fraction * Width, MarkerCentre(gauge), 3);
        });
    }

    [Fact]
    public void A_changed_value_moves_the_marker()
    {
        UiHost.Run(() =>
        {
            var gauge = Create(400);

            gauge.Value = 2000;

            Assert.Equal(Width, MarkerCentre(gauge), 3);
        });
    }

    [Fact]
    public void No_value_hides_the_marker()
    {
        UiHost.Run(() =>
        {
            var gauge = Create(null);
            var marker = (FrameworkElement)gauge.Template.FindName("PART_Marker", gauge);

            Assert.Equal(Visibility.Collapsed, marker.Visibility);
        });
    }

    [Fact]
    public void A_value_shows_the_marker()
    {
        UiHost.Run(() =>
        {
            var gauge = Create(900);
            var marker = (FrameworkElement)gauge.Template.FindName("PART_Marker", gauge);

            Assert.Equal(Visibility.Visible, marker.Visibility);
        });
    }

    [Fact]
    public void The_marker_is_not_clipped_at_the_ends_of_the_scale()
    {
        UiHost.Run(() =>
        {
            var gauge = Create(100);
            var marker = (Canvas)gauge.Template.FindName("PART_Marker", gauge);

            Assert.False(marker.ClipToBounds);
            Assert.Equal(0, MarkerCentre(gauge), 3);
        });
    }

    [Fact]
    public void The_zone_limits_move_the_segments()
    {
        UiHost.Run(() =>
        {
            var gauge = UiHost.Prepare(new Gauge { Value = 800, ElevatedFrom = 800, HighAbove = 1200 });

            Assert.Equal(0.25, Star(gauge, "PART_Good"), 6);
            Assert.Equal(0.25, Star(gauge, "PART_Elevated"), 6);
            Assert.Equal(0.5, Star(gauge, "PART_High"), 6);
        });
    }
}

using System.Windows;
using Deskair.App.Platform.Tray;

namespace Deskair.Tests.Platform;

public class FlyoutPositionerTests
{
    private const double Gap = FlyoutPositioner.Gap;

    // A 1920 x 1080 screen with a 48 px taskbar; the work area is what the taskbar leaves.
    private static readonly Size flyout = new(360, 500);

    private static readonly Rect workAreaBottom = new(0, 0, 1920, 1032);
    private static readonly Rect workAreaTop = new(0, 48, 1920, 1032);
    private static readonly Rect workAreaLeft = new(48, 0, 1872, 1080);
    private static readonly Rect workAreaRight = new(0, 0, 1872, 1080);

    private static Rect Icon(double x, double y) => new(x, y, 24, 24);

    [Fact]
    public void A_taskbar_at_the_bottom_puts_the_flyout_above_the_icon_centred_on_it()
    {
        var position = FlyoutPositioner.Position(Icon(1000, 1044), flyout, workAreaBottom);

        Assert.Equal(1012 - 180, position.X);
        Assert.Equal(1032 - 500 - Gap, position.Y);
    }

    [Fact]
    public void A_taskbar_at_the_top_puts_the_flyout_below_it()
    {
        var position = FlyoutPositioner.Position(Icon(1000, 12), flyout, workAreaTop);

        Assert.Equal(1012 - 180, position.X);
        Assert.Equal(48 + Gap, position.Y);
    }

    [Fact]
    public void A_taskbar_at_the_left_puts_the_flyout_to_its_right_centred_on_the_icon()
    {
        var position = FlyoutPositioner.Position(Icon(12, 600), flyout, workAreaLeft);

        Assert.Equal(48 + Gap, position.X);
        Assert.Equal(612 - 250, position.Y);
    }

    [Fact]
    public void A_taskbar_at_the_right_puts_the_flyout_to_its_left()
    {
        var position = FlyoutPositioner.Position(Icon(1884, 600), flyout, workAreaRight);

        Assert.Equal(1872 - 360 - Gap, position.X);
        Assert.Equal(612 - 250, position.Y);
    }

    [Fact]
    public void An_icon_near_the_right_corner_keeps_the_flyout_inside_the_screen()
    {
        var position = FlyoutPositioner.Position(Icon(1880, 1044), flyout, workAreaBottom);

        Assert.Equal(1920 - 360, position.X);
    }

    [Fact]
    public void An_icon_near_the_left_corner_keeps_the_flyout_inside_the_screen()
    {
        var position = FlyoutPositioner.Position(Icon(4, 1044), flyout, workAreaBottom);

        Assert.Equal(0, position.X);
    }

    [Fact]
    public void An_icon_near_the_top_of_a_side_taskbar_keeps_the_flyout_inside()
    {
        var position = FlyoutPositioner.Position(Icon(12, 4), flyout, workAreaLeft);

        Assert.Equal(0, position.Y);
    }

    [Fact]
    public void An_icon_near_the_bottom_of_a_side_taskbar_keeps_the_flyout_inside()
    {
        var position = FlyoutPositioner.Position(Icon(1884, 1070), flyout, workAreaRight);

        Assert.Equal(1080 - 500, position.Y);
    }

    [Fact]
    public void A_flyout_taller_than_the_work_area_is_pinned_to_its_top()
    {
        var tall = new Size(360, 2000);

        var position = FlyoutPositioner.Position(Icon(1000, 1044), tall, workAreaBottom);

        Assert.Equal(0, position.Y);
    }

    [Fact]
    public void A_flyout_wider_than_the_work_area_is_pinned_to_its_left()
    {
        var wide = new Size(3000, 500);

        var position = FlyoutPositioner.Position(Icon(1000, 1044), wide, workAreaBottom);

        Assert.Equal(0, position.X);
    }

    [Fact]
    public void A_work_area_that_does_not_start_at_zero_is_respected_on_a_second_monitor()
    {
        var secondMonitor = new Rect(1920, 0, 1920, 1032);

        var position = FlyoutPositioner.Position(new Rect(3800, 1044, 24, 24), flyout, secondMonitor);

        Assert.Equal(1920 + 1920 - 360, position.X);
        Assert.Equal(1032 - 500 - Gap, position.Y);
    }

    [Fact]
    public void An_icon_inside_the_work_area_such_as_in_the_overflow_popup_gets_the_flyout_above_it()
    {
        var position = FlyoutPositioner.Position(Icon(1700, 900), flyout, workAreaBottom);

        Assert.Equal(900 - 500 - Gap, position.Y);
        Assert.Equal(1712 - 180, position.X);
    }

    [Fact]
    public void An_icon_inside_the_work_area_near_its_top_gets_the_flyout_below_it()
    {
        var position = FlyoutPositioner.Position(Icon(1700, 100), flyout, workAreaBottom);

        Assert.Equal(100 + 24 + Gap, position.Y);
    }

    [Fact]
    public void The_flyout_never_leaves_the_work_area()
    {
        var areas = new[] { workAreaBottom, workAreaTop, workAreaLeft, workAreaRight };
        var icons = new[] { Icon(0, 0), Icon(1896, 0), Icon(0, 1056), Icon(1896, 1056), Icon(960, 1044), Icon(12, 540), Icon(1884, 540) };

        foreach (var area in areas)
        {
            foreach (var icon in icons)
            {
                var position = FlyoutPositioner.Position(icon, flyout, area);

                Assert.True(position.X >= area.Left && position.X + flyout.Width <= area.Right, $"x for {icon} in {area}");
                Assert.True(position.Y >= area.Top && position.Y + flyout.Height <= area.Bottom, $"y for {icon} in {area}");
            }
        }
    }
}

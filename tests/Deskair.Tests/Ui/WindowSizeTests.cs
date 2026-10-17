using System.Windows;

namespace Deskair.Tests.Ui;

public class WindowSizeTests
{
    [Fact]
    public void The_smallest_window_is_below_the_design_size_and_above_what_the_layouts_need()
    {
        var (minWidth, minHeight) = UiHost.Run(() => ((double)Application.Current.FindResource("Size.Window.MinWidth"), (double)Application.Current.FindResource("Size.Window.MinHeight")));

        Assert.InRange(minWidth, 1100, 1200);
        Assert.InRange(minHeight, 700, 728);
    }
}

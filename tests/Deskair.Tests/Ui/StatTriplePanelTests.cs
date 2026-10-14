using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Deskair.App.Ui.Controls;

namespace Deskair.Tests.Ui;

public class StatTriplePanelTests
{
    private static List<Rect> Arrange(int count, double width)
    {
        var children = Enumerable.Range(0, count).Select(_ => new Border { Height = 30 }).ToList();
        var panel = new StatTriplePanel { Gap = 16 };
        children.ForEach(child => panel.Children.Add(child));

        panel.Measure(new Size(width, 100));
        panel.Arrange(new Rect(0, 0, width, 100));

        return children.Select(child => LayoutInformation.GetLayoutSlot(child)).ToList();
    }

    [Fact]
    public void Three_stats_get_equal_columns_with_room_beside_each_divider()
    {
        var slots = UiHost.Run(() => Arrange(3, 300));

        // Columns of 100: the first ends 8 before the first divider, the others start 16 after their divider; the last runs to the edge.
        Assert.Equal(new Rect(0, 0, 92, 100), slots[0]);
        Assert.Equal(new Rect(116, 0, 76, 100), slots[1]);
        Assert.Equal(new Rect(216, 0, 84, 100), slots[2]);
    }

    [Fact]
    public void Two_stats_split_the_width_in_two()
    {
        var slots = UiHost.Run(() => Arrange(2, 200));

        Assert.Equal(new Rect(0, 0, 92, 100), slots[0]);
        Assert.Equal(new Rect(116, 0, 84, 100), slots[1]);
    }

    [Fact]
    public void The_panel_is_as_tall_as_its_tallest_stat()
    {
        var height = UiHost.Run(() =>
        {
            var panel = new StatTriplePanel();
            panel.Children.Add(new Border { Height = 20 });
            panel.Children.Add(new Border { Height = 34 });
            panel.Measure(new Size(300, double.PositiveInfinity));

            return panel.DesiredSize.Height;
        });

        Assert.Equal(34, height);
    }
}

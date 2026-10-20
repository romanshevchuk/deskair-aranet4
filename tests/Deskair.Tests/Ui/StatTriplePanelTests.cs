using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Deskair.App.Ui.Controls;

namespace Deskair.Tests.Ui;

/// <summary>How the hero's three stats share their width: equal when they fit, more room for a long value, equal again when nothing fits.</summary>
public class StatTriplePanelTests
{
    [Fact]
    public void Columns_are_equal_when_every_value_fits_its_share()
    {
        var widths = StatTriplePanel.Allocate(324, [70, 90, 100]);

        Assert.All(widths, width => Assert.Equal(108, width, 0.01));
    }

    [Fact]
    public void A_value_that_needs_more_than_its_share_takes_what_it_needs_and_the_others_share_the_rest()
    {
        var widths = StatTriplePanel.Allocate(324, [60, 90, 130]);

        Assert.Equal(130, widths[2], 0.01);
        Assert.Equal(97, widths[0], 0.01);
        Assert.Equal(97, widths[1], 0.01);
        Assert.Equal(324, widths.Sum(), 0.01);
    }

    [Fact]
    public void Two_long_values_can_both_take_more_than_an_equal_share()
    {
        var widths = StatTriplePanel.Allocate(324, [60, 120, 130]);

        Assert.Equal(120, widths[1], 0.01);
        Assert.Equal(130, widths[2], 0.01);
        Assert.Equal(74, widths[0], 0.01);
    }

    [Fact]
    public void When_the_values_cannot_all_fit_the_columns_are_equal()
    {
        var widths = StatTriplePanel.Allocate(300, [150, 150, 150]);

        Assert.All(widths, width => Assert.Equal(100, width, 0.01));
    }

    [Fact]
    public void The_dividers_are_drawn_again_when_a_value_changes_and_moves_the_columns()
    {
        UiHost.Run(() =>
        {
            var middle = new Border { Width = 40 };
            var panel = new CountingPanel { DividerBrush = Brushes.Black, Children = { new Border { Width = 40 }, middle, new Border { Width = 40 } } };
            var host = new Border { Width = 300, Child = panel };

            Show(host);
            var before = panel.Renders;

            middle.Width = 150;
            Show(host);

            Assert.True(panel.Renders > before);
        });
    }

    private static void Show(FrameworkElement host)
    {
        host.Measure(new Size(300, 100));
        host.Arrange(new Rect(0, 0, 300, 100));
        host.UpdateLayout();
        new RenderTargetBitmap(300, 100, 96, 96, PixelFormats.Pbgra32).Render(host);
    }

    private sealed class CountingPanel : StatTriplePanel
    {
        public int Renders { get; private set; }

        protected override void OnRender(DrawingContext dc)
        {
            Renders++;
            base.OnRender(dc);
        }
    }
}

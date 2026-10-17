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
}

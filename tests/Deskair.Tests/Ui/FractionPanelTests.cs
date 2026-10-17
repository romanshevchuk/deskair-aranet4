using System.Windows;
using System.Windows.Controls;
using Deskair.App.Ui.Controls;

namespace Deskair.Tests.Ui;

public class FractionPanelTests
{
    [Theory]
    [InlineData(0.5, 40, 300, 130)]
    [InlineData(0.0, 40, 300, 0)]
    [InlineData(1.0, 40, 300, 260)]
    [InlineData(0.25, 40, 300, 55)]
    public void A_child_is_centred_on_its_fraction_and_kept_inside_the_panel(double position, double width, double panelWidth, double expected)
        => Assert.Equal(expected, FractionPanel.Left(position, width, panelWidth), 6);

    [Fact]
    public void Children_are_arranged_at_their_positions()
    {
        UiHost.Run(() =>
        {
            var panel = new FractionPanel();
            var middle = new Border { Width = 20, Height = 10 };
            var end = new Border { Width = 30, Height = 10 };
            FractionPanel.SetPosition(middle, 0.5);
            FractionPanel.SetPosition(end, 1);
            panel.Children.Add(middle);
            panel.Children.Add(end);

            panel.Measure(new Size(200, 50));
            panel.Arrange(new Rect(0, 0, 200, 50));

            Assert.Equal(90, middle.TranslatePoint(new Point(0, 0), panel).X, 0.5);
            Assert.Equal(170, end.TranslatePoint(new Point(0, 0), panel).X, 0.5);
        });
    }
}

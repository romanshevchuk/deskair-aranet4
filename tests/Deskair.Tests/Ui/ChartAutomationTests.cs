using System.Windows.Automation;
using System.Windows.Automation.Peers;
using Deskair.App.Ui.Charts;

namespace Deskair.Tests.Ui;

/// <summary>Charts are drawn, not controls: they need their own automation peer to be read by Narrator (accessibility.md "Screen readers").</summary>
public class ChartAutomationTests
{
    [Fact]
    public void A_named_chart_is_an_image_with_that_name()
    {
        UiHost.Run(() =>
        {
            var chart = new ZoneRibbon();
            AutomationProperties.SetName(chart, "Last 24 hours by air quality zone");

            var peer = UIElementAutomationPeer.CreatePeerForElement(chart);

            Assert.NotNull(peer);
            Assert.Equal("Last 24 hours by air quality zone", peer.GetName());
            Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
            Assert.True(peer.IsControlElement());
        });
    }

    [Fact]
    public void A_chart_without_a_name_is_left_out_of_the_tree()
    {
        UiHost.Run(() =>
        {
            var peer = UIElementAutomationPeer.CreatePeerForElement(new LiveChart());

            Assert.False(peer.IsControlElement());
        });
    }
}

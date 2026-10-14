using System.Xml.Linq;
using Deskair.App.Platform.Notifications;

namespace Deskair.Tests.Platform;

public class ToastXmlBuilderTests
{
    private static readonly Uri logo = new("file:///C:/Users/Test/AppData/Local/Deskair/toast/ring-high.png");

    private static XElement Build(string title = "CO₂ is elevated · Office", string body = "1068 ppm. Open a window.", string tag = "A1")
        => XElement.Parse(ToastXmlBuilder.Build(new ToastText(title, body, "Snooze 1 h", "Dismiss"), logo, tag));

    [Fact]
    public void The_toast_has_the_title_and_the_body_as_text()
    {
        var texts = Build().Descendants("text").Select(text => text.Value).ToList();

        Assert.Equal(["CO₂ is elevated · Office", "1068 ppm. Open a window."], texts);
    }

    [Fact]
    public void The_zone_ring_is_the_app_logo()
    {
        var image = Build().Descendants("image").Single();

        Assert.Equal("appLogoOverride", image.Attribute("placement")?.Value);
        Assert.Equal(logo.AbsoluteUri, image.Attribute("src")?.Value);
    }

    [Fact]
    public void Clicking_the_toast_opens_the_app()
    {
        var toast = Build();

        Assert.Equal(ToastArguments.Open, toast.Attribute("launch")?.Value);
        Assert.Equal("foreground", toast.Attribute("activationType")?.Value);
    }

    [Fact]
    public void Snooze_goes_to_the_app_and_Dismiss_is_handled_by_Windows()
    {
        var actions = Build().Descendants("action").ToList();

        Assert.Equal(2, actions.Count);
        Assert.Equal(("Snooze 1 h", ToastArguments.Snooze, "foreground"), (actions[0].Attribute("content")?.Value, actions[0].Attribute("arguments")?.Value, actions[0].Attribute("activationType")?.Value));
        Assert.Equal(("Dismiss", "system"), (actions[1].Attribute("content")?.Value, actions[1].Attribute("activationType")?.Value));
        Assert.Equal("dismiss", actions[1].Attribute("arguments")?.Value);
    }

    [Fact]
    public void Characters_that_mean_something_in_xml_are_escaped()
    {
        var toast = Build(title: "CO₂ · Kids & <room> \"A\"", body: "x < y");

        Assert.Equal("CO₂ · Kids & <room> \"A\"", toast.Descendants("text").First().Value);
        Assert.Equal("x < y", toast.Descendants("text").Last().Value);
    }

    [Theory]
    [InlineData("action=snooze", ToastAction.Snooze)]
    [InlineData("action=open", ToastAction.Open)]
    public void Arguments_from_a_click_are_understood(string arguments, ToastAction expected)
    {
        Assert.Equal(expected, ToastArguments.Parse(arguments));
    }

    [Theory]
    [InlineData("")]
    [InlineData("action=unknown")]
    [InlineData("something else")]
    public void Arguments_nobody_sent_are_ignored(string arguments)
    {
        Assert.Null(ToastArguments.Parse(arguments));
    }
}

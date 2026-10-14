using Deskair.App.Platform.Notifications;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Services.Alerts;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Platform;

public class ToastTextBuilderTests
{
    private static readonly DeviceId device = new("aranet4", "AA:01");
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 13, 12);

    private readonly ToastTextBuilder builder;

    public ToastTextBuilderTests()
    {
        var localizer = new FakeLocalizer();
        builder = new ToastTextBuilder(localizer, TestFormatter.Create(localizer));
    }

    [Fact]
    public void An_elevated_alert_names_the_room_and_gives_the_value_in_its_body()
    {
        var text = builder.Build(new AlertNotification(device, "Office", Zone.Elevated, 1068, now));

        Assert.Equal("Toast_Elevated_Title(room=Office)", text.Title);
        Assert.Equal("Toast_Elevated_Body(value=1068 ppm)", text.Body);
    }

    [Fact]
    public void A_high_alert_uses_its_own_title_and_body()
    {
        var text = builder.Build(new AlertNotification(device, "Bedroom", Zone.High, 1732, now));

        Assert.Equal("Toast_High_Title(room=Bedroom)", text.Title);
        Assert.Equal("Toast_High_Body(value=1732 ppm)", text.Body);
    }

    [Fact]
    public void The_buttons_have_their_words()
    {
        var text = builder.Build(new AlertNotification(device, "Office", Zone.Elevated, 1068, now));

        Assert.Equal(("Toast_Snooze", "Toast_Dismiss"), (text.SnoozeButton, text.DismissButton));
    }
}

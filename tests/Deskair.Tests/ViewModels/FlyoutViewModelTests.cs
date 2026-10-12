using Deskair.App.ViewModels.Flyout;
using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class FlyoutViewModelTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);

    private readonly FlyoutViewModel viewModel = new(new FakeLocalizer());

    private static List<Reading> History(double earlierPpm, double recentPpm)
    {
        var readings = new List<Reading>();

        for (var minute = 36; minute >= 30; minute -= 2)
        {
            readings.Add(Samples.Co2(now.AddMinutes(-minute), earlierPpm));
        }

        for (var minute = 6; minute >= 0; minute -= 2)
        {
            readings.Add(Samples.Co2(now.AddMinutes(-minute), recentPpm));
        }

        return readings;
    }

    [Fact]
    public void A_reading_gives_zone_value_room_and_advice()
    {
        var recent = History(1300, 1050);

        viewModel.Update(recent[^1], recent, now, "Office");

        Assert.Equal(Zone.Elevated, viewModel.Zone);
        Assert.Equal(1050, viewModel.Co2Ppm);
        Assert.Equal("Office", viewModel.RoomName);
        Assert.Equal(recent[^1].Timestamp, viewModel.UpdatedAt);
        Assert.Equal(TrendDirection.Falling, viewModel.Trend);
        Assert.Equal("Advice_Elevated_Title", viewModel.AdviceTitle);
        Assert.Equal("Advice_Elevated_Text_Falling", viewModel.AdviceText);
        Assert.False(viewModel.ShowNotConnected);
    }

    [Fact]
    public void The_advice_is_the_same_as_the_hero_gives()
    {
        var recent = History(1050, 1900);
        var hero = TestHero.Create();

        viewModel.Update(recent[^1], recent, now, "Office");
        hero.Update(recent[^1], recent, now);

        Assert.Equal(hero.AdviceTitle, viewModel.AdviceTitle);
        Assert.Equal(hero.AdviceText, viewModel.AdviceText);
        Assert.Equal(hero.Zone, viewModel.Zone);
    }

    [Fact]
    public void No_reading_shows_the_not_connected_state_but_keeps_the_room()
    {
        var recent = History(1300, 1050);
        viewModel.Update(recent[^1], recent, now, "Office");

        viewModel.Update(null, [], now, "Office");

        Assert.True(viewModel.ShowNotConnected);
        Assert.Equal("Office", viewModel.RoomName);
        Assert.Null(viewModel.Zone);
        Assert.Null(viewModel.Co2Ppm);
        Assert.Null(viewModel.UpdatedAt);
        Assert.Null(viewModel.AdviceTitle);
        Assert.Null(viewModel.AdviceText);
    }

    [Fact]
    public void A_new_reading_clears_the_not_connected_state()
    {
        viewModel.Update(null, [], now, "Office");
        var recent = History(600, 600);

        viewModel.Update(recent[^1], recent, now, "Office");

        Assert.False(viewModel.ShowNotConnected);
    }
}

using Deskair.App.Localization;
using Deskair.App.ViewModels.Live;
using Deskair.Domain.Analytics.Trend;
using Deskair.Domain.Readings;
using Deskair.Domain.Zones;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class HeroViewModelTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);

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

    private static HeroViewModel Update(double earlierPpm, double recentPpm)
    {
        var viewModel = TestHero.Create();
        var recent = History(earlierPpm, recentPpm);

        viewModel.Update(recent[^1], recent, now);

        return viewModel;
    }

    [Theory]
    [InlineData(600, Zone.Good)]
    [InlineData(1000, Zone.Elevated)]
    [InlineData(1400, Zone.Elevated)]
    [InlineData(1401, Zone.High)]
    public void Zone_comes_from_the_classifier(double ppm, Zone expected)
    {
        var viewModel = Update(ppm, ppm);

        Assert.Equal(expected, viewModel.Zone);
        Assert.Equal(ppm, viewModel.Co2Ppm);
    }

    [Fact]
    public void Falling_trend_has_text_and_the_observed_change()
    {
        var viewModel = Update(1300, 1050);

        Assert.Equal(TrendDirection.Falling, viewModel.Trend);
        Assert.Equal("Trend_Falling", viewModel.TrendText);
        Assert.Equal("Trend_Detail(ppm=250, min=30)", viewModel.TrendDetail);
    }

    [Fact]
    public void Steady_trend_has_no_detail()
    {
        var viewModel = Update(1100, 1110);

        Assert.Equal(TrendDirection.Steady, viewModel.Trend);
        Assert.Equal("Trend_Steady", viewModel.TrendText);
        Assert.Null(viewModel.TrendDetail);
    }

    [Theory]
    [InlineData(600, 600, "Advice_Good_Title", "Advice_Good_Text")]
    [InlineData(1300, 1050, "Advice_Elevated_Title", "Advice_Elevated_Text_Falling")]
    [InlineData(1050, 1070, "Advice_Elevated_Title", "Advice_Elevated_Text_Steady")]
    [InlineData(1050, 1300, "Advice_Elevated_Title", "Advice_Elevated_Text_Rising")]
    [InlineData(1900, 1500, "Advice_High_Title", "Advice_High_Text_Falling")]
    [InlineData(1500, 1900, "Advice_High_Title", "Advice_High_Text_Rising")]
    public void Advice_follows_zone_and_trend(double earlier, double recent, string title, string text)
    {
        var viewModel = Update(earlier, recent);

        Assert.Equal(title, viewModel.AdviceTitle);
        Assert.Equal(text, viewModel.AdviceText);
    }

    [Fact]
    public void The_value_unit_and_zone_come_as_text_for_the_view()
    {
        var viewModel = Update(1300, 1050);

        Assert.Equal("1,050", viewModel.ValueText);
        Assert.Equal("ppm", viewModel.UnitText);
        Assert.Equal("Zone_Elevated", viewModel.ZoneText);
    }

    [Fact]
    public void The_three_stats_are_filled_from_the_recent_readings()
    {
        var viewModel = Update(1300, 1050);

        Assert.Equal(3, viewModel.Stats.Count);
        Assert.Equal("Stat_GoodShare", viewModel.Stats[0].Label);
    }

    [Fact]
    public void No_reading_clears_everything()
    {
        var viewModel = Update(1300, 1050);

        viewModel.Update(null, [], now);

        Assert.Null(viewModel.Zone);
        Assert.Null(viewModel.Co2Ppm);
        Assert.Equal(TrendDirection.Steady, viewModel.Trend);
        Assert.Null(viewModel.TrendText);
        Assert.Null(viewModel.TrendDetail);
        Assert.Null(viewModel.AdviceTitle);
        Assert.Null(viewModel.AdviceText);
        Assert.Null(viewModel.ValueText);
        Assert.Null(viewModel.ZoneText);
        Assert.Empty(viewModel.Stats);
    }

    [Theory]
    [InlineData(600, 600)]
    [InlineData(1300, 1050)]
    [InlineData(1050, 1300)]
    [InlineData(1900, 1500)]
    [InlineData(1500, 1900)]
    [InlineData(1500, 1500)]
    public void Every_text_the_view_model_asks_for_exists_in_both_languages(double earlier, double recent)
    {
        foreach (var culture in new[] { "en-US", "uk-UA" })
        {
            var localizer = new Localizer();
            localizer.SetCulture(System.Globalization.CultureInfo.GetCultureInfo(culture));
            var viewModel = TestHero.Create(localizer);
            var readings = History(earlier, recent);

            viewModel.Update(readings[^1], readings, now);

            Assert.DoesNotContain("[", viewModel.TrendText);
            Assert.DoesNotContain("[", viewModel.AdviceTitle);
            Assert.DoesNotContain("[", viewModel.AdviceText);
            Assert.DoesNotContain("[", viewModel.TrendDetail ?? string.Empty);
        }
    }
}

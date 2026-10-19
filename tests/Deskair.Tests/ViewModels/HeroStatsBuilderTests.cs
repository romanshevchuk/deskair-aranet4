using Deskair.App.Localization;
using Deskair.App.ViewModels.Live;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class HeroStatsBuilderTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 12, 12);

    private static HeroStatsBuilder Create(ILocalizer? localizer = null)
    {
        var text = localizer ?? new FakeLocalizer();

        return new HeroStatsBuilder(text, TestFormatter.Create(text), new LocalCalendar(TimeZoneInfo.Utc));
    }

    private static List<Reading> At(params (int MinutesAgo, double Ppm)[] values)
        => values.Select(value => Samples.Co2(now.AddMinutes(-value.MinutesAgo), value.Ppm)).OrderBy(reading => reading.Timestamp).ToList();

    [Fact]
    public void Three_stats_in_the_order_good_share_peak_time_in_zone()
    {
        var readings = At((120, 600), (90, 1800), (60, 700), (30, 1100), (0, 1200));

        var stats = Create().Build(readings, now);

        Assert.Equal(3, stats.Count);
        Assert.Equal("Stat_GoodShare", stats[0].Label);
        Assert.StartsWith("Stat_Peak", stats[1].Label);
        Assert.Equal("Stat_InZone_Elevated", stats[2].Label);
    }

    [Fact]
    public void Good_share_is_a_percentage_in_the_selected_culture()
    {
        var readings = At((40, 600), (30, 700), (20, 800), (10, 1200));

        Assert.Equal("75%", Create().Build(readings, now)[0].Value);
    }

    [Fact]
    public void Peak_shows_the_value_with_its_unit_and_the_local_time()
    {
        var readings = At((90, 1800), (60, 700), (0, 900));

        var peak = Create().Build(readings, now)[1];

        Assert.Equal("1800 ppm", peak.Value);
        Assert.Equal("Stat_Peak(time=10:30)", peak.Label);
    }

    [Fact]
    public void A_peak_that_is_the_latest_reading_says_right_now()
    {
        var readings = At((60, 700), (0, 1800));

        Assert.Equal("Stat_PeakNow", Create().Build(readings, now)[1].Label);
    }

    [Theory]
    [InlineData(0, "Duration_LessThanMinute")]
    [InlineData(38, "Duration_Minutes(min=38)")]
    [InlineData(59, "Duration_Minutes(min=59)")]
    [InlineData(60, "Duration_Hours(h=1)")]
    [InlineData(125, "Duration_HoursMinutes(h=2, min=5)")]
    public void Time_in_the_zone_is_minutes_or_hours_and_minutes(int minutes, string expected)
    {
        var readings = At((minutes + 30, 500), (minutes, 1200), (0, 1300));

        Assert.Equal(expected, Create().Build(readings, now)[2].Value);
    }

    [Fact]
    public void No_readings_gives_no_stats()
    {
        Assert.Empty(Create().Build([], now));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("uk-UA")]
    public void Every_text_exists_in_both_languages(string culture)
    {
        var localizer = new Localizer();
        localizer.SetCulture(System.Globalization.CultureInfo.GetCultureInfo(culture));
        var good = At((300, 600), (200, 1800), (100, 600), (0, 700));
        var elevated = At((300, 600), (200, 1800), (100, 1200), (0, 1300));
        var high = At((300, 600), (0, 1800));

        foreach (var readings in new[] { good, elevated, high })
        {
            foreach (var stat in Create(localizer).Build(readings, now))
            {
                Assert.DoesNotContain("[", stat.Value);
                Assert.DoesNotContain("[", stat.Label);
            }
        }
    }
}

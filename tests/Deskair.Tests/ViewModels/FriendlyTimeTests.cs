using System.Globalization;
using Deskair.App.Localization;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class FriendlyTimeTests
{
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 13, 12);

    private static FriendlyTime Create(TimeZoneInfo? zone = null, string culture = "en-US")
    {
        var localizer = new FakeCultureLocalizer(CultureInfo.GetCultureInfo(culture));

        return new FriendlyTime(localizer, new LocalCalendar(zone ?? TimeZoneInfo.Utc), new FakeClock(now, zone));
    }

    [Fact]
    public void A_moment_today_is_just_the_time()
    {
        Assert.Equal("09:05", Create().Text(Samples.Utc(2026, 10, 13, 9, 5)));
    }

    [Fact]
    public void A_moment_on_another_day_has_the_date_too()
    {
        Assert.Equal("14 Oct, 07:00", Create().Text(Samples.Utc(2026, 10, 14, 7)));
        Assert.Equal("10 Oct, 12:00", Create().Text(Samples.Utc(2026, 10, 10, 12)));
    }

    [Fact]
    public void The_day_is_the_local_day_not_the_utc_day()
    {
        var kyiv = Samples.Kyiv;

        // 22:30 UTC on the 13th is 01:30 on the 14th in Kyiv (UTC+3), while "now" (12:00 UTC) is the 13th there.
        Assert.Equal("14 Oct, 01:30", Create(kyiv).Text(Samples.Utc(2026, 10, 13, 22, 30)));
    }

    [Fact]
    public void The_month_follows_the_language()
    {
        Assert.Equal("14 жовт., 07:00", Create(culture: "uk-UA").Text(Samples.Utc(2026, 10, 14, 7)));
    }
}

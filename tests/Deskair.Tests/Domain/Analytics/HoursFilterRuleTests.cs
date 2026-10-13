using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class HoursFilterRuleTests
{
    private static readonly LocalCalendar utc = new(TimeZoneInfo.Utc);
    private static readonly NightWindow night = NightWindow.Default;

    [Fact]
    public void Includes_everything_for_AllDay()
    {
        Assert.True(HoursFilterRule.Includes(Samples.Utc(2026, 10, 9, 13), HoursFilterMode.AllDay, utc, night));
    }

    [Theory]
    [InlineData(22, 0, true)]
    [InlineData(23, 30, true)]
    [InlineData(3, 0, true)]
    [InlineData(6, 59, true)]
    [InlineData(7, 0, false)]
    [InlineData(13, 0, false)]
    [InlineData(21, 59, false)]
    public void Includes_Night_is_22_00_up_to_07_00_by_default(int hour, int minute, bool expected)
    {
        var instant = Samples.Utc(2026, 10, 9, hour, minute);

        Assert.Equal(expected, HoursFilterRule.Includes(instant, HoursFilterMode.Night, utc, night));
    }

    [Theory]
    [InlineData(23, 59, false)]
    [InlineData(0, 0, true)]
    [InlineData(9, 59, true)]
    [InlineData(10, 0, false)]
    public void Includes_Night_follows_a_window_chosen_by_the_user(int hour, int minute, bool expected)
    {
        var morningSleeper = new NightWindow(new TimeOnly(0, 0), new TimeOnly(10, 0));

        Assert.Equal(expected, HoursFilterRule.Includes(Samples.Utc(2026, 10, 9, hour, minute), HoursFilterMode.Night, utc, morningSleeper));
    }

    [Fact]
    public void Includes_Night_follows_local_time()
    {
        var calendar = new LocalCalendar(Samples.Kyiv);

        // 20:30 UTC in January is 22:30 in Kyiv.
        Assert.True(HoursFilterRule.Includes(Samples.Utc(2026, 1, 15, 20, 30), HoursFilterMode.Night, calendar, night));
    }

    [Fact]
    public void Apply_keeps_the_order_and_does_not_change_the_input()
    {
        var readings = new[]
        {
            Samples.Co2(Samples.Utc(2026, 10, 9, 23), 900),
            Samples.Co2(Samples.Utc(2026, 10, 9, 12), 700),
            Samples.Co2(Samples.Utc(2026, 10, 10, 1), 1100)
        };

        var kept = HoursFilterRule.Apply(readings, HoursFilterMode.Night, utc, night);

        Assert.Equal(new[] { 900.0, 1100.0 }, kept.Select(reading => reading.Co2Ppm));
        Assert.Equal(3, readings.Length);
    }

    [Fact]
    public void Apply_returns_an_empty_list_for_empty_input()
    {
        Assert.Empty(HoursFilterRule.Apply([], HoursFilterMode.Night, utc, night));
    }
}

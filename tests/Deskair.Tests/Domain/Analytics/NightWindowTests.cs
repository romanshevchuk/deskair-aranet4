using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class NightWindowTests
{
    private static readonly LocalCalendar utc = new(TimeZoneInfo.Utc);
    private static readonly LocalCalendar kyiv = new(Samples.Kyiv);

    private static NightWindow Window(int startHour, int endHour) => new(new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    [Fact]
    public void The_default_is_22_00_to_07_00()
    {
        Assert.Equal(Window(22, 7), NightWindow.Default);
        Assert.True(NightWindow.Default.CrossesMidnight);
        Assert.Equal(TimeSpan.FromHours(9), NightWindow.Default.Length);
    }

    [Theory]
    [InlineData(23, 7, 8)]
    [InlineData(2, 10, 8)]
    [InlineData(0, 6, 6)]
    public void The_length_counts_across_midnight_or_not(int start, int end, int hours)
    {
        Assert.Equal(TimeSpan.FromHours(hours), Window(start, end).Length);
    }

    [Fact]
    public void A_window_that_starts_where_it_ends_is_empty_and_includes_nothing()
    {
        var empty = Window(7, 7);

        Assert.True(empty.IsEmpty);
        Assert.Equal(TimeSpan.Zero, empty.Length);
        Assert.False(empty.Includes(TimeSpan.FromHours(7)));
        Assert.False(empty.Includes(TimeSpan.FromHours(12)));
    }

    [Fact]
    public void Last_finished_night_is_the_one_that_ended_this_morning()
    {
        var (start, end) = NightWindow.Default.LastFinished(Samples.Utc(2026, 10, 13, 15), utc);

        Assert.Equal(Samples.Utc(2026, 10, 12, 22), start);
        Assert.Equal(Samples.Utc(2026, 10, 13, 7), end);
    }

    [Fact]
    public void While_the_night_is_going_on_last_night_is_still_the_one_before()
    {
        var (start, end) = NightWindow.Default.LastFinished(Samples.Utc(2026, 10, 13, 23, 30), utc);

        Assert.Equal(Samples.Utc(2026, 10, 12, 22), start);
        Assert.Equal(Samples.Utc(2026, 10, 13, 7), end);
    }

    [Fact]
    public void A_night_is_finished_at_the_moment_it_ends()
    {
        var (_, end) = NightWindow.Default.LastFinished(Samples.Utc(2026, 10, 13, 7), utc);

        Assert.Equal(Samples.Utc(2026, 10, 13, 7), end);
    }

    [Fact]
    public void Just_before_the_end_the_night_before_is_the_last_finished()
    {
        var (_, end) = NightWindow.Default.LastFinished(Samples.Utc(2026, 10, 13, 6, 59), utc);

        Assert.Equal(Samples.Utc(2026, 10, 12, 7), end);
    }

    [Fact]
    public void A_window_inside_one_day_has_its_night_on_the_same_date()
    {
        var (start, end) = Window(2, 10).LastFinished(Samples.Utc(2026, 10, 13, 15), utc);

        Assert.Equal(Samples.Utc(2026, 10, 13, 2), start);
        Assert.Equal(Samples.Utc(2026, 10, 13, 10), end);
    }

    [Fact]
    public void Last_finished_night_uses_local_time()
    {
        // 13 Oct 2026 is summer time in Kyiv (UTC+3): 22:00 on the 12th is 19:00 UTC, 07:00 on the 13th is 04:00 UTC.
        var (start, end) = NightWindow.Default.LastFinished(Samples.Utc(2026, 10, 13, 12), kyiv);

        Assert.Equal(Samples.Utc(2026, 10, 12, 19), start);
        Assert.Equal(Samples.Utc(2026, 10, 13, 4), end);
    }

    [Fact]
    public void The_night_the_clocks_go_back_is_an_hour_longer()
    {
        // Kyiv clocks go back on 25 Oct 2026 at 04:00 (UTC+3 to UTC+2): the night of 24 to 25 October lasts ten hours.
        var (start, end) = NightWindow.Default.LastFinished(Samples.Utc(2026, 10, 25, 12), kyiv);

        Assert.Equal(TimeSpan.FromHours(10), end - start);
    }

    [Fact]
    public void The_night_the_clocks_go_forward_is_an_hour_shorter()
    {
        // Kyiv clocks go forward on 29 Mar 2026 at 03:00 (UTC+2 to UTC+3): the night of 28 to 29 March lasts eight hours.
        var (start, end) = NightWindow.Default.LastFinished(Samples.Utc(2026, 3, 29, 12), kyiv);

        Assert.Equal(TimeSpan.FromHours(8), end - start);
    }

    [Fact]
    public void An_empty_window_has_no_last_night()
    {
        Assert.Throws<InvalidOperationException>(() => Window(7, 7).LastFinished(Samples.Utc(2026, 10, 13, 12), utc));
    }
}

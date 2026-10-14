using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Time;

public class HistoryPeriodTests
{
    private static readonly LocalCalendar calendar = new(Samples.Kyiv);

    // Wednesday 14 October 2026, 13:00 in Kyiv (summer time, UTC+3).
    private static readonly DateTimeOffset now = new(2026, 10, 14, 10, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset Local(int month, int day, int hour = 0) => calendar.InstantAt(new DateOnly(2026, month, day), new TimeOnly(hour, 0));

    [Fact]
    public void The_latest_day_is_the_last_24_hours()
    {
        var period = HistoryPeriod.For(Period.Day, 0, now, calendar);

        Assert.True(period.IsRolling);
        Assert.Equal(now.AddHours(-24), period.Start);
        Assert.Equal(now, period.End);
    }

    [Fact]
    public void An_earlier_day_is_a_calendar_day()
    {
        var period = HistoryPeriod.For(Period.Day, 1, now, calendar);

        Assert.False(period.IsRolling);
        Assert.Equal(Local(10, 13), period.Start);
        Assert.Equal(Local(10, 14), period.End);
    }

    [Fact]
    public void The_latest_week_is_seven_local_days_ending_now()
    {
        var period = HistoryPeriod.For(Period.Week, 0, now, calendar);

        Assert.Equal(Local(10, 8), period.Start);
        Assert.Equal(now, period.End);
        Assert.Equal((new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 14)), period.Dates(calendar));
    }

    [Fact]
    public void An_earlier_week_is_the_seven_whole_days_before()
    {
        var period = HistoryPeriod.For(Period.Week, 1, now, calendar);

        Assert.Equal(Local(10, 1), period.Start);
        Assert.Equal(Local(10, 8), period.End);
        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7)), period.Dates(calendar));
    }

    [Fact]
    public void A_month_is_thirty_days()
    {
        var latest = HistoryPeriod.For(Period.Month, 0, now, calendar);
        var before = HistoryPeriod.For(Period.Month, 1, now, calendar);

        Assert.Equal(Local(9, 15), latest.Start);
        Assert.Equal(Local(8, 16), before.Start);
        Assert.Equal(Local(9, 15), before.End);
    }

    [Fact]
    public void A_year_is_twelve_calendar_months_ending_with_this_month()
    {
        var period = HistoryPeriod.For(Period.Year, 0, now, calendar);

        Assert.Equal(calendar.StartOfLocalDay(new DateOnly(2025, 11, 1)), period.Start);
        Assert.Equal(now, period.End);
    }

    [Fact]
    public void A_week_across_the_autumn_clock_change_is_seven_local_days_and_an_hour_longer()
    {
        // Summer time ends on 25 October 2026; the week of 26 October to 1 November ends on 1 November at midnight.
        var later = new DateTimeOffset(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);
        var period = HistoryPeriod.For(Period.Week, 2, later, calendar);

        Assert.Equal((new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 25)), period.Dates(calendar));
        Assert.Equal(TimeSpan.FromHours((7 * 24) + 1), period.End - period.Start);
    }

    [Fact]
    public void The_last_24_hours_compare_with_the_24_hours_before_and_other_periods_with_the_one_before()
    {
        var rolling = HistoryPeriod.For(Period.Day, 0, now, calendar);
        var week = HistoryPeriod.For(Period.Week, 0, now, calendar);

        Assert.Equal((now.AddHours(-48), now.AddHours(-24)), (rolling.Before(now, calendar).Start, rolling.Before(now, calendar).End));
        Assert.Equal(HistoryPeriod.For(Period.Week, 1, now, calendar), week.Before(now, calendar));
    }

    [Theory]
    // First reading on 6 October: the week of 1 to 7 October holds it, so ‹ can go one further, to 24 to 30 September (offset 2), which is empty.
    [InlineData(Period.Week, 2)]
    // The 30 days ending today hold it; ‹ goes to the 30 days before (offset 1).
    [InlineData(Period.Month, 1)]
    // Calendar days: today (rolling) back to 6 October is 8 steps, then one empty day.
    [InlineData(Period.Day, 9)]
    [InlineData(Period.Year, 0)]
    public void Back_stops_one_period_before_the_first_reading(Period range, int expected)
    {
        Assert.Equal(expected, HistoryPeriod.MaxOffset(range, now, Local(10, 6, 9), calendar));
    }

    [Fact]
    public void Back_is_off_when_nothing_is_stored()
    {
        Assert.Equal(0, HistoryPeriod.MaxOffset(Period.Week, now, null, calendar));
    }

    [Fact]
    public void A_negative_offset_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HistoryPeriod.For(Period.Day, -1, now, calendar));
    }
}

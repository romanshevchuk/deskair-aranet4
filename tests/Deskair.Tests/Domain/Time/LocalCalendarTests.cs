using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Time;

public class LocalCalendarTests
{
    private static readonly LocalCalendar kyiv = new(Samples.Kyiv);

    [Fact]
    public void StartOfLocalDay_for_a_date_follows_the_offset_of_that_date()
    {
        // Summer time starts at 03:00 on 29 March 2026, so 29 March begins at UTC+2 and 30 March at UTC+3.
        Assert.Equal(Samples.Utc(2026, 3, 28, 22), kyiv.StartOfLocalDay(new DateOnly(2026, 3, 29)));
        Assert.Equal(Samples.Utc(2026, 3, 29, 21), kyiv.StartOfLocalDay(new DateOnly(2026, 3, 30)));
    }

    [Fact]
    public void LocalDateOf_gives_the_calendar_date_in_the_zone()
    {
        // 22:30 UTC on 29 March is 01:30 on 30 March in Kyiv (UTC+3).
        Assert.Equal(new DateOnly(2026, 3, 30), kyiv.LocalDateOf(Samples.Utc(2026, 3, 29, 22, 30)));
    }

    [Fact]
    public void StartOfLocalDay_uses_the_offset_at_local_midnight()
    {
        // 13:00 local on 29 March 2026: summer time has just started, but local midnight was still UTC+2.
        var instant = Samples.Utc(2026, 3, 29, 10);

        Assert.Equal(Samples.Utc(2026, 3, 28, 22), kyiv.StartOfLocalDay(instant));
    }

    [Fact]
    public void StartOfLocalDay_is_stable_for_any_instant_in_the_same_local_day()
    {
        var morning = kyiv.StartOfLocalDay(Samples.Utc(2026, 10, 9, 5));
        var evening = kyiv.StartOfLocalDay(Samples.Utc(2026, 10, 9, 18));

        Assert.Equal(morning, evening);
    }

    [Theory]
    [InlineData(2026, 10, 9, 24)]
    [InlineData(2026, 3, 29, 23)]
    [InlineData(2026, 10, 25, 25)]
    public void DayLength_is_23_or_25_hours_when_daylight_saving_changes(int year, int month, int day, int hours)
    {
        Assert.Equal(TimeSpan.FromHours(hours), kyiv.DayLength(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(60, 2, 30)]
    [InlineData(61, 2, 30)]
    [InlineData(0, 2, 0)]
    [InlineData(-10, 2, 0)]
    public void ExpectedSlots_is_elapsed_time_divided_by_interval_rounded_down(int minutes, int intervalMinutes, int expected)
    {
        var start = Samples.Utc(2026, 10, 9, 12);

        var slots = LocalCalendar.ExpectedSlots(start, start.AddMinutes(minutes), TimeSpan.FromMinutes(intervalMinutes));

        Assert.Equal(expected, slots);
    }

    [Fact]
    public void ExpectedSlots_rejects_an_interval_of_zero()
    {
        var start = Samples.Utc(2026, 10, 9, 12);

        Assert.Throws<ArgumentOutOfRangeException>(() => LocalCalendar.ExpectedSlots(start, start.AddHours(1), TimeSpan.Zero));
    }
}

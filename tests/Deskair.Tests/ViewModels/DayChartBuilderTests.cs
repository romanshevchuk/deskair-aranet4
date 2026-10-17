using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.AirImproved;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class DayChartBuilderTests
{
    private readonly DayChartBuilder builder;
    private readonly LocalCalendar calendar = new(TimeZoneInfo.Utc);

    public DayChartBuilderTests()
    {
        var localizer = new FakeLocalizer();
        builder = new DayChartBuilder(localizer, TestFormatter.Create(localizer), calendar);
    }

    private static List<HourOfDayAverage> Typical(Func<int, double?> average)
        => [.. Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, average(hour), average(hour) is null ? 0 : 5))];

    [Fact]
    public void The_last_24_hours_hold_one_night_clipped_to_the_window()
    {
        var spans = builder.NightSpans(NightWindow.Default, Samples.Utc(2026, 10, 13, 13), Samples.Utc(2026, 10, 14, 13));

        var night = Assert.Single(spans);
        Assert.Equal(Samples.Utc(2026, 10, 13, 22), night.Start);
        Assert.Equal(Samples.Utc(2026, 10, 14, 7), night.End);
    }

    [Fact]
    public void A_calendar_day_has_the_morning_and_the_evening_of_the_night()
    {
        var spans = builder.NightSpans(NightWindow.Default, Samples.Utc(2026, 10, 14), Samples.Utc(2026, 10, 15));

        Assert.Equal(
            [(Samples.Utc(2026, 10, 14), Samples.Utc(2026, 10, 14, 7)), (Samples.Utc(2026, 10, 14, 22), Samples.Utc(2026, 10, 15))],
            spans.Select(span => (span.Start, span.End)));
    }

    [Fact]
    public void A_window_that_starts_in_the_night_has_the_rest_of_it()
    {
        var spans = builder.NightSpans(NightWindow.Default, Samples.Utc(2026, 10, 14, 3), Samples.Utc(2026, 10, 14, 12));

        var night = Assert.Single(spans);
        Assert.Equal((Samples.Utc(2026, 10, 14, 3), Samples.Utc(2026, 10, 14, 7)), (night.Start, night.End));
    }

    [Fact]
    public void A_night_that_does_not_cross_midnight_is_found_too()
    {
        var spans = builder.NightSpans(new NightWindow(new TimeOnly(2, 0), new TimeOnly(10, 0)), Samples.Utc(2026, 10, 14), Samples.Utc(2026, 10, 15));

        var night = Assert.Single(spans);
        Assert.Equal((Samples.Utc(2026, 10, 14, 2), Samples.Utc(2026, 10, 14, 10)), (night.Start, night.End));
    }

    [Fact]
    public void An_empty_night_window_or_range_has_no_spans()
    {
        Assert.Empty(builder.NightSpans(new NightWindow(new TimeOnly(7, 0), new TimeOnly(7, 0)), Samples.Utc(2026, 10, 14), Samples.Utc(2026, 10, 15)));
        Assert.Empty(builder.NightSpans(NightWindow.Default, Samples.Utc(2026, 10, 14), Samples.Utc(2026, 10, 14)));
    }

    [Fact]
    public void The_typical_line_has_a_point_in_the_middle_of_each_hour_of_the_window()
    {
        var line = builder.TypicalLine(Typical(hour => hour * 10), Samples.Utc(2026, 10, 14, 13, 20), Samples.Utc(2026, 10, 15, 13, 20));

        Assert.Equal(24, line.Count);
        Assert.Equal(Samples.Utc(2026, 10, 14, 13, 30), line[0].Time);
        Assert.Equal(130, line[0].Value);
        Assert.Equal(Samples.Utc(2026, 10, 15, 12, 30), line[^1].Time);
        Assert.Equal(120, line[^1].Value);
        Assert.Equal(0, line[11].Value);
    }

    [Fact]
    public void A_calendar_day_has_24_points_in_order_of_the_hours()
    {
        var line = builder.TypicalLine(Typical(hour => 500 + hour), Samples.Utc(2026, 10, 14), Samples.Utc(2026, 10, 15));

        Assert.Equal(24, line.Count);
        Assert.Equal(Enumerable.Range(500, 24).Select(value => (double?)value), line.Select(point => point.Value));
    }

    [Fact]
    public void An_hour_without_readings_leaves_a_gap_in_the_typical_line()
    {
        var line = builder.TypicalLine(Typical(hour => hour == 5 ? null : 700), Samples.Utc(2026, 10, 14), Samples.Utc(2026, 10, 15));

        Assert.Null(line[5].Value);
        Assert.Equal(700, line[4].Value);
    }

    [Fact]
    public void No_typical_hours_give_no_line()
    {
        Assert.Empty(builder.TypicalLine([], Samples.Utc(2026, 10, 14), Samples.Utc(2026, 10, 15)));
    }

    [Fact]
    public void A_marker_says_when_and_by_how_much_the_air_improved()
    {
        var events = new[] { new AirImprovedEvent(Samples.Utc(2026, 10, 14, 14, 16), 1500, 934, TimeSpan.FromMinutes(12)) };

        var marker = Assert.Single(builder.Markers(events));

        Assert.Equal(Samples.Utc(2026, 10, 14, 14, 16), marker.Time);
        Assert.Equal(1500, marker.Value);
        Assert.Equal("History_AirImproved_Hover(time=14:16, drop=934, unit=ppm, min=12)", marker.Text);
    }

    [Fact]
    public void Markers_keep_at_least_60_minutes_between_them()
    {
        var events = new[]
        {
            new AirImprovedEvent(Samples.Utc(2026, 10, 14, 14), 1500, 300, TimeSpan.FromMinutes(10)),
            new AirImprovedEvent(Samples.Utc(2026, 10, 14, 14, 30), 1500, 300, TimeSpan.FromMinutes(10)),
            new AirImprovedEvent(Samples.Utc(2026, 10, 14, 15, 10), 1500, 300, TimeSpan.FromMinutes(10))
        };

        Assert.Equal(2, builder.Markers(events).Count);
    }
}

using Deskair.App.ViewModels.ChartModels;
using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.Comfort;
using Deskair.Domain.Analytics.HourGrid;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class HourGridBuilderTests
{
    private static readonly DateOnly today = new(2026, 10, 14);
    private readonly HourGridBuilder builder = new(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer()));

    private static HourGridDay Day(int date, double? value, int hoursShown = 24)
        => new(new DateOnly(2026, 10, date), Enumerable.Repeat(value, 24).ToArray(), hoursShown, value, null);

    private HourGridModel Build(params HourGridDay[] days)
        => builder.Build(days, [], Metric.Co2, Period.Week, NightWindow.Default, today);

    [Fact]
    public void Two_or_more_days_without_readings_fold_into_one_row_and_a_single_one_stays()
    {
        var model = Build(Day(8, 900), Day(9, null), Day(10, null), Day(11, 900), Day(12, null), Day(13, 900), Day(14, 900, 13));

        Assert.Equal(6, model.Rows.Count);
        Assert.Equal("History_Grid_NoReadings(from=9 Oct, to=10 Oct)", model.Rows[1].CollapsedText);
        Assert.False(model.Rows[3].IsCollapsed);
        Assert.All(model.Rows[3].Cells, cell => Assert.Null(cell!.Value));
    }

    [Fact]
    public void Today_is_never_folded_and_its_later_hours_are_not_drawn()
    {
        var model = Build(Day(12, null), Day(13, null), Day(14, null, 13));

        Assert.Equal(2, model.Rows.Count);
        Assert.Equal("Chart_Today", model.Rows[^1].Label);
        Assert.True(model.Rows[^1].Emphasis);
        Assert.Equal(13, model.Rows[^1].Cells.Count(cell => cell is not null));
    }

    [Fact]
    public void Week_rows_say_the_weekday_and_date_and_mondays_and_today_always_show_their_label()
    {
        var model = Build(Day(11, 900), Day(12, 900), Day(14, 900));

        Assert.Equal("Sun 11", model.Rows[0].Label);
        Assert.False(model.Rows[0].LabelAlways);
        Assert.True(model.Rows[1].LabelAlways);
        Assert.True(model.Rows[2].LabelAlways);
    }

    [Fact]
    public void The_row_end_bars_are_captioned_by_what_they_show()
    {
        Assert.Equal("History_Grid_BarZones", Build(Day(8, 900)).BarCaption);
        Assert.Equal("History_Grid_BarRange", BuildTemperature([], ComfortDay(8, 1)).BarCaption);
    }

    [Fact]
    public void The_best_and_worst_day_are_tagged()
    {
        var model = Build(Day(11, 900), Day(12, 1300), Day(13, 1000));

        Assert.Equal([HourGridTag.Best, HourGridTag.Worst, HourGridTag.None], model.Rows.Select(row => row.Tag));
    }

    [Fact]
    public void A_square_says_where_and_how_much()
    {
        var model = Build(Day(11, 912));

        Assert.Equal("History_Grid_Value(where=History_Grid_Cell(day=Sun 11 Oct, from=16:00, to=17:00), value=912, unit=ppm)", model.Rows[0].Cells[16]!.Text);
    }

    [Fact]
    public void A_worst_window_that_runs_past_midnight_ends_the_next_morning()
    {
        var typical = Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is 23 or 0 or 1 ? 1400 : 900, 10)).ToList();

        var model = builder.Build([Day(11, 900)], typical, Metric.Co2, Period.Week, NightWindow.Default, today);

        Assert.Equal("History_Grid_Worst(from=23:00, to=02:00)", model.WorstText);
    }

    [Fact]
    public void The_typical_row_outlines_the_worst_three_hours_and_names_them()
    {
        var typical = Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is >= 16 and < 19 ? 1400 : 900, 10)).ToList();

        var model = builder.Build([Day(11, 900)], typical, Metric.Co2, Period.Week, NightWindow.Default, today);

        Assert.Equal(16, model.WorstStart);
        Assert.Equal("History_Grid_Worst(from=16:00, to=19:00)", model.WorstText);
        Assert.Equal("History_Grid_Typical", model.Typical!.Label);
        Assert.Equal((22, 7), model.Night);
    }

    private HourGridModel BuildTemperature(IReadOnlyList<HourOfDayAverage> typical, params HourGridDay[] days)
        => builder.Build(days, typical, Metric.Temperature, Period.Week, NightWindow.Default, today);

    private static HourGridDay ComfortDay(int date, double inside)
        => new(new DateOnly(2026, 10, date), Enumerable.Repeat((double?)21, 24).ToArray(), 24, 21, null) { Comfort = new ComfortShare(1 - inside, inside, 0) };

    [Fact]
    public void Temperature_uses_its_comfort_palette_steps_and_legend_groups()
    {
        var model = BuildTemperature([], ComfortDay(8, 1));

        Assert.Equal(HourGridPalette.Temperature, model.Palette);
        Assert.Equal([18.0, 20, 24, 26], model.Steps.Select(step => step.Value));
        Assert.Equal([("Comfort_Cool", 0, 2), ("Comfort_Comfortable", 2, 1), ("Comfort_Warm", 3, 2)], model.LegendGroups);
    }

    [Fact]
    public void Humidity_uses_its_own_palette_and_legend_groups()
    {
        var model = builder.Build([ComfortDay(8, 1)], [], Metric.Humidity, Period.Week, NightWindow.Default, today);

        Assert.Equal(HourGridPalette.Humidity, model.Palette);
        Assert.Equal([25.0, 30, 50, 60], model.Steps.Select(step => step.Value));
        Assert.Equal("Comfort_Dry", model.LegendGroups[0].Name);
        Assert.Equal("Comfort_Humid", model.LegendGroups[2].Name);
    }

    [Fact]
    public void Co2_keeps_the_zone_palette_and_legend()
    {
        var model = Build(Day(8, 900));

        Assert.Equal(HourGridPalette.Zones, model.Palette);
        Assert.Equal(5, model.Steps.Count);
        Assert.Equal([("Zone_Good", 0, 2), ("Zone_Elevated", 2, 2), ("Zone_High", 4, 2)], model.LegendGroups);
    }

    [Fact]
    public void A_comfort_row_carries_its_share_and_only_the_worst_is_tagged()
    {
        var model = BuildTemperature([], ComfortDay(8, 1), ComfortDay(9, 0.5), ComfortDay(10, 0.8));

        Assert.All(model.Rows, row => Assert.NotNull(row.Comfort));
        Assert.Equal(HourGridTag.None, model.Rows[0].Tag);
        Assert.Equal(HourGridTag.Worst, model.Rows[1].Tag);
        Assert.Equal(HourGridTag.None, model.Rows[2].Tag);
    }

    [Fact]
    public void The_typical_row_outlines_the_coolest_three_hours_and_names_them()
    {
        var typical = Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is >= 3 and <= 5 ? 17 : 21, 1)).ToList();

        var model = BuildTemperature(typical, ComfortDay(8, 1));

        Assert.Equal(3, model.WorstStart);
        Assert.Equal("History_Grid_Coolest(from=03:00, to=06:00)", model.WorstText);
    }

    [Fact]
    public void A_warm_afternoon_is_named_warmest_and_a_dry_night_driest()
    {
        var warm = Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is >= 14 and <= 16 ? 27 : 22, 1)).ToList();
        var dry = Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is >= 1 and <= 3 ? 20 : 40, 1)).ToList();

        Assert.Equal("History_Grid_Warmest(from=14:00, to=17:00)", BuildTemperature(warm, ComfortDay(8, 1)).WorstText);
        Assert.Equal(
            "History_Grid_Driest(from=01:00, to=04:00)",
            builder.Build([ComfortDay(8, 1)], dry, Metric.Humidity, Period.Week, NightWindow.Default, today).WorstText);
    }

    [Fact]
    public void A_comfortable_typical_day_outlines_nothing()
    {
        var typical = Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, 22, 1)).ToList();

        var model = BuildTemperature(typical, ComfortDay(8, 1));

        Assert.Null(model.WorstStart);
        Assert.Null(model.WorstText);
    }
}

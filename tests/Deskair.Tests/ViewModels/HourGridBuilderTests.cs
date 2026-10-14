using Deskair.App.ViewModels.ChartModels;
using Deskair.App.ViewModels.History;
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
    public void The_typical_row_outlines_the_worst_three_hours_and_names_them()
    {
        var typical = Enumerable.Range(0, 24).Select(hour => new HourOfDayAverage(hour, hour is >= 16 and < 19 ? 1400 : 900, 10)).ToList();

        var model = builder.Build([Day(11, 900)], typical, Metric.Co2, Period.Week, NightWindow.Default, today);

        Assert.Equal(16, model.WorstStart);
        Assert.Equal("History_Grid_Worst(from=16:00, to=19:00)", model.WorstText);
        Assert.Equal("History_Grid_Typical", model.Typical!.Label);
        Assert.Equal((22, 7), model.Night);
    }
}

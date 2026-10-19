using Deskair.App.ViewModels.ChartModels;
using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.HourGrid;
using Deskair.Domain.Metrics;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class HourGridBuilderYearTests
{
    private static readonly DateOnly today = new(2026, 10, 14);
    private readonly HourGridBuilder builder = new(new FakeLocalizer(), TestFormatter.Create(new FakeLocalizer()));

    private static HourGridMonth Month(int year, int month, double? value, int hours = 400, int shown = -1)
    {
        var length = DateTime.DaysInMonth(year, month);

        return new HourGridMonth(
            new DateOnly(year, month, 1),
            Enumerable.Range(0, 31).Select(index => index < length ? value : null).ToArray(),
            shown < 0 ? length : shown,
            value,
            null,
            value is null ? 0 : hours);
    }

    private HourGridModel Build(params HourGridMonth[] months) => builder.BuildYear(months, Metric.Co2, today);

    [Fact]
    public void The_year_grid_has_31_columns_with_centred_day_numbers_and_no_night_or_typical_row()
    {
        var model = Build(Month(2026, 9, 900), Month(2026, 10, 900, shown: 14));

        Assert.Equal(31, model.Columns);
        Assert.True(model.CenterColumnLabels);
        Assert.Equal([(0, "1"), (4, "5"), (9, "10"), (14, "15"), (19, "20"), (24, "25"), (30, "31")], model.ColumnLabels);
        Assert.Null(model.Typical);
        Assert.Null(model.Night);
        Assert.Null(model.NightText);
        Assert.Equal("History_Grid_UnitDay", model.UnitText);
    }

    [Fact]
    public void A_month_row_has_its_name_a_square_per_day_and_stops_at_today()
    {
        var model = Build(Month(2026, 9, 900), Month(2026, 10, 1000, shown: 14));

        Assert.Equal("Sep 2026", model.Rows[0].Label);
        Assert.Equal(31, model.Rows[0].Cells.Count);
        Assert.Equal(30, model.Rows[0].Cells.Count(cell => cell is not null));
        Assert.Equal(14, model.Rows[1].Cells.Count(cell => cell is not null));
        Assert.True(model.Rows[1].Emphasis);
        Assert.False(model.Rows[0].Emphasis);
        Assert.True(model.Rows[0].LabelAlways);
    }

    [Fact]
    public void A_day_without_enough_readings_is_an_outlined_square_and_a_day_with_them_has_a_tooltip()
    {
        var days = new double?[31];
        days[2] = 1100;
        var month = new HourGridMonth(new DateOnly(2026, 9, 1), days, 30, 1100, null, 300);

        var row = Build(month).Rows[0];

        Assert.Equal(1100, row.Cells[2]!.Value);
        Assert.Equal("History_Grid_Value(where=Thu 3 Sep 2026, value=1100, unit=ppm)", row.Cells[2]!.Text);
        Assert.Null(row.Cells[3]!.Value);
        Assert.Equal("History_Grid_Value_None(where=Fri 4 Sep 2026)", row.Cells[3]!.Text);
    }

    [Fact]
    public void Two_or_more_months_without_readings_fold_into_one_row_and_the_current_month_never_does()
    {
        var model = Build(Month(2026, 6, null), Month(2026, 7, null), Month(2026, 8, 900), Month(2026, 9, null), Month(2026, 10, null, shown: 14));

        Assert.Equal(4, model.Rows.Count);
        Assert.Equal("History_Grid_NoReadings(from=Jun 2026, to=Jul 2026)", model.Rows[0].CollapsedText);
        Assert.Equal("Sep 2026", model.Rows[2].Label);
        Assert.False(model.Rows[2].IsCollapsed);
        Assert.Equal("Oct 2026", model.Rows[3].Label);
    }

    [Fact]
    public void The_best_and_worst_month_are_tagged()
    {
        var model = Build(Month(2026, 7, 900), Month(2026, 8, 1300), Month(2026, 9, 1000), Month(2026, 10, 1100, shown: 14));

        Assert.Equal(
            [HourGridTag.Best, HourGridTag.Worst, HourGridTag.None, HourGridTag.None],
            model.Rows.Select(row => row.Tag));
    }

    [Fact]
    public void A_month_with_too_few_hours_or_in_progress_is_not_tagged()
    {
        var model = Build(Month(2026, 6, 800), Month(2026, 7, 400, hours: 100), Month(2026, 8, 1300), Month(2026, 9, 1000), Month(2026, 10, 300, shown: 14));

        Assert.Equal(HourGridTag.Best, model.Rows[0].Tag);
        Assert.Equal(HourGridTag.None, model.Rows[1].Tag);
        Assert.Equal(HourGridTag.Worst, model.Rows[2].Tag);
        Assert.Equal(HourGridTag.None, model.Rows[^1].Tag);
    }

    [Fact]
    public void Pressure_gets_no_tags_and_no_steps()
    {
        var model = builder.BuildYear([Month(2026, 7, 1010), Month(2026, 8, 1012), Month(2026, 9, 1014)], Metric.Pressure, today);

        Assert.All(model.Rows, row => Assert.Equal(HourGridTag.None, row.Tag));
        Assert.Empty(model.Steps);
    }

    [Fact]
    public void The_row_ends_with_the_month_average()
    {
        var model = Build(Month(2026, 9, 987.4));

        Assert.Equal("987", model.Rows[0].AverageText);
    }
}

using Deskair.App.ViewModels.History;
using Deskair.Domain.Metrics;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class ChartHeaderViewModelTests
{
    private readonly ChartHeaderViewModel viewModel = new(new FakeLocalizer());

    [Fact]
    public void The_day_line_is_hourly_and_the_title_is_the_metric()
    {
        viewModel.UpdateForDay(Metric.Humidity, hasGaps: false);

        Assert.Equal("Metric_Humidity", viewModel.Title);
        Assert.Equal("History_Chart_Hourly", viewModel.Subtitle);
    }

    [Theory]
    [InlineData(Metric.Co2, true, true, true, false, "")]
    [InlineData(Metric.Temperature, true, false, true, true, "History_Legend_Line")]
    [InlineData(Metric.Humidity, true, false, true, true, "History_Legend_Line")]
    [InlineData(Metric.Pressure, false, false, false, false, "History_Legend_Line")]
    public void The_day_legend_follows_the_metric(Metric metric, bool day, bool co2, bool typical, bool comfort, string text)
    {
        viewModel.UpdateForDay(metric, hasGaps: false);

        Assert.Equal((day, co2, typical, comfort, text), (viewModel.ShowDayLegend, viewModel.ShowDayCo2Legend, viewModel.ShowDayTypicalLegend, viewModel.ShowComfortLegend, viewModel.LegendText));
    }

    [Fact]
    public void A_gap_adds_the_no_data_legend_and_none_removes_it()
    {
        viewModel.UpdateForDay(Metric.Co2, hasGaps: true);
        Assert.True(viewModel.ShowNoDataLegend);

        viewModel.UpdateForDay(Metric.Co2, hasGaps: false);
        Assert.False(viewModel.ShowNoDataLegend);
    }

    [Theory]
    [InlineData(false, "History_Chart_ByHour")]
    [InlineData(true, "History_Chart_ByDay")]
    public void The_grid_has_a_title_and_no_legend_of_its_own_here(bool byDay, string subtitle)
    {
        viewModel.UpdateForDay(Metric.Co2, hasGaps: true);

        viewModel.UpdateForGrid(Metric.Co2, byDay);

        Assert.Equal(("Metric_Co2", subtitle, false, false), (viewModel.Title, viewModel.Subtitle, viewModel.ShowDayLegend, viewModel.ShowNoDataLegend));
    }

    [Theory]
    [InlineData(false, "History_Chart_MeansByHour")]
    [InlineData(true, "History_Chart_MeansByDay")]
    public void The_pressure_line_says_what_the_means_are_and_keeps_the_gap_legend(bool byDay, string subtitle)
    {
        viewModel.UpdateForDay(Metric.Temperature, hasGaps: false);

        viewModel.UpdateForLine(Metric.Pressure, byDay, hasGaps: true);

        Assert.Equal(("Metric_Pressure", subtitle, false, true, string.Empty), (viewModel.Title, viewModel.Subtitle, viewModel.ShowDayLegend, viewModel.ShowNoDataLegend, viewModel.LegendText));
    }
}

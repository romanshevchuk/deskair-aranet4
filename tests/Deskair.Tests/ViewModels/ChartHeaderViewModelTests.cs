using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class ChartHeaderViewModelTests
{
    private readonly ChartHeaderViewModel viewModel = new(new FakeLocalizer());

    [Theory]
    [InlineData(Period.Week, Period.Day, "History_Chart_Daily")]
    [InlineData(Period.Month, Period.Day, "History_Chart_Daily")]
    [InlineData(Period.Month, Period.Week, "History_Chart_Weekly")]
    [InlineData(Period.Year, Period.Week, "History_Chart_Weekly")]
    [InlineData(Period.Year, Period.Month, "History_Chart_Monthly")]
    [InlineData(Period.Day, Period.Day, "History_Chart_Hourly")]
    public void The_subtitle_says_what_one_bar_or_point_stands_for(Period range, Period grouping, string expected)
    {
        viewModel.Update(Metric.Co2, range, grouping, HoursFilterMode.AllDay, hasGaps: false);

        Assert.Equal("Metric_Co2", viewModel.Title);
        Assert.Equal(expected, viewModel.Subtitle);
    }

    [Fact]
    public void The_title_is_the_metric()
    {
        viewModel.Update(Metric.Humidity, Period.Week, Period.Day, HoursFilterMode.AllDay, hasGaps: false);

        Assert.Equal("Metric_Humidity", viewModel.Title);
    }

    [Fact]
    public void With_the_night_filter_the_subtitle_says_nights_only()
    {
        viewModel.Update(Metric.Co2, Period.Week, Period.Day, HoursFilterMode.Night, hasGaps: false);

        Assert.Equal("History_Chart_NightsOnly(what=History_Chart_Daily)", viewModel.Subtitle);
    }

    [Fact]
    public void The_day_line_ignores_the_night_filter_so_it_does_not_claim_nights_only()
    {
        viewModel.Update(Metric.Co2, Period.Day, Period.Day, HoursFilterMode.Night, hasGaps: false);

        Assert.Equal("History_Chart_Hourly", viewModel.Subtitle);
    }

    [Fact]
    public void CO2_shows_the_zone_legend_and_no_legend_text()
    {
        viewModel.Update(Metric.Co2, Period.Week, Period.Day, HoursFilterMode.AllDay, hasGaps: false);

        Assert.True(viewModel.ShowZoneLegend);
        Assert.Equal(string.Empty, viewModel.LegendText);
    }

    [Theory]
    [InlineData(Period.Week, "History_Legend_Bars")]
    [InlineData(Period.Month, "History_Legend_Bars")]
    [InlineData(Period.Day, "History_Legend_Line")]
    public void Other_metrics_explain_how_to_read_the_chart(Period range, string expected)
    {
        viewModel.Update(Metric.Temperature, range, Period.Day, HoursFilterMode.AllDay, hasGaps: false);

        Assert.False(viewModel.ShowZoneLegend);
        Assert.Equal(expected, viewModel.LegendText);
    }

    [Fact]
    public void A_gap_adds_the_no_data_legend_and_none_removes_it()
    {
        viewModel.Update(Metric.Co2, Period.Week, Period.Day, HoursFilterMode.AllDay, hasGaps: true);
        Assert.True(viewModel.ShowNoDataLegend);

        viewModel.Update(Metric.Co2, Period.Week, Period.Day, HoursFilterMode.AllDay, hasGaps: false);
        Assert.False(viewModel.ShowNoDataLegend);
    }
}

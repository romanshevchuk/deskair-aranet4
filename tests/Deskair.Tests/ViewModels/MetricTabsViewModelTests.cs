using Deskair.App.ViewModels.Live;
using Deskair.Domain.Metrics;

namespace Deskair.Tests.ViewModels;

public class MetricTabsViewModelTests
{
    [Fact]
    public void Select_sets_metric_and_range()
    {
        var viewModel = new MetricTabsViewModel();

        viewModel.Select(Metric.Humidity, LiveRange.SixHours);

        Assert.Equal(Metric.Humidity, viewModel.SelectedMetric);
        Assert.Equal(LiveRange.SixHours, viewModel.SelectedRange);
    }

    [Theory]
    [InlineData(Metric.Co2, LiveRange.OneHour, true)]
    [InlineData(Metric.Co2, LiveRange.SixHours, true)]
    [InlineData(Metric.Co2, LiveRange.TwentyFourHours, false)]
    [InlineData(Metric.Temperature, LiveRange.OneHour, false)]
    [InlineData(Metric.Pressure, LiveRange.SixHours, false)]
    public void ShowRibbon_only_for_co2_on_one_and_six_hours(Metric metric, LiveRange range, bool expected)
    {
        var viewModel = new MetricTabsViewModel();

        viewModel.Select(metric, range);

        Assert.Equal(expected, viewModel.ShowRibbon);
    }

    [Fact]
    public void Selecting_the_same_values_raises_nothing()
    {
        var viewModel = new MetricTabsViewModel();
        viewModel.Select(Metric.Co2, LiveRange.OneHour);
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        viewModel.Select(Metric.Co2, LiveRange.OneHour);

        Assert.Empty(raised);
    }

    [Fact]
    public void Changing_the_selection_raises_ShowRibbon()
    {
        var viewModel = new MetricTabsViewModel();
        viewModel.Select(Metric.Co2, LiveRange.OneHour);
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        viewModel.Select(Metric.Co2, LiveRange.TwentyFourHours);

        Assert.Contains(nameof(MetricTabsViewModel.ShowRibbon), raised);
    }
}

using Deskair.App.ViewModels.History;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;

namespace Deskair.Tests.ViewModels;

public class HistoryToolbarViewModelTests
{
    [Fact]
    public void Changing_the_range_returns_to_the_latest_period()
    {
        var viewModel = new HistoryToolbarViewModel { Range = Period.Week, MaxOffset = 5, Offset = 3 };

        viewModel.Range = Period.Month;

        Assert.Equal(0, viewModel.Offset);
        Assert.True(viewModel.IsLatest);
    }

    [Theory]
    [InlineData(nameof(HistoryToolbarViewModel.Metric), true)]
    [InlineData(nameof(HistoryToolbarViewModel.Range), true)]
    [InlineData(nameof(HistoryToolbarViewModel.Offset), true)]
    [InlineData(nameof(HistoryToolbarViewModel.PeriodLabel), false)]
    [InlineData(nameof(HistoryToolbarViewModel.MaxOffset), false)]
    public void Only_the_choices_ask_for_different_data(string property, bool expected)
        => Assert.Equal(expected, HistoryToolbarViewModel.IsChoice(property));

    [Fact]
    public void The_arrows_stay_inside_the_first_reading_and_today()
    {
        var viewModel = new HistoryToolbarViewModel { Metric = Metric.Co2, Range = Period.Week, MaxOffset = 1 };

        Assert.False(viewModel.NextPeriodCommand.CanExecute(null));
        Assert.True(viewModel.PreviousPeriodCommand.CanExecute(null));

        viewModel.PreviousPeriodCommand.Execute(null);

        Assert.Equal(1, viewModel.Offset);
        Assert.False(viewModel.PreviousPeriodCommand.CanExecute(null));
        Assert.True(viewModel.NextPeriodCommand.CanExecute(null));
    }
}

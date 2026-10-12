using Deskair.App.ViewModels.History;
using Deskair.Domain.Time;

namespace Deskair.Tests.ViewModels;

public class HistoryToolbarViewModelTests
{
    [Theory]
    [InlineData(Period.Day)]
    [InlineData(Period.Week)]
    public void Day_and_week_have_no_groupings(Period range)
    {
        var viewModel = new HistoryToolbarViewModel { Range = range };

        Assert.Empty(viewModel.GroupingOptions());
    }

    [Fact]
    public void Month_groups_by_day_or_week()
    {
        var viewModel = new HistoryToolbarViewModel { Range = Period.Month };

        Assert.Equal([Period.Day, Period.Week], viewModel.GroupingOptions());
    }

    [Fact]
    public void Year_groups_by_week_or_month()
    {
        var viewModel = new HistoryToolbarViewModel { Range = Period.Year };

        Assert.Equal([Period.Week, Period.Month], viewModel.GroupingOptions());
    }

    [Fact]
    public void Range_change_keeps_an_allowed_grouping()
    {
        var viewModel = new HistoryToolbarViewModel { Range = Period.Month, Grouping = Period.Week };

        viewModel.Range = Period.Year;

        Assert.Equal(Period.Week, viewModel.Grouping);
    }

    [Fact]
    public void Range_change_picks_the_first_allowed_grouping_when_current_is_not_allowed()
    {
        var viewModel = new HistoryToolbarViewModel { Range = Period.Month, Grouping = Period.Day };

        viewModel.Range = Period.Year;

        Assert.Equal(Period.Week, viewModel.Grouping);
    }
}

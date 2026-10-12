using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.TypicalDay;
using Deskair.Domain.Zones;

namespace Deskair.Tests.ViewModels;

public class TypicalDayViewModelTests
{
    private static List<HourOfDayAverage> Day(Func<int, double?> average)
    {
        return Enumerable.Range(0, 24)
            .Select(hour => new HourOfDayAverage(hour, average(hour), average(hour) is null ? 0 : 3))
            .ToList();
    }

    [Fact]
    public void Update_creates_24_bars_with_zones()
    {
        var viewModel = new TypicalDayViewModel();

        viewModel.Update(Day(hour => hour switch { < 8 => 600, < 16 => 1200, _ => 1500 }));

        Assert.Equal(24, viewModel.Bars.Count);
        Assert.Equal(Zone.Good, viewModel.Bars[0].Zone);
        Assert.Equal(Zone.Elevated, viewModel.Bars[10].Zone);
        Assert.Equal(Zone.High, viewModel.Bars[20].Zone);
        Assert.Equal(1200, viewModel.Bars[10].Average);
    }

    [Fact]
    public void Update_flags_the_worst_three_hour_window()
    {
        var viewModel = new TypicalDayViewModel();

        viewModel.Update(Day(hour => hour is 14 or 15 or 16 ? 1800 : 500));

        var flagged = viewModel.Bars.Where(bar => bar.InWorstWindow).Select(bar => bar.Hour);
        Assert.Equal([14, 15, 16], flagged);
    }

    [Fact]
    public void Worst_window_wraps_past_midnight()
    {
        var viewModel = new TypicalDayViewModel();

        viewModel.Update(Day(hour => hour is 23 or 0 or 1 ? 1800 : 500));

        var flagged = viewModel.Bars.Where(bar => bar.InWorstWindow).Select(bar => bar.Hour).Order();
        Assert.Equal([0, 1, 23], flagged);
    }

    [Fact]
    public void Hours_without_data_have_no_average_or_zone()
    {
        var viewModel = new TypicalDayViewModel();

        viewModel.Update(Day(hour => hour == 5 ? null : 700));

        Assert.Null(viewModel.Bars[5].Average);
        Assert.Null(viewModel.Bars[5].Zone);
    }

    [Fact]
    public void No_complete_window_flags_nothing()
    {
        var viewModel = new TypicalDayViewModel();

        viewModel.Update(Day(_ => null));

        Assert.All(viewModel.Bars, bar => Assert.False(bar.InWorstWindow));
    }
}

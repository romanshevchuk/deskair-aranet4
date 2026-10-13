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

    [Fact]
    public void The_worst_window_is_named_from_its_first_hour_to_the_hour_after_its_last()
    {
        var viewModel = new TypicalDayViewModel(new Deskair.Tests.Fakes.FakeLocalizer());

        viewModel.Update(Day(hour => hour is 15 or 16 or 17 ? 1800 : 500));

        Assert.Equal("History_TypicalDay_Worst(from=15, to=18)", viewModel.WorstText);
    }

    [Fact]
    public void A_worst_window_across_midnight_wraps_the_second_number()
    {
        var viewModel = new TypicalDayViewModel(new Deskair.Tests.Fakes.FakeLocalizer());

        viewModel.Update(Day(hour => hour is 22 or 23 or 0 ? 1800 : 500));

        Assert.Equal("History_TypicalDay_Worst(from=22, to=01)", viewModel.WorstText);
    }

    [Fact]
    public void Without_data_or_a_localizer_there_is_no_text()
    {
        var withoutData = new TypicalDayViewModel(new Deskair.Tests.Fakes.FakeLocalizer());
        var withoutLocalizer = new TypicalDayViewModel();

        withoutData.Update(Day(_ => null));
        withoutLocalizer.Update(Day(hour => hour == 14 ? 1800 : 500));

        Assert.Equal(string.Empty, withoutData.WorstText);
        Assert.Equal(string.Empty, withoutLocalizer.WorstText);
    }
}

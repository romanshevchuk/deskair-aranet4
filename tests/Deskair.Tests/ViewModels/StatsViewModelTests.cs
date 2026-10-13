using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Services.Settings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.ViewModels;

public class StatsViewModelTests
{
    private static readonly DateTimeOffset start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private static Bucket WithData(int day, double average, double minimum, double maximum, int count, int expected)
    {
        return new Bucket(start.AddDays(day), start.AddDays(day + 1), average, minimum, maximum, count, expected, BucketState.Complete);
    }

    private static Bucket Empty(int day, int expected)
    {
        return new Bucket(start.AddDays(day), start.AddDays(day + 1), null, null, null, 0, expected, BucketState.NoData);
    }

    [Fact]
    public void Average_is_weighted_by_reading_count()
    {
        var viewModel = new StatsViewModel();

        viewModel.Update([WithData(0, 600, 500, 700, 1, 10), WithData(1, 1000, 800, 1200, 3, 10)]);

        Assert.Equal(900, viewModel.Average);
    }

    [Fact]
    public void Highest_and_lowest_come_from_buckets_with_data()
    {
        var viewModel = new StatsViewModel();

        viewModel.Update([WithData(0, 600, 450, 700, 5, 10), Empty(1, 10), WithData(2, 1000, 800, 1500, 5, 10)]);

        Assert.Equal(1500, viewModel.Highest);
        Assert.Equal(450, viewModel.Lowest);
    }

    [Fact]
    public void Coverage_is_readings_present_over_expected()
    {
        var viewModel = new StatsViewModel();

        viewModel.Update([WithData(0, 600, 500, 700, 8, 10), Empty(1, 10)]);

        Assert.Equal(0.4, viewModel.Coverage, 10);
    }

    [Fact]
    public void All_buckets_without_data_clears_every_value()
    {
        var viewModel = new StatsViewModel();
        viewModel.Update([WithData(0, 600, 500, 700, 8, 10)]);

        viewModel.Update([Empty(0, 10), Empty(1, 10)]);

        Assert.Null(viewModel.Average);
        Assert.Null(viewModel.Highest);
        Assert.Null(viewModel.Lowest);
        Assert.Equal(0, viewModel.Coverage);
    }

    [Fact]
    public void No_buckets_clears_every_value()
    {
        var viewModel = new StatsViewModel();
        viewModel.Update([WithData(0, 600, 500, 700, 8, 10)]);

        viewModel.Update([]);

        Assert.Null(viewModel.Average);
        Assert.Equal(0, viewModel.Coverage);
    }

    [Fact]
    public void Full_coverage_hides_the_chip_and_gaps_show_it()
    {
        var viewModel = new StatsViewModel();

        viewModel.Update([WithData(0, 600, 500, 700, 10, 10), WithData(1, 700, 500, 800, 10, 10)]);
        Assert.False(viewModel.ShowCoverage);
        Assert.True(viewModel.HasData);

        viewModel.Update([WithData(0, 600, 500, 700, 10, 10), Empty(1, 10)]);
        Assert.True(viewModel.ShowCoverage);

        viewModel.Update([WithData(0, 600, 500, 700, 9, 10)]);
        Assert.True(viewModel.ShowCoverage);
    }

    [Fact]
    public void No_buckets_shows_no_chip_and_no_data()
    {
        var viewModel = new StatsViewModel();

        viewModel.Update([]);

        Assert.False(viewModel.ShowCoverage);
        Assert.False(viewModel.HasData);
    }

    [Fact]
    public async Task Values_are_given_as_text_in_the_unit_shown()
    {
        var settings = new SettingsService(new FakeSettingsStore { Stored = AppSettings.Default with { TemperatureUnit = TemperatureUnit.Fahrenheit } }, new StrongReferenceMessenger());
        await settings.LoadAsync(TestContext.Current.CancellationToken);
        var localizer = new FakeLocalizer();
        var viewModel = new StatsViewModel(new MetricFormatter(localizer, settings), localizer, new LocalCalendar(TimeZoneInfo.Utc));

        viewModel.Update([WithData(0, 20, 10, 30, 10, 10)], Metric.Temperature);

        Assert.Equal(("68.0 °F", "86.0 °F", "50.0 °F"), (viewModel.AverageText, viewModel.HighestText, viewModel.LowestText));
    }

    [Fact]
    public void Without_a_formatter_there_is_no_text()
    {
        var viewModel = new StatsViewModel();

        viewModel.Update([WithData(0, 600, 500, 700, 10, 10)]);

        Assert.Null(viewModel.AverageText);
    }
}

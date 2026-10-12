using Deskair.App.ViewModels.History;
using Deskair.Domain.Analytics.Aggregation;

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
}

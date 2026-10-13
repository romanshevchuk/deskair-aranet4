using Deskair.Domain.Analytics.Aggregation;

namespace Deskair.Tests.Domain.Analytics;

public class BucketStatisticsTests
{
    private static readonly DateTimeOffset start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private static Bucket Day(int day, double? average, int count = 10, BucketState state = BucketState.Complete)
        => new(start.AddDays(day), start.AddDays(day + 1), average, average, average, count, 10, state);

    [Fact]
    public void The_average_is_weighted_by_reading_count()
    {
        Assert.Equal(900, BucketStatistics.WeightedAverage([Day(0, 600, 1), Day(1, 1000, 3)]));
    }

    [Fact]
    public void Buckets_without_readings_do_not_count_in_the_average()
    {
        Assert.Equal(600, BucketStatistics.WeightedAverage([Day(0, 600), Day(1, null, 0, BucketState.NoData)]));
    }

    [Fact]
    public void No_readings_give_no_average()
    {
        Assert.Null(BucketStatistics.WeightedAverage([]));
        Assert.Null(BucketStatistics.WeightedAverage([Day(0, null, 0, BucketState.NoData)]));
    }

    [Fact]
    public void Lowest_and_highest_are_the_buckets_with_the_lowest_and_highest_average()
    {
        var buckets = new[] { Day(0, 800), Day(1, 500), Day(2, 1200) };

        Assert.Equal(start.AddDays(1), BucketStatistics.Lowest(buckets)!.Start);
        Assert.Equal(start.AddDays(2), BucketStatistics.Highest(buckets)!.Start);
    }

    [Fact]
    public void A_tie_goes_to_the_earliest_bucket()
    {
        var buckets = new[] { Day(0, 700), Day(1, 700) };

        Assert.Equal(start, BucketStatistics.Lowest(buckets)!.Start);
        Assert.Equal(start, BucketStatistics.Highest(buckets)!.Start);
    }

    [Fact]
    public void Partial_and_empty_buckets_are_never_chosen()
    {
        var buckets = new[] { Day(0, 300, 2, BucketState.Partial), Day(1, null, 0, BucketState.NoData), Day(2, 900), Day(3, 2000, 2, BucketState.Partial) };

        Assert.Equal(start.AddDays(2), BucketStatistics.Lowest(buckets)!.Start);
        Assert.Equal(start.AddDays(2), BucketStatistics.Highest(buckets)!.Start);
    }

    [Fact]
    public void Without_a_complete_bucket_there_is_no_lowest_or_highest()
    {
        Assert.Null(BucketStatistics.Lowest([Day(0, 300, 2, BucketState.Partial)]));
        Assert.Null(BucketStatistics.Highest([]));
    }
}

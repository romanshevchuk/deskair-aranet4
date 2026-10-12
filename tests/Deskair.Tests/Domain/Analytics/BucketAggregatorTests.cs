using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class BucketAggregatorTests
{
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 9, 12);
    private static readonly DateTimeOffset end = start.AddMinutes(10);
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);

    [Fact]
    public void Aggregate_uses_the_mean_of_existing_readings()
    {
        var readings = new List<Reading>
        {
            Samples.Co2(start, 800),
            Samples.Co2(start.AddMinutes(2), 1000)
        };

        var bucket = BucketAggregator.Aggregate(readings, Metric.Co2, start, end, interval);

        Assert.Equal(900, bucket.Average);
        Assert.Equal(800, bucket.Minimum);
        Assert.Equal(1000, bucket.Maximum);
        Assert.Equal(2, bucket.ReadingCount);
        Assert.Equal(5, bucket.ExpectedCount);
        Assert.Equal(BucketState.Partial, bucket.State);
    }

    [Fact]
    public void Aggregate_is_half_open_so_a_reading_at_the_end_belongs_to_the_next_bucket()
    {
        var readings = new List<Reading>
        {
            Samples.Co2(start, 700),
            Samples.Co2(end, 2000)
        };

        var bucket = BucketAggregator.Aggregate(readings, Metric.Co2, start, end, interval);

        Assert.Equal(1, bucket.ReadingCount);
        Assert.Equal(700, bucket.Maximum);
    }

    [Fact]
    public void Aggregate_returns_NoData_without_inventing_values()
    {
        var bucket = BucketAggregator.Aggregate([], Metric.Co2, start, end, interval);

        Assert.Equal(BucketState.NoData, bucket.State);
        Assert.Null(bucket.Average);
        Assert.Null(bucket.Minimum);
        Assert.Null(bucket.Maximum);
    }

    [Fact]
    public void Aggregate_reads_the_requested_metric()
    {
        var readings = new List<Reading> { Samples.Climate(start, 22.0, 40, 1010) };

        var bucket = BucketAggregator.Aggregate(readings, Metric.Temperature, start, end, interval);

        Assert.Equal(22.0, bucket.Average);
    }
}

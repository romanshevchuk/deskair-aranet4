using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class BucketExtremeTimesTests
{
    private static readonly DateTimeOffset start = Samples.Utc(2026, 10, 14);

    [Fact]
    public void A_bucket_knows_when_its_lowest_and_highest_reading_were_taken()
    {
        var readings = new[]
        {
            Samples.Co2(start.AddHours(8), 900),
            Samples.Co2(start.AddHours(14).AddMinutes(16), 1530),
            Samples.Co2(start.AddHours(4), 420),
            Samples.Co2(start.AddHours(20), 800)
        };

        var bucket = BucketAggregator.Aggregate(readings, Metric.Co2, start, start.AddDays(1), TimeSpan.FromMinutes(2));

        Assert.Equal(1530, bucket.Maximum);
        Assert.Equal(start.AddHours(14).AddMinutes(16), bucket.MaximumAt);
        Assert.Equal(420, bucket.Minimum);
        Assert.Equal(start.AddHours(4), bucket.MinimumAt);
    }

    [Fact]
    public void When_values_are_equal_the_first_one_counts()
    {
        var readings = new[] { Samples.Co2(start.AddHours(9), 700), Samples.Co2(start.AddHours(6), 700) };

        var bucket = BucketAggregator.Aggregate(readings, Metric.Co2, start, start.AddDays(1), TimeSpan.FromMinutes(2));

        Assert.Equal(start.AddHours(6), bucket.MaximumAt);
        Assert.Equal(start.AddHours(6), bucket.MinimumAt);
    }

    [Fact]
    public void An_empty_bucket_has_no_times()
    {
        var bucket = BucketAggregator.Aggregate([], Metric.Co2, start, start.AddDays(1), TimeSpan.FromMinutes(2));

        Assert.Null(bucket.MinimumAt);
        Assert.Null(bucket.MaximumAt);
    }

    [Fact]
    public void Readings_outside_the_bucket_are_not_counted()
    {
        var readings = new[] { Samples.Co2(start.AddHours(-1), 3000), Samples.Co2(start.AddHours(1), 800) };

        var bucket = BucketAggregator.Aggregate(readings, Metric.Co2, start, start.AddDays(1), TimeSpan.FromMinutes(2));

        Assert.Equal(start.AddHours(1), bucket.MaximumAt);
    }
}

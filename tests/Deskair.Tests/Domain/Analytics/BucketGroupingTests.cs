using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class BucketGroupingTests
{
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);

    [Fact(Skip = "TODO(HIST-03a)")]
    public void Group_by_day_gives_one_bucket_per_local_day_with_no_holes()
    {
        var calendar = new LocalCalendar(Samples.Kyiv);
        var start = Samples.Utc(2026, 3, 27, 22);
        var end = Samples.Utc(2026, 3, 30, 22);

        var buckets = BucketAggregator.Group([], Metric.Co2, start, end, Period.Day, calendar, interval);

        Assert.Equal(3, buckets.Count);
        Assert.All(buckets, bucket => Assert.Equal(BucketState.NoData, bucket.State));
    }

    [Fact(Skip = "TODO(HIST-03a)")]
    public void Group_by_day_makes_the_spring_forward_day_23_hours_long()
    {
        var calendar = new LocalCalendar(Samples.Kyiv);
        var start = Samples.Utc(2026, 3, 27, 22);
        var end = Samples.Utc(2026, 3, 30, 22);

        var buckets = BucketAggregator.Group([], Metric.Co2, start, end, Period.Day, calendar, interval);

        Assert.Equal(TimeSpan.FromHours(23), buckets[1].End - buckets[1].Start);
    }

    [Fact(Skip = "TODO(HIST-03a)")]
    public void Group_returns_nothing_for_an_empty_range()
    {
        var calendar = new LocalCalendar(TimeZoneInfo.Utc);
        var moment = Samples.Utc(2026, 10, 9);

        var buckets = BucketAggregator.Group([], Metric.Co2, moment, moment, Period.Day, calendar, interval);

        Assert.Empty(buckets);
    }
}

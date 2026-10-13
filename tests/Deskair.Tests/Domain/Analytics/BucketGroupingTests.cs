using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Domain.Analytics;

public class BucketGroupingTests
{
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);
    private static readonly LocalCalendar utc = new(TimeZoneInfo.Utc);

    // Local midnight of 28 March and of 31 March 2026 in Kyiv; summer time starts on 29 March.
    private static readonly DateTimeOffset kyivStart = Samples.Utc(2026, 3, 27, 22);
    private static readonly DateTimeOffset kyivEnd = Samples.Utc(2026, 3, 30, 21);

    [Fact]
    public void Group_by_day_gives_one_bucket_per_local_day_with_no_holes()
    {
        var calendar = new LocalCalendar(Samples.Kyiv);

        var buckets = BucketAggregator.Group([], Metric.Co2, kyivStart, kyivEnd, Period.Day, calendar, interval);

        Assert.Equal(3, buckets.Count);
        Assert.All(buckets, bucket => Assert.Equal(BucketState.NoData, bucket.State));
        Assert.Equal(kyivStart, buckets[0].Start);
        Assert.Equal(kyivEnd, buckets[^1].End);

        for (var index = 1; index < buckets.Count; index++)
        {
            Assert.Equal(buckets[index - 1].End, buckets[index].Start);
        }
    }

    [Fact]
    public void Group_by_day_makes_the_spring_forward_day_23_hours_long()
    {
        var calendar = new LocalCalendar(Samples.Kyiv);

        var buckets = BucketAggregator.Group([], Metric.Co2, kyivStart, kyivEnd, Period.Day, calendar, interval);

        Assert.Equal(TimeSpan.FromHours(23), buckets[1].End - buckets[1].Start);
    }

    [Fact]
    public void Group_returns_nothing_for_an_empty_range()
    {
        var moment = Samples.Utc(2026, 10, 9);

        var buckets = BucketAggregator.Group([], Metric.Co2, moment, moment, Period.Day, utc, interval);

        Assert.Empty(buckets);
    }

    [Fact]
    public void Group_clips_the_last_bucket_to_now_so_the_current_day_is_not_partial_just_for_being_unfinished()
    {
        var today = Samples.Utc(2026, 10, 9);
        var readings = Enumerable.Range(0, 360).Select(slot => Samples.Co2(today.AddMinutes(slot * 2), 800)).ToList();

        var buckets = BucketAggregator.Group(readings, Metric.Co2, Samples.Utc(2026, 10, 8), today.AddHours(12), Period.Day, utc, interval);

        Assert.Equal(2, buckets.Count);
        Assert.Equal(today.AddHours(12), buckets[1].End);
        Assert.Equal(360, buckets[1].ExpectedCount);
        Assert.Equal(BucketState.Complete, buckets[1].State);
        Assert.Equal(BucketState.NoData, buckets[0].State);
    }

    [Fact]
    public void Group_puts_each_reading_in_its_bucket_and_a_reading_on_the_boundary_in_the_next_one()
    {
        var firstDay = Samples.Utc(2026, 10, 8);
        var readings = new List<Reading>
        {
            Samples.Co2(firstDay.AddHours(10), 700),
            Samples.Co2(firstDay.AddDays(1), 900),
            Samples.Co2(firstDay.AddDays(1).AddHours(11), 1100)
        };

        var buckets = BucketAggregator.Group(readings, Metric.Co2, firstDay, firstDay.AddDays(2), Period.Day, utc, interval);

        Assert.Equal(1, buckets[0].ReadingCount);
        Assert.Equal(2, buckets[1].ReadingCount);
        Assert.Equal(1000, buckets[1].Average);
    }

    [Fact]
    public void Group_by_month_follows_calendar_months_and_clips_the_first_and_last_bucket()
    {
        var start = Samples.Utc(2026, 9, 15);
        var end = Samples.Utc(2026, 11, 10, 6);

        var buckets = BucketAggregator.Group([], Metric.Co2, start, end, Period.Month, utc, interval);

        Assert.Equal(3, buckets.Count);
        Assert.Equal((start, Samples.Utc(2026, 10, 1)), (buckets[0].Start, buckets[0].End));
        Assert.Equal((Samples.Utc(2026, 10, 1), Samples.Utc(2026, 11, 1)), (buckets[1].Start, buckets[1].End));
        Assert.Equal((Samples.Utc(2026, 11, 1), end), (buckets[2].Start, buckets[2].End));
    }

    [Fact]
    public void Group_by_week_counts_seven_day_weeks_back_from_the_end_and_clips_the_first_one()
    {
        var start = Samples.Utc(2026, 9, 10);
        var end = Samples.Utc(2026, 10, 9, 12);

        var buckets = BucketAggregator.Group([], Metric.Co2, start, end, Period.Week, utc, interval);

        Assert.Equal(5, buckets.Count);
        Assert.Equal((start, Samples.Utc(2026, 9, 12)), (buckets[0].Start, buckets[0].End));
        Assert.Equal((Samples.Utc(2026, 9, 12), Samples.Utc(2026, 9, 19)), (buckets[1].Start, buckets[1].End));
        Assert.Equal((Samples.Utc(2026, 10, 3), end), (buckets[4].Start, buckets[4].End));
    }

    [Fact]
    public void Group_by_week_gives_one_bucket_for_exactly_seven_days_ending_at_midnight()
    {
        var buckets = BucketAggregator.Group([], Metric.Co2, Samples.Utc(2026, 10, 3), Samples.Utc(2026, 10, 10), Period.Week, utc, interval);

        var week = Assert.Single(buckets);
        Assert.Equal(TimeSpan.FromDays(7), week.End - week.Start);
    }

    [Fact]
    public void Group_rejects_Year_because_a_year_is_a_range_not_a_grouping()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BucketAggregator.Group([], Metric.Co2, Samples.Utc(2026, 1, 1), Samples.Utc(2026, 10, 1), Period.Year, utc, interval));
    }

    [Fact]
    public void A_day_with_every_reading_of_its_night_is_complete_when_only_the_night_is_counted()
    {
        // 9 night hours at 2 minute intervals are 270 readings: 00:00 to 07:00 and 22:00 to midnight. Counted against the whole day that would be 37 %.
        var day = Samples.Utc(2026, 10, 12);
        var readings = Enumerable.Range(0, 210).Select(slot => Samples.Co2(day.AddMinutes(slot * 2), 700))
            .Concat(Enumerable.Range(0, 60).Select(slot => Samples.Co2(day.AddHours(22).AddMinutes(slot * 2), 800)))
            .ToList();

        var bucket = Assert.Single(BucketAggregator.Group(readings, Metric.Co2, day, day.AddDays(1), Period.Day, utc, interval, NightWindow.Default));

        Assert.Equal(270, bucket.ExpectedCount);
        Assert.Equal(270, bucket.ReadingCount);
        Assert.Equal(BucketState.Complete, bucket.State);
    }

    [Fact]
    public void A_range_inside_the_night_expects_readings_for_all_of_it()
    {
        var day = Samples.Utc(2026, 10, 12);
        var readings = Enumerable.Range(0, 30).Select(slot => Samples.Co2(day.AddHours(22).AddMinutes(slot * 2), 800)).ToList();

        var bucket = Assert.Single(BucketAggregator.Group(readings, Metric.Co2, day.AddHours(22), day.AddDays(1), Period.Day, utc, interval, NightWindow.Default));

        Assert.Equal(60, bucket.ExpectedCount);
        Assert.Equal(30, bucket.ReadingCount);
    }

    [Fact]
    public void Without_a_night_window_every_hour_is_expected()
    {
        var day = Samples.Utc(2026, 10, 12);

        var bucket = Assert.Single(BucketAggregator.Group([], Metric.Co2, day, day.AddDays(1), Period.Day, utc, interval));

        Assert.Equal(720, bucket.ExpectedCount);
    }
}

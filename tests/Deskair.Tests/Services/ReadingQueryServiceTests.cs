using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Analytics.HoursFilter;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Queries;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Services;

public class ReadingQueryServiceTests
{
    private static readonly DeviceId device = new("aranet4", "AA:01");
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 9, 12);

    private readonly FakeReadingStore store = new();
    private readonly ReadingQueryService service;

    public ReadingQueryServiceTests()
    {
        service = new ReadingQueryService(
            store,
            new DeviceRegistry(new FakeDeviceStore()),
            new FakeClock(now),
            new LocalCalendar(TimeZoneInfo.Utc));
    }

    private async Task<ReadingQueryService> ServiceForFiveMinuteSensorAsync()
    {
        var devices = new FakeDeviceStore();
        devices.Devices.Add(new DeviceInfo(
            device,
            "Office",
            null,
            [new IntervalChange(Samples.Utc(2026, 1, 1), TimeSpan.FromMinutes(5))],
            null));
        var registry = new DeviceRegistry(devices);
        await registry.LoadAsync(CancellationToken.None);

        return new ReadingQueryService(store, registry, new FakeClock(now), new LocalCalendar(TimeZoneInfo.Utc));
    }

    [Fact]
    public async Task Buckets_group_by_day_and_expect_readings_at_the_device_interval()
    {
        var firstDay = Samples.Utc(2026, 10, 8);
        store.Readings.AddRange(Enumerable.Range(0, 288).Select(slot => Samples.Co2(firstDay.AddMinutes(slot * 5), 800)));
        var queries = await ServiceForFiveMinuteSensorAsync();

        var buckets = await queries.GetBucketsAsync(
            device, Metric.Co2, firstDay, firstDay.AddDays(2), Period.Day, HoursFilterMode.AllDay, CancellationToken.None);

        Assert.Equal(2, buckets.Count);
        Assert.Equal(288, buckets[0].ExpectedCount);
        Assert.Equal(BucketState.Complete, buckets[0].State);
        Assert.Equal(BucketState.NoData, buckets[1].State);
        Assert.Equal((device, firstDay, firstDay.AddDays(2)), store.Requested);
    }

    [Fact]
    public async Task Buckets_apply_the_hours_filter_before_grouping()
    {
        var day = Samples.Utc(2026, 10, 8);
        store.Readings.AddRange([Samples.Co2(day.AddHours(12), 500), Samples.Co2(day.AddHours(23), 1200)]);
        var queries = await ServiceForFiveMinuteSensorAsync();

        var buckets = await queries.GetBucketsAsync(
            device, Metric.Co2, day, day.AddDays(1), Period.Day, HoursFilterMode.Night, CancellationToken.None);

        var bucket = Assert.Single(buckets);
        Assert.Equal(1, bucket.ReadingCount);
        Assert.Equal(1200, bucket.Average);
    }

    [Fact]
    public async Task Buckets_without_data_are_all_NoData()
    {
        var day = Samples.Utc(2026, 10, 6);
        var queries = await ServiceForFiveMinuteSensorAsync();

        var buckets = await queries.GetBucketsAsync(
            device, Metric.Co2, day, day.AddDays(3), Period.Day, HoursFilterMode.AllDay, CancellationToken.None);

        Assert.Equal(3, buckets.Count);
        Assert.All(buckets, bucket => Assert.Equal(BucketState.NoData, bucket.State));
    }

    [Fact]
    public async Task Live_returns_the_readings_of_the_window_ending_now_oldest_first()
    {
        store.Readings.AddRange(
        [
            Samples.Co2(now.AddHours(-1), 900),
            Samples.Co2(now.AddHours(-25), 500),
            Samples.Co2(now, 1000),
            Samples.Co2(now.AddHours(-23), 700)
        ]);

        var recent = await service.GetRecentAsync(device, TimeSpan.FromHours(24), CancellationToken.None);

        Assert.Equal(new[] { 700.0, 900.0, 1000.0 }, recent.Select(reading => reading.Co2Ppm));
        Assert.Equal((device, now.AddHours(-24), now), store.Requested);
    }

    [Fact]
    public async Task Live_returns_an_empty_list_without_readings()
    {
        var recent = await service.GetRecentAsync(device, TimeSpan.FromHours(24), CancellationToken.None);

        Assert.Empty(recent);
    }

    [Theory]
    [InlineData(Period.Day)]
    [InlineData(Period.Week)]
    public async Task TypicalDay_for_Day_and_Week_uses_the_last_7_days(Period period)
    {
        store.Readings.AddRange(
        [
            Samples.Co2(now.AddDays(-6).AddHours(-2), 800),
            Samples.Co2(now.AddDays(-8).AddHours(-2), 2000)
        ]);

        var hours = await service.GetTypicalDayAsync(device, period, now.AddDays(-30), now, CancellationToken.None);

        Assert.Equal(800, hours[10].Average);
        Assert.Equal(1, hours.Sum(hour => hour.ReadingCount));
        Assert.Equal((device, now.AddDays(-7), now), store.Requested);
    }

    [Fact]
    public async Task TypicalDay_for_Month_uses_the_selected_range()
    {
        var rangeStart = Samples.Utc(2026, 9, 1);
        var rangeEnd = Samples.Utc(2026, 10, 1);
        store.Readings.AddRange(
        [
            Samples.Co2(Samples.Utc(2026, 9, 15, 6), 600),
            Samples.Co2(Samples.Utc(2026, 10, 5, 6), 1500)
        ]);

        var hours = await service.GetTypicalDayAsync(device, Period.Month, rangeStart, rangeEnd, CancellationToken.None);

        Assert.Equal(600, hours[6].Average);
        Assert.Equal((device, rangeStart, rangeEnd), store.Requested);
    }

    [Fact]
    public async Task TypicalDay_for_Year_uses_the_last_90_days()
    {
        store.Readings.AddRange(
        [
            Samples.Co2(now.AddDays(-60).AddHours(-2), 900),
            Samples.Co2(now.AddDays(-100).AddHours(-2), 2000)
        ]);

        var hours = await service.GetTypicalDayAsync(device, Period.Year, now.AddDays(-365), now, CancellationToken.None);

        Assert.Equal(900, hours[10].Average);
        Assert.Equal(1, hours.Sum(hour => hour.ReadingCount));
        Assert.Equal((device, now.AddDays(-90), now), store.Requested);
    }

    [Fact]
    public async Task TypicalDay_without_data_gives_24_empty_hours()
    {
        var hours = await service.GetTypicalDayAsync(device, Period.Week, now.AddDays(-7), now, CancellationToken.None);

        Assert.Equal(24, hours.Count);
        Assert.All(hours, hour => Assert.Null(hour.Average));
    }

    [Fact]
    public async Task Zone_share_counts_the_readings_in_the_range_by_zone()
    {
        store.Readings.AddRange([
            Samples.Co2(now.AddHours(-3), 500),
            Samples.Co2(now.AddHours(-2), 1200),
            Samples.Co2(now.AddHours(-1), 1500),
            Samples.Co2(now.AddDays(-9), 500)
        ]);

        var share = await service.GetZoneShareAsync(device, now.AddDays(-1), now, HoursFilterMode.AllDay, CancellationToken.None);

        Assert.Equal((1.0 / 3, 1.0 / 3, 1.0 / 3), (share!.Good, share.Elevated, share.High));
    }

    [Fact]
    public async Task Zone_share_follows_the_hours_filter()
    {
        store.Readings.AddRange([
            Samples.Co2(Samples.Utc(2026, 10, 8, 23), 1500),
            Samples.Co2(Samples.Utc(2026, 10, 8, 12), 500)
        ]);

        var share = await service.GetZoneShareAsync(device, now.AddDays(-2), now, HoursFilterMode.Night, CancellationToken.None);

        Assert.Equal(1, share!.High);
    }

    [Fact]
    public async Task Zone_share_is_null_without_readings()
    {
        Assert.Null(await service.GetZoneShareAsync(device, now.AddDays(-1), now, HoursFilterMode.AllDay, CancellationToken.None));
    }

    [Fact]
    public async Task A_device_with_no_stored_readings_has_none()
    {
        Assert.False(await service.HasReadingsAsync(device, CancellationToken.None));
    }

    [Fact]
    public async Task A_device_with_a_stored_reading_has_readings()
    {
        store.Readings.Add(Samples.Co2(now, 800));

        Assert.True(await service.HasReadingsAsync(device, CancellationToken.None));
    }
}

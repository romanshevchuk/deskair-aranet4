using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Analytics.Aggregation;
using Deskair.Domain.Metrics;
using Deskair.Domain.Readings;
using Deskair.Domain.Time;
using Deskair.Services.Devices;
using Deskair.Services.Queries;
using Deskair.Services.Settings;
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
            device, Metric.Co2, firstDay, firstDay.AddDays(2), Period.Day, CancellationToken.None);

        Assert.Equal(2, buckets.Count);
        Assert.Equal(288, buckets[0].ExpectedCount);
        Assert.Equal(BucketState.Complete, buckets[0].State);
        Assert.Equal(BucketState.NoData, buckets[1].State);
        Assert.Equal((device, firstDay, firstDay.AddDays(2)), store.Requested);
    }

    [Fact]
    public async Task Buckets_without_data_are_all_NoData()
    {
        var day = Samples.Utc(2026, 10, 6);
        var queries = await ServiceForFiveMinuteSensorAsync();

        var buckets = await queries.GetBucketsAsync(
            device, Metric.Co2, day, day.AddDays(3), Period.Day, CancellationToken.None);

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
        Assert.Equal((device, now.AddHours(-24), now + TimeSpan.FromMinutes(1)), store.Requested);
    }

    [Fact]
    public async Task Live_includes_a_reading_stored_on_a_slot_just_after_now()
    {
        // Measured 25 s ago, stored on the nearest slot of the grid, which is 35 s ahead.
        store.Readings.AddRange([Samples.Co2(now.AddSeconds(35), 1100), Samples.Co2(now.AddSeconds(90), 1200)]);

        var recent = await service.GetRecentAsync(device, TimeSpan.FromHours(1), CancellationToken.None);

        Assert.Equal([1100], recent.Select(reading => reading.Co2Ppm));
    }

    [Fact]
    public async Task Live_returns_an_empty_list_without_readings()
    {
        var recent = await service.GetRecentAsync(device, TimeSpan.FromHours(24), CancellationToken.None);

        Assert.Empty(recent);
    }

    [Fact]
    public async Task TypicalDay_for_Day_uses_the_7_days_before_the_shown_day()
    {
        // behaviour-rules.md "Aggregation" rule 7: a day is compared with the week before it, so the shown day itself is left out.
        var dayStart = now.AddDays(-1);
        store.Readings.AddRange(
        [
            Samples.Co2(dayStart.AddDays(-6).AddHours(-2), 800),
            Samples.Co2(dayStart.AddHours(2), 2000),
            Samples.Co2(dayStart.AddDays(-8), 2000)
        ]);

        var hours = await service.GetTypicalDayAsync(device, Period.Day, dayStart, now, CancellationToken.None);

        Assert.Equal(800, hours[10].Average);
        Assert.Equal(1, hours.Sum(hour => hour.ReadingCount));
        Assert.Equal((device, dayStart.AddDays(-7), dayStart), store.Requested);
    }

    [Theory]
    [InlineData(Period.Week)]
    [InlineData(Period.Year)]
    public async Task TypicalDay_for_Week_and_Year_uses_the_shown_period(Period period)
    {
        var rangeStart = now.AddDays(period == Period.Week ? -7 : -365);
        store.Readings.AddRange(
        [
            Samples.Co2(now.AddDays(-5).AddHours(-2), 900),
            Samples.Co2(rangeStart.AddDays(-1), 2000)
        ]);

        var hours = await service.GetTypicalDayAsync(device, period, rangeStart, now, CancellationToken.None);

        Assert.Equal(900, hours[10].Average);
        Assert.Equal(1, hours.Sum(hour => hour.ReadingCount));
        Assert.Equal((device, rangeStart, now), store.Requested);
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

        var share = await service.GetZoneShareAsync(device, now.AddDays(-1), now, CancellationToken.None);

        Assert.Equal((1.0 / 3, 1.0 / 3, 1.0 / 3), (share!.Good, share.Elevated, share.High));
    }

    [Fact]
    public async Task Zone_share_is_null_without_readings()
    {
        Assert.Null(await service.GetZoneShareAsync(device, now.AddDays(-1), now, CancellationToken.None));
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

    private async Task<ReadingQueryService> ServiceWithNightAsync(TimeOnly nightStart, TimeOnly nightEnd)
    {
        var devices = new FakeDeviceStore();
        devices.Devices.Add(new DeviceInfo(device, "Office", null, [new IntervalChange(Samples.Utc(2026, 1, 1), TimeSpan.FromMinutes(30))], null));
        var registry = new DeviceRegistry(devices);
        await registry.LoadAsync(CancellationToken.None);
        var settings = new SettingsService(new FakeSettingsStore { Stored = AppSettings.Default with { NightStart = nightStart, NightEnd = nightEnd } }, new StrongReferenceMessenger());
        await settings.LoadAsync(CancellationToken.None);

        return new ReadingQueryService(store, registry, new FakeClock(now), new LocalCalendar(TimeZoneInfo.Utc), settings);
    }


    [Fact]
    public async Task Last_night_is_the_night_that_ended_this_morning_in_the_default_hours()
    {
        // now is 9 October 12:00, so last night is 8 October 22:00 to 9 October 07:00.
        store.Readings.AddRange([
            Samples.Co2(Samples.Utc(2026, 10, 8, 21, 30), 2000),
            Samples.Co2(Samples.Utc(2026, 10, 8, 22), 700),
            Samples.Co2(Samples.Utc(2026, 10, 9, 6, 30), 900),
            Samples.Co2(Samples.Utc(2026, 10, 9, 7), 2000)]);
        var queries = await ServiceWithNightAsync(new TimeOnly(22, 0), new TimeOnly(7, 0));

        var summary = await queries.GetLastNightAsync(device, Metric.Co2, CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(Samples.Utc(2026, 10, 8, 22), summary.Start);
        Assert.Equal(Samples.Utc(2026, 10, 9, 7), summary.End);
        Assert.Equal(2, summary.ReadingCount);
        Assert.Equal(800, summary.Average);
    }

    [Fact]
    public async Task Last_night_follows_the_hours_the_user_chose()
    {
        var queries = await ServiceWithNightAsync(new TimeOnly(23, 0), new TimeOnly(8, 0));

        var summary = await queries.GetLastNightAsync(device, Metric.Co2, CancellationToken.None);

        Assert.Equal(Samples.Utc(2026, 10, 8, 23), summary!.Start);
        Assert.Equal(Samples.Utc(2026, 10, 9, 8), summary.End);
        Assert.False(summary.HasData);
    }

    [Fact]
    public async Task Without_settings_last_night_uses_the_default_hours()
    {
        var summary = await service.GetLastNightAsync(device, Metric.Co2, CancellationToken.None);

        Assert.Equal(Samples.Utc(2026, 10, 8, 22), summary!.Start);
    }

    [Fact]
    public async Task An_empty_night_window_has_no_last_night()
    {
        var queries = await ServiceWithNightAsync(new TimeOnly(7, 0), new TimeOnly(7, 0));

        Assert.Null(await queries.GetLastNightAsync(device, Metric.Co2, CancellationToken.None));
    }

    private static IEnumerable<Reading> NightOfReadings(DateTimeOffset eveningStart, double ppm)
        => Enumerable.Range(0, 18).Select(slot => Samples.Co2(eveningStart.AddMinutes(slot * 30), ppm));

    [Fact]
    public async Task Nights_count_the_nights_that_start_in_the_period_and_give_the_best_and_worst()
    {
        // Two nights of 9 hours at 30-minute intervals. The 12:00 reading is outside every night and must not count.
        store.Readings.AddRange(NightOfReadings(Samples.Utc(2026, 10, 7, 22), 800));
        store.Readings.AddRange(NightOfReadings(Samples.Utc(2026, 10, 8, 22), 1200));
        store.Readings.Add(Samples.Co2(Samples.Utc(2026, 10, 8, 12), 2000));
        var queries = await ServiceWithNightAsync(new TimeOnly(22, 0), new TimeOnly(7, 0));

        var nights = await queries.GetNightsAsync(
            device, Metric.Co2, Samples.Utc(2026, 10, 7), Samples.Utc(2026, 10, 9), CancellationToken.None);

        Assert.NotNull(nights);
        Assert.Equal(2, nights.Count);
        Assert.Equal(1000, nights.Average);
        Assert.Equal(Samples.Utc(2026, 10, 7, 22), nights.Lowest!.Start);
        Assert.Equal(Samples.Utc(2026, 10, 8, 22), nights.Highest!.Start);
    }

    [Fact]
    public async Task An_empty_night_window_has_no_nights()
    {
        store.Readings.AddRange(NightOfReadings(Samples.Utc(2026, 10, 7, 22), 800));
        var queries = await ServiceWithNightAsync(new TimeOnly(7, 0), new TimeOnly(7, 0));

        Assert.Null(await queries.GetNightsAsync(
            device, Metric.Co2, Samples.Utc(2026, 10, 7), Samples.Utc(2026, 10, 9), CancellationToken.None));
    }
}

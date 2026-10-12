using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Sensors;
using Deskair.Services.Storage;
using Deskair.Services.Sync;
using Deskair.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace Deskair.Tests.Services;

public sealed class SyncServiceTests : IAsyncDisposable
{
    private static readonly DeviceId device = new("aranet4", "AA:01");
    private static readonly TimeSpan interval = TimeSpan.FromMinutes(2);
    private static readonly DateTimeOffset now = Samples.Utc(2026, 10, 9, 12);
    private static readonly DateTimeOffset oldSync = Samples.Utc(2026, 10, 8, 8);

    private readonly FakeReadingStore readings = new();
    private readonly FakeDeviceStore devices = new();
    private readonly FakeSensor sensor = new();
    private readonly ListLogger<SyncService> logger = new();
    private readonly SyncService service;

    public SyncServiceTests()
    {
        devices.Devices.Add(new DeviceInfo(device, "Office", null, [], oldSync));
        service = new SyncService(readings, devices, new FakeClock(now), logger);
    }

    public ValueTask DisposeAsync() => sensor.DisposeAsync();

    private static Reading At(int minutesBeforeNow, double ppm = 800) => Samples.Co2(now.AddMinutes(-minutesBeforeNow), ppm);

    private DateTimeOffset? SavedSync => devices.Devices.Single().LastSyncUtc;

    [Fact]
    public async Task Sync_merges_the_downloaded_history_as_device_readings_and_saves_the_sync_time()
    {
        sensor.History.AddRange([At(6), At(4), At(2)]);

        var result = await service.SyncAsync(sensor, device, interval, CancellationToken.None);

        Assert.Equal(new SyncResult(true, 3), result);
        var merge = Assert.Single(readings.Merges);
        Assert.Equal((device, ReadingSource.Device, interval), (merge.Device, merge.Source, merge.Interval));
        Assert.Equal(now, SavedSync);
    }

    [Fact]
    public async Task Sync_counts_only_slots_that_were_empty()
    {
        readings.Readings.Add(At(4));
        sensor.History.AddRange([At(6), At(4, 999), At(2)]);

        var result = await service.SyncAsync(sensor, device, interval, CancellationToken.None);

        Assert.Equal(new SyncResult(true, 2), result);
        Assert.Equal(800, readings.Readings.Single(reading => reading.Timestamp == now.AddMinutes(-4)).Co2Ppm);
    }

    [Fact]
    public async Task Sync_asks_for_history_since_the_last_stored_reading()
    {
        readings.Readings.Add(At(30));

        await service.SyncAsync(sensor, device, interval, CancellationToken.None);

        Assert.Equal(now.AddMinutes(-30), sensor.HistorySince);
    }

    [Fact]
    public async Task Sync_asks_for_seven_days_when_nothing_is_stored_yet()
    {
        await service.SyncAsync(sensor, device, interval, CancellationToken.None);

        Assert.Equal(now.AddDays(-7), sensor.HistorySince);
    }

    [Fact]
    public async Task Sync_that_loses_the_sensor_part_way_is_incomplete_stores_nothing_and_keeps_the_sync_time()
    {
        sensor.History.AddRange([At(6), At(4), At(2)]);
        sensor.FailHistoryAfter = 2;

        var result = await service.SyncAsync(sensor, device, interval, CancellationToken.None);

        Assert.Equal(new SyncResult(false, 0), result);
        Assert.Empty(readings.Merges);
        Assert.Empty(readings.Readings);
        Assert.Equal(oldSync, SavedSync);
        Assert.Empty(devices.Saved);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Exception is InvalidOperationException);
    }

    [Fact]
    public async Task Sync_that_is_cancelled_part_way_is_incomplete_stores_nothing_and_keeps_the_sync_time()
    {
        sensor.History.AddRange([At(6), At(4), At(2)]);
        sensor.PauseHistoryAfter = 2;
        using var cancel = new CancellationTokenSource();

        var syncing = service.SyncAsync(sensor, device, interval, cancel.Token);
        await sensor.HistoryPaused.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await cancel.CancelAsync();
        var result = await syncing;

        Assert.Equal(new SyncResult(false, 0), result);
        Assert.Empty(readings.Merges);
        Assert.Equal(oldSync, SavedSync);
    }

    [Fact]
    public async Task Sync_of_a_sensor_without_history_is_complete_and_does_nothing()
    {
        sensor.Capabilities = new SensorCapabilities(History: false, Battery: false, SignalStrength: false);
        sensor.History.Add(At(2));

        var result = await service.SyncAsync(sensor, device, interval, CancellationToken.None);

        Assert.Equal(new SyncResult(true, 0), result);
        Assert.Equal(0, sensor.HistoryCalls);
        Assert.Empty(readings.Merges);
        Assert.Equal(oldSync, SavedSync);
    }

    [Fact]
    public async Task Sync_stores_the_readings_even_if_the_device_has_no_device_file_yet()
    {
        devices.Devices.Clear();
        sensor.History.Add(At(2));

        var result = await service.SyncAsync(sensor, device, interval, CancellationToken.None);

        Assert.Equal(new SyncResult(true, 1), result);
        Assert.Empty(devices.Saved);
    }

    [Fact]
    public async Task Sync_of_an_empty_history_is_complete_and_still_saves_the_sync_time()
    {
        var result = await service.SyncAsync(sensor, device, interval, CancellationToken.None);

        Assert.Equal(new SyncResult(true, 0), result);
        Assert.Equal(now, SavedSync);
    }
}

using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Sensors;
using Deskair.Services.Storage;
using Microsoft.Extensions.Logging;

namespace Deskair.Services.Sync;

/// <summary>On launch, reconnect and resume: read history since the last stored slot and merge it.</summary>
public sealed class SyncService
{
    private readonly IReadingStore readings;
    private readonly IDeviceStore devices;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SyncService> logger;

    public SyncService(
        IReadingStore readings,
        IDeviceStore devices,
        TimeProvider timeProvider,
        ILogger<SyncService> logger)
    {
        this.readings = readings;
        this.devices = devices;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    private static readonly TimeSpan sensorMemory = TimeSpan.FromDays(7);

    public Task<SyncResult> SyncAsync(ISensor sensor, DeviceId device, TimeSpan interval, CancellationToken cancellationToken)
        => SyncAsync(sensor, device, interval, null, cancellationToken);

    /// <summary>Syncs, and passes the sensor's download progress on to <paramref name="progress"/> when it reports any.</summary>
    public async Task<SyncResult> SyncAsync(
        ISensor sensor,
        DeviceId device,
        TimeSpan interval,
        IProgress<HistoryProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!sensor.Capabilities.History)
        {
            return new SyncResult(true, 0);
        }

        var now = timeProvider.GetUtcNow();

        try
        {
            var since = await SinceAsync(device, interval, now, cancellationToken);
            var downloaded = await DownloadAsync(sensor, device, since, progress, cancellationToken);

            if (downloaded is null)
            {
                return new SyncResult(false, 0);
            }

            var added = downloaded.Count == 0
                ? 0
                : await readings.MergeAsync(device, downloaded, ReadingSource.Device, interval, cancellationToken);

            await SaveSyncTimeAsync(device, now, cancellationToken);

            return new SyncResult(true, added);
        }
        catch (OperationCanceledException)
        {
            return new SyncResult(false, 0);
        }
    }

    /// <summary>
    /// Where the history download starts: after the newest stored reading, or earlier when the last week has a hole in it (a slot the live polling skipped, or time the app was closed
    /// but the sensor kept measuring). Merging only fills empty slots, so asking for more than is missing is harmless.
    /// </summary>
    private async Task<DateTimeOffset> SinceAsync(DeviceId device, TimeSpan interval, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var last = await readings.GetLastReadingTimeAsync(device, cancellationToken);

        if (last is not { } newest)
        {
            return now - sensorMemory;
        }

        var stored = await readings.ReadRangeAsync(device, now - sensorMemory, newest, cancellationToken);
        var hole = FirstHole(stored, interval);

        return hole ?? newest;
    }

    /// <summary>The time of the stored reading just before the first hole (a distance of more than one and a half intervals), or null when there is none.</summary>
    private static DateTimeOffset? FirstHole(IReadOnlyList<Reading> stored, TimeSpan interval)
    {
        var ordered = stored.Select(reading => reading.Timestamp).Order().ToList();

        for (var index = 1; index < ordered.Count; index++)
        {
            if (ordered[index] - ordered[index - 1] > interval * 1.5)
            {
                return ordered[index - 1];
            }
        }

        return null;
    }

    private async Task<List<Reading>?> DownloadAsync(
        ISensor sensor,
        DeviceId device,
        DateTimeOffset since,
        IProgress<HistoryProgress>? progress,
        CancellationToken cancellationToken)
    {
        var downloaded = new List<Reading>();

        try
        {
            await foreach (var reading in sensor.ReadHistoryAsync(since, progress, cancellationToken).WithCancellation(cancellationToken))
            {
                downloaded.Add(reading);
            }

            return downloaded;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Downloading the history of {Provider} {Address} stopped early; nothing was stored.", device.ProviderId, device.Address);

            return null;
        }
    }

    private async Task SaveSyncTimeAsync(DeviceId device, DateTimeOffset syncedAt, CancellationToken cancellationToken)
    {
        var known = await devices.ListAsync(cancellationToken);
        var info = known.FirstOrDefault(candidate => candidate.Id == device);

        if (info is null)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("{Provider} {Address} has no device file yet, so the sync time was not saved.", device.ProviderId, device.Address);
            }

            return;
        }

        await devices.SaveAsync(info with { LastSyncUtc = syncedAt }, cancellationToken);
    }
}

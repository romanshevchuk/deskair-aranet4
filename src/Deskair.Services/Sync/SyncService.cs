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

    public async Task<SyncResult> SyncAsync(
        ISensor sensor,
        DeviceId device,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        if (!sensor.Capabilities.History)
        {
            return new SyncResult(true, 0);
        }

        var now = timeProvider.GetUtcNow();

        try
        {
            var since = await readings.GetLastReadingTimeAsync(device, cancellationToken) ?? now - sensorMemory;
            var downloaded = await DownloadAsync(sensor, device, since, cancellationToken);

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

    private async Task<List<Reading>?> DownloadAsync(
        ISensor sensor,
        DeviceId device,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        var downloaded = new List<Reading>();

        try
        {
            await foreach (var reading in sensor.ReadHistoryAsync(since, cancellationToken).WithCancellation(cancellationToken))
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

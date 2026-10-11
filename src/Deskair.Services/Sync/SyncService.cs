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

    // TODO(SYNC-02a) [L, sonnet]
    // Spec: docs/engineering/sync-and-import.md "Sensor sync"; docs/product/behaviour-rules.md "Sync and data".
    // Input : a connected sensor, its device, the interval from ConnectAsync, a CancellationToken.
    // Output: SyncResult with the number of newly filled slots.
    // Rules : read history since the last stored slot (or 7 days back), merge through IReadingStore.MergeAsync
    //         with ReadingSource.Device, then save LastSyncUtc through IDeviceStore.
    // Edge  : cancellation or a sensor error mid-download returns Complete = false and must NOT advance LastSyncUtc;
    //         a sensor without History capability returns Complete = true with 0 added.
    // Done  : add Services/SyncServiceTests (fake sensor, store, clock), including the cancellation case.
    public Task<SyncResult> SyncAsync(
        ISensor sensor,
        DeviceId device,
        TimeSpan interval,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(SYNC-02a)");
}

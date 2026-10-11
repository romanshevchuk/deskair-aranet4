using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Sensors;
using Deskair.Services.Storage;
using Microsoft.Extensions.Logging;

namespace Deskair.Services.Live;

/// <summary>Polls current readings at the sensor's interval, stores them and raises ReadingReceived.</summary>
public sealed class LiveFeed
{
    private readonly IReadingStore readings;
    private readonly IMessenger messenger;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<LiveFeed> logger;

    public LiveFeed(
        IReadingStore readings,
        IMessenger messenger,
        TimeProvider timeProvider,
        ILogger<LiveFeed> logger)
    {
        this.readings = readings;
        this.messenger = messenger;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    // TODO(LIVE-05a) [L, sonnet]
    // Spec: docs/engineering/architecture.md section 6 (LiveFeed); docs/engineering/sync-and-import.md.
    // Input : a connected sensor, its device, the interval, a CancellationToken.
    // Output: runs until cancelled; each poll stores the reading (ReadingSource.Device) and sends ReadingReceived.
    // Rules : wait one interval between polls using TimeProvider (so tests can advance time); a failed poll is logged and retried.
    // Edge  : cancellation ends the loop without throwing to the caller; never two polls in parallel.
    // Done  : add Services/LiveFeedTests with a fake sensor and FakeTimeProvider-style clock.
    public Task RunAsync(
        ISensor sensor,
        DeviceId device,
        TimeSpan interval,
        CancellationToken cancellationToken)
        => throw new NotImplementedException("TODO(LIVE-05a)");
}

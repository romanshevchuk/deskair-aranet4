using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Messages;
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

    public async Task RunAsync(
        ISensor sensor,
        DeviceId device,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await PollAsync(sensor, device, interval, cancellationToken);
                await Task.Delay(interval, timeProvider, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelling is how the loop is stopped; it is not an error.
        }
    }

    private async Task PollAsync(ISensor sensor, DeviceId device, TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            var reading = await sensor.ReadCurrentAsync(cancellationToken);

            await readings.MergeAsync(device, [reading], ReadingSource.Device, interval, cancellationToken);
            messenger.Send(new ReadingReceived(device, reading));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Reading the current values of {Provider} {Address} failed; trying again in {Interval}.", device.ProviderId, device.Address, interval);
        }
    }
}

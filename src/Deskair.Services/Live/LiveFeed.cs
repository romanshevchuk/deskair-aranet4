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
    /// <summary>How long one current reading may take. A sensor that left range can stay "connected" for Windows and never answer; after this the read counts as a lost connection.</summary>
    public static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(30);

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

    /// <summary>
    /// Reads the current values once and raises ReadingReceived, without storing the reading. A failed read is logged and not thrown.
    /// It shows a value right after connecting; it must not be stored, because the history sync asks the sensor for everything after the newest stored reading,
    /// and the history download contains this reading anyway.
    /// </summary>
    public Task PollOnceAsync(ISensor sensor, DeviceId device, TimeSpan interval, CancellationToken cancellationToken)
        => PollAsync(sensor, device, interval, store: false, rethrowConnectionFailure: false, cancellationToken);

    /// <summary>
    /// Polls at the sensor's interval until cancelled. A failed read is logged and the next interval tries again, except when the connection itself failed (<see cref="IOException"/>,
    /// <see cref="TimeoutException"/>): that is logged and thrown, so <see cref="SensorSession"/> opens a new connection instead of polling a dead one.
    /// A read that takes longer than <see cref="ReadTimeout"/> counts as a lost connection too.
    /// With <paramref name="pollFirst"/> false the first poll waits one interval, for a caller that has just read.
    /// </summary>
    public async Task RunAsync(
        ISensor sensor,
        DeviceId device,
        TimeSpan interval,
        CancellationToken cancellationToken,
        bool pollFirst = true)
    {
        var skip = !pollFirst;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!skip)
                {
                    await PollAsync(sensor, device, interval, store: true, rethrowConnectionFailure: true, cancellationToken);
                }

                skip = false;
                await Task.Delay(interval, timeProvider, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelling is how the loop is stopped; it is not an error.
        }
    }

    private async Task<Reading> ReadWithTimeoutAsync(ISensor sensor, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(ReadTimeout, timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            return await sensor.ReadCurrentAsync(linked.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The sensor did not answer within {ReadTimeout.TotalSeconds:0} s.");
        }
    }

    private async Task PollAsync(ISensor sensor, DeviceId device, TimeSpan interval, bool store, bool rethrowConnectionFailure, CancellationToken cancellationToken)
    {
        try
        {
            var reading = await ReadWithTimeoutAsync(sensor, cancellationToken);

            if (store)
            {
                await readings.MergeAsync(device, [reading], ReadingSource.Device, interval, cancellationToken);
            }

            messenger.Send(new ReadingReceived(device, reading));

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Live reading from {Address}: {Co2} ppm at {Time:HH:mm:ss}.", device.Address, reading.Co2Ppm, reading.Timestamp);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Reading the current values of {Provider} {Address} failed; trying again in {Interval}.", device.ProviderId, device.Address, interval);

            if (rethrowConnectionFailure && exception is IOException or TimeoutException)
            {
                throw;
            }
        }
    }
}

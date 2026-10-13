using CommunityToolkit.Mvvm.Messaging;
using Deskair.Domain.Readings;
using Deskair.Services.Messages;
using Deskair.Services.Sensors;
using Deskair.Services.Sync;
using Microsoft.Extensions.Logging;

namespace Deskair.Services.Live;

/// <summary>
/// Runs one device: connect, download the history, then poll live readings until stopped. If the sensor cannot be reached the session
/// tries again every 30 seconds. Starting another device stops the one that is running.
/// </summary>
public sealed class SensorSession : IAsyncDisposable, IDisposable
{
    /// <summary>How long one attempt to connect may take. Windows can wait a long time for a sensor that is not there; two minutes also leaves time to type a pairing PIN, which happens inside the connect.</summary>
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromMinutes(2);

    private static readonly TimeSpan retryAfter = TimeSpan.FromSeconds(30);

    private readonly IEnumerable<ISensorProvider> providers;
    private readonly LiveFeed liveFeed;
    private readonly SyncService sync;
    private readonly IMessenger messenger;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SensorSession> logger;
    private readonly SemaphoreSlim gate = new(1, 1);
    private CancellationTokenSource? running;
    private Task? loop;

    public SensorSession(
        IEnumerable<ISensorProvider> providers,
        LiveFeed liveFeed,
        SyncService sync,
        IMessenger messenger,
        TimeProvider timeProvider,
        ILogger<SensorSession> logger)
    {
        this.providers = providers;
        this.liveFeed = liveFeed;
        this.sync = sync;
        this.messenger = messenger;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task StartAsync(DeviceId device)
    {
        // Retry, resuming from sleep and choosing a room can all ask for a start at about the same time: one at a time, so no loop is left running that nobody can stop.
        await gate.WaitAsync();

        try
        {
            await StopCoreAsync();

            var provider = providers.FirstOrDefault(candidate => candidate.Id == device.ProviderId);

            if (provider is null)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError("No sensor provider named {Provider} for {Address}.", device.ProviderId, device.Address);
                }

                return;
            }

            running = new CancellationTokenSource();
            var token = running.Token;

            // On the thread pool, so the loop never resumes on the UI thread (which may be busy or shutting down).
            loop = Task.Run(() => RunAsync(provider, device, token));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await gate.WaitAsync();

        try
        {
            await StopCoreAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task StopCoreAsync()
    {
        if (running is null)
        {
            return;
        }

        await running.CancelAsync();

        if (loop is not null)
        {
            await loop;
        }

        running.Dispose();
        running = null;
        loop = null;
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    /// <summary>Stops the session without waiting for it. The app host disposes services synchronously at exit.</summary>
    public void Dispose()
    {
        running?.Cancel();
    }

    private async Task RunAsync(ISensorProvider provider, DeviceId device, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var sensor = provider.Create(device);
                var status = await ConnectAsync(sensor, cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Connected to {Address}; interval {Interval}, battery {Battery} %.", device.Address, status.Interval, status.BatteryPercent);
                }

                messenger.Send(new SensorStatusUpdated(device, status));

                // Show the current value at once: the first history download can take half a minute.
                await liveFeed.PollOnceAsync(sensor, device, status.Interval, cancellationToken);

                SyncResult result;
                messenger.Send(new SyncStarted(device));

                try
                {
                    result = await sync.SyncAsync(sensor, device, status.Interval, new ProgressMessages(messenger, device), cancellationToken);
                }
                catch
                {
                    // Whatever went wrong, the "syncing" note must not stay on screen.
                    messenger.Send(new SyncCompleted(device, 0));

                    throw;
                }

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("History sync for {Address} finished: {Added} readings added, complete {Complete}.", device.Address, result.ReadingsAdded, result.Complete);
                }

                messenger.Send(new SyncCompleted(device, result.ReadingsAdded));

                await liveFeed.RunAsync(sensor, device, status.Interval, cancellationToken, pollFirst: false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(exception, "The sensor {Address} could not be reached; trying again in {Seconds} s.", device.Address, retryAfter.TotalSeconds);
                }

                messenger.Send(new SensorConnectionFailed(device));
            }

            try
            {
                await Task.Delay(retryAfter, timeProvider, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task<SensorStatus> ConnectAsync(ISensor sensor, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(ConnectTimeout, timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            return await sensor.ConnectAsync(linked.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The sensor did not answer within {ConnectTimeout.TotalSeconds:0} s.");
        }
    }

    /// <summary>Turns the sensor's download progress into messages, as it arrives (no thread hop).</summary>
    private sealed class ProgressMessages : IProgress<HistoryProgress>
    {
        private readonly IMessenger messenger;
        private readonly DeviceId device;

        public ProgressMessages(IMessenger messenger, DeviceId device)
        {
            this.messenger = messenger;
            this.device = device;
        }

        public void Report(HistoryProgress value) => messenger.Send(new SyncProgressed(device, value));
    }
}

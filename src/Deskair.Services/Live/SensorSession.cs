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
    private static readonly TimeSpan retryAfter = TimeSpan.FromSeconds(30);

    private readonly IEnumerable<ISensorProvider> providers;
    private readonly LiveFeed liveFeed;
    private readonly SyncService sync;
    private readonly IMessenger messenger;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SensorSession> logger;
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
        await StopAsync();

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

    public async Task StopAsync()
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
                var status = await sensor.ConnectAsync(cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Connected to {Address}; interval {Interval}, battery {Battery} %.", device.Address, status.Interval, status.BatteryPercent);
                }

                messenger.Send(new SensorStatusUpdated(device, status));

                var result = await sync.SyncAsync(sensor, device, status.Interval, cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("History sync for {Address} finished: {Added} readings added, complete {Complete}.", device.Address, result.ReadingsAdded, result.Complete);
                }

                messenger.Send(new SyncCompleted(device, result.ReadingsAdded));

                await liveFeed.RunAsync(sensor, device, status.Interval, cancellationToken);
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
}

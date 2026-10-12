using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Platform;
using Microsoft.Extensions.Logging;

namespace Deskair.Services.Connection;

/// <summary>
/// Keeps the connection state of the selected device up to date and announces changes as <see cref="ConnectionStateChanged"/>.
/// It listens for readings and sensor status, and re-checks on a timer because silence is what makes a state stale.
/// </summary>
public sealed class ConnectionTracker
{
    private static readonly TimeSpan checkEvery = TimeSpan.FromSeconds(15);

    private readonly DeviceRegistry devices;
    private readonly IBluetoothRadio radio;
    private readonly IMessenger messenger;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ConnectionTracker> logger;
    private readonly Lock gate = new();
    private DateTimeOffset? lastReadingAt;
    private int? batteryPercent;

    public ConnectionTracker(DeviceRegistry devices, IBluetoothRadio radio, IMessenger messenger, TimeProvider timeProvider, ILogger<ConnectionTracker> logger)
    {
        this.logger = logger;
        this.devices = devices;
        this.radio = radio;
        this.messenger = messenger;
        this.timeProvider = timeProvider;
    }

    public ConnectionState Current { get; private set; } = ConnectionState.FirstRun;

    /// <summary>
    /// Works out the first state, starts listening and starts the background check that notices silence. Everything stops when
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    public Task StartAsync(DateTimeOffset? lastReading, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            lastReadingAt = lastReading;
            batteryPercent = null;
        }

        messenger.Register<ConnectionTracker, ReadingReceived>(this, static (self, message) => self.OnReading(message));
        messenger.Register<ConnectionTracker, SensorStatusUpdated>(this, static (self, message) => self.OnStatus(message));
        messenger.Register<ConnectionTracker, DeviceSelected>(this, static (self, _) => self.OnDeviceSelected());
        radio.StateChanged += OnRadioChanged;
        cancellationToken.Register(() =>
        {
            messenger.UnregisterAll(this);
            radio.StateChanged -= OnRadioChanged;
        });

        Evaluate();

        // The loop runs until the first wait here, so its timer exists when this returns; after that it continues on the thread pool.
        // Failures are logged inside the loop, so nothing is lost by not awaiting it.
        _ = WatchAsync(cancellationToken);

        return Task.CompletedTask;
    }

    public void Evaluate()
    {
        ConnectionState state;

        lock (gate)
        {
            var now = timeProvider.GetUtcNow();
            var selected = devices.Selected;
            var interval = selected is { } device ? devices.IntervalFor(device, now) : TimeSpan.FromMinutes(2);

            state = ConnectionMonitor.Derive(new ConnectionInputs(selected is not null, lastReadingAt, now, interval, radio.IsOn, batteryPercent));

            if (state == Current)
            {
                return;
            }

            Current = state;
        }

        messenger.Send(new ConnectionStateChanged(state));
    }

    private async Task WatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(checkEvery, timeProvider, cancellationToken).ConfigureAwait(false);
                CheckSafely();
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelling is how the loop is stopped; it is not an error.
        }
    }

    private void CheckSafely()
    {
        try
        {
            Evaluate();
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Checking the connection state failed; trying again in {Seconds} s.", checkEvery.TotalSeconds);
            }
        }
    }

    private void OnRadioChanged(object? sender, EventArgs args) => Evaluate();

    private void OnReading(ReadingReceived message)
    {
        if (message.Device != devices.Selected)
        {
            return;
        }

        lock (gate)
        {
            lastReadingAt = message.Reading.Timestamp;
        }

        Evaluate();
    }

    private void OnStatus(SensorStatusUpdated message)
    {
        if (message.Device != devices.Selected)
        {
            return;
        }

        lock (gate)
        {
            batteryPercent = message.Status.BatteryPercent;
        }

        Evaluate();
    }

    private void OnDeviceSelected()
    {
        lock (gate)
        {
            lastReadingAt = null;
            batteryPercent = null;
        }

        Evaluate();
    }
}

using CommunityToolkit.Mvvm.Messaging;
using Deskair.Services.Devices;
using Deskair.Services.Messages;
using Deskair.Services.Platform;

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
    private readonly Lock gate = new();
    private DateTimeOffset? lastReadingAt;
    private int? batteryPercent;

    public ConnectionTracker(DeviceRegistry devices, IBluetoothRadio radio, IMessenger messenger, TimeProvider timeProvider)
    {
        this.devices = devices;
        this.radio = radio;
        this.messenger = messenger;
        this.timeProvider = timeProvider;
    }

    public ConnectionState Current { get; private set; } = ConnectionState.FirstRun;

    /// <summary>Starts listening and works out the first state. Listening stops when <paramref name="cancellationToken"/> is cancelled.</summary>
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

        return Task.CompletedTask;
    }

    /// <summary>Starts, then checks every 15 seconds until cancelled.</summary>
    public async Task RunAsync(DateTimeOffset? lastReading, CancellationToken cancellationToken)
    {
        await StartAsync(lastReading, cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(checkEvery, timeProvider, cancellationToken);
                Evaluate();
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelling is how the loop is stopped; it is not an error.
        }
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

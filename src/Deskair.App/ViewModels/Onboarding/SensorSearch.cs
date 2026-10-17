using Deskair.Services.Platform;
using Deskair.Services.Sensors;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Onboarding;

/// <summary>
/// Looks for sensors with every provider and keeps the state of the search: what was found, whether a scan is running, whether nothing was found, whether Bluetooth is off.
/// It knows nothing about the UI; <see cref="Changed"/> can fire on any thread, and the listener reads the state again. Each <see cref="Start"/> begins a new search,
/// and a search that was replaced can no longer change the state.
/// </summary>
public sealed class SensorSearch
{
    /// <summary>How long a scan may find nothing before the screen says so.</summary>
    public static readonly TimeSpan NoSensorAfter = TimeSpan.FromSeconds(15);

    /// <summary>A scan that has found nothing is restarted after this long. A fresh scan often finds a sensor at once when the first one saw nothing for a long time.</summary>
    public static readonly TimeSpan RescanAfter = TimeSpan.FromSeconds(8);

    private readonly IEnumerable<ISensorProvider> providers;
    private readonly IBluetoothRadio radio;
    private readonly TimeProvider timeProvider;
    private readonly ILogger logger;
    private readonly Lock gate = new();
    private CancellationTokenSource? running;
    private CancellationTokenRegistration lifetimeRegistration;
    private CancellationToken lifetime;
    private List<DiscoveredSensor> nearby = [];
    private bool isScanning;
    private bool noSensorFound;
    private bool bluetoothOff;
    private bool active;
    private int generation;

    public SensorSearch(IEnumerable<ISensorProvider> providers, IBluetoothRadio radio, TimeProvider timeProvider, ILogger logger)
    {
        this.providers = providers;
        this.radio = radio;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    /// <summary>Raised after any part of the state changed, on whichever thread caused it.</summary>
    public event EventHandler? Changed;

    public IReadOnlyList<DiscoveredSensor> Nearby => Read(() => nearby);

    public bool IsScanning => Read(() => isScanning);

    /// <summary>True when nothing was found within <see cref="NoSensorAfter"/>, or the scan ended without a sensor.</summary>
    public bool NoSensorFound => Read(() => noSensorFound);

    /// <summary>True when the scan could not run, or had to stop, because Bluetooth is off.</summary>
    public bool BluetoothOff => Read(() => bluetoothOff);

    /// <summary>Starts scanning in the background until <paramref name="cancellationToken"/> is cancelled, <see cref="Stop"/> is called or a sensor is paired. Calling it again restarts the search.</summary>
    public void Start(CancellationToken cancellationToken)
    {
        CancellationTokenSource next;
        int mine;

        lock (gate)
        {
            lifetime = cancellationToken;
            active = true;
            radio.StateChanged -= OnRadioTurnedOn;
            radio.StateChanged += OnRadioTurnedOn;
            lifetimeRegistration.Dispose();
            lifetimeRegistration = cancellationToken.Register(() => radio.StateChanged -= OnRadioTurnedOn);
            running?.Cancel();
            next = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            running = next;
            mine = ++generation;
        }

        _ = RunAsync(mine, next);
    }

    /// <summary>Stops the search; it starts again only with <see cref="Start"/>.</summary>
    public void Stop()
    {
        lock (gate)
        {
            active = false;
            running?.Cancel();
        }
    }

    /// <summary>Scans with every provider until it is cancelled or Bluetooth goes off. Stopping is not an error.</summary>
    public async Task ScanAsync(CancellationToken cancellationToken)
    {
        Update(null, () =>
        {
            nearby = [];
            bluetoothOff = !radio.IsOn;
        });

        if (BluetoothOff)
        {
            return;
        }

        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        radio.StateChanged += OnRadioChanged;

        try
        {
            OnRadioChanged(radio, EventArgs.Empty);
            await Task.WhenAll(providers.ToList().Select(provider => CollectAsync(provider, source.Token)));
        }
        finally
        {
            radio.StateChanged -= OnRadioChanged;
        }

        void OnRadioChanged(object? sender, EventArgs args)
        {
            if (!radio.IsOn)
            {
                Update(null, () => bluetoothOff = true);
                source.Cancel();
            }
        }
    }

    private T Read<T>(Func<T> read)
    {
        lock (gate)
        {
            return read();
        }
    }

    /// <summary>Changes the state; a change from a search that has been replaced (<paramref name="owner"/> is not the current one) is dropped.</summary>
    private void Update(int? owner, Action change)
    {
        lock (gate)
        {
            if (owner is { } id && id != generation)
            {
                return;
            }

            change();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Bluetooth came on (for example just after the app started, before Windows had reported it): scan again.</summary>
    private void OnRadioTurnedOn(object? sender, EventArgs args)
    {
        CancellationToken token;

        lock (gate)
        {
            if (!active || !radio.IsOn || !bluetoothOff || lifetime.IsCancellationRequested)
            {
                return;
            }

            token = lifetime;
        }

        Start(token);
    }

    private async Task RunAsync(int owner, CancellationTokenSource source)
    {
        var cancellationToken = source.Token;
        Update(owner, () =>
        {
            isScanning = true;
            noSensorFound = false;
        });
        _ = WatchForSilenceAsync(owner, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Looking for sensors (Bluetooth on: {BluetoothOn}).", radio.IsOn);
        }

        try
        {
            var again = true;

            while (again && !cancellationToken.IsCancellationRequested)
            {
                again = await ScanOnceAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(exception, "Scanning for sensors failed.");
            }
        }
        finally
        {
            Update(owner, () => isScanning = false);
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            Update(owner, () => noSensorFound = nearby.Count == 0);
        }

        Release(source);
    }

    private void Release(CancellationTokenSource source)
    {
        lock (gate)
        {
            if (running == source)
            {
                running = null;
            }

            source.Dispose();
        }
    }

    /// <summary>One scan. Returns true when it was stopped only because it found nothing for <see cref="RescanAfter"/>, so the caller scans again.</summary>
    private async Task<bool> ScanOnceAsync(CancellationToken cancellationToken)
    {
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var watchdog = RestartIfEmptyAsync(attempt);

        await ScanAsync(attempt.Token);
        await attempt.CancelAsync();

        return await watchdog && !cancellationToken.IsCancellationRequested;
    }

    private async Task<bool> RestartIfEmptyAsync(CancellationTokenSource attempt)
    {
        try
        {
            await Task.Delay(RescanAfter, timeProvider, attempt.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (Nearby.Count > 0)
        {
            return false;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("No sensor found in {Seconds} s; scanning again.", RescanAfter.TotalSeconds);
        }

        await attempt.CancelAsync();

        return true;
    }

    private async Task WatchForSilenceAsync(int owner, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(NoSensorAfter, timeProvider, cancellationToken);

            Update(owner, () =>
            {
                if (nearby.Count == 0)
                {
                    noSensorFound = true;
                }
            });
        }
        catch (OperationCanceledException)
        {
            // The scan was stopped or restarted before the time was up.
        }
    }

    private async Task CollectAsync(ISensorProvider provider, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var sensor in provider.ScanAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                Add(sensor, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancelling is how a scan is stopped.
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Scanning for {Provider} sensors failed.", provider.Id);
            }
        }
    }

    private void Add(DiscoveredSensor sensor, CancellationToken cancellationToken)
    {
        var isNew = false;

        Update(null, () =>
        {
            // A scan that was stopped must not add to the list of the scan that replaced it.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var list = nearby.ToList();
            var index = list.FindIndex(known => known.Id == sensor.Id);

            if (index >= 0)
            {
                list[index] = sensor;
            }
            else
            {
                list.Add(sensor);
                isNew = true;
            }

            nearby = list;
        });

        if (isNew && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Found sensor {Address} ({Name}), signal {Signal} dBm.", sensor.Id.Address, sensor.Name, sensor.SignalDbm);
        }
    }
}

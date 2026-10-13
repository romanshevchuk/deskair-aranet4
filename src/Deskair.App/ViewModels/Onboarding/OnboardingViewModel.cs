using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Deskair.App.Localization;
using Deskair.Domain.Readings;
using Deskair.Services.Devices;
using Deskair.Services.Platform;
using Deskair.Services.Sensors;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Onboarding;

/// <summary>
/// First run: the checklist, the sensors found nearby, the room name, and pairing. Pairing saves the room, selects it and starts the sensor session
/// (<see cref="DeviceEnrollment"/>); the shell then leaves this screen because a sensor exists.
/// </summary>
public sealed partial class OnboardingViewModel : ObservableObject
{
    /// <summary>How long a scan may find nothing before the screen says so.</summary>
    public static readonly TimeSpan NoSensorAfter = TimeSpan.FromSeconds(15);

    /// <summary>A scan that has found nothing is restarted after this long. A fresh scan often finds a sensor at once when the first one saw nothing for a long time.</summary>
    public static readonly TimeSpan RescanAfter = TimeSpan.FromSeconds(8);

    private readonly IEnumerable<ISensorProvider> providers;
    private readonly DeviceEnrollment enrollment;
    private readonly IBluetoothRadio radio;
    private readonly ILocalizer localizer;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<OnboardingViewModel> logger;
    private readonly Lock gate = new();
    private CancellationTokenSource? scan;
    private CancellationTokenSource? running;
    private CancellationToken lifetime;
    private bool active;

    [ObservableProperty]
    private int stepIndex;

    [ObservableProperty]
    private IReadOnlyList<DiscoveredSensor> nearby = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSearching))]
    [NotifyPropertyChangedFor(nameof(ShowNoSensor))]
    [NotifyPropertyChangedFor(nameof(ShowBluetoothOff))]
    private IReadOnlyList<NearbySensorItem> nearbySensors = [];

    [ObservableProperty]
    private IReadOnlyList<OnboardingStep> checklist = [];

    [ObservableProperty]
    private string roomName;

    /// <summary>True when the scan could not run, or had to stop, because Bluetooth is off. The view shows the Bluetooth hint.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSearching))]
    [NotifyPropertyChangedFor(nameof(ShowNoSensor))]
    [NotifyPropertyChangedFor(nameof(ShowBluetoothOff))]
    private bool bluetoothOff;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSearching))]
    private bool isScanning;

    /// <summary>True when nothing was found within <see cref="NoSensorAfter"/>, or the scan ended without a sensor. The view offers Retry.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSearching))]
    [NotifyPropertyChangedFor(nameof(ShowNoSensor))]
    private bool noSensorFound;

    /// <summary>True while the sensor search is shown on top of Live because the user asked to add another sensor (not at first run). The view then offers Cancel.</summary>
    [ObservableProperty]
    private bool isOpen;

    [ObservableProperty]
    private bool isPairing;

    [ObservableProperty]
    private bool pairFailed;

    /// <summary>"Looking for sensors": a scan is running, nothing is found yet and nothing is wrong.</summary>
    public bool ShowSearching => IsScanning && !NoSensorFound && !BluetoothOff && NearbySensors.Count == 0;

    /// <summary>"No sensor found" with Retry.</summary>
    public bool ShowNoSensor => NoSensorFound && !BluetoothOff && NearbySensors.Count == 0;

    /// <summary>The Bluetooth hint with Retry.</summary>
    public bool ShowBluetoothOff => BluetoothOff && NearbySensors.Count == 0;

    public OnboardingViewModel(
        IEnumerable<ISensorProvider> providers,
        DeviceEnrollment enrollment,
        IBluetoothRadio radio,
        ILocalizer localizer,
        TimeProvider timeProvider,
        ILogger<OnboardingViewModel> logger)
    {
        this.providers = providers;
        this.enrollment = enrollment;
        this.radio = radio;
        this.localizer = localizer;
        this.timeProvider = timeProvider;
        this.logger = logger;
        roomName = localizer.Get("Onboarding_Room_DefaultName");
        checklist = BuildChecklist();
    }

    /// <summary>Starts scanning in the background until <paramref name="cancellationToken"/> is cancelled or a sensor is paired. Calling it again restarts the scan.</summary>
    public void Start(CancellationToken cancellationToken)
    {
        lifetime = cancellationToken;
        active = true;
        radio.StateChanged -= OnRadioTurnedOn;
        radio.StateChanged += OnRadioTurnedOn;
        cancellationToken.Register(() => radio.StateChanged -= OnRadioTurnedOn);
        var next = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previous = Interlocked.Exchange(ref running, next);
        previous?.Cancel();

        _ = RunAsync(next.Token);
    }

    /// <summary>Scans with every provider until it is cancelled, the step changes or Bluetooth goes off. Stopping is not an error.</summary>
    public async Task ScanAsync(CancellationToken cancellationToken)
    {
        Nearby = [];
        BluetoothOff = !radio.IsOn;

        if (BluetoothOff)
        {
            return;
        }

        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        scan = source;
        radio.StateChanged += OnRadioChanged;

        try
        {
            OnRadioChanged(radio, EventArgs.Empty);
            await Task.WhenAll(providers.ToList().Select(provider => CollectAsync(provider, source.Token)));
        }
        finally
        {
            radio.StateChanged -= OnRadioChanged;
            scan = null;
        }

        void OnRadioChanged(object? sender, EventArgs args)
        {
            if (!radio.IsOn)
            {
                BluetoothOff = true;
                source.Cancel();
            }
        }
    }

    partial void OnStepIndexChanged(int value)
    {
        scan?.Cancel();
    }

    partial void OnNearbyChanged(IReadOnlyList<DiscoveredSensor> value)
    {
        NearbySensors = [.. value.Select(ToItem)];
        Checklist = BuildChecklist();
    }

    /// <summary>Settings → Devices → Add: shows the sensor search on top of Live and starts scanning.</summary>
    public void OpenToAddSensor()
    {
        PairFailed = false;
        IsOpen = true;
        Start(lifetime);
    }

    [RelayCommand]
    private void Cancel()
    {
        Close();
    }

    [RelayCommand]
    private void Retry()
    {
        Start(lifetime);
    }

    [RelayCommand]
    private async Task PairAsync(NearbySensorItem sensor)
    {
        IsPairing = true;
        PairFailed = false;

        try
        {
            var name = string.IsNullOrWhiteSpace(RoomName) ? localizer.Get("Onboarding_Room_DefaultName") : RoomName;
            await enrollment.EnrollAsync(sensor.Id, name, CancellationToken.None);
            Close();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            PairFailed = true;

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(exception, "Adding the sensor {Address} failed.", sensor.Id.Address);
            }
        }
        finally
        {
            IsPairing = false;
        }
    }

    /// <summary>Bluetooth came on (for example just after the app started, before Windows had reported it): scan again.</summary>
    private void OnRadioTurnedOn(object? sender, EventArgs args)
    {
        if (active && radio.IsOn && BluetoothOff && !lifetime.IsCancellationRequested)
        {
            Start(lifetime);
        }
    }

    private void Close()
    {
        IsOpen = false;
        active = false;
        running?.Cancel();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        IsScanning = true;
        NoSensorFound = false;
        _ = WatchForSilenceAsync(cancellationToken);

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
            IsScanning = false;
        }

        if (!cancellationToken.IsCancellationRequested && Nearby.Count == 0)
        {
            NoSensorFound = true;
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

    private async Task WatchForSilenceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(NoSensorAfter, timeProvider, cancellationToken);

            if (Nearby.Count == 0)
            {
                NoSensorFound = true;
            }
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
                Add(sensor);
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

    private void Add(DiscoveredSensor sensor)
    {
        lock (gate)
        {
            var list = Nearby.ToList();
            var index = list.FindIndex(known => known.Id == sensor.Id);

            if (index >= 0)
            {
                list[index] = sensor;
            }
            else
            {
                list.Add(sensor);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Found sensor {Address} ({Name}), signal {Signal} dBm.", sensor.Id.Address, sensor.Name, sensor.SignalDbm);
                }
            }

            Nearby = list;
        }
    }

    private NearbySensorItem ToItem(DiscoveredSensor sensor)
    {
        var detail = sensor.SignalDbm is { } dbm
            ? localizer.Get(SignalClassifier.Classify(dbm) switch
            {
                SignalLevel.Good => "Footer_Signal_Good",
                SignalLevel.Fair => "Footer_Signal_Fair",
                _ => "Footer_Signal_Weak"
            })
            : string.Empty;

        return new NearbySensorItem(sensor.Id, sensor.Name ?? sensor.Id.Address, detail);
    }

    /// <summary>The providers' own steps, then "pick your sensor". The providers' steps count as done once a sensor has been found.</summary>
    private List<OnboardingStep> BuildChecklist()
    {
        var found = Nearby.Count > 0;
        var keys = providers.SelectMany(provider => provider.OnboardingStepKeys).Distinct().ToList();
        var steps = keys.Select((key, index) => new OnboardingStep(index + 1, localizer.Get(key), found)).ToList();
        steps.Add(new OnboardingStep(steps.Count + 1, localizer.Get("Onboarding_Step_Pick"), false));

        return steps;
    }
}

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
/// First run: the checklist, the sensors found nearby, the room name, and pairing. The search itself is <see cref="SensorSearch"/>; this class shows its state on the UI thread.
/// Pairing saves the room, selects it and starts the sensor session (<see cref="DeviceEnrollment"/>); the shell then leaves this screen because a sensor exists.
/// </summary>
public sealed partial class OnboardingViewModel : ObservableObject
{
    /// <summary>How long a scan may find nothing before the screen says so.</summary>
    public static readonly TimeSpan NoSensorAfter = SensorSearch.NoSensorAfter;

    /// <summary>A scan that has found nothing is restarted after this long.</summary>
    public static readonly TimeSpan RescanAfter = SensorSearch.RescanAfter;

    private readonly IEnumerable<ISensorProvider> providers;
    private readonly DeviceEnrollment enrollment;
    private readonly ILocalizer localizer;
    private readonly ILogger<OnboardingViewModel> logger;
    private readonly SensorSearch search;
    private CancellationToken lifetime;

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

    public OnboardingViewModel(
        IEnumerable<ISensorProvider> providers,
        DeviceEnrollment enrollment,
        IBluetoothRadio radio,
        ILocalizer localizer,
        TimeProvider timeProvider,
        IUiDispatcher dispatcher,
        ILogger<OnboardingViewModel> logger)
    {
        this.providers = providers;
        this.enrollment = enrollment;
        this.localizer = localizer;
        this.logger = logger;
        search = new SensorSearch(providers, radio, timeProvider, logger);
        search.Changed += (_, _) => dispatcher.Post(ShowSearchState);
        roomName = localizer.Get("Onboarding_Room_DefaultName");
        checklist = BuildChecklist();
    }

    /// <summary>"Looking for sensors": a scan is running, nothing is found yet and nothing is wrong.</summary>
    public bool ShowSearching => IsScanning && !NoSensorFound && !BluetoothOff && NearbySensors.Count == 0;

    /// <summary>"No sensor found" with Retry.</summary>
    public bool ShowNoSensor => NoSensorFound && !BluetoothOff && NearbySensors.Count == 0;

    /// <summary>The Bluetooth hint with Retry.</summary>
    public bool ShowBluetoothOff => BluetoothOff && NearbySensors.Count == 0;

    /// <summary>Starts scanning in the background until <paramref name="cancellationToken"/> is cancelled or a sensor is paired. Calling it again restarts the scan.</summary>
    public void Start(CancellationToken cancellationToken)
    {
        lifetime = cancellationToken;
        search.Start(cancellationToken);
    }

    /// <summary>Scans with every provider until it is cancelled or Bluetooth goes off. Stopping is not an error.</summary>
    public Task ScanAsync(CancellationToken cancellationToken) => search.ScanAsync(cancellationToken);

    /// <summary>Settings → Devices → Add: shows the sensor search on top of Live and starts scanning.</summary>
    public void OpenToAddSensor()
    {
        PairFailed = false;
        IsOpen = true;
        Start(lifetime);
    }

    partial void OnNearbyChanged(IReadOnlyList<DiscoveredSensor> value)
    {
        NearbySensors = [.. value.Select(ToItem)];
        Checklist = BuildChecklist();
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

    private void Close()
    {
        IsOpen = false;
        search.Stop();
    }

    /// <summary>Copies the search state to the properties the view binds to. Always runs on the UI thread and always reads the latest state, so a post that arrives late does no harm.</summary>
    private void ShowSearchState()
    {
        Nearby = search.Nearby;
        BluetoothOff = search.BluetoothOff;
        IsScanning = search.IsScanning;
        NoSensorFound = search.NoSensorFound;
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

    /// <summary>The steps the providers name, then "pick your sensor". The provider steps count as done once a sensor has been found.</summary>
    private List<OnboardingStep> BuildChecklist()
    {
        var found = Nearby.Count > 0;
        var keys = providers.SelectMany(provider => provider.OnboardingStepKeys).Distinct().ToList();
        var steps = keys.Select((key, index) => new OnboardingStep(index + 1, localizer.Get(key), found)).ToList();
        steps.Add(new OnboardingStep(steps.Count + 1, localizer.Get("Onboarding_Step_Pick"), false));

        return steps;
    }
}

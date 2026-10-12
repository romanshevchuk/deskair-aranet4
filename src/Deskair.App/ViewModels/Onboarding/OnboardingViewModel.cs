using CommunityToolkit.Mvvm.ComponentModel;
using Deskair.Services.Devices;
using Deskair.Services.Platform;
using Deskair.Services.Sensors;
using Microsoft.Extensions.Logging;

namespace Deskair.App.ViewModels.Onboarding;

public sealed partial class OnboardingViewModel : ObservableObject
{
    private readonly IEnumerable<ISensorProvider> providers;
    private readonly DeviceRegistry devices;
    private readonly IBluetoothRadio radio;
    private readonly ILogger<OnboardingViewModel> logger;
    private readonly Lock gate = new();
    private CancellationTokenSource? scan;

    [ObservableProperty]
    private int stepIndex;

    [ObservableProperty]
    private IReadOnlyList<DiscoveredSensor> nearby = [];

    [ObservableProperty]
    private string? roomName;

    /// <summary>True when the scan could not run, or had to stop, because Bluetooth is off. The view shows the Bluetooth hint.</summary>
    [ObservableProperty]
    private bool bluetoothOff;

    public OnboardingViewModel(
        IEnumerable<ISensorProvider> providers,
        DeviceRegistry devices,
        IBluetoothRadio radio,
        ILogger<OnboardingViewModel> logger)
    {
        this.providers = providers;
        this.devices = devices;
        this.radio = radio;
        this.logger = logger;
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
            }

            Nearby = list;
        }
    }
}

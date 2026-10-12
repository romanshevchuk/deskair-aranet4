using Deskair.Services.Platform;
using Microsoft.Extensions.Logging;
using Windows.Devices.Radios;

namespace Deskair.App.Platform.Bluetooth;

/// <summary>
/// Follows the Windows Bluetooth radio. It reads the state once at start and then follows the change event; <see cref="StateChanged"/> fires when it changes.
/// With no Bluetooth adapter, or without permission to read it, <see cref="IsOn"/> stays false. Needs a manual check by toggling Bluetooth in Windows settings.
/// </summary>
public sealed class BluetoothRadioMonitor : IBluetoothRadio, IDisposable
{
    private readonly ILogger<BluetoothRadioMonitor> logger;
    private Radio? radio;
    private volatile bool isOn;

    public BluetoothRadioMonitor(ILogger<BluetoothRadioMonitor> logger)
    {
        this.logger = logger;
        _ = InitializeAsync();
    }

    public event EventHandler? StateChanged;

    public bool IsOn => isOn;

    public void Dispose()
    {
        if (radio is not null)
        {
            radio.StateChanged -= OnRadioStateChanged;
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            var access = await Radio.RequestAccessAsync();

            if (access != RadioAccessStatus.Allowed)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Windows did not allow reading the Bluetooth radio ({Access}); Bluetooth is treated as off.", access);
                }

                return;
            }

            var radios = await Radio.GetRadiosAsync();
            radio = radios.FirstOrDefault(candidate => candidate.Kind == RadioKind.Bluetooth);

            if (radio is null)
            {
                return;
            }

            radio.StateChanged += OnRadioStateChanged;
            Update(radio.State);
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "Reading the Bluetooth radio failed; Bluetooth is treated as off.");
            }
        }
    }

    private void OnRadioStateChanged(Radio sender, object args) => Update(sender.State);

    private void Update(RadioState state)
    {
        var on = state == RadioState.On;

        if (on == isOn)
        {
            return;
        }

        isOn = on;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

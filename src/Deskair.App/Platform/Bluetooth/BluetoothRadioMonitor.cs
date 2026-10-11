using Deskair.Services.Platform;

namespace Deskair.App.Platform.Bluetooth;

public sealed class BluetoothRadioMonitor : IBluetoothRadio
{
    // TODO(BLE-03a) [M, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Bluetooth" (check radio state to show the Bluetooth off message).
    // Input : none.
    // Output: IsOn reflects the Windows Bluetooth radio; StateChanged fires when it changes.
    // Rules : read the radio state once at start and then follow its change event.
    // Edge  : no Bluetooth adapter means IsOn = false.
    // Done  : manual check by toggling Bluetooth in Windows settings.
    public bool IsOn => throw new NotImplementedException("TODO(BLE-03a)");

    public event EventHandler? StateChanged
    {
        add => throw new NotImplementedException("TODO(BLE-03a)");
        remove => throw new NotImplementedException("TODO(BLE-03a)");
    }
}

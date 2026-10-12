using Deskair.Services.Platform;

namespace Deskair.App.Platform.Bluetooth;

/// <summary>A radio that is always on. The simulated app uses it so it needs no Bluetooth hardware.</summary>
public sealed class SimulatedRadio : IBluetoothRadio
{
    public bool IsOn => true;

    public event EventHandler? StateChanged
    {
        add { }
        remove { }
    }
}

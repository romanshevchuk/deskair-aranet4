using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

/// <summary>A Bluetooth radio a test can switch on and off.</summary>
public sealed class FakeBluetoothRadio : IBluetoothRadio
{
    private bool isOn = true;

    public bool IsOn
    {
        get => isOn;
        set
        {
            if (isOn == value)
            {
                return;
            }

            isOn = value;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? StateChanged;
}

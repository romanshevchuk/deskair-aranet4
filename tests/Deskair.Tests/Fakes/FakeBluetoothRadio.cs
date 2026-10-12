using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

/// <summary>A Bluetooth radio a test can switch on and off.</summary>
public sealed class FakeBluetoothRadio : IBluetoothRadio
{
    private bool isOn = true;

    /// <summary>Makes reading <see cref="IsOn"/> throw, like a radio API that fails.</summary>
    public bool FailReads { get; set; }

    public bool IsOn
    {
        get => FailReads ? throw new InvalidOperationException("radio unavailable") : isOn;
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

using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

public sealed class FakeSystemSettings : ISystemSettings
{
    public int BluetoothOpened { get; private set; }

    public void OpenBluetoothSettings()
    {
        BluetoothOpened++;
    }
}

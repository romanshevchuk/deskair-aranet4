using System.Diagnostics;
using Deskair.Services.Platform;

namespace Deskair.App.Platform.Launch;

/// <summary>Opens Windows Settings pages through their <c>ms-settings:</c> addresses.</summary>
public sealed class WindowsSystemSettings : ISystemSettings
{
    public void OpenBluetoothSettings()
    {
        using var settings = Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
    }
}

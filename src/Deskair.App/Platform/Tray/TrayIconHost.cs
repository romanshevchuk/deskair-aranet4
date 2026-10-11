using Deskair.Domain.Zones;

namespace Deskair.App.Platform.Tray;

/// <summary>Owns the notification-area icon: click handling, tooltip and the context menu.</summary>
public sealed class TrayIconHost : IDisposable
{
    // TODO(TRAY-01a) [L, sonnet]
    // Spec: docs/design/screens-and-states.md "Tray icon states" and "Screens" (tray menu); docs/engineering/wpf-mapping.md "Tray".
    // Input : the current zone (null = not connected) and optional CO2 value.
    // Output: the tray icon and tooltip ("Office · 1,068 ppm · Elevated") are updated.
    // Rules : left click opens the flyout, right click the menu; the icon colour always updates, even when toasts are off.
    // Edge  : a tray icon library needs approval first (questions.md Q8).
    // Done  : manual check on Windows 10 and 11; no automated test.
    public void Update(Zone? zone, double? co2Ppm)
        => throw new NotImplementedException("TODO(TRAY-01a)");

    public void Dispose()
    {
    }
}

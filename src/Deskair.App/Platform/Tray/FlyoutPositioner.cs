using System.Windows;

namespace Deskair.App.Platform.Tray;

public static class FlyoutPositioner
{
    // TODO(TRAY-02a) [M, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Tray" and "Riskiest pieces"; spike S2.
    // Input : tray icon rectangle, flyout size, monitor work area (all in device-independent units).
    // Output: the top-left point for the flyout window.
    // Rules : place it next to the icon on the side facing the screen; keep it fully inside the work area.
    // Edge  : taskbar at any edge, icon near a corner, flyout larger than the free space.
    // Done  : add Tests/Platform/FlyoutPositionerTests for the four taskbar edges and make them pass.
    public static Point Position(Rect trayIcon, Size flyout, Rect workArea)
        => throw new NotImplementedException("TODO(TRAY-02a)");
}

using System.Runtime.InteropServices;
using System.Windows;

namespace Deskair.App.Platform.Tray;

/// <summary>Where the tray icon (or the cursor) is and what the monitor under it looks like: its work area (what the taskbar leaves) and its scale. Pixels in, device-independent units out.</summary>
internal readonly record struct ScreenPoint(Point Pixels, Rect WorkAreaPixels, double Scale)
{
    private const int EffectiveDpi = 0;
    private const double DefaultDpi = 96;

    /// <summary>The cursor now, or null when Windows does not give it.</summary>
    internal static ScreenPoint? AtCursor()
    {
        return NativeMethods.GetCursorPos(out var cursor) ? At(cursor) : null;
    }

    /// <summary>
    /// The middle of the tray icon with this id, or null when Windows does not know where it is (it can be hidden).
    /// The cursor is no help when the icon was opened with the keyboard (Win+B, then Enter): it is wherever the mouse was left.
    /// </summary>
    internal static ScreenPoint? AtTrayIcon(Guid iconId)
    {
        var identifier = new NativeMethods.NativeIconIdentifier { size = Marshal.SizeOf<NativeMethods.NativeIconIdentifier>(), guid = iconId };

        if (NativeMethods.Shell_NotifyIconGetRect(ref identifier, out var rect) != 0 || rect.right <= rect.left || rect.bottom <= rect.top)
        {
            return null;
        }

        var middle = new NativeMethods.NativePoint { x = (rect.left + rect.right) / 2, y = (rect.top + rect.bottom) / 2 };

        return At(middle);
    }

    internal Rect WorkArea => new(WorkAreaPixels.X / Scale, WorkAreaPixels.Y / Scale, WorkAreaPixels.Width / Scale, WorkAreaPixels.Height / Scale);

    /// <summary>A small square around the point in device-independent units, standing in for the tray icon.</summary>
    internal Rect IconRectangle(double size) => new((Pixels.X / Scale) - (size / 2), (Pixels.Y / Scale) - (size / 2), size, size);

    private static ScreenPoint? At(NativeMethods.NativePoint point)
    {
        var monitor = NativeMethods.MonitorFromPoint(point, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.NativeMonitorInfo { size = Marshal.SizeOf<NativeMethods.NativeMonitorInfo>() };

        if (monitor == 0 || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return null;
        }

        var scale = NativeMethods.GetDpiForMonitor(monitor, EffectiveDpi, out var dpiX, out _) == 0 && dpiX > 0 ? dpiX / DefaultDpi : 1;
        var work = new Rect(info.work.left, info.work.top, info.work.right - info.work.left, info.work.bottom - info.work.top);

        return new ScreenPoint(new Point(point.x, point.y), work, scale);
    }
}

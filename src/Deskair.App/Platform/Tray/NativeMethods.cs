using System.Runtime.InteropServices;

namespace Deskair.App.Platform.Tray;

internal static class NativeMethods
{
    /// <summary>The width of a small icon (the tray icon size) at the current DPI.</summary>
    internal const int SmallIconWidth = 49;

    internal const uint MonitorDefaultToNearest = 2;
    internal const uint SwpNoSize = 0x0001;
    internal const uint SwpNoZOrder = 0x0004;
    internal const uint SwpNoActivate = 0x0010;

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(nint handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(nint monitor, ref NativeMonitorInfo info);

    [DllImport("shcore.dll")]
    internal static extern int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("shell32.dll")]
    internal static extern int Shell_NotifyIconGetRect(ref NativeIconIdentifier identifier, out NativeRect rect);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeIconIdentifier
    {
        internal int size;
        internal nint window;
        internal uint id;
        internal Guid guid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {
        internal int x;
        internal int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        internal int left;
        internal int top;
        internal int right;
        internal int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeMonitorInfo
    {
        internal int size;
        internal NativeRect monitor;
        internal NativeRect work;
        internal uint flags;
    }
}

using System.Runtime.InteropServices;

namespace Deskair.App.Platform.Tray;

internal static class NativeMethods
{
    /// <summary>The width of a small icon (the tray icon size) at the current DPI.</summary>
    internal const int SmallIconWidth = 49;

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(nint handle);
}

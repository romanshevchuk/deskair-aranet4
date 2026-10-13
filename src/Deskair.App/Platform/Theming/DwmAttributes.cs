using System.Runtime.InteropServices;

namespace Deskair.App.Platform.Theming;

/// <summary>The few Desktop Window Manager calls the window frame needs. Each returns Windows' result code (0 is success); a window that cannot take an attribute is not an error for the app.</summary>
internal static class DwmAttributes
{
    internal const int UseImmersiveDarkMode = 20;
    internal const int SystemBackdropType = 38;
    internal const int BackdropNone = 1;
    internal const int BackdropMainWindow = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(nint window, ref Margins margins);

    internal static int Set(nint window, int attribute, int value) => DwmSetWindowAttribute(window, attribute, ref value, sizeof(int));

    /// <summary>Lets the window frame, and so the backdrop behind it, show through the whole client area.</summary>
    internal static int ExtendFrameIntoClientArea(nint window)
    {
        var margins = new Margins { left = -1, right = -1, top = -1, bottom = -1 };

        return DwmExtendFrameIntoClientArea(window, ref margins);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int left;
        public int right;
        public int top;
        public int bottom;
    }
}

using System.Windows;

namespace Deskair.App.Platform.Theming;

/// <summary>Dark title bar and Mica backdrop on Windows 11; solid colour on Windows 10.</summary>
public sealed class BackdropService
{
    // TODO(WIN-01a) [L, sonnet]
    // Spec: docs/engineering/wpf-mapping.md "Theming", "Backdrop", "Custom title bar"; spike S3.
    // Input : a window, whether the theme is dark.
    // Output: the title bar and backdrop match the theme.
    // Rules : DWM attributes on Windows 11 only; keep snap layouts; Windows 10 falls back to the window.bg token.
    // Edge  : decide native title bar versus custom chrome first (questions.md Q14).
    // Done  : manual check on Windows 10 and 11 in both themes.
    public void Apply(Window window, bool dark)
        => throw new NotImplementedException("TODO(WIN-01a)");
}

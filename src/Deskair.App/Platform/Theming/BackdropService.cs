using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Deskair.App.Ui.Theme;

namespace Deskair.App.Platform.Theming;

/// <summary>
/// The window frame's look: a dark or light title bar that follows the theme, and the Mica backdrop on Windows 11 (build 22621 and later), where the window background
/// becomes see-through so Mica shows between the cards. On Windows 10 and early Windows 11 there is no Mica and the window keeps the solid <c>Window.Bg</c> token.
/// Snap layouts and dragging come from <see cref="System.Windows.Shell.WindowChrome"/> on the window itself (docs/plans/spike-s3-title-bar.md).
/// </summary>
public sealed class BackdropService
{
    /// <summary>The first Windows build where the documented system backdrop attribute exists (Windows 11 22H2).</summary>
    public const int FirstBuildWithSystemBackdrop = 22621;

    private readonly ThemeService theme;
    private readonly Func<int> windowsBuild;
    private readonly List<Window> attached = [];

    public BackdropService(ThemeService theme)
        : this(theme, () => Environment.OSVersion.Version.Build)
    {
    }

    /// <summary>For tests: another Windows build, to try the Windows 10 path on Windows 11.</summary>
    public BackdropService(ThemeService theme, Func<int> windowsBuild)
    {
        this.theme = theme;
        this.windowsBuild = windowsBuild;
        theme.ThemeChanged += OnThemeChanged;
    }

    /// <summary>Applies the frame to the window now (creating its handle if needed) and again each time the theme changes, until the window closes.</summary>
    public void Attach(Window window)
    {
        attached.Add(window);
        window.Closed += (_, _) => attached.Remove(window);
        Apply(window, theme.IsDark);
    }

    /// <summary>Gives the window a dark or light title bar and, where Windows has it, the Mica backdrop. Safe to repeat.</summary>
    public void Apply(Window window, bool dark)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();

        _ = DwmAttributes.Set(handle, DwmAttributes.UseImmersiveDarkMode, dark ? 1 : 0);

        if (windowsBuild() >= FirstBuildWithSystemBackdrop)
        {
            UseMica(window, handle);
        }
        else
        {
            // No Mica here (Windows 10, early Windows 11): a solid background from the token, which follows the theme.
            window.SetResourceReference(Window.BackgroundProperty, "Window.Bg.Brush");
        }
    }

    private static void UseMica(Window window, nint handle)
    {
        var source = HwndSource.FromHwnd(handle);

        if (source?.CompositionTarget is { } target)
        {
            target.BackgroundColor = Colors.Transparent;
        }

        // Without extending the frame over the client area the backdrop only shows in the title bar.
        _ = DwmAttributes.ExtendFrameIntoClientArea(handle);

        if (DwmAttributes.Set(handle, DwmAttributes.SystemBackdropType, DwmAttributes.BackdropMainWindow) == 0)
        {
            window.Background = Brushes.Transparent;
        }
        else
        {
            window.SetResourceReference(Window.BackgroundProperty, "Window.Bg.Brush");
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        foreach (var window in attached.ToList())
        {
            Apply(window, theme.IsDark);
        }
    }
}

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Deskair.App.Platform.Theming;
using Deskair.Services.Settings;

namespace Deskair.Tests.Ui;

public class BackdropServiceTests
{
    private const int Windows11 = 26200;
    private const int Windows10 = 19045;
    private const int DwmDarkMode = 20;
    private const int DwmBackdropType = 38;
    private const int MainWindowBackdrop = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);

    private static int Attribute(Window window, int attribute)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        var result = DwmGetWindowAttribute(handle, attribute, out var value, sizeof(int));
        Assert.Equal(0, result);

        return value;
    }

    private static BackdropService Create(int build) => new(UiHost.Theme, () => build);

    [Fact]
    public void On_Windows_11_the_window_gets_Mica_and_a_see_through_background()
    {
        var result = UiHost.Run(ThemeChoice.Light, () =>
        {
            var window = new Window();

            Create(Windows11).Apply(window, dark: false);

            return (Backdrop: Attribute(window, DwmBackdropType), Dark: Attribute(window, DwmDarkMode), window.Background);
        });

        Assert.Equal(MainWindowBackdrop, result.Backdrop);
        Assert.Equal(0, result.Dark);
        Assert.Equal(Brushes.Transparent, result.Background);
    }

    [Fact]
    public void A_dark_theme_asks_for_a_dark_title_bar()
    {
        var dark = UiHost.Run(ThemeChoice.Dark, () =>
        {
            var window = new Window();

            Create(Windows11).Apply(window, dark: true);

            return Attribute(window, DwmDarkMode);
        });

        Assert.Equal(1, dark);
    }

    [Fact]
    public void Before_Windows_11_22H2_there_is_no_Mica_and_the_background_is_the_window_colour_token()
    {
        var result = UiHost.Run(ThemeChoice.Light, () =>
        {
            var window = new Window();

            Create(Windows10).Apply(window, dark: false);

            return (Backdrop: Attribute(window, DwmBackdropType), window.Background, Token: (Brush)Application.Current.FindResource("Window.Bg.Brush"));
        });

        Assert.Equal(0, result.Backdrop);
        Assert.Same(result.Token, result.Background);
    }

    [Fact]
    public void Applying_again_after_Mica_to_an_older_build_gives_the_solid_background_back()
    {
        var background = UiHost.Run(ThemeChoice.Light, () =>
        {
            var window = new Window();
            Create(Windows11).Apply(window, dark: false);

            Create(Windows10).Apply(window, dark: false);

            return window.Background;
        });

        Assert.NotEqual(Brushes.Transparent, background);
    }

    [Fact]
    public void Attach_applies_when_the_window_gets_its_handle_and_follows_the_theme()
    {
        var result = UiHost.Run(ThemeChoice.Light, () =>
        {
            var window = new Window();
            var service = Create(Windows11);

            service.Attach(window);
            var light = Attribute(window, DwmDarkMode);

            UiHost.Theme.Apply(ThemeChoice.Dark);
            var dark = Attribute(window, DwmDarkMode);
            UiHost.Theme.Apply(ThemeChoice.Light);

            return (light, dark, Backdrop: Attribute(window, DwmBackdropType));
        });

        Assert.Equal((0, 1, MainWindowBackdrop), result);
    }

    [Fact]
    public void A_closed_window_is_no_longer_followed()
    {
        UiHost.Run(ThemeChoice.Light, () =>
        {
            var window = new Window { ShowActivated = false, Width = 50, Height = 50 };
            var service = Create(Windows11);
            service.Attach(window);
            window.Show();
            window.Close();

            // Changing the theme after the window is gone must not throw.
            UiHost.Theme.Apply(ThemeChoice.Dark);
            UiHost.Theme.Apply(ThemeChoice.Light);
        });
    }
}

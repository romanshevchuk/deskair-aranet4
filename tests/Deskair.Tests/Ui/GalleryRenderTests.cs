using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.Ui.Views.Gallery;
using Deskair.Services.Settings;

namespace Deskair.Tests.Ui;

/// <summary>
/// Renders the gallery to PNG files in artifacts/visual so the controls can be checked by eye without running the app.
/// Off in normal runs; switch on with the environment variable DESKAIR_RENDER=1.
/// </summary>
public class GalleryRenderTests
{
    private const double PageWidth = 1200;

    [Theory]
    [InlineData(ThemeChoice.Light, "en-US")]
    [InlineData(ThemeChoice.Dark, "en-US")]
    [InlineData(ThemeChoice.Light, "uk-UA")]
    [InlineData(ThemeChoice.Dark, "uk-UA")]
    public void Render_the_gallery(ThemeChoice theme, string culture)
    {
        RenderOutput.Require();

        var png = UiHost.Run(theme, () =>
        {
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
            var language = new LanguageService(UiHost.Localizer, new WeakReferenceMessenger(), CultureInfo.GetCultureInfo(culture));
            var window = new GalleryWindow(UiHost.Theme, language);

            // The gallery's own switches select Light and English while it loads, so set the wanted look afterwards.
            UiHost.Theme.Apply(theme);
            UiHost.Localizer.SetCulture(CultureInfo.GetCultureInfo(culture));
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            var scroll = FindScrollViewer(window);
            var page = (System.Windows.FrameworkElement)scroll.Content;
            scroll.Content = null;

            return ControlRenderer.ToPng(page, PageWidth);
        });

        RenderOutput.Save($"gallery-{theme.ToString().ToLowerInvariant()}-{culture[..2]}.png", png);
    }

    private static ScrollViewer FindScrollViewer(GalleryWindow window)
    {
        var grid = (Grid)window.Content;

        return grid.Children.OfType<ScrollViewer>().Single();
    }
}

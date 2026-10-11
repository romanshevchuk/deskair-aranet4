using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using Deskair.App.Localization;
using Deskair.App.Ui.Views.Gallery;
using Deskair.App.Ui.Views.Shell;
using Deskair.App.ViewModels.Shell;

namespace Deskair.Tests.Ui;

/// <summary>
/// Builds the windows without showing them. This catches XAML and resource errors that the compiler cannot see.
/// It does not check how anything looks: the project owner reviews that in the gallery.
/// </summary>
public class WindowSmokeTests
{
    [Fact]
    public void The_main_window_loads()
    {
        UiHost.Run(() =>
        {
            var window = new MainWindow(new ShellViewModel());

            Assert.Equal("Deskair", window.Title);
        });
    }

    [Fact]
    public void The_gallery_window_loads_with_a_localized_title()
    {
        UiHost.Run(() =>
        {
            var language = new LanguageService(UiHost.Localizer, new WeakReferenceMessenger(), CultureInfo.GetCultureInfo("en-US"));

            var window = new GalleryWindow(UiHost.Theme, language);

            Assert.Equal("Control gallery", window.Title);
        });
    }
}
